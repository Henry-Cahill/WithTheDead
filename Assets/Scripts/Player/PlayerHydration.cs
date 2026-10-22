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
        private IWaterPoint _lastSource;
        private bool _lastFill;
        private Vector3 _lastPos;
        private const float CheckDistance = 0.5f;
        private Keyboard _keyboard; // cached keyboard instance

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
            Prompt = "";
            _source = null;
            _fill = false;

            if (_stats != null && _stats.IsDead) return;
            // A full-screen panel or build mode owns input; don't fight the container-search prompt.
            if (UIState.GameplayBlocked || UIState.BuildMode) return;
            if (_interactor != null && (_interactor.IsBusy || _interactor.MenuOpen)) return;

            // Re-evaluated every frame: a source can run dry, the mains can shut off and the last empty bottle
            // can be filled while the survivor stands still.
            Evaluate();
            _keyboard = Keyboard.current;

            if (_source != null)
            {
                Keyboard kb = _keyboard;
                if (kb != null && kb.gKey.wasPressedThisFrame) Act();
            }
        }

        private void Evaluate()
        {
            Vector3 pos = transform.position; pos.y = 0f;
            IWaterPoint src = WaterPoints.Nearest(pos, CloneZConfig.WaterSourceRange, _interactor);
            if (src == null) return;

            // If source has no water, always update Prompt (dry message) and cache
            if (!src.HasWater)
            {
                Prompt = src.DryMessage;
                // Cache the source and fill state (fill false) for comparison next frame
                _source = null;
                _fill = false;
                _lastSource = src;
                _lastFill = false;
                return;
            }

            bool hasEmpty = _inventory != null && _inventory.HasEmptyBottle();

            // Determine if we need to update Prompt based on changes
            bool sourceChanged = src != _lastSource;
            bool fillChanged = hasEmpty != _lastFill;
            bool dryNow = !src.HasWater; // always false here, but kept per spec for completeness
            if (sourceChanged || fillChanged || dryNow)
            {
                _source = src;
                _fill = hasEmpty;
                if (hasEmpty)
                {
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
                // Update cache after changing Prompt
                _lastSource = src;
                _lastFill = hasEmpty;
            }
            else
            {
                // No change, keep Prompt unchanged; still set _source and _fill for Act usage
                _source = src;
                _fill = hasEmpty;
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
