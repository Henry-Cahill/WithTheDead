using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Doors and windows. Where they go: the front door in the wall facing the road (centred on a farmhouse, a church or a
    // courthouse, to one side on a ranch, a bungalow or a shotgun house), a back door opposite; garage, roll-up and barn
    // doors along the front or the gable ends; windows bay by bay along every wall but the shared ones, the upper
    // storeys' over the ground storey's, a shop front's glass between its pilasters. And how they look: sash windows
    // with frames, meeting rails, sills, stone lintels or trim heads and shutters, picture windows, school ribbons,
    // office bands, arched church windows in coloured glass, shop fronts with kick plates, display glass and transoms;
    // a lit window here and there (it glows by night). Doors are real openings in the ground storey's walls (no leaf:
    // the interiors may hang one); the closed ones are drawn on the wall. The ground storey's doors and windows are
    // listed for the interiors exactly as placed.
    internal static partial class BuildingPass
    {
        private static Surf LitGlass => DecorPalette.Glow(new Color(0.46f, 0.36f, 0.21f));

        #region Placing
        private static void PlanOpenings(Job j)
        {
            Plan p = j.P;
            if (!j.Lite && p.Enterable) Doors(j);
            Windows(j);
        }

        private static void Doors(Job j)
        {
            Plan p = j.P;
            int front = p.Front >= 0 ? p.Front : LongestFree(j, -1);
            if (front < 0) return;
            if (p.BigDoors > 0) front = BigDoors(j, front);
            float w = p.Door == DoorKind.Double ? DoubleDoorWidth : DoorWidth;
            int edge = front;
            float s = DoorSpot(j, edge, w);
            if (s < 0f)
            {
                edge = LongestFree(j, front);
                s = edge >= 0 ? DoorSpot(j, edge, w) : -1f;
            }
            if (s < 0f) return;
            AddDoor(j, edge, s, w, p.Door);
            j.DoorEdge = edge;
            j.DoorAt = s;
            if (!p.BackDoor || j.F.Size < 45f) return;
            int back = Opposite(j, edge);
            if (back < 0 || j.Len[back] < DoorWidth + 1.4f) return;
            float bs = Mathf.Clamp(j.Len[back] * Range(j.Rng, 0.3f, 0.7f), DoorWidth * 0.5f + 0.5f, j.Len[back] - DoorWidth * 0.5f - 0.5f);
            if (Clear(j, back, bs, DoorWidth)) AddDoor(j, back, bs, DoorWidth, p.Door == DoorKind.Shop ? DoorKind.Plain : DoorKind.House);
        }

        // Where along the front the door goes, by what the building is; -1 when there's no room.
        private static float DoorSpot(Job j, int edge, float w)
        {
            Plan p = j.P;
            float len = j.Len[edge];
            if (len < w + 0.9f) return -1f;
            bool left = MapStyle.Unit(j.F.Data.id, SaltPlan + 3) < 0.5f;
            float t;
            switch (p.Arch)
            {
                case Arch.House:
                    switch (p.Style)
                    {
                        case HouseStyle.Ranch: case HouseStyle.Cottage: case HouseStyle.Bungalow: t = left ? 0.36f : 0.64f; break;
                        case HouseStyle.Shotgun: t = left ? 0.27f : 0.73f; break;
                        default: t = 0.5f; break;
                    }
                    break;
                case Arch.Trailer: t = left ? 0.3f : 0.7f; break;
                case Arch.Row: case Arch.Store: t = MapStyle.Unit(j.F.Data.id, SaltPlan + 4) < 0.45f ? 0.5f : left ? 0.2f : 0.8f; break;
                case Arch.Motel: t = left ? 0.12f : 0.88f; break;
                default: t = 0.5f; break;
            }
            float s = Mathf.Clamp(len * t, w * 0.5f + 0.45f, len - w * 0.5f - 0.45f);
            if (Clear(j, edge, s, w)) return s;
            // Beside whatever's in the way.
            for (float step = 0.6f; step < len; step += 0.6f)
            {
                if (s + step < len - w * 0.5f - 0.45f && Clear(j, edge, s + step, w)) return s + step;
                if (s - step > w * 0.5f + 0.45f && Clear(j, edge, s - step, w)) return s - step;
            }
            return -1f;
        }

        // True when nothing else is on wall <paramref name="edge"/>'s ground storey within a door's width of s.
        private static bool Clear(Job j, int edge, float s, float w)
        {
            foreach (Hole h in j.Holes)
                if (h.Edge == edge && h.Storey == 0 && Mathf.Abs(h.S - s) < (h.W + w) * 0.5f + 0.3f) return false;
            return true;
        }

        private static void AddDoor(Job j, int edge, float s, float w, DoorKind kind)
        {
            float head = kind == DoorKind.Double ? 2.35f : DoorHeight;
            j.Holes.Add(new Hole { Edge = edge, Storey = 0, S = s, W = w, Sill = 0f, Head = head, Win = WindowKind.None, Door = kind });
            if (j.Record == null)
                j.Record = new EnterableBuilding
                {
                    Footprint = j.F, Parent = j.Site.Town.World, StoreyHeight = GroundStorey, Storeys = 1, WallThickness = Thick,
                    Seed = unchecked((int)MapStyle.Hash(j.F.Data.id, SaltInterior)),
                };
            Vector2 at = j.At(edge, s);
            j.Record.Doors.Add(new Doorway { Edge = edge, At = at, Outward = j.Out[edge], Width = w });
            j.Site.Take(at + j.Out[edge] * 1.2f, 1.1f);   // keep the doorstep clear of props
        }

        // Garage, roll-up and barn doors; returns the wall the people's door should try first.
        private static int BigDoors(Job j, int front)
        {
            Plan p = j.P;
            int edge = front;
            bool barn = p.Arch == Arch.Barn || p.Arch == Arch.TobaccoBarn || p.Arch == Arch.Rickhouse;
            if (barn)
            {
                // A barn's big doors are in its gable ends: the end nearest the road, and a tobacco barn's both.
                int best = -1;
                float bestScore = float.MinValue;
                for (int i = 0; i < j.N; i++)
                {
                    if (j.Sh.EdgeAxis[i] != 1 || j.Len[i] < p.BigDoorWidth + 0.8f || j.Party(i)) continue;
                    float score = j.Len[i] + (p.RoadDir == Vector2.zero ? 0f : Vector2.Dot(j.Out[i], p.RoadDir) * 6f);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = i;
                }
                if (best >= 0) edge = best;
            }
            float len = j.Len[edge];
            int count = Mathf.Clamp(p.BigDoors, 1, Mathf.Max(1, Mathf.FloorToInt((len - 1.5f) / (p.BigDoorWidth + 1.6f))));
            float width = Mathf.Min(p.BigDoorWidth, len - 1.2f);
            if (width < 1.8f) return front;
            for (int k = 0; k < count; k++)
            {
                float s = count == 1 ? len * 0.5f : Mathf.Lerp(width * 0.5f + 0.9f, len - width * 0.5f - 0.9f, k / (float)(count - 1));
                j.Holes.Add(new Hole { Edge = edge, Storey = 0, S = s, W = width, Sill = 0f, Head = p.BigDoorHeight, Win = WindowKind.None,
                    Door = DoorKind.Plain, Closed = true });
            }
            if (p.Arch == Arch.TobaccoBarn)
            {
                int back = Opposite(j, edge);
                if (back >= 0 && j.Len[back] >= width + 0.8f)
                    j.Holes.Add(new Hole { Edge = back, Storey = 0, S = j.Len[back] * 0.5f, W = width, Sill = 0f, Head = p.BigDoorHeight,
                        Win = WindowKind.None, Door = DoorKind.Plain, Closed = true });
            }
            // The people's door: beside the big ones if there's room, else along the next wall.
            if (len - count * (width + 0.3f) >= DoorWidth + 1.6f) return edge;
            int side = LongestFree(j, edge);
            return side >= 0 ? side : edge;
        }

        // The longest wall not shared with a neighbour (and not <paramref name="except"/>), or -1.
        private static int LongestFree(Job j, int except)
        {
            int best = -1;
            for (int i = 0; i < j.N; i++)
                if (i != except && !j.Party(i) && j.Len[i] >= DoorWidth + 0.9f && (best < 0 || j.Len[i] > j.Len[best])) best = i;
            return best;
        }

        // The wall facing most nearly away from wall <paramref name="edge"/>.
        private static int Opposite(Job j, int edge)
        {
            int best = -1;
            float bestDot = -0.5f;
            for (int i = 0; i < j.N; i++)
            {
                if (i == edge || j.Party(i) || j.Len[i] < 1.5f) continue;
                float d = -Vector2.Dot(j.Out[i], j.Out[edge]) + j.Len[i] * 0.01f;
                if (d <= bestDot) continue;
                bestDot = d;
                best = i;
            }
            return best;
        }

        private static void Windows(Job j)
        {
            Plan p = j.P;
            long id = j.F.Data.id;
            int front = j.DoorEdge >= 0 ? j.DoorEdge : p.Front;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 1.4f || j.Party(i)) continue;
                bool isFront = i == front;
                bool alongLength = j.Sh.EdgeAxis[i] == 0;
                for (int k = 0; k < p.Storeys; k++)
                {
                    WindowKind kind = k == 0 ? (isFront ? p.FrontWindow : p.GroundWindow) : p.UpperWindow;
                    if (kind == WindowKind.None) continue;
                    // Churches, barns and warehouses light their long walls only; a store's sides stay blank.
                    if ((p.Arch == Arch.Church || p.Arch == Arch.Warehouse || p.Arch == Arch.Barn || p.Arch == Arch.Rickhouse)
                        && !alongLength) continue;
                    if (p.Arch == Arch.Store && !isFront && k == 0 && Unit(id, i, 1) < 0.6f) continue;
                    if (kind == WindowKind.Storefront)
                    {
                        if (k == 0 && !j.Lite) Storefront(j, i);
                        else if (k == 0) j.Holes.Add(new Hole { Edge = i, Storey = 0, S = len * 0.5f, W = len - 0.8f, Sill = 0.5f, Head = 2.85f, Win = WindowKind.Storefront });
                        continue;
                    }
                    if (p.Arch == Arch.Motel && isFront)
                    {
                        MotelBays(j, i, k);
                        continue;
                    }
                    Size(p, kind, k, len, out float w, out float h, out float sill, out float spacing);
                    float floor = j.FloorY(k);
                    if (kind == WindowKind.Band)
                    {
                        if (j.TopAt(i, len * 0.5f) >= floor + sill + h + 0.2f && len > 2.4f)
                            AddWindow(j, i, k, len * 0.5f, len - 1.2f, sill, h, kind, id);
                        continue;
                    }
                    int n = Mathf.Max(1, Mathf.RoundToInt(len / spacing));
                    if (len < w + 0.7f) continue;
                    for (int b = 0; b < n; b++)
                    {
                        float s = len * (b + 0.5f) / n;
                        if (j.TopAt(i, s) < floor + sill + h + 0.2f) continue;
                        if (k == 0 && !Clear(j, i, s, w - 0.3f)) continue;
                        AddWindow(j, i, k, s, w, sill, h, kind, id);
                    }
                }
            }
        }

        private static float Unit(long id, int edge, int k) => MapStyle.Unit(id, SaltWindowsLit + edge * 31 + k * 7);

        private static void AddWindow(Job j, int edge, int storey, float s, float w, float sill, float h, WindowKind kind, long id)
        {
            bool lit = j.P.Lit && Unit(id, edge, storey * 13 + Mathf.RoundToInt(s * 3f)) < 0.3f;
            j.Holes.Add(new Hole { Edge = edge, Storey = storey, S = s, W = w, Sill = sill, Head = sill + h, Win = kind, Lit = lit });
            if (storey != 0 || j.Record == null) return;
            j.Record.Windows.Add(new WindowSpan { Edge = edge, Storey = 0, From = s - w * 0.5f, To = s + w * 0.5f, Sill = sill, Head = sill + h });
        }

        private static void Size(Plan p, WindowKind kind, int storey, float len, out float w, out float h, out float sill, out float spacing)
        {
            spacing = p.Spacing;
            switch (kind)
            {
                case WindowKind.Picture: w = 1.8f; h = 1.25f; sill = 0.95f; spacing = Mathf.Max(spacing, 3.4f); break;
                case WindowKind.Ribbon: w = Mathf.Min(2.6f, spacing - 0.7f); h = p.WindowHeight; sill = p.Sill; break;
                case WindowKind.Band: w = len - 1.2f; h = 1.4f; sill = 0.9f; break;
                case WindowKind.Arched: w = p.WindowWidth; h = p.WindowHeight; sill = p.Sill; break;
                case WindowKind.Small:
                    w = p.Arch == Arch.Trailer ? 0.9f : 1.0f;
                    h = p.Arch == Arch.Rickhouse || p.Arch == Arch.Warehouse || p.Arch == Arch.Barn ? p.WindowHeight : 0.8f;
                    sill = p.Arch == Arch.Row || p.Arch == Arch.Store || p.Arch == Arch.Garage || p.Arch == Arch.Shed ? 1.3f : p.Sill;
                    if (p.Arch == Arch.Row) spacing = 4.5f;
                    break;
                default:
                    w = p.WindowWidth;
                    h = storey == 0 ? p.WindowHeight : p.WindowHeight * 0.95f;
                    sill = storey == 0 ? p.Sill : 0.75f;
                    if (p.Arch == Arch.Row && storey > 0) h = Mathf.Min(h, p.Storey - 1.1f);
                    break;
            }
        }

        // A shop front: display glass either side of the door between brick piers, bays of about three metres.
        private static void Storefront(Job j, int edge)
        {
            float len = j.Len[edge];
            const float Pier = 0.4f;
            float a = Pier, b = len - Pier;
            float doorA = -1f, doorB = -1f;
            foreach (Hole h in j.Holes)
                if (h.Edge == edge && h.Storey == 0 && h.IsDoor) { doorA = h.S - h.W * 0.5f - 0.12f; doorB = h.S + h.W * 0.5f + 0.12f; }
            if (doorA >= 0f)
            {
                StorefrontRun(j, edge, a, doorA);
                StorefrontRun(j, edge, doorB, b);
            }
            else StorefrontRun(j, edge, a, b);
        }

        private static void StorefrontRun(Job j, int edge, float a, float b)
        {
            float len = b - a;
            if (len < 0.7f) return;
            int panes = Mathf.Max(1, Mathf.RoundToInt(len / 6f));
            float each = len / panes;
            for (int k = 0; k < panes; k++)
            {
                float s0 = a + each * k + (k > 0 ? 0.2f : 0f), s1 = a + each * (k + 1) - (k < panes - 1 ? 0.2f : 0f);
                AddWindow(j, edge, 0, (s0 + s1) * 0.5f, s1 - s0, 0.5f, 2.35f, WindowKind.Storefront, j.F.Data.id);
            }
        }

        // A motel's front: a door and a window to each room, along both floors.
        private static void MotelBays(Job j, int edge, int storey)
        {
            float len = j.Len[edge];
            int bays = Mathf.Max(1, Mathf.FloorToInt(len / 3.8f));
            float each = len / bays;
            for (int b = 0; b < bays; b++)
            {
                float c = each * (b + 0.5f);
                if (storey == 0 && !Clear(j, edge, c - 0.8f, 0.95f)) continue;
                j.Holes.Add(new Hole { Edge = edge, Storey = storey, S = c - 0.8f, W = 0.95f, Sill = 0f, Head = 2.1f, Win = WindowKind.None,
                    Door = DoorKind.House, Closed = true });
                AddWindow(j, edge, storey, c + 0.65f, 1.3f, 0.95f, 1.15f, WindowKind.Picture, j.F.Data.id);
            }
        }
        #endregion

        #region Drawing
        private static void Openings(Job j)
        {
            foreach (Hole h in j.Holes)
            {
                if (h.IsDoor) DoorShape(j, h);
                else WindowShape(j, h);
            }
        }

        private static void WindowShape(Job j, Hole h)
        {
            Plan p = j.P;
            int e = h.Edge;
            float floor = j.FloorY(h.Storey);
            RaidMesh m = h.Storey == 0 ? j.Facing(e) : j.Upper;
            float y0 = floor + h.Sill, y1 = floor + h.Head, yc = (y0 + y1) * 0.5f, height = y1 - y0;
            Surf glass = h.Lit ? LitGlass : j.GlassS;
            if (j.Lite)
            {
                OnWall(j, m, e, h.S, yc, h.W, height, 0.01f, glass);
                return;
            }
            bool masonry = p.Wall == WallKind.Brick || p.Wall == WallKind.Stone || p.Wall == WallKind.Block || p.Wall == WallKind.Stucco
                || p.Wall == WallKind.Concrete;
            Surf frame = p.Arch == Arch.Row || p.Arch == Arch.Store ? j.AccentS : j.TrimS;
            switch (h.Win)
            {
                case WindowKind.Storefront: StorefrontShape(j, h, m); return;
                case WindowKind.Arched: ArchedShape(j, h, m, y0, y1); return;
            }
            OnWall(j, m, e, h.S, yc, h.W + 0.14f, height + 0.14f, 0.006f, frame);
            OnWall(j, m, e, h.S, yc, h.W, height, 0.012f, glass);
            switch (h.Win)
            {
                case WindowKind.Sash:
                    OnWall(j, m, e, h.S, y0 + height * 0.52f, h.W, 0.05f, 0.016f, frame);
                    break;
                case WindowKind.Picture:
                    OnWall(j, m, e, h.S, yc, 0.06f, height, 0.016f, frame);
                    break;
                case WindowKind.Ribbon:
                case WindowKind.Band:
                {
                    int bars = Mathf.Max(1, Mathf.RoundToInt(h.W / (h.Win == WindowKind.Band ? 1.6f : 0.75f)));
                    for (int k = 1; k < bars; k++)
                        OnWall(j, m, e, h.S - h.W * 0.5f + h.W * k / bars, yc, 0.06f, height, 0.016f, frame);
                    OnWall(j, m, e, h.S, y0 + height * 0.68f, h.W, 0.06f, 0.016f, frame);
                    break;
                }
            }
            Surf sill = masonry ? WallSurf(WallKind.Stone, 0) : j.TrimS;
            OnWallBox(j, m, e, h.S, y0 - 0.04f, h.W + 0.24f, 0.07f, 0.09f, sill);
            if (h.Win != WindowKind.Band && h.Win != WindowKind.Ribbon)
            {
                if (masonry) OnWallBox(j, m, e, h.S, y1 + 0.1f, h.W + 0.32f, 0.17f, 0.05f, sill);
                else OnWallBox(j, m, e, h.S, y1 + 0.07f, h.W + 0.26f, 0.12f, 0.04f, j.TrimS);
            }
            if (p.Shutters && h.Win == WindowKind.Sash)
            {
                float sw = h.W * 0.42f, off = h.W * 0.5f + 0.1f + sw * 0.5f;
                OnWall(j, m, e, h.S - off, yc, sw, height, 0.02f, j.AccentS);
                OnWall(j, m, e, h.S + off, yc, sw, height, 0.02f, j.AccentS);
            }
            if (h.Storey == 0) Inner(j, m, e, h.S, yc, h.W, height, glass);
        }

        // The pane seen from inside, on the wall's inner face.
        private static void Inner(Job j, RaidMesh m, int e, float s, float y, float w, float h, in Surf glass)
        {
            Vector2 c = j.At(e, s) - j.Out[e] * (Thick + 0.006f);
            m.Upright(new Vector3(c.x, y, c.y), To3(-j.Out[e], 0f), w, h, glass);
        }

        private static void StorefrontShape(Job j, Hole h, RaidMesh m)
        {
            int e = h.Edge;
            Surf frame = j.AccentS;
            Surf glass = h.Lit ? LitGlass : DecorPalette.Gloss(new Color(0.13f, 0.16f, 0.19f));
            float w = h.W;
            OnWallBox(j, m, e, h.S, 0.25f, w, 0.5f, 0.09f, frame);                 // the kick plate
            OnWall(j, m, e, h.S, 1.5f, w, 1.92f, 0.015f, glass);                   // display glass
            OnWall(j, m, e, h.S, 2.68f, w, 0.34f, 0.015f, glass);                  // the transom
            OnWallBox(j, m, e, h.S, 2.5f, w + 0.1f, 0.08f, 0.06f, frame);
            OnWallBox(j, m, e, h.S, 2.9f, w + 0.14f, 0.1f, 0.06f, frame);
            OnWallBox(j, m, e, h.S, 0.53f, w, 0.06f, 0.1f, frame);
            int bars = Mathf.Max(1, Mathf.RoundToInt(w / 1.5f));
            for (int k = 0; k <= bars; k++)
                OnWallBox(j, m, e, h.S - w * 0.5f + w * k / bars, 1.7f, 0.08f, 2.4f, 0.06f, frame);
            Inner(j, m, e, h.S, 1.7f, w, 2.3f, glass);
        }

        // A tall church window with a round head, in coloured glass: blue, ruby, amber and green panes.
        private static void ArchedShape(Job j, Hole h, RaidMesh m, float y0, float y1)
        {
            int e = h.Edge;
            float w = h.W, r = w * 0.5f, spring = y1 - r;
            System.Random rng = MapStyle.Rng(j.F.Data.id + e * 977 + Mathf.RoundToInt(h.S * 10f), SaltWindowsLit);
            Color[] stain = { new Color(0.12f, 0.18f, 0.44f), new Color(0.48f, 0.10f, 0.10f), new Color(0.58f, 0.40f, 0.12f), new Color(0.12f, 0.32f, 0.18f) };
            Color tone = stain[rng.Next(stain.Length)], other = stain[rng.Next(stain.Length)];
            Surf glass = h.Lit ? DecorPalette.Glow(tone * 0.9f) : DecorPalette.Gloss(tone * 0.6f);
            Surf glass2 = h.Lit ? DecorPalette.Glow(other * 0.9f) : DecorPalette.Gloss(other * 0.6f);
            Surf frame = j.P.Wall == WallKind.Siding ? j.TrimS : WallSurf(WallKind.Stone, 0);
            float rect = spring - y0;
            OnWall(j, m, e, h.S, (y0 + spring) * 0.5f, w + 0.16f, rect, 0.006f, frame);
            OnWall(j, m, e, h.S - w * 0.25f, (y0 + spring) * 0.5f, w * 0.5f, rect - 0.08f, 0.012f, glass);
            OnWall(j, m, e, h.S + w * 0.25f, (y0 + spring) * 0.5f, w * 0.5f, rect - 0.08f, 0.012f, glass2);
            OnWall(j, m, e, h.S, (y0 + spring) * 0.5f, 0.05f, rect, 0.016f, frame);
            // The round head: a fan of the frame, the glass in front of it.
            Vector2 c = j.At(e, h.S);
            Vector3 o = To3(j.Out[e], 0f), along = To3(j.Dir[e], 0f);
            const int Segs = 7;
            for (int k = 0; k < Segs; k++)
            {
                float a0 = Mathf.PI * k / Segs, a1 = Mathf.PI * (k + 1) / Segs;
                Vector3 centre = new Vector3(c.x, spring, c.y);
                Vector3 p0 = centre + (along * Mathf.Cos(a0) + Vector3.up * Mathf.Sin(a0)), p1 = centre + (along * Mathf.Cos(a1) + Vector3.up * Mathf.Sin(a1));
                Vector3 dir0 = p0 - centre, dir1 = p1 - centre;
                Tri3(m, centre + o * 0.006f, centre + dir0 * (r + 0.08f) + o * 0.006f, centre + dir1 * (r + 0.08f) + o * 0.006f, o, frame);
                Tri3(m, centre + o * 0.012f, centre + dir0 * (r - 0.04f) + o * 0.012f, centre + dir1 * (r - 0.04f) + o * 0.012f, o, k < Segs / 2 ? glass2 : glass);
            }
            OnWallBox(j, m, e, h.S, y0 - 0.05f, w + 0.3f, 0.09f, 0.1f, frame);
            if (h.Storey == 0) Inner(j, m, e, h.S, (y0 + spring) * 0.5f, w, rect, glass);
        }

        private static void DoorShape(Job j, Hole h)
        {
            Plan p = j.P;
            int e = h.Edge;
            RaidMesh m = h.Storey == 0 ? j.Facing(e) : j.Upper;
            float floor = j.FloorY(h.Storey);
            float w = h.W, top = floor + h.Head;
            if (h.Closed)
            {
                ClosedDoor(j, h, m, floor);
                return;
            }
            bool formal = p.Arch == Arch.Church || p.Arch == Arch.Courthouse || p.Arch == Arch.Civic;
            Surf casing = p.Arch == Arch.Row || p.Arch == Arch.Store ? j.AccentS : formal && p.Wall != WallKind.Siding ? WallSurf(WallKind.Stone, 0) : j.TrimS;
            float c = formal ? 0.2f : 0.11f;
            OnWallBox(j, m, e, h.S - w * 0.5f - c * 0.5f, top * 0.5f, c, top, 0.06f, casing);
            OnWallBox(j, m, e, h.S + w * 0.5f + c * 0.5f, top * 0.5f, c, top, 0.06f, casing);
            OnWallBox(j, m, e, h.S, top + c * 0.6f, w + c * 2f, c * 1.2f, 0.07f, casing);
            if ((h.Door == DoorKind.Shop || h.Door == DoorKind.Double) && top + 0.6f < GroundStorey)
            {
                OnWall(j, m, e, h.S, top + 0.42f, w, 0.42f, 0.012f, j.GlassS);
                OnWallBox(j, m, e, h.S, top + 0.68f, w + c * 2f, 0.08f, 0.06f, casing);
            }
        }

        private static void ClosedDoor(Job j, Hole h, RaidMesh m, float floor)
        {
            Plan p = j.P;
            int e = h.Edge;
            float w = h.W, ht = h.Head, mid = floor + ht * 0.5f;
            if (j.Lite)
            {
                OnWallBox(j, m, e, h.S, mid, w, ht, 0.04f, DecorPalette.Matte(new Color(0.55f, 0.55f, 0.53f)));
                return;
            }
            switch (p.Arch)
            {
                case Arch.Barn: case Arch.TobaccoBarn:
                {
                    // Sliding barn doors on a rail, braced with a white X on a red barn.
                    Surf boards = WallSurf(WallKind.Boards, p.WallTone);
                    OnWallBox(j, m, e, h.S, mid, w, ht, 0.07f, boards);
                    Surf brace = p.WallTone == BoardRed ? DecorPalette.Matte(new Color(0.92f, 0.91f, 0.88f)) : DecorPalette.Matte(TonesOf(p.Wall)[p.WallTone] * 0.6f);
                    Vector2 c = j.At(e, h.S) + j.Out[e] * 0.09f;
                    Vector3 o = To3(j.Out[e], 0f), along = To3(j.Dir[e], 0f);
                    for (int half = -1; half <= 1; half += 2)
                    {
                        Vector3 centre = new Vector3(c.x, mid, c.y) + along * (w * 0.25f * half);
                        Vector3 d0 = along * (w * 0.23f) + Vector3.up * (ht * 0.46f), d1 = along * (w * 0.23f) - Vector3.up * (ht * 0.46f);
                        Beam(m, centre - d0, centre + d0, 0.14f, 0.04f, o, brace);
                        Beam(m, centre - d1, centre + d1, 0.14f, 0.04f, o, brace);
                        OnWallBox(j, m, e, h.S + w * 0.25f * half, mid, w * 0.5f - 0.04f, 0.12f, 0.09f, brace);
                    }
                    OnWallBox(j, m, e, h.S, floor + ht + 0.1f, w * 2.1f, 0.12f, 0.1f, DecorPalette.Metal(new Color(0.22f, 0.22f, 0.23f)));
                    return;
                }
                case Arch.Warehouse: case Arch.Industrial: case Arch.Rickhouse: case Arch.Civic:
                {
                    // A roll-up door with its drum housing over it.
                    Surf slats = p.Arch == Arch.Civic ? DecorPalette.Matte(new Color(0.62f, 0.12f, 0.10f)) : WallSurf(WallKind.Metal, 2);
                    OnWallBox(j, m, e, h.S, mid, w, ht, 0.06f, slats);
                    for (int k = 1; k < 6; k++)
                        OnWall(j, m, e, h.S, floor + ht * k / 6f, w, 0.03f, 0.065f, DecorPalette.Matte(new Color(0.35f, 0.35f, 0.36f)));
                    OnWallBox(j, m, e, h.S, floor + ht + 0.22f, w + 0.3f, 0.44f, 0.18f, DecorPalette.Metal(new Color(0.5f, 0.5f, 0.5f)));
                    OnWallBox(j, m, e, h.S - w * 0.5f - 0.08f, mid, 0.16f, ht, 0.1f, j.TrimS);
                    OnWallBox(j, m, e, h.S + w * 0.5f + 0.08f, mid, 0.16f, ht, 0.1f, j.TrimS);
                    return;
                }
                case Arch.Motel:
                    OnWallBox(j, m, e, h.S, mid, w, ht, 0.04f, j.AccentS);
                    OnWallBox(j, m, e, h.S, floor + ht + 0.06f, w + 0.2f, 0.12f, 0.06f, j.TrimS);
                    OnWall(j, m, e, h.S, floor + 1.55f, 0.18f, 0.12f, 0.05f, DecorPalette.Metal(new Color(0.7f, 0.62f, 0.3f)));
                    return;
                default:
                {
                    // A garage's paneled overhead door.
                    Surf panel = DecorPalette.Matte(new Color(0.90f, 0.89f, 0.85f));
                    OnWallBox(j, m, e, h.S, mid, w, ht, 0.05f, panel);
                    for (int k = 1; k < 4; k++)
                        OnWall(j, m, e, h.S, floor + ht * k / 4f, w, 0.03f, 0.055f, DecorPalette.Matte(new Color(0.6f, 0.6f, 0.58f)));
                    OnWallBox(j, m, e, h.S, floor + ht + 0.06f, w + 0.24f, 0.12f, 0.06f, j.TrimS);
                    return;
                }
            }
        }
        #endregion
    }
}
