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

            // A shove deals no damage but reliably staggers and shoves back (PZ), and may put it down: likelier from behind
            // or into one already reeling. One on the ground has nothing left to shove.
            if (info.Kind == DamageKind.Shove)
            {
                if (AI.IsDowned) return;
                if (RollKnockdown(info)) AI.KnockDown(info.HitDirection, info.Knockback * 0.5f);
                else AI.Stagger(0.7f, info.HitDirection, info.Knockback);
                ZombieVoices.Hurt(this);
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

            // Fire doesn't stop them: a burning one keeps coming (PZ), so a burn neither staggers it nor makes it cry out.
            if (info.Kind == DamageKind.Burn) return;

            // On the ground: a blow (a stomp) keeps it there a moment longer.
            if (AI.IsDowned)
            {
                if (info.Kind == DamageKind.Melee) AI.Pin(info.HitDirection);
                ZombieVoices.Hurt(this);
                return;
            }

            // Survived the blow: it may go down, or reel for a moment (longer the harder the weapon shoves), or only flinch
            // and keep coming — a melee weapon carries its own chances (MeleeStats); a round always staggers, briefly.
            if (RollKnockdown(info)) AI.KnockDown(info.HitDirection, info.Knockback * 0.5f);
            else if (info.Stagger >= 1f || UnityEngine.Random.value < info.Stagger)
            {
                float stun = info.Kind == DamageKind.Melee
                    ? CloneZConfig.MeleeStaggerBase + CloneZConfig.MeleeStaggerPerKnockback * info.Knockback + (info.IsHeadshot ? 0.2f : 0f)
                    : info.IsHeadshot ? 0.5f : 0.25f;
                AI.Stagger(stun, info.HitDirection, info.Knockback);
            }
            else AI.Flinch(info.HitDirection, info.Knockback);
            ZombieVoices.Hurt(this);
        }

        // A blow's knockdown chance, more against a body already reeling or struck from behind; a crawler is down already.
        private bool RollKnockdown(in DamageInfo info)
        {
            float chance = info.Knockdown;
            if (chance <= 0f || AI.Type == ZombieType.Crawler) return false;
            if (AI.IsStaggered) chance += CloneZConfig.KnockdownReelingBonus;
            if (info.FromBehind) chance += CloneZConfig.KnockdownFromBehindBonus;
            return UnityEngine.Random.value < Mathf.Min(0.95f, chance);
        }

        private void Die(Vector3 hitDirection)
        {
            if (IsDead) return;
            IsDead = true;
            Health = 0f;
            GameManager.Instance?.ReportZombieKill();
            ZombieVoices.Say(this, SfxIds.ZombieDie, transform.position + Vector3.up * (AI != null && AI.Type == ZombieType.Crawler ? 0.4f : 1.4f), 0.85f, 36f, true);
            OnDeath?.Invoke(this);
            AI.Kill(hitDirection);
        }
    }
}
