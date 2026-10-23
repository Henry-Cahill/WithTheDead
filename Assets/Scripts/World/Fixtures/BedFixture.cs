using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The bed (on its <see cref="SleepSpot"/>). "Sleep" turns in until rested, with the same checks as the T key —
    /// not with the dead close by, not while a wound bleeds, not too panicked to drop off. "Lie down and rest" lies on
    /// top of the covers awake: an open-ended job (moving or [E] gets up) in which endurance comes back three times as
    /// fast and fatigue and nerves ease, while the clock runs on as normal (see <see cref="PlayerSeat.LieDown"/>); with
    /// the TV on across the room it can be "Lie back and watch TV" instead. The survivor can also sit on the edge of the
    /// bed, look under it, and make it again once it's been slept in or lain on — the made and the rumpled bedding are
    /// two renderers it swaps. Saves whether it's made.
    /// </summary>
    [DisallowMultipleComponent]
    public class BedFixture : Interactable
    {
        private SleepSpot _spot;
        private TvFixture _nearbyTv;
        private Renderer _made, _rumpled;
        private bool _isMade = true;
        private PlayerSleep _sleeper;
        private float _plumpCalmAt; private float _blanketFluffCalmAt; private float _mattressSearchCooldownAt;
        private PlayerInteractor _playerInteractor;
        // Existing style reference:
        // "Dust, a single odd sock and a shoebox of old birthday cards. Nothing you can use."
        private static readonly string[] _underBedDiscoveries = new[]
        {
            "Dust, a single odd sock and a shoebox of old birthday cards. Nothing you can use.",
            "Dust bunnies and a lost hairpin. Nothing hiding under there — you checked.",
            "A cracked thermos of tea, long gone cold, still faintly scented of peppermint.",
            "A faded newspaper clipping about a local high‑school basketball win, dated January 1993.",
            "A rusted pair of garden shears, the blades dulled by winter frost.",
            "An empty tin of marshmallows, its label peeled away, reminding you of campfire nights.",
            "A small wooden figurine of a horse, its paint chipped, a childhood souvenir.",
            "A tarnished tin of fire‑steel, its flint broken – a relic from the last winter’s campfires.",
            "A brittle newspaper clipping about the county’s annual horse‑show, dated December 1992.",
            "A half‑filled jar of preserves, the lid warped from the cold, its sweet scent faint in the air.",
            "A cracked ceramic mug, the blue glaze faded, still warm from the last use.",
            "A bundle of dried corn husks, tied with twine, probably meant for a winter craft.",
            "A rusted pocketknife, its handle splintered, the blade still dull but sharp enough to cut rope.",
            "A torn page from a devotional book, the verses bleeding onto the floorboards.",
            "A solitary pine‑cone, ice crystals clinging to its scales, a reminder of the forest outside."
        };
        private static readonly string[] _mattressFinds = new[]
        {
            "oldphoto", // a faded family photograph
            "bandage", // a clean cloth bandage
            "candle", // a short wax candle
            "matches", // a tiny box of matches
            "tinofbeans", // a half‑empty tin of beans
            "scrapmetal" // a piece of bent metal
        };

        private float _lullabyCalmAt;
        // Cached radius for sleep proximity checks to avoid per‑frame config lookup.
        private float _sleepCheckRadius;
        private void Awake()
        {
            _playerInteractor = PlayerInteractor.Current;
            // Initialize cached sleep check radius.
            _sleepCheckRadius = CloneZConfig.SleepRange + 0.5f;
            _mattressSearchCooldownAt = 0f;
        }

        /// <summary>True while the bed is made (fresh runs start with it made).</summary>
        public bool IsMade => _isMade;

        public override string DisplayName => _spot != null && !string.IsNullOrEmpty(_spot.DisplayName) ? _spot.DisplayName : "Bed";
        public override float Reach => CloneZConfig.BedReach;
        public override string SaveKey => KeyFor("bed", transform.position);

        /// <summary>On top of the covers, down the middle of the bed level with the survivor: the label follows them
        /// along it, and the bed is in reach from either side or the foot.</summary>
        public override Vector3 InteractPoint
        {
            get
            {
                float hz = SleepSpot.Length * 0.5f - 0.2f;
                var local = new Vector3(0f, 0.58f, 0f);
                PlayerInteractor pi = _playerInteractor != null ? _playerInteractor : (_playerInteractor = PlayerInteractor.Current);
                if (pi != null) local.z = Mathf.Clamp(transform.InverseTransformPoint(pi.transform.position).z, -hz, hz);
                return transform.TransformPoint(local);
            }
        }

        /// <summary>Hands the bed its sleep spot and its two sets of bedding (made, rumpled).</summary>
        internal void Bind(SleepSpot spot, Renderer made, Renderer rumpled)
        {
            _spot = spot;
            _made = made;
            _rumpled = rumpled;
            SetMade(true);
        }

        private void Update()
        {
            // However the survivor dropped off here — the bed's own action or the T key — it's been slept in.
            if (!_isMade) return;
            if (_playerInteractor == null) _playerInteractor = PlayerInteractor.Current;
            PlayerInteractor pi = _playerInteractor;
            if (pi == null) return;
            if (_sleeper == null) _sleeper = pi.GetComponent<PlayerSleep>();
            if (_sleeper != null && _sleeper.IsSleeping && SleepSpot.Nearest(pi.transform.position, _sleepCheckRadius, out _) == _spot)
                SetMade(false);
            // Cache nearby TV for use in GetActions to avoid per‑frame allocation.
            _nearbyTv = TvFixture.PlayingNear(transform.position, CloneZConfig.TvReach + SleepSpot.Length * 0.5f);
        }

        private void SetMade(bool made)
        {
            _isMade = made;
            if (_made != null) _made.enabled = made;
            if (_rumpled != null) _rumpled.enabled = !made;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerSeat seat = ctx.Seat;
            bool onEdge = seat != null && seat.IsSittingOn(this);

            string why = null;
            bool canSleep = ctx.Sleep != null && ctx.Sleep.CanSleep(out why);
            into.Add(new InteractAction("Sleep", Sleep).DisabledIf(!canSleep, why ?? "You can't sleep now"));

            into.Add(RestAction("Lie down and rest", "Resting", null).DisabledIf(seat == null, "You can't lie down now"));
            TvFixture tv = _nearbyTv;
            if (tv != null) into.Add(RestAction("Lie back and watch TV", "Watching TV in bed", tv).DisabledIf(seat == null, "You can't lie down now"));

            if (onEdge) into.Add(new InteractAction("Get up off the bed", c => c.Seat.Stand()));
            else into.Add(new InteractAction("Sit on the edge of the bed", SitOnEdge).DisabledIf(seat == null, "You can't sit down now"));

            if (!_isMade)
                into.Add(new InteractAction("Make the bed", MakeBed)
                    .Timed(CloneZConfig.MakeBedSeconds, "Making the bed", HumanoidActivity.Search)
                    .DisabledIf(onEdge, "Get up off the bed first"));
            else {
                into.Add(new InteractAction("Plump the pillows", PlumpPillows));
                into.Add(new InteractAction("Fluff the blankets", FluffBlankets));
            }
                
                // Calm the survivor with a short lullaby while seated or lying in the bed.
                into.Add(new InteractAction("Sing a lullaby", SingLullaby)
                    .Timed(2f, "Singing lullaby", HumanoidActivity.None)
                    .DisabledIf(seat == null, "You need to be sitting or lying to sing"));

            into.Add(new InteractAction("Search the mattress", SearchMattress)
            .Timed(2.5f, "Rummaging under mattress", HumanoidActivity.Search)
            .DisabledIf(seat == null, "You need to be sitting or lying to search")
            .DisabledIf(Time.time < _mattressSearchCooldownAt,
                "You already searched the mattress, give it a moment."));
            into.Add(new InteractAction("Look under the bed", LookUnder).Timed(1.6f, "Looking under the bed", HumanoidActivity.Search));
            into.Add(new InteractAction("Inspect the bed", InspectBed));
        }

        private void Sleep(InteractContext ctx)
        {
            if (ctx.Sleep == null) return;
            if (!ctx.Sleep.TrySleep(out string reason))
            {
                ctx.Notify(reason);
                return;
            }
            SetMade(false);
        }

        // Lying on the bed awake: PlayerSeat does the resting while the job runs; watching the TV adds its calm.
        private InteractAction RestAction(string label, string busy, TvFixture tv)
        {
            var a = new InteractAction(label).Timed(-1f, busy, HumanoidActivity.None);
            a.Begin = c =>
            {
                string reason = null;
                if (c.Seat == null || !c.Seat.LieDown(transform, out reason))
                {
                    c.Notify(reason ?? "You can't lie down now");
                    if (c.Interactor != null) c.Interactor.StopJob();
                    return;
                }
                SetMade(false);
            };
            if (tv != null) a.Tick = (c, dt, elapsed) => tv.Soothe(c, dt);
            a.Stopped = c =>
            {
                if (c.Seat != null) c.Seat.GetUp();
            };
            return a;
        }

        // On whichever long side the survivor stands, a little in from the edge, facing out into the room.
        private void SitOnEdge(InteractContext ctx)
        {
            if (ctx.Seat == null) return;
            Vector3 me = transform.InverseTransformPoint(ctx.Position);
            float side = me.x < 0f ? -1f : 1f;
            float hz = SleepSpot.Length * 0.5f;
            var local = new Vector3(side * (SleepSpot.Width * 0.5f - CloneZConfig.BedEdgeInset), CloneZConfig.BedEdgeSeatHeight,
                Mathf.Clamp(me.z, -hz + 0.65f, hz - 0.3f));
            float yaw = transform.eulerAngles.y + (side < 0f ? 270f : 90f);
            if (!ctx.Seat.TrySit(this, transform.TransformPoint(local), yaw, CloneZConfig.BedEdgeComfort, out string reason))
                ctx.Notify(reason);
        }

        private void MakeBed(InteractContext ctx)
        {
            SetMade(true);
            ctx.Notify("You shake out the quilt, tuck in the sheet and square the pillows. Better.");
        }

        private void PlumpPillows(InteractContext ctx)
        {
            if (Time.time >= _plumpCalmAt)
            {
                _plumpCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify("You plump the pillows. The room looks a little more like somebody's home.");
            }
            else ctx.Notify("The pillows are as plump as they're going to get.");
        }

        private void FluffBlankets(InteractContext ctx)
        {
            if (Time.time >= _blanketFluffCalmAt)
            {
                _blanketFluffCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify("You fluff the blankets. The bed looks tidier.");
            }
            else ctx.Notify("The blankets are as fluffed as they'll get now.");
        }

        private void SingLullaby(InteractContext ctx)
        {
            if (Time.time >= _lullabyCalmAt)
            {
                _lullabyCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify("Your soft humming soothes the nerves.");
            }
            else ctx.Notify("Your voice is already echoing.");
        }

        private void LookUnder(InteractContext ctx)
        {
            var msg = _underBedDiscoveries[Random.Range(0, _underBedDiscoveries.Length)];
            ctx.Notify(msg);
        }

        private void SearchMattress(InteractContext ctx)
        {
            // Set cooldown regardless of success.
            _mattressSearchCooldownAt = Time.time + CloneZConfig.TidyCooldown;
            // Pick a random find.
            string itemId = _mattressFinds[Random.Range(0, _mattressFinds.Length)];
            // Get the item definition.
            var def = ItemDatabase.Get(itemId);
            // Add the item to the survivor's inventory.
            if (def != null && ctx.Inventory?.Inventory.Add(def, 1, enforceWeight: false) > 0)
                ctx.Notify($"You pull out a {itemId.Replace("_", " ")} from beneath the mattress.");
            else
                ctx.Notify("You find something, but your pack is full.");
        }

        public override void CaptureState(FixtureSave save) => save.State = _isMade ? 0 : 1;

        private void InspectBed(InteractContext ctx)
        {
            ctx.Notify(_isMade ? "The bedding is neatly made." : "The sheets are rumpled and the pillows look flat.");
        }

        public override void RestoreState(FixtureSave save) => SetMade((save.State & 1) == 0);
    }
}
