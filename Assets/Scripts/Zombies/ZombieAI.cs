using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    public enum ZombieType { Shambler, FastShambler, Sprinter, Crawler }

    /// <summary>
    /// NavMesh-driven zombie brain. A state machine (idle → wander → investigate
    /// noise → chase → attack) that detects the player by sight and sound, mirroring
    /// Project Zomboid's zombie AI states and vision/attraction model.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ZombieHealth))]
    public class ZombieAI : MonoBehaviour
    {
        private enum State { Idle, Wander, Investigate, Chase, Attack, Stagger, Dead }

        [Header("Type")]
        public ZombieType Type = ZombieType.Shambler;

        /// <summary>True while this zombie is actively chasing or attacking (read by the panic system).</summary>
        public bool IsHunting => _state == State.Chase || _state == State.Attack;
        /// <summary>True once the zombie has died and its brain is switched off.</summary>
        public bool IsDeadState => _state == State.Dead;
        /// <summary>Direction the killing blow travelled (zero if unknown), so the body falls away from it.</summary>
        public Vector3 DeathDirection { get; private set; }

        /// <summary>Raised as an attack winds up; true when it is aimed at a barricade or wall.</summary>
        public event System.Action<bool> AttackStarted;
        /// <summary>Raised when the zombie is knocked back: the push and how long it reels.</summary>
        public event System.Action<Vector3, float> Staggered;

        private NavMeshAgent _agent;
        private Renderer _renderer;
        private ZombieAvatar _avatar;
        private PlayerStats _player;
        private PlayerController _playerController;
        private PlayerSkills _playerSkills;

        private State _state = State.Idle;
        private float _stateTimer;
        private float _baseSpeed = CloneZConfig.ZombieShamblerSpeed;

        // Perception
        private bool _hasTarget;
        private Vector3 _lastKnownPos;
        private float _lastSeenTime = -999f;

        // Investigate
        private Vector3 _investigatePos;
        private bool _investigating;

        // Attack
        private float _attackCooldown;

        // Stagger / knockback
        private float _staggerTimer;
        private Vector3 _knockback;

        // A lit flashlight draws the eye: a zombie standing in its beam spots the survivor from further off.
        private const float FlashlightBeamRange = 22f;
        private const float FlashlightVisionMultiplier = 1.6f;
        private static readonly float FlashlightBeamCos = Mathf.Cos(25f * Mathf.Deg2Rad);

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _renderer = GetComponentInChildren<Renderer>();
            _avatar = GetComponent<ZombieAvatar>();
            _agent.updateRotation = false;
            _agent.stoppingDistance = CloneZConfig.ZombieAttackRange * 0.7f;
        }

        public void Configure(ZombieType type)
        {
            Type = type;
            Color tint;
            switch (type)
            {
                case ZombieType.Sprinter: _baseSpeed = CloneZConfig.ZombieSprinterSpeed; tint = new Color(0.45f, 0.18f, 0.15f); break;
                case ZombieType.FastShambler: _baseSpeed = CloneZConfig.ZombieFastShamblerSpeed; tint = new Color(0.30f, 0.42f, 0.22f); break;
                case ZombieType.Crawler: _baseSpeed = CloneZConfig.ZombieCrawlerSpeed; tint = new Color(0.5f, 0.5f, 0.42f); break;
                default: _baseSpeed = CloneZConfig.ZombieShamblerSpeed; tint = new Color(0.36f, 0.5f, 0.30f); break;
            }

            if (_avatar == null) _avatar = GetComponent<ZombieAvatar>();
            if (_avatar != null) _avatar.ApplyType(type, tint);   // the humanoid crawls rather than being squashed
            else
            {
                SetColor(tint);
                if (type == ZombieType.Crawler) transform.localScale = new Vector3(1f, 0.5f, 1f);
            }

            _agent.speed = _baseSpeed * 0.5f;
            _agent.acceleration = 12f;
            _agent.angularSpeed = 999f;
            EnterIdle();
        }

        private void SetColor(Color c)
        {
            if (_renderer == null) return;
            var mat = _renderer.material; // instance so each zombie can differ
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }

        private void Update()
        {
            if (_state == State.Dead) return;

            if (_player == null || _player.IsDead)
                _player = GameManager.Instance != null ? GameManager.Instance.Player : null;

            if (_state == State.Stagger) { TickStagger(); return; }

            _attackCooldown -= Time.deltaTime;
            // A survivor under a raid's spawn protection is invisible to the dead: anyone already on
            // their trail loses interest and goes back to wandering.
            if (ExtractionManager.PlayerProtected && (_hasTarget || _state == State.Chase || _state == State.Attack))
                GiveUpTarget();
            Perceive();
            Think();
            FaceMovement();
        }

        private void GiveUpTarget()
        {
            StopAllCoroutines();   // drops a lunge or a bash under way (the brain is alive, so no death fall runs)
            _hasTarget = false;
            _investigating = false;
            _lastSeenTime = -999f;
            EnterIdle();
        }

        #region Perception
        private void Perceive()
        {
            // Nothing to see or hear of a protected survivor (their own noise included).
            if (ExtractionManager.PlayerProtected) return;

            if (_player != null && !_player.IsDead && CanSeePlayer())
            {
                _hasTarget = true;
                _lastKnownPos = _player.transform.position;
                _lastSeenTime = Time.time;
                _investigating = false;
                return;
            }

            // Can't see the player — listen for noise instead.
            if (_state != State.Chase && _state != State.Attack &&
                NoiseSystem.TryHear(transform.position, CloneZConfig.ZombieHearingRadius, out Vector3 noisePos, out _))
            {
                _investigatePos = noisePos;
                _investigating = true;
            }
        }

        private bool CanSeePlayer()
        {
            if (ExtractionManager.PlayerProtected) return false;
            if (_playerController == null) _playerController = _player.GetComponent<PlayerController>();
            if (_playerSkills == null) _playerSkills = _player.GetComponent<PlayerSkills>();

            Vector3 to = _player.transform.position - transform.position;
            float dist = to.magnitude;

            // A crouched survivor with Sneak skill is harder to notice (PZ getSneakSpotMod).
            float radius = EffectiveVisionRadius();
            if (_playerSkills != null && _playerController != null && _playerController.IsSneaking)
                radius = Mathf.Max(CloneZConfig.ZombieAttackRange + 0.5f, radius * _playerSkills.StealthVisionMult(true));
            // ...but shining a flashlight at one gives them away, sneaking or not.
            if (InFlashlightBeam(-to)) radius *= FlashlightVisionMultiplier;
            if (dist > radius) return false;

            bool alerted = _state == State.Chase || _state == State.Attack;
            if (!alerted)
            {
                float angle = Vector3.Angle(transform.forward, CloneZUtil.Flat(to));
                if (angle > CloneZConfig.ZombieVisionAngle * 0.5f) return false;
            }

            // Line of sight: obstacles (Default layer) block; other zombies (Ignore
            // Raycast layer) are excluded from the mask so they don't block sight.
            int mask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 chest = _player.transform.position + Vector3.up * 1.0f;
            if (Physics.Linecast(eye, chest, out RaycastHit hit, mask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlayerStats>() != null;
            return true;
        }

        /// <summary>
        /// Sight radius after darkness and weather (PZ shrinks a zombie's vision in the
        /// dark, rain and fog). Never drops below arm's reach so they still catch you up close.
        /// </summary>
        private float EffectiveVisionRadius()
        {
            float m = 1f;
            if (TimeOfDay.Instance != null) m *= TimeOfDay.Instance.ZombieVisionMultiplier;
            if (WeatherSystem.Instance != null) m *= WeatherSystem.Instance.VisionMultiplier;
            return Mathf.Max(CloneZConfig.ZombieAttackRange + 0.5f, CloneZConfig.ZombieVisionRadius * m);
        }

        /// <summary>True when the survivor's flashlight is on and this zombie stands in its beam: within
        /// range and inside the cone around the aim direction. <paramref name="fromPlayer"/> is the vector
        /// from the survivor to this zombie.</summary>
        private bool InFlashlightBeam(Vector3 fromPlayer)
        {
            PlayerGear gear = PlayerGear.Instance;
            if (gear == null || !gear.FlashlightOn || _playerController == null) return false;
            fromPlayer.y = 0f;
            float sqr = fromPlayer.sqrMagnitude;
            if (sqr > FlashlightBeamRange * FlashlightBeamRange) return false;
            if (sqr < 0.01f) return true;
            Vector3 aim = _playerController.AimDirection;
            aim.y = 0f;
            float aimSqr = aim.sqrMagnitude;
            if (aimSqr < 1e-6f) return false;
            return Vector3.Dot(aim, fromPlayer) / Mathf.Sqrt(aimSqr * sqr) >= FlashlightBeamCos;
        }
        #endregion

        #region State machine
        private void Think()
        {
            switch (_state)
            {
                case State.Idle: TickIdle(); break;
                case State.Wander: TickWander(); break;
                case State.Investigate: TickInvestigate(); break;
                case State.Chase: TickChase(); break;
                case State.Attack: TickAttack(); break;
            }
        }

        private void EnterIdle()
        {
            _state = State.Idle;
            _stateTimer = Random.Range(0.4f, 1.2f);
            SafeStop();
        }

        private void TickIdle()
        {
            if (BecomeAggressiveIfPossible()) return;
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                // PZ wanders to a random nearby tile at random intervals.
                if (TryPickNavPoint(transform.position, 6f, out Vector3 p))
                {
                    _investigatePos = p;
                    _state = State.Wander;
                    _agent.speed = _baseSpeed * 0.45f;
                    SafeSetDestination(p);
                }
                else EnterIdle();
            }
        }

        private void TickWander()
        {
            if (BecomeAggressiveIfPossible()) return;
            if (!_agent.pathPending && _agent.remainingDistance <= 1.0f) EnterIdle();
        }

        private void TickInvestigate()
        {
            if (BecomeAggressiveIfPossible()) return;
            _agent.speed = _baseSpeed * 0.7f;
            SafeSetDestination(_investigatePos);
            if (!_agent.pathPending && _agent.remainingDistance <= 1.2f)
            {
                _investigating = false;
                EnterIdle();
            }
        }

        private bool BecomeAggressiveIfPossible()
        {
            if (_hasTarget)
            {
                _state = State.Chase;
                _agent.speed = _baseSpeed;
                return true;
            }
            if (_investigating)
            {
                _state = State.Investigate;
                return true;
            }
            return false;
        }

        private void TickChase()
        {
            _agent.speed = _baseSpeed;

            bool lost = Time.time - _lastSeenTime > CloneZConfig.ZombieForgetTime;
            if (lost && !_hasTargetVisibleNow())
            {
                _hasTarget = false;
                _investigatePos = _lastKnownPos;
                _investigating = true;
                _state = State.Investigate;
                return;
            }

            Vector3 targetPos = (_player != null && Time.time - _lastSeenTime < 0.25f)
                ? _player.transform.position
                : _lastKnownPos;
            SafeSetDestination(targetPos);

            // A barricade or house wall standing between us and the target has to be torn down
            // first — the carved NavMesh already dead-ends us against it, so start swinging.
            if (_attackCooldown <= 0f && _player != null && !_player.IsDead)
            {
                IDamageable wall = NearestObstruction(_player.transform.position);
                if (wall != null)
                {
                    StartCoroutine(DoAttackStructure(wall));
                    return;
                }
            }

            if (_player != null && !_player.IsDead)
            {
                float dist = Vector3.Distance(transform.position, _player.transform.position);
                if (dist <= CloneZConfig.ZombieAttackRange && _attackCooldown <= 0f)
                    StartCoroutine(DoAttack());
            }
        }

        private bool _hasTargetVisibleNow() => _player != null && !_player.IsDead && CanSeePlayer();

        // The nearest player-built structure (barricade or house wall/doorway) that blocks the
        // path to the target — whichever is closer if both are in reach.
        private IDamageable NearestObstruction(Vector3 targetPos)
        {
            float range = CloneZConfig.ZombieAttackRange + 0.8f;
            Barricade bar = Barricade.NearestObstructing(transform.position, targetPos, range);
            BuildPiece piece = BuildPiece.NearestObstructing(transform.position, targetPos, range);
            if (bar == null) return piece;
            if (piece == null) return bar;
            float db = (bar.Position - transform.position).sqrMagnitude;
            float dp = (piece.Position - transform.position).sqrMagnitude;
            return db <= dp ? (IDamageable)bar : piece;
        }

        private void TickAttack()
        {
            // Handled by the DoAttack coroutine; hold position and face the player.
            SafeStop();
        }

        private IEnumerator DoAttack()
        {
            _state = State.Attack;
            _attackCooldown = CloneZConfig.ZombieAttackCooldown;
            SafeStop();
            AttackStarted?.Invoke(false);

            // Wind-up (the lunge) before the strike lands.
            float t = 0f;
            while (t < CloneZConfig.ZombieAttackWindup)
            {
                if (_state == State.Dead || _state == State.Stagger) yield break;
                if (_player != null) FaceTowards(_player.transform.position);
                t += Time.deltaTime;
                yield return null;
            }

            if (_state == State.Dead || _state == State.Stagger) yield break;

            // Land the hit if still in reach (and the survivor isn't under a raid's spawn protection).
            if (_player != null && !_player.IsDead && !ExtractionManager.PlayerProtected &&
                Vector3.Distance(transform.position, _player.transform.position) <= CloneZConfig.ZombieAttackRange + 0.3f)
            {
                bool bite = Random.value < CloneZConfig.ZombieBiteChance;
                _player.ReceiveZombieAttack(bite);
            }

            if (_state != State.Dead && _state != State.Stagger)
            {
                _state = State.Chase;
                SafeResume();
            }
        }

        // Beats on a structure (barricade or house wall) that blocks the path to the player,
        // chipping away at its health. (The reference is typed as the interface, so we lean on
        // IsDead — not a Unity null check — to notice it was destroyed mid-swing.)
        private IEnumerator DoAttackStructure(IDamageable wall)
        {
            _state = State.Attack;
            _attackCooldown = CloneZConfig.ZombieAttackCooldown;
            SafeStop();
            AttackStarted?.Invoke(true);

            float t = 0f;
            while (t < CloneZConfig.ZombieAttackWindup)
            {
                if (_state == State.Dead || _state == State.Stagger) yield break;
                if (wall != null && !wall.IsDead) FaceTowards(wall.Transform.position);
                t += Time.deltaTime;
                yield return null;
            }

            if (_state == State.Dead || _state == State.Stagger) yield break;

            if (wall != null && !wall.IsDead &&
                Vector3.Distance(transform.position, wall.Transform.position) <= CloneZConfig.ZombieAttackRange + 1.0f)
            {
                Vector3 pos = wall.Transform.position;
                var info = new DamageInfo(
                    CloneZConfig.ZombieBarricadeDamage, DamageKind.Melee,
                    pos, CloneZUtil.Flat(pos - transform.position), gameObject);
                wall.TakeDamage(info);
            }

            if (_state != State.Dead && _state != State.Stagger)
            {
                _state = State.Chase;
                SafeResume();
            }
        }
        #endregion

        #region Stagger & death
        public void Stagger(float duration, Vector3 direction, float force)
        {
            if (_state == State.Dead) return;
            StopAllCoroutines();
            _state = State.Stagger;
            _staggerTimer = duration;
            _knockback = CloneZUtil.Flat(direction) * force;
            SafeStop();
            Staggered?.Invoke(CloneZUtil.Flat(direction) * Mathf.Max(force, 0.1f), duration);
        }

        private void TickStagger()
        {
            _staggerTimer -= Time.deltaTime;
            if (_agent.isOnNavMesh && _knockback.sqrMagnitude > 0.01f)
                _agent.Move(_knockback * Time.deltaTime);
            _knockback = Vector3.Lerp(_knockback, Vector3.zero, Time.deltaTime * 8f);

            if (_staggerTimer <= 0f)
            {
                _state = _hasTarget ? State.Chase : State.Idle;
                SafeResume();
                if (_state == State.Idle) EnterIdle();
            }
        }

        public void Kill(Vector3 hitDirection = default)
        {
            if (_state == State.Dead) return;
            _state = State.Dead;
            DeathDirection = CloneZUtil.Flat(hitDirection);
            StopAllCoroutines();
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _agent.enabled = false;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            StartCoroutine(DeathFall());
        }

        private IEnumerator DeathFall()
        {
            // Topple over (the humanoid body plays its own collapse), then sink into the ground and despawn.
            if (_avatar == null)
            {
                Quaternion from = transform.rotation;
                Quaternion to = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2.5f;
                    transform.rotation = Quaternion.Slerp(from, to, t);
                    yield return null;
                }
            }
            yield return new WaitForSeconds(_avatar == null ? 2.5f : 2.9f);

            float s = 0f;
            Vector3 start = transform.position;
            while (s < 1f)
            {
                s += Time.deltaTime;
                transform.position = start + Vector3.down * s * 1.5f;
                yield return null;
            }
            Destroy(gameObject);
        }
        #endregion

        #region NavMesh helpers
        private void FaceMovement()
        {
            Vector3 dir = _agent.enabled && _agent.velocity.sqrMagnitude > 0.05f
                ? _agent.velocity
                : (_hasTarget && _player != null ? _player.transform.position - transform.position : Vector3.zero);
            dir = CloneZUtil.Flat(dir);
            if (dir == Vector3.zero) return;
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
        }

        private void FaceTowards(Vector3 worldPos) => FaceDir(worldPos - transform.position);

        private void FaceDir(Vector3 dir)
        {
            dir = CloneZUtil.Flat(dir);
            if (dir == Vector3.zero) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 12f * Time.deltaTime);
        }

        private void SafeStop()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }

        private void SafeResume()
        {
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        }

        private void SafeSetDestination(Vector3 pos)
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(pos);
            }
        }

        private static bool TryPickNavPoint(Vector3 center, float radius, out Vector3 result)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(r.x, 0f, r.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = center;
            return false;
        }
        #endregion
    }
}
