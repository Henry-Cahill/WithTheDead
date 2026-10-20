using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>How a raid ended for the local survivor.</summary>
    public enum RaidOutcome { Extracted, Killed, MissingInAction, Abandoned }

    /// <summary>Who died, who did it and with what — the kill feed, quests, reputation and morality read this.</summary>
    public struct KillInfo
    {
        public Actor Victim;
        public Actor Killer;
        public string WeaponId;
        public BodyZone Zone;
        public bool Surrendered;   // the victim had their hands up (a mercy killing counts against you)
        public Vector3 Position;
    }

    /// <summary>
    /// The game-wide event bus. Systems that must react to things happening elsewhere (quests counting kills,
    /// the morality ledger noticing mercy, squads sharing progress, the HUD's toasts) subscribe here instead of
    /// being wired into each other. Everything is raised on the main thread; events are cleared on world rebuild
    /// only for scene objects (static subscribers stay).
    /// </summary>
    public static class GameEvents
    {
        public static event Action<PlayerStats> LocalPlayerSpawned;
        public static event Action<string, RaidKind> RaidStarted;              // zone id, raid kind
        public static event Action<RaidOutcome, string> RaidEnded;             // outcome, exit name (if extracted)
        public static event Action<KillInfo> ActorKilled;
        public static event Action<ItemStack, string> ItemLooted;              // stack taken into the pack, source label
        public static event Action<string> LocationVisited;                    // landmark / quest location id
        public static event Action<string, int> ItemCrafted;                   // item id, count
        public static event Action<bool> EmissionStateChanged;                 // true = an emission/psi storm is under way
        public static event Action<string, string> ArtifactPropertyLearned;    // artifact id, property id
        public static event Action<int> DayAdvanced;                           // new hub day
        public static event Action<string> Toast;                              // short HUD message
        public static event Action<string, string> Subtitle;                   // speaker, line
        public static event Action<string, float> QuestProgress;               // objective key, amount (squad sharing)
        public static event Action<Vector3, float, string> WorldEvent;         // position, radius, kind (flare, heli, shot)

        /// <summary>Raises the LocalPlayerSpawned event for the given player stats.</summary>
        public static void RaiseLocalPlayerSpawned(PlayerStats p) => Safe(() => LocalPlayerSpawned?.Invoke(p));
        /// <summary>Raises the RaidStarted event when a raid begins in the given zone with the specified kind.</summary>
        public static void RaiseRaidStarted(string zone, RaidKind kind) => Safe(() => RaidStarted?.Invoke(zone, kind));
        /// <summary>Raises the RaidEnded event with the outcome and exit name (if extracted).</summary>
        public static void RaiseRaidEnded(RaidOutcome o, string exit) => Safe(() => RaidEnded?.Invoke(o, exit));
        /// <summary>Raises the ActorKilled event for the supplied kill info.</summary>
        public static void RaiseActorKilled(KillInfo k) => Safe(() => ActorKilled?.Invoke(k));
        /// <summary>Raises the ItemLooted event when an item stack is taken from a source label into the pack.</summary>
        public static void RaiseItemLooted(ItemStack s, string source) => Safe(() => ItemLooted?.Invoke(s, source));
        /// <summary>Raises the LocationVisited event for the given landmark or quest location id.</summary>
        public static void RaiseLocationVisited(string id) => Safe(() => LocationVisited?.Invoke(id));
        /// <summary>Raises the ItemCrafted event when an item id is crafted a number of times.</summary>
        public static void RaiseItemCrafted(string id, int n) => Safe(() => ItemCrafted?.Invoke(id, n));
        /// <summary>Raises the EmissionStateChanged event to indicate whether a psi storm is active.</summary>
        public static void RaiseEmission(bool active) => Safe(() => EmissionStateChanged?.Invoke(active));
        /// <summary>Raises the ArtifactPropertyLearned event when a player learns an artifact property.</summary>
        public static void RaiseArtifactLearned(string artifact, string prop) => Safe(() => ArtifactPropertyLearned?.Invoke(artifact, prop));
        /// <summary>Raises the DayAdvanced event when the hub day counter increments.</summary>
        public static void RaiseDayAdvanced(int day) => Safe(() => DayAdvanced?.Invoke(day));
        /// <summary>Raises the Toast event to display a short HUD message.</summary>
        public static void RaiseToast(string msg) => Safe(() => Toast?.Invoke(msg));
        /// <summary>Raises the Subtitle event to show a line spoken by a speaker.</summary>
        public static void RaiseSubtitle(string speaker, string line) => Safe(() => Subtitle?.Invoke(speaker, line));
        /// <summary>Raises the QuestProgress event for a specific quest objective key and amount.</summary>
        public static void RaiseQuestProgress(string key, float amount) => Safe(() => QuestProgress?.Invoke(key, amount));
        /// <summary>Raises the WorldEvent event for a world occurrence at a position with radius and kind.</summary>
        public static void RaiseWorldEvent(Vector3 pos, float radius, string kind) => Safe(() => WorldEvent?.Invoke(pos, radius, kind));

        // One misbehaving listener must never break the raiser (a quest bug can't stop a kill registering).
        private static void Safe(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
