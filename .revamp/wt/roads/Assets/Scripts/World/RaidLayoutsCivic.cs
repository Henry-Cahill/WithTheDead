using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        #region Clinic
        /// <summary>The town clinic: waiting room with reception, an exam room, the doctor's office, a
        /// three-bed ward and the pharmacy (medicine cabinets, a medical fridge). 18 × 12 m. Everything in it works: the
        /// reception and doctor's desks and their computers, the waiting-room chairs, the examination couch and the ward's
        /// beds to lie on, the scrub sink, the medicine cabinets, the medical fridge, the wheelchair, the privacy screens, the
        /// drip stands and the doctor's bookcase.</summary>
        public static RaidBuilding Clinic(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "Clinic", center, turns, false, 18f, 12f)
            {
                Kind = RaidBuildingKind.Clinic,
                Exterior = ExteriorStyle.Stucco,
                ExteriorColor = new Color(0.74f, 0.74f, 0.70f),
                DoorWood = new Color(0.62f, 0.60f, 0.56f),
                Casing = new Color(0.66f, 0.66f, 0.62f),
            };
            System.Random rng = b.Rng;
            RaidRoom waiting = b.Room("Waiting Room", -9f, -6f, -2f, 0f, ClinicGreen, FloorKind.LinoGreen);
            waiting.Lit = Chance(rng, 0.7);
            RaidRoom exam = b.Room("Exam Room", -2f, -6f, 4f, 0f, ClinicGreen, FloorKind.LinoGreen);
            exam.Lit = Chance(rng, 0.6);
            RaidRoom office = b.Room("Doctor's Office", 4f, -6f, 9f, 0f, OfficeBlue, FloorKind.LinoBeige);
            office.Lit = Chance(rng, 0.5);
            RaidRoom ward = b.Room("Ward", -9f, 0f, 2f, 6f, ClinicGreen, FloorKind.LinoGreen);
            ward.Lit = Chance(rng, 0.6);
            ward.Flicker = true;
            RaidRoom pharm = b.Room("Pharmacy", 2f, 0f, 9f, 6f, ClinicGreen, FloorKind.LinoGrey);
            if (Chance(rng, 0.5)) b.Overgrown.Add(ward);

            b.Door(-5.5f, -6f, 1.6f);
            b.Door(-2f, -3.0f, 1.4f);
            b.Door(-5.5f, 0f, 1.6f);
            b.Door(0.5f, 0f, 1.4f);
            b.Door(4f, -3.0f, 1.4f);
            b.Door(2f, 3.0f, 1.4f);
            b.Door(6.5f, 0f, 1.4f);
            b.Door(5.5f, 6f, 1.5f);
            b.Window(-8.0f, -6f); b.Window(-3.2f, -6f); b.Window(1.0f, -6f); b.Window(6.5f, -6f);
            b.Window(-9f, -3f); b.Window(-9f, 3f);
            b.Window(-6f, 6f); b.Window(-2f, 6f);
            b.Window(9f, -3f); b.Window(9f, 3f);
            b.BuildShell();

            // Waiting room.
            WorkDesk(b, waiting, b.N(waiting, -3.3f, 0.75f), 1.6f, FurnitureBuilder.Oak, false, 0, false, false, "Reception desk");
            Color waitingChair = new Color(0.30f, 0.40f, 0.42f);
            for (int i = 0; i < 3; i++)
                b.WoodChairSeat(waiting, b.Wst(waiting, -4.2f + i * 0.6f, 0.45f), waitingChair, "Waiting-room chair");
            b.WoodChairSeat(waiting, b.S(waiting, -8.3f, 0.45f), waitingChair, "Waiting-room chair");
            b.WoodChairSeat(waiting, b.S(waiting, -7.7f, 0.45f), waitingChair, "Waiting-room chair");
            b.Furnish(waiting, new RaidBuilding.Place(-8.4f, -0.6f, 0f), Sz(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            b.Poster(waiting, 'E', -1.0f, 1.5f, 0.45f, 0.65f, 3);
            b.CeilingLight(waiting, -5.5f, -3.0f);
            b.Litter(waiting, 0.8f);

            // Exam room.
            b.ExamCouch(exam, new RaidBuilding.Place(1.0f, -4.6f, 90f));
            b.MedicineCabinet(exam, b.N(exam, 2.6f, 0.42f), ContainerType.MedCabinet, "Medicine Cabinet");
            b.SteelCounter(exam, b.S(exam, 3.0f, 0.65f), 1.4f, 0f, ContainerType.Shelf, "Sink counter", null, true);
            b.StoolSeat(exam, new RaidBuilding.Place(1.0f, -3.5f, 0f), new Color(0.20f, 0.30f, 0.30f));
            b.IvStand(exam, new RaidBuilding.Place(2.3f, -3.9f, 0f));
            b.CeilingLight(exam, 1.0f, -3.0f);
            b.Litter(exam, 0.7f);

            // Doctor's office.
            WorkDesk(b, office, b.E(office, -3.0f, 0.75f), 1.6f, FurnitureBuilder.Walnut, false, Chance(rng, 0.5) ? 2 : 1, false, true, "Doctor's Desk");
            b.OfficeChairSeat(office, new RaidBuilding.Place(7.65f, -3.0f, 90f), new Color(0.14f, 0.16f, 0.20f));
            b.Lockers(office, b.S(office, 5.0f, 0.5f), Sz(1.0f, 1.9f, 0.5f), 2, new Color(0.55f, 0.58f, 0.60f), ContainerType.Locker, "Locker");
            b.FilingCabinet(office, b.S(office, 8.4f, 0.62f), 4, Putty);
            b.ReadingShelf(office, b.N(office, 8.2f, 0.32f), FurnitureBuilder.Oak, 2, CivicReading.Shelf.Clinic);
            b.CeilingLight(office, 6.5f, -3.0f);
            b.Litter(office, 0.6f);

            // Ward.
            for (int i = 0; i < 3; i++)
            {
                float x = -7.6f + i * 3f;
                b.WardBed(ward, b.N(ward, x, 2.0f));
                b.IvStand(ward, new RaidBuilding.Place(x + 0.7f, 3.4f, 0f));
            }
            b.PrivacyScreen(ward, new RaidBuilding.Place(-6.1f, 4.8f, 90f));
            b.PrivacyScreen(ward, new RaidBuilding.Place(-3.1f, 4.8f, 90f));
            b.MedicineCabinet(ward, b.Wst(ward, 1.7f, 0.42f), ContainerType.MedCabinet, "Medicine Cabinet");
            b.WheelchairSeat(ward, new RaidBuilding.Place(-0.5f, 2.0f, 30f));
            b.CeilingLight(ward, -6.0f, 2.5f);
            b.CeilingLight(ward, -1.5f, 2.5f);
            b.Litter(ward, 0.7f);

            // Pharmacy.
            b.Shelving(pharm, b.E(pharm, 4.5f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 5, 2, new Color(0.80f, 0.80f, 0.78f), "Shelf",
                RaidBuilding.SpareStock(ContainerType.MedCabinet, "shelf"));
            b.MedicineCabinet(pharm, b.N(pharm, 3.2f, 0.42f), ContainerType.MedCabinet, "Medicine Cabinet");
            b.MedicineCabinet(pharm, b.N(pharm, 7.9f, 0.42f), ContainerType.MedCabinet, "Medicine Cabinet");
            b.WorkFridge(pharm, b.E(pharm, 1.0f, 0.72f), new Color(0.86f, 0.86f, 0.84f), ContainerType.Fridge, "Medical Fridge", true);
            StorageOptions boxes = RaidBuilding.SpareStock(ContainerType.MedCabinet, "boxes");
            boxes.ContainerName = "Boxes on the table";
            boxes.LookLabel = "Go through the boxes";
            b.WorkTable(pharm, new RaidBuilding.Place(5.5f, 2.8f, 0f), Sz(1.6f, 0.9f, 0.8f), (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.6f, 0.8f, new Color(0.70f, 0.70f, 0.68f));
                for (int i = 0; i < 3; i++) FurnitureBuilder.CardboardBox(m, r, new Vector3(R(r, -0.6f, 0.6f), 0.755f, R(r, -0.2f, 0.2f)), Sz(0.28f, 0.18f, 0.22f), R(r, 0f, 90f));
            }, "Work table", boxes);
            b.Clutter(pharm, 2, false);
            b.CeilingLight(pharm, 5.5f, 3.0f);
            b.Litter(pharm, 0.8f);

            b.DoorSign(-5.5f, -6f, "CLINIC", new Color(0.86f, 0.86f, 0.82f), new Color(0.62f, 0.10f, 0.08f));
            b.Finish();
            return b;
        }
        #endregion

        #region Warehouse / garage
        private static readonly string[] DepotTyreLines =
        {
            "Bald as eggs, the lot of them. Somebody was saving them for a bonfire.",
            "Four worn radials. One's still got a roofing nail in the tread.",
            "You tip the top one aside. Nothing behind the stack but a spider that's given up on the winter.",
        };

        private static readonly string[] DepotJerryLines =
        {
            "Empty. It booms hollow when you knock on it \u2014 somebody beat you to it.",
            "You unscrew the cap and sniff: fumes and nothing else. Not a cupful left in it.",
        };

        /// <summary>The depot: a racked warehouse bay, a repair garage with a car on the floor and tool
        /// benches, and a site office. 22 × 16 m, cinder block. Everything in it works: every bay of racking can be searched,
        /// the benches have drawers, a pegboard and a vice, the car's trunk and glovebox open, and in the site office the desk,
        /// the CB set, the lockers, the sofa and the filing cabinet.</summary>
        public static RaidBuilding Warehouse(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "Depot", center, turns, false, 22f, 16f)
            {
                Kind = RaidBuildingKind.Depot,
                Exterior = ExteriorStyle.CinderBlock,
                ExteriorColor = new Color(0.56f, 0.56f, 0.53f),
                DoorWood = new Color(0.30f, 0.36f, 0.40f),
                Casing = new Color(0.32f, 0.34f, 0.36f),
                WallCore = new Color(0.30f, 0.30f, 0.29f),
            };
            System.Random rng = b.Rng;
            RaidRoom bay = b.Room("Warehouse", -11f, -8f, 4f, 8f, GarageGrey, FloorKind.Concrete);
            bay.Light = LightStyle.ShopLamp;
            bay.Lit = Chance(rng, 0.5);
            RaidRoom garage = b.Room("Garage", 4f, -8f, 11f, 2f, GarageGrey, FloorKind.Concrete);
            garage.Light = LightStyle.ShopLamp;
            garage.Lit = Chance(rng, 0.5);
            garage.Flicker = Chance(rng, 0.5);
            RaidRoom office = b.Room("Site Office", 4f, 2f, 11f, 8f, BrownGreen, FloorKind.LinoBeige);
            office.Lit = Chance(rng, 0.5);

            b.Door(-4f, -8f, 3.2f, false);
            b.Door(1.5f, -8f, 1.4f);
            b.Door(7.5f, -8f, 3.0f, false);
            b.Door(4f, -3.0f, 2.0f, false);
            b.Door(4f, 5.0f, 1.4f);
            b.Door(7.5f, 2f, 1.4f);
            b.Door(-6f, 8f, 1.5f);
            b.Window(-2f, 8f); b.Window(7.5f, 8f);
            b.Window(11f, 5.0f); b.Window(11f, -3.5f);
            b.BuildShell();
            b.RollUpDoor(-4f, -8f);
            b.RollUpDoor(7.5f, -8f);

            // Warehouse bay: racking along the west wall and two double-sided islands — every bay of it to search, though the
            // open racking was picked over long ago.
            Color rack = new Color(0.16f, 0.26f, 0.44f);
            Vector3 rackSize = Sz(1.8f, 2.2f, 0.6f);
            float[] wallZ = { -6.0f, -3.9f, -1.8f, 0.3f, 2.4f, 4.5f };
            for (int i = 0; i < wallZ.Length; i++)
            {
                RaidBuilding.Place p = b.Wst(bay, wallZ[i], 0.6f);
                if (i == 1) b.Shelving(bay, p, rackSize, 4, 3, rack, ContainerType.Shelf, "Warehouse Shelf", true);
                else b.Shelving(bay, p, rackSize, 4, 3, rack, "Racking", RaidBuilding.SpareStock(ContainerType.Shelf, "racking"));
            }
            float[] islandX = { -5.0f, 0f };
            float[] islandZ = { -1.8f, 0.2f, 2.2f };
            for (int ix = 0; ix < islandX.Length; ix++)
                for (int iz = 0; iz < islandZ.Length; iz++)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var p = new RaidBuilding.Place(islandX[ix] + s * 0.3f, islandZ[iz], s > 0 ? 90f : 270f);
                        if (ix == 0 && iz == 1 && s > 0) b.Shelving(bay, p, rackSize, 4, 0, rack, ContainerType.Shelf, "Warehouse Shelf", true);
                        else b.Shelving(bay, p, rackSize, 4, ix == 0 ? 3 : 0, rack, "Racking", RaidBuilding.SpareStock(ContainerType.Shelf, "racking"));
                    }
            b.StackedCrate(bay, new RaidBuilding.Place(-9.0f, -7.1f, 0f), Sz(1.1f, 1.4f, 1.1f), Sz(1.05f, 0.8f, 1.05f), Sz(0.8f, 0.55f, 0.75f), 0.05f, 8f,
                false, ContainerType.Crate, "Crate", true);
            b.Furnish(bay, new RaidBuilding.Place(-8.0f, 6.5f, 0f), Sz(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            b.Furnish(bay, new RaidBuilding.Place(-2.5f, 6.5f, 90f), Sz(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            b.Furnish(bay, new RaidBuilding.Place(1.5f, -5.0f, 0f), Sz(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, Chance(r, 0.6)), "pallet");
            Vector3[] oil = { new Vector3(3.3f, 0f, 7.2f), new Vector3(2.65f, 0f, 7.25f), new Vector3(3.3f, 0f, 6.55f) };
            foreach (Vector3 o in oil)
                b.Furnish(bay, new RaidBuilding.Place(o.x, o.z, 0f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.12f, 0.12f, 0.13f)), "barrel");
            b.CeilingLight(bay, -5.0f, -4.5f);
            b.CeilingLight(bay, -2.0f, 4.5f);
            b.Clutter(bay, 3, false);
            b.Litter(bay, 0.35f);

            // Garage: the car (its trunk and its glovebox), the benches, and tyres and a jerry can to look over.
            Color paint = Pick(rng, new[] { new Color(0.45f, 0.12f, 0.10f), new Color(0.20f, 0.28f, 0.42f), new Color(0.58f, 0.52f, 0.38f) });
            b.GarageCar(garage, new RaidBuilding.Place(7.5f, -3.5f, 0f), paint);
            b.ToolBench(garage, b.E(garage, -1.0f, 0.75f), ContainerType.ToolBench, "Tool Bench");
            b.ToolBench(garage, b.E(garage, -6.0f, 0.75f), ContainerType.ToolBench, "Tool Bench");
            b.LookPiece(garage, new RaidBuilding.Place(4.6f, 1.2f, 0f), Sz(0.7f, 1.0f, 0.7f), true, (m, r) => FurnitureBuilder.TyreStack(m, r, Vector3.zero, 4),
                "Stack of tyres", new Vector3(0f, 0.9f, 0.2f), "Check the tyres", DepotTyreLines);
            b.Furnish(garage, new RaidBuilding.Place(4.6f, -6.6f, 0f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.50f, 0.20f, 0.10f)), "barrel");
            b.Furnish(garage, new RaidBuilding.Place(4.6f, -7.3f, 0f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.12f, 0.12f, 0.13f)), "barrel");
            b.LookPiece(garage, new RaidBuilding.Place(5.4f, 0.9f, 20f), Sz(0.4f, 0.5f, 0.3f), false, (m, r) => FurnitureBuilder.JerryCan(m, r, Vector3.zero, 0f),
                "Jerry can", new Vector3(0f, 0.5f, 0f), "Heft the jerry can", DepotJerryLines);
            b.CeilingLight(garage, 7.5f, -3.5f);
            b.Litter(garage, 0.6f);

            // Site office: the foreman's desk with the CB set on it, the lockers, the sofa, the filing cabinet.
            RaidBuilding.Place officeDesk = b.N(office, 7.5f, 0.75f);
            bool deskLamp = office.Lit || Chance(rng, 0.4);
            Transform desk = b.DrawerDesk(office, officeDesk, 1.5f, new Color(0.45f, 0.45f, 0.44f), true, "Desk", (m, r) =>
            {
                b.DeskLamp(m, r, new Vector3(-0.5f, 0.76f, -0.15f), 20f, BlackShade, deskLamp, false);
                DeskClutter(m, r, 1.2f);
            }, true);
            b.FieldRadio(desk, officeDesk, new Vector3(0.35f, 0.76f, -0.1f), -10f, CivicRadio.Band.Citizens);
            b.OfficeChairSeat(office, new RaidBuilding.Place(7.5f, 6.7f, 0f), new Color(0.25f, 0.20f, 0.16f));
            b.Lockers(office, b.E(office, 3.0f, 0.5f), Sz(1.0f, 1.9f, 0.5f), 2, new Color(0.30f, 0.38f, 0.46f), ContainerType.Locker, "Locker");
            b.SofaSeats(office, b.Wst(office, 6.95f, 0.88f), 1.8f, new Color(0.36f, 0.24f, 0.16f));
            b.FilingCabinet(office, b.E(office, 6.6f, 0.62f), 4, new Color(0.42f, 0.44f, 0.40f));
            b.Poster(office, 'S', 9.5f, 1.6f, 0.45f, 0.65f, 1);
            b.CeilingLight(office, 7.5f, 5.0f);
            b.Litter(office, 0.6f);

            b.DoorSign(1.5f, -8f, "DEPOT AUTO", new Color(0.70f, 0.56f, 0.18f), new Color(0.10f, 0.10f, 0.10f));
            b.Finish();
            return b;
        }
        #endregion
    }
}
