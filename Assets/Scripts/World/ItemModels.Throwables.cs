using UnityEngine;

namespace CloneZ
{
    // The throwables' models (Weapons/Throwables.cs): what the survivor sees fly, and what lies on the ground or in a drawer.
    public static partial class ItemModels
    {
        /// <summary>A molotov: an amber glass bottle, petrol to the shoulder, an old label, and a twist of rag stuffed in its neck
        /// with a tail of it hanging down the side.</summary>
        private static PartNode BuildMolotov(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0.15f;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            PartMat glass = PartMat.Of(new Color(0.42f, 0.27f, 0.1f), smoothness: 0.92f, metallic: 0.1f);
            PartMat petrol = PartMat.Of(new Color(0.56f, 0.38f, 0.12f), smoothness: 0.9f, metallic: 0.1f);
            PartMat label = PartMat.Of(new Color(0.82f, 0.76f, 0.6f), smoothness: 0.2f);
            PartMat rag = PartMat.Of(new Color(0.78f, 0.74f, 0.66f), smoothness: 0.1f);
            PartMat ragDirty = PartMat.Of(new Color(0.5f, 0.44f, 0.36f), smoothness: 0.1f);

            Cylinder(root, "Body", -0.045f, 0.085f, 0.18f, glass);
            Cylinder(root, "Petrol", -0.075f, 0.087f, 0.11f, petrol);
            Cylinder(root, "Label", -0.05f, 0.089f, 0.06f, label);
            Cylinder(root, "Base", -0.137f, 0.078f, 0.01f, glass);
            Sphere(root, "Shoulder", new Vector3(0f, 0.045f, 0f), Vector3.zero, new Vector3(0.085f, 0.06f, 0.085f), glass);
            Cylinder(root, "Neck", 0.1f, 0.032f, 0.07f, glass);
            Cylinder(root, "Lip", 0.133f, 0.037f, 0.01f, glass);
            Sphere(root, "Rag", new Vector3(0f, 0.15f, 0f), new Vector3(10f, 0f, 15f), new Vector3(0.048f, 0.04f, 0.045f), rag);
            Block(root, "RagTail", new Vector3(0.026f, 0.11f, 0.004f), new Vector3(0.028f, 0.085f, 0.006f), ragDirty).localEulerAngles = new Vector3(0f, 0f, -14f);
            Block(root, "RagEnd", new Vector3(0.036f, 0.062f, 0.006f), new Vector3(0.022f, 0.03f, 0.006f), rag).localEulerAngles = new Vector3(0f, 10f, -24f);
            return root;
        }

        /// <summary>A pipe bomb lying on its side: a length of steel pipe with a cap screwed on each end, a band of tape round
        /// it, and the fuse poking out of one cap.</summary>
        private static PartNode BuildPipeBomb(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0.05f;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            PartMat pipe = PartMat.Of(def.Tint, smoothness: 0.45f, metallic: 0.85f);
            PartMat cap = PartMat.Of(new Color(0.3f, 0.31f, 0.32f), smoothness: 0.4f, metallic: 0.85f);
            PartMat tape = PartMat.Of(new Color(0.62f, 0.63f, 0.64f), smoothness: 0.35f, metallic: 0.2f);
            PartMat fuse = PartMat.Of(new Color(0.16f, 0.13f, 0.1f), smoothness: 0.1f);
            Vector3 lay = new Vector3(0f, 0f, 90f);

            Rod(root, "Pipe", Vector3.zero, lay, 0.05f, 0.2f, pipe);
            Rod(root, "CapA", new Vector3(0.108f, 0f, 0f), lay, 0.064f, 0.032f, cap);
            Rod(root, "CapB", new Vector3(-0.108f, 0f, 0f), lay, 0.064f, 0.032f, cap);
            Rod(root, "Tape", new Vector3(-0.02f, 0f, 0f), lay, 0.053f, 0.045f, tape);
            Rod(root, "Fuse", new Vector3(0.14f, 0.012f, 0f), new Vector3(0f, 0f, 60f), 0.006f, 0.05f, fuse);
            return root;
        }

        /// <summary>A string of firecrackers: red paper bangers plaited either side of a dark fuse, a gaudy wrapper round the
        /// middle and the fuse's tail.</summary>
        private static PartNode BuildFirecrackers(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0.03f;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            PartMat paper = PartMat.Of(def.Tint, smoothness: 0.25f);
            PartMat paperDark = PartMat.Of(new Color(def.Tint.r * 0.7f, def.Tint.g * 0.7f, def.Tint.b * 0.7f), smoothness: 0.25f);
            PartMat wrap = PartMat.Of(new Color(0.9f, 0.74f, 0.2f), smoothness: 0.4f, metallic: 0.3f);
            PartMat fuse = PartMat.Of(new Color(0.18f, 0.15f, 0.12f), smoothness: 0.1f);

            for (int i = 0; i < 9; i++)
            {
                float x = -0.08f + i * 0.02f, side = (i & 1) == 0 ? 1f : -1f;
                Rod(root, "Banger" + i, new Vector3(x, 0f, side * 0.014f), new Vector3(90f, 0f, side * 22f), 0.013f, 0.045f, (i % 3) == 0 ? paperDark : paper);
            }
            Rod(root, "Fuse", new Vector3(0f, 0.002f, 0f), new Vector3(0f, 0f, 90f), 0.004f, 0.2f, fuse);
            Rod(root, "Tail", new Vector3(0.112f, 0.004f, 0.008f), new Vector3(0f, 30f, 90f), 0.004f, 0.04f, fuse);
            Block(root, "Wrapper", new Vector3(0f, 0.003f, 0f), new Vector3(0.04f, 0.018f, 0.05f), wrap);
            return root;
        }

        /// <summary>A twin-bell wind-up alarm clock standing on its feet: a round painted case, a cream face with its hands,
        /// two domed bells on top with the hammer between them, and the winding key behind.</summary>
        private static PartNode BuildAlarmClock(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0.075f;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            PartMat paint = PartMat.Of(def.Tint, smoothness: 0.55f, metallic: 0.35f);
            PartMat chrome = PartMat.Of(new Color(0.78f, 0.79f, 0.8f), smoothness: 0.8f, metallic: 0.9f);
            PartMat face = PartMat.Of(new Color(0.93f, 0.9f, 0.8f), smoothness: 0.3f);
            PartMat ink = PartMat.Of(new Color(0.08f, 0.08f, 0.09f), smoothness: 0.2f);
            Vector3 facing = new Vector3(90f, 0f, 0f);

            Rod(root, "Case", Vector3.zero, facing, 0.11f, 0.045f, paint);
            Rod(root, "Bezel", new Vector3(0f, 0f, 0.022f), facing, 0.104f, 0.006f, chrome);
            Rod(root, "Face", new Vector3(0f, 0f, 0.025f), facing, 0.092f, 0.004f, face);
            Block(root, "HourHand", new Vector3(0.008f, 0.012f, 0.028f), new Vector3(0.005f, 0.028f, 0.002f), ink).localEulerAngles = new Vector3(0f, 0f, -35f);
            Block(root, "MinuteHand", new Vector3(-0.012f, 0.004f, 0.029f), new Vector3(0.004f, 0.038f, 0.002f), ink).localEulerAngles = new Vector3(0f, 0f, 70f);
            Sphere(root, "BellL", new Vector3(-0.034f, 0.062f, 0f), new Vector3(0f, 0f, 22f), new Vector3(0.05f, 0.032f, 0.05f), paint);
            Sphere(root, "BellR", new Vector3(0.034f, 0.062f, 0f), new Vector3(0f, 0f, -22f), new Vector3(0.05f, 0.032f, 0.05f), paint);
            Block(root, "Bridge", new Vector3(0f, 0.084f, 0f), new Vector3(0.07f, 0.005f, 0.006f), chrome);
            Block(root, "Hammer", new Vector3(0f, 0.068f, 0f), new Vector3(0.005f, 0.03f, 0.005f), chrome);
            Sphere(root, "FootL", new Vector3(-0.032f, -0.06f, 0f), Vector3.zero, new Vector3(0.018f, 0.018f, 0.018f), chrome);
            Sphere(root, "FootR", new Vector3(0.032f, -0.06f, 0f), Vector3.zero, new Vector3(0.018f, 0.018f, 0.018f), chrome);
            Block(root, "Key", new Vector3(0f, 0f, -0.032f), new Vector3(0.03f, 0.02f, 0.004f), chrome);
            return root;
        }

        /// <summary>A transistor radio with a kitchen timer taped to its side: a painted case with a grille, a dial and a
        /// carrying strap, the aerial up, and a white wind-up timer held on with two bands of tape.</summary>
        private static PartNode BuildTimerRadio(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0.06f;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            PartMat body = PartMat.Of(def.Tint, smoothness: 0.35f, metallic: 0.1f);
            PartMat grille = PartMat.Of(new Color(0.12f, 0.12f, 0.13f), smoothness: 0.3f, metallic: 0.4f);
            PartMat chrome = PartMat.Of(new Color(0.78f, 0.79f, 0.8f), smoothness: 0.8f, metallic: 0.9f);
            PartMat dial = PartMat.Of(new Color(0.9f, 0.86f, 0.7f), smoothness: 0.3f);
            PartMat timer = PartMat.Of(new Color(0.94f, 0.94f, 0.92f), smoothness: 0.5f);
            PartMat mark = PartMat.Of(new Color(0.8f, 0.14f, 0.12f), smoothness: 0.3f);
            PartMat tape = PartMat.Of(new Color(0.62f, 0.63f, 0.64f), smoothness: 0.35f, metallic: 0.2f);
            PartMat strap = PartMat.Of(new Color(0.18f, 0.12f, 0.08f), smoothness: 0.25f);

            Block(root, "Case", Vector3.zero, new Vector3(0.16f, 0.1f, 0.05f), body);
            Block(root, "Grille", new Vector3(-0.025f, 0f, 0.026f), new Vector3(0.09f, 0.075f, 0.003f), grille);
            for (int i = 0; i < 5; i++)
                Block(root, "Slat" + i, new Vector3(-0.025f, -0.03f + i * 0.015f, 0.028f), new Vector3(0.088f, 0.003f, 0.002f), chrome);
            Rod(root, "Dial", new Vector3(0.05f, 0.012f, 0.026f), new Vector3(90f, 0f, 0f), 0.03f, 0.006f, dial);
            Block(root, "Strap", new Vector3(0f, 0.056f, 0f), new Vector3(0.12f, 0.012f, 0.02f), strap);
            Rod(root, "Aerial", new Vector3(0.07f, 0.12f, -0.015f), new Vector3(0f, 0f, -12f), 0.004f, 0.14f, chrome);
            Rod(root, "Timer", new Vector3(0.098f, -0.01f, 0f), new Vector3(0f, 0f, 90f), 0.06f, 0.035f, timer);
            Block(root, "TimerMark", new Vector3(0.116f, 0.012f, 0f), new Vector3(0.002f, 0.012f, 0.004f), mark);
            Block(root, "TapeA", new Vector3(0.075f, -0.01f, 0f), new Vector3(0.05f, 0.065f, 0.054f), tape).localEulerAngles = new Vector3(0f, 0f, 8f);
            return root;
        }
    }
}
