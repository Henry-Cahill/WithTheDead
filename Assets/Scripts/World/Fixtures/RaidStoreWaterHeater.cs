using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The county office's gas water heater in the utility room, as a water point (<see cref="IWaterPoint"/>): its cold
    /// inlet has been shut off, but the tank still holds clean water that runs from the brass drain valve at its foot — a
    /// limited store, drawn a bottle or a drink at a time (G works here too) until the tank is dry. Knock on the tank to
    /// hear how much is left; peer through the burner's sight glass at the pilot light, which may have gone out and can be
    /// lit again. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreWaterHeater : Interactable, IWaterPoint
    {
        private Transform _body;
        private Vector3 _valve, _point;
        private Renderer _pilotGlow;
        private int _water, _capacity;
        private bool _pilot;

        public override string DisplayName => "Water heater";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        #region IWaterPoint
        public Vector3 WaterPosition => _valve;
        public string WaterLabel => "water heater";
        public bool HasWater => _water > 0;
        public bool IsTainted => false;
        public string DryMessage => "The water heater's tank has been drained dry";

        public void DrawWater()
        {
            if (_water > 0) _water--;
            MakeNoise(CloneZConfig.RaidHeaterValveNoise);
            if (_water == 0) KitchenParts.NotifyNear(_valve, 3f, "The valve coughs, spits and runs dry. The tank's empty.");
        }
        #endregion

        /// <summary>The heater's fixture on its body <paramref name="body"/>: <paramref name="valve"/> is the drain valve,
        /// <paramref name="point"/> where the survivor reaches, <paramref name="pilotGlow"/> the flame in the sight glass.</summary>
        internal static RaidStoreWaterHeater Create(Transform body, Vector3 valve, Vector3 point, Renderer pilotGlow, int seed)
        {
            var go = new GameObject("Fixture_WaterHeater");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var h = go.AddComponent<RaidStoreWaterHeater>();
            h._body = body;
            h._valve = valve;
            h._point = point;
            h._pilotGlow = pilotGlow;
            var rng = new System.Random(seed);
            h._capacity = CloneZConfig.RaidHeaterWaterMax;
            h._water = CloneZConfig.RaidHeaterWaterMin + rng.Next(CloneZConfig.RaidHeaterWaterMax - CloneZConfig.RaidHeaterWaterMin + 1);
            h.SetPilot(rng.NextDouble() < CloneZConfig.RaidHeaterPilotChance);
            return h;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool water = HasWater;
            into.Add(new InteractAction("Drain the tank into a bottle", c => WaterUse.FillBottle(c, this))
                .DisabledIf(!water, DryMessage)
                .DisabledIf(water && !WaterUse.HasEmptyBottle(ctx), "You have no empty bottle"));
            into.Add(new InteractAction("Drink from the drain valve", c => WaterUse.Drink(c, this)).DisabledIf(!water, DryMessage));
            into.Add(new InteractAction("Knock on the tank", Knock));
            into.Add(new InteractAction("Check the pilot light", CheckPilot));
            if (_pilot)
                into.Add(new InteractAction("Turn off the pilot", Extinguish));
            else
                into.Add(new InteractAction("Relight the pilot", Relight)
                    .Timed(CloneZConfig.RaidHeaterRelightSeconds, "Relighting the pilot", HumanoidActivity.Use));
        }

        private void Knock(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            float fill = (float)_water / Mathf.Max(1, _capacity);
            ctx.Notify(_water <= 0 ? "It booms, hollow from top to bottom. Nothing left in it." :
                fill < 0.35f ? "It rings hollow until you knock near the bottom. Not much left." :
                fill < 0.7f ? "Dull from the middle down \u2014 about half a tank." :
                "A dull thud all the way up. Plenty in there.");
        }

        private void CheckPilot(InteractContext ctx)
        {
            ctx.Notify(_pilot
                ? "Through the little sight glass a blue pilot flame stands steady. The tank's warm to the touch."
                : "The sight glass is dark: the pilot's gone out. The tank is stone cold.");
        }

        private void Relight(InteractContext ctx)
        {
            SetPilot(true);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify("You turn the knob to PILOT, hold it in and click the igniter. On the fourth click a little blue flame catches.");
        }

        private void Extinguish(InteractContext ctx)
        {
            SetPilot(false);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify("You turn the knob to OFF, and the pilot flame sputters out, leaving the heater dark.");
        }

        private void SetPilot(bool lit)
        {
            _pilot = lit;
            if (_pilotGlow != null) _pilotGlow.enabled = lit;
        }
    }
}
