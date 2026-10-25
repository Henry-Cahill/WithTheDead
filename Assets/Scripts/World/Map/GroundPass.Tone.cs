using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The snow's tone: one see-through sheet over the ground (a grid of 2.56 m cells on a chunk, 5.12 m on a Lite one,
    // about 2.9 m over a fixed map and its scenery ring), each corner looking up how drifted, scoured, grimy or shaded the
    // snow is there in a small table (ToneMaterial). Drifts and scoured crust come from noise fixed to the world and drawn
    // out along the wind (from the north-west), so neighbouring chunks agree; grime from the land use (industry, lots,
    // farmyards, town centres); shade, calm and leaf litter under the mapped woods. Plazas and forecourts get trodden snow
    // in broad patches; water and parking lots show through untouched, the lots' stall rows getting their packed snow as
    // extra geometry in the same mesh (GroundPass.Parking).
    internal static partial class GroundPass
    {
        private static readonly Vector2 Downwind = new Vector2(0.7071f, -0.7071f);   // the wind blows to the south-east

        // The table: 64 columns by three bands of 16 rows. Across the first two, the snow's tone: across, scoured crust
        // (0) through untouched (0.5) to fresh drift (1); down the middle band, clean to grimy; up the first, clean to
        // shaded under trees, with leaf litter showing through (so a point between woods and grime passes through
        // clean snow). In the third, packed snow lying on paving: across, how much of it (0 none).
        private const int ToneW = 64, ToneBand = 16, ToneH = ToneBand * 3;

        // A point's tone: x how drifted, y how grimy (or, below 0, how shaded under trees).
        private static Vector2 ToneUv(Vector2 t)
            => new Vector2(t.x, t.y >= 0f ? (ToneBand + 0.5f + Mathf.Clamp01(t.y) * (ToneBand - 1)) / ToneH
                : (ToneBand - 0.5f - Mathf.Clamp01(-t.y) * (ToneBand - 1)) / ToneH);

        private static Vector2 PackedUv(float amount) => new Vector2(Mathf.Clamp01(amount), ToneBand * 2.5f / ToneH);

        // The tone's working buffers, kept between sites (main thread only), so a streamed piece allocates no more than its
        // mesh.
        private static Owner[] s_ToneOwners = new Owner[0];
        private static Vector2[] s_ToneAt = new Vector2[0];
        private static int[] s_ToneIndex = new int[0];
        private static readonly List<Vector3> s_ToneVerts = new List<Vector3>(4096), s_ToneNormals = new List<Vector3>(4096);
        private static readonly List<Vector2> s_ToneUvs = new List<Vector2>(4096);
        private static readonly List<int> s_ToneTris = new List<int>(16384);

        /// <summary>
        /// Generate the snow‑tone mesh for a given ground patch.
        /// </summary>
        private static void Tone(Ground g)
        {
            // Determine whether we are generating tone for a streamed chunk (full detail) or a static site
            bool chunk = g.Site.Chunked;
            // Use the larger half‑extent for static sites so the tone extends a bit beyond visible ground
            float half = chunk ? g.Half : g.Half + SceneryReach;
            // 2.56 m cells on a chunk (5.12 m on a Lite one: far off, the tone's broad anyway), about 2.9 m on a fixed map.
            // Choose cell resolution: finer on streamed chunks (2.56 m) and coarser on static sites
            int cells = chunk ? (g.Lite ? 25 : 50) : Mathf.CeilToInt(half * 2f / 2.9f);
            // Compute cell size based on total extent and number of cells
            float cell = half * 2f / cells;
            // Grid dimensions: rows = cells+1 to include the far edge, total points = row*row
            int row = cells + 1, points = row * row;
            // Ensure working buffers are large enough for the grid points
            if (s_ToneOwners.Length < points)
            {
                // Allocate fresh arrays for owners, tone values and vertex indices
                s_ToneOwners = new Owner[points];
                // Allocate parallel arrays for tone UVs and vertex indices
                s_ToneAt = new Vector2[points];
                s_ToneIndex = new int[points];
            }
            Owner[] owners = s_ToneOwners;
            Vector2[] tone = s_ToneAt;
            int[] index = s_ToneIndex;
            System.Array.Clear(owners, 0, points);
            Raster(g.Owners, owners, half, cell, row);
            foreach (List<Owner> band in g.Ring) Raster(band, owners, half, cell, row);
            for (int j = 0; j <= cells; j++)
                for (int i = 0; i <= cells; i++)
                {
                    var p = new Vector2(-half + i * cell, -half + j * cell);
                    float fade = chunk ? 1f : Mathf.Clamp01((half - Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y))) / 20f);
                    tone[j * row + i] = ToneAt(g, p, owners[j * row + i], fade);
                }
            System.Array.Clear(owners, 0, points);   // hold on to no site's areas
            List<Vector3> verts = s_ToneVerts;
            List<Vector2> uvs = s_ToneUvs;
            List<int> tris = s_ToneTris;
            verts.Clear();
            uvs.Clear();
            tris.Clear();
            for (int k = 0; k < points; k++) index[k] = -1;
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                    if (Clean(tone[a]) && Clean(tone[b]) && Clean(tone[c]) && Clean(tone[d])) continue;
                    int va = Vertex(a), vb = Vertex(b), vc = Vertex(c), vd = Vertex(d);
                    tris.Add(va); tris.Add(vc); tris.Add(vd);   // clockwise seen from above: the face looks up
                    tris.Add(va); tris.Add(vd); tris.Add(vb);
                }
            int first = verts.Count;
            verts.AddRange(g.ToneVerts);
            uvs.AddRange(g.ToneUvs);
            foreach (int t in g.ToneTris) tris.Add(first + t);
            if (tris.Count == 0) return;

            var mesh = new Mesh { name = "SnowTone" };
            RaidMesh.CreatedMeshes?.Add(mesh);
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            List<Vector3> normals = s_ToneNormals;
            while (normals.Count < verts.Count) normals.Add(Vector3.up);
            mesh.SetNormals(normals, 0, verts.Count);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
            var go = new GameObject("SnowTone");
            go.transform.SetParent(g.Site.Town.Scenery, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ToneMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;

            int Vertex(int k)
            {
                if (index[k] >= 0) return index[k];
                index[k] = verts.Count;
                verts.Add(new Vector3(-half + (k % row) * cell, ToneY, -half + (k / row) * cell));
                uvs.Add(ToneUv(tone[k]));
                return index[k];
            }
        }

        private static bool Clean(Vector2 t) => Mathf.Abs(t.x - 0.5f) < 0.04f && Mathf.Abs(t.y) < 0.02f;

        // Which owner's ground shows at each point of the tone's grid, from the partition's trapezoids (they don't
        // overlap, so each point gets the one it's in).
        private static void Raster(List<Owner> owners, Owner[] into, float half, float cell, int row)
        {
            foreach (Owner o in owners)
                foreach (Trap t in o.Traps)
                {
                    int j0 = Mathf.Max(0, Mathf.CeilToInt((t.Y0 + half) / cell)), j1 = Mathf.Min(row - 1, Mathf.FloorToInt((t.Y1 + half) / cell));
                    float h = t.Y1 - t.Y0;
                    for (int j = j0; j <= j1; j++)
                    {
                        float y = -half + j * cell, f = h > 1e-6f ? (y - t.Y0) / h : 0f;
                        float l = t.L0 + (t.L1 - t.L0) * f, r = t.R0 + (t.R1 - t.R0) * f;
                        int i0 = Mathf.Max(0, Mathf.CeilToInt((l + half) / cell)), i1 = Mathf.Min(row - 1, Mathf.FloorToInt((r + half) / cell));
                        for (int i = i0; i <= i1; i++) into[j * row + i] = o;
                    }
                }
        }

        // How drifted (x: scoured 0, untouched 0.5, drifted 1) and how grimy (y), or below 0 how shaded under trees, the
        // snow is at a point whose ground is <paramref name="o"/>'s, both eased to nothing (0.5, 0) by
        // <paramref name="fade"/>; water and parking lots untouched.
        private static Vector2 ToneAt(Ground g, Vector2 p, Owner o, float fade)
        {
            Vector2 w = g.World(p);
            float along = Vector2.Dot(w, Downwind), across = Vector2.Dot(w, new Vector2(-Downwind.y, Downwind.x));
            float grime = 0f, shade = 0f, calm = 1f;
            if (o != null)
                switch (o.Kind)
                {
                    case "water": case "parking": return new Vector2(0.5f, 0f);
                    case "fuel": case "plaza":
                        float packed = 0.6f * WorldNoise(new Vector2(along / 11f, across / 7f), 1f, SaltTone + 3)
                            + 0.4f * WorldNoise(w, 5f, SaltTone + 4);
                        return new Vector2(0.5f + 0.45f * Smooth(0.45f, 0.78f, packed) * fade, 0.06f * fade);
                    case "industrial": grime = 0.45f; break;
                    case "farmyard": grime = 0.35f; break;
                    case "lot": case "quarry": grime = 0.3f; break;
                    case "commercial": case "retail": grime = 0.18f; break;
                    case "forest": calm = 0.4f; shade = 0.75f; break;   // the trees break the wind and shade the snow
                    case "scrub": case "orchard": calm = 0.75f; shade = 0.2f; break;
                }
            float large = WorldNoise(new Vector2(along / 60f, across / 15f), 1f, SaltTone);
            float streak = WorldNoise(new Vector2(along / 18f, across / 6f), 1f, SaltTone + 1);
            float drift = 0.5f + calm * (1.15f * (large - 0.5f) + 0.5f * (streak - 0.5f));
            float vary = 0.5f + WorldNoise(w, 17f, SaltTone + 2);
            float y = shade > 0f ? -Mathf.Clamp01(shade * (0.7f + 0.3f * vary)) : Mathf.Clamp01(grime * vary);
            return new Vector2(Mathf.Lerp(0.5f, Mathf.Clamp01(drift), fade), y * fade);
        }
    }
}
