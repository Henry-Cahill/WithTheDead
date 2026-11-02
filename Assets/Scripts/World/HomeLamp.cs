using System;
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
    /// has power (<see cref="PowerGrid.PoweredAt"/>: on the statewide map, its building's own breaker on too) — and, with a
    /// dusk sensor like the porch lantern's, only
    /// while it's dark out. A wall light hangs on one of the house's circuits (<see cref="LightCircuits"/>) and is
    /// worked by that circuit's wall switch; a free-standing lamp has a switch of its own, a <see cref="LampSwitch"/>
    /// fixture the survivor reaches for. Created with <see cref="Attach"/>; it lives on the light's own object, so it
    /// goes when the furniture or wall it hangs on is dismantled. The raid town's lamps are the same lamps, with a few
    /// things the home never needed: a ceiling fitting (<see cref="HangFromCeiling"/>), a failing tube's flicker and a
    /// bulb that has gone (<see cref="DeadBulb"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeLamp : MonoBehaviour
    {
        /// <summary>Daylight (<see cref="TimeOfDay.LightLevel"/>, 1 at noon) below which a dusk sensor turns its lamp on.</summary>
        public const float SwitchOnBelow = 0.45f;

        // A flickering lamp's glowing shade drops out with its light through the deeper dips and the outages.
        private const float FlickerGlowLevel = 0.45f;

        private static readonly List<HomeLamp> s_All = new List<HomeLamp>();

        private RoomLight _light;
        private Renderer _glow;
        private Renderer _bulb;
        private string _circuit;
        private bool _duskSensor;
        private bool _dark;
        private bool _switchedOn;
        private bool _deadBulb;
        private bool _overhead, _ceilingsShown, _isFlickering;
        private bool _lit;
        private float _next;

        /// <summary>True while the lamp is lit.</summary>
        public bool IsOn => _light != null && _light.On;

        /// <summary>The circuit the lamp hangs on, or null for a free-standing lamp with its own switch.</summary>
        public string Circuit => _circuit;

        /// <summary>True for a lamp that lights only after dusk (the porch lantern's sensor).</summary>
        public bool HasDuskSensor => _duskSensor;

        /// <summary>True when the lamp's switch (its own, or its circuit's) is on — lit or not.</summary>
        public bool SwitchedOn => _circuit != null ? LightCircuits.IsOn(_circuit) : _switchedOn;

        /// <summary>True when the lamp's bulb has gone: it stays dark whatever its switch says.</summary>
        public bool DeadBulb
        {
            get => _deadBulb;
            internal set
            {
                if (_deadBulb == value) return;
                _deadBulb = value;
                Refresh();
            }
        }

        /// <summary>True while the lamp flickers like a failing tube (its glowing shade dropping out with the deeper dips).</summary>
        public bool Flicker
        {
            get => _light != null && _light.Flicker;
internal set
                    {
                        if (_light == null) return;
                        _light.Flicker = value;
                        _isFlickering = value;
            _isFlickering = _light?.Flicker ?? false;
                        SyncGlow();
                    }
        }

        /// <summary>Hangs a lamp's light at <paramref name="bulb"/> (world) under <paramref name="parent"/>, with
        /// <paramref name="glow"/> (world-space geometry, may be null) as its lit shade. A lamp on a
        /// <paramref name="circuit"/> is worked by that circuit's wall switch; one without is free-standing and gets
        /// its own switch fixture, named <paramref name="displayName"/> (by default from the furniture it stands on).
        /// A <paramref name="duskSensor"/> keeps the lamp dark by day even when it's switched on. The light is
        /// <paramref name="color"/> (by default <see cref="RoomLight.DefaultColor"/>); <paramref name="furniture"/> is the
        /// piece a free-standing lamp stands on, whose colliders mustn't block the reach for its switch (by default
        /// <paramref name="parent"/>).</summary>
        internal static HomeLamp Attach(Transform parent, Vector3 bulb, float range, float intensity, bool shadows, RaidMesh glow,
            string circuit = null, bool duskSensor = false, string displayName = null, Color? color = null, Transform furniture = null)
        {
            RoomLight light = RoomLight.Spawn(parent, bulb, color ?? RoomLight.DefaultColor, range, intensity, false, shadows);
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
            lamp._next = Time.time + UnityEngine.Random.value * CloneZConfig.LampCheckInterval;   // spread the lamps' checks over the frames
            lamp.Refresh();
            if (lamp._circuit == null)
            {
                Transform piece = furniture != null ? furniture : parent;
                LampSwitch.Add(lamp, piece, displayName ?? LampSwitch.NameFor(piece));
            }
            return lamp;
        }

        /// <summary>Makes this a ceiling fitting's lamp: its glowing shade shows only in the views that show the ceilings
        /// (the shoulder and first-person cameras). The isometric camera looks down into roofless rooms, so there the lit
        /// lamp shows its bare bulb instead, hanging where the fitting is.</summary>
        internal void HangFromCeiling()
        {
            _overhead = true;
            _ceilingsShown = ViewState.CeilingsVisible;
            if (_light != null) _light.ShowBulb = !_ceilingsShown;
            SyncGlow();
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
        public static void CountOnCircuit(string circuit, out int lamps, out int lit) => CountOnCircuit(circuit, out lamps, out lit, out _, out _);

        /// <summary>How many lamps hang on <paramref name="circuit"/> and how many of them are lit right now — and of the
        /// dark ones, how many have a dead bulb and how many are only waiting for dusk (switched on, with power, but their
        /// sensor still sees daylight).</summary>
        public static void CountOnCircuit(string circuit, out int lamps, out int lit, out int dead, out int waiting)
        {
            lamps = lit = dead = waiting = 0;
            for (int i = 0; i < s_All.Count; i++)
            {
                HomeLamp l = s_All[i];
                if (l == null || l._circuit != circuit) continue;
                lamps++;
                if (l.IsOn) lit++;
                else if (l._deadBulb) dead++;
                else if (l._duskSensor && !l._dark && l.SwitchedOn && PowerGrid.PoweredAt(l)) waiting++;
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
            // Update view‑state dependent glow for ceiling fittings
            if (_overhead)
            {
                if (ViewState.CeilingsVisible != _ceilingsShown) SyncGlow();
            }
            // Update flicker‑dependent glow if flicker state changed; a flickering lamp's shade follows its light's
            // level every frame, dropping out through the dips.
            bool flickerNow = _light?.Flicker ?? false;
            if (flickerNow != _isFlickering || (flickerNow && _glow != null))
            {
                _isFlickering = flickerNow;
            SyncGlow();
            _isFlickering = _light?.Flicker ?? false;
            }
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
            bool lit = SwitchedOn && PowerGrid.PoweredAt(this) && (!_duskSensor || _dark) && !_deadBulb;
            _lit = lit;
            if (_light != null && _light.On != lit) _light.On = lit;
            SyncGlow();
            // A dark lamp's bulb never shows (the camera's cutaway can leave one drawing when it lets go of it).
            if (!lit && _bulb != null && !_bulb.forceRenderingOff) _bulb.forceRenderingOff = true;
        }

        // Shows the glowing shade while the lamp is lit — a ceiling fitting's only while the ceilings show, a flickering
        // lamp's not through its dips — and hands a ceiling fitting its bare bulb whenever the view leaves the ceilings out.
        private void SyncGlow()
        {
            if (_overhead)
            {
                bool ceilings = ViewState.CeilingsVisible;
                if (ceilings != _ceilingsShown)
                {
                    _ceilingsShown = ceilings;
                    if (_light != null) _light.ShowBulb = !ceilings;
                }
            }
            if (_glow == null) return;
            bool show = _lit && (!_overhead || _ceilingsShown) && (_light == null || !_light.Flicker || _light.Level > FlickerGlowLevel);
            if (_glow.enabled != show) _glow.enabled = show;
        }
    }
}
