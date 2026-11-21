using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Holds simple road‑type information for a street‑lamp so other systems can vary its appearance.
    /// </summary>
    internal sealed class StreetLampMetadata : MonoBehaviour
    {
        /// <summary>True if the lamp stands on a primary (wider) road.</summary>
        public bool IsPrimaryRoad;
    }
}
