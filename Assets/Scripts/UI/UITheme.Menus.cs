using UnityEngine;

namespace CloneZ
{
    public static partial class UITheme
    {
        /// <summary>Title screen, character select/create and the menu radio.</summary>
        public static class Menu
        {
            public static readonly Color TitleRed = new Color(0.86f, 0.24f, 0.20f);
            public static readonly Color PlaceColor = new Color(0.88f, 0.83f, 0.74f);
            public static readonly Color CardColor = new Color(0.055f, 0.065f, 0.085f, 0.90f);
            public static readonly Color CardActive = new Color(0.10f, 0.11f, 0.14f, 0.95f);
            public static readonly Color HomeColor = new Color(0.90f, 0.72f, 0.44f);
            public static readonly Color Backdrop = new Color(0.02f, 0.025f, 0.04f, 1f);
            public static readonly Color BuildText = new Color(0.55f, 0.58f, 0.64f);
            public static readonly Color HintText = new Color(0.66f, 0.70f, 0.76f);
            public static readonly Color FootnoteText = new Color(0.68f, 0.71f, 0.76f);   // map attribution, radio station label
            public static readonly Color TraitPositive = new Color(0.3f, 0.5f, 0.3f);
            public static readonly Color TraitNegative = new Color(0.55f, 0.3f, 0.25f);
        }

        /// <summary>The in-game pause menu and its survivor card.</summary>
        public static class Pause
        {
            public static readonly Color Amber = new Color(0.95f, 0.68f, 0.25f, 1f);
            public static readonly Color Dim = new Color(0.62f, 0.66f, 0.72f, 1f);
            public static readonly Color Idle = new Color(0.74f, 0.77f, 0.82f, 1f);
            public static readonly Color DangerIdle = new Color(0.86f, 0.52f, 0.46f, 1f);
            public static readonly Color DangerHot = new Color(1f, 0.42f, 0.34f, 1f);
            public static readonly Color Good = new Color(0.55f, 0.88f, 0.5f, 1f);
            public static readonly Color Bad = new Color(1f, 0.4f, 0.34f, 1f);
            public static readonly Color Frost = new Color(0.55f, 0.75f, 1f, 1f);
            public static readonly Color WorldDim = new Color(0.02f, 0.025f, 0.035f, 0.5f);
            public static readonly Color Scrim = new Color(0.008f, 0.01f, 0.016f, 0.94f);
            public static readonly Color Rivet = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            public static readonly Color Card = new Color(0.025f, 0.03f, 0.04f, 0.84f);
        }

        /// <summary>The hub's screens, item rows and tooltip.</summary>
        public static class Hub
        {
            public static readonly Color CardColor = new Color(0.095f, 0.107f, 0.126f, 0.98f);
            public static readonly Color SelectedColor = new Color(0.25f, 0.20f, 0.11f, 0.98f);
            public static readonly Color HeaderColor = new Color(0.13f, 0.145f, 0.17f, 0.98f);
            public static readonly Color Info = new Color(0.52f, 0.72f, 0.92f);
            public static readonly Color Dimmer = new Color(0.54f, 0.57f, 0.62f);   // darkest that keeps 4.5:1 on the header
            public static readonly Color PressedTint = new Color(0.8f, 0.8f, 0.8f, 1f);
            public static readonly Color SkillBar = new Color(0.5f, 0.75f, 0.45f);
            public static readonly Color OwnedRow = new Color(0.3f, 0.6f, 0.3f, 0.15f);
            public static readonly Color TooltipBack = new Color(0.045f, 0.05f, 0.06f, 0.97f);

            // Item category name colours.
            public static readonly Color CatFood = new Color(0.88f, 0.76f, 0.50f);
            public static readonly Color CatDrink = new Color(0.50f, 0.76f, 0.94f);
            public static readonly Color CatMedical = new Color(0.94f, 0.52f, 0.52f);
            public static readonly Color CatWeapon = new Color(0.86f, 0.66f, 0.44f);
            public static readonly Color CatFirearm = new Color(0.97f, 0.64f, 0.32f);
            public static readonly Color CatAmmo = new Color(0.90f, 0.82f, 0.48f);
            public static readonly Color CatMaterial = new Color(0.70f, 0.64f, 0.56f);
            public static readonly Color CatClothing = new Color(0.64f, 0.80f, 0.58f);
            public static readonly Color CatOther = new Color(0.78f, 0.79f, 0.86f);

            /// <summary>The hover highlight for a tooltip-bearing element: slightly bluish lift, at least 60% opaque.</summary>
            public static Color HoverLighten(Color c) =>
                new Color(Mathf.Min(1f, c.r + 0.06f), Mathf.Min(1f, c.g + 0.065f), Mathf.Min(1f, c.b + 0.075f), Mathf.Max(c.a, 0.6f));
        }
    }
}
