using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The complete serialisable snapshot of a run — world clock, weather, utilities,
    /// score, the survivor (stats/skills/wounds/clothing/pack/equipment) and every
    /// dynamic world object (containers, barricades, farm plots, water sources).
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
        public int Kills;
        public int Wave;
        public float TimeSurvived;

        // The survivor.
        public PlayerSave Player;

        // Dynamic world objects.
        public List<ContainerSave> Containers = new List<ContainerSave>();
        public List<BarricadeSave> Barricades = new List<BarricadeSave>();
        public List<FarmPlotSave> Farm = new List<FarmPlotSave>();
        public List<WaterSourceSave> Water = new List<WaterSourceSave>();
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
    }

    /// <summary>One tracked injury.</summary>
    [Serializable]
    public class WoundSave
    {
        public int Type;                // (int)WoundType
        public bool Bleeding, Bandaged, CleanBandage, Disinfected, Infected, Prone;
        public float Sepsis, Healing, HealSeconds;
    }

    /// <summary>A worn garment: which slot and which item.</summary>
    [Serializable]
    public class ClothingSave
    {
        public int Slot;                // (int)ClothingSlot
        public string Id;
    }

    /// <summary>A world container. <see cref="Kind"/> is 0..5 for the world container types,
    /// 6 for a corpse and 7 for a dropped sack (see <see cref="LootContainer.KindSack"/>).</summary>
    [Serializable]
    public class ContainerSave
    {
        public int Kind;
        public Vector3 Position;
        public float Yaw;
        public bool Searched;
        public float Lifetime;
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
}
