using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// The survivor's flashlight beam: a warm white, shadow-casting spot light from the torch clipped to
    /// the survivor's chest (the lens drawn by <see cref="PlayerGearVisuals"/>), shining where the
    /// survivor aims — at the cursor's spot on the ground in the isometric view, along the camera in the
    /// shoulder and first-person views. It follows <see cref="PlayerGear.FlashlightOn"/> and sputters as
    /// <see cref="PlayerGear.FlashlightCharge01"/> runs low. Created and ticked by the
    /// <see cref="VisionController"/> (it has no Update of its own).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlashlightFX : MonoBehaviour
    {
        /// <summary>Reach of the beam, metres.</summary>
        public const float Range = 22f;
        /// <summary>Outer cone angle of the beam, degrees.</summary>
        public const float SpotAngle = 46f;
        private const float InnerSpotAngle = 24f;
        private const float Intensity = 38f;
        private static readonly Color BeamColor = new Color(1f, 0.93f, 0.8f);
        private const float FadeSeconds = 0.08f;

        // Where the torch is held: the lens of the flashlight drawn on the left chest strap (see
        // PlayerGearVisuals), else this spot off the chest bone (or this high above the feet without a rig).
        private const float ChestHeight = 1.385f;
        private const float ChestForward = 0.2f;
        private const float ChestSide = -0.115f;       // negative = the survivor's left
        private const float LensClearance = 0.04f;     // start the beam just in front of the lens
        private const float FollowSharpness = 25f;
        private const float TurnSharpness = 18f;

        // Isometric aiming: the beam points at the cursor's spot on the ground, kept within this band so
        // it neither stabs at the feet nor skims flat along the floor.
        private const float MinAimDistance = 4.5f;
        private const float MaxAimDistance = 14f;
        private const float AimPointHeight = 0.15f;

        // Low battery: below LowCharge the beam dims towards FlatOutput and starts to sputter.
        private const float LowCharge = 0.15f;
        private const float FlatOutput = 0.45f;
        private const float LitThreshold = 0.3f;

        // Shadows on URP's "Medium" tier (512 px); the biases are in metres, converted to URP's units.
        private const int ShadowResolution = 512;
        private const float ShadowDepthBiasMetres = 0.03f;
        private const float ShadowNormalBiasMetres = 0.04f;
        private const float SoftShadowKernel = 2.5f;   // URP scales biases by the PCF radius (Medium = 5x5)

        /// <summary>True while the beam is on and bright enough to see by.</summary>
        public bool IsLit { get; private set; }
        /// <summary>True while the beam's light is on at all (fading or sputtering included).</summary>
        public bool IsOn => _light != null && _light.enabled;
        /// <summary>Where the beam starts (world space).</summary>
        public Vector3 Origin { get; private set; }
        /// <summary>Unit direction of the beam.</summary>
        public Vector3 Direction { get; private set; } = Vector3.forward;
        /// <summary>Cosine of the beam's half angle (for "is it in the beam" tests).</summary>
        public float CosHalfAngle => Mathf.Cos(SpotAngle * 0.5f * Mathf.Deg2Rad);

        private Light _light;
        private float _level;
        private bool _hasPose;
        private Vector3 _pos;
        private Vector3 _dir = Vector3.forward;
        private float _dipLeft, _dipDepth, _dipTimer;

        /// <summary>Creates the spot light (called once by the vision controller).</summary>
        internal void Build()
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.color = BeamColor;
            _light.range = Range;
            _light.spotAngle = SpotAngle;
            _light.innerSpotAngle = InnerSpotAngle;
            _light.intensity = 0f;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1f;
            _light.shadowNearPlane = 0.1f;
            float texel = Mathf.Tan(SpotAngle * 0.5f * Mathf.Deg2Rad) * Range / ShadowResolution * SoftShadowKernel;
            _light.shadowBias = ShadowDepthBiasMetres / texel;
            _light.shadowNormalBias = ShadowNormalBiasMetres / texel;
            _light.cookie = VisionAssets.FlashlightCookie();
            UniversalAdditionalLightData data = _light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.softShadowQuality = SoftShadowQuality.Medium;
            // Lights the survivor too (an aimed gun catches the beam), but their body casts no shadow in it.
            data.renderingLayers = uint.MaxValue;
            data.customShadowLayers = true;
            data.shadowRenderingLayers = ~VisionRig.SurvivorRenderingLayerMask;
            if (Application.isPlaying)
                data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            _light.enabled = false;
        }

        /// <summary>Follows the gear state, aims the beam and applies the low-battery sputter.</summary>
        internal void Tick(VisionController vision, float dt)
        {
            if (_light == null) return;
            PlayerGear gear = PlayerGear.Instance;
            bool on = gear != null && gear.FlashlightOn && vision.PlayerTransform != null;
            if (dt > 0f) _level = Mathf.MoveTowards(_level, on ? 1f : 0f, dt / FadeSeconds);
            else if (!on) _level = 0f;

            if (_level <= 0f)
            {
                if (_light.enabled) _light.enabled = false;
                IsLit = false;
                _hasPose = false;
                return;
            }

            UpdatePose(vision, dt);
            float output = _level * Sputter(gear, dt);
            _light.intensity = Intensity * output;
            if (!_light.enabled) _light.enabled = true;
            IsLit = output > LitThreshold;
        }

        private void UpdatePose(VisionController vision, float dt)
        {
            Transform player = vision.PlayerTransform;
            Vector3 facing = vision.Facing;
            Vector3 origin;
            Transform lens = vision.FlashlightLens;
            if (lens != null)
            {
                origin = lens.position;
            }
            else
            {
                var right = new Vector3(facing.z, 0f, -facing.x);
                Transform chest = vision.ChestBone;
                origin = chest != null
                    ? chest.position + facing * ChestForward + right * ChestSide
                    : player.position + Vector3.up * ChestHeight + facing * ChestForward + right * ChestSide;
            }

            Vector3 dir;
            Camera cam = vision.Camera;
            if (ViewState.Current != CameraView.Isometric && cam != null)
            {
                dir = cam.transform.forward;
            }
            else
            {
                Vector3 flat = vision.AimPoint - origin;
                flat.y = 0f;
                float dist = flat.magnitude;
                Vector3 flatDir = dist > 0.05f ? flat / dist : facing;
                dist = Mathf.Clamp(dist, MinAimDistance, MaxAimDistance);
                Vector3 spot = new Vector3(origin.x, player.position.y + AimPointHeight, origin.z) + flatDir * dist;
                dir = spot - origin;
            }
            if (dir.sqrMagnitude < 1e-6f) dir = facing;
            dir.Normalize();
            origin += dir * LensClearance;

            if (!_hasPose || dt <= 0f)
            {
                _pos = origin;
                _dir = dir;
                _hasPose = true;
            }
            else
            {
                _pos = Vector3.Lerp(_pos, origin, 1f - Mathf.Exp(-FollowSharpness * dt));
                _dir = Vector3.Slerp(_dir, dir, 1f - Mathf.Exp(-TurnSharpness * dt)).normalized;
            }
            transform.SetPositionAndRotation(_pos, Quaternion.LookRotation(_dir, Vector3.up));
            Origin = _pos;
            Direction = _dir;
        }

        // 1 with a healthy battery; below LowCharge the beam dims, wavers and drops out in short dips.
        private float Sputter(PlayerGear gear, float dt)
        {
            if (gear == null || gear.FlashlightCharge01 >= LowCharge)
            {
                _dipLeft = 0f;
                return 1f;
            }

            float weak = 1f - Mathf.Clamp01(gear.FlashlightCharge01 / LowCharge);
            float k = Mathf.Lerp(1f, FlatOutput, weak);
            k *= 1f - 0.25f * weak * Mathf.PerlinNoise(Time.time * 9f, 0.37f);
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
    }
}
