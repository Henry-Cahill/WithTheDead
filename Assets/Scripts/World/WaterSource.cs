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
    /// registry, built entirely from code with its colliders stripped so it never disturbs the
    /// baked navmesh. The barrel runs its fill simulation off the world clock like a farm plot.
    /// </summary>
    public class WaterSource : MonoBehaviour, IDemolishable
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
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails)
        {
            bool barrel = Kind == WaterSourceKind.RainBarrel;
            planks = barrel ? CloneZConfig.SalvageBarrelPlanks : CloneZConfig.SalvageTapPlanks;
            nails = barrel ? CloneZConfig.SalvageBarrelNails : CloneZConfig.SalvageTapNails;
        }
        public bool CarvesNavMesh => true;
        public void Dismantle() { All.Remove(this); Destroy(gameObject); }
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
        /// <summary>Builds a tap or a rain barrel from code (colliders stripped) and returns it.</summary>
        public static WaterSource Spawn(Transform parent, Vector3 position, WaterSourceKind kind)
        {
            var root = new GameObject(kind == WaterSourceKind.RainBarrel ? "RainBarrel" : "WaterTap");
            root.transform.SetParent(parent);
            root.transform.position = position;

            var src = root.AddComponent<WaterSource>();
            src.Kind = kind;
            if (kind == WaterSourceKind.RainBarrel) src.BuildBarrel(position);
            else src.BuildTap(position);

            src._lastHours = TimeOfDay.Now;
            src.RefreshVisual();
            return src;
        }

        private void BuildTap(Vector3 position)
        {
            var enamel = new Color(0.72f, 0.74f, 0.78f);
            var pipe = new Color(0.55f, 0.58f, 0.62f);
            // A little sink: basin, splashback and a spout. Visual only.
            Strip(CloneZUtil.Box("Basin", position + new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.5f, 0.6f), enamel, transform));
            Strip(CloneZUtil.Box("Splashback", position + new Vector3(0f, 0.95f, -0.24f), new Vector3(0.8f, 0.4f, 0.12f), enamel, transform));
            Strip(CloneZUtil.Box("Spout", position + new Vector3(0f, 0.98f, -0.02f), new Vector3(0.08f, 0.08f, 0.36f), pipe, transform));
        }

        private void BuildBarrel(Vector3 position)
        {
            Water = CloneZConfig.WaterBarrelStart;

            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Drum";
            drum.transform.SetParent(transform, false);
            drum.transform.position = position + new Vector3(0f, 0.6f, 0f);
            drum.transform.localScale = new Vector3(0.72f, 0.6f, 0.72f);
            drum.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.26f, 0.33f, 0.31f));
            Strip(drum);

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water";
            water.transform.SetParent(transform, false);
            water.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(BarrelWater, smoothness: 0.7f);
            Strip(water);
            _waterFill = water.transform;
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

        // Raises/lowers the water disc inside the drum to match the level.
        private void RefreshVisual()
        {
            if (_waterFill == null) return;
            float level = Fill01;
            _waterFill.gameObject.SetActive(level > 0.02f);
            if (level <= 0.02f) return;

            float h = Mathf.Lerp(0.04f, 0.55f, level);           // visible depth of water in the drum
            _waterFill.localScale = new Vector3(0.64f, h * 0.5f, 0.64f);
            _waterFill.position = transform.position + new Vector3(0f, 0.32f + h * 0.5f, 0f);
        }
        #endregion
    }
}
