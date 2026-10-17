using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        #region County office (photo 3)
        /// <summary>The county office: an open-plan floor of desks with CRTs and lamps (photo 3), a lobby
        /// with reception, a break room, the manager's office, records and a utility room. 20 × 14 m.</summary>
        public static RaidBuilding Office(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "CountyOffice", center, turns, false, 20f, 14f)
            {
                Exterior = ExteriorStyle.Brick,
                ExteriorColor = new Color(0.56f, 0.46f, 0.36f),
                DoorWood = new Color(0.46f, 0.32f, 0.20f),
                Casing = new Color(0.62f, 0.60f, 0.54f),
            };
            System.Random rng = b.Rng;
            RaidRoom open = b.Room("Open Office", -10f, -7f, 3f, 3f, OfficeBlue, FloorKind.LinoBeige);
            open.Lit = true;
            RaidRoom lobby = b.Room("Lobby", 3f, -7f, 10f, -1f, TealBeige, FloorKind.LinoGrey);
            lobby.Lit = Chance(rng, 0.6);
            RaidRoom brk = b.Room("Break Room", 3f, -1f, 10f, 3f, KitchenMint, FloorKind.Checker);
            brk.Lit = Chance(rng, 0.55);
            brk.Flicker = Chance(rng, 0.5);
            RaidRoom mgr = b.Room("Manager", -10f, 3f, -3f, 7f, BrownGreen, FloorKind.Carpet);
            mgr.FloorTint = new Color(0.24f, 0.30f, 0.22f);
            mgr.Light = LightStyle.Dome;
            RaidRoom rec = b.Room("Records", -3f, 3f, 3f, 7f, OfficeBlue, FloorKind.LinoGrey);
            RaidRoom util = b.Room("Utility", 3f, 3f, 10f, 7f, GarageGrey, FloorKind.Concrete);
            util.Light = LightStyle.Bulb;

            b.Door(6.5f, -7f, 1.8f, true, true);
            b.Door(3f, -4.0f, 1.5f);
            b.Door(5.0f, -1f, 1.4f);
            b.Door(3f, 1.0f, 1.4f);
            b.Door(-5.5f, 3f, 1.4f);
            b.Door(0.6f, 3f, 1.4f);
            b.Door(8.3f, 3f, 1.4f);
            b.Door(6.0f, 7f, 1.5f);
            b.Door(-10f, -3.0f, 1.4f);
            b.Window(-8.0f, -7f); b.Window(-4.5f, -7f); b.Window(-1.0f, -7f); b.Window(8.8f, -7f);
            b.Window(-10f, 0.8f); b.Window(-10f, 5.0f);
            b.Window(-6.5f, 7f); b.Window(0f, 7f);
            b.Window(10f, -4f); b.Window(10f, 1.0f);
            b.BuildShell();

            // --- Open office: the photo-3 corner on the north wall ---
            b.Container(open, ContainerType.Desk, "Office Desk", b.N(open, -2.4f, 0.75f), Sz(1.5f, 0.9f, 0.75f),
                (m, r) => OfficeDesk(b, m, r, 1.5f, FurnitureBuilder.Oak, true, 2, true));
            b.Furnish(open, b.N(open, -3.75f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.16f, 0.26f, 0.42f)), "barrel");
            b.Furnish(open, b.N(open, -0.95f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.45f, 0.24f, 0.10f)), "barrel");
            b.Furnish(open, new RaidBuilding.Place(-2.4f, 1.55f, 0f), Sz(0.45f, 0.7f, 0.45f), false, (m, r) => FurnitureBuilder.Stool(m, r, new Color(0.16f, 0.16f, 0.18f)), "stool");
            b.Furnish(open, new RaidBuilding.Place(-0.9f, 0.9f, 20f), Sz(0.5f, 0.4f, 0.4f), false, (m, r) => FurnitureBuilder.CardboardBox(m, r, Vector3.zero, Sz(0.5f, 0.38f, 0.4f), 0f), "box");
            b.RugAt(-2.4f, 0.3f, 2.4f, 1.6f, 0f, 1);
            b.Poster(open, 'N', -2.4f, 1.75f, 0.45f, 0.65f, 1);
            b.Clock(open, 'N', -7.0f, 2.2f);

            // A pod of four desks, back to back.
            b.Container(open, ContainerType.Desk, "Office Desk", new RaidBuilding.Place(-7.25f, -2.375f, 180f), Sz(1.5f, 0.9f, 0.75f),
                (m, r) => OfficeDesk(b, m, r, 1.5f, Putty, false, 0, false));
            b.Furnish(open, new RaidBuilding.Place(-5.75f, -2.375f, 180f), Sz(1.5f, 0.9f, 0.75f), true,
                (m, r) => OfficeDesk(b, m, r, 1.5f, Putty, false, 1, false), "desk");
            b.Furnish(open, new RaidBuilding.Place(-7.25f, -1.625f, 0f), Sz(1.5f, 0.9f, 0.75f), true,
                (m, r) => OfficeDesk(b, m, r, 1.5f, Putty, Chance(r, 0.3), 0, false), "desk");
            b.Container(open, ContainerType.Desk, "Office Desk", new RaidBuilding.Place(-5.75f, -1.625f, 0f), Sz(1.5f, 0.9f, 0.75f),
                (m, r) => OfficeDesk(b, m, r, 1.5f, Putty, true, 2, false));
            Color chair = new Color(0.20f, 0.24f, 0.32f);
            b.Furnish(open, new RaidBuilding.Place(-7.25f, -3.2f, 0f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, chair), "chair");
            b.Furnish(open, new RaidBuilding.Place(-5.7f, -3.3f, 18f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, chair), "chair");
            b.Furnish(open, new RaidBuilding.Place(-7.3f, -0.8f, 180f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, chair), "chair");
            b.Furnish(open, new RaidBuilding.Place(-5.75f, -0.75f, 205f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, chair), "chair");

            b.Furnish(open, b.Wst(open, 2.05f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "filing cabinet");
            b.Furnish(open, b.Wst(open, 2.53f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "filing cabinet");
            b.Furnish(open, b.S(open, -1.0f, 0.75f), Sz(1.5f, 0.9f, 0.75f), true, (m, r) => OfficeDesk(b, m, r, 1.5f, FurnitureBuilder.Oak, false, 1, false), "desk");
            b.Furnish(open, new RaidBuilding.Place(-1.0f, -5.7f, 180f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, chair), "chair");
            b.Furnish(open, b.E(open, -1.5f, 0.6f), Sz(0.9f, 1.1f, 0.6f), true, (m, r) => FurnitureBuilder.Copier(m, r), "copier");
            b.Furnish(open, b.E(open, -6.0f, 0.32f), Sz(0.32f, 1.4f, 0.32f), true, (m, r) => FurnitureBuilder.WaterCooler(m, r), "water cooler");
            b.Furnish(open, new RaidBuilding.Place(-9.4f, -6.4f, 0f), Sz(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            b.CeilingLight(open, -6.5f, -2.0f);
            b.CeilingLight(open, -2.4f, -1.0f);
            b.CeilingLight(open, 0.8f, -4.5f);
            b.SpawnPoint(open, -2.4f, -1.0f, 0f);
            b.Litter(open, 0.9f);

            // --- Lobby ---
            b.Furnish(lobby, new RaidBuilding.Place(8.2f, -3.3f, 180f), Sz(2.0f, 1.1f, 0.75f), true,
                (m, r) => OfficeDesk(b, m, r, 2.0f, FurnitureBuilder.Walnut, false, 0, false), "reception desk");
            b.Furnish(lobby, b.Wst(lobby, -6.3f, 0.45f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Walnut), "chair");
            b.Furnish(lobby, b.Wst(lobby, -5.75f, 0.45f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Walnut), "chair");
            b.Furnish(lobby, new RaidBuilding.Place(9.4f, -6.4f, 0f), Sz(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            b.Poster(lobby, 'E', -1.9f, 1.5f, 0.45f, 0.65f, 3);
            b.CeilingLight(lobby, 6.5f, -4.0f);
            b.Litter(lobby, 0.7f);

            // --- Break room ---
            Color white = new Color(0.82f, 0.82f, 0.78f);
            b.Furnish(brk, b.N(brk, 5.6f, 0.62f), Sz(2.0f, 1.1f, 0.62f), true,
                (m, r) => FurnitureBuilder.KitchenCounter(m, r, 2.0f, white, new Color(0.62f, 0.58f, 0.48f), true), "counter");
            b.UpperCabinets(brk, 'N', 5.6f, 2.0f, white);
            b.Container(brk, ContainerType.Fridge, "Fridge", b.E(brk, -0.3f, 0.72f), Sz(0.76f, 1.75f, 0.72f),
                (m, r) => FurnitureBuilder.Fridge(m, r, new Color(0.84f, 0.84f, 0.80f)));
            bool power = brk.Lit;
            b.Furnish(brk, b.S(brk, 8.0f, 0.8f), Sz(0.9f, 1.85f, 0.8f), true, (m, r) => FurnitureBuilder.VendingMachine(m, r, power), "vending machine");
            b.CeilingLight(brk, 6.5f, 1.0f);
            b.Litter(brk, 0.8f);

            // --- Manager ---
            b.Container(mgr, ContainerType.Desk, "Manager's Desk", new RaidBuilding.Place(-8.0f, 5.0f, 270f), Sz(1.8f, 0.9f, 0.85f), (m, r) =>
            {
                FurnitureBuilder.Desk(m, r, 1.8f, 0.85f, FurnitureBuilder.Walnut, true);
                bool lit = Chance(r, 0.5);
                Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, new Vector3(-0.6f, 0.76f, -0.2f), 20f, GreenShade, lit);
                if (lit) b.LampLight(RaidBuilding.WorldOf(m, bulb), false);
                FurnitureBuilder.CrtMonitor(m, r, new Vector3(0.45f, 0.76f, -0.12f), -15f, false);
                DeskClutter(m, r, 1.4f);
            });
            b.Furnish(mgr, new RaidBuilding.Place(-8.85f, 5.0f, 90f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, new Color(0.18f, 0.10f, 0.08f)), "chair");
            b.Furnish(mgr, new RaidBuilding.Place(-6.9f, 4.5f, 270f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Walnut), "chair");
            b.Furnish(mgr, new RaidBuilding.Place(-6.9f, 5.55f, 255f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Walnut), "chair");
            b.Furnish(mgr, b.E(mgr, 5.5f, 0.32f), Sz(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, FurnitureBuilder.Walnut), "bookcase");
            b.Furnish(mgr, b.N(mgr, -4.5f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "filing cabinet");
            b.RugAt(-7.6f, 5.0f, 2.2f, 1.6f, 90f, 0);
            b.Poster(mgr, 'E', 4.0f, 1.6f, 0.5f, 0.7f, 2);
            b.CeilingLight(mgr, -6.5f, 5.0f);
            b.Litter(mgr, 0.6f);

            // --- Records ---
            b.Container(rec, ContainerType.Shelf, "Records Shelf", b.Wst(rec, 5.0f, 0.5f), Sz(1.4f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 5, 1, new Color(0.40f, 0.42f, 0.40f)));
            b.Furnish(rec, b.E(rec, 5.0f, 0.5f), Sz(1.4f, 1.9f, 0.5f), true,
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 5, 1, new Color(0.40f, 0.42f, 0.40f)), "shelf");
            b.Furnish(rec, b.N(rec, -1.6f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "filing cabinet");
            b.Furnish(rec, b.N(rec, 1.6f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "filing cabinet");
            b.Clutter(rec, 3, false);
            b.CeilingLight(rec, 0f, 5f);
            b.Litter(rec, 1.2f);

            // --- Utility ---
            b.Furnish(util, new RaidBuilding.Place(9.4f, 4.8f, 0f), Sz(0.6f, 1.6f, 0.6f), true, (m, r) =>
            {
                m.Cylinder(Vector3.zero, 0.28f, 1.5f, 12, DecorPalette.Gloss(new Color(0.78f, 0.76f, 0.70f)));
                m.Cylinder(new Vector3(0.1f, 1.5f, 0f), 0.03f, 1.2f, 6, DecorPalette.Metal(new Color(0.5f, 0.35f, 0.2f)));
                m.Cylinder(new Vector3(-0.1f, 1.5f, 0f), 0.03f, 1.2f, 6, DecorPalette.Metal(new Color(0.5f, 0.35f, 0.2f)));
            }, "water heater");
            b.Container(util, ContainerType.Shelf, "Supply Shelf", b.N(util, 8.4f, 0.5f), Sz(1.4f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 4, 3, new Color(0.30f, 0.32f, 0.34f)));
            b.Furnish(util, b.Wst(util, 5.5f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.12f, 0.12f, 0.13f)), "barrel");
            b.Furnish(util, b.Wst(util, 6.15f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.16f, 0.26f, 0.42f)), "barrel");
            b.Clutter(util, 2, false);
            b.CeilingLight(util, 6.5f, 5.0f);
            b.Litter(util, 0.6f);

            b.DoorSign(6.5f, -7f, "COUNTY OFFICE", new Color(0.12f, 0.18f, 0.32f), new Color(0.88f, 0.86f, 0.80f));
            b.Finish();
            return b;
        }
        #endregion
    }
}
