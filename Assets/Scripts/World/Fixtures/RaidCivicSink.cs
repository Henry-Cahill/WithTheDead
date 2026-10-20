using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The deep steel sink let into a workplace counter — the National Guard post's kitchenette, the clinic's examination
    /// room (see <see cref="RaidBuilding.SteelCounter"/>). The cold tap turns on and off: a stream of water runs from the
    /// gooseneck into the bowl and a running tap is heard a little way off; it runs while the mains do
    /// (<see cref="WaterMains"/>) and splutters dry by itself once they're shut off. It's an <see cref="IWaterPoint"/>, so G
    /// drinks and fills a bottle here too. And the everyday things: wash your hands and face — or, at the clinic, scrub up
    /// properly — and rinse your wounds, each running the tap for as long as it takes. There's no plug: what runs, runs
    /// away. Saves whether the tap's left running.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicSink : Interactable, IWaterPoint
    {
        private const float TapTurn = 45f;
        private const float StreamRadius = 0.007f;
        private const string Dry = "The tap is dry \u2014 the water's been shut off";

        private static readonly string[] ScrubLines =
        {
            "Soap to the elbows, a full minute under the tap, nails and all. Whoever trained here would approve.",
"You scrub up the way the poster over the sink tells you to, step by step. Your hands come out pink and stinging-clean.",
            "You run icy water over your hands, the frosted metal of the tap biting your fingertips.",
            "A rag of homemade lye soap scrapes across your knuckles, leaving a sharp, chemical scent.",
            "The clank of the metal faucet echoes in the cramped clinic as water splashes into the basin.",
            "A faint whiff of bleach drifts from the disinfectant bottle beside the sink, stinging your nose.",
            "Your breath forms clouds in the cold air, and the water leaves a thin film you scrape off with a rough towel.",
            "You recall the faded poster above warning of proper hand decontamination, its cracked letters guiding each motion.",

        };

        private string _name, _key;
        private bool _clinical;
        private Transform _host;
        private Vector3 _point, _spout, _drain;
        private FixturePart _tapHead;
        private MeshRenderer _streamR, _splashR;
        private Transform _stream, _splash;
        private bool _running, _openedForJob;
        private float _drop, _nextNoise, _nextCheck, _wobbleSeed;
        private int _scrub;

        private Action<InteractContext> _toggleTap, _wash, _rinse, _openForJob, _endJob;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _host != null ? _host : transform;
        public override string SaveKey => _key;

        #region IWaterPoint
        /// <summary>
        /// Position of the water spout, used by IWaterPoint consumers to know where water originates.
        /// </summary>
        public Vector3 WaterPosition => _spout;
                /// <summary>
        /// UI label for this water source.
        /// </summary>
        public string WaterLabel => "tap";
                /// <summary>
        /// Indicates if the water mains are currently on.
        /// </summary>
        public bool HasWater => WaterMains.MainsOn;
                /// <summary>
        /// Whether this water source is contaminated.
        /// </summary>
        public bool IsTainted => false;
                /// <summary>
        /// Message shown when the water mains are off.
        /// </summary>
        public string DryMessage => Dry;
                /// <summary>
        /// No-op implementation required by IWaterPoint to indicate water can be drawn.
        /// </summary>
        public void DrawWater() { }   // mains water, as much as you like while it runs
        #endregion

        /// <summary>Fits the sink drawn by <see cref="FurnitureBuilder.SteelCounterBody"/> at <paramref name="sinkX"/> along the
        /// counter <paramref name="host"/> (whose frame is <paramref name="f"/>): the turning cold tap, the stream and its
        /// splash. A <paramref name="clinical"/> sink is for scrubbing up.</summary>
        internal static CivicSink Create(Transform host, in KitchenFrame f, float sinkX, string name, bool clinical)
        {
            var bowl = new Vector3(sinkX, FurnitureBuilder.SteelDeckY + 0.02f, FurnitureBuilder.SteelSinkZ + 0.1f);
            var go = new GameObject("Sink_" + name);
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(f.P(bowl), Quaternion.identity);
            var s = go.AddComponent<CivicSink>();
            s._host = host;
            s._name = name;
            s._clinical = clinical;
            s._point = f.P(bowl);
            s._key = KeyFor("civicsink", s._point);
            s._wobbleSeed = UnityEngine.Random.value * 100f;

            Vector3 spout = FurnitureBuilder.SteelSpout + new Vector3(sinkX, 0f, 0f);
            s._spout = f.P(spout);
            s._drain = f.P(new Vector3(spout.x, FurnitureBuilder.SteelSinkBottom + 0.001f, spout.z));

            Vector3 head = f.P(FurnitureBuilder.SteelColdTap + new Vector3(sinkX, 0f, 0f));
            var hm = new RaidMesh();
            hm.SetFrame(head, f.Yaw);
            FurnitureBuilder.SinkTapHead(hm);
            hm.ResetFrame();
            s._tapHead = FixturePart.Hinge(go.transform, "ColdTap", hm, head, Vector3.up, -TapTurn).WithSpeed(0.25f);

            Surf water = KitchenParts.Water;
            s._streamR = KitchenParts.PieceAt(go.transform, "TapWater", f, spout, d => FurnitureBuilder.SinkWaterColumn(d, water));
            s._splashR = KitchenParts.PieceAt(go.transform, "TapSplash", f, new Vector3(spout.x, FurnitureBuilder.SteelSinkBottom + 0.001f, spout.z),
                d => FurnitureBuilder.SinkSplash(d, water));
            s._stream = s._streamR != null ? s._streamR.transform : null;
            s._splash = s._splashR != null ? s._splashR.transform : null;
            s.Refresh(true);
            return s;
        }

        private void Awake()
        {
            _toggleTap = ToggleTap;
            _wash = Wash;
            _rinse = Rinse;
            _openForJob = OpenForJob;
            _endJob = EndJob;
        }

        private void Update()
        {
            if (!_running) return;
            float now = Time.time;
            if (now >= _nextCheck)
            {
                _nextCheck = now + CloneZConfig.KitchenStatusInterval;
                if (!WaterMains.MainsOn)
                {
                    _openedForJob = false;
                    SetRunning(false, false);
                    KitchenParts.NotifyNear(_point, CloneZConfig.KitchenHearRange, "The tap splutters and runs dry");
                    return;
                }
            }
            if (now >= _nextNoise)
            {
                _nextNoise = now + CloneZConfig.KitchenTapNoiseInterval;
                MakeNoise(CloneZConfig.KitchenTapNoiseRadius);
            }
            float a = Mathf.PerlinNoise(now * 7.3f, _wobbleSeed), b = Mathf.PerlinNoise(_wobbleSeed, now * 6.1f);
            if (_stream != null) _stream.localScale = new Vector3(StreamRadius * (0.85f + 0.3f * a), _drop, StreamRadius * (0.85f + 0.3f * b));
            if (_splash != null) _splash.localScale = Vector3.one * (0.8f + 0.4f * b);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_running ? "Turn off the tap" : "Turn on the tap", _toggleTap));
            WaterUse.AddDrinkAndFill(ctx, this, into);
            bool mains = WaterMains.MainsOn;

            var wash = _clinical
                ? new InteractAction("Scrub up", _wash).Timed(CloneZConfig.CivicSinkScrubSeconds, "Scrubbing up")
                : new InteractAction("Wash your hands and face", _wash).Timed(CloneZConfig.CivicSinkWashSeconds, "Washing your hands and face");
            wash.DisabledIf(!mains, Dry);
            wash.Begin = _openForJob;
            wash.Stopped = _endJob;
            into.Add(wash);

            int open = OpenWounds(ctx);
            var rinse = new InteractAction("Rinse your wounds", _rinse)
                .Timed(CloneZConfig.CivicSinkRinseSeconds, "Rinsing your wounds")
                .DisabledIf(open == 0, "You've no open wounds to rinse")
                .DisabledIf(open > 0 && !mains, Dry);
            rinse.Begin = _openForJob;
            rinse.Stopped = _endJob;
            into.Add(rinse);

            var temp = new InteractAction("Check water temperature", ctx => { ctx.Notify(PowerGrid.HousePowered ? "The water feels warm." : "The water is icy‑cold."); })
                .DisabledIf(!WaterMains.MainsOn, Dry);
            into.Add(temp);
        }

        #region The tap
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
                ctx.Notify("The pipes knock and shudder, and nothing comes. The water's off.");
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
                _nextNoise = Time.time + CloneZConfig.KitchenTapNoiseInterval;
                if (!instant) MakeNoise(CloneZConfig.KitchenTapNoiseRadius);
            }
            Refresh(false);
        }

        // Shows the stream from the spout down to the bowl's floor while the tap runs.
        private void Refresh(bool first)
        {
            if (first && _tapHead != null) _tapHead.SetOpen(_running, true);
            if (_streamR != null)
            {
                _streamR.enabled = _running;
                _drop = Mathf.Max(0.02f, _stream.position.y - _drain.y);
                _stream.localScale = new Vector3(StreamRadius, _drop, StreamRadius);
            }
            if (_splashR != null) _splashR.enabled = _running;
        }

        // A wash runs the tap while it lasts (when it isn't running already), and turns it off after.
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
        #endregion

        #region Washing
        private void Wash(InteractContext ctx)
        {
            EndJob(ctx);
            if (!WaterMains.MainsOn)
            {
                ctx.Notify(Dry);
                return;
            }
            WaterUse.Freshen(ctx, _clinical ? 1.2f : 1f);
            if (_clinical) ctx.Notify(ScrubLines[_scrub++ % ScrubLines.Length]);
            else ctx.Notify(PowerGrid.HousePowered
                ? "The water runs hot after a moment. You scrub your hands with the gritty soap and splash your face. Better."
                : "Cold water, hard soap. You scrub your hands and splash your face. It wakes you up.");
        }

        private void Rinse(InteractContext ctx)
        {
            EndJob(ctx);
            if (!WaterMains.MainsOn)
            {
                ctx.Notify(Dry);
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
                ctx.Notify("You hold the cuts under the cold water until it runs clear. It stings, but they're cleaner for it.");
            else if (rinsed > 0)
                ctx.Notify("The fresh cuts rinse clean \u2014 but the hot, swollen one is past anything water can do. It needs antibiotics.");
            else if (festering > 0)
                ctx.Notify("Water does nothing for the hot, angry swelling. That needs antibiotics, not a rinse.");
            else
                ctx.Notify("There's nothing left open to rinse.");
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
        #endregion

        public override void CaptureState(FixtureSave save) => save.State = _running && !_openedForJob ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            _openedForJob = false;
            SetRunning((save.State & 1) != 0, true);
        }
    }
}
