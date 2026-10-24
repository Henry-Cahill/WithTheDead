using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The snow's tone: one see-through sheet over the ground (a grid of 2.56 m cells on a chunk, about 2.9 m over a fixed
    // map and its scenery ring), each corner looking up how drifted, scoured, grimy or shaded the snow is there in a
    // small table (ToneMaterial). Drifts and scoured crust come from noise fixed to the world and drawn out along the wind
    // (from the north-west), so neighbouring chunks agree; grime and shade from the land use (industry, lots, farmyards,
    // town centres, woods). Plazas and forecourts get trodden snow in broad patches; water and parking lots show through
    // untouched, the lots' stall rows getting their packed snow as extra geometry in the same mesh (GroundPass.Parking).
    internal static partial class GroundPass
    {
        private static readonly Vector2 Downwind = new Vector2(0.7071f, -0.7071f);   // the wind blows to the south-east

        // The table: 64 columns by two bands of 16 rows. In the first, the snow's tone: across, scoured crust (0) through
        // untouched (0.5) to fresh drift (1); down, clean to grimy or shaded. In the second, packed snow lying on paving:
        // across, how much of it (0 none).
        private const int ToneW = 64, ToneBand = 16, ToneH = ToneBand * 2;

        private static Vector2 ToneUv(float drift, float grime) => new Vector2(drift, (0.5f + Mathf.Clamp01(grime) * (ToneBand - 1)) / ToneH);

        private static Vector2 PackedUv(float amount) => new Vector2(Mathf.Clamp01(amount), ToneBand * 1.5f / ToneH);

        private static void Tone(Ground g)
        {
            bool chunk = g.Site.Chunked;
            float half = chunk ? g.Half : g.Half + SceneryReach;
            int cells = chunk ? 50 : Mathf.CeilToInt(half * 2f / 2.9f);
            float cell = half * 2f / cells;
            int row = cells + 1;
            var owners = new Owner[row * row];
            Raster(g.Owners, owners, half, cell, row);
            foreach (List<Owner> band in g.Ring) Raster(band, owners, half, cell, row);
            var tone = new Vector2[row * row];
            for (int j = 0; j <= cells; j++)
                for (int i = 0; i <= cells; i++)
                {
                    var p = new Vector2(-half + i * cell, -half + j * cell);
                    float fade = chunk ? 1f : Mathf.Clamp01((half - Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y))) / 20f);
                    tone[j * row + i] = ToneAt(g, p, owners[j * row + i], fade);
                }
            int extra = g.ToneVerts.Count;
            var verts = new List<Vector3>(row * row / 2 + extra);
            var uvs = new List<Vector2>(row * row / 2 + extra);
            var tris = new List<int>(cells * cells * 3 + g.ToneTris.Count);
            var index = new int[row * row];
            for (int k = 0; k < index.Length; k++) index[k] = -1;
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
            var normals = new List<Vector3>(verts.Count);
            for (int k = 0; k < verts.Count; k++) normals.Add(Vector3.up);
            mesh.SetNormals(normals);
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
                uvs.Add(ToneUv(tone[k].x, tone[k].y));
                return index[k];
            }
        }

        private static bool Clean(Vector2 t) => Mathf.Abs(t.x - 0.5f) < 0.04f && t.y < 0.02f;

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

        // How drifted (x: scoured 0, untouched 0.5, drifted 1) and how grimy or shaded (y) the snow is at a point whose
        // ground is <paramref name="o"/>'s, both eased to nothing (0.5, 0) by <paramref name="fade"/>; water and parking
        // lots untouched.
        private static Vector2 ToneAt(Ground g, Vector2 p, Owner o, float fade)
        {
            Vector2 w = g.World(p);
            float along = Vector2.Dot(w, Downwind), across = Vector2.Dot(w, new Vector2(-Downwind.y, Downwind.x));
            float grime = 0f, calm = 1f;
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
                    case "forest": calm = 0.4f; grime = 0.32f; break;   // the trees break the wind and shade the snow
                    case "scrub": case "orchard": calm = 0.75f; break;
                }
            float large = WorldNoise(new Vector2(along / 60f, across / 15f), 1f, SaltTone);
            float streak = WorldNoise(new Vector2(along / 18f, across / 6f), 1f, SaltTone + 1);
            float drift = 0.5f + calm * (1.1f * (large - 0.5f) + 0.75f * (streak - 0.5f));
            grime *= 0.5f + WorldNoise(w, 17f, SaltTone + 2);
            return new Vector2(Mathf.Lerp(0.5f, Mathf.Clamp01(drift), fade), Mathf.Clamp01(grime) * fade);
        }
    }
}
