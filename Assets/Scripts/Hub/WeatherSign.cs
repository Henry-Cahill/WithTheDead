using UnityEngine;
using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>
    /// Interactable attached to the hub’s front sign. Press E to get a short weather toast.
    /// </summary>
    public class WeatherSign : Interactable
    {
        public override string DisplayName => "Hub sign";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.5f;
        public override float Reach => 2.6f;
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Check weather", c =>
            {
                // Period‑correct message – the hub is always winter‑bound in 1993 Kentucky.
                GameEvents.RaiseToast("Cold wind howls outside; snow blankets the streets.");
            }));
        }
    }
}
