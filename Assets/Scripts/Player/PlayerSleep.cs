using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Lets the player sleep at a bed to pass the night, shedding fatigue and slowly
    /// healing (Project Zomboid's sleeping regen). Sleeping fast-forwards the
    /// day/night clock and is only possible when no zombies are close. The survivor
    /// wakes when rested, at dawn, if attacked, if the horde closes in, or on input.
    /// </summary>
    public class PlayerSleep : MonoBehaviour
    {
        private PlayerStats _stats;
        private PlayerWounds _wounds;
        private PlayerPanic _panic;

        public bool IsSleeping { get; private set; }
        public bool NearBed { get; private set; }
        public string Prompt { get; private set; } = "";

        private bool _wakeRequested;
        private float _sleepElapsed;
        private readonly Collider[] _dangerBuf = new Collider[24];

        private PlayerWounds Wounds => _wounds != null ? _wounds : (_wounds = GetComponent<PlayerWounds>());
        private PlayerPanic Panic => _panic != null ? _panic : (_panic = GetComponent<PlayerPanic>());

        private void Awake() => _stats = GetComponent<PlayerStats>();

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

            SleepSpot bed = SleepSpot.Nearest(transform.position, CloneZConfig.SleepRange, out _);
            NearBed = bed != null;

            if (NearBed && !UIState.GameplayBlocked)
            {
                bool danger = ZombiesNearby();
                bool bleeding = Wounds != null && Wounds.IsBleeding;
                bool panicked = Panic != null && Panic.TooPanickedToSleep;
                Prompt = bleeding ? "Bandage your wounds before resting"
                       : panicked ? "Too panicked to sleep"
                       : danger ? "Too dangerous to sleep \u2014 zombies near"
                       : "[T]  Sleep until rested";
                if (!danger && !bleeding && !panicked && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                    StartSleep();
            }
            else Prompt = "";
        }

        private void StartSleep()
        {
            IsSleeping = true;
            _wakeRequested = false;
            _sleepElapsed = 0f;
            UIState.GameplayBlocked = true;   // freezes movement & combat while asleep
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.TimeScale = CloneZConfig.SleepTimeScale;
        }

        private void StopSleep()
        {
            IsSleeping = false;
            UIState.GameplayBlocked = false;
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.TimeScale = 1f;
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
            var t = TimeOfDay.Instance;
            return t != null && t.Hour >= CloneZConfig.SleepWakeHour && t.Hour < 11f && !t.IsNight;
        }

        private bool ZombiesNearby()
        {
            int n = Physics.OverlapSphereNonAlloc(
                transform.position, CloneZConfig.SleepDangerRadius, _dangerBuf,
                1 << CloneZConfig.IgnoreRaycastLayer, QueryTriggerInteraction.Ignore);
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
