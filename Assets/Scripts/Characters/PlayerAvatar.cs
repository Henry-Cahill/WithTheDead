using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's body. Builds the survivor's <see cref="HumanoidRig"/> on the player and drives its
    /// <see cref="HumanoidAnimator"/> from the gameplay components: walking, sneaking and sprinting
    /// from the real ground speed; swings, shoves, shots and reloads from <see cref="PlayerCombat"/>;
    /// the clothes from <see cref="PlayerClothing"/>; the weapon in hand from the inventory (or the
    /// hammer, trowel or watering can while working, or empty hands while working a fixture or
    /// reading); sitting on a chair, the edge of the bed or the toilet and lying on the bed to rest
    /// from <see cref="PlayerSeat"/>; sleep in the nearest bed; the head turned to the fixture being
    /// worked (a hand reaching for it); and the fall on death. It reads gameplay state but never
    /// changes it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const string HammerId = "hammer";
        private const string TrowelId = "trowel";
        private const string WateringCanId = "wateringcan";
        private const float AimHoldSeconds = 2.5f;   // how long the gun stays up after a shot
        private const float SeatCarrySeconds = 0.45f; // the body eases onto a seat (or off it) this long
        private const float SeatCarryMaxSqr = 2.5f * 2.5f;   // farther than this, a move is a teleport, not a step

        public HumanoidRig Rig { get; private set; }
        public HumanoidAnimator Animator { get; private set; }
        /// <summary>A seat that isn't a fixture (a car's driver's seat): the figure sits on it while set.</summary>
        public Transform ExternalSeat;

        private PlayerController _controller;
        private PlayerCombat _combat;
        private PlayerInventory _inventory;
        private PlayerClothing _clothing;
        private PlayerSleep _sleep;
        private PlayerStats _stats;
        private PlayerInteractor _interactor;
        private PlayerBuilder _builder;
        private PlayerFarming _farming;
        private PlayerSeat _seat;
        private Actor _localActor;

        private HumanoidLook _look;
        // Cached item definitions for tools to avoid per-frame lookups
        private ItemDef _hammerDef;
        private ItemDef _trowelDef;
        private ItemDef _wateringCanDef;
        private readonly ItemDef[] _shownWorn = new ItemDef[PlayerClothing.Slots.Length];
        private bool _lookDirty = true;
        private bool _started;
        private bool _subscribed;
        private Vector3 _lastPos;
        private bool _hasLastPos;
        private float _aimHold;
        private int _seatVersion;
        private Vector3 _rigPos;
        private Quaternion _rigRot = Quaternion.identity;
        private bool _hasRigPose;

        private void Awake()
        {
            _look = HumanoidLook.Survivor();
            Rig = HumanoidRig.Create(transform, _look);
            Animator = Rig.gameObject.AddComponent<HumanoidAnimator>();
            Animator.AutoUpdate = false;   // evaluated below, after the inputs are set
            _localActor = Actor.LocalPlayer;
        }


        // The gameplay components are added after this one is created, so they're looked up here.
        private void Start()
        {
            _controller = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _inventory = GetComponent<PlayerInventory>();
            _clothing = GetComponent<PlayerClothing>();
            _sleep = GetComponent<PlayerSleep>();
            _stats = GetComponent<PlayerStats>();
            _interactor = GetComponent<PlayerInteractor>();
            _builder = GetComponent<PlayerBuilder>();
            _farming = GetComponent<PlayerFarming>();
            _seat = GetComponent<PlayerSeat>();
            _seatVersion = _seat != null ? _seat.Version : 0;
                        _hammerDef = ItemDatabase.Get(HammerId);
            _trowelDef = ItemDatabase.Get(TrowelId);
            _wateringCanDef = ItemDatabase.Get(WateringCanId);
            _started = true;
            Subscribe();
            _localActor = Actor.LocalPlayer;

            RefreshOutfit();
            RefreshHeld(HumanoidActivity.None);
            _lastPos = transform.position;
            _hasLastPos = true;
            Animator.Snap();
            if (_stats != null && _stats.IsDead) Animator.Die(Vector3.zero);
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            if (_combat != null)
            {
                _combat.OnSwing += HandleSwing;
                _combat.OnShove += HandleShove;
                _combat.OnFire += HandleFire;
                _combat.OnDryFire += HandleDryFire;
                _combat.OnReload += HandleReload;
            }
            if (_stats != null)
            {
                _stats.OnDamaged += HandleDamaged;
                _stats.OnDied += HandleDied;
            }
            if (_clothing != null) _clothing.Changed += MarkLookDirty;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_combat != null)
            {
                _combat.OnSwing -= HandleSwing;
                _combat.OnShove -= HandleShove;
                _combat.OnFire -= HandleFire;
                _combat.OnDryFire -= HandleDryFire;
                _combat.OnReload -= HandleReload;
            }
            if (_stats != null)
            {
                _stats.OnDamaged -= HandleDamaged;
                _stats.OnDied -= HandleDied;
            }
            if (_clothing != null) _clothing.Changed -= MarkLookDirty;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Rig == null) return;   // paused: hold the pose

            if (_lookDirty) RefreshOutfit();
            HumanoidActivity activity = CurrentActivity();
            RefreshHeld(activity);

            // Ground speed from how far the body actually moved, so blocked walking doesn't stride.
            Vector3 pos = transform.position;
            Vector3 velocity = _hasLastPos ? (pos - _lastPos) / dt : Vector3.zero;
            if (velocity.sqrMagnitude > 400f) velocity = Vector3.zero;   // a teleport, not a stride
            _lastPos = pos;
            _hasLastPos = true;

            HumanoidAnimator a = Animator;

            // Sitting down or getting up sets the body down over the seat (or beside it) in one step: the figure
            // is eased across instead, and the step is no stride.
            bool driving = ExternalSeat != null;
            bool seated = driving || (_seat != null && _seat.IsSeated);
            if (_seat != null && _seat.Version != _seatVersion)
            {
                _seatVersion = _seat.Version;
                if (_hasRigPose && (_rigPos - pos).sqrMagnitude < SeatCarryMaxSqr) a.CarryFrom(_rigPos, _rigRot, SeatCarrySeconds);
                velocity = Vector3.zero;
            }
            if (seated) velocity = Vector3.zero;

            _aimHold = Mathf.Max(0f, _aimHold - dt);
            a.Velocity = velocity;
            a.Gait = HumanoidGait.Survivor;
            a.Sneaking = _controller != null && _controller.IsSneaking;
            a.Activity = activity;
            a.Reloading = _combat != null && _combat.IsReloading;
            a.Aiming = _aimHold > 0f || a.Reloading;
            a.Seated = seated;
            if (driving)
            {
                a.SeatPoint = ExternalSeat.position;
                a.SeatRecline = 0.2f;
            }
            else if (seated)
            {
                a.SeatPoint = _seat.SeatPoint;
                a.SeatRecline = Mathf.InverseLerp(0.35f, 0.9f, _seat.Comfort);
            }

            // Asleep in the nearest bed, or lying on the bed awake to rest.
            bool asleep = _sleep != null && _sleep.IsSleeping;
            bool resting = !asleep && _seat != null && _seat.IsLyingDown;
            if (asleep || resting)
            {
                Transform bed = resting && _seat.RestBed != null ? _seat.RestBed : NearestBed();
                a.Lie(bed, resting);
            }
            else if (a.IsLying) a.WakeUp();

            SetAttention(a, activity);
            a.Evaluate(dt);

            _rigPos = Rig.transform.position;
            _rigRot = Rig.transform.rotation;
            _hasRigPose = true;
        }

        // The head turns to the fixture being worked (or the one whose menu is open), and a hand working it reaches
        // for it; a reader looks down at the book in their hands.
        private void SetAttention(HumanoidAnimator a, HumanoidActivity activity)
        {
            Interactable f = null;
            if (_interactor != null)
            {
                if (_interactor.IsWorking) f = _interactor.JobTarget;
                else if (_interactor.MenuOpen) f = _interactor.MenuTarget;
            }

            if (activity == HumanoidActivity.Read)
            {
                Transform chest = Rig.Bone(HumanBone.Chest);
                a.HasLookTarget = true;
                a.LookTarget = chest.position + transform.forward * 0.32f - Vector3.up * 0.24f;
            }
            else if (f != null)
            {
                a.HasLookTarget = true;
                a.LookTarget = f.InteractPoint;
            }
            else a.HasLookTarget = false;

            a.HasReachTarget = f != null && activity == HumanoidActivity.Use;
            if (a.HasReachTarget) a.ReachTarget = f.InteractPoint;

            // Hands up (or cuffed): both hands over the head.
            if (_localActor == null) _localActor = Actor.LocalPlayer;
            Actor self = _localActor;
            if (self != null && self.gameObject == gameObject && (self.Surrendered || self.Restrained))
            {
                a.Activity = HumanoidActivity.Use;
                a.HasReachTarget = true;
                a.ReachTarget = transform.position + Vector3.up * 2.2f;
            }
        }

        private HumanoidActivity CurrentActivity()
        {
            if (_stats != null && _stats.IsDead) return HumanoidActivity.None;
            if (_interactor != null && _interactor.IsSearching) return HumanoidActivity.Search;
            if (_interactor != null && _interactor.IsWorking) return _interactor.JobActivity;
            if (_builder != null && _builder.IsBuilding) return HumanoidActivity.Build;
            if (_farming != null && _farming.IsBusy)
            {
                string label = _farming.WorkLabel ?? string.Empty;
                if (label.StartsWith("Water")) return HumanoidActivity.Water;
                if (label.StartsWith("Plant")) return HumanoidActivity.Plant;
                return HumanoidActivity.Dig;   // tilling, harvesting and clearing are trowel work
            }
            return HumanoidActivity.None;
        }

        /// <summary>The tool for the job in hand, otherwise the equipped weapon.</summary>
        private void RefreshHeld(HumanoidActivity activity)
        {
            ItemDef def = null;
            HoldStyle style = HoldStyle.None;
            switch (activity)
            {
                            case HumanoidActivity.Build: def = _hammerDef; style = HoldStyle.Melee; break;
                case HumanoidActivity.Dig: def = _trowelDef; style = HoldStyle.Melee; break;
                case HumanoidActivity.Water: def = _wateringCanDef; style = HoldStyle.Carry; break;
                case HumanoidActivity.Plant: break;   // seeds go in by hand
                case HumanoidActivity.Use:
                case HumanoidActivity.Read: break;    // the weapon's put by to work a fixture or hold a book
                default:
                    def = _inventory != null ? _inventory.EquippedWeapon : null;
                    if (def != null) style = def.IsFirearm ? HoldStyle.Firearm : def.IsWeapon ? HoldStyle.Melee : HoldStyle.None;
                    break;
            }
            Rig.SetHeldItem(def, style);
        }

        private void MarkLookDirty() => _lookDirty = true;

        private bool OutfitChanged()
        {
            if (_clothing == null) return false;
            ClothingSlot[] slots = PlayerClothing.Slots;
            for (int i = 0; i < slots.Length; i++)
                if (_clothing.GetWorn(slots[i]) != _shownWorn[(int)slots[i]]) return true;
            return false;
        }

        private void RefreshOutfit()
        {
            _lookDirty = false;
            ClothingSlot[] slots = PlayerClothing.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                ItemDef worn = _clothing != null ? _clothing.GetWorn(slots[i]) : null;
                _shownWorn[(int)slots[i]] = worn;
                _look.Wear(slots[i], worn);
            }
            Rig.ApplyLook(_look);
        }

        private Transform NearestBed()
        {
            SleepSpot bed = SleepSpot.Nearest(transform.position, CloneZConfig.SleepRange + 0.5f, out _);
            return bed != null ? bed.transform : null;
        }

        private void HandleSwing()
        {
            ItemDef weapon = _inventory != null ? _inventory.EquippedWeapon : null;
            float cooldown = _combat != null ? _combat.SwingCooldown : 0.55f;
            Animator.Swing(Mathf.Clamp(cooldown * 0.8f, 0.35f, 0.7f), weapon != null && weapon.TwoHanded);
        }

        private void HandleShove() => Animator.Shove();

        private void HandleFire()
        {
            Animator.Fire();
            _aimHold = AimHoldSeconds;
        }

        private void HandleDryFire() => _aimHold = AimHoldSeconds;

        private void HandleReload() => _aimHold = Mathf.Max(_aimHold, 0.5f);

        private void HandleDamaged() => Animator.Flinch(-transform.forward);

        private void HandleDied() => Animator.Die(Vector3.zero);
    }
}
