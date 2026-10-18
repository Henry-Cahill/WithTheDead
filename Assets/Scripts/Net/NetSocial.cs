using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>Item lists on the wire (every field a stack carries: wear, age, repairs, extras).</summary>
    public static class NetCodec
    {
        public static void Write(NetOut o, List<StackSave> items)
        {
            o.I(items != null ? items.Count : 0);
            if (items == null) return;
            foreach (StackSave s in items) o.S(s.Id).I(s.Count).F(s.SpawnHours).I(s.Condition).I(s.TimesRepaired).S(s.Extra ?? "");
        }

        public static List<StackSave> ReadSaves(NetIn m)
        {
            int n = m.I();
            if (n < 0 || n > 1024) throw new InvalidOperationException("bad item count " + n);
            var list = new List<StackSave>(n);
            for (int i = 0; i < n; i++)
                list.Add(new StackSave { Id = m.S(), Count = m.I(), SpawnHours = m.F(), Condition = m.I(), TimesRepaired = m.I(), Extra = m.S() });
            return list;
        }
    }

    /// <summary>
    /// Runners together (the doc's DayZ / Gray Zone social layer): proximity voice and the walkie-talkie, squads
    /// (who you can't shoot, whose kills and finds count for your jobs too), trading hand to hand, and what one
    /// runner can do to another — cuff someone with their hands up, make them drop their gun, feed them, bandage
    /// them, give them blood, shake them awake. A runner who dies leaves a body with everything they carried
    /// (bar the secure pouch) for the living.
    /// </summary>
    public static class NetSocial
    {
        public const float VoiceRange = 30f;
        public const int Channels = 8;
        public const int MaxSquad = 4;

        /// <summary>The walkie's channel, 1..8 (0 = switched off).</summary>
        public static int RadioChannel = 1;
        /// <summary>Runners heard talking, with the time they were last heard (the HUD lists them).</summary>
        public static readonly Dictionary<ulong, float> Speaking = new Dictionary<ulong, float>();
        public static ulong InviteFrom = ulong.MaxValue;
        public static string InviteSquad;

        private static bool s_Hooked, s_Reported;
        private static ulong s_LastAttacker = ulong.MaxValue;
        private static uint s_LastAttackerNet;
        private static string s_LastWeapon = "";
        private static float s_RadioAt, s_SquadAt;
        private static int s_SentChannel = -2;
        private static readonly Dictionary<string, Action<ulong, NetIn>> s_Peer = new Dictionary<string, Action<ulong, NetIn>>();

        /// <summary>A runner's actor id on every peer (the AI's ids never set the top bit).</summary>
        public static uint PlayerNetId(ulong client) => 0x80000000u | (uint)(client & 0x7FFFFFFF);

        public static bool HasWalkie
        {
            get
            {
                PlayerInventory inv = HubKit.Carried;
                return inv != null && inv.Has("radio_walkie");
            }
        }

        public static string NameOf(ulong client) => NetSession.Clients.TryGetValue(client, out NetClientInfo c) ? c.Name : "A runner";

        public static string MySquad
        {
            get
            {
                NetworkManager nm = NetSession.NM;
                return nm != null && NetSession.Clients.TryGetValue(nm.LocalClientId, out NetClientInfo c) ? c.Squad : "";
            }
        }

        public static bool SameSquad(ulong other)
        {
            string mine = MySquad;
            return !string.IsNullOrEmpty(mine) && NetSession.Clients.TryGetValue(other, out NetClientInfo c) && c.Squad == mine;
        }

        #region Registration and the per-frame work
        internal static void Register()
        {
            NetSession.Register("cz.relay", OnRelay);
            NetSession.Register("cz.p2p", OnP2P);
            NetSession.Register("cz.voice", OnVoiceUp);
            NetSession.Register("cz.voicex", OnVoiceDown);
            NetSession.Register("cz.radio", (s, m) =>
            {
                int ch = m.I();
                bool has = m.B();
                if (NetSession.NM.IsServer && NetSession.Clients.TryGetValue(s, out NetClientInfo c)) c.RadioChannel = has ? ch : -1;
            });
            NetSession.Register("cz.squad", OnSquad);
            NetSession.Register("cz.qshare", OnQuestShare);
            NetSession.Register("cz.qsharex", (s, m) => QuestBook.Apply(m.S(), m.F()));

            Peer("squad.invite", (from, m) =>
            {
                InviteFrom = from;
                InviteSquad = m.S();
                GameEvents.RaiseToast(NameOf(from) + " invites you into their squad. [" + GameInput.KeyName(GameAction.Squad) + "] to answer.");
                Sfx.PlayUI(SfxIds.UiClick);
            });
            PlayerTrade.Register();
            PeerActions.Register();
            s_SentChannel = -2;

            if (s_Hooked) return;
            s_Hooked = true;
            GameEvents.QuestProgress += ShareQuest;
            GameEvents.RaidEnded += (o, exit) => { if (o == RaidOutcome.Killed) ReportDeath(s_LastAttacker, s_LastAttackerNet, s_LastWeapon); };
            WorldHooks.RaidStarted += (w, map, z) => { s_Reported = false; s_LastAttacker = ulong.MaxValue; s_LastAttackerNet = 0; s_LastWeapon = ""; };
        }

        internal static void Tick()
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null) return;
            Actor me = Actor.LocalPlayer;
            if (me != null && me.NetId == 0) me.NetId = PlayerNetId(nm.LocalClientId);
            if (me != null) me.OwnerClientId = nm.LocalClientId;

            int ch = HasWalkie ? RadioChannel : -1;
            if (ch != s_SentChannel || Time.time >= s_RadioAt)
            {
                s_SentChannel = ch;
                s_RadioAt = Time.time + 5f;
                using (var o = new NetOut(16)) { o.I(RadioChannel).B(ch > 0); NetSession.ToServer("cz.radio", o); }
            }
            if (Time.time >= s_SquadAt) { s_SquadAt = Time.time + 1f; ApplySquads(); }
            VoiceCapture.Ensure();
        }
        #endregion

        #region Peer messages (relayed by the server)
        public static void Peer(string op, Action<ulong, NetIn> handler) => s_Peer[op] = handler;

        /// <summary>Sends <paramref name="op"/> to one other runner through the server.</summary>
        public static void SendPeer(ulong target, string op, Action<NetOut> write = null)
        {
            if (!NetSession.Active) return;
            byte[] payload;
            using (var inner = new NetOut(256, 1 << 15))
            {
                write?.Invoke(inner);
                payload = inner.W.ToArray();
            }
            using (var o = new NetOut(payload.Length + 64, 1 << 16))
            {
                o.UL(target).S(op).Bytes(payload);
                NetSession.ToServer("cz.relay", o, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private static void OnRelay(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            ulong target = m.UL();
            string op = m.S();
            byte[] payload = m.Bytes();
            if (!NetSession.Clients.ContainsKey(target)) return;
            using (var o = new NetOut(payload.Length + 64, 1 << 16))
            {
                o.UL(sender).S(op).Bytes(payload);
                NetSession.SendTo(target, "cz.p2p", o, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private static void OnP2P(ulong server, NetIn m)
        {
            ulong from = m.UL();
            string op = m.S();
            byte[] payload = m.Bytes();
            if (!s_Peer.TryGetValue(op, out Action<ulong, NetIn> h)) return;
            using (var r = new FastBufferReader(payload, Allocator.Temp))
            {
                try { h(from, new NetIn(r)); }
                catch (Exception e) { Debug.LogWarning("[CloneZ] Peer message '" + op + "' failed: " + e.Message); }
            }
        }
        #endregion

        #region Voice
        /// <summary>Client: one frame of our voice (8 kHz mu-law) — <paramref name="radio"/> when keyed on the walkie.</summary>
        internal static void SendVoice(byte[] frame, bool radio)
        {
            using (var o = new NetOut(frame.Length + 16, 4096)) { o.B(radio).Bytes(frame); NetSession.ToServer("cz.voice", o, NetworkDelivery.Unreliable); }
        }

        // Server: everyone within earshot hears it from the speaker's mouth; everyone on the channel hears the walkie.
        private static void OnVoiceUp(ulong sender, NetIn m)
        {
            NetworkManager nm = NetSession.NM;
            if (!nm.IsServer) return;
            bool radio = m.B();
            byte[] frame = m.Bytes();
            if (frame == null || frame.Length == 0 || frame.Length > 2048) return;
            if (!NetSession.Clients.TryGetValue(sender, out NetClientInfo from) || !from.Alive) return;
            foreach (ulong id in nm.ConnectedClientsIds)
            {
                if (id == sender || !NetSession.Clients.TryGetValue(id, out NetClientInfo to)) continue;
                if (to.InRaid != from.InRaid) continue;
                bool near = Vector3.Distance(to.Position, from.Position) <= VoiceRange;
                bool byRadio = !near && radio && from.RadioChannel > 0 && to.RadioChannel == from.RadioChannel && to.Alive;
                if (!near && !byRadio) continue;
                using (var o = new NetOut(frame.Length + 32, 4096))
                {
                    o.UL(sender).B(byRadio).Bytes(frame);
                    NetSession.SendTo(id, "cz.voicex", o, NetworkDelivery.Unreliable);
                }
            }
        }

        private static void OnVoiceDown(ulong server, NetIn m)
        {
            ulong speaker = m.UL();
            bool radio = m.B();
            byte[] frame = m.Bytes();
            Speaking[speaker] = Time.time;
            VoicePlayback.For(speaker, radio).Push(frame);
        }
        #endregion

        #region Squads
        /// <summary>Client: asks the server to form a squad (0), join <paramref name="squad"/> (1) or leave (2).</summary>
        public static void SquadOp(byte op, string squad = "")
        {
            using (var o = new NetOut(64)) { o.By(op).S(squad); NetSession.ToServer("cz.squad", o); }
        }

        public static void Invite(ulong who)
        {
            string squad = MySquad;
            NetworkManager nm = NetSession.NM;
            if (nm == null) return;
            if (string.IsNullOrEmpty(squad)) { squad = "net:" + nm.LocalClientId; SquadOp(0); }
            string s = squad;
            SendPeer(who, "squad.invite", o => o.S(s));
            GameEvents.RaiseToast("Invited " + NameOf(who) + ".");
        }

        public static void AcceptInvite()
        {
            if (InviteFrom == ulong.MaxValue || string.IsNullOrEmpty(InviteSquad)) return;
            SquadOp(1, InviteSquad);
            InviteFrom = ulong.MaxValue;
            InviteSquad = null;
        }

        private static void OnSquad(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer || !NetSession.Clients.TryGetValue(sender, out NetClientInfo c)) return;
            byte op = m.By();
            string arg = m.S();
            if (op == 0 && string.IsNullOrEmpty(c.Squad)) c.Squad = "net:" + sender;
            else if (op == 1 && !string.IsNullOrEmpty(arg))
            {
                int size = 0;
                foreach (NetClientInfo x in NetSession.Clients.Values) if (x.Squad == arg) size++;
                if (size == 0 || size >= MaxSquad)
                {
                    using (var o = new NetOut(64)) { o.S(size == 0 ? "That squad broke up." : "That squad is full."); NetSession.SendTo(sender, "cz.qsharex", o); }
                    return;
                }
                c.Squad = arg;
            }
            else if (op == 2) c.Squad = "";
            NetSession.PushRoster();
            ApplySquads();
        }

        /// <summary>Puts the roster's squads on the bodies: squadmates never fight, and see each other through walls.</summary>
        public static void ApplySquads()
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null) return;
            Actor me = Actor.LocalPlayer;
            if (me != null && NetSession.Clients.TryGetValue(nm.LocalClientId, out NetClientInfo mine))
            {
                string old = me.Squad;
                string want = string.IsNullOrEmpty(mine.Squad) ? (old != null && old.StartsWith("net:") ? null : old) : mine.Squad;
                if (want != old)
                {
                    me.Squad = want;
                    foreach (Actor a in Actor.All)
                        if (a != null && a.Kind == ActorKind.Companion && a.Squad == old) a.Squad = want;
                }
            }
            foreach (KeyValuePair<ulong, RemotePlayer> kv in NetPlayers.Proxies)
                if (kv.Value != null && NetSession.Clients.TryGetValue(kv.Key, out NetClientInfo ci))
                    kv.Value.Actor.Squad = string.IsNullOrEmpty(ci.Squad) ? null : ci.Squad;
        }

        // Kills, finds and captures count for the whole squad's jobs (extractions stay personal).
        private static void ShareQuest(string key, float amount)
        {
            if (!NetSession.Active || string.IsNullOrEmpty(MySquad) || string.IsNullOrEmpty(key)) return;
            if (!(key.StartsWith("kill:") || key.StartsWith("visit:") || key == "capture_outpost" || key == "survive_emission")) return;
            using (var o = new NetOut(96)) { o.S(key).F(amount); NetSession.ToServer("cz.qshare", o); }
        }

        private static void OnQuestShare(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer || !NetSession.Clients.TryGetValue(sender, out NetClientInfo from) || string.IsNullOrEmpty(from.Squad)) return;
            string key = m.S();
            float amount = Mathf.Clamp(m.F(), 0f, 5f);
            foreach (NetClientInfo c in NetSession.Clients.Values)
            {
                if (c.Id == sender || c.Squad != from.Squad) continue;
                using (var o = new NetOut(96)) { o.S(key).F(amount); NetSession.SendTo(c.Id, "cz.qsharex", o); }
            }
        }
        #endregion

        #region Death
        /// <summary>Remembers who hurt us last (the killer, if this is what kills us).</summary>
        public static void NoteAttacker(ulong client, uint net, string weapon)
        {
            s_LastAttacker = client;
            s_LastAttackerNet = net;
            s_LastWeapon = weapon ?? "";
        }

        /// <summary>We died: tell everyone (and who did it), and leave our body with what we carried for the living.</summary>
        public static void ReportDeath(ulong attackerClient, uint attackerNet, string weapon)
        {
            if (s_Reported || !NetSession.Active || !GameSession.IsExtraction) return;
            s_Reported = true;
            NetworkManager nm = NetSession.NM;
            using (var o = new NetOut(64)) { o.UL(nm.LocalClientId).UL(attackerClient).S(weapon ?? ""); NetSession.ToServer("cz.died", o); }
            Actor me = Actor.LocalPlayer;
            PlayerStats stats = me != null ? me.GetComponent<PlayerStats>() : null;
            if (stats == null || GameSession.IsMonsterRun) return;
            RaidKit kit = RaidLoadout.Capture(stats);
            if (kit == null) return;
            var lost = new List<StackSave>();
            foreach (RaidKitPiece p in kit.Clothing)
                if (p.Slot != (int)ClothingSlot.Pouch) lost.Add(new StackSave { Id = p.Id, Count = 1, Condition = p.Condition, TimesRepaired = p.TimesRepaired, Extra = p.Extra });
            foreach (RaidKitStack s in kit.Items)
                if (!s.InPouch) lost.Add(new StackSave { Id = s.Id, Count = s.Count, Condition = s.Condition, TimesRepaired = s.TimesRepaired, Extra = s.Extra });
            NetPlayers.PublishCorpse(me.transform.position, lost);
        }
        #endregion
    }

    /// <summary>
    /// The microphone: while push-to-talk (Caps Lock) or the walkie key (Y) is held, 16 kHz capture is averaged down
    /// to 8 kHz, mu-law coded (a byte a sample — 8 KB/s, plenty for a voice) and sent in 40 ms frames.
    /// </summary>
    public sealed class VoiceCapture : MonoBehaviour
    {
        private const int MicRate = 16000, FrameSamples = 320;
        public static bool Talking { get; private set; }
        public static bool OnRadio { get; private set; }
        public static bool NoMicrophone { get; private set; }

        private static VoiceCapture s_Instance;
        private AudioClip _mic;
        private string _device;
        private int _read;
        private readonly byte[] _frame = new byte[FrameSamples];
        private int _fill;
        private float _carry;
        private bool _odd;

        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[VoiceCapture]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<VoiceCapture>();
        }

        private void Update()
        {
            Actor me = Actor.LocalPlayer;
            bool alive = me != null && me.IsAlive;
            bool proximity = GameInput.HeldRaw(GameAction.PushToTalk);
            bool radio = GameInput.HeldRaw(GameAction.Radio) && NetSocial.HasWalkie && NetSocial.RadioChannel > 0;
            bool want = NetSession.Active && alive && (proximity || radio) && !NoMicrophone;
            OnRadio = want && radio;
            if (want && _mic == null) Begin();
            else if (!want && _mic != null) End();
            Talking = _mic != null;
            if (_mic != null) Pump();
        }

        private void Begin()
        {
            if (Microphone.devices.Length == 0)
            {
                NoMicrophone = true;
                GameEvents.RaiseToast("No microphone found — voice is off.");
                return;
            }
            _device = Microphone.devices[0];
            _mic = Microphone.Start(_device, true, 1, MicRate);
            _read = 0;
            _fill = 0;
            if (OnRadio) Sfx.PlayUI(SfxIds.UiClick, 0.6f);
        }

        private void End()
        {
            if (_fill > 0) Flush();
            Microphone.End(_device);
            _mic = null;
        }

        private void Pump()
        {
            int pos = Microphone.GetPosition(_device);
            if (pos < 0) return;
            int len = _mic.samples;
            int n = pos - _read;
            if (n < 0) n += len;
            if (n < 2) return;
            var data = new float[n];
            _mic.GetData(data, _read);
            _read = (_read + n) % len;
            for (int i = 0; i < data.Length; i++)
            {
                // Average each pair of 16 kHz samples into one at 8 kHz.
                if (!_odd) { _carry = data[i]; _odd = true; continue; }
                _odd = false;
                _frame[_fill++] = MuLaw.Encode((_carry + data[i]) * 0.5f * 1.6f);
                if (_fill == FrameSamples) Flush();
            }
        }

        private void Flush()
        {
            var copy = new byte[_fill];
            Array.Copy(_frame, copy, _fill);
            _fill = 0;
            NetSocial.SendVoice(copy, OnRadio);
        }

        private void OnDisable() { if (_mic != null) End(); }
    }

    /// <summary>G.711 mu-law: 14-bit samples in a byte, the way telephones always have.</summary>
    public static class MuLaw
    {
        public static byte Encode(float f)
        {
            int s = Mathf.Clamp((int)(f * 32767f), -32767, 32767);
            int sign = (s >> 8) & 0x80;
            if (sign != 0) s = -s;
            if (s > 32635) s = 32635;
            s += 0x84;
            int exp = 7;
            for (int mask = 0x4000; (s & mask) == 0 && exp > 0; exp--, mask >>= 1) { }
            int mant = (s >> (exp + 3)) & 0x0F;
            return (byte)~(sign | (exp << 4) | mant);
        }

        public static float Decode(byte b)
        {
            int u = ~b & 0xFF;
            int sign = u & 0x80, exp = (u >> 4) & 7, mant = u & 0x0F;
            int s = (((mant << 3) + 0x84) << exp) - 0x84;
            return (sign != 0 ? -s : s) / 32768f;
        }
    }

    /// <summary>
    /// One voice being heard: a streaming 8 kHz clip fed from a small jitter buffer. A voice in the room plays from
    /// the speaker's body (3D, fading out by 30 m, muffled when they're out of sight); one on the walkie plays from
    /// your own radio, band-limited and crackling.
    /// </summary>
    public sealed class VoicePlayback : MonoBehaviour
    {
        private const int Rate = 8000;
        private static readonly Dictionary<ulong, VoicePlayback> s_Near = new Dictionary<ulong, VoicePlayback>();
        private static readonly Dictionary<ulong, VoicePlayback> s_Radio = new Dictionary<ulong, VoicePlayback>();

        private readonly float[] _ring = new float[Rate];
        private readonly object _lock = new object();
        private int _w, _r, _count;
        private bool _primed;
        private AudioSource _src;
        private AudioLowPassFilter _muffle;
        private ulong _speaker;
        private bool _radio;

        public static VoicePlayback For(ulong speaker, bool radio)
        {
            Dictionary<ulong, VoicePlayback> map = radio ? s_Radio : s_Near;
            if (map.TryGetValue(speaker, out VoicePlayback v) && v != null) return v;
            var go = new GameObject((radio ? "Radio voice " : "Voice ") + speaker);
            v = go.AddComponent<VoicePlayback>();
            v._speaker = speaker;
            v._radio = radio;
            v.Setup();
            map[speaker] = v;
            return v;
        }

        private void Setup()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = AudioClip.Create("Voice", Rate, 1, Rate, true, Read);
            _src.loop = true;
            _src.playOnAwake = false;
            _src.dopplerLevel = 0f;
            if (_radio)
            {
                _src.spatialBlend = 0f;
                _src.volume = 0.85f;
                gameObject.AddComponent<AudioHighPassFilter>().cutoffFrequency = 450f;
                gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 2800f;
                gameObject.AddComponent<AudioDistortionFilter>().distortionLevel = 0.45f;
            }
            else
            {
                _src.spatialBlend = 1f;
                _src.rolloffMode = AudioRolloffMode.Linear;
                _src.minDistance = 2f;
                _src.maxDistance = NetSocial.VoiceRange;
                _muffle = gameObject.AddComponent<AudioLowPassFilter>();
                _muffle.cutoffFrequency = 22000f;
            }
            _src.Play();
            DontDestroyOnLoad(gameObject);
        }

        public void Push(byte[] frame)
        {
            if (frame == null) return;
            lock (_lock)
            {
                for (int i = 0; i < frame.Length; i++)
                {
                    _ring[_w] = MuLaw.Decode(frame[i]);
                    _w = (_w + 1) % _ring.Length;
                    if (_count < _ring.Length) _count++;
                    else _r = (_r + 1) % _ring.Length;   // too far behind: drop the oldest
                }
            }
        }

        // Called by the audio system (possibly off the main thread).
        private void Read(float[] data)
        {
            lock (_lock)
            {
                if (!_primed && _count >= Rate / 12) _primed = true;   // ~80 ms banked before speaking
                for (int i = 0; i < data.Length; i++)
                {
                    if (_primed && _count > 0)
                    {
                        data[i] = _ring[_r];
                        _r = (_r + 1) % _ring.Length;
                        _count--;
                    }
                    else data[i] = 0f;
                }
                if (_count == 0) _primed = false;
            }
        }

        private void LateUpdate()
        {
            if (_radio)
            {
                Actor me = Actor.LocalPlayer;
                if (me != null) transform.position = me.EyePosition;
                return;
            }
            RemotePlayer p = NetPlayers.ProxyOf(_speaker);
            Actor listener = Actor.LocalPlayer;
            if (p != null) transform.position = p.Actor.EyePosition;
            else if (listener != null) transform.position = listener.EyePosition + Vector3.forward * 12f;   // heard through a wall
            if (_muffle != null && listener != null)
            {
                bool seen = p != null && !Physics.Linecast(listener.EyePosition, p.Actor.EyePosition, Actor.SightMask, QueryTriggerInteraction.Ignore);
                _muffle.cutoffFrequency = Mathf.MoveTowards(_muffle.cutoffFrequency, seen ? 22000f : 900f, 40000f * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Dictionary<ulong, VoicePlayback> map = _radio ? s_Radio : s_Near;
            if (map.TryGetValue(_speaker, out VoicePlayback v) && v == this) map.Remove(_speaker);
        }
    }

    /// <summary>
    /// Hands up (F3) and handcuffs. Hands up: no shooting, a slow walk, and the Zone's people leave you be (killing
    /// someone with their hands up is remembered). Cuffed: the same, until someone uncuffs you or you struggle free
    /// (hold F3 for half a minute).
    /// </summary>
    public sealed class PlayerRestraint : MonoBehaviour, IStatModSource
    {
        public const float StruggleSeconds = 30f;
        public static PlayerRestraint Local { get; private set; }
        public float Struggle01 { get; private set; }
        public bool HandsUp => _actor != null && _actor.Surrendered;
        public bool Cuffed => _actor != null && _actor.Restrained;

        private Actor _actor;
        private PlayerStats _stats;

        private void Awake()
        {
            _actor = GetComponent<Actor>();
            _stats = GetComponent<PlayerStats>();
            Local = this;
        }

        private void OnDestroy() { if (Local == this) Local = null; }

        private void Update()
        {
            if (_actor == null || _stats == null) return;
            if (_stats.IsDead || HubRules.SafeZone)
            {
                _actor.Surrendered = false;
                _actor.Restrained = false;
                Struggle01 = 0f;
                return;
            }
            if (_actor.Restrained)
            {
                if (GameInput.Held(GameAction.HandsUp))
                {
                    Struggle01 += Time.deltaTime / StruggleSeconds;
                    if (Struggle01 >= 1f) { Free(); GameEvents.RaiseToast("You work your hands free of the cuffs."); }
                }
                return;
            }
            if (GameInput.Pressed(GameAction.HandsUp) && !_stats.Incapacitated)
            {
                _actor.Surrendered = !_actor.Surrendered;
                GameEvents.RaiseToast(_actor.Surrendered ? "Hands up." : "Hands down.");
            }
        }

        public void Cuff(string by)
        {
            if (_actor == null) return;
            _actor.Restrained = true;
            _actor.Surrendered = true;
            Struggle01 = 0f;
            Sfx.PlayUI(SfxIds.Handcuffs);
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv != null) inv.EquipBareHands();
            GameEvents.RaiseToast(by + " cuffs your hands. Hold [" + GameInput.KeyName(GameAction.HandsUp) + "] to struggle free.");
        }

        public void Free()
        {
            if (_actor == null) return;
            _actor.Restrained = false;
            _actor.Surrendered = false;
            Struggle01 = 0f;
            Sfx.PlayUI(SfxIds.Handcuffs, 0.6f);
        }

        public void ApplyMods(StatMods m)
        {
            if (_actor == null) return;
            if (_actor.Restrained) { m.MoveSpeed *= 0.55f; m.SprintSpeed *= 0.3f; }
            else if (_actor.Surrendered) { m.MoveSpeed *= 0.6f; m.SprintSpeed *= 0.4f; }
        }
    }

    /// <summary>What can be done to another runner, as seen from both ends (the doer and the done-to).</summary>
    public static class PeerActions
    {
        internal static void Register()
        {
            NetSocial.Peer("act.cuff", (from, m) =>
            {
                PlayerRestraint r = PlayerRestraint.Local;
                PlayerStats st = r != null ? r.GetComponent<PlayerStats>() : null;
                bool helpless = r != null && (r.HandsUp || (st != null && st.Incapacitated));
                if (r == null || r.Cuffed || !helpless) { NetSocial.SendPeer(from, "act.refund", o => o.S("handcuffs").S("They pulled away.")); return; }
                r.Cuff(NetSocial.NameOf(from));
            });
            NetSocial.Peer("act.uncuff", (from, m) =>
            {
                PlayerRestraint r = PlayerRestraint.Local;
                if (r == null || !r.Cuffed) return;
                r.Free();
                GameEvents.RaiseToast(NetSocial.NameOf(from) + " takes the cuffs off.");
            });
            NetSocial.Peer("act.refund", (from, m) =>
            {
                ItemDef def = ItemDatabase.Get(m.S());
                string why = m.S();
                PlayerInventory inv = HubKit.Carried;
                if (def != null && inv != null) inv.Inventory.Add(def, 1, enforceWeight: false);
                if (!string.IsNullOrEmpty(why)) GameEvents.RaiseToast(why);
            });
            NetSocial.Peer("act.disarm", (from, m) =>
            {
                PlayerRestraint r = PlayerRestraint.Local;
                PlayerInventory inv = HubKit.Carried;
                if (r == null || inv == null || !(r.HandsUp || r.Cuffed)) return;
                ItemStack held = inv.EquippedStack;
                if (held == null) return;
                inv.Drop(held, held.Count);
                inv.EquipBareHands();
                GameEvents.RaiseToast(NetSocial.NameOf(from) + " makes you drop your " + held.Def.DisplayName + ".");
            });
            NetSocial.Peer("act.feed", (from, m) =>
            {
                ItemDef def = ItemDatabase.Get(m.S());
                PlayerInventory inv = HubKit.Carried;
                if (def == null || inv == null) return;
                inv.Inventory.Add(def, 1, enforceWeight: false);
                ItemStack fed = null;
                for (int i = inv.Inventory.Stacks.Count - 1; i >= 0; i--) if (inv.Inventory.Stacks[i].Def == def) { fed = inv.Inventory.Stacks[i]; break; }
                GameEvents.RaiseToast(NetSocial.NameOf(from) + " feeds you " + def.DisplayName + ".");
                if (fed != null) inv.Use(fed);
            });
            NetSocial.Peer("act.bandage", (from, m) =>
            {
                bool sterile = m.B();
                PlayerInventory inv = HubKit.Carried;
                PlayerWounds w = inv != null ? inv.GetComponent<PlayerWounds>() : null;
                if (w != null && w.ApplyBandage(sterile)) GameEvents.RaiseToast(NetSocial.NameOf(from) + " dresses your wound.");
            });
            NetSocial.Peer("act.blood", (from, m) =>
            {
                string bag = m.S();
                PlayerInventory inv = HubKit.Carried;
                PlayerBlood b = inv != null ? inv.GetComponent<PlayerBlood>() : null;
                if (b == null) return;
                b.Transfuse(BloodTypes.FromBagId(bag), out string message);
                Sfx.PlayUI(SfxIds.Injector);
                GameEvents.RaiseToast(NetSocial.NameOf(from) + " runs a line into your arm. " + message);
            });
            NetSocial.Peer("act.rouse", (from, m) =>
            {
                PlayerInventory inv = HubKit.Carried;
                PlayerBlood b = inv != null ? inv.GetComponent<PlayerBlood>() : null;
                if (b != null && b.Rouse()) GameEvents.RaiseToast(NetSocial.NameOf(from) + " shakes you awake.");
            });
        }
    }

    /// <summary>
    /// Another runner's body as something you can walk up to: cuff them when their hands are up (or they're out
    /// cold), make them drop their gun, take the cuffs off; feed them, dress a wound, give them a pint of blood,
    /// shake them awake; invite them into your squad, or offer a trade.
    /// </summary>
    public sealed class RemotePlayerInteract : Interactable
    {
        private RemotePlayer _p;

        public override string DisplayName => _p != null ? _p.Actor.DisplayName + (_p.Actor.Restrained ? " (cuffed)" : _p.Actor.Surrendered ? " (hands up)" : _p.Incapacitated ? " (out cold)" : "") : "Runner";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.2f;
        public override float Reach => 2.2f;
        public override bool IsAvailable => isActiveAndEnabled && _p != null && !_p.IsDead && NetSession.Active;

        private void Awake() => _p = GetComponent<RemotePlayer>();

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_p == null || ctx.Inventory == null) return;
            Actor a = _p.Actor;
            ulong who = _p.Owner;
            string name = a.DisplayName;
            bool helpless = a.Surrendered || a.Restrained || _p.Incapacitated;
            PlayerInventory inv = ctx.Inventory;

            if (helpless && !a.Restrained)
                into.Add(new InteractAction("Cuff " + name, c =>
                {
                    if (c.Inventory.Consume("handcuffs", 1) <= 0) return;
                    NetSocial.SendPeer(who, "act.cuff");
                }).Timed(3f, "Cuffing...").DisabledIf(!inv.Has("handcuffs"), "You need handcuffs"));
            if (a.Restrained)
                into.Add(new InteractAction("Take the cuffs off " + name, c =>
                {
                    NetSocial.SendPeer(who, "act.uncuff");
                    ItemDef cuffs = ItemDatabase.Get("handcuffs");
                    if (cuffs != null) c.Inventory.Inventory.Add(cuffs, 1, enforceWeight: false);
                }).Timed(2f, "Unlocking..."));
            if (helpless && !string.IsNullOrEmpty(_p.Held))
                into.Add(new InteractAction("Make them drop the " + HumanUtil.Pretty(_p.Held), c => NetSocial.SendPeer(who, "act.disarm")));
            if (_p.Incapacitated)
                into.Add(new InteractAction("Shake them awake", c => NetSocial.SendPeer(who, "act.rouse")).Timed(4f, "Shaking them..."));

            ItemStack food = null, bandage = null, blood = null;
            foreach (ItemStack s in inv.Inventory.Stacks)
            {
                if (s == null || s.Def == null) continue;
                if (food == null && (s.Def.HungerRestore > 0f || (s.Def.ThirstRestore > 0f && (!s.Def.IsWaterContainer || s.Def.HoldsWater)))) food = s;
                if (bandage == null && s.Def.StopsBleeding) bandage = s;
                if (blood == null && s.Def.Id.StartsWith("bloodbag_") && s.Def.Id != "bloodbag_empty") blood = s;
            }
            if (food != null)
            {
                ItemDef d = food.Def;
                into.Add(new InteractAction((helpless ? "Feed them: " : "Give them: ") + d.DisplayName, c =>
                {
                    if (c.Inventory.Consume(d.Id, 1) <= 0) return;
                    NetSocial.SendPeer(who, "act.feed", o => o.S(d.Id));
                }).Timed(2.5f, "Feeding..."));
            }
            if (bandage != null)
            {
                ItemDef d = bandage.Def;
                into.Add(new InteractAction("Dress their wound (" + d.DisplayName + ")", c =>
                {
                    if (c.Inventory.Consume(d.Id, 1) <= 0) return;
                    NetSocial.SendPeer(who, "act.bandage", o => o.B(d.SterileBandage));
                    if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.FirstAid, 4f);
                }).Timed(4f, "Bandaging..."));
            }
            if (blood != null)
            {
                ItemDef d = blood.Def;
                into.Add(new InteractAction("Give them blood (" + BloodTypes.FromBagId(d.Id) + ")", c =>
                {
                    if (!c.Inventory.Has("ivkit") || c.Inventory.Consume(d.Id, 1) <= 0) return;
                    c.Inventory.Consume("ivkit", 1);
                    NetSocial.SendPeer(who, "act.blood", o => o.S(d.Id));
                    if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.FirstAid, 6f);
                }).Timed(8f, "Transfusing...").DisabledIf(!inv.Has("ivkit"), "You need an IV kit"));
            }

            if (!NetSocial.SameSquad(who))
                into.Add(new InteractAction("Invite " + name + " into your squad", c => NetSocial.Invite(who)));
            into.Add(new InteractAction("Offer " + name + " a trade", c => PlayerTrade.Request(who)));
        }
    }

    /// <summary>
    /// Trading hand to hand: each side lays out what they offer, both mark ready, and the swap happens on both
    /// machines at once (the lower client id leads, so the two can't disagree about who goes first).
    /// </summary>
    public static class PlayerTrade
    {
        public static ulong Partner { get; private set; } = ulong.MaxValue;
        public static ulong Requested { get; private set; } = ulong.MaxValue;
        public static readonly List<KeyValuePair<ItemStack, int>> Mine = new List<KeyValuePair<ItemStack, int>>();
        public static List<StackSave> Theirs { get; private set; } = new List<StackSave>();
        public static bool MeReady { get; private set; }
        public static bool ThemReady { get; private set; }
        public static event Action Changed;
        private static string s_ThemSig = "";

        public static bool Active => Partner != ulong.MaxValue;

        internal static void Register()
        {
            NetSocial.Peer("trade.req", (from, m) =>
            {
                Requested = from;
                GameEvents.RaiseToast(NetSocial.NameOf(from) + " wants to trade. [" + GameInput.KeyName(GameAction.Squad) + "] to answer.");
                Sfx.PlayUI(SfxIds.UiClick);
            });
            NetSocial.Peer("trade.open", (from, m) => Open(from));
            NetSocial.Peer("trade.offer", (from, m) =>
            {
                if (from != Partner) return;
                Theirs = NetCodec.ReadSaves(m);
                MeReady = ThemReady = false;
                Changed?.Invoke();
            });
            NetSocial.Peer("trade.ready", (from, m) =>
            {
                if (from != Partner) return;
                ThemReady = m.B();
                s_ThemSig = m.S();
                Changed?.Invoke();
                TryGo();
            });
            NetSocial.Peer("trade.go", (from, m) =>
            {
                // The leader: the follower checked its side; check ours, swap, tell them to swap.
                string sig = m.S();
                if (from != Partner || !MeReady || sig != Signature() || !StillHave()) { SendCancel("The trade fell through."); return; }
                Commit();
                NetSocial.SendPeer(from, "trade.done", o => o.S(sig));
            });
            NetSocial.Peer("trade.done", (from, m) =>
            {
                if (from != Partner || m.S() != Signature()) return;
                Commit();
            });
            NetSocial.Peer("trade.cancel", (from, m) =>
            {
                if (from != Partner) return;
                string why = m.S();
                End();
                GameEvents.RaiseToast(string.IsNullOrEmpty(why) ? NetSocial.NameOf(from) + " walked away from the trade." : why);
            });
        }

        public static void Request(ulong who)
        {
            NetSocial.SendPeer(who, "trade.req");
            GameEvents.RaiseToast("You offer " + NetSocial.NameOf(who) + " a trade.");
        }

        public static void Accept()
        {
            if (Requested == ulong.MaxValue) return;
            ulong who = Requested;
            Requested = ulong.MaxValue;
            NetSocial.SendPeer(who, "trade.open");
            Open(who);
        }

        private static void Open(ulong who)
        {
            Partner = who;
            Mine.Clear();
            Theirs = new List<StackSave>();
            MeReady = ThemReady = false;
            s_ThemSig = "";
            HubScreens.Open(HubScreens.Trade);
        }

        public static void Offer(ItemStack s, int count)
        {
            if (!Active || s == null) return;
            for (int i = 0; i < Mine.Count; i++)
                if (ReferenceEquals(Mine[i].Key, s)) { Mine[i] = new KeyValuePair<ItemStack, int>(s, Mathf.Min(s.Count, Mine[i].Value + count)); SendOffer(); return; }
            Mine.Add(new KeyValuePair<ItemStack, int>(s, Mathf.Min(s.Count, count)));
            SendOffer();
        }

        public static void Withdraw(int index)
        {
            if (index < 0 || index >= Mine.Count) return;
            Mine.RemoveAt(index);
            SendOffer();
        }

        public static int Offered(ItemStack s)
        {
            for (int i = 0; i < Mine.Count; i++) if (ReferenceEquals(Mine[i].Key, s)) return Mine[i].Value;
            return 0;
        }

        private static List<StackSave> MineSaves()
        {
            var list = new List<StackSave>();
            foreach (KeyValuePair<ItemStack, int> kv in Mine)
            {
                if (kv.Key == null || kv.Key.Def == null) continue;
                StackSave s = ProfileStore.ToSave(kv.Key);
                s.Count = kv.Value;
                list.Add(s);
            }
            return list;
        }

        private static void SendOffer()
        {
            MeReady = ThemReady = false;
            List<StackSave> saves = MineSaves();
            NetSocial.SendPeer(Partner, "trade.offer", o => NetCodec.Write(o, saves));
            Changed?.Invoke();
        }

        public static void SetReady(bool ready)
        {
            if (!Active) return;
            MeReady = ready;
            string sig = Signature();
            NetSocial.SendPeer(Partner, "trade.ready", o => o.B(ready).S(sig));
            Changed?.Invoke();
            TryGo();
        }

        // Both ready on the same deal: the follower checks its side and hands over to the leader.
        private static void TryGo()
        {
            NetworkManager nm = NetSession.NM;
            if (nm == null || !MeReady || !ThemReady || s_ThemSig != Signature()) return;
            bool leader = nm.LocalClientId < Partner;
            if (leader) return;
            if (!StillHave()) { SendCancel("Something you offered is gone."); return; }
            string sig = Signature();
            NetSocial.SendPeer(Partner, "trade.go", o => o.S(sig));
        }

        // The deal as both sides see it: the lower id's offer first.
        private static string Signature()
        {
            NetworkManager nm = NetSession.NM;
            string mine = Sig(MineSaves()), theirs = Sig(Theirs);
            return nm != null && nm.LocalClientId < Partner ? mine + "#" + theirs : theirs + "#" + mine;
        }

        private static string Sig(List<StackSave> list)
        {
            var sb = new StringBuilder();
            foreach (StackSave s in list) sb.Append(s.Id).Append('x').Append(s.Count).Append('c').Append(s.Condition).Append(';');
            return sb.ToString();
        }

        private static bool StillHave()
        {
            Inventory inv = HubKit.Pack;
            if (inv == null) return false;
            foreach (KeyValuePair<ItemStack, int> kv in Mine)
                if (kv.Key == null || !inv.Stacks.Contains(kv.Key) || kv.Key.Count < kv.Value) return false;
            return true;
        }

        private static void Commit()
        {
            Inventory inv = HubKit.Pack;
            if (inv == null) return;
            foreach (KeyValuePair<ItemStack, int> kv in Mine) inv.Remove(kv.Key, kv.Value);
            foreach (StackSave s in Theirs) ProfileStore.AddSave(inv, s);
            string who = NetSocial.NameOf(Partner);
            End();
            HubKit.AfterChange();
            Sfx.PlayUI(SfxIds.UiBuy);
            GameEvents.RaiseToast("Traded with " + who + ".");
        }

        private static void SendCancel(string why)
        {
            if (Active) NetSocial.SendPeer(Partner, "trade.cancel", o => o.S(why ?? ""));
            End();
            if (!string.IsNullOrEmpty(why)) GameEvents.RaiseToast(why);
        }

        /// <summary>Walks away from the trade (closing the screen).</summary>
        public static void Cancel() { if (Active) SendCancel(""); }

        private static void End()
        {
            Partner = ulong.MaxValue;
            Mine.Clear();
            Theirs = new List<StackSave>();
            MeReady = ThemReady = false;
            Changed?.Invoke();
            if (UIScreen.Current is PlayerTradeScreen s) s.Close();
        }
    }

    /// <summary>The hand-to-hand trade window: your kit, your offer, theirs, and the ready switch.</summary>
    public sealed class PlayerTradeScreen : HubScreenBase
    {
        protected override string Title => "Trade with " + NetSocial.NameOf(PlayerTrade.Partner);
        private bool _committing;

        protected override void BuildContent()
        {
            if (!PlayerTrade.Active) { HubUI.Para(Body, "No trade under way.", 18, UIKit.TextDim); return; }
            PlayerTrade.Changed -= OnChanged;
            PlayerTrade.Changed += OnChanged;
            RectTransform[] cols = HubUI.Columns(Body, 14f, 1f, 1f, 1f);

            HubUI.Card(cols[0]);
            HubUI.Heading(HubUI.Stack(HubUI.Area(cols[0], 0f, 0.93f, 1f, 1f), 0f, 8), "What you carry", 20);
            RectTransform kit = HubUI.Scroll(HubUI.Area(cols[0], 0f, 0f, 1f, 0.93f));
            Inventory pack = HubKit.Pack;
            int row = 0;
            if (pack != null)
                foreach (ItemStack s in pack.Stacks)
                {
                    if (s == null || s.Def == null || HubKit.InPouch(s)) continue;
                    int left = s.Count - PlayerTrade.Offered(s);
                    if (left <= 0) continue;
                    RectTransform r = HubUI.ItemRow(kit, s, Tip, row++);
                    ItemStack pick = s;
                    HubUI.Btn(r, "Offer 1", () => { Click(); PlayerTrade.Offer(pick, 1); }, 90f, 28f);
                    if (left > 1) HubUI.Btn(r, "All", () => { Click(); PlayerTrade.Offer(pick, pick.Count); }, 60f, 28f);
                }

            HubUI.Card(cols[1]);
            HubUI.Heading(HubUI.Stack(HubUI.Area(cols[1], 0f, 0.93f, 1f, 1f), 0f, 8), "Your offer", 20);
            RectTransform mine = HubUI.Scroll(HubUI.Area(cols[1], 0f, 0.14f, 1f, 0.93f));
            for (int i = 0; i < PlayerTrade.Mine.Count; i++)
            {
                KeyValuePair<ItemStack, int> kv = PlayerTrade.Mine[i];
                if (kv.Key == null || kv.Key.Def == null) continue;
                RectTransform r = HubUI.Row(mine, 34f, i % 2 == 0 ? UIKit.RowColor : UIKit.RowAlt);
                HubUI.Cell(r, HubUI.ItemName(kv.Key.Def), -1f, 16);
                HubUI.Cell(r, "x" + kv.Value, 60f, 15, UIKit.TextDim, TextAnchor.MiddleRight);
                int idx = i;
                HubUI.Btn(r, "Take back", () => { Click(); PlayerTrade.Withdraw(idx); }, 110f, 28f);
            }
            RectTransform bottom = HubUI.Stack(HubUI.Area(cols[1], 0f, 0f, 1f, 0.14f), 6f, 10);
            HubUI.Btn(bottom, PlayerTrade.MeReady ? "Ready \u2713 (click to change your mind)" : "Mark ready", () => { Click(); PlayerTrade.SetReady(!PlayerTrade.MeReady); },
                -1f, 40f, PlayerTrade.MeReady ? UIKit.Good : UIKit.ButtonColor);

            HubUI.Card(cols[2]);
            HubUI.Heading(HubUI.Stack(HubUI.Area(cols[2], 0f, 0.93f, 1f, 1f), 0f, 8), "Their offer", 20);
            RectTransform theirs = HubUI.Scroll(HubUI.Area(cols[2], 0f, 0.14f, 1f, 0.93f));
            var view = new Inventory(9999f);
            foreach (StackSave s in PlayerTrade.Theirs) ProfileStore.AddSave(view, s);
            for (int i = 0; i < view.Stacks.Count; i++) HubUI.ItemRow(theirs, view.Stacks[i], Tip, i);
            if (view.Stacks.Count == 0) HubUI.Para(theirs, "Nothing yet.", 15, UIKit.TextDim);
            RectTransform state = HubUI.Stack(HubUI.Area(cols[2], 0f, 0f, 1f, 0.14f), 6f, 10);
            HubUI.Para(state, PlayerTrade.ThemReady ? UIKit.Colored("They're ready.", UIKit.Good) : "They haven't agreed yet.", 17);
            HubUI.Para(state, "Changing either offer un-readies both sides.", 14, UIKit.TextDim);
        }

        private void OnChanged()
        {
            if (this == null) { PlayerTrade.Changed -= OnChanged; return; }
            _committing = !PlayerTrade.Active;
            if (!_committing) Refresh();
        }

        protected override void OnClosed()
        {
            PlayerTrade.Changed -= OnChanged;
            if (PlayerTrade.Active) PlayerTrade.Cancel();
            base.OnClosed();
        }
    }

    /// <summary>The session's runners: squads, invitations, trades and the walkie's channel.</summary>
    public sealed class SquadScreen : HubScreenBase
    {
        protected override string Title => "Runners in this session";

        protected override void BuildContent()
        {
            NetworkManager nm = NetSession.NM;
            RectTransform area = HubUI.Area(Body, 0.1f, 0.04f, 0.9f, 0.98f);
            HubUI.Card(area);
            RectTransform col = HubUI.Stack(area, 8f, 16);
            if (nm == null || !NetSession.Active)
            {
                HubUI.Heading(col, "Playing alone", 22);
                HubUI.Para(col, "Host or join a session from the main menu to run with others.", 16, UIKit.TextDim);
                return;
            }
            string mine = NetSocial.MySquad;
            HubUI.Para(col, "Hold [" + GameInput.KeyName(GameAction.PushToTalk) + "] to talk to anyone within " + (int)NetSocial.VoiceRange + " m. Carry a walkie-talkie and hold ["
                + GameInput.KeyName(GameAction.Radio) + "] to talk to everyone on its channel, wherever they are. [" + GameInput.KeyName(GameAction.HandsUp) + "] puts your hands up.", 15, UIKit.TextDim);

            RectTransform radio = HubUI.Row(col, 40f);
            HubUI.Cell(radio, "Walkie channel" + (NetSocial.HasWalkie ? "" : "  (you have no walkie)"), 320f, 16);
            var opts = new string[NetSocial.Channels + 1];
            opts[0] = "Off";
            for (int i = 1; i <= NetSocial.Channels; i++) opts[i] = "Channel " + i;
            HubUI.Cycle(radio, "", opts, Mathf.Clamp(NetSocial.RadioChannel, 0, NetSocial.Channels), i => { NetSocial.RadioChannel = i; });

            if (NetSocial.InviteFrom != ulong.MaxValue)
            {
                RectTransform inv = HubUI.Row(col, 46f, new Color(0.2f, 0.35f, 0.2f, 0.4f));
                HubUI.Cell(inv, NetSocial.NameOf(NetSocial.InviteFrom) + " invites you into their squad.", -1f, 17);
                HubUI.Btn(inv, "Join", () => { Click(); NetSocial.AcceptInvite(); Invoke(nameof(Later), 0.4f); }, 110f, 34f, UIKit.Good);
                HubUI.Btn(inv, "Decline", () => { Click(); NetSocial.InviteFrom = ulong.MaxValue; Refresh(); }, 110f, 34f);
            }
            if (PlayerTrade.Requested != ulong.MaxValue)
            {
                RectTransform tr = HubUI.Row(col, 46f, new Color(0.25f, 0.3f, 0.15f, 0.4f));
                HubUI.Cell(tr, NetSocial.NameOf(PlayerTrade.Requested) + " wants to trade.", -1f, 17);
                HubUI.Btn(tr, "Trade", () => { Click(); PlayerTrade.Accept(); }, 110f, 34f, UIKit.Good);
            }

            HubUI.Heading(col, string.IsNullOrEmpty(mine) ? "You run alone" : "Your squad", 20);
            if (!string.IsNullOrEmpty(mine))
            {
                RectTransform leave = HubUI.Row(col, 40f);
                HubUI.Cell(leave, "Squadmates can't hurt each other, share kills and finds for jobs, and always see each other.", -1f, 15, UIKit.TextDim);
                HubUI.Btn(leave, "Leave the squad", () => { Click(); NetSocial.SquadOp(2); Invoke(nameof(Later), 0.4f); }, 170f, 32f);
            }

            HubUI.Heading(col, "Everyone", 20);
            int n = 0;
            foreach (NetClientInfo c in NetSession.Clients.Values)
            {
                bool me = c.Id == nm.LocalClientId;
                RectTransform r = HubUI.Row(col, 44f, n++ % 2 == 0 ? UIKit.RowColor : UIKit.RowAlt);
                HubUI.Cell(r, c.Name + (me ? "  (you)" : ""), 300f, 17);
                HubUI.Cell(r, Factions.NameOf(c.Faction), 200f, 15, Factions.Get(c.Faction).Color);
                HubUI.Cell(r, "Level " + c.Level, 110f, 15, UIKit.TextDim);
                HubUI.Cell(r, !string.IsNullOrEmpty(c.Squad) && c.Squad == mine ? "squadmate" : !string.IsNullOrEmpty(c.Squad) ? "in a squad" : "", 140f, 15, UIKit.Accent);
                HubUI.Cell(r, c.InRaid ? "in the Zone" : "in the bar", 140f, 15, UIKit.TextDim);
                HubUI.Spacer(r);
                if (me) continue;
                ulong id = c.Id;
                bool same = !string.IsNullOrEmpty(mine) && c.Squad == mine;
                HubUI.Btn(r, "Invite", () => { Click(); NetSocial.Invite(id); }, 100f, 32f, null, !same);
                HubUI.Btn(r, "Trade", () => { Click(); PlayerTrade.Request(id); }, 100f, 32f);
            }
        }

        private void Later() => Refresh();
    }

    /// <summary>The social line on the HUD: hands up / cuffed, talking, who you can hear, waiting invitations.</summary>
    public sealed class HudSocial : MonoBehaviour
    {
        private Text _text;
        private readonly StringBuilder _sb = new StringBuilder();

        public static HudSocial Create()
        {
            RectTransform slot = HudLayer.Anchor("Social", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 300f), new Vector2(520f, 120f));
            var h = slot.gameObject.AddComponent<HudSocial>();
            h._text = UIKit.Label(slot, "", 15, new Color(0.9f, 0.92f, 0.85f), TextAnchor.LowerLeft);
            UIKit.Stretch(h._text.rectTransform);
            return h;
        }

        private void Update()
        {
            if (GameInput.Pressed(GameAction.Squad) && NetSession.Active) HubScreens.Open(HubScreens.Squad);
        }

        private void LateUpdate()
        {
            _sb.Length = 0;
            PlayerRestraint r = PlayerRestraint.Local;
            if (r != null && r.Cuffed) _sb.Append(UIKit.Colored("Cuffed", UIKit.Bad)).Append(" — hold [").Append(GameInput.KeyName(GameAction.HandsUp)).Append("] to struggle free (")
                .Append(Mathf.RoundToInt(r.Struggle01 * 100f)).Append("%)\n");
            else if (r != null && r.HandsUp) _sb.Append(UIKit.Colored("Hands up", UIKit.Warn)).Append(" — [").Append(GameInput.KeyName(GameAction.HandsUp)).Append("] to lower them\n");
            if (NetSession.Active)
            {
                if (VoiceCapture.Talking) _sb.Append(UIKit.Colored(VoiceCapture.OnRadio ? "Transmitting on channel " + NetSocial.RadioChannel : "Talking", UIKit.Good)).Append('\n');
                foreach (KeyValuePair<ulong, float> kv in NetSocial.Speaking)
                    if (Time.time - kv.Value < 0.4f) _sb.Append("\u266A ").Append(NetSocial.NameOf(kv.Key)).Append('\n');
                if (NetSocial.InviteFrom != ulong.MaxValue || PlayerTrade.Requested != ulong.MaxValue)
                    _sb.Append(UIKit.Colored("[" + GameInput.KeyName(GameAction.Squad) + "] someone is waiting on an answer", UIKit.Accent)).Append('\n');
            }
            _text.text = _sb.ToString();
        }
    }

    /// <summary>Hands up and cuffs for everyone; the social HUD, the walkie and the runners' screens when playing together.</summary>
    public sealed class SocialModule : IGameModule
    {
        public int Order => 92;

        public void Init()
        {
            HubScreens.Register(HubScreens.Trade, arg => UIScreen.Show<PlayerTradeScreen>());
            HubScreens.Register(HubScreens.Squad, arg => UIScreen.Show<SquadScreen>());
            ItemActions.RegisterUse("radio_walkie", (inv, s) =>
            {
                NetSocial.RadioChannel = (NetSocial.RadioChannel + 1) % (NetSocial.Channels + 1);
                GameEvents.RaiseToast(NetSocial.RadioChannel == 0 ? "Walkie off." : "Walkie on channel " + NetSocial.RadioChannel + ". Hold [" + GameInput.KeyName(GameAction.Radio) + "] to talk.");
                Sfx.PlayUI(SfxIds.UiClick);
                return true;
            });
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<PlayerRestraint>() == null) p.gameObject.AddComponent<PlayerRestraint>();
                if (UnityEngine.Object.FindAnyObjectByType<HudSocial>() == null) HudSocial.Create();
            };
        }
    }
}
