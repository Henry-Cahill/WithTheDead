using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A zombie's body: the same humanoid as the survivor, dressed as a random dead survivor (grimy
    /// clothes, pale eyes, blood) and animated from <see cref="ZombieAI"/> — a shuffle, lurch, sprint
    /// or crawl by type, arms reaching for prey, lunges and barricade-beating on attack, staggering
    /// when struck, and a collapse on death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZombieAvatar : MonoBehaviour
    {
        public HumanoidRig Rig { get; private set; }
        public HumanoidAnimator Animator { get; private set; }

        private ZombieAI _ai;
        private HumanoidLook _look;
        private bool _reaches;     // this one shambles with its arms out even when idle
        private bool _deathShown;
        private Vector3 _lastPos;
        private bool _hasLastPos;

        private void Awake()
        {
            var rng = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            _look = HumanoidLook.RandomZombie(rng);
            Rig = HumanoidRig.Create(transform, _look);
            Animator = Rig.gameObject.AddComponent<HumanoidAnimator>();
            Animator.AutoUpdate = false;
            Animator.Gait = HumanoidGait.Shambler;
            Animator.Limp = rng.NextDouble() < 0.3 ? 0.4f + 0.6f * (float)rng.NextDouble() : 0f;
            _reaches = rng.NextDouble() < 0.5;
        }

        /// <summary>Sets the gait for the zombie type and tints the skin with the type's colour, so the
        /// dangerous ones still read at a glance. A crawler keeps the low hit box it always had.</summary>
        public void ApplyType(ZombieType type, Color skin)
        {
            _look.SkinColor = skin;
            Rig.ApplyLook(_look);

            switch (type)
            {
                case ZombieType.Sprinter: Animator.Gait = HumanoidGait.Runner; Animator.Limp = 0f; break;
                case ZombieType.FastShambler: Animator.Gait = HumanoidGait.Lurcher; break;
                case ZombieType.Crawler: Animator.Gait = HumanoidGait.Crawler; break;
                default: Animator.Gait = HumanoidGait.Shambler; break;
            }

            if (type == ZombieType.Crawler)
            {
                // A crawler lies a body length along the ground, so its hit box does too. It stays a metre
                // tall, as the old squashed capsule was, so gunfire cast just below chest height connects.
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null) capsule.enabled = false;
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.5f, HumanoidAnimator.CrawlShift + 0.9f);
                box.size = new Vector3(0.6f, 1f, 1.9f);
                var agent = GetComponent<NavMeshAgent>();
                if (agent != null) agent.height = 1f;
            }
        }

        private void Start()
        {
            _ai = GetComponent<ZombieAI>();
            if (_ai != null)
            {
                _ai.AttackStarted += HandleAttack;
                _ai.Staggered += HandleStagger;
            }
            _lastPos = transform.position;
            _hasLastPos = true;
            Animator.Snap();
        }

        private void OnDestroy()
        {
            if (_ai == null) return;
            _ai.AttackStarted -= HandleAttack;
            _ai.Staggered -= HandleStagger;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Rig == null) return;

            Vector3 pos = transform.position;
            Vector3 velocity = _hasLastPos ? (pos - _lastPos) / dt : Vector3.zero;
            if (velocity.sqrMagnitude > 400f) velocity = Vector3.zero;
            _lastPos = pos;
            _hasLastPos = true;

            HumanoidAnimator a = Animator;
            if (_ai != null && _ai.IsDeadState)
            {
                if (!_deathShown)
                {
                    _deathShown = true;
                    a.Die(_ai.DeathDirection);
                }
                a.Velocity = Vector3.zero;
                a.Evaluate(dt);
                return;
            }

            bool hunting = _ai != null && _ai.IsHunting;
            a.Velocity = velocity;
            a.Hunting = hunting;
            a.ArmsOut = hunting || _reaches;

            PlayerStats prey = hunting && GameManager.Instance != null ? GameManager.Instance.Player : null;
            a.HasLookTarget = prey != null && !prey.IsDead;
            if (a.HasLookTarget) a.LookTarget = prey.transform.position + Vector3.up * 1.6f;

            a.Evaluate(dt);
        }

        private void HandleAttack(bool structure)
        {
            if (structure) Animator.Bash(CloneZConfig.ZombieAttackWindup);
            else Animator.Lunge(CloneZConfig.ZombieAttackWindup);
        }

        private void HandleStagger(Vector3 push, float duration) => Animator.Stagger(push, duration);
    }
}
