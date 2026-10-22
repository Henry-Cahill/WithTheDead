using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Lets the survivor work the land. Press F to act on the farm plot you're standing
    /// at — sow seeds into bare soil, water a thirsty crop with a can, or pull a ripe one
    /// — or, with a trowel and no plot in reach, break new ground where you're facing.
    /// Each job is a timed action (like searching a container) that cancels if you walk
    /// off. Farming trains the Farming skill, whose higher levels swell the harvest.
    ///
    /// Unlike build mode this needs no toggle and never touches combat: the work is bound
    /// to the F key, not the mouse, so aiming and swinging stay free while you garden.
    /// </summary>
    public class PlayerFarming : MonoBehaviour
    {
        private const string TrowelId = "trowel";
        private const string WateringCanId = "wateringcan";

        private enum FarmAction { None, Till, Plant, Water, Harvest, Clear }

        /// <summary>
/// Current on‑screen prompt describing the farming action available.
/// </summary>
public string Prompt { get; private set; } = "";
        /// <summary>
/// True while a farming job is in progress.
/// </summary>
public bool IsBusy { get; private set; }
        /// <summary>
/// Fraction (0‑1) of the current job's timer that has elapsed.
/// </summary>
public float WorkProgress { get; private set; }
        /// <summary>
/// Short label shown while a farming job is being performed.
/// </summary>
public string WorkLabel { get; private set; } = "";

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _inventory = GetComponent<PlayerInventory>();
            _skills = GetComponent<PlayerSkills>();
            _interactor = GetComponent<PlayerInteractor>();
            _keyboard = Keyboard.current;
        }
        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerInteractor _interactor;
        // Cached nearest farm plot to avoid repeated heavy lookups when player is stationary.
        // Cached Keyboard reference to avoid per-frame static property lookup.
        private Keyboard _keyboard;
        private FarmPlot _cachedPlot;
        private Vector3 _cachedPos;

        // Current frame's evaluation.
        private FarmAction _action;
        private FarmPlot _plot;
        private Vector3 _tillPos;
        private string _seedId;
        private FarmCrop _crop;

        // Frozen while a job is in progress.
        private FarmAction _busyAction;
        private FarmPlot _busyPlot;
        private Vector3 _busyTillPos;
        private string _busySeedId;
        private FarmCrop _busyCrop;
        private float _workTimer;
        private float _workTotal;

        private readonly Collider[] _overlapBuf = new Collider[16];

        private FarmPlot GetCachedNearestPlot(Vector3 pos)
        {
            // If the player hasn't moved significantly since last lookup, reuse cached plot.
            // Compare squared distance for performance.
            const float moveThresholdSqr = 0.01f; // ~0.1 units movement tolerance.
            if (_cachedPlot != null && (pos - _cachedPos).sqrMagnitude < moveThresholdSqr)
            {
                return _cachedPlot;
            }
            // Recompute and cache the result.
            _cachedPos = pos;
            _cachedPlot = FarmPlot.Nearest(pos, CloneZConfig.FarmActionRange);
            return _cachedPlot;
        }

        private void Update()
        {
            if (_stats != null && _stats.IsDead) { CancelWork(); Prompt = ""; return; }

            // A full-screen panel (inventory / skills / sleep) or build mode owns input.
            if (UIState.GameplayBlocked || UIState.BuildMode) { CancelWork(); Prompt = ""; return; }

            if (IsBusy) { TickWork(); return; }

            Evaluate();

            Keyboard kb = _keyboard ?? Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame && _action != FarmAction.None)
                StartWork();
        }

        #region Evaluation
        private void Evaluate()
        {
            _action = FarmAction.None;
            _plot = null;
            Prompt = "";

            // Don't fight the container-search (or fixture job) prompt for screen space.
            if (_interactor != null && _interactor.IsBusy) return;

            Vector3 pos = transform.position; pos.y = 0f;
            FarmPlot plot = GetCachedNearestPlot(pos);

            if (plot != null)
            {
                _plot = plot;
                EvaluatePlot(plot);
            }
            else
            {
                EvaluateTill(pos);
            }
        }

        private void EvaluatePlot(FarmPlot plot)
        {
            switch (plot.State)
            {
                case FarmPlot.PlotState.Mature:
                    if (HasTrowel()) { _action = FarmAction.Harvest; Prompt = "[F]  Harvest " + plot.CropName; }
                    else Prompt = "Need a trowel to harvest " + plot.CropName;
                    break;

                case FarmPlot.PlotState.Dead:
                    _action = FarmAction.Clear;
                    Prompt = "[F]  Clear dead crop";
                    break;

                case FarmPlot.PlotState.Empty:
                    if (FindSeed(out _seedId, out _crop))
                    {
                        // Ensure enough seeds for the planting cost before offering the action.
                        if (_inventory != null && _inventory.Has(_seedId, CloneZConfig.FarmSeedCost))
                        {
                            _action = FarmAction.Plant;
                            Prompt = "[F]  Plant " + _crop.Name;
                        }
                        else
                        {
                            Prompt = "Tilled plot \u2014 need " + CloneZConfig.FarmSeedCost + " seeds to sow";
                        }
                    }
                    else Prompt = "Tilled plot \u2014 need seeds to sow";
                    break;

                case FarmPlot.PlotState.Growing:
                    if (plot.NeedsWater && HasCan())
                    {
                        _action = FarmAction.Water;
                        Prompt = "[F]  Water crop   (moisture " + Pct(plot.Water01) + ")";
                    }
                    else if (plot.IsParched && !HasCan())
                    {
                        Prompt = plot.CropName + " wilting \u2014 need a watering can   (growth " + Pct(plot.Growth01) + ")";
                    }
                    else
                    {
                        Prompt = plot.CropName + " growing \u2014 " + Pct(plot.Growth01) + "   (moisture " + Pct(plot.Water01) + ")";
                    }
                    break;
            }
        }

        private void EvaluateTill(Vector3 pos)
        {
            if (!HasTrowel()) return;   // nothing to do bare-handed away from a plot

            // Only offer to till when the survivor stops to work, so the prompt isn't
            // stuck on screen the whole time you're carrying a trowel around.
            if (_controller != null && _controller.CurrentSpeed > 0.15f) return;

            Vector3 aim = _controller != null ? CloneZUtil.Flat(_controller.AimDirection) : CloneZUtil.Flat(transform.forward);
            if (aim == Vector3.zero) aim = CloneZUtil.Flat(transform.forward);
            if (aim == Vector3.zero) aim = Vector3.forward;

            Vector3 spot = ClampToArena(pos + aim * CloneZConfig.FarmTillDistance);

            if (FarmPlot.AnyWithin(spot, CloneZConfig.FarmPlotSpacing))
                Prompt = "Too close to another plot";
            else if (!IsGroundClear(spot))
                Prompt = "Aim at open ground to till a plot";
            else
            {
                _action = FarmAction.Till;
                _tillPos = spot;
                Prompt = "[F]  Till soil";
            }
        }

        // A tilled plot needs clear ground: nothing solid (walls, cover, containers) and no
        // zombie standing on the footprint. Plots themselves have no colliders (AnyWithin covers those).
        private bool IsGroundClear(Vector3 pos)
        {
            float h = CloneZConfig.FarmPlotSize * 0.5f + 0.05f;
            Vector3 center = pos + Vector3.up * 0.6f;              // clears the ground slab (top at y=0)
            Vector3 half = new Vector3(h, 0.5f, h);
            int mask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);

            int n = Physics.OverlapBoxNonAlloc(center, half, _overlapBuf, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlapBuf[i];
                if (c == null) continue;
                if (c.GetComponentInParent<PlayerStats>() != null) continue; // ignore ourselves
                return false;
            }
            return true;
        }

        private bool FindSeed(out string seedId, out FarmCrop crop)
        {
            seedId = null; crop = null;
            if (_inventory == null) return false;
            var stacks = _inventory.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemDef def = stacks[i].Def;
                if (def == null || !def.IsSeed) continue;
                FarmCrop c = FarmCrop.Get(def.CropId);
                if (c == null) continue;
                seedId = def.Id;
                crop = c;
                return true;
            }
            return false;
        }

        private bool HasTrowel() => _inventory != null && _inventory.Has(TrowelId);
        private bool HasCan() => _inventory != null && _inventory.Has(WateringCanId);

        private static Vector3 ClampToArena(Vector3 p)
        {
            float lim = CloneZConfig.ArenaHalfSize - 0.6f;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            p.y = 0f;
            return p;
        }

        private static string Pct(float f01) => Mathf.RoundToInt(Mathf.Clamp01(f01) * 100f) + "%";
        #endregion

        #region Work
        private void StartWork()
        {
            IsBusy = true;
            _workTimer = 0f;
            WorkProgress = 0f;

            _busyAction = _action;
            _busyPlot = _plot;
            _busyTillPos = _tillPos;
            _busySeedId = _seedId;
            _busyCrop = _crop;

            switch (_busyAction)
            {
                case FarmAction.Till: _workTotal = CloneZConfig.FarmTillTime; WorkLabel = "Tilling soil"; break;
                case FarmAction.Plant: _workTotal = CloneZConfig.FarmPlantTime; WorkLabel = "Planting " + (_busyCrop != null ? _busyCrop.Name : "seeds"); break;
                case FarmAction.Water: _workTotal = CloneZConfig.FarmWaterTime; WorkLabel = "Watering"; break;
                case FarmAction.Harvest: _workTotal = CloneZConfig.FarmHarvestTime; WorkLabel = "Harvesting " + (_busyPlot != null ? _busyPlot.CropName : "crop"); break;
                case FarmAction.Clear: _workTotal = CloneZConfig.FarmClearTime; WorkLabel = "Clearing plot"; break;
                default: IsBusy = false; return;
            }
            _workTotal = Mathf.Max(0.1f, _workTotal);
        }

        private void TickWork()
        {
            // Walking away, or losing the tools / target, aborts the job.
            bool moved = _controller != null && _controller.CurrentSpeed > 0.15f;
            if (moved || !StillValid()) { CancelWork(); return; }

            _workTimer += Time.deltaTime;
            WorkProgress = Mathf.Clamp01(_workTimer / _workTotal);
            Prompt = WorkLabel + "\u2026   [move to cancel]";

            if (_workTimer >= _workTotal) CompleteWork();
        }

        private bool StillValid()
        {
            switch (_busyAction)
            {
                case FarmAction.Till:
                    return HasTrowel();
                case FarmAction.Plant:
                    return _busyPlot != null && _busyPlot.State == FarmPlot.PlotState.Empty
                        && _inventory != null && _inventory.Has(_busySeedId, CloneZConfig.FarmSeedCost);
                case FarmAction.Water:
                    return _busyPlot != null && _busyPlot.State == FarmPlot.PlotState.Growing && HasCan();
                case FarmAction.Harvest:
                    return _busyPlot != null && _busyPlot.State == FarmPlot.PlotState.Mature && HasTrowel();
                case FarmAction.Clear:
                    return _busyPlot != null && _busyPlot.State == FarmPlot.PlotState.Dead;
                default:
                    return false;
            }
        }

        private void CompleteWork()
        {
            switch (_busyAction)
            {
                case FarmAction.Till:
                    FarmPlot.Spawn(null, _busyTillPos);
                    AwardXp(CloneZConfig.XpFarmTill);
                    _inventory.WearTool(TrowelId);
                    break;

                case FarmAction.Plant:
                    _inventory.Consume(_busySeedId, CloneZConfig.FarmSeedCost);
                    if (_busyPlot != null) _busyPlot.Plant(_busyCrop);
                    AwardXp(CloneZConfig.XpFarmPlant);
                    break;

                case FarmAction.Water:
                    if (_busyPlot != null) _busyPlot.AddWater();
                    _inventory.WearTool(WateringCanId);
                    break;

                case FarmAction.Harvest:
                    if (_busyPlot != null)
                    {
                        float yieldMult = _skills != null ? _skills.FarmingYieldMult : 1f;
                        int n = _busyPlot.Harvest(yieldMult, out string produceId);
                        if (n > 0 && !string.IsNullOrEmpty(produceId))
                        {
                            _inventory.AddStarter(produceId, n);   // adds without a weight check (like a cooked serving)
                            AwardXp(CloneZConfig.XpFarmHarvestPerCrop * n);
                        }
                    }
                    _inventory.WearTool(TrowelId);
                    break;

                case FarmAction.Clear:
                    if (_busyPlot != null) _busyPlot.Clear();
                    break;
            }

            IsBusy = false;
            WorkProgress = 0f;
            Prompt = "";
        }

        private void CancelWork()
        {
            IsBusy = false;
            WorkProgress = 0f;
            _busyPlot = null;
        }

        private void AwardXp(float amount)
        {
            if (_skills != null) _skills.AddXp(PlayerSkills.Skill.Farming, amount);
        }
        #endregion
    }
}
