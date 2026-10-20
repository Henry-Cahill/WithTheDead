using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A raid ranch's bath: a built-in tub with a hot and a cold tap over the drain and a plug on a chain. The taps run
    /// while the mains do — loud enough to be heard a little way off — and with the plug in they fill the tub; the water
    /// is hot while the house has power for the water heater and goes cold standing. Pull the plug and it drains away
    /// with a long gurgle. It's an <see cref="IWaterPoint"/>: drink from the taps or fill a bottle (G works here too) —
    /// and once the mains are off, from what's standing in the tub (clean, unless somebody's bathed in it). "Have a soak"
    /// sits the survivor down in the water to lie back for as long as they like (an open-ended job: moving or [E] climbs
    /// out): a hot bath soaks the panic and the tiredness away and warms them through; a cold one chills. Either way they
    /// climb out dripping. A hot bath steams the room a little (an <see cref="IWarmthSource"/>). Saves the taps, the plug,
    /// the water and its warmth.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidHomeBath : Interactable, IWaterPoint, IWarmthSource
    {
        private const float SoakHipsBack = 0.3f;   // the hips rest this far back from the middle, away from the taps
        private const float SoakHipsY = 0.14f;      // just off the tub's floor

        private Transform _body;
        private string _key;
        private Vector3 _point, _spout, _middle, _seat;
        private float _yaw, _end;
        private FixturePart _hot, _cold;
        private Renderer _stream, _waterR, _plugIn, _plugOut;
        private Transform _water;
        private Vector3 _waterFull;
        private bool _running, _plugged, _borrowed, _bathedIn, _toldFull, _soaking;
        private float _level;    // 0 empty .. 1 full
        private float _heat;     // the standing water's warmth: 0 stone cold .. 1 hot
        private float _nextNoise, _nextWatch;

        public override string DisplayName => "Bath";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _body != null ? _body : transform;
        public override string SaveKey => _key;

        /// <summary>True while water is actually coming out of the taps.</summary>
        public bool Running => _running && WaterMains.MainsOn;

        // The water heater runs off the mains electricity.
        private static bool HotSupply => PowerGrid.HousePowered;

        #region IWaterPoint
        public Vector3 WaterPosition => _spout;
        public string WaterLabel => WaterMains.MainsOn ? "taps" : "bath";
        public bool HasWater => WaterMains.MainsOn || _level >= CloneZConfig.BathDrinkShare;
        public bool IsTainted => !WaterMains.MainsOn && _bathedIn;
        public string DryMessage => _level > 0.02f ? "There's barely a puddle left in the bath" : "The taps are dry \u2014 the water's been shut off";

        /// <summary>
        /// Updates the stored water level and visual when the taps are off.
        /// </summary>
        public void DrawWater()
        {
            if (WaterMains.MainsOn) return;   // straight from the taps: as much as you like while they run
            _level = Mathf.Max(0f, _level - CloneZConfig.BathDrinkShare);
            ShowWater();
        }
        #endregion

        #region IWarmthSource
        public Vector3 WarmthPosition => _middle;
        public float WarmthRadius => CloneZConfig.BathWarmthRadius;
        public float Warmth => _level > 0.3f && _heat > 0.5f ? CloneZConfig.BathWarmth * _heat : 0f;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            HomeComfort.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HomeComfort.Unregister(this);
        }

        /// <summary>The bath's fixture on <paramref name="body"/> (the tub's furniture root): <paramref name="f"/> is the
        /// tub's frame (front toward +Z) and <paramref name="end"/> which end the taps are at (+1: +X, -1: -X).</summary>
        internal static RaidHomeBath Create(Transform body, in KitchenFrame f, float end)
        {
            Vector3 point = f.P(end * 0.42f, FurnitureBuilder.BathHeight + 0.08f, FurnitureBuilder.BathDepth * 0.5f - 0.05f);
            var go = new GameObject("BathFixture");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var b = go.AddComponent<RaidHomeBath>();
            b._body = body;
            b._yaw = f.Yaw;
            b._end = end;
            b._point = point;
            b._spout = f.P(FurnitureBuilder.BathSpout(end));
            b._middle = f.P(0f, 0.6f, 0f);
            b._seat = f.P(-end * SoakHipsBack, SoakHipsY, 0f);
            b._key = KeyFor("bath", f.Origin);
            return b;
        }

        /// <summary>Hands over the hot and cold taps' heads (hinged to turn when running), the stream from the spout, the
        /// water (drawn full, its origin on the surface's middle; it sinks with the level) and the plug in the drain and out.</summary>
        internal void SetParts(FixturePart hot, FixturePart cold, Renderer stream, Renderer water, Renderer plugIn, Renderer plugOut)
        {
            _hot = hot;
            _cold = cold;
            _stream = stream;
            _waterR = water;
            _water = water != null ? water.transform : null;
            if (_water != null) _waterFull = _water.position;
            _plugIn = plugIn;
            _plugOut = plugOut;
            SetTaps(_running, true);
            ShowPlug();
            ShowWater();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool running = Running;
            if (_stream != null && _stream.enabled != running) _stream.enabled = running;

            // Plugged, the running taps fill the tub (hot while there's power for the heater, mixing with what's there);
            // unplugged, whatever's in it drains away. Standing water cools.
            float before = _level;
            if (running && _plugged && _level < 1f)
            {
                float inflow = dt / CloneZConfig.BathFillSeconds;
                float supply = HotSupply ? 1f : 0.05f;
                _heat = _level < 0.01f ? supply : _heat + (supply - _heat) * (inflow / (_level + inflow));
                _level = Mathf.Min(1f, _level + inflow);
                if (_level >= 1f && !_toldFull)
                {
                    _toldFull = true;
                    KitchenParts.NotifyNear(_middle, 4f, "The bath's full to the overflow \u2014 the taps are still running.");
                }
            }
            else if (!_plugged && _level > 0f) _level = Mathf.Max(0f, _level - dt / CloneZConfig.BathDrainSeconds);
            if (!running && _heat > 0f) _heat = Mathf.Max(0f, _heat - dt / CloneZConfig.BathCoolSeconds);
            if (_level != before) ShowWater();
            if (_level < 0.9f) _toldFull = false;

            if (running && Time.time >= _nextNoise)
            {
                _nextNoise = Time.time + CloneZConfig.BathTapNoiseInterval;
                MakeNoise(CloneZConfig.BathTapNoiseRadius);
            }
            if (_soaking && Time.time >= _nextWatch) Watch();
        }

        // Belt and braces: however the soak ended, the survivor isn't left sitting in the tub.
        private void Watch()
        {
            _nextWatch = Time.time + 0.5f;
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi != null && pi.JobTarget == this) return;
            _soaking = false;
            InteractContext ctx = pi != null ? pi.Context : null;
            if (ctx != null && ctx.Seat != null && ctx.Seat.IsSittingOn(this)) ctx.Seat.Stand();
        }

        private void ShowWater()
        {
            if (_water == null) return;
            bool any = _level > 0.01f;
            if (_waterR.enabled != any) _waterR.enabled = any;
            float depth = FurnitureBuilder.BathWaterTopY - FurnitureBuilder.BathFloorY - 0.006f;
            _water.position = _waterFull + Vector3.down * ((1f - _level) * depth);
        }

        private void ShowPlug()
        {
            if (_plugIn != null && _plugIn.enabled != _plugged) _plugIn.enabled = _plugged;
            if (_plugOut != null && _plugOut.enabled == _plugged) _plugOut.enabled = !_plugged;
        }

        /// <summary>
        /// Returns the interactions available for the bath, depending on water, taps and plug state.
        /// </summary>
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool mains = WaterMains.MainsOn;
            bool deep = _level >= CloneZConfig.BathSoakLevel;
            if (deep) into.Add(SoakAction(ctx));
            if (_running) into.Add(new InteractAction("Turn off the taps", c => Turn(c, false)));
            else if (!_plugged && _level < 0.05f) into.Add(new InteractAction("Run a bath", RunBath).DisabledIf(!mains, DryMessage));
            else into.Add(new InteractAction("Turn on the taps", c => Turn(c, true)));
            into.Add(new InteractAction(_plugged ? "Pull the plug out" : "Put the plug in", c => SetPlug(c, !_plugged)));
            WaterUse.AddDrinkAndFill(ctx, this, into);

            var wash = new InteractAction("Wash your hands and face", Wash)
                .Timed(CloneZConfig.BathWashSeconds, "Washing", HumanoidActivity.Use)
                .DisabledIf(!mains && _level < CloneZConfig.BathWashLevel, DryMessage);
            wash.Begin = BorrowTaps;
            wash.Stopped = ReturnTaps;
            into.Add(wash);
            if (_level > 0.05f) {
                into.Add(new InteractAction("Feel the water", FeelWater));
                into.Add(new InteractAction("Check water temperature", CheckTemp));
            }
        }

        #region The taps and the plug
        private void RunBath(InteractContext ctx)
        {
            _borrowed = false;
            SetPlugged(true);
            SetTaps(true, false);
            ctx.Notify(HotSupply
                ? "You put the plug in and run the bath. Steam starts to curl up off the water."
                : "You put the plug in and run the bath. It comes out cold \u2014 no power for the water heater.");
        }

        private void Turn(InteractContext ctx, bool on)
        {
            _borrowed = false;
            SetTaps(on, false);
            if (on && !WaterMains.MainsOn) ctx.Notify("The pipes knock and shudder, but nothing comes out. The water's off.");
        }

        private void SetTaps(bool on, bool instant)
        {
            _running = on;
            if (_hot != null) _hot.SetOpen(on, instant);
            if (_cold != null) _cold.SetOpen(on, instant);
            if (!instant) MakeNoise(CloneZConfig.BathTapTurnNoise);
            bool running = Running;
            if (_stream != null && _stream.enabled != running) _stream.enabled = running;
        }

        private void SetPlug(InteractContext ctx, bool plugged)
        {
            bool full = _level > 0.1f;
            SetPlugged(plugged);
            if (plugged && Running) ctx.Notify("You push the plug in and the bath starts to fill.");
            else if (!plugged && full)
            {
                MakeNoise(CloneZConfig.BathDrainNoise);
                ctx.Notify(WaterMains.MainsOn
                    ? "Out comes the plug, and the bath drains away with a long, rude gurgle."
                    : "Out comes the plug, and the water you'd saved swirls away down the drain.");
            }
        }

        private void SetPlugged(bool plugged)
        {
            _plugged = plugged;
            ShowPlug();
        }

        // A wash runs the taps for its length while the mains are on (and turns them off again after).
        private void BorrowTaps(InteractContext ctx)
        {
            if (!WaterMains.MainsOn || _running) return;
            _borrowed = true;
            SetTaps(true, false);
        }

        private void ReturnTaps(InteractContext ctx)
        {
            if (!_borrowed) return;
            _borrowed = false;
            SetTaps(false, false);
        }

        private void Wash(InteractContext ctx)
        {
            bool mains = WaterMains.MainsOn;
            ReturnTaps(ctx);
            if (!mains)
            {
                _level = Mathf.Max(0f, _level - CloneZConfig.BathDrinkShare);
                _bathedIn = true;
                ShowWater();
            }
            WaterUse.Freshen(ctx, CloneZConfig.BasinWashFreshen);
            ctx.Notify(!mains ? "You wash in the water standing in the tub. Not clean, exactly, but cleaner."
                : HotSupply ? "Hot water from the tap. You scrub your hands and face until they sting."
                : "The water's cold \u2014 no power for the heater \u2014 but it wakes you up.");
        }

        private void FeelWater(InteractContext ctx)
        {
            string depth = _level >= 0.9f ? "brimming" : _level >= CloneZConfig.BathSoakLevel ? "deep enough to get into" : "only a few inches deep";
            string warm = _heat >= 0.75f ? "hot — properly hot" : _heat >= 0.45f ? "warm" : _heat >= 0.2f ? "lukewarm" : "stone cold";
            ctx.Notify("You trail a hand in the bath. It's " + depth + ", and " + warm + (_bathedIn ? ". Somebody's already been in it." : "."));
        }

        private void CheckTemp(InteractContext ctx)
        {
            string warm = _heat >= 0.75f ? "hot — properly hot" : _heat >= 0.45f ? "warm" : _heat >= 0.2f ? "lukewarm" : "stone cold";
            ctx.Notify("The water feels " + warm + ".");
        }
        #endregion

        #region A soak
        // An open-ended soak: in, lying back in the water, until the survivor moves or presses [E] (or the water's gone).
        private InteractAction SoakAction(InteractContext ctx)
        {
            var a = new InteractAction("Have a soak").Timed(-1f, "Soaking in the bath", HumanoidActivity.None)
                .DisabledIf(ctx.Seat == null, "You can't get in now");
            a.Begin = GetIn;
            a.Tick = WhileSoaking;
            a.Stopped = GetOut;
            return a;
        }

        private void GetIn(InteractContext ctx)
        {
            string reason = null;
            if (ctx.Seat == null || !ctx.Seat.TrySit(this, _seat, _yaw + (_end > 0f ? 90f : 270f), CloneZConfig.BathSoakComfort, out reason))
            {
                ctx.Notify(reason ?? "You can't get in now");
                if (ctx.Interactor != null) ctx.Interactor.StopJob();
                return;
            }
            _soaking = true;
            _bathedIn = true;
            _nextWatch = Time.time + 0.5f;
            MakeNoise(CloneZConfig.BathTapTurnNoise);
            if (ctx.Stats != null) ctx.Stats.Wetness = Mathf.Max(ctx.Stats.Wetness, CloneZConfig.BathWetness);
            ctx.Notify(_heat >= 0.6f ? "You lower yourself into the hot water an inch at a time. Oh, that's good."
                : _heat >= 0.25f ? "You ease down into the water. It's gone lukewarm, but it's still a bath."
                : "You force yourself down into the cold water, gasping. This is not relaxing.");
        }

        private void WhileSoaking(InteractContext ctx, float dt, float elapsed)
        {
            if (ctx.Seat == null || !ctx.Seat.IsSittingOn(this))
            {
                if (ctx.Interactor != null) ctx.Interactor.StopJob();
                return;
            }
            if (_level < CloneZConfig.BathSoakLevel * 0.5f)
            {
                ctx.Notify("The water's drained away from under you.");
                if (ctx.Interactor != null) ctx.Interactor.StopJob();
                return;
            }
            PlayerStats s = ctx.Stats;
            if (s == null || dt <= 0f) return;
            float h = _heat;
            s.Panic = Mathf.Max(0f, s.Panic - CloneZConfig.BathSoakCalmPerSec * (0.3f + 0.7f * h) * dt);
            s.Fatigue = Mathf.Max(0f, s.Fatigue - CloneZConfig.BathSoakRestPerSec * h * dt);
            if (h >= 0.35f)
            {
                if (s.Temperature < CloneZConfig.BathSoakWarmTarget)
                    s.Temperature = Mathf.Min(CloneZConfig.BathSoakWarmTarget, s.Temperature + CloneZConfig.BathSoakWarmPerSec * h * dt);
            }
            else if (h < 0.2f) s.Temperature = Mathf.Max(0f, s.Temperature - CloneZConfig.BathColdChillPerSec * dt);
            s.Wetness = Mathf.Max(s.Wetness, CloneZConfig.BathWetness);
        }

        // Out of the water and back onto the floor where the survivor got in, dripping.
        private void GetOut(InteractContext ctx)
        {
            if (!_soaking) return;
            _soaking = false;
            if (ctx.Seat != null && ctx.Seat.IsSittingOn(this)) ctx.Seat.Stand();
            if (ctx.Stats != null) ctx.Stats.Wetness = Mathf.Max(ctx.Stats.Wetness, CloneZConfig.BathWetness);
            MakeNoise(CloneZConfig.BathTapTurnNoise);
            ctx.Notify(_heat >= 0.45f ? "You climb out pink and dripping. A towel would be good about now."
                : "You climb out dripping and shivering. Find a towel, quick.");
        }
        #endregion

        public override void CaptureState(FixtureSave save)
        {
            save.State = (_running && !_borrowed ? 1 : 0) | (_plugged ? 2 : 0) | (_bathedIn ? 4 : 0) | 8;
            save.A = _level;
            save.B = _heat;
        }

                public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 8) == 0) return;
            _borrowed = false;
            SetTaps((save.State & 1) != 0, true);
            SetPlugged((save.State & 2) != 0);
            _bathedIn = (save.State & 4) != 0;
            _level = Mathf.Clamp01(save.A);
            _heat = Mathf.Clamp01(save.B);
            ShowWater();
        }
    }
}
