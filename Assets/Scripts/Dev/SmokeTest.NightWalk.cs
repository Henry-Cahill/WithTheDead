using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    /// <summary>
    /// <c>nightwalk</c>: a night walk through Bardstown, profiled. Into the Kettle from the title screen, the clock set to
    /// 23:00 under a dry sky, then a stop in the bar, one in the street outside its door and one in each of up to
    /// <see cref="WalkInteriors"/> lit rooms nearby (a lit ceiling or desk lamp, 10 m or more from the last stop), and the
    /// street once more at noon. At each stop the game's camera renders <see cref="WalkFrames"/> frames into a 1920x1080
    /// target: the median frame, the camera's and both shadow passes' CPU and GPU time, and the render waited out on the
    /// GPU. Each stop is measured twice, with the main light's cascades as the game has them and with the other count
    /// (4 or 2), and counts the punctual lights casting shadows in view: lights, shadow maps, atlas texels and the scale URP
    /// would shrink them by. Any URP message about squeezing the additional lights' shadow atlas fails the run. With
    /// <c>-clonez-smoke-shots &lt;dir&gt;</c> each stop is photographed and the numbers are written to nightwalk.csv there.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const int WalkFrames = 30, WalkWarmup = 8, WalkSynced = 12, WalkInteriors = 6;
        private const int WalkWidth = 1920, WalkHeight = 1080;
        private const float WalkNight = 23f, WalkNoon = 12f;
        private const string AtlasMessage = "additional punctual light";   // all three of URP's atlas messages

        private static int s_AtlasMessages;
        private static string s_FirstAtlasMessage;
        private readonly List<string> _walkCsv = new List<string>();

        // Counts URP's "Reduced additional punctual light shadows resolution...", "Too many additional punctual lights
        // shadows..." and "...URP removed N shadow maps..." (logged once per change in the shadow requests).
        private static void CountAtlasMessage(string message)
        {
            if (message == null || message.IndexOf(AtlasMessage, StringComparison.Ordinal) < 0) return;
            s_AtlasMessages++;
            if (s_FirstAtlasMessage == null) s_FirstAtlasMessage = message;
        }

        private IEnumerator NightWalk()
        {
            s_AtlasMessages = 0;
            s_FirstAtlasMessage = null;
            Debug.Log("[Smoke] nightwalk on " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ", "
                + SystemInfo.graphicsMemorySize + " MB), GPU recorder " + (SystemInfo.supportsGpuRecorder ? "yes" : "no"));
            _walkCsv.Add("stop,cascades,frame_ms,render_ms,camera_cpu_ms,camera_gpu_ms,main_shadow_cpu_ms,main_shadow_gpu_ms,"
                + "extra_shadow_cpu_ms,extra_shadow_gpu_ms,shadowed_lights,points,spots,shadow_maps,atlas_texels,atlas_scale,atlas_messages");

            // Into the Kettle the way the title screen goes.
            var boot = FindAnyObjectByType<GameBootstrapper>();
            MainMenuController old = FindAnyObjectByType<MainMenuController>();
            if (old != null) Destroy(old.gameObject);
            yield return null;
            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            menu.Bind(boot);
            yield return new WaitForSeconds(1f);
            Try("into the Kettle", () => typeof(MainMenuController).GetMethod("EnterHub", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, null));
            float t0 = Time.realtimeSinceStartup;
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null, 180f);
            Check("hub up", GameSession.IsHub && Actor.LocalPlayer != null, (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");
            Check("the Kettle stands in the town", KettleLobby.Active && KentuckyStreamer.Active != null);
            if (!KettleLobby.Active || Actor.LocalPlayer == null) yield break;
            yield return Until(() => !LoadingScreen.Showing, 20f);
            yield return new WaitForSeconds(2f);

            Actor me = Actor.LocalPlayer;
            me.Untargetable = true;   // the dead mustn't end the walk
            SetWalkSky(WalkNight);
            yield return new WaitForSeconds(1.5f);
            yield return WalkStop("bar");

            // Out of the front door (the raid starts where the survivor stands) and a few steps into the street.
            KettleDoor door = KettleLobby.Door;
            Check("front door", door != null);
            if (door == null) yield break;
            Try("open the door", () => door.SetOpen(true, false));
            yield return new WaitForSeconds(1f);
            var cc = me.GetComponent<CharacterController>();
            Vector3 outward = door.transform.rotation * Vector3.back;
            for (int i = 0; i < 600 && Vector3.Dot(me.transform.position - door.transform.position, outward) < 5.5f; i++)
            {
                Vector3 lane = door.transform.position + outward * Vector3.Dot(me.transform.position - door.transform.position, outward);
                Vector3 side = lane - me.transform.position;
                side.y = 0f;
                Vector3 step = (side.magnitude > 0.15f ? side.normalized : outward) * 0.06f;
                if (cc != null && cc.enabled) cc.Move(step + Vector3.down * 0.02f);
                yield return null;
            }
            // Blocked on the way (a keeper or a door leaf in the doorway): stepped out to where the walk ends instead, which
            // starts the raid all the same (KettleDoor sees the survivor go from inside to out through the open door).
            bool walked = Vector3.Dot(me.transform.position - door.transform.position, outward) >= 5f;
            if (!walked)
            {
                Vector3 end = door.transform.position + outward * 5.5f;
                if (NavMesh.SamplePosition(end, out NavMeshHit ground, 3f, NavMesh.AllAreas)) end = ground.position + Vector3.up * 0.05f;
                PlaceSurvivor(me, end, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
                yield return Until(() => KettleLobby.RaidUnderway, 5f);
            }
            Check("out in the street, the raid under way", !KettleLobby.Inside(me.transform.position) && KettleLobby.RaidUnderway,
                (walked ? "walked out" : "stepped out, the walk blocked") + " to " + me.transform.position);
            SetWalkSky(WalkNight);
            yield return new WaitForSeconds(2f);
            Vector3 street = me.transform.position;
            yield return WalkStop("street");

            // Lit rooms round the bar, each 10 m or more from the stops before it (found from the door, so every run of the walk
            // stops in the same rooms).
            List<Vector3> rooms = LitRoomStops(door.transform.position + outward * 5.5f, WalkInteriors, 80f, 10f);
            Check("lit rooms near the bar", rooms.Count > 0, rooms.Count + " found");
            for (int i = 0; i < rooms.Count; i++)
            {
                PlaceSurvivor(me, rooms[i], 45f + 90f * i);
                yield return new WaitForSeconds(2f);
                yield return WalkStop("room " + (i + 1));
            }

            // The street by day (the sun's cascades decide this one).
            PlaceSurvivor(me, street, 0f);
            SetWalkSky(WalkNoon);
            yield return new WaitForSeconds(2f);
            yield return WalkStop("street by day");
            SetWalkSky(WalkNight);

            Check("no shadow-atlas squeeze on the walk", s_AtlasMessages == 0,
                s_AtlasMessages == 0 ? "URP never shrank or dropped a shadow map" : s_AtlasMessages + " URP messages, the first: " + s_FirstAtlasMessage);
            if (!string.IsNullOrEmpty(s_ShotDir))
            {
                string csv = Path.Combine(s_ShotDir, "nightwalk.csv");
                try { File.WriteAllLines(csv, _walkCsv); Debug.Log("[Smoke] nightwalk numbers written to " + csv); }
                catch (Exception e) { Debug.LogWarning("[Smoke] nightwalk.csv not written: " + e.Message); }
            }
        }

        private static void SetWalkSky(float hour)
        {
            Climate.Winter = false;
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.Restore(Weather.Clear, 0f, 0.2f, 0.2f);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, hour);
        }

        private static void PlaceSurvivor(Actor me, Vector3 at, float yaw)
        {
            var cc = me.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            me.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        // Floor points under lit lamps (on the NavMesh, so on the floor rather than a table top), outside the bar, nearest
        // first and each at least `spacing` from the ones before.
        private static List<Vector3> LitRoomStops(Vector3 from, int count, float reach, float spacing)
        {
            var lamps = new List<(float d, Vector3 at)>();
            IReadOnlyList<RoomLight> all = RoomLight.All;
            for (int i = 0; i < all.Count; i++)
            {
                RoomLight l = all[i];
                if (l == null || !l.On || l.Light == null) continue;
                Vector3 p = l.transform.position;
                if (KettleLobby.Inside(p)) continue;
                float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(from.x, 0f, from.z));
                if (d <= reach) lamps.Add((d, p));
            }
            lamps.Sort((a, b) => a.d.CompareTo(b.d));
            var stops = new List<Vector3>();
            foreach (var (_, at) in lamps)
            {
                if (stops.Count >= count) break;
                if (!NavMesh.SamplePosition(at + Vector3.down * 2f, out NavMeshHit hit, 1.6f, NavMesh.AllAreas)) continue;
                if (at.y - hit.position.y < 0.3f || at.y - hit.position.y > 3.5f) continue;
                bool apart = true;
                foreach (Vector3 s in stops) if ((s - hit.position).sqrMagnitude < spacing * spacing) { apart = false; break; }
                if (apart) stops.Add(hit.position + Vector3.up * 0.05f);
            }
            return stops;
        }

        // One stop: the frames as the game has its cascades, then with the other count, a picture and a census.
        private IEnumerator WalkStop(string label)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Check("camera at " + label, false);
                yield break;
            }
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int gameCascades = urp != null ? urp.shadowCascadeCount : 0;
            var rt = new RenderTexture(WalkWidth, WalkHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                { name = "[NightWalk]", antiAliasing = 1 };
            rt.Create();
            try
            {
                yield return MeasureFrames(label, cam, rt, urp, -1);
                int other = gameCascades >= 4 ? 2 : 4;
                if (urp != null) yield return MeasureFrames(label, cam, rt, urp, other);
                SaveWalkShot(label, cam, rt, urp, -1);
                // By day the sun's shadows show what the cascades are worth: a picture with the other count too.
                if (urp != null && TimeOfDay.Instance != null && !TimeOfDay.Instance.IsNight) SaveWalkShot(label + " " + other + " cascades", cam, rt, urp, other);
            }
            finally
            {
                rt.Release();
                Destroy(rt);
            }
        }

        // `cascades` -1 leaves the asset as the game has it; otherwise it's set just before each render and put back after.
        private IEnumerator MeasureFrames(string label, Camera cam, RenderTexture rt, UniversalRenderPipelineAsset urp, int cascades)
        {
            int gameCascades = urp != null ? urp.shadowCascadeCount : 0;
            int used = cascades > 0 ? cascades : gameCascades;
            for (int i = 0; i < WalkWarmup; i++)
            {
                RenderWalkFrame(cam, rt, urp, cascades);
                yield return null;
            }
            Recorder camera = Recorder.Get("UniversalRenderPipeline.RenderSingleCameraInternal: " + cam.name);
            Recorder main = Recorder.Get("Draw Main Light Shadowmap");
            Recorder extra = Recorder.Get("Draw Additional Lights Shadowmap");
            foreach (Recorder r in new[] { camera, main, extra }) if (r.isValid) r.enabled = true;
            var frame = new List<double>();
            var camCpu = new List<double>();
            var camGpu = new List<double>();
            var mainCpu = new List<double>();
            var mainGpu = new List<double>();
            var extraCpu = new List<double>();
            var extraGpu = new List<double>();
            for (int i = 0; i < WalkFrames; i++)
            {
                RenderWalkFrame(cam, rt, urp, cascades);
                yield return null;
                frame.Add(Time.unscaledDeltaTime * 1000.0);
                Sample(camera, camCpu, camGpu);
                Sample(main, mainCpu, mainGpu);
                Sample(extra, extraCpu, extraGpu);
            }
            // The render on its own, waited out on the GPU (a one-texel readback after each).
            var synced = new List<double>();
            var sw = new Stopwatch();
            for (int i = 0; i < WalkSynced; i++)
            {
                sw.Restart();
                RenderWalkFrame(cam, rt, urp, cascades);
                AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                synced.Add(sw.Elapsed.TotalMilliseconds);
            }
            ShadowCensus(cam, urp, out int lights, out int points, out int spots, out int maps, out long texels, out int scale, out string detail);
            string stop = label + (cascades > 0 ? " (" + cascades + " cascades)" : "");
            Debug.Log("[Smoke] nightwalk " + stop + ": frame " + Num(Median(frame)) + " ms, render " + Num(Median(synced)) + " ms (camera cpu "
                + Num(Median(camCpu)) + " gpu " + Num(Median(camGpu)) + "; sun shadows cpu " + Num(Median(mainCpu)) + " gpu " + Num(Median(mainGpu))
                + " with " + used + " cascades; lamp shadows cpu " + Num(Median(extraCpu)) + " gpu " + Num(Median(extraGpu)) + "); " + lights
                + " shadowed lights in view (" + points + " point, " + spots + " spot), " + maps + " shadow maps, "
                + (texels / 1024) + "K atlas texels" + (scale > 1 ? ", URP shrinks them " + scale + "x" : "") + "; atlas messages so far "
                + s_AtlasMessages + (detail.Length > 0 ? "; " + detail : ""));
            _walkCsv.Add(string.Join(",", Csv(stop), used.ToString(CultureInfo.InvariantCulture), Num(Median(frame)), Num(Median(synced)),
                Num(Median(camCpu)), Num(Median(camGpu)), Num(Median(mainCpu)), Num(Median(mainGpu)), Num(Median(extraCpu)), Num(Median(extraGpu)),
                lights.ToString(CultureInfo.InvariantCulture), points.ToString(CultureInfo.InvariantCulture), spots.ToString(CultureInfo.InvariantCulture),
                maps.ToString(CultureInfo.InvariantCulture), texels.ToString(CultureInfo.InvariantCulture), scale.ToString(CultureInfo.InvariantCulture),
                s_AtlasMessages.ToString(CultureInfo.InvariantCulture)));
        }

        private static void RenderWalkFrame(Camera cam, RenderTexture rt, UniversalRenderPipelineAsset urp, int cascades)
        {
            int keep = urp != null ? urp.shadowCascadeCount : 0;
            if (cascades > 0 && urp != null) urp.shadowCascadeCount = cascades;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
                else
                {
                    RenderTexture target = cam.targetTexture;
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = target;
                }
            }
            finally
            {
                if (cascades > 0 && urp != null) urp.shadowCascadeCount = keep;
            }
        }

        private static void Sample(Recorder r, List<double> cpu, List<double> gpu)
        {
            if (!r.isValid) return;
            cpu.Add(r.elapsedNanoseconds / 1e6);
            if (r.gpuElapsedNanoseconds > 0) gpu.Add(r.gpuElapsedNanoseconds / 1e6);
        }

        // The punctual lights casting shadows in the camera's view, and what they ask of the additional lights' shadow atlas
        // (texels at their resolution tiers; the scale is URP's first estimate of how far it must shrink them all).
        private static void ShadowCensus(Camera cam, UniversalRenderPipelineAsset urp, out int lights, out int points, out int spots,
            out int maps, out long texels, out int scale, out string detail)
        {
            lights = points = spots = maps = 0;
            texels = 0;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var names = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (Light l in FindObjectsByType<Light>())
            {
                if (l == null || !l.isActiveAndEnabled || l.shadows == LightShadows.None) continue;
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(l.transform.position, Vector3.one * (2f * l.range)))) continue;
                int slices = l.type == LightType.Point ? 6 : 1;
                int res = ShadowTierResolution(urp, l);
                lights++;
                if (slices == 6) points++;
                else spots++;
                maps += slices;
                texels += (long)slices * res * res;
                string key = l.name + " " + (slices == 6 ? "point" : "spot") + " " + res;
                names[key] = names.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            int atlas = urp != null ? urp.additionalLightsShadowmapResolution : 2048;
            scale = 1;
            while (texels > (long)atlas * atlas * scale * scale) scale *= 2;
            var sb = new StringBuilder();
            foreach (var kv in names)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Value).Append("x ").Append(kv.Key);
            }
            detail = sb.ToString();
        }

        private static int ShadowTierResolution(UniversalRenderPipelineAsset urp, Light l)
        {
            if (urp == null) return 1024;
            int tier = UniversalAdditionalLightData.AdditionalLightsShadowDefaultResolutionTier;
            if (l.TryGetComponent(out UniversalAdditionalLightData data)) tier = data.additionalLightsShadowResolutionTier;
                        if (tier <= UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow) return urp.additionalLightsShadowResolutionTierLow;
            if (tier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium) return urp.additionalLightsShadowResolutionTierMedium;
            return urp.additionalLightsShadowResolutionTierHigh;
        }

        private static void SaveWalkShot(string label, Camera cam, RenderTexture rt, UniversalRenderPipelineAsset urp, int cascades)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) return;
            RenderWalkFrame(cam, rt, urp, cascades);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                tex.Apply(false);
                File.WriteAllBytes(Path.Combine(s_ShotDir, "nightwalk-" + label.Replace(' ', '-') + ".png"), tex.EncodeToPNG());
            }
            catch (Exception e) { Debug.LogWarning("[Smoke] nightwalk shot " + label + " failed: " + e.Message); }
            finally
            {
                RenderTexture.active = active;
                Destroy(tex);
            }
        }

        private static double Median(List<double> v)
        {
            if (v.Count == 0) return 0;
            v.Sort();
            return v.Count % 2 == 1 ? v[v.Count / 2] : 0.5 * (v[v.Count / 2 - 1] + v[v.Count / 2]);
        }

        private static string Num(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Csv(string s) => s.IndexOf(',') >= 0 ? "\"" + s + "\"" : s;
    }
}
