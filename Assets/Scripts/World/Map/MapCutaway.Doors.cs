using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The doors hung in a building's outside walls (the interiors' working doors, objects of their own) hide with the
    // facing they're in: a door left standing in a wall cut down to its stub would float. They're found among the live
    // interactables when a building's cut facings change, and again whenever interactables come or go (the interiors
    // furnish and clear buildings as the survivor comes and goes, so they're never kept longer); kept hidden every frame
    // while their facing is cut (the game's other cutaway may show one again when its ray lets go of it), and shown again
    // when their facing comes back.
    internal static partial class MapCutaway
    {
        /// <summary>A door's point lies on its wall's middle line: this close to the outline it's in that wall.</summary>
        private const float DoorReach = 0.45f;

        private static readonly List<Renderer> s_DoorParts = new List<Renderer>(16);

        // The live interactables last frame: how many, and the newest (they're added at the end).
        private static int s_Interactables = -1;
        private static Interactable s_LastInteractable;
        // after the existing static fields
        private static int s_InteractablesChecksum = 0;

        /// <summary>Door renderers hidden with cut walls so far (for the harness).</summary>
        internal static int DoorsHidden;

        // Whether interactables came or went since the last call (a door furnished in a cut wall, or one cleared away).
        private static bool InteractablesChanged()
        {
            List<Interactable> all = Interactable.All;
            int n = all.Count;
            // quick check – count changed or last element changed
            Interactable last = n > 0 ? all[n - 1] : null;
            bool quickChanged = n != s_Interactables || !ReferenceEquals(last, s_LastInteractable);
            // full checksum – XOR of all instance IDs (fast, catches any add/remove/move)
#pragma warning disable CS0619 // GetInstanceID is obsolete
            int newChecksum = 0;
            for (int i = 0; i < n; i++) newChecksum ^= all[i].GetHashCode();
#pragma warning restore CS0619
            bool changed = quickChanged || newChecksum != s_InteractablesChecksum;
            // store current state for next frame
            s_Interactables = n;
            s_LastInteractable = last;
            s_InteractablesChecksum = newChecksum;
            return changed;
        }

        private static void ForgetInteractables()
        {
            s_Interactables = -1;
            s_LastInteractable = null;
            s_InteractablesChecksum = 0;
        }

        /// <summary>
        /// The doors' frame, after <see cref="Run"/>: hides again whatever door of a cut wall shows (the game's other
        /// cutaway shows a door it had hidden when its ray lets go of it, whatever frame that is), and finds them afresh
        /// when interactables came or went (buildings furnished or cleared).
        /// </summary>
        internal static void KeepDoors()
        {
            bool changed = InteractablesChanged();
            for (int i = 0; i < s_Active.Count; i++)
            {
                Building b = s_Active[i];
                int cut = b.Applied & WallBits;
                if (cut == 0 || b.Walls == null) { if (b.DoorCut != -1) ShowDoors(b); b.DoorCut = -1; continue; }
                if (changed || b.DoorCut != cut) { Doors(b, cut); b.DoorCut = cut; }
                else KeepDoorsHidden(b);
            }
        }

        // Shows the doors hidden for the facings cut until now, then finds and hides those in the facings in `cut` (bits 0-3).
        private static void Doors(Building b, int cut)
        {
            // Ensure any doors previously hidden by cut walls are made visible before re‑evaluating
            ShowDoors(b);
            // If there are no cut walls (cut == 0) or the building lacks edge points, nothing to process
            if (cut == 0 || b.Pts == null) return;
            // Grab the current list of all interactable objects in the scene
            List<Interactable> all = Interactable.All;
            // Scan each interactable to find doors that may intersect a cut wall
            for (int i = 0; i < all.Count; i++)
            {
                // Skip items that aren't HomeDoor instances; also protect against unexpected nulls
                if (!(all[i] is HomeDoor door) || door == null) continue;
                // Position where the player can interact with the door (world coordinates)
                Vector3 p = door.InteractPoint;
                // Project the 3D point onto the horizontal XZ plane for edge distance calculations
                var q = new Vector2(p.x, p.z);
                // Reject doors that lie outside the building's horizontal bounds or vertical limits
                if (!Near(b.Bounds, q, DoorReach) || p.y < b.Base - 1f || p.y > b.Base + b.ClipTop + 1f) continue;
                // Determine the nearest wall edge to the door and how far it is from that edge
                int edge = NearestEdge(b.Pts, q, out float d);
                // Only consider doors close enough to an edge that is currently cut
                if (d > DoorReach || (cut & (1 << b.EdgeGroup[edge])) == 0) continue;
                // Record the door for later hiding; lazily initialise the list on first use
                (b.DoorObjs ??= new List<HomeDoor>(4)).Add(door);
            }
            // After collecting relevant doors, hide those that intersect cut walls
            KeepDoorsHidden(b);
        }

        // Hides whatever of the doors in the cut walls shows now (once at the cut, then at every scan while it lasts).
        private static void KeepDoorsHidden(Building b)
        {
            List<HomeDoor> doors = b.DoorObjs;
            if (doors == null) return;
            for (int i = doors.Count - 1; i >= 0; i--)
            {
                HomeDoor door = doors[i];
                if (door == null)
                {
                    doors.RemoveAt(i);
                    continue;
                }
                door.GetComponentsInChildren(false, s_DoorParts);
                for (int k = 0; k < s_DoorParts.Count; k++)
                {
                    Renderer r = s_DoorParts[k];
                    if (r.shadowCastingMode == ShadowCastingMode.Off) { if (!r.enabled) continue; r.enabled = false; List<Renderer> off = b.DoorNoShadow ??= new List<Renderer>(4); if (!off.Contains(r)) off.Add(r); DoorsHidden++; continue; }
                    if (r.shadowCastingMode != ShadowCastingMode.On) continue;   // casts nothing, or someone else hid it
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    List<Renderer> hidden = b.Doors ??= new List<Renderer>(4);
                    if (!hidden.Contains(r)) hidden.Add(r);   // hidden again after the other cutaway let go of it
                    DoorsHidden++;
                }
            }
            s_DoorParts.Clear();
        }

        // Shows the doors' renderers hidden here (those still hidden: the other cutaway may hold one of them itself now).
        private static void ShowDoors(Building b)
        {
            List<Renderer> hidden = b.Doors;
            if (hidden != null)
            {
                for (int i = 0; i < hidden.Count; i++)
                {
                    Renderer r = hidden[i];
                    if (r != null && r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) r.shadowCastingMode = ShadowCastingMode.On;
                }
                hidden.Clear();
            }
            List<Renderer> off = b.DoorNoShadow;
            if (off != null)
            {
                for (int i = 0; i < off.Count; i++)
                {
                    Renderer r = off[i];
                    if (r != null) r.enabled = true;
                }
                off.Clear();
            }
            b.DoorObjs?.Clear();
        }

        // The edge of p nearest q (edge i runs from p[i] to p[i + 1]) and how far it is.
        private static int NearestEdge(Vector2[] p, Vector2 q, out float distance)
        {
            float best = float.MaxValue;
            int at = 0;
            for (int i = 0; i < p.Length; i++)
            {
                Vector2 a = p[i], ab = p[i + 1 < p.Length ? i + 1 : 0] - a;
                float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                float d = (a + ab * t - q).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                at = i;
            }
            distance = Mathf.Sqrt(best);
            return at;
        }
    }
}
