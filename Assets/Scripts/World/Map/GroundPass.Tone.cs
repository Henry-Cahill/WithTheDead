using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The snow's tone: one see-through sheet over the ground (a grid, 4 m a cell on a chunk, 5 m over a fixed map and
    // its scenery ring), each corner looking up how drifted, scoured or grimy the snow is there in a small table
    // (ToneMaterial). Drifts and scoured crust come from noise fixed to the world and stretched along the wind (from
    // the north-west), so neighbouring chunks agree; grime from the land use (industry, lots, farmyards, town centres).
    // Water and paving show through untouched.
    internal static partial class GroundPass
    {
        private static readonly Vector2 Downwind = new Vector2(0.7071f, -0.7071f);   // the wind blows to the south-east

        private static void Tone(Ground g)
        {
            bool chunk = g.Site.Chunked;
            float half = chunk ? g.Half : g.Half + SceneryReach;
            int cells = Mathf.CeilToInt(half * 2f / (chunk ? 4f : 5f));
            float cell = half * 2f / cells;
            int row = cells + 1;
            var uv = new Vector2[row * row];
            for (int j = 0; j <= cells; j++)
                for (int i = 0; i <= cells; i++)
                {
                    var p = new Vector2(-half + i * cell, -half + j * cell);
                    uv[j * row + i] = ToneAt(g, p, chunk ? 1f : Mathf.Clamp01((half - Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y))) / 20f));
                }
            var verts = new List<Vector3>(row * row);
            var uvs = new List<Vector2>(row * row);
            var tris = new List<int>(cells * cells * 6);
            var index = new int[row * row];
            for (int k = 0; k < index.Length; k++) index[k] = -1;
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                    if (Clean(uv[a]) && Clean(uv[b]) && Clean(uv[c]) && Clean(uv[d])) continue;
                    int va = Vertex(a), vb = Vertex(b), vc = Vertex(c), vd = Vertex(d);
                    tris.Add(va); tris.Add(vc); tris.Add(vd);   // clockwise seen from above: the face looks up
                    tris.Add(va); tris.Add(vd); tris.Add(vb);
                }
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
                uvs.Add(uv[k]);
                return index[k];
            }
        }

        private static bool Clean(Vector2 uv) => Mathf.Abs(uv.x - 0.5f) < 0.04f && uv.y < 0.02f;

        // The table's coordinates at a point: u how scoured (0) or drifted (1) the snow is, v how grimy; both eased to
        // nothing (0.5, 0) by <paramref name="fade"/> and on water and paving.
        private static Vector2 ToneAt(Ground g, Vector2 p, float fade)
        {
            Owner o = OwnerAnywhere(g, p);
            if (o != null && (o.Kind == "water" || o.Kind == "parking" || o.Kind == "fuel" || o.Kind == "plaza")) return new Vector2(0.5f, 0f);
            Vector2 w = g.World(p);
            float along = Vector2.Dot(w, Downwind), across = Vector2.Dot(w, new Vector2(-Downwind.y, Downwind.x));
            float large = WorldNoise(new Vector2(along / 70f, across / 26f), 1f, SaltTone);
            float mid = WorldNoise(new Vector2(along / 21f, across / 8f), 1f, SaltTone + 1);
            float drift = 0.5f + 0.95f * (0.65f * (large - 0.5f) + 0.35f * (mid - 0.5f));
            float grime = 0f;
            if (o != null)
                switch (o.Kind)
                {
                    case "industrial": grime = 0.55f; break;
                    case "farmyard": grime = 0.4f; break;
                    case "lot": case "quarry": grime = 0.35f; break;
                    case "commercial": case "retail": grime = 0.22f; break;
                    case "residential": grime = 0.05f; break;
                    case "forest": drift = Mathf.Lerp(drift, 0.5f, 0.5f); break;   // the trees break the wind
                }
            grime *= 0.6f + 0.8f * WorldNoise(w, 23f, SaltTone + 2);
            return new Vector2(Mathf.Lerp(0.5f, Mathf.Clamp01(drift), fade), Mathf.Clamp01(grime) * fade);
        }

        // The owner at a point of a fixed map's square or scenery ring (or a chunk's square).
        private static Owner OwnerAnywhere(Ground g, Vector2 p)
        {
            if (Mathf.Abs(p.x) <= g.Half && Mathf.Abs(p.y) <= g.Half) return g.OwnerAt(p);
            for (int b = 0; b < g.Ring.Count; b++)
            {
                if (!RingBand(g, b).Contains(p)) continue;
                List<Owner> owners = g.Ring[b];
                for (int i = owners.Count - 1; i >= 0; i--)
                {
                    Owner o = owners[i];
                    if (o.Bounds.Contains(p) && Inside(o.Pts, p)) return o;
                }
                return null;
            }
            return null;
        }
    }
}
