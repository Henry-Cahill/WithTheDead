using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What kind of harm a blow does. The first four are the original kinds (values kept so nothing that
    /// stores them shifts); the rest are the Zone's damage types — each suit and artifact resists them
    /// separately (see <see cref="Resistances"/>).
    /// </summary>
    public enum DamageKind
    {
        Melee, Bullet, Shove, Environment,
        Radiation, Burn, Shock, Chemical, Psi, Explosion, Fall, Bite,
    }

    /// <summary>
    /// The seven hit zones of a body (Tarkov's limb model). Every humanoid — survivor, remote player,
    /// stalker, zombie — is split into these; animals and mutants use <see cref="Thorax"/> for the body
    /// and <see cref="Head"/> for the skull.
    /// </summary>
    public enum BodyZone { Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg }

    public static class BodyZones
    {
        public const int Count = 7;
        public static readonly BodyZone[] All =
            { BodyZone.Head, BodyZone.Thorax, BodyZone.Stomach, BodyZone.LeftArm, BodyZone.RightArm, BodyZone.LeftLeg, BodyZone.RightLeg };

                /// <summary>Returns the display name for a body zone.</summary>
        public static string Name(BodyZone z)
        {
            switch (z)
            {
                case BodyZone.Head: return "Head";
                case BodyZone.Thorax: return "Thorax";
                case BodyZone.Stomach: return "Stomach";
                case BodyZone.LeftArm: return "Left arm";
                case BodyZone.RightArm: return "Right arm";
                case BodyZone.LeftLeg: return "Left leg";
                default: return "Right leg";
            }
        }

                /// <summary>True if the zone represents an arm (left or right).</summary>
        public static bool IsArm(BodyZone z) => z == BodyZone.LeftArm || z == BodyZone.RightArm;
                /// <summary>True if the zone represents a leg (left or right).</summary>
        public static bool IsLeg(BodyZone z) => z == BodyZone.LeftLeg || z == BodyZone.RightLeg;
                /// <summary>True if the zone is a vital part (head or thorax).</summary>
        public static bool IsVital(BodyZone z) => z == BodyZone.Head || z == BodyZone.Thorax;

        /// <summary>The zone a hit lands on, from where it struck relative to a standing body: height
        /// above the feet (metres, body ~1.8 m tall) and which side of the body's centre line.</summary>
        public static BodyZone FromHit(Transform body, Vector3 hitPoint, float bodyHeight = 1.8f)
        {
            if (body == null) return BodyZone.Thorax;
            Vector3 local = body.InverseTransformPoint(hitPoint);
            float h = Mathf.Clamp01(local.y / Mathf.Max(0.5f, bodyHeight));
            bool left = local.x < 0f;
            if (h > 0.84f) return BodyZone.Head;
            if (h > 0.48f)
            {
                if (Mathf.Abs(local.x) > 0.21f) return left ? BodyZone.LeftArm : BodyZone.RightArm;
                return h > 0.64f ? BodyZone.Thorax : BodyZone.Stomach;
            }
            return left ? BodyZone.LeftLeg : BodyZone.RightLeg;
        }

        /// <summary>A random zone weighted like a melee strike or a zombie's lunge (torso and arms most).</summary>
        public static BodyZone RandomStrike()
        {
            float r = Random.value;
            if (r < 0.10f) return BodyZone.Head;
            if (r < 0.40f) return BodyZone.Thorax;
            if (r < 0.55f) return BodyZone.Stomach;
            if (r < 0.67f) return BodyZone.LeftArm;
            if (r < 0.79f) return BodyZone.RightArm;
            if (r < 0.90f) return BodyZone.LeftLeg;
            return BodyZone.RightLeg;
        }
    }

    /// <summary>Per-type protection 0..1 (fraction of that damage absorbed), summed from worn suits, armour,
    /// artifacts and traits, and capped by <see cref="Cap"/>.</summary>
    [System.Serializable]
    public struct Resistances
    {
        public float Bullet, Radiation, Burn, Shock, Chemical, Psi, Explosion;
        public const float Cap = 0.85f;

                /// <summary>Gets the resistance value for the specified damage kind.</summary>
        public float For(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Bullet: return Bullet;
                case DamageKind.Radiation: return Radiation;
                case DamageKind.Burn: return Burn;
                case DamageKind.Shock: return Shock;
                case DamageKind.Chemical: return Chemical;
                case DamageKind.Psi: return Psi;
                case DamageKind.Explosion: return Explosion;
                default: return 0f;
            }
        }

        public void Add(in Resistances o)
        {
            Bullet += o.Bullet; Radiation += o.Radiation; Burn += o.Burn; Shock += o.Shock;
            Chemical += o.Chemical; Psi += o.Psi; Explosion += o.Explosion;
        }

        public void Add(DamageKind kind, float amount)
        {
            switch (kind)
            {
                case DamageKind.Bullet: Bullet += amount; break;
                case DamageKind.Radiation: Radiation += amount; break;
                case DamageKind.Burn: Burn += amount; break;
                case DamageKind.Shock: Shock += amount; break;
                case DamageKind.Chemical: Chemical += amount; break;
                case DamageKind.Psi: Psi += amount; break;
                case DamageKind.Explosion: Explosion += amount; break;
            }
        }

        /// <summary>The multiplier left after resistance: 1 - clamp(resist, 0, Cap).</summary>
        public float Multiplier(DamageKind kind) => 1f - Mathf.Clamp(For(kind), 0f, Cap);
    }

    /// <summary>Describes a single instance of damage being dealt.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageKind Kind;
        public Vector3 HitPoint;
        public Vector3 HitDirection;   // world direction the blow travels
        public bool IsHeadshot;
        public bool FromBehind;
        public float Knockback;
        public GameObject Source;

        // Ballistics & the limb model. A zero penetration means "no armour test" (melee, claws, the
        // environment); ZoneKnown false lets the receiver pick a zone itself.
        public BodyZone Zone;
        public bool ZoneKnown;
        public float Penetration;      // 0..~70: compared against an armour class x10 (see Ballistics)
        public float ArmorDamage;      // fraction of the round's damage that wears armour durability
        public float BleedChance;      // 0..1 extra chance the wound bleeds (hollow points, claws)
        public float FractureChance;   // 0..1 extra chance a limb hit breaks the bone
        public string WeaponId;        // what dealt it (kill feed, quests)
        public ulong AttackerClientId; // networked attacker (ulong.MaxValue = AI / environment)
        /// <summary>True when <see cref="Amount"/> is in hit points of a limb model (bullets, claws, explosions:
        /// a 9 mm round ~55). False is the original zombie-arena scale (a knife ~1.1); receivers convert with
        /// <see cref="DamageScale"/>.</summary>
        public bool HpScale;

        public DamageInfo(float amount, DamageKind kind, Vector3 hitPoint, Vector3 hitDir, GameObject source)
        {
            Amount = amount;
            Kind = kind;
            HitPoint = hitPoint;
            HitDirection = hitDir;
            IsHeadshot = false;
            FromBehind = false;
            Knockback = 0f;
            Source = source;
            Zone = BodyZone.Thorax;
            ZoneKnown = false;
            Penetration = 0f;
            ArmorDamage = 0f;
            BleedChance = 0f;
            FractureChance = 0f;
            WeaponId = null;
            AttackerClientId = ulong.MaxValue;
            HpScale = false;
        }

        /// <summary>The damage in limb hit points, whichever scale it was dealt in.</summary>
        public float AmountHp => HpScale ? Amount : Amount * DamageScale.ArenaToHp;

        /// <summary>The damage on the original zombie-arena scale, whichever scale it was dealt in.</summary>
        public float AmountArena => HpScale ? Amount / DamageScale.ArenaToHp : Amount;

        /// <summary>Sets the struck zone (and the headshot flag that goes with it).</summary>
        public DamageInfo WithZone(BodyZone zone)
        {
            Zone = zone;
            ZoneKnown = true;
            IsHeadshot = zone == BodyZone.Head;
            return this;
        }
    }

    /// <summary>Anything that can receive damage (zombies, the player).</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        Transform Transform { get; }
        void TakeDamage(in DamageInfo info);
    }

    /// <summary>Converts between the original zombie-arena damage scale and limb hit points.</summary>
    public static class DamageScale
    {
        /// <summary>One arena damage point in limb hit points (a kitchen knife's 1.1 ≈ 33 HP).</summary>
        public const float ArenaToHp = 30f;
    }
}
