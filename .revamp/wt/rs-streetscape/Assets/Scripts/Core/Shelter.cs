using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The interiors of the current world — boxes of space under a roof. The raid map registers each
    /// building's interior as it builds it; the body-temperature model treats a survivor inside one as
    /// sheltered from the wind and snow. Cleared whenever a new world is built. A streamed map registers a
    /// piece's interiors under that piece (<see cref="Owner"/>) and forgets them when the piece is unloaded.
    /// </summary>
    public static class Shelter
    {
        private static readonly List<Bounds> s_Interiors = new List<Bounds>();
        private static readonly List<object> s_Owners = new List<object>();

        /// <summary>Whose interiors are being registered (a streamed map piece while it's built); null otherwise.</summary>
        public static object Owner;

        public static int Count => s_Interiors.Count;

        /// <summary>Registers an interior volume (world space), under the current <see cref="Owner"/>.</summary>
        public static void Register(Bounds interior)
        {
            s_Interiors.Add(interior);
            s_Owners.Add(Owner);
        }

        /// <summary>Forgets the interiors registered under <paramref name="owner"/>.</summary>
        public static void Forget(object owner)
        {
            if (owner == null) return;
            for (int i = s_Interiors.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(s_Owners[i], owner)) continue;
                s_Interiors.RemoveAt(i);
                s_Owners.RemoveAt(i);
            }
        }

        /// <summary>Forgets every interior (called before a world is built).</summary>
        public static void Clear()
        {
            s_Interiors.Clear();
            s_Owners.Clear();
            Owner = null;
        }

        /// <summary>True when the position lies inside a registered interior.</summary>
        public static bool IsSheltered(Vector3 position)
        {
            for (int i = 0; i < s_Interiors.Count; i++)
                if (s_Interiors[i].Contains(position)) return true;
            return false;
        }
    }
}
