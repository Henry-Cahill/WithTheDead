using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Global registry of all StreetLampInteraction instances.
    /// Allows fast lookup of nearby lamps for block‑toggle actions.
    /// </summary>
    internal static class StreetLampRegistry
    {
        private static readonly HashSet<StreetLampInteraction> s_lamps = new();
        public static void Register(StreetLampInteraction lamp) => s_lamps.Add(lamp);
        public static void Unregister(StreetLampInteraction lamp) => s_lamps.Remove(lamp);
        /// <summary>
        /// Returns every registered lamp whose position is within <paramref name="radius"/> of <paramref name="center"/>.
        /// </summary>
        public static IEnumerable<StreetLampInteraction> GetNearby(Vector3 center, float radius)
        {
            float sqr = radius * radius;
            foreach (var lamp in s_lamps)
            {
                if (lamp == null) continue;
                if ((lamp.transform.position - center).sqrMagnitude <= sqr)
                    yield return lamp;
            }
        }
    }
}
