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
        public float MaxHealth { get; private set; } = CloneZConfig.ZombieHealthNormal;
        public float Health { get; private set; } = CloneZConfig.ZombieHealthNormal;
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        /// <summary>Raised once when this zombie dies (argument is self).</summary>
        public event Action<ZombieHealth> OnDeath;

        private ZombieAI _aiCache;
        private ZombieAI AI => _aiCache != null ? _aiCache : (_aiCache = GetComponent<ZombieAI>());

        public void SetMaxHealth(float max)
        {
            MaxHealth = Mathf.Max(0.1f, max);
            Health = MaxHealth;
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

            float dealt = info.Amount * CloneZConfig.NonPlayerReceivedDamageMultiplier;
            Health -= dealt;

            if (Health <= 0f)
            {
                Die();
                return;
            }

            // Survived the blow: brief stagger scaled by how hard it hit.
            float stun = info.IsHeadshot ? 0.5f : 0.25f;
            AI.Stagger(stun, info.HitDirection, info.Knockback);
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Health = 0f;
            GameManager.Instance?.ReportZombieKill();
            OnDeath?.Invoke(this);
            AI.Kill();
        }
    }
}
