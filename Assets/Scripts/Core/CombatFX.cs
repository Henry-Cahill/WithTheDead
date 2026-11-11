using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Throwaway visual feedback for gunfire: a tracer round's red streak and a brief muzzle flash. Both
    /// self-destruct, so callers can fire and forget.
    /// </summary>
    public static class CombatFX
    {
        /// <summary>Tracer streaks drawn this session (the smoke test counts them).</summary>
        public static int TracersDrawn { get; private set; }

        // Statics outlive a play session in the editor (domain reload is off).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession() => TracersDrawn = 0;

        /// <summary>
        /// A tracer round's trail (<see cref="AmmoStats.Tracer"/>): a red streak that flies from <paramref name="from"/>
        /// to where the round stopped, <paramref name="to"/>, then fades there. Plain rounds draw no trail.
        /// </summary>
        public static void Tracer(Vector3 from, Vector3 to)
        {
            if (DedicatedServer.Requested || (to - from).sqrMagnitude < 0.01f) return;
            TracersDrawn++;
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            // Vertex-coloured and unlit (shared, never one per shot): the streak burns as bright by night as by day.
            lr.sharedMaterial = ZoneKit.BeamMaterial();
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.positionCount = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            go.AddComponent<TracerStreak>().Init(lr, from, to);
        }

        /// <summary>
        /// Spawns a brief, bright flash at the given position to represent a gun muzzle blast.
        /// </summary>
        public static void MuzzleFlash(Vector3 position)
        {
            // Use a sphere primitive as a cheap flash mesh; it will be hidden quickly.
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Remove the default collider – the flash shouldn't interact with physics.
            Object.Destroy(go.GetComponent<Collider>());
            // Name the object for easier debugging in the hierarchy.
            go.name = "MuzzleFlash";
            // Position it at the muzzle location.
            go.transform.position = position;
            // Scale to a size that looks like a flash but doesn't obscure the scene.
            go.transform.localScale = Vector3.one * 0.35f;
            // Grab the renderer to assign an unlit, bright material.
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = CloneZUtil.UnlitMat(new Color(1f, 0.86f, 0.42f));
            // No shadows – the flash is a visual effect only.
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Fade out and optionally shrink the flash so it disappears naturally.
            go.AddComponent<FxFade>().Init(0.05f, scaleDown: true);
        }
    }

    /// <summary>Fades a flash (or a line) out over a short life, then destroys the object.</summary>
    public class FxFade : MonoBehaviour
    {
        private float _life = 0.05f;
        private float _t;
        private bool _scaleDown;
        private Vector3 _startScale;
        private LineRenderer _line;
        private float _startWidth;

        public void Init(float life, bool scaleDown = false)
        {
            _life = Mathf.Max(0.01f, life);
            _scaleDown = scaleDown;
            _startScale = transform.localScale;
            _line = GetComponent<LineRenderer>();
            if (_line != null) _startWidth = _line.widthMultiplier;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(_t / _life);
            if (_scaleDown) transform.localScale = _startScale * k;
            if (_line != null) _line.widthMultiplier = _startWidth * k;
            if (_t >= _life) Destroy(gameObject);
        }
    }

    /// <summary>
    /// A tracer round's streak (<see cref="CombatFX.Tracer"/>): the burning head flies from the muzzle to where the round
    /// stopped, trailing up to <see cref="Length"/> metres of red behind it (slowed to be seen, and never there in less than
    /// <see cref="MinFlight"/>), then that last stretch glows on and fades over <see cref="Fade"/>.
    /// </summary>
    public sealed class TracerStreak : MonoBehaviour
    {
        private const float Speed = 280f, Length = 8f, MinFlight = 0.05f, Fade = 0.3f, HeadWidth = 0.06f, TailWidth = 0.02f;
        private static readonly Color HeadColor = new Color(1f, 0.3f, 0.16f, 1f), TailColor = new Color(1f, 0.12f, 0.06f, 0f);

        private LineRenderer _line;
        private Vector3 _from, _dir;
        private float _dist, _flight, _t;

        public void Init(LineRenderer line, Vector3 from, Vector3 to)
        {
            _line = line;
            _from = from;
            Vector3 d = to - from;
            _dist = d.magnitude;
            _dir = d / _dist;
            _flight = Mathf.Max(MinFlight, _dist / Speed);
            _line.startColor = TailColor;
            Place();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            Place();
        }

        // Point 0 is the tail, point 1 the head.
        private void Place()
        {
            float run = _dist * Mathf.Clamp01(_t / _flight);
            float k = _t <= _flight ? 1f : 1f - (_t - _flight) / Fade;
            if (k <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            _line.SetPosition(0, _from + _dir * Mathf.Max(0f, run - Length));
            _line.SetPosition(1, _from + _dir * run);
            _line.startWidth = TailWidth * k;
            _line.endWidth = HeadWidth * k;
            _line.endColor = new Color(HeadColor.r, HeadColor.g, HeadColor.b, k);
        }
    }
}
