using System.Reflection;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Installs the synthesiser as the game's sound backend, starts building every sound in the background, and wires
    /// the sounds no single system owns. Each part of the soundscape has its own director: footsteps
    /// (<see cref="FootstepAudio"/>), the survivor's body (<see cref="BodyAudio"/>), the dead (<see cref="ZombieVoices"/>),
    /// the world's ambience and weather (<see cref="AmbienceDirector"/>), the fight (<see cref="WeaponAudio"/>) and the
    /// score (<see cref="MusicDirector"/>); the mixer (<see cref="SfxMixer"/>) plays them all, in the space the survivor
    /// stands in (<see cref="SfxAcoustics"/>).
    /// </summary>
    public sealed class AudioModule : IGameModule
    {
        private static bool s_SessionStarted;

        public int Order => -50;

        public void Init()
        {
            var synth = new SfxSynth();
            Sfx.Backend = synth;
            _ = GameSettings.Current;   // loading applies the saved master volume
#if UNITY_EDITOR || DEBUG
            foreach (FieldInfo f in typeof(SfxIds).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.GetValue(null) is string id && !synth.Has(id))
                    Debug.LogWarning("[CloneZ] No sound recipe for '" + id + "'.");
#endif
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<ListenerRig>() == null) p.gameObject.AddComponent<ListenerRig>();
                FootstepAudio.OnPlayerBuilt(p);
                BodyAudio.OnPlayerBuilt(p);
            };
            GameEvents.ItemLooted += (stack, source) => Sfx.PlayUI(SfxIds.Pickup, 0.45f, Random.Range(0.92f, 1.08f));
            StartSession();
        }

        // Modules initialise once per run of the game, but in the editor a run spans several play sessions (domain
        // reload is off): statics and event hooks survive, while every object the last session made is gone. So the
        // per-session half — background builds, the mixer, the directors — starts again with each session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession() => s_SessionStarted = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResumeSession()
        {
            if (Sfx.Backend != null) StartSession();
        }

        /// <summary>Initialise audio subsystem for a new play session.</summary>
        private static void StartSession()
        {
            // Prevent double‑initialisation across multiple calls within the same session.
            if (s_SessionStarted) return;
            s_SessionStarted = true;
            // Clear any pending audio job queues from previous sessions.
            AudioJobs.Reset();
            // Ensure we only have a single shutdown handler registered.
            Application.quitting -= AudioJobs.Shutdown;
            Application.quitting += AudioJobs.Shutdown;
            // If the backend is the synthesiser, reset its state and warm up caches.
            if (Sfx.Backend is SfxSynth synth)
            {
                // Reset runtime state (e.g., playing voices) before reuse.
                synth.ResetRuntime();
                // Pre‑warm common sound recipes to avoid hitches on first play.
                synth.Prewarm();
            }
            // Guarantee each audio subsystem has its singleton instance created.
            SfxMixer.Ensure();
            SfxAcoustics.Ensure();
            WeaponAudio.Ensure();
            FootstepAudio.Ensure();
            BodyAudio.Ensure();
            ZombieVoices.Ensure();
            AmbienceDirector.Ensure();
            MusicDirector.Ensure();
        }
    }

    /// <summary>
    /// The survivor hears from their own head, not from the camera: in the isometric view the camera hangs high
    /// above, and a listener there would make every sound distant and every direction wrong. The ears ride the head
    /// bone; they turn with the camera's heading so left on screen is left in the speakers.
    /// </summary>
    public sealed class ListenerRig : MonoBehaviour
    {
        private Transform _ears;
        private AudioListener _listener;
        private Transform _head;
        private PlayerAvatar _avatar;
        private Camera _mutedCam;

        /// <summary>Current world position of the ears attached to the player's head.</summary>
        public static Vector3 Position { get; private set; }
        /// <summary>Whether the ListenerRig is currently active in the scene.</summary>
        public static bool Active { get; private set; }
        /// <summary>The ears themselves (the AudioListener's transform), or null with no rig.</summary>
        public static Transform Ears { get; private set; }

        private void Start()
        {
            _avatar = GetComponent<PlayerAvatar>();
            var go = new GameObject("Ears");
            _ears = go.transform;
            _listener = go.AddComponent<AudioListener>();
            Ears = _ears;
            Active = true;
        }

        /// <summary>Update listener position each frame after all other transforms have moved.</summary>
        private void LateUpdate()
        {
            // Abort if the ears GameObject was destroyed (e.g., during scene unload).
            if (_ears == null) return;
            // Lazily cache the head bone transform for the first time a player avatar is present.
            if (_head == null && _avatar != null && _avatar.Rig != null && _avatar.Rig.Bones != null)
                _head = _avatar.Rig.Bone(HumanBone.Head);

            // Track the current main camera; switch AudioListener components when the camera changes.
            Camera cam = Camera.main;
            if (cam != _mutedCam)
            {
                // Re‑enable the previously muted camera's listener.
                if (_mutedCam != null) SetListener(_mutedCam, true);
                _mutedCam = cam;
                // Disable the new camera's listener so only the ears listener is active.
                if (cam != null) SetListener(cam, false);
            }
            // Ensure the AudioListener attached to the ears is enabled.
            if (!_listener.enabled) _listener.enabled = true;

            // Determine ear position: follow the head bone if available, otherwise use a default offset above the player.
            Vector3 pos = _head != null ? _head.position : transform.position + Vector3.up * 1.65f;
            // Base rotation on the player object; in first‑person view follow camera rotation, otherwise yaw‑only.
            Quaternion rot = transform.rotation;
            if (cam != null) rot = ViewState.FirstPersonLike ? cam.transform.rotation : Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            // Apply the calculated position and rotation to the ears transform.
            _ears.SetPositionAndRotation(pos, rot);
            // Expose the current ear world position for other systems.
            Position = pos;
        }

        private static void SetListener(Camera cam, bool on)
        {
            var l = cam.GetComponent<AudioListener>();
            if (l != null) l.enabled = on;
        }

        private void OnDestroy()
        {
            Active = false;
            if (Ears == _ears) Ears = null;
            if (_mutedCam != null) SetListener(_mutedCam, true);
            if (_ears != null) Destroy(_ears.gameObject);
        }
    }
}
