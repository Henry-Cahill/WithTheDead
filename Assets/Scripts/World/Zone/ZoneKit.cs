using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Shared pieces for the zone overlays: the extra materials they need (chain-link, light beams, steam and smoke),
    /// chunked meshes for scattered geometry, particle emitters and flickering flame lights.
    /// </summary>
    internal static class ZoneKit
    {
        /// <summary>Creates a matte surface with the given colour.</summary>
        /// <summary>Creates a matte surface with the given colour.</summary>
        public static Surf M(Color c) => DecorPalette.Matte(c);
        /// <summary>Creates a glossy surface with the given colour.</summary>
        /// <summary>Creates a glossy surface with the given colour.</summary>
        public static Surf G(Color c) => DecorPalette.Gloss(c);
        /// <summary>Creates a metal surface with the given colour.</summary>
        /// <summary>Creates a metal surface with the given colour.</summary>
        public static Surf Mt(Color c) => DecorPalette.Metal(c);
        /// <summary>Creates a glass surface with the given colour.</summary>
        /// <summary>Creates a glass surface with the given colour.</summary>
        public static Surf Gs(Color c) => DecorPalette.Glass(c);
        /// <summary>Creates a glowing surface with the given colour.</summary>
        /// <summary>Creates a glowing surface with the given colour.</summary>
        public static Surf Gl(Color c) => DecorPalette.Glow(c);
        /// <summary>Generates a random float between <paramref name="a"/> and <paramref name="b"/>.</summary>
        /// <summary>Generates a random float between two bounds.</summary>
        public static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        /// <summary>Returns true with probability <paramref name="p"/>.</summary>
        /// <summary>Returns true with given probability.</summary>
        public static bool Chance(System.Random rng, double p) => rng.NextDouble() < p;
        /// <summary>Vary the colour <paramref name="c"/> slightly with randomness.</summary>
        /// <summary>Varies a colour by a small random amount.</summary>
        public static Color V(Color c, System.Random rng, float amount = 0.08f) => DecorPalette.Vary(c, amount, rng);

        public static readonly Color Rust = new Color(0.40f, 0.21f, 0.11f);
        public static readonly Color RustDark = new Color(0.26f, 0.15f, 0.09f);
        public static readonly Color Concrete = new Color(0.55f, 0.55f, 0.52f);
        public static readonly Color ConcreteDark = new Color(0.38f, 0.38f, 0.36f);
        public static readonly Color Galvanised = new Color(0.56f, 0.58f, 0.60f);
        public static readonly Color WetWood = new Color(0.30f, 0.24f, 0.17f);
        public static readonly Color Bark = new Color(0.19f, 0.16f, 0.13f);
        public static readonly Color WardenOlive = new Color(0.30f, 0.34f, 0.24f);

        #region Objects
        /// <summary>A child object at a world position (unrotated), on the Default layer.</summary>
        /// <summary>Creates a child GameObject at a given world position on the Default layer.</summary>
        public static Transform Child(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            return go.transform;
        }

        /// <summary>A solid box collider at a world placement under <paramref name="parent"/>.</summary>
        /// <summary>Creates a solid box collider at a world position under the given parent.</summary>
        public static BoxCollider Solid(Transform parent, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var go = new GameObject("Solid");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, rotation);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            return bc;
        }

        /// <summary>Keeps an object (and everything under it) out of the navmesh bake.</summary>
        /// <summary>Marks a GameObject to be ignored by NavMesh baking.</summary>
        public static void NoBake(GameObject go)
        {
            if (go == null || go.GetComponent<NavMeshModifier>() != null) return;
            var mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
        }

        /// <summary>Emits a mesh with its origin at <paramref name="origin"/> and returns the renderer (null when empty).</summary>
        /// <summary>Emits a mesh from a RaidMesh and returns its renderer, or null if empty.</summary>
        public static MeshRenderer Emit(RaidMesh m, string name, Transform parent, Vector3 origin, ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            if (m == null || m.IsEmpty) return null;
            GameObject go = m.Emit(name, parent, origin, shadows);
            return go != null ? go.GetComponent<MeshRenderer>() : null;
        }

        /// <summary>Recursively sets the Unity layer on a transform and all its children.</summary>
        public static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }
        #endregion

        #region Materials
        private static Material s_ChainLink, s_Beam, s_Steam, s_Smoke, s_Embers, s_Disc;
        private static Texture2D s_ChainTex;
        private static float s_SmokeTintAt = -1f;

        /// <summary>See-through chain-link mesh (lit, alpha-blended, both faces drawn), tiled in world metres.</summary>
        public static Surf ChainLink
        {
            get
            {
                if (s_ChainLink == null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                    if (sh == null) sh = Shader.Find("Sprites/Default");
                    var mat = new Material(sh) { name = "ZoneChainLink", hideFlags = HideFlags.DontUnloadUnusedAsset };
                    Set(mat, "_Surface", 1f);
                    Set(mat, "_Blend", 0f);
                    Set(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
                    Set(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    Set(mat, "_SrcBlendAlpha", (float)BlendMode.One);
                    Set(mat, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    Set(mat, "_ZWrite", 0f);
                    Set(mat, "_Cull", 0f);
                    Set(mat, "_Smoothness", 0.35f);
                    Set(mat, "_Metallic", 0.5f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    mat.SetShaderPassEnabled("ShadowCaster", false);
                    mat.SetShaderPassEnabled("DepthOnly", false);
                    mat.SetShaderPassEnabled("DepthNormals", false);
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", ChainTexture());
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", ChainTexture());
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    s_ChainLink = mat;
                }
                return new Surf(s_ChainLink, Vector2.zero, 1f / 0.42f);
            }
        }

        private static void Set(Material m, string p, float v)
        {
            if (m.HasProperty(p)) m.SetFloat(p, v);
        }

        private static Texture2D ChainTexture()
        {
            if (s_ChainTex != null) return s_ChainTex;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "ZoneChainLink", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int a = (x + y) % 32, b = (x - y + 64) % 32;
                    bool wire = a < 3 || b < 3;
                    byte g = (byte)(wire ? 150 + ((x * 7 + y * 3) % 30) : 0);
                    px[y * size + x] = new Color32(g, (byte)(g + 4), (byte)(g + 8), (byte)(wire ? 255 : 0));
                }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            s_ChainTex = tex;
            return tex;
        }

        // Sprites/Default honours vertex colour and draws both faces with no fog: a beam seen through the murk.
        private static Shader GlowShader()
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Unlit/Transparent");
            return s;
        }

        /// <summary>The soft, see-through material of a searchlight's beam (vertex colour sets its fade).</summary>
        public static Material BeamMaterial()
        {
            if (s_Beam == null) s_Beam = new Material(GlowShader()) { name = "ZoneBeam", color = Color.white, hideFlags = HideFlags.DontUnloadUnusedAsset };
            return s_Beam;
        }

        /// <summary>A faint see-through disc for a spinning rotor (tinted per use through vertex colour).</summary>
        public static Material DiscMaterial()
        {
            if (s_Disc == null) s_Disc = new Material(GlowShader()) { name = "ZoneRotorDisc", color = Color.white, hideFlags = HideFlags.DontUnloadUnusedAsset };
            return s_Disc;
        }

        /// <summary>Steam: pale puffs, unlit but dimmed with the ambient light so they sink into the night.</summary>
        public static Material SteamMaterial()
        {
            if (s_Steam == null)
            {
                s_Steam = new Material(VisionAssets.BreathMaterial()) { name = "ZoneSteam", hideFlags = HideFlags.DontUnloadUnusedAsset };
                if (s_Steam.HasProperty("_BaseMap")) s_Steam.SetTexture("_BaseMap", VisionAssets.Puff());
            }
            TintByAmbient();
            return s_Steam;
        }

        /// <summary>Smoke: darker puffs, dimmed the same way.</summary>
        public static Material SmokeMaterial()
        {
            if (s_Smoke == null)
            {
                s_Smoke = new Material(VisionAssets.BreathMaterial()) { name = "ZoneSmoke", hideFlags = HideFlags.DontUnloadUnusedAsset };
                if (s_Smoke.HasProperty("_BaseMap")) s_Smoke.SetTexture("_BaseMap", VisionAssets.Puff());
            }
            TintByAmbient();
            return s_Smoke;
        }

        /// <summary>Glowing sparks and embers (unlit, full brightness).</summary>
        public static Material EmberMaterial()
        {
            if (s_Embers == null)
            {
                s_Embers = new Material(GlowShader()) { name = "ZoneEmbers", color = Color.white, hideFlags = HideFlags.DontUnloadUnusedAsset };
                s_Embers.mainTexture = VisionAssets.SoftDot();
            }
            return s_Embers;
        }

        /// <summary>Re-tints the steam and smoke to the scene's ambient light (call now and then; cheap).</summary>
        public static void TintByAmbient()
        {
            if (Time.time - s_SmokeTintAt < 0.5f && s_SmokeTintAt >= 0f) return;
            s_SmokeTintAt = Time.time;
            Color a = RenderSettings.ambientLight;
            float k = Mathf.Clamp(0.25f + (a.r + a.g + a.b) * 1.6f, 0.25f, 1f);
            if (s_Steam != null && s_Steam.HasProperty("_BaseColor")) s_Steam.SetColor("_BaseColor", new Color(k, k, k, 1f));
            if (s_Smoke != null && s_Smoke.HasProperty("_BaseColor")) s_Smoke.SetColor("_BaseColor", new Color(k * 0.55f, k * 0.55f, k * 0.55f, 1f));
        }
        #endregion

        #region Beams
        private static Mesh s_Cone;

        /// <summary>A unit beam cone along +Z (apex at the origin, radius 1 at z = 1) fading from <paramref name="apexAlpha"/> to nothing.</summary>
        public static Mesh BeamCone()
        {
            if (s_Cone != null) return s_Cone;
            const int sides = 18, rings = 4;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings;
                float alpha = Mathf.Pow(1f - t, 1.6f);
                for (int s = 0; s <= sides; s++)
                {
                    float a = (float)s / sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * t, Mathf.Sin(a) * t, t));
                    cols.Add(new Color(1f, 1f, 1f, alpha));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * (sides + 1) + s, b = a + sides + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            var mesh = new Mesh { name = "ZoneBeamCone" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(new Vector3(0f, 0f, 0.5f), new Vector3(2f, 2f, 1f));
            s_Cone = mesh;
            return mesh;
        }

        /// <summary>A beam cone renderer under <paramref name="parent"/> (pointing along its +Z): <paramref name="length"/>
        /// metres long, spreading at <paramref name="spotAngle"/>, tinted <paramref name="tint"/> (alpha = strength).</summary>
        public static MeshRenderer Beam(Transform parent, float length, float spotAngle, Color tint)
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(parent, false);
            float r = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * length;
            go.transform.localScale = new Vector3(r, r, length);
            go.AddComponent<MeshFilter>().sharedMesh = BeamCone();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = BeamMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", tint);
            mr.SetPropertyBlock(block);
            // Add a subtle glowing tip at the far end of the beam
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "BeamTip";
            tip.transform.SetParent(go.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, length);
            tip.transform.localScale = Vector3.one * 0.05f;
            // Remove the default collider
            Object.Destroy(tip.GetComponent<Collider>());
            // Use the same beam material but make it brighter
            var tipRenderer = tip.GetComponent<MeshRenderer>();
            tipRenderer.sharedMaterial = BeamMaterial();
            var tipBlock = new MaterialPropertyBlock();
            tipBlock.SetColor("_Color", tint * 2f);
            tipRenderer.SetPropertyBlock(tipBlock);
            NoBake(go);
            return mr;
        }
        #endregion

        #region Particles
        /// <summary>A rising plume (steam from a vent, smoke from a stack or a fire) at a world point.</summary>
        public static ParticleSystem Plume(Transform parent, Vector3 at, bool smoke, float size, float rate, float rise, Color tint)
        {
            var go = new GameObject(smoke ? "Smoke" : "Steam");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(-90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float life = Mathf.Clamp(4f + size * 1.5f, 4f, 14f);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = life;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = tint;
            main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(rate * life * 1.2f), 8, 220);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.01f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = Mathf.Max(0.05f, size * 0.15f);

            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.4f));

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0.1f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.25f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = smoke ? SmokeMaterial() : SteamMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.AddComponent<ZoneSmokeTint>();
            ps.Play();
            return ps;
        }

        /// <summary>A small fire: glowing embers drifting up, a lick of smoke above and a flickering warm light.</summary>
        public static void Fire(Transform parent, Vector3 at, float scale)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(-90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * scale, 1.2f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.32f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.82f, 0.35f, 0.8f));
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 26f * scale;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.25f * scale;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = EmberMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();

            Plume(parent, at + Vector3.up * 0.6f * scale, true, 0.9f * scale, 3f, 0.7f, new Color(0.35f, 0.33f, 0.31f, 0.45f));
            ZoneFlameLight.Spawn(parent, at + Vector3.up * 0.5f * scale, new Color(1f, 0.58f, 0.26f), 7f * scale, 2.4f, 0.35f);
        }
        #endregion
    }

    /// <summary>Keeps the zone's steam and smoke dimmed to the night (one shared material tint, refreshed twice a second).</summary>
    internal sealed class ZoneSmokeTint : MonoBehaviour
    {
        private void Update() => ZoneKit.TintByAmbient();
    }

    /// <summary>
    /// Scattered geometry (trees, undergrowth, debris) merged into one mesh per square chunk, so hundreds of pieces
    /// cost a handful of renderers that the camera can still cull. Keeps the emitted renderers for whoever needs them
    /// (the canopy fade).
    /// </summary>
    internal sealed class ZoneChunks
    {
        private readonly float _size;
        private readonly Dictionary<long, RaidMesh> _meshes = new Dictionary<long, RaidMesh>();
        private readonly List<long> _order = new List<long>();

        /// <summary>Creates a ZoneChunks helper for a given chunk size.</summary>
        public ZoneChunks(float size) { _size = Mathf.Max(2f, size); }

        private long Key(Vector3 p)
        {
            long x = Mathf.FloorToInt(p.x / _size), z = Mathf.FloorToInt(p.z / _size);
            return (x + 100000L) * 1000003L + (z + 100000L);
        }

        /// <summary>The mesh of the chunk holding <paramref name="p"/>.</summary>
        public RaidMesh At(Vector3 p)
        {
            long k = Key(p);
            if (!_meshes.TryGetValue(k, out RaidMesh m))
            {
                m = new RaidMesh();
                _meshes[k] = m;
                _order.Add(k);
            }
            return m;
        }

        /// <summary>Emits every chunk under <paramref name="parent"/>; returns the renderers in build order.</summary>
        public List<MeshRenderer> Emit(Transform parent, string name, ShadowCastingMode shadows)
        {
            var list = new List<MeshRenderer>(_order.Count);
            for (int i = 0; i < _order.Count; i++)
            {
                RaidMesh m = _meshes[_order[i]];
                if (m.IsEmpty) continue;
                long k = _order[i];
                float cx = ((k / 1000003L) - 100000L + 0.5f) * _size, cz = ((k % 1000003L) - 100000L + 0.5f) * _size;
                MeshRenderer r = ZoneKit.Emit(m, name, parent, new Vector3(cx, 0f, cz), shadows);
                if (r != null) list.Add(r);
            }
            return list;
        }
    }

    /// <summary>
    /// A point light that breathes like a flame (a campfire, candles, an oil lantern): a smooth flicker with no
    /// blackouts, switched off beyond <see cref="CullDistance"/> of the camera. Every one is listed in <see cref="All"/>
    /// so the uncanny events can make them gutter (<see cref="Disturb"/>).
    /// </summary>
    public sealed class ZoneFlameLight : MonoBehaviour
    {
        public static readonly List<ZoneFlameLight> All = new List<ZoneFlameLight>();
        public const float CullDistance = 48f;

        /// <summary>Gets the Light component for this flame light.</summary>
        public Light Light { get; private set; }
        /// <summary>Base intensity for the flame light.</summary>
        public float Intensity = 1.5f;
        /// <summary>0 steady .. 1 wild.</summary>
        /// <summary>Flicker amount (0 steady … 1 wild).</summary>
        public float Flicker = 0.3f;
        /// <summary>Multiplies the output (1 = normal); the uncanny events dip it.</summary>
        /// <summary>Multiplier applied by events (1 = normal).</summary>
        public float Disturb = 1f;

        private float _seed;
        private float _cullCheck;
        private bool _culled;

        public static ZoneFlameLight Spawn(Transform parent, Vector3 position, Color color, float range, float intensity, float flicker)
        {
            var go = new GameObject("FlameLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            var f = go.AddComponent<ZoneFlameLight>();
            f.Light = light;
            f.Intensity = intensity;
            f.Flicker = flicker;
            return f;
        }

        private void Awake()
        {
            _seed = Random.value * 100f;
            if (Light == null) Light = GetComponent<Light>();
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Update()
        {
            if (Light == null) return;
            _cullCheck -= Time.deltaTime;
            if (_cullCheck <= 0f)
            {
                _cullCheck = 0.5f + Random.value * 0.2f;
                Camera cam = Camera.main;
                Vector3 eye = cam != null ? cam.transform.position : transform.position;
                if (Actor.LocalPlayer != null) eye = Actor.LocalPlayer.transform.position;
                _culled = (eye - transform.position).sqrMagnitude > CullDistance * CullDistance;
            }
            if (_culled)
            {
                if (Light.enabled) Light.enabled = false;
                return;
            }
            float t = Time.time * 7f + _seed;
            float n = Mathf.PerlinNoise(t, _seed) * 0.7f + Mathf.PerlinNoise(t * 2.3f, _seed + 5f) * 0.3f;
            float level = Mathf.Lerp(1f, n * 1.3f, Flicker) * Mathf.Max(0f, Disturb);
            Light.intensity = Intensity * level;
            bool on = level > 0.02f;
            if (Light.enabled != on) Light.enabled = on;
        }
    }

    /// <summary>
    /// A set piece under construction: a root under the baked world (marked for the isometric cutaway) with a local
    /// frame at the site's centre turned by its yaw, and three meshes — solid (baked, casts shadows), upper (roofs,
    /// platforms and overhangs: drawn but kept out of the navmesh bake) and flat (ground decals, no shadows).
    /// </summary>
    internal sealed class SiteBuilder
    {
        public readonly Transform Root;
        public readonly Vector3 Center;
        public readonly float Yaw;
        public readonly Quaternion Rot;
        public readonly System.Random Rng;
        public readonly RaidMesh Solid = new RaidMesh();
        public readonly RaidMesh Upper = new RaidMesh();
        public readonly RaidMesh Flat = new RaidMesh();
        private Transform _colliders;
        private readonly List<MeshRenderer> _roofs = new List<MeshRenderer>();

        /// <summary>Roof meshes drawn here go to <see cref="CeilingVisibility"/> (hidden from the isometric view).</summary>
        public readonly RaidMesh Roof = new RaidMesh();

        /// <summary>Creates a SiteBuilder for constructing a zone site.</summary>
        public SiteBuilder(Transform parent, string name, Vector3 center, float yaw, System.Random rng, bool cutaway = true)
        {
            Center = new Vector3(center.x, 0f, center.z);
            Yaw = yaw;
            Rot = Quaternion.Euler(0f, yaw, 0f);
            Rng = rng;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Center, Quaternion.identity);
            Root = go.transform;
            if (cutaway) go.AddComponent<CutawayOccluder>();
            Solid.SetFrame(Center, yaw);
            Upper.SetFrame(Center, yaw);
            Flat.SetFrame(Center, yaw);
            Roof.SetFrame(Center, yaw);
        }

        /// <summary>A local point in world space.</summary>
        /// <summary>Transform a local point to world space.</summary>
        public Vector3 W(float x, float y, float z) => Center + Rot * new Vector3(x, y, z);
        /// <summary>Transform a local vector to world space.</summary>
        public Vector3 W(Vector3 local) => Center + Rot * local;
        /// <summary>A local yaw in world degrees.</summary>
        /// <summary>Adds a local yaw offset to the base yaw.</summary>
        public float WYaw(float localYaw) => Yaw + localYaw;
        /// <summary>Rotates a local vector by the builder's rotation.</summary>
        public Vector3 Dir(Vector3 local) => Rot * local;

        /// <summary>A solid box collider in the local frame (yaw-only local rotation).</summary>
        /// <summary>Adds a solid box collider at a local point with optional yaw.</summary>
        public void Collider(Vector3 localCenter, Vector3 size, float localYaw = 0f)
        {
            if (_colliders == null) _colliders = ZoneKit.Child(Root, "Colliders", Center);
            ZoneKit.Solid(_colliders, W(localCenter), size, Rot * Quaternion.Euler(0f, localYaw, 0f));
        }

        /// <summary>A solid box collider with a full local rotation.</summary>
        /// <summary>Adds a solid box collider at a local point with full rotation.</summary>
        public void Collider(Vector3 localCenter, Vector3 size, Quaternion localRot)
        {
            if (_colliders == null) _colliders = ZoneKit.Child(Root, "Colliders", Center);
            ZoneKit.Solid(_colliders, W(localCenter), size, Rot * localRot);
        }

        /// <summary>Draws into <paramref name="m"/> with the frame moved to a local point and yaw, then puts it back.</summary>
        /// <summary>Draw into a mesh with a temporary frame offset then restore.</summary>
        public void At(RaidMesh m, Vector3 local, float localYaw, System.Action<RaidMesh> draw)
        {
            m.SetFrame(W(local), Yaw + localYaw);
            draw(m);
            m.SetFrame(Center, Yaw);
        }

        /// <summary>A storage fixture (a gun locker, a medicine chest...) whose reach point is a local point.</summary>
        /// <summary>Creates a storage fixture with a given reach point.</summary>
        public StorageFixture Storage(string displayName, string keyKind, Vector3 localReach, ContainerType type, int maxStacks, float emptyChance = 0f)
        {
            Vector3 reach = W(localReach);
            return StorageFixture.Create(Root, displayName, Interactable.KeyFor(keyKind, reach), reach, null, Root, Rng.Next(),
                new StorageOptions
                {
                    Noun = displayName.ToLowerInvariant(),
                    LootType = type,
                    EmptyChance = emptyChance,
                    MaxStacks = maxStacks,
                    RaidBoost = false,
                });
        }

        /// <summary>Emits the meshes. Call once, after drawing.</summary>
        /// <summary>Emits all stored meshes for this builder.</summary>
        public void Finish()
        {
            ZoneKit.Emit(Solid, "Solid", Root, Center, ShadowCastingMode.On);
            MeshRenderer up = ZoneKit.Emit(Upper, "Upper", Root, Center, ShadowCastingMode.On);
            if (up != null) ZoneKit.NoBake(up.gameObject);
            MeshRenderer roof = ZoneKit.Emit(Roof, "Roof", Root, Center, ShadowCastingMode.On);
            if (roof != null)
            {
                ZoneKit.NoBake(roof.gameObject);
                CeilingVisibility.Register(roof);
            }
            MeshRenderer flat = ZoneKit.Emit(Flat, "Ground", Root, Center, ShadowCastingMode.Off);
            if (flat != null) ZoneKit.NoBake(flat.gameObject);
        }
    }
}
