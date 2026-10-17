using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Visible breath in the cold (reference photo 3): while <see cref="Climate.BreathVisible"/>, a small
    /// puff of vapour leaves the survivor's mouth every few seconds — more often when running or
    /// sprinting — growing, drifting forward and up, and fading over about a second. Skipped in first
    /// person (it would fog the camera). Ticked by the <see cref="VisionController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BreathFX : MonoBehaviour
    {
        private const float IdleInterval = 3.5f;          // seconds between breaths at rest / walking
        private const float ExertedInterval = 1.6f;       // ...when running or sprinting
        private const float IntervalJitter = 0.5f;
        private const float ExhaleSeconds = 0.3f;         // one breath is emitted over this long
        private const int ParticlesPerBreath = 7;
        private const float Lifetime = 1.2f;
        private const float StartSize = 0.07f;
        private const float EndSizeScale = 4.5f;
        private const float ExhaleSpeed = 0.38f;          // metres per second out of the mouth
        private const float RiseSpeed = 0.1f;
        private const float Spread = 0.06f;
        private const float CarryVelocity = 0.35f;        // share of the survivor's own velocity the puff keeps
        private const float Drag = 1.6f;
        private const float DayAlpha = 0.4f;
        private const float NightAlpha = 0.3f;
        private static readonly Color VapourColor = new Color(0.9f, 0.92f, 0.95f, 1f);

        // The mouth, relative to the head bone (the bone sits at the base of the skull).
        private static readonly Vector3 MouthOffset = new Vector3(0f, 0.045f, 0.11f);
        private const float FallbackMouthHeight = 1.63f;
        private const float FallbackMouthForward = 0.12f;

        private ParticleSystem _ps;
        private ParticleSystem.EmitParams _emit;
        private float _nextBreath = 1f;
        private float _exhaleLeft;
        private float _emitted;
        private Vector3 _lastPos;
        private bool _hasLastPos;

        /// <summary>Creates the particle system (called once by the vision controller).</summary>
        internal void Build()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            main.startSpeed = 0f;
            main.startLifetime = Lifetime;
            main.startSize = StartSize;
            main.startColor = VapourColor;
            main.gravityModifier = -0.015f;   // warm breath rises a little

            ParticleSystem.EmissionModule emission = _ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _ps.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = _ps.sizeOverLifetime;
            size.enabled = true;
            var grow = new AnimationCurve(new Keyframe(0f, 1f / EndSizeScale, 0f, 2.2f), new Keyframe(1f, 1f, 0.3f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(EndSizeScale, grow);

            ParticleSystem.ColorOverLifetimeModule col = _ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.45f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.LimitVelocityOverLifetimeModule limit = _ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 10f;
            limit.drag = Drag;

            ParticleSystem.RotationOverLifetimeModule spin = _ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = VisionAssets.BreathMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.maxParticleSize = 0.25f;

            _emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            _ps.Play();
        }

        /// <summary>Times the breaths and emits the vapour.</summary>
        internal void Tick(VisionController vision, float dt)
        {
            if (_ps == null || dt <= 0f) return;
            Transform player = vision.PlayerTransform;
            PlayerStats stats = vision.Player;
            if (player == null || stats == null) return;

            Vector3 pos = player.position;
            Vector3 velocity = _hasLastPos ? (pos - _lastPos) / dt : Vector3.zero;
            if (velocity.sqrMagnitude > 400f) velocity = Vector3.zero;   // a teleport, not a stride
            _lastPos = pos;
            _hasLastPos = true;

            bool breathe = Climate.BreathVisible && !stats.IsDead && ViewState.Current != CameraView.FirstPerson;
            if (!breathe)
            {
                _exhaleLeft = 0f;
                return;
            }

            bool exerted = stats.Exertion == Exertion.Running || stats.Exertion == Exertion.Sprinting;
            if (_exhaleLeft <= 0f)
            {
                _nextBreath -= dt;
                if (exerted) _nextBreath = Mathf.Min(_nextBreath, ExertedInterval + IntervalJitter);
                if (_nextBreath > 0f) return;
                _nextBreath = (exerted ? ExertedInterval : IdleInterval) + Random.Range(-IntervalJitter, IntervalJitter);
                _exhaleLeft = ExhaleSeconds;
                _emitted = 0f;
            }

            // Spread one breath's particles evenly over the exhale.
            _exhaleLeft -= dt;
            float due = ParticlesPerBreath * Mathf.Clamp01(1f - _exhaleLeft / ExhaleSeconds);
            int n = Mathf.FloorToInt(due - _emitted);
            if (n <= 0) return;
            _emitted += n;

            Vector3 facing = vision.Facing;
            Transform head = vision.HeadBone;
            Vector3 mouth;
            Vector3 forward;
            if (head != null)
            {
                mouth = head.TransformPoint(MouthOffset);
                forward = CloneZUtil.Flat(head.forward);
                if (forward == Vector3.zero) forward = facing;
            }
            else
            {
                mouth = pos + Vector3.up * FallbackMouthHeight + facing * FallbackMouthForward;
                forward = facing;
            }

            float alpha = Mathf.Lerp(NightAlpha, DayAlpha, 1f - vision.Night01) * (exerted ? 1.15f : 1f);
            Color c = VapourColor;
            c.a = Mathf.Clamp01(alpha);
            for (int i = 0; i < n; i++)
            {
                _emit.position = mouth + Random.insideUnitSphere * 0.015f;
                _emit.velocity = forward * ExhaleSpeed + Vector3.up * RiseSpeed + Random.insideUnitSphere * Spread
                                 + velocity * CarryVelocity;
                _emit.startSize = StartSize * Random.Range(0.85f, 1.2f);
                _emit.startLifetime = Lifetime * Random.Range(0.85f, 1.15f);
                _emit.startColor = c;
                _emit.rotation = Random.Range(0f, 360f);
                _ps.Emit(_emit, 1);
            }
        }
    }
}
