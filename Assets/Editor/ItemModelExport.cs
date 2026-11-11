using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Writes every hand-built item model (<see cref="ItemModels"/>) to Tools/AssetPipeline/out/handbuilt/&lt;id&gt;.json
    /// in the generated models' file format, so the asset pipeline's mesh stage can fit those items' meshes onto them,
    /// as it does with the generated ones (the pose and size of a held item come from its model), and beside them
    /// <see cref="CatalogueFileName"/>, those items as the pipeline's catalogue describes them, which brings in the ones only
    /// code registers. Run it from the menu once, and again after changing or adding a hand-built model; or headless:
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

        /// <summary>One hand-built item as the pipeline's catalogue (catalog.py) describes an item to the models.</summary>
        [Serializable]
        private sealed class CatalogueItem
        {
            public string id;
            public string name;
            public string category;
            public float weight;
            public float[] tint;
            public string description;
            public string[] traits;
        }

        [Serializable]
        private sealed class CatalogueFile
        {
            public int version = 1;
            public CatalogueItem[] items;
        }

        /// <summary>The catalogue of the hand-built items written beside their models. The pipeline reads its items from the
        /// content database's INSERT scripts; this adds the ones only code registers (ZoneContent's guns), so the mesh stage
        /// can make them too.</summary>
        public const string CatalogueFileName = "catalogue.json";

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
            var catalogue = new System.Collections.Generic.List<CatalogueItem>();
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
                catalogue.Add(Describe(def));
                n++;
            }
            File.WriteAllText(Path.Combine(dir, CatalogueFileName),
                JsonUtility.ToJson(new CatalogueFile { items = catalogue.ToArray() }, true));
            return n;
        }

        // catalog.py's wording for each ClothingSlot, in the enum's order, which meshgen.py reads to tell worn things apart.
        private static readonly string[] SlotTraits =
        {
            "Clothing worn on the head", "Clothing worn on the torso", "Clothing worn on the legs", "Clothing worn on the feet",
            "Clothing worn on the hands", "Backpack worn on the back", "Chest rig (load-bearing vest with pouches)",
            "Thigh bag strapped to the leg", "Head-mounted eyewear or optics", "Mask worn over the face",
            "Body armour (vest or plate carrier)", "Small secure pouch",
        };

        // The item as catalog.py phrases one for the models: its category's name and the traits that shape it.
        private static CatalogueItem Describe(ItemDef def)
        {
            var traits = new System.Collections.Generic.List<string>();
            if (def.IsClothing)
            {
                int slot = (int)def.ClothingSlot;
                traits.Add(slot >= 0 && slot < SlotTraits.Length ? SlotTraits[slot] : "Clothing");
            }
            if (def.IsWeapon)
                traits.Add("Melee weapon (" + (def.TwoHanded ? "two-handed, " : "") + "reach " + def.WeaponRange.ToString("0.##") + " m)");
            if (def.IsFirearm)
            {
                string kind = def.TwoHanded ? "long gun" : "handgun";
                string cal = CalibreName(Ballistics.CaliberOfGun(def));
                traits.Add("Firearm (" + kind + ", magazine " + def.MagazineSize + (def.PelletsPerShot > 1 ? ", shotgun" : "") +
                           (string.IsNullOrEmpty(cal) ? "" : ", chambered in " + cal) + ")");
            }
            if (def.MaxStack > 1) traits.Add("Stacks to " + def.MaxStack);
            Color t = def.Tint;
            return new CatalogueItem
            {
                id = def.Id,
                name = string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName,
                category = def.Category.ToString(),
                weight = def.Weight,
                tint = new[] { t.r, t.g, t.b },
                description = def.Description ?? "",
                traits = traits.ToArray(),
            };
        }

        private static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };

        // A calibre as people write it (the ballistics tables key them shorter), for the models describing the gun.
        private static string CalibreName(string caliber)
        {
            switch (caliber)
            {
                case "9x19": return "9x19mm";
                case "12g": return "12 gauge";
                case "545": return "5.45x39mm";
                case "556": return "5.56x45mm";
                case "762r": return "7.62x54R";
                case "38sp": return ".38 Special";
                case "45acp": return ".45 ACP";
                case "22lr": return ".22 Long Rifle";
                case "3006": return ".30-06 Springfield";
                case "3030": return ".30-30 Winchester";
                case "flare": return "signal flares";
                default: return caliber;
            }
        }
    }
}
