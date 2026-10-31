using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Import settings for the generated item meshes' textures (Assets/Resources/GeneratedItemMeshes, see
    /// <see cref="GeneratedItemMeshes"/>): each level's base colour (<c>_tex0</c>, <c>_tex1</c>) as sRGB colour, the
    /// near level's normal map (<c>_nrm0</c>) as a normal map and its metallic map (<c>_msk0</c>: metallic in red,
    /// smoothness in alpha) as linear data with its alpha kept. All get mipmaps for the floor at a distance and are
    /// clamped at the atlas edge; the caps sit well above what the pipeline bakes, so a bigger bake still fits.
    /// </summary>
    internal sealed class GeneratedItemMeshImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/" + GeneratedItemMeshes.ResourceFolder + "/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.isReadable = false;
            ti.textureCompression = TextureImporterCompression.Compressed;

            if (name.EndsWith("_nrm0", StringComparison.OrdinalIgnoreCase))
            {
                ti.textureType = TextureImporterType.NormalMap;   // linear, and packed the way URP's normal sampling expects
                ti.maxTextureSize = 2048;
            }
            else if (name.EndsWith("_msk0", StringComparison.OrdinalIgnoreCase))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = false;
                ti.maxTextureSize = 1024;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.maxTextureSize = name.EndsWith("_tex0", StringComparison.OrdinalIgnoreCase) ? 2048 : 512;
            }
        }
    }

    /// <summary>
    /// Makes Resources/ItemMeshes/Template.mat (<see cref="GeneratedItemMeshes.TemplatePath"/>), the material every
    /// generated item mesh's near level is cloned from: URP Lit, metallic workflow, with a normal map and a metallic map.
    /// It carries flat placeholder maps of its own (FlatNormal, FlatMask), so URP's material validation keeps both
    /// keywords on, and being an asset under Resources it keeps those shader variants in a player build, which a
    /// material made only at runtime would lose to variant stripping. Made when the editor loads if it is missing;
    /// CloneZ > Asset Pipeline > Recreate Item Mesh Material remakes it.
    /// </summary>
    [InitializeOnLoad]
    internal static class ItemMeshMaterialSetup
    {
        private const string Folder = "Assets/Resources/ItemMeshes";
        private const string MaterialPath = Folder + "/Template.mat";
        private const string NormalPath = Folder + "/FlatNormal.png";
        private const string MaskPath = Folder + "/FlatMask.png";

        static ItemMeshMaterialSetup()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += () => Ensure();
        }

        [MenuItem("CloneZ/Asset Pipeline/Recreate Item Mesh Material")]
        private static void Recreate()
        {
            AssetDatabase.DeleteAsset(MaterialPath);
            Ensure();
        }

        /// <summary>Makes the template (and its placeholder maps) if it is missing; returns it, or null when URP's Lit
        /// shader isn't there.</summary>
        internal static Material Ensure()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogWarning("[CloneZ] URP's Lit shader wasn't found, so the item mesh material wasn't made.");
                return null;
            }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "ItemMeshes");

            Texture2D normal = Placeholder(NormalPath, new Color32(128, 128, 255, 255), true);
            Texture2D mask = Placeholder(MaskPath, new Color32(0, 0, 0, 51), false);   // not metal, smoothness 0.2

            var m = new Material(lit) { name = "Template", enableInstancing = true };
            m.SetFloat("_WorkflowMode", 1f);   // metallic
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.SetTexture("_MetallicGlossMap", mask);
            m.SetFloat("_Smoothness", 1f);     // multiplies the map's alpha, so the map alone decides
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.EnableKeyword("_NORMALMAP");
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(m, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CloneZ] Made " + MaterialPath + " for the generated item meshes.");
            return m;
        }

        private static Texture2D Placeholder(string path, Color32 colour, bool normalMap)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false, linear: true);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = colour;
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
