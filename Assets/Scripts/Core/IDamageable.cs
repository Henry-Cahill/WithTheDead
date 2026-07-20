using UnityEngine;

namespace CloneZ
{
    public enum DamageKind { Melee, Bullet, Shove, Environment }

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
        }
    }

    /// <summary>Anything that can receive damage (zombies, the player).</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        Transform Transform { get; }
        void TakeDamage(in DamageInfo info);
    }
}
