using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// A contact sheet of the generated item meshes (<see cref="GeneratedItemMeshes"/>) as the game draws them: each
    /// tile shows the item's old primitive model on the left and each of its generated versions beside it, hovering at
    /// their base heights over the floor, lit like an overcast day. It loads the meshes through the game's own loader,
    /// with their normal and metallic maps, so a broken file, a missing texture or a mesh turned the wrong way shows up
    /// here first. From the menu (CloneZ > Asset Pipeline > Compare Item Meshes), or headless:
    /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.ItemMeshShots.RunBatch
    /// -clonez-itemshots &lt;outDir&gt; [-clonez-itemshots-ids A,B,C] -logFile &lt;log&gt;</c>.
    /// Writes items.png (the sheet) and items.md (tile order and any problems). Exit code: 0 ok, 1 harness failure,
    /// 2 problems logged while loading, 3 no generated meshes.
    /// </summary>
    public static class ItemMeshShots
    {
        private const int Tile = 600;
        private const int Columns = 3;
        private const int MaxItems = 48;

        [MenuItem("CloneZ/Asset Pipeline/Compare Item Meshes")]
        public static void RunMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "ItemMeshShots"));
            int code = Run(outDir, null);
            Debug.Log("[ItemMeshShots] finished with code " + code + "; see " + outDir);
            if (code != 3) EditorUtility.RevealInFinder(Path.Combine(outDir, "items.png"));
        }

        public static void RunBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = ArgValue(args, "-clonez-itemshots") ?? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "ItemMeshShots");
            string ids = ArgValue(args, "-clonez-itemshots-ids");
            int code;
            try
            {
                code = Run(outDir, string.IsNullOrEmpty(ids) ? null : ids.Split(','));
            }
            catch (Exception e)
            {
                Debug.LogError("[ItemMeshShots] " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        /// <summary>Renders the sheet for <paramref name="ids"/> (null: every item with a generated mesh, up to 48).</summary>
        public static int Run(string outDir, IList<string> ids)
        {
            Directory.CreateDirectory(outDir);
            var problems = new List<string>();
            Application.LogCallback onLog = (msg, trace, type) =>
            {
                if (type != LogType.Log && msg.Contains("Generated mesh")) problems.Add(msg);
            };

            string folder = Path.Combine(Application.dataPath, "Resources", GeneratedItemMeshes.ResourceFolder);
            if (ids == null)
            {
                // Files are <id>@<version>.bytes; one tile per item shows all of its versions.
                ids = Directory.Exists(folder)
                    ? Directory.GetFiles(folder, "*@*.bytes").Select(f => Path.GetFileNameWithoutExtension(f))
                        .Select(s => s.Substring(0, s.LastIndexOf('@'))).Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).Take(MaxItems).ToList()
                    : new List<string>();
            }
            if (ids.Count == 0)
            {
                Debug.LogWarning("[ItemMeshShots] no generated item meshes in " + folder);
                return 3;
            }

            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;   // never a picture of a shader still compiling
            Application.logMessageReceived += onLog;
            ItemMeshMaterialSetup.Ensure();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GeneratedItemMeshes.Clear();   // freshly pulled files, not what an earlier run cached
            var rig = new Rig();
            var md = new StringBuilder("# Generated item meshes\n\nEach tile: the old primitive model (left), then each generated version.\n\n");
            var tiles = new List<Texture2D>();
            try
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i].Trim();
                    ItemDef def = ItemDatabase.Get(id);
                    int count = def != null ? GeneratedItemMeshes.VariantCount(def) : 0;
                    var meshes = new List<ItemMeshBaker.Baked>(count);
                    for (int v = 0; v < count; v++) meshes.Add(GeneratedItemMeshes.Get(def, ItemMeshBaker.Lod.Near, v));
                    ItemModelDesc old = def != null ? (ItemModels.Describe(def) ?? GeneratedItemModels.Describe(def)) : null;
                    string note = def == null ? "not in the catalogue" : count == 0 ? "no generated mesh" : null;
                    if (note != null) problems.Add(id + ": " + note);
                    md.Append(i + 1).Append(". `").Append(id).Append('`');
                    foreach (ItemMeshBaker.Baked mesh in meshes)
                    {
                        bool maps = mesh.Materials[0].IsKeywordEnabled("_NORMALMAP");
                        md.Append(" — ").Append(mesh.VertexCount).Append(" verts, ").Append(mesh.Mesh.GetIndexCount(0) / 3).Append(" tris, size ")
                          .Append(mesh.Bounds.size.ToString("F3")).Append(" m, base ").Append(mesh.BaseHeight.ToString("F3"))
                          .Append(maps ? ", normal + metallic maps" : ", **no maps**");
                        if (!maps) problems.Add(id + ": a version is drawn without its normal and metallic maps");
                    }
                    if (note != null) md.Append(" — **").Append(note).Append("**");
                    md.Append('\n');
                    tiles.Add(rig.Shoot(old, meshes));
                }

                int rows = (tiles.Count + Columns - 1) / Columns;
                var sheet = new Texture2D(Tile * Columns, Tile * rows, TextureFormat.RGB24, false);
                var fill = new Color32[sheet.width * sheet.height];
                for (int p = 0; p < fill.Length; p++) fill[p] = new Color32(30, 31, 36, 255);
                sheet.SetPixels32(fill);
                for (int t = 0; t < tiles.Count; t++)
                {
                    int x = t % Columns * Tile, y = (rows - 1 - t / Columns) * Tile;   // first tile top-left
                    sheet.SetPixels(x, y, Tile, Tile, tiles[t].GetPixels());
                }
                sheet.Apply(false);
                File.WriteAllBytes(Path.Combine(outDir, "items.png"), sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);

                if (problems.Count > 0)
                {
                    md.Append("\n## Problems\n\n");
                    foreach (string p in problems) md.Append("- ").Append(p).Append('\n');
                }
                File.WriteAllText(Path.Combine(outDir, "items.md"), md.ToString());
                return problems.Count > 0 ? 2 : 0;
            }
            finally
            {
                foreach (Texture2D t in tiles) Object.DestroyImmediate(t);
                rig.Dispose();
                Application.logMessageReceived -= onLog;
                ShaderUtil.allowAsyncCompilation = asyncShaders;
            }
        }

        private static string ArgValue(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : null;
        }

        /// <summary>Camera, sun, floor, and the two models of the tile being shot.</summary>
        private sealed class Rig : IDisposable
        {
            private readonly GameObject _root;
            private readonly Camera _cam;
            private bool _cameraRender;
            private GameObject _models;

            public Rig()
            {
                _root = new GameObject("[ItemMeshShots]");
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(_root.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                _cam.orthographic = true;
                _cam.allowMSAA = false;
                UniversalAdditionalCameraData data = _cam.GetUniversalAdditionalCameraData();
                data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

                var sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(_root.transform, false);
                sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Light sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.intensity = 1.1f;
                RenderSettings.sun = sun;
                Color ambient = new Color(0.42f, 0.44f, 0.48f);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = false;

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.name = "Floor";
                floor.transform.SetParent(_root.transform, false);
                floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
                floor.transform.localScale = new Vector3(40f, 40f, 1f);
                floor.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.36f, 0.35f, 0.33f), 0.05f);
            }

            /// <summary>One tile: the old model on the left, then each generated version in a row along +X, each at its
            /// own hover height.</summary>
            public Texture2D Shoot(ItemModelDesc old, IList<ItemMeshBaker.Baked> meshes)
            {
                if (_models != null) Object.DestroyImmediate(_models);
                _models = new GameObject("Models");
                _models.transform.SetParent(_root.transform, false);

                float size = old != null ? old.Bounds.size.magnitude : 0f;
                foreach (ItemMeshBaker.Baked m in meshes) size = Mathf.Max(size, m.Bounds.size.magnitude);
                float pitch = Mathf.Max(size * 1.1f, 0.1f);
                int count = 1 + meshes.Count;
                float x0 = -pitch * (count - 1) / 2f;
                if (old != null) BuildPrimitives(old, new Vector3(x0, old.BaseHeight, 0f));
                for (int i = 0; i < meshes.Count; i++)
                {
                    var go = new GameObject("Generated " + (i + 1));
                    go.transform.SetParent(_models.transform, false);
                    go.transform.localPosition = new Vector3(x0 + pitch * (i + 1), meshes[i].BaseHeight, 0f);
                    go.AddComponent<MeshFilter>().sharedMesh = meshes[i].Mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = meshes[i].Materials;
                }

                float span = pitch * (count - 1) + size;
                Quaternion rot = Quaternion.Euler(38f, -25f, 0f);
                Vector3 focus = new Vector3(0f, size * 0.25f, 0f);
                _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * (span * 4f + 2f), rot);
                _cam.orthographicSize = span * 0.36f;
                _cam.nearClipPlane = 0.01f;
                _cam.farClipPlane = span * 8f + 10f;
                return Capture();
            }

            private void BuildPrimitives(ItemModelDesc desc, Vector3 at)
            {
                var root = new GameObject("Old");
                root.transform.SetParent(_models.transform, false);
                root.transform.localPosition = at;
                foreach (Part p in desc.Parts)
                {
                    PrimitiveType type = p.Shape == PartShape.Sphere ? PrimitiveType.Sphere
                        : p.Shape == PartShape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                    GameObject g = GameObject.CreatePrimitive(type);
                    Object.DestroyImmediate(g.GetComponent<Collider>());
                    g.transform.SetParent(root.transform, false);
                    g.transform.localPosition = p.Position;
                    g.transform.localRotation = p.Rotation;
                    g.transform.localScale = p.Scale;
                    g.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(p.Mat.Color, p.Mat.Smoothness, p.Mat.Metallic);
                }
            }

            private Texture2D Capture()
            {
                var rt = new RenderTexture(Tile, Tile, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[ItemMeshShots]" };
                rt.Create();
                RenderTexture active = RenderTexture.active;
                try
                {
                    // Twice: the first frame of a new view settles anything first-use (shader variants).
                    RenderInto(rt);
                    RenderInto(rt);
                    RenderTexture.active = rt;
                    var tex = new Texture2D(Tile, Tile, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, Tile, Tile), 0, 0, false);
                    tex.Apply(false);
                    return tex;
                }
                finally
                {
                    RenderTexture.active = active;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

            private void RenderInto(RenderTexture rt)
            {
                _cam.enabled = true;
                try
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (!_cameraRender && RenderPipeline.SupportsRenderRequest(_cam, request))
                    {
                        RenderPipeline.SubmitRenderRequest(_cam, request);
                        return;
                    }
                    _cameraRender = true;
                    _cam.targetTexture = rt;
                    try { _cam.Render(); }
                    finally { _cam.targetTexture = null; }
                }
                finally
                {
                    _cam.enabled = false;
                }
            }

            public void Dispose()
            {
                if (_root != null) Object.DestroyImmediate(_root);
            }
        }
    }
}
