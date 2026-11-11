using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    /// <c>cutaway</c>: what the isometric cutaway's hidden pieces cost, and what it remembers. A cut wall, roof or partition
    /// stops drawing but goes on casting its shadows (<see cref="ShadowCastingMode.ShadowsOnly"/>, in
    /// <see cref="IsoCutaway"/> and, on the statewide map, <see cref="MapCutaway"/>'s shells), so it's still drawn into the
    /// sun's cascades and every shadowed lamp's maps. This deploys into the statewide map (Bardstown, or the zone named with
    /// <c>-clonez-smoke-zone</c>), finds the densest block near the start (the building shells with the most others within
    /// <see cref="CutBlockReach"/>; in the procedural town, the cutaway pieces) and tries the survivor at every spot of a
    /// grid over it, keeping the spot indoors and the one outdoors where the cutaways hide the most. At each of those, at
    /// 23:00 and at noon under a clear sky, it:
    /// <list type="bullet">
    /// <item>counts what the cutaways hide that still casts shadows: renderers, triangles, how much of it is away from the
    /// building the survivor is in, and the shadow views it's drawn into (each of the sun's cascades within its shadow
    /// distance, six faces a shadowed point lamp and one a spot lamp within its range: an upper bound), beside every shadow
    /// caster in those views;</item>
    /// <item>renders the game's camera at 1920x1080, <see cref="CutRounds"/> times in turn each way (<see cref="CutStates"/>):
    /// as the game has it, with nothing the cutaways hide casting shadows, and with only the hidden parts of the building
    /// the survivor is in casting them. It logs each way's median frame, the render waited out on the GPU, and the
    /// camera's and both shadow passes' CPU and GPU time a render, and what each way saves on the first.</item>
    /// </list>
    /// Then, on a streamed map, it walks east across the state, standing at a few spots at each stop so the cutaway's rays
    /// meet the walls and furniture there, and fails if the cutaway still remembers a collider of a piece, interior or
    /// window once it has been let go of (<see cref="IsoCutaway.LetGo"/>). It logs what the cutaway remembers at each
    /// stop, what it has forgotten, and the longest a frame spent forgetting. With <c>-clonez-smoke-shots &lt;dir&gt;</c>
    /// each stop is photographed each way and the numbers go to cutaway.csv and cutaway-cache.csv there. It renders, so
    /// it can't run with <c>-nographics</c>.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const float CutBlockReach = 30f;                    // a block: the buildings within this of one of them
        private const float CutSearch = 400f;                       // blocks are looked for this far from the start (in Full detail)
        private const float CutGridReach = 20f, CutGridStep = 4f;   // the spots tried over the block
        private const float CutTryWait = 0.5f;                      // seconds at each: past both cutaways' restore delay
        private const float CutSettle = 2.5f;                       // seconds at a stop before it's measured
        private const int CutRounds = 6, CutFrames = 15, CutSkip = 4, CutSynced = 8;
        private const int CutHops = 8, CutWalkSpots = 6;
        private const float CutHop = 160f, CutWalkRing = 10f, CutWalkWait = 0.6f;

        // The ways each stop is rendered: as the game has it; with nothing the cutaways hide casting shadows; and with the
        // hidden parts of the buildings the survivor isn't in casting none (the survivor's own keeping theirs).
        private static readonly string[] CutStates = { "as the game has it", "nothing hidden casting shadows", "only the survivor's building's hidden parts casting shadows" };

        private sealed class CutSample
        {
            public readonly List<double> Frame = new List<double>(), Synced = new List<double>(),
                CamCpu = new List<double>(), CamGpu = new List<double>(), MainCpu = new List<double>(),
                MainGpu = new List<double>(), ExtraCpu = new List<double>(), ExtraGpu = new List<double>();
            public int Renders;   // the most times a frame the camera rendered (the game's own render besides the measured one)
        }

        // What the cutaways hide that still casts shadows at a stop, and the shadow views round it.
        private sealed class CutCensus
        {
            public readonly List<Renderer> Casters = new List<Renderer>();
            /// <summary>Those of the casters away from the building the survivor is in (null where that isn't known).</summary>
            public List<Renderer> Elsewhere = new List<Renderer>();
            public int IsoPieces, IsoCasters, MapBuildings, MapOccupied, MapCasters, OtherUnseen;
            public long IsoTris, MapTris, ElsewhereTris;
            public int SunViews, Points, Spots;
            public long HiddenViews, HiddenViewTris, AllViews, AllViewTris;
        }

        private struct CutLight
        {
            public Vector3 At;
            public float Range;
            public int Views;
        }

        private readonly List<string> _cutCsv = new List<string>();
        private readonly Dictionary<Mesh, long> _cutTris = new Dictionary<Mesh, long>();
        private double _cutForgetMs;

        private IEnumerator CutawayRun()
        {
            yield return ToHub();
            yield return Deploy(RaidKind.Operative, ZoneCatalog.FeaturedZone);
            Actor me = Actor.LocalPlayer;
            IsoCameraFollow follow = IsoCameraFollow.Instance;
            IsoCutaway iso = follow != null ? follow.Cutaway : null;
            Camera cam = follow != null ? follow.GetComponent<Camera>() : null;
            bool ready = me != null && iso != null && cam != null && follow.View == CameraView.Isometric && follow.WallCutaway;
            Check("cutaway: the isometric camera runs its cutaway", ready, me == null ? "no survivor" : follow == null ? "no camera"
                : iso == null ? "no cutaway" : follow.View + " view, wall cutaway " + (follow.WallCutaway ? "on" : "off"));
            if (!ready) yield break;
            bool untargetable = me.Untargetable;
            me.Untargetable = true;   // the dead may come; this run is about walls
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Debug.Log("[Smoke] cutaway on " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ", "
                + SystemInfo.graphicsMemorySize + " MB), GPU recorder " + (SystemInfo.supportsGpuRecorder ? "yes" : "no") + ", "
                + (streamer != null ? "the statewide map" : "a fixed map") + ", shadows " + GameSettings.Current.ShadowQuality);
            _cutCsv.Add("stop,time,state,iso_pieces,iso_casters,iso_tris,map_buildings,map_occupied,map_casters,map_tris,"
                + "elsewhere_casters,elsewhere_tris,other_unseen,sun_cascades,point_lamps,spot_lamps,hidden_views,hidden_view_tris,"
                + "all_views,all_view_tris,frame_ms,render_ms,camera_cpu_ms,camera_gpu_ms,main_shadow_cpu_ms,main_shadow_gpu_ms,"
                + "extra_shadow_cpu_ms,extra_shadow_gpu_ms,renders_a_frame");

            // The densest block near the start, its interiors furnished.
            SetWalkSky(WalkNight);
            List<MapCutaway.Building> shells = CutShells();
            Vector3 start = me.transform.position;
            Vector3 middle = CutBlock(start, shells, streamer, out int dense);
            NavMeshHit ground = default;
            bool found = dense > 0 && NavMesh.SamplePosition(middle, out ground, 6f, NavMesh.AllAreas);
            Check("cutaway: a dense block near the start", found, dense + (shells.Count > 0 ? " building shells" : " cutaway pieces")
                + " within " + CutBlockReach + " m of " + Round(middle) + ", " + Mathf.RoundToInt(CutDistance(middle, start)) + " m from the start");
            if (!found)
            {
                me.Untargetable = untargetable;
                yield break;
            }
            Vector3 floor = ground.position;
            Teleport(me, floor + Vector3.up * 0.05f);
            if (streamer != null) yield return Until(() => streamer.IsNavigableAt(floor), 60f);
            yield return new WaitForSeconds(6f);   // the interiors round it furnished, and the NavMesh taking them in

            // Every spot of the grid over it: where the cutaways hide the most, indoors and out.
            Vector3 inside = default, outside = default;
            long inTris = 0, outTris = 0;
            int tried = 0;
            var probe = new CutCensus();
            for (float dz = -CutGridReach; dz <= CutGridReach + 0.01f; dz += CutGridStep)
                for (float dx = -CutGridReach; dx <= CutGridReach + 0.01f; dx += CutGridStep)
                {
                    Vector3 p = new Vector3(floor.x + dx, floor.y + 0.3f, floor.z + dz);
                    if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 1.2f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - floor.y) > 1.5f) continue;
                    Vector3 spot = hit.position + Vector3.up * 0.05f;
                    Teleport(me, spot);
                    iso.RestoreAll();
                    MapCutaway.RestoreAll();
                    yield return new WaitForSeconds(CutTryWait);
                    tried++;
                    CutHidden(iso, shells, me.transform.position, probe);
                    long tris = probe.IsoTris + probe.MapTris;
                    if (tris == 0) continue;
                    if (CutIndoors(spot, shells)) { if (tris > inTris) { inTris = tris; inside = spot; } }
                    else if (tris > outTris) { outTris = tris; outside = spot; }
                }
            Check("cutaway: spots in the block where the cutaways hide something", inTris > 0 && outTris > 0, tried + " spots tried; indoors "
                + (inTris > 0 ? Round(inside) + " (" + inTris + " triangles hidden)" : "none") + "; outdoors "
                + (outTris > 0 ? Round(outside) + " (" + outTris + " triangles hidden)" : "none"));

            // Each spot by night and by day: what's hidden, and the frames each of the ways (CutStates).
            var rt = new RenderTexture(WalkWidth, WalkHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                { name = "[Cutaway]", antiAliasing = 1 };
            rt.Create();
            var most = new double[CutStates.Length];
            var mostAt = new string[CutStates.Length];
            try
            {
                foreach ((string label, Vector3 at, long tris) in new[] { ("indoors", inside, inTris), ("outdoors", outside, outTris) })
                {
                    if (tris == 0) continue;
                    foreach ((string time, float hour) in new[] { ("night", WalkNight), ("noon", WalkNoon) })
                    {
                        Teleport(me, at);
                        SetWalkSky(hour);
                        iso.RestoreAll();
                        MapCutaway.RestoreAll();
                        yield return new WaitForSeconds(CutSettle);
                        var census = new CutCensus();
                        CutHidden(iso, shells, me.transform.position, census);
                        CutViews(cam, urp, census);
                        string stop = label + " (" + time + ")";
                        Debug.Log("[Smoke] cutaway " + stop + " at " + Round(at) + ": hidden but casting shadows: " + census.IsoCasters
                            + " renderers of " + census.IsoPieces + " pieces the ray cutaway holds (" + census.IsoTris + " triangles), "
                            + census.MapCasters + " of " + census.MapBuildings + " cut buildings' shells (" + census.MapTris
                            + " triangles; the survivor in " + census.MapOccupied + "); " + (census.Elsewhere == null ? "how much of it is"
                                : census.Elsewhere.Count + " of them (" + census.ElsewhereTris + " triangles) are") + " away from the building the survivor is in; "
                            + census.OtherUnseen + " other unseen casters round them (ceilings and the like). Shadow views: the sun's " + census.SunViews
                            + " cascades, " + census.Points + " point and " + census.Spots + " spot lamps; the hidden pieces are drawn "
                            + census.HiddenViews + " times (" + census.HiddenViewTris + " triangles) a render at most, of "
                            + census.AllViews + " shadow draws (" + census.AllViewTris + " triangles) in all: "
                            + Pct(census.HiddenViews, census.AllViews) + " of the draws, " + Pct(census.HiddenViewTris, census.AllViewTris)
                            + " of the triangles");
                        var ways = new CutSample[CutStates.Length];
                        for (int k = 0; k < ways.Length; k++) ways[k] = new CutSample();
                        var off = new[] { null, census.Casters, census.Elsewhere };
                        yield return CutFramesWays(cam, rt, off, ways);
                        for (int k = 0; k < ways.Length; k++) if (k == 0 || off[k] != null) CutLog(stop, k, census, ways[k]);
                        for (int k = 1; k < ways.Length; k++)
                        {
                            if (off[k] == null) continue;
                            CutSample a = ways[0], b = ways[k];
                            double sunCpu = Median(a.MainCpu) - Median(b.MainCpu), sunGpu = Median(a.MainGpu) - Median(b.MainGpu);
                            double lampCpu = Median(a.ExtraCpu) - Median(b.ExtraCpu), lampGpu = Median(a.ExtraGpu) - Median(b.ExtraGpu);
                            double render = Median(a.Synced) - Median(b.Synced);
                            Debug.Log("[Smoke] cutaway " + stop + ", " + CutStates[k] + ": saves " + Num(sunCpu + lampCpu) + " ms CPU and "
                                + Num(sunGpu + lampGpu) + " ms GPU of the shadow passes a render (the sun's " + Num(sunCpu) + " and "
                                + Num(sunGpu) + ", the lamps' " + Num(lampCpu) + " and " + Num(lampGpu) + "), and " + Num(render)
                                + " ms of the render waited out (" + Pct(render, Median(a.Synced)) + ")");
                            if (mostAt[k] == null || sunGpu + lampGpu > most[k])
                            {
                                most[k] = sunGpu + lampGpu;
                                mostAt[k] = stop;
                            }
                        }
                        CutShot(label + "-" + time, cam, rt, null);
                        CutShot(label + "-" + time + "-no-shadows", cam, rt, census.Casters);
                        if (census.Elsewhere != null && census.Elsewhere.Count != census.Casters.Count)
                            CutShot(label + "-" + time + "-own-building-shadows", cam, rt, census.Elsewhere);
                    }
                }
            }
            finally
            {
                rt.Release();
                Destroy(rt);
            }
            for (int k = 1; k < CutStates.Length; k++)
                if (mostAt[k] != null)
                    Debug.Log("[Smoke] cutaway: " + CutStates[k] + " saves at most " + Num(most[k]) + " ms GPU of the shadow passes a render ("
                        + mostAt[k] + ")");

            // What the ray cutaway remembers as the survivor goes on, and that it forgets what's let go of.
            var cache = new StringBuilder("stop,east_m,cached_colliders,cached_pieces,stale,forgotten,sites_let_go,forget_ms\n");
            if (streamer != null) yield return CutCacheWalk(streamer, me, iso, cache);
            else Skip("cutaway: the cache across streamed pieces", "a fixed map lets nothing go");
            me.Untargetable = untargetable;
            SetWalkSky(WalkNight);

            if (!string.IsNullOrEmpty(s_ShotDir))
            {
                try
                {
                    File.WriteAllLines(Path.Combine(s_ShotDir, "cutaway.csv"), _cutCsv);
                    if (streamer != null) File.WriteAllText(Path.Combine(s_ShotDir, "cutaway-cache.csv"), cache.ToString());
                    Debug.Log("[Smoke] cutaway numbers written to " + s_ShotDir);
                }
                catch (Exception e) { Debug.LogWarning("[Smoke] cutaway numbers not written: " + e.Message); }
            }
        }

        // The buildings with shells of their own on the sites in place (the statewide map's Full pieces); none elsewhere.
        private static List<MapCutaway.Building> CutShells()
        {
            var all = new List<MapCutaway.Building>();
            foreach (MapCutawaySite site in FindObjectsByType<MapCutawaySite>())
                if (site != null && site.isActiveAndEnabled && site.Buildings != null)
                    foreach (MapCutaway.Building b in site.Buildings)
                        if (b != null && b.Site != null && b.Pts != null) all.Add(b);
            return all;
        }

        // The middle of the densest block within CutSearch of `from` (in Full detail on a streamed map): the shell, or failing
        // shells the cutaway piece, with the most others within CutBlockReach.
        private static Vector3 CutBlock(Vector3 from, List<MapCutaway.Building> shells, KentuckyStreamer streamer, out int count)
        {
            var points = new List<Vector3>();
            if (shells.Count > 0)
                foreach (MapCutaway.Building b in shells) points.Add(new Vector3(b.Bounds.center.x, b.Base, b.Bounds.center.y));
            else
                foreach (CutawayOccluder o in FindObjectsByType<CutawayOccluder>())
                    if (o != null && o.isActiveAndEnabled) points.Add(o.transform.position);
            points.RemoveAll(p => CutDistance(p, from) > CutSearch || (streamer != null && streamer.DetailAt(p) != KentuckyMap.Detail.Full));
            Vector3 best = from;
            count = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                int n = 0;
                for (int k = 0; k < points.Count; k++) if (CutDistance(points[k], points[i]) <= CutBlockReach) n++;
                float d = CutDistance(points[i], from);
                if (n < count || (n == count && d >= bestDistance)) continue;
                count = n;
                bestDistance = d;
                best = points[i];
            }
            return best;
        }

        // Indoors: inside a shell's footprint on the statewide map (the shells have no colliders); elsewhere, under a ceiling
        // (the procedural town's are on the overhead colliders' layer, so the shoulder camera stops under them).
        private static bool CutIndoors(Vector3 feet, List<MapCutaway.Building> shells)
        {
            if (shells.Count == 0)
                return Physics.Raycast(feet + Vector3.up * 1.9f, Vector3.up, 12f, CloneZConfig.WorldMask | (1 << RaidTown.OverheadColliderLayer),
                    QueryTriggerInteraction.Ignore);
            var q = new Vector2(feet.x, feet.z);
            foreach (MapCutaway.Building b in shells)
                if (b.Site != null && b.Bounds.Contains(q) && InFootprint(b.Pts, q)) return true;
            return false;
        }

        private static bool InFootprint(Vector2[] p, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            return inside;
        }

        // What the two cutaways hide now that still casts its shadows: the ray cutaway's pieces, and the cut buildings' shells,
        // storeys, floors and doors; and how much of that is away from the building the survivor is in.
        private void CutHidden(IsoCutaway iso, List<MapCutaway.Building> shells, Vector3 feet, CutCensus c)
        {
            c.Casters.Clear();
            c.IsoPieces = iso.HiddenCount;
            iso.ShadowCasters(c.Casters);
            c.IsoCasters = c.Casters.Count;
            c.IsoTris = 0;
            for (int i = 0; i < c.Casters.Count; i++) c.IsoTris += TrisOf(c.Casters[i]);
            c.MapBuildings = c.MapOccupied = 0;
            MapCutaway.Building home = null;
            IReadOnlyList<MapCutaway.Building> cut = MapCutaway.Cut;
            for (int i = 0; i < cut.Count; i++)
            {
                MapCutaway.Building b = cut[i];
                int before = c.Casters.Count;
                CutUnseen(c.Casters, b.Walls);
                CutUnseen(c.Casters, b.Upper);
                if (b.Bands != null) foreach (MeshRenderer r in b.Bands) CutUnseen(c.Casters, r);
                if (b.BandWalls != null) foreach (MapCutaway.Building w in b.BandWalls) if (w != null) CutUnseen(c.Casters, w.Walls);
                if (b.Structure != null) foreach (Renderer r in b.Structure) CutUnseen(c.Casters, r);
                if (b.Doors != null) foreach (Renderer r in b.Doors) CutUnseen(c.Casters, r);
                if (c.Casters.Count == before) continue;
                c.MapBuildings++;
                if (b.Occupied) c.MapOccupied++;
            }
            c.MapCasters = c.Casters.Count - c.IsoCasters;
            c.MapTris = 0;
            for (int i = c.IsoCasters; i < c.Casters.Count; i++) c.MapTris += TrisOf(c.Casters[i]);

            // The building the survivor stands in, by its footprint. The procedural town has no footprints to go by: there, all
            // of it is away from them outdoors, and indoors it's not known (null).
            var q = new Vector2(feet.x, feet.z);
            foreach (MapCutaway.Building b in shells)
                if (b.Site != null && b.Bounds.Contains(q) && InFootprint(b.Pts, q)) { home = b; break; }
            c.ElsewhereTris = 0;
            if (shells.Count == 0 && CutIndoors(feet, shells))
            {
                c.Elsewhere = null;
                return;
            }
            if (c.Elsewhere == null) c.Elsewhere = new List<Renderer>();
            c.Elsewhere.Clear();
            for (int i = 0; i < c.Casters.Count; i++)
            {
                Renderer r = c.Casters[i];
                Vector3 m = r.bounds.center;
                var at = new Vector2(m.x, m.z);
                if (home != null && home.Bounds.Contains(at) && InFootprint(home.Pts, at)) continue;
                c.Elsewhere.Add(r);
                c.ElsewhereTris += TrisOf(r);
            }
        }

        // A renderer hidden from view that still casts its shadows.
        private static void CutUnseen(List<Renderer> into, Renderer r)
        {
            if (r == null || !r.enabled || r.forceRenderingOff || !r.gameObject.activeInHierarchy) return;
            if (r.shadowCastingMode != ShadowCastingMode.ShadowsOnly || into.Contains(r)) return;
            into.Add(r);
        }

        // The shadow views being drawn now (the sun's cascades within its shadow distance of the camera, six faces a shadowed
        // point lamp and one a spot lamp in view, within their range), and how often the hidden pieces and every shadow caster
        // round them are drawn into them: an upper bound, as URP culls each view more tightly.
        private void CutViews(Camera cam, UniversalRenderPipelineAsset urp, CutCensus c)
        {
            var lights = new List<CutLight>();
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            c.SunViews = c.Points = c.Spots = 0;
            float sunReach = urp != null ? urp.shadowDistance : 50f;
            foreach (Light l in FindObjectsByType<Light>())
            {
                if (l == null || !l.isActiveAndEnabled || l.shadows == LightShadows.None || l.intensity <= 0f) continue;
                if (l.type == LightType.Directional)
                {
                    c.SunViews = Mathf.Max(c.SunViews, urp != null ? urp.shadowCascadeCount : 1);
                    continue;
                }
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(l.transform.position, Vector3.one * (2f * l.range)))) continue;
                bool point = l.type == LightType.Point;
                if (point) c.Points++;
                else c.Spots++;
                lights.Add(new CutLight { At = l.transform.position, Range = l.range, Views = point ? 6 : 1 });
            }
            Vector3 eye = cam.transform.position;
            c.HiddenViews = c.HiddenViewTris = 0;
            foreach (Renderer r in c.Casters)
            {
                int v = CutViewsOf(r.bounds, eye, c.SunViews, sunReach, lights);
                c.HiddenViews += v;
                c.HiddenViewTris += v * TrisOf(r);
            }
            var hidden = new HashSet<Renderer>(c.Casters);
            c.AllViews = c.AllViewTris = 0;
            c.OtherUnseen = 0;
            foreach (Renderer r in FindObjectsByType<Renderer>())
            {
                if (r == null || !(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled || r.forceRenderingOff) continue;
                if (r.shadowCastingMode == ShadowCastingMode.Off || !r.gameObject.activeInHierarchy) continue;
                int v = CutViewsOf(r.bounds, eye, c.SunViews, sunReach, lights);
                if (v == 0) continue;
                c.AllViews += v;
                c.AllViewTris += v * TrisOf(r);
                if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly && !hidden.Contains(r) && r.bounds.SqrDistance(eye) < sunReach * sunReach)
                    c.OtherUnseen++;
            }
        }

        private static int CutViewsOf(Bounds b, Vector3 eye, int sunViews, float sunReach, List<CutLight> lights)
        {
            int v = sunViews > 0 && b.SqrDistance(eye) <= sunReach * sunReach ? sunViews : 0;
            for (int i = 0; i < lights.Count; i++)
                if (b.SqrDistance(lights[i].At) <= lights[i].Range * lights[i].Range) v += lights[i].Views;
            return v;
        }

        private long TrisOf(Renderer r)
        {
            Mesh m = r is SkinnedMeshRenderer s ? s.sharedMesh : r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            if (m == null) return 0;
            if (_cutTris.TryGetValue(m, out long t)) return t;
            t = 0;
            for (int i = 0; i < m.subMeshCount; i++)
                if (m.GetTopology(i) == MeshTopology.Triangles) t += m.GetIndexCount(i) / 3;
            _cutTris[m] = t;
            return t;
        }

        // Frames each of the ways in turn, each round starting one way later so that none always follows another (drift
        // evens out): the renderers in off[k] are switched off for the whole of way k's frames, the game's own render
        // included, and back on after; a way with none to switch off (null) is left out. A batch editor's camera renders
        // the game's view besides the measured one, so the passes' times are per render.
        private IEnumerator CutFramesWays(Camera cam, RenderTexture rt, List<Renderer>[] off, CutSample[] ways)
        {
            for (int i = 0; i < WalkWarmup; i++)
            {
                RenderWalkFrame(cam, rt, null, -1);
                yield return null;
            }
            Recorder camera = Recorder.Get("UniversalRenderPipeline.RenderSingleCameraInternal: " + cam.name);
            Recorder main = Recorder.Get("Draw Main Light Shadowmap");
            Recorder extra = Recorder.Get("Draw Additional Lights Shadowmap");
            foreach (Recorder r in new[] { camera, main, extra }) if (r.isValid) r.enabled = true;
            var switched = new List<Renderer>();
            var sw = new Stopwatch();
            for (int round = 0; round < CutRounds; round++)
                for (int j = 0; j < ways.Length; j++)
                {
                    int k = (round + j) % ways.Length;
                    if (k > 0 && off[k] == null) continue;
                    CutSample s = ways[k];
                    if (off[k] != null)
                        foreach (Renderer r in off[k])
                            if (r != null && !r.forceRenderingOff)
                            {
                                r.forceRenderingOff = true;
                                switched.Add(r);
                            }
                    try
                    {
                        for (int i = 0; i < CutSkip + CutFrames; i++)
                        {
                            RenderWalkFrame(cam, rt, null, -1);
                            yield return null;
                            if (i < CutSkip) continue;   // the GPU's timings trail by a frame or two
                            int renders = camera.isValid ? Mathf.Max(1, camera.sampleBlockCount) : 1;
                            s.Renders = Math.Max(s.Renders, renders);
                            s.Frame.Add(Time.unscaledDeltaTime * 1000.0);
                            CutSampleRecorder(camera, renders, s.CamCpu, s.CamGpu);
                            CutSampleRecorder(main, renders, s.MainCpu, s.MainGpu);
                            CutSampleRecorder(extra, renders, s.ExtraCpu, s.ExtraGpu);
                        }
                        for (int i = 0; i < CutSynced; i++)
                        {
                            AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();   // from an idle GPU
                            sw.Restart();
                            RenderWalkFrame(cam, rt, null, -1);
                            AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                            s.Synced.Add(sw.Elapsed.TotalMilliseconds);
                        }
                    }
                    finally
                    {
                        foreach (Renderer r in switched) if (r != null) r.forceRenderingOff = false;
                        switched.Clear();
                    }
                }
        }

        private static void CutSampleRecorder(Recorder r, int renders, List<double> cpu, List<double> gpu)
        {
            if (!r.isValid) return;
            cpu.Add(r.elapsedNanoseconds / 1e6 / renders);
            if (r.gpuElapsedNanoseconds > 0) gpu.Add(r.gpuElapsedNanoseconds / 1e6 / renders);
        }

        private void CutLog(string stop, int way, CutCensus c, CutSample s)
        {
            Debug.Log("[Smoke] cutaway " + stop + ", " + CutStates[way] + ": frame " + Num(Median(s.Frame)) + " ms, render " + Num(Median(s.Synced))
                + " ms waited out; a render: camera cpu " + Num(Median(s.CamCpu)) + " gpu " + Num(Median(s.CamGpu)) + ", sun shadows cpu "
                + Num(Median(s.MainCpu)) + " gpu " + Num(Median(s.MainGpu)) + ", lamp shadows cpu " + Num(Median(s.ExtraCpu)) + " gpu "
                + Num(Median(s.ExtraGpu)) + " (" + s.Renders + " renders a frame)");
            string[] stopTime = stop.Split(' ');
            _cutCsv.Add(string.Join(",", Csv(stopTime[0]), Csv(stopTime.Length > 1 ? stopTime[1].Trim('(', ')') : ""), Csv(CutStates[way]),
                I(c.IsoPieces), I(c.IsoCasters), I(c.IsoTris), I(c.MapBuildings), I(c.MapOccupied), I(c.MapCasters), I(c.MapTris),
                I(c.Elsewhere != null ? c.Elsewhere.Count : -1), I(c.Elsewhere != null ? c.ElsewhereTris : -1), I(c.OtherUnseen),
                I(c.SunViews), I(c.Points), I(c.Spots), I(c.HiddenViews), I(c.HiddenViewTris), I(c.AllViews), I(c.AllViewTris),
                Num(Median(s.Frame)), Num(Median(s.Synced)), Num(Median(s.CamCpu)), Num(Median(s.CamGpu)), Num(Median(s.MainCpu)),
                Num(Median(s.MainGpu)), Num(Median(s.ExtraCpu)), Num(Median(s.ExtraGpu)), I(s.Renders)));
        }

        // A picture of the stop: as the game has it, or with `noShadows` drawn into no shadow map.
        private static void CutShot(string label, Camera cam, RenderTexture rt, List<Renderer> noShadows)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) return;
            var switched = new List<Renderer>();
            if (noShadows != null)
                foreach (Renderer r in noShadows)
                    if (r != null && !r.forceRenderingOff)
                    {
                        r.forceRenderingOff = true;
                        switched.Add(r);
                    }
            RenderTexture active = RenderTexture.active;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                RenderWalkFrame(cam, rt, null, -1);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                tex.Apply(false);
                File.WriteAllBytes(Path.Combine(s_ShotDir, "cutaway-" + label + ".png"), tex.EncodeToPNG());
            }
            catch (Exception e) { Debug.LogWarning("[Smoke] cutaway shot " + label + " failed: " + e.Message); }
            finally
            {
                RenderTexture.active = active;
                foreach (Renderer r in switched) if (r != null) r.forceRenderingOff = false;
                Destroy(tex);
            }
        }

        // East across the state a stop at a time, standing at a few spots round each so the rays meet what's there; what the
        // cutaway remembers at each, and nothing destroyed with what was let go of still in it a frame later.
        private IEnumerator CutCacheWalk(KentuckyStreamer streamer, Actor me, IsoCutaway iso, StringBuilder csv)
        {
            Recorder forget = Recorder.Get(IsoCutaway.ForgetProfilerName);
            if (forget.isValid) forget.enabled = true;
            _cutForgetMs = 0;
            Vector3 start = me.transform.position;
            int forgotten0 = iso.Forgotten, sites0 = MapCutaway.SitesLetGo, staleStops = 0, worstStale = 0, most = 0;
            float started = Time.realtimeSinceStartup;
            for (int hop = 1; hop <= CutHops; hop++)
            {
                Vector3 at = start + new Vector3(hop * CutHop, 0f, 0f);
                yield return CacheHopTo(streamer, me, at);
                yield return Until(() => streamer.IsNavigableAt(at), 30f);
                yield return CutWatch(3f, forget);   // the interiors round it furnished
                Vector3 here = me.transform.position;
                for (int i = 0; i < CutWalkSpots; i++)
                {
                    float a = i * Mathf.PI * 2f / CutWalkSpots;
                    Vector3 p = here + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * CutWalkRing;
                    if (!NavMesh.SamplePosition(p + Vector3.up, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
                    Teleport(me, hit.position + Vector3.up * 0.05f);
                    yield return CutWatch(CutWalkWait, forget);
                }
                // Whatever was let go of on the way forgotten (a frame after it went), then what's left that's destroyed.
                float until = Time.realtimeSinceStartup + 3f;
                while (iso.ForgetPending && Time.realtimeSinceStartup < until) yield return null;
                int stale = iso.ForgetPending ? -1 : iso.StaleEntries();
                if (stale != 0) staleStops++;
                worstStale = Mathf.Max(worstStale, stale);
                most = Mathf.Max(most, iso.CachedColliders);
                Debug.Log("[Smoke] cutaway cache " + hop + " (" + Mathf.RoundToInt(hop * CutHop) + " m east): " + iso.CachedColliders
                    + " colliders and " + iso.CachedPieces + " pieces remembered, " + (stale < 0 ? "still forgetting" : stale + " of them destroyed")
                    + "; " + (iso.Forgotten - forgotten0) + " forgotten so far as " + (MapCutaway.SitesLetGo - sites0)
                    + " pieces' sites, and the interiors and windows behind, went; the longest a frame spent forgetting " + Num(_cutForgetMs) + " ms");
                csv.Append(hop).Append(',').Append(Mathf.RoundToInt(hop * CutHop)).Append(',').Append(iso.CachedColliders).Append(',')
                    .Append(iso.CachedPieces).Append(',').Append(stale).Append(',').Append(iso.Forgotten - forgotten0).Append(',')
                    .Append(MapCutaway.SitesLetGo - sites0).Append(',').Append(Num(_cutForgetMs)).Append('\n');
            }
            int forgotten = iso.Forgotten - forgotten0;
            Check("cutaway: nothing let go of is still remembered", staleStops == 0 && forgotten > 0, CutHops + " stops over "
                + Mathf.RoundToInt(CutHops * CutHop) + " m in " + (Time.realtimeSinceStartup - started).ToString("0", CultureInfo.InvariantCulture)
                + " s; at most " + most + " colliders remembered, " + forgotten + " entries forgotten as " + (MapCutaway.SitesLetGo - sites0)
                + " pieces' sites, and the interiors and windows behind them, went (they'd have stayed until 4096 colliders were "
                + "remembered); " + (staleStops == 0 ? "none destroyed left behind" : staleStops + " stops left up to " + worstStale + " destroyed")
                + "; the longest a frame spent forgetting " + Num(_cutForgetMs) + " ms");
        }

        // Waits, keeping the longest a frame has spent forgetting.
        private IEnumerator CutWatch(float seconds, Recorder forget)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (forget.isValid) _cutForgetMs = Math.Max(_cutForgetMs, forget.elapsedNanoseconds / 1e6);
            }
        }

        private static float CutDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static string Round(Vector3 p) => "(" + Mathf.RoundToInt(p.x) + ", " + Mathf.RoundToInt(p.y) + ", " + Mathf.RoundToInt(p.z) + ")";

        private static string Pct(double part, double whole) => whole > 0 ? (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + "%" : "n/a";

        private static string I(long v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
