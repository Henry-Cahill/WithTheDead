using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The rooms of a home, furnished like the starter home and the raid town's houses with their own working pieces: the
    // living room's sofa facing the TV, the kitchen's run of fridge, worktop, sink and range under the wall cupboards, the
    // bed between its nightstands, the bath, the den's desk, the laundry's washer, the garage's car and bench, the shed.
    internal static partial class InteriorPass
    {
        private static bool Has(InteriorPlan plan, RoomRole role, int unit)
        {
            foreach (PlanRoom r in plan.Rooms)
                if (r.Role == role && r.Unit == unit) return true;
            return false;
        }

        #region Living rooms
        private static void Living(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            bool home = c.Plan.Kind == RaidBuildingKind.House;
            // A cabin's or a flat's one room: the kitchenette along a wall, and the bed in a corner when there's no bedroom.
            if (!Has(c.Plan, RoomRole.Kitchen, c.P.Unit)) Kitchenette(c);
            if (home && !Has(c.Plan, RoomRole.Bedroom, c.P.Unit) && c.Area >= 13f) Bed(c, true);

            // The sofa against the longest free wall, the TV across the room facing it, the coffee table between them.
            int[] order = c.Sides(false);
            int sofaSide = -1;
            RaidBuilding.Place sofa = default;
            float sofaW = 0f;
            foreach (int s in order)
            {
                if (s == c.DoorSide && order.Length > 1 && c.Len(s) < 5f) continue;
                sofaW = Mathf.Clamp(c.Len(s) - 1.4f, 1.4f, 2.2f);
                if (c.Wall(s, sofaW, 0.88f, 0.85f, 0.5f, out sofa)) { sofaSide = s; break; }
            }
            Color fabric = Upholstery[rng.Next(Upholstery.Length)];
            if (sofaSide >= 0)
            {
                b.HouseSofa(c.R, sofa, sofaW, fabric);
                int opp = RoomCtx.Opposite(sofaSide);
                bool alongX = sofaSide == 0 || sofaSide == 2;
                float across = alongX ? c.Depth : c.Width, t = alongX ? sofa.X : sofa.Z;
                c.Span(opp, out float lo, out float hi);
                if (across >= 2.7f && c.Wall(opp, 1.0f, 0.45f, 1.1f, Mathf.InverseLerp(lo + 0.5f, hi - 0.5f, t), out RaidBuilding.Place tv))
                    b.HouseTv(c.R, tv);
                if (across >= 3.3f)
                {
                    Vector2 front = c.OnWall(sofaSide, t, 0.88f + 0.5f + 0.3f);
                    if (c.Floor(alongX ? 1.0f : 0.55f, alongX ? 0.55f : 1.0f, 0f, front, 0.12f, out RaidBuilding.Place table))
                    {
                        b.HouseCoffeeTable(c.R, alongX ? table : new RaidBuilding.Place(table.X, table.Z, 90f), 1.0f, 0.55f);
                        b.RugAt(table.X, table.Z, alongX ? 2.2f : 1.6f, alongX ? 1.6f : 2.2f, 0f, rng.Next(3));
                    }
                }
                // The standard lamp at the sofa's end.
                float end = t + (rng.NextDouble() < 0.5 ? -1f : 1f) * (sofaW * 0.5f + 0.3f);
                if (c.WallAt(sofaSide, end, 0.36f, 0.36f, 1.55f, out RaidBuilding.Place lamp))
                    LampPiece(c, lamp, HomeFurnitureKind.FloorLamp);
            }
            // An armchair turned toward the room from a corner, and a bookcase.
            if (c.Area >= 11f)
            {
                Vector2 corner = new Vector2(rng.NextDouble() < 0.5 ? c.Inner.xMin + 0.7f : c.Inner.xMax - 0.7f,
                    rng.NextDouble() < 0.5 ? c.Inner.yMin + 0.7f : c.Inner.yMax - 0.7f);
                if (c.Floor(0.95f, 0.88f, 0f, corner, 0.05f, out RaidBuilding.Place chair))
                {
                    Vector2 toward = c.Inner.center - new Vector2(chair.X, chair.Z);
                    chair.Yaw = Mathf.Atan2(toward.x, toward.y) * Mathf.Rad2Deg;
                    b.HomePiece(c.R, chair, HomeFurnitureKind.Armchair);
                }
            }
            if (c.Area >= 9f && c.AnyWall(c.Sides(false), 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place shelf, out _))
                b.HomePiece(c.R, shelf, HomeFurnitureKind.Bookshelf);
            if (c.Area > 18f) MoreLoot(c, 1, c.Sides(false));
            WallDecor(c, 2);
        }

        // A kitchenette along one wall of a cabin's or a flat's living room: the sink, the range and the fridge. False when no
        // wall had room for it.
        private static bool Kitchenette(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            foreach (int side in order)
            {
                c.FreeRun(side, out float a, out float e);
                if (e - a < 2.2f) continue;
                float t = a + 0.05f;
                if (c.WallAt(side, t + 0.61f, 1.22f, 0.63f, 1.0f, out RaidBuilding.Place sink))
                {
                    Sink(c, sink);
                    t += 1.24f;
                }
                if (c.WallAt(side, t + 0.38f, 0.76f, 0.62f, 1.08f, out RaidBuilding.Place range))
                {
                    b.HouseStove(c.R, range);
                    t += 0.78f;
                }
                if (c.WallAt(side, t + 0.36f, 0.72f, 0.70f, 1.72f, out RaidBuilding.Place fridge)) b.HomePiece(c.R, fridge, HomeFurnitureKind.Fridge);
                if (c.Inside[side]) b.UpperCabinets(c.R, RoomCtx.Char(side), a + 0.05f + 0.61f, 1.2f, FurnitureBuilder.HomeCabinetPaint);
                return true;
            }
            return false;
        }
        #endregion

        #region Kitchens and dining rooms
        private static void Kitchen(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            // The worktop run along the wall with the most clear wall (a partition by choice, for the cupboards over it).
            int best = -1;
            float bestScore = -1f;
            for (int s = 0; s < 4; s++)
            {
                c.FreeRun(s, out float a0, out float b0);
                float score = (b0 - a0) + (c.Inside[s] ? 0.8f : 0f) + (c.Panes[s].Count > 0 ? 0.4f : 0f);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            c.FreeRun(best, out float a, out float e);
            float len = e - a;
            bool fridgeDone = false, stoveDone = false, sinkDone = false;
            float t = a + 0.03f, counterFrom = float.MaxValue, counterTo = float.MinValue;
            RaidBuilding.Place counterAt = default;
            bool haveCounter = false;
            // Fridge, worktop, sink, range, worktop: as much as the wall takes.
            if (len >= 3.3f && c.WallAt(best, t + 0.36f, 0.72f, 0.70f, 1.72f, out RaidBuilding.Place fridge))
            {
                b.HomePiece(c.R, fridge, HomeFurnitureKind.Fridge);
                t += 0.76f;
                fridgeDone = true;
            }
            if (len >= 4.4f && c.WallAt(best, t + 0.61f, 1.22f, 0.63f, 0.95f, out RaidBuilding.Place counter))
            {
                b.HomePiece(c.R, counter, HomeFurnitureKind.Counter);
                if (rng.NextDouble() < 0.6) b.HomePiece(c.R, b.OffsetPlace(counter, -0.25f, -0.075f), HomeFurnitureKind.Radio);
                counterFrom = Mathf.Min(counterFrom, t);
                counterTo = Mathf.Max(counterTo, t + 1.22f);
                t += 1.24f;
                counterAt = counter;
                haveCounter = true;
            }
            if (c.WallAt(best, t + 0.61f, 1.22f, 0.63f, 1.0f, out RaidBuilding.Place sink))
            {
                Sink(c, sink);
                counterFrom = Mathf.Min(counterFrom, t);
                counterTo = Mathf.Max(counterTo, t + 1.22f);
                t += 1.24f;
                sinkDone = true;
            }
            if (c.WallAt(best, t + 0.38f, 0.76f, 0.62f, 1.08f, out RaidBuilding.Place range))
            {
                b.HouseStove(c.R, range);
                t += 0.78f;
                stoveDone = true;
            }
            if (e - t >= 1.25f && c.WallAt(best, t + 0.61f, 1.22f, 0.63f, 0.95f, out RaidBuilding.Place counter2))
            {
                b.HomePiece(c.R, counter2, HomeFurnitureKind.Counter);
                counterFrom = Mathf.Min(counterFrom, t);
                counterTo = Mathf.Max(counterTo, t + 1.22f);
                if (!haveCounter && rng.NextDouble() < 0.6) b.HomePiece(c.R, b.OffsetPlace(counter2, -0.25f, -0.075f), HomeFurnitureKind.Radio);
                t += 1.24f;
            }
            // The wall cupboards over the worktops, when the wall is one the cupboards can hang on.
            if (c.Inside[best] && counterTo > counterFrom)
                b.UpperCabinets(c.R, RoomCtx.Char(best), (counterFrom + counterTo) * 0.5f, counterTo - counterFrom, FurnitureBuilder.HomeCabinetPaint);
            // Whatever didn't fit goes on another wall.
            int[] others = c.Sides(true);
            if (!sinkDone && c.AnyWall(others, 1.22f, 0.63f, 1.0f, -1f, out RaidBuilding.Place s2, out _)) Sink(c, s2);
            if (!stoveDone && c.AnyWall(others, 0.76f, 0.62f, 1.08f, -1f, out RaidBuilding.Place r2, out _)) b.HouseStove(c.R, r2);
            if (!fridgeDone && c.AnyWall(others, 0.72f, 0.70f, 1.72f, 0f, out RaidBuilding.Place f2, out _)) b.HomePiece(c.R, f2, HomeFurnitureKind.Fridge);

            // The table and chairs, unless the house has a dining room.
            if (!Has(c.Plan, RoomRole.Dining, c.P.Unit)) TableAndChairs(c, false);
            if (c.Area > 12f) MoreLoot(c, 1, c.Sides(true));
            WallDecor(c, 4);
        }

        private static void Dining(RoomCtx c)
        {
            TableAndChairs(c, true);
            if (c.AnyWall(c.Sides(false), 1.04f, 0.5f, 0.9f, 0.5f, out RaidBuilding.Place hutch, out _))
                c.B.HomePiece(c.R, hutch, HomeFurnitureKind.Dresser);
            MoreLoot(c, c.Area > 12f ? 1 : 0, c.Sides(false));
            WallDecor(c, 2);
        }

        // A table out on the floor with chairs pulled up round it; a dining room's is laid on a rug.
        private static void TableAndChairs(RoomCtx c, bool rug)
        {
            RaidBuilding b = c.B;
            if (c.Width < 2.4f || c.Depth < 2.4f) return;
            if (!c.Floor(1.0f, 0.8f, 0f, c.Inner.center, 0.6f, out RaidBuilding.Place table)) return;
            b.HomePiece(c.R, table, HomeFurnitureKind.Table);
            if (rug) b.RugAt(table.X, table.Z, 2.4f, 2.0f, 0f, c.Rng.Next(3));
            // The chairs round it, as the ranch house sets them; one pushed back.
            var seats = new[] { new RaidBuilding.Place(table.X - 0.95f, table.Z, 90f), new RaidBuilding.Place(table.X + 0.95f, table.Z, 270f),
                new RaidBuilding.Place(table.X, table.Z + 0.85f, 180f), new RaidBuilding.Place(table.X, table.Z - 0.85f, 0f) };
            int n = c.Area > 9f ? 4 : 2;
            for (int i = 0; i < n; i++)
            {
                RaidBuilding.Place s = seats[i];
                var fp = new Rect(s.X - 0.23f, s.Z - 0.23f, 0.46f, 0.46f);
                if (!c.Inner.Contains(new Vector2(fp.xMin, fp.yMin)) || !c.Inner.Contains(new Vector2(fp.xMax, fp.yMax))) continue;
                if (!c.Clear(fp)) continue;
                if (c.Rng.NextDouble() < 0.3) s.Yaw += MapStyle.Range(c.Rng, -25f, 25f);
                c.Taken.Add(fp);
                b.HomePiece(c.R, s, HomeFurnitureKind.Chair);
            }
        }
        #endregion

        #region Bathrooms
        private static void Bathroom(RoomCtx c, bool public_)
        {
            RaidBuilding b = c.B;
            if (public_)
            {
                Restroom(c);
                return;
            }
            int[] order = c.Sides(true);
            // The bath along a wall it fills, else a shower stall in a corner.
            bool bath = false;
            foreach (int s in order)
            {
                float across = s == 0 || s == 2 ? c.Depth : c.Width;
                if (c.Len(s) < FurnitureBuilder.BathLength + 0.05f || across < 1.7f) continue;
                if (c.Wall(s, FurnitureBuilder.BathLength, FurnitureBuilder.BathDepth, FurnitureBuilder.BathHeight, c.Rng.NextDouble() < 0.5 ? 0f : 1f,
                    out RaidBuilding.Place tub))
                {
                    b.HouseBath(c.R, tub, c.Rng.NextDouble() < 0.5);
                    bath = true;
                    break;
                }
            }
            if (!bath && c.AnyWall(order, 0.9f, 0.9f, 2.05f, 0f, out RaidBuilding.Place shower, out _))
                b.HomePiece(c.R, shower, HomeFurnitureKind.Shower);
            if (c.AnyWall(order, 0.42f, 0.70f, 0.8f, -1f, out RaidBuilding.Place toilet, out int ts))
            {
                Toilet(c, toilet);
                if (c.Inside[ts]) TowelRail(c, ts, ts == 0 || ts == 2 ? toilet.X : toilet.Z);
            }
            if (c.AnyWall(order, 0.52f, 0.45f, 0.9f, -1f, out RaidBuilding.Place basin, out int bs))
            {
                b.HomePiece(c.R, basin, HomeFurnitureKind.Basin);
                if (c.Inside[bs]) b.HangBathCabinet(c.R, RoomCtx.Char(bs), bs == 0 || bs == 2 ? basin.X : basin.Z, 1.5f);
            }
        }

        // A public restroom: a row of stalls along one wall, the basins along another.
        private static void Restroom(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int side = c.Sides(true)[0];
            c.FreeRun(side, out float a, out float e);
            int n = Mathf.Clamp(Mathf.FloorToInt((e - a - 0.2f) / 0.95f), 1, 4);
            float across = side == 0 || side == 2 ? c.Depth : c.Width;
            Surf stall = DecorPalette.Matte(new Color(0.56f, 0.60f, 0.58f));
            for (int i = 0; i < n; i++)
            {
                float t = a + 0.1f + 0.95f * (i + 0.5f);
                if (!c.WallAt(side, t, 0.42f, 0.70f, 0.8f, out RaidBuilding.Place toilet)) continue;
                Toilet(c, toilet);
                if (n > 1 && i < n - 1 && across >= 1.8f)
                {
                    // The stall's side panel.
                    var at = new RaidBuilding.Place(0f, 0f, RoomCtx.YawOf(side));
                    Vector2 q = c.OnWall(side, t + 0.475f, 0.65f);
                    at.X = q.x;
                    at.Z = q.y;
                    b.Furnish(c.R, at, new Vector3(0.04f, 1.8f, 1.3f), true, (m, r) => m.Box(new Vector3(0f, 1.0f, 0f), new Vector3(0.03f, 1.6f, 1.25f), stall), "stall");
                }
            }
            int other = RoomCtx.Opposite(side);
            for (int i = 0; i < 2; i++)
                if (c.Wall(c.Inside[other] ? other : c.Sides(true)[1], 0.52f, 0.45f, 0.9f, i == 0 ? 0.3f : 0.7f, out RaidBuilding.Place basin))
                    b.HomePiece(c.R, basin, HomeFurnitureKind.Basin);
        }
        #endregion

        #region Bedrooms
        private static void Bedroom(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            int bedSide = Bed(c, false);
            // The wardrobe (the room's own storage), a dresser with now and then a portable TV on it, a rug by the bed.
            ContainerType? own = c.NextLoot();
            if (own.HasValue) LootPiece(c, own.Value, c.Sides(true));
            else if (c.AnyWall(c.Sides(true), 1.0f, 0.6f, 1.95f, -1f, out RaidBuilding.Place wardrobe, out _)) b.Wardrobe(c.R, wardrobe, Woods[rng.Next(Woods.Length)]);
            if (c.Area >= 8f && c.AnyWall(c.Sides(false), 1.04f, 0.5f, 0.9f, -1f, out RaidBuilding.Place dresser, out _))
            {
                b.HomePiece(c.R, dresser, HomeFurnitureKind.Dresser);
                // A motel room's portable TV (a set has a light of its own: not in every bedroom).
                if (c.P.Role == RoomRole.Guest && rng.NextDouble() < 0.8) b.HomePiece(c.R, b.OffsetPlace(dresser, -0.18f, 0.05f, 30f), HomeFurnitureKind.Tv);
            }
            if (c.P.Role == RoomRole.Guest && c.Area > 9f && c.Floor(0.45f, 0.45f, 0f, c.Inner.center, 0.3f, out RaidBuilding.Place seat))
                b.WoodChairSeat(c.R, seat, Woods[rng.Next(Woods.Length)]);
            if (c.Area > 14f) MoreLoot(c, 1, c.Sides(false));
            WallDecor(c, 2);
        }

        // The bed with its head to a wall (the one facing the door if it can), a nightstand either side; or, in a cabin's one
        // room, in a corner. Returns the wall it's against, or -1.
        private static int Bed(RoomCtx c, bool corner)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            if (c.DoorSide >= 0)
            {
                int opp = RoomCtx.Opposite(c.DoorSide);
                for (int i = 0; i < order.Length; i++)
                    if (order[i] == opp) { order[i] = order[0]; order[0] = opp; break; }
            }
            foreach (int s in order)
            {
                float across = s == 0 || s == 2 ? c.Depth : c.Width;
                if (across < 2.6f) continue;
                if (!c.Wall(s, 1.4f, 2.0f, 1.1f, corner ? (c.Rng.NextDouble() < 0.5 ? 0f : 1f) : 0.5f, out RaidBuilding.Place bed)) continue;
                b.HouseBed(c.R, bed, Quilts[c.Rng.Next(Quilts.Length)]);
                float t = s == 0 || s == 2 ? bed.X : bed.Z;
                for (int e = -1; e <= 1; e += 2)
                    if (c.WallAt(s, t + e * (0.7f + 0.27f), 0.45f, 0.40f, 0.62f, out RaidBuilding.Place stand))
                    {
                        LampPiece(c, stand, HomeFurnitureKind.Nightstand);
                        if (corner) break;
                    }
                if (!corner && c.Area > 9f)
                {
                    Vector2 foot = c.OnWall(s, t, 2.0f + 0.4f);
                    var rug = new Rect(foot.x - 0.8f, foot.y - 0.5f, 1.6f, 1.0f);
                    if (c.Clear(rug)) b.RugAt(foot.x, foot.y, s == 0 || s == 2 ? 1.6f : 1.0f, s == 0 || s == 2 ? 1.0f : 1.6f, 0f, c.Rng.Next(3));
                }
                return s;
            }
            return -1;
        }

        private static void Bunks(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int n = Mathf.Clamp(Mathf.FloorToInt(c.Area / 6f), 1, 6);
            int[] order = c.Sides(false);
            for (int i = 0; i < n; i++)
            {
                if (!c.AnyWall(order, 0.95f, 2.0f, 1.75f, -1f, out RaidBuilding.Place bunk, out int side)) break;
                b.ArmyBunk(c.R, bunk);
                Vector2 foot = c.OnWall(side, side == 0 || side == 2 ? bunk.X : bunk.Z, 2.0f + 0.3f);
                if (c.Floor(0.8f, 0.42f, RoomCtx.YawOf(side) + 180f, foot, 0.02f, out RaidBuilding.Place locker)) b.Footlocker(c.R, locker);
            }
            MoreLoot(c, 2, order);
        }
        #endregion

        #region Halls, dens, laundries, closets
        private static void Hall(RoomCtx c)
        {
            RaidBuilding b = c.B;
            bool home = c.Plan.Kind == RaidBuildingKind.House;
            float narrow = Mathf.Min(c.Width, c.Depth);
            if (narrow >= 1.6f && c.Area > 6f)
            {
                if (home)
                {
                    if (c.AnyWall(c.Sides(true), 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place shelf, out _)) b.HomePiece(c.R, shelf, HomeFurnitureKind.Bookshelf);
                }
                else if (c.AnyWall(c.Sides(true), 0.32f, 0.32f, 1.4f, -1f, out RaidBuilding.Place cooler, out _)) WaterCooler(c, cooler);
            }
            if (!home && narrow >= 1.6f && c.Area > 12f && c.AnyWall(c.Sides(true), 1.5f, 0.45f, 0.9f, -1f, out RaidBuilding.Place bench, out _))
                b.WoodChairSeat(c.R, bench, FurnitureBuilder.Oak, "Bench");
            WallDecor(c, home ? 2 : 3);
        }

        private static void Den(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            if (c.AnyWall(order, 1.5f, 0.75f, 0.9f, 0.5f, out RaidBuilding.Place desk, out int ds))
            {
                RaidLayouts.WorkDesk(b, c.R, desk, 1.5f, Woods[c.Rng.Next(Woods.Length)], c.Rng.NextDouble() < 0.2, DeskLampOf(c, 0.8), false, true);
                Vector2 front = c.OnWall(ds, ds == 0 || ds == 2 ? desk.X : desk.Z, 0.75f + 0.45f);
                if (c.Floor(0.5f, 0.5f, RoomCtx.YawOf(ds) + 180f, front, 0.02f, out RaidBuilding.Place chair))
                    b.HouseDeskChair(c.R, chair, Upholstery[c.Rng.Next(Upholstery.Length)]);
            }
            if (c.AnyWall(order, 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place shelf, out _)) b.HomePiece(c.R, shelf, HomeFurnitureKind.Bookshelf);
            if (c.Area > 8f && c.Floor(0.95f, 0.88f, 0f, c.Inner.center, 0.3f, out RaidBuilding.Place arm)) b.HomePiece(c.R, arm, HomeFurnitureKind.Armchair);
            MoreLoot(c, 1, order);
            WallDecor(c, 2);
        }

        private static void Laundry(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            bool home = c.Plan.Kind == RaidBuildingKind.House;
            // A home's washer by the water heater; a block's laundry room has its washers and dryers in a row, a folding table.
            int machines = home ? 1 : Mathf.Clamp(Mathf.FloorToInt(c.Area / 3.5f), 2, 8);
            for (int i = 0; i < machines; i++)
            {
                if (!c.AnyWall(order, 0.68f, 0.65f, 0.9f, 0f, out RaidBuilding.Place w, out _)) break;
                if (home || (i & 1) == 0) b.HouseWasher(c.R, w);
                else b.Furnish(c.R, w, new Vector3(0.68f, 1.06f, 0.65f), true, (m, r) => Dryer(m), "Dryer");
            }
            if (home && c.AnyWall(order, 0.6f, 0.6f, 1.6f, 1f, out RaidBuilding.Place heater, out _)) WaterHeater(c, heater);
            if (!home && c.Area > 12f && c.Floor(1.8f, 0.8f, 0f, c.Inner.center, 0.7f, out RaidBuilding.Place table))
                b.Furnish(c.R, table, new Vector3(1.8f, 0.9f, 0.8f), true, (m, r) =>
                {
                    FurnitureBuilder.Table(m, r, 1.8f, 0.8f, new Color(0.78f, 0.76f, 0.70f));
                    for (int k = 0; k < 3; k++)
                        if (r.NextDouble() < 0.6)
                            m.Box(new Vector3(-0.55f + k * 0.55f, 0.79f, MapStyle.Range(r, -0.1f, 0.1f)), new Vector3(0.36f, 0.06f + 0.04f * r.Next(3), 0.28f),
                                DecorPalette.Matte(Quilts[r.Next(Quilts.Length)]));
                }, "Folding table");
            MoreLoot(c, 1, order);
            Clutter(c, 1);
        }

        // A 1993 dryer (front +Z): the cabinet, its square door with the handle along the top, the control panel at the back.
        private static void Dryer(RaidMesh m)
        {
            Surf enamel = DecorPalette.Gloss(new Color(0.90f, 0.89f, 0.85f)), dark = DecorPalette.Gloss(new Color(0.18f, 0.18f, 0.19f));
            Surf chrome = DecorPalette.Metal(FurnitureBuilder.Chrome);
            m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.68f, 0.9f, 0.62f), enamel);
            m.Box(new Vector3(0f, 0.98f, -0.26f), new Vector3(0.68f, 0.16f, 0.1f), enamel);
            m.Box(new Vector3(0f, 0.98f, -0.205f), new Vector3(0.52f, 0.08f, 0.01f), dark);
            m.Box(new Vector3(0f, 0.47f, 0.315f), new Vector3(0.5f, 0.46f, 0.012f), enamel);
            m.Box(new Vector3(0f, 0.68f, 0.33f), new Vector3(0.3f, 0.025f, 0.02f), chrome);
            m.Box(new Vector3(0f, 0.04f, 0.3f), new Vector3(0.6f, 0.08f, 0.02f), dark);
        }

        private static void Closet(RoomCtx c)
        {
            MoreLoot(c, Mathf.Max(1, LootCount(c, 0.25f, 2)), c.Sides(false));
            Clutter(c, 1);
        }
        #endregion

        #region Garages and sheds
        private static void Garage(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            bool bays = c.P.Role == RoomRole.Bays;
            bool fire = c.Plan.Use == InteriorUse.FireStation;
            // The car on the floor (a family's in its garage, a customer's in the service bay).
            if (!fire && rng.NextDouble() < (bays ? 0.75 : 0.55))
            {
                bool alongX = c.Width >= c.Depth;
                float len = alongX ? c.Width : c.Depth, wide = alongX ? c.Depth : c.Width;
                if (len >= 5.0f && wide >= 2.7f && c.Floor(alongX ? 4.4f : 1.8f, alongX ? 1.8f : 4.4f, 0f, c.Inner.center, 0.25f, out RaidBuilding.Place car))
                    b.GarageCar(c.R, new RaidBuilding.Place(car.X, car.Z, alongX ? (rng.NextDouble() < 0.5 ? 90f : 270f) : (rng.NextDouble() < 0.5 ? 0f : 180f)),
                        CarPaints[rng.Next(CarPaints.Length)]);
            }
            int[] order = c.Sides(false);
            if (fire)
            {
                // Turnout gear in the lockers along the wall, the hose on its rack.
                for (int i = 0; i < 2; i++)
                    if (c.AnyWall(order, 1.5f, 0.5f, 1.9f, -1f, out RaidBuilding.Place lockers, out _))
                        b.Lockers(c.R, lockers, new Vector3(1.5f, 1.9f, 0.5f), 3, new Color(0.62f, 0.14f, 0.10f), ContainerType.Locker, "Gear locker");
                if (c.AnyWall(order, 1.2f, 0.4f, 1.6f, -1f, out RaidBuilding.Place rack, out _))
                    b.Furnish(c.R, rack, new Vector3(1.2f, 1.6f, 0.4f), true, HoseRack, "hose rack");
            }
            LootPiece(c, ContainerType.ToolBench, order);
            MoreLoot(c, LootCount(c, 0.05f, 3), order);
            if (c.AnyWall(order, 0.7f, 0.7f, 1.0f, -1f, out RaidBuilding.Place tyres, out _))
                b.LookPiece(c.R, tyres, new Vector3(0.7f, 1.0f, 0.7f), true, (m, r) => FurnitureBuilder.TyreStack(m, r, Vector3.zero, 2 + r.Next(3)),
                    "Stack of tyres", new Vector3(0f, 0.9f, 0.2f), "Check the tyres", TyreLines);
            if (c.AnyWall(order, 0.4f, 0.3f, 0.5f, -1f, out RaidBuilding.Place can, out _))
                b.LookPiece(c.R, can, new Vector3(0.4f, 0.5f, 0.3f), false, (m, r) => FurnitureBuilder.JerryCan(m, r, Vector3.zero, 0f),
                    "Jerry can", new Vector3(0f, 0.5f, 0f), "Heft the jerry can", JerryLines);
            Clutter(c, 2);
        }

        private static void Shed(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            if (c.Area >= 7f) LootPiece(c, ContainerType.ToolBench, order);
            MoreLoot(c, LootCount(c, 0.08f, 2), order);
            if (c.AnyWall(order, 0.4f, 0.3f, 0.5f, -1f, out RaidBuilding.Place can, out _))
                b.LookPiece(c.R, can, new Vector3(0.4f, 0.5f, 0.3f), false, (m, r) => FurnitureBuilder.JerryCan(m, r, Vector3.zero, 0f),
                    "Jerry can", new Vector3(0f, 0.5f, 0f), "Heft the jerry can", JerryLines);
            Clutter(c, c.Area > 10f ? 3 : 2);
        }

        private static readonly Color[] CarPaints =
        {
            new Color(0.45f, 0.12f, 0.10f), new Color(0.20f, 0.28f, 0.42f), new Color(0.58f, 0.52f, 0.38f), new Color(0.26f, 0.30f, 0.24f),
            new Color(0.70f, 0.70f, 0.68f), new Color(0.14f, 0.14f, 0.15f),
        };

        private static readonly string[] TyreLines =
        {
            "Bald as eggs, the lot of them. Somebody was saving them for a bonfire.",
            "Four worn radials. One's still got a roofing nail in the tread.",
            "Snow tires, the studs half worn down. Somebody meant to put these on before the roads closed.",
        };

        private static readonly string[] JerryLines =
        {
            "Empty. It booms hollow when you knock on it \u2014 somebody beat you to it.",
            "You unscrew the cap and sniff: fumes and nothing else. Not a cupful left in it.",
        };

        // A wall rack of folded canvas hose (the piece's frame, front +Z).
        private static void HoseRack(RaidMesh m, System.Random r)
        {
            Surf steel = DecorPalette.Metal(new Color(0.40f, 0.42f, 0.44f));
            Surf hose = DecorPalette.Matte(new Color(0.78f, 0.74f, 0.62f));
            m.Box(new Vector3(0f, 0.8f, -0.15f), new Vector3(1.2f, 1.6f, 0.04f), steel);
            for (int k = 0; k < 6; k++)
                m.Box(new Vector3(0f, 0.35f + k * 0.18f, 0.02f), new Vector3(1.1f, 0.14f, 0.3f), Quaternion.Euler(0f, 0f, (k % 2 == 0 ? 1f : -1f) * 2f), hose);
        }
        #endregion

        #region Fittings the raid town's layouts keep to themselves
        // A toilet: the home's working one in play; in edit mode (the harness) its fixture would copy its materials (Unity
        // refuses renderer.material there), so it's drawn plain.
        private static void Toilet(RoomCtx c, RaidBuilding.Place p)
        {
            if (Application.isPlaying) c.B.HomePiece(c.R, p, HomeFurnitureKind.Toilet);
            else c.B.Furnish(c.R, p, new Vector3(0.42f, 0.8f, 0.70f), true, (m, r) => FurnitureBuilder.Toilet(m, r), "Toilet");
        }

        // A towel rail on a bathroom's partition (in play: its towel's fixture copies materials, which edit mode refuses).
        private static void TowelRail(RoomCtx c, int side, float t)
        {
            if (Application.isPlaying) c.B.HangTowelRail(c.R, RoomCtx.Char(side), t, 1.42f);
        }

        // The kitchen sink: the starter home's working one in play. In edit mode (the harness) nothing wakes, and the live
        // sink, its config unread, would set its water at NaN: there its unit is drawn plain.
        private static void Sink(RoomCtx c, RaidBuilding.Place p)
        {
            if (Application.isPlaying)
            {
                c.B.HouseSink(c.R, p);
                return;
            }
            c.B.Furnish(c.R, p, new Vector3(1.22f, 1.0f, 0.63f), true, (m, r) => FurnitureBuilder.SinkUnit(m, r, FurnitureBuilder.HomeKitchen), "Kitchen sink");
        }

        // A home's piece with a lamp (a standard lamp, a bedside table) while the building's lamp budget lasts, its lamp
        // left on in a room whose lights were; past the budget a bedside table comes plain (its lamp dark for good) and a
        // standard lamp not at all.
        private static void LampPiece(RoomCtx c, RaidBuilding.Place p, HomeFurnitureKind kind)
        {
            if (c.LampLeft)
            {
                c.Build.Lamps++;
                c.B.LeaveLampOn(c.B.HomePiece(c.R, p, kind), c.P.Lit ? 0.5 : 0.04);
                return;
            }
            if (kind != HomeFurnitureKind.Nightstand) return;
            Color wood = Woods[c.Rng.Next(Woods.Length)];
            c.B.Furnish(c.R, p, new Vector3(0.45f, 0.62f, 0.40f), true, (m, r) => FurnitureBuilder.Nightstand(m, r, wood, false), "Nightstand");
        }

        // A desk lamp's setting for RaidLayouts.WorkDesk (0 none, 1 off, 2 on), within the building's lamp budget.
        private static int DeskLampOf(RoomCtx c, double chance)
        {
            if (!c.LampLeft || c.Rng.NextDouble() >= chance) return 0;
            c.Build.Lamps++;
            return c.P.Lit && c.Rng.NextDouble() < 0.5 ? 2 : 1;
        }

        // The office's bottled-water cooler (drink, fill a bottle).
        private static void WaterCooler(RoomCtx c, RaidBuilding.Place p)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.32f, 1.4f, 0.32f), true, (m, r) => FurnitureBuilder.OfficeWaterCoolerBody(m, r), "Water cooler");
            KitchenFrame f = b.FrameOf(p);
            MeshRenderer water = KitchenParts.PieceAt(t, "JugWater", f, new Vector3(0f, FurnitureBuilder.OfficeJugBase + 0.012f, 0f), FurnitureBuilder.OfficeJugWater);
            RaidStoreWaterCooler.Create(t, f.P(FurnitureBuilder.OfficeCoolerTaps), water, b.Rng.Next());
        }

        // A gas water heater (drain the tank, check the pilot).
        private static void WaterHeater(RoomCtx c, RaidBuilding.Place p)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.6f, 1.6f, 0.6f), true, (m, r) => FurnitureBuilder.OfficeWaterHeater(m, r), "Water heater");
            KitchenFrame f = b.FrameOf(p);
            MeshRenderer pilot = KitchenParts.PieceAt(t, "PilotFlame", f, FurnitureBuilder.OfficeHeaterPilot + new Vector3(0f, 0f, 0.003f),
                FurnitureBuilder.OfficeHeaterPilotGlow);
            RaidStoreWaterHeater.Create(t, f.P(FurnitureBuilder.OfficeHeaterValve), f.P(0f, 0.55f, 0.36f), pilot, b.Rng.Next());
        }
        #endregion
    }
}
