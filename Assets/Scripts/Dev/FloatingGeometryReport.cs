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
    /// (<c>Logs/FloatingGeometry.png</c>) and close-ups of a few zombies (<c>Logs/FloatingGeometry_closeup_*.png</c>):
    /// vertices of a character's skinned body hanging away from the character as posed this frame (baked, because the
    /// renderer's bounds never show them), separate parts under a character drawn away from it, bones that have
    /// wandered off, anything drawn wholly above the rooftops, line renderers, and every scene's root objects. Run it
    /// from <c>CloneZ &gt; Debug &gt; Report Floating Geometry</c> or F10 in <see cref="DebugMode"/>.
    /// </summary>
    public static class FloatingGeometryReport
    {
        private const float RoofTop = 3.0f;   // the wave world draws nothing wholly above this but the wall caps
        private const int MaxListed = 80;

        private struct Group
        {
            public int Count;
            public float Far;
            public Vector3 Sample;
        }

        public static void Run()
        {
            var sb = new StringBuilder(16384);
            var flagged = new List<Object>();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(dir);
            string txt = Path.Combine(dir, "FloatingGeometry.txt");
            string png = Path.Combine(dir, "FloatingGeometry.png");

            Camera cam = Camera.main;
            sb.Append("Floating geometry report, ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
              .Append("  mode=").Append(GameSession.Mode).Append("  scene=").Append(SceneManager.GetActiveScene().name);
            if (cam != null) sb.Append("  camera=").Append(F(cam.transform.position)).Append(" looking ").Append(F(cam.transform.forward));
            sb.AppendLine();

            Transform worst = Characters(sb, flagged, out HashSet<Transform> owners);
            HighUp(sb, flagged, owners);
            Lines(sb);
            Roots(sb);

            File.WriteAllText(txt, sb.ToString());
            ScreenCapture.CaptureScreenshot(png);
            CloseUps(dir, worst, cam, sb);
            File.WriteAllText(txt, sb.ToString());
            if (flagged.Count > 0 && !Application.isBatchMode)
            {
                Selection.objects = flagged.ToArray();
                EditorGUIUtility.PingObject(flagged[0]);
            }
            Debug.Log("[FloatingGeometry] " + flagged.Count + " flagged. Report: " + txt + "  Game view: " + png);
        }

        #region Characters
        // Every humanoid (survivor, zombies, people): its body baked as posed this frame. Returns the character with the
        // most body vertices away from it, for the close-up.
        private static Transform Characters(StringBuilder sb, List<Object> flagged, out HashSet<Transform> owners)
        {
            owners = new HashSet<Transform>();
            FieldInfo meshField = typeof(HumanoidRig).GetField("_mesh", BindingFlags.Instance | BindingFlags.NonPublic);
            HumanoidRig[] rigs = Object.FindObjectsByType<HumanoidRig>(FindObjectsInactive.Exclude);
            sb.AppendLine().Append("== Characters: ").Append(rigs.Length)
              .AppendLine(" (body vertices baked as posed now; flagged when they hang away from the character)");
            var baked = new Mesh();
            int clean = 0, worstCount = 0;
            Transform worst = null;
            foreach (HumanoidRig rig in rigs)
            {
                Transform owner = rig.transform.parent != null ? rig.transform.parent : rig.transform;
                owners.Add(owner);
                var lines = new StringBuilder();
                int away = 0;
                try { away = CheckRig(rig, owner, meshField, baked, lines); }
                catch (Exception e) { lines.Append("    check failed: ").Append(e.GetType().Name).Append(": ").AppendLine(e.Message); }
                if (lines.Length == 0) { clean++; continue; }

                SkinnedMeshRenderer body = rig.Body;
                sb.Append("  ").Append(PathOf(owner)).Append(" at ").Append(F(owner.position))
                  .Append(body != null && body.forceRenderingOff ? "  (hidden: out of sight)" : "  (drawn)")
                  .Append("  shape ").Append(rig.Look != null ? Describe(rig.Look.Shape) : "?").AppendLine();
                sb.Append(lines);
                flagged.Add(owner.gameObject);
                if (away > worstCount) { worstCount = away; worst = owner; }
            }
            Object.DestroyImmediate(baked);
            sb.Append("  ").Append(clean).AppendLine(" clean");
            return worst;
        }

        private static int CheckRig(HumanoidRig rig, Transform owner, FieldInfo meshField, Mesh baked, StringBuilder o)
        {
            ZombieAI ai = owner.GetComponent<ZombieAI>();
            bool lying = ai != null && (ai.IsDeadState || ai.Type == ZombieType.Crawler);
            float size = Mathf.Max(1f, rig.transform.lossyScale.y);
            float reach = (lying ? 2.6f : 1.4f) * size, top = (lying ? 1.3f : 2.3f) * size;
            Vector3 root = owner.position;

            // A bone that has wandered off (or gone non-finite) takes everything skinned to it along.
            if (rig.Bones != null)
                foreach (Transform b in rig.Bones)
                    if (b != null && Away(b.position - root, reach, top))
                        o.Append("    bone ").Append(b.name).Append(" at ").Append(F(b.position)).Append(" scale ").AppendLine(F(b.lossyScale));

            int total = 0;
            SkinnedMeshRenderer body = rig.Body;
            Mesh shared = body != null ? body.sharedMesh : null;
            if (body == null || shared == null) o.AppendLine("    no body mesh (only the separate parts are drawn)");
            else
            {
                int bindposes = -1;
                try { bindposes = shared.bindposes.Length; } catch (Exception) { }
                if (bindposes >= 0 && bindposes != body.bones.Length)
                    o.Append("    mesh has ").Append(bindposes).Append(" bind poses for ").Append(body.bones.Length).AppendLine(" bones");

                body.BakeMesh(baked, true);
                Vector3[] v = baked.vertices;
                int[] sub = SubmeshOf(shared);
                BoneWeight[] w = shared.isReadable ? shared.boneWeights : null;
                HumanoidSlot[] slots = meshField != null && meshField.GetValue(rig) is HumanoidMesh hm ? hm.Slots : null;
                Transform bt = body.transform;
                Matrix4x4 toWorld = Matrix4x4.TRS(bt.position, bt.rotation, Vector3.one);
                var groups = new SortedDictionary<string, Group>(StringComparer.Ordinal);
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(v[i]);
                    if (!Away(p - root, reach, top)) continue;
                    int s = sub != null && i < sub.Length ? sub[i] : -1;
                    string slot = slots != null && s >= 0 && s < slots.Length ? slots[s].ToString() : "submesh " + s;
                    string bone = "?";
                    if (w != null && i < w.Length)
                    {
                        int bi = w[i].boneIndex0;
                        bone = bi >= 0 && bi < body.bones.Length && body.bones[bi] != null
                            ? body.bones[bi].name + " " + w[i].weight0.ToString("0.00") + (w[i].weight1 > 0f && w[i].boneIndex1 < body.bones.Length && body.bones[w[i].boneIndex1] != null ? " / " + body.bones[w[i].boneIndex1].name + " " + w[i].weight1.ToString("0.00") : "")
                            : "bone index " + bi;
                    }
                    string key = slot + " on " + bone;
                    groups.TryGetValue(key, out Group g);
                    g.Count++;
                    float d = (p - root).magnitude;
                    if (!(d <= g.Far)) { g.Far = d; g.Sample = p; }   // NaN counts as farthest
                    groups[key] = g;
                    total++;
                }
                if (total > 0)
                {
                    o.Append("    ").Append(total).Append(" of ").Append(v.Length).AppendLine(" body vertices hang away from the character:");
                    foreach (KeyValuePair<string, Group> kv in groups)
                        o.Append("      ").Append(kv.Value.Count).Append("  ").Append(kv.Key).Append("   farthest ")
                         .Append(F(kv.Value.Sample)).Append(" (").Append(kv.Value.Far.ToString("0.0")).AppendLine(" m from the feet)");
                }
            }

            // Anything else drawn under the character (a held item, a hat, gear): is it with the body?
            foreach (Renderer r in owner.GetComponentsInChildren<Renderer>())
            {
                if (r == body || r is ParticleSystemRenderer || !r.enabled) continue;
                Bounds b = r.bounds;
                if (Away(b.center - root, reach, top) || b.size.magnitude > 6f)
                {
                    o.Append("    part ").Append(PathOf(r.transform)).Append(" (").Append(r.GetType().Name).Append(r.forceRenderingOff ? ", hidden" : ", drawn")
                     .Append(") centre ").Append(F(b.center)).Append(" size ").AppendLine(F(b.size));
                    total++;
                }
            }
            return total;
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
        private static void HighUp(StringBuilder sb, List<Object> flagged, HashSet<Transform> owners)
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
            sb.AppendLine().Append("== Drawn wholly above ").Append(RoofTop.ToString("0.0")).Append(" m (not part of a character): ").Append(groups.Count).AppendLine();
            int listed = 0;
            foreach (KeyValuePair<string, (int n, Renderer first)> kv in groups)
            {
                if (listed++ >= MaxListed) { sb.AppendLine("    ..."); break; }
                Bounds b = kv.Value.first.bounds;
                sb.Append("    ").Append(kv.Value.n > 1 ? kv.Value.n + "x " : "").Append(kv.Key).Append(" centre ").Append(F(b.center))
                  .Append(" size ").Append(F(b.size)).Append(" material ")
                  .AppendLine(kv.Value.first.sharedMaterial != null ? kv.Value.first.sharedMaterial.name : "none");
                if (flagged.Count < 200) flagged.Add(kv.Value.first.gameObject);
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

        private static string F(Vector3 v) => v.ToString("F2");
    }
}
#endif
