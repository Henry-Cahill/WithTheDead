using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Lets the survivor craft barricades from planks and nails (a hammer must be in the
    /// pack). Press B to toggle build mode: aim to position a new wall and left-click to
    /// hammer it up, or face an existing barricade to nail on another plank. The left-click
    /// build is a timed action (like searching) that cancels if you move, and the hammering
    /// draws zombies. B or Esc leaves build mode; combat is held while it is on.
    /// </summary>
    public class PlayerBuilder : MonoBehaviour
    {
        private const string HammerId = "hammer";
        private const string PlankId = "plank";
        private const string NailsId = "nails";

        public string Prompt { get; private set; } = "";
        public bool BuildMode { get; private set; }
        public bool IsBuilding { get; private set; }
        public float BuildProgress { get; private set; }
        public string BuildLabel { get; private set; } = "";

        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerInteractor _interactor;

        // Current frame's evaluation.
        private Barricade _reinforceTarget;
        private Vector3 _candidatePos;
        private Vector3 _candidateForward;
        private bool _placeValid;

        // Frozen while a build is in progress.
        private bool _buildIsReinforce;
        private Barricade _buildReinforce;
        private Vector3 _buildPos;
        private Vector3 _buildForward;
        private float _buildTimer;
        private float _buildTotal;
        private float _hammerTimer;

        private GameObject _ghost;
        private Renderer _ghostRenderer;
        private readonly Collider[] _overlapBuf = new Collider[16];

        private static readonly Color GhostReady = new Color(0.35f, 0.9f, 0.4f, 1f);
        private static readonly Color GhostShort = new Color(0.9f, 0.7f, 0.25f, 1f);

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

            Evaluate();

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && CanBuildNow())
                StartBuild();
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
                Vector3 candidate = origin + aim * CloneZConfig.BarricadePlaceDistance;
                candidate = ClampToArena(candidate);
                _candidatePos = candidate;
                _candidateForward = aim;
                _placeValid = IsSpotClear(candidate, aim);
                UpdateGhost();
            }

            Prompt = ComposePrompt();
        }

        private string ComposePrompt()
        {
            // Don't fight the container-search prompt for screen space.
            if (_interactor != null && _interactor.IsSearching) return "";
            return CoreBuildMessage() + "      [B / Esc]  exit build mode";
        }

        private string CoreBuildMessage()
        {
            bool hasHammer = HasHammer();
            bool hasMats = HasMaterials();

            if (_reinforceTarget != null)
            {
                if (!hasHammer) return "Need a Hammer to reinforce";
                if (!hasMats) return "Need " + CostText() + " to reinforce";
                return "[LMB]  Reinforce barricade  (" + _reinforceTarget.Planks + "/" + CloneZConfig.BarricadeMaxPlanks + ")";
            }

            if (_placeValid)
            {
                if (!hasHammer) return "Need a Hammer to build";
                if (!hasMats) return "Need " + CostText() + " to build";
                return "[LMB]  Build barricade  (" + CostText() + ")";
            }
            return "Aim at a clear spot to build";
        }

        private static string CostText()
        {
            string planks = CloneZConfig.BarricadePlankCost == 1 ? "1 plank" : CloneZConfig.BarricadePlankCost + " planks";
            string nails = CloneZConfig.BarricadeNailCost == 1 ? "1 box of nails" : CloneZConfig.BarricadeNailCost + " boxes of nails";
            return planks + " + " + nails;
        }

        private bool CanBuildNow()
        {
            if (_interactor != null && _interactor.IsSearching) return false;
            if (!HasHammer() || !HasMaterials()) return false;
            return _reinforceTarget != null ? _reinforceTarget.CanReinforce : _placeValid;
        }

        private bool HasHammer() => _inventory != null && _inventory.Has(HammerId);
        private bool HasMaterials() => _inventory != null
            && _inventory.Has(PlankId, CloneZConfig.BarricadePlankCost)
            && _inventory.Has(NailsId, CloneZConfig.BarricadeNailCost);

        private static Vector3 ClampToArena(Vector3 p)
        {
            float lim = CloneZConfig.ArenaHalfSize - 0.4f;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            p.y = 0f;
            return p;
        }

        // A spot is buildable when nothing solid (walls, cover, containers, other
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

            float speedMult = _skills != null ? _skills.CarpentryBuildTimeMult : 1f;
            _buildTotal = Mathf.Max(0.1f, CloneZConfig.BarricadeBuildTime * speedMult);

            _buildIsReinforce = _reinforceTarget != null;
            _buildReinforce = _reinforceTarget;
            _buildPos = _buildIsReinforce ? _reinforceTarget.Position : _candidatePos;
            _buildForward = _candidateForward;
            BuildLabel = _buildIsReinforce ? "Reinforcing barricade" : "Building barricade";
            HideGhost();
        }

        private void TickBuild()
        {
            // Moving, running out of materials, or losing the target all abort the job.
            bool moved = _controller != null && _controller.CurrentSpeed > 0.15f;
            bool targetGone = _buildIsReinforce && (_buildReinforce == null || !_buildReinforce.CanReinforce);
            if (moved || targetGone || !HasHammer() || !HasMaterials())
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

            _inventory.Consume(PlankId, CloneZConfig.BarricadePlankCost);
            _inventory.Consume(NailsId, CloneZConfig.BarricadeNailCost);

            if (_buildIsReinforce)
            {
                if (_buildReinforce != null) _buildReinforce.AddPlank();
            }
            else
            {
                float healthMult = _skills != null ? _skills.CarpentryHealthMult : 1f;
                Barricade.Spawn(null, _buildPos, _buildForward, healthMult);
            }

            if (_skills != null) _skills.AddXp(PlayerSkills.Skill.Carpentry, CloneZConfig.XpCarpentryPerPlank);
            NoiseSystem.Emit(_buildPos, CloneZConfig.NoiseHammering);
            _inventory.WearTool(HammerId);   // hammering a barricade wears the hammer down
            Prompt = "";
        }

        private void CancelBuild()
        {
            IsBuilding = false;
            BuildProgress = 0f;
            _buildReinforce = null;
        }
        #endregion

        #region Ghost preview
        private void BuildGhost()
        {
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "BarricadeGhost";
            _ghost.transform.SetParent(transform);   // torn down with the player on restart
            var col = _ghost.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _ghost.transform.localScale = new Vector3(CloneZConfig.BarricadeWidth, 0.06f, CloneZConfig.BarricadeDepth + 0.1f);
            _ghostRenderer = _ghost.GetComponent<Renderer>();
            _ghostRenderer.sharedMaterial = CloneZUtil.UnlitMat(GhostReady);
            _ghost.SetActive(false);
        }

        private void UpdateGhost()
        {
            if (_ghost == null) return;
            if (!_placeValid) { HideGhost(); return; }

            _ghost.transform.SetPositionAndRotation(
                _candidatePos + Vector3.up * 0.03f,
                Quaternion.LookRotation(_candidateForward, Vector3.up));

            Color c = (HasHammer() && HasMaterials()) ? GhostReady : GhostShort;
            _ghostRenderer.sharedMaterial.color = c;
            if (_ghostRenderer.sharedMaterial.HasProperty("_BaseColor"))
                _ghostRenderer.sharedMaterial.SetColor("_BaseColor", c);
            _ghost.SetActive(true);
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
