using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A cloud of toxic gas: breathing it without a gas mask (or with a spent filter) sickens and hurts
    /// the survivor; a worn mask burns filter instead (see <see cref="PlayerGear"/>). The raid map spawns
    /// these with <see cref="Spawn"/>; the gear and the HUD read <see cref="ConcentrationAt"/>.
    ///
    /// The cloud is drawn as a slow, churning layer of large soft billboards hugging the ground — a
    /// sickly yellow-green, unlit but dimmed with the ambient light so it sinks into the night instead
    /// of glowing through it — with a faint green light breathing inside. The gas is thickest at the
    /// heart and thins smoothly to nothing at <see cref="Radius"/>.
    /// </summary>
    public class ToxicZone : MonoBehaviour
    {
        public static readonly List<ToxicZone> All = new List<ToxicZone>();

        /// <summary>Radius of the cloud on the ground, in metres.</summary>
        public float Radius = 8f;

        /// <summary>How high the gas layer reaches above the zone's base, in metres (it hugs the ground).</summary>
        public const float CloudHeight = 3.2f;

        private const float GlowIntensity = 0.7f;
        private static readonly Color GasColorA = new Color(0.55f, 0.65f, 0.25f, 0.25f);
        private static readonly Color GasColorB = new Color(0.62f, 0.70f, 0.20f, 0.20f);
        private static readonly Color GlowColor = new Color(0.55f, 0.85f, 0.30f);

        private static Material s_Material;
        private static Texture2D s_Texture;
        private static int s_ColorId;
        private static int s_TintFrame = -1;
        private static int s_LastTintUpdateFrame = -1;
        private float GetGlowFactor(float phase)
        {
            // Compute glow factor per instance each frame for independent pulsation.
            return 0.8f + 0.2f * Mathf.Sin(Time.time * 0.7f + phase);
        }

        private ParticleSystem _particles;
        private Light _glow;
        private float _builtRadius = -1f;
        private float _phase;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); } // now also handled in OnDestroy for safety

        /// <summary>Creates a gas cloud of the given radius centred on the ground at <paramref name="center"/>.</summary>
        public static ToxicZone Spawn(Transform parent, Vector3 center, float radius)
        {
            var go = new GameObject("ToxicZone");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var z = go.AddComponent<ToxicZone>();
            z.Radius = radius;
            return z;
        }

        /// <summary>Gas concentration at a world position, 0 (clean air) .. 1 (the heart of a cloud): a
        /// smooth falloff from the centre to the rim, thinning out above the ground-hugging layer.</summary>
        public static float ConcentrationAt(Vector3 position)
        {
            float best = 0f;
            for (int i = 0; i < All.Count; i++)
            {
                ToxicZone z = All[i];
                if (z == null) continue;
                Vector3 c = z.transform.position;
                float r = Mathf.Max(0.01f, z.Radius);
                float dx = position.x - c.x, dz = position.z - c.z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= r * r) continue;

                float t = Mathf.Sqrt(d2) / r;
                float k = 1f - t * t * (3f - 2f * t);
                float dy = position.y - c.y;
                float top = CloudHeight * 0.6f;
                if (dy > top) k *= 1f - Mathf.Clamp01((dy - top) / (CloudHeight - top));
                else if (dy < -1.5f) k *= 1f - Mathf.Clamp01((-1.5f - dy) / 1.5f);   // a cellar below the cloud
                if (k > best) best = k;
            }
            return Mathf.Clamp01(best);
        }

        // Built in Start rather than Awake: Spawn sets the radius after AddComponent.
        private void Start()
        {
            _phase = Random.value * 10f;
            Build();
        }

        private void Update()
        {
            if (Mathf.Abs(_builtRadius - Radius) > 0.01f) Build();   // resized after spawning
            if (s_LastTintUpdateFrame != Time.frameCount) { UpdateSharedTint(); s_LastTintUpdateFrame = Time.frameCount; }
            if (_glow != null) _glow.intensity = GlowIntensity * GetGlowFactor(_phase);
        }

        private void OnDestroy()
        {
            // Guarantee removal from the global list even if OnDisable wasn’t called.
            All.Remove(this);
            // Destroy any runtime children that were created in Build().
            if (_particles != null) Destroy(_particles.gameObject);
            if (_glow != null) Destroy(_glow.gameObject);
        }

        /// <summary>Builds or rebuilds the particle system and glow based on the current radius.</summary>
        // Build or rebuild the particle system and its glow according to the current radius.
        private void Build()
        {
            _builtRadius = Radius;
            float r = Mathf.Max(0.5f, Radius);

            if (_particles == null)
            {
                var go = new GameObject("Gas");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                _particles = go.AddComponent<ParticleSystem>();
            }
            ParticleSystem ps = _particles;
            // Set particle system to start in a stopped state and clear any existing particles.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Density follows the area: enough big puffs to overlap into one churning layer.
            int count = Mathf.Clamp(Mathf.RoundToInt(r * r * 1.1f), 24, 320);
            float size = Mathf.Clamp(r * 0.5f, 2.2f, 5.5f);
            const float lifetime = 12f;

            // Set up the main particle module: looping, duration, pre‑warm, lifetime, speed, size, rotation, colour, particle limit, etc.
            var main = ps.main;
            main.loop = true;
            main.duration = lifetime;
            main.prewarm = true;   // the cloud is already there when the raid starts
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime * 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(GasColorA, GasColorB);
            main.maxParticles = count;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Configure emission rate based on particle count and lifetime for a steady cloud density.
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = count / lifetime;

            // A flat disc a little above the ground; the jitter spreads the puffs through the layer.
            // Shape a flat disc slightly above ground; jitter spreads particles vertically for a volumetric effect.
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = r * 0.78f;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.position = new Vector3(0f, 1.25f, 0f);
            shape.randomPositionAmount = 0.45f;
            shape.randomDirectionAmount = 0.7f;

            // Slow drift, a lazy swirl about the centre and a little turbulence: the gas churns in place.
            // Velocity over lifetime adds lazy swirl and turbulence, making the gas churn.
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Noise adds subtle random motion to avoid a perfectly uniform look.
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            // Rotation over lifetime gives each puff a slow spin for visual variety.
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

            // Size over lifetime makes puffs grow then shrink, adding depth to the cloud.
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.75f, 1f, 1.2f));

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            // Color over lifetime uses a gradient to fade particles in and out smoothly.
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(fade);

            // Set up the renderer for billboards that face the camera, with appropriate sorting and shadows off.
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 3f;   // big puffs may fill the view when the camera is inside the cloud
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = GasMaterial();

            // Ensure the particle system starts playing after all settings are applied.
            ps.Play();

            // Create a point light to provide a subtle green glow within the cloud, if not already present.
            if (_glow == null)
            {
                var lightGo = new GameObject("GasGlow");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                _glow = lightGo.AddComponent<Light>();
                _glow.type = LightType.Point;
                _glow.color = GlowColor;
                _glow.shadows = LightShadows.None;
            }
            // Set range based on radius, clamped to avoid too small or too large light extents.
            _glow.range = Mathf.Clamp(r * 0.9f, 3f, 14f);
            _glow.intensity = GlowIntensity;
        }

        // The gas is unlit, so it is dimmed by hand with the ambient light (once a frame for every
        // cloud): a full sickly green by day, a murky haze at night instead of a glow in the dark.
        private static void UpdateSharedTint()
        {
            if (s_Material == null || s_TintFrame == Time.frameCount) return;
            s_TintFrame = Time.frameCount;
            Color ambient = RenderSettings.ambientLight;
            float lum = ambient.r * 0.3f + ambient.g * 0.59f + ambient.b * 0.11f;
            float k = Mathf.Clamp(lum / 0.42f, 0.16f, 1f);
            s_Material.SetColor(s_ColorId, new Color(k, k, k, 1f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResetStaticState()
        {
            All.Clear();
            s_Material = null;
            s_Texture = null;
            s_TintFrame = -1;
        }

        // An unlit, alpha-blended particle material over a procedural soft puff (shared by every cloud).
        private static Material GasMaterial()
        {
            if (s_Material != null) return s_Material;
            Texture2D tex = PuffTexture();

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material m;
            if (shader != null)
            {
                m = new Material(shader) { name = "ToxicGas" };
                SetIfPresent(m, "_Surface", 1f);   // transparent
                SetIfPresent(m, "_Blend", 0f);     // alpha blend
                SetIfPresent(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetIfPresent(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetIfPresent(m, "_SrcBlendAlpha", (float)BlendMode.One);
                SetIfPresent(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                SetIfPresent(m, "_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.DisableKeyword("_ALPHAMODULATE_ON");
                m.renderQueue = (int)RenderQueue.Transparent;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                s_ColorId = Shader.PropertyToID("_BaseColor");
            }
            else
            {
                // Sprites/Default is always available, alpha-blends and respects the particle colour.
                m = new Material(Shader.Find("Sprites/Default")) { name = "ToxicGas" };
                m.mainTexture = tex;
                s_ColorId = Shader.PropertyToID("_Color");
            }
            m.SetColor(s_ColorId, Color.white);
            s_Material = m;
            return m;
        }

        private static void SetIfPresent(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }

        // A soft round puff with a little lumpy noise in it, white (the particle colour tints it).
        /// <summary>
        /// Generates a circular soft‑round puff texture with subtle Perlin noise to give the gas particles a lumpy appearance.
        /// </summary>
        // Ensure the procedural puff texture is created once and reused for all cloud instances.
        private static Texture2D PuffTexture()
        {
            if (s_Texture != null) return s_Texture;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true)
            {
                name = "ToxicGasPuff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float edge = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                    edge = edge * edge * (3f - 2f * edge);
                    float lumps = Mathf.PerlinNoise(x * 0.11f + 3.7f, y * 0.11f + 1.3f) * 0.6f
                                + Mathf.PerlinNoise(x * 0.27f + 9.1f, y * 0.27f + 4.2f) * 0.4f;
                    float a = edge * Mathf.Lerp(0.55f, 1f, lumps);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            s_Texture = tex;
            return tex;
        }
    }
}
