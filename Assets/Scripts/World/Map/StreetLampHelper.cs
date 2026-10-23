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
    public static void PlaceLamp(OutdoorBatches into, KentuckyMap.Site site, Vector2 pos, float yaw)
    {
        // Respect placement grid – avoid overlapping props.
        if (!site.Free(pos, 0.2f)) return;
        site.Take(pos, 0.2f);

        // 1️⃣ Base plate – a thin square slab on the ground.
        // size 0.30 m × 0.02 m × 0.30 m, centred at (pos.x, 0.01 m, pos.y)
        // material: existing StreetLampMetal matte metal.
        var basePlateSurf = KentuckyMap.StreetLampMetal;
        var baseCenter = To3(pos, 0.01f);
        var baseMesh = into.Solid(baseCenter);
        baseMesh.Box(baseCenter, new Vector3(0.30f, 0.02f, 0.30f), 0f, basePlateSurf);
        // Ensure consistent orientation for added geometry.
        // (Removed SetFrame call; geometry uses explicit yaw values.)
        // 1️⃣ Snow mound – a small white sphere that sits on the ground beside the base plate.
        var moundCenter = To3(pos, 0.08f); // slightly above ground
        var moundMesh = into.Solid(moundCenter);
        moundMesh.Sphere(moundCenter, 0.15f, SnowMoundSurf);
        
        // 2️⃣ Iron pole – a vertical cylinder.
        // radius 0.04 m, height 4.00 m, base at (pos.x, 0.02 m, pos.y)
        // material: StreetLampMetal.
        var poleHeight = 4.0f;
        var poleCenter = To3(pos, 0.02f + poleHeight * 0.5f);
        var poleMesh = into.Solid(poleCenter);
        poleMesh.Cylinder(poleCenter, 0.04f, poleHeight, 12, KentuckyMap.StreetLampMetal);
        
        // 3️⃣ Arm – a short rectangular bracket that angles upward.
        // size 0.50 m × 0.02 m × 0.02 m, pivoted 30° up from the pole.
        // positioned at middle‑height of pole (2 m), offset forward by 0.20 m.
        // material: StreetLampMetal.
        float armLength = 0.50f;
        float armAngle = 30f * Mathf.Deg2Rad;
        var armPivot = To3(pos, 0.02f + 2.0f);
        var armOffset = new Vector3(Mathf.Sin(armAngle) * armLength * 0.5f,
            Mathf.Cos(armAngle) * armLength * 0.5f,
            0f);
        var armCenter = armPivot + armOffset;
        var armMesh = into.Solid(armCenter);
        var armYaw = yaw + 90f; // arm sticks out to the side of the road.
        armMesh.Box(armCenter, new Vector3(armLength, 0.02f, 0.02f), armYaw, KentuckyMap.StreetLampMetal);
        
        // 3️⃣ Wooden sign – a thin board that hangs from the arm tip, with a tiny deterministic yaw wobble.
        float signWobble = ((pos.x + pos.y) * 0.123f) % 4f - 2f; // -2° … +2°
        var signCenter = armCenter + new Vector3(0f, -0.05f, 0f); // hangs slightly below arm tip
        var signMesh = into.Solid(signCenter);
        signMesh.Box(signCenter, new Vector3(0.80f, 0.10f, 0.20f), yaw + signWobble, SignSurf);
        
        // 4️⃣ Icicle – a thin vertical box that dangles from the lower edge of the sign.
        var icicleCenter = signCenter + new Vector3(0f, -0.12f, 0f);
        var icicleMesh = into.Solid(icicleCenter);
        icicleMesh.Box(icicleCenter, new Vector3(0.02f, 0.10f, 0.02f), yaw, SnowMoundSurf);
        


        
        // 4️⃣ Shade – a semi‑transparent glass sphere at the end of the arm.
        // radius 0.25 m, centre at arm tip.
        // material: a matte yellow (warm street‑lamp light colour) to suggest illumination.
        var shadeCenter = armCenter + new Vector3(Mathf.Sin(armAngle) * armLength * 0.5f,
            Mathf.Cos(armAngle) * armLength * 0.5f,
            0f);
        var shadeSurf = DecorPalette.Matte(KentuckyMap.StreetLampLightColor);
        var shadeMesh = into.Solid(shadeCenter);
        shadeMesh.Sphere(shadeCenter, 0.25f, shadeSurf);
        
        // 5️⃣ Inner bulb – a tiny emissive sphere inside the shade.
        // radius 0.07 m, centred at same point as shade.
        // material: same colour but with a tiny emissive boost (use same Matte surf – the game’s lighting will treat it as a bright spot).
        var bulbSurf = DecorPalette.Matte(KentuckyMap.StreetLampLightColor);
        var bulbMesh = into.Solid(shadeCenter);
        bulbMesh.Sphere(shadeCenter, 0.07f, bulbSurf);
        

        var rustColor = new Color(0.40f, 0.30f, 0.20f);
        var rustSurf = DecorPalette.Matte(rustColor);
        int seed = ((int)(pos.x * 73856093)) ^ ((int)(pos.y * 19349663));
        var rng = new System.Random(seed);
        int speckCount = rng.Next(3, 6);
        for (int i = 0; i < speckCount; i++)
    {

            float h = (float)rng.NextDouble() * (poleHeight - 0.2f) + 0.1f;
            float angle = (float)rng.NextDouble() * 360f;
            var speckCenter = To3(pos, 0.02f + h);
            var speckMesh = into.Solid(speckCenter);
            speckMesh.Box(speckCenter, new Vector3(0.02f, 0.02f, 0.02f), angle, rustSurf);
        }
        
        // 7️⃣ Light source – add a point light so the fixture actually glows.
        // position at shade centre, colour warm yellow, range 8 m, intensity 2.5, slight flicker.
        site.Town.Light(site.Town.Overhead, shadeCenter, KentuckyMap.StreetLampLightColor, 8f, 2.5f, flicker:true, shadows:true);
    }
    }
}
