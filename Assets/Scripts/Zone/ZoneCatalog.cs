using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One of the Zones left behind by the Passing: a map with its own rules. Nobody explains why.</summary>
    public sealed class ZoneDef
    {
        public string Id, Name, Blurb, Rule;
        /// <summary>Fixed map layout: the same streets every raid, so routes can be learned.</summary>
        public int LayoutSeed;
        public int Depth;                   // 1 outer .. 3 deep
        public int MinLevel;                // character level needed to buy passage in
        public float LootMultiplier = 1f;
        public float AnomalyDensity = 1f;
        public float ArtifactRichness = 1f;
        public float ZombieDensity = 1f;
        public float MutantShare = 0.2f;     // of hostile spawns at night
        public float HumanSquads = 2f;       // A-Life squads roaming by day
        public float Radiation;              // background radiation per second outdoors
        public float FogDensity = 0.3f;
        public float ForestDensity;          // 0 town .. 1 deep woods
        public float EmissionMult = 1f;
        public bool HasCordon;
        public bool Winter = true;
        public float StartHour = 22f;
        public string[] Anomalies = new string[0];
        public string[] Artifacts = new string[0];
        public string[] Landmarks = new string[0];
        public string[] Mutants = new string[0];
        public string[] Wildlife = new string[0];
        public Color Tint = Color.white;
        /// <summary>A real place in Kentucky to build the map from instead of the procedural town (a name from the world
        /// store's places, e.g. "Bardstown"; see <see cref="KentuckyMap"/>), or null for the procedural town.</summary>
        public string RealPlace;
        /// <summary>Half the side of the playable square around <see cref="RealPlace"/>, in metres.</summary>
        public float RealHalfSize = 200f;
    }

    /// <summary>
    /// The Zones. Six were left behind; three are reachable from the hub. Each is a fixed map (learnable — the
    /// knowledge is the real loot) with its own hazards, artifacts, threats and pace.
    /// </summary>
    public static class ZoneCatalog
    {
        public const string DefaultZone = "intro";

        public static readonly List<ZoneDef> All = new List<ZoneDef>
        {
            new ZoneDef
            {
                Id = "intro",
                Name = "Introduction",
                Depth = 0,
                MinLevel = 1,
                LayoutSeed = 0,
                Blurb = "Wave‑survival tutorial map.",
                Rule = "Guides the player through the basics.",
                LootMultiplier = 1f,
                AnomalyDensity = 0f,
                ArtifactRichness = 0f,
                ZombieDensity = 0f,
                MutantShare = 0f,
                HumanSquads = 0f,
                Radiation = 0f,
                FogDensity = 0.3f,
                Tint = Color.white,
            },
            new ZoneDef
            {
                Id = "bardstown", Name = "Bardstown, Kentucky", Depth = 1, MinLevel = 1, LayoutSeed = 1780,
                RealPlace = "Bardstown", RealHalfSize = 200f,
                Blurb = "The real streets of Bardstown, Kentucky, snowed in and emptied. The town is still being built. "
                    + "Map data © OpenStreetMap contributors.",
                Rule = "Every street is where it really is; learn the town like a local.",
                LootMultiplier = 1f, AnomalyDensity = 0.7f, ArtifactRichness = 0.7f, ZombieDensity = 1f, MutantShare = 0.12f,
                HumanSquads = 2f, Radiation = 0f, FogDensity = 0.25f, HasCordon = true, StartHour = 22f,
                Anomalies = new[] { "sinkhole", "embermoss", "staticbloom", "greysyrup" },
                Artifacts = new[] { "art_hollowstone", "art_coldspark", "art_mosscalm", "art_greywax", "art_saltlung" },
                Mutants = new[] { "hound" }, Wildlife = new[] { "deer", "wolf" },
                Tint = new Color(0.80f, 0.84f, 0.90f),
            },
        };

        public static ZoneDef Get(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i];
            return All[0];
        }

        /// <summary>The zone of the current raid.</summary>
        public static ZoneDef Current => Get(GameSession.ZoneId);
    }
}
