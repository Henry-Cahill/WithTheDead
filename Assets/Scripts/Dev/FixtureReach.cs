#if UNITY_EDITOR
using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Whether the survivor can use a fixture (an <see cref="Interactable"/>), by <see cref="PlayerInteractor"/>'s own rules:
    /// somewhere within the fixture's <see cref="Interactable.Reach"/> of its handle (flat) and
    /// <see cref="CloneZConfig.FixtureReachVertical"/> of it (up), a body stands clear on a floor (the survivor's capsule,
    /// 0.4 m round and 2 m tall), with nothing solid between that body's chest and the handle but the fixture's own parts.
    /// For the harnesses: the Kentucky Atlas's survey and the Introduction town's.
    /// </summary>
    internal static class FixtureReach
    {
        private const float BodyRadius = 0.4f, BodyHeight = 2f, Chest = 1.2f;
        private static readonly float[] Rings = { 0.35f, 0.5f, 0.8f, 1.1f, 1.4f };
        private static readonly int Solid = ~(1 << CloneZConfig.IgnoreRaycastLayer);
        private static readonly Collider[] s_Body = new Collider[8];

        /// <summary>Null when a survivor standing on the floor at height <paramref name="floor"/> (or a little above it: a porch,
        /// a stair) can use <paramref name="f"/>; else why not. Over a basement (<paramref name="floor"/> below ground) the body
        /// passes through the ground's box, as <see cref="LevelStructure.KeepUp"/> lets it.</summary>
        internal static string Why(Interactable f, float floor)
        {
            Vector3 ip = f.InteractPoint;
            Transform own = f.OcclusionRoot;
            float reach = f.Reach;
            bool basement = floor < -0.3f;
            int crowded = 0, tooHigh = 0, hidden = 0;
            string blocker = null, under = null;
            foreach (float ring in Rings)
            {
                if (ring > reach - 0.05f) break;
                for (int k = 0; k < 16; k++)
                {
                    float a = k * (Mathf.PI / 8f);
                    var above = new Vector3(ip.x + Mathf.Cos(a) * ring, floor + 1.2f, ip.z + Mathf.Sin(a) * ring);
                    bool found = Physics.Raycast(above, Vector3.down, out RaycastHit ground, 1.6f, Solid, QueryTriggerInteraction.Ignore);
                    if (!found || ground.point.y < floor - 0.25f || ground.point.y > floor + 0.6f)
                    {
                        under ??= found ? Path(ground.collider.transform) + " at " + F2(ground.point.y) + " m" : "nothing";
                        continue;
                    }
                    Vector3 feet = ground.point;
                    if (BodyBlocked(feet, basement))
                    {
                        crowded++;
                        continue;
                    }
                    if (Mathf.Abs(ip.y - feet.y) > CloneZConfig.FixtureReachVertical)
                    {
                        tooHigh++;
                        continue;
                    }
                    Vector3 from = feet + Vector3.up * Chest;
                    Vector3 to = ip - (ip - from).normalized * 0.06f;
                    if (InSight(from, to, own, basement)) return null;
                    hidden++;
                    blocker ??= s_Blocker;
                }
            }
            if (hidden > 0) return "behind " + blocker + " from everywhere a body stands within reach";
            if (tooHigh > 0) return "too high or too low to reach (its handle at " + F2(ip.y) + " m)";
            if (crowded > 0) return "no room to stand within reach";
            return "no floor within reach (its handle at " + F2(ip.y) + " m, the floor at " + F2(floor) + " m; under the first try: " + under + ")";
        }

        private static string s_Blocker;

        // PlayerInteractor.InSight's test: nothing solid from the chest to the handle but the fixture's own parts (and, in a
        // basement, the ground's box overhead). s_Blocker: what was met when it's false.
        private static bool InSight(Vector3 from, Vector3 to, Transform own, bool basement)
        {
            s_Blocker = null;
            for (int pass = 0; pass < 3; pass++)
            {
                if (!Physics.Linecast(from, to, out RaycastHit hit, Solid, QueryTriggerInteraction.Ignore)) return true;
                Transform t = hit.collider.transform;
                if (basement && hit.collider is BoxCollider && t.name == "Ground")
                {
                    if ((to - hit.point).sqrMagnitude < 1e-4f) return true;
                    from = hit.point + (to - from).normalized * 0.01f;
                    continue;
                }
                if (own != null && t.IsChildOf(own)) return true;
                s_Blocker = Path(t);
                return false;
            }
            s_Blocker = "the ground's box";
            return false;
        }

        // Something solid where a survivor's body would stand (the capsule a little slimmer, so a wall it would only touch
        // doesn't count); over a basement the body passes through the ground's box.
        private static bool BodyBlocked(Vector3 feet, bool basement)
        {
            int n = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (BodyRadius + 0.05f), feet + Vector3.up * (BodyHeight - BodyRadius),
                BodyRadius - 0.03f, s_Body, Solid, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = s_Body[i];
                if (basement && c is BoxCollider && c.gameObject.name == "Ground") continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Takes every switched-on collider under <paramref name="root"/> out of the physics scene and puts it back. In this
        /// Unity (6000.7 alpha) the scene's queries can lose static colliders, everywhere past some line across the map (rays
        /// and bodies go through the ground, floors and walls there while they read as enabled and in place), and neither a
        /// transform sync nor a simulation step brings them back; taken out and put back in, they're found again (as
        /// KentuckyStreamer.KeepGroundFound does for its snowfield). Call before checking anything by rays. Returns how many.
        /// </summary>
        internal static int Reinsert(Transform root)
        {
            if (root == null) return 0;
            int n = 0;
            foreach (Collider c in root.GetComponentsInChildren<Collider>(false))
            {
                if (!c.enabled) continue;
                c.enabled = false;
                c.enabled = true;
                n++;
            }
            Physics.SyncTransforms();
            return n;
        }

        /// <summary>The fixture's type and what the game calls it.</summary>
        internal static string Name(Interactable f)
        {
            string label;
            try { label = f.DisplayName; }
            catch (Exception) { label = null; }
            return f.GetType().Name + (string.IsNullOrEmpty(label) ? "" : " \"" + label + "\"");
        }

        internal static string Path(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        private static string F2(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
