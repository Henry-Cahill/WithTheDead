using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The portable TV on the bedroom dresser (see <see cref="LivingReceiver"/>): it runs off the mains, so it needs the
    /// house powered to come on and dies when the power goes (or the main breaker is thrown). On, its picture glows —
    /// tinted by the channel, its brightness shifting as the picture changes, a grey shimmer of snow once the channel
    /// has gone off the air — and casts a small, bluish, flickering light over the room. Three channels, three volume
    /// steps; "Watch TV" settles the nerves, and the edge of the bed is close enough to watch from. The rabbit ears can
    /// be fiddled with, and the set given a thump.
    /// </summary>
    [DisallowMultipleComponent]
    public class TvFixture : LivingReceiver
    {
        /// <summary>The cold blue of a cathode-ray tube's light on a dark room.</summary>
        public static readonly Color LightColour = new Color(0.58f, 0.70f, 1f);

        private static readonly List<TvFixture> s_Sets = new List<TvFixture>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer _picture;
        private RoomLight _light;
        private MaterialPropertyBlock _block;
        private Color _tint = Color.white;
        private bool _onAir = true;
        private float _bright = 1f, _brightTarget = 1f, _flickerAt, _airCheckAt;
        private float _thumpCalmAt;
        private float _reach;   // a set across a bigger room reaches farther (0: the usual)
        private string _name;

        public override string DisplayName => _name ?? "Portable TV";
        public override float Reach => _reach > 0f ? _reach : CloneZConfig.TvReach;
        protected override BroadcastStation[] Dial => Broadcasts.TvChannels;
        protected override string SaveKind => "tv";
        protected override string SourceName => "TV";
        protected override float CaptionRange => CloneZConfig.TvCaptionRange;
        protected override float CalmPerSecond => CloneZConfig.WatchTvPanicEasePerSec;
        protected override bool HasPower => PowerGrid.HousePowered;

        protected override string DeathNotice
        {
            get
            {
                string why = PowerGrid.NoPowerReason;
                return "The TV dies \u2014 the picture shrinks to a bright dot and fades" + (why != null ? ". " + why + "." : ".");
            }
        }

        /// <summary>Builds the TV's fixture on <paramref name="host"/> (the TV's furniture root): <paramref name="screen"/>
        /// is the middle of the glass (world), <paramref name="facing"/> the way the screen faces, <paramref name="picture"/>
        /// the lit picture's renderer and <paramref name="light"/> the glow it casts on the room. A set in a bigger room
        /// (a raid house's living room) may reach farther than the bedroom's — <paramref name="reach"/>, so it can be
        /// watched from the sofa — go by its own <paramref name="displayName"/>, and stand on a stand of its own
        /// (<paramref name="freeStanding"/>) rather than on a dresser.</summary>
        internal static TvFixture Create(Transform host, Vector3 screen, Vector3 facing, Renderer picture, RoomLight light,
            float reach = 0f, string displayName = null, bool freeStanding = false)
        {
            var go = new GameObject("Fixture_TV");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(screen, Quaternion.LookRotation(facing, Vector3.up));
            var tv = go.AddComponent<TvFixture>();
            tv._picture = picture;
            tv._light = light;
            tv._block = new MaterialPropertyBlock();
            tv._reach = reach;
            tv._name = displayName;
            tv.Init(host, screen, HomeFurnitureKind.Dresser, freeStanding);
            return tv;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!s_Sets.Contains(this)) s_Sets.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_Sets.Remove(this);
        }

        /// <summary>The nearest TV that's playing within <paramref name="range"/> (flat) of <paramref name="point"/>, or null.</summary>
        public static TvFixture PlayingNear(Vector3 point, float range)
        {
            TvFixture best = null;
            float bestD = range;
            for (int i = 0; i < s_Sets.Count; i++)
            {
                TvFixture tv = s_Sets[i];
                if (tv == null || !tv.IsOn || !tv.IsAvailable) continue;
                float d = LivingFixtures.FlatDistance(tv.InteractPoint, point);
                if (d <= bestD) { bestD = d; best = tv; }
            }
            return best;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool near = KnobsInReach(ctx);
            if (!IsOn)
            {
                into.Add(new InteractAction("Turn on the TV", TurnOn)
                    .DisabledIf(!near, TooFar)
                    .DisabledIf(!PowerGrid.HousePowered, PowerGrid.NoPowerReason));
                return;
            }
            into.Add(PassTime("Watch TV", "Watching TV", "The TV is off"));
            into.Add(new InteractAction("Turn off the TV", TurnOff).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Change the channel", ChangeChannel).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn the volume up", c => Louder(c, 1))
                .DisabledIf(AtLoudest, "It won't go any louder")
                .DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn the volume down", c => Louder(c, -1))
                .DisabledIf(AtQuietest, "It's down as low as it goes")
                .DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Adjust the rabbit ears", AdjustAerial).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Check volume", ctx => ctx.Notify("Volume: " + VolumeWord)).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Check signal", CheckSignal).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Give the set a thump", Thump).DisabledIf(!near, TooFar));
        }

        private void TurnOn(InteractContext ctx)
        {
            if (!PowerGrid.HousePowered)
            {
                ctx.Notify(PowerGrid.NoPowerReason);
                return;
            }
            Switch(true);
            ctx.Notify("The set warms up with a whine. " + Describe(Station) + (_onAir ? "." : " \u2014 nothing but snow."));
        }

        private void TurnOff(InteractContext ctx) => Switch(false);

        private void ChangeChannel(InteractContext ctx)
        {
            BroadcastStation s = TuneNext();
            // Immediately update channel state and picture tint
            RefreshChannel();
            if (IsOn) Paint();
            ctx.Notify(Describe(s) + (_onAir ? "" : " \u2014 nothing but snow"));
        }

        private void Louder(InteractContext ctx, int step)
        {
            Turn(step);
            ctx.Notify("Volume: " + VolumeWord + (Volume >= 2 ? ". The dead will hear it too." : "."));
        }

        private void AdjustAerial(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            ctx.Notify(_onAir
                ? "You walk the rabbit ears round until the ghosting clears. Mostly."
                : "You point the rabbit ears every which way. There's nothing left out there to pick up.");
        }

        private void CheckSignal(InteractContext ctx) { ctx.Notify(_onAir ? "The channel is clear." : "No signal – just snow."); }

        private void Thump(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.TvThumpNoise);
            _bright = 0.2f;   // the picture jumps
            if (_onAir && Time.time >= _thumpCalmAt)
            {
                _thumpCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify("You give the set a thump on the side. The picture rolls, then settles. Strangely satisfying.");
            }
            else ctx.Notify(_onAir ? "Another thump. The picture is exactly as good as it was." : "You thump the set. The snow doesn't care.");
        }

        #region The picture
        protected override void ShowOn(bool on)
        {
            if (_picture != null) _picture.enabled = on;
            if (_light != null) _light.On = on;
            if (!on) return;
            RefreshChannel();
            _bright = _brightTarget = 1f;
            Paint();
        }

        protected override void Retuned()
        {
            RefreshChannel();
            _bright = 0.15f;   // a blink as the picture comes in
            if (IsOn) Paint();
        }

        protected override void Animate(float dt)
        {
            float now = Time.time;
            if (now >= _airCheckAt) RefreshChannel();
            if (now >= _flickerAt)
            {
                // A programme's picture brightens and dims with the scene; snow just seethes.
                _flickerAt = now + (_onAir ? CloneZConfig.TvFlickerInterval * Random.Range(0.6f, 1.8f) : 0.05f);
                _brightTarget = _onAir ? Random.Range(0.62f, 1f) : Random.Range(0.45f, 0.85f);
            }
            _bright = Mathf.MoveTowards(_bright, _brightTarget, dt * (_onAir ? 2.5f : 14f));
            Paint();
        }

        private void RefreshChannel()
        {
            _airCheckAt = Time.time + 1f;
            _onAir = Broadcasts.IsOnAir(Station);
            _tint = Broadcasts.ScreenTint(Station);
        }

        // Tints the lit picture (drawn white) by the channel and its brightness, and the room light to match.
        private void Paint()
        {
            Color c = _tint * _bright;
            c.a = 1f;
            if (_picture != null && _block != null)
            {
                _picture.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                _block.SetColor(ColorId, c);
                _picture.SetPropertyBlock(_block);
            }
            if (_light != null && _light.Light != null)
            {
                _light.Light.color = Color.Lerp(LightColour, _tint, 0.35f);
                _light.Light.intensity = CloneZConfig.TvLightIntensity * _bright;
            }
        }
        #endregion
    }
}
