using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A set that plays the broadcasts (<see cref="Broadcasts"/>) — the portable TV and the kitchen radio. Switched on, it
    /// plays whatever it's tuned to: a caption for each line the survivor can hear from where they are (one every few
    /// seconds while there's a programme, a hiss now and then once the station has gone off the air), and every few
    /// seconds a noise that carries as far as its volume — low, medium or loud. A set left blaring draws the dead.
    /// It's retuned round its dial and turned up or down a step at a time (the knobs need a hand on them; watching or
    /// listening doesn't), and watching or listening is an open-ended job that eases the nerves. A set that needs the
    /// mains dies when the power goes. Saves whether it's on, what it's tuned to and how loud.
    /// </summary>
    public abstract class LivingReceiver : Interactable
    {
        protected const string TooFar = "Get closer to reach the knobs";
        private const int MaxVolume = 2;

        private Transform _host;
        private Vector3 _point;
        private HomeFurnitureKind _supportKind;
        private Transform _support;
        private bool _supportFound;
        private int[] _cursors;
        private Vector3 _savePos;
        private float _noiseAt, _captionAt;

        /// <summary>Switched on (and, for a mains set, powered).</summary>
        public bool IsOn { get; private set; }

        /// <summary>Where on the dial it is (index into <see cref="Dial"/>).</summary>
        protected int Tuned { get; private set; }

        /// <summary>0 low, 1 medium, 2 loud.</summary>
        protected int Volume { get; private set; } = CloneZConfig.ReceiverDefaultVolume;

        /// <summary>The station it's tuned to.</summary>
        public BroadcastStation Station => Dial[Mathf.Clamp(Tuned, 0, Dial.Length - 1)];

        /// <summary>The furniture root of the set itself.</summary>
        protected Transform Host => _host;

        /// <summary>The stations round the dial, in order.</summary>
        protected abstract BroadcastStation[] Dial { get; }
        /// <summary>The kind in the save key ("tv", "radio").</summary>
        protected abstract string SaveKind { get; }
        /// <summary>The caption's source ("TV", "Radio"), followed by the station's place on the dial.</summary>
        protected abstract string SourceName { get; }
        /// <summary>How far (flat m) the survivor follows what's on at medium volume.</summary>
        protected abstract float CaptionRange { get; }
        /// <summary>False when a mains set has lost its power.</summary>
        protected virtual bool HasPower => true;
        /// <summary>Shows the set on or off (the picture, the dial light).</summary>
        protected abstract void ShowOn(bool on);
        /// <summary>Called every frame while it plays (a flickering picture).</summary>
        protected virtual void Animate(float dt) { }
        /// <summary>Called once the set has been retuned (or restored).</summary>
        protected virtual void Retuned() { }
        /// <summary>What the survivor is told when a mains set dies with the power (null: nothing).</summary>
        protected virtual string DeathNotice => null;

        public override Vector3 InteractPoint => _point;
        public override string SaveKey => KeyFor(SaveKind, _savePos);

        /// <summary>The set's own furniture, or the dresser / counter it stands on: neither blocks the reach.</summary>
        public override Transform OcclusionRoot
        {
            get
            {
                if (!_supportFound && _host != null)
                {
                    HomeFurniture under = LivingFixtures.Under(_host.position, _supportKind);
                    if (under != null)
                    {
                        _support = under.transform;
                        _supportFound = true;
                    }
                }
                if (_support != null) return _support;
                return _host != null ? _host : transform;
            }
        }

        /// <summary>Sets the set up on <paramref name="host"/> (its furniture root), its knobs at <paramref name="point"/>,
        /// standing on furniture of <paramref name="support"/> kind — or, <paramref name="freeStanding"/>, on a stand of its own
        /// that's part of the host (nothing else to look for under it).</summary>
        protected void Init(Transform host, Vector3 point, HomeFurnitureKind support, bool freeStanding = false)
        {
            _host = host;
            _point = point;
            _supportKind = support;
            _supportFound = freeStanding;
            _cursors = new int[Dial.Length];
            _savePos = (_host != null ? _host.position : transform.position);
            IsOn = false;
            ShowOn(false);
        }

        protected virtual void Update()
        {
            if (!IsOn) return;
            if (!HasPower)
            {
                PowerFailed();
                return;
            }
            float now = Time.time;
            if (now >= _noiseAt)
            {
                _noiseAt = now + CloneZConfig.ReceiverNoiseInterval;
                MakeNoise(CloneZConfig.ReceiverNoiseRadius[Volume]);
            }
            if (now >= _captionAt) Caption(now);
            Animate(Time.deltaTime);
        }

        #region Knobs
        /// <summary>True when the survivor stands close enough to work the knobs.</summary>
        protected bool KnobsInReach(InteractContext ctx) => LivingFixtures.FlatDistance(ctx.Position, _point) <= CloneZConfig.ReceiverKnobReach;

        /// <summary>Switches the set on or off.</summary>
        protected void Switch(bool on)
        {
            if (IsOn == on) return;
            IsOn = on;
            ShowOn(on);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            if (on) Freshen();
        }

        /// <summary>Turns the dial on to the next station; returns it.</summary>
        protected BroadcastStation TuneNext()
        {
            Tuned = (Tuned + 1) % Dial.Length;
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            Retuned();
            if (IsOn) _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;
            return Station;
        }

        /// <summary>Turns the dial on to the previous station; returns it.</summary>
        protected BroadcastStation TunePrevious()
        {
            Tuned = (Tuned - 1 + Dial.Length) % Dial.Length;
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            Retuned();
            if (IsOn) _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;
            return Station;
        }

        /// <summary>Turns it up (+1) or down (-1) a step: the next noise goes out at the new loudness at once.</summary>
        protected void Turn(int step)
        {
            Volume = Mathf.Clamp(Volume + step, 0, MaxVolume);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _noiseAt = Time.time;
        }

        protected bool AtLoudest => Volume >= MaxVolume;
        protected bool AtQuietest => Volume <= 0;

        /// <summary>"low", "medium" or "loud".</summary>
        protected string VolumeWord => Volume <= 0 ? "low" : Volume >= MaxVolume ? "loud" : "medium";

        /// <summary>"Channel 7 (Channel 7)" style: where it is on the dial and what it calls itself.</summary>
        protected static string Describe(BroadcastStation s)
        {
            string name = Broadcasts.NameOf(s), title = Broadcasts.TitleOf(s);
            return name == title ? name : name + " \u2014 " + title;
        }
        #endregion

        #region Watching and listening
        /// <summary>An open-ended job watching / listening to the set: the nerves ease while it plays (much less once
        /// the station is only static); it ends when the set goes off.</summary>
        protected InteractAction PassTime(string label, string busy, string offReason)
        {
            var a = new InteractAction(label).Timed(-1f, busy, HumanoidActivity.None).DisabledIf(!IsOn, offReason);
            a.Tick = (c, dt, elapsed) =>
            {
                if (!IsOn)
                {
                    if (c.Interactor != null) c.Interactor.StopJob();
                    return;
                }
                Soothe(c, dt);
            };
            return a;
        }

        /// <summary>Eases the survivor's nerves for <paramref name="dt"/> seconds of attention to the set (nothing while it's off).</summary>
        public void Soothe(InteractContext ctx, float dt)
        {
            if (!IsOn || ctx.Stats == null || dt <= 0f) return;
            float k = Broadcasts.IsOnAir(Station) ? 1f : CloneZConfig.DeadAirReliefFactor;
            ctx.Stats.Panic = Mathf.Max(0f, ctx.Stats.Panic - CalmPerSecond * k * dt);
        }

        /// <summary>Panic shed per second of watching / listening.</summary>
        protected abstract float CalmPerSecond { get; }
        #endregion

        #region Playing
        // The next caption is due: one line of the programme if the survivor can hear it from where they are.
        private void Caption(float now)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null || !InEarshot(pi))
            {
                _captionAt = now + 1f;
                return;
            }
            BroadcastStation st = Station;
            bool onAir = Broadcasts.IsOnAir(st);
            string line = Broadcasts.NextLine(st, ref _cursors[Mathf.Clamp(Tuned, 0, _cursors.Length - 1)]) ?? "*static*";
            Subtitles.Show(SourceName + " \u2014 " + Broadcasts.NameOf(st), line, Broadcasts.CaptionColour(st), CloneZConfig.ReceiverCaptionSeconds);
            _captionAt = now + (onAir
                ? Random.Range(CloneZConfig.ReceiverCaptionMin, CloneZConfig.ReceiverCaptionMax)
                : Random.Range(CloneZConfig.ReceiverStaticCaptionMin, CloneZConfig.ReceiverStaticCaptionMax));
        }

        // Awake, alive and close enough for what's on to carry (farther the louder it plays).
        private bool InEarshot(PlayerInteractor pi)
        {
            InteractContext ctx = pi.Context;
            if (ctx != null && ctx.Stats != null && ctx.Stats.IsDead) return false;
            if (ctx != null && ctx.Sleep != null && ctx.Sleep.IsSleeping) return false;
            float range = CaptionRange * CloneZConfig.ReceiverHearingScale[Volume];
            return LivingFixtures.FlatDistance(pi.transform.position, _point) <= range;
        }

        private void PowerFailed()
        {
            IsOn = false;
            ShowOn(false);
            string notice = DeathNotice;
            PlayerInteractor pi = PlayerInteractor.Current;
            if (notice != null && pi != null && LivingFixtures.FlatDistance(pi.transform.position, _point) <= CloneZConfig.TvDeathNoticeRange)
                pi.Notify(notice);
        }

        // Just switched on (or restored playing): the first caption soon, the first noise a moment later.
        private void Freshen()
        {
            _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption;
            _noiseAt = Time.time + 0.5f;
        }
        #endregion

        #region Persistence
        public override void CaptureState(FixtureSave save)
        {
            save.State = IsOn ? 1 : 0;
            save.A = Tuned;
            save.B = Volume;
        }

        public override void RestoreState(FixtureSave save)
        {
            // Ensure the cursor array is valid (it may be null if RestoreState runs before Init)
            if (_cursors == null || _cursors.Length != Dial.Length)
                _cursors = new int[Dial.Length];
            Tuned = Mathf.Clamp(Mathf.RoundToInt(save.A), 0, Dial.Length - 1);
            Volume = Mathf.Clamp(Mathf.RoundToInt(save.B), 0, MaxVolume);
            IsOn = (save.State & 1) != 0 && HasPower;
            Retuned();
            ShowOn(IsOn);
            if (IsOn) Freshen();
        }
        #endregion
    }
}
