using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Lets the player sleep at a bed to pass the night, shedding fatigue and slowly
    /// healing (Project Zomboid's sleeping regen). Sleeping fast-forwards the
    /// day/night clock and is only possible when no zombies are close. The survivor
    /// wakes when rested, at dawn, if attacked, if the horde closes in, or on input.
    /// T sleeps at the nearest bed; the bed's own fixture asks through <see cref="TrySleep"/>,
    /// which applies the same checks. Falling asleep gets the survivor up off any seat.
    /// </summary>
    public class PlayerSleep : MonoBehaviour
    {
        private PlayerStats _stats;
        private static TimeOfDay _timeOfDay;
        private PlayerWounds _wounds;
        private PlayerPanic _panic;
        private PlayerSeat _seat;

        /// <summary>True while the survivor is currently sleeping.</summary>
        public bool IsSleeping { get; private set; }
        /// <summary>True when the survivor is close enough to a bed to consider sleeping.</summary>
        public bool NearBed { get; private set; }
        /// <summary>The UI prompt shown to the player about sleeping state or actions.</summary>
        public string Prompt { get; private set; } = "";

        private bool _wakeRequested;
        private float _sleepElapsed;
        private readonly Collider[] _dangerBuf = new Collider[24];
        // Cached nearest bed lookup to avoid per‑frame searches
        private SleepSpot _cachedBed;
        private Vector3 _lastPos;
        private const float BedCacheThresholdSqr = 1f * 1f;

        private PlayerWounds Wounds => _wounds != null ? _wounds : (_wounds = GetComponent<PlayerWounds>());
        private PlayerPanic Panic => _panic != null ? _panic : (_panic = GetComponent<PlayerPanic>());
        private PlayerSeat Seat => _seat != null ? _seat : (_seat = GetComponent<PlayerSeat>());

        private const string BleedingReason = "Bandage your wounds before resting";
        private const string PanickedReason = "Too panicked to sleep";
        private const string DangerReason = "Too dangerous to sleep \u2014 zombies near";

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _timeOfDay = TimeOfDay.Instance;
        _lastPos = transform.position;
    }


        private void OnEnable()
        {
            if (_stats == null) _stats = GetComponent<PlayerStats>();
            if (_stats != null) _stats.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDamaged -= OnDamaged;
            if (IsSleeping) StopSleep();
        }

        private void OnDamaged() => _wakeRequested = true;   // a bite in the night jolts you awake

        private void Update()
        {
            if (_stats != null && _stats.IsDead)
            {
                if (IsSleeping) StopSleep();
                Prompt = "";
                NearBed = false;
                return;
            }

            if (IsSleeping) { TickSleep(); return; }

// Cached nearest bed lookup: only recompute when the player moved more than the threshold or when no cached bed exists.
if ((_lastPos - transform.position).sqrMagnitude > BedCacheThresholdSqr || _cachedBed == null)
{
    _cachedBed = SleepSpot.Nearest(transform.position, CloneZConfig.SleepRange, out _);
    _lastPos = transform.position;
}
NearBed = _cachedBed != null;

            if (NearBed && !UIState.GameplayBlocked)
            {
                string blocked = SleepBlocker();
                Prompt = blocked ?? "[T]  Sleep until rested";
                if (blocked == null && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                    StartSleep();
            }
            else Prompt = "";
        }

        /// <summary>True when the survivor could go to sleep right now: a bed close by, no wound bleeding, nerves
        /// steady enough and no zombies near (the T key's checks). <paramref name="reason"/> says why not.</summary>
        public bool CanSleep(out string reason)
        {
            reason = null;
            if (_stats != null && _stats.IsDead) { reason = "You can't sleep now"; return false; }
            if (IsSleeping) { reason = "You're already asleep"; return false; }
            if (SleepSpot.Nearest(transform.position, CloneZConfig.SleepRange, out _) == null) { reason = "You need a bed to sleep in"; return false; }
            reason = SleepBlocker();
            return reason == null;
        }

        /// <summary>Goes to sleep in the nearest bed if <see cref="CanSleep"/> allows; otherwise returns false with the reason.</summary>
        public bool TrySleep(out string reason)
        {
            if (!CanSleep(out reason)) return false;
            StartSleep();
            return true;
        }

        // Why sleep is out of the question at the bed right now, or null when it isn't.
        private string SleepBlocker()
        {
            if (Wounds != null && Wounds.IsBleeding) return BleedingReason;
            if (Panic != null && Panic.TooPanickedToSleep) return PanickedReason;
            if (ZombiesNearby()) return DangerReason;
            return null;
        }

        private void StartSleep()
        {
            if (Seat != null)
            {
                Seat.Stand();
                Seat.GetUp();
            }
            IsSleeping = true;
            _wakeRequested = false;
            _sleepElapsed = 0f;
            UIState.GameplayBlocked = true;   // freezes movement & combat while asleep
            if (_timeOfDay != null) _timeOfDay.TimeScale = CloneZConfig.SleepTimeScale;
        }

        private void StopSleep()
        {
            IsSleeping = false;
            UIState.GameplayBlocked = false;
            if (_timeOfDay != null) _timeOfDay.TimeScale = 1f;
            Prompt = "";
        }

        private void TickSleep()
        {
            float dt = Time.deltaTime;
            _sleepElapsed += dt;
            if (_stats != null) _stats.Sleep(dt);
            Prompt = "";

            // A short settling-in window stops the same T-press (or an instant dawn)
            // from waking us on the very frame we lie down.
            if (_sleepElapsed < 0.4f) return;

            bool rested = _stats != null && _stats.Fatigue <= 0.02f;
            bool dawn = ReachedDawn();
            bool danger = ZombiesNearby();
            bool input = WakeInputPressed();

            if (_wakeRequested || rested || dawn || danger || input)
                StopSleep();
        }

        private static bool ReachedDawn()
        {
            var t = _timeOfDay;
            return t != null && t.Hour >= CloneZConfig.SleepWakeHour && t.Hour < 11f && !t.IsNight;
        }

        private bool ZombiesNearby()
        {
            int n = Physics.OverlapSphereNonAlloc(
                transform.position, CloneZConfig.SleepDangerRadius, _dangerBuf,
                1 << CloneZConfig.IgnoreRaycastLayer, QueryTriggerInteraction.Ignore);   // zombies live on Ignore Raycast
            for (int i = 0; i < n; i++)
                if (_dangerBuf[i] != null && _dangerBuf[i].GetComponentInParent<ZombieAI>() != null)
                    return true;
            return false;
        }

        private static bool WakeInputPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            return kb.tKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame ||
                   kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
                   kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                   kb.spaceKey.wasPressedThisFrame;
        }
    }
}
