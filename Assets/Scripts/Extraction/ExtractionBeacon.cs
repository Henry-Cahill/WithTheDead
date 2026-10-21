using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The in-world beacon for one <see cref="RaidExit"/>: a tall glowing light column that reads across
    /// the dark town, a ring marking the zone on the ground, a flickering flare with a pool of light and
    /// coloured flare smoke drifting up. Green while the exit is open, dim red while it is closed; while
    /// the survivor extracts the ring pulses and a glow fills the zone with the progress. Purely visual
    /// (no colliders, built after the navmesh bake and outside the world root) and drawn with fog-free
    /// sprite materials so the column stays visible from far beyond the fog line. Built and animated by
    /// <see cref="ExtractionManager"/>; plain class, not a component.
    /// </summary>
    public sealed class ExtractionBeacon
    {
        /// <summary>The exit this beacon marks.</summary>
        public readonly RaidExit Exit;
        /// <summary>Root of the beacon's objects (a child of the manager).</summary>
        public readonly GameObject Root;

        private static readonly Color OpenColor = new Color(0.30f, 1.00f, 0.42f);
        private static readonly Color ClosedColor = new Color(0.80f, 0.12f, 0.08f);

        private const float RingLift = 0.08f;        // clear of road decals so the ring never z-fights
        private const float BeamHalfWidth = 0.9f;
        private const float BeamHeight = 60f;

        private static Material s_GlowMat;
        private static Material s_SmokeMat;
        private static Texture2D s_SoftDot;
        private static Mesh s_BeamMesh;
        private static Mesh s_DiscMesh;
        private static MaterialPropertyBlock s_Block;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Transform _root;
        private readonly Transform _beam;
        private readonly Transform _flare;
        private readonly Transform _fill;
        private readonly MeshRenderer _beamRenderer;
        private readonly MeshRenderer _ringRenderer;
        private readonly MeshRenderer _flareRenderer;
        private readonly MeshRenderer _fillRenderer;
        private readonly Light _light;
        private readonly ParticleSystem _smoke;
        private readonly Mesh _ringMesh;
        private readonly float _radius;
        private readonly float _seed;
        private bool _styledOpen;
        private bool _styled;

        /// <summary>Builds the beacon for an exit under <paramref name="parent"/> (null for no exit).</summary>
        public static ExtractionBeacon Build(RaidExit exit, Transform parent)
        {
            return exit == null ? null : new ExtractionBeacon(exit, parent);
        }

        private ExtractionBeacon(RaidExit exit, Transform parent)
        {
            Exit = exit;
            _radius = Mathf.Max(0.5f, exit.Radius);
            _seed = Random.value * 100f;

            Root = new GameObject("ExitBeacon " + (string.IsNullOrEmpty(exit.Name) ? "Exit" : exit.Name));
            _root = Root.transform;
            _root.SetParent(parent, false);
            _root.position = exit.Position;

            _beam = MakePart("Beam", BeamMesh(), Vector3.zero, out _beamRenderer);

            _ringMesh = BuildRadialMesh("ExitRing",
                new[] { 0f, _radius - 0.5f, _radius - 0.14f, _radius },
                new[] { 0.10f, 0.16f, 1f, 0f }, 56, flatOnGround: true);
            MakePart("Ring", _ringMesh, new Vector3(0f, RingLift, 0f), out _ringRenderer);

            // The progress glow: the soft disc laid flat, scaled out to the ring as the extraction fills.
            _fill = MakePart("Progress", DiscMesh(), new Vector3(0f, RingLift + 0.01f, 0f), out _fillRenderer);
            _fill.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _fill.localScale = Vector3.zero;

            _flare = MakePart("Flare", DiscMesh(), new Vector3(0f, 0.45f, 0f), out _flareRenderer);

            var lightGo = new GameObject("FlareLight");
            lightGo.transform.SetParent(_root, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.bounceIntensity = 0f;
            _light.range = Mathf.Max(7f, _radius + 5f);

            _smoke = BuildSmoke(_root);
            ApplyStyle(exit.Open);
        }

        /// <summary>Animates the beacon: faces the column and flare to the camera, flickers the flare and
        /// its light, follows the exit opening/closing, and shows extraction progress when
        /// <paramref name="extractingHere"/>.</summary>
        public void Tick(Camera cam, float time, bool extractingHere, float progress01)
        {
            if (Root == null) return;
            bool open = Exit.Open;
            if (!_styled || open != _styledOpen) ApplyStyle(open);

            Color c = open ? OpenColor : ClosedColor;
            float flicker = 0.8f + 0.4f * Mathf.PerlinNoise(time * 6.5f, _seed);

            float beamFade = 1f;
            if (cam != null)
            {
                Transform ct = cam.transform;
                Vector3 to = ct.position - _root.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > 0.01f) _beam.rotation = Quaternion.LookRotation(to / d, Vector3.up);
                // Fade the column out when the camera is right beside it (standing in the zone up close).
                beamFade = Mathf.Clamp01((d - 3f) / 9f);
                _flare.rotation = ct.rotation;
            }

            SetColor(_beamRenderer, c, (open ? 0.55f : 0.22f) * beamFade * (0.92f + 0.08f * flicker));
            float ringAlpha = open ? 0.85f : 0.4f;
            if (extractingHere) ringAlpha *= 0.7f + 0.3f * Mathf.Sin(time * 10f);
            SetColor(_ringRenderer, c, ringAlpha);
            SetColor(_flareRenderer, Color.Lerp(c, Color.white, 0.55f), (open ? 1f : 0.45f) * flicker);
            _flare.localScale = Vector3.one * ((open ? 0.55f : 0.35f) * (0.85f + 0.3f * flicker));
            _light.intensity = (open ? 2.4f : 0.7f) * flicker;

            float p = extractingHere ? Mathf.Clamp01(progress01) : 0f;
            _fill.localScale = Vector3.one * (_radius * p);
            if (p > 0f) SetColor(_fillRenderer, c, 0.32f);
        }

        /// <summary>Frees the beacon's own mesh (the objects go with their parent).</summary>
        public void Dispose()
        {
            if (_ringMesh != null) Object.Destroy(_ringMesh);
            if (Root != null) Object.Destroy(Root);
        }

        // Re-tints the parts that don't animate every frame: the light colour and the smoke.
        private void ApplyStyle(bool open)
        {
            _styled = true;
            _styledOpen = open;
            Color c = open ? OpenColor : ClosedColor;
            _light.color = c;
            if (_smoke != null)
            {
                ParticleSystem.MainModule main = _smoke.main;
                main.startColor = new Color(c.r, c.g, c.b, open ? 0.32f : 0.16f);
                ParticleSystem.EmissionModule emission = _smoke.emission;
                emission.rateOverTime = open ? 6f : 2.5f;
            }
        }

        private Transform MakePart(string name, Mesh mesh, Vector3 localPos, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            Transform t = go.transform;
            t.SetParent(_root, false);
            t.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GlowMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return t;
        }

        private static void SetColor(Renderer r, Color c, float alpha)
        {
            if (s_Block == null) s_Block = new MaterialPropertyBlock();
            c.a = Mathf.Clamp01(alpha);
            s_Block.Clear();
            s_Block.SetColor(ColorId, c);
            s_Block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(s_Block);
        }

        #region Smoke
        private static ParticleSystem BuildSmoke(Transform parent)
        {
            var go = new GameObject("FlareSmoke");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // the cone emits along local +Z: now straight up

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;          // the column of smoke is already standing when the raid begins
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.015f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 6f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.15f;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.6f));

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            // A gentle drift, as if a light wind leans the smoke over (all three axes in one curve mode).
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0.12f);
            vel.z = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.25f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = SmokeMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();
            return ps;
        }
        #endregion

        #region Shared materials & meshes
        // Sprites/Default is always included, honours vertex colour and alpha, and applies no fog, so a
        // beacon far beyond the fog line still glows.
        private static Shader GlowShader()
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Unlit/Transparent");
            return s;
        }

        private static Material GlowMaterial()
        {
            if (s_GlowMat != null) return s_GlowMat;
            s_GlowMat = new Material(GlowShader()) { name = "ExitGlow", color = Color.white };
            return s_GlowMat;
        }

        private static Material SmokeMaterial()
        {
            if (s_SmokeMat != null) return s_SmokeMat;
            s_SmokeMat = new Material(GlowShader()) { name = "ExitSmoke", color = Color.white };
            s_SmokeMat.mainTexture = SoftDot();
            return s_SmokeMat;
        }

        private static Texture2D SoftDot()
        {
            if (s_SoftDot != null) return s_SoftDot;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "ExitSmokeDot" };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;   // 0 centre .. 1 edge
                    float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            s_SoftDot = tex;
            return s_SoftDot;
        }

        // A vertical quad (XY plane) for the light column: bright core, soft halo either side, fading out
        // toward the top. Turned to face the camera about the vertical axis each frame.
        private static Mesh BeamMesh()
        {
            if (s_BeamMesh != null) return s_BeamMesh;
            float[] xs = { -1f, -0.2f, 0f, 0.2f, 1f };
            float[] xa = { 0f, 0.3f, 1f, 0.3f, 0f };
            float[] ys = { 0f, 1.2f, 5f, 14f, 30f, BeamHeight };
            float[] ya = { 0.6f, 1f, 0.8f, 0.5f, 0.22f, 0f };

            int cols = xs.Length, rows = ys.Length;
            var verts = new Vector3[cols * rows];
            var colors = new Color[verts.Length];
            var uvs = new Vector2[verts.Length];
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    int k = j * cols + i;
                    verts[k] = new Vector3(xs[i] * BeamHalfWidth, ys[j], 0f);
                    colors[k] = new Color(1f, 1f, 1f, xa[i] * ya[j]);
                    uvs[k] = new Vector2((xs[i] + 1f) * 0.5f, ys[j] / BeamHeight);
                }

            var tris = new int[(cols - 1) * (rows - 1) * 6];
            int n = 0;
            for (int j = 0; j < rows - 1; j++)
                for (int i = 0; i < cols - 1; i++)
                {
                    int a = j * cols + i, b = a + 1, c = a + cols, d = c + 1;
                    tris[n++] = a; tris[n++] = c; tris[n++] = b;
                    tris[n++] = b; tris[n++] = c; tris[n++] = d;
                }

            s_BeamMesh = new Mesh { name = "ExitBeam" };
            s_BeamMesh.vertices = verts;
            s_BeamMesh.colors = colors;
            s_BeamMesh.uv = uvs;
            s_BeamMesh.triangles = tris;
            s_BeamMesh.RecalculateBounds();
            return s_BeamMesh;
        }

        // A unit soft glow disc in the XY plane: opaque centre fading to nothing at radius 1.
        private static Mesh DiscMesh()
        {
            if (s_DiscMesh != null) return s_DiscMesh;
            s_DiscMesh = BuildRadialMesh("ExitGlowDisc", new[] { 0f, 0.35f, 1f }, new[] { 1f, 0.55f, 0f }, 28, flatOnGround: false);
            return s_DiscMesh;
        }

        // Concentric rings of vertices (radius + alpha each) joined into a fan/annulus, white with the
        // given alphas. Flat on the ground (XZ) or upright (XY).
        private static Mesh BuildRadialMesh(string name, float[] radii, float[] alphas, int segments, bool flatOnGround)
        {
            int rings = radii.Length;
            var verts = new Vector3[rings * segments];
            var colors = new Color[verts.Length];
            var uvs = new Vector2[verts.Length];
            float maxR = Mathf.Max(0.001f, radii[rings - 1]);
            for (int k = 0; k < rings; k++)
            {
                float r = Mathf.Max(0f, radii[k]);
                for (int s = 0; s < segments; s++)
                {
                    float ang = s / (float)segments * Mathf.PI * 2f;
                    float cx = Mathf.Cos(ang), cy = Mathf.Sin(ang);
                    int v = k * segments + s;
                    verts[v] = flatOnGround ? new Vector3(cx * r, 0f, cy * r) : new Vector3(cx * r, cy * r, 0f);
                    colors[v] = new Color(1f, 1f, 1f, alphas[k]);
                    uvs[v] = new Vector2(0.5f + 0.5f * cx * r / maxR, 0.5f + 0.5f * cy * r / maxR);
                }
            }

            var tris = new int[(rings - 1) * segments * 6];
            int n = 0;
            for (int k = 0; k < rings - 1; k++)
                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;
                    int a = k * segments + s, b = k * segments + s1;
                    int c = (k + 1) * segments + s, d = (k + 1) * segments + s1;
                    tris[n++] = a; tris[n++] = c; tris[n++] = b;
                    tris[n++] = b; tris[n++] = c; tris[n++] = d;
                }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion
    }
}
