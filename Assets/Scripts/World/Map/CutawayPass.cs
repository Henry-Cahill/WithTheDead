using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// Emits the per-building shells (<see cref="BuildingShell"/>) of a Full site as objects the isometric cutaway can
    /// hide one building at a time (<see cref="MapCutaway"/>): for each building its ground-storey walls under the site's
    /// world root (a NavMesh source like every wall) and everything above them under its overhead root (never baked),
    /// both standing on the footprint's middle; then the site's list of them, for the runtime, on a
    /// <see cref="MapCutawaySite"/> on the walls' parent. Runs after the buildings and their insides; the shells' meshes
    /// go back to <see cref="MapCutaway"/>'s pool, and the shells point at the shared batches from then on. Shared
    /// (batched) shells are emitted with the site's batches instead.
    /// </summary>
    internal static class CutawayPass
    {
        private const int BuildingsPerStep = 8;
        /// <summary>A roof reaching this far past the walls in plan (a porch roof, a canopy) gets an outline of its own, so
        /// standing under it, or behind it, lifts it.</summary>
        private const float Overhang = 0.45f;
        private const float DefaultWallThickness = 0.22f;

        private static readonly List<Vector3> s_Verts = new List<Vector3>(4096);

        internal static IEnumerable<string> Build(Site site)
        {
            List<BuildingShell> shells = site.Shells;
            int own = 0;
            for (int i = 0; i < shells.Count; i++)
                if (!shells[i].Batched) own++;
            if (own == 0) yield break;
            var thickness = new Dictionary<Footprint, float>();
            foreach (EnterableBuilding e in site.Enterables)
                if (e.Footprint != null && e.WallThickness > 0.01f) thickness[e.Footprint] = e.WallThickness;
            DecorPalette.Matte(MapCutaway.CapColour);   // the cut walls' cap colour joins the palette with the build's
            Transform walls = Container("Building shells", site.Town.World), upper = Container("Building shells", site.Town.Overhead);
            var buildings = new List<MapCutaway.Building>(own);
            int done = 0;
            for (int i = 0; i < shells.Count; i++)
            {
                BuildingShell shell = shells[i];
                if (shell.Batched) continue;
                MapCutaway.Building b = Emit(shell, walls, upper, thickness);
                MapCutaway.Retire(site, shell);
                if (b != null) buildings.Add(b);
                if (++done % BuildingsPerStep == 0) yield return "cutaway";
            }
            MapCutawaySite.Attach(walls.gameObject, upper, buildings);
            yield return "cutaway";
        }

        private static Transform Container(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static MapCutaway.Building Emit(BuildingShell shell, Transform wallsRoot, Transform upperRoot, Dictionary<Footprint, float> thickness)
        {
            Footprint f = shell.Footprint;
            if (f == null || f.Pts.Count < 3) return null;
            Vector3 origin = To3(f.Middle, 0f);
            string id = f.Data != null ? f.Data.id.ToString(CultureInfo.InvariantCulture) : "";
            RaidMesh w = shell.Walls[0], u = shell.Upper;
            GameObject wallsGo = w != null ? w.Emit("Walls " + id, wallsRoot, origin, ShadowCastingMode.On) : null;
            GameObject upperGo = u != null && u != w ? u.Emit("Upper " + id, upperRoot, origin, ShadowCastingMode.On) : null;
            if (wallsGo == null && upperGo == null) return null;

            var b = new MapCutaway.Building
            {
                Id = f.Data != null ? f.Data.id : 0,
                Kind = f.Data != null ? f.Data.kind : null,
                Middle = f.Middle,
                Local = f.Pts.ToArray(),
                WallThickness = thickness.TryGetValue(f, out float t) ? t : DefaultWallThickness,
                Walls = wallsGo != null ? wallsGo.GetComponent<MeshRenderer>() : null,
                Upper = upperGo != null ? upperGo.GetComponent<MeshRenderer>() : null,
            };
            int n = b.Local.Length;
            b.EdgeGroup = new byte[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = b.Local[(i + 1) % n] - b.Local[i];
                b.EdgeGroup[i] = (byte)BuildingShell.Facing(new Vector2(d.y, -d.x));   // the footprint runs anticlockwise
                if (wallsGo != null && d.sqrMagnitude > 0.0025f) b.Groups |= 1 << b.EdgeGroup[i];
            }
            Mesh wallsMesh = wallsGo != null ? wallsGo.GetComponent<MeshFilter>().sharedMesh : null;
            Mesh upperMesh = upperGo != null ? upperGo.GetComponent<MeshFilter>().sharedMesh : null;
            b.WallTop = wallsMesh != null ? Mathf.Max(shell.WallTop, wallsMesh.bounds.max.y) : 0f;
            b.Top = Mathf.Max(shell.Top, b.WallTop);
            if (upperMesh != null)
            {
                b.Top = Mathf.Max(b.Top, upperMesh.bounds.max.y);
                b.LocalRoof = RoofOutline(upperMesh, b);
            }
            return b;
        }

        // An octagon round everything above the walls (the bounds along x, z and both diagonals), when it reaches past the
        // footprint's own by more than Overhang anywhere; else null. Site-local metres.
        private static Vector2[] RoofOutline(Mesh upper, MapCutaway.Building b)
        {
            s_Verts.Clear();
            upper.GetVertices(s_Verts);
            if (s_Verts.Count == 0) return null;
            Octagon roof = Octagon.Empty, walls = Octagon.Empty;
            for (int i = 0; i < s_Verts.Count; i++) roof.Add(s_Verts[i].x, s_Verts[i].z);
            for (int i = 0; i < b.Local.Length; i++) walls.Add(b.Local[i].x - b.Middle.x, b.Local[i].y - b.Middle.y);
            if (!roof.Beyond(walls, Overhang)) return null;
            List<Vector2> outline = roof.Outline();
            if (outline.Count < 3) return null;
            var pts = new Vector2[outline.Count];
            for (int i = 0; i < pts.Length; i++) pts[i] = outline[i] + b.Middle;
            return pts;
        }

        // The least and greatest of x, z, x + z and x - z over a set of points: the octagon that holds them.
        private struct Octagon
        {
            private float _x0, _x1, _z0, _z1, _s0, _s1, _d0, _d1;

            public static Octagon Empty => new Octagon
            {
                _x0 = float.MaxValue, _z0 = float.MaxValue, _s0 = float.MaxValue, _d0 = float.MaxValue,
                _x1 = float.MinValue, _z1 = float.MinValue, _s1 = float.MinValue, _d1 = float.MinValue,
            };

            public void Add(float x, float z)
            {
                _x0 = Mathf.Min(_x0, x);
                _x1 = Mathf.Max(_x1, x);
                _z0 = Mathf.Min(_z0, z);
                _z1 = Mathf.Max(_z1, z);
                _s0 = Mathf.Min(_s0, x + z);
                _s1 = Mathf.Max(_s1, x + z);
                _d0 = Mathf.Min(_d0, x - z);
                _d1 = Mathf.Max(_d1, x - z);
            }

            /// <summary>True when this reaches past <paramref name="o"/> by more than <paramref name="by"/> metres anywhere.</summary>
            public bool Beyond(Octagon o, float by)
            {
                float diagonal = by * 1.41421356f;   // x + z grows by root two per metre along a diagonal
                return _x0 < o._x0 - by || _x1 > o._x1 + by || _z0 < o._z0 - by || _z1 > o._z1 + by
                    || _s0 < o._s0 - diagonal || _s1 > o._s1 + diagonal || _d0 < o._d0 - diagonal || _d1 > o._d1 + diagonal;
            }

            /// <summary>The octagon's corners, anticlockwise: the box cut by the four diagonal half-planes.</summary>
            public List<Vector2> Outline()
            {
                var poly = new List<Vector2>
                {
                    new Vector2(_x0, _z0), new Vector2(_x1, _z0), new Vector2(_x1, _z1), new Vector2(_x0, _z1),
                };
                poly = Cut(poly, 1f, 1f, _s0);     // x + z >= s0
                poly = Cut(poly, -1f, -1f, -_s1);  // x + z <= s1
                poly = Cut(poly, 1f, -1f, _d0);    // x - z >= d0
                poly = Cut(poly, -1f, 1f, -_d1);   // x - z <= d1
                return poly;
            }

            // Keeps the part of a convex polygon where a * x + b * z >= c.
            private static List<Vector2> Cut(List<Vector2> poly, float a, float b, float c)
            {
                var o = new List<Vector2>(poly.Count + 2);
                for (int i = 0; i < poly.Count; i++)
                {
                    Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                    float fp = a * p.x + b * p.y - c, fq = a * q.x + b * q.y - c;
                    if (fp >= 0f) o.Add(p);
                    if ((fp >= 0f) != (fq >= 0f)) o.Add(Vector2.Lerp(p, q, fp / (fp - fq)));
                }
                return o;
            }
        }
    }
}
