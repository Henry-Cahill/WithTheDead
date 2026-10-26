using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The game's sound API: positional one-shots, flat sounds and attached loops by id (see <see cref="SfxIds"/>).
    /// Every positional sound is heard from the survivor's head (the listener rides the head bone, never the camera —
    /// <see cref="ListenerRig"/>). Each sound belongs to a bus (effects, ambience, music, interface) with its own
    /// settings slider; the mixer (<see cref="SfxMixer"/>) scales it by its bus, ducks the beds under loud events,
    /// muffles it through walls and with distance, swaps in a distant version far away, and freezes the world's sounds
    /// while the game is paused. Volumes are therefore set through <see cref="SetVolume"/> or <see cref="FadeTo"/> (a
    /// direct write to AudioSource.volume is taken as the new base volume). One-shot sources are pooled. With no backend
    /// registered every call is a silent no-op, so gameplay never depends on audio.
    /// </summary>
    public static class Sfx
    {
        public static ISfxBackend Backend;

        public static bool Enabled => Backend != null;

        /// <summary>Where the survivor hears from: the ears on the head bone, else the active listener.</summary>
        public static Vector3 ListenerPosition => SfxMixer.Ears;

        /// <summary>The routing of <paramref name="id"/> (never null).</summary>
        public static SfxMeta Meta(string id)
        {
            if (Backend == null || string.IsNullOrEmpty(id)) return SfxMeta.Default;
            return Backend.Meta(id) ?? SfxMeta.Default;
        }

        /// <summary>True when <paramref name="id"/> can start without being rendered on the spot.</summary>
        public static bool Ready(string id) => Backend != null && !string.IsNullOrEmpty(id) && Backend.IsReady(id);

        /// <summary>Asks for <paramref name="id"/> to be built in the background ahead of the queue.</summary>
        public static void Request(string id)
        {
            if (Backend != null && !string.IsNullOrEmpty(id)) Backend.Request(id);
        }

        internal static AudioClip Clip(string id)
        {
            if (Backend == null || string.IsNullOrEmpty(id)) return null;
            try { return Backend.Get(id); }
            catch (System.Exception e) { Debug.LogException(e); return null; }
        }

        #region One-shots
        /// <summary>A positional one-shot. Returns the source (null when silent or out of earshot).</summary>
        public static AudioSource Play(string id, Vector3 position, float volume = 1f, float pitch = 1f, float maxDistance = 45f)
            => SfxMixer.PlayAt(id, position, volume, pitch, maxDistance, true);

        /// <summary>A flat (non-positional) sound: menus, the survivor's own breathing.</summary>
        public static AudioSource PlayUI(string id, float volume = 1f, float pitch = 1f) => PlayFlat(id, volume, pitch);

        /// <summary>A flat sound, optionally panned (-1 left .. +1 right).</summary>
        public static AudioSource PlayFlat(string id, float volume = 1f, float pitch = 1f, float pan = 0f)
        {
            AudioSource src = SfxMixer.PlayAt(id, Vector3.zero, volume, pitch, 10f, false);
            if (src != null) src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            return src;
        }

        /// <summary>A positional one-shot <paramref name="delay"/> seconds of game time from now (thunder after the flash).</summary>
        public static void PlayDelayed(string id, Vector3 position, float delay, float volume = 1f, float pitch = 1f, float maxDistance = 45f)
            => SfxMixer.Schedule(id, position, delay, volume, pitch, maxDistance, true, true);
        #endregion

        #region Loops and owned sources
        /// <summary>A looping sound that follows a transform until <see cref="Stop"/> (an engine, a rotor, an anomaly's hum).</summary>
        public static AudioSource Loop(string id, Transform follow, float volume = 1f, float maxDistance = 25f, float pitch = 1f)
        {
            if (follow == null) return null;
            AudioClip clip = Clip(id);
            if (clip == null) return null;
            return SfxMixer.StartLoop(clip, Meta(id), follow, volume, pitch, maxDistance, true, "SfxLoop " + id);
        }

        /// <summary>A flat looping bed (wind, rain, a mask's breathing) until <see cref="Stop"/>.</summary>
        public static AudioSource LoopFlat(string id, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = Clip(id);
            if (clip == null) return null;
            return SfxMixer.StartLoop(clip, Meta(id), null, volume, pitch, 10f, false, "SfxBed " + id);
        }

        /// <summary>
        /// A source the caller drives itself (assigns clips, schedules them) but which still answers to its bus, the
        /// ducking and the pause: the music uses these. Positional when <paramref name="parent"/> is given and
        /// <paramref name="spatial"/> is true. Release it with <see cref="Stop"/>.
        /// </summary>
        public static AudioSource CreateSource(string name, SfxBus bus, Transform parent = null, bool spatial = false, float maxDistance = 25f)
            => SfxMixer.CreateOwned(name, bus, parent, spatial, maxDistance);

        /// <summary>Stops a sound; loops and owned sources are destroyed, pooled one-shots go back to the pool.</summary>
        public static void Stop(AudioSource src) => SfxMixer.Stop(src);
        #endregion

        #region Control
        /// <summary>Sets a playing sound's base volume (before its bus, ducking and occlusion).</summary>
        public static void SetVolume(AudioSource src, float volume) => SfxMixer.SetVolume(src, volume);

        /// <summary>A playing sound's base volume (before its bus, ducking and occlusion).</summary>
        public static float GetVolume(AudioSource src) => SfxMixer.GetVolume(src);

        /// <summary>Glides a sound's base volume to <paramref name="volume"/> over <paramref name="seconds"/> (real time);
        /// with <paramref name="stopAtZero"/>, a fade to silence stops it (destroying a loop).</summary>
        public static void FadeTo(AudioSource src, float volume, float seconds, bool stopAtZero = false) => SfxMixer.FadeTo(src, volume, seconds, stopAtZero);

        /// <summary>An extra low-pass on one sound (a bed heard from indoors); 22000 or more removes it.</summary>
        public static void SetLowPass(AudioSource src, float hz) => SfxMixer.SetLowPass(src, hz);

        /// <summary>Ducks ambience and music by <paramref name="db"/> (negative) for <paramref name="hold"/> seconds, recovering over <paramref name="release"/>.</summary>
        public static void Duck(float db, float hold = 0.6f, float release = 2.5f) => SfxMixer.Duck(db, hold, release);

        /// <summary>A blast too close: the mix is muffled and the ears ring, by <paramref name="strength"/> 0..1, recovering over a few seconds.</summary>
        public static void Concuss(float strength) => SfxMixer.Concuss(strength);

        /// <summary>The current gain of a bus (its slider times ducking and the pause), for diagnostics and tools.</summary>
        public static float BusGain(SfxBus bus) => SfxMixer.BusGain(bus);
        #endregion
    }
}
