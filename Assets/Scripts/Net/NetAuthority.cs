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
        public static bool IsNetworked { get; private set; }
        public static bool IsHost { get; private set; }
        public static ulong LocalClientId { get; private set; }
        private static bool s_Server = true;

        /// <summary>True in single-player and on the host / dedicated server.</summary>
        public static bool IsServer => !IsNetworked || s_Server;

        /// <summary>True on a client that is not also the server.</summary>
        public static bool IsClientOnly => IsNetworked && !s_Server;

        public static void Set(bool networked, bool server, bool host, ulong localClientId)
        {
            IsNetworked = networked;
            s_Server = server;
            IsHost = host;
            LocalClientId = localClientId;
        }

        public static void Reset() => Set(false, true, false, 0);
    }
}
