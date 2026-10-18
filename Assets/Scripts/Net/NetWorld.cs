using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The shared world. The server owns everything that thinks — the dead, the Zone's people, the beasts — and
    /// sends each client only those near it (interest management) at 10 Hz; clients draw them as proxies. The map,
    /// the loot rolls and the anomalies come from the shared raid seed, so only changes travel: the clock and the
    /// weather, emissions and the helicopter, containers being searched (one runner at a time) and emptied, things
    /// dropped and picked up, doors and switches, outposts changing hands.
    /// </summary>
    public static class NetWorld
    {
        public static float PendingHour = -1f;
        private const float InterestRadius = 85f;
        private static float s_AiAt, s_WorldAt;
        private static bool s_Hooked;
        private static readonly Dictionary<ulong, HashSet<uint>> s_Known = new Dictionary<ulong, HashSet<uint>>();
        private static readonly Dictionary<uint, NetAgentProxy> s_Agents = new Dictionary<uint, NetAgentProxy>();
        private static readonly Dictionary<string, ulong> s_Locks = new Dictionary<string, ulong>();
        private static readonly List<Actor> s_Scratch = new List<Actor>(128);

        internal static void Register()
        {
            NetSession.Register("cz.ai", OnAi);
            NetSession.Register("cz.aispawn", OnAiSpawn);
            NetSession.Register("cz.aigone", (s, m) => { uint id = m.U(); if (s_Agents.TryGetValue(id, out NetAgentProxy p) && p != null) Object.Destroy(p.gameObject); s_Agents.Remove(id); });
            NetSession.Register("cz.world", OnWorld);
            NetSession.Register("cz.emis", (s, m) => EmissionDirector.RemoteStart(m.B()));
            NetSession.Register("cz.heli", (s, m) => HeliFlyoverDirector.Play(m.V(), m.V()));
            NetSession.Register("cz.lock", OnLock);
            NetSession.Register("cz.locked", OnLocked);
            NetSession.Register("cz.cont", OnContents);
            NetSession.Register("cz.ground", OnGround);
            NetSession.Register("cz.fix", OnFixture);
            NetSession.Register("cz.outpost", OnOutpost);
            NetSession.Register("cz.outcap", OnOutpostCapture);
            NetSession.Register("cz.outq", OnOutpostQuery);
            NetSession.Register("cz.outno", (s, m) => GameEvents.RaiseToast(m.S()));

            if (s_Hooked) return;
            s_Hooked = true;
            EmissionDirector.Started += psi => { if (NetSession.NM != null && NetSession.NM.IsServer) using (var o = new NetOut(8)) { o.B(psi); NetSession.ToClients("cz.emis", o); } };
            HeliFlyoverDirector.Started += (a, b) => { if (NetSession.NM != null && NetSession.NM.IsServer) using (var o = new NetOut(32)) { o.V(a).V(b); NetSession.ToClients("cz.heli", o); } };
            InventoryUIController.ContainerOpened += c => { if (NetSession.Active && c != null) using (var o = new NetOut(64)) { o.S(KeyOf(c)).B(true); NetSession.ToServer("cz.lock", o); } };
            InventoryUIController.ContainerClosed += c =>
            {
                if (!NetSession.Active || c == null) return;
                SendContents(c);
                using (var o = new NetOut(64)) { o.S(KeyOf(c)).B(false); NetSession.ToServer("cz.lock", o); }
            };
            PlayerInventory.GroundTaken += (def, n, at) => SendGround(false, def, n, at, null);
            PlayerInventory.GroundDropped += (def, n, at, extra) => SendGround(true, def, n, at, extra);
            PlayerInteractor.ActionRan += SendFixture;
            WorldHooks.RaidStarted += (w, m, z) =>
            {
                s_Known.Clear();
                s_Agents.Clear();
                s_Locks.Clear();
                if (PendingHour >= 0f && TimeOfDay.Instance != null) { TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, PendingHour); PendingHour = -1f; }
                if (NetSession.Active && !NetSession.NM.IsServer) using (var o = new NetOut(8)) NetSession.ToServer("cz.outq", o);
            };
        }

        internal static void Tick()
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null || !nm.IsServer) return;
            if (Time.time >= s_AiAt) { s_AiAt = Time.time + 0.1f; SendAi(nm); }
            if (Time.time >= s_WorldAt)
            {
                s_WorldAt = Time.time + 3f;
                TimeOfDay t = TimeOfDay.Instance;
                WeatherSystem w = WeatherSystem.Instance;
                using (var o = new NetOut(32))
                {
                    o.F(t != null ? t.Hour : -1f).I(t != null ? t.Day : 1).By((byte)(w != null ? w.Current : Weather.Clear))
                        .F(w != null ? w.PrecipitationIntensity : 0f).F(w != null ? w.FogIntensity : 0f).F(w != null ? w.Cloudiness : 0f);
                    NetSession.ToClients("cz.world", o, NetworkDelivery.UnreliableSequenced);
                }
            }
        }

        private static void OnWorld(ulong s, NetIn m)
        {
            float hour = m.F();
            int day = m.I();
            var weather = (Weather)m.By();
            float rain = m.F(), fog = m.F(), cloud = m.F();
            TimeOfDay t = TimeOfDay.Instance;
            if (t != null && hour >= 0f && Mathf.Abs(Mathf.DeltaAngle(t.Hour * 15f, hour * 15f)) > 1.5f) t.RestoreClock(day, hour);
            WeatherSystem w = WeatherSystem.Instance;
            if (w != null && w.Current != weather) w.Restore(weather, rain, fog, cloud);
        }

        #region AI
        private static byte KindCode(Actor a) => a.Kind == ActorKind.Zombie ? (byte)0 : a.Kind == ActorKind.Human || a.Kind == ActorKind.Companion ? (byte)1 : (byte)2;

        private static void SendAi(NetworkManager nm)
        {
            s_Scratch.Clear();
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a.IsPlayerControlled || a.GetComponent<NetAgentProxy>() != null) continue;
                if (a.NetId == 0) a.NetId = Actor.AllocateNetId();
                s_Scratch.Add(a);
            }
            foreach (ulong client in nm.ConnectedClientsIds)
            {
                if (client == nm.LocalClientId || !NetSession.Clients.TryGetValue(client, out NetClientInfo ci) || !ci.InRaid) continue;
                if (!s_Known.TryGetValue(client, out HashSet<uint> known)) s_Known[client] = known = new HashSet<uint>();
                var visible = new List<Actor>();
                foreach (Actor a in s_Scratch)
                {
                    float d = Vector3.Distance(a.transform.position, ci.Position);
                    if (d > InterestRadius) continue;
                    // Close things always; further ones only when the client could see them (no tracking through walls).
                    if (d > 12f && Physics.Linecast(ci.Eye, a.Center, Actor.SightMask, QueryTriggerInteraction.Ignore)
                        && Physics.Linecast(ci.Eye, a.EyePosition, Actor.SightMask, QueryTriggerInteraction.Ignore) && !known.Contains(a.NetId)) continue;
                    visible.Add(a);
                }
                var keep = new HashSet<uint>();
                using (var o = new NetOut(64 + visible.Count * 24, 1 << 15))
                {
                    o.I(visible.Count);
                    foreach (Actor a in visible)
                    {
                        if (!known.Contains(a.NetId)) SendSpawn(client, a);
                        keep.Add(a.NetId);
                        byte flags = 0;
                        if (!a.IsAlive) flags |= 1;
                        var z = a.GetComponent<ZombieAI>();
                        if (z != null && z.IsHunting) flags |= 2;
                        var h = a.GetComponent<HumanNpc>();
                        if (h != null) { if (h.Crouched) flags |= 4; if (h.Aiming) flags |= 8; }
                        var cr = a.GetComponent<Creature>();
                        if (cr != null && cr.Attacking) flags |= 16;
                        if (a.Surrendered) flags |= 32;
                        if (a.Cloaked) flags |= 64;
                        o.U(a.NetId).V(a.transform.position).By((byte)(a.transform.eulerAngles.y / 360f * 255f)).By(flags);
                    }
                    NetSession.SendTo(client, "cz.ai", o, NetworkDelivery.UnreliableSequenced);
                }
                foreach (uint id in known)
                    if (!keep.Contains(id)) using (var o = new NetOut(8)) { o.U(id); NetSession.SendTo(client, "cz.aigone", o); }
                known.Clear();
                foreach (uint id in keep) known.Add(id);
            }
        }

        private static void SendSpawn(ulong client, Actor a)
        {
            string sub = "";
            var cr = a.GetComponent<Creature>();
            if (cr != null) sub = cr.Def.Id;
            var h = a.GetComponent<HumanNpc>();
            if (h != null) sub = h.Loadout.Id + "|" + (h.Gun != null ? h.Gun.Id : "");
            using (var o = new NetOut(128))
            {
                o.U(a.NetId).By(KindCode(a)).By((byte)a.Kind).S(sub).S(a.Faction).S(a.DisplayName).I((int)a.NetId * 7919);
                NetSession.SendTo(client, "cz.aispawn", o);
            }
        }

        private static void OnAiSpawn(ulong s, NetIn m)
        {
            uint id = m.U();
            byte code = m.By();
            var kind = (ActorKind)m.By();
            string sub = m.S(), faction = m.S(), name = m.S();
            int seed = m.I();
            if (s_Agents.TryGetValue(id, out NetAgentProxy old) && old != null) return;
            s_Agents[id] = NetAgentProxy.Spawn(id, code, kind, sub, faction, name, seed);
        }

        private static void OnAi(ulong s, NetIn m)
        {
            int n = m.I();
            for (int i = 0; i < n; i++)
            {
                uint id = m.U();
                Vector3 p = m.V();
                float yaw = m.By() / 255f * 360f;
                byte flags = m.By();
                if (s_Agents.TryGetValue(id, out NetAgentProxy a) && a != null) a.Push(p, yaw, flags);
            }
        }
        #endregion

        #region Containers, ground items, fixtures
        public static string KeyOf(LootContainer c)
        {
            if (!string.IsNullOrEmpty(c.FixtureKey)) return c.FixtureKey;
            Vector3 p = c.transform.position;
            return "c" + Mathf.RoundToInt(p.x * 10f) + "_" + Mathf.RoundToInt(p.y * 10f) + "_" + Mathf.RoundToInt(p.z * 10f);
        }

        private static LootContainer Find(string key)
        {
            foreach (LootContainer c in LootContainer.All) if (c != null && KeyOf(c) == key) return c;
            return null;
        }

        // One runner rummages at a time: the server hands out the lock.
        private static void OnLock(ulong sender, NetIn m)
        {
            string key = m.S();
            bool take = m.B();
            if (!NetSession.NM.IsServer) return;
            if (take)
            {
                bool free = !s_Locks.TryGetValue(key, out ulong holder) || holder == sender || !NetSession.Clients.ContainsKey(holder);
                if (free) s_Locks[key] = sender;
                if (!free) using (var o = new NetOut(64)) { o.S(key); NetSession.SendTo(sender, "cz.locked", o); }
            }
            else if (s_Locks.TryGetValue(key, out ulong h) && h == sender) s_Locks.Remove(key);
        }

        private static void OnLocked(ulong s, NetIn m)
        {
            m.S();
            GameEvents.RaiseToast("Someone else is going through that.");
            var ui = Object.FindAnyObjectByType<InventoryUIController>();
            if (ui != null && ui.IsOpen) ui.Close();
        }

        private static void SendContents(LootContainer c)
        {
            List<StackSave> items = ProfileStore.ToSaves(c.Contents);
            using (var o = new NetOut(512, 1 << 16))
            {
                o.S(KeyOf(c)).B(true);
                NetCodec.Write(o, items);
                NetSession.ToServer("cz.cont", o, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        /// <summary>Server: a container's contents changed without anyone opening it (the Central Economy restocked it).</summary>
        public static void BroadcastContents(LootContainer c)
        {
            if (!NetSession.Active || c == null || !NetSession.NM.IsServer) return;
            using (var o = new NetOut(512, 1 << 16))
            {
                o.S(KeyOf(c)).B(c.Searched);
                NetCodec.Write(o, ProfileStore.ToSaves(c.Contents));
                NetSession.ToClients("cz.cont", o, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private static void OnContents(ulong sender, NetIn m)
        {
            string key = m.S();
            bool searched = m.B();
            List<StackSave> items = NetCodec.ReadSaves(m);
            if (NetSession.NM.IsServer)
                using (var o = new NetOut(512, 1 << 16))
                {
                    o.S(key).B(searched);
                    NetCodec.Write(o, items);
                    NetSession.ToClients("cz.cont", o, NetworkDelivery.ReliableFragmentedSequenced, sender);
                }
            if (sender == NetSession.NM.LocalClientId) return;
            LootContainer c = Find(key);
            if (c != null) c.RestoreContents(searched, items);
        }

        private static void SendGround(bool drop, ItemDef def, int n, Vector3 at, ItemExtra extra)
        {
            if (!NetSession.Active || def == null) return;
            using (var o = new NetOut(96)) { o.B(drop).S(def.Id).I(n).V(at).S(extra != null ? ItemExtra.Serialize(extra) : ""); NetSession.ToServer("cz.ground", o); }
        }

        /// <summary>Server: a ground item vanished on its own (the Central Economy's lifetime) — gone for everyone.</summary>
        public static void GroundGone(ItemDef def, int n, Vector3 at) => SendGround(false, def, n, at, null);

        private static void OnGround(ulong sender, NetIn m)
        {
            bool drop = m.B();
            string id = m.S();
            int n = m.I();
            Vector3 at = m.V();
            string extra = m.S();
            if (NetSession.NM.IsServer)
                using (var o = new NetOut(96)) { o.B(drop).S(id).I(n).V(at).S(extra); NetSession.ToClients("cz.ground", o, NetworkDelivery.ReliableSequenced, sender); }
            if (sender == NetSession.NM.LocalClientId) return;
            ItemDef def = ItemDatabase.Get(id);
            if (def == null) return;
            if (drop) WorldItem.Spawn(null, at, def, n, extra: ItemExtra.Parse(extra));
            else
            {
                var near = new List<WorldItem>();
                WorldItem.Near(at, 0.8f, near);
                foreach (WorldItem w in near) if (w.Def == def) { w.Take(Mathf.Min(n, w.Count)); break; }
            }
        }

        private static void SendFixture(Interactable f)
        {
            if (!NetSession.Active || f == null || string.IsNullOrEmpty(f.SaveKey)) return;
            var save = new FixtureSave { Key = f.SaveKey };
            f.CaptureState(save);
            using (var o = new NetOut(256)) { o.S(f.SaveKey).S(JsonUtility.ToJson(save)); NetSession.ToServer("cz.fix", o); }
        }

        private static void OnFixture(ulong sender, NetIn m)
        {
            string key = m.S(), json = m.S();
            if (NetSession.NM.IsServer)
                using (var o = new NetOut(256)) { o.S(key).S(json); NetSession.ToClients("cz.fix", o, NetworkDelivery.ReliableSequenced, sender); }
            if (sender == NetSession.NM.LocalClientId) return;
            FixtureSave save = JsonUtility.FromJson<FixtureSave>(json);
            foreach (Interactable f in Interactable.All) if (f != null && f.SaveKey == key) { f.RestoreState(save); break; }
        }

        /// <summary>Client: ask the server to let us raise our flag over an outpost (it checks the garrison and the lock).</summary>
        public static void RequestOutpost(string id, string faction)
        {
            using (var o = new NetOut(96)) { o.S(id).S(faction); NetSession.ToServer("cz.outcap", o); }
        }

        /// <summary>Server: an outpost changed hands — tell every client (the taker hears it as theirs).</summary>
        public static void OutpostTaken(Outpost site, ulong taker)
        {
            if (!NetSession.Active || site == null) return;
            using (var o = new NetOut(96)) { o.S(site.Id).S(site.Owner).UL((ulong)site.LockedUntil).B(true).UL(taker); NetSession.ToClients("cz.outpost", o); }
        }

        private static void OnOutpostCapture(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            string id = m.S(), faction = m.S();
            foreach (Outpost op in OutpostSystem.Sites)
            {
                if (op == null || op.Id != id) continue;
                if (!op.CanBeTaken(out string why))
                {
                    using (var o = new NetOut(96)) { o.S(why); NetSession.SendTo(sender, "cz.outno", o); }
                    return;
                }
                op.Taken(faction, ProfileStore.NowUnix + (long)OutpostSystem.LockSeconds, false, NetSocial.NameOf(sender));
                OutpostTaken(op, sender);
                return;
            }
        }

        // A client entering the raid asks how the outposts stand; the server answers with each one.
        private static void OnOutpostQuery(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            foreach (Outpost op in OutpostSystem.Sites)
                if (op != null)
                    using (var o = new NetOut(96)) { o.S(op.Id).S(op.Owner).UL((ulong)op.LockedUntil).B(false).UL(ulong.MaxValue); NetSession.SendTo(sender, "cz.outpost", o); }
        }

        private static void OnOutpost(ulong sender, NetIn m)
        {
            if (NetSession.NM.IsServer) return;
            string id = m.S(), owner = m.S();
            long until = (long)m.UL();
            bool fresh = m.B();
            ulong taker = m.UL();
            foreach (Outpost op in OutpostSystem.Sites)
            {
                if (op == null || op.Id != id) continue;
                if (fresh) op.Taken(owner, until, taker == NetSession.NM.LocalClientId, NetSocial.NameOf(taker));
                else op.Show(owner, until);
            }
        }
        #endregion
    }

    /// <summary>
    /// Something the server's AI owns, seen on a client: a body interpolated between snapshots (a dead one from the
    /// zombie pool, a person in their faction's kit, a beast), with a hit capsule so local shots draw right — the
    /// damage itself is the server's to decide.
    /// </summary>
    public sealed class NetAgentProxy : MonoBehaviour, IDamageable
    {
        public Actor Actor { get; private set; }
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        private Vector3 _from, _to;
        private float _yawFrom, _yawTo, _t, _interval = 0.1f, _last;
        private HumanoidAnimator _anim;
        private CreatureBody _beast;
        private Vector3 _prev;
        private byte _flags;

        public static NetAgentProxy Spawn(uint id, byte code, ActorKind kind, string sub, string faction, string name, int seed)
        {
            var go = new GameObject("Proxy " + name);
            var actor = go.AddComponent<Actor>();
            actor.Kind = kind;
            actor.Faction = faction;
            actor.DisplayName = name;
            actor.NetId = id;
            var p = go.AddComponent<NetAgentProxy>();
            p.Actor = actor;
            var rng = new System.Random(seed);
            if (code == 2)
            {
                CreatureDef def = CreatureCatalog.Get(sub);
                if (def != null)
                {
                    actor.Height = def.Height;
                    p._beast = go.AddComponent<CreatureBody>();
                    p._beast.Build(def);
                    actor.Head = p._beast.Head;
                }
            }
            else
            {
                HumanoidLook look;
                if (code == 0) { look = HumanoidLook.RandomZombie(rng); look.SkinColor = new Color(0.36f, 0.5f, 0.30f); }
                else
                {
                    look = HumanoidLook.Survivor();
                    look.SkinColor = HumanLoadouts.SkinTones[rng.Next(HumanLoadouts.SkinTones.Length)];
                    look.TopColor = Color.Lerp(look.TopColor, Factions.Get(faction).Color, 0.45f);
                }
                HumanoidRig rig = HumanoidRig.Create(go.transform, look);
                p._anim = rig.gameObject.AddComponent<HumanoidAnimator>();
                p._anim.AutoUpdate = false;
                p._anim.Gait = code == 0 ? HumanoidGait.Shambler : HumanoidGait.Survivor;
                actor.Head = rig.Bone(HumanBone.Head);
                if (code == 1)
                {
                    string[] parts = sub.Split('|');
                    ItemDef gun = parts.Length > 1 ? ItemDatabase.Get(parts[1]) : null;
                    if (gun != null) rig.SetHeldItem(gun, HoldStyle.Firearm);
                }
            }
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, actor.Height * 0.5f, 0f);
            col.height = actor.Height;
            col.radius = 0.35f;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CloneZConfig.IgnoreRaycastLayer;
            go.AddComponent<ActorFog>();
            return p;
        }

        public void Push(Vector3 pos, float yaw, byte flags)
        {
            if (_last > 0f) _interval = Mathf.Clamp(Time.time - _last, 0.05f, 0.3f);
            _last = Time.time;
            _from = transform.position == Vector3.zero ? pos : transform.position;
            _to = pos;
            _yawFrom = transform.eulerAngles.y;
            _yawTo = yaw;
            _t = 0f;
            _flags = flags;
            Actor.Surrendered = (flags & 32) != 0;
            Actor.Cloaked = (flags & 64) != 0;
            bool dead = (flags & 1) != 0;
            if (dead && !IsDead)
            {
                IsDead = true;
                if (_anim != null) _anim.Die(-transform.forward);
                if (_beast != null) _beast.Die(-transform.forward);
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }

        private void Update()
        {
            _t += Time.deltaTime / _interval;
            transform.position = Vector3.Lerp(_from, _to, Mathf.Clamp01(_t));
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(_yawFrom, _yawTo, Mathf.Clamp01(_t)), 0f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 v = (transform.position - _prev) / dt;
            _prev = transform.position;
            if (_beast != null) { _beast.ExternalSpeed01 = Mathf.Clamp01(v.magnitude / 6f); _beast.ExternalAttack = (_flags & 16) != 0; _beast.ExternalCloak = Actor.Cloaked; }
            if (_anim == null) return;
            _anim.Velocity = IsDead ? Vector3.zero : v;
            _anim.Hunting = (_flags & 2) != 0;
            _anim.ArmsOut = _anim.Hunting;
            _anim.Sneaking = (_flags & 4) != 0;
            _anim.Aiming = (_flags & 8) != 0;
            _anim.Evaluate(dt);
        }

        // A client's melee on a proxy goes to the server, which decides.
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            NetPlayers.RequestHit(Actor.NetId, info);
        }
    }
}
