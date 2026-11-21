#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static CloneZ.KentuckyMap;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// The exteriors' own picture harness: builds the fixed square round each place asked for (as the map harness does),
    /// notes what the building pass made of every building, and photographs examples of each kind of building up close
    /// with the game's isometric camera, by overcast day and (some) by night, front and back, plus a few of the cheap
    /// ones past the edge. Run headless: <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod
    /// CloneZ.BuildingShots.Run -clonez-out &lt;dir&gt; [-clonez-buildings-place Bardstown,Frankfort]</c> with
    /// <c>CLONEZ_WORLD</c> set. Writes the pictures and report.md, and exits by itself: 0 ok, 1 harness failure, 2 errors
    /// logged while building, 3 no world store.
    /// </summary>
    public static class BuildingShots
    {
        /// <summary>
        /// Entry point for the BuildingShots harness; parses command-line arguments and runs the image capture process.
        /// </summary>
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = Arg(args, "-clonez-out");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "BuildingShots");
            string places = Arg(args, "-clonez-buildings-place");
            int code = 1;
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ShaderUtil.allowAsyncCompilation = false;
                code = new Harness(outDir, string.IsNullOrEmpty(places) ? "Bardstown" : places, GameVolumeProfile()).Execute();
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildingShots] the harness failed to start: " + e);
                try
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllText(Path.Combine(outDir, "report.md"), "# Building shots\n\nFailed to start: " + e + "\n");
                }
                catch (Exception) { }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncShaders;
            }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// Retrieves the value for a named command-line argument if present.
        /// </summary>
        private static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : null;
        }

        /// <summary>
        /// Creates a VolumeProfile for the scene, preferring any found or falling back to the default.
        /// </summary>
        private static VolumeProfile GameVolumeProfile()
        {
            string scene = "Assets/Scenes/SampleScene.unity";
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled) { scene = s.path; break; }
            foreach (string dep in AssetDatabase.GetDependencies(scene, false))
                if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(VolumeProfile)) return AssetDatabase.LoadAssetAtPath<VolumeProfile>(dep);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        }

        /// <summary>
        /// The main harness that controls the building shot generation process.
        /// </summary>
        private sealed class Harness
        {
            private readonly string _out;
            private readonly string[] _places;
            private readonly VolumeProfile _profile;
            private readonly StringBuilder _md = new StringBuilder();
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private int _errors, _failures, _shots;
            private readonly List<string> _firstErrors = new List<string>();

            private float _isoYaw = 45f, _isoPitch = 38f, _isoClearance = 9f, _isoNear = 0.3f, _lookHeight = 1f;
            private GameObject _rig;
            private Camera _cam;
            private Light _sun;
            private ZoneDef _zone;
            private Transform _world;
            private readonly List<GameObject> _roots = new List<GameObject>();
            private readonly List<Mesh> _meshes = new List<Mesh>();
            private readonly List<Light> _lights = new List<Light>();
            private bool _cameraRender, _pathConfirmed;

            /// <summary>
            /// Constructs the harness with output directory, places list and volume profile.
            /// </summary>
            public Harness(string outDir, string places, VolumeProfile profile)
            {
                _out = outDir;
                _places = places.Split(',');
                _profile = profile;
            }

            /// <summary>
            /// Executes the harness, processing each place and generating reports.
            /// </summary>
            public int Execute()
            {
                Application.logMessageReceived += OnLog;
                int code = 0;
                try
                {
                    Directory.CreateDirectory(_out);
                    _md.Append("# Building shots\n\nThe game's isometric camera (yaw ").Append(F0(_isoYaw)).Append(", the back views from the opposite side), ")
                        .Append("overcast 10:00 or the raid's night; roofs on (edit mode: no cutaway). Map data © OpenStreetMap contributors (ODbL).\n\n");
                    ReadCamera();
                    BuildRig();
                    foreach (string raw in _places)
                    {
                        string place = raw.Trim();
                        if (place.Length == 0) continue;
                        try { Place(place); }
                        catch (Exception e)
                        {
                            _failures++;
                            _md.Append("\n**").Append(place).Append(" failed**: ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append("\n");
                            Debug.LogError("[BuildingShots] " + place + " failed: " + e);
                        }
                        finally { Discard(); }
                    }
                }
                catch (Exception e)
                {
                    _failures++;
                    Debug.LogError("[BuildingShots] the harness failed: " + e);
                }
                finally
                {
                    BuildingPass.Inspect = null;
                    if (_rig != null) Object.DestroyImmediate(_rig);
                    Application.logMessageReceived -= OnLog;
                }
                if (_failures > 0) code = 1;
                else if (_errors > 0) code = 2;
                _md.Append("\n## Log\n\n").Append(_errors).Append(" errors logged").Append(_firstErrors.Count > 0 ? ":\n\n" : ".\n");
                foreach (string e in _firstErrors) _md.Append("- `").Append(e).Append("`\n");
                _md.Append("\n**Exit ").Append(code).Append("**, ").Append(_shots).Append(" pictures, ").Append(F1(_clock.Elapsed.TotalSeconds)).Append(" s.\n");
                File.WriteAllText(Path.Combine(_out, "report.md"), _md.ToString());
                return code;
            }

            /// <summary>
            /// Handles log messages from Unity, counting errors and storing the first few.
            /// </summary>
            private void OnLog(string message, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                if (message != null && message.StartsWith("[BuildingShots]", StringComparison.Ordinal)) return;
                _errors++;
                if (_firstErrors.Count < 12) _firstErrors.Add((message ?? "").Replace('\n', ' ').Replace('`', '\''));
            }

            #region A place
            /// <summary>
            /// Processes a single place: builds the zone, records stats, and renders pictures.
            /// </summary>
            private void Place(string place)
            {
                _zone = StartZone(place);
                Climate.Winter = _zone.Winter;
                Shelter.Clear();
                StarterHome.SetActive(false);
                ViewState.Current = CameraView.Isometric;
                var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
                _world = new GameObject("World").transform;
                var noted = new List<BuildingPass.Inspected>();
                BuildingPass.Inspect = noted;
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = _meshes;
                var sw = Stopwatch.StartNew();
                RaidMapInfo info;
                string why;
                try { info = KentuckyMap.Build(_world, _zone, out why); }
                finally
                {
                    RaidMesh.CreatedMeshes = outer;
                    BuildingPass.Inspect = null;
                }
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!before.Contains(go) && go != _world.gameObject) _roots.Add(go);
                _md.Append("## ").Append(place).Append("\n\n");
                if (info == null)
                {
                    _failures++;
                    _md.Append("**KentuckyMap.Build failed**: ").Append(why).Append("\n\n");
                    return;
                }
                _md.Append("Built in ").Append(F0(sw.Elapsed.TotalMilliseconds)).Append(" ms. ").Append(KentuckyMap.LastStats).Append("\n\n");
                _lights.Clear();
                _lights.AddRange(_world.GetComponentsInChildren<Light>(true));
                foreach (GameObject go in _roots) _lights.AddRange(go.GetComponentsInChildren<Light>(true));
                Census(noted, info.HalfSize);
                Pictures(place, noted, info.HalfSize);
            }

            /// <summary>
            /// Starts the zone build process for a given place name, copying defaults if needed.
            /// </summary>
            private static ZoneDef StartZone(string place)
            {
                ZoneDef src = null;
                foreach (ZoneDef z in ZoneCatalog.All)
                    if (z.Id == "bardstown") { src = z; break; }
                var copy = new ZoneDef();
                if (src != null)
                {
                    foreach (FieldInfo f in typeof(ZoneDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (!f.IsInitOnly) f.SetValue(copy, f.GetValue(src));
                }
                else
                {
                    copy.Id = "bardstown";
                    copy.RealPlace = "Bardstown";
                    copy.LayoutSeed = 1780;
                }
                copy.RealStatewide = false;
                copy.RealPlace = place;
                return copy;
            }

            /// <summary>
            /// Destroys all temporary objects and meshes created during the harness run.
            /// </summary>
            /// <summary>
            /// Disposes of temporary objects, meshes, and lighting after a place is processed.
            /// </summary>
            private void Discard()
            {
                foreach (GameObject go in _roots)
                    if (go != null) Object.DestroyImmediate(go);
                _roots.Clear();
                if (_world != null) Object.DestroyImmediate(_world.gameObject);
                _world = null;
                foreach (Mesh m in _meshes)
                    if (m != null) Object.DestroyImmediate(m);
                _meshes.Clear();
                _lights.Clear();
                CeilingVisibility.Clear();
                Shelter.Clear();
            }

            // What the pass made of the place's buildings: how many of each kind, inside the square and past it.
            /// <summary>
            /// Records building statistics for a given place and writes them to the markdown report.
            /// </summary>
            /// <summary>
            /// Records building statistics for a place and writes them to the markdown report.
            /// </summary>
            private void Census(List<BuildingPass.Inspected> noted, float half)
            {
                var inside = new SortedDictionary<string, int>(StringComparer.Ordinal);
                var outside = new SortedDictionary<string, int>(StringComparer.Ordinal);
                var storeys = new SortedDictionary<int, int>();
                int edge = 0, enterable = 0;
                foreach (BuildingPass.Inspected b in noted)
                {
                    SortedDictionary<string, int> d = b.Scenery ? outside : inside;
                    d.TryGetValue(b.Label, out int n);
                    d[b.Label] = n + 1;
                    if (b.Edge) edge++;
                    if (b.Scenery) continue;
                    storeys.TryGetValue(b.Storeys, out int s);
                    storeys[b.Storeys] = s + 1;
                    if (b.Enterable) enterable++;
                }
                _md.Append("| what | inside the square | past its edge |\n|---|--:|--:|\n");
                var keys = new SortedSet<string>(inside.Keys, StringComparer.Ordinal);
                keys.UnionWith(outside.Keys);
                foreach (string k in keys)
                {
                    inside.TryGetValue(k, out int a);
                    outside.TryGetValue(k, out int b);
                    _md.Append("| ").Append(k).Append(" | ").Append(a).Append(" | ").Append(b).Append(" |\n");
                }
                _md.Append("\nStoreys inside: ");
                foreach (KeyValuePair<int, int> kv in storeys) _md.Append(kv.Key).Append(": ").Append(kv.Value).Append(", ");
                _md.Append("enterable ").Append(enterable).Append(", the edge's (solid, shut) ").Append(edge).Append(".\n\n");
            }
            #endregion

            #region Pictures
            // The kinds worth a look, in the order they're photographed (the budget runs out at the end of the list).
            private static readonly string[] Order =
            {
                "Row", "Store", "Office", "Church", "Courthouse", "Civic", "School", "Hospital", "Apartments", "Motel",
                "House/Victorian", "House/Foursquare", "House/TwoStorey", "House/Farmhouse", "House/Bungalow", "House/Ranch",
                "House/Cottage", "House/Cape", "House/Shotgun", "Trailer", "Warehouse", "Industrial", "Rickhouse", "Parking",
                "Canopy", "Barn", "TobaccoBarn", "Silo", "Shed", "Garage", "Greenhouse", "Ruin",
            };

            /// <summary>
            /// Generates picture filenames, ordering, and calls Shot for each building type.
            /// </summary>
            /// <summary>
            /// Generates picture filenames, ordering, and calls Shot for each building type.
            /// </summary>
            private void Pictures(string place, List<BuildingPass.Inspected> noted, float half)
            {
                _md.Append("| file | building | what the pass made of it |\n|---|---|---|\n");
                string tag = Safe(place);
                // A wide look at the middle of the place first.
                Shot(tag + "_00_middle_day.png", Vector3.zero, 26f, _isoYaw, false, "the middle of " + place + " (wide)");
                Shot(tag + "_00_middle_night.png", Vector3.zero, 26f, _isoYaw, true, "the middle of " + place + " by night");
                int budget = 34;
                foreach (string kind in Order)
                {
                    if (budget <= 0) break;
                    BuildingPass.Inspected best = default;
                    float bestScore = float.MinValue;
                    bool found = false;
                    foreach (BuildingPass.Inspected b in noted)
                    {
                        if (b.Scenery || b.Label != kind) continue;
                        // The most telling example: the biggest that isn't huge, nearest the middle.
                        float size = Mathf.Min(b.F.Size, 2500f);
                        float score = size * 0.02f - b.F.Middle.magnitude * 0.02f + b.Storeys * 2f;
                        if (score <= bestScore) continue;
                        bestScore = score;
                        best = b;
                        found = true;
                    }
                    if (!found) continue;
                    Subject(tag, best, kind, ref budget);
                }
                // The cheap ones past the edge: two of them.
                int far = 0;
                foreach (BuildingPass.Inspected b in noted)
                {
                    if (!b.Scenery || b.Edge || b.F.Size < 60f || far >= 2) continue;
                    if (Mathf.Abs(b.F.Middle.x) > half + 40f || Mathf.Abs(b.F.Middle.y) > half + 40f) continue;
                    far++;
                    Subject(tag, b, "lite_" + b.Label.Replace('/', '_'), ref budget);
                }
                foreach (BuildingPass.Inspected b in noted)
                {
                    if (!b.Edge) continue;
                    Subject(tag, b, "edge_" + b.Label.Replace('/', '_'), ref budget);
                    break;
                }
                _md.Append('\n');
            }

            private void Subject(string tag, BuildingPass.Inspected b, string kind, ref int budget)
            {
                Rect r = b.F.Bounds;
                float extent = Mathf.Max(r.width, r.height);
                float size = Mathf.Clamp(extent * 0.55f + b.Top * 0.35f, 6f, 46f);
                Vector3 at = To3(b.F.Middle, 0f);
                string what = b.Describe();
                string name = tag + "_" + kind.Replace('/', '_');
                Shot(name + "_day.png", at, size, _isoYaw, false, what);
                budget--;
                if (budget > 0)
                {
                    Shot(name + "_back.png", at, size, _isoYaw + 180f, false, what + " (from behind)");
                    budget--;
                }
                bool night = kind == "Row" || kind == "Store" || kind.StartsWith("House", StringComparison.Ordinal) || kind == "Church"
                    || kind == "Apartments" || kind == "Office";
                if (night && budget > 0)
                {
                    Shot(name + "_night.png", at, size, _isoYaw, true, what + " (night)");
                    budget--;
                }
            }

            /// <summary>
            /// Renders a single shot image using the configured camera and lighting.
            /// </summary>
            private void Shot(string file, Vector3 at, float size, float yaw, bool night, string what)
            {
                try
                {
                    Light(night, at);
                    Quaternion rot = Quaternion.Euler(_isoPitch, yaw, 0f);
                    Vector3 focus = at + Vector3.up * _lookHeight;
                    float distance = size / Mathf.Tan(Mathf.Clamp(_isoPitch, 15f, 89f) * Mathf.Deg2Rad) + _isoClearance + 30f;
                    _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
                    _cam.orthographic = true;
                    _cam.orthographicSize = size;
                    _cam.nearClipPlane = _isoNear;
                    _cam.farClipPlane = 500f;
                    Texture2D tex = Capture(1400, 900);
                    try { File.WriteAllBytes(Path.Combine(_out, file), tex.EncodeToPNG()); }
                    finally { Object.DestroyImmediate(tex); }
                    _shots++;
                    _md.Append("| ").Append(file).Append(" | ").Append(what).Append(" |\n");
                }
                catch (Exception e)
                {
                    _failures++;
                    Debug.LogError("[BuildingShots] picture " + file + " failed: " + e);
                }
            }
            #endregion

            #region The rig (as the cutaway harness's)
            /// <summary>
            /// Reads the camera configuration from a temporary GameObject.
            /// </summary>
            private void ReadCamera()
            {
                var go = new GameObject("[BuildingShots] camera numbers");
                go.SetActive(false);
                try
                {
                    var f = go.AddComponent<IsoCameraFollow>();
                    _isoYaw = f.IsoYaw;
                    _isoPitch = f.IsoPitch;
                    _isoClearance = f.IsoClearance;
                    _isoNear = f.IsoNearClip;
                    _lookHeight = f.LookHeight;
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            /// <summary>
            /// Constructs the rig GameObject hierarchy with camera and lighting for image capture.
            /// </summary>
            private void BuildRig()
            {
                _rig = new GameObject("[BuildingShots]");
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(_rig.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.allowHDR = true;
                _cam.allowMSAA = false;
                UniversalAdditionalCameraData data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                var sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(_rig.transform, false);
                _sun = sunGo.AddComponent<Light>();
                _sun.type = LightType.Directional;
                _sun.shadows = LightShadows.Soft;
                _sun.useColorTemperature = true;
                _sun.colorTemperature = 5000f;
                RenderSettings.sun = _sun;
                if (_profile != null)
                {
                    var volGo = new GameObject("Game Volume");
                    volGo.transform.SetParent(_rig.transform, false);
                    var volume = volGo.AddComponent<Volume>();
                    volume.isGlobal = true;
                    volume.sharedProfile = _profile;
                }
            }

            // The game's overcast day (10:00) or the raid's night (the zone's start hour), by the sums the map harness uses.
            /// <summary>
            /// Configures lighting based on time of day and weather for a shot.
            /// </summary>
            private void Light(bool night, Vector3 near)
            {
                float hour = night ? _zone.StartHour : 10f;
                const float cloud = 0.7f;
                float elevation = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
                float weatherDim = Mathf.Lerp(1f, 0.5f, cloud);
                float level = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);
                float dark = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.55f, level));
                float ambientScale = Mathf.Lerp(1f, 0.25f, dark), moonScale = Mathf.Lerp(1f, 0.3f, dark);
                float warmth = Mathf.Clamp01(elevation * 1.6f);
                _sun.transform.rotation = Quaternion.Euler((hour - 6f) / 12f * 180f, -35f + hour / 24f * 30f, 0f);
                _sun.color = Color.Lerp(new Color(1f, 0.62f, 0.36f), new Color(1f, 0.96f, 0.86f), warmth);
                _sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * moonScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                _sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
                Color ambient = Color.Lerp(new Color(0.07f, 0.08f, 0.13f), new Color(0.40f, 0.42f, 0.47f), daylight)
                    * Mathf.Lerp(0.7f, 1f, weatherDim) * ambientScale;
                _cam.backgroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), new Color(0.30f, 0.42f, 0.55f), daylight)
                    * Mathf.Lerp(0.6f, 1f, weatherDim) * ambientScale;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = false;
                foreach (Light l in _lights)
                {
                    if (l == null) continue;
                    l.enabled = night && (l.transform.position - near).sqrMagnitude < 50f * 50f;
                    l.shadows = LightShadows.None;
                }
            }

            /// <summary>
            /// Captures a RenderTexture into a Texture2D of given dimensions.
            /// </summary>
            private Texture2D Capture(int w, int h)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[BuildingShots]", antiAliasing = 1 };
                rt.Create();
                RenderTexture active = RenderTexture.active;
                try
                {
                    RenderInto(rt);
                    RenderInto(rt);
                    Texture2D tex = Read(rt, w, h);
                    if (!_pathConfirmed)
                    {
                        if (Uniform(tex))
                        {
                            _cameraRender = !_cameraRender;
                            Object.DestroyImmediate(tex);
                            RenderInto(rt);
                            RenderInto(rt);
                            tex = Read(rt, w, h);
                        }
                        _pathConfirmed = true;
                    }
                    return tex;
                }
                finally
                {
                    RenderTexture.active = active;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

            /// <summary>
            /// Reads pixel data from a RenderTexture into a new Texture2D.
            /// </summary>
            private static Texture2D Read(RenderTexture rt, int w, int h)
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false);
                return tex;
            }

            /// <summary>
            /// Determines if a texture is uniformly colored, used to detect rendering issues.
            /// </summary>
            private static bool Uniform(Texture2D tex)
            {
                Color32 first = tex.GetPixel(0, 0);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        Color32 p = tex.GetPixel(x * tex.width / 16 + 3, y * tex.height / 16 + 3);
                        if (p.r != first.r || p.g != first.g || p.b != first.b) return false;
                    }
                return true;
            }

            /// <summary>
            /// Renders the current view into the provided RenderTexture, handling custom pipelines.
            /// </summary>
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
                    _cam.targetTexture = rt;
                    try { _cam.Render(); }
                    finally { _cam.targetTexture = null; }
                }
                finally
                {
                    _cam.enabled = false;
                }
            }
            #endregion

            /// <summary>
            /// Sanitizes a string for use in file names, converting non-alphanumerics to underscores.
            /// </summary>
            private static string Safe(string s)
            {
                var b = new StringBuilder(s.Length);
                foreach (char c in s) b.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
                return b.ToString();
            }

            /// <summary>
            /// Formats a double as an integer with thousands separators for markdown output.
            /// </summary>
            private static string F0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);
            /// <summary>
            /// Formats a double with one decimal place for markdown output.
            /// </summary>
            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    internal static partial class BuildingPass
    {
        /// <summary>While set (by <see cref="BuildingShots"/>), every building the pass raises is noted here.</summary>
        internal static List<Inspected> Inspect;

        /// <summary>What the pass made of one building.</summary>
        internal struct Inspected
        {
            public Footprint F;
            public Arch Arch;
            public HouseStyle Style;
            public RoofKind Roof;
            public WallKind Wall;
            public int Storeys;
            public float Top;
            public bool Scenery, Edge, Lite, Enterable;
            public string Use, Name;

            public string Label => Arch == Arch.House ? "House/" + Style : Arch.ToString();

            public string Describe() => (F.Data.kind ?? "?") + " " + F.Data.id + (string.IsNullOrEmpty(Name) ? "" : " \"" + Name + "\"")
                + (string.IsNullOrEmpty(Use) ? "" : " (" + Use + ")") + ", " + Mathf.RoundToInt(F.Size) + " m² | " + Label + ", " + Storeys
                + " storeys, " + Roof + " roof, " + Wall + ", top " + Top.ToString("0.0", CultureInfo.InvariantCulture) + " m"
                + (Lite ? ", Lite" : "") + (Edge ? ", at the edge" : "") + (Enterable ? ", enterable" : "");
        }

        private static void Note(Job j)
        {
            Plan p = j.P;
            Inspect.Add(new Inspected
            {
                F = j.F, Arch = p.Arch, Style = p.Style, Roof = p.Roof, Wall = p.Wall, Storeys = p.Storeys, Top = j.Top,
                Scenery = j.Scenery, Edge = j.Edge, Lite = j.Lite, Enterable = j.Record != null, Use = p.Use, Name = p.Name,
            });
        }
    }
}
#endif
