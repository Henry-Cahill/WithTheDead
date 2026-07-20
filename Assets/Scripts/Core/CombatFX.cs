using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Throwaway visual feedback for gunfire: a fading tracer line and a brief
    /// muzzle flash. Both self-destruct, so callers can fire and forget.
    /// </summary>
    public static class CombatFX
    {
        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = CloneZUtil.UnlitMat(color);
            lr.useWorldSpace = true;
            lr.numCapVertices = 0;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.045f;
            lr.endWidth = 0.02f;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.35f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            go.AddComponent<FxFade>().Init(0.06f);
        }

        public static void MuzzleFlash(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "MuzzleFlash";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.35f;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = CloneZUtil.UnlitMat(new Color(1f, 0.86f, 0.42f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
