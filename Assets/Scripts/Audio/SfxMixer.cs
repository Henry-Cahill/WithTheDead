using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    /// <summary>
    /// The mixer behind <see cref="Sfx"/>. It owns every source the game's sounds play from ("voices") and, every frame
    /// after gameplay, sets each one's volume from its base volume × its bus (the settings sliders, ducking under loud
    /// events, the pause) × occlusion, and its low-pass from walls between it and the ears, air absorption over distance,
    /// concussion and the pause. Occlusion is probed with a few line casts per frame across the voices (world geometry is
    /// on the Default layer; characters are not), and a sound is probed as it starts, so a shot behind a wall is muffled
    /// from its first sample. While the game is paused the world's sounds freeze, the beds sink and the music softens.
    /// It also completes the background audio jobs (<see cref="AudioJobs"/>) and plays delayed sounds.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class SfxMixer : MonoBehaviour
    {
        internal sealed class Voice
        {
            public AudioSource Src, Key;
            public AudioLowPassFilter Lp;
            public SfxMeta Meta = SfxMeta.Default;
            public SfxBus Bus;
            public bool Pooled, Spatial, PausedByMixer, StopAtFadeEnd;
            public float Base = 1f, Applied = -1f, ManualLp = FullBand;
            public float Occlusion, OcclusionTarget, NextProbe;
            public float FadeTarget, FadeRate;
            public float StartedAt;
        }

        private struct Delayed
        {
            public float At;
            public string Id;
            public Vector3 Pos;
            public float Volume, Pitch, Range;
            public bool Spatial, Companions;
        }

        private const int MaxPool = 64, MaxDelayed = 256, ProbesPerFrame = 6;
        private const float FullBand = 22000f;
        // World geometry only: characters sit on Ignore Raycast, so they never muffle anything.
        private const int OcclusionMask = 1 << 0;

        private static SfxMixer s_Instance;
        private static Transform s_Root;
        private static readonly List<Voice> s_Voices = new List<Voice>(128);
        private static readonly List<Voice> s_Pool = new List<Voice>(MaxPool);
        private static readonly Dictionary<AudioSource, Voice> s_BySource = new Dictionary<AudioSource, Voice>(128);
        private static readonly List<Delayed> s_Delayed = new List<Delayed>(16);
        private static readonly float[] s_BusGain = { 1f, 1f, 1f, 1f };
        private static AnimationCurve s_Rolloff;
        private static Vector3 s_Ears;
        private static bool s_HasEars, s_Paused;
        private static float s_DuckDb, s_DuckHoldUntil, s_DuckRelease = 4f, s_Concuss, s_PauseStartedAt;

        private AudioListener _listener;
        private static Camera s_MainCam; // Cached main camera to avoid per-frame lookup
        private float _findListenerAt;
        private int _probeCursor;

        /// <summary>Where the survivor hears from: the ears on the head bone, else the active listener.</summary>
        internal static Vector3 Ears => s_Ears;

        internal static float DistanceToEars(Vector3 p) => s_HasEars ? Vector3.Distance(s_Ears, p) : 0f;

        internal static float BusGain(SfxBus bus) => s_BusGain[(int)bus];

        /// <summary>Creates the mixer (and the [Sfx] root every pooled sound plays under) if it is not running.</summary>
        internal static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[Sfx]");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            else go.hideFlags = HideFlags.HideAndDontSave;
            s_Root = go.transform;
            s_Voices.Clear();
            s_Pool.Clear();
            s_BySource.Clear();
            s_Delayed.Clear();
            // Statics outlive a play session in the editor (no domain reload); the clocks they compare against restart.
            s_DuckDb = 0f;
            s_DuckHoldUntil = 0f;
            s_DuckRelease = 4f;
            s_Concuss = 0f;
            s_Paused = false;
            s_PauseStartedAt = 0f;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_Instance = go.AddComponent<SfxMixer>();
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        // A new scene (quitting to the title, leaving a raid): the old world's one-shots, anything the pause froze and
        // anything still scheduled end with it, as they did when the pool lived in the scene.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            s_Delayed.Clear();
            for (int i = 0; i < s_Pool.Count; i++)
            {
                Voice v = s_Pool[i];
                if (v.Src == null || (v.Bus != SfxBus.Effects && v.Bus != SfxBus.Ambience && !v.PausedByMixer)) continue;
                v.Src.Stop();
                v.PausedByMixer = false;
            }
        }

        #region Starting sounds
        internal static AudioSource PlayAt(string id, Vector3 position, float volume, float pitch, float maxDistance, bool spatial, bool companions = true, int variation = -1)
        {
            if (Sfx.Backend == null || string.IsNullOrEmpty(id)) return null;
            SfxMeta meta = Sfx.Meta(id);
            float dist = spatial ? DistanceToEars(position) : 0f;
            if (spatial && meta.Far != null && dist > meta.FarDistance)
            {
                id = meta.Far;
                meta = Sfx.Meta(id);
                variation = -1;
            }
            float range = Mathf.Max(3f, Mathf.Max(maxDistance, meta.MinRange));
            if (spatial && s_HasEars && dist > range * 1.1f) return null;
            AudioClip clip = Sfx.Clip(id, variation);
            if (clip == null) return null;

            Ensure();
            Voice v = Rent();
            AudioSource src = v.Src;
            Transform t = src.transform;
            if (t.parent != s_Root) t.SetParent(s_Root, false);
            t.position = position;
            Configure(v, meta, volume, pitch, range, spatial);
            src.loop = false;
            src.clip = clip;
            if (spatial) Prime(v, position);
            Mix(v, dist);
            src.Play();
            v.StartedAt = Time.unscaledTime;
            Consequences(meta, position, dist, volume, pitch, range, spatial, companions);
            return src;
        }

        internal static void Schedule(string id, Vector3 position, float delay, float volume, float pitch, float maxDistance, bool spatial, bool companions)
        {
            if (Sfx.Backend == null || string.IsNullOrEmpty(id)) return;
            if (delay <= 0f) { PlayAt(id, position, volume, pitch, maxDistance, spatial, companions); return; }
            Ensure();
            if (s_Delayed.Count >= MaxDelayed) return;
            s_Delayed.Add(new Delayed
            {
                At = Time.time + delay, Id = id, Pos = position, Volume = volume, Pitch = pitch, Range = maxDistance,
                Spatial = spatial, Companions = companions,
            });
        }

        internal static AudioSource StartLoop(AudioClip clip, SfxMeta meta, Transform parent, float volume, float pitch, float range, bool spatial, string name)
        {
            Ensure();
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : s_Root, false);
            Voice v = Create(go, false);
            Configure(v, meta, volume, pitch, Mathf.Max(range, meta.MinRange), spatial);
            AudioSource src = v.Src;
            src.loop = true;
            src.clip = clip;
            // Two of the same loop (two anomalies, two engines) never start in phase.
            if (clip.samples > 1) src.timeSamples = Random.Range(0, clip.samples);
            if (spatial) Prime(v, go.transform.position);
            Mix(v, spatial ? DistanceToEars(go.transform.position) : 0f);
            src.Play();
            v.StartedAt = Time.unscaledTime;
            return src;
        }

        internal static AudioSource CreateOwned(string name, SfxBus bus, Transform parent, bool spatial, float range)
        {
            Ensure();
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : s_Root, false);
            Voice v = Create(go, false);
            bool positional = spatial && parent != null;
            var meta = new SfxMeta { Bus = bus, Priority = bus == SfxBus.Music ? 0 : 64, NoOcclusion = !positional, Reverb = positional ? 1f : 0f };
            Configure(v, meta, 1f, 1f, range, positional);
            Mix(v, positional ? DistanceToEars(go.transform.position) : 0f);
            return v.Src;
        }

        internal static void Stop(AudioSource src)
        {
            if (src == null) return;
            if (s_BySource.TryGetValue(src, out Voice v))
            {
                if (v.Pooled)
                {
                    src.Stop();
                    v.PausedByMixer = false;
                    return;
                }
                Forget(v);
                Kill(src.gameObject);
                return;
            }
            src.Stop();
            if (src.loop) Kill(src.gameObject);
        }
        #endregion

        #region Control
        internal static void SetVolume(AudioSource src, float volume)
        {
            if (src == null) return;
            if (s_BySource.TryGetValue(src, out Voice v))
            {
                v.Base = Mathf.Clamp01(volume);
                v.FadeRate = 0f;
            }
            else src.volume = Mathf.Clamp01(volume);
        }

        internal static float GetVolume(AudioSource src)
        {
            if (src == null) return 0f;
            return s_BySource.TryGetValue(src, out Voice v) ? v.Base : src.volume;
        }

        internal static void FadeTo(AudioSource src, float volume, float seconds, bool stopAtZero)
        {
            if (src == null) return;
            if (!s_BySource.TryGetValue(src, out Voice v)) { src.volume = Mathf.Clamp01(volume); return; }
            v.FadeTarget = Mathf.Clamp01(volume);
            v.FadeRate = Mathf.Abs(v.FadeTarget - v.Base) / Mathf.Max(0.01f, seconds);
            v.StopAtFadeEnd = stopAtZero;
            if (v.FadeRate <= 0f) { v.Base = v.FadeTarget; v.FadeRate = 0.0001f; }
        }

        internal static void SetLowPass(AudioSource src, float hz)
        {
            if (src != null && s_BySource.TryGetValue(src, out Voice v)) v.ManualLp = Mathf.Clamp(hz, 80f, FullBand);
        }

        internal static void Duck(float db, float hold, float release)
        {
            if (db >= 0f) return;
            if (db < s_DuckDb)
            {
                s_DuckDb = Mathf.Max(-40f, db);
                s_DuckRelease = Mathf.Abs(s_DuckDb) / Mathf.Max(0.1f, release);
            }
            s_DuckHoldUntil = Mathf.Max(s_DuckHoldUntil, Time.unscaledTime + Mathf.Max(0f, hold));
        }

        internal static void Concuss(float strength)
        {
            strength = Mathf.Clamp01(strength);
            if (strength < 0.05f) return;
            if (strength > s_Concuss + 0.15f) PlayAt(SfxIds.EarRing, Vector3.zero, 0.2f + 0.5f * strength, Random.Range(0.97f, 1.03f), 10f, false);
            s_Concuss = Mathf.Max(s_Concuss, strength);
            Duck(-14f * strength, 0.8f, 4f);
        }
        #endregion

        #region Voices
        private static AnimationCurve Rolloff
        {
            get
            {
                if (s_Rolloff != null) return s_Rolloff;
                // Near sounds stand out and far ones fade in a long, quiet tail (closer to how distance sounds than a
                // straight line), still reaching silence at the source's range so out-of-range sounds cost nothing.
                float[] t = { 0f, 0.05f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
                float[] y = { 1f, 0.86f, 0.74f, 0.56f, 0.43f, 0.33f, 0.25f, 0.18f, 0.12f, 0.07f, 0.03f, 0f };
                var keys = new Keyframe[t.Length];
                for (int i = 0; i < t.Length; i++)
                {
                    int a = Mathf.Max(0, i - 1), b = Mathf.Min(t.Length - 1, i + 1);
                    float slope = (y[b] - y[a]) / Mathf.Max(1e-4f, t[b] - t[a]);
                    keys[i] = new Keyframe(t[i], y[i], slope, slope);
                }
                s_Rolloff = new AnimationCurve(keys);
                return s_Rolloff;
            }
        }

        private static Voice Create(GameObject go, bool pooled)
        {
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Custom;
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Rolloff);
            var lp = go.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = FullBand;
            lp.lowpassResonanceQ = 1f;
            lp.enabled = false;
            var v = new Voice { Src = src, Key = src, Lp = lp, Pooled = pooled };
            s_Voices.Add(v);
            s_BySource[src] = v;
            if (pooled) s_Pool.Add(v);
            return v;
        }

        // A free pooled source, a new one while the pool grows, else the least important (then oldest) one is cut.
        private static Voice Rent()
        {
            for (int i = 0; i < s_Pool.Count; i++)
            {
                Voice v = s_Pool[i];
                if (v.Src != null && !v.Src.isPlaying && !v.PausedByMixer) return v;
            }
            if (s_Pool.Count < MaxPool)
            {
                var go = new GameObject("Sfx");
                go.transform.SetParent(s_Root, false);
                return Create(go, true);
            }
            Voice steal = null;
            for (int i = 0; i < s_Pool.Count; i++)
            {
                Voice v = s_Pool[i];
                if (v.Src == null) continue;
                if (steal == null || v.Meta.Priority > steal.Meta.Priority || (v.Meta.Priority == steal.Meta.Priority && v.StartedAt < steal.StartedAt)) steal = v;
            }
            if (steal == null)
            {
                var go = new GameObject("Sfx");
                go.transform.SetParent(s_Root, false);
                return Create(go, true);
            }
            steal.Src.Stop();
            steal.PausedByMixer = false;
            return steal;
        }

        private static void Configure(Voice v, SfxMeta meta, float volume, float pitch, float range, bool spatial)
        {
            AudioSource src = v.Src;
            v.Meta = meta ?? SfxMeta.Default;
            v.Bus = v.Meta.Bus;
            v.Spatial = spatial;
            v.Base = Mathf.Clamp01(volume);
            v.Applied = -1f;
            v.ManualLp = FullBand;
            v.FadeRate = 0f;
            v.StopAtFadeEnd = false;
            v.Occlusion = v.OcclusionTarget = 0f;
            v.NextProbe = 0f;
            v.PausedByMixer = false;
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.priority = Mathf.Clamp(v.Meta.Priority, 0, 256);
            src.spatialBlend = spatial ? 1f : 0f;
            src.dopplerLevel = spatial ? v.Meta.Doppler : 0f;
            src.spread = spatial ? v.Meta.Spread : 0f;
            src.minDistance = Mathf.Min(2f, range * 0.1f);
            src.maxDistance = Mathf.Max(3f, range);
            src.reverbZoneMix = spatial ? Mathf.Clamp01(v.Meta.Reverb) : 0f;
            src.panStereo = 0f;
        }

        private static void Forget(Voice v)
        {
            s_Voices.Remove(v);
            s_Pool.Remove(v);
            if (!ReferenceEquals(v.Key, null)) s_BySource.Remove(v.Key);
        }

        private static void Kill(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        // Ducking, a blast too close, and the follow-up sound a recipe asks for.
        private static void Consequences(SfxMeta meta, Vector3 position, float dist, float volume, float pitch, float range, bool spatial, bool companions)
        {
            if (meta.DuckDb < 0f && dist < meta.DuckRange) Duck(meta.DuckDb * Mathf.Sqrt(1f - dist / meta.DuckRange), meta.DuckHold, meta.DuckRelease);
            if (spatial && s_HasEars && meta.ConcussRange > 0f && dist < meta.ConcussRange) Concuss(1f - dist / meta.ConcussRange);
            if (companions && !string.IsNullOrEmpty(meta.Then) && dist <= range)
            {
                float delay = Mathf.Max(0.01f, meta.ThenDelay + Random.Range(-meta.ThenJitter, meta.ThenJitter));
                Schedule(meta.Then, position, delay, volume * meta.ThenVolume, pitch, Mathf.Max(8f, range * 0.5f), spatial, false);
            }
        }
        #endregion

        #region Every frame
        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            AudioJobs.Pump(2.5f);
            UpdateEars();
            bool paused = Time.timeScale <= 0f;
            if (paused && !s_Paused) s_PauseStartedAt = Time.unscaledTime;
            s_Paused = paused;

            if (Time.unscaledTime > s_DuckHoldUntil) s_DuckDb = Mathf.MoveTowards(s_DuckDb, 0f, s_DuckRelease * dt);
            s_Concuss = Mathf.MoveTowards(s_Concuss, 0f, dt / 6f);
            GameSettings settings = GameSettings.Current;
            s_BusGain[(int)SfxBus.Effects] = Mathf.Clamp01(settings.EffectsVolume);
            s_BusGain[(int)SfxBus.Ambience] = Mathf.Clamp01(settings.AmbienceVolume) * Db(s_DuckDb) * (s_Paused ? 0.35f : 1f);
            s_BusGain[(int)SfxBus.Music] = Mathf.Clamp01(settings.MusicVolume) * Db(s_DuckDb * 0.6f) * (s_Paused ? 0.6f : 1f);
            s_BusGain[(int)SfxBus.Interface] = Mathf.Clamp01(settings.InterfaceVolume);

            RunDelayed();

            for (int i = s_Voices.Count - 1; i >= 0; i--)
            {
                Voice v = s_Voices[i];
                if (v.Src != null) continue;
                s_Voices.RemoveAt(i);
                s_Pool.Remove(v);
                s_BySource.Remove(v.Key);
            }

            Probe();

            for (int i = s_Voices.Count - 1; i >= 0; i--)
            {
                Voice v = s_Voices[i];
                AudioSource src = v.Src;
                // Callers may still flatten a positional loop by hand (spatialBlend = 0); follow what the source does.
                bool spatial = src.spatialBlend >= 0.5f;
                if (spatial != v.Spatial)
                {
                    v.Spatial = spatial;
                    v.Occlusion = v.OcclusionTarget = 0f;
                    src.reverbZoneMix = spatial ? Mathf.Clamp01(v.Meta.Reverb) : 0f;
                }
                // The pause freezes the world as it was; sounds started while paused (the menus') play on.
                if (s_Paused && v.Bus == SfxBus.Effects && (v.PausedByMixer || v.StartedAt < s_PauseStartedAt))
                {
                    if (src.isPlaying) { src.Pause(); v.PausedByMixer = true; }
                    continue;
                }
                if (v.PausedByMixer)
                {
                    v.PausedByMixer = false;
                    src.UnPause();
                }
                if (v.Pooled && !src.isPlaying) continue;

                // A direct write to AudioSource.volume since last frame is taken as the new base volume.
                if (v.Applied >= 0f && Mathf.Abs(src.volume - v.Applied) > 0.0005f) v.Base = Mathf.Clamp01(src.volume);
                if (v.FadeRate > 0f)
                {
                    v.Base = Mathf.MoveTowards(v.Base, v.FadeTarget, v.FadeRate * dt);
                    if (Mathf.Approximately(v.Base, v.FadeTarget))
                    {
                        v.FadeRate = 0f;
                        if (v.StopAtFadeEnd && v.Base <= 0.0001f)
                        {
                            Stop(src);
                            continue;
                        }
                    }
                }
                v.Occlusion = Mathf.MoveTowards(v.Occlusion, v.OcclusionTarget, dt * 4f);
                Mix(v, v.Spatial ? DistanceToEars(src.transform.position) : 0f);
            }
        }

        private void UpdateEars()
        {
            if (ListenerRig.Active)
            {
                s_Ears = ListenerRig.Position;
                s_HasEars = true;
                return;
            }
            if (_listener == null || !_listener.isActiveAndEnabled)
            {
                _listener = null;
                if (s_MainCam == null || !s_MainCam.isActiveAndEnabled)
                    s_MainCam = Camera.main;
                if (s_MainCam != null) _listener = s_MainCam.GetComponent<AudioListener>();
                if ((_listener == null || !_listener.isActiveAndEnabled) && Time.unscaledTime >= _findListenerAt)
                {
                    _findListenerAt = Time.unscaledTime + 1f;
                    _listener = FindAnyObjectByType<AudioListener>();
                    // Update cached camera if we found a listener via FindAnyObjectByType
                    if (_listener != null)
                    {
                        var cam = _listener.GetComponent<Camera>();
                        if (cam != null) s_MainCam = cam;
                    }
                }
            }
            s_HasEars = _listener != null && _listener.isActiveAndEnabled;
            if (s_HasEars) s_Ears = _listener.transform.position;
        }

        private static void RunDelayed()
        {
            if (s_Delayed.Count == 0) return;
            float now = Time.time;
            for (int i = s_Delayed.Count - 1; i >= 0; i--)
            {
                if (i >= s_Delayed.Count || now < s_Delayed[i].At) continue;
                Delayed d = s_Delayed[i];
                s_Delayed.RemoveAt(i);
                PlayAt(d.Id, d.Pos, d.Volume, d.Pitch, d.Range, d.Spatial, d.Companions);
            }
        }

        // The volume and low-pass a voice should have right now.
        private static void Mix(Voice v, float dist)
        {
            AudioSource src = v.Src;
            // A blast deafens the world, not the score or the interface; anything placed in the world (the jukebox's
            // music too) is walled off and dulled by distance.
            bool deafens = !v.Meta.NoOcclusion && v.Bus != SfxBus.Interface && v.Bus != SfxBus.Music;
            bool placed = v.Spatial && !v.Meta.NoOcclusion && v.Bus != SfxBus.Interface;
            float gain = s_BusGain[(int)v.Bus];
            if (deafens) gain *= 1f - 0.45f * s_Concuss;
            if (placed) gain *= 1f - 0.5f * v.Occlusion;
            float vol = Mathf.Clamp01(v.Base * gain);
            if (Mathf.Abs(vol - src.volume) > 0.0005f) src.volume = vol;
            v.Applied = src.volume;

            float cutoff = v.ManualLp;
            if (placed)
            {
                // Air soaks up the highs of anything far off; walls take far more.
                if (dist > 15f) cutoff = Mathf.Min(cutoff, Mathf.Max(1800f, FullBand * Mathf.Exp(-(dist - 15f) / 160f)));
                if (v.Occlusion > 0.001f) cutoff = Mathf.Min(cutoff, FullBand * Mathf.Pow(600f / FullBand, v.Occlusion));
            }
            if (deafens && s_Concuss > 0.01f) cutoff = Mathf.Min(cutoff, FullBand * Mathf.Pow(450f / FullBand, Mathf.Pow(s_Concuss, 0.7f)));
            if (s_Paused && v.Bus == SfxBus.Ambience) cutoff = Mathf.Min(cutoff, 1200f);

            AudioLowPassFilter lp = v.Lp;
            if (lp == null) return;
            if (cutoff >= 20000f)
            {
                if (lp.enabled) lp.enabled = false;
                return;
            }
            if (!lp.enabled) lp.enabled = true;
            if (Mathf.Abs(lp.cutoffFrequency - cutoff) > cutoff * 0.01f) lp.cutoffFrequency = cutoff;
        }

        // A few occlusion probes per frame, round-robin over the playing positional voices.
        private void Probe()
        {
            if (!s_HasEars) return;
            int n = s_Voices.Count, budget = ProbesPerFrame;
            float now = Time.unscaledTime;
            for (int k = 0; k < n && budget > 0; k++)
            {
                _probeCursor = (_probeCursor + 1) % n;
                Voice v = s_Voices[_probeCursor];
                if (!v.Spatial || v.Meta.NoOcclusion || now < v.NextProbe || !v.Src.isPlaying) continue;
                v.OcclusionTarget = Occlusion(v.Src.transform.position);
                v.NextProbe = now + 0.1f + Random.value * 0.08f;
                budget--;
            }
        }

        private static void Prime(Voice v, Vector3 position)
        {
            if (v.Meta.NoOcclusion || !s_HasEars) return;
            v.Occlusion = v.OcclusionTarget = Occlusion(position);
            v.NextProbe = Time.unscaledTime + 0.1f + Random.value * 0.08f;
        }

        /// <summary>
        /// How walled-off a point is from the ears, 0..1: one line cast straight there; if that is blocked, two more past
        /// either side of it, so sound bending round a corner or a pillar is only partly muffled.
        /// </summary>
        internal static float Occlusion(Vector3 source)
        {
            if (!s_HasEars) return 0f;
            Vector3 d = source - s_Ears;
            float dist = d.magnitude;
            if (dist < 1.5f) return 0f;
            if (!Blocked(s_Ears, source)) return 0f;
            Vector3 side = Vector3.Cross(d / dist, Vector3.up);
            side = side.sqrMagnitude < 0.01f ? Vector3.right : side.normalized;
            int blocked = 1;
            if (Blocked(s_Ears, source + side * 0.9f + Vector3.up * 0.3f)) blocked++;
            if (Blocked(s_Ears, source - side * 0.9f + Vector3.up * 0.3f)) blocked++;
            return blocked / 3f;
        }

        // Blocked by world geometry short of the source itself (a source inside its own wall or body is not "behind" it).
        private static bool Blocked(Vector3 from, Vector3 to)
        {
            if (!Physics.Linecast(from, to, out RaycastHit hit, OcclusionMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.distance < Vector3.Distance(from, to) - 0.6f;
        }

        private static float Db(float db) => db >= 0f ? 1f : Mathf.Pow(10f, db / 20f);
        #endregion
    }
}
