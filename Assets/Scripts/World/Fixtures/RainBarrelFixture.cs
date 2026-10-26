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
        private const float CoverSeconds = 1f;
        private bool _capped;
        private bool _boiled;
        private bool _filtered; // true after the water has been filtered with a rag
        private string _key;   // save key, from where the barrel stands (see SaveKey)
        // Random period‑accurate notes displayed when inspecting the barrel.
        private static readonly string[] _barrelNotes = new[] {
            "The metal is nicked where the old chain once held a bucket.",
            "A faded “Do Not Drink” sticker peels from the side, its paint long gone.",
            "You spot a tiny rust‑eaten hole near the base, water drips through it.",
            "Someone scrawled “Water's fine after a fire” in shaky chalk.",
            "A dried‑out leaf clings to the rim, a reminder of late‑summer storms.",
            "A rusted tin sign half‑buried in snow reads “Pure Water – Keep Closed”.",
            "A frayed rope loops around the barrel, its ends knotted into a crude latch.",
            "Bits of corn husk float on the surface, evidence of a recent harvest gone awry.",
            "A faint set of rabbit tracks leads up to the barrel’s base, then disappears.",
            "Ice crystals lace the rim, catching the pale morning light like tiny glass beads."
        };

        public override string DisplayName => "Rain barrel";
        public override Vector3 InteractPoint => transform.position + Vector3.up * (WaterSource.BarrelStandHeight + 0.9f);   // the rim, up on the stand
        // Keyed by the barrel's spot: WaterSource.Spawn places the barrel before adding this fixture, and a loaded game
        // spawns it back at its saved position, so the key is stable across runs.
        public override string SaveKey => _key ?? (_key = KeyFor("rainbarrel", transform.position));

        private WaterSource Barrel => _barrel != null ? _barrel : (_barrel = GetComponent<WaterSource>());

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // Cache the barrel component for quicker access and to handle potential missing component.
            WaterSource b = Barrel;
            if (b == null) return;
            // Determine if the barrel currently contains water for actions that need it.
            bool water = b.HasWater;
            // Add the action to fill a bottle with tainted water, disabling it if conditions aren't met.
            into.Add(new InteractAction("Fill a water bottle  (tainted)", c => WaterUse.FillBottle(c, b))
                .DisabledIf(!water, b.DryMessage)
                .DisabledIf(_capped, "The barrel is covered.")
                .DisabledIf(water && !WaterUse.HasEmptyBottle(ctx), "You have no empty bottle"));
            // Add the simple action that just reports the current water level.
            into.Add(new InteractAction("Check the water level", CheckLevel));
            // New instant action to inspect the barrel and show a random period‑accurate note.
            into.Add(new InteractAction("Inspect the barrel", c => {
                int i = UnityEngine.Random.Range(0, _barrelNotes.Length);
                c.Notify(_barrelNotes[i]);
            }));
            if (water)
            {
                if (_boiled)
                    into.Add(new InteractAction("Drink boiled water (safe)", c => {
                        WaterUse.Drink(c, b);
                        c.Notify("You sip the boiled water. It feels clean and safe.");
                    })
                        .DisabledIf(_capped, "The barrel is covered."));
                else if (_filtered)
                    into.Add(new InteractAction("Drink filtered water (safe)", c => {
                        WaterUse.Drink(c, b);
                        c.Notify("You drink the filtered water. It still tastes a bit off, but it’s safe.");
                    })
                        .DisabledIf(_capped, "The barrel is covered."));
                else
                    into.Add(new InteractAction("Drink from the barrel  (tainted)", c => WaterUse.Drink(c, b))
                        .DisabledIf(_capped, "The barrel is covered.")
                        .DisabledIf(!water, b.DryMessage));
            }
            into.Add(new InteractAction("Wash your hands in the barrel", Wash)
                .Timed(WashSeconds, "Washing your hands")
                .DisabledIf(!water, b.DryMessage)
                .DisabledIf(_capped, "The barrel is covered."));
            into.Add(new InteractAction("Wash your face in the barrel", WashFace)
                .Timed(WashSeconds, "Washing your face")
                .DisabledIf(!water, b.DryMessage)
                .DisabledIf(_capped, "The barrel is covered."));
            into.Add(new InteractAction("Skim the leaves off the top", Skim).Timed(SkimSeconds, "Skimming the barrel")
                .DisabledIf(_capped, "The barrel is covered."));
            into.Add(new InteractAction("Shake the barrel", Shake).Timed(ShakeSeconds, "Shaking the barrel").DisabledIf(!Barrel.HasWater, Barrel.DryMessage)
                .DisabledIf(_capped, "The barrel is covered."));
            into.Add(new InteractAction("Shake the barrel vigorously", VigorousShake).Timed(ShakeSeconds * 2, "Shaking the barrel hard").DisabledIf(!Barrel.HasWater, Barrel.DryMessage)
                .DisabledIf(_capped, "The barrel is covered."));
            // New action: filter water with a rag
            into.Add(new InteractAction("Filter water with a rag", c => {
                _filtered = true;
                c.Notify("You run a dirty rag through the water, pulling out grit and grime. The water looks clearer.");
            })
                .Timed(4f, "Filtering water")
                .DisabledIf(!water, b.DryMessage)
                .DisabledIf(_capped, "The barrel is covered.")
                .DisabledIf(_filtered, "The water has already been filtered.")
                .DisabledIf(_boiled, "The water is already boiled; filtering is unnecessary."));
            into.Add(new InteractAction(_capped ? "Uncover the barrel" : "Cover the barrel", ToggleCover)
                .Timed(CoverSeconds, _capped ? "Uncovering the barrel" : "Covering the barrel"));
            into.Add(new InteractAction(_boiled ? "Water already boiled" : "Boil the water", BoilWater)
                .Timed(5f, "Boiling the water")
                .DisabledIf(!Barrel.HasWater, Barrel.DryMessage)
                .DisabledIf(_capped, "The barrel is covered.")
                .DisabledIf(_boiled, "The water has already been boiled.")
                .DisabledIf(HomeComfort.WarmthAt(transform.position) < 0.1f, "No fire source nearby to boil the water."));
        }

        private void CheckLevel(InteractContext ctx)
        {
            float fill = Barrel != null ? Barrel.Fill01 : 0f;
// Added 7 new fill‑level messages (0.65–0.98) to give richer, period‑appropriate feedback.
            string level =
                fill <= 0.01f ? "Bone dry. It needs a good downpour." :
                fill < 0.10f ? "A thin film of slush clings to the bottom." :
                fill < 0.20f ? "Just a shallow pool, enough for a quick sip." :
                fill < 0.25f ? "Barely a puddle in the bottom." :
                fill < 0.50f ? "About a quarter full — a drink or two's worth." :
                fill < 0.60f ? "Halfway full – the water is dark and still." :
                // ---- new entries start here ----
                fill < 0.65f ? "The surface shivers, a faint green tinge hinting at algae." :
                fill < 0.70f ? "A quiet mood settles – the barrel swells with somber water." :
                fill < 0.75f ? "Roughly half full, the water feels colder now." :
                fill < 0.80f ? "Nearly full, the surface ripples with the faint glow of distant snow." :
                fill < 0.85f ? "A faint scent drifts – the water hints at algae." :
                fill < 0.90f ? "Most of the way to the brim." :
                // ---- original tail unchanged ----
                fill < 0.92f ? "The water shudders, a cold breath stirring the surface." :
                fill < 0.94f ? "Almost full, the surface glints like frozen glass." :
                fill < 0.96f ? "The barrel is brimming; a single drop threatens an overflow." :
                fill < 0.98f ? "The water swells, dark and heavy, ready to spill over." :
                fill < 0.99f ? "Almost brimming – the barrel gives a quiet warning." :
                "Brimming — it can't hold another drop.";
            WeatherSystem w = WeatherSystem.Instance;
            if (w != null && w.RainIntensity > 0.05f) level += " Rain drums on the water.";
            if (_capped) level += " The barrel is covered, preserving the water inside.";
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

        private static void VigorousShake(InteractContext ctx)
        {
            WaterUse.Freshen(ctx, 0.5f); // stronger freshen than normal shake
            ctx.Notify("You shake the barrel hard, splashing cold water over yourself.");
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

        private void ToggleCover(InteractContext ctx)
        {
            _capped = !_capped;
            if (Barrel != null) Barrel.ShowLid(_capped);
            ctx.Notify(_capped ? "You place a makeshift lid over the barrel, sealing the water inside." : "You remove the lid, exposing the barrel again.");
        }

        private void BoilWater(InteractContext ctx)
        {
            _boiled = true;
            _filtered = false; // boiled water no longer needs filtering
            ctx.Notify("The water reaches a rolling boil, killing the pathogens. It’s now safe to drink.");
        }

        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            int state = 0;
            if (_capped) state |= 1; // bit 0 = capped
            if (_boiled) state |= 2; // bit 1 = boiled
            if (_filtered) state |= 4; // bit 2 = filtered
            save.State = state;
        }

        public override void RestoreState(FixtureSave save)
        {
            base.RestoreState(save);
            _capped = (save.State & 1) != 0;
            _boiled = (save.State & 2) != 0;
            _filtered = (save.State & 4) != 0;
            if (Barrel != null) Barrel.ShowLid(_capped);
        }
    }
}
