using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The bathroom's pedestal basin: a single-lever mixer tap and a plug on a chain. The tap runs while the mains do (it
    /// stops by itself after the shut-off, the lever left up) and can be heard a little way off. With the plug in, running
    /// water fills the bowl — a basinful is about a drink's worth of clean water, and it stays after the shut-off for as
    /// long as the plug does; pull the plug and it drains away. It's an <see cref="IWaterPoint"/>: drink from the tap or
    /// fill a bottle (G works here too). And the everyday things: wash your hands and face, brush your teeth, have a shave
    /// — each runs the tap for you while the mains are on, or uses some of the basinful once they're off. Saves the tap,
    /// the plug and the water in the bowl.
    /// </summary>
    [DisallowMultipleComponent]
    public class BasinFixture : Interactable, IWaterPoint
    {
        private static readonly string[] BrushLines =
        {
            "Two minutes of brushing, counted in your head. A small, stubborn piece of normal.",
            "You brush until your gums tingle and rinse with a splash. Minty. Almost civilised.",
            "The frigid air bites your cheeks as you scrub, reminding you the world outside is still frozen.",
            "You watch the thin stream of water turn into a misty cloud, disappearing like breath in the room.",
            "A thin film of soot clings to your toothbrush, a reminder of the burnt logs in the stove.",
            "The metal of the tap feels icy, and you wonder how long the mains will stay silent.",
            "You spit the frothy foam into the basin, hearing it echo in the tiny, empty space.",
            "Each swipe feels like a small rebellion against the cold that seeps through the cracked walls.",
            "The bite of winter air sharpens the mint as you brush your teeth.",
            "The tap's echo fills the empty house, a hollow sound that fades like a forgotten habit.",
            "A flash of pre‑outbreak routine surfaces, the simple rhythm of brushing before the world fell.",
            "The water tastes of metal, a thin grit that clings to the mint and to memory.",
            "Your reflection shivers in the cracked mirror, the fractured glass framing a resolve that steadies your hand.",
            "In that brief pause you find a quiet resolve, each brushstroke a small claim against the silence.",
            "The cold wind rattles the cracked window as you brush, a reminder that winter still claws at the walls.",
            "Each minty swipe feels like a promise to survive the desolate snow outside.",
            "The frosty breath you exhale mixes with the water, echoing the lingering ghost of pre‑outbreak mornings.",
            "Your toothbrush scratches the porcelain, a quiet defiance against the world that fell silent.",
            "The metal basin shivers under the tap, mirroring the resolve you feel to endure the endless winter."
        };

        private static readonly string[] ShaveLines =
        {
            "A slow, careful shave. You only nick yourself once.",
            "Lather, scrape, rinse. Steady hands. It's good to do something properly for a change.",
"You shave by the mirror's say-so. The face underneath looks almost like the old you.",
            "The tap coughs each time you lift the lever, a reminder the mains are a memory now.",
            "Your reflection flickers in the cracked mirror, showing a face half‑lost to the frost.",
            "Cold water runs over the razor, turning the shave into a bite of ice on your skin.",
            "The scent of soot from the stove clings to the steam, mixing with the taste of metal.",
            "Each shave feels like salvaging a small piece of normality from the relentless cold.",
            
        };

        private HomeFurniture _host;
        private string _key;
        private Vector3 _point, _spout;
        private FixturePart _lever;
        private Renderer _stream, _sheetRenderer, _plugIn, _plugOut;
        private Transform _sheet;
        private Vector3 _sheetFull;
        private bool _tapOpen, _plugged, _borrowedTap;
        private float _level;                // water standing in the bowl: 0 none .. 1 full
        private float _nextNoise;

        public override string DisplayName => "Basin";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _host != null ? _host.transform : transform;
        public override string SaveKey => _key;

        /// <summary>True while water is actually running from the tap.</summary>
        public bool Running => _tapOpen && WaterMains.MainsOn;

        #region IWaterPoint
        public Vector3 WaterPosition => _spout;
        public string WaterLabel => WaterMains.MainsOn ? "tap" : "basin";
        public bool HasWater => WaterMains.MainsOn || _level >= CloneZConfig.BasinDrinkLevel;
        public bool IsTainted => false;
        public string DryMessage => _level > 0.02f ? "There's barely a puddle left in the basin" : "The tap is dry \u2014 the water's been shut off";

        /// <summary>Drains water from the basin when the mains are off, reducing the stored level and updating the visual.</summary>
        public void DrawWater()
        {
            if (WaterMains.MainsOn) return;   // straight from the tap: as much as you like while it runs
            _level = Mathf.Max(0f, _level - 1f);   // a basinful makes a single drink
            ShowWater();
        }
        #endregion

        /// <summary>The basin's fixture on <paramref name="host"/> (the basin itself): its points come from the basin's
        /// drawing (<see cref="FurnitureBuilder.PedestalBasin"/>) and its key from where it stands.</summary>
        internal static BasinFixture Create(HomeFurniture host)
        {
            Vector3 point = BathroomFixtures.Point(host, FurnitureBuilder.BasinReach);
            var go = new GameObject("BasinFixture");
            go.transform.SetParent(host.transform, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var b = go.AddComponent<BasinFixture>();
            b._host = host;
            b._point = point;
            b._spout = BathroomFixtures.Point(host, FurnitureBuilder.BasinSpout);
            b._key = KeyFor("basin", host.transform.position);
            return b;
        }

        /// <summary>Hands over the tap's lever (hinged to lift when on), the stream (shown while it runs), the water sheet
        /// (drawn full, its origin on the surface's middle; it sinks and narrows with the level) and the plug in the drain
        /// and out on the deck.</summary>
        internal void SetParts(FixturePart lever, Renderer stream, Renderer sheet, Renderer plugIn, Renderer plugOut)
        {
            _lever = lever;
            _stream = stream;
            _sheetRenderer = sheet;
            _sheet = sheet != null ? sheet.transform : null;
            if (_sheet != null) _sheetFull = _sheet.position;
            _plugIn = plugIn;
            _plugOut = plugOut;
            SetTap(_tapOpen, true);
            ShowPlug();
            ShowWater();
        }

        private void Update()
        {
            bool running = Running;
            if (_stream != null && _stream.enabled != running) _stream.enabled = running;

            // Plugged, the running tap fills the bowl; unplugged, whatever's in it drains away.
            float before = _level;
            if (_plugged)
            {
                if (running && _level < 1f) _level = Mathf.Min(1f, _level + Time.deltaTime / CloneZConfig.BasinFillSeconds);
            }
            else if (_level > 0f) _level = Mathf.Max(0f, _level - Time.deltaTime / CloneZConfig.BasinDrainSeconds);
            if (_level != before) ShowWater();

            if (running && Time.time >= _nextNoise)
            {
                _nextNoise = Time.time + CloneZConfig.BasinTapNoiseInterval;
                MakeNoise(CloneZConfig.BasinTapNoiseRadius);
            }
        }

        private void ShowWater()
        {
            if (_sheet == null) return;
            bool any = _level > 0.01f;
            if (_sheetRenderer.enabled != any) _sheetRenderer.enabled = any;
            float k = FurnitureBuilder.BasinWaterScale(_level);
            _sheet.localScale = new Vector3(k, 1f, k);
            _sheet.position = _sheetFull + Vector3.down * ((1f - _level) * FurnitureBuilder.BasinWaterDepth);
        }

        private void ShowPlug()
        {
            if (_plugIn != null && _plugIn.enabled != _plugged) _plugIn.enabled = _plugged;
            if (_plugOut != null && _plugOut.enabled == _plugged) _plugOut.enabled = !_plugged;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_tapOpen ? "Turn off the tap" : "Turn on the tap", c => Turn(c, !_tapOpen)));
            into.Add(new InteractAction(_plugged ? "Pull the plug out" : "Put the plug in", c => SetPlug(c, !_plugged)));
            // Tiny action to let player check current water level in the basin
            into.Add(new InteractAction("Check water level", CheckLevel));
            WaterUse.AddDrinkAndFill(ctx, this, into);

            bool mains = WaterMains.MainsOn;
            var wash = new InteractAction("Wash your hands and face", Wash)
                .Timed(CloneZConfig.BasinWashSeconds, "Washing", HumanoidActivity.Use)
                .DisabledIf(!mains && _level < CloneZConfig.BasinWashWater * 0.5f, DryMessage);
            wash.Begin = BorrowTap;
            wash.Stopped = ReturnTap;
            into.Add(wash);

            var brush = new InteractAction("Brush your teeth", Brush)
                .Timed(CloneZConfig.BasinBrushSeconds, "Brushing your teeth", HumanoidActivity.Use);
            brush.Begin = BorrowTap;
            brush.Stopped = ReturnTap;
            into.Add(brush);

            var rinse = new InteractAction("Rinse your mouth", Rinse)
                .Timed(CloneZConfig.BasinBrushSeconds / 2f, "Rinsing", HumanoidActivity.Use);
            rinse.Begin = BorrowTap;
            rinse.Stopped = ReturnTap;
            into.Add(rinse);

            var shave = new InteractAction("Have a shave", Shave)
                .Timed(CloneZConfig.BasinShaveSeconds, "Shaving", HumanoidActivity.Use)
                .DisabledIf(!mains && _level < CloneZConfig.BasinShaveWater * 0.5f, "There's no water to shave with");
            shave.Begin = BorrowTap;
            shave.Stopped = ReturnTap;
            into.Add(shave);
        }

        private void Turn(InteractContext ctx, bool open)
        {
            _borrowedTap = false;
            SetTap(open, false);
            if (open && !WaterMains.MainsOn) ctx.Notify("The tap coughs and shudders, and nothing comes. The water's off.");
        }

        private void SetTap(bool open, bool instant)
        {
            _tapOpen = open;
            if (_lever != null) _lever.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.BasinTapTurnNoise);
            bool running = Running;
            if (_stream != null && _stream.enabled != running) _stream.enabled = running;
        }

        private void SetPlug(InteractContext ctx, bool plugged)
        {
            _plugged = plugged;
            ShowPlug();
            if (plugged && Running) ctx.Notify("You push the plug in and the basin starts to fill.");
            else if (!plugged && _level > 0.05f)
                ctx.Notify(WaterMains.MainsOn
                    ? "The water gurgles away down the drain."
                    : "Out comes the plug, and the water you saved swirls away down the drain.");
        }

        // A wash, a brush or a shave runs the tap for its length while the mains are on (and turns it off again after).
        private void BorrowTap(InteractContext ctx)
        {
            if (!WaterMains.MainsOn || _tapOpen) return;
            _borrowedTap = true;
            SetTap(true, false);
        }

        private void ReturnTap(InteractContext ctx)
        {
            if (!_borrowedTap) return;
            _borrowedTap = false;
            SetTap(false, false);
        }

        // Once the mains are off, a share of the basinful; true when there was water to be had.
        private bool UseStored(float share)
        {
            if (WaterMains.MainsOn) return true;
            if (_level <= 0.02f) return false;
            _level = Mathf.Max(0f, _level - share);
            ShowWater();
            return true;
        }

        private void Wash(InteractContext ctx)
        {
            bool mains = WaterMains.MainsOn;

            UseStored(CloneZConfig.BasinWashWater);
            WaterUse.Freshen(ctx, CloneZConfig.BasinWashFreshen);
            ctx.Notify(!mains ? "You wash in the water you saved in the basin. Every drop counts now."
                : PowerGrid.HousePowered ? "Warm water and soap. You scrub your hands, splash your face and feel more like yourself."
                : "The water's cold \u2014 no power for the heater \u2014 but it wakes you up.");
        }

        private void Brush(InteractContext ctx)
        {
            bool wet = UseStored(CloneZConfig.BasinBrushWater);
            BathroomFixtures.Nerves(ctx, -CloneZConfig.BasinBrushCalm);
            ctx.Notify(wet ? BrushLines[Random.Range(0, BrushLines.Length)]
                : "No water to rinse with, so you brush dry and spit into the basin. Better than nothing.");
        }

        private void Rinse(InteractContext ctx)
        {
            bool wet = UseStored(CloneZConfig.BasinBrushWater);
            // No extra calm effect; just a quick rinse.
            ctx.Notify(wet ? "You rinse your mouth, feeling a brief freshness." : "No water left to rinse; you gargle dry.");
        }

        private void Shave(InteractContext ctx)
        {

            UseStored(CloneZConfig.BasinShaveWater);
            BathroomFixtures.Nerves(ctx, -CloneZConfig.BasinShaveCalm);
            ctx.Notify(ShaveLines[Random.Range(0, ShaveLines.Length)]);
        }

        private void CheckLevel(InteractContext ctx)
        {
            float fill = _level;
            string level =
                fill <= 0.01f ? "Bone‑dry. No water in the basin." :
                fill < 0.25f ? "Barely a puddle in the bottom." :
                fill < 0.5f ? "About a quarter full." :
                fill < 0.75f ? "Roughly half full." :
                fill < 0.99f ? "Nearly full." :
                "Brimming — it can't hold any other water.";
            ctx.Notify(level);
        }


        public override void CaptureState(FixtureSave save)
        {
            save.State = (_tapOpen && !_borrowedTap ? 1 : 0) | (_plugged ? 2 : 0) | 4;
            save.A = _level;
        }

        public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 4) == 0) return;
            _borrowedTap = false;
            SetTap((save.State & 1) != 0, true);
            _plugged = (save.State & 2) != 0;
            ShowPlug();
            _level = Mathf.Clamp01(save.A);
            ShowWater();
        }
    }
}
