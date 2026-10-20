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
        
    public  NetOut F(float v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut D(double v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut B(bool v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut By(byte v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut S(string v) { W.WriteValueSafe(v ?? ""); return this; }
        
    public  NetOut V(Vector3 v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut Q(Quaternion v) { W.WriteValueSafe(v); return this; }
        
    public  NetOut Bytes(byte[] v) { W.WriteValueSafe(v ?? new byte[0]); return this; }
        
    public  void Dispose() => W.Dispose();
    }

    /// <summary>A message being read.</summary>
    
    public  struct NetIn
    {
        
    public  FastBufferReader R;
        
    public  NetIn(FastBufferReader r) { R = r; }
        /// <summary>Reads an int from the buffer.</summary>
        
    public  int I() { R.ReadValueSafe(out int v); return v; }
        /// <summary>Reads a uint from the buffer.</summary>
        
    public  uint U() { R.ReadValueSafe(out uint v); return v; }
        /// <summary>Reads an unsigned long from the buffer.</summary>
        
    public  ulong UL() { R.ReadValueSafe(out ulong v); return v; }
        /// <summary>Reads a float from the buffer.</summary>
        
    public  float F() { R.ReadValueSafe(out float v); return v; }
        /// <summary>Reads a double from the buffer.</summary>
        
    public  double D() { R.ReadValueSafe(out double v); return v; }
        /// <summary>Reads a bool from the buffer.</summary>
        
    public  bool B() { R.ReadValueSafe(out bool v); return v; }
        /// <summary>Reads a byte from the buffer.</summary>
        
    public  byte By() { R.ReadValueSafe(out byte v); return v; }
        /// <summary>Reads a string from the buffer.</summary>
        
    public  string S() { R.ReadValueSafe(out string v); return v; }
        /// <summary>Reads a Vector3 from the buffer.</summary>
        
    public  Vector3 V() { R.ReadValueSafe(out Vector3 v); return v; }
        /// <summary>Reads a Quaternion from the buffer.</summary>
        
    public  Quaternion Q() { R.ReadValueSafe(out Quaternion v); return v; }
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
        
    public  const string Version = "clonez-1";
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
            NetSession s = Create();
            s._utp.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            s._nm.ConnectionApprovalCallback = s.Approve;
            s._nm.OnClientConnectedCallback += s.OnClientConnected;
            s._nm.OnClientDisconnectCallback += s.OnClientDisconnected;
            if (!s._nm.StartHost())
            {
                GameEvents.RaiseToast("Could not host on port " + port + ".");
                Stop();
                return;
            }
            NetAuthority.Set(true, true, true, s._nm.LocalClientId);
            GameSession.Networked = true;
            s.RegisterAll();
            Clients[s._nm.LocalClientId] = LocalInfo(s._nm.LocalClientId);
            Debug.Log("[CloneZ] Hosting on port " + port);
            GameSession.AutoStartHub = true;
            GameFlow.Reload();
        }

        /// <summary>Joins a host; the session's stage (hub or raid, with its seed) arrives once connected.</summary>
        /// <summary>Joins a remote host at the address/port and moves the client into the hub when connected.</summary>
        
    public  static void Join(string address, ushort port)
        {
            NetSession s = Create();
            s._utp.SetConnectionData(string.IsNullOrEmpty(address) ? "127.0.0.1" : address, port);
            CharacterProfile c = ProfileStore.ActiveCharacter;
            s._nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(Version + "|" + (c != null ? c.Name : "Runner") + "|" + (c != null ? c.Faction : Factions.Freehold) + "|" + (c != null ? c.Level : 1));
            s._nm.OnClientConnectedCallback += s.OnClientConnected;
            s._nm.OnClientDisconnectCallback += s.OnClientDisconnected;
            if (!s._nm.StartClient())
            {
                GameEvents.RaiseToast("Could not reach " + address + ":" + port + ".");
                Stop();
                return;
            }
            GameEvents.RaiseToast("Connecting to " + address + ":" + port + "...");
        }

        /// <summary>Stops the current session, shuts down the network and clears all state.</summary>
        
    public  static void Stop()
        {
            if (Instance == null) return;
            if (Instance._nm != null && !Instance._nm.ShutdownInProgress) Instance._nm.Shutdown();
            Destroy(Instance.gameObject);
            Instance = null;
            Clients.Clear();
            s_Handlers.Clear();
            HostRaidZone = null;
            HostRaidSeed = 0;
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
        private void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            string payload = req.Payload != null ? Encoding.UTF8.GetString(req.Payload) : "";
            string[] p = payload.Split('|');
            bool versionOk = p.Length > 0 && p[0] == Version;
            bool room = Clients.Count < SandboxSettings.Current.MaxPlayers;
            res.Approved = versionOk && room;
            res.CreatePlayerObject = false;
            res.Pending = false;
            if (!res.Approved) { res.Reason = !versionOk ? "Different game version." : "The server is full."; return; }
            Clients[req.ClientNetworkId] = new NetClientInfo
            {
                Id = req.ClientNetworkId,
                Name = p.Length > 1 ? p[1] : "Runner",
                Faction = p.Length > 2 ? p[2] : Factions.Freehold,
                Level = p.Length > 3 && int.TryParse(p[3], out int lv) ? lv : 1,
            };
        }

        private void OnClientConnected(ulong id)
        {
            if (_nm.IsServer)
            {
                if (id == _nm.LocalClientId) return;
                // Tell the newcomer where the session is: the rules, whether the host's raid is open, the clock.
                using (var o = new NetOut(1024, 1 << 16))
                {
                    o.S(SandboxSettings.Current.ToJson());
                    WriteHostStage(o);
                    o.F(TimeOfDay.Instance != null ? TimeOfDay.Instance.Hour : 12f);
                    o.S(CentralEconomy.ToJson());
                    SendTo(id, "cz.session", o, NetworkDelivery.ReliableFragmentedSequenced);
                }
                GameEvents.RaiseToast((Clients.TryGetValue(id, out NetClientInfo ci) ? ci.Name : "A runner") + " joined.");
                BroadcastRoster();
            }
            else if (id == _nm.LocalClientId)
            {
                NetAuthority.Set(true, false, false, id);
                GameSession.Networked = true;
                RegisterAll();
            }
        }

        private void OnClientDisconnected(ulong id)
        {
            if (_nm != null && _nm.IsServer)
            {
                if (Clients.TryGetValue(id, out NetClientInfo ci)) GameEvents.RaiseToast(ci.Name + " left.");
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
            Register("cz.joinraid", (s, m) => { if (Clients.TryGetValue(s, out NetClientInfo c)) c.InRaid = true; });
            Register("cz.leftraid", (s, m) => { if (Clients.TryGetValue(s, out NetClientInfo c)) c.InRaid = false; });
            Register("cz.roster", OnRoster);
            foreach (string n in new[] { "cz.session", "cz.hoststage", "cz.joinraid", "cz.leftraid", "cz.roster" })
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
        private bool Transition(GameMode mode, string zone, int lz, RaidKind kind)
        {
            if (!Active) return false;
            if (mode == GameMode.Hub)
            {
                if (_nm.IsServer)
                {
                    foreach (NetClientInfo c in Clients.Values)
                        if (c.Id != _nm.LocalClientId && c.InRaid)
                        {
                            GameEvents.RaiseToast("Waiting for everyone to get out of the Zone before the host can leave.");
                            return true;
                        }
                    GameSession.Mode = GameMode.Hub;
                    BroadcastHostStage();
                    return false;
                }
                using (var o = new NetOut()) ToServer("cz.leftraid", o);
                return false;
            }
            if (_nm.IsServer)
            {
                GameSession.RaidSeed = UnityEngine.Random.Range(1, int.MaxValue);
                GameFlow.PrepareRaid(zone, lz, kind);
                BroadcastHostStage();
                if (Clients.TryGetValue(_nm.LocalClientId, out NetClientInfo me)) me.InRaid = true;
                return false;
            }
            if (string.IsNullOrEmpty(HostRaidZone))
            {
                GameEvents.RaiseToast("The Zone runs on the host's machine: once they go in, you can follow them.");
                return true;
            }
            HubKit.Commit();
            GameSession.RaidSeed = HostRaidSeed;
            GameFlow.PrepareRaid(HostRaidZone, lz, kind);
            GameSession.AutoStartRaid = true;
            GameSession.AutoStartHub = false;
            using (var o = new NetOut()) ToServer("cz.joinraid", o);
            GameFlow.Reload();
            return true;
        }

        private void OnHostStage(ulong sender, NetIn m)
        {
            ReadHostStage(m);
            GameEvents.RaiseToast(HostRaidZone != null ? "The host has gone into " + ZoneCatalog.Get(HostRaidZone).Name + ". You can follow from the door." : "The host is back at the bar.");
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
            for (int i = 0; i < n; i++)
            {
                ulong id = m.UL();
                if (!Clients.TryGetValue(id, out NetClientInfo c)) Clients[id] = c = new NetClientInfo { Id = id };
                c.Name = m.S(); c.Faction = m.S(); c.Squad = m.S(); c.Level = m.I();
                seen.Add(id);
            }
            var gone = new List<ulong>();
            foreach (ulong id in Clients.Keys) if (!seen.Contains(id)) gone.Add(id);
            foreach (ulong id in gone) Clients.Remove(id);
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
