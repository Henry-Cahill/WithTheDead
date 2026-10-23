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
        // Random colour palette for street lamps – hand‑built look
        private static readonly Color[] s_colors = new[]
        {
            new Color(1f, 0.85f, 0.65f), // warm amber
            new Color(1f, 0.95f, 0.80f), // pale yellow
            new Color(1f, 1f, 0.9f) // soft white
        };

        /// <summary>
        /// Returns a random colour from the palette.
        /// </summary>
        public static Color GetRandomColor()
        {
            // UnityEngine.Random is safe here – called only during lamp init.
            return s_colors[UnityEngine.Random.Range(0, s_colors.Length)];
        }

        /// <summary>
        /// Apply a random colour/style to the given lamp instance.
        /// Sets the Light colour and the emissive tint of its lamp‑glass material.
        /// </summary>
        public static void ApplyRandomStyle(StreetLampInteraction lamp)
        {
            if (lamp == null) return;
            var rndCol = GetRandomColor();
            // Set the Light component colour (if present)
            var light = lamp.GetComponentInChildren<Light>();
            if (light != null) light.color = rndCol;
            // Set emissive colour on the glass renderer (if any)
            var rend = lamp.GetComponentInChildren<Renderer>();
            if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_EmissionColor"))
            {
                rend.sharedMaterial.SetColor("_EmissionColor", rndCol * 0.8f);
                rend.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// Bulk‑toggle all lamps within radius of centre.
        /// </summary>
        public static void ToggleAllNearby(Vector3 center, float radius, bool on)
        {
            foreach (var lamp in GetNearby(center, radius))
            {
                lamp.ApplyState(on);
            }
        }

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
