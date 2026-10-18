using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// An artifact lying in an anomaly, latent: invisible until a detector good enough for its tier is switched on
    /// close by. Only then can it be picked up. The raid's shared prize (the Sunken Star) also sings — a faint,
    /// wrong chord every so often that anyone near enough can follow.
    /// </summary>
    public sealed class ArtifactNode : Interactable
    {
        public static readonly List<ArtifactNode> All = new List<ArtifactNode>();

        public string ArtifactId { get; private set; }
        public int Tier { get; private set; }
        public bool IsPrize { get; private set; }
        public bool Revealed { get; private set; }

        private Transform _visual;
        private Light _glow;
        private float _revealedUntil, _songAt;

        public override string DisplayName => Revealed ? ArtifactCatalog.NameOf(ArtifactId) : "";
        public override bool IsAvailable => isActiveAndEnabled && Revealed;
        public override float Reach => 1.8f;

        public static ArtifactNode Spawn(Transform parent, string artifactId, Vector3 at, bool prize)
        {
            ArtifactCatalog.Def def = ArtifactCatalog.Get(artifactId);
            if (def == null) return null;
            var go = new GameObject("Artifact " + artifactId);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            ZoneKit.NoBake(go);
            var n = go.AddComponent<ArtifactNode>();
            n.ArtifactId = artifactId;
            n.Tier = prize ? 1 : def.DetectorTier;
            n.IsPrize = prize;
            n.BuildVisual(def);
            return n;
        }

        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        private void BuildVisual(ArtifactCatalog.Def def)
        {
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            var m = new RaidMesh();
            Vector3 c = transform.position + Vector3.up * 0.18f;
            m.SetFrame(c, 0f);
            var rng = new System.Random(def.Id.GetHashCode());
            m.Ellipsoid(Vector3.zero, new Vector3(0.16f, 0.13f, 0.15f), ZoneKit.Gl(def.Color), 8, 5);
            for (int i = 0; i < 3; i++)
                m.Ellipsoid(new Vector3(ZoneKit.R(rng, -0.08f, 0.08f), ZoneKit.R(rng, -0.05f, 0.06f), ZoneKit.R(rng, -0.08f, 0.08f)),
                    Vector3.one * ZoneKit.R(rng, 0.05f, 0.09f), ZoneKit.M(ZoneKit.V(def.Color * 0.6f, rng)), 6, 4);
            ZoneKit.Emit(m, "Mesh", _visual, c, ShadowCastingMode.Off);
            var lg = new GameObject("Glow");
            lg.transform.SetParent(_visual, false);
            lg.transform.position = c + Vector3.up * 0.2f;
            _glow = lg.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.color = def.Color;
            _glow.range = IsPrize ? 6f : 2.5f;
            _glow.intensity = IsPrize ? 2.2f : 1.1f;
            _glow.shadows = LightShadows.None;
            _visual.gameObject.SetActive(false);
        }

        /// <summary>A detector found it: it shows for a while (and stays while the detector keeps it in range).</summary>
        public void Reveal(float seconds = 45f)
        {
            _revealedUntil = Mathf.Max(_revealedUntil, Time.time + seconds);
            if (Revealed) return;
            Revealed = true;
            _visual.gameObject.SetActive(true);
            Sfx.Play(SfxIds.DetectorAlert, transform.position, 0.4f, 1.3f, 8f);
        }

        private void Update()
        {
            if (Revealed && Time.time > _revealedUntil)
            {
                Revealed = false;
                _visual.gameObject.SetActive(false);
            }
            if (_visual.gameObject.activeSelf)
            {
                _visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 1.7f) * 0.04f);
                _visual.Rotate(0f, 25f * Time.deltaTime, 0f);
            }
            if (IsPrize && Time.time > _songAt)
            {
                _songAt = Time.time + Random.Range(18f, 30f);
                Sfx.Play(SfxIds.UncannyWhisper, transform.position + Vector3.up, 0.55f, 0.55f, 70f);
                GameEvents.RaiseWorldEvent(transform.position, 70f, "lure");
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Take " + ArtifactCatalog.NameOf(ArtifactId), Take).Timed(1.2f, "Easing it free..."));
        }

        private void Take(InteractContext ctx)
        {
            if (this == null || ctx.Inventory == null) return;
            ItemDef def = ItemDatabase.Get(ArtifactId);
            if (def == null) return;
            var extra = new ItemExtra { FoundInRaid = true };
            if (ctx.Inventory.Inventory.Add(def, 1, true, extra: extra) <= 0)
            {
                ctx.Notify("No room for it.");
                return;
            }
            ItemStack stack = null;
            List<ItemStack> stacks = ctx.Inventory.Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0; i--) if (stacks[i].Def == def) { stack = stacks[i]; break; }
            GameEvents.RaiseItemLooted(stack, "anomaly");
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null) c.ArtifactsFound++;
            if (IsPrize)
            {
                GameEvents.RaiseToast("You have the Sunken Star. Everything in the Zone can feel it now.");
                GameEvents.RaiseWorldEvent(transform.position, 200f, "prize_taken");
            }
            else GameEvents.RaiseToast(ArtifactCatalog.NameOf(ArtifactId) + " — " + ArtifactCatalog.KnownCount(ArtifactId) + " properties known.");
            ZoneExposure.Add(0.01f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Where the artifacts lie: inside the anomalies (a reason to walk into danger), more in richer Zones; one shared
    /// prize per raid that every crew hears about; and after each emission the field is reshuffled — what nobody took
    /// is gone and new finds surface elsewhere. Placement draws from the raid's seed, so every peer agrees.
    /// </summary>
    public static class ArtifactField
    {
        private static Transform s_Root;
        private static ZoneDef s_Zone;
        private static int s_Seed, s_Reshuffles;

        public static ArtifactNode Prize { get; private set; }

        public static void Build(Transform root, ZoneDef zone, int seed)
        {
            s_Root = root;
            s_Zone = zone;
            s_Seed = seed;
            s_Reshuffles = 0;
            Prize = null;
            Scatter(new System.Random(seed ^ 0x4A71F));
            PlacePrize(new System.Random(seed ^ 0x57A6));
        }

        private static void Scatter(System.Random rng)
        {
            if (s_Zone == null || s_Zone.Artifacts.Length == 0 || Anomaly.All.Count == 0) return;
            int want = Mathf.RoundToInt(Anomaly.All.Count * 0.35f * s_Zone.ArtifactRichness);
            var hosts = new List<Anomaly>(Anomaly.All);
            for (int i = 0; i < want && hosts.Count > 0; i++)
            {
                int k = rng.Next(hosts.Count);
                Anomaly a = hosts[k];
                hosts.RemoveAt(k);
                if (a.Def.Id == "tollgate" && rng.NextDouble() < 0.5) continue;
                string id = s_Zone.Artifacts[rng.Next(s_Zone.Artifacts.Length)];
                if (id == "art_sunkenstar") continue;
                ArtifactNode.Spawn(s_Root, id, InsidePoint(a, rng), false);
            }
        }

        private static Vector3 InsidePoint(Anomaly a, System.Random rng)
        {
            float ang = (float)rng.NextDouble() * 360f, r = a.Radius * Mathf.Lerp(0.2f, 0.65f, (float)rng.NextDouble());
            Vector3 p = a.transform.position + Quaternion.Euler(0f, ang, 0f) * new Vector3(r, 0f, 0f);
            p.y = a.transform.position.y + 0.02f;
            return p;
        }

        // The shared prize sits behind the worst ground: a tollgate if there is one, else the densest cluster.
        private static void PlacePrize(System.Random rng)
        {
            if (s_Zone == null || Anomaly.All.Count == 0) return;
            Anomaly best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < Anomaly.All.Count; i++)
            {
                Anomaly a = Anomaly.All[i];
                int near = 0;
                for (int j = 0; j < Anomaly.All.Count; j++)
                    if (j != i && (Anomaly.All[j].transform.position - a.transform.position).sqrMagnitude < 15f * 15f) near++;
                float score = near + (a.Def.Id == "tollgate" ? 6f : 0f) + (float)rng.NextDouble();
                if (score > bestScore) { bestScore = score; best = a; }
            }
            if (best == null) return;
            Prize = ArtifactNode.Spawn(s_Root, "art_sunkenstar", InsidePoint(best, rng), true);
        }

        public static void AnnouncePrize()
        {
            if (Prize == null || s_Zone == null) return;
            GameEvents.RaiseSubtitle("Radio", "...repeat, the Sunken Star has surfaced in " + s_Zone.Name + ". Every crew in the Zone heard this too.");
        }

        /// <summary>After an emission: unclaimed finds are gone, new ones surface (the prize stays where it is).</summary>
        public static void Reshuffle()
        {
            if (s_Root == null) return;
            for (int i = ArtifactNode.All.Count - 1; i >= 0; i--)
            {
                ArtifactNode n = ArtifactNode.All[i];
                if (n != null && !n.IsPrize) Object.Destroy(n.gameObject);
            }
            s_Reshuffles++;
            Scatter(new System.Random(s_Seed ^ (0x5EED * (s_Reshuffles + 1))));
        }

        public static void Clear()
        {
            s_Root = null;
            Prize = null;
        }
    }

    /// <summary>
    /// Artifacts worn on the belt: every property applies, known or not. Wear one long enough and you notice what it
    /// does (the property is learned for good — account knowledge, it survives death). Carried loose in the pack they
    /// leak a little radiation unless a lead-lined case holds them. Some draw mutants to whoever carries them.
    /// </summary>
    public sealed class ArtifactBelt : MonoBehaviour, IStatModSource
    {
        public const int BaseSlots = 2;
        private const string LeadCase = "leadcase";

        private PlayerInventory _inv;
        private PlayerClothing _clothing;
        private readonly List<string> _belt = new List<string>();
        private int _loose;
        private float _tick, _lureAt;
        private readonly Dictionary<string, float> _wornSeconds = new Dictionary<string, float>();

        public IReadOnlyList<string> Worn => _belt;

        public int Slots
        {
            get
            {
                if (_clothing == null) return BaseSlots;
                ItemDef suit = _clothing.GetWorn(ClothingSlot.Armor);
                if (suit == null) return BaseSlots;
                if (suit.Id == "suit_halo") return BaseSlots + 3;
                return suit.Id.StartsWith("suit_") ? BaseSlots + 2 : BaseSlots;
            }
        }

        private void Awake()
        {
            _inv = GetComponent<PlayerInventory>();
            _clothing = GetComponent<PlayerClothing>();
        }

        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            float dt = 1f - _tick;
            _tick = 1f;
            Refresh();
            if (HubRules.SafeZone) return;

            bool lure = false;
            for (int i = 0; i < _belt.Count; i++)
            {
                ArtifactCatalog.Def d = ArtifactCatalog.Get(_belt[i]);
                if (d == null) continue;
                if (d.LuresMutants) lure = true;
                for (int p = 0; p < d.Properties.Length; p++)
                {
                    string prop = d.Properties[p];
                    if (prop == "lure") lure = true;
                    if (ArtifactCatalog.IsKnown(d.Id, prop)) continue;
                    string key = d.Id + ":" + prop;
                    _wornSeconds.TryGetValue(key, out float s);
                    s += dt;
                    _wornSeconds[key] = s;
                    ArtifactCatalog.Property pr = ArtifactCatalog.Prop(prop);
                    if (pr != null && s >= pr.LearnSeconds) ArtifactCatalog.Learn(d.Id, prop);
                }
            }
            if (_belt.Count > 0) ZoneExposure.Add(0.00012f * _belt.Count * dt);
            if (lure && Time.time > _lureAt)
            {
                _lureAt = Time.time + 20f;
                GameEvents.RaiseWorldEvent(transform.position, 80f, "lure");
            }
        }

        private void Refresh()
        {
            _belt.Clear();
            _loose = 0;
            if (_inv == null || _inv.Inventory == null) return;
            int cases = 0;
            List<ItemStack> stacks = _inv.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack s = stacks[i];
                if (s.Def == null) continue;
                if (s.Def.Id == LeadCase) { cases += s.Count; continue; }
                if (!ArtifactCatalog.IsArtifact(s.Def.Id)) continue;
                if (s.Extra != null && s.Extra.OnBelt && _belt.Count < Slots) _belt.Add(s.Def.Id);
                else _loose += s.Count;
            }
            _loose = Mathf.Max(0, _loose - cases * 2);
        }

        public void ApplyMods(StatMods mods)
        {
            for (int i = 0; i < _belt.Count; i++)
            {
                ArtifactCatalog.Def d = ArtifactCatalog.Get(_belt[i]);
                if (d == null) continue;
                for (int p = 0; p < d.Properties.Length; p++)
                {
                    ArtifactCatalog.Property pr = ArtifactCatalog.Prop(d.Properties[p]);
                    pr?.Apply?.Invoke(mods);
                }
            }
            mods.RadiationPerSec += _loose * 0.004f;
        }

        public static int CountOnBelt(PlayerInventory inv)
        {
            int n = 0;
            if (inv == null || inv.Inventory == null) return 0;
            foreach (ItemStack s in inv.Inventory.Stacks)
                if (s.Extra != null && s.Extra.OnBelt && ArtifactCatalog.IsArtifact(s.Def != null ? s.Def.Id : null)) n++;
            return n;
        }

        /// <summary>The belt verbs on every artifact in the pack, and its tooltip of known properties.</summary>
        internal static void RegisterItemVerbs()
        {
            ItemActions.RegisterMenu((inv, stack, into) =>
            {
                if (stack.Def == null || !ArtifactCatalog.IsArtifact(stack.Def.Id)) return;
                bool on = stack.Extra != null && stack.Extra.OnBelt;
                if (on)
                {
                    into.Add(new ItemMenuAction("Take off belt", () => { stack.Extra.OnBelt = false; inv.Inventory.RaiseChanged(); }));
                    return;
                }
                var belt = inv.GetComponent<ArtifactBelt>();
                int slots = belt != null ? belt.Slots : BaseSlots;
                bool room = CountOnBelt(inv) < slots;
                into.Add(new ItemMenuAction(room ? "Wear on belt" : "Belt full (" + slots + ")", () =>
                {
                    if (stack.Extra == null) stack.Extra = new ItemExtra();
                    stack.Extra.OnBelt = true;
                    inv.Inventory.RaiseChanged();
                    Sfx.PlayUI(SfxIds.GearRustle, 0.5f);
                }, room));
            });
            ItemDef.TooltipProviders.Add(def => ArtifactCatalog.IsArtifact(def != null ? def.Id : null) ? ArtifactCatalog.Describe(def.Id) : null);
        }
    }
}
