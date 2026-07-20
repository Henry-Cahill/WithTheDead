using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The municipal water supply. For the first few days the taps run freely; then — as in
    /// Project Zomboid's scheduled water shut-off — the mains run dry for good, forcing the
    /// survivor onto collected rain water (which must be boiled) or scavenged bottled water.
    ///
    /// A lightweight singleton like <see cref="TimeOfDay"/> / <see cref="WeatherSystem"/>,
    /// created by the bootstrapper on the Environment object.
    /// </summary>
    public class WaterMains : MonoBehaviour
    {
        public static WaterMains Instance { get; private set; }

        /// <summary>True while the taps still run.</summary>
        public bool IsOn { get; private set; } = true;

        /// <summary>The day the mains are scheduled to run dry (rolled at startup).</summary>
        public int ShutoffDay { get; private set; }

        /// <summary>Raised once, the moment the water is shut off.</summary>
        public event Action ShutOff;

        /// <summary>Convenience: are the mains currently running? (true before the system exists).</summary>
        public static bool MainsOn => Instance == null || Instance.IsOn;

        public string StatusText => IsOn ? "ON" : "SHUT OFF";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            ShutoffDay = UnityEngine.Random.Range(CloneZConfig.WaterShutoffDayMin, CloneZConfig.WaterShutoffDayMax + 1);
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

        /// <summary>Restores the mains state (on/off + scheduled shut-off day) from a saved game.</summary>
        public void Restore(bool isOn, int shutoffDay)
        {
            IsOn = isOn;
            ShutoffDay = Mathf.Max(1, shutoffDay);
        }
    }
}
