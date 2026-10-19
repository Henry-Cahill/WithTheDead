using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One line of a carcass's loot: an item id, how many, and the chance it is there at all.</summary>
    public struct CreatureLoot
    {
        public string Id;
        public int Min, Max;
        public float Chance;

        public CreatureLoot(string id, int min, int max, float chance = 1f)
        {
            Id = id;
            Min = min;
            Max = Mathf.Max(min, max);
            Chance = chance;
        }
    }

    /// <summary>
    /// Tuning for one species of the Zone's non-human life. Hit points are limb hit points (a 9 mm round ≈ 55);
    /// distances are metres, speeds metres per second.
    /// </summary>
    public sealed class CreatureDef
    {
        public string Id, Name, CorpseName;
        public ActorKind Kind = ActorKind.Mutant;
        public float MaxHp = 100f;
        public float WalkSpeed = 1.5f, RunSpeed = 5f, Acceleration = 14f, TurnRate = 9f;
        /// <summary>Sight reach against a fully lit target; scaled down by the target's visibility and the dark.</summary>
        public float Sight = 30f;
        public float NightSight = 1f;
        /// <summary>Added to a noise's own radius (as for the dead).</summary>
        public float Hearing = 8f;
        /// <summary>Vision cone (degrees) while not yet alerted.</summary>
        public float Fov = 200f;
        /// <summary>Anything this close is noticed whichever way the creature faces.</summary>
        public float Awareness = 5f;
        /// <summary>Seconds a target out of sight is still hunted.</summary>
        public float ForgetTime = 10f;
        public float AttackDamage, AttackRange = 1.6f, AttackCooldown = 1.2f, AttackWindup = 0.25f;
        public DamageKind AttackKind = DamageKind.Bite;
        public float Bleed, Fracture, Knockback;
        public int PackMin = 1, PackMax = 1;
        /// <summary>Actor height (sight lines, explosions) and the agent's avoidance radius.</summary>
        public float Height = 1f, Radius = 0.4f;
        /// <summary>Damage in one blow that knocks it back a step (0 = never staggers).</summary>
        public float StaggerThreshold;
        public float Xp;
        public float CorpseLifetime = 240f;
        public CreatureLoot[] Loot = new CreatureLoot[0];

        public bool IsMutant => Kind == ActorKind.Mutant;
        public bool IsPack => PackMax > 1;
    }

    /// <summary>
    /// The creatures the threat director can field. Mutants each carry one trick (enemies as puzzles); wildlife
    /// follows its own instincts. Ids match <see cref="ZoneDef.Mutants"/> and <see cref="ZoneDef.Wildlife"/>.
    /// </summary>
    public static class CreatureCatalog
    {
        public const string Hound = "hound", Shade = "shade", Puppeteer = "puppeteer", Thumper = "thumper",
            Wolf = "wolf", Bear = "bear", Deer = "deer";

        public const string MutantMeat = "mutantmeat", RawMeat = "rawmeat";
        public const string HoundFang = "hound_fang", ShadeTendril = "shade_tendril", PuppeteerGland = "puppeteer_gland",
            ThumperHoof = "thumper_hoof", WolfPelt = "wolf_pelt", BearPelt = "bear_pelt", DeerHide = "deer_hide";

        public static readonly string[] AllIds = { Hound, Shade, Puppeteer, Thumper, Wolf, Bear, Deer };

        private static readonly Dictionary<string, CreatureDef> s_Defs = new Dictionary<string, CreatureDef>
        {
            {
                Hound, new CreatureDef
                {
                    Id = Hound, Name = "Hound", CorpseName = "Hound carcass", Kind = ActorKind.Mutant,
                    MaxHp = 120f, WalkSpeed = 2.4f, RunSpeed = 7.2f, Acceleration = 30f, TurnRate = 12f,
                    Sight = 32f, NightSight = 1f, Hearing = 9f, Fov = 230f, Awareness = 6f, ForgetTime = 9f,
                    AttackDamage = 20f, AttackRange = 1.6f, AttackCooldown = 1.1f, AttackWindup = 0.22f, AttackKind = DamageKind.Bite,
                    Bleed = 0.35f, PackMin = 3, PackMax = 5, Height = 0.85f, Radius = 0.35f, StaggerThreshold = 40f, Xp = 60f,
                    Loot = new[] { new CreatureLoot(MutantMeat, 1, 1, 0.7f), new CreatureLoot(HoundFang, 1, 2, 0.5f) },
                }
            },
            {
                Shade, new CreatureDef
                {
                    Id = Shade, Name = "Shade", CorpseName = "Shade remains", Kind = ActorKind.Mutant,
                    MaxHp = 500f, WalkSpeed = 2.0f, RunSpeed = 6.5f, Acceleration = 26f, TurnRate = 10f,
                    Sight = 34f, NightSight = 1f, Hearing = 10f, Fov = 210f, Awareness = 5f, ForgetTime = 14f,
                    AttackDamage = 45f, AttackRange = 1.9f, AttackCooldown = 1.6f, AttackWindup = 0.15f, AttackKind = DamageKind.Melee,
                    Bleed = 0.6f, Knockback = 1.5f, Height = 1.95f, Radius = 0.4f, StaggerThreshold = 0f, Xp = 450f,
                    Loot = new[] { new CreatureLoot(MutantMeat, 2, 2), new CreatureLoot(ShadeTendril, 1, 1, 0.8f) },
                }
            },
            {
                Puppeteer, new CreatureDef
                {
                    Id = Puppeteer, Name = "Puppeteer", CorpseName = "Puppeteer remains", Kind = ActorKind.Mutant,
                    MaxHp = 350f, WalkSpeed = 1.0f, RunSpeed = 1.6f, Acceleration = 8f, TurnRate = 6f,
                    Sight = 32f, NightSight = 1f, Hearing = 8f, Fov = 200f, Awareness = 5f, ForgetTime = 16f,
                    AttackDamage = 12f, AttackRange = 1.8f, AttackCooldown = 1.8f, AttackWindup = 0.45f, AttackKind = DamageKind.Melee,
                    Bleed = 0.1f, Height = 1.75f, Radius = 0.4f, StaggerThreshold = 70f, Xp = 380f,
                    Loot = new[] { new CreatureLoot(MutantMeat, 1, 1), new CreatureLoot(PuppeteerGland, 1, 1, 0.9f) },
                }
            },
            {
                Thumper, new CreatureDef
                {
                    Id = Thumper, Name = "Thumper", CorpseName = "Thumper carcass", Kind = ActorKind.Mutant,
                    MaxHp = 1500f, WalkSpeed = 1.3f, RunSpeed = 3.0f, Acceleration = 6f, TurnRate = 3.5f,
                    Sight = 30f, NightSight = 1f, Hearing = 7f, Fov = 180f, Awareness = 6f, ForgetTime = 18f,
                    AttackDamage = 80f, AttackRange = 6f, AttackCooldown = 3.8f, AttackWindup = 1.0f, AttackKind = DamageKind.Explosion,
                    Fracture = 0.25f, Knockback = 8f, Height = 3.2f, Radius = 1.1f, StaggerThreshold = 0f, Xp = 900f,
                    CorpseLifetime = 300f,
                    Loot = new[] { new CreatureLoot(MutantMeat, 3, 4), new CreatureLoot(ThumperHoof, 1, 1) },
                }
            },
            {
                Wolf, new CreatureDef
                {
                    Id = Wolf, Name = "Wolf", CorpseName = "Wolf carcass", Kind = ActorKind.Animal,
                    MaxHp = 150f, WalkSpeed = 2.2f, RunSpeed = 7.6f, Acceleration = 30f, TurnRate = 12f,
                    Sight = 34f, NightSight = 1f, Hearing = 12f, Fov = 230f, Awareness = 7f, ForgetTime = 10f,
                    AttackDamage = 18f, AttackRange = 1.6f, AttackCooldown = 1.2f, AttackWindup = 0.2f, AttackKind = DamageKind.Bite,
                    Bleed = 0.3f, PackMin = 3, PackMax = 6, Height = 0.95f, Radius = 0.38f, StaggerThreshold = 45f, Xp = 45f,
                    Loot = new[] { new CreatureLoot(RawMeat, 2, 2), new CreatureLoot(WolfPelt, 1, 1, 0.6f) },
                }
            },
            {
                Bear, new CreatureDef
                {
                    Id = Bear, Name = "Bear", CorpseName = "Bear carcass", Kind = ActorKind.Animal,
                    MaxHp = 1200f, WalkSpeed = 1.6f, RunSpeed = 8.5f, Acceleration = 18f, TurnRate = 6f,
                    Sight = 26f, NightSight = 0.75f, Hearing = 10f, Fov = 200f, Awareness = 8f, ForgetTime = 12f,
                    AttackDamage = 55f, AttackRange = 2.2f, AttackCooldown = 1.4f, AttackWindup = 0.3f, AttackKind = DamageKind.Melee,
                    Bleed = 0.5f, Fracture = 0.2f, Knockback = 4f, Height = 1.3f, Radius = 0.7f, StaggerThreshold = 0f, Xp = 300f,
                    CorpseLifetime = 300f,
                    Loot = new[] { new CreatureLoot(RawMeat, 4, 5), new CreatureLoot(BearPelt, 1, 1, 0.8f) },
                }
            },
            {
                Deer, new CreatureDef
                {
                    Id = Deer, Name = "Deer", CorpseName = "Deer carcass", Kind = ActorKind.Animal,
                    MaxHp = 100f, WalkSpeed = 1.4f, RunSpeed = 9.0f, Acceleration = 22f, TurnRate = 10f,
                    Sight = 40f, NightSight = 0.8f, Hearing = 16f, Fov = 300f, Awareness = 12f, ForgetTime = 6f,
                    AttackDamage = 0f, PackMin = 1, PackMax = 3, Height = 1.5f, Radius = 0.4f, StaggerThreshold = 30f, Xp = 20f,
                    Loot = new[] { new CreatureLoot(RawMeat, 3, 3), new CreatureLoot(DeerHide, 1, 1, 0.6f) },
                }
            },
        };

        public static CreatureDef Get(string id) => id != null && s_Defs.TryGetValue(id, out CreatureDef d) ? d : null;

        public static bool IsMutantId(string id)
        {
            CreatureDef d = Get(id);
            return d != null && d.IsMutant;
        }
    }
}
