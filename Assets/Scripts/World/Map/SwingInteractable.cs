using UnityEngine;
using System.Collections.Generic;
using CloneZ;

namespace CloneZ
{
    // Minimal interactable that lets the player "Sit" on a swing.
    // No animation – it only provides the action entry in the UI.
    public sealed class SwingInteractable : Interactable
    {
        public override string DisplayName => "Swing";

        // Called by the interaction system to populate the action list.
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // The "Sit" action does nothing but close the prompt; actual sitting logic
            // is handled elsewhere (e.g., PlayerAnimator). Keeping it simple avoids
            // extra dependencies for this night‑shift task.
            into.Add(new InteractAction("Sit", ctx => { /* placeholder – no effect */ }));
        }

        // Required stub – swing has no persistent state.
        public override string SaveKey => null;
        public override void CaptureState(FixtureSave w) { }
        public override void RestoreState(FixtureSave r) { }
    }
}
