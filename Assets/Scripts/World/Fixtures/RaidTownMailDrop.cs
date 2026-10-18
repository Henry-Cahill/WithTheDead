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
        };

        private Vector3 _point;
        private int _read;

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
            return m;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Pull open the chute", PeerIn).Timed(1.2f, "Peering into the chute", HumanoidActivity.Use));
            into.Add(new InteractAction("Read the collection times", c => c.Notify(
                "LAST COLLECTION 5 PM WEEKDAYS, NOON SATURDAY. Taped under it in marker: NO PICK-UP UNTIL FURTHER NOTICE.")));
        }

        private void PeerIn(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify(Letters[_read % Letters.Length]);
            _read++;
        }
    }
}
