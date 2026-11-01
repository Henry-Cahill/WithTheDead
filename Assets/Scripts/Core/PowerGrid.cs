using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The electricity supply. Like <see cref="WaterMains"/> (and Project Zomboid's scheduled power cut),
    /// the grid runs for the first few days and then fails for good: the starter home's lamps, light
    /// switches, fridge and TV go dead, while battery things (the radio, the wall clock, a flashlight)
    /// and the gas range keep working. The survivor can also cut the house off themselves at the
    /// breaker panel (<see cref="MainBreakerOn"/>), e.g. to silence a TV left blaring.
    ///
    /// A lightweight singleton created by the bootstrapper on the Environment object.
    /// </summary>
    public class PowerGrid : MonoBehaviour
    {
        public static PowerGrid Instance { get; private set; }

        /// <summary>True while the grid still supplies power.</summary>
        public bool IsOn { get; private set; } = true;

        /// <summary>The day the grid is scheduled to fail (rolled at startup).</summary>
        public int ShutoffDay { get; private set; }

        /// <summary>The house's main breaker (the survivor can throw it at the breaker panel).</summary>
        public bool MainBreakerOn { get; set; } = true;

        /// <summary>Raised once, the moment the grid fails.</summary>
        public event Action ShutOff;

        /// <summary>Convenience: is the grid supplying power? (true before the system exists).</summary>
        public static bool GridOn => Instance == null || Instance.IsOn;

        /// <summary>Is there power at the house's sockets right now: the grid is up and the main breaker on.</summary>
        public static bool HousePowered => Instance == null || (Instance.IsOn && Instance.MainBreakerOn);

        /// <summary>Is there power at the sockets where <paramref name="at"/> is: <see cref="HousePowered"/>, and on the statewide
        /// map the main breaker of the building it stands in on too (<see cref="BuildingPower"/>).</summary>
        public static bool PoweredAt(Component at) => HousePowered && BuildingPower.OnAt(at);

        /// <summary>Why something electric at <paramref name="at"/> won't work right now, or null when it has power.</summary>
        public static string NoPowerReasonAt(Component at) => NoPowerReason ?? (BuildingPower.OnAt(at) ? null : "The main breaker is off");

        /// <summary>Why something electric won't work right now, or null when the house has power.</summary>
        public static string NoPowerReason
        {
            get
            {
                if (Instance == null) return null;
                if (!Instance.IsOn) return "The power's out";
                if (!Instance.MainBreakerOn) return "The main breaker is off";
                return null;
            }
        }

        public string StatusText => IsOn ? "ON" : "OFF";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            ShutoffDay = UnityEngine.Random.Range(CloneZConfig.PowerShutoffDayMin, CloneZConfig.PowerShutoffDayMax + 1);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsOn) return;
            if (TimeOfDay.Instance != null && TimeOfDay.Instance.Day >= ShutoffDay)
            {
                IsOn = false;
                ShutOff?.Invoke();
            }
        }

        /// <summary>Restores the grid from a saved game. A save from before the grid existed passes
        /// <paramref name="shutoffDay"/> 0: the failure is then scheduled a few days after the saved day,
        /// so loading an old run doesn't plunge the house into darkness at once.</summary>
        public void Restore(bool isOn, int shutoffDay, bool breakerOn)
        {
            if (shutoffDay <= 0)
            {
                int today = TimeOfDay.Instance != null ? TimeOfDay.Instance.Day : 1;
                shutoffDay = today + UnityEngine.Random.Range(CloneZConfig.PowerShutoffLateDelayMin, CloneZConfig.PowerShutoffLateDelayMax + 1);
                isOn = true;
            }
            IsOn = isOn;
            ShutoffDay = Mathf.Max(1, shutoffDay);
            MainBreakerOn = breakerOn;
        }
    }
}
