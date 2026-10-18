using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A fire hydrant on a street corner, frozen up for the winter: nothing to be had from it without a hydrant wrench, but
    /// it can be tried, and the snow brushed off its bonnet. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownHydrant : Interactable
    {
        private Vector3 _point;
        private bool _brushed;

        public override string DisplayName => "Fire hydrant";
        public override Vector3 InteractPoint => _point;

        /// <summary>A hydrant's fixture under <paramref name="host"/>; <paramref name="point"/> is its bonnet.</summary>
        internal static RaidTownHydrant Create(Transform host, Vector3 point)
        {
            var go = new GameObject("Fixture_Hydrant");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var h = go.AddComponent<RaidTownHydrant>();
            h._point = point;
            return h;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Try the valve cap", c => c.Notify(
                "You wrench at the cap with both hands. Frozen solid \u2014 and the valve nut wants a pentagon wrench you haven't got.")));
            into.Add(new InteractAction(_brushed ? "Read the tag" : "Knock the snow off it", Brush));
        }

        private void Brush(InteractContext ctx)
        {
            _brushed = true;
            ctx.Notify("Under the snow a painted tag reads FLUSHED 10-92. Somebody looked after this once.");
        }
    }
}
