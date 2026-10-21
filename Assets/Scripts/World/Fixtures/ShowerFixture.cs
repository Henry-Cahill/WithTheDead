using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The bathroom's shower stall: a glass door that swings out and a mixer that sets the rose spraying — while the
    /// mains run (it stops by itself after the shut-off, the lever left on), loud enough to be heard a little way off.
    /// The water is hot while the house has power for the water heater: a hot shower running warms the air round it
    /// (an <see cref="IWarmthSource"/>). "Take a shower" does it all: the door opens, the survivor steps in under the
    /// water (the stall's own collider stood aside meanwhile), the door's pulled to, and when it's done — or abandoned —
    /// they step back out where they came in. Hot, it washes the panic and some of the tiredness away; cold, it's a
    /// shock that chills to the bone. Either way you come out dripping. Saves the door and the mixer.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShowerFixture : Interactable, IWarmthSource
    {
        private HomeFurniture _host;
        private string _key;
        private Vector3 _point, _standIn, _stepOut, _middle;
        private FixturePart _door, _lever;
        private Renderer _sprayA, _sprayB;
        private Collider _stall;
        private bool _on, _doorOpen;
        private bool _inside, _turnedOnToShower, _hasEntry, _flick;
        private PlayerInteractor _playerInteractor;
        private Vector3 _enteredFrom;
        private float _enteredYaw;
        private float _underWater;
        private float _nextNoise, _nextFlick, _nextCheck;
        private float _closeDoorAt = -1f;

        public override string DisplayName => "Shower";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _host != null ? _host.transform : transform;
        public override string SaveKey => _key;

        /// <summary>True while water is actually coming out of the rose.</summary>
        public bool Running => _on && WaterMains.MainsOn;

        // The water heater runs off the mains electricity.
        private static bool Hot => PowerGrid.HousePowered;

        #region IWarmthSource
        /// <summary>The world position that provides warmth from the shower.</summary>
        public Vector3 WarmthPosition => _middle;
        /// <summary>Radius around the shower that provides warmth.</summary>
        public float WarmthRadius => CloneZConfig.ShowerWarmthRadius;
        /// <summary>The amount of warmth the shower provides when running and hot.</summary>
        public float Warmth => Running && Hot ? CloneZConfig.ShowerWarmth : 0f;
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
            _playerInteractor = null;
            if (!_inside) return;
            _inside = false;
            SetStallSolid(true);
        }

        /// <summary>The shower's fixture on <paramref name="host"/> (the stall itself, whose solid footprint it stands aside
        /// while the survivor showers): its points come from the stall's drawing (<see cref="FurnitureBuilder.ShowerStall"/>)
        /// and its key from where it stands.</summary>
        internal static ShowerFixture Create(HomeFurniture host)
        {
            Vector3 point = BathroomFixtures.Point(host, FurnitureBuilder.ShowerReach);
            var go = new GameObject("ShowerFixture");
            go.transform.SetParent(host.transform, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var s = go.AddComponent<ShowerFixture>();
            s._host = host;
            s._point = point;
            s._standIn = BathroomFixtures.Point(host, FurnitureBuilder.ShowerStandIn);
            s._stepOut = BathroomFixtures.Point(host, FurnitureBuilder.ShowerStepOut);
            s._middle = BathroomFixtures.Point(host, new Vector3(0f, 1.2f, 0f));
            s._stall = host.GetComponentInChildren<Collider>();
            s._key = KeyFor("shower", host.transform.position);
            return s;
        }

        /// <summary>Hands over the glass door (hinged to swing out), the mixer's lever (turned when on) and the spray's two
        /// looks, which alternate while it runs.</summary>
        internal void SetParts(FixturePart door, FixturePart lever, Renderer sprayA, Renderer sprayB)
        {
            _door = door;
            _lever = lever;
            _sprayA = sprayA;
            _sprayB = sprayB;
            if (_door != null) _door.SetOpen(_doorOpen, true);
            if (_lever != null) _lever.SetOpen(_on, true);
            ShowSpray(false, false);
        }

        private void Update()
        {
            if (_playerInteractor == null)
                _playerInteractor = PlayerInteractor.Current;
            
            if (Running)
            {
                if (Time.time >= _nextFlick)
                {
                    _nextFlick = Time.time + CloneZConfig.ShowerSprayFlicker;
                    _flick = !_flick;
                }
                ShowSpray(_flick, !_flick);
                if (Time.time >= _nextNoise)
                {
                    _nextNoise = Time.time + CloneZConfig.ShowerNoiseInterval;
                    MakeNoise(CloneZConfig.ShowerNoiseRadius);
                }
            }
            else ShowSpray(false, false);

            if (_closeDoorAt >= 0f && Time.time >= _closeDoorAt)
            {
                _closeDoorAt = -1f;
                if (_inside) SetDoor(false, false);   // pulled to behind you
            }
            Watch();
        }

        // Belt and braces: the survivor is never left in the stall with its collider stood aside once the shower's over
        // (however the job ended), nor shut inside it with the collider back (a save made mid-shower puts them there).
        private void Watch()
        {
            PlayerInteractor pi = _playerInteractor;
            if (_inside)
            {
                if (pi != null && pi.JobTarget == this) return;
                _inside = false;
                _closeDoorAt = -1f;
                if (pi != null) pi.Context.Teleport(ExitSpot(), _enteredYaw);
                SetStallSolid(true);
                _hasEntry = false;
                return;
            }
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.5f;
            if (pi != null && InStall(pi.transform.position)) pi.Context.Teleport(_stepOut);
        }

        private bool InStall(Vector3 p)
        {
            if (_host == null) return false;
            Vector3 l = BathroomFixtures.Local(_host, p);
            float half = FurnitureBuilder.ShowerInsideHalf;
            return Mathf.Abs(l.x) < half && Mathf.Abs(l.z) < half && l.y > -0.5f && l.y < 1.5f;
        }

        // Back where the survivor stood to step in, if that's still close by; else the clear spot in front of the stall.
        private Vector3 ExitSpot()
        {
            if (_hasEntry && _host != null && BathroomFixtures.Flat(_enteredFrom, _host.transform.position) < 2.5f) return _enteredFrom;
            return _stepOut;
        }

        private void SetStallSolid(bool solid)
        {
            if (_stall != null) _stall.enabled = solid;
        }

        private void ShowSpray(bool a, bool b)
        {
            if (_sprayA != null && _sprayA.enabled != a) _sprayA.enabled = a;
            if (_sprayB != null && _sprayB.enabled != b) _sprayB.enabled = b;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            var shower = new InteractAction("Take a shower", FinishShower)
                .Timed(CloneZConfig.ShowerSeconds, "Showering", HumanoidActivity.Use)
                .DisabledIf(!WaterMains.MainsOn, "The shower's dry \u2014 the water's been shut off");
            shower.Begin = StepIn;
            shower.Tick = WhileShowering;
            shower.Stopped = AbandonShower;
            into.Add(shower);
            into.Add(new InteractAction(_on ? "Turn off the shower" : "Turn on the shower", c => Turn(c, !_on)));
            if (_on && !_inside) into.Add(new InteractAction("Shut off shower", c => { Turn(c, false); SetDoor(false, true); }));
            into.Add(new InteractAction(_doorOpen ? "Close the shower door" : "Open the shower door", c => SetDoor(!_doorOpen, false)));
            into.Add(new InteractAction("Inspect shower", ctx =>
            {
                if (!WaterMains.MainsOn) ctx.Notify("The water’s shut off.");
                else if (!Running) ctx.Notify("The shower is off.");
                else ctx.Notify(Hot ? "Hot water is flowing." : "Cold water is flowing.");
            }));
            if (_inside)
            {
                into.Add(new InteractAction("Turn off water", c => Turn(c, false)));
            }
        }

        private void Turn(InteractContext ctx, bool on)
        {
            _turnedOnToShower = false;
            SetOn(on, false);
            if (!on) return;
            if (!WaterMains.MainsOn) ctx.Notify("The pipes knock and shudder, but nothing comes out. The water's off.");
            else if (!Hot) ctx.Notify("It runs cold and stays cold \u2014 no power for the water heater.");
        }

        private void SetOn(bool on, bool instant)
        {
            if (_on == on && !instant) return;
            _on = on;
            if (_lever != null) _lever.SetOpen(on, instant);
            if (!instant) MakeNoise(CloneZConfig.ShowerMixerNoise);
            if (!Running) ShowSpray(false, false);
        }

        private void SetDoor(bool open, bool instant)
        {
            if (_doorOpen == open && !instant) return;
            _doorOpen = open;
            if (_door != null) _door.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.ShowerDoorNoise);
        }

        // Into the stall: the water on (if it isn't), the door open, the stall's collider stood aside so there's room to
        // stand under the rose facing it, and the door pulled to behind you once you're in.
        private void StepIn(InteractContext ctx)
        {
            _enteredFrom = ctx.Position;
            _enteredYaw = ctx.Transform != null ? ctx.Transform.eulerAngles.y : 0f;
            _hasEntry = true;
            _underWater = 0f;
            _turnedOnToShower = !_on;
            SetOn(true, false);
            SetDoor(true, false);
            SetStallSolid(false);
            _inside = true;
            ctx.Teleport(_standIn, (_host != null ? _host.Yaw : 0f) + 180f);
            _closeDoorAt = Time.time + CloneZConfig.FixturePartSeconds + 0.15f;
        }

        private void WhileShowering(InteractContext ctx, float dt, float progress)
        {
if (!WaterMains.MainsOn)
        {
            ctx.Notify("The spray splutters and dies. The water's been shut off.");
            // Turn the shower off so it won't magically restart when the mains come back on
            SetOn(false, false);
            if (ctx.Interactor != null) ctx.Interactor.StopJob();
            return;
        }
            if (Running) _underWater += dt;
        }

        private void FinishShower(InteractContext ctx)
        {
            bool hot = Hot;
            PlayerStats s = ctx.Stats;
            if (s != null)
            {
                if (hot)
                {
                    s.Panic = Mathf.Max(0f, s.Panic - CloneZConfig.ShowerHotCalm);
                    s.Fatigue = Mathf.Clamp01(s.Fatigue - CloneZConfig.ShowerHotRest);
                }
                else
                {
                    s.Temperature = Mathf.Max(0f, s.Temperature - CloneZConfig.ShowerColdChill);
                    s.Panic = Mathf.Min(CloneZConfig.PanicMax, s.Panic + CloneZConfig.ShowerColdPanic);
                }
                s.Wetness = Mathf.Max(s.Wetness, CloneZConfig.ShowerWetness);
            }
            StepOut(ctx);
            ctx.Notify(hot ? "The hot water is bliss." : "Ice cold \u2014 the water heater's dead.");
        }

        // Stepping out before the end: a soaking (and, cold, a chill) for however long you stood under it.
        private void AbandonShower(InteractContext ctx)
        {
            PlayerStats s = ctx.Stats;
            if (s != null && _underWater >= CloneZConfig.ShowerSoakAfter)
            {
                float share = Mathf.Clamp01(_underWater / CloneZConfig.ShowerSeconds);
                s.Wetness = Mathf.Max(s.Wetness, CloneZConfig.ShowerWetness * Mathf.Max(0.5f, share));
                if (!Hot) s.Temperature = Mathf.Max(0f, s.Temperature - CloneZConfig.ShowerColdChill * share);
            }
            StepOut(ctx);
        }

        // Out of the stall: the water off again if the shower turned it on, the door open, the survivor back where they
        // stood, and only then the stall solid again.
        private void StepOut(InteractContext ctx)
        {
            if (_turnedOnToShower)
            {
                _turnedOnToShower = false;
                SetOn(false, false);
            }
            _closeDoorAt = -1f;
            SetDoor(true, false);
            if (_inside)
            {
                _inside = false;
                ctx.Teleport(ExitSpot(), _enteredYaw);
            }
            SetStallSolid(true);
            _hasEntry = false;
        }

        public override void CaptureState(FixtureSave save)
            => save.State = (_doorOpen ? 1 : 0) | (_on && !_turnedOnToShower ? 2 : 0) | 4;

        public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 4) == 0) return;
            _turnedOnToShower = false;
            _closeDoorAt = -1f;
            SetOn((save.State & 2) != 0, true);
            SetDoor((save.State & 1) != 0, true);
        }
    }
}
