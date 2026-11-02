using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A stretch of a real building's wall on the statewide map that can be broken through, as the wave map's starter house walls
    /// can: an inside partition (<see cref="PartitionWall"/>) or a stretch of the ground storey's outside wall
    /// (<see cref="OutsideWall"/>). The dead batter a hole in it where their prey is on the far side and they can't get round
    /// (<see cref="ZombieAI"/> finds the place through <see cref="NearestObstructing"/>), a survivor knocks one through with the
    /// build tool's Demolish (aimed: see <see cref="IDemolishAim"/>), and a blast (a breaching charge, a grenade) blows one
    /// open. A hole (<see cref="WallBreach"/>) is a doorway wide and tall, cut out of the wall's meshes and colliders, clear of
    /// its ends and of whatever hangs on it or stands against it; a survivor can nail planks across it again, and the dead
    /// batter those down. The holes are kept by the wall's key while the raid lasts, so a building taken down behind the
    /// survivors and raised again still has them. Its object's frame is the wall's: along it, up from its floor, across it
    /// (either side of its middle).
    /// </summary>
    internal abstract class BreakableWall : MonoBehaviour, IDemolishable, IDemolishAim
    {
        /// <summary>The breakable walls standing now (none off the statewide map).</summary>
        internal static readonly List<BreakableWall> All = new List<BreakableWall>(256);

        /// <summary>A hole kept while its building is down: where it is along the wall, and whether (from which side, how
        /// soundly) it's boarded over.</summary>
        internal struct Kept
        {
            public float Centre;
            public bool Boarded;
            public int Side;
            public float Health;
        }

        protected const float EndMargin = 0.2f;   // wall left standing at either end of a hole (a corner, a doorway's casing)
        protected const float Against = 0.55f;    // how far off its faces something counts as standing against it

        private static readonly Dictionary<string, List<Kept>> s_Kept = new Dictionary<string, List<Kept>>(64);
        private static object s_KeptFor;   // the map the holes were made in (a new raid starts with none)
        private static readonly Blast s_Blast = new Blast();
        protected static readonly Collider[] s_Hits = new Collider[64];
        protected static readonly List<Renderer> s_Renderers = new List<Renderer>(8);
        private static readonly List<Collider> s_Colliders = new List<Collider>(8);
        protected static readonly List<Vector3> s_Vertices = new List<Vector3>(256);
        private static readonly List<float> s_Alongs = new List<float>(256);
        private static readonly List<Vector2> s_Forbid = new List<Vector2>(16);
        private static readonly List<Vector2> s_Spots = new List<Vector2>(8);
        private static readonly List<Vector2> s_Stretches = new List<Vector2>(64);
        private static readonly List<MeshFilter> s_Filters = new List<MeshFilter>(128);
        private static readonly List<int> s_Tris = new List<int>(1024);
        protected static readonly System.Comparison<WallBreach> s_ByStart = (x, y) => x.A.CompareTo(y.A);
        // From a wall along its object's z to the box the cut is made in: x along it, y up, z across it.
        private static readonly Matrix4x4 s_Swap = new Matrix4x4(new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 1f, 0f, 0f),
            new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 1f));

        protected bool _alongX = true;
        protected float _lo, _hi, _height;
        protected string _key;
        protected Bounds _near;
        protected readonly List<WallBreach> _holes = new List<WallBreach>(1);
        private readonly List<Vector2> _blocked = new List<Vector2>(8);
        private float _blockedAt = float.NegativeInfinity;
        private float _aim = float.NaN;
        private WallBreach _aimHole;
        private string _aimWhy;

        /// <summary>True when the wall runs along its object's x (else its z).</summary>
        internal bool AlongX => _alongX;
        /// <summary>How high it stands (its object's floor is 0).</summary>
        internal float Height => _height;
        /// <summary>Half its thickness: its faces stand this far either side of its middle.</summary>
        internal abstract float HalfThick { get; }
        /// <summary>Half the depth a hole is cut through: the wall, its paper, its trims (and outside, its siding and base).</summary>
        protected virtual float CutDepth => HalfThick + 0.06f;
        /// <summary>How wide a hole is.</summary>
        internal static float Width => CloneZConfig.RaidPartitionBreachWidth;
        /// <summary>How high a hole goes: a tall doorway's height, or the whole wall where that's all but it.</summary>
        internal float Top => _height - CloneZConfig.RaidPartitionBreachHeight < 0.25f ? _height + 0.05f : CloneZConfig.RaidPartitionBreachHeight;
        /// <summary>Where the meshes made for its holes are listed, to go when they do (null: nowhere).</summary>
        internal abstract List<Mesh> Meshes { get; }
        /// <summary>The holes in it, and the places in it being battered.</summary>
        internal IReadOnlyList<WallBreach> Holes => _holes;
        /// <summary>How long it is.</summary>
        internal float Length => _hi - _lo;
        /// <summary>The middle of the hole the build tool's last aim picked (NaN when it picked none).</summary>
        internal float Aimed => _aim;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear();
            s_Kept.Clear();
            s_KeptFor = null;
            CombatResolver.Breakables.Remove(s_Blast);
        }

        /// <summary>The frame set (<see cref="_alongX"/>, <see cref="_lo"/>, <see cref="_hi"/>, <see cref="_height"/>,
        /// <see cref="_key"/>): the quick test's bounds worked out and the holes kept for it put back.</summary>
        protected void Begin()
        {
            Vector3 a = World(_lo, 0f, 0f), b = World(_hi, _height, 0f);
            _near = new Bounds(a, Vector3.zero);
            _near.Encapsulate(b);
            _near.Expand(new Vector3(7f, 4f, 7f));
            Restore();
        }

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (!CombatResolver.Breakables.Contains(s_Blast)) CombatResolver.Breakables.Add(s_Blast);
        }

        protected virtual void OnDisable() => All.Remove(this);

        #region What sets one wall apart
        /// <summary>True when the dead at <paramref name="from"/> may batter this wall to get at <paramref name="towards"/>
        /// (a partition: they're in the building; an outside wall: they're outside it, their prey in it).</summary>
        protected abstract bool CanBatter(Vector3 from, Vector3 towards);

        /// <summary>The meshes a hole is cut out of (each in its own object's space, taken to the wall's for the cut).</summary>
        protected abstract void CutMeshes(List<MeshFilter> into);

        /// <summary>The wall's colliders made again round its holes (open or boarded: the boards have their own).</summary>
        protected abstract void Shape();

        /// <summary>What hangs on it, for <see cref="Blocked"/> (a partition's switches, panel and fittings; an outside wall's
        /// doorways and windows): see <see cref="Block"/>.</summary>
        protected virtual void Hangings() { }

        /// <summary>The render meshes the NavMesh is baked from that might stand against it, for <see cref="Blocked"/>; those
        /// <see cref="IsOwnMesh"/> counts only where they stand out beyond its cut.</summary>
        protected virtual void NearMeshes(List<MeshFilter> into) { }

        /// <summary>True for a mesh the wall itself is drawn in (an outside wall's shell): only what stands out of it blocks.</summary>
        protected virtual bool IsOwnMesh(MeshFilter mf) => false;

        /// <summary>The top of the tree <see cref="NearMeshes"/> came from (NavMeshModifiers above it don't count).</summary>
        protected virtual Transform MeshesRoot => null;

        /// <summary>A hole opened, boarded or unboarded: its parts handed to whatever hides the wall (the cutaway).</summary>
        protected virtual void PartsChanged(WallBreach hole) { }

        /// <summary>A hole broken through: whatever hung across it comes down.</summary>
        protected virtual void Fell(WallBreach hole) { }

        /// <summary>A blow on a hole (or on the wall where one's being battered): what hangs near it may be knocked askew.</summary>
        internal virtual void Jolted(WallBreach hole) { }
        #endregion

        #region The wall's own frame
        protected float AlongOf(Vector3 local) => _alongX ? local.x : local.z;
        protected float AcrossOf(Vector3 local) => _alongX ? local.z : local.x;

        /// <summary>A point in the wall's object: so far along it, so high, so far across it.</summary>
        internal Vector3 Local(float along, float y, float across) => _alongX ? new Vector3(along, y, across) : new Vector3(across, y, along);

        /// <summary>A box's size in the wall's object: so long along it, so high, so thick across it.</summary>
        internal Vector3 Size(float along, float y, float across) => _alongX ? new Vector3(along, y, across) : new Vector3(across, y, along);

        protected Vector3 World(float along, float y, float across) => transform.TransformPoint(Local(along, y, across));

        /// <summary>The side of the wall (+1 or -1, across it) a world point is on.</summary>
        internal int SideOf(Vector3 world) => AcrossOf(transform.InverseTransformPoint(world)) >= 0f ? 1 : -1;
        #endregion

        #region Where a hole goes
        /// <summary>
        /// The place in a wall the dead at <paramref name="from"/> should batter to get at <paramref name="towards"/>: in a
        /// wall standing between them that they may batter (see <see cref="CanBatter"/>), where the straight line from one to
        /// the other crosses it within <paramref name="range"/> of it (a place being battered already, boards nailed across a
        /// hole, or a new one). Null when there's none, or the way through is open there already.
        /// </summary>
        internal static WallBreach NearestObstructing(Vector3 from, Vector3 towards, float range)
        {
            s_Candidates.Clear();
            int near = 0, between = 0;
            for (int i = 0; i < All.Count; i++)
            {
                BreakableWall w = All[i];
                if (w == null) continue;
                Bounds n = w._near;
                if (from.x < n.min.x || from.x > n.max.x || from.z < n.min.z || from.z > n.max.z) continue;
                Vector3 f = w.transform.InverseTransformPoint(from), t = w.transform.InverseTransformPoint(towards);
                if (f.y < -1f || f.y > 1.6f || t.y < -1.6f || t.y > 1.9f) continue;   // not both on its floor
                near++;
                float fa = w.AcrossOf(f), ta = w.AcrossOf(t);
                if (fa * ta >= 0f) continue;   // not either side of it
                float fu = w.AlongOf(f), tu = w.AlongOf(t);
                float u = fu + (tu - fu) * (fa / (fa - ta));
                if (u < w._lo || u > w._hi) continue;
                float sqr = (u - fu) * (u - fu) + fa * fa;
                if (sqr > range * range) continue;
                between++;
                if (!w.CanBatter(from, towards)) continue;
                s_Candidates.Add(new Candidate { Wall = w, Along = u, Sqr = sqr });
            }
            if (s_Candidates.Count == 0)
            {
                LastMiss = near == 0 ? "no wall near" : between == 0 ? "no wall between them in reach" : "no wall of theirs to batter";
                return null;
            }
            // The nearest wall in the way first; one with no room for a hole there (a wall meeting it, what stands against it)
            // passes to the next.
            if (s_Candidates.Count > 1) s_Candidates.Sort((x, y) => x.Sqr.CompareTo(y.Sqr));
            for (int i = 0; i < s_Candidates.Count && i < 3; i++)
            {
                Candidate c = s_Candidates[i];
                if (!c.Wall.SpotFor(c.Along, 1f, true, out float spot, out WallBreach hole, out string why))
                {
                    LastMiss = why;
                    continue;
                }
                s_Candidates.Clear();
                if (hole != null)
                {
                    if (!hole.IsOpen) return hole;
                    LastMiss = "broken through there already";
                    return null;
                }
                return c.Wall.Make(spot);
            }
            s_Candidates.Clear();
            return null;
        }

        /// <summary>Why the last <see cref="NearestObstructing"/> that found nothing found nothing (for the harnesses).</summary>
        internal static string LastMiss;

        private struct Candidate
        {
            public BreakableWall Wall;
            public float Along, Sqr;
        }

        private static readonly List<Candidate> s_Candidates = new List<Candidate>(4);

        /// <summary>
        /// Where a hole would go nearest <paramref name="want"/> along the wall, no further off than <paramref name="slack"/>: a
        /// hole there already (or a place being battered, <paramref name="hole"/>), else room for a new one, its middle in
        /// <paramref name="spot"/>, clear of the wall's ends, of what hangs on it or stands against it, and of its other holes;
        /// <paramref name="walkable"/>, only where the NavMesh lies in front of it both sides (the dead's holes lead somewhere).
        /// False, with <paramref name="why"/>, when there's no room for one there.
        /// </summary>
        internal bool SpotFor(float want, float slack, bool walkable, out float spot, out WallBreach hole, out string why)
        {
            spot = want;
            hole = null;
            why = null;
            for (int i = 0; i < _holes.Count; i++)
            {
                WallBreach h = _holes[i];
                if (h == null || want < h.A - 0.05f || want > h.B + 0.05f) continue;
                hole = h;
                spot = h.Centre;
                return true;
            }
            float half = Width * 0.5f, min = _lo + EndMargin + half, max = _hi - EndMargin - half;
            if (min > max)
            {
                why = "There's too little wall here to break through";
                return false;
            }
            Blocked();
            s_Forbid.Clear();
            for (int i = 0; i < _blocked.Count; i++) s_Forbid.Add(new Vector2(_blocked[i].x - half, _blocked[i].y + half));
            for (int i = 0; i < _holes.Count; i++)
                if (_holes[i] != null) s_Forbid.Add(new Vector2(_holes[i].A - half, _holes[i].B + half));
            s_Spots.Clear();
            Try(Mathf.Clamp(want, min, max), want, min, max, slack);
            for (int i = 0; i < s_Forbid.Count; i++)
            {
                Try(s_Forbid[i].x, want, min, max, slack);
                Try(s_Forbid[i].y, want, min, max, slack);
            }
            s_Forbid.Clear();
            s_Spots.Sort((x, y) => x.y.CompareTo(y.y));
            bool found = false;
            for (int i = 0; i < s_Spots.Count && i < 4 && !found; i++)
            {
                if (walkable && !Walkable(s_Spots[i].x)) continue;
                spot = s_Spots[i].x;
                found = true;
            }
            if (!found)
                why = s_Spots.Count > 0 ? "There's no way through to be had there" : "Something's in the way there, on the wall or against it";
            s_Spots.Clear();
            return found;
        }

        // A hole's middle at c (kept between min and max), as a candidate (with how far it is from want) unless something
        // forbids it there or it's further off than slack.
        private static void Try(float c, float want, float min, float max, float slack)
        {
            if (c < min - 1e-4f || c > max + 1e-4f) return;
            c = Mathf.Clamp(c, min, max);
            float off = Mathf.Abs(c - want);
            if (off > slack) return;
            for (int i = 0; i < s_Forbid.Count; i++)
                if (c > s_Forbid[i].x + 1e-4f && c < s_Forbid[i].y - 1e-4f) return;
            for (int i = 0; i < s_Spots.Count; i++)
                if (Mathf.Abs(s_Spots[i].x - c) < 0.01f) return;
            s_Spots.Add(new Vector2(c, off));
        }

        /// <summary>True when a body could walk up to a hole with its middle at <paramref name="centre"/> from either side: the
        /// NavMesh (as baked now) comes as near the wall as it does to a clear one (its agents' reach off the wall's face) both
        /// sides, at the hole's middle and half a metre either way: anything standing there, solid or not (a bench, a box of
        /// litter), keeps it further off.</summary>
        internal bool Walkable(float centre)
        {
            float floor = transform.position.y;
            for (int side = -1; side <= 1; side += 2)
                for (int k = -1; k <= 1; k++)
                {
                    Vector3 p = World(centre + k * 0.5f, 0.1f, side * (HalfThick + 0.65f));
                    if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 0.15f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - floor) > 0.4f)
                        return false;
                }
            return true;
        }

        // What a hole mustn't break into, as stretches along the wall: whatever hangs on it (Hangings) and whatever stands
        // against it either side, solid (furniture, a wall meeting it, a door swung back against it) or baked into the NavMesh
        // (a bench, a box of litter, what stands out of the wall's own mesh). Looked at afresh every couple of seconds at most:
        // furniture gets broken up, doors swing.
        private void Blocked()
        {
            float now = Time.realtimeSinceStartup;
            if (Explain == null && now >= _blockedAt && now - _blockedAt < 2f) return;
            _blockedAt = now;
            _blocked.Clear();
            Hangings();
            Standing();
            Baked();
        }

        // Whatever the NavMesh is baked from that stands against it either side, solid or not (a bench, a box of litter, the
        // furniture merged into its room's mesh, a wall meeting it; in its own mesh, what stands out beyond the cut): where
        // its triangles come within reach, from a hand's breadth off the floor to head height.
        private void Baked()
        {
            NearMeshes(s_Filters);
            if (s_Filters.Count == 0) return;
            float reach = HalfThick + Against, y0 = 0.15f, y1 = 1.9f, own = CutDepth;
            var strip = new Bounds(World((_lo + _hi) * 0.5f, (y0 + y1) * 0.5f, 0f), Vector3.zero);
            for (int k = 0; k < 8; k++) strip.Encapsulate(World((k & 1) != 0 ? _hi : _lo, (k & 2) != 0 ? y1 : y0, (k & 4) != 0 ? reach : -reach));
            for (int i = 0; i < s_Filters.Count; i++)
            {
                MeshFilter mf = s_Filters[i];
                if (mf == null || mf.transform.IsChildOf(transform)) continue;
                Mesh mesh = mf.sharedMesh;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mesh == null || !mesh.isReadable || mr == null || !mr.enabled || !mr.bounds.Intersects(strip) || OffNav(mf.transform)) continue;
                bool mine = IsOwnMesh(mf);
                Matrix4x4 m = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                mesh.GetVertices(s_Vertices);
                s_Stretches.Clear();
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    mesh.GetTriangles(s_Tris, s);
                    for (int t = 0; t + 2 < s_Tris.Count; t += 3)
                    {
                        Vector3 a = m.MultiplyPoint3x4(s_Vertices[s_Tris[t]]), b = m.MultiplyPoint3x4(s_Vertices[s_Tris[t + 1]]),
                            c = m.MultiplyPoint3x4(s_Vertices[s_Tris[t + 2]]);
                        if (Mathf.Max(a.y, b.y, c.y) < y0 || Mathf.Min(a.y, b.y, c.y) > y1) continue;
                        float ca = AcrossOf(a), cb = AcrossOf(b), cc = AcrossOf(c);
                        float least = Mathf.Min(ca, Mathf.Min(cb, cc)), most = Mathf.Max(ca, Mathf.Max(cb, cc));
                        if (least > reach || most < -reach) continue;
                        if (mine && least >= -own && most <= own) continue;   // the wall itself, its paper and trims
                        float la = AlongOf(a), lb = AlongOf(b), lc = AlongOf(c);
                        float lo = Mathf.Min(la, Mathf.Min(lb, lc)), hi = Mathf.Max(la, Mathf.Max(lb, lc));
                        if (hi < _lo - 0.1f || lo > _hi + 0.1f) continue;
                        s_Stretches.Add(new Vector2(lo, hi));
                    }
                }
                Merged(mf);
            }
            s_Filters.Clear();
            s_Vertices.Clear();
            s_Tris.Clear();
        }

        // The stretches one mesh's triangles came within reach over, run together, as what it blocks.
        private void Merged(MeshFilter mf)
        {
            if (s_Stretches.Count == 0) return;
            s_Stretches.Sort((x, y) => x.x.CompareTo(y.x));
            Vector2 cur = s_Stretches[0];
            for (int i = 1; i < s_Stretches.Count; i++)
            {
                Vector2 next = s_Stretches[i];
                if (next.x <= cur.y + 0.05f)
                {
                    cur.y = Mathf.Max(cur.y, next.y);
                    continue;
                }
                Block(cur.x - 0.05f, cur.y + 0.05f, mf);
                cur = next;
            }
            Block(cur.x - 0.05f, cur.y + 0.05f, mf);
            s_Stretches.Clear();
        }

        // True when a NavMeshModifier on the object (or one above it, up to MeshesRoot) keeps it out of the NavMesh bake.
        private bool OffNav(Transform t)
        {
            Transform top = MeshesRoot;
            for (Transform p = t; p != null && p != top; p = p.parent)
            {
                var mod = p.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
                if (mod != null && mod.ignoreFromBuild && (p == t || mod.applyToChildren)) return true;
            }
            return false;
        }

        /// <summary>Something hung on the wall (an object of its own: everything it draws and every collider it has), blocking
        /// the stretch it covers; a mesh of several fittings hung along it (RaidBuilding's "Fittings") each where its vertices
        /// gather.</summary>
        protected void Hung(Transform c)
        {
            var mf = c.GetComponent<MeshFilter>();
            Mesh mesh = mf != null ? mf.sharedMesh : null;
            if (c.name == "Fittings" && mesh != null && mesh.isReadable)
            {
                Clusters(c, mesh);
                return;
            }
            float lo = float.MaxValue, hi = float.MinValue;
            Spread(transform.InverseTransformPoint(c.position), ref lo, ref hi);
            c.GetComponentsInChildren(true, s_Renderers);
            for (int k = 0; k < s_Renderers.Count; k++)
            {
                Renderer r = s_Renderers[k];
                if (r != null && r.enabled && r.bounds.size != Vector3.zero) Corners(r.bounds, ref lo, ref hi);
            }
            c.GetComponentsInChildren(true, s_Colliders);
            for (int k = 0; k < s_Colliders.Count; k++)
            {
                Collider col = s_Colliders[k];
                if (col == null || !col.enabled) continue;
                if (col is BoxCollider b) BoxCorners(b, ref lo, ref hi);
                else Corners(col.bounds, ref lo, ref hi);
            }
            s_Renderers.Clear();
            s_Colliders.Clear();
            Block(lo - 0.1f, hi + 0.1f, c);
        }

        /// <summary>For the harness: what blocked each stretch of the wall when it was last looked at.</summary>
        internal static List<string> Explain;

        /// <summary>A stretch along the wall a hole mustn't reach into, and (for the harness) what blocks it.</summary>
        protected void Block(float lo, float hi, Object why)
        {
            _blocked.Add(new Vector2(lo, hi));
            if (Explain != null) Explain.Add(lo.ToString("0.0") + ".." + hi.ToString("0.0") + " " + (why is Component c ? c.transform.parent != null
                ? c.transform.parent.name + "/" + c.name : c.name : why != null ? why.name : "?"));
        }

        // The fittings hung along it (one mesh, RaidBuilding's), each where its vertices gather.
        private void Clusters(Transform c, Mesh mesh)
        {
            mesh.GetVertices(s_Vertices);
            s_Alongs.Clear();
            Matrix4x4 m = transform.worldToLocalMatrix * c.localToWorldMatrix;
            for (int k = 0; k < s_Vertices.Count; k++) s_Alongs.Add(AlongOf(m.MultiplyPoint3x4(s_Vertices[k])));
            s_Vertices.Clear();
            if (s_Alongs.Count == 0) return;
            s_Alongs.Sort();
            float start = s_Alongs[0], end = start;
            for (int k = 1; k < s_Alongs.Count; k++)
            {
                float a = s_Alongs[k];
                if (a - end > 0.15f)
                {
                    Block(start - 0.08f, end + 0.08f, c);
                    start = a;
                }
                end = a;
            }
            Block(start - 0.08f, end + 0.08f, c);
            s_Alongs.Clear();
        }

        // Whatever solid stands within reach of either face, from a hand's breadth off the floor to head height.
        private void Standing()
        {
            Vector3 centre = World((_lo + _hi) * 0.5f, 1f, 0f);
            Vector3 half = Size((_hi - _lo) * 0.5f + 0.1f, 0.85f, HalfThick + Against);
            int n = Physics.OverlapBoxNonAlloc(centre, half, s_Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = s_Hits[i];
                s_Hits[i] = null;
                if (col == null || !col.enabled || col.transform.IsChildOf(transform)) continue;
                Rigidbody body = col.attachedRigidbody;
                if (body != null && !body.isKinematic) continue;   // loose things: kicked aside
                if (col is CharacterController || col.GetComponentInParent<NavMeshAgent>() != null || col.GetComponentInParent<Actor>() != null) continue;
                float lo = float.MaxValue, hi = float.MinValue, least = float.MaxValue, most = float.MinValue;
                if (col is BoxCollider box) BoxCorners(box, ref lo, ref hi, ref least, ref most);
                else Corners(col.bounds, ref lo, ref hi, ref least, ref most);
                // Lying within the wall's own thickness, it's the wall (an outside wall's colliders are the site's own boxes).
                if (least >= -CutDepth && most <= CutDepth) continue;
                if (lo > hi) continue;
                Block(lo - 0.05f, hi + 0.05f, col);
                if (Explain != null) Explain[Explain.Count - 1] += " (across " + least.ToString("0.00") + ".." + most.ToString("0.00") + ")";
            }
        }

        private void Spread(Vector3 local, ref float lo, ref float hi, ref float least, ref float most)
        {
            Spread(local, ref lo, ref hi);
            float c = AcrossOf(local);
            if (c < least) least = c;
            if (c > most) most = c;
        }

        private void Corners(Bounds b, ref float lo, ref float hi, ref float least, ref float most)
        {
            Vector3 c = b.center, e = b.extents;
            for (int k = 0; k < 8; k++)
                Spread(transform.InverseTransformPoint(c + Corner(e, k)), ref lo, ref hi, ref least, ref most);
        }

        private void BoxCorners(BoxCollider box, ref float lo, ref float hi, ref float least, ref float most)
        {
            Vector3 c = box.center, e = box.size * 0.5f;
            Transform t = box.transform;
            for (int k = 0; k < 8; k++)
                Spread(transform.InverseTransformPoint(t.TransformPoint(c + Corner(e, k))), ref lo, ref hi, ref least, ref most);
        }

        protected void Spread(Vector3 local, ref float lo, ref float hi)
        {
            float a = AlongOf(local);
            if (a < lo) lo = a;
            if (a > hi) hi = a;
        }

        protected void Corners(Bounds b, ref float lo, ref float hi)
        {
            Vector3 c = b.center, e = b.extents;
            for (int k = 0; k < 8; k++)
                Spread(transform.InverseTransformPoint(c + Corner(e, k)), ref lo, ref hi);
        }

        protected void BoxCorners(BoxCollider box, ref float lo, ref float hi)
        {
            Vector3 c = box.center, e = box.size * 0.5f;
            Transform t = box.transform;
            for (int k = 0; k < 8; k++)
                Spread(transform.InverseTransformPoint(t.TransformPoint(c + Corner(e, k))), ref lo, ref hi);
        }

        protected static Vector3 Corner(Vector3 e, int k)
            => new Vector3((k & 1) != 0 ? e.x : -e.x, (k & 2) != 0 ? e.y : -e.y, (k & 4) != 0 ? e.z : -e.z);
        #endregion

        #region Holes
        private static readonly List<MeshFilter> s_Cut = new List<MeshFilter>(2);

        // A place in the wall to batter (or knock) a hole through, its middle `centre` along it.
        private WallBreach Make(float centre)
        {
            var go = new GameObject("Breach");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Local(centre, 0f, 0f);
            var hole = go.AddComponent<WallBreach>();
            hole.Init(this, centre - Width * 0.5f, centre + Width * 0.5f, Top);
            _holes.Add(hole);
            return hole;
        }

        /// <summary>The box (in the wall's object) a hole is cut out of the wall's meshes in.</summary>
        protected void HoleBox(WallBreach hole, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(hole.A, -0.05f, -CutDepth);
            max = new Vector3(hole.B, hole.Top, CutDepth);
        }

        /// <summary>From a mesh's own space (its object's) to the box a hole is cut in: x along the wall, y up, z across.</summary>
        protected Matrix4x4 ToBox(Transform meshObject) => (_alongX ? Matrix4x4.identity : s_Swap) * transform.worldToLocalMatrix * meshObject.localToWorldMatrix;

        /// <summary>A hole broken through (or put back as it was, <paramref name="restoring"/>, as the building's raised): cut
        /// out of the wall's meshes and colliders, and whatever hung across it down with it.</summary>
        internal void Opened(WallBreach hole, bool restoring)
        {
            HoleBox(hole, out Vector3 min, out Vector3 max);
            CutMeshes(s_Cut);
            for (int i = 0; i < s_Cut.Count; i++)
            {
                MeshFilter mf = s_Cut[i];
                if (mf != null && mf.sharedMesh != null) MeshCut.RemoveBox(mf.sharedMesh, ToBox(mf.transform), min, max);
            }
            s_Cut.Clear();
            Fell(hole);
            Shape();
            Changed(hole, restoring);
        }

        /// <summary>A hole opened, boarded over or its boards gone: whatever hides the wall gathers its parts again, the holes
        /// are kept, and (but while the building's raised) the dead think again and the NavMesh takes it in.</summary>
        internal void Changed(WallBreach hole, bool restoring)
        {
            PartsChanged(hole);
            _blockedAt = float.NegativeInfinity;
            Keep();
            if (restoring) return;
            RaidDoor.Changes++;
            KentuckyStreamer map = KentuckyStreamer.Active;
            if (map != null) map.InvalidateNav(hole.Bounds);
            // The dead making for a door think again now, and again once the NavMesh has taken the hole in.
            if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(ThinkAgain());
        }

        private static System.Collections.IEnumerator ThinkAgain()
        {
            yield return new WaitForSeconds(1.5f);
            RaidDoor.Changes++;
        }

        /// <summary>The stretches of the wall left standing between its holes (open or boarded; the boards have their own
        /// collider), as (from, to) along it, in order; and the holes that stop short of the top, for what stands over them.</summary>
        protected void Stretches(List<Vector2> into)
        {
            into.Clear();
            _holes.Sort(s_ByStart);
            float cur = _lo;
            for (int i = 0; i < _holes.Count; i++)
            {
                WallBreach h = _holes[i];
                if (h == null || h.State == WallBreach.Intact) continue;
                if (h.A > cur + 0.01f) into.Add(new Vector2(cur, h.A));
                cur = Mathf.Max(cur, h.B);
            }
            if (_hi > cur + 0.01f) into.Add(new Vector2(cur, _hi));
        }

        // The holes are the raid's: a new map starts with none.
        private static void Mind()
        {
            object map = KentuckyStreamer.Active;
            if (ReferenceEquals(s_KeptFor, map)) return;
            s_Kept.Clear();
            s_KeptFor = map;
        }

        private void Keep()
        {
            Mind();
            List<Kept> list = null;
            for (int i = 0; i < _holes.Count; i++)
            {
                WallBreach h = _holes[i];
                if (h != null && h.State != WallBreach.Intact) (list ??= new List<Kept>(1)).Add(h.Keep());
            }
            if (list != null) s_Kept[_key] = list;
            else s_Kept.Remove(_key);
        }

        // The holes made in it while its building last stood, made again (Keep replaces the list as each goes back in).
        private void Restore()
        {
            Mind();
            if (!s_Kept.TryGetValue(_key, out List<Kept> list)) return;
            for (int i = 0; i < list.Count; i++) Make(list[i].Centre).Restore(list[i]);
        }
        #endregion

        #region The build tool
        public string DemolishName => "wall section";

        public bool CanDemolish(out string reason)
        {
            reason = _aimWhy;
            return !float.IsNaN(_aim);
        }

        // Plaster and studs: nothing worth carrying off (as the starter house's own walls give nothing back).
        public void GetSalvage(out int planks, out int nails)
        {
            planks = 0;
            nails = 0;
        }

        public bool CarvesNavMesh => false;

        /// <summary>Knocks a hole through where the build tool was last aimed.</summary>
        public void Dismantle()
        {
            if (float.IsNaN(_aim)) return;
            WallBreach hole = _aimHole != null ? _aimHole : Make(_aim);
            _aim = float.NaN;
            _aimHole = null;
            hole.BreakThrough(false);
        }

        public void AimAt(Vector3 point)
        {
            _aim = float.NaN;
            _aimHole = null;
            if (!SpotFor(AlongOf(transform.InverseTransformPoint(point)), 0.6f, false, out float spot, out WallBreach hole, out _aimWhy)) return;
            if (hole != null && hole.State != WallBreach.Intact)
            {
                _aimWhy = hole.IsOpen ? "There's a hole through here already" : "There are boards nailed across here: take those down";
                return;
            }
            _aim = spot;
            _aimHole = hole;
            _aimWhy = null;
        }

        public bool AimBox(out Vector3 centre, out Quaternion rotation, out Vector3 size)
        {
            float h = Mathf.Min(Top, _height);
            rotation = transform.rotation;
            centre = World(float.IsNaN(_aim) ? 0f : _aim, h * 0.5f, 0f);
            size = Size(Width, h, HalfThick * 2f + 0.06f);
            return !float.IsNaN(_aim);
        }

        /// <summary>The wall standing at <paramref name="point"/> (on one of its faces or in it), for the build tool's ray when
        /// it meets a collider that isn't one of the wall's own objects (an outside wall's are the site's): null when none.</summary>
        internal static BreakableWall At(Vector3 point)
        {
            for (int i = 0; i < All.Count; i++)
            {
                BreakableWall w = All[i];
                if (w == null || !w._near.Contains(point)) continue;
                Vector3 l = w.transform.InverseTransformPoint(point);
                float along = w.AlongOf(l);
                if (along >= w._lo && along <= w._hi && l.y >= -0.1f && l.y <= w._height && Mathf.Abs(w.AcrossOf(l)) <= w.CutDepth + 0.05f)
                    return w;
            }
            return null;
        }

        /// <summary>The wall a ray meeting <paramref name="c"/> at <paramref name="point"/> struck when <paramref name="c"/> is one
        /// of the site's own boxes (RaidTown's PropColliders, an outside wall's colliders): null for anything else in the wall
        /// (a door, a window), which has its own say.</summary>
        internal static BreakableWall AtSiteBox(Collider c, Vector3 point)
        {
            if (!(c is BoxCollider) || c.attachedRigidbody != null) return null;
            BreakableWall w = At(point);
            if (w == null) return null;
            Transform t = c.transform;
            for (int k = 0; k < 3 && t != null; k++, t = t.parent)
                if (t.name == "PropColliders") return w;
            return null;
        }
        #endregion

        #region For the harness
        /// <summary>The triangles of the wall's meshes still inside <paramref name="hole"/> (none once it's broken through).</summary>
        internal int TrianglesIn(WallBreach hole)
        {
            CutMeshes(s_Cut);
            var tris = new List<int>();
            int n = 0;
            float top = Mathf.Min(hole.Top, _height) - 0.02f;
            HoleBox(hole, out Vector3 min, out Vector3 max);
            foreach (MeshFilter mf in s_Cut)
            {
                Mesh mesh = mf != null ? mf.sharedMesh : null;
                if (mesh == null || !mesh.isReadable) continue;
                Matrix4x4 m = ToBox(mf.transform);
                mesh.GetVertices(s_Vertices);
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    mesh.GetTriangles(tris, s);
                    for (int t = 0; t + 2 < tris.Count; t += 3)
                    {
                        Vector3 c = m.MultiplyPoint3x4((s_Vertices[tris[t]] + s_Vertices[tris[t + 1]] + s_Vertices[tris[t + 2]]) / 3f);
                        if (c.x > min.x + 0.02f && c.x < max.x - 0.02f && c.y > 0.02f && c.y < top && c.z > min.z && c.z < max.z) n++;
                    }
                }
            }
            s_Cut.Clear();
            s_Vertices.Clear();
            return n;
        }

        /// <summary>The colliders standing across the middle of <paramref name="hole"/> in the wall's thickness (the wall's own, and
        /// its boards).</summary>
        internal int CollidersAcross(WallBreach hole)
        {
            Vector3 centre = World(hole.Centre, 1f, 0f), half = Size(Width * 0.5f - 0.1f, 0.8f, 0.05f);
            int n = Physics.OverlapBoxNonAlloc(centre, half, s_Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore), mine = 0;
            for (int i = 0; i < n; i++)
            {
                Collider col = s_Hits[i];
                s_Hits[i] = null;
                if (col == null || !col.enabled || col is CharacterController || col.GetComponentInParent<Actor>() != null) continue;
                mine++;
            }
            return mine;
        }

        /// <summary>A point in the wall's object (along, up, across), in the world.</summary>
        internal Vector3 WorldAt(float along, float y, float across) => World(along, y, across);
        #endregion

        // A blast near a wall (a breaching charge, a grenade) batters it where it's nearest, the harder the nearer: a charge
        // set against one blows a hole through.
        private sealed class Blast : IExplosionTarget
        {
            public void OnExplosion(Vector3 center, float radius, float damage)
            {
                float reach = radius + 1.2f;
                for (int i = All.Count - 1; i >= 0; i--)
                {
                    BreakableWall w = All[i];
                    if (w == null) continue;
                    Vector3 l = w.transform.InverseTransformPoint(center);
                    float along = w.AlongOf(l), u = Mathf.Clamp(along, w._lo, w._hi), y = Mathf.Clamp(l.y, 0f, w._height);
                    float d = new Vector3(along - u, l.y - y, w.AcrossOf(l)).magnitude;
                    if (d > reach || !w.SpotFor(u, 1.2f, false, out float spot, out WallBreach hole, out _)) continue;
                    if (hole != null && hole.IsOpen) continue;
                    if (hole == null) hole = w.Make(spot);
                    float falloff = 1f - Mathf.Clamp01(d / reach);
                    Vector3 at = hole.transform.position;
                    hole.TakeDamage(new DamageInfo(damage * falloff, DamageKind.Explosion, at, (at - center).normalized, null));
                }
            }
        }
    }
}
