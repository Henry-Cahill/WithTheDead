namespace CloneZ
{
    /// <summary>
    /// Who decides what happens. Single-player is its own server, so every check passes there. In a networked
    /// session only the server spawns AI, schedules world events, rolls loot and resolves hits; clients run the
    /// same systems in "follow" mode, applying whatever the server sends. Gameplay code asks these flags instead
    /// of talking to the netcode directly.
    /// </summary>
    public static class NetAuthority
    {
        /// <summary>Indicates whether the current game session is using networked multiplayer.</summary>
        public static bool IsNetworked { get; private set; }
        /// <summary>Indicates whether this instance is the host (or dedicated server) for the current session.</summary>
        public static bool IsHost { get; private set; }
        /// <summary>The client identifier for the local player, used for networking messages.</summary>
        public static ulong LocalClientId { get; private set; }
        private static bool s_Server = true;

        /// <summary>True in single-player and on the host / dedicated server.</summary>
        public static bool IsServer => !IsNetworked || s_Server;

        /// <summary>True on a client that is not also the server.</summary>
        public static bool IsClientOnly => IsNetworked && !s_Server;

        /// <summary>Sets the current authority flags for networked mode, host/client role, and local client ID.
        /// This is called by the bootstrapper when the session type is known.</summary>
        public static void Set(bool networked, bool server, bool host, ulong localClientId)
        {
            IsNetworked = networked;
            s_Server = server;
            IsHost = host;
            LocalClientId = localClientId;
        }

        /// <summary>Resets the authority to single‑player defaults (not networked, server = true, host = false, client ID = 0).</summary>
        public static void Reset() => Set(false, true, false, 0);
    }
}
