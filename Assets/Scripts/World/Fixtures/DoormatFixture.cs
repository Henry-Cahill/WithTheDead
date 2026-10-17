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
            var look = new InteractAction("Look under the mat", LookUnder).Timed(1.4f, "Lifting the mat", HumanoidActivity.Search);
            look.Begin = c => Lift(true);
            look.Stopped = c => Lift(false);
            into.Add(look);
        }

        private static void Wipe(InteractContext ctx)
        {
            bool muddy = ctx.Stats != null && ctx.Stats.Wetness > 0.3f;
            ctx.Notify(muddy
                ? "You stamp the worst of the mud and rain off your boots. The mat's seen worse."
                : "You scuff your boots back and forth across the bristles. Somebody raised you right.");
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

        public override void CaptureState(FixtureSave save) => save.State = _lookedUnder ? 1 : 0;

        public override void RestoreState(FixtureSave save) => _lookedUnder = (save.State & 1) != 0;
    }
}
