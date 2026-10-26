using System.Collections.Generic;
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
        private PlayerGear _playerGear;
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

        // A raid's shut doors: a chase that dead-ends against them (a partial path) makes for the nearest one in the way and
        // batters it, rather than milling against a wall. Nothing of this runs where there are no raid doors.
        private RaidDoor _door;
        private Vector3 _doorStand;
        private float _doorRethinkAt;
        private int _doorChanges;          // RaidDoor.Changes when the way round was last looked for
        private bool _doorRecheck;         // .. and look once more (a carve lifts a frame or so after its door opens)
        private Vector3 _doorTarget;       // .. and where the target was then
        private static NavMeshPath s_DoorPath;

        // A lit flashlight draws the eye: a zombie standing in its beam spots the survivor from further off.
        private const float FlashlightBeamRange = 22f;
        private const float FlashlightVisionMultiplier = 1.6f;
        private static readonly float FlashlightBeamCos = Mathf.Cos(25f * Mathf.Deg2Rad);
        // Cache raycast mask that excludes the IgnoreRaycast layer for zombie sight checks.
        private static readonly int _sightRaycastMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        /// <summary>Updates the internal NavMeshAgent reference and other components.</summary>
        private void Awake()
        {
            _playerGear = PlayerGear.Instance;
            _agent = GetComponent<NavMeshAgent>();
            _renderer = GetComponentInChildren<Renderer>();
            _avatar = GetComponent<ZombieAvatar>();
            // Cache self actor reference
            _self = GetComponent<Actor>();
            // Cache player and its components if present at startup
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null)
            {
                _player = gm.Player;
                _playerController = _player.GetComponent<PlayerController>();
                _playerSkills = _player.GetComponent<PlayerSkills>();
            }
            _agent.updateRotation = false;
            _agent.stoppingDistance = CloneZConfig.ZombieAttackRange * 0.7f;
        }

        /// <summary>Initialises the zombie's type, applying colour and speed tweaks.</summary>
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

    // Add up to two tiny rust patches on the forearm for visual detail
        // Deterministic but random per zombie instance
        for (int i = 0; i < 2; i++)
        {
            if (Random.value < 0.5f)
            {
                var rust = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rust.transform.SetParent(transform, false);
                rust.transform.localScale = new Vector3(0.04f, 0.02f, 0.01f);
                rust.transform.localPosition = new Vector3(Random.Range(-0.05f, 0.05f), 1.15f, Random.Range(0.05f, 0.12f));
                rust.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var rend = rust.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = rend.material;
                    Color baseColor = new Color(0.4f, 0.25f, 0.1f);
                    float v = Random.Range(-0.05f, 0.05f);
                    Color col = new Color(baseColor.r + v, baseColor.g + v, baseColor.b + v);
                    mat.color = col;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                }
            }
        }
        // Add a tiny blood splotch on the face for visual detail
        if (Random.value < 0.3f)
        {
            var blood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blood.transform.SetParent(transform, false);
            blood.transform.localScale = new Vector3(0.07f, 0.02f, 0.02f);
            blood.transform.localPosition = new Vector3(Random.Range(-0.04f, 0.04f), 1.78f, Random.Range(-0.02f, 0.02f));
            blood.transform.localRotation = Quaternion.identity;
            var rend = blood.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = rend.material;
                Color col = new Color(0.5f, 0.05f, 0.05f);
                mat.color = col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            }
         }
        // Add torn‑cloth strip visual detail
        for (int i = 0; i < 2; i++)
        {
            if (Random.value < 0.5f)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.transform.SetParent(transform, false);
                strip.transform.localScale = new Vector3(0.01f, 0.08f, 0.02f);
                strip.transform.localPosition = new Vector3(Random.Range(-0.07f, 0.07f), 1.45f, Random.Range(-0.02f, 0.02f));
                strip.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-15f, 15f));
                var rend = strip.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = rend.material;
                    Color baseCol = new Color(0.5f, 0.4f, 0.35f);
                    float v = Random.Range(-0.04f, 0.04f);
                    Color col = new Color(baseCol.r + v, baseCol.g + v, baseCol.b + v);
                    mat.color = col;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                }
            }
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

            if (_player != null && _player.IsDead) _player = null;


            if (_state == State.Stagger) { TickStagger(); return; }

            _attackCooldown -= Time.deltaTime;
            // A survivor under a raid's spawn protection is invisible to the dead: anyone already on
            // their trail loses interest and goes back to wandering.
            if (_prey != null && IsProtected(_prey) && (_hasTarget || _state == State.Chase || _state == State.Attack))
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
            _door = null;
            _prey = null;
            _lastSeenTime = -999f;
            EnterIdle();
        }

        #region Perception
        // What the dead hunt: anything alive and warm that isn't one of the Zone's own — survivors (local and
        // remote), stalkers, companions. Gathered once a frame for every zombie.
        private static readonly List<Actor> s_Prey = new List<Actor>(16);
        private static int s_PreyFrame = -1;
        private Actor _self;
        private Actor _prey;
        private float _retarget;

        /// <summary>What this zombie is hunting (null when it isn't).</summary>
        public Actor Prey => _prey;

        /// <summary>Where the hunted thing is (for the body's lean and gaze), or null.</summary>
        /// <summary>The current chase target's position (or null if none).</summary>
        public Vector3? HuntTarget => _hasTarget && _prey != null ? _prey.transform.position : (Vector3?)null;

        /// <summary>Points this zombie at a target as if it had seen it (a Puppeteer's command, a lure).</summary>
        /// <summary>Issues a command to directly set a target for this zombie (used by Puppeteer).</summary>
        public void Command(Actor target)
        {
            if (_state == State.Dead || target == null || !target.IsAlive || IsProtected(target)) return;
            _prey = target;
            _hasTarget = true;
            _lastKnownPos = target.transform.position;
            _lastSeenTime = Time.time;
            _investigating = false;
        }

        private static bool IsProtected(Actor a) => a != null && a.Kind == ActorKind.Player && ExtractionManager.PlayerProtected;

        private void RefreshPrey()
        {
            if (s_PreyFrame == Time.frameCount) return;
            s_PreyFrame = Time.frameCount;
            s_Prey.Clear();
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a == _self || a.Kind == ActorKind.Zombie || !a.IsAlive || a.Untargetable) continue;
                if (_self != null && !_self.IsHostileTo(a)) continue;
                s_Prey.Add(a);
            }
        }

        /// <summary>Finds the current prey, updates target data and reacts to visible or heard cues.</summary>
        private void Perceive()
        {
            if (_prey != null && (!_prey.IsAlive || _prey.Untargetable || IsProtected(_prey))) _prey = null;
            bool seen = _prey != null && CanSee(_prey);
            _retarget -= Time.deltaTime;
            if (_retarget <= 0f)
            {
                _retarget = Random.Range(0.3f, 0.5f);
                Actor best = FindVisiblePrey(seen ? _prey : null);
                if (best != null) { _prey = best; seen = true; }
            }

            if (seen)
            {
                _hasTarget = true;
                _lastKnownPos = _prey.transform.position;
                _lastSeenTime = Time.time;
                _investigating = false;
                return;
            }

            // Can't see anyone — listen for noise instead (a protected survivor's own noise included: they make none).
            if (_state != State.Chase && _state != State.Attack &&
                NoiseSystem.TryHear(transform.position, CloneZConfig.ZombieHearingRadius, out Vector3 noisePos, out _))
            {
                if (ExtractionManager.PlayerProtected && _player != null && (noisePos - _player.transform.position).sqrMagnitude < 4f) return;
                _investigatePos = noisePos;
                _investigating = true;
            }
        }

        // The nearest prey this zombie can see; the current one is kept unless another is much closer.
        private Actor FindVisiblePrey(Actor current)
        {
            RefreshPrey();
            Actor best = current;
            float bestSqr = current != null ? (current.transform.position - transform.position).sqrMagnitude * 0.5f : float.MaxValue;
            float far = EffectiveVisionRadius() * 1.2f;
            for (int i = 0; i < s_Prey.Count; i++)
            {
                Actor a = s_Prey[i];
                if (a == null || a == current || IsProtected(a)) continue;
                float sqr = (a.transform.position - transform.position).sqrMagnitude;
                if (sqr > far * far || sqr >= bestSqr) continue;
                if (!CanSee(a)) continue;
                best = a;
                bestSqr = sqr;
            }
            return best;
        }

        private bool CanSee(Actor target)
        {
            if (target == null || IsProtected(target)) return false;
            Vector3 to = target.transform.position - transform.position;
            float dist = to.magnitude;
            if (target.Cloaked && dist > 2.5f) return false;

            // Light on them, their stance and camouflage (see Stealth) bend how far off they're noticed.
            float radius = EffectiveVisionRadius() * Mathf.Lerp(0.55f, 1.15f, target.Visibility01);
            if (_player != null && target.transform == _player.transform)
            {

                // A crouched survivor with Sneak skill is harder to notice (PZ getSneakSpotMod).
                if (_playerSkills != null && _playerController != null && _playerController.IsSneaking)
                    radius = Mathf.Max(CloneZConfig.ZombieAttackRange + 0.5f, radius * _playerSkills.StealthVisionMult(true));
                // ...but shining a flashlight at one gives them away, sneaking or not.
                if (InFlashlightBeam(-to)) radius *= FlashlightVisionMultiplier;
            }
            radius = Mathf.Max(CloneZConfig.ZombieAttackRange + 0.5f, radius);
            if (dist > radius) return false;

            bool alerted = _state == State.Chase || _state == State.Attack;
            if (!alerted)
            {
                float angle = Vector3.Angle(transform.forward, CloneZUtil.Flat(to));
                if (angle > CloneZConfig.ZombieVisionAngle * 0.5f) return false;
            }

            // Line of sight: obstacles (Default layer) block; other zombies (Ignore
            // Raycast layer) are excluded from the mask so they don't block sight.
            int mask = _sightRaycastMask;
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 chest = target.transform.position + Vector3.up * 1.0f;
            if (Physics.Linecast(eye, chest, out RaycastHit hit, mask, QueryTriggerInteraction.Ignore))
                return hit.collider.transform.IsChildOf(target.transform);
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

            if (_playerGear == null || !_playerGear.FlashlightOn || _playerController == null) return false;
            fromPlayer.y = 0f;
            float sqr = fromPlayer.sqrMagnitude;
            if (sqr > FlashlightBeamRange * FlashlightBeamRange) return false;
            if (sqr < 0.01f) return false;
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

            // A zombie set on a shut door keeps at it a while after its prey drops out of sight behind it.
            bool lost = Time.time - _lastSeenTime > (_door != null ? CloneZConfig.ZombieDoorForgetTime : CloneZConfig.ZombieForgetTime);
            if (lost && !_hasTargetVisibleNow())
            {
                _hasTarget = false;
                _door = null;
                _investigatePos = _lastKnownPos;
                _investigating = true;
                _state = State.Investigate;
                return;
            }

            Vector3 targetPos = (_prey != null && Time.time - _lastSeenTime < 0.25f)
                ? _prey.transform.position
                : _lastKnownPos;
            SafeSetDestination(RaidDoor.Registry.Count > 0 ? DoorDetour(targetPos) : targetPos);

            // A barricade or house wall standing between us and the target has to be torn down
            // first — the carved NavMesh already dead-ends us against it, so start swinging.
            if (_attackCooldown <= 0f && _prey != null && _prey.IsAlive)
            {
                IDamageable wall = NearestObstruction(_prey.transform.position);
                if (wall != null)
                {
                    StartCoroutine(DoAttackStructure(wall));
                    return;
                }
            }

            if (_prey != null && _prey.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, _prey.transform.position);
                if (dist <= CloneZConfig.ZombieAttackRange && _attackCooldown <= 0f)
                    StartCoroutine(DoAttack());
            }
        }

        private bool _hasTargetVisibleNow() => _prey != null && _prey.IsAlive && CanSee(_prey);

        // The nearest player-built structure (barricade or house wall/doorway) that blocks the
        // path to the target — whichever is closer if both are in reach — or, in a raid, a shut
        // door standing in the way.
        private IDamageable NearestObstruction(Vector3 targetPos)
        {
            float range = CloneZConfig.ZombieAttackRange + 0.8f;
            Barricade bar = Barricade.NearestObstructing(transform.position, targetPos, range);
            BuildPiece piece = BuildPiece.NearestObstructing(transform.position, targetPos, range);
            IDamageable wall;
            if (bar == null) wall = piece;
            else if (piece == null) wall = bar;
            else
            {
                float db = (bar.Position - transform.position).sqrMagnitude;
                float dp = (piece.Position - transform.position).sqrMagnitude;
                wall = db <= dp ? (IDamageable)bar : piece;
            }
            if (RaidDoor.Registry.Count == 0) return wall;

            RaidDoor door = DoorInReach(targetPos, range);
            if (door == null) return wall;
            if (wall == null) return door;
            return door.SqrDistance(transform.position) <= (wall.Transform.position - transform.position).sqrMagnitude ? door : wall;
        }

        // The shut raid door to batter: the one this zombie made for once it's within reach, or — while the target can't be
        // reached at all — one in reach standing between them (which it then sticks with). Never a door it could simply
        // walk round.
        private RaidDoor DoorInReach(Vector3 targetPos, float range)
        {
            Vector3 p = transform.position;
            if (_door != null && _door.Obstructs && _door.SqrDistance(p) <= range * range) return _door;
            if (!_agent.enabled || !_agent.isOnNavMesh || _agent.pathPending || _agent.pathStatus != NavMeshPathStatus.PathPartial) return null;
            RaidDoor door = RaidDoor.NearestObstructing(p, targetPos, range);
            if (door != null)
            {
                _door = door;
                _doorStand = door.StandingPoint(door.SideOfDoor(p));
                _doorChanges = RaidDoor.Changes;
                _doorTarget = targetPos;
            }
            return door;
        }

        // While the target is out of reach behind shut doors (the path ends short of it), heads for the nearest shut door it
        // can walk up to that leads toward the target, and stays with it until the door opens or bursts, or a way round turns
        // up — looked for only when a door has changed or the target has moved. Thought over a few times a second at most
        // (less often while no door offers a way), with a path shared by every zombie (no allocation).
        private Vector3 DoorDetour(Vector3 targetPos)
        {
            if (_door != null && !_door.Obstructs) _door = null;   // opened, or battered down: on through
            float now = Time.time;
            if (now >= _doorRethinkAt && _agent.enabled && _agent.isOnNavMesh && !_agent.pathPending)
            {
                _doorRethinkAt = now + CloneZConfig.ZombieDoorRethink * Random.Range(0.75f, 1.25f);
                if (s_DoorPath == null) s_DoorPath = new NavMeshPath();
                if (_door == null)
                {
                    if (_agent.pathStatus == NavMeshPathStatus.PathPartial)
                    {
                        _door = RaidDoor.Detour(transform.position, targetPos, CloneZConfig.ZombieDoorSearchRange, s_DoorPath, out _doorStand);
                        if (_door == null) _doorRethinkAt = now + CloneZConfig.ZombieDoorIdleRethink * Random.Range(0.75f, 1.25f);
                        _doorChanges = RaidDoor.Changes;
                        _doorTarget = targetPos;
                    }
                }
                else if (RaidDoor.Changes != _doorChanges || _doorRecheck || (targetPos - _doorTarget).sqrMagnitude > 2.25f)
                {
                    _doorRecheck = RaidDoor.Changes != _doorChanges;
                    _doorChanges = RaidDoor.Changes;
                    _doorTarget = targetPos;
                    if (Reachable(targetPos)) _door = null;   // a way round has opened up
                }
            }
            return _door != null ? _doorStand : targetPos;
        }

        // True when a complete path runs from here to (the navmesh nearest) `target`.
        private bool Reachable(Vector3 target)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) target = hit.position;
            return NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, s_DoorPath)
                && s_DoorPath.status == NavMeshPathStatus.PathComplete;
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
            ZombieVoices.Say(this, SfxIds.ZombieAttack, transform.position + Vector3.up * (Type == ZombieType.Crawler ? 0.45f : 1.6f), 0.9f, 30f);

            // Wind-up (the lunge) before the strike lands.
            float t = 0f;
            while (t < CloneZConfig.ZombieAttackWindup)
            {
                if (_state == State.Dead || _state == State.Stagger) yield break;
                if (_prey != null) FaceTowards(_prey.transform.position);
                t += Time.deltaTime;
                yield return null;
            }

            if (_state == State.Dead || _state == State.Stagger) yield break;

            // Land the hit if still in reach (and the survivor isn't under a raid's spawn protection).
            if (_prey != null && _prey.IsAlive && !IsProtected(_prey) &&
                Vector3.Distance(transform.position, _prey.transform.position) <= CloneZConfig.ZombieAttackRange + 0.3f)
            {
                bool bite = Random.value < CloneZConfig.ZombieBiteChance;
                var survivor = _prey.GetComponent<PlayerStats>();
                IZombieAttackable other = survivor == null ? _prey.GetComponent<IZombieAttackable>() : null;
                if (survivor != null) survivor.ReceiveZombieAttack(bite);
                else if (other != null) other.ReceiveZombieAttack(bite);
                else if (_prey.Damageable != null)
                {
                    var info = new DamageInfo(bite ? 1.6f : 0.7f, bite ? DamageKind.Bite : DamageKind.Melee, _prey.Center,
                        CloneZUtil.Flat(_prey.transform.position - transform.position), gameObject) { BleedChance = 0.4f };
                    CombatResolver.Apply(_prey.Damageable, info.WithZone(BodyZones.RandomStrike()), _self);
                }
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
            Sfx.Play(SfxIds.DoorBash, transform.position + Vector3.up * 1.2f + transform.forward * 0.6f, 0.6f, Random.Range(0.85f, 1.1f), 36f);

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
        /// <summary>Applies a stagger effect, stopping movement and notifying listeners.</summary>
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

        /// <summary>Ends the zombie's life, records death direction, stops physics and triggers fall animation.</summary>
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
