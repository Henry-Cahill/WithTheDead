using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CloneZ
{
    // The floating-geometry report (FloatingGeometryReport, F10 in Debug Mode) on the lobby and wave runs' worlds as played,
    // twice each: the lobby's town once the dead roam it and the hub on its own; the waves after two are cut down and after
    // Continue. The run fails on anything it flags that isn't one of the known false positives below: a character's body,
    // bones or parts off where they belong; a piece of the world that, with everything it touches, touches nothing
    // reaching the ground. The report's text goes to Logs (or the -clonez-smoke-shots folder, with a picture of each kind
    // of thing flagged) as FloatingGeometry-<mode>-<where>.txt. With -clonez-smoke-floating-prop, a test prop hangs in the
    // air a few metres from the survivor while the report runs, and the check must fail.
    public sealed partial class SmokeTest
    {
        // Known false positives: a pattern for the report's name for the thing (each report lists its findings' names at
        // the end; * stands for any run of characters), and why it isn't floating.
        private static readonly (string pattern, string why)[] KnownFloating =
        {
        };

        private void CheckFloating(string where)
        {
            string name = "no floating geometry (" + where + ")";
#if UNITY_EDITOR
            GameObject prop = null;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clonez-smoke-floating-prop") >= 0)
            {
                prop = PlantFloatingProp();
                Check("floating test prop hung in the air (" + where + ")", prop != null, prop != null ? "at " + prop.transform.position : "no clear spot near the survivor");
            }
            string file = Path.Combine(s_ShotDir ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs"),
                "FloatingGeometry-" + s_Mode + "-" + Slug(where) + ".txt");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var fresh = new List<FloatingGeometryReport.Finding>();
            int known = 0;
            try
            {
                foreach (FloatingGeometryReport.Finding f in FloatingGeometryReport.Check(file))
                    if (IsKnownFloating(f.Key)) known++;
                    else fresh.Add(f);
                clock.Stop();
                // A picture of each thing flagged (each character, one porch lamp of a street of them), up to six.
                var kinds = new HashSet<string>();
                foreach (FloatingGeometryReport.Finding f in fresh)
                {
                    int colon = f.Key.IndexOf(": ", StringComparison.Ordinal);
                    string kind = f.Key.StartsWith("character ", StringComparison.Ordinal) && colon > 0 ? f.Key.Substring(0, colon) : Regex.Replace(f.Key, "[0-9]+", "#");
                    if (kinds.Count < 6 && kinds.Add(kind)) Picture("floating-" + Slug(where) + "-" + kinds.Count, f.Target);
                }
            }
            catch (Exception e)
            {
                Check(name, false, "the report failed: " + e.GetType().Name + ": " + e.Message);
                return;
            }
            finally { if (prop != null) Destroy(prop); }

            for (int i = 0; i < fresh.Count && i < 40; i++) Debug.Log("[Smoke] floating geometry (" + where + "): " + fresh[i].Detail);
            Check(name, fresh.Count == 0, (fresh.Count == 0 ? "nothing new" : fresh.Count + " new, the first " + fresh[0].Key)
                + (known > 0 ? ", " + known + " known false positive(s)" : "") + ", " + clock.ElapsedMilliseconds + " ms; report " + file);
#else
            Skip(name, "the floating-geometry report runs in the editor only");
#endif
        }

        private static string Slug(string s) => Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

        // A picture of something flagged (with -clonez-smoke-shots), from close by, on the first side with a clear view of it.
        private static void Picture(string name, UnityEngine.Object target)
        {
            if (string.IsNullOrEmpty(s_ShotDir) || !(target is GameObject go) || go == null) return;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            float away = Mathf.Clamp(b.size.magnitude * 3f, 0.8f, 25f);
            Vector3 eye = b.center + new Vector3(0f, 0.5f, -1f).normalized * away;
            for (int k = 0; k < 16; k++)
            {
                Vector3 dir = Quaternion.Euler(k < 8 ? 30f : 5f, k * 45f, 0f) * Vector3.back;
                if (Physics.Linecast(b.center + dir * (b.extents.magnitude + 0.02f), b.center + dir * away, ~0, QueryTriggerInteraction.Ignore)) continue;
                eye = b.center + dir * away;
                break;
            }
            var shot = new GameObject("[SmokeShot]").AddComponent<Camera>();
            if (Camera.main != null) shot.CopyFrom(Camera.main);
            shot.orthographic = false;
            shot.fieldOfView = 40f;
            shot.nearClipPlane = 0.03f;
            shot.farClipPlane = 300f;
            shot.cullingMask = ~0;
            shot.transform.position = eye;
            shot.transform.LookAt(b.center);
            Capture(name, shot, false);
            Destroy(shot.gameObject);
        }

        private static bool IsKnownFloating(string key)
        {
            foreach (var known in KnownFloating)
                if (Regex.IsMatch(key, "^" + Regex.Escape(known.pattern).Replace("\\*", ".*") + "$")) return true;
            return false;
        }

#if UNITY_EDITOR
        // The test prop -clonez-smoke-floating-prop hangs: a crate-sized box, collider and all, a metre clear of whatever
        // is under it, a few metres from the survivor, with nothing solid (a collider or anything drawn) within half a metre.
        private static GameObject PlantFloatingProp()
        {
            Vector3 me = Actor.LocalPlayer != null ? Actor.LocalPlayer.transform.position : Vector3.zero;
            var size = new Vector3(0.6f, 0.5f, 0.4f);
            Physics.SyncTransforms();
            for (int ring = 2; ring <= 8; ring += 2)
                for (int step = 0; step < 12; step++)
                {
                    Vector3 at = me + Quaternion.Euler(0f, step * 30f, 0f) * Vector3.forward * ring;
                    if (!Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out RaycastHit under, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    Vector3 centre = under.point + Vector3.up * (1f + size.y * 0.5f);
                    if (!FloatingGeometryReport.IsClear(new Bounds(centre, size + Vector3.one))) continue;
                    GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    prop.name = "SmokeFloatingProp";
                    prop.transform.position = centre;
                    prop.transform.localScale = size;
                    Physics.SyncTransforms();
                    Debug.Log("[Smoke] floating test prop at " + centre + ", 1 m over " + under.collider.name);
                    return prop;
                }
            return null;
        }
#endif
    }
}
