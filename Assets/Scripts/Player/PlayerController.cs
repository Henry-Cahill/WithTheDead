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
        private Camera _cam;

        private float _verticalVelocity;
        private float _footstepTimer;

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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            AimDirection = transform.forward;
        }

        private void Start() => _cam = Camera.main;

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            UpdateAim();

            if (_stats.IsDead)
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

            Move();
            FaceAim();
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
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
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
                // Nothing solid under the crosshair: the ground plane if it's looking down, else open sky.
                var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                AimPoint = ground.Raycast(ray, out float enter) && enter > skip && enter < skip + CrosshairRange
                    ? ray.GetPoint(enter)
                    : ray.GetPoint(skip + CrosshairSkyDistance);
            }

            CrosshairZombie = null;
            if (CastPastSelf(origin, dir, solid, 1 << CloneZConfig.IgnoreRaycastLayer, out RaycastHit zombieHit, out _))
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
            if (_skills == null) _skills = GetComponent<PlayerSkills>();
            if (_combat == null) _combat = GetComponent<PlayerCombat>();
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
            if (_inventory == null) _inventory = GetComponent<PlayerInventory>();
            if (_inventory != null) speed *= _inventory.MoveSpeedMultiplier;
            // An exhausted or badly-hurt survivor moves slower still.
            speed *= _stats.FatigueMoveMultiplier;
            speed *= _stats.PainMoveMultiplier;
            speed *= _stats.ColdMoveMultiplier;   // cold-stiff muscles and heavy wet clothes drag
            speed *= _stats.SicknessMoveMultiplier; // food poisoning slows a queasy survivor to a trudge
            CurrentSpeed = speed;

            // Gravity keeps us pinned to the ground.
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += CloneZConfig.Gravity * Time.deltaTime;

            Vector3 velocity = worldMove * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            KeepInsideArena();
            EmitFootsteps(moving, wantSprint);
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

            NoiseSystem.Emit(transform.position, radius);
            _footstepTimer = interval;
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
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += CloneZConfig.Gravity * Time.deltaTime;
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private void KeepInsideArena()
        {
            float half = CloneZConfig.ArenaHalfSize;
            Vector3 p = transform.position;
            if (Mathf.Abs(p.x) > half || Mathf.Abs(p.z) > half)
            {
                p.x = Mathf.Clamp(p.x, -half, half);
                p.z = Mathf.Clamp(p.z, -half, half);
                transform.position = p;
            }
        }
    }
}
