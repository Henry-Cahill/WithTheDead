using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CloneZ
{
    /// <summary>A message being written (a thin wrapper over the netcode's buffer writer).</summary>
    
    public  sealed class NetOut : IDisposable
    {
                
    public  FastBufferWriter W;
        /// <summary>Creates a new NetOut with a pre‑allocated buffer of the given size and maximum capacity.</summary>
        
    public  NetOut(int size = 256, int max = 1 << 16) { W = new FastBufferWriter(size, Allocator.Temp, max); }
        /// <summary>Writes an int to the buffer and returns this for chaining.</summary>
        
    public  NetOut I(int v) { W.WriteValueSafe(v); return this; }
        /// <summary>Writes a uint to the buffer and returns this for chaining.</summary>
        
    public  NetOut U(uint v) { W.WriteValueSafe(v); return this; }
        /// <summary>Writes an unsigned long to the buffer and returns this for chaining.</summary>
        
    public  NetOut UL(ulong v) { W.WriteValueSafe(v); return this; }
        
            /// <summary>Writes a float to the buffer and returns this for chaining.</summary>
        public  NetOut F(float v) { W.WriteValueSafe(v); return this; }
        
                            /// <summary>Writes a double to the buffer and returns this for chaining.</summary>
        public  NetOut D(double v) { W.WriteValueSafe(v); return this; }
        
                    /// <summary>Writes a bool to the buffer and returns this for chaining.</summary>
        public  NetOut B(bool v) { W.WriteValueSafe(v); return this; }
        
            /// <summary>Writes a byte to the buffer and returns this for chaining.</summary>
        public  NetOut By(byte v) { W.WriteValueSafe(v); return this; }
        
            /// <summary>Writes a string to the buffer and returns this for chaining.</summary>
        public  NetOut S(string v) { W.WriteValueSafe(v ?? ""); return this; }
        
            /// <summary>Writes a Vector3 to the buffer and returns this for chaining.</summary>
        public  NetOut V(Vector3 v) { W.WriteValueSafe(v); return this; }
        
            /// <summary>Writes a Quaternion to the buffer and returns this for chaining.</summary>
        public  NetOut Q(Quaternion v) { W.WriteValueSafe(v); return this; }
        
            /// <summary>Writes a byte array to the buffer and returns this for chaining.</summary>
        public  NetOut Bytes(byte[] v) { W.WriteValueSafe(v ?? new byte[0]); return this; }
        
    public  void Dispose() => W.Dispose();
    }

    /// <summary>A message being read.</summary>
    
    public  struct NetIn
    {
        
    public  FastBufferReader R;
        
            /// <summary>Creates a NetIn wrapper around a FastBufferReader.</summary>
        public  NetIn(FastBufferReader r) { R = r; }
        /// <summary>Reads an int from the buffer.</summary>
        
            /// <summary>Reads an int from the buffer.</summary>
        public  int I() { R.ReadValueSafe(out int v); return v; }
        /// <summary>Reads a uint from the buffer.</summary>
        
            /// <summary>Reads a uint from the buffer.</summary>
        public  uint U() { R.ReadValueSafe(out uint v); return v; }
        /// <summary>Reads an unsigned long from the buffer.</summary>
        
            /// <summary>Reads an unsigned long from the buffer.</summary>
        public  ulong UL() { R.ReadValueSafe(out ulong v); return v; }
        /// <summary>Reads a float from the buffer.</summary>
        
            /// <summary>Reads a float from the buffer.</summary>
        public  float F() { R.ReadValueSafe(out float v); return v; }
        /// <summary>Reads a double from the buffer.</summary>
        
            /// <summary>Reads a double from the buffer.</summary>
        public  double D() { R.ReadValueSafe(out double v); return v; }
        /// <summary>Reads a bool from the buffer.</summary>
        
            /// <summary>Reads a bool from the buffer.</summary>
        public  bool B() { R.ReadValueSafe(out bool v); return v; }
        /// <summary>Reads a byte from the buffer.</summary>
        
            /// <summary>Reads a byte from the buffer.</summary>
        public  byte By() { R.ReadValueSafe(out byte v); return v; }
        /// <summary>Reads a string from the buffer.</summary>
        
            /// <summary>Reads a string from the buffer.</summary>
        public  string S() { R.ReadValueSafe(out string v); return v; }
        /// <summary>Reads a Vector3 from the buffer.</summary>
        
            /// <summary>Reads a Vector3 from the buffer.</summary>
        public  Vector3 V() { R.ReadValueSafe(out Vector3 v); return v; }
        /// <summary>Reads a Quaternion from the buffer.</summary>
        
            /// <summary>Reads a Quaternion from the buffer.</summary>
        public  Quaternion Q() { R.ReadValueSafe(out Quaternion v); return v; }
        /// <summary>Reads a byte array from the buffer.</summary>
        
            /// <summary>Reads a byte array from the buffer.</summary>
        public  byte[] Bytes() { R.ReadValueSafe(out byte[] v); return v; }
    }

    /// <summary>What the server knows about each connected runner.</summary>
    
    public  sealed class NetClientInfo
    {
        
    public  ulong Id;
        
    public  string Name = "Runner", Faction = Factions.Freehold, Squad = "";
        
    public  int Level = 1;
        
    public  bool InRaid;
        
    public  bool Alive = true;
        
    public  Vector3 Position;
        
    public  Vector3 Eye;
        
    public  int RadioChannel = -1;
        
    public  float LastShot;
        /// <summary>Server: where the runner connects from ("address:port"; empty when the transport can't say).</summary>
        public string Address = "";
        /// <summary>Server: when the runner was let in, and when their body's last state arrived (unscaled time).</summary>
        public float JoinedAt, StateAt;
    }

    /// <summary>
    /// The network session (Netcode for GameObjects, decided in week one per the doc): one NetworkManager and Unity
    /// Transport built at runtime — no prefab hash tables to keep in sync — and every bit of game state carried as
    /// named messages. The server (the host) is authoritative: it owns the AI, the world clock, world events and
    /// every hit; clients send their own body's state and their shots and render everything else. A session moves
    /// between the hub and the raid together; the raid seed rides along, so every peer builds the same Zone.
    /// </summary>
    
    public  sealed class NetSession : MonoBehaviour
    {
        
    public  const string Version = "clonez-2";
        /// <summary>Singleton instance of the current NetSession, created by Create().</summary>
        
    public  static NetSession Instance { get; private set; }
        /// <summary>Gets whether the session is active (connected as server or client).</summary>
        
    public  static bool Active => Instance != null && Instance._nm != null && (Instance._nm.IsServer || Instance._nm.IsConnectedClient);
        /// <summary>Gets the underlying NetworkManager, or null if the session isn't running.</summary>
        
    public  static NetworkManager NM => Instance != null ? Instance._nm : null;
        /// <summary>Dictionary of all known clients keyed by their network IDs.</summary>
        
    public  static readonly Dictionary<ulong, NetClientInfo> Clients = new Dictionary<ulong, NetClientInfo>();
        /// <summary>Server: a client dropped out of the session (camps and cars they brought leave with them).</summary>
        /// <summary>Invoked on the server when a client disconnects or is removed.</summary>
        
    public  static event Action<ulong> ClientLeft;

        /// <summary>True when the session's server plays nobody (a <see cref="DedicatedServer"/>): known on the server from
        /// the start, and on a client once the session's details arrive. Such a server opens the Zone the first runner
        /// deploys into, where a host has to go in first.</summary>
        public static bool ServerDedicated { get; private set; }
        /// <summary>The build the session's server runs (its <see cref="BuildInfo.Version"/>).</summary>
        public static string ServerBuild { get; private set; } = "";
        /// <summary>A client asked the dedicated server for a raid and is waiting for it to open (then goes straight in).</summary>
        public static bool OpeningRaid => s_Opening && Time.unscaledTime - s_OpeningAt < OpenRaidPatience;
        /// <summary>Server (dedicated): when the open raid's world was built (unscaled time); -1 while none is open or it's
        /// still being built.</summary>
        public static float ServedRaidSince => s_RaidSince;

        /// <summary>A runner's name for a toast. A dedicated server's toasts are its log, where runners who share a name need
        /// telling apart: there it's <see cref="DedicatedServer.Who"/> ("Runner #3").</summary>
        public static string ToastName(ulong id) => DedicatedServer.Requested ? DedicatedServer.Who(id)
            : Clients.TryGetValue(id, out NetClientInfo c) ? c.Name : "A runner";

        private const float OpenRaidPatience = 30f;
        // Seconds an open raid on a dedicated server waits with nobody in it before it closes: time for the runner who
        // opened it to load in, or for someone who dropped out to come back. A runner who has just set off for it counts
        // as in it for a while longer, as a slow machine builds the map (their last state from the bar may even arrive
        // after they've left it, and say they're not in the raid).
        private const float EmptyRaidGrace = 90f, JoiningGrace = 240f;
        private static bool s_Opening;
        private static float s_OpeningAt;
        private static int s_OpeningLz;
        private static RaidKind s_OpeningKind;
        private static float s_RaidSince = -1f, s_EmptySince = -1f, s_JoinedAt = -1000f;

        private NetworkManager _nm;
        private UnityTransport _utp;
        private static readonly Dictionary<string, Action<ulong, NetIn>> s_Handlers = new Dictionary<string, Action<ulong, NetIn>>();
        private static readonly List<ulong> s_Targets = new List<ulong>();

        #region Starting and stopping
        private static NetSession Create()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[NetSession]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<NetSession>();
            Instance._nm = go.AddComponent<NetworkManager>();
            Instance._utp = go.AddComponent<UnityTransport>();
            // A world is built on the main thread, for seconds on end; the packets that arrive meanwhile wait in this queue.
            Instance._utp.MaxPacketQueueSize = 1024;
            Instance._nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Instance._utp,
                ConnectionApproval = true,
                EnableSceneManagement = false,
                TickRate = 30,
            };
            return Instance;
        }

        /// <summary>Hosts a session on <paramref name="port"/> and walks the host into the hub.</summary>
        /// <summary>Hosts a new game session on the given port and moves the host into the hub.</summary>
        
    public  static void Host(ushort port)
        {
            // Create (or retrieve) the singleton NetSession and its NetworkManager/Transport.
            NetSession s = Create();
            // Listen on all interfaces; the third argument is the advertised address for clients.
            s._utp.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            // Register server‑side callbacks for approval and client connection events.
            s._nm.ConnectionApprovalCallback = s.Approve;
            s._nm.OnClientConnectedCallback += s.OnClientConnected;
            s._nm.OnClientDisconnectCallback += s.OnClientDisconnected;
            // Attempt to start hosting; on failure report and clean up.
            if (!s._nm.StartHost())
            {
                GameEvents.RaiseToast("Could not host on port " + port + ".");
                Stop();
                return;
            }
            // Grant full authority to the host client (owns bodies, input, AI).
            NetAuthority.Set(true, true, true, s._nm.LocalClientId);
            GameSession.Networked = true;
            // Register all network message handlers now that the session is live.
            s.RegisterAll();
            // Record host client info for the roster.
            Clients[s._nm.LocalClientId] = LocalInfo(s._nm.LocalClientId);
            Debug.Log("[CloneZ] Hosting on port " + port);
            GameSession.AutoStartHub = true;
            GameFlow.Reload();
        }

        /// <summary>Serves a session on <paramref name="port"/> with nobody playing here: the <see cref="DedicatedServer"/>.
        /// It stays on the stage it's on; the bootstrapper builds what that stage needs.</summary>
        public static void Serve(ushort port)
        {
            NetSession s = Create();
            s._utp.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            s._nm.ConnectionApprovalCallback = s.Approve;
            s._nm.OnClientConnectedCallback += s.OnClientConnected;
            s._nm.OnClientDisconnectCallback += s.OnClientDisconnected;
            if (!s._nm.StartServer())
            {
                Stop();
                return;
            }
            ServerDedicated = true;
            ServerBuild = BuildInfo.Version;
            // The server owns the AI, the clock and every hit, and has no body of its own (no entry in the roster).
            NetAuthority.Set(true, true, true, s._nm.LocalClientId);
            GameSession.Networked = true;
            s.RegisterAll();
            WorldHooks.RaidStarted -= OnServedRaidBuilt;
            WorldHooks.RaidStarted += OnServedRaidBuilt;
            Debug.Log("[CloneZ] Serving on port " + port);
        }

        /// <summary>Joins a host; the session's stage (hub or raid, with its seed) arrives once connected.</summary>
        /// <summary>Joins a remote host at the address/port and moves the client into the hub when connected.</summary>
        
    public  static void Join(string address, ushort port)
        {
            // Ensure a singleton NetSession exists for the client side.
            NetSession s = Create();
            // Use loopback if no address supplied; set the remote host address/port.
            s._utp.SetConnectionData(Resolve(address), port);
            // Encode version, player name, faction, level and this build (see Approve) into the connection payload.
            CharacterProfile c = ProfileStore.ActiveCharacter;
            s._nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(Version + "|" + (c != null ? c.Name : "Runner") + "|" + (c != null ? c.Faction : Factions.Freehold) + "|" + (c != null ? c.Level : 1)
                + "|" + BuildInfo.Version + "|" + (Application.isEditor ? "editor" : "player"));
            // Register callbacks for connection events.
            s._nm.OnClientConnectedCallback += s.OnClientConnected;
            s._nm.OnClientDisconnectCallback += s.OnClientDisconnected;
            // Attempt to start the client; on failure report and clean up.
            if (!s._nm.StartClient())
            {
                GameEvents.RaiseToast("Could not reach " + address + ":" + port + ".");
                Stop();
                return;
            }
            GameEvents.RaiseToast("Connecting to " + address + ":" + port + "...");
        }

        // The transport takes an IP address only: a name (the in-house server's dynamic-DNS name, say) is looked up here.
        private static string Resolve(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return "127.0.0.1";
            address = address.Trim();
            if (System.Net.IPAddress.TryParse(address, out _)) return address;
            try
            {
                foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostAddresses(address))
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ip.ToString();
            }
            catch (Exception e) { Debug.LogWarning("[CloneZ] Couldn't look up " + address + ": " + e.Message); }
            return address;
        }

        /// <summary>Stops the current session, shuts down the network and clears all state.</summary>
        
    public  static void Stop()
        {
            // If no session exists, nothing to stop.
            if (Instance == null) return;
            // Shut down the NetworkManager gracefully if it hasn't already begun shutdown.
            if (Instance._nm != null && !Instance._nm.ShutdownInProgress) Instance._nm.Shutdown();
            // Destroy the singleton GameObject and clear the static reference.
            Destroy(Instance.gameObject);
            Instance = null;
            // Clear all session‑wide collections and state.
            Clients.Clear();
            s_Handlers.Clear();
            HostRaidZone = null;
            HostRaidSeed = 0;
            ServerDedicated = false;
            ServerBuild = "";
            s_Opening = false;
            s_RaidSince = s_EmptySince = -1f;
            s_JoinedAt = -1000f;
            WorldHooks.RaidStarted -= OnServedRaidBuilt;
            NetAuthority.Reset();
            GameSession.Networked = false;
            GameSession.RaidSeed = 0;
            CombatResolver.ShotRouter = null;
            CombatResolver.Rewind = null;
            GameFlow.NetworkTransition = null;
            NetClock.ViewTimeSource = null;
        }

        private static NetClientInfo LocalInfo(ulong id)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            return new NetClientInfo { Id = id, Name = c != null ? c.Name : "Host", Faction = c != null ? c.Faction : Factions.Freehold, Level = c != null ? c.Level : 1 };
        }

        // The server lets runners in while there's room and the versions agree.
        /// <summary>Server‑side approval of a client connection: checks version compatibility and player‑count limits.</summary>
        private void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            // Decode the payload; empty payload means no client info.
            string payload = req.Payload != null ? Encoding.UTF8.GetString(req.Payload) : "";
            string[] p = payload.Split('|');
            // Verify that the client runs the same game version.
            bool versionOk = p.Length > 0 && p[0] == Version;
            // And the same build: the netcode's messages change from one build to the next. The editor is the developer's,
            // and joins (or hosts) whatever's running.
            string build = p.Length > 4 ? p[4] : "an older one";
            bool sameBuild = build == BuildInfo.Version || (p.Length > 5 && p[5] == "editor") || Application.isEditor;
            // Enforce player‑count limit defined in sandbox settings.
            bool room = Clients.Count < SandboxSettings.Current.MaxPlayers;
            // Approve only if the version, the build and capacity checks pass.
            res.Approved = versionOk && sameBuild && room;
            // The server creates player objects manually; no automatic prefab.
            res.CreatePlayerObject = false;
            res.Pending = false;
            // If rejected, give a short reason for the client.
            string address = AddressOf(req.ClientNetworkId);
            if (!res.Approved)
            {
                res.Reason = !versionOk ? "Different game version."
                    : !sameBuild ? "The server runs build " + BuildInfo.Version + " and you have " + build + ". Update WithTheDead, then join again."
                    : "The server is full.";
                if (ServerDedicated) Debug.Log("[Server] Turned away " + (p.Length > 1 ? p[1] : "a runner") + " from " + address + ": " + res.Reason);
                return;
            }
            // Record the new client’s basic info for the roster.
            Clients[req.ClientNetworkId] = new NetClientInfo
            {
                Id = req.ClientNetworkId,
                Name = p.Length > 1 ? p[1] : "Runner",
                Faction = p.Length > 2 ? p[2] : Factions.Freehold,
                Level = p.Length > 3 && int.TryParse(p[3], out int lv) ? lv : 1,
                Address = address,
                JoinedAt = Time.unscaledTime,
            };
        }

        // Where a client connects from, for the server's log.
        private string AddressOf(ulong id)
        {
            var endpoint = _utp != null ? _utp.GetEndpoint(id) : default;
            return endpoint.IsValid ? endpoint.Address : "an unknown address";
        }

        /// <summary>Handles a newly connected client, sending initial session data if this is the server or establishing authority on the local client.</summary>
        /// <summary>Handles a newly connected client, sending initial session data if this is the server or establishing authority on the local client.</summary>
        private void OnClientConnected(ulong id)
        {
            // Server side: inform the new client about the current game settings and host stage.
            if (_nm.IsServer)
            {
                // Ignore the host itself – it already has the data.
                if (id == _nm.LocalClientId) return;
                // Tell the newcomer where the session is: the rules, whether the host's raid is open, the clock.
                using (var o = new NetOut(1024, 1 << 16))
                {
                    o.S(SandboxSettings.Current.ToJson());
                    WriteHostStage(o);
                    o.F(TimeOfDay.Instance != null ? TimeOfDay.Instance.Hour : 12f);
                    o.S(CentralEconomy.ToJson());
                    o.B(ServerDedicated).S(BuildInfo.Version);
                    // Send as a reliable, possibly fragmented message.
                    SendTo(id, "cz.session", o, NetworkDelivery.ReliableFragmentedSequenced);
                }
                // Notify everyone that a runner joined and broadcast the updated roster. A dedicated server's log also
                // says from where, and how many are on.
                GameEvents.RaiseToast(!DedicatedServer.Requested ? ToastName(id) + " joined."
                    : ToastName(id) + " joined from " + (Clients.TryGetValue(id, out NetClientInfo ci) ? ci.Address : "an unknown address")
                      + "; " + Clients.Count + " connected.");
                BroadcastRoster();
            }
            // Client side: this is the local player joining a remote host.
            else if (id == _nm.LocalClientId)
            {
                // Set authority for the local client (only controls its own body).
                NetAuthority.Set(true, false, false, id);
                GameSession.Networked = true;
                // Register all message handlers now that we are connected.
                RegisterAll();
            }
        }

        /// <summary>Handles cleanup when a client disconnects: removes them from the session and notifies remaining participants.</summary>
        private void OnClientDisconnected(ulong id)
        {
            // Server side: clean up client state and inform remaining players.
            if (_nm != null && _nm.IsServer)
            {
                // A dedicated server's log also says how long they were on, and how many are left.
                if (Clients.TryGetValue(id, out NetClientInfo ci))
                    GameEvents.RaiseToast(!DedicatedServer.Requested ? ci.Name + " left."
                        : ToastName(id) + " left after " + DedicatedServer.Duration(Time.unscaledTime - ci.JoinedAt) + "; " + (Clients.Count - 1) + " connected.");
                Clients.Remove(id);
                NetPlayers.RemoveProxy(id);
                BroadcastRoster();
                try { ClientLeft?.Invoke(id); }
                catch (Exception e) { Debug.LogException(e); }
                return;
            }
            // We were dropped (or refused).
            string why = _nm != null && !string.IsNullOrEmpty(_nm.DisconnectReason) ? _nm.DisconnectReason : "Disconnected from the server.";
            Stop();
            GameEvents.RaiseToast(why);
            GameFlow.ToMainMenu();
        }
        #endregion

        #region Messaging
        /// <summary>Registers a handler for a named message.</summary>
        
    public  static void Register(string name, Action<ulong, NetIn> handler) => s_Handlers[name] = handler;

        private void RegisterAll()
        {
            CustomMessagingManager cmm = _nm.CustomMessagingManager;
            foreach (NetModuleHooks.Registration r in NetModuleHooks.All) r(this);
            foreach (KeyValuePair<string, Action<ulong, NetIn>> kv in s_Handlers)
            {
                Action<ulong, NetIn> h = kv.Value;
                string name = kv.Key;
                cmm.RegisterNamedMessageHandler(name, (sender, reader) =>
                {
                    try { h(sender, new NetIn(reader)); }
                    catch (Exception e) { Debug.LogWarning("[CloneZ] Bad '" + name + "' message from " + sender + ": " + e.Message); }
                });
            }
            Register("cz.session", OnSession);
            Register("cz.hoststage", OnHostStage);
            // A runner setting off for the raid will next be seen in it, far from the bar: not a move to flag (NetPlayers).
            Register("cz.joinraid", (s, m) => { if (Clients.TryGetValue(s, out NetClientInfo c)) { c.InRaid = true; c.Position = Vector3.zero; } s_JoinedAt = Time.unscaledTime; });
            Register("cz.leftraid", (s, m) => { if (Clients.TryGetValue(s, out NetClientInfo c)) { c.InRaid = false; c.Position = Vector3.zero; } });
            Register("cz.roster", OnRoster);
            Register("cz.openraid", OnOpenRaid);
            // The built-ins above are added after the loop, so they need their own netcode registration.
            foreach (string n in new[] { "cz.session", "cz.hoststage", "cz.joinraid", "cz.leftraid", "cz.roster", "cz.openraid" })
            {
                Action<ulong, NetIn> h = s_Handlers[n];
                string name = n;
                cmm.RegisterNamedMessageHandler(name, (sender, reader) =>
                {
                    try { h(sender, new NetIn(reader)); }
                    catch (Exception e) { Debug.LogWarning("[CloneZ] Bad '" + name + "' message: " + e.Message); }
                });
            }
            GameFlow.NetworkTransition = Transition;
            NetClock.ViewTimeSource = () => _nm != null ? _nm.ServerTime.Time - (_nm.IsServer ? 0d : NetPlayers.InterpolationDelay) : 0d;
        }

        /// <summary>Sends a message to a specific client (or the host if the client is local).</summary>
        
    public  static void SendTo(ulong client, string name, NetOut o, NetworkDelivery d = NetworkDelivery.ReliableSequenced)
        {
            NetworkManager nm = NM;
            if (nm == null) return;
            if (client == nm.LocalClientId) { Dispatch(name, client, o); return; }
            nm.CustomMessagingManager.SendNamedMessage(name, client, o.W, d);
        }

        /// <summary>Sends a message from the client to the server.</summary>
        
    public  static void ToServer(string name, NetOut o, NetworkDelivery d = NetworkDelivery.ReliableSequenced)
        {
            NetworkManager nm = NM;
            if (nm == null) return;
            if (nm.IsServer) { Dispatch(name, nm.LocalClientId, o); return; }
            nm.CustomMessagingManager.SendNamedMessage(name, NetworkManager.ServerClientId, o.W, d);
        }

        /// <summary>Server: sends to every client except <paramref name="except"/> (and never loops back to the host's own handler unless asked).</summary>
        /// <summary>Sends a message from the server to all clients (optionally excluding one).</summary>
        
    public  static void ToClients(string name, NetOut o, NetworkDelivery d = NetworkDelivery.ReliableSequenced, ulong except = ulong.MaxValue, bool includeHost = false)
        {
            NetworkManager nm = NM;
            if (nm == null || !nm.IsServer) return;
            s_Targets.Clear();
            foreach (ulong id in nm.ConnectedClientsIds)
                if (id != except && id != nm.LocalClientId) s_Targets.Add(id);
            if (s_Targets.Count > 0) nm.CustomMessagingManager.SendNamedMessage(name, s_Targets, o.W, d);
            if (includeHost && except != nm.LocalClientId) Dispatch(name, nm.LocalClientId, o);
        }

        // The host talking to itself: run the handler on a reader over what was written.
        private static void Dispatch(string name, ulong sender, NetOut o)
        {
            if (!s_Handlers.TryGetValue(name, out Action<ulong, NetIn> h)) return;
            using (var reader = new FastBufferReader(o.W, Allocator.Temp))
            {
                try { h(sender, new NetIn(reader)); }
                catch (Exception e) { Debug.LogWarning("[CloneZ] Local '" + name + "' failed: " + e.Message); }
            }
        }
        #endregion

        #region Session and stage
        /// <summary>The raid the host is running (null while the host is in the hub) — the one clients deploy into.</summary>
        /// <summary>Gets or sets the host's raid zone name (null if none).</summary>
        
    public  static string HostRaidZone { get; private set; }
        /// <summary>Gets or sets the host's raid seed (0 if none).</summary>
        
    public  static int HostRaidSeed { get; private set; }

        // A newcomer learns the rules, the host's clock and whether a raid is running, and walks into their own hub.
        private void OnSession(ulong sender, NetIn m)
        {
            SandboxSettings s = SandboxSettings.FromJson(m.S());
            if (s != null) SandboxSettings.Adopt(s);
            ReadHostStage(m);
            NetWorld.PendingHour = m.F();
            CentralEconomy.Adopt(m.S());
            ServerDedicated = m.B();
            ServerBuild = m.S();
            if (ServerBuild != BuildInfo.Version) Debug.LogWarning("[CloneZ] The server runs build " + ServerBuild + "; this is " + BuildInfo.Version + ".");
            GameSession.AutoStartHub = true;
            GameSession.AutoStartRaid = false;
            GameFlow.Reload();
        }

        /// <summary>Writes the current host stage (raid flag, zone, seed) to the buffer.</summary>
private static void WriteHostStage(NetOut o)
        {
            bool raid = GameSession.IsExtraction;
            o.B(raid).S(raid ? GameSession.ZoneId : "").I(raid ? GameSession.RaidSeed : 0);
        }

        /// <summary>Reads the host stage (raid flag, zone, seed) from the buffer and stores them.</summary>
private static void ReadHostStage(NetIn m)
        {
            bool raid = m.B();
            string zone = m.S();
            int seed = m.I();
            HostRaidZone = raid ? zone : null;
            HostRaidSeed = seed;
        }

        private void BroadcastHostStage()
        {
            using (var o = new NetOut(64)) { WriteHostStage(o); ToClients("cz.hoststage", o); }
        }

        // The raid runs on the host's machine (it owns the Zone's AI). The host going in opens it — and nobody is
        // dragged along; anyone in their hub can then deploy into it (as their runner, a scav, or a hunter invading
        // it), and the host can't leave until they're all out.
        /// <summary>Manages the transition between hub and raid modes, handling host authority, seed generation and client notifications.</summary>
        private bool Transition(GameMode mode, string zone, int lz, RaidKind kind)
        {
            // No active session – cannot transition.
            if (!Active) return false;
            // Transitioning to the hub (menu) from a raid.
            if (mode == GameMode.Hub)
            {
                if (_nm.IsServer)
                {
                    // Server must ensure no other players are still in a raid before leaving.
                    foreach (NetClientInfo c in Clients.Values)
                        if (c.Id != _nm.LocalClientId && c.InRaid)
                        {
                            GameEvents.RaiseToast("Waiting for everyone to get out of the Zone before the host can leave.");
                            // stay in raid until all leave.
                            return true;
                        }
                    // All clear – switch host to hub mode and broadcast the change.
                    GameSession.Mode = GameMode.Hub;
                    BroadcastHostStage();
                    return false;
                }
                // Client informs the server it is leaving the raid.
                using (var o = new NetOut()) ToServer("cz.leftraid", o);
                return false;
            }
            // Transitioning into a raid (non‑hub mode).
            if (_nm.IsServer)
            {
                // Host picks a random seed for the raid to ensure deterministic generation.
                GameSession.RaidSeed = UnityEngine.Random.Range(1, int.MaxValue);
                GameFlow.PrepareRaid(zone, lz, kind);
                BroadcastHostStage();
                if (Clients.TryGetValue(_nm.LocalClientId, out NetClientInfo me)) me.InRaid = true;
                return false;
            }
            // Client side – ensure we have host stage info.
            if (string.IsNullOrEmpty(HostRaidZone))
            {
                // A dedicated server opens the Zone this runner picked, for everyone; in they go once it's open (OnHostStage).
                if (ServerDedicated)
                {
                    if (OpeningRaid) return true;
                    s_Opening = true;
                    s_OpeningAt = Time.unscaledTime;
                    s_OpeningLz = lz;
                    s_OpeningKind = kind;
                    using (var o = new NetOut(64)) { o.S(zone ?? "").By((byte)kind); ToServer("cz.openraid", o); }
                    GameEvents.RaiseToast("Opening " + ZoneCatalog.Get(zone).Name + " on the server...");
                    return true;
                }
                GameEvents.RaiseToast("The Zone runs on the host's machine: once they go in, you can follow them.");
                return true;
            }
            Follow(lz, kind);
            return true;
        }

        // Into the raid under way on the server, built from its seed, wherever this runner chose to come in.
        private static void Follow(int lz, RaidKind kind)
        {
            s_Opening = false;
            HubKit.Commit();
            GameSession.RaidSeed = HostRaidSeed;
            GameFlow.PrepareRaid(HostRaidZone, lz, kind);
            GameSession.AutoStartRaid = true;
            GameSession.AutoStartHub = false;
            using (var o = new NetOut()) ToServer("cz.joinraid", o);
            GameFlow.Reload();
        }

        private void OnHostStage(ulong sender, NetIn m)
        {
            ReadHostStage(m);
            // This runner asked the dedicated server for the raid: it's open, so in they go.
            if (HostRaidZone != null && OpeningRaid && GameSession.IsHub)
            {
                Follow(s_OpeningLz, s_OpeningKind);
                return;
            }
            if (ServerDedicated)
                GameEvents.RaiseToast(HostRaidZone != null ? "A raid into " + ZoneCatalog.Get(HostRaidZone).Name + " is open on the server. You can join it from the door." : "The server's raid is over.");
            else
                GameEvents.RaiseToast(HostRaidZone != null ? "The host has gone into " + ZoneCatalog.Get(HostRaidZone).Name + ". You can follow from the door." : "The host is back at the bar.");
        }

        // Server (dedicated): a runner deploying opens their Zone for everyone. One raid runs at a time, so while one is
        // open they're pointed at it instead.
        private void OnOpenRaid(ulong sender, NetIn m)
        {
            string zone = m.S();
            var kind = (RaidKind)m.By();
            if (_nm == null || !_nm.IsServer || !ServerDedicated) return;
            if (GameSession.IsExtraction)
            {
                using (var o = new NetOut(64)) { WriteHostStage(o); SendTo(sender, "cz.hoststage", o); }
                return;
            }
            ZoneDef z = ZoneCatalog.Get(zone);
            GameSession.RaidSeed = UnityEngine.Random.Range(1, int.MaxValue);
            // The raid's own rules are an operative raid's (or the open Zone's, with no dawn deadline); how each runner
            // comes in (their runner, a scav, a hunter) is theirs.
            GameFlow.PrepareRaid(z.Id, -1, kind == RaidKind.OpenZone ? RaidKind.OpenZone : RaidKind.Operative);
            s_RaidSince = s_EmptySince = -1f;
            BroadcastHostStage();
            GameEvents.RaiseToast(ToastName(sender) + " opened " + z.Name + " (seed " + GameSession.RaidSeed + ").");
            GameSession.AutoStartRaid = true;
            GameSession.AutoStartHub = false;
            GameFlow.Reload();
        }

        // Server (dedicated): the open raid's world is built; from now on it closes once it has stood empty a while.
        private static void OnServedRaidBuilt(Transform world, RaidMapInfo map, ZoneDef zone)
        {
            if (ServerDedicated) s_RaidSince = Time.unscaledTime;
        }

        /// <summary>Server (dedicated): ends the open raid and goes back to waiting with no world built.</summary>
        public static void CloseServedRaid(string why)
        {
            if (Instance == null || !ServerDedicated || !GameSession.IsExtraction) return;
            Debug.Log("[Server] Closing " + ZoneCatalog.Current.Name + ": " + why + ".");
            s_RaidSince = s_EmptySince = -1f;
            s_JoinedAt = -1000f;
            GameSession.Mode = GameMode.Hub;
            Instance.BroadcastHostStage();
            GameSession.AutoStartRaid = false;
            GameSession.AutoStartHub = false;
            GameFlow.Reload();
        }

        private void Update()
        {
            if (!ServerDedicated || _nm == null || !_nm.IsServer) return;
            DedicatedServer.Tick();
            // A dedicated server's raid closes once nobody has been in it (or on their way in) for a while.
            if (!GameSession.IsExtraction || s_RaidSince < 0f) return;
            float now = Time.unscaledTime;
            bool anyone = now - s_JoinedAt < JoiningGrace;
            foreach (NetClientInfo c in Clients.Values) anyone |= c.InRaid;
            if (anyone)
            {
                s_EmptySince = -1f;
                return;
            }
            if (s_EmptySince < 0f) s_EmptySince = Mathf.Max(now, s_RaidSince);
            if (now - s_EmptySince >= EmptyRaidGrace) CloseServedRaid("nobody has been in it for " + Mathf.RoundToInt(EmptyRaidGrace) + " s");
        }

        private void BroadcastRoster()
        {
            using (var o = new NetOut(512))
            {
                o.I(Clients.Count);
                foreach (NetClientInfo c in Clients.Values) o.UL(c.Id).S(c.Name).S(c.Faction).S(c.Squad).I(c.Level);
                ToClients("cz.roster", o, NetworkDelivery.ReliableSequenced, ulong.MaxValue, includeHost: false);
            }
        }

        /// <summary>Pushes the latest roster to all clients.</summary>
        
    public  static void PushRoster() { if (Instance != null && Instance._nm != null && Instance._nm.IsServer) Instance.BroadcastRoster(); }

        private void OnRoster(ulong sender, NetIn m)
        {
            int n = m.I();
            var seen = new HashSet<ulong>();
            // Read each client entry from the roster payload.
            for (int i = 0; i < n; i++)
            {
                ulong id = m.UL();
                if (!Clients.TryGetValue(id, out NetClientInfo c)) Clients[id] = c = new NetClientInfo { Id = id };
                c.Name = m.S(); c.Faction = m.S(); c.Squad = m.S(); c.Level = m.I();
                seen.Add(id);
            }
            // Find any clients that were present before but not in this update.
            var gone = new List<ulong>();
            foreach (ulong id in Clients.Keys) if (!seen.Contains(id)) gone.Add(id);
            // Remove missing clients from the roster.
            foreach (ulong id in gone) Clients.Remove(id);
            // Refresh squad assignments after roster changes.
            NetSocial.ApplySquads();
        }
        #endregion

        private void OnDestroy() { if (Instance == this) Instance = null; }
    }

    /// <summary>Lets the net modules (players, world, social) register their messages when a session starts.</summary>
    
    public  static class NetModuleHooks
    {
        
    public  delegate void Registration(NetSession session);
        
    public  static readonly List<Registration> All = new List<Registration>();
    }

    /// <summary>Wires the menu's host/join buttons and the per-session net modules.</summary>
    
    public  sealed class NetModule : IGameModule
    {
        
    public  int Order => 90;

        
    public  void Init()
        {
            MainMenuController.HostAction = port => NetSession.Host(port);
            MainMenuController.JoinAction = (addr, port) => NetSession.Join(addr, port);
            NetModuleHooks.All.Add(s => NetPlayers.Register());
            NetModuleHooks.All.Add(s => NetWorld.Register());
            NetModuleHooks.All.Add(s => NetSocial.Register());
        }
    }
}
