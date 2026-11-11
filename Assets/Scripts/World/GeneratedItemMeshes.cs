using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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
    /// What's made here stays cached while it's in use, within a budget of video memory (<see cref="Budget"/>). Past it,
    /// <see cref="Trim"/> lets go of the items drawn least recently, so a long walk across the state doesn't keep adding to
    /// video memory. An item let go of is read from its files again the next time it's asked for.
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

        /// <summary>How long an item has to go undrawn, and not looked up, before <see cref="Trim"/> may let it go
        /// (seconds). It must not have been used in the frame before either, however long that took. Anything in view is
        /// drawn every frame, so this only spares what was in view a moment ago.</summary>
        public const float IdleSeconds = 2f;

        // Once over budget, Trim lets go of items until the cache is down to this share of it, so that it happens now and
        // then rather than every time something new comes into view.
        private const float TrimTo = 0.75f;

        /// <summary>Over budget with everything in use, <see cref="Trim"/> looks again after this long (seconds) rather
        /// than every frame.</summary>
        internal const float RetrySeconds = 1f;

        private const uint Magic = 0x324D5A43;   // "CZM2" read as a little-endian uint
        private const int Version = 3;
        private const uint FlagDeflate = 1;
        private const uint FlagMaps = 2;
        private const int HeaderSize = 28;

        // Everything made for one item: all its versions, and what they cost.
        private sealed class Item
        {
            public string Id;
            public Variant[] Variants;
            public long Bytes;        // its meshes and the maps loaded so far (MeshBytes, MapBytes)
            public float LastUsed;    // when it was last looked up; each level's Baked.LastUsed has when it was last drawn
            public int LastUsedFrame; // and in which frame
        }

        private sealed class Variant
        {
            public string Stem;
            public Mesh[] Meshes;
            public ItemMeshBaker.Baked[] Baked;   // per level of detail, made on first use
            public readonly List<Texture2D> Maps = new List<Texture2D>(4);   // loaded for those levels' materials
            public float BaseHeight, Smoothness, Metallic;
            public bool HasMaps;
        }

        private static readonly Dictionary<string, Item> s_Items = new Dictionary<string, Item>(256);
        private static readonly List<(float lastUsed, Item item)> s_Idle = new List<(float, Item)>(64);
        private static long s_Bytes, s_Budget;
        private static float s_NextTrim;
        private static Material s_Template;
        private static bool s_TemplateLoaded;

        /// <summary>The video memory the cache keeps to, in bytes (see <see cref="Trim"/>): a sixteenth of the graphics
        /// card's, between 64 and 256 MB, unless set. Set 0 for that default again.</summary>
        public static long Budget
        {
            get => s_Budget > 0 ? s_Budget : s_Budget = DefaultBudget();
            set => s_Budget = value;
        }

        /// <summary>The video memory taken by the meshes and maps cached here, in bytes. It's worked out from their sizes
        /// and formats, so it costs nothing to ask.</summary>
        public static long Bytes => s_Bytes;

        /// <summary>The part of <see cref="Bytes"/> that <see cref="Trim"/> has to keep for now: the items held, and those
        /// drawn or looked up in the last <see cref="IdleSeconds"/> or the frame before. While that's over the budget, so is
        /// the cache. It's worked out item by item when asked.</summary>
        public static long BytesInUse
        {
            get
            {
                float now = Time.unscaledTime;
                int frame = Time.frameCount;
                long bytes = 0;
                foreach (Item item in s_Items.Values)
                {
                    if (item.Bytes > 0 && InUse(item, now, frame, out _)) bytes += item.Bytes;
                }
                return bytes;
            }
        }

        /// <summary>The video memory taken by everything read in since the cache was last cleared, in bytes, counting again
        /// any item read in again after <see cref="Trim"/> let it go. Short of those, it's what the cache would hold
        /// without a budget.</summary>
        public static long BytesRead { get; private set; }

        /// <summary>How many items <see cref="Trim"/> has let go of since the cache was last cleared.</summary>
        public static int Evictions { get; private set; }

        /// <summary>How many generated versions <paramref name="def"/> has; 0 when it has none. The item's files are
        /// read the first time it is asked about, and again if <see cref="Trim"/> has let it go since.</summary>
        public static int VariantCount(ItemDef def) => Find(def)?.Variants.Length ?? 0;

        /// <summary>One version of the generated mesh for <paramref name="def"/> at this level of detail, or null
        /// when there is none. <paramref name="variant"/> wraps around the versions there are. Whatever keeps the result
        /// beyond this frame has to hold it (<see cref="ItemMeshBaker.Hold"/>) or look it up again when
        /// <see cref="ItemMeshBaker.Generation"/> changes.</summary>
        public static ItemMeshBaker.Baked Get(ItemDef def, ItemMeshBaker.Lod lod, int variant = 0)
        {
            Item item = Find(def);
            if (item == null || item.Variants.Length == 0) return null;
            Variant v = item.Variants[(int)((uint)variant % (uint)item.Variants.Length)];
            int level = Mathf.Min((int)lod, v.Meshes.Length - 1);
            ItemMeshBaker.Baked baked = v.Baked[level] ?? (v.Baked[level] = Bake(item, v, level));
            baked.MarkUsed(item.LastUsed, item.LastUsedFrame);
            return baked;
        }

        /// <summary>
        /// Keeps the cache to its <see cref="Budget"/>. Once it's over, the items drawn least recently are let go of
        /// until it's down to three quarters of the budget: their meshes and materials are destroyed and their maps
        /// unloaded. An item drawn or looked up in the last <see cref="IdleSeconds"/> or the frame before stays, and so
        /// does a held one (<see cref="ItemMeshBaker.Hold"/>). If every item is in use (<see cref="BytesInUse"/>), the cache
        /// stays over budget until some aren't, looking again every <see cref="RetrySeconds"/>. Letting anything go bumps
        /// <see cref="ItemMeshBaker.Generation"/>.
        ///
        /// <see cref="GroundItemRenderer"/> calls it once a frame, before it looks anything up or draws, so what it lets
        /// go of is never drawn. Under budget it only compares two numbers.
        /// </summary>
        public static void Trim()
        {
            if (s_Bytes <= Budget) return;
            float now = Time.unscaledTime;
            if (now < s_NextTrim) return;
            int frame = Time.frameCount;

            s_Idle.Clear();
            foreach (Item item in s_Items.Values)
            {
                if (item.Bytes == 0) continue;   // an item with no generated mesh: only its name is kept
                if (!InUse(item, now, frame, out float lastUsed)) s_Idle.Add((lastUsed, item));
            }
            s_Idle.Sort((a, b) => a.lastUsed.CompareTo(b.lastUsed));

            long target = (long)(Budget * (double)TrimTo);
            int evicted = 0;
            for (int i = 0; i < s_Idle.Count && s_Bytes > target; i++)
            {
                Item item = s_Idle[i].item;
                s_Items.Remove(item.Id);
                s_Bytes -= item.Bytes;
                Release(item);
                evicted++;
            }
            s_Idle.Clear();

            if (evicted > 0)
            {
                Evictions += evicted;
                ItemMeshBaker.Generation++;
            }
            if (s_Bytes > Budget) s_NextTrim = now + RetrySeconds;
        }

        /// <summary>Destroys every mesh and material made here, unloads their maps and drops the cache, so regenerated
        /// files are read again. <see cref="ItemMeshBaker.Clear"/> calls it with the baker's own clear.</summary>
        public static void Clear()
        {
            foreach (Item item in s_Items.Values) Release(item);
            s_Items.Clear();
            s_Bytes = 0;
            s_NextTrim = 0f;
            BytesRead = 0;
            Evictions = 0;
            ItemMeshBaker.Generation++;
        }

        // About a sixteenth of the graphics card's memory, from 64 MB up to 256 MB (4 GB cards and up). An item seen
        // close up takes 3.9 MB on average and 13 MB at most; one only seen from afar takes about 1.5 MB.
        private static long DefaultBudget() => Mathf.Clamp(SystemInfo.graphicsMemorySize / 16, 64, 256) * 1024L * 1024L;

        // Whether Trim has to keep an item: held, or drawn or looked up in the last IdleSeconds, or in this frame or the one
        // before (however long that took). `lastUsed` is when it last was.
        private static bool InUse(Item item, float now, int frame, out float lastUsed)
        {
            lastUsed = item.LastUsed;
            int lastFrame = item.LastUsedFrame;
            bool held = false;
            foreach (Variant v in item.Variants)
            {
                foreach (ItemMeshBaker.Baked b in v.Baked)
                {
                    if (b == null) continue;
                    if (b.LastUsed > lastUsed) lastUsed = b.LastUsed;
                    if (b.LastUsedFrame > lastFrame) lastFrame = b.LastUsedFrame;
                    if (b.Holders > 0) held = true;
                }
            }
            return held || now - lastUsed < IdleSeconds || lastFrame >= frame - 1;
        }

        private static void Release(Item item)
        {
            foreach (Variant v in item.Variants)
            {
                foreach (Mesh m in v.Meshes) Release(m);
                foreach (ItemMeshBaker.Baked b in v.Baked)
                {
                    if (b == null) continue;
                    foreach (Material m in b.Materials) Release(m);
                }
                // The maps come from Resources, so they're assets: unloaded rather than destroyed. They're read from disk
                // again if anything asks for them.
                foreach (Texture2D t in v.Maps)
                {
                    if (t != null) Resources.UnloadAsset(t);
                }
                v.Maps.Clear();
            }
        }

        // Destroy is refused outside play mode, where the editor tools load these meshes too.
        private static void Release(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        private static void Charge(Item item, long bytes)
        {
            item.Bytes += bytes;
            s_Bytes += bytes;
            BytesRead += bytes;
        }

        // The item's entry in the cache, read from its files if it has none, and marked as just used.
        private static Item Find(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (!s_Items.TryGetValue(def.Id, out Item item))
            {
                item = Read(def.Id);
                s_Items[def.Id] = item;
            }
            item.LastUsed = Time.unscaledTime;
            item.LastUsedFrame = Time.frameCount;
            return item;
        }

        private static Item Read(string id)
        {
            var item = new Item { Id = id };
            var list = new List<Variant>(3);
            // Every slot is looked at, so a version that failed to generate doesn't hide the ones after it.
            for (int n = 1; n <= MaxVariants; n++)
            {
                string stem = id + "@" + n;
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

            item.Variants = list.ToArray();
            long bytes = 0;
            foreach (Variant v in item.Variants)
            {
                foreach (Mesh m in v.Meshes) bytes += MeshBytes(m);
            }
            Charge(item, bytes);
            return item;
        }

        // A mesh's vertex and index buffers, as the graphics card holds them.
        private static long MeshBytes(Mesh m)
        {
            long stride = 0;
            for (int s = 0; s < m.vertexBufferCount; s++) stride += m.GetVertexBufferStride(s);
            long indices = 0;
            for (int s = 0; s < m.subMeshCount; s++) indices += m.GetIndexCount(s);
            return m.vertexCount * stride + indices * (m.indexFormat == IndexFormat.UInt32 ? 4 : 2);
        }

        // A map's mip chain, in the format it was imported to (block-compressed).
        private static long MapBytes(Texture2D t)
        {
            long bytes = 0;
            for (int level = 0; level < t.mipmapCount; level++)
                bytes += GraphicsFormatUtility.ComputeMipmapSize(Mathf.Max(1, t.width >> level), Mathf.Max(1, t.height >> level), t.graphicsFormat);
            return bytes;
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

        private static ItemMeshBaker.Baked Bake(Item item, Variant v, int lod)
        {
            Mesh mesh = v.Meshes[lod];
            int mapsBefore = v.Maps.Count;
            var baked = new ItemMeshBaker.Baked
            {
                Mesh = mesh,
                Materials = new[] { CreateMaterial(v, lod) },
                BaseHeight = v.BaseHeight,
                Bounds = mesh.bounds,
                VertexCount = mesh.vertexCount,
            };
            long bytes = 0;
            for (int i = mapsBefore; i < v.Maps.Count; i++) bytes += MapBytes(v.Maps[i]);
            Charge(item, bytes);
            return baked;
        }

        private static Material CreateMaterial(Variant v, int lod)
        {
            Texture2D baseMap = LoadMap(v, v.Stem + "_tex" + lod);
            Material m = null;

            // The near level gets its normal and metallic maps when the template that keeps their variants is there.
            if (lod == 0 && v.HasMaps && Template != null)
            {
                Texture2D normalMap = LoadMap(v, v.Stem + "_nrm0");
                Texture2D maskMap = LoadMap(v, v.Stem + "_msk0");
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

        // A map for one of the variant's materials, kept with the variant so it's unloaded with it (Release).
        private static Texture2D LoadMap(Variant v, string name)
        {
            Texture2D tex = Resources.Load<Texture2D>(ResourceFolder + "/" + name);
            if (tex == null) Debug.LogWarning("[CloneZ] Generated mesh texture '" + name + "' is missing");
            else v.Maps.Add(tex);
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
