using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The portable generator at the quarantine site, its tank part full. "Pull the starter cord" rattles it over (loud
    /// enough on its own) and now and then it catches; running, it hammers out a din every couple of seconds that every
    /// dead thing in earshot comes to — a lure, for as long as its fuel lasts or until somebody switches it off again. A
    /// green lamp on its panel shows it's running. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownGenerator : Interactable
    {
        private Vector3 _point;
        private Renderer _lamp;
        private bool _running;
        private float _fuel, _noiseAt;
        private int _pulls;
        private bool _oilApplied;
        private float _fuelDrainRate = 1f; // units of fuel per second
        private const string FuelCanItemId = "fuel_can"; // ID of the fuel can item
        private const float FuelCanRestore = 30f; // units of fuel added per can

        public override string DisplayName => "Generator";
        public override Vector3 InteractPoint => _point;

        /// <summary>The generator's fixture under <paramref name="host"/>: <paramref name="point"/> is its control panel,
        /// <paramref name="lamp"/> the run lamp on it.</summary>
        internal static RaidTownGenerator Create(Transform host, Vector3 point, Renderer lamp, int seed)
        {
            var go = new GameObject("Fixture_Generator");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var g = go.AddComponent<RaidTownGenerator>();
            g._point = point;
            g._lamp = lamp;
            var rng = new System.Random(seed);
            g._fuel = Mathf.Lerp(CloneZConfig.RaidGeneratorFuelMin, CloneZConfig.RaidGeneratorFuelMax, (float)rng.NextDouble());
            g.ShowRunning(false);
            return g;
        }

        private void Update()
        {
            if (!_running) return;
            _fuel -= Time.deltaTime * _fuelDrainRate;
            if (_fuel <= 0f)
            {
                _fuel = 0f;
                Stop();
                KitchenParts.NotifyNear(_point, 20f, "The generator coughs, splutters and dies. Out of fuel.");
                return;
            }
            if (Time.time < _noiseAt) return;
            _noiseAt = Time.time + CloneZConfig.RaidGeneratorNoiseInterval;
            MakeNoise(CloneZConfig.RaidGeneratorNoise);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_running) into.Add(new InteractAction("Switch off the generator", SwitchOff));
            else
                into.Add(new InteractAction("Pull the starter cord", Pull)
                    .Timed(CloneZConfig.RaidGeneratorPullSeconds, "Pulling the cord", HumanoidActivity.Use)
                    .DisabledIf(_fuel <= 0f, "Its tank is dry"));
            if (!_oilApplied) into.Add(new InteractAction("Oil the generator", Oil));
            // Refuel action – enabled only when the player has a fuel can
            into.Add(new InteractAction("Refuel generator", Refuel)
                .DisabledIf(!ctx.Inventory?.Has(FuelCanItemId) ?? true,
                "You need a fuel can to refuel the generator"));
            into.Add(new InteractAction("Check the fuel", CheckFuel));
            into.Add(new InteractAction("Estimate runtime", EstimateRuntime));
            into.Add(new InteractAction("Inspect generator", Inspect));
            into.Add(new InteractAction("Check next noise", CheckNoise));
        }

        private void Pull(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.RaidGeneratorPullNoise);
            _pulls++;
            // A cold engine takes a few goes; by the fourth pull it's as good as warm.
            if (_pulls < 4 && Random.value >= CloneZConfig.RaidGeneratorStartChance)
            {
                ctx.Notify(_pulls == 1
                    ? "The cord rasps out and snaps back. The engine turns over, coughs once and goes quiet."
                    : "Another pull. It sputters, almost catches, and dies again.");
                return;
            }
            _pulls = 0;
            _running = true;
            _noiseAt = Time.time;
            ShowRunning(true);
            ctx.Notify("It catches with a bang and settles into a hammering roar. Everything for blocks around will hear that.");
        }

        private void CheckNoise(InteractContext ctx)
        {
            float remaining = Mathf.Max(0f, _noiseAt - Time.time);
            ctx.Notify($"The next roar will come in about {(int)remaining}s.");
        }


        private void SwitchOff(InteractContext ctx)
        {
            Stop();
            ctx.Notify("You flick the kill switch. The roar winds down to a tick of cooling metal, and the quiet rushes back in.");
        }

        private void Stop()
        {
            _running = false;
            ShowRunning(false);
        }

        private void Oil(InteractContext ctx)
        {
            _oilApplied = true;
            _fuelDrainRate = 0.5f; // fuel now drains at half the original rate
            ctx.Notify("You pour a bit of oil into the engine. It will now consume fuel twice as slowly.");
        }

        // Refuel action – consumes a fuel can and restores fuel
        private void Refuel(InteractContext ctx)
        {
            // Ensure the player actually has a fuel can
            if (ctx.Inventory == null || !ctx.Inventory.Has(FuelCanItemId))
            {
                ctx.Notify("You have nothing to pour into the tank.");
                return;
            }
            // Find a stack with the fuel can and remove one unit
            var inv = ctx.Inventory;
            foreach (var stack in inv.Inventory.Stacks)
            {
                if (stack.Def != null && stack.Def.Id == FuelCanItemId)
                {
                    inv.Inventory.Remove(stack, 1);
                    break;
                }
            }
            // Restore fuel, clamped to the maximum allowed
            _fuel = Mathf.Min(_fuel + FuelCanRestore, CloneZConfig.RaidGeneratorFuelMax);
            float percent = (_fuel / CloneZConfig.RaidGeneratorFuelMax) * 100f;
            ctx.Notify($"You pour the fuel into the tank. Fuel gauge rises to {(int)percent}%.");
            // If the generator was off and now has fuel, update the inspect message
            if (!_running && _fuel > 0f)
                Inspect(ctx);
        }
        private void Inspect(InteractContext ctx)
        {
            float percent = _fuel / CloneZConfig.RaidGeneratorFuelMax;
            if (_running)
                ctx.Notify($"The generator rumbles on. Fuel gauge reads about {(int)(percent * 100)}%.");
            else
                ctx.Notify($"The generator is off. Fuel gauge reads about {(int)(percent * 100)}%.");
        }

        private void CheckFuel(InteractContext ctx)
        {
            float f = _fuel / CloneZConfig.RaidGeneratorFuelMax;
            ctx.Notify(_fuel <= 0f ? "You unscrew the cap and look in. Bone dry; not even the smell of fuel left." :
                f < 0.35f ? "A slosh in the bottom of the tank. It won't run for long." :
                f < 0.7f ? "The tank's about half full. Enough to make a racket for a while." :
                "Most of a tank. Somebody filled it and never got to use it." +
                (_oilApplied ? " (Oil applied – fuel lasts longer.)" : ""));
        }

        private void EstimateRuntime(InteractContext ctx)
        {
            if (_fuel <= 0f)
            {
                ctx.Notify("No fuel left – the generator can’t run any longer.");
                return;
            }
            float secs = _fuel / _fuelDrainRate; // adjust for oil‑reduced consumption
            int mins = (int)(secs / 60f);
            secs = secs % 60f;
            ctx.Notify($"It will run for about {mins} min {(int)secs}s more.");
        }

        private void ShowRunning(bool on)
        {
            if (_lamp != null) _lamp.enabled = on;
        }
    }
}
