#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// The map harness behind <c>CloneZ.EditorTools.CloneZBatch.MapShots</c> (see there for the command line). In an
    /// empty edit-mode scene it builds the Kentucky map's start square the way a raid does (<see cref="KentuckyMap.Build"/>
    /// on a copy of the bardstown zone with <see cref="ZoneDef.RealStatewide"/> off: the fixed square and its scenery ring,
    /// built at once), photographs it the way the game shows it (a top-down overview, the isometric camera at spots picked
    /// from the map data, two shoulder-height views; by night as a raid starts and in plain overcast daylight), times the
    /// build pass by pass, builds single streamed chunks across the state the way <see cref="KentuckyStreamer"/> does,
    /// and writes the pictures, report.json and report.md. Every stage and every picture is fenced, so one failure never
    /// loses the rest, and the report is always written. Everything that touches the map's internals is in the MapApi
    /// region at the bottom: fix it there when they're renamed.
    /// </summary>
    public static class MapShots
    {
        public const int ExitOk = 0, ExitHarness = 1, ExitMap = 2, ExitNoWorld = 3;

        /// <summary>What to run (from the command line, see CloneZBatch.MapShots).</summary>
        public sealed class Options
        {
            public string OutDir;
            /// <summary>The place to build the start square around; null for the bardstown zone's own.</summary>
            public string Place;
            /// <summary>A handful of pictures, no pass profile, no chunk tests.</summary>
            public bool Quick;
            /// <summary>The game scene's Global Volume profile (its bloom, tonemapping and vignette).</summary>
            public VolumeProfile GameProfile;
        }

        /// <summary>Runs the harness; returns the process exit code (see the Exit constants).</summary>
        public static int Run(Options options) => new Harness(options).Execute();

        /// <summary>A report saying the harness couldn't even start, so a run always leaves one.</summary>
        public static void WriteFailure(string outDir, string error)
        {
            try
            {
                Directory.CreateDirectory(outDir);
                var r = new Report { exitCode = ExitHarness, exitReason = error, started = Now() };
                File.WriteAllText(Path.Combine(outDir, "report.json"), JsonUtility.ToJson(r, true));
                File.WriteAllText(Path.Combine(outDir, "report.md"), "# CLONE-Z map harness\n\nThe harness failed before it started: "
                    + error + "\n");
            }
            catch (Exception e)
            {
                Debug.LogError("[MapShots] couldn't write the failure report: " + e);
            }
        }

        private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        #region Report (report.json is these, as JsonUtility writes them)
        [Serializable]
        public sealed class Report
        {
            public string harness = "CloneZ MapShots 1";
            public string started, unity, graphics, worldStore, buildId, attribution, place, zone;
            public double placeX, placeZ;
            public float halfSize;
            public bool quick;
            public int exitCode;
            public string exitReason;
            public double totalSeconds;
            public SquareReport square = new SquareReport();
            /// <summary>A second, warm build of the same square, each step of KentuckyMap.Passes timed.</summary>
            public List<StepTiming> squarePasses = new List<StepTiming>();
            public double squarePassesTotalMs;
            public string squarePassesError;
            public LogReport squarePassesLog = new LogReport();
            public List<SpotReport> spots = new List<SpotReport>();
            public List<ShotReport> shots = new List<ShotReport>();
            public List<ChunkReport> chunks = new List<ChunkReport>();
            /// <summary>Every region file read for the chunk tests (the streamer reads and sorts them off the main thread).</summary>
            public List<RegionReport> regions = new List<RegionReport>();
            public BlockReport louisvilleBlock = new BlockReport();
            public DeterminismReport determinism = new DeterminismReport();
            /// <summary>Everything logged as a warning or an error during the whole run (the build stages' own logs are
            /// in their sections).</summary>
            public LogReport runLog = new LogReport();
            public List<StageReport> stages = new List<StageReport>();
            public List<string> notes = new List<string>();
        }

        [Serializable]
        public sealed class SquareReport
        {
            public bool built;
            public string why, lastStats;
            /// <summary>KentuckyMap.Build, cold: the first map built in the process.</summary>
            public double buildMs;
            public float spawnX, spawnZ, spawnYaw;
            public List<string> exits = new List<string>();
            public int ceilingRenderers;
            /// <summary>What the map data holds around the place (read through the public API), to set against LastStats:
            /// buildings whose middle is inside the square, those of them whose outline comes within 1 m of the edge or
            /// crosses it, those in the scenery ring; street pieces, lines and land-use areas within reach.</summary>
            public int dataBuildingsInside, dataBuildingsAtEdge, dataBuildingsRing, dataStreetPieces, dataLines, dataAreas;
            public string dataBuildingKinds, dataAreaKinds, dataLineKinds;
            public SceneCounts total = new SceneCounts();
            public List<SceneCounts> roots = new List<SceneCounts>();
            public LogReport log = new LogReport();
        }

        [Serializable]
        public sealed class SceneCounts
        {
            public string name;
            public int gameObjects, renderers, meshFilters, distinctMeshes, materialSlots, distinctMaterials;
            public int lights, lightsShadowed, colliders, shadowCasters, lootContainers;
            public long vertices, triangles, distinctMeshVertices;
        }

        [Serializable]
        public sealed class LogReport
        {
            public int errors, warnings;
            /// <summary>Complaints that only happen because the harness builds in edit mode (Destroy instead of
            /// DestroyImmediate...): reported, never failing the run.</summary>
            public int editModeArtefacts;
            public List<string> first = new List<string>();
        }

        [Serializable]
        public sealed class StepTiming
        {
            public string name;
            public double ms, maxMs;
            public int count;
        }

        [Serializable]
        public sealed class SpotReport
        {
            public string id, title, why;
            public bool found;
            public float x, z, yaw;
        }

        [Serializable]
        public sealed class ShotReport
        {
            public string file, title, look, camera;
            public float x, z;
            public int width, height;
            public double renderMs;
            /// <summary>Mean Rec.709 luma (0..1), the share of near-black pixels (luma &lt; 0.03) and of clipped white.</summary>
            public float meanLuma, darkFraction, clippedFraction;
            public bool ok;
            public string error;
        }

        [Serializable]
        public sealed class ChunkReport
        {
            public string label, place;
            public int cx, cz, rx, rz;
            public bool regionMissing;
            /// <summary>Reading the region file and sorting it into chunks (the streamer does both off the main thread;
            /// 0 when the region was already read for an earlier chunk).</summary>
            public double regionReadMs, regionIndexMs;
            /// <summary>Every ChunkSteps step, back to back (what the streamer spreads over frames).</summary>
            public double totalMs, activateMs, slowestMs;
            public string slowestStep;
            public List<StepTiming> steps = new List<StepTiming>();
            public int bucketStreets, bucketLines, bucketOwned, bucketForeign, bucketAreas, bucketPois;
            public int buildings, containers, trees, lights;
            public SceneCounts counts = new SceneCounts();
            public bool failed;
            public string error;
            public LogReport log = new LogReport();
        }

        [Serializable]
        public sealed class BlockReport
        {
            public string place;
            public bool built;
            public int centreCx, centreCz;
            public double totalMs, maxChunkMs;
            public SceneCounts counts = new SceneCounts();
            public List<ChunkReport> chunks = new List<ChunkReport>();
            public string seams;
        }

        [Serializable]
        public sealed class DeterminismReport
        {
            public bool ran, match;
            public string label, detail;
            public int cx, cz, objectsA, objectsB, meshes, differingMeshes;
            public long verticesA, verticesB, trianglesA, trianglesB;
            public string orderedHashA, orderedHashB, unorderedHashA, unorderedHashB;
            /// <summary>The first meshes that came out different (their place in the piece, vertex count, position).</summary>
            public List<string> differences = new List<string>();
        }

        [Serializable]
        public sealed class RegionReport
        {
            public int rx, rz;
            public bool exists;
            public double readMs, indexMs;
        }

        [Serializable]
        public sealed class StageReport
        {
            public string name, error;
            public bool ok;
            public double ms;
        }
        #endregion

        private enum ShotKind { Top, Iso, Shoulder }

        /// <summary>A lighting: the game's own sums for an hour under a sky (see ApplyLook).</summary>
        private sealed class Look
        {
            public string Name;
            public float Hour;
            /// <summary>WeatherSystem's cloudiness, fog and precipitation (a winter raid opens overcast: 0.7, 0.2, 0).</summary>
            public float Cloud, WeatherFog, Precip;
            /// <summary>The game's distance fog (the plain daylight look leaves it off, so geometry reads).</summary>
            public bool Fog;
            /// <summary>The survivor's line-of-sight light and glow (VisionController), which only shine by night.</summary>
            public bool Sight;
        }

        /// <summary>The player camera's numbers, read from IsoCameraFollow's defaults (and GameBootstrapper's camera).</summary>
        private sealed class CameraNumbers
        {
            public float IsoYaw = 45f, IsoPitch = 38f, IsoClearance = 9f, IsoNear = 0.3f, LookHeight = 1f, FogBaseline = 24f;
            public bool CompensateFog = true;
            public float[] Zoom = { 5.5f, 7f, 9f, 11f, 12.5f };
            public int ZoomStep = 2;
            public float ShoulderFov = 55f, ShoulderPitch = 8f, PivotBelowHead = 0.14f, EyeHeight = 1.6f, EyeAboveHeadBone = 0.07f;
            public float MouseNear = 0.05f, CollisionRadius = 0.22f, Far = 400f;
            public Vector3 Shoulder = new Vector3(0.45f, 0.15f, -2.4f);
            public string Source = "the defaults known when the harness was written";

            public float IsoDistance(float size) => size / Mathf.Tan(Mathf.Clamp(IsoPitch, 15f, 89f) * Mathf.Deg2Rad) + IsoClearance;
            public float PivotHeight => EyeHeight - EyeAboveHeadBone - PivotBelowHead;
            public float Wide => Zoom[Zoom.Length - 1];
            public float Opening => Zoom[Mathf.Clamp(ZoomStep, 0, Zoom.Length - 1)];
        }

        private sealed class Shot
        {
            public string File, Title;
            public ShotKind Kind;
            public Look Look;
            /// <summary>Where the survivor stands (top-down: the middle of the picture), map metres x, z.</summary>
            public Vector2 At;
            /// <summary>Which way the survivor faces (compass degrees); the isometric camera keeps its own heading.</summary>
            public float Yaw;
            /// <summary>Orthographic half-height (isometric: the zoom; top-down: half the side shown).</summary>
            public float Size;
            public int W = 1600, H = 1000;
            public bool Roofs, Marks, AllLights, Survivor = true;
        }

        private sealed class Spot
        {
            public string Id, Title, Why;
            public bool Found;
            public Vector2 At;
            public float Yaw;
            /// <summary>For a street-level view: the street's direction and carriageway width there.</summary>
            public Vector2 StreetDir;
            public float StreetWidth;
            public string StreetName;
        }

        private sealed class MapLight
        {
            public Light Light;
            public bool Enabled;
            public LightShadows Shadows;
            public RoomLight Room;
            public Renderer Bulb;
        }

        private sealed partial class Harness
        {
            private readonly Options _o;
            private readonly Report _r = new Report();
            private readonly string _out;
            private readonly Stopwatch _clock = Stopwatch.StartNew();

            private ZoneDef _zone;
            private MapBuild _build;
            private MapPlace _place;
            private float _half;
            private string _noWorld;
            private Look _day, _night;
            private CameraNumbers _cn = new CameraNumbers();
            private readonly SandboxSettings _sandbox = new SandboxSettings();
            private readonly GameSettings _settings = new GameSettings();

            // The start square as KentuckyMap.Build left it.
            private Transform _world;
            private readonly List<GameObject> _mapRoots = new List<GameObject>();
            private readonly List<Mesh> _meshes = new List<Mesh>();
            private RaidMapInfo _info;
            private bool _squareAttempted, _squareBuilt;
            private readonly List<Spot> _spots = new List<Spot>();

            // Health: map errors fail the run with 2, harness failures with 1.
            private readonly object _logLock = new object();
            private LogReport _capture;
            private int _mapErrors, _harnessErrors;

            // The photo rig.
            private GameObject _rig, _figure, _marks;
            private Renderer _figureRenderer;
            private Camera _cam;
            private Light _sun, _sight, _periphery;
            private Volume _gameVolume, _plainVolume;
            private readonly List<Renderer> _ceilings = new List<Renderer>();
            private readonly List<MapLight> _lights = new List<MapLight>();

            public Harness(Options o)
            {
                _o = o;
                _out = string.IsNullOrEmpty(o.OutDir) ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "MapShots") : o.OutDir;
            }

            public int Execute()
            {
                _r.started = Now();
                _r.quick = _o.Quick;
                StackTraceLogType trace = Application.GetStackTraceLogType(LogType.Log);
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.logMessageReceivedThreaded += OnLog;
                try
                {
                    Directory.CreateDirectory(_out);
                    Note("writing to " + _out + (_o.Quick ? " (quick)" : ""));
                    if (Stage("setup", Setup) && _noWorld == null)
                    {
                        if (Stage("square: KentuckyMap.Build", BuildSquare) && _squareBuilt)
                        {
                            Stage("square: count", CountSquare);
                            Stage("square: pick spots", PickSpots);
                            Stage("square: pictures", ShootSquare);
                        }
                        Stage("square: discard", DiscardSquare);
                        if (!_o.Quick)
                        {
                            Stage("square: passes (warm rebuild)", ProfilePasses);
                            Stage("chunks: stress test", ChunkStress);
                            Stage("chunks: Louisville 3x3", LouisvilleBlock);
                            Stage("chunks: determinism", Determinism);
                        }
                    }
                }
                catch (Exception e)
                {
                    _harnessErrors++;
                    Debug.LogError("[MapShots] the harness failed: " + e);
                }
                finally
                {
                    try { Cleanup(); }
                    catch (Exception e) { _harnessErrors++; Debug.LogError("[MapShots] cleaning up failed: " + e); }
                    Application.logMessageReceivedThreaded -= OnLog;
                    Application.SetStackTraceLogType(LogType.Log, trace);
                    Finish();
                }
                return _r.exitCode;
            }

            #region Stages, notes and the log
            private bool Stage(string name, Action act)
            {
                var st = new StageReport { name = name };
                var sw = Stopwatch.StartNew();
                Note(name + " ...");
                try
                {
                    act();
                    st.ok = true;
                }
                catch (Exception e)
                {
                    _harnessErrors++;
                    st.error = Brief(e);
                    Debug.LogError("[MapShots] " + name + " failed: " + e);
                }
                st.ms = Ms(sw);
                _r.stages.Add(st);
                Note(name + (st.ok ? " done" : " FAILED") + " in " + F0(st.ms) + " ms");
                return st.ok;
            }

            private void Note(string message)
                => Debug.Log("[MapShots] " + (_clock.Elapsed.TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture) + "s " + message);

            private void Remark(string message)
            {
                _r.notes.Add(message);
                Note(message);
            }

            // Map-building stages capture what the map code logs: its errors fail the run (exit 2).
            private void Begin(LogReport into)
            {
                lock (_logLock) _capture = into;
            }

            private void End()
            {
                lock (_logLock) _capture = null;
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Log) return;
                if (message != null && message.StartsWith("[MapShots]", StringComparison.Ordinal)) return;
                // The editor's own AI Assistant relay complains in batch mode; it's no part of the game (as SmokeTest).
                if (stack != null && stack.Contains("com.unity.ai.assistant")) return;
                bool artefact = message != null && (message.IndexOf("edit mode", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("play mode", StringComparison.OrdinalIgnoreCase) >= 0);
                lock (_logLock)
                {
                    Add(_r.runLog, type, message, stack, artefact);
                    if (_capture == null) return;
                    Add(_capture, type, message, stack, artefact);
                    if (type != LogType.Warning && !artefact) _mapErrors++;
                }
            }

            private static void Add(LogReport into, LogType type, string message, string stack, bool artefact)
            {
                if (artefact) into.editModeArtefacts++;
                else if (type == LogType.Warning) into.warnings++;
                else into.errors++;
                if (into.first.Count >= 20) return;
                into.first.Add((artefact ? "(edit mode) " : "") + type + ": " + FirstLine(message, 300)
                    + (string.IsNullOrEmpty(stack) ? "" : "  @ " + FirstLine(stack, 200)));
            }

            private static string FirstLine(string s, int max)
            {
                if (string.IsNullOrEmpty(s)) return "";
                int n = s.IndexOf('\n');
                string line = (n < 0 ? s : s.Substring(0, n)).Trim();
                return line.Length > max ? line.Substring(0, max) + "..." : line;
            }

            private static string Brief(Exception e) => e.GetType().Name + ": " + FirstLine(e.Message, 300) + "  @ "
                + FirstLine(e.StackTrace, 200);

            private static double Ms(Stopwatch sw) => Math.Round(sw.Elapsed.TotalMilliseconds, 2);

            private static string F0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);

            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
            #endregion

            #region Setup: the zone, the world store, the raid's runtime state, the photo rig
            private void Setup()
            {
                _r.unity = Application.unityVersion;
                _r.graphics = SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName;
                _r.worldStore = WorldStore.Root;
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                    Remark("No graphics device (was -nographics passed?): the pictures will fail.");
                _zone = StartZone(_o.Place);
                _r.zone = _zone.Id;
                _r.place = _zone.RealPlace;
                _half = Mathf.Clamp(_zone.RealHalfSize, 60f, 400f);   // as KentuckyMap.Build clamps it
                _r.halfSize = _half;
                _day = new Look { Name = "day", Hour = 10f, Cloud = 0.7f, WeatherFog = 0.2f, Fog = false, Sight = false };
                _night = new Look { Name = "night", Hour = _zone.StartHour, Cloud = 0.7f, WeatherFog = 0.2f, Fog = true, Sight = true };

                _build = WorldStore.OpenCurrent(out string why);
                if (_build == null)
                {
                    _noWorld = "the world store at " + WorldStore.Root + " can't be read: " + why;
                    Debug.LogError("[MapShots] " + _noWorld);
                    return;
                }
                _r.buildId = _build.Id;
                _r.attribution = _build.Attribution;
                string streamable = MapApi.Use(_build);
                if (streamable != null) Remark(streamable);
                _place = _build.FindPlace(_zone.RealPlace);
                if (_place == null)
                {
                    _noWorld = "build " + _build.Id + " has no place called " + _zone.RealPlace;
                    Debug.LogError("[MapShots] " + _noWorld);
                    return;
                }
                _r.placeX = _place.x;
                _r.placeZ = _place.z;
                Note("build " + _build.Id + ", " + _place.name + " (" + _place.kind + ", pop " + _place.population + ") at "
                    + F1(_place.x) + ", " + F1(_place.z));

                // What GameBootstrapper.BuildRaidWorld sets before it builds a raid's map.
                Climate.Winter = _zone.Winter;
                Shelter.Clear();
                StarterHome.SetActive(false);
                ViewState.Current = CameraView.Isometric;

                ReadCameraNumbers();
                BuildRig();
            }

            // A copy of the bardstown zone (or the first real-place one), fixed rather than statewide: the start square
            // and its scenery ring, built at once.
            private static ZoneDef StartZone(string place)
            {
                ZoneDef src = null;
                foreach (ZoneDef z in ZoneCatalog.All)
                    if (z.Id == "bardstown") { src = z; break; }
                if (src == null)
                    foreach (ZoneDef z in ZoneCatalog.All)
                        if (!string.IsNullOrEmpty(z.RealPlace)) { src = z; break; }
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
                if (!string.IsNullOrEmpty(place)) copy.RealPlace = place;
                return copy;
            }

            // IsoCameraFollow's own numbers (a component added to an inactive object in edit mode runs nothing).
            private void ReadCameraNumbers()
            {
                var go = new GameObject("[MapShots] camera numbers");
                go.SetActive(false);
                try
                {
                    var f = go.AddComponent<IsoCameraFollow>();
                    _cn.IsoYaw = f.IsoYaw;
                    _cn.IsoPitch = f.IsoPitch;
                    _cn.IsoClearance = f.IsoClearance;
                    _cn.IsoNear = f.IsoNearClip;
                    _cn.LookHeight = f.LookHeight;
                    _cn.FogBaseline = f.IsoFogBaseline;
                    _cn.CompensateFog = f.CompensateIsoFog;
                    if (f.IsoZoomSizes != null && f.IsoZoomSizes.Length > 0) _cn.Zoom = (float[])f.IsoZoomSizes.Clone();
                    _cn.ZoomStep = Mathf.Clamp(f.IsoZoomStep, 0, _cn.Zoom.Length - 1);
                    _cn.ShoulderFov = f.ThirdPersonFov;
                    _cn.ShoulderPitch = f.ThirdPersonStartPitch;
                    _cn.Shoulder = f.ShoulderOffset;
                    _cn.PivotBelowHead = f.PivotBelowHead;
                    _cn.EyeHeight = f.EyeHeight;
                    _cn.EyeAboveHeadBone = f.EyeAboveHeadBone;
                    _cn.MouseNear = f.MouseLookNearClip;
                    _cn.CollisionRadius = f.CollisionRadius;
                    _cn.Source = "IsoCameraFollow's defaults";
                }
                catch (Exception e)
                {
                    Remark("Couldn't read IsoCameraFollow's numbers (" + e.GetType().Name + ": " + e.Message + "); using " + _cn.Source + ".");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
                Note("camera: iso yaw " + _cn.IsoYaw + " pitch " + _cn.IsoPitch + ", zoom " + string.Join("/", _cn.Zoom)
                    + " (opens at " + _cn.Opening + "), shoulder fov " + _cn.ShoulderFov + " pitch " + _cn.ShoulderPitch + " (" + _cn.Source + ")");
            }

            private void BuildRig()
            {
                _rig = new GameObject("[MapShots]");

                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(_rig.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.allowHDR = true;
                _cam.allowMSAA = false;
                _cam.farClipPlane = _cn.Far;
                UniversalAdditionalCameraData data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;   // the game's camera has it on (SampleScene, PostFx)
                data.renderShadows = true;
                // The game ships no anti-aliasing; SMAA only so thin geometry reads in the pictures.
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;

                // The game scene's Directional Light, which TimeOfDay swings and tints.
                var sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(_rig.transform, false);
                _sun = sunGo.AddComponent<Light>();
                _sun.type = LightType.Directional;
                _sun.shadows = LightShadows.Soft;
                _sun.useColorTemperature = true;
                _sun.colorTemperature = 5000f;
                RenderSettings.sun = _sun;

                var volGo = new GameObject("Game Volume");
                volGo.transform.SetParent(_rig.transform, false);
                _gameVolume = volGo.AddComponent<Volume>();
                _gameVolume.isGlobal = true;
                _gameVolume.priority = 0f;
                _gameVolume.sharedProfile = _o.GameProfile;
                if (_o.GameProfile == null) Remark("The game scene's volume profile wasn't found: pictures lack its bloom, tonemapping and vignette.");

                // Top-down pictures are maps, not views: no vignette darkening their corners.
                var plainGo = new GameObject("Plain Volume");
                plainGo.transform.SetParent(_rig.transform, false);
                _plainVolume = plainGo.AddComponent<Volume>();
                _plainVolume.isGlobal = true;
                _plainVolume.priority = 100f;
                VolumeProfile plain = ScriptableObject.CreateInstance<VolumeProfile>();
                plain.Add<Vignette>(true).intensity.Override(0f);
                _plainVolume.sharedProfile = plain;
                _plainVolume.enabled = false;

                // The survivor's sight light and peripheral glow, as VisionController.Bind makes them.
                _sight = NewLight("SightLight");
                _sight.color = new Color(0.78f, 0.86f, 1f);
                _sight.spotAngle = 155f;
                _sight.innerSpotAngle = 110f;
                _sight.range = 16f;
                _sight.shadows = LightShadows.Soft;
                _sight.shadowStrength = 1f;
                _sight.shadowNearPlane = 0.1f;
                _sight.cookie = VisionAssets.VisionCookie(155f, 1.62f, 6f, 0.35f, 12f);
                UniversalAdditionalLightData sd = _sight.GetUniversalAdditionalLightData();
                sd.usePipelineSettings = false;
                sd.softShadowQuality = SoftShadowQuality.Medium;
                // Its shadow tier stays URP's default, High, which is what the game asks for (it can't be set in edit mode).
                float texel = Mathf.Tan(155f * 0.5f * Mathf.Deg2Rad) * 16f / 1024f * 2.5f;
                _sight.shadowBias = 0.04f / texel;
                _sight.shadowNormalBias = 0.06f / texel;

                _periphery = NewLight("PeripheryLight");
                _periphery.color = new Color(0.8f, 0.87f, 1f);
                _periphery.range = 7f;
                _periphery.spotAngle = 90f;
                _periphery.innerSpotAngle = 50f;
                _periphery.shadows = LightShadows.None;
                _periphery.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

                // A survivor-sized stand-in (1.8 m), for scale.
                _figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _figure.name = "Survivor";
                Object.DestroyImmediate(_figure.GetComponent<Collider>());
                _figure.transform.SetParent(_rig.transform, false);
                _figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                _figureRenderer = _figure.GetComponent<Renderer>();
                _figureRenderer.sharedMaterial = LitMaterial(new Color(0.16f, 0.2f, 0.27f));
                _figure.SetActive(false);
            }

            private Light NewLight(string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_rig.transform, false);
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.enabled = false;
                return light;
            }

            private static Material LitMaterial(Color c)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh != null ? sh : Shader.Find("Standard")) { name = "[MapShots] lit" };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                return m;
            }

            private static Material UnlitMaterial(Color c)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                var m = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "[MapShots] unlit" };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                return m;
            }

            private void Cleanup()
            {
                if (_rig != null) Object.DestroyImmediate(_rig);
                MapApi.ReleaseRegions();
            }
            #endregion
        }

        private sealed partial class Harness
        {
            #region The start square
            private void BuildSquare()
            {
                _squareAttempted = true;
                List<GameObject> before = SceneRoots();
                _world = new GameObject("World").transform;
                string why = null;
                var sw = Stopwatch.StartNew();
                Begin(_r.square.log);
                try
                {
                    using (MapApi.TrackMeshes(_meshes)) _info = KentuckyMap.Build(_world, _zone, out why);
                }
                catch (Exception e)
                {
                    // Build reports its own failures as null; anything escaping it is the map's failure all the same.
                    _info = null;
                    why = "KentuckyMap.Build threw " + Brief(e);
                }
                finally
                {
                    End();
                }
                _r.square.buildMs = Ms(sw);
                _r.square.why = why;
                _r.square.lastStats = KentuckyMap.LastStats;
                foreach (GameObject go in SceneRoots())
                    if (!before.Contains(go) && go != _world.gameObject) _mapRoots.Add(go);
                _squareBuilt = _info != null;
                _r.square.built = _squareBuilt;
                LogReport log = _r.square.log;
                Note("KentuckyMap.Build " + (_squareBuilt ? "built" : "FAILED") + " in " + F0(_r.square.buildMs) + " ms; logged "
                    + log.errors + " errors, " + log.warnings + " warnings, " + log.editModeArtefacts + " edit-mode artefacts");
                if (!_squareBuilt)
                {
                    Debug.LogError("[MapShots] KentuckyMap.Build failed: " + why);
                    return;
                }
                Note(KentuckyMap.LastStats);
                _r.square.spawnX = _info.PlayerSpawn.x;
                _r.square.spawnZ = _info.PlayerSpawn.z;
                _r.square.spawnYaw = _info.PlayerSpawnYaw;
                foreach (RaidExit e in _info.Exits)
                    _r.square.exits.Add(e.Name + " at " + F1(e.Position.x) + ", " + F1(e.Position.z));
            }

            private List<Transform> MapRootTransforms()
            {
                var roots = new List<Transform>();
                if (_world != null) roots.Add(_world);
                foreach (GameObject go in _mapRoots)
                    if (go != null) roots.Add(go.transform);
                return roots;
            }

            private void CountSquare()
            {
                List<Transform> roots = MapRootTransforms();
                _r.square.total = Count("all", roots);
                foreach (Transform t in roots) _r.square.roots.Add(Count(t.name, new[] { t }));
                _r.square.ceilingRenderers = CeilingVisibility.Count;
                SceneCounts c = _r.square.total;
                Note("square: " + F0(c.gameObjects) + " objects, " + F0(c.renderers) + " renderers, " + F0(c.vertices) + " vertices, "
                    + F0(c.triangles) + " triangles, " + c.distinctMaterials + " materials, " + c.lights + " lights, " + F0(c.colliders) + " colliders");
            }

            private void DiscardSquare()
            {
                foreach (GameObject go in _mapRoots)
                    if (go != null) Object.DestroyImmediate(go);
                _mapRoots.Clear();
                if (_world != null) Object.DestroyImmediate(_world.gameObject);
                _world = null;
                DestroyMeshes(_meshes);
                _ceilings.Clear();
                _lights.Clear();
                CeilingVisibility.Clear();
                Shelter.Clear();
            }

            private static void DestroyMeshes(List<Mesh> meshes)
            {
                foreach (Mesh m in meshes)
                    if (m != null) Object.DestroyImmediate(m);
                meshes.Clear();
            }

            private static List<GameObject> SceneRoots()
            {
                var list = new List<GameObject>();
                SceneManager.GetActiveScene().GetRootGameObjects(list);
                return list;
            }

            // The same square again, its passes driven one at a time with a stopwatch: warm (the code, textures and
            // materials are loaded by now), so it shows what each pass costs rather than the first build's one-offs.
            private void ProfilePasses()
            {
                List<GameObject> before = SceneRoots();
                var world = new GameObject("World (pass profile)").transform;
                var meshes = new List<Mesh>();
                var steps = new List<StepTiming>();
                Begin(_r.squarePassesLog);
                try
                {
                    _r.squarePassesTotalMs = Math.Round(MapApi.ProfileSquare(world, _zone, _build, _place, steps, meshes), 2);
                }
                catch (Exception e)
                {
                    // The map's own code threw (KentuckyMap.Build would have reported it as a failed build).
                    _mapErrors++;
                    _r.squarePassesError = Brief(e);
                    Debug.LogError("[MapShots] the pass-by-pass build failed in the map code: " + e);
                }
                finally
                {
                    End();
                    foreach (GameObject go in SceneRoots())
                        if (!before.Contains(go)) Object.DestroyImmediate(go);
                    DestroyMeshes(meshes);
                    CeilingVisibility.Clear();
                    Shelter.Clear();
                }
                _r.squarePasses = Aggregate(steps);
                Note("passes: " + F0(_r.squarePassesTotalMs) + " ms over " + steps.Count + " steps");
            }

            // Steps by name, in the order they first ran (the buildings pass yields many "buildings" steps).
            private static List<StepTiming> Aggregate(List<StepTiming> steps)
            {
                var list = new List<StepTiming>();
                var at = new Dictionary<string, StepTiming>();
                foreach (StepTiming s in steps)
                {
                    if (!at.TryGetValue(s.name, out StepTiming a))
                    {
                        a = new StepTiming { name = s.name };
                        at[s.name] = a;
                        list.Add(a);
                    }
                    a.ms = Math.Round(a.ms + s.ms, 2);
                    a.count++;
                    a.maxMs = Math.Max(a.maxMs, Math.Round(s.ms, 2));
                }
                return list;
            }
            #endregion

            #region Counting
            private static SceneCounts Count(string name, IEnumerable<Transform> roots)
            {
                var c = new SceneCounts { name = name };
                var meshes = new HashSet<Mesh>();
                var materials = new HashSet<Material>();
                foreach (Transform root in roots)
                {
                    if (root == null) continue;
                    c.gameObjects += root.GetComponentsInChildren<Transform>(true).Length;
                    foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        c.renderers++;
                        if (r.shadowCastingMode != ShadowCastingMode.Off) c.shadowCasters++;
                        Material[] mats = r.sharedMaterials;
                        c.materialSlots += mats.Length;
                        foreach (Material m in mats)
                            if (m != null) materials.Add(m);
                    }
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        c.meshFilters++;
                        Mesh m = mf.sharedMesh;
                        if (m == null) continue;
                        long v = m.vertexCount;
                        c.vertices += v;
                        c.triangles += Triangles(m);
                        if (meshes.Add(m)) c.distinctMeshVertices += v;
                    }
                    foreach (Light l in root.GetComponentsInChildren<Light>(true))
                    {
                        c.lights++;
                        if (l.shadows != LightShadows.None) c.lightsShadowed++;
                    }
                    c.colliders += root.GetComponentsInChildren<Collider>(true).Length;
                    c.lootContainers += root.GetComponentsInChildren<LootContainer>(true).Length;
                }
                c.distinctMeshes = meshes.Count;
                c.distinctMaterials = materials.Count;
                return c;
            }

            private static long Triangles(Mesh m)
            {
                long n = 0;
                for (int i = 0; i < m.subMeshCount; i++)
                    if (m.GetTopology(i) == MeshTopology.Triangles) n += (long)m.GetIndexCount(i) / 3;
                return n;
            }

            // The overhead renderers CeilingVisibility toggles, and every light the map made with its state as built.
            private void CollectCeilings(IEnumerable<Transform> roots)
            {
                _ceilings.Clear();
                List<Renderer> registered = MapApi.CeilingRenderers();
                if (registered != null)
                {
                    foreach (Renderer r in registered)
                        if (r != null) _ceilings.Add(r);
                    return;
                }
                // Not readable: the renderers under the ceiling roots that the isometric view hides.
                foreach (Transform root in roots)
                {
                    if (root == null) continue;
                    foreach (CeilingVisibility cv in root.GetComponentsInChildren<CeilingVisibility>(true))
                        foreach (Renderer r in cv.GetComponentsInChildren<Renderer>(true))
                            if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) _ceilings.Add(r);
                }
            }

            private void CollectLights(IEnumerable<Transform> roots)
            {
                _lights.Clear();
                foreach (Transform root in roots)
                {
                    if (root == null) continue;
                    foreach (Light l in root.GetComponentsInChildren<Light>(true))
                    {
                        var m = new MapLight { Light = l, Enabled = l.enabled, Shadows = l.shadows, Room = l.GetComponent<RoomLight>() };
                        if (m.Room != null)
                        {
                            Transform bulb = m.Room.transform.Find("Bulb");
                            if (bulb != null) m.Bulb = bulb.GetComponent<Renderer>();
                        }
                        _lights.Add(m);
                    }
                }
            }
            #endregion
        }

        // The map data around the place, read through the world store's public API, in map metres from the place.
        private sealed class Sample
        {
            public readonly List<SStreet> Streets = new List<SStreet>();
            public readonly List<SLine> Lines = new List<SLine>();
            public readonly List<SBuilding> Buildings = new List<SBuilding>();
            public readonly List<SArea> Areas = new List<SArea>();
            /// <summary>The street network's nodes, by exact grid position.</summary>
            public readonly Dictionary<long, SNode> Nodes = new Dictionary<long, SNode>();
        }

        private sealed class SStreet
        {
            public long Id;
            public string Kind, Name;
            public float Width;
            public int Rank;
            public bool Tunnel;
            /// <summary>The square of the region the piece came from (regions overlap their neighbours a little).</summary>
            public Rect Own;
            public List<Vector2> Pts;
        }

        private sealed class SLine
        {
            public string Kind, Name;
            public Rect Own;
            public List<Vector2> Pts;
        }

        private sealed class SBuilding
        {
            public string Kind, Name, Use;
            public Vector2 Mid;
            public float Area;
            public Rect Bounds;
        }

        private sealed class SArea
        {
            public string Kind, Name;
            public List<Vector2> Pts;
        }

        private sealed class SNode
        {
            public Vector2 At;
            /// <summary>Street id: 1 when the street ends here, 2 when it passes through.</summary>
            public readonly Dictionary<long, int> Ways = new Dictionary<long, int>();
            public readonly Dictionary<long, SStreet> Of = new Dictionary<long, SStreet>();
        }

        private sealed partial class Harness
        {
            private Sample _sample;

            #region Spots, picked from the map data
            private void PickSpots()
            {
                var sw = Stopwatch.StartNew();
                Sample s = LoadSample(_half + 90f);
                _sample = s;
                Note("map data: " + s.Streets.Count + " street pieces, " + s.Lines.Count + " lines, " + s.Buildings.Count + " buildings, "
                    + s.Areas.Count + " areas, " + s.Nodes.Count + " street nodes, read in " + F0(Ms(sw)) + " ms");
                SummariseData(s);
                float inner = _half - 12f;
                Pick("spawn", () => SpawnSpot(s));
                Pick("downtown", () => Downtown(s));
                Pick("residential", () => Residential(s, inner));
                Pick("intersection", () => Intersection(s, inner));
                Pick("green", () => BestArea(s, inner, GreenWeight, "Green",
                    "the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half)"));
                Pick("parking", () => Parking(s, inner));
                Pick("edge", () => Edge(s));
                Pick("waterway", () => Waterway(s, inner));
                Pick("landmark", () => Landmark(s, inner));
            }

            private void Pick(string id, Func<Spot> pick)
            {
                Spot spot;
                try
                {
                    spot = pick() ?? new Spot { Why = "nothing of the kind in the square" };
                }
                catch (Exception e)
                {
                    _harnessErrors++;
                    spot = new Spot { Why = "picking it failed: " + Brief(e) };
                    Debug.LogError("[MapShots] picking the " + id + " spot failed: " + e);
                }
                spot.Id = id;
                _spots.Add(spot);
                _r.spots.Add(new SpotReport { id = id, title = spot.Title, why = spot.Why, found = spot.Found, x = spot.At.x, z = spot.At.y, yaw = spot.Yaw });
                Note("spot " + id + ": " + (spot.Found ? F1(spot.At.x) + ", " + F1(spot.At.y) + ": " + spot.Title : "none (" + spot.Why + ")"));
            }

            private Sample LoadSample(float reach)
            {
                var s = new Sample();
                int size = _build.RegionSize;
                var near = Rect.MinMaxRect(-reach, -reach, reach, reach);
                foreach (MapRegion r in _build.RegionsAround(_place.x, _place.z, reach))
                {
                    if (r == null) continue;
                    double ox = (double)r.rx * size - _place.x, oz = (double)r.rz * size - _place.z;
                    var own = Rect.MinMaxRect((float)ox, (float)oz, (float)(ox + size), (float)(oz + size));
                    long gx0 = (long)r.rx * size * 10L, gz0 = (long)r.rz * size * 10L;
                    if (r.streets != null)
                        foreach (MapStreet st in r.streets)
                        {
                            if (st == null || st.pts == null || st.pts.Length < 4) continue;
                            List<Vector2> pts = Local(st.pts, ox, oz);
                            if (!Bounds(pts).Overlaps(near)) continue;
                            var item = new SStreet
                            {
                                Id = st.id, Kind = st.kind ?? "", Name = st.name, Width = st.width, Rank = Rank(st.kind),
                                Tunnel = st.tunnel, Own = own, Pts = pts,
                            };
                            s.Streets.Add(item);
                            if (item.Rank < 1 || st.tunnel) continue;
                            for (int k = 0; k + 1 < st.pts.Length; k += 2)
                            {
                                Vector2 at = pts[k / 2];
                                if (!near.Contains(at)) continue;
                                long key = ((gx0 + st.pts[k]) << 32) ^ ((gz0 + st.pts[k + 1]) & 0xffffffffL);
                                if (!s.Nodes.TryGetValue(key, out SNode n)) s.Nodes[key] = n = new SNode { At = at };
                                int add = k == 0 || k + 2 >= st.pts.Length ? 1 : 2;
                                n.Ways[st.id] = n.Ways.TryGetValue(st.id, out int had) ? Math.Max(had, add) : add;
                                n.Of[st.id] = item;
                            }
                        }
                    if (r.lines != null)
                        foreach (MapLine l in r.lines)
                        {
                            if (l == null || l.pts == null || l.pts.Length < 4) continue;
                            List<Vector2> pts = Local(l.pts, ox, oz);
                            if (Bounds(pts).Overlaps(near)) s.Lines.Add(new SLine { Kind = l.kind ?? "", Name = l.name, Own = own, Pts = pts });
                        }
                    if (r.buildings != null)
                        foreach (MapBuilding b in r.buildings)
                        {
                            if (b == null || b.pts == null || b.pts.Length < 6) continue;
                            List<Vector2> pts = Local(b.pts, ox, oz);
                            Vector2 mid = Average(pts);
                            if (!near.Contains(mid)) continue;
                            s.Buildings.Add(new SBuilding
                            {
                                Kind = b.kind ?? "", Name = b.name, Use = b.use, Mid = mid, Area = Mathf.Abs(SignedArea(pts)), Bounds = Bounds(pts),
                            });
                        }
                    if (r.areas != null)
                        foreach (MapArea a in r.areas)
                        {
                            if (a == null || a.pts == null || a.pts.Length < 6) continue;
                            List<Vector2> pts = Clip(Clip(Local(a.pts, ox, oz), own), near);
                            if (pts.Count >= 3) s.Areas.Add(new SArea { Kind = a.kind ?? "", Name = a.name, Pts = pts });
                        }
                }
                return s;
            }

            // What the map data holds around the place, to set against what KentuckyMap built (LastStats, the counts).
            private void SummariseData(Sample s)
            {
                SquareReport sq = _r.square;
                var buildingKinds = new Dictionary<string, int>();
                foreach (SBuilding b in s.Buildings)
                {
                    bool inside = Mathf.Abs(b.Mid.x) < _half && Mathf.Abs(b.Mid.y) < _half;
                    if (!inside)
                    {
                        sq.dataBuildingsRing++;
                        continue;
                    }
                    sq.dataBuildingsInside++;
                    float edge = _half - 1f;
                    if (b.Bounds.xMin <= -edge || b.Bounds.xMax >= edge || b.Bounds.yMin <= -edge || b.Bounds.yMax >= edge) sq.dataBuildingsAtEdge++;
                    buildingKinds[b.Kind] = buildingKinds.TryGetValue(b.Kind, out int n) ? n + 1 : 1;
                }
                var areaKinds = new Dictionary<string, int>();
                foreach (SArea a in s.Areas) areaKinds[a.Kind] = areaKinds.TryGetValue(a.Kind, out int n) ? n + 1 : 1;
                var lineKinds = new Dictionary<string, int>();
                foreach (SLine l in s.Lines) lineKinds[l.Kind] = lineKinds.TryGetValue(l.Kind, out int n) ? n + 1 : 1;
                sq.dataStreetPieces = s.Streets.Count;
                sq.dataLines = s.Lines.Count;
                sq.dataAreas = s.Areas.Count;
                sq.dataBuildingKinds = Kinds(buildingKinds);
                sq.dataAreaKinds = Kinds(areaKinds);
                sq.dataLineKinds = Kinds(lineKinds);
            }

            private static string Kinds(Dictionary<string, int> counts)
            {
                var list = new List<KeyValuePair<string, int>>(counts);
                list.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));
                var parts = new List<string>();
                foreach (KeyValuePair<string, int> kv in list) parts.Add((string.IsNullOrEmpty(kv.Key) ? "?" : kv.Key) + " " + kv.Value);
                return string.Join(", ", parts);
            }

            private Spot SpawnSpot(Sample s)
            {
                var at = new Vector2(_info.PlayerSpawn.x, _info.PlayerSpawn.z);
                SStreet street = NearestStreet(s, at);
                string name = street != null ? StreetLabel(street) : "a street";
                return new Spot
                {
                    Found = true, At = at, Yaw = _info.PlayerSpawnYaw, StreetName = name,
                    Title = "The raid's opening frame: the survivor's spawn on " + name + ", at the game's starting zoom",
                    Why = "KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street",
                };
            }

            // The place's own point, moved onto the nearest street: the survivor (and by night the sight light) must stand
            // somewhere they could, and a place node is often inside a building (Bardstown's is in the courthouse).
            private Spot Downtown(Sample s)
            {
                Vector2 at = Vector2.zero;
                string name = null;
                float bestDist = float.MaxValue;
                foreach (SStreet st in s.Streets)
                {
                    if (st.Rank < 1 || st.Tunnel) continue;
                    for (int i = 0; i + 1 < st.Pts.Count; i++)
                    {
                        Vector2 a = st.Pts[i], ab = st.Pts[i + 1] - a;
                        float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(-a, ab) / ab.sqrMagnitude) : 0f;
                        Vector2 p = a + ab * t;
                        if (p.magnitude >= bestDist) continue;
                        bestDist = p.magnitude;
                        at = p;
                        name = StreetLabel(st);
                    }
                }
                return new Spot
                {
                    Found = true, At = at, StreetName = name,
                    Title = "Downtown " + _place.name + ": the middle of the square" + (name != null ? ", on " + name : ""),
                    Why = "the place's own point in places.json (its OSM place node, which the square is built around), moved "
                        + (name != null ? F1(bestDist) + " m onto the nearest street" : "nowhere: no street in reach"),
                };
            }

            private Spot Residential(Sample s, float inner)
            {
                Spot best = null;
                int bestCount = 0;
                float bestDist = float.MaxValue;
                foreach (SStreet st in s.Streets)
                {
                    if (st.Kind != "residential" && st.Kind != "living_street") continue;
                    for (int i = 0; i + 1 < st.Pts.Count; i++)
                    {
                        Vector2 a = st.Pts[i], b = st.Pts[i + 1];
                        float len = Vector2.Distance(a, b);
                        if (len < 1f || !st.Own.Contains((a + b) * 0.5f)) continue;
                        int n = Mathf.Max(1, Mathf.CeilToInt(len / 6f));
                        for (int k = 0; k < n; k++)
                        {
                            Vector2 p = Vector2.Lerp(a, b, (k + 0.5f) / n);
                            if (Mathf.Abs(p.x) > inner || Mathf.Abs(p.y) > inner) continue;
                            int count = 0;
                            foreach (SBuilding bl in s.Buildings)
                                if (IsHome(bl.Kind) && (bl.Mid - p).sqrMagnitude < 900f) count++;
                            float dist = p.magnitude;
                            if (count < bestCount || (count == bestCount && dist >= bestDist)) continue;
                            bestCount = count;
                            bestDist = dist;
                            best = new Spot { Found = true, At = p, StreetDir = (b - a) / len, StreetWidth = st.Width, StreetName = StreetLabel(st) };
                        }
                    }
                }
                if (best == null) return null;
                best.Title = "The densest residential street: " + best.StreetName + ", " + bestCount + " homes within 30 m";
                best.Why = "of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m";
                return best;
            }

            private Spot Intersection(Sample s, float inner)
            {
                SNode best = null;
                int bestScore = 0, bestDegree = 0;
                float bestDist = float.MaxValue;
                foreach (SNode n in s.Nodes.Values)
                {
                    if (Mathf.Abs(n.At.x) > inner || Mathf.Abs(n.At.y) > inner) continue;
                    int degree = 0, score = 0;
                    foreach (KeyValuePair<long, int> w in n.Ways)
                    {
                        degree += w.Value;
                        score += w.Value * n.Of[w.Key].Rank;
                    }
                    if (degree < 3) continue;
                    float dist = n.At.magnitude;
                    if (score < bestScore || (score == bestScore && dist >= bestDist)) continue;
                    best = n;
                    bestScore = score;
                    bestDegree = degree;
                    bestDist = dist;
                }
                if (best == null) return null;
                var names = new List<string>();
                var streets = new List<SStreet>(best.Of.Values);
                streets.Sort((a, b) => b.Rank.CompareTo(a.Rank));
                foreach (SStreet st in streets)
                {
                    string label = StreetLabel(st);
                    if (!names.Contains(label)) names.Add(label);
                }
                return new Spot
                {
                    Found = true, At = best.At,
                    Title = "The busiest intersection: " + string.Join(" / ", names) + " (" + bestDegree + " ways in)",
                    Why = "the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through)",
                };
            }

            private static float GreenWeight(string kind)
            {
                switch (kind)
                {
                    case "park": case "cemetery": case "playground": case "pitch": case "golf": return 1f;
                    case "grass": case "meadow": case "orchard": return 0.5f;
                    default: return 0f;
                }
            }

            private Spot BestArea(Sample s, float inner, Func<string, float> weight, string what, string why)
            {
                var square = Rect.MinMaxRect(-inner, -inner, inner, inner);
                SArea best = null;
                List<Vector2> bestPts = null;
                float bestScore = 0f, bestArea = 0f;
                foreach (SArea a in s.Areas)
                {
                    float w = weight(a.Kind);
                    if (w <= 0f) continue;
                    List<Vector2> pts = Clip(a.Pts, square);
                    if (pts.Count < 3) continue;
                    float area = Mathf.Abs(SignedArea(pts));
                    if (area * w <= bestScore) continue;
                    best = a;
                    bestPts = pts;
                    bestScore = area * w;
                    bestArea = area;
                }
                if (best == null) return null;
                return new Spot
                {
                    Found = true, At = InteriorPoint(bestPts), Why = why,
                    Title = what + ": " + Describe(best.Kind, best.Name) + ", " + F0(bestArea) + " m² inside the square",
                };
            }

            private Spot Parking(Sample s, float inner)
            {
                Spot lot = BestArea(s, inner, k => k == "parking" ? 1f : 0f, "Parking", "the biggest parking area inside the square");
                if (lot != null) return lot;
                // No lot mapped: the commercial strip, where the most shops and offices stand along a main street.
                Spot best = null;
                int bestCount = 0;
                foreach (SStreet st in s.Streets)
                {
                    if (st.Rank < 2) continue;
                    for (int i = 0; i + 1 < st.Pts.Count; i++)
                    {
                        Vector2 a = st.Pts[i], b = st.Pts[i + 1];
                        float len = Vector2.Distance(a, b);
                        if (len < 1f || !st.Own.Contains((a + b) * 0.5f)) continue;
                        int n = Mathf.Max(1, Mathf.CeilToInt(len / 8f));
                        for (int k = 0; k < n; k++)
                        {
                            Vector2 p = Vector2.Lerp(a, b, (k + 0.5f) / n);
                            if (Mathf.Abs(p.x) > inner || Mathf.Abs(p.y) > inner) continue;
                            int count = 0;
                            foreach (SBuilding bl in s.Buildings)
                                if (IsShop(bl) && (bl.Mid - p).sqrMagnitude < 1225f) count++;
                            if (count <= bestCount) continue;
                            bestCount = count;
                            best = new Spot { Found = true, At = p, StreetName = StreetLabel(st) };
                        }
                    }
                }
                if (best == null) return null;
                best.Title = "Commercial strip: " + best.StreetName + ", " + bestCount + " shops and offices within 35 m";
                best.Why = "no parking area is mapped in the square: the point along a main street with the most shops and offices within 35 m";
                return best;
            }

            private Spot Edge(Sample s)
            {
                float h = _half;
                Spot best = null;
                float bestScore = float.MinValue;
                foreach (SStreet st in s.Streets)
                {
                    if (st.Rank < 2 || st.Tunnel) continue;
                    for (int i = 0; i + 1 < st.Pts.Count; i++)
                    {
                        Vector2 a = st.Pts[i], b = st.Pts[i + 1];
                        if (!st.Own.Contains((a + b) * 0.5f)) continue;
                        for (int side = 0; side < 4; side++)
                        {
                            if (!CrossEdge(a, b, h, side, out Vector2 p)) continue;
                            float score = st.Rank * 1000f - (side == 0 || side == 2 ? Mathf.Abs(p.x) : Mathf.Abs(p.y));
                            if (score <= bestScore) continue;
                            bestScore = score;
                            best = new Spot
                            {
                                Found = true, At = p, StreetName = StreetLabel(st),
                                Title = "The square's edge: where " + StreetLabel(st) + " leaves it (" + SideName(side)
                                    + "), the playable map giving way to the scenery ring",
                                Why = "the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits)",
                            };
                        }
                    }
                }
                return best ?? new Spot
                {
                    Found = true, At = new Vector2(0f, -h), Title = "The square's edge (south): the playable map giving way to the scenery ring",
                    Why = "no street leaves the square: the middle of its south edge",
                };
            }

            private Spot Waterway(Sample s, float inner)
            {
                Spot best = null;
                float bestDist = float.MaxValue;
                float[] limits = { inner, _half + 70f };   // inside the square first, then the scenery ring
                foreach (float limit in limits)
                {
                    foreach (SLine l in s.Lines)
                    {
                        bool rail = l.Kind == "rail" || l.Kind == "abandoned" || l.Kind == "disused";
                        bool water = l.Kind == "river" || l.Kind == "canal" || l.Kind == "stream";
                        if (!rail && !water) continue;
                        for (int i = 0; i + 1 < l.Pts.Count; i++)
                        {
                            Vector2 a = l.Pts[i], b = l.Pts[i + 1];
                            float len = Vector2.Distance(a, b);
                            if (len < 0.5f || !l.Own.Contains((a + b) * 0.5f)) continue;
                            int n = Mathf.Max(1, Mathf.CeilToInt(len / 4f));
                            for (int k = 0; k < n; k++)
                            {
                                Vector2 p = Vector2.Lerp(a, b, (k + 0.5f) / n);
                                if (Mathf.Abs(p.x) > limit || Mathf.Abs(p.y) > limit) continue;
                                float dist = p.magnitude;
                                if (dist >= bestDist) continue;
                                bestDist = dist;
                                best = new Spot { Found = true, At = p, Title = (rail ? "Railway: " : "Waterway: ") + Describe(l.Kind, l.Name) };
                            }
                        }
                    }
                    if (best != null)
                    {
                        best.Why = "the point of a railway, river, canal or stream nearest the square's middle"
                            + (limit > _half ? " (none inside the square: this one is in the scenery ring)" : "");
                        return best;
                    }
                }
                return BestArea(s, inner, k => k == "water" ? 1f : 0f, "Water", "no railway or river in reach: the biggest water area inside the square");
            }

            private Spot Landmark(Sample s, float inner)
            {
                SBuilding best = null;
                for (int pass = 0; pass < 2 && best == null; pass++)
                    foreach (SBuilding b in s.Buildings)
                    {
                        if (Mathf.Abs(b.Mid.x) > inner || Mathf.Abs(b.Mid.y) > inner) continue;
                        if (pass == 0 && !IsLandmark(b.Kind)) continue;
                        if (best == null || b.Area > best.Area) best = b;
                    }
                if (best == null) return null;
                return new Spot
                {
                    Found = true, At = best.Mid,
                    Title = "Landmark: the biggest building in the square, " + Describe(best.Kind, best.Name) + " (" + F0(best.Area) + " m² footprint)",
                    Why = "the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings",
                };
            }

            private static bool IsHome(string kind) => kind == "house" || kind == "apartments" || kind == "trailer";

            private static bool IsShop(SBuilding b) => b.Kind == "retail" || b.Kind == "commercial" || b.Kind == "office"
                || (b.Use != null && b.Use.StartsWith("shop:", StringComparison.Ordinal));

            private static bool IsLandmark(string kind)
            {
                switch (kind)
                {
                    case "church": case "school": case "warehouse": case "industrial": case "civic": case "hospital":
                    case "retail": case "commercial": case "office": case "hotel": case "apartments": return true;
                    default: return false;
                }
            }

            // KentuckyMap's own ranking of street kinds (how much a street counts as a way out of town).
            private static int Rank(string kind)
            {
                switch (kind)
                {
                    case "motorway": case "trunk": return 6;
                    case "primary": return 5;
                    case "secondary": return 4;
                    case "tertiary": return 3;
                    case "unclassified": case "residential": return 2;
                    case "service": case "track": return 1;
                    default: return 0;
                }
            }

            private static string StreetLabel(SStreet st) => !string.IsNullOrEmpty(st.Name) ? st.Name : "a " + st.Kind + " street";

            private static string Describe(string kind, string name) => string.IsNullOrEmpty(name) ? "a " + kind : kind + " '" + name + "'";

            private static string SideName(int side) => side == 0 ? "north" : side == 1 ? "east" : side == 2 ? "south" : "west";

            private static SStreet NearestStreet(Sample s, Vector2 p)
            {
                SStreet best = null;
                float bestDist = float.MaxValue;
                foreach (SStreet st in s.Streets)
                {
                    if (st.Rank < 1) continue;
                    for (int i = 0; i + 1 < st.Pts.Count; i++)
                    {
                        float d = SegmentDistance(p, st.Pts[i], st.Pts[i + 1]);
                        if (d >= bestDist) continue;
                        bestDist = d;
                        best = st;
                    }
                }
                return best;
            }
            #endregion
        }

        #region Plane geometry (map metres: x east, y = z north)
        private static List<Vector2> Local(int[] pts, double ox, double oz)
        {
            var list = new List<Vector2>(pts.Length / 2);
            for (int i = 0; i + 1 < pts.Length; i += 2) list.Add(new Vector2((float)(ox + pts[i] * 0.1), (float)(oz + pts[i + 1] * 0.1)));
            return list;
        }

        private static Rect Bounds(List<Vector2> pts)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (Vector2 p in pts)
            {
                x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y);
                x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y);
            }
            return pts.Count == 0 ? new Rect() : Rect.MinMaxRect(x0, z0, x1, z1);
        }

        private static Vector2 Average(List<Vector2> pts)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 p in pts) sum += p;
            return pts.Count > 0 ? sum / pts.Count : sum;
        }

        private static float SignedArea(List<Vector2> p)
        {
            double a = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) a += (double)p[j].x * p[i].y - (double)p[i].x * p[j].y;
            return (float)(a * 0.5);
        }

        private static Vector2 Centroid(List<Vector2> p)
        {
            double a = 0, cx = 0, cz = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            {
                double cross = (double)p[j].x * p[i].y - (double)p[i].x * p[j].y;
                a += cross;
                cx += (p[j].x + p[i].x) * cross;
                cz += (p[j].y + p[i].y) * cross;
            }
            if (Math.Abs(a) < 1e-6) return Average(p);
            return new Vector2((float)(cx / (3 * a)), (float)(cz / (3 * a)));
        }

        private static bool Inside(List<Vector2> p, Vector2 q)
        {
            bool c = false;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x) c = !c;
            return c;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        private static float EdgeDistance(List<Vector2> p, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) best = Mathf.Min(best, SegmentDistance(q, p[j], p[i]));
            return best;
        }

        // The centroid when it's well inside, else the inside point farthest from the outline (a coarse grid search).
        private static Vector2 InteriorPoint(List<Vector2> p)
        {
            Rect b = Bounds(p);
            float step = Mathf.Max(1.5f, Mathf.Max(b.width, b.height) / 32f);
            Vector2 best = Average(p);
            float bestDist = -1f;
            for (float z = b.yMin + step * 0.5f; z < b.yMax; z += step)
                for (float x = b.xMin + step * 0.5f; x < b.xMax; x += step)
                {
                    var q = new Vector2(x, z);
                    if (!Inside(p, q)) continue;
                    float d = EdgeDistance(p, q);
                    if (d <= bestDist) continue;
                    bestDist = d;
                    best = q;
                }
            Vector2 c = Centroid(p);
            return Inside(p, c) && EdgeDistance(p, c) >= bestDist * 0.5f ? c : best;
        }

        // Sutherland-Hodgman against an axis-aligned rectangle.
        private static List<Vector2> Clip(List<Vector2> poly, Rect r)
        {
            List<Vector2> o = poly;
            for (int edge = 0; edge < 4 && o.Count > 0; edge++)
            {
                List<Vector2> input = o;
                o = new List<Vector2>(input.Count + 4);
                for (int i = 0; i < input.Count; i++)
                {
                    Vector2 cur = input[i], prev = input[(i + input.Count - 1) % input.Count];
                    bool cin = In(cur, r, edge), pin = In(prev, r, edge);
                    if (cin)
                    {
                        if (!pin) o.Add(CrossAt(prev, cur, r, edge));
                        o.Add(cur);
                    }
                    else if (pin) o.Add(CrossAt(prev, cur, r, edge));
                }
            }
            return o;
        }

        private static bool In(Vector2 p, Rect r, int edge)
            => edge == 0 ? p.x >= r.xMin : edge == 1 ? p.x <= r.xMax : edge == 2 ? p.y >= r.yMin : p.y <= r.yMax;

        private static Vector2 CrossAt(Vector2 a, Vector2 b, Rect r, int edge)
        {
            float t;
            switch (edge)
            {
                case 0: t = (r.xMin - a.x) / (b.x - a.x); break;
                case 1: t = (r.xMax - a.x) / (b.x - a.x); break;
                case 2: t = (r.yMin - a.y) / (b.y - a.y); break;
                default: t = (r.yMax - a.y) / (b.y - a.y); break;
            }
            return Vector2.Lerp(a, b, t);
        }

        // Where segment a-b crosses a side of the square of half-side h (0 north, 1 east, 2 south, 3 west), if it does.
        private static bool CrossEdge(Vector2 a, Vector2 b, float h, int side, out Vector2 p)
        {
            p = default;
            bool alongZ = side == 1 || side == 3;   // the east and west sides are lines of constant x
            float c = side == 0 || side == 1 ? h : -h;
            float va = alongZ ? a.x : a.y, vb = alongZ ? b.x : b.y;
            if ((va - c) * (vb - c) > 0f || Mathf.Approximately(va, vb)) return false;
            p = Vector2.Lerp(a, b, (c - va) / (vb - va));
            return Mathf.Abs(alongZ ? p.y : p.x) <= h;
        }
        #endregion

        private sealed partial class Harness
        {
            #region Pictures of the square
            private void ShootSquare()
            {
                List<Transform> roots = MapRootTransforms();
                CollectCeilings(roots);
                CollectLights(roots);
                BuildMarks();
                Note(_ceilings.Count + " overhead renderers (hidden from the isometric view), " + _lights.Count + " map lights");
                float reach = _half + 96f;
                var shots = new List<Shot>
                {
                    new Shot
                    {
                        File = "00_overview.png", Kind = ShotKind.Top, Look = _day, Size = reach, W = 2048, H = 2048, Roofs = true, Marks = true,
                        Title = "Top-down overview of the whole start square (" + F0(_half * 2f) + " m, red outline) and its scenery ring, roofs on, "
                            + "north up; cyan = spawn, yellow = exits, magenta = the close-up spots",
                    },
                };
                if (!_o.Quick)
                    shots.Add(new Shot
                    {
                        File = "00_overview_night.png", Kind = ShotKind.Top, Look = _night, Size = reach, W = 2048, H = 2048, Roofs = true,
                        Marks = true, AllLights = true,
                        Title = "The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is",
                    });
                foreach (Spot s in _spots)
                {
                    if (!s.Found) continue;
                    bool opener = s.Id == "spawn";
                    if (_o.Quick && !opener && s.Id != "downtown") continue;
                    foreach (Look look in new[] { _day, _night })
                    {
                        if (_o.Quick && opener && look != _night) continue;
                        shots.Add(new Shot
                        {
                            File = "iso_" + s.Id + "_" + look.Name + ".png", Kind = ShotKind.Iso, Look = look, At = s.At,
                            Yaw = opener ? s.Yaw : _cn.IsoYaw, Size = opener ? _cn.Opening : _cn.Wide, Title = s.Title,
                        });
                    }
                }
                Spot spawn = _spots.Find(s => s.Id == "spawn" && s.Found);
                if (spawn != null)
                    foreach (Look look in new[] { _day, _night })
                    {
                        if (_o.Quick && look != _day) continue;
                        shots.Add(new Shot
                        {
                            File = "tp_downtown_" + look.Name + ".png", Kind = ShotKind.Shoulder, Look = look, At = spawn.At, Yaw = spawn.Yaw,
                            Survivor = false,
                            Title = "Third-person shoulder view from the raid's spawn on " + spawn.StreetName + " (downtown), looking along the street",
                        });
                    }
                Spot home = _spots.Find(s => s.Id == "residential" && s.Found);
                if (home != null && !_o.Quick)
                {
                    // On the sidewalk, facing along the street towards the middle of the map.
                    Vector2 dir = home.StreetDir.sqrMagnitude > 0.5f ? home.StreetDir : Vector2.up;
                    if (Vector2.Dot(dir, -home.At) < 0f) dir = -dir;
                    Vector2 left = new Vector2(-dir.y, dir.x);
                    Vector2 at = home.At + left * (Mathf.Clamp(home.StreetWidth, 3f, 20f) * 0.5f + 1.2f);
                    float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                    foreach (Look look in new[] { _day, _night })
                        shots.Add(new Shot
                        {
                            File = "tp_residential_" + look.Name + ".png", Kind = ShotKind.Shoulder, Look = look, At = at, Yaw = yaw,
                            Survivor = false,
                            Title = "Third-person shoulder view on the sidewalk of " + home.StreetName + " (the densest residential street), looking along it",
                        });
                }
                foreach (Shot shot in shots) Shoot(shot);
            }

            // Thin unlit marks high over the overview: the square's outline, the spawn, the exits and the spots.
            private void BuildMarks()
            {
                if (_marks != null) Object.DestroyImmediate(_marks);
                _marks = new GameObject("Marks");
                _marks.transform.SetParent(_rig.transform, false);
                const float y = 80f, w = 1.4f;
                float h = _half;
                Material red = UnlitMaterial(new Color(1f, 0.12f, 0.08f));
                Mark(new Vector3(0f, y, h), new Vector3(h * 2f + w, 0.2f, w), red);
                Mark(new Vector3(0f, y, -h), new Vector3(h * 2f + w, 0.2f, w), red);
                Mark(new Vector3(h, y, 0f), new Vector3(w, 0.2f, h * 2f + w), red);
                Mark(new Vector3(-h, y, 0f), new Vector3(w, 0.2f, h * 2f + w), red);
                if (_info != null)
                {
                    Material yellow = UnlitMaterial(new Color(1f, 0.85f, 0.1f));
                    foreach (RaidExit e in _info.Exits) Mark(new Vector3(e.Position.x, y, e.Position.z), new Vector3(7f, 0.2f, 7f), yellow);
                }
                Material magenta = UnlitMaterial(new Color(1f, 0.15f, 0.9f)), cyan = UnlitMaterial(new Color(0.1f, 0.9f, 1f));
                foreach (Spot s in _spots)
                    if (s.Found) Mark(new Vector3(s.At.x, y + 0.5f, s.At.y), new Vector3(5f, 0.2f, 5f), s.Id == "spawn" ? cyan : magenta);
                _marks.SetActive(false);
            }

            private void Mark(Vector3 at, Vector3 size, Material m)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(_marks.transform, false);
                go.transform.position = at;
                go.transform.localScale = size;
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            private void Shoot(Shot s)
            {
                var rep = new ShotReport { file = s.File, title = s.Title, look = s.Look.Name, x = s.At.x, z = s.At.y, width = s.W, height = s.H };
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                float shadowDistance = urp != null ? urp.shadowDistance : 0f;
                int cascades = urp != null ? urp.shadowCascadeCount : 0;
                try
                {
                    var survivor = new Vector3(s.At.x, 0f, s.At.y);
                    Vector3 facing = Quaternion.Euler(0f, s.Yaw, 0f) * Vector3.forward;
                    ApplyLook(s.Look, s.Kind, s.Size, survivor, facing);
                    ShowOverhead(s.Roofs || s.Kind == ShotKind.Shoulder);   // CeilingVisibility: hidden only in the isometric view
                    Budget(s.AllLights ? (Vector3?)null : survivor);
                    bool figure = s.Kind != ShotKind.Top && s.Survivor;
                    _figure.SetActive(figure);
                    if (figure)
                    {
                        _figure.transform.SetPositionAndRotation(survivor + Vector3.up * 0.9f, Quaternion.Euler(0f, s.Yaw, 0f));
                        // By night the sight light shines from inside the survivor, whose body the game keeps out of its shadows.
                        _figureRenderer.shadowCastingMode = s.Look.Sight ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    }
                    if (_marks != null) _marks.SetActive(s.Marks);
                    _plainVolume.enabled = s.Kind == ShotKind.Top;
                    switch (s.Kind)
                    {
                        case ShotKind.Top:
                            const float height = 320f;
                            _cam.transform.SetPositionAndRotation(new Vector3(s.At.x, height, s.At.y), Quaternion.Euler(90f, 0f, 0f));
                            _cam.orthographic = true;
                            _cam.orthographicSize = s.Size;
                            _cam.nearClipPlane = 1f;
                            _cam.farClipPlane = height + 40f;
                            // The game's 50 m shadow distance ends far above the ground from up here.
                            if (urp != null)
                            {
                                urp.shadowDistance = height + 40f;
                                urp.shadowCascadeCount = 1;
                            }
                            rep.camera = "top-down orthographic, " + F0(s.Size * 2f) + " m square, north up";
                            break;
                        case ShotKind.Iso:
                            PoseIso(survivor, s.Size);
                            rep.camera = "isometric: orthographic, yaw " + _cn.IsoYaw + ", pitch " + _cn.IsoPitch + ", zoom " + s.Size
                                + " (half-height, m), " + F1(_cn.IsoDistance(s.Size)) + " m from the survivor's chest";
                            break;
                        default:
                            PoseShoulder(survivor, s.Yaw);
                            rep.camera = "shoulder: perspective fov " + _cn.ShoulderFov + ", pitch " + _cn.ShoulderPitch + ", heading " + F0(Mathf.Repeat(s.Yaw, 360f));
                            break;
                    }
                    var sw = Stopwatch.StartNew();
                    Texture2D tex = Capture(s.W, s.H);
                    rep.renderMs = Ms(sw);
                    try
                    {
                        Measure(tex, rep);
                        File.WriteAllBytes(Path.Combine(_out, s.File), tex.EncodeToPNG());
                    }
                    finally
                    {
                        Object.DestroyImmediate(tex);
                    }
                    rep.ok = true;
                    Note("picture " + s.File + ": luma " + rep.meanLuma.ToString("0.000", CultureInfo.InvariantCulture) + ", "
                        + (rep.darkFraction * 100f).ToString("0", CultureInfo.InvariantCulture) + "% near-black, " + F0(rep.renderMs) + " ms");
                }
                catch (Exception e)
                {
                    _harnessErrors++;
                    rep.error = Brief(e);
                    Debug.LogError("[MapShots] picture " + s.File + " failed: " + e);
                }
                finally
                {
                    if (urp != null)
                    {
                        urp.shadowDistance = shadowDistance;
                        urp.shadowCascadeCount = cascades;
                    }
                }
                _r.shots.Add(rep);
            }

            // IsoCameraFollow.PoseIsometric: a fixed heading and pitch, centred on the survivor's chest, backed off with the zoom.
            private void PoseIso(Vector3 survivor, float size)
            {
                Quaternion rot = Quaternion.Euler(_cn.IsoPitch, _cn.IsoYaw, 0f);
                Vector3 focus = survivor + Vector3.up * _cn.LookHeight;
                _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * _cn.IsoDistance(size), rot);
                _cam.orthographic = true;
                _cam.orthographicSize = size;
                _cam.nearClipPlane = _cn.IsoNear;
                _cam.farClipPlane = _cn.Far;
            }

            // IsoCameraFollow.PoseThirdPerson as the view opens: over the right shoulder, pulled in short of anything solid.
            private void PoseShoulder(Vector3 survivor, float yaw)
            {
                Quaternion rot = Quaternion.Euler(_cn.ShoulderPitch, yaw, 0f);
                Vector3 pivot = survivor + Vector3.up * _cn.PivotHeight;
                Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * _cn.Shoulder;
                float full = offset.magnitude;
                Vector3 dir = full > 1e-4f ? offset / full : rot * Vector3.back;
                float allowed = full;
                Physics.SyncTransforms();
                if (Physics.SphereCast(pivot, _cn.CollisionRadius, dir, out RaycastHit hit, full, ~(1 << CloneZConfig.IgnoreRaycastLayer),
                        QueryTriggerInteraction.Ignore))
                    allowed = hit.distance;
                _cam.transform.SetPositionAndRotation(pivot + dir * allowed, rot);
                _cam.orthographic = false;
                _cam.fieldOfView = _cn.ShoulderFov;
                _cam.nearClipPlane = _cn.MouseNear;
                _cam.farClipPlane = _cn.Far;
            }

            private void ShowOverhead(bool show)
            {
                foreach (Renderer r in _ceilings)
                    if (r != null) r.shadowCastingMode = show ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            }

            // The lights as the game would have them around the survivor: RoomLight's budget (fittings past 45 m off, the four
            // nearest that want shadows get them) and the street lamps' 64 m cull. No survivor (the night overview): all on,
            // none shadowed.
            private void Budget(Vector3? survivor)
            {
                foreach (MapLight m in _lights)
                {
                    if (m.Light == null) continue;
                    m.Light.enabled = m.Enabled;
                    m.Light.shadows = m.Room != null || survivor == null ? LightShadows.None : m.Shadows;
                    if (m.Bulb != null) m.Bulb.forceRenderingOff = false;
                }
                if (survivor == null) return;
                Vector3 c = survivor.Value;
                var nearest = new List<KeyValuePair<float, MapLight>>();
                foreach (MapLight m in _lights)
                {
                    if (m.Light == null) continue;
                    float cull = m.Room != null ? RoomLight.CullDistance : 64f;
                    float d = (m.Light.transform.position - c).sqrMagnitude;
                    if (d > cull * cull)
                    {
                        m.Light.enabled = false;
                        if (m.Bulb != null) m.Bulb.forceRenderingOff = true;
                        continue;
                    }
                    if (m.Room != null && m.Room.CastShadows && m.Room.On) nearest.Add(new KeyValuePair<float, MapLight>(d, m));
                }
                nearest.Sort((a, b) => a.Key.CompareTo(b.Key));
                for (int i = 0; i < nearest.Count && i < RoomLight.MaxShadowedLights; i++) nearest[i].Value.Light.shadows = LightShadows.Soft;
            }

            private Texture2D Capture(int w, int h)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[MapShots]", antiAliasing = 1 };
                rt.Create();
                RenderTexture active = RenderTexture.active;
                try
                {
                    // Twice: the first frame of a new view settles anything first-use (shader variants, volume blending).
                    RenderInto(rt);
                    RenderInto(rt);
                    Texture2D tex = Read(rt, w, h);
                    if (!_pathConfirmed)
                    {
                        // The first picture proves the render path: a picture of one flat colour means nothing was drawn.
                        if (Uniform(tex))
                        {
                            _cameraRender = !_cameraRender;
                            Remark("The first picture came out one flat colour; switching to " + (_cameraRender ? "Camera.Render" : "a render request") + ".");
                            Object.DestroyImmediate(tex);
                            RenderInto(rt);
                            RenderInto(rt);
                            tex = Read(rt, w, h);
                            if (Uniform(tex)) Remark("It's still one flat colour: rendering doesn't work here (graphics device " + SystemInfo.graphicsDeviceType + ").");
                        }
                        _pathConfirmed = true;
                        Note("rendering through " + (_cameraRender ? "Camera.Render" : "RenderPipeline.SubmitRenderRequest"));
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

            private bool _cameraRender, _pathConfirmed;

            private static Texture2D Read(RenderTexture rt, int w, int h)
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false);
                return tex;
            }

            private static bool Uniform(Texture2D tex)
            {
                Color32 first = tex.GetPixel(0, 0);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        Color32 p = tex.GetPixel(x * tex.width / 32 + 3, y * tex.height / 32 + 3);
                        if (p.r != first.r || p.g != first.g || p.b != first.b) return false;
                    }
                return true;
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
                    _cam.targetTexture = rt;
                    try { _cam.Render(); }
                    finally { _cam.targetTexture = null; }
                }
                finally
                {
                    _cam.enabled = false;
                }
            }

            private static void Measure(Texture2D tex, ShotReport rep)
            {
                Color32[] px = tex.GetPixels32();
                double sum = 0;
                int dark = 0, clipped = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 p = px[i];
                    float l = (0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b) / 255f;
                    sum += l;
                    if (l < 0.03f) dark++;
                    if (p.r > 250 && p.g > 250 && p.b > 250) clipped++;
                }
                rep.meanLuma = (float)(sum / Math.Max(1, px.Length));
                rep.darkFraction = dark / (float)Math.Max(1, px.Length);
                rep.clippedFraction = clipped / (float)Math.Max(1, px.Length);
            }
            #endregion

            #region The game's light, by the game's own sums
            private static readonly Color DayAmbient = new Color(0.40f, 0.42f, 0.47f), NightAmbient = new Color(0.07f, 0.08f, 0.13f);
            private static readonly Color DayFog = new Color(0.55f, 0.60f, 0.66f), NightFog = new Color(0.04f, 0.05f, 0.08f);
            private static readonly Color DaySky = new Color(0.30f, 0.42f, 0.55f), NightSky = new Color(0.02f, 0.03f, 0.06f);
            private static readonly Color NoonSun = new Color(1f, 0.96f, 0.86f), DuskSun = new Color(1f, 0.62f, 0.36f);

            // GameBootstrapper.SetupEnvironment, then each frame: TimeOfDay.Apply (with VisionController's raid night scaling the
            // ambient and the moon), WeatherSystem.ApplyFog, ZoneAtmosphere (the zone's fog and tint, the sandbox's night
            // darkness) and IsoCameraFollow's fog anchoring; and VisionController's sight light and glow. Default settings.
            private void ApplyLook(Look look, ShotKind kind, float isoSize, Vector3 survivor, Vector3 facing)
            {
                float hour = look.Hour;
                float elevation = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
                float weatherDim = Mathf.Lerp(1f, 0.5f, look.Cloud);
                float level = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);
                float night = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.55f, level));
                float ambientScale = Mathf.Lerp(1f, 0.25f, night), moonScale = Mathf.Lerp(1f, 0.3f, night);
                float warmth = Mathf.Clamp01(elevation * 1.6f);

                _sun.transform.rotation = Quaternion.Euler((hour - 6f) / 12f * 180f, -35f + hour / 24f * 30f, 0f);
                _sun.color = Color.Lerp(DuskSun, NoonSun, warmth);
                _sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * moonScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                _sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
                Color ambient = Color.Lerp(NightAmbient, DayAmbient, daylight) * Mathf.Lerp(0.7f, 1f, weatherDim) * ambientScale;
                Color fog = Color.Lerp(NightFog, DayFog, daylight) * Mathf.Lerp(0.75f, 1f, weatherDim) * ambientScale;
                _cam.backgroundColor = Color.Lerp(NightSky, DaySky, daylight) * Mathf.Lerp(0.6f, 1f, weatherDim) * ambientScale;

                float murk = Mathf.Max(look.WeatherFog, look.Precip * 0.35f);
                float start = Mathf.Lerp(35f, 6f, murk), end = Mathf.Lerp(78f, 28f, murk);
                float zoneFog = Mathf.Clamp01(_zone.FogDensity * (1f - _settings.FogReadability * 0.75f));
                end = Mathf.Lerp(end, Mathf.Min(end, 24f), zoneFog);
                start = Mathf.Lerp(start, 1.5f, zoneFog);
                if (_settings.FogReadability > 0f) end = Mathf.Max(end, Mathf.Lerp(end, 70f, _settings.FogReadability * 0.5f));
                if (level < 0.35f) ambient *= Mathf.Lerp(1.15f, 0.4f, _sandbox.NightDarkness);
                ambient = Tint(ambient, _zone.Tint);
                fog = Tint(fog, _zone.Tint);
                if (kind == ShotKind.Iso && _cn.CompensateFog)
                {
                    float shift = _cn.IsoDistance(isoSize) - _cn.FogBaseline;
                    start = Mathf.Max(0f, start + shift);
                    end = Mathf.Max(start + 1f, end + shift);
                }
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = look.Fog && kind != ShotKind.Top;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = fog;
                RenderSettings.fogStartDistance = start;
                RenderSettings.fogEndDistance = end;

                // VisionController: a shadowed spot at the eye aimed where the survivor faces, and a glow from overhead.
                bool sight = look.Sight && kind != ShotKind.Top && night > 0.01f;
                _sight.enabled = sight;
                _periphery.enabled = sight;
                if (!sight) return;
                Vector3 eye = survivor + Vector3.up * 1.62f + facing * 0.08f;
                _sight.transform.SetPositionAndRotation(eye - facing * 0.25f, Quaternion.LookRotation(facing, Vector3.up));
                _sight.intensity = 16f * night;
                _periphery.transform.position = survivor + Vector3.up * 4f;
                _periphery.intensity = 1.2f * night;
            }

            private static Color Tint(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
            #endregion
        }

        private sealed class Target
        {
            public string Label, Place;
            public double X, Z;
        }

        private sealed class Fingerprint
        {
            public int Objects;
            public long Vertices, Triangles;
            public ulong Ordered = 14695981039346656037UL, Unordered;
            /// <summary>Every mesh in order: where it is in the piece, and its own hash.</summary>
            public readonly List<string> Names = new List<string>();
            public readonly List<ulong> Hashes = new List<ulong>();
        }

        private sealed partial class Harness
        {
            #region Streamed chunks
            private Transform _streamed, _overheadRoot;

            // The streamer's two parents: the pieces under the world, their overhead under a root with CeilingVisibility.
            private void ChunkParents()
            {
                _streamed = new GameObject("Streamed").transform;
                var overhead = new GameObject("RaidOverhead");
                overhead.AddComponent<CeilingVisibility>();
                _overheadRoot = overhead.transform;
            }

            private void DropChunkParents()
            {
                if (_streamed != null) Object.DestroyImmediate(_streamed.gameObject);
                if (_overheadRoot != null) Object.DestroyImmediate(_overheadRoot.gameObject);
                _streamed = _overheadRoot = null;
                CeilingVisibility.Clear();
                Shelter.Clear();
            }

            private static int ChunkOf(double metres) => (int)Math.Floor(metres / MapApi.ChunkSize);

            private void ChunkStress()
            {
                ChunkParents();
                try
                {
                    var targets = new List<Target> { new Target { Label = "start-centre", Place = _place.name, X = _place.x, Z = _place.z } };
                    Target rural = Rural();
                    if (rural != null) targets.Add(rural);
                    AddPlace(targets, "louisville-downtown", "Louisville");
                    AddPlace(targets, "lexington-downtown", "Lexington");
                    AddPlace(targets, "hazard", "Hazard");
                    foreach (Target t in targets) _r.chunks.Add(OneChunk(t.Label, t.Place, ChunkOf(t.X), ChunkOf(t.Z), t.X, t.Z, null));
                }
                finally
                {
                    DropChunkParents();
                }
            }

            private void AddPlace(List<Target> into, string label, string name)
            {
                MapPlace p = _build.FindPlace(name);
                if (p == null)
                {
                    Remark("Chunk test " + label + " skipped: build " + _build.Id + " has no place called " + name + ".");
                    return;
                }
                into.Add(new Target { Label = label, Place = p.name, X = p.x, Z = p.z });
            }

            // Farm country about 5 km out of the start town: of the chunks on that ring (every 10°), the one with a road and a
            // handful of buildings (a farmstead), not a village.
            private Target Rural()
            {
                Target best = null;
                int bestScore = int.MinValue;
                for (int i = 0; i < 36; i++)
                {
                    double a = i * 10.0 * Math.PI / 180.0;
                    int cx = ChunkOf(_place.x + Math.Sin(a) * 5000.0), cz = ChunkOf(_place.z + Math.Cos(a) * 5000.0);
                    MapApi.PrepareRegion(_build, MapApi.RegionOf(cx), MapApi.RegionOf(cz));
                    int[] b = MapApi.Bucket(cx, cz);
                    int streets = b[0], owned = b[2], areas = b[4];
                    if (streets == 0 || owned > 12) continue;
                    int score = Math.Min(owned, 6) * 3 + Math.Min(streets, 4) + (areas > 0 ? 2 : 0);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = new Target
                    {
                        Label = "rural-5km", Place = "5 km from " + _place.name + " (bearing " + (i * 10) + "°)",
                        X = (cx + 0.5) * MapApi.ChunkSize, Z = (cz + 0.5) * MapApi.ChunkSize,
                    };
                }
                if (best == null) Remark("No chunk with a road 5 km from " + _place.name + ": the rural chunk test is skipped.");
                return best;
            }

            private ChunkReport OneChunk(string label, string place, int cx, int cz, double originX, double originZ, List<MapApi.Chunk> keep)
            {
                var rep = new ChunkReport { label = label, place = place, cx = cx, cz = cz, rx = MapApi.RegionOf(cx), rz = MapApi.RegionOf(cz) };
                MapApi.RegionTiming t = MapApi.PrepareRegion(_build, rep.rx, rep.rz);
                rep.regionMissing = !t.Exists;
                rep.regionReadMs = Math.Round(t.ReadMs, 2);
                rep.regionIndexMs = Math.Round(t.IndexMs, 2);
                int[] b = MapApi.Bucket(cx, cz);
                rep.bucketStreets = b[0];
                rep.bucketLines = b[1];
                rep.bucketOwned = b[2];
                rep.bucketForeign = b[3];
                rep.bucketAreas = b[4];
                rep.bucketPois = b[5];
                var middle = new Vector3((float)((cx + 0.5) * MapApi.ChunkSize - originX), 0f, (float)((cz + 0.5) * MapApi.ChunkSize - originZ));
                MapApi.Chunk c;
                Begin(rep.log);
                try
                {
                    c = MapApi.BuildChunk(_build, _place.name, cx, cz, _zone.LayoutSeed, middle, _streamed, _overheadRoot);
                }
                finally
                {
                    End();
                }
                if (c.Failed)
                {
                    _mapErrors++;
                    rep.failed = true;
                    rep.error = c.Error;
                }
                rep.steps = Aggregate(c.Steps);
                rep.totalMs = Math.Round(c.TotalMs, 2);
                rep.activateMs = Math.Round(c.ActivateMs, 2);
                foreach (StepTiming s in c.Steps)
                    if (s.ms > rep.slowestMs)
                    {
                        rep.slowestMs = Math.Round(s.ms, 2);
                        rep.slowestStep = s.name;
                    }
                rep.counts = Count(label, new[] { c.Root.transform, c.Overhead.transform });
                rep.buildings = c.Buildings;
                rep.containers = c.Containers;
                rep.trees = c.Trees;
                rep.lights = c.Lights;
                Note("chunk " + label + " " + cx + "_" + cz + ": " + F0(rep.totalMs) + " ms, slowest step " + rep.slowestStep + " "
                    + F0(rep.slowestMs) + " ms, " + F0(rep.counts.gameObjects) + " objects, " + F0(rep.counts.vertices) + " vertices"
                    + (c.Failed ? "; FAILED: " + c.Error : ""));
                if (keep != null) keep.Add(c);
                else MapApi.Unload(c);
                return rep;
            }

            private void LouisvilleBlock()
            {
                BlockReport block = _r.louisvilleBlock;
                MapPlace lou = _build.FindPlace("Louisville");
                if (lou == null)
                {
                    Remark("No Louisville in build " + _build.Id + ": the 3x3 block is skipped.");
                    return;
                }
                block.place = lou.name;
                int cx0 = ChunkOf(lou.x), cz0 = ChunkOf(lou.z);
                block.centreCx = cx0;
                block.centreCz = cz0;
                ChunkParents();
                var kept = new List<MapApi.Chunk>();
                try
                {
                    // Nearest first, as the streamer builds them.
                    int[] dx = { 0, 1, -1, 0, 0, 1, -1, 1, -1 }, dz = { 0, 0, 0, 1, -1, 1, 1, -1, -1 };
                    for (int i = 0; i < dx.Length; i++)
                    {
                        ChunkReport rep = OneChunk("louisville " + dx[i] + "," + dz[i], lou.name, cx0 + dx[i], cz0 + dz[i], lou.x, lou.z, kept);
                        block.chunks.Add(rep);
                        block.totalMs = Math.Round(block.totalMs + rep.totalMs, 2);
                        block.maxChunkMs = Math.Max(block.maxChunkMs, rep.totalMs);
                    }
                    var roots = new List<Transform> { _streamed, _overheadRoot };
                    block.counts = Count("Louisville 3x3", roots);
                    block.built = true;
                    CollectCeilings(roots);
                    CollectLights(roots);

                    // In map metres from Louisville's point: the block's middle, and the centre chunk's corner nearest the point,
                    // where four chunks meet.
                    float size = MapApi.ChunkSize;
                    var middle = new Vector2((float)((cx0 + 0.5) * size - lou.x), (float)((cz0 + 0.5) * size - lou.z));
                    double fx = lou.x / size - cx0, fz = lou.z / size - cz0;
                    var corner = new Vector2((float)((cx0 + (fx < 0.5 ? 0 : 1)) * size - lou.x), (float)((cz0 + (fz < 0.5 ? 0 : 1)) * size - lou.z));
                    float half = size * 1.5f + 6f;
                    float px = 2048f / (half * 2f);
                    block.seams = "in chunks_louisville_top.png the seams are the lines x = " + F0((half - size * 0.5f) * px) + " and "
                        + F0((half + size * 0.5f) * px) + " px, y = the same (from the top left); the iso pictures are centred on the corner "
                        + "where four chunks meet (" + F1(corner.x) + ", " + F1(corner.y) + ")";
                    Shoot(new Shot
                    {
                        File = "chunks_louisville_top.png", Kind = ShotKind.Top, Look = _day, At = middle, Size = half, W = 2048, H = 2048, Roofs = true,
                        Title = "Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up",
                    });
                    foreach (Look look in new[] { _day, _night })
                        Shoot(new Shot
                        {
                            File = "chunks_louisville_iso_" + look.Name + ".png", Kind = ShotKind.Iso, Look = look, At = corner, Yaw = _cn.IsoYaw,
                            Size = _cn.Wide, Title = "Streamed look: downtown Louisville in the isometric view, centred where four chunks meet",
                        });
                }
                finally
                {
                    foreach (MapApi.Chunk k in kept) MapApi.Unload(k);
                    DropChunkParents();
                    _ceilings.Clear();
                    _lights.Clear();
                }
            }

            // The start town's middle chunk, built twice: the vertex and triangle counts and a hash of every vertex must agree.
            private void Determinism()
            {
                DeterminismReport d = _r.determinism;
                int cx = ChunkOf(_place.x), cz = ChunkOf(_place.z);
                d.label = "start-centre";
                d.cx = cx;
                d.cz = cz;
                ChunkParents();
                try
                {
                    Fingerprint a = BuildPrint(cx, cz), b = BuildPrint(cx, cz);
                    d.ran = true;
                    d.objectsA = a.Objects;
                    d.objectsB = b.Objects;
                    d.verticesA = a.Vertices;
                    d.verticesB = b.Vertices;
                    d.trianglesA = a.Triangles;
                    d.trianglesB = b.Triangles;
                    d.orderedHashA = a.Ordered.ToString("x16");
                    d.orderedHashB = b.Ordered.ToString("x16");
                    d.unorderedHashA = a.Unordered.ToString("x16");
                    d.unorderedHashB = b.Unordered.ToString("x16");
                    d.match = a.Objects == b.Objects && a.Vertices == b.Vertices && a.Triangles == b.Triangles && a.Unordered == b.Unordered;
                    if (a.Hashes.Count != b.Hashes.Count) d.differences.Add("mesh count " + a.Hashes.Count + " vs " + b.Hashes.Count);
                    else
                        for (int i = 0; i < a.Hashes.Count; i++)
                        {
                            if (a.Hashes[i] == b.Hashes[i]) continue;
                            d.differingMeshes++;
                            if (d.differences.Count < 12) d.differences.Add(a.Names[i] + (a.Names[i] != b.Names[i] ? " vs " + b.Names[i] : ""));
                        }
                    d.meshes = a.Hashes.Count;
                    if (d.match) d.detail = a.Ordered == b.Ordered ? "identical, in the same order" : "the same geometry, emitted in a different order";
                    else
                    {
                        d.detail = "objects " + a.Objects + " vs " + b.Objects + ", vertices " + a.Vertices + " vs " + b.Vertices
                            + ", triangles " + a.Triangles + " vs " + b.Triangles + "; " + d.differingMeshes + " of " + d.meshes
                            + " meshes differ (positions to the millimetre)";
                        Remark("Determinism: chunk " + cx + "_" + cz + " came out different the second time (" + d.detail + "), first: "
                            + string.Join("; ", d.differences));
                    }
                    Note("determinism: " + (d.match ? "" : "MISMATCH: ") + d.detail);
                }
                finally
                {
                    DropChunkParents();
                }
            }

            private Fingerprint BuildPrint(int cx, int cz)
            {
                var middle = new Vector3((float)((cx + 0.5) * MapApi.ChunkSize - _place.x), 0f, (float)((cz + 0.5) * MapApi.ChunkSize - _place.z));
                MapApi.Chunk c;
                Begin(new LogReport());
                try
                {
                    c = MapApi.BuildChunk(_build, _place.name, cx, cz, _zone.LayoutSeed, middle, _streamed, _overheadRoot);
                }
                finally
                {
                    End();
                }
                try
                {
                    if (c.Failed) _mapErrors++;
                    return Print(c.Root.transform, c.Overhead.transform);
                }
                finally
                {
                    MapApi.Unload(c);
                }
            }

            // FNV-1a over every mesh's place in the piece and its vertices to the millimetre: in order, and as an
            // order-free sum of the meshes' own hashes.
            private static Fingerprint Print(params Transform[] roots)
            {
                var f = new Fingerprint();
                var verts = new List<Vector3>();
                foreach (Transform root in roots)
                {
                    f.Objects += root.GetComponentsInChildren<Transform>(true).Length;
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        string path = PathOf(mf.transform, root);
                        ulong h = Mix(14695981039346656037UL, path);
                        Vector3 at = root.InverseTransformPoint(mf.transform.position);
                        h = Mix(Mix(Mix(h, Q(at.x)), Q(at.y)), Q(at.z));
                        Mesh m = mf.sharedMesh;
                        int count = 0;
                        if (m != null)
                        {
                            count = m.vertexCount;
                            f.Vertices += count;
                            f.Triangles += Triangles(m);
                            h = Mix(h, (ulong)count);
                            if (m.isReadable)
                            {
                                m.GetVertices(verts);
                                foreach (Vector3 v in verts) h = Mix(Mix(Mix(h, Q(v.x)), Q(v.y)), Q(v.z));
                            }
                        }
                        f.Names.Add((root == roots[0] ? "root" : "overhead") + path + " (" + count + " vertices at " + F1(at.x) + ", " + F1(at.z) + ")");
                        f.Hashes.Add(h);
                        f.Ordered = Mix(f.Ordered, h);
                        unchecked { f.Unordered += h; }
                    }
                }
                return f;
            }

            private static ulong Q(float v) => unchecked((ulong)(long)Mathf.RoundToInt(v * 1000f));

            private static ulong Mix(ulong h, ulong v)
            {
                unchecked
                {
                    for (int i = 0; i < 8; i++)
                    {
                        h ^= (v >> (i * 8)) & 0xff;
                        h *= 1099511628211UL;
                    }
                }
                return h;
            }

            private static ulong Mix(ulong h, string s)
            {
                unchecked
                {
                    foreach (char ch in s)
                    {
                        h ^= ch;
                        h *= 1099511628211UL;
                    }
                }
                return h;
            }

            private static string PathOf(Transform t, Transform root)
            {
                var sb = new StringBuilder();
                for (; t != null && t != root; t = t.parent) sb.Insert(0, "/" + t.name);
                return sb.ToString();
            }
            #endregion
        }

        private sealed partial class Harness
        {
            #region The report
            private void Finish()
            {
                int code;
                string reason;
                if (_noWorld != null)
                {
                    code = ExitNoWorld;
                    reason = _noWorld;
                }
                else if (_mapErrors > 0 || (_squareAttempted && !_squareBuilt))
                {
                    code = ExitMap;
                    reason = _squareAttempted && !_squareBuilt ? "KentuckyMap.Build failed: " + _r.square.why
                        : _mapErrors + " error(s) logged or thrown by the map code while building (see the logs below)";
                    if (_harnessErrors > 0) reason += "; also " + _harnessErrors + " harness failure(s)";
                }
                else if (_harnessErrors > 0 || !_squareAttempted)
                {
                    code = ExitHarness;
                    reason = _harnessErrors + " harness failure(s): see the stages below and unity.log";
                }
                else
                {
                    code = ExitOk;
                    reason = "ok";
                }
                _r.exitCode = code;
                _r.exitReason = reason;
                _r.totalSeconds = Math.Round(_clock.Elapsed.TotalSeconds, 1);
                foreach (MapApi.RegionTiming t in MapApi.Loaded)
                    _r.regions.Add(new RegionReport { rx = t.Rx, rz = t.Rz, exists = t.Exists, readMs = Math.Round(t.ReadMs, 2), indexMs = Math.Round(t.IndexMs, 2) });
                try
                {
                    File.WriteAllText(Path.Combine(_out, "report.json"), JsonUtility.ToJson(_r, true));
                }
                catch (Exception e)
                {
                    Debug.LogError("[MapShots] couldn't write report.json: " + e);
                }
                try
                {
                    File.WriteAllText(Path.Combine(_out, "report.md"), Markdown());
                }
                catch (Exception e)
                {
                    Debug.LogError("[MapShots] couldn't write report.md: " + e);
                }
                Debug.Log("[MapShots] exit " + code + " (" + reason + ") after " + F1(_r.totalSeconds) + " s");
            }

            private string Markdown()
            {
                var md = new StringBuilder();
                string place = _place != null ? _place.name : _r.place;
                md.Append("# CLONE-Z map harness: ").Append(place).Append(" (build ").Append(_r.buildId ?? "?").Append(")\n\n");
                md.Append("**Exit ").Append(_r.exitCode).Append("**: ").Append(Esc(_r.exitReason)).Append(". ")
                    .Append(_o.Quick ? "Quick run" : "Full run").Append(", ").Append(F1(_r.totalSeconds)).Append(" s, started ")
                    .Append(_r.started).Append(".  \nUnity ").Append(_r.unity).Append(", ").Append(Esc(_r.graphics)).Append(". World store `")
                    .Append(_r.worldStore).Append("`. ").Append(Esc(_r.attribution)).Append("\n\n");

                SquareReport sq = _r.square;
                md.Append("## Start square: `KentuckyMap.Build`\n\nThe fixed ").Append(F0(_half * 2f)).Append(" m square around ").Append(place)
                    .Append(" plus its scenery ring, built at once in edit mode from a copy of zone `").Append(_r.zone)
                    .Append("` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: ")
                    .Append("see the Louisville pictures for that look.)\n\n| | |\n|---|---|\n");
                Row(md, "built", !_squareAttempted ? "not run" : sq.built ? "yes" : "**no**: " + Esc(sq.why));
                Row(md, "build time (cold: the first map built in the process)", F0(sq.buildMs) + " ms");
                if (!_o.Quick) Row(md, "the same square again, warm, pass by pass", F0(_r.squarePassesTotalMs) + " ms");
                Row(md, "logged while building", sq.log.errors + " errors, " + sq.log.warnings + " warnings, " + sq.log.editModeArtefacts + " edit-mode artefacts");
                Row(md, "spawn", F1(sq.spawnX) + ", " + F1(sq.spawnZ) + ", facing " + F0(sq.spawnYaw) + "°");
                Row(md, "exits", Esc(string.Join("; ", sq.exits)));
                Row(md, "overhead renderers (hidden in the iso view)", sq.ceilingRenderers.ToString(CultureInfo.InvariantCulture));
                Row(md, "`KentuckyMap.LastStats`", Esc(sq.lastStats));
                if (sq.dataBuildingsInside + sq.dataBuildingsRing + sq.dataStreetPieces > 0)
                {
                    Row(md, "map data: buildings", sq.dataBuildingsInside + " with their middle inside the square (" + sq.dataBuildingsAtEdge
                        + " of them reach within 1 m of its edge or past it), " + sq.dataBuildingsRing + " in the scenery ring; inside: "
                        + Esc(sq.dataBuildingKinds));
                    Row(md, "map data: streets, lines, areas", sq.dataStreetPieces + " street pieces, " + sq.dataLines + " lines ("
                        + Esc(sq.dataLineKinds) + "), " + sq.dataAreas + " land-use areas (" + Esc(sq.dataAreaKinds) + ") within reach");
                }
                md.Append('\n');
                if (sq.roots.Count > 0)
                {
                    CountsHeader(md, "root");
                    foreach (SceneCounts c in sq.roots) CountsRow(md, c.name, c);
                    CountsRow(md, "**all**", sq.total);
                    md.Append('\n');
                }

                if (_r.squarePasses.Count > 0 || _r.squarePassesError != null)
                {
                    md.Append("### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)\n\n");
                    if (_r.squarePassesError != null) md.Append("**The map code threw:** ").Append(Esc(_r.squarePassesError)).Append("\n\n");
                    md.Append("| step | ms | share | runs | slowest run ms |\n|---|--:|--:|--:|--:|\n");
                    foreach (StepTiming s in _r.squarePasses)
                        md.Append("| ").Append(Esc(s.name)).Append(" | ").Append(F0(s.ms)).Append(" | ")
                            .Append(_r.squarePassesTotalMs > 0 ? (s.ms / _r.squarePassesTotalMs * 100).ToString("0", CultureInfo.InvariantCulture) + "%" : "")
                            .Append(" | ").Append(s.count).Append(" | ").Append(F0(s.maxMs)).Append(" |\n");
                    md.Append("| **total** | **").Append(F0(_r.squarePassesTotalMs)).Append("** | | | |\n\n");
                    LogSection(md, "Logged during the pass-by-pass build", _r.squarePassesLog);
                }

                if (_r.chunks.Count > 0 || _r.louisvilleBlock.chunks.Count > 0)
                {
                    md.Append("## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)\n\n");
                    md.Append("Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). ")
                        .Append("Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).\n\n");
                    ChunkTable(md, _r.chunks);
                    foreach (ChunkReport c in _r.chunks) StepsTable(md, c);
                }
                if (_r.regions.Count > 0)
                {
                    md.Append("### Regions read (8 km files; the streamer reads and sorts them off the main thread)\n\n| region | data | read ms | sort into chunks ms |\n|---|---|--:|--:|\n");
                    foreach (RegionReport g in _r.regions)
                        md.Append("| ").Append(g.rx).Append('_').Append(g.rz).Append(" | ").Append(g.exists ? "yes" : "none (open snow)").Append(" | ")
                            .Append(F0(g.readMs)).Append(" | ").Append(F0(g.indexMs)).Append(" |\n");
                    md.Append('\n');
                }
                BlockReport block = _r.louisvilleBlock;
                if (block.chunks.Count > 0)
                {
                    md.Append("### Louisville 3x3 (centre chunk ").Append(block.centreCx).Append('_').Append(block.centreCz).Append(")\n\n");
                    md.Append("Nine chunks in ").Append(F0(block.totalMs)).Append(" ms (slowest ").Append(F0(block.maxChunkMs)).Append(" ms); ")
                        .Append(Esc(block.seams)).Append(".\n\n");
                    ChunkTable(md, block.chunks);
                    CountsHeader(md, "block");
                    CountsRow(md, "Louisville 3x3", block.counts);
                    md.Append('\n');
                }
                DeterminismReport d = _r.determinism;
                if (d.ran)
                {
                    md.Append("### Determinism\n\nChunk ").Append(d.cx).Append('_').Append(d.cz).Append(" (").Append(d.label).Append(") built twice: **")
                        .Append(d.match ? "match" : "MISMATCH").Append("**: ").Append(Esc(d.detail)).Append(" (").Append(F0(d.verticesA))
                        .Append(" vertices, ").Append(F0(d.trianglesA)).Append(" triangles, ").Append(F0(d.objectsA)).Append(" objects, ")
                        .Append(d.meshes).Append(" meshes; hash ").Append(d.unorderedHashA).Append(" vs ").Append(d.unorderedHashB).Append(").\n\n");
                    if (d.differences.Count > 0)
                    {
                        md.Append("Meshes that came out different (first ").Append(d.differences.Count).Append("):\n\n");
                        foreach (string line in d.differences) md.Append("- `").Append(line.Replace("`", "'")).Append("`\n");
                        md.Append('\n');
                    }
                }

                md.Append("## Pictures\n\nNight = the game's look as a raid starts (").Append(F0(_zone != null ? _zone.StartHour : 22f))
                    .Append(":00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). ")
                    .Append("Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at ")
                    .Append("its widest zoom (").Append(_cn.Wide).Append(" m half-height) except the spawn (the game's opening zoom, ").Append(_cn.Opening)
                    .Append("), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. ")
                    .Append("Roofs and upper storeys are hidden in the isometric view, as in the game.\n\n");
                md.Append("| file | shows | mean luma | near-black | ms |\n|---|---|--:|--:|--:|\n");
                foreach (ShotReport s in _r.shots)
                    md.Append("| ").Append(s.ok ? "`" + s.file + "`" : "~~" + s.file + "~~ " + Esc(s.error)).Append(" | ").Append(Esc(s.title))
                        .Append(" | ").Append(s.meanLuma.ToString("0.000", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append((s.darkFraction * 100f).ToString("0", CultureInfo.InvariantCulture)).Append("% | ").Append(F0(s.renderMs)).Append(" |\n");
                md.Append('\n');
                if (_r.spots.Count > 0)
                {
                    md.Append("### Spots (map metres from the place: x east, z north)\n\n| spot | x, z | why |\n|---|---|---|\n");
                    foreach (SpotReport s in _r.spots)
                        md.Append("| ").Append(s.id).Append(" | ").Append(s.found ? F1(s.x) + ", " + F1(s.z) : "none").Append(" | ")
                            .Append(Esc(s.why)).Append(" |\n");
                    md.Append('\n');
                }

                LogSection(md, "Logged while `KentuckyMap.Build` ran", sq.log);
                LogSection(md, "Everything logged during the run", _r.runLog);
                md.Append("## Stages\n\n| stage | ok | ms | error |\n|---|---|--:|---|\n");
                foreach (StageReport s in _r.stages)
                    md.Append("| ").Append(s.name).Append(" | ").Append(s.ok ? "yes" : "**no**").Append(" | ").Append(F0(s.ms)).Append(" | ")
                        .Append(Esc(s.error)).Append(" |\n");
                md.Append('\n');
                if (_r.notes.Count > 0)
                {
                    md.Append("## Notes\n\n");
                    foreach (string n in _r.notes) md.Append("- ").Append(n).Append('\n');
                    md.Append('\n');
                }
                md.Append("## Limits of this harness\n\n")
                    .Append("- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, ")
                    .Append("fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); ")
                    .Append("shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).\n")
                    .Append("- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), ")
                    .Append("replicated rather than run; the pictures add SMAA, which the game doesn't use.\n")
                    .Append("- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component ")
                    .Append("callbacks, so activation costs are lower than in play mode.\n");
                return md.ToString();
            }

            private static void Row(StringBuilder md, string a, string b) => md.Append("| ").Append(a).Append(" | ").Append(b).Append(" |\n");

            private static void CountsHeader(StringBuilder md, string first)
            {
                md.Append("| ").Append(first).Append(" | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials ")
                    .Append("| material slots | lights (shadowed) | colliders | shadow casters | loot containers |\n")
                    .Append("|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|\n");
            }

            private static void CountsRow(StringBuilder md, string name, SceneCounts c)
            {
                md.Append("| ").Append(Esc(name)).Append(" | ").Append(F0(c.gameObjects)).Append(" | ").Append(F0(c.renderers)).Append(" | ")
                    .Append(F0(c.meshFilters)).Append(" | ").Append(F0(c.distinctMeshes)).Append(" | ").Append(F0(c.vertices)).Append(" | ")
                    .Append(F0(c.triangles)).Append(" | ").Append(F0(c.distinctMaterials)).Append(" | ").Append(F0(c.materialSlots)).Append(" | ")
                    .Append(F0(c.lights)).Append(" (").Append(F0(c.lightsShadowed)).Append(") | ").Append(F0(c.colliders)).Append(" | ")
                    .Append(F0(c.shadowCasters)).Append(" | ").Append(F0(c.lootContainers)).Append(" |\n");
            }

            private static void ChunkTable(StringBuilder md, List<ChunkReport> chunks)
            {
                md.Append("| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers ")
                    .Append("| vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |\n")
                    .Append("|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|\n");
                foreach (ChunkReport c in chunks)
                    md.Append("| ").Append(Esc(c.label)).Append(c.failed ? " **FAILED**" : "").Append(" | ").Append(Esc(c.place)).Append(" | ")
                        .Append(c.cx).Append('_').Append(c.cz).Append(" (").Append(c.rx).Append('_').Append(c.rz).Append(c.regionMissing ? ", no data" : "")
                        .Append(") | ").Append(F0(c.regionReadMs)).Append(" + ").Append(F0(c.regionIndexMs)).Append(" | **").Append(F0(c.totalMs))
                        .Append("** | ").Append(Esc(c.slowestStep)).Append(' ').Append(F0(c.slowestMs)).Append(" | ").Append(F0(c.activateMs)).Append(" | ")
                        .Append(F0(c.counts.gameObjects)).Append(" | ").Append(F0(c.counts.renderers)).Append(" | ").Append(F0(c.counts.vertices))
                        .Append(" | ").Append(F0(c.counts.triangles)).Append(" | ").Append(c.counts.lights).Append(" | ").Append(F0(c.counts.colliders))
                        .Append(" | ").Append(c.buildings).Append(" | ").Append(c.bucketStreets).Append('/').Append(c.bucketOwned).Append('/')
                        .Append(c.bucketAreas).Append(" | ").Append(c.log.errors).Append("E ").Append(c.log.warnings).Append('W')
                        .Append(c.failed ? " " + Esc(c.error) : "").Append(" |\n");
                md.Append('\n');
            }

            private static void StepsTable(StringBuilder md, ChunkReport c)
            {
                if (c.steps.Count == 0) return;
                md.Append("<details><summary>").Append(Esc(c.label)).Append(": steps</summary>\n\n| step | ms | runs | slowest |\n|---|--:|--:|--:|\n");
                foreach (StepTiming s in c.steps)
                    md.Append("| ").Append(Esc(s.name)).Append(" | ").Append(F1(s.ms)).Append(" | ").Append(s.count).Append(" | ").Append(F1(s.maxMs)).Append(" |\n");
                md.Append("\n</details>\n\n");
            }

            private static void LogSection(StringBuilder md, string title, LogReport log)
            {
                md.Append("### ").Append(title).Append(": ").Append(log.errors).Append(" errors, ").Append(log.warnings).Append(" warnings, ")
                    .Append(log.editModeArtefacts).Append(" edit-mode artefacts\n\n");
                if (log.first.Count == 0)
                {
                    md.Append("(nothing)\n\n");
                    return;
                }
                foreach (string line in log.first) md.Append("- `").Append(line.Replace("`", "'")).Append("`\n");
                md.Append('\n');
            }

            private static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
            #endregion
        }

        #region MapApi: the ONLY code in the harness that touches the map's internals
        // KentuckyMap.Area / Site / Passes / ChunkSize / ChunkJob / ChunkSteps / BareGround, MapRegionIndex, RaidTown, RaidMesh's
        // statics, DecorTextures.SignsUsed, DecorPalette.Apply and CeilingVisibility's renderer list. When the map code renames or
        // reshapes any of these, this region is the one place to fix. Everything else uses the public API (KentuckyMap.Build and
        // LastStats, WorldStore / MapBuild and the MapData DTOs) or the game's public types (RoomLight, CeilingVisibility...),
        // plus VisionAssets.VisionCookie for the sight light.
        private static class MapApi
        {
            public const int ChunkSize = KentuckyMap.ChunkSize;

            /// <summary>A streamed piece built by the harness, with what the streamer would know about it.</summary>
            public sealed class Chunk
            {
                public int Cx, Cz;
                public Vector3 Middle;
                public GameObject Root, Overhead;
                public readonly List<Mesh> Meshes = new List<Mesh>();
                public readonly List<string> Signs = new List<string>();
                /// <summary>Every step in order, named as ChunkSteps yields them.</summary>
                public readonly List<StepTiming> Steps = new List<StepTiming>();
                public double TotalMs, ActivateMs;
                public bool Failed;
                public string Error;
                public int Buildings, Containers, Trees, Lights;
            }

            public struct RegionTiming
            {
                public int Rx, Rz;
                public bool Exists;
                public double ReadMs, IndexMs;
            }

            /// <summary>Every region read so far, in order, with its timings.</summary>
            public static readonly List<RegionTiming> Loaded = new List<RegionTiming>();

            private static readonly Dictionary<long, MapRegionIndex> s_Regions = new Dictionary<long, MapRegionIndex>();
            private static int s_RegionSize = 8192;

            /// <summary>Remembers the build's region size; a complaint when its regions don't divide into chunks.</summary>
            public static string Use(MapBuild build)
            {
                s_RegionSize = build.RegionSize;
                return build.RegionSize % ChunkSize != 0
                    ? "Build " + build.Id + "'s " + build.RegionSize + " m regions don't divide into " + ChunkSize + " m chunks: the statewide map can't stream it."
                    : null;
            }

            private static long Key(int x, int z) => ((long)x << 32) | (uint)z;

            public static int RegionOf(int chunk)
            {
                int per = Math.Max(1, s_RegionSize / ChunkSize);
                return chunk >= 0 ? chunk / per : -((-chunk + per - 1) / per);
            }

            /// <summary>Reads region (rx, rz) and sorts it into chunks, unless that's done already (the streamer does both off the
            /// main thread). A region that can't be read stays open snow, as in the game.</summary>
            public static RegionTiming PrepareRegion(MapBuild build, int rx, int rz)
            {
                var t = new RegionTiming();
                long key = Key(rx, rz);
                if (s_Regions.TryGetValue(key, out MapRegionIndex had))
                {
                    t.Exists = had.Data != null;
                    return t;
                }
                MapRegion data = null;
                var sw = Stopwatch.StartNew();
                if (build.HasRegion(rx, rz))
                {
                    try
                    {
                        data = build.LoadRegion(rx, rz);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[MapShots] region " + rx + "_" + rz + " of build " + build.Id + " couldn't be read (" + e.GetType().Name
                            + ": " + e.Message + "); it stays open snow, as in the game.");
                    }
                }
                t.ReadMs = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                s_Regions[key] = new MapRegionIndex(data, rx, rz, build.RegionSize, ChunkSize);
                t.IndexMs = sw.Elapsed.TotalMilliseconds;
                t.Exists = data != null;
                t.Rx = rx;
                t.Rz = rz;
                Loaded.Add(t);
                return t;
            }

            /// <summary>What chunk (cx, cz) reads from its region (once prepared): streets, lines, owned buildings, neighbours'
            /// buildings, areas, points of interest.</summary>
            public static int[] Bucket(int cx, int cz)
            {
                if (!s_Regions.TryGetValue(Key(RegionOf(cx), RegionOf(cz)), out MapRegionIndex index)) return new int[6];
                MapRegionIndex.Bucket b = index.Get(cx, cz);
                return new[] { b.Streets.Count, b.Lines.Count, b.Owned.Count, b.Foreign.Count, b.Areas.Count, b.Pois.Count };
            }

            public static void ReleaseRegions() => s_Regions.Clear();

            /// <summary>KentuckyStreamer's Begin, every Step and Finish for one piece, back to back, each step timed. The piece ends
            /// up built and active at <paramref name="middle"/> under the two parents (or as bare snow when its build threw, as in
            /// the game); its loot isn't rolled and no NavMesh sources are gathered.</summary>
            public static Chunk BuildChunk(MapBuild build, string placeName, int cx, int cz, int layoutSeed, Vector3 middle,
                Transform streamed, Transform overheadRoot)
            {
                PrepareRegion(build, RegionOf(cx), RegionOf(cz));
                MapRegionIndex region = s_Regions[Key(RegionOf(cx), RegionOf(cz))];
                var c = new Chunk { Cx = cx, Cz = cz, Middle = middle };
                string name = "Piece " + cx + "_" + cz;
                // Built inactive at the origin, where the builders' local and world positions agree; moved into place after.
                c.Root = new GameObject(name);
                c.Root.SetActive(false);
                c.Root.transform.SetParent(streamed, false);
                c.Root.transform.position = Vector3.zero;
                c.Overhead = new GameObject(name);
                c.Overhead.SetActive(false);
                c.Overhead.transform.SetParent(overheadRoot, false);
                c.Overhead.transform.position = Vector3.zero;
                var job = new KentuckyMap.ChunkJob
                {
                    Build = build, Region = region, Place = placeName, Cx = cx, Cz = cz,
                    Seed = unchecked(layoutSeed * 486187739 + cx * 73856093 ^ cz * 19349663),
                    Root = c.Root.transform, Overhead = c.Overhead.transform, Offset = middle,
                };
                var uv = new Vector3((float)Wrap((cx + 0.5) * ChunkSize), 0f, (float)Wrap((cz + 0.5) * ChunkSize));
                var total = Stopwatch.StartNew();
                var sw = new Stopwatch();
                string last = "(start)";
                using (IEnumerator<string> steps = KentuckyMap.ChunkSteps(job).GetEnumerator())
                {
                    while (true)
                    {
                        List<Mesh> meshes = RaidMesh.CreatedMeshes;
                        List<string> signs = DecorTextures.SignsUsed;
                        Vector3 origin = RaidMesh.PlanarOrigin;
                        object owner = Shelter.Owner;
                        Func<string, bool> gate = ItemDatabase.LootGate;
                        Action<string, int> rolled = ItemDatabase.LootRolled;
                        RaidMesh.CreatedMeshes = c.Meshes;
                        DecorTextures.SignsUsed = c.Signs;
                        RaidMesh.PlanarOrigin = uv;
                        Shelter.Owner = c;
                        ItemDatabase.LootGate = null;
                        ItemDatabase.LootRolled = null;
                        bool more;
                        sw.Restart();
                        try
                        {
                            more = steps.MoveNext();
                        }
                        catch (Exception e)
                        {
                            more = false;
                            c.Failed = true;
                            c.Error = "threw after step '" + last + "': " + e.GetType().Name + ": " + e.Message;
                            Debug.LogError("[MapShots] building chunk " + cx + "_" + cz + " threw after step '" + last
                                + "' (the game leaves the piece as open snow): " + e);
                        }
                        finally
                        {
                            RaidMesh.CreatedMeshes = meshes;
                            DecorTextures.SignsUsed = signs;
                            RaidMesh.PlanarOrigin = origin;
                            Shelter.Owner = owner;
                            ItemDatabase.LootGate = gate;
                            ItemDatabase.LootRolled = rolled;
                        }
                        double ms = sw.Elapsed.TotalMilliseconds;
                        if (!more)
                        {
                            if (c.Failed || ms > 0.05) c.Steps.Add(new StepTiming { name = c.Failed ? "(threw)" : "(end)", ms = ms, count = 1, maxMs = ms });
                            break;
                        }
                        last = steps.Current;
                        c.Steps.Add(new StepTiming { name = last, ms = ms, count = 1, maxMs = ms });
                    }
                }
                c.TotalMs = total.Elapsed.TotalMilliseconds;

                if (c.Failed)
                {
                    // KentuckyStreamer.Finish: a piece whose build threw is emptied and laid as bare snow.
                    Shelter.Forget(c);
                    for (int i = c.Root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(c.Root.transform.GetChild(i).gameObject);
                    for (int i = c.Overhead.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(c.Overhead.transform.GetChild(i).gameObject);
                    foreach (Mesh m in c.Meshes)
                        if (m != null) Object.DestroyImmediate(m);
                    c.Meshes.Clear();
                    List<Mesh> outer = RaidMesh.CreatedMeshes;
                    RaidMesh.CreatedMeshes = c.Meshes;
                    try { KentuckyMap.BareGround(c.Root.transform, ChunkSize * 0.5f); }
                    finally { RaidMesh.CreatedMeshes = outer; }
                }
                else if (job.Site != null)
                {
                    c.Buildings = job.Site.Buildings;
                    c.Containers = job.Site.Containers;
                    c.Trees = job.Site.Trees;
                    c.Lights = job.Town != null ? job.Town.LightCount : 0;
                }
                sw.Restart();
                c.Root.transform.position = middle;
                c.Overhead.transform.position = middle;
                c.Root.SetActive(true);
                c.Overhead.SetActive(true);
                c.ActivateMs = sw.Elapsed.TotalMilliseconds;
                return c;
            }

            public static void Unload(Chunk c)
            {
                Shelter.Forget(c);
                if (c.Root != null) Object.DestroyImmediate(c.Root);
                if (c.Overhead != null) Object.DestroyImmediate(c.Overhead);
                foreach (Mesh m in c.Meshes)
                    if (m != null) Object.DestroyImmediate(m);
                c.Meshes.Clear();
            }

            private static double Wrap(double v)
            {
                double r = v % RaidMesh.PlanarPeriod;
                return r < 0 ? r + RaidMesh.PlanarPeriod : r;
            }

            /// <summary>The fixed square as KentuckyMap.Build makes it, its parts timed: reading the area, setting up the site,
            /// each step KentuckyMap.Passes yields, then emitting the overhead and outdoor meshes (exits and spawn, which are
            /// data-only, aren't included). Returns the total ms.</summary>
            public static double ProfileSquare(Transform world, ZoneDef zone, MapBuild build, MapPlace place, List<StepTiming> steps, List<Mesh> meshes)
            {
                float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = meshes;
                double total = 0;
                var sw = Stopwatch.StartNew();
                try
                {
                    var area = new KentuckyMap.Area(build, place, half);
                    total += Lap(steps, "(read the area)", sw);
                    var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
                    CeilingVisibility.Clear();
                    var town = new RaidTown(world, zone.LayoutSeed, info);
                    var site = new KentuckyMap.Site(build, town, area);
                    total += Lap(steps, "(set up the site)", sw);
                    using (IEnumerator<string> e = KentuckyMap.Passes(site).GetEnumerator())
                    {
                        while (true)
                        {
                            bool more = e.MoveNext();
                            if (!more)
                            {
                                if (sw.Elapsed.TotalMilliseconds > 0.05) total += Lap(steps, "(after the last pass)", sw);
                                break;
                            }
                            total += Lap(steps, e.Current, sw);
                        }
                    }
                    site.Overhead.Emit("Overhead", town.Overhead, true);
                    site.Roofs.Emit("Roofs", town.Overhead, false);
                    total += Lap(steps, "(emit the overhead meshes)", sw);
                    town.EmitOutdoor();
                    DecorPalette.Apply();
                    total += Lap(steps, "(emit the outdoor meshes)", sw);
                }
                finally
                {
                    RaidMesh.CreatedMeshes = outer;
                }
                return total;
            }

            private static double Lap(List<StepTiming> steps, string name, Stopwatch sw)
            {
                double ms = sw.Elapsed.TotalMilliseconds;
                steps.Add(new StepTiming { name = name, ms = ms, count = 1, maxMs = ms });
                sw.Restart();
                return ms;
            }

            /// <summary>Every renderer CeilingVisibility toggles between the views (its private list), or null when that can't
            /// be read.</summary>
            public static List<Renderer> CeilingRenderers()
            {
                FieldInfo f = typeof(CeilingVisibility).GetField("s_Renderers", BindingFlags.NonPublic | BindingFlags.Static);
                return f != null ? f.GetValue(null) as List<Renderer> : null;
            }

            /// <summary>While alive, every mesh the map bakes is added to <paramref name="into"/>, so the harness can free them.</summary>
            public static IDisposable TrackMeshes(List<Mesh> into) => new MeshScope(into);

            private sealed class MeshScope : IDisposable
            {
                private readonly List<Mesh> _outer;

                public MeshScope(List<Mesh> into)
                {
                    _outer = RaidMesh.CreatedMeshes;
                    RaidMesh.CreatedMeshes = into;
                }

                public void Dispose() => RaidMesh.CreatedMeshes = _outer;
            }
        }
        #endregion
    }
}
#endif
