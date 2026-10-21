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
            // Pole: a thin vertical box 0.1 m × 0.1 m × 3 m.
            var pole = parent.Solid(new Vector3(pos.x, KentuckyMap.StreetY + 1.5f, pos.y));
            pole.Box(new Vector3(0f, 0f, 0f), new Vector3(0.1f, 3f, 0.1f), yaw, KentuckyMap.StreetLampMetal);
            // Lamp head: a short box 0.5 m × 0.2 m × 0.5 m, positioned near the top.
            var head = parent.Solid(new Vector3(pos.x, KentuckyMap.StreetY + 2.9f, pos.y));
            head.Box(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.2f, 0.5f), yaw, KentuckyMap.StreetLampMetal);
            // Add a simple point light (warm yellow, short range, no flicker).
            site.Town.Light(site.Town.World, new Vector3(pos.x, KentuckyMap.StreetY + 3.0f, pos.y),
                KentuckyMap.StreetLampLightColor, 5f, 0.3f, false, false);
            // Collider so the player can’t walk through the pole.
            site.Town.PropCollider(new Vector3(pos.x, KentuckyMap.StreetY + 1.5f, pos.y), new Vector3(0.2f, 3f, 0.2f), 0f);
        }
    }
}
