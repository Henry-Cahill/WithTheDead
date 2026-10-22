using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The coir welcome mat on the front stoop: wipe your feet on it, or lift its near edge to see what's under it
    /// (no spare key — the mat swings up on its far edge and drops back). Remembers whether it's been looked under.
    /// Its reach is short, so it doesn't steal [E] from the front door just behind it.
    /// </summary>
    [DisallowMultipleComponent]
    public class DoormatFixture : Interactable
    {
        private string _key;
        private Vector3 _point;
        private FixturePart _mat;
        private bool _lookedUnder;
        /// <summary>True while a spare key is hidden under the mat; persists via SaveState.</summary>
        private bool _hasKeyHidden;

        public override string DisplayName => "Doormat";
        public override Vector3 InteractPoint => _point;
        public override float Reach => 1.2f;
        public override string SaveKey => _key;

        /// <summary>The mat's fixture under <paramref name="host"/> (the yard); <paramref name="point"/> is its near edge.</summary>
        internal static DoormatFixture Create(Transform host, Vector3 point)
        {
            var go = new GameObject("Doormat");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var d = go.AddComponent<DoormatFixture>();
            d._point = point;
            d._key = KeyFor("doormat", point);
            return d;
        }

        /// <summary>Hands over the mat itself, hinged on its far edge to lift.</summary>
        internal void SetMat(FixturePart mat) => _mat = mat;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Wipe your feet", Wipe).Timed(1.2f, "Wiping your feet", HumanoidActivity.None));
            into.Add(new InteractAction("Kick the mat", Kick).Timed(0.6f, "Kicking the mat", HumanoidActivity.None));
            var look = new InteractAction("Look under the mat", LookUnder).Timed(1.4f, "Lifting the mat", HumanoidActivity.Search);
            look.Begin = c => Lift(true);
            look.Stopped = c => Lift(false);
            into.Add(look);
            // New: hide key – only show if player carries a spare key and key isn’t already hidden
            if (!_hasKeyHidden && ctx.Inventory != null && ctx.Inventory.Has("spare_key"))
            {
                into.Add(new InteractAction("Hide spare key under mat", HideKey).Timed(1.0f, "Hiding key", HumanoidActivity.None));
            }
            // New: retrieve key – only show after the mat has been looked under and a key is hidden
            if (_lookedUnder && _hasKeyHidden)
            {
                into.Add(new InteractAction("Retrieve spare key", RetrieveKey).Timed(1.0f, "Retrieving key", HumanoidActivity.Search));
            }
        }

        private static void Kick(InteractContext ctx)
        {
            ctx.Notify("You give the mat a hard kick, scattering a puff of dust.");
        }

        private static void HideKey(InteractContext ctx)
        {
            const string keyId = "spare_key"; // item id must exist in the catalogue
            if (ctx.Inventory != null && ctx.Inventory.Consume(keyId, 1) > 0)
            {
                var fixture = ctx.Interactor.FixtureTarget as DoormatFixture;
                if (fixture != null) fixture._hasKeyHidden = true;
                ctx.Notify("You slip the spare key under the mat. It will be safe for later.");
            }
            else
            {
                ctx.Notify("You don't have a spare key to hide.");
            }
        }
        private static void RetrieveKey(InteractContext ctx)
        {
            const string keyId = "spare_key";
            var fixture = ctx.Interactor.FixtureTarget as DoormatFixture;
            if (fixture != null && fixture._hasKeyHidden)
            {
                ItemDef def = ItemDatabase.Get(keyId);
                if (def != null) ctx.Inventory?.Inventory.Add(def, 1, enforceWeight: false);
                fixture._hasKeyHidden = false;
                ctx.Notify("You pull the spare key out from under the mat.");
            }
            else
            {
                ctx.Notify("There is no key hidden here.");
            }
        }






        private static void Wipe(InteractContext ctx)
        {
            bool muddy = ctx.Stats != null && ctx.Stats.Wetness > 0.3f;
            ctx.Notify(muddy
                ? "You stamp the worst of the mud and rain off your boots. The mat's seen worse."
                : "You scuff your boots back and forth across the bristles. Somebody raised you right.");
            if (ctx.Stats != null && ctx.Stats.Wetness > 0f)
                ctx.Stats.Wetness = Mathf.Max(0f, ctx.Stats.Wetness - 0.15f);
        }

        private void LookUnder(InteractContext ctx)
        {
            Lift(false);
            ctx.Notify(_lookedUnder
                ? "Still no key. The beetle hasn't moved."
                : "No spare key. Just a dead beetle on its back and a square of damp, pale concrete.");
            _lookedUnder = true;
        }

        private void Lift(bool up)
        {
            if (_mat != null) _mat.SetOpen(up);
        }

        public override void CaptureState(FixtureSave save) => save.State = (_lookedUnder ? 1 : 0) | (_hasKeyHidden ? 2 : 0);

        public override void RestoreState(FixtureSave save)
        {
            int state = save.State;
            _lookedUnder = (state & 1) != 0;
            _hasKeyHidden = (state & 2) != 0;
        }
    }
}
