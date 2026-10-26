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
        private bool _torn;
        private const string _name = "Crumpled note";

        private void Awake()
        {
            if (!_torn)
                SetRandomText();
        }

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => transform.position + Vector3.up * 0.5f;
        public override float Reach => CloneZConfig.CivicRadioReach;
        public override string SaveKey => KeyFor("civicradio_note", transform.position);

public void SetRandomText()
        {
            _text = CivicRadio.RandomGuardCall();
        }

        private void TearUp(InteractContext ctx)
        {
            _torn = true;
            _text = null;
            ctx.Notify("You tear the note into useless scraps.");
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_torn)
            {
                // No actions when torn; could add a “Pick up scraps” stub later.
            }
            else
            {
                into.Add(new InteractAction("Read note", c => c.Notify(_text ?? "The note is blank.")));
                into.Add(new InteractAction("Tear up note", TearUp));
            }
        }

        public override void CaptureState(FixtureSave save) { save.Text = _torn ? "torn" : ""; }
        public override void RestoreState(FixtureSave save) { _torn = save.Text == "torn"; if (!_torn && string.IsNullOrEmpty(_text)) SetRandomText(); }
    }
}
