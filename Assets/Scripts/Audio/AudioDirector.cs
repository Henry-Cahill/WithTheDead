using System.Reflection;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Installs the synthesiser as the game's sound backend, starts building every sound in the background, and wires
    /// the sounds no single system owns. Each part of the soundscape has its own director: footsteps
    /// (<see cref="FootstepAudio"/>), the survivor's body (<see cref="BodyAudio"/>), the dead (<see cref="ZombieVoices"/>),
    /// the world's ambience and weather (<see cref="AmbienceDirector"/>), the fight (<see cref="WeaponAudio"/>) and the
    /// score (<see cref="MusicDirector"/>); the mixer (<see cref="SfxMixer"/>) plays them all.
    /// </summary>
    public sealed class AudioModule : IGameModule
    {
        public int Order => -50;

        public void Init()
        {
            AudioJobs.Reset();
            Application.quitting -= AudioJobs.Shutdown;
            Application.quitting += AudioJobs.Shutdown;
            var synth = new SfxSynth();
            Sfx.Backend = synth;
            _ = GameSettings.Current;   // loading applies the saved master volume
#if UNITY_EDITOR || DEBUG
            foreach (FieldInfo f in typeof(SfxIds).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.GetValue(null) is string id && !synth.Has(id))
                    Debug.LogWarning("[CloneZ] No sound recipe for '" + id + "'.");
#endif
            synth.Prewarm();
            SfxMixer.Ensure();
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<ListenerRig>() == null) p.gameObject.AddComponent<ListenerRig>();
                FootstepAudio.OnPlayerBuilt(p);
                BodyAudio.OnPlayerBuilt(p);
            };
            GameEvents.ItemLooted += (stack, source) => Sfx.PlayUI(SfxIds.Pickup, 0.45f, Random.Range(0.92f, 1.08f));
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

        private void LateUpdate()
        {
            if (_ears == null) return;
            if (_head == null && _avatar != null && _avatar.Rig != null && _avatar.Rig.Bones != null)
                _head = _avatar.Rig.Bone(HumanBone.Head);

            Camera cam = Camera.main;
            if (cam != _mutedCam)
            {
                if (_mutedCam != null) SetListener(_mutedCam, true);
                _mutedCam = cam;
                if (cam != null) SetListener(cam, false);
            }
            if (!_listener.enabled) _listener.enabled = true;

            Vector3 pos = _head != null ? _head.position : transform.position + Vector3.up * 1.65f;
            Quaternion rot = transform.rotation;
            if (cam != null) rot = ViewState.FirstPersonLike ? cam.transform.rotation : Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            _ears.SetPositionAndRotation(pos, rot);
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
