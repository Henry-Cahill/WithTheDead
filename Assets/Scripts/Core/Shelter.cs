using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The interiors of the current world — boxes of space under a roof. The raid map registers each
    /// building's interior as it builds it; the body-temperature model treats a survivor inside one as
    /// sheltered from the wind and snow. Cleared whenever a new world is built.
    /// </summary>
    public static class Shelter
    {
        private static readonly List<Bounds> s_Interiors = new List<Bounds>();

        public static int Count => s_Interiors.Count;

        /// <summary>Registers an interior volume (world space).</summary>
        public static void Register(Bounds interior) => s_Interiors.Add(interior);

        /// <summary>Forgets every interior (called before a world is built).</summary>
        public static void Clear() => s_Interiors.Clear();

        /// <summary>True when the position lies inside a registered interior.</summary>
        public static bool IsSheltered(Vector3 position)
        {
            for (int i = 0; i < s_Interiors.Count; i++)
                if (s_Interiors[i].Contains(position)) return true;
            return false;
        }
    }
}
