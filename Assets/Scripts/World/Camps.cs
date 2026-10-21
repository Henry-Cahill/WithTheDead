using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// DayZ's base building and raiding, fitted to raids into a Zone. What a runner builds in a Zone (walls,
    /// doorways, floors, barricades, gates with combination locks) is their camp: it is saved with the account per
    /// Zone and stands again the next time they go in. The offline-raid rule is simple and absolute: a camp is only
    /// in the Zone while its owner is, so nobody — player or AI — can touch it while you're away. While you're
    /// there, others can break in: pry a lock off with a crowbar, batter a wall down, or blow it open.
    /// </summary>
    public static class ZoneCamps
    {
        private static readonly Dictionary<string, ulong> s_TagClient = new Dictionary<string, ulong>();
        private static readonly Dictionary<ulong, string> s_ClientTag = new Dictionary<ulong, string>();
        private static bool s_Hooked;

        /// <summary>This account's owner tag (a short random id, made once).</summary>
        public static string MyTag
        {
            get
            {
                AccountProfile a = ProfileStore.Account;
                if (a == null) return "local";
                if (string.IsNullOrEmpty(a.Id)) { a.Id = System.Guid.NewGuid().ToString("N").Substring(0, 12); ProfileStore.Save(); }
                return a.Id;
            }
        }

        /// <summary>Checks if the given owner tag matches the local player's tag.</summary>
        public static bool IsMine(string owner) => owner != null && owner == MyTag;

        /// <summary>Checks if the given owner tag belongs to a teammate in the same squad.</summary>
        public static bool IsSquadmate(string owner)
            => owner != null && NetSession.Active && s_TagClient.TryGetValue(owner, out ulong c) && NetSocial.SameSquad(c);

        /// <summary>True when the local runner may attack a structure: someone else's camp, in the Zone.</summary>
        public static bool CanBash(string owner) => GameSession.IsExtraction && !string.IsNullOrEmpty(owner) && !IsMine(owner) && !IsSquadmate(owner);

        #region Save and restore
        internal static void Save(string zoneId)
        {
            AccountProfile a = ProfileStore.Account;
            if (a == null || string.IsNullOrEmpty(zoneId) || GameSession.IsMonsterRun) return;
            string me = MyTag;
            ZoneWorldState z = a.Zone(zoneId);
            z.Camp.Clear();
            z.CampBarricades.Clear();
            z.Gates.Clear();
            foreach (BuildPiece p in BuildPiece.All)
                if (p != null && p.Health > 0f && p.Owner == me)
                    z.Camp.Add(new HousePieceSave { Type = (int)p.Type, Position = p.transform.position, Yaw = p.transform.eulerAngles.y, Health = p.Health, MaxHealth = p.MaxHealth });
            foreach (Barricade b in Barricade.All)
                if (b != null && b.Planks > 0 && b.Owner == me)
                    z.CampBarricades.Add(new BarricadeSave { Position = b.transform.position, Forward = b.transform.forward, Planks = b.Planks, Health = b.Health, MaxHealth = b.MaxHealth, PerPlank = b.PerPlankHealth });
            foreach (CampGate g in CampGate.All)
                if (g != null && g.Hung && g.Piece != null && g.Piece.Owner == me) z.Gates.Add(g.Capture());
            ProfileStore.Save();
        }

        internal static void Restore(string zoneId)
        {
            AccountProfile a = ProfileStore.Account;
            if (a == null || GameSession.IsMonsterRun) return;
            ZoneWorldState z = a.Zone(zoneId);
            string me = MyTag;
            foreach (HousePieceSave h in z.Camp)
            {
                BuildPiece p = BuildPiece.SpawnRestored(null, (BuildPieceType)h.Type, h.Position, h.Yaw, h.Health, h.MaxHealth, false);
                p.Owner = me;
                if (p.Type == BuildPieceType.Doorway) CampGate.On(p);
// Announce(p); // removed redundant network broadcast
            }
            foreach (BarricadeSave s in z.CampBarricades)
            {
                Barricade b = Barricade.SpawnRestored(null, s.Position, s.Forward, s.Planks, s.Health, s.MaxHealth, s.PerPlank);
                b.Owner = me;
// Announce(b); // removed redundant network broadcast
            }
            foreach (CampGateSave g in z.Gates)
            {
                CampGate gate = CampGate.At(g.Position);
                if (gate != null) { gate.Apply(true, g.Open, g.Locked, g.Code); gate.Broadcast(); }
            }
            if (z.Camp.Count + z.CampBarricades.Count > 0)
                GameEvents.RaiseToast("Your camp stands where you left it (" + (z.Camp.Count + z.CampBarricades.Count) + " pieces).");
        }
        #endregion

        #region Networking
        internal static void Register()
        {
            NetSession.Register("cz.build", OnBuild);
            NetSession.Register("cz.unbuild", OnUnbuild);
            NetSession.Register("cz.gate", OnGate);
            NetSession.Register("cz.campgone", OnCampGone);
            NetSession.Register("cz.campq", OnCampQuery);
            NetSession.Register("cz.boom", NetExplosions.OnBoom);
            NetSession.Register("cz.boomfx", NetExplosions.OnBoomFx);
            if (s_Hooked) return;
            s_Hooked = true;
            NetSession.ClientLeft += id =>
            {
                if (!s_ClientTag.TryGetValue(id, out string tag)) return;
                s_ClientTag.Remove(id);
                Gone(tag);
                using (var o = new NetOut(64)) { o.S(tag); NetSession.ToClients("cz.campgone", o); }
            };
        }

        private static void WritePiece(NetOut o, BuildPiece p)
            => o.By(0).By((byte)p.Type).V(p.transform.position).F(p.transform.eulerAngles.y).V(Vector3.forward).I(0).F(p.Health).F(p.MaxHealth).F(0f).S(p.Owner).UL(OwnerClient(p.Owner));

        private static void WriteBarricade(NetOut o, Barricade b)
            => o.By(1).By(0).V(b.transform.position).F(0f).V(b.transform.forward).I(b.Planks).F(b.Health).F(b.MaxHealth).F(b.PerPlankHealth).S(b.Owner).UL(OwnerClient(b.Owner));

        private static ulong OwnerClient(string tag)
        {
            if (IsMine(tag) && NetSession.NM != null) return NetSession.NM.LocalClientId;
            return tag != null && s_TagClient.TryGetValue(tag, out ulong c) ? c : ulong.MaxValue;
        }

        /// <summary>Broadcasts a newly built piece to other players.</summary>
        public static void Announce(BuildPiece p)
        {
            if (!NetSession.Active || p == null || p.Owner == null) return;
            using (var o = new NetOut(128)) { WritePiece(o, p); Send("cz.build", o); }
        }

        /// <summary>Broadcasts a newly built barricade to other players.</summary>
        public static void Announce(Barricade b)
        {
            if (!NetSession.Active || b == null || b.Owner == null) return;
            using (var o = new NetOut(128)) { WriteBarricade(o, b); Send("cz.build", o); }
        }

        // From a client to the server (which passes it on); from the server straight to the clients.
        private static void Send(string name, NetOut o)
        {
            if (NetSession.NM.IsServer) NetSession.ToClients(name, o);
            else NetSession.ToServer(name, o);
        }

        private static void Relay(ulong sender, string name, NetOut o)
        {
            if (NetSession.NM.IsServer) NetSession.ToClients(name, o, Unity.Netcode.NetworkDelivery.ReliableSequenced, sender);
        }

        private static void OnBuild(ulong sender, NetIn m)
        {
            byte kind = m.By(), type = m.By();
            Vector3 pos = m.V();
            float yaw = m.F();
            Vector3 fwd = m.V();
            int planks = m.I();
            float hp = m.F(), max = m.F(), per = m.F();
            string owner = m.S();
            ulong client = m.UL();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
            {
                client = sender;
                s_ClientTag[sender] = owner;
                using (var o = new NetOut(128))
                {
                    o.By(kind).By(type).V(pos).F(yaw).V(fwd).I(planks).F(hp).F(max).F(per).S(owner).UL(client);
                    Relay(sender, "cz.build", o);
                }
            }
            if (client != ulong.MaxValue) s_TagClient[owner] = client;
            if (IsMine(owner)) return;
            if (kind == 0)
            {
                BuildPiece p = FindPiece(pos);
                if (p == null)
                {
                    p = BuildPiece.SpawnRestored(null, (BuildPieceType)type, pos, yaw, hp, max, false);
                    p.Owner = owner;
                    if (p.Type == BuildPieceType.Doorway) CampGate.On(p);
                }
            }
            else if (FindBarricade(pos) == null)
            {
                Barricade b = Barricade.SpawnRestored(null, pos, fwd, planks, hp, max, per);
                b.Owner = owner;
            }
        }

        private static void OnUnbuild(ulong sender, NetIn m)
        {
            byte kind = m.By();
            Vector3 pos = m.V();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
                using (var o = new NetOut(32)) { o.By(kind).V(pos); Relay(sender, "cz.unbuild", o); }
            if (kind == 0) { BuildPiece p = FindPiece(pos); if (p != null) p.Vanish(); }
            else { Barricade b = FindBarricade(pos); if (b != null) b.Vanish(); }
        }

        private static void OnGate(ulong sender, NetIn m)
        {
            Vector3 pos = m.V();
            bool hung = m.B(), open = m.B(), locked = m.B();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
                using (var o = new NetOut(48)) { o.V(pos).B(hung).B(open).B(locked); Relay(sender, "cz.gate", o); }
            CampGate g = CampGate.At(pos);
            if (g != null) g.Apply(hung, open, locked, g.Code);
        }

        private static void OnCampGone(ulong sender, NetIn m)
        {
            string tag = m.S();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
                using (var o = new NetOut(64)) { o.S(tag); Relay(sender, "cz.campgone", o); }
            Gone(tag);
        }

        // A runner left the Zone: their camp leaves with them (offline-raid protection) — unless the server's rules
        // leave camps standing (and open to raiders) until the raid ends.
        private static void Gone(string tag)
        {
            if (string.IsNullOrEmpty(tag) || IsMine(tag) || !SandboxSettings.Current.OfflineRaidProtection) return;
            foreach (BuildPiece p in new List<BuildPiece>(BuildPiece.All)) if (p != null && p.Owner == tag) p.Vanish();
            foreach (Barricade b in new List<Barricade>(Barricade.All)) if (b != null && b.Owner == tag) b.Vanish();
        }

        // A client entering the raid asks for every camp already standing.
        private static void OnCampQuery(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            foreach (BuildPiece p in BuildPiece.All)
                if (p != null && p.Owner != null && p.Health > 0f)
                    using (var o = new NetOut(128)) { WritePiece(o, p); NetSession.SendTo(sender, "cz.build", o); }
            foreach (Barricade b in Barricade.All)
                if (b != null && b.Owner != null && b.Planks > 0)
                    using (var o = new NetOut(128)) { WriteBarricade(o, b); NetSession.SendTo(sender, "cz.build", o); }
            foreach (CampGate g in CampGate.All)
                if (g != null && g.Hung)
                    using (var o = new NetOut(48)) { o.V(g.transform.position).B(true).B(g.Open).B(g.Locked); NetSession.SendTo(sender, "cz.gate", o); }
        }

        internal static void OnRemoved(MonoBehaviour s, byte kind)
        {
            if (!NetSession.Active || s == null) return;
            string owner = s is BuildPiece p ? p.Owner : s is Barricade b ? b.Owner : null;
            if (owner == null) return;
            using (var o = new NetOut(32)) { o.By(kind).V(s.transform.position); Send("cz.unbuild", o); }
        }

        internal static void LeavingRaid()
        {
            if (!NetSession.Active) return;
            using (var o = new NetOut(64)) { o.S(MyTag); Send("cz.campgone", o); }
        }

        internal static void AskForCamps()
        {
            if (NetSession.Active && !NetSession.NM.IsServer) using (var o = new NetOut(8)) NetSession.ToServer("cz.campq", o);
        }

        /// <summary>Finds a built piece at the given world position.</summary>
        public static BuildPiece FindPiece(Vector3 pos)
        {
            foreach (BuildPiece p in BuildPiece.All) if (p != null && (p.transform.position - pos).sqrMagnitude < 0.01f) return p;
            return null;
        }

        /// <summary>Finds a barricade at the given world position.</summary>
        public static Barricade FindBarricade(Vector3 pos)
        {
            foreach (Barricade b in Barricade.All) if (b != null && (b.transform.position - pos).sqrMagnitude < 0.01f) return b;
            return null;
        }
        #endregion
    }

    /// <summary>
    /// A gate hung in a camp doorway (planks and nails), and its combination lock. The owner — and their squad —
    /// open it freely; anyone else has to pry the lock off with a crowbar (loud, slow), batter the doorway down, or
    /// blow it open.
    /// </summary>
    public sealed class CampGate : Interactable
    {
        public static new readonly List<CampGate> All = new List<CampGate>();
        /// <summary>The build piece this gate is attached to.</summary>
        public BuildPiece Piece { get; private set; }
        /// <summary>Whether the gate is currently hung (installed).</summary>
        public bool Hung { get; private set; }
        /// <summary>Whether the gate is currently open.</summary>
        public bool Open { get; private set; }
        /// <summary>Whether the gate is currently locked with a combination lock.</summary>
        public bool Locked { get; private set; }
        /// <summary>The combination lock code, if the gate is locked.</summary>
        public string Code { get; private set; }

        private Transform _pivot;
        private GameObject _block;
        private float _noiseAt;

        /// <summary>Creates or retrieves the CampGate component for a piece.</summary>
        public static CampGate On(BuildPiece p)
        {
            CampGate g = p.GetComponent<CampGate>();
            if (g == null) g = p.gameObject.AddComponent<CampGate>();
            g.Piece = p;
            return g;
        }

        /// <summary>Finds the CampGate at the given world position.</summary>
        public static CampGate At(Vector3 pos)
        {
            foreach (CampGate g in All) if (g != null && (g.transform.position - pos).sqrMagnitude < 0.01f) return g;
            return null;
        }

        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        public override string DisplayName => !Hung ? "Camp doorway" : Locked ? "Gate (locked)" : "Gate";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.1f;
        public override float Reach => 2.4f;
        public override bool IsAvailable => isActiveAndEnabled && Piece != null && Piece.Health > 0f && GameSession.IsExtraction;

        private bool Trusted => Piece != null && (ZoneCamps.IsMine(Piece.Owner) || ZoneCamps.IsSquadmate(Piece.Owner));

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerInventory inv = ctx.Inventory;
            if (inv == null || Piece == null) return;
            if (!Hung)
            {
                if (!Trusted) return;
                bool can = inv.Has("hammer") && inv.Has("plank", 4) && inv.Has("nails", 6);
                into.Add(new InteractAction("Hang a gate (4 planks, 6 nails)", c =>
                {
                    if (!c.Inventory.Has("plank", 4) || !c.Inventory.Has("nails", 6)) return;
                    c.Inventory.Consume("plank", 4);
                    c.Inventory.Consume("nails", 6);
                    c.Inventory.WearTool("hammer");
                    if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.Carpentry, 8f);
                    Apply(true, false, false, null);
                    Broadcast();
                }).Timed(6f, "Hanging the gate...", HumanoidActivity.Build).DisabledIf(!can, "Needs a hammer, 4 planks and 6 nails"));
                return;
            }
            if (Locked && !Trusted)
            {
                into.Add(new InteractAction("Pry the lock off", c =>
                {
                    Apply(true, Open, false, null);
                    Broadcast();
                    c.Inventory.WearTool("crowbar");
                    c.Notify("The lock gives.");
                }).Timed(45f, "Prying at the lock...", HumanoidActivity.Build).DisabledIf(!inv.Has("crowbar"), "A combination lock. A crowbar would get it off — loudly."));
                into[into.Count - 1].Tick = (c, dt, p) =>
                {
                    if (Time.time < _noiseAt) return;
                    _noiseAt = Time.time + 1.5f;
                    NoiseSystem.Emit(transform.position, 28f);
                    Sfx.Play(SfxIds.DoorBash, transform.position, 0.7f, Random.Range(0.9f, 1.1f), 40f);
                };
                return;
            }
            into.Add(new InteractAction(Open ? "Close the gate" : "Open the gate", c =>
            {
                Apply(true, !Open, Locked, Code);
                Broadcast();
                Sfx.Play(Open ? SfxIds.DoorOpen : SfxIds.DoorClose, transform.position, 0.8f, 0.85f, 18f);
            }));
            if (!Trusted) return;
            if (!Locked)
                into.Add(new InteractAction("Fit a combination lock", c =>
                {
                    if (c.Inventory.Consume("combolock", 1) <= 0) return;
                    string code = Random.Range(0, 1000).ToString("000");
                    Apply(true, false, true, code);
                    Broadcast();
                    c.Notify("Locked. The code is " + code + " — your squad knows it too.");
                }).Timed(3f, "Fitting the lock...").DisabledIf(!inv.Has("combolock"), "Needs a combination lock"));
            else
                into.Add(new InteractAction("Take the lock off (" + (Code ?? "???") + ")", c =>
                {
                    Apply(true, Open, false, null);
                    Broadcast();
                    ItemDef lockDef = ItemDatabase.Get("combolock");
                    if (lockDef != null) c.Inventory.Inventory.Add(lockDef, 1, enforceWeight: false);
                }).Timed(2f, "Unlocking..."));
        }

        /// <summary>Applies the gate's hung/open/locked state and updates its visuals.</summary>
        public void Apply(bool hung, bool open, bool locked, string code)
        {
            Hung = hung;
            Open = open;
            Locked = locked;
            Code = locked ? code : null;
            if (!Hung) { if (_pivot != null) _pivot.gameObject.SetActive(false); if (_block != null) _block.SetActive(false); return; }
            EnsureLeaf();
            _pivot.gameObject.SetActive(true);
            _pivot.localRotation = Quaternion.Euler(0f, Open ? -100f : 0f, 0f);
            _block.SetActive(!Open);
        }

        /// <summary>Sends the gate's current state to other players.</summary>
        public void Broadcast()
        {
            if (!NetSession.Active) return;
            using (var o = new NetOut(48))
            {
                o.V(transform.position).B(Hung).B(Open).B(Locked);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.gate", o);
                else NetSession.ToServer("cz.gate", o);
            }
        }

        /// <summary>Creates a saved representation of the gate's state.</summary>
        public CampGateSave Capture() => new CampGateSave { Position = transform.position, Open = Open, Locked = Locked, Code = Code };

        // A plank leaf on a hinge at the left jamb, and a blocking box (with a navmesh carve) while it's shut.
        private void EnsureLeaf()
        {
            if (_pivot != null) return;
            float gap = CloneZConfig.HouseDoorwayGapWidth;
            float h = CloneZConfig.HouseWallHeight - CloneZConfig.HouseDoorwayLintelHeight;
            _pivot = new GameObject("Gate hinge").transform;
            _pivot.SetParent(transform, false);
            _pivot.localPosition = new Vector3(-gap * 0.5f, 0f, 0f);
            Color wood = new Color(0.47f, 0.36f, 0.24f);
            for (int i = 0; i < 4; i++)
            {
                var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Slat";
                slat.transform.SetParent(_pivot, false);
                slat.transform.localPosition = new Vector3(gap * (0.125f + i * 0.25f), h * 0.5f, 0f);
                slat.transform.localScale = new Vector3(gap * 0.23f, h * 0.96f, 0.06f);
                slat.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(wood * (i % 2 == 0 ? 1f : 0.92f));
                Destroy(slat.GetComponent<Collider>());
            }
            var brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brace.name = "Brace";
            brace.transform.SetParent(_pivot, false);
            brace.transform.localPosition = new Vector3(gap * 0.5f, h * 0.5f, -0.05f);
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, 55f);
            brace.transform.localScale = new Vector3(0.08f, h * 1.05f, 0.05f);
            brace.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(wood * 0.8f);
            Destroy(brace.GetComponent<Collider>());

            _block = new GameObject("Gate block");
            _block.transform.SetParent(transform, false);
            var col = _block.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h * 0.5f, 0f);
            col.size = new Vector3(gap, h, 0.12f);
            var carve = _block.AddComponent<NavMeshObstacle>();
            carve.shape = NavMeshObstacleShape.Box;
            carve.center = col.center;
            carve.size = new Vector3(gap, h, 0.3f);
            carve.carving = true;
            carve.carveOnlyStationary = true;
        }
    }

    /// <summary>Blasts break camps: every wall, gate and barricade near an explosion takes its force.</summary>
    public sealed class StructureBlast : IExplosionTarget
    {
        /// <summary>Handles an explosion, damaging nearby structures.</summary>
        public void OnExplosion(Vector3 center, float radius, float damage)
        {
            float reach = radius + 1.2f;
            foreach (BuildPiece p in new List<BuildPiece>(BuildPiece.All))
            {
                if (p == null || p.Health <= 0f || p.Prebuilt) continue;
                float d = Vector3.Distance(center, p.transform.position + Vector3.up * 0.8f);
                if (d > reach) continue;
                float falloff = 1f - Mathf.Clamp01(d / reach);
                p.TakeDamage(new DamageInfo(damage * falloff, DamageKind.Explosion, p.transform.position, (p.transform.position - center).normalized, null));
            }
            foreach (Barricade b in new List<Barricade>(Barricade.All))
            {
                if (b == null || b.Planks <= 0) continue;
                float d = Vector3.Distance(center, b.transform.position + Vector3.up * 0.6f);
                if (d > reach) continue;
                float falloff = 1f - Mathf.Clamp01(d / reach);
                b.TakeDamage(new DamageInfo(damage * falloff, DamageKind.Explosion, b.transform.position, (b.transform.position - center).normalized, null));
            }
        }
    }

    /// <summary>
    /// Explosions go through the server (it owns the AI, so it decides who a blast hurts); everyone sees and
    /// hears them.
    /// </summary>
    public static class NetExplosions
    {
        /// <summary>Detonates an explosion and synchronises it over the network.</summary>
        public static void Detonate(Vector3 at, float radius, float damage, GameObject source, string weapon)
        {
            if (GameSession.Networked && NetSession.Active && !NetSession.NM.IsServer)
            {
                using (var o = new NetOut(64)) { o.V(at).F(radius).F(damage).S(weapon ?? "explosion"); NetSession.ToServer("cz.boom", o); }
                return;
            }
            CombatResolver.Explosion(at, radius, damage, source, weapon ?? "explosion");
            ExplosionFx.Play(at, radius);
            if (NetSession.Active) using (var o = new NetOut(32)) { o.V(at).F(radius); NetSession.ToClients("cz.boomfx", o); }
        }

        internal static void OnBoom(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            Vector3 at = m.V();
            float radius = Mathf.Clamp(m.F(), 0.5f, 8f);
            float damage = Mathf.Clamp(m.F(), 0f, 800f);
            string weapon = m.S();
            RemotePlayer p = NetPlayers.ProxyOf(sender);
            if (p != null && (p.transform.position - at).magnitude > 80f) return;
            Detonate(at, radius, damage, p != null ? p.gameObject : null, weapon);
        }

        internal static void OnBoomFx(ulong sender, NetIn m)
        {
            Vector3 at = m.V();
            float radius = m.F();
            Sfx.Play(SfxIds.Explosion, at, 1f, Random.Range(0.9f, 1.05f), 120f);
            GameEvents.RaiseWorldEvent(at, radius * 6f, "explosion");
            ExplosionFx.Play(at, radius);
        }
    }

    /// <summary>A flash, a fireball and a smoke puff.</summary>
    public sealed class ExplosionFx : MonoBehaviour
    {
        private Light _light;
        private Transform _ball, _smoke;
        private Material _ballMat, _smokeMat;
        private float _t, _radius;

        /// <summary>Spawns the visual effect for an explosion.</summary>
        public static void Play(Vector3 at, float radius)
        {
            var go = new GameObject("Explosion");
            go.transform.position = at + Vector3.up * 0.3f;
            var fx = go.AddComponent<ExplosionFx>();
            fx._radius = radius;
            fx._light = go.AddComponent<Light>();
            fx._light.type = LightType.Point;
            fx._light.color = new Color(1f, 0.65f, 0.3f);
            fx._light.range = radius * 5f;
            fx._light.intensity = 12f;
            fx._ball = Sphere(go.transform, new Color(1f, 0.55f, 0.15f, 0.9f), out fx._ballMat);
            fx._smoke = Sphere(go.transform, new Color(0.18f, 0.17f, 0.16f, 0.7f), out fx._smokeMat);
            Destroy(go, 3.5f);
        }

        private static Transform Sphere(Transform parent, Color c, out Material mat)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false);
            s.transform.localScale = Vector3.one * 0.2f;
            mat = CreatureMaterials.TransparentLit(c, 0.2f);
            Renderer r = s.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return s.transform;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / 0.35f);
            _ball.localScale = Vector3.one * Mathf.Lerp(0.2f, _radius * 1.4f, k);
            Color b = _ballMat.color;
            b.a = Mathf.Lerp(0.9f, 0f, Mathf.Clamp01(_t / 0.5f));
            _ballMat.color = b;
            _smoke.localScale = Vector3.one * Mathf.Lerp(0.5f, _radius * 2.2f, Mathf.Clamp01(_t / 2.5f));
            _smoke.localPosition = Vector3.up * _t * 0.8f;
            Color s = _smokeMat.color;
            s.a = Mathf.Lerp(0.7f, 0f, Mathf.Clamp01((_t - 0.3f) / 3f));
            _smokeMat.color = s;
            _light.intensity = Mathf.Lerp(12f, 0f, Mathf.Clamp01(_t / 0.4f));
        }

        private void OnDestroy()
        {
            if (_ballMat != null) Destroy(_ballMat);
            if (_smokeMat != null) Destroy(_smokeMat);
        }
    }

    /// <summary>
    /// Things that go bang: a frag grenade (the grenade key, or "Throw" from the pack — a lob that bounces and goes
    /// off three and a half seconds later) and a breaching charge (armed against the nearest wall or gate: ten
    /// seconds of beeping, then a hole).
    /// </summary>
    public static class Explosives
    {
        public const float GrenadeFuse = 3.5f, GrenadeRadius = 6f, GrenadeDamage = 260f;
        public const float BreachFuse = 10f, BreachRadius = 2.5f, BreachDamage = 650f;

        internal static void Register()
        {
            ItemActions.RegisterUse("grenade", (inv, s) => { Throw(inv); return true; });
            ItemActions.RegisterUse("breachcharge", (inv, s) => { Arm(inv); return true; });
        }

        /// <summary>Throws a grenade from the given inventory.</summary>
        public static void Throw(PlayerInventory inv)
        {
            if (inv == null || HubRules.SafeZone || inv.Consume("grenade", 1) <= 0) { if (inv != null && !inv.Has("grenade")) GameEvents.RaiseToast("No grenades."); return; }
            Transform t = inv.transform;
            PlayerController ctrl = inv.GetComponent<PlayerController>();
            Vector3 aim = ctrl != null ? ctrl.AimDirection : t.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = t.forward;
            aim.Normalize();
            Camera cam = Camera.main;
            float lift = ViewState.MouseLook && cam != null ? Mathf.Clamp(cam.transform.forward.y + 0.35f, 0.1f, 0.9f) : 0.45f;
            Vector3 v = (aim + Vector3.up * lift).normalized * 13f;
            Sfx.Play(SfxIds.Grenade, t.position, 0.8f, 1f, 12f);
            Grenade.Spawn(t.position + Vector3.up * 1.6f + aim * 0.5f, v, inv.gameObject);
        }

        /// <summary>Arms a breach charge from the given inventory.</summary>
        public static void Arm(PlayerInventory inv)
        {
            if (inv == null || HubRules.SafeZone) return;
            Vector3 at = inv.transform.position + inv.transform.forward * 0.6f;
            if (inv.Consume("breachcharge", 1) <= 0) return;
            BreachCharge.Place(at, inv.gameObject);
            GameEvents.RaiseToast("Charge armed — ten seconds. Get clear.");
        }
    }

    /// <summary>A thrown frag: physics, a fuse, then a server-side blast.</summary>
    public sealed class Grenade : MonoBehaviour
    {
        private float _fuse = Explosives.GrenadeFuse;
        private GameObject _thrower;

        /// <summary>Spawns a grenade object with the given parameters.</summary>
public static void Spawn(Vector3 at, Vector3 velocity, GameObject thrower)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Grenade";
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.11f;
            go.layer = CloneZConfig.IgnoreRaycastLayer;
            go.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.22f, 0.27f, 0.18f));
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;
            var g = go.AddComponent<Grenade>();
            g._thrower = thrower;
            if (thrower != null)
                foreach (Collider c in thrower.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(go.GetComponent<Collider>(), c);
        }

        private void Update()
        {
            _fuse -= Time.deltaTime;
            if (_fuse > 0f) return;
            NetExplosions.Detonate(transform.position, Explosives.GrenadeRadius, Explosives.GrenadeDamage, _thrower, "grenade");
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision c)
        {
            if (c.relativeVelocity.magnitude > 2f) Sfx.Play(SfxIds.BoltLand, transform.position, 0.5f, 0.8f, 15f);
        }
    }

    /// <summary>A breaching charge: ten seconds of beeping, then a blast that takes walls and gates apart.</summary>
    public sealed class BreachCharge : MonoBehaviour
    {
        private float _fuse = Explosives.BreachFuse, _beep;
        private GameObject _owner;
        private Light _led;

        /// <summary>Places a breaching charge at the given location.</summary>
public static void Place(Vector3 at, GameObject owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Breaching charge";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(at.x, 0.12f, at.z);
            go.transform.localScale = new Vector3(0.3f, 0.2f, 0.12f);
            go.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.3f, 0.3f, 0.25f));
            var c = go.AddComponent<BreachCharge>();
            c._owner = owner;
            c._led = new GameObject("LED").AddComponent<Light>();
            c._led.transform.SetParent(go.transform, false);
            c._led.transform.localPosition = Vector3.up * 0.8f;
            c._led.color = Color.red;
            c._led.range = 1.5f;
            c._led.intensity = 0f;
            NoiseSystem.Emit(at, 6f);
        }

        private void Update()
        {
            _fuse -= Time.deltaTime;
            _beep -= Time.deltaTime;
            float rate = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(_fuse / Explosives.BreachFuse));
            if (_beep <= 0f)
            {
                _beep = rate;
                Sfx.Play(SfxIds.DetectorBeep, transform.position, 0.9f, 1.5f, 25f);
                _led.intensity = 3f;
            }
            else _led.intensity = Mathf.MoveTowards(_led.intensity, 0f, Time.deltaTime * 20f);
            if (_fuse > 0f) return;
            NetExplosions.Detonate(transform.position, Explosives.BreachRadius, Explosives.BreachDamage, _owner, "breachcharge");
            Destroy(gameObject);
        }
    }

    /// <summary>Wires camps (save, restore, gates, net), structural blasts and the explosives in.</summary>
    public sealed class CampModule : IGameModule
    {
        /// <summary>Order of execution for the camp module during bootstrap.</summary>
public int Order => 94;

        /// <summary>Initialises the camp system, registering events and hooks.</summary>
public void Init()
        {
            CombatResolver.Breakables.Add(new StructureBlast());
            Explosives.Register();
            BuildPiece.Raised += p =>
            {
                if (p.Type == BuildPieceType.Doorway && p.Owner != null) CampGate.On(p);
                ZoneCamps.Announce(p);
            };
            Barricade.Raised += ZoneCamps.Announce;
            BuildPiece.Removed += p => ZoneCamps.OnRemoved(p, 0);
            Barricade.Removed += b => ZoneCamps.OnRemoved(b, 1);
            WorldHooks.RaidStarted += (world, map, zone) =>
            {
                ZoneCamps.Restore((zone ?? ZoneCatalog.Current).Id);
                ZoneCamps.AskForCamps();
            };
            GameEvents.RaidEnded += (o, exit) =>
            {
                ZoneCamps.Save(GameSession.ZoneId);
                ZoneCamps.LeavingRaid();
            };
            WorldHooks.PlayerBuilt += p => { if (p != null && p.GetComponent<GrenadeKey>() == null) p.gameObject.AddComponent<GrenadeKey>(); };
            NetModuleHooks.All.Add(s => ZoneCamps.Register());
        }
    }

    /// <summary>The grenade key throws the next frag from the pack.</summary>
    public sealed class GrenadeKey : MonoBehaviour
    {
        private PlayerInventory _inv;
        private float _ready;

        private void Awake() => _inv = GetComponent<PlayerInventory>();

        private void Update()
        {
            if (_inv == null || Time.time < _ready || !GameInput.Pressed(GameAction.Grenade)) return;
            if (GameSession.IsMonsterRun) return;
            _ready = Time.time + 1f;
            Explosives.Throw(_inv);
        }
    }
}
