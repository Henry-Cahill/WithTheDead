using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    /// <summary>
    /// Procedural textures, meshes and materials shared by the vision and lighting effects (the
    /// line-of-sight light, <see cref="RoomLight"/> bulbs, snow, breath, the flashlight and the
    /// night-vision scope). Everything is generated on first use and cached for the session; an asset
    /// that has been destroyed meanwhile (scene teardown, UnloadUnusedAssets) is simply rebuilt.
    /// </summary>
    internal static class VisionAssets
    {
        // Line-of-sight light cookie: rays within this elevation (degrees) of the horizon get the
        // horizon value; the ease to full strength runs from here to the caller's ease angle.
        private const float HorizonEaseFromDeg = 2f;

        private const int VisionCookieSize = 256;
        private const int FlashCookieSize = 128;
        private const int SoftDotSize = 32;
        private const int PuffSize = 64;
        private const int ScopeSize = 256;
        private const int NoiseSize = 128;

        /// <summary>Normalised radius (of the scope sprite's half size) inside which the scope is clear.</summary>
        public const float ScopeClearRadius = 0.8f;

        private static Texture2D s_VisionCookie;
        private static int s_VisionCookieKey;
        private static Texture2D s_FlashCookie;
        private static Texture2D s_SoftDot;
        private static Texture2D s_Puff;
        private static Texture2D s_Scanlines;
        private static Texture2D s_Noise;
        private static Sprite s_Scope;
        private static Material s_SnowMat;
        private static Material s_BreathMat;
        private static Mesh s_BulbMesh;
        private static readonly Dictionary<int, Material> s_BulbMats = new Dictionary<int, Material>();

        #region Light cookies
        /// <summary>
        /// The cookie of the survivor's line-of-sight spot light. A point light at eye height lights the
        /// floor at your feet many times brighter than the floor a few metres away (inverse square times
        /// a grazing angle), which reads as a torch rather than "what I can see". The cookie damps every
        /// ray that would meet the floor (or a ceiling as far above) closer than
        /// <paramref name="flatRadius"/>, so the floor is evenly lit out to that radius and then fades
        /// naturally; rays near eye level are damped to <paramref name="horizonFloor"/>, so walls and
        /// figures close in front don't glare. It is symmetric top to bottom and left to right, so it is
        /// correct whichever way the cookie atlas happens to be oriented.
        /// </summary>
        /// <param name="spotAngle">Outer spot angle of the light, degrees.</param>
        /// <param name="planeDistance">Height of the light above the floor, metres.</param>
        /// <param name="flatRadius">Floor distance out to which the hot spot is flattened, metres.</param>
        /// <param name="horizonFloor">Cookie value for rays at the horizon (0..1).</param>
        /// <param name="horizonEaseDegrees">Elevation (degrees) from which rays get full strength again.</param>
        public static Texture2D VisionCookie(float spotAngle, float planeDistance, float flatRadius, float horizonFloor,
            float horizonEaseDegrees)
        {
            int key = Mathf.RoundToInt(spotAngle * 10f) * 7919 + Mathf.RoundToInt(planeDistance * 100f) * 104729
                      + Mathf.RoundToInt(flatRadius * 100f) * 31 + Mathf.RoundToInt(horizonFloor * 1000f)
                      + Mathf.RoundToInt(horizonEaseDegrees * 10f) * 1000003;
            if (s_VisionCookie != null && s_VisionCookieKey == key) return s_VisionCookie;
            if (s_VisionCookie != null) Object.Destroy(s_VisionCookie);

            const int size = VisionCookieSize;
            var tex = NewTexture("VisionCookie", size, false, true, TextureWrapMode.Clamp, FilterMode.Bilinear);
            float t = Mathf.Tan(Mathf.Clamp(spotAngle, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            float h = Mathf.Max(0.2f, planeDistance);
            float reference = Mathf.Sqrt(flatRadius * flatRadius + h * h);
            float easeTo = Mathf.Max(HorizonEaseFromDeg + 0.5f, horizonEaseDegrees);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                // Light-space slope of the ray: (x/z, y/z) spans [-tan, tan] across the cookie.
                float vy = Mathf.Abs(((y + 0.5f) / size * 2f - 1f) * t);
                for (int x = 0; x < size; x++)
                {
                    float ux = ((x + 0.5f) / size * 2f - 1f) * t;
                    float c = 1f;
                    if (vy > 1e-4f)
                    {
                        // Distance at which the ray meets a plane h below (or above) the light.
                        float dist = h / vy * Mathf.Sqrt(ux * ux + vy * vy + 1f);
                        float r = dist / reference;
                        c = Mathf.Min(1f, r * r * r);

        }
                    float elevation = Mathf.Atan2(vy, Mathf.Sqrt(1f + ux * ux)) * Mathf.Rad2Deg;
                    c *= Mathf.Lerp(horizonFloor, 1f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(HorizonEaseFromDeg, easeTo, elevation)));
                    // URP samples a spot cookie's RGB from the colour atlas (alpha is ignored), so the value goes in RGB.
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c * 255f), 0, 255);
                    px[y * size + x] = new Color32(b, b, b, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_VisionCookie = tex;
            s_VisionCookieKey = key;
            return tex;
        }

        /// <summary>A torch beam: a hot centre easing off towards the rim, with a faint reflector ring.</summary>
        public static Texture2D FlashlightCookie()
        {
            if (s_FlashCookie != null) return s_FlashCookie;
            const int size = FlashCookieSize;
            var tex = NewTexture("FlashlightCookie", size, false, true, TextureWrapMode.Clamp, FilterMode.Bilinear);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Lerp(1f, 0.58f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.85f, r)));
                    float ringT = (r - 0.8f) / 0.06f;
                    float ring = 0.14f * Mathf.Exp(-ringT * ringT);
                    float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 1f, r));
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01((core + ring) * edge) * 255f), 0, 255);
                    px[y * size + x] = new Color32(b, b, b, 255);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_FlashCookie = tex;
            return tex;
        }
        #endregion

        #region Particle textures + materials
        /// <summary>A small soft white dot (snowflakes), with mipmaps so distant specks don't sparkle.</summary>
        public static Texture2D SoftDot()
        {
            if (s_SoftDot != null) return s_SoftDot;
            const int size = SoftDotSize;
            var tex = NewTexture("SoftDot", size, true, false, TextureWrapMode.Clamp, FilterMode.Trilinear);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Pow(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r)), 1.4f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            s_SoftDot = tex;
            return tex;
        }

        /// <summary>A soft, slightly cloudy blob for breath vapour.</summary>
        public static Texture2D Puff()
        {
            if (s_Puff != null) return s_Puff;
            const int size = PuffSize;
            var tex = NewTexture("BreathPuff", size, true, false, TextureWrapMode.Clamp, FilterMode.Trilinear);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r2 = dx * dx + dy * dy;
                    float cloud = 0.7f + 0.3f * Mathf.PerlinNoise(x * 0.13f + 3.1f, y * 0.13f + 7.7f);
                    float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, Mathf.Sqrt(r2)));
                    float a = Mathf.Exp(-r2 * 3f) * cloud * edge;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            s_Puff = tex;
            return tex;
        }

        /// <summary>Alpha-blended unlit particle material for snowflakes.</summary>
        public static Material SnowMaterial()
        {
            if (s_SnowMat == null) s_SnowMat = ParticleMaterial("SnowFlakes", SoftDot());
            return s_SnowMat;
        }

        /// <summary>Alpha-blended unlit particle material for breath vapour.</summary>
        public static Material BreathMaterial()
        {
            if (s_BreathMat == null) s_BreathMat = ParticleMaterial("BreathVapour", Puff());
            return s_BreathMat;
        }

        // "Universal Render Pipeline/Particles/Unlit" set up as an alpha-blended transparent, the same
        // state URP's material inspector writes for Surface = Transparent, Blend = Alpha.
        private static void SetFloat(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }

        private static Material ParticleMaterial(string name, Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = name };
            SetFloat(m, "_Surface", 1f);
            SetFloat(m, "_Blend", 0f);
            SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(m, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(m, "_ZWrite", 0f);
            SetFloat(m, "_Cull", (float)CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.enableInstancing = true;
            return m;
        }
        #endregion

        #region Night-vision overlay
        /// <summary>
        /// The night-vision scope mask: white, clear inside <see cref="ScopeClearRadius"/> of its half
        /// size, ramping to opaque at the rim (tint it with the Image colour). Its corners are opaque, so
        /// solid panels of the same colour can carry the mask to the screen edges.
        /// </summary>
        public static Sprite ScopeSprite()
        {
            if (s_Scope != null && s_Scope.texture != null) return s_Scope;
            const int size = ScopeSize;
            var tex = NewTexture("NightVisionScope", size, false, false, TextureWrapMode.Clamp, FilterMode.Bilinear);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ScopeClearRadius, 1f, r));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_Scope = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return s_Scope;
        }

        /// <summary>Two dark rows then two clear rows, tiled one texel per screen pixel for scanlines.</summary>
        public static Texture2D Scanlines()
        {
            if (s_Scanlines != null) return s_Scanlines;
            var tex = NewTexture("NightVisionScanlines", 4, false, false, TextureWrapMode.Repeat, FilterMode.Point);
            var px = new Color32[16];
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    px[y * 4 + x] = new Color32(0, 0, 0, (byte)(y >= 2 ? 255 : 0));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_Scanlines = tex;
            return tex;
        }

        /// <summary>Random light and dark speckles for the sensor noise layered over the scope.</summary>
        public static Texture2D Noise()
        {
            if (s_Noise != null) return s_Noise;
            const int size = NoiseSize;
            var tex = NewTexture("NightVisionNoise", size, false, false, TextureWrapMode.Repeat, FilterMode.Point);
            var px = new Color32[size * size];
            var rng = new System.Random(52731);
            for (int i = 0; i < px.Length; i++)
            {
                byte g = (byte)rng.Next(0, 256);
                byte a = (byte)rng.Next(0, 256);
                px[i] = new Color32(g, g, g, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_Noise = tex;
            return tex;
        }
        #endregion

        #region Light bulbs
        /// <summary>A low-poly unit-diameter sphere for light bulbs (no collider, nothing to bake).</summary>
        public static Mesh BulbMesh()
        {
            if (s_BulbMesh != null) return s_BulbMesh;
            const int rings = 6, segments = 10;
            var verts = new List<Vector3>((rings + 1) * (segments + 1));
            var norms = new List<Vector3>(verts.Capacity);
            var tris = new List<int>(rings * segments * 6);
            for (int r = 0; r <= rings; r++)
            {
                float lat = Mathf.PI * r / rings;
                for (int s = 0; s <= segments; s++)
                {
                    float lon = 2f * Mathf.PI * s / segments;
                    var n = new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon));
                    verts.Add(n * 0.5f);
                    norms.Add(n);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
            var mesh = new Mesh { name = "LightBulb" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            s_BulbMesh = mesh;
            return mesh;
        }

        /// <summary>A shared unlit material glowing in <paramref name="hdrColor"/> (values above 1 bloom).
        /// Shared per (quantised) colour; instantiate it if you need to animate it.</summary>
        public static Material BulbMaterial(Color hdrColor)
        {
            int key = Quant(hdrColor.r) * 1000003 + Quant(hdrColor.g) * 1009 + Quant(hdrColor.b);
            if (s_BulbMats.TryGetValue(key, out Material cached) && cached != null) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader) { name = "LightBulb" };
            SetBulbColor(m, hdrColor);
            m.enableInstancing = true;
            s_BulbMats[key] = m;
            return m;
        }

        /// <summary>Writes a bulb material's colour (allocation free; used by flickering bulbs).</summary>
        public static void SetBulbColor(Material m, Color hdrColor)
        {
            if (m == null) return;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, hdrColor);
            else m.color = hdrColor;
        }

        static VisionAssets()
        {
            SceneManager.sceneUnloaded += (_) => CleanupCaches();
        }

        private static void CleanupCaches()
        {
            // Textures
            Object.Destroy(s_VisionCookie);
            s_VisionCookie = null;
            Object.Destroy(s_FlashCookie);
            s_FlashCookie = null;
            Object.Destroy(s_SoftDot);
            s_SoftDot = null;
            Object.Destroy(s_Puff);
            s_Puff = null;
            Object.Destroy(s_Scanlines);
            s_Scanlines = null;
            Object.Destroy(s_Noise);
            s_Noise = null;
            // Sprite (its texture is already destroyed above)
            s_Scope = null;
            // Materials
            Object.Destroy(s_SnowMat);
            s_SnowMat = null;
            Object.Destroy(s_BreathMat);
            s_BreathMat = null;
            // Bulb mesh
            Object.Destroy(s_BulbMesh);
            s_BulbMesh = null;
            // Bulb materials dictionary
            foreach (var kv in s_BulbMats)
                Object.Destroy(kv.Value);
            s_BulbMats.Clear();
        }
        #endregion

        private static Texture2D NewTexture(string name, int size, bool mips, bool linear, TextureWrapMode wrap, FilterMode filter)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, mips, linear)
            {
                name = name,
                wrapMode = wrap,
                filterMode = filter,
            };
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static int Quant(float v) => Mathf.Clamp(Mathf.RoundToInt(v * 40f), 0, 999);
    }
}
