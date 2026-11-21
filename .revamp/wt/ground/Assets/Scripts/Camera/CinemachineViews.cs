using Unity.Cinemachine;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The camera rig as Cinemachine sees it (the doc's Unity setup: one virtual camera per mode, blended on switch).
    /// <see cref="IsoCameraFollow"/> stays the brain of each view — the isometric cutaways, the shoulder collision,
    /// the head-bone eye — and writes the pose it wants into that view's <see cref="CinemachineCamera"/>; the
    /// <see cref="CinemachineBrain"/> on the real camera picks the live one by priority and blends between them
    /// (shoulder ↔ first person ↔ bodycam glide; anything to or from the orthographic isometric view cuts). Recoil
    /// and blasts shake the perspective views through Cinemachine impulses, scaled by the camera-shake setting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CinemachineViews : MonoBehaviour
    {
        public static CinemachineViews Instance { get; private set; }

        private CinemachineBrain _brain;
        private readonly CinemachineCamera[] _vcams = new CinemachineCamera[4];
        private CameraView _live = (CameraView)(-1);
        private static CinemachineImpulseSource s_Impulse;

        public static CinemachineViews Attach(Camera cam)
        {
            var v = cam.GetComponent<CinemachineViews>();
            if (v == null) v = cam.gameObject.AddComponent<CinemachineViews>();
            return v;
        }

        private void Awake()
        {
            Instance = this;
            _brain = GetComponent<CinemachineBrain>();
            if (_brain == null) _brain = gameObject.AddComponent<CinemachineBrain>();
            _brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            _brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            _brain.LensModeOverride = new CinemachineBrain.LensModeOverrideSettings { Enabled = true, DefaultMode = LensSettings.OverrideModes.Perspective };
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.3f);
            _brain.ShowDebugText = false;

            var blends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
            blends.CustomBlends = new[]
            {
                Cut("CM Isometric", "**ANY CAMERA**"), Cut("**ANY CAMERA**", "CM Isometric"),
                Ease("CM Shoulder", "CM FirstPerson", 0.3f), Ease("CM FirstPerson", "CM Shoulder", 0.3f),
                Ease("CM FirstPerson", "CM Bodycam", 0.25f), Ease("CM Bodycam", "CM FirstPerson", 0.25f),
                Ease("CM Shoulder", "CM Bodycam", 0.3f), Ease("CM Bodycam", "CM Shoulder", 0.3f),
            };
            _brain.CustomBlends = blends;

            string[] names = { "CM Isometric", "CM Shoulder", "CM FirstPerson", "CM Bodycam" };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(names[i]);
                var vc = go.AddComponent<CinemachineCamera>();
                vc.Priority = 0;
                vc.Lens = LensSettings.Default;
                vc.Lens.ModeOverride = i == 0 ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.Perspective;
                if (i > 0)
                {
                    var listener = go.AddComponent<CinemachineImpulseListener>();
                    listener.Gain = 1f;
                    listener.ChannelMask = -1;
                    listener.Use2DDistance = false;
                }
                _vcams[i] = vc;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            for (int i = 0; i < _vcams.Length; i++) if (_vcams[i] != null) Destroy(_vcams[i].gameObject);
        }

        private static CinemachineBlenderSettings.CustomBlend Cut(string from, string to)
            => new CinemachineBlenderSettings.CustomBlend { From = from, To = to, Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) };

        private static CinemachineBlenderSettings.CustomBlend Ease(string from, string to, float s)
            => new CinemachineBlenderSettings.CustomBlend { From = from, To = to, Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, s) };

        /// <summary>Writes the pose and lens of <paramref name="view"/> into its virtual camera, makes it live and
        /// updates the brain (which moves the real camera, blending if the view just changed).</summary>
        public void Drive(CameraView view, Vector3 pos, Quaternion rot, float fov, float orthoSize, float near, float far)
        {
            int i = Mathf.Clamp((int)view, 0, 3);
            CinemachineCamera vc = _vcams[i];
            vc.transform.SetPositionAndRotation(pos, rot);
            LensSettings lens = vc.Lens;
            lens.FieldOfView = Mathf.Clamp(fov, 10f, 140f);
            lens.OrthographicSize = Mathf.Max(0.5f, orthoSize);
            lens.NearClipPlane = near;
            lens.FarClipPlane = far;
            lens.ModeOverride = i == 0 ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.Perspective;
            vc.Lens = lens;
            if (view != _live)
            {
                for (int k = 0; k < 4; k++) _vcams[k].Priority = k == i ? 20 : 0;
                _live = view;
            }
            // With the world frozen (the pause menu switching views), blends still play out in real time.
            bool paused = Time.timeScale <= 0f;
            if (_brain.IgnoreTimeScale != paused) _brain.IgnoreTimeScale = paused;
            _brain.ManualUpdate();
        }

        /// <summary>A kick of the camera (recoil, a blast nearby), scaled by the camera-shake setting.</summary>
        public static void Shake(Vector3 at, float strength)
        {
            float k = GameSettings.Current.CameraShake;
            if (k <= 0.01f || strength <= 0f || ViewState.Current == CameraView.Isometric) return;
            if (s_Impulse == null)
            {
                var go = new GameObject("[CameraImpulse]");
                DontDestroyOnLoad(go);
                s_Impulse = go.AddComponent<CinemachineImpulseSource>();
                s_Impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
                s_Impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
                s_Impulse.ImpulseDefinition.ImpulseDuration = 0.18f;
            }
            s_Impulse.transform.position = at;
            s_Impulse.GenerateImpulseAtPositionWithVelocity(at, Random.insideUnitSphere.normalized * 0.05f + Vector3.down * strength * 0.12f * k);
        }
    }
}
