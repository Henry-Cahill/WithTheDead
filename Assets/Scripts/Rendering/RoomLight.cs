using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// A room's light fitting — a ceiling bulb or a desk lamp: a warm point light with a small glowing
    /// bulb, optionally flickering like a failing tube (random dips and the odd brief outage). In the
    /// pitch-dark raid nights these are the warm yellow pools of light in a black world.
    ///
    /// Every fitting is listed in <see cref="All"/>, and together they keep to a budget: only the
    /// <see cref="MaxShadowedLights"/> fittings nearest the survivor cast shadows (a shadowed point light
    /// renders six shadow maps), and fittings more than <see cref="CullDistance"/> away switch off
    /// entirely, so a whole town of lamps stays cheap. The bulb has no collider and is excluded from the
    /// NavMesh bake, so fittings can be spawned anywhere under the world root.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomLight : MonoBehaviour
    {
        /// <summary>Suggested warm tungsten colour for callers.</summary>
        public static readonly Color DefaultColor = new Color(1f, 0.82f, 0.55f);
        /// <summary>Suggested range for a ceiling fitting ~2.4 m up that lights a whole room.</summary>
        public const float CeilingRange = 7.5f;
        /// <summary>Suggested intensity for a ceiling fitting.</summary>
        public const float CeilingIntensity = 3f;
        /// <summary>Suggested range for a desk / table lamp a little above the desk top.</summary>
        public const float LampRange = 4f;
        /// <summary>Suggested intensity for a desk / table lamp.</summary>
        public const float LampIntensity = 1.6f;

        /// <summary>How many fittings (the nearest to the survivor) may cast shadows at once.</summary>
        public const int MaxShadowedLights = 4;
        /// <summary>Fittings farther than this from the survivor are switched off.</summary>
        public const float CullDistance = 45f;
        private const float BudgetInterval = 0.25f;

        private const float BulbDiameter = 0.11f;
        private const float BulbGlow = 4f;               // HDR multiplier, so the bulb catches the bloom

        // Point-light shadows use URP's "Low" tier (256 px per cube face); URP ignores normal bias for points.
        private const float ShadowDepthBias = 0.9f;
        private const float ShadowNearPlane = 0.1f;

        // Flicker
        private const float FlickerWobble = 0.12f;       // constant shimmer, fraction of full output
        private const float WobbleSpeed = 7f;
        private const float DipMinInterval = 0.25f, DipMaxInterval = 2.2f;
        private const float DipMinSeconds = 0.04f, DipMaxSeconds = 0.16f;
        private const float DipMinDepth = 0.25f, DipMaxDepth = 0.75f;
        private const float OutageMinInterval = 3f, OutageMaxInterval = 11f;
        private const float OutageMinSeconds = 0.08f, OutageMaxSeconds = 0.5f;
        private const float DoubleBlinkChance = 0.35f;

        private const float LitThreshold = 0.3f;         // output above which the fitting counts as lit
        private const float LitRangeFraction = 0.7f;     // how far into its range a point counts as lit

        private static readonly List<RoomLight> s_All = new List<RoomLight>();
        private static readonly RoomLight[] s_Nearest = new RoomLight[MaxShadowedLights];
        private static readonly float[] s_NearestSqr = new float[MaxShadowedLights];
        private static float s_NextBudget;
        private static int s_BudgetFrame = -1;

        /// <summary>Every live fitting (iterate with a for loop to stay allocation free).</summary>
        public static IReadOnlyList<RoomLight> All => s_All;

        /// <summary>The fitting's point light.</summary>
        public Light Light { get; private set; }
        /// <summary>The light colour.</summary>
        public Color Color { get; private set; }
        /// <summary>The light range in metres.</summary>
        public float Range { get; private set; }
        /// <summary>The light's full (unflickered) intensity.</summary>
        public float Intensity { get; private set; }
        /// <summary>True when the caller asked for shadows (the budget decides whether it gets them).</summary>
        public bool CastShadows { get; private set; }
        /// <summary>Flickering (dips and brief outages) on or off.</summary>
        public bool Flicker { get; set; }
        /// <summary>The switch: false turns the fitting off.</summary>
        public bool On { get; set; } = true;
        /// <summary>Draw the small glowing bulb (turn it off when the fitting's own mesh already glows).</summary>
        public bool ShowBulb
        {
            get => _showBulb;
            set
            {
                if (_showBulb == value) return;
                _showBulb = value;
                _applied = -1f;
                ApplyState();
            }
        }
        /// <summary>Current output, 0 (dark) .. 1 (full), including flicker and the switch.</summary>
        public float Level { get; private set; } = 1f;
        /// <summary>True while the fitting is switched on, within range of the survivor and not in a flicker dip.</summary>
        public bool IsLit => On && !_culled && Light != null && Level > LitThreshold;
        /// <summary>True while this fitting holds one of the shadow-casting slots.</summary>
        public bool IsCastingShadows => _shadowed;

        private MeshRenderer _bulb;
        private Material _bulbShared;
        private Material _bulbInstance;   // own copy, only once the bulb has to dim (flicker / switch)
        private bool _showBulb = true;
        private bool _culled;
        private bool _shadowed;
        private float _applied = -1f;
        private float _seed;
        private float _dipTimer, _dipLeft, _dipDepth;
        private float _outageTimer, _outageLeft;

        /// <summary>
        /// Creates a fitting at <paramref name="position"/> (world space) under <paramref name="parent"/>.
        /// Suggested values: <see cref="DefaultColor"/>; a ceiling bulb <see cref="CeilingRange"/> /
        /// <see cref="CeilingIntensity"/> about 2.4 m up (keep it at least 0.15 m below any ceiling slab);
        /// a desk lamp <see cref="LampRange"/> / <see cref="LampIntensity"/>. A lamp shade around the
        /// bulb casts real shadows, so leave it open at the bottom.
        /// </summary>
        public static RoomLight Spawn(Transform parent, Vector3 position, Color color, float range, float intensity,
            bool flicker = false, bool castShadows = true)
        {
            var go = new GameObject("RoomLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var fitting = go.AddComponent<RoomLight>();
            fitting.Build(color, range, intensity, flicker, castShadows);
            return fitting;
        }

        /// <summary>True when <paramref name="point"/> is well inside the pool of a lit fitting
        /// (walls are not considered — pair it with a line-of-sight test).</summary>
        public static bool IsPointLit(Vector3 point)
        {
            for (int i = 0; i < s_All.Count; i++)
            {
                RoomLight l = s_All[i];
                if (l == null || !l.IsLit) continue;
                float r = l.Range * LitRangeFraction;
                if ((point - l.transform.position).sqrMagnitude <= r * r) return true;
            }
            return false;
        }

        private void Build(Color color, float range, float intensity, bool flicker, bool castShadows)
        {
            Color = color;
            Range = Mathf.Clamp(range, 0.5f, 40f);
            Intensity = Mathf.Max(0f, intensity);
            Flicker = flicker;
            CastShadows = castShadows;

            _seed = Random.value * 100f;
            _dipTimer = Random.Range(DipMinInterval, DipMaxInterval);
            _outageTimer = Random.Range(OutageMinInterval, OutageMaxInterval);

            Light light = GetComponent<Light>();
            if (light == null) light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = Range;
            light.intensity = Intensity;
            // Shadows start off; the budget hands them to the fittings nearest the survivor.
            light.shadows = LightShadows.None;
            light.shadowStrength = 1f;
            light.shadowBias = ShadowDepthBias;
            light.shadowNormalBias = 0f;
            light.shadowNearPlane = ShadowNearPlane;
            UniversalAdditionalLightData data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.softShadowQuality = SoftShadowQuality.Medium;
            data.renderingLayers = uint.MaxValue;   // includes the survivor's layer (see VisionRig)
            if (Application.isPlaying)
                data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
            Light = light;

            BuildBulb();
            _applied = -1f;
            ApplyState();

            // A fitting created after a budget pass shouldn't wait for the next one to be culled / shadowed.
            s_NextBudget = 0f;
        }

        private void BuildBulb()
        {
            var go = new GameObject("Bulb");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * BulbDiameter;
            go.AddComponent<MeshFilter>().sharedMesh = VisionAssets.BulbMesh();
            _bulb = go.AddComponent<MeshRenderer>();
            _bulb.shadowCastingMode = ShadowCastingMode.Off;   // never shadow its own light
            _bulb.receiveShadows = false;
            _bulb.lightProbeUsage = LightProbeUsage.Off;
            _bulb.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _bulbShared = VisionAssets.BulbMaterial(BulbColor(1f));
            _bulb.sharedMaterial = _bulbShared;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        private Color BulbColor(float level)
        {
            Color c = Color * (BulbGlow * level);
            c.a = 1f;
            return c;
        }

        private void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
        }

        private void OnDisable()
        {
            s_All.Remove(this);
        }

        private void OnDestroy()
        {
            if (_bulbInstance != null) Destroy(_bulbInstance);
        }

        private void Update()
        {
            float now = Time.time;
            if (Time.frameCount != s_BudgetFrame && (now >= s_NextBudget || s_NextBudget - now > BudgetInterval))
            {
                s_BudgetFrame = Time.frameCount;
                s_NextBudget = now + BudgetInterval;
                RefreshBudget();
            }

            if (Light == null) return;
            Level = !On ? 0f : Flicker ? EvaluateFlicker(Time.deltaTime) : 1f;
            ApplyState();
        }

        // Pushes the switch, the cull state and the flicker level into the light and the bulb.
        private void ApplyState()
        {
            if (Light == null) return;
            float level = _culled ? 0f : Level;
            bool lit = level > 0.001f;
            if (Light.enabled != lit) Light.enabled = lit;
            if (Mathf.Abs(level - _applied) < 0.002f) return;
            _applied = level;
            Light.intensity = Intensity * level;

            if (_bulb == null) return;
            bool show = lit && _showBulb;
            if (_bulb.forceRenderingOff == show) _bulb.forceRenderingOff = !show;
            if (!show) return;
            if (level < 0.999f || _bulbInstance != null)
            {
                // Only a dimmed bulb needs its own material; a steady one keeps the shared, batchable one.
                if (_bulbInstance == null)
                {
                    _bulbInstance = new Material(_bulbShared);
                    _bulb.sharedMaterial = _bulbInstance;
                }
                VisionAssets.SetBulbColor(_bulbInstance, BulbColor(level));
            }
        }

        // A failing fitting: a constant shimmer, short random dips and the occasional blackout,
        // sometimes blinking twice.
        private float EvaluateFlicker(float dt)
        {
            if (_outageLeft > 0f)
            {
                _outageLeft -= dt;
                return 0f;
            }

            _outageTimer -= dt;
            if (_outageTimer <= 0f)
            {
                _outageLeft = Random.Range(OutageMinSeconds, OutageMaxSeconds);
                _outageTimer = Random.value < DoubleBlinkChance
                    ? _outageLeft + Random.Range(0.12f, 0.3f)
                    : Random.Range(OutageMinInterval, OutageMaxInterval);
                return 0f;
            }

            float level = 1f - FlickerWobble * Mathf.PerlinNoise(_seed, Time.time * WobbleSpeed);
            if (_dipLeft > 0f)
            {
                _dipLeft -= dt;
                level *= 1f - _dipDepth;
            }
            else
            {
                _dipTimer -= dt;
                if (_dipTimer <= 0f)
                {
                    _dipLeft = Random.Range(DipMinSeconds, DipMaxSeconds);
                    _dipDepth = Random.Range(DipMinDepth, DipMaxDepth);
                    _dipTimer = Random.Range(DipMinInterval, DipMaxInterval);
                }
            }
            return Mathf.Clamp01(level);
        }

        private void SetCulled(bool culled)
        {
            if (_culled == culled) return;
            _culled = culled;
            _applied = -1f;
            ApplyState();
        }

        private void SetShadowed(bool shadowed)
        {
            _shadowed = shadowed;
            if (Light == null) return;
            LightShadows want = shadowed ? LightShadows.Soft : LightShadows.None;
            if (Light.shadows != want) Light.shadows = want;
        }

        // Culls far fittings and hands the shadow slots to the nearest ones that want them.
        private static void RefreshBudget()
        {
            if (!TryGetSurvivor(out Vector3 center)) return;

            for (int i = 0; i < MaxShadowedLights; i++)
            {
                s_Nearest[i] = null;
                s_NearestSqr[i] = float.MaxValue;
            }

            float cullSqr = CullDistance * CullDistance;
            for (int i = 0; i < s_All.Count; i++)
            {
                RoomLight l = s_All[i];
                if (l == null || l.Light == null) continue;
                float d = (l.transform.position - center).sqrMagnitude;
                bool culled = d > cullSqr;
                l.SetCulled(culled);
                if (!culled && l.CastShadows && l.On) InsertNearest(l, d);
            }

            for (int i = 0; i < s_All.Count; i++)
            {
                RoomLight l = s_All[i];
                if (l == null) continue;
                bool shadowed = false;
                for (int k = 0; k < MaxShadowedLights; k++)
                    if (s_Nearest[k] == l) { shadowed = true; break; }
                l.SetShadowed(shadowed);
            }
        }

        private static void InsertNearest(RoomLight l, float sqr)
        {
            for (int i = 0; i < MaxShadowedLights; i++)
            {
                if (sqr >= s_NearestSqr[i]) continue;
                for (int j = MaxShadowedLights - 1; j > i; j--)
                {
                    s_Nearest[j] = s_Nearest[j - 1];
                    s_NearestSqr[j] = s_NearestSqr[j - 1];
                }
                s_Nearest[i] = l;
                s_NearestSqr[i] = sqr;
                return;
            }
        }

        private static bool TryGetSurvivor(out Vector3 position)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Player != null)
            {
                position = gm.Player.transform.position;
                return true;
            }
            Camera cam = Camera.main;
            if (cam != null)
            {
                position = cam.transform.position;
                return true;
            }
            position = default;
            return false;
        }
    }
}
