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

        public CharacterProfile Active =>
            ActiveIndex >= 0 && ActiveIndex < Characters.Count ? Characters[ActiveIndex] : null;

        public bool HasFlag(string f) => Flags.Contains(f);
        public void SetFlag(string f) { if (!Flags.Contains(f)) Flags.Add(f); }

        public bool Knows(string k) => Knowledge.Contains(k);
        public bool Learn(string k)
        {
            if (string.IsNullOrEmpty(k) || Knowledge.Contains(k)) return false;
            Knowledge.Add(k);
            return true;
        }

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

        // Statistics.
        public int Raids, Extracts, Deaths, Kills, PlayerKills, ScavRuns, ArtifactsFound;

        public bool HasFlag(string f) => Flags.Contains(f);
        public void SetFlag(string f) { if (!Flags.Contains(f)) Flags.Add(f); }

        public float GetFactionRep(string faction)
        {
            for (int i = 0; i < FactionRep.Count; i++) if (FactionRep[i].Faction == faction) return FactionRep[i].Rep;
            return faction == Faction ? 400f : 0f;
        }

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

        public TraderState Trader(string id)
        {
            for (int i = 0; i < Traders.Count; i++) if (Traders[i].Id == id) return Traders[i];
            var t = new TraderState { Id = id };
            Traders.Add(t);
            return t;
        }

        public QuestState Quest(string id)
        {
            for (int i = 0; i < Quests.Count; i++) if (Quests[i].Id == id) return Quests[i];
            return null;
        }

        public HideoutModuleState Module(string id)
        {
            for (int i = 0; i < Hideout.Count; i++) if (Hideout[i].Id == id) return Hideout[i];
            var m = new HideoutModuleState { Id = id };
            Hideout.Add(m);
            return m;
        }

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
    }

    [Serializable]
    public class ALifeSquadState
    {
        public string Id;
        public string Faction;
        public int Members;
        public Vector3 Position;
        public string Goal;
        public float Strength = 1f;
    }
}
