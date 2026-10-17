using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// The survivor's construction tool. Press B to toggle build mode, then pick from the
    /// palette (mouse wheel, or 1–7): a defensive <see cref="Barricade"/>, the house pieces —
    /// floor, wall, doorway, roof and window — or Demolish to tear a structure back down. Barricades
    /// are aimed and hammered up (or an existing one is reinforced with another plank); house
    /// pieces snap to a 2 m tile grid under the cursor so they assemble into a building;
    /// Demolish pries whatever the cursor is over apart, recovering some of its planks and
    /// nails. Every action costs (or refunds) materials, needs a hammer in the pack, is a timed
    /// job that cancels if you move, and the hammering draws zombies. B or Esc leaves build
    /// mode; combat is held while it is on.
    /// </summary>
    public class PlayerBuilder : MonoBehaviour
    {
        private const string HammerId = "hammer";
        private const string PlankId = "plank";
        private const string NailsId = "nails";

        /// <summary>The build palette: a barricade, the five house pieces, and a demolish tool.</summary>
        public enum BuildSelection { Barricade, Floor, Wall, Doorway, Roof, Window, Demolish }
        private const int SelectionCount = 7;

        public string Prompt { get; private set; } = "";
        public bool BuildMode { get; private set; }
        public bool IsBuilding { get; private set; }
        public float BuildProgress { get; private set; }
        public string BuildLabel { get; private set; } = "";
        public BuildSelection Selection { get; private set; } = BuildSelection.Barricade;

        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerInteractor _interactor;
        private Camera _cam;

        // Current frame's barricade evaluation.
        private Barricade _reinforceTarget;
        private Vector3 _candidatePos;
        private Vector3 _candidateForward;
        private bool _placeValid;

        // Current frame's house-piece evaluation.
        private Vector3 _houseCenter;      // occupancy centre from BuildPiece.CenterFor
        private float _houseYaw;

        // Current frame's demolish target (the structure or world object under the cursor).
        private IDemolishable _demolishTarget;

        // Frozen while a build is in progress.
        private BuildSelection _buildSelection;
        private bool _buildIsReinforce;
        private Barricade _buildReinforce;
        private IDemolishable _buildDemolishTarget;
        private Vector3 _buildPos;
        private Vector3 _buildForward;
        private float _buildTimer;
        private float _buildTotal;
        private float _hammerTimer;

        private GameObject _ghost;
        private Renderer _ghostRenderer;
        private readonly Collider[] _overlapBuf = new Collider[16];
        private readonly RaycastHit[] _rayBuf = new RaycastHit[32];

        private static readonly Color GhostReady = new Color(0.35f, 0.9f, 0.4f, 1f);
        private static readonly Color GhostShort = new Color(0.9f, 0.7f, 0.25f, 1f);
        private static readonly Color GhostDemolish = new Color(0.95f, 0.35f, 0.25f, 1f);

        private Camera Cam
        {
            get { if (_cam == null) _cam = Camera.main; return _cam; }
        }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _inventory = GetComponent<PlayerInventory>();
            _skills = GetComponent<PlayerSkills>();
            _interactor = GetComponent<PlayerInteractor>();
            BuildGhost();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (_stats != null && _stats.IsDead) { ExitBuildMode(); return; }

            // Any full-screen panel (inventory / skills / sleep) forces build mode off.
            if (UIState.GameplayBlocked) { ExitBuildMode(); return; }

            // B flips build mode on or off, abandoning any hammering in progress.
            if (kb != null && kb.bKey.wasPressedThisFrame)
            {
                if (BuildMode) ExitBuildMode(); else EnterBuildMode();
                return;
            }

            if (!BuildMode) { Prompt = ""; return; }

            // Esc backs out: cancel the current job first, otherwise leave build mode.
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (IsBuilding) CancelBuild(); else ExitBuildMode();
                return;
            }

            if (IsBuilding) { TickBuild(); return; }

            HandleSelectionInput(kb, mouse);
            Evaluate();

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && CanBuildNow())
                StartBuild();
        }

        // Mouse wheel cycles the palette; the number keys jump straight to a piece.
        private void HandleSelectionInput(Keyboard kb, Mouse mouse)
        {
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Selection = BuildSelection.Barricade;
                else if (kb.digit2Key.wasPressedThisFrame) Selection = BuildSelection.Floor;
                else if (kb.digit3Key.wasPressedThisFrame) Selection = BuildSelection.Wall;
                else if (kb.digit4Key.wasPressedThisFrame) Selection = BuildSelection.Doorway;
                else if (kb.digit5Key.wasPressedThisFrame) Selection = BuildSelection.Roof;
                else if (kb.digit6Key.wasPressedThisFrame) Selection = BuildSelection.Window;
                else if (kb.digit7Key.wasPressedThisFrame) Selection = BuildSelection.Demolish;
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.5f) Selection = (BuildSelection)(((int)Selection + 1) % SelectionCount);
                else if (scroll < -0.5f) Selection = (BuildSelection)(((int)Selection + SelectionCount - 1) % SelectionCount);
            }
        }

        private void EnterBuildMode()
        {
            BuildMode = true;
            UIState.BuildMode = true;
        }

        private void ExitBuildMode()
        {
            if (IsBuilding) CancelBuild();
            if (BuildMode)
            {
                BuildMode = false;
                UIState.BuildMode = false;
            }
            HideGhost();
            Prompt = "";
        }

        #region Evaluation
        private void Evaluate()
        {
            if (Selection == BuildSelection.Barricade) EvaluateBarricade();
            else if (Selection == BuildSelection.Demolish) EvaluateDemolish();
            else EvaluateHouse();
            Prompt = ComposePrompt();
        }

        private void EvaluateBarricade()
        {
            Vector3 aim = _controller != null ? CloneZUtil.Flat(_controller.AimDirection) : CloneZUtil.Flat(transform.forward);
            if (aim == Vector3.zero) aim = CloneZUtil.Flat(transform.forward);
            if (aim == Vector3.zero) aim = Vector3.forward;

            Vector3 origin = transform.position; origin.y = 0f;

            // Prefer reinforcing a barricade we're standing at and facing.
            _reinforceTarget = null;
            Barricade near = Barricade.Nearest(origin, CloneZConfig.BarricadeBuildRange, out _);
            if (near != null && near.CanReinforce)
            {
                Vector3 toB = CloneZUtil.Flat(near.Position - origin);
                if (Vector3.Dot(toB, aim) > 0.1f) _reinforceTarget = near;
            }

            if (_reinforceTarget != null)
            {
                _placeValid = false;
                HideGhost();
            }
            else
            {
                Vector3 candidate = ClampToArena(origin + aim * CloneZConfig.BarricadePlaceDistance);
                _candidatePos = candidate;
                _candidateForward = aim;
                _placeValid = IsSpotClear(candidate, aim);
                UpdateGhost();
            }
        }

        // House pieces snap to a 2 m tile grid under the cursor: floors/roofs to a cell centre,
        // walls/doorways to the nearest tile edge (so they wrap the floors).
        private void EvaluateHouse()
        {
            _reinforceTarget = null;
            BuildPieceType type = PieceType;
            Vector3 ground = SnapHouseGround(type, out float yaw);
            _houseYaw = yaw;
            _houseCenter = BuildPiece.CenterFor(type, ground);
            _placeValid = HousePlaceValid(type, _houseCenter, yaw);
            UpdateGhost();
        }

        // Demolish targets whatever demolishable thing the cursor is over: a ray takes the nearest
        // demolishable collider (barricades, house pieces, containers, cover), skipping the survivor's
        // own body and the ground; if it finds none, a proximity check catches the collider-less
        // furniture (bed, stove, plot, water) and any built piece the ray happened to miss.
        private void EvaluateDemolish()
        {
            _reinforceTarget = null;
            _demolishTarget = null;

            Vector3 me = transform.position; me.y = 0f;
            float reach = CloneZConfig.HousePlaceReach + 1f;

            Camera cam = Cam;
            Mouse mouse = Mouse.current;
            if (cam != null && mouse != null)
            {
                // Gather every collider under the cursor (the crosshair in the shoulder / first-person
                // views) and take the NEAREST demolishable one, so the ray isn't stopped by the survivor's
                // own body, the ground, or something in front of the thing you're pointing at (built
                // pieces often sit right next to you).
                Vector2 sp = ViewState.MouseLook ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : mouse.position.ReadValue();
                Ray ray = cam.ScreenPointToRay(sp);
                int n = Physics.RaycastNonAlloc(ray, _rayBuf, 200f, 1 << 0, QueryTriggerInteraction.Ignore);
                float reachSqr = reach * reach;
                float nearest = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    RaycastHit h = _rayBuf[i];
                    if (h.distance >= nearest) continue;
                    IDemolishable d = h.collider.GetComponentInParent<IDemolishable>();
                    if (d == null) continue;
                    Vector3 flat = h.point; flat.y = 0f;
                    if ((flat - me).sqrMagnitude > reachSqr) continue;
                    nearest = h.distance;
                    _demolishTarget = d;
                }
            }

            // Fallback: nearest demolishable near the cursor — also catches the collider-less furniture
            // the ray passes straight through, and any built piece the ray happened to miss.
            if (_demolishTarget == null)
                _demolishTarget = NearestDemolishableNearCursor(GroundCursor(), me, reach);

            _placeValid = _demolishTarget != null;
            UpdateGhost();
        }

        // The ground point under the cursor (used to pick collider-less world objects to demolish).
        private Vector3 GroundCursor()
        {
            Vector3 p = _controller != null ? _controller.AimPoint : transform.position + CloneZUtil.Flat(transform.forward) * 2f;
            p.y = 0f;
            return p;
        }

        // Nearest demolishable to the cursor within a small pick radius and the survivor's reach.
        // Covers the collider-less furniture (bed, plot) that a ray passes through, plus the player's
        // own builds and the home's furniture as a safety net when the ray misses them.
        private IDemolishable NearestDemolishableNearCursor(Vector3 cursor, Vector3 me, float reach)
        {
            float bestSqr = 1.4f * 1.4f;   // pick radius around the cursor
            IDemolishable best = null;
            ConsiderRegistry(SleepSpot.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(CookingStation.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(FarmPlot.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(WaterSource.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(Barricade.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(BuildPiece.All, cursor, me, reach, ref bestSqr, ref best);
            ConsiderRegistry(HomeFurniture.All, cursor, me, reach, ref bestSqr, ref best);
            return best;
        }

        private static void ConsiderRegistry<T>(List<T> list, Vector3 cursor, Vector3 me, float reach, ref float bestSqr, ref IDemolishable best)
            where T : Component
        {
            float reachSqr = reach * reach;
            for (int i = 0; i < list.Count; i++)
            {
                T c = list[i];
                if (c == null || c is not IDemolishable d) continue;
                Vector3 p = c.transform.position;
                float dc = (p.x - cursor.x) * (p.x - cursor.x) + (p.z - cursor.z) * (p.z - cursor.z);
                if (dc >= bestSqr) continue;
                float dm = (p.x - me.x) * (p.x - me.x) + (p.z - me.z) * (p.z - me.z);
                if (dm > reachSqr) continue;
                bestSqr = dc; best = d;
            }
        }

        private Vector3 SnapHouseGround(BuildPieceType type, out float yaw)
        {
            Vector3 origin = transform.position; origin.y = 0f;
            Vector3 p = _controller != null ? _controller.AimPoint : origin + CloneZUtil.Flat(transform.forward) * 2f;
            p.y = 0f;

            // Keep placement within arm's reach of the survivor.
            Vector3 to = p - origin;
            float reach = CloneZConfig.HousePlaceReach;
            if (to.sqrMagnitude > reach * reach) p = origin + to.normalized * reach;

            float T = CloneZConfig.HouseTileSize;
            yaw = 0f;
            if (type == BuildPieceType.Wall || type == BuildPieceType.Doorway || type == BuildPieceType.Window)
            {
                // Nearest vertical edge (wall runs N–S) vs horizontal edge (wall runs E–W).
                float vx = Mathf.Round((p.x - T * 0.5f) / T) * T + T * 0.5f;
                float vz = Mathf.Round(p.z / T) * T;
                float hx = Mathf.Round(p.x / T) * T;
                float hz = Mathf.Round((p.z - T * 0.5f) / T) * T + T * 0.5f;
                float vDist = (p.x - vx) * (p.x - vx) + (p.z - vz) * (p.z - vz);
                float hDist = (p.x - hx) * (p.x - hx) + (p.z - hz) * (p.z - hz);
                if (vDist <= hDist) { yaw = 90f; return ClampToArena(new Vector3(vx, 0f, vz)); }
                yaw = 0f; return ClampToArena(new Vector3(hx, 0f, hz));
            }

            // Floor / roof: snap to the cell centre.
            return ClampToArena(new Vector3(Mathf.Round(p.x / T) * T, 0f, Mathf.Round(p.z / T) * T));
        }

        private bool HousePlaceValid(BuildPieceType type, Vector3 center, float yaw)
        {
            if (BuildPiece.Occupied(center, CloneZConfig.HousePieceSnapEpsilon)) return false;
            if (type == BuildPieceType.Wall || type == BuildPieceType.Doorway || type == BuildPieceType.Window) return HouseWallClear(center, yaw);
            return true;   // floors / roofs only need an empty slot on their layer
        }

        // A wall/doorway slot is clear when no world obstacle (perimeter walls, cover, containers)
        // and no zombie sits in the footprint. Our own house pieces, barricades and the survivor
        // are ignored, so walls can sit on floor edges and butt up against each other.
        private bool HouseWallClear(Vector3 center, float yaw)
        {
            float T = CloneZConfig.HouseTileSize;
            float h = CloneZConfig.HouseWallHeight;
            float thick = CloneZConfig.HouseWallThickness;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            var half = new Vector3(T * 0.5f - 0.06f, h * 0.5f - 0.05f, thick * 0.5f);
            int mask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);

            int n = Physics.OverlapBoxNonAlloc(center, half, _overlapBuf, rot, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlapBuf[i];
                if (c == null) continue;
                if (c.GetComponentInParent<PlayerStats>() != null) continue;
                if (c.GetComponentInParent<BuildPiece>() != null) continue;
                if (c.GetComponentInParent<Barricade>() != null) continue;
                return false;
            }
            return true;
        }

        private string ComposePrompt()
        {
            // Don't fight the container-search prompt for screen space.
            if (_interactor != null && _interactor.IsSearching) return "";
            return "\u00AB " + PieceName(Selection) + " \u00BB   " + CoreBuildMessage()
                 + "   \u00B7   wheel/1-7 \u00B7 B/Esc exit";
        }

        private string CoreBuildMessage()
        {
            if (!HasHammer()) return "Need a Hammer in your pack";

            if (Selection == BuildSelection.Demolish)
            {
                if (_demolishTarget == null) return "Aim at a structure or object to remove";
                if (!_demolishTarget.CanDemolish(out string why)) return why ?? "Can't dismantle that";
                SalvagePreview(out int p, out int n, out string name);
                string recovers = (p <= 0 && n <= 0) ? "  (no materials to recover)" : "  (recovers " + SalvageText(p, n) + ")";
                return "[LMB] Dismantle " + name + recovers;
            }

            if (Selection == BuildSelection.Barricade)
            {
                if (_reinforceTarget != null)
                {
                    if (!HasMaterials(Selection)) return "Need " + CostText(Selection) + " to reinforce";
                    return "[LMB] Reinforce  (" + _reinforceTarget.Planks + "/" + CloneZConfig.BarricadeMaxPlanks + ")";
                }
                if (!_placeValid) return "Aim at a clear spot";
                if (!HasMaterials(Selection)) return "Need " + CostText(Selection);
                return "[LMB] Build barricade  (" + CostText(Selection) + ")";
            }

            if (!_placeValid) return "Move the cursor to a clear tile";
            if (!HasMaterials(Selection)) return "Need " + CostText(Selection);
            return "[LMB] Place " + PieceName(Selection).ToLower() + "  (" + CostText(Selection) + ")";
        }

        private static string CostText(BuildSelection s)
        {
            int pc = PlankCost(s), nc = NailCost(s);
            string planks = pc == 1 ? "1 plank" : pc + " planks";
            string nails = nc == 1 ? "1 box of nails" : nc + " boxes of nails";
            return planks + " + " + nails;
        }

        // How many planks/nails prying the currently-aimed target apart would return.
        private void SalvagePreview(out int planks, out int nails, out string name)
        {
            planks = 0; nails = 0; name = "structure";
            if (_demolishTarget != null)
            {
                _demolishTarget.GetSalvage(out planks, out nails);
                name = _demolishTarget.DemolishName;
            }
        }

        private static string SalvageText(int planks, int nails)
        {
            if (planks <= 0 && nails <= 0) return "nothing usable";
            string p = planks == 1 ? "1 plank" : planks + " planks";
            string n = nails == 1 ? "1 box of nails" : nails + " boxes of nails";
            if (planks <= 0) return n;
            if (nails <= 0) return p;
            return p + " + " + n;
        }

        private bool CanBuildNow()
        {
            if (_interactor != null && _interactor.IsSearching) return false;
            if (!HasHammer()) return false;
            if (Selection == BuildSelection.Demolish) return _demolishTarget != null && _demolishTarget.CanDemolish(out _);
            if (!HasMaterials(Selection)) return false;
            if (Selection == BuildSelection.Barricade)
                return _reinforceTarget != null ? _reinforceTarget.CanReinforce : _placeValid;
            return _placeValid;
        }

        private bool HasHammer() => _inventory != null && _inventory.Has(HammerId);
        private bool HasMaterials(BuildSelection s) => _inventory != null
            && _inventory.Has(PlankId, PlankCost(s))
            && _inventory.Has(NailsId, NailCost(s));

        private BuildPieceType PieceType => (BuildPieceType)((int)Selection - 1);

        private static int PlankCost(BuildSelection s) => s switch
        {
            BuildSelection.Barricade => CloneZConfig.BarricadePlankCost,
            BuildSelection.Floor => CloneZConfig.HouseFloorPlankCost,
            BuildSelection.Wall => CloneZConfig.HouseWallPlankCost,
            BuildSelection.Doorway => CloneZConfig.HouseDoorwayPlankCost,
            BuildSelection.Roof => CloneZConfig.HouseRoofPlankCost,
            BuildSelection.Window => CloneZConfig.HouseWindowPlankCost,
            _ => 0,   // Demolish
        };

        private static int NailCost(BuildSelection s) => s switch
        {
            BuildSelection.Barricade => CloneZConfig.BarricadeNailCost,
            BuildSelection.Floor => CloneZConfig.HouseFloorNailCost,
            BuildSelection.Wall => CloneZConfig.HouseWallNailCost,
            BuildSelection.Doorway => CloneZConfig.HouseDoorwayNailCost,
            BuildSelection.Roof => CloneZConfig.HouseRoofNailCost,
            BuildSelection.Window => CloneZConfig.HouseWindowNailCost,
            _ => 0,   // Demolish
        };

        private static float BuildTimeFor(BuildSelection s) => s switch
        {
            BuildSelection.Barricade => CloneZConfig.BarricadeBuildTime,
            BuildSelection.Floor => CloneZConfig.HouseFloorBuildTime,
            BuildSelection.Wall => CloneZConfig.HouseWallBuildTime,
            BuildSelection.Doorway => CloneZConfig.HouseDoorwayBuildTime,
            BuildSelection.Roof => CloneZConfig.HouseRoofBuildTime,
            BuildSelection.Window => CloneZConfig.HouseWindowBuildTime,
            _ => CloneZConfig.HouseSalvageTime,   // Demolish
        };

        private static string PieceName(BuildSelection s) => s switch
        {
            BuildSelection.Barricade => "Barricade",
            BuildSelection.Floor => "Floor",
            BuildSelection.Wall => "Wall",
            BuildSelection.Doorway => "Doorway",
            BuildSelection.Roof => "Roof",
            BuildSelection.Window => "Window",
            _ => "Demolish",
        };

        private static Vector3 ClampToArena(Vector3 p)
        {
            float lim = CloneZConfig.ArenaHalfSize - 0.4f;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            p.y = 0f;
            return p;
        }

        // A barricade spot is buildable when nothing solid (walls, cover, containers, other
        // barricades) and no zombie already occupies the footprint.
        private bool IsSpotClear(Vector3 pos, Vector3 forward)
        {
            float w = CloneZConfig.BarricadeWidth;
            float d = CloneZConfig.BarricadeDepth;
            Vector3 center = pos + Vector3.up * 0.9f;                 // clears the ground slab (top at y=0)
            Vector3 half = new Vector3(w * 0.5f + 0.05f, 0.8f, d * 0.5f + 0.05f);
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            int mask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);

            int n = Physics.OverlapBoxNonAlloc(center, half, _overlapBuf, rot, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlapBuf[i];
                if (c == null) continue;
                if (c.GetComponentInParent<PlayerStats>() != null) continue; // ignore ourselves
                if (c.GetComponentInParent<BuildPiece>() != null) continue;  // fine to barricade a doorway/among house pieces
                return false;
            }
            return true;
        }
        #endregion

        #region Building
        private void StartBuild()
        {
            IsBuilding = true;
            _buildTimer = 0f;
            _hammerTimer = 0f;
            BuildProgress = 0f;
            _buildSelection = Selection;

            float speedMult = _skills != null ? _skills.CarpentryBuildTimeMult : 1f;
            _buildTotal = Mathf.Max(0.1f, BuildTimeFor(Selection) * speedMult);

            _buildDemolishTarget = null;

            if (Selection == BuildSelection.Barricade)
            {
                _buildIsReinforce = _reinforceTarget != null;
                _buildReinforce = _reinforceTarget;
                _buildPos = _buildIsReinforce ? _reinforceTarget.Position : _candidatePos;
                _buildForward = _candidateForward;
                BuildLabel = _buildIsReinforce ? "Reinforcing barricade" : "Building barricade";
            }
            else if (Selection == BuildSelection.Demolish)
            {
                _buildIsReinforce = false;
                _buildReinforce = null;
                _buildDemolishTarget = _demolishTarget;
                var mb = _demolishTarget as MonoBehaviour;
                _buildPos = mb != null ? mb.transform.position : transform.position;
                _buildForward = Vector3.forward;
                BuildLabel = "Dismantling " + (_demolishTarget != null ? _demolishTarget.DemolishName : "structure");
            }
            else
            {
                _buildIsReinforce = false;
                _buildReinforce = null;
                _buildPos = _houseCenter;         // occupancy centre (also where hammering noise emits)
                _buildForward = Vector3.forward;
                BuildLabel = "Building " + PieceName(Selection).ToLower();
            }
            HideGhost();
        }

        private void TickBuild()
        {
            // Moving, running out of materials, or losing the target all abort the job.
            bool moved = _controller != null && _controller.CurrentSpeed > 0.15f;
            bool targetGone = _buildSelection == BuildSelection.Demolish
                ? DemolishTargetLost()
                : _buildIsReinforce && (_buildReinforce == null || !_buildReinforce.CanReinforce);
            if (moved || targetGone || !HasHammer() || !HasMaterials(_buildSelection))
            {
                CancelBuild();
                return;
            }

            float dt = Time.deltaTime;
            _buildTimer += dt;
            BuildProgress = _buildTotal > 0f ? Mathf.Clamp01(_buildTimer / _buildTotal) : 1f;
            Prompt = BuildLabel + "\u2026   [move to cancel]";

            // Rhythmic hammering pulses draw the horde while you work.
            _hammerTimer -= dt;
            if (_hammerTimer <= 0f)
            {
                NoiseSystem.Emit(_buildPos, CloneZConfig.NoiseHammering);
                _hammerTimer = 0.4f;
            }

            if (_buildTimer >= _buildTotal) CompleteBuild();
        }

        private void CompleteBuild()
        {
            IsBuilding = false;
            BuildProgress = 0f;

            if (_buildSelection == BuildSelection.Demolish) { CompleteDemolish(); return; }

            _inventory.Consume(PlankId, PlankCost(_buildSelection));
            _inventory.Consume(NailsId, NailCost(_buildSelection));

            float healthMult = _skills != null ? _skills.CarpentryHealthMult : 1f;
            if (_buildSelection == BuildSelection.Barricade)
            {
                if (_buildIsReinforce)
                {
                    if (_buildReinforce != null) _buildReinforce.AddPlank();
                }
                else
                {
                    Barricade.Spawn(null, _buildPos, _buildForward, healthMult);
                }
            }
            else
            {
                BuildPiece.Spawn(null, (BuildPieceType)((int)_buildSelection - 1), _buildPos, _houseYaw, healthMult);
            }

            // Bigger pieces cost more planks, so they train Carpentry proportionally more.
            if (_skills != null) _skills.AddXp(PlayerSkills.Skill.Carpentry, CloneZConfig.XpCarpentryPerPlank * PlankCost(_buildSelection));
            NoiseSystem.Emit(_buildPos, CloneZConfig.NoiseHammering);
            _inventory.WearTool(HammerId);   // hammering wears the hammer down
            Prompt = "";
        }

        // Tears the captured target down, hands back part of its materials, and re-bakes the navmesh
        // if it was a baked-in obstacle (a cover block or a world container).
        private void CompleteDemolish()
        {
            IDemolishable t = _buildDemolishTarget;
            int plankBack = 0, nailBack = 0;
            Vector3 pos = _buildPos;

            if (t != null && t.CanDemolish(out _))
            {
                t.GetSalvage(out plankBack, out nailBack);
                if (t is MonoBehaviour mb && mb != null) pos = mb.transform.position;
                bool carves = t.CarvesNavMesh;
                t.Dismantle();
                if (carves) WorldNavMesh.Rebuild();   // free the space the baked obstacle occupied
            }

            if (plankBack > 0) _inventory.AddStarter(PlankId, plankBack);   // added without a weight check so salvage isn't lost
            if (nailBack > 0) _inventory.AddStarter(NailsId, nailBack);
            if (_skills != null) _skills.AddXp(PlayerSkills.Skill.Carpentry, CloneZConfig.XpCarpentrySalvage);
            NoiseSystem.Emit(pos, CloneZConfig.NoiseHammering);
            _inventory.WearTool(HammerId);

            _buildDemolishTarget = null;
            Prompt = "";
        }

        // True once the target being dismantled is gone (e.g. a zombie smashed the wall mid-swing).
        private bool DemolishTargetLost() => (_buildDemolishTarget as MonoBehaviour) == null;

        private void CancelBuild()
        {
            IsBuilding = false;
            BuildProgress = 0f;
            _buildReinforce = null;
            _buildDemolishTarget = null;
        }
        #endregion

        #region Ghost preview
        private void BuildGhost()
        {
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "BuildGhost";
            _ghost.transform.SetParent(transform);   // torn down with the player on restart
            var col = _ghost.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _ghostRenderer = _ghost.GetComponent<Renderer>();
            _ghostRenderer.sharedMaterial = CloneZUtil.UnlitMat(GhostReady);
            _ghost.SetActive(false);
        }

        private void UpdateGhost()
        {
            if (_ghost == null) return;
            if (!_placeValid) { HideGhost(); return; }

            if (Selection == BuildSelection.Demolish)
            {
                if (!UpdateDemolishHighlight()) { HideGhost(); return; }
                SetGhostColor(GhostDemolish);
                _ghost.SetActive(true);
                return;
            }

            if (Selection == BuildSelection.Barricade)
            {
                _ghost.transform.localScale = new Vector3(CloneZConfig.BarricadeWidth, 0.06f, CloneZConfig.BarricadeDepth + 0.1f);
                _ghost.transform.SetPositionAndRotation(
                    _candidatePos + Vector3.up * 0.03f,
                    Quaternion.LookRotation(_candidateForward, Vector3.up));
            }
            else
            {
                _ghost.transform.localScale = GhostSize(PieceType);
                _ghost.transform.SetPositionAndRotation(_houseCenter, Quaternion.Euler(0f, _houseYaw, 0f));
            }

            SetGhostColor((HasHammer() && HasMaterials(Selection)) ? GhostReady : GhostShort);
            _ghost.SetActive(true);
        }

        // Overlays the ghost box on the target the cursor is pointing at, sized from its renderers,
        // so it's obvious what will be torn down.
        private bool UpdateDemolishHighlight()
        {
            if (!(_demolishTarget is MonoBehaviour mb) || mb == null) return false;

            var rends = mb.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                _ghost.transform.SetPositionAndRotation(mb.transform.position, Quaternion.identity);
                _ghost.transform.localScale = Vector3.one;
                return true;
            }

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            _ghost.transform.SetPositionAndRotation(b.center, Quaternion.identity);
            _ghost.transform.localScale = b.size + new Vector3(0.1f, 0.1f, 0.1f);
            return true;
        }

        private void SetGhostColor(Color c)
        {
            _ghostRenderer.sharedMaterial.color = c;
            if (_ghostRenderer.sharedMaterial.HasProperty("_BaseColor"))
                _ghostRenderer.sharedMaterial.SetColor("_BaseColor", c);
        }

        // A single translucent box sized to the piece's footprint, used to preview placement.
        private static Vector3 GhostSize(BuildPieceType type)
        {
            float t = CloneZConfig.HouseTileSize;
            switch (type)
            {
                case BuildPieceType.Floor: return new Vector3(t, CloneZConfig.HouseFloorThickness, t);
                case BuildPieceType.Roof: return new Vector3(t + 0.15f, CloneZConfig.HouseRoofThickness, t + 0.15f);
                default: return new Vector3(t, CloneZConfig.HouseWallHeight, CloneZConfig.HouseWallThickness); // Wall, Doorway
            }
        }

        private void HideGhost()
        {
            if (_ghost != null && _ghost.activeSelf) _ghost.SetActive(false);
        }

        private void OnDisable()
        {
            if (BuildMode) { BuildMode = false; UIState.BuildMode = false; }
            HideGhost();
        }
        #endregion
    }
}
