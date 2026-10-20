using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Warfare outposts (Gray Zone Warfare's PvEvP): sandbagged positions held by a faction's garrison. Clear the
    /// garrison and raise your faction's flag to take one; a freshly taken outpost can't be retaken for 30 minutes.
    /// Holding one pays your faction in reputation, keeps a supply crate stocked and gives your side a safe spot in
    /// the Zone. Ownership is remembered between raids (and shared by everyone on a server).
    /// </summary>
    public static class OutpostSystem
    {
        public const float LockSeconds = 30f * 60f;
        private static readonly List<Outpost> s_Sites = new List<Outpost>();

        /// <summary>Read‑only collection of all outpost sites.</summary>
        public static IReadOnlyList<Outpost> Sites => s_Sites;

        /// <summary>Builds the sandbag positions on the published outpost points (before the navmesh bake, so the
        /// sandbags are cover the AI knows about).</summary>
        public static void BuildSites(Transform world, ZoneDef zone, System.Random rng)
        {
            s_Sites.Clear();
            if (zone == null) return;
            IReadOnlyList<Vector3> pts = WorldMarkers.Get(WorldMarkers.Outpost);
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 c = pts[i];
                var root = new GameObject("Outpost " + i).transform;
                root.SetParent(world, false);
                root.position = c;
                var m = new RaidMesh();
                m.SetFrame(c, ZoneKit.R(rng, 0f, 360f));
                Color sand = new Color(0.52f, 0.47f, 0.36f);
                for (int s = 0; s < 14; s++)
                {
                    if (s == 3 || s == 10) continue;                  // two gaps to walk through
                    float a = s / 14f * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(a) * 5f, 0f, Mathf.Sin(a) * 5f);
                    float yaw = -a * Mathf.Rad2Deg + 90f;
                    for (int layer = 0; layer < 3; layer++)
                        m.Box(p + Vector3.up * (0.18f + layer * 0.3f), new Vector3(2.3f, 0.32f, 0.6f), Quaternion.Euler(0f, yaw + layer * 4f, 0f),
                            ZoneKit.M(ZoneKit.V(sand, rng, 0.05f)));
                    ZoneKit.Solid(root, c + Quaternion.Euler(0f, m.FrameRotation.eulerAngles.y, 0f) * (p + Vector3.up * 0.5f), new Vector3(2.3f, 1f, 0.6f),
                        Quaternion.Euler(0f, m.FrameRotation.eulerAngles.y + yaw, 0f));
                }
                m.Cylinder(new Vector3(0f, 0f, 0f), 0.05f, 6f, 6, ZoneKit.Mt(ZoneKit.Galvanised));
                m.Box(new Vector3(-2f, 0.45f, 1.5f), new Vector3(1.2f, 0.9f, 0.8f), ZoneKit.M(ZoneKit.WardenOlive));
                ZoneKit.Emit(m, "Sandbags", root, c);
                var o = new GameObject("Flag").AddComponent<Outpost>();
                o.transform.SetParent(root, false);
                o.transform.position = c;
                o.Init(zone.Id + ":" + i, c);
                s_Sites.Add(o);
            }
        }

        /// <summary>Initialises outpost garrisons for the given zone and random seed.</summary>
        public static void Begin(ZoneDef zone, int seed)
        {
            var rng = new System.Random(seed ^ 0x0B57A);
            AccountProfile acct = ProfileStore.Account;
            for (int i = 0; i < s_Sites.Count; i++)
            {
                Outpost o = s_Sites[i];
                if (o == null) continue;
                OutpostState st = State(acct, o.Id, zone.Id, i % 2 == 0 ? Factions.Vanguard : Factions.Scavs);
                o.Load(st);
                o.SpawnGarrison(rng);
            }
        }

        /// <summary>Creates or retrieves the persistent state for an outpost.</summary>
        public static OutpostState State(AccountProfile acct, string id, string zone, string defaultOwner)
        {
            if (acct == null) return new OutpostState { Id = id, ZoneId = zone, Owner = defaultOwner };
            OutpostState st = acct.Outposts.Find(x => x.Id == id);
            if (st == null)
            {
                st = new OutpostState { Id = id, ZoneId = zone, Owner = defaultOwner };
                acct.Outposts.Add(st);
            }
            return st;
        }
    }

    public sealed class Outpost : Interactable
    {
        /// <summary>Unique identifier for this outpost instance.</summary>
        public string Id { get; private set; }
        /// <summary>Current owning faction, or Vanguard if uninitialized.</summary>
        public string Owner => _state != null ? _state.Owner : Factions.Vanguard;
        /// <summary>Whether the outpost is currently locked against capture.</summary>
        public bool Locked => _state != null && _state.LockedUntilUnix > ProfileStore.NowUnix;

        private OutpostState _state;
        private readonly List<HumanNpc> _garrison = new List<HumanNpc>();
        private Renderer _flag;
        private Vector3 _centre;

        /// <summary>Display name shown to the player, indicating owner and lock status.</summary>
        public override string DisplayName => "Outpost — " + Factions.NameOf(Owner) + (Locked ? " (secured)" : "");
        /// <summary>Interaction distance for the outpost.</summary>
        public override float Reach => 3f;
        /// <summary>World position a player should stand at to interact with the outpost.</summary>
        public override Vector3 InteractPoint => _centre + Vector3.up * 1.2f;

        /// <summary>Initialises the outpost's identifier and visual flag.</summary>
        public void Init(string id, Vector3 centre)
        {
            Id = id;
            _centre = centre;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(pole.GetComponent<Collider>());
            pole.name = "Banner";
            pole.transform.SetParent(transform, false);
            pole.transform.position = centre + new Vector3(0.65f, 5.3f, 0f);
            pole.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            _flag = pole.GetComponent<Renderer>();
            _flag.sharedMaterial = CloneZUtil.Mat(Color.grey, 0.1f);
            _flag.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Loads persisted state into the outpost and updates its appearance.</summary>
        public void Load(OutpostState st)
        {
            _state = st;
            Recolour();
        }

        private void Recolour()
        {
            if (_flag == null) return;
            var block = new MaterialPropertyBlock();
            Color c = Factions.Get(Owner).Color;
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            _flag.SetPropertyBlock(block);
        }

        /// <summary>Spawns a garrison of NPCs defending the outpost.</summary>
        public void SpawnGarrison(System.Random rng)
        {
            Actor me = Actor.LocalPlayer;
            bool ours = me != null && Owner == me.Faction;
            int n = ours ? 2 : 3;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = _centre + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 2.5f;
                HumanNpc g = HumanNpc.Spawn(p, Owner, HumanLoadouts.DefaultIdFor(Owner, rng), rng, "outpost_" + Id);
                g.SetGuard(g.transform.position);
                _garrison.Add(g);
            }
        }

        private bool GarrisonStanding()
        {
            for (int i = 0; i < _garrison.Count; i++)
            {
                HumanNpc g = _garrison[i];
                if (g != null && !g.IsDead && !g.Actor.Surrendered && !g.Actor.Restrained) return true;
            }
            return false;
        }

        /// <summary>Adds interaction actions for the player based on ownership and capture conditions.</summary>
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            Actor me = Actor.LocalPlayer;
            if (me == null) return;
            if (Owner == me.Faction)
            {
                into.Add(new InteractAction("Held by " + Factions.NameOf(Owner), null).DisabledIf(true, "Your side holds it"));
                // Owner can voluntarily abandon the outpost, reverting it to neutral (Vanguard) ownership.
                into.Add(new InteractAction("Abandon outpost", Abandon).Timed(3f, "Tearing down the flag…"));
                return;
            }
            CanBeTaken(out string reason);
            into.Add(new InteractAction("Raise the " + Factions.NameOf(me.Faction) + " flag", Capture)
                .Timed(8f, "Raising the flag...").DisabledIf(reason != null, reason));
        }

        /// <summary>Unix timestamp until which the outpost remains locked.</summary>
        public long LockedUntil => _state != null ? _state.LockedUntilUnix : 0;

        /// <summary>False (with why) while the outpost is secured or its garrison still stands (the server's view).</summary>
        public bool CanBeTaken(out string why)
        {
            long left = LockedUntil - ProfileStore.NowUnix;
            why = left > 0 ? "Secured for another " + Mathf.CeilToInt(left / 60f) + " min" : GarrisonStanding() ? "The garrison still holds it" : null;
            return why == null;
        }

        private void Abandon(InteractContext ctx)
        {
            // Reset ownership to neutral (Vanguard) and clear lock timer.
            if (_state == null) _state = OutpostSystem.State(ProfileStore.Account, Id, GameSession.ZoneId, Owner);
            _state.Owner = Factions.Vanguard;
            _state.LockedUntilUnix = 0;
            Recolour();
            GameEvents.RaiseToast("Outpost abandoned.");
            ProfileStore.Save();
        }

        private void Capture(InteractContext ctx)
        {
            Actor me = Actor.LocalPlayer;
if (me == null || !CanBeTaken(out _)) return;
            // Multiplayer: request server to capture.
            if (GameSession.Networked && !NetAuthority.IsServer) { NetWorld.RequestOutpost(Id, me.Faction); return; }
Taken(me.Faction, ProfileStore.NowUnix + (long)OutpostSystem.LockSeconds, true, null);
            if (GameSession.Networked) NetWorld.OutpostTaken(this, NetAuthority.LocalClientId);
        }

        /// <summary>The outpost changes hands (on every peer): the flag, the 30-minute lock, the supply crate; the taker
        /// gets the standing and the job progress.</summary>
        public void Taken(string faction, long lockedUntil, bool byMe, string takerName)
        {
            if (_state == null) _state = OutpostSystem.State(ProfileStore.Account, Id, GameSession.ZoneId, Owner);
            string old = _state.Owner;
            _state.Owner = faction;
            _state.LockedUntilUnix = lockedUntil;
            Recolour();
            if (byMe)
            {
                Factions.AdjustReputation(faction, 80f, "took an outpost");
                if (old != Factions.Scavs) Factions.AdjustReputation(old, -60f, "took their outpost");
                GameEvents.RaiseToast("Outpost taken for " + Factions.NameOf(faction) + ". It can't be retaken for 30 minutes.");
                GameEvents.RaiseQuestProgress("capture_outpost", 1f);
            }
            else GameEvents.RaiseToast((takerName ?? "Someone") + " took an outpost for " + Factions.NameOf(faction) + ".");
            // Seeded by the site and the lock time, so every peer finds the same crate in the same place.
            LootContainer crate = LootContainer.Spawn(transform.parent, ContainerType.GunLocker, _centre + new Vector3(-2f, 0f, -1.2f), HumanUtil.StableHash(Id) ^ (int)(lockedUntil & 0x7FFFFFFF));
            if (crate != null) crate.DisplayName = "Outpost supply crate";
            ProfileStore.Save();
        }

        /// <summary>A client's view of an outpost it didn't see change hands (joining a raid already under way).</summary>
        public void Show(string owner, long lockedUntil)
        {
            if (_state == null) _state = new OutpostState { Id = Id, ZoneId = GameSession.ZoneId, Owner = owner };
            _state.Owner = owner;
            _state.LockedUntilUnix = lockedUntil;
            Recolour();
        }
    }

    /// <summary>
    /// Information as loot: a dead stalker's PDA can hold the location of their stash. Reading one adds the cache to
    /// your character's notes; the next raid into that Zone, it's there to dig up. You can bury your own stash too
    /// (with a trowel): what you leave in it waits for you between raids — until someone else digs it up.
    /// </summary>
    public static class PdaStashes
    {
        internal static void RegisterItems()
        {
            ItemActions.RegisterUse("pda", (inv, stack) => { Read(stack); return true; });
            ItemActions.RegisterMenu((inv, stack, into) =>
            {
                if (stack.Def == null || stack.Def.Id != "trowel" || !GameSession.IsExtraction || GameSession.IsBorrowedBody) return;
                into.Add(new ItemMenuAction("Bury a stash here", () => BuryHere(inv)));
            });
        }

        private static void Read(ItemStack stack)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            string note = stack.Extra != null ? stack.Extra.Note : null;
            if (c == null || string.IsNullOrEmpty(note) || !note.StartsWith("pda:"))
            {
                GameEvents.RaiseSubtitle("PDA", "Battery's almost dead. Nothing but static and a half-written message to someone called Mira.");
                return;
            }
            int seed = int.TryParse(note.Substring(4), out int s) ? s : 1;
            var rng = new System.Random(seed);
            ZoneDef zone = rng.NextDouble() < 0.6 ? ZoneCatalog.Current : ZoneCatalog.All[rng.Next(ZoneCatalog.All.Count)];
            float half = 60f;
            var pos = new Vector3((float)(rng.NextDouble() * 2 - 1) * half, 0f, (float)(rng.NextDouble() * 2 - 1) * half);
            string where = zone == ZoneCatalog.Current && GameSession.IsExtraction ? HumanUtil.DescribeLocation(pos, zone.Name) : HumanUtil.DescribeRegion(pos, zone.Name);
            var stash = new BuriedStash { Id = "pda" + seed, ZoneId = zone.Id, Position = pos, FromPda = true, Hint = where };
            if (c.Stashes.Exists(x => x.Id == stash.Id)) { GameEvents.RaiseToast("You've already noted this stash: " + where); return; }
            var inv = new Inventory(999f);
            ItemDatabase.RollLoot(ContainerType.GunLocker, rng, inv);
            ItemDatabase.RollLoot(ContainerType.MedCabinet, rng, inv);
            ItemDatabase.RollLoot(ContainerType.Desk, rng, inv);
            ItemDef cash = ItemDatabase.Get(Money.Id);
            if (cash != null) inv.Add(cash, 40 + rng.Next(160), false);
            stash.Items = ProfileStore.ToSaves(inv);
            c.Stashes.Add(stash);
            stack.Extra.Note = "read";
            GameEvents.RaiseSubtitle("PDA", "Last entry: \"Stashed the good stuff " + where + ". If I don't make it back, it's yours, whoever you are.\"");
            GameEvents.RaiseToast("Stash noted: " + where);
            ProfileStore.Save();
        }

        private static void BuryHere(PlayerInventory inv)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || inv == null) return;
            Vector3 p = inv.transform.position;
            var stash = new BuriedStash { Id = "own" + ProfileStore.NowUnix, ZoneId = GameSession.ZoneId, Position = p, Hint = HumanUtil.DescribeLocation(p, ZoneCatalog.Current.Name) };
            c.Stashes.Add(stash);
            StashCache.Spawn(stash, true);
            GameEvents.RaiseToast("You dig a hole and line it. Put what you want to leave inside.");
        }

        /// <summary>The unopened stashes your character knows about in this Zone appear as diggable mounds.</summary>
        public static void SpawnForZone(ZoneDef zone)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || zone == null || GameSession.IsBorrowedBody) return;
            foreach (BuriedStash s in c.Stashes)
                if (s.ZoneId == zone.Id && !s.Opened) StashCache.Spawn(s, false);
        }
    }

    /// <summary>A buried cache: a mound and a lid; dig it up to open it. Player-made caches keep what is left inside.</summary>
    public sealed class StashCache : Interactable
    {
        private BuriedStash _data;
        private LootContainer _box;
        private bool _dug;

        public override string DisplayName => _dug ? "Stash" : "Disturbed ground";
        public override float Reach => 2.2f;

        public static StashCache Spawn(BuriedStash data, bool open)
        {
            Vector3 p = data.Position;
            if (Physics.Raycast(new Vector3(p.x, 20f, p.z), Vector3.down, out RaycastHit hit, 40f, 1, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            var go = new GameObject("Stash " + data.Id);
            go.transform.position = p;
            var m = new RaidMesh();
            m.SetFrame(p, 0f);
            m.Ellipsoid(new Vector3(0f, 0.05f, 0f), new Vector3(1.2f, 0.25f, 0.9f), ZoneKit.M(new Color(0.3f, 0.25f, 0.18f)), 8, 4);
            ZoneKit.Emit(m, "Mound", go.transform, p, ShadowCastingMode.Off);
            var s = go.AddComponent<StashCache>();
            s._data = data;
            if (open) s.Open();
            GameEvents.RaidEnded += s.OnRaidEnded;
            return s;
        }

        private void OnDestroy() => GameEvents.RaidEnded -= OnRaidEnded;

        public override bool IsAvailable => isActiveAndEnabled && !_dug;

        /// <summary>Adds interaction actions for the player based on ownership and capture conditions.</summary>
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool trowel = ctx.Inventory != null && ctx.Inventory.Has("trowel");
            into.Add(new InteractAction("Dig it up", c => Open()).Timed(trowel ? 3f : 9f, trowel ? "Digging..." : "Digging with your hands..."));
        }

        private void Open()
        {
            if (_dug) return;
            _dug = true;
            _box = gameObject.AddComponent<LootContainer>();
            _box.DisplayName = "Stash";
            _box.Type = ContainerType.Crate;
            _box.Contents = new Inventory(999f);
            foreach (StackSave st in _data.Items) ProfileStore.AddSave(_box.Contents, st);
            _box.Searched = true;
            if (_data.FromPda) _data.Opened = true;
        }

        // A player's own cache keeps whatever is in it when the raid ends.
        private void OnRaidEnded(RaidOutcome o, string exit)
        {
            if (_box == null || _data == null || _data.FromPda) return;
            _data.Items = ProfileStore.ToSaves(_box.Contents);
            if (_data.Items.Count == 0) ProfileStore.ActiveCharacter?.Stashes.Remove(_data);
        }
    }
}
