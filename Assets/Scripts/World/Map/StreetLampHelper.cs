using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class StreetLampHelper
    {
// Helper to convert 2‑D map coords to 3‑D world coords (y is up).
    private static Vector3 To3(Vector2 v, float y) => new Vector3(v.x, y, v.y);

    // Emissive surface for the bulb – uses the colour already defined in KentuckyMap.
    private static readonly Surf LampLightSurf = DecorPalette.Gloss(KentuckyMap.StreetLampLightColor);
    // Snow mound – low‑poly white “snow” surface.
    private static readonly Surf SnowMoundSurf = DecorPalette.Gloss(Color.white * 0.85f);
    // Wooden street‑sign – fallback if wood palette missing.
    private static readonly Surf SignSurf = DecorPalette.Gloss(new Color(0.45f, 0.30f, 0.15f));

    /// <summary>
    /// Builds a period‑correct 1993‑style street lamp at the given map position.
    /// All parts are simple boxes (≤ 5 primitives) and reuse existing materials.
    /// </summary>
    public static void PlaceLamp(OutdoorBatches parent, KentuckyMap.Site site, Vector2 pos, float yaw)
    {
        // Reserve a small radius so lamps don’t overlap other props.
        const float radius = 0.30f; // extra space for icicle
        const float baseHeight = 0.10f; // concrete slab thickness
        const float baseSize = 0.30f; // square footprint of the slab
        if (!site.Free(new Vector2(pos.x, pos.y), radius)) return;
        site.Take(new Vector2(pos.x, pos.y), radius);

        // Concrete base – short squat box.
        var baseCenter = To3(pos, KentuckyMap.StreetY + baseHeight / 2f);
        var baseObj = parent.Solid(baseCenter);
        baseObj.Box(baseCenter, new Vector3(baseSize, baseHeight, baseSize), 0f, KentuckyMap.StreetLampMetal);

        // Tall pole.
        const float poleHeight = 2.40f;
        var poleCenter = To3(pos, KentuckyMap.StreetY + baseHeight + poleHeight / 2f);
        var pole = parent.Solid(poleCenter);
        pole.Box(poleCenter, new Vector3(0.07f, poleHeight, 0.07f), yaw, KentuckyMap.StreetLampMetal);

        // Horizontal arm – supports the shade.
        const float armY = poleHeight - 0.30f; // 0.30 m below pole top
        var armCenter = To3(pos, KentuckyMap.StreetY + baseHeight + armY);
        var arm = parent.Solid(armCenter);
        arm.Box(armCenter, new Vector3(0.02f, 0.02f, 0.60f), yaw, KentuckyMap.StreetLampMetal);

        // Light shade – small glowing cube.
        const float shadeY = poleHeight - 0.25f;
        // Offset outward along road direction.
        Vector2 offset2D = Quaternion.Euler(0f, yaw, 0f) * new Vector2(0.60f, 0f);
        var shadePos2D = pos + offset2D;
        var shadeCenter = To3(shadePos2D, KentuckyMap.StreetY + baseHeight + shadeY);
        var shade = parent.Solid(shadeCenter);
        shade.Box(shadeCenter, new Vector3(0.20f, 0.20f, 0.20f), yaw, LampLightSurf);

        // Metal cap – non‑emissive top of the shade.
        var capCenter = shadeCenter + new Vector3(0f, 0.12f, 0f);
        var cap = parent.Solid(capCenter);
        cap.Box(capCenter, new Vector3(0.22f, 0.02f, 0.22f), yaw, KentuckyMap.StreetLampMetal);

        // Add a point light (warm yellow).
        site.Town.Light(site.Town.World,
            new Vector3(pos.x, KentuckyMap.StreetY + baseHeight + poleHeight + 0.5f, pos.y),
            KentuckyMap.StreetLampLightColor,
            5f, 0.3f, true, false);

        // Collider so the player can’t walk through the pole.
        site.Town.PropCollider(
            new Vector3(pos.x, KentuckyMap.StreetY + baseHeight + poleHeight / 2f, pos.y),
            new Vector3(0.2f, poleHeight + baseHeight, 0.2f),
            0f);
    }
    }
}
