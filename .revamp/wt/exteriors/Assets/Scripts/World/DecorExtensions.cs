using UnityEngine;

namespace CloneZ
{
    internal static class DecorExtensions
    {
        /// <summary>
        /// Draws an outdoor water tap at <paramref name="pos"/>. <paramref name="high"/> selects a taller commercial‑style tap.
        /// </summary>
        public static void AddOutdoorTap(RaidTown town, Vector3 pos, bool high)
        {
            // pipe height: short (0.4 m) for residential, tall (0.8 m) for larger houses
            float pipeHeight = high ? 0.80f : 0.40f;
            // base plate (metal)
            town.Out.Solid(pos).Box(pos, new Vector3(0.15f, 0.02f, 0.10f),
                DecorPalette.Metal(new Color(0.55f, 0.50f, 0.45f)));
            // vertical pipe
            town.Out.Solid(pos).Cylinder(pos + new Vector3(0f, 0.01f, 0f), 0.015f, pipeHeight, 12,
                DecorPalette.Metal(new Color(0.50f, 0.50f, 0.48f)), Quaternion.identity, 1f);
            // faucet handle – small cylinder protruding forward
            town.Out.Solid(pos).Cylinder(pos + new Vector3(0f, pipeHeight - 0.05f, 0.04f), 0.01f, 0.07f, 12,
                DecorPalette.Metal(new Color(0.70f, 0.55f, 0.35f)), Quaternion.identity, 1f);
            // three random rust spots on the pipe
            for (int i = 0; i < 3; i++)
            {
                if (town.Rng.NextDouble() < 0.5)
                {
                    float y = (float)(town.Rng.NextDouble() * pipeHeight * 0.8 + pipeHeight * 0.1);
                    town.Out.Solid(pos).Cylinder(pos + new Vector3(0f, y, 0f), 0.008f, 0.012f, 6,
                        DecorPalette.Metal(new Color(0.55f, 0.30f, 0.15f)), Quaternion.identity, 1f);
                }
            }
            // optional tiny drip emitter (glow) – about 20 % chance
            if (town.Rng.NextDouble() < 0.20)
            {
                town.Out.Solid(pos).Cylinder(pos + new Vector3(0f, pipeHeight + 0.01f, 0.07f), 0.004f, 0.01f, 6,
                    DecorPalette.Glow(new Color(1f, 0.55f, 0.20f)), Quaternion.identity, 1f);
            }
            // keep the footprint clear for later props
            town.Keepout.Add(Rect.MinMaxRect(pos.x - 0.18f, pos.z - 0.18f, pos.x + 0.18f, pos.z + 0.18f));
        }
    }
}
