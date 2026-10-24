using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The buildings of one map site (the fixed square, or one streamed piece) that have shells of their own, for the
    /// isometric cutaway (<see cref="MapCutaway"/>). <see cref="CutawayPass"/> puts it on the object holding the site's
    /// walls; it registers them while it's switched on (a streamed piece's from when the piece is switched on, built,
    /// until it's let go of) and takes them out, putting back whatever was cut, when it's switched off or destroyed.
    /// No Update: <see cref="MapCutawayDriver"/> runs them all.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapCutawaySite : MonoBehaviour
    {
        internal List<MapCutaway.Building> Buildings;
        /// <summary>Where the stand-ins for cut walls go: the site's shells under its overhead root (never baked, and gone
        /// with the site).</summary>
        internal Transform Overlays;
        private bool _registered;

        internal static MapCutawaySite Attach(GameObject host, Transform overlays, List<MapCutaway.Building> buildings)
        {
            var site = host.AddComponent<MapCutawaySite>();
            site.Buildings = buildings;
            site.Overlays = overlays;
            // Added to a live map (the fixed square, built in play mode): OnEnable ran before the list was handed over.
            if (Application.isPlaying && site.isActiveAndEnabled) site.Register();
            return site;
        }

        /// <summary>Registers the site's buildings (done by OnEnable in play mode; the harness calls it in edit mode).</summary>
        internal void Register()
        {
            if (_registered || Buildings == null) return;
            _registered = true;
            MapCutaway.Register(this);
        }

        /// <summary>Takes the site's buildings out again.</summary>
        internal void Unregister()
        {
            if (!_registered) return;
            _registered = false;
            MapCutaway.Unregister(this);
        }

        private void OnEnable() => Register();

        private void OnDisable() => Unregister();
    }
}
