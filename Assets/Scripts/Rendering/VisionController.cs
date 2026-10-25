using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// The heart of the <see cref="VisionRig"/>: decides how dark the night is and lights only what the
    /// survivor can see, Project Zomboid style, then drives the other view effects in a fixed order
    /// once per frame (after the player has moved, the avatar has posed and the camera has followed).
    ///
    /// Line-of-sight darkness is done with lighting alone. At night the ambient light and moonlight are
    /// scaled right down (<see cref="TimeOfDay.AmbientScale"/> / <see cref="TimeOfDay.MoonlightScale"/>;
    /// near black in a raid, milder in wave survival), and a wide, soft, shadow-casting spot light at the
    /// survivor's eye — aimed where they face — reveals what is in view: walls and props cast shadows,
    /// so rooms behind walls stay dark. A small unshadowed glow keeps the ground right around the body
    /// faintly visible, behind them too. Both fade out by day. Night-vision goggles lift the dark and
    /// stretch the sight light (still line of sight); the flashlight, snow, breath and the night-vision
    /// look live in <see cref="FlashlightFX"/>, <see cref="SnowFX"/>, <see cref="BreathFX"/> and
    /// <see cref="NightVisionFX"/>, and every zombie's <see cref="ZombieVisibility"/> is decided from the
    /// <see cref="ZombieVisibility.Sight"/> built here.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class VisionController : MonoBehaviour
    {
        // How dark the night gets: TimeOfDay.AmbientScale / MoonlightScale at full night.
        private const float RaidNightAmbient = 0.25f;
        private const float RaidNightMoonlight = 0.3f;
        private const float RaidNightVisionAmbient = 0.55f;
        private const float WaveNightAmbient = 0.6f;
        private const float WaveNightMoonlight = 0.65f;
        private const float WaveNightVisionAmbient = 0.95f;
        private const float FullNightLightLevel = 0.18f;   // TimeOfDay.LightLevel at or below which it is full night
        private const float FullDayLightLevel = 0.55f;     // ...and at or above which it is full day
        private const float NightVisionFadeSeconds = 0.25f;

        // The sight light: a shadowed spot at the survivor's eye, aimed where they face.
        private const float EyeHeight = 1.62f;             // standing; the head bone lowers it when crouching
        private const float HeadToEye = 0.075f;            // eyes above the head bone (base of the skull)
        private const float MinEyeHeight = 0.45f, MaxEyeHeight = 1.9f;
        private const float EyeForward = 0.08f;
        private const float SightBackOffset = 0.25f;       // the light sits this far behind the eyes, so a wall you face doesn't glare
        private const float WallClearance = 0.12f;         // ...but never closer than this to a wall behind
        private const float SightSpotAngle = 155f;
        private const float SightInnerAngle = 110f;
        private const float SightRange = 16f;
        private const float SightIntensity = 16f;          // floor ≈ 0.1 irradiance out to SightFlatRadius
        private static readonly Color SightColor = new Color(0.78f, 0.86f, 1f);
        private const float SightFlatRadius = 6f;          // cookie: the floor is evenly lit out to this distance
        private const float SightHorizonFloor = 0.35f;     // cookie: eye-level rays are dimmed to this, so walls and
        private const float SightHorizonEaseDegrees = 12f; // ...zombies close in front don't glare; full strength this far off level
        private const float SightShadowDepthBias = 0.04f;  // metres
        private const float SightShadowNormalBias = 0.06f; // metres
        private const int SightShadowResolution = 1024;    // the URP asset's "High" additional-light shadow tier
        private const float SoftShadowKernel = 2.5f;       // URP scales the biases by the PCF radius (Medium = 5x5)
        private const float NightVisionRangeScale = 1.6f;
        private const float NightVisionIntensityScale = 2.2f;

        // The peripheral glow: an unshadowed spot pointing straight down from well above the survivor. It
        // lays a soft disc of light around the body (behind them too) and lights their head and shoulders,
        // and — pointing down from high up — never puts a hot spot on a ceiling or a nearby wall top.
        private const float PeripheryHeight = 4f;
        private const float PeripheryRange = 7f;
        private const float PeripheryIntensity = 1.2f;
        private const float PeripheryAngle = 90f;          // disc radius ≈ height × tan(angle / 2)
        private const float PeripheryInnerAngle = 50f;
        private static readonly Color PeripheryColor = new Color(0.8f, 0.87f, 1f);
        private const float NightVisionPeripheryScale = 1.8f;
        private const float NightVisionPeripheryAngle = 110f;

        // What the survivor can see (drives ZombieVisibility).
        private const float ConeHalfAngle = 85f;
        private const float AwarenessRadius = 3f;
        private const float DaySightDistance = 30f;
        private const float MinDaySightDistance = 12f;     // the floor for fog and rain
        private const float LitSightDistance = 30f;        // a zombie in a lit room / the beam, at night

        private const float FacingSharpness = 12f;
        private const float EyeHeightSharpness = 6f;
        private const float SurvivorRescanSeconds = 0.5f;
        private const float SunCheckSeconds = 2f;

        private static readonly int SolidMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
        private static readonly float CosConeHalfAngle = Mathf.Cos(ConeHalfAngle * Mathf.Deg2Rad);

        /// <summary>The installed controller (null when no vision rig is running).</summary>
        public static VisionController Active { get; private set; }

        /// <summary>The camera the effects are framed for.</summary>
        public Camera Camera { get; private set; }
        /// <summary>The survivor.</summary>
        public PlayerStats Player { get; private set; }
        /// <summary>The survivor's root transform.</summary>
        public Transform PlayerTransform { get; private set; }
        /// <summary>The survivor's controller (aim), if any.</summary>
        public PlayerController Controller { get; private set; }
        /// <summary>The survivor's head bone, if the avatar has a rig.</summary>
        public Transform HeadBone { get; private set; }
        /// <summary>The survivor's chest bone, if the avatar has a rig.</summary>
        public Transform ChestBone { get; private set; }
        /// <summary>The lens of the flashlight drawn on the survivor's chest strap (null when none is shown).</summary>
        public Transform FlashlightLens => _gearVisuals != null ? _gearVisuals.FlashlightLens : null;

        /// <summary>0 in daylight .. 1 at full night (from <see cref="TimeOfDay.LightLevel"/>).</summary>
        public float Night01 { get; private set; }
        /// <summary>0 .. 1 blend of the night-vision effect (eases over 0.25 s as the goggles switch).</summary>
        public float NightVisionBlend { get; private set; }
        /// <summary>True while the night-vision effect is mostly on.</summary>
        public bool NightVisionActive => NightVisionBlend > 0.5f;
        /// <summary>The survivor's eye: the origin of every line-of-sight test.</summary>
        public Vector3 EyePosition { get; private set; }
        /// <summary>Flat unit vector the survivor looks along (the smoothed aim direction).</summary>
        public Vector3 Facing { get; private set; } = Vector3.forward;
        /// <summary>Where the survivor aims (the cursor's spot on the ground in the isometric view).</summary>
        public Vector3 AimPoint => Controller != null ? Controller.AimPoint : EyePosition + Facing * 6f;
        /// <summary>How far the survivor can see right now (daylight, darkness, night vision).</summary>
        public float SightDistance { get; private set; }

        /// <summary>The line-of-sight spot light.</summary>
        public Light SightLight => _sight;
        /// <summary>The peripheral glow light.</summary>
        public Light PeripheryLight => _periphery;
        /// <summary>The flashlight beam effect.</summary>
        public FlashlightFX Flashlight { get; private set; }
        /// <summary>The night-vision look.</summary>
        public NightVisionFX NightVision { get; private set; }
        /// <summary>The falling snow.</summary>
        public SnowFX Snow { get; private set; }
        /// <summary>The breath vapour.</summary>
        public BreathFX Breath { get; private set; }

        private PlayerAvatar _avatar;
        private PlayerGearVisuals _gearVisuals;
        private PlayerGear _playerGear;
        private TimeOfDay _timeOfDay;
        private WeatherSystem _weather;
        private float _gearLookup;
        private Light _sight;
        private Light _periphery;
        private UniversalRenderPipelineAsset _urpAsset;
        private float _eyeHeight = EyeHeight;
        private bool _posed;
        private float _appliedRange = -1f;
        private ZombieVisibility.Sight _lastSight;
        private bool _hasSight;

        private readonly List<Renderer> _bodyScan = new List<Renderer>(48);
        private readonly List<Renderer> _bodyShadowOff = new List<Renderer>(48);
        private readonly List<Renderer> _bodyLayered = new List<Renderer>(48);
        private readonly List<uint> _bodyLayeredMasks = new List<uint>(48);
        private bool _bodyShadowsOff;
        private bool _bodyLayersOn;
        private float _bodyRescan;
        private int _bodyHierarchy = -1;

        private Light _sun;
        private bool _sunPatched;
        private float _sunCheck;

        /// <summary>Wires the controller to the camera and survivor and builds every effect
        /// (called once by <see cref="VisionRig.Install"/>).</summary>
        internal void Bind(Camera camera, PlayerStats player)
        {
            Camera = camera;
            Player = player;
            PlayerTransform = player != null ? player.transform : null;
            Controller = player != null ? player.GetComponent<PlayerController>() : null;
            _avatar = player != null ? player.GetComponent<PlayerAvatar>() : null;
            _gearVisuals = player != null ? player.GetComponent<PlayerGearVisuals>() : null;
CacheBones();
            _urpAsset = UniversalRenderPipeline.asset;

            _sight = NewLight("SightLight", LightType.Spot);
            _sight.color = SightColor;
            _sight.spotAngle = SightSpotAngle;
            _sight.innerSpotAngle = SightInnerAngle;
            _sight.range = SightRange;
            _sight.shadows = LightShadows.Soft;
            _sight.shadowStrength = 1f;
            _sight.shadowNearPlane = 0.1f;
            _sight.cookie = VisionAssets.VisionCookie(SightSpotAngle, EyeHeight, SightFlatRadius, SightHorizonFloor,
                SightHorizonEaseDegrees);
            UniversalAdditionalLightData data = _sight.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.softShadowQuality = SoftShadowQuality.Medium;
            // Shining from inside the survivor's head, it must neither light nor be shadowed by their body.
            data.renderingLayers = ~VisionRig.SurvivorRenderingLayerMask;
            if (Application.isPlaying)
                data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            ApplySightRange(SightRange);

            _periphery = NewLight("PeripheryLight", LightType.Spot);
            _periphery.color = PeripheryColor;
            _periphery.range = PeripheryRange;
            _periphery.spotAngle = PeripheryAngle;
            _periphery.innerSpotAngle = PeripheryInnerAngle;
            _periphery.shadows = LightShadows.None;
            _periphery.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _periphery.GetUniversalAdditionalLightData().renderingLayers = uint.MaxValue;
            PatchSun();

            Flashlight = NewChild<FlashlightFX>("Flashlight");
            Flashlight.Build();
            NightVision = NewChild<NightVisionFX>("NightVision");
            NightVision.Build();
            Snow = NewChild<SnowFX>("Snow");
            Snow.Build();
            Breath = NewChild<BreathFX>("Breath");
            Breath.Build();
        }

        /// <summary>
        /// True when the survivor can see <paramref name="point"/> right now: in the vision cone (or within
        /// arm's reach), close enough for the light, and not hidden behind anything solid.
        /// </summary>
        public bool CanSee(Vector3 point)
        {
            if (!_hasSight) return true;
            return ZombieVisibility.CanSee(in _lastSight, point, EyePosition);
        }

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            ZombieVisibility.ReleaseAll();
            RestoreBodyShadows();
            RestoreBodyLayers();
            _bodyShadowsOff = false;
            _bodyLayersOn = false;
            UnpatchSun();
            TimeOfDay.AmbientScale = 1f;
            TimeOfDay.MoonlightScale = 1f;
            _weather = null;
        }

        /// <summary>
        /// Cache the main camera on awake if it hasn't been assigned yet.
        /// This protects against the rare case where Bind hasn't set the camera.
        /// </summary>
        private void Awake()
        {
            if (Camera == null)
                Camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_urpAsset != UniversalRenderPipeline.asset) _urpAsset = UniversalRenderPipeline.asset;
            if (PlayerTransform == null || _sight == null)
            {
                // The survivor is gone: everything shows and the night is left alone.
                if (ZombieVisibility.HasSight) ZombieVisibility.ReleaseAll();
                _hasSight = false;
                return;
            }

            float dt = Time.deltaTime;
            // Cached camera set in Awake; no per-frame lookup needed.
            if (HeadBone == null) CacheBones();
            if (_gearVisuals == null)
            {
                // The gear visuals are added by PlayerGear after the rig is installed; look now and then.
                _gearLookup -= dt;
                if (_gearLookup <= 0f)
                {
                    _gearLookup = 1f;
                    _gearVisuals = PlayerTransform.GetComponent<PlayerGearVisuals>();
                }
            }

            UpdateNight(dt);
            ApplyDarkness();
            UpdatePose(dt);
            UpdateSightLights();
            Flashlight.Tick(this, dt);
            UpdateSurvivorRenderers(dt);
            _sunCheck -= dt;
            if (_sunCheck <= 0f)
            {
                _sunCheck = SunCheckSeconds;
                PatchSun();
            }
            UpdateSight(dt);
            Snow.Tick(this, dt);
            Breath.Tick(this, dt);
            NightVision.Tick(this, dt);
        }

        private void UpdateNight(float dt)
        {
            TimeOfDay clock = _timeOfDay ?? (_timeOfDay = TimeOfDay.Instance);
            float level = clock != null ? clock.LightLevel : 1f;
            Night01 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FullNightLightLevel, FullDayLightLevel, level));

            PlayerGear gear = _playerGear ?? (_playerGear = PlayerGear.Instance);
            bool goggles = gear != null && gear.NightVisionOn;
            if (dt > 0f)
                NightVisionBlend = Mathf.MoveTowards(NightVisionBlend, goggles ? 1f : 0f, dt / NightVisionFadeSeconds);
        }

        // Raids get near-black nights, wave survival keeps its milder dark; night vision lifts both.
        // TimeOfDay applies these every frame (it resets them to 1 whenever a new clock starts).
        private void ApplyDarkness()
        {
            bool raid = GameSession.IsExtraction;
            float nightAmbient = Mathf.Lerp(raid ? RaidNightAmbient : WaveNightAmbient,
                raid ? RaidNightVisionAmbient : WaveNightVisionAmbient, NightVisionBlend);
            TimeOfDay.AmbientScale = Mathf.Lerp(1f, nightAmbient, Night01);
            TimeOfDay.MoonlightScale = Mathf.Lerp(1f, raid ? RaidNightMoonlight : WaveNightMoonlight, Night01);
        }

        private void UpdatePose(float dt)
        {
            Transform player = PlayerTransform;
            Vector3 root = player.position;

            Vector3 aim = Controller != null ? Controller.AimDirection : player.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 1e-4f) { aim = player.forward; aim.y = 0f; }
            if (aim.sqrMagnitude < 1e-4f) aim = Vector3.forward;
            aim.Normalize();

            float eyeTarget = EyeHeight;
            if (HeadBone != null)
                eyeTarget = Mathf.Clamp(HeadBone.position.y - root.y + HeadToEye, MinEyeHeight, MaxEyeHeight);

            if (!_posed)
            {
                _posed = true;
                Facing = aim;
                _eyeHeight = eyeTarget;
            }
            else if (dt > 0f)
            {
                // Turn about the vertical, so a 180° swing never tips the cone through the floor.
                float angle = Vector3.SignedAngle(Facing, aim, Vector3.up);
                Facing = (Quaternion.AngleAxis(angle * (1f - Mathf.Exp(-FacingSharpness * dt)), Vector3.up) * Facing).normalized;
                _eyeHeight = Mathf.Lerp(_eyeHeight, eyeTarget, 1f - Mathf.Exp(-EyeHeightSharpness * dt));
            }
            EyePosition = root + Vector3.up * _eyeHeight + Facing * EyeForward;

            Vector3 lightPos = EyePosition - Facing * SightBackOffset;
            if (Physics.Raycast(EyePosition, -Facing, out RaycastHit hit, SightBackOffset + WallClearance, SolidMask,
                    QueryTriggerInteraction.Ignore))
                lightPos = EyePosition - Facing * Mathf.Max(0f, hit.distance - WallClearance);
            _sight.transform.SetPositionAndRotation(lightPos, Quaternion.LookRotation(Facing, Vector3.up));
            _periphery.transform.position = root + Vector3.up * PeripheryHeight;
        }

        /// <summary>Update the sight and periphery lights based on night‑vision state.</summary>
        // Adjust the line‑of‑sight spot light and peripheral glow based on whether night‑vision is active.
        // "goggles" is the blend from 0‑1 representing night‑vision intensity.
        // The range/intensity of the sight light scale with the blend; it's only enabled when bright enough.
        // The peripheral glow (a low‑intensity downward‑facing light) also scales with the blend.
        // This method keeps the lights in sync with the current vision state each frame.
        private void UpdateSightLights()
        {
            float goggles = NightVisionBlend;
            float range = SightRange * Mathf.Lerp(1f, NightVisionRangeScale, goggles);
            float intensity = SightIntensity * Mathf.Lerp(1f, NightVisionIntensityScale, goggles) * Night01;
            bool on = intensity > 0.01f;
            if (_sight.enabled != on) _sight.enabled = on;
            if (on)
            {
                _sight.intensity = intensity;
                if (Mathf.Abs(range - _appliedRange) > 0.01f) ApplySightRange(range);
            }

            float glow = PeripheryIntensity * Mathf.Lerp(1f, NightVisionPeripheryScale, goggles) * Night01;
            bool glowOn = glow > 0.005f;
            if (_periphery.enabled != glowOn) _periphery.enabled = glowOn;
            if (glowOn)
            {
                _periphery.intensity = glow;
                _periphery.spotAngle = Mathf.Lerp(PeripheryAngle, NightVisionPeripheryAngle, goggles);
            }
        }

        // URP's shadow biases are in shadow-map texels, which grow with the cone and the range; keep the
        // biases constant in metres so the floor neither acnes nor lets thin walls leak.
        private void ApplySightRange(float range)
        {
            _appliedRange = range;
            _sight.range = range;
            float texel = Mathf.Tan(SightSpotAngle * 0.5f * Mathf.Deg2Rad) * range / SightShadowResolution * SoftShadowKernel;
            _sight.shadowBias = SightShadowDepthBias / texel;
            _sight.shadowNormalBias = SightShadowNormalBias / texel;
        }

        // The sight light and the flashlight shine from inside the survivor's own body. The survivor's
        // renderers live on their own rendering layer, which the sight light skips and the flashlight's
        // shadows skip, so the body neither glows nor blots out the view — yet it is still lit, and still
        // casts shadows, by the sun, the room lights and the peripheral glow. If rendering layers are
        // switched off in the URP asset, fall back to turning the body's shadows off while either shines.
        /// <summary>Update the survivor's renderers so they render on the correct layer or have shadows disabled when needed.</summary>
        // Ensure the survivor's renderers are on the dedicated rendering layer when URP rendering layers are enabled.
        // If rendering layers are disabled, turn off their shadow casting while the sight light or flashlight is on.
        // The method rescans periodically or when the hierarchy changes, applying or restoring layer/shadow settings.
        private void UpdateSurvivorRenderers(float dt)
        {
            UniversalRenderPipelineAsset urp = _urpAsset;
            bool layers = urp != null && urp.useRenderingLayers;
            bool hideShadows = !layers && (_sight.enabled || (Flashlight != null && Flashlight.IsOn));
            _bodyRescan -= dt;
            int hierarchy = PlayerTransform.hierarchyCount;
            // Rescan now and then, and at once when something new (a drawn weapon, a backpack) joins the body.
            if (_bodyRescan > 0f && hierarchy == _bodyHierarchy && hideShadows == _bodyShadowsOff && layers == _bodyLayersOn)
                return;
            _bodyRescan = SurvivorRescanSeconds;
            _bodyHierarchy = hierarchy;
            if (!hideShadows && _bodyShadowsOff) RestoreBodyShadows();
            if (!layers && _bodyLayersOn) RestoreBodyLayers();
            _bodyShadowsOff = hideShadows;
            _bodyLayersOn = layers;
            if (!hideShadows && !layers) return;

            for (int i = _bodyShadowOff.Count - 1; i >= 0; i--)
                if (_bodyShadowOff[i] == null) _bodyShadowOff.RemoveAt(i);
            for (int i = _bodyLayered.Count - 1; i >= 0; i--)
            {
                if (_bodyLayered[i] != null) continue;
                _bodyLayered.RemoveAt(i);
                _bodyLayeredMasks.RemoveAt(i);
            }

            PlayerTransform.GetComponentsInChildren(true, _bodyScan);
            for (int i = 0; i < _bodyScan.Count; i++)
            {
                Renderer r = _bodyScan[i];
                if (r == null) continue;
                if (layers && r.renderingLayerMask != VisionRig.SurvivorRenderingLayerMask)
                {
                    _bodyLayered.Add(r);
                    _bodyLayeredMasks.Add(r.renderingLayerMask);
                    r.renderingLayerMask = VisionRig.SurvivorRenderingLayerMask;
                }
                if (hideShadows && r.shadowCastingMode == ShadowCastingMode.On)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    if (!_bodyShadowOff.Contains(r)) _bodyShadowOff.Add(r);
                }
            }
            _bodyScan.Clear();
        }

        private void RestoreBodyShadows()
        {
            for (int i = 0; i < _bodyShadowOff.Count; i++)
                if (_bodyShadowOff[i] != null) _bodyShadowOff[i].shadowCastingMode = ShadowCastingMode.On;
            _bodyShadowOff.Clear();
        }

        private void RestoreBodyLayers()
        {
            for (int i = 0; i < _bodyLayered.Count; i++)
            {
                Renderer r = _bodyLayered[i];
                if (r != null && r.renderingLayerMask == VisionRig.SurvivorRenderingLayerMask)
                    r.renderingLayerMask = _bodyLayeredMasks[i];
            }
            _bodyLayered.Clear();
            _bodyLayeredMasks.Clear();
        }

        // The sun (moon by night) must light the survivor's rendering layer too.
        /// <summary>Patch the sun (or moon) light to include the survivor's rendering layer, ensuring it lights the player.</summary>
        // Add the survivor's rendering layer to the sun (or moon) light so it lights the player.
        // This is needed because the sun otherwise only lights default layers.
        // The method also ensures shadow layers include the player when custom shadow layers are used.
        private void PatchSun()
        {
            TimeOfDay clock = _timeOfDay ?? (_timeOfDay = TimeOfDay.Instance);
            Light sun = clock != null && clock.Sun != null ? clock.Sun : RenderSettings.sun;
            if (sun == _sun) return;
            UnpatchSun();
            _sun = sun;
            if (sun == null) return;
            UniversalAdditionalLightData data = sun.GetUniversalAdditionalLightData();
            uint mask = VisionRig.SurvivorRenderingLayerMask;
            uint lighting = data.renderingLayers;
            bool shadowsOk = !data.customShadowLayers || (data.shadowRenderingLayers & mask) != 0;
            if ((lighting & mask) != 0 && shadowsOk) return;
            data.renderingLayers = lighting | mask;
            if (data.customShadowLayers) data.shadowRenderingLayers = data.shadowRenderingLayers | mask;
            _sunPatched = true;
        }

        private void UnpatchSun()
        {
            if (_sun != null && _sunPatched)
            {
                UniversalAdditionalLightData data = _sun.GetUniversalAdditionalLightData();
                uint keep = ~VisionRig.SurvivorRenderingLayerMask;
                data.renderingLayers = data.renderingLayers & keep;
                if (data.customShadowLayers) data.shadowRenderingLayers = data.shadowRenderingLayers & keep;
            }
            _sun = null;
            _sunPatched = false;
        }

        /// <summary>Recalculate the survivor's sight distance and update zombie visibility based on time of day, weather, and night‑vision.</summary>
        // Update the dynamic sight distance based on day/night cycle, weather, and night‑vision.
        // It also builds a ZombieVisibility.Sight struct containing all needed parameters for AI visibility checks.
        // The resulting sight data is fed to ZombieVisibility.TickAll each frame.
        /// <summary>Update the dynamic sight distance and feed visibility data to zombies.</summary
        private void UpdateSight(float dt)
        {
            WeatherSystem weather = _weather ??= WeatherSystem.Instance;
            float daySight = Mathf.Max(MinDaySightDistance, DaySightDistance * (weather != null ? weather.VisionMultiplier : 1f));
            float nightSight = SightRange * Mathf.Lerp(1f, NightVisionRangeScale, NightVisionBlend);
            SightDistance = Mathf.Lerp(daySight, nightSight, Night01);

            FlashlightFX beam = Flashlight;
            _lastSight = new ZombieVisibility.Sight
            {
                Eye = EyePosition,
                Facing = Facing,
                CosHalfAngle = CosConeHalfAngle,
                AwarenessRadius = AwarenessRadius,
                SightDistance = SightDistance,
                LitSightDistance = Mathf.Max(SightDistance, Mathf.Min(LitSightDistance, daySight)),
                BeamOn = beam != null && beam.IsLit,
                BeamOrigin = beam != null ? beam.Origin : EyePosition,
                BeamDirection = beam != null ? beam.Direction : Facing,
                BeamRange = FlashlightFX.Range,
                BeamCosHalfAngle = beam != null ? beam.CosHalfAngle : 1f,
            };
            _hasSight = true;
            ZombieVisibility.TickAll(in _lastSight, dt);
        }

        private void CacheBones()
        {
            HumanoidRig rig = _avatar != null ? _avatar.Rig : null;
            if (rig == null || rig.Bones == null) return;
            HeadBone = rig.Bone(HumanBone.Head);
            ChestBone = rig.Bone(HumanBone.Chest);
        }

        private Light NewLight(string name, LightType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        private T NewChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }
    }
}
