using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>A starting profession: skill head-starts, a little kit, and how many trait points it leaves.</summary>
    public sealed class ProfessionDef
    {
        public string Id, Name, Blurb;
        public int Points;                                   // trait points this profession leaves you with
        public (PlayerSkills.Skill Skill, int Levels)[] Skills = new (PlayerSkills.Skill, int)[0];
        public (string Id, int Count)[] Kit = new (string, int)[0];
    }

    /// <summary>A Zomboid-style trait: positive ones cost points, negative ones pay them back.</summary>
    public sealed class TraitDef
    {
        public string Id, Name, Blurb;
        public int Cost;                                     // >0 costs points; <0 grants them
        public System.Action<StatMods> Apply;
        public string[] Excludes = new string[0];
    }

    /// <summary>
    /// Character creation: profession plus point-buy traits (players build a story before they spawn), a faction
    /// to join, and a blood type rolled at birth. The traits' effects ride on the survivor as a stat-mod source.
    /// </summary>
    public static class ProfessionCatalog
    {
        public static readonly List<ProfessionDef> Professions = new List<ProfessionDef>
        {
            new ProfessionDef { Id = "drifter", Name = "Drifter", Points = 8, Blurb = "No trade, no ties. Plenty of room for quirks." },
            new ProfessionDef { Id = "medic", Name = "Field Medic", Points = 2, Blurb = "Knows how to keep people alive.",
                Skills = new[] { (PlayerSkills.Skill.FirstAid, 3) }, Kit = new[] { ("fieldkit", 1), ("splint", 1), ("suturekit", 1) } },
            new ProfessionDef { Id = "soldier", Name = "Ex-Soldier", Points = 0, Blurb = "Served on the cordon before crossing it.",
                Skills = new[] { (PlayerSkills.Skill.Aiming, 2), (PlayerSkills.Skill.Reloading, 2), (PlayerSkills.Skill.Fitness, 1) }, Kit = new[] { ("ammo9mm", 30) } },
            new ProfessionDef { Id = "mechanic", Name = "Mechanic", Points = 4, Blurb = "Can get anything with wheels running again.",
                Skills = new[] { (PlayerSkills.Skill.Mechanics, 3), (PlayerSkills.Skill.Blunt, 1) }, Kit = new[] { ("scrap", 3), ("cleaningkit", 1) } },
            new ProfessionDef { Id = "hunter", Name = "Hunter", Points = 2, Blurb = "Quiet in the woods, patient with a rifle.",
                Skills = new[] { (PlayerSkills.Skill.Aiming, 1), (PlayerSkills.Skill.Sneak, 2), (PlayerSkills.Skill.Trapping, 2), (PlayerSkills.Skill.Foraging, 1) },
                Kit = new[] { ("snare", 2), ("lighter", 1) } },
            new ProfessionDef { Id = "chemist", Name = "Chemist", Points = 4, Blurb = "Turns junk and reagents into filters and medicine.",
                Skills = new[] { (PlayerSkills.Skill.FirstAid, 1), (PlayerSkills.Skill.Mechanics, 1) }, Kit = new[] { ("chemicals", 4), ("gasmaskfilter", 1) } },
            new ProfessionDef { Id = "scavenger", Name = "Scavenger", Points = 4, Blurb = "Light on the feet, quick with a locker.",
                Skills = new[] { (PlayerSkills.Skill.Lightfooted, 2), (PlayerSkills.Skill.Nimble, 1), (PlayerSkills.Skill.Sneak, 1) }, Kit = new[] { ("bolt", 10) } },
            new ProfessionDef { Id = "carpenter", Name = "Carpenter", Points = 4, Blurb = "Builds walls and barricades that hold.",
                Skills = new[] { (PlayerSkills.Skill.Carpentry, 3) }, Kit = new[] { ("hammer", 1), ("nails", 10), ("plank", 6) } },
        };

        public static readonly List<TraitDef> Traits = new List<TraitDef>
        {
            T("strong", "Strong", 4, "Carry 20% more.", m => m.CarryCapacity *= 1.2f, "weak"),
            T("athletic", "Athletic", 4, "Stamina drains 20% slower.", m => m.StaminaDrain *= 0.8f, "smoker"),
            T("fast_healer", "Fast Healer", 3, "Wounds and limbs heal 40% faster.", m => m.HealRate *= 1.4f, "slow_healer"),
            T("steady", "Steady Hands", 4, "Spread and recoil 15% lower.", m => { m.AimSpread *= 0.85f; m.Recoil *= 0.85f; }),
            T("lightstep", "Light-footed", 3, "Your footsteps carry 20% less far.", m => m.NoiseMult *= 0.8f, "clumsy"),
            T("thickskin", "Thick-skinned", 3, "Bleed 15% less; infections take hold less often.", m => { m.BleedRate *= 0.85f; m.InfectionResist += 0.25f; }, "hemophiliac"),
            T("zoneborn", "Zone-born", 4, "Grew up near the cordon: resists radiation and psi.", m => { m.Resist.Radiation += 0.15f; m.Resist.Psi += 0.1f; }),
            T("nerves", "Nerves of Steel", 3, "Panic builds 40% slower.", m => m.PanicRate *= 0.6f, "cowardly"),
            T("packmule", "Pack Mule", 2, "+4 kg carry capacity.", m => m.CarryBonusKg += 4f, "weak"),
            T("ironstomach", "Iron Stomach", 2, "Hunger and thirst 10% slower.", m => { m.HungerRate *= 0.9f; m.ThirstRate *= 0.9f; }, "bigeater"),
            T("nightowl", "Night Owl", 3, "Sees a little better in the dark.", m => m.NightVision += 0.25f),
            T("weak", "Feeble", -4, "Carry 20% less.", m => m.CarryCapacity *= 0.8f, "strong", "packmule"),
            T("smoker", "Smoker", -2, "Stamina recovers 15% slower.", m => m.StaminaRegen *= 0.85f, "athletic"),
            T("slow_healer", "Slow Healer", -3, "Wounds heal 30% slower.", m => m.HealRate *= 0.7f, "fast_healer"),
            T("hemophiliac", "Hemophiliac", -4, "Bleed 40% harder.", m => m.BleedRate *= 1.4f, "thickskin"),
            T("clumsy", "Clumsy", -2, "Footsteps carry 30% further.", m => m.NoiseMult *= 1.3f, "lightstep"),
            T("shortsighted", "Short-sighted", -2, "Spread 10% wider.", m => m.AimSpread *= 1.1f),
            T("cowardly", "Cowardly", -3, "Panic builds 50% faster.", m => m.PanicRate *= 1.5f, "nerves"),
            T("bigeater", "Hearty Appetite", -2, "Hunger 25% faster.", m => m.HungerRate *= 1.25f, "ironstomach"),
            T("restless", "Restless Sleeper", -2, "Fatigue builds 25% faster.", m => m.FatigueRate *= 1.25f),
            T("conspicuous", "Conspicuous", -3, "Easier to spot (+25% visibility).", m => m.VisibilityMult *= 1.25f),
            T("weaklungs", "Weak Lungs", -2, "Stamina drains 10% faster; gas hurts more.", m => { m.StaminaDrain *= 1.1f; m.Resist.Chemical -= 0.1f; }),
            T("frost_resist", "Frost Resistance", 3, "Stamina drains 15% slower in the cold.", m => m.StaminaDrain *= 0.85f),
            T("firestarter", "Firestarter", 2, "Night vision improves by a small margin.", m => m.NightVision += 0.15f),
            T("handy", "Handy", 2, "Carry capacity increases by 2 kg.", m => m.CarryBonusKg += 2f),
            T("superstitious", "Superstitious", 1, "You’re less likely to panic.", m => m.PanicRate *= 0.9f),
        };

        private static TraitDef T(string id, string name, int cost, string blurb, System.Action<StatMods> apply, params string[] excludes)
            => new TraitDef { Id = id, Name = name, Cost = cost, Blurb = blurb, Apply = apply, Excludes = excludes };

        public static ProfessionDef Get(string id) => Professions.Find(p => p.Id == id) ?? Professions[0];
        public static TraitDef Trait(string id) => Traits.Find(t => t.Id == id);

        /// <summary>Points left after picking traits (must be ≥ 0 to create the character).</summary>
        public static int PointsLeft(string profession, List<string> traits)
        {
            int p = Get(profession).Points;
            if (traits != null) foreach (string t in traits) { TraitDef d = Trait(t); if (d != null) p -= d.Cost; }
            return p;
        }

        public static bool Conflicts(List<string> traits, TraitDef t)
        {
            if (traits == null || t == null) return false;
            foreach (string e in t.Excludes) if (traits.Contains(e)) return true;
            foreach (string other in traits)
            {
                TraitDef o = Trait(other);
                if (o != null && System.Array.IndexOf(o.Excludes, t.Id) >= 0) return true;
            }
            return false;
        }

        /// <summary>Adds the profession's kit to a new character's stash and its head-start to their skills.</summary>
        public static void ApplyKit(CharacterProfile c)
        {
            ProfessionDef p = Get(c.Profession);
            foreach ((string id, int n) in p.Kit) StarterPack.AddStash(c, id, n);
            int count = System.Enum.GetValues(typeof(PlayerSkills.Skill)).Length;
            while (c.SkillXp.Count < count) c.SkillXp.Add(0f);
            foreach ((PlayerSkills.Skill s, int levels) in p.Skills)
                c.SkillXp[(int)s] = Mathf.Max(c.SkillXp[(int)s], PlayerSkills.XpForLevel(levels));
        }
    }

    /// <summary>The active character's traits as a stat-mod source on the survivor.</summary>
    public class TraitEffects : MonoBehaviour, IStatModSource
    {
        public void ApplyMods(StatMods mods)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || GameSession.IsScavRun || GameSession.IsWaveSurvival) return;
            for (int i = 0; i < c.Traits.Count; i++)
            {
                TraitDef t = ProfessionCatalog.Trait(c.Traits[i]);
                t?.Apply?.Invoke(mods);
            }
        }
    }
}
