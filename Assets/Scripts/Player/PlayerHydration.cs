using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Lets the survivor get water. Stand at a tap, basin, rain barrel or any other water point
    /// (<see cref="IWaterPoint"/>) and press G: with an empty bottle in the pack you fill it (clean
    /// from a running tap, tainted from the barrel); otherwise you drink straight from the source.
    /// Tap water is safe while the mains run; rain-barrel water is tainted and risks a sick stomach
    /// unless it's boiled first. (The same drink / fill actions are in each fixture's Q menu.)
    ///
    /// A context action bound to G (not the mouse) — like <see cref="PlayerFarming"/>'s F key,
    /// it never interferes with aiming, swinging or building, and needs no mode toggle.
    /// </summary>
    public class PlayerHydration : MonoBehaviour
    {
        public string Prompt { get; private set; } = "";

        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerFoodSickness _sickness;
        private PlayerInteractor _interactor;

        private IWaterPoint _source;
        private bool _fill;
        private Vector3 _lastPos;
        private const float CheckDistance = 0.5f;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _inventory = GetComponent<PlayerInventory>();
            _sickness = GetComponent<PlayerFoodSickness>();
            _interactor = GetComponent<PlayerInteractor>();
            _lastPos = transform.position;
        }

        private void Update()
        {
            if (_stats != null && _stats.IsDead) return;
            // A full-screen panel or build mode owns input; don't fight the container-search prompt.
            if (UIState.GameplayBlocked || UIState.BuildMode) return;
            if (_interactor != null && (_interactor.IsBusy || _interactor.MenuOpen)) return;

            // Check if the player moved enough to re‑evaluate water points.
            float moved = Vector3.Distance(_lastPos, transform.position);
            if (moved >= CheckDistance)
            {
                Prompt = "";
                _source = null;
                _fill = false;
                Evaluate();
                _lastPos = transform.position;
            }

            if (_source != null)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.gKey.wasPressedThisFrame) Act();
            }
        }

        private void Evaluate()
        {
            Vector3 pos = transform.position; pos.y = 0f;
            IWaterPoint src = WaterPoints.Nearest(pos, CloneZConfig.WaterSourceRange, _interactor);
            if (src == null) return;

            if (!src.HasWater)
            {
                // Show the status but offer no action (leave _source null so G does nothing).
                Prompt = src.DryMessage;
                return;
            }

            _source = src;
            bool hasEmpty = _inventory != null && _inventory.HasEmptyBottle();

            if (hasEmpty)
            {
                _fill = true;
                Prompt = src.IsTainted
                    ? "[G]  Fill bottle   (tainted \u2014 boil before drinking)"
                    : "[G]  Fill bottle";
            }
            else
            {
                Prompt = src.IsTainted
                    ? "[G]  Drink from the " + src.WaterLabel + "   (tainted)"
                    : "[G]  Drink from the " + src.WaterLabel;
            }
        }

        private void Act()
        {
            if (_source == null) return;

            if (_fill)
            {
                // Fill an empty bottle; only draw the source down if a bottle was actually filled.
                if (_inventory != null && _inventory.FillOneBottle(_source.IsTainted))
                    _source.DrawWater();
            }
            else
            {
                _stats.Drink(CloneZConfig.WaterDrinkAmount);
                Sfx.PlayUI(SfxIds.Drink, 0.6f, Random.Range(0.9f, 1.1f));
                if (_source.IsTainted && _sickness != null) _sickness.OnDrankTainted();
                _source.DrawWater();
            }
        }
    }
}
