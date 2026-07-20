using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Detects the nearest searchable container and routes E / I / Esc input to the
    /// inventory UI. Searching an unopened container takes time (PZ-style rummaging):
    /// the search bar fills while the player stays put, and cancels if they move off.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public LootContainer Target { get; private set; }
        public string PromptText { get; private set; } = "";

        public bool IsSearching { get; private set; }
        public float SearchProgress { get; private set; }
        public string SearchTargetName { get; private set; } = "";

        private InventoryUIController _ui;
        private PlayerStats _stats;
        private PlayerController _controller;

        private LootContainer _searching;
        private float _searchTimer;
        private float _searchTotal;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
        }

        public void Bind(InventoryUIController ui) => _ui = ui;

        private void Update()
        {
            if (_stats != null && _stats.IsDead)
            {
                CancelSearch();
                if (_ui != null && _ui.IsOpen) _ui.Close();
                PromptText = "";
                return;
            }

            // While asleep the world is frozen (GameplayBlocked set, but not by our own
            // inventory panel), so suppress container prompts and input.
            if (UIState.GameplayBlocked && (_ui == null || !_ui.IsOpen))
            {
                CancelSearch();
                PromptText = "";
                return;
            }

            UpdateTarget();
            if (IsSearching) TickSearch();

            Keyboard kb = Keyboard.current;
            if (kb == null || _ui == null) return;

            if (kb.eKey.wasPressedThisFrame)
            {
                if (_ui.IsOpen) _ui.Close();
                else if (IsSearching) CancelSearch();
                else if (Target != null) BeginOrOpen(Target);
            }
            else if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            {
                CancelSearch();
                if (_ui.IsOpen) _ui.Close();
                else _ui.OpenInventoryOnly();
            }
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                if (IsSearching) CancelSearch();
                else if (_ui.IsOpen) _ui.Close();
            }
        }

        /// <summary>Opens an already-searched container instantly; otherwise starts a timed search.</summary>
        private void BeginOrOpen(LootContainer container)
        {
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

        private void CancelSearch()
        {
            IsSearching = false;
            SearchProgress = 0f;
            _searching = null;
        }

        private void UpdateTarget()
        {
            float range = CloneZConfig.InteractRange;
            float bestSqr = range * range;
            LootContainer best = null;
            Vector3 p = transform.position;

            var all = LootContainer.All;
            for (int i = 0; i < all.Count; i++)
            {
                LootContainer c = all[i];
                if (c == null) continue;
                float d = (c.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = c; }
            }
            Target = best;

            // Auto-close a container the player has walked away from.
            if (_ui != null && _ui.IsOpen && _ui.CurrentContainer != null)
            {
                float leave = range * 1.7f;
                if ((_ui.CurrentContainer.transform.position - p).sqrMagnitude > leave * leave)
                    _ui.Close();
            }

            UpdatePrompt(best);
        }

        private void UpdatePrompt(LootContainer best)
        {
            if (IsSearching)
                PromptText = "Searching " + SearchTargetName + "\u2026   [E] cancel";
            else if (_ui != null && _ui.IsOpen)
                PromptText = "[E] / [Esc]  Close";
            else if (best != null)
                PromptText = best.Searched
                    ? "[E]  Open " + best.DisplayName + (best.IsEmpty ? "  (empty)" : "")
                    : "[E]  Search " + best.DisplayName;
            else
                PromptText = "[I]  Inventory";
        }
    }
}
