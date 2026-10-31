using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Textured item meshes generated offline on the DGX Spark by the asset pipeline's mesh stage
    /// (Tools/AssetPipeline/mesh): reference photos turned into meshes by TRELLIS.2 and fitted onto the item's
    /// primitive model. An item has up to <see cref="MaxVariants"/> different versions, each read from
    /// <c>Resources/GeneratedItemMeshes/&lt;item id&gt;@&lt;n&gt;.bytes</c> with its maps beside it: <c>_tex0</c> and
    /// <c>_tex1</c> (each level of detail's base colour), and for the near level <c>_nrm0</c> (a normal map baked from
    /// the full-detail mesh) and <c>_msk0</c> (URP's metallic map).
    ///
    /// An item that has them is drawn with them instead of any primitive model, hand-built ones included (see
    /// <see cref="ItemMeshBaker.Get(ItemDef, ItemMeshBaker.Lod, int)"/>). The fit keeps the primitive model's size,
    /// resting pose and axes, so dropped items, the bob and held items work unchanged. Each version gets one instanced
    /// material per level of detail, made with its textures the first time that level is drawn.
    ///
    /// The file layout is written by Tools/AssetPipeline/mesh/meshpost.py and documented there.
    /// </summary>
    public static class GeneratedItemMeshes
    {
        public const string ResourceFolder = "GeneratedItemMeshes";

        /// <summary>The material every near level is cloned from (Resources/ItemMeshes/Template.mat, made by the
        /// editor's ItemMeshMaterialSetup): URP Lit with a normal map and a metallic map. Being an asset in the build,
        /// it keeps those shader variants in the player, which a material made only at runtime would lose to variant
        /// stripping.</summary>
        public const string TemplatePath = "ItemMeshes/Template";

        /// <summary>The most versions of one item the game looks for.</summary>
        public const int MaxVariants = 8;

        private const uint Magic = 0x324D5A43;   // "CZM2" read as a little-endian uint
        private const int Version = 3;
        private const uint FlagDeflate = 1;
        private const uint FlagMaps = 2;
        private const int HeaderSize = 28;

        private sealed class Variant
        {
            public string Stem;
            public Mesh[] Meshes;
            public ItemMeshBaker.Baked[] Baked;   // per level of detail, made on first use
            public float BaseHeight, Smoothness, Metallic;
            public bool HasMaps;
        }

        private static readonly Dictionary<string, Variant[]> s_Items = new Dictionary<string, Variant[]>(256);
        private static Material s_Template;
        private static bool s_TemplateLoaded;

        /// <summary>How many generated versions <paramref name="def"/> has; 0 when it has none. The item's files are
        /// read once, the first time it is asked about.</summary>
        public static int VariantCount(ItemDef def) => Variants(def)?.Length ?? 0;

        /// <summary>One version of the generated mesh for <paramref name="def"/> at this level of detail, or null
        /// when there is none. <paramref name="variant"/> wraps around the versions there are.</summary>
        public static ItemMeshBaker.Baked Get(ItemDef def, ItemMeshBaker.Lod lod, int variant = 0)
        {
            Variant[] variants = Variants(def);
            if (variants == null || variants.Length == 0) return null;
            Variant v = variants[(int)((uint)variant % (uint)variants.Length)];
            int level = Mathf.Min((int)lod, v.Meshes.Length - 1);
            return v.Baked[level] ?? (v.Baked[level] = Bake(v, level));
        }

        /// <summary>Destroys every mesh and material made here and drops the cache, so regenerated files are read
        /// again. <see cref="ItemMeshBaker.Clear"/> calls it with the baker's own clear.</summary>
        public static void Clear()
        {
            foreach (Variant[] variants in s_Items.Values)
            {
                foreach (Variant v in variants)
                {
                    foreach (Mesh m in v.Meshes) Release(m);
                    foreach (ItemMeshBaker.Baked b in v.Baked)
                    {
                        if (b == null) continue;
                        foreach (Material m in b.Materials) Release(m);
                    }
                }
            }
            s_Items.Clear();
        }

        // Destroy is refused outside play mode, where the editor tools load these meshes too.
        private static void Release(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        private static Variant[] Variants(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (s_Items.TryGetValue(def.Id, out Variant[] cached)) return cached;

            var list = new List<Variant>(3);
            // Every slot is looked at, so a version that failed to generate doesn't hide the ones after it.
            for (int n = 1; n <= MaxVariants; n++)
            {
                string stem = def.Id + "@" + n;
                TextAsset ta = Resources.Load<TextAsset>(ResourceFolder + "/" + stem);
                if (ta == null) continue;
                byte[] data = ta.bytes;
                Resources.UnloadAsset(ta);
                try
                {
                    list.Add(Parse(stem, data));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[CloneZ] Generated mesh '" + stem + "' is unreadable: " + e.Message);
                }
            }

            Variant[] variants = list.ToArray();
            s_Items[def.Id] = variants;
            return variants;
        }

        private static Variant Parse(string stem, byte[] data)
        {
            if (data.Length < HeaderSize) throw new FormatException("too short");
            Variant v;
            uint flags;
            byte[] body;
            using (var head = new BinaryReader(new MemoryStream(data, 0, HeaderSize)))
            {
                if (head.ReadUInt32() != Magic) throw new FormatException("not a CZM2 file");
                int version = head.ReadUInt16();
                if (version != Version) throw new FormatException("unsupported version " + version);
                int count = head.ReadUInt16();
                v = new Variant
                {
                    Stem = stem,
                    BaseHeight = head.ReadSingle(),
                    Smoothness = Mathf.Clamp01(head.ReadSingle()),
                    Metallic = Mathf.Clamp01(head.ReadSingle()),
                    Meshes = new Mesh[count],
                    Baked = new ItemMeshBaker.Baked[count],
                };
                flags = head.ReadUInt32();
                body = new byte[checked((int)head.ReadUInt32())];
            }
            v.HasMaps = (flags & FlagMaps) != 0;

            using (Stream packed = new MemoryStream(data, HeaderSize, data.Length - HeaderSize))
            using (Stream source = (flags & FlagDeflate) != 0 ? new DeflateStream(packed, CompressionMode.Decompress) : packed)
            {
                int read = 0;
                while (read < body.Length)
                {
                    int got = source.Read(body, read, body.Length - read);
                    if (got <= 0) throw new FormatException("the body ends early");
                    read += got;
                }
            }

            using (var r = new BinaryReader(new MemoryStream(body)))
            {
                for (int lod = 0; lod < v.Meshes.Length; lod++) v.Meshes[lod] = ReadLod(r, stem, lod);
            }
            return v;
        }

        private static Mesh ReadLod(BinaryReader r, string stem, int lod)
        {
            int vertexCount = checked((int)r.ReadUInt32());
            int indexCount = checked((int)r.ReadUInt32());
            if (vertexCount == 0 || indexCount % 3 != 0) throw new FormatException("empty or ragged LOD " + lod);
            var boxMin = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            var boxSize = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) / 65535f;

            var verts = new Vector3[vertexCount];
            var norms = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                verts[i] = boxMin + Vector3.Scale(new Vector3(r.ReadUInt16(), r.ReadUInt16(), r.ReadUInt16()), boxSize);
                norms[i] = OctDecode(r.ReadInt16() / 32767f, r.ReadInt16() / 32767f);
                var t = new Vector3(r.ReadSByte(), r.ReadSByte(), r.ReadSByte()).normalized;
                tangents[i] = new Vector4(t.x, t.y, t.z, r.ReadSByte() < 0 ? -1f : 1f);
                uvs[i] = new Vector2(r.ReadUInt16() / 65535f, r.ReadUInt16() / 65535f);
            }

            var tris = new int[indexCount];
            for (int i = 0; i < indexCount; i++)
            {
                int index = r.ReadUInt16();
                if (index >= vertexCount) throw new FormatException("index out of range in LOD " + lod);
                tris[i] = index;
            }
            r.BaseStream.Position += (4 - r.BaseStream.Position % 4) % 4;

            var mesh = new Mesh { name = "ItemMesh_" + stem + "_" + (ItemMeshBaker.Lod)lod };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, calculateBounds: true);
            mesh.UploadMeshData(markNoLongerReadable: true);
            return mesh;
        }

        /// <summary>A unit vector from its octahedral encoding (two numbers in [-1, 1]), as meshpost.oct_encode writes it.</summary>
        private static Vector3 OctDecode(float x, float y)
        {
            var n = new Vector3(x, y, 1f - Mathf.Abs(x) - Mathf.Abs(y));
            if (n.z < 0f)
            {
                float ox = n.x;
                n.x = (1f - Mathf.Abs(n.y)) * (ox >= 0f ? 1f : -1f);
                n.y = (1f - Mathf.Abs(ox)) * (n.y >= 0f ? 1f : -1f);
            }
            return n.normalized;
        }

        private static ItemMeshBaker.Baked Bake(Variant v, int lod)
        {
            Mesh mesh = v.Meshes[lod];
            return new ItemMeshBaker.Baked
            {
                Mesh = mesh,
                Materials = new[] { CreateMaterial(v, lod) },
                BaseHeight = v.BaseHeight,
                Bounds = mesh.bounds,
                VertexCount = mesh.vertexCount,
            };
        }

        private static Material CreateMaterial(Variant v, int lod)
        {
            Texture2D baseMap = Load(v.Stem + "_tex" + lod);
            Material m = null;

            // The near level gets its normal and metallic maps when the template that keeps their variants is there.
            if (lod == 0 && v.HasMaps && Template != null)
            {
                Texture2D normalMap = Load(v.Stem + "_nrm0");
                Texture2D maskMap = Load(v.Stem + "_msk0");
                if (normalMap != null && maskMap != null)
                {
                    m = new Material(Template);
                    m.SetTexture("_BumpMap", normalMap);
                    m.SetTexture("_MetallicGlossMap", maskMap);
                }
            }

            if (m == null)
            {
                m = new Material(CloneZUtil.LitShader);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", v.Smoothness);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", v.Metallic);
            }

            m.name = "ItemMesh_" + v.Stem + "_" + lod;
            m.color = Color.white;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (baseMap != null)
            {
                m.mainTexture = baseMap;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", baseMap);
            }
            m.enableInstancing = true;   // without this every copy on the ground is its own draw call
            return m;
        }

        private static Texture2D Load(string name)
        {
            Texture2D tex = Resources.Load<Texture2D>(ResourceFolder + "/" + name);
            if (tex == null) Debug.LogWarning("[CloneZ] Generated mesh texture '" + name + "' is missing");
            return tex;
        }

        private static Material Template
        {
            get
            {
                if (!s_TemplateLoaded)
                {
                    s_TemplateLoaded = true;
                    s_Template = Resources.Load<Material>(TemplatePath);
                    if (s_Template == null)
                        Debug.LogWarning("[CloneZ] Resources/" + TemplatePath + ".mat is missing, so generated item meshes " +
                                         "are drawn without their normal and metallic maps. Unity's editor makes it (ItemMeshMaterialSetup).");
                }
                return s_Template;
            }
        }
    }
}
