using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The groups that live in (and around) the Zone, how they feel about each other, and how they feel about
    /// the survivor personally. Who you work for decides where you are safe: the base relation comes from
    /// your faction's standing with theirs, and your own reputation with them (earned on jobs, lost by
    /// shooting their people) can override it either way.
    /// </summary>
    public static class Factions
    {
        // Player-joinable (picked at character creation; each has a base, vendors and outposts).
        public const string Freehold = "freehold";       // free runners: loose collective of independent zone-runners
        public const string Vanguard = "vanguard";       // disciplined paramilitary sworn to contain the Zone
        public const string Lantern = "lantern";         // smugglers and fences who run the black market
        // NPC-only.
        public const string Wardens = "wardens";         // the cordon's border troops
        public const string Scavs = "scavs";             // desperate raiders and bandits
        public const string Institute = "institute";     // the research institute (the legal economy)
        public const string Choir = "choir";             // the Hollow Choir: zealots guarding the deep Zone
        public const string Zone = "zone";               // the dead, mutants and wild animals

        public static readonly string[] Joinable = { Freehold, Vanguard, Lantern };
        public static readonly string[] AllIds = { Freehold, Vanguard, Lantern, Wardens, Scavs, Institute, Choir, Zone };

        public sealed class Def
        {
            public string Id, Name, Blurb;
            public Color Color;
            public bool Joinable;
        }

        private static readonly Dictionary<string, Def> s_Defs = new Dictionary<string, Def>
        {
            { Freehold,  new Def { Id = Freehold,  Name = "Freehold",            Color = new Color(0.55f, 0.72f, 0.40f), Joinable = true,
                Blurb = "Independent runners who share tips, routes and campfires. Friendly with most; nobody's soldiers." } },
            { Vanguard,  new Def { Id = Vanguard,  Name = "Vanguard",            Color = new Color(0.78f, 0.32f, 0.26f), Joinable = true,
                Blurb = "A disciplined paramilitary sworn to keep the Zone contained. Good kit, strict rules, enemies of the Choir and smugglers." } },
            { Lantern,   new Def { Id = Lantern,   Name = "Lantern Syndicate",   Color = new Color(0.86f, 0.70f, 0.28f), Joinable = true,
                Blurb = "Smugglers and fences. The best black-market prices and the worst friends. The Wardens shoot them on sight." } },
            { Wardens,   new Def { Id = Wardens,   Name = "Cordon Wardens",      Color = new Color(0.42f, 0.50f, 0.36f),
                Blurb = "Border troops. Anyone in the restricted belt without papers is a target." } },
            { Scavs,     new Def { Id = Scavs,     Name = "Scavengers",          Color = new Color(0.55f, 0.45f, 0.35f),
                Blurb = "Desperate raiders. The Fence sends his own runners out among them." } },
            { Institute, new Def { Id = Institute, Name = "The Institute",       Color = new Color(0.40f, 0.62f, 0.85f),
                Blurb = "Scientists who study the Zone legally. Safe, low-paying contracts and fixed prices for artifacts." } },
            { Choir,     new Def { Id = Choir,     Name = "Hollow Choir",        Color = new Color(0.62f, 0.40f, 0.78f),
                Blurb = "Zealots who guard the deep Zone and the thing they believe sleeps there. Hostile to all." } },
            { Zone,      new Def { Id = Zone,      Name = "The Zone",            Color = new Color(0.45f, 0.55f, 0.30f),
                Blurb = "The dead, the mutated and the wild." } },
        };

        // Base relations: -1 hostile, 0 neutral, +1 allied. Symmetric; anything unlisted is neutral.
        private static readonly Dictionary<string, int> s_Rel = new Dictionary<string, int>();

        static Factions()
        {
            foreach (string a in AllIds)
            {
                Set(a, Zone, -1);
                Set(a, Choir, -1);
            }
            Set(Zone, Zone, 1);
            Set(Choir, Choir, 1);
            Set(Choir, Zone, 0);          // the Choir walks among the dead untouched
            Set(Freehold, Freehold, 1); Set(Vanguard, Vanguard, 1); Set(Lantern, Lantern, 1);
            Set(Wardens, Wardens, 1); Set(Scavs, Scavs, 1); Set(Institute, Institute, 1);
            Set(Scavs, Freehold, -1); Set(Scavs, Vanguard, -1); Set(Scavs, Wardens, -1); Set(Scavs, Institute, -1);
            Set(Scavs, Lantern, 0);       // the fences buy from scavs
            Set(Vanguard, Lantern, -1);
            Set(Wardens, Lantern, -1);
            Set(Wardens, Freehold, 0);
            Set(Institute, Freehold, 1); Set(Institute, Vanguard, 1);
        }

        private static string Key(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;

        private static void Set(string a, string b, int rel) => s_Rel[Key(a, b)] = rel;

        public static int Relation(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
            return s_Rel.TryGetValue(Key(a, b), out int r) ? r : 0;
        }

        public static Def Get(string id) => id != null && s_Defs.TryGetValue(id, out Def d) ? d : s_Defs[Zone];
        public static string NameOf(string id) => Get(id).Name;

        /// <summary>
        /// Whether <paramref name="self"/> attacks <paramref name="other"/>. When one side is a player-controlled
        /// survivor, that player's personal reputation with the other faction shifts the base relation: deeply
        /// disliked (&lt; -300) means hostile, well liked (&gt; 300) means friendly, whatever the factions think.
        /// </summary>
        public static bool AreHostile(string a, string b, Actor self, Actor other)
        {
            int rel = Relation(a, b);
            Actor player = self != null && self.IsPlayerControlled ? self : (other != null && other.IsPlayerControlled ? other : null);
            if (player != null && player.Kind != ActorKind.PlayerMonster)
            {
                string npcFaction = player == self ? b : a;
                if (npcFaction == Zone || npcFaction == Choir) return true;
                float rep = PlayerReputation(npcFaction);
                if (rep <= -300f) return true;
                if (rep >= 300f) return false;
                // Wardens only shoot runners caught inside the restricted cordon belt.
                if (npcFaction == Wardens && rel >= 0) return CordonRules.PlayerInRestrictedBelt;
            }
            return rel < 0;
        }

        /// <summary>The local survivor's personal reputation with a faction (-1000..1000), from the profile.</summary>
        public static float PlayerReputation(string faction)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            return c != null ? c.GetFactionRep(faction) : 0f;
        }

        /// <summary>Shifts the local survivor's reputation with a faction (killing their people, finishing their jobs).</summary>
        public static void AdjustReputation(string faction, float delta, string reason = null)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || string.IsNullOrEmpty(faction) || faction == Zone) return;
            c.AddFactionRep(faction, delta);
            if (!string.IsNullOrEmpty(reason) && Mathf.Abs(delta) >= 5f)
                GameEvents.RaiseToast((delta > 0 ? "+" : "") + Mathf.RoundToInt(delta) + " rep with " + NameOf(faction) + " (" + reason + ")");
        }
    }

    /// <summary>Where the cordon's restricted belt runs on the current map (set by the raid builder).</summary>
    public static class CordonRules
    {
        /// <summary>Distance from the map edge inside which the Wardens shoot unlicensed runners.</summary>
        public static float BeltWidth = 14f;
        public static bool Active;
        /// <summary>A bribe paid at the gate (or Warden reputation) lets the survivor through the belt this raid.</summary>
        public static bool PassBought;

        public static bool PlayerInRestrictedBelt
        {
            get
            {
                if (!Active || PassBought || Actor.LocalPlayer == null) return false;
                Vector3 p = Actor.LocalPlayer.transform.position;
                float half = CloneZConfig.ArenaHalfSize;
                return Mathf.Abs(p.x) > half - BeltWidth || Mathf.Abs(p.z) > half - BeltWidth;
            }
        }
    }
}
