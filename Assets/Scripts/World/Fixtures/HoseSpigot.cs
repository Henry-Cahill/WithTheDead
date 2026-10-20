using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The outside tap: a standpipe with a red wheel beside the hose reel, round the west side of the house by the
    /// rain barrel. Turning the wheel sets water trickling from the hose's nozzle out on the lawn — clean mains water
    /// for as long as the mains run (it stops by itself when they're shut off, the wheel left open). The hose waters
    /// the vegetable plots within reach, and it's a water point (<see cref="IWaterPoint"/>): drink from it or fill a
    /// bottle here, from the menu or with G. Usable from the yard only. Saves whether the tap is left open.
    /// </summary>
    [DisallowMultipleComponent]
    public class HoseSpigot : Interactable, IWaterPoint
    {
        private string _key;
        private Vector3 _point, _spout, _face, _normal;
        private FixturePart _handle;
        private Renderer _trickle;
        private bool _open;
        private bool _openedToWater;   // opened just for a watering, to be closed again after
        private float _nextCheck;

        public override string DisplayName => "Outside tap";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);
        public override string SaveKey => _key;

        /// <summary>True while water is actually running out of the hose.</summary>
        public bool Running => _open && WaterMains.MainsOn;

        #region IWaterPoint
        public Vector3 WaterPosition => _spout;
        public string WaterLabel => "hose";
        public bool HasWater => WaterMains.MainsOn;
        public bool IsTainted => false;
        public string DryMessage => "The outside tap is dry \u2014 the water's been shut off";
        public void DrawWater() { }   // mains water: limitless while it runs
        #endregion

        /// <summary>The tap's fixture under <paramref name="host"/> (the yard). <paramref name="point"/> is the wheel,
        /// <paramref name="spout"/> where the water comes from; the survivor must stand in front of the plane through
        /// <paramref name="face"/> facing <paramref name="normal"/> (outside the house wall).</summary>
        internal static HoseSpigot Create(Transform host, Vector3 point, Vector3 spout, Vector3 face, Vector3 normal)
        {
            var go = new GameObject("OutsideTap");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var s = go.AddComponent<HoseSpigot>();
            s._point = point;
            s._spout = spout;
            s._face = face;
            s._normal = normal;
            s._key = KeyFor("outsidetap", point);
            return s;
        }

        /// <summary>Hands over the wheel (hinged to turn open) and the trickle out of the nozzle (shown while running).</summary>
        internal void SetParts(FixturePart handle, Renderer trickle)
        {
            _handle = handle;
            _trickle = trickle;
            if (_handle != null) _handle.SetOpen(_open, true);
            RefreshTrickle();
        }

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + CloneZConfig.SwitchSyncInterval;
            RefreshTrickle();
        }

        private void RefreshTrickle()
        {
            bool run = Running;
            if (_trickle != null && _trickle.enabled != run) _trickle.enabled = run;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool mains = WaterMains.MainsOn;
            into.Add(new InteractAction(_open ? "Turn off the outside tap" : "Turn on the outside tap", c => Turn(c, !_open)));
            into.Add(new InteractAction("Check tap", c => { if (_open) { c.Notify(WaterMains.MainsOn ? "The tap is open and water is running." : "The tap is open but the mains are shut off."); } else { c.Notify("The tap is closed."); } }));

            int plots = PlotsInReach(out _);
            var water = new InteractAction("Water the garden", WaterGarden)
                .Timed(CloneZConfig.GardenWaterSeconds, "Watering the garden", HumanoidActivity.Water)
                .DisabledIf(!mains, DryMessage)
                .DisabledIf(mains && plots == 0, "Nothing's planted within reach of the hose");
            water.Begin = c =>
            {
                if (_open) return;
                _openedToWater = true;
                SetOpen(true, false);
            };
            water.Stopped = c => EndWatering();
            into.Add(water);

            WaterUse.AddDrinkAndFill(ctx, this, into);
            into.Add(new InteractAction("Coil the hose", c => c.Notify("You wind the hose back onto its reel, neat as anything. Habit."))
                .Timed(CloneZConfig.HoseCoilSeconds, "Coiling the hose"));
        }

        private void Turn(InteractContext ctx, bool open)
        {
            _openedToWater = false;
            SetOpen(open, false);
            if (open && !WaterMains.MainsOn) ctx.Notify("The wheel squeaks round, but nothing comes out \u2014 the water's been shut off.");
        }

        private void SetOpen(bool open, bool instant)
        {
            _open = open;
            if (_handle != null) _handle.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.OutsideTapNoise);
            RefreshTrickle();
        }

        // How many garden plots the hose reaches, and how many of those are thirsty.
        private int PlotsInReach(out int thirsty)
        {
            thirsty = 0;
            int n = 0;
            float r2 = CloneZConfig.GardenWaterRange * CloneZConfig.GardenWaterRange;
            List<FarmPlot> all = FarmPlot.All;
            for (int i = 0; i < all.Count; i++)
            {
                FarmPlot p = all[i];
                if (p == null) continue;
                Vector3 d = p.transform.position - _point;
                d.y = 0f;
                if (d.sqrMagnitude > r2) continue;
                n++;
                if (p.NeedsWater) thirsty++;
            }
            return n;
        }

        private void WaterGarden(InteractContext ctx)
        {
            int plots = PlotsInReach(out int thirsty);
            float r2 = CloneZConfig.GardenWaterRange * CloneZConfig.GardenWaterRange;
            List<FarmPlot> all = FarmPlot.All;
            for (int i = 0; i < all.Count; i++)
            {
                FarmPlot p = all[i];
                if (p == null) continue;
                Vector3 d = p.transform.position - _point;
                d.y = 0f;
                if (d.sqrMagnitude <= r2) p.AddWater();
            }
            EndWatering();
            if (plots == 0) ctx.Notify("There's nothing within reach of the hose to water.");
            else if (thirsty > 0) ctx.Notify("You drag the hose over and give the vegetable bed a good long soak.");
            else ctx.Notify("The soil was damp already. You give it a sprinkle anyway.");
        }

        // Closes the tap again if it was only opened for the watering.
        private void EndWatering()
        {
            if (!_openedToWater) return;
            _openedToWater = false;
            SetOpen(false, false);
        }

        public override void CaptureState(FixtureSave save) => save.State = _open && !_openedToWater ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            _openedToWater = false;
            SetOpen((save.State & 1) != 0, true);
        }
    }
}
