using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The hidden morality track (Metro): sparing people who give up, listening to the bar's stories, helping the
    /// wounded and paying your family raise it; executing the surrendered, robbing the helpless and sending
    /// someone ahead to die on a deadly route lower it. It is never shown anywhere. It decides how the
    /// Lodestar answers a wish.
    /// </summary>
    public sealed class MoralLedger : IGameModule
    {
        public const float SparedSurrendered = 4f, KilledSurrendered = -8f, ListenedToStory = 1.5f, HelpedWounded = 5f,
            GaveToBeggar = 2f, SacrificedSomeone = -15f, RobbedHelpless = -4f, ShotTheReturned = -3f, PaidFamily = 1f,
            MissedFamily = -2f, ReturnedLostItem = 3f, KilledInnocent = -10f;

        public int Order => 10;

        public void Init()
        {
            GameEvents.ActorKilled += OnKill;
        }

        /// <summary>Records a deed (nothing is ever displayed).</summary>
        public static void Record(string deed, float amount)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || GameSession.IsWaveSurvival) return;
            c.Moral = Mathf.Clamp(c.Moral + amount, -200f, 200f);
            if (Debug.isDebugBuild) Debug.Log("[CloneZ] (hidden) moral " + (amount >= 0 ? "+" : "") + amount + " for " + deed);
        }

        public static float Value => ProfileStore.ActiveCharacter != null ? ProfileStore.ActiveCharacter.Moral : 0f;

        /// <summary>Broadly good (true), broadly bad (false) — the ending reads it this way.</summary>
        public static bool IsGood => Value >= 10f;

        private static void OnKill(KillInfo k)
        {
            if (k.Killer == null || k.Killer != Actor.LocalPlayer || k.Victim == null) return;
            if (k.Surrendered) Record("killed someone with their hands up", KilledSurrendered);
            else if (k.Victim.Kind == ActorKind.Human && (k.Victim.Faction == Factions.Institute || k.Victim.Faction == Factions.Freehold)
                     && !k.Victim.IsHostileTo(k.Killer))
                Record("killed someone who meant no harm", KilledInnocent);
        }
    }

    /// <summary>
    /// The hub's screens by name. Stations in the bar (a trader's counter, the stash locker, the hideout door,
    /// the map table...) open them through here, so the world never needs to know which class draws each one.
    /// </summary>
    public static class HubScreens
    {
        public delegate void Opener(string arg);
        private static readonly System.Collections.Generic.Dictionary<string, Opener> s_Screens =
            new System.Collections.Generic.Dictionary<string, Opener>();

        public const string Trader = "trader", Stash = "stash", Hideout = "hideout", Quests = "quests", Medic = "medic",
            Insurance = "insurance", Flea = "flea", Guide = "guide", Deploy = "deploy", Character = "character",
            Tablet = "tablet", Upkeep = "upkeep", Manual = "manual", Settings = "settings", Sandbox = "sandbox",
            Technician = "technician", Lodestar = "lodestar", Squad = "squad", Trade = "trade";

        public static void Register(string id, Opener open)
        {
            if (!string.IsNullOrEmpty(id) && open != null) s_Screens[id] = open;
        }

        public static bool Has(string id) => id != null && s_Screens.ContainsKey(id);

        public static void Open(string id, string arg = null)
        {
            if (id != null && s_Screens.TryGetValue(id, out Opener o))
            {
                try { o(arg); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
            else Debug.LogWarning("[CloneZ] No hub screen registered for '" + id + "'.");
        }
    }
}
