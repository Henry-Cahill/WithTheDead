using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A moving piece of a fixture — a cupboard door, a drawer, an oven door, a window sash, a toilet seat —
    /// drawn with <see cref="RaidMesh"/> in world space and animated between closed and open. A hinged part
    /// turns about a world axis through its hinge; a sliding part slides along a world offset. Parts are kept
    /// out of the navmesh bake (they move), and stop updating once they come to rest.
    ///
    /// Typical use: draw the part's geometry in world space exactly where it sits when CLOSED, then
    /// <c>FixturePart.Hinge(host, "Door", mesh, hingePoint, Vector3.up, 100f)</c>; call
    /// <see cref="SetOpen"/> to swing it. Add colliders (if any) as children of the part so they move with it.
    /// </summary>
    [DisallowMultipleComponent]
    public class FixturePart : MonoBehaviour
    {
        private enum Motion { Hinge, Slide }

        private Motion _motion;
        private Vector3 _axis;           // world axis a hinge turns about
        private float _angle;            // degrees a hinge turns when fully open
        private Vector3 _offset;         // world offset a slide travels when fully open
        private Vector3 _closedPos;      // world position (the pivot) when closed
        private Quaternion _closedRot;   // world rotation when closed
        private float _t, _target;       // 0 closed .. 1 open
        private float _cachedE; // cached easing value (SmoothStep)
        private float _seconds = CloneZConfig.FixturePartSeconds;

        /// <summary>True once the part has been told to open (it may still be moving).</summary>
        public bool IsOpen => _target > 0.5f;

        /// <summary>How far open the part is right now, 0..1.</summary>
        public float Openness => _t;

        /// <summary>The renderer drawing the part (null for an empty mesh).</summary>
        public MeshRenderer Renderer { get; private set; }

        /// <summary>A part that turns <paramref name="openAngle"/> degrees about <paramref name="axis"/> (world) through
        /// <paramref name="hinge"/> (world) when opened. <paramref name="mesh"/> is drawn in world space, closed.</summary>
        internal static FixturePart Hinge(Transform parent, string name, RaidMesh mesh, Vector3 hinge, Vector3 axis, float openAngle,
            ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            FixturePart p = Create(parent, name, mesh, hinge, shadows);
            p._motion = Motion.Hinge;
            p._axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.up;
            p._angle = openAngle;
            return p;
        }

        /// <summary>A part that slides by <paramref name="openOffset"/> (world) when opened — a drawer, a sash.
        /// <paramref name="pivot"/> is any convenient world point on it (the mesh is stored relative to it).</summary>
        internal static FixturePart Slide(Transform parent, string name, RaidMesh mesh, Vector3 pivot, Vector3 openOffset,
            ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            FixturePart p = Create(parent, name, mesh, pivot, shadows);
            p._motion = Motion.Slide;
            p._offset = openOffset;
            return p;
        }

        private static FixturePart Create(Transform parent, string name, RaidMesh mesh, Vector3 pivot, ShadowCastingMode shadows)
        {
            GameObject go = mesh != null && !mesh.IsEmpty ? mesh.Emit(name, parent, pivot, shadows) : null;
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.position = pivot;
            }
            go.transform.rotation = Quaternion.identity;   // the geometry was drawn in world axes
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var p = go.AddComponent<FixturePart>();
            p._closedPos = pivot;
            p._closedRot = Quaternion.identity;
            p.Renderer = go.GetComponent<MeshRenderer>();
            p.enabled = false;
            DecorPalette.Apply();
            return p;
        }

        /// <summary>How long a full swing / slide takes (default <see cref="CloneZConfig.FixturePartSeconds"/>).</summary>
        public FixturePart WithSpeed(float seconds)
        {
            _seconds = Mathf.Max(0.01f, seconds);
            return this;
        }

        /// <summary>Opens or closes the part, animated unless <paramref name="instant"/>.</summary>
        public void SetOpen(bool open, bool instant = false) => SetOpenness(open ? 1f : 0f, instant);

        /// <summary>Moves the part toward a partial opening (0 closed .. 1 open).</summary>
        public void SetOpenness(float t, bool instant = false)
        {
            _target = Mathf.Clamp01(t);
            if (instant)
            {
                _t = _target;
                Apply();
                enabled = false;
                return;
            }
            enabled = !Mathf.Approximately(_t, _target);
        }

        private void Update()
        {
            _t = Mathf.MoveTowards(_t, _target, Time.deltaTime / _seconds);
            _cachedE = Mathf.SmoothStep(0f, 1f, _t);
            Apply();
            if (Mathf.Approximately(_t, _target)) enabled = false;
        }

        private void Apply()
        {
            // Use cached easing value instead of recomputing
            float e = _cachedE;
            if (_motion == Motion.Hinge)
                transform.SetPositionAndRotation(_closedPos, Quaternion.AngleAxis(_angle * e, _axis) * _closedRot);
            else
                transform.SetPositionAndRotation(_closedPos + _offset * e, _closedRot);
        }

        /// <summary>A world point on the part as it sits right now, given where that point is when closed.</summary>
        public Vector3 CurrentPoint(Vector3 closedWorldPoint)
        {
            float e = _cachedE;
            if (_motion == Motion.Hinge)
                return _closedPos + Quaternion.AngleAxis(_angle * e, _axis) * (closedWorldPoint - _closedPos);
            return closedWorldPoint + _offset * e;
        }
    }
}
