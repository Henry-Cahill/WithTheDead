using UnityEngine;

namespace CloneZ
{
    // The kitchen's loose furniture (see the per-area hooks in HomeFurniture.cs): the counter along the west wall — a
    // cupboard behind two doors and a pair of drawers under the worktop, each a storage fixture — and the fridge-freezer,
    // whose doors, compartments, lamp and cooling are its KitchenFridge. The sink, the gas stove and the wall cupboards
    // are fitted elsewhere (KitchenSink, KitchenStove, StarterHome.Kitchen.cs).
    public partial class HomeFurniture
    {
        private const float KitchenCounterDoorAngle = 95f;   // the end door just clears the dining chair
        private static readonly Color KitchenFridgeEnamel = new Color(0.88f, 0.87f, 0.83f);

        private Color _kitchenEnamel;   // the fridge's paint, picked while its cabinet is drawn and used again for its doors

        partial void DrawKitchen(RaidMesh m, System.Random rng, ref bool drawn)
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Counter:
                    FurnitureBuilder.KitchenCounterBody(m, rng, FurnitureBuilder.HomeKitchen);
                    drawn = true;
                    break;
                case HomeFurnitureKind.Fridge:
                    _kitchenEnamel = DecorPalette.Vary(KitchenFridgeEnamel, 0.03f, rng);
                    FurnitureBuilder.FridgeCabinet(m, _kitchenEnamel);
                    drawn = true;
                    break;
            }
        }

        partial void AttachKitchen()
        {
            if (Kind == HomeFurnitureKind.Counter) AttachKitchenCounter();
            else if (Kind == HomeFurnitureKind.Fridge) KitchenFridge.Build(this, _kitchenEnamel);
        }

        // The counter's cupboard and drawers. Both doors hang on their left edges — the left one at the counter's end, the
        // right one at the middle, so in the corner it swings clear of the sink's doors. The cutlery drawer is the one by
        // the sink; the far one collects odds and ends.
        private void AttachKitchenCounter()
        {
            Transform root = transform;
            Vector3 at = root.position;
            var f = new KitchenFrame(at, Yaw);
            FurnitureBuilder.KitchenTone tone = FurnitureBuilder.HomeKitchen;
            const float h = FurnitureBuilder.CounterLength * 0.5f, front = FurnitureBuilder.CounterFront;
            const float y0 = FurnitureBuilder.CounterDoorY0, y1 = FurnitureBuilder.CounterDoorY1;

            FixturePart left = KitchenParts.Door(root, "CupboardDoorL", f, d => FurnitureBuilder.CounterDoor(d, tone, true),
                new Vector3(-h + 0.002f, y0, front), Vector3.right, Vector3.forward, KitchenCounterDoorAngle);
            FixturePart right = KitchenParts.Door(root, "CupboardDoorR", f, d => FurnitureBuilder.CounterDoor(d, tone, false),
                new Vector3(0.003f, y0, front), Vector3.right, Vector3.forward, KitchenCounterDoorAngle);
            StorageFixture.Create(root, "Kitchen cupboard", Interactable.KeyFor("countercupboard", at), f.P(0f, (y0 + y1) * 0.5f, front + 0.01f),
                new[] { left, right }, root, Seed * 17 + 1, new StorageOptions
                {
                    LootType = ContainerType.Pantry,
                    EmptyChance = CloneZConfig.CounterCupboardEmptyChance,
                    MaxStacks = CloneZConfig.CounterCupboardMaxStacks,
                    ExtraActions = (ctx, list) => list.Add(new InteractAction("Wipe down the worktop", KitchenWipeWorktop)
                        .Timed(CloneZConfig.KitchenWipeSeconds, "Wiping down the worktop")),
                });

            const float dy = (FurnitureBuilder.CounterDrawerY0 + FurnitureBuilder.CounterDrawerY1) * 0.5f;
            var travel = new Vector3(0f, 0f, FurnitureBuilder.CounterDrawerTravel);
            FixturePart cutlery = KitchenParts.Drawer(root, "CutleryDrawer", f, d => FurnitureBuilder.CounterDrawer(d, tone, false, true),
                new Vector3(h * 0.5f, dy, front), travel);
            FixturePart odds = KitchenParts.Drawer(root, "KitchenDrawer", f, d => FurnitureBuilder.CounterDrawer(d, tone, true, false),
                new Vector3(-h * 0.5f, dy, front), travel);
            StorageFixture.Create(root, "Cutlery drawer", Interactable.KeyFor("counterdrawer", at), f.P(h * 0.5f, dy, front + 0.02f),
                new[] { cutlery }, root, Seed * 17 + 2, KitchenDrawerOptions());
            StorageFixture.Create(root, "Kitchen drawer", Interactable.KeyFor("counterdrawer", at, 1), f.P(-h * 0.5f, dy, front + 0.02f),
                new[] { odds }, root, Seed * 17 + 3, KitchenDrawerOptions());
        }

        private static StorageOptions KitchenDrawerOptions() => new StorageOptions
        {
            LootType = ContainerType.Desk,
            EmptyChance = CloneZConfig.CounterDrawerEmptyChance,
            MaxStacks = CloneZConfig.CounterDrawerMaxStacks,
            OpenVerb = "Pull out",
            CloseVerb = "Push in",
        };

        private static void KitchenWipeWorktop(InteractContext ctx)
            => ctx.Notify("You sweep the crumbs off the worktop into your palm and brush them into the sink. Small things.");
    }
}
