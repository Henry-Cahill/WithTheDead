using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The doors hung in a building's outside walls (the interiors' working doors, objects of their own) hide with the
    // facing they're in: a door left standing in a wall cut down to its stub would float. They're found among the live
    // interactables when a building's cut facings change (the interiors furnish and clear buildings as the survivor comes
    // and goes, so they're never kept longer), kept hidden at every scan while their facing is cut (the game's other
    // cutaway may show one again when it lets go of it), and shown again when their facing comes back.
    internal static partial class MapCutaway
    {
        /// <summary>A door's point lies on its wall's middle line: this close to the outline it's in that wall.</summary>
        private const float DoorReach = 0.45f;

        private static readonly List<Renderer> s_DoorParts = new List<Renderer>(16);

        /// <summary>Door renderers hidden with cut walls so far (for the harness).</summary>
        internal static int DoorsHidden;

        // Shows the doors hidden for the facings cut until now, then finds and hides those in the facings in `cut` (bits 0-3).
        private static void Doors(Building b, int cut)
        {
            ShowDoors(b);
            if (cut == 0 || b.Pts == null) return;
            List<Interactable> all = Interactable.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (!(all[i] is HomeDoor door) || door == null) continue;
                Vector3 p = door.InteractPoint;
                var q = new Vector2(p.x, p.z);
                if (!Near(b.Bounds, q, DoorReach) || p.y < b.Base - 1f || p.y > b.Base + b.ClipTop + 1f) continue;
                int edge = NearestEdge(b.Pts, q, out float d);
                if (d > DoorReach || (cut & (1 << b.EdgeGroup[edge])) == 0) continue;
                (b.DoorObjs ??= new List<HomeDoor>(4)).Add(door);
            }
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
                    if (r.shadowCastingMode != ShadowCastingMode.On) continue;   // casts nothing, or someone else hid it
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    (b.Doors ??= new List<Renderer>(4)).Add(r);
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
