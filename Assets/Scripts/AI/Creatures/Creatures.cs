using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The Zone's non-human life. Each mutant carries one trick — enemies as puzzles, not bullet sponges: hounds hunt
    /// in packs and come from the flanks; a Shade is all but invisible until it is on you; a Puppeteer keeps its
    /// distance, attacks the mind and turns the dead on you; a Thumper's slam goes straight through cover. Wildlife
    /// follows its instincts: deer bolt, wolves hunt in packs at night, bears defend their ground. Lures (a worn
    /// artifact, a flare, gunfire, the Sunken Star) draw mutants in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Creature : MonoBehaviour, IDamageable, IZombieAttackable
    {
        public enum Mode { Wander, Hunt, Flee, Dead }

        public static readonly List<Creature> All = new List<Creature>();

        public CreatureDef Def { get; private set; }
        public Actor Actor { get; private set; }
        public Mode State { get; private set; }
        public bool IsDead { get; private set; }
        public Transform Transform => transform;
        public float Speed01 => _agent != null && _agent.enabled ? Mathf.Clamp01(_agent.velocity.magnitude / Mathf.Max(0.1f, Def.RunSpeed)) : 0f;
        public bool Attacking => Time.time < _attackAnimUntil;
        public List<Creature> Pack;

        private NavMeshAgent _agent;
        private CreatureBody _body;
        private float _hp, _think, _attackReady, _trickReady, _lastSeen = -99f, _attackAnimUntil, _revealUntil, _wanderAt;
        private Actor _target, _lastAttacker;
        private Vector3 _home, _lastPos, _lure;
        private bool _hasLure;
        private int _packIndex;

        public static Creature Spawn(string id, Vector3 position, List<Creature> pack = null)
        {
            CreatureDef def = CreatureCatalog.Get(id);
            if (def == null) return null;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, NavMesh.AllAreas)) position = hit.position;
            var root = new GameObject(def.Name);
            root.layer = CloneZConfig.IgnoreRaycastLayer;
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            var col = root.AddComponent<CapsuleCollider>();
            bool upright = id == CreatureCatalog.Shade || id == CreatureCatalog.Puppeteer;
            col.direction = upright ? 1 : 2;
            col.center = new Vector3(0f, def.Height * 0.55f, 0f);
            col.height = upright ? def.Height : Mathf.Max(def.Radius * 2.2f, def.Height * 1.4f);
            col.radius = def.Radius;

            var actor = root.AddComponent<Actor>();
            actor.Kind = def.Kind;
            actor.Faction = Factions.Zone;
            actor.Height = def.Height;
            actor.DisplayName = def.Name;
            actor.NetId = Actor.AllocateNetId();

            var c = root.AddComponent<Creature>();
            c.Def = def;
            c.Actor = actor;
            c._hp = def.MaxHp;
            c._home = position;
            c.Pack = pack;
            if (pack != null) { c._packIndex = pack.Count; pack.Add(c); }
            c._body = root.AddComponent<CreatureBody>();
            c._body.Build(def);
            actor.Head = c._body.Head;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = def.Radius;
            agent.height = def.Height;
            agent.speed = def.WalkSpeed;
            agent.acceleration = def.Acceleration;
            agent.angularSpeed = def.TurnRate * 60f;
            agent.stoppingDistance = def.AttackRange * 0.7f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            c._agent = agent;
            root.AddComponent<ActorFog>();
            if (id == CreatureCatalog.Shade) actor.Cloaked = true;
            return c;
        }

        private void OnEnable()
        {
            All.Add(this);
            GameEvents.WorldEvent += OnWorldEvent;
        }

        private void OnDisable()
        {
            All.Remove(this);
            GameEvents.WorldEvent -= OnWorldEvent;
        }

        // Lures: a worn lure artifact, the Sunken Star, a flare, a firefight. Mutants come; animals keep away from noise.
        private void OnWorldEvent(Vector3 at, float radius, string kind)
        {
            if (IsDead || (at - transform.position).sqrMagnitude > radius * radius) return;
            if (Def.IsMutant && (kind == "lure" || kind == "flare" || kind == "prize_taken" || kind == "gunfire"))
            {
                _lure = at;
                _hasLure = true;
            }
            else if (!Def.IsMutant && (kind == "gunfire" || kind == "explosion")) Flee(at);
        }

        private void Update()
        {
            if (IsDead) return;
            if (Def.Id == CreatureCatalog.Shade) Actor.Cloaked = Time.time > _revealUntil && (_target == null || HumanUtil.FlatDistance(transform.position, _target.transform.position) > 3.5f);
            _think -= Time.deltaTime;
            if (_think > 0f) return;
            _think = Random.Range(0.18f, 0.3f);
            Perceive();
            switch (State)
            {
                case Mode.Wander: Wander(); break;
                case Mode.Hunt: Hunt(); break;
                case Mode.Flee: if (_agent.enabled && !_agent.pathPending && _agent.remainingDistance < 1.5f) State = Mode.Wander; break;
            }
        }

        private void Perceive()
        {
            float light = TimeOfDay.Instance != null ? TimeOfDay.Instance.LightLevel : 1f;
            float sight = Def.Sight * Mathf.Lerp(Def.NightSight, 1f, light);
            Actor best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a == Actor || !a.IsAlive || a.Faction == Factions.Zone || a.Faction == Factions.Choir) continue;
                if (a.Kind == ActorKind.Zombie) continue;
                float d = Vector3.Distance(a.transform.position, transform.position);
                float reach = sight * Mathf.Clamp(a.Visibility01, 0.3f, 1f);
                if (d > reach && d > Def.Awareness) continue;
                if (d > Def.Awareness && State != Mode.Hunt && Vector3.Angle(transform.forward, a.transform.position - transform.position) > Def.Fov * 0.5f) continue;
                if (!Actor.CanSee(a, Mathf.Max(reach, Def.Awareness))) continue;
                if (d < bestD) { bestD = d; best = a; }
            }

            if (best != null)
            {
                _lastSeen = Time.time;
                if (Def.Id == CreatureCatalog.Deer || (Def.Id == CreatureCatalog.Wolf && !HumanUtil.IsNight && (Pack == null || Pack.Count < 3)))
                {
                    if (bestD < Def.Sight * 0.6f) Flee(best.transform.position);
                    return;
                }
                if (Def.Id == CreatureCatalog.Bear && State != Mode.Hunt && bestD > 14f && _hp > Def.MaxHp * 0.95f) return;   // bears warn first
                if (_target != best && State != Mode.Hunt)
                {
                    if (Def.Id == CreatureCatalog.Wolf) Sfx.Play(SfxIds.WolfHowl, transform.position, 0.6f, Random.Range(0.95f, 1.1f), 60f);
                    else if (Def.Id == CreatureCatalog.Bear) Sfx.Play(SfxIds.BearRoar, transform.position, 0.9f, 1f, 50f);
                    else if (Def.IsMutant) Sfx.Play(Def.Id == CreatureCatalog.Hound ? SfxIds.MutantGrowl : SfxIds.MutantScreech, transform.position + Vector3.up, 0.8f, Random.Range(0.9f, 1.1f), 45f);
                }
                _target = best;
                State = Mode.Hunt;
                if (Pack != null) foreach (Creature m in Pack) if (m != null && !m.IsDead && m._target == null) { m._target = best; m.State = Mode.Hunt; }
                return;
            }
            if (State == Mode.Hunt && Time.time - _lastSeen > Def.ForgetTime) { _target = null; State = Mode.Wander; }
        }

        private void Wander()
        {
            if (Def.Id == CreatureCatalog.Deer && _agent.velocity.sqrMagnitude < 0.05f && Random.value < 0.3f) return;   // grazing
            if (_hasLure)
            {
                _hasLure = false;
                Go(_lure, Def.RunSpeed * 0.7f);
                return;
            }
            if (Time.time < _wanderAt) return;
            _wanderAt = Time.time + Random.Range(5f, 14f);
            Vector3 centre = Pack != null && _packIndex > 0 && Pack[0] != null && !Pack[0].IsDead ? Pack[0].transform.position : _home;
            Vector2 o = Random.insideUnitCircle * (Pack != null && _packIndex > 0 ? 4f : 18f);
            Go(centre + new Vector3(o.x, 0f, o.y), Def.WalkSpeed);
        }

        private void Hunt()
        {
            if (_target == null || !_target.IsAlive) { _target = null; State = Mode.Wander; return; }
            Vector3 me = transform.position, t = _target.transform.position;
            float d = HumanUtil.FlatDistance(me, t);

            if (_hp < Def.MaxHp * 0.3f && Def.Id != CreatureCatalog.Thumper && Def.Id != CreatureCatalog.Shade) { Flee(t); return; }
            if (Pack != null && Def.Id == CreatureCatalog.Hound && AlivePack() < 2) { Flee(t); return; }

            switch (Def.Id)
            {
                case CreatureCatalog.Puppeteer:
                    // Keeps its distance; attacks the mind and sends the dead.
                    if (d < 10f) Go(me + (me - t).normalized * 8f, Def.RunSpeed);
                    else if (d > 22f) Go(t, Def.WalkSpeed);
                    else Stop();
                    if (Time.time >= _trickReady && Actor.CanSee(_target, 30f)) PsiBlast();
                    return;
                case CreatureCatalog.Thumper:
                    if (d <= Def.AttackRange && Time.time >= _attackReady) { Slam(); return; }
                    Go(t, d > 12f ? Def.RunSpeed : Def.WalkSpeed);
                    return;
                case CreatureCatalog.Hound:
                case CreatureCatalog.Wolf:
                    // Pack hunters come from the flanks: each takes its own angle until close, then dashes in.
                    if (d > 5f && Pack != null && Pack.Count > 1)
                    {
                        float ang = (_packIndex - (Pack.Count - 1) * 0.5f) * 45f;
                        Vector3 approach = t + Quaternion.Euler(0f, ang, 0f) * (me - t).normalized * Mathf.Max(3f, d * 0.6f);
                        Go(approach, Def.RunSpeed);
                        return;
                    }
                    break;
            }
            if (d <= Def.AttackRange && Time.time >= _attackReady) { Bite(); return; }
            Go(t, Def.RunSpeed);
        }

        private int AlivePack()
        {
            int n = 0;
            foreach (Creature m in Pack) if (m != null && !m.IsDead) n++;
            return n;
        }

        private void Bite()
        {
            _attackReady = Time.time + Def.AttackCooldown;
            _attackAnimUntil = Time.time + Def.AttackWindup + 0.25f;
            _revealUntil = Time.time + 2f;
            Stop();
            Face(_target.transform.position);
            Actor victim = _target;
            StartCoroutine(Strike(victim));
        }

        private System.Collections.IEnumerator Strike(Actor victim)
        {
            yield return new WaitForSeconds(Def.AttackWindup);
            if (IsDead || victim == null || !victim.IsAlive) yield break;
            if (HumanUtil.FlatDistance(transform.position, victim.transform.position) > Def.AttackRange + 0.5f) yield break;
            Sfx.Play(Def.IsMutant ? SfxIds.MutantGrowl : SfxIds.HitFlesh, victim.Center, 0.8f, Random.Range(0.9f, 1.1f), 20f);
            var info = new DamageInfo(Def.AttackDamage, Def.AttackKind, victim.Center, (victim.transform.position - transform.position).normalized, gameObject)
            {
                HpScale = true, BleedChance = Def.Bleed, FractureChance = Def.Fracture, Knockback = Def.Knockback, WeaponId = Def.Id,
            }.WithZone(Def.Height < 1.2f ? (Random.value < 0.6f ? (Random.value < 0.5f ? BodyZone.LeftLeg : BodyZone.RightLeg) : BodyZone.Stomach) : BodyZones.RandomStrike());
            CombatResolver.Apply(victim.Damageable, info, Actor);
        }

        // The Thumper's slam: everything within reach is hit, walls or no walls.
        private void Slam()
        {
            _attackReady = Time.time + Def.AttackCooldown;
            _attackAnimUntil = Time.time + Def.AttackWindup + 0.4f;
            Stop();
            StartCoroutine(SlamRoutine());
        }

        private System.Collections.IEnumerator SlamRoutine()
        {
            yield return new WaitForSeconds(Def.AttackWindup);
            if (IsDead) yield break;
            Vector3 c = transform.position;
            Sfx.Play(SfxIds.Explosion, c, 0.8f, 0.6f, 70f);
            CreatureRingFx.Spawn(c + Vector3.up * 0.2f, new Color(0.55f, 0.48f, 0.38f, 0.45f), new Vector3(1f, 0.3f, 1f), new Vector3(Def.AttackRange * 2.2f, 0.6f, Def.AttackRange * 2.2f), 0.6f);
            NoiseSystem.Emit(c, 35f);
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || a == Actor || !a.IsAlive || a.Faction == Factions.Zone) continue;
                float d = HumanUtil.FlatDistance(a.transform.position, c);
                if (d > Def.AttackRange) continue;
                float k = 1f - d / Def.AttackRange * 0.6f;
                var info = new DamageInfo(Def.AttackDamage * k, DamageKind.Explosion, a.Center, (a.transform.position - c).normalized, gameObject)
                { HpScale = true, FractureChance = Def.Fracture, Knockback = Def.Knockback * k, WeaponId = Def.Id }.WithZone(BodyZones.RandomStrike());
                CombatResolver.Apply(a.Damageable, info, Actor);
            }
        }

        // The Puppeteer: a wave that goes for the mind, and the dead nearby turn on whoever it marks.
        private void PsiBlast()
        {
            _trickReady = Time.time + Random.Range(6f, 9f);
            _attackAnimUntil = Time.time + 0.8f;
            Vector3 c = Actor.EyePosition;
            Sfx.Play(SfxIds.PsiWhisper, c, 1f, 0.7f, 40f);
            CreatureRingFx.Spawn(c, new Color(0.6f, 0.4f, 0.9f, 0.35f), Vector3.one * 0.5f, Vector3.one * 9f, 0.7f);
            Actor victim = _target;
            var info = new DamageInfo(30f, DamageKind.Psi, victim.EyePosition, (victim.transform.position - transform.position).normalized, gameObject)
            { HpScale = true, WeaponId = Def.Id }.WithZone(BodyZone.Head);
            CombatResolver.Apply(victim.Damageable, info, Actor);
            var stats = victim.GetComponent<PlayerStats>();
            if (stats != null) stats.Panic = Mathf.Min(CloneZConfig.PanicMax, stats.Panic + 25f);
            foreach (ZombieVisibility zv in ZombieVisibility.All)
            {
                if (zv == null || (zv.transform.position - transform.position).sqrMagnitude > 25f * 25f) continue;
                var z = zv.GetComponent<ZombieAI>();
                if (z != null) z.Command(victim);
            }
        }

        private void Flee(Vector3 from)
        {
            State = Mode.Flee;
            _target = null;
            Vector3 away = (transform.position - from);
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            Go(HumanUtil.ClampToMap(transform.position + away.normalized * 30f, 6f), Def.RunSpeed);
        }

        private void Go(Vector3 p, float speed)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
            _agent.speed = speed;
            _agent.isStopped = false;
            if ((_agent.destination - p).sqrMagnitude > 0.5f) _agent.SetDestination(p);
        }

        private void Stop()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        private void Face(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        #region Damage
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            float hp = info.AmountHp;
            if (!info.HpScale) hp *= CloneZConfig.NonPlayerReceivedDamageMultiplier;
            if (info.Zone == BodyZone.Head && info.ZoneKnown) hp *= 1.8f;
            if (info.Kind == DamageKind.Psi || info.Kind == DamageKind.Radiation) hp *= Def.IsMutant ? 0.1f : 0.6f;
            _hp -= hp;
            _revealUntil = Time.time + 2.5f;
            if (Def.StaggerThreshold > 0f && hp >= Def.StaggerThreshold) _attackReady = Mathf.Max(_attackReady, Time.time + 0.6f);
            Actor attacker = info.Source != null ? info.Source.GetComponentInParent<Actor>() : null;
            if (attacker != null) _lastAttacker = attacker;
            if (attacker != null && attacker != Actor && attacker.Faction != Factions.Zone)
            {
                if (Def.Id == CreatureCatalog.Deer) Flee(attacker.transform.position);
                else { _target = attacker; State = Mode.Hunt; _lastSeen = Time.time; }
            }
            if (_hp <= 0f) Die(info.HitDirection);
        }

        public void ReceiveZombieAttack(bool bite) => TakeDamage(new DamageInfo(30f, DamageKind.Bite, Actor.Center, transform.forward, null) { HpScale = true });

        private void Die(Vector3 dir)
        {
            IsDead = true;
            State = Mode.Dead;
            Actor.Cloaked = false;
            if (_agent != null) _agent.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            _body.Die(dir);
            Sfx.Play(Def.IsMutant ? SfxIds.MutantScreech : SfxIds.HitFlesh, transform.position + Vector3.up, 0.6f, 0.7f, 30f);

            var loot = gameObject.AddComponent<LootContainer>();
            loot.DisplayName = Def.CorpseName;
            loot.Type = ContainerType.Corpse;
            loot.Contents = new Inventory(999f);
            foreach (CreatureLoot l in Def.Loot)
            {
                if (Random.value > l.Chance) continue;
                ItemDef d = ItemDatabase.Get(l.Id);
                if (d != null) loot.Contents.Add(d, Random.Range(l.Min, l.Max + 1), false, extra: new ItemExtra { FoundInRaid = true });
            }
            loot.SearchTime = 2.5f;
            loot.Lifetime = Def.CorpseLifetime;
            if (Def.Xp > 0f && _lastAttacker != null && _lastAttacker == Actor.LocalPlayer)
            {
                var skills = Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerSkills>() : null;
                if (skills != null) skills.AddXp(PlayerSkills.Skill.Aiming, Def.Xp * 0.2f);
            }
        }
        #endregion
    }

    /// <summary>
    /// A creature's body, put together from simple solid parts on joints: four legs that swing with the gait for the
    /// beasts, a hunched frame for the Puppeteer, a tall smeared outline for the Shade (nearly see-through while it
    /// hunts), a slab of a Thumper. Death drops it on its side.
    /// </summary>
    public sealed class CreatureBody : MonoBehaviour
    {
        public Transform Head { get; private set; }
        private Transform _root, _torso;
        private readonly List<Transform> _legs = new List<Transform>();
        private readonly List<float> _legPhase = new List<float>();
        private Creature _c;
        private CreatureDef _def;
        private Material _cloak;
        private Renderer[] _renderers;
        private Material[][] _solid;
        private float _cycle, _deadT = -1f;
        private Vector3 _deathAxis;
        private bool _cloaked;

        public void Build(CreatureDef def)
        {
            _def = def;
            _root = CreatureParts.Pivot("Body", transform, Vector3.zero);
            switch (def.Id)
            {
                case CreatureCatalog.Shade: Biped(def, new Color(0.2f, 0.22f, 0.27f), 0.35f, 2f, true); break;
                case CreatureCatalog.Puppeteer: Biped(def, new Color(0.72f, 0.62f, 0.56f), 0.5f, 1.75f, false); break;
                case CreatureCatalog.Thumper: Quad(def, new Color(0.38f, 0.3f, 0.28f), 2.2f, 1.6f, 1.4f, 1.2f, 0.9f); break;
                case CreatureCatalog.Bear: Quad(def, new Color(0.3f, 0.2f, 0.13f), 1.9f, 1.0f, 0.75f, 0.65f, 0.45f); break;
                case CreatureCatalog.Deer: Quad(def, new Color(0.55f, 0.4f, 0.26f), 1.3f, 0.55f, 0.85f, 0.35f, 0.3f); break;
                case CreatureCatalog.Wolf: Quad(def, new Color(0.42f, 0.42f, 0.4f), 1.2f, 0.42f, 0.55f, 0.32f, 0.28f); break;
                default: Quad(def, new Color(0.46f, 0.38f, 0.34f), 1.1f, 0.42f, 0.5f, 0.34f, 0.3f); break;   // hound: hairless, mottled
            }
            _renderers = GetComponentsInChildren<Renderer>();
            _solid = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++) _solid[i] = _renderers[i].sharedMaterials;
        }

        private void Quad(CreatureDef def, Color c, float length, float bodyH, float legLen, float bodyW, float headSize)
        {
            float hipY = legLen;
            _torso = CreatureParts.Pivot("Torso", _root, new Vector3(0f, hipY + bodyH * 0.45f, 0f));
            string k = def.Id;
            CreatureParts.Attach(_torso, k + "_torso", b => b
                .Add(CreaturePrim.Sphere, Vector3.zero, Vector3.zero, new Vector3(bodyW * 1.6f, bodyH, length), c)
                .Add(CreaturePrim.Sphere, new Vector3(0f, bodyH * 0.1f, length * 0.3f), Vector3.zero, new Vector3(bodyW * 1.8f, bodyH * 1.1f, length * 0.5f), c * 0.92f));
            Head = CreatureParts.Pivot("Head", _torso, new Vector3(0f, bodyH * 0.35f, length * 0.55f));
            CreatureParts.Attach(Head, k + "_head", b => b
                .Add(CreaturePrim.Sphere, Vector3.zero, Vector3.zero, Vector3.one * headSize, c * 0.95f)
                .Add(CreaturePrim.Taper, new Vector3(0f, -headSize * 0.1f, headSize * 0.55f), new Vector3(90f, 0f, 0f), new Vector3(headSize * 0.5f, headSize * 0.7f, headSize * 0.45f), c * 0.85f)
                .Add(CreaturePrim.Sphere, new Vector3(headSize * 0.25f, headSize * 0.15f, headSize * 0.4f), Vector3.zero, Vector3.one * headSize * 0.12f,
                    def.IsMutant ? new Color(0.95f, 0.85f, 0.4f) : new Color(0.05f, 0.05f, 0.05f), 0.8f)
                .Add(CreaturePrim.Sphere, new Vector3(-headSize * 0.25f, headSize * 0.15f, headSize * 0.4f), Vector3.zero, Vector3.one * headSize * 0.12f,
                    def.IsMutant ? new Color(0.95f, 0.85f, 0.4f) : new Color(0.05f, 0.05f, 0.05f), 0.8f));
            if (def.Id == CreatureCatalog.Deer)
                CreatureParts.Attach(Head, "deer_antlers", b => b
                    .Add(CreaturePrim.Cylinder, new Vector3(0.1f, 0.3f, -0.05f), new Vector3(0f, 0f, -20f), new Vector3(0.04f, 0.45f, 0.04f), new Color(0.75f, 0.68f, 0.55f))
                    .Add(CreaturePrim.Cylinder, new Vector3(-0.1f, 0.3f, -0.05f), new Vector3(0f, 0f, 20f), new Vector3(0.04f, 0.45f, 0.04f), new Color(0.75f, 0.68f, 0.55f)));
            float[] xs = { -bodyW * 0.55f, bodyW * 0.55f, -bodyW * 0.55f, bodyW * 0.55f };
            float[] zs = { length * 0.35f, length * 0.35f, -length * 0.35f, -length * 0.35f };
            float thick = Mathf.Max(0.07f, bodyW * 0.28f);
            for (int i = 0; i < 4; i++)
            {
                Transform hip = CreatureParts.Pivot("Leg" + i, _root, new Vector3(xs[i], hipY, zs[i]));
                CreatureParts.Attach(hip, k + "_leg", b => b
                    .Add(CreaturePrim.Taper, new Vector3(0f, -legLen * 0.5f, 0f), new Vector3(180f, 0f, 0f), new Vector3(thick, legLen, thick), c * 0.88f));
                _legs.Add(hip);
                _legPhase.Add(i == 0 || i == 3 ? 0f : Mathf.PI);
            }
            CreatureParts.Attach(_torso, k + "_tail", b => b.Add(CreaturePrim.Taper, new Vector3(0f, bodyH * 0.1f, -length * 0.62f), new Vector3(-110f, 0f, 0f),
                new Vector3(thick * 0.6f, length * 0.35f, thick * 0.6f), c * 0.8f));
        }

        private void Biped(CreatureDef def, Color c, float width, float height, bool shade)
        {
            float legLen = height * 0.45f;
            string k = def.Id;
            _torso = CreatureParts.Pivot("Torso", _root, new Vector3(0f, legLen, 0f), new Vector3(shade ? 8f : 25f, 0f, 0f));
            CreatureParts.Attach(_torso, k + "_torso", b => b
                .Add(CreaturePrim.Taper, new Vector3(0f, height * 0.22f, 0f), Vector3.zero, new Vector3(width, height * 0.45f, width * 0.6f), c));
            Head = CreatureParts.Pivot("Head", _torso, new Vector3(0f, height * 0.5f, 0.05f));
            float hs = shade ? 0.24f : 0.48f;
            CreatureParts.Attach(Head, k + "_head", b => b
                .Add(CreaturePrim.Sphere, Vector3.zero, Vector3.zero, new Vector3(hs, hs * (shade ? 1.4f : 1.1f), hs), c * 1.05f)
                .Add(CreaturePrim.Sphere, new Vector3(hs * 0.2f, 0f, hs * 0.42f), Vector3.zero, Vector3.one * hs * 0.14f, shade ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.1f, 0.1f), 0.9f)
                .Add(CreaturePrim.Sphere, new Vector3(-hs * 0.2f, 0f, hs * 0.42f), Vector3.zero, Vector3.one * hs * 0.14f, shade ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.1f, 0.1f), 0.9f));
            for (int side = -1; side <= 1; side += 2)
            {
                Transform arm = CreatureParts.Pivot("Arm", _torso, new Vector3(side * width * 0.6f, height * 0.42f, 0f));
                float armLen = shade ? height * 0.55f : height * 0.35f;
                CreatureParts.Attach(arm, k + "_arm", b => b.Add(CreaturePrim.Taper, new Vector3(0f, -armLen * 0.5f, 0f), new Vector3(180f, 0f, 0f),
                    new Vector3(width * 0.22f, armLen, width * 0.22f), c * 0.9f));
                _legs.Add(arm);
                _legPhase.Add(side < 0 ? Mathf.PI : 0f);
                Transform leg = CreatureParts.Pivot("Leg", _root, new Vector3(side * width * 0.25f, legLen, 0f));
                CreatureParts.Attach(leg, k + "_leg", b => b.Add(CreaturePrim.Taper, new Vector3(0f, -legLen * 0.5f, 0f), new Vector3(180f, 0f, 0f),
                    new Vector3(width * 0.3f, legLen, width * 0.3f), c * 0.85f));
                _legs.Add(leg);
                _legPhase.Add(side < 0 ? 0f : Mathf.PI);
            }
        }

        private void Start() => _c = GetComponent<Creature>();

        public void Die(Vector3 dir)
        {
            _deadT = 0f;
            _deathAxis = Vector3.Cross(Vector3.up, dir.sqrMagnitude > 0.01f ? dir.normalized : transform.right);
            SetCloak(false);
        }

        private void LateUpdate()
        {
            if (_deadT >= 0f)
            {
                if (_deadT < 1f)
                {
                    _deadT += Time.deltaTime * 2.2f;
                    _root.localRotation = Quaternion.AngleAxis(Mathf.Min(1f, _deadT) * 88f, transform.InverseTransformDirection(_deathAxis));
                }
                return;
            }
            if (_c == null) return;
            float speed = _c.Speed01;
            _cycle += Time.deltaTime * Mathf.Lerp(2f, 11f, speed) * (speed > 0.02f ? 1f : 0f);
            float swing = Mathf.Lerp(0f, 38f, Mathf.Clamp01(speed * 2f));
            for (int i = 0; i < _legs.Count; i++)
                _legs[i].localRotation = Quaternion.Euler(Mathf.Sin(_cycle + _legPhase[i]) * swing, 0f, 0f);
            if (_torso != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(_cycle)) * 0.04f * speed;
                float lunge = _c.Attacking ? 18f : 0f;
                _torso.localPosition = new Vector3(_torso.localPosition.x, _torso.localPosition.y, 0f) + Vector3.up * (bob - (_c.Attacking ? 0.05f : 0f)) * 0f;
                _root.localRotation = Quaternion.Euler(lunge * 0.4f, 0f, 0f);
                _root.localPosition = Vector3.up * bob;
            }
            SetCloak(_c.Actor.Cloaked);
        }

        // The Shade: a smear in the air while it hunts; solid when it strikes or is hurt.
        private void SetCloak(bool on)
        {
            if (on == _cloaked || _renderers == null) return;
            _cloaked = on;
            if (on && _cloak == null) _cloak = CreatureMaterials.TransparentLit(new Color(0.5f, 0.55f, 0.6f, 0.07f), 0.9f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                if (on)
                {
                    var mats = new Material[_solid[i].Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = _cloak;
                    _renderers[i].sharedMaterials = mats;
                    _renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    _renderers[i].sharedMaterials = _solid[i];
                    _renderers[i].shadowCastingMode = ShadowCastingMode.On;
                }
            }
        }

        private void OnDestroy()
        {
            if (_cloak != null) Destroy(_cloak);
        }
    }

    /// <summary>
    /// Who the Zone sends, and when (tension 6: the dead are the main pressure, mutants and people the second): by
    /// day the people move and the beasts keep to their lairs; at night the mutants come out and the wolves hunt.
    /// Populations scale with the zone (deeper is worse) and the sandbox, spawn out of sight, and come in packs.
    /// </summary>
    public sealed class ThreatDirector : MonoBehaviour
    {
        private ZoneDef _zone;
        private System.Random _rng;
        private float _tick = 5f;

        public static void Begin(ZoneDef zone, int seed)
        {
            var old = FindAnyObjectByType<ThreatDirector>();
            if (old != null) Destroy(old.gameObject);
            if (zone == null) return;
            var t = new GameObject("ThreatDirector").AddComponent<ThreatDirector>();
            t._zone = zone;
            t._rng = new System.Random(seed ^ 0x7487);
            t.SeedLairs();
        }

        // A few residents from the start: wildlife by day, a pack in a lair.
        private void SeedLairs()
        {
            foreach (string w in _zone.Wildlife)
                if (_rng.NextDouble() < 0.7) SpawnGroup(w, AwayPoint(40f));
            IReadOnlyList<Vector3> lairs = WorldMarkers.Get(WorldMarkers.MutantLair);
            for (int i = 0; i < lairs.Count && _zone.Mutants.Length > 0; i++)
                SpawnGroup(_zone.Mutants[_rng.Next(_zone.Mutants.Length)], lairs[i]);
        }

        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 20f;
            bool night = HumanUtil.IsNight;
            int mutants = 0, animals = 0;
            foreach (Creature c in Creature.All) if (c != null && !c.IsDead) { if (c.Def.IsMutant) mutants++; else animals++; }
            int wantMutants = Mathf.RoundToInt((night ? 6f : 1.5f) * _zone.MutantShare * 2f * (0.7f + _zone.Depth * 0.3f));
            int wantAnimals = night ? 3 : 4;
            if (mutants < wantMutants && _zone.Mutants.Length > 0)
            {
                string id = _zone.Mutants[_rng.Next(_zone.Mutants.Length)];
                if (id == CreatureCatalog.Thumper && _rng.NextDouble() < 0.6) id = _zone.Mutants[0];
                SpawnGroup(id, AwayPoint(55f));
            }
            if (animals < wantAnimals && _zone.Wildlife.Length > 0)
            {
                string id = _zone.Wildlife[_rng.Next(_zone.Wildlife.Length)];
                if (id == CreatureCatalog.Wolf && !night && _rng.NextDouble() < 0.6) id = CreatureCatalog.Deer;
                if (System.Array.IndexOf(_zone.Wildlife, id) >= 0 || id == CreatureCatalog.Deer) SpawnGroup(id, AwayPoint(55f));
            }
        }

        private Vector3 AwayPoint(float minDist)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 p = HumanUtil.RandomMapPoint(_rng, 10f);
                if (HumanUtil.NearestPlayerDistance(p) > minDist) return p;
            }
            return HumanUtil.EdgePoint(_rng, 8f);
        }

        private void SpawnGroup(string id, Vector3 at)
        {
            CreatureDef def = CreatureCatalog.Get(id);
            if (def == null) return;
            int n = def.PackMin + _rng.Next(def.PackMax - def.PackMin + 1);
            List<Creature> pack = n > 1 ? new List<Creature>() : null;
            for (int i = 0; i < n; i++)
                Creature.Spawn(id, at + new Vector3((float)_rng.NextDouble() * 4f - 2f, 0f, (float)_rng.NextDouble() * 4f - 2f), pack);
        }
    }
}
