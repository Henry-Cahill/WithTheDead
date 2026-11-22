using UnityEngine;

namespace CloneZ
{
    public static partial class UITheme
    {
        /// <summary>The in-raid HUD colours (readouts, prompts, overlays, view kits).</summary>
        public static class Hud
        {
            // Base HUD palette (aliased by HudUI).
            public static readonly Color Text = new Color(0.95f, 0.95f, 0.95f, 0.94f);
            public static readonly Color Amber = new Color(1f, 0.70f, 0.22f, 1f);
            // Danger and Panel are tuned together: red readouts keep 4.5:1 contrast even with snow behind the panel.
            public static readonly Color Danger = new Color(1f, 0.40f, 0.33f, 1f);
            public static readonly Color Good = new Color(0.42f, 0.88f, 0.42f, 1f);
            public static readonly Color Teal = new Color(0.247f, 0.824f, 0.78f, 1f);
            public static readonly Color Panel = new Color(0.025f, 0.03f, 0.04f, 0.82f);
            public static readonly Color Frame = Glass(0.14f);

            // Readouts (aliased by HudReadouts).
            public static readonly Color Cold = new Color(0.45f, 0.72f, 1f, 1f);
            public static readonly Color Hot = new Color(1f, 0.5f, 0.3f, 1f);
            public static readonly Color Dim = new Color(0.70f, 0.73f, 0.78f, 0.9f);
            public static readonly Color LowHealthFlash = new Color(1f, 0.85f, 0.8f, 1f);

            // HUDController: captions, progress rows, notices, prompts, overlays.
            public static readonly Color SubtitleSpeaker = new Color(0.85f, 0.75f, 0.5f);
            public static readonly Color DamageVignette = new Color(0.82f, 0.06f, 0.06f);
            public static readonly Color SearchText = new Color(0.95f, 0.9f, 0.7f);
            public static readonly Color SearchFill = new Color(0.95f, 0.85f, 0.35f);
            public static readonly Color BuildText = new Color(0.72f, 0.9f, 0.55f);
            public static readonly Color BuildFill = new Color(0.55f, 0.85f, 0.4f);
            public static readonly Color LevelUp = new Color(0.5f, 0.95f, 0.55f);
            public static readonly Color WaterNotice = new Color(0.55f, 0.8f, 0.98f);
            public static readonly Color PowerNotice = new Color(1f, 0.82f, 0.45f);
            public static readonly Color Prompt = new Color(0.95f, 0.9f, 0.55f);
            public static readonly Color SleepPrompt = new Color(0.7f, 0.85f, 1f);
            public static readonly Color SleepBackdrop = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            public static readonly Color SleepOutline = new Color(0.08f, 0.08f, 0.12f, 0.8f);
            public static readonly Color SleepText = new Color(0.8f, 0.85f, 0.95f);
            public static readonly Color GameOverBackdrop = new Color(0.05f, 0f, 0f, 0.72f);
            public static readonly Color GameOverText = new Color(0.85f, 0.1f, 0.1f);
            public static readonly Color GameOverCrack = Glass(0.35f);

            // Bodycam on-screen display.
            public static readonly Color BodycamOsd = new Color(0.96f, 0.96f, 0.92f, 0.95f);
            public static readonly Color BodycamRec = new Color(1f, 0.16f, 0.12f, 1f);

            // Combat feedback.
            public static readonly Color DamageArc = new Color(0.92f, 0.08f, 0.06f, 1f);
            public static readonly Color HitMarker = Glass(1f);
            public static readonly Color KillMarker = new Color(1f, 0.2f, 0.16f, 1f);
            public static readonly Color HeadshotMarker = new Color(1f, 0.72f, 0.2f, 1f);

            // Shared view-kit bits (first person, hands, shoulder).
            public static readonly Color LightOn = new Color(1f, 0.87f, 0.38f, 1f);
            public static readonly Color NvgOn = new Color(0.45f, 1f, 0.45f, 1f);
            public static readonly Color HealthTrail = new Color(1f, 0.86f, 0.78f, 1f);
            public static readonly Color FirstPersonTrack = Ink(0.45f);
            public static readonly Color PipEmpty = Glass(0.16f);

            // Hands.
            public static readonly Color HandsDisc = Fade(Backdrop, 0.62f);
            public static readonly Color RingIdle = Glass(0.55f);
            public static readonly Color RingEmpty = Glass(0.28f);
            public static readonly Color Ammo = new Color(0.94f, 0.92f, 0.8f, 1f);

            // Key hints and the interaction menu.
            public static readonly Color KeyCap = new Color(0.20f, 0.22f, 0.28f, 0.92f);
            public static readonly Color KeyLabel = new Color(0.96f, 0.95f, 0.82f, 1f);
            public static readonly Color HintAction = Glass(0.72f);
            public static readonly Color MenuFocus = new Color(1f, 0.96f, 0.82f, 0.95f);
            public static readonly Color MenuTitle = new Color(1f, 0.86f, 0.45f, 1f);
            public static readonly Color MenuRow = Glass(0.04f);
            public static readonly Color MenuRowHover = new Color(1f, 0.85f, 0.4f, 0.22f);
            public static readonly Color MenuRowPressed = new Color(1f, 0.8f, 0.3f, 0.35f);
            public static readonly Color MenuLabel = new Color(0.97f, 0.97f, 0.95f, 1f);
            public static readonly Color MenuDisabled = new Color(0.62f, 0.62f, 0.62f, 0.85f);
            public static readonly Color MenuAccent = new Color(1f, 0.78f, 0.3f, 0.85f);

            // Moodles, by severity.
            public static readonly Color[] MoodleBad =
            {
                new Color(0.80f, 0.70f, 0.30f, 0.92f), new Color(0.86f, 0.52f, 0.20f, 0.92f),
                new Color(0.82f, 0.30f, 0.16f, 0.94f), new Color(0.66f, 0.10f, 0.10f, 0.96f),
            };
            public static readonly Color[] MoodleChill =
            {
                new Color(0.45f, 0.62f, 0.85f, 0.92f), new Color(0.32f, 0.52f, 0.88f, 0.92f),
                new Color(0.22f, 0.40f, 0.85f, 0.94f), new Color(0.16f, 0.28f, 0.78f, 0.96f),
            };
            public static readonly Color MoodleAlarm = new Color(1f, 0.25f, 0.2f, 0.98f);

            // Raid overlay.
            public static readonly Color RaidProtection = new Color(0.45f, 1f, 0.45f, 1f);
            public static readonly Color RaidExtracting = new Color(0.55f, 1f, 0.62f, 1f);

            // Stamina.
            public static readonly Color StaminaTrack = Glass(0.16f);

            // Status row.
            public static readonly Color StatusAbsent = Glass(0.22f);
            public static readonly Color StatusButton = new Color(0.34f, 0.36f, 0.40f, 0.95f);
            public static readonly Color StatusButtonNormal = new Color(0.82f, 0.82f, 0.82f, 1f);
            public static readonly Color StatusButtonPressed = new Color(0.62f, 0.62f, 0.62f, 1f);
            public static readonly Color WaterOn = new Color(0.55f, 0.8f, 0.95f);
            public static readonly Color WaterOff = new Color(0.92f, 0.42f, 0.35f);

            // Subtitles and toasts.
            public static readonly Color SubtitleText = new Color(0.96f, 0.96f, 0.93f, 1f);
            public static readonly Color Toast = new Color(0.97f, 0.92f, 0.76f, 1f);

            // Watch row.
            public static readonly Color WatchGhost = Fade(Teal, 0.075f);
            public static readonly Color WatchTealDim = Fade(Teal, 0.85f);
            public static readonly Color BellIdle = Glass(0.32f);
            public static readonly Color BellAlarm = new Color(1f, 0.22f, 0.18f, 1f);
            public static readonly Color Toxic = new Color(0.78f, 0.95f, 0.25f, 1f);
            public static readonly Color WatchFace = new Color(0.012f, 0.04f, 0.042f, 0.82f);   // a faintly teal LCD face
            public static readonly Color WatchCaption = new Color(0.85f, 0.88f, 0.88f, 0.75f);
        }
    }
}
