using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // On the roofs: eave trim with a thin drift of snow along it and a brick chimney on every home raised whole, and a
    // skin of snow on three roofs in ten. All of it goes with the building's roof (shown from outside, cut away around
    // the survivor; see BuildingShell), or as scenery past the edge.
    internal static partial class BuildingPass
    {
        private const float EaveHeight = 0.12f;
        private const float EaveDepth = 0.20f;   // out from the wall
        private const float ChimneyBase = 0.3f;
        private const float ChimneyHeight = 1.5f;
        private const int SaltChimneyBrick = 0x5A50, SaltChimneyIce = 0x5A4F;

        // Every residential building (by kind, or used as a residence) the pass raised whole: not the tiny ones, nor those
        // the edge cuts through.
        private static void Roofs(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 6f || (!f.Inside(half, 1f) && f.Bounds.Overlaps(square))) continue;
                string use = f.Data.use ?? "";
                if (!use.Contains("residential") && !Residential(f.Data.kind)) continue;
                float roofY = HeightOf(f.Data);
                Eaves(site, f, roofY);
                Chimney(site, f, roofY);
            }
        }

        // Trim along every wall at the roof's edge, in the roof's colour, with snow along its top.
        private static void Eaves(Site site, Footprint f, float roofY)
        {
            float y = roofY + EaveHeight * 0.5f;
            Surf roof = RoofSurf(f.Data);
            RaidMesh m = UpperOf(site, f);
            List<Vector2> pts = f.Pts;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 0.05f) continue;
                Vector3 at = To3((a + b) * 0.5f, y);
                Quaternion along = Quaternion.LookRotation(new Vector3(d.x, 0f, d.y));
                m.Box(at, new Vector3(EaveDepth, EaveHeight, len), along, roof);
                Vector3 snow = at + new Vector3(0f, EaveHeight * 0.5f + 0.01f, 0f);
                m.Box(snow, new Vector3(EaveDepth, 0.02f, len), along, DecorMaterials.Snow);
            }
        }

        // A brick chimney over the middle of the roof, with a cap and a vent; snow iced over one cap in seven.
        private static void Chimney(Site site, Footprint f, float roofY)
        {
            Vector3 at = To3(f.Middle, roofY + ChimneyHeight * 0.5f);
            Surf brick = DecorMaterials.WallExterior(ExteriorStyle.Brick, BrickColors[MapStyle.Pick(f.Data.id, SaltChimneyBrick, BrickColors.Length)]);
            RaidMesh m = UpperOf(site, f);
            float shaft = ChimneyHeight * 0.9f;
            m.Cylinder(at, ChimneyBase * 0.5f, shaft, 6, brick);
            Vector3 cap = at + new Vector3(0f, shaft + ChimneyHeight * 0.05f, 0f);
            m.Box(cap, new Vector3(ChimneyBase, ChimneyHeight * 0.1f, ChimneyBase), Quaternion.identity, brick);
            Vector3 vent = at + new Vector3(ChimneyBase * 0.55f, shaft * 0.5f, 0f);
            m.Cylinder(vent, ChimneyBase * 0.15f, ChimneyHeight * 0.2f, 6, brick);
            if (MapStyle.Unit(f.Data.id, SaltChimneyIce) >= 0.15f) return;
            Vector3 ice = cap + new Vector3(0f, ChimneyHeight * 0.06f, 0f);
            m.Box(ice, new Vector3(ChimneyBase * 1.1f, 0.02f, ChimneyBase * 1.1f), Quaternion.identity, DecorMaterials.Snow);
        }

        // A skin of snow just over the roof (a canopy's roof inside the square is at its own height).
        private static void RoofSnow(Site site, Footprint f, bool inside)
        {
            float roofY = inside && f.Data.kind == "canopy" ? 4.2f : HeightOf(f.Data);
            float y = roofY + 0.011f;
            var at = new Vector3(f.Middle.x, roofY, f.Middle.y);
            RaidMesh roof = inside ? UpperOf(site, f) : site.Town.Far.Solid(at);
            List<int> tris = MapGeometry.Triangulate(f.Pts);
            for (int i = 0; i + 2 < tris.Count; i += 3)
                roof.Tri(To3(f.Pts[tris[i]], y), To3(f.Pts[tris[i + 1]], y), To3(f.Pts[tris[i + 2]], y), DecorMaterials.Snow);
        }

        // The mesh a building's roof furniture goes into: its shell's upper part, or the shared roof batch for a building
        // without a shell.
        private static RaidMesh UpperOf(Site site, Footprint f)
        {
            for (int i = site.Shells.Count - 1; i >= 0; i--)
                if (site.Shells[i].Footprint == f) return site.Shells[i].Upper;
            return site.Roofs.At(To3(f.Middle, 0f));
        }
    }
}
