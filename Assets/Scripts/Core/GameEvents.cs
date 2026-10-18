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

        public static void RaiseLocalPlayerSpawned(PlayerStats p) => Safe(() => LocalPlayerSpawned?.Invoke(p));
        public static void RaiseRaidStarted(string zone, RaidKind kind) => Safe(() => RaidStarted?.Invoke(zone, kind));
        public static void RaiseRaidEnded(RaidOutcome o, string exit) => Safe(() => RaidEnded?.Invoke(o, exit));
        public static void RaiseActorKilled(KillInfo k) => Safe(() => ActorKilled?.Invoke(k));
        public static void RaiseItemLooted(ItemStack s, string source) => Safe(() => ItemLooted?.Invoke(s, source));
        public static void RaiseLocationVisited(string id) => Safe(() => LocationVisited?.Invoke(id));
        public static void RaiseItemCrafted(string id, int n) => Safe(() => ItemCrafted?.Invoke(id, n));
        public static void RaiseEmission(bool active) => Safe(() => EmissionStateChanged?.Invoke(active));
        public static void RaiseArtifactLearned(string artifact, string prop) => Safe(() => ArtifactPropertyLearned?.Invoke(artifact, prop));
        public static void RaiseDayAdvanced(int day) => Safe(() => DayAdvanced?.Invoke(day));
        public static void RaiseToast(string msg) => Safe(() => Toast?.Invoke(msg));
        public static void RaiseSubtitle(string speaker, string line) => Safe(() => Subtitle?.Invoke(speaker, line));
        public static void RaiseQuestProgress(string key, float amount) => Safe(() => QuestProgress?.Invoke(key, amount));
        public static void RaiseWorldEvent(Vector3 pos, float radius, string kind) => Safe(() => WorldEvent?.Invoke(pos, radius, kind));

        // One misbehaving listener must never break the raiser (a quest bug can't stop a kill registering).
        private static void Safe(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
