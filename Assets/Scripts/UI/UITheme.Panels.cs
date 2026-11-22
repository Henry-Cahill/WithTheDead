using UnityEngine;

namespace CloneZ
{
    public static partial class UITheme
    {
        /// <summary>The health panel (H): worn-metal frame, status readouts and the treatment buttons.</summary>
        public static class Health
        {
            public static readonly Color Panel = new Color(0.06f, 0.05f, 0.06f, 0.95f);
            public static readonly Color Trim = new Color(0.04f, 0.03f, 0.05f, 0.9f);
            public static readonly Color Rivet = new Color(0.12f, 0.10f, 0.08f, 1f);
            public static readonly Color Hinge = new Color(0.45f, 0.25f, 0.15f, 1f);   // rust-tinted
            public static readonly Color TopAccent = new Color(0.5f, 0.68f, 0.9f, 0.6f);
            public static readonly Color Title = new Color(0.95f, 0.7f, 0.7f);

            public static readonly Color PainLabel = new Color(0.8f, 0.85f, 0.8f);
            public static readonly Color PainHigh = new Color(0.9f, 0.3f, 0.25f);
            public static readonly Color PainMid = new Color(0.95f, 0.7f, 0.25f);
            public static readonly Color PainLow = new Color(0.72f, 0.85f, 0.72f);
            public static readonly Color PanicLabel = new Color(0.78f, 0.78f, 0.9f);
            public static readonly Color PanicHigh = new Color(0.9f, 0.35f, 0.4f);
            public static readonly Color PanicMid = new Color(0.9f, 0.62f, 0.4f);
            public static readonly Color PanicLow = new Color(0.74f, 0.76f, 0.9f);
            public static readonly Color TempCold = new Color(0.5f, 0.72f, 0.95f);
            public static readonly Color TempHot = new Color(0.95f, 0.5f, 0.4f);
            public static readonly Color TempNormal = new Color(0.72f, 0.85f, 0.75f);
            public static readonly Color SicknessHigh = new Color(0.85f, 0.85f, 0.3f);
            public static readonly Color SicknessMid = new Color(0.8f, 0.82f, 0.5f);
            public static readonly Color SicknessLow = new Color(0.72f, 0.85f, 0.72f);

            public static readonly Color ClothingHeader = new Color(0.82f, 0.85f, 0.9f);
            public static readonly Color ClothingButton = new Color(0.24f, 0.28f, 0.34f);
            public static readonly Color ClothingLabelDisabled = new Color(0.8f, 0.82f, 0.86f, 0.5f);
            public static readonly Color ButtonLabel = new Color(0.95f, 0.96f, 0.98f, 1f);
            public static readonly Color ButtonLabelDisabled = Fade(ButtonLabel, 0.4f);
            public static readonly Color ButtonDisabled = Fade(UITheme.ButtonDisabled, 0.9f);

            public static readonly Color NoInjuries = new Color(0.6f, 0.85f, 0.6f);
            public static readonly Color WoundRow = new Color(0.9f, 0.9f, 0.92f);

            public static readonly Color Bandage = new Color(0.24f, 0.42f, 0.30f);
            public static readonly Color Disinfect = new Color(0.22f, 0.40f, 0.44f);
            public static readonly Color Painkiller = new Color(0.26f, 0.34f, 0.48f);
            public static readonly Color Antibiotic = new Color(0.38f, 0.28f, 0.46f);
            public static readonly Color FirstAid = new Color(0.5f, 0.26f, 0.26f);
            public static readonly Color Calm = new Color(0.30f, 0.30f, 0.52f);
        }

        /// <summary>The skills panel (K): levels, progress bars and the per-category accents.</summary>
        public static class Skills
        {
            public static readonly Color Panel = new Color(0.05f, 0.06f, 0.08f, 0.94f);
            public static readonly Color TopAccent = Health.TopAccent;
            public static readonly Color Title = new Color(0.95f, 0.9f, 0.65f);
            public static readonly Color Name = new Color(0.9f, 0.92f, 0.95f);
            public static readonly Color Progress = new Color(0.7f, 0.74f, 0.8f);
            public static readonly Color Separator = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            public static readonly Color Rivet = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            public static readonly Color CornerPlate = new Color(0.25f, 0.25f, 0.27f, 0.9f);

            public static readonly Color Combat = new Color(0.95f, 0.55f, 0.35f);
            public static readonly Color Physical = new Color(0.95f, 0.8f, 0.3f);
            public static readonly Color Crafting = new Color(0.6f, 0.82f, 0.45f);    // crafting / utility
            public static readonly Color Cooking = new Color(0.95f, 0.68f, 0.4f);     // cooking / hearth
            public static readonly Color Farming = new Color(0.5f, 0.78f, 0.4f);      // farming / growth
            public static readonly Color Agility = new Color(0.45f, 0.8f, 0.95f);     // agility / stealth
        }

        /// <summary>The loading card's text.</summary>
        public static class Loading
        {
            public static readonly Color Title = new Color(0.96f, 0.92f, 0.84f);
            public static readonly Color Subtitle = new Color(0.70f, 0.74f, 0.80f);
            public static readonly Color Stage = new Color(0.80f, 0.82f, 0.86f);
            public static readonly Color Tip = new Color(0.62f, 0.66f, 0.72f);
        }

        /// <summary>The winter night behind the title screen and loading card.</summary>
        public static class Winter
        {
            public static readonly Color Silhouette = new Color(0.028f, 0.032f, 0.048f, 1f);
            public static readonly Color LitWindow = new Color(1f, 0.74f, 0.38f, 0.9f);
            public static readonly Color Snow = new Color(0.70f, 0.74f, 0.82f, 1f);
            public static readonly Color SnowEdge = new Color(0.86f, 0.89f, 0.95f, 1f);
            public static readonly Color Beacon = new Color(0.95f, 0.22f, 0.16f, 0.9f);
            public static readonly Color Flake = new Color(0.92f, 0.95f, 1f);
            public static readonly Color SkyTop = new Color(0.015f, 0.022f, 0.045f);
            public static readonly Color SkyMid = new Color(0.045f, 0.058f, 0.095f);
            public static readonly Color SkyGlow = new Color(0.17f, 0.13f, 0.11f);   // the town's lights on the horizon
        }

        /// <summary>Hover highlight for rows that show a tooltip.</summary>
        public static class Tooltip
        {
            public static readonly Color RowHover = new Color(0.45f, 0.6f, 0.75f, 0.22f);
        }
    }
}
