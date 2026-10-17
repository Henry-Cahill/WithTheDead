using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Turns an <see cref="ItemModelDesc"/> into a single combined mesh that can be drawn instanced.
    ///
    /// The old ground visual for one dropped item was a few hundred GameObjects, each with its own
    /// renderer and its own freshly allocated material. That is roughly three hundred draw calls and
    /// ten material allocations per item, which is unworkable once the world holds items in the
    /// millions. Baking flattens a whole model into one mesh with the part colours written into
    /// vertex colours, and groups the parts by surface so the mesh needs only a handful of shared
    /// materials — shared across every item in the game, not per instance. Drawing a thousand copies
    /// of the same item then costs the same handful of draw calls as drawing one.
    ///
    /// Meshes are generated here rather than taken from <see cref="GameObject.CreatePrimitive"/>,
    /// because the built-in sphere is 515 vertices. At the part counts these models use that would put
    /// a single dress well past a hundred thousand vertices, for something that is usually a few
    /// pixels across on the floor. Generating the primitives lets the tessellation be chosen per level
    /// of detail instead.
    /// </summary>
    public static class ItemMeshBaker
    {
        /// <summary>How much detail a bake keeps. Near items get every part; far items get the large
        /// parts only, welded into a single material so they cost one draw call.</summary>
        public enum Lod { Near = 0, Far = 1 }

        /// <summary>A model ready to draw: one mesh, and one material per submesh.</summary>
        public sealed class Baked
        {
            public Mesh Mesh;
            public Material[] Materials;
            public float BaseHeight;
            public Bounds Bounds;
            public int VertexCount;
        }

        private static readonly Dictionary<(string, Lod), Baked> s_Cache = new Dictionary<(string, Lod), Baked>(256);

        /// <summary>Shared materials, keyed by quantised surface, so near-identical surfaces across all
        /// items collapse onto one material instead of one per part per item.</summary>
        private static readonly Dictionary<int, Material> s_Palette = new Dictionary<int, Material>(512);

        /// <summary>Parts smaller than this fraction of the model's largest part are dropped from the
        /// far bake — fray threads and single stitches are not readable at range.</summary>
        private const float FarVolumeFraction = 0.02f;
        private const int FarMinParts = 4;

        /// <summary>The baked model for an item, or null when the item has no bespoke model.</summary>
        public static Baked Get(ItemDef def, Lod lod)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;

            var key = (def.Id, lod);
            if (s_Cache.TryGetValue(key, out Baked cached)) return cached;

            // Items with no bespoke model use one generated offline by the LLM asset pipeline if there
            // is one, and otherwise fall back to a plain tinted primitive — described and baked exactly
            // like any other model, so every kind of item costs the same to draw.
            ItemModelDesc desc = ItemModels.Describe(def)
                ?? GeneratedItemModels.Describe(def)
                ?? FallbackItemModel.Describe(def);
            Baked baked = desc == null ? null : Bake(desc, def.Id, lod);
            s_Cache[key] = baked;
            return baked;
        }

        /// <summary>Drops every cached mesh and material. Meshes created at runtime are not reclaimed
        /// by ordinary garbage collection, so they have to be destroyed by hand.</summary>
        public static void Clear()
        {
            foreach (Baked b in s_Cache.Values)
            {
                if (b?.Mesh != null) Object.Destroy(b.Mesh);
            }
            s_Cache.Clear();

            foreach (Material m in s_Palette.Values)
            {
                if (m != null) Object.Destroy(m);
            }
            s_Palette.Clear();
        }

        // ── Baking ────────────────────────────────────────────────────────────────────────────────────

        private static Baked Bake(ItemModelDesc desc, string id, Lod lod)
        {
            List<Part> parts = SelectParts(desc, lod);
            if (parts.Count == 0) return null;

            bool far = lod == Lod.Far;

            // Group the parts by surface. Near bakes keep the distinct surfaces so the model still
            // reads; far bakes collapse everything onto one, since at range the silhouette is all that
            // survives anyway and one submesh means one draw call.
            var groups = new Dictionary<int, List<Part>>(16);
            var groupMat = new Dictionary<int, PartMat>(16);

            for (int i = 0; i < parts.Count; i++)
            {
                PartMat pm = parts[i].Mat;
                int key = far ? 0 : SurfaceKey(pm);
                if (!groups.TryGetValue(key, out List<Part> list))
                {
                    list = new List<Part>(32);
                    groups[key] = list;
                    groupMat[key] = pm;
                }
                list.Add(parts[i]);
            }

            if (far) groupMat[0] = AverageSurface(parts);

            var verts = new List<Vector3>(1024);
            var norms = new List<Vector3>(1024);
            var colors = new List<Color>(1024);
            var subIndices = new List<int[]>(groups.Count);
            var materials = new List<Material>(groups.Count);

            foreach (KeyValuePair<int, List<Part>> group in groups)
            {
                var indices = new List<int>(2048);
                List<Part> list = group.Value;

                for (int i = 0; i < list.Count; i++) Emit(list[i], lod, verts, norms, colors, indices);

                subIndices.Add(indices.ToArray());
                materials.Add(GetMaterial(groupMat[group.Key]));
            }

            var mesh = new Mesh { name = "ItemModel_" + id + "_" + lod };

            // Past 65k vertices the default 16-bit index buffer cannot address the mesh.
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(colors);
            mesh.subMeshCount = subIndices.Count;
            for (int s = 0; s < subIndices.Count; s++) mesh.SetTriangles(subIndices[s], s, calculateBounds: false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(markNoLongerReadable: true);

            return new Baked
            {
                Mesh = mesh,
                Materials = materials.ToArray(),
                BaseHeight = desc.BaseHeight,
                Bounds = mesh.bounds,
                VertexCount = verts.Count,
            };
        }

        /// <summary>Which parts survive into this level of detail.</summary>
        private static List<Part> SelectParts(ItemModelDesc desc, Lod lod)
        {
            if (lod == Lod.Near) return desc.Parts;

            float biggest = 0f;
            for (int i = 0; i < desc.Parts.Count; i++) biggest = Mathf.Max(biggest, desc.Parts[i].Volume);

            float cutoff = biggest * FarVolumeFraction;
            var kept = new List<Part>(32);
            for (int i = 0; i < desc.Parts.Count; i++)
            {
                if (desc.Parts[i].Volume >= cutoff) kept.Add(desc.Parts[i]);
            }

            // Never decimate a model out of existence — a handful of parts is still a silhouette.
            if (kept.Count < FarMinParts)
            {
                kept.Clear();
                var bySize = new List<Part>(desc.Parts);
                bySize.Sort((a, b) => b.Volume.CompareTo(a.Volume));
                for (int i = 0; i < Mathf.Min(FarMinParts, bySize.Count); i++) kept.Add(bySize[i]);
            }

            return kept;
        }

        /// <summary>Writes one primitive into the shared vertex buffers, already placed.</summary>
        private static void Emit(in Part part, Lod lod, List<Vector3> verts, List<Vector3> norms,
            List<Color> colors, List<int> indices)
        {
            Matrix4x4 m = Matrix4x4.TRS(part.Position, part.Rotation, part.Scale);

            // Non-uniform scale is used heavily (flattened ellipsoids), so normals need the
            // inverse transpose rather than the plain matrix or they end up skewed.
            Matrix4x4 nm = Matrix4x4.Transpose(m.inverse);

            int baseVert = verts.Count;
            PrimitiveMesh src = PrimitiveMesh.For(part.Shape, lod);

            for (int i = 0; i < src.Verts.Length; i++)
            {
                verts.Add(m.MultiplyPoint3x4(src.Verts[i]));
                norms.Add(nm.MultiplyVector(src.Norms[i]).normalized);
                colors.Add(part.Mat.Color);
            }

            for (int i = 0; i < src.Tris.Length; i++) indices.Add(baseVert + src.Tris[i]);
        }

        // ── Shared materials ──────────────────────────────────────────────────────────────────────────

        /// <summary>Quantises a surface so near-identical ones share a material.</summary>
        private static int SurfaceKey(in PartMat m)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(m.Color.r * 31f), 0, 31);
            int g = Mathf.Clamp(Mathf.RoundToInt(m.Color.g * 31f), 0, 31);
            int b = Mathf.Clamp(Mathf.RoundToInt(m.Color.b * 31f), 0, 31);
            int s = Mathf.Clamp(Mathf.RoundToInt(m.Smoothness * 15f), 0, 15);
            int t = Mathf.Clamp(Mathf.RoundToInt(m.Metallic * 7f), 0, 7);
            return (r << 19) | (g << 14) | (b << 9) | (s << 5) | t;
        }

        private static Material GetMaterial(in PartMat m)
        {
            int key = SurfaceKey(m);
            if (s_Palette.TryGetValue(key, out Material cached)) return cached;

            Material mat = CloneZUtil.Mat(m.Color, m.Smoothness, m.Metallic);
            mat.enableInstancing = true;   // without this every copy is its own draw call
            s_Palette[key] = mat;
            return mat;
        }

        /// <summary>The one surface a far bake is drawn with: the parts' colours weighted by how much
        /// of the model each covers, so the result matches the shape's overall tone.</summary>
        private static PartMat AverageSurface(List<Part> parts)
        {
            float total = 0f;
            Color sum = Color.black;
            float smooth = 0f, metal = 0f;

            for (int i = 0; i < parts.Count; i++)
            {
                float w = Mathf.Max(parts[i].Volume, 1e-9f);
                total += w;
                sum += parts[i].Mat.Color * w;
                smooth += parts[i].Mat.Smoothness * w;
                metal += parts[i].Mat.Metallic * w;
            }

            if (total <= 0f) return parts.Count > 0 ? parts[0].Mat : PartMat.Of(Color.grey);
            return new PartMat(sum / total, smooth / total, metal / total);
        }
    }
}
