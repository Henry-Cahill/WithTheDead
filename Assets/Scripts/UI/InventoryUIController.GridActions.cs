using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CloneZ
{
    // The grid windows' footer — "Transfer All", "Transfer only...", "Tidy up" and "⚙ Transfer / sort
    // options", each acting from its own window toward the other — plus the list / grid view toggle.
    // The view, the sort and the keep-the-weapon option persist in PlayerPrefs.
    public partial class InventoryUIController
    {
        private const string ViewPrefKey = "clonez.inventoryView";          // "grid" (default) or "list"
        private const string SortPrefKey = "clonez.inventorySort";          // a GridSortMode
        private const string KeepWeaponPrefKey = "clonez.inventoryKeepWeapon";

        private static readonly string[] s_GridSortNames = { "size", "category", "name", "weight" };
        private static readonly int s_CategoryCount = System.Enum.GetValues(typeof(ItemCategory)).Cast<int>().Max() + 1;

        private GridSortMode _gridSort = GridSortMode.Size;
        private bool _keepWeapon = true;

        private void LoadGridPrefs()
        {
            _gridMode = PlayerPrefs.GetString(ViewPrefKey, "grid") != "list";
            _gridSort = (GridSortMode)Mathf.Clamp(PlayerPrefs.GetInt(SortPrefKey, (int)GridSortMode.Size), 0, s_GridSortNames.Length - 1);
            _keepWeapon = PlayerPrefs.GetInt(KeepWeaponPrefKey, 1) != 0;
        }

        /// <summary>Switches between the grid windows and the verbose list (remembered across runs).</summary>
        private void SetViewMode(bool grid)
        {
            if (_gridMode == grid) return;
            CancelDrag();
            HideMenu();
            HideTooltip(null);
            _hover = null;
            _tipOwner = null;
            _gridMode = grid;
            PlayerPrefs.SetString(ViewPrefKey, grid ? "grid" : "list");
            PlayerPrefs.Save();
            if (!IsOpen) return;

            ShowActiveRoot();
            if (grid)
            {
                if (_openContainer != null) _openContainer.EnsureLayout();
                KeepOnScreen(_leftWin.Root);
                KeepOnScreen(_rightWin.Root);
                _gridPoll = 0f;
            }
            _dirty = false;
            Refresh();
        }

        #region Footer actions
        // "Transfer All": everything in this window to the other — the pack into the open container (or
        // onto the ground), or the container / the ground into the pack as far as the weight allows.
        private void TransferAll(bool fromLeft)
        {
            if (_player == null) return;
            CancelDrag();
            if (fromLeft) ReportSent(SendPackAcross(true, default));
            else ReportTaken(TakeAcross(true, default, out int left), left);
        }

        // "Transfer only...": a menu of the categories this window holds; picking one moves just those.
        private void ShowTransferOnly(bool fromLeft)
        {
            if (_player == null) return;
            CancelDrag();
            var units = new int[s_CategoryCount];
            if (fromLeft)
            {
                ItemStack keep = KeptWeapon();
                List<ItemStack> stacks = _player.Inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                    if (stacks[i] != keep && stacks[i].Def != null) units[(int)stacks[i].Def.Category] += stacks[i].Count;
            }
            else if (_openInventory != null)
            {
                List<ItemStack> stacks = _openInventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                    if (stacks[i].Def != null) units[(int)stacks[i].Def.Category] += stacks[i].Count;
            }
            else
            {
                GatherGround();
                for (int i = 0; i < _ground.Count; i++) units[(int)_ground[i].Def.Category] += _ground[i].Count;
            }

            var options = new List<MenuOption>(s_CategoryCount);
            for (int c = 0; c < s_CategoryCount; c++)
            {
                if (units[c] == 0) continue;
                var cat = (ItemCategory)c;
                options.Add(new MenuOption(cat + "  (" + units[c] + ")", () => TransferCategory(fromLeft, cat)));
            }
            if (options.Count == 0)
            {
                GridStatus(fromLeft ? _leftWin : _rightWin, "Nothing to transfer", false);
                return;
            }
            ShowMenu(fromLeft ? "Transfer only\u2026  \u2192  " + LootName() : "Transfer only\u2026  \u2192  pack", options);
        }

        private void TransferCategory(bool fromLeft, ItemCategory category)
        {
            if (_player == null) return;
            if (fromLeft) ReportSent(SendPackAcross(false, category));
            else ReportTaken(TakeAcross(false, category, out int left), left);
        }

        // "Tidy up": repacks the grid in the chosen order (all-or-nothing — it never spills anything).
        // The ground is always laid out in that order, so there it only re-sorts.
        private void TidyUp(bool left)
        {
            CancelDrag();
            string how = "Sorted by " + s_GridSortNames[(int)_gridSort];
            if (left)
            {
                InventoryLayout layout = _player != null ? _player.Layout : null;
                if (layout == null) return;
                bool ok = layout.Tidy(_gridSort);
                GridStatus(_leftWin, ok ? how : "Not enough room to repack everything", !ok);
            }
            else if (_openInventory != null && _openContainer != null)
            {
                InventoryLayout layout = _openContainer.EnsureLayout();
                bool ok = layout != null && layout.Tidy(_gridSort);
                GridStatus(_rightWin, ok ? how : "Couldn't repack it", !ok);
            }
            else GridStatus(_rightWin, how, false);
            MarkDirty();
        }

        // "⚙ Transfer / sort options": the sort used by Tidy up (and the ground), and whether the bulk
        // transfers leave the weapon in hand where it is.
        private void ShowGridOptions()
        {
            CancelDrag();
            var options = new List<MenuOption>(s_GridSortNames.Length + 1);
            for (int i = 0; i < s_GridSortNames.Length; i++)
            {
                var mode = (GridSortMode)i;
                options.Add(new MenuOption((mode == _gridSort ? "\u25cf  " : "\u25cb  ") + "Sort by " + s_GridSortNames[i], () => SetGridSort(mode)));
            }
            options.Add(new MenuOption((_keepWeapon ? "\u25a0  " : "\u25a1  ") + "Keep equipped weapon when transferring", ToggleKeepWeapon));
            ShowMenu("Transfer / sort options", options, 300f);
        }

        private void SetGridSort(GridSortMode mode)
        {
            _gridSort = mode;
            PlayerPrefs.SetInt(SortPrefKey, (int)mode);
            PlayerPrefs.Save();
            MarkDirty();   // the ground re-sorts at once; the grids on the next "Tidy up"
        }

        private void ToggleKeepWeapon()
        {
            _keepWeapon = !_keepWeapon;
            PlayerPrefs.SetInt(KeepWeaponPrefKey, _keepWeapon ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion

        #region Bulk moves
        // The weapon in hand, when the option keeps it out of bulk transfers.
        private ItemStack KeptWeapon() => _keepWeapon && _player != null && _player.EquippedWeapon != null ? _player.EquippedStack : null;

        // Moves every pack stack (or every one of a category) to the loot window: into the open container,
        // or onto the ground. Returns how many stacks went.
        private int SendPackAcross(bool all, ItemCategory category)
        {
            ItemStack keep = KeptWeapon();
            var snapshot = new List<ItemStack>(_player.Inventory.Stacks);
            int sent = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                ItemStack s = snapshot[i];
                if (s == keep || s.Def == null || (!all && s.Def.Category != category) || !Holds(_player.Inventory, s)) continue;
                if (_openInventory != null)
                {
                    if (_player.Inventory.TransferTo(_openInventory, s, s.Count, enforceWeight: false) > 0) sent++;
                }
                else
                {
                    _player.Drop(s, s.Count);
                    sent++;
                }
            }
            KeepHandsHonest();
            MarkDirty();
            return sent;
        }

        // Moves every container stack / ground pile (or every one of a category) into the pack, as far as the
        // weight allows. Returns how many went; <paramref name="left"/> counts those left (wholly or partly) behind.
        private int TakeAcross(bool all, ItemCategory category, out int left)
        {
            int taken = 0;
            left = 0;
            if (_openInventory != null)
            {
                var snapshot = new List<ItemStack>(_openInventory.Stacks);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    ItemStack s = snapshot[i];
                    if (s.Def == null || (!all && s.Def.Category != category) || !Holds(_openInventory, s)) continue;
                    int want = s.Count;
                    int got = _openInventory.TransferTo(_player.Inventory, s, want, enforceWeight: true);
                    if (got > 0) taken++;
                    if (got < want) left++;
                }
            }
            else
            {
                GatherGround();
                var snapshot = new List<WorldItem>(_ground);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    WorldItem w = snapshot[i];
                    if (w.Def == null || (!all && w.Def.Category != category) || !w.StillThere) continue;
                    int want = w.Count;
                    int got = _player.PickUp(w);
                    if (got > 0) taken++;
                    if (got < want) left++;
                }
            }
            MarkDirty();
            return taken;
        }

        private void ReportSent(int stacks)
        {
            if (stacks == 0) GridStatus(_leftWin, "Nothing to transfer", false);
            else GridStatus(_leftWin, "Moved " + Plural(stacks, "stack") + " \u2192 " + LootName(), false);
        }

        private void ReportTaken(int stacks, int left)
        {
            if (left > 0) GridStatus(_rightWin, Plural(left, "stack") + " left behind \u2014 too heavy or no room");
            else GridStatus(_rightWin, stacks == 0 ? "Nothing to transfer" : "Took " + Plural(stacks, "stack"), false);
        }

        private string LootName() => _openInventory != null && _openContainer != null ? _openContainer.DisplayName : "Ground";
        #endregion
    }
}
