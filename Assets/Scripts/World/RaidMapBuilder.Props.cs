using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public static partial class RaidMapBuilder
    {
        #region Interactive outdoor props
        // The town's outdoor props made usable, the way the starter home's yard is: dumpsters with a lid to lift and rubbish to
        // go through, wrecks with a glovebox and a trunk (and a horn), the forecourt's pumps, the quarantine site's generator,
        // the mail drop boxes and hydrants, and the checkpoint's guard booth with its desk and radio. Each prop's static mesh
        // stays in the outdoor batches (and so in the navmesh bake) with its collider among the prop colliders; what's
        // interactive hangs on a renderer-less object of its own under the town's props root — its moving parts (kept out of
        // the bake) and its fixtures. Their contents are seeded from the prop's place rather than from the town's random stream.

        private const string TownPropsName = "TownProps";

        /// <summary>The object every interactive outdoor prop hangs under (made on first use).</summary>
        private static Transform TownPropRoot(RaidTown town)
        {
            Transform root = town.World.Find(TownPropsName);
            if (root != null) return root;
            root = new GameObject(TownPropsName).transform;
            root.SetParent(town.World, false);
            return root;
        }

        /// <summary>A renderer-less, unrotated object for one prop's fixtures and moving parts.</summary>
        private static Transform TownPropHost(RaidTown town, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(TownPropRoot(town), false);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            return go.transform;
        }

        /// <summary>A wreck's glovebox (on the passenger side of the cab) and its trunk — the bed of a pickup or a truck —
        /// each a little storage worth a look, and from the cab the horn (a lure, if the battery's still good) and the
        /// ignition. <paramref name="size"/> is the wreck's footprint as drawn (width, height, length).</summary>
        private static void WreckStorage(RaidTown town, Vector3 pos, float yaw, DecorProps.CarKind kind, Vector3 size)
        {
            int seed = KitchenParts.SeedAt(pos, 17);
            Transform host = TownPropHost(town, "Wreck", pos);
            var f = new KitchenFrame(pos, yaw);
            float hw = size.x * 0.5f, hl = size.z * 0.5f;
            bool truck = kind == DecorProps.CarKind.ArmyTruck, pickup = kind == DecorProps.CarKind.Pickup;
            float doorZ = truck ? hl - 0.85f : kind == DecorProps.CarKind.Wagon ? 0.3f : pickup ? 0.5f : 0.25f;
            bool live = (seed % 100) < CloneZConfig.RaidHornLiveChance * 100f;

            Vector3 cab = f.P(hw + 0.12f, truck ? 1.3f : 0.95f, doorZ);
            StorageFixture.Create(host, truck ? "Truck cab" : "Glovebox", Interactable.KeyFor("glovebox", pos), cab, null, host, seed,
                new StorageOptions
                {
                    Noun = truck ? "cab" : "glovebox",
                    LookLabel = truck ? "Search the cab" : "Search the glovebox",
                    LootType = ContainerType.Desk,
                    EmptyChance = CloneZConfig.RaidGloveboxEmptyChance,
                    MaxStacks = CloneZConfig.RaidGloveboxMaxStacks,
                    RaidBoost = false,
                    ExtraActions = (ctx, list) => WreckCabActions(list, cab, live),
                });

            bool police = kind == DecorProps.CarKind.Police;
            string back = truck || pickup ? "Truck bed" : "Trunk";
            Vector3 rear = f.P(0f, truck ? 1.15f : pickup ? 0.95f : 0.85f, -hl - 0.12f);
            StorageFixture.Create(host, back, Interactable.KeyFor("trunk", pos), rear, null, host, seed + 1,
                new StorageOptions
                {
                    Noun = back.ToLowerInvariant(),
                    LookLabel = truck || pickup ? "Search the truck bed" : "Pop the trunk",
                    LootType = police ? ContainerType.GunLocker : ContainerType.Crate,
                    EmptyChance = police ? CloneZConfig.RaidTrunkEmptyChance + 0.1f : CloneZConfig.RaidTrunkEmptyChance,
                    MaxStacks = police ? 1 : CloneZConfig.RaidTrunkMaxStacks,
                    SearchTime = CloneZConfig.RaidTrunkSearchSeconds,
                    Noise = CloneZConfig.FixtureNoiseDoor,
                    RaidBoost = false,
                });
        }

        // From the driver's seat: the horn (a racket heard across town while there's charge in the battery) and the ignition.
        private static void WreckCabActions(List<InteractAction> into, Vector3 cab, bool live)
        {
            float next = 0f;
            var horn = new InteractAction("Lean on the horn").Timed(CloneZConfig.RaidHornSeconds, "Leaning on the horn", HumanoidActivity.Use);
            horn.Tick = (c, dt, progress) =>
            {
                if (!live || Time.time < next) return;
                next = Time.time + 0.4f;
                NoiseSystem.Emit(cab, CloneZConfig.RaidHornNoise);
            };
            horn.Run = c => c.Notify(live
                ? "The horn blares across the snow, flat and endless. Every dead ear in town just turned your way."
                : "A dry click from under the dash. The battery's long dead.");
            into.Add(horn);
            into.Add(new InteractAction("Try the ignition", c =>
            {
                NoiseSystem.Emit(cab, CloneZConfig.RaidIgnitionNoise);
                c.Notify(live
                    ? "No keys, but the wires hang loose under the column. You touch two together: the starter grinds, grinds, and nothing catches."
                    : "No keys, and nothing happens when you twist the wires together. Dead as everything else.");
            }));
        }

        /// <summary>A dumpster (its body drawn and made solid like any prop) whose right-hand lid lifts on its back hinge,
        /// with the rubbish inside to go through.</summary>
        private static void DumpsterProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(1.9f, 1.3f, 1.1f), true, (m, r) => FurnitureBuilder.TownDumpsterBody(m, r));
            int seed = KitchenParts.SeedAt(pos, 31);
            Transform host = TownPropHost(town, "Dumpster", pos);
            var f = new KitchenFrame(pos, yaw);
            FixturePart lid = KitchenParts.Door(host, "DumpsterLid", f, FurnitureBuilder.TownDumpsterLid, FurnitureBuilder.TownDumpsterHinge,
                Vector3.forward, Vector3.up, 105f);
            StorageFixture bin = StorageFixture.Create(host, "Dumpster", Interactable.KeyFor("dumpster", pos), f.P(FurnitureBuilder.TownDumpsterReach),
                new[] { lid }, host, seed, new StorageOptions
                {
                    Noun = "dumpster",
                    OpenVerb = "Lift the lid of",
                    CloseVerb = "Let the lid drop on",
                    LookLabel = "Rummage through the dumpster",
                    LootType = ContainerType.Crate,
                    EmptyChance = CloneZConfig.RaidDumpsterEmptyChance,
                    MaxStacks = CloneZConfig.RaidDumpsterMaxStacks,
                    Noise = CloneZConfig.RaidDumpsterLidNoise,
                    RaidBoost = false,
                });
            // Some were left propped open by the last people to use them.
            if ((seed & 1) == 0)
            {
                lid.SetOpen(true, true);
                bin.SetOpen(true, false);
            }
        }

        /// <summary>Trash bags slumped round <paramref name="at"/> (drawn by the caller) to tear open and go through.</summary>
        private static void TrashBagsProp(RaidTown town, Vector3 at)
        {
            at.y = 0f;
            Transform host = TownPropHost(town, "TrashBags", at);
            StorageFixture.Create(host, "Trash bags", Interactable.KeyFor("trashbags", at), at + Vector3.up * 0.35f, null, host,
                KitchenParts.SeedAt(at, 37), new StorageOptions
                {
                    Noun = "trash bags",
                    LookLabel = "Tear open the trash bags",
                    LootType = ContainerType.Shelf,
                    EmptyChance = CloneZConfig.RaidTrashBagEmptyChance,
                    MaxStacks = CloneZConfig.RaidTrashBagMaxStacks,
                    SearchTime = CloneZConfig.RaidTrashBagSearchSeconds,
                    RaidBoost = false,
                });
        }

        /// <summary>A gas pump on its island (drawn and made solid like any prop) to be worked from either side.</summary>
        private static void PumpProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(1.0f, 1.7f, 1.6f), true, (m, r) => DecorProps.GasPump(m, r));
            RaidTownGasPump.Create(TownPropHost(town, "GasPump", pos), pos, yaw, 0.5f);
        }

        /// <summary>The quarantine site's generator, with its starter cord, its kill switch and its run lamp.</summary>
        private static void GeneratorProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(1.1f, 0.8f, 0.7f), true, (m, r) => DecorProps.Generator(m, r));
            Transform host = TownPropHost(town, "Generator", pos);
            var f = new KitchenFrame(pos, yaw);
            MeshRenderer lamp = KitchenParts.PieceAt(host, "RunLamp", f, new Vector3(0.3f, 0.46f, 0.323f),
                m => FurnitureBuilder.TownLampGlow(m, new Color(0.3f, 0.95f, 0.4f), 0.03f, 0.02f));
            RaidTownGenerator.Create(host, f.P(0.2f, 0.6f, 0.42f), lamp, KitchenParts.SeedAt(pos, 41));
        }

        /// <summary>A fire hydrant (walk-through, like before) to try.</summary>
        private static void HydrantProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(0.4f, 0.8f, 0.4f), false, (m, r) => DecorProps.Hydrant(m, r));
            RaidTownHydrant.Create(TownPropHost(town, "Hydrant", pos), pos + Vector3.up * 0.62f);
        }

        /// <summary>A blue public mail drop box on the sidewalk, its chute to peer into.</summary>
        private static void MailDropProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(0.5f, 1.4f, 0.5f), true, (m, r) => DecorProps.Mailbox(m, r, true));
            var f = new KitchenFrame(pos, yaw);
            RaidTownMailDrop.Create(TownPropHost(town, "MailDrop", pos), f.P(0f, 0.98f, 0.33f), KitchenParts.SeedAt(pos, 43));
        }

        // The checkpoint log, read a page at a time. All original.
        private static readonly string[] BoothLog =
        {
            "The checkpoint log, in three different hands. 0600: \u201CRelieved Pvt. Hale. Nothing to report. Snow.\u201D",
            "0830: \u201CTraffic backed up at the main road. Commander ordered us to hold position. No word from HQ.\u201D",
            "1140: \u201CTurned back six vehicles. One woman would not stop crying. Sent her to the clinic per orders.\u201D",
            "1320: \u201COrders changed. No clear directive. Radio static, nothing but wind.\u201D",
            "1615: \u201CClinic not answering. Lt. says hold the line and keep the barrier down.\u201D",
            "1840: \u201CRadio gone silent. No contact. Everyone uneasy.\u201D",
            "2050: \u201CMovement past the barrier. Challenged twice. They kept coming.\u201D Then just a line of ink trailing off the page.",
        };

        /// <summary>The checkpoint's guard booth: its body drawn and made solid like any prop, its front hatch sliding open on
        /// the desk inside (a little storage, and the checkpoint log to read), and the booth radio on the ledge.</summary>
        private static void GuardBoothProp(RaidTown town, Vector3 pos, float yaw)
        {
            Prop(town, pos, yaw, new Vector3(1.7f, 2.5f, 1.7f), true, (m, r) => FurnitureBuilder.TownBoothBody(m, r));
            int seed = KitchenParts.SeedAt(pos, 47);
            Transform host = TownPropHost(town, "GuardBooth", pos);
            var f = new KitchenFrame(pos, yaw);
            FixturePart hatch = KitchenParts.Drawer(host, "BoothHatch", f, FurnitureBuilder.TownBoothHatch, new Vector3(-0.25f, 1.55f, 0.78f),
                new Vector3(FurnitureBuilder.TownBoothHatchTravel, 0f, 0f));
            int page = 0;
            StorageFixture.Create(host, "Guard booth", Interactable.KeyFor("booth", pos), f.P(FurnitureBuilder.TownBoothHatchReach), new[] { hatch },
                host, seed, new StorageOptions
                {
                    Noun = "booth hatch",
                    ContainerName = "Booth desk",
                    OpenVerb = "Slide open",
                    CloseVerb = "Slide shut",
                    LookLabel = "Search the booth desk",
                    LootType = ContainerType.Desk,
                    EmptyChance = CloneZConfig.RaidBoothEmptyChance,
                    MaxStacks = CloneZConfig.RaidBoothMaxStacks,
                    RaidBoost = false,
                    ExtraActions = (ctx, list) => list.Add(new InteractAction("Read the logbook", c => c.Notify(BoothLog[page++ % BoothLog.Length]))),
                });
            MeshRenderer dial = KitchenParts.PieceAt(host, "RadioDial", f, FurnitureBuilder.TownBoothRadio + new Vector3(-0.07f, 0.16f, 0.128f),
                FurnitureBuilder.TownFieldRadioDial);
            RaidTownFieldRadio.Create(host, "Booth radio", f.P(FurnitureBuilder.TownBoothRadioReach), dial, seed + 7);
        }
        #endregion
    }
}
