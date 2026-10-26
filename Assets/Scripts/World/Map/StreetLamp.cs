using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// A street light that still burns: the fixture at the foot of its pole (carrying the pole's collider, so the pole never
    /// blocks the reach for its own switch) with its glowing lens up top. The lamps keep no Update and, in play, no lights
    /// of their own: one driver (<see cref="StreetLampDriver"/>) works them all, a few times a second lending its pool of
    /// <see cref="MaxLit"/> spot lights to the burning lamps nearest the survivor (the nearest within
    /// <see cref="ShadowReach"/> with shadows), however many lamps the streamed pieces hold, and putting every lamp out by day
    /// (photocells); each frame it flickers the failing ones it lights, and blinks the map's flashing signal lenses and
    /// aviation beacons (<see cref="Blinkers"/>). A map built in the editor (previews, the map harness) has no driver
    /// running, so a few of its lamps (the builder's cap) get a light of their own instead. The survivor can switch a lamp,
    /// dim, steady or set off its flicker, or switch and dim the block's lamps; a lamp's state is saved under its place
    /// (<c>"streetlamp@x,z"</c>: bit 0 on, bit 1 flickering, bit 2 dimmed) and remembered when a streamed piece is built again.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class StreetLamp : Interactable
    {
        /// <summary>Real lights burning at once: the driver's pool, lent to the nearest burning lamps to the survivor.</summary>
        public const int MaxLit = 8;
        /// <summary>Lamps farther than this from the survivor only glow.</summary>
        public const float LightReach = 64f;
        /// <summary>The nearest lit lamp within this casts shadows.</summary>
        public const float ShadowReach = 22f;
        private const float BlockRadius = 30f;   // how far "the block's lamps" reach
        private const float ClickNoise = 2f;
        private const int OnBit = 1, FlickerBit = 2, DimBit = 4;
        private const float DimLevel = 0.3f;

        private static readonly List<StreetLamp> s_All = new List<StreetLamp>();
        // Lamps the survivor switched or steadied, by key: a streamed piece built again gets them back as they were left.
        // Forgotten when a new map starts (ForgetSwitches), so one raid's switches don't carry into the next.
        private static readonly Dictionary<string, int> s_Changed = new Dictionary<string, int>();
        private static StreetLampDriver s_Driver;
        private static readonly Dictionary<Material, Material[]> s_LensStates = new Dictionary<Material, Material[]>();
        private static Material s_DeadLens;
        private static readonly Dictionary<int, Mesh> s_LensMeshes = new Dictionary<int, Mesh>();

        // An edit-mode build's own light (null in play), or the pool's light lent to the lamp while it's among the nearest.
        private Light _light, _lent;
        private Transform _head;
        private Renderer _lens;
        private Material[] _states;   // the lens off, dim, on
        private Color _colour;
        private float _intensity, _range, _angle, _phase, _level = 1f;
        private bool _on = true, _flicker, _dim, _ready, _lit;
        private int _shown = -1;
        private string _key;
        private System.Action<InteractContext> _switch, _toggleFlicker, _dimLamp, _blockOn, _blockOff, _blockDim;

        /// <summary>The lens's shape.</summary>
        internal enum Lens { Cobra, Globe, Dome }

        /// <summary>Puts a burning lamp's fixture at the foot of its pole (<paramref name="foot"/>, the pole
        /// <paramref name="pole"/> in size, its collider), its lens at <paramref name="lens"/> turned <paramref name="yaw"/>
        /// degrees (all in the site's coordinates, under <paramref name="town"/>'s world), and the spot light it shines
        /// with when lit: lent by the driver in play; with <paramref name="light"/> in an editor build, its own (counted in
        /// the town's lights).</summary>
        internal static StreetLamp Attach(RaidTown town, Vector3 foot, Vector3 pole, Vector3 lens, float yaw, Lens shape, float size,
            Color colour, float range, float intensity, float spotAngle, bool light, bool flicker)
        {
            var go = new GameObject("StreetLamp");
            go.transform.SetParent(town.World, false);
            go.transform.localPosition = foot;
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, pole.y * 0.5f, 0f);
            box.size = pole;

            // The lens (and an editor build's light) on one object, pitched to look down: the lens meshes are made lying
            // that way.
            var head = new GameObject("Lens");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = lens - foot;
            head.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            head.AddComponent<MeshFilter>().sharedMesh = LensMesh(shape, size);
            var r = head.AddComponent<MeshRenderer>();
            Material[] states = LensStates(colour);
            r.sharedMaterial = states[2];
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;

            Light spot = null;
            if (light && !Application.isPlaying)
            {
                spot = head.AddComponent<Light>();
                Setup(spot);
                Shine(spot, colour, range, intensity, spotAngle);
                town.LightCount++;
            }

            StreetLamp lamp = go.AddComponent<StreetLamp>();
            lamp._light = spot;
            lamp._head = head.transform;
            lamp._lens = r;
            lamp._states = states;
            lamp._colour = colour;
            lamp._intensity = intensity;
            lamp._range = range;
            lamp._angle = spotAngle;
            lamp._flicker = flicker;
            lamp._phase = Mathf.Repeat(foot.x * 0.37f + foot.z * 0.61f, 100f);
            return lamp;
        }

        // What every street light shares: a spot, no shadows until the driver says, on every rendering layer (the
        // survivor's too, see VisionRig).
        private static void Setup(Light spot)
        {
            spot.type = LightType.Spot;
            spot.shadows = LightShadows.None;
            spot.shadowBias = 0.05f;
            spot.shadowNormalBias = 0.4f;
            spot.shadowNearPlane = 0.2f;
            UniversalAdditionalLightData data = spot.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.renderingLayers = uint.MaxValue;
            if (Application.isPlaying)
                data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
        }

        private static void Shine(Light spot, Color colour, float range, float intensity, float angle)
        {
            spot.color = colour;
            spot.range = range;
            spot.intensity = intensity;
            spot.spotAngle = angle;
            spot.innerSpotAngle = angle * 0.45f;
        }

        // The light the lamp shines with right now: its own (an editor build) or the one lent from the pool.
        private Light Shining => _light != null ? _light : _lent;
        private float Power => _dim ? DimLevel : 1f;

        /// <summary>True when <paramref name="point"/> is in the pool of a lamp that's lit right now (walls aside: pair it
        /// with a line-of-sight test), like <see cref="RoomLight.IsPointLit"/>.</summary>
        public static bool IsPointLit(Vector3 point)
        {
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp l = s_All[i];
                if (l == null || !l._lit || l._level < 0.3f) continue;
                Light shining = l.Shining;
                if (shining == null || !shining.enabled) continue;
                Vector3 at = shining.transform.position;
                if (point.y > at.y) continue;
                float reach = shining.range * 0.55f;
                float dx = point.x - at.x, dz = point.z - at.z;
                if (dx * dx + dz * dz <= reach * reach) return true;
            }
            return false;
        }

        #region Looks
        // The lens dark, dimmed (a flicker's dip) and lit, for a light colour.
        private static Material[] LensStates(Color colour)
        {
            Material on = VisionAssets.BulbMaterial(new Color(colour.r * 3f, colour.g * 3f, colour.b * 3f, 1f));
            if (s_LensStates.TryGetValue(on, out Material[] states) && states[0] != null && states[1] != null) return states;
            if (s_DeadLens == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                s_DeadLens = new Material(sh) { name = "StreetLampLensDark", hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
                if (s_DeadLens.HasProperty("_BaseColor")) s_DeadLens.SetColor("_BaseColor", new Color(0.36f, 0.36f, 0.34f));
                if (s_DeadLens.HasProperty("_Smoothness")) s_DeadLens.SetFloat("_Smoothness", 0.8f);
            }
            Material dim = VisionAssets.BulbMaterial(new Color(colour.r * 0.8f, colour.g * 0.8f, colour.b * 0.8f, 1f));
            states = new[] { s_DeadLens, dim, on };
            s_LensStates[on] = states;
            return states;
        }

        // A shared lens mesh per shape and size (made outside any streamed piece's mesh list, so it outlives them).
        private static Mesh LensMesh(Lens shape, float size)
        {
            int key = (int)shape * 1000 + Mathf.RoundToInt(size * 100f);
            if (s_LensMeshes.TryGetValue(key, out Mesh mesh) && mesh != null) return mesh;
            List<Mesh> created = RaidMesh.CreatedMeshes;
            RaidMesh.CreatedMeshes = null;
            try
            {
                var m = new RaidMesh();
                Surf any = DecorPalette.Matte(Color.white);
                // Lying as the lens object is pitched: its local y runs out along the arm, its local z points down.
                switch (shape)
                {
                    case Lens.Cobra: m.Ellipsoid(Vector3.zero, new Vector3(size * 0.85f, size * 1.6f, size * 0.32f), any, 10, 5); break;
                    case Lens.Globe: m.Ellipsoid(Vector3.zero, new Vector3(size, size, size * 1.35f), any, 10, 6); break;
                    default: m.Ellipsoid(Vector3.zero, new Vector3(size, size, size * 0.45f), any, 10, 4); break;
                }
                mesh = m.ToMesh("StreetLampLens", Vector3.zero);
                mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            finally
            {
                RaidMesh.CreatedMeshes = created;
            }
            s_LensMeshes[key] = mesh;
            return mesh;
        }
        #endregion

        #region Being one of the lamps
        protected override void OnEnable()
        {
            base.OnEnable();
            s_All.Add(this);
            if (s_Driver == null) s_Driver = new GameObject("StreetLamps").AddComponent<StreetLampDriver>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_All.Remove(this);
            _lent = null;
            _lit = false;
        }

        private void Awake()
        {
            _switch = c => Switch(c, !_on);
            _toggleFlicker = c => SetFlicker(c, !_flicker);
            _dimLamp = c => SetDim(c, !_dim);
            _blockOn = c => SwitchBlock(c, true);
            _blockOff = c => SwitchBlock(c, false);
            _blockDim = c => SetBlockDim(c, !_dim);
        }

        private string Key => _key ??= KeyFor("streetlamp", transform.position);

        // First seen by the driver (the piece it's in moved into place by then): back as the survivor left it.
        private void Ready()
        {
            _ready = true;
            if (s_Changed.TryGetValue(Key, out int state)) Apply(state);
        }

        private void Apply(int state)
        {
            _on = (state & OnBit) != 0;
            _flicker = (state & FlickerBit) != 0;
            _dim = (state & DimBit) != 0;
        }

        private void Changed() => s_Changed[Key] = (_on ? OnBit : 0) | (_flicker ? FlickerBit : 0) | (_dim ? DimBit : 0);

        public override string DisplayName => "Street lamp";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.2f;
        public override bool IsAvailable => base.IsAvailable && _lens != null;
        public override string SaveKey => Key;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_on ? "Turn off street lamp" : "Turn on street lamp", _switch));
            if (_on) into.Add(new InteractAction(_flicker ? "Turn off flicker" : "Turn on flicker", _toggleFlicker));
            if (_on) into.Add(new InteractAction(_dim ? "Brighten street lamp" : "Dim street lamp", _dimLamp));
            into.Add(_on ? new InteractAction("Turn off block lamps", _blockOff) : new InteractAction("Turn on block lamps", _blockOn));
            into.Add(new InteractAction(_dim ? "Brighten block lamps" : "Dim block lamps", _blockDim));
        }

        private void Switch(InteractContext ctx, bool on)
        {
            _on = on;
            Changed();
            MakeNoise(ClickNoise);
            ctx.Notify(on ? "The street lamp clicks on." : "The street lamp clicks off.");
            s_NextScan = 0f;
        }

        private void SetFlicker(InteractContext ctx, bool flicker)
        {
            _flicker = flicker;
            Changed();
            MakeNoise(ClickNoise);
            ctx.Notify(flicker ? "The lamp begins to flicker." : "The lamp steadies.");
            s_NextScan = 0f;
        }

        private void SetDim(InteractContext ctx, bool dim)
        {
            _dim = dim;
            Changed();
            MakeNoise(ClickNoise);
            ctx.Notify(dim ? "The street lamp dims to a low glow." : "The street lamp brightens.");
            s_NextScan = 0f;
        }

        private void SwitchBlock(InteractContext ctx, bool on)
        {
            float reach = BlockRadius * BlockRadius;
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp lamp = s_All[i];
                if (lamp == null || (lamp.transform.position - transform.position).sqrMagnitude > reach) continue;
                lamp._on = on;
                lamp.Changed();
            }
            MakeNoise(ClickNoise);
            ctx.Notify(on ? "All nearby street lamps click on." : "All nearby street lamps click off.");
            s_NextScan = 0f;
        }

        private void SetBlockDim(InteractContext ctx, bool dim)
        {
            float reach = BlockRadius * BlockRadius;
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp lamp = s_All[i];
                if (lamp == null || (lamp.transform.position - transform.position).sqrMagnitude > reach) continue;
                lamp._dim = dim;
                lamp.Changed();
            }
            MakeNoise(ClickNoise);
            ctx.Notify(dim ? "The block's street lamps dim together." : "The block's street lamps brighten.");
            s_NextScan = 0f;
        }

        public override void CaptureState(FixtureSave save) => save.State = (_on ? OnBit : 0) | (_flicker ? FlickerBit : 0) | (_dim ? DimBit : 0);

        public override void RestoreState(FixtureSave save)
        {
            Apply(save.State);
            Changed();
            s_NextScan = 0f;
        }
        #endregion

        #region The driver's work
        /// <summary>Forgets the lamps switched on the last map (a new map starts).</summary>
        internal static void ForgetSwitches() => s_Changed.Clear();

        // Domain reload is off in the editor: a new play session mustn't start with the last one's switches.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSwitches() => s_Changed.Clear();

        private static float s_NextScan;
        private static bool s_Night = true;
        private static readonly StreetLamp[] s_Nearest = new StreetLamp[MaxLit];
        private static readonly float[] s_NearestSqr = new float[MaxLit];
        private static readonly Light[] s_Pool = new Light[MaxLit];
        private static Transform s_Survivor;

        /// <summary>Once a frame, from the driver.</summary>
        internal static void Tick()
        {
            float now = Time.time;
            Blinkers.Tick(now);
            if (now >= s_NextScan || s_NextScan - now > 1f)
            {
                s_NextScan = now + 0.25f;
                Scan();
            }
            // The failing lamps among the lit ones.
            for (int i = 0; i < MaxLit; i++)
            {
                StreetLamp l = s_Nearest[i];
                if (l == null || !l._lit || !l._flicker) continue;
                l.Flicker(now);
            }
        }

        private static void Scan()
        {
            TimeOfDay day = TimeOfDay.Instance;
            // Photocells: on at dusk, off at dawn, with a little slack either way.
            if (day != null) s_Night = s_Night ? day.LightLevel < 0.55f : day.LightLevel < 0.45f;
            bool have = Survivor(out Vector3 centre);
            for (int i = 0; i < MaxLit; i++)
            {
                s_Nearest[i] = null;
                s_NearestSqr[i] = float.MaxValue;
            }
            float reach = LightReach * LightReach;
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp l = s_All[i];
                if (l == null) continue;
                if (!l._ready) l.Ready();
                l._lit = false;
                bool burning = s_Night && l._on;
                if (!burning)
                {
                    l._level = 0f;
                    l.Show(0);
                }
                else
                {
                    if (!l._flicker) l._level = 1f;
                    l.Show(LensFor((l._flicker ? l._level : 1f) * l.Power));
                }
                if (!burning || !have || l._head == null) continue;
                float d = (l._head.position - centre).sqrMagnitude;
                if (d <= reach) Insert(l, d);
            }
            bool shadow = s_NearestSqr[0] <= ShadowReach * ShadowReach;
            // The lamps' own lights (an editor build's): on only for the nearest.
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp l = s_All[i];
                if (l == null) continue;
                l._lent = null;
                if (l._light == null) continue;
                int slot = System.Array.IndexOf(s_Nearest, l);
                bool lit = slot >= 0;
                if (l._light.enabled != lit) l._light.enabled = lit;
                LightShadows want = lit && slot == 0 && shadow ? LightShadows.Soft : LightShadows.None;
                if (l._light.shadows != want) l._light.shadows = want;
                if (lit && !l._flicker) l._light.intensity = l._intensity * l.Power;
            }
            // The pool's lights lent to the nearest lamps that have none of their own.
            for (int i = 0; i < MaxLit; i++)
            {
                StreetLamp l = s_Nearest[i];
                if (l != null) l._lit = true;
                Light pool = Pool(i, l != null && l._light == null);
                if (pool == null) continue;
                if (l == null || l._light != null)
                {
                    if (pool.enabled) pool.enabled = false;
                    continue;
                }
                l._lent = pool;
                pool.transform.SetPositionAndRotation(l._head.position, l._head.rotation);
                Shine(pool, l._colour, l._range, l._intensity * (l._flicker ? l._level : 1f) * l.Power, l._angle);
                LightShadows want = i == 0 && shadow ? LightShadows.Soft : LightShadows.None;
                if (pool.shadows != want) pool.shadows = want;
                if (!pool.enabled) pool.enabled = true;
            }
        }

        // The pool's light i, made under the driver the first time it's wanted (play only: an editor build lights its own).
        private static Light Pool(int i, bool want)
        {
            Light l = s_Pool[i];
            if (l != null || !want || !Application.isPlaying || s_Driver == null) return l;
            var go = new GameObject("StreetLight " + i);
            go.transform.SetParent(s_Driver.transform, false);
            l = go.AddComponent<Light>();
            Setup(l);
            l.enabled = false;
            s_Pool[i] = l;
            return l;
        }

        private static void Insert(StreetLamp l, float sqr)
        {
            for (int i = 0; i < MaxLit; i++)
            {
                if (sqr >= s_NearestSqr[i]) continue;
                for (int j = MaxLit - 1; j > i; j--)
                {
                    s_Nearest[j] = s_Nearest[j - 1];
                    s_NearestSqr[j] = s_NearestSqr[j - 1];
                }
                s_Nearest[i] = l;
                s_NearestSqr[i] = sqr;
                return;
            }
        }

        // A failing lamp: a shimmer, sudden dips and now and then a few seconds dark.
        private void Flicker(float now)
        {
            float t = now + _phase;
            float level = 1f - 0.15f * Mathf.PerlinNoise(_phase, t * 6f);
            float outage = Mathf.PerlinNoise(t * 0.35f, _phase * 0.3f);
            if (outage > 0.74f) level = 0f;
            else if (Mathf.PerlinNoise(t * 2.7f, _phase + 7f) > 0.8f) level *= 0.35f;
            _level = level;
            Light shining = Shining;
            if (shining != null) shining.intensity = _intensity * level * Power;
            Show(LensFor(level));
        }

        private static int LensFor(float level) => level > 0.6f ? 2 : level > 0.15f ? 1 : 0;

        private void Show(int state)
        {
            if (_shown == state || _lens == null || _states == null) return;
            _shown = state;
            _lens.sharedMaterial = _states[state];
        }

        private static bool Survivor(out Vector3 position)
        {
            if (s_Survivor == null)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.Player != null) s_Survivor = gm.Player.transform;
                else if (Camera.main != null) s_Survivor = Camera.main.transform;
            }
            position = s_Survivor != null ? s_Survivor.position : default;
            return s_Survivor != null;
        }
        #endregion
    }

    /// <summary>Works every <see cref="StreetLamp"/>, holds the pool of street lights it lends them, and blinks the
    /// <see cref="Blinkers"/>, once a frame; made with the first lamp, gone with the scene.</summary>
    internal sealed class StreetLampDriver : MonoBehaviour
    {
        private void Update() => StreetLamp.Tick();
    }

    /// <summary>
    /// The map's blinking lights, each a shared unlit material the driver flips: traffic signals left on night flash
    /// (amber on the main road, red on the side roads, in turn, once a second) and the red aviation beacons on masts and
    /// water towers. Built maps show them lit until the driver runs.
    /// </summary>
    internal static class Blinkers
    {
        private static Material s_Amber, s_Red, s_Beacon;
        private static readonly Color AmberOn = new Color(3.2f, 1.9f, 0.25f, 1f), RedOn = new Color(3.2f, 0.25f, 0.15f, 1f);
        private static readonly Color BeaconOn = new Color(4.5f, 0.35f, 0.2f, 1f);
        private static readonly Color AmberOff = new Color(0.30f, 0.20f, 0.05f, 1f), RedOff = new Color(0.28f, 0.05f, 0.04f, 1f);
        private static int s_Phase = -1;

        /// <summary>A flashing amber signal lens.</summary>
        internal static Material Amber => s_Amber != null ? s_Amber : s_Amber = Make("SignalAmber", AmberOn);
        /// <summary>A flashing red signal lens.</summary>
        internal static Material Red => s_Red != null ? s_Red : s_Red = Make("SignalRed", RedOn);
        /// <summary>An aviation beacon.</summary>
        internal static Material Beacon => s_Beacon != null ? s_Beacon : s_Beacon = Make("Beacon", BeaconOn);

        private static Material Make(string name, Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var m = new Material(sh) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            Set(m, c);
            return m;
        }

        /// <summary>Flips the lenses for the time (allocation free).</summary>
        internal static void Tick(float now)
        {
            int phase = (int)(now * 2f) & 3;
            if (phase == s_Phase) return;
            s_Phase = phase;
            bool amber = (phase & 1) == 0;
            Set(s_Amber, amber ? AmberOn : AmberOff);
            Set(s_Red, amber ? RedOff : RedOn);
            Set(s_Beacon, phase == 0 ? BeaconOn : RedOff);
        }

        private static void Set(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else m.color = c;
        }
    }
}
