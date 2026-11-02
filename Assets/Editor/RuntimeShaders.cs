using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// The game makes its materials at runtime with <c>Shader.Find</c>, and a player build carries a shader only when
    /// something in the build uses it, so in a build <c>Shader.Find</c> returns null and the world can't be made. This
    /// keeps a material in <see cref="Folder"/> (a Resources folder, so every build has them) for each shader the scripts
    /// find by name, plus one for each variant the scripts switch on at runtime (transparent, alpha-clipped, emissive), so
    /// a build has those variants too. CloneZBatch's builds bring it up to date first; CloneZ ▸ Build ▸ Update Runtime
    /// Shaders does it by hand.
    /// </summary>
    public static class RuntimeShaders
    {
        public const string Folder = "Assets/Resources/RuntimeShaders";

        // The variants scripts switch on at runtime, by URP's names (Lit, Unlit, Particles); one is kept for a shader that
        // has the property.
        private static readonly string[,] Variants =
        {
            { "Transparent", "_Surface", "_SURFACE_TYPE_TRANSPARENT" },
            { "AlphaClip", "_AlphaClip", "_ALPHATEST_ON" },
            { "Emission", "_EmissionColor", "_EMISSION" },
        };

        [MenuItem("CloneZ/Build/Update Runtime Shaders")]
        private static void UpdateFromMenu() => Debug.Log("[RuntimeShaders] " + Ensure() + " material(s) made or updated in " + Folder + ".");

        /// <summary>Makes sure every shader the scripts find by name has its materials; returns how many it made or mended.</summary>
        public static int Ensure()
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (Match m in Regex.Matches(File.ReadAllText(file), "Shader\\.Find\\(\\s*\"([^\"]+)\"\\s*\\)"))
                    names.Add(m.Groups[1].Value);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
            int made = 0;
            foreach (string name in names)
            {
                Shader shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning("[RuntimeShaders] The scripts look for a shader called '" + name + "', and there's none.");
                    continue;
                }
                made += Keep(shader, name, null, null, null);
                for (int i = 0; i < Variants.GetLength(0); i++)
                    if (shader.FindPropertyIndex(Variants[i, 1]) >= 0) made += Keep(shader, name, Variants[i, 0], Variants[i, 1], Variants[i, 2]);
            }
            if (made > 0) AssetDatabase.SaveAssets();
            return made;
        }

        private static int Keep(Shader shader, string name, string suffix, string property, string keyword)
        {
            string path = Folder + "/" + name.Replace('/', '-') + (suffix != null ? " " + suffix : "") + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool emissive = property == "_EmissionColor";
            if (mat != null && mat.shader == shader && (keyword == null || mat.IsKeywordEnabled(keyword))
                && (!emissive || mat.globalIlluminationFlags == MaterialGlobalIlluminationFlags.RealtimeEmissive)) return 0;
            bool fresh = mat == null;
            if (fresh) mat = new Material(shader);
            else mat.shader = shader;
            if (emissive)
            {
                mat.SetColor(property, Color.white);
                // Emission flagged black (a new material's default) has the shader GUIs switch the keyword back off.
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else if (property != null) mat.SetFloat(property, 1f);
            if (keyword != null) mat.EnableKeyword(keyword);
            if (fresh) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return 1;
        }
    }
}
