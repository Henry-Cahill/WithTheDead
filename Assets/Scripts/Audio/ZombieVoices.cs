using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The voices of the dead. Every corpse-walker within earshot has a throat of its own (one of the recipes' throats,
    /// with a pitch, a loudness and a manner drawn from its id, so it sounds the same every time it is heard, on every
    /// peer, from its first groan to its death rattle) and its own clock: the idle
    /// ones moan and breathe now and then, the hunters groan often and loud, a Sprinter on the chase shrieks, a Crawler
    /// gurgles as it drags itself along. The moment one fixes on the survivor it snarls. Its attack, its pain and its
    /// death (<see cref="Say"/>, <see cref="Hurt"/>) come out of the same throat and cut off whatever it was groaning.
    /// A horde is a chorus, not a wall: only a few speak at once, the nearest and most dangerous first, none of them talks
    /// over itself, and a voice follows its zombie while it lasts. Silent while paused and in the hub.
    /// </summary>
    public sealed class ZombieVoices : MonoBehaviour
    {
        private const float ScanEvery = 0.25f;
        /// <summary>Hunters are heard from this far, idle ones from <see cref="IdleReach"/>.</summary>
        private const float HuntReach = 48f, IdleReach = 32f;
        private const int MaxVoices = 5, MaxStartsPerScan = 2;
        private const int RankIdle = 0, RankGroan = 1, RankHurt = 2, RankAlert = 3, RankAttack = 4, RankDie = 5;

        // One of the dead: its voice, its clock, and what it is saying now.
        private sealed class Mouth
        {
            public Actor Actor;
            public ZombieAI Ai;
            public HumanoidAnimator Body;            // a network proxy has no brain, only a body that leans in when it hunts
            public int Throat, LastLine;             // which of the recipes' throats is its own (ZombieThroats)
            public float Pitch, Volume, Talk;        // its own throat's size, and how much it uses it
            public float NextAt, BusyUntil, SeenAt, AlertAt, AlertUntil, HurtAt;
            public float Dist, Score;
            public int Rank;
            public bool Hunting, Known;
            public AudioSource Src;
            public AudioClip Clip;
        }

        private static ZombieVoices s_Instance;
        private static readonly System.Comparison<Mouth> s_ByScore = (a, b) => a.Score.CompareTo(b.Score);

        private readonly Dictionary<Actor, Mouth> _mouths = new Dictionary<Actor, Mouth>(64);
        private readonly List<Mouth> _near = new List<Mouth>(32);
        private readonly List<Mouth> _speaking = new List<Mouth>(8);
        private readonly List<Actor> _stale = new List<Actor>(16);
        private float _clock, _scanAt, _pruneAt;

        /// <summary>Ensures a single instance exists (it outlives scenes).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[ZombieVoices]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<ZombieVoices>();
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        /// <summary>
        /// One of the dead says <paramref name="id"/> (its lunge, its death) at <paramref name="at"/> in its own voice,
        /// cutting off whatever it was groaning. Without the director it is a plain one-shot (still in its own throat).
        /// </summary>
        public static void Say(Component zombie, string id, Vector3 at, float volume, float range, bool death = false)
        {
            if (zombie == null) return;
            Actor a = zombie.GetComponent<Actor>();
            if (a == null)
            {
                Sfx.Play(id, at, volume, 1f, range);
                return;
            }
            if (s_Instance == null || Time.timeScale <= 0f)
            {
                uint h = Hash(a);
                int v = Lines(id) > 0 ? ThroatOf(h) : -1;
                if (v >= 0) Sfx.Play(id, at, volume, PitchOf(h), range, v);
                else Sfx.Play(id, at, volume, PitchOf(h), range);
                return;
            }
            Mouth m = s_Instance.MouthOf(a);
            s_Instance.Utter(m, id, at, volume, range, death ? RankDie : RankAttack);
            m.NextAt = Mathf.Max(m.NextAt, m.BusyUntil + Random.Range(0.6f, 1.6f));
        }

        /// <summary>A grunt of pain from one of the dead when it is hit or shoved (not for every bullet).</summary>
        public static void Hurt(Component zombie)
        {
            if (zombie == null || s_Instance == null || Time.timeScale <= 0f) return;
            Actor a = zombie.GetComponent<Actor>();
            if (a == null) return;
            ZombieVoices v = s_Instance;
            Mouth m = v.MouthOf(a);
            if (v._clock < m.HurtAt || (v._clock < m.BusyUntil && m.Rank >= RankHurt) || Random.value > 0.6f) return;
            m.HurtAt = v._clock + Random.Range(0.8f, 1.5f);
            if (v.Utter(m, SfxIds.ZombieHurt, MouthAt(m), 0.8f, 30f, RankHurt)) m.NextAt = Mathf.Max(m.NextAt, m.BusyUntil + Random.Range(0.4f, 1.2f));
        }

        private void Update()
        {
            Actor local = Actor.LocalPlayer;
            if (Time.timeScale <= 0f || local == null || GameSession.IsHub) return;
            _clock += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            Follow();
            if (_clock < _scanAt) return;
            _scanAt = _clock + ScanEvery;
            Vector3 ears = ListenerRig.Active ? ListenerRig.Position : local.transform.position;
            Scan(ears, local);
            Speak();
            if (_clock >= _pruneAt) Prune();
        }

        // A voice moves with its zombie (a Sprinter covers metres in one shriek).
        private void Follow()
        {
            for (int i = _speaking.Count - 1; i >= 0; i--)
            {
                Mouth m = _speaking[i];
                if (_clock >= m.BusyUntil || !Playing(m))
                {
                    _speaking.RemoveAt(i);
                    continue;
                }
                if (m.Actor != null) m.Src.transform.position = MouthAt(m);
            }
        }

        // Who is near enough to be heard, who has just started hunting, and how much each matters.
        private void Scan(Vector3 ears, Actor local)
        {
            _near.Clear();
            List<Actor> all = Actor.All;
            for (int i = 0; i < all.Count; i++)
            {
                Actor a = all[i];
                if (a == null || a.Kind != ActorKind.Zombie || !a.IsAlive) continue;
                float d2 = (a.transform.position - ears).sqrMagnitude;
                if (d2 > HuntReach * HuntReach) continue;
                Mouth m = MouthOf(a);
                m.SeenAt = _clock;
                m.Dist = Mathf.Sqrt(d2);
                bool hunting = m.Ai != null ? m.Ai.IsHunting : m.Body != null && m.Body.Hunting;
                bool onMe = hunting && (m.Ai == null || m.Ai.Prey == local);
                if (hunting && !m.Hunting && m.Known && _clock >= m.AlertAt && (onMe || m.Dist < 22f))
                {
                    m.AlertAt = _clock + 14f;      // one snarl, not one every time it loses and finds you
                    m.AlertUntil = _clock + 1.2f;  // said soon, or not at all
                }
                m.Hunting = hunting;
                m.Known = true;
                if (!hunting && m.Dist > IdleReach) continue;
                m.Score = m.Dist + (onMe ? 0f : hunting ? 12f : 25f);
                _near.Add(m);
            }
            _near.Sort(s_ByScore);
        }

        // The most dangerous speak first: the newly hunting snarl (they may run one over the budget), then whoever is due.
        private void Speak()
        {
            int busy = 0;
            for (int i = 0; i < _speaking.Count; i++)
                if (_clock < _speaking[i].BusyUntil) busy++;
            int starts = 0;
            for (int i = 0; i < _near.Count && starts < MaxStartsPerScan; i++)
            {
                Mouth m = _near[i];
                if (m.AlertUntil <= 0f) continue;
                if (_clock > m.AlertUntil || busy > MaxVoices) { m.AlertUntil = 0f; continue; }
                if (_clock < m.BusyUntil && m.Rank >= RankAlert) continue;
                m.AlertUntil = 0f;
                if (!Utter(m, SfxIds.ZombieAlert, MouthAt(m), 0.95f, 50f, RankAlert)) continue;
                busy++;
                starts++;
                m.NextAt = m.BusyUntil + Interval(m) * 0.5f;
            }
            for (int i = 0; i < _near.Count && starts < MaxStartsPerScan; i++)
            {
                Mouth m = _near[i];
                if (_clock < m.NextAt || _clock < m.BusyUntil) continue;
                if (busy >= MaxVoices)
                {
                    m.NextAt = _clock + Random.Range(0.5f, 2f);
                    continue;
                }
                Choose(m, out string id, out float volume, out float range, out int rank);
                if (Utter(m, id, MouthAt(m), volume, range, rank))
                {
                    busy++;
                    starts++;
                }
                m.NextAt = _clock + Interval(m);
            }
        }

        // What it says when its turn comes: Crawlers gurgle, hunting Sprinters shriek as often as they groan, hunters
        // groan loud, the idle moan and breathe.
        private static void Choose(Mouth m, out string id, out float volume, out float range, out int rank)
        {
            ZombieType type = m.Ai != null ? m.Ai.Type : ZombieType.Shambler;
            rank = m.Hunting ? RankGroan : RankIdle;
            if (type == ZombieType.Crawler)
            {
                id = SfxIds.ZombieCrawl;
                volume = m.Hunting ? 0.8f : 0.55f;
                range = m.Hunting ? 32f : 24f;
            }
            else if (m.Hunting && type == ZombieType.Sprinter && Random.value < 0.55f)
            {
                id = SfxIds.ZombieChase;
                volume = 0.95f;
                range = 60f;
            }
            else if (m.Hunting)
            {
                id = SfxIds.ZombieGroan;
                volume = 0.9f;
                range = 46f;
            }
            else if (Random.value < 0.6f)
            {
                id = SfxIds.ZombieIdle;
                volume = 0.6f;
                range = 30f;
            }
            else
            {
                id = SfxIds.ZombieGroan;
                volume = 0.55f;
                range = 34f;
            }
        }

        // Seconds from one utterance's start to the next: hunters every few seconds (Sprinters more), the idle far less.
        private static float Interval(Mouth m)
        {
            float t;
            if (!m.Hunting) t = Random.Range(6f, 15f);
            else if (m.Ai != null && m.Ai.Type == ZombieType.Sprinter) t = Random.Range(1.8f, 3.4f);
            else t = Random.Range(2.6f, 5.2f);
            return t * m.Talk;
        }

        // Says `id` in this zombie's voice at `at` (its own throat and size), cutting off what it was saying (a quick
        // fade, not a click).
        private bool Utter(Mouth m, string id, Vector3 at, float volume, float range, int rank)
        {
            if (_clock < m.BusyUntil && Playing(m)) Sfx.FadeTo(m.Src, 0f, 0.08f, true);
            m.BusyUntil = 0f;
            m.Src = null;
            m.Clip = null;
            int v = Variation(m, id);
            float pitch = m.Pitch * Random.Range(0.97f, 1.03f);
            AudioSource src = v >= 0 ? Sfx.Play(id, at, volume * m.Volume, pitch, range, v) : Sfx.Play(id, at, volume * m.Volume, pitch, range);
            if (src == null) return false;
            m.Src = src;
            m.Clip = src.clip;
            m.Rank = rank;
            m.BusyUntil = _clock + (src.clip != null ? src.clip.length / Mathf.Max(0.1f, src.pitch) : 2f);
            if (!_speaking.Contains(m)) _speaking.Add(m);
            return true;
        }

        // How many lines each throat has for a vocal laid out by throat (ZombieThroats); 0 for any other sound.
        private static int Lines(string id)
        {
            if (id == SfxIds.ZombieGroan) return ZombieThroats.Groans;
            if (id == SfxIds.ZombieIdle) return ZombieThroats.Idles;
            if (id == SfxIds.ZombieCrawl) return ZombieThroats.Crawls;
            if (id == SfxIds.ZombieAlert || id == SfxIds.ZombieChase || id == SfxIds.ZombieAttack || id == SfxIds.ZombieHurt || id == SfxIds.ZombieDie) return 1;
            return 0;
        }

        // The variation that is this zombie's own: its throat, saying one of its lines (not the same one twice running).
        private static int Variation(Mouth m, string id)
        {
            int lines = Lines(id);
            if (lines <= 0) return -1;
            int line = 0;
            if (lines > 1)
            {
                line = (m.LastLine + 1 + Random.Range(0, lines - 1)) % lines;
                m.LastLine = line;
            }
            return m.Throat + ZombieThroats.Count * line;
        }

        private static bool Playing(Mouth m) => m.Src != null && m.Src.isPlaying && m.Src.clip == m.Clip;

        // Where the voice comes from: the head, or near the ground for a Crawler.
        private static Vector3 MouthAt(Mouth m)
        {
            bool low = m.Ai != null && m.Ai.Type == ZombieType.Crawler;
            return m.Actor.transform.position + Vector3.up * (low ? 0.45f : 1.6f);
        }

        private Mouth MouthOf(Actor a)
        {
            if (_mouths.TryGetValue(a, out Mouth m)) return m;
            uint h = Hash(a);
            m = new Mouth
            {
                Actor = a,
                Ai = a.GetComponent<ZombieAI>(),
                Throat = ThroatOf(h),
                Pitch = PitchOf(h),
                Volume = 0.9f + 0.2f * ((h >> 16) & 0xFF) / 255f,
                Talk = 0.75f + 0.6f * ((h >> 24) & 0xFF) / 255f,
                NextAt = _clock + Random.Range(0.3f, 4f),
            };
            if (m.Ai == null) m.Body = a.GetComponentInChildren<HumanoidAnimator>();
            _mouths[a] = m;
            return m;
        }

        // Forgets the dead long out of earshot (their voices come back the same when they return: they hash from the id).
        private void Prune()
        {
            _pruneAt = _clock + 5f;
            _stale.Clear();
            foreach (KeyValuePair<Actor, Mouth> kv in _mouths)
            {
                Mouth m = kv.Value;
                if (m.Actor == null || (_clock - m.SeenAt > 20f && _clock >= m.BusyUntil)) _stale.Add(kv.Key);
            }
            for (int i = 0; i < _stale.Count; i++)
            {
                if (_mouths.TryGetValue(_stale[i], out Mouth m)) _speaking.Remove(m);
                _mouths.Remove(_stale[i]);
            }
        }

        // A zombie's own pitch, 0.86..1.14 (the size of its throat), and which throat it has (its own bits of the hash).
        private static float PitchOf(uint hash) => 0.86f + 0.28f * (hash & 0xFFFF) / 65535f;
        private static int ThroatOf(uint hash) => (int)(Mix(hash ^ 0x9E3779B9u) % (uint)ZombieThroats.Count);

        // Its network id where it has one (the same voice on every peer), else its instance; well mixed.
        private static uint Hash(Actor a) => Mix(a.NetId != 0 ? a.NetId : (uint)a.GetHashCode());

        private static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return x;
        }
    }
}
