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
        };

        private static readonly string[] Jams =
        {
            "Halfway through, the rollers clunk and stop. A red lamp blinks: PAPER JAM \u2014 AREA B. Of course it does.",
            "It chews the sheet, shudders and gives up. The red lamp comes on and stays on.",
            "There's a crunch from deep inside, then silence and a smell of hot toner. Jammed.",
        };

        private static readonly string[] Copies =
        {
            "A warm copy slides into the tray: the sheet on the glass, a shade greyer and a touch crooked.",
            "It spits out a copy with a black stripe down one side. Good enough for county work.",
        };

        private static readonly string[] Clears =
        {
            "You swing the side panel open and tease out a warm, pleated concertina of paper. The red lamp goes out.",
            "You prise a crumpled sheet from between the rollers and smudge toner up to your wrist. The lamp goes out.",
        };

        private Transform _body;
        private Vector3 _point;
        private FixturePart _lid;
        private Renderer _jamLamp;
        private bool _jammed;
        private int _original, _said;
        private float _noiseAt;

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
            into.Add(new InteractAction(open ? "Close the lid" : "Lift the lid", c => SetLid(!open)));
            if (open) into.Add(new InteractAction("Read what's on the glass", c => c.Notify(Originals[_original])));
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
                    ShowJam(true);
                    c.Notify(Jams[_said++ % Jams.Length]);
                    return;
                }
                c.Notify(_lid != null && _lid.IsOpen
                    ? "With the lid up the copy comes out black round the edges, the page floating in the middle like a window."
                    : Copies[_said++ % Copies.Length]);
            };
            return a;
        }

        private void ClearJam(InteractContext ctx)
        {
            _jammed = false;
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
    }
}
