using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A fire hydrant on a street corner, frozen up for the winter: nothing to be had from it without a hydrant wrench, but
    /// it can be tried, and the snow brushed off its bonnet. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownHydrant : Interactable, IWaterPoint
    {
        private Vector3 _point;
        private bool _brushed;
        private bool _opened;
        private string _key;

        public override string DisplayName => "Fire hydrant";
        public override string SaveKey => _key;

        // IWaterPoint implementation
        /// <summary>Returns the world position where water emerges from the hydrant.</summary>
        public Vector3 WaterPosition => _point;
        /// <summary>Label used for UI when displaying this water source.</summary>
        public string WaterLabel => "hydrant";
        /// <summary>True when the hydrant has been opened with a wrench and can supply water.</summary>
        public bool HasWater => _opened;
        /// <summary>Hydrant water is never contaminated, so this is always false.</summary>
        public bool IsTainted => false;
        /// <summary>Shown when the hydrant is closed; tells the player a wrench is needed to open it.</summary>
        public string DryMessage => "The hydrant is frozen solid – you need a wrench to open it";
        /// <summary>Provides unlimited water when the hydrant is opened; otherwise does nothing.</summary>
        public void DrawWater() { /* unlimited water when opened */ }

        public override Vector3 InteractPoint => _point;

        /// <summary>A hydrant's fixture under <paramref name="host"/>; <paramref name="point"/> is its bonnet.</summary>
        internal static RaidTownHydrant Create(Transform host, Vector3 point)
        {
            var go = new GameObject("Fixture_Hydrant");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var h = go.AddComponent<RaidTownHydrant>();
            h._point = point;
            h._key = Interactable.KeyFor("hydrant", point);
            return h;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Try the valve cap", c => c.Notify(
                "You wrench at the cap with both hands. Frozen solid — and the valve nut wants a pentagon wrench you haven't got.")));
            into.Add(new InteractAction(_brushed ? "Read the tag" : "Knock the snow off it", Brush));
            into.Add(new InteractAction("Inspect the metal", c => c.Notify("The hydrant’s iron is corroded and stained by years of winter ice.")));
            // Open/close action
            into.Add(new InteractAction(_opened ? "Close the valve" : "Open with wrench", c =>
            {
                _opened = !_opened;
                ctx.Notify(_opened ? "You pry open the hydrant with the wrench – cold water rushes out." : "You shut the hydrant valve.");
            }));
        }

        private void Brush(InteractContext ctx)
        {
            _brushed = true;
            ctx.Notify("Under the snow a painted tag reads FLUSHED 10-92. Somebody looked after this once.");
        }

        public override void CaptureState(FixtureSave save) => save.State = _opened ? 1 : 0;
        public override void RestoreState(FixtureSave save) => _opened = (save.State & 1) != 0;
    }
}
