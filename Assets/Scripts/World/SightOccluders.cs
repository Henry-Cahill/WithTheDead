using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Keeps the solid world on the Sight Occluder layer (<see cref="CloneZConfig.SightOccluderLayer"/>), the one layer the
    /// survivor's line of sight is cast against (<see cref="ZombieVisibility"/>). Everything solid that stood on Default
    /// moves there (walls, building shells and floors, doors, furniture, cars, props, the ground), so it hides the dead as
    /// it always did while the casts pass over everything else (characters, triggers, ceilings, water). Rays that used to
    /// test Default alone test <see cref="CloneZConfig.WorldMask"/>, so for them nothing changes.
    ///
    /// The bootstrapper claims the whole scene once a mode has started (<see cref="ClaimScene"/>: the world, and the cars and
    /// camps its start hooks set down), and the streamed map claims each piece and interior with
    /// <see cref="Claim(Transform)"/> before it wakes, so the physics scene never sees the change. Whatever solid turns up
    /// later (a barricade, a wall the survivor builds, a streamed window) is caught by <see cref="Sweep"/> round the
    /// survivor. Characters stay where they are (they don't hide the dead), and so does anything on another layer: Ignore
    /// Raycast is see-through (glass doors, the dead).
    /// </summary>
    public static class SightOccluders
    {
        private const int DefaultLayer = 0;

        private static readonly List<Collider> s_Colliders = new List<Collider>(256);
        private static readonly List<GameObject> s_Roots = new List<GameObject>(64);
        private static readonly Collider[] s_Hits = new Collider[256];
        // Colliders a sweep found on Default and left there (characters'), so it doesn't look at them again.
        private static readonly HashSet<Collider> s_Left = new HashSet<Collider>();
        // The last few colliders the sweep moved, oldest overwritten first (what the builders missed, for diagnostics).
        private static readonly Collider[] s_Recent = new Collider[16];
        private static int s_RecentNext;

        /// <summary>Colliders moved by <see cref="Sweep"/> this play session rather than by a builder: what was spawned
        /// after the world was built, or what a builder missed.</summary>
        public static int Swept { get; private set; }

        /// <summary>Adds the last few colliders <see cref="Sweep"/> moved (up to 16) to <paramref name="into"/>.</summary>
        public static void RecentlySwept(List<Collider> into)
        {
            for (int i = 0; i < s_Recent.Length; i++)
            {
                Collider c = s_Recent[(s_RecentNext + i) % s_Recent.Length];
                if (c != null) into.Add(c);
            }
        }

        /// <summary>Claims the solids of everything in the active scene (see <see cref="Claim(Transform)"/>). Returns how many
        /// objects moved.</summary>
        public static int ClaimScene()
        {
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(s_Roots);
            int moved = 0;
            for (int i = 0; i < s_Roots.Count; i++)
                if (s_Roots[i] != null) moved += Claim(s_Roots[i].transform);
            s_Roots.Clear();
            return moved;
        }

        /// <summary>Moves every solid collider under <paramref name="root"/> (inactive ones too) from Default onto the
        /// Sight Occluder layer. Returns how many objects moved.</summary>
        public static int Claim(Transform root)
        {
            if (root == null) return 0;
            bool actors = root.GetComponentInParent<Actor>(true) != null || root.GetComponentInChildren<Actor>(true) != null;
            root.GetComponentsInChildren(true, s_Colliders);
            int moved = 0;
            for (int i = 0; i < s_Colliders.Count; i++)
                if (Claim(s_Colliders[i], actors)) moved++;
            s_Colliders.Clear();
            return moved;
        }

        /// <summary>Moves <paramref name="c"/>'s object onto the Sight Occluder layer when the collider is solid, stands on
        /// Default and isn't part of a character. True when it moved.</summary>
        public static bool Claim(Collider c) => Claim(c, true);

        private static bool Claim(Collider c, bool actors)
        {
            if (c == null || c.isTrigger) return false;
            GameObject go = c.gameObject;
            if (go.layer != DefaultLayer) return false;
            if (c is CharacterController || (actors && c.GetComponentInParent<Actor>(true) != null)) return false;
            go.layer = CloneZConfig.SightOccluderLayer;
            return true;
        }

        /// <summary>
        /// The safety net for what was spawned after the world was built: claims up to <paramref name="max"/> solid
        /// colliders still on Default within <paramref name="radius"/> of <paramref name="centre"/>. Returns how many moved;
        /// once the world round the survivor is claimed, it finds only characters.
        /// </summary>
        public static int Sweep(Vector3 centre, float radius, int max)
        {
            int n = Physics.OverlapSphereNonAlloc(centre, radius, s_Hits, 1 << DefaultLayer, QueryTriggerInteraction.Ignore);
            int moved = 0;
            for (int i = 0; i < n; i++)
            {
                Collider c = s_Hits[i];
                s_Hits[i] = null;
                if (moved >= max || c == null || s_Left.Contains(c)) continue;
                if (Claim(c, true))
                {
                    moved++;
                    s_Recent[s_RecentNext] = c;
                    s_RecentNext = (s_RecentNext + 1) % s_Recent.Length;
                }
                else s_Left.Add(c);
            }
            Swept += moved;
            return moved;
        }

        // Enter Play Mode keeps statics from one play session to the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetLastPlaySession()
        {
            s_Left.Clear();
            System.Array.Clear(s_Recent, 0, s_Recent.Length);
            s_RecentNext = 0;
            Swept = 0;
        }
    }
}
