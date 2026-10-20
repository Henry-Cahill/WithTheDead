using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The isometric fog of war by day (Zomboid): from above you could otherwise see everything nearby, so the ground
    /// the survivor can't see — behind walls, behind their back beyond arm's reach — is shaded. It's the same line of
    /// sight that hides other people (<see cref="ActorFog"/>) and that the night's sight light uses
    /// (<see cref="VisionController"/>), drawn as a ring mesh between the sight polygon and the far distance.
    /// Isometric view only; first and third person see what the eyes see.
    /// </summary>
    public sealed class IsoVisionShade : MonoBehaviour
    {
        private const int Rays = 180;
        // Cached TimeOfDay singleton to avoid per‑frame FindAnyObjectByType lookups.
        private static TimeOfDay _timeOfDay;
        private static readonly Vector3[] _rayDirs = new Vector3[Rays];
        // Optional cache for outer offset (dir * Outer) to avoid per‑frame multiplication
        private static readonly Vector3[] _rayOuter = new Vector3[Rays];
        
        
        private const float Range = 40f, Behind = 3.5f, ConeHalf = 105f, Outer = 95f, Height = 0.05f;

        private Mesh _mesh;
        private Vector3[] _verts;
        private MeshRenderer _renderer;
        private Material _mat;
        private Transform _player;
        private PlayerController _ctrl;
        private float _next, _alpha;

        public static void Attach(PlayerStats p)
        {
            if (p == null || FindAnyObjectByType<IsoVisionShade>() != null) return;
            var go = new GameObject("Iso vision shade");
            var s = go.AddComponent<IsoVisionShade>();
            s._player = p.transform;
            s._ctrl = p.GetComponent<PlayerController>();
        }

        private void Awake()
        {
                        // Populate static ray direction and outer offset caches
        for (int i = 0; i < Rays; i++)
        {
            float ang = i * (360f / Rays);
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            _rayDirs[i] = dir;
            _rayOuter[i] = dir * Outer;
        }

        // Cache the TimeOfDay singleton for later use.
        _timeOfDay = TimeOfDay.Instance;

            _mesh.MarkDynamic();
            _verts = new Vector3[Rays * 2];
            var tris = new int[Rays * 6];
            for (int i = 0; i < Rays; i++)
            {
                int a = i, b = (i + 1) % Rays, A = Rays + i, B = Rays + (i + 1) % Rays, t = i * 6;
                tris[t] = a; tris[t + 1] = A; tris[t + 2] = b;
                tris[t + 3] = b; tris[t + 4] = A; tris[t + 5] = B;
            }
            _mesh.vertices = _verts;
            _mesh.triangles = tris;
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _mat = CreatureMaterials.TransparentUnlit(new Color(0.02f, 0.03f, 0.05f, 0f));
            _renderer.sharedMaterial = _mat;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            gameObject.layer = CloneZConfig.IgnoreRaycastLayer;
        }

        private void LateUpdate()
        {
            if (_player == null) { Destroy(gameObject); return; }
            bool on = ViewState.Current == CameraView.Isometric && !HubRules.SafeZone && !GameSession.IsMonsterRun;
            // Fades in and out with the view (and lightens at night, when the sight light does the work).
            float night = _timeOfDay != null ? 1f - Mathf.InverseLerp(0.18f, 0.55f, _timeOfDay.LightLevel) : 0f;
            // If the cached reference was lost (e.g., after a scene reload), refresh it.
            if (_timeOfDay == null) _timeOfDay = TimeOfDay.Instance;
            float want = on ? Mathf.Lerp(0.42f, 0.15f, night) : 0f;
            _alpha = Mathf.MoveTowards(_alpha, want, Time.deltaTime * 1.5f);
            _renderer.enabled = _alpha > 0.005f;
            Color c = _mat.color;
            c.a = _alpha;
            _mat.color = c;
            if (!_renderer.enabled || Time.time < _next) return;
            _next = Time.time + 1f / 20f;
            Rebuild();
        }

        private void Rebuild()
        {
            Vector3 feet = _player.position;
            Vector3 eye = feet + Vector3.up * 1.5f;
            Vector3 facing = _ctrl != null ? _ctrl.AimDirection : _player.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = _player.forward;
            facing.Normalize();
            for (int i = 0; i < Rays; i++)
            {
                float ang = i * (360f / Rays);
                Vector3 dir = _rayDirs[i];
                Vector3 outer = _rayOuter[i];
                float max = Vector3.Angle(facing, dir) <= ConeHalf ? Range : Behind;
                float d = max;
                if (Physics.Raycast(eye, dir, out RaycastHit hit, max, Actor.SightMask, QueryTriggerInteraction.Ignore))
                    d = hit.distance + 0.35f;   // the wall itself stays in the light
                Vector3 inner = feet + dir * d;
                inner.y = outer.y = feet.y + Height;
                _verts[i] = inner;
                _verts[Rays + i] = outer;
            }
            _mesh.vertices = _verts;
            _mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_mesh != null) Destroy(_mesh);
        }
    }

    public sealed class IsoVisionShadeModule : IGameModule
    {
        public int Order => 81;
        public void Init() => WorldHooks.PlayerBuilt += IsoVisionShade.Attach;
    }
}
