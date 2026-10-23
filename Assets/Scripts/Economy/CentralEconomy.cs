using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One loot rule: how many of a thing the Zone holds (nominal), the floor restocks refill to (min), how
    /// long one left on the ground lasts (lifetime, seconds) and how often a shortfall is topped up (restock, seconds).
    /// Negative fields fall back to the item's category.</summary>
    [Serializable]
    public class EconomyRule
    {
        public string Id;
        public string Category;
        public int Nominal = -1;
        public int Min = -1;
        public float Lifetime = -1f;
        public float Restock = -1f;
    }

    /// <summary>The whole economy file (economy.json beside the save): edit it, press Reload in the sandbox rules.</summary>
    [Serializable]
    public class EconomyConfig
    {
        public int Version = 1;
        public string Note = "DayZ-style Central Economy. Nominal = how many stacks of a thing a raid's Zone may hold; Min = the floor restocks refill to; " +
            "Lifetime = seconds a dropped item lasts on the ground; Restock = seconds between top-ups. Item rules override their category's. Counts scale with each Zone's loot multiplier.";
        public float CheckSeconds = 90f;
        public int RestockBatch = 6;
        public float RestockMinDistance = 45f;
        public List<EconomyRule> Categories = new List<EconomyRule>();
        public List<EconomyRule> Items = new List<EconomyRule>();
    }

    /// <summary>
    /// DayZ's Central Economy (the doc: "a data-driven loot dial you can tune without patching code — worth copying
    /// almost exactly"). A raid's loot is rolled from the containers' tables as before, but every roll asks the
    /// economy first: once the Zone holds the nominal number of a thing, no more appear. In long raids (the open
    /// Zone) the server counts what's left in containers every minute and a half, and when a thing has fallen below
    /// its minimum, re-rolls emptied containers far from anyone until it's back. Things dropped on the ground last
    /// their lifetime, then the Zone takes them. All of it lives in economy.json.
    /// </summary>
    public static class CentralEconomy
    {
        /// <summary>The loaded economy configuration, populated from economy.json.</summary>
        public static EconomyConfig Config { get; private set; }
        /// <summary>The full path to the economy.json file in the persistent data folder.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, "economy.json");

        private static readonly Dictionary<string, EconomyRule> s_Items = new Dictionary<string, EconomyRule>();
        private static readonly List<Actor> s_PlayerCache = new List<Actor>();
        private static readonly Dictionary<string, EconomyRule> s_Categories = new Dictionary<string, EconomyRule>();
        private static readonly Dictionary<string, int> s_Counts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> s_RestockedAt = new Dictionary<string, float>();

        private struct Dropped { public string Id; public Vector3 At; public float Expires; }
        private static readonly List<Dropped> s_Dropped = new List<Dropped>();
        // Cached collections for Tick to reduce allocations
        private static readonly List<string> s_ShortOf = new List<string>();
        private static readonly List<LootContainer> s_Candidates = new List<LootContainer>();
        private static readonly System.Random s_Rng = new System.Random(Environment.TickCount);

        /// <summary>The loot gate runs on every peer (they all roll the same seeded loot, so it must agree); restocks
        /// and expiry are the server's.</summary>
        public static bool GateOn => SandboxSettings.Current.CentralEconomy && GameSession.IsExtraction;
        /// <summary>Whether the central economy is active on the server (GateOn and this instance is the server).</summary>
        public static bool Active => GateOn && NetAuthority.IsServer;

        /// <summary>Playing together, the host's economy is everyone's (the gate must agree on every machine).</summary>
        public static void Adopt(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                EconomyConfig cfg = JsonUtility.FromJson<EconomyConfig>(json);
                if (cfg != null && cfg.Categories != null && cfg.Categories.Count > 0) Apply(cfg);
            }
            catch (Exception e) { Debug.LogWarning("[CloneZ] The host's economy didn't parse: " + e.Message); }
        }

        /// <summary>Serialize the current economy config to JSON, or return empty if none.</summary>
        public static string ToJson() => Config != null ? JsonUtility.ToJson(Config) : "";

        #region The file
        /// <summary>Load the economy configuration from disk (or defaults) and apply it.</summary>
        public static void Load()
        {
            EconomyConfig cfg = null;
            try
            {
                if (File.Exists(FilePath)) cfg = JsonUtility.FromJson<EconomyConfig>(File.ReadAllText(FilePath));
            }
            catch (Exception e) { Debug.LogWarning("[CloneZ] economy.json unreadable (" + e.Message + "); using defaults."); }
            if (cfg == null || cfg.Categories == null || cfg.Categories.Count == 0)
            {
                cfg = Defaults();
                try { File.WriteAllText(FilePath, JsonUtility.ToJson(cfg, true)); }
                catch (Exception e) { Debug.LogWarning("[CloneZ] Could not write economy.json: " + e.Message); }
            }
            Apply(cfg);
        }

        private static void Apply(EconomyConfig cfg)
        {
            Config = cfg;
            s_Items.Clear();
            s_Categories.Clear();
            foreach (EconomyRule r in cfg.Categories) if (r != null && !string.IsNullOrEmpty(r.Category)) s_Categories[r.Category] = r;
            if (cfg.Items != null) foreach (EconomyRule r in cfg.Items) if (r != null && !string.IsNullOrEmpty(r.Id)) s_Items[r.Id] = r;
        }

        private static EconomyConfig Defaults()
        {
            var c = new EconomyConfig();
            void Cat(ItemCategory cat, int nominal, int min, float life, float restock) =>
                c.Categories.Add(new EconomyRule { Category = cat.ToString(), Nominal = nominal, Min = min, Lifetime = life, Restock = restock });
            void It(string id, int nominal, int min, float life = -1f, float restock = -1f) =>
                c.Items.Add(new EconomyRule { Id = id, Nominal = nominal, Min = min, Lifetime = life, Restock = restock });
            Cat(ItemCategory.Food, 160, 40, 3600f, 600f);
            Cat(ItemCategory.Drink, 100, 25, 3600f, 600f);
            Cat(ItemCategory.Medical, 70, 15, 7200f, 900f);
            Cat(ItemCategory.Weapon, 30, 6, 7200f, 1200f);
            Cat(ItemCategory.Firearm, 14, 3, 14400f, 1800f);
            Cat(ItemCategory.Ammo, 80, 20, 7200f, 900f);
            Cat(ItemCategory.Material, 180, 40, 3600f, 600f);
            Cat(ItemCategory.Clothing, 80, 15, 7200f, 1200f);
            Cat(ItemCategory.Misc, 140, 30, 3600f, 600f);
            It("grenade", 5, 1); It("breachcharge", 2, 0); It("drone_fpv", 1, 0); It("drone_ground", 1, 0); It("tablet", 2, 0);
            It("carwheel", 8, 2); It("carbattery", 3, 1); It("sparkplug", 5, 2); It("radiator", 3, 1); It("gascan", 4, 1);
            It("handcuffs", 4, 1); It("combolock", 4, 1); It("radio_walkie", 4, 1); It("gps", 2, 0); It("bloodbag_o_neg", 1, 0);
            It("ivkit", 4, 1); It("surgicalkit", 2, 0); It("antirad", 6, 2); It("gasmask_filter", 8, 2);
            return c;
        }

        /// <summary>Get the rule for an item ID, falling back to its category or a blank rule.</summary>
        public static EconomyRule RuleFor(string id)
        {
            if (Config == null) Load();
            s_Items.TryGetValue(id, out EconomyRule own);
            ItemDef def = ItemDatabase.Get(id);
            EconomyRule cat = null;
            if (def != null) s_Categories.TryGetValue(def.Category.ToString(), out cat);
            if (own == null) return cat ?? new EconomyRule { Id = id };
            if (cat == null) return own;
            return new EconomyRule
            {
                Id = id,
                Nominal = own.Nominal >= 0 ? own.Nominal : cat.Nominal,
                Min = own.Min >= 0 ? own.Min : cat.Min,
                Lifetime = own.Lifetime >= 0f ? own.Lifetime : cat.Lifetime,
                Restock = own.Restock >= 0f ? own.Restock : cat.Restock,
            };
        }

        private static float Scale => GameSession.IsExtraction ? Mathf.Max(0.2f, ZoneCatalog.Current.LootMultiplier * SandboxSettings.Current.LootAbundance) : 1f;
        /// <summary>Calculate the nominal max count for an item ID, scaling with loot multipliers.</summary>
        public static int NominalOf(string id) { EconomyRule r = RuleFor(id); return r.Nominal < 0 ? int.MaxValue : Mathf.CeilToInt(r.Nominal * Scale); }
        /// <summary>Calculate the minimum required count for an item ID, scaling with loot multipliers.</summary>
        public static int MinOf(string id) { EconomyRule r = RuleFor(id); return r.Min < 0 ? 0 : Mathf.CeilToInt(r.Min * Scale); }
        /// <summary>Get the current tracked count for an item ID.</summary>
        public static int Count(string id) => s_Counts.TryGetValue(id, out int n) ? n : 0;
        #endregion

        #region Hooks
        internal static void Install()
        {
            Load();
            ItemDatabase.LootGate = id => !GateOn || Count(id) < NominalOf(id);
            ItemDatabase.LootRolled = (id, n) => { if (GateOn) s_Counts[id] = Count(id) + 1; };
            PlayerInventory.GroundDropped += (def, n, at, extra) =>
            {
                if (!SandboxSettings.Current.CentralEconomy || !GameSession.IsExtraction || def == null) return;
                EconomyRule r = RuleFor(def.Id);
                float life = r.Lifetime > 0f ? r.Lifetime : 3600f;
                s_Dropped.Add(new Dropped { Id = def.Id, At = at, Expires = Time.time + life });
            };
            // Loot is rolled while the map is built, so the tally restarts when a raid ends (not when the next begins).
            GameEvents.RaidEnded += (o, exit) => { s_Counts.Clear(); s_RestockedAt.Clear(); s_Dropped.Clear(); };
            WorldHooks.RaidStarted += (w, m, z) =>
            {
                if (UnityEngine.Object.FindAnyObjectByType<EconomyTicker>() == null) new GameObject("[CentralEconomy]").AddComponent<EconomyTicker>();
            };
        }

        /// <summary>Counts what the Zone's containers hold right now (stacks per item).</summary>
        public static void Recount()
        {
            s_Counts.Clear();
            foreach (LootContainer c in LootContainer.All)
            {
                if (c == null || c.Contents == null || c.IsSack || c.Type == ContainerType.Corpse) continue;
                foreach (ItemStack s in c.Contents.Stacks)
                    if (s != null && s.Def != null) s_Counts[s.Def.Id] = Count(s.Def.Id) + 1;
            }
        }

        // Server, every CheckSeconds: what's short of its minimum (and due a restock) gets emptied containers re-rolled.
        internal static void Tick()
        {
            if (!Active || Config == null) return;
            // Update cached player list for proximity checks
            s_PlayerCache.Clear();
            foreach (Actor a in Actor.All)
                if (a != null && a.IsPlayerControlled)
                    s_PlayerCache.Add(a);
            Recount();
            s_ShortOf.Clear();
            var shortOf = s_ShortOf;
            foreach (string id in ItemDatabase.AllIds)
            {
                int min = MinOf(id);
                if (min <= 0 || Count(id) >= min) continue;
                float every = RuleFor(id).Restock;
                if (every > 0f && s_RestockedAt.TryGetValue(id, out float at) && Time.time - at < every) continue;
                shortOf.Add(id);
            }
            if (shortOf.Count == 0) return;
            var candidates = new List<LootContainer>();
            foreach (LootContainer c in LootContainer.All)
            {
                if (c == null || c.Contents == null || !c.Searched || !c.IsEmpty || c.Type == ContainerType.Corpse || c.IsSack || c.IsFixture) continue;
                if (NearAnyone(c.transform.position, Config.RestockMinDistance)) continue;
                candidates.Add(c);
            }
            var rng = new System.Random(Environment.TickCount);
            int restocked = 0;
            for (int i = 0; i < Config.RestockBatch && candidates.Count > 0; i++)
            {
                int k = rng.Next(candidates.Count);
                LootContainer c = candidates[k];
                candidates.RemoveAt(k);
                ItemDatabase.RollLoot(c.Type, rng, c.Contents);
                c.Searched = false;
                NetWorld.BroadcastContents(c);
                restocked++;
            }
            foreach (string id in shortOf) s_RestockedAt[id] = Time.time;
            if (restocked > 0) Debug.Log("[CloneZ] Central economy restocked " + restocked + " container(s); short: " + shortOf.Count + " item type(s).");
        }

        // Ground items past their lifetime fade back into the Zone.
        internal static void Expire()
        {
            if (s_Dropped.Count == 0 || !Active) return;
            var near = new List<WorldItem>();
            for (int i = s_Dropped.Count - 1; i >= 0; i--)
            {
                Dropped d = s_Dropped[i];
                if (Time.time < d.Expires) continue;
                s_Dropped.RemoveAt(i);
                if (NearAnyone(d.At, 25f)) { d.Expires = Time.time + 120f; s_Dropped.Add(d); continue; }
                WorldItem.Near(d.At, 0.6f, near);
                foreach (WorldItem w in near)
                    if (w.Def != null && w.Def.Id == d.Id)
                    {
                        NetWorld.GroundGone(w.Def, w.Count, w.Position);
                        w.Consume();
                        break;
                    }
            }
        }

        private static bool NearAnyone(Vector3 p, float range)
        {
            float r2 = range * range;
            foreach (Actor a in s_PlayerCache)
                if (a != null && a.IsPlayerControlled && (a.transform.position - p).sqrMagnitude < r2) return true;
            return false;
        }
        #endregion
    }

    /// <summary>Runs the economy's checks while a raid is live.</summary>
    public sealed class EconomyTicker : MonoBehaviour
    {
        private float _checkAt, _expireAt;
private bool _destroyed;

        private void Update()
        {
            if (!GameSession.IsExtraction) { if (!_destroyed) { _destroyed = true; Destroy(gameObject); } return; }
            if (Time.time >= _expireAt)
            {
                _expireAt = Time.time + 15f;
                CentralEconomy.Expire();
            }
            if (Time.time < _checkAt || CentralEconomy.Config == null) return;
            _checkAt = Time.time + Mathf.Max(20f, CentralEconomy.Config.CheckSeconds);
            CentralEconomy.Tick();
        }
    }

    public sealed class CentralEconomyModule : IGameModule
    {
        /// <summary>Module order for installation.</summary>
        public int Order => 15;
        /// <summary>Initialize the central economy system at game start.</summary>
        public void Init() => CentralEconomy.Install();
    }
}
