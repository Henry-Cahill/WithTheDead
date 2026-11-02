using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// A stretch of a real building's ground-storey outside wall (one edge of its footprint), breakable as
    /// <see cref="BreakableWall"/> has it: the dead outside batter a hole through it to get at their prey inside, a survivor
    /// knocks one through with the build tool, a blast blows one open. The wall is the shell's (<see cref="CutawayPass"/>'s
    /// "Walls &lt;id&gt;" mesh, its outside face, its core and the rooms' paper inside all in one), so a hole is cut out of that
    /// mesh (and the cutaway's stand-in of it made again), and the site's own collider boxes lying in the wall are cut back round
    /// it. Never through the wall's doorways or windows, nor what stands out of it (a sign, a lamp, a step) or against it
    /// either side. Made for a building when its ground floor is first furnished, under its shell's walls (so it, and its holes,
    /// stay while the shell does); the holes are kept by the building and edge, so a shell built again gets them back.
    /// </summary>
    internal sealed class OutsideWall : BreakableWall
    {
        /// <summary>The ground storey's walls stop here (BuildingPass.GroundStorey): above it stand the storeys above.</summary>
        private const float GroundStorey = 3f;

        private EnterableBuilding _record;
        private Transform _site;
        private MeshFilter _walls;
        private long _id;
        private int _edge, _facing;
        private float _thick;
        private Vector2 _a, _d, _out;
        private readonly List<Mesh> _meshes = new List<Mesh>(4);
        private readonly HashSet<WallBreach> _shown = new HashSet<WallBreach>();

        internal override float HalfThick => _thick * 0.5f;
        // Its siding, its trims and the foundation's band stand out of it outside; the rooms' paper and skirting inside.
        protected override float CutDepth => HalfThick + 0.12f;
        internal override List<Mesh> Meshes => _meshes;

        /// <summary>The ground storey's outside walls of the building <paramref name="build"/> furnishes (its ground floor), edge
        /// by edge where a hole has room, made breakable: once for a shell (they stay with it while its piece stands).</summary>
        internal static void FitAll(InteriorBuild build)
        {
            InteriorPlan plan = build.Plan;
            EnterableBuilding rec = plan.Record;
            if (plan.Level != 0 || rec == null || rec.Footprint == null || build.Root == null) return;
            Transform site = build.Root.transform.parent;
            Transform shells = site != null ? site.Find("Building shells") : null;
            Transform walls = shells != null ? shells.Find("Walls " + plan.Id) : null;
            var mf = walls != null ? walls.GetComponent<MeshFilter>() : null;
            // Not baked yet (its piece still being built): fitted when the floor's next furnished.
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable || mf.sharedMesh.vertexCount == 0) return;
            List<Vector2> p = rec.Footprint.Pts;
            int n = p.Count;
            float t = rec.WallThickness > 0.01f ? rec.WallThickness : 0.22f;
            for (int i = 0; i < n; i++)
            {
                string name = "Outside wall " + i;
                if (walls.Find(name) != null) continue;   // this shell has it already
                Vector2 a = p[i], c = p[(i + 1) % n];
                float len = Vector2.Distance(a, c);
                if (len < Width + 2f * EndMargin) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                Vector2 prevD = (p[i] - p[(i + n - 1) % n]).normalized, nextD = (p[(i + 2) % n] - c).normalized;
                // As far as the wall's inner face runs (InteriorPass.Shell's corners).
                float s0 = Mathf.Max(0f, InnerEnd(prevD, d, t)), s1 = Mathf.Min(len, len - InnerEnd(d, nextD, t));
                if (s1 - s0 < Width + 2f * EndMargin) continue;
                Vector2 mid = a + d * ((s0 + s1) * 0.5f) - outward * (t * 0.5f);
                var go = new GameObject(name);
                go.transform.SetParent(walls, false);
                go.transform.position = site.TransformPoint(new Vector3(mid.x, 0f, mid.y));
                go.transform.rotation = Quaternion.LookRotation(new Vector3(outward.x, 0f, outward.y), Vector3.up);
                var w = go.AddComponent<OutsideWall>();
                w._record = rec;
                w._site = site;
                w._walls = mf;
                w._id = plan.Id;
                w._edge = i;
                w._facing = BuildingShell.Facing(outward);
                w._thick = t;
                w._a = a;
                w._d = d;
                w._out = outward;
                w._alongX = true;
                w._lo = -(s1 - s0) * 0.5f;
                w._hi = (s1 - s0) * 0.5f;
                w._height = GroundStorey;
                w._key = "outside@" + plan.Id + "#" + i;
                w.Begin();
            }
        }

        // How far along an edge its inner face starts where the previous edge meets it (InteriorPass.Shell's InnerEnd).
        private static float InnerEnd(Vector2 before, Vector2 after, float t)
        {
            float cross = before.x * after.y - before.y * after.x, dot = Vector2.Dot(before, after);
            if (Mathf.Abs(cross) < 0.02f && dot > 0f) return 0f;
            return Mathf.Clamp(t * cross / Mathf.Max(0.05f, 1f + dot), -2f * t, 4f * t);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _meshes.Count; i++)
                if (_meshes[i] != null) Destroy(_meshes[i]);
            _meshes.Clear();
        }

        // The dead outside batter it to get at their prey in the building.
        protected override bool CanBatter(Vector3 from, Vector3 towards) => !InFootprint(from) && InFootprint(towards);

        private bool InFootprint(Vector3 world)
        {
            Vector3 l = _site.InverseTransformPoint(world);
            List<Vector2> p = _record.Footprint.Pts;
            bool inside = false;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                if ((p[i].y > l.z) != (p[j].y > l.z) && l.x < (p[j].x - p[i].x) * (l.z - p[i].y) / (p[j].y - p[i].y) + p[i].x) inside = !inside;
            return inside;
        }

        protected override void CutMeshes(List<MeshFilter> into)
        {
            if (_walls != null) into.Add(_walls);
        }

        // Where its holes are, the site's boxes lying in the wall cut back round them; and the cutaway's stand-in of the walls
        // (a copy of the mesh just cut) made again when next it's wanted.
        protected override void Shape()
        {
            MapCutaway.WallsChanged(_id);
            foreach (WallBreach h in Holes)
                if (h != null && h.State != WallBreach.Intact) Carve(h);
        }

        private void Carve(WallBreach h)
        {
            Vector3 centre = World(h.Centre, h.Top * 0.5f, 0f), half = Size(Width * 0.5f - 0.02f, h.Top * 0.5f - 0.05f, HalfThick);
            int n = Physics.OverlapBoxNonAlloc(centre, half, s_Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = s_Hits[i];
                s_Hits[i] = null;
                if (!(col is BoxCollider box) || !box.enabled || box.transform.IsChildOf(transform)) continue;
                Transform bt = box.transform;
                float s0 = float.MaxValue, s1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue, c0 = float.MaxValue, c1 = float.MinValue;
                Vector3 e = box.size * 0.5f;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 l = transform.InverseTransformPoint(bt.TransformPoint(box.center + Corner(e, k)));
                    s0 = Mathf.Min(s0, l.x); s1 = Mathf.Max(s1, l.x);
                    y0 = Mathf.Min(y0, l.y); y1 = Mathf.Max(y1, l.y);
                    c0 = Mathf.Min(c0, l.z); c1 = Mathf.Max(c1, l.z);
                }
                if (c0 < -CutDepth || c1 > CutDepth) continue;   // not the wall's own: something standing in the way
                int along = Axis(bt.InverseTransformDirection(transform.right)), up = Axis(bt.InverseTransformDirection(transform.up));
                float across = (c0 + c1) * 0.5f;
                Piece(box, along, up, s0, h.A, y0, y1, across);
                Piece(box, along, up, h.B, s1, y0, y1, across);
                if (h.Top < y1 - 0.01f) Piece(box, along, up, Mathf.Max(s0, h.A), Mathf.Min(s1, h.B), Mathf.Max(y0, h.Top), y1, across);
                box.enabled = false;
                InteriorPass.Kill(box);
            }
        }

        // Which of a box's own axes a direction runs along.
        private static int Axis(Vector3 v)
        {
            v = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            return v.x >= v.y && v.x >= v.z ? 0 : v.y >= v.z ? 1 : 2;
        }

        // What's left of a box of the wall, from s0 to s1 along it and y0 to y1 up, as a box of its own on the same object.
        private void Piece(BoxCollider from, int along, int up, float s0, float s1, float y0, float y1, float across)
        {
            if (s1 - s0 < 0.02f || y1 - y0 < 0.02f) return;
            var bc = from.gameObject.AddComponent<BoxCollider>();
            Vector3 size = from.size;
            size[along] = s1 - s0;
            size[up] = y1 - y0;
            bc.size = size;
            bc.center = from.transform.InverseTransformPoint(World((s0 + s1) * 0.5f, (y0 + y1) * 0.5f, across));
            bc.sharedMaterial = from.sharedMaterial;
        }

        // Its doorways and windows (on the ground storey, low enough for a hole to reach), with room for their casings.
        protected override void Hangings()
        {
            foreach (Doorway d in _record.Doors)
            {
                if (d.Edge != _edge) continue;
                float at = AlongOf(transform.InverseTransformPoint(_site.TransformPoint(new Vector3(d.At.x, 0f, d.At.y))));
                Block(at - d.Width * 0.5f - 0.25f, at + d.Width * 0.5f + 0.25f, this);
            }
            foreach (WindowSpan w in _record.Windows)
            {
                if (w.Edge != _edge || w.Storey != 0 || w.Sill > Top) continue;
                float a = AlongAt(w.From), b = AlongAt(w.To);
                Block(Mathf.Min(a, b) - 0.15f, Mathf.Max(a, b) + 0.15f, this);
            }
        }

        // A distance along the footprint's edge, along the wall.
        private float AlongAt(float s)
        {
            Vector2 q = _a + _d * s - _out * HalfThick;
            return AlongOf(transform.InverseTransformPoint(_site.TransformPoint(new Vector3(q.x, 0f, q.y))));
        }

        // Its own mesh (what stands out of it: a sign, a lamp, a step, a casing) and the furnished ground floor's.
        protected override void NearMeshes(List<MeshFilter> into)
        {
            into.Clear();
            InteriorBuild ib = InteriorStreamer.Active != null ? InteriorStreamer.Active.BuildOf(_id, 0) : null;
            if (ib != null && ib.Root != null) ib.Root.GetComponentsInChildren(false, into);
            if (_walls != null) into.Add(_walls);
        }

        protected override bool IsOwnMesh(MeshFilter mf) => mf == _walls;

        /// <summary>True while the map's cutaway has this wall's facing lowered (its colliders still stand).</summary>
        internal bool CutAway => MapCutaway.FacingCut(_id, _facing);

        // A hole's parts hide with its wall's facing when the cutaway lowers it, as a working window's do.
        protected override void PartsChanged(WallBreach hole)
        {
            if (_shown.Add(hole)) MapCutaway.AddWindow(_id, 0, _facing, hole.gameObject);
            else MapCutaway.WindowPartsChanged(hole.gameObject);
        }
    }
}
