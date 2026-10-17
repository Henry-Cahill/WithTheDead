using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// One way out of a raid: a named spot on the map edge. The survivor extracts by standing inside
    /// its radius for <see cref="ExtractSeconds"/>. Built by <see cref="RaidMapBuilder"/> and run by
    /// <see cref="ExtractionManager"/>.
    /// </summary>
    public sealed class RaidExit
    {
        public string Name;
        public Vector3 Position;
        public float Radius = 4f;
        /// <summary>Seconds the survivor must hold inside the zone to get out.</summary>
        public float ExtractSeconds = 8f;
        /// <summary>Closed exits are shown but can't be used (e.g. one that opens late in the night).</summary>
        public bool Open = true;
        /// <summary>A short line about the exit for the HUD list, e.g. "Flare at the overpass".</summary>
        public string Hint;
    }

    /// <summary>What <see cref="RaidMapBuilder.Build"/> made: where the survivor starts, the exits and
    /// the size of the playable area.</summary>
    public sealed class RaidMapInfo
    {
        public Vector3 PlayerSpawn;
        public float PlayerSpawnYaw;
        public readonly List<RaidExit> Exits = new List<RaidExit>();
        /// <summary>Playable half-extent of the map (the bootstrapper sets
        /// <see cref="CloneZConfig.ArenaHalfSize"/> from it).</summary>
        public float HalfSize = 70f;
    }
}
