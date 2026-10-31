using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Survival movement: move relative to the camera, aim Project Zomboid style, sprint/sneak with
    /// stamina, and radiate footstep noise that draws zombies. Aiming follows the camera view
    /// (<see cref="ViewState"/>): in the isometric view the survivor faces the ground point under the
    /// mouse cursor; in the shoulder and first-person views the aim runs through the crosshair at the
    /// screen centre and the survivor faces where the camera looks, strafing as they move.
    /// Sitting down (or lying on the bed to rest, see <see cref="PlayerSeat"/>) holds the body still —
    /// no walking, no turning to the aim — until a movement key or Space gets the survivor up again.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        private const float CrosshairRange = 150f;      // how far the crosshair looks for a surface
        private const float CrosshairSkyDistance = 60f; // the aim point when the crosshair is on open sky
        private const float MinAimFlatDistance = 0.75f; // aim points nearer than this use the view's heading

        private CharacterController _controller;
        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerCombat _combat;
        private PlayerSeat _seat;
        private Camera _cam;

        private float _verticalVelocity;
        private float _groundY;
        private float _footstepTimer;
        private float _fallSpeed, _airborneAt;   // for the landing sound
        private Plane _groundPlane; // cached plane at the player’s current Y position

        /// <summary>Where the survivor aims: the ground under the cursor (isometric), or the first solid
        /// surface under the crosshair (shoulder / first person).</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>Flat (XZ) unit direction from the survivor toward <see cref="AimPoint"/>; melee arcs,
        /// placement and the isometric gun use it.</summary>
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        /// <summary>The living zombie under the crosshair in the shoulder / first-person views — the first
        /// thing the crosshair ray meets, with nothing solid in front of it — else null (always null in the
        /// isometric view). <see cref="PlayerCombat"/> shoots at it and the crosshair turns red.</summary>
        public ZombieHealth CrosshairZombie { get; private set; }

        /// <summary>Where the crosshair ray meets <see cref="CrosshairZombie"/>.</summary>
        public Vector3 CrosshairZombiePoint { get; private set; }

        public bool IsSneaking { get; private set; }
        public float CurrentSpeed { get; private set; }

        /// <summary>Forgets a fall in progress, for a survivor just set down somewhere else (see <see cref="FallWatch"/>).</summary>
        public void StopFalling()
        {
            _verticalVelocity = 0f;
            _fallSpeed = 0f;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            AimDirection = transform.forward;
        }

        // Optional siblings are fetched in Start, not Awake: the bootstrapper adds PlayerCombat, PlayerInventory and
        // PlayerSeat after this controller, and Awake runs inside AddComponent — before they exist.
        private void Start()
        {
            _cam = Camera.main;
            _seat = GetComponent<PlayerSeat>();
            _skills = GetComponent<PlayerSkills>();
            _combat = GetComponent<PlayerCombat>();
            _inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            // Cached camera reference initialized in Start() – no per‑frame lookup needed
            UpdateAim();

            if (_stats.IsDead || _stats.Incapacitated)
            {
                _stats.Exertion = Exertion.Idle;
                CurrentSpeed = 0f;
                ApplyGravityOnly();
                return;
            }

            if (UIState.GameplayBlocked)
            {
                _stats.Exertion = Exertion.Idle;
                CurrentSpeed = 0f;   // standing still, so a search begun from the inventory doesn't read a stale stride
                ApplyGravityOnly();
                return;
            }

            if (HoldWhileSeated()) return;

            Move();
            FaceAim();
        }

        // Seated, or lying on the bed resting: the body stays where it was set down (a seat's own collider would
        // only shove a walking body off it) and doesn't turn to the aim. A movement key or Space gets the survivor
        // up; the next frame they walk as normal.
        private bool HoldWhileSeated()
        {
            if (_seat == null || (!_seat.IsSeated && !_seat.IsLyingDown)) return false;
            _stats.Exertion = Exertion.Idle;
            CurrentSpeed = 0f;
            IsSneaking = false;
            _footstepTimer = 0f;
            ApplyGravityOnly();
            if (GetUpPressed()) _seat.GetUpFromInput();
            return true;
        }

        private static bool GetUpPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            return kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                || kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame
                || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame;
        }

        private void UpdateAim()
        {
            if (_cam == null) return;
            if (ViewState.MouseLook)
            {
                UpdateCrosshairAim();
                return;
            }
            CrosshairZombie = null;

            // Isometric: aim at the ground under the mouse cursor.
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Ray ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                var ground = _groundPlane;
            if (ground.Raycast(ray, out float enter))
            {
                AimPoint = ray.GetPoint(enter);
                Vector3 dir = CloneZUtil.Flat(AimPoint - transform.position);
                if (dir != Vector3.zero) AimDirection = dir;
            }
            else
            {
                // Looking at or above the horizon: aim flat along the view.
                Vector3 dir = CloneZUtil.Flat(_cam.transform.forward);
                if (dir != Vector3.zero)
                {
                    AimDirection = dir;
                    AimPoint = transform.position + dir * 6f;
                }
            }
        }

        // Shoulder / first-person views: the aim runs through the crosshair at the screen centre. The aim
        // point is the first solid surface under it (never the survivor; zombies live on Ignore Raycast and
        // are looked for separately), and a living zombie in front of that surface is noted for the gun.
        private void UpdateCrosshairAim()
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 dir = ray.direction;

            // The shoulder camera sits behind the survivor: whatever lies between it and them is no target.
            Vector3 chest = transform.position + Vector3.up * 1.2f;
            float skip = Mathf.Max(0f, Vector3.Dot(chest - ray.origin, dir) - 0.3f);
            Vector3 origin = ray.origin + dir * skip;

            float solid = CrosshairRange;
            if (CastPastSelf(origin, dir, CrosshairRange, ~(1 << CloneZConfig.IgnoreRaycastLayer), out RaycastHit hit, out float hitDistance))
            {
                AimPoint = hit.point;
                solid = hitDistance;
            }
            else
            {
                // Nothing solid under the crosshair: test the ground from the adjusted origin.
                // Use the cached ground plane to avoid allocations.
                Ray offsetRay = new Ray(origin, dir);
                if (_groundPlane.Raycast(offsetRay, out float enter) && enter > 0f && enter < CrosshairRange)
                {
                    AimPoint = offsetRay.GetPoint(enter);
                }
                else
                {
                    // Open sky – aim a fixed distance ahead from the adjusted origin.
                    AimPoint = offsetRay.GetPoint(CrosshairSkyDistance);
                }
            }

                CrosshairZombie = null;
                if (CastPastSelf(origin, dir, solid, ~ (1 << CloneZConfig.IgnoreRaycastLayer),
                    out RaycastHit zombieHit, out _))
                {
                    var zombie = zombieHit.collider.GetComponentInParent<ZombieHealth>();
                    if (zombie != null && !zombie.IsDead)
                    {
                        CrosshairZombie = zombie;
                        CrosshairZombiePoint = zombieHit.point;
                    }
                }

            // Melee arcs and placement aim along the ground toward the aim point; when that's at the
            // survivor's own feet (looking straight down) the view's heading stands in.
            Vector3 flat = AimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > MinAimFlatDistance * MinAimFlatDistance) AimDirection = flat.normalized;
            else
            {
                Vector3 view = ViewForward();
                if (view != Vector3.zero) AimDirection = view;
            }
        }

        // The nearest hit along a ray that isn't the survivor's own collider. Allocation-free: when the ray
        // meets the survivor it simply casts again from just beyond them. distance is from origin.
        private bool CastPastSelf(Vector3 origin, Vector3 dir, float range, int mask, out RaycastHit hit, out float distance)
        {
            distance = 0f;
            for (int i = 0; i < 4 && distance < range; i++)
            {
                if (!Physics.Raycast(origin, dir, out hit, range - distance, mask, QueryTriggerInteraction.Ignore)) break;
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    distance += hit.distance;
                    return true;
                }
                float step = hit.distance + 0.05f;
                origin += dir * step;
                distance += step;
            }
            hit = default;
            return false;
        }

        // The camera's heading on the ground plane, from its yaw rather than its flattened forward so it
        // holds steady even when the view pitches steeply up or down.
        private Vector3 ViewForward()
        {
            if (_cam == null) return Vector3.zero;
            return Quaternion.Euler(0f, _cam.transform.eulerAngles.y, 0f) * Vector3.forward;
        }

        private void Move()
        {
            Keyboard kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            bool moving = input.sqrMagnitude > 0.01f;

            // Movement basis relative to the camera's heading: W is up-screen in the isometric view and
            // straight ahead in the mouse-look views.
            Vector3 fwd = ViewForward();
            if (fwd == Vector3.zero) fwd = Vector3.forward;
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            Vector3 worldMove = (fwd * input.y + right * input.x);

            IsSneaking = kb != null && kb.leftCtrlKey.isPressed;
            bool wantSprint = kb != null && kb.leftShiftKey.isPressed && moving && !IsSneaking && _stats.CanSprint;

            float speed;
            if (!moving) { speed = 0f; _stats.Exertion = Exertion.Idle; }
            else if (IsSneaking) { speed = CloneZConfig.PlayerSneakSpeed; _stats.Exertion = Exertion.Walking; }
            else if (wantSprint) { speed = CloneZConfig.PlayerSprintSpeed; _stats.Exertion = Exertion.Sprinting; }
            else { speed = CloneZConfig.PlayerWalkSpeed; _stats.Exertion = Exertion.Walking; }

            // Skills: Sprinting quickens the sprint, Nimble speeds movement with a gun
            // out, and the matching perks train while you do it.
            // if (_skills == null) _skills = GetComponent<PlayerSkills>();
            // if (_combat == null) _combat = GetComponent<PlayerCombat>();
            if (_skills != null && moving)
            {
                float sdt = Time.deltaTime;
                if (wantSprint)
                {
                    speed *= _skills.SprintSpeedMult;
                    _skills.AddXp(PlayerSkills.Skill.Sprinting, CloneZConfig.XpSprintPerSec * sdt);
                    _skills.AddXp(PlayerSkills.Skill.Fitness, CloneZConfig.XpFitnessPerSec * sdt);
                }
                if (IsSneaking)
                {
                    _skills.AddXp(PlayerSkills.Skill.Sneak, CloneZConfig.XpSneakPerSec * sdt);
                    _skills.AddXp(PlayerSkills.Skill.Lightfooted, CloneZConfig.XpLightfootPerSec * sdt);
                }
                if (_combat != null && _combat.EquippedFirearm != null)
                {
                    speed *= _skills.NimbleMoveMult;
                    _skills.AddXp(PlayerSkills.Skill.Nimble, CloneZConfig.XpNimblePerSec * sdt);
                }
            }

            // Carrying a heavy load slows the player (encumbrance).
            if (_inventory != null) speed *= _inventory.MoveSpeedMultiplier;
            // An exhausted or badly-hurt survivor moves slower still.
            speed *= _stats.FatigueMoveMultiplier;
            speed *= _stats.PainMoveMultiplier;
            speed *= _stats.ColdMoveMultiplier;   // cold-stiff muscles and heavy wet clothes drag
            speed *= _stats.SicknessMoveMultiplier; // food poisoning slows a queasy survivor to a trudge
            // Broken legs, blood loss, overload, artifacts, traits and Zone marks.
            StatMods mods = PlayerModifiers.Current;
            speed *= Mathf.Max(0.02f, mods.MoveSpeed);
            if (wantSprint) speed *= Mathf.Max(0.1f, mods.SprintSpeed);
            CurrentSpeed = speed;

            // Gravity keeps us pinned to the ground.
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += CloneZConfig.Gravity * Time.deltaTime;

            Vector3 velocity = worldMove * speed + Vector3.up * _verticalVelocity;
            bool wasGrounded = _controller.isGrounded;
            LevelStructure.KeepUp(_controller);
            if (_controller.enabled) _controller.Move(velocity * Time.deltaTime);
            SnapDown(wasGrounded, velocity);
            TrackLanding();

            KeepInsideArena();
            EmitFootsteps(moving, wantSprint);
        }

        private void LateUpdate()
        {
            // Update cached ground Y and plane after movement has been applied.
            _groundY = transform.position.y;
            _groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundY, 0f));
        }

        private void EmitFootsteps(bool moving, bool sprinting)
        {
            if (!moving) { _footstepTimer = 0f; return; }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer > 0f) return;

            float radius, interval;
            if (IsSneaking) { radius = CloneZConfig.NoiseFootstepSneak; interval = 0.6f; }
            else if (sprinting) { radius = CloneZConfig.NoiseFootstepSprint; interval = 0.28f; }
            else { radius = CloneZConfig.NoiseFootstepWalk; interval = 0.45f; }

            // Lightfooted survivors tread more quietly, drawing fewer zombies (PZ getLightfootMod).
            if (_skills != null) radius *= _skills.FootstepNoiseMult;
            radius *= PlayerModifiers.Current.NoiseMult;

            NoiseSystem.Emit(transform.position, radius);
            Footstep?.Invoke(sprinting, IsSneaking);
            _footstepTimer = interval;
        }

        /// <summary>Raised on every footstep of the survivor (sprinting, sneaking) — the AI hears it; the audio follows the
        /// body's stride instead and falls back to this when there is no animated body.</summary>
        public event System.Action<bool, bool> Footstep;

        /// <summary>Raised when the survivor touches down after a drop, with how fast they hit the ground (m/s) — for the audio.</summary>
        public event System.Action<float> Landed;

        // Touching down after falling faster than a stumble off a kerb (sound only; no gameplay rides on it).
        private void TrackLanding()
        {
            if (!_controller.enabled) { _fallSpeed = 0f; return; }
            if (!_controller.isGrounded)
            {
                _fallSpeed = Mathf.Max(_fallSpeed, -_verticalVelocity);
                _airborneAt = Time.time;
                return;
            }
            if (_fallSpeed > 4.5f && Time.time - _airborneAt < 0.2f) Landed?.Invoke(_fallSpeed);
            _fallSpeed = 0f;
        }

        private void FaceAim()
        {
            // Mouse-look views: face where the camera looks and strafe (PZ); isometric: face the cursor.
            Vector3 dir = ViewState.MouseLook ? ViewForward() : AimDirection;
            if (dir == Vector3.zero) return;
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, CloneZConfig.PlayerTurnSpeed * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (!_controller.enabled)
            {
                // Seated, the body's capsule is off (the seat holds the survivor up).
                _verticalVelocity = 0f;
                return;
            }
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += CloneZConfig.Gravity * Time.deltaTime;
            LevelStructure.KeepUp(_controller);
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private static readonly RaycastHit[] s_SnapHits = new RaycastHit[8];

        // Walking down a slope or a flight of stairs faster than gravity pulls: back down onto the ground under the feet, no
        // further than a 45° slope drops over the step just taken (anything deeper is a fall).
        private void SnapDown(bool wasGrounded, Vector3 velocity)
        {
            if (!_controller.enabled || !wasGrounded || _controller.isGrounded || velocity.y > 0f) return;
            float flat = new Vector2(velocity.x, velocity.z).magnitude * Time.deltaTime;
            float big = _controller.radius, small = big * 0.9f;
            Vector3 bottom = transform.TransformPoint(_controller.center) + Vector3.down * (_controller.height * 0.5f - big);
            int n = Physics.SphereCastNonAlloc(bottom, small, Vector3.down, s_SnapHits, flat + 0.06f + (big - small), ~0, QueryTriggerInteraction.Ignore);
            // Over a basement the body passes through the ground's box (LevelStructure): it isn't ground there.
            bool overBasement = LevelStructure.OverBasement(transform.position);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = s_SnapHits[i];
                Collider c = h.collider;
                if (c == null || c == _controller || h.distance <= 0f || h.normal.y < 0.7f) continue;
                if (overBasement && c is BoxCollider && c.gameObject.name == "Ground") continue;
                best = Mathf.Min(best, h.distance);
            }
            if (best == float.MaxValue) return;
            _controller.Move(Vector3.down * Mathf.Max(0f, best - (big - small) + 0.01f));
        }

        private void KeepInsideArena()
        {
            Vector3 p = transform.position;
            // On the streamed Kentucky map the arena follows the survivor; only the edge of the world holds them.
            Vector3 q = KentuckyStreamer.Active != null ? CloneZConfig.ClampToWorld(p) : CloneZConfig.ClampToArena(p);
            if (q.x != p.x || q.z != p.z) transform.position = q;
        }
    }
}
