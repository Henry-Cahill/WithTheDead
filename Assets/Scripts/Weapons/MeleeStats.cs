using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// How a melee weapon handles, beyond the damage it does: how far and how wide it reaches, how long a blow takes to come
    /// (the wind-up, from the press to the blow landing) and to recover from (before the next swing, stomp or shove), how
    /// likely a blow is to stagger one of the dead (stopping it in its tracks and spoiling its lunge) or to put it on the
    /// ground, and how many bodies one swing carries through. That last follows the arc: a stab or a short chop finds one
    /// body, a wider swing two, a full sweep three, each further body taking less of the blow
    /// (<see cref="CloneZConfig.MeleeCarryThrough"/>). Made by <see cref="For"/>.
    /// </summary>
    public sealed class MeleeStats
    {
        /// <summary>The weapon's item id, or <see cref="BareHandsId"/>.</summary>
        public string Id;
        public float Damage;          // raw, on the zombie-arena scale (the dead apply their received multiplier)
        public float Reach;           // metres
        public float Arc;             // degrees across the swing
        public float WindUp;          // seconds from the press to the blow landing
        public float Recovery;        // seconds after it lands before the next swing, stomp or shove
        public float Knockback;       // how hard a blow shoves what it hits back
        public float Stagger;         // 0..1 chance a blow staggers one of the dead (the rest only flinch and keep coming)
        public float Knockdown;       // 0..1 chance a blow puts one of the dead on the ground
        public int MaxTargets;        // bodies one swing carries through (see TargetsFor)
        public float Endurance;       // endurance a swing costs
        public float BleedChance, FractureChance;
        public bool TwoHanded;
        public PlayerSkills.Skill Skill;

        /// <summary>The id bare hands go by (in kill feeds, as the "weapon").</summary>
        public const string BareHandsId = "fists";

        /// <summary>Press to press: the wind-up and the recovery.</summary>
        public float Cycle => WindUp + Recovery;

        // How the hand-made weapons handle: the share of a swing spent winding up, and the chances a blow staggers or fells.
        // Anything else is worked out from its weight and its kind (see For).
        private static readonly Dictionary<string, Vector3> s_Tuned = new Dictionary<string, Vector3>
        {
            { "kitchenknife", new Vector3(0.28f, 0.12f, 0f) },   // a quick stab at one body: they flinch and keep coming
            { "hammer", new Vector3(0.36f, 0.45f, 0.1f) },
            { "fryingpan", new Vector3(0.37f, 0.6f, 0.15f) },
            { "crowbar", new Vector3(0.4f, 0.65f, 0.2f) },
            { "baseballbat", new Vector3(0.4f, 0.8f, 0.35f) },   // a wide sweep that rocks a crowd back and fells some of it
            { "fireaxe", new Vector3(0.45f, 0.55f, 0.45f) },     // a long heave that splits one and cleaves into the next
        };

        private static MeleeStats s_BareHands;

        /// <summary>The profile of bare hands: a quick short punch at one body.</summary>
        public static MeleeStats BareHands => s_BareHands ?? (s_BareHands = MakeBareHands());

        /// <summary>The profile of a melee weapon (bare hands for null, or for anything that isn't a melee weapon). Damage, reach,
        /// arc, swing time and knockback are the catalogue's; the rest is tuned by hand for the weapons above, else worked out
        /// from the weapon's weight, grip and kind: heavier swings wind up longer, cost more breath and stagger and fell more,
        /// and blades stagger and fell less than blunt weapons of their weight.</summary>
        public static MeleeStats For(ItemDef weapon)
        {
            if (weapon == null || !weapon.IsWeapon) return BareHands;
            bool blade = weapon.WeaponSkill == PlayerSkills.Skill.Blade;
            float heavy = Mathf.Clamp01((weapon.Weight - 0.3f) / 2.7f + (weapon.TwoHanded ? 0.15f : 0f));
            Vector3 handling = s_Tuned.TryGetValue(weapon.Id, out Vector3 tuned) ? tuned
                : new Vector3(Mathf.Lerp(0.28f, 0.45f, heavy),
                    blade ? Mathf.Lerp(0.15f, 0.5f, heavy) : Mathf.Lerp(0.35f, 0.85f, heavy),
                    blade ? Mathf.Lerp(0f, 0.3f, heavy) : Mathf.Lerp(0.05f, 0.4f, heavy));
            float cycle = Mathf.Max(0.25f, weapon.SwingCooldown);
            return new MeleeStats
            {
                Id = weapon.Id,
                Damage = weapon.WeaponDamage,
                Reach = weapon.WeaponRange,
                Arc = weapon.WeaponArc,
                WindUp = cycle * handling.x,
                Recovery = cycle * (1f - handling.x),
                Knockback = weapon.Knockback,
                Stagger = Mathf.Clamp01(handling.y),
                Knockdown = Mathf.Clamp01(handling.z),
                MaxTargets = TargetsFor(weapon.WeaponArc),
                Endurance = CloneZConfig.MeleeEnduranceCost * Mathf.Lerp(0.6f, 2f, heavy),
                BleedChance = blade ? 0.35f : 0.1f,
                FractureChance = weapon.WeaponSkill == PlayerSkills.Skill.Blunt ? 0.15f : 0.05f,
                TwoHanded = weapon.TwoHanded,
                Skill = weapon.WeaponSkill,
            };
        }

        private static MeleeStats MakeBareHands()
        {
            float cycle = CloneZConfig.BareHandCooldown;
            return new MeleeStats
            {
                Id = BareHandsId,
                Damage = CloneZConfig.BareHandDamage,
                Reach = CloneZConfig.BareHandRange,
                Arc = CloneZConfig.BareHandArc,
                WindUp = cycle * CloneZConfig.BareHandWindUpShare,
                Recovery = cycle * (1f - CloneZConfig.BareHandWindUpShare),
                Knockback = CloneZConfig.BareHandKnockback,
                Stagger = CloneZConfig.BareHandStagger,
                Knockdown = CloneZConfig.BareHandKnockdown,
                MaxTargets = 1,
                Endurance = CloneZConfig.MeleeEnduranceCost * 0.6f,
                BleedChance = 0f,
                FractureChance = 0.05f,
                Skill = PlayerSkills.Skill.Blunt,
            };
        }

        /// <summary>Bodies one swing of <paramref name="arc"/> degrees carries through: a stab or a short chop (under 75°) finds
        /// one, a wider swing two, a full sweep (95° and over) three.</summary>
        public static int TargetsFor(float arc) => arc < 75f ? 1 : arc < 95f ? 2 : 3;

        /// <summary>The share of a blow the <paramref name="index"/>-th body it meets takes (the first takes it all).</summary>
        public static float Carry(int index) => index <= 0 ? 1f : Mathf.Pow(CloneZConfig.MeleeCarryThrough, index);
    }
}
