using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Everything the survivor reaches out and uses. Each frame it picks the one thing in focus — a fixture of
    /// the home (<see cref="Interactable"/>: a door, a tap, a cupboard, a lamp), a searchable container or an
    /// item on the ground — preferring whatever is under the cursor (the crosshair in the shoulder views), and
    /// otherwise the nearest thing in front of the survivor, among those within reach and in sight. Then it
    /// routes the keys:
    /// <list type="bullet">
    /// <item>E — the focused thing's main action: pick up, search / open, or a fixture's first action.</item>
    /// <item>Q — a fixture's numbered action menu (pick with 1–9 or the mouse; Q / Esc closes it).</item>
    /// <item>I / Tab — the inventory; Esc — cancel, close.</item>
    /// </list>
    /// Searching an unopened container takes time (PZ-style rummaging), as do a fixture's timed actions
    /// (washing up, taking a shower, reading): a bar fills while the survivor stays put, and moving off,
    /// getting hurt or walking away abandons it.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>The live survivor's interactor (fixtures look here to see what the panels show).</summary>
        public static PlayerInteractor Current { get; private set; }

        /// <summary>The searchable container in focus, or null.</summary>
        public LootContainer Target { get; private set; }
        /// <summary>The ground item in focus, or null.</summary>
        public WorldItem PickupTarget { get; private set; }
        /// <summary>The fixture in focus, or null.</summary>
        public Interactable FixtureTarget { get; private set; }
        /// <summary>The HUD prompt text displayed to the player.</summary>
        public string PromptText { get; private set; } = "";

        /// <summary>True when something is in focus; <see cref="FocusPoint"/> / <see cref="FocusName"/> label it on screen.</summary>
                public bool HasFocus => _focus != FocusKind.None;
        /// <summary>The world position of the current focus point for HUD display.</summary>
        public Vector3 FocusPoint { get; private set; }
        /// <summary>The name string for the currently focused interactable.</summary>
        public string FocusName { get; private set; } = "";

        /// <summary>True while a container search is ongoing.</summary>
        public bool IsSearching { get; private set; }
        /// <summary>Current progress (0‑1) of the ongoing container search.</summary>
        public float SearchProgress { get; private set; }
        /// <summary>Name of the container currently being searched, shown in the HUD prompt.</summary>
        public string SearchTargetName { get; private set; } = "";

        /// <summary>The container being rummaged through, or null.</summary>
        public LootContainer SearchTarget => IsSearching ? _searching : null;

        /// <summary>True while a fixture's timed action runs.</summary>
        public bool IsWorking => _job != null;
        /// <summary>True while the survivor is occupied: rummaging or working at a fixture.</summary>
        public bool IsBusy => IsSearching || IsWorking;
        /// <summary>The running job's progress 0..1 (an open-ended job pulses).</summary>
        public float JobProgress { get; private set; }
        /// <summary>The running job's bar text ("Washing up").</summary>
        public string JobLabel { get; private set; } = "";
        /// <summary>True when the running job lasts until stopped.</summary>
        public bool JobOpenEnded => _job != null && _job.IsOpenEnded;
        /// <summary>The survivor's pose for the running job.</summary>
        public HumanoidActivity JobActivity => _job != null ? _job.Activity : HumanoidActivity.None;
        /// <summary>The fixture being worked at, or null.</summary>
        public Interactable JobTarget => _job != null ? _jobTarget : null;

        /// <summary>True while a fixture's action menu is open.</summary>
        public bool MenuOpen => _menuTarget != null;
        /// <summary>The fixture whose action menu is currently open, or null.</summary>
        /// <summary>The interactable whose action menu is currently open, or null.</summary>
        public Interactable MenuTarget => _menuTarget;
        /// <summary>Read‑only list of actions currently shown in the fixture's menu.</summary>
        public IReadOnlyList<InteractAction> MenuActions => _menuActions;
        /// <summary>Moves on whenever the menu's entries change (the HUD rebuilds it then).</summary>
        /// <summary>Incremented each time the menu list changes, forcing the HUD to refresh.</summary>
        public int MenuVersion { get; private set; }

        /// <summary>The container the inventory panel shows right now, or null.</summary>
        /// <summary>The inventory UI controller currently showing a container, or null.</summary>
        public LootContainer ViewedContainer => _ui != null && _ui.IsOpen ? _ui.CurrentContainer : null;

        /// <summary>The survivor as fixtures see them.</summary>
        /// <summary>Context object exposing the survivor to interactable fixtures.</summary>
        public InteractContext Context => _ctx;

        /// <summary>Short messages for the HUD's toasts ("The door is locked").</summary>
        public event System.Action<string> Notice;

        private enum FocusKind { None, Pickup, Container, Fixture }

        private InventoryUIController _ui;
        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerInventory _inventory;
        private Camera _cam;
        private readonly InteractContext _ctx = new InteractContext();

        private LootContainer _searching;
        private float _searchTimer;
        private float _searchTotal;

        private FocusKind _focus;
        private readonly List<InteractAction> _actions = new List<InteractAction>(12);

        private InteractAction _job;
        private Interactable _jobTarget;
        private float _jobT;

        private Interactable _menuTarget;
        private readonly List<InteractAction> _menuActions = new List<InteractAction>(12);
        private int _menuSignature;

        private static readonly int SightMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _inventory = GetComponent<PlayerInventory>();
            Current = this;
            _cam = Camera.main;
        }

        private void Start()
        {
            _ctx.Player = gameObject;
            _ctx.Transform = transform;
            _ctx.Stats = _stats;
            _ctx.Inventory = _inventory;
            _ctx.Interactor = this;
            _ctx.Controller = _controller;
            _ctx.Body = GetComponent<CharacterController>();
            _ctx.Sleep = GetComponent<PlayerSleep>();
            _ctx.Wounds = GetComponent<PlayerWounds>();
            _ctx.Panic = GetComponent<PlayerPanic>();
            _ctx.Temperature = GetComponent<PlayerTemperature>();
            _ctx.Sickness = GetComponent<PlayerFoodSickness>();
            _ctx.Skills = GetComponent<PlayerSkills>();
            _ctx.Seat = GetComponent<PlayerSeat>();
            _ctx.Hydration = GetComponent<PlayerHydration>();
            _cam = Camera.main;
        }

        private void OnEnable()
        {
            Current = this;
            if (_stats == null) _stats = GetComponent<PlayerStats>();
            if (_stats != null) _stats.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDamaged -= OnDamaged;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            UIState.ActionMenuOpen = false;
        }

        /// <summary>Binds the UI controller for inventory interactions.</summary>
        /// <summary>Binds the provided UI controller for inventory interactions.</summary>
        public void Bind(InventoryUIController ui) => _ui = ui;

        /// <summary>Shows a short message on the HUD.</summary>
        /// <summary>Shows a short HUD message (e.g., “The door is locked”).</summary>
        public void Notify(string message)
        {
            if (!string.IsNullOrEmpty(message)) Notice?.Invoke(message);
        }

        /// <summary>Puts the inventory panel away (a cupboard closed while its contents were on screen).</summary>
        /// <summary>Closes the inventory panel (e.g., when a cupboard is hidden).</summary>
        public void CloseInventory()
        {
            if (_ui != null && _ui.IsOpen) _ui.Close();
        }

        /// <summary>Opens the survivor's own inventory panel (where cooking, boiling and crafting happen).</summary>
        /// <summary>Opens the survivor’s personal inventory UI.</summary>
        public void OpenInventory()
        {
            if (_ui == null) return;
            CancelSearch();
            StopJob();
            CloseMenu();
            if (!_ui.IsOpen) _ui.OpenInventoryOnly();
        }

        // A hit while rummaging or working makes the survivor drop what they're doing.
        private void OnDamaged()
        {
            CancelSearch();
            StopJob();
        }

        private void Update()
        {
            if (_stats != null && _stats.IsDead)
            {
                CancelSearch();
                StopJob();
                CloseMenu();
                UIState.ActionMenuOpen = false;
                if (_ui != null && _ui.IsOpen) _ui.Close();
                ClearFocus();
                PromptText = "";
                return;
            }

            // While asleep the world is frozen (GameplayBlocked set, but not by our own
            // inventory panel), so suppress container prompts and input.
            if (UIState.GameplayBlocked && (_ui == null || !_ui.IsOpen))
            {
                CancelSearch();
                StopJob();
                CloseMenu();
                UIState.ActionMenuOpen = false;
                ClearFocus();
                PromptText = "";
                return;
            }

            UpdateFocus();
            if (IsSearching) TickSearch();
            if (IsWorking) TickJob();
            UpdateMenu();
            UpdatePrompt();
            UIState.ActionMenuOpen = MenuOpen;

            Keyboard kb = Keyboard.current;
            if (kb == null || _ui == null) return;

            if (MenuOpen && HandleMenuKeys(kb)) return;

            if (kb.eKey.wasPressedThisFrame)
            {
                if (_ui.IsOpen) _ui.Close();
                else if (IsSearching) CancelSearch();
                else if (IsWorking) StopJob();
                else
                {
                    CloseMenu();
                    ActOnFocus();
                }
            }
            else if (kb.qKey.wasPressedThisFrame)
            {
                if (!_ui.IsOpen && !IsBusy && _focus == FocusKind.Fixture) OpenMenu(FixtureTarget);
            }
            else if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            {
                CancelSearch();
                StopJob();
                CloseMenu();
                if (_ui.IsOpen) _ui.Close();
                else _ui.OpenInventoryOnly();
            }
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                if (IsSearching) CancelSearch();
                else if (IsWorking) StopJob();
                else if (_ui.IsOpen) _ui.Close();
                else return;
                UIState.EscapeConsumedFrame = Time.frameCount;
            }
        }

        #region Focus
        // Scores every candidate in reach: closeness to the cursor on screen (capped, so with the cursor far from
        // everything the nearest thing wins), plus a little for distance, plus a penalty behind the survivor.
        private void UpdateFocus()
        {
            Vector3 p = transform.position;

            Vector2 cursor = CursorPoint();
            float pickRadius = Mathf.Max(40f, Screen.height * CloneZConfig.FixtureCursorPickFraction);
            Vector3 aim = _controller != null ? _controller.AimDirection : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 1e-4f) aim = transform.forward;
            aim.Normalize();

            float best = float.MaxValue;
            FocusKind kind = FocusKind.None;
            Interactable bestFixture = null;
            LootContainer bestContainer = null;
            WorldItem bestItem = null;

            // Fixtures (not while placing things in build mode: the cursor is busy there).
            if (!UIState.BuildMode)
            {
                List<Interactable> all = Interactable.All;
                for (int i = 0; i < all.Count; i++)
                {
                    Interactable f = all[i];
                    if (f == null || !f.IsAvailable) continue;
                    Vector3 ip = f.InteractPoint;
                    if (Mathf.Abs(ip.y - p.y) > CloneZConfig.FixtureReachVertical) continue;
                    float d = Flat(ip - p).magnitude;
                    float reach = f.Reach;
                    if (d > reach) continue;
                    float s = Score(ip, d, reach, cursor, pickRadius, p, aim);
                    if (s >= best || !InSight(ip, f.OcclusionRoot)) continue;
                    best = s;
                    kind = FocusKind.Fixture;
                    bestFixture = f;
                }
            }

            // Containers (a fixture's own storage is reached through the fixture). As before, a container needs
            // only to be within reach — the raid town's sit at floor level among other furniture.
            float range = CloneZConfig.InteractRange;
            List<LootContainer> containers = LootContainer.All;
            for (int i = 0; i < containers.Count; i++)
            {
                LootContainer c = containers[i];
                if (c == null || c.IsFixture) continue;
                Vector3 cp = c.transform.position;
                float d = Flat(cp - p).magnitude;
                if (d > range) continue;
                float s = Score(cp, d, range, cursor, pickRadius, p, aim);
                if (s >= best) continue;
                best = s;
                kind = FocusKind.Container;
                bestContainer = c;
            }

            // Items on the ground: the one nearest the survivor and the one nearest the cursor's ground point
            // (handles are made only for those two, so a floor strewn with things costs nothing extra).
            WorldItem nearMe = WorldItem.Nearest(p, range);
            WorldItem nearCursor = null;
            if (_controller != null && Flat(_controller.AimPoint - p).sqrMagnitude <= (range + 0.7f) * (range + 0.7f))
                nearCursor = WorldItem.Nearest(_controller.AimPoint, 0.7f);
            for (int i = 0; i < 2; i++)
            {
                WorldItem it = i == 0 ? nearMe : nearCursor;
                if (it == null) continue;
                Vector3 ip = it.Position;
                float d = Flat(ip - p).magnitude;
                if (d > range) continue;
                float s = Score(ip, d, range, cursor, pickRadius, p, aim);
                if (s >= best) continue;
                best = s;
                kind = FocusKind.Pickup;
                bestItem = it;
            }

            _focus = kind;
            FixtureTarget = kind == FocusKind.Fixture ? bestFixture : null;
            Target = kind == FocusKind.Container ? bestContainer : null;
            PickupTarget = kind == FocusKind.Pickup ? bestItem : null;
            switch (kind)
            {
                case FocusKind.Fixture: FocusPoint = bestFixture.InteractPoint; FocusName = bestFixture.DisplayName; break;
                case FocusKind.Container: FocusPoint = bestContainer.transform.position; FocusName = bestContainer.DisplayName; break;
                case FocusKind.Pickup: FocusPoint = bestItem.Position; FocusName = bestItem.PickupLabel; break;
                default: FocusName = ""; break;
            }

            // Auto-close a container the player has walked away from.
            if (_ui != null && _ui.IsOpen && _ui.CurrentContainer != null)
            {
                float leave = range * 1.7f;
                if ((_ui.CurrentContainer.transform.position - p).sqrMagnitude > leave * leave)
                    _ui.Close();
            }
        }

        private void ClearFocus()
        {
            _focus = FocusKind.None;
            FixtureTarget = null;
            Target = null;
            PickupTarget = null;
            FocusName = "";
        }

        private Vector2 CursorPoint()
        {
            if (ViewState.MouseLook || Mouse.current == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return Mouse.current.position.ReadValue();
        }

        private float Score(Vector3 point, float dist, float reach, Vector2 cursor, float pickRadius, Vector3 me, Vector3 aim)
        {
            float cap = CloneZConfig.FixtureCursorScoreCap;
            float s = cap;
            if (_cam != null)
            {
                Vector3 sp = _cam.WorldToScreenPoint(point);
                if (sp.z > 0f) s = Mathf.Min(((Vector2)sp - cursor).magnitude / pickRadius, cap);
            }
            s += CloneZConfig.FixtureDistanceWeight * dist / Mathf.Max(0.01f, reach);
            Vector3 to = Flat(point - me);
            if (to.sqrMagnitude > 0.04f && Vector3.Dot(to.normalized, aim) < -0.3f) s += 0.5f;
            return s;
        }

        // Nothing solid between the survivor's chest and the point (short of it, so the fixture's own surface
        // doesn't count) other than what belongs to the thing itself.
        private bool InSight(Vector3 point, Transform own)
        {
            Vector3 from = transform.position + Vector3.up * 1.2f;
            Vector3 to = point - (point - from).normalized * 0.06f;
            if (!Physics.Linecast(from, to, out RaycastHit hit, SightMask, QueryTriggerInteraction.Ignore)) return true;
            Transform t = hit.collider.transform;
            return t.IsChildOf(transform) || (own != null && t.IsChildOf(own));
        }

        /// <summary>True when nothing solid stands between the survivor and <paramref name="point"/> but parts of
        /// <paramref name="own"/> — the sight test that decides what [E] can reach, for whatever else must not work
        /// through a wall (the inventory's list of nearby cupboards, the [G] tap).</summary>
        public bool CanSee(Vector3 point, Transform own) => InSight(point, own);

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private void ActOnFocus()
        {
            switch (_focus)
            {
                case FocusKind.Pickup: TryPickUp(); break;
                case FocusKind.Container: BeginOrOpen(Target); break;
                case FocusKind.Fixture:
                    // If the fixture has no actions, provide a fallback inspect notification.
                    _actions.Clear();
                    FixtureTarget.GetActions(_ctx, _actions);
                    if (_actions.Count == 0)
                    {
                        Notify(FixtureTarget.DisplayName);
                        // Skip performing any primary action.
                    }
                    else
                    {
                        RunPrimary(FixtureTarget);
                    }
                    break;
            }
        }
        #endregion

        #region Fixture actions
        private void RunPrimary(Interactable f)
        {
            if (f == null) return;
            _actions.Clear();
            f.GetActions(_ctx, _actions);
            InteractAction first = FirstEnabled(_actions);
            if (first != null) Perform(f, first);
            else if (_actions.Count > 0) Notify(_actions[0].DisabledReason ?? "You can't do that now");
        }

        private static InteractAction FirstEnabled(List<InteractAction> list)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Enabled) return list[i];
            return null;
        }

        /// <summary>Raised after the local survivor finishes an action on a fixture (the netcode replicates its new state).</summary>
        public static event System.Action<Interactable> ActionRan;

        /// <summary>Carries out <paramref name="a"/> at <paramref name="f"/>: at once, or as a timed job.</summary>
        /// <summary>Performs the given action on a fixture, either instantly or as a timed job.</summary>
        public void Perform(Interactable f, InteractAction a)
        {
            if (f == null || a == null) return;
            if (!a.Enabled)
            {
                Notify(a.DisabledReason ?? "You can't do that now");
                return;
            }
            if (!a.IsTimed)
            {
                a.Run?.Invoke(_ctx);
                ActionRan?.Invoke(f);
                return;
            }
            CancelSearch();
            StopJob();
            _job = a;
            _jobTarget = f;
            _jobT = 0f;
            JobProgress = 0f;
            JobLabel = string.IsNullOrEmpty(a.BusyLabel) ? a.Label : a.BusyLabel;
            a.Begin?.Invoke(_ctx);
        }

        private void TickJob()
        {
            InteractAction job = _job;
            Interactable f = _jobTarget;
            if (job == null) return;

            float leave = (f != null ? f.Reach : CloneZConfig.FixtureReach) * 1.4f;
            bool gone = f == null || !f.IsAvailable || Flat(f.InteractPoint - transform.position).sqrMagnitude > leave * leave;
            bool moved = !job.AllowMove && _controller != null && _controller.CurrentSpeed > CloneZConfig.FixtureMoveCancelSpeed;
            if (gone || moved || UIState.BuildMode)
            {
                StopJob();
                return;
            }

            float dt = Time.deltaTime;
            _jobT += dt;
            if (job.IsOpenEnded)
            {
                JobProgress = 0.55f + 0.45f * Mathf.Sin(_jobT * 2.4f);
                job.Tick?.Invoke(_ctx, dt, _jobT);
                return;
            }

            JobProgress = Mathf.Clamp01(_jobT / job.Duration);
            job.Tick?.Invoke(_ctx, dt, JobProgress);
            if (_job != job) return;   // the tick stopped it
            if (_jobT >= job.Duration)
            {
                ClearJob();
                job.Run?.Invoke(_ctx);
                ActionRan?.Invoke(f);
            }
        }

        /// <summary>Abandons (or, open-ended, finishes) the fixture job in progress.</summary>
        /// <summary>Stops the current fixture job (or finishes an open‑ended one).</summary>
        public void StopJob()
        {
            if (_job == null) return;
            InteractAction job = _job;
            ClearJob();
            job.Stopped?.Invoke(_ctx);
        }

        private void ClearJob()
        {
            _job = null;
            _jobTarget = null;
            _jobT = 0f;
            JobProgress = 0f;
            JobLabel = "";
        }
        #endregion

        #region Action menu
        /// <summary>Opens <paramref name="f"/>'s action menu.</summary>
        /// <summary>Opens the action menu for the given fixture.</summary>
        public void OpenMenu(Interactable f)
        {
            if (f == null) return;
            _menuTarget = f;
            _menuSignature = 0;
            RefreshMenu();
            if (_menuActions.Count == 0) CloseMenu();
        }

        /// <summary>Closes the action menu.</summary>
        /// <summary>Closes any open fixture action menu.</summary>
        public void CloseMenu()
        {
            if (_menuTarget == null) return;
            _menuTarget = null;
            _menuActions.Clear();
            MenuVersion++;
        }

        /// <summary>Carries out entry <paramref name="index"/> of the open menu (and closes it).</summary>
        /// <summary>Selects the menu entry at the given index and performs its action.</summary>
        public void ChooseMenu(int index)
        {
            if (_menuTarget == null || index < 0 || index >= _menuActions.Count) return;
            Interactable f = _menuTarget;
            InteractAction a = _menuActions[index];
            if (!a.Enabled)
            {
                Notify(a.DisabledReason ?? "You can't do that now");
                return;
            }
            CloseMenu();
            Perform(f, a);
        }

        private void UpdateMenu()
        {
            if (_menuTarget == null) return;
            Interactable f = _menuTarget;
            float leave = f.Reach * 1.3f;
            if (!f.IsAvailable || UIState.BuildMode || IsBusy || (_ui != null && _ui.IsOpen)
                || Flat(f.InteractPoint - transform.position).sqrMagnitude > leave * leave)
            {
                CloseMenu();
                return;
            }
            RefreshMenu();
        }

        // Re-lists the menu's actions (their labels follow the fixture's state) and bumps the version on a change.
        private void RefreshMenu()
        {
            _menuActions.Clear();
            _menuTarget.GetActions(_ctx, _menuActions);
            int sig = 17;
            unchecked
            {
                for (int i = 0; i < _menuActions.Count; i++)
                {
                    InteractAction a = _menuActions[i];
                    sig = sig * 31 + (a.Label != null ? a.Label.GetHashCode() : 0);
                    sig = sig * 31 + (a.Enabled ? 1 : 2);
                }
            }
            if (sig != _menuSignature)
            {
                _menuSignature = sig;
                MenuVersion++;
            }
        }

        // 1–9 pick an entry; Q or Esc close the menu. Returns true when the key press was used.
        private bool HandleMenuKeys(Keyboard kb)
        {
            if (kb.qKey.wasPressedThisFrame) { CloseMenu(); return true; }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
                UIState.EscapeConsumedFrame = Time.frameCount;
                return true;
            }
            int n = Mathf.Min(9, _menuActions.Count);
            for (int i = 0; i < n; i++)
            {
                if (DigitPressed(kb, i + 1))
                {
                    ChooseMenu(i);
                    return true;
                }
            }
            return false;
        }

        private static bool DigitPressed(Keyboard kb, int digit)
        {
            switch (digit)
            {
                case 1: return kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
                case 2: return kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
                case 3: return kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame;
                case 4: return kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
                case 5: return kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame;
                case 6: return kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame;
                case 7: return kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame;
                case 8: return kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame;
                case 9: return kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame;
                default: return false;
            }
        }
        #endregion

        #region Containers
        /// <summary>Opens an already-searched container instantly; otherwise starts a timed search.</summary>
        private void BeginOrOpen(LootContainer container)
        {
            if (container == null || _ui == null) return;
            StopJob();
            if (container.Searched || container.SearchTime <= 0f)
            {
                _ui.OpenContainer(container);
                return;
            }
            _searching = container;
            _searchTimer = 0f;
            _searchTotal = container.SearchTime;
            SearchProgress = 0f;
            SearchTargetName = container.DisplayName;
            IsSearching = true;
        }

        /// <summary>Opens a container picked from the inventory's container column (or a fixture's storage): at
        /// once if it has been searched, otherwise after the usual timed rummage, which then opens it in the panel.</summary>
        /// <summary>Starts a timed search of a container or opens it instantly if already searchable.</summary>
        public void SearchOrOpen(LootContainer container)
        {
            if (container == null || _ui == null) return;
            BeginOrOpen(container);
        }

        private void TickSearch()
        {
            // Container gone, out of reach, or the player started moving? Abandon the search.
            float leave = CloneZConfig.InteractRange * 1.3f;
            if (_searching == null ||
                (_searching.transform.position - transform.position).sqrMagnitude > leave * leave ||
                (_controller != null && _controller.CurrentSpeed > 0.15f))
            {
                CancelSearch();
                return;
            }

            _searchTimer += Time.deltaTime;
            SearchProgress = _searchTotal > 0f ? Mathf.Clamp01(_searchTimer / _searchTotal) : 1f;

            if (_searchTimer >= _searchTotal)
            {
                LootContainer done = _searching;
                CancelSearch();
                done.Searched = true;
                if (_ui != null) _ui.OpenContainer(done);
            }
        }

        /// <summary>Abandons any search in progress.</summary>
        /// <summary>Cancels any ongoing container search.</summary>
        public void CancelSearch()
        {
            IsSearching = false;
            SearchProgress = 0f;
            _searching = null;
        }

        /// <summary>Adds the focused ground item to the pack (as much as fits); any overflow stays put.</summary>
        private void TryPickUp()
        {
            if (PickupTarget == null || _inventory == null) return;
            _inventory.PickUp(PickupTarget);
        }
        #endregion

        private void UpdatePrompt()
        {
            if (IsSearching)
                PromptText = "Searching " + SearchTargetName + "\u2026   [E] cancel";
            else if (IsWorking)
                PromptText = JobLabel + "\u2026   [E] " + (JobOpenEnded ? "stop" : "cancel");
            else if (_ui != null && _ui.IsOpen)
                PromptText = "[E] / [Esc]  Close";
            else if (MenuOpen)
                PromptText = "[1\u2013" + Mathf.Min(9, _menuActions.Count) + "]  Choose   [Q] / [Esc]  Close";
            else if (_focus == FocusKind.Pickup)
            {
                Inventory inv = _inventory != null ? _inventory.Inventory : null;
                ItemDef def = PickupTarget.Def;
                string why = null;
                if (inv != null && def != null && !inv.WouldFit(def))
                    why = inv.CurrentWeight + def.Weight > inv.MaxWeight + 1e-4f ? "too heavy" : "no room";
                PromptText = why == null
                    ? "[E]  Pick up " + PickupTarget.PickupLabel
                    : "[E]  Pick up " + PickupTarget.PickupLabel + "  (" + why + ")";
            }
            else if (_focus == FocusKind.Container)
                PromptText = Target.Searched
                    ? "[E]  Open " + Target.DisplayName + (Target.IsEmpty ? "  (empty)" : "")
                    : "[E]  Search " + Target.DisplayName;
            else if (_focus == FocusKind.Fixture)
                PromptText = FixturePrompt(FixtureTarget);
            else
                PromptText = "[I]  Inventory";
        }

private string FixturePrompt(Interactable f)
        {
            _actions.Clear();
            f.GetActions(_ctx, _actions);
            if (_actions.Count == 0) return "[E] Inspect";
            InteractAction first = FirstEnabled(_actions);
            string more = _actions.Count > 1 ? "     [Q]  More (" + _actions.Count + ")" : "";
            if (first != null) return "[E]  " + first.Label + more;
            InteractAction a = _actions[0];
            return a.Label + "  \u2014 " + (a.DisabledReason ?? "not now") + more;
        }
    }
}
