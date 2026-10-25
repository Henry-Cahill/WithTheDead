#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    // The cutaway harness's streamed pieces: their build steps timed with the shells baked at once, queued on the piece's
    // bake, or all batched (the counts before the cutaway); and pieces loaded, cut, let go of while cut and loaded again,
    // to check that nothing of them is left behind.
    public static partial class CutawayShots
    {
        private sealed partial class Harness
        {
            /// <summary>One streamed piece built the way <see cref="KentuckyStreamer"/> builds it.</summary>
            private sealed class Piece
            {
                public int Cx, Cz;
                public GameObject Root, Over;
                public readonly List<Mesh> Meshes = new List<Mesh>();
                public MapCutawaySite Site;
                public readonly List<KeyValuePair<string, double>> Steps = new List<KeyValuePair<string, double>>();
                public double TotalMs;
                public bool Failed;
                public long EmitTicks, Emitted;
            }

            private static readonly string[] Configs = { "batched (before)", "baked at once", "queued on the bake" };

            private readonly Dictionary<long, MapRegionIndex> _regions = new Dictionary<long, MapRegionIndex>();

            /// <summary>Collect and report timing data for building streamed pieces in various configurations.</summary>
            private void ChunkTimings()
            {
                // Header for the markdown table describing build step timings across configurations
                // Build markdown header describing the timing table for streamed pieces across configurations
                _md.Append("## Streamed pieces: build steps\n\nThe piece holding each place's middle, built the way the streamer builds it (`ChunkSteps`, ")
                    .Append("Full detail, every step timed back to back on the main thread; the waits while its meshes are packed on a worker don't count), ")
                    .Append("after a warm-up build of it, three times each (the middle total is shown, the range in brackets): with every building in the ")
                    .Append("shared batches as before the cutaway, with the shells baked as `CutawayPass` emits them (on the main thread), and queued on the ")
                    .Append("piece's bake (`Site.Bake`, packed on a worker with the piece's other meshes: what the game does). \"interior plans\" is the ")
                    .Append("interiors' tail, which `CutawayPass` closes with a step of its own (it used to be charged to \"cutaway\"); \"meshes\" is the ")
                    .Append("piece's own objects and meshes steps. Register = switching the piece on (its buildings into the cutaway's hash); read = every ")
                    .Append("building's meshes read at once (the game reads them a slice a frame round the survivor).\n\n")
                    .Append("| piece | shells | total ms | slowest step | cutaway steps: Σ / max ms | interior plans ms | meshes ms | register ms | read ms ")
                    .Append("| objects | renderers | vertices |\n|---|---|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|\n");
                var targets = new[] { new[] { "start-centre", _place.name }, new[] { "lexington-downtown", "Lexington" }, new[] { "louisville-downtown", "Louisville" } };
                // Create temporary root objects to hold streamed geometry for the benchmarks
                Transform streamed = new GameObject("Streamed").transform, overhead = new GameObject("RaidOverhead").transform;
                const int size = KentuckyMap.ChunkSize;
                try
                {
                    MapCutaway.Clear();
                    foreach (string[] t in targets)
                    {
                        // For each target location, find its map place and compute the chunk coordinates
                        MapPlace p = _build.FindPlace(t[1]);
                        if (p == null)
                        {
                            _md.Append("| ").Append(t[0]).Append(" | no place called ").Append(t[1]).Append(" |||||||||||\n");
                            continue;
                        }
                        int cx = (int)Math.Floor(p.x / size), cz = (int)Math.Floor(p.z / size);
                        var middle = new Vector3((float)((cx + 0.5) * size - p.x), 0f, (float)((cz + 0.5) * size - p.z));
                        // Build a piece once to warm up caches, then unload it before timing runs
                        UnloadPiece(BuildPiece(p, cx, cz, middle, streamed, overhead));
                        for (int config = 0; config < Configs.Length; config++)
                        {
                            // For each configuration (batched, baked, queued) run three builds and keep the middle timing as representative
                            MapCutaway.OwnShells = config != 0;
                            CutawayPass.Deferred = config == 2;
                            // Three builds; the one with the middle total is reported (one build's steps are noisy).
                            var rounds = new List<Piece>(3);
                            try
                            {
                                for (int round = 0; round < 3; round++)
                                {
                                    Piece piece = BuildPiece(p, cx, cz, middle, streamed, overhead);
                                    rounds.Add(piece);
                                    // Hide earlier rounds so they don't interfere with later timing
                                    if (round < 2) HideRound(piece);
                                }
                                // Sort by total time and pick the median piece for reporting
                                rounds.Sort((x, y) => x.TotalMs.CompareTo(y.TotalMs));
                                ReportPiece(t[0] + " " + cx + "_" + cz, config, rounds[1], rounds[0].TotalMs, rounds[2].TotalMs);
                            }
                            finally
                            {
                                // Ensure all temporary pieces are cleaned up even if a build fails
                                foreach (Piece piece in rounds) UnloadPiece(piece);
                            }
                        }
                    }
                }
                finally
                {
                    // After each configuration we restore the global cutaway state and destroy the temporary roots
                    // Restore global state after finishing all configurations for this target
                    // Clean up after the entire ChunkTimings operation for all targets
                    MapCutaway.OwnShells = true;
                    CutawayPass.Deferred = true;
                    MapCutaway.Clear();
                    Object.DestroyImmediate(streamed.gameObject);
                    Object.DestroyImmediate(overhead.gameObject);
                }
                _md.Append('\n');
            }

            // A round kept only for its timing: switched off so the next round's objects don't sit on top of it.
            private static void HideRound(Piece piece)
            {
                if (piece.Root != null) piece.Root.SetActive(false);
                if (piece.Over != null) piece.Over.SetActive(false);
            }

            /// <summary>Generate a markdown table row reporting timings and mesh stats for a piece.</summary>
            private void ReportPiece(string label, int config, Piece piece, double fastest, double slowestTotal)
            {
                double cutSum = 0, cutMax = 0, plans = 0, meshes = 0, slowest = 0;
                string slowestName = "";
                // Iterate over each recorded step, aggregating cutaway, interior and mesh times and finding the slowest step
                foreach (KeyValuePair<string, double> s in piece.Steps)
                {
                    if (s.Key == "cutaway")
                    {
                        cutSum += s.Value;
                        cutMax = Math.Max(cutMax, s.Value);
                    }
                    if (s.Key == "interior plans") plans += s.Value;
                    if (s.Key == "objects" || s.Key == "meshes") meshes += s.Value;
                    if (s.Value <= slowest) continue;
                    slowest = s.Value;
                    slowestName = s.Key;
                }
                double register = 0, read = 0;
                int shells = 0;
                if (piece.Site != null)
                {
                    shells = piece.Site.Buildings.Count;
                    piece.Root.SetActive(true);
                    piece.Over.SetActive(true);
                    // Register the piece's buildings into the cutaway and time how long it takes
                    var sw = Stopwatch.StartNew();
                    piece.Site.Register();
                    register = sw.Elapsed.TotalMilliseconds;
                    // Resolve all cutaway geometry and time the read step
                    sw.Restart();
                    MapCutaway.ResolveAll();
                    read = sw.Elapsed.TotalMilliseconds;
                }
                // Count meshes, renderers and vertices for both the piece's main and overhead objects
                Count(piece.Root.transform, out int o1, out int r1, out long v1);
                Count(piece.Over.transform, out int o2, out int r2, out long v2);
                // Append a markdown row with all collected timing and mesh statistics
                _md.Append("| ").Append(label).Append(" | ").Append(Configs[config]).Append(config > 0 ? " (" + shells + ")" : "").Append(" | ")
                    .Append(F1(piece.TotalMs)).Append(" (").Append(F0(fastest)).Append("-").Append(F0(slowestTotal)).Append(") | ")
                    .Append(slowestName).Append(' ').Append(F1(slowest)).Append(" | ")
                    .Append(config > 0 ? F1(cutSum) + " / " + F1(cutMax) : "").Append(" | ").Append(F1(plans)).Append(" | ").Append(F1(meshes)).Append(" | ")
                    .Append(config > 0 ? F1(register) : "").Append(" | ").Append(config > 0 ? F1(read) : "").Append(" | ")
                    .Append(F0(o1 + o2)).Append(" | ").Append(F0(r1 + r2)).Append(" | ").Append(F0(v1 + v2)).Append(" |\n");
                if (piece.Failed) Fail("piece " + label + " (" + Configs[config] + ") threw while building");
                if (piece.Site != null) piece.Site.Unregister();
            }

            // Four streamed pieces of Louisville, built as KentuckyStreamer builds them: their buildings register when the
            // pieces are switched on, one inside them is cut, its piece is let go of while cut and built again, and they all
            // go with their pieces (here unregistered by hand: nothing runs OnDisable in edit mode). Nothing may be left.
            /// <summary>
            /// Generate a markdown report that builds streamed pieces, then loads, cuts, unloads and reloads them to verify no leftovers.
            /// </summary>
            private void Pieces()
            {
                MapPlace lou = _build.FindPlace("Louisville");
                _md.Append("## Streamed pieces: load, cut, unload, reload\n\n");
                if (lou == null)
                {
                    _md.Append("No Louisville in this build.\n\n");
                    return;
                }
                MapCutaway.Clear();
                int overlays0 = OverlayMeshes();
                long bytes0 = MapCutaway.HeightBytes;
                const int size = KentuckyMap.ChunkSize;
                int cx0 = (int)Math.Floor(lou.x / size), cz0 = (int)Math.Floor(lou.z / size);
                Transform streamed = new GameObject("Streamed").transform, overhead = new GameObject("RaidOverhead").transform;
                var pieces = new List<Piece>();
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int cx = cx0 + (i & 1), cz = cz0 + (i >> 1);
                        pieces.Add(BuildPiece(lou, cx, cz, Middle(lou, cx, cz), streamed, overhead));
                    }
                    int registered = 0;
                    _buildings.Clear();
                    foreach (Piece p in pieces)
                    {
                        if (p.Site == null) continue;
                        p.Site.Register();
                        registered += p.Site.Buildings.Count;
                        _buildings.AddRange(p.Site.Buildings);
                    }
                    _md.Append("Built pieces ").Append(cx0).Append('_').Append(cz0).Append(" to ").Append(cx0 + 1).Append('_').Append(cz0 + 1)
                        .Append(" (Full detail): ").Append(registered).Append(" buildings registered (").Append(MapCutaway.Registered).Append(" in the cutaway, ")
                        .Append(MapCutaway.CellCount).Append(" hash cells).\n\n");
                    if (registered != MapCutaway.Registered) Fail("the pieces registered " + MapCutaway.Registered + " buildings, their lists hold " + registered);
                    MapCutaway.ResolveAll();
                    CollectLights(new[] { streamed, overhead });
                    CheckMeshes("the Louisville pieces");

                    // The biggest building of the first piece: stand inside it, cut, photograph. The tallest other one with
                    // a free spot behind it inside the four pieces: stand there.
                    Vector3 block0 = Middle(lou, cx0, cz0);
                    Rect block = Rect.MinMaxRect(block0.x - size * 0.5f + 8f, block0.z - size * 0.5f + 8f, block0.x + size * 1.5f - 8f, block0.z + size * 1.5f - 8f);
                    MapCutaway.Building big = null, tall = null;
                    Vector3 tallSpot = default;
                    if (pieces[0].Site != null)
                        foreach (MapCutaway.Building b in pieces[0].Site.Buildings)
                            if (b.Walls != null && b.Upper != null && (big == null || Area(b.Local) > Area(big.Local))) big = b;
                    foreach (MapCutaway.Building b in _buildings)
                    {
                        if (b.Upper == null || b == big || (tall != null && b.Top <= tall.Top)) continue;
                        Vector3? behind = BehindOf(b, _isoYaw);
                        if (!behind.HasValue || !block.Contains(new Vector2(behind.Value.x, behind.Value.z))) continue;
                        tall = b;
                        tallSpot = behind.Value;
                    }
                    _md.Append("| file | shows | cut away | old test |\n|---|---|---|---|\n");
                    if (tall != null)
                    {
                        string what = "Behind " + Describe(tall) + " in streamed Louisville";
                        Shot("25_louisville_behind_roofs_on.png", what + ", roofs on (before)", tallSpot, _isoYaw, Wide, true, false, false);
                        Shot("25_louisville_behind.png", what, tallSpot, _isoYaw, Wide, true, true, false);
                    }
                    if (big == null)
                    {
                        _md.Append("\nNo building with walls and a roof in piece ").Append(cx0).Append('_').Append(cz0).Append(".\n\n");
                        return;
                    }
                    long bigId = big.Id;
                    Vector3 at = Inside(big);
                    string inside = "Inside " + Describe(big) + " in streamed piece " + cx0 + "_" + cz0;
                    Shot("24_louisville_inside_roofs_on.png", inside + ", roofs on (before)", at, _isoYaw, Wide, true, false, false);
                    Shot("24_louisville_inside.png", inside, at, _isoYaw, Wide, true, true, false);
                    _md.Append('\n');
                    int cutBefore = MapCutaway.CutBuildings, overlaysCut = OverlayMeshes();

                    // Let the piece go while it's cut, as the streamer would.
                    Piece first = pieces[0];
                    List<MapCutaway.Building> gone = first.Site.Buildings;
                    int goneOverlays = 0;
                    foreach (MapCutaway.Building b in gone)
                        if (b.OverlayMesh != null) goneOverlays++;
                    first.Site.Unregister();
                    UnloadPiece(first);
                    pieces.RemoveAt(0);
                    bool clean = MapCutaway.Registered == registered - gone.Count && !StillCut(gone);
                    int overlaysAfter = OverlayMeshes();
                    MapCutaway.Run(at, _isoYaw, _isoPitch, Reach(Wide), true, _t += 10f, true);
                    _md.Append("Cut inside it: ").Append(cutBefore).Append(" building(s), ").Append(goneOverlays).Append(" cut-wall stand-in(s) of its piece made. ")
                        .Append("Its piece let go of while cut: ").Append(MapCutaway.Registered).Append(" still registered, ")
                        .Append(clean ? "nothing of it left cut or registered" : "**something of it was left behind**").Append(", stand-ins ")
                        .Append(overlaysCut).Append(" → ").Append(overlaysAfter).Append("; a scan on the empty spot afterwards cut ")
                        .Append(MapCutaway.CutBuildings).Append(" buildings.\n\n");
                    if (!clean) Fail("letting go of a piece while cut left some of it behind");
                    if (overlaysAfter != overlaysCut - goneOverlays) Fail("the stand-ins of a piece let go of weren't destroyed");

                    // Build it again, as the streamer would when the survivor comes back, and cut the same building.
                    Piece again = BuildPiece(lou, cx0, cz0, Middle(lou, cx0, cz0), streamed, overhead);
                    pieces.Add(again);
                    MapCutaway.Building same = null;
                    if (again.Site != null)
                    {
                        again.Site.Register();
                        foreach (MapCutaway.Building b in again.Site.Buildings)
                            if (b.Id == bigId) same = b;
                    }
                    MapCutaway.RestoreAll();
                    MapCutaway.ResolveAll();
                    MapCutaway.Run(at, _isoYaw, _isoPitch, Reach(Wide), true, _t += 10f, true);
                    bool recut = same != null && same.Applied != 0;
                    _md.Append("Built again: ").Append(MapCutaway.Registered).Append(" registered; standing in the same spot cuts ")
                        .Append(recut ? Parts(same.Applied) + " of it again" : "**nothing of it**").Append(".\n\n");
                    if (!recut) Fail("a piece built again didn't cut where it did before");
                }
                finally
                {
                    foreach (Piece p in pieces)
                    {
                        if (p.Site != null) p.Site.Unregister();
                        UnloadPiece(p);
                    }
                    Object.DestroyImmediate(streamed.gameObject);
                    Object.DestroyImmediate(overhead.gameObject);
                    _buildings.Clear();
                    int overlays = OverlayMeshes();
                    _md.Append("Every piece let go of: ").Append(MapCutaway.Registered).Append(" buildings and ").Append(MapCutaway.SiteCount).Append(" sites registered, ")
                        .Append(MapCutaway.CellCount).Append(" hash cells, ").Append(MapCutaway.CutBuildings).Append(" cut, ")
                        .Append(MapCutaway.HeightBytes - bytes0).Append(" bytes of heights and ").Append(overlays - overlays0).Append(" stand-ins left.\n\n");
                    if (MapCutaway.Registered != 0 || MapCutaway.SiteCount != 0 || MapCutaway.CellCount != 0 || MapCutaway.CutBuildings != 0
                        || MapCutaway.HeightBytes != bytes0 || overlays != overlays0)
                        Fail("something was left behind after every piece went");
                }
            }

            private static Vector3 Middle(MapPlace p, int cx, int cz)
                => new Vector3((float)((cx + 0.5) * KentuckyMap.ChunkSize - p.x), 0f, (float)((cz + 0.5) * KentuckyMap.ChunkSize - p.z));

            private static bool StillCut(List<MapCutaway.Building> list)
            {
                foreach (MapCutaway.Building b in list)
                    if (b.Applied != 0 || b.Active || b.Site != null || b.Overlay != null || b.OverlayMesh != null) return true;
                return false;
            }

            private static int OverlayMeshes()
            {
                int n = 0;
                foreach (Mesh m in Resources.FindObjectsOfTypeAll<Mesh>())
                    if (m != null && m.name.StartsWith("Walls cut ", StringComparison.Ordinal)) n++;
                return n;
            }

            // KentuckyStreamer's Begin, Steps and Finish for one piece (no loot roll, no NavMesh), every step timed.
            /// <summary>
            /// Build a single streamed piece, timing each step, and optionally register, cut, and read its meshes.
            /// </summary>
            private Piece BuildPiece(MapPlace place, int cx, int cz, Vector3 middle, Transform streamed, Transform overhead)
            {
                const int size = KentuckyMap.ChunkSize;
                int per = _build.RegionSize / size;
                int rx = cx >= 0 ? cx / per : -((-cx + per - 1) / per), rz = cz >= 0 ? cz / per : -((-cz + per - 1) / per);
                long key = ((long)rx << 32) | (uint)rz;
                if (!_regions.TryGetValue(key, out MapRegionIndex region))
                    _regions[key] = region = new MapRegionIndex(_build.HasRegion(rx, rz) ? _build.LoadRegion(rx, rz) : null, rx, rz, _build.RegionSize, size);
                // Create GameObjects that will hold the piece's geometry and its overhead objects
                string name = "Piece " + cx + "_" + cz;
                var piece = new Piece { Cx = cx, Cz = cz, Root = new GameObject(name), Over = new GameObject(name) };
                piece.Root.SetActive(false);
                piece.Root.transform.SetParent(streamed, false);
                piece.Over.SetActive(false);
                piece.Over.transform.SetParent(overhead, false);
                // Set up the job that drives KentuckyMap's chunk building for this piece
                var job = new KentuckyMap.ChunkJob
                {
                    Build = _build, Region = region, Place = place.name, Cx = cx, Cz = cz, Seed = unchecked(_zone.LayoutSeed * 486187739 + cx * 73856093 ^ cz * 19349663),
                    Root = piece.Root.transform, Overhead = piece.Over.transform, Offset = middle, Detail = KentuckyMap.Detail.Full,
                };
                // Swap global state so that the chunk builder writes into this piece's collections
                List<Mesh> outerMeshes = RaidMesh.CreatedMeshes;
                List<string> outerSigns = DecorTextures.SignsUsed;
                Vector3 outerUv = RaidMesh.PlanarOrigin;
                object owner = Shelter.Owner;
                Func<string, bool> gate = ItemDatabase.LootGate;
                Action<string, int> rolled = ItemDatabase.LootRolled;
                RaidMesh.CreatedMeshes = piece.Meshes;
                DecorTextures.SignsUsed = new List<string>();
                RaidMesh.PlanarOrigin = new Vector3((float)Wrap((cx + 0.5) * size), 0f, (float)Wrap((cz + 0.5) * size));
                Shelter.Owner = piece.Root;
                ItemDatabase.LootGate = null;
                ItemDatabase.LootRolled = null;
                long emitted = CutawayPass.Emitted, emitTicks = CutawayPass.EmitTicks;
                var total = Stopwatch.StartNew();
                var sw = new Stopwatch();
                double waited = 0;
                try
                {
                    // Iterate over each chunk step, measuring elapsed time; handle the special WaitStep where meshes are packed async
                    using (IEnumerator<string> steps = KentuckyMap.ChunkSteps(job).GetEnumerator())
                        while (true)
                        {
                            sw.Restart();
                            bool more;
                            try
                            {
                                if (job.Site != null) job.Site.BeginStep();
                                more = steps.MoveNext();
                            }
                            catch (Exception e)
                            {
                                piece.Failed = true;
                                job.Abandon();
                                Debug.LogError("[CutawayShots] building piece " + cx + "_" + cz + " threw: " + e);
                                break;
                            }
                            if (!more) break;
                            if (steps.Current == KentuckyMap.WaitStep)
                            {
                                // The meshes are being packed on a worker: the streamer gives these frames to other work.
                                System.Threading.Thread.Yield();
                                waited += sw.Elapsed.TotalMilliseconds;
                                continue;
                            }
                            piece.Steps.Add(new KeyValuePair<string, double>(steps.Current, sw.Elapsed.TotalMilliseconds));
                        }
                }
                finally
                {
                    piece.TotalMs = total.Elapsed.TotalMilliseconds - waited;
                    piece.Emitted = CutawayPass.Emitted - emitted;
                    piece.EmitTicks = CutawayPass.EmitTicks - emitTicks;
                    // Restore the original global state so later builds are unaffected
                    RaidMesh.CreatedMeshes = outerMeshes;
                    DecorTextures.SignsUsed = outerSigns;
                    RaidMesh.PlanarOrigin = outerUv;
                    Shelter.Owner = owner;
                    ItemDatabase.LootGate = gate;
                    ItemDatabase.LootRolled = rolled;
                }
                // Position the piece at its calculated middle point and enable it for later use
                piece.Root.transform.position = middle;
                piece.Over.transform.position = middle;
                piece.Root.SetActive(true);
                piece.Over.SetActive(true);
                piece.Site = piece.Root.GetComponentInChildren<MapCutawaySite>(true);
                return piece;
            }

            private static void UnloadPiece(Piece p)
            {
                if (p == null) return;
                if (p.Site != null) p.Site.Unregister();
                if (p.Root != null)
                {
                    Shelter.Forget(p.Root);
                    Object.DestroyImmediate(p.Root);
                }
                if (p.Over != null) Object.DestroyImmediate(p.Over);
                foreach (Mesh m in p.Meshes)
                    if (m != null) Object.DestroyImmediate(m);
                p.Meshes.Clear();
            }

            private static double Wrap(double v)
            {
                double r = v % RaidMesh.PlanarPeriod;
                return r < 0 ? r + RaidMesh.PlanarPeriod : r;
            }
        }
    }
}
#endif
