using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The kitchen sink under the front window — the fixture fronting the kitchen tap's <see cref="WaterSource"/>. It
    /// lives on the tap's own object, so it's the water point G drinks and fills at too (see <see cref="WaterPoints"/>).
    ///
    /// The cold tap turns on and off: a stream of water runs into the bowl and a running tap is heard a little way off.
    /// It runs only while the mains do (<see cref="WaterMains"/>) — a running tap splutters dry by itself when they're
    /// shut off. The plug goes in and comes out; with it in, the running tap fills the bowl with a couple of drinks' worth
    /// of clean water, which the survivor can still drink, bottle and wash with once the mains have failed (and which
    /// drains away if the plug is pulled). Faces can be splashed, cuts rinsed and the washing-up done, and behind the
    /// two doors under the bowl there's a cupboard (a <see cref="StorageFixture"/>). Saves the tap, the plug, the water
    /// standing in the bowl and when the washing-up was last done.
    /// </summary>
    [DisallowMultipleComponent]
    public class KitchenSink : Interactable, IWaterPoint
    {
        private const float LeftDoorAngle = 88f;     // the door by the corner stops short of the counter's end
        private const float RightDoorAngle = 100f;
        private const float TapTurn = 45f;            // the cold tap's cross-head stands turned (a ×, not a +) while it runs
        private const float StreamRadius = 0.0065f;
        private const string NoWater = "There's no water to wash with \u2014 the tap's dry and the sink is empty";
        private const string GaveOut = "The water gave out before you'd finished";
        // Existing style reference:
        // private const string NoWater = "There's no water to wash with \u2014 the tap's dry and the sink is empty";
        private const string FrostyMorning = "The cold wind rattles the shutters, turning the kitchen into a silent icebox.";
        private const string OldRecipe = "Grandma's chicken broth recipe, scribbled on a torn notebook: chicken, carrots, potatoes, love.";
        private const string WarningSign = "Do not use the gas stove while the power's out – risk of carbon buildup.";
        private const string RadioStatic = "A broken radio crackles: '———... all we got left is the howling wind...'";
        private const string Graffiti = "“Keep the fire alive, or the night will swallow you whole” scratched on the back wall.";
        // New style references – see existing FrostyMorning entry for tone and length.
        private const string SnowboundCough = "A cough comes up in the cold, the kind that rattles your chest when the wind whistles through cracked windows.";
        private const string GrandmaLettuce = "Grandma’s scribbled tip: “Wrap lettuce in damp cloth, keep it crisp longer in the freezer’s cold belly.”";
        private const string CoalStoveWarning = "Keep the coal stove’s fire low after dark – a runaway blaze will melt the roof before the sunrise.";
        private const string HiddenCache = "A crumpled map tucked under a tin can: “Old barn north of the creek, stash buried beneath the oak. – J.”";
        // Kitchen note themes: winter hardship, heirloom cooking tips, fire‑safety advice, and a hidden‑supply hint.
        private static readonly string[] KitchenNotes = new[] {
            FrostyMorning,
            OldRecipe,
            WarningSign,
            RadioStatic,
            Graffiti,
            SnowboundCough,
            GrandmaLettuce,
            CoalStoveWarning,
            HiddenCache
        };

        private static readonly Vector3 PointLocal = new Vector3(FurnitureBuilder.SinkBowlX, FurnitureBuilder.SinkDeckY + 0.01f, FurnitureBuilder.SinkBowlZ);
        private static readonly Vector3 DrainLocal = new Vector3(FurnitureBuilder.SinkBowlX, FurnitureBuilder.SinkBowlBottom, FurnitureBuilder.SinkBowlZ);

        private Transform _fittings;          // everything built onto the unit (built again should the unit ever be moved)
        private Vector3 _builtAt;
        private FixturePart _tapHead;
        private Transform _stream, _splash, _sheet;
        private MeshRenderer _streamR, _splashR, _sheetR, _plugInR, _plugOutR, _dishesR;

        private bool _running;                // the cold tap is open and water is coming out
        public bool IsRunning => _running;
    private bool _plugIn;
    private bool _openedForJob;           // turned on just for a wash, to be turned off again after
    private bool _toldFull;               // the survivor's been told the bowl is brimming (once a fill)
    private float _level;                 // water standing in the bowl, 0..KitchenBasinCapacity
    private float _cleanUntil = -1f;      // world hour the washing-up stays done until (past it, there's more)
    private float _drop;                  // the stream's length, spout to where it lands
    private float _nextNoise, _nextCheck, _wobbleSeed;
    // Cached config values for per‑frame performance
    private float _capacity;
    private float _fillRate;
    private float _drainRate;
    private float _statusInterval;
    private float _noiseInterval;
    private float _noiseRadius;

        private Action<InteractContext> _toggleTap, _putPlug, _pullPlug, _wash, _rinse, _washDishes, _openForJob, _endJob;

        public override string DisplayName => "Kitchen sink";
        public override Vector3 InteractPoint => transform.position + transform.rotation * PointLocal;
        public override string SaveKey => KeyFor("kitchensink", transform.position);

        private bool Dirty => TimeOfDay.Now >= _cleanUntil;

        #region IWaterPoint
        public Vector3 WaterPosition => transform.position + transform.rotation * DrainLocal + new Vector3(0f, 0.1f, 0f);
        public string WaterLabel => WaterMains.MainsOn ? "tap" : "sink";
        public bool HasWater => WaterMains.MainsOn || _level >= CloneZConfig.KitchenBasinDraw - 0.001f;
        public bool IsTainted => false;
        public string DryMessage => _level > 0.01f ? "There's only a puddle left in the sink" : "The tap is dry \u2014 the water's been shut off";

        /// <summary>Mains water is limitless while it runs; after that, each drink or bottle comes out of the bowl.</summary>
        public void DrawWater()
        {
            if (!WaterMains.MainsOn) Take(false, CloneZConfig.KitchenBasinDraw);
        }
        #endregion

        /// <summary>Fits the sink to the kitchen tap <paramref name="tap"/> (its unit already drawn by
        /// <see cref="FurnitureBuilder.SinkUnit"/>): the doors and the cupboard under the bowl, the turning cold tap, the
        /// plug, the water, the washing-up. Fresh: the tap off, the plug out, the bowl empty and the dishes waiting.</summary>
        internal static KitchenSink Attach(WaterSource tap)
        {
            var s = tap.gameObject.AddComponent<KitchenSink>();
            s._wobbleSeed = UnityEngine.Random.value * 100f;
            s.BuildFittings();
            return s;
        }

        private void Awake()
        {
            _toggleTap = ToggleTap;
            _putPlug = c => SetPlug(true);
            _pullPlug = PullPlug;
            _wash = Wash;
            _rinse = Rinse;
            _washDishes = WashDishes;
            _openForJob = OpenForJob;
            _endJob = EndJob;
            // Cache config values for per‑frame use
            _capacity = CloneZConfig.KitchenBasinCapacity;
            _fillRate = CloneZConfig.KitchenBasinFillSeconds;
            _drainRate = CloneZConfig.KitchenBasinDrainSeconds;
            _statusInterval = CloneZConfig.KitchenStatusInterval;
            _noiseInterval = CloneZConfig.KitchenTapNoiseInterval;
            _noiseRadius = CloneZConfig.KitchenTapNoiseRadius;
            // Add warm source component
            var warmth = gameObject.AddComponent<KitchenSinkWarmth>();
            warmth.Initialize(this);
        }

        #region Building
        private void BuildFittings()
        {
            if (_fittings != null) Destroy(_fittings.gameObject);
            Vector3 o = transform.position;
            _builtAt = o;
            // The unit faces the way its root is turned (a raid kitchen's sink stands against any wall).
            var f = new KitchenFrame(o, transform.eulerAngles.y);
            _fittings = new GameObject("SinkFittings").transform;
            _fittings.SetParent(transform, false);
            _fittings.SetPositionAndRotation(o, Quaternion.identity);

            // The pair of doors under the bowl, and the cupboard behind them.
            FurnitureBuilder.KitchenTone tone = FurnitureBuilder.HomeKitchen;
            const float y0 = FurnitureBuilder.SinkDoorY0, front = FurnitureBuilder.SinkDoorFront;
            FixturePart left = KitchenParts.Door(_fittings, "SinkDoorL", f, d => FurnitureBuilder.SinkDoor(d, tone, true),
                new Vector3(FurnitureBuilder.SinkDoorX0, y0, front), Vector3.right, Vector3.forward, LeftDoorAngle);
            FixturePart right = KitchenParts.Door(_fittings, "SinkDoorR", f, d => FurnitureBuilder.SinkDoor(d, tone, false),
                new Vector3(FurnitureBuilder.SinkDoorX1, y0, front), Vector3.left, Vector3.forward, RightDoorAngle);
            StorageFixture.Create(_fittings, "Cupboard under the sink", KeyFor("sinkcupboard", o),
                f.P(FurnitureBuilder.SinkDoorMid, (y0 + FurnitureBuilder.SinkDoorY1) * 0.5f, front + 0.01f), new[] { left, right },
                transform, KitchenParts.SeedAt(o, 1), new StorageOptions
                {
                    Noun = "cupboard under the sink",
                    LootType = ContainerType.Shelf,
                    EmptyChance = CloneZConfig.SinkCupboardEmptyChance,
                    MaxStacks = CloneZConfig.SinkCupboardMaxStacks,
                });

            // The cold tap's cross-head turns as the tap opens.
            Vector3 head = f.P(FurnitureBuilder.SinkColdTap);
            var hm = new RaidMesh();
            hm.SetFrame(head, f.Yaw);
            FurnitureBuilder.SinkTapHead(hm);
            hm.ResetFrame();
            _tapHead = FixturePart.Hinge(_fittings, "ColdTap", hm, head, Vector3.up, -TapTurn).WithSpeed(0.25f);

            // The water: the stream from the spout, its splash, and the water standing in the bowl.
            Surf water = KitchenParts.Water;
            Vector3 spout = FurnitureBuilder.SinkSpout;
            _streamR = KitchenParts.PieceAt(_fittings, "TapWater", f, spout, d => FurnitureBuilder.SinkWaterColumn(d, water));
            _splashR = KitchenParts.PieceAt(_fittings, "TapSplash", f, new Vector3(spout.x, FurnitureBuilder.SinkBowlBottom, spout.z),
                d => FurnitureBuilder.SinkSplash(d, water));
            _sheetR = KitchenParts.PieceAt(_fittings, "BasinWater", f, DrainLocal, d => FurnitureBuilder.SinkBasinWater(d, water));
            _stream = _streamR != null ? _streamR.transform : null;
            _splash = _splashR != null ? _splashR.transform : null;
            _sheet = _sheetR != null ? _sheetR.transform : null;

            // The plug, in the drain or lying beside it on its chain, and the washing-up left in the bowl.
            _plugInR = KitchenParts.PieceAt(_fittings, "PlugIn", f, DrainLocal, d => FurnitureBuilder.SinkPlug(d, true));
            _plugOutR = KitchenParts.PieceAt(_fittings, "PlugOut", f, DrainLocal, d => FurnitureBuilder.SinkPlug(d, false));
            var rng = new System.Random(KitchenParts.SeedAt(o, 2));
            _dishesR = KitchenParts.PieceAt(_fittings, "DirtyDishes", f, DrainLocal, d => FurnitureBuilder.DirtyDishes(d, rng));

            ApplyAll();
        }
        #endregion

        private void Update()
        {
            // An old save's sink is carried to its place in the furnished home after it's built: fit it out again there.
            if ((transform.position - _builtAt).sqrMagnitude > 1e-6f) BuildFittings();

            float now = Time.time;
            if (now >= _nextCheck)
            {
                _nextCheck = now + _statusInterval;
                if (_running && !WaterMains.MainsOn) RunDry();
                RefreshDishes();
            }

            // The bowl fills while the tap runs with the plug in (brimming over into the overflow), and drains without it.
            float dt = Time.deltaTime, cap = _capacity;
            bool moved = false;
            if (_plugIn)
            {
                if (_running && _level < cap)
                {
                    _level = Mathf.Min(_capacity, _level + _capacity * dt / _fillRate);
                    moved = true;
                    if (_level >= cap && !_toldFull)
                    {
                        _toldFull = true;
                        KitchenParts.NotifyNear(InteractPoint, CloneZConfig.KitchenHearRange, "The sink's full to the brim \u2014 the rest is running away down the overflow");
                    }
                }
            }
            else if (_level > 0f)
            {
                _level = Mathf.Max(0f, _level - _capacity * dt / _drainRate);
                moved = true;
            }
            if (_toldFull && _level < cap * 0.9f) _toldFull = false;
            if (moved) RefreshWater();

            // If the tap is not running and the water level cannot change this frame (plug is in or basin empty),
            // skip further updates like noise and wobble to make idle sinks cheaper.
            if (!_running)
            {
                if (_plugIn || _level <= 0f)
                    return;

                // No noise or wobble needed when not running.
            }
            if (now >= _nextNoise)
            {
                _nextNoise = now + _noiseInterval;
                MakeNoise(_noiseRadius);
            }
            Wobble(now);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool mains = WaterMains.MainsOn;
            if (mains)
            {
                // Existing tap toggle
                into.Add(new InteractAction(_running ? "Turn off the tap" : "Turn on the tap", _toggleTap));
                // New combined action: turn off tap and pull plug when both are active
                if (_running && _plugIn)
                {
                    into.Add(new InteractAction("Turn off tap and pull plug", ctx => {
                        SetRunning(false, false);
                        SetPlug(false);
                        ctx.Notify("You turn off the tap and pull the plug.");
                    }));
                }
                var plug = PlugAction(mains);
                if (plug != null) into.Add(plug);
                WaterUse.AddDrinkAndFill(ctx, this, into);
            }
            else
            {
                // Once the mains are off, what's standing in the sink comes first.
                WaterUse.AddDrinkAndFill(ctx, this, into);
                into.Add(new InteractAction("Turn on the tap", _toggleTap));
                var plug = PlugAction(mains);
                if (plug != null) into.Add(plug);
            }

            var wash = new InteractAction("Wash your hands and face", _wash)
                .Timed(CloneZConfig.KitchenWashSeconds, "Washing your hands and face")
                .DisabledIf(Lacking(mains, CloneZConfig.KitchenWashWater), NoWater);
            wash.Begin = _openForJob;
            wash.Stopped = _endJob;
            into.Add(wash);

            int open = OpenWounds(ctx);
            var rinse = new InteractAction("Rinse your wounds", _rinse)
                .Timed(CloneZConfig.KitchenRinseSeconds, "Rinsing your wounds")
                .DisabledIf(open == 0, "You've no open wounds to rinse")
                .DisabledIf(open > 0 && Lacking(mains, CloneZConfig.KitchenRinseWater), NoWater);
            rinse.Begin = _openForJob;
            rinse.Stopped = _endJob;
            into.Add(rinse);

            bool dirty = Dirty;
            var dishes = new InteractAction("Wash the dishes", _washDishes)
                .Timed(CloneZConfig.KitchenDishesSeconds, "Washing the dishes")
                .DisabledIf(!dirty, "The dishes are done \u2014 nothing's piled up in the sink since")
                .DisabledIf(dirty && Lacking(mains, CloneZConfig.KitchenDishesWater), NoWater);
            dishes.Begin = _openForJob;
            dishes.Stopped = _endJob;
            into.Add(dishes);
// When mains are off and water is standing, allow emptying the sink.
        if (!WaterMains.MainsOn && _level > 0.05f)
            into.Add(new InteractAction("Empty the sink", EmptySink));
            into.Add(new InteractAction("Check water level", ShowWaterLevel));
            // New action to read kitchen notes
            into.Add(new InteractAction("Read kitchen notes", ctx => {
                var note = KitchenNotes[UnityEngine.Random.Range(0, KitchenNotes.Length)];
                ctx.Notify(note);
            }));
            // New action – let the survivor feel the running water’s warmth.
            if (_running) {
                into.Add(new InteractAction("Warm your hands", ctx => {
                    ctx.Notify("The running water warms your hands a little.");
                }));
            }
        }

        private InteractAction PlugAction(bool mains)
        {
            // If the tap is running and the plug is in, the combined action already pulls the plug
            // and turns the tap off, so we must not offer a second “Pull the plug out” action.
            if (_running && _plugIn) return null;
            if (!_plugIn) return new InteractAction("Put the plug in", _putPlug);
            return new InteractAction(!mains && _level > 0.05f ? "Pull the plug out and let the water go" : "Pull the plug out", _pullPlug);
        }

        // With the mains off a wash takes its water from the bowl, and there may not be enough.
        private bool Lacking(bool mains, float need) => !mains && _level < need - 0.001f;

        #region The tap and the plug
        private void ToggleTap(InteractContext ctx)
        {
            _openedForJob = false;
            if (_running)
            {
                SetRunning(false, false);
                return;
            }
            if (!WaterMains.MainsOn)
            {
                ctx.Notify("Nothing comes out \u2014 the water's been shut off");
                return;
            }
            SetRunning(true, false);
        }

        private void SetRunning(bool on, bool instant)
        {
            _running = on && WaterMains.MainsOn;
            if (_tapHead != null) _tapHead.SetOpen(_running, instant);
            if (_running)
            {
                _nextNoise = Time.time + _noiseInterval;
                if (!instant) MakeNoise(_noiseRadius);
            }
            RefreshWater();
        }

        // The mains have failed with the tap running.
        private void RunDry()
        {
            _openedForJob = false;
            SetRunning(false, false);
            KitchenParts.NotifyNear(InteractPoint, CloneZConfig.KitchenHearRange, "The tap splutters and runs dry");
        }

        private void PullPlug(InteractContext ctx)
        {
            if (_level > 0.05f) ctx.Notify("The water gurgles away down the drain");
            SetPlug(false);
        }

        private void SetPlug(bool inDrain)
        {
            _plugIn = inDrain;
            if (_plugInR != null) _plugInR.enabled = inDrain;
            if (_plugOutR != null) _plugOutR.enabled = !inDrain;
            RefreshWater();
        }

        // Takes water for a drink or a wash: the mains give it freely; otherwise it comes out of the bowl, if there's enough.
        private bool Take(bool mains, float amount)
        {
            if (mains) return true;
            if (_level < amount - 0.001f) return false;
            _level = Mathf.Max(0f, _level - amount);
            RefreshWater();
            return true;
        }
        #endregion

        #region Washing
        // A wash runs the tap while it lasts (when the mains still run and it isn't running already).
        private void OpenForJob(InteractContext ctx)
        {
            if (_running || !WaterMains.MainsOn) return;
            _openedForJob = true;
            SetRunning(true, false);
        }

        private void EndJob(InteractContext ctx)
        {
            if (!_openedForJob) return;
            _openedForJob = false;
            SetRunning(false, false);
        }

        private void Wash(InteractContext ctx)
        {
            bool mains = WaterMains.MainsOn;
            if (Take(mains, CloneZConfig.KitchenWashWater))
            {
                WaterUse.Freshen(ctx, 1f);
                ctx.Notify(mains
                    ? "You splash cold water on your face and scrub your hands until they squeak. You feel more like yourself."
                    : "You scoop the standing water over your face and scrub your hands in what's left. It'll do.");
            }
            else ctx.Notify(GaveOut);
            EndJob(ctx);
        }

        private void Rinse(InteractContext ctx)
        {
            if (!Take(WaterMains.MainsOn, CloneZConfig.KitchenRinseWater))
            {
                ctx.Notify(GaveOut);
                EndJob(ctx);
                return;
            }
            int rinsed = 0, festering = 0;
            if (ctx.Wounds != null)
            {
                IReadOnlyList<Wound> wounds = ctx.Wounds.Wounds;
                for (int i = 0; i < wounds.Count; i++)
                {
                    Wound w = wounds[i];
                    if (w == null || w.Bandaged) continue;
                    if (w.Infected) { festering++; continue; }
                    w.Sepsis = Mathf.Max(0f, w.Sepsis - CloneZConfig.KitchenRinseSepsis);
                    rinsed++;
                }
            }
            if (rinsed > 0 && festering == 0)
                ctx.Notify("You hold the cuts under the water until it runs clear. It stings like anything, but they're cleaner for it.");
            else if (rinsed > 0)
                ctx.Notify("The fresh cuts rinse clean \u2014 but the hot, swollen one is past anything water can do. It needs antibiotics.");
            else if (festering > 0)
                ctx.Notify("Water does nothing for the hot, angry swelling. That needs antibiotics, not a rinse.");
            else
                ctx.Notify("There's nothing left open to rinse.");
            EndJob(ctx);
        }

        private static int OpenWounds(InteractContext ctx)
        {
            if (ctx.Wounds == null) return 0;
            IReadOnlyList<Wound> wounds = ctx.Wounds.Wounds;
            int n = 0;
            for (int i = 0; i < wounds.Count; i++)
                if (wounds[i] != null && !wounds[i].Bandaged) n++;
            return n;
        }

        private void WashDishes(InteractContext ctx)
        {
            if (!Take(WaterMains.MainsOn, CloneZConfig.KitchenDishesWater))
            {
                ctx.Notify(GaveOut);
                EndJob(ctx);
                return;
            }
            _cleanUntil = TimeOfDay.Now + CloneZConfig.KitchenDishesDirtyHours;
            RefreshDishes();
            if (ctx.Stats != null) ctx.Stats.Panic = Mathf.Max(0f, ctx.Stats.Panic - CloneZConfig.KitchenDishesCalm);
            ctx.Notify("You scrub the plates and the mug, rinse them and stand them in the rack to drip. Such an ordinary job. It steadies you.");
            EndJob(ctx);
        }

        // Empties the sink when mains are off and there is standing water.
        private void EmptySink(InteractContext ctx)
        {
            _level = 0f;
            _toldFull = false;
            RefreshWater();
            ctx.Notify("You empty the sink; the water drains away.");
        }

        // Shows the current water level in the sink.
        private void ShowWaterLevel(InteractContext ctx)
        {
            float percent = (_level / _capacity) * 100f;
            ctx.Notify($"The sink holds {percent:0}% of its capacity.");
        }
        #endregion

        #region Looks
        private void ApplyAll()
        {
            if (_tapHead != null) _tapHead.SetOpen(_running, true);
            if (_plugInR != null) _plugInR.enabled = _plugIn;
            if (_plugOutR != null) _plugOutR.enabled = !_plugIn;
            RefreshDishes();
            RefreshWater();
        }

        private void RefreshDishes()
        {
            bool dirty = Dirty;
            if (_dishesR != null && _dishesR.enabled != dirty) _dishesR.enabled = dirty;
        }

        // Puts the water standing in the bowl at its level, and runs the stream from the spout down to where it lands.
        private void RefreshWater()
        {
            float bottom = transform.position.y + FurnitureBuilder.SinkBowlBottom;
            float surface = bottom + FurnitureBuilder.SinkWaterDepth * Mathf.Clamp01(_level / _capacity);
            bool standing = _level > 0.004f;
            if (_sheetR != null)
            {
                _sheetR.enabled = standing;
                Vector3 p = _sheet.position;
                _sheet.position = new Vector3(p.x, surface, p.z);
            }
            float land = standing ? surface : bottom + (_plugIn ? 0.008f : 0.001f);
            if (_splashR != null)
            {
                _splashR.enabled = _running;
                Vector3 p = _splash.position;
                _splash.position = new Vector3(p.x, land, p.z);
            }
            if (_streamR != null)
            {
                _streamR.enabled = _running;
                _drop = Mathf.Max(0.02f, _stream.position.y - land);
                _stream.localScale = new Vector3(StreamRadius, _drop, StreamRadius);
            }
        }

        // The running water never hangs quite still: its width wavers and the splash pulses.
        private void Wobble(float t)
        {
            float a = Mathf.PerlinNoise(t * 7.3f, _wobbleSeed), b = Mathf.PerlinNoise(_wobbleSeed, t * 6.1f);
            if (_stream != null) _stream.localScale = new Vector3(StreamRadius * (0.85f + 0.3f * a), _drop, StreamRadius * (0.85f + 0.3f * b));
            if (_splash != null) _splash.localScale = Vector3.one * (0.8f + 0.4f * b);
        }
        #endregion

        #region Persistence
        public override void CaptureState(FixtureSave save)
        {
            save.State = (_running && !_openedForJob ? 1 : 0) | (_plugIn ? 2 : 0);
            save.A = _level;
            save.B = _cleanUntil;
        }

        public override void RestoreState(FixtureSave save)
        {
            _openedForJob = false;
            _plugIn = (save.State & 2) != 0;
            _level = Mathf.Clamp(save.A, 0f, _capacity);
            _toldFull = _level >= _capacity * 0.9f;
            _cleanUntil = save.B;
            _running = (save.State & 1) != 0 && WaterMains.MainsOn;
            if (_running) _nextNoise = Time.time + _noiseInterval;
            ApplyAll();
        }
        #endregion
    }
}
