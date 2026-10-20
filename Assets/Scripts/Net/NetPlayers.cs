using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The other runners. Each client sends its own body's state; the server forwards it only to those who could
    /// see it — line of sight from the viewer's head, or close, or a squadmate (the doc's visibility on the server:
    /// no wallhacks, no shoulder-peeking round corners, and the same rule draws the isometric fog of war). Shots
    /// are resolved on the server with remote bodies rewound to what the shooter saw; damage goes to whoever owns
    /// the body. A runner who dies leaves a body with everything they carried for anyone to loot.
    /// </summary>
    public static class NetPlayers
        {
            private static Camera s_MainCamera;
            
        public const double InterpolationDelay = 0.1;
        private static readonly Dictionary<ulong, RemotePlayer> s_Proxies = new Dictionary<ulong, RemotePlayer>();
        private static float s_SendAt;
        private static uint s_Seq;
        private static string s_LastHeld = "";

        /// <summary>Read‑only map of remote player IDs to their proxy objects.</summary>
        public static IReadOnlyDictionary<ulong, RemotePlayer> Proxies => s_Proxies;

        internal static void Register()
        {
            NetSession.Register("cz.ps", OnState);
            NetSession.Register("cz.psx", OnForwarded);
            NetSession.Register("cz.shot", OnShot);
            NetSession.Register("cz.shotfx", OnShotFx);
            NetSession.Register("cz.dmg", OnDamage);
            NetSession.Register("cz.zatk", (s, m) => { bool bite = m.B(); var me = LocalStats; if (me == null) return; NetSocial.NoteAttacker(ulong.MaxValue, 0, "the dead"); me.ReceiveZombieAttack(bite); if (me.IsDead) NetSocial.ReportDeath(ulong.MaxValue, 0, "the dead"); });
            NetSession.Register("cz.hit", OnHitRequest);
            NetSession.Register("cz.died", OnDied);
            NetSession.Register("cz.corpse", OnCorpse);
            CombatResolver.ShotRouter = Route;
            CombatResolver.Rewind = Rewind;
            NetTicker.Ensure();
        }

        private static PlayerStats LocalStats => Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerStats>() : null;

        /// <summary>Remove a remote player proxy and destroy its GameObject.</summary>
        public static void RemoveProxy(ulong id)
        {
            if (s_Proxies.TryGetValue(id, out RemotePlayer p) && p != null) Object.Destroy(p.gameObject);
            s_Proxies.Remove(id);
        }

        /// <summary>Get the proxy RemotePlayer for a given player ID, or null if none.</summary>
        public static RemotePlayer ProxyOf(ulong id) => s_Proxies.TryGetValue(id, out RemotePlayer p) ? p : null;

        #region Sending our own state
        internal static void Tick()
        {
            if (!NetSession.Active) return;
            Actor me = Actor.LocalPlayer;
            if (me == null || Time.time < s_SendAt) return;
            s_SendAt = Time.time + 1f / 15f;
            var stats = me.GetComponent<PlayerStats>();
            var ctrl = me.GetComponent<PlayerController>();
            var combat = me.GetComponent<PlayerCombat>();
            var inv = me.GetComponent<PlayerInventory>();
            byte flags = 0;
            if (ctrl != null && ctrl.IsSneaking) flags |= 1;
            if (combat != null && combat.AimingDownSights) flags |= 2;
            if (stats != null && stats.IsDead) flags |= 4;
            if (stats != null && stats.Incapacitated) flags |= 8;
            if (me.Surrendered) flags |= 16;
            if (me.Restrained) flags |= 32;
            if (GameSession.IsMonsterRun) flags |= 64;
            if (me.Cloaked) flags |= 128;
            PlayerMonster hunter = PlayerMonster.Local;
            if (hunter != null && hunter.Attacking) flags |= 2;   // a hunter never aims: the bit means "striking"
            string held = hunter != null && hunter.Def != null ? "form:" + hunter.Def.Id : inv != null && inv.EquippedWeapon != null ? inv.EquippedWeapon.Id : "";
            if (held != s_LastHeld) s_LastHeld = held;
            if (s_MainCamera == null || !s_MainCamera) s_MainCamera = Camera.main;
            Camera cam = s_MainCamera;
            float pitch = cam != null && ViewState.MouseLook ? -cam.transform.eulerAngles.x : 0f;
            using (var o = new NetOut(160))
            {
                o.U(++s_Seq).V(me.transform.position).F(me.transform.eulerAngles.y).F(pitch).By(flags).S(held)
                    .By((byte)(stats != null ? Mathf.Clamp(stats.Health, 0f, 100f) : 100f)).B(GameSession.IsExtraction);
                NetSession.ToServer("cz.ps", o, NetworkDelivery.UnreliableSequenced);
            }
        }
        #endregion

        #region Receiving state (server forwards, everyone renders)
        private static void OnState(ulong sender, NetIn m)
        {
            NetworkManager nm = NetSession.NM;
            uint seq = m.U();
            Vector3 pos = m.V();
            float yaw = m.F(), pitch = m.F();
            byte flags = m.By();
            string held = m.S();
            byte hp = m.By();
            bool inRaid = m.B();

            if (nm.IsServer)
            {
                // A message from a client (or the host's own body): validate, record, forward to whoever may see it.
                ulong owner = sender;
                if (NetSession.Clients.TryGetValue(owner, out NetClientInfo ci))
                {
                    if (ci.Position != Vector3.zero && (pos - ci.Position).magnitude > 12f && owner != nm.LocalClientId)
                        Debug.LogWarning("[CloneZ] " + ci.Name + " moved implausibly fast (" + (pos - ci.Position).magnitude.ToString("0") + " m).");
                    ci.Position = pos;
                    ci.Eye = pos + Vector3.up * 1.6f;
                    ci.Alive = (flags & 4) == 0;
                    ci.InRaid = inRaid;
                }
                if (owner != nm.LocalClientId) Apply(owner, seq, pos, yaw, pitch, flags, held, hp, inRaid);
                foreach (ulong id in nm.ConnectedClientsIds)
                {
                    if (id == owner || id == nm.LocalClientId) continue;
                    if (!Visible(owner, id, pos)) continue;
                    using (var o = new NetOut(170)) {

                        o.UL(owner).U(seq).V(pos).F(yaw).F(pitch).By(flags).S(held).By(hp).B(inRaid);
                        NetSession.SendTo(id, "cz.psx", o, NetworkDelivery.UnreliableSequenced);
                    }
                }
            }
        }

        internal static void OnForwarded(ulong sender, NetIn m)
        {
            ulong owner = m.UL();
            Apply(owner, m.U(), m.V(), m.F(), m.F(), m.By(), m.S(), m.By(), m.B());
        }

        // Server-side culling: a viewer only receives a body it could see — or one right beside it, or a squadmate.
        private static bool Visible(ulong owner, ulong viewer, Vector3 pos)
        {
            if (!NetSession.Clients.TryGetValue(viewer, out NetClientInfo v) || !NetSession.Clients.TryGetValue(owner, out NetClientInfo o)) return true;
            if (!string.IsNullOrEmpty(o.Squad) && o.Squad == v.Squad) return true;
            if (v.InRaid != o.InRaid) return false;
            float d = Vector3.Distance(v.Position, pos);
            if (d < 6f) return true;
            if (d > 160f) return false;
            Vector3 eye = v.Eye, chest = pos + Vector3.up * 1.2f, head = pos + Vector3.up * 1.65f;
            return !Physics.Linecast(eye, chest, Actor.SightMask, QueryTriggerInteraction.Ignore)
                || !Physics.Linecast(eye, head, Actor.SightMask, QueryTriggerInteraction.Ignore);
        }

        private static void Apply(ulong owner, uint seq, Vector3 pos, float yaw, float pitch, byte flags, string held, byte hp, bool inRaid)
        {
            NetworkManager nm = NetSession.NM;
            if (owner == nm.LocalClientId) return;
            if (inRaid != GameSession.IsExtraction) { RemoveProxy(owner); return; }
            if (!s_Proxies.TryGetValue(owner, out RemotePlayer p) || p == null)
            {
                NetSession.Clients.TryGetValue(owner, out NetClientInfo ci);
                p = RemotePlayer.Spawn(owner, ci, pos, yaw, (flags & 64) != 0);
                s_Proxies[owner] = p;
            }
            p.Push(seq, nm.IsServer ? nm.ServerTime.Time : nm.ServerTime.Time, pos, yaw, pitch, flags, held, hp);
        }
        #endregion

        #region Shots, hits and damage
        // On a client every shot goes to the server; the local result only draws the tracer.
        private static bool Route(ShotRequest req)
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null || nm.IsServer) return false;
            using (var o = new NetOut(160))
            {
                o.V(req.Origin).V(req.Direction).F(req.Range).S(req.AmmoId).S(req.WeaponId).F(req.DamageMult).D(req.ViewTime);
                NetSession.ToServer("cz.shot", o, NetworkDelivery.ReliableSequenced);
            }
            return true;
        }

        private static void OnShot(ulong sender, NetIn m)
        {
            NetworkManager nm = NetSession.NM;
            if (!nm.IsServer) return;
            var req = new ShotRequest
            {
                Origin = m.V(), Direction = m.V().normalized, Range = m.F(), AmmoId = m.S(), WeaponId = m.S(), DamageMult = m.F(), ViewTime = m.D(),
                ShooterClient = sender,
            };
            // Basic sanity: the round leaves near the shooter's body, at a believable rate, within the gun's reach.
            if (NetSession.Clients.TryGetValue(sender, out NetClientInfo ci))
            {
                if ((req.Origin - ci.Position).magnitude > 4f) return;
                if (Time.time - ci.LastShot < 0.045f) return;
                ci.LastShot = Time.time;
            }
            req.Range = Mathf.Min(req.Range, 400f);
            req.DamageMult = Mathf.Clamp(req.DamageMult, 0f, 2f);
            RemotePlayer shooter = ProxyOf(sender);
            if (shooter != null) { req.Shooter = shooter.gameObject; req.ShooterActor = shooter.Actor; }
            ShotResult r = CombatResolver.Resolve(req);
            using (var o = new NetOut(64))
            {
                o.UL(sender).V(req.Origin).V(r.End).B(r.Hit && r.Target != null).S(req.WeaponId);
                NetSession.ToClients("cz.shotfx", o, NetworkDelivery.UnreliableSequenced, sender, includeHost: true);
            }
        }

        // Everyone else sees and hears the shot.
        private static void OnShotFx(ulong sender, NetIn m)
        {
            ulong shooter = m.UL();
            if (NetSession.NM != null && shooter == NetSession.NM.LocalClientId) return;
            Vector3 from = m.V(), to = m.V();
            bool flesh = m.B();
            string weapon = m.S();
            Sfx.Play(HumanNpc.GunSound(ItemDatabase.Get(weapon)), from, 1f, Random.Range(0.96f, 1.04f), 140f);
            if (flesh) Sfx.Play(SfxIds.HitFlesh, to, 0.5f, 1f, 18f);
            NetTracer.Draw(from, to);
            RemotePlayer p = ProxyOf(shooter);
            if (p != null) p.Fired();
        }

        /// <summary>Server: damage that landed on a remote runner's body goes to the runner who owns it.</summary>
        internal static void SendDamage(ulong owner, in DamageInfo info, Actor attacker)
        {
            using (var o = new NetOut(96))
            {
                o.F(info.Amount).By((byte)info.Kind).V(info.HitPoint).V(info.HitDirection).By((byte)info.Zone).B(info.ZoneKnown)
                    .F(info.Penetration).F(info.ArmorDamage).F(info.BleedChance).F(info.FractureChance).S(info.WeaponId).B(info.HpScale)
                    .UL(attacker != null && attacker.OwnerClientId != ulong.MaxValue ? attacker.OwnerClientId : ulong.MaxValue)
                    .U(attacker != null ? attacker.NetId : 0u);
                NetSession.SendTo(owner, "cz.dmg", o);
            }
        }

        private static void OnDamage(ulong sender, NetIn m)
        {
            PlayerStats me = LocalStats;
            if (me == null) return;
            var info = new DamageInfo(m.F(), (DamageKind)m.By(), m.V(), m.V(), null)
            {
                Zone = (BodyZone)m.By(), ZoneKnown = m.B(), Penetration = m.F(), ArmorDamage = m.F(), BleedChance = m.F(), FractureChance = m.F(),
                WeaponId = m.S(), HpScale = m.B(), AttackerClientId = m.UL(),
            };
            uint attackerNet = m.U();
            RemotePlayer killer = info.AttackerClientId != ulong.MaxValue ? ProxyOf(info.AttackerClientId) : null;
            if (killer != null) info.Source = killer.gameObject;
            NetSocial.NoteAttacker(info.AttackerClientId, attackerNet, info.WeaponId);
            me.TakeDamage(info);
            if (me.IsDead) NetSocial.ReportDeath(info.AttackerClientId, attackerNet, info.WeaponId);
        }

        /// <summary>A client's melee (or anything else applied locally) on something the server owns: ask the server.</summary>
        public static void RequestHit(uint targetNetId, in DamageInfo info)
        {
            using (var o = new NetOut(96))
            {
                o.U(targetNetId).F(info.Amount).By((byte)info.Kind).V(info.HitPoint).V(info.HitDirection).By((byte)info.Zone).B(info.ZoneKnown).B(info.HpScale).S(info.WeaponId);
                NetSession.ToServer("cz.hit", o);
            }
        }

        private static void OnHitRequest(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            uint target = m.U();
            var info = new DamageInfo(m.F(), (DamageKind)m.By(), m.V(), m.V(), null) { Zone = (BodyZone)m.By(), ZoneKnown = m.B(), HpScale = m.B(), WeaponId = m.S(), AttackerClientId = sender };
            info.Amount = Mathf.Min(info.Amount, info.HpScale ? 400f : 15f);
            Actor a = Actor.FindByNetId(target);
            RemotePlayer from = ProxyOf(sender);
            if (from != null) info.Source = from.gameObject;
            float reach = info.Kind == DamageKind.Psi ? 14f : 4f;   // a Puppeteer's pulse reaches further than claws
            if (a != null && a.Damageable != null && (from == null || (a.transform.position - from.transform.position).magnitude < reach))
                CombatResolver.Apply(a.Damageable, info, from != null ? from.Actor : null);
        }

        // Lag compensation: put every remote body where the shooter saw it, cast, put them back.
        private static void Rewind(double time, bool rewind)
        {
            foreach (RemotePlayer p in s_Proxies.Values) if (p != null) p.Rewind(time, rewind);
        }
        #endregion

        #region Deaths and bodies
        private static void OnDied(ulong sender, NetIn m)
        {
            ulong victim = m.UL(), killer = m.UL();
            string weapon = m.S();
            if (NetSession.NM.IsServer)
            {
                using (var o = new NetOut(64)) { o.UL(victim).UL(killer).S(weapon); NetSession.ToClients("cz.died", o, NetworkDelivery.ReliableSequenced, victim); }
            }
            RemotePlayer v = ProxyOf(victim);
            if (v != null) v.Die();
            string vn = NetSession.Clients.TryGetValue(victim, out NetClientInfo vi) ? vi.Name : "A runner";
            string kn = killer != ulong.MaxValue && NetSession.Clients.TryGetValue(killer, out NetClientInfo ki) ? ki.Name : null;
            GameEvents.RaiseToast(kn != null ? kn + " killed " + vn : vn + " died");
            if (NetSession.NM != null && killer == NetSession.NM.LocalClientId && v != null)
                GameEvents.RaiseActorKilled(new KillInfo { Victim = v.Actor, Killer = Actor.LocalPlayer, WeaponId = weapon, Position = v.transform.position, Surrendered = v.Actor.Surrendered });
        }

        /// <summary>Our body's contents become a lootable body for everyone (PvP looting).</summary>
        public static void PublishCorpse(Vector3 at, List<StackSave> items)
        {
            if (!NetSession.Active || items == null) return;
            using (var o = new NetOut(1024, 1 << 16))
            {
                o.UL(NetSession.NM.LocalClientId).V(at);
                NetCodec.Write(o, items);
                NetSession.ToServer("cz.corpse", o, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private static void OnCorpse(ulong sender, NetIn m)
        {
            ulong owner = m.UL();
            Vector3 at = m.V();
            List<StackSave> items = NetCodec.ReadSaves(m);
            if (NetSession.NM.IsServer)
            {
                using (var o = new NetOut(1024, 1 << 16))
                {
                    o.UL(owner).V(at);
                    NetCodec.Write(o, items);
                    NetSession.ToClients("cz.corpse", o, NetworkDelivery.ReliableFragmentedSequenced, owner);
                }
            }
            if (NetSession.NM.LocalClientId == owner) return;
            string name = NetSession.Clients.TryGetValue(owner, out NetClientInfo ci) ? ci.Name : "a runner";
            LootContainer body = LootContainer.SpawnSaved(null, 6, at, 0f, false, 900f, items);
            if (body != null) body.DisplayName = "Body of " + name;
        }
        #endregion
    }

    /// <summary>Drives the per-frame net work (sending our state, interpolation happens on each proxy).</summary>
    public sealed class NetTicker : MonoBehaviour
    {
        private static NetTicker s_Instance;

        /// <summary>Create the singleton NetTicker GameObject if not already present.</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[NetTicker]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<NetTicker>();
        }

        private void Update()
        {
            if (!NetSession.Active) return;
            NetPlayers.Tick();
            NetWorld.Tick();
            NetSocial.Tick();
        }
    }

    /// <summary>A brief tracer line for a shot seen over the network.</summary>
    public static class NetTracer
    {
        /// <summary>Draw a brief line segment representing a shot across the network.</summary>
        public static void Draw(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.widthMultiplier = 0.015f;
            lr.material = ZoneKit.BeamMaterial();
            lr.startColor = new Color(1f, 0.9f, 0.6f, 0.7f);
            lr.endColor = new Color(1f, 0.9f, 0.6f, 0f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(go, 0.06f);
        }
    }

    /// <summary>
    /// Another runner's body on this machine: a humanoid dressed in their faction's colours (or the thing they're
    /// playing in a hunter run), interpolated 100 ms behind, with a hit capsule the server rewinds for lag
    /// compensation. On the server, damage it takes goes to its owner; the dead's teeth go there too.
    /// </summary>
    public sealed class RemotePlayer : MonoBehaviour, IDamageable, IZombieAttackable
    {
        private struct Snap { public double T; public Vector3 P; public float Yaw, Pitch; }

        /// <summary>Unique identifier of the remote player this proxy represents.</summary>
        public ulong Owner { get; private set; }
        /// <summary>The Actor component attached to the remote player's body.</summary>
        public Actor Actor { get; private set; }
        /// <summary>Whether the remote player is dead.</summary>
        public bool IsDead { get; private set; }
        /// <summary>Convenient accessor for this object's Transform.</summary>
        public Transform Transform => transform;
        /// <summary>Name of the item currently held by the remote player, or empty string.</summary>
        public string Held { get; private set; } = "";
        /// <summary>Out cold (knocked out by shock or blood loss).</summary>
        public bool Incapacitated => (_flags & 8) != 0;

        private readonly List<Snap> _buffer = new List<Snap>(32);
        private readonly List<Snap> _history = new List<Snap>(64);
        private HumanoidRig _rig;
        private HumanoidAnimator _anim;
        private CreatureBody _beast;
        private Vector3 _lastPos, _savedPos;
        private Quaternion _savedRot;
        private byte _flags;
        private bool _monster;

        /// <summary>Spawn a new RemotePlayer proxy for a given network ID.</summary>
        public static RemotePlayer Spawn(ulong owner, NetClientInfo info, Vector3 pos, float yaw, bool monster)
        {
            var go = new GameObject("Runner " + owner);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.95f, 0f);
            cap.height = 1.9f;
            cap.radius = 0.35f;
            var actor = go.AddComponent<Actor>();
            actor.Kind = monster ? ActorKind.PlayerMonster : ActorKind.RemotePlayer;
            actor.OwnerClientId = owner;
            actor.Faction = monster ? Factions.Zone : info != null ? info.Faction : Factions.Freehold;
            actor.Squad = info != null ? info.Squad : "";
            actor.DisplayName = info != null ? info.Name : "Runner";
            actor.Height = 1.8f;
            actor.NetId = NetSocial.PlayerNetId(owner);
            var p = go.AddComponent<RemotePlayer>();
            p.Owner = owner;
            p.Actor = actor;
            p._monster = monster;
            HumanoidLook look = HumanoidLook.Survivor();
            look.TopColor = Color.Lerp(look.TopColor, Factions.Get(actor.Faction).Color, 0.5f);
            if (monster) { look = HumanoidLook.RandomZombie(new System.Random((int)owner)); look.SkinColor = new Color(0.25f, 0.27f, 0.32f); look.Scale = 1.12f; }
            p._rig = HumanoidRig.Create(go.transform, look);
            p._anim = p._rig.gameObject.AddComponent<HumanoidAnimator>();
            p._anim.AutoUpdate = false;
            p._anim.Gait = monster ? HumanoidGait.Runner : HumanoidGait.Survivor;
            actor.Head = p._rig.Bone(HumanBone.Head);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CloneZConfig.IgnoreRaycastLayer;
            go.AddComponent<ActorFog>();
            go.AddComponent<RemotePlayerInteract>();
            p._lastPos = pos;
            return p;
        }

        /// <summary>Update this proxy's state with the latest snapshot from the server.</summary>
        public void Push(uint seq, double now, Vector3 pos, float yaw, float pitch, byte flags, string held, byte hp)
        {
            _buffer.Add(new Snap { T = now, P = pos, Yaw = yaw, Pitch = pitch });
            if (_buffer.Count > 30) _buffer.RemoveAt(0);
            _history.Add(new Snap { T = now, P = pos, Yaw = yaw });
            while (_history.Count > 0 && now - _history[0].T > 1.2) _history.RemoveAt(0);
            _flags = flags;
            Actor.Surrendered = (flags & 16) != 0;
            Actor.Restrained = (flags & 32) != 0;
            Actor.Cloaked = (flags & 128) != 0;
            bool dead = (flags & 4) != 0;
            if (dead && !IsDead) Die();
            if (held != Held)
            {
                Held = held;
                if (held.StartsWith("form:")) { BecomeBeast(held.Substring(5)); return; }
                ItemDef d = ItemDatabase.Get(held);
                _rig.SetHeldItem(d, d == null ? HoldStyle.None : d.IsFirearm ? HoldStyle.Firearm : d.IsWeapon ? HoldStyle.Melee : HoldStyle.None);
            }
        }

        // A hunter's body: the creature it chose, not the zombie-shaped stand-in.
        private void BecomeBeast(string form)
        {
            CreatureDef def = CreatureCatalog.Get(form);
            if (def == null || _beast != null) return;
            foreach (Renderer r in _rig.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var go = new GameObject("Hunter body");
            go.transform.SetParent(transform, false);
            _beast = go.AddComponent<CreatureBody>();
            _beast.Build(def);
            _beast.ExternalSpeed01 = 0f;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CloneZConfig.IgnoreRaycastLayer;
            if (_beast.Head != null) Actor.Head = _beast.Head;
            Actor.DisplayName = def.Name;
            Actor.Height = def.Height;
            ActorFog fog = GetComponent<ActorFog>();
            if (fog != null) { Destroy(fog); gameObject.AddComponent<ActorFog>(); }
        }

        private void Update()
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null || _buffer.Count == 0 || IsDead) return;
            double render = nm.ServerTime.Time - NetPlayers.InterpolationDelay;
            Snap a = _buffer[0], b = _buffer[_buffer.Count - 1];
            for (int i = 0; i < _buffer.Count - 1; i++)
                if (_buffer[i].T <= render && _buffer[i + 1].T >= render) { a = _buffer[i]; b = _buffer[i + 1]; break; }
            float k = b.T > a.T ? Mathf.Clamp01((float)((render - a.T) / (b.T - a.T))) : 1f;
            transform.position = Vector3.Lerp(a.P, b.P, k);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(a.Yaw, b.Yaw, k), 0f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _anim == null) return;
            Vector3 v = (transform.position - _lastPos) / dt;
            _lastPos = transform.position;
            if (v.sqrMagnitude > 400f) v = Vector3.zero;
            if (_beast != null)
            {
                _beast.ExternalSpeed01 = IsDead ? 0f : Mathf.Clamp01(v.magnitude / 6f);
                _beast.ExternalAttack = (_flags & 2) != 0;
                _beast.ExternalCloak = Actor.Cloaked;
                return;
            }
            _anim.Velocity = IsDead ? Vector3.zero : v;
            _anim.Sneaking = (_flags & 1) != 0;
            _anim.Aiming = (_flags & 2) != 0;
            bool hands = Actor.Surrendered || Actor.Restrained;
            _anim.HasReachTarget = hands;
            if (hands) _anim.ReachTarget = transform.position + Vector3.up * 2.2f;
            _anim.Activity = hands ? HumanoidActivity.Use : HumanoidActivity.None;
            _anim.Evaluate(dt);
        }

        /// <summary>Trigger the firing animation on this proxy's weapon.</summary>
        public void Fired() { if (_anim != null) _anim.Fire(); }

        /// <summary>Mark this remote player as dead and play the death animation.</summary>
        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            if (_anim != null) _anim.Die(-transform.forward);
            if (_beast != null) _beast.Die(-transform.forward);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        /// <summary>Rewind or restore this proxy's position for lag compensation.</summary>
        public void Rewind(double time, bool rewind)
        {
            if (rewind)
            {
                _savedPos = transform.position;
                _savedRot = transform.rotation;
                for (int i = _history.Count - 1; i >= 0; i--)
                    if (_history[i].T <= time) { transform.position = _history[i].P; break; }
                Physics.SyncTransforms();
            }
            else
            {
                transform.SetPositionAndRotation(_savedPos, _savedRot);
                Physics.SyncTransforms();
            }
        }

        // Server: hits on this body are the owner's to suffer.
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            if (NetSession.NM != null && NetSession.NM.IsServer)
            {
                Actor attacker = info.Source != null ? info.Source.GetComponentInParent<Actor>() : null;
                if (attacker != null && attacker.IsPlayerControlled && !attacker.IsHostileTo(Actor) && !ServerRules.FriendlyFire) return;
                NetPlayers.SendDamage(Owner, info, attacker);
            }
            else if (Actor.NetId != 0) NetPlayers.RequestHit(Actor.NetId, info);
        }

        public void ReceiveZombieAttack(bool bite)
        {
            if (IsDead || NetSession.NM == null || !NetSession.NM.IsServer) return;
            using (var o = new NetOut(8)) { o.B(bite); NetSession.SendTo(Owner, "cz.zatk", o); }
        }
    }
}
