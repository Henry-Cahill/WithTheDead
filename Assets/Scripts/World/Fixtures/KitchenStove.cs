using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The gas range's controls — the fixture on the kitchen's <see cref="CookingStation"/>. It burns gas, so it carries
    /// on when the power has gone. Each of the four burners lights and goes out on its own (its knob turns, a ring of
    /// blue flame flickers over it and the hob throws a warm glow round the kitchen); the oven heats up behind its
    /// window and its door drops open; and "Cook something" gets the ingredients out (the cooking itself happens in the
    /// inventory panel, which works anywhere near a stove). Anything lit warms the room round it a little
    /// (<see cref="IWarmthSource"/>, see <see cref="HomeComfort"/>). Saves the burners, the oven and its door.
    /// </summary>
    [DisallowMultipleComponent]
    public class KitchenStove : Interactable, IWarmthSource
    {
        private const float DoorAngle = 85f;          // the oven door drops nearly flat
        private const float KnobTurn = 90f;           // a burner knob turned to full
        private const float OvenKnobTurn = 135f;      // the thermostat turned up to a baking heat
        private static readonly Color FlameGlow = new Color(1f, 0.58f, 0.26f);

        // The burners as the cook standing at the range sees them (see FurnitureBuilder.StoveBurners).
        private static readonly string[] LightLabels =
        {
            "Light the front-left burner", "Light the front-right burner", "Light the back-left burner", "Light the back-right burner",
        };
        private static readonly string[] OffLabels =
        {
            "Turn off the front-left burner", "Turn off the front-right burner", "Turn off the back-left burner", "Turn off the back-right burner",
        };

        private Color _enamel;
        private Transform _fittings;          // everything built onto the range (built again should it ever be moved)
        private Vector3 _builtAt;
        private readonly FixturePart[] _knobs = new FixturePart[4];
        private readonly MeshRenderer[] _flames = new MeshRenderer[4];
        private FixturePart _ovenKnob, _ovenDoor;
        private MeshRenderer _window, _element;
        private RoomLight _glow;
        private float _flickerSeed;

        private int _lit;                     // bit i: burner i is lit
        private bool _oven, _doorOpen;

        private readonly Action<InteractContext>[] _toggleBurner = new Action<InteractContext>[4];
        private Action<InteractContext> _allOn, _allOff, _toggleOven, _toggleDoor, _cook;

        public override string DisplayName => "Gas stove";
        public override Vector3 InteractPoint => transform.position + transform.rotation * new Vector3(0f, 0.95f, 0.2f);
        public override string SaveKey => KeyFor("gasstove", transform.position);

        #region IWarmthSource
        public Vector3 WarmthPosition => transform.position + new Vector3(0f, 0.9f, 0f);
        public float WarmthRadius => CloneZConfig.StoveWarmthRadius;

        public float Warmth
        {
            get
            {
                float w = LitCount * CloneZConfig.StoveBurnerWarmth;
                if (_oven) w += CloneZConfig.StoveOvenWarmth + (_doorOpen ? CloneZConfig.StoveOvenOpenWarmth : 0f);
                return Mathf.Min(w, CloneZConfig.ComfortWarmthMax);
            }
        }
        #endregion

        private int LitCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < 4; i++) if ((_lit & (1 << i)) != 0) n++;
                return n;
            }
        }

        /// <summary>Fits the controls to the gas range <paramref name="range"/> (its body already drawn by
        /// <see cref="FurnitureBuilder.GasRange"/> in <paramref name="enamel"/>): the knobs, the flames, the oven door and
        /// its window, the oven's element and the hob's glow. Fresh: everything off, the oven door shut.</summary>
        internal static KitchenStove Attach(GameObject range, Color enamel)
        {
            var s = range.AddComponent<KitchenStove>();
            s._enamel = enamel;
            s._flickerSeed = UnityEngine.Random.value * 50f;
            s.BuildFittings();
            return s;
        }

        private void Awake()
        {
            for (int i = 0; i < 4; i++)
            {
                int burner = i;
                _toggleBurner[i] = c => SetBurner(burner, (_lit & (1 << burner)) == 0, true);
            }
            _allOn = c => { for (int i = 0; i < 4; i++) SetBurner(i, true, false); };
            _allOff = c => { for (int i = 0; i < 4; i++) SetBurner(i, false, false); };
            _toggleOven = ToggleOven;
            _toggleDoor = c => SetDoor(!_doorOpen, false);
            _cook = Cook;
        }

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

        #region Building
        private void BuildFittings()
        {
            if (_fittings != null) Destroy(_fittings.gameObject);
            Vector3 o = transform.position;
            _builtAt = o;
            // The range faces the way its root is turned (a raid kitchen's stove stands against any wall).
            var f = new KitchenFrame(o, transform.eulerAngles.y);
            _fittings = new GameObject("StoveFittings").transform;
            _fittings.SetParent(transform, false);
            _fittings.SetPositionAndRotation(o, Quaternion.identity);

            // The oven door drops open about its bottom edge; the window's glow rides on it.
            Color enamel = _enamel;
            var hinge = new Vector3(0f, FurnitureBuilder.StoveDoorHingeY, FurnitureBuilder.StoveDoorFront);
            _ovenDoor = KitchenParts.Door(_fittings, "OvenDoor", f, d => FurnitureBuilder.OvenDoor(d, enamel), hinge, Vector3.up, Vector3.forward, DoorAngle);
            _window = KitchenParts.PieceInFrame(_ovenDoor.transform, "OvenWindow", f, hinge, FurnitureBuilder.OvenWindowGlow);
            _element = KitchenParts.PieceInFrame(_fittings, "OvenElement", f, Vector3.zero, FurnitureBuilder.OvenHeatGlow);

            // A knob on the back-guard and a ring of flame for each burner, and the oven's thermostat.
            for (int i = 0; i < 4; i++)
            {
                _knobs[i] = Knob(f, "BurnerKnob" + i, new Vector3(FurnitureBuilder.StoveKnobX[i], FurnitureBuilder.StoveKnobY, FurnitureBuilder.StoveKnobZ), KnobTurn);
                _flames[i] = KitchenParts.PieceAt(_fittings, "BurnerFlame" + i, f, FurnitureBuilder.StoveBurners[i], FurnitureBuilder.BurnerFlame);
            }
            _ovenKnob = Knob(f, "OvenKnob", FurnitureBuilder.StoveOvenKnob, OvenKnobTurn);

            // The flames' warm light on the wall, the worktops and the cook.
            _glow = RoomLight.Spawn(_fittings, f.P(0f, 1.15f, 0.1f), FlameGlow, CloneZConfig.StoveGlowRange, CloneZConfig.StoveGlowIntensity,
                false, false);
            _glow.ShowBulb = false;
            _glow.On = false;

            ApplyAll();
        }

        private FixturePart Knob(in KitchenFrame f, string name, Vector3 at, float turn)
        {
            Vector3 p = f.P(at);
            var m = new RaidMesh();
            m.SetFrame(p, f.Yaw);
            FurnitureBuilder.StoveKnob(m);
            m.ResetFrame();
            return FixturePart.Hinge(_fittings, name, m, p, f.D(Vector3.back), turn).WithSpeed(0.2f);
        }
        #endregion

        private void Update()
        {
            // An old save's stove is carried to its place in the furnished home after it's built: fit it out again there.
            if ((transform.position - _builtAt).sqrMagnitude > 1e-6f) BuildFittings();
            if (_lit == 0) return;

            // Gas flames never burn quite steady.
            float t = Time.time * 8f;
            for (int i = 0; i < 4; i++)
            {
                if ((_lit & (1 << i)) == 0 || _flames[i] == null) continue;
                float k = Mathf.PerlinNoise(t + i * 3.7f, _flickerSeed);
                _flames[i].transform.localScale = new Vector3(1f, 0.75f + 0.5f * k, 1f);
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            for (int i = 0; i < 4; i++)
                into.Add(new InteractAction((_lit & (1 << i)) != 0 ? OffLabels[i] : LightLabels[i], _toggleBurner[i]));
            // Light all burners if any are off
            if ((_lit & 0xF) != 0xF) // not all burners lit
                into.Add(new InteractAction("Light all burners", _allOn));
            if (_lit != 0) into.Add(new InteractAction("Turn off all the burners", _allOff));
            into.Add(new InteractAction(_oven ? "Turn off the oven" : "Turn on the oven", _toggleOven));
            into.Add(new InteractAction(_doorOpen ? "Close the oven door" : "Open the oven door", _toggleDoor));
            into.Add(new InteractAction("Cook something", _cook));
        }

        #region The controls
        private void SetBurner(int i, bool on, bool noisy)
        {
            int bit = 1 << i;
            if (((_lit & bit) != 0) == on) return;
            _lit = on ? _lit | bit : _lit & ~bit;
            if (_knobs[i] != null) _knobs[i].SetOpen(on);
            if (_flames[i] != null) _flames[i].enabled = on;
            if (on && noisy) MakeNoise(CloneZConfig.FixtureNoiseSmall);   // the igniter's ticking
            RefreshGlow();
        }

        private void ToggleOven(InteractContext ctx)
        {
            SetOven(!_oven, false);
            if (_oven) ctx.Notify("The oven ticks and catches with a soft whump. It'll take a while to heat through.");
        }

        private void SetOven(bool on, bool instant)
        {
            _oven = on;
            if (_ovenKnob != null) _ovenKnob.SetOpen(on, instant);
            if (on && !instant) MakeNoise(CloneZConfig.FixtureNoiseSmall);
            RefreshOven();
        }

        private void SetDoor(bool open, bool instant)
        {
            _doorOpen = open;
            if (_ovenDoor != null) _ovenDoor.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.FixtureNoiseSmall);
            RefreshOven();
        }

        // You can't cook without a flame: light one if nothing's lit, then get out what there is to cook.
        private void Cook(InteractContext ctx)
        {
            if (_lit == 0 && !_oven) SetBurner(0, true, true);
            if (ctx.Interactor != null) ctx.Interactor.OpenInventory();
        }
        #endregion

        #region Looks
        private void ApplyAll()
        {
            for (int i = 0; i < 4; i++)
            {
                bool on = (_lit & (1 << i)) != 0;
                if (_knobs[i] != null) _knobs[i].SetOpen(on, true);
                if (_flames[i] != null) _flames[i].enabled = on;
            }
            if (_ovenKnob != null) _ovenKnob.SetOpen(_oven, true);
            if (_ovenDoor != null) _ovenDoor.SetOpen(_doorOpen, true);
            RefreshOven();
        }

        // The oven's element glows while it's on (seen with the door open); its window glows while the door is shut.
        private void RefreshOven()
        {
            if (_element != null) _element.enabled = _oven;
            if (_window != null) _window.enabled = _oven && !_doorOpen;
            RefreshGlow();
        }

        // The hob's warm light, while a burner burns or the hot oven stands open.
        private void RefreshGlow()
        {
            if (_glow != null) _glow.On = _lit != 0 || (_oven && _doorOpen);
        }
        #endregion

        #region Persistence
        public override void CaptureState(FixtureSave save) => save.State = (_lit & 0xF) | (_oven ? 16 : 0) | (_doorOpen ? 32 : 0);

        public override void RestoreState(FixtureSave save)
        {
            _lit = save.State & 0xF;
            _oven = (save.State & 16) != 0;
            _doorOpen = (save.State & 32) != 0;
            ApplyAll();
        }
        #endregion
    }
}
