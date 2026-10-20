using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The county office's bottled-water cooler: a water point (<see cref="IWaterPoint"/>) the survivor can drink from or
    /// fill a bottle at — from the menu or with G — for as long as the jug lasts. The jug is clean water that needs neither
    /// the mains nor the power; it holds a few drinks' worth (some offices had already drunk theirs dry), its level shows
    /// through the plastic, and it glugs a fat bubble up through itself every time water is drawn.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreWaterCooler : Interactable, IWaterPoint
    {
        private Transform _body;
        private Vector3 _taps;
        private Transform _water;
        private Renderer _waterRenderer;
        private int _drinks;
        private bool _triedHot;

        public override string DisplayName => "Water cooler";
        public override Vector3 InteractPoint => _taps;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        #region IWaterPoint
        public Vector3 WaterPosition => _taps;
        public string WaterLabel => "water cooler";
        public bool HasWater => _drinks > 0;
        public bool IsTainted => false;
        public string DryMessage => "The water cooler's jug is empty";

        public void DrawWater()
        {
            if (_drinks > 0) _drinks--;
            Refresh();
            MakeNoise(CloneZConfig.RaidCoolerGlugNoise);
            KitchenParts.NotifyNear(_taps, 3f, _drinks > 0
                ? "The jug glugs, a fat bubble wobbling up through what's left."
                : "The jug gives a last hollow glug. That's the end of it.");
        }
        #endregion

        /// <summary>The cooler's fixture on its body <paramref name="body"/>: <paramref name="taps"/> is where the water
        /// comes out, <paramref name="water"/> the jug's water (scaled to the level from its base).</summary>
        internal static RaidStoreWaterCooler Create(Transform body, Vector3 taps, Renderer water, int seed)
        {
            var go = new GameObject("Fixture_WaterCooler");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(taps, Quaternion.identity);
            var c = go.AddComponent<RaidStoreWaterCooler>();
            c._body = body;
            c._taps = taps;
            c._waterRenderer = water;
            c._water = water != null ? water.transform : null;
            var rng = new System.Random(seed);
            c._drinks = rng.NextDouble() < CloneZConfig.RaidCoolerJugEmptyChance ? 0 : 2 + rng.Next(CloneZConfig.RaidCoolerJugMax - 1);
            c.Refresh();
            return c;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            WaterUse.AddDrinkAndFill(ctx, this, into);
            into.Add(new InteractAction("Check the jug", CheckJug));
            into.Add(new InteractAction("Try the red tap", TryHot));
            into.Add(new InteractAction("Listen for glug", ListenGlug));
        }

        private void CheckJug(InteractContext ctx)
        {
            float fill = (float)_drinks / CloneZConfig.RaidCoolerJugMax;
            ctx.Notify(_drinks <= 0 ? "Empty \u2014 a ring of scale and a dead fly at the bottom of the jug." :
                fill < 0.3f ? "A couple of inches left in the jug. A drink or two." :
                fill < 0.7f ? "The jug's about half full." :
                "Nearly full. Nobody had time to drink it.");
        }

        private void TryHot(InteractContext ctx)
        {
            ctx.Notify(_triedHot
                ? "Still cold. The heater's unplugged; there's a space heater's plug where its own should be."
                : "The red tap runs as cold as the blue one. So much for a hot drink.");
            _triedHot = true;
        }

        private void ListenGlug(InteractContext ctx)
        {
            ctx.Notify(_drinks > 0 ? "The jug glugs, a fat bubble wobbling up through what's left." : "The jug gives a last hollow glug. That's the end of it.");
        }

        // The water in the jug, from its neck up to the level.
        private void Refresh()
        {
            float fill = Mathf.Clamp01((float)_drinks / CloneZConfig.RaidCoolerJugMax);
            if (_waterRenderer != null) _waterRenderer.enabled = fill > 0.01f;
            if (_water != null) _water.localScale = new Vector3(1f, Mathf.Max(0.02f, fill), 1f);
        }
    }
}
