using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// Where a site's carriageways meet: every point inside the site's square where two or more street pieces have a
    /// vertex (to 5 cm, on the same layer, tunnels aside; footways, paths, steps, cycleways, bridleways and pedestrian
    /// streets aren't carriageways and never make a junction), with an arm for each way a street leaves it: one where a
    /// piece ends there, two where it runs through. Two arms can be one road simply carrying on from one mapped piece to
    /// the next. Read through <see cref="Site.Junctions"/>, which finds them once per site; <see cref="RoadPass"/> shapes
    /// each one into a paved patch (<see cref="RoadPass.ArmSetback"/>, <see cref="RoadPass.StopLine"/>). Junctions come
    /// sorted by position and layer and their arms by street id, so a site always lists them alike.
    /// </summary>
    internal static class MapJunctions
    {
        private const float Grid = 0.05f;   // vertices that round to the same point of this grid are one point
        private const float MinHalfWidth = 1.5f;

        /// <summary>True for the kinds of street people walk (no junctions, no carriageway).</summary>
        internal static bool IsPath(string kind) => kind == "footway" || kind == "path" || kind == "steps"
            || kind == "cycleway" || kind == "bridleway" || kind == "pedestrian" || kind == "corridor";

        private struct Touch
        {
            public int Piece, Vertex;
        }

        private struct SortedArm
        {
            public JunctionArm Arm;
            public int Piece;
            public float Angle;
        }

        private static readonly System.Comparison<SortedArm> s_ByStreet = (a, b) =>
        {
            int c = a.Arm.Street.Data.id.CompareTo(b.Arm.Street.Data.id);
            if (c == 0) c = a.Arm.Vertex.CompareTo(b.Arm.Vertex);
            if (c == 0) c = a.Angle.CompareTo(b.Angle);
            return c != 0 ? c : a.Piece.CompareTo(b.Piece);
        };

        internal static List<Junction> Find(Site site)
        {
            List<Street> streets = site.Area.Streets;
            var points = new Dictionary<(int x, int z, int layer), List<Touch>>();
            for (int p = 0; p < streets.Count; p++)
            {
                Street s = streets[p];
                if (s.Data.tunnel || IsPath(s.Data.kind)) continue;
                for (int v = 0; v < s.Pts.Count; v++)
                {
                    var key = (Snap(s.Pts[v].x), Snap(s.Pts[v].y), s.Data.layer);
                    if (!points.TryGetValue(key, out List<Touch> touches)) points[key] = touches = new List<Touch>(2);
                    touches.Add(new Touch { Piece = p, Vertex = v });
                }
            }

            var found = new List<((int x, int z, int layer) key, Junction junction)>();
            var arms = new List<SortedArm>(8);
            foreach (KeyValuePair<(int x, int z, int layer), List<Touch>> point in points)
            {
                List<Touch> touches = point.Value;
                if (!SeveralPieces(touches)) continue;
                Vector2 at = streets[touches[0].Piece].Pts[touches[0].Vertex];
                if (Mathf.Abs(at.x) > site.Half || Mathf.Abs(at.y) > site.Half) continue;
                arms.Clear();
                foreach (Touch t in touches)
                {
                    AddArm(arms, streets, t, -1, point.Key);
                    AddArm(arms, streets, t, 1, point.Key);
                }
                if (arms.Count < 2) continue;
                arms.Sort(s_ByStreet);
                var junction = new Junction { At = at, Layer = point.Key.layer };
                foreach (SortedArm a in arms) junction.Arms.Add(a.Arm);
                found.Add((point.Key, junction));
            }
            found.Sort((a, b) => a.key.x != b.key.x ? a.key.x.CompareTo(b.key.x)
                : a.key.z != b.key.z ? a.key.z.CompareTo(b.key.z) : a.key.layer.CompareTo(b.key.layer));
            var junctions = new List<Junction>(found.Count);
            foreach (((int x, int z, int layer) key, Junction junction) f in found) junctions.Add(f.junction);
            return junctions;
        }

        private static int Snap(float v) => Mathf.RoundToInt(v / Grid);

        private static bool SeveralPieces(List<Touch> touches)
        {
            for (int i = 1; i < touches.Count; i++)
                if (touches[i].Piece != touches[0].Piece) return true;
            return false;
        }

        // The arm leaving the junction from a touch towards the piece's previous (step -1) or next (step 1) vertex. None
        // where the piece ends, or where that vertex is the junction's point too (the touch there makes the arm).
        private static void AddArm(List<SortedArm> arms, List<Street> streets, Touch t, int step, (int x, int z, int layer) point)
        {
            Street s = streets[t.Piece];
            int n = t.Vertex + step;
            if (n < 0 || n >= s.Pts.Count) return;
            Vector2 to = s.Pts[n];
            if (Snap(to.x) == point.x && Snap(to.y) == point.z) return;
            Vector2 dir = (to - s.Pts[t.Vertex]).normalized;
            arms.Add(new SortedArm
            {
                Arm = new JunctionArm { Street = s, Vertex = t.Vertex, Dir = dir, HalfWidth = Mathf.Max(MinHalfWidth, s.Data.width * 0.5f) },
                Piece = t.Piece,
                Angle = Mathf.Atan2(dir.y, dir.x),
            });
        }
    }
}
