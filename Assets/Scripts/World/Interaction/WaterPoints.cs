using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Anything the survivor can drink from or fill a bottle at: a <see cref="WaterSource"/> (the kitchen
    /// tap, the rain barrel) or a fixture such as the bathroom basin, the toilet tank or the garden spigot.
    /// G (<see cref="PlayerHydration"/>) uses the nearest one; fixtures also offer drink / fill in their
    /// action menus through <see cref="WaterUse"/>.
    /// </summary>
    public interface IWaterPoint
    {
        /// <summary>Where the water is (reach is measured to it).</summary>
        Vector3 WaterPosition { get; }
        /// <summary>Lower-case name for prompts: "tap", "rain barrel", "toilet tank".</summary>
        string WaterLabel { get; }
        /// <summary>True when there's water to be had right now.</summary>
        bool HasWater { get; }
        /// <summary>Tainted water (barrel, toilet) should be boiled before drinking.</summary>
        bool IsTainted { get; }
        /// <summary>Why there's no water ("The tap is dry — the water's been shut off").</summary>
        string DryMessage { get; }
        /// <summary>Takes one drink's / bottle's worth of water.</summary>
        void DrawWater();
    }

    /// <summary>Finds the nearest <see cref="IWaterPoint"/>.</summary>
    public static class WaterPoints
    {
        /// <summary>The nearest water point within <paramref name="range"/> (flat) of <paramref name="p"/>, or null.
        /// A fixture that is itself a water point takes the place of a <see cref="WaterSource"/> on the same
        /// object (the kitchen sink fixture fronts the kitchen tap). Given a <paramref name="viewer"/>, only water
        /// it can see counts (<see cref="PlayerInteractor.CanSee"/>), so a tap behind a wall is out of reach.</summary>
        public static IWaterPoint Nearest(Vector3 p, float range, PlayerInteractor viewer = null)
        {
            IWaterPoint best = null;
            float bestSqr = range * range;
            List<WaterSource> sources = WaterSource.All;
            for (int i = 0; i < sources.Count; i++)
            {
                WaterSource s = sources[i];
                if (s == null) continue;
                Interactable front = s.GetComponent<Interactable>();
                IWaterPoint w = front as IWaterPoint ?? s;
                float d = FlatSqr(w.WaterPosition, p);
                if (d >= bestSqr) continue;
                bool seen = viewer == null || (front != null
                    ? viewer.CanSee(front.InteractPoint, front.OcclusionRoot)
                    : viewer.CanSee(s.transform.position + Vector3.up * 0.9f, s.transform));
                if (seen) { bestSqr = d; best = w; }
            }
            List<Interactable> fixtures = Interactable.All;
            for (int i = 0; i < fixtures.Count; i++)
            {
                Interactable f = fixtures[i];
                if (f == null || !(f is IWaterPoint w) || !f.IsAvailable || f.GetComponent<WaterSource>() != null) continue;
                float d = FlatSqr(w.WaterPosition, p);
                if (d >= bestSqr) continue;
                if (viewer == null || viewer.CanSee(f.InteractPoint, f.OcclusionRoot)) { bestSqr = d; best = w; }
            }
            return best;
        }

        private static float FlatSqr(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.sqrMagnitude;
        }
    }

    /// <summary>The shared drink / fill-a-bottle / wash logic every tap, basin and barrel uses.</summary>
    public static class WaterUse
    {
        /// <summary>A drink straight from <paramref name="src"/>: slakes thirst; tainted water may turn the stomach.</summary>
        public static bool Drink(InteractContext ctx, IWaterPoint src)
        {
            if (src == null || ctx.Stats == null) return false;
            if (!src.HasWater) { ctx.Notify(src.DryMessage); return false; }
            ctx.Stats.Drink(CloneZConfig.WaterDrinkAmount);
            if (src.IsTainted && ctx.Sickness != null) ctx.Sickness.OnDrankTainted();
            src.DrawWater();
            return true;
        }

        /// <summary>Fills one empty bottle in the pack from <paramref name="src"/>.</summary>
        public static bool FillBottle(InteractContext ctx, IWaterPoint src)
        {
            if (src == null || ctx.Inventory == null) return false;
            if (!src.HasWater) { ctx.Notify(src.DryMessage); return false; }
            if (!ctx.Inventory.FillOneBottle(src.IsTainted)) { ctx.Notify("You have no empty bottle"); return false; }
            src.DrawWater();
            return true;
        }

        /// <summary>True when the pack holds an empty bottle to fill.</summary>
        public static bool HasEmptyBottle(InteractContext ctx) => ctx.Inventory != null && ctx.Inventory.HasEmptyBottle();

        /// <summary>The standard pair of water actions ("Drink from the tap", "Fill a bottle") for a fixture's menu.</summary>
        public static void AddDrinkAndFill(InteractContext ctx, IWaterPoint src, List<InteractAction> into)
        {
            bool water = src.HasWater;
            string taint = src.IsTainted ? "  (tainted)" : "";
            into.Add(new InteractAction("Drink from the " + src.WaterLabel + taint, c => Drink(c, src))
                .DisabledIf(!water, src.DryMessage));
            into.Add(new InteractAction("Fill a water bottle" + taint, c => FillBottle(c, src))
                .DisabledIf(!water, src.DryMessage)
                .DisabledIf(water && !HasEmptyBottle(ctx), "You have no empty bottle"));
        }

        /// <summary>Splashing yourself with water: a little refreshed and calmer, cooler when hot, a touch damp.</summary>
        public static void Freshen(InteractContext ctx, float amount)
        {
            PlayerStats s = ctx.Stats;
            if (s == null) return;
            s.Fatigue = Mathf.Clamp01(s.Fatigue - 0.04f * amount);
            s.Panic = Mathf.Max(0f, s.Panic - 6f * amount);
            if (s.Temperature > CloneZConfig.TempComfortHigh) s.Temperature = Mathf.Max(CloneZConfig.TempNeutral, s.Temperature - 0.06f * amount);
            s.Wetness = Mathf.Clamp01(s.Wetness + 0.04f * amount);
        }
    }

    /// <summary>Something that warms the air round it while it's on (a lit burner, a hot shower).</summary>
    public interface IWarmthSource
    {
        Vector3 WarmthPosition { get; }
        /// <summary>How far the warmth reaches (m).</summary>
        float WarmthRadius { get; }
        /// <summary>How much it lifts the body's warmth target right beside it (0..<see cref="CloneZConfig.ComfortWarmthMax"/>); 0 when off.</summary>
        float Warmth { get; }
    }

    /// <summary>The registry of <see cref="IWarmthSource"/>s, read by <see cref="PlayerTemperature"/>.</summary>
    public static class HomeComfort
    {
        private static readonly List<IWarmthSource> s_Sources = new List<IWarmthSource>();

        public static void Register(IWarmthSource s) { if (s != null && !s_Sources.Contains(s)) s_Sources.Add(s); }
        public static void Unregister(IWarmthSource s) => s_Sources.Remove(s);

        /// <summary>The extra warmth (0..<see cref="CloneZConfig.ComfortWarmthMax"/>) felt at <paramref name="p"/>:
        /// the strongest source in reach, fading to nothing at its radius.</summary>
        public static float WarmthAt(Vector3 p)
        {
            float best = 0f;
            for (int i = s_Sources.Count - 1; i >= 0; i--)
            {
                IWarmthSource s = s_Sources[i];
                if (s == null || (s is Object o && o == null)) { s_Sources.RemoveAt(i); continue; }
                float w = s.Warmth;
                if (w <= 0f || s.WarmthRadius <= 0f) continue;
                Vector3 d = s.WarmthPosition - p;
                d.y = 0f;
                float k = 1f - Mathf.Clamp01(d.magnitude / s.WarmthRadius);
                best = Mathf.Max(best, w * k);
            }
            return Mathf.Min(best, CloneZConfig.ComfortWarmthMax);
        }
    }
}
