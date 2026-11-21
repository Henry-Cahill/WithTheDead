using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CloneZ
{
    // The cutaway's background work, a few tenths of a millisecond a frame: reading the heights of the roofs the scans meet
    // (MapCutaway.Heights.cs) and making the walls' stand-ins (MapCutaway.Overlay.cs) for the buildings round the survivor
    // before they're needed, in this order: a stand-in a cut is waiting on, the roofs the scans met unread, then the
    // nearest buildings within PrewarmRadius still wanting either. One job of each kind at a time, a slice a frame.
    internal static partial class MapCutaway
    {
        /// <summary>Time a frame may spend reading meshes and making stand-ins (ms), and while a cut is waiting on one.</summary>
        internal static float WorkBudgetMs = 0.5f, UrgentBudgetMs = 2f;
        /// <summary>Buildings this close to the survivor get their heights read and their stand-in made ahead of need.</summary>
        internal static float PrewarmRadius = 14f;

        private const int PrewarmSlots = 8;
        private static readonly List<Building> s_Urgent = new List<Building>(32);
        private static readonly List<Building> s_ToRead = new List<Building>(256);
        private static readonly Building[] s_Prewarm = new Building[PrewarmSlots];
        private static readonly float[] s_PrewarmD = new float[PrewarmSlots];
        private static int s_PrewarmCount;
        private static float s_NextEvict;
        // While the harness's immediate runs: whatever a cut needs is made at once.
        private static bool s_Immediate;

        /// <summary>The most a frame's work took so far (ms).</summary>
        internal static double WorkMaxMs;

        /// <summary>Cuts held back for a stand-in to be made, and the longest such hold (frames: calls of <see cref="Work"/>).</summary>
        internal static int Holds, LongestHold;
        private static int s_Frame;

        // The part of `mask` (what building b's cut wants now) that can be shown: all of it once the walls' stand-in it needs
        // is ready (or can't be made: the walls are then cut whole); until then what's cut already, while the stand-in is made
        // first thing (a frame or two: holding the cut back that long beats showing bits of wall floating over a lifted roof,
        // or a frame's spike to make it at once).
        private static int Ready(Building b, int mask)
        {
            b.Wanted = mask;
            if (b.Walls == null || !NeedsStandIn(b, mask) || OverlayReady(b) || b.OverlayFailed)
            {
                if (b.HoldFrame >= 0)
                {
                    LongestHold = Mathf.Max(LongestHold, s_Frame - b.HoldFrame);
                    b.HoldFrame = -1;
                }
                return mask;
            }
            if (s_Immediate)
            {
                MakeOverlayNow(b);
                if (OverlayReady(b) || b.OverlayFailed) return mask;
            }
            if (b.HoldFrame < 0)
            {
                b.HoldFrame = s_Frame;
                Holds++;
            }
            Urge(b);
            return b.Applied & mask;
        }

        // A cut is waiting on b's stand-in.
        private static void Urge(Building b)
        {
            if (b.Urgent || b.OverlayFailed || OverlayReady(b)) return;
            b.Urgent = true;
            s_Urgent.Add(b);
        }

        private static void WantRead(Building b)
        {
            if (b.ToRead || b.Resolved) return;
            b.ToRead = true;
            s_ToRead.Add(b);
        }

        /// <summary>Reads meshes and makes stand-ins for up to <paramref name="budgetMs"/> (more while a cut waits on one).
        /// The driver runs it every frame in the isometric view; the harness between its frames.</summary>
        internal static void Work(float budgetMs)
        {
            s_Frame++;
            long t0 = Stopwatch.GetTimestamp();
            float budget = s_Urgent.Count > 0 ? Mathf.Max(budgetMs, UrgentBudgetMs) : budgetMs;
            long deadline = t0 + (long)(budget * Stopwatch.Frequency / 1000.0);
            for (int guard = 0; guard < 64; guard++)
            {
                // A stand-in a cut waits on comes first: one being made ahead of need makes way for it.
                Building urgent = NextUrgent();
                if (urgent != null && !ReferenceEquals(s_Job.B, urgent))
                {
                    AbortOverlayJob();
                    if (!StartOverlayJob(urgent))
                    {
                        DropUrgent(urgent);
                        Release(urgent);
                        continue;
                    }
                }
                if (s_Job.B != null)
                {
                    Building b = s_Job.B;
                    if (!StepOverlayJob(deadline)) break;
                    DropUrgent(b);
                    Release(b);
                }
                else if (s_Height.B != null)
                {
                    if (!StepHeightJob(deadline)) break;
                }
                else if (!StartNext()) break;
                if (Stopwatch.GetTimestamp() >= deadline) break;
            }
            double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
            if (ms > WorkMaxMs) WorkMaxMs = ms;
        }

        // A cut held back for b's stand-in, shown now it's made (or can't be).
        private static void Release(Building b)
        {
            if (b.Site == null || b.HoldFrame < 0) return;
            int mask = Ready(b, b.Wanted);
            if (mask != b.Applied) Apply(b, mask);
        }

        private static Building NextUrgent()
        {
            while (s_Urgent.Count > 0)
            {
                Building b = s_Urgent[0];
                if (b.Site != null && !b.OverlayFailed && !OverlayReady(b) && b.HoldFrame >= 0) return b;
                DropUrgent(b);
                Release(b);
            }
            return null;
        }

        private static void DropUrgent(Building b)
        {
            if (!b.Urgent) return;
            b.Urgent = false;
            s_Urgent.Remove(b);
        }

        // Starts the next job: the roofs the scans met unread, then the nearest building round the survivor wanting its
        // heights or its stand-in. False when there's nothing to do.
        private static bool StartNext()
        {
            while (s_ToRead.Count > 0)
            {
                int last = s_ToRead.Count - 1;
                Building b = s_ToRead[last];
                s_ToRead.RemoveAt(last);
                b.ToRead = false;
                if (StartHeightJob(b)) return true;
            }
            for (int i = 0; i < s_PrewarmCount; i++)
            {
                Building b = s_Prewarm[i];
                if (b == null || b.Site == null) continue;
                if (!b.Resolved && StartHeightJob(b)) return true;
                if (WantsOverlay(b) && StartOverlayJob(b)) return true;
                s_Prewarm[i] = null;   // nothing to do for it (or not baked yet): skipped till the next scan
            }
            return false;
        }

        private static bool WantsOverlay(Building b) => b.Walls != null && b.Groups != 0 && !b.OverlayFailed && !OverlayReady(b);

        // The buildings within PrewarmRadius of the survivor still wanting their heights or their stand-in, nearest first.
        private static void Prewarm(Vector2 at)
        {
            s_PrewarmCount = 0;
            int stamp = ++s_Stamp;
            int x0 = Cell(at.x - PrewarmRadius), x1 = Cell(at.x + PrewarmRadius);
            int z0 = Cell(at.y - PrewarmRadius), z1 = Cell(at.y + PrewarmRadius);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!s_Cells.TryGetValue(Key(x, z), out List<Building> cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Building b = cell[i];
                        if (b.Stamp == stamp) continue;
                        b.Stamp = stamp;
                        if (b.Resolved && !WantsOverlay(b)) continue;
                        float d = RectDistance(b.Bounds, at);
                        if (d > PrewarmRadius) continue;
                        if (s_PrewarmCount == PrewarmSlots && d >= s_PrewarmD[PrewarmSlots - 1]) continue;   // farther than all kept
                        int k = s_PrewarmCount < PrewarmSlots ? s_PrewarmCount++ : PrewarmSlots - 1;   // when full, the last goes
                        while (k > 0 && s_PrewarmD[k - 1] > d)
                        {
                            s_Prewarm[k] = s_Prewarm[k - 1];
                            s_PrewarmD[k] = s_PrewarmD[k - 1];
                            k--;
                        }
                        s_Prewarm[k] = b;
                        s_PrewarmD[k] = d;
                    }
                }
            float now = Time.unscaledTime;
            if (now < s_NextEvict && !s_Immediate) return;
            s_NextEvict = now + 1f;
            Evict(at);
        }

        // Forgets a building that's going in every list of work.
        private static void Unqueue(Building b)
        {
            DropUrgent(b);
            b.HoldFrame = -1;
            b.Wanted = 0;
            if (b.ToRead)
            {
                b.ToRead = false;
                s_ToRead.Remove(b);
            }
            for (int i = 0; i < s_PrewarmCount; i++)
                if (ReferenceEquals(s_Prewarm[i], b)) s_Prewarm[i] = null;
            if (ReferenceEquals(s_Height.B, b)) AbortHeightJob();
            if (ReferenceEquals(s_Job.B, b)) AbortOverlayJob();
        }

        private static float RectDistance(Rect r, Vector2 q)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r.xMin - q.x, q.x - r.xMax));
            float dz = Mathf.Max(0f, Mathf.Max(r.yMin - q.y, q.y - r.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Lets go of every stand-in and every roof's heights read so far, as if the survivor came to them for the
        /// first time (the harness, to time a first visit). Nothing may be cut.</summary>
        internal static void DropCaches()
        {
            RestoreAll();
            AbortHeightJob();
            AbortOverlayJob();
            for (int i = s_Overlays.Count - 1; i >= 0; i--) ReleaseOverlay(s_Overlays[i]);
            for (int i = 0; i < s_Sites.Count; i++)
            {
                List<Building> list = s_Sites[i].Buildings;
                for (int k = 0; k < list.Count; k++)
                {
                    Building b = list[k];
                    if (b.Site == null) continue;
                    Unqueue(b);
                    if (b.Height != null) HeightBytes -= b.Height.Length;
                    b.Height = null;
                    if (b.Upper != null && b.UpperMesh != null && b.UpperMesh.isReadable && b.UpperMesh.vertexCount > 0) b.Resolved = false;
                }
            }
        }
    }
}
