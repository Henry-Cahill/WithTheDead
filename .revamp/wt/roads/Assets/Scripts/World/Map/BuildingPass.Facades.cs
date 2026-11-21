using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Windows painted on: the upper floors of a tall building (four storeys and up) and every floor of one drawn cheaply
    // (a Lite site's, scenery's past the edge) get one quad a wall for a run of floors instead of a few boxes a window. Its
    // texture is a grid of windows, alpha-tested so the wall shows between them: sash windows punched in brick, ribbon
    // windows, continuous bands, or a whole curtain wall of glass; a few panes lit after dark (an emission map, see
    // BuildingPass.Materials). The grid is laid out like the windows it stands for: whole bays along each wall, a row a
    // storey, its pattern of lit panes shifted from building to building and wall to wall.
    internal static partial class BuildingPass
    {
        /// <summary>From this many storeys up a building's upper floors get painted windows.</summary>
        private const int TallStoreys = 4;
        private const int SaltFacade = 0xFACE;
        /// <summary>The grid textures hold 4 x 4 bays and storeys.</summary>
        private const int GridCells = 4;

        internal enum FacadeStyle { Punched, Ribbon, Band, Curtain, Small }

        private static FacadeStyle StyleOf(Plan p, WindowKind kind)
        {
            if (p.Curtain) return FacadeStyle.Curtain;
            switch (kind)
            {
                case WindowKind.Ribbon: return FacadeStyle.Ribbon;
                case WindowKind.Band: return FacadeStyle.Band;
                case WindowKind.Small: return FacadeStyle.Small;
                default: return FacadeStyle.Punched;
            }
        }

        // A bay of the grid: the plan's window spacing, a curtain wall's panels, a band's mullions.
        private static float GridSpacing(Plan p, FacadeStyle style)
        {
            switch (style)
            {
                case FacadeStyle.Curtain: return 1.5f * 2f;
                case FacadeStyle.Band: return Mathf.Clamp(p.Spacing, 2.4f, 3.6f);
                case FacadeStyle.Small: return Mathf.Clamp(p.Spacing, 2.5f, 6f);
                default: return Mathf.Clamp(p.Spacing, 2.0f, 4.2f);
            }
        }

        // The painted windows of every wall the openings left to them (Job.GridLow/GridHigh).
        private static void Facades(Job j)
        {
            Plan p = j.P;
            for (int i = 0; i < j.N; i++)
            {
                bool low = j.GridLow[i], high = j.GridHigh[i] && p.Storeys > 1;
                if (!low && !high) continue;
                float len = j.Len[i];
                float margin = Mathf.Min(0.5f, len * 0.08f);
                if (len - margin * 2f < 1f) continue;
                float u0 = MapStyle.Pick(j.Id, SaltFacade + i * 7, GridCells) / (float)GridCells;
                float v0 = MapStyle.Pick(j.Id, SaltFacade + i * 7 + 3, GridCells) / (float)GridCells;
                if (low)
                {
                    WindowKind kind = i == p.Front ? p.FrontWindow : p.GroundWindow;
                    GridBand(j, i, margin, len - margin, 0, 0, StyleOf(p, kind == WindowKind.Storefront ? WindowKind.Sash : kind), u0, v0);
                }
                if (high) GridBand(j, i, margin, len - margin, 1, p.Storeys - 1, StyleOf(p, p.UpperWindow), u0, v0 + 0.5f);
            }
        }

        // One quad of painted windows on wall i from a to b along it, storeys k0 to k1, stretch by stretch of the wall's top.
        private static void GridBand(Job j, int i, float a, float b, int k0, int k1, FacadeStyle style, float u0, float v0)
        {
            Plan p = j.P;
            float storey = k0 == 0 ? p.FirstStorey : p.Storey;
            float y0 = j.FloorY(k0), y1 = y0 + storey * (k1 - k0 + 1);
            float spacing = GridSpacing(p, style);
            int bays = Mathf.Max(1, Mathf.RoundToInt((b - a) / spacing));
            float bay = (b - a) / bays;
            Surf surf = FacadeSurf(style, p.Lit, p.Wall == WallKind.Siding || p.Wall == WallKind.Trailer || p.Trim.r > 0.8f);
            RaidMesh m = k0 == 0 ? j.Facing(i) : j.Upper;
            Vector3 o = To3(j.Out[i], 0f), along = To3(j.Dir[i], 0f);
            float cellU = bay * GridCells, cellV = storey * GridCells;
            foreach (Seg seg in j.Tops[i])
            {
                float s0 = Mathf.Max(a, seg.S0), s1 = Mathf.Min(b, seg.S1);
                float top = Mathf.Min(y1, seg.Top - (p.Roof == RoofKind.Flat ? p.Parapet + 0.1f : 0.15f));
                if (s1 - s0 < bay * 0.5f || top - y0 < storey * 0.55f) continue;
                // Whole storeys only: a part-storey of windows under the eaves would show cut-off panes.
                int rows = Mathf.Max(1, Mathf.FloorToInt((top - y0) / storey + 0.25f));
                float yTop = Mathf.Min(top, y0 + rows * storey);
                Vector2 c2 = j.At(i, (s0 + s1) * 0.5f) + j.Out[i] * 0.012f;
                var centre = new Vector3(c2.x, (y0 + yTop) * 0.5f, c2.y);
                var uv = new Rect(u0 + (s0 - a) / cellU, v0, (s1 - s0) / cellU, (yTop - y0) / cellV);
                m.QuadUV(centre, along * ((s1 - s0) * 0.5f), Vector3.up * ((yTop - y0) * 0.5f), surf, uv);
            }
        }
    }
}
