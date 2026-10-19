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
    }

    /// <summary>
    /// The Zones. Six were left behind; three are reachable from the hub. Each is a fixed map (learnable — the
    /// knowledge is the real loot) with its own hazards, artifacts, threats and pace.
    /// </summary>
    public static class ZoneCatalog
    {
        public const string DefaultZone = "kestrel";

        public static readonly List<ZoneDef> All = new List<ZoneDef>
        {
            new ZoneDef
            {
                Id = "kestrel", Name = "Kestrel Flats", Depth = 1, MinLevel = 1, LayoutSeed = 1993,
                Blurb = "The outer Zone: a snowbound, emptied town just inside the cordon. Most runners start here.",
                Rule = "The dead are many; the anomalies are few and lie where the old maps say.",
                LootMultiplier = 1f, AnomalyDensity = 0.7f, ArtifactRichness = 0.7f, ZombieDensity = 1f, MutantShare = 0.12f,
                HumanSquads = 2f, Radiation = 0f, FogDensity = 0.25f, HasCordon = true, StartHour = 22f,
                Anomalies = new[] { "sinkhole", "embermoss", "staticbloom", "greysyrup" },
                Artifacts = new[] { "art_hollowstone", "art_coldspark", "art_mosscalm", "art_greywax", "art_saltlung" },
                Landmarks = new[] { "crashed_helicopter", "quarantine_tent", "warden_checkpoint" },
                Mutants = new[] { "hound" }, Wildlife = new[] { "deer", "wolf" },
                Tint = new Color(0.78f, 0.84f, 0.92f),
            },
            new ZoneDef
            {
                Id = "mirelock", Name = "Mirelock Woods", Depth = 2, MinLevel = 5, LayoutSeed = 7741,
                Blurb = "A drowned settlement swallowed by forest. Fog that never lifts, and things that move in it.",
                Rule = "The fog never lifts and the woods hunt at night; artifacts grow thick in the hollows.",
                LootMultiplier = 1.15f, AnomalyDensity = 1.1f, ArtifactRichness = 1.4f, ZombieDensity = 0.7f, MutantShare = 0.45f,
                HumanSquads = 1.5f, Radiation = 0.002f, FogDensity = 0.75f, ForestDensity = 0.85f, StartHour = 20f,
                Anomalies = new[] { "sinkhole", "embermoss", "staticbloom", "greysyrup", "tollgate" },
                Artifacts = new[] { "art_needlebloom", "art_driftshell", "art_whisperglass", "art_mosscalm", "art_emberheart" },
                Landmarks = new[] { "drowned_chapel", "hunters_hide", "returned_cemetery" },
                Mutants = new[] { "hound", "shade", "puppeteer" }, Wildlife = new[] { "wolf", "bear", "deer" },
                Tint = new Color(0.70f, 0.80f, 0.70f),
            },
            new ZoneDef
            {
                Id = "spillway", Name = "The Spillway", Depth = 3, MinLevel = 12, LayoutSeed = 31337,
                Blurb = "The industrial heart below the old dam. Radiation, the Choir, and the rumour of the Lodestar.",
                Rule = "Emissions come twice as often and the ground itself is hot; the richest artifacts are here.",
                LootMultiplier = 1.35f, AnomalyDensity = 1.6f, ArtifactRichness = 1.9f, ZombieDensity = 0.8f, MutantShare = 0.55f,
                HumanSquads = 2.5f, Radiation = 0.006f, FogDensity = 0.45f, EmissionMult = 2f, StartHour = 21f,
                Anomalies = new[] { "sinkhole", "embermoss", "staticbloom", "greysyrup", "tollgate" },
                Artifacts = new[] { "art_lodeknot", "art_whisperglass", "art_emberheart", "art_hollowstone", "art_coldspark", "art_needlebloom" },
                Landmarks = new[] { "cooling_tower", "choir_sanctum", "lodestar_site" },
                Mutants = new[] { "hound", "shade", "puppeteer", "thumper" }, Wildlife = new[] { "wolf" },
                Tint = new Color(0.86f, 0.78f, 0.66f),
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
