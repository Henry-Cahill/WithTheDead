using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A crumpled note placed on a Guard‑band radio desk. When read it shows a random distress‑call line.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicRadioNote : Interactable
    {
        private string _text;
        private const string _name = "Crumpled note";

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => transform.position + Vector3.up * 0.5f;
        public override float Reach => CloneZConfig.CivicRadioReach;
        public override string SaveKey => KeyFor("civicradio_note", transform.position);

        public void SetRandomText()
        {
            _text = CivicRadio.RandomGuardCall();
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Read note", c => c.Notify(_text ?? "The note is blank.")));
        }

        public override void CaptureState(FixtureSave save) { }
        public override void RestoreState(FixtureSave save) { }
    }
}
