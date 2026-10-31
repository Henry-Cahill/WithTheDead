using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Writes every hand-built item model (<see cref="ItemModels"/>) to Tools/AssetPipeline/out/handbuilt/&lt;id&gt;.json
    /// in the generated models' file format, so the asset pipeline's mesh stage can fit those items' meshes onto them,
    /// as it does with the generated ones (the pose and size of a held item come from its model). Run it from the menu
    /// once, and again after changing a hand-built model; or headless:
    /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.ItemModelExport.ExportBatch
    /// -logFile &lt;log&gt;</c>.
    /// </summary>
    public static class ItemModelExport
    {
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
            public int version = 1;
            public string id;
            public float baseHeight;
            public FilePart[] parts;
        }

        [MenuItem("CloneZ/Asset Pipeline/Export Hand-Built Item Models")]
        public static void ExportMenu()
        {
            int n = Export(out string dir);
            Debug.Log("[CloneZ] Exported " + n + " hand-built item models to " + dir +
                      ". The mesh stage picks them up on its next start (AssetPipeline.bat).");
            EditorUtility.RevealInFinder(dir);
        }

        /// <summary>Batch-mode entry point; exits the editor with 0 on success.</summary>
        public static void ExportBatch()
        {
            int code = 0;
            try
            {
                int n = Export(out string dir);
                Debug.Log("[CloneZ] Exported " + n + " hand-built item models to " + dir);
                if (n == 0) code = 2;
            }
            catch (Exception e)
            {
                Debug.LogError("[CloneZ] Hand-built model export failed: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        /// <summary>Writes the files and returns how many items had a hand-built model.</summary>
        public static int Export(out string dir)
        {
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "AssetPipeline", "out", "handbuilt"));
            Directory.CreateDirectory(dir);
            int n = 0;
            foreach (string id in ItemDatabase.AllIds)
            {
                ItemDef def = ItemDatabase.Get(id);
                ItemModelDesc desc = def != null ? ItemModels.Describe(def) : null;
                if (desc == null || desc.Parts.Count == 0) continue;

                var file = new FileModel { id = id, baseHeight = desc.BaseHeight, parts = new FilePart[desc.Parts.Count] };
                for (int i = 0; i < desc.Parts.Count; i++)
                {
                    Part p = desc.Parts[i];
                    // The primitives are symmetric, so a mirroring (negative) scale is the same shape as a positive one.
                    var size = new Vector3(Mathf.Abs(p.Scale.x), Mathf.Abs(p.Scale.y), Mathf.Abs(p.Scale.z));
                    // Files hold real sizes; the cylinder mesh is two units tall, so its scale.y is half the height.
                    if (p.Shape == PartShape.Cylinder) size.y *= 2f;
                    Color c = p.Mat.Color;
                    file.parts[i] = new FilePart
                    {
                        shape = p.Shape.ToString().ToLowerInvariant(),
                        pos = V(p.Position),
                        rot = V(p.Rotation.eulerAngles),
                        size = V(size),
                        color = new[] { c.r, c.g, c.b },
                        smooth = p.Mat.Smoothness,
                        metal = p.Mat.Metallic,
                    };
                }
                File.WriteAllText(Path.Combine(dir, id + ".json"), JsonUtility.ToJson(file));
                n++;
            }
            return n;
        }

        private static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    }
}
