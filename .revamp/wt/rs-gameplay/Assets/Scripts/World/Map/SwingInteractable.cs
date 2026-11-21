using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>A porch swing the survivor can "Sit" on (nothing happens yet: the action only closes the prompt). Nothing
    /// to save.</summary>
    public sealed class SwingInteractable : Interactable
    {
        private static readonly System.Action<InteractContext> s_Sit = ctx => { };

        public override string DisplayName => "Swing";

        public override void GetActions(InteractContext ctx, List<InteractAction> into) => into.Add(new InteractAction("Sit", s_Sit));
    }
}
