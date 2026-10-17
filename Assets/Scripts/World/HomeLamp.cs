using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A lamp of the wave-survival starter home — the bedside lamp, the standard lamp, the kitchen-diner's sconce,
    /// the store room's bulkhead, the bathroom's vanity light or the porch lantern: a warm <see cref="RoomLight"/>
    /// together with an optional glowing shell drawn just over the lamp's daytime shade or globe, shown only while the
    /// lamp is lit. Lamps are worked by hand, never by the clock: a lamp is lit while it's switched on and the house
    /// has power (<see cref="PowerGrid.HousePowered"/>) — and, with a dusk sensor like the porch lantern's, only
    /// while it's dark out. A wall light hangs on one of the house's circuits (<see cref="LightCircuits"/>) and is
    /// worked by that circuit's wall switch; a free-standing lamp has a switch of its own, a <see cref="LampSwitch"/>
    /// fixture the survivor reaches for. Created with <see cref="Attach"/>; it lives on the light's own object, so it
    /// goes when the furniture or wall it hangs on is dismantled.
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeLamp : MonoBehaviour
    {
        /// <summary>Daylight (<see cref="TimeOfDay.LightLevel"/>, 1 at noon) below which a dusk sensor turns its lamp on.</summary>
        public const float SwitchOnBelow = 0.45f;

        private static readonly List<HomeLamp> s_All = new List<HomeLamp>();

        private RoomLight _light;
        private Renderer _glow;
        private Renderer _bulb;
        private string _circuit;
        private bool _duskSensor;
        private bool _dark;
        private bool _switchedOn;
        private float _next;

        /// <summary>True while the lamp is lit.</summary>
        public bool IsOn => _light != null && _light.On;

        /// <summary>The circuit the lamp hangs on, or null for a free-standing lamp with its own switch.</summary>
        public string Circuit => _circuit;

        /// <summary>True for a lamp that lights only after dusk (the porch lantern's sensor).</summary>
        public bool HasDuskSensor => _duskSensor;

        /// <summary>True when the lamp's switch (its own, or its circuit's) is on — lit or not.</summary>
        public bool SwitchedOn => _circuit != null ? LightCircuits.IsOn(_circuit) : _switchedOn;

        /// <summary>Hangs a lamp's light at <paramref name="bulb"/> (world) under <paramref name="parent"/>, with
        /// <paramref name="glow"/> (world-space geometry, may be null) as its lit shade. A lamp on a
        /// <paramref name="circuit"/> is worked by that circuit's wall switch; one without is free-standing and gets
        /// its own switch fixture, named <paramref name="displayName"/> (by default from the furniture it stands on).
        /// A <paramref name="duskSensor"/> keeps the lamp dark by day even when it's switched on.</summary>
        internal static HomeLamp Attach(Transform parent, Vector3 bulb, float range, float intensity, bool shadows, RaidMesh glow,
            string circuit = null, bool duskSensor = false, string displayName = null)
        {
            RoomLight light = RoomLight.Spawn(parent, bulb, RoomLight.DefaultColor, range, intensity, false, shadows);
            var lamp = light.gameObject.AddComponent<HomeLamp>();
            lamp._light = light;
            lamp._circuit = string.IsNullOrEmpty(circuit) ? null : circuit;
            lamp._duskSensor = duskSensor;
            if (glow != null && !glow.IsEmpty)
            {
                GameObject g = glow.Emit("Glow", light.transform, bulb, ShadowCastingMode.Off);
                g.transform.rotation = Quaternion.identity;   // the geometry is in world axes
                g.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                lamp._glow = g.GetComponent<Renderer>();
                lamp._glow.receiveShadows = false;
                // A wall light's glowing globe shows it's lit; its bare bulb would only poke through the shade.
                if (lamp._circuit != null) light.ShowBulb = false;
            }
            Transform b = light.transform.Find("Bulb");
            if (b != null) lamp._bulb = b.GetComponent<Renderer>();
            lamp._next = Time.time + Random.value * CloneZConfig.LampCheckInterval;   // spread the lamps' checks over the frames
            lamp.Refresh();
            if (lamp._circuit == null) LampSwitch.Add(lamp, parent, displayName ?? LampSwitch.NameFor(parent));
            return lamp;
        }

        /// <summary>Switches the lamp on or off: a free-standing lamp's own switch, or a wall light's whole circuit.</summary>
        public void SetSwitchedOn(bool on)
        {
            if (_circuit != null)
            {
                LightCircuits.Set(_circuit, on);
                return;
            }
            _switchedOn = on;
            Refresh();
        }

        /// <summary>Brings every lamp up to date at once (a switch flipped, the breaker thrown).</summary>
        public static void RefreshAll()
        {
            for (int i = s_All.Count - 1; i >= 0; i--)
            {
                HomeLamp l = s_All[i];
                if (l == null) { s_All.RemoveAt(i); continue; }
                l.Refresh();
            }
        }

        /// <summary>How many lamps hang on <paramref name="circuit"/>, and how many of them are lit right now.</summary>
        public static void CountOnCircuit(string circuit, out int lamps, out int lit)
        {
            lamps = lit = 0;
            for (int i = 0; i < s_All.Count; i++)
            {
                HomeLamp l = s_All[i];
                if (l == null || l._circuit != circuit) continue;
                lamps++;
                if (l.IsOn) lit++;
            }
        }

        private void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
        }

        private void OnDisable()
        {
            s_All.Remove(this);
        }

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + CloneZConfig.LampCheckInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (_duskSensor)
            {
                TimeOfDay tod = TimeOfDay.Instance;
                float level = tod != null ? tod.LightLevel : 0f;
                _dark = _dark ? level < SwitchOnBelow + CloneZConfig.LampDuskHysteresis : level < SwitchOnBelow;
            }
            bool lit = SwitchedOn && PowerGrid.HousePowered && (!_duskSensor || _dark);
            if (_light != null && _light.On != lit) _light.On = lit;
            if (_glow != null && _glow.enabled != lit) _glow.enabled = lit;
            // A dark lamp's bulb never shows (the camera's cutaway can leave one drawing when it lets go of it).
            if (!lit && _bulb != null && !_bulb.forceRenderingOff) _bulb.forceRenderingOff = true;
        }
    }
}
