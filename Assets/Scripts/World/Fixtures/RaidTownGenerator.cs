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
            _fuel -= Time.deltaTime;
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
            into.Add(new InteractAction("Check the fuel", CheckFuel));
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

        private void CheckFuel(InteractContext ctx)
        {
            float f = _fuel / CloneZConfig.RaidGeneratorFuelMax;
            ctx.Notify(_fuel <= 0f ? "You unscrew the cap and look in. Bone dry; not even the smell of fuel left." :
                f < 0.35f ? "A slosh in the bottom of the tank. It won't run for long." :
                f < 0.7f ? "The tank's about half full. Enough to make a racket for a while." :
                "Most of a tank. Somebody filled it and never got to use it.");
        }

        private void ShowRunning(bool on)
        {
            if (_lamp != null) _lamp.enabled = on;
        }
    }
}
