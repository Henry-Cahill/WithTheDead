using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A person in the Zone: a stalker, a soldier, a scavenger, a warden on a tower, a zealot of the Choir, a hired
    /// companion. Humans fight from cover: they look for something solid between them and the threat, crouch behind
    /// it, peek out to fire short bursts, reload in cover, push or flank when they're bold and fall back when hurt —
    /// and some put their hands up. Shots leave the muzzle and are resolved like the survivor's own, so cover works
    /// both ways. A body keeps everything the person carried (gun with its mods, ammo, armour, money, sometimes a PDA).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HumanNpc : MonoBehaviour, IDamageable, IZombieAttackable
    {
        public enum Mode { Idle, Travel, Guard, Investigate, Combat, Flee, Surrendered, Follow, Dead }

        public static readonly List<HumanNpc> All = new List<HumanNpc>();

        public string Name { get; private set; }
        public HumanLoadout Loadout { get; private set; }
        public Actor Actor { get; private set; }
        public Mode State { get; private set; } = Mode.Idle;
        public Actor Target => _target;
        public bool InCover => _inCover;
        public bool Crouched { get; private set; }
        public bool Aiming => State == Mode.Combat && _target != null;
        public ItemDef Gun { get; private set; }
        public Inventory Pack { get; private set; }
        public float Health01 => _maxHp > 0f ? _hp / _maxHp : 0f;
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        /// <summary>A squad this person belongs to (A-Life), or null.</summary>
        [System.NonSerialized] public object SquadTag;
        /// <summary>Guard post / travel goal / the actor to follow.</summary>
        public Vector3 Goal;
        public Actor FollowTarget;
        /// <summary>When true this person never leaves the guard post (cordon towers).</summary>
        public bool HoldPost;

        public event System.Action<HumanNpc> Died;

        private NavMeshAgent _agent;
        private HumanNpcAvatar _avatar;
        private float _hp, _maxHp;
        private string _ammoId;
        private int _mag, _magSize, _reserve;
        private float _fireCooldown, _range, _noise;
        private bool _auto;
        private ItemDef _vest, _helmet, _suit;
        private Actor _target;
        private Vector3 _lastSeen;
        private float _lastSeenTime = -999f, _spottedAt, _thinkAt, _nextShot, _reloadUntil, _peekUntil, _coverUntil, _repathAt;
        private int _burst;
        private Vector3 _cover;
        private bool _inCover, _hasCover;
        private readonly HashSet<Actor> _grudges = new HashSet<Actor>();
        private Light _flash;
        private float _flashOff;
        private System.Random _rng;

        #region Spawning
        /// <summary>Creates a person of <paramref name="faction"/> wearing <paramref name="loadout"/> (an id such as
        /// "vanguard", optionally "/gunId" to force the gun) on the navmesh near <paramref name="position"/>.</summary>
        public static HumanNpc Spawn(Vector3 position, string faction, string loadout, System.Random rng = null, string squad = null)
        {
            rng = rng ?? new System.Random(Random.Range(int.MinValue, int.MaxValue));
            HumanLoadout l = HumanLoadouts.Resolve(loadout, faction, rng, out string gunOverride);
            if (string.IsNullOrEmpty(faction)) faction = l.Faction;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 6f, NavMesh.AllAreas)) position = hit.position;

            var root = new GameObject("Human " + l.Id);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = new Vector3(0f, 0.95f, 0f);
            capsule.height = 1.9f;
            capsule.radius = 0.35f;

            var actor = root.AddComponent<Actor>();
            actor.Kind = ActorKind.Human;
            actor.Faction = faction;
            actor.Height = 1.8f;
            actor.Squad = squad;
            actor.NetId = Actor.AllocateNetId();

            var npc = root.AddComponent<HumanNpc>();
            npc._rng = rng;
            npc.Actor = actor;
            npc.Loadout = l;
            npc.Name = HumanNames.Roll(faction, rng);
            actor.DisplayName = npc.Name;
            npc.Equip(rng, gunOverride);

            npc._avatar = root.AddComponent<HumanNpcAvatar>();
            npc._avatar.Build(npc, rng);
            actor.Head = npc._avatar.Head;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CloneZConfig.IgnoreRaycastLayer;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.9f;
            agent.speed = 1.6f;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.4f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            npc._agent = agent;

            root.AddComponent<HumanNpcInteract>();
            root.AddComponent<ActorFog>();
            npc.Goal = position;
            return npc;
        }

        private void Equip(System.Random rng, string gunOverride)
        {
            HumanLoadout l = Loadout;
            Pack = new Inventory(999f);
            GunPick pick = l.PickGun(rng);
            Gun = ItemDatabase.Get(!string.IsNullOrEmpty(gunOverride) ? gunOverride : pick != null ? pick.GunId : null);
            if (Gun == null || !Gun.IsFirearm) Gun = ItemDatabase.Get("pistol");
            if (Gun != null)
            {
                _ammoId = pick != null && pick.AmmoId != null && Ballistics.Accepts(Gun, pick.AmmoId) ? pick.AmmoId : Gun.AmmoId;
                _magSize = Mathf.Max(1, Gun.MagazineSize);
                _mag = _magSize;
                _reserve = _magSize * (l.MagsMin + rng.Next(Mathf.Max(1, l.MagsMax - l.MagsMin + 1)));
                _fireCooldown = Mathf.Max(0.07f, Gun.FireCooldown);
                _range = Mathf.Max(25f, Gun.FirearmRange);
                _noise = Gun.FirearmNoise;
                GunStats gs = Ballistics.Gun(Gun.Id);
                _auto = gs != null && gs.FullAuto;

                var extra = new ItemExtra { FoundInRaid = true };
                for (int i = 0; i < 3; i++)
                {
                    if (rng.NextDouble() >= l.ModChance) continue;
                    var fits = new List<ModStats>();
                    foreach (ModStats m in Ballistics.AllMods) if (Ballistics.ModFits(Gun, m) && !extra.Mods.Contains(m.Id)) fits.Add(m);
                    if (fits.Count > 0) extra.Mods.Add(fits[rng.Next(fits.Count)].Id);
                }
                extra.Dirt = (float)rng.NextDouble() * 0.4f;
                int cond = Gun.MaxCondition > 0 ? Mathf.RoundToInt(Gun.MaxCondition * Mathf.Lerp(0.35f, 0.95f, (float)rng.NextDouble())) : -1;
                Pack.Add(Gun, 1, false, condition: cond, extra: extra);
            }

            _vest = rng.NextDouble() < l.VestChance ? HumanLoadouts.PickItem(l.Vests, rng) : null;
            _helmet = rng.NextDouble() < l.HelmetChance ? HumanLoadouts.PickItem(l.Helmets, rng) : null;
            _suit = rng.NextDouble() < l.SuitChance ? HumanLoadouts.PickItem(l.Suits, rng) : null;
            if (_vest != null) Pack.Add(_vest, 1, false, condition: Worn(_vest, rng), extra: new ItemExtra { FoundInRaid = true });
            if (_helmet != null) Pack.Add(_helmet, 1, false, condition: Worn(_helmet, rng), extra: new ItemExtra { FoundInRaid = true });
            if (_suit != null) Pack.Add(_suit, 1, false, condition: Worn(_suit, rng), extra: new ItemExtra { FoundInRaid = true });

            foreach (LootPick lp in l.Carry)
            {
                if (rng.NextDouble() >= lp.Chance) continue;
                ItemDef d = ItemDatabase.Get(lp.Id);
                if (d != null) Pack.Add(d, lp.Min + rng.Next(lp.Max - lp.Min + 1), false, extra: new ItemExtra { FoundInRaid = true });
            }
            int money = l.MoneyMin + rng.Next(Mathf.Max(1, l.MoneyMax - l.MoneyMin + 1));
            ItemDef cash = ItemDatabase.Get(Money.Id);
            if (cash != null && money > 0) Pack.Add(cash, money, false);
            if (rng.NextDouble() < l.PdaChance)
            {
                ItemDef pda = ItemDatabase.Get("pda");
                if (pda != null) Pack.Add(pda, 1, false, extra: new ItemExtra { FoundInRaid = true, Note = "pda:" + rng.Next() });
            }

            _maxHp = 240f + l.Skill * 90f;
            if (Loadout.Faction == Factions.Choir) _maxHp *= 1.15f;
            _hp = _maxHp;
        }

        private static int Worn(ItemDef d, System.Random rng)
            => d.MaxCondition > 0 ? Mathf.RoundToInt(d.MaxCondition * Mathf.Lerp(0.4f, 1f, (float)rng.NextDouble())) : -1;
        #endregion

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        #region Brain
        private void Update()
        {
            if (IsDead) return;
            UpdateFlash();
            if (Actor.Restrained || State == Mode.Surrendered)
            {
                Stop();
                Crouched = false;
                return;
            }

            if (Time.time >= _thinkAt)
            {
                _thinkAt = Time.time + Random.Range(0.2f, 0.32f);
                Perceive();
                Decide();
            }

            if (State == Mode.Combat) Fight();
            else if (State == Mode.Follow) FollowLeader();
        }

        private void Perceive()
        {
            bool night = HumanUtil.IsNight;
            float sight = (night ? 22f : 45f) * Mathf.Lerp(0.8f, 1.2f, Loadout.Skill);
            Actor best = null;
            float bestScore = float.MaxValue;
            Vector3 eye = Actor.EyePosition;
            Vector3 fwd = transform.forward;
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a == Actor || !a.IsAlive || a.Surrendered || a.Restrained) continue;
                bool hostile = _grudges.Contains(a) || Actor.IsHostileTo(a);
                if (FollowTarget != null && FollowTarget.IsAlive && (FollowTarget.IsHostileTo(a) && a.Kind != ActorKind.Player)) hostile = true;
                if (!hostile) continue;
                Vector3 to = a.Center - eye;
                float d = to.magnitude;
                float reach = sight * Mathf.Clamp(a.Visibility01, 0.25f, 1f);
                if (a.Kind == ActorKind.Zombie) reach = Mathf.Min(reach, 30f);
                if (d > reach) continue;
                bool alerted = State == Mode.Combat || State == Mode.Investigate;
                if (!alerted && d > 4f && Vector3.Angle(fwd, to) > 75f) continue;
                if (!Actor.CanSee(a, reach)) continue;
                float score = d * (a.Kind == ActorKind.Zombie ? 1.4f : 1f) * (a == _target ? 0.7f : 1f);
                if (score < bestScore) { bestScore = score; best = a; }
            }

            if (best != null)
            {
                if (_target != best) _spottedAt = Time.time + Mathf.Lerp(0.9f, 0.25f, Loadout.Skill);
                _target = best;
                _lastSeen = best.Center;
                _lastSeenTime = Time.time;
                return;
            }

            if (State != Mode.Combat && NoiseSystem.TryHear(transform.position, 10f, out Vector3 src, out float strength) && strength > 0.15f)
            {
                _lastSeen = src;
                _lastSeenTime = Time.time - 6f;
                if (State != Mode.Investigate && !HoldPost) Bark("Heard something.");
                State = Mode.Investigate;
            }
        }

        private void Decide()
        {
            float since = Time.time - _lastSeenTime;
            if (_target != null && (!_target.IsAlive || _target.Surrendered)) { _target = null; _hasCover = false; }

            if (_target != null && since < 14f)
            {
                if (Health01 < 0.3f && Loadout.Faction != Factions.Choir && State != Mode.Flee)
                {
                    if (_target.IsPlayerControlled && HumanUtil.FlatDistance(_target.transform.position, transform.position) < 14f
                        && _rng.NextDouble() < Loadout.SurrenderChance)
                    {
                        Surrender();
                        return;
                    }
                    if (!HoldPost && _rng.NextDouble() < 0.5f) { BeginFlee(); return; }
                }
                if (State != Mode.Combat && State != Mode.Flee)
                {
                    State = Mode.Combat;
                    Bark(_target.Kind == ActorKind.Zombie ? "Dead ones!" : "Contact!");
                    AlertSquad(_target);
                }
                return;
            }

            if (State == Mode.Combat)
            {
                _target = null;
                State = HoldPost ? Mode.Guard : Mode.Investigate;
                _hasCover = false;
                _inCover = false;
                Crouched = false;
            }

            switch (State)
            {
                case Mode.Investigate:
                    if (Time.time - _lastSeenTime > 25f || HumanUtil.FlatDistance(transform.position, _lastSeen) < 2f) State = HoldPost ? Mode.Guard : Mode.Idle;
                    else MoveTo(_lastSeen, 2.6f);
                    break;
                case Mode.Flee:
                    if (_agent.enabled && !_agent.pathPending && _agent.remainingDistance < 1.5f) State = Mode.Idle;
                    break;
                case Mode.Guard:
                    if (HumanUtil.FlatDistance(transform.position, Goal) > 1.5f) MoveTo(Goal, 1.6f);
                    else { Stop(); transform.Rotate(0f, Mathf.Sin(Time.time * 0.3f) * 20f * 0.3f, 0f); }
                    break;
                case Mode.Travel:
                    if (HumanUtil.FlatDistance(transform.position, Goal) < 2.5f) State = Mode.Idle;
                    else MoveTo(Goal, 1.6f);
                    break;
                case Mode.Follow:
                    break;
                default:
                    Stop();
                    break;
            }
        }

        /// <summary>Sets a travel goal (A-Life squads, companions told to go somewhere).</summary>
        public void TravelTo(Vector3 goal)
        {
            Goal = goal;
            if (State == Mode.Idle || State == Mode.Travel || State == Mode.Guard) State = HoldPost ? Mode.Guard : Mode.Travel;
        }

        public void SetGuard(Vector3 post)
        {
            Goal = post;
            HoldPost = true;
            State = Mode.Guard;
        }

        public void SetFollow(Actor leader)
        {
            FollowTarget = leader;
            HoldPost = false;
            State = Mode.Follow;
        }

        /// <summary>Told by a squadmate (or hurt by) someone: fight them.</summary>
        public void Alert(Actor threat)
        {
            if (IsDead || threat == null || State == Mode.Surrendered) return;
            if (_target == null)
            {
                _target = threat;
                _lastSeen = threat.Center;
                _lastSeenTime = Time.time - 2f;
                _spottedAt = Time.time + 0.6f;
            }
            if (State != Mode.Flee) State = Mode.Combat;
        }

        private void AlertSquad(Actor threat)
        {
            for (int i = 0; i < All.Count; i++)
            {
                HumanNpc o = All[i];
                if (o == this || o.IsDead || o.SquadTag == null || o.SquadTag != SquadTag) continue;
                if ((o.transform.position - transform.position).sqrMagnitude < 40f * 40f) o.Alert(threat);
            }
        }
        #endregion

        #region Combat
        private void Fight()
        {
            if (_target == null) return;
            Vector3 me = transform.position;
            Vector3 aimAt = _target.Center;
            bool visible = Time.time - _lastSeenTime < 0.5f;
            float dist = Vector3.Distance(me, _target.transform.position);

            Face(_target.transform.position);
            if (Time.time < _reloadUntil) { Crouched = _inCover; return; }
            if (_mag <= 0)
            {
                Reload();
                return;
            }

            // Melee range against the dead: back off while shooting.
            if (_target.Kind == ActorKind.Zombie && dist < 3f && Time.time >= _repathAt)
            {
                _repathAt = Time.time + 0.6f;
                Vector3 away = me + (me - _target.transform.position).normalized * 4f;
                MoveTo(away, 4f);
                _inCover = false;
            }
            else if (!_hasCover && Time.time >= _repathAt)
            {
                _repathAt = Time.time + 1.2f;
                if (TryFindCover(_target, out _cover)) { _hasCover = true; MoveTo(_cover, 4.2f); }
                else if (dist > _range * 0.8f) MoveTo(_target.transform.position, 3.5f);
                else Strafe();
            }
            else if (_hasCover && !_inCover)
            {
                if (HumanUtil.FlatDistance(me, _cover) < 0.8f) { _inCover = true; Stop(); _coverUntil = Time.time + Random.Range(6f, 14f); }
                else if (Time.time >= _repathAt) { _repathAt = Time.time + 0.8f; MoveTo(_cover, 4.2f); }
            }
            else if (_inCover)
            {
                // Bold fighters move up when the target goes quiet; everyone shifts cover now and then.
                bool lost = Time.time - _lastSeenTime > 4f;
                if (Time.time > _coverUntil || (lost && _rng.NextDouble() < Loadout.Aggression * 0.08f))
                {
                    _hasCover = false;
                    _inCover = false;
                    if (lost) MoveTo(Vector3.Lerp(me, _lastSeen, 0.5f), 4f);
                }
            }

            // Peek: stand up out of cover to shoot, duck back between bursts.
            Crouched = _inCover && Time.time > _peekUntil;
            if (!visible || Time.time < _spottedAt || Time.time < _nextShot) return;
            if (_inCover && Crouched)
            {
                _peekUntil = Time.time + Random.Range(1.2f, 2.4f);
                _nextShot = Time.time + 0.25f;
                return;
            }
            if (!ClearShot(aimAt)) { _repathAt = 0f; _hasCover = false; return; }
            Shoot(aimAt, dist);
        }

        private void Shoot(Vector3 aimAt, float dist)
        {
            Vector3 muzzle = Actor.EyePosition - Vector3.up * 0.1f + transform.forward * 0.4f;
            if (Loadout.Skill > 0.65f && _rng.NextDouble() < 0.18 && _target.Head != null) aimAt = _target.EyePosition;
            float spread = Mathf.Lerp(5.5f, 1.1f, Loadout.Skill);
            if (_agent.enabled && _agent.velocity.sqrMagnitude > 0.5f) spread *= 1.6f;
            if (HumanUtil.IsNight) spread *= 1.25f;
            if (dist > 30f) spread *= 1.2f;
            Vector3 dir = (aimAt - muzzle).normalized;
            dir = Quaternion.AngleAxis((float)(_rng.NextDouble() * 2 - 1) * spread, Vector3.up)
                * Quaternion.AngleAxis((float)(_rng.NextDouble() * 2 - 1) * spread * 0.6f, transform.right) * dir;

            AmmoStats ammo = Ballistics.Ammo(_ammoId);
            int pellets = ammo != null ? Mathf.Max(1, ammo.Pellets) : 1;
            for (int p = 0; p < pellets; p++)
            {
                Vector3 d = pellets > 1 ? Quaternion.Euler((float)(_rng.NextDouble() - 0.5) * 6f, (float)(_rng.NextDouble() - 0.5) * 6f, 0f) * dir : dir;
                CombatResolver.Fire(new ShotRequest
                {
                    Origin = muzzle, Direction = d, Range = _range, AmmoId = _ammoId, WeaponId = Gun != null ? Gun.Id : "gun",
                    DamageMult = 1f, Shooter = gameObject, ShooterActor = Actor, ShooterClient = ulong.MaxValue,
                });
            }
            _mag--;
            Sfx.Play(GunSound(Gun), muzzle, 1f, Random.Range(0.95f, 1.05f), 140f);
            NoiseSystem.Emit(transform.position, _noise);
            Flash(muzzle);
            if (_avatar != null) _avatar.OnFired();

            if (_auto)
            {
                if (_burst <= 0) _burst = 2 + _rng.Next(4);
                _burst--;
                _nextShot = Time.time + (_burst > 0 ? _fireCooldown : Random.Range(0.5f, 1.1f));
                if (_burst <= 0 && _inCover) _peekUntil = Time.time;
            }
            else
            {
                _nextShot = Time.time + Mathf.Max(_fireCooldown, Random.Range(0.45f, 0.9f));
                if (_inCover && _rng.NextDouble() < 0.4) _peekUntil = Time.time;
            }
        }

        public static string GunSound(ItemDef gun)
        {
            if (gun == null) return SfxIds.GunPistol;
            string cal = Ballistics.CaliberOfGun(gun) ?? "";
            if (gun.Id.Contains("mosin") || cal.Contains("7.62x54")) return SfxIds.GunSniper;
            if (gun.Id.Contains("shotgun") || cal.Contains("12")) return SfxIds.GunShotgun;
            if (gun.Id.Contains("pistol") || cal.Contains("9x")) return SfxIds.GunPistol;
            return SfxIds.GunRifle;
        }

        private void Reload()
        {
            if (_reserve <= 0)
            {
                _mag = 0;
                if (_target != null && _target.Kind != ActorKind.Zombie && Loadout.Faction != Factions.Choir && _rng.NextDouble() < 0.5) BeginFlee();
                return;
            }
            int take = Mathf.Min(_reserve, _magSize);
            _reserve -= take;
            _mag = take;
            _reloadUntil = Time.time + Random.Range(2.2f, 3.2f);
            Sfx.Play(SfxIds.Reload, transform.position + Vector3.up * 1.2f, 0.6f, 1f, 14f);
            if (_rng.NextDouble() < 0.5) Bark("Reloading!");
            if (_avatar != null) _avatar.OnReload(_reloadUntil - Time.time);
        }

        private bool ClearShot(Vector3 aimAt)
        {
            Vector3 from = Actor.EyePosition;
            Vector3 d = aimAt - from;
            float len = d.magnitude;
            if (Physics.Raycast(from, d / len, out RaycastHit hit, len, CombatResolver.ShotMask, QueryTriggerInteraction.Ignore))
            {
                Actor a = hit.collider.GetComponentInParent<Actor>();
                if (a == null) return hit.distance > len - 0.6f;   // the world is in the way
                if (a == Actor) return true;
                if (a != _target && !Actor.IsHostileTo(a)) return false;   // don't shoot through friends
            }
            return true;
        }

        // Cover: a spot nearby with something solid between it and the threat at crouch height.
        private bool TryFindCover(Actor threat, out Vector3 spot)
        {
            spot = default;
            Vector3 me = transform.position;
            Vector3 t = threat.Center;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < 14; i++)
            {
                Vector2 o = Random.insideUnitCircle * 11f;
                Vector3 p = me + new Vector3(o.x, 0f, o.y);
                if (!NavMesh.SamplePosition(p, out NavMeshHit nh, 2f, NavMesh.AllAreas)) continue;
                p = nh.position;
                Vector3 low = p + Vector3.up * 0.8f;
                Vector3 toT = t - low;
                float dt = toT.magnitude;
                if (dt < 5f) continue;
                if (!Physics.Raycast(low, toT / dt, out RaycastHit h, dt, Actor.SightMask, QueryTriggerInteraction.Ignore) || h.distance > 2.2f) continue;
                Vector3 high = p + Vector3.up * 1.6f;
                bool peek = !Physics.Raycast(high, (t - high).normalized, Vector3.Distance(t, high) - 0.4f, Actor.SightMask, QueryTriggerInteraction.Ignore);
                float score = Vector3.Distance(me, p) + (peek ? 0f : 4f) + (Loadout.Aggression > 0.6f ? dt * 0.15f : -dt * 0.1f);
                if (score < best) { best = score; spot = p; found = true; }
            }
            return found;
        }

        private void Strafe()
        {
            Vector3 side = Vector3.Cross(Vector3.up, (_target.transform.position - transform.position).normalized) * (_rng.NextDouble() < 0.5 ? -1f : 1f);
            MoveTo(transform.position + side * Random.Range(2f, 4.5f), 3f);
        }

        private void BeginFlee()
        {
            State = Mode.Flee;
            _hasCover = false;
            _inCover = false;
            Crouched = false;
            Vector3 away = _target != null ? (transform.position - _target.transform.position).normalized : transform.forward;
            Vector3 dest = HumanUtil.ClampToMap(transform.position + away * 28f, 6f);
            MoveTo(dest, 4.6f);
            Bark("Falling back!");
        }

        private void Surrender()
        {
            State = Mode.Surrendered;
            Actor.Surrendered = true;
            _target = null;
            Stop();
            Crouched = false;
            Bark("Don't shoot! I give up!");
            GameEvents.RaiseSubtitle(Name, "Don't shoot! Take what you want!");
        }

        /// <summary>Let a surrendered person go (they walk off; mercy is remembered).</summary>
        public void Release()
        {
            Actor.Surrendered = false;
            Actor.Restrained = false;
            _grudges.Clear();
            State = Mode.Flee;
            Vector3 away = Actor.LocalPlayer != null ? (transform.position - Actor.LocalPlayer.transform.position).normalized : transform.forward;
            MoveTo(HumanUtil.ClampToMap(transform.position + away * 40f, 6f), 3.5f);
            MoralLedger.Record("let a surrendered stalker go", MoralLedger.SparedSurrendered);
            GameEvents.RaiseQuestProgress("spare", 1f);
        }

        private void FollowLeader()
        {
            if (FollowTarget == null || !FollowTarget.IsAlive) { State = Mode.Idle; return; }
            float d = HumanUtil.FlatDistance(transform.position, FollowTarget.transform.position);
            if (d > 4f && Time.time >= _repathAt)
            {
                _repathAt = Time.time + 0.5f;
                Vector3 behind = FollowTarget.transform.position - FollowTarget.transform.forward * 2.2f + FollowTarget.transform.right * 1.2f;
                MoveTo(behind, d > 10f ? 4.5f : 2f);
            }
            else if (d < 2.5f) Stop();
        }
        #endregion

        #region Movement helpers
        private void MoveTo(Vector3 p, float speed)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
            _agent.speed = speed * (Crouched ? 0.5f : 1f);
            _agent.isStopped = false;
            if ((_agent.destination - p).sqrMagnitude > 0.25f) _agent.SetDestination(p);
        }

        private void Stop()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        private void Face(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            if (_agent != null && _agent.enabled) _agent.updateRotation = false;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_agent != null && _agent.enabled && State != Mode.Combat) _agent.updateRotation = true;
        }

        private void Flash(Vector3 at)
        {
            if (_flash == null)
            {
                var go = new GameObject("MuzzleFlash");
                go.transform.SetParent(transform, false);
                _flash = go.AddComponent<Light>();
                _flash.type = LightType.Point;
                _flash.range = 7f;
                _flash.intensity = 3f;
                _flash.color = new Color(1f, 0.8f, 0.5f);
                _flash.shadows = LightShadows.None;
            }
            _flash.transform.position = at;
            _flash.enabled = true;
            _flashOff = Time.time + 0.05f;
        }

        private void UpdateFlash()
        {
            if (_flash != null && _flash.enabled && Time.time > _flashOff) _flash.enabled = false;
        }

        private float _barkAt;
        private void Bark(string line)
        {
            if (Time.time < _barkAt || Actor.LocalPlayer == null) return;
            if ((Actor.LocalPlayer.transform.position - transform.position).sqrMagnitude > 25f * 25f) return;
            _barkAt = Time.time + 6f;
            GameEvents.RaiseSubtitle(Name, line);
        }
        #endregion

        #region Damage
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            float hp = info.AmountHp;
            BodyZone zone = info.ZoneKnown ? info.Zone : BodyZones.FromHit(transform, info.HitPoint);
            if (!info.HpScale) hp *= CloneZConfig.NonPlayerReceivedDamageMultiplier;

            switch (zone)
            {
                case BodyZone.Head: hp *= 2.3f; hp = Armour(hp, info, _helmet); break;
                case BodyZone.Thorax: case BodyZone.Stomach: hp = Armour(hp, info, _suit ?? _vest); break;
                default: hp *= 0.65f; break;
            }
            if (info.Kind != DamageKind.Bullet && info.Kind != DamageKind.Melee && info.Kind != DamageKind.Bite && _suit != null)
            {
                ArmorStats s = Ballistics.Armor(_suit.Id);
                if (s != null) hp *= s.Resist.Multiplier(info.Kind);
            }
            if (Loadout.Faction == Factions.Choir && info.Kind == DamageKind.Psi) hp *= 0.1f;
            _hp -= hp;

            Actor attacker = info.Source != null ? info.Source.GetComponentInParent<Actor>() : null;
            if (attacker != null && attacker != Actor)
            {
                if (attacker.IsPlayerControlled && !Actor.IsHostileTo(attacker))
                {
                    _grudges.Add(attacker);
                    if (attacker == Actor.LocalPlayer) Factions.AdjustReputation(Actor.Faction, -40f, "attacked one of theirs");
                }
                if (State == Mode.Surrendered && _hp > 0f) { State = Mode.Flee; Actor.Surrendered = false; }
                Alert(attacker);
                AlertSquad(attacker);
            }
            if (_avatar != null) _avatar.OnHit(info.HitDirection);
            if (_hp <= 0f) Die(info, attacker);
        }

        private static float Armour(float hp, in DamageInfo info, ItemDef worn)
        {
            if (worn == null || info.Kind != DamageKind.Bullet) return hp;
            ArmorStats a = Ballistics.Armor(worn.Id);
            if (a == null || a.Class <= 0) return hp;
            float chance = Ballistics.PenetrationChance(info.Penetration, a.Class, 0.8f);
            return Random.value < chance ? hp * 0.85f : hp * a.BluntThrough;
        }

        public void ReceiveZombieAttack(bool bite)
        {
            var info = new DamageInfo(bite ? 45f : 22f, bite ? DamageKind.Bite : DamageKind.Melee, Actor.Center, transform.forward, null) { HpScale = true };
            TakeDamage(info);
        }

        private void Die(in DamageInfo info, Actor killer)
        {
            if (IsDead) return;
            IsDead = true;
            State = Mode.Dead;
            Actor.Surrendered = false;
            if (_agent != null) _agent.enabled = false;
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.enabled = false;
            if (_avatar != null) _avatar.OnDied(info.HitDirection);
            if (killer == Actor.LocalPlayer && killer != null)
            {
                if (Actor.Faction != Factions.Choir && Actor.Faction != Factions.Scavs) Factions.AdjustReputation(Actor.Faction, -120f, "killed one of theirs");
                CharacterProfile c = ProfileStore.ActiveCharacter;
                if (c != null) c.Kills++;
            }

            // The body keeps everything the person carried.
            if (_mag > 0 || _reserve > 0)
            {
                ItemDef ammo = ItemDatabase.Get(_ammoId);
                if (ammo != null) Pack.Add(ammo, Mathf.Max(1, _reserve / 2 + _mag), false, extra: new ItemExtra { FoundInRaid = true });
            }
            var loot = gameObject.AddComponent<LootContainer>();
            loot.DisplayName = "Body of " + Name;
            loot.Type = ContainerType.Corpse;
            loot.Contents = Pack;
            loot.SearchTime = CloneZConfig.SearchTimeCorpse;
            loot.Lifetime = 420f;
            Died?.Invoke(this);
        }
        #endregion
    }

    /// <summary>The person's body: dressed from their loadout, carrying their gun, animated from the brain.</summary>
    public sealed class HumanNpcAvatar : MonoBehaviour
    {
        public HumanoidRig Rig { get; private set; }
        public HumanoidAnimator Animator { get; private set; }
        public Transform Head => Rig != null ? Rig.Bone(HumanBone.Head) : null;

        private HumanNpc _npc;
        private NavMeshAgent _agent;
        private Vector3 _lastPos;
        private bool _deathShown;
        private float _reloadUntil;

        public void Build(HumanNpc npc, System.Random rng)
        {
            _npc = npc;
            HumanLoadout l = npc.Loadout;
            HumanoidLook look = HumanoidLook.Survivor();
            look.Shape.Build = (BodyBuild)rng.Next(4);
            look.Shape.Hair = (HairStyle)rng.Next(6);
            look.SkinColor = HumanLoadouts.SkinTones[rng.Next(HumanLoadouts.SkinTones.Length)];
            look.HairColor = HumanLoadouts.HairColors[rng.Next(HumanLoadouts.HairColors.Length)];
            Dress(look, ClothingSlot.Torso, l.Torso, 1f, rng);
            Dress(look, ClothingSlot.Legs, l.Legs, 1f, rng);
            Dress(look, ClothingSlot.Feet, l.Feet, 1f, rng);
            Dress(look, ClothingSlot.Head, l.Head, l.HeadChance, rng);
            Dress(look, ClothingSlot.Hands, l.Hands, l.HandsChance, rng);
            if (l.TintBlend > 0f)
            {
                look.TopColor = Color.Lerp(look.TopColor, l.Tint, l.TintBlend);
                look.TopTrimColor = Color.Lerp(look.TopTrimColor, l.Tint * 0.7f, l.TintBlend);
            }
            if (l.Faction == Factions.Choir)
            {
                look.Shape.Top = TopStyle.Coat;
                look.TopColor = new Color(0.22f, 0.2f, 0.24f);
                look.Shape.Headwear = HeadwearStyle.Mask;
                look.HatColor = new Color(0.8f, 0.78f, 0.72f);
            }
            look.Scale = 0.96f + (float)rng.NextDouble() * 0.08f;
            Rig = HumanoidRig.Create(transform, look);
            Animator = Rig.gameObject.AddComponent<HumanoidAnimator>();
            Animator.AutoUpdate = false;
            Animator.Gait = HumanoidGait.Survivor;
            if (npc.Gun != null) Rig.SetHeldItem(npc.Gun, HoldStyle.Firearm);
        }

        private static void Dress(HumanoidLook look, ClothingSlot slot, string[] ids, float chance, System.Random rng)
        {
            if (rng.NextDouble() >= chance) return;
            ItemDef d = HumanLoadouts.PickItem(ids, rng);
            if (d != null && d.IsClothing) look.Wear(slot, d);
        }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _lastPos = transform.position;
            Animator.Snap();
        }

        public void OnFired() { if (Animator != null) Animator.Fire(); }
        public void OnReload(float seconds) => _reloadUntil = Time.time + seconds;
        public void OnHit(Vector3 dir) { if (Animator != null) Animator.Flinch(transform.position - dir); }
        public void OnDied(Vector3 dir) { if (Animator != null && !_deathShown) { _deathShown = true; Animator.Die(dir); } }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Animator == null) return;
            Vector3 v = (transform.position - _lastPos) / dt;
            _lastPos = transform.position;
            if (v.sqrMagnitude > 400f) v = Vector3.zero;
            if (_npc == null || _npc.IsDead) { Animator.Velocity = Vector3.zero; Animator.Evaluate(dt); return; }

            Animator.Velocity = v;
            Animator.Sneaking = _npc.Crouched;
            Animator.Aiming = _npc.Aiming;
            Animator.Reloading = Time.time < _reloadUntil;
            bool hands = _npc.State == HumanNpc.Mode.Surrendered || _npc.Actor.Restrained;
            Animator.HasReachTarget = hands;
            if (hands) Animator.ReachTarget = transform.position + Vector3.up * 2.2f + transform.forward * 0.1f;
            Animator.Activity = hands ? HumanoidActivity.Use : HumanoidActivity.None;
            Actor t = _npc.Target;
            Animator.HasLookTarget = t != null;
            if (t != null) Animator.LookTarget = t.EyePosition;
            Animator.Evaluate(dt);
        }
    }

    /// <summary>
    /// Talking to the Zone's people. Anyone who isn't hostile will talk (a line, sometimes a rumour worth more than a
    /// gun); a Freehold loner can be hired; someone with their hands up can be cuffed, searched or let go.
    /// </summary>
    public sealed class HumanNpcInteract : Interactable
    {
        private HumanNpc _npc;
        private float _talkedAt = -99f;

        public override string DisplayName => _npc != null ? _npc.Name + " (" + Factions.NameOf(_npc.Actor.Faction) + ")" : "";
        public override float Reach => 2.4f;
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.2f;

        public override bool IsAvailable
        {
            get
            {
                if (_npc == null) _npc = GetComponent<HumanNpc>();
                if (_npc == null || _npc.IsDead || !isActiveAndEnabled) return false;
                Actor me = Actor.LocalPlayer;
                return me != null && (_npc.Actor.Surrendered || _npc.Actor.Restrained || !_npc.Actor.IsHostileTo(me));
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_npc == null) return;
            Actor a = _npc.Actor;
            if (a.Surrendered || a.Restrained)
            {
                string tie = ctx.Inventory == null ? null : ctx.Inventory.Has("handcuffs") ? "handcuffs" : ctx.Inventory.Has("rope") ? "rope" : null;
                if (!a.Restrained)
                    into.Add(new InteractAction(tie == "rope" ? "Tie their hands" : "Restrain", c =>
                    {
                        if (tie == null || c.Inventory.Consume(tie, 1) <= 0) return;
                        a.Restrained = true;
                        Sfx.Play(SfxIds.Handcuffs, transform.position + Vector3.up, 0.7f, 1f, 10f);
                    }).Timed(2f, tie == "rope" ? "Tying..." : "Cuffing...").DisabledIf(tie == null, "Needs handcuffs or rope"));
                into.Add(new InteractAction("Search", c =>
                {
                    MoralLedger.Record("robbed someone who gave up", MoralLedger.RobbedHelpless);
                    var box = GetComponent<LootContainer>();
                    if (box == null)
                    {
                        box = gameObject.AddComponent<LootContainer>();
                        box.DisplayName = _npc.Name + "'s gear";
                        box.Type = ContainerType.Corpse;
                        box.Contents = _npc.Pack;
                        box.Searched = true;
                    }
                    if (c.Interactor != null) c.Interactor.SearchOrOpen(box);
                }).Timed(1.5f, "Searching..."));
                into.Add(new InteractAction("Let them go", c => _npc.Release()));
                return;
            }

            into.Add(new InteractAction("Talk", Talk));
            if (CompanionService.CanHire(_npc))
            {
                int fee = CompanionService.FeeFor(_npc);
                into.Add(new InteractAction("Hire (" + Money.Format(fee) + ")", c => CompanionService.Hire(_npc, c))
                    .DisabledIf(Money.Available(ctx.Inventory) < fee, "Can't afford"));
            }
        }

        private void Talk(InteractContext ctx)
        {
            if (Time.time - _talkedAt < 1f) return;
            _talkedAt = Time.time;
            string line = ZoneTalk.LineFor(_npc);
            GameEvents.RaiseSubtitle(_npc.Name, line);
        }
    }

    /// <summary>What the Zone's people say: faction small talk, and rumours that point at real places this raid.</summary>
    public static class ZoneTalk
    {
        private static readonly Dictionary<string, string[]> s_Lines = new Dictionary<string, string[]>
        {
            [Factions.Freehold] = new[] { "Throw your bolts, brother. Every time.", "Saw the sky go red twice this week. Getting worse.",
                "Don't sell to the Fence if the Institute will take it. Heat follows you.", "Share the fire if you like. Just keep the gun down." },
            [Factions.Vanguard] = new[] { "Keep moving, civilian.", "This sector is under Vanguard watch.", "Smugglers get one warning. You've had yours." },
            [Factions.Lantern] = new[] { "Got something shiny? I know people.", "The Wardens can't see what they're not paid to see.", "Prices are good this week. For you." },
            [Factions.Wardens] = new[] { "Papers. No? Then turn around.", "Belt's closed after dark.", "Anyone past the wire without a pass gets shot. Nothing personal." },
            [Factions.Scavs] = new[] { "What are you looking at?", "Easy. Easy. We're just passing.", "You got smokes?" },
            [Factions.Institute] = new[] { "Fascinating readings today. Terrifying, but fascinating.", "If you find anything unusual, the Institute pays fair prices.",
                "Please don't touch the instruments." },
            [Factions.Choir] = new[] { "It sings to us.", "Go home. It does not want you." },
        };

        public static string LineFor(HumanNpc npc)
        {
            if (npc == null) return "...";
            // One time in three, a rumour that points at something real on this map.
            if (Random.value < 0.35f)
            {
                string r = Rumour();
                if (r != null) return r;
            }
            string[] lines = s_Lines.TryGetValue(npc.Actor.Faction, out string[] l) ? l : s_Lines[Factions.Freehold];
            return lines[Random.Range(0, lines.Length)];
        }

        private static string Rumour()
        {
            ZoneDef zone = ZoneCatalog.Current;
            if (ArtifactNode.All.Count > 0 && Random.value < 0.6f)
            {
                ArtifactNode n = ArtifactNode.All[Random.Range(0, ArtifactNode.All.Count)];
                string where = HumanUtil.DescribeLocation(n.transform.position, null);
                ProfileStore.Account?.Learn("rumour:" + n.ArtifactId + ":" + zone.Id);
                return "Word is there's a " + ArtifactCatalog.NameOf(n.ArtifactId) + " " + where + ". In the anomalies, of course.";
            }
            if (Anomaly.All.Count > 0)
            {
                Anomaly a = Anomaly.All[Random.Range(0, Anomaly.All.Count)];
                ProfileStore.Account?.Learn("anomaly_rule:" + a.Def.Id);
                return "Mind the " + a.Def.Name.ToLowerInvariant() + " " + HumanUtil.DescribeLocation(a.transform.position, null) + ". " + a.Def.Rule;
            }
            return null;
        }
    }
}
