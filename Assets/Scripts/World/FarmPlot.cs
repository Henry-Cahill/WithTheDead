using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A tilled patch of soil the survivor can sow, water and harvest — Project
    /// Zomboid's farming plot. A sown plot grows its crop over in-game hours, but
    /// only while it holds moisture: plots dry out over time, rain (the
    /// <see cref="WeatherSystem"/>) refills them, and a watering can tops them up. A
    /// plot left parched wilts and eventually dies; a harvested plot returns to bare
    /// soil so it can be resown.
    ///
    /// Like <see cref="CookingStation"/> and <see cref="SleepSpot"/> it keeps a static
    /// registry and is built entirely from code with its colliders stripped, so it
    /// never disturbs the baked navmesh. The plot runs its own growth simulation off
    /// the world clock (<see cref="TimeOfDay.Now"/>), matching how perishable food ages.
    /// </summary>
    public class FarmPlot : MonoBehaviour, IDemolishable
    {
        public static readonly List<FarmPlot> All = new List<FarmPlot>();

        public enum PlotState { Empty, Growing, Mature, Dead }

        /// <summary>Current growth state of the plot.</summary>
        public PlotState State { get; private set; } = PlotState.Empty;
        /// <summary>The crop currently planted in the plot, or null if empty.</summary>
        public FarmCrop Crop { get; private set; }
        /// <summary>Current water amount in the plot (0..FarmWaterMax).</summary>
        public float Water { get; private set; }       // 0..FarmWaterMax
        /// <summary>Growth progress of the crop as a 0‑1 value.</summary>
        public float Growth01 { get; private set; }     // 0..1 toward maturity
        /// <summary>Health of the crop (0..1), zero means dead.</summary>
        public float Health { get; private set; } = 1f; // 0..1; hits 0 -> the crop dies

        private float _lastHours;                        // world hour the sim last advanced
        private GameObject _plant;                       // the growing shoot (null when bare)
        private Renderer _plantRenderer;
        private Renderer _soilRenderer;

        private static readonly Color SoilDry = new Color(0.34f, 0.25f, 0.17f);
        private static readonly Color SoilWet = new Color(0.20f, 0.14f, 0.09f);
        private static readonly Color Shoot = new Color(0.36f, 0.66f, 0.32f);
        private static readonly Color Wilt = new Color(0.58f, 0.52f, 0.24f);
        private static readonly Color Dead = new Color(0.32f, 0.27f, 0.20f);

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        public string DemolishName => "plot";
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails) { planks = 0; nails = 0; }   // just tilled soil
        public bool CarvesNavMesh => false;
        public void Dismantle() { All.Remove(this); Destroy(gameObject); }
        #endregion

        #region Registry queries
        /// <summary>Nearest plot within <paramref name="range"/> of a point, or null.</summary>
        public static FarmPlot Nearest(Vector3 p, float range)
        {
            FarmPlot best = null;
            float bestSqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var plot = All[i];
                if (plot == null) continue;
                float d = (plot.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = plot; }
            }
            return best;
        }

        /// <summary>True if any plot already sits within <paramref name="range"/> of a point
        /// (used to stop new plots being tilled on top of one another).</summary>
        public static bool AnyWithin(Vector3 p, float range)
        {
            float sqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var plot = All[i];
                if (plot == null) continue;
                if ((plot.transform.position - p).sqrMagnitude < sqr) return true;
            }
            return false;
        }
        #endregion

        #region Readouts (for the farming prompt)
        /// <summary>Display name of the planted crop, or empty if none.</summary>
        public string CropName => Crop != null ? Crop.Name : "";
        /// <summary>Water amount normalized to 0‑1 range based on FarmWaterMax.</summary>
        public float Water01 => CloneZConfig.FarmWaterMax > 0f ? Water / CloneZConfig.FarmWaterMax : 0f;
        /// <summary>True when water is below the low‑threshold, indicating the plot is thirsty.</summary>
        public bool IsParched => Water < CloneZConfig.FarmWaterLowThreshold;
        /// <summary>True when water is below the top‑up threshold, indicating watering is advisable.</summary>
        public bool NeedsWater => Water < CloneZConfig.FarmWaterMax * CloneZConfig.FarmWaterTopUpBelow;
        /// <summary>World hour the growth sim last advanced (captured/restored by the save system).</summary>
        public float LastHours => _lastHours;

        /// <summary>Human‑readable word describing the plot's current state.</summary>
        public string StateWord
        {
            get
            {
                switch (State)
                {
                    case PlotState.Growing: return "growing";
                    case PlotState.Mature: return "ripe";
                    case PlotState.Dead: return "dead";
                    default: return "tilled";
                }
            }
        }
        #endregion

        #region Building
        /// <summary>Breaks new ground into a bare, tilled plot and returns it.</summary>
        public static FarmPlot Spawn(Transform parent, Vector3 position)
        {
            var root = new GameObject("FarmPlot");
            root.transform.SetParent(parent);
            root.transform.position = position;

            float s = CloneZConfig.FarmPlotSize;
            var soil = CloneZUtil.Box("Soil", position + new Vector3(0f, 0.05f, 0f),
                new Vector3(s, 0.1f, s), SoilDry, root.transform);
            Strip(soil);

            var plot = root.AddComponent<FarmPlot>();
            plot._soilRenderer = soil.GetComponent<Renderer>();
            return plot;
        }

        private static void Strip(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        #endregion

        #region Actions
        /// <summary>Sows a crop into a bare plot.</summary>
        public void Plant(FarmCrop crop)
        {
            if (crop == null || State != PlotState.Empty) return;
            Crop = crop;
            State = PlotState.Growing;
            Growth01 = 0f;
            Health = 1f;
            Water = CloneZConfig.FarmWaterStart;
            _lastHours = TimeOfDay.Now;
            BuildPlant();
            RefreshVisual();
        }

        /// <summary>Restores a plot's full growth state (crop, moisture, growth, health) from a saved game.</summary>
        public void Restore(string cropId, PlotState state, float water, float growth, float health, float lastHours)
        {
            Crop = FarmCrop.Get(cropId);
            State = state;
            Water = Mathf.Clamp(water, 0f, CloneZConfig.FarmWaterMax);
            Growth01 = Mathf.Clamp01(growth);
            Health = Mathf.Clamp01(health);
            _lastHours = lastHours;
            if (State != PlotState.Empty && Crop != null) BuildPlant();
            RefreshVisual();
        }

        /// <summary>Adds a pass of the watering can to the plot.</summary>
        public void AddWater()
        {
            Water = Mathf.Min(CloneZConfig.FarmWaterMax, Water + CloneZConfig.FarmCanRefill);
            RefreshVisual();
        }

        /// <summary>Pulls a mature crop, returning how many were gathered (0 if not ready) and the
        /// produce item id. <paramref name="yieldMult"/> is the farmer's yield bonus. The plot goes
        /// back to bare soil, ready to resow.</summary>
        public int Harvest(float yieldMult, out string produceId)
        {
            produceId = Crop != null ? Crop.ProduceId : null;
            if (State != PlotState.Mature || Crop == null) return 0;

            int baseN = Random.Range(Crop.YieldMin, Crop.YieldMax + 1);
            int n = Mathf.Max(1, Mathf.RoundToInt(baseN * Mathf.Max(0.1f, yieldMult)));
            ResetToEmpty();
            return n;
        }

        /// <summary>Clears a dead crop, returning the plot to bare soil.</summary>
        public void Clear()
        {
            if (State == PlotState.Dead) ResetToEmpty();
        }

        private void ResetToEmpty()
        {
            State = PlotState.Empty;
            Crop = null;
            Growth01 = 0f;
            Health = 1f;
            if (_plant != null) { Destroy(_plant); _plant = null; _plantRenderer = null; }
            RefreshVisual();
        }
        #endregion

        #region Growth simulation
        private void Update()
        {
            if (State == PlotState.Growing) Advance();
        }

        // Advances the crop by however many in-game hours have passed since the last tick.
        private void Advance()
        {
            float now = TimeOfDay.Now;
            float dH = now - _lastHours;
            _lastHours = now;
            if (dH <= 0f) { RefreshVisual(); return; }

            // Rain tops the plot up; the sun and soil draw the moisture back down.
            float rain = WeatherSystem.Instance != null ? WeatherSystem.Instance.RainIntensity : 0f;
            Water += CloneZConfig.FarmWaterRainPerHour * rain * dH;
            Water -= CloneZConfig.FarmWaterDryPerHour * dH;
            Water = Mathf.Clamp(Water, 0f, CloneZConfig.FarmWaterMax);

            if (Water > CloneZConfig.FarmWaterLowThreshold)
            {
                // Watered: the crop grows, and a wilted plot slowly recovers.
                Growth01 += dH / Mathf.Max(0.1f, Crop.GrowHours);
                Health = Mathf.Min(1f, Health + CloneZConfig.FarmRecoverPerHour * dH);
                if (Growth01 >= 1f)
                {
                    Growth01 = 1f;
                    State = PlotState.Mature;
                }
            }
            else
            {
                // Parched: the crop wilts and, left long enough, dies.
                Health -= CloneZConfig.FarmWiltPerHour * dH;
                if (Health <= 0f)
                {
                    Health = 0f;
                    State = PlotState.Dead;
                }
            }

            RefreshVisual();
        }

        // Redraws the soil (darker when wet) and the shoot (taller and riper as it grows).
        private void RefreshVisual()
        {
            if (_soilRenderer != null)
                Tint(_soilRenderer, Color.Lerp(SoilDry, SoilWet, Water01));

            if (_plant == null) return;

            float height;
            Color color;
            switch (State)
            {
                case PlotState.Growing:
                    height = Mathf.Lerp(0.12f, 0.6f, Growth01);
                    color = Color.Lerp(Shoot, Crop != null ? Crop.RipeColor : Shoot, Growth01);
                    if (IsParched) color = Color.Lerp(color, Wilt, 0.5f);   // yellows when thirsty
                    break;
                case PlotState.Mature:
                    height = 0.72f;
                    color = Crop != null ? Crop.RipeColor : Shoot;
                    break;
                case PlotState.Dead:
                    height = 0.28f;
                    color = Dead;
                    break;
                default:
                    _plant.SetActive(false);
                    return;
            }

            _plant.SetActive(true);
            float w = CloneZConfig.FarmPlotSize * 0.5f;
            _plant.transform.localScale = new Vector3(w, height, w);
            _plant.transform.localPosition = new Vector3(0f, 0.1f + height * 0.5f, 0f);
            if (_plantRenderer != null) Tint(_plantRenderer, color);
        }

        private void BuildPlant()
        {
            if (_plant != null) return;
            _plant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _plant.name = "Crop";
            _plant.transform.SetParent(transform, false);
            var col = _plant.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _plantRenderer = _plant.GetComponent<Renderer>();
            _plantRenderer.sharedMaterial = CloneZUtil.Mat(Shoot);
        }

        private static void Tint(Renderer r, Color c)
        {
            var m = r.sharedMaterial;
            if (m == null) return;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
        #endregion
    }

    /// <summary>A kind of crop: what seed sows it, what it yields, and how it grows.
    /// The catalogue is keyed by crop id (the produce item id). Grounded in PZ's
    /// farming crops (potato / cabbage / tomato / carrot) with demo-scaled grow times.</summary>
    public class FarmCrop
    {
        public string Id;           // crop key (== produce item id)
        public string Name;
        public string SeedId;       // item consumed to sow
        public string ProduceId;    // food item yielded on harvest
        public float GrowHours;     // in-game hours to mature when kept watered
        public int YieldMin;
        public int YieldMax;
        public Color RipeColor;

        private static Dictionary<string, FarmCrop> s_Crops;

        /// <summary>Looks up a crop by its id (returns null if unknown). Crops are loaded from the
        /// content database as part of <see cref="ItemDatabase.EnsureInit"/>; the built-in
        /// <see cref="BuildSeed"/> catalogue is used only if that database can't be opened.</summary>
        public static FarmCrop Get(string id)
        {
            if (s_Crops == null) ItemDatabase.EnsureInit();
            if (s_Crops == null) SetAll(BuildSeed());
            return id != null && s_Crops.TryGetValue(id, out FarmCrop c) ? c : null;
        }

        /// <summary>The built-in crop catalogue that seeds the content database.</summary>
        public static List<FarmCrop> BuildSeed()
        {
            return new List<FarmCrop>
            {
                new FarmCrop { Id = "potato", Name = "Potatoes", SeedId = "potatoseeds", ProduceId = "potato", GrowHours = 12f, YieldMin = 2, YieldMax = 5, RipeColor = new Color(0.72f, 0.6f, 0.34f) },
                new FarmCrop { Id = "carrot", Name = "Carrots", SeedId = "carrotseeds", ProduceId = "carrot", GrowHours = 11f, YieldMin = 2, YieldMax = 5, RipeColor = new Color(0.5f, 0.7f, 0.3f) },
                new FarmCrop { Id = "tomato", Name = "Tomatoes", SeedId = "tomatoseeds", ProduceId = "tomato", GrowHours = 14f, YieldMin = 2, YieldMax = 6, RipeColor = new Color(0.8f, 0.32f, 0.24f) },
                new FarmCrop { Id = "cabbage", Name = "Cabbages", SeedId = "cabbageseeds", ProduceId = "cabbage", GrowHours = 16f, YieldMin = 1, YieldMax = 3, RipeColor = new Color(0.5f, 0.68f, 0.4f) },
            };
        }

        /// <summary>Replaces the live crop catalogue — called by <see cref="ItemDatabase"/> once the
        /// content database (or the seed fallback) has been loaded.</summary>
        public static void SetAll(IEnumerable<FarmCrop> crops)
        {
            s_Crops = new Dictionary<string, FarmCrop>();
            foreach (FarmCrop c in crops) s_Crops[c.Id] = c;
        }
    }
}
