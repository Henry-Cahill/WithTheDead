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

            // Pole: a thin vertical box with jittered height (RaidMesh takes world positions).
            var poleCenter = new Vector3(pos.x, KentuckyMap.StreetY + poleHeight / 2f, pos.y);
            var pole = parent.Solid(poleCenter);
            pole.Box(poleCenter, new Vector3(0.1f, poleHeight, 0.1f), variedYaw, KentuckyMap.StreetLampMetal);
            // Lamp head: positioned near the top of the pole.
            var headCenter = new Vector3(pos.x, KentuckyMap.StreetY + poleHeight - 0.1f, pos.y);
            var head = parent.Solid(headCenter);
            head.Box(headCenter, new Vector3(0.5f, 0.2f, 0.5f), variedYaw, KentuckyMap.StreetLampMetal);
            // Add a simple point light (warm yellow, short range, no flicker).
            site.Town.Light(site.Town.World, new Vector3(pos.x, KentuckyMap.StreetY + poleHeight + 0.5f, pos.y),
                KentuckyMap.StreetLampLightColor, 5f, 0.3f, false, false);
            // Collider so the player can’t walk through the pole.
            site.Town.PropCollider(new Vector3(pos.x, KentuckyMap.StreetY + poleHeight / 2f, pos.y), new Vector3(0.2f, poleHeight, 0.2f), 0f);
        }
    }
}
