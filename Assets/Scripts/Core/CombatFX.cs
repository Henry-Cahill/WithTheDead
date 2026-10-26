using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Throwaway visual feedback for gunfire: a fading tracer line and a brief
    /// muzzle flash. Both self-destruct, so callers can fire and forget.
    /// </summary>
    public static class CombatFX
    {
        /// <summary>
        /// Spawns a short-lived tracer line between two points, using the given colour.
        /// </summary>
        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            // Create a temporary GameObject to host the tracer visual; naming helps debugging.
            var go = new GameObject("Tracer");
            // Add a LineRenderer component, which draws a line in the scene.
            var lr = go.AddComponent<LineRenderer>();
            // Use an unlit material so the tracer is visible regardless of scene lighting.
            lr.material = CloneZUtil.UnlitMat(color);
            // Render in world space so the positions match the actual world coordinates.
            lr.useWorldSpace = true;
            // No cap vertices – keeps the line thin and cheap to render.
            lr.numCapVertices = 0;
            // The tracer consists of exactly two points: start and end.
            lr.positionCount = 2;
            // Assign the start and end positions of the line.
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            // Slightly wider at the origin for visibility, tapering toward the target.
            lr.startWidth = 0.045f;
            lr.endWidth = 0.02f;
            // Preserve the chosen colour at the start, fade to semi‑transparent at the tail.
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.35f);
            // Tracers should not affect lighting or cast shadows.
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            // Attach a short‑lived fade component to clean up the object automatically.
            go.AddComponent<FxFade>().Init(0.06f);
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

    /// <summary>Fades a tracer/flash out over a short life, then destroys the object.</summary>
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
}
