using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Where the survivor can get water — a plumbed tap or a rain barrel.</summary>
    public enum WaterSourceKind { Tap, RainBarrel }

    /// <summary>
    /// A place to drink or fill a bottle. A <see cref="WaterSourceKind.Tap"/> gives clean water
    /// but only while the mains run (see <see cref="WaterMains"/>); once the water is shut off it
    /// runs dry. A <see cref="WaterSourceKind.RainBarrel"/> collects rain over time (fed by the
    /// <see cref="WeatherSystem"/>) but its water is tainted — safe only once boiled at a stove.
    ///
    /// Follows the <see cref="CookingStation"/> / <see cref="FarmPlot"/> pattern: a static
    /// registry, built entirely from code with the raid map's furniture builders (the tap is the
    /// kitchen sink unit, the barrel an open poly drum showing its water). Both are solid and their
    /// meshes are baked into the navmesh. The barrel runs its fill simulation off the world clock
    /// like a farm plot. The tap is fitted out as the working kitchen sink (<see cref="KitchenSink"/>,
    /// on the same object): it fronts the tap as the water point, and can keep a bowlful of water
    /// standing for after the mains have failed.
    /// </summary>
    public class WaterSource : MonoBehaviour, IDemolishable, IWaterPoint
    {
        public static readonly List<WaterSource> All = new List<WaterSource>();

        public WaterSourceKind Kind { get; private set; }
        public float Water { get; private set; }   // rain-barrel level 0..WaterBarrelMax (taps ignore this)

        private float _lastHours;                   // world hour the fill sim last advanced
        private Transform _waterFill;               // the barrel's water disc (scales with the level)

        private static readonly Color BarrelWater = new Color(0.34f, 0.5f, 0.52f);

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        public string DemolishName => DisplayName;   // "rain barrel" or "tap"
        public bool CanDemolish(out string reason)
        {
            // Whatever is kept in the cupboard under the sink would be lost with it.
            LootContainer holding = Kind == WaterSourceKind.Tap ? FixtureContents.FirstHolding(transform) : null;
            if (holding != null) { reason = "Empty the " + holding.DisplayName.ToLowerInvariant() + " first"; return false; }
            reason = null;
            return true;
        }
        public void GetSalvage(out int planks, out int nails)
        {
            bool barrel = Kind == WaterSourceKind.RainBarrel;
            planks = barrel ? CloneZConfig.SalvageBarrelPlanks : CloneZConfig.SalvageTapPlanks;
            nails = barrel ? CloneZConfig.SalvageBarrelNails : CloneZConfig.SalvageTapNails;
        }
        public bool CarvesNavMesh => true;
        public void Dismantle() { All.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }   // out of the rebuild that follows
        #endregion

        #region Readouts
        /// <summary>A barrel holds tainted water; a tap runs clean.</summary>
        public bool IsTainted => Kind == WaterSourceKind.RainBarrel;

        /// <summary>True when there's water to be had right now.</summary>
        public bool HasWater =>
            Kind == WaterSourceKind.Tap ? WaterMains.MainsOn : Water >= CloneZConfig.WaterBarrelDrawPerUse;

        /// <summary>Barrel fill 0..1 (always 1 for a running tap, 0 for a dry one).</summary>
        public float Fill01
        {
            get
            {
                if (Kind == WaterSourceKind.Tap) return WaterMains.MainsOn ? 1f : 0f;
                return Mathf.Clamp01(Water / Mathf.Max(0.01f, CloneZConfig.WaterBarrelMax));
            }
        }

        /// <summary>A short label for the source (for the hydration prompt).</summary>
        public string DisplayName => Kind == WaterSourceKind.RainBarrel ? "rain barrel" : "tap";
        #endregion

        #region IWaterPoint
        public Vector3 WaterPosition => transform.position;
        public string WaterLabel => DisplayName;
        public string DryMessage => Kind == WaterSourceKind.Tap
            ? "The tap is dry \u2014 the water's been shut off"
            : "The rain barrel is empty";
        public void DrawWater() => Draw();
        #endregion

        #region Registry queries
        /// <summary>Nearest source within <paramref name="range"/> of a point, or null.</summary>
        public static WaterSource Nearest(Vector3 p, float range)
        {
            WaterSource best = null;
            float bestSqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s == null) continue;
                float d = (s.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = s; }
            }
            return best;
        }

        /// <summary>True if any source already sits within <paramref name="range"/> of a point.</summary>
        public static bool AnyWithin(Vector3 p, float range)
        {
            float sqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s != null && (s.transform.position - p).sqrMagnitude < sqr) return true;
            }
            return false;
        }
        #endregion

        /// <summary>Draws one use of water from a barrel (taps are limitless while the mains run).</summary>
        public void Draw()
        {
            if (Kind != WaterSourceKind.RainBarrel) return;
            Water = Mathf.Max(0f, Water - CloneZConfig.WaterBarrelDrawPerUse);
            RefreshVisual();
        }

        /// <summary>Restores a rain barrel's collected water level from a saved game (taps hold none).</summary>
        public void Restore(float water)
        {
            if (Kind == WaterSourceKind.RainBarrel)
                Water = Mathf.Clamp(water, 0f, CloneZConfig.WaterBarrelMax);
            _lastHours = TimeOfDay.Now;
            RefreshVisual();
        }

        #region Building
        /// <summary>Builds a tap or a rain barrel from code and returns it. A tap is the kitchen sink: a
        /// run of base cabinets with a steel bowl, drainer and mixer tap, its back toward -Z, standing on
        /// the floor, fitted out as a <see cref="KitchenSink"/>. A barrel is fronted by a
        /// <see cref="RainBarrelFixture"/>.</summary>
        public static WaterSource Spawn(Transform parent, Vector3 position, WaterSourceKind kind)
        {
            position.y = BuildPiece.FloorTopAt(position);
            var root = new GameObject(kind == WaterSourceKind.RainBarrel ? "RainBarrel" : "WaterTap");
            root.transform.SetParent(parent);
            root.transform.position = position;

            var src = root.AddComponent<WaterSource>();
            src.Kind = kind;
            if (kind == WaterSourceKind.RainBarrel)
            {
                src.BuildBarrel(position);
                root.AddComponent<RainBarrelFixture>();   // fill a bottle, check the level, wash, skim ([E] / Q)
            }
            else src.BuildTap(position);
            DecorPalette.Apply();

            src._lastHours = TimeOfDay.Now;
            src.RefreshVisual();
            return src;
        }

        private void BuildTap(Vector3 position)
        {
            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            FurnitureBuilder.SinkUnit(m, new System.Random(1993), FurnitureBuilder.HomeKitchen);
            m.ResetFrame();
            m.ApplyTo(gameObject, position);
            Solid(new Vector3(0f, 0.5f, 0f), new Vector3(1.22f, 1.0f, 0.63f));
            KitchenSink.Attach(this);   // the tap, the plug, the water, the washing-up and the cupboard under the bowl
        }

        // A blue poly rain barrel, open at the top so the collected water shows inside it.
        private void BuildBarrel(Vector3 position)
        {
            Water = CloneZConfig.WaterBarrelStart;

            const float r = 0.34f, h = 0.96f, wall = 0.025f;
            Color plastic = new Color(0.17f, 0.33f, 0.46f);
            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            m.Cylinder(Vector3.zero, r, h, 16, DecorPalette.Gloss(plastic), Quaternion.identity, 1f, false);
            m.Cylinder(Vector3.zero, r, 0.02f, 16, DecorPalette.Gloss(plastic * 0.8f));
            foreach (float y in new[] { 0.22f, 0.5f, 0.78f })
                m.Cylinder(new Vector3(0f, y, 0f), r + 0.008f, 0.04f, 16, DecorPalette.Gloss(plastic * 0.86f), Quaternion.identity, 1f, false);
            InnerWall(m, r - wall, h, 16, DecorPalette.Matte(plastic * 0.45f));
            Rim(m, r - wall, r + 0.012f, h, 16, DecorPalette.Gloss(plastic * 0.9f));
            m.Cylinder(new Vector3(0f, 0.02f, 0f), r - wall, 0.004f, 16, DecorPalette.Matte(plastic * 0.35f));
            // A brass spigot low on the side.
            m.CylinderX(new Vector3(0f, 0.16f, r + 0.03f), 0.022f, 0.08f, 8, DecorPalette.Metal(FurnitureBuilder.Brass), 90f);
            m.Box(new Vector3(0f, 0.2f, r + 0.07f), new Vector3(0.03f, 0.05f, 0.03f), DecorPalette.Metal(FurnitureBuilder.Brass));
            m.ResetFrame();
            m.ApplyTo(gameObject, position);
            Solid(new Vector3(0f, h * 0.5f, 0f), new Vector3(r * 2f, h, r * 2f));

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water";
            water.transform.SetParent(transform, false);
            water.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(BarrelWater, smoothness: 0.7f);
            Strip(water);
            _waterFill = water.transform;
        }

        // The inside of the barrel's wall, facing inward (seen past the open top).
        private static void InnerWall(RaidMesh m, float radius, float height, int sides, in Surf s)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * radius;
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * radius;
                m.Quad((p0 + p1) * 0.5f + Vector3.up * (height * 0.5f), (p1 - p0) * 0.5f, Vector3.up * (height * 0.5f), s);
            }
        }

        // The flat ring of the barrel's lip between the inner and outer wall.
        private static void Rim(RaidMesh m, float inner, float outer, float y, int sides, in Surf s)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var d0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                var d1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                Vector3 top = Vector3.up * y;
                m.Tri(top + d0 * inner, top + d0 * outer, top + d1 * outer, s);
                m.Tri(top + d0 * inner, top + d1 * outer, top + d1 * inner, s);
            }
        }

        // Kitchen units and barrels are solid, like the furniture round them.
        private void Solid(Vector3 center, Vector3 size)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
        }

        private static void Strip(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        #endregion

        #region Rain-fill simulation (barrels only)
        private void Update()
        {
            if (Kind != WaterSourceKind.RainBarrel) return;

            float now = TimeOfDay.Now;
            float dH = now - _lastHours;
            _lastHours = now;
            if (dH <= 0f) return;

            float rain = WeatherSystem.Instance != null ? WeatherSystem.Instance.RainIntensity : 0f;
            if (rain > 0f)
            {
                Water = Mathf.Min(CloneZConfig.WaterBarrelMax, Water + CloneZConfig.WaterBarrelRainPerHour * rain * dH);
                RefreshVisual();
            }
        }

        // Raises/lowers the water inside the open barrel to match the level (its surface shows from above).
        private void RefreshVisual()
        {
            if (_waterFill == null) return;
            float level = Fill01;
            _waterFill.gameObject.SetActive(level > 0.02f);
            if (level <= 0.02f) return;

            float h = Mathf.Lerp(0.04f, 0.88f, level);           // depth of water standing in the barrel
            _waterFill.localScale = new Vector3(0.62f, h * 0.5f, 0.62f);
            _waterFill.position = transform.position + new Vector3(0f, 0.025f + h * 0.5f, 0f);
        }
        #endregion
    }
}
