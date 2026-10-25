using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Falling snow: soft white specks drifting down through the dark on a slight wind (reference
    /// photo 1), from a world-space particle system that follows the view. In the isometric view the
    /// spawn box is fitted to what the camera actually frames at its current zoom (plus the strip high
    /// flakes need to fill the bottom of the screen) and the flakes are sized to stay specks of a few pixels; over
    /// the shoulder and in first person a tighter box sits ahead of the camera. How hard it snows comes
    /// from <see cref="Climate.SnowIntensity"/>. Flakes are only born over open ground (not inside
    /// registered <see cref="Shelter"/> interiors), and none at all while the survivor is indoors.
    /// Ticked by the <see cref="VisionController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SnowFX : MonoBehaviour
    {
        /// <summary>Most flakes alive at once.</summary>
        public const int MaxParticles = 1500;
        // Flakes per second per square metre of spawn box at SnowIntensity 1. A steady fall holds
        // rate × height / (2 × fall speed) flakes, so density stays the same whatever the box size.
        // (Winter's light night flurry is SnowIntensity ≈ 0.15–0.28.)
        private const float RatePerSquareMetre = 0.25f;

        // Isometric spawn box, fitted to the camera's framing at the survivor.
        private const float IsoFramingMargin = 1.15f;
        private const float IsoMinBox = 22f, IsoMaxBox = 60f;
        private const float IsoMinHeight = 6f, IsoMaxHeight = 14f;
        private const float IsoHeightOfCamera = 0.6f;    // the box stops well short of the camera
        private const float IsoMinFlakePixels = 3f, IsoMaxFlakePixels = 6f;   // at the survivor's distance
        // Close views: a box ahead of the camera.
        private const float CloseBoxSize = 28f;
        private const float CloseBoxHeight = 10f;
        private const float CloseAheadOffset = 9f;
        private const float CloseMinSize = 0.022f, CloseMaxSize = 0.045f;
        private const float BelowGround = 0.5f;          // the box starts this far under the survivor's feet

        // Flakes
        private const float FallSpeed = 1.25f;
        private const float FallJitter = 0.35f;
        private static readonly Color FlakeColor = new Color(0.92f, 0.94f, 1f, 0.8f);
        private const float MinScreenSize = 0.0015f;     // fraction of the screen, so distant flakes stay specks
        private const float MaxScreenSize = 0.008f;      // ...and flakes brushing the camera don't blot the view
        private const float ShelterProbeHeight = 1f;

        // Wind: a gentle drift that slowly veers and gusts.
        private const float WindSpeed = 0.7f;
        private const float WindGust = 0.45f;
        private const float WindVeerDegrees = 35f;
        private static readonly Vector3 WindBase = new Vector3(0.85f, 0f, 0.35f);

        private ParticleSystem _ps;
        private ParticleSystem.EmitParams _emit;
        private float _toEmit;
        private bool _filled, _indoors;
        private CameraView _lastView;
        private readonly RoofMap _roofs = new RoofMap();
        // The noise lets a flake wander this far off its line: rooms are kept that much clear.
        private const float RoofPad = 0.75f;

        /// <summary>Creates the particle system (called once by the vision controller).</summary>
        internal void Build()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 10f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = MaxParticles;
            main.startSpeed = 0f;
            main.startLifetime = 8f;
            main.startSize = CloseMinSize;
            main.startColor = FlakeColor;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = _ps.emission;
            emission.enabled = false;   // flakes are emitted by hand, so sheltered columns can be skipped
            ParticleSystem.ShapeModule shape = _ps.shape;
            shape.enabled = false;

            ParticleSystem.NoiseModule noise = _ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.25f;
            noise.damping = true;
            noise.octaveCount = 1;
            noise.quality = ParticleSystemNoiseQuality.Low;

            ParticleSystem.ColorOverLifetimeModule col = _ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = VisionAssets.SnowMaterial();
            renderer.minParticleSize = MinScreenSize;
            renderer.maxParticleSize = MaxScreenSize;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.sortMode = ParticleSystemSortMode.None;

            _emit = new ParticleSystem.EmitParams { startColor = FlakeColor, applyShapeToPosition = false };
            _ps.Play();
        }

        /// <summary>Moves the spawn box with the view and emits this frame's flakes.</summary>
        internal void Tick(VisionController vision, float dt)
        {
            if (_ps == null) return;
            Transform player = vision.PlayerTransform;
            Camera cam = vision.Camera;
            if (player == null || dt <= 0f) return;

            CameraView view = ViewState.Current;
            bool close = view != CameraView.Isometric && cam != null;
            if (view != _lastView)
            {
                _lastView = view;
                _filled = false;   // the box moved: fill the new one at once rather than waiting for flakes to fall
            }

            float intensity = Mathf.Clamp01(Climate.SnowIntensity);
            // Indoors, no snow in any view (from above, flakes over the street would drift across the room): those in the
            // air go at once, and the box fills again straight away on the way back out.
            bool indoors = Shelter.Count > 0 && Shelter.IsSheltered(player.position + Vector3.up * ShelterProbeHeight);
            if (indoors != _indoors)
            {
                _indoors = indoors;
                if (indoors) _ps.Clear();
                else _filled = false;
            }
            if (indoors) intensity = 0f;
            if (intensity <= 0.001f)
            {
                _toEmit = 0f;
                return;
            }

            Vector3 centre;
            float size, height, minSize, maxSize;
            if (close)
            {
                size = CloseBoxSize;
                height = CloseBoxHeight;
                centre = cam.transform.position + CloneZUtil.Flat(cam.transform.forward) * CloseAheadOffset;
                minSize = CloseMinSize;
                maxSize = CloseMaxSize;
            }
            else
            {
                FitIsoBox(cam, player.position, out centre, out size, out height, out float metresPerPixel);
                minSize = IsoMinFlakePixels * metresPerPixel;
                maxSize = IsoMaxFlakePixels * metresPerPixel;
            }

            float floorY = player.position.y - BelowGround;
            float rate = RatePerSquareMetre * size * size * intensity;
            Vector3 wind = Wind();

            if (!_filled)
            {
                _filled = true;
                // Pre-fill the box with the flakes the steady fall would hold, so the view is already snowing.
                int fill = Mathf.RoundToInt(rate * height * 0.5f / FallSpeed);
                EmitFlakes(fill, centre, size, floorY, height, true, wind, minSize, maxSize);
            }

            _toEmit += rate * dt;
            int n = (int)_toEmit;
            _toEmit -= n;
            if (n > 0) EmitFlakes(n, centre, size, floorY, height, false, wind, minSize, maxSize);
        }

        // The isometric camera looks down from far off (orthographic, or through a narrow lens) and
        // zooms. Fit the box to the ground it frames plus the strip a high flake needs to reach the bottom
        // of the screen, and size the flakes from the screen's metres-per-pixel at the survivor.
        private static void FitIsoBox(Camera cam, Vector3 survivor, out Vector3 centre, out float size,
            out float height, out float metresPerPixel)
        {
            if (cam == null)
            {
                centre = survivor;
                size = IsoMaxBox;
                height = IsoMaxHeight;
                metresPerPixel = 0.017f;
                return;
            }

            Transform ct = cam.transform;
            float viewHeight = cam.orthographic
                ? 2f * cam.orthographicSize
                : 2f * Mathf.Max(1f, Vector3.Distance(ct.position, survivor)) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * Mathf.Max(0.5f, cam.aspect);
            float sinPitch = Mathf.Clamp(-ct.forward.y, 0.2f, 1f);
            float tanPitch = sinPitch / Mathf.Sqrt(Mathf.Max(0.01f, 1f - sinPitch * sinPitch));
            float groundDepth = viewHeight / sinPitch;

            height = Mathf.Clamp((ct.position.y - survivor.y) * IsoHeightOfCamera, IsoMinHeight, IsoMaxHeight);
            // A flake high in the air shows up-screen of the ground beneath it, so the flakes that fill the
            // bottom of the screen hang over ground nearer the camera: stretch the box back towards it.
            float reach = height / tanPitch;
            size = Mathf.Clamp(Mathf.Max(viewWidth, groundDepth + reach) * IsoFramingMargin, IsoMinBox, IsoMaxBox);
            centre = survivor - CloneZUtil.Flat(ct.forward) * (reach * 0.5f);
            metresPerPixel = viewHeight / Mathf.Max(240f, Screen.height);
        }

        // Flakes are born anywhere in the box (fading in, so none pop) and live until they reach the
        // floor. The pre-fill samples the steady state, where flakes thin out towards the top of the box.
        // None falls inside a building: a flake whose drift would carry it over a room on the way down isn't born.
        private void EmitFlakes(int count, Vector3 centre, float size, float floorY, float height, bool prefill,
            Vector3 wind, float minSize, float maxSize)
        {
            float half = size * 0.5f;
            int room = MaxParticles - _ps.particleCount;
            if (count > room) count = room;
            if (count <= 0) return;
            float reach = half + height / (FallSpeed - FallJitter) * wind.magnitude;
            _roofs.Cover(Rect.MinMaxRect(centre.x - reach, centre.z - reach, centre.x + reach, centre.z + reach), RoofPad);
            for (int i = 0; i < count; i++)
            {
                float x = centre.x + Random.Range(-half, half);
                float z = centre.z + Random.Range(-half, half);
                float u = Random.value;
                float y = floorY + height * (prefill ? 1f - Mathf.Sqrt(u) : u);
                float fall = FallSpeed + Random.Range(-FallJitter, FallJitter);
                float life = Mathf.Max(0.4f, (y - floorY) / fall);
                if (_roofs.CoveredAlong(x, z, wind.x * life, wind.z * life)) continue;

                _emit.position = new Vector3(x, y, z);
                _emit.velocity = new Vector3(wind.x, -fall, wind.z);
                _emit.startLifetime = life;
                _emit.startSize = Random.Range(minSize, maxSize);
                _ps.Emit(_emit, 1);
            }
        }

        private static Vector3 Wind()
        {
            float t = Time.time;
            float veer = (Mathf.PerlinNoise(t * 0.05f, 1.7f) - 0.5f) * 2f * WindVeerDegrees;
            float gust = 1f + WindGust * (Mathf.PerlinNoise(t * 0.21f, 5.3f) - 0.5f) * 2f;
            return Quaternion.Euler(0f, veer, 0f) * WindBase.normalized * (WindSpeed * gust);
        }
    }
}
