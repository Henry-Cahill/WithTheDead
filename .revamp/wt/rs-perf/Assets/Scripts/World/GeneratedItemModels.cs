using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Ground models produced by the offline LLM asset pipeline (Tools/AssetPipeline), loaded from
    /// <c>Resources/GeneratedItemModels/&lt;item id&gt;.json</c>.
    ///
    /// They sit between the hand-built models in <see cref="ItemModels"/> and the single tinted
    /// primitive of <see cref="FallbackItemModel"/>: a bespoke C# model always wins, and an item with
    /// neither falls back as before. A generated model is the same flat list of placed primitives a
    /// hand-built one flattens to, so it bakes and draws instanced exactly like one.
    ///
    /// Files are read lazily, once per item type, the first time that item needs drawing.
    /// </summary>
    public static class GeneratedItemModels
    {
        public const string ResourceFolder = "GeneratedItemModels";

        private static readonly Dictionary<string, ItemModelDesc> s_Cache = new Dictionary<string, ItemModelDesc>(256);

        // Filled by JsonUtility through reflection, which the compiler cannot see.
#pragma warning disable 0649
        [Serializable]
        private sealed class FilePart
        {
            public string shape;
            public float[] pos;
            public float[] rot;
            public float[] size;
            public float[] color;
            public float smooth;
            public float metal;
        }

        [Serializable]
        private sealed class FileModel
        {
            public int version;
            public string id;
            public float baseHeight;
            public FilePart[] parts;
        }
#pragma warning restore 0649

        /// <summary>The generated model for <paramref name="def"/>, or null when there is none.</summary>
        public static ItemModelDesc Describe(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (s_Cache.TryGetValue(def.Id, out ItemModelDesc cached)) return cached;

            ItemModelDesc desc = null;
            TextAsset ta = Resources.Load<TextAsset>(ResourceFolder + "/" + def.Id);
            if (ta != null)
            {
                desc = Parse(ta.text, def.Id);
                Resources.UnloadAsset(ta);
            }

            s_Cache[def.Id] = desc;
            return desc;
        }

        /// <summary>Turns a pipeline JSON file into a model description; null if it holds nothing usable.</summary>
        public static ItemModelDesc Parse(string json, string idForLog = null)
        {
            FileModel file;
            try
            {
                file = JsonUtility.FromJson<FileModel>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Generated model '" + idForLog + "' is not valid JSON: " + e.Message);
                return null;
            }
            if (file?.parts == null || file.parts.Length == 0) return null;

            var parts = new List<Part>(file.parts.Length);
            for (int i = 0; i < file.parts.Length; i++)
            {
                FilePart p = file.parts[i];
                if (p == null || !Vec3(p.pos, out Vector3 pos) || !Vec3(p.size, out Vector3 size)) continue;
                Vec3(p.rot, out Vector3 rot);

                PartShape shape = ShapeOf(p.shape);

                // Files hold real sizes for every shape; the cylinder mesh is two units tall, so its
                // scale.y is half the real height, as in the hand-built models.
                if (shape == PartShape.Cylinder) size.y *= 0.5f;

                Color color = Vec3(p.color, out Vector3 c) ? new Color(c.x, c.y, c.z) : Color.gray;
                parts.Add(new Part(shape, pos, Quaternion.Euler(rot), size,
                    PartMat.Of(color, Mathf.Clamp01(p.smooth), Mathf.Clamp01(p.metal))));
            }

            if (parts.Count == 0) return null;
            return new ItemModelDesc(parts, Mathf.Max(0.02f, file.baseHeight));
        }

        /// <summary>Drops the cache so edited or regenerated files are read again.</summary>
        public static void Clear() => s_Cache.Clear();

        private static PartShape ShapeOf(string s)
        {
            switch (s)
            {
                case "sphere": return PartShape.Sphere;
                case "cylinder": return PartShape.Cylinder;
                default: return PartShape.Cube;
            }
        }

        private static bool Vec3(float[] a, out Vector3 v)
        {
            if (a == null || a.Length < 3 || !float.IsFinite(a[0]) || !float.IsFinite(a[1]) || !float.IsFinite(a[2]))
            {
                v = Vector3.zero;
                return false;
            }
            v = new Vector3(a[0], a[1], a[2]);
            return true;
        }
    }
}
