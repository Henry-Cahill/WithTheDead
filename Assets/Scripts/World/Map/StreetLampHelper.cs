using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class StreetLampHelper
    {
        // Builds a simple pole + lamp head, adds a collider and a point light.
        // 'parent' is the OutdoorBatches to receive the geometry.
        // 'pos' is world‑space (x,z) at ground level; y will be raised to StreetY.
        internal static void PlaceLamp(OutdoorBatches parent, KentuckyMap.Site site, Vector2 pos, float yaw)
        {
            // Reserve a small radius so lamps don’t overlap other props.
            const float radius = 0.25f;
            const float baseHeight = 0.15f; // concrete slab thickness
            const float baseSize = 0.3f; // square footprint of the slab
            if (!site.Free(new Vector2(pos.x, pos.y), radius)) return;
            site.Take(new Vector2(pos.x, pos.y), radius);

            // Deterministic variation based on world position (without reseeding everyone else's UnityEngine.Random).
            int seed = ((int)(pos.x * 73856093f)) ^ ((int)(pos.y * 19349663f));
            Random.State savedRandom = Random.state;
            Random.InitState(seed);
            float yawJitter = (Random.value - 0.5f) * 10f; // ±5°
            float variedYaw = yaw + yawJitter;
            float poleHeight = 3f + (Random.value - 0.5f) * 0.1f; // ±0.05 m
            Random.state = savedRandom;

            // Concrete base: a short square box on the ground.
            var baseCenter = new Vector3(pos.x, KentuckyMap.StreetY + baseHeight / 2f, pos.y);
            var baseObj = parent.Solid(baseCenter);
            baseObj.Box(baseCenter, new Vector3(baseSize, baseHeight, baseSize), 0f, KentuckyMap.StreetLampMetal);
            var poleCenter = new Vector3(pos.x,
                KentuckyMap.StreetY + baseHeight + poleHeight / 2f,
                pos.y);
            var pole = parent.Solid(poleCenter);
            pole.Box(poleCenter, new Vector3(0.1f, poleHeight, 0.1f), variedYaw, KentuckyMap.StreetLampMetal);
            // Lamp head: positioned near the top of the pole.
            var headCenter = new Vector3(pos.x, KentuckyMap.StreetY + poleHeight - 0.1f, pos.y);
            var head = parent.Solid(headCenter);
            head.Box(headCenter, new Vector3(0.5f, 0.2f, 0.5f), variedYaw, KentuckyMap.StreetLampMetal);
            // Snow mound: a low, wide disc to suggest packed snow.
            var snowCenter = new Vector3(pos.x, KentuckyMap.StreetY + baseHeight + 0.02f, pos.y);
            var snow = parent.Solid(snowCenter);
            snow.Box(snowCenter, new Vector3(0.6f, 0.04f, 0.6f), 0f, KentuckyMap.StreetLampMetal);
            // Add a simple point light (warm yellow, short range, subtle flicker).
            site.Town.Light(site.Town.World,
                new Vector3(pos.x,
                    KentuckyMap.StreetY + baseHeight + poleHeight + 0.5f,
                    pos.y),
                KentuckyMap.StreetLampLightColor,
                5f, // range
                0.3f, // base intensity
                true, // enable subtle flicker
                false); // no shadows
            // Collider so the player can’t walk through the pole.
            // Collider so the player can’t walk through the pole and base.
            site.Town.PropCollider(
                new Vector3(pos.x,
                    KentuckyMap.StreetY + baseHeight + poleHeight / 2f,
                    pos.y),
                new Vector3(0.2f, poleHeight + baseHeight, 0.2f),
                0f);
        }
    }
}
