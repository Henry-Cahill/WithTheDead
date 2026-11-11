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
            if (exits.Exists(x => x.Condition == ExitCondition.Switch) && !PlaceSwitch(rng))
                new GameObject("PowerSwitchSite").AddComponent<PowerSwitchSite>().Rng = new System.Random(rng.Next());
        }

        private static readonly Vector3 SwitchSize = new Vector3(0.3f, 0.45f, 0.15f);

        /// <summary>Hangs the switch for a power-gated exit on a wall inside a building somewhere on the map, by one of the
        /// storage fixtures in its sheltered rooms. False while the map has no such wall: a streamed map fits its rooms out
        /// only as the survivor nears them, after the exits are rolled, and a <see cref="PowerSwitchSite"/> waits for one.</summary>
        internal static bool PlaceSwitch(System.Random rng)
        {
            var spots = new List<Vector3>();
            foreach (LootContainer c in LootContainer.All)
                if (c != null && c.IsFixture && Shelter.IsSheltered(c.transform.position + Vector3.up)) spots.Add(c.transform.position);
            if (spots.Count == 0) return false;
            Physics.SyncTransforms();
            for (int i = 0; i < spots.Count && i < 64; i++)
            {
                int pick = i + rng.Next(spots.Count - i);
                (spots[i], spots[pick]) = (spots[pick], spots[i]);
                if (!OnWall(spots[i], out Vector3 at, out Quaternion facing)) continue;
                var go = new GameObject("PowerSwitch");
                go.transform.SetPositionAndRotation(at, facing);
                GameObject box = CloneZUtil.Box("SwitchBox", at, SwitchSize, new Color(0.55f, 0.5f, 0.2f), go.transform);
                box.transform.localRotation = Quaternion.identity;
                Object.Destroy(box.GetComponent<Collider>());
                go.AddComponent<PowerSwitch>();
                WorldMarkers.Add("exit_switch", at);
                return true;
            }
            return false;
        }

        // A place for the switch box on the nearest wall round a spot: 1.2 m up from the floor under it, on a tall upright
        // surface within reach, its back to the wall and nothing else where it would hang.
        private static bool OnWall(Vector3 near, out Vector3 at, out Quaternion facing)
        {
            at = default;
            facing = Quaternion.identity;
            if (!Floor(near, out float floor)) return false;
            var from = new Vector3(near.x, floor + 1.2f, near.z);
            float best = float.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                if (!Physics.Raycast(from, dir, out RaycastHit wall, 4f, ~0, QueryTriggerInteraction.Ignore) || wall.distance >= best) continue;
                if (Mathf.Abs(wall.normal.y) > 0.2f || wall.collider.bounds.size.y < 2.2f) continue;   // not a wardrobe's side
                Vector3 n = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
                Quaternion q = Quaternion.LookRotation(n);
                Vector3 p = wall.point + n * (SwitchSize.z * 0.5f + 0.005f);
                if (Physics.CheckBox(p + n * 0.01f, SwitchSize * 0.5f - Vector3.one * 0.01f, q, ~0, QueryTriggerInteraction.Ignore)) continue;
                best = wall.distance;
                at = p;
                facing = q;
            }
            return best < float.MaxValue;
        }

        // The floor under a spot: the first broad, level surface below it (not the top of the furniture it's in).
        private static bool Floor(Vector3 near, out float y)
        {
            y = 0f;
            RaycastHit[] hits = Physics.RaycastAll(near + Vector3.up * 0.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in hits)
            {
                Vector3 size = h.collider.bounds.size;
                if (h.normal.y < 0.7f || size.x < 2f || size.z < 2f) continue;
                y = h.point.y;
                return true;
            }
            return false;
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

    /// <summary>Waits for a furnished room to hang a power-gated exit's switch in (see
    /// <see cref="RaidWorldRules.PlaceSwitch"/>), looking every couple of seconds, then goes.</summary>
    public sealed class PowerSwitchSite : MonoBehaviour
    {
        internal System.Random Rng;
        private float _next;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 2f;
            if (RaidWorldRules.PlaceSwitch(Rng)) Destroy(gameObject);
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
            Sfx.Play(SfxIds.SwitchHeavy, transform.position, 0.5f, 0.8f, 30f);   // the breaker trips back out
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
                    Sfx.Play(SfxIds.SwitchHeavy, transform.position, 0.85f, Random.Range(0.95f, 1.05f), 40f);
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
                    Sfx.Play(SfxIds.SwitchHeavy, transform.position, 0.7f, Random.Range(0.82f, 0.88f), 30f);
                    c.Notify("The freight gate powers down.");
                }).Timed(1.5f, "Resetting the switch").DisabledIf(!_on, "Already off"));
            // Inspect action
            into.Add(new InteractAction("Inspect the power switch", c => c.Notify("A rugged switch that powers freight‑gate exits.")));
        }
    }
}
