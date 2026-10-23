using System.Collections.Generic;
using System;
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
        private int _notesRead;
            private bool _openedToWater;   // opened just for a watering, to be closed again after
            private float _nextCheck;
            private float _autoCloseAt; // game‑time at which the tap will auto‑close (0 = no timer)
            private const int NotesShift = 1; // bits start after the open flag
        private const int NotesMask = 0x1E; // four bits (max 15 notes)
        private const float AutoCloseDelay = 300f; // 5 minutes in seconds

        // Flavor: collection of period‑correct notes that can be read from the tap.
        private static readonly string[] _tapNotes = new[]
        {
            "The outside tap is dry — the water's been shut off.", // style reference
            "A thin layer of frost clings to the wheel; winter's grip is deep here.",
            "Old Mrs. Caldwell left a note: \"Don’t forget the chickens need water at dawn.\"",
            "The paint on the hose reel is peeling; it’s seen more rain than anyone remembers.",
            "Someone scratched: \"If the mains stay dead, dig a well before the frost kills the crops.\"",
            "A child's doodle of a snowman leans against the tap, a reminder of carefree days.",
            "The rusted handle squeaks like a warning bell when turned in the cold.",
            "A faded newspaper clipping is taped here: “Winter 1993 – Water rationing announced. Conserve what you can.”",
            "Grandpa Earl’s scrawl reads, “If the pipe freezes, tap it slowly to avoid bursts – we lost the back‑yard well once.”",
            "A small hand‑drawn map shows the old well behind the barn, marked “Dry – dig deeper when spring thaws.”",
            "An aged photograph of the house in summer hangs crookedly, captioned “Before the freeze, the garden burst with beans.”"
        };

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
            s._notesRead = 0;
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
            // Auto‑close handling
            if (_autoCloseAt > 0f && Time.time >= _autoCloseAt)
            {
                SetOpen(false, false);
                _autoCloseAt = 0f;
                // Give the survivor a cue that the tap shut itself
                MakeNoise(CloneZConfig.OutsideTapNoise);
            }
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
            into.Add(new InteractAction("Check water flow", c => { if (_open && WaterMains.MainsOn) c.Notify("Water is rushing out at normal pressure."); else if (_open) c.Notify("The tap is open but the mains are shut off – no flow."); else c.Notify("The tap is closed, so no water is flowing."); }));

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

            // Action to leave the tap open for a limited time, then auto‑close.
            var autoClose = new InteractAction("Leave tap open for 5 min", c =>
            {
                // Open the tap if it isn’t already.
                if (!_open) SetOpen(true, false);
                // Set the auto‑close timer.
                _autoCloseAt = Time.time + AutoCloseDelay;
                c.Notify("You leave the tap open; it will shut itself in five minutes.");
            })
            .DisabledIf(!_open || _openedToWater, "The tap is closed – turn it on first, or finish watering first.");
            into.Add(autoClose);

            var coil = new InteractAction("Coil the hose", c => c.Notify("You wind the hose back onto its reel, neat as anything. Habit."))
                .Timed(CloneZConfig.HoseCoilSeconds, "Coiling the hose");
            coil.Stopped = c => {
                if (_open) {
                    SetOpen(false, false);
                    // Cancel any pending auto‑close timer so it won’t fire on a closed tap.
                    _autoCloseAt = 0f;
                    c.Notify("You remember to shut the tap.");
                }
            };
            var readNote = new InteractAction("Read tap note", c =>
            {
                int idx = _notesRead % _tapNotes.Length;
                c.Notify(_tapNotes[idx]);
                _notesRead++;
                if (_notesRead >= _tapNotes.Length)
                    c.Notify("You've read all the notes; the old house feels a little less lonely.");
            });
            into.Add(readNote);
        }


private void Turn(InteractContext ctx, bool open)
        {
            _openedToWater = false;
            SetOpen(open, false);
            if (!open) _autoCloseAt = 0f; // cancel any pending auto‑close // Clear auto‑close timer when manually turning off
            if (open && !WaterMains.MainsOn) ctx.Notify("The wheel squeaks round, but nothing comes out — the water's been shut off.");
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
            _autoCloseAt = 0f; // ensure no stray timer remains
        }

        public override void CaptureState(FixtureSave save)
        {
            // bit 0 = open flag, bits 1‑4 = notes‑read count
            int openBit = _open && !_openedToWater ? 1 : 0;
            int notesBits = (_notesRead << NotesShift) & NotesMask;
            save.State = openBit | notesBits;
        }

        public override void RestoreState(FixtureSave save)
        {
            _openedToWater = false;
            _notesRead = (save.State & NotesMask) >> NotesShift;
            SetOpen((save.State & 1) != 0, true);
        }
    }
}
