using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Zombie hit points. Applies Project Zomboid's "non-player received damage"
    /// multiplier, handles shoves/staggers, and drives death.
    /// </summary>
    public class ZombieHealth : MonoBehaviour, IDamageable
    {
        /// <summary>Maximum health for this zombie.</summary>
        public float MaxHealth { get; private set; } = CloneZConfig.ZombieHealthNormal;
        /// <summary>Current health of the zombie.</summary>
        public float Health { get; private set; } = CloneZConfig.ZombieHealthNormal;
        /// <summary>Whether the zombie is dead.</summary>
        public bool IsDead { get; private set; }
        /// <summary>Shortcut to the zombie's Transform component.</summary>
        public Transform Transform => transform;

        /// <summary>Raised once when this zombie dies (argument is self).</summary>
        public event Action<ZombieHealth> OnDeath;

        private ZombieAI _aiCache;
        private ZombieAI AI => _aiCache != null ? _aiCache : (_aiCache = GetComponent<ZombieAI>());

        /// <summary>Sets the maximum health for this zombie and restores current health to that maximum.</summary>
        public void SetMaxHealth(float max)
        {
            MaxHealth = Mathf.Max(0.1f, max);
            Health = MaxHealth;
        }

        private void Awake()
        {
            // Every body in the world is an Actor, so the living (and other monsters) can see and hunt it.
            var actor = GetComponent<Actor>();
            if (actor == null) actor = gameObject.AddComponent<Actor>();
            actor.Kind = ActorKind.Zombie;
            actor.Faction = Factions.Zone;
            actor.Height = 1.8f;
            actor.DisplayName = "One of the dead";
            if (actor.NetId == 0) actor.NetId = Actor.AllocateNetId();
        }

        // Where a round lands matters: the head drops them, a limb barely slows them.
        private static float ZoneMultiplier(BodyZone z)
        {
            switch (z)
            {
                case BodyZone.Head: return 2.5f;
                case BodyZone.Thorax: return 1f;
                case BodyZone.Stomach: return 0.9f;
                case BodyZone.LeftArm:
                case BodyZone.RightArm: return 0.5f;
                default: return 0.6f;
            }
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;

            // A shove deals no damage but reliably staggers and shoves back (PZ).
            if (info.Kind == DamageKind.Shove)
            {
                AI.Stagger(0.7f, info.HitDirection, info.Knockback);
                return;
            }
            // The dead don't feel the Zone's radiation or its psychic pressure.
            if (info.Kind == DamageKind.Radiation || info.Kind == DamageKind.Psi) return;

            float amount = info.AmountArena;
            if (info.ZoneKnown && info.Kind != DamageKind.Melee) amount *= ZoneMultiplier(info.Zone);
            float dealt = amount * CloneZConfig.NonPlayerReceivedDamageMultiplier;
            Health -= dealt;

            if (Health <= 0f)
            {
                Die(info.HitDirection);
                return;
            }

            // Survived the blow: brief stagger scaled by how hard it hit.
            float stun = info.IsHeadshot ? 0.5f : 0.25f;
            AI.Stagger(stun, info.HitDirection, info.Knockback);
        }

        private void Die(Vector3 hitDirection)
        {
            if (IsDead) return;
            IsDead = true;
            Health = 0f;
            GameManager.Instance?.ReportZombieKill();
            Sfx.Play(SfxIds.ZombieDie, transform.position + Vector3.up * 1.4f, 0.75f, UnityEngine.Random.Range(0.85f, 1.1f), 25f);
            OnDeath?.Invoke(this);
            AI.Kill(hitDirection);
        }
    }
}
