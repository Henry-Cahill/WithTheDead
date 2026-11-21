using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>One of the places runners live between raids. Guides walk you between them for a fee.</summary>
    public sealed class HubDef
    {
        public string Id, Name, Blurb;
        public int MinLevel;
        public int PassageCost;        // first visit (unlock) — later trips cost TravelCost
        public int TravelCost;
        public string Faction;         // who runs the place
        public string NearestZone;     // the zone whose insertion is cheapest from here
    }

    public static class HubCatalog
    {
        public const string Kettle = "kettle", NorthCamp = "northcamp", Dam = "dam";

        public static readonly List<HubDef> All = new List<HubDef>
        {
            new HubDef { Id = Kettle, Name = "The Rusty Kettle", MinLevel = 1, PassageCost = 0, TravelCost = 150, Faction = Factions.Freehold, NearestZone = "intro",
                Blurb = "A bar in the last village before the cordon. Smoke, bad vodka, the best gossip in the region." },
            new HubDef { Id = NorthCamp, Name = "North Camp", MinLevel = 6, PassageCost = 2500, TravelCost = 400, Faction = Factions.Freehold, NearestZone = "intro",
                Blurb = "A Freehold camp in an old sawmill at the edge of the woods. Closer to the fog, further from the law." },
            new HubDef { Id = Dam, Name = "The Dam Bunker", MinLevel = 12, PassageCost = 6000, TravelCost = 900, Faction = Factions.Vanguard, NearestZone = "intro",
                Blurb = "Vanguard's concrete bunker in the dam wall, above the Spillway. Strict, safe, and expensive." },
        };

        public static HubDef Get(string id) => All.Find(h => h.Id == id) ?? All[0];
    }
}
