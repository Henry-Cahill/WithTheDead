using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A blue public mail drop box on a Main Street sidewalk. Its chute still opens on a drift of letters nobody is coming
    /// to collect — a few of them can be made out, one after another — and the collection times on its front tell their
    /// own story. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownMailDrop : Interactable
    {

// What can be made out through the chute, in turn. All original flavour.
        private static readonly string[] Letters =
        {
            "A drift of envelopes. The top one is addressed to the county tax office in a careful hand, stamped and sealed. Somebody still meant to pay.",
            "Red and green envelopes \u2014 Christmas cards, weeks early. Somebody wanted them out while there was still a post to send them.",
            "A postcard lying face up: a lake and a single pine. On the back, in pencil: \u201CWish you were here. Glad you're not.\u201D No name.",
            "A fat envelope marked PHOTOS \u2014 DO NOT BEND, bent in half to make it fit the slot.",
            "A torn invitation, cream paper edged with glitter \u2014 a community pot‑luck scheduled for the evening before the storm. No RSVP.",
            "A yellowed flyer, corners browned: \u201CFREE COLD‑CUT HAM – PICK‑UP TODAY\u201D. The address points to the shuttered town hall.",
            "A handwritten note, ink smudged by frost: \u201CMeet me at the old oak, midnight. Bring the lockbox.\u201D No signature.",
            "A glossy catalog, pages half‑frosted, advertising winter coats labeled \u201CLimited stock\u201D. The price tag reads \u201C$0 – sample\u201D.",
            "An eviction notice, type‑written on yellowed stock, stating the lease ends March 1st and the landlord will lock the doors at sunrise. No response.",
            "The Sunday church bulletin, folded thin, lists a prayer circle for “the ones lost in the woods” and a call for donations to repair the stained‑glass window.",
            "A handwritten recipe card, ink smudged: “Grandma’s turnip soup – 2 cups water, 1 cup diced turnips, pinch of salt, simmer till tender. Share with anyone who’s cold.”",
            "A faded flyer, corners torn: “Missing: 8‑year‑old Ellie Harper, last seen near Miller’s Creek, wearing a red sweater and blue boots. Please call the sheriff.”",
            "A ration‑card reminder, stamped “County Health Dept”: “Winter food rations will be distributed at the school gym, 9 AM–12 PM, bring your metal ID tag.”",
            "A hand‑drawn weather warning, charcoal on cheap paper: “Heavy snow expected tonight. Stay indoors after dark; roads will be impassable.”",
            "A crinkled newspaper clipping reads: “Main Road to Miller’s Bridge shut after ice‑fall, detour via County Line expected until thaw.”",
            "A shaky grocery list scrawled in blue ink: “Canned beans, powdered milk, salt, fire‑starter – enough for three days.”",
            "A faded church flyer pinned to a post: “Winter shelter tonight at St. Luke’s, 7 PM. Bring blankets and warm drinks.”",
            "A desperate telegram, ink smeared: “Please, find my son Eli, last seen near the old mill. Return him safe.”",
            "A desperate note from a farmer, ink trembling: “Mary, the crops are ruined, please bring the seedbags before sunset.” No name.",
            "A warning scribbled on parchment, frost‑etched: “The well’s ice may burst at any moment. Keep away.” No signature.",
            "A hand‑drawn map, charcoal smudged: “Shortcut through the woods to the old mill – follow the bent pine.” No legend.",
            "A solemn obituary, printed in ink: “Constable Harris, 45 years of service, died in the night. Rest in peace.” No signature.",
            "A hand‑written postcard, faded ink: “Thinking of home’s fire‑pit, wish you could hear the creek again.” No return address.",
            "A torn grocery receipt, ink smudged: “J. L. Grocers – 5 lbs. flour, 2 cans beans, 1‑lb. bacon. Paid $3.45.”",
            "A small flyer, creased: “Volunteer fire‑watch, meet at the old school gym, 6 PM nightly. Bring lanterns.”",
            "A folded note, penciled hurriedly: “Do not trust the man in the blue coat. He’s feeding the dead.”",
            "A weather‑alert handout, stamped “County Emergency”: “Blizzard warning – winds 45 mph, zero visibility after midnight. Stay sheltered.”",
            "A brief obituary clipping, printed in stark type: “Mrs. Eleanor Whitaker, beloved baker, 78. Services at St. Luke’s, 2 PM Saturday.”",
        };

        // Additional flavour text for box notes
        private static readonly string[] BoxNotes =
        {
            "A faded sketch of the town’s main street, pencilled in by a child who dreamed of a future before the snow.",
            "A torn flyer announcing a “Winter dance at the community hall – dress warm, bring a partner.” No date.",
            "A crumpled memo from the sheriff: “Patrol the north road nightly. Keep the barricades stocked.”",
            "A weather‑log sheet, entries hand‑written: “Jan 12 – 2 in. snow, wind 30 mph. Jan 13 – clear, bitter cold.”",
            "A handwritten thank‑you note, ink bleeding: “To the volunteers who kept the lights on. You’re the town’s heartbeat.”",
            "A faded newspaper clipping: “Local farmer wins ‘Best Harvest’ award – celebration postponed due to blizzard.”"
        };

private Vector3 _point;
         private int _read;
         private int _historyRead;
         private bool _isEmpty;
         private Color _originalColor;
         private MeshRenderer _renderer;

        public override string DisplayName => "Mail drop box";
        public override Vector3 InteractPoint => _point;

        /// <summary>A mail drop box's fixture under <paramref name="host"/>; <paramref name="point"/> is its chute.</summary>
        internal static RaidTownMailDrop Create(Transform host, Vector3 point, int seed)
        {
            var go = new GameObject("Fixture_MailDrop");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var m = go.AddComponent<RaidTownMailDrop>();
            m._point = point;
            m._read = (seed & 0x7fff) % Letters.Length;
                         m._historyRead = ((seed >> 1) & 0x7fff) % BoxNotes.Length;
                         m._renderer = m.GetComponent<MeshRenderer>();
            if (m._renderer != null) m._originalColor = m._renderer.sharedMaterial.color;
            return m;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
                        var pull = new InteractAction("Pull open the chute", PeerIn).Timed(1.2f, "Peering into the chute", HumanoidActivity.Use);
            pull = pull.DisabledIf(IsEmpty(), "The chute is empty.");
            into.Add(pull);
            into.Add(new InteractAction("Read the collection times", c => c.Notify(
                "LAST COLLECTION 5 PM WEEKDAYS, NOON SATURDAY. Taped under it in marker: NO PICK-UP UNTIL FURTHER NOTICE.")));
            into.Add(new InteractAction("Inspect the box", c => c.Notify("The blue paint is chipped and the metal groans with each gust of wind.")));
            into.Add(new InteractAction("Read next letter (preview)", PeekNext).Timed(0.8f, "Peeking into the chute", HumanoidActivity.Use));
            into.Add(new InteractAction("Read box history", ReadHistory));
        }

        private bool IsEmpty() => _read >= Letters.Length;

        private void PeekNext(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            if (IsEmpty())
            {
                ctx.Notify("The chute is empty.");
                return;
            }
            ctx.Notify(Letters[_read]);
        }

        private void PeerIn(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            if (_read >= Letters.Length)
            {
                ctx.Notify("The chute is empty.");
                return;
            }
            ctx.Notify(Letters[_read]);
            _read++;
            if (IsEmpty())
            {
                _isEmpty = true;
                // Dim the box to indicate it’s empty
                if (_renderer != null)
                    _renderer.sharedMaterial.color = Color.Lerp(_originalColor, Color.gray, 0.6f);
            }
        }
        
        private void ReadHistory(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            if (BoxNotes.Length == 0)
            {
                ctx.Notify("There are no notes left.");
                return;
            }
            ctx.Notify(BoxNotes[_historyRead]);
            _historyRead = (_historyRead + 1) % BoxNotes.Length;
        }
    }
}