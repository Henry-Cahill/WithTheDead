using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The rain barrel by the west wall, as a fixture: fill a bottle from it or drink (the water's tainted — boil it
    /// first), see how much rain it has caught, wash your hands in it, or skim the leaves off the top. The barrel
    /// itself (<see cref="WaterSource"/>) catches the rain and keeps the level; this only fronts it for [E] and Q.
    /// [E] fills a bottle when there's one to fill, and otherwise just checks the level — never a raw drink.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WaterSource))]
    public class RainBarrelFixture : Interactable
    {
        private const float WashSeconds = 3f, SkimSeconds = 2.5f;
        private const float ShakeSeconds = 1.5f;
        private WaterSource _barrel;

        public override string DisplayName => "Rain barrel";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 0.9f;   // the rim

        private WaterSource Barrel => _barrel != null ? _barrel : (_barrel = GetComponent<WaterSource>());

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            WaterSource b = Barrel;
            if (b == null) return;
            bool water = b.HasWater;
            into.Add(new InteractAction("Fill a water bottle  (tainted)", c => WaterUse.FillBottle(c, b))
                .DisabledIf(!water, b.DryMessage)
                .DisabledIf(water && !WaterUse.HasEmptyBottle(ctx), "You have no empty bottle"));
            into.Add(new InteractAction("Check the water level", CheckLevel));
            into.Add(new InteractAction("Drink from the barrel  (tainted)", c => WaterUse.Drink(c, b))
                .DisabledIf(!water, b.DryMessage));
            into.Add(new InteractAction("Wash your hands in the barrel", Wash)
                .Timed(WashSeconds, "Washing your hands")
                .DisabledIf(!water, b.DryMessage));
            into.Add(new InteractAction("Wash your face in the barrel", WashFace)
                .Timed(WashSeconds, "Washing your face")
                .DisabledIf(!water, b.DryMessage));
            into.Add(new InteractAction("Skim the leaves off the top", Skim).Timed(SkimSeconds, "Skimming the barrel"));
            into.Add(new InteractAction("Shake the barrel", Shake).Timed(ShakeSeconds, "Shaking the barrel").DisabledIf(!Barrel.HasWater, Barrel.DryMessage));
        }

        private void CheckLevel(InteractContext ctx)
        {
            float fill = Barrel != null ? Barrel.Fill01 : 0f;
            string level =
                fill <= 0.01f ? "Bone dry. It needs a good downpour." :
                fill < 0.10f ? "A thin film of slush clings to the bottom." :
                fill < 0.20f ? "Just a shallow pool, enough for a quick sip." :
                fill < 0.25f ? "Barely a puddle in the bottom." :
                fill < 0.5f ? "About a quarter full \u2014 a drink or two's worth." :
                fill < 0.60f ? "Halfway full – the water is dark and still." :
                fill < 0.75f ? "Roughly half full." :
                fill < 0.85f ? "Nearly full, the surface ripples with the faint glow of distant snow." :
                fill < 0.99f ? "Most of the way to the brim." :
                "Brimming \u2014 it can't hold another drop.";
            WeatherSystem w = WeatherSystem.Instance;
            if (w != null && w.RainIntensity > 0.05f) level += " Rain drums on the water.";
            ctx.Notify(level);
        }

        private static void Wash(InteractContext ctx)
        {
            WaterUse.Freshen(ctx, 0.8f);
            ctx.Notify("You scrub your hands in the cold rainwater. Better than nothing.");
        }

        private static void WashFace(InteractContext ctx)
        {
            // Slightly less refreshing than washing hands
            WaterUse.Freshen(ctx, 0.4f);
            ctx.Notify("You splash water on your face. It's chilly, but it helps.");
        }

        private static void Shake(InteractContext ctx)
        {
            WaterUse.Freshen(ctx, 0.2f);
            ctx.Notify("You shake the barrel, splashing cold water over yourself.");
        }

        private void Skim(InteractContext ctx)
        {
            ctx.Notify(Barrel != null && Barrel.Water > 0.05f
                ? "You fish out a few drowned leaves and a beetle doing the backstroke."
                : "There's nothing to skim but a damp leaf stuck to the bottom.");
        }
    }
}
