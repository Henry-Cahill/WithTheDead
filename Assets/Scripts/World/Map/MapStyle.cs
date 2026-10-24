using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What the real-world map's passes share about looks. Every random choice about a feature comes from its id and a
    /// salt of the caller's own (<see cref="Hash"/>, <see cref="Unit"/>, <see cref="Pick"/>, <see cref="Rng"/>), never
    /// from Unity's random, the clock or the order things are built in, so a feature comes out the same on every machine,
    /// in every rebuild and in whichever chunk builds it. Also the heights the flat ground layers are drawn at, and the
    /// few colours more than one pass draws with.
    /// </summary>
    internal static class MapStyle
    {
        /// <summary>Frozen water (ponds, lakes, rivers, streams): the lowest of the flat layers over the snow.</summary>
        public const float IceY = 0.006f;
        /// <summary>Plowed parking lots and the railways' gravel beds.</summary>
        public const float LotY = 0.008f;
        /// <summary>The streets' surface; their markings, kerbs and sidewalks go on top of it.</summary>
        public const float StreetY = 0.012f;

        /// <summary>Glazed ice: frozen water, the icy crust on trodden paths and kerb tops, the snow banked round a lot.</summary>
        public static Surf Ice => DecorPalette.Gloss(new Color(0.60f, 0.68f, 0.76f));

        /// <summary>A well-mixed hash of a feature's id and a salt; different salts give independent choices about the
        /// same feature.</summary>
        public static uint Hash(long id, int salt)
        {
            unchecked
            {
                ulong z = (ulong)id * 0x9E3779B97F4A7C15UL + (ulong)(uint)salt * 0xC2B2AE3D27D4EB4FUL + 0x165667B19E3779F9UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (uint)(z ^ (z >> 32));
            }
        }

        /// <summary>A number in [0, 1) from a feature's id and a salt.</summary>
        public static float Unit(long id, int salt) => (Hash(id, salt) >> 8) * (1f / 16777216f);

        /// <summary>An index in [0, <paramref name="count"/>) from a feature's id and a salt.</summary>
        public static int Pick(long id, int salt, int count) => count > 0 ? (int)(Hash(id, salt) % (uint)count) : 0;

        /// <summary>A random stream of a feature's own, for a run of choices about it.</summary>
        public static System.Random Rng(long id, int salt) => new System.Random(unchecked((int)Hash(id, salt)));

        /// <summary>A stand-in id for something mapped without one, from where it stands (to the decimetre).</summary>
        public static long Key(Vector2 p) => ((long)Mathf.RoundToInt(p.x * 10f) << 32) | (uint)Mathf.RoundToInt(p.y * 10f);

        /// <summary>A number in [<paramref name="min"/>, <paramref name="max"/>) from <paramref name="rng"/>.</summary>
        public static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
