using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// The light and laser on the gun in the survivor's hands, as the world sees them while <see cref="PlayerGear"/> has them
    /// on: a shadow-casting spot light from the weapon light's lens, dimming and sputtering as its battery runs low the way the
    /// flashlight does, and a red dot where the laser meets something, with a faint beam back to the emitter. Both leave the
    /// gun from their attachment (<see cref="WeaponAttachments.Fitted"/>) and point where the barrel points, so at the low
    /// ready they light (or mark) the ground ahead; as the gun comes up to aim they swing onto the shot's line
    /// (<see cref="PlayerCombat.ShotLine"/>), so the dot sits where the round will go. (The figure's arms don't follow the
    /// camera's pitch; the shots do.) The lit beam also counts as the survivor's beam for who is seen in the dark
    /// (<see cref="VisionController"/>), and the dead notice it as they notice the flashlight's.
    ///
    /// The light, dot and beam live in an object at the scene's root, outside the survivor: a dot across the room is no part
    /// of the body. The light's shadows are the survivor's own, under the <see cref="ShadowBudget"/>; its place in the shadow
    /// atlas is kept only while a gun with a light is in hand. Added by <see cref="PlayerGear"/>.
    /// </summary>
    [DefaultExecutionOrder(60)]   // after the avatar has posed the gun (0) and the camera has moved (50), before the vision (900)
    [DisallowMultipleComponent]
    public sealed class WeaponTactical : MonoBehaviour
    {
        /// <summary>Reach of the weapon light's beam, metres.</summary>
        public const float LightRange = 24f;
        /// <summary>Outer cone angle of the weapon light's beam, degrees: a tighter throw than the flashlight's.</summary>
        public const float LightAngle = 36f;
        /// <summary>How far the laser reaches, metres.</summary>
        public const float LaserRange = 80f;

        private const float LightInnerAngle = 16f;
        private const float LightIntensity = 44f;
        private static readonly Color LightColor = new Color(0.95f, 0.97f, 1f);
        private const float FadeSeconds = 0.06f;
        private const float LitThreshold = 0.3f;
        private const float LensClearance = 0.03f;   // the beam starts just in front of the lens
        private const float RaiseSharpness = 12f;     // as quickly as the figure brings the gun up (HumanoidAnimator)

        // Low battery, as the flashlight's: below LowCharge the beam dims towards FlatOutput and starts to sputter.
        private const float LowCharge = 0.15f;
        private const float FlatOutput = 0.45f;

        // Shadows on URP's "Medium" tier (512 px); the biases are in metres, converted to URP's units.
        private const int ShadowResolution = 512;
        private const float ShadowDepthBiasMetres = 0.03f;
        private const float ShadowNormalBiasMetres = 0.04f;
        private const float SoftShadowKernel = 2.5f;

        private const float BeamWidth = 0.004f;
        private static readonly Color BeamNear = new Color(1f, 0.12f, 0.08f, 0.5f);
        private static readonly Color BeamFar = new Color(1f, 0.12f, 0.08f, 0.14f);

        /// <summary>The local survivor's (null when there is none).</summary>
        public static WeaponTactical Local { get; private set; }

        /// <summary>True while the weapon light is on and bright enough to see by.</summary>
        public bool LightLit { get; private set; }
        /// <summary>Where the weapon light's beam starts (world space).</summary>
        public Vector3 LightOrigin { get; private set; }
        /// <summary>Unit direction of the weapon light's beam.</summary>
        public Vector3 LightDirection { get; private set; } = Vector3.forward;
        /// <summary>Cosine of the weapon light's half angle (for "is it in the beam" tests).</summary>
        public float LightCosHalfAngle => Mathf.Cos(LightAngle * 0.5f * Mathf.Deg2Rad);
        /// <summary>The weapon light (null until a gun with one is first held).</summary>
        public Light Light => _light;
        /// <summary>True while the laser's dot is drawn: switched on, and meeting something within reach.</summary>
        public bool LaserShown { get; private set; }
        /// <summary>Where the laser's dot is (world space).</summary>
        public Vector3 LaserDot { get; private set; }
        /// <summary>Where the laser leaves the gun (world space).</summary>
        public Vector3 LaserOrigin { get; private set; }

        private PlayerGear _gear;
        private PlayerAvatar _avatar;
        private PlayerCombat _combat;
        private PlayerStats _stats;
        private Transform _root;
        private Light _light;
        private LineRenderer _beam;
        private Transform _dot;
        private float _level, _raise;
        private float _dipLeft, _dipDepth, _dipTimer;

        private static readonly RaycastHit[] s_Hits = new RaycastHit[24];
        private static Mesh s_Quad;
        private static Texture2D s_DotTex;
        private static Material s_DotMat, s_BeamMat;

        private void Awake()
        {
            _gear = GetComponent<PlayerGear>();
            _avatar = GetComponent<PlayerAvatar>();
            _combat = GetComponent<PlayerCombat>();
            _stats = GetComponent<PlayerStats>();
        }

        private void OnEnable() => Local = this;

        private void OnDisable()
        {
            if (Local == this) Local = null;
            Hide();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_avatar == null) _avatar = GetComponent<PlayerAvatar>();
            HumanoidRig rig = _avatar != null ? _avatar.Rig : null;
            WeaponAttachments.Fitted fit = rig != null ? rig.HeldAttachments : null;
            bool alive = _stats == null || !_stats.IsDead;
            Transform lens = fit != null ? fit.LightLens : null;
            Transform emitter = fit != null ? fit.LaserEmitter : null;
            bool lightOn = alive && lens != null && _gear != null && _gear.WeaponLightOn;
            bool laserOn = alive && emitter != null && _gear != null && _gear.LaserOn;
            if (fit != null)
            {
                fit.SetLightLit(lightOn);
                fit.SetLaserLit(laserOn);
            }

            // The gun comes up to aim (after a shot, aiming down the sights): the devices swing onto the shot's line with it.
            HumanoidAnimator anim = _avatar != null ? _avatar.Animator : null;
            bool raised = anim != null && anim.Aiming && !anim.Reloading;
            _raise = dt > 0f ? Mathf.Lerp(_raise, raised ? 1f : 0f, 1f - Mathf.Exp(-RaiseSharpness * dt)) : raised ? 1f : 0f;
            bool aimed = (lightOn || laserOn) && _raise > 0.001f && _combat != null;
            Vector3 target = aimed ? ShotEnd() : Vector3.zero;

            UpdateLight(lens, lightOn, aimed, target, dt);
            UpdateLaser(emitter, laserOn, aimed, target);
        }

        #region Aim
        // Where a device on the gun points: along the barrel, swung onto the shot's end as the gun comes up.
        private Vector3 Aim(Transform device, bool aimed, Vector3 target)
        {
            Vector3 barrel = device.forward;
            if (!aimed) return barrel;
            Vector3 to = target - device.position;
            if (to.sqrMagnitude < 0.04f) return barrel;
            return Vector3.Slerp(barrel, to.normalized, _raise).normalized;
        }

        // Where the next round would end, without spread: the first thing along the shot's line that isn't the survivor.
        private Vector3 ShotEnd()
        {
            _combat.ShotLine(out Vector3 from, out Vector3 dir);
            return Cast(from, dir, LaserRange, out RaycastHit hit) ? hit.point : from + dir * LaserRange;
        }

        // The nearest thing a ray meets that a round would (the world and characters), the survivor's own colliders aside.
        private bool Cast(Vector3 from, Vector3 dir, float range, out RaycastHit hit)
        {
            int n = Physics.RaycastNonAlloc(from, dir, s_Hits, range, CombatResolver.ShotMask, QueryTriggerInteraction.Ignore);
            int best = -1;
            for (int i = 0; i < n; i++)
            {
                if (best >= 0 && s_Hits[i].distance >= s_Hits[best].distance) continue;
                if (s_Hits[i].collider.transform.IsChildOf(transform)) continue;
                best = i;
            }
            hit = best >= 0 ? s_Hits[best] : default;
            return best >= 0;
        }
        #endregion

        #region Light
        private void UpdateLight(Transform lens, bool on, bool aimed, Vector3 target, float dt)
        {
            if (lens == null)
            {
                // No light on the gun in hand: its place in the shadow atlas is given back.
                if (_light != null && _light.gameObject.activeSelf) _light.gameObject.SetActive(false);
                _level = 0f;
                LightLit = false;
                return;
            }
            EnsureLight();
            if (!_light.gameObject.activeSelf) _light.gameObject.SetActive(true);
            if (dt > 0f) _level = Mathf.MoveTowards(_level, on ? 1f : 0f, dt / FadeSeconds);
            else if (!on) _level = 0f;
            if (_level <= 0f)
            {
                if (_light.enabled) _light.enabled = false;
                LightLit = false;
                return;
            }

            Vector3 dir = Aim(lens, aimed, target);
            Vector3 origin = lens.position + dir * LensClearance;
            _light.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > 0.98f ? lens.up : Vector3.up));
            float output = _level * Sputter(_gear != null ? _gear.WeaponLightCharge01 : 1f, dt);
            _light.intensity = LightIntensity * output;
            if (!_light.enabled) _light.enabled = true;
            LightLit = output > LitThreshold;
            LightOrigin = origin;
            LightDirection = dir;
        }

        // 1 with a healthy battery; below LowCharge the beam dims, wavers and drops out in short dips (as FlashlightFX's).
        private float Sputter(float charge, float dt)
        {
            if (charge >= LowCharge)
            {
                _dipLeft = 0f;
                return 1f;
            }
            float weak = 1f - Mathf.Clamp01(charge / LowCharge);
            float k = Mathf.Lerp(1f, FlatOutput, weak);
            k *= 1f - 0.25f * weak * Mathf.PerlinNoise(Time.time * 9f, 0.71f);
            if (_dipLeft > 0f)
            {
                _dipLeft -= dt;
                k *= _dipDepth;
            }
            else
            {
                _dipTimer -= dt;
                if (_dipTimer <= 0f)
                {
                    _dipLeft = Random.Range(0.04f, 0.14f);
                    _dipDepth = Random.Range(0.05f, 0.4f);
                    _dipTimer = Random.Range(0.12f, Mathf.Lerp(1.6f, 0.45f, weak));
                }
            }
            return k;
        }

        private void EnsureLight()
        {
            if (_light != null) return;
            var go = new GameObject("Weapon Light");
            go.transform.SetParent(Root, false);
            _light = go.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.color = LightColor;
            _light.range = LightRange;
            _light.spotAngle = LightAngle;
            _light.innerSpotAngle = LightInnerAngle;
            _light.intensity = 0f;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1f;
            _light.shadowNearPlane = 0.1f;
            float texel = Mathf.Tan(LightAngle * 0.5f * Mathf.Deg2Rad) * LightRange / ShadowResolution * SoftShadowKernel;
            _light.shadowBias = ShadowDepthBiasMetres / texel;
            _light.shadowNormalBias = ShadowNormalBiasMetres / texel;
            _light.cookie = VisionAssets.FlashlightCookie();
            UniversalAdditionalLightData data = _light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.softShadowQuality = SoftShadowQuality.Medium;
            // It shines from the gun's own side, inches from the barrel and whatever's on it: it lights neither the survivor
            // nor their kit (their rendering layer), else the barrel ahead of it glares white, and they cast no shadow in it.
            data.renderingLayers = ~VisionRig.SurvivorRenderingLayerMask;
            data.customShadowLayers = true;
            data.shadowRenderingLayers = ~VisionRig.SurvivorRenderingLayerMask;
            if (Application.isPlaying)
                data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            // One of the survivor's own: the budget keeps its atlas space while the gun is in hand, so switching it on never
            // overfills the atlas.
            ShadowedLight.Add(_light, pinned: true);
            _light.enabled = false;
        }
        #endregion

        #region Laser
        private void UpdateLaser(Transform emitter, bool on, bool aimed, Vector3 target)
        {
            LaserShown = false;
            if (!on || emitter == null)
            {
                if (_beam != null && _beam.enabled) _beam.enabled = false;
                if (_dot != null && _dot.gameObject.activeSelf) _dot.gameObject.SetActive(false);
                return;
            }
            EnsureLaser();
            Vector3 from = emitter.position;
            Vector3 dir = Aim(emitter, aimed, target);
            bool hit = Cast(from, dir, LaserRange, out RaycastHit h);
            Vector3 end = hit ? h.point : from + dir * LaserRange;
            LaserOrigin = from;

            _beam.SetPosition(0, from);
            _beam.SetPosition(1, end);
            if (!_beam.enabled) _beam.enabled = true;

            Camera cam = Camera.main;
            if (!hit || cam == null)
            {
                if (_dot.gameObject.activeSelf) _dot.gameObject.SetActive(false);
                return;
            }
            // A glowing dot facing the camera, big enough to see at any range and pulled a little off the surface.
            Vector3 eye = cam.transform.position;
            float size = cam.orthographic ? cam.orthographicSize * 0.011f : Vector3.Distance(eye, end) * 0.011f;
            size = Mathf.Clamp(size, 0.03f, 0.4f);
            Vector3 toEye = eye - end;
            Vector3 at = end + (toEye.sqrMagnitude > 1e-6f ? toEye.normalized : -dir) * (size * 0.5f + 0.01f);
            Vector3 facing = cam.orthographic ? -cam.transform.forward : toEye;
            _dot.SetPositionAndRotation(at, Quaternion.LookRotation(-facing, cam.transform.up));
            _dot.localScale = Vector3.one * size;
            if (!_dot.gameObject.activeSelf) _dot.gameObject.SetActive(true);
            LaserShown = true;
            LaserDot = end;
        }

        private void EnsureLaser()
        {
            if (_beam == null)
            {
                var go = new GameObject("Laser Beam");
                go.layer = CloneZConfig.IgnoreRaycastLayer;   // nothing solid: sight and shots pass through it
                go.transform.SetParent(Root, false);
                _beam = go.AddComponent<LineRenderer>();
                _beam.useWorldSpace = true;
                _beam.positionCount = 2;
                _beam.numCapVertices = 0;
                _beam.startWidth = BeamWidth;
                _beam.endWidth = BeamWidth * 0.6f;
                _beam.startColor = BeamNear;
                _beam.endColor = BeamFar;
                _beam.shadowCastingMode = ShadowCastingMode.Off;
                _beam.receiveShadows = false;
                _beam.sharedMaterial = BeamMaterial();
                _beam.enabled = false;
            }
            if (_dot == null)
            {
                var go = new GameObject("Laser Dot");
                go.layer = CloneZConfig.IgnoreRaycastLayer;
                go.transform.SetParent(Root, false);
                go.AddComponent<MeshFilter>().sharedMesh = Quad();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = DotMaterial();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                go.SetActive(false);
                _dot = go.transform;
            }
        }
        #endregion

        private Transform Root
        {
            get
            {
                if (_root == null) _root = new GameObject("[Weapon devices] " + name).transform;
                return _root;
            }
        }

        private void Hide()
        {
            if (_light != null) _light.enabled = false;
            if (_beam != null) _beam.enabled = false;
            if (_dot != null) _dot.gameObject.SetActive(false);
            LightLit = LaserShown = false;
            _level = 0f;
        }

        #region Shared assets
        // Meshes and materials made in a play session are destroyed when it ends; statics outlive it in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetLastPlaySession()
        {
            s_Quad = null;
            s_DotTex = null;
            s_DotMat = s_BeamMat = null;
            Local = null;
        }

        private static Mesh Quad()
        {
            if (s_Quad != null) return s_Quad;
            s_Quad = new Mesh { name = "LaserDot" };
            s_Quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) });
            s_Quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            s_Quad.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            s_Quad.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            s_Quad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            s_Quad.RecalculateBounds();
            return s_Quad;
        }

        // A solid red core inside a soft halo, blended over whatever it lands on (added light would wash out to white on a pale
        // wall: an LDR camera can't go brighter than white).
        private static Material DotMaterial()
        {
            if (s_DotMat == null) s_DotMat = Blended("LaserDot", DotTexture(), new Color(1f, 0.06f, 0.04f, 1f));
            return s_DotMat;
        }

        // White with the dot in alpha: solid out to a fifth of the radius, then a halo fading to nothing at the rim.
        private static Texture2D DotTexture()
        {
            if (s_DotTex != null) return s_DotTex;
            const int size = 64;
            s_DotTex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "LaserDot", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = 1f - Mathf.SmoothStep(0.16f, 0.24f, r);
                    float halo = 0.45f * (1f - Mathf.SmoothStep(0f, 1f, r)) * (1f - Mathf.SmoothStep(0f, 1f, r));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.Max(core, halo)) * 255f));
                }
            s_DotTex.SetPixels32(px);
            s_DotTex.Apply(true, true);
            return s_DotTex;
        }

        private static Material BeamMaterial()
        {
            if (s_BeamMat == null) s_BeamMat = Blended("LaserBeam", null, Color.white);
            return s_BeamMat;
        }

        // "Universal Render Pipeline/Particles/Unlit" set up as an alpha-blended transparent (the state URP's inspector writes
        // for Surface = Transparent, Blend = Alpha), tinted by the vertex colour.
        private static Material Blended(string name, Texture texture, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return CloneZUtil.UnlitMat(color);
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
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.enableInstancing = true;
            return m;
        }

        private static void SetFloat(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
        #endregion
    }
}
