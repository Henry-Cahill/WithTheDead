using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Top-down survival movement: move relative to the camera, face the mouse
    /// cursor (Project Zomboid style), sprint/sneak with stamina, and radiate
    /// footstep noise that draws zombies.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        private CharacterController _controller;
        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerCombat _combat;
        private Camera _cam;

        private float _verticalVelocity;
        private float _footstepTimer;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
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
                ApplyGravityOnly();
                return;
            }

            if (UIState.GameplayBlocked)
            {
                _stats.Exertion = Exertion.Idle;
                ApplyGravityOnly();
                return;
            }

            Move();
            FaceAim();
        }

        private void UpdateAim()
        {
            Mouse mouse = Mouse.current;
            if (_cam == null || mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = _cam.ScreenPointToRay(screen);
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (ground.Raycast(ray, out float enter))
            {
                AimPoint = ray.GetPoint(enter);
                Vector3 dir = CloneZUtil.Flat(AimPoint - transform.position);
                if (dir != Vector3.zero) AimDirection = dir;
            }
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

            // Movement basis relative to the camera's yaw.
            Vector3 fwd = _cam != null ? CloneZUtil.Flat(_cam.transform.forward) : Vector3.forward;
            Vector3 right = _cam != null ? CloneZUtil.Flat(_cam.transform.right) : Vector3.right;
            if (fwd == Vector3.zero) fwd = Vector3.forward;
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
            Vector3 dir = AimDirection;
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
