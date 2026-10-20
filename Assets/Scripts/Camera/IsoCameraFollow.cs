using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The player camera. V cycles three views — Isometric → Third person → First person → Isometric —
    /// but never while a menu owns the screen:
    /// <list type="bullet">
    /// <item><description><b>Isometric</b> (photo 1): Project Zomboid's view. Yawed 45° so square rooms
    /// read as diamonds, pitched ~38° down and rendered orthographic like PZ's projection, so nothing
    /// shrinks toward the top of the screen. The mouse wheel zooms in smooth steps, the follow is damped,
    /// and walls standing between the camera and the survivor are cut away (<see cref="IsoCutaway"/>).
    /// The survivor faces the ground under the mouse cursor.</description></item>
    /// <item><description><b>Third person</b> (photo 3): over the right shoulder, with mouse-look, camera
    /// collision and a crosshair (<see cref="ViewCrosshair"/>). The survivor faces where the camera
    /// looks and strafes, PZ-style.</description></item>
    /// <item><description><b>First person</b>: mouse-look from the eyes with the body hidden.</description></item>
    /// </list>
    /// This is the only writer of <see cref="ViewState.Current"/>: <see cref="PlayerController"/> and
    /// <see cref="PlayerCombat"/> read it to aim at the cursor or through the crosshair, and other systems
    /// read it to show ceilings or hide gear in first person. The cursor is locked for mouse-look only while
    /// playing — a menu, sleep, death or the end of a raid always hands it back.
    /// </summary>
    [DefaultExecutionOrder(50)]   // after the avatar has posed the head; before the ground items read the camera
    [DisallowMultipleComponent]
    public class IsoCameraFollow : MonoBehaviour
    {
        /// <summary>The survivor the camera follows (set by the bootstrapper).</summary>
        public Transform Target;

        [Header("Isometric (orthographic)")]
        [Tooltip("Compass heading the camera looks along; 45 looks diagonally across the grid.")]
        public float IsoYaw = 45f;
        [Tooltip("Degrees the camera looks down.")]
        public float IsoPitch = 38f;
        [Tooltip("Half the view's height in metres at each mouse-wheel zoom step, closest first.")]
        public float[] IsoZoomSizes = { 5.5f, 7f, 9f, 11f, 12.5f };
        [Tooltip("The zoom step in use (an index into IsoZoomSizes).")]
        public int IsoZoomStep = 2;
        [Tooltip("How quickly the view glides to a new zoom step.")]
        public float ZoomSharpness = 8f;
        [Tooltip("How tightly the camera follows the survivor (higher is snappier).")]
        public float FollowDamping = 8f;
        [Tooltip("Height above the survivor's feet the camera centres on.")]
        public float LookHeight = 1.0f;
        [Tooltip("Depth in front of the camera at which the bottom edge of the view meets the ground. The camera " +
                 "backs off with the zoom to keep it, so anything up to ~0.6x this tall at the bottom edge stays in front of the near plane.")]
        public float IsoClearance = 9f;
        [Tooltip("Cut away walls and other tall geometry standing between the camera and the survivor.")]
        public bool WallCutaway = true;
        [Tooltip("Shift distance fog with the camera's distance so it stays anchored to the survivor at every zoom.")]
        public bool CompensateIsoFog = true;
        [Tooltip("Camera distance the weather's fog distances were tuned for (the original isometric camera).")]
        public float IsoFogBaseline = 24f;
        public float IsoNearClip = 0.3f;

        [Header("Third person")]
        public float ThirdPersonFov = 55f;
        [Tooltip("Camera offset from the upper-back pivot in view space: right, up and back (negative).")]
        public Vector3 ShoulderOffset = new Vector3(0.45f, 0.15f, -2.4f);
        [Tooltip("How far below the head bone the pivot sits: the upper back.")]
        public float PivotBelowHead = 0.14f;
        [Tooltip("Radius of the camera-collision sphere cast.")]
        public float CollisionRadius = 0.22f;
        [Tooltip("How fast the camera pulls in when something comes between it and the survivor.")]
        public float PullInSharpness = 30f;
        [Tooltip("How fast it eases back out once the way is clear.")]
        public float ReleaseSharpness = 5f;
        [Tooltip("Shoulder-view pitch limits in degrees (negative looks up).")]
        public float ThirdPersonMinPitch = -40f;
        public float ThirdPersonMaxPitch = 65f;
        [Tooltip("Pitch the shoulder view starts at when entered from the isometric view.")]
        public float ThirdPersonStartPitch = 8f;
        [Tooltip("The body hides while collision holds the camera closer than this to the pivot.")]
        public float HideBodyDistance = 0.55f;

        [Header("First person")]
        public float FirstPersonFov = 55f;
        [Tooltip("Eye height above the player root, used when the body has no head bone.")]
        public float EyeHeight = 1.6f;
        [Tooltip("The eyes sit this far above the head bone (the base of the skull).")]
        public float EyeAboveHeadBone = 0.07f;
        [Tooltip("Nudges the eye a touch forward of the head.")]
        public float EyeForward = 0.12f;

        [Header("Mouse-look (shoulder and first person)")]
        [Tooltip("Degrees turned per pixel of mouse movement.")]
        public float MouseSensitivity = 0.12f;
        [Tooltip("First-person pitch limits in degrees (negative looks up).")]
        public float MinPitch = -75f;
        public float MaxPitch = 80f;
        [Tooltip("Near clip plane while the camera is close to the survivor, so a wall a step away doesn't clip.")]
        public float MouseLookNearClip = 0.05f;

        [Header("Transitions")]
        [Tooltip("Seconds the camera glides between the shoulder and first-person views (0 cuts). Changes " +
                 "to or from the orthographic isometric view always cut.")]
        public float ViewBlendSeconds = 0.3f;
        [Tooltip("How closely the eye and shoulder heights track the head bone (sneaking lowers the view).")]
        public float HeadFollowSharpness = 10f;

        /// <summary>The live player camera, or null before one exists.</summary>
        public static IsoCameraFollow Instance { get; private set; }

        /// <summary>True while the first-person view is active. A thin wrapper over
        /// <see cref="ViewState.Current"/>, kept for older callers.</summary>
        public static bool FirstPersonActive => ViewState.Current == CameraView.FirstPerson;

        /// <summary>True while mouse-look actually has the mouse: a shoulder or first-person view with the
        /// cursor locked (no menu open, the survivor alive and awake, the raid still on). The crosshair
        /// shows exactly then.</summary>
        public static bool MouseLookEngaged { get; private set; }

        /// <summary>The point the view centres on: the survivor's chest in the isometric view, the
        /// shoulder pivot or the eyes otherwise. Effects that should surround the survivor rather than the
        /// far-off isometric camera (snow, rain, ground-item detail) can centre on this.</summary>
        public static Vector3 FocusPoint { get; private set; }

        /// <summary>True while the camera has the survivor's body hidden: in first person, or when the
        /// shoulder camera has been pushed right up against them. Gear drawn on the body can hide with it.</summary>
        public static bool SurvivorHidden { get; private set; }

        /// <summary>The view this camera is showing.</summary>
        public CameraView View { get; private set; } = CameraView.Isometric;

        /// <summary>True in the first-person view.</summary>
        public bool FirstPerson => View == CameraView.FirstPerson;

        /// <summary>The isometric view's current orthographic half-height (it eases toward the zoom step).</summary>
        public float IsoSize => _isoSize;

        /// <summary>How far the isometric camera currently sits from the survivor's chest along its view.</summary>
        public float IsoDistance => IsoCameraDistance(_isoSize);

        private Camera _cam;
        private IsoCutaway _cutaway;
        private ViewCrosshair _crosshair;

        // What we know about the target, refreshed whenever Target changes.
        private Transform _cachedTarget;
        private PlayerStats _stats;
        private PlayerSleep _sleep;
        private PlayerAvatar _avatar;
        private Transform _head;
        private float _headHeight = HumanoidProportions.HeadY;   // head bone above the feet, smoothed

        // Isometric follow.
        private Vector3 _isoFocus;
        private Vector3 _isoFocusVelocity;
        private float _isoSize;
        private float _nextZoomStep;

        // Mouse-look.
        private float _yaw;
        private float _pitch;
        private int _skipMouseFrames;
        private float _shoulderDistance = -1f;   // < 0 re-seeds from the collision distance
        private readonly RaycastHit[] _castHits = new RaycastHit[16];

        // View blends (shoulder <-> first person).
        private bool _placed;
        private bool _lensDirty;
        private float _blend = 1f;
        private Vector3 _blendPos;
        private Quaternion _blendRot = Quaternion.identity;
        private float _blendFov;

        private bool _bodyHidden;

        // Fog anchoring (see OnBeginCameraRendering).
        private bool _fogShifted;
        private float _fogStart;
        private float _fogEnd;

        private float EyeHeightNow => _head != null ? _headHeight + EyeAboveHeadBone : EyeHeight;
        private float PivotHeight => (_head != null ? _headHeight : EyeHeight - EyeAboveHeadBone) - PivotBelowHead;
        private float ChestHeight => Mathf.Max(0.5f, (_head != null ? _headHeight : EyeHeight) * 0.75f);

        #region Lifecycle
        // Statics outlive a play session when domain reload is off; every session starts clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            MouseLookEngaged = false;
            SurvivorHidden = false;
            FocusPoint = Vector3.zero;
            ViewState.Current = CameraView.Isometric;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cutaway = new IsoCutaway();
            _isoSize = ZoomSize();
            ViewState.Current = View;   // every run opens in the isometric view
            // Cinemachine owns the real camera: each view drives its own virtual camera, the brain blends them.
            _views = CinemachineViews.Attach(_cam);
            ViewBlendSeconds = 0f;
        }

        private CinemachineViews _views;
        private PlayerCombat _combat;

        /// <summary>True when the server allows only first-person views in this raid (tension 10: no shoulder-peeking in PvP).</summary>
        public static bool FirstPersonOnly => ServerRules.FirstPersonOnly && ServerRules.PvpEnabled && GameSession.Networked && GameSession.IsExtraction;

        private void OnEnable()
        {
            Instance = this;
            ViewState.Current = View;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void Start()
        {
            if (_crosshair == null) _crosshair = ViewCrosshair.Create(transform);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreFog();
            if (_cutaway != null) _cutaway.RestoreAll();
            SetBodyHidden(false);

            // Torn down mid mouse-look (a scene reload, quitting to the menu): hand the cursor back, and
            // stop anything aiming as if the mouse still steered a view.
            if (MouseLookEngaged || Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            MouseLookEngaged = false;
            ViewState.Current = CameraView.Isometric;
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public API
        /// <summary>Places the camera on its target at once: no follow lag, zoom glide or view blend.</summary>
        public void Snap()
        {
            if (Target == null) return;
            if (_cam == null) _cam = GetComponent<Camera>();
            CacheTarget();
            ResolveHead(true);
            _blend = 1f;
            _isoFocus = Target.position + Vector3.up * LookHeight;
            _isoFocusVelocity = Vector3.zero;
            _isoSize = ZoomSize();
            _shoulderDistance = -1f;

            ComputePose(0f, out Vector3 pos, out Quaternion rot, out float fov);
            transform.SetPositionAndRotation(pos, rot);
            _lensDirty = true;
            ApplyLens(fov, false);
            _placed = true;
            ViewState.Current = View;
        }

        /// <summary>Switches to <paramref name="view"/> and publishes it in <see cref="ViewState.Current"/>.
        /// The shoulder and first-person views glide into each other over <see cref="ViewBlendSeconds"/>;
        /// changes to or from the orthographic isometric view cut.</summary>
        public void SetView(CameraView view)
        {
            ViewState.Current = view;
            if (view == View) return;

            CameraView from = View;
            if (from == CameraView.Isometric || view == CameraView.Isometric) _blend = 1f;
            else BeginBlend();
            View = view;
            _lensDirty = true;

            if (view == CameraView.Isometric)
            {
                if (Target != null) _isoFocus = Target.position + Vector3.up * LookHeight;
                _isoFocusVelocity = Vector3.zero;
            }
            else
            {
                if (from == CameraView.Isometric)
                {
                    // Fresh mouse-look starts behind the survivor, looking the way they face.
                    _yaw = FacingYaw();
                    _pitch = view == CameraView.ThirdPerson ? ThirdPersonStartPitch : 0f;
                }
                ClampPitch();
                _skipMouseFrames = 2;
            }

            if (view == CameraView.ThirdPerson) _shoulderDistance = -1f;
            if (view != CameraView.FirstPerson) SetBodyHidden(false);   // leaving first person shows the body at once
        }

        /// <summary>Steps to the next view: Isometric → Third person → First person → Bodycam → Isometric (first person
        /// and bodycam only, when the server forbids the others in PvP).</summary>
        public void CycleView()
        {
            if (FirstPersonOnly)
            {
                SetView(View == CameraView.FirstPerson ? CameraView.Bodycam : CameraView.FirstPerson);
                return;
            }
            switch (View)
            {
                case CameraView.Isometric: SetView(CameraView.ThirdPerson); break;
                case CameraView.ThirdPerson: SetView(CameraView.FirstPerson); break;
                case CameraView.FirstPerson: SetView(CameraView.Bodycam); break;
                default: SetView(CameraView.Isometric); break;
            }
        }

        /// <summary>Switches straight to the first-person view, or back to the isometric one (the original
        /// two-view toggle, kept for older callers).</summary>
        public void SetFirstPerson(bool on) => SetView(on ? CameraView.FirstPerson : CameraView.Isometric);
        #endregion

        #region Input
        private void Update()
        {
            if (Target == null || UIState.GameplayBlocked) return;
            CacheTarget();
            if (_stats != null && _stats.IsDead) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.vKey.wasPressedThisFrame) CycleView();
            if (FirstPersonOnly && View != CameraView.FirstPerson && View != CameraView.Bodycam && !(_stats != null && _stats.IsDead)) SetView(CameraView.FirstPerson);

            // Each view's own controls (its action map): rotate the isometric view, swap the shoulder.
            GameInput.ApplyView(View);
            if (View == CameraView.Isometric && GameInput.Pressed(GameAction.RotateView)) IsoYaw = Mathf.Repeat(IsoYaw + 90f, 360f);
            if (View == CameraView.ThirdPerson && GameInput.Pressed(GameAction.SwapShoulder)) ShoulderOffset.x = -ShoulderOffset.x;

            // The wheel zooms the isometric view, unless build mode has it cycling the palette.
            if (View == CameraView.Isometric && !UIState.BuildMode) ReadZoom();
        }

        private void ReadZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || IsoZoomSizes == null || IsoZoomSizes.Length == 0) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f || Time.unscaledTime < _nextZoomStep) return;
            _nextZoomStep = Time.unscaledTime + 0.06f;   // one step per notch, even from a fast spin
            IsoZoomStep = Mathf.Clamp(IsoZoomStep + (scroll > 0f ? -1 : 1), 0, IsoZoomSizes.Length - 1);
        }

        private float ZoomSize()
        {
            if (IsoZoomSizes == null || IsoZoomSizes.Length == 0) return 9f;
            return Mathf.Max(1f, IsoZoomSizes[Mathf.Clamp(IsoZoomStep, 0, IsoZoomSizes.Length - 1)]);
        }

        // The orthographic camera backs off with the zoom so the bottom edge of the view always meets the
        // ground IsoClearance in front of it: tall things there stay clear of the near plane, while the
        // top edge stays well inside the shadow distance.
        private float IsoCameraDistance(float size)
            => size / Mathf.Tan(Mathf.Clamp(IsoPitch, 15f, 89f) * Mathf.Deg2Rad) + IsoClearance;

        private void ApplyCursor(bool engage)
        {
            if (engage)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    _skipMouseFrames = 2;   // the warp to the centre arrives as one big delta: ignore it
                }
                if (Cursor.visible) Cursor.visible = false;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible) Cursor.visible = true;
            }
            MouseLookEngaged = engage;
        }

        private void ReadMouseLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 d = mouse.delta.ReadValue();
            if (_skipMouseFrames > 0) { _skipMouseFrames--; return; }
            float sens = MouseSensitivity * Mathf.Max(0.05f, GameSettings.Current.MouseSensitivity);
            _yaw = Mathf.Repeat(_yaw + d.x * sens, 360f);
            _pitch -= d.y * sens;
            ClampPitch();
        }

        private void ClampPitch()
        {
            if (View == CameraView.ThirdPerson) _pitch = Mathf.Clamp(_pitch, ThirdPersonMinPitch, ThirdPersonMaxPitch);
            else _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
        }

        // True once the raid is over (extracted, killed or caught by dawn) and its summary owns the screen.
        private static bool RaidOver()
        {
            ExtractionManager m = ExtractionManager.Instance;
            if (m == null) return false;
            ExtractionManager.RaidState s = m.State;
            return s == ExtractionManager.RaidState.Extracted || s == ExtractionManager.RaidState.Dead
                || s == ExtractionManager.RaidState.MissingInAction;
        }
        #endregion

        #region Placement
        private void LateUpdate()
        {
            if (Target == null) { MouseLookEngaged = false; return; }   // no survivor yet (title screen): leave the cursor be
            if (_cam == null) _cam = GetComponent<Camera>();
            CacheTarget();
            if (!_placed) Snap();

            bool dead = _stats != null && _stats.IsDead;
            if (dead && View != CameraView.Isometric) SetView(CameraView.Isometric);   // watch the fall from above

            bool engage = View != CameraView.Isometric && !UIState.GameplayBlocked && !dead && !RaidOver();
            ApplyCursor(engage);
            if (engage) ReadMouseLook();

            float dt = Time.deltaTime;
            UpdateHeadHeight(dt);
            ComputePose(dt, out Vector3 pos, out Quaternion rot, out float fov);

            bool blending = _blend < 1f;
            if (blending)
            {
                _blend = Mathf.Min(1f, _blend + Time.unscaledDeltaTime / Mathf.Max(0.01f, ViewBlendSeconds));
                float s = _blend * _blend * (3f - 2f * _blend);
                pos = Vector3.Lerp(_blendPos, pos, s);
                rot = Quaternion.Slerp(_blendRot, rot, s);
                fov = Mathf.Lerp(_blendFov, fov, s);
            }
            transform.SetPositionAndRotation(pos, rot);
            if (_views != null)
            {
                if (View != CameraView.Isometric)
                {
        
                    if (View == CameraView.FirstPerson || View == CameraView.Bodycam)
                    {
                        if (View == CameraView.FirstPerson) fov = GameSettings.Current.FieldOfView;
                        if (_combat != null && _combat.AimingDownSights)
                        {
                            GunStats gs = Ballistics.Gun(_combat.EquippedFirearm != null ? _combat.EquippedFirearm.Id : null);
                            float zoom = _combat.Build.Zoom > 1f ? _combat.Build.Zoom : gs != null ? gs.AdsZoom : 1.15f;
                            fov /= Mathf.Max(1f, zoom);
                        }
                    }
                }
                float near = View == CameraView.Isometric ? IsoNearClip : MouseLookNearClip;
                _views.Drive(View, pos, rot, fov, _isoSize, near, _cam != null ? _cam.farClipPlane : 400f);
            }
            else ApplyLens(fov, blending);

            UpdateBodyVisibility();
            if (_cutaway != null)
                _cutaway.Tick(pos, rot * Vector3.forward, View == CameraView.Isometric, Target, ChestHeight,
                    WallCutaway && View == CameraView.Isometric);
        }

        // Orthographic for the isometric view, perspective for the others. The field of view is only
        // touched while a view change settles, so others may adjust it in between.
        private void ApplyLens(float fov, bool blending)
        {
            if (_cam == null) return;
            bool ortho = View == CameraView.Isometric;
            if (_cam.orthographic != ortho) _cam.orthographic = ortho;
            if (ortho)
            {
                if (_cam.orthographicSize != _isoSize) _cam.orthographicSize = _isoSize;
            }
            else if (blending || _lensDirty)
            {
                _cam.fieldOfView = fov;
                _lensDirty = blending;
            }
            float near = ortho ? IsoNearClip : MouseLookNearClip;
            if (_cam.nearClipPlane != near) _cam.nearClipPlane = near;
        }

        private void ComputePose(float dt, out Vector3 pos, out Quaternion rot, out float fov)
        {
            switch (View)
            {
                case CameraView.ThirdPerson:
                    PoseThirdPerson(dt, out pos, out rot);
                    fov = ThirdPersonFov;
                    break;
                case CameraView.FirstPerson:
                    PoseFirstPerson(out pos, out rot);
                    fov = FirstPersonFov;
                    break;
                case CameraView.Bodycam:
                    PoseBodycam(out pos, out rot);
                    fov = BodycamFov;
                    break;
                default:
                    PoseIsometric(dt, out pos, out rot);
                    fov = _cam != null ? _cam.fieldOfView : ThirdPersonFov;   // unused: the view is orthographic
                    break;
            }
        }

        // PZ's camera: a fixed diagonal heading and pitch, centred on the survivor's chest with a damped
        // follow; the wheel eases the orthographic size, and the camera backs off to suit it.
        private void PoseIsometric(float dt, out Vector3 pos, out Quaternion rot)
        {
            Vector3 want = Target.position + Vector3.up * LookHeight;
            if (dt > 0f)
            {
                _isoFocus = Vector3.SmoothDamp(_isoFocus, want, ref _isoFocusVelocity,
                    1f / Mathf.Max(0.01f, FollowDamping), Mathf.Infinity, dt);
                _isoSize = Mathf.Lerp(_isoSize, ZoomSize(), 1f - Mathf.Exp(-ZoomSharpness * dt));
            }
            // A teleport (a respawn, a loaded save) cuts rather than sweeping across the map.
            if ((want - _isoFocus).sqrMagnitude > 900f) { _isoFocus = want; _isoFocusVelocity = Vector3.zero; }

            rot = Quaternion.Euler(IsoPitch, IsoYaw, 0f);
            pos = _isoFocus - rot * Vector3.forward * IsoCameraDistance(_isoSize);
            FocusPoint = _isoFocus;
        }

        // Over the right shoulder: orbit the upper-back pivot by the mouse-look angles, then pull in along
        // the pivot-to-camera line whenever something solid is in the way.
        private void PoseThirdPerson(float dt, out Vector3 pos, out Quaternion rot)
        {
            rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = Target.position + Vector3.up * PivotHeight;
            Vector3 offset = rot * ShoulderOffset;
            float full = offset.magnitude;
            Vector3 dir = full > 1e-4f ? offset / full : rot * Vector3.back;
            float allowed = ClearDistance(pivot, dir, full);

            if (_shoulderDistance < 0f) _shoulderDistance = allowed;
            else if (allowed < _shoulderDistance)
            {
                // Pull in fast, and never sit more than a hair past the obstacle: the sphere's radius
                // keeps the lens out of the wall either way.
                float eased = Mathf.Lerp(_shoulderDistance, allowed, 1f - Mathf.Exp(-PullInSharpness * dt));
                _shoulderDistance = Mathf.Min(eased, allowed + CollisionRadius * 0.5f);
            }
            else _shoulderDistance = Mathf.Lerp(_shoulderDistance, allowed, 1f - Mathf.Exp(-ReleaseSharpness * dt));

            _shoulderDistance = Mathf.Clamp(_shoulderDistance, 0f, full);
            pos = pivot + dir * _shoulderDistance;
            FocusPoint = pivot;
        }

        // How far the camera can back away from the pivot along dir before its sphere meets something
        // solid. The survivor (the cast starts inside their capsule), zombies (Ignore Raycast) and
        // triggers never push it in.
        private float ClearDistance(Vector3 pivot, Vector3 dir, float full)
        {
            int n = Physics.SphereCastNonAlloc(pivot, CollisionRadius, dir, _castHits, full,
                ~(1 << CloneZConfig.IgnoreRaycastLayer), QueryTriggerInteraction.Ignore);
            float best = full;
            for (int i = 0; i < n; i++)
            {
                Collider c = _castHits[i].collider;
                if (c == null || c.transform.IsChildOf(Target)) continue;
                if (_castHits[i].distance < best) best = _castHits[i].distance;
            }
            return best;
        }

        private void PoseFirstPerson(out Vector3 pos, out Quaternion rot)
        {
            rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 eye = Target.position + Vector3.up * EyeHeightNow;
            pos = eye + rot * Vector3.forward * EyeForward;
            FocusPoint = eye;
        }

        [Header("Bodycam")]
        [Tooltip("The bodycam's wide lens (the URP lens distortion bends it further, per the accessibility setting).")]
        public float BodycamFov = 100f;
        [Tooltip("How much of the chest's own sway (breathing, the walk, recoil) the strapped-on camera takes.")]
        public float BodycamSway = 0.65f;

        // Bodycam: strapped to the chest, not the eyes. It turns with the body, takes only part of the look's pitch,
        // and carries every lurch of the torso — the heavy camera motion that sells the view.
        private void PoseBodycam(out Vector3 pos, out Quaternion rot)
        {
            Quaternion look = Quaternion.Euler(_pitch * 0.8f, _yaw, 0f);
            Transform chest = _avatar != null && _avatar.Rig != null && _avatar.Rig.Bones != null ? _avatar.Rig.Bone(HumanBone.Chest) : null;
            if (chest != null)
            {
                Quaternion bodyLocal = Quaternion.Inverse(Target.rotation) * chest.rotation;
                rot = look * Quaternion.Slerp(Quaternion.identity, bodyLocal, BodycamSway);
                pos = chest.position + look * new Vector3(0.04f, 0.05f, 0.2f);
            }
            else
            {
                rot = look;
                pos = Target.position + Vector3.up * (EyeHeightNow - 0.35f) + look * Vector3.forward * 0.2f;
            }
            FocusPoint = pos;
        }

        // Remembers where the camera is so the next mouse-look view can glide in from there.
        private void BeginBlend()
        {
            if (!_placed || ViewBlendSeconds <= 0f) { _blend = 1f; return; }
            _blendPos = transform.position;
            _blendRot = transform.rotation;
            _blendFov = _cam != null ? _cam.fieldOfView : ThirdPersonFov;
            _blend = 0f;
        }
        #endregion

        #region Survivor
        private void CacheTarget()
        {
            if (ReferenceEquals(Target, _cachedTarget)) return;
            if (_bodyHidden) SetBodyHidden(false);   // hand the previous body its looks back
            _cachedTarget = Target;
            _stats = Target != null ? Target.GetComponent<PlayerStats>() : null;
            _sleep = Target != null ? Target.GetComponent<PlayerSleep>() : null;
            _avatar = Target != null ? Target.GetComponent<PlayerAvatar>() : null;
            _combat = Target != null ? Target.GetComponent<PlayerCombat>() : null;
            _head = null;
            ResolveHead(true);
        }

        // Finds the rig's head bone once the avatar has built it; snap takes its height at once.
        private void ResolveHead(bool snap)
        {
            if (_head == null && _avatar != null)
            {
                HumanoidRig rig = _avatar.Rig;
                if (rig != null && rig.Bones != null)
                {
                    _head = rig.Bone(HumanBone.Head);
                    snap = true;
                }
            }
            if (snap && _head != null && Target != null)
                _headHeight = Mathf.Clamp(_head.position.y - Target.position.y, 0.5f, 2.2f);
        }

        // Tracks the head bone's height above the feet, smoothed, so sneaking lowers the eye and the
        // shoulder pivot without the walk's bob shaking the view. Held while asleep or dead, when the
        // body lies down.
        private void UpdateHeadHeight(float dt)
        {
            ResolveHead(false);
            if (_head == null || dt <= 0f) return;
            if (_stats != null && _stats.IsDead) return;
            if (_sleep != null && _sleep.IsSleeping) return;
            float raw = Mathf.Clamp(_head.position.y - Target.position.y, 0.5f, 2.2f);
            _headHeight = Mathf.Lerp(_headHeight, raw, 1f - Mathf.Exp(-HeadFollowSharpness * dt));
        }

        private float FacingYaw()
        {
            if (Target == null) return transform.eulerAngles.y;
            Vector3 f = CloneZUtil.Flat(Target.forward);
            return f == Vector3.zero ? transform.eulerAngles.y : Quaternion.LookRotation(f, Vector3.up).eulerAngles.y;
        }

        // The body hides in first person (once the glide is half way in) and when collision has pulled the
        // shoulder camera right up against it; it shows everywhere else. Gear other systems hang on the
        // survivor hides itself by reading ViewState.
        private void UpdateBodyVisibility()
        {
            bool hide = false;
            if (View == CameraView.FirstPerson) hide = _blend >= 0.5f;
            else if (View == CameraView.ThirdPerson && _blend >= 1f)
                hide = _shoulderDistance < (_bodyHidden ? HideBodyDistance + 0.15f : HideBodyDistance);
            SetBodyHidden(hide);
        }

        private void SetBodyHidden(bool hide)
        {
            if (hide == _bodyHidden) return;
            _bodyHidden = hide;
            SurvivorHidden = hide;
            HumanoidRig rig = _avatar != null ? _avatar.Rig : null;
            if (rig != null) rig.SetVisible(!hide);   // the body together with whatever is in its hands
        }
        #endregion

        #region Fog
        // The weather's linear fog is measured by depth from the camera and was tuned for the original
        // isometric camera, about IsoFogBaseline metres from the survivor. While this camera renders an
        // isometric frame the fog is moved by however far the zoom has moved the camera from that, so the
        // haze sits in the same place around the survivor at every zoom step. The weather's own values are
        // restored the moment the frame is done, so nothing else ever sees the shift.
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!ReferenceEquals(cam, _cam) || _fogShifted || !CompensateIsoFog || View != CameraView.Isometric) return;
            if (!RenderSettings.fog || RenderSettings.fogMode != FogMode.Linear) return;
            float shift = Vector3.Distance(transform.position, FocusPoint) - IsoFogBaseline;
            if (Mathf.Abs(shift) < 0.05f) return;

            _fogStart = RenderSettings.fogStartDistance;
            _fogEnd = RenderSettings.fogEndDistance;
            float start = Mathf.Max(0f, _fogStart + shift);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = Mathf.Max(start + 1f, _fogEnd + shift);
            _fogShifted = true;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (ReferenceEquals(cam, _cam)) RestoreFog();
        }

        private void RestoreFog()
        {
            if (!_fogShifted) return;
            _fogShifted = false;
            RenderSettings.fogStartDistance = _fogStart;
            RenderSettings.fogEndDistance = _fogEnd;
        }
        #endregion
    }
}
