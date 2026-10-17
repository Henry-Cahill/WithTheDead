using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The complete serialisable snapshot of a run — world clock, weather, utilities,
    /// score, the survivor (stats/skills/wounds/clothing/pack/equipment) and every
    /// dynamic world object (containers, barricades, house pieces, farm plots, water
    /// sources, props and the starter home's furniture).
    /// Written to disk as JSON by <see cref="SaveSystem"/> and rebuilt by the
    /// <see cref="GameBootstrapper"/> when a save is found on boot.
    ///
    /// All types here are plain <see cref="SerializableAttribute"/> DTOs so Unity's
    /// <see cref="JsonUtility"/> can round-trip them (no Dictionaries — clothing is
    /// stored as an array of slot/id pairs instead).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int Version;
        public string SavedAtUtc;

        // World clock & atmosphere.
        public int Day = 1;
        public float Hour;
        public int Weather;             // (int)Weather
        public float Rain, Fog, Cloud;

        // Utilities & score.
        public bool MainsOn = true;
        public int ShutoffDay;
        public bool PowerOn = true;
        public int PowerShutoffDay;     // 0 in a save from before the power grid existed (the loader schedules one)
        public bool BreakerOn = true;   // the house's main breaker
        public int Kills;
        public int Wave;
        public float TimeSurvived;

        // The survivor.
        public PlayerSave Player;

        // Dynamic world objects.
        public List<ContainerSave> Containers = new List<ContainerSave>();
        public List<BarricadeSave> Barricades = new List<BarricadeSave>();
        public List<HousePieceSave> House = new List<HousePieceSave>();
        public List<FarmPlotSave> Farm = new List<FarmPlotSave>();
        public List<WaterSourceSave> Water = new List<WaterSourceSave>();

        // HasPrebuiltFlags is false for saves written before house pieces recorded whether the game raised
        // them (HousePieceSave.Prebuilt), so the loader infers that from the starter home's blueprint instead.
        public bool HasPrebuiltFlags;

        // Pre-placed props (bed, stove, cover). HasWorldProps is false for saves written before
        // these became demolishable, so the loader knows to rebuild the defaults instead of
        // ending up with an empty base.
        public bool HasWorldProps;
        public List<PropSave> Props = new List<PropSave>();

        // The starter home's loose furniture. HasFurniture is false for saves written before the home
        // was furnished, so the loader knows to furnish it (around whatever the run kept).
        public bool HasFurniture;
        public List<FurnitureSave> Furniture = new List<FurnitureSave>();

        // The state of every interactive fixture (doors, windows, taps, lamps, switches…). HasFixtures is
        // false for saves written before the home's fixtures were interactive, so the loader knows to lay
        // out the bathroom, kitchen and media furniture added with them.
        public bool HasFixtures;
        public List<FixtureSave> Fixtures = new List<FixtureSave>();
    }

    /// <summary>The survivor's full state.</summary>
    [Serializable]
    public class PlayerSave
    {
        public Vector3 Position;
        public float FacingYaw;

        // Vitals (mirrors the public fields on PlayerStats).
        public float Health, Endurance, Hunger, Thirst, Fatigue, Infection;
        public bool Infected;
        public float Pain, Panic, Temperature, Wetness, Sickness;

        // Skills — XP per skill, indexed by the PlayerSkills.Skill enum order.
        public float[] SkillXp;

        // Wounds & medical timers.
        public List<WoundSave> Wounds = new List<WoundSave>();
        public float AntibioticTimer, PainkillerTimer, PainkillerRelief;
        public float BetaTimer;         // beta-blocker cover remaining (PlayerPanic)

        // Worn clothing (slot/id pairs) and the carried pack.
        public ClothingSave[] Clothing;
        public List<StackSave> Items = new List<StackSave>();

        // Equipped weapon and, for firearms, the rounds in the magazine.
        public string EquippedWeaponId;
        public int LoadedAmmo;
    }

    /// <summary>One stack in an inventory: item id, quantity, its age stamp (for perishables)
    /// and its wear (for damageable gear).</summary>
    [Serializable]
    public class StackSave
    {
        public string Id;
        public int Count;
        public float SpawnHours;
        public int Condition;
        public int TimesRepaired;
    }

    /// <summary>One tracked injury.</summary>
    [Serializable]
    public class WoundSave
    {
        public int Type;                // (int)WoundType
        public bool Bleeding, Bandaged, CleanBandage, Disinfected, Infected, Prone;
        public float Sepsis, Healing, HealSeconds;
    }

    /// <summary>A worn garment: which slot, which item, and its per-piece wear (condition + how
    /// many times it has been mended).</summary>
    [Serializable]
    public class ClothingSave
    {
        public int Slot;                // (int)ClothingSlot
        public string Id;
        public int Condition;
        public int TimesRepaired;
    }

    /// <summary>A world container. <see cref="Kind"/> is the <see cref="ContainerType"/> int for the
    /// world container types (Corpse included), or <see cref="LootContainer.KindSack"/> for a dropped
    /// ground item (restored as a <see cref="WorldItem"/> from its single saved stack).</summary>
    [Serializable]
    public class ContainerSave
    {
        public int Kind;
        public Vector3 Position;
        public float Yaw;
        public bool Searched;
        public float Lifetime;
        public string FixtureKey;       // a fixture's storage (LootContainer.FixtureKey): poured back into the live fixture
        public List<StackSave> Items = new List<StackSave>();
    }

    /// <summary>A player-built barricade.</summary>
    [Serializable]
    public class BarricadeSave
    {
        public Vector3 Position;
        public Vector3 Forward;
        public int Planks;
        public float Health, MaxHealth, PerPlank;
    }

    /// <summary>A house piece (floor, wall, doorway, window or roof) — the survivor's own, or one of the
    /// starter home's (<see cref="Prebuilt"/>). <see cref="Type"/> is the <see cref="BuildPieceType"/>;
    /// <see cref="Position"/> is the piece's occupancy centre and <see cref="Yaw"/> its facing.</summary>
    [Serializable]
    public class HousePieceSave
    {
        public int Type;                // (int)BuildPieceType
        public Vector3 Position;
        public float Yaw;
        public float Health, MaxHealth;
        public bool Prebuilt;           // raised by the game for free (BuildPiece.Prebuilt): dismantles for nothing
    }

    /// <summary>A farm plot and its growing crop.</summary>
    [Serializable]
    public class FarmPlotSave
    {
        public Vector3 Position;
        public string CropId;
        public int State;               // (int)FarmPlot.PlotState
        public float Water, Growth, Health, LastHours;
    }

    /// <summary>A tap or rain barrel (and, for a barrel, its collected water).</summary>
    [Serializable]
    public class WaterSourceSave
    {
        public Vector3 Position;
        public int Kind;                // (int)WaterSourceKind
        public float Water;
    }

    /// <summary>A pre-placed prop the survivor may have demolished: a bed, a stove or a cover block.
    /// <see cref="Kind"/> is 0 Bed, 1 Stove, 2 Cover; <see cref="Size"/> is used only by cover.</summary>
    [Serializable]
    public class PropSave
    {
        public const int KindBed = 0, KindStove = 1, KindCover = 2;
        public int Kind;
        public Vector3 Position;
        public Vector3 Size;
    }

    /// <summary>A piece of the starter home's furniture: <see cref="Kind"/> is the
    /// <see cref="HomeFurnitureKind"/>, <see cref="Yaw"/> its facing and <see cref="Seed"/> its
    /// variation (fabric, wood tone, what's left on it).</summary>
    [Serializable]
    public class FurnitureSave
    {
        public int Kind;
        public Vector3 Position;
        public float Yaw;
        public int Seed;
    }

    /// <summary>The state of one interactive fixture (<see cref="Interactable"/>), matched back to the live
    /// fixture by <see cref="Key"/>. What <see cref="State"/>, <see cref="A"/>, <see cref="B"/> and
    /// <see cref="Text"/> mean is up to the fixture (open/locked bits, a water level, a channel…).</summary>
    [Serializable]
    public class FixtureSave
    {
        public string Key;
        public int State;
        public float A, B;
        public string Text;
    }
}
