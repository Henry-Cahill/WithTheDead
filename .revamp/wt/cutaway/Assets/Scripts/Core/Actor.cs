using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>What sort of creature an <see cref="Actor"/> is.</summary>
    public enum ActorKind { Player, RemotePlayer, Zombie, Human, Mutant, Animal, PlayerMonster, Companion }

    /// <summary>
    /// Anything alive that others can see, hear, target and fight: the survivor, other players (their
    /// network proxies), the dead, zone stalkers, mutants, animals. Every AI picks targets from
    /// <see cref="All"/> instead of assuming a single player, so co-op, PvP, faction wars and wildlife
    /// hunting all fall out of one list. Carries the faction (see <see cref="Factions"/>), the eye point
    /// used for sight lines, and the damage receiver.
    /// </summary>
    public class Actor : MonoBehaviour
    {
        public static readonly List<Actor> All = new List<Actor>();

        /// <summary>The survivor this machine controls (null on a dedicated host or before spawning).</summary>
        public static Actor LocalPlayer { get; private set; }

        public ActorKind Kind = ActorKind.Human;
        public string Faction = Factions.Zone;
        public string DisplayName = "";
        public float Height = 1.8f;
        /// <summary>The head bone (sight lines leave from here); falls back to a point at eye height.</summary>
        public Transform Head;
        /// <summary>The network client that owns this actor (players and player monsters), else ulong.MaxValue.</summary>
        public ulong OwnerClientId = ulong.MaxValue;
        /// <summary>A stable id for kill credit and quests across the network (0 = unassigned).</summary>
        public uint NetId;

        /// <summary>0 (invisible) .. 1 (lit up): how easily this actor is seen — light on them, stance and
        /// movement (written each frame by <see cref="Stealth"/> for players; 1 for AI by default).</summary>
        public float Visibility01 = 1f;
        /// <summary>A cloaked mutant or a player under a Shade's cloak — sight checks fail beyond touching range.</summary>
        public bool Cloaked;
        /// <summary>Squad / party tag: actors sharing a non-empty squad never fight each other.</summary>
        public string Squad;
        /// <summary>Sits out of every fight (a raid's spawn protection, a surrendered stalker, a sleeper in a safe hub).</summary>
        public bool Untargetable;
        /// <summary>Hands up: a stalker who gave in. Killing one anyway is remembered (see <see cref="MoralLedger"/>).</summary>
        public bool Surrendered;
        /// <summary>Restrained with cuffs or rope: can't fight, can be led.</summary>
        public bool Restrained;

        private IDamageable _damageable;

        /// <summary>Provides quick access to the <see cref="IDamageable"/> component on this actor.</summary>
        public IDamageable Damageable
        {
            get
            {
                if (_damageable == null) _damageable = GetComponent<IDamageable>();
                return _damageable;
            }
            set => _damageable = value;
        }

        /// <summary>Indicates whether the actor is currently alive and active.</summary>
        public bool IsAlive => this != null && isActiveAndEnabled && Damageable != null && !Damageable.IsDead;
        /// <summary>True if the actor is any type of player‑controlled character.</summary>
        public bool IsPlayerControlled => Kind == ActorKind.Player || Kind == ActorKind.RemotePlayer || Kind == ActorKind.PlayerMonster;
        /// <summary>True if the actor is a humanoid (player, remote player, human, companion or zombie).</summary>
        public bool IsHumanoid => Kind == ActorKind.Player || Kind == ActorKind.RemotePlayer || Kind == ActorKind.Human || Kind == ActorKind.Companion || Kind == ActorKind.Zombie;

        /// <summary>World‑space position of the actor's eyes (or head bone if present).</summary>
        public Vector3 EyePosition => Head != null ? Head.position : transform.position + Vector3.up * (Height * 0.92f);
        /// <summary>World‑space centre of the actor's body used for various calculations.</summary>
        public Vector3 Center => transform.position + Vector3.up * (Height * 0.55f);

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (Kind == ActorKind.Player) LocalPlayer = this;
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
            if (LocalPlayer == this) LocalPlayer = null;
        }

        /// <summary>Marks this actor as the local survivor (called when the player body is built).</summary>
        public void MakeLocalPlayer()
        {
            Kind = ActorKind.Player;
            LocalPlayer = this;
        }

        /// <summary>True when <paramref name="other"/> is someone this actor would attack on sight.</summary>
        public bool IsHostileTo(Actor other)
        {
            if (other == null || other == this || !other.IsAlive || other.Untargetable) return false;
            if (!string.IsNullOrEmpty(Squad) && Squad == other.Squad) return false;
            if (IsPlayerControlled && other.IsPlayerControlled && other.Kind != ActorKind.PlayerMonster && Kind != ActorKind.PlayerMonster)
                return ServerRules.PvpEnabled && (string.IsNullOrEmpty(Squad) || Squad != other.Squad);
            return Factions.AreHostile(Faction, other.Faction, this, other);
        }

        /// <summary>Clear line of sight from this actor's eyes to <paramref name="other"/>'s centre (walls block,
        /// characters don't).</summary>
        public bool CanSee(Actor other, float maxDistance)
        {
            if (other == null) return false;
            Vector3 from = EyePosition, to = other.Center;
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist > maxDistance) return false;
            if (other.Cloaked && dist > 2.5f) return false;
            return !Physics.Raycast(from, d / Mathf.Max(0.001f, dist), dist - 0.3f, SightMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Layers that block sight: the default world layer only (characters live on Ignore Raycast).</summary>
        public static int SightMask => 1 << 0;

        #region Queries
        /// <summary>The nearest living actor satisfying <paramref name="filter"/> within <paramref name="maxDistance"/>.</summary>
        public static Actor Nearest(Vector3 from, float maxDistance, Func<Actor, bool> filter)
        {
            Actor best = null;
            float bestSqr = maxDistance * maxDistance;
            for (int i = 0; i < All.Count; i++)
            {
                Actor a = All[i];
                if (a == null || !a.IsAlive || (filter != null && !filter(a))) continue;
                float sqr = (a.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = a; }
            }
            return best;
        }

        /// <summary>Every living player-controlled survivor (local and remote), for the dead to hunt.</summary>
        public static void Players(List<Actor> into)
        {
            into.Clear();
            for (int i = 0; i < All.Count; i++)
            {
                Actor a = All[i];
                if (a != null && a.IsAlive && (a.Kind == ActorKind.Player || a.Kind == ActorKind.RemotePlayer)) into.Add(a);
            }
        }

        /// <summary>Living actors of a kind within a radius.</summary>
        public static int CountNear(Vector3 from, float radius, ActorKind kind)
        {
            int n = 0;
            float r2 = radius * radius;
            for (int i = 0; i < All.Count; i++)
            {
                Actor a = All[i];
                if (a != null && a.Kind == kind && a.IsAlive && (a.transform.position - from).sqrMagnitude <= r2) n++;
            }
            return n;
        }

        /// <summary>Finds the actor that owns the given network identifier.</summary>
        public static Actor FindByNetId(uint id)
        {
            if (id == 0) return null;
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i].NetId == id) return All[i];
            return null;
        }
        #endregion

        private static uint s_NextNetId = 1;

        /// <summary>A fresh actor id (server / single-player side).</summary>
        public static uint AllocateNetId() => s_NextNetId++;
    }

    /// <summary>
    /// Anything that takes a zombie's lunge (bite or scratch): the local survivor, a remote player's proxy (it
    /// forwards the hit to its owner), a zone stalker. Lets the dead hunt more than one kind of prey.
    /// </summary>
    public interface IZombieAttackable
    {
        /// <summary>Called when a zombie bites or scratches this target.</summary>
        void ReceiveZombieAttack(bool bite);
    }

    /// <summary>The server's rules for a session (PvP, friendly fire, first-person-only and so on). Single-player
    /// reads the same switches from the sandbox settings.</summary>
    public static class ServerRules
    {
        public static bool PvpEnabled = true;
        public static bool FriendlyFire = false;
        public static bool FirstPersonOnly = false;
        public static bool AllowPlayerMonsters = true;
        public static float VoiceRange = 30f;

        /// <summary>Copies relevant rule settings from a sandbox configuration.</summary>
        public static void FromSandbox(SandboxSettings s)
        {
            if (s == null) return;
            PvpEnabled = s.PvpEnabled;
            FriendlyFire = s.FriendlyFire;
            FirstPersonOnly = s.FirstPersonOnlyInPvp && s.PvpEnabled;
            AllowPlayerMonsters = s.AllowPlayerMonsters;
            VoiceRange = s.VoiceRange;
        }
    }
}
