using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>What an exit demands before it lets you out (Tarkov's conditional extracts turn the map into a puzzle).</summary>
    public enum ExitCondition
    {
        None,
        /// <summary>Pay the smuggler (military-grade rounds from the pack).</summary>
        Pay,
        /// <summary>A culvert too narrow for a backpack.</summary>
        NoBackpack,
        /// <summary>Opens only after a power switch somewhere on the map is thrown.</summary>
        Switch,
        /// <summary>Only with a scavenger at your side (a scav run, or a friendly scav nearby).</summary>
        CoopScav,
        /// <summary>A landing zone: fire a flare, the helicopter comes, survive until it lands (Beautiful Light / GZW).</summary>
        Flare,
    }

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

        public ExitCondition Condition;
        /// <summary>The fare for a <see cref="ExitCondition.Pay"/> exit.</summary>
        public int Cost;
        /// <summary>Set once the switch for a <see cref="ExitCondition.Switch"/> exit has been thrown.</summary>
        public bool Powered;
        /// <summary>For a flare exit: the helicopter is on its way / has landed.</summary>
        public bool HeliCalled, HeliLanded;
        public float HeliEta;
        /// <summary>The map side (0 N, 1 E, 2 S, 3 W) this exit sits on.</summary>
        public int Side;

        /// <summary>A one-line requirement for the HUD ("Pay 400 MG", "No backpack").</summary>
        public string Requirement
        {
            get
            {
                switch (Condition)
                {
                    case ExitCondition.Pay: return "Pay " + Money.Format(Cost);
                    case ExitCondition.NoBackpack: return "No backpack";
                    case ExitCondition.Switch: return Powered ? "Gate powered" : "Needs power (find the switch)";
                    case ExitCondition.CoopScav: return "Needs a scav with you";
                    case ExitCondition.Flare: return HeliLanded ? "Helicopter waiting" : HeliCalled ? "Helicopter inbound " + Mathf.CeilToInt(HeliEta) + "s" : "Fire a flare to call the helicopter";
                    default: return null;
                }
            }
        }
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
