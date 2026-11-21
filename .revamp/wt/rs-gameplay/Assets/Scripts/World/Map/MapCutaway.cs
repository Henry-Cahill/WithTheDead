using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    /// <summary>
    /// The Kentucky map's isometric cutaway, Project Zomboid-style: roofs and upper storeys show from outside, and the
    /// building the survivor is in, and whatever stands between the camera and them, is lifted away.
    /// <para>
    /// Building: on a Full site each building inside the square draws into a shell of its own (<see cref="ShellFor"/>,
    /// one mesh for its ground-storey walls and one for everything above), which <see cref="CutawayPass"/> emits as two
    /// objects: the walls under the site's world root (a NavMesh source, like every wall), the upper part under its
    /// overhead root (never baked), and leaves the site's list of them on a <see cref="MapCutawaySite"/>, which registers
    /// it here while the site is active (a streamed piece: from the moment it's switched on until it's let go of).
    /// Lite sites, scenery past the edge and the tiniest sheds draw into the shared batches and are never cut.
    /// </para>
    /// <para>
    /// Running (<see cref="MapCutawayDriver"/>, one per scene, after the camera): in the isometric view only, a few times
    /// a second (at once when the camera turns or the survivor jumps), the local survivor's sight line toward the camera
    /// is followed in plan through the buildings near it, found through a spatial hash: the building they stand in loses
    /// its upper part and its walls turned to the camera; a building whose upper part rises above the sight line (by its
    /// real heights, read from its mesh the first time: MapCutaway.Heights.cs) within a body's width of it, or a wider
    /// berth in front of the survivor, loses its upper part (standing under a porch roof or an awning that hides them
    /// lifts it too); one whose walls rise above it loses those walls' facings and its upper part (a roof never floats
    /// over cut walls). A part comes back <see cref="RestoreDelay"/> seconds after it was last needed out of the way, a
    /// building already cut is tested with a little slack (no flicker at the edges), and everything comes back at once in
    /// any other view. Cut parts switch to <see cref="ShadowCastingMode.ShadowsOnly"/> (as <see cref="CeilingVisibility"/>
    /// and <see cref="IsoCutaway"/> do: unseen, but still keeping the sun and the moon out of the rooms); cut walls are
    /// shown by a stand-in that keeps the other facings and a capped stub of the cut ones (MapCutaway.Overlay.cs).
    /// Nothing here has colliders, so <see cref="IsoCutaway"/>'s rays never meet these renderers (it keeps cutting the
    /// interiors' partitions and the props in the way). Allocation-free per frame.
    /// </para>
    /// </summary>
    internal static partial class MapCutaway
    {
        #region Tuning
        /// <summary>Seconds between scans while nothing changes.</summary>
        internal static float ScanInterval = 0.1f;
        /// <summary>Seconds a part stays cut after it was last in the way (walking out of a door, past a corner).</summary>
        internal static float RestoreDelay = 0.4f;
        /// <summary>Half the width of the survivor's own sight line (metres): whatever it passes through, walls or roofs,
        /// is cut.</summary>
        internal static float BodyHalfWidth = 0.5f;
        /// <summary>Half the width of the wider berth in front of the survivor, where only roofs are cut, so the dead
        /// coming at them from the side don't walk out from under one.</summary>
        internal static float ViewHalfWidth = 1.1f;
        /// <summary>The berth starts this far in front of the survivor (toward the camera), so a roof beside them stays.</summary>
        internal static float ViewStart = 0.8f;
        /// <summary>The sight line starts this far behind the survivor's middle: less than their radius, so a wall at their
        /// back is never in it.</summary>
        internal static float Behind = 0.25f;
        /// <summary>Inside a building, its own walls are only cut by the sight line this far in front of the survivor (a
        /// wing of an L), never the wall they lean on.</summary>
        internal static float InFront = 0.3f;
        /// <summary>Extra reach for a building already cut, so it doesn't flicker as the survivor walks past.</summary>
        internal static float Slack = 0.45f;
        /// <summary>Extra width of the sight line for a building already cut.</summary>
        internal static float WidthSlack = 0.15f;
        /// <summary>A survivor this close outside the building they were in is still in it (standing in the doorway).</summary>
        internal static float DoorwaySlack = 0.45f;
        /// <summary>A survivor who moves this far between scans has jumped (a respawn, a long drive in a frame): what was
        /// cut for the old spot comes back at once.</summary>
        internal static float JumpDistance = 6f;
        #endregion

        private const float Never = -1e6f;
        private const int WallBits = 15, UpperBit = 16, AllBits = 31;
        private const float CellSize = 16f;

        private static readonly Dictionary<long, List<Building>> s_Cells = new Dictionary<long, List<Building>>(512);
        private static readonly Stack<List<Building>> s_FreeCells = new Stack<List<Building>>(64);
        private static readonly List<MapCutawaySite> s_Sites = new List<MapCutawaySite>(64);
        private static readonly List<Building> s_Active = new List<Building>(32);
        private static int s_Count, s_Stamp;
        private static float s_MaxTop;
        private static MapCutawayDriver s_Driver;

        // The last scan: whether it was in the isometric view, and where from.
        private static bool s_Iso;
        private static float s_Yaw, s_NextScan, s_NextSweep;
        private static Vector3 s_Feet;

        /// <summary>Buildings registered (with a shell of their own, on an active site).</summary>
        internal static int Registered => s_Count;

        /// <summary>Sites registered, and spatial hash cells in use (for the harness's leak checks).</summary>
        internal static int SiteCount => s_Sites.Count;
        internal static int CellCount => s_Cells.Count;

        /// <summary>The highest point of anything registered, in the world (bounds the scan's reach).</summary>
        internal static float MaxTop => s_MaxTop;

        /// <summary>Buildings with anything cut away right now.</summary>
        internal static int CutBuildings => s_Active.Count;

        /// <summary>The buildings cut away right now (read only; for the harness and debugging).</summary>
        internal static IReadOnlyList<Building> Cut => s_Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Cells.Clear();
            s_Sites.Clear();
            s_Active.Clear();
            s_Rehash.Clear();
            s_Count = 0;
            s_MaxTop = 0f;
            s_Driver = null;
            s_Iso = false;
            s_NextScan = s_NextSweep = 0f;
        }

        #region Sites coming and going
        /// <summary>Adds a site's buildings (its <see cref="MapCutawaySite"/> calls this when it's switched on).</summary>
        internal static void Register(MapCutawaySite site)
        {
            if (site == null || site.Buildings == null || IndexOf(site) >= 0) return;
            s_Sites.Add(site);
            Vector3 offset = site.transform.position;
            List<Building> list = site.Buildings;
            site.MaxTop = offset.y;
            for (int i = 0; i < list.Count; i++)
            {
                Building b = list[i];
                if (b.Site != null) continue;
                b.Site = site;
                b.Base = offset.y;
                b.Pts = Moved(b.Local, offset);
                b.Bounds = BoundsOf(b.Pts);
                b.Cover = new Rect(b.Bounds.xMin - CoverMargin, b.Bounds.yMin - CoverMargin,
                    b.Bounds.width + 2f * CoverMargin, b.Bounds.height + 2f * CoverMargin);
                Hash(b);
                site.MaxTop = Mathf.Max(site.MaxTop, b.Base + b.Top);
                s_Count++;
            }
            s_MaxTop = Mathf.Max(s_MaxTop, site.MaxTop);
            if (Application.isPlaying && s_Driver == null) s_Driver = new GameObject("MapCutaway").AddComponent<MapCutawayDriver>();
        }

        /// <summary>Takes a site's buildings out, putting back whatever of them was cut (its <see cref="MapCutawaySite"/>
        /// calls this when it's switched off or destroyed: a streamed piece let go of, the map torn down).</summary>
        internal static void Unregister(MapCutawaySite site)
        {
            int at = IndexOf(site);
            if (at < 0) return;
            s_Sites.RemoveAt(at);
            Forget(site);
            MaxTopAgain();
        }

        // By reference: Unity's equality would take any two destroyed sites for one another.
        private static int IndexOf(MapCutawaySite site)
        {
            for (int i = 0; i < s_Sites.Count; i++)
                if (ReferenceEquals(s_Sites[i], site)) return i;
            return -1;
        }

        private static void MaxTopAgain()
        {
            s_MaxTop = 0f;
            for (int i = 0; i < s_Sites.Count; i++)
                if (!ReferenceEquals(s_Sites[i], null)) s_MaxTop = Mathf.Max(s_MaxTop, s_Sites[i].MaxTop);
        }

        // Unregisters a site's buildings (the site may already be destroyed: only its list is read).
        private static void Forget(MapCutawaySite site)
        {
            List<Building> list = site.Buildings;
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                Building b = list[i];
                if (b.Site != site) continue;
                if (b.Applied != 0) Apply(b, 0);
                ReleaseOverlay(b);
                if (b.Active)
                {
                    s_Active.Remove(b);
                    b.Active = false;
                }
                Unhash(b);
                Reset(b, AllBits);
                b.Occupied = false;
                b.Site = null;
                // The heights are in the world's plan: read afresh should the site ever come back elsewhere.
                if (b.Height != null) HeightBytes -= b.Height.Length;
                b.Height = null;
                b.Resolved = false;
                s_Count--;
            }
        }

        /// <summary>Unregisters every site at once (the harness, between builds).</summary>
        internal static void Clear()
        {
            for (int i = s_Sites.Count - 1; i >= 0; i--) Forget(s_Sites[i]);
            s_Sites.Clear();
            s_Active.Clear();
            s_Cells.Clear();
            s_Rehash.Clear();
            s_Count = 0;
            s_MaxTop = 0f;
            s_Iso = false;
        }

        // A site destroyed without being switched off first can't happen in play mode, but its buildings must never
        // linger: every few seconds the sites are checked.
        private static void Sweep(float now)
        {
            if (now < s_NextSweep) return;
            s_NextSweep = now + 5f;
            for (int i = s_Sites.Count - 1; i >= 0; i--)
            {
                if (s_Sites[i] != null) continue;
                Debug.LogWarning("[MapCutaway] a site went without unregistering its buildings; dropping them.");
                MapCutawaySite gone = s_Sites[i];
                s_Sites.RemoveAt(i);
                Forget(gone);
                MaxTopAgain();
            }
        }
        #endregion

        #region Each frame
        /// <summary>The driver's frame: the local survivor and the player camera, in the isometric view only.</summary>
        internal static void Tick()
        {
            float now = Time.unscaledTime;
            Sweep(now);
            IsoCameraFollow cam = IsoCameraFollow.Instance;
            if (s_Count == 0 || cam == null || !cam.isActiveAndEnabled || cam.Target == null || ViewState.Current != CameraView.Isometric)
            {
                if (s_Iso || s_Active.Count > 0) RestoreAll();
                return;
            }
            float pitch = cam.IsoPitch;
            float reach = cam.IsoDistance * Mathf.Cos(Mathf.Clamp(pitch, 10f, 89f) * Mathf.Deg2Rad) + 1f;
            Run(cam.Target.position, cam.IsoYaw, pitch, reach, cam.WallCutaway, now, false);
        }

        /// <summary>
        /// The cutaway for a survivor whose feet are at <paramref name="feet"/>, seen by the isometric camera at
        /// <paramref name="yaw"/> (compass heading it looks along) and <paramref name="pitch"/> (degrees down), whose
        /// sight line toward the camera is followed <paramref name="reach"/> metres along the ground (to the camera: past
        /// it nothing is drawn). <paramref name="walls"/>: cut walls as well as roofs. Scans when it's time (at once when
        /// the camera has turned or the survivor jumped); with <paramref name="immediate"/> it scans now, reads every
        /// building it meets whatever the time, and lets go of whatever isn't in the way any more at once (the harness's
        /// pictures).
        /// </summary>
        internal static void Run(Vector3 feet, float yaw, float pitch, float reach, bool walls, float now, bool immediate)
        {
            bool turned = !s_Iso || Mathf.Abs(Mathf.DeltaAngle(yaw, s_Yaw)) > 1f;
            bool jumped = (feet - s_Feet).sqrMagnitude > JumpDistance * JumpDistance;
            if (immediate || jumped) ResetAll(AllBits);
            else if (turned) ResetAll(WallBits);   // the walls turned to the camera are others now: no lingering
            if (!immediate && !turned && !jumped && now < s_NextScan) return;
            s_NextScan = now + ScanInterval;
            s_Iso = true;
            s_Yaw = yaw;
            s_Feet = feet;
            Scan(feet, yaw, pitch, reach, walls, now, immediate);
            Settle(now);
        }

        /// <summary>Brings everything back at once (any view but the isometric one; the camera or the survivor gone).</summary>
        internal static void RestoreAll()
        {
            for (int i = s_Active.Count - 1; i >= 0; i--)
            {
                Building b = s_Active[i];
                if (b.Applied != 0) Apply(b, 0);
                Reset(b, AllBits);
                b.Occupied = false;
                b.Active = false;
            }
            s_Active.Clear();
            s_Iso = false;
        }

        private static void ResetAll(int parts)
        {
            for (int i = 0; i < s_Active.Count; i++) Reset(s_Active[i], parts);
        }

        private static void Reset(Building b, int parts)
        {
            for (int p = 0; p < 5; p++)
                if ((parts & (1 << p)) != 0) b.Seen[p] = Never;
        }

        // Finds what's in the way now and marks it seen.
        private static void Scan(Vector3 feet, float yawDeg, float pitchDeg, float camReach, bool walls, float now, bool unbudgeted)
        {
            float yaw = yawDeg * Mathf.Deg2Rad;
            var look = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));   // the camera's heading on the ground
            Vector2 back = -look;                                      // from the survivor toward the camera
            var side = new Vector2(back.y, -back.x);
            float tan = Mathf.Tan(Mathf.Clamp(pitchDeg, 10f, 89f) * Mathf.Deg2Rad);
            var at = new Vector2(feet.x, feet.z);
            float y0 = feet.y;
            int facing = 0;
            for (int g = 0; g < 4; g++)
                if (Vector2.Dot(Cardinal(g), look) < 0.2f) facing |= 1 << g;
            float reach = Mathf.Clamp((s_MaxTop - y0) / tan, 0.5f, Mathf.Max(2f, camReach)) + Slack;
            float width = Mathf.Max(BodyHalfWidth, ViewHalfWidth) + Slack;
            Vector2 a = at - back * (Behind + DoorwaySlack), c = at + back * reach;
            int x0 = Cell(Mathf.Min(a.x, c.x) - width), x1 = Cell(Mathf.Max(a.x, c.x) + width);
            int z0 = Cell(Mathf.Min(a.y, c.y) - width), z1 = Cell(Mathf.Max(a.y, c.y) + width);
            int stamp = ++s_Stamp;
            long deadline = Stopwatch.GetTimestamp() + (long)(ResolveBudgetMs * Stopwatch.Frequency / 1000.0);
            bool read = false;
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!s_Cells.TryGetValue(Key(x, z), out List<Building> cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Building b = cell[i];
                        if (b.Stamp == stamp) continue;
                        b.Stamp = stamp;
                        if (!b.Resolved && (unbudgeted || !read || Stopwatch.GetTimestamp() < deadline) && Resolve(b)) read = true;
                        int want = Want(b, at, y0, back, side, tan, reach, walls ? facing : 0, walls);
                        if (want == 0) continue;
                        for (int p = 0; p < 5; p++)
                            if ((want & (1 << p)) != 0) b.Seen[p] = now;
                        if (b.Active) continue;
                        b.Active = true;
                        s_Active.Add(b);
                    }
                }
            if (s_Rehash.Count > 0) Rehash();
        }

        // What of building b is in the way of a survivor standing at s (plan) with their feet at y0.
        private static int Want(Building b, Vector2 s, float y0, Vector2 back, Vector2 side, float tan, float reach, int facing, bool walls)
        {
            int want = 0;
            bool cut = b.Applied != 0;
            float far = cut ? Slack : 0f, wide = cut ? WidthSlack : 0f;
            bool inside = b.Bounds.Contains(s) && Contains(b.Pts, s);
            if (!inside && b.Occupied) inside = Near(b.Bounds, s, DoorwaySlack) && DistanceToEdges(b.Pts, s) <= DoorwaySlack;
            b.Occupied = inside;
            if (inside) want |= UpperBit | (facing & b.Groups);
            else if (b.Upper != null && RoofHides(b, s, y0, back, side, tan, reach, far, wide)) want |= UpperBit;
            if (walls && b.Groups != 0)
            {
                // The building they're in keeps its far walls unless one of them (a wing of it) stands in front of them.
                float from = inside ? InFront : -Behind;
                float top = Mathf.Min((b.Base + b.WallTop - y0) / tan + far, reach);
                if (top > from)
                {
                    Vector2[] p = b.Pts;
                    float w = BodyHalfWidth + wide;
                    for (int i = 0; i < p.Length; i++)
                    {
                        int bit = 1 << b.EdgeGroup[i];
                        if ((want & bit) != 0 || (b.Groups & bit) == 0) continue;
                        if (Crosses(p[i], p[i + 1 < p.Length ? i + 1 : 0], s, back, side, from, top, w)) want |= bit;
                    }
                }
            }
            if ((want & WallBits) != 0) want |= UpperBit;   // a roof never floats over cut walls
            return want;
        }

        // Cuts what has been in the way within RestoreDelay, and brings back the rest.
        private static void Settle(float now)
        {
            for (int i = s_Active.Count - 1; i >= 0; i--)
            {
                Building b = s_Active[i];
                int mask = 0;
                if (b.Site != null)
                    for (int p = 0; p < 5; p++)
                        if (now - b.Seen[p] <= RestoreDelay) mask |= 1 << p;
                if ((mask & WallBits) != 0) mask |= UpperBit;
                if (mask != b.Applied) Apply(b, mask);
                if (mask != 0) continue;
                b.Active = false;
                b.Occupied = false;
                int last = s_Active.Count - 1;
                s_Active[i] = s_Active[last];
                s_Active.RemoveAt(last);
            }
        }

        // Shows and hides a building's parts to match mask (bits 0-3: the walls facing north, east, south and west; bit 4:
        // the upper part).
        private static void Apply(Building b, int mask)
        {
            int was = b.Applied;
            b.Applied = mask;
            if (((was ^ mask) & UpperBit) != 0 && b.Upper != null)
                b.Upper.shadowCastingMode = (mask & UpperBit) != 0 ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            int walls = mask & WallBits;
            if (((was ^ mask) & WallBits) == 0 || b.Walls == null) return;
            Doors(b, walls);
            if (walls == 0)
            {
                b.Walls.shadowCastingMode = ShadowCastingMode.On;
                if (b.Overlay != null) b.Overlay.enabled = false;
                return;
            }
            // The walls themselves only cast shadows now (they're baked into the NavMesh, so never changed); the
            // stand-in shows the rest, or, when it can't be made, every wall goes.
            b.Walls.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            ShowOverlay(b, walls);
        }
        #endregion

        #region Plane geometry (x, z)
        private static Vector2 Cardinal(int facing)
            => facing == 0 ? Vector2.up : facing == 1 ? Vector2.right : facing == 2 ? Vector2.down : Vector2.left;

        // True when the segment a-b passes through the box u in [u0, u1], |v| <= w, where u runs along back and v along
        // side from s (Liang-Barsky).
        private static bool Crosses(Vector2 a, Vector2 b, Vector2 s, Vector2 back, Vector2 side, float u0, float u1, float w)
        {
            Vector2 da = a - s, db = b - s;
            float au = Vector2.Dot(da, back), av = Vector2.Dot(da, side);
            float du = Vector2.Dot(db, back) - au, dv = Vector2.Dot(db, side) - av;
            float t0 = 0f, t1 = 1f;
            return Clip(-du, au - u0, ref t0, ref t1) && Clip(du, u1 - au, ref t0, ref t1)
                && Clip(-dv, av + w, ref t0, ref t1) && Clip(dv, w - av, ref t0, ref t1);
        }

        // True when the polygon overlaps the same box: an edge passes through it, or the box lies inside the polygon.
        private static bool Crosses(Vector2[] p, Vector2 s, Vector2 back, Vector2 side, float u0, float u1, float w)
        {
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if (Crosses(p[j], p[i], s, back, side, u0, u1, w)) return true;
            return Contains(p, s + back * u0);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (p > -1e-9f && p < 1e-9f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        private static bool Contains(Vector2[] p, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            return inside;
        }

        private static float DistanceToEdges(Vector2[] p, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                Vector2 a = p[j], ab = p[i] - a;
                float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, (a + ab * t - q).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }

        private static bool Near(Rect r, Vector2 q, float margin)
            => q.x >= r.xMin - margin && q.x <= r.xMax + margin && q.y >= r.yMin - margin && q.y <= r.yMax + margin;

        private static Vector2[] Moved(Vector2[] local, Vector3 offset)
        {
            var pts = new Vector2[local.Length];
            for (int i = 0; i < local.Length; i++) pts[i] = new Vector2(local[i].x + offset.x, local[i].y + offset.z);
            return pts;
        }

        private static Rect BoundsOf(Vector2[] p)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            for (int i = 0; i < p.Length; i++)
            {
                x0 = Mathf.Min(x0, p[i].x);
                z0 = Mathf.Min(z0, p[i].y);
                x1 = Mathf.Max(x1, p[i].x);
                z1 = Mathf.Max(z1, p[i].y);
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        private static int Cell(float v) => Mathf.FloorToInt(v / CellSize);

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;
        #endregion
    }
}
