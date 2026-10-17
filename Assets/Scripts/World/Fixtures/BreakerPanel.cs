using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The house's breaker panel, in the store room: a grey steel box whose tin door opens on the dead-front of
    /// breakers. With the door open the survivor can throw the main breaker (<see cref="PowerGrid.MainBreakerOn"/>):
    /// every light and appliance in the house goes dead at once — handy for silencing a set left blaring — and comes
    /// back when it's thrown again, as long as the grid is still up. "Check the power" reads the meter. The main
    /// breaker's handle shows which way it's thrown. The door is saved here; the breaker is saved with the grid.
    /// </summary>
    [DisallowMultipleComponent]
    public class BreakerPanel : Interactable
    {
        private string _key;
        private Vector3 _point, _face, _normal;
        private FixturePart _door, _handle;
        private bool _open;
        private float _nextSync;

        public override string DisplayName => "Breaker panel";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);
        public override string SaveKey => _key;

        /// <summary>True while the house's main breaker is on (it is, before there's a grid to throw it on).</summary>
        private static bool BreakerOn => PowerGrid.Instance == null || PowerGrid.Instance.MainBreakerOn;

        /// <summary>A panel under <paramref name="host"/> (the wall piece). <paramref name="point"/> is where the survivor
        /// reaches, <paramref name="face"/> the box's spot on the wall face and <paramref name="normal"/> the way it faces.</summary>
        internal static BreakerPanel Create(Transform host, Vector3 point, Vector3 face, Vector3 normal)
        {
            var go = new GameObject("BreakerPanel");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var p = go.AddComponent<BreakerPanel>();
            p._point = point;
            p._face = face;
            p._normal = normal;
            p._key = KeyFor("breakerpanel", point);
            return p;
        }

        /// <summary>Hands over the door (hinged to swing open) and the main breaker's handle (hinged to flip up when on).</summary>
        internal void SetParts(FixturePart door, FixturePart handle)
        {
            _door = door;
            _handle = handle;
            if (_door != null) _door.SetOpen(_open, true);
            SyncHandle(true);
        }

        private void Update()
        {
            if (Time.time < _nextSync) return;
            _nextSync = Time.time + CloneZConfig.SwitchSyncInterval;
            SyncHandle(false);
        }

        private void SyncHandle(bool instant)
        {
            if (_handle == null) return;
            bool on = BreakerOn;
            if (instant || _handle.IsOpen != on) _handle.SetOpen(on, instant);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool on = BreakerOn;
            var breaker = new InteractAction(on ? "Switch off the main breaker" : "Switch on the main breaker", c => Throw(c, !on))
                .DisabledIf(!_open, "The panel's door is shut")
                .DisabledIf(_open && PowerGrid.Instance == null, "There's no supply to switch");
            into.Add(new InteractAction(_open ? "Close the breaker panel" : "Open the breaker panel", c => SetOpen(!_open, false)));
            into.Add(breaker);
            into.Add(new InteractAction("Check the power", CheckPower));
        }

        private void SetOpen(bool open, bool instant)
        {
            _open = open;
            if (_door != null) _door.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.BreakerDoorNoise);
        }

        private void Throw(InteractContext ctx, bool on)
        {
            PowerGrid grid = PowerGrid.Instance;
            if (grid == null) return;
            grid.MainBreakerOn = on;
            SyncHandle(false);
            HomeLamp.RefreshAll();
            MakeNoise(CloneZConfig.BreakerNoise);
            if (!on) ctx.Notify("Clunk. The house goes quiet \u2014 every light and appliance is dead.");
            else if (grid.IsOn) ctx.Notify("Clunk. Somewhere in the house, something hums back to life.");
            else ctx.Notify("Clunk. Nothing \u2014 the grid is down anyway.");
        }

        private static void CheckPower(InteractContext ctx)
        {
            bool grid = PowerGrid.GridOn, breaker = BreakerOn;
            if (grid && breaker) ctx.Notify("The meter is spinning \u2014 the grid is live.");
            else if (grid) ctx.Notify("The main breaker is off. The grid is still live \u2014 throw it back on for power.");
            else if (breaker) ctx.Notify("The meter has stopped. The grid is down.");
            else ctx.Notify("The main breaker is off \u2014 and the meter has stopped anyway. The grid is down.");
        }

        public override void CaptureState(FixtureSave save) => save.State = _open ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            SetOpen((save.State & 1) != 0, true);
            SyncHandle(true);
        }
    }
}
