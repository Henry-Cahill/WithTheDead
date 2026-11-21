using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The county office's photocopier. "Run off a copy" sets it grinding — the lamp sweeps, the rollers clatter, and the
    /// racket carries through the office — and more often than not it ends in a paper jam with the red lamp lit, after
    /// which nothing more comes out of it until "Clear the paper jam" has fished the crumpled sheet out of its innards. Its
    /// lid lifts on the last original somebody left on the glass, which can be read. It needs the power (on for a whole
    /// raid). Nothing about it is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreCopier : Interactable
    {
        // The sheet left on the glass: one per copier, all original flavour.
        private static readonly string[] Originals =
        {
            "A county memo stamped URGENT: \u201CNon-essential staff are dismissed until further notice. Do not discuss the clinic with members of the public.\u201D",
            "A sign-up sheet for the office Christmas potluck. Eleven names, then, in two different pens: \u201Ccancelled?\u201D \u201CCANCELLED.\u201D",
            "A hand-drawn map of town with arrows to the Guard post: \u201CDISTRIBUTION POINT \u2014 WATER AND BLANKETS. BRING PHOTO ID.\u201D",
            "A missing-person form, half filled in. The description stops at \u201Clast seen wearing\u201D.",
            "Somebody's hand, pressed flat on the glass. By the look of the tray they copied it a dozen times.",
            "A county memo about budget cuts: \u201CAll non-essential purchases suspended until further notice.\u201D",
            "A flyer for the town meeting: \u201CAgenda – infrastructure, health, security.\u201D",
            "A draft of a permit application: \u201CConstruction of new community center – pending approval.\u201D",
            "A notice of street closure: \u201CMain St. closed for repairs – detour via 2nd Ave.\u201D",
            "A form for reporting potholes: \u201CReport #: ____ – location – description.\u201D",
            "A winter road‑closure warning: \u201CIcy conditions on Hwy 7 – keep off unless you have chains.\u201D",
            "A handwritten grocery list: \u201CMilk, beans, coffee, bacon, flour.\u201D",
            "A church bulletin announcement: \u201CSunday service at 10 a.m.; potluck supper after.\u201D",
            "A personal note about a missing family member: \u201CMissing: Elsie, age 7, last seen near the creek.\u201D",
            "A county health‑department flyer about flu shots: \u201CFree flu vaccination this week at the clinic.\u201D"
        };

private static readonly string[] Jams =
{
    "Halfway through, the rollers clunk and stop. A red lamp blinks: PAPER JAM \u2014 AREA B. Of course it does.",
    "It chews the sheet, shudders and gives up. The red lamp comes on and stays on.",
    "There's a crunch from deep inside, then silence and a smell of hot toner. Jammed.",
    "The rollers freeze and seize on a cold winter sheet, flashing the red lamp: PAPER JAM — FROZEN.",
    "A torn page catches on the feed, ripping into ribbons as the lamp blazes red: PAPER JAM — TORN.",
    "A loose sheet slips into the gears, the jam alarm chirps, and the lamp flashes: PAPER JAM — LOOSE SHEET.",
    "Spilled toner clings to the rollers, forming a sticky patch that stops the feed; the red lamp glows: TONER JAM — STICKY.",
    "A faint burnt smell rises as the heated drum overheats, halting the paper; the lamp shines red: PAPER JAM — BURNED.",
    "A heavy envelope wedges the rollers with a thump, the red lamp flickers: PAPER JAM — ENVELOPE.",
    "The jam alarm chirps: PAPER JAM \u2014 TRAY FULL. Of course it does.",
    "A thud echoes as the rollers stall. Red lamp blinks: JAM DETECTED \u2014 CLEAR MANUALLY.",
    // New jam variants – static, label, torn receipt, moisture, staple
    "A sudden puff of static charges the rollers, making them seize – the red lamp flashes: ELECTRIC JAM.",
    "A sticky label peels onto the feed, the paper sticks and the machine grinds to a halt – red lamp flares.",
    "A torn receipt fragments across the rollers, the shards jam the gears – red lamp blinks: PAPER JAM — TORN RECEIPT.",
    "A damp coffee spill drips onto the belt, the moisture makes the rollers stick – red lamp glows: PAPER JAM — MOISTURE.",
    "A forgotten stapled stack feeds whole, the staples jam the feed – the red lamp screams: STAPLE JAM."
};
        private static readonly string[] Copies =
        {
            "A warm copy slides into the tray: the sheet on the glass, a shade greyer and a touch crooked.",
            "It spits out a copy with a black stripe down one side. Good enough for county work.",
            "A pale copy drifts out, the ink faint, the paper curling at the edges.",
            "A crisp, bright copy lands with a thin line of toner, almost perfect.",
            "A thin sheet slides out, its grain rough and the edges frosted like a night‑time breath.",
            "A half‑yellow copy drops, a carbon‑copy slip tucked in the corner and a faint type‑writer smudge staining the margin.",
            "A bleached page emerges, dry and crackling, a torn corner where a frantic hand ripped it.",
            "A damp‑looking copy lands, a faint frost line along its edge, hinting at the cold beyond the office walls.",
            "A faded copy appears, the watermark “County” barely visible, the toner leaving a ghostly shadow of the original.",
            "A dull‑gray copy emerges, its corners frayed like old rope, the ink smeared by the cold draft seeping in.",
            "A thin sheet pops out, its surface dusted with a pale frost line, as if the copier breathed winter air.",
            "A pale, almost translucent copy slides free, the toner barely clinging, and a faint smell of stale coffee lingers.",
            "A crisp copy lands, the paper speckled with tiny ice crystals that melt into the fibers as it cools.",
            "A weary copy drops, the letters slightly blurred, the margin edged with a subtle white streak hinting at the office’s chill.",
        };

        private static readonly string[] Clears =
        {
            "You swing the side panel open and tease out a warm, pleated concertina of paper. The red lamp goes out.",
            "You prise a crumpled sheet from between the rollers and smudge toner up to your wrist. The lamp goes out.",
            "You yank the jammed sheet free, the plastic creaks as the rollers part.",
            "You tap the tray, and a fresh sheet slides out, the red lamp dimming.",
        };

        private Transform _body;
        private Vector3 _point;
        private FixturePart _lid;
        private Renderer _jamLamp;
        private bool _jammed;
        private int _original, _said; private bool _originalTaken;
        private float _noiseAt;
        private bool _copyAvailable;
        private string _currentCopyText;

        public override string DisplayName => "Photocopier";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        /// <summary>The copier's fixture on its body <paramref name="body"/>: <paramref name="point"/> is the control panel,
        /// <paramref name="lid"/> the hinged lid, <paramref name="jamLamp"/> the red lamp lit while it's jammed.</summary>
        internal static RaidStoreCopier Create(Transform body, Vector3 point, FixturePart lid, Renderer jamLamp, int seed)
        {
            var go = new GameObject("Fixture_Copier");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var c = go.AddComponent<RaidStoreCopier>();
            c._body = body;
            c._point = point;
            c._lid = lid;
            c._jamLamp = jamLamp;
            c._original = (seed & 0x7fff) % Originals.Length;
            c._said = seed & 3;
            c.ShowJam(false);
            return c;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            string dead = LightingFixtures.NothingHappens();
            if (_jammed)
            {
                into.Add(new InteractAction("Clear the paper jam", ClearJam)
                    .Timed(CloneZConfig.RaidCopierClearSeconds, "Clearing the paper jam", HumanoidActivity.Use));
                into.Add(Copy().DisabledIf(true, "It's jammed \u2014 clear it first"));
            }
            else into.Add(Copy().DisabledIf(dead != null, dead));
            bool open = _lid != null && _lid.IsOpen;
            if (!_jammed)
                into.Add(new InteractAction("Inspect copier", c => c.Notify("The copier hums smoothly – no jam in sight.")));
            else
                into.Add(new InteractAction("Inspect copier", c => c.Notify(GetJamMessage(_said))));
            into.Add(new InteractAction(open ? "Close the lid" : "Lift the lid", c => SetLid(!open)));
            if (_copyAvailable)
            {
                into.Add(new InteractAction("Take copy", c =>
                {
                    c.Notify($"You tuck the fresh copy into your satchel: {_currentCopyText}");
                    _copyAvailable = false;
                    _currentCopyText = null;
                }));
}
            if (open) {
                into.Add(new InteractAction("Read what's on the glass", c => c.Notify(Originals[_original])));
                if (!_originalTaken) {
                    into.Add(new InteractAction("Take the original sheet", c => {
                        _originalTaken = true;
                        c.Notify(Originals[_original]);
                    }));
                // New action: copy the original sheet left on the glass
                into.Add(CopyOriginal().DisabledIf(_jammed, "It's jammed — clear it first"));
                }
            }
        }

        private InteractAction Copy()
        {
            var a = new InteractAction("Run off a copy").Timed(CloneZConfig.RaidCopierCopySeconds, "Copying", HumanoidActivity.Use);
            a.Begin = c =>
            {
                _noiseAt = 0f;
                c.Notify("The copier wheezes awake. A bar of light sweeps under the lid and the rollers start to grind.");
            };
            a.Tick = (c, dt, progress) =>
            {
                if (Time.time < _noiseAt) return;
                _noiseAt = Time.time + CloneZConfig.RaidCopierNoiseInterval;
                MakeNoise(CloneZConfig.RaidCopierNoise);
            };
            a.Run = c =>
            {
                MakeNoise(CloneZConfig.RaidCopierNoise);
                if (Random.value < CloneZConfig.RaidCopierJamChance)
                {
                    _jammed = true;
                    _copyAvailable = false;
                    _currentCopyText = null;
                    ShowJam(true);
                    c.Notify(GetJamMessage(_said++));
                    return;
                }
                string copyText = _lid != null && _lid.IsOpen
                    ? "With the lid up the copy comes out black round the edges, the page floating in the middle like a window."
                    : Copies[_said++ % Copies.Length];
                c.Notify(copyText);
                _copyAvailable = true;
                _currentCopyText = copyText;
            };
            return a;
        }

        private InteractAction CopyOriginal()
        {
            var a = new InteractAction("Copy original sheet")
                .Timed(CloneZConfig.RaidCopierCopySeconds, "Copying original sheet", HumanoidActivity.Use);
            a.Begin = c =>
            {
                _noiseAt = 0f;
                c.Notify("The copier whirs, the lid lifts slightly and the rollers begin to grind on the original sheet.");
            };
            a.Tick = (c, dt, progress) =>
            {
                if (Time.time < _noiseAt) return;
                _noiseAt = Time.time + CloneZConfig.RaidCopierNoiseInterval;
                MakeNoise(CloneZConfig.RaidCopierNoise);
            };
            a.Run = c =>
            {
                MakeNoise(CloneZConfig.RaidCopierNoise);
                // Use the same jam chance as a normal copy
                if (Random.value < CloneZConfig.RaidCopierJamChance)
                {
                    _jammed = true;
                    _copyAvailable = false;
                    _currentCopyText = null;
                    ShowJam(true);
                    c.Notify(GetJamMessage(_said++));
                    return;
                }
                // Build a copy text that references the original sheet
                string copyText = $"Copy of note: {Originals[_original]}";
                c.Notify(copyText);
                _copyAvailable = true;
                _currentCopyText = copyText;
            };
            return a;
        }

        private void ClearJam(InteractContext ctx)
        {
            _jammed = false;
            _copyAvailable = false;
            _currentCopyText = null;
            ShowJam(false);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify(Clears[_said++ % Clears.Length]);
        }

        private void SetLid(bool open)
        {
            if (_lid == null) return;
            _lid.SetOpen(open);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
        }

        private void ShowJam(bool on)
        {
            if (_jamLamp != null) _jamLamp.enabled = on;
        }

        /// <summary>Returns a jam description, cycling safely through the Jams array.</summary>
private string GetJamMessage(int index) => Jams[index % Jams.Length];
    }
}
