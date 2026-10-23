using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // Every type here is a plain [Serializable] DTO so JsonUtility can round-trip the profile file
    // (lists of entries instead of dictionaries). ProfileStore owns the I/O.

    /// <summary>The account: its characters (up to three), the season, and what was learned that no wipe takes away.</summary>
    [Serializable]
    public class AccountProfile
    {
        public int Version = 1;
        public string AccountName = "Runner";
        /// <summary>A random id for this account (tells camps and cars apart when playing together).</summary>
        public string Id;
        public int Season = 1;
        public long SeasonStartedUnix;
        public int Wipes;
        public int ActiveIndex;
        public List<CharacterProfile> Characters = new List<CharacterProfile>();
        /// <summary>Map knowledge, safe routes and anomaly rules the player has learned — survives deaths and wipes.</summary>
        public List<string> Knowledge = new List<string>();
        /// <summary>Artifact properties identified ("artifact:property") — knowledge, so it survives wipes too.</summary>
        public List<string> ArtifactKnowledge = new List<string>();
        /// <summary>Progress towards the rumoured wish-granting Lodestar (server-wide when hosting).</summary>
        public LodestarState Lodestar = new LodestarState();
        /// <summary>Who holds each warfare outpost and when it can next be taken.</summary>
        public List<OutpostState> Outposts = new List<OutpostState>();
        /// <summary>Remembered A-Life squads and the persistent state of open-zone maps.</summary>
        public List<ZoneWorldState> Zones = new List<ZoneWorldState>();
        public List<string> Flags = new List<string>();

        public const int MaxCharacters = 3;

        /// <summary>Gets the currently active character profile, or null if none.</summary>
        public CharacterProfile Active =>
            ActiveIndex >= 0 && ActiveIndex < Characters.Count ? Characters[ActiveIndex] : null;

        /// <summary>Returns true if the object has the given flag.</summary>
        /// <summary>Returns true if the character has the given flag.</summary>
        public bool HasFlag(string f) => Flags != null && Flags.Contains(f);
        /// <summary>Adds the flag if not already present.</summary>
        /// <summary>Adds the flag to the character if not present.</summary>
        public void SetFlag(string f) { if (Flags == null) Flags = new List<string>(); if (!Flags.Contains(f)) Flags.Add(f); }

        /// <summary>Checks if the account knows the given knowledge key.</summary>
        public bool Knows(string k) => Knowledge.Contains(k);
        /// <summary>Attempts to learn a new knowledge key; returns false if empty or already known.</summary>
        public bool Learn(string k)
        {
            if (string.IsNullOrEmpty(k) || Knowledge.Contains(k)) return false;
            Knowledge.Add(k);
            return true;
        }

        /// <summary>Gets or creates a ZoneWorldState for the given zone id.</summary>
        public ZoneWorldState Zone(string id)
        {
            for (int i = 0; i < Zones.Count; i++) if (Zones[i].ZoneId == id) return Zones[i];
            var z = new ZoneWorldState { ZoneId = id };
            Zones.Add(z);
            return z;
        }
    }

    /// <summary>One character: who they are, what they own, who they know and what the Zone has done to them.</summary>
    [Serializable]
    public class CharacterProfile
    {
        public string Id;
        public string Name = "Runner";
        public string Profession = "drifter";
        public List<string> Traits = new List<string>();
        public string Faction = Factions.Freehold;
        public string BloodType = "O+";
        public bool Ironman;
        public bool Dead;
        public string CauseOfDeath;
        public long CreatedUnix;

        // Progression (never lost on death; reset by a wipe).
        public int Level = 1;
        public float Xp;
        public List<float> SkillXp = new List<float>();
        public List<SkillBookState> Books = new List<SkillBookState>();

        // Possessions.
        public RaidKit Kit;
        public List<StackSave> Stash = new List<StackSave>();
        public int StashRows = 14;
        public int PouchLevel;                       // 0: 2x2, 1: 3x2, 2: 3x3, 3: 4x3 secure pouch
        public List<InsuranceClaim> Insurance = new List<InsuranceClaim>();
        public List<FleaListing> Listings = new List<FleaListing>();

        // Relationships.
        public List<TraderState> Traders = new List<TraderState>();
        public List<FactionRepEntry> FactionRep = new List<FactionRepEntry>();
        public List<QuestState> Quests = new List<QuestState>();

        // The hideout.
        public List<HideoutModuleState> Hideout = new List<HideoutModuleState>();
        public List<CraftJob> Crafts = new List<CraftJob>();
        public List<AnimalState> Animals = new List<AnimalState>();

        // The Zone's long-term cost on the body (survives death — it's the character's body that pays).
        public float Exposure;
        public List<string> ZoneMarks = new List<string>();
        public HealthCarry Health = new HealthCarry();

        /// <summary>Hidden. Mercy, listening and helping raise it; cruelty lowers it. Never shown.</summary>
        public float Moral;

        /// <summary>Black-market heat: every artifact sold to the fence adds some; the Wardens come to collect when it's high.</summary>
        public float Heat;

        // The pressure behind every run: rent on the room above the bar, a debt, family to feed.
        public int Day = 1;
        public int RentPaidThroughDay = 7;
        public float Debt = 6000f;
        public int FamilyPaidThroughDay = 3;
        public int MissedPayments;

        // Scav runs, corpses, stashes.
        public long ScavReadyUnix;
        public List<CorpseRecord> Corpses = new List<CorpseRecord>();
        public List<BuriedStash> Stashes = new List<BuriedStash>();

        public List<string> Flags = new List<string>();
        public List<string> UnlockedHubs = new List<string> { "kettle" };
        public string Hub = "kettle";
        /// <summary>Saved kits (the field tablet's loadouts): worn pieces and carried items, refilled from the stash.</summary>
        public List<LoadoutPreset> Loadouts = new List<LoadoutPreset>();

        // Statistics.
        public int Raids, Extracts, Deaths, Kills, PlayerKills, ScavRuns, ArtifactsFound;

        /// <summary>Returns true if the object has the given flag.</summary>
        /// <summary>Returns true if the character has the given flag.</summary>
        public bool HasFlag(string f) => Flags != null && Flags.Contains(f);
        /// <summary>Adds the flag if not already present.</summary>
        /// <summary>Adds the flag to the character if not present.</summary>
        public void SetFlag(string f) { if (Flags == null) Flags = new List<string>(); if (!Flags.Contains(f)) Flags.Add(f); }

        /// <summary>Retrieves the reputation value for a faction, defaulting to 0 or 400 for own faction.</summary>
        public float GetFactionRep(string faction)
        {
            for (int i = 0; i < FactionRep.Count; i++) if (FactionRep[i].Faction == faction) return FactionRep[i].Rep;
            return faction == Faction ? 400f : 0f;
        }

        /// <summary>Adds delta to the reputation for a faction, clamped between -1000 and 1000.</summary>
        public void AddFactionRep(string faction, float delta)
        {
            for (int i = 0; i < FactionRep.Count; i++)
                if (FactionRep[i].Faction == faction)
                {
                    FactionRep[i].Rep = Mathf.Clamp(FactionRep[i].Rep + delta, -1000f, 1000f);
                    return;
                }
            FactionRep.Add(new FactionRepEntry { Faction = faction, Rep = Mathf.Clamp(GetFactionRep(faction) + delta, -1000f, 1000f) });
        }

        /// <summary>Gets or creates a TraderState for the given trader id.</summary>
        public TraderState Trader(string id)
        {
            for (int i = 0; i < Traders.Count; i++) if (Traders[i].Id == id) return Traders[i];
            var t = new TraderState { Id = id };
            Traders.Add(t);
            return t;
        }

        /// <summary>Gets a QuestState by id, or null if not found.</summary>
        public QuestState Quest(string id)
        {
            for (int i = 0; i < Quests.Count; i++) if (Quests[i].Id == id) return Quests[i];
            return null;
        }

        /// <summary>Gets or creates a HideoutModuleState for the given module id.</summary>
        public HideoutModuleState Module(string id)
        {
            for (int i = 0; i < Hideout.Count; i++) if (Hideout[i].Id == id) return Hideout[i];
            var m = new HideoutModuleState { Id = id };
            Hideout.Add(m);
            return m;
        }

        /// <summary>Gets skill XP at index, or 0 if out of range.</summary>
        public float GetSkillXp(int idx) => idx >= 0 && idx < SkillXp.Count ? SkillXp[idx] : 0f;
    }

    /// <summary>Wounds and Zone sickness carried out of a raid into the hub (healed over days or by the medic).</summary>
    [Serializable]
    public class HealthCarry
    {
        public List<float> LimbHp = new List<float>();     // per BodyZone; empty = full health
        public List<bool> Fractured = new List<bool>();
        public float Blood = 5000f;
        public float Radiation;
        public List<string> Diseases = new List<string>();
        public float Hunger, Thirst;

        public bool IsHealthy => LimbHp.Count == 0 && Radiation <= 0f && Diseases.Count == 0 && Blood >= 4999f;
    }

    [Serializable] public class TraderState { public string Id; public float Rep; public int Spent; public int Loyalty = 1; public long RestockUnix; public List<OfferStock> Stock = new List<OfferStock>(); }
    [Serializable] public class OfferStock { public string OfferId; public int Left; }
    [Serializable] public class FactionRepEntry { public string Faction; public float Rep; }

    /// <summary>0 available, 1 active, 2 objectives done (ready to hand in), 3 turned in, 4 failed.</summary>
    [Serializable]
    public class QuestState
    {
        public string Id;
        public int Status;
        public List<float> Progress = new List<float>();
        public long AcceptedUnix;
        public int CompletedDay;
        public const int Available = 0, Active = 1, Ready = 2, Done = 3, Failed = 4;
    }

    [Serializable] public class HideoutModuleState { public string Id; public int Level; public float Fuel; }
    [Serializable] public class CraftJob { public string RecipeId; public string ModuleId; public long StartUnix; public long DoneUnix; }
    [Serializable] public class AnimalState { public string Kind; public string Name; public float Fed = 1f; public float Produce; public long LastTickUnix; }
    [Serializable] public class SkillBookState { public string BookId; public float Read; public bool Finished; }

    /// <summary>Insured gear that will come back from the trader on <see cref="ReturnOnDay"/> unless someone took it.</summary>
    [Serializable] public class InsuranceClaim { public string TraderId; public List<StackSave> Items = new List<StackSave>(); public int ReturnOnDay; }

    /// <summary>A body left in a zone (corpse-recovery rule): its gear waits there until recovered or looted.</summary>
    [Serializable]
    public class CorpseRecord
    {
        public string ZoneId;
        public Vector3 Position;
        public List<StackSave> Items = new List<StackSave>();
        public int DiedOnDay;
        public bool Recovered;
        public bool Looted;
    }

    /// <summary>A hidden stash: buried by the player, or tipped off by a dead stalker's PDA.</summary>
    [Serializable]
    public class BuriedStash
    {
        public string Id;
        public string ZoneId;
        public Vector3 Position;
        public List<StackSave> Items = new List<StackSave>();
        public string Hint;
        public bool FromPda;
        public bool Opened;
    }

    [Serializable]
    public class FleaListing
    {
        public string Id;
        public string Seller;
        public StackSave Item;
        public int Price;
        public long ListedUnix;
        public long ExpiresUnix;
        public bool Mine;
        public bool Sold;
    }

    [Serializable] public class OutpostState { public string Id; public string ZoneId; public string Owner; public long LockedUntilUnix; }

    [Serializable]
    public class LodestarState
    {
        public List<string> Clues = new List<string>();
        public bool SiteRevealed;
        public bool WishMade;
        public string Wish;
        public int Pilgrims;      // players (or companions) who walked the last stretch first
    }

    /// <summary>What persists of a zone between raids: the living A-Life squads and, in the open zone, the
    /// containers already emptied.</summary>
    [Serializable]
    public class ZoneWorldState
    {
        public string ZoneId;
        public List<ALifeSquadState> Squads = new List<ALifeSquadState>();
        public List<string> LootedContainers = new List<string>();
        public int EmissionsSeen;
        public long LastVisitUnix;
        /// <summary>The runner's camp here (DayZ base building): it stands in the Zone only while its owner is in it.</summary>
        public List<HousePieceSave> Camp = new List<HousePieceSave>();
        public List<BarricadeSave> CampBarricades = new List<BarricadeSave>();
        public List<CampGateSave> Gates = new List<CampGateSave>();
        /// <summary>The cars found here, as they were left (parts, fuel, where they stand).</summary>
        public List<VehicleSave> Vehicles = new List<VehicleSave>();
    }

    /// <summary>A saved kit (the field tablet's loadouts): what was worn and carried, refilled from the stash.</summary>
    [Serializable]
    public class LoadoutPreset
    {
        public string Name;
        public List<string> Worn = new List<string>();
        public List<StackSave> Carried = new List<StackSave>();
    }

    /// <summary>A gate hung in a camp doorway, and its combination lock.</summary>
    [Serializable]
    public class CampGateSave
    {
        public Vector3 Position;
        public bool Open, Locked;
        public string Code;
    }

    /// <summary>One car: which spot it was found at, its parts, its fuel, where it was left.</summary>
    [Serializable]
    public class VehicleSave
    {
        public int Spot;
        public Vector3 Position;
        public float Yaw;
        public int Wheels;
        public bool Battery, SparkPlug, Radiator, Hotwired;
        public float Fuel, Engine = 1f, Charge = 1f, Tyres = 1f;
    }

    [Serializable]
    public class ALifeSquadState
    {
        public string Id;
        public string Faction;
        public string LoadoutId;
        public int Members;
        public Vector3 Position;
        public string Goal;
        public float Strength = 1f;
    }
}
