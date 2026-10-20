using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Shows interior ceilings (and other overhead pieces such as the tent roof and the gas-station
    /// canopy) only from the shoulder and first-person views, PZ-style: the isometric camera looks
    /// down into roofless rooms. Hidden ceilings keep casting shadows, so the moon never lights a room
    /// from above in either view. One instance lives on the raid map's ceiling root; ceiling renderers
    /// register themselves through <see cref="Register"/>.
    /// </summary>
    public class CeilingVisibility : MonoBehaviour
    {
        private static readonly List<Renderer> s_Renderers = new List<Renderer>(64);
        private static bool s_Shown;

        /// <summary>Adds a ceiling renderer and puts it straight into the current view's state.</summary>
        public static void Register(Renderer r)
        {
            if (r == null) return;
            s_Renderers.Add(r);
            Apply(r, ViewState.CeilingsVisible);
        }

        /// <summary>Forgets every ceiling (called before a new map is built).</summary>
        public static void Clear()
        {
            s_Renderers.Clear();
            // s_Applied removed - no need to reset
        }

        /// <summary>Number of registered ceiling renderers.</summary>
        public static int Count => s_Renderers.Count;

        private static void Apply(Renderer r, bool shown)
            => r.shadowCastingMode = shown ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;

        private void LateUpdate()
        {
            bool shown = ViewState.CeilingsVisible;
            if (shown == s_Shown) return;
            s_Shown = shown;
            for (int i = s_Renderers.Count - 1; i >= 0; i--)
            {
                Renderer r = s_Renderers[i];
                if (r == null) { s_Renderers.RemoveAt(i); continue; }
                Apply(r, shown);
            }
        }

        private void OnDestroy() => Clear();
    }
}
