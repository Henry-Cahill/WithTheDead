using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A wall light switch by a door: a cream plate of one or more toggles, each working one of the house's lighting
    /// circuits (<see cref="LightCircuits"/>) — [E] flips the first, Q lists them all. A toggle visibly flips up (on)
    /// and down (off) and clicks (a faint noise). With the power out it still flips, but nothing lights until the
    /// power's back. Usable only from the room it's in: the wall stops the reach from the far side. Saves its
    /// circuits' states by its position.
    /// </summary>
    [DisallowMultipleComponent]
    public class LightSwitch : Interactable
    {
        private string _name;
        private string _key;
        private Vector3 _point, _face, _normal;
        private string[] _circuits;
        private FixturePart[] _toggles;
        private float _nextSync;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);
        public override string SaveKey => _key;

        /// <summary>A switch under <paramref name="host"/> (the wall piece) working <paramref name="circuits"/>, in the order
        /// its actions are listed. <paramref name="point"/> is where the survivor reaches (a few cm proud of the plate),
        /// <paramref name="face"/> the plate's spot on the wall face and <paramref name="normal"/> the way it faces.</summary>
        internal static LightSwitch Create(Transform host, string displayName, Vector3 point, Vector3 face, Vector3 normal, string[] circuits)
        {
            var go = new GameObject("LightSwitch");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var s = go.AddComponent<LightSwitch>();
            s._name = displayName;
            s._point = point;
            s._face = face;
            s._normal = normal;
            s._circuits = circuits ?? new string[0];
            s._key = KeyFor("lightswitch", point);
            return s;
        }

        /// <summary>Hands over the toggles (one per circuit, in the same order), hinged to flip up when on.</summary>
        internal void SetToggles(FixturePart[] toggles)
        {
            _toggles = toggles;
            Sync(true);
        }

        private void Update()
        {
            if (Time.time < _nextSync) return;
            _nextSync = Time.time + CloneZConfig.SwitchSyncInterval;
            Sync(false);
        }

        // Keeps each toggle up or down with its circuit (a circuit can change from elsewhere, e.g. a loaded save).
        private void Sync(bool instant)
        {
            if (_toggles == null) return;
            for (int i = 0; i < _toggles.Length && i < _circuits.Length; i++)
            {
                FixturePart t = _toggles[i];
                if (t == null) continue;
                bool on = LightCircuits.IsOn(_circuits[i]);
                if (instant || t.IsOpen != on) t.SetOpen(on, instant);
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            for (int i = 0; i < _circuits.Length; i++)
            {
                string circuit = _circuits[i];
                bool on = LightCircuits.IsOn(circuit);
                into.Add(new InteractAction((on ? "Switch off the " : "Switch on the ") + LightCircuits.Noun(circuit), c => Flip(c, circuit, !on)));
            }
        }

        private void Flip(InteractContext ctx, string circuit, bool on)
        {
            LightCircuits.Set(circuit, on);
            Sync(false);
            MakeNoise(CloneZConfig.LightSwitchNoise);
            if (!on) return;
            if (!PowerGrid.HousePowered)
            {
                ctx.Notify(LightingFixtures.NothingHappens());
                return;
            }
            HomeLamp.CountOnCircuit(circuit, out int lamps, out int lit);
            if (lamps == 0) ctx.Notify("Nothing happens. Whatever it was wired to is gone.");
            else if (lit == 0 && circuit == LightCircuits.Porch) ctx.Notify("The porch lantern stays dark for now \u2014 its sensor waits for dusk.");
        }

        public override void CaptureState(FixtureSave save)
        {
            int bits = 0;
            for (int i = 0; i < _circuits.Length && i < 30; i++)
                if (LightCircuits.IsOn(_circuits[i])) bits |= 1 << i;
            save.State = bits;
        }

        public override void RestoreState(FixtureSave save)
        {
            for (int i = 0; i < _circuits.Length && i < 30; i++)
                LightCircuits.Set(_circuits[i], (save.State & (1 << i)) != 0);
            Sync(true);
        }
    }
}
