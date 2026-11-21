using UnityEngine;
using CloneZ;

namespace CloneZ
{
    /// <summary>
    /// Helper for placing a small hanging icicle.
    /// </summary>
    internal static class IceHelper
    {
        /// <summary>
        /// Adds a simple icicle geometry at the given position.
        /// </summary>
        public static void PlaceIcicle(OutdoorBatches parent, Vector3 pos)
        {
            // Ice material – a subtle glossy blue similar to other ice features.
            var iceMat = DecorPalette.Gloss(new Color(0.60f, 0.68f, 0.76f));
            var icicle = parent.Solid(pos);
            // Thin vertical box representing an icicle.
            icicle.Box(pos, new Vector3(0.02f, 0.08f, 0.02f), 0f, iceMat);
        }
    }
}
