using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>A collider that belongs to one body zone of an actor (heads take more, armour covers some).</summary>
    public sealed class Hitbox : MonoBehaviour
    {
        public BodyZone Zone = BodyZone.Thorax;
        public Actor Owner;
        public float DamageMult = 1f;
    }

    /// <summary>One bullet (or pellet) to resolve.</summary>
    public struct ShotRequest
    {
        public Vector3 Origin, Direction;
        public float Range;
        public string AmmoId, WeaponId;
        public float DamageMult;
        public GameObject Shooter;
        public Actor ShooterActor;
        public ulong ShooterClient;
        /// <summary>Server time the shooter saw the world at (lag compensation rewinds remote players to it).</summary>
        public double ViewTime;
    }

    public struct ShotResult
    {
        public bool Hit;
        public Vector3 End, Normal;
        public IDamageable Target;
        public Actor TargetActor;
        public BodyZone Zone;
        public bool Killed;
    }

    /// <summary>
    /// Resolves every bullet, blow and blast in the game against whatever it hits: the world blocks, characters
    /// take damage on the zone struck, kills are reported to <see cref="GameEvents.ActorKilled"/>. Shots leave
    /// the muzzle, never the camera, so cover blocks a round even when the camera sees past it. A networked
    /// client hands its shots to the server through <see cref="ShotRouter"/>; the server rewinds remote players
    /// (<see cref="Rewind"/>) to what the shooter saw before casting.
    /// </summary>
    public static class CombatResolver
    {
        /// <summary>When set (networked client), shots are sent here instead of being resolved locally.</summary>
        public static Func<ShotRequest, bool> ShotRouter;

        /// <summary>Set by the netcode on the server: moves remote players' hitboxes to a past time for one cast, then back.</summary>
        public static Action<double, bool> Rewind;

        /// <summary>Raised after a shot resolves (tracers, impacts, network broadcast).</summary>
        public static event Action<ShotRequest, ShotResult> ShotResolved;

        /// <summary>Layers bullets interact with: the world and characters.</summary>
        public static int ShotMask => (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);

        private static readonly RaycastHit[] s_Hits = new RaycastHit[24];

        /// <summary>Fires one bullet. Locally resolved unless a router takes it; returns the local result (End only when routed).</summary>
        public static ShotResult Fire(ShotRequest req)
        {
            if (ShotRouter != null && ShotRouter(req))
            {
                // The server will resolve it; draw the tracer to where the round would reach locally.
                return Cast(req, applyDamage: false);
            }
            return Resolve(req);
        }

        /// <summary>Resolves a shot with damage (single-player, or on the server).</summary>
        public static ShotResult Resolve(ShotRequest req)
        {
            bool rewound = false;
            if (Rewind != null && req.ViewTime > 0)
            {
                Rewind(req.ViewTime, true);
                rewound = true;
            }
            ShotResult r;
            try { r = Cast(req, applyDamage: true); }
            finally { if (rewound) Rewind(req.ViewTime, false); }
            ShotResolved?.Invoke(req, r);
            return r;
        }

        private static ShotResult Cast(ShotRequest req, bool applyDamage)
        {
            var result = new ShotResult { End = req.Origin + req.Direction * req.Range };
            int n = Physics.RaycastNonAlloc(req.Origin, req.Direction, s_Hits, req.Range, ShotMask, QueryTriggerInteraction.Ignore);
            Array.Sort(s_Hits, 0, n, HitDistance.Instance);
            Transform shooter = req.Shooter != null ? req.Shooter.transform : null;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = s_Hits[i];
                if (shooter != null && h.collider.transform.IsChildOf(shooter)) continue;
                Hitbox box = h.collider.GetComponent<Hitbox>();
                IDamageable target = box != null && box.Owner != null ? box.Owner.Damageable : h.collider.GetComponentInParent<IDamageable>();
                if (target == null) break; // world geometry stops the round
                if (target.IsDead) continue; // skip dead actors

                // Record hit details now that we have a live target.
                result.Hit = true;
                result.End = h.point;
                result.Normal = h.normal;

                Actor actor = box != null ? box.Owner : (target.Transform != null ? target.Transform.GetComponent<Actor>() : null);
                BodyZone zone = box != null ? box.Zone : BodyZones.FromHit(target.Transform, h.point, actor != null ? actor.Height : 1.8f);
                result.Target = target;
                result.TargetActor = actor;
                result.Zone = zone;
                if (applyDamage)
                {
                    DamageInfo info = BulletDamage(req, h.point, zone);
                    if (box != null) info.Amount *= box.DamageMult;
                    result.Killed = Apply(target, info, req.ShooterActor);
                }
                break;
            }
            return result;
        }

        /// <summary>The damage a round of <paramref name="req"/>'s ammunition does on a zone.</summary>
        public static DamageInfo BulletDamage(in ShotRequest req, Vector3 point, BodyZone zone)
        {
            AmmoStats ammo = Ballistics.Ammo(req.AmmoId);
            float dmg = (ammo != null ? ammo.Damage : 45f) * Mathf.Max(0.05f, req.DamageMult);
            var info = new DamageInfo(dmg, DamageKind.Bullet, point, req.Direction, req.Shooter)
            {
                HpScale = true,
                Penetration = ammo != null ? ammo.Penetration : 20f,
                ArmorDamage = ammo != null ? ammo.ArmorDamage : 0.4f,
                BleedChance = ammo != null ? ammo.Fragmentation : 0.15f,
                FractureChance = BodyZones.IsArm(zone) || BodyZones.IsLeg(zone) ? 0.18f : 0f,
                WeaponId = req.WeaponId,
                AttackerClientId = req.ShooterClient,
                Knockback = 0.6f,
            }.WithZone(zone);
            return info;
        }

        /// <summary>Applies damage and reports the kill if this blow was the one. Returns true on a kill.</summary>
        public static bool Apply(IDamageable target, DamageInfo info, Actor attacker)
        {
            if (target == null || target.IsDead) return false;
            target.TakeDamage(info);
            if (!target.IsDead) return false;
            Actor victim = target.Transform != null ? target.Transform.GetComponent<Actor>() : null;
            GameEvents.RaiseActorKilled(new KillInfo
            {
                Victim = victim,
                Killer = attacker != null ? attacker : (info.Source != null ? info.Source.GetComponentInParent<Actor>() : null),
                WeaponId = info.WeaponId,
                Zone = info.Zone,
                Surrendered = victim != null && victim.Surrendered,
                Position = target.Transform != null ? target.Transform.position : info.HitPoint,
            });
            return true;
        }

        /// <summary>A blast: damage falls off to the edge, walls shield, everyone hears it.</summary>
        public static void Explosion(Vector3 center, float radius, float damage, GameObject source, string weaponId = "explosion")
        {
            NoiseSystem.Emit(center, radius * 6f);
            GameEvents.RaiseWorldEvent(center, radius * 6f, "explosion");
            WeaponAudio.Report(SfxIds.Explosion, center, 1f, UnityEngine.Random.Range(0.9f, 1.05f));
            Actor attacker = source != null ? source.GetComponentInParent<Actor>() : null;
            var done = new HashSet<IDamageable>();
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || !a.IsAlive) continue;
                Vector3 to = a.Center - center;
                float d = to.magnitude;
                if (d > radius) continue;
                if (Physics.Raycast(center + Vector3.up * 0.2f, to.normalized, d - 0.3f, Actor.SightMask, QueryTriggerInteraction.Ignore)) continue;
                IDamageable t = a.Damageable;
                if (t == null || !done.Add(t)) continue;
                float falloff = 1f - Mathf.Clamp01(d / radius);
                var info = new DamageInfo(damage * falloff * falloff, DamageKind.Explosion, a.Center, to.normalized, source)
                {
                    HpScale = true, Knockback = 4f * falloff, BleedChance = 0.5f, FractureChance = 0.3f, WeaponId = weaponId,
                }.WithZone(BodyZones.RandomStrike());
                Apply(t, info, attacker);
            }
            for (int i = 0; i < Breakables.Count; i++)
            {
                IExplosionTarget e = Breakables[i];
                if (e != null) e.OnExplosion(center, radius, damage);
            }
        }

        /// <summary>Things a blast can break besides creatures (built walls, gates, doors).</summary>
        public static readonly List<IExplosionTarget> Breakables = new List<IExplosionTarget>();

        private sealed class HitDistance : IComparer<RaycastHit>
        {
            public static readonly HitDistance Instance = new HitDistance();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }

    /// <summary>Something a blast can damage that isn't a creature.</summary>
    public interface IExplosionTarget
    {
        void OnExplosion(Vector3 center, float radius, float damage);
    }
}
