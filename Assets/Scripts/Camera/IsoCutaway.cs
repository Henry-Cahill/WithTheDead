using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Project Zomboid's wall cutaway for the isometric camera. A few times a second it looks from the
    /// camera to the survivor — a fat ray at the chest, thin ones at the head, the feet and either side of
    /// the waist — and hides whatever stands in the way: pieces marked with <see cref="CutawayOccluder"/>
    /// (the raid map's wall segments) first and foremost, and, for unmarked geometry such as the wave
    /// mode's starter-house walls, any Default-layer collider whose top rises more than
    /// <see cref="FallbackMinTop"/> above the survivor's feet.
    ///
    /// Hidden renderers switch to <see cref="ShadowCastingMode.ShadowsOnly"/>: they vanish from view but
    /// still cast shadows and keep room light in, and their colliders are untouched, so walls still block
    /// movement, shots and sight. A piece comes back <see cref="RestoreDelay"/> seconds after it last stood
    /// in the way, and everything comes back at once when the view leaves isometric. The ground, the
    /// survivor and the zombies are never touched. Allocation-free per frame: non-alloc casts into a fixed
    /// buffer, and each collider is classified once and cached (a marked piece's renderers are gathered
    /// again only when its parts are rebuilt — see <see cref="CutawayOccluder.PartsChanged"/>).
    /// </summary>
    public sealed class IsoCutaway
    {
        /// <summary>Seconds between occlusion scans.</summary>
        public float ScanInterval = 0.1f;

        /// <summary>Seconds a piece stays cut away after it last stood between the camera and the survivor.</summary>
        public float RestoreDelay = 0.4f;

        /// <summary>Unmarked Default-layer geometry is cut only if its collider's top is this far above the
        /// survivor's feet: walls, tall shelves and trees, never the ground, floors or low furniture.</summary>
        public float FallbackMinTop = 1.4f;

        /// <summary>Renderers whose top is lower than this above the survivor's feet stay visible when their
        /// piece is cut (floors, rugs, skirting), so a cut wall never takes the floor with it.</summary>
        public float KeepBelow = 0.3f;

        /// <summary>Radius of the sphere cast at the survivor's chest.</summary>
        public float ChestProbeRadius = 0.3f;

        /// <summary>How far left and right of the survivor the waist rays aim, widening the cut.</summary>
        public float SideProbe = 0.85f;

        /// <summary>A waist ray's hit only cuts when it lies at least this far in front of the survivor
        /// (toward the camera), so a wall the survivor stands beside or behind is never cut by it.</summary>
        public float SideProbeMargin = 0.35f;

        /// <summary>Pieces currently cut away.</summary>
        public int HiddenCount => _hidden.Count;

        // An unmarked piece hides at most this many renderers, so a stray collider on a big root can't
        // take a whole building with it.
        private const int MaxFallbackRenderers = 24;
        // ...nor any renderer reaching more than this far (each side) beyond the object's colliders.
        private const float MaxFallbackOverhang = 2f;
        // Classified colliders are forgotten past this many (only while nothing is cut away).
        private const int CacheLimit = 4096;

        /// <summary>What hides and returns as one: a marked occluder, or one unmarked collider's object.</summary>
        private sealed class Piece
        {
            public GameObject Root;
            public CutawayOccluder Marker;
            public Renderer[] Renderers;
            public int Parts;   // the marker's PartsVersion when Renderers was gathered
            public bool Hidden;
            public float LastSeen;
        }

        /// <summary>How a renderer was hidden. Pieces can share renderers, so it is reference counted.</summary>
        private struct HiddenRenderer
        {
            public int Count;
            public ShadowCastingMode Original;
            public bool ForcedOff;   // it cast no shadow to begin with, so its rendering is simply switched off
            public bool Foreign;     // already hidden by someone else when we got to it: left alone
        }

        // Colliders that never cut (the ground, low props, the survivor) all map to this.
        private static readonly Piece NotAnOccluder = new Piece { Renderers = new Renderer[0] };

        private readonly Dictionary<Collider, Piece> _byCollider = new Dictionary<Collider, Piece>(256);
        private readonly Dictionary<GameObject, Piece> _byRoot = new Dictionary<GameObject, Piece>(128);
        private readonly Dictionary<Renderer, HiddenRenderer> _renderers = new Dictionary<Renderer, HiddenRenderer>(128);
        private readonly List<Piece> _hidden = new List<Piece>(32);
        private readonly List<Renderer> _found = new List<Renderer>(32);
        private readonly List<Renderer> _keep = new List<Renderer>(32);
        private readonly List<Collider> _solids = new List<Collider>(8);
        private readonly RaycastHit[] _hits = new RaycastHit[32];

        private Transform _target;
        private Vector3 _eye;
        private Vector3 _forward = Vector3.forward;
        private bool _orthographic;
        private Vector3 _feet;
        private Vector3 _viewAhead = Vector3.forward;
        private float _feetY;
        private float _now;
        private float _nextScan;

        /// <summary>
        /// Runs the cutaway for one frame. <paramref name="eye"/> and <paramref name="forward"/> are the
        /// camera's position and view direction; with <paramref name="orthographic"/> the sight lines run
        /// parallel to the view (from the camera plane) instead of out of the eye. <paramref name="chestHeight"/>
        /// is measured from the survivor's feet. Pass <paramref name="active"/> false (any view but isometric)
        /// to put everything back.
        /// </summary>
        public void Tick(Vector3 eye, Vector3 forward, bool orthographic, Transform target, float chestHeight, bool active)
        {
            if (!active || target == null)
            {
                if (_hidden.Count > 0 || _renderers.Count > 0) RestoreAll();
                return;
            }

            _now = Time.time;
            if (_now >= _nextScan)
            {
                _nextScan = _now + ScanInterval;
                Scan(eye, forward, orthographic, target, chestHeight);
            }

            for (int i = _hidden.Count - 1; i >= 0; i--)
            {
                Piece p = _hidden[i];
                bool gone = p.Root == null;   // knocked down or dismantled while cut away
                if (!gone && _now - p.LastSeen <= RestoreDelay)
                {
                    Refresh(p);   // a barricade that nailed on or lost a plank this frame hides its new boards too
                    continue;
                }
                Show(p);
                _hidden.RemoveAt(i);
                if (gone) _byRoot.Remove(p.Root);
            }

            if (_byCollider.Count > CacheLimit && _hidden.Count == 0)
            {
                _byCollider.Clear();
                _byRoot.Clear();
            }
        }

        /// <summary>Brings every cut-away piece back at once (leaving isometric, or the camera going away).</summary>
        public void RestoreAll()
        {
            for (int i = _hidden.Count - 1; i >= 0; i--) Show(_hidden[i]);
            _hidden.Clear();
            foreach (KeyValuePair<Renderer, HiddenRenderer> kv in _renderers) Restore(kv.Key, kv.Value);
            _renderers.Clear();
        }

        private void Scan(Vector3 eye, Vector3 forward, bool orthographic, Transform target, float chestHeight)
        {
            _target = target;
            _eye = eye;
            _forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            _orthographic = orthographic;
            Vector3 feet = target.position;
            _feet = feet;
            _feetY = feet.y;
            Vector3 ahead = _forward;
            ahead.y = 0f;
            _viewAhead = ahead.sqrMagnitude > 1e-4f ? ahead.normalized : Vector3.forward;   // the heading on the ground
            Vector3 right = new Vector3(_viewAhead.z, 0f, -_viewAhead.x);

            Probe(feet + Vector3.up * chestHeight, ChestProbeRadius, false);    // the chest, with a fat ray
            Probe(feet + Vector3.up * (chestHeight + 0.55f), 0f, false);         // the head
            Probe(feet + Vector3.up * 0.25f, 0f, false);                        // the feet and the floor round them
            Probe(feet + right * SideProbe + Vector3.up * 0.9f, 0f, true);      // either side of the waist, so
            Probe(feet - right * SideProbe + Vector3.up * 0.9f, 0f, true);      // the cut is wider than the body
        }

        // Casts along the sight line to a point on (or beside) the survivor and cuts what it meets. An
        // orthographic view's sight lines are parallel to the view, starting from the camera plane; a
        // perspective one's leave the eye. A side probe's point can lie beyond a wall the survivor stands
        // beside, so its hits only count when they are clearly on the camera's side of the survivor: a wall
        // behind them is never cut.
        private void Probe(Vector3 to, float radius, bool side)
        {
            Vector3 from = _orthographic ? to - _forward * Vector3.Dot(to - _eye, _forward) : _eye;
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.1f) return;
            dir /= dist;
            dist -= radius + 0.05f;   // stop short, so nothing just behind the survivor is ever caught
            if (dist <= 0f) return;

            int mask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
            int n = radius > 0f
                ? Physics.SphereCastNonAlloc(from, radius, dir, _hits, dist, mask, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(from, dir, _hits, dist, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i].collider;
                if (c == null) continue;
                if (side && Vector3.Dot(_hits[i].point - _feet, _viewAhead) >= -SideProbeMargin) continue;
                Piece p = Classify(c);
                if (p == NotAnOccluder) continue;
                p.LastSeen = _now;
                if (!p.Hidden) Hide(p);
            }
        }

        // The piece a collider belongs to, worked out on first sight and cached.
        private Piece Classify(Collider c)
        {
            if (_byCollider.TryGetValue(c, out Piece p))
            {
                // If we have a cached piece that references a disabled CutawayOccluder,
                // treat it as “not an occluder” and rebuild so the wall can become visible again.
                if (p != NotAnOccluder && p.Marker != null && !p.Marker.enabled)
                {
                    _byCollider.Remove(c);
                    p = null;
                }
                else if (p == NotAnOccluder || p.Root != null)
                {
                    return p;
                }
            }
            p = Build(c);
            _byCollider[c] = p;
            return p;
        }

        private Piece Build(Collider c)
        {
            GameObject go = c.gameObject;
            if (go.layer == CloneZConfig.IgnoreRaycastLayer) return NotAnOccluder;        // zombies
            if (_target != null && c.transform.IsChildOf(_target)) return NotAnOccluder;   // the survivor

            CutawayOccluder marker = c.GetComponentInParent<CutawayOccluder>();
            if (marker != null)
                return marker.enabled ? PieceFor(marker.gameObject, marker, null) : NotAnOccluder;

            // Unmarked: only tall solids on the Default layer, which leaves the ground, floors and low
            // furniture alone.
            if (go.layer != 0 || c.bounds.max.y < _feetY + FallbackMinTop) return NotAnOccluder;
            return PieceFor(go, null, c);
        }

        private Piece PieceFor(GameObject root, CutawayOccluder marker, Collider fallback)
        {
            if (_byRoot.TryGetValue(root, out Piece p) && p.Root != null) return p;

            p = new Piece { Root = root, Marker = marker, Renderers = Gather(root, fallback), Parts = marker != null ? marker.PartsVersion : 0 };
            _byRoot[root] = p;
            return p;
        }

        // Gathers the renderers that hide with a piece: everything under a marked occluder, or for an
        // unmarked object only what overlaps its own colliders (a doorway's two jambs and lintel, a
        // window's sill and head) and isn't far bigger than them, so a building-wide merged mesh is never
        // taken along with one tall prop. Floor-level parts always stay.
        private Renderer[] Gather(GameObject root, Collider fallback)
        {
            _found.Clear();
            _keep.Clear();
            root.GetComponentsInChildren<Renderer>(false, _found);
            Bounds near = default;
            if (fallback != null)
            {
                near = fallback.bounds;
                root.GetComponents<Collider>(_solids);
                for (int i = 0; i < _solids.Count; i++)
                {
                    Collider other = _solids[i];
                    if (other != null && other.enabled && !other.isTrigger) near.Encapsulate(other.bounds);
                }
                _solids.Clear();
                near.Expand(0.5f);
            }
            float keepBelow = _feetY + KeepBelow;
            for (int i = 0; i < _found.Count; i++)
            {
                Renderer r = _found[i];
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;   // no particles, lines or trails
                if (r.gameObject.layer == CloneZConfig.IgnoreRaycastLayer) continue;
                if (_target != null && r.transform.IsChildOf(_target)) continue;
                Bounds b = r.bounds;
                if (b.max.y < keepBelow) continue;
                if (fallback != null && (!near.Intersects(b)
                    || b.extents.x > near.extents.x + MaxFallbackOverhang
                    || b.extents.z > near.extents.z + MaxFallbackOverhang)) continue;
                _keep.Add(r);
                if (fallback != null && _keep.Count >= MaxFallbackRenderers) break;
            }
            return _keep.ToArray();
        }

        // Gathers a marked piece's renderers afresh once its parts have been rebuilt (a barricade nails on or
        // loses a plank), so while it's cut away its new parts hide with it and its marker still tells the truth.
        private void Refresh(Piece p)
        {
            if (p.Marker == null || p.Marker.PartsVersion == p.Parts) return;
            if (p.Hidden) Release(p.Renderers);
            p.Renderers = Gather(p.Root, null);
            p.Parts = p.Marker.PartsVersion;
            if (p.Hidden) Hold(p.Renderers);
        }

        private void Hide(Piece p)
        {
            Refresh(p);
            p.Hidden = true;
            _hidden.Add(p);
            if (p.Marker != null) p.Marker.IsCutAway = true;
            Hold(p.Renderers);
        }

        private void Show(Piece p)
        {
            p.Hidden = false;
            if (p.Marker != null) p.Marker.IsCutAway = false;
            Release(p.Renderers);
        }

        // Stops a piece's renderers drawing; they keep their shadows. Pieces can share renderers, so each counts
        // how many pieces hold it.
        private void Hold(Renderer[] rs)
        {
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null) continue;
                if (_renderers.TryGetValue(r, out HiddenRenderer h))
                {
                    h.Count++;
                    _renderers[r] = h;
                    continue;
                }

                h = new HiddenRenderer { Count = 1 };
                ShadowCastingMode mode = r.shadowCastingMode;
                if (mode == ShadowCastingMode.ShadowsOnly || r.forceRenderingOff) h.Foreign = true;
                else if (mode == ShadowCastingMode.Off)
                {
                    h.ForcedOff = true;
                    r.forceRenderingOff = true;
                }
                else
                {
                    h.Original = mode;
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;   // unseen, but still shades the room
                }
                _renderers[r] = h;
            }
        }

        // Lets go of a piece's renderers; each draws again once no piece holds it.
        private void Release(Renderer[] rs)
        {
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (!_renderers.TryGetValue(r, out HiddenRenderer h)) continue;
                if (--h.Count > 0)
                {
                    _renderers[r] = h;
                    continue;
                }
                _renderers.Remove(r);
                Restore(r, h);
            }
        }

        private static void Restore(Renderer r, HiddenRenderer h)
        {
            if (r == null || h.Foreign) return;
            if (h.ForcedOff) r.forceRenderingOff = false;
            else if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) r.shadowCastingMode = h.Original;
        }
    }
}
