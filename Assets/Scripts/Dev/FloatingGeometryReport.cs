#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// Dev aid, editor only: in Play Mode, writes <c>Logs/FloatingGeometry.txt</c>, a capture of the Game view
    /// (<c>Logs/FloatingGeometry.png</c>) and close-ups of a few zombies (<c>Logs/FloatingGeometry_closeup_*.png</c>). It
    /// flags vertices of a character's skinned body hanging away from the character as posed this frame (baked, because the
    /// renderer's bounds never show them), separate parts under a character that are away from it or off its posed body,
    /// bones that have wandered off, and anything drawn in the world that, with everything it touches, touches nothing
    /// reaching the ground; it lists, for the eye only, anything drawn wholly above the rooftops, line renderers and every
    /// scene's root objects. Run it from <c>CloneZ &gt; Debug &gt; Report Floating Geometry</c> or F10 in
    /// <see cref="DebugMode"/>. The lobby and wave smoke runs take it through <see cref="Check"/> and fail on what it flags
    /// (SmokeTest.Floating.cs).
    /// </summary>
    public static partial class FloatingGeometryReport
    {
        private const float RoofTop = 3.0f;   // the wave world draws nothing wholly above this
        private const int MaxListed = 80;

        /// <summary>One thing the report flags. <see cref="Key"/> names it the same way from one run to the next (by
        /// names and paths, never positions or counts), so the smoke test can tell known false positives from new
        /// findings; <see cref="Detail"/> says where it is and what's wrong with it.</summary>
        public readonly struct Finding
        {
            public readonly string Key, Detail;
            public readonly Object Target;

            public Finding(string key, string detail, Object target)
            {
                Key = key;
                Detail = detail;
                Target = target;
            }
        }

        private struct Group
        {
            public int Count;
            public float Far;
            public Vector3 Sample;
        }

        public static void Run()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(dir);
            string txt = Path.Combine(dir, "FloatingGeometry.txt");
            string png = Path.Combine(dir, "FloatingGeometry.png");

            StringBuilder sb = Compose(out List<Finding> found, out Transform worst);
            File.WriteAllText(txt, sb.ToString());
            ScreenCapture.CaptureScreenshot(png);
            CloseUps(dir, worst, Camera.main, sb);
            File.WriteAllText(txt, sb.ToString());
            var flagged = new List<Object>();
            foreach (Finding f in found)
                if (f.Target != null && flagged.Count < 200 && !flagged.Contains(f.Target)) flagged.Add(f.Target);
            if (flagged.Count > 0 && !Application.isBatchMode)
            {
                Selection.objects = flagged.ToArray();
                EditorGUIUtility.PingObject(flagged[0]);
            }
            Debug.Log("[FloatingGeometry] " + found.Count + " flagged. Report: " + txt + "  Game view: " + png);
        }

        /// <summary>The report without its pictures or the editor's selection: writes the text to <paramref name="txt"/>
        /// and returns what it flagged.</summary>
        public static List<Finding> Check(string txt)
        {
            StringBuilder sb = Compose(out List<Finding> found, out _);
            Directory.CreateDirectory(Path.GetDirectoryName(txt));
            File.WriteAllText(txt, sb.ToString());
            return found;
        }

        private static StringBuilder Compose(out List<Finding> found, out Transform worst)
        {
            var sb = new StringBuilder(16384);
            found = new List<Finding>();
            Camera cam = Camera.main;
            sb.Append("Floating geometry report, ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
              .Append("  mode=").Append(GameSession.Mode).Append("  scene=").Append(SceneManager.GetActiveScene().name);
            if (cam != null) sb.Append("  camera=").Append(F(cam.transform.position)).Append(" looking ").Append(F(cam.transform.forward));
            sb.AppendLine();

            worst = Characters(sb, found, out HashSet<Transform> owners);
            Adrift(sb, found, owners);
            HighUp(sb, owners);
            Lines(sb);
            Roots(sb);

            sb.AppendLine().Append("== Flagged: ").Append(found.Count).AppendLine(" (as the smoke test names them)");
            foreach (Finding f in found) sb.Append("    ").AppendLine(f.Key);
            return sb;
        }

        #region Characters
        // Every humanoid (survivor, zombies, people): its body baked as posed this frame. Returns the character with the
        // most body vertices away from it, for the close-up.
        private static Transform Characters(StringBuilder sb, List<Finding> found, out HashSet<Transform> owners)
        {
            owners = new HashSet<Transform>();
            FieldInfo meshField = typeof(HumanoidRig).GetField("_mesh", BindingFlags.Instance | BindingFlags.NonPublic);
            HumanoidRig[] rigs = Object.FindObjectsByType<HumanoidRig>(FindObjectsInactive.Exclude);
            sb.AppendLine().Append("== Characters: ").Append(rigs.Length)
              .AppendLine(" (body vertices baked as posed now; flagged when they hang away from the character, or a part is off the body)");
            var baked = new Mesh();
            var issues = new List<(string key, string line)>();
            int clean = 0, worstCount = 0;
            Transform worst = null;
            foreach (HumanoidRig rig in rigs)
            {
                Transform owner = rig.transform.parent != null ? rig.transform.parent : rig.transform;
                owners.Add(owner);
                var lines = new StringBuilder();
                issues.Clear();
                int away = 0;
                try { away = CheckRig(rig, owner, meshField, baked, lines, issues); }
                catch (Exception e) { Issue(lines, issues, "check failed", "check failed: " + e.GetType().Name + ": " + e.Message); }
                if (lines.Length == 0) { clean++; continue; }

                SkinnedMeshRenderer body = rig.Body;
                string who = PathOf(owner), seen = body != null && body.forceRenderingOff ? "hidden: out of sight" : "drawn";
                sb.Append("  ").Append(who).Append(" at ").Append(F(owner.position)).Append("  (").Append(seen).Append(")")
                  .Append("  shape ").Append(rig.Look != null ? Describe(rig.Look.Shape) : "?").AppendLine();
                sb.Append(lines);
                foreach ((string key, string line) in issues)
                    found.Add(new Finding("character " + who + ": " + key, who + " at " + F(owner.position) + " (" + seen + "): " + line, owner.gameObject));
                if (away > worstCount) { worstCount = away; worst = owner; }
            }
            Object.DestroyImmediate(baked);
            sb.Append("  ").Append(clean).AppendLine(" clean");
            return worst;
        }

        // What's wrong with one character: the report's lines into o, and each problem into issues, keyed by the bone,
        // colour slot or part it's about. Returns how much of it is away from the character.
        private static int CheckRig(HumanoidRig rig, Transform owner, FieldInfo meshField, Mesh baked, StringBuilder o, List<(string key, string line)> issues)
        {
            ZombieAI ai = owner.GetComponent<ZombieAI>();
            HumanoidAnimator anim = rig.GetComponent<HumanoidAnimator>();
            // The dead, crawlers and sleepers lie along the ground: their bodies reach out along it, not up.
            bool lying = ai != null && (ai.IsDeadState || ai.Type == ZombieType.Crawler) || anim != null && (anim.IsDead || anim.IsLying)
                || owner.TryGetComponent(out IDamageable life) && life.IsDead;
            float size = Mathf.Max(1f, rig.transform.lossyScale.y);
            float reach = (lying ? 2.6f : 1.4f) * size, top = 2.3f * size;   // as high for the lying: the dying take a moment to go down
            Vector3 root = owner.position;

            // A bone that has wandered off (or gone non-finite) takes everything skinned to it along.
            if (rig.Bones != null)
                foreach (Transform b in rig.Bones)
                    if (b != null && Away(b.position - root, reach, top))
                        Issue(o, issues, "bone " + b.name, "bone " + b.name + " at " + F(b.position) + " scale " + F(b.lossyScale));

            int total = 0;
            Vector3[] posed = null;
            SkinnedMeshRenderer body = rig.Body;
            Mesh shared = body != null ? body.sharedMesh : null;
            if (body == null || shared == null) Issue(o, issues, "no body mesh", "no body mesh (only the separate parts are drawn)");
            else
            {
                int bindposes = -1;
                try { bindposes = shared.bindposes.Length; } catch (Exception) { }
                if (bindposes >= 0 && bindposes != body.bones.Length)
                    Issue(o, issues, "bind poses", "mesh has " + bindposes + " bind poses for " + body.bones.Length + " bones");

                body.BakeMesh(baked, true);
                Vector3[] v = baked.vertices;
                posed = new Vector3[v.Length];
                int[] sub = SubmeshOf(shared);
                BoneWeight[] w = shared.isReadable ? shared.boneWeights : null;
                HumanoidSlot[] slots = meshField != null && meshField.GetValue(rig) is HumanoidMesh hm ? hm.Slots : null;
                Transform bt = body.transform;
                Matrix4x4 toWorld = Matrix4x4.TRS(bt.position, bt.rotation, Vector3.one);
                var groups = new SortedDictionary<string, Group>(StringComparer.Ordinal);
                var byBone = new SortedDictionary<string, Group>(StringComparer.Ordinal);   // by slot and first bone, for the issues
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 p = posed[i] = toWorld.MultiplyPoint3x4(v[i]);
                    if (!Away(p - root, reach, top)) continue;
                    int s = sub != null && i < sub.Length ? sub[i] : -1;
                    string slot = slots != null && s >= 0 && s < slots.Length ? slots[s].ToString() : "submesh " + s;
                    string bone = "?", first = "?";
                    if (w != null && i < w.Length)
                    {
                        int bi = w[i].boneIndex0;
                        bool known = bi >= 0 && bi < body.bones.Length && body.bones[bi] != null;
                        first = known ? body.bones[bi].name : "bone index " + bi;
                        bone = known
                            ? first + " " + w[i].weight0.ToString("0.00") + (w[i].weight1 > 0f && w[i].boneIndex1 < body.bones.Length && body.bones[w[i].boneIndex1] != null ? " / " + body.bones[w[i].boneIndex1].name + " " + w[i].weight1.ToString("0.00") : "")
                            : first;
                    }
                    float d = (p - root).magnitude;
                    Tally(groups, slot + " on " + bone, p, d);
                    Tally(byBone, slot + " on " + first, p, d);
                    total++;
                }
                if (total > 0)
                {
                    o.Append("    ").Append(total).Append(" of ").Append(v.Length).AppendLine(" body vertices hang away from the character:");
                    foreach (KeyValuePair<string, Group> kv in groups)
                        o.Append("      ").Append(kv.Value.Count).Append("  ").Append(kv.Key).Append("   farthest ")
                         .Append(F(kv.Value.Sample)).Append(" (").Append(kv.Value.Far.ToString("0.0")).AppendLine(" m from the feet)");
                    foreach (KeyValuePair<string, Group> kv in byBone)
                        issues.Add(("body " + kv.Key, kv.Value.Count + " body vertices of " + kv.Key + " hang away from the character, the farthest at "
                            + F(kv.Value.Sample) + " (" + kv.Value.Far.ToString("0.0") + " m from the feet)"));
                }
            }

            // Anything else drawn under the character (a held item, a hat, gear) goes with the body: near the character, and
            // touching the body as posed now, itself or through the parts it touches (a rivet on a lid on a bag on the
            // back). A detail left at a fixed spot on the root floats off the body as soon as the body moves.
            var parts = new List<Renderer>();
            var lo = new List<Vector3>();
            var hi = new List<Vector3>();
            foreach (Renderer r in owner.GetComponentsInChildren<Renderer>())
            {
                if (r == body || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || !r.enabled) continue;
                Bounds b = r.bounds;
                parts.Add(r);
                lo.Add(b.min);
                hi.Add(b.max);
            }
            var sets = new Sets(parts.Count);
            for (int i = 0; i < parts.Count; i++)
                if (posed == null || Near(posed, new Bounds((lo[i] + hi[i]) * 0.5f, hi[i] - lo[i]), PartGap, out _)) sets.Ground(i);
            for (int i = 0; i < parts.Count; i++)
                for (int j = i + 1; j < parts.Count; j++)
                    sets.Meet(i, j, lo, hi);
            var off = new SortedDictionary<int, List<int>>();
            for (int i = 0; i < parts.Count; i++)
            {
                var b = new Bounds((lo[i] + hi[i]) * 0.5f, hi[i] - lo[i]);
                string why = Away(b.center - root, reach, top) ? "away from the character" : b.size.magnitude > 6f ? "too big for it" : null;
                if (why != null) Part(o, issues, owner, parts[i], b, why);
                else if (!sets.Grounded(i))
                {
                    if (!off.TryGetValue(sets.Find(i), out List<int> group)) off[sets.Find(i)] = group = new List<int>();
                    group.Add(i);
                }
                else continue;
                total++;
            }
            foreach (List<int> group in off.Values)
            {
                int main = Biggest(group, parts, lo, hi);
                var b = new Bounds((lo[main] + hi[main]) * 0.5f, hi[main] - lo[main]);
                Near(posed, b, PartGap, out float gap);
                Part(o, issues, owner, parts[main], b, "off the body, " + gap.ToString("0.00") + " m from it"
                    + (group.Count > 1 ? ", with " + (group.Count - 1) + " more part(s) touching it" : ""));
            }
            return total;
        }

        private const float PartGap = 0.05f;

        private static void Part(StringBuilder o, List<(string key, string line)> issues, Transform owner, Renderer r, Bounds b, string why)
        {
            string type = r.GetType().Name;
            Issue(o, issues, "part " + RelativePath(r.transform, owner) + " (" + type + ")",
                "part " + PathOf(r.transform) + " (" + type + (r.forceRenderingOff ? ", hidden" : ", drawn") + ") centre " + F(b.center)
                + " size " + F(b.size) + ": " + why);
        }

        private static void Issue(StringBuilder o, List<(string key, string line)> issues, string key, string line)
        {
            o.Append("    ").AppendLine(line);
            issues.Add((key, line));
        }

        private static void Tally(SortedDictionary<string, Group> groups, string key, Vector3 p, float d)
        {
            groups.TryGetValue(key, out Group g);
            g.Count++;
            if (!(d <= g.Far)) { g.Far = d; g.Sample = p; }   // NaN counts as farthest
            groups[key] = g;
        }

        // Whether any of the points lies within gap of the box; if none does, how far the nearest is.
        private static bool Near(Vector3[] points, Bounds box, float gap, out float nearest)
        {
            Vector3 c = box.center, e = box.extents;
            float best = float.MaxValue;
            foreach (Vector3 p in points)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(p.x - c.x) - e.x), dy = Mathf.Max(0f, Mathf.Abs(p.y - c.y) - e.y), dz = Mathf.Max(0f, Mathf.Abs(p.z - c.z) - e.z);
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 < best) best = d2;
                if (best <= gap * gap) break;
            }
            nearest = Mathf.Sqrt(best);
            return best <= gap * gap;
        }

        // Which submesh (and so which colour slot) each vertex is drawn in.
        private static int[] SubmeshOf(Mesh m)
        {
            if (!m.isReadable) return null;
            var of = new int[m.vertexCount];
            for (int i = 0; i < of.Length; i++) of[i] = -1;
            for (int s = 0; s < m.subMeshCount; s++)
                foreach (int index in m.GetIndices(s))
                    if (index >= 0 && index < of.Length && of[index] < 0) of[index] = s;
            return of;
        }

        private static string Describe(HumanoidShape s)
            => s.Build + " hair " + s.Hair + " head " + s.Headwear + " top " + s.Top + " bottom " + s.Bottom + " feet " + s.Feet
               + " hands " + s.Hands + (s.Zombie ? " zombie" : "") + " gore " + s.Gore + " torn " + s.TornSleeves;
        #endregion

        #region The world
        // Anything drawn wholly above the rooftops that isn't part of a character, grouped by name.
        private static void HighUp(StringBuilder sb, HashSet<Transform> owners)
        {
            var groups = new SortedDictionary<string, (int n, Renderer first)>(StringComparer.Ordinal);
            var particles = new StringBuilder();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!r.enabled || r.forceRenderingOff || UnderAny(r.transform, owners)) continue;
                Bounds b = r.bounds;
                if (r is ParticleSystemRenderer)
                {
                    var ps = r.GetComponent<ParticleSystem>();
                    if (ps != null && ps.particleCount > 0 && b.max.y > RoofTop)
                        particles.Append("    ").Append(PathOf(r.transform)).Append(": ").Append(ps.particleCount).Append(" particles, bounds ")
                                 .Append(F(b.min)).Append("..").Append(F(b.max)).Append(", material ")
                                 .AppendLine(r.sharedMaterial != null ? r.sharedMaterial.name : "none");
                    continue;
                }
                if (b.min.y <= RoofTop) continue;
                string key = PathOf(r.transform) + " (" + r.GetType().Name + ")";
                if (groups.TryGetValue(key, out var g)) groups[key] = (g.n + 1, g.first);
                else groups[key] = (1, r);
            }
            sb.AppendLine().Append("== Drawn wholly above ").Append(RoofTop.ToString("0.0")).Append(" m (not part of a character): ").Append(groups.Count)
              .AppendLine(" (listed, not flagged: the town and the hub build above it; what floats up there touches nothing, above)");
            int listed = 0;
            foreach (KeyValuePair<string, (int n, Renderer first)> kv in groups)
            {
                if (listed++ >= MaxListed) { sb.AppendLine("    ..."); break; }
                Bounds b = kv.Value.first.bounds;
                sb.Append("    ").Append(kv.Value.n > 1 ? kv.Value.n + "x " : "").Append(kv.Key).Append(" centre ").Append(F(b.center))
                  .Append(" size ").Append(F(b.size)).Append(" material ")
                  .AppendLine(kv.Value.first.sharedMaterial != null ? kv.Value.first.sharedMaterial.name : "none");
            }
            if (particles.Length > 0) sb.AppendLine("  particle systems reaching above it:").Append(particles);
        }

        private static void Lines(StringBuilder sb)
        {
            var lines = new StringBuilder();
            foreach (LineRenderer l in Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude))
                if (l.enabled && l.positionCount > 0)
                    lines.Append("    ").Append(PathOf(l.transform)).Append(": ").Append(l.positionCount).Append(" points ")
                         .Append(F(l.GetPosition(0))).Append(" .. ").AppendLine(F(l.GetPosition(l.positionCount - 1)));
            foreach (TrailRenderer t in Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Exclude))
                if (t.enabled && t.positionCount > 0)
                    lines.Append("    trail ").Append(PathOf(t.transform)).Append(": ").Append(t.positionCount).AppendLine(" points");
            sb.AppendLine().AppendLine("== Line and trail renderers");
            sb.Append(lines.Length > 0 ? lines.ToString() : "    none\n");
        }

        // Every scene's root objects (the DontDestroyOnLoad ones included): leftovers show up here.
        private static void Roots(StringBuilder sb)
        {
            var roots = new List<Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.parent == null && (t.hideFlags & HideFlags.HideInHierarchy) == 0) roots.Add(t);
            roots.Sort((a, b) => string.CompareOrdinal(a.gameObject.scene.name + "/" + a.name, b.gameObject.scene.name + "/" + b.name));
            sb.AppendLine().Append("== Root objects: ").Append(roots.Count).AppendLine();
            int listed = 0;
            foreach (Transform r in roots)
            {
                if (listed++ >= MaxListed * 2) { sb.AppendLine("    ..."); break; }
                sb.Append("    [").Append(r.gameObject.scene.name).Append("] ").Append(r.name).Append(r.gameObject.activeSelf ? "" : " (inactive)")
                  .Append(" at ").Append(F(r.position)).Append(", ").Append(r.childCount).AppendLine(" children");
            }
        }
        #endregion

        #region Pictures
        // Close-ups of up to three zombies (the worst flagged one first, then the nearest drawn ones), each from the
        // camera's side and from the zombie's own side, close enough to see a small thing hanging off the body.
        private static void CloseUps(string dir, Transform worst, Camera main, StringBuilder sb)
        {
            foreach (string old in Directory.GetFiles(dir, "FloatingGeometry_closeup_*.png")) File.Delete(old);
            var picks = new List<Transform>();
            if (worst != null) picks.Add(worst);
            Vector3 eye = main != null ? main.transform.position : Vector3.zero;
            var near = new List<Transform>();
            foreach (ZombieAvatar z in Object.FindObjectsByType<ZombieAvatar>(FindObjectsInactive.Exclude))
                if (z.Rig != null && z.Rig.Body != null && !z.Rig.Body.forceRenderingOff && z.transform != worst) near.Add(z.transform);
            near.Sort((a, b) => (a.position - eye).sqrMagnitude.CompareTo((b.position - eye).sqrMagnitude));
            for (int i = 0; i < near.Count && picks.Count < 3; i++) picks.Add(near[i]);

            sb.AppendLine().AppendLine("== Close-ups");
            for (int i = 0; i < picks.Count; i++)
            {
                Transform who = picks[i];
                Vector3 toward = who.position - eye;
                toward.y = 0f;
                toward = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, toward);
                string a = Path.Combine(dir, "FloatingGeometry_closeup_" + i + "_a.png"), b = Path.Combine(dir, "FloatingGeometry_closeup_" + i + "_b.png");
                CloseUp(who, main, -toward, a);
                CloseUp(who, main, side, b);
                sb.Append("    ").Append(i).Append(": ").Append(PathOf(who)).Append(" at ").Append(F(who.position))
                  .Append(who == worst ? " (most flagged)" : " (nearest drawn)").AppendLine();
            }
            if (picks.Count == 0) sb.AppendLine("    no zombie to photograph");
        }

        // One close-up of a character from 2.2 m on the given side, drawn even if out of the survivor's sight.
        private static void CloseUp(Transform who, Camera main, Vector3 fromSide, string file)
        {
            var go = new GameObject("FloatingGeometryCam") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(1280, 720, 24);
            var forced = new List<Renderer>();
            try
            {
                foreach (Renderer r in who.GetComponentsInChildren<Renderer>())
                    if (r.forceRenderingOff) { r.forceRenderingOff = false; forced.Add(r); }
                Camera c = go.AddComponent<Camera>();
                if (main != null) c.CopyFrom(main);
                c.orthographic = false;
                c.fieldOfView = 50f;
                c.nearClipPlane = 0.05f;
                c.farClipPlane = 120f;
                c.transform.position = who.position + fromSide.normalized * 2.2f + Vector3.up * 1.4f;
                c.transform.LookAt(who.position + Vector3.up * 0.95f);
                c.targetTexture = rt;
                c.Render();
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                c.targetTexture = null;
            }
            catch (Exception e) { Debug.LogWarning("[FloatingGeometry] close-up failed: " + e.Message); }
            finally
            {
                foreach (Renderer r in forced) if (r != null) r.forceRenderingOff = true;
                Object.DestroyImmediate(go);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
        #endregion

        private static bool Away(Vector3 d, float reach, float top)
        {
            if (float.IsNaN(d.x + d.y + d.z) || float.IsInfinity(d.x + d.y + d.z)) return true;
            return d.x * d.x + d.z * d.z > reach * reach || d.y > top || d.y < -0.5f;
        }

        private static bool UnderAny(Transform t, HashSet<Transform> owners)
        {
            for (; t != null; t = t.parent) if (owners.Contains(t)) return true;
            return false;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (t = t.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        // The path below root (root itself left out).
        private static string RelativePath(Transform t, Transform root)
        {
            string path = t.name;
            for (t = t.parent; t != null && t != root; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        private static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        private static string F(Vector3 v) => v.ToString("F2");
    }
}
#endif
