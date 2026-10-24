using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The bathroom's toilet. The seat and lid lift on their shared hinge at the back (the seat can't come up under a
    /// closed lid, so lifting it brings the lid too, and closing the lid brings the seat down with it). "Use the toilet"
    /// wants the lid up, sits the survivor down for a quiet minute and leaves the bowl wanting a flush; the lever empties
    /// the cistern with a roar the dead can hear, and the cistern fills again over twenty seconds or so while the mains
    /// run — after the shut-off, the last cisternful is all there is. The heavy china lid lifts off the cistern (it's stood up
    /// against the wall), and then the water in it can be drunk or bottled: a couple of drinks' worth, tainted, an old
    /// survivor's trick (it's an <see cref="IWaterPoint"/>, so G works here too). The lid is also somewhere to sit.
    /// [E] follows the everyday round — lift the lid, use it, flush it, close the lid — and Q lists the rest. Saves the
    /// seat, lid and cistern lid, whether it wants flushing and how full the cistern is.
    /// </summary>
    [DisallowMultipleComponent]
    public class ToiletFixture : Interactable, IWaterPoint
    {
        private enum Step { Lid, Use, Flush }

        // Where the water point goes while the cistern's lid is on: far out of anyone's reach.
        private static readonly Vector3 OutOfReach = new Vector3(100000f, 0f, 100000f);

        private static readonly string[] UseLines =
        {
            // New lines – frost‑bitten solitude, remnants of home, warning of danger
            "You take a minute to yourself. Small mercies.",
            "Business seen to. Some things don't change, even now.",
            "A quiet minute on the throne. For a moment you almost forget what's outside.",
            "Cold night fatigue drags you down, even as the seat offers brief relief.",
            "The distant crack of gunfire flickers through the bathroom walls.",
            "A grim reminder of lost kin lingers in the stale, damp air.",
            "The stale air tastes of rusted metal and forgotten promises.",
            "A fleeting hope of warmth rises with the steam of the cistern.",
            "Even the water seems to laugh, tasting of rust and regret.",
            "A distant howl drifts in, reminding you of unseen dangers.",
            "Steam rises from the cistern, a fleeting veil against the cold.",
            "The porcelain glints faintly, a brief glimpse of civilization.",
            "You hear the faint echo of your own breath, a solitary companion.",
                "Your breath freezes in the air, a fleeting cloud of frost.",
                "A mournful wind sighs through the boarded walls, echoing distant sorrow.",
                "A cracked frame catches a ghost of a family photograph, a memory flickering.",
                "A loose pipe rattles, its metallic clang cutting the cold silence.",
                "Steam swirls, and a ghostly silhouette briefly appears within its veil.",
                "A cold draft whistles through the tiles, reminding you of the world beyond.",
                "The flickering light above casts uneasy shadows on the porcelain.",
                "You feel a lingering scent of rust and old soap.",
                "A distant siren wails, echoing through the cracked window.",
                "The water drips slowly, a metronome of solitude.",
                "The frosted window panes echo the world’s cold breath.",
            "Your breath hangs like smoke, a brief banner of life in the icy air.",
            "A faded family portrait leans against the wall, its faces ghost‑white with snow.",
            "The chipped ceramic bears a crack shaped like a lightning strike, a reminder of the storm outside.",
            "A rusted pipe shudders, sending a thin drip that taps a morse of dread.",
            "The dim bulb flickers once, then steadies, as if warning of an unseen presence.",
            "A thin layer of snow tracks across the floor, leading to the cracked door.",
            "The distant howl of wolves drifts through the walls, a reminder that the night is far from over.",
            "A spider's web glistens in the dim light, unnoticed."
        };

        // Graffiti scribbles found on the bathroom wall – style reference: "You hear the faint echo of your own breath, a solitary companion."
        private static readonly string[] GraffitiLines = {
            "The world outside is a cold whisper, but the walls remember summer.",
            "Someone wrote: “Stay quiet, the dead are listening.”",
            "A child's hand traced a heart around a cracked mirror – love in the apocalypse.",
            "“Don’t trust the lights,” the graffiti warns, smudged by water.",
            "A faded slogan: “Survive the night, the morning will be yours.”"
        };


        private HomeFurniture _host;
        private string _key;
        private Vector3 _point, _sitPoint, _water;
        private FixturePart _seat, _lid, _cisternLid, _lever;
        private Transform _sheet;
        private Renderer _sheetRenderer;
        private Vector3 _sheetFull;
        private bool _seatUp, _lidUp, _cisternOff, _wantsFlush, _flushed, _satToUse;
        private bool _seatClean = true; // true = seat is clean, false = dirty after use
        private MaterialPropertyBlock _seatBlock;
        private float _level = 1f;           // the cistern: 0 empty .. 1 full
        private float _leverBackAt = -1f;

        public override string DisplayName => "Toilet";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _host != null ? _host.transform : transform;
        public override string SaveKey => _key;

        private static float DrinkShare => 1f / Mathf.Max(1f, CloneZConfig.ToiletCisternDrinks);
        private bool CanFlush => _level >= CloneZConfig.ToiletFlushMinLevel;
        private float Facing => _host != null ? _host.Yaw : 0f;

        // Where the hips rest: on the closed lid, a little lower on the seat, lower still on the bare rim.
        private Vector3 SitPoint => _sitPoint + Vector3.down * (!_lidUp ? 0f : _seatUp ? 0.045f : 0.02f);

        private string NoFlushReason => WaterMains.MainsOn ? "The cistern's still filling"
            : _level > 0.02f ? "There isn't enough water left in the cistern to flush"
            : "The cistern's empty — the water's off (you can turn it back on at the toilet)";

        #region IWaterPoint
        // While the cistern's lid is on there's no getting at the water, so the point lies far out of reach and G passes
        // the toilet over (for the basin, say).
        /// <summary>World-space position of the cistern's water point (or far away when the lid is closed).</summary>
        public Vector3 WaterPosition => _cisternOff ? _water : OutOfReach;
        /// <summary>Label used for UI when displaying water source information.</summary>
        public string WaterLabel => "cistern";
        /// <summary>Indicates whether the cistern currently holds enough water to drink.</summary>
        public bool HasWater => _cisternOff && _level >= DrinkShare * 0.5f;
        /// <summary>Indicates the cistern water is always considered tainted (unfiltered).</summary>
        public bool IsTainted => true;
        /// <summary>Message shown when the cistern lid is on or water is unavailable.</summary>
        public string DryMessage => !_cisternOff ? "The cistern's lid is on"
            : WaterMains.MainsOn ? "The cistern's empty \u2014 it's filling again"
            : "The cistern's dry, and with the water off it won't fill again";

        /// <summary>Reduces the cistern water level by one drink share and updates the visual level.</summary>
        public void DrawWater()
        {
            _level = Mathf.Max(0f, _level - DrinkShare);
            ShowLevel();
        }
        #endregion

        /// <summary>The toilet's fixture on <paramref name="host"/> (the toilet itself): its points come from the toilet's
        /// drawing (<see cref="FurnitureBuilder.ToiletBody"/>) and its key from where it stands.</summary>
        /// <summary>Creates a new toilet fixture instance attached to the given home furniture.</summary>
        internal static ToiletFixture Create(HomeFurniture host)
        {
            Vector3 point = BathroomFixtures.Point(host, FurnitureBuilder.ToiletReach);
            var go = new GameObject("ToiletFixture");
            go.transform.SetParent(host.transform, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var t = go.AddComponent<ToiletFixture>();
            t._host = host;
            t._point = point;
            t._sitPoint = BathroomFixtures.Point(host, FurnitureBuilder.ToiletSitPoint);
            t._water = BathroomFixtures.Point(host, FurnitureBuilder.ToiletCisternWater);
            t._key = KeyFor("toilet", host.transform.position);
            return t;
        }

        /// <summary>Hands over the moving parts (seat, lid, cistern lid and flush lever, each hinged to open) and the
        /// cistern's water sheet (drawn full; it sinks with the level).</summary>
        /// <summary>Sets up the moving parts (seat, lid, cistern lid, lever, sheet) for the toilet.</summary>
        internal void SetParts(FixturePart seat, FixturePart lid, FixturePart cisternLid, FixturePart lever, Renderer sheet)
        {
            _seat = seat;
            _lid = lid;
            _cisternLid = cisternLid;
            _lever = lever;
            _sheetRenderer = sheet;
            _sheet = sheet != null ? sheet.transform : null;
            if (_sheet != null) _sheetFull = _sheet.position;
            ShowParts(true);
            ShowLevel();
        }

        private void Update()
        {
            // The cistern fills again while the mains run.
            if (_level < 1f && WaterMains.MainsOn)
            {
                _level = Mathf.Min(1f, _level + Time.deltaTime / CloneZConfig.ToiletCisternRefillSeconds);
                ShowLevel();
            }
            if (_leverBackAt >= 0f && Time.time >= _leverBackAt)
            {
                _leverBackAt = -1f;
                if (_lever != null) _lever.SetOpen(false);
            }
        }

        /// <summary>Updates the visual parts of the toilet (seat, lid, cistern lid) instantly.</summary>
        private void ShowParts(bool instant)
        {
            if (_seat != null) _seat.SetOpen(_seatUp, instant);
            if (_seat != null && _seat.Renderer != null)
                FixtureColour.Set(_seat.Renderer, ref _seatBlock, _seatClean ? Color.white : new Color(0.8f, 0.8f, 0.8f));
            if (_lid != null) _lid.SetOpen(_lidUp, instant);
            if (_cisternLid != null) _cisternLid.SetOpen(_cisternOff, instant);
        }

        /// <summary>Updates the visual water sheet to reflect the current cistern level.</summary>
        private void ShowLevel()
        {
            if (_sheet == null) return;
            bool any = _level > 0.01f;
            if (_sheetRenderer.enabled != any) _sheetRenderer.enabled = any;
            _sheet.position = _sheetFull + Vector3.down * ((1f - _level) * FurnitureBuilder.ToiletCisternDepth);
        }

        /// <summary>Generates the list of actions (lift lid, use, flush, sit, etc.) for the player.</summary>
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // [E] follows the everyday round: lift the lid, use it, flush it, close the lid.
            Step first = !_lidUp ? Step.Lid : _wantsFlush ? Step.Flush : _flushed ? Step.Lid : Step.Use;
            AddStep(into, first);
            if (first != Step.Lid) AddStep(into, Step.Lid);
            // Add graffiti inspection interaction before seat actions
            into.Add(new InteractAction("Read graffiti on the wall", InspectGraffiti));
            into.Add(new InteractAction(_seatUp ? "Put the seat down" : "Lift the seat", c => SetSeat(!_seatUp)));
            into.Add(new InteractAction("Wipe the seat", WipeSeat));
            into.Add(new InteractAction("Scrub the seat", ScrubSeat));
            if (first != Step.Use) AddStep(into, Step.Use);
            if (first != Step.Flush) AddStep(into, Step.Flush);
            PlayerSeat seat = ctx.Seat;
            if (seat != null && seat.IsSeated && seat.CurrentSeat == this) into.Add(new InteractAction("Stand up", c => { if (c.Seat != null) c.Seat.Stand(); }));
            else into.Add(new InteractAction("Sit down", SitDown));
            into.Add(new InteractAction(_cisternOff ? "Put the cistern lid back" : "Lift the cistern lid", c => SetCisternLid(c, !_cisternOff)));
            if (!_cisternOff) return;
            WaterUse.AddDrinkAndFill(ctx, this, into);
            into.Add(new InteractAction("Look into the cistern", LookIn));
            // Allow the survivor to toggle the house water mains from the toilet.
            if (WaterMains.Instance != null)
            {
                if (WaterMains.MainsOn)
                    into.Add(new InteractAction("Turn off water mains", c => SetWaterMains(c, false)));
                else
                    into.Add(new InteractAction("Turn on water mains", c => SetWaterMains(c, true)));
            }
        }

        /// <summary>Adds a single interaction step (lid, use, or flush) to the action list.</summary>
        private void AddStep(List<InteractAction> into, Step step)
        {
            switch (step)
            {
                case Step.Lid:
                    into.Add(new InteractAction(_lidUp ? "Close the lid" : "Lift the lid", c => SetLid(!_lidUp)));
                    break;
                case Step.Use:
                    var use = new InteractAction("Use the toilet", Used)
                        .Timed(CloneZConfig.ToiletUseSeconds, "Using the toilet", HumanoidActivity.Use)
                        .DisabledIf(!_lidUp, "Lift the lid first");
                    use.Begin = SitToUse;
                    use.Stopped = StandFromUse;
                    into.Add(use);
                    break;
                default:
                    into.Add(new InteractAction("Flush the toilet", Flush).DisabledIf(!CanFlush, NoFlushReason));
                    break;
            }
        }

        /// <summary>Sets the lid state and updates related parts (seat, flush state).</summary>
        private void SetLid(bool up)
        {
            _lidUp = up;
            if (!up)
            {
                _seatUp = false;   // the lid comes down on the seat, and brings it down too
                _flushed = false;
            }
            ShowParts(false);
            MakeNoise(up ? CloneZConfig.ToiletLiftNoise : CloneZConfig.ToiletLidNoise);
        }

        /// <summary>Sets the seat state and ensures the lid is up if the seat is raised.</summary>
        private void SetSeat(bool up)
        {
            _seatUp = up;
            if (up) _lidUp = true;   // the seat can't come up under a closed lid: they lift together
            ShowParts(false);
            MakeNoise(up ? CloneZConfig.ToiletLiftNoise : CloneZConfig.ToiletLidNoise);
        }

        /// <summary>Handles the player trying to sit on the toilet, with appropriate checks.</summary>
        private void SitDown(InteractContext ctx)
        {
            if (ctx.Seat == null)
            {
                ctx.Notify("You can't sit down right now");
                return;
            }
            if (!ctx.Seat.TrySit(this, SitPoint, Facing, CloneZConfig.ToiletSitComfort, out string reason)) ctx.Notify(reason);
        }

        // Sits down for the job (unless already sitting here), and gets up again after.
        /// <summary>Prepares the survivor to sit for using the toilet, handling initial sit if needed.</summary>
        private void SitToUse(InteractContext ctx)
        {
            _satToUse = false;
            PlayerSeat seat = ctx.Seat;
            if (seat == null || (seat.IsSeated && seat.CurrentSeat == this)) return;
            _satToUse = seat.TrySit(this, SitPoint, Facing, CloneZConfig.ToiletSitComfort, out _);
        }

        /// <summary>Makes the survivor stand up after using the toilet if they were seated.</summary>
        private void StandFromUse(InteractContext ctx)
        {
            if (!_satToUse) return;
            _satToUse = false;
            PlayerSeat seat = ctx.Seat;
            if (seat != null && seat.IsSeated && seat.CurrentSeat == this) seat.Stand();
        }

        /// <summary>Handles the core "use" action: sits, sets flush flag, and notifies a random line.</summary>
        private void Used(InteractContext ctx)
        {
            StandFromUse(ctx);
            _wantsFlush = true;
            _seatClean = false; // using the toilet makes the seat dirty
            ShowParts(false); // update seat colour to show it’s dirty
            _flushed = false;
            BathroomFixtures.Nerves(ctx, -CloneZConfig.ToiletUseCalm);
            string line = UseLines[Random.Range(0, UseLines.Length)];
            if (!CanFlush && !WaterMains.MainsOn) line += " Not that there's the water left to flush it.";
            ctx.Notify(line);
            LightingFixtures.Calm(ctx, CloneZConfig.ToiletUseCalm);
        }

        /// <summary>Flushes the cistern, plays sound, and notifies the player of the result.</summary>
        private void Flush(InteractContext ctx)
        {
            bool mains = WaterMains.MainsOn;
            _level = 0f;
            ShowLevel();
            _wantsFlush = false;
            _flushed = true;
            if (_lever != null)
            {
                _lever.SetOpen(true);
                _leverBackAt = Time.time + CloneZConfig.ToiletLeverHold;
            }
            MakeNoise(CloneZConfig.ToiletFlushNoise);
            ctx.Notify(mains
                ? "The cistern empties with a roar and a gurgle, then hisses as it fills. Loud, that."
                : "Down it all goes with a roar, then silence. The cistern doesn't refill. The water's off.");
        }

        /// <summary>Opens or closes the heavy cistern lid and updates the visual state.</summary>
        private void SetCisternLid(InteractContext ctx, bool off)
        {
            _cisternOff = off;
            ShowParts(false);
            MakeNoise(CloneZConfig.ToiletCisternLidNoise);
            ctx.Notify(off
                ? "You lift the heavy china lid off the cistern and stand it against the wall."
                : "You set the cistern lid back on with a careful clunk.");
        }

        /// <summary>Turns the house water mains on or off and notifies the player.</summary>
    private void SetWaterMains(InteractContext ctx, bool on)
    {
        // WaterMains.Instance may be null in tests; guard against it.
        if (WaterMains.Instance != null)
        {
            // Use Restore to change the IsOn state (setter is private).
            WaterMains.Instance.Restore(on, WaterMains.Instance.ShutoffDay);
        }
        ctx.Notify(on ? "You flip the main water valve – the house will start refilling again." : "You flip the main water valve off – the cistern will no longer refill.");
        // Re‑use an existing sound to give feedback.
        MakeNoise(on ? CloneZConfig.ToiletLidNoise : CloneZConfig.ToiletLidNoise);
    }

/// <summary>Shows a description of the current cistern water level to the player.</summary>
    private void LookIn(InteractContext ctx)
    {
        bool mains = WaterMains.MainsOn;
        if (_level <= 0.02f)
            ctx.Notify(mains ? "Empty, but the valve's hissing away as it fills." : "Bone dry. With the water off, it'll stay that way.");
        else if (_level >= 0.98f)
            ctx.Notify("Full to the mark. Clear enough to look at \u2014 but you'd boil it before drinking, if you could.");
        else
        {
            string how = _level > 0.55f ? "More than half full" : _level > 0.2f ? "A few inches of water in the bottom" : "Just a puddle in the bottom";
            ctx.Notify(how + (mains ? ", and filling with a hiss." : ". With the water off, that's the last of it."));
        }
    }

        /// <summary>Shows a random graffiti line from the bathroom wall.</summary>
        private void InspectGraffiti(InteractContext ctx)
        {
            string line = GraffitiLines[Random.Range(0, GraffitiLines.Length)];
            ctx.Notify(line);
        }


        /// <summary>Wipes the toilet seat, providing a small calming effect.</summary>
        private void WipeSeat(InteractContext ctx)
        {
            ctx.Notify("You wipe away grime from the cold porcelain.");
            // a small calming effect, re‑using the existing toilet‑use calm value
            LightingFixtures.Calm(ctx, CloneZConfig.ToiletUseCalm);
        }
        
        private void ScrubSeat(InteractContext ctx)
        {
            if (_seatClean)
            {
                ctx.Notify("The seat is already clean.");
                return;
            }
            _seatClean = true;
            ctx.Notify("You scrub the seat until the grime is gone.");
            ShowParts(false); // update seat colour to show it’s clean
            // a bigger calming effect than a simple wipe
            LightingFixtures.Calm(ctx, CloneZConfig.ToiletUseCalm * 1.5f);
        }

        /// <summary>Saves the toilet's state (seat, lid, cistern, flush flag, level).</summary>
        public override void CaptureState(FixtureSave save)
        {
            save.State = (_seatUp ? 1 : 0) | (_lidUp ? 2 : 0) | (_cisternOff ? 4 : 0) | (_wantsFlush ? 8 : 0)
                | (_seatClean ? 16 : 0) | 32; // 32 marks that the save block is valid
            save.A = _level;
        }

        /// <summary>Restores the toilet's saved state, resetting visual parts accordingly.</summary>
        public override void RestoreState(FixtureSave save)
        {
            // 32 marks a save with the seat's cleanliness in bit 16; older saves carry only their marker, 16.
            if ((save.State & (16 | 32)) == 0) return;
            _seatUp = (save.State & 1) != 0;
            _lidUp = (save.State & 2) != 0 || _seatUp;
            _cisternOff = (save.State & 4) != 0;
            _wantsFlush = (save.State & 8) != 0;
            _seatClean = (save.State & 32) == 0 || (save.State & 16) != 0;
            _flushed = false;
            _level = Mathf.Clamp01(save.A);
            _leverBackAt = -1f;
            if (_lever != null) _lever.SetOpen(false, true);
            ShowParts(true);
            ShowLevel();
        }
    }
}
