using UnityEngine;

namespace CloneZ
{
    public static partial class UITheme
    {
        /// <summary>Colours of the inventory screen (list view, grid view, container picker, menus and tooltips).</summary>
        public static class Bag
        {
            #region Window and chrome
            public static readonly Color WindowPanel = new Color(0.11f, 0.12f, 0.14f, 0.98f);
            public static readonly Color TitleText = new Color(0.95f, 0.92f, 0.85f);
            public static readonly Color Seam = new Color(0.07f, 0.08f, 0.09f, 0.8f);
            // Cool blue accent: the window's top strip, the corner rivets, the grid's highlights, the picker's selection bar.
            public static readonly Color BlueAccent = new Color(0.5f, 0.68f, 0.9f);
            public static readonly Color HeaderDim = new Color(0.66f, 0.7f, 0.76f);
            public static readonly Color TooltipBg = new Color(0.06f, 0.07f, 0.09f, 0.96f);
            public static readonly Color Lever = new Color(0.4f, 0.3f, 0.2f, 1f);
            #endregion

            #region Buttons
            public static readonly Color CloseButton = new Color(0.5f, 0.15f, 0.15f);
            public static readonly Color GridViewButton = new Color(0.28f, 0.34f, 0.44f);
            public static readonly Color StoreAllButton = new Color(0.28f, 0.34f, 0.44f);
            public static readonly Color DropAllButton = new Color(0.42f, 0.26f, 0.18f);
            public static readonly Color TakeButton = new Color(0.2f, 0.4f, 0.25f);   // per-row Take and the pane's Take All
            public static readonly Color StoreButton = new Color(0.28f, 0.32f, 0.4f);
            public static readonly Color TakeOffButton = new Color(0.3f, 0.34f, 0.46f);
            public static readonly Color DropButton = new Color(0.4f, 0.28f, 0.2f);
            public static readonly Color UnequipButton = new Color(0.38f, 0.32f, 0.22f);
            public static readonly Color EquipButton = new Color(0.2f, 0.38f, 0.42f);
            public static readonly Color MoreButton = new Color(0.26f, 0.28f, 0.32f);
            #endregion

            #region Context menu
            public static readonly Color MenuPanel = new Color(0.07f, 0.08f, 0.1f, 0.98f);
            public static readonly Color MenuTitle = new Color(0.6f, 0.66f, 0.74f);
            public static readonly Color MenuOption = new Color(0.17f, 0.19f, 0.23f);
            #endregion

            #region Weight meter
            public static readonly Color WeightFill = new Color(0.5f, 0.72f, 0.5f);
            public static readonly Color WeightHeavy = new Color(0.95f, 0.7f, 0.2f);
            public static readonly Color WeightOver = new Color(0.95f, 0.3f, 0.2f);
            #endregion

            #region List view
            public static readonly Color RowEven = Glass(0.05f);
            public static readonly Color RowOdd = Glass(0.02f);
            public static readonly Color RowPinned = new Color(0.45f, 0.62f, 0.85f, 0.12f);
            public static readonly Color RowSub = Ink(0.22f);
            public static readonly Color RowBolt = new Color(0.58f, 0.55f, 0.52f, 0.85f);   // the worn-metal bolt on each row
            public static readonly Color TextMain = new Color(0.92f, 0.92f, 0.92f);
            public static readonly Color ListTextDim = new Color(0.72f, 0.74f, 0.78f);
            public static readonly Color TextFaint = Glass(0.38f);
            public static readonly Color CategoryText = new Color(0.6f, 0.6f, 0.8f);   // PZ's category-column tint
            public static readonly Color WornText = new Color(0.62f, 0.8f, 1f);
            public static readonly Color ListGood = new Color(0.5f, 0.79f, 0.54f);
            public static readonly Color ListWarn = new Color(0.85f, 0.76f, 0.29f);
            public static readonly Color ListBad = new Color(0.85f, 0.41f, 0.29f);
            public static readonly Color ListRotten = new Color(0.5f, 0.68f, 0.29f);
            public static readonly Color ListAmber = new Color(0.88f, 0.65f, 0.24f);
            public static readonly Color ListWater = new Color(0.5f, 0.78f, 0.95f);
            public static readonly Color EquippedTick = new Color(0.24f, 0.62f, 0.32f);
            public static readonly Color DamageScar = new Color(1f, 0f, 0f, 0.3f);
            public static readonly Color IconDarkText = new Color(0.1f, 0.1f, 0.12f);   // glyph on a light item tint
            #endregion

            #region Container picker
            public static readonly Color FloorTint = new Color(0.4f, 0.38f, 0.33f);
            public static readonly Color ContainerFallback = new Color(0.5f, 0.45f, 0.38f);   // a container with no material colour
            public static readonly Color PickerSelected = new Color(0.3f, 0.42f, 0.56f, 0.55f);
            public static readonly Color PickerHover = new Color(0.45f, 0.6f, 0.75f, 0.22f);
            public static readonly Color PickerPressed = new Color(0.45f, 0.6f, 0.75f, 0.35f);
            public static readonly Color SearchFill = new Color(0.5f, 0.72f, 0.9f);
            #endregion

            #region Grid view
            public static readonly Color GridWindow = new Color32(0x0d, 0x0f, 0x11, 0xe6);     // 90 %
            public static readonly Color GridBorder = new Color32(0x5a, 0x5f, 0x66, 0xff);
            public static readonly Color GridTitleBar = new Color32(0x1a, 0x1d, 0x21, 0xff);
            public static readonly Color GridHeader = new Color32(0x20, 0x24, 0x29, 0xff);
            public static readonly Color GridButton = new Color32(0x2a, 0x2e, 0x33, 0xff);
            public static readonly Color GridTextDim = new Color(0.64f, 0.67f, 0.72f);
            public static readonly Color GridOver = new Color(0.95f, 0.36f, 0.3f);
            public static readonly Color GridCharge = new Color(0.35f, 0.85f, 0.95f);
            public static readonly Color GroundTint = new Color(0.42f, 0.4f, 0.34f);
            public static readonly Color PocketTint = new Color(0.45f, 0.5f, 0.56f);
            public static readonly Color GridCloseButton = new Color(0.4f, 0.16f, 0.16f);
            public static readonly Color InHandBadge = new Color(0.55f, 0.95f, 0.6f);
            public static readonly Color SlotCaption = new Color(0.82f, 0.85f, 0.9f, 0.85f);
            public static readonly Color RottenTint = new Color(0.3f, 0.36f, 0.12f);
            public static readonly Color StaleTint = new Color(0.5f, 0.44f, 0.16f);
            public static readonly Color FitAll = new Color(0.3f, 0.85f, 0.4f, 0.4f);
            public static readonly Color FitPart = new Color(0.95f, 0.75f, 0.25f, 0.4f);
            public static readonly Color FitNone = new Color(0.95f, 0.3f, 0.25f, 0.4f);
            #endregion
        }
    }
}
