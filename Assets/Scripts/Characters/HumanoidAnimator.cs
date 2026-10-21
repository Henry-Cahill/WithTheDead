using UnityEngine;

namespace CloneZ
{
    /// <summary>How a humanoid moves: the survivor's walk and run, or one of the dead's gaits.</summary>
    public enum HumanoidGait : byte { Survivor, Shambler, Lurcher, Runner, Crawler }

    /// <summary>A sustained task that takes over the arms (and often the legs) — rummaging, hammering,
    /// gardening, <see cref="Use"/>: working a fixture (a tap, a switch, a drawer: one hand reaching out
    /// toward it at chest height), or <see cref="Read"/>: a book or a magazine held open in both hands,
    /// the head bowed over it. New values go on the end.</summary>
    public enum HumanoidActivity : byte { None, Search, Build, Dig, Plant, Water, Use, Read }

    /// <summary>
    /// Procedural animation for a <see cref="HumanoidRig"/>. There are no clips: every frame the pose
    /// is computed from what the character is doing — a gait cycle driven by real ground speed (so feet
    /// don't slide), a crouch, the weapon in hand, one-shot actions such as a swing or a lunge, and
    /// whole-body states (sitting on a chair, lying in bed asleep or awake, crawling, dead). Hands that
    /// need to land somewhere exact — on a gun, on a bat handle, on the ground while searching, on a
    /// switch, in the lap — are placed with two-bone IK, and the hips are lowered every frame until a
    /// foot is planted on the floor (or set down on the seat).
    ///
    /// A driver (<see cref="PlayerAvatar"/>, <see cref="ZombieAvatar"/>) sets the inputs each frame and
    /// fires the triggers; this component evaluates in LateUpdate, after movement has been applied.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig))]
    public sealed partial class HumanoidAnimator : MonoBehaviour
    {
        // ---- Inputs, set by the driver every frame ----
        [System.NonSerialized] public Vector3 Velocity;          // world-space ground velocity
        [System.NonSerialized] public bool Sneaking;
        [System.NonSerialized] public HumanoidGait Gait;
        [System.NonSerialized] public HumanoidActivity Activity;
        [System.NonSerialized] public bool Aiming;               // firearm raised to the eye (else held low)
        [System.NonSerialized] public bool Reloading;
        [System.NonSerialized] public bool ArmsOut;              // the dead's reaching posture
        [System.NonSerialized] public bool Hunting;              // the dead lean in when they have prey
        [System.NonSerialized] public float Limp;                // 0..1, drags the left leg
        [System.NonSerialized] public bool HasLookTarget;
        [System.NonSerialized] public Vector3 LookTarget;        // world point the head turns toward
        [System.NonSerialized] public bool HasReachTarget;
        [System.NonSerialized] public Vector3 ReachTarget;       // world point a working hand reaches for (Use)
        [System.NonSerialized] public bool Seated;               // sitting: the hips rest on SeatPoint
        [System.NonSerialized] public Vector3 SeatPoint;         // world point on the seat's surface under the hips
        [System.NonSerialized] public float SeatRecline;         // 0 sat up straight .. 1 sunk back in an armchair
        [System.NonSerialized] public bool AutoUpdate = true;
        [System.NonSerialized] public bool ProbeGround = true;   // raycast for the floor under the feet
        [System.NonSerialized] public int GroundMask = 1;        // Default layer: floors and terrain

        /// <summary>True when the character is dead.</summary>
        public bool IsDead => _dead;
        /// <summary>True while lying in bed asleep.</summary>
        public bool IsSleeping => _lying && !_lyingAwake;
        /// <summary>True while lying in bed, asleep or awake.</summary>
        public bool IsLying => _lying;
        /// <summary>True once the body has (mostly) settled onto a seat.</summary>
        public bool IsSitting => _sitW > 0.5f;

        private const int BoneCount = HumanoidProportions.BoneCount;
        private const int Hips = 0, Spine = 1, Chest = 2, Neck = 3, HeadB = 4;
        private const int UArmL = 5, FArmL = 6, HandL = 7, UArmR = 8, FArmR = 9, HandR = 10;
        private const int ThighL = 11, ShinL = 12, FootL = 13, ThighR = 14, ShinR = 15, FootR = 16;

        private HumanoidRig _rig;
        private Transform _root;
        private Transform[] _b;
        private HumanoidProportions _bound;
        private readonly Quaternion[] _q = new Quaternion[BoneCount];
        private readonly Quaternion[] _q2 = new Quaternion[BoneCount];
        private Vector3 _hips, _hips2;   // offsets of the hips from their bind position, model space

        private float _time, _seed;
        private float _phase, _speed, _move;
        private Vector3 _moveLocal = Vector3.forward;
        private float _crouch, _aimW, _activityW, _armsOutW, _hunchW, _crawlW;
        private HumanoidActivity _shownActivity;
        private float _groundOffset;
        private bool _groundInit;
        private bool _groundDirty = true;

        // One-shot actions.
        private enum Act : byte { None, Swing, Punch, Shove, Lunge, Bash }
        private Act _act;
        private float _actT, _actDur, _actWindup;
        private bool _actTwoHanded;
        private float _recoil, _flinch, _flinchDur;
        private Vector3 _flinchDir;
        private float _stagger, _staggerDur;
        private Vector3 _staggerDir;

        // Whole-body states.
        private bool _dead, _fallBackward, _diedLying;
        private float _deathT;
        private bool _lying, _lyingAwake;
        private Transform _bed;
        private float _lieW, _awakeW, _sitW;
        private Vector3 _lieFromPos;
        private Quaternion _lieFromRot = Quaternion.identity;

        // The pose the root is being eased over from (see CarryFrom).
        private Vector3 _carryPos;
        private Quaternion _carryRot = Quaternion.identity;
        private float _carryW, _carryDur = 0.4f;

        private const float SitSeconds = 0.55f;   // settling onto a seat, or getting up off it

        private Vector2 _look;

        private void Awake()
        {
            _seed = Random.value * 100f;
            Bind();
        }

        private bool Bind()
        {
            if (_rig == null) _rig = GetComponent<HumanoidRig>();
            if (_rig == null || _rig.Bones == null) return false;
            _root = _rig.transform;
            _b = _rig.Bones;
            _bound = _rig.Proportions;
            return true;
        }

        private void LateUpdate()
        {
            if (AutoUpdate) Evaluate(Time.deltaTime);
        }

        // ── Triggers ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>A melee swing with whatever is in hand (bare hands throw a punch instead).</summary>
        public void Swing(float duration, bool twoHanded)
        {
            if (_dead) return;
            bool bare = _rig != null && _rig.HeldStyle != HoldStyle.Melee;
            StartAct(bare ? Act.Punch : Act.Swing, bare ? 0.36f : Mathf.Clamp(duration, 0.3f, 0.9f), 0f);
            _actTwoHanded = twoHanded && !bare;
        }

        /// <summary>Pushes the character's shoulder into a shove (used by zombies).</summary>
        public void Shove() { if (!_dead) StartAct(Act.Shove, 0.38f, 0f); }

        /// <summary>The dead lunge for a bite: <paramref name="windup"/> seconds of reaching, then the snap.</summary>
        public void Lunge(float windup) { if (!_dead) StartAct(Act.Lunge, windup + 0.4f, windup); }

        /// <summary>The dead beat on a barricade: arms up through the windup, then down.</summary>
        public void Bash(float windup) { if (!_dead) StartAct(Act.Bash, windup + 0.35f, windup); }

        /// <summary>Triggers a recoil effect for a fired weapon.</summary>
        public void Fire() { _recoil = 1f; }

        /// <summary>Applies a brief flinch reaction away from the impact direction.</summary>
        public void Flinch(Vector3 fromWorld)
        {
            if (_dead) return;
            _flinch = 1f;
            _flinchDur = 0.35f;
            _flinchDir = fromWorld.sqrMagnitude > 1e-6f ? fromWorld.normalized : -_root.forward;
        }

        /// <summary>Applies a stagger push that briefly disrupts movement.</summary>
        public void Stagger(Vector3 pushWorld, float duration)
        {
            if (_dead) return;
            _stagger = _staggerDur = Mathf.Max(0.15f, duration + 0.2f);
            _staggerDir = pushWorld.sqrMagnitude > 1e-6f ? pushWorld.normalized : -_root.forward;
            if (_act == Act.Lunge || _act == Act.Bash) _act = Act.None;
        }

        /// <summary>Collapses the body: it falls away from <paramref name="hitDirection"/> (the
        /// direction the killing blow travelled), or backward if that is unknown.</summary>
        public void Die(Vector3 hitDirection)
        {
            if (_dead || !Bind()) return;
            _dead = true;
            _deathT = 0f;
            _act = Act.None;
            Vector3 fwd = Flat(_root.forward);
            _fallBackward = hitDirection.sqrMagnitude < 1e-6f || Vector3.Dot(Flat(hitDirection), fwd) <= 0.3f;
            if (_crawlW > 0.5f) _fallBackward = false;
            _diedLying = _lieW > 0.5f;   // dying in bed: stay in bed
        }

        /// <summary>Lies down in <paramref name="bed"/> (or on the spot if null) asleep, until <see cref="WakeUp"/>.</summary>
        public void Sleep(Transform bed) => Lie(bed, false);

        /// <summary>Lies on <paramref name="bed"/> (or on the spot if null) until <see cref="WakeUp"/>: asleep, or
        /// <paramref name="awake"/> — resting with the hands laced behind the head and a knee up. Already lying
        /// down, it only drops off or comes round.</summary>
        public void Lie(Transform bed, bool awake)
        {
            if (_dead || !Bind()) return;
            _lyingAwake = awake;
            if (_lying) return;
            _lying = true;
            _bed = bed;
            _lieFromPos = _root.localPosition;
            _lieFromRot = _root.localRotation;
        }

        /// <summary>Wakes the character up, ending the lying state.</summary>
        public void WakeUp()
        {
            _lying = false;
            _lyingAwake = false;
        }

        /// <summary>Eases the body over from its last world pose (<paramref name="worldPos"/> /
        /// <paramref name="worldRot"/>: where the rig stood) after the character was moved in one step — set
        /// down over a chair to sit, or back on its feet beside it — instead of popping across.</summary>
        public void CarryFrom(Vector3 worldPos, Quaternion worldRot, float seconds)
        {
            _carryPos = worldPos;
            _carryRot = worldRot;
            _carryDur = Mathf.Max(0.05f, seconds);
            _carryW = 1f;
        }

        /// <summary>Skips smoothing on the next frame (after a spawn or teleport).</summary>
        public void Snap() { _groundInit = false; _groundDirty = true; _speed = 0f; _carryW = 0f; }

        private void StartAct(Act act, float duration, float windup)
        {
            _act = act;
            _actT = 0f;
            _actDur = duration;
            _actWindup = windup;
        }

        // ── Evaluation ───────────────────────────────────────────────────────────────────────────────

        /// <summary>Poses the rig for this frame. Called from LateUpdate unless <see cref="AutoUpdate"/>
        /// is off (previews drive it by hand).</summary>
        public void Evaluate(float dt)
        {
            if (!Bind()) return;
            if (_bound != _rig.Proportions) _bound = _rig.Proportions;
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            _time += dt;
            Tick(dt);

            float fall = PlaceRoot(dt);

            for (int i = 0; i < BoneCount; i++) _q[i] = Quaternion.identity;
            _hips = Vector3.zero;
            PoseUpright();

            if (_crawlW > 0.001f) { PoseCrawl(); Blend(_crawlW); }
            float sit = Mathf.SmoothStep(0f, 1f, _sitW);
            if (sit > 0.001f) { PoseSitting(); Blend(sit); }
            if (_dead && !_diedLying) { PoseDead(); Blend(Mathf.SmoothStep(0f, 1f, _deathT / 0.85f)); }
            else if (_lieW > 0.001f) { PoseLying(); Blend(Mathf.SmoothStep(0f, 1f, _lieW)); }

            Write();

            float upright = 1f - Mathf.Max(_crawlW, Mathf.Max(fall, _dead ? 1f : _lieW));
            Ground(1f - Mathf.Max(Mathf.Max(_crawlW, sit), Mathf.Max(fall, _lieW)));   // a seat holds the hips up
            if (upright > 0.01f) Arms(upright);
            else if (_crawlW > 0.01f && !_dead) CrawlArms(_crawlW);
            if (!_dead && _lieW > 0.01f && _awakeW > 0.01f) RestArms(Mathf.SmoothStep(0f, 1f, _lieW) * Mathf.SmoothStep(0f, 1f, _awakeW));
            if (!_dead) Look(dt, 1f - _lieW);
        }

        private void Tick(float dt)
        {
            Transform frame = _root.parent != null ? _root.parent : _root;
            Vector3 v = Velocity;
            v.y = 0f;
            float raw = _dead || _lying || Seated ? 0f : v.magnitude;
            _groundDirty = _groundDirty || raw > 0.01f;
            _speed = Mathf.Lerp(_speed, raw, 1f - Mathf.Exp(-dt * 8f));
            if (raw > 0.05f)
            {
                Vector3 local = Quaternion.Inverse(YawOf(frame.rotation)) * (v / raw);
                _moveLocal = Vector3.Slerp(_moveLocal, local, 1f - Mathf.Exp(-dt * 10f));
            }
            _move = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.55f, _speed));

            bool backward = _moveLocal.z < -0.25f;
            _phase += dt * (_speed / CycleLength()) * Mathf.PI * 2f * (backward ? -1f : 1f);
            if (Mathf.Abs(_phase) > 10000f) _phase = Mathf.Repeat(_phase, Mathf.PI * 2f);

            if (_act != Act.None)
            {
                _actT += dt;
                if (_actT >= _actDur) _act = Act.None;
            }
            _recoil = Mathf.Max(0f, _recoil - dt / 0.16f);
            if (_flinch > 0f) _flinch = Mathf.Max(0f, _flinch - dt / _flinchDur);
            if (_stagger > 0f) _stagger = Mathf.Max(0f, _stagger - dt);
            if (_dead) _deathT += dt;

            float k = 1f - Mathf.Exp(-dt * 7f);
            _aimW = Mathf.Lerp(_aimW, Aiming ? 1f : 0f, 1f - Mathf.Exp(-dt * 12f));
            if (Activity != HumanoidActivity.None) _shownActivity = Activity;
            _activityW = Mathf.MoveTowards(_activityW, Activity != HumanoidActivity.None ? 1f : 0f, dt * 4f);
            if (_activityW <= 0f) _shownActivity = HumanoidActivity.None;

            bool zombie = Gait != HumanoidGait.Survivor;
            _armsOutW = Mathf.Lerp(_armsOutW, ArmsOut ? 1f : 0f, k);
            _hunchW = Mathf.Lerp(_hunchW, zombie ? (Hunting ? 1f : 0.75f) : 0f, k);
            _crawlW = Mathf.MoveTowards(_crawlW, Gait == HumanoidGait.Crawler ? 1f : 0f, dt * 3f);

            float crouch = _dead ? 0.6f : Sneaking ? 0.42f : 0f;   // the knees go first when the body drops
            switch (_shownActivity)
            {
                case HumanoidActivity.Search: crouch = Mathf.Max(crouch, 0.8f * _activityW); break;
                case HumanoidActivity.Dig:
                case HumanoidActivity.Plant: crouch = Mathf.Max(crouch, 0.95f * _activityW); break;
                case HumanoidActivity.Build: crouch = Mathf.Max(crouch, 0.18f * _activityW); break;
                case HumanoidActivity.Use:
                    // Bob down to a low drawer or the cupboard under a counter.
                    if (HasReachTarget)
                        crouch = Mathf.Max(crouch, Mathf.Clamp01((0.95f - (ReachTarget.y - frame.position.y)) / 0.6f) * 0.75f * _activityW);
                    break;
            }
            _crouch = Mathf.Lerp(_crouch, crouch, k);

            _lieW = Mathf.MoveTowards(_lieW, _lying || _diedLying ? 1f : 0f, dt / 0.9f);
            _awakeW = Mathf.MoveTowards(_awakeW, _lyingAwake ? 1f : 0f, dt / 0.6f);
            _sitW = Mathf.MoveTowards(_sitW, Seated && !_lying && !_dead ? 1f : 0f, dt / (_lying ? 0.25f : SitSeconds));
            if (_carryW > 0f) _carryW = Mathf.Max(0f, _carryW - dt / _carryDur);
        }

        private float CycleLength()
        {
            float len = Mathf.Clamp(0.95f + 0.33f * _speed, 1.0f, 3.0f);
            return Gait == HumanoidGait.Shambler || Gait == HumanoidGait.Crawler ? len * 0.8f : len;
        }

        // Lying flat, the root is lifted by the depth of the body below the bone plane: the back when
        // face up, the chest when face down. A crawler's body trails a metre behind its root.
        private const float BackLift = 0.12f;
        private const float FrontLift = 0.135f;
        public const float CrawlShift = -1.0f;

        /// <summary>Places the rig root under the character: on the floor while standing (or sitting), on the
        /// bed while lying down, and toppling over when dead; eased over from where it stood after a sudden move
        /// (see <see cref="CarryFrom"/>). Returns how far through a fall the body is.</summary>
        private float PlaceRoot(float dt)
        {
            Transform parent = _root.parent;
            // The dead keep the floor they fell on (and then sink through it); the seated keep the floor they sat
            // down from — the ray would only find the chair under them.
            if (!_dead && !Seated)
            {
                float target = 0f;
if (_groundDirty && !_dead && !Seated && parent != null) {
                    // The ray starts inside the character's own collider, which a ray never reports.
                    Vector3 origin = parent.position + Vector3.up * 0.9f;
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, GroundMask, QueryTriggerInteraction.Ignore))
                        target = Mathf.Clamp(hit.point.y - parent.position.y, -0.35f, 0.3f);
                    _groundDirty = false;
                }
                _groundOffset = _groundInit ? Mathf.Lerp(_groundOffset, target, 1f - Mathf.Exp(-dt * 14f)) : target;
                _groundInit = true;
            }

            Vector3 pos = new Vector3(0f, _groundOffset, 0f);
            Quaternion rot = Quaternion.identity;

            if (_crawlW > 0.001f)
            {
                // Prone: face down on the chest, the body laid out behind the shoulders.
                rot = Quaternion.Euler(90f * _crawlW, 0f, 0f);
                pos += new Vector3(0f, FrontLift * _crawlW, CrawlShift * _crawlW);
            }

            float fall = 0f;
            if (_dead && !_diedLying)
            {
                float t = Mathf.Clamp01((_deathT - 0.12f) / 0.7f);
                fall = t * t;   // a falling body accelerates
                float settle = _deathT > 0.82f ? Mathf.Sin(Mathf.Clamp01((_deathT - 0.82f) / 0.25f) * Mathf.PI) * 3f : 0f;
                float angle = (90f + settle) * fall * (_fallBackward ? -1f : 1f);
                rot = Quaternion.Euler(angle, 0f, 0f);
                pos.y += (_fallBackward ? BackLift : FrontLift) * fall;
                if (_crawlW > 0.5f)
                {
                    rot = Quaternion.Euler(90f, 0f, 0f);
                    pos = new Vector3(0f, _groundOffset + FrontLift, CrawlShift);
                }
            }
            else if (_lieW > 0.001f && parent != null)
            {
                float w = Mathf.SmoothStep(0f, 1f, _lieW);
                LiePose(parent, out Vector3 lpos, out Quaternion lrot);
                pos = Vector3.Lerp(_lying ? _lieFromPos : pos, lpos, w);
                rot = Quaternion.Slerp(_lying ? _lieFromRot : rot, lrot, w);
            }

            if (_carryW > 0.001f && parent != null)
            {
                float k = Mathf.SmoothStep(0f, 1f, _carryW);
                pos = Vector3.Lerp(pos, parent.InverseTransformPoint(_carryPos), k);
                rot = Quaternion.Slerp(rot, Quaternion.Inverse(parent.rotation) * _carryRot, k);
            }

            _root.localPosition = pos;
            _root.localRotation = rot;
            return fall;
        }

        /// <summary>Where the rig root goes to lie on its back in the bed, in the parent's space.</summary>
        private void LiePose(Transform parent, out Vector3 pos, out Quaternion rot)
        {
            // Lying face up with the head toward the pillow (the bed's -Z end), the back on the mattress.
            Vector3 worldPos;
            Quaternion worldRot;
            if (_bed != null)
            {
                worldRot = _bed.rotation * Quaternion.Euler(-90f, 0f, 0f);
                worldPos = _bed.position + _bed.rotation * new Vector3(0f, 0.51f + 0.115f, 0.97f);
            }
            else
            {
                worldRot = YawOf(parent.rotation) * Quaternion.Euler(-90f, 0f, 0f);
                worldPos = parent.position + Vector3.up * (_groundOffset + 0.115f);
            }
            pos = parent.InverseTransformPoint(worldPos);
            rot = Quaternion.Inverse(parent.rotation) * worldRot;
        }

        private void Blend(float w)
        {
            if (w <= 0f) return;
            for (int i = 0; i < BoneCount; i++) _q[i] = Quaternion.Slerp(_q[i], _q2[i], w);
            _hips = Vector3.Lerp(_hips, _hips2, w);
        }

        private void Write()
        {
            HumanoidProportions p = _bound;
            for (int i = 0; i < BoneCount; i++)
            {
                _b[i].localRotation = _q[i];
                _b[i].localPosition = p.LocalBind(i);
            }
            _b[Hips].localPosition = p.LocalBind(Hips) + _hips;
        }

        /// <summary>Lowers (or raises) the hips until the lowest heel or toe touches the floor.</summary>
        private void Ground(float weight)
        {
            if (weight <= 0.001f) return;
            float lowest = Mathf.Min(SoleHeight(_b[FootL]), SoleHeight(_b[FootR]));
            Vector3 hp = _b[Hips].localPosition;
            hp.y -= lowest * weight;
            _b[Hips].localPosition = hp;
        }

        private float SoleHeight(Transform foot)
        {
            const float ankle = HumanoidProportions.AnkleY;
            float heel = _root.InverseTransformPoint(foot.TransformPoint(new Vector3(0f, -ankle, -0.075f))).y;
            float toe = _root.InverseTransformPoint(foot.TransformPoint(new Vector3(0f, -ankle + 0.004f, 0.19f))).y;
            return Mathf.Min(heel, toe);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────────────────────

        private static Quaternion YawOf(Quaternion q)
        {
            Vector3 f = q * Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(f, Vector3.up) : Quaternion.identity;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero;
        }

        private static Quaternion E(float x, float y, float z) => Quaternion.Euler(x, y, z);

        private static float Bell(float t, float a, float b) =>
            t <= a || t >= b ? 0f : Mathf.Sin((t - a) / (b - a) * Mathf.PI);
    }
}
