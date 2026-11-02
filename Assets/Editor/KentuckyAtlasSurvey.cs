using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CloneZ.EditorTools
{
    // The atlas run headless: the buildings round a town's middle built one by one as the window builds them, and what the
    // game made of each and what the checks found, for a first sweep of a town (and to check the atlas itself).
    public sealed partial class KentuckyAtlasWindow
    {
        /// <summary>
        /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.KentuckyAtlasWindow.Survey
        /// -clonez-out &lt;dir&gt; [-clonez-atlas-place Bardstown] [-clonez-atlas-count 10] [-clonez-atlas-save]
        /// [-clonez-atlas-levels] [-clonez-atlas-breach] [-clonez-atlas-windows] [-clonez-atlas-ids &lt;id,id,…&gt;] -logFile &lt;log&gt;</c>
        /// with <c>CLONEZ_WORLD</c> pointing at a world store. Builds the buildings nearest the place's middle, each in its
        /// piece with its interior, and writes report.md (what the game made of each, the doors, windows and inside the checks
        /// found wrong) and atlas-map.png (the map as the window draws it). With -clonez-atlas-save it also saves a record with
        /// corrections for the second building, checks the game builds it the corrected way, and leaves the record in the
        /// store's validation folder (so point CLONEZ_WORLD at a copy). With -clonez-atlas-levels it takes the nearest buildings
        /// likely to have floors above or below the ground one (mapped with storeys, or houses), checks every floor (furnished
        /// where it stands, reached from the front door on the NavMesh) and takes the cutaway's pictures of the first few,
        /// a floor at a time. With -clonez-atlas-breach it knocks each furnished floor's longest partition through and checks the
        /// hole (cut clean, the NavMesh leading through it, boards closing it), the place the dead pick to batter the next, and
        /// (single-storey buildings) that a floor raised again keeps its holes; on the ground floor it does the same for the
        /// longest stretch of outside wall with room for a hole (the NavMesh leading in from outside, the dead outside picking
        /// the next stretch to get at their prey inside). With -clonez-atlas-windows it fits every furnished floor's windows
        /// as the streamer does round a survivor and looks through each working and fixed one (nothing solid across it but its
        /// own boards or drawn curtains, its glass see-through, the NavMesh not leading through it), counting the painted ones,
        /// and pictures a few from outside and from the room. Exits 0, 1 when the survey failed, 2 when the
        /// game logged errors or a correction wasn't applied, 3 with no world store.
        /// </summary>
        public static void Survey()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = Arg(args, "-clonez-out") ?? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "AtlasSurvey");
            string place = Arg(args, "-clonez-atlas-place") ?? "Bardstown";
            int count = int.TryParse(Arg(args, "-clonez-atlas-count"), out int n) ? Mathf.Clamp(n, 1, 500) : 10;
            bool save = Array.IndexOf(args, "-clonez-atlas-save") >= 0;
            bool levels = Array.IndexOf(args, "-clonez-atlas-levels") >= 0;
            // Each furnished floor's longest partition knocked through, boarded and opened again, and the dead's pick of the next.
            KentuckyAtlas.BreachTest = Array.IndexOf(args, "-clonez-atlas-breach") >= 0;
            KentuckyAtlas.BreachPictures = KentuckyAtlas.BreachTest ? outDir : null;
            // Every furnished floor's windows fitted and looked through.
            KentuckyAtlas.WindowTest = Array.IndexOf(args, "-clonez-atlas-windows") >= 0;
            KentuckyAtlas.WindowPictures = KentuckyAtlas.WindowTest ? outDir : null;
            HashSet<long> only = null;
            string ids = Arg(args, "-clonez-atlas-ids");
            if (ids != null)
            {
                only = new HashSet<long>();
                foreach (string id in ids.Split(','))
                    if (long.TryParse(id.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)) only.Add(v);
            }
            int code = 1;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                code = RunSurvey(outDir, place, count, save, levels, only);
            }
            catch (Exception e)
            {
                Debug.LogError("[KentuckyAtlas] The survey failed: " + e);
                try
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllText(Path.Combine(outDir, "report.md"), "# Kentucky Atlas survey\n\nFailed: " + e + "\n");
                }
                catch (Exception) { }
            }
            finally
            {
                KentuckyAtlas.Clear();
                KentuckyAtlas.BreachTest = false;
                KentuckyAtlas.BreachPictures = null;
                KentuckyAtlas.WindowTest = false;
                KentuckyAtlas.WindowPictures = null;
            }
            EditorApplication.Exit(code);
        }

        private static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : null;
        }

        private static int RunSurvey(string outDir, string placeName, int count, bool save, bool levels, HashSet<long> only)
        {
            Directory.CreateDirectory(outDir);
            var md = new StringBuilder("# Kentucky Atlas survey\n\n");
            MapBuild build = WorldStore.OpenCurrent(out string why);
            if (build == null)
            {
                File.WriteAllText(Path.Combine(outDir, "report.md"), md + "No world store at " + WorldStore.Root + ": " + why + "\n");
                return 3;
            }
            build.UseCorrections = false;
            MapPlace place = build.FindPlace(placeName);
            if (place == null)
            {
                File.WriteAllText(Path.Combine(outDir, "report.md"), md + "Build " + build.Id + " has no place called " + placeName + ".\n");
                return 1;
            }
            int size = build.RegionSize, rx = (int)Math.Floor(place.x / size), rz = (int)Math.Floor(place.z / size);
            MapRegion data = build.LoadRegion(rx, rz);
            var near = new List<KeyValuePair<double, MapBuilding>>();
            foreach (MapBuilding b in data.buildings ?? new MapBuilding[0])
            {
                if (b == null || b.pts == null || b.pts.Length < 6) continue;
                // Just the ones asked for; else floors above or below the ground one: mapped with storeys, or a house (about
                // half of them have a basement).
                if (only != null ? !only.Contains(b.id) : levels && b.levels < 2 && !(b.kind == "house" && string.IsNullOrEmpty(b.use))) continue;
                double sx = 0, sz = 0;
                for (int k = 0; k + 1 < b.pts.Length; k += 2)
                {
                    sx += b.pts[k];
                    sz += b.pts[k + 1];
                }
                int m = b.pts.Length / 2;
                double dx = (double)rx * size + sx / m / 10.0 - place.x, dz = (double)rz * size + sz / m / 10.0 - place.z;
                near.Add(new KeyValuePair<double, MapBuilding>(dx * dx + dz * dz, b));
            }
            near.Sort((a, b) => a.Key.CompareTo(b.Key));
            if (near.Count > count) near.RemoveRange(count, near.Count - count);

            int errors = 0;
            var firstErrors = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                errors++;
                if (firstErrors.Count < 10) firstErrors.Add(message);
            }
            Application.logMessageReceived += OnLog;
            md.Append("Build ").Append(build.Id).Append(", ").Append(near.Count).Append(" buildings nearest the middle of ").Append(place.name)
                .Append(" (region ").Append(rx).Append('_').Append(rz).Append("), each built in its piece the way the game streams it, furnished, ")
                .Append("with the store's corrections. Unity ").Append(Application.unityVersion).Append(". Map data © OpenStreetMap contributors (ODbL).\n\n")
                .Append("| building | kind (use) | the game made | doors | windows | inside | floors | problems | ms |\n|---|---|---|--:|--:|---|---|---|--:|\n");
            var reports = new List<AtlasReport>();
            var levelLines = new StringBuilder();
            var breachLines = new StringBuilder();
            var windowLines = new StringBuilder();
            var pictures = new List<string>();
            int built = 0, withProblems = 0, leveled = 0, pictured = 0, fixtures = 0, outOfReach = 0, breaches = 0, through = 0, outside = 0, outsideThrough = 0;
            int working = 0, fixedClear = 0, painted = 0, looked = 0, seen = 0, screened = 0;
            try
            {
                foreach (KeyValuePair<double, MapBuilding> kv in near)
                {
                    MapBuilding b = kv.Value;
                    AtlasReport r = KentuckyAtlas.Build(build, rx, rz, b, false, true);
                    reports.Add(r);
                    if (r.Built) built++;
                    if (r.Problems.Count > 0) withProblems++;
                    fixtures += r.Fixtures;
                    outOfReach += r.FixturesOutOfReach;
                    breaches += r.Breaches;
                    through += r.BreachesThrough;
                    outside += r.OutsideBreaches;
                    outsideThrough += r.OutsideThrough;
                    working += r.WindowsWorking;
                    fixedClear += r.WindowsClear;
                    painted += r.WindowsPainted;
                    looked += r.WindowsLooked;
                    seen += r.WindowsSeen;
                    screened += r.WindowsScreened;
                    foreach (string note in r.Notes)
                    {
                        if (note.StartsWith("Breach:", StringComparison.Ordinal))
                            breachLines.Append("- `").Append(b.id).Append("` ").Append(Md(note.Substring(7).Trim())).Append('\n');
                        else if (note.StartsWith("Windows:", StringComparison.Ordinal))
                            windowLines.Append("- `").Append(b.id).Append("` ").Append(Md(note.Substring(8).Trim())).Append('\n');
                    }
                    string floors = "";
                    if (!string.IsNullOrEmpty(r.Levels))
                    {
                        leveled++;
                        floors = r.Levels + ", " + r.LevelsReached + " of " + r.LevelLines.Count + " reached";
                        levelLines.Append("### `").Append(rx).Append('_').Append(rz).Append('_').Append(b.id).Append("` ").Append(Md(r.Summary)).Append("\n\n");
                        foreach (string line in r.LevelLines) levelLines.Append("- ").Append(line).Append('\n');
                        levelLines.Append('\n');
                        if (levels && pictured < 4)
                        {
                            pictured++;
                            pictures.AddRange(KentuckyAtlas.LevelPictures(r, outDir));
                        }
                    }
                    md.Append("| `").Append(rx).Append('_').Append(rz).Append('_').Append(b.id).Append("` | ").Append(Md(b.kind))
                        .Append(string.IsNullOrEmpty(b.use) ? "" : " (" + Md(b.use) + ")").Append(" | ").Append(Md(r.Built ? r.Summary : "not built: " + r.Error))
                        .Append(" | ").Append(r.Doors).Append(r.BlockedDoors > 0 ? " (" + r.BlockedDoors + " blocked)" : "").Append(" | ").Append(r.Windows)
                        .Append(" | ").Append(r.Furnished ? Md(r.Use + ": " + r.RoomCount + " rooms, " + r.Containers + " containers") : r.Enterable ? "**not furnished**" : "-")
                        .Append(" | ").Append(Md(floors))
                        .Append(" | ").Append(r.Problems.Count == 0 ? "" : Md(string.Join("; ", r.Problems))).Append(" | ")
                        .Append(Math.Round(r.BuildMs + r.FurnishMs).ToString(CultureInfo.InvariantCulture)).Append(" |\n");
                }
                md.Append("\n").Append(built).Append(" of ").Append(near.Count).Append(" built; ").Append(withProblems).Append(" with something the checks found wrong; ")
                    .Append(leveled).Append(" with floors above or below the ground one; ").Append(fixtures).Append(" fixtures furnished, ")
                    .Append(outOfReach).Append(" of them out of a survivor's reach.\n\n");
                if (levelLines.Length > 0) md.Append("## Floors\n\n").Append(levelLines);
                if (KentuckyAtlas.BreachTest)
                    md.Append("\n## Breaches\n\n").Append(breaches).Append(" partitions knocked through, the NavMesh leading through ").Append(through)
                        .Append(" of the holes; ").Append(outside).Append(" outside walls knocked through, the NavMesh leading in through ")
                        .Append(outsideThrough).Append(" of those.\n\n").Append(breachLines).Append('\n');
                if (KentuckyAtlas.WindowTest)
                    md.Append("\n## Windows\n\n").Append(working).Append(" working windows and ").Append(fixedClear)
                        .Append(" fixed ones (real openings, clear glass); ").Append(painted).Append(" painted on (no floor at them). Looked through ")
                        .Append(looked).Append(" of the working and fixed ones: the eye went through ").Append(seen).Append(", ").Append(screened)
                        .Append(" boarded or with their curtains drawn.\n\n").Append(windowLines).Append('\n');
                if (pictures.Count > 0)
                {
                    md.Append("## Pictures\n\nThe game's isometric view, the survivor (red) on each floor's landing, after the cutaway's scan.\n\n")
                        .Append("| file | shows | cut away |\n|---|---|---|\n");
                    foreach (string row in pictures) md.Append(row).Append('\n');
                    md.Append('\n');
                }

                bool corrected = true;
                if (save && near.Count >= 2)
                    corrected = RoundTrip(build, rx, rz, near[1].Value, md);
                KentuckyAtlas.Clear();

                string png = Path.Combine(outDir, "atlas-map.png");
                RenderPng(place.x, place.z, 0.8, 1400, 900, png);
                md.Append("The map as the atlas draws it round ").Append(place.name).Append(": `atlas-map.png`.\n\n");
                md.Append("## Log\n\n").Append(errors).Append(" errors logged").Append(firstErrors.Count > 0 ? ":\n\n" : ".\n");
                foreach (string e in firstErrors) md.Append("- `").Append(Md(e.Split('\n')[0])).Append("`\n");
                File.WriteAllText(Path.Combine(outDir, "report.md"), md.ToString());
                Debug.Log("[KentuckyAtlas] Survey of " + place.name + ": " + built + " of " + near.Count + " built, " + withProblems + " with problems, "
                    + errors + " errors logged. " + outDir);
                return errors > 0 || !corrected ? 2 : 0;
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }

        // Saves a record with corrections for a building, reads it back, and builds the building with it: the game must
        // build it the corrected way.
        private static bool RoundTrip(MapBuild build, int rx, int rz, MapBuilding b, StringBuilder md)
        {
            var rec = new BuildingCheck { id = b.id, note = "Kentucky Atlas survey: a correction to check the game applies it" };
            MapValidation.SetCheck(rec.checks, 0, MapValidation.Ok);
            MapValidation.SetCheck(rec.checks, 2, MapValidation.Bad);
            rec.fix.arch = "Church";
            rec.fix.roof = "hipped";
            rec.fix.storeys = 2;
            rec.fix.facing = "S";
            rec.build = build.Id;
            rec.by = "unity@" + Environment.MachineName;
            rec.updated = MapValidation.Now();
            MapValidation.Save(build.ValidationFolder, rx, rz, rec);
            BuildingChecksFile back = MapValidation.Load(build.ValidationFolder, rx, rz);
            BuildingCheck read = back != null ? Array.Find(back.buildings, c => c != null && c.id == b.id) : null;
            bool same = read != null && read.fix.arch == "Church" && read.fix.roof == "hipped" && read.fix.storeys == 2 && read.checks.outside == MapValidation.Bad
                && read.updated == rec.updated;
            AtlasReport r = KentuckyAtlas.Build(build, rx, rz, b, false, true);
            bool applied = r.Built && r.Arch == "Church" && r.Roof == "Hip" && r.Storeys == 2;
            AtlasReport plain = KentuckyAtlas.Build(build, rx, rz, b, false, false);
            md.Append("## Corrections\n\nSaved a record for `").Append(rx).Append('_').Append(rz).Append('_').Append(b.id).Append("` (archetype Church, hipped roof, ")
                .Append("2 storeys, front facing S) to ").Append(MapValidation.PathOf(build.ValidationFolder, rx, rz)).Append(": read back ")
                .Append(same ? "the same" : "**different**").Append(". Built with it: ").Append(Md(r.Summary)).Append(applied ? " (**applied**)" : " (**not applied**)")
                .Append("; as mapped: ").Append(Md(plain.Summary)).Append(".\n\n");
            return same && applied;
        }

        // The map round a point as the window draws it (without what's drawn over it), to a PNG.
        private static void RenderPng(double x, double z, double mpp, int w, int h, string path)
        {
            var win = CreateInstance<KentuckyAtlasWindow>();
            RenderTexture flat = null;
            Texture2D tex = null;
            try
            {
                if (win._build == null) throw new Exception("no world store: " + win._why);
                win._cx = x;
                win._cz = z;
                win._mpp = mpp;
                win._mapRect = new Rect(0f, 0f, w, h);
                int size = win._build.RegionSize;
                double halfW = w * 0.5 * mpp, halfH = h * 0.5 * mpp;
                for (int rz = (int)Math.Floor((z - halfH) / size); rz <= (int)Math.Floor((z + halfH) / size); rz++)
                    for (int rx = (int)Math.Floor((x - halfW) / size); rx <= (int)Math.Floor((x + halfW) / size); rx++)
                    {
                        if (!win._build.HasRegion(rx, rz)) continue;
                        AtlasRegion r = Prepare(win._build, rx, rz);
                        win.MakeMeshes(r);
                        win._regions[Key(rx, rz)] = r;
                    }
                win._redraw = true;
                win.RenderMap(1f);
                flat = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(win._rt, flat);
                RenderTexture was = RenderTexture.active;
                RenderTexture.active = flat;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = was;
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                if (flat != null) RenderTexture.ReleaseTemporary(flat);
                if (tex != null) DestroyImmediate(tex);
                DestroyImmediate(win);
            }
        }

        private static string Md(string s) => (s ?? "").Replace("|", "\\|").Replace("\n", " ");
    }
}
