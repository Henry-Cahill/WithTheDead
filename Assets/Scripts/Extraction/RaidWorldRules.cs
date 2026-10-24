using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The per-raid rolls on top of a zone's fixed layout: fresh loot in every container, which exits are open
    /// and what each demands, which spawn side the survivor gets and the exits that side can reach, and the
    /// helicopter insertion at the chosen landing zone.
    /// </summary>
    public static class RaidWorldRules
    {
        /// <summary>The insertion point value for walking in through the cordon rather than flying to a landing zone.</summary>
        public const int OnFoot = -2;

        /// <summary>Shows the insertion (the helicopter set-down). Set by the helicopter system; null = a plain arrival.</summary>
        public static System.Action<PlayerStats, Vector3> InsertionPresenter;

        /// <summary>Re-rolls every container's loot from the raid seed (the map layout came from the zone's seed).</summary>
        public static void RerollLoot(Transform world, int raidSeed)
        {
            LootContainer[] all = world.GetComponentsInChildren<LootContainer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                LootContainer c = all[i];
                if (c == null || c.Type == ContainerType.Corpse || c.IsSack) continue;
                Vector3 p = c.transform.position;
                int seed = raidSeed ^ (Mathf.RoundToInt(p.x * 37f) * 73856093) ^ (Mathf.RoundToInt(p.z * 37f) * 19349663) ^ (i * 83492791);
                RollLoot(c, new System.Random(seed));
            }
        }

        /// <summary>Fills one map container afresh for this raid (a quarter of the fixtures come up bare).</summary>
        public static void RollLoot(LootContainer c, System.Random rng)
        {
            if (c == null || c.Type == ContainerType.Corpse || c.IsSack) return;
            c.Contents = new Inventory(999f);
            if (c.IsFixture && rng.NextDouble() < 0.25) { c.Searched = c.SearchTime <= 0f; return; }
            ItemDatabase.RollLoot(c.Type, rng, c.Contents);
            if (c.IsFixture && c.Contents.Stacks.Count > 5) c.Contents.Stacks.RemoveRange(5, c.Contents.Stacks.Count - 5);
            c.Searched = c.SearchTime <= 0f;
        }

        /// <summary>Opens two to four exits for this raid and gives some of them conditions.</summary>
        public static void RollExits(RaidMapInfo map, System.Random rng)
        {
            List<RaidExit> exits = map.Exits;
            if (exits.Count == 0) return;
            float half = map.HalfSize;
            for (int i = 0; i < exits.Count; i++)
            {
                RaidExit e = exits[i];
                Vector3 p = e.Position;
                e.Side = Mathf.Abs(p.z) >= Mathf.Abs(p.x) ? (p.z > 0f ? 0 : 2) : (p.x > 0f ? 1 : 3);
                e.Open = rng.NextDouble() < 0.7;
                e.Condition = ExitCondition.None;
            }
            // Conditions: at most one of each kind, never all exits conditional.
            var kinds = new List<ExitCondition> { ExitCondition.Pay, ExitCondition.NoBackpack, ExitCondition.Switch, ExitCondition.CoopScav, ExitCondition.Flare };
            int conditional = 0;
            for (int i = 0; i < exits.Count && kinds.Count > 0; i++)
            {
                if (conditional >= exits.Count - 1 || rng.NextDouble() < 0.35) continue;
                int k = rng.Next(kinds.Count);
                RaidExit e = exits[i];
                e.Condition = kinds[k];
                kinds.RemoveAt(k);
                e.Open = true;
                conditional++;
                switch (e.Condition)
                {
                    case ExitCondition.Pay: e.Cost = 300 + rng.Next(5) * 100; e.Hint = "Smuggler's truck — pay the fare"; break;
                    case ExitCondition.NoBackpack: e.Hint = "Storm culvert — too tight for a backpack"; break;
                    case ExitCondition.Switch: e.Hint = "Freight gate — needs the power on"; break;
                    case ExitCondition.CoopScav: e.Hint = "Scav trail — they only let their own through"; break;
                    case ExitCondition.Flare: e.Hint = "Landing zone — fire a flare, hold out"; e.Radius = 9f; e.ExtractSeconds = 3f; break;
                }
            }
            if (!exits.Exists(x => x.Open)) exits[rng.Next(exits.Count)].Open = true;
            if (GameSession.IsOpenZone)
                foreach (RaidExit e in exits) if (e.Condition == ExitCondition.None) e.Open = true;
            if (exits.Exists(x => x.Condition == ExitCondition.Switch)) PlaceSwitch(map, rng);
        }

        // The switch for a power-gated exit sits inside a building somewhere else on the map.
        private static void PlaceSwitch(RaidMapInfo map, System.Random rng)
        {
            var spots = new List<Vector3>();
            foreach (LootContainer c in LootContainer.All)
                if (c != null && c.IsFixture && Shelter.IsSheltered(c.transform.position + Vector3.up)) spots.Add(c.transform.position);
            Vector3 at = spots.Count > 0 ? spots[rng.Next(spots.Count)] : Vector3.zero;
            var go = new GameObject("PowerSwitch");
            go.transform.position = new Vector3(at.x, 1.2f, at.z);
            var box = CloneZUtil.Box("SwitchBox", go.transform.position, new Vector3(0.3f, 0.45f, 0.15f), new Color(0.55f, 0.5f, 0.2f), go.transform);
            Object.Destroy(box.GetComponent<Collider>());
            go.AddComponent<PowerSwitch>();
            WorldMarkers.Add("exit_switch", go.transform.position);
        }

        /// <summary>
        /// Picks the arrival point: the chosen landing zone (0 N, 1 E, 2 S, 3 W) or a random side, and closes exits
        /// too close to it (each spawn side reaches its own set of exits). True when it's a helicopter insertion.
        /// <paramref name="lz"/> = <see cref="OnFoot"/> walks in from the map's own entry point (through the cordon).
        /// </summary>
        public static bool PickInsertion(RaidMapInfo map, int lz, ref Vector3 spawn, ref float yaw)
        {
            IReadOnlyList<Vector3> pads = WorldMarkers.Get(WorldMarkers.Landing);
            bool heli = false;
            if (pads.Count > 0 && lz != OnFoot)
            {
                int pick = lz >= 0 && lz < pads.Count ? lz : Random.Range(0, pads.Count);
                spawn = pads[pick] + Vector3.up * 0.6f;
                Vector3 toCentre = -spawn;
                toCentre.y = 0f;
                if (toCentre.sqrMagnitude > 0.01f) yaw = Quaternion.LookRotation(toCentre).eulerAngles.y;
                heli = true;
            }
            if (heli) {
                int open = 0;
                foreach (RaidExit e in map.Exits)
                {
                    if (ExtractionManager.FlatDistance(e.Position, spawn) < 45f) e.Open = false;
                    if (e.Open) open++;
                }
                if (open == 0)
                {
                    RaidExit far = null;
                    float best = 0f;
                    foreach (RaidExit e in map.Exits)
                    {
                        float d = ExtractionManager.FlatDistance(e.Position, spawn);
                        if (d > best) { best = d; far = e; }
                    }
                    if (far != null) far.Open = true;
                }
            }
            return heli;
        }

        public static void PlayInsertion(PlayerStats player, Vector3 at)
        {
            if (InsertionPresenter != null)
            {
                try { InsertionPresenter(player, at); return; }
                catch (System.Exception e) { Debug.LogException(e); }
            }
            GameEvents.RaiseToast("The helicopter sets you down at the landing zone and lifts away.");
        }
    }

    /// <summary>The power switch for a freight-gate exit.</summary>
    public class PowerSwitch : Interactable
    {
        private const float PowerDuration = 30f; // seconds the switch stays on after being thrown
        private float _remainingPower; // counts down while the switch is on
        private bool _on;
        private Renderer _renderer;
        private void Awake()
        {
            // The visual box created in PlaceSwitch is a child named "SwitchBox"
            var box = transform.Find("SwitchBox");
            if (box != null) _renderer = box.GetComponent<Renderer>();
            UpdateColor();
        }

        private void Update()
        {
            if (_on && _remainingPower > 0f)
            {
                _remainingPower -= Time.deltaTime;
                if (_remainingPower <= 0f)
                    AutoPowerOff();
            }
        }

        private void AutoPowerOff()
        {
            _on = false;
            _remainingPower = 0f;
            UpdateColor();
            if (ExtractionManager.Instance != null)
                foreach (RaidExit e in ExtractionManager.Instance.Exits)
                    if (e.Condition == ExitCondition.Switch) e.Powered = false;
            NoiseSystem.Emit(transform.position, 15f);
            Sfx.Play(SfxIds.UiClick, transform.position, 0.6f);
            GameEvents.RaiseToast("The freight‑gate power fades; the gate powers down.");
        }
        private void UpdateColor()
        {
            if (_renderer != null)
                _renderer.sharedMaterial.color = _on ? Color.green : new Color(0.55f, 0.2f, 0.2f);
        }
        public override void CaptureState(FixtureSave save)
        {
            save.State = _on ? 1 : 0;
        }
        public override void RestoreState(FixtureSave save)
        {
            _on = save.State != 0;
            UpdateColor();
        }

        public override string DisplayName => "Power switch";
        public override string SaveKey => KeyFor("powerswitch", transform.position);
        public override Vector3 InteractPoint => transform.position;
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // Throw action
into.Add(new InteractAction(_on ? "The power is on" : "Throw the power switch", c =>
                {
                    if (_on) return;
                    _on = true;
                    _remainingPower = PowerDuration;
                    UpdateColor();
                    if (ExtractionManager.Instance != null)
                        foreach (RaidExit e in ExtractionManager.Instance.Exits)
                            if (e.Condition == ExitCondition.Switch) e.Powered = true;
                    NoiseSystem.Emit(transform.position, 25f);
                    Sfx.Play(SfxIds.DoorBash, transform.position, 0.8f);
                    c.Notify("Somewhere a freight gate hums awake.");
                }).Timed(2.5f, "Throwing the switch").DisabledIf(_on, "Already on"));
            // Reset action appears after the switch has been thrown
into.Add(new InteractAction("Reset the power switch", c =>
                {
                    if (!_on) return;
                    _on = false;
                    _remainingPower = 0f;
                    UpdateColor();
                    if (ExtractionManager.Instance != null)
                        foreach (RaidExit e in ExtractionManager.Instance.Exits)
                            if (e.Condition == ExitCondition.Switch) e.Powered = false;
                    NoiseSystem.Emit(transform.position, 15f);
                    Sfx.Play(SfxIds.UiClick, transform.position, 0.6f);
                    c.Notify("The freight gate powers down.");
                }).Timed(1.5f, "Resetting the switch").DisabledIf(!_on, "Already off"));
            // Inspect action
            into.Add(new InteractAction("Inspect the power switch", c => c.Notify("A rugged switch that powers freight‑gate exits.")));
        }
    }
}
