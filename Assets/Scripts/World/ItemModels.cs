using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds bespoke, hand-modelled ground visuals for specific items, assembled out of primitives
    /// (the project keeps no imported meshes or prefabs).
    ///
    /// A builder no longer creates anything in the scene. It <em>describes</em> a model — a list of
    /// placed primitives and their colours — which is then baked once into a single combined mesh and
    /// drawn instanced for every copy of that item lying in the world. This matters because the world
    /// carries items in the millions: building a few hundred GameObjects per dropped item, as this
    /// used to, could never have reached that scale. Descriptions depend only on the item definition,
    /// never on the individual item, so one description per item type serves every instance of it and
    /// is cached here for the life of the session.
    /// </summary>
    public static class ItemModels
    {
        private static readonly Dictionary<string, ItemModelDesc> s_Cache = new Dictionary<string, ItemModelDesc>(128);

        /// <summary>
        /// The model for <paramref name="def"/>, or null when the item has no bespoke model and the
        /// caller should fall back to a simple tinted primitive. Built at most once per item type.
        /// </summary>
        public static ItemModelDesc Describe(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (s_Cache.TryGetValue(def.Id, out ItemModelDesc cached)) return cached;

            // The builders were written to hang their root off a parent, so give them one to hang off.
            var host = new PartNode("Host");
            PartNode root = Dispatch(def, host, out float baseHeight);

            ItemModelDesc desc = root == null ? null : ItemModelDesc.FromRoot(root, baseHeight);
            s_Cache[def.Id] = desc;
            return desc;
        }

        /// <summary>True when <paramref name="def"/> has a bespoke model.</summary>
        public static bool Has(ItemDef def) => Describe(def) != null;

        /// <summary>
        /// Builds the model as real GameObjects under <paramref name="parent"/>. This is the old way
        /// every dropped item was drawn and is far too costly for the ground, but it remains the
        /// simplest way to show one model up close, so it is kept for inspection and for checking the
        /// instanced path against something known good.
        /// </summary>
        public static bool TryBuild(ItemDef def, Transform parent, out Transform visual, out float baseHeight)
        {
            visual = null;
            baseHeight = 0f;

            ItemModelDesc desc = Describe(def);
            if (desc == null) return false;

            baseHeight = desc.BaseHeight;
            visual = desc.ToGameObject(parent);
            return true;
        }

        /// <summary>Picks the builder for an item id and lets it describe its model.</summary>
        private static PartNode Dispatch(ItemDef def, PartNode parent, out float baseHeight)
        {
            baseHeight = 0f;

            switch (def.Id)
            {
                case "cannedbeans":
                    return BuildCan(def, parent, out baseHeight);
                case "cannedsoup":
                    return BuildSoupCan(def, parent, out baseHeight);
                case "soda":
                    return BuildSodaCan(def, parent, out baseHeight);
                case "juicebox":
                    return BuildJuiceBox(def, parent, out baseHeight);
                case "chips":
                    return BuildChipBag(def, parent, out baseHeight);
                case "crackers":
                    return BuildCrackerBox(def, parent, out baseHeight);
                case "cerealbox":
                    return BuildCerealBox(def, parent, out baseHeight);
                case "apple":
                    return BuildApple(def, parent, out baseHeight);
                case "cabbage":
                    return BuildCabbage(def, parent, out baseHeight);
                case "tomato":
                    return BuildTomato(def, parent, out baseHeight);
                case "carrot":
                    return BuildCarrot(def, parent, out baseHeight);
                case "breadloaf":
                    return BuildBreadLoaf(def, parent, out baseHeight);
                case "potato":
                    return BuildPotato(def, parent, out baseHeight);
                case "bakedpotato":
                    return BuildBakedPotato(def, parent, out baseHeight);
                case "rawmeat":
                    return BuildRawMeat(def, parent, out baseHeight);
                case "cookedmeat":
                    return BuildCookedMeat(def, parent, out baseHeight);
                case "chicken":
                    return BuildRawChicken(def, parent, out baseHeight);
                case "cookedchicken":
                    return BuildCookedChicken(def, parent, out baseHeight);
                case "waterbottle":
                    return BuildWaterBottle(def, parent, out baseHeight);
                case "taintedwater":
                    return BuildTaintedWater(def, parent, out baseHeight);
                case "emptybottle":
                    return BuildEmptyBottle(def, parent, out baseHeight);
                case "bandage":
                    return BuildBandage(def, parent, out baseHeight);
                case "pills":
                    return BuildPills(def, parent, out baseHeight);
                case "antibiotics":
                    return BuildAntibiotics(def, parent, out baseHeight);
                case "disinfectant":
                    return BuildDisinfectant(def, parent, out baseHeight);
                case "firstaid":
                    return BuildFirstAidKit(def, parent, out baseHeight);
                case "betablockers":
                    return BuildBetaBlockers(def, parent, out baseHeight);
                case "kitchenknife":
                    return BuildKitchenKnife(def, parent, out baseHeight);
                case "hammer":
                    return BuildHammer(def, parent, out baseHeight);
                case "fryingpan":
                    return BuildFryingPan(def, parent, out baseHeight);
                case "crowbar":
                    return BuildCrowbar(def, parent, out baseHeight);
                case "baseballbat":
                    return BuildBaseballBat(def, parent, out baseHeight);
                case "fireaxe":
                    return BuildFireAxe(def, parent, out baseHeight);
                case "pistol":
                    return BuildPistol(def, parent, out baseHeight);
                case "shotgun":
                    return BuildShotgun(def, parent, out baseHeight);
                case "ammo9mm":
                    return BuildPistolAmmo(def, parent, out baseHeight);
                case "shotgunshells":
                    return BuildShotgunShells(def, parent, out baseHeight);
                case "plank":
                    return BuildPlank(def, parent, out baseHeight);
                case "nails":
                    return BuildNailBox(def, parent, out baseHeight);
                case "ducttape":
                    return BuildDuctTape(def, parent, out baseHeight);
                case "rag":
                    return BuildRag(def, parent, out baseHeight);
                case "rope":
                    return BuildRope(def, parent, out baseHeight);
                case "canopener":
                    return BuildCanOpener(def, parent, out baseHeight);
                case "tincan":
                    return BuildTinCan(def, parent, out baseHeight);
                case "needle":
                    return BuildNeedle(def, parent, out baseHeight);
                case "thread":
                    return BuildThread(def, parent, out baseHeight);
                case "trowel":
                    return BuildTrowel(def, parent, out baseHeight);
                case "wateringcan":
                    return BuildWateringCan(def, parent, out baseHeight);
                case "potatoseeds":
                    return BuildPotatoSeeds(def, parent, out baseHeight);
                case "carrotseeds":
                    return BuildCarrotSeeds(def, parent, out baseHeight);
                case "tomatoseeds":
                    return BuildTomatoSeeds(def, parent, out baseHeight);
                case "cabbageseeds":
                    return BuildCabbageSeeds(def, parent, out baseHeight);
                case "battery":
                    return BuildBattery(def, parent, out baseHeight);
                case "cap":
                    return BuildCap(def, parent, out baseHeight);
                case "beanie":
                    return BuildBeanie(def, parent, out baseHeight);
                case "bikehelmet":
                    return BuildBikeHelmet(def, parent, out baseHeight);
                case "tshirt":
                    return BuildTshirt(def, parent, out baseHeight);
                case "hoodie":
                    return BuildHoodie(def, parent, out baseHeight);
                case "jacket":
                    return BuildJacket(def, parent, out baseHeight);
                case "leatherjacket":
                    return BuildLeatherJacket(def, parent, out baseHeight);
                case "raincoat":
                    return BuildRaincoat(def, parent, out baseHeight);
                case "shorts":
                    return BuildShorts(def, parent, out baseHeight);
                case "jeans":
                    return BuildJeans(def, parent, out baseHeight);
                case "trousers":
                    return BuildTrousers(def, parent, out baseHeight);
                case "sneakers":
                    return BuildSneakers(def, parent, out baseHeight);
                case "boots":
                    return BuildBoots(def, parent, out baseHeight);
                case "gloves":
                    return BuildGloves(def, parent, out baseHeight);
                case "leathergloves":
                    return BuildLeatherGloves(def, parent, out baseHeight);
                case "Dress_Knees":
                    return BuildDressKnees(def, parent, out baseHeight);
                case "DressKnees_Straps":
                    return BuildDressKneesStraps(def, parent, out baseHeight);
                case "Dress_Long":
                    return BuildDressLong(def, parent, out baseHeight);
                case "Dress_long_Straps":
                    return BuildDressLongStraps(def, parent, out baseHeight);
                case "Dress_Normal":
                    return BuildDressNormal(def, parent, out baseHeight);
                case "Dress_SatinNegligee":
                    return BuildDressSatinNegligee(def, parent, out baseHeight);
                case "Dress_Short":
                    return BuildDressShort(def, parent, out baseHeight);
                case "Dress_SmallBlackStrapless":
                    return BuildDressSmallBlackStrapless(def, parent, out baseHeight);
                case "Dress_SmallBlackStraps":
                    return BuildDressSmallBlackStraps(def, parent, out baseHeight);
                case "Dress_SmallStrapless":
                    return BuildDressSmallStrapless(def, parent, out baseHeight);
                case "Dress_SmallStraps":
                    return BuildDressSmallStraps(def, parent, out baseHeight);
                case "Dress_Straps":
                    return BuildDressStraps(def, parent, out baseHeight);
                case "WeddingDress":
                    return BuildWeddingDress(def, parent, out baseHeight);
                case "BlackRobe":
                    return BuildBlackRobe(def, parent, out baseHeight);
                case "Dress_Long_Crafted_Burlap":
                    return BuildDressLongCraftedBurlap(def, parent, out baseHeight);
                case "Dress_Long_Crafted_Cotton":
                    return BuildDressLongCraftedCotton(def, parent, out baseHeight);
                case "Dress_Long_Crafted_Denim":
                    return BuildDressLongCraftedDenim(def, parent, out baseHeight);
                case "Dress_Long_Crafted_DenimBlack":
                    return BuildDressLongCraftedDenimBlack(def, parent, out baseHeight);
                case "Dress_Long_Crafted_DenimLight":
                    return BuildDressLongCraftedDenimLight(def, parent, out baseHeight);
                case "Dress_Knees_Crafted_Burlap":
                    return BuildDressKneesCraftedBurlap(def, parent, out baseHeight);
                case "Dress_Knees_Crafted_Cotton":
                    return BuildDressKneesCraftedCotton(def, parent, out baseHeight);
                case "Dress_Knees_Crafted_Denim":
                    return BuildDressKneesCraftedDenim(def, parent, out baseHeight);
                case "Dress_Knees_Crafted_DenimBlack":
                    return BuildDressKneesCraftedDenimBlack(def, parent, out baseHeight);
                case "Dress_Knees_Crafted_DenimLight":
                    return BuildDressKneesCraftedDenimLight(def, parent, out baseHeight);
                case "Dress_SmallGarbageStrapless":
                    return BuildDressSmallGarbageStrapless(def, parent, out baseHeight);
                case "Dress_SmallTarpStrapless":
                    return BuildDressSmallTarpStrapless(def, parent, out baseHeight);
                default:
                    return null;
            }
        }

        /// <summary>
        /// A sealed food tin: a steel body with a paper label wrapped round the middle and the classic
        /// rolled seams top and bottom, capped by a recessed lid. The label takes the item's tint so the
        /// can reads as that food where it lies on the floor.
        /// </summary>
        private static PartNode BuildCan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Height the can's centre floats at — matches the hover of the primitive food cube it replaces.
            baseHeight = 0.26f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color tin = new Color(0.79f, 0.80f, 0.83f);   // bare steel
            Color lid = new Color(0.70f, 0.71f, 0.74f);   // slightly darker recessed lid
            Color label = def.Tint;                       // printed paper label
            Color trim = new Color(label.r * 0.55f, label.g * 0.55f, label.b * 0.55f);   // label border

            PartMat tinMat = PartMat.Of(tin, smoothness: 0.55f, metallic: 0.85f);
            PartMat lidMat = PartMat.Of(lid, smoothness: 0.45f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.12f);

            const float bodyD = 0.20f;   // body diameter
            const float bodyH = 0.29f;   // full can height
            const float rimD = 0.212f;   // rolled-seam diameter (pokes past the body)
            const float half = bodyH * 0.5f;

            // Steel body.
            Cylinder(root, "Body", 0f, bodyD, bodyH, tinMat);

            // Rolled seams top and bottom.
            Cylinder(root, "SeamTop", half - 0.008f, rimD, 0.016f, tinMat);
            Cylinder(root, "SeamBottom", -(half - 0.008f), rimD, 0.016f, tinMat);

            // Recessed sealed lid, sitting just inside the top seam.
            Cylinder(root, "Lid", half - 0.004f, bodyD - 0.02f, 0.012f, lidMat);

            // Paper label wrapped round the middle, standing a hair proud of the body.
            Cylinder(root, "Label", 0f, bodyD + 0.006f, 0.19f, labelMat);

            // Printed borders along the top and bottom edges of the label.
            Cylinder(root, "LabelTrimTop", 0.097f, bodyD + 0.008f, 0.008f, trimMat);
            Cylinder(root, "LabelTrimBottom", -0.097f, bodyD + 0.008f, 0.008f, trimMat);

            return root;
        }

        /// <summary>
        /// A tall, narrow condensed-soup tin in the classic style: a slim steel body with rolled seams,
        /// a recessed lid, and a two-tone wrapped label — the item's tint across the top, a cream band
        /// below, split by a gold divider — so it reads distinctly from the shorter beans can on the floor.
        /// </summary>
        private static PartNode BuildSoupCan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Slightly higher hover than the beans can, keeping the taller tin's base near the ground.
            baseHeight = 0.28f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color tin = new Color(0.79f, 0.80f, 0.83f);       // bare steel
            Color lid = new Color(0.70f, 0.71f, 0.74f);       // slightly darker recessed lid
            Color topBand = def.Tint;                          // printed upper half of the label
            Color bottomBand = new Color(0.90f, 0.87f, 0.78f); // cream lower half
            Color gold = new Color(0.83f, 0.68f, 0.30f);       // gilt divider and edge trim

            PartMat tinMat = PartMat.Of(tin, smoothness: 0.55f, metallic: 0.85f);
            PartMat lidMat = PartMat.Of(lid, smoothness: 0.45f, metallic: 0.85f);
            PartMat topMat = PartMat.Of(topBand, smoothness: 0.12f);
            PartMat bottomMat = PartMat.Of(bottomBand, smoothness: 0.12f);
            PartMat goldMat = PartMat.Of(gold, smoothness: 0.35f, metallic: 0.55f);

            const float bodyD = 0.17f;   // body diameter (narrower than the beans can)
            const float bodyH = 0.34f;   // full can height (taller than the beans can)
            const float rimD = 0.182f;   // rolled-seam diameter (pokes past the body)
            const float half = bodyH * 0.5f;

            // Steel body.
            Cylinder(root, "Body", 0f, bodyD, bodyH, tinMat);

            // Rolled seams top and bottom.
            Cylinder(root, "SeamTop", half - 0.008f, rimD, 0.016f, tinMat);
            Cylinder(root, "SeamBottom", -(half - 0.008f), rimD, 0.016f, tinMat);

            // Recessed sealed lid, sitting just inside the top seam.
            Cylinder(root, "Lid", half - 0.004f, bodyD - 0.018f, 0.012f, lidMat);

            // Two-tone paper label: tinted top half over a cream lower half, each standing a hair proud.
            Cylinder(root, "LabelTop", 0.07f, bodyD + 0.006f, 0.14f, topMat);
            Cylinder(root, "LabelBottom", -0.07f, bodyD + 0.006f, 0.14f, bottomMat);

            // Gilt divider between the bands and matching trim along the label's top and bottom edges.
            Cylinder(root, "LabelDivider", 0f, bodyD + 0.010f, 0.022f, goldMat);
            Cylinder(root, "LabelTrimTop", 0.138f, bodyD + 0.010f, 0.010f, goldMat);
            Cylinder(root, "LabelTrimBottom", -0.138f, bodyD + 0.010f, 0.010f, goldMat);

            return root;
        }

        /// <summary>
        /// An aluminium beverage can: a drawn body printed in the item's tint that necks in to a bright
        /// silver lid with a stay-on pull tab, a rolled standing base below, and a wrapped white label band
        /// with a bright swoosh across the middle — so it reads unmistakably as a can of soda where it lies
        /// on the floor, distinct from the paper-labelled food tins.
        /// </summary>
        private static PartNode BuildSodaCan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Tall, slim can — hover it high enough that the gentle bob never dips the base through the floor.
            baseHeight = 0.29f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color brand = def.Tint;                                                         // printed aluminium body
            Color brandDeep = new Color(brand.r * 0.5f, brand.g * 0.5f, brand.b * 0.5f);    // shaded printed trim lines
            Color alu = new Color(0.82f, 0.83f, 0.86f);                                     // bright aluminium lid, tab, base
            Color aluShade = new Color(0.64f, 0.65f, 0.68f);                                // recessed lid panel and tab rivet
            Color band = new Color(0.95f, 0.95f, 0.92f);                                    // printed white label band
            Color accent = new Color(0.85f, 0.22f, 0.18f);                                  // bright brand swoosh

            PartMat brandMat = PartMat.Of(brand, smoothness: 0.45f, metallic: 0.35f);  // glossy printed metal
            PartMat aluMat = PartMat.Of(alu, smoothness: 0.60f, metallic: 0.90f);
            PartMat aluShadeMat = PartMat.Of(aluShade, smoothness: 0.50f, metallic: 0.90f);
            PartMat bandMat = PartMat.Of(band, smoothness: 0.20f);
            PartMat accentMat = PartMat.Of(accent, smoothness: 0.25f);
            PartMat trimMat = PartMat.Of(brandDeep, smoothness: 0.35f, metallic: 0.35f);

            const float bodyD = 0.175f;   // body diameter
            const float bodyH = 0.30f;    // straight printed wall height

            // Printed aluminium body.
            Cylinder(root, "Body", -0.015f, bodyD, bodyH, brandMat);

            // Rolled base: a slightly inset silver standing rim with the concave underside dome tucked below it.
            Cylinder(root, "BaseRim", -0.16f, 0.168f, 0.022f, aluMat);
            Cylinder(root, "BaseDome", -0.172f, 0.12f, 0.012f, aluShadeMat);

            // Necked-in silver shoulder stepping the body diameter down to the smaller lid.
            Cylinder(root, "NeckLow", 0.145f, 0.162f, 0.02f, aluMat);
            Cylinder(root, "NeckHigh", 0.16f, 0.148f, 0.014f, aluMat);

            // Stamped lid with a recessed inner panel.
            Cylinder(root, "Lid", 0.17f, 0.142f, 0.012f, aluMat);
            Cylinder(root, "LidPanel", 0.175f, 0.122f, 0.006f, aluShadeMat);

            // Stay-on pull tab riveted at the centre of the lid.
            Cylinder(root, "TabRivet", 0.18f, 0.024f, 0.008f, aluShadeMat);
            Block(root, "Tab", new Vector3(0f, 0.181f, 0.012f), new Vector3(0.03f, 0.006f, 0.072f), aluMat);

            // Wrapped white label band round the middle with a bright swoosh and shaded edge trims.
            Cylinder(root, "LabelBand", -0.01f, bodyD + 0.004f, 0.11f, bandMat);
            Cylinder(root, "Swoosh", 0.012f, bodyD + 0.006f, 0.024f, accentMat);
            Cylinder(root, "TrimTop", 0.045f, bodyD + 0.006f, 0.008f, trimMat);
            Cylinder(root, "TrimBottom", -0.065f, bodyD + 0.006f, 0.008f, trimMat);

            return root;
        }

        /// <summary>
        /// A single-serving juice carton (a little Tetra-Brik): an upright printed box with a flat sealed
        /// top — a raised centre fin and the two top corners folded down into little ears — a foil-sealed
        /// straw hole, and the classic thin bendy straw poked in and standing proud. A cream brand band and
        /// a printed fruit motif dress the front. The carton takes the item's tint, so its box-plus-straw
        /// silhouette is what tells it apart from the soda can and the water bottle it shares that tint with.
        /// </summary>
        private static PartNode BuildJuiceBox(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the little upright carton, keeping its base just off the ground like its siblings.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color carton = def.Tint;                                                          // printed foil-carton
            Color shade = new Color(carton.r * 0.78f, carton.g * 0.78f, carton.b * 0.78f);    // folded ears / base seam
            Color foil = new Color(0.85f, 0.86f, 0.90f);                                      // silvered top seal / straw patch
            Color band = new Color(0.97f, 0.95f, 0.89f);                                      // cream printed brand band
            Color trim = new Color(carton.r * 0.5f, carton.g * 0.5f, carton.b * 0.5f);        // darker brand stripe
            Color fruit = new Color(0.98f, 0.55f, 0.14f);                                     // printed fruit motif
            Color leaf = new Color(0.42f, 0.66f, 0.28f);                                      // little leaf on the fruit
            Color straw = new Color(0.95f, 0.95f, 0.97f);                                     // white bendy straw
            Color strawBand = new Color(0.86f, 0.24f, 0.20f);                                 // red stripe round the straw

            PartMat cartonMat = PartMat.Of(carton, smoothness: 0.16f);   // coated printed carton, faint sheen
            PartMat shadeMat = PartMat.Of(shade, smoothness: 0.14f);
            PartMat foilMat = PartMat.Of(foil, smoothness: 0.55f, metallic: 0.7f);
            PartMat bandMat = PartMat.Of(band, smoothness: 0.15f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.15f);
            PartMat fruitMat = PartMat.Of(fruit, smoothness: 0.20f);
            PartMat leafMat = PartMat.Of(leaf, smoothness: 0.15f);
            PartMat strawMat = PartMat.Of(straw, smoothness: 0.40f);
            PartMat strawBandMat = PartMat.Of(strawBand, smoothness: 0.40f);

            const float w = 0.10f;       // carton width (broad printed face on Z)
            const float d = 0.07f;       // carton depth
            const float bodyH = 0.16f;   // carton body height
            const float half = bodyH * 0.5f;
            const float top = half;
            float faceZ = d * 0.5f + 0.003f;   // front/back printed face

            // Printed carton body.
            Block(root, "Body", Vector3.zero, new Vector3(w, bodyH, d), cartonMat);

            // Shaded seam along the folded, glued base.
            Block(root, "BaseSeam", new Vector3(0f, -half + 0.006f, 0f), new Vector3(w + 0.002f, 0.012f, d + 0.002f), shadeMat);

            // Sealed top: a flat foil-glued cap, a raised centre fin running front-to-back, and the two side
            // corners folded down into little triangular ears.
            Block(root, "TopSeal", new Vector3(0f, top - 0.004f, 0f), new Vector3(w - 0.004f, 0.01f, d - 0.004f), foilMat);
            Block(root, "FinSeal", new Vector3(0f, top + 0.012f, 0f), new Vector3(0.01f, 0.024f, d - 0.006f), foilMat);
            Block(root, "EarLeft", new Vector3(-w * 0.5f + 0.012f, top - 0.006f, 0f), new Vector3(0.03f, 0.008f, d * 0.85f), shadeMat).localEulerAngles = new Vector3(0f, 0f, 34f);
            Block(root, "EarRight", new Vector3(w * 0.5f - 0.012f, top - 0.006f, 0f), new Vector3(0.03f, 0.008f, d * 0.85f), shadeMat).localEulerAngles = new Vector3(0f, 0f, -34f);

            // Foil-sealed straw hole punched near the front of the top face.
            Rod(root, "StrawHole", new Vector3(0.02f, top + 0.002f, 0.014f), Vector3.zero, 0.02f, 0.006f, foilMat);

            // The classic thin straw poked in and standing proud, wrapped by a red stripe with a bent tip.
            Rod(root, "Straw", new Vector3(0.02f, top + 0.055f, 0.014f), new Vector3(4f, 0f, 9f), 0.013f, 0.12f, strawMat);
            Rod(root, "StrawStripe", new Vector3(0.02f, top + 0.055f, 0.014f), new Vector3(4f, 0f, 9f), 0.016f, 0.014f, strawBandMat);
            Rod(root, "StrawTip", new Vector3(0.006f, top + 0.116f, 0.014f), new Vector3(4f, 0f, 40f), 0.013f, 0.03f, strawMat);

            // Printed cream brand band with a darker stripe beneath it, on the front and back faces.
            Block(root, "BrandFront", new Vector3(0f, half - 0.03f, faceZ), new Vector3(w * 0.86f, 0.028f, 0.005f), bandMat);
            Block(root, "BrandBack", new Vector3(0f, half - 0.03f, -faceZ), new Vector3(w * 0.86f, 0.028f, 0.005f), bandMat);
            Block(root, "StripeFront", new Vector3(0f, half - 0.05f, faceZ + 0.001f), new Vector3(w * 0.86f, 0.008f, 0.005f), trimMat);
            Block(root, "StripeBack", new Vector3(0f, half - 0.05f, -(faceZ + 0.001f)), new Vector3(w * 0.86f, 0.008f, 0.005f), trimMat);

            // A little printed fruit low on the front: two overlapping orbs and a tucked leaf, standing a hair
            // proud of the carton like the rest of the artwork.
            Sphere(root, "Fruit1", new Vector3(-0.012f, -0.032f, faceZ), Vector3.zero, new Vector3(0.04f, 0.04f, 0.016f), fruitMat);
            Sphere(root, "Fruit2", new Vector3(0.016f, -0.04f, faceZ), Vector3.zero, new Vector3(0.03f, 0.03f, 0.014f), fruitMat);
            Sphere(root, "Leaf", new Vector3(-0.004f, -0.006f, faceZ + 0.002f), new Vector3(0f, 0f, 32f), new Vector3(0.024f, 0.008f, 0.012f), leafMat);

            return root;
        }

        /// <summary>
        /// A puffed foil snack bag: a mylar pouch printed in the item's tint, fattest across the middle
        /// and tapering to pinched, crimped seals top and bottom, with a cream label patch and a brand
        /// stripe on each face so it reads as a bag of chips where it lies on the floor.
        /// </summary>
        private static PartNode BuildChipBag(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover height matching the sibling canned-food models and the primitive food cube.
            baseHeight = 0.26f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color foil = def.Tint;                                                    // printed mylar
            Color crimp = new Color(foil.r * 0.55f, foil.g * 0.55f, foil.b * 0.55f);  // shadowed pinched seals
            Color panel = new Color(0.93f, 0.91f, 0.83f);                             // cream label patch
            Color stripe = new Color(0.78f, 0.20f, 0.16f);                            // red brand stripe

            PartMat foilMat = PartMat.Of(foil, smoothness: 0.40f);   // glossy mylar sheen
            PartMat crimpMat = PartMat.Of(crimp, smoothness: 0.30f);
            PartMat panelMat = PartMat.Of(panel, smoothness: 0.10f);
            PartMat stripeMat = PartMat.Of(stripe, smoothness: 0.10f);

            const float w = 0.20f;   // bag width
            const float d = 0.11f;   // centre puff depth

            // Puffed pouch: fattest across the centre, tapering to the pinched shoulders.
            Block(root, "Body", new Vector3(0f, 0f, 0f), new Vector3(w, 0.15f, d), foilMat);
            Block(root, "ShoulderTop", new Vector3(0f, 0.085f, 0f), new Vector3(w * 0.88f, 0.06f, 0.085f), foilMat);
            Block(root, "ShoulderBottom", new Vector3(0f, -0.085f, 0f), new Vector3(w * 0.88f, 0.06f, 0.085f), foilMat);

            // Crimped seals top and bottom, pinched flat and standing a touch wider than the bag.
            Block(root, "SealTop", new Vector3(0f, 0.135f, 0f), new Vector3(w + 0.015f, 0.03f, 0.03f), crimpMat);
            Block(root, "SealBottom", new Vector3(0f, -0.135f, 0f), new Vector3(w + 0.015f, 0.03f, 0.03f), crimpMat);

            // Cream label patch on the front and back of the puff, each crossed by a brand stripe.
            float faceZ = d * 0.5f + 0.004f;
            Block(root, "LabelFront", new Vector3(0f, -0.005f, faceZ), new Vector3(w * 0.66f, 0.11f, 0.008f), panelMat);
            Block(root, "LabelBack", new Vector3(0f, -0.005f, -faceZ), new Vector3(w * 0.66f, 0.11f, 0.008f), panelMat);
            Block(root, "StripeFront", new Vector3(0f, 0.035f, faceZ + 0.004f), new Vector3(w * 0.66f, 0.028f, 0.006f), stripeMat);
            Block(root, "StripeBack", new Vector3(0f, 0.035f, -(faceZ + 0.004f)), new Vector3(w * 0.66f, 0.028f, 0.006f), stripeMat);

            return root;
        }

        /// <summary>
        /// A rectangular cardboard cracker carton: a kraft-card box with a folded top-flap rim, a
        /// printed label panel in the item's tint on each face, a brand stripe, and a little pale
        /// stack-of-crackers motif — so it reads as a boxed snack, distinct from the tins and the
        /// puffed chip bag where it lies on the floor.
        /// </summary>
        private static PartNode BuildCrackerBox(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the upright carton, keeping its base just off the ground like its siblings.
            baseHeight = 0.24f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color card = new Color(0.79f, 0.64f, 0.42f);                               // kraft cardboard
            Color flap = new Color(card.r * 0.86f, card.g * 0.86f, card.b * 0.86f);    // shaded folded top flaps
            Color label = def.Tint;                                                    // printed front panel
            Color trim = new Color(label.r * 0.55f, label.g * 0.55f, label.b * 0.55f); // brand stripe / border
            Color cracker = new Color(0.90f, 0.78f, 0.52f);                            // cream cracker motif

            PartMat cardMat = PartMat.Of(card, smoothness: 0.06f);
            PartMat flapMat = PartMat.Of(flap, smoothness: 0.06f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.12f);
            PartMat crackerMat = PartMat.Of(cracker, smoothness: 0.08f);

            const float w = 0.19f;    // carton width
            const float h = 0.25f;    // carton height
            const float d = 0.075f;   // carton depth (a flat box standing on end)
            const float half = h * 0.5f;

            // Cardboard carton body.
            Block(root, "Body", Vector3.zero, new Vector3(w, h, d), cardMat);

            // Folded top flaps: a slightly inset, shaded rim across the top edge.
            Block(root, "Flaps", new Vector3(0f, half - 0.004f, 0f), new Vector3(w - 0.01f, 0.02f, d - 0.006f), flapMat);

            // Printed label panels standing a hair proud of the front and back faces.
            float faceZ = d * 0.5f + 0.004f;
            Block(root, "LabelFront", new Vector3(0f, 0.01f, faceZ), new Vector3(w * 0.82f, h * 0.72f, 0.006f), labelMat);
            Block(root, "LabelBack", new Vector3(0f, 0.01f, -faceZ), new Vector3(w * 0.82f, h * 0.72f, 0.006f), labelMat);

            // Brand stripe across the top of each label.
            Block(root, "StripeFront", new Vector3(0f, 0.075f, faceZ + 0.004f), new Vector3(w * 0.82f, 0.03f, 0.006f), trimMat);
            Block(root, "StripeBack", new Vector3(0f, 0.075f, -(faceZ + 0.004f)), new Vector3(w * 0.82f, 0.03f, 0.006f), trimMat);

            // A little stack-of-crackers motif on the front, two pale squares hinting at the contents.
            Block(root, "Cracker1", new Vector3(-0.026f, -0.035f, faceZ + 0.004f), new Vector3(0.045f, 0.045f, 0.005f), crackerMat);
            Block(root, "Cracker2", new Vector3(0.026f, -0.05f, faceZ + 0.004f), new Vector3(0.045f, 0.045f, 0.005f), crackerMat);

            return root;
        }

        /// <summary>
        /// A tall breakfast-cereal carton: a fully printed cardboard box standing on end — taller and wider
        /// than the cracker box — with folded top flaps, a cream brand band and stripe near the top, pale
        /// nutrition panels down the narrow sides, and a printed "serving suggestion" bowl of cereal on the
        /// front. The carton takes the item's tint so it reads as that brightly-boxed breakfast on the floor.
        /// </summary>
        private static PartNode BuildCerealBox(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the tall carton, keeping its base floating just off the ground like its siblings.
            baseHeight = 0.30f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color card = def.Tint;                                                     // bright printed carton
            Color flap = new Color(card.r * 0.80f, card.g * 0.80f, card.b * 0.80f);    // shaded folded top flaps
            Color brand = new Color(0.96f, 0.94f, 0.88f);                              // cream brand band
            Color trim = new Color(card.r * 0.50f, card.g * 0.50f, card.b * 0.50f);    // darker brand stripe / border
            Color side = new Color(0.92f, 0.90f, 0.84f);                               // pale side nutrition panel
            Color bowl = new Color(0.93f, 0.93f, 0.96f);                               // white serving bowl
            Color flake = new Color(0.82f, 0.55f, 0.24f);                              // toasted cereal flakes

            PartMat cardMat = PartMat.Of(card, smoothness: 0.18f);   // printed, faintly glossy carton
            PartMat flapMat = PartMat.Of(flap, smoothness: 0.10f);
            PartMat brandMat = PartMat.Of(brand, smoothness: 0.15f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.15f);
            PartMat sideMat = PartMat.Of(side, smoothness: 0.10f);
            PartMat bowlMat = PartMat.Of(bowl, smoothness: 0.35f);
            PartMat flakeMat = PartMat.Of(flake, smoothness: 0.10f);

            const float w = 0.22f;    // carton width (broad printed face)
            const float h = 0.32f;    // carton height (tall)
            const float d = 0.075f;   // carton depth (a thin box standing on end)
            const float half = h * 0.5f;

            // Printed cardboard carton body.
            Block(root, "Body", Vector3.zero, new Vector3(w, h, d), cardMat);

            // Folded top flaps: a slightly inset, shaded rim across the top edge.
            Block(root, "Flaps", new Vector3(0f, half - 0.004f, 0f), new Vector3(w - 0.01f, 0.02f, d - 0.006f), flapMat);

            // Pale nutrition-information panels standing a hair proud of each narrow side.
            float sideX = w * 0.5f + 0.003f;
            Block(root, "SidePanelRight", new Vector3(sideX, -0.01f, 0f), new Vector3(0.006f, h * 0.82f, d * 0.7f), sideMat);
            Block(root, "SidePanelLeft", new Vector3(-sideX, -0.01f, 0f), new Vector3(0.006f, h * 0.82f, d * 0.7f), sideMat);

            // Front and back printing: a cream brand band near the top with a darker stripe beneath it.
            float faceZ = d * 0.5f + 0.004f;
            Block(root, "BrandFront", new Vector3(0f, half - 0.06f, faceZ), new Vector3(w * 0.88f, 0.06f, 0.006f), brandMat);
            Block(root, "BrandBack", new Vector3(0f, half - 0.06f, -faceZ), new Vector3(w * 0.88f, 0.06f, 0.006f), brandMat);
            Block(root, "BrandStripeFront", new Vector3(0f, half - 0.10f, faceZ + 0.001f), new Vector3(w * 0.88f, 0.014f, 0.006f), trimMat);
            Block(root, "BrandStripeBack", new Vector3(0f, half - 0.10f, -(faceZ + 0.001f)), new Vector3(w * 0.88f, 0.014f, 0.006f), trimMat);

            // A printed "serving suggestion" low on the front face: a shallow white bowl heaped with toasted
            // flakes, standing a hair proud of the carton like the rest of the artwork.
            Sphere(root, "Bowl", new Vector3(0f, -0.05f, faceZ), Vector3.zero, new Vector3(0.13f, 0.075f, 0.02f), bowlMat);
            Sphere(root, "Flakes", new Vector3(0f, -0.035f, faceZ + 0.004f), Vector3.zero, new Vector3(0.11f, 0.05f, 0.02f), flakeMat);

            // A couple of stray flakes tumbling beside the bowl.
            Block(root, "Flake1", new Vector3(-0.078f, -0.055f, faceZ + 0.003f), new Vector3(0.02f, 0.02f, 0.005f), flakeMat).localEulerAngles = new Vector3(0f, 0f, 30f);
            Block(root, "Flake2", new Vector3(0.08f, -0.07f, faceZ + 0.003f), new Vector3(0.018f, 0.018f, 0.005f), flakeMat).localEulerAngles = new Vector3(0f, 0f, 18f);

            return root;
        }

        /// <summary>
        /// A piece of fresh fruit: a rounded, waxy body — a main sphere fattened by a fuller shoulder set a
        /// little high, so it is widest above the middle and dimples at the top — with shadowed wells at the
        /// stem and blossom ends, a short woody stalk leaning out of the top, and a single tilted leaf. The
        /// body takes the item's tint, so the same model serves a red or a green apple where it lies.
        /// </summary>
        private static PartNode BuildApple(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the rounded fruit, keeping its base just off the ground like its siblings.
            baseHeight = 0.24f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                          // apple skin, red or green per the item's tint
            Color stalk = new Color(0.36f, 0.24f, 0.13f);   // woody brown stem
            Color well = new Color(0.24f, 0.14f, 0.08f);    // shadowed stem well and blossom end
            Color leaf = new Color(0.36f, 0.55f, 0.22f);    // green leaf

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.5f);     // waxy fruit sheen
            PartMat stalkMat = PartMat.Of(stalk, smoothness: 0.15f);
            PartMat wellMat = PartMat.Of(well, smoothness: 0.1f);
            PartMat leafMat = PartMat.Of(leaf, smoothness: 0.2f);

            // Rounded body: a main sphere with a fuller, flattened shoulder sphere set slightly high, so the
            // fruit swells above the middle and pinches into a shallow dimple at the top.
            Sphere(root, "Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.30f, 0.285f, 0.30f), skinMat);
            Sphere(root, "Shoulders", new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.31f, 0.185f, 0.31f), skinMat);

            // Shadowed wells nestled into the stem end and the blossom end.
            Sphere(root, "StemWell", new Vector3(0f, 0.11f, 0f), Vector3.zero, new Vector3(0.08f, 0.05f, 0.08f), wellMat);
            Sphere(root, "BlossomEnd", new Vector3(0f, -0.135f, 0f), Vector3.zero, new Vector3(0.055f, 0.04f, 0.055f), wellMat);

            // Woody stalk rising out of the stem well, leaning a touch for a natural look.
            Rod(root, "Stem", new Vector3(0f, 0.185f, 0.006f), new Vector3(6f, 0f, 5f), 0.022f, 0.11f, stalkMat);

            // A single small leaf, tilted up and out beside the top of the stalk.
            Sphere(root, "Leaf", new Vector3(0.062f, 0.20f, -0.008f), new Vector3(8f, 22f, 34f), new Vector3(0.12f, 0.02f, 0.055f), leafMat);

            return root;
        }

        /// <summary>
        /// A round head of cabbage: a dense pale-green ball of tightly furled inner leaves, wrapped by a
        /// looser rosette of darker, cupped outer leaves, with pale midribs fanning up from a trimmed core
        /// stub at the base. The tight head takes the item's tint so it reads as green cabbage where it lies.
        /// </summary>
        private static PartNode BuildCabbage(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the rounded head, keeping its base floating just off the ground like the apple.
            baseHeight = 0.24f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color inner = def.Tint;                                                   // pale inner-leaf green
            Color outer = Color.Lerp(inner, new Color(0.24f, 0.42f, 0.30f), 0.6f);    // darker, bluer wrapper leaves
            Color mid = Color.Lerp(inner, outer, 0.5f);                               // leaves between the two tones
            Color vein = new Color(0.84f, 0.87f, 0.63f);                              // pale rib / midvein
            Color core = new Color(0.86f, 0.84f, 0.66f);                              // trimmed straw-coloured stem

            PartMat innerMat = PartMat.Of(inner, smoothness: 0.28f);   // faint waxy leaf sheen
            PartMat outerMat = PartMat.Of(outer, smoothness: 0.24f);
            PartMat midMat = PartMat.Of(mid, smoothness: 0.26f);
            PartMat veinMat = PartMat.Of(vein, smoothness: 0.15f);
            PartMat coreMat = PartMat.Of(core, smoothness: 0.12f);

            // Dense inner head: a slightly flattened ball capped by a tighter crown so the top reads as a
            // furled centre rather than a smooth sphere.
            Sphere(root, "Head", new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.33f, 0.32f, 0.33f), innerMat);
            Sphere(root, "Crown", new Vector3(0f, 0.10f, 0f), Vector3.zero, new Vector3(0.24f, 0.17f, 0.24f), innerMat);

            // Loose outer leaves: six cupped, flattened discs flaring up and out around the lower flanks, each
            // yawed to its own slice of the head and tilted so it wraps the ball.
            Sphere(root, "Leaf1", new Vector3(0f, -0.03f, 0.11f), new Vector3(52f, 0f, 0f), new Vector3(0.24f, 0.11f, 0.21f), outerMat);
            Sphere(root, "Leaf2", new Vector3(0.095f, -0.04f, 0.055f), new Vector3(52f, 60f, 0f), new Vector3(0.23f, 0.10f, 0.21f), outerMat);
            Sphere(root, "Leaf3", new Vector3(0.095f, -0.02f, -0.055f), new Vector3(50f, 120f, 0f), new Vector3(0.24f, 0.11f, 0.20f), outerMat);
            Sphere(root, "Leaf4", new Vector3(0f, -0.05f, -0.11f), new Vector3(52f, 180f, 0f), new Vector3(0.23f, 0.10f, 0.21f), outerMat);
            Sphere(root, "Leaf5", new Vector3(-0.095f, -0.03f, -0.055f), new Vector3(50f, 240f, 0f), new Vector3(0.24f, 0.11f, 0.20f), outerMat);
            Sphere(root, "Leaf6", new Vector3(-0.095f, -0.045f, 0.055f), new Vector3(52f, 300f, 0f), new Vector3(0.23f, 0.10f, 0.21f), outerMat);

            // A tighter ring of mid-tone leaves higher up, offset between the outer ones, to build the layered look.
            Sphere(root, "MidLeaf1", new Vector3(0.05f, 0.035f, 0.087f), new Vector3(38f, 30f, 0f), new Vector3(0.20f, 0.09f, 0.18f), midMat);
            Sphere(root, "MidLeaf2", new Vector3(0.087f, 0.045f, -0.05f), new Vector3(38f, 120f, 0f), new Vector3(0.20f, 0.09f, 0.18f), midMat);
            Sphere(root, "MidLeaf3", new Vector3(-0.05f, 0.035f, -0.087f), new Vector3(38f, 210f, 0f), new Vector3(0.20f, 0.09f, 0.18f), midMat);
            Sphere(root, "MidLeaf4", new Vector3(-0.087f, 0.04f, 0.05f), new Vector3(38f, 300f, 0f), new Vector3(0.20f, 0.09f, 0.18f), midMat);

            // Pale midribs fanning up from the core, peeking between the outer leaves like the thick white veins.
            Rod(root, "Rib1", new Vector3(0f, -0.06f, 0.12f), new Vector3(62f, 0f, 0f), 0.02f, 0.14f, veinMat);
            Rod(root, "Rib2", new Vector3(0.114f, -0.07f, 0.037f), new Vector3(62f, 72f, 0f), 0.02f, 0.13f, veinMat);
            Rod(root, "Rib3", new Vector3(0.07f, -0.065f, -0.097f), new Vector3(60f, 144f, 0f), 0.02f, 0.13f, veinMat);
            Rod(root, "Rib4", new Vector3(-0.07f, -0.07f, -0.097f), new Vector3(60f, 216f, 0f), 0.02f, 0.13f, veinMat);
            Rod(root, "Rib5", new Vector3(-0.114f, -0.065f, 0.037f), new Vector3(62f, 288f, 0f), 0.02f, 0.13f, veinMat);

            // Trimmed core: a short pale stem stub cut flat at the base, the way a picked cabbage is left.
            Cylinder(root, "Core", -0.15f, 0.055f, 0.08f, coreMat);
            Sphere(root, "CoreBase", new Vector3(0f, -0.115f, 0f), Vector3.zero, new Vector3(0.12f, 0.06f, 0.12f), coreMat);

            return root;
        }

        /// <summary>
        /// A ripe tomato: a glossy, slightly flattened red globe with swollen shoulders sinking into a dimple
        /// at the top, crowned by a green star of curling calyx sepals around a short cut stem, and a pale
        /// blossom scar underneath. The skin takes the item's tint so it reads as a red tomato where it lies.
        /// </summary>
        private static PartNode BuildTomato(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height for the squat fruit, keeping its base floating just off the ground like the apple.
            baseHeight = 0.22f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                                                    // ripe tomato red
            Color deep = new Color(skin.r * 0.80f, skin.g * 0.76f, skin.b * 0.76f);   // shaded underside
            Color calyx = new Color(0.33f, 0.48f, 0.19f);                             // green sepal star
            Color stem = new Color(0.31f, 0.40f, 0.17f);                              // woody green stem
            Color well = new Color(0.26f, 0.31f, 0.15f);                              // shaded dimple under the calyx
            Color scar = new Color(0.80f, 0.74f, 0.58f);                              // pale blossom scar beneath

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.55f);   // wet, glossy fruit sheen
            PartMat deepMat = PartMat.Of(deep, smoothness: 0.5f);
            PartMat calyxMat = PartMat.Of(calyx, smoothness: 0.22f);
            PartMat stemMat = PartMat.Of(stem, smoothness: 0.18f);
            PartMat wellMat = PartMat.Of(well, smoothness: 0.12f);
            PartMat scarMat = PartMat.Of(scar, smoothness: 0.1f);

            // Squat, glossy body: a wide flattened globe with swollen shoulders up top and a fuller belly below,
            // so the fruit bulges around the middle the way a real tomato does rather than reading as a ball.
            Sphere(root, "Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.34f, 0.26f, 0.34f), skinMat);
            Sphere(root, "Shoulders", new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.345f, 0.17f, 0.345f), skinMat);

            // Darker belly peeking out beneath the body, grounding the fruit with a shaded underside.
            Sphere(root, "Belly", new Vector3(0f, -0.04f, 0f), Vector3.zero, new Vector3(0.33f, 0.22f, 0.33f), deepMat);

            // Pale blossom scar dimpling the very bottom, opposite the stem.
            Sphere(root, "BlossomScar", new Vector3(0f, -0.128f, 0f), Vector3.zero, new Vector3(0.05f, 0.028f, 0.05f), scarMat);

            // Shaded dimple at the top where the skin sinks in around the calyx.
            Sphere(root, "StemWell", new Vector3(0f, 0.105f, 0f), Vector3.zero, new Vector3(0.13f, 0.06f, 0.13f), wellMat);

            // Green calyx: a six-point star of slim sepals fanning out from the dimple and lifting a little over
            // the shoulders, the way the crown clings to a fresh-picked tomato.
            const int sepalCount = 6;
            for (int i = 0; i < sepalCount; i++)
            {
                float deg = i * (360f / sepalCount);
                float rad = deg * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * 0.03f, 0.115f, -Mathf.Sin(rad) * 0.03f);
                Sphere(root, "Sepal" + (i + 1), pos, new Vector3(0f, deg, 14f), new Vector3(0.16f, 0.024f, 0.055f), calyxMat);
            }

            // Short cut stem rising from the centre of the calyx, with a knobby severed tip.
            Rod(root, "Stem", new Vector3(0f, 0.15f, 0f), new Vector3(4f, 0f, 3f), 0.03f, 0.075f, stemMat);
            Sphere(root, "StemTip", new Vector3(0.005f, 0.192f, 0.004f), Vector3.zero, new Vector3(0.042f, 0.03f, 0.042f), stemMat);

            return root;
        }

        /// <summary>
        /// A whole carrot lying on its side: a long orange root that tapers from a broad, faintly green
        /// shoulder down to a fine point, banded with a few darker circumferential grooves and trailing a
        /// pale taproot and a couple of fibrous rootlets, crowned at the wide end by a bushy spray of
        /// feathery green tops. The root takes the item's tint so it reads as an orange carrot where it lies.
        /// </summary>
        private static PartNode BuildCarrot(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for a slender root resting on its side, like the potato and loaf beside it.
            baseHeight = 0.14f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                                                    // orange carrot root
            Color deep = new Color(skin.r * 0.80f, skin.g * 0.78f, skin.b * 0.72f);   // shaded groove rings
            Color collar = new Color(0.44f, 0.46f, 0.22f);                            // green-tinged shoulder
            Color stalk = new Color(0.30f, 0.42f, 0.16f);                             // dark leaf stalks
            Color frond = new Color(0.42f, 0.56f, 0.24f);                             // bright feathery foliage
            Color taproot = new Color(0.87f, 0.76f, 0.55f);                           // pale fine root hairs

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.28f);   // fresh root, faint waxy sheen
            PartMat deepMat = PartMat.Of(deep, smoothness: 0.2f);
            PartMat collarMat = PartMat.Of(collar, smoothness: 0.2f);
            PartMat stalkMat = PartMat.Of(stalk, smoothness: 0.2f);
            PartMat frondMat = PartMat.Of(frond, smoothness: 0.25f);
            PartMat taprootMat = PartMat.Of(taproot, smoothness: 0.1f);

            // Tapered root laid along X: a chain of overlapping round segments shrinking from the broad crown
            // at +X to a fine point at -X, each lifted a touch more than the last so the carrot curves gently
            // upward toward its tip the way a fresh-pulled root does.
            Sphere(root, "Crown", new Vector3(0.155f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.115f, 0.115f), skinMat);
            Sphere(root, "Seg1", new Vector3(0.105f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.108f, 0.108f), skinMat);
            Sphere(root, "Seg2", new Vector3(0.055f, 0.002f, 0f), Vector3.zero, new Vector3(0.11f, 0.097f, 0.097f), skinMat);
            Sphere(root, "Seg3", new Vector3(0.005f, 0.006f, 0f), Vector3.zero, new Vector3(0.11f, 0.084f, 0.084f), skinMat);
            Sphere(root, "Seg4", new Vector3(-0.05f, 0.012f, 0f), Vector3.zero, new Vector3(0.11f, 0.068f, 0.068f), skinMat);
            Sphere(root, "Seg5", new Vector3(-0.10f, 0.02f, 0f), Vector3.zero, new Vector3(0.10f, 0.05f, 0.05f), skinMat);
            Sphere(root, "Seg6", new Vector3(-0.15f, 0.03f, 0f), Vector3.zero, new Vector3(0.09f, 0.032f, 0.032f), skinMat);
            Sphere(root, "Tip", new Vector3(-0.195f, 0.04f, 0f), Vector3.zero, new Vector3(0.075f, 0.016f, 0.016f), skinMat);

            // Darker rings banding the root — the faint circumferential grooves a carrot grows in. A cylinder
            // laid along X (the 90-degree roll on Z) makes a thin disc that stands a hair proud of the body.
            Rod(root, "Groove1", new Vector3(0.10f, 0.001f, 0f), new Vector3(0f, 0f, 90f), 0.108f, 0.014f, deepMat);
            Rod(root, "Groove2", new Vector3(0.04f, 0.004f, 0f), new Vector3(0f, 0f, 90f), 0.093f, 0.013f, deepMat);
            Rod(root, "Groove3", new Vector3(-0.02f, 0.009f, 0f), new Vector3(0f, 0f, 90f), 0.079f, 0.012f, deepMat);
            Rod(root, "Groove4", new Vector3(-0.08f, 0.016f, 0f), new Vector3(0f, 0f, 90f), 0.058f, 0.011f, deepMat);

            // Green-tinged shoulder collar capping the broad end, where the leaves sprout from the root.
            Sphere(root, "Collar", new Vector3(0.20f, 0.02f, 0f), Vector3.zero, new Vector3(0.078f, 0.075f, 0.078f), collarMat);

            // Pale fine taproot trailing off the pointed end, plus a couple of wispy rootlets on the underside.
            Rod(root, "TapRoot", new Vector3(-0.225f, 0.045f, 0f), new Vector3(0f, 0f, 78f), 0.007f, 0.06f, taprootMat);
            Rod(root, "Rootlet1", new Vector3(-0.02f, -0.03f, 0.03f), new Vector3(20f, 30f, 60f), 0.005f, 0.03f, taprootMat);
            Rod(root, "Rootlet2", new Vector3(-0.09f, -0.02f, -0.02f), new Vector3(-20f, -20f, 70f), 0.005f, 0.025f, taprootMat);

            // Leaf stalks springing up and out from the shoulder, kept near-vertical so they read as a rising
            // clump rather than splaying flat.
            Rod(root, "Stalk1", new Vector3(0.20f, 0.06f, 0f), new Vector3(0f, 0f, -25f), 0.012f, 0.14f, stalkMat);
            Rod(root, "Stalk2", new Vector3(0.205f, 0.06f, 0.02f), new Vector3(-18f, 0f, -22f), 0.011f, 0.13f, stalkMat);
            Rod(root, "Stalk3", new Vector3(0.205f, 0.06f, -0.02f), new Vector3(18f, 0f, -22f), 0.011f, 0.13f, stalkMat);
            Rod(root, "Stalk4", new Vector3(0.195f, 0.06f, 0.035f), new Vector3(0f, 0f, -12f), 0.01f, 0.12f, stalkMat);
            Rod(root, "Stalk5", new Vector3(0.195f, 0.06f, -0.035f), new Vector3(0f, 0f, -12f), 0.01f, 0.12f, stalkMat);
            Rod(root, "Stalk6", new Vector3(0.205f, 0.07f, 0f), new Vector3(0f, 0f, -8f), 0.012f, 0.15f, stalkMat);

            // Bushy, feathery foliage: a loose dome of squashed leaf clumps heaped up and forward of the crown,
            // with a couple of small extra tufts for a frilly, carrot-top silhouette.
            Sphere(root, "Frond1", new Vector3(0.27f, 0.17f, 0f), new Vector3(0f, 0f, 15f), new Vector3(0.10f, 0.07f, 0.10f), frondMat);
            Sphere(root, "Frond2", new Vector3(0.25f, 0.15f, 0.055f), new Vector3(12f, 18f, 8f), new Vector3(0.085f, 0.06f, 0.085f), frondMat);
            Sphere(root, "Frond3", new Vector3(0.25f, 0.15f, -0.055f), new Vector3(-12f, -18f, 8f), new Vector3(0.085f, 0.06f, 0.085f), frondMat);
            Sphere(root, "Frond4", new Vector3(0.285f, 0.195f, 0.03f), new Vector3(0f, 12f, 22f), new Vector3(0.08f, 0.055f, 0.08f), frondMat);
            Sphere(root, "Frond5", new Vector3(0.285f, 0.195f, -0.03f), new Vector3(0f, -12f, 22f), new Vector3(0.08f, 0.055f, 0.08f), frondMat);
            Sphere(root, "Frond6", new Vector3(0.30f, 0.215f, 0f), new Vector3(0f, 0f, 10f), new Vector3(0.075f, 0.05f, 0.075f), frondMat);
            Sphere(root, "Frond7", new Vector3(0.235f, 0.135f, 0f), Vector3.zero, new Vector3(0.075f, 0.05f, 0.075f), frondMat);

            return root;
        }

        /// <summary>
        /// A crusty loaf of bread: straight risen sides under a domed, overhanging "mushroom" top, a darker
        /// baked underside, and a few pale slashes scored diagonally across the crown where the crust split and
        /// bloomed in the oven. The crust takes the item's tint so the loaf reads as golden bread where it lies.
        /// </summary>
        private static PartNode BuildBreadLoaf(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover height, keeping the loaf's flat base floating just off the ground like its siblings.
            baseHeight = 0.16f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color crust = def.Tint;                                                          // golden baked crust
            Color baseCrust = new Color(crust.r * 0.70f, crust.g * 0.62f, crust.b * 0.52f);  // darker baked underside
            Color slash = new Color(0.95f, 0.86f, 0.64f);                                    // pale floury scored crumb

            PartMat crustMat = PartMat.Of(crust, smoothness: 0.08f);   // matte, barely-there sheen
            PartMat baseMat = PartMat.Of(baseCrust, smoothness: 0.06f);
            PartMat slashMat = PartMat.Of(slash, smoothness: 0.05f);

            const float len = 0.30f;    // loaf length (X)
            const float wid = 0.155f;   // loaf width (Z)

            // Straight-sided lower body — the part that rose against the tin.
            Block(root, "Body", new Vector3(0f, -0.02f, 0f), new Vector3(len, 0.12f, wid), crustMat);

            // Darker baked underside peeking out beneath the body.
            Block(root, "Base", new Vector3(0f, -0.075f, 0f), new Vector3(len - 0.01f, 0.03f, wid), baseMat);

            // Risen mushroom top: a squashed dome that overhangs the sides and rounds the upper corners.
            Sphere(root, "Crown", new Vector3(0f, 0.035f, 0f), Vector3.zero, new Vector3(len + 0.015f, 0.13f, wid + 0.02f), crustMat);

            // A few pale slashes scored diagonally across the crown, following the curve down towards the ends.
            Block(root, "Slash1", new Vector3(-0.08f, 0.078f, 0f), new Vector3(0.03f, 0.03f, 0.12f), slashMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "Slash2", new Vector3(0f, 0.088f, 0f), new Vector3(0.03f, 0.03f, 0.12f), slashMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "Slash3", new Vector3(0.08f, 0.078f, 0f), new Vector3(0.03f, 0.03f, 0.12f), slashMat).localEulerAngles = new Vector3(0f, 20f, 0f);

            return root;
        }

        /// <summary>
        /// A raw potato: a knobbly oblong tuber built from a fat central lobe crowded by smaller bumps at
        /// each end and along one side, so it reads as a lumpy hand-dug spud rather than a smooth egg. Matte
        /// earthy skin (the item's tint) is mottled with a couple of darker russet patches and dotted with
        /// the small sunken dark "eyes" a potato sprouts from, so it looks unmistakably like a potato where
        /// it lies on the floor.
        /// </summary>
        private static PartNode BuildPotato(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for an oblong tuber resting on its side, like the bread loaf beside it.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                                                       // earthy potato skin
            Color patch = new Color(skin.r * 0.78f, skin.g * 0.78f, skin.b * 0.74f);      // darker russet blemish
            Color eye = new Color(0.32f, 0.22f, 0.12f);                                   // dark sunken sprouting eye

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.12f);   // matte, un-waxy earthy skin
            PartMat patchMat = PartMat.Of(patch, smoothness: 0.1f);
            PartMat eyeMat = PartMat.Of(eye, smoothness: 0.08f);

            // Oblong body: a stretched central lobe fattened by overlapping bumps at each end and on one side,
            // so the outline is lumpy and irregular the way a dug-up potato is, not a tidy ellipsoid.
            Sphere(root, "Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.34f, 0.24f, 0.26f), skinMat);
            Sphere(root, "EndFat", new Vector3(0.13f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.19f, 0.20f), skinMat);
            Sphere(root, "EndNarrow", new Vector3(-0.14f, -0.005f, 0.015f), Vector3.zero, new Vector3(0.13f, 0.16f, 0.17f), skinMat);
            Sphere(root, "Knuckle", new Vector3(0f, 0.075f, -0.03f), Vector3.zero, new Vector3(0.15f, 0.11f, 0.14f), skinMat);
            Sphere(root, "SideBulge", new Vector3(-0.03f, -0.015f, 0.10f), Vector3.zero, new Vector3(0.14f, 0.13f, 0.11f), skinMat);

            // Darker russet patches mottling the skin.
            Sphere(root, "Patch1", new Vector3(0.07f, 0.085f, 0.05f), Vector3.zero, new Vector3(0.10f, 0.05f, 0.09f), patchMat);
            Sphere(root, "Patch2", new Vector3(-0.10f, 0.045f, -0.05f), Vector3.zero, new Vector3(0.08f, 0.045f, 0.07f), patchMat);

            // Scattered dark eyes sunk a hair into the skin — the little pits a potato sprouts from.
            Sphere(root, "Eye1", new Vector3(0.09f, 0.095f, 0.02f), Vector3.zero, new Vector3(0.030f, 0.022f, 0.030f), eyeMat);
            Sphere(root, "Eye2", new Vector3(-0.04f, 0.075f, 0.075f), Vector3.zero, new Vector3(0.026f, 0.020f, 0.026f), eyeMat);
            Sphere(root, "Eye3", new Vector3(0.01f, 0.055f, -0.10f), Vector3.zero, new Vector3(0.028f, 0.022f, 0.028f), eyeMat);
            Sphere(root, "Eye4", new Vector3(-0.13f, 0.02f, 0.02f), Vector3.zero, new Vector3(0.024f, 0.018f, 0.024f), eyeMat);
            Sphere(root, "Eye5", new Vector3(0.17f, -0.01f, 0.0f), Vector3.zero, new Vector3(0.022f, 0.018f, 0.022f), eyeMat);

            return root;
        }

        /// <summary>
        /// A baked jacket potato: the same oblong tuber, now oven-darkened (the item's cooked, browner tint)
        /// with a crisped underside, split open along the top so a mound of fluffy pale flesh billows out
        /// between the curled-back edges of the skin, finished with a melting pat of butter. The split and
        /// the pale flesh read it apart from the raw spud at a glance where it lies on the floor.
        /// </summary>
        private static PartNode BuildBakedPotato(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover matching the raw potato it was baked from.
            baseHeight = 0.19f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                                                       // oven-darkened jacket
            Color crisp = new Color(skin.r * 0.72f, skin.g * 0.72f, skin.b * 0.68f);      // charred, crisped underside
            Color flesh = new Color(0.95f, 0.89f, 0.73f);                                // fluffy pale interior
            Color fleshShade = new Color(0.86f, 0.78f, 0.60f);                           // shadowed flesh in the split
            Color butter = new Color(0.96f, 0.82f, 0.32f);                               // melting yellow butter

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.22f);     // baked, faintly oiled jacket
            PartMat crispMat = PartMat.Of(crisp, smoothness: 0.15f);
            PartMat fleshMat = PartMat.Of(flesh, smoothness: 0.08f);   // matte, fluffy crumb
            PartMat fleshShadeMat = PartMat.Of(fleshShade, smoothness: 0.08f);
            PartMat butterMat = PartMat.Of(butter, smoothness: 0.55f); // glossy melting pat

            // Oblong baked body, plumped a little from the oven, with fatter and narrower ends.
            Sphere(root, "Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.34f, 0.24f, 0.26f), skinMat);
            Sphere(root, "EndFat", new Vector3(0.13f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.19f, 0.20f), skinMat);
            Sphere(root, "EndNarrow", new Vector3(-0.13f, -0.005f, 0f), Vector3.zero, new Vector3(0.14f, 0.17f, 0.18f), skinMat);

            // Crisped, slightly charred underside where it sat on the oven rack.
            Sphere(root, "CrispBase", new Vector3(0f, -0.075f, 0f), Vector3.zero, new Vector3(0.30f, 0.11f, 0.22f), crispMat);

            // Shadowed flesh filling the cut, then the fluffy pale mound billowing up out of the split.
            Sphere(root, "FleshBed", new Vector3(0f, 0.075f, 0f), Vector3.zero, new Vector3(0.22f, 0.08f, 0.11f), fleshShadeMat);
            Sphere(root, "Flesh", new Vector3(0f, 0.105f, 0f), Vector3.zero, new Vector3(0.22f, 0.10f, 0.11f), fleshMat);
            Sphere(root, "FleshFluff1", new Vector3(0.05f, 0.135f, 0.01f), Vector3.zero, new Vector3(0.10f, 0.06f, 0.07f), fleshMat);
            Sphere(root, "FleshFluff2", new Vector3(-0.05f, 0.125f, -0.01f), Vector3.zero, new Vector3(0.09f, 0.055f, 0.06f), fleshMat);
            Sphere(root, "FleshFluff3", new Vector3(0f, 0.13f, 0.03f), Vector3.zero, new Vector3(0.07f, 0.05f, 0.05f), fleshMat);

            // Dark skin split back on either side of the opening, curling up like a burst jacket.
            Sphere(root, "SkinCurlL", new Vector3(0f, 0.10f, 0.075f), new Vector3(18f, 0f, 0f), new Vector3(0.26f, 0.06f, 0.05f), skinMat);
            Sphere(root, "SkinCurlR", new Vector3(0f, 0.10f, -0.075f), new Vector3(-18f, 0f, 0f), new Vector3(0.26f, 0.06f, 0.05f), skinMat);

            // A pat of butter melting into the fluffy flesh on top.
            Block(root, "Butter", new Vector3(0.015f, 0.17f, 0f), new Vector3(0.06f, 0.02f, 0.05f), butterMat).localEulerAngles = new Vector3(0f, 0f, 8f);

            return root;
        }

        /// <summary>
        /// A raw cut of red meat on the bone: an irregular, glistening slab of muscle built from a few
        /// overlapping rounded lobes, wrapped along one edge by a creamy fat cap, streaked with pale
        /// marbling across the top, and finished with a stub of bone and a pair of rounded condyle knobs
        /// poking from one end so it reads unmistakably as a fresh chop where it lies. The muscle takes the
        /// item's tint so it stays raw-meat red on the floor.
        /// </summary>
        private static PartNode BuildRawMeat(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for a thick cut lying flat on the ground, like the bread loaf beside it.
            baseHeight = 0.15f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color meat = def.Tint;                                                          // raw red muscle
            Color meatDeep = new Color(meat.r * 0.70f, meat.g * 0.58f, meat.b * 0.58f);      // shadowed underside
            Color fat = new Color(0.92f, 0.86f, 0.73f);                                     // creamy fat cap
            Color marble = new Color(0.95f, 0.90f, 0.80f);                                  // pale marbling streaks
            Color bone = new Color(0.93f, 0.90f, 0.82f);                                    // off-white bone

            PartMat meatMat = PartMat.Of(meat, smoothness: 0.45f);       // wet, glistening raw sheen
            PartMat meatDeepMat = PartMat.Of(meatDeep, smoothness: 0.35f);
            PartMat fatMat = PartMat.Of(fat, smoothness: 0.3f);
            PartMat marbleMat = PartMat.Of(marble, smoothness: 0.3f);
            PartMat boneMat = PartMat.Of(bone, smoothness: 0.2f);

            // Irregular slab of muscle: a main flattened lobe crowded by two smaller ones so the cut looks
            // hand-butchered rather than a neat disc, leaning right to leave room for the bone on the left.
            Sphere(root, "Body", new Vector3(0.03f, 0f, 0f), Vector3.zero, new Vector3(0.30f, 0.15f, 0.26f), meatMat);
            Sphere(root, "Lobe1", new Vector3(0.10f, 0.006f, 0.045f), Vector3.zero, new Vector3(0.18f, 0.14f, 0.19f), meatMat);
            Sphere(root, "Lobe2", new Vector3(-0.02f, 0.002f, -0.05f), Vector3.zero, new Vector3(0.17f, 0.13f, 0.16f), meatMat);

            // Darker, shadowed underside so the cut reads with depth as it bobs and spins.
            Sphere(root, "Underside", new Vector3(0.03f, -0.05f, 0f), Vector3.zero, new Vector3(0.28f, 0.08f, 0.24f), meatDeepMat);

            // Creamy fat cap wrapping the outer long edge of the cut.
            Sphere(root, "FatCap", new Vector3(0.05f, 0.012f, 0.125f), Vector3.zero, new Vector3(0.26f, 0.10f, 0.07f), fatMat);

            // Pale marbling streaked across the top surface, standing a hair proud of the muscle and tilted
            // for a natural, grainy look.
            Block(root, "Marble1", new Vector3(0.02f, 0.074f, 0.01f), new Vector3(0.17f, 0.012f, 0.02f), marbleMat).localEulerAngles = new Vector3(0f, 24f, 0f);
            Block(root, "Marble2", new Vector3(0.08f, 0.078f, -0.035f), new Vector3(0.11f, 0.012f, 0.018f), marbleMat).localEulerAngles = new Vector3(0f, -16f, 0f);
            Block(root, "Marble3", new Vector3(-0.01f, 0.072f, 0.055f), new Vector3(0.10f, 0.01f, 0.016f), marbleMat).localEulerAngles = new Vector3(0f, 10f, 0f);

            // A stub of bone poking from the left end, capped by a pair of rounded condyle knobs so the cut
            // reads clearly as being on the bone. The bone lies along X (a Unity cylinder points up Y, so the
            // 90° roll on Z lays it on its side); its right end tucks up inside the muscle.
            Rod(root, "Bone", new Vector3(-0.15f, 0f, -0.01f), new Vector3(0f, 0f, 90f), 0.05f, 0.16f, boneMat);
            Sphere(root, "Knuckle1", new Vector3(-0.235f, 0.014f, 0.03f), Vector3.zero, new Vector3(0.062f, 0.062f, 0.058f), boneMat);
            Sphere(root, "Knuckle2", new Vector3(-0.235f, 0.014f, -0.03f), Vector3.zero, new Vector3(0.062f, 0.062f, 0.058f), boneMat);

            return root;
        }

        /// <summary>
        /// A cooked cut of meat on the bone: the same hand-butchered chop as its raw counterpart, now
        /// seared to a rich brown with a darker roasted crust, criss-crossed by charred grill marks across
        /// the top, its fat cap rendered golden and its bone browned at the joint. The muscle takes the
        /// item's (cooked, brown) tint so it reads as a hot, ready-to-eat chop where it lies on the floor.
        /// </summary>
        private static PartNode BuildCookedMeat(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for a thick cut lying flat on the ground, matching the raw chop it cooks from.
            baseHeight = 0.15f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color meat = def.Tint;                                                          // browned cooked muscle
            Color meatDeep = new Color(meat.r * 0.68f, meat.g * 0.60f, meat.b * 0.58f);      // shadowed underside
            Color sear = new Color(meat.r * 0.66f, meat.g * 0.54f, meat.b * 0.46f);          // darker roasted crust
            Color grill = new Color(0.16f, 0.11f, 0.08f);                                   // charred grill marks
            Color fat = new Color(0.85f, 0.71f, 0.48f);                                     // rendered golden fat
            Color bone = new Color(0.91f, 0.86f, 0.75f);                                    // heat-browned bone

            PartMat meatMat = PartMat.Of(meat, smoothness: 0.5f);        // juicy, glazed cooked sheen
            PartMat meatDeepMat = PartMat.Of(meatDeep, smoothness: 0.4f);
            PartMat searMat = PartMat.Of(sear, smoothness: 0.4f);
            PartMat grillMat = PartMat.Of(grill, smoothness: 0.15f);     // matte char
            PartMat fatMat = PartMat.Of(fat, smoothness: 0.45f);         // glistening rendered fat
            PartMat boneMat = PartMat.Of(bone, smoothness: 0.2f);

            // Irregular slab of muscle: the same hand-butchered lobes as the raw cut, leaning right to leave
            // room for the bone on the left, so cooking a chop keeps its familiar shape.
            Sphere(root, "Body", new Vector3(0.03f, 0f, 0f), Vector3.zero, new Vector3(0.30f, 0.15f, 0.26f), meatMat);
            Sphere(root, "Lobe1", new Vector3(0.10f, 0.006f, 0.045f), Vector3.zero, new Vector3(0.18f, 0.14f, 0.19f), meatMat);
            Sphere(root, "Lobe2", new Vector3(-0.02f, 0.002f, -0.05f), Vector3.zero, new Vector3(0.17f, 0.13f, 0.16f), meatMat);

            // Darker, shadowed underside so the cut reads with depth as it bobs and spins.
            Sphere(root, "Underside", new Vector3(0.03f, -0.05f, 0f), Vector3.zero, new Vector3(0.28f, 0.08f, 0.24f), meatDeepMat);

            // A subtly darker roasted crust skimming the top surface, a hair proud of the muscle so the
            // seared face catches the light before the grill marks are laid over it.
            Sphere(root, "Sear", new Vector3(0.03f, 0.03f, 0f), Vector3.zero, new Vector3(0.27f, 0.09f, 0.23f), searMat);

            // Rendered golden fat cap wrapping the outer long edge of the cut — browned by the heat, not raw.
            Sphere(root, "FatCap", new Vector3(0.05f, 0.014f, 0.125f), Vector3.zero, new Vector3(0.26f, 0.10f, 0.07f), fatMat);

            // Charred grill marks criss-crossed across the top: three dark stripes running one way with two
            // laid perpendicular for the classic crosshatch of a seared chop. Both sets share the same tilt,
            // so the Z-running and X-running stripes stay 90 degrees apart; each stands a hair proud of the crust.
            Block(root, "GrillA1", new Vector3(-0.05f, 0.078f, 0f), new Vector3(0.015f, 0.01f, 0.19f), grillMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "GrillA2", new Vector3(0.03f, 0.082f, 0f), new Vector3(0.015f, 0.01f, 0.19f), grillMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "GrillA3", new Vector3(0.11f, 0.078f, 0.01f), new Vector3(0.015f, 0.01f, 0.15f), grillMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "GrillB1", new Vector3(0.0f, 0.08f, -0.06f), new Vector3(0.20f, 0.01f, 0.015f), grillMat).localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(root, "GrillB2", new Vector3(0.05f, 0.082f, 0.03f), new Vector3(0.20f, 0.01f, 0.015f), grillMat).localEulerAngles = new Vector3(0f, 20f, 0f);

            // A stub of bone poking from the left end, capped by a pair of rounded condyle knobs and browned
            // by the heat. The bone lies along X (a Unity cylinder points up Y, so the 90-degree roll on Z
            // lays it on its side); its right end tucks up inside the muscle.
            Rod(root, "Bone", new Vector3(-0.15f, 0f, -0.01f), new Vector3(0f, 0f, 90f), 0.05f, 0.16f, boneMat);
            Sphere(root, "Knuckle1", new Vector3(-0.235f, 0.014f, 0.03f), Vector3.zero, new Vector3(0.062f, 0.062f, 0.058f), boneMat);
            Sphere(root, "Knuckle2", new Vector3(-0.235f, 0.014f, -0.03f), Vector3.zero, new Vector3(0.062f, 0.062f, 0.058f), boneMat);

            return root;
        }

        /// <summary>
        /// A whole raw chicken, plucked and trussed the way it comes wrapped from the shop: a plump, breast-up
        /// carcass with a rounded rump, the two drumsticks tucked forward with their cut leg-bones poking past
        /// the front, folded wing nubs against the flanks, a small parson's-nose tail, and the dark hollow of the
        /// body cavity at the rear. The skin is drawn from the item's tint but lifted towards a pale, moist
        /// poultry pink so it reads as raw bird — not red beef — where it lies on the floor.
        /// </summary>
        private static PartNode BuildRawChicken(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre hover for a whole bird sitting breast-up, keeping its underside just off the ground.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = Color.Lerp(def.Tint, new Color(0.96f, 0.85f, 0.78f), 0.45f);   // pale, moist raw poultry flesh
            Color skinDeep = new Color(skin.r * 0.82f, skin.g * 0.80f, skin.b * 0.80f);  // shadowed belly and rear
            Color skinHi = Color.Lerp(skin, Color.white, 0.16f);                         // faint sheen along the breast
            Color cavity = new Color(0.44f, 0.25f, 0.25f);                               // dark hollow of the body cavity
            Color bone = new Color(0.93f, 0.90f, 0.82f);                                 // exposed, off-white leg-bone tips

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.35f);       // moist, faintly glistening raw skin
            PartMat skinDeepMat = PartMat.Of(skinDeep, smoothness: 0.3f);
            PartMat skinHiMat = PartMat.Of(skinHi, smoothness: 0.42f);
            PartMat cavityMat = PartMat.Of(cavity, smoothness: 0.15f);
            PartMat boneMat = PartMat.Of(bone, smoothness: 0.2f);

            BuildChickenCarcass(root, skinMat, skinDeepMat, skinHiMat, cavityMat, boneMat);

            return root;
        }

        /// <summary>
        /// A whole roasted chicken: the same trussed bird as its raw counterpart, now browned to the item's
        /// (cooked) tint with crisp golden skin drawn across the breast, darker roasted patches over the back and
        /// drumsticks, its cut leg-bones heat-browned, and loops of pale butcher's twine tying it up so it reads
        /// as a hot Sunday roast where it lies on the floor.
        /// </summary>
        private static PartNode BuildCookedChicken(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for a whole bird sitting breast-up, matching the raw chicken it roasts from.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color skin = def.Tint;                                                       // roasted golden-brown skin
            Color skinDeep = new Color(skin.r * 0.72f, skin.g * 0.66f, skin.b * 0.62f);  // shadowed belly and rear
            Color crisp = Color.Lerp(skin, new Color(0.95f, 0.78f, 0.45f), 0.5f);        // crisp, glazed skin highlight
            Color charr = new Color(skin.r * 0.5f, skin.g * 0.42f, skin.b * 0.36f);       // darker roasted patches
            Color cavity = new Color(0.34f, 0.22f, 0.16f);                              // browned hollow of the cavity
            Color bone = new Color(0.91f, 0.86f, 0.75f);                                // heat-browned leg-bone tips
            Color twine = new Color(0.86f, 0.80f, 0.66f);                               // pale butcher's twine

            PartMat skinMat = PartMat.Of(skin, smoothness: 0.5f);        // glazed roast sheen
            PartMat skinDeepMat = PartMat.Of(skinDeep, smoothness: 0.42f);
            PartMat crispMat = PartMat.Of(crisp, smoothness: 0.55f);
            PartMat charMat = PartMat.Of(charr, smoothness: 0.3f);
            PartMat cavityMat = PartMat.Of(cavity, smoothness: 0.2f);
            PartMat boneMat = PartMat.Of(bone, smoothness: 0.2f);
            PartMat twineMat = PartMat.Of(twine, smoothness: 0.1f);

            // Same trussed bird as the raw chicken, now with crisp golden skin drawn across the breast.
            BuildChickenCarcass(root, skinMat, skinDeepMat, crispMat, cavityMat, boneMat);

            // Darker roasted patches where the skin caught the most heat — over the back and each drumstick.
            Sphere(root, "CharBack", new Vector3(-0.12f, 0.12f, 0.02f), Vector3.zero, new Vector3(0.13f, 0.055f, 0.14f), charMat);
            Sphere(root, "CharDrumL", new Vector3(0.085f, -0.03f, 0.075f), Vector3.zero, new Vector3(0.09f, 0.06f, 0.085f), charMat);
            Sphere(root, "CharDrumR", new Vector3(0.085f, -0.03f, -0.075f), Vector3.zero, new Vector3(0.09f, 0.06f, 0.085f), charMat);

            // Butcher's twine: a tie across the drumstick ends and a pair of loops over the body.
            Block(root, "TwineTie", new Vector3(0.17f, -0.048f, 0f), new Vector3(0.02f, 0.02f, 0.16f), twineMat);
            Block(root, "TwineLoopFront", new Vector3(0.03f, 0.125f, 0f), new Vector3(0.02f, 0.02f, 0.28f), twineMat);
            Block(root, "TwineLoopRear", new Vector3(-0.07f, 0.115f, 0f), new Vector3(0.02f, 0.02f, 0.27f), twineMat);

            return root;
        }

        /// <summary>
        /// Builds the shared trussed-bird carcass — a breast-up body, rounded rump, raised breast, shadowed
        /// belly, hollow rear cavity and tail, plus the two tucked drumsticks with exposed leg-bone tips and
        /// folded wings — from the supplied material set, so the raw and roasted chickens are unmistakably the
        /// same bird in two states. Callers dress the surface on top (a raw sheen, or crisp skin, char and twine).
        /// </summary>
        private static void BuildChickenCarcass(PartNode root, PartMat skinMat, PartMat skinDeepMat,
            PartMat skinHiMat, PartMat cavityMat, PartMat boneMat)
        {
            // Plump, breast-up carcass: a main mass, a fuller rounded rump behind, a raised breast in front and
            // a shadowed belly beneath, so the bird reads as a rounded whole chicken rather than a flat cut.
            Sphere(root, "Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.36f, 0.23f, 0.26f), skinMat);
            Sphere(root, "Rump", new Vector3(-0.11f, 0.005f, 0f), Vector3.zero, new Vector3(0.20f, 0.20f, 0.235f), skinMat);
            Sphere(root, "Breast", new Vector3(0.05f, 0.075f, 0f), Vector3.zero, new Vector3(0.24f, 0.17f, 0.205f), skinMat);
            Sphere(root, "Belly", new Vector3(0f, -0.075f, 0f), Vector3.zero, new Vector3(0.30f, 0.11f, 0.22f), skinDeepMat);

            // Sheen (raw) or crisp skin (roasted) drawn across the top of the breast via the caller's highlight material.
            Sphere(root, "BreastTop", new Vector3(0.06f, 0.115f, 0f), Vector3.zero, new Vector3(0.20f, 0.075f, 0.16f), skinHiMat);

            // Dark hollow of the body cavity at the rear, with the little parson's-nose tail perched above it.
            Sphere(root, "Cavity", new Vector3(-0.205f, -0.005f, 0f), Vector3.zero, new Vector3(0.10f, 0.115f, 0.12f), cavityMat);
            Sphere(root, "Tail", new Vector3(-0.20f, 0.075f, 0f), Vector3.zero, new Vector3(0.075f, 0.07f, 0.08f), skinDeepMat);

            // Two drumsticks tucked forward and trussed: a meaty thigh at each rear flank tapering into a lower
            // drumstick, finished with a cut leg-bone poking past the front the way trussed birds are tied.
            Sphere(root, "ThighL", new Vector3(-0.05f, -0.04f, 0.115f), new Vector3(0f, 16f, 0f), new Vector3(0.20f, 0.15f, 0.13f), skinMat);
            Sphere(root, "ThighR", new Vector3(-0.05f, -0.04f, -0.115f), new Vector3(0f, -16f, 0f), new Vector3(0.20f, 0.15f, 0.13f), skinMat);
            Sphere(root, "DrumL", new Vector3(0.08f, -0.07f, 0.075f), Vector3.zero, new Vector3(0.12f, 0.10f, 0.10f), skinMat);
            Sphere(root, "DrumR", new Vector3(0.08f, -0.07f, -0.075f), Vector3.zero, new Vector3(0.12f, 0.10f, 0.10f), skinMat);

            // Exposed leg-bone tips (a Unity cylinder points up Y, so the 90-degree roll on Z lays each along X).
            Rod(root, "AnkleBoneL", new Vector3(0.185f, -0.05f, 0.05f), new Vector3(0f, 0f, 90f), 0.028f, 0.09f, boneMat);
            Rod(root, "AnkleBoneR", new Vector3(0.185f, -0.05f, -0.05f), new Vector3(0f, 0f, 90f), 0.028f, 0.09f, boneMat);

            // Folded wing nubs held against the front flanks, above the legs.
            Sphere(root, "WingL", new Vector3(0.095f, 0.025f, 0.12f), new Vector3(0f, -18f, 12f), new Vector3(0.12f, 0.075f, 0.085f), skinMat);
            Sphere(root, "WingR", new Vector3(0.095f, 0.025f, -0.12f), new Vector3(0f, 18f, -12f), new Vector3(0.12f, 0.075f, 0.085f), skinMat);
        }

        /// <summary>
        /// A disposable PET drinking-water bottle: a glossy blue water column ringed with grip flutes and a
        /// wrapped paper label, a clear plastic shoulder and threaded neck rising above the fill line, and a
        /// bold ribbed screw cap. The water body takes the item's tint so it reads as a water bottle where it
        /// lies on the floor, while the pale shoulder and neck sell the empty, see-through top of the bottle.
        /// </summary>
        private static PartNode BuildWaterBottle(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Tall, slim vessel — hover it high enough that the gentle bob never dips the base through the floor.
            baseHeight = 0.30f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color water = def.Tint;                                                     // blue water column
            Color rib = new Color(water.r * 0.82f, water.g * 0.82f, water.b * 0.82f);   // shaded grip flutes and foot
            Color clear = Color.Lerp(water, Color.white, 0.6f);                         // clear plastic above the fill line
            Color label = new Color(0.95f, 0.96f, 0.98f);                               // white paper label
            Color stripe = def.Tint;                                                    // printed brand band
            Color stripeDeep = new Color(0.10f, 0.34f, 0.68f);                          // deep-blue accent lines
            Color cap = new Color(0.13f, 0.40f, 0.78f);                                 // bold screw cap
            Color capRim = new Color(cap.r * 0.78f, cap.g * 0.78f, cap.b * 0.78f);      // knurled cap rim

            PartMat waterMat = PartMat.Of(water, smoothness: 0.82f, metallic: 0.05f);
            PartMat ribMat = PartMat.Of(rib, smoothness: 0.8f, metallic: 0.05f);
            PartMat clearMat = PartMat.Of(clear, smoothness: 0.88f, metallic: 0.05f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.25f);
            PartMat stripeMat = PartMat.Of(stripe, smoothness: 0.3f);
            PartMat stripeDeepMat = PartMat.Of(stripeDeep, smoothness: 0.3f);
            PartMat capMat = PartMat.Of(cap, smoothness: 0.4f);
            PartMat capRimMat = PartMat.Of(capRim, smoothness: 0.4f);

            const float bodyD = 0.15f;   // main body diameter

            // Water-filled body: a tall glossy column with a rounded base and a low standing foot ring.
            Cylinder(root, "Body", -0.05f, bodyD, 0.30f, waterMat);
            Sphere(root, "BaseRound", new Vector3(0f, -0.185f, 0f), Vector3.zero, new Vector3(0.152f, 0.08f, 0.152f), waterMat);
            Cylinder(root, "FootRing", -0.20f, 0.12f, 0.025f, ribMat);

            // Moulded grip flutes ringing the lower body, just below the label.
            Cylinder(root, "Rib1", -0.145f, bodyD + 0.006f, 0.012f, ribMat);
            Cylinder(root, "Rib2", -0.115f, bodyD + 0.006f, 0.012f, ribMat);

            // Wrapped paper label with a printed brand band, an accent line and matching edge trims.
            Cylinder(root, "Label", -0.02f, bodyD + 0.003f, 0.14f, labelMat);
            Cylinder(root, "LabelStripe", 0.005f, bodyD + 0.006f, 0.03f, stripeMat);
            Cylinder(root, "LabelAccent", -0.06f, bodyD + 0.006f, 0.016f, stripeDeepMat);
            Cylinder(root, "LabelTrimTop", 0.048f, bodyD + 0.005f, 0.008f, stripeDeepMat);
            Cylinder(root, "LabelTrimBottom", -0.088f, bodyD + 0.005f, 0.008f, stripeDeepMat);

            // Clear plastic shoulder doming up from the body to the neck, above the water's fill line.
            Sphere(root, "Shoulder", new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.152f, 0.145f, 0.152f), clearMat);

            // Threaded neck: a slim clear stack with the support ring below the screw threads.
            Cylinder(root, "Neck", 0.145f, 0.052f, 0.06f, clearMat);
            Cylinder(root, "SupportRing", 0.12f, 0.07f, 0.012f, clearMat);
            Cylinder(root, "Threads", 0.16f, 0.06f, 0.035f, clearMat);

            // Bold screw cap with a knurled rim at its base.
            Cylinder(root, "Cap", 0.198f, 0.064f, 0.05f, capMat);
            Cylinder(root, "CapRim", 0.176f, 0.067f, 0.012f, capRimMat);

            return root;
        }

        /// <summary>
        /// The same slim PET-bottle silhouette as the clean water bottle, but refilled from a rain barrel
        /// with murky, sediment-laden water: a cloudy green-grey column, brown grit settled at the base,
        /// flecks of filth clinging to the wall, a greasy scum ring at the surface, a stained peeling label
        /// and a dull weathered cap — so it reads as unsafe (boil before drinking) where it lies on the floor.
        /// </summary>
        private static PartNode BuildTaintedWater(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Tall, slim vessel — same hover as the clean bottle so the gentle bob never dips the base through the floor.
            baseHeight = 0.30f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color water = def.Tint;                                                     // murky green-grey water column
            Color murk = new Color(water.r * 0.66f, water.g * 0.64f, water.b * 0.58f);  // darker, cloudier water and shaded flutes
            Color sludge = new Color(0.32f, 0.25f, 0.15f);                              // brown sediment settled at the base
            Color debris = new Color(0.22f, 0.18f, 0.11f);                              // flecks of filth clinging to the wall
            Color scum = new Color(0.56f, 0.58f, 0.36f);                                // greasy film ringing the surface
            Color clear = Color.Lerp(water, new Color(0.80f, 0.80f, 0.72f), 0.5f);      // grimy, clouded plastic above the fill line
            Color label = new Color(0.80f, 0.77f, 0.64f);                               // stained, yellowed paper label
            Color stain = new Color(0.45f, 0.38f, 0.24f);                               // brown water stain creeping up the label
            Color cap = new Color(0.42f, 0.40f, 0.34f);                                 // dull, weathered screw cap
            Color capRim = new Color(cap.r * 0.78f, cap.g * 0.78f, cap.b * 0.78f);      // knurled cap rim and foot

            PartMat waterMat = PartMat.Of(water, smoothness: 0.5f, metallic: 0.0f);
            PartMat murkMat = PartMat.Of(murk, smoothness: 0.38f, metallic: 0.0f);
            PartMat sludgeMat = PartMat.Of(sludge, smoothness: 0.18f);
            PartMat debrisMat = PartMat.Of(debris, smoothness: 0.14f);
            PartMat scumMat = PartMat.Of(scum, smoothness: 0.28f);
            PartMat clearMat = PartMat.Of(clear, smoothness: 0.62f, metallic: 0.0f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);
            PartMat stainMat = PartMat.Of(stain, smoothness: 0.1f);
            PartMat capMat = PartMat.Of(cap, smoothness: 0.3f);
            PartMat capRimMat = PartMat.Of(capRim, smoothness: 0.3f);

            const float bodyD = 0.15f;   // main body diameter

            // Murky water-filled body: a cloudy column with a rounded base and a low standing foot ring.
            Cylinder(root, "Body", -0.05f, bodyD, 0.30f, waterMat);
            Sphere(root, "BaseRound", new Vector3(0f, -0.185f, 0f), Vector3.zero, new Vector3(0.152f, 0.08f, 0.152f), murkMat);
            Cylinder(root, "FootRing", -0.20f, 0.12f, 0.025f, capRimMat);

            // Brown grit settled on the floor of the bottle (drawn a hair proud of the body so it shows).
            Cylinder(root, "Sediment", -0.172f, bodyD + 0.004f, 0.035f, sludgeMat);

            // Flecks of filth clinging to the wall just above the label, poking through the surface so they read.
            Sphere(root, "Fleck1", new Vector3(0.073f, 0.055f, 0f), Vector3.zero, new Vector3(0.02f, 0.02f, 0.02f), debrisMat);
            Sphere(root, "Fleck2", new Vector3(-0.05f, 0.04f, 0.052f), Vector3.zero, new Vector3(0.015f, 0.015f, 0.015f), debrisMat);
            Sphere(root, "Fleck3", new Vector3(0.02f, 0.065f, -0.072f), Vector3.zero, new Vector3(0.013f, 0.013f, 0.013f), debrisMat);

            // Moulded grip flutes ringing the lower body.
            Cylinder(root, "Rib1", -0.145f, bodyD + 0.006f, 0.012f, murkMat);
            Cylinder(root, "Rib2", -0.115f, bodyD + 0.006f, 0.012f, murkMat);

            // Greasy scum ring pooling at the water's surface, just below the shoulder.
            Cylinder(root, "Scum", 0.082f, bodyD + 0.004f, 0.014f, scumMat);

            // Stained, half-peeled paper label with a brown water stain creeping up from its base.
            Cylinder(root, "Label", -0.03f, bodyD + 0.003f, 0.11f, labelMat);
            Cylinder(root, "LabelStain", -0.065f, bodyD + 0.006f, 0.035f, stainMat);

            // Grimy, clouded plastic shoulder doming up from the body to the neck, above the water's fill line.
            Sphere(root, "Shoulder", new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.152f, 0.145f, 0.152f), clearMat);

            // Threaded neck: a slim clouded stack with the support ring below the screw threads.
            Cylinder(root, "Neck", 0.145f, 0.052f, 0.06f, clearMat);
            Cylinder(root, "SupportRing", 0.12f, 0.07f, 0.012f, clearMat);
            Cylinder(root, "Threads", 0.16f, 0.06f, 0.035f, clearMat);

            // Dull, weathered screw cap with a knurled rim at its base.
            Cylinder(root, "Cap", 0.198f, 0.064f, 0.05f, capMat);
            Cylinder(root, "CapRim", 0.176f, 0.067f, 0.012f, capRimMat);

            return root;
        }

        /// <summary>
        /// The same slim PET-bottle silhouette as the water bottle, but empty and waiting to be refilled:
        /// a clear, faintly blue plastic body with no water column, moulded grip flutes and a few soft crush
        /// creases pressed into an emptied bottle, a plain frosted label remnant instead of bold branding, and
        /// a pale translucent screw cap — so it reads as a reusable empty vessel where it lies on the floor.
        /// </summary>
        private static PartNode BuildEmptyBottle(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Tall, slim vessel — same hover as the filled bottle so the gentle bob never dips the base through the floor.
            baseHeight = 0.30f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color plastic = def.Tint;                                                            // clear, faintly blue PET plastic
            Color plasticShade = new Color(plastic.r * 0.85f, plastic.g * 0.85f, plastic.b * 0.85f); // shaded flutes, creases and foot
            Color plasticHi = Color.Lerp(plastic, Color.white, 0.55f);                           // bright clear shoulder and neck
            Color frost = Color.Lerp(plastic, Color.white, 0.72f);                               // frosted paper label remnant
            Color frostTrim = new Color(frost.r * 0.86f, frost.g * 0.86f, frost.b * 0.86f);       // embossed measurement rings
            Color cap = Color.Lerp(plastic, Color.white, 0.3f);                                  // pale translucent screw cap
            Color capRim = new Color(cap.r * 0.82f, cap.g * 0.82f, cap.b * 0.82f);                // knurled cap rim

            PartMat plasticMat = PartMat.Of(plastic, smoothness: 0.9f, metallic: 0.05f);
            PartMat plasticShadeMat = PartMat.Of(plasticShade, smoothness: 0.85f, metallic: 0.05f);
            PartMat plasticHiMat = PartMat.Of(plasticHi, smoothness: 0.92f, metallic: 0.05f);
            PartMat frostMat = PartMat.Of(frost, smoothness: 0.4f);
            PartMat frostTrimMat = PartMat.Of(frostTrim, smoothness: 0.4f);
            PartMat capMat = PartMat.Of(cap, smoothness: 0.5f, metallic: 0.05f);
            PartMat capRimMat = PartMat.Of(capRim, smoothness: 0.5f, metallic: 0.05f);

            const float bodyD = 0.15f;   // main body diameter

            // Empty clear body: a tall glossy column with no water, a rounded base and a low standing foot ring.
            Cylinder(root, "Body", -0.05f, bodyD, 0.30f, plasticMat);
            Sphere(root, "BaseRound", new Vector3(0f, -0.185f, 0f), Vector3.zero, new Vector3(0.152f, 0.08f, 0.152f), plasticMat);
            Cylinder(root, "FootRing", -0.20f, 0.12f, 0.025f, plasticShadeMat);

            // Moulded grip flutes ringing the lower body.
            Cylinder(root, "Rib1", -0.145f, bodyD + 0.006f, 0.012f, plasticShadeMat);
            Cylinder(root, "Rib2", -0.115f, bodyD + 0.006f, 0.012f, plasticShadeMat);

            // Soft crush creases pressed into the emptied, squeezed bottle (also gives the slow spin something to catch).
            Block(root, "Crease1", new Vector3(0.071f, -0.035f, 0f), new Vector3(0.02f, 0.13f, 0.03f), plasticShadeMat).localEulerAngles = new Vector3(0f, 0f, 6f);
            Block(root, "Crease2", new Vector3(-0.045f, 0.02f, 0.055f), new Vector3(0.018f, 0.1f, 0.026f), plasticShadeMat).localEulerAngles = new Vector3(3f, 40f, -5f);
            Block(root, "Crease3", new Vector3(-0.03f, -0.065f, -0.062f), new Vector3(0.016f, 0.08f, 0.024f), plasticShadeMat).localEulerAngles = new Vector3(-2f, -34f, 4f);

            // Plain frosted label remnant with a couple of embossed measurement rings — no bold branding.
            Cylinder(root, "Label", -0.02f, bodyD + 0.003f, 0.11f, frostMat);
            Cylinder(root, "LabelTrimTop", 0.03f, bodyD + 0.006f, 0.008f, frostTrimMat);
            Cylinder(root, "LabelTrimBottom", -0.07f, bodyD + 0.006f, 0.008f, frostTrimMat);

            // Clear plastic shoulder doming up from the body to the neck.
            Sphere(root, "Shoulder", new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.152f, 0.145f, 0.152f), plasticHiMat);

            // Threaded neck: a slim clear stack with the support ring below the screw threads.
            Cylinder(root, "Neck", 0.145f, 0.052f, 0.06f, plasticHiMat);
            Cylinder(root, "SupportRing", 0.12f, 0.07f, 0.012f, plasticHiMat);
            Cylinder(root, "Threads", 0.16f, 0.06f, 0.035f, plasticHiMat);

            // Pale translucent screw cap with a knurled rim at its base.
            Cylinder(root, "Cap", 0.198f, 0.064f, 0.05f, capMat);
            Cylinder(root, "CapRim", 0.176f, 0.067f, 0.012f, capRimMat);

            return root;
        }

        /// <summary>
        /// A rolled sterile gauze bandage lying on its side: a cream cloth roll with the wound layers
        /// showing as concentric rings on each end, a pale paper band wrapped round the middle, and the
        /// loose end tucked forward toward the floor. The item's medical tint paints a small red cross on
        /// the band so the roll reads as a first-aid dressing where it lies.
        /// </summary>
        private static PartNode BuildBandage(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the roll — chosen so its underside still clears the floor at the low point
            // of the pickup's bob (amplitude 0.06).
            baseHeight = 0.17f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color gauze = new Color(0.95f, 0.94f, 0.90f);       // clean cream cloth
            Color gauzeShade = new Color(0.85f, 0.84f, 0.79f);  // shaded rolled layers / recessed ends
            Color hub = new Color(0.77f, 0.76f, 0.71f);         // darker rolled centre
            Color band = new Color(0.86f, 0.89f, 0.92f);        // pale medical paper wrapper
            Color bandTrim = new Color(band.r * 0.8f, band.g * 0.8f, band.b * 0.8f);   // wrapper edge lines
            Color cross = def.Tint;                             // medical red cross

            PartMat gauzeMat = PartMat.Of(gauze, smoothness: 0.08f);
            PartMat shadeMat = PartMat.Of(gauzeShade, smoothness: 0.08f);
            PartMat hubMat = PartMat.Of(hub, smoothness: 0.06f);
            PartMat bandMat = PartMat.Of(band, smoothness: 0.10f);
            PartMat bandTrimMat = PartMat.Of(bandTrim, smoothness: 0.10f);
            PartMat crossMat = PartMat.Of(cross, smoothness: 0.12f);

            const float rollD = 0.19f;              // outer diameter of the roll
            const float rollLen = 0.18f;            // width of the roll along its lying axis (X)
            const float end = rollLen * 0.5f;
            const float rot = 90f;                  // lays a Rod's length along X
            const float bandD = rollD + 0.01f;      // wrapper stands a hair proud of the roll

            // Main gauze roll, laid on its side along X.
            Rod(root, "Roll", Vector3.zero, new Vector3(0f, 0f, rot), rollD, rollLen, gauzeMat);

            // Each rolled end: a shaded end disc, two concentric rings hinting at the wound layers, and a
            // darker hub at the centre. Stepped a hair proud of the end face so the rings never z-fight.
            for (int s = -1; s <= 1; s += 2)
            {
                Rod(root, "EndCap", new Vector3(s * (end + 0.001f), 0f, 0f), new Vector3(0f, 0f, rot), rollD - 0.004f, 0.005f, shadeMat);
                Rod(root, "Layer1", new Vector3(s * (end + 0.003f), 0f, 0f), new Vector3(0f, 0f, rot), rollD * 0.66f, 0.005f, gauzeMat);
                Rod(root, "Layer2", new Vector3(s * (end + 0.005f), 0f, 0f), new Vector3(0f, 0f, rot), rollD * 0.36f, 0.005f, shadeMat);
                Rod(root, "Hub", new Vector3(s * (end + 0.007f), 0f, 0f), new Vector3(0f, 0f, rot), 0.045f, 0.005f, hubMat);
            }

            // Pale paper band wrapped round the middle with a darker edge line on each side.
            Rod(root, "Band", Vector3.zero, new Vector3(0f, 0f, rot), bandD, 0.05f, bandMat);
            Rod(root, "BandEdgeL", new Vector3(-0.026f, 0f, 0f), new Vector3(0f, 0f, rot), bandD + 0.002f, 0.006f, bandTrimMat);
            Rod(root, "BandEdgeR", new Vector3(0.026f, 0f, 0f), new Vector3(0f, 0f, rot), bandD + 0.002f, 0.006f, bandTrimMat);

            // Small red medical cross sitting on the crown of the band.
            float crownY = bandD * 0.5f + 0.004f;
            Block(root, "CrossArmX", new Vector3(0f, crownY, 0f), new Vector3(0.044f, 0.006f, 0.014f), crossMat);
            Block(root, "CrossArmZ", new Vector3(0f, crownY, 0f), new Vector3(0.014f, 0.006f, 0.044f), crossMat);

            // Loose end of the bandage tucked out of the front of the roll and angled down toward the floor.
            var tail = Block(root, "Tail", new Vector3(0f, -0.072f, 0.145f), new Vector3(0.15f, 0.006f, 0.17f), gauzeMat);
            tail.localEulerAngles = new Vector3(16f, 0f, 0f);

            return root;
        }

        /// <summary>
        /// A pharmacy bottle of painkillers standing upright: an amber plastic bottle with a white
        /// child-proof cap, a paper label wrapped round the body, and a couple of red-and-white capsules
        /// spilled at its base. The item's medical tint paints the label's cross and stripe and the coloured
        /// half of each capsule so the bottle reads as medicine where it lies on the floor.
        /// </summary>
        private static PartNode BuildPills(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the bottle — chosen so the capsules spilled at the base still clear the
            // floor at the low point of the pickup's bob (amplitude 0.06).
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color amber = new Color(0.74f, 0.44f, 0.16f);                 // translucent-looking amber plastic
            Color amberDeep = new Color(amber.r * 0.72f, amber.g * 0.72f, amber.b * 0.72f);  // shaded recessed base
            Color capWhite = new Color(0.92f, 0.92f, 0.94f);              // child-proof push cap
            Color capShade = new Color(capWhite.r * 0.85f, capWhite.g * 0.85f, capWhite.b * 0.85f);  // knurled grip ring
            Color label = new Color(0.96f, 0.96f, 0.93f);                 // printed paper label
            Color red = def.Tint;                                         // medical red — cross, stripe, capsule half
            Color pillWhite = new Color(0.95f, 0.95f, 0.96f);             // capsule body

            PartMat amberMat = PartMat.Of(amber, smoothness: 0.65f);
            PartMat amberDeepMat = PartMat.Of(amberDeep, smoothness: 0.6f);
            PartMat capMat = PartMat.Of(capWhite, smoothness: 0.3f);
            PartMat capShadeMat = PartMat.Of(capShade, smoothness: 0.3f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.1f);
            PartMat redMat = PartMat.Of(red, smoothness: 0.15f);
            PartMat pillWhiteMat = PartMat.Of(pillWhite, smoothness: 0.35f);
            PartMat pillRedMat = PartMat.Of(red, smoothness: 0.35f);

            const float bodyD = 0.16f;   // bottle body diameter
            const float bodyH = 0.20f;   // bottle body height
            const float capD = 0.18f;    // cap slightly overhangs the body rim

            // Amber bottle body with a darker recessed base.
            Cylinder(root, "Body", 0f, bodyD, bodyH, amberMat);
            Cylinder(root, "Base", -0.09f, bodyD * 0.97f, 0.02f, amberDeepMat);

            // White child-proof cap with a knurled grip ring at its lower edge and a slightly inset top.
            Cylinder(root, "Cap", 0.1225f, capD, 0.075f, capMat);
            Cylinder(root, "CapRim", 0.10f, capD + 0.004f, 0.03f, capShadeMat);
            Cylinder(root, "CapTop", 0.162f, capD * 0.78f, 0.01f, capShadeMat);

            // Paper label wrapped round the body with a red band near the top.
            Cylinder(root, "Label", -0.015f, bodyD + 0.006f, 0.13f, labelMat);
            Cylinder(root, "LabelStripe", 0.035f, bodyD + 0.008f, 0.022f, redMat);

            // Red medical cross printed on the front of the label.
            const float crossZ = 0.087f;
            Block(root, "CrossV", new Vector3(0f, -0.02f, crossZ), new Vector3(0.013f, 0.046f, 0.008f), redMat);
            Block(root, "CrossH", new Vector3(0f, -0.02f, crossZ), new Vector3(0.046f, 0.013f, 0.008f), redMat);

            // Two red-and-white capsules spilled on the floor in front of the bottle at different angles.
            BuildCapsule(root, new Vector3(-0.03f, -0.077f, 0.135f), 12f, pillWhiteMat, pillRedMat);
            BuildCapsule(root, new Vector3(0.055f, -0.077f, 0.13f), -32f, pillWhiteMat, pillRedMat);

            return root;
        }

        /// <summary>
        /// A course of antibiotics: a small white medicine carton lying flat with a silver foil blister
        /// strip resting diagonally across its lid. The card is printed with the item's medical tint — a
        /// cross and brand bands — while the strip carries a grid of glossy plastic pill blisters, so it
        /// reads as a sealed box of pills, distinct from the open painkiller bottle and the gauze roll,
        /// where it lies on the floor.
        /// </summary>
        private static PartNode BuildAntibiotics(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the carton — chosen so its underside still clears the floor at the low
            // point of the pickup's bob (amplitude 0.06).
            baseHeight = 0.15f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color card = new Color(0.95f, 0.95f, 0.93f);                          // white cardboard carton
            Color red = def.Tint;                                                 // medical tint — cross and brand bands
            Color redDeep = new Color(red.r * 0.7f, red.g * 0.7f, red.b * 0.7f);  // darker printed dosage text
            Color foil = new Color(0.80f, 0.82f, 0.85f);                          // silver blister backing foil
            Color dome = new Color(0.87f, 0.90f, 0.93f);                          // glossy clear-plastic pill blister

            PartMat cardMat = PartMat.Of(card, smoothness: 0.10f);
            PartMat redMat = PartMat.Of(red, smoothness: 0.15f);
            PartMat redDeepMat = PartMat.Of(redDeep, smoothness: 0.15f);
            PartMat foilMat = PartMat.Of(foil, smoothness: 0.55f, metallic: 0.9f);
            PartMat domeMat = PartMat.Of(dome, smoothness: 0.8f);

            const float boxLen = 0.24f;          // carton length (X)
            const float boxH = 0.09f;            // carton height (Y)
            const float boxDep = 0.15f;          // carton depth (Z)
            const float topY = boxH * 0.5f;      // lid (top face)
            const float frontZ = boxDep * 0.5f;  // front / back faces
            const float eps = 0.002f;

            // Cardboard carton body.
            Block(root, "Body", Vector3.zero, new Vector3(boxLen, boxH, boxDep), cardMat);

            // Lid branding near the +X end (kept clear of the blister strip that rests over the middle):
            // a red band carrying a cream medical cross flanked by two darker dosage lines.
            Block(root, "LidBand", new Vector3(0.088f, topY + eps, 0f), new Vector3(0.05f, 0.005f, boxDep - 0.024f), redMat);
            Block(root, "LidCrossV", new Vector3(0.088f, topY + eps * 2f, 0f), new Vector3(0.016f, 0.005f, 0.05f), cardMat);
            Block(root, "LidCrossH", new Vector3(0.088f, topY + eps * 2f, 0f), new Vector3(0.05f, 0.005f, 0.016f), cardMat);
            Block(root, "LidText1", new Vector3(0.088f, topY + eps * 2f, 0.046f), new Vector3(0.05f, 0.004f, 0.006f), redDeepMat);
            Block(root, "LidText2", new Vector3(0.088f, topY + eps * 2f, -0.046f), new Vector3(0.05f, 0.004f, 0.006f), redDeepMat);

            // Front and back faces: a red brand band running the length of the carton with a cream cross
            // and a darker dosage line below it, so the box still reads as medicine when it spins side-on.
            for (int s = -1; s <= 1; s += 2)
            {
                float z = s * (frontZ + eps);
                float zc = s * (frontZ + eps * 2f);
                Block(root, "FaceBand", new Vector3(0f, 0.012f, z), new Vector3(boxLen - 0.02f, 0.05f, 0.005f), redMat);
                Block(root, "FaceCrossV", new Vector3(-0.078f, 0.012f, zc), new Vector3(0.016f, 0.042f, 0.005f), cardMat);
                Block(root, "FaceCrossH", new Vector3(-0.078f, 0.012f, zc), new Vector3(0.044f, 0.016f, 0.005f), cardMat);
                Block(root, "FaceText", new Vector3(0.05f, -0.028f, z), new Vector3(0.11f, 0.007f, 0.005f), redDeepMat);
            }

            // Silver foil blister strip resting diagonally across the lid, built under a yawed sub-root so
            // the pockets sit in a simple grid; the strip's underside rests on the lid (local y 0 == box top).
            var strip = new PartNode("Blister");
            strip.SetParent(root, false);
            strip.localPosition = new Vector3(-0.03f, topY, 0f);
            strip.localEulerAngles = new Vector3(0f, 20f, 0f);

            const float foilLen = 0.20f;   // strip length (local X)
            const float foilWid = 0.06f;   // strip width (local Z)
            const float foilThk = 0.012f;  // foil card thickness

            Block(strip, "Foil", new Vector3(0f, foilThk * 0.5f, 0f), new Vector3(foilLen, foilThk, foilWid), foilMat);

            // A 3 x 2 grid of glossy plastic pill blisters domed up out of the foil (their lower halves
            // sink into the card so each reads as a sealed bubble).
            for (int col = 0; col < 3; col++)
            {
                float x = -0.06f + col * 0.06f;
                for (int rz = -1; rz <= 1; rz += 2)
                    Sphere(strip, "Blister", new Vector3(x, foilThk + 0.006f, rz * 0.015f), Vector3.zero, new Vector3(0.03f, 0.028f, 0.03f), domeMat);
            }

            return root;
        }

        /// <summary>
        /// A trigger-spray bottle of disinfectant standing upright: a glossy bottle of pale antiseptic
        /// with a clear plastic shoulder above the fill line and a moulded trigger sprayer — a
        /// forward-pointing nozzle and a finger trigger — screwed onto the neck. The item's medical tint
        /// paints the label band, a red cross and the trigger, so the bottle reads as a first-aid
        /// disinfectant, distinct from the painkiller bottle, the gauze roll and the boxed antibiotics,
        /// where it lies on the floor.
        /// </summary>
        private static PartNode BuildDisinfectant(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the bottle — chosen so the foot still clears the floor at the low point of
            // the pickup's bob (amplitude 0.06), matching the hover of the other bottles.
            baseHeight = 0.22f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color liquid = new Color(0.52f, 0.80f, 0.70f);                                       // pale antiseptic, mint-green
            Color liquidShade = new Color(liquid.r * 0.82f, liquid.g * 0.82f, liquid.b * 0.82f);  // shaded grip flutes
            Color clear = Color.Lerp(liquid, Color.white, 0.62f);                                // clouded clear plastic above the fill line
            Color label = new Color(0.95f, 0.96f, 0.97f);                                        // white paper label
            Color sprayer = new Color(0.86f, 0.87f, 0.90f);                                      // light-grey trigger sprayer housing
            Color sprayerDark = new Color(0.52f, 0.54f, 0.58f);                                  // shaded nozzle orifice
            Color red = def.Tint;                                                                // medical red — label band, cross, trigger
            Color redDeep = new Color(red.r * 0.7f, red.g * 0.7f, red.b * 0.7f);                 // darker label trim lines

            PartMat liquidMat = PartMat.Of(liquid, smoothness: 0.8f, metallic: 0.05f);
            PartMat liquidShadeMat = PartMat.Of(liquidShade, smoothness: 0.78f, metallic: 0.05f);
            PartMat clearMat = PartMat.Of(clear, smoothness: 0.86f, metallic: 0.05f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.2f);
            PartMat sprayerMat = PartMat.Of(sprayer, smoothness: 0.35f);
            PartMat sprayerDarkMat = PartMat.Of(sprayerDark, smoothness: 0.3f);
            PartMat redMat = PartMat.Of(red, smoothness: 0.2f);
            PartMat redDeepMat = PartMat.Of(redDeep, smoothness: 0.2f);

            const float bodyD = 0.15f;   // bottle body diameter

            // Bottle body: a glossy column of antiseptic with a low standing foot and two moulded grip flutes.
            Cylinder(root, "Body", -0.02f, bodyD, 0.22f, liquidMat);
            Cylinder(root, "Foot", -0.125f, bodyD - 0.012f, 0.02f, clearMat);
            Cylinder(root, "Rib1", -0.10f, bodyD + 0.006f, 0.01f, liquidShadeMat);
            Cylinder(root, "Rib2", -0.07f, bodyD + 0.006f, 0.01f, liquidShadeMat);

            // Wrapped white label with a red brand band bracketed by darker trim lines and a red medical
            // cross on the front, so it reads as first-aid where it lies.
            Cylinder(root, "Label", -0.025f, bodyD + 0.004f, 0.13f, labelMat);
            Cylinder(root, "LabelBand", 0.018f, bodyD + 0.007f, 0.028f, redMat);
            Cylinder(root, "LabelTrimTop", 0.035f, bodyD + 0.006f, 0.006f, redDeepMat);
            Cylinder(root, "LabelTrimBottom", 0.001f, bodyD + 0.006f, 0.006f, redDeepMat);
            const float crossZ = bodyD * 0.5f + 0.006f;
            Block(root, "CrossV", new Vector3(0f, -0.045f, crossZ), new Vector3(0.015f, 0.05f, 0.008f), redMat);
            Block(root, "CrossH", new Vector3(0f, -0.045f, crossZ), new Vector3(0.05f, 0.015f, 0.008f), redMat);

            // Clear plastic shoulder doming up from the body to the neck, above the liquid's fill line.
            Sphere(root, "Shoulder", new Vector3(0f, 0.085f, 0f), Vector3.zero, new Vector3(0.152f, 0.12f, 0.152f), clearMat);
            Cylinder(root, "Neck", 0.145f, 0.062f, 0.05f, clearMat);

            // Trigger sprayer: a screw collar, a moulded head, a barrel jutting forward to the nozzle, and a
            // finger trigger angled at the front — the silhouette that marks this out as a spray bottle.
            Cylinder(root, "Collar", 0.178f, 0.078f, 0.045f, sprayerMat);
            Block(root, "Head", new Vector3(0f, 0.228f, 0.006f), new Vector3(0.066f, 0.085f, 0.078f), sprayerMat);
            Block(root, "Barrel", new Vector3(0f, 0.248f, 0.078f), new Vector3(0.052f, 0.052f, 0.10f), sprayerMat);
            Rod(root, "Nozzle", new Vector3(0f, 0.248f, 0.132f), new Vector3(90f, 0f, 0f), 0.03f, 0.02f, sprayerMat);
            Rod(root, "NozzleTip", new Vector3(0f, 0.248f, 0.146f), new Vector3(90f, 0f, 0f), 0.02f, 0.01f, sprayerDarkMat);
            var trigger = Block(root, "Trigger", new Vector3(0f, 0.188f, 0.082f), new Vector3(0.042f, 0.085f, 0.016f), redMat);
            trigger.localEulerAngles = new Vector3(20f, 0f, 0f);

            return root;
        }

        /// <summary>
        /// A hard-shell first-aid kit: a chunky red plastic case with an overhanging clam-shell lid, a
        /// moulded carry handle arched across the back and two steel latches clamping it shut, blazoned with
        /// a bold white cross on the lid and both faces. The item's medical tint paints the case itself, so
        /// the kit reads as the iconic red-and-white first-aid box — distinct from the flat antibiotics
        /// carton, the gauze roll, the painkiller bottle and the spray bottle — where it lies on the floor.
        /// </summary>
        private static PartNode BuildFirstAidKit(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the case — chosen so its underside still clears the floor at the low point of
            // the pickup's bob (amplitude 0.06). The carry handle rises above centre and never nears the floor.
            baseHeight = 0.17f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color shell = def.Tint;                                                          // moulded red case (the lid)
            Color shellDeep = new Color(shell.r * 0.82f, shell.g * 0.82f, shell.b * 0.82f);  // shaded lower shell
            Color seam = new Color(shell.r * 0.45f, shell.g * 0.45f, shell.b * 0.45f);       // recessed lid/base lip
            Color white = new Color(0.95f, 0.96f, 0.96f);                                    // painted first-aid cross
            Color handle = new Color(0.19f, 0.20f, 0.22f);                                   // charcoal carry handle
            Color metal = new Color(0.66f, 0.68f, 0.72f);                                    // steel latches and hinge

            PartMat shellMat = PartMat.Of(shell, smoothness: 0.35f);
            PartMat shellDeepMat = PartMat.Of(shellDeep, smoothness: 0.32f);
            PartMat seamMat = PartMat.Of(seam, smoothness: 0.2f);
            PartMat whiteMat = PartMat.Of(white, smoothness: 0.25f);
            PartMat handleMat = PartMat.Of(handle, smoothness: 0.3f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.6f, metallic: 0.8f);

            const float boxLen = 0.34f;    // case width (X)
            const float boxDep = 0.21f;    // case depth (Z)
            const float baseH = 0.105f;    // lower shell height
            const float lidH = 0.085f;     // upper shell (lid) height
            const float seamY = 0.01f;     // height where the lid closes onto the base
            const float baseCY = seamY - baseH * 0.5f;   // base centre
            const float lidCY = seamY + lidH * 0.5f;     // lid centre
            const float topY = seamY + lidH;             // top face of the lid
            const float frontZ = boxDep * 0.5f;          // front / back faces (±Z)
            const float eps = 0.002f;

            // Lower shell (the base tray) in a shaded red; the lid rests on top a hair wider so it reads as
            // an overhanging clam-shell lid.
            Block(root, "Base", new Vector3(0f, baseCY, 0f), new Vector3(boxLen, baseH, boxDep), shellDeepMat);
            Block(root, "Lid", new Vector3(0f, lidCY, 0f), new Vector3(boxLen + 0.006f, lidH, boxDep + 0.006f), shellMat);

            // A thin darker band standing a touch proud all the way round the seam — the rubber-sealed lip
            // where the lid closes onto the base.
            Block(root, "Seam", new Vector3(0f, seamY, 0f), new Vector3(boxLen + 0.01f, 0.012f, boxDep + 0.01f), seamMat);

            // Bold white cross on the top of the lid, set toward the front so it clears the carry handle.
            Block(root, "TopCrossV", new Vector3(0f, topY + eps, -0.04f), new Vector3(0.032f, 0.006f, 0.11f), whiteMat);
            Block(root, "TopCrossH", new Vector3(0f, topY + eps, -0.04f), new Vector3(0.11f, 0.006f, 0.032f), whiteMat);

            // Bold white cross on the front and back faces so the kit still reads as first-aid side-on.
            for (int s = -1; s <= 1; s += 2)
            {
                float z = s * (frontZ + 0.006f);
                Block(root, "FaceCrossV", new Vector3(0f, 0f, z), new Vector3(0.032f, 0.11f, 0.006f), whiteMat);
                Block(root, "FaceCrossH", new Vector3(0f, 0f, z), new Vector3(0.11f, 0.032f, 0.006f), whiteMat);
            }

            // Moulded carry handle arched across the back of the lid: two posts rising from mounting plates
            // with an open grip beneath a top bar.
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * 0.06f;
                Block(root, "HandleMount", new Vector3(x, topY + 0.006f, 0.05f), new Vector3(0.05f, 0.014f, 0.055f), handleMat);
                Block(root, "HandlePost", new Vector3(x, topY + 0.03f, 0.05f), new Vector3(0.024f, 0.05f, 0.03f), handleMat);
            }
            Block(root, "HandleBar", new Vector3(0f, topY + 0.055f, 0.05f), new Vector3(0.156f, 0.022f, 0.032f), handleMat);

            // Two steel latches clamp the lid to the base at the front, outboard of the cross and straddling
            // the seam; a hinge pair does the same at the back.
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * 0.115f;
                Block(root, "Latch", new Vector3(x, seamY, frontZ + 0.006f), new Vector3(0.042f, 0.055f, 0.016f), metalMat);
                Block(root, "LatchCatch", new Vector3(x, seamY - 0.008f, frontZ + 0.012f), new Vector3(0.03f, 0.018f, 0.01f), metalMat);
                Block(root, "Hinge", new Vector3(s * 0.09f, seamY, -frontZ - 0.005f), new Vector3(0.05f, 0.02f, 0.014f), metalMat);
            }

            return root;
        }

        /// <summary>
        /// A push-through blister strip of beta blockers lying flat: a silver foil card carrying a grid of
        /// clear-plastic pill blisters — the pair at one end already popped and empty — printed with the
        /// item's calming blue tint, with a few round blue tablets spilled beside it. The cool blue print
        /// and tablets set it apart from the red-cross medical items (the gauze roll, painkiller bottle,
        /// boxed antibiotics, spray bottle and first-aid case), so it reads as the steady-your-nerves pills
        /// where it lies on the floor.
        /// </summary>
        private static PartNode BuildBetaBlockers(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Centre height of the strip — a thin card, so it hovers low, chosen so the foil and the
            // spilled tablets still clear the floor at the low point of the pickup's bob (amplitude 0.06).
            baseHeight = 0.14f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color foil = new Color(0.80f, 0.82f, 0.85f);                              // silver lidding foil
            Color foilShade = new Color(0.64f, 0.66f, 0.70f);                         // shaded edge lip / torn flap
            Color dome = new Color(0.82f, 0.86f, 0.94f);                              // clear plastic pill blister, cool-tinted
            Color blue = def.Tint;                                                    // calming blue — brand print and tablets
            Color blueDeep = new Color(blue.r * 0.7f, blue.g * 0.7f, blue.b * 0.7f);  // darker dosage text / tablet score
            Color tablet = Color.Lerp(blue, Color.white, 0.18f);                      // solid blue-purple tablet, a touch lighter
            Color hole = new Color(0.18f, 0.18f, 0.20f);                              // popped, empty pocket

            PartMat foilMat = PartMat.Of(foil, smoothness: 0.55f, metallic: 0.9f);
            PartMat foilShadeMat = PartMat.Of(foilShade, smoothness: 0.5f, metallic: 0.85f);
            PartMat domeMat = PartMat.Of(dome, smoothness: 0.8f);
            PartMat blueMat = PartMat.Of(blue, smoothness: 0.2f);
            PartMat blueDeepMat = PartMat.Of(blueDeep, smoothness: 0.2f);
            PartMat tabletMat = PartMat.Of(tablet, smoothness: 0.3f);
            PartMat scoreMat = PartMat.Of(blueDeep, smoothness: 0.25f);
            PartMat holeMat = PartMat.Of(hole, smoothness: 0.2f);

            const float stripLen = 0.30f;   // strip length (X)
            const float stripWid = 0.15f;   // strip width (Z) — two columns of blisters
            const float cardThk = 0.012f;   // foil card thickness
            const float topY = cardThk * 0.5f;
            const float eps = 0.001f;

            // Silver foil backing card sitting on a slightly larger shaded lip that frames the edge.
            Block(root, "Rim", new Vector3(0f, -eps, 0f), new Vector3(stripLen + 0.006f, cardThk * 0.7f, stripWid + 0.006f), foilShadeMat);
            Block(root, "Foil", Vector3.zero, new Vector3(stripLen, cardThk, stripWid), foilMat);

            // Blue brand band printed along the front edge of the flange, carrying a cross at the -X end and
            // two dosage lines, so the strip still reads as pharma packaging when it spins side-on.
            const float bandZ = -stripWid * 0.5f + 0.02f;
            Block(root, "BrandBand", new Vector3(0f, topY + eps, bandZ), new Vector3(stripLen - 0.03f, 0.003f, 0.02f), blueMat);
            Block(root, "BandCrossV", new Vector3(-0.11f, topY + eps * 2f, bandZ), new Vector3(0.012f, 0.003f, 0.03f), foilMat);
            Block(root, "BandCrossH", new Vector3(-0.11f, topY + eps * 2f, bandZ), new Vector3(0.03f, 0.003f, 0.012f), foilMat);
            Block(root, "BandText1", new Vector3(0.01f, topY + eps * 2f, bandZ), new Vector3(0.12f, 0.003f, 0.006f), blueDeepMat);
            Block(root, "BandText2", new Vector3(0.05f, topY + eps * 2f, bandZ - 0.008f), new Vector3(0.08f, 0.003f, 0.004f), blueDeepMat);

            // Blister grid: four rows along X, two columns along Z. The first three rows are still sealed
            // (clear plastic domes); the row at the +X end has been pushed through, leaving two dark pockets.
            const float domeD = 0.05f;
            const float domeH = 0.032f;
            float[] rowX = { -0.105f, -0.035f, 0.035f, 0.105f };
            float[] colZ = { 0.042f, -0.012f };
            for (int r = 0; r < rowX.Length; r++)
            {
                bool popped = r == rowX.Length - 1;   // last row already used
                foreach (float z in colZ)
                {
                    if (popped)
                        Rod(root, "Pocket", new Vector3(rowX[r], topY - 0.003f, z), Vector3.zero, domeD * 0.82f, 0.006f, holeMat);
                    else
                        Sphere(root, "Blister", new Vector3(rowX[r], topY + domeH * 0.25f, z), Vector3.zero,
                            new Vector3(domeD, domeH, domeD), domeMat);
                }
            }

            // A little foil torn up at the +X end where the tablets were pushed out.
            var flap = Block(root, "TornFlap", new Vector3(stripLen * 0.5f - 0.008f, topY + 0.012f, 0.02f),
                new Vector3(0.028f, 0.002f, 0.05f), foilShadeMat);
            flap.localEulerAngles = new Vector3(0f, 0f, 36f);

            // Three round blue tablets spilled on the floor just past the popped end — their colour marks
            // these out as the calming beta-blocker pills, distinct from the painkiller bottle's capsules.
            BuildTablet(root, new Vector3(0.20f, 0.002f, 0.03f), new Vector3(4f, 20f, 5f), tabletMat, scoreMat);
            BuildTablet(root, new Vector3(0.175f, 0.002f, -0.06f), new Vector3(3f, -30f, -5f), tabletMat, scoreMat);
            BuildTablet(root, new Vector3(0.235f, 0.002f, -0.04f), new Vector3(-3f, 46f, 4f), tabletMat, scoreMat);

            return root;
        }

        /// <summary>
        /// A kitchen knife lying flat on the floor — the first melee weapon with a bespoke model: a
        /// polished, tapered steel blade with a bright ground edge and a thicker spine, a chunky chrome
        /// bolster, and a dark full-tang handle pinned by three rivets. The blade takes the item's steel
        /// tint while the handle stays black, so the knife reads clearly from the iso camera as it turns.
        /// Laid along X with the point forward, roughly balanced about the spin centre.
        /// </summary>
        private static PartNode BuildKitchenKnife(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Flat and thin, so it hovers low; the bob never dips the bolster through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                     // polished blade
            Color edge = new Color(0.90f, 0.92f, 0.96f);               // bright sharpened edge
            Color spine = new Color(steel.r * 0.82f, steel.g * 0.82f, steel.b * 0.82f);  // darker, thicker back
            Color chrome = new Color(0.82f, 0.84f, 0.88f);             // bolster, tang and rivets
            Color grip = new Color(0.13f, 0.13f, 0.15f);               // matte black moulded handle

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.72f, metallic: 0.9f);
            PartMat edgeMat = PartMat.Of(edge, smoothness: 0.85f, metallic: 0.85f);
            PartMat spineMat = PartMat.Of(spine, smoothness: 0.55f, metallic: 0.9f);
            PartMat chromeMat = PartMat.Of(chrome, smoothness: 0.8f, metallic: 0.95f);
            PartMat gripMat = PartMat.Of(grip, smoothness: 0.35f);

            const float bladeThick = 0.011f;   // blade thickness, lying flat
            const float bladeWidth = 0.05f;    // spine-to-edge width of the plate

            // Flat blade plate: a straight spine running forward from the bolster.
            Block(root, "Blade", new Vector3(0.05f, 0f, 0f), new Vector3(0.21f, bladeThick, bladeWidth), steelMat);

            // Tapered point: a flattened lens nudged toward the edge for a hint of belly, its rear tucked
            // under the plate so the blade narrows smoothly to the tip.
            Sphere(root, "Tip", new Vector3(0.16f, 0f, -0.005f), Vector3.zero,
                new Vector3(0.18f, bladeThick, 0.048f), steelMat);

            // Bright ground edge down the cutting side, set a hair lower than the plate.
            Block(root, "Edge", new Vector3(0.05f, -0.0015f, -0.023f), new Vector3(0.21f, 0.008f, 0.012f), edgeMat);

            // Thicker spine along the back of the blade.
            Block(root, "Spine", new Vector3(0.045f, 0f, 0.024f), new Vector3(0.2f, 0.014f, 0.007f), spineMat);

            // Chunky chrome bolster / finger guard, taller and wider than the blade.
            Block(root, "Bolster", new Vector3(-0.065f, 0f, 0f), new Vector3(0.03f, 0.038f, 0.056f), chromeMat);

            // Dark full-tang handle: a flattened grip rounded off at the butt.
            Block(root, "Handle", new Vector3(-0.1475f, 0f, 0f), new Vector3(0.135f, 0.032f, 0.042f), gripMat);
            Sphere(root, "Butt", new Vector3(-0.213f, 0f, 0f), Vector3.zero, new Vector3(0.03f, 0.032f, 0.042f), gripMat);

            // Exposed tang strip along the top of the grip, pinned by three rivets.
            Block(root, "Tang", new Vector3(-0.1475f, 0.015f, 0f), new Vector3(0.135f, 0.005f, 0.012f), chromeMat);
            Sphere(root, "Rivet1", new Vector3(-0.105f, 0.018f, 0f), Vector3.zero, new Vector3(0.014f, 0.009f, 0.014f), chromeMat);
            Sphere(root, "Rivet2", new Vector3(-0.147f, 0.018f, 0f), Vector3.zero, new Vector3(0.014f, 0.009f, 0.014f), chromeMat);
            Sphere(root, "Rivet3", new Vector3(-0.189f, 0.018f, 0f), Vector3.zero, new Vector3(0.014f, 0.009f, 0.014f), chromeMat);

            return root;
        }

        /// <summary>
        /// A carpenter's claw hammer lying flat on its cheek: a wooden handle running the long axis with a
        /// forged steel head across the top — a round striking face on one side and a curved, split claw on
        /// the other. The head takes the item's steel tint; the handle is a fixed hickory brown.
        /// </summary>
        private static PartNode BuildHammer(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Lies on its side so it hovers low; the striking face is the thickest part below centre and the
            // bob never dips it through the floor.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                          // forged head
            Color bright = new Color(0.82f, 0.84f, 0.90f);                   // polished striking face
            Color dark = new Color(steel.r * 0.78f, steel.g * 0.78f, steel.b * 0.78f);  // chamfer / shaded steel
            Color wood = new Color(0.55f, 0.36f, 0.20f);                     // hickory shaft
            Color woodDark = new Color(0.40f, 0.26f, 0.14f);                 // grip and butt

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.62f, metallic: 0.85f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.8f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.5f, metallic: 0.85f);
            PartMat woodMat = PartMat.Of(wood, smoothness: 0.22f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.2f);

            const float lay = 90f;      // lays a Rod's length along X (the handle)
            const float across = 90f;   // lays a Rod's length along Z (the head's axis)

            // ---- Wooden handle, running along X with the head at the +X end ----
            // Main shaft, passing up through the head's eye and poking out the top.
            Rod(root, "Shaft", new Vector3(-0.075f, 0f, 0f), new Vector3(0f, 0f, lay), 0.023f, 0.30f, woodMat);
            // A swelling throat just below the head where a real handle thickens.
            Rod(root, "Throat", new Vector3(0.015f, 0f, 0f), new Vector3(0f, 0f, lay), 0.03f, 0.05f, woodMat);
            // Darker grip section, a flared collar and a rounded butt.
            Rod(root, "Grip", new Vector3(-0.16f, 0f, 0f), new Vector3(0f, 0f, lay), 0.028f, 0.085f, woodDarkMat);
            Rod(root, "ButtFlare", new Vector3(-0.208f, 0f, 0f), new Vector3(0f, 0f, lay), 0.032f, 0.02f, woodDarkMat);
            Sphere(root, "Butt", new Vector3(-0.223f, 0f, 0f), Vector3.zero, new Vector3(0.032f, 0.03f, 0.03f), woodDarkMat);

            // ---- Steel head, its axis running across the handle along Z ----
            // The eye: the bulky centre the handle is driven through, rounded off at the cheeks.
            Block(root, "Eye", new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0.03f, 0.034f), steelMat);
            Sphere(root, "EyeRound", new Vector3(0.05f, 0f, 0f), Vector3.zero, new Vector3(0.05f, 0.033f, 0.044f), steelMat);
            // The handle end wedged proud of the top of the head.
            Block(root, "Wedge", new Vector3(0.076f, 0f, 0f), new Vector3(0.006f, 0.024f, 0.022f), brightMat);

            // Poll and striking face on the +Z side: a neck flaring to a bright, chamfered face.
            Rod(root, "Poll", new Vector3(0.05f, 0f, 0.03f), new Vector3(across, 0f, 0f), 0.03f, 0.045f, steelMat);
            Rod(root, "Face", new Vector3(0.05f, 0f, 0.058f), new Vector3(across, 0f, 0f), 0.033f, 0.016f, brightMat);
            Rod(root, "FaceRim", new Vector3(0.05f, 0f, 0.067f), new Vector3(across, 0f, 0f), 0.035f, 0.005f, darkMat);

            // Curved claw on the -Z side: a solid neck that splits into two tapering tines hooking back
            // toward the handle, with the nail slot between them.
            Block(root, "ClawNeck", new Vector3(0.052f, 0f, -0.02f), new Vector3(0.016f, 0.032f, 0.03f), steelMat);
            BuildClawTine(root, 0.0095f, steelMat);    // upper tine
            BuildClawTine(root, -0.0095f, steelMat);   // lower tine

            return root;
        }

        /// <summary>Builds one tapering claw tine at vertical offset <paramref name="yOff"/>: two short
        /// segments hooking back toward the handle so the pair reads as the hammer's curved, split claw.
        /// The <see cref="Block"/> helper takes no rotation, so each segment's hook is set on its transform.</summary>
        private static void BuildClawTine(PartNode parent, float yOff, PartMat mat)
        {
            Block(parent, "ClawSeg", new Vector3(0.05f, yOff, -0.038f), new Vector3(0.012f, 0.012f, 0.05f), mat)
                .localEulerAngles = new Vector3(0f, 12f, 0f);
            Block(parent, "ClawTip", new Vector3(0.041f, yOff, -0.067f), new Vector3(0.009f, 0.01f, 0.03f), mat)
                .localEulerAngles = new Vector3(0f, 34f, 0f);
        }

        /// <summary>
        /// A cast-iron frying pan lying flat on the floor — the third melee weapon with a bespoke model: a
        /// shallow round skillet with a seasoned, heat-stained cooking surface ringed by a raised cast lip,
        /// and a long tapering one-piece handle running out to one side that ends in a flared paddle with a
        /// suggested hanging hole. The pan reads as black cast iron, so the item's light steel tint is used
        /// only as the base the dark iron, seasoning and worn rim are derived from — a deliberate deviation
        /// (like the medical items) so the skillet doesn't look like polished cutlery. Laid along X with the
        /// bowl centred on the spin pivot so the handle sweeps cleanly as it turns.
        /// </summary>
        private static PartNode BuildFryingPan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Shallow and flat, so it hovers low; the body's underside is the lowest part and the bob never
            // dips it through the floor (0.11 - 0.06 bob - 0.015 half-body > 0).
            baseHeight = 0.11f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                       // light tool-steel tint (base only)
            Color iron = new Color(steel.r * 0.30f, steel.g * 0.29f, steel.b * 0.28f);    // dark cast-iron body + handle
            Color season = new Color(steel.r * 0.34f, steel.g * 0.30f, steel.b * 0.26f);  // seasoned, oiled cooking surface
            Color stain = new Color(steel.r * 0.19f, steel.g * 0.17f, steel.b * 0.15f);   // heat-darkened centre / hang hole
            Color rimC = new Color(steel.r * 0.37f, steel.g * 0.37f, steel.b * 0.38f);    // worn, rubbed-shiny cast lip

            PartMat ironMat = PartMat.Of(iron, smoothness: 0.2f, metallic: 0.55f);
            PartMat seasonMat = PartMat.Of(season, smoothness: 0.42f, metallic: 0.5f);
            PartMat stainMat = PartMat.Of(stain, smoothness: 0.3f, metallic: 0.4f);
            PartMat rimMat = PartMat.Of(rimC, smoothness: 0.4f, metallic: 0.7f);

            // ---- Round pan body, coaxial about the spin centre ----
            // Outer body: a shallow dark-iron disc forming the pan's underside and the floor of the well.
            Cylinder(root, "Body", 0f, 0.30f, 0.03f, ironMat);
            // Seasoned cooking surface filling the interior up to the lip, with a heat-darkened centre patch.
            Cylinder(root, "Cook", 0.017f, 0.268f, 0.008f, seasonMat);
            Cylinder(root, "Stain", 0.022f, 0.13f, 0.005f, stainMat);

            // Raised cast lip: a ring of short blocks stood around the edge so the pan reads as a walled
            // skillet rather than a flat plate. Each faces the centre and overlaps its neighbours into a
            // continuous, faintly faceted cast wall. (The Block helper takes no rotation, so it's set after.)
            const int rimSeg = 24;
            const float rimR = 0.143f;
            for (int i = 0; i < rimSeg; i++)
            {
                float a = i / (float)rimSeg * Mathf.PI * 2f;
                Block(root, "Rim", new Vector3(Mathf.Cos(a) * rimR, 0.02f, Mathf.Sin(a) * rimR),
                    new Vector3(0.02f, 0.03f, 0.05f), rimMat)
                    .localEulerAngles = new Vector3(0f, -i / (float)rimSeg * 360f, 0f);
            }

            // ---- One-piece cast handle running out along +X, level with the lip ----
            // A chunky throat where the handle joins the pan wall, then a long tapering bar.
            Rod(root, "Throat", new Vector3(0.17f, 0.013f, 0f), new Vector3(0f, 0f, 90f), 0.05f, 0.07f, ironMat);
            Rod(root, "Handle", new Vector3(0.26f, 0.014f, 0f), new Vector3(0f, 0f, 90f), 0.032f, 0.2f, ironMat);
            // A flat spine along the top of the handle, as cast handles have.
            Block(root, "Spine", new Vector3(0.255f, 0.027f, 0f), new Vector3(0.2f, 0.007f, 0.026f), ironMat);
            // Flared paddle end with a suggested hanging hole (a dark cylinder set through it reads as a hole).
            Rod(root, "EndFlare", new Vector3(0.36f, 0.014f, 0f), new Vector3(0f, 0f, 90f), 0.044f, 0.035f, ironMat);
            Rod(root, "HangHole", new Vector3(0.365f, 0.014f, 0f), new Vector3(90f, 0f, 0f), 0.016f, 0.06f, stainMat);

            return root;
        }

        /// <summary>
        /// A steel crowbar (wrecking bar) lying flat — the fourth melee weapon with a bespoke model. A long
        /// forged bar with a curved gooseneck claw hooking up from the +X end and a flattened, angled pry
        /// tip at the -X end, each split by a nail-pulling slot. The item's light steel tint paints the raw
        /// forged bar, with the working ends rubbed bright from use. Laid along X with the bar near the
        /// floor so the hook rises clear as the pickup spins and bobs.
        /// </summary>
        private static PartNode BuildCrowbar(ItemDef def, PartNode parent, out float baseHeight)
        {
            // The round bar is the lowest part; the hook and pry tip only rise above it, so it hovers low
            // without the bob dipping the bar through the floor (0.12 - 0.06 bob - 0.013 half-bar > 0).
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                     // raw forged bar
            Color bright = new Color(0.84f, 0.86f, 0.92f);                              // pry surfaces worn shiny
            Color dark = new Color(steel.r * 0.68f, steel.g * 0.68f, steel.b * 0.68f);  // shaded forge / nail slots

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.55f, metallic: 0.85f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.82f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.4f, metallic: 0.8f);

            const float lay = 90f;         // lays a Rod's length along X
            const float shaftD = 0.026f;   // bar thickness

            // ---- Long forged shaft running along X, the claw end at +X ----
            Rod(root, "Shaft", new Vector3(-0.02f, 0f, 0f), new Vector3(0f, 0f, lay), shaftD, 0.40f, steelMat);

            // ---- Gooseneck claw hooking up and back from the +X end ----
            // Traced as short segments along an arc in the X-Y (side-on) plane, tapering toward the claw and
            // brightening at the worn tip.
            const float cx = 0.17f, cy = 0.052f, arcR = 0.052f;   // arc centre and radius
            const float aStart = -90f, aEnd = 166f;               // from the shaft end, up and back over
            const int seg = 8;
            Vector3 prev = ArcPoint(cx, cy, arcR, aStart);
            for (int i = 1; i <= seg; i++)
            {
                float a = Mathf.Lerp(aStart, aEnd, i / (float)seg);
                Vector3 p = ArcPoint(cx, cy, arcR, a);
                Vector3 mid = (prev + p) * 0.5f;
                float len = (p - prev).magnitude * 1.3f;                       // overlap so the curve reads solid
                float dirDeg = Mathf.Atan2(p.y - prev.y, p.x - prev.x) * Mathf.Rad2Deg;
                float d = Mathf.Lerp(shaftD, 0.02f, i / (float)seg);           // taper toward the claw
                // A Rod points along +Y at rest; rotating about Z by (chord angle − 90) lays it along the chord.
                Rod(root, "Hook", mid, new Vector3(0f, 0f, dirDeg - 90f), d, len, i >= seg - 1 ? brightMat : steelMat);
                prev = p;
            }

            // Flattened, forked claw at the hook's tip — a nail puller facing back toward the shaft.
            float tanDeg = aEnd + 90f;                             // tangent direction of the arc at its end
            float tanRad = tanDeg * Mathf.Deg2Rad;
            Vector3 tip = ArcPoint(cx, cy, arcR, aEnd);
            Vector3 tanDir = new Vector3(Mathf.Cos(tanRad), Mathf.Sin(tanRad), 0f);
            Vector3 perp = new Vector3(Mathf.Cos(tanRad - Mathf.PI * 0.5f), Mathf.Sin(tanRad - Mathf.PI * 0.5f), 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pc = tip + tanDir * 0.016f + perp * (0.011f * s);
                Block(root, "ClawProng", pc, new Vector3(0.032f, 0.016f, 0.012f), brightMat)
                    .localEulerAngles = new Vector3(0f, 0f, tanDeg);
            }
            // Dark notch nestled in the crotch of the fork.
            Block(root, "ClawSlot", tip - tanDir * 0.004f, new Vector3(0.016f, 0.016f, 0.014f), darkMat)
                .localEulerAngles = new Vector3(0f, 0f, tanDeg);

            // ---- Angled flat pry / chisel tip at the -X end ----
            // A short bend off the shaft, then a wide thin blade tapering to a bright chisel edge with a slot.
            Rod(root, "Bend", new Vector3(-0.243f, 0.009f, 0f), new Vector3(0f, 0f, 68f), shaftD * 0.9f, 0.05f, steelMat);
            Block(root, "Chisel", new Vector3(-0.294f, 0.03f, 0f), new Vector3(0.06f, 0.013f, 0.05f), steelMat)
                .localEulerAngles = new Vector3(0f, 0f, -22f);
            Block(root, "ChiselEdge", new Vector3(-0.322f, 0.041f, 0f), new Vector3(0.018f, 0.007f, 0.052f), brightMat)
                .localEulerAngles = new Vector3(0f, 0f, -22f);
            Block(root, "ChiselSlot", new Vector3(-0.326f, 0.043f, 0f), new Vector3(0.02f, 0.016f, 0.012f), darkMat)
                .localEulerAngles = new Vector3(0f, 0f, -22f);

            return root;
        }

        /// <summary>
        /// A wooden baseball bat lying flat — the fifth melee weapon with a bespoke model. A lathe-turned
        /// club with a rounded knob and a thin handle that swells through a smooth taper into a fat barrel
        /// doming off at the far end, finished with a painted trademark ring and a branded oval on the
        /// barrel. The item's warm wood tint paints the bare timber; the knob is a shade darker and the cut
        /// barrel end shows lighter end-grain. Laid along X with the barrel as the +X business end and the
        /// bat roughly balanced on the spin pivot so it sweeps cleanly as the pickup turns.
        /// </summary>
        private static PartNode BuildBaseballBat(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Lies on its side; the fat barrel is the widest part, so it hovers just clear of the floor and
            // the bob never dips the barrel through it (0.12 - 0.06 bob - 0.035 barrel radius > 0).
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color wood = def.Tint;                                                       // bare turned timber
            Color woodDark = new Color(wood.r * 0.72f, wood.g * 0.72f, wood.b * 0.72f);  // knob and shaded butt
            Color grain = Color.Lerp(wood, Color.white, 0.28f);                          // lighter cut end-grain
            Color ring = new Color(0.55f, 0.12f, 0.12f);                                 // painted trademark ring
            Color logo = new Color(0.16f, 0.12f, 0.10f);                                 // burnt-in barrel logo

            PartMat woodMat = PartMat.Of(wood, smoothness: 0.32f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.28f);
            PartMat grainMat = PartMat.Of(grain, smoothness: 0.3f);
            PartMat ringMat = PartMat.Of(ring, smoothness: 0.5f);
            PartMat logoMat = PartMat.Of(logo, smoothness: 0.35f);

            const float lay = 90f;   // lays a Rod's length along X (the bat's long axis)

            // The classic bat silhouette as diameter keypoints from the handle (-X) to the barrel (+X),
            // interpolated for each turned segment so the profile reads as one smooth lathe curve.
            float[] xs = { -0.29f, -0.16f, -0.06f, 0.02f, 0.10f, 0.20f, 0.27f, 0.30f };
            float[] ds = { 0.030f, 0.031f, 0.042f, 0.056f, 0.066f, 0.070f, 0.068f, 0.058f };

            float Profile(float x)
            {
                if (x <= xs[0]) return ds[0];
                for (int k = 1; k < xs.Length; k++)
                {
                    if (x <= xs[k])
                    {
                        float t = (x - xs[k - 1]) / (xs[k] - xs[k - 1]);
                        return Mathf.Lerp(ds[k - 1], ds[k], t);
                    }
                }
                return ds[ds.Length - 1];
            }

            // ---- Turned wooden body: a chain of short coaxial discs following the profile ----
            const int seg = 24;
            const float bx0 = -0.29f, bx1 = 0.30f;
            float step = (bx1 - bx0) / seg;
            for (int i = 0; i <= seg; i++)
            {
                float x = bx0 + step * i;
                Rod(root, "Body", new Vector3(x, 0f, 0f), new Vector3(0f, 0f, lay), Profile(x), step * 1.9f, woodMat);
            }

            // ---- Knob at the -X end: a flared collar rounded off with a butt cap ----
            Rod(root, "KnobFlare", new Vector3(-0.298f, 0f, 0f), new Vector3(0f, 0f, lay), 0.052f, 0.018f, woodDarkMat);
            Sphere(root, "Knob", new Vector3(-0.312f, 0f, 0f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), woodDarkMat);

            // ---- Domed barrel end at the +X end, capped by a lighter end-grain face ----
            Sphere(root, "EndCap", new Vector3(0.305f, 0f, 0f), Vector3.zero, new Vector3(0.062f, 0.066f, 0.066f), woodMat);
            Sphere(root, "EndGrain", new Vector3(0.328f, 0f, 0f), Vector3.zero, new Vector3(0.028f, 0.05f, 0.05f), grainMat);

            // ---- Painted trademark ring on the taper and a branded oval on the barrel crown ----
            Rod(root, "Ring", new Vector3(0.065f, 0f, 0f), new Vector3(0f, 0f, lay), Profile(0.065f) + 0.003f, 0.012f, ringMat);
            Sphere(root, "Logo", new Vector3(0.19f, 0.033f, 0f), Vector3.zero, new Vector3(0.075f, 0.012f, 0.045f), logoMat);

            return root;
        }

        /// <summary>
        /// A firefighter's pick-head fire axe lying flat on its cheek — the sixth melee weapon with a
        /// bespoke model. A long wooden haft runs the length with a broad, red-painted head at the far end:
        /// a fanned bit sweeping forward to a bright bare-steel cutting edge, and a tapering steel pick
        /// rising from the back of the head over the handle. The item's red tint paints the head — a real
        /// fire axe's signature colour, and the reason the axe uses its tint on the head (not, like the
        /// knife or crowbar, on a steel blade) — while the haft is a fixed hickory brown and the honed edge,
        /// pick and wedge are bare steel. Laid along X with the head at the +X end and the axe roughly
        /// balanced on the spin pivot so it sweeps cleanly as the pickup turns.
        /// </summary>
        private static PartNode BuildFireAxe(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Lies on its cheek; the head and haft rest near the floor and only the pick rises above them,
            // so it hovers low without the bob dipping the head through the floor (0.12 - 0.06 bob - 0.03
            // half-head > 0).
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color paint = def.Tint;                                                        // red-painted head
            Color paintDeep = new Color(paint.r * 0.7f, paint.g * 0.7f, paint.b * 0.7f);   // shaded paint / cheek hollow
            Color steel = new Color(0.62f, 0.64f, 0.70f);                                  // bare forged steel (pick, wedge)
            Color bright = new Color(0.86f, 0.88f, 0.93f);                                 // honed cutting edge
            Color dark = new Color(0.34f, 0.35f, 0.38f);                                   // blackened pick point
            Color wood = new Color(0.55f, 0.36f, 0.20f);                                   // hickory haft
            Color woodDark = new Color(0.40f, 0.26f, 0.14f);                               // shaded collar and knob
            Color rubber = new Color(0.12f, 0.12f, 0.13f);                                 // moulded grip near the butt

            PartMat paintMat = PartMat.Of(paint, smoothness: 0.35f, metallic: 0.2f);
            PartMat paintDeepMat = PartMat.Of(paintDeep, smoothness: 0.3f, metallic: 0.2f);
            PartMat steelMat = PartMat.Of(steel, smoothness: 0.6f, metallic: 0.85f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.85f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.45f, metallic: 0.7f);
            PartMat woodMat = PartMat.Of(wood, smoothness: 0.24f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.2f);
            PartMat rubberMat = PartMat.Of(rubber, smoothness: 0.4f);

            const float lay = 90f;   // lays a Rod's length along X (the haft)

            // ---- Wooden haft running along X, the head at the +X end ----
            // Main shaft, passing up into the head's eye at the +X end.
            Rod(root, "Haft", new Vector3(-0.05f, 0f, 0f), new Vector3(0f, 0f, lay), 0.026f, 0.64f, woodMat);
            // A swelling shoulder just below the head, where a real haft thickens into the eye.
            Rod(root, "Shoulder", new Vector3(0.22f, 0f, 0f), new Vector3(0f, 0f, lay), 0.032f, 0.06f, woodMat);
            // Moulded rubber grip near the butt, bracketed by two collars, with a flare and a rounded knob.
            Rod(root, "Grip", new Vector3(-0.30f, 0f, 0f), new Vector3(0f, 0f, lay), 0.03f, 0.1f, rubberMat);
            Rod(root, "CollarTop", new Vector3(-0.253f, 0f, 0f), new Vector3(0f, 0f, lay), 0.033f, 0.008f, woodDarkMat);
            Rod(root, "CollarBottom", new Vector3(-0.347f, 0f, 0f), new Vector3(0f, 0f, lay), 0.033f, 0.008f, woodDarkMat);
            Rod(root, "ButtFlare", new Vector3(-0.36f, 0f, 0f), new Vector3(0f, 0f, lay), 0.034f, 0.02f, woodDarkMat);
            Sphere(root, "Knob", new Vector3(-0.375f, 0f, 0f), Vector3.zero, new Vector3(0.036f, 0.034f, 0.034f), woodDarkMat);

            // ---- Red-painted head at the +X end ----
            // The eye: the bulky centre the haft is driven through, its cheeks rounded off.
            Block(root, "Eye", new Vector3(0.29f, 0f, 0f), new Vector3(0.07f, 0.055f, 0.052f), paintMat);
            Sphere(root, "EyeRound", new Vector3(0.29f, 0f, 0f), Vector3.zero, new Vector3(0.075f, 0.058f, 0.06f), paintMat);
            // Bright steel wedge pinning the haft, stood proud of the top of the eye.
            Block(root, "Wedge", new Vector3(0.29f, 0.03f, 0f), new Vector3(0.02f, 0.014f, 0.05f), steelMat);

            // The bit: a fan sweeping forward from the eye, widening across Z and thinning in Y toward the edge.
            Block(root, "BitNeck", new Vector3(0.335f, 0f, 0f), new Vector3(0.05f, 0.045f, 0.09f), paintMat);
            Sphere(root, "BitFan", new Vector3(0.385f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.036f, 0.18f), paintMat);
            // A shaded hollow ground into each cheek so the flat fan reads as a bit, not a slab.
            Sphere(root, "CheekHollowTop", new Vector3(0.37f, 0.016f, 0f), Vector3.zero, new Vector3(0.08f, 0.014f, 0.12f), paintDeepMat);
            Sphere(root, "CheekHollowBottom", new Vector3(0.37f, -0.016f, 0f), Vector3.zero, new Vector3(0.08f, 0.014f, 0.12f), paintDeepMat);

            // Bright honed cutting edge hugging the fan's +X rim, with the toe and heel corners picked out.
            Sphere(root, "Edge", new Vector3(0.43f, 0f, 0f), Vector3.zero, new Vector3(0.03f, 0.02f, 0.17f), brightMat);
            Sphere(root, "Toe", new Vector3(0.414f, 0f, 0.083f), Vector3.zero, new Vector3(0.03f, 0.018f, 0.03f), brightMat);
            Sphere(root, "Heel", new Vector3(0.414f, 0f, -0.083f), Vector3.zero, new Vector3(0.03f, 0.018f, 0.03f), brightMat);

            // ---- Tapering steel pick rising from the back of the head, up and over the haft ----
            // (A Rod points along +Y at rest; euler Z = 70 tilts its axis to ~160 deg from +X, so the pick
            // leans up and back off the eye — the signature of a fireman's pick-head axe.)
            Block(root, "PickBase", new Vector3(0.258f, 0.014f, 0f), new Vector3(0.03f, 0.03f, 0.03f), steelMat);
            Rod(root, "PickShaft", new Vector3(0.226f, 0.028f, 0f), new Vector3(0f, 0f, 70f), 0.028f, 0.06f, steelMat);
            Rod(root, "PickTip", new Vector3(0.19f, 0.048f, 0f), new Vector3(0f, 0f, 70f), 0.016f, 0.055f, steelMat);
            Sphere(root, "PickPoint", new Vector3(0.166f, 0.062f, 0f), Vector3.zero, new Vector3(0.014f, 0.014f, 0.014f), darkMat);

            return root;
        }

        /// <summary>
        /// A semi-automatic M9 pistol lying flat on its side — the first firearm with a bespoke model
        /// (firearms otherwise fall back to a plain flat box). The classic side profile reads clearly from
        /// the overhead camera: a slab slide with an open top showing the barrel, an ejection port, front
        /// and rear sights and rear cocking serrations; a frame below carrying the trigger inside its guard,
        /// the slide stop, decocker and takedown pin; and a raked, checkered grip running down to a magazine
        /// floorplate. The item's dark gunmetal tint paints the slide and frame, with the barrel, hammer and
        /// controls picked out in lighter steel and the grip panels near-black. Laid along X with the muzzle
        /// at +X, the sights up along +Z and the flat right side facing the camera.
        /// </summary>
        private static PartNode BuildPistol(ItemDef def, PartNode parent, out float baseHeight)
        {
            // A thin, flat object resting on its side, so it hovers low and the gentle bob never dips it
            // through the floor. Its bulk lies in the X-Z plane (the side profile); Y is only its thickness.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color gm = def.Tint;                                                     // dark gunmetal slide + frame
            Color gmDark = new Color(gm.r * 0.6f, gm.g * 0.6f, gm.b * 0.6f);         // shaded gunmetal
            Color steel = new Color(0.42f, 0.44f, 0.48f);                            // barrel, hammer, controls
            Color dark = new Color(0.08f, 0.08f, 0.09f);                             // ports, serrations, bore
            Color gripDark = new Color(0.12f, 0.12f, 0.13f);                         // grip core
            Color panel = new Color(0.16f, 0.15f, 0.15f);                            // checkered grip panels

            PartMat gmMat = PartMat.Of(gm, smoothness: 0.42f, metallic: 0.75f);
            PartMat gmDarkMat = PartMat.Of(gmDark, smoothness: 0.4f, metallic: 0.7f);
            PartMat steelMat = PartMat.Of(steel, smoothness: 0.6f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.3f, metallic: 0.4f);
            PartMat gripMat = PartMat.Of(gripDark, smoothness: 0.35f);
            PartMat panelMat = PartMat.Of(panel, smoothness: 0.28f);

            const float lay = 90f;   // lays a Rod's length along X (the bore)
            const float side = 0.015f;   // half the gun's thickness — controls sit a hair proud of this face

            // ---- Slide: the slab top assembly running along X, the muzzle at +X ----
            Block(root, "Slide", new Vector3(-0.008f, 0f, 0.052f), new Vector3(0.205f, 0.03f, 0.032f), gmMat);
            // A rounded dorsal along the top of the slide.
            Rod(root, "SlideTop", new Vector3(-0.008f, 0f, 0.066f), new Vector3(0f, 0f, lay), 0.03f, 0.19f, gmMat);
            // Open top forward of the breech (the Beretta's signature cut) revealing the barrel below.
            Block(root, "OpenTop", new Vector3(0.028f, 0f, 0.066f), new Vector3(0.11f, 0.022f, 0.012f), darkMat);
            // Exposed barrel showing through the open top and poking out at the muzzle, capped by a dark bore.
            Rod(root, "Barrel", new Vector3(0.03f, 0f, 0.062f), new Vector3(0f, 0f, lay), 0.02f, 0.165f, steelMat);
            Rod(root, "Bore", new Vector3(0.112f, 0f, 0.062f), new Vector3(0f, 0f, lay), 0.012f, 0.006f, darkMat);
            // Ejection port cut into the right (camera-facing) side of the slide.
            Block(root, "EjectionPort", new Vector3(0.03f, side + 0.001f, 0.056f), new Vector3(0.05f, 0.008f, 0.022f), darkMat);
            // Front and rear sights standing off the top edge, with a dark notch in the rear sight.
            Block(root, "FrontSight", new Vector3(0.078f, 0f, 0.076f), new Vector3(0.008f, 0.02f, 0.014f), gmMat);
            Block(root, "RearSight", new Vector3(-0.088f, 0f, 0.074f), new Vector3(0.016f, 0.022f, 0.014f), gmMat);
            Block(root, "RearNotch", new Vector3(-0.088f, 0f, 0.083f), new Vector3(0.004f, 0.024f, 0.008f), darkMat);
            // Rear cocking serrations grooved into the side of the slide.
            for (int i = 0; i < 4; i++)
            {
                float sx = -0.058f - i * 0.011f;
                Block(root, "Serration", new Vector3(sx, side + 0.001f, 0.052f), new Vector3(0.004f, 0.006f, 0.03f), darkMat);
            }
            // Exposed spur hammer at the rear of the frame.
            Block(root, "Hammer", new Vector3(-0.112f, 0f, 0.05f), new Vector3(0.012f, 0.016f, 0.022f), steelMat);
            Sphere(root, "HammerSpur", new Vector3(-0.117f, 0f, 0.062f), Vector3.zero, new Vector3(0.014f, 0.016f, 0.016f), steelMat);

            // ---- Frame beneath the slide, carrying the controls and trigger guard ----
            Block(root, "Frame", new Vector3(0.02f, 0f, 0.028f), new Vector3(0.155f, 0.024f, 0.022f), gmMat);
            // Slide stop and its pin on the frame side.
            Block(root, "SlideStop", new Vector3(-0.03f, side + 0.001f, 0.03f), new Vector3(0.03f, 0.006f, 0.01f), steelMat);
            Sphere(root, "TakedownPin", new Vector3(0.008f, side + 0.001f, 0.028f), Vector3.zero, new Vector3(0.012f, 0.006f, 0.012f), steelMat);
            // Frame-mounted decocker / safety lever at the rear.
            Block(root, "Safety", new Vector3(-0.082f, side + 0.001f, 0.042f), new Vector3(0.02f, 0.006f, 0.012f), steelMat);
            Sphere(root, "SafetyLever", new Vector3(-0.09f, side + 0.002f, 0.042f), Vector3.zero, new Vector3(0.014f, 0.008f, 0.016f), steelMat);
            // Trigger guard: a front strut and a bottom bar enclosing the trigger, closed off at the rear.
            Block(root, "GuardFront", new Vector3(0.05f, 0f, -0.002f), new Vector3(0.012f, 0.024f, 0.05f), gmMat);
            Block(root, "GuardBottom", new Vector3(0.014f, 0f, -0.022f), new Vector3(0.085f, 0.022f, 0.012f), gmMat);
            Block(root, "GuardRear", new Vector3(-0.03f, 0f, -0.012f), new Vector3(0.014f, 0.022f, 0.024f), gmDarkMat);
            Block(root, "Trigger", new Vector3(0.034f, 0f, -0.004f), new Vector3(0.008f, 0.018f, 0.022f), steelMat);

            // ---- Raked, checkered grip hanging from the rear of the frame, holding the magazine ----
            // A yawed sub-root lets the whole grip rake forward at the bottom (as a pistol grip does) while
            // its parts are placed simply, straight down its own -Z, in the sub-root's local frame.
            var grip = new PartNode("Grip");
            grip.SetParent(root, false);
            grip.localPosition = new Vector3(-0.062f, 0f, 0.012f);
            grip.localEulerAngles = new Vector3(0f, -16f, 0f);   // rakes the mag end forward, like a real grip

            Block(grip, "GripCore", new Vector3(0f, 0f, -0.052f), new Vector3(0.05f, 0.03f, 0.105f), gripMat);
            Sphere(grip, "Backstrap", new Vector3(-0.018f, 0f, 0.004f), Vector3.zero, new Vector3(0.03f, 0.032f, 0.03f), gripMat);
            Block(grip, "PanelRight", new Vector3(0.002f, 0.016f, -0.05f), new Vector3(0.032f, 0.005f, 0.08f), panelMat);
            Block(grip, "PanelLeft", new Vector3(0.002f, -0.016f, -0.05f), new Vector3(0.032f, 0.005f, 0.08f), panelMat);
            Sphere(grip, "GripScrew", new Vector3(0.002f, 0.019f, -0.05f), Vector3.zero, new Vector3(0.01f, 0.005f, 0.01f), steelMat);
            // Magazine release button high on the grip, behind the trigger guard.
            Block(grip, "MagRelease", new Vector3(0.022f, 0.016f, -0.012f), new Vector3(0.012f, 0.006f, 0.012f), steelMat);
            // Magazine floorplate protruding slightly at the bottom of the grip.
            Block(grip, "MagBase", new Vector3(0.005f, 0f, -0.108f), new Vector3(0.052f, 0.033f, 0.016f), gmDarkMat);

            return root;
        }

        /// <summary>
        /// A pump-action shotgun lying flat on its side — the second firearm with a bespoke model. The
        /// item's warm brown tint paints the wooden buttstock and the ribbed pump forend, while the barrel,
        /// magazine tube, receiver and exposed action bar are picked out in dark blued steel, the trigger
        /// and controls in lighter steel, and a small brass bead sits on the barrel rib at the muzzle. Laid
        /// along X with the muzzle at +X, the sights up along +Z and the flat side facing the camera, so the
        /// long side profile reads clearly from the overhead view.
        /// </summary>
        private static PartNode BuildShotgun(ItemDef def, PartNode parent, out float baseHeight)
        {
            // A long but thin object resting on its side, so it hovers low and the gentle bob never dips it
            // through the floor. Its bulk lies in the X-Z plane (the side profile); Y is only its thickness.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color wood = def.Tint;                                                        // walnut stock + forend
            Color woodDark = new Color(wood.r * 0.68f, wood.g * 0.68f, wood.b * 0.68f);   // checkering / spacer shading
            Color blued = new Color(0.13f, 0.13f, 0.15f);                                 // blued barrel, receiver, mag tube
            Color bluedHi = new Color(0.22f, 0.23f, 0.26f);                               // lit edges, muzzle band, top rib
            Color steel = new Color(0.42f, 0.44f, 0.48f);                                 // trigger, safety, magazine cap
            Color dark = new Color(0.05f, 0.05f, 0.06f);                                  // bore, ejection port, recoil pad
            Color bead = new Color(0.85f, 0.68f, 0.32f);                                  // brass front bead

            PartMat woodMat = PartMat.Of(wood, smoothness: 0.28f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.24f);
            PartMat bluedMat = PartMat.Of(blued, smoothness: 0.5f, metallic: 0.85f);
            PartMat bluedHiMat = PartMat.Of(bluedHi, smoothness: 0.55f, metallic: 0.85f);
            PartMat steelMat = PartMat.Of(steel, smoothness: 0.6f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.3f, metallic: 0.35f);
            PartMat beadMat = PartMat.Of(bead, smoothness: 0.5f, metallic: 0.7f);

            const float lay = 90f;      // lays a Rod's length along X (the bore)
            const float side = 0.024f;  // half the gun's thickness — controls sit a hair proud of this face

            // ---- Barrel running along X with the muzzle at +X, topped by a thin sighting rib ----
            Rod(root, "Barrel", new Vector3(0.13f, 0f, 0.024f), new Vector3(0f, 0f, lay), 0.032f, 0.44f, bluedMat);
            Block(root, "Rib", new Vector3(0.15f, 0f, 0.043f), new Vector3(0.34f, 0.015f, 0.009f), bluedMat);
            // A widened muzzle band with a dark bore, and a brass bead sight on the rib near the muzzle.
            Rod(root, "Muzzle", new Vector3(0.342f, 0f, 0.024f), new Vector3(0f, 0f, lay), 0.038f, 0.02f, bluedHiMat);
            Rod(root, "Bore", new Vector3(0.352f, 0f, 0.024f), new Vector3(0f, 0f, lay), 0.022f, 0.006f, darkMat);
            Sphere(root, "FrontBead", new Vector3(0.322f, 0f, 0.053f), Vector3.zero, new Vector3(0.012f, 0.012f, 0.012f), beadMat);

            // ---- Magazine tube slung under the barrel, capped at the front and tied to the barrel by a hanger ----
            Rod(root, "MagTube", new Vector3(0.12f, 0f, -0.006f), new Vector3(0f, 0f, lay), 0.024f, 0.40f, bluedMat);
            Rod(root, "MagCap", new Vector3(0.322f, 0f, -0.006f), new Vector3(0f, 0f, lay), 0.028f, 0.018f, steelMat);
            Block(root, "Hanger", new Vector3(0.27f, 0f, 0.009f), new Vector3(0.02f, 0.026f, 0.034f), bluedMat);

            // ---- Wooden pump forend wrapping the magazine tube, grooved like a corncob and linked back to the
            // receiver by the exposed action bar that sells the pump action ----
            Rod(root, "Forend", new Vector3(0.055f, 0f, -0.008f), new Vector3(0f, 0f, lay), 0.05f, 0.14f, woodMat);
            for (int i = 0; i < 4; i++)
            {
                float rx = 0.01f + i * 0.028f;
                Rod(root, "ForendRib", new Vector3(rx, 0f, -0.008f), new Vector3(0f, 0f, lay), 0.054f, 0.008f, woodDarkMat);
            }
            Block(root, "ActionBar", new Vector3(-0.03f, 0f, -0.019f), new Vector3(0.12f, 0.028f, 0.011f), bluedMat);

            // ---- Blued receiver in the middle, with a top-rib highlight and an ejection port on the near side ----
            Block(root, "Receiver", new Vector3(-0.15f, 0f, 0.006f), new Vector3(0.14f, 0.05f, 0.06f), bluedMat);
            Block(root, "ReceiverTop", new Vector3(-0.15f, 0f, 0.038f), new Vector3(0.13f, 0.048f, 0.011f), bluedHiMat);
            Block(root, "EjectionPort", new Vector3(-0.128f, side + 0.001f, 0.014f), new Vector3(0.055f, 0.008f, 0.024f), darkMat);

            // ---- Trigger inside its guard, with the crossbolt safety at the rear of the guard ----
            Block(root, "GuardBottom", new Vector3(-0.148f, 0f, -0.048f), new Vector3(0.075f, 0.02f, 0.011f), bluedMat);
            Block(root, "GuardFront", new Vector3(-0.114f, 0f, -0.036f), new Vector3(0.012f, 0.02f, 0.028f), bluedMat);
            Block(root, "GuardRear", new Vector3(-0.182f, 0f, -0.036f), new Vector3(0.012f, 0.02f, 0.028f), bluedMat);
            Block(root, "Trigger", new Vector3(-0.146f, 0f, -0.03f), new Vector3(0.008f, 0.015f, 0.02f), steelMat);
            Sphere(root, "Safety", new Vector3(-0.185f, side + 0.001f, -0.028f), Vector3.zero, new Vector3(0.011f, 0.008f, 0.011f), steelMat);

            // ---- Wooden buttstock: a wrist with a semi-pistol grip swell, then a comb and belly sweeping back
            // to a tall butt closed off by a pale spacer and a black recoil pad ----
            Block(root, "Wrist", new Vector3(-0.27f, 0f, -0.006f), new Vector3(0.1f, 0.048f, 0.048f), woodMat);
            Block(root, "GripCap", new Vector3(-0.246f, 0f, -0.04f), new Vector3(0.05f, 0.046f, 0.042f), woodMat);
            Block(root, "Comb", new Vector3(-0.35f, 0f, 0.004f), new Vector3(0.15f, 0.048f, 0.052f), woodMat);
            Block(root, "StockBelly", new Vector3(-0.33f, 0f, -0.028f), new Vector3(0.1f, 0.048f, 0.03f), woodMat);
            Block(root, "Butt", new Vector3(-0.42f, 0f, 0.002f), new Vector3(0.028f, 0.048f, 0.088f), woodMat);
            Block(root, "Spacer", new Vector3(-0.435f, 0f, 0.002f), new Vector3(0.006f, 0.046f, 0.09f), woodDarkMat);
            Block(root, "RecoilPad", new Vector3(-0.444f, 0f, 0.002f), new Vector3(0.015f, 0.046f, 0.094f), darkMat);

            // A darker checkered panel on the wrist, facing the camera.
            Block(root, "WristCheck", new Vector3(-0.272f, side + 0.001f, -0.006f), new Vector3(0.07f, 0.004f, 0.03f), woodDarkMat);

            return root;
        }

        /// <summary>
        /// An opened box of pistol ammunition: a kraft cardboard carton with its lid flipped up at the back,
        /// a printed label and a dark calibre placard on the front, a tray of brass 9mm cartridges standing
        /// nose-up inside, and a couple of loose rounds spilled in front. The cases carry the item's brass
        /// tint so the box reads unmistakably as ammunition where it lies on the floor.
        /// </summary>
        private static PartNode BuildPistolAmmo(ItemDef def, PartNode parent, out float baseHeight)
        {
            // A small box; hover its centre at the ammo primitive's height so the gentle bob never dips it
            // through the floor.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color brass = def.Tint;                                                          // cartridge cases (brass gold)
            Color brassDeep = new Color(brass.r * 0.66f, brass.g * 0.66f, brass.b * 0.66f);  // head, cannelure, shading
            Color copper = new Color(0.72f, 0.45f, 0.24f);                                   // FMJ bullet jacket
            Color primer = new Color(0.82f, 0.82f, 0.85f);                                   // nickel primer
            Color card = new Color(0.60f, 0.45f, 0.29f);                                     // kraft cardboard box
            Color cardDark = new Color(card.r * 0.8f, card.g * 0.8f, card.b * 0.8f);         // box interior / shading
            Color placard = new Color(0.16f, 0.15f, 0.14f);                                  // dark calibre placard
            Color cream = new Color(0.90f, 0.87f, 0.78f);                                    // label backing panel

            PartMat brassMat = PartMat.Of(brass, smoothness: 0.55f, metallic: 0.8f);
            PartMat brassDeepMat = PartMat.Of(brassDeep, smoothness: 0.5f, metallic: 0.8f);
            PartMat copperMat = PartMat.Of(copper, smoothness: 0.5f, metallic: 0.7f);
            PartMat primerMat = PartMat.Of(primer, smoothness: 0.6f, metallic: 0.9f);
            PartMat cardMat = PartMat.Of(card, smoothness: 0.06f);
            PartMat cardDarkMat = PartMat.Of(cardDark, smoothness: 0.06f);
            PartMat labelMat = PartMat.Of(def.Tint, smoothness: 0.12f);
            PartMat placardMat = PartMat.Of(placard, smoothness: 0.1f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.1f);

            // Lowers the whole arrangement so its mass centres on the root, which hovers at baseHeight.
            const float floor = -0.10f;         // ground plane the box and spilled rounds rest on
            const float slabH = 0.028f;         // box bottom-slab thickness
            const float wallH = 0.088f;         // box wall height above the slab
            const float wall = 0.016f;          // wall thickness
            const float boxW = 0.28f;           // box width  (X)
            const float boxD = 0.185f;          // box depth  (Z)
            float innerY = floor + slabH;       // interior floor the standing rounds sit on
            float wallCy = innerY + wallH * 0.5f;
            float wallTop = innerY + wallH;

            // ---- Cardboard carton: a bottom slab and four thin walls forming an open tray ----
            Block(root, "BoxBottom", new Vector3(0f, floor + slabH * 0.5f, 0f), new Vector3(boxW, slabH, boxD), cardDarkMat);
            Block(root, "WallFront", new Vector3(0f, wallCy, -(boxD * 0.5f - wall * 0.5f)), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallBack", new Vector3(0f, wallCy, boxD * 0.5f - wall * 0.5f), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallLeft", new Vector3(-(boxW * 0.5f - wall * 0.5f), wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);
            Block(root, "WallRight", new Vector3(boxW * 0.5f - wall * 0.5f, wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);

            // Printed label band, a cream backing panel and a dark calibre placard across the front wall.
            float frontZ = -(boxD * 0.5f) - 0.002f;
            Block(root, "Label", new Vector3(0f, innerY + 0.032f, frontZ), new Vector3(boxW - 0.03f, 0.05f, 0.005f), labelMat);
            Block(root, "LabelBase", new Vector3(0.075f, innerY + 0.032f, frontZ - 0.001f), new Vector3(0.1f, 0.036f, 0.006f), creamMat);
            Block(root, "Placard", new Vector3(-0.07f, innerY + 0.032f, frontZ - 0.002f), new Vector3(0.11f, 0.03f, 0.006f), placardMat);

            // ---- Lid flipped up and hinged at the back top edge, leaning open over the rear wall ----
            var lid = new PartNode("Lid");
            lid.SetParent(root, false);
            lid.localPosition = new Vector3(0f, wallTop, boxD * 0.5f - wall * 0.5f);
            lid.localEulerAngles = new Vector3(32f, 0f, 0f);   // tips the lid back and open
            Block(lid, "Panel", new Vector3(0f, 0.075f, 0f), new Vector3(boxW, 0.15f, 0.01f), cardMat);
            Block(lid, "PanelInner", new Vector3(0f, 0.075f, -0.006f), new Vector3(boxW - 0.02f, 0.14f, 0.004f), cardDarkMat);
            Block(lid, "LidLabel", new Vector3(0f, 0.088f, 0.006f), new Vector3(boxW - 0.04f, 0.075f, 0.004f), labelMat);

            // ---- A tray of cartridges standing nose-up inside the box (four across, two deep) ----
            float[] gx = { -0.093f, -0.031f, 0.031f, 0.093f };
            float[] gz = { -0.04f, 0.04f };
            int n = 0;
            for (int zi = 0; zi < gz.Length; zi++)
            {
                for (int xi = 0; xi < gx.Length; xi++)
                {
                    float yaw = (n % 2 == 0 ? 12f : -18f) + n * 5f;   // a slight per-round spin, purely cosmetic
                    BuildCartridge(root, new Vector3(gx[xi], innerY, gz[zi]), new Vector3(0f, yaw, 0f),
                        brassMat, brassDeepMat, copperMat, primerMat);
                    n++;
                }
            }

            // ---- A couple of loose rounds spilled on the ground in front of the box ----
            BuildCartridge(root, new Vector3(-0.10f, floor + 0.03f, -0.15f), new Vector3(90f, 78f, 0f),
                brassMat, brassDeepMat, copperMat, primerMat);
            BuildCartridge(root, new Vector3(0.10f, floor + 0.03f, -0.172f), new Vector3(90f, -104f, 0f),
                brassMat, brassDeepMat, copperMat, primerMat);

            return root;
        }

        /// <summary>
        /// A box of 12-gauge shotgun shells: a printed carton with its lid hinged open, holding a tray of
        /// fat plastic hulls standing brass-down and crimp-up, with a few loose shells spilled out in front.
        /// The hulls take the item's tint so the shells read as that ammunition where they lie on the floor.
        /// </summary>
        private static PartNode BuildShotgunShells(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the box at the ammo primitive's height so the gentle bob never dips it through the floor.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color hull = def.Tint;                                                          // plastic shell hull (shotshell red)
            Color hullDeep = new Color(hull.r * 0.55f, hull.g * 0.55f, hull.b * 0.55f);     // ridges, crimp seams, shading
            Color hullFold = Color.Lerp(hull, Color.white, 0.14f);                          // folded crimp catching the light
            Color brass = new Color(0.83f, 0.69f, 0.32f);                                   // brass head
            Color brassDeep = new Color(brass.r * 0.66f, brass.g * 0.66f, brass.b * 0.66f); // rim and head shading
            Color primer = new Color(0.82f, 0.82f, 0.85f);                                  // nickel primer
            Color card = new Color(0.20f, 0.42f, 0.27f);                                    // printed hunting-green carton
            Color cardDark = new Color(card.r * 0.78f, card.g * 0.78f, card.b * 0.78f);     // interior / shading
            Color placard = new Color(0.14f, 0.13f, 0.12f);                                 // dark gauge placard
            Color cream = new Color(0.90f, 0.87f, 0.78f);                                   // label backing panel

            PartMat hullMat = PartMat.Of(hull, smoothness: 0.42f);
            PartMat hullDeepMat = PartMat.Of(hullDeep, smoothness: 0.35f);
            PartMat hullFoldMat = PartMat.Of(hullFold, smoothness: 0.42f);
            PartMat brassMat = PartMat.Of(brass, smoothness: 0.55f, metallic: 0.8f);
            PartMat brassDeepMat = PartMat.Of(brassDeep, smoothness: 0.5f, metallic: 0.8f);
            PartMat primerMat = PartMat.Of(primer, smoothness: 0.6f, metallic: 0.9f);
            PartMat cardMat = PartMat.Of(card, smoothness: 0.08f);
            PartMat cardDarkMat = PartMat.Of(cardDark, smoothness: 0.08f);
            PartMat labelMat = PartMat.Of(def.Tint, smoothness: 0.14f);
            PartMat placardMat = PartMat.Of(placard, smoothness: 0.1f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.1f);

            // Lowers the whole arrangement so its mass centres on the root, which hovers at baseHeight.
            const float floor = -0.10f;    // ground plane the box and spilled shells rest on
            const float slabH = 0.03f;     // box bottom-slab thickness
            const float wallH = 0.07f;     // box wall height above the slab (fat shells poke well above it)
            const float wall = 0.016f;     // wall thickness
            const float boxW = 0.255f;     // box width  (X)
            const float boxD = 0.195f;     // box depth  (Z)
            float innerY = floor + slabH;  // interior floor the standing shells sit on
            float wallCy = innerY + wallH * 0.5f;
            float wallTop = innerY + wallH;

            // ---- Printed carton: a bottom slab and four thin walls forming an open tray ----
            Block(root, "BoxBottom", new Vector3(0f, floor + slabH * 0.5f, 0f), new Vector3(boxW, slabH, boxD), cardDarkMat);
            Block(root, "WallFront", new Vector3(0f, wallCy, -(boxD * 0.5f - wall * 0.5f)), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallBack", new Vector3(0f, wallCy, boxD * 0.5f - wall * 0.5f), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallLeft", new Vector3(-(boxW * 0.5f - wall * 0.5f), wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);
            Block(root, "WallRight", new Vector3(boxW * 0.5f - wall * 0.5f, wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);

            // Printed label band, a cream backing panel and a dark gauge placard across the front wall.
            float frontZ = -(boxD * 0.5f) - 0.002f;
            Block(root, "Label", new Vector3(0f, innerY + 0.03f, frontZ), new Vector3(boxW - 0.03f, 0.048f, 0.005f), labelMat);
            Block(root, "LabelBase", new Vector3(0.06f, innerY + 0.03f, frontZ - 0.001f), new Vector3(0.09f, 0.034f, 0.006f), creamMat);
            Block(root, "Placard", new Vector3(-0.066f, innerY + 0.03f, frontZ - 0.002f), new Vector3(0.09f, 0.028f, 0.006f), placardMat);

            // ---- Lid flipped up and hinged at the back top edge, leaning open over the rear wall ----
            var lid = new PartNode("Lid");
            lid.SetParent(root, false);
            lid.localPosition = new Vector3(0f, wallTop, boxD * 0.5f - wall * 0.5f);
            lid.localEulerAngles = new Vector3(34f, 0f, 0f);   // tips the lid back and open
            Block(lid, "Panel", new Vector3(0f, 0.075f, 0f), new Vector3(boxW, 0.15f, 0.01f), cardMat);
            Block(lid, "PanelInner", new Vector3(0f, 0.075f, -0.006f), new Vector3(boxW - 0.02f, 0.14f, 0.004f), cardDarkMat);
            Block(lid, "LidLabel", new Vector3(0f, 0.086f, 0.006f), new Vector3(boxW - 0.05f, 0.07f, 0.004f), labelMat);

            // ---- A tray of shells standing crimp-up inside the box (three across, two deep) ----
            float[] gx = { -0.075f, 0f, 0.075f };
            float[] gz = { -0.045f, 0.045f };
            int n = 0;
            for (int zi = 0; zi < gz.Length; zi++)
            {
                for (int xi = 0; xi < gx.Length; xi++)
                {
                    float yaw = (n % 2 == 0 ? 18f : -24f) + n * 11f;   // a per-shell spin so the star crimps vary
                    BuildShotgunShell(root, new Vector3(gx[xi], innerY, gz[zi]), new Vector3(0f, yaw, 0f),
                        hullMat, hullDeepMat, hullFoldMat, brassMat, brassDeepMat, primerMat);
                    n++;
                }
            }

            // ---- A few loose shells spilled on the ground in front of the box ----
            BuildShotgunShell(root, new Vector3(-0.115f, floor + 0.036f, -0.16f), new Vector3(90f, 64f, 0f),
                hullMat, hullDeepMat, hullFoldMat, brassMat, brassDeepMat, primerMat);
            BuildShotgunShell(root, new Vector3(0.02f, floor + 0.036f, -0.19f), new Vector3(90f, -98f, 0f),
                hullMat, hullDeepMat, hullFoldMat, brassMat, brassDeepMat, primerMat);
            BuildShotgunShell(root, new Vector3(0.14f, floor + 0.036f, -0.14f), new Vector3(90f, 128f, 0f),
                hullMat, hullDeepMat, hullFoldMat, brassMat, brassDeepMat, primerMat);

            return root;
        }

        /// <summary>Builds one 9mm cartridge under <paramref name="parent"/> in its own position and tilt
        /// frame: a brass case on a rimmed head with a nickel primer, capped by a copper round-nose bullet.
        /// Built standing on its base so callers can stand a trayful nose-up or lay a few flat by tilting the
        /// frame (a quarter-turn about X lays it on its side).</summary>
        private static void BuildCartridge(PartNode parent, Vector3 localPos, Vector3 euler,
            PartMat brassMat, PartMat brassDeepMat, PartMat copperMat, PartMat primerMat)
        {
            var c = new PartNode("Cartridge");
            c.SetParent(parent, false);
            c.localPosition = localPos;
            c.localEulerAngles = euler;

            const float caseD = 0.052f;    // case-body diameter
            const float headD = 0.058f;    // rimmed head, a hair wider than the body
            const float caseBase = 0.022f; // top of the head, where the case body begins
            const float caseTop = 0.125f;  // height of the case mouth above the base
            const float bodyH = caseTop - caseBase;

            // Rimmed brass head with a nickel primer set into the base.
            Cylinder(c, "Head", 0.011f, headD, 0.022f, brassDeepMat);
            Cylinder(c, "Primer", 0.004f, 0.026f, 0.012f, primerMat);
            // Brass case body up to the mouth, with a darker cannelure ring near the neck.
            Cylinder(c, "Case", caseBase + bodyH * 0.5f, caseD, bodyH, brassMat);
            Cylinder(c, "Cannelure", caseTop - 0.01f, caseD + 0.001f, 0.008f, brassDeepMat);
            // Copper full-metal-jacket bullet seated in the mouth, with a rounded nose.
            Cylinder(c, "Bullet", caseTop + 0.008f, 0.05f, 0.03f, copperMat);
            Sphere(c, "BulletNose", new Vector3(0f, caseTop + 0.028f, 0f), Vector3.zero, new Vector3(0.05f, 0.062f, 0.05f), copperMat);
        }

        /// <summary>Builds one 12-gauge shotgun shell under <paramref name="parent"/> in its own position and
        /// tilt frame: a brass head with a nickel primer, a plastic hull, and a folded six-point star crimp at
        /// the mouth. Built standing on its base so callers can stand a trayful crimp-up or lay a few flat by
        /// tilting the frame (a quarter-turn about X lays it on its side).</summary>
        private static void BuildShotgunShell(PartNode parent, Vector3 localPos, Vector3 euler,
            PartMat hullMat, PartMat hullDeepMat, PartMat hullFoldMat,
            PartMat brassMat, PartMat brassDeepMat, PartMat primerMat)
        {
            var s = new PartNode("Shell");
            s.SetParent(parent, false);
            s.localPosition = localPos;
            s.localEulerAngles = euler;

            const float headD = 0.076f;    // brass head (the widest part, forms the rim)
            const float hullD = 0.07f;     // plastic hull body
            const float crownD = 0.062f;   // crimped mouth, drawn in a little from the hull
            const float brassH = 0.052f;   // height of the brass head
            const float hullTop = 0.152f;  // height of the crimp above the base

            // Brass head: a rolled extractor rim at the very base, the head wall, and a shading groove where
            // the brass meets the hull, with a nickel primer poking out of the centre of the base.
            Cylinder(s, "Rim", 0.006f, headD + 0.004f, 0.012f, brassDeepMat);
            Cylinder(s, "Head", brassH * 0.5f, headD, brassH, brassMat);
            Cylinder(s, "BrassGroove", brassH - 0.006f, headD + 0.002f, 0.006f, brassDeepMat);
            Cylinder(s, "Primer", 0f, 0.03f, 0.014f, primerMat);

            // Plastic hull, sunk a little into the brass so there's no seam, with a ridge line and a mouth
            // that draws in to the crimp.
            float hullBottom = brassH - 0.014f;
            float hullH = hullTop - 0.012f - hullBottom;
            Cylinder(s, "Hull", hullBottom + hullH * 0.5f, hullD, hullH, hullMat);
            Cylinder(s, "Ridge", brassH + 0.024f, hullD + 0.001f, 0.005f, hullDeepMat);
            Cylinder(s, "Mouth", hullTop - 0.016f, crownD, 0.02f, hullMat);

            // Folded star crimp: a flat cap crossed by three seam lines (a six-point star) with a dimple where
            // the folds meet in the centre.
            Cylinder(s, "CrimpTop", hullTop - 0.002f, crownD, 0.01f, hullFoldMat);
            for (int i = 0; i < 3; i++)
            {
                var seam = Block(s, "CrimpSeam" + i, new Vector3(0f, hullTop + 0.004f + i * 0.001f, 0f),
                    new Vector3(crownD * 0.9f, 0.005f, 0.013f), hullDeepMat);
                seam.localEulerAngles = new Vector3(0f, i * 60f, 0f);
            }
            Sphere(s, "CrimpCentre", new Vector3(0f, hullTop + 0.006f, 0f), Vector3.zero, new Vector3(0.016f, 0.01f, 0.016f), hullDeepMat);
        }

        /// <summary>
        /// A sawn wooden plank: a long, flat board carrying the item's wood tint, grounded by a shaded
        /// underside and finished with pale cross-cut end grain at each sawn end, darker and lighter grain
        /// streaks running its length, a couple of knots with their swirl, and a fine end-check split — so a
        /// dropped building plank reads unmistakably as a board of lumber where it lies on the floor.
        /// </summary>
        private static PartNode BuildPlank(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for a thin board lying flat, matching the tools and loaf beside it.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color wood = def.Tint;                                                       // planed board face
            Color woodBase = new Color(wood.r * 0.62f, wood.g * 0.58f, wood.b * 0.52f);    // shaded sawn underside
            Color grainDark = new Color(wood.r * 0.74f, wood.g * 0.72f, wood.b * 0.68f);   // darker grain line / eased edge
            Color grainHi = Color.Lerp(wood, new Color(0.82f, 0.71f, 0.55f), 0.30f);       // lighter grain streak
            Color endGrain = Color.Lerp(wood, new Color(0.80f, 0.70f, 0.56f), 0.42f);      // pale cross-cut end
            Color knotDeep = new Color(wood.r * 0.46f, wood.g * 0.40f, wood.b * 0.32f);    // dark dense knot
            Color knotRing = new Color(wood.r * 0.66f, wood.g * 0.60f, wood.b * 0.50f);    // swirl around a knot
            Color split = new Color(wood.r * 0.38f, wood.g * 0.34f, wood.b * 0.28f);       // shadowed end-check crack

            PartMat woodMat = PartMat.Of(wood, smoothness: 0.12f);       // matte softwood
            PartMat baseMat = PartMat.Of(woodBase, smoothness: 0.08f);
            PartMat darkMat = PartMat.Of(grainDark, smoothness: 0.10f);
            PartMat hiMat = PartMat.Of(grainHi, smoothness: 0.14f);
            PartMat endMat = PartMat.Of(endGrain, smoothness: 0.06f);    // rough cross-cut fibres
            PartMat knotMat = PartMat.Of(knotDeep, smoothness: 0.18f);   // knots are denser and glossier
            PartMat knotRingMat = PartMat.Of(knotRing, smoothness: 0.12f);
            PartMat splitMat = PartMat.Of(split, smoothness: 0.08f);

            const float len = 0.62f;      // board length (X)
            const float thick = 0.034f;   // board thickness (Y)
            const float wid = 0.145f;     // board width (Z)
            const float top = thick * 0.5f;

            // Main board.
            Block(root, "Body", Vector3.zero, new Vector3(len, thick, wid), woodMat);

            // Shaded underside peeking out beneath the board so it looks solid, not floating.
            Block(root, "Base", new Vector3(0f, -0.015f, 0f), new Vector3(len - 0.02f, 0.010f, wid - 0.01f), baseMat);

            // Pale cross-cut end grain, nudged a hair proud of each sawn end and inset on the other faces so
            // only the end reads as raw fibres.
            Block(root, "EndCapPos", new Vector3(len * 0.5f - 0.004f, 0f, 0f), new Vector3(0.012f, thick - 0.003f, wid - 0.004f), endMat);
            Block(root, "EndCapNeg", new Vector3(-(len * 0.5f - 0.004f), 0f, 0f), new Vector3(0.012f, thick - 0.003f, wid - 0.004f), endMat);

            // Grain streaks running the length of the top face, alternating darker and lighter so the board
            // shows figure rather than a flat colour; a couple are yawed a touch so the grain isn't dead straight.
            Block(root, "Grain1", new Vector3(-0.02f, top - 0.001f, -0.050f), new Vector3(0.54f, 0.005f, 0.010f), darkMat).localEulerAngles = new Vector3(0f, 2f, 0f);
            Block(root, "Grain2", new Vector3(0.01f, top - 0.001f, -0.018f), new Vector3(0.50f, 0.005f, 0.008f), hiMat);
            Block(root, "Grain3", new Vector3(0.0f, top - 0.001f, 0.020f), new Vector3(0.52f, 0.005f, 0.011f), darkMat).localEulerAngles = new Vector3(0f, -2f, 0f);
            Block(root, "Grain4", new Vector3(-0.015f, top - 0.001f, 0.052f), new Vector3(0.48f, 0.005f, 0.009f), hiMat);

            // Eased long edges, shaded a touch darker like a chamfered plank edge.
            Block(root, "EdgeNear", new Vector3(0f, top - 0.002f, wid * 0.5f - 0.006f), new Vector3(len - 0.03f, 0.006f, 0.008f), darkMat);
            Block(root, "EdgeFar", new Vector3(0f, top - 0.002f, -(wid * 0.5f - 0.006f)), new Vector3(len - 0.03f, 0.006f, 0.008f), darkMat);

            // A big knot with its grain swirl near one end, and a smaller one further along.
            Sphere(root, "KnotRing1", new Vector3(0.11f, top - 0.001f, -0.026f), Vector3.zero, new Vector3(0.058f, 0.008f, 0.046f), knotRingMat);
            Sphere(root, "Knot1", new Vector3(0.11f, top + 0.001f, -0.026f), Vector3.zero, new Vector3(0.032f, 0.008f, 0.026f), knotMat);
            Sphere(root, "KnotRing2", new Vector3(-0.17f, top - 0.001f, 0.034f), Vector3.zero, new Vector3(0.040f, 0.007f, 0.034f), knotRingMat);
            Sphere(root, "Knot2", new Vector3(-0.17f, top + 0.001f, 0.034f), Vector3.zero, new Vector3(0.022f, 0.007f, 0.020f), knotMat);

            // A fine split checking in from one end, the way seasoned lumber cracks along the grain.
            Block(root, "Split", new Vector3(0.21f, top, 0.008f), new Vector3(0.16f, 0.004f, 0.004f), splitMat).localEulerAngles = new Vector3(0f, 3f, 0f);

            return root;
        }

        /// <summary>
        /// A box of nails: a small open kraft carton with its back flap torn open, brimming with a jumble
        /// of bright steel nails standing head-up and splayed at loose angles above the rim, plus a few
        /// spilled flat in front. The nails carry the item's steel tint and the front is printed with a
        /// couple of nail silhouettes, so a dropped box reads unmistakably as hardware where it lies.
        /// </summary>
        private static PartNode BuildNailBox(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the small carton so the standing nails clear the floor at the bottom of the bob.
            baseHeight = 0.16f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                          // bright bare steel nails
            Color steelDeep = new Color(steel.r * 0.68f, steel.g * 0.68f, steel.b * 0.68f);  // points, shading
            Color steelHi = Color.Lerp(steel, Color.white, 0.35f);                           // glinting head tops
            Color card = new Color(0.60f, 0.45f, 0.29f);                                     // kraft cardboard carton
            Color cardDark = new Color(card.r * 0.80f, card.g * 0.80f, card.b * 0.80f);      // interior / shading
            Color cream = new Color(0.90f, 0.87f, 0.78f);                                    // printed label backing
            Color ink = new Color(0.16f, 0.15f, 0.14f);                                      // dark print / placard

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.55f, metallic: 0.85f);
            PartMat steelDeepMat = PartMat.Of(steelDeep, smoothness: 0.45f, metallic: 0.85f);
            PartMat steelHiMat = PartMat.Of(steelHi, smoothness: 0.60f, metallic: 0.90f);
            PartMat cardMat = PartMat.Of(card, smoothness: 0.06f);
            PartMat cardDarkMat = PartMat.Of(cardDark, smoothness: 0.06f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.10f);

            // Lowers the whole arrangement so its mass centres on the root, which hovers at baseHeight.
            const float floor = -0.09f;         // ground plane the carton and spilled nails rest on
            const float slabH = 0.022f;         // box bottom-slab thickness
            const float wallH = 0.055f;         // box wall height (nails poke well above it)
            const float wall = 0.014f;          // wall thickness
            const float boxW = 0.18f;           // box width (X)
            const float boxD = 0.15f;           // box depth (Z)
            float innerY = floor + slabH;       // interior floor the standing nails sit on
            float wallCy = innerY + wallH * 0.5f;
            float wallTop = innerY + wallH;

            // ---- Kraft carton: a bottom slab and four thin walls forming an open tray ----
            Block(root, "BoxBottom", new Vector3(0f, floor + slabH * 0.5f, 0f), new Vector3(boxW, slabH, boxD), cardDarkMat);
            Block(root, "WallFront", new Vector3(0f, wallCy, -(boxD * 0.5f - wall * 0.5f)), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallBack", new Vector3(0f, wallCy, boxD * 0.5f - wall * 0.5f), new Vector3(boxW, wallH, wall), cardMat);
            Block(root, "WallLeft", new Vector3(-(boxW * 0.5f - wall * 0.5f), wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);
            Block(root, "WallRight", new Vector3(boxW * 0.5f - wall * 0.5f, wallCy, 0f), new Vector3(wall, wallH, boxD), cardMat);

            // Printed front label: a cream backing panel, a dark brand placard, and two little nail
            // silhouettes so the carton advertises its own contents.
            float frontZ = -(boxD * 0.5f) - 0.002f;
            Block(root, "Label", new Vector3(0f, innerY + 0.026f, frontZ), new Vector3(boxW - 0.02f, 0.042f, 0.004f), creamMat);
            Block(root, "Placard", new Vector3(-0.038f, innerY + 0.03f, frontZ - 0.001f), new Vector3(0.078f, 0.024f, 0.005f), inkMat);
            Block(root, "NailPrintA", new Vector3(0.05f, innerY + 0.028f, frontZ - 0.001f), new Vector3(0.006f, 0.03f, 0.005f), inkMat).localEulerAngles = new Vector3(0f, 0f, 22f);
            Block(root, "NailPrintB", new Vector3(0.066f, innerY + 0.028f, frontZ - 0.001f), new Vector3(0.006f, 0.03f, 0.005f), inkMat).localEulerAngles = new Vector3(0f, 0f, -16f);

            // ---- Back flap torn open, hinged at the back top edge and leaning out over the rear wall ----
            var flap = new PartNode("Flap");
            flap.SetParent(root, false);
            flap.localPosition = new Vector3(0f, wallTop, boxD * 0.5f - wall * 0.5f);
            flap.localEulerAngles = new Vector3(34f, 0f, 0f);   // tips the flap back and open
            Block(flap, "Panel", new Vector3(0f, 0.042f, 0f), new Vector3(boxW, 0.084f, 0.008f), cardMat);
            Block(flap, "PanelInner", new Vector3(0f, 0.042f, -0.005f), new Vector3(boxW - 0.02f, 0.078f, 0.004f), cardDarkMat);
            Block(flap, "FlapLabel", new Vector3(0f, 0.05f, 0.006f), new Vector3(boxW - 0.05f, 0.04f, 0.004f), creamMat);

            // ---- A jumble of nails standing on the box floor, splayed at loose angles so the heads spread ----
            float[,] stand = {
                { -0.048f, -0.028f, 7f, -4f }, { -0.012f, -0.034f, -5f, 6f }, { 0.038f, -0.030f, 6f, 9f },
                { -0.052f,  0.006f, -6f, -8f }, { -0.006f,  0.002f, 3f, 2f }, { 0.050f,  0.014f, -7f, 7f },
                { -0.030f,  0.034f, 8f, -5f }, {  0.024f,  0.036f, -4f, 8f },
            };
            for (int i = 0; i < stand.GetLength(0); i++)
            {
                BuildNail(root, new Vector3(stand[i, 0], innerY, stand[i, 1]),
                    new Vector3(stand[i, 2], 0f, stand[i, 3]), steelMat, steelDeepMat, steelHiMat);
            }

            // ---- A few loose nails spilled flat on the ground in front of the box ----
            BuildNail(root, new Vector3(-0.075f, floor + 0.008f, -0.105f), new Vector3(90f, 100f, 0f), steelMat, steelDeepMat, steelHiMat);
            BuildNail(root, new Vector3(0.08f, floor + 0.008f, -0.16f), new Vector3(90f, -85f, 0f), steelMat, steelDeepMat, steelHiMat);
            BuildNail(root, new Vector3(0.0f, floor + 0.008f, -0.098f), new Vector3(90f, 210f, 0f), steelMat, steelDeepMat, steelHiMat);

            return root;
        }

        /// <summary>
        /// A roll of duct tape lying flat like a squat donut: a wound silver-grey band with two shaded seam
        /// bands round its wall, faint concentric winds pressed into its face, a kraft cardboard core around a
        /// dark centre hole, and a peeled tab of tape curling off the rim — so a dropped roll reads
        /// unmistakably as duct tape where it lies, with the off-centre tab making its slow spin easy to read.
        /// </summary>
        private static PartNode BuildDuctTape(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the flat roll so its rim and the drooping tab clear the floor at the bottom of the bob.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color tape = def.Tint;                                                      // silvered duct-tape backing
            Color tapeEdge = new Color(tape.r * 0.86f, tape.g * 0.86f, tape.b * 0.86f); // shaded wound layers
            Color tapeHi = Color.Lerp(tape, Color.white, 0.22f);                        // catching highlight wind
            Color adhesive = new Color(tape.r * 0.78f, tape.g * 0.76f, tape.b * 0.70f); // duller sticky underside
            Color core = new Color(0.62f, 0.46f, 0.30f);                                // kraft cardboard core
            Color coreDark = new Color(core.r * 0.72f, core.g * 0.72f, core.b * 0.72f); // core inner shadow
            Color hole = new Color(0.09f, 0.09f, 0.10f);                                // dark centre hole

            PartMat tapeMat = PartMat.Of(tape, smoothness: 0.32f, metallic: 0.25f);
            PartMat edgeMat = PartMat.Of(tapeEdge, smoothness: 0.30f, metallic: 0.22f);
            PartMat hiMat = PartMat.Of(tapeHi, smoothness: 0.40f, metallic: 0.30f);
            PartMat adhesiveMat = PartMat.Of(adhesive, smoothness: 0.10f);
            PartMat coreMat = PartMat.Of(core, smoothness: 0.06f);
            PartMat coreDarkMat = PartMat.Of(coreDark, smoothness: 0.06f);
            PartMat holeMat = PartMat.Of(hole, smoothness: 0.05f);

            const float outerD = 0.188f;   // roll outer diameter
            const float rollH = 0.058f;    // roll width (its height as it lies flat)
            const float top = rollH * 0.5f;

            // Wound tape mass, with two shaded seam bands hinting the wrapped layers around the outer wall.
            Cylinder(root, "Body", 0f, outerD, rollH, tapeMat);
            Cylinder(root, "EdgeBandTop", 0.016f, outerD + 0.001f, 0.004f, edgeMat);
            Cylinder(root, "EdgeBandBottom", -0.016f, outerD + 0.001f, 0.004f, edgeMat);

            // Concentric winds pressed into the top face: decreasing discs, each a hair prouder than the last
            // and alternately shaded, read as the spiral of wrapped tape.
            Cylinder(root, "WindA", top + 0.002f, 0.168f, 0.003f, edgeMat);
            Cylinder(root, "WindB", top + 0.0045f, 0.140f, 0.003f, hiMat);
            Cylinder(root, "WindC", top + 0.007f, 0.112f, 0.003f, edgeMat);

            // Kraft core, an inner shadow ring, and the dark centre hole, stacked proud of the winds.
            Cylinder(root, "CoreTop", top + 0.0095f, 0.090f, 0.003f, coreMat);
            Cylinder(root, "CoreInner", top + 0.011f, 0.072f, 0.003f, coreDarkMat);
            Cylinder(root, "Hole", top + 0.013f, 0.056f, 0.003f, holeMat);

            // A hint of the same core and hole on the underside so the centre still reads as bored through
            // when the roll tips during its bob.
            Cylinder(root, "CoreBottom", -top - 0.0025f, 0.090f, 0.003f, coreMat);
            Cylinder(root, "HoleBottom", -top - 0.005f, 0.056f, 0.003f, holeMat);

            // ---- A peeled tab of tape curling off the rim (also makes the slow spin easy to read) ----
            var tab = new PartNode("Tab");
            tab.SetParent(root, false);
            tab.localPosition = new Vector3(0.086f, 0.007f, 0f);
            tab.localEulerAngles = new Vector3(0f, 0f, -12f);   // droops out from the rim
            Block(tab, "TapeTab", new Vector3(0.026f, 0f, 0f), new Vector3(0.050f, 0.005f, 0.052f), tapeMat);
            Block(tab, "TapeTabAdhesive", new Vector3(0.026f, -0.0035f, 0f), new Vector3(0.045f, 0.0025f, 0.048f), adhesiveMat);

            var tabTip = new PartNode("TabTip");
            tabTip.SetParent(tab, false);
            tabTip.localPosition = new Vector3(0.052f, 0f, 0f);
            tabTip.localEulerAngles = new Vector3(0f, 0f, -22f);   // extra curl at the tip
            Block(tabTip, "TapeTabTip", new Vector3(0.018f, 0f, 0f), new Vector3(0.036f, 0.005f, 0.050f), tapeMat);
            Block(tabTip, "TapeTabTipAdhesive", new Vector3(0.018f, -0.0035f, 0f), new Vector3(0.032f, 0.0025f, 0.046f), adhesiveMat);

            return root;
        }

        /// <summary>
        /// A wad of ripped bed sheets — the game's improvised, unsterile bandage. A few folded fabric
        /// layers are stacked at slightly different angles with soft rolled fold edges, a couple of torn
        /// strips trail off the ends, and dirt and a faded old bloodstain are soaked into the cloth so it
        /// reads as the dirty rag that stops bleeding but risks infection. The item's tint is the linen
        /// colour of the sheets.
        /// </summary>
        private static PartNode BuildRag(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the low cloth pile so the bottom fold and the drooping torn strips still clear the
            // floor at the low point of the pickup's bob (amplitude 0.06).
            baseHeight = 0.15f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color cloth = def.Tint;                                                          // off-white linen sheet
            Color clothHi = Color.Lerp(cloth, Color.white, 0.24f);                           // sun-catching fold crests
            Color clothShade = new Color(cloth.r * 0.82f, cloth.g * 0.82f, cloth.b * 0.82f);  // shaded lower folds
            Color clothDeep = new Color(cloth.r * 0.60f, cloth.g * 0.60f, cloth.b * 0.62f);   // deep crease shadow
            Color grime = new Color(0.50f, 0.46f, 0.38f);                                    // dingy dirt smudge
            Color bloodOld = new Color(0.46f, 0.25f, 0.21f);                                 // faded old bloodstain

            PartMat clothMat = PartMat.Of(cloth, smoothness: 0.05f);
            PartMat hiMat = PartMat.Of(clothHi, smoothness: 0.05f);
            PartMat shadeMat = PartMat.Of(clothShade, smoothness: 0.05f);
            PartMat deepMat = PartMat.Of(clothDeep, smoothness: 0.05f);
            PartMat grimeMat = PartMat.Of(grime, smoothness: 0.04f);
            PartMat bloodMat = PartMat.Of(bloodOld, smoothness: 0.06f);

            // ---- Folded sheet layers: a stack of flat cloth slabs, each nudged and yawed a little so the
            // bundle looks loosely folded rather than crisply stacked. ----
            var fold1 = Block(root, "Fold1", new Vector3(0f, -0.038f, 0f), new Vector3(0.300f, 0.030f, 0.225f), shadeMat);
            fold1.localEulerAngles = new Vector3(0f, -5f, 0f);
            var fold2 = Block(root, "Fold2", new Vector3(0.012f, -0.012f, 0.008f), new Vector3(0.292f, 0.030f, 0.218f), clothMat);
            fold2.localEulerAngles = new Vector3(0f, 4f, 0f);
            var fold3 = Block(root, "Fold3", new Vector3(-0.008f, 0.014f, -0.006f), new Vector3(0.278f, 0.028f, 0.205f), hiMat);
            fold3.localEulerAngles = new Vector3(0f, -2f, 0f);
            var fold4 = Block(root, "Fold4", new Vector3(0.028f, 0.036f, 0.022f), new Vector3(0.205f, 0.026f, 0.150f), clothMat);
            fold4.localEulerAngles = new Vector3(0f, 9f, 0f);

            // Soft rolled fold edges — a cloth fold is a rounded crease, not a sharp cube edge — so
            // cylinders laid along the width hide the hard block fronts and read as the turned-over folds.
            Rod(root, "FoldRollFront", new Vector3(0.006f, -0.006f, 0.104f), new Vector3(0f, 0f, 90f), 0.052f, 0.276f, clothMat);
            Rod(root, "FoldRollBack", new Vector3(-0.004f, 0.016f, -0.095f), new Vector3(0f, 0f, 90f), 0.044f, 0.250f, shadeMat);
            Rod(root, "FoldRollTop", new Vector3(0.030f, 0.038f, 0.086f), new Vector3(0f, 0f, 90f), 0.034f, 0.190f, hiMat);

            // Creases pressed into the top layer.
            var crease1 = Block(root, "Crease1", new Vector3(0.010f, 0.050f, 0f), new Vector3(0.230f, 0.004f, 0.009f), deepMat);
            crease1.localEulerAngles = new Vector3(0f, 5f, 0f);
            var crease2 = Block(root, "Crease2", new Vector3(0.030f, 0.050f, 0.045f), new Vector3(0.007f, 0.004f, 0.150f), deepMat);
            crease2.localEulerAngles = new Vector3(0f, 9f, 0f);

            // ---- Torn strips trailing off the ends — the "ripped" in ripped sheets — each drooping a
            // little toward the floor with a couple of frayed tongues at the torn tip. ----
            BuildRagStrip(root, new Vector3(0.150f, -0.004f, 0.055f), new Vector3(14f, -8f, 0f), 0.150f, 0.050f, clothMat, shadeMat);
            BuildRagStrip(root, new Vector3(0.158f, 0.012f, -0.036f), new Vector3(9f, 7f, 0f), 0.118f, 0.036f, shadeMat, deepMat);
            BuildRagStrip(root, new Vector3(-0.150f, -0.008f, -0.060f), new Vector3(12f, 168f, 0f), 0.132f, 0.044f, hiMat, shadeMat);

            // ---- Dirt and a faded old bloodstain soaked into the cloth: flat discs sitting proud of the
            // top so the rag reads as the used, unsterile dressing that risks infection. ----
            Sphere(root, "Grime", new Vector3(-0.050f, 0.051f, 0.030f), Vector3.zero, new Vector3(0.115f, 0.006f, 0.085f), grimeMat);
            Sphere(root, "Blood", new Vector3(0.052f, 0.051f, -0.020f), Vector3.zero, new Vector3(0.078f, 0.006f, 0.062f), bloodMat);
            Sphere(root, "BloodSpeck", new Vector3(0.092f, 0.050f, 0.028f), Vector3.zero, new Vector3(0.032f, 0.005f, 0.028f), bloodMat);

            return root;
        }

        /// <summary>
        /// A coil of rope: several concentric loops of thick, fibrous cord wound flat into a hank, each inner
        /// loop nudged up a touch so the coil domes like a real piled coil. The strands are shaded in
        /// alternating tones around every loop so the cord shows the barber-pole banding of laid rope; a short
        /// twine lashing binds the coil across one side, and the loose end trails out to a whipped, frayed tip
        /// (which also makes the pickup's slow spin easy to read). The item's tint is the natural hemp colour.
        /// </summary>
        private static PartNode BuildRope(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Low centre hover for the flat coil, matching the other tools and materials; the bob (amplitude
            // 0.06) never dips the cord (radius ~0.018) through the floor from here.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fibre = def.Tint;                                                           // natural hemp cord
            Color fibreHi = Color.Lerp(fibre, new Color(0.84f, 0.74f, 0.54f), 0.38f);         // sun-caught strand
            Color fibreDark = new Color(fibre.r * 0.70f, fibre.g * 0.70f, fibre.b * 0.68f);   // shaded / grooved strand
            Color fray = Color.Lerp(fibre, new Color(0.88f, 0.80f, 0.62f), 0.5f);             // pale teased-out fibres
            Color twine = new Color(fibre.r * 0.50f, fibre.g * 0.48f, fibre.b * 0.44f);       // darker binding twine

            PartMat fibreMat = PartMat.Of(fibre, smoothness: 0.07f);
            PartMat fibreHiMat = PartMat.Of(fibreHi, smoothness: 0.08f);
            PartMat fibreDarkMat = PartMat.Of(fibreDark, smoothness: 0.06f);
            PartMat frayMat = PartMat.Of(fray, smoothness: 0.05f);
            PartMat twineMat = PartMat.Of(twine, smoothness: 0.06f);

            const float cordD = 0.036f;   // rope thickness

            // ---- Concentric loops wound flat into the coil. The alternating strand tones cycle around each
            // loop (segment counts kept off multiples of three) so the banding drifts round like laid twist. ----
            RopeRing(root, 0.145f, 0.000f, 26, cordD, fibreMat, fibreHiMat, fibreDarkMat);
            RopeRing(root, 0.108f, 0.007f, 20, cordD, fibreDarkMat, fibreMat, fibreHiMat);
            RopeRing(root, 0.072f, 0.013f, 14, cordD, fibreHiMat, fibreDarkMat, fibreMat);
            RopeRing(root, 0.038f, 0.018f, 10, cordD * 0.94f, fibreMat, fibreHiMat, fibreDarkMat);

            // ---- Loose end trailing out from the outer loop, lying flat and running out to a bound, frayed
            // tip so the coil isn't a closed ring. ----
            const float tailDeg = 200f;                                              // heading the end trails out
            float tailRad = tailDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(tailRad), 0f, Mathf.Sin(tailRad));    // outward direction
            Vector3 perp = new Vector3(-Mathf.Sin(tailRad), 0f, Mathf.Cos(tailRad));  // across the cord
            float layYaw = 90f - tailDeg;                                            // lays a Rod along that heading
            Vector3 exit = new Vector3(0.145f * Mathf.Cos(tailRad), 0f, 0.145f * Mathf.Sin(tailRad));

            const float tailSeg = 0.05f;
            for (int k = 0; k < 3; k++)
            {
                Vector3 p = exit + dir * (tailSeg * (k + 0.5f));
                Rod(root, "Tail" + k, p, new Vector3(90f, layYaw, 0f), cordD, tailSeg + 0.012f,
                    (k % 2 == 0) ? fibreMat : fibreHiMat);
            }

            // Whipping wrapped round the cord near the tip (a fatter, darker collar), then the teased-out
            // frayed fibres fanned and drooping past it.
            Rod(root, "Whipping", exit + dir * (tailSeg * 3f + 0.006f), new Vector3(90f, layYaw, 0f),
                cordD + 0.008f, 0.022f, twineMat);

            Vector3 frayBase = exit + dir * (tailSeg * 3f + 0.028f);
            float[] fan = { -24f, -9f, 8f, 23f };
            for (int f = 0; f < fan.Length; f++)
            {
                float fyaw = 90f - (tailDeg + fan[f]);
                Vector3 fp = frayBase + dir * 0.014f + perp * (fan[f] * 0.0006f);
                Rod(root, "Fray" + f, fp, new Vector3(101f, fyaw, 0f), 0.010f, 0.044f, frayMat);   // X=101 droops the tip
            }

            // ---- A short twine lashing binding the coil across one side: a couple of turns laid radially,
            // hugging the top of the hank. ----
            const float tieDeg = 52f;
            for (int t = 0; t < 3; t++)
            {
                float td = (tieDeg + (t - 1) * 8f) * Mathf.Deg2Rad;
                Vector3 tp = new Vector3(0.092f * Mathf.Cos(td), 0.024f, 0.092f * Mathf.Sin(td));
                Rod(root, "Lashing" + t, tp, new Vector3(90f, 90f - tieDeg, 0f), 0.020f, 0.120f, twineMat);
            }

            return root;
        }

        /// <summary>
        /// A manual rotary "butterfly" can opener lying flat. Two stamped-steel handles trail back from a
        /// compact cutting head, splaying open a little at rest and dipped in red plastic grips; the head
        /// carries a serrated drive gear and a small round cutting wheel, with the winged turn-key standing
        /// proud on top. The item's steel tint paints the handles and body (its signature material, like the
        /// knife and hammer); the grips and key are fixed accents so the tool reads unmistakably where it
        /// lies. Laid along X with the cutting head at the +X end.
        /// </summary>
        private static PartNode BuildCanOpener(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Flat and low, like the other hand tools; the tallest part is the turn-key on top and the bob
            // never dips the mechanism through the floor.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                     // stamped-steel handles + body
            Color bright = new Color(0.86f, 0.88f, 0.93f);                              // polished wheels, key, rivets
            Color dark = new Color(steel.r * 0.5f, steel.g * 0.5f, steel.b * 0.52f);    // gear shadow, axles, slots
            Color grip = new Color(0.72f, 0.16f, 0.14f);                                // red plastic dip grips
            Color gripDark = new Color(0.52f, 0.10f, 0.09f);                            // shaded grip ends

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.68f, metallic: 0.9f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.85f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.5f, metallic: 0.85f);
            PartMat gripMat = PartMat.Of(grip, smoothness: 0.35f);
            PartMat gripDarkMat = PartMat.Of(gripDark, smoothness: 0.35f);

            // ---- Two handles, pivoting at the head and splaying open toward the back (-X) ----
            // Each handle is its own tilt frame anchored at the joint so only the butt end swings out.
            for (int s = -1; s <= 1; s += 2)
            {
                var arm = new PartNode(s > 0 ? "HandleTop" : "HandleBottom");
                arm.SetParent(root, false);
                arm.localPosition = new Vector3(0.028f, 0f, s * 0.013f);
                arm.localEulerAngles = new Vector3(0f, s * 8f, 0f);   // butt end spreads to its own side

                // Flat stamped lever running back along the frame's -X, necking down toward the pivot.
                Block(arm, "Lever", new Vector3(-0.09f, 0f, 0f), new Vector3(0.185f, 0.011f, 0.016f), steelMat);
                Block(arm, "Neck", new Vector3(0.005f, 0f, 0f), new Vector3(0.03f, 0.009f, 0.011f), steelMat);
                // Red plastic grip dipped over the butt half, rounded off with a shaded end cap.
                Block(arm, "Grip", new Vector3(-0.135f, 0f, 0f), new Vector3(0.09f, 0.018f, 0.022f), gripMat);
                Sphere(arm, "GripEnd", new Vector3(-0.18f, 0f, 0f), Vector3.zero,
                    new Vector3(0.016f, 0.018f, 0.022f), gripDarkMat);
            }

            // ---- Pivot joining the handles, just ahead of where they meet ----
            Rod(root, "Pivot", new Vector3(0.028f, 0f, 0f), Vector3.zero, 0.022f, 0.03f, steelMat);
            Sphere(root, "PivotCap", new Vector3(0.028f, 0.016f, 0f), Vector3.zero,
                new Vector3(0.016f, 0.009f, 0.016f), brightMat);

            // ---- Compact cutting head at +X ----
            const float headX = 0.075f;
            // Housing plate the wheels and key are set into.
            Block(root, "Head", new Vector3(0.055f, 0.002f, 0f), new Vector3(0.06f, 0.016f, 0.05f), steelMat);

            // Serrated drive gear underneath, its axis vertical: a dark hub ringed by steel teeth that bite
            // the can's rim. Each tooth faces the hub and overlaps its neighbours into a continuous ring.
            Rod(root, "Gear", new Vector3(headX, -0.006f, 0f), Vector3.zero, 0.03f, 0.014f, darkMat);
            const int teeth = 12;
            const float toothR = 0.019f;
            for (int i = 0; i < teeth; i++)
            {
                float a = i / (float)teeth * Mathf.PI * 2f;
                Block(root, "Tooth", new Vector3(headX + Mathf.Cos(a) * toothR, -0.006f, Mathf.Sin(a) * toothR),
                    new Vector3(0.009f, 0.012f, 0.006f), steelMat)
                    .localEulerAngles = new Vector3(0f, -i / (float)teeth * 360f, 0f);
            }

            // Small round cutting wheel reaching past the front on a short bracket, its blade standing in a
            // vertical plane the way it rolls round a lid, offset to one side of the drive gear.
            Block(root, "CutArm", new Vector3(0.092f, -0.002f, 0.013f), new Vector3(0.032f, 0.01f, 0.012f), steelMat);
            Rod(root, "CutWheel", new Vector3(0.106f, 0.004f, 0.016f), new Vector3(90f, 0f, 0f), 0.028f, 0.005f, brightMat);
            Rod(root, "CutHub", new Vector3(0.106f, 0.004f, 0.016f), new Vector3(90f, 0f, 0f), 0.01f, 0.008f, darkMat);

            // ---- Winged turn-key standing on top of the head ----
            Rod(root, "KeyShaft", new Vector3(headX, 0.022f, 0f), Vector3.zero, 0.009f, 0.03f, steelMat);
            Rod(root, "KeyHub", new Vector3(headX, 0.04f, 0f), Vector3.zero, 0.016f, 0.012f, brightMat);
            // Two flat wings whose outer tips lift into the classic butterfly you pinch and turn.
            Block(root, "WingTop", new Vector3(headX, 0.043f, 0.024f), new Vector3(0.013f, 0.006f, 0.032f), brightMat)
                .localEulerAngles = new Vector3(-18f, 0f, 0f);
            Block(root, "WingBottom", new Vector3(headX, 0.043f, -0.024f), new Vector3(0.013f, 0.006f, 0.032f), brightMat)
                .localEulerAngles = new Vector3(18f, 0f, 0f);
            // Rounded knobs on the lifted wing tips.
            Sphere(root, "WingKnobTop", new Vector3(headX, 0.048f, 0.04f), Vector3.zero,
                new Vector3(0.014f, 0.01f, 0.014f), brightMat);
            Sphere(root, "WingKnobBottom", new Vector3(headX, 0.048f, -0.04f), Vector3.zero,
                new Vector3(0.014f, 0.01f, 0.014f), brightMat);

            return root;
        }

        /// <summary>
        /// An empty, opened tin can — the scrap left behind when a canned food is opened. Bare, dented
        /// tin-plate with its top cut away: a jagged rim of torn teeth surrounds the open mouth, the
        /// interior reads as a dark empty cavity, and the circular lid is peeled back on its hinge,
        /// standing up and out over one side. A faded scrap of the old paper label still clings to the
        /// body. Steel-tinted so it reads as salvage rather than a full food tin on the floor.
        /// </summary>
        private static PartNode BuildTinCan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Same hover as the sealed food tins it is opened from, so it sits naturally among them.
            baseHeight = 0.26f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                          // bare tin-plate body
            Color bright = new Color(0.86f, 0.88f, 0.93f);                                   // raw cut metal at the torn edge
            Color dark = new Color(steel.r * 0.68f, steel.g * 0.68f, steel.b * 0.7f);        // dents, scuffs, shaded steel
            Color innerWall = new Color(steel.r * 0.42f, steel.g * 0.42f, steel.b * 0.44f);  // shadowed inner wall
            Color innerFloor = new Color(steel.r * 0.26f, steel.g * 0.26f, steel.b * 0.28f); // dark empty interior floor
            Color label = new Color(0.85f, 0.81f, 0.70f);                                    // faded torn paper scrap
            Color labelShade = new Color(0.66f, 0.62f, 0.52f);                               // its grimy torn edges

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.6f, metallic: 0.85f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.85f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.45f, metallic: 0.8f);
            PartMat innerWallMat = PartMat.Of(innerWall, smoothness: 0.35f, metallic: 0.6f);
            PartMat innerFloorMat = PartMat.Of(innerFloor, smoothness: 0.3f, metallic: 0.5f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.1f);
            PartMat labelShadeMat = PartMat.Of(labelShade, smoothness: 0.1f);

            const float bodyD = 0.20f;     // body diameter (matches the beans tin)
            const float bodyH = 0.28f;     // body height (a touch shorter, its lid cut off)
            const float half = bodyH * 0.5f;
            const float rimY = half;       // top edge, where the lid was cut away

            // ---- Steel body and intact rolled base ----
            Cylinder(root, "Body", 0f, bodyD, bodyH, steelMat);
            Cylinder(root, "SeamBottom", -(half - 0.008f), bodyD + 0.012f, 0.016f, steelMat);
            // Slightly recessed stamped base underneath.
            Cylinder(root, "BaseRecess", -(half - 0.002f), bodyD - 0.03f, 0.006f, darkMat);

            // ---- Open mouth: a dark cavity painted over the body's top so it reads as hollow ----
            // A shadowed inner wall ring, then a smaller darker disc a hair higher for the recessed floor
            // (the same trick the duct-tape roll uses to fake a hole with solid primitives).
            Cylinder(root, "InnerWall", rimY + 0.001f, bodyD - 0.026f, 0.004f, innerWallMat);
            Cylinder(root, "InnerFloor", rimY + 0.003f, bodyD - 0.085f, 0.004f, innerFloorMat);

            // ---- Jagged cut rim ringing the open top, skipping the side the lid is hinged to (+X) ----
            const int jag = 16;
            const float jagR = bodyD * 0.5f;
            for (int i = 0; i < jag; i++)
            {
                float a = i / (float)jag * Mathf.PI * 2f;
                if (Mathf.Cos(a) > 0.8f) continue;               // leave a gap for the peeled lid's hinge
                float h = (i % 2 == 0) ? 0.016f : 0.008f;        // alternating torn teeth
                Block(root, "Jag", new Vector3(Mathf.Cos(a) * jagR, rimY + h * 0.5f - 0.003f, Mathf.Sin(a) * jagR),
                    new Vector3(0.018f, h, 0.009f), brightMat)
                    .localEulerAngles = new Vector3(0f, -a * Mathf.Rad2Deg, 0f);
            }

            // ---- Peeled-back lid, hinged at the +X rim and standing up and out over the side ----
            var lid = new PartNode("Lid");
            lid.SetParent(root, false);
            lid.localPosition = new Vector3(bodyD * 0.5f - 0.006f, rimY - 0.004f, 0f);
            lid.localEulerAngles = new Vector3(0f, 0f, -118f);   // swing the flap up and outward on its hinge
            // Round lid, its far edge lifted; a brighter cut-metal underside shows where it tore free.
            Rod(lid, "LidTop", new Vector3(-0.086f, 0f, 0f), Vector3.zero, bodyD - 0.014f, 0.006f, steelMat);
            Rod(lid, "LidUnder", new Vector3(-0.086f, -0.004f, 0f), Vector3.zero, bodyD - 0.022f, 0.004f, brightMat);
            // Bent hinge tab keeping it attached to the rim.
            Block(lid, "Hinge", new Vector3(-0.012f, -0.002f, 0f), new Vector3(0.028f, 0.006f, 0.05f), darkMat);
            // A couple of torn nicks around the lid's free edge.
            Rod(lid, "LidNick1", new Vector3(-0.17f, 0.002f, 0.03f), Vector3.zero, 0.02f, 0.005f, brightMat);
            Rod(lid, "LidNick2", new Vector3(-0.16f, 0.002f, -0.04f), Vector3.zero, 0.016f, 0.005f, brightMat);

            // ---- Faded scrap of the old paper label, torn and clinging to the -X side ----
            const float lblR = bodyD * 0.5f + 0.004f;
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI + (i - 1.5f) * 0.19f;         // a short arc centred on the -X face
                float hh = 0.12f - (i % 2) * 0.03f;              // ragged torn bottom edge
                Block(root, "LabelScrap", new Vector3(Mathf.Cos(a) * lblR, -0.005f, Mathf.Sin(a) * lblR),
                    new Vector3(0.006f, hh, 0.034f), (i == 0 || i == 3) ? labelShadeMat : labelMat)
                    .localEulerAngles = new Vector3(0f, -a * Mathf.Rad2Deg, 0f);
            }

            // ---- A few dark scuffs down the bare steel for a used, salvaged look ----
            float[] scuffAngles = { 0.6f, 1.15f, 4.2f };
            for (int i = 0; i < scuffAngles.Length; i++)
            {
                float a = scuffAngles[i];
                Block(root, "Scuff", new Vector3(Mathf.Cos(a) * (bodyD * 0.5f + 0.001f), (i - 1) * 0.03f,
                        Mathf.Sin(a) * (bodyD * 0.5f + 0.001f)),
                    new Vector3(0.006f, 0.13f - i * 0.02f, 0.02f), darkMat)
                    .localEulerAngles = new Vector3(0f, -a * Mathf.Rad2Deg, 0f);
            }

            return root;
        }

        /// <summary>
        /// A sewing needle lying flat, threaded and ready to mend clothing. A polished steel shaft tapers
        /// from a flattened eye end — with the dark eye slot and its bright polished ends — down to a sharp
        /// point, and a length of cream thread is looped through the eye and trails off in a loose curve so
        /// the tiny needle still reads clearly where it lies on the floor.
        /// </summary>
        private static PartNode BuildNeedle(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Flat and low like the other hand tools; the needle barely rises off the ground.
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                        // polished needle body
            Color bright = new Color(0.88f, 0.9f, 0.95f);                                  // honed point and eye ends
            Color dark = new Color(steel.r * 0.45f, steel.g * 0.45f, steel.b * 0.48f);     // the eye hole
            Color thread = new Color(0.92f, 0.89f, 0.82f);                                 // cream sewing thread
            Color threadShade = new Color(0.78f, 0.74f, 0.66f);                            // shaded thread strands

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.75f, metallic: 0.9f);
            PartMat brightMat = PartMat.Of(bright, smoothness: 0.9f, metallic: 0.9f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.4f, metallic: 0.7f);
            PartMat threadMat = PartMat.Of(thread, smoothness: 0.2f);
            PartMat threadShadeMat = PartMat.Of(threadShade, smoothness: 0.2f);

            // ---- Tapered steel shaft, laid along X with the point at +X ----
            // Each segment is a Rod tilted (0,0,90) so its length runs along X; the diameter steps down
            // toward the tip so the shaft reads as a taper.
            Rod(root, "Shaft1", new Vector3(-0.05f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.014f, 0.11f, steelMat);
            Rod(root, "Shaft2", new Vector3(0.045f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.010f, 0.09f, steelMat);
            Rod(root, "Shaft3", new Vector3(0.10f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.007f, 0.05f, steelMat);
            // Honed point tapering to a sharp tip.
            Rod(root, "Point", new Vector3(0.14f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.004f, 0.05f, brightMat);
            Sphere(root, "Tip", new Vector3(0.172f, 0f, 0f), Vector3.zero, new Vector3(0.016f, 0.003f, 0.003f), brightMat);

            // ---- Flattened eye end at -X ----
            Block(root, "EyePlate", new Vector3(-0.125f, 0f, 0f), new Vector3(0.06f, 0.007f, 0.02f), steelMat);
            Sphere(root, "EyeCap", new Vector3(-0.153f, 0f, 0f), Vector3.zero, new Vector3(0.016f, 0.008f, 0.022f), steelMat);
            // The eye itself: a dark slot through the flat, painted a hair proud on both faces, with its
            // polished ends catching the light.
            Block(root, "EyeSlotTop", new Vector3(-0.123f, 0.005f, 0f), new Vector3(0.034f, 0.003f, 0.009f), darkMat);
            Block(root, "EyeSlotBottom", new Vector3(-0.123f, -0.005f, 0f), new Vector3(0.034f, 0.003f, 0.009f), darkMat);
            Block(root, "EyeRimNear", new Vector3(-0.106f, 0.004f, 0f), new Vector3(0.006f, 0.004f, 0.016f), brightMat);
            Block(root, "EyeRimFar", new Vector3(-0.14f, 0.004f, 0f), new Vector3(0.006f, 0.004f, 0.016f), brightMat);

            // ---- Cream thread looped through the eye and trailing off in a loose curve ----
            // A little bunch where it doubles through the eye.
            Sphere(root, "ThreadKnot", new Vector3(-0.135f, 0.009f, 0f), Vector3.zero, new Vector3(0.016f, 0.012f, 0.014f), threadMat);
            // Trailing strand: short Rods laid flat (euler 90,90-heading,0) tracing a gentle S back and to
            // the side, thinning toward the loose end.
            Rod(root, "Thread1", new Vector3(-0.185f, 0.005f, 0.015f), new Vector3(90f, -66.8f, 0f), 0.006f, 0.076f, threadMat);
            Rod(root, "Thread2", new Vector3(-0.25f, 0.005f, 0.025f), new Vector3(90f, -99.5f, 0f), 0.006f, 0.061f, threadShadeMat);
            Rod(root, "Thread3", new Vector3(-0.305f, 0.005f, 0f), new Vector3(90f, -128.7f, 0f), 0.005f, 0.064f, threadMat);
            Sphere(root, "ThreadTip", new Vector3(-0.335f, 0.005f, -0.02f), Vector3.zero, new Vector3(0.008f, 0.006f, 0.008f), threadShadeMat);

            return root;
        }

        /// <summary>
        /// A spool of sewing thread lying on its side: a small wooden reel — two wide flanges joined by a
        /// central barrel — wound with thread in the item's tint, the dark centre bore showing at each end
        /// and a loose strand trailing off, so the little spool reads clearly (and its slow spin shows)
        /// where it lies on the floor.
        /// </summary>
        private static PartNode BuildThread(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Small and low like the other hand tools; the spool rests on its flange rims.
            baseHeight = 0.14f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color threadCol = def.Tint;                                                            // wound thread
            Color threadHi = Color.Lerp(def.Tint, Color.white, 0.28f);                             // catch-light windings
            Color threadShade = new Color(def.Tint.r * 0.76f, def.Tint.g * 0.76f, def.Tint.b * 0.8f); // shaded coils and loose end
            Color wood = new Color(0.62f, 0.46f, 0.28f);                                           // wooden reel
            Color woodDark = new Color(wood.r * 0.7f, wood.g * 0.7f, wood.b * 0.7f);               // flange grain and shading
            Color bore = new Color(0.16f, 0.12f, 0.08f);                                           // dark centre hole

            PartMat threadMat = PartMat.Of(threadCol, smoothness: 0.2f);
            PartMat threadHiMat = PartMat.Of(threadHi, smoothness: 0.25f);
            PartMat threadShadeMat = PartMat.Of(threadShade, smoothness: 0.2f);
            PartMat woodMat = PartMat.Of(wood, smoothness: 0.3f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.3f);
            PartMat boreMat = PartMat.Of(bore, smoothness: 0.2f);

            const float flangeD = 0.13f;    // flange (end-cap) diameter
            const float barrelD = 0.055f;   // wooden core diameter
            const float halfLen = 0.075f;   // half the spool's length along X

            // ---- Wooden reel, axis laid along X (a Rod tilted 0,0,90 runs the cylinder length along X) ----
            // Central barrel spanning the two flanges.
            Rod(root, "Barrel", Vector3.zero, new Vector3(0f, 0f, 90f), barrelD, halfLen * 2f + 0.004f, woodMat);
            // A flange at each end, with a concentric grain ring and the dark centre bore showing on the face.
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = s * halfLen;
                Rod(root, "Flange" + s, new Vector3(ex, 0f, 0f), new Vector3(0f, 0f, 90f), flangeD, 0.012f, woodMat);
                Rod(root, "Grain" + s, new Vector3(ex + s * 0.007f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.09f, 0.005f, woodDarkMat);
                Rod(root, "Bore" + s, new Vector3(ex + s * 0.011f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.03f, 0.006f, boreMat);
            }

            // ---- Wound thread built up on the barrel, stopping a little short of the flanges ----
            // A slightly-narrow shaded under-layer, then fine circumferential rings: thread is wound round
            // the barrel, so alternating highlit and base bands — with the darker under-layer showing in the
            // grooves between them — read as coiled strand that bulges gently toward the middle.
            Rod(root, "Winding", Vector3.zero, new Vector3(0f, 0f, 90f), 0.09f, 0.12f, threadShadeMat);
            const int rings = 11;
            for (int i = 0; i < rings; i++)
            {
                float t = i / (rings - 1f);                 // 0..1 across the winding
                float u = (t - 0.5f) * 2f;                  // -1..1, zero at the middle
                float x = Mathf.Lerp(-0.052f, 0.052f, t);
                float d = 0.118f - 0.02f * u * u;           // fattest in the middle, tapering toward the flanges
                PartMat m = (i % 2 == 0) ? threadHiMat : threadMat;
                Rod(root, "Wind" + i, new Vector3(x, 0f, 0f), new Vector3(0f, 0f, 90f), d, 0.007f, m);
            }

            // ---- Loose thread end coming off the lower side and trailing away in a gentle curve ----
            // Short Rods laid flat (euler 90, 90-heading, 0) trace the strand; its asymmetry also makes the
            // pickup's slow spin read.
            Sphere(root, "LooseKnot", new Vector3(0.005f, -0.03f, 0.055f), Vector3.zero, new Vector3(0.02f, 0.016f, 0.02f), threadMat);
            Rod(root, "Loose1", new Vector3(0.045f, -0.033f, 0.08f), new Vector3(90f, 55f, 0f), 0.006f, 0.075f, threadMat);
            Rod(root, "Loose2", new Vector3(0.08f, -0.036f, 0.115f), new Vector3(90f, 45f, 0f), 0.006f, 0.06f, threadShadeMat);
            Rod(root, "Loose3", new Vector3(0.10f, -0.038f, 0.15f), new Vector3(90f, 30f, 0f), 0.005f, 0.05f, threadMat);
            Sphere(root, "LooseTip", new Vector3(0.105f, -0.04f, 0.175f), Vector3.zero, new Vector3(0.008f, 0.006f, 0.008f), threadShadeMat);

            return root;
        }

        /// <summary>
        /// A garden hand trowel lying flat — a bespoke model for the farming tool that breaks new plots and
        /// lifts ripe crops. A dished, leaf-shaped steel scoop tapering to a point at the +X end joins, by a
        /// short flattened tang and a brass ferrule, a rounded wooden handle at the -X end. The item's muted
        /// blue-grey tint paints the blade steel; the scoop's concavity is read the frying-pan way, as
        /// concentric inset shells that darken and rise toward a shadowed hollow, cupped by bright raised
        /// rims. Laid along X so the point sweeps clear as the pickup spins and bobs.
        /// </summary>
        private static PartNode BuildTrowel(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Lies on its side so it hovers low; the scoop's underside is the lowest part and the bob never
            // dips it through the floor (0.12 - 0.06 bob - 0.035 to the scoop underside > 0).
            baseHeight = 0.12f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                                                          // painted, worn blade steel
            Color steelHi = Color.Lerp(steel, Color.white, 0.32f);                           // honed tip and raised rims
            Color steelMid = new Color(steel.r * 0.82f, steel.g * 0.82f, steel.b * 0.85f);   // dished scoop bed
            Color steelDeep = new Color(steel.r * 0.55f, steel.g * 0.55f, steel.b * 0.62f);  // shadowed scoop hollow
            Color wood = new Color(0.55f, 0.36f, 0.20f);                                     // wooden handle
            Color woodDark = new Color(0.40f, 0.26f, 0.14f);                                 // grip rings and butt
            Color brass = new Color(0.74f, 0.62f, 0.36f);                                    // ferrule collar
            Color brassHi = Color.Lerp(brass, Color.white, 0.3f);                            // proud ferrule ring

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.5f, metallic: 0.7f);
            PartMat steelHiMat = PartMat.Of(steelHi, smoothness: 0.72f, metallic: 0.8f);
            PartMat steelMidMat = PartMat.Of(steelMid, smoothness: 0.42f, metallic: 0.65f);
            PartMat steelDeepMat = PartMat.Of(steelDeep, smoothness: 0.3f, metallic: 0.55f);
            PartMat woodMat = PartMat.Of(wood, smoothness: 0.22f);
            PartMat woodDarkMat = PartMat.Of(woodDark, smoothness: 0.2f);
            PartMat brassMat = PartMat.Of(brass, smoothness: 0.55f, metallic: 0.85f);
            PartMat brassHiMat = PartMat.Of(brassHi, smoothness: 0.7f, metallic: 0.9f);

            const float lay = 90f;         // lays a Rod's length along X
            const float bladeY = -0.010f;  // the scoop sits a touch below the handle line, as a cranked trowel does

            // ---- Wooden handle running along X, the rounded butt at the -X end ----
            Rod(root, "Grip", new Vector3(-0.085f, 0f, 0f), new Vector3(0f, 0f, lay), 0.046f, 0.11f, woodMat);
            Sphere(root, "GripSwell", new Vector3(-0.085f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.055f, 0.055f), woodMat);
            Rod(root, "GripRingFront", new Vector3(-0.038f, 0f, 0f), new Vector3(0f, 0f, lay), 0.05f, 0.012f, woodDarkMat);
            Rod(root, "GripRingRear", new Vector3(-0.128f, 0f, 0f), new Vector3(0f, 0f, lay), 0.05f, 0.012f, woodDarkMat);
            Sphere(root, "Butt", new Vector3(-0.15f, 0f, 0f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), woodDarkMat);

            // ---- Brass ferrule stepping the handle down to the steel tang ----
            Rod(root, "FerruleBase", new Vector3(-0.016f, 0f, 0f), new Vector3(0f, 0f, lay), 0.05f, 0.032f, brassMat);
            Rod(root, "FerruleNeck", new Vector3(0.014f, 0f, 0f), new Vector3(0f, 0f, lay), 0.036f, 0.03f, brassMat);
            Rod(root, "FerruleRing", new Vector3(-0.03f, 0f, 0f), new Vector3(0f, 0f, lay), 0.053f, 0.008f, brassHiMat);

            // ---- Steel tang: a round shank from the ferrule that flattens and widens into the blade ----
            Rod(root, "Tang", new Vector3(0.045f, 0f, 0f), new Vector3(0f, 0f, lay), 0.02f, 0.05f, steelMat);
            Block(root, "TangSpread", new Vector3(0.078f, -0.005f, 0f), new Vector3(0.075f, 0.016f, 0.055f), steelMat);

            // ---- Dished, leaf-shaped scoop tapering to a point at +X ----
            // Bright steel underside and outline, then a smaller darker bed and a shadowed hollow set proud
            // and inset (the frying-pan trick) so the concentric shells read as a scooped dish.
            Sphere(root, "BladeUnder", new Vector3(0.115f, bladeY, 0f), Vector3.zero, new Vector3(0.155f, 0.05f, 0.12f), steelMat);
            Sphere(root, "BladeTip", new Vector3(0.185f, bladeY, 0f), Vector3.zero, new Vector3(0.07f, 0.04f, 0.058f), steelMat);
            Sphere(root, "BladePoint", new Vector3(0.213f, bladeY + 0.002f, 0f), Vector3.zero, new Vector3(0.032f, 0.028f, 0.026f), steelHiMat);
            Sphere(root, "ScoopBed", new Vector3(0.115f, bladeY + 0.014f, 0f), Vector3.zero, new Vector3(0.132f, 0.044f, 0.096f), steelMidMat);
            Sphere(root, "ScoopHollow", new Vector3(0.12f, bladeY + 0.023f, 0f), Vector3.zero, new Vector3(0.10f, 0.038f, 0.068f), steelDeepMat);

            // Bright raised rims cupping the scoop: two rails along the long edges and a wall across the heel.
            Sphere(root, "RimLeft", new Vector3(0.108f, bladeY + 0.016f, 0.047f), new Vector3(-16f, 0f, 0f), new Vector3(0.13f, 0.02f, 0.028f), steelHiMat);
            Sphere(root, "RimRight", new Vector3(0.108f, bladeY + 0.016f, -0.047f), new Vector3(16f, 0f, 0f), new Vector3(0.13f, 0.02f, 0.028f), steelHiMat);
            Sphere(root, "RimBack", new Vector3(0.052f, bladeY + 0.014f, 0f), Vector3.zero, new Vector3(0.028f, 0.022f, 0.095f), steelHiMat);

            return root;
        }

        /// <summary>
        /// A galvanised garden watering can — the farming companion to the trowel, and the last curated
        /// tool to get a bespoke model. A straight-sided steel drum ringed with rolled ribs, a bright rolled
        /// rim around a dark open filling mouth, a long spout rising from the lower front to a perforated
        /// round rose above the body, a carrying handle arching over the top in the spout's plane, and a
        /// smaller D-handle at the back for pouring leverage. The item's steel-blue tint paints the
        /// enamelled body so it reads as a watering can where it lies on the floor.
        /// </summary>
        private static PartNode BuildWateringCan(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Stands upright on its base; the drum's foot is the lowest part, hovering just clear so the
            // gentle bob never dips it through the floor (0.24 - 0.06 bob - 0.13 to the foot > 0).
            baseHeight = 0.24f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color metal = def.Tint;                                                          // enamelled steel-blue body
            Color metalDeep = new Color(metal.r * 0.72f, metal.g * 0.72f, metal.b * 0.72f);  // ribs, base skirt and shading
            Color metalHi = Color.Lerp(metal, Color.white, 0.3f);                            // rolled rim and rose highlights
            Color handleCol = new Color(metal.r * 0.6f, metal.g * 0.62f, metal.b * 0.66f);   // darker galvanised handle bar
            Color dark = new Color(0.08f, 0.09f, 0.10f);                                      // open mouth and rose holes

            PartMat metalMat = PartMat.Of(metal, smoothness: 0.42f, metallic: 0.6f);
            PartMat metalDeepMat = PartMat.Of(metalDeep, smoothness: 0.35f, metallic: 0.55f);
            PartMat metalHiMat = PartMat.Of(metalHi, smoothness: 0.6f, metallic: 0.7f);
            PartMat handleMat = PartMat.Of(handleCol, smoothness: 0.5f, metallic: 0.8f);
            PartMat darkMat = PartMat.Of(dark, smoothness: 0.2f);

            const float bodyD = 0.29f;   // main drum diameter

            // ---- Steel drum body, standing on a slightly flared foot ----
            Cylinder(root, "Body", 0f, bodyD, 0.24f, metalMat);
            Cylinder(root, "BaseFlare", -0.108f, 0.30f, 0.028f, metalDeepMat);
            Cylinder(root, "Base", -0.124f, 0.28f, 0.012f, metalDeepMat);

            // Rolled galvanised ribs strengthening the drum, standing a hair proud of the body.
            Cylinder(root, "RibLow", -0.05f, bodyD + 0.008f, 0.016f, metalDeepMat);
            Cylinder(root, "RibHigh", 0.05f, bodyD + 0.008f, 0.016f, metalDeepMat);

            // Bright rolled rim around a dark open filling mouth (the dark disc reads as the hole).
            Cylinder(root, "Rim", 0.122f, 0.30f, 0.024f, metalHiMat);
            Cylinder(root, "Mouth", 0.13f, 0.256f, 0.008f, darkMat);

            // ---- Long spout rising from the lower front (+X) up toward the rose ----
            const float spoutDeg = 42f;
            float sr = spoutDeg * Mathf.Deg2Rad;
            Vector3 sdir = new Vector3(Mathf.Cos(sr), Mathf.Sin(sr), 0f);
            Vector3 s0 = new Vector3(0.12f, -0.03f, 0f);
            // A Rod points along +Y at rest; a Z-rotation of (heading − 90) lays its length along the spout.
            float lay = spoutDeg - 90f;

            Sphere(root, "SpoutCollar", s0, Vector3.zero, new Vector3(0.075f, 0.075f, 0.075f), metalMat);
            Rod(root, "Spout1", s0 + sdir * 0.10f, new Vector3(0f, 0f, lay), 0.058f, 0.20f, metalMat);
            Rod(root, "Spout2", s0 + sdir * 0.24f, new Vector3(0f, 0f, lay), 0.046f, 0.16f, metalMat);

            // ---- Perforated rose at the spout's tip, its face turned along the spout ----
            // Built in a sub-root whose local Y runs along the spout, so the round faceplate turns the right
            // way and the sprinkler holes sit flat on it.
            var rose = new PartNode("Rose");
            rose.SetParent(root, false);
            rose.localPosition = s0 + sdir * 0.34f;
            rose.localEulerAngles = new Vector3(0f, 0f, lay);   // local Y now points along the spout

            Rod(rose, "Neck", new Vector3(0f, -0.03f, 0f), Vector3.zero, 0.05f, 0.04f, metalMat);
            Cylinder(rose, "Plate", 0f, 0.115f, 0.024f, metalMat);
            Cylinder(rose, "RoseRim", 0.012f, 0.122f, 0.012f, metalHiMat);
            Cylinder(rose, "Face", 0.02f, 0.104f, 0.006f, metalHiMat);

            // Sprinkler holes: a centre hole ringed by two rows of dark pinpricks across the faceplate.
            Cylinder(rose, "Hole", 0.024f, 0.01f, 0.008f, darkMat);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Cylinder(rose, "HoleInner" + i, 0f, 0.009f, 0.008f, darkMat).localPosition =
                    new Vector3(0.026f * Mathf.Cos(a), 0.024f, 0.026f * Mathf.Sin(a));
            }
            for (int i = 0; i < 11; i++)
            {
                float a = i * (360f / 11f) * Mathf.Deg2Rad;
                Cylinder(rose, "HoleOuter" + i, 0f, 0.008f, 0.008f, darkMat).localPosition =
                    new Vector3(0.045f * Mathf.Cos(a), 0.024f, 0.045f * Mathf.Sin(a));
            }

            // ---- Carrying handle arching over the top, in the spout's plane ----
            // Riveted mount pads front and back, then a round bar traced up and over as short arc segments.
            Block(root, "MountFront", new Vector3(0.132f, 0.10f, 0f), new Vector3(0.022f, 0.03f, 0.055f), metalDeepMat);
            Block(root, "MountBack", new Vector3(-0.132f, 0.10f, 0f), new Vector3(0.022f, 0.03f, 0.055f), metalDeepMat);

            const float hcx = 0f, hcy = 0.10f, hR = 0.14f;   // arch centre and radius
            const int hseg = 10;
            Vector3 hprev = ArcPoint(hcx, hcy, hR, 0f);      // front foot at (+R, cy), sweeping over to the back
            for (int i = 1; i <= hseg; i++)
            {
                float a = Mathf.Lerp(0f, 180f, i / (float)hseg);
                Vector3 p = ArcPoint(hcx, hcy, hR, a);
                Vector3 mid = (hprev + p) * 0.5f;
                float len = (p - hprev).magnitude * 1.35f;   // overlap so the arch reads solid
                float dirDeg = Mathf.Atan2(p.y - hprev.y, p.x - hprev.x) * Mathf.Rad2Deg;
                Rod(root, "Handle" + i, mid, new Vector3(0f, 0f, dirDeg - 90f), 0.017f, len, handleMat);
                hprev = p;
            }

            // ---- Smaller D-handle bowing off the back (−X) for pouring leverage ----
            Block(root, "DMountTop", new Vector3(-0.14f, 0.078f, 0f), new Vector3(0.02f, 0.025f, 0.05f), metalDeepMat);
            Block(root, "DMountBottom", new Vector3(-0.14f, -0.04f, 0f), new Vector3(0.02f, 0.025f, 0.05f), metalDeepMat);

            const float dcx = -0.115f, dcy = 0.02f, dR = 0.075f;   // arc centre and radius on the back
            const int dseg = 6;
            Vector3 dprev = ArcPoint(dcx, dcy, dR, 128f);
            for (int i = 1; i <= dseg; i++)
            {
                float a = Mathf.Lerp(128f, 232f, i / (float)dseg);   // sweeps out round the −X side
                Vector3 p = ArcPoint(dcx, dcy, dR, a);
                Vector3 mid = (dprev + p) * 0.5f;
                float len = (p - dprev).magnitude * 1.35f;
                float dirDeg = Mathf.Atan2(p.y - dprev.y, p.x - dprev.x) * Mathf.Rad2Deg;
                Rod(root, "DHandle" + i, mid, new Vector3(0f, 0f, dirDeg - 90f), 0.015f, len, handleMat);
                dprev = p;
            }

            return root;
        }

        /// <summary>
        /// A packet of potato seeds: a small upright paper seed envelope with the pinked, tear-open top
        /// edge of a garden packet, printed on the front with a sprouting tan potato over a cream label and
        /// a few planting lines on the back, with a couple of little seed potatoes spilled at its foot so a
        /// dropped packet reads unmistakably as something you plant. The envelope takes the item's earthy
        /// tint so it stays kraft-brown where it lies on the floor.
        /// </summary>
        private static PartNode BuildPotatoSeeds(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the upright packet so its bottom edge and the spilled seed potatoes clear the floor at
            // the bottom of the bob.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color paper = def.Tint;                                                          // kraft paper envelope
            Color paperShade = new Color(paper.r * 0.80f, paper.g * 0.80f, paper.b * 0.80f);  // shaded folds
            Color seal = new Color(paper.r * 0.66f, paper.g * 0.66f, paper.b * 0.66f);        // glued, folded top seal
            Color cream = new Color(0.90f, 0.87f, 0.78f);                                     // printed label field
            Color ink = new Color(0.20f, 0.17f, 0.13f);                                       // dark printed text
            Color pic = new Color(0.80f, 0.68f, 0.46f);                                       // printed potato picture
            Color eye = new Color(0.32f, 0.22f, 0.12f);                                       // dark potato eyes
            Color sprout = new Color(0.52f, 0.66f, 0.34f);                                    // pale green sprout
            Color spud = new Color(0.72f, 0.60f, 0.36f);                                      // real spilled seed potatoes

            PartMat paperMat = PartMat.Of(paper, smoothness: 0.10f);
            PartMat sealMat = PartMat.Of(seal, smoothness: 0.10f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.10f);
            PartMat picMat = PartMat.Of(pic, smoothness: 0.12f);
            PartMat eyeMat = PartMat.Of(eye, smoothness: 0.08f);
            PartMat sproutMat = PartMat.Of(sprout, smoothness: 0.18f);
            PartMat spudMat = PartMat.Of(spud, smoothness: 0.12f);

            const float packW = 0.17f;          // envelope width (X)
            const float packH = 0.23f;          // envelope height (Y)
            const float packD = 0.028f;         // envelope thickness (Z)
            const float packBottom = -0.10f;    // local ground plane the packet and spilled seeds rest on
            const float bodyCy = packBottom + packH * 0.5f;
            const float packTop = packBottom + packH;
            const float frontZ = packD * 0.5f;  // front face of the envelope
            const float backZ = -packD * 0.5f;  // back face

            // ---- Paper envelope, fuller and bulging low where the seeds have settled ----
            Block(root, "Body", new Vector3(0f, bodyCy, 0f), new Vector3(packW, packH, packD), paperMat);
            Sphere(root, "Bulge", new Vector3(0f, packBottom + 0.045f, 0f), Vector3.zero, new Vector3(packW - 0.03f, 0.075f, packD + 0.02f), paperMat);

            // Folded, glued top seal with the classic pinked (zig-zag) tear edge of a garden packet above it.
            Block(root, "TopFold", new Vector3(0f, packTop - 0.014f, 0f), new Vector3(packW, 0.028f, packD + 0.004f), sealMat);
            for (int i = 0; i < 9; i++)
            {
                float x = -0.072f + i * 0.018f;
                Block(root, "Tooth" + i, new Vector3(x, packTop, 0f), new Vector3(0.016f, 0.016f, packD), sealMat)
                    .localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            // ---- Front: cream label field, variety text, and a printed sprouting potato ----
            Block(root, "Label", new Vector3(0f, 0.05f, frontZ + 0.002f), new Vector3(packW - 0.028f, 0.13f, 0.004f), creamMat);
            Block(root, "Title", new Vector3(0f, 0.10f, frontZ + 0.003f), new Vector3(packW - 0.05f, 0.02f, 0.004f), inkMat);
            Block(root, "Subtitle", new Vector3(0f, 0.079f, frontZ + 0.003f), new Vector3(0.085f, 0.007f, 0.004f), inkMat);

            // Printed potato: an oblong tan tuber with a knobbly bump, dark eyes, and a little green sprout.
            Sphere(root, "Spud", new Vector3(0f, 0.035f, frontZ + 0.012f), new Vector3(0f, 0f, 8f), new Vector3(0.11f, 0.08f, 0.02f), picMat);
            Sphere(root, "SpudLobe", new Vector3(0.035f, 0.05f, frontZ + 0.012f), Vector3.zero, new Vector3(0.055f, 0.05f, 0.018f), picMat);
            Sphere(root, "PicEye1", new Vector3(0.018f, 0.045f, frontZ + 0.02f), Vector3.zero, new Vector3(0.016f, 0.013f, 0.012f), eyeMat);
            Sphere(root, "PicEye2", new Vector3(-0.028f, 0.028f, frontZ + 0.02f), Vector3.zero, new Vector3(0.013f, 0.011f, 0.011f), eyeMat);
            Sphere(root, "PicEye3", new Vector3(0.04f, 0.02f, frontZ + 0.02f), Vector3.zero, new Vector3(0.012f, 0.010f, 0.011f), eyeMat);
            Rod(root, "PicSprout", new Vector3(0.03f, 0.076f, frontZ + 0.016f), new Vector3(0f, 0f, 18f), 0.006f, 0.03f, sproutMat);
            Sphere(root, "PicLeaf1", new Vector3(0.048f, 0.092f, frontZ + 0.016f), new Vector3(0f, 0f, 35f), new Vector3(0.026f, 0.014f, 0.012f), sproutMat);
            Sphere(root, "PicLeaf2", new Vector3(0.022f, 0.09f, frontZ + 0.016f), new Vector3(0f, 0f, -30f), new Vector3(0.022f, 0.013f, 0.012f), sproutMat);

            // Small printed brand box and net-weight bar low on the front.
            Block(root, "Brand", new Vector3(-0.05f, -0.002f, frontZ + 0.003f), new Vector3(0.032f, 0.022f, 0.004f), inkMat);
            Block(root, "BrandInner", new Vector3(-0.05f, -0.002f, frontZ + 0.004f), new Vector3(0.022f, 0.012f, 0.004f), creamMat);
            Block(root, "Weight", new Vector3(0.045f, -0.006f, frontZ + 0.003f), new Vector3(0.045f, 0.008f, 0.004f), inkMat);

            // ---- Back: a cream instruction panel with a few printed planting lines ----
            Block(root, "BackPanel", new Vector3(0f, 0.04f, backZ - 0.002f), new Vector3(packW - 0.04f, 0.15f, 0.004f), creamMat);
            Block(root, "BackTitle", new Vector3(0f, 0.10f, backZ - 0.003f), new Vector3(packW - 0.06f, 0.016f, 0.004f), inkMat);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.06f - i * 0.025f;
                Block(root, "BackLine" + i, new Vector3(0f, y, backZ - 0.003f), new Vector3(packW - 0.07f, 0.006f, 0.004f), inkMat);
            }

            // ---- A few little seed potatoes spilled at the foot of the packet ----
            BuildSeedPotato(root, new Vector3(-0.05f, packBottom + 0.024f, 0.052f), 20f, 1.0f, spudMat, eyeMat, sproutMat, true);
            BuildSeedPotato(root, new Vector3(0.015f, packBottom + 0.028f, 0.072f), -40f, 1.15f, spudMat, eyeMat, sproutMat, false);
            BuildSeedPotato(root, new Vector3(0.07f, packBottom + 0.020f, 0.05f), 120f, 0.85f, spudMat, eyeMat, sproutMat, true);

            return root;
        }

        /// <summary>Builds one little seed potato under <paramref name="parent"/> in its own position and
        /// yaw frame: a knobbly tan tuber with a couple of dark eyes and, optionally, a short green sprout,
        /// scaled by <paramref name="scale"/> so a few can be spilled at different sizes and angles.</summary>
        private static void BuildSeedPotato(PartNode parent, Vector3 localPos, float yaw, float scale,
            PartMat skinMat, PartMat eyeMat, PartMat sproutMat, bool withSprout)
        {
            var p = new PartNode("SeedPotato");
            p.SetParent(parent, false);
            p.localPosition = localPos;
            p.localEulerAngles = new Vector3(0f, yaw, 0f);
            float s = scale;

            Sphere(p, "Body", Vector3.zero, Vector3.zero, new Vector3(0.062f * s, 0.048f * s, 0.052f * s), skinMat);
            Sphere(p, "Bump", new Vector3(0.024f * s, 0.006f * s, 0.004f * s), Vector3.zero, new Vector3(0.032f * s, 0.03f * s, 0.032f * s), skinMat);
            Sphere(p, "Eye1", new Vector3(0.008f * s, 0.02f * s, 0.02f * s), Vector3.zero, new Vector3(0.012f * s, 0.009f * s, 0.012f * s), eyeMat);
            Sphere(p, "Eye2", new Vector3(-0.02f * s, 0.012f * s, -0.012f * s), Vector3.zero, new Vector3(0.010f * s, 0.008f * s, 0.010f * s), eyeMat);
            if (withSprout)
            {
                Rod(p, "Sprout", new Vector3(0.012f * s, 0.03f * s, 0.006f * s), new Vector3(12f, 0f, 16f), 0.005f * s, 0.03f * s, sproutMat);
                Sphere(p, "SproutTip", new Vector3(0.02f * s, 0.05f * s, 0.008f * s), Vector3.zero, new Vector3(0.016f * s, 0.01f * s, 0.012f * s), sproutMat);
            }
        }

        /// <summary>
        /// A garden seed packet for carrots: the same upright kraft-paper envelope with a pinked tear-top as
        /// the potato-seed packet, so the two read as a matching set on the shelf, but printed here with a
        /// carrot — an orange tapered root banded by grooves under a frilly green top — and a little scatter
        /// of tiny spilled seeds at its foot. The envelope takes the item's tint so it reads as brown seed
        /// paper where it lies on the floor.
        /// </summary>
        private static PartNode BuildCarrotSeeds(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the upright packet so its bottom edge and the spilled seeds clear the floor at the bottom
            // of the bob, matching the potato-seed packet beside it.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color paper = def.Tint;                                                     // kraft paper envelope
            Color seal = new Color(paper.r * 0.66f, paper.g * 0.66f, paper.b * 0.66f);  // glued, folded top seal
            Color cream = new Color(0.90f, 0.87f, 0.78f);                               // printed label field
            Color ink = new Color(0.20f, 0.17f, 0.13f);                                 // dark printed text
            Color carrotPic = new Color(0.85f, 0.50f, 0.20f);                           // printed carrot root (item orange)
            Color carrotDeep = new Color(0.66f, 0.37f, 0.14f);                          // shaded grooves banding the root
            Color leaf = new Color(0.42f, 0.56f, 0.24f);                                // bright feathery carrot top
            Color stalk = new Color(0.30f, 0.42f, 0.16f);                               // darker leaf stalks
            Color seed = new Color(0.60f, 0.49f, 0.30f);                                // real spilled carrot seeds
            Color seedHi = new Color(0.70f, 0.59f, 0.38f);                              // lighter seed catch-light
            Color seedDeep = new Color(0.44f, 0.35f, 0.21f);                            // shaded seed pile

            PartMat paperMat = PartMat.Of(paper, smoothness: 0.10f);
            PartMat sealMat = PartMat.Of(seal, smoothness: 0.10f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.10f);
            PartMat carrotMat = PartMat.Of(carrotPic, smoothness: 0.14f);
            PartMat carrotDeepMat = PartMat.Of(carrotDeep, smoothness: 0.12f);
            PartMat leafMat = PartMat.Of(leaf, smoothness: 0.22f);
            PartMat stalkMat = PartMat.Of(stalk, smoothness: 0.18f);
            PartMat seedMat = PartMat.Of(seed, smoothness: 0.12f);
            PartMat seedHiMat = PartMat.Of(seedHi, smoothness: 0.14f);
            PartMat seedDeepMat = PartMat.Of(seedDeep, smoothness: 0.10f);

            const float packW = 0.17f;          // envelope width (X)
            const float packH = 0.23f;          // envelope height (Y)
            const float packD = 0.028f;         // envelope thickness (Z)
            const float packBottom = -0.10f;    // local ground plane the packet and spilled seeds rest on
            const float bodyCy = packBottom + packH * 0.5f;
            const float packTop = packBottom + packH;
            const float frontZ = packD * 0.5f;  // front face of the envelope
            const float backZ = -packD * 0.5f;  // back face

            // ---- Paper envelope, fuller and bulging low where the seeds have settled ----
            Block(root, "Body", new Vector3(0f, bodyCy, 0f), new Vector3(packW, packH, packD), paperMat);
            Sphere(root, "Bulge", new Vector3(0f, packBottom + 0.045f, 0f), Vector3.zero, new Vector3(packW - 0.03f, 0.075f, packD + 0.02f), paperMat);

            // Folded, glued top seal with the classic pinked (zig-zag) tear edge of a garden packet above it.
            Block(root, "TopFold", new Vector3(0f, packTop - 0.014f, 0f), new Vector3(packW, 0.028f, packD + 0.004f), sealMat);
            for (int i = 0; i < 9; i++)
            {
                float x = -0.072f + i * 0.018f;
                Block(root, "Tooth" + i, new Vector3(x, packTop, 0f), new Vector3(0.016f, 0.016f, packD), sealMat)
                    .localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            // ---- Front: cream label field, variety text, and a printed carrot ----
            Block(root, "Label", new Vector3(0f, 0.05f, frontZ + 0.002f), new Vector3(packW - 0.028f, 0.13f, 0.004f), creamMat);
            Block(root, "Title", new Vector3(0f, 0.10f, frontZ + 0.003f), new Vector3(packW - 0.05f, 0.02f, 0.004f), inkMat);

            // Printed carrot: an orange tapered root pointing down, banded by a couple of darker grooves, under
            // a frilly green top sprouting from the crown. Flattened in Z so it sits proud of the label like a
            // print rather than a lump.
            Sphere(root, "Root1", new Vector3(0f, 0.035f, frontZ + 0.012f), Vector3.zero, new Vector3(0.066f, 0.048f, 0.018f), carrotMat);
            Sphere(root, "Root2", new Vector3(0f, 0.014f, frontZ + 0.012f), Vector3.zero, new Vector3(0.05f, 0.046f, 0.018f), carrotMat);
            Sphere(root, "Root3", new Vector3(0f, -0.005f, frontZ + 0.012f), Vector3.zero, new Vector3(0.034f, 0.042f, 0.017f), carrotMat);
            Sphere(root, "RootTip", new Vector3(0f, -0.026f, frontZ + 0.011f), Vector3.zero, new Vector3(0.016f, 0.036f, 0.016f), carrotMat);
            Block(root, "PicGroove1", new Vector3(0f, 0.024f, frontZ + 0.016f), new Vector3(0.054f, 0.006f, 0.005f), carrotDeepMat);
            Block(root, "PicGroove2", new Vector3(0f, 0.003f, frontZ + 0.016f), new Vector3(0.038f, 0.005f, 0.005f), carrotDeepMat);
            Block(root, "PicGroove3", new Vector3(0f, -0.016f, frontZ + 0.016f), new Vector3(0.022f, 0.005f, 0.005f), carrotDeepMat);

            // Frilly green top: a few stalks fanning up from the crown into a heap of feathery frond clumps,
            // kept below the title bar.
            Rod(root, "PicStalkC", new Vector3(0f, 0.05f, frontZ + 0.013f), Vector3.zero, 0.006f, 0.028f, stalkMat);
            Rod(root, "PicStalkL", new Vector3(-0.018f, 0.05f, frontZ + 0.013f), new Vector3(0f, 0f, 24f), 0.005f, 0.026f, stalkMat);
            Rod(root, "PicStalkR", new Vector3(0.018f, 0.05f, frontZ + 0.013f), new Vector3(0f, 0f, -24f), 0.005f, 0.026f, stalkMat);
            Sphere(root, "PicFrondM", new Vector3(0f, 0.062f, frontZ + 0.014f), Vector3.zero, new Vector3(0.032f, 0.02f, 0.013f), leafMat);
            Sphere(root, "PicFrondL", new Vector3(-0.027f, 0.067f, frontZ + 0.014f), new Vector3(0f, 0f, 22f), new Vector3(0.03f, 0.021f, 0.013f), leafMat);
            Sphere(root, "PicFrondR", new Vector3(0.027f, 0.067f, frontZ + 0.014f), new Vector3(0f, 0f, -22f), new Vector3(0.03f, 0.021f, 0.013f), leafMat);
            Sphere(root, "PicFrondT", new Vector3(0f, 0.076f, frontZ + 0.014f), Vector3.zero, new Vector3(0.036f, 0.024f, 0.014f), leafMat);

            // Small printed brand box and net-weight bar low on the front.
            Block(root, "Brand", new Vector3(-0.05f, -0.002f, frontZ + 0.003f), new Vector3(0.032f, 0.022f, 0.004f), inkMat);
            Block(root, "BrandInner", new Vector3(-0.05f, -0.002f, frontZ + 0.004f), new Vector3(0.022f, 0.012f, 0.004f), creamMat);
            Block(root, "Weight", new Vector3(0.045f, -0.006f, frontZ + 0.003f), new Vector3(0.045f, 0.008f, 0.004f), inkMat);

            // ---- Back: a cream instruction panel with a few printed planting lines ----
            Block(root, "BackPanel", new Vector3(0f, 0.04f, backZ - 0.002f), new Vector3(packW - 0.04f, 0.15f, 0.004f), creamMat);
            Block(root, "BackTitle", new Vector3(0f, 0.10f, backZ - 0.003f), new Vector3(packW - 0.06f, 0.016f, 0.004f), inkMat);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.06f - i * 0.025f;
                Block(root, "BackLine" + i, new Vector3(0f, y, backZ - 0.003f), new Vector3(packW - 0.07f, 0.006f, 0.004f), inkMat);
            }

            // ---- A few little heaps of carrot seeds spilled at the foot of the packet, with a couple of
            // strays trailing forward so the pile reads as spilled and the spin has something to catch on ----
            BuildCarrotSeedHeap(root, new Vector3(-0.045f, packBottom + 0.003f, 0.05f), 18f, 1.0f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.028f, packBottom + 0.003f, 0.072f), -35f, 1.2f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.075f, packBottom + 0.003f, 0.048f), 120f, 0.8f, seedMat, seedHiMat, seedDeepMat);
            Sphere(root, "Stray1", new Vector3(-0.012f, packBottom + 0.008f, 0.096f), new Vector3(0f, 35f, 8f), new Vector3(0.014f, 0.008f, 0.010f), seedMat);
            Sphere(root, "Stray2", new Vector3(0.055f, packBottom + 0.008f, 0.088f), new Vector3(0f, -50f, 8f), new Vector3(0.013f, 0.008f, 0.010f), seedHiMat);

            return root;
        }

        /// <summary>Builds one little spilled heap of carrot seeds under <paramref name="parent"/> in its own
        /// position and yaw frame: a low shaded mound with a scatter of tiny oblong seed flecks resting on top,
        /// alternating tone and tilt, scaled by <paramref name="scale"/> so a few heaps can be spilled at
        /// different sizes.</summary>
        private static void BuildCarrotSeedHeap(PartNode parent, Vector3 localPos, float yaw, float scale,
            PartMat seedMat, PartMat seedHiMat, PartMat seedDeepMat)
        {
            var h = new PartNode("SeedHeap");
            h.SetParent(parent, false);
            h.localPosition = localPos;
            h.localEulerAngles = new Vector3(0f, yaw, 0f);
            float s = scale;

            // Low mound the loose seeds have gathered into.
            Sphere(h, "Mound", new Vector3(0f, 0.006f * s, 0f), Vector3.zero, new Vector3(0.055f * s, 0.016f * s, 0.042f * s), seedDeepMat);

            // A scatter of tiny oblong seed flecks resting on the mound. Each row is x, z, yaw; the tone and a
            // small height step cycle so the pile reads as many little seeds rather than one blob.
            float[,] flecks =
            {
                { -0.018f,  0.010f,  10f },
                {  0.006f, -0.008f, -25f },
                {  0.022f,  0.006f,  40f },
                { -0.006f,  0.014f,  75f },
                {  0.014f, -0.014f, 120f },
                { -0.024f, -0.006f, -55f },
                {  0.030f,  0.012f,  15f },
                {  0.000f,  0.000f,  90f },
            };
            for (int i = 0; i < flecks.GetLength(0); i++)
            {
                PartMat m = (i % 3 == 0) ? seedDeepMat : (i % 3 == 1) ? seedMat : seedHiMat;
                float fy = (0.013f + 0.003f * (i % 2)) * s;   // small height variation so they pile a little
                Sphere(h, "Seed" + i, new Vector3(flecks[i, 0] * s, fy, flecks[i, 1] * s),
                    new Vector3(0f, flecks[i, 2], 8f), new Vector3(0.014f * s, 0.008f * s, 0.010f * s), m);
            }
        }

        /// <summary>
        /// A garden seed packet for tomatoes: the same upright kraft-paper envelope with a pinked tear-top as
        /// the potato- and carrot-seed packets, so all three read as a matching set on the shelf, but printed
        /// here with a plump red tomato — a squat lobed fruit under a green calyx star — and a little scatter
        /// of tiny pale spilled seeds at its foot. The envelope takes the item's tint so it reads as brown
        /// seed paper where it lies on the floor.
        /// </summary>
        private static PartNode BuildTomatoSeeds(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the upright packet so its bottom edge and the spilled seeds clear the floor at the bottom
            // of the bob, matching the potato- and carrot-seed packets beside it.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color paper = def.Tint;                                                     // kraft paper envelope
            Color seal = new Color(paper.r * 0.66f, paper.g * 0.66f, paper.b * 0.66f);  // glued, folded top seal
            Color cream = new Color(0.90f, 0.87f, 0.78f);                               // printed label field
            Color ink = new Color(0.20f, 0.17f, 0.13f);                                 // dark printed text
            Color tomatoPic = new Color(0.82f, 0.28f, 0.22f);                           // printed tomato (item red)
            Color tomatoDeep = new Color(0.64f, 0.21f, 0.16f);                          // shaded belly and lobe seams
            Color shine = new Color(0.96f, 0.72f, 0.66f);                               // glossy highlight on the fruit
            Color calyx = new Color(0.42f, 0.56f, 0.24f);                               // bright green sepal star
            Color stem = new Color(0.30f, 0.42f, 0.16f);                                // darker woody stem nub
            Color seed = new Color(0.85f, 0.79f, 0.60f);                                // real spilled tomato seeds (pale tan)
            Color seedHi = new Color(0.92f, 0.87f, 0.70f);                              // lighter seed catch-light
            Color seedDeep = new Color(0.64f, 0.58f, 0.42f);                            // shaded seed pile

            PartMat paperMat = PartMat.Of(paper, smoothness: 0.10f);
            PartMat sealMat = PartMat.Of(seal, smoothness: 0.10f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.10f);
            PartMat tomatoMat = PartMat.Of(tomatoPic, smoothness: 0.50f);   // wet, glossy fruit sheen
            PartMat tomatoDeepMat = PartMat.Of(tomatoDeep, smoothness: 0.45f);
            PartMat shineMat = PartMat.Of(shine, smoothness: 0.60f);
            PartMat calyxMat = PartMat.Of(calyx, smoothness: 0.22f);
            PartMat stemMat = PartMat.Of(stem, smoothness: 0.18f);
            PartMat seedMat = PartMat.Of(seed, smoothness: 0.12f);
            PartMat seedHiMat = PartMat.Of(seedHi, smoothness: 0.14f);
            PartMat seedDeepMat = PartMat.Of(seedDeep, smoothness: 0.10f);

            const float packW = 0.17f;          // envelope width (X)
            const float packH = 0.23f;          // envelope height (Y)
            const float packD = 0.028f;         // envelope thickness (Z)
            const float packBottom = -0.10f;    // local ground plane the packet and spilled seeds rest on
            const float bodyCy = packBottom + packH * 0.5f;
            const float packTop = packBottom + packH;
            const float frontZ = packD * 0.5f;  // front face of the envelope
            const float backZ = -packD * 0.5f;  // back face

            // ---- Paper envelope, fuller and bulging low where the seeds have settled ----
            Block(root, "Body", new Vector3(0f, bodyCy, 0f), new Vector3(packW, packH, packD), paperMat);
            Sphere(root, "Bulge", new Vector3(0f, packBottom + 0.045f, 0f), Vector3.zero, new Vector3(packW - 0.03f, 0.075f, packD + 0.02f), paperMat);

            // Folded, glued top seal with the classic pinked (zig-zag) tear edge of a garden packet above it.
            Block(root, "TopFold", new Vector3(0f, packTop - 0.014f, 0f), new Vector3(packW, 0.028f, packD + 0.004f), sealMat);
            for (int i = 0; i < 9; i++)
            {
                float x = -0.072f + i * 0.018f;
                Block(root, "Tooth" + i, new Vector3(x, packTop, 0f), new Vector3(0.016f, 0.016f, packD), sealMat)
                    .localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            // ---- Front: cream label field, variety text, and a printed tomato ----
            Block(root, "Label", new Vector3(0f, 0.05f, frontZ + 0.002f), new Vector3(packW - 0.028f, 0.13f, 0.004f), creamMat);
            Block(root, "Title", new Vector3(0f, 0.10f, frontZ + 0.003f), new Vector3(packW - 0.05f, 0.02f, 0.004f), inkMat);

            // Printed tomato: a squat red fruit with swollen shoulders and a shaded belly, faint vertical lobe
            // seams, a glossy highlight, and a green calyx star on top. Flattened in Z so it sits proud of the
            // label like a print rather than a lump, and kept below the title bar.
            Sphere(root, "Fruit", new Vector3(0f, 0.028f, frontZ + 0.012f), Vector3.zero, new Vector3(0.084f, 0.07f, 0.018f), tomatoMat);
            Sphere(root, "Shoulders", new Vector3(0f, 0.04f, frontZ + 0.012f), Vector3.zero, new Vector3(0.088f, 0.044f, 0.018f), tomatoMat);
            Sphere(root, "Belly", new Vector3(0f, 0.012f, frontZ + 0.011f), Vector3.zero, new Vector3(0.072f, 0.044f, 0.017f), tomatoDeepMat);
            Block(root, "Seam0", new Vector3(0f, 0.03f, frontZ + 0.016f), new Vector3(0.004f, 0.056f, 0.005f), tomatoDeepMat);
            Block(root, "SeamL", new Vector3(-0.022f, 0.03f, frontZ + 0.016f), new Vector3(0.004f, 0.05f, 0.005f), tomatoDeepMat);
            Block(root, "SeamR", new Vector3(0.022f, 0.03f, frontZ + 0.016f), new Vector3(0.004f, 0.05f, 0.005f), tomatoDeepMat);
            Sphere(root, "Shine", new Vector3(-0.024f, 0.045f, frontZ + 0.016f), Vector3.zero, new Vector3(0.022f, 0.018f, 0.01f), shineMat);

            // Green calyx crown: a small star of sepals fanning from the top of the fruit, with a short woody
            // stem nub in the middle. Each row is x, y, roll, and length.
            float[,] sepals =
            {
                {  0.000f, 0.073f,   0f, 0.030f },
                { -0.020f, 0.068f,  42f, 0.028f },
                {  0.020f, 0.068f, -42f, 0.028f },
                { -0.033f, 0.060f,  70f, 0.026f },
                {  0.033f, 0.060f, -70f, 0.026f },
            };
            for (int i = 0; i < sepals.GetLength(0); i++)
                Sphere(root, "Sepal" + i, new Vector3(sepals[i, 0], sepals[i, 1], frontZ + 0.015f),
                    new Vector3(0f, 0f, sepals[i, 2]), new Vector3(0.013f, sepals[i, 3], 0.012f), calyxMat);
            Rod(root, "PicStem", new Vector3(0f, 0.072f, frontZ + 0.016f), Vector3.zero, 0.006f, 0.014f, stemMat);

            // Small printed brand box and net-weight bar low on the front.
            Block(root, "Brand", new Vector3(-0.05f, -0.002f, frontZ + 0.003f), new Vector3(0.032f, 0.022f, 0.004f), inkMat);
            Block(root, "BrandInner", new Vector3(-0.05f, -0.002f, frontZ + 0.004f), new Vector3(0.022f, 0.012f, 0.004f), creamMat);
            Block(root, "Weight", new Vector3(0.045f, -0.006f, frontZ + 0.003f), new Vector3(0.045f, 0.008f, 0.004f), inkMat);

            // ---- Back: a cream instruction panel with a few printed planting lines ----
            Block(root, "BackPanel", new Vector3(0f, 0.04f, backZ - 0.002f), new Vector3(packW - 0.04f, 0.15f, 0.004f), creamMat);
            Block(root, "BackTitle", new Vector3(0f, 0.10f, backZ - 0.003f), new Vector3(packW - 0.06f, 0.016f, 0.004f), inkMat);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.06f - i * 0.025f;
                Block(root, "BackLine" + i, new Vector3(0f, y, backZ - 0.003f), new Vector3(packW - 0.07f, 0.006f, 0.004f), inkMat);
            }

            // ---- A few little heaps of tomato seeds spilled at the foot of the packet, with a couple of
            // strays trailing forward so the pile reads as spilled and the spin has something to catch on.
            // Reuses the carrot packet's generic seed-heap helper with pale tomato-seed tones. ----
            BuildCarrotSeedHeap(root, new Vector3(-0.045f, packBottom + 0.003f, 0.05f), 18f, 1.0f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.028f, packBottom + 0.003f, 0.072f), -35f, 1.2f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.075f, packBottom + 0.003f, 0.048f), 120f, 0.8f, seedMat, seedHiMat, seedDeepMat);
            Sphere(root, "Stray1", new Vector3(-0.012f, packBottom + 0.008f, 0.096f), new Vector3(0f, 35f, 8f), new Vector3(0.014f, 0.008f, 0.010f), seedMat);
            Sphere(root, "Stray2", new Vector3(0.055f, packBottom + 0.008f, 0.088f), new Vector3(0f, -50f, 8f), new Vector3(0.013f, 0.008f, 0.010f), seedHiMat);

            return root;
        }

        /// <summary>
        /// A garden seed packet for cabbages: the same upright kraft-paper envelope with a pinked tear-top as
        /// the potato-, carrot- and tomato-seed packets, so all four read as a matching set on the shelf, but
        /// printed here with a round layered green cabbage head — darker cupped wrapper leaves flaring around a
        /// paler furled centre laced with pale ribs — and a little scatter of tiny round brown spilled seeds at
        /// its foot. The envelope takes the item's tint so it reads as brown seed paper where it lies on the floor.
        /// </summary>
        private static PartNode BuildCabbageSeeds(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the upright packet so its bottom edge and the spilled seeds clear the floor at the bottom
            // of the bob, matching the other seed packets beside it.
            baseHeight = 0.20f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color paper = def.Tint;                                                          // kraft paper envelope
            Color seal = new Color(paper.r * 0.66f, paper.g * 0.66f, paper.b * 0.66f);        // glued, folded top seal
            Color cream = new Color(0.90f, 0.87f, 0.78f);                                     // printed label field
            Color ink = new Color(0.20f, 0.17f, 0.13f);                                       // dark printed text
            Color leafInner = new Color(0.55f, 0.72f, 0.42f);                                 // pale inner-leaf green
            Color leafOuter = Color.Lerp(leafInner, new Color(0.24f, 0.42f, 0.30f), 0.6f);    // darker wrapper leaves
            Color leafMid = Color.Lerp(leafInner, leafOuter, 0.5f);                           // leaves between the two tones
            Color vein = new Color(0.84f, 0.87f, 0.63f);                                      // pale rib / midvein
            Color seed = new Color(0.40f, 0.28f, 0.18f);                                      // real spilled cabbage seeds (round, brown)
            Color seedHi = new Color(0.50f, 0.36f, 0.24f);                                    // lighter seed catch-light
            Color seedDeep = new Color(0.28f, 0.19f, 0.12f);                                  // shaded seed pile

            PartMat paperMat = PartMat.Of(paper, smoothness: 0.10f);
            PartMat sealMat = PartMat.Of(seal, smoothness: 0.10f);
            PartMat creamMat = PartMat.Of(cream, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.10f);
            PartMat leafInnerMat = PartMat.Of(leafInner, smoothness: 0.26f);   // faint waxy leaf sheen
            PartMat leafOuterMat = PartMat.Of(leafOuter, smoothness: 0.22f);
            PartMat leafMidMat = PartMat.Of(leafMid, smoothness: 0.24f);
            PartMat veinMat = PartMat.Of(vein, smoothness: 0.15f);
            PartMat seedMat = PartMat.Of(seed, smoothness: 0.14f);
            PartMat seedHiMat = PartMat.Of(seedHi, smoothness: 0.16f);
            PartMat seedDeepMat = PartMat.Of(seedDeep, smoothness: 0.10f);

            const float packW = 0.17f;          // envelope width (X)
            const float packH = 0.23f;          // envelope height (Y)
            const float packD = 0.028f;         // envelope thickness (Z)
            const float packBottom = -0.10f;    // local ground plane the packet and spilled seeds rest on
            const float bodyCy = packBottom + packH * 0.5f;
            const float packTop = packBottom + packH;
            const float frontZ = packD * 0.5f;  // front face of the envelope
            const float backZ = -packD * 0.5f;  // back face

            // ---- Paper envelope, fuller and bulging low where the seeds have settled ----
            Block(root, "Body", new Vector3(0f, bodyCy, 0f), new Vector3(packW, packH, packD), paperMat);
            Sphere(root, "Bulge", new Vector3(0f, packBottom + 0.045f, 0f), Vector3.zero, new Vector3(packW - 0.03f, 0.075f, packD + 0.02f), paperMat);

            // Folded, glued top seal with the classic pinked (zig-zag) tear edge of a garden packet above it.
            Block(root, "TopFold", new Vector3(0f, packTop - 0.014f, 0f), new Vector3(packW, 0.028f, packD + 0.004f), sealMat);
            for (int i = 0; i < 9; i++)
            {
                float x = -0.072f + i * 0.018f;
                Block(root, "Tooth" + i, new Vector3(x, packTop, 0f), new Vector3(0.016f, 0.016f, packD), sealMat)
                    .localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            // ---- Front: cream label field, variety text, and a printed cabbage ----
            Block(root, "Label", new Vector3(0f, 0.05f, frontZ + 0.002f), new Vector3(packW - 0.028f, 0.13f, 0.004f), creamMat);
            Block(root, "Title", new Vector3(0f, 0.10f, frontZ + 0.003f), new Vector3(packW - 0.05f, 0.02f, 0.004f), inkMat);

            // Printed cabbage: a round layered green head. Darker cupped wrapper leaves flare around and behind
            // a paler mid-tone ball, with a lighter furled centre and a few pale ribs fanning up from the base.
            // Flattened in Z so it sits proud of the label like a print rather than a lump, and kept below the
            // title bar. The wrapper leaves are drawn behind the head so they peek out only past its silhouette.
            Sphere(root, "WrapL", new Vector3(-0.05f, 0.012f, frontZ + 0.010f), new Vector3(0f, 0f, 34f), new Vector3(0.05f, 0.078f, 0.016f), leafOuterMat);
            Sphere(root, "WrapR", new Vector3(0.05f, 0.012f, frontZ + 0.010f), new Vector3(0f, 0f, -34f), new Vector3(0.05f, 0.078f, 0.016f), leafOuterMat);
            Sphere(root, "WrapBot", new Vector3(0f, -0.012f, frontZ + 0.010f), Vector3.zero, new Vector3(0.096f, 0.05f, 0.016f), leafOuterMat);
            Sphere(root, "Head", new Vector3(0f, 0.026f, frontZ + 0.012f), Vector3.zero, new Vector3(0.096f, 0.086f, 0.018f), leafMidMat);
            Sphere(root, "HeadHi", new Vector3(-0.016f, 0.036f, frontZ + 0.013f), Vector3.zero, new Vector3(0.052f, 0.052f, 0.016f), leafInnerMat);
            Sphere(root, "Crown", new Vector3(0.010f, 0.05f, frontZ + 0.014f), Vector3.zero, new Vector3(0.04f, 0.034f, 0.015f), leafInnerMat);
            Rod(root, "Vein0", new Vector3(0f, 0.022f, frontZ + 0.015f), Vector3.zero, 0.005f, 0.056f, veinMat);
            Rod(root, "VeinL", new Vector3(-0.026f, 0.018f, frontZ + 0.015f), new Vector3(0f, 0f, 26f), 0.004f, 0.05f, veinMat);
            Rod(root, "VeinR", new Vector3(0.026f, 0.018f, frontZ + 0.015f), new Vector3(0f, 0f, -26f), 0.004f, 0.05f, veinMat);

            // Small printed brand box and net-weight bar low on the front.
            Block(root, "Brand", new Vector3(-0.05f, -0.002f, frontZ + 0.003f), new Vector3(0.032f, 0.022f, 0.004f), inkMat);
            Block(root, "BrandInner", new Vector3(-0.05f, -0.002f, frontZ + 0.004f), new Vector3(0.022f, 0.012f, 0.004f), creamMat);
            Block(root, "Weight", new Vector3(0.045f, -0.006f, frontZ + 0.003f), new Vector3(0.045f, 0.008f, 0.004f), inkMat);

            // ---- Back: a cream instruction panel with a few printed planting lines ----
            Block(root, "BackPanel", new Vector3(0f, 0.04f, backZ - 0.002f), new Vector3(packW - 0.04f, 0.15f, 0.004f), creamMat);
            Block(root, "BackTitle", new Vector3(0f, 0.10f, backZ - 0.003f), new Vector3(packW - 0.06f, 0.016f, 0.004f), inkMat);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.06f - i * 0.025f;
                Block(root, "BackLine" + i, new Vector3(0f, y, backZ - 0.003f), new Vector3(packW - 0.07f, 0.006f, 0.004f), inkMat);
            }

            // ---- A few little heaps of cabbage seeds spilled at the foot of the packet, with a couple of
            // strays trailing forward so the pile reads as spilled and the spin has something to catch on.
            // Reuses the carrot packet's generic seed-heap helper with round brown cabbage-seed tones. ----
            BuildCarrotSeedHeap(root, new Vector3(-0.045f, packBottom + 0.003f, 0.05f), 18f, 1.0f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.028f, packBottom + 0.003f, 0.072f), -35f, 1.2f, seedMat, seedHiMat, seedDeepMat);
            BuildCarrotSeedHeap(root, new Vector3(0.075f, packBottom + 0.003f, 0.048f), 120f, 0.8f, seedMat, seedHiMat, seedDeepMat);
            Sphere(root, "Stray1", new Vector3(-0.012f, packBottom + 0.008f, 0.096f), new Vector3(0f, 35f, 8f), new Vector3(0.011f, 0.010f, 0.011f), seedMat);
            Sphere(root, "Stray2", new Vector3(0.055f, packBottom + 0.008f, 0.088f), new Vector3(0f, -50f, 8f), new Vector3(0.010f, 0.009f, 0.010f), seedHiMat);

            return root;
        }

        /// <summary>
        /// A matched pair of cylindrical dry-cell batteries lying side by side, the way a couple of loose
        /// cells sit in a drawer. Each cell has a dark plastic jacket with a bright gold band, a raised
        /// copper positive terminal at one end and a flat negative cap at the other, with printed + and −
        /// marks on top. The item's tint (a nickel grey) paints the steel casing and end caps, while the
        /// jacket keeps the classic copper-top styling so the pickup reads unmistakably as batteries.
        /// </summary>
        private static PartNode BuildBattery(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Cells lie on their sides; hover them high enough that the gentle bob never dips them through the floor.
            baseHeight = 0.15f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color steel = def.Tint;                            // nickel casing and terminals (honours the DB tint)
            Color steelHi = new Color(0.80f, 0.81f, 0.84f);   // bright rolled crimps and rims
            Color wrap = new Color(0.15f, 0.16f, 0.20f);      // dark plastic jacket
            Color wrapDeep = new Color(0.09f, 0.10f, 0.13f);  // shaded trim line
            Color band = new Color(0.84f, 0.66f, 0.26f);      // bright gold label band
            Color cap = new Color(0.82f, 0.55f, 0.24f);       // copper positive terminal
            Color capHi = new Color(0.95f, 0.74f, 0.38f);     // bright contact tip
            Color mark = new Color(0.06f, 0.06f, 0.07f);      // printed + / − polarity marks

            PartMat steelMat = PartMat.Of(steel, smoothness: 0.55f, metallic: 0.85f);
            PartMat steelHiMat = PartMat.Of(steelHi, smoothness: 0.65f, metallic: 0.90f);
            PartMat wrapMat = PartMat.Of(wrap, smoothness: 0.35f);
            PartMat wrapDeepMat = PartMat.Of(wrapDeep, smoothness: 0.30f);
            PartMat bandMat = PartMat.Of(band, smoothness: 0.45f, metallic: 0.55f);
            PartMat capMat = PartMat.Of(cap, smoothness: 0.55f, metallic: 0.80f);
            PartMat capHiMat = PartMat.Of(capHi, smoothness: 0.65f, metallic: 0.85f);
            PartMat markMat = PartMat.Of(mark, smoothness: 0.25f);

            // Two cells nestled side by side, the second flipped end-for-end so their terminals alternate.
            BuildCell(root, new Vector3(0f, 0f, 0.075f), 0f,
                wrapMat, wrapDeepMat, bandMat, steelMat, steelHiMat, capMat, capHiMat, markMat);
            BuildCell(root, new Vector3(0.006f, 0f, -0.075f), 180f,
                wrapMat, wrapDeepMat, bandMat, steelMat, steelHiMat, capMat, capHiMat, markMat);

            return root;
        }

        /// <summary>
        /// A baseball cap resting crown-up: a rounded six-panel dome, seamed with contrast top-stitching
        /// that converges at a fabric button on top, ringed by ventilation eyelets, opening onto a dark
        /// sweatband below, with a long curved peak jutting forward — its stitched rows and dark under-visor
        /// two-toning the bill — and a plastic snapback strap across the back. The cloth takes the item's
        /// tint so it reads as that cap where it lies. The crown sits on the spin pivot with the peak swept
        /// out to one side, the way the frying pan balances its bowl on the pivot with the handle out front.
        /// </summary>
        private static PartNode BuildCap(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover the crown high enough that the gentle bob never dips the drooping peak tip through the floor.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                          // cap cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f); // shaded band and button
            Color stitch = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.5f);          // contrast top-stitching
            Color sweat = new Color(0.14f, 0.14f, 0.17f);                                     // dark inner sweatband
            Color underbill = new Color(0.24f, 0.25f, 0.28f);                                 // dark under-visor / binding
            Color eyelet = new Color(0.10f, 0.10f, 0.12f);                                    // vent holes
            Color strap = new Color(0.16f, 0.17f, 0.20f);                                     // plastic snapback strap
            Color buckle = new Color(0.55f, 0.56f, 0.60f);                                    // metal slider and snaps
            Color logo = new Color(0.90f, 0.88f, 0.80f);                                      // small front emblem

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.14f);   // matte cotton twill
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.13f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat sweatMat = PartMat.Of(sweat, smoothness: 0.1f);
            PartMat underbillMat = PartMat.Of(underbill, smoothness: 0.12f);
            PartMat eyeletMat = PartMat.Of(eyelet, smoothness: 0.2f, metallic: 0.3f);
            PartMat strapMat = PartMat.Of(strap, smoothness: 0.25f);
            PartMat buckleMat = PartMat.Of(buckle, smoothness: 0.5f, metallic: 0.7f);
            PartMat logoMat = PartMat.Of(logo, smoothness: 0.15f);

            const float crownCY = 0.03f;   // vertical centre of the dome, shared by the seam and eyelet meridians

            // Rounded crown dome, centred on the spin pivot so the cap turns evenly.
            Sphere(root, "Crown", new Vector3(0f, crownCY, 0f), Vector3.zero, new Vector3(0.24f, 0.175f, 0.24f), fabricMat);

            // Lower band and the rolled binding at the very edge, closing onto a dark hollow so the opening
            // reads from below (an opaque dome can't be bored, so a recessed dark disc stands in for the inside).
            Cylinder(root, "Band", -0.045f, 0.248f, 0.05f, fabricDeepMat);
            Cylinder(root, "BandBinding", -0.068f, 0.252f, 0.012f, stitchMat);
            Cylinder(root, "Cavity", -0.062f, 0.205f, 0.014f, sweatMat);

            // Fabric-covered button (squatchee) capping the point where the panels meet.
            Cylinder(root, "Button", 0.117f, 0.034f, 0.012f, fabricDeepMat);
            Sphere(root, "ButtonTop", new Vector3(0f, 0.122f, 0f), Vector3.zero, new Vector3(0.032f, 0.02f, 0.032f), fabricDeepMat);

            // Six seams of dashed top-stitching, each in its own yaw frame so they radiate evenly and converge
            // at the button. Every dash sits on a slightly inflated meridian of the dome, tilted to the surface
            // slope, so the stitching hugs the curve rather than cutting straight through it.
            float[] seamPhi = { 18f, 40f, 62f, 84f, 106f };
            const float seamRx = 0.126f, seamRy = 0.092f;
            for (int i = 0; i < 6; i++)
            {
                var seam = new PartNode("Seam" + (i + 1));
                seam.SetParent(root, false);
                seam.localEulerAngles = new Vector3(0f, i * 60f, 0f);
                for (int k = 0; k < seamPhi.Length; k++)
                {
                    float phi = seamPhi[k] * Mathf.Deg2Rad;
                    float x = seamRx * Mathf.Sin(phi);
                    float y = crownCY + seamRy * Mathf.Cos(phi);
                    float tan = Mathf.Atan2(-seamRy * Mathf.Sin(phi), seamRx * Mathf.Cos(phi)) * Mathf.Rad2Deg;
                    Sphere(seam, "Dash" + (k + 1), new Vector3(x, y, 0f), new Vector3(0f, 0f, tan),
                        new Vector3(0.026f, 0.009f, 0.012f), stitchMat);
                }
            }

            // Ventilation eyelets: a stitched ring around a dark hole on each panel, set midway between the seams.
            const float eyeRx = 0.125f, eyeRy = 0.090f;
            float eyePhi = 66f * Mathf.Deg2Rad;
            float eyeX = eyeRx * Mathf.Sin(eyePhi);
            float eyeY = crownCY + eyeRy * Mathf.Cos(eyePhi);
            float eyeTan = Mathf.Atan2(-eyeRy * Mathf.Sin(eyePhi), eyeRx * Mathf.Cos(eyePhi)) * Mathf.Rad2Deg;
            for (int i = 0; i < 6; i++)
            {
                var vent = new PartNode("Eyelet" + (i + 1));
                vent.SetParent(root, false);
                vent.localEulerAngles = new Vector3(0f, i * 60f + 30f, 0f);
                Sphere(vent, "Ring", new Vector3(eyeX, eyeY, 0f), new Vector3(0f, 0f, eyeTan), new Vector3(0.026f, 0.006f, 0.026f), stitchMat);
                Sphere(vent, "Hole", new Vector3(eyeX + 0.002f, eyeY, 0f), new Vector3(0f, 0f, eyeTan), new Vector3(0.014f, 0.012f, 0.014f), eyeletMat);
            }

            // Small embroidered emblem on the front panel, facing out over the peak.
            Sphere(root, "Logo", new Vector3(0.121f, 0.05f, 0f), Vector3.zero, new Vector3(0.012f, 0.034f, 0.05f), logoMat);

            // Curved peak jutting forward (+X): a chain of flattened fabric segments that taper in width and
            // droop toward a rounded tip, the back segment tucking under the crown front so only the bill shows.
            Sphere(root, "Bill0", new Vector3(0.115f, -0.030f, 0f), Vector3.zero, new Vector3(0.10f, 0.030f, 0.235f), fabricMat);
            Sphere(root, "Bill1", new Vector3(0.175f, -0.037f, 0f), Vector3.zero, new Vector3(0.10f, 0.028f, 0.215f), fabricMat);
            Sphere(root, "Bill2", new Vector3(0.230f, -0.046f, 0f), Vector3.zero, new Vector3(0.10f, 0.026f, 0.175f), fabricMat);
            Sphere(root, "Bill3", new Vector3(0.280f, -0.057f, 0f), Vector3.zero, new Vector3(0.095f, 0.024f, 0.125f), fabricMat);
            Sphere(root, "BillTip", new Vector3(0.320f, -0.068f, 0f), Vector3.zero, new Vector3(0.075f, 0.022f, 0.075f), fabricMat);

            // Dark under-visor beneath the peak: each shell a touch larger and lower than the fabric above it, so a
            // thin band of it peeks round the edge as sewn binding and the whole underside reads as the darker lining.
            Sphere(root, "UnderBill0", new Vector3(0.115f, -0.038f, 0f), Vector3.zero, new Vector3(0.105f, 0.024f, 0.245f), underbillMat);
            Sphere(root, "UnderBill1", new Vector3(0.175f, -0.045f, 0f), Vector3.zero, new Vector3(0.105f, 0.023f, 0.225f), underbillMat);
            Sphere(root, "UnderBill2", new Vector3(0.230f, -0.054f, 0f), Vector3.zero, new Vector3(0.105f, 0.022f, 0.185f), underbillMat);
            Sphere(root, "UnderBill3", new Vector3(0.280f, -0.064f, 0f), Vector3.zero, new Vector3(0.10f, 0.020f, 0.135f), underbillMat);
            Sphere(root, "UnderBillTip", new Vector3(0.320f, -0.075f, 0f), Vector3.zero, new Vector3(0.08f, 0.018f, 0.085f), underbillMat);

            // Stitched seam where the peak meets the crown.
            Block(root, "BillSeam", new Vector3(0.10f, -0.014f, 0f), new Vector3(0.022f, 0.01f, 0.205f), stitchMat);

            // Rows of top-stitching running the length of the peak, the hallmark of a baseball cap's bill; each
            // row is tilted down a little to follow the peak's droop and the outer rows are kept short where it narrows.
            float[,] billRows =
            {
                // centre x, centre y, z offset, length
                { 0.205f, -0.023f, 0f, 0.165f },
                { 0.200f, -0.021f, 0.037f, 0.150f },
                { 0.200f, -0.021f, -0.037f, 0.150f },
                { 0.180f, -0.019f, 0.072f, 0.110f },
                { 0.180f, -0.019f, -0.072f, 0.110f },
            };
            for (int i = 0; i < billRows.GetLength(0); i++)
            {
                var bs = Block(root, "BillStitch" + (i + 1),
                    new Vector3(billRows[i, 0], billRows[i, 1], billRows[i, 2]),
                    new Vector3(billRows[i, 3], 0.006f, 0.010f), stitchMat);
                bs.localEulerAngles = new Vector3(0f, 0f, -10f);
            }

            // Plastic snapback across the back: a strap panel with a slider at the very rear and a row of snap studs.
            Block(root, "Strap", new Vector3(-0.118f, -0.028f, 0f), new Vector3(0.045f, 0.016f, 0.075f), strapMat);
            Block(root, "Slider", new Vector3(-0.126f, -0.028f, 0f), new Vector3(0.016f, 0.02f, 0.05f), buckleMat);
            for (int i = -1; i <= 1; i++)
                Sphere(root, "Snap" + (i + 2), new Vector3(-0.108f, -0.016f, i * 0.022f), Vector3.zero, new Vector3(0.011f, 0.009f, 0.011f), buckleMat);

            return root;
        }

        /// <summary>
        /// A knitted wool beanie sitting crown-up: a rounded dome of soft wool gathered to a knot at the top,
        /// ringed by a thick folded cuff of vertical ribbing round the bottom, with a small woven brand tab on
        /// the front. The tint colours the wool so the hat reads as that garment where it lies, and the ribbing
        /// sweeps as it slowly turns so the spin stays legible on an otherwise round shape.
        /// </summary>
        private static PartNode BuildBeanie(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the folded cuff's bottom edge through the floor.
            baseHeight = 0.17f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color wool = def.Tint;                                                       // main knit body
            Color woolDeep = new Color(wool.r * 0.78f, wool.g * 0.78f, wool.b * 0.78f);  // shaded rib valleys and cuff base
            Color woolHi = Color.Lerp(wool, new Color(0.95f, 0.95f, 0.92f), 0.20f);      // raised knit columns catching light
            Color lining = new Color(wool.r * 0.42f, wool.g * 0.42f, wool.b * 0.42f);    // dark inside, seen through the opening
            Color label = new Color(0.86f, 0.82f, 0.70f);                                // cream woven tag
            Color labelInk = new Color(wool.r * 0.5f, wool.g * 0.5f, wool.b * 0.5f);     // printing on the tag

            PartMat woolMat = PartMat.Of(wool, smoothness: 0.08f);       // matte wool
            PartMat woolDeepMat = PartMat.Of(woolDeep, smoothness: 0.07f);
            PartMat woolHiMat = PartMat.Of(woolHi, smoothness: 0.09f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.06f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);
            PartMat labelInkMat = PartMat.Of(labelInk, smoothness: 0.12f);

            const float crownCY = 0.035f;  // vertical centre of the dome, shared by the knit-column meridians

            // Straight-sided knit tube, rounded off by a squashed dome on top; together they read as the snug
            // wool cap. The dome overlaps the tube so there's no seam between them.
            Cylinder(root, "Body", -0.01f, 0.204f, 0.12f, woolMat);
            Sphere(root, "Crown", new Vector3(0f, crownCY, 0f), Vector3.zero, new Vector3(0.206f, 0.185f, 0.206f), woolMat);

            // Gathered yarn knot where the knit decreases meet at the very top, capped by a tiny cross of stitches.
            Cylinder(root, "Gather", 0.113f, 0.05f, 0.02f, woolDeepMat);
            Sphere(root, "GatherTop", new Vector3(0f, 0.126f, 0f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.05f), woolDeepMat);
            Block(root, "GatherStitchA", new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.004f, 0.006f), woolHiMat);
            Block(root, "GatherStitchB", new Vector3(0f, 0.14f, 0f), new Vector3(0.006f, 0.004f, 0.03f), woolHiMat);

            // Thick folded cuff round the bottom: a darker base band (the rib valleys), a rounded fold roll at the
            // top where the wool doubles over, and a shaded hem at the very bottom. A dark disc recessed up inside
            // stands in for the hollow the head would fill, so the opening reads from a low angle.
            Cylinder(root, "Cuff", -0.049f, 0.216f, 0.074f, woolDeepMat);
            Cylinder(root, "CuffRoll", -0.018f, 0.222f, 0.026f, woolMat);
            Cylinder(root, "CuffHem", -0.083f, 0.212f, 0.014f, woolDeepMat);
            Cylinder(root, "Lining", -0.07f, 0.185f, 0.012f, liningMat);

            // Dense vertical ribbing round the cuff — the hallmark of a knitted watch cap. Each rib is a raised
            // wool column standing proud of the darker cuff base, facing outward like the segments of a ring.
            const int cuffRibs = 32;
            const float cuffRibR = 0.110f;
            for (int i = 0; i < cuffRibs; i++)
            {
                float deg = i * 360f / cuffRibs;
                float rad = deg * Mathf.Deg2Rad;
                var rib = Block(root, "CuffRib" + i,
                    new Vector3(cuffRibR * Mathf.Cos(rad), -0.05f, cuffRibR * Mathf.Sin(rad)),
                    new Vector3(0.010f, 0.06f, 0.009f), woolMat);
                rib.localEulerAngles = new Vector3(0f, -deg, 0f);
            }

            // Softer knit columns running up the dome, each a line of raised stitches on its own meridian so they
            // radiate from the gather and sweep visibly as the hat turns. Every stitch is tilted to the dome's
            // slope and sits a hair proud of the surface, hugging the curve rather than cutting through it.
            const int domeCols = 12;
            float[] colPhi = { 22f, 44f, 66f, 86f };
            const float colRx = 0.106f, colRy = 0.096f;
            for (int i = 0; i < domeCols; i++)
            {
                var col = new PartNode("KnitCol" + (i + 1));
                col.SetParent(root, false);
                col.localEulerAngles = new Vector3(0f, i * (360f / domeCols), 0f);
                for (int k = 0; k < colPhi.Length; k++)
                {
                    float phi = colPhi[k] * Mathf.Deg2Rad;
                    float x = colRx * Mathf.Sin(phi);
                    float y = crownCY + colRy * Mathf.Cos(phi);
                    float tan = Mathf.Atan2(-colRy * Mathf.Sin(phi), colRx * Mathf.Cos(phi)) * Mathf.Rad2Deg;
                    Sphere(col, "Stitch" + (k + 1), new Vector3(x, y, 0f), new Vector3(0f, 0f, tan),
                        new Vector3(0.030f, 0.008f, 0.014f), woolHiMat);
                }
            }

            // Small woven brand tab folded over the front of the cuff, set a little off-centre so it breaks the
            // symmetry and gives the slow spin something to catch. Built in its own yawed frame so the printed
            // strip sits proud of the tag without the tag's own scale distorting it.
            var tab = new PartNode("LabelTab");
            tab.SetParent(root, false);
            tab.localPosition = new Vector3(0.110f, -0.043f, 0.02f);
            tab.localEulerAngles = new Vector3(0f, 6f, 0f);
            Block(tab, "Label", Vector3.zero, new Vector3(0.006f, 0.03f, 0.024f), labelMat);
            Block(tab, "LabelInk", new Vector3(0.004f, 0f, 0f), new Vector3(0.004f, 0.02f, 0.016f), labelInkMat);

            return root;
        }

        /// <summary>
        /// A hard-shell bicycle helmet resting crown-up: an aerodynamic teardrop shell drawn back into a point
        /// at the rear, moulded ventilation channels running front-to-back over the crown between raised ribs, a
        /// short peaked visor at the front and the retention dial at the back, with the chin straps splayed out
        /// across the floor and the dark padded interior showing round the opening beneath. The tint colours the
        /// glossy shell so the helmet reads as that garment where it lies, and the fanned straps break the
        /// symmetry so the slow spin stays legible on an otherwise rounded shape.
        /// </summary>
        private static PartNode BuildBikeHelmet(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the padded underside or the splayed straps through the floor.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color shell = def.Tint;                                                          // glossy hard outer shell
            Color shellDeep = new Color(shell.r * 0.80f, shell.g * 0.80f, shell.b * 0.80f);  // shaded tail and lower edges
            Color shellHi = Color.Lerp(shell, new Color(0.96f, 0.96f, 0.92f), 0.28f);        // raised ribs catching light
            Color vent = new Color(0.05f, 0.05f, 0.06f);                                     // dark ventilation channels
            Color foam = new Color(0.90f, 0.90f, 0.86f);                                     // pale EPS liner at the opening
            Color pad = new Color(0.11f, 0.11f, 0.13f);                                      // dark comfort padding inside
            Color strap = new Color(0.13f, 0.13f, 0.16f);                                    // nylon chin webbing
            Color buckle = new Color(0.08f, 0.08f, 0.10f);                                   // strap sliders and clasp
            Color dial = new Color(0.22f, 0.22f, 0.25f);                                     // rear retention dial
            Color dialHi = new Color(0.42f, 0.42f, 0.46f);                                   // dial knob / buckle release
            Color reflect = new Color(0.86f, 0.87f, 0.83f);                                  // small reflective rear decal

            PartMat shellMat = PartMat.Of(shell, smoothness: 0.72f);        // hard glossy polycarbonate
            PartMat shellDeepMat = PartMat.Of(shellDeep, smoothness: 0.70f);
            PartMat shellHiMat = PartMat.Of(shellHi, smoothness: 0.74f);
            PartMat ventMat = PartMat.Of(vent, smoothness: 0.25f);
            PartMat foamMat = PartMat.Of(foam, smoothness: 0.10f);
            PartMat padMat = PartMat.Of(pad, smoothness: 0.12f);
            PartMat strapMat = PartMat.Of(strap, smoothness: 0.20f);
            PartMat buckleMat = PartMat.Of(buckle, smoothness: 0.40f);
            PartMat dialMat = PartMat.Of(dial, smoothness: 0.5f, metallic: 0.2f);
            PartMat dialHiMat = PartMat.Of(dialHi, smoothness: 0.55f, metallic: 0.3f);
            PartMat reflectMat = PartMat.Of(reflect, smoothness: 0.6f, metallic: 0.2f);

            const float CY = 0.02f;                                 // vertical centre of the main shell dome
            const float ax = 0.1275f, ay = 0.0875f, az = 0.1125f;   // shell radii, shared by the vent surface sampler
            const float floorY = -0.058f;                           // height the splayed straps rest at on the floor

            // Samples the main shell's top surface so the vent channels and ribs hug the curved crown instead of
            // floating above it or sinking inside — the same trick the cap and beanie use for their stitch meridians.
            float SurfY(float x, float z)
            {
                float t = 1f - (x * x) / (ax * ax) - (z * z) / (az * az);
                return CY + ay * Mathf.Sqrt(Mathf.Max(t, 0f));
            }

            // Lays a moulded strip along the crown (front +X to back −X) as a run of short segments, each dropped
            // onto the sampled shell surface so a straight strip still follows the dome. Raised ribs sit a little
            // proud; the darker vent channels between them barely clear the surface so they read as recesses.
            void Strip(string name, float xFront, float xBack, float z, float width, float thick, float yBias, PartMat mat, int segs)
            {
                float span = Mathf.Abs(xFront - xBack);
                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 0.5f) / segs;
                    float x = Mathf.Lerp(xFront, xBack, t);
                    Block(root, name + i, new Vector3(x, SurfY(x, z) + yBias, z),
                        new Vector3(span / segs * 1.3f, thick, width), mat);
                }
            }

            // Lays one flat webbing strap on the floor between two points, yawed so its length runs from the rim
            // anchor out to a slider or the buckle, so the straps fan out around the resting helmet.
            void LayStrap(string name, float x0, float z0, float x1, float z1, float width)
            {
                Vector3 mid = new Vector3((x0 + x1) * 0.5f, floorY, (z0 + z1) * 0.5f);
                float len = Vector2.Distance(new Vector2(x0, z0), new Vector2(x1, z1));
                float yaw = Mathf.Atan2(-(z1 - z0), x1 - x0) * Mathf.Rad2Deg;
                var s = Block(root, name, mid, new Vector3(len, 0.006f, width), strapMat);
                s.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Aerodynamic teardrop shell: a broad glossy dome rounded down over the brow at the front and drawn
            // back into a tapered point at the rear, a little longer front-to-back than it is wide.
            Sphere(root, "Shell", new Vector3(0f, CY, 0f), Vector3.zero, new Vector3(2f * ax, 2f * ay, 2f * az), shellMat);
            Sphere(root, "Brow", new Vector3(0.075f, CY - 0.012f, 0f), Vector3.zero, new Vector3(0.16f, 0.15f, 0.205f), shellMat);
            Sphere(root, "Tail", new Vector3(-0.095f, CY - 0.020f, 0f), Vector3.zero, new Vector3(0.15f, 0.125f, 0.165f), shellMat);
            Sphere(root, "TailTip", new Vector3(-0.150f, CY - 0.036f, 0f), Vector3.zero, new Vector3(0.09f, 0.085f, 0.10f), shellDeepMat);

            // Short peaked visor jutting from the brow: a couple of flattened shell segments drooping forward to
            // a rounded tip, marking the front and giving the helmet a mountain-bike silhouette.
            Sphere(root, "Visor0", new Vector3(0.150f, CY - 0.030f, 0f), Vector3.zero, new Vector3(0.07f, 0.022f, 0.185f), shellMat);
            Sphere(root, "Visor1", new Vector3(0.188f, CY - 0.044f, 0f), Vector3.zero, new Vector3(0.06f, 0.020f, 0.150f), shellMat);
            Sphere(root, "VisorTip", new Vector3(0.216f, CY - 0.055f, 0f), Vector3.zero, new Vector3(0.05f, 0.018f, 0.105f), shellDeepMat);

            // Underside reads as the hollow, padded inside rather than a solid ball: a dark opening across the
            // base with a pale foam liner ring peeking above it and a foam brow pad at the front lip.
            Sphere(root, "Underside", new Vector3(0f, -0.055f, 0f), Vector3.zero, new Vector3(0.185f, 0.05f, 0.165f), padMat);
            Sphere(root, "Liner", new Vector3(0f, -0.046f, 0f), Vector3.zero, new Vector3(0.165f, 0.05f, 0.145f), foamMat);
            Block(root, "BrowPad", new Vector3(0.088f, -0.016f, 0f), new Vector3(0.05f, 0.02f, 0.145f), foamMat);

            // Raised central ridge along the crown, flanked by moulded vent channels and slim ribs stepping out
            // to each side — the aerodynamic scoops that mark it out as a cycling helmet. Every run hugs the dome.
            Strip("Ridge", 0.060f, -0.095f, 0f, 0.030f, 0.012f, 0.006f, shellHiMat, 4);

            Strip("VentInFL", 0.055f, 0.000f, 0.048f, 0.030f, 0.012f, -0.001f, ventMat, 3);
            Strip("VentInFR", 0.055f, 0.000f, -0.048f, 0.030f, 0.012f, -0.001f, ventMat, 3);
            Strip("VentInBL", -0.020f, -0.085f, 0.048f, 0.028f, 0.012f, -0.001f, ventMat, 3);
            Strip("VentInBR", -0.020f, -0.085f, -0.048f, 0.028f, 0.012f, -0.001f, ventMat, 3);

            Strip("RibMidL", 0.045f, -0.075f, 0.070f, 0.016f, 0.012f, 0.006f, shellHiMat, 3);
            Strip("RibMidR", 0.045f, -0.075f, -0.070f, 0.016f, 0.012f, 0.006f, shellHiMat, 3);

            Strip("VentOutL", 0.035f, -0.060f, 0.092f, 0.024f, 0.011f, -0.001f, ventMat, 3);
            Strip("VentOutR", 0.035f, -0.060f, -0.092f, 0.024f, 0.011f, -0.001f, ventMat, 3);

            // Dark intake scoops sitting on the sloped brow, angled to follow it, above where the forehead vents would be.
            Sphere(root, "BrowVentC", new Vector3(0.125f, 0.058f, 0f), new Vector3(0f, 0f, -18f), new Vector3(0.055f, 0.02f, 0.032f), ventMat);
            Sphere(root, "BrowVentL", new Vector3(0.120f, 0.060f, 0.048f), new Vector3(0f, 0f, -18f), new Vector3(0.06f, 0.02f, 0.03f), ventMat);
            Sphere(root, "BrowVentR", new Vector3(0.120f, 0.060f, -0.048f), new Vector3(0f, 0f, -18f), new Vector3(0.06f, 0.02f, 0.03f), ventMat);

            // Reflective decal on the tail and the occipital retention cradle with its adjuster dial facing back.
            Sphere(root, "Reflector", new Vector3(-0.135f, 0.050f, 0f), Vector3.zero, new Vector3(0.05f, 0.012f, 0.06f), reflectMat);
            Sphere(root, "DialHousing", new Vector3(-0.152f, -0.030f, 0f), Vector3.zero, new Vector3(0.05f, 0.055f, 0.075f), dialMat);
            Rod(root, "Dial", new Vector3(-0.170f, -0.030f, 0f), new Vector3(0f, 0f, 90f), 0.042f, 0.018f, dialMat);
            Rod(root, "DialKnob", new Vector3(-0.180f, -0.030f, 0f), new Vector3(0f, 0f, 90f), 0.026f, 0.014f, dialHiMat);

            // Chin straps splayed out across the floor: each side runs from front and rear anchors under the rim
            // down to a slider level with the ear, then a single strap forward to the chin buckle where they clasp.
            LayStrap("StrapLFront", 0.075f, 0.070f, 0.005f, 0.150f, 0.017f);
            LayStrap("StrapLRear", -0.095f, 0.055f, 0.005f, 0.150f, 0.015f);
            LayStrap("StrapLChin", 0.005f, 0.150f, 0.205f, 0.020f, 0.017f);
            LayStrap("StrapRFront", 0.075f, -0.070f, 0.005f, -0.150f, 0.017f);
            LayStrap("StrapRRear", -0.095f, -0.055f, 0.005f, -0.150f, 0.015f);
            LayStrap("StrapRChin", 0.005f, -0.150f, 0.205f, -0.020f, 0.017f);

            Block(root, "SliderL", new Vector3(0.005f, floorY, 0.150f), new Vector3(0.022f, 0.012f, 0.03f), buckleMat);
            Block(root, "SliderR", new Vector3(0.005f, floorY, -0.150f), new Vector3(0.022f, 0.012f, 0.03f), buckleMat);
            Block(root, "Buckle", new Vector3(0.205f, floorY, 0f), new Vector3(0.04f, 0.016f, 0.055f), buckleMat);
            Block(root, "BuckleRelease", new Vector3(0.222f, floorY + 0.004f, 0f), new Vector3(0.02f, 0.012f, 0.03f), dialHiMat);

            return root;
        }

        /// <summary>
        /// A cotton t-shirt laid out flat on the ground, seen from above: a soft torso panel that widens across
        /// the shoulders and tapers to a hemmed bottom, two short sleeves splayed out to each side ending in
        /// ribbed cuffs, and a ribbed crew neckband ringing a dark neck hole so the opening reads as a real
        /// collar. A few raised folds and shaded creases keep the jersey looking relaxed rather than moulded, and
        /// a small off-centre chest print breaks the left–right symmetry so the slow spin stays legible. The tint
        /// colours the fabric so the tee reads as that garment where it lies.
        /// </summary>
        private static PartNode BuildTshirt(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                            // main cotton jersey
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded creases / underside
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised folds catching light
            Color rib = Color.Lerp(fabric, new Color(0.90f, 0.90f, 0.88f), 0.12f);              // ribbed collar / cuff / hem bands
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);            // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);     // dark inside, seen through the neck
            Color print = new Color(0.88f, 0.86f, 0.78f);                                       // cream chest screen-print
            Color printInk = new Color(fabric.r * 0.5f, fabric.g * 0.5f, fabric.b * 0.5f);      // motif inside the print

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.10f);   // matte cotton
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.09f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.12f);
            PartMat ribMat = PartMat.Of(rib, smoothness: 0.11f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.10f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat printMat = PartMat.Of(print, smoothness: 0.14f);
            PartMat printInkMat = PartMat.Of(printInk, smoothness: 0.13f);

            // Soft torso laid flat: overlapping flattened mounds so the jersey reads as cloth, not a board — widest
            // across the shoulders (+X, where the collar and sleeves sit) and tapering to the hem at the far end (−X).
            Sphere(root, "Torso", new Vector3(-0.01f, 0f, 0f), Vector3.zero, new Vector3(0.32f, 0.05f, 0.25f), fabricMat);
            Sphere(root, "Shoulders", new Vector3(0.10f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.05f, 0.29f), fabricMat);
            Sphere(root, "HemMound", new Vector3(-0.165f, -0.002f, 0f), Vector3.zero, new Vector3(0.12f, 0.046f, 0.225f), fabricMat);

            // Short sleeves splayed out to each side from the shoulder line, each a soft cap ending in a ribbed cuff
            // with its own hem stitch, and an angled armhole seam where it meets the body.
            Sphere(root, "SleeveL", new Vector3(0.075f, 0f, 0.195f), Vector3.zero, new Vector3(0.15f, 0.05f, 0.17f), fabricMat);
            Sphere(root, "SleeveR", new Vector3(0.075f, 0f, -0.195f), Vector3.zero, new Vector3(0.15f, 0.05f, 0.17f), fabricMat);
            Block(root, "CuffL", new Vector3(0.075f, 0.001f, 0.278f), new Vector3(0.155f, 0.052f, 0.020f), ribMat);
            Block(root, "CuffR", new Vector3(0.075f, 0.001f, -0.278f), new Vector3(0.155f, 0.052f, 0.020f), ribMat);
            Block(root, "CuffStitchL", new Vector3(0.075f, 0.030f, 0.267f), new Vector3(0.140f, 0.006f, 0.006f), stitchMat);
            Block(root, "CuffStitchR", new Vector3(0.075f, 0.030f, -0.267f), new Vector3(0.140f, 0.006f, 0.006f), stitchMat);
            var armSeamL = Block(root, "ArmSeamL", new Vector3(0.072f, 0.028f, 0.118f), new Vector3(0.140f, 0.006f, 0.012f), stitchMat);
            armSeamL.localEulerAngles = new Vector3(0f, 32f, 0f);
            var armSeamR = Block(root, "ArmSeamR", new Vector3(0.072f, 0.028f, -0.118f), new Vector3(0.140f, 0.006f, 0.012f), stitchMat);
            armSeamR.localEulerAngles = new Vector3(0f, -32f, 0f);

            // Dark neck hole ringed by a ribbed crew neckband: short radial ribs set round an oval so the collar
            // reads from above, with the recessed lining standing in for the hollow the head would fill.
            Sphere(root, "Neckline", new Vector3(0.128f, 0.010f, 0f), Vector3.zero, new Vector3(0.10f, 0.035f, 0.15f), liningMat);
            const int collarRibs = 18;
            const float nCx = 0.128f, nrx = 0.055f, nrz = 0.088f;
            for (int i = 0; i < collarRibs; i++)
            {
                float deg = i * 360f / collarRibs;
                float rad = deg * Mathf.Deg2Rad;
                var collarRib = Block(root, "Collar" + i,
                    new Vector3(nCx + nrx * Mathf.Cos(rad), 0.020f, nrz * Mathf.Sin(rad)),
                    new Vector3(0.024f, 0.028f, 0.010f), ribMat);
                collarRib.localEulerAngles = new Vector3(0f, -deg, 0f);   // lay each rib's length across the band, radially
            }

            // Bottom hem band across the far end with a double-needle stitch above it; side seams run up each flank.
            Block(root, "HemBand", new Vector3(-0.215f, 0.004f, 0f), new Vector3(0.022f, 0.050f, 0.235f), ribMat);
            Block(root, "HemStitch", new Vector3(-0.205f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.215f), stitchMat);
            Block(root, "SideSeamL", new Vector3(-0.04f, 0.027f, 0.126f), new Vector3(0.300f, 0.006f, 0.008f), stitchMat);
            Block(root, "SideSeamR", new Vector3(-0.04f, 0.027f, -0.126f), new Vector3(0.300f, 0.006f, 0.008f), stitchMat);

            // A few soft folds and shaded creases so the cotton looks relaxed, set off-centre to break the symmetry.
            Sphere(root, "FoldA", new Vector3(-0.02f, 0.030f, 0.030f), new Vector3(0f, 24f, 0f), new Vector3(0.24f, 0.020f, 0.05f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(-0.10f, 0.028f, -0.050f), new Vector3(0f, -14f, 0f), new Vector3(0.17f, 0.018f, 0.045f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.06f, 0.022f, 0.000f), new Vector3(0f, 8f, 0f), new Vector3(0.22f, 0.014f, 0.03f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.02f, 0.024f, -0.080f), new Vector3(0f, -20f, 0f), new Vector3(0.12f, 0.012f, 0.028f), fabricDeepMat);

            // Small screen print on the chest, in its own yawed frame and set off to one side so the slow spin
            // always has something to catch.
            var printFrame = new PartNode("Print");
            printFrame.SetParent(root, false);
            printFrame.localPosition = new Vector3(0.055f, 0.032f, 0.050f);
            printFrame.localEulerAngles = new Vector3(0f, 10f, 0f);
            Block(printFrame, "PrintBase", Vector3.zero, new Vector3(0.080f, 0.006f, 0.060f), printMat);
            Block(printFrame, "PrintInk", new Vector3(0f, 0.004f, 0f), new Vector3(0.045f, 0.006f, 0.034f), printInkMat);

            return root;
        }

        /// <summary>
        /// A pullover hoodie laid flat, face-up: a soft heavy-fleece body with the hood folded down above the
        /// shoulders, long sleeves ending in ribbed cuffs, a ribbed waistband, a kangaroo pouch pocket across the
        /// front and two drawcords trailing from the hood's front eyelets. Takes the item's tint as the fleece
        /// colour so it reads as that hoodie where it lies; the folded hood, pocket and drawstrings set it apart
        /// from the tee.
        /// </summary>
        private static PartNode BuildHoodie(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // heavy cotton fleece
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);   // shaded creases / underside
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);           // raised folds catching light
            Color rib = Color.Lerp(fabric, new Color(0.90f, 0.90f, 0.88f), 0.12f);               // ribbed cuffs / waistband
            Color ribDeep = new Color(fabric.r * 0.70f, fabric.g * 0.70f, fabric.b * 0.70f);      // grooves between the ribs
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);       // dark inside the hood / neck
            Color cord = new Color(0.90f, 0.89f, 0.83f);                                          // cream drawstring cords
            Color aglet = new Color(0.20f, 0.20f, 0.22f);                                         // dark cord tips / eyelets

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.09f);   // matte fleece
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.08f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.11f);
            PartMat ribMat = PartMat.Of(rib, smoothness: 0.10f);
            PartMat ribDeepMat = PartMat.Of(ribDeep, smoothness: 0.09f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.10f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat cordMat = PartMat.Of(cord, smoothness: 0.12f);
            PartMat agletMat = PartMat.Of(aglet, smoothness: 0.30f);

            // Soft heavy body laid flat: overlapping flattened mounds, widest across the shoulders (+X, where the
            // hood and sleeves sit) and tapering to the ribbed waistband at the far end (−X). A touch thicker in Y
            // than the tee so the fleece reads as a heavier garment.
            Sphere(root, "Torso", new Vector3(-0.02f, 0f, 0f), Vector3.zero, new Vector3(0.32f, 0.055f, 0.26f), fabricMat);
            Sphere(root, "Shoulders", new Vector3(0.10f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.058f, 0.30f), fabricMat);
            Sphere(root, "Chest", new Vector3(0.02f, 0.012f, 0f), Vector3.zero, new Vector3(0.22f, 0.05f, 0.22f), fabricMat);
            Sphere(root, "WaistMound", new Vector3(-0.17f, -0.002f, 0f), Vector3.zero, new Vector3(0.12f, 0.05f, 0.235f), fabricMat);

            // ── Hood, folded down above the shoulders ──────────────────────────────────────────
            // Puffy outer shell with a highlighted fold on top and shaded folds bunching at each side; a dark
            // lining stands in for the hollow inside, and a rolled casing curves round the front opening.
            Sphere(root, "HoodShell", new Vector3(0.25f, 0.012f, 0f), Vector3.zero, new Vector3(0.20f, 0.088f, 0.30f), fabricMat);
            Sphere(root, "HoodFold", new Vector3(0.285f, 0.05f, 0f), Vector3.zero, new Vector3(0.13f, 0.05f, 0.24f), fabricHiMat);
            Sphere(root, "HoodBunchL", new Vector3(0.17f, 0.02f, 0.11f), new Vector3(0f, -22f, 0f), new Vector3(0.13f, 0.03f, 0.10f), fabricDeepMat);
            Sphere(root, "HoodBunchR", new Vector3(0.17f, 0.02f, -0.11f), new Vector3(0f, 22f, 0f), new Vector3(0.13f, 0.03f, 0.10f), fabricDeepMat);
            // Dark opening (the hood mouth) sitting between the hood and the collar.
            Sphere(root, "HoodMouth", new Vector3(0.135f, 0.03f, 0f), Vector3.zero, new Vector3(0.10f, 0.05f, 0.22f), liningMat);
            Sphere(root, "HoodMouthDeep", new Vector3(0.15f, 0.018f, 0f), Vector3.zero, new Vector3(0.075f, 0.04f, 0.17f), liningMat);

            // Rolled casing round the front edge of the hood opening: a shallow arc of fat cord-channel segments
            // bulging toward the neck (−X) at the middle. Fatter than a seam so it reads as the drawstring channel.
            const int casingSegs = 7;
            for (int i = 0; i < casingSegs; i++)
            {
                float t = i / (float)(casingSegs - 1);
                float z = Mathf.Lerp(-0.15f, 0.15f, t);
                float x = 0.135f - 0.05f * Mathf.Sin(t * Mathf.PI);          // curve toward the neck at the middle
                float dxdt = -0.05f * Mathf.PI * Mathf.Cos(t * Mathf.PI);    // arc tangent, for laying each segment
                const float dzdt = 0.30f;
                float heading = Mathf.Atan2(dzdt, dxdt) * Mathf.Rad2Deg;
                Rod(root, "Casing" + i, new Vector3(x, 0.04f, z), new Vector3(90f, 90f - heading, 0f), 0.020f, 0.058f, fabricHiMat);
            }

            // ── Long sleeves, splayed out and angled back toward the hem, each with a ribbed cuff ─────────────
            // (Left = +Z, Right = −Z.) Two mounds per sleeve so they drape, a highlight fold along the top, a ribbed
            // cuff band with a groove at the wrist, and an armhole seam where each sleeve meets the body.
            Sphere(root, "SleeveL", new Vector3(0.05f, 0f, 0.205f), new Vector3(0f, -16f, 0f), new Vector3(0.17f, 0.055f, 0.24f), fabricMat);
            Sphere(root, "SleeveLFore", new Vector3(-0.03f, 0f, 0.30f), new Vector3(0f, -8f, 0f), new Vector3(0.14f, 0.05f, 0.15f), fabricMat);
            Sphere(root, "SleeveLFold", new Vector3(0.03f, 0.032f, 0.23f), new Vector3(0f, -16f, 0f), new Vector3(0.11f, 0.02f, 0.15f), fabricHiMat);
            var cuffL = Block(root, "CuffL", new Vector3(-0.05f, 0.001f, 0.35f), new Vector3(0.075f, 0.056f, 0.028f), ribMat);
            cuffL.localEulerAngles = new Vector3(0f, -8f, 0f);
            var cuffLGroove = Block(root, "CuffLGroove", new Vector3(-0.05f, 0.031f, 0.35f), new Vector3(0.075f, 0.006f, 0.006f), ribDeepMat);
            cuffLGroove.localEulerAngles = new Vector3(0f, -8f, 0f);
            var armSeamL = Block(root, "ArmSeamL", new Vector3(0.055f, 0.03f, 0.10f), new Vector3(0.145f, 0.006f, 0.012f), stitchMat);
            armSeamL.localEulerAngles = new Vector3(0f, 34f, 0f);

            Sphere(root, "SleeveR", new Vector3(0.05f, 0f, -0.205f), new Vector3(0f, 16f, 0f), new Vector3(0.17f, 0.055f, 0.24f), fabricMat);
            Sphere(root, "SleeveRFore", new Vector3(-0.03f, 0f, -0.30f), new Vector3(0f, 8f, 0f), new Vector3(0.14f, 0.05f, 0.15f), fabricMat);
            Sphere(root, "SleeveRFold", new Vector3(0.03f, 0.032f, -0.23f), new Vector3(0f, 16f, 0f), new Vector3(0.11f, 0.02f, 0.15f), fabricHiMat);
            var cuffR = Block(root, "CuffR", new Vector3(-0.05f, 0.001f, -0.35f), new Vector3(0.075f, 0.056f, 0.028f), ribMat);
            cuffR.localEulerAngles = new Vector3(0f, 8f, 0f);
            var cuffRGroove = Block(root, "CuffRGroove", new Vector3(-0.05f, 0.031f, -0.35f), new Vector3(0.075f, 0.006f, 0.006f), ribDeepMat);
            cuffRGroove.localEulerAngles = new Vector3(0f, 8f, 0f);
            var armSeamR = Block(root, "ArmSeamR", new Vector3(0.055f, 0.03f, -0.10f), new Vector3(0.145f, 0.006f, 0.012f), stitchMat);
            armSeamR.localEulerAngles = new Vector3(0f, -34f, 0f);

            // ── Kangaroo pouch pocket across the front, topstitched, with a slanted hand opening each side ─────
            Block(root, "Pocket", new Vector3(-0.085f, 0.03f, 0f), new Vector3(0.15f, 0.016f, 0.20f), fabricMat);
            Block(root, "PocketHemStitch", new Vector3(-0.16f, 0.041f, 0f), new Vector3(0.006f, 0.006f, 0.20f), stitchMat);
            Block(root, "PocketSideL", new Vector3(-0.085f, 0.041f, 0.098f), new Vector3(0.15f, 0.006f, 0.006f), stitchMat);
            Block(root, "PocketSideR", new Vector3(-0.085f, 0.041f, -0.098f), new Vector3(0.15f, 0.006f, 0.006f), stitchMat);
            var pocketSlitL = Block(root, "PocketSlitL", new Vector3(-0.02f, 0.042f, 0.072f), new Vector3(0.07f, 0.008f, 0.014f), liningMat);
            pocketSlitL.localEulerAngles = new Vector3(0f, 34f, 0f);
            var pocketSlitR = Block(root, "PocketSlitR", new Vector3(-0.02f, 0.042f, -0.072f), new Vector3(0.07f, 0.008f, 0.014f), liningMat);
            pocketSlitR.localEulerAngles = new Vector3(0f, -34f, 0f);

            // ── Ribbed waistband across the hem, corrugated with alternating raised ribs ──────────────────
            Block(root, "Waistband", new Vector3(-0.225f, 0.004f, 0f), new Vector3(0.030f, 0.058f, 0.235f), ribMat);
            const int waistRibs = 15;
            for (int i = 0; i < waistRibs; i++)
            {
                float z = Mathf.Lerp(-0.108f, 0.108f, i / (float)(waistRibs - 1));
                Block(root, "WaistRib" + i, new Vector3(-0.225f, 0.034f, z), new Vector3(0.028f, 0.006f, 0.008f),
                    (i % 2 == 0) ? ribDeepMat : fabricHiMat);
            }

            // ── Front placket seam below the hood, with two eyelets the drawcords exit ────────────────────
            Block(root, "Placket", new Vector3(0.08f, 0.03f, 0f), new Vector3(0.11f, 0.006f, 0.006f), stitchMat);
            Rod(root, "EyeletL", new Vector3(0.085f, 0.04f, 0.024f), Vector3.zero, 0.018f, 0.012f, agletMat);
            Rod(root, "EyeletHoleL", new Vector3(0.085f, 0.047f, 0.024f), Vector3.zero, 0.008f, 0.008f, liningMat);
            Rod(root, "EyeletR", new Vector3(0.085f, 0.04f, -0.024f), Vector3.zero, 0.018f, 0.012f, agletMat);
            Rod(root, "EyeletHoleR", new Vector3(0.085f, 0.047f, -0.024f), Vector3.zero, 0.008f, 0.008f, liningMat);

            // Two drawcords trailing from the eyelets down over the chest, each a knot at the eyelet, a couple of
            // segments laid along a gentle curve and a dark aglet at the tip. (Rod euler (90, 90−heading, 0) lays a
            // segment flat along a heading measured from +X toward +Z.)
            Sphere(root, "KnotL", new Vector3(0.085f, 0.05f, 0.024f), Vector3.zero, new Vector3(0.022f, 0.016f, 0.022f), cordMat);
            Rod(root, "CordL1", new Vector3(0.026f, 0.05f, 0.034f), new Vector3(90f, -80f, 0f), 0.010f, 0.12f, cordMat);
            Rod(root, "CordL2", new Vector3(-0.075f, 0.05f, 0.039f), new Vector3(90f, -98f, 0f), 0.010f, 0.085f, cordMat);
            Rod(root, "AgletL", new Vector3(-0.129f, 0.05f, 0.031f), new Vector3(90f, -98f, 0f), 0.014f, 0.024f, agletMat);

            Sphere(root, "KnotR", new Vector3(0.085f, 0.05f, -0.024f), Vector3.zero, new Vector3(0.022f, 0.016f, 0.022f), cordMat);
            Rod(root, "CordR1", new Vector3(0.026f, 0.05f, -0.034f), new Vector3(90f, 260f, 0f), 0.010f, 0.12f, cordMat);
            Rod(root, "CordR2", new Vector3(-0.075f, 0.05f, -0.039f), new Vector3(90f, 278f, 0f), 0.010f, 0.085f, cordMat);
            Rod(root, "AgletR", new Vector3(-0.129f, 0.05f, -0.031f), new Vector3(90f, 278f, 0f), 0.014f, 0.024f, agletMat);

            // A few soft folds and shaded creases so the fleece looks relaxed, set off-centre to break the symmetry.
            Sphere(root, "FoldA", new Vector3(-0.03f, 0.032f, 0.03f), new Vector3(0f, 22f, 0f), new Vector3(0.22f, 0.02f, 0.05f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.10f, 0.024f, -0.04f), new Vector3(0f, -16f, 0f), new Vector3(0.16f, 0.014f, 0.03f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.0f, 0.026f, 0.06f), new Vector3(0f, 12f, 0f), new Vector3(0.14f, 0.014f, 0.03f), fabricDeepMat);

            return root;
        }

        /// <summary>
        /// A padded (puffer) jacket laid flat, face-up: a quilted nylon shell with rows of fat baffles down each
        /// side of a full-length front zip, a padded stand collar wrapping the neck, long quilted sleeves ending
        /// in elastic cuffs, two flap hand-pockets low on the front and a ribbed elastic hem. Takes the item's
        /// tint as the shell colour so it reads as that jacket where it lies; the horizontal quilting, the central
        /// metal zipper and the flap pockets set it apart from the tee and the hoodie.
        /// </summary>
        private static PartNode BuildJacket(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                              // padded nylon shell
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);    // shaded quilt valleys / underside
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.22f);            // baffle crowns catching light
            Color rib = Color.Lerp(fabric, new Color(0.90f, 0.90f, 0.88f), 0.12f);                // elastic cuffs / hem / brand tab
            Color ribDeep = new Color(fabric.r * 0.70f, fabric.g * 0.70f, fabric.b * 0.70f);        // grooves between the ribs
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);              // quilting topstitch
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);         // dark neck hole / pocket mouths
            Color zip = new Color(0.75f, 0.76f, 0.79f);                                            // zipper coil / slider / snaps
            Color zipHi = new Color(0.88f, 0.89f, 0.92f);                                          // bright zip highlights
            Color zipTape = new Color(fabric.r * 0.55f, fabric.g * 0.55f, fabric.b * 0.55f);        // zip tape flanking the coil

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.24f);   // faint nylon sheen (glossier than cotton/fleece)
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.22f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.27f);
            PartMat ribMat = PartMat.Of(rib, smoothness: 0.14f);
            PartMat ribDeepMat = PartMat.Of(ribDeep, smoothness: 0.12f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.16f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat zipMat = PartMat.Of(zip, smoothness: 0.55f, metallic: 0.85f);
            PartMat zipHiMat = PartMat.Of(zipHi, smoothness: 0.65f, metallic: 0.85f);
            PartMat zipTapeMat = PartMat.Of(zipTape, smoothness: 0.18f);

            // Soft padded body laid flat: overlapping flattened mounds, widest across the shoulders (+X, where the
            // collar and sleeves sit) and tapering to the ribbed hem at the far end (−X). Thicker in Y than the tee
            // or hoodie so the down-filled shell reads as a bulky winter garment.
            Sphere(root, "Torso", new Vector3(-0.03f, 0f, 0f), Vector3.zero, new Vector3(0.33f, 0.062f, 0.27f), fabricMat);
            Sphere(root, "Shoulders", new Vector3(0.085f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.064f, 0.31f), fabricMat);
            Sphere(root, "Chest", new Vector3(0.02f, 0.012f, 0f), Vector3.zero, new Vector3(0.24f, 0.056f, 0.23f), fabricMat);
            Sphere(root, "HemMound", new Vector3(-0.185f, -0.002f, 0f), Vector3.zero, new Vector3(0.12f, 0.056f, 0.24f), fabricMat);

            // ── Quilted padding: rows of fat baffles running across the body, split left and right of the central
            // zip channel, with a shaded valley and topstitch line between each row. This horizontal quilting is the
            // padded jacket's signature. (Rod euler (90,0,0) lays each baffle's length along Z, across the body.)
            float[] baffleX = { 0.055f, 0.008f, -0.040f, -0.088f, -0.136f, -0.180f };
            float[] baffleHalfZ = { 0.150f, 0.160f, 0.154f, 0.144f, 0.132f, 0.118f };
            const float zipHalf = 0.024f;    // half-width of the central zip channel the baffles stop short of
            const float puff = 0.052f;       // baffle diameter (the padded thickness)
            const float baffleY = 0.034f;    // proud of the base mounds
            for (int i = 0; i < baffleX.Length; i++)
            {
                float len = baffleHalfZ[i] - zipHalf;                 // each side's baffle length
                float cz = zipHalf + len * 0.5f;                      // centre of each side baffle
                PartMat bm = (i % 2 == 0) ? fabricMat : fabricHiMat; // alternate shading so the rows read distinctly
                Rod(root, "BaffleL" + i, new Vector3(baffleX[i], baffleY, cz), new Vector3(90f, 0f, 0f), puff, len, bm);
                Rod(root, "BaffleR" + i, new Vector3(baffleX[i], baffleY, -cz), new Vector3(90f, 0f, 0f), puff, len, bm);
            }
            // Shaded valleys with a recessed topstitch line where consecutive baffle rows meet (both split at the zip).
            for (int i = 0; i < baffleX.Length - 1; i++)
            {
                float vx = (baffleX[i] + baffleX[i + 1]) * 0.5f;
                float halfZ = Mathf.Min(baffleHalfZ[i], baffleHalfZ[i + 1]);
                float vlen = halfZ - zipHalf;
                float vcz = zipHalf + vlen * 0.5f;
                Block(root, "ValleyL" + i, new Vector3(vx, 0.028f, vcz), new Vector3(0.014f, 0.028f, vlen), fabricDeepMat);
                Block(root, "ValleyR" + i, new Vector3(vx, 0.028f, -vcz), new Vector3(0.014f, 0.028f, vlen), fabricDeepMat);
                Block(root, "QuiltStitchL" + i, new Vector3(vx, 0.045f, vcz), new Vector3(0.006f, 0.006f, vlen), stitchMat);
                Block(root, "QuiltStitchR" + i, new Vector3(vx, 0.045f, -vcz), new Vector3(0.006f, 0.006f, vlen), stitchMat);
            }

            // ── Full-length front zip down the centre ────────────────────────────────────────────────────────
            // A raised placket carries the zip from the collar (+X) down to the hem (−X): a fabric storm flap each
            // side of a dark tape, a bright metal coil of fine teeth, a slider partway down with a hanging pull tab,
            // and a zip garage where the coil ends under the collar. This central closure is the jacket's other tell.
            const float zipTop = 0.10f, zipBot = -0.198f;
            float zipLen = zipTop - zipBot;
            float zipMid = (zipTop + zipBot) * 0.5f;
            Block(root, "PlacketL", new Vector3(zipMid, 0.050f, 0.020f), new Vector3(zipLen, 0.048f, 0.026f), fabricMat);
            Block(root, "PlacketR", new Vector3(zipMid, 0.050f, -0.020f), new Vector3(zipLen, 0.048f, 0.026f), fabricMat);
            Block(root, "ZipTape", new Vector3(zipMid, 0.058f, 0f), new Vector3(zipLen, 0.020f, 0.024f), zipTapeMat);
            // Fine metal coil: a run of tiny teeth alternating a hair to each side so they read as interlocking.
            const int teeth = 24;
            for (int i = 0; i < teeth; i++)
            {
                float tx = Mathf.Lerp(zipBot + 0.012f, zipTop - 0.012f, i / (float)(teeth - 1));
                float tz = (i % 2 == 0) ? 0.004f : -0.004f;
                Block(root, "Tooth" + i, new Vector3(tx, 0.066f, tz), new Vector3(0.009f, 0.006f, 0.014f),
                    (i % 2 == 0) ? zipMat : zipHiMat);
            }
            // Slider body partway down with a bright top, then a bail, pull tab and end drop hanging toward the hem.
            Block(root, "Slider", new Vector3(-0.045f, 0.068f, 0f), new Vector3(0.032f, 0.014f, 0.030f), zipMat);
            Block(root, "SliderTop", new Vector3(-0.045f, 0.077f, 0f), new Vector3(0.024f, 0.006f, 0.022f), zipHiMat);
            Rod(root, "PullBail", new Vector3(-0.063f, 0.070f, 0f), new Vector3(90f, 0f, 0f), 0.012f, 0.010f, zipMat);
            Block(root, "PullTab", new Vector3(-0.083f, 0.070f, 0f), new Vector3(0.028f, 0.010f, 0.014f), zipMat);
            Block(root, "PullTabEnd", new Vector3(-0.101f, 0.070f, 0f), new Vector3(0.010f, 0.008f, 0.020f), zipHiMat);
            Block(root, "ZipGarage", new Vector3(0.098f, 0.060f, 0f), new Vector3(0.020f, 0.040f, 0.050f), fabricHiMat);

            // ── Padded stand collar wrapping the neck ───────────────────────────────────────────────────────
            // A dark neck hole with a horseshoe of fat padded segments round its back and sides (open at the front,
            // where the zip runs up to meet it), a brighter crown roll on top and a topstitch along the seam.
            Sphere(root, "NeckHole", new Vector3(0.092f, 0.020f, 0f), Vector3.zero, new Vector3(0.075f, 0.045f, 0.15f), liningMat);
            const int collarSegs = 7;
            const float ncx = 0.088f, collarR = 0.078f;
            for (int i = 0; i < collarSegs; i++)
            {
                float th = Mathf.Lerp(82f, -82f, i / (float)(collarSegs - 1)) * Mathf.Deg2Rad;
                float x = ncx + collarR * Mathf.Cos(th);
                float z = collarR * Mathf.Sin(th);
                float heading = Mathf.Atan2(Mathf.Cos(th), -Mathf.Sin(th)) * Mathf.Rad2Deg;  // arc tangent
                Rod(root, "Collar" + i, new Vector3(x, 0.044f, z), new Vector3(90f, 90f - heading, 0f), 0.056f, 0.064f, fabricMat);
                Rod(root, "CollarTop" + i, new Vector3(x, 0.060f, z), new Vector3(90f, 90f - heading, 0f), 0.032f, 0.060f, fabricHiMat);
                Rod(root, "CollarStitch" + i, new Vector3(x, 0.062f, z), new Vector3(90f, 90f - heading, 0f), 0.006f, 0.050f, stitchMat);
            }

            // ── Long sleeves, splayed out and angled back toward the hem, quilted like the body and ending in an
            // elastic cuff. (Left = +Z, Right = −Z.) Two mounds per sleeve so they drape, a couple of cross-stitched
            // quilt seams, a ribbed cuff band with a groove at the wrist, and an armhole seam where each meets the body.
            Sphere(root, "SleeveL", new Vector3(0.03f, 0f, 0.215f), new Vector3(0f, -18f, 0f), new Vector3(0.18f, 0.060f, 0.24f), fabricMat);
            Sphere(root, "SleeveLFore", new Vector3(-0.06f, 0f, 0.305f), new Vector3(0f, -10f, 0f), new Vector3(0.14f, 0.054f, 0.16f), fabricMat);
            Sphere(root, "SleeveLFold", new Vector3(0.0f, 0.034f, 0.245f), new Vector3(0f, -14f, 0f), new Vector3(0.11f, 0.02f, 0.15f), fabricHiMat);
            var slStitch1 = Block(root, "SleeveLStitch1", new Vector3(0.03f, 0.033f, 0.225f), new Vector3(0.16f, 0.006f, 0.006f), stitchMat);
            slStitch1.localEulerAngles = new Vector3(0f, -18f, 0f);
            var slStitch2 = Block(root, "SleeveLStitch2", new Vector3(-0.045f, 0.033f, 0.29f), new Vector3(0.15f, 0.006f, 0.006f), stitchMat);
            slStitch2.localEulerAngles = new Vector3(0f, -10f, 0f);
            var cuffL = Block(root, "CuffL", new Vector3(-0.085f, 0.001f, 0.35f), new Vector3(0.085f, 0.058f, 0.030f), ribMat);
            cuffL.localEulerAngles = new Vector3(0f, -10f, 0f);
            var cuffLGroove = Block(root, "CuffLGroove", new Vector3(-0.085f, 0.032f, 0.35f), new Vector3(0.085f, 0.006f, 0.006f), ribDeepMat);
            cuffLGroove.localEulerAngles = new Vector3(0f, -10f, 0f);
            var armSeamL = Block(root, "ArmSeamL", new Vector3(0.05f, 0.036f, 0.115f), new Vector3(0.15f, 0.006f, 0.012f), stitchMat);
            armSeamL.localEulerAngles = new Vector3(0f, 36f, 0f);

            Sphere(root, "SleeveR", new Vector3(0.03f, 0f, -0.215f), new Vector3(0f, 18f, 0f), new Vector3(0.18f, 0.060f, 0.24f), fabricMat);
            Sphere(root, "SleeveRFore", new Vector3(-0.06f, 0f, -0.305f), new Vector3(0f, 10f, 0f), new Vector3(0.14f, 0.054f, 0.16f), fabricMat);
            Sphere(root, "SleeveRFold", new Vector3(0.0f, 0.034f, -0.245f), new Vector3(0f, 14f, 0f), new Vector3(0.11f, 0.02f, 0.15f), fabricHiMat);
            var srStitch1 = Block(root, "SleeveRStitch1", new Vector3(0.03f, 0.033f, -0.225f), new Vector3(0.16f, 0.006f, 0.006f), stitchMat);
            srStitch1.localEulerAngles = new Vector3(0f, 18f, 0f);
            var srStitch2 = Block(root, "SleeveRStitch2", new Vector3(-0.045f, 0.033f, -0.29f), new Vector3(0.15f, 0.006f, 0.006f), stitchMat);
            srStitch2.localEulerAngles = new Vector3(0f, 10f, 0f);
            var cuffR = Block(root, "CuffR", new Vector3(-0.085f, 0.001f, -0.35f), new Vector3(0.085f, 0.058f, 0.030f), ribMat);
            cuffR.localEulerAngles = new Vector3(0f, 10f, 0f);
            var cuffRGroove = Block(root, "CuffRGroove", new Vector3(-0.085f, 0.032f, -0.35f), new Vector3(0.085f, 0.006f, 0.006f), ribDeepMat);
            cuffRGroove.localEulerAngles = new Vector3(0f, 10f, 0f);
            var armSeamR = Block(root, "ArmSeamR", new Vector3(0.05f, 0.036f, -0.115f), new Vector3(0.15f, 0.006f, 0.012f), stitchMat);
            armSeamR.localEulerAngles = new Vector3(0f, -36f, 0f);

            // ── Two flap hand-pockets low on the front, each a dark opening under a padded flap with a topstitched
            // edge and a metal snap. ────────────────────────────────────────────────────────────────────────
            Block(root, "PocketMouthL", new Vector3(-0.125f, 0.046f, 0.10f), new Vector3(0.085f, 0.010f, 0.11f), liningMat);
            Block(root, "PocketFlapL", new Vector3(-0.125f, 0.056f, 0.10f), new Vector3(0.07f, 0.016f, 0.10f), fabricMat);
            Block(root, "PocketFlapStitchL", new Vector3(-0.152f, 0.066f, 0.10f), new Vector3(0.006f, 0.006f, 0.10f), stitchMat);
            Rod(root, "PocketSnapL", new Vector3(-0.150f, 0.066f, 0.10f), Vector3.zero, 0.016f, 0.011f, zipMat);
            Block(root, "PocketMouthR", new Vector3(-0.125f, 0.046f, -0.10f), new Vector3(0.085f, 0.010f, 0.11f), liningMat);
            Block(root, "PocketFlapR", new Vector3(-0.125f, 0.056f, -0.10f), new Vector3(0.07f, 0.016f, 0.10f), fabricMat);
            Block(root, "PocketFlapStitchR", new Vector3(-0.152f, 0.066f, -0.10f), new Vector3(0.006f, 0.006f, 0.10f), stitchMat);
            Rod(root, "PocketSnapR", new Vector3(-0.150f, 0.066f, -0.10f), Vector3.zero, 0.016f, 0.011f, zipMat);

            // ── Ribbed elastic hem across the far end, corrugated with alternating raised ribs ─────────────────
            Block(root, "Hem", new Vector3(-0.222f, 0.004f, 0f), new Vector3(0.030f, 0.056f, 0.235f), ribMat);
            const int hemRibs = 13;
            for (int i = 0; i < hemRibs; i++)
            {
                float z = Mathf.Lerp(-0.105f, 0.105f, i / (float)(hemRibs - 1));
                Block(root, "HemRib" + i, new Vector3(-0.222f, 0.033f, z), new Vector3(0.028f, 0.006f, 0.008f),
                    (i % 2 == 0) ? ribDeepMat : fabricHiMat);
            }

            // A woven brand tab on one side of the chest and a couple of shaded creases so the nylon looks relaxed
            // and the slow spin always has something off-centre to catch.
            var brandFrame = new PartNode("Brand");
            brandFrame.SetParent(root, false);
            brandFrame.localPosition = new Vector3(0.0f, 0.052f, 0.11f);
            brandFrame.localEulerAngles = new Vector3(0f, 14f, 0f);
            Block(brandFrame, "BrandPatch", Vector3.zero, new Vector3(0.05f, 0.006f, 0.036f), ribMat);
            Block(brandFrame, "BrandInk", new Vector3(0f, 0.004f, 0f), new Vector3(0.03f, 0.006f, 0.020f), liningMat);
            Sphere(root, "CreaseA", new Vector3(-0.09f, 0.040f, -0.05f), new Vector3(0f, -16f, 0f), new Vector3(0.16f, 0.016f, 0.03f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.145f, 0.038f, 0.04f), new Vector3(0f, 12f, 0f), new Vector3(0.13f, 0.014f, 0.03f), fabricDeepMat);

            return root;
        }

        /// <summary>
        /// A leather biker jacket laid flat, face-up: a smooth, glossy hide with the wide lapels folded open at
        /// the neck, an off-centre diagonal front zip with bright chrome teeth, snap-studded epaulettes across the
        /// shoulders, two angled zip pockets on the front, a buckled waist belt and short zip cuffs on the sleeves.
        /// Takes the item's tint as the leather colour so it reads as that jacket where it lies; the asymmetric
        /// zip, the open lapels, the belt and the chrome hardware set it apart from the padded jacket and the hoodie.
        /// </summary>
        private static PartNode BuildLeatherJacket(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color leather = def.Tint;                                                                // main hide
            Color leatherDeep = new Color(leather.r * 0.70f, leather.g * 0.70f, leather.b * 0.70f);   // shaded creases / underside / panels
            Color leatherHi = Color.Lerp(leather, new Color(0.95f, 0.92f, 0.86f), 0.22f);            // grain catching the light
            Color lining = new Color(leather.r * 0.40f, leather.g * 0.40f, leather.b * 0.40f);        // dark neck hole / under the open lapels / pocket mouths
            Color stitch = Color.Lerp(leather, new Color(0.86f, 0.80f, 0.66f), 0.45f);               // waxed-thread topstitch
            Color metal = new Color(0.80f, 0.81f, 0.84f);                                            // chrome zip coil / buckle / snaps
            Color metalHi = new Color(0.92f, 0.93f, 0.96f);                                          // bright metal highlights
            Color metalDark = new Color(0.30f, 0.31f, 0.34f);                                        // zip tape / recessed metal

            PartMat leatherMat = PartMat.Of(leather, smoothness: 0.45f);   // waxed-hide sheen — glossier than nylon or cotton
            PartMat leatherDeepMat = PartMat.Of(leatherDeep, smoothness: 0.40f);
            PartMat leatherHiMat = PartMat.Of(leatherHi, smoothness: 0.55f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.30f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.70f, metallic: 0.90f);
            PartMat metalHiMat = PartMat.Of(metalHi, smoothness: 0.80f, metallic: 0.90f);
            PartMat metalDarkMat = PartMat.Of(metalDark, smoothness: 0.30f, metallic: 0.50f);

            // ── Body: a fitted flat hide, thinner in Y than the down-filled puffer since leather sits close-cut ──
            // Widest across the shoulders (+X, where the collar and sleeves sit), tapering to the hem (−X).
            Sphere(root, "Torso", new Vector3(-0.03f, 0f, 0f), Vector3.zero, new Vector3(0.33f, 0.050f, 0.27f), leatherMat);
            Sphere(root, "Shoulders", new Vector3(0.095f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.315f), leatherMat);
            Sphere(root, "Chest", new Vector3(0.02f, 0.010f, 0f), Vector3.zero, new Vector3(0.24f, 0.046f, 0.23f), leatherMat);
            Sphere(root, "HemMound", new Vector3(-0.19f, -0.002f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.245f), leatherMat);

            // A yoke seam across the shoulders and two princess seams down the front panels, in waxed thread.
            Block(root, "YokeSeam", new Vector3(0.055f, 0.030f, 0f), new Vector3(0.006f, 0.006f, 0.30f), stitchMat);
            Block(root, "PrincessSeamL", new Vector3(-0.06f, 0.030f, 0.12f), new Vector3(0.26f, 0.006f, 0.006f), stitchMat);
            Block(root, "PrincessSeamR", new Vector3(-0.06f, 0.030f, -0.12f), new Vector3(0.26f, 0.006f, 0.006f), stitchMat);

            // ── Wide lapels folded open at the neck, a stand collar band behind, dark lining showing at the folds ──
            Sphere(root, "NeckHole", new Vector3(0.105f, 0.014f, 0f), Vector3.zero, new Vector3(0.075f, 0.038f, 0.135f), liningMat);
            Sphere(root, "Collar", new Vector3(0.165f, 0.020f, 0f), Vector3.zero, new Vector3(0.065f, 0.034f, 0.20f), leatherMat);
            Sphere(root, "CollarRoll", new Vector3(0.180f, 0.034f, 0f), Vector3.zero, new Vector3(0.040f, 0.028f, 0.17f), leatherHiMat);
            Block(root, "CollarStitch", new Vector3(0.150f, 0.040f, 0f), new Vector3(0.006f, 0.006f, 0.17f), stitchMat);

            // Left lapel (+Z): folded-back leather over a sliver of lining, an edge topstitch and a snap at the point.
            Sphere(root, "LapelLUnder", new Vector3(0.132f, 0.016f, 0.088f), new Vector3(0f, -30f, 0f), new Vector3(0.155f, 0.022f, 0.075f), liningMat);
            Sphere(root, "LapelL", new Vector3(0.126f, 0.028f, 0.072f), new Vector3(0f, -30f, 0f), new Vector3(0.165f, 0.030f, 0.088f), leatherMat);
            var lapelLStitch = Block(root, "LapelLStitch", new Vector3(0.116f, 0.046f, 0.078f), new Vector3(0.150f, 0.006f, 0.006f), stitchMat);
            lapelLStitch.localEulerAngles = new Vector3(0f, -30f, 0f);
            Rod(root, "LapelSnapL", new Vector3(0.052f, 0.046f, 0.118f), Vector3.zero, 0.018f, 0.012f, metalMat);
            Rod(root, "LapelSnapHiL", new Vector3(0.052f, 0.053f, 0.118f), Vector3.zero, 0.009f, 0.010f, metalHiMat);

            // Right lapel (−Z), mirrored.
            Sphere(root, "LapelRUnder", new Vector3(0.132f, 0.016f, -0.088f), new Vector3(0f, 30f, 0f), new Vector3(0.155f, 0.022f, 0.075f), liningMat);
            Sphere(root, "LapelR", new Vector3(0.126f, 0.028f, -0.072f), new Vector3(0f, 30f, 0f), new Vector3(0.165f, 0.030f, 0.088f), leatherMat);
            var lapelRStitch = Block(root, "LapelRStitch", new Vector3(0.116f, 0.046f, -0.078f), new Vector3(0.150f, 0.006f, 0.006f), stitchMat);
            lapelRStitch.localEulerAngles = new Vector3(0f, 30f, 0f);
            Rod(root, "LapelSnapR", new Vector3(0.052f, 0.046f, -0.118f), Vector3.zero, 0.018f, 0.012f, metalMat);
            Rod(root, "LapelSnapHiR", new Vector3(0.052f, 0.053f, -0.118f), Vector3.zero, 0.009f, 0.010f, metalHiMat);

            // ── Snap-studded epaulettes across each shoulder, a small strap from the sleeve head toward the collar ──
            Block(root, "EpauletteL", new Vector3(0.135f, 0.034f, 0.115f), new Vector3(0.050f, 0.010f, 0.130f), leatherMat);
            Block(root, "EpauletteLStitch", new Vector3(0.135f, 0.045f, 0.115f), new Vector3(0.006f, 0.006f, 0.120f), stitchMat);
            Rod(root, "EpauletteSnapL", new Vector3(0.135f, 0.046f, 0.062f), Vector3.zero, 0.015f, 0.011f, metalMat);
            Block(root, "EpauletteR", new Vector3(0.135f, 0.034f, -0.115f), new Vector3(0.050f, 0.010f, 0.130f), leatherMat);
            Block(root, "EpauletteRStitch", new Vector3(0.135f, 0.045f, -0.115f), new Vector3(0.006f, 0.006f, 0.120f), stitchMat);
            Rod(root, "EpauletteSnapR", new Vector3(0.135f, 0.046f, -0.062f), Vector3.zero, 0.015f, 0.011f, metalMat);

            // ── Asymmetric diagonal front zip — the biker jacket's signature closure, offset to one side up top and
            // raked down toward the centre hem, with an overlapping leather storm flap along its high side. ──
            Vector3 zipTop = new Vector3(0.095f, 0.050f, 0.048f);
            Vector3 zipBot = new Vector3(-0.205f, 0.032f, -0.018f);
            Vector3 zipMid = (zipTop + zipBot) * 0.5f;
            float zipYaw = Mathf.Atan2(-(zipBot.z - zipTop.z), zipBot.x - zipTop.x) * Mathf.Rad2Deg;  // lay a block from top→bot
            float zipRun = Vector2.Distance(new Vector2(zipTop.x, zipTop.z), new Vector2(zipBot.x, zipBot.z));
            Vector2 zdir = new Vector2(zipBot.x - zipTop.x, zipBot.z - zipTop.z).normalized;
            Vector2 zperp = new Vector2(-zdir.y, zdir.x);
            if (zperp.y < 0f) zperp = -zperp;                          // point the storm flap to the +Z (overlapping) side
            Vector3 flapCtr = zipMid + new Vector3(zperp.x, 0f, zperp.y) * 0.034f;

            var stormFlap = Block(root, "StormFlap", new Vector3(flapCtr.x, 0.048f, flapCtr.z), new Vector3(zipRun * 0.94f, 0.026f, 0.058f), leatherMat);
            stormFlap.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            var stormFlapHi = Block(root, "StormFlapHi", new Vector3(flapCtr.x, 0.062f, flapCtr.z), new Vector3(zipRun * 0.82f, 0.006f, 0.030f), leatherHiMat);
            stormFlapHi.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            var stormFlapStitch = Block(root, "StormFlapStitch",
                new Vector3(zipMid.x + zperp.x * 0.010f, 0.058f, zipMid.z + zperp.y * 0.010f),
                new Vector3(zipRun * 0.9f, 0.006f, 0.006f), stitchMat);
            stormFlapStitch.localEulerAngles = new Vector3(0f, zipYaw, 0f);

            // Dark zip tape then a run of bright chrome teeth along the diagonal.
            var zipTape = Block(root, "ZipTape", new Vector3(zipMid.x, 0.050f, zipMid.z), new Vector3(zipRun, 0.016f, 0.026f), metalDarkMat);
            zipTape.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            const int teeth = 18;
            for (int i = 0; i < teeth; i++)
            {
                float t = i / (float)(teeth - 1);
                Vector3 p = Vector3.Lerp(zipTop, zipBot, t);
                var tooth = Block(root, "Tooth" + i, new Vector3(p.x, 0.062f, p.z), new Vector3(0.010f, 0.006f, 0.013f),
                    (i % 2 == 0) ? metalMat : metalHiMat);
                tooth.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            }
            // Slider partway down with a bright top, then a pull tab hanging further along the zip toward the hem.
            Vector3 sliderP = Vector3.Lerp(zipTop, zipBot, 0.42f);
            var slider = Block(root, "Slider", new Vector3(sliderP.x, 0.066f, sliderP.z), new Vector3(0.030f, 0.014f, 0.030f), metalMat);
            slider.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            var sliderTop = Block(root, "SliderTop", new Vector3(sliderP.x, 0.075f, sliderP.z), new Vector3(0.022f, 0.006f, 0.022f), metalHiMat);
            sliderTop.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            Vector3 pullP = Vector3.Lerp(zipTop, zipBot, 0.56f);
            var pullTab = Block(root, "PullTab", new Vector3(pullP.x, 0.068f, pullP.z), new Vector3(0.030f, 0.010f, 0.014f), metalMat);
            pullTab.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            Vector3 pullEndP = Vector3.Lerp(zipTop, zipBot, 0.66f);
            var pullTabEnd = Block(root, "PullTabEnd", new Vector3(pullEndP.x, 0.068f, pullEndP.z), new Vector3(0.012f, 0.008f, 0.020f), metalHiMat);
            pullTabEnd.localEulerAngles = new Vector3(0f, zipYaw, 0f);
            // Zip stop tucked under the collar at the top of the run.
            Block(root, "ZipStop", new Vector3(zipTop.x + 0.010f, 0.056f, zipTop.z), new Vector3(0.020f, 0.030f, 0.040f), leatherHiMat);

            // A small zip ticket-pocket on the upper chest (one side only), breaking the symmetry like the real thing.
            var ticketWelt = Block(root, "TicketWelt", new Vector3(-0.01f, 0.034f, 0.10f), new Vector3(0.085f, 0.012f, 0.020f), leatherDeepMat);
            ticketWelt.localEulerAngles = new Vector3(0f, -58f, 0f);
            var ticketTeeth = Block(root, "TicketTeeth", new Vector3(-0.01f, 0.043f, 0.10f), new Vector3(0.070f, 0.006f, 0.006f), metalHiMat);
            ticketTeeth.localEulerAngles = new Vector3(0f, -58f, 0f);
            Rod(root, "TicketPull", new Vector3(-0.042f, 0.044f, 0.128f), Vector3.zero, 0.014f, 0.010f, metalMat);

            // ── Two angled zip hand-pockets low on the front, each a leather welt over a dark mouth with chrome teeth. ──
            for (int s = -1; s <= 1; s += 2)
            {
                float z = 0.105f * s;
                string sfx = s > 0 ? "L" : "R";
                float yaw = -32f * s;
                var welt = Block(root, "PocketWelt" + sfx, new Vector3(-0.085f, 0.032f, z), new Vector3(0.13f, 0.012f, 0.024f), leatherMat);
                welt.localEulerAngles = new Vector3(0f, yaw, 0f);
                var mouth = Block(root, "PocketMouth" + sfx, new Vector3(-0.085f, 0.038f, z), new Vector3(0.115f, 0.008f, 0.012f), metalDarkMat);
                mouth.localEulerAngles = new Vector3(0f, yaw, 0f);
                var teethP = Block(root, "PocketTeeth" + sfx, new Vector3(-0.085f, 0.043f, z), new Vector3(0.11f, 0.006f, 0.006f), metalHiMat);
                teethP.localEulerAngles = new Vector3(0f, yaw, 0f);
                Rod(root, "PocketPull" + sfx, new Vector3(-0.14f, 0.044f, z + 0.052f * s), Vector3.zero, 0.016f, 0.011f, metalMat);
            }

            // ── Buckled waist belt across the lower body, the classic Perfecto touch ──
            Block(root, "Belt", new Vector3(-0.175f, 0.034f, -0.02f), new Vector3(0.050f, 0.016f, 0.42f), leatherDeepMat);
            Block(root, "BeltStitchTop", new Vector3(-0.158f, 0.044f, -0.02f), new Vector3(0.006f, 0.006f, 0.40f), stitchMat);
            Block(root, "BeltStitchBottom", new Vector3(-0.192f, 0.044f, -0.02f), new Vector3(0.006f, 0.006f, 0.40f), stitchMat);
            for (int i = 0; i < 4; i++)
            {
                float z = Mathf.Lerp(-0.17f, 0.14f, i / 3f);
                Block(root, "BeltLoop" + i, new Vector3(-0.175f, 0.048f, z), new Vector3(0.020f, 0.014f, 0.018f), leatherMat);
            }
            // The belt's free tail runs past the buckle to the +Z side, with a couple of punch holes.
            Block(root, "BeltTail", new Vector3(-0.175f, 0.040f, 0.155f), new Vector3(0.046f, 0.014f, 0.11f), leatherMat);
            Block(root, "BeltHole0", new Vector3(-0.175f, 0.050f, 0.135f), new Vector3(0.010f, 0.010f, 0.010f), leatherDeepMat);
            Block(root, "BeltHole1", new Vector3(-0.175f, 0.050f, 0.175f), new Vector3(0.010f, 0.010f, 0.010f), leatherDeepMat);
            // Chrome buckle, offset toward the +Z side where the tail threads through.
            Block(root, "Buckle", new Vector3(-0.175f, 0.050f, 0.085f), new Vector3(0.058f, 0.012f, 0.058f), metalMat);
            Block(root, "BuckleHole", new Vector3(-0.175f, 0.056f, 0.085f), new Vector3(0.034f, 0.014f, 0.034f), leatherDeepMat);
            Block(root, "BucklePin", new Vector3(-0.175f, 0.057f, 0.085f), new Vector3(0.044f, 0.008f, 0.006f), metalHiMat);
            Block(root, "BuckleFrameHi", new Vector3(-0.175f, 0.058f, 0.085f), new Vector3(0.058f, 0.005f, 0.010f), metalHiMat);

            // ── Long sleeves splayed out and angled back toward the hem, with a panel seam and a short zip cuff. ──
            // (Left = +Z, Right = −Z.)
            Sphere(root, "SleeveL", new Vector3(0.03f, 0f, 0.215f), new Vector3(0f, -18f, 0f), new Vector3(0.18f, 0.050f, 0.24f), leatherMat);
            Sphere(root, "SleeveLFore", new Vector3(-0.06f, 0f, 0.305f), new Vector3(0f, -10f, 0f), new Vector3(0.14f, 0.046f, 0.16f), leatherMat);
            Sphere(root, "SleeveLFold", new Vector3(0.0f, 0.030f, 0.245f), new Vector3(0f, -14f, 0f), new Vector3(0.11f, 0.018f, 0.15f), leatherHiMat);
            var slSeam = Block(root, "SleeveLSeam", new Vector3(-0.01f, 0.030f, 0.25f), new Vector3(0.26f, 0.006f, 0.006f), stitchMat);
            slSeam.localEulerAngles = new Vector3(0f, -14f, 0f);
            var slArm = Block(root, "ArmSeamL", new Vector3(0.05f, 0.034f, 0.115f), new Vector3(0.15f, 0.006f, 0.012f), stitchMat);
            slArm.localEulerAngles = new Vector3(0f, 36f, 0f);
            var cuffZipL = Block(root, "CuffZipL", new Vector3(-0.095f, 0.032f, 0.35f), new Vector3(0.075f, 0.010f, 0.014f), metalDarkMat);
            cuffZipL.localEulerAngles = new Vector3(0f, -10f, 0f);
            var cuffTeethL = Block(root, "CuffTeethL", new Vector3(-0.095f, 0.039f, 0.35f), new Vector3(0.070f, 0.006f, 0.006f), metalHiMat);
            cuffTeethL.localEulerAngles = new Vector3(0f, -10f, 0f);
            Rod(root, "CuffPullL", new Vector3(-0.055f, 0.040f, 0.34f), Vector3.zero, 0.014f, 0.010f, metalMat);

            Sphere(root, "SleeveR", new Vector3(0.03f, 0f, -0.215f), new Vector3(0f, 18f, 0f), new Vector3(0.18f, 0.050f, 0.24f), leatherMat);
            Sphere(root, "SleeveRFore", new Vector3(-0.06f, 0f, -0.305f), new Vector3(0f, 10f, 0f), new Vector3(0.14f, 0.046f, 0.16f), leatherMat);
            Sphere(root, "SleeveRFold", new Vector3(0.0f, 0.030f, -0.245f), new Vector3(0f, 14f, 0f), new Vector3(0.11f, 0.018f, 0.15f), leatherHiMat);
            var srSeam = Block(root, "SleeveRSeam", new Vector3(-0.01f, 0.030f, -0.25f), new Vector3(0.26f, 0.006f, 0.006f), stitchMat);
            srSeam.localEulerAngles = new Vector3(0f, 14f, 0f);
            var srArm = Block(root, "ArmSeamR", new Vector3(0.05f, 0.034f, -0.115f), new Vector3(0.15f, 0.006f, 0.012f), stitchMat);
            srArm.localEulerAngles = new Vector3(0f, -36f, 0f);
            var cuffZipR = Block(root, "CuffZipR", new Vector3(-0.095f, 0.032f, -0.35f), new Vector3(0.075f, 0.010f, 0.014f), metalDarkMat);
            cuffZipR.localEulerAngles = new Vector3(0f, 10f, 0f);
            var cuffTeethR = Block(root, "CuffTeethR", new Vector3(-0.095f, 0.039f, -0.35f), new Vector3(0.070f, 0.006f, 0.006f), metalHiMat);
            cuffTeethR.localEulerAngles = new Vector3(0f, 10f, 0f);
            Rod(root, "CuffPullR", new Vector3(-0.055f, 0.040f, -0.34f), Vector3.zero, 0.014f, 0.010f, metalMat);

            // ── Plain topstitched leather hem across the far end ──
            Block(root, "Hem", new Vector3(-0.215f, 0.004f, 0f), new Vector3(0.020f, 0.050f, 0.235f), leatherDeepMat);
            Block(root, "HemStitch", new Vector3(-0.205f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.215f), stitchMat);

            // A couple of shaded creases so the hide looks worn-in, set off-centre to break the symmetry.
            Sphere(root, "CreaseA", new Vector3(-0.10f, 0.028f, -0.05f), new Vector3(0f, -18f, 0f), new Vector3(0.16f, 0.014f, 0.03f), leatherDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.05f, 0.030f, 0.05f), new Vector3(0f, 14f, 0f), new Vector3(0.14f, 0.014f, 0.03f), leatherDeepMat);

            return root;
        }

        /// <summary>
        /// A hooded waterproof raincoat laid flat, face-up: a glossy coated shell with an attached hood raised
        /// at the neck, a storm cape over the shoulders, a press-stud front fly, big flap patch pockets, smooth
        /// tab-adjuster cuffs and heat-welded (sealed) seams throughout. Takes the item's tint as the coat colour
        /// so it reads as that slicker where it lies; the wet sheen, the stiff-brimmed hood with its toggle
        /// drawcords, the storm cape and the snap-fastened fly set it apart from the other coats.
        /// </summary>
        private static PartNode BuildRaincoat(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                              // glossy waterproof shell
            Color fabricDeep = new Color(fabric.r * 0.72f, fabric.g * 0.72f, fabric.b * 0.72f);    // shaded folds / underside / cape shadow
            Color fabricHi = Color.Lerp(fabric, new Color(0.98f, 0.98f, 0.95f), 0.28f);            // wet sheen catching the light
            Color seam = Color.Lerp(fabric, new Color(0.98f, 0.98f, 0.95f), 0.16f);                // heat-welded (sealed) seams
            Color lining = new Color(fabric.r * 0.40f, fabric.g * 0.40f, fabric.b * 0.40f);         // dark hood opening / neck / pocket mouths
            Color cord = new Color(0.93f, 0.92f, 0.86f);                                           // cream hood drawcords
            Color toggle = new Color(0.24f, 0.22f, 0.20f);                                         // dark barrel toggles / cord tips
            Color snap = new Color(0.80f, 0.81f, 0.84f);                                           // metal press studs
            Color snapHi = new Color(0.92f, 0.93f, 0.96f);                                         // bright stud highlights
            Color eyelet = new Color(0.58f, 0.59f, 0.62f);                                         // metal grommets round the hood cord

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.62f);   // wet-look waterproof sheen — the glossiest garment
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.55f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.72f);
            PartMat seamMat = PartMat.Of(seam, smoothness: 0.66f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.28f);   // matte inside
            PartMat cordMat = PartMat.Of(cord, smoothness: 0.20f);
            PartMat toggleMat = PartMat.Of(toggle, smoothness: 0.35f);
            PartMat snapMat = PartMat.Of(snap, smoothness: 0.70f, metallic: 0.90f);
            PartMat snapHiMat = PartMat.Of(snapHi, smoothness: 0.80f, metallic: 0.90f);
            PartMat eyeletMat = PartMat.Of(eyelet, smoothness: 0.60f, metallic: 0.85f);

            // Soft shell laid flat: overlapping flattened mounds, widest across the shoulders (+X, where the
            // collar, hood and sleeves sit) and running out to a longer, slightly flared skirt at the far end
            // (−X) — a raincoat hangs lower and wider than a jacket. Not padded, so it stays fairly thin in Y.
            Sphere(root, "Torso", new Vector3(-0.05f, 0f, 0f), Vector3.zero, new Vector3(0.36f, 0.052f, 0.27f), fabricMat);
            Sphere(root, "Shoulders", new Vector3(0.085f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.315f), fabricMat);
            Sphere(root, "Chest", new Vector3(0.02f, 0.010f, 0f), Vector3.zero, new Vector3(0.24f, 0.048f, 0.23f), fabricMat);
            Sphere(root, "Skirt", new Vector3(-0.185f, 0.002f, 0f), Vector3.zero, new Vector3(0.16f, 0.050f, 0.285f), fabricMat);
            Sphere(root, "HemMound", new Vector3(-0.262f, -0.002f, 0f), Vector3.zero, new Vector3(0.10f, 0.048f, 0.255f), fabricMat);

            // Two welded panel seams run the length of the front, sealed rather than stitched — smooth glossy
            // lines that catch the light, in keeping with a waterproof coat.
            Block(root, "PanelSeamL", new Vector3(-0.10f, 0.030f, 0.11f), new Vector3(0.26f, 0.006f, 0.008f), seamMat);
            Block(root, "PanelSeamR", new Vector3(-0.10f, 0.030f, -0.11f), new Vector3(0.26f, 0.006f, 0.008f), seamMat);

            // ── Storm cape over the shoulders: a wide extra layer of shell laid across the chest, its free edge
            // welded and casting a shadow on the body below — the classic hooded-slicker cape that sheds water
            // off the back and shoulders and hides the top of the fly. ────────────────────────────────────────
            Sphere(root, "StormCape", new Vector3(0.05f, 0.032f, 0f), Vector3.zero, new Vector3(0.18f, 0.020f, 0.33f), fabricMat);
            Sphere(root, "StormCapeHi", new Vector3(0.075f, 0.046f, 0f), Vector3.zero, new Vector3(0.11f, 0.012f, 0.28f), fabricHiMat);
            Block(root, "StormCapeEdge", new Vector3(-0.035f, 0.044f, 0f), new Vector3(0.010f, 0.008f, 0.30f), seamMat);
            Block(root, "StormCapeShadow", new Vector3(-0.055f, 0.026f, 0f), new Vector3(0.022f, 0.006f, 0.30f), fabricDeepMat);

            // ── Front fly, fastened with a row of press studs, its top tucked under the storm cape (waterproof
            // coats keep the closure behind a covered band). A raised placket down the centre carries the snaps. ──
            const float plkTop = -0.02f, plkBot = -0.245f;
            float plkLen = plkTop - plkBot;
            float plkMid = (plkTop + plkBot) * 0.5f;
            Block(root, "Placket", new Vector3(plkMid, 0.036f, 0f), new Vector3(plkLen, 0.014f, 0.055f), fabricMat);
            Block(root, "PlacketEdgeL", new Vector3(plkMid, 0.045f, 0.030f), new Vector3(plkLen, 0.006f, 0.006f), seamMat);
            Block(root, "PlacketEdgeR", new Vector3(plkMid, 0.045f, -0.030f), new Vector3(plkLen, 0.006f, 0.006f), seamMat);
            const int flySnaps = 5;
            for (int i = 0; i < flySnaps; i++)
            {
                float x = Mathf.Lerp(plkTop - 0.02f, plkBot + 0.02f, i / (float)(flySnaps - 1));
                Rod(root, "FlySnap" + i, new Vector3(x, 0.046f, 0f), Vector3.zero, 0.018f, 0.012f, snapMat);
                Rod(root, "FlySnapHi" + i, new Vector3(x, 0.053f, 0f), Vector3.zero, 0.009f, 0.010f, snapHiMat);
            }

            // ── Collar and attached hood raised at the neck ───────────────────────────────────────────────────
            // A dark neck hole and a low collar band, then the hood itself standing up behind and over it (+X):
            // a rounded shell with a wet-sheen crown and side walls, a dark opening (the hood mouth), and a stiff
            // rolled brim curving round the front of the opening. The raised, brimmed hood is the raincoat's tell.
            Sphere(root, "NeckHole", new Vector3(0.092f, 0.018f, 0f), Vector3.zero, new Vector3(0.075f, 0.040f, 0.15f), liningMat);
            Sphere(root, "Collar", new Vector3(0.14f, 0.024f, 0f), Vector3.zero, new Vector3(0.05f, 0.032f, 0.19f), fabricMat);
            Block(root, "CollarSeam", new Vector3(0.118f, 0.044f, 0f), new Vector3(0.006f, 0.006f, 0.17f), seamMat);
            // Throat tab with a snap, closing the collar at the very top of the fly.
            Block(root, "ThroatTab", new Vector3(0.104f, 0.044f, 0f), new Vector3(0.05f, 0.010f, 0.05f), fabricMat);
            Rod(root, "ThroatSnap", new Vector3(0.088f, 0.052f, 0f), Vector3.zero, 0.016f, 0.011f, snapMat);
            Rod(root, "ThroatSnapHi", new Vector3(0.088f, 0.058f, 0f), Vector3.zero, 0.008f, 0.009f, snapHiMat);

            Sphere(root, "HoodShell", new Vector3(0.245f, 0.030f, 0f), Vector3.zero, new Vector3(0.22f, 0.11f, 0.28f), fabricMat);
            Sphere(root, "HoodBack", new Vector3(0.305f, 0.018f, 0f), Vector3.zero, new Vector3(0.12f, 0.075f, 0.22f), fabricMat);
            Sphere(root, "HoodCrown", new Vector3(0.250f, 0.088f, 0f), Vector3.zero, new Vector3(0.15f, 0.05f, 0.20f), fabricHiMat);
            Sphere(root, "HoodSideL", new Vector3(0.235f, 0.020f, 0.13f), new Vector3(0f, -18f, 0f), new Vector3(0.10f, 0.05f, 0.12f), fabricMat);
            Sphere(root, "HoodSideR", new Vector3(0.235f, 0.020f, -0.13f), new Vector3(0f, 18f, 0f), new Vector3(0.10f, 0.05f, 0.12f), fabricMat);
            Sphere(root, "HoodMouth", new Vector3(0.155f, 0.040f, 0f), Vector3.zero, new Vector3(0.09f, 0.055f, 0.20f), liningMat);
            Sphere(root, "HoodMouthDeep", new Vector3(0.175f, 0.030f, 0f), Vector3.zero, new Vector3(0.07f, 0.045f, 0.16f), liningMat);

            // Stiff rolled brim round the front of the hood opening: an arc of fat segments bulging toward the
            // face (−X) at the middle, with a bright highlight roll on top. (Rod euler (90, 90−heading, 0) lays
            // each segment flat along the arc's tangent.)
            const int brimSegs = 9;
            for (int i = 0; i < brimSegs; i++)
            {
                float t = i / (float)(brimSegs - 1);
                float z = Mathf.Lerp(-0.185f, 0.185f, t);
                float x = 0.15f - 0.06f * Mathf.Sin(t * Mathf.PI);           // bulge toward the face at the middle
                float dxdt = -0.06f * Mathf.PI * Mathf.Cos(t * Mathf.PI);    // arc tangent, for laying each segment
                const float dzdt = 0.37f;
                float heading = Mathf.Atan2(dzdt, dxdt) * Mathf.Rad2Deg;
                Rod(root, "Brim" + i, new Vector3(x, 0.058f, z), new Vector3(90f, 90f - heading, 0f), 0.032f, 0.07f, fabricMat);
                Rod(root, "BrimHi" + i, new Vector3(x, 0.073f, z), new Vector3(90f, 90f - heading, 0f), 0.016f, 0.062f, fabricHiMat);
            }

            // Two toggle drawcords from grommets on the brim, trailing down over the chest with a barrel toggle
            // partway along each and a dark tip at the end. (Left = +Z, right = −Z; Rod euler (90, 90−heading, 0)
            // lays a segment flat along a heading measured from +X toward +Z.)
            Rod(root, "HoodEyeletL", new Vector3(0.10f, 0.066f, 0.035f), Vector3.zero, 0.024f, 0.014f, eyeletMat);
            Rod(root, "HoodEyeletHoleL", new Vector3(0.10f, 0.074f, 0.035f), Vector3.zero, 0.010f, 0.010f, liningMat);
            Sphere(root, "CordKnotL", new Vector3(0.10f, 0.060f, 0.035f), Vector3.zero, new Vector3(0.020f, 0.016f, 0.020f), cordMat);
            Rod(root, "CordL1", new Vector3(0.045f, 0.052f, 0.045f), new Vector3(90f, -80f, 0f), 0.010f, 0.12f, cordMat);
            Rod(root, "ToggleL", new Vector3(-0.02f, 0.052f, 0.050f), new Vector3(90f, -80f, 0f), 0.030f, 0.05f, toggleMat);
            Rod(root, "CordL2", new Vector3(-0.075f, 0.050f, 0.052f), new Vector3(90f, -98f, 0f), 0.010f, 0.06f, cordMat);
            Rod(root, "CordTipL", new Vector3(-0.115f, 0.050f, 0.048f), new Vector3(90f, -98f, 0f), 0.014f, 0.022f, toggleMat);

            Rod(root, "HoodEyeletR", new Vector3(0.10f, 0.066f, -0.035f), Vector3.zero, 0.024f, 0.014f, eyeletMat);
            Rod(root, "HoodEyeletHoleR", new Vector3(0.10f, 0.074f, -0.035f), Vector3.zero, 0.010f, 0.010f, liningMat);
            Sphere(root, "CordKnotR", new Vector3(0.10f, 0.060f, -0.035f), Vector3.zero, new Vector3(0.020f, 0.016f, 0.020f), cordMat);
            Rod(root, "CordR1", new Vector3(0.045f, 0.052f, -0.045f), new Vector3(90f, 260f, 0f), 0.010f, 0.12f, cordMat);
            Rod(root, "ToggleR", new Vector3(-0.02f, 0.052f, -0.050f), new Vector3(90f, 260f, 0f), 0.030f, 0.05f, toggleMat);
            Rod(root, "CordR2", new Vector3(-0.075f, 0.050f, -0.052f), new Vector3(90f, 278f, 0f), 0.010f, 0.06f, cordMat);
            Rod(root, "CordTipR", new Vector3(-0.115f, 0.050f, -0.048f), new Vector3(90f, 278f, 0f), 0.014f, 0.022f, toggleMat);

            // ── Long sleeves, splayed out and angled back toward the hem, each with a wet-sheen fold, a welded
            // seam down its length, a raglan armhole seam and a smooth cuff with an adjuster tab and snap. ──────
            // (Left = +Z, right = −Z.)
            Sphere(root, "SleeveL", new Vector3(0.03f, 0f, 0.215f), new Vector3(0f, -18f, 0f), new Vector3(0.18f, 0.052f, 0.24f), fabricMat);
            Sphere(root, "SleeveLFore", new Vector3(-0.06f, 0f, 0.305f), new Vector3(0f, -10f, 0f), new Vector3(0.14f, 0.048f, 0.16f), fabricMat);
            Sphere(root, "SleeveLSheen", new Vector3(0.0f, 0.032f, 0.245f), new Vector3(0f, -14f, 0f), new Vector3(0.11f, 0.014f, 0.15f), fabricHiMat);
            var slSeam = Block(root, "SleeveLSeam", new Vector3(-0.01f, 0.030f, 0.25f), new Vector3(0.26f, 0.006f, 0.008f), seamMat);
            slSeam.localEulerAngles = new Vector3(0f, -14f, 0f);
            var slArm = Block(root, "ArmSeamL", new Vector3(0.05f, 0.034f, 0.115f), new Vector3(0.15f, 0.006f, 0.010f), seamMat);
            slArm.localEulerAngles = new Vector3(0f, 36f, 0f);
            var cuffL = Block(root, "CuffL", new Vector3(-0.085f, 0.028f, 0.35f), new Vector3(0.075f, 0.020f, 0.030f), fabricMat);
            cuffL.localEulerAngles = new Vector3(0f, -10f, 0f);
            var cuffLSeam = Block(root, "CuffLSeam", new Vector3(-0.062f, 0.040f, 0.35f), new Vector3(0.006f, 0.006f, 0.030f), seamMat);
            cuffLSeam.localEulerAngles = new Vector3(0f, -10f, 0f);
            var cuffTabL = Block(root, "CuffTabL", new Vector3(-0.072f, 0.038f, 0.318f), new Vector3(0.055f, 0.010f, 0.016f), fabricMat);
            cuffTabL.localEulerAngles = new Vector3(0f, 22f, 0f);
            Rod(root, "CuffSnapL", new Vector3(-0.052f, 0.046f, 0.312f), Vector3.zero, 0.014f, 0.010f, snapMat);
            Rod(root, "CuffSnapHiL", new Vector3(-0.052f, 0.052f, 0.312f), Vector3.zero, 0.008f, 0.008f, snapHiMat);

            Sphere(root, "SleeveR", new Vector3(0.03f, 0f, -0.215f), new Vector3(0f, 18f, 0f), new Vector3(0.18f, 0.052f, 0.24f), fabricMat);
            Sphere(root, "SleeveRFore", new Vector3(-0.06f, 0f, -0.305f), new Vector3(0f, 10f, 0f), new Vector3(0.14f, 0.048f, 0.16f), fabricMat);
            Sphere(root, "SleeveRSheen", new Vector3(0.0f, 0.032f, -0.245f), new Vector3(0f, 14f, 0f), new Vector3(0.11f, 0.014f, 0.15f), fabricHiMat);
            var srSeam = Block(root, "SleeveRSeam", new Vector3(-0.01f, 0.030f, -0.25f), new Vector3(0.26f, 0.006f, 0.008f), seamMat);
            srSeam.localEulerAngles = new Vector3(0f, 14f, 0f);
            var srArm = Block(root, "ArmSeamR", new Vector3(0.05f, 0.034f, -0.115f), new Vector3(0.15f, 0.006f, 0.010f), seamMat);
            srArm.localEulerAngles = new Vector3(0f, -36f, 0f);
            var cuffR = Block(root, "CuffR", new Vector3(-0.085f, 0.028f, -0.35f), new Vector3(0.075f, 0.020f, 0.030f), fabricMat);
            cuffR.localEulerAngles = new Vector3(0f, 10f, 0f);
            var cuffRSeam = Block(root, "CuffRSeam", new Vector3(-0.062f, 0.040f, -0.35f), new Vector3(0.006f, 0.006f, 0.030f), seamMat);
            cuffRSeam.localEulerAngles = new Vector3(0f, 10f, 0f);
            var cuffTabR = Block(root, "CuffTabR", new Vector3(-0.072f, 0.038f, -0.318f), new Vector3(0.055f, 0.010f, 0.016f), fabricMat);
            cuffTabR.localEulerAngles = new Vector3(0f, -22f, 0f);
            Rod(root, "CuffSnapR", new Vector3(-0.052f, 0.046f, -0.312f), Vector3.zero, 0.014f, 0.010f, snapMat);
            Rod(root, "CuffSnapHiR", new Vector3(-0.052f, 0.052f, -0.312f), Vector3.zero, 0.008f, 0.008f, snapHiMat);

            // ── Two big flap patch pockets low on the front, each a square patch with a welded outline, a flap
            // over the top and a press stud. ─────────────────────────────────────────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                float z = 0.10f * s;
                string sfx = s > 0 ? "L" : "R";
                Block(root, "PocketBody" + sfx, new Vector3(-0.15f, 0.030f, z), new Vector3(0.12f, 0.012f, 0.12f), fabricMat);
                Block(root, "PocketHem" + sfx, new Vector3(-0.208f, 0.040f, z), new Vector3(0.006f, 0.006f, 0.12f), seamMat);
                Block(root, "PocketSideOuter" + sfx, new Vector3(-0.15f, 0.040f, z + 0.058f * s), new Vector3(0.12f, 0.006f, 0.006f), seamMat);
                Block(root, "PocketSideInner" + sfx, new Vector3(-0.15f, 0.040f, z - 0.058f * s), new Vector3(0.12f, 0.006f, 0.006f), seamMat);
                Block(root, "PocketFlap" + sfx, new Vector3(-0.10f, 0.042f, z), new Vector3(0.05f, 0.014f, 0.118f), fabricMat);
                Block(root, "PocketFlapSeam" + sfx, new Vector3(-0.122f, 0.052f, z), new Vector3(0.006f, 0.006f, 0.10f), seamMat);
                Rod(root, "PocketSnap" + sfx, new Vector3(-0.112f, 0.052f, z), Vector3.zero, 0.016f, 0.011f, snapMat);
                Rod(root, "PocketSnapHi" + sfx, new Vector3(-0.112f, 0.058f, z), Vector3.zero, 0.008f, 0.009f, snapHiMat);
            }

            // ── Plain welded hem across the far end ──
            Block(root, "Hem", new Vector3(-0.302f, 0.004f, 0f), new Vector3(0.018f, 0.048f, 0.245f), fabricMat);
            Block(root, "HemSeam", new Vector3(-0.290f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.225f), seamMat);

            // A couple of wet-look sheen streaks and a few shaded creases so the coated shell looks slick and
            // relaxed, set off-centre so the slow spin always has something to catch.
            Sphere(root, "Sheen1", new Vector3(-0.12f, 0.032f, 0.06f), new Vector3(0f, 14f, 0f), new Vector3(0.20f, 0.014f, 0.05f), fabricHiMat);
            Sphere(root, "Sheen2", new Vector3(-0.03f, 0.030f, -0.08f), new Vector3(0f, -12f, 0f), new Vector3(0.16f, 0.012f, 0.045f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.19f, 0.026f, -0.05f), new Vector3(0f, -16f, 0f), new Vector3(0.16f, 0.012f, 0.03f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.235f, 0.024f, 0.05f), new Vector3(0f, 12f, 0f), new Vector3(0.13f, 0.012f, 0.03f), fabricDeepMat);

            return root;
        }

        /// <summary>
        /// A pair of denim shorts laid flat, face-up: a waistband with belt loops, a buttoned fly, slanted
        /// hip pockets and two stubby legs split by the crotch, each finished with a rolled turn-up. Takes the
        /// item's tint as the denim so it reads as those shorts where it lies; the short cuffed legs and the
        /// crotch gap set the legwear apart from the flat-bodied tops.
        /// </summary>
        private static PartNode BuildShorts(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                            // denim
            Color fabricDeep = new Color(fabric.r * 0.76f, fabric.g * 0.76f, fabric.b * 0.76f);  // shaded creases / crotch shadow
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised folds catching light
            Color fade = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.36f);             // sun-worn panels / pale turn-up facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.84f, 0.52f), 0.55f);           // warm topstitching thread
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark pocket bags and the gap between the legs
            Color metal = new Color(0.78f, 0.79f, 0.82f);                                        // button, rivets, zip teeth
            Color metalHi = new Color(0.90f, 0.91f, 0.94f);
            Color patch = new Color(0.42f, 0.28f, 0.18f);                                        // leather brand tab on the waistband
            Color patchInk = new Color(0.24f, 0.16f, 0.10f);

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.10f);   // matte cotton twill
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.09f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.12f);
            PartMat fadeMat = PartMat.Of(fade, smoothness: 0.12f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat metalHiMat = PartMat.Of(metalHi, smoothness: 0.62f, metallic: 0.9f);
            PartMat patchMat = PartMat.Of(patch, smoothness: 0.30f);
            PartMat patchInkMat = PartMat.Of(patchInk, smoothness: 0.25f);

            // ── Seat and hips, laid flat with the waist at +X and the legs running out to −X ─────────────
            Sphere(root, "Seat", new Vector3(0.075f, 0f, 0f), Vector3.zero, new Vector3(0.22f, 0.058f, 0.30f), fabricMat);
            Sphere(root, "Hips", new Vector3(0.14f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.056f, 0.29f), fabricMat);
            Sphere(root, "Rise", new Vector3(0.02f, 0.012f, 0f), Vector3.zero, new Vector3(0.16f, 0.05f, 0.20f), fabricMat);

            // Waistband across the top with a topstitch along each of its edges.
            Block(root, "Waistband", new Vector3(0.185f, 0.002f, 0f), new Vector3(0.046f, 0.064f, 0.295f), fabricMat);
            Block(root, "WaistStitchTop", new Vector3(0.204f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);
            Block(root, "WaistStitchLow", new Vector3(0.166f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);

            // Five belt loops bridging the band, standing proud of it.
            const int loops = 5;
            for (int i = 0; i < loops; i++)
            {
                float z = Mathf.Lerp(-0.115f, 0.115f, i / (float)(loops - 1));
                Block(root, "BeltLoop" + i, new Vector3(0.185f, 0.036f, z), new Vector3(0.054f, 0.010f, 0.016f), fabricMat);
                Block(root, "BeltLoopBar" + i, new Vector3(0.203f, 0.042f, z), new Vector3(0.008f, 0.006f, 0.014f), stitchMat);
            }

            // ── Buttoned fly on the centre front: a raised placket over a short zip, closed by a shank button ──
            Block(root, "Placket", new Vector3(0.128f, 0.032f, 0.014f), new Vector3(0.080f, 0.012f, 0.042f), fabricMat);
            Block(root, "ZipTape", new Vector3(0.128f, 0.034f, -0.010f), new Vector3(0.070f, 0.008f, 0.016f), liningMat);
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Lerp(0.096f, 0.160f, i / 5f);
                Block(root, "ZipTooth" + i, new Vector3(x, 0.040f, -0.010f), new Vector3(0.007f, 0.005f, 0.015f), metalHiMat);
            }
            Block(root, "FlyStitch", new Vector3(0.126f, 0.040f, 0.032f), new Vector3(0.082f, 0.006f, 0.006f), stitchMat);
            var flyCurve = Block(root, "FlyCurve", new Vector3(0.090f, 0.040f, 0.022f), new Vector3(0.006f, 0.006f, 0.030f), stitchMat);
            flyCurve.localEulerAngles = new Vector3(0f, -34f, 0f);   // hooks the J-stitch round the bottom of the fly
            Rod(root, "FlyButton", new Vector3(0.192f, 0.040f, 0.030f), Vector3.zero, 0.024f, 0.009f, metalMat);
            Rod(root, "FlyButtonPip", new Vector3(0.192f, 0.047f, 0.030f), Vector3.zero, 0.011f, 0.007f, metalHiMat);

            // ── Slanted hip pockets, one each side, riveted at the corners ─────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var mouth = Block(root, "PocketMouth" + side, new Vector3(0.086f, 0.036f, s * 0.104f),
                    new Vector3(0.090f, 0.010f, 0.018f), liningMat);
                mouth.localEulerAngles = new Vector3(0f, s * 38f, 0f);
                var mouthStitch = Block(root, "PocketStitch" + side, new Vector3(0.092f, 0.044f, s * 0.112f),
                    new Vector3(0.090f, 0.006f, 0.006f), stitchMat);
                mouthStitch.localEulerAngles = new Vector3(0f, s * 38f, 0f);
                Rod(root, "PocketRiveTop" + side, new Vector3(0.121f, 0.042f, s * 0.086f), Vector3.zero, 0.012f, 0.008f, metalMat);
                Rod(root, "PocketRiveLow" + side, new Vector3(0.051f, 0.042f, s * 0.140f), Vector3.zero, 0.012f, 0.008f, metalMat);
            }

            // ── Legs (left = +Z, right = −Z), stubby and flaring a little toward the rolled turn-ups ───────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float z = s * 0.095f;

                Sphere(root, "Leg" + side, new Vector3(-0.055f, 0f, z), new Vector3(0f, -s * 4f, 0f),
                    new Vector3(0.26f, 0.056f, 0.160f), fabricMat);
                Sphere(root, "LegFlare" + side, new Vector3(-0.175f, 0f, s * 0.105f), Vector3.zero,
                    new Vector3(0.120f, 0.052f, 0.175f), fabricMat);

                // Rolled turn-up: a band of the pale inside face capped by the fat fold at the very end.
                Block(root, "Cuff" + side, new Vector3(-0.222f, 0.002f, s * 0.105f), new Vector3(0.040f, 0.060f, 0.185f), fadeMat);
                Rod(root, "CuffRoll" + side, new Vector3(-0.242f, 0.004f, s * 0.105f), new Vector3(90f, 0f, 0f), 0.050f, 0.185f, fadeMat);
                Block(root, "CuffStitch" + side, new Vector3(-0.202f, 0.034f, s * 0.105f), new Vector3(0.006f, 0.006f, 0.175f), stitchMat);
                Block(root, "CuffShade" + side, new Vector3(-0.192f, 0.030f, s * 0.105f), new Vector3(0.008f, 0.006f, 0.170f), fabricDeepMat);

                // Inseam and outseam topstitching down each leg.
                Block(root, "Inseam" + side, new Vector3(-0.120f, 0.032f, s * 0.022f), new Vector3(0.200f, 0.006f, 0.006f), stitchMat);
                var outseam = Block(root, "Outseam" + side, new Vector3(-0.100f, 0.032f, s * 0.170f),
                    new Vector3(0.240f, 0.006f, 0.008f), stitchMat);
                outseam.localEulerAngles = new Vector3(0f, -s * 3f, 0f);
            }

            // Dark valley between the legs, and the curved rise seam running up from it to the fly.
            Block(root, "CrotchGap", new Vector3(-0.150f, -0.006f, 0f), new Vector3(0.180f, 0.020f, 0.035f), liningMat);
            Sphere(root, "CrotchShade", new Vector3(-0.045f, 0.020f, 0f), Vector3.zero, new Vector3(0.09f, 0.030f, 0.06f), fabricDeepMat);
            Block(root, "RiseSeam", new Vector3(0.040f, 0.034f, 0f), new Vector3(0.090f, 0.006f, 0.006f), stitchMat);
            Block(root, "RiseSeamLow", new Vector3(-0.020f, 0.030f, 0f), new Vector3(0.060f, 0.006f, 0.006f), stitchMat);

            // Worn fading over the hips and a couple of soft folds, set off-centre so the slow spin always has
            // something to catch.
            Sphere(root, "FadeA", new Vector3(0.030f, 0.032f, 0.060f), new Vector3(0f, 18f, 0f), new Vector3(0.16f, 0.014f, 0.04f), fadeMat);
            Sphere(root, "FadeB", new Vector3(-0.020f, 0.030f, -0.075f), new Vector3(0f, -14f, 0f), new Vector3(0.13f, 0.012f, 0.035f), fadeMat);
            Sphere(root, "FoldA", new Vector3(-0.090f, 0.032f, 0.100f), new Vector3(0f, 8f, 0f), new Vector3(0.18f, 0.018f, 0.045f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.120f, 0.026f, -0.090f), new Vector3(0f, -10f, 0f), new Vector3(0.16f, 0.012f, 0.030f), fabricDeepMat);

            // Small leather brand tab on the waistband, in its own yawed frame and set to one side.
            var tab = new PartNode("BrandTab");
            tab.SetParent(root, false);
            tab.localPosition = new Vector3(0.190f, 0.038f, -0.086f);
            tab.localEulerAngles = new Vector3(0f, 12f, 0f);
            Block(tab, "TabBase", Vector3.zero, new Vector3(0.030f, 0.008f, 0.046f), patchMat);
            Block(tab, "TabInk", new Vector3(0f, 0.005f, 0f), new Vector3(0.016f, 0.006f, 0.028f), patchInkMat);

            return root;
        }

        /// <summary>
        /// A pair of jeans laid flat with the seat facing up: a waistband with belt loops over a yoked seat,
        /// two arcuate-stitched back patch pockets and a leather brand tab, running out into full-length
        /// tapered legs that finish in plain turned hems. Takes the item's tint as the denim so it reads as
        /// those jeans where it lies; the long legs and the back pockets set them apart from the cuffed shorts.
        /// </summary>
        private static PartNode BuildJeans(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                            // denim
            Color fabricDeep = new Color(fabric.r * 0.76f, fabric.g * 0.76f, fabric.b * 0.76f);  // shaded creases / crotch shadow
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised folds catching light
            Color fade = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.36f);             // sun-worn seat and knee whiskering
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.84f, 0.52f), 0.55f);           // warm topstitching thread
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // pocket bags and the gap between the legs
            Color metal = new Color(0.78f, 0.79f, 0.82f);                                        // bar tacks over the pocket corners
            Color patch = new Color(0.42f, 0.28f, 0.18f);                                        // leather brand tab on the waistband
            Color patchInk = new Color(0.24f, 0.16f, 0.10f);

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.10f);   // matte cotton twill
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.09f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.12f);
            PartMat fadeMat = PartMat.Of(fade, smoothness: 0.12f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat patchMat = PartMat.Of(patch, smoothness: 0.30f);
            PartMat patchInkMat = PartMat.Of(patchInk, smoothness: 0.25f);

            // ── Seat and hips, laid flat with the waist at +X and the legs running out to −X ─────────────
            Sphere(root, "Seat", new Vector3(0.070f, 0f, 0f), Vector3.zero, new Vector3(0.24f, 0.058f, 0.30f), fabricMat);
            Sphere(root, "Hips", new Vector3(0.140f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.056f, 0.29f), fabricMat);
            Sphere(root, "Rise", new Vector3(0.010f, 0.012f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.21f), fabricMat);

            // Waistband across the top with a topstitch along each of its edges.
            Block(root, "Waistband", new Vector3(0.185f, 0.002f, 0f), new Vector3(0.046f, 0.064f, 0.295f), fabricMat);
            Block(root, "WaistStitchTop", new Vector3(0.204f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);
            Block(root, "WaistStitchLow", new Vector3(0.166f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);

            // Five belt loops bridging the band, standing proud of it.
            const int loops = 5;
            for (int i = 0; i < loops; i++)
            {
                float z = Mathf.Lerp(-0.115f, 0.115f, i / (float)(loops - 1));
                Block(root, "BeltLoop" + i, new Vector3(0.185f, 0.036f, z), new Vector3(0.054f, 0.010f, 0.016f), fabricMat);
                Block(root, "BeltLoopBar" + i, new Vector3(0.203f, 0.042f, z), new Vector3(0.008f, 0.006f, 0.014f), stitchMat);
            }

            // ── Yoke: the shallow V seam that shapes the seat, sitting just under the waistband ───────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var yoke = Block(root, "Yoke" + side, new Vector3(0.126f, 0.032f, s * 0.070f),
                    new Vector3(0.140f, 0.007f, 0.007f), stitchMat);
                yoke.localEulerAngles = new Vector3(0f, s * 16f, 0f);   // slopes down and out from the centre back
                var yokeShade = Block(root, "YokeShade" + side, new Vector3(0.120f, 0.028f, s * 0.070f),
                    new Vector3(0.140f, 0.006f, 0.010f), fabricDeepMat);
                yokeShade.localEulerAngles = new Vector3(0f, s * 16f, 0f);
            }

            // Centre-back seam running from the waistband down through the yoke to the crotch.
            Block(root, "BackSeam", new Vector3(0.100f, 0.034f, 0f), new Vector3(0.110f, 0.006f, 0.007f), stitchMat);
            Block(root, "BackSeamLow", new Vector3(0.015f, 0.030f, 0f), new Vector3(0.080f, 0.006f, 0.006f), stitchMat);

            // ── Back patch pockets, outlined in topstitch with the arcuate line across each ───────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float pz = s * 0.086f;

                Block(root, "Pocket" + side, new Vector3(0.046f, 0.034f, pz), new Vector3(0.115f, 0.010f, 0.118f), fabricMat);
                Block(root, "PocketTop" + side, new Vector3(0.102f, 0.040f, pz), new Vector3(0.007f, 0.006f, 0.118f), stitchMat);
                Block(root, "PocketNear" + side, new Vector3(0.046f, 0.040f, pz - s * 0.058f), new Vector3(0.113f, 0.006f, 0.006f), stitchMat);
                Block(root, "PocketFar" + side, new Vector3(0.046f, 0.040f, pz + s * 0.058f), new Vector3(0.113f, 0.006f, 0.006f), stitchMat);
                // Pointed bottom corner, so the pocket reads as the classic five-pocket shape rather than a square.
                var point = Block(root, "PocketPoint" + side, new Vector3(-0.008f, 0.040f, pz), new Vector3(0.007f, 0.006f, 0.070f), stitchMat);
                point.localEulerAngles = new Vector3(0f, 34f, 0f);

                // Arcuate: two shallow lines meeting across the pocket face.
                var arcA = Block(root, "PocketArcA" + side, new Vector3(0.056f, 0.041f, pz - 0.028f),
                    new Vector3(0.006f, 0.006f, 0.062f), stitchMat);
                arcA.localEulerAngles = new Vector3(0f, 16f, 0f);
                var arcB = Block(root, "PocketArcB" + side, new Vector3(0.056f, 0.041f, pz + 0.028f),
                    new Vector3(0.006f, 0.006f, 0.062f), stitchMat);
                arcB.localEulerAngles = new Vector3(0f, -16f, 0f);

                // Bar tacks pinning the top corners of the pocket mouth.
                Block(root, "PocketTackNear" + side, new Vector3(0.100f, 0.043f, pz - 0.050f), new Vector3(0.012f, 0.006f, 0.008f), metalMat);
                Block(root, "PocketTackFar" + side, new Vector3(0.100f, 0.043f, pz + 0.050f), new Vector3(0.012f, 0.006f, 0.008f), metalMat);
            }

            // ── Full-length legs (left = +Z, right = −Z), tapering from the seat down to a plain hem ──────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";

                Sphere(root, "Thigh" + side, new Vector3(-0.070f, 0f, s * 0.094f), new Vector3(0f, -s * 3f, 0f),
                    new Vector3(0.22f, 0.056f, 0.172f), fabricMat);
                Sphere(root, "Knee" + side, new Vector3(-0.205f, 0f, s * 0.100f), Vector3.zero,
                    new Vector3(0.17f, 0.052f, 0.156f), fabricMat);
                Sphere(root, "Calf" + side, new Vector3(-0.315f, 0f, s * 0.103f), Vector3.zero,
                    new Vector3(0.16f, 0.048f, 0.146f), fabricMat);
                Sphere(root, "Ankle" + side, new Vector3(-0.398f, 0f, s * 0.104f), Vector3.zero,
                    new Vector3(0.10f, 0.046f, 0.140f), fabricMat);

                // Plain turned hem: a band of doubled denim closed by a single topstitch, no roll.
                Block(root, "Hem" + side, new Vector3(-0.428f, 0f, s * 0.104f), new Vector3(0.030f, 0.052f, 0.142f), fabricMat);
                Block(root, "HemStitch" + side, new Vector3(-0.416f, 0.030f, s * 0.104f), new Vector3(0.006f, 0.006f, 0.136f), stitchMat);
                Block(root, "HemShade" + side, new Vector3(-0.443f, 0.024f, s * 0.104f), new Vector3(0.006f, 0.006f, 0.138f), fabricDeepMat);

                // Inseam and outseam topstitching down the full length of each leg.
                Block(root, "Inseam" + side, new Vector3(-0.260f, 0.030f, s * 0.026f), new Vector3(0.340f, 0.006f, 0.006f), stitchMat);
                var outseam = Block(root, "Outseam" + side, new Vector3(-0.240f, 0.030f, s * 0.172f),
                    new Vector3(0.390f, 0.006f, 0.008f), stitchMat);
                outseam.localEulerAngles = new Vector3(0f, -s * 2f, 0f);

                // Whiskering worn into the knee, and a soft fold further down the shin.
                Sphere(root, "Whisker" + side, new Vector3(-0.200f, 0.030f, s * 0.100f), new Vector3(0f, s * 6f, 0f),
                    new Vector3(0.05f, 0.012f, 0.12f), fadeMat);
                Sphere(root, "ShinFold" + side, new Vector3(-0.300f, 0.028f, s * 0.115f), new Vector3(0f, -s * 8f, 0f),
                    new Vector3(0.13f, 0.012f, 0.04f), fabricHiMat);
            }

            // Dark valley running the length of the gap between the legs.
            Block(root, "CrotchGap", new Vector3(-0.270f, -0.006f, 0f), new Vector3(0.350f, 0.020f, 0.036f), liningMat);
            Sphere(root, "CrotchShade", new Vector3(-0.070f, 0.020f, 0f), Vector3.zero, new Vector3(0.10f, 0.030f, 0.06f), fabricDeepMat);

            // Worn fading over the seat and a couple of soft creases, set off-centre so the slow spin always
            // has something to catch.
            Sphere(root, "SeatFade", new Vector3(0.055f, 0.032f, 0.008f), new Vector3(0f, 10f, 0f), new Vector3(0.13f, 0.014f, 0.09f), fadeMat);
            Sphere(root, "FoldA", new Vector3(-0.130f, 0.032f, 0.108f), new Vector3(0f, 8f, 0f), new Vector3(0.19f, 0.018f, 0.045f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.160f, 0.026f, -0.098f), new Vector3(0f, -10f, 0f), new Vector3(0.17f, 0.012f, 0.032f), fabricDeepMat);

            // Leather brand tab riveted to the back of the waistband, in its own yawed frame and set to one side.
            var tab = new PartNode("BrandTab");
            tab.SetParent(root, false);
            tab.localPosition = new Vector3(0.186f, 0.038f, -0.086f);
            tab.localEulerAngles = new Vector3(0f, 10f, 0f);
            Block(tab, "TabBase", Vector3.zero, new Vector3(0.034f, 0.008f, 0.052f), patchMat);
            Block(tab, "TabInk", new Vector3(0f, 0.005f, 0f), new Vector3(0.018f, 0.006f, 0.032f), patchInkMat);

            return root;
        }

        /// <summary>
        /// A pair of work trousers laid flat, front face-up: a waistband closed by an extended tab and
        /// button over a zip fly, plain vertical welt pockets down the sides, a flapped utility pocket and
        /// a hammer loop on the thighs, and full-length legs pressed with a centre crease that runs down to
        /// a plain hem. Takes the item's tint as the twill so it reads as those trousers where it lies; the
        /// pressed creases, tonal stitching and workwear pockets set them apart from the riveted jeans.
        /// </summary>
        private static PartNode BuildTrousers(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                            // cotton work twill
            Color fabricDeep = new Color(fabric.r * 0.74f, fabric.g * 0.74f, fabric.b * 0.74f);  // shaded folds and the gap between the legs
            Color fabricHi = Color.Lerp(fabric, new Color(0.96f, 0.94f, 0.88f), 0.22f);          // raised folds catching light
            Color pressed = Color.Lerp(fabric, new Color(0.96f, 0.94f, 0.88f), 0.34f);          // sheen along the pressed centre creases
            Color stitch = Color.Lerp(fabric, new Color(0.94f, 0.92f, 0.86f), 0.32f);           // tonal thread, not the gold of denim
            Color lining = new Color(fabric.r * 0.40f, fabric.g * 0.40f, fabric.b * 0.40f);      // pocket bags and the valley between the legs
            Color metal = new Color(0.72f, 0.73f, 0.76f);                                        // waist button and zip teeth
            Color metalHi = new Color(0.87f, 0.88f, 0.91f);
            Color label = new Color(0.88f, 0.86f, 0.78f);                                        // woven care label at the waist
            Color labelInk = new Color(0.22f, 0.23f, 0.26f);

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.11f);   // matte cotton twill
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.09f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.13f);
            PartMat pressedMat = PartMat.Of(pressed, smoothness: 0.20f);  // a pressed crease takes a little more light
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat metalHiMat = PartMat.Of(metalHi, smoothness: 0.62f, metallic: 0.9f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.18f);
            PartMat labelInkMat = PartMat.Of(labelInk, smoothness: 0.16f);

            // ── Front of the seat and hips, laid flat with the waist at +X and the legs running out to −X ──
            Sphere(root, "Front", new Vector3(0.072f, 0f, 0f), Vector3.zero, new Vector3(0.24f, 0.058f, 0.30f), fabricMat);
            Sphere(root, "Hips", new Vector3(0.140f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.056f, 0.29f), fabricMat);
            Sphere(root, "Rise", new Vector3(0.010f, 0.012f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.21f), fabricMat);

            // Waistband across the top, topstitched along both edges.
            Block(root, "Waistband", new Vector3(0.185f, 0.002f, 0f), new Vector3(0.048f, 0.064f, 0.295f), fabricMat);
            Block(root, "WaistStitchTop", new Vector3(0.205f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);
            Block(root, "WaistStitchLow", new Vector3(0.165f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.285f), stitchMat);

            // Six belt loops bridging the band, standing proud of it.
            const int loops = 6;
            for (int i = 0; i < loops; i++)
            {
                float z = Mathf.Lerp(-0.120f, 0.120f, i / (float)(loops - 1));
                Block(root, "BeltLoop" + i, new Vector3(0.185f, 0.036f, z), new Vector3(0.056f, 0.010f, 0.015f), fabricMat);
                Block(root, "BeltLoopBar" + i, new Vector3(0.204f, 0.042f, z), new Vector3(0.008f, 0.006f, 0.013f), stitchMat);
            }

            // ── Extended tab waist closure over a zip fly: the workwear alternative to the jeans shank button ──
            Block(root, "WaistTab", new Vector3(0.186f, 0.036f, 0.052f), new Vector3(0.052f, 0.011f, 0.086f), fabricMat);
            Block(root, "WaistTabStitch", new Vector3(0.186f, 0.043f, 0.088f), new Vector3(0.046f, 0.006f, 0.007f), stitchMat);
            Rod(root, "WaistButton", new Vector3(0.186f, 0.045f, 0.030f), Vector3.zero, 0.022f, 0.008f, metalMat);
            Rod(root, "WaistButtonPip", new Vector3(0.186f, 0.051f, 0.030f), Vector3.zero, 0.010f, 0.006f, metalHiMat);

            Block(root, "Placket", new Vector3(0.120f, 0.032f, 0.020f), new Vector3(0.104f, 0.012f, 0.048f), fabricMat);
            Block(root, "ZipTape", new Vector3(0.120f, 0.034f, -0.012f), new Vector3(0.090f, 0.008f, 0.018f), liningMat);
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(0.078f, 0.162f, i / 6f);
                Block(root, "ZipTooth" + i, new Vector3(x, 0.040f, -0.012f), new Vector3(0.007f, 0.005f, 0.016f), metalHiMat);
            }
            Block(root, "ZipPull", new Vector3(0.150f, 0.044f, -0.012f), new Vector3(0.020f, 0.007f, 0.024f), metalMat);
            Block(root, "FlyStitch", new Vector3(0.118f, 0.040f, 0.043f), new Vector3(0.106f, 0.006f, 0.006f), stitchMat);
            var flyCurve = Block(root, "FlyCurve", new Vector3(0.072f, 0.040f, 0.030f), new Vector3(0.006f, 0.006f, 0.034f), stitchMat);
            flyCurve.localEulerAngles = new Vector3(0f, -38f, 0f);   // hooks the J-stitch round the bottom of the fly

            // ── Plain vertical welt pockets down the side seams, no rivets and no denim slant ─────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float pz = s * 0.132f;

                Block(root, "PocketMouth" + side, new Vector3(0.078f, 0.034f, pz), new Vector3(0.110f, 0.010f, 0.016f), liningMat);
                Block(root, "PocketWelt" + side, new Vector3(0.078f, 0.040f, pz - s * 0.012f), new Vector3(0.110f, 0.007f, 0.010f), fabricMat);
                Block(root, "PocketStitch" + side, new Vector3(0.078f, 0.045f, pz - s * 0.012f), new Vector3(0.106f, 0.005f, 0.005f), stitchMat);
                Block(root, "PocketBarTop" + side, new Vector3(0.130f, 0.043f, pz), new Vector3(0.010f, 0.006f, 0.020f), stitchMat);
                Block(root, "PocketBarLow" + side, new Vector3(0.026f, 0.043f, pz), new Vector3(0.010f, 0.006f, 0.020f), stitchMat);
            }

            // ── Flapped utility pocket on one thigh, and a hammer loop on the other: the workwear tell ────
            Block(root, "ThighPocket", new Vector3(-0.120f, 0.032f, 0.108f), new Vector3(0.120f, 0.011f, 0.116f), fabricMat);
            Block(root, "ThighPocketNear", new Vector3(-0.120f, 0.039f, 0.052f), new Vector3(0.118f, 0.006f, 0.006f), stitchMat);
            Block(root, "ThighPocketFar", new Vector3(-0.120f, 0.039f, 0.164f), new Vector3(0.118f, 0.006f, 0.006f), stitchMat);
            Block(root, "ThighPocketLow", new Vector3(-0.178f, 0.039f, 0.108f), new Vector3(0.006f, 0.006f, 0.118f), stitchMat);
            Block(root, "ThighFlap", new Vector3(-0.070f, 0.042f, 0.108f), new Vector3(0.048f, 0.010f, 0.120f), fabricHiMat);
            Block(root, "ThighFlapEdge", new Vector3(-0.092f, 0.048f, 0.108f), new Vector3(0.006f, 0.006f, 0.116f), stitchMat);
            Rod(root, "ThighFlapSnap", new Vector3(-0.086f, 0.049f, 0.108f), Vector3.zero, 0.014f, 0.008f, metalMat);

            var loop = new PartNode("HammerLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(-0.086f, 0.034f, -0.150f);
            loop.localEulerAngles = new Vector3(0f, 8f, 0f);
            Block(loop, "LoopStrap", Vector3.zero, new Vector3(0.016f, 0.010f, 0.074f), fabricMat);
            Block(loop, "LoopArch", new Vector3(0f, 0.010f, 0.030f), new Vector3(0.014f, 0.014f, 0.016f), fabricMat);
            Block(loop, "LoopTackNear", new Vector3(0f, 0.008f, -0.030f), new Vector3(0.014f, 0.006f, 0.010f), stitchMat);
            Block(loop, "LoopTackFar", new Vector3(0f, 0.008f, 0.056f), new Vector3(0.014f, 0.006f, 0.010f), stitchMat);

            // ── Full-length legs (left = +Z, right = −Z), straight cut and pressed down the centre ────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";

                Sphere(root, "Thigh" + side, new Vector3(-0.075f, 0f, s * 0.096f), new Vector3(0f, -s * 3f, 0f),
                    new Vector3(0.23f, 0.056f, 0.176f), fabricMat);
                Sphere(root, "Knee" + side, new Vector3(-0.215f, 0f, s * 0.104f), Vector3.zero,
                    new Vector3(0.18f, 0.052f, 0.166f), fabricMat);
                Sphere(root, "Calf" + side, new Vector3(-0.330f, 0f, s * 0.108f), Vector3.zero,
                    new Vector3(0.17f, 0.050f, 0.160f), fabricMat);
                Sphere(root, "Ankle" + side, new Vector3(-0.416f, 0f, s * 0.110f), Vector3.zero,
                    new Vector3(0.11f, 0.048f, 0.156f), fabricMat);

                // Pressed centre crease running the length of the leg: the line that says trousers, not jeans.
                Block(root, "Crease" + side, new Vector3(-0.240f, 0.032f, s * 0.104f), new Vector3(0.390f, 0.007f, 0.012f), pressedMat);
                Block(root, "CreaseShade" + side, new Vector3(-0.240f, 0.028f, s * 0.118f), new Vector3(0.380f, 0.006f, 0.008f), fabricDeepMat);

                // Plain turned hem, closed by a single tonal topstitch.
                Block(root, "Hem" + side, new Vector3(-0.448f, 0f, s * 0.110f), new Vector3(0.032f, 0.054f, 0.158f), fabricMat);
                Block(root, "HemStitch" + side, new Vector3(-0.434f, 0.031f, s * 0.110f), new Vector3(0.006f, 0.006f, 0.152f), stitchMat);
                Block(root, "HemShade" + side, new Vector3(-0.464f, 0.024f, s * 0.110f), new Vector3(0.006f, 0.006f, 0.154f), fabricDeepMat);

                // Inseam and outseam running the full length of each leg.
                Block(root, "Inseam" + side, new Vector3(-0.270f, 0.030f, s * 0.026f), new Vector3(0.360f, 0.006f, 0.006f), stitchMat);
                var outseam = Block(root, "Outseam" + side, new Vector3(-0.250f, 0.030f, s * 0.180f),
                    new Vector3(0.400f, 0.006f, 0.008f), stitchMat);
                outseam.localEulerAngles = new Vector3(0f, -s * 2f, 0f);

                // Reinforced knee panel: a second layer of twill topstitched over the knee.
                Block(root, "KneePanel" + side, new Vector3(-0.215f, 0.032f, s * 0.104f), new Vector3(0.090f, 0.008f, 0.150f), fabricHiMat);
                Block(root, "KneePanelTop" + side, new Vector3(-0.172f, 0.038f, s * 0.104f), new Vector3(0.006f, 0.006f, 0.146f), stitchMat);
                Block(root, "KneePanelLow" + side, new Vector3(-0.258f, 0.038f, s * 0.104f), new Vector3(0.006f, 0.006f, 0.146f), stitchMat);
            }

            // Dark valley running the length of the gap between the legs.
            Block(root, "CrotchGap", new Vector3(-0.280f, -0.006f, 0f), new Vector3(0.360f, 0.020f, 0.036f), liningMat);
            Sphere(root, "CrotchShade", new Vector3(-0.070f, 0.020f, 0f), Vector3.zero, new Vector3(0.10f, 0.030f, 0.06f), fabricDeepMat);

            // Soft folds set off-centre so the slow spin always has something to catch.
            Sphere(root, "FoldA", new Vector3(-0.150f, 0.032f, -0.108f), new Vector3(0f, -8f, 0f), new Vector3(0.19f, 0.018f, 0.042f), fabricHiMat);
            Sphere(root, "CreaseB", new Vector3(-0.320f, 0.028f, 0.118f), new Vector3(0f, 10f, 0f), new Vector3(0.16f, 0.012f, 0.030f), fabricDeepMat);
            Sphere(root, "Scuff", new Vector3(-0.360f, 0.030f, -0.100f), new Vector3(0f, -12f, 0f), new Vector3(0.10f, 0.012f, 0.036f), pressedMat);

            // Woven care label stitched inside the back of the waistband, in its own yawed frame.
            var tag = new PartNode("CareLabel");
            tag.SetParent(root, false);
            tag.localPosition = new Vector3(0.172f, 0.038f, -0.092f);
            tag.localEulerAngles = new Vector3(0f, -12f, 0f);
            Block(tag, "TagBase", Vector3.zero, new Vector3(0.030f, 0.007f, 0.044f), labelMat);
            Block(tag, "TagInkTop", new Vector3(0.006f, 0.005f, 0f), new Vector3(0.005f, 0.005f, 0.028f), labelInkMat);
            Block(tag, "TagInkLow", new Vector3(-0.004f, 0.005f, 0f), new Vector3(0.005f, 0.005f, 0.022f), labelInkMat);

            return root;
        }

        /// <summary>
        /// A pair of low-top canvas trainers standing sole-down side by side, kicked off at slightly
        /// different angles so the slow spin always shows a change of profile. The item's tint paints the
        /// canvas upper over a white foam midsole and a dark rubber outsole, with a coloured side flash,
        /// laced eyelets and a padded collar so the pair reads as footwear rather than a folded garment.
        /// </summary>
        private static PartNode BuildSneakers(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hovers high enough that the bob never dips the soles through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color canvas = def.Tint;                                                             // canvas upper
            Color canvasDeep = new Color(canvas.r * 0.76f, canvas.g * 0.76f, canvas.b * 0.76f);   // shaded panels and seams
            Color canvasHi = Color.Lerp(canvas, new Color(0.98f, 0.97f, 0.95f), 0.30f);           // tongue and raised panels
            Color foam = Color.Lerp(canvas, new Color(0.98f, 0.97f, 0.94f), 0.62f);               // white foam midsole and toe cap
            Color foamShade = new Color(foam.r * 0.86f, foam.g * 0.86f, foam.b * 0.86f);
            Color rubber = new Color(0.19f, 0.19f, 0.21f);                                        // dark outsole
            Color rubberHi = new Color(0.30f, 0.30f, 0.33f);                                      // tread lugs catching light
            Color accent = new Color(0.20f, 0.42f, 0.72f);                                        // side flash, heel tab, tongue badge
            Color lace = new Color(0.94f, 0.93f, 0.89f);                                          // cotton laces
            Color lining = new Color(0.13f, 0.13f, 0.15f);                                        // dark inside the shoe
            Color metal = new Color(0.72f, 0.73f, 0.76f);                                         // eyelet rings
            Color stitch = Color.Lerp(canvas, new Color(0.95f, 0.93f, 0.87f), 0.45f);             // topstitching

            PartMat canvasMat = PartMat.Of(canvas, smoothness: 0.12f);
            PartMat canvasDeepMat = PartMat.Of(canvasDeep, smoothness: 0.10f);
            PartMat canvasHiMat = PartMat.Of(canvasHi, smoothness: 0.14f);
            PartMat foamMat = PartMat.Of(foam, smoothness: 0.18f);
            PartMat foamShadeMat = PartMat.Of(foamShade, smoothness: 0.15f);
            PartMat rubberMat = PartMat.Of(rubber, smoothness: 0.34f);
            PartMat rubberHiMat = PartMat.Of(rubberHi, smoothness: 0.30f);
            PartMat accentMat = PartMat.Of(accent, smoothness: 0.22f);
            PartMat laceMat = PartMat.Of(lace, smoothness: 0.13f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.08f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.60f, metallic: 0.85f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);

            // Both shoes are built standing on their own local zero, so drop the pair to centre the mass on the root.
            const float ground = -0.055f;

            BuildSneaker(root, new Vector3(0.006f, ground, 0.074f), -7f,
                canvasMat, canvasDeepMat, canvasHiMat, foamMat, foamShadeMat, rubberMat, rubberHiMat,
                accentMat, laceMat, liningMat, metalMat, stitchMat);
            BuildSneaker(root, new Vector3(-0.016f, ground, -0.076f), 14f,
                canvasMat, canvasDeepMat, canvasHiMat, foamMat, foamShadeMat, rubberMat, rubberHiMat,
                accentMat, laceMat, liningMat, metalMat, stitchMat);

            return root;
        }

        /// <summary>Builds one low-top trainer under <paramref name="parent"/> in its own yaw frame: it
        /// stands on the frame's local zero with the toe at +X and the heel at −X, so callers place a pair
        /// side by side and yaw each one a little. Rubber outsole and foam midsole carry a canvas upper with
        /// a rubber-edged toe cap, a side flash, laced eyelets over a tongue and a padded collar ring round
        /// the dark opening.</summary>
        private static void BuildSneaker(PartNode parent, Vector3 localPos, float yawDeg,
            PartMat canvasMat, PartMat canvasDeepMat, PartMat canvasHiMat, PartMat foamMat, PartMat foamShadeMat,
            PartMat rubberMat, PartMat rubberHiMat, PartMat accentMat, PartMat laceMat, PartMat liningMat,
            PartMat metalMat, PartMat stitchMat)
        {
            var shoe = new PartNode("Sneaker");
            shoe.SetParent(parent, false);
            shoe.localPosition = localPos;
            shoe.localEulerAngles = new Vector3(0f, yawDeg, 0f);

            const float halfWidth = 0.053f;   // canvas half-width, so side detail can sit proud of it
            const float laceY = 0.108f;       // the plane the laces cross over

            // ── Rubber outsole, rounded off at both ends and grooved with tread that shows on the sidewall ──
            Block(shoe, "Outsole", new Vector3(0.005f, 0.012f, 0f), new Vector3(0.280f, 0.024f, 0.106f), rubberMat);
            Sphere(shoe, "OutsoleToe", new Vector3(0.145f, 0.012f, 0f), Vector3.zero, new Vector3(0.090f, 0.026f, 0.104f), rubberMat);
            Sphere(shoe, "OutsoleHeel", new Vector3(-0.132f, 0.012f, 0f), Vector3.zero, new Vector3(0.078f, 0.026f, 0.102f), rubberMat);
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(-0.126f, 0.148f, i / 6f);
                Block(shoe, "Tread" + i, new Vector3(x, 0.008f, 0f), new Vector3(0.013f, 0.016f, 0.112f), rubberHiMat);
            }

            // ── Foam midsole sitting on the outsole, with a shaded lip and a pinstripe down each sidewall ──
            Block(shoe, "MidsoleLip", new Vector3(0.005f, 0.026f, 0f), new Vector3(0.284f, 0.006f, 0.113f), foamShadeMat);
            Block(shoe, "Midsole", new Vector3(0.005f, 0.038f, 0f), new Vector3(0.284f, 0.028f, 0.110f), foamMat);
            Sphere(shoe, "MidsoleToe", new Vector3(0.148f, 0.036f, 0f), Vector3.zero, new Vector3(0.092f, 0.030f, 0.108f), foamMat);
            Sphere(shoe, "MidsoleHeel", new Vector3(-0.134f, 0.038f, 0f), Vector3.zero, new Vector3(0.080f, 0.032f, 0.106f), foamMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Block(shoe, "MidStripe" + side, new Vector3(0.000f, 0.041f, s * 0.056f), new Vector3(0.230f, 0.007f, 0.007f), accentMat);
            }

            // ── Canvas upper: vamp over the foot, quarters round the ankle, counter at the heel ────────────
            Sphere(shoe, "Vamp", new Vector3(0.070f, 0.052f, 0f), Vector3.zero, new Vector3(0.200f, 0.066f, 0.104f), canvasMat);
            Sphere(shoe, "Quarter", new Vector3(-0.050f, 0.055f, 0f), Vector3.zero, new Vector3(0.200f, 0.082f, 0.106f), canvasMat);
            Sphere(shoe, "HeelCounter", new Vector3(-0.128f, 0.052f, 0f), Vector3.zero, new Vector3(0.086f, 0.078f, 0.100f), canvasMat);
            Block(shoe, "ThroatSeam", new Vector3(0.078f, 0.084f, 0f), new Vector3(0.120f, 0.006f, 0.006f), stitchMat);
            Sphere(shoe, "QuarterSeam", new Vector3(-0.096f, 0.062f, 0f), Vector3.zero, new Vector3(0.014f, 0.070f, 0.104f), canvasDeepMat);

            // Rubber-edged toe cap, closed off by a stitched seam where it meets the canvas.
            Sphere(shoe, "ToeCap", new Vector3(0.146f, 0.048f, 0f), Vector3.zero, new Vector3(0.088f, 0.052f, 0.106f), foamMat);
            Sphere(shoe, "ToeCapEdge", new Vector3(0.152f, 0.038f, 0f), Vector3.zero, new Vector3(0.082f, 0.030f, 0.109f), foamShadeMat);
            Block(shoe, "ToeCapSeam", new Vector3(0.106f, 0.058f, 0f), new Vector3(0.006f, 0.030f, 0.104f), stitchMat);

            // Punched vent holes on top of the vamp.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(shoe, "Vent" + side, new Vector3(0.092f, 0.080f, s * 0.019f), Vector3.zero, new Vector3(0.013f, 0.008f, 0.013f), liningMat);
                Sphere(shoe, "VentBack" + side, new Vector3(0.070f, 0.082f, s * 0.021f), Vector3.zero, new Vector3(0.013f, 0.008f, 0.013f), liningMat);
            }

            // ── Coloured side flash sweeping up from the midsole toward the collar on both sides ───────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float z = s * (halfWidth + 0.003f);
                Sphere(shoe, "Flash0" + side, new Vector3(0.062f, 0.056f, z), new Vector3(0f, 0f, 10f), new Vector3(0.078f, 0.022f, 0.010f), accentMat);
                Sphere(shoe, "Flash1" + side, new Vector3(0.004f, 0.068f, z), new Vector3(0f, 0f, 16f), new Vector3(0.078f, 0.024f, 0.010f), accentMat);
                Sphere(shoe, "Flash2" + side, new Vector3(-0.050f, 0.082f, z), new Vector3(0f, 0f, 22f), new Vector3(0.062f, 0.022f, 0.010f), accentMat);
            }

            // ── Padded collar ring round the opening, with the dark inside of the shoe showing through ─────
            Sphere(shoe, "Lining", new Vector3(-0.062f, 0.086f, 0f), Vector3.zero, new Vector3(0.118f, 0.024f, 0.074f), liningMat);
            const int collarSegments = 16;
            for (int i = 0; i < collarSegments; i++)
            {
                float deg = i * 360f / collarSegments;
                float rad = deg * Mathf.Deg2Rad;
                Vector3 p = new Vector3(-0.062f + 0.066f * Mathf.Cos(rad), 0.104f, 0.040f * Mathf.Sin(rad));
                var pad = Block(shoe, "Collar" + i, p, new Vector3(0.022f, 0.016f, 0.026f), i % 2 == 0 ? canvasMat : canvasHiMat);
                pad.localEulerAngles = new Vector3(0f, -deg, 0f);   // faces the padding outward round the opening
            }

            // Pull tab at the back of the collar.
            Block(shoe, "HeelTab", new Vector3(-0.140f, 0.106f, 0f), new Vector3(0.016f, 0.030f, 0.042f), accentMat);
            Block(shoe, "HeelTabStitch", new Vector3(-0.134f, 0.118f, 0f), new Vector3(0.006f, 0.006f, 0.034f), stitchMat);

            // ── Tongue sloping down toward the toe, badged near the collar ─────────────────────────────────
            var tongue = Block(shoe, "Tongue", new Vector3(0.020f, 0.094f, 0f), new Vector3(0.092f, 0.014f, 0.060f), canvasHiMat);
            tongue.localEulerAngles = new Vector3(0f, 0f, -12f);
            Block(shoe, "TongueBadge", new Vector3(-0.006f, 0.106f, 0f), new Vector3(0.026f, 0.006f, 0.030f), accentMat);

            // ── Eyelets in two rows, laced across in a criss-cross and tied off at the top ─────────────────
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(0.058f, -0.029f, i / 3f);
                for (int s = -1; s <= 1; s += 2)
                {
                    string side = s > 0 ? "L" : "R";
                    Rod(shoe, "Eyelet" + i + side, new Vector3(x, 0.100f, s * 0.033f), Vector3.zero, 0.013f, 0.006f, metalMat);
                    Rod(shoe, "EyeletHole" + i + side, new Vector3(x, 0.104f, s * 0.033f), Vector3.zero, 0.007f, 0.004f, liningMat);
                }
            }

            // Lays one lace between two eyelets, yawed to run from the first to the second.
            void Lace(string name, float x0, float z0, float x1, float z1)
            {
                Vector3 mid = new Vector3((x0 + x1) * 0.5f, laceY, (z0 + z1) * 0.5f);
                float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
                float yaw = Mathf.Atan2(-(z1 - z0), x1 - x0) * Mathf.Rad2Deg;
                var l = Block(shoe, name, mid, new Vector3(len, 0.007f, 0.014f), laceMat);
                l.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            Block(shoe, "LaceBar", new Vector3(0.058f, laceY, 0f), new Vector3(0.011f, 0.007f, 0.068f), laceMat);
            for (int i = 0; i < 3; i++)
            {
                float x0 = Mathf.Lerp(0.058f, -0.029f, i / 3f);
                float x1 = Mathf.Lerp(0.058f, -0.029f, (i + 1) / 3f);
                Lace("LaceCrossL" + i, x0, 0.033f, x1, -0.033f);
                Lace("LaceCrossR" + i, x0, -0.033f, x1, 0.033f);
            }

            // Knot and bow sitting over the top eyelets, with the ends trailing back over the collar.
            Sphere(shoe, "LaceKnot", new Vector3(-0.038f, laceY + 0.006f, 0f), Vector3.zero, new Vector3(0.028f, 0.020f, 0.026f), laceMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(shoe, "BowLoop" + side, new Vector3(-0.056f, laceY + 0.008f, s * 0.030f), new Vector3(0f, s * 24f, 0f),
                    new Vector3(0.048f, 0.014f, 0.032f), laceMat);
                var tail = Block(shoe, "LaceTail" + side, new Vector3(-0.070f, laceY + 0.002f, s * 0.044f),
                    new Vector3(0.058f, 0.006f, 0.012f), laceMat);
                tail.localEulerAngles = new Vector3(0f, -s * 34f, 0f);
            }
        }

        /// <summary>
        /// A pair of leather work boots standing sole-down side by side, yawed apart so the slow spin keeps
        /// showing a fresh profile. The item's tint paints the leather over a lugged rubber outsole with a
        /// stacked heel and a proud stitched welt; the tall laced shaft, speed hooks and back pull loop are
        /// what separate these from the low canvas trainers.
        /// </summary>
        private static PartNode BuildBoots(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Tall boots hover higher than the trainers so the bob never sinks the lugs through the floor.
            baseHeight = 0.18f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color leather = def.Tint;                                                               // oiled leather upper
            Color leatherDeep = new Color(leather.r * 0.70f, leather.g * 0.70f, leather.b * 0.70f);  // heel counter and shaded panels
            Color leatherHi = Color.Lerp(leather, new Color(0.86f, 0.74f, 0.58f), 0.26f);            // toe cap and worn highlights
            Color welt = Color.Lerp(leather, new Color(0.82f, 0.68f, 0.44f), 0.45f);                 // tan stitched welt round the sole
            Color rubber = new Color(0.15f, 0.15f, 0.16f);                                           // outsole and stacked heel
            Color rubberDeep = new Color(0.10f, 0.10f, 0.11f);                                       // heel breast and grooves
            Color rubberHi = new Color(0.26f, 0.26f, 0.28f);                                         // tread lugs catching light
            Color lace = new Color(0.55f, 0.44f, 0.26f);                                             // waxed cotton laces
            Color lining = new Color(0.12f, 0.11f, 0.10f);                                           // dark inside the shaft
            Color metal = new Color(0.68f, 0.60f, 0.36f);                                            // brass eyelets and speed hooks
            Color stitch = Color.Lerp(leather, new Color(0.95f, 0.90f, 0.72f), 0.55f);               // heavy contrast stitching

            PartMat leatherMat = PartMat.Of(leather, smoothness: 0.34f);
            PartMat leatherDeepMat = PartMat.Of(leatherDeep, smoothness: 0.28f);
            PartMat leatherHiMat = PartMat.Of(leatherHi, smoothness: 0.40f);
            PartMat weltMat = PartMat.Of(welt, smoothness: 0.22f);
            PartMat rubberMat = PartMat.Of(rubber, smoothness: 0.26f);
            PartMat rubberDeepMat = PartMat.Of(rubberDeep, smoothness: 0.22f);
            PartMat rubberHiMat = PartMat.Of(rubberHi, smoothness: 0.30f);
            PartMat laceMat = PartMat.Of(lace, smoothness: 0.20f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.08f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.62f, metallic: 0.85f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.16f);

            // Each boot stands on its own local zero, so drop the pair to balance the shafts over the root.
            const float ground = -0.10f;

            BuildBoot(root, new Vector3(0.008f, ground, 0.072f), -9f,
                leatherMat, leatherDeepMat, leatherHiMat, weltMat, rubberMat, rubberDeepMat, rubberHiMat,
                laceMat, liningMat, metalMat, stitchMat);
            BuildBoot(root, new Vector3(-0.014f, ground, -0.074f), 15f,
                leatherMat, leatherDeepMat, leatherHiMat, weltMat, rubberMat, rubberDeepMat, rubberHiMat,
                laceMat, liningMat, metalMat, stitchMat);

            return root;
        }

        /// <summary>Builds one leather work boot under <paramref name="parent"/> in its own yaw frame: it
        /// stands on the frame's local zero with the toe at +X and the heel at −X, so callers stand a pair
        /// side by side and yaw each one a little. A lugged outsole and stacked heel carry a welted leather
        /// upper whose shaft rises past the ankle, laced through brass eyelets and speed hooks over a
        /// gusseted tongue, with a padded collar round the dark opening and a pull loop at the back.</summary>
        private static void BuildBoot(PartNode parent, Vector3 localPos, float yawDeg,
            PartMat leatherMat, PartMat leatherDeepMat, PartMat leatherHiMat, PartMat weltMat,
            PartMat rubberMat, PartMat rubberDeepMat, PartMat rubberHiMat, PartMat laceMat,
            PartMat liningMat, PartMat metalMat, PartMat stitchMat)
        {
            var boot = new PartNode("Boot");
            boot.SetParent(parent, false);
            boot.localPosition = localPos;
            boot.localEulerAngles = new Vector3(0f, yawDeg, 0f);

            const float halfWidth = 0.053f;   // upper's half-width, so side detail can sit proud of it
            const float shaftX = -0.045f;     // the shaft (and its collar) rises over the ankle, behind the laces

            // ── Lugged rubber outsole with a stacked heel block at the back ────────────────────────────────
            Block(boot, "Outsole", new Vector3(0.005f, 0.014f, 0f), new Vector3(0.286f, 0.028f, 0.112f), rubberMat);
            Sphere(boot, "OutsoleToe", new Vector3(0.150f, 0.014f, 0f), Vector3.zero, new Vector3(0.096f, 0.030f, 0.110f), rubberMat);
            Block(boot, "Heel", new Vector3(-0.114f, 0.024f, 0f), new Vector3(0.092f, 0.048f, 0.112f), rubberMat);
            Block(boot, "HeelBreast", new Vector3(-0.070f, 0.020f, 0f), new Vector3(0.012f, 0.040f, 0.110f), rubberDeepMat);
            Block(boot, "HeelCap", new Vector3(-0.118f, 0.006f, 0f), new Vector3(0.086f, 0.014f, 0.116f), rubberHiMat);
            for (int i = 0; i < 8; i++)
            {
                float x = Mathf.Lerp(-0.140f, 0.152f, i / 7f);
                Block(boot, "Lug" + i, new Vector3(x, 0.006f, 0f), new Vector3(0.018f, 0.014f, 0.118f), rubberHiMat);
            }

            // ── Tan welt standing proud all the way round the sole, hand-stitched to the upper ─────────────
            Block(boot, "Welt", new Vector3(0.006f, 0.036f, 0f), new Vector3(0.288f, 0.011f, 0.119f), weltMat);
            Sphere(boot, "WeltToe", new Vector3(0.152f, 0.036f, 0f), Vector3.zero, new Vector3(0.096f, 0.012f, 0.117f), weltMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Block(boot, "WeltStitch" + side, new Vector3(0.010f, 0.040f, s * 0.061f), new Vector3(0.250f, 0.004f, 0.004f), stitchMat);
            }

            // ── Leather upper: vamp over the foot, quarters and counter round the heel ─────────────────────
            Sphere(boot, "Vamp", new Vector3(0.075f, 0.064f, 0f), Vector3.zero, new Vector3(0.192f, 0.072f, 0.104f), leatherMat);
            Sphere(boot, "Quarter", new Vector3(-0.045f, 0.078f, 0f), Vector3.zero, new Vector3(0.196f, 0.098f, 0.106f), leatherMat);
            Sphere(boot, "HeelCounter", new Vector3(-0.118f, 0.082f, 0f), Vector3.zero, new Vector3(0.092f, 0.104f, 0.100f), leatherDeepMat);
            Block(boot, "CounterSeam", new Vector3(-0.090f, 0.088f, 0f), new Vector3(0.006f, 0.070f, 0.102f), stitchMat);

            // Reinforced toe cap, closed off by a stitched seam where it meets the vamp.
            Sphere(boot, "ToeCap", new Vector3(0.148f, 0.058f, 0f), Vector3.zero, new Vector3(0.094f, 0.062f, 0.106f), leatherHiMat);
            Sphere(boot, "ToeScuff", new Vector3(0.158f, 0.048f, 0f), Vector3.zero, new Vector3(0.070f, 0.036f, 0.096f), leatherDeepMat);
            Block(boot, "ToeCapSeam", new Vector3(0.108f, 0.072f, 0f), new Vector3(0.006f, 0.036f, 0.104f), stitchMat);

            // ── Shaft climbing past the ankle — the tall silhouette that reads as a boot ────────────────────
            Sphere(boot, "ShaftLow", new Vector3(shaftX, 0.132f, 0f), Vector3.zero, new Vector3(0.162f, 0.112f, 0.104f), leatherMat);
            Sphere(boot, "ShaftHigh", new Vector3(shaftX - 0.004f, 0.186f, 0f), Vector3.zero, new Vector3(0.150f, 0.092f, 0.100f), leatherMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float z = s * (halfWidth + 0.002f);
                Block(boot, "ShaftSeam" + side, new Vector3(shaftX - 0.030f, 0.150f, z), new Vector3(0.006f, 0.110f, 0.005f), stitchMat);
                Sphere(boot, "ShaftCrease" + side, new Vector3(shaftX + 0.026f, 0.116f, s * 0.030f), new Vector3(0f, 0f, 14f),
                    new Vector3(0.060f, 0.010f, 0.070f), leatherDeepMat);
            }

            // Padded collar ring round the opening, with the dark inside of the shaft showing through.
            Sphere(boot, "Lining", new Vector3(shaftX - 0.004f, 0.208f, 0f), Vector3.zero, new Vector3(0.122f, 0.026f, 0.076f), liningMat);
            const int collarSegments = 16;
            for (int i = 0; i < collarSegments; i++)
            {
                float deg = i * 360f / collarSegments;
                float rad = deg * Mathf.Deg2Rad;
                Vector3 p = new Vector3(shaftX - 0.004f + 0.068f * Mathf.Cos(rad), 0.222f, 0.044f * Mathf.Sin(rad));
                var pad = Block(boot, "Collar" + i, p, new Vector3(0.024f, 0.020f, 0.026f), i % 2 == 0 ? leatherMat : leatherHiMat);
                pad.localEulerAngles = new Vector3(0f, -deg, 0f);   // faces the padded roll outward round the opening
            }

            // Pull loop stitched to the back of the shaft.
            Block(boot, "PullLoop", new Vector3(-0.122f, 0.216f, 0f), new Vector3(0.016f, 0.036f, 0.040f), leatherHiMat);
            Block(boot, "PullLoopStitch", new Vector3(-0.116f, 0.200f, 0f), new Vector3(0.006f, 0.006f, 0.034f), stitchMat);

            // ── Gusseted tongue filling the throat, sloping up the front of the shaft ──────────────────────
            var tongue = Block(boot, "Tongue", new Vector3(0.046f, 0.158f, 0f), new Vector3(0.136f, 0.014f, 0.064f), leatherHiMat);
            tongue.localEulerAngles = new Vector3(0f, 0f, 120f);
            var gusset = Block(boot, "Gusset", new Vector3(0.038f, 0.152f, 0f), new Vector3(0.120f, 0.010f, 0.076f), leatherDeepMat);
            gusset.localEulerAngles = new Vector3(0f, 0f, 120f);

            // ── Brass eyelets low on the throat, speed hooks up top, laced criss-cross between them ────────
            const int eyelets = 4;
            Vector3[] anchor = new Vector3[eyelets + 2];
            for (int i = 0; i < eyelets; i++)
            {
                float t = i / (eyelets - 1f);
                anchor[i] = new Vector3(Mathf.Lerp(0.072f, 0.030f, t), Mathf.Lerp(0.104f, 0.170f, t), 0.033f);
                for (int s = -1; s <= 1; s += 2)
                {
                    string side = s > 0 ? "L" : "R";
                    Rod(boot, "Eyelet" + i + side, new Vector3(anchor[i].x, anchor[i].y, s * anchor[i].z),
                        new Vector3(0f, 0f, 30f), 0.014f, 0.007f, metalMat);
                    Rod(boot, "EyeletHole" + i + side, new Vector3(anchor[i].x - 0.002f, anchor[i].y + 0.003f, s * anchor[i].z),
                        new Vector3(0f, 0f, 30f), 0.007f, 0.005f, liningMat);
                }
            }

            // Open speed hooks above the eyelets: a short post with a lip curling back toward the heel.
            for (int i = 0; i < 2; i++)
            {
                anchor[eyelets + i] = new Vector3(0.020f - i * 0.010f, 0.192f + i * 0.022f, 0.036f);
                for (int s = -1; s <= 1; s += 2)
                {
                    string side = s > 0 ? "L" : "R";
                    Vector3 h = new Vector3(anchor[eyelets + i].x, anchor[eyelets + i].y, s * anchor[eyelets + i].z);
                    Rod(boot, "HookPost" + i + side, h, new Vector3(0f, 0f, 24f), 0.010f, 0.018f, metalMat);
                    Block(boot, "HookLip" + i + side, h + new Vector3(-0.012f, 0.008f, 0f),
                        new Vector3(0.020f, 0.006f, 0.007f), metalMat);
                }
            }

            // Lays one lace between two anchor points, yawed and pitched to run from the first to the second.
            void Lace(string name, Vector3 a, Vector3 b)
            {
                Vector3 d = b - a;
                float flat = Mathf.Sqrt(d.x * d.x + d.z * d.z);
                float yaw = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(d.y, flat) * Mathf.Rad2Deg;
                var l = Block(boot, name, (a + b) * 0.5f, new Vector3(d.magnitude, 0.007f, 0.013f), laceMat);
                l.localEulerAngles = new Vector3(0f, yaw, pitch);   // Z pitches the length up, then Y aims it across
            }

            Block(boot, "LaceBar", new Vector3(anchor[0].x + 0.004f, anchor[0].y + 0.008f, 0f),
                new Vector3(0.011f, 0.007f, 0.068f), laceMat);
            for (int i = 0; i < anchor.Length - 1; i++)
            {
                Vector3 lowL = new Vector3(anchor[i].x, anchor[i].y + 0.006f, anchor[i].z);
                Vector3 lowR = new Vector3(anchor[i].x, anchor[i].y + 0.006f, -anchor[i].z);
                Vector3 upL = new Vector3(anchor[i + 1].x, anchor[i + 1].y + 0.006f, anchor[i + 1].z);
                Vector3 upR = new Vector3(anchor[i + 1].x, anchor[i + 1].y + 0.006f, -anchor[i + 1].z);
                Lace("LaceCrossL" + i, lowL, upR);
                Lace("LaceCrossR" + i, lowR, upL);
            }

            // Knot and bow tied off at the top hooks, ends trailing down the outside of the shaft.
            Vector3 top = anchor[anchor.Length - 1];
            Sphere(boot, "LaceKnot", new Vector3(top.x - 0.008f, top.y + 0.012f, 0f), Vector3.zero,
                new Vector3(0.028f, 0.022f, 0.026f), laceMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(boot, "BowLoop" + side, new Vector3(top.x - 0.020f, top.y + 0.014f, s * 0.030f), new Vector3(0f, s * 26f, 0f),
                    new Vector3(0.046f, 0.014f, 0.030f), laceMat);
                var tail = Block(boot, "LaceTail" + side, new Vector3(top.x - 0.006f, top.y - 0.028f, s * 0.042f),
                    new Vector3(0.060f, 0.006f, 0.012f), laceMat);
                tail.localEulerAngles = new Vector3(0f, -s * 30f, -58f);
            }
        }

        /// <summary>
        /// A pair of knitted wool gloves dropped flat, palm-down and yawed apart so the slow spin keeps
        /// showing a fresh angle. The item's tint is the yarn: raised cable columns run down the back of
        /// each hand and a ribbed band closes the wrist, while the thumbs sit on opposite sides so the two
        /// read as a matched left and right rather than the same glove twice.
        /// </summary>
        private static PartNode BuildGloves(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Soft flat garment, so it hovers at the same height as the other laid-out clothing.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color wool = def.Tint;                                                          // the knitted yarn
            Color woolDeep = new Color(wool.r * 0.72f, wool.g * 0.72f, wool.b * 0.72f);     // purl rows and shaded valleys
            Color woolHi = Color.Lerp(wool, new Color(0.96f, 0.94f, 0.92f), 0.22f);         // raised cables and fingertips
            Color lining = new Color(wool.r * 0.40f, wool.g * 0.40f, wool.b * 0.40f);       // dark inside the wrist opening
            Color label = new Color(0.90f, 0.88f, 0.80f);                                   // woven care tab
            Color ink = new Color(0.20f, 0.20f, 0.22f);                                     // printing on the tab

            // Wool is matte: nothing here takes a sheen the way leather or nylon does.
            PartMat woolMat = PartMat.Of(wool, smoothness: 0.07f);
            PartMat woolDeepMat = PartMat.Of(woolDeep, smoothness: 0.05f);
            PartMat woolHiMat = PartMat.Of(woolHi, smoothness: 0.09f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.04f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.10f);
            PartMat inkMat = PartMat.Of(ink, smoothness: 0.08f);

            // Each glove rests on its own local zero, so drop the pair to sit the palms on the root's plane.
            const float ground = -0.02f;

            BuildGlove(root, new Vector3(0.014f, ground, 0.066f), -9f, 1,
                woolMat, woolDeepMat, woolHiMat, liningMat, labelMat, inkMat);
            BuildGlove(root, new Vector3(-0.016f, ground, -0.068f), 14f, -1,
                woolMat, woolDeepMat, woolHiMat, liningMat, labelMat, inkMat);

            return root;
        }

        /// <summary>Builds one knitted glove under <paramref name="parent"/> in its own yaw frame: it lies
        /// palm-down on the frame's local zero with the fingertips at +X and the wrist at −X, so callers can
        /// lay a pair side by side and yaw each one a little. <paramref name="thumbSide"/> (+1 or −1) picks
        /// which flank the thumb and its gusset swing out to, mirroring the glove without flipping any
        /// scales — four splayed fingers, cable columns up the back of the hand and a ribbed cuff rolled
        /// over the dark opening make up the rest.</summary>
        private static void BuildGlove(PartNode parent, Vector3 localPos, float yawDeg, int thumbSide,
            PartMat woolMat, PartMat woolDeepMat, PartMat woolHiMat, PartMat liningMat,
            PartMat labelMat, PartMat inkMat)
        {
            var glove = new PartNode("Glove");
            glove.SetParent(parent, false);
            glove.localPosition = localPos;
            glove.localEulerAngles = new Vector3(0f, yawDeg, 0f);

            const float thick = 0.038f;         // how deep the flattened knit hand is
            const float midY = thick * 0.5f;    // everything is built about the hand's mid-height, palm on y0
            const float topY = midY + thick * 0.5f;

            // ── Flattened hand: the back panel, a knuckle swell and the shaded palm underneath ─────────────
            Sphere(glove, "Back", new Vector3(0.006f, midY, 0f), Vector3.zero, new Vector3(0.150f, thick, 0.104f), woolMat);
            Sphere(glove, "Knuckles", new Vector3(0.062f, midY + 0.002f, 0f), Vector3.zero, new Vector3(0.070f, thick * 0.94f, 0.102f), woolMat);
            Sphere(glove, "Wrist", new Vector3(-0.056f, midY, 0f), Vector3.zero, new Vector3(0.064f, thick * 0.92f, 0.094f), woolMat);
            Block(glove, "Palm", new Vector3(0.010f, midY - 0.012f, 0f), new Vector3(0.132f, 0.014f, 0.092f), woolDeepMat);

            // ── Knitted face: cable columns standing proud of the purl rows sunk between them ──────────────
            for (int i = -1; i <= 1; i++)
            {
                Block(glove, "Cable" + (i + 1), new Vector3(0.012f, topY - 0.003f, i * 0.028f),
                    new Vector3(0.120f, 0.008f, 0.013f), woolHiMat);
                Block(glove, "Purl" + (i + 1), new Vector3(0.012f, topY - 0.006f, i * 0.028f + 0.014f),
                    new Vector3(0.116f, 0.006f, 0.010f), woolDeepMat);
            }

            // Chevron knitted across the knuckles, pointing back toward the wrist.
            for (int s = -1; s <= 1; s += 2)
            {
                var arm = Block(glove, "Chevron" + (s > 0 ? "L" : "R"), new Vector3(0.048f, topY - 0.003f, s * 0.020f),
                    new Vector3(0.052f, 0.007f, 0.010f), woolHiMat);
                arm.localEulerAngles = new Vector3(0f, s * 32f, 0f);   // meets its mirror in a V over the knuckles
            }

            // Soft creases where the knit slumps now nobody's hand is filling it.
            Sphere(glove, "CreaseA", new Vector3(-0.020f, topY - 0.004f, thumbSide * 0.026f), new Vector3(0f, thumbSide * 24f, 0f),
                new Vector3(0.058f, 0.008f, 0.020f), woolDeepMat);
            Sphere(glove, "CreaseB", new Vector3(0.030f, topY - 0.005f, -thumbSide * 0.030f), new Vector3(0f, -thumbSide * 16f, 0f),
                new Vector3(0.044f, 0.008f, 0.016f), woolDeepMat);

            // Builds one finger in its own splay frame: a chain of squashed segments with knit joint grooves.
            void Finger(string name, float z, float splayDeg, float len, float dia)
            {
                var f = new PartNode(name);
                f.SetParent(glove, false);
                f.localPosition = new Vector3(0.086f, midY - 0.002f, z);
                f.localEulerAngles = new Vector3(0f, splayDeg, 0f);

                Sphere(f, "Base", new Vector3(len * 0.20f, 0f, 0f), Vector3.zero, new Vector3(len * 0.58f, dia * 1.02f, dia), woolMat);
                Sphere(f, "Mid", new Vector3(len * 0.55f, 0f, 0f), Vector3.zero, new Vector3(len * 0.52f, dia * 0.96f, dia * 0.96f), woolMat);
                Sphere(f, "Tip", new Vector3(len * 0.86f, -0.001f, 0f), Vector3.zero, new Vector3(len * 0.36f, dia * 0.86f, dia * 0.88f), woolHiMat);
                Block(f, "Joint", new Vector3(len * 0.44f, dia * 0.40f, 0f), new Vector3(0.005f, 0.005f, dia * 0.86f), woolDeepMat);
                Block(f, "TipRib", new Vector3(len * 0.70f, dia * 0.36f, 0f), new Vector3(0.004f, 0.004f, dia * 0.78f), woolDeepMat);
            }

            // Fingers fan out from the knuckles, longest in the middle; a Block yawed θ aims its length at
            // (cos θ, −sin θ), so the thumb-side finger needs a negative splay to lean that way.
            Finger("Index", thumbSide * 0.036f, -thumbSide * 9f, 0.076f, 0.030f);
            Finger("Middle", thumbSide * 0.012f, -thumbSide * 2f, 0.084f, 0.031f);
            Finger("Ring", -thumbSide * 0.013f, thumbSide * 5f, 0.076f, 0.029f);
            Finger("Little", -thumbSide * 0.036f, thumbSide * 12f, 0.060f, 0.026f);

            // ── Thumb swinging out of the side of the hand, on its own raked frame ─────────────────────────
            var thumb = new PartNode("Thumb");
            thumb.SetParent(glove, false);
            thumb.localPosition = new Vector3(0.020f, midY - 0.003f, thumbSide * 0.042f);
            thumb.localEulerAngles = new Vector3(0f, -thumbSide * 46f, 0f);

            Sphere(thumb, "Base", new Vector3(0.016f, 0f, 0f), Vector3.zero, new Vector3(0.054f, 0.036f, 0.040f), woolMat);
            Sphere(thumb, "Mid", new Vector3(0.044f, 0f, 0f), Vector3.zero, new Vector3(0.046f, 0.032f, 0.035f), woolMat);
            Sphere(thumb, "Tip", new Vector3(0.064f, -0.001f, 0f), Vector3.zero, new Vector3(0.032f, 0.028f, 0.030f), woolHiMat);
            Block(thumb, "Joint", new Vector3(0.034f, 0.015f, 0f), new Vector3(0.005f, 0.005f, 0.032f), woolDeepMat);

            // Gusset seam where the thumb is knitted into the palm.
            var gusset = Block(glove, "Gusset", new Vector3(0.014f, topY - 0.005f, thumbSide * 0.034f),
                new Vector3(0.048f, 0.006f, 0.008f), woolDeepMat);
            gusset.localEulerAngles = new Vector3(0f, -thumbSide * 44f, 0f);

            // ── Ribbed wristband, rolled over at the end with the dark inside of the glove showing ─────────
            Block(glove, "Cuff", new Vector3(-0.086f, midY, 0f), new Vector3(0.064f, thick * 1.06f, 0.106f), woolDeepMat);
            Rod(glove, "CuffRoll", new Vector3(-0.114f, midY, 0f), new Vector3(90f, 0f, 0f), thick * 1.14f, 0.106f, woolMat);
            Sphere(glove, "Lining", new Vector3(-0.122f, midY, 0f), Vector3.zero, new Vector3(0.014f, thick * 0.86f, 0.086f), liningMat);

            const int ribs = 13;
            for (int i = 0; i < ribs; i++)
            {
                float z = Mathf.Lerp(-0.046f, 0.046f, i / (ribs - 1f));
                Block(glove, "CuffRib" + i, new Vector3(-0.084f, midY + thick * 0.52f, z),
                    new Vector3(0.056f, 0.008f, 0.006f), i % 2 == 0 ? woolMat : woolHiMat);
            }

            // Woven care tab stitched just above the cuff, off to one side for a little asymmetry.
            var tab = new PartNode("Label");
            tab.SetParent(glove, false);
            tab.localPosition = new Vector3(-0.046f, midY + thick * 0.44f, -thumbSide * 0.028f);
            tab.localEulerAngles = new Vector3(0f, -thumbSide * 18f, 0f);
            Block(tab, "Tag", Vector3.zero, new Vector3(0.026f, 0.005f, 0.016f), labelMat);
            Block(tab, "TagInk", new Vector3(0f, 0.004f, 0f), new Vector3(0.016f, 0.003f, 0.004f), inkMat);
        }

        /// <summary>
        /// A pair of leather gloves lying flat palm-down side by side, kicked slightly out of line with one
        /// another. The hide takes the item's tint and the whole model is glossier than the wool pair: the
        /// knit's cables and ribbed cuff give way to stitched darts, a padded knuckle bar, punched vent
        /// holes and a snapped wrist strap.
        /// </summary>
        private static PartNode BuildLeatherGloves(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Flat garment, so it hovers at the same height as the rest of the laid-out clothing.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color hide = def.Tint;                                                              // the leather itself
            Color hideDeep = new Color(hide.r * 0.70f, hide.g * 0.70f, hide.b * 0.70f);         // palm and shaded folds
            Color hideHi = Color.Lerp(hide, new Color(0.62f, 0.46f, 0.32f), 0.32f);             // knuckle panel and worn edges
            Color lining = new Color(hide.r * 0.36f, hide.g * 0.36f, hide.b * 0.36f);           // dark inside the wrist and the punched holes
            Color stitch = Color.Lerp(hide, new Color(0.92f, 0.86f, 0.70f), 0.55f);             // waxed topstitching
            Color metal = new Color(0.78f, 0.79f, 0.82f);                                       // strap snap
            Color metalHi = new Color(0.90f, 0.91f, 0.94f);

            // Waxed hide takes a sheen the knit never could — this is the main read against the wool pair.
            PartMat hideMat = PartMat.Of(hide, smoothness: 0.40f);
            PartMat hideDeepMat = PartMat.Of(hideDeep, smoothness: 0.32f);
            PartMat hideHiMat = PartMat.Of(hideHi, smoothness: 0.46f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.16f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.22f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.72f, metallic: 0.88f);
            PartMat metalHiMat = PartMat.Of(metalHi, smoothness: 0.80f, metallic: 0.90f);

            // Each glove rests on its own local zero, so drop the pair to sit the palms on the root's plane.
            const float ground = -0.018f;

            BuildLeatherGlove(root, new Vector3(0.012f, ground, 0.068f), -11f, 1,
                hideMat, hideDeepMat, hideHiMat, liningMat, stitchMat, metalMat, metalHiMat);
            BuildLeatherGlove(root, new Vector3(-0.018f, ground, -0.070f), 13f, -1,
                hideMat, hideDeepMat, hideHiMat, liningMat, stitchMat, metalMat, metalHiMat);

            return root;
        }

        /// <summary>Builds one leather glove under <paramref name="parent"/> in its own yaw frame: it lies
        /// palm-down on the frame's local zero with the fingertips at +X and the wrist at −X, so callers can
        /// lay a pair side by side and yaw each one a little. <paramref name="thumbSide"/> (+1 or −1) picks
        /// which flank the thumb and its gusset swing out to, mirroring the glove without flipping any
        /// scales — three stitched darts down the back of the hand, a padded knuckle bar, punched vent
        /// holes and a snapped wrist strap over the bound opening make up the rest.</summary>
        private static void BuildLeatherGlove(PartNode parent, Vector3 localPos, float yawDeg, int thumbSide,
            PartMat hideMat, PartMat hideDeepMat, PartMat hideHiMat, PartMat liningMat,
            PartMat stitchMat, PartMat metalMat, PartMat metalHiMat)
        {
            var glove = new PartNode("Glove");
            glove.SetParent(parent, false);
            glove.localPosition = localPos;
            glove.localEulerAngles = new Vector3(0f, yawDeg, 0f);

            const float thick = 0.032f;         // leather sits tighter than the knit, so the hand is flatter
            const float midY = thick * 0.5f;    // everything is built about the hand's mid-height, palm on y0
            const float topY = midY + thick * 0.5f;

            // Lays a run of topstitch dashes between two points on the back of the hand, each dash turned to
            // follow the line — a Block yawed θ aims its length at (cos θ, −sin θ).
            void Seam(string name, Vector3 a, Vector3 b, int dashes, float dashLen)
            {
                float yaw = Mathf.Atan2(-(b.z - a.z), b.x - a.x) * Mathf.Rad2Deg;
                for (int i = 0; i < dashes; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (dashes - 1f));
                    var d = Block(glove, name + i, p, new Vector3(dashLen, 0.004f, 0.0035f), stitchMat);
                    d.localEulerAngles = new Vector3(0f, yaw, 0f);
                }
            }

            // ── Flattened hand: the back panel, a knuckle swell and the shaded palm underneath ─────────────
            Sphere(glove, "Back", new Vector3(0.006f, midY, 0f), Vector3.zero, new Vector3(0.152f, thick, 0.100f), hideMat);
            Sphere(glove, "Knuckles", new Vector3(0.062f, midY + 0.002f, 0f), Vector3.zero, new Vector3(0.068f, thick * 0.96f, 0.098f), hideMat);
            Sphere(glove, "Wrist", new Vector3(-0.056f, midY, 0f), Vector3.zero, new Vector3(0.064f, thick * 0.94f, 0.092f), hideMat);
            Block(glove, "Palm", new Vector3(0.010f, midY - 0.012f, 0f), new Vector3(0.134f, 0.014f, 0.090f), hideDeepMat);

            // ── Three stitched darts (the "points") running back from the knuckles, the leather glove's
            //    answer to the knit's cable columns ─────────────────────────────────────────────────────────
            for (int i = -1; i <= 1; i++)
            {
                float z = i * 0.024f;
                Block(glove, "Point" + (i + 1), new Vector3(0.006f, topY - 0.004f, z),
                    new Vector3(0.104f, 0.007f, 0.009f), hideHiMat);       // welt raised either side of the seam
                Seam("PointStitch" + (i + 1),
                    new Vector3(-0.042f, topY - 0.002f, z), new Vector3(0.052f, topY - 0.002f, z), 7, 0.010f);
            }

            // ── Padded knuckle bar: a raised panel with a roll over each knuckle ───────────────────────────
            Block(glove, "KnucklePanel", new Vector3(0.062f, topY - 0.003f, 0f),
                new Vector3(0.046f, 0.008f, 0.092f), hideHiMat);
            for (int i = 0; i < 4; i++)
            {
                float z = Mathf.Lerp(-0.034f, 0.034f, i / 3f);
                // A Rod tilted (0, 0, 90) lays its length along the hand's local X, across the knuckle.
                Rod(glove, "Knuckle" + i, new Vector3(0.062f, topY - 0.001f, z), new Vector3(0f, 0f, 90f),
                    0.013f, 0.038f, hideMat);
            }
            Seam("KnuckleSeam", new Vector3(0.040f, topY - 0.001f, -0.042f), new Vector3(0.040f, topY - 0.001f, 0.042f), 6, 0.008f);

            // Punched ventilation holes across the back of the hand, dark through the hide.
            for (int i = 0; i < 6; i++)
            {
                float x = 0.014f + (i / 3) * 0.020f;
                float z = (i % 3 - 1) * 0.024f + 0.012f;
                Rod(glove, "Vent" + i, new Vector3(x, topY - 0.001f, z), Vector3.zero, 0.006f, 0.006f, liningMat);
            }

            // Soft creases where the hide slumps now nobody's hand is filling it.
            Sphere(glove, "CreaseA", new Vector3(-0.022f, topY - 0.004f, thumbSide * 0.030f), new Vector3(0f, thumbSide * 26f, 0f),
                new Vector3(0.056f, 0.008f, 0.018f), hideDeepMat);
            Sphere(glove, "CreaseB", new Vector3(0.032f, topY - 0.005f, -thumbSide * 0.034f), new Vector3(0f, -thumbSide * 15f, 0f),
                new Vector3(0.042f, 0.008f, 0.014f), hideDeepMat);

            // Builds one finger in its own splay frame: tapering segments with a fourchette seam down each side.
            void Finger(string name, float z, float splayDeg, float len, float dia)
            {
                var f = new PartNode(name);
                f.SetParent(glove, false);
                f.localPosition = new Vector3(0.086f, midY - 0.002f, z);
                f.localEulerAngles = new Vector3(0f, splayDeg, 0f);

                Sphere(f, "Base", new Vector3(len * 0.20f, 0f, 0f), Vector3.zero, new Vector3(len * 0.60f, dia * 1.02f, dia), hideMat);
                Sphere(f, "Mid", new Vector3(len * 0.56f, 0f, 0f), Vector3.zero, new Vector3(len * 0.52f, dia * 0.94f, dia * 0.94f), hideMat);
                Sphere(f, "Tip", new Vector3(len * 0.87f, -0.001f, 0f), Vector3.zero, new Vector3(len * 0.34f, dia * 0.84f, dia * 0.86f), hideHiMat);
                Block(f, "Crease", new Vector3(len * 0.44f, dia * 0.40f, 0f), new Vector3(0.004f, 0.004f, dia * 0.84f), hideDeepMat);

                // Fourchettes: the panels between the fingers are stitched down both flanks.
                for (int s = -1; s <= 1; s += 2)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float x = Mathf.Lerp(len * 0.16f, len * 0.82f, i / 3f);
                        Block(f, "Fourchette" + (s > 0 ? "L" : "R") + i, new Vector3(x, dia * 0.18f, s * dia * 0.46f),
                            new Vector3(0.007f, 0.003f, 0.003f), stitchMat);
                    }
                }
            }

            // Fingers fan out from the knuckles, longest in the middle; a Block yawed θ aims its length at
            // (cos θ, −sin θ), so the thumb-side finger needs a negative splay to lean that way.
            Finger("Index", thumbSide * 0.035f, -thumbSide * 9f, 0.082f, 0.028f);
            Finger("Middle", thumbSide * 0.012f, -thumbSide * 2f, 0.090f, 0.029f);
            Finger("Ring", -thumbSide * 0.013f, thumbSide * 5f, 0.082f, 0.027f);
            Finger("Little", -thumbSide * 0.035f, thumbSide * 12f, 0.066f, 0.024f);

            // ── Thumb swinging out of the side of the hand, on its own raked frame ─────────────────────────
            var thumb = new PartNode("Thumb");
            thumb.SetParent(glove, false);
            thumb.localPosition = new Vector3(0.020f, midY - 0.003f, thumbSide * 0.040f);
            thumb.localEulerAngles = new Vector3(0f, -thumbSide * 46f, 0f);

            Sphere(thumb, "Base", new Vector3(0.016f, 0f, 0f), Vector3.zero, new Vector3(0.054f, 0.032f, 0.038f), hideMat);
            Sphere(thumb, "Mid", new Vector3(0.045f, 0f, 0f), Vector3.zero, new Vector3(0.046f, 0.029f, 0.033f), hideMat);
            Sphere(thumb, "Tip", new Vector3(0.066f, -0.001f, 0f), Vector3.zero, new Vector3(0.032f, 0.025f, 0.028f), hideHiMat);
            Block(thumb, "Crease", new Vector3(0.034f, 0.013f, 0f), new Vector3(0.004f, 0.004f, 0.030f), hideDeepMat);

            // Gusset seam where the thumb is set into the palm, welted and stitched like the darts.
            var gusset = Block(glove, "Gusset", new Vector3(0.014f, topY - 0.005f, thumbSide * 0.034f),
                new Vector3(0.050f, 0.006f, 0.008f), hideHiMat);
            gusset.localEulerAngles = new Vector3(0f, -thumbSide * 44f, 0f);
            Seam("GussetStitch", new Vector3(-0.004f, topY - 0.003f, thumbSide * 0.022f),
                new Vector3(0.032f, topY - 0.003f, thumbSide * 0.046f), 5, 0.009f);

            // ── Bound wrist opening: a hemmed edge piped along the rim, dark inside the glove ──────────────
            Block(glove, "Hem", new Vector3(-0.090f, midY, 0f), new Vector3(0.056f, thick * 1.04f, 0.100f), hideDeepMat);
            Rod(glove, "HemPipe", new Vector3(-0.116f, midY, 0f), new Vector3(90f, 0f, 0f), thick * 1.10f, 0.100f, hideHiMat);
            Sphere(glove, "Lining", new Vector3(-0.124f, midY, 0f), Vector3.zero, new Vector3(0.014f, thick * 0.84f, 0.082f), liningMat);
            Seam("HemStitch", new Vector3(-0.104f, topY - 0.002f, -0.040f), new Vector3(-0.104f, topY - 0.002f, 0.040f), 6, 0.008f);

            // Short opening slit on the little-finger flank, so the glove can be pulled on over a cuff.
            var slit = Block(glove, "Slit", new Vector3(-0.078f, topY - 0.003f, -thumbSide * 0.038f),
                new Vector3(0.038f, 0.006f, 0.005f), liningMat);
            slit.localEulerAngles = new Vector3(0f, -thumbSide * 6f, 0f);

            // ── Adjuster strap snapped across the back of the wrist ────────────────────────────────────────
            Block(glove, "Strap", new Vector3(-0.062f, topY - 0.002f, 0.004f), new Vector3(0.020f, 0.008f, 0.104f), hideMat);
            Block(glove, "StrapEdge", new Vector3(-0.062f, topY + 0.002f, 0.004f), new Vector3(0.014f, 0.004f, 0.100f), hideHiMat);
            Block(glove, "StrapTail", new Vector3(-0.062f, topY - 0.002f, thumbSide * 0.058f), new Vector3(0.018f, 0.007f, 0.024f), hideMat);
            Block(glove, "Keeper", new Vector3(-0.062f, topY, -thumbSide * 0.030f), new Vector3(0.026f, 0.006f, 0.010f), hideDeepMat);
            Rod(glove, "Snap", new Vector3(-0.062f, topY + 0.004f, thumbSide * 0.052f), Vector3.zero, 0.013f, 0.005f, metalMat);
            Rod(glove, "SnapPip", new Vector3(-0.062f, topY + 0.007f, thumbSide * 0.052f), Vector3.zero, 0.006f, 0.003f, metalHiMat);

            // Maker's mark stamped into the hide between the strap and the darts.
            Sphere(glove, "Stamp", new Vector3(-0.036f, topY - 0.003f, -thumbSide * 0.024f), Vector3.zero,
                new Vector3(0.026f, 0.006f, 0.014f), hideHiMat);
            Block(glove, "StampInk", new Vector3(-0.036f, topY - 0.001f, -thumbSide * 0.024f),
                new Vector3(0.014f, 0.004f, 0.004f), hideDeepMat);
        }

        /// <summary>
        /// A short strapless dress cut out of a builder's tarpaulin, laid flat face-up with the top edge
        /// at +X and the hem at −X. It is the bin-liner dress's opposite number and is built to contrast
        /// with it at every point: a tarp is not smooth film but a woven scrim laminated between two
        /// coats, so the surface carries a visible cross-hatch grid and only a dull sheen rather than the
        /// bin bag's glassy facets. It is also far too stiff to gather, so it stands in broad shallow
        /// rolls, and it is far too tough to tape — it is held together the way a tarp always is, by
        /// punched brass grommets with paracord laced through them, along a folded edge band with a rope
        /// run inside it. Where the bin liner tears, this frays into stiff plastic threads.
        /// </summary>
        private static PartNode BuildDressSmallTarpStrapless(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The blue every builder's tarp in the world is, with the catalogue tint allowed only a
            // little say — a tarp that is not tarp-blue stops reading as salvage.
            Color tarpBlue = new Color(0.145f, 0.310f, 0.520f);
            Color sheet = Color.Lerp(tarpBlue, def.Tint, 0.14f);                                  // the laminated sheet
            Color sheetDeep = new Color(sheet.r * 0.60f, sheet.g * 0.64f, sheet.b * 0.72f);       // the underside of a roll
            Color scrim = Color.Lerp(sheet, new Color(0.70f, 0.80f, 0.90f), 0.42f);               // the woven grid showing through
            Color faded = Color.Lerp(sheet, new Color(0.72f, 0.78f, 0.80f), 0.50f);               // sun-bleached and chalky
            Color sheen = Color.Lerp(sheet, new Color(0.62f, 0.74f, 0.88f), 0.45f);               // the dull gloss of the coating
            Color band = Color.Lerp(sheet, new Color(0.06f, 0.16f, 0.30f), 0.55f);                // the darker folded edge band
            Color brass = new Color(0.74f, 0.60f, 0.26f);                                         // punched grommets
            Color rope = new Color(0.86f, 0.84f, 0.74f);                                          // paracord and the hem rope
            Color ropeShade = new Color(0.58f, 0.56f, 0.48f);
            Color lining = new Color(sheet.r * 0.42f, sheet.g * 0.44f, sheet.b * 0.50f);          // the inside, seen past the edge

            // A tarp's coating is satin at best — well below the bin liner's 0.66, well above cloth.
            PartMat sheetMat = PartMat.Of(sheet, smoothness: 0.30f);
            PartMat sheetDeepMat = PartMat.Of(sheetDeep, smoothness: 0.24f);
            PartMat scrimMat = PartMat.Of(scrim, smoothness: 0.22f);
            PartMat fadedMat = PartMat.Of(faded, smoothness: 0.18f);
            PartMat sheenMat = PartMat.Of(sheen, smoothness: 0.38f);
            PartMat bandMat = PartMat.Of(band, smoothness: 0.28f);
            PartMat brassMat = PartMat.Of(brass, smoothness: 0.74f, metallic: 0.70f);
            PartMat ropeMat = PartMat.Of(rope, smoothness: 0.14f);
            PartMat ropeShadeMat = PartMat.Of(ropeShade, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.20f);

            // Lays a flat band between two points, aimed from one to the other — used for the edge bands,
            // the lacing and the scrim, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var b = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                b.localEulerAngles = new Vector3(0f, yaw, 0f);
                return b;
            }

            // ── Body: a stiff tube that holds its own shape, barely narrowed at the waist ─────────────────
            Sphere(root, "TopEdge", new Vector3(0.122f, 0f, 0f), Vector3.zero, new Vector3(0.08f, 0.058f, 0.216f), sheetMat);
            Sphere(root, "Bust", new Vector3(0.055f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.060f, 0.224f), sheetMat);
            Sphere(root, "Waist", new Vector3(-0.048f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.058f, 0.214f), sheetMat);
            Sphere(root, "Hip", new Vector3(-0.155f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.058f, 0.234f), sheetMat);
            Sphere(root, "Skirt", new Vector3(-0.255f, -0.002f, 0f), Vector3.zero, new Vector3(0.13f, 0.056f, 0.244f), sheetMat);

            // ── Woven scrim: the cross-hatch grid laminated inside the sheet, and the one thing that says
            //    tarpaulin rather than plastic bag on sight ──────────────────────────────────────────────
            for (int i = 0; i < 15; i++)
            {
                float x = Mathf.Lerp(0.140f, -0.290f, i / 14f);
                float halfZ = Mathf.Lerp(0.096f, 0.118f, i / 14f);
                Block(root, "Warp" + i, new Vector3(x, 0.038f, 0f), new Vector3(0.007f, 0.005f, halfZ * 2f), scrimMat);
            }
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f - 0.5f) * 0.212f;
                Block(root, "Weft" + i, new Vector3(-0.070f, 0.037f, z), new Vector3(0.420f, 0.005f, 0.007f), scrimMat);
            }

            // ── Broad stiff rolls: tarp is too heavy to crease, so it bends in long shallow curves ────────
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f * 2f - 1f;
                var roll = Sphere(root, "Roll" + i, new Vector3(-0.090f + Mathf.Abs(t) * 0.050f, 0.040f, t * 0.086f),
                    new Vector3(0f, t * 10f, 0f), new Vector3(0.330f, 0.018f, 0.052f), sheenMat);
                roll.localEulerAngles = new Vector3(0f, t * 10f, 0f);
                Sphere(root, "RollUnder" + i, new Vector3(-0.100f + Mathf.Abs(t) * 0.050f, 0.032f, t * 0.086f + 0.034f),
                    new Vector3(0f, t * 10f, 0f), new Vector3(0.300f, 0.013f, 0.030f), sheetDeepMat);
            }

            // Chalky sun-bleaching across the face, where it has lain out in the weather.
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(0.100f, -0.240f, i / 4f);
                Sphere(root, "SunFade" + i, new Vector3(x, 0.042f, Mathf.Sin(i * 2.1f) * 0.052f),
                    new Vector3(0f, i * 31f, 0f), new Vector3(0.090f, 0.010f, 0.076f), fadedMat);
            }

            // ── Folded edge band with a rope run inside it, the way a tarp is finished ────────────────────
            Band("EdgeBand", new Vector3(0.164f, 0f, -0.100f), new Vector3(0.164f, 0f, 0.100f), 0.034f, 0.038f, 0.026f, bandMat);
            Band("EdgeBandFold", new Vector3(0.150f, 0f, -0.098f), new Vector3(0.150f, 0f, 0.098f), 0.008f, 0.050f, 0.007f, sheenMat);
            // The rope core, showing as a run of bulges under the band.
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f - 0.5f) * 0.180f;
                Sphere(root, "RopeCore" + i, new Vector3(0.172f, 0.046f, z), Vector3.zero,
                    new Vector3(0.020f, 0.014f, 0.024f), ropeMat);
            }
            Sphere(root, "Inside", new Vector3(0.138f, 0.018f, 0f), Vector3.zero, new Vector3(0.052f, 0.032f, 0.158f), liningMat);

            // ── Grommets: punched brass eyelets, the fastening a tarp actually has ────────────────────────
            // Five along the top edge and two down each side, each a brass ring round a dark hole.
            void Grommet(string name, float x, float z)
            {
                Sphere(root, name + "Ring", new Vector3(x, 0.046f, z), Vector3.zero,
                    new Vector3(0.026f, 0.012f, 0.026f), brassMat);
                Sphere(root, name + "Hole", new Vector3(x, 0.052f, z), Vector3.zero,
                    new Vector3(0.013f, 0.008f, 0.013f), liningMat);
            }

            for (int i = 0; i < 5; i++) Grommet("TopEyelet" + i, 0.164f, (i / 4f - 0.5f) * 0.176f);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                for (int i = 0; i < 3; i++) Grommet("SideEyelet" + side + i, 0.050f - i * 0.110f, s * (0.104f + i * 0.006f));
            }

            // ── Paracord laced through the side grommets, pulling the two edges together ──────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                for (int i = 0; i < 3; i++)
                {
                    float x0 = 0.050f - i * 0.110f;
                    float x1 = 0.050f - (i + 1) * 0.110f;
                    int dir = (i % 2 == 0) ? 1 : -1;
                    Band("Lace" + side + i,
                        new Vector3(x0, 0f, s * 0.104f), new Vector3(x1, 0f, s * (0.104f + dir * 0.030f)),
                        0.010f, 0.052f, 0.008f, ropeMat);
                    Band("LaceShade" + side + i,
                        new Vector3(x0, 0f, s * 0.112f), new Vector3(x1, 0f, s * (0.112f + dir * 0.030f)),
                        0.006f, 0.046f, 0.005f, ropeShadeMat);
                }
                // The loose end left hanging past the last eyelet.
                Band("LaceTail" + side, new Vector3(-0.280f, 0f, s * 0.116f), new Vector3(-0.330f, 0f, s * 0.150f),
                    0.009f, 0.048f, 0.008f, ropeMat);
            }

            // ── Reinforced corner patches, the triangles a tarp carries at every corner ───────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                var corner = new PartNode(s > 0 ? "CornerPatchL" : "CornerPatchR");
                corner.SetParent(root, false);
                corner.localPosition = new Vector3(0.150f, 0.044f, s * 0.092f);
                corner.localEulerAngles = new Vector3(0f, s * -32f, 0f);
                // A wedge, built as bars narrowing away from the corner.
                for (int i = 0; i < 3; i++)
                    Block(corner, "Ply" + i, new Vector3(-i * 0.020f, 0f, 0f),
                        new Vector3(0.020f, 0.008f, 0.056f - i * 0.016f), bandMat);
                Block(corner, "PlyStitch", new Vector3(-0.022f, 0.005f, 0f), new Vector3(0.006f, 0.005f, 0.040f), ropeShadeMat);
            }

            // ── Cut hem: tarp frays into stiff plastic threads rather than tearing like film ──────────────
            Block(root, "HemCut", new Vector3(-0.310f, 0.006f, 0f), new Vector3(0.016f, 0.056f, 0.238f), sheetDeepMat);
            Block(root, "HemScrim", new Vector3(-0.302f, 0.040f, 0f), new Vector3(0.008f, 0.006f, 0.230f), scrimMat);
            for (int i = 0; i < 12; i++)
            {
                float z = (i / 11f - 0.5f) * 0.226f;
                float len = 0.014f + Mathf.Abs(Mathf.Sin(i * 2.7f)) * 0.024f;   // each thread a different length
                var thread = Block(root, "FrayThread" + i, new Vector3(-0.322f - len * 0.5f, 0.032f, z),
                    new Vector3(len, 0.005f, 0.005f), scrimMat);
                thread.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.6f) * 20f, 0f);
            }
            // A patch where the two coatings have parted and the scrim is showing bare.
            var delam = new PartNode("Delamination");
            delam.SetParent(root, false);
            delam.localPosition = new Vector3(-0.212f, 0.042f, -0.070f);
            delam.localEulerAngles = new Vector3(0f, 24f, 0f);
            Sphere(delam, "BareScrim", Vector3.zero, Vector3.zero, new Vector3(0.076f, 0.010f, 0.050f), scrimMat);
            Block(delam, "LiftedCoat", new Vector3(0.030f, 0.006f, 0f), new Vector3(0.026f, 0.008f, 0.048f), sheenMat);
            Block(delam, "CoatEdge", new Vector3(0.042f, 0.008f, 0f), new Vector3(0.007f, 0.006f, 0.046f), sheetDeepMat);

            return root;
        }

        /// <summary>
        /// A short strapless dress cut out of a black bin liner, laid flat face-up with the top edge at
        /// +X and the hem at −X. Every other garment in the set is cloth; this one is plastic sheeting,
        /// and it is built to look like nothing else on the floor because of it. Polythene does not
        /// drape or crease softly — it takes hard angular facets that hold their shape and throw a
        /// bright specular streak along every ridge, so the whole surface is glossy planes rather than
        /// rounded folds. It is held together with parcel tape instead of stitching, the bag's own
        /// drawstring hem has been kept as the strapless top edge, the bottom is simply cut and has
        /// begun to tear, and the sack's printed recycling mark still sits across the front.
        /// </summary>
        private static PartNode BuildDressSmallGarbageStrapless(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Bin-liner black, barely touched by the catalogue tint: the point of the item is that it is
            // obviously a rubbish bag, and a dyed-looking one would read as cloth.
            Color poly = new Color(0.105f, 0.100f, 0.115f);
            Color film = Color.Lerp(poly, def.Tint, 0.09f);                                    // the sheeting itself
            Color filmDeep = new Color(film.r * 0.52f, film.g * 0.52f, film.b * 0.58f);        // the dark side of a facet
            Color sheen = Color.Lerp(film, new Color(0.62f, 0.64f, 0.72f), 0.62f);             // the lit side of a facet
            Color glint = Color.Lerp(film, new Color(0.90f, 0.92f, 0.97f), 0.80f);             // hard specular streak on a ridge
            Color tape = new Color(0.62f, 0.61f, 0.58f);                                       // parcel tape holding it together
            Color tapeEdge = new Color(0.46f, 0.45f, 0.43f);                                   // its duller torn edge
            Color cord = new Color(0.78f, 0.78f, 0.80f);                                       // the bag's drawstring
            Color print = new Color(0.86f, 0.88f, 0.90f);                                      // printed markings on the sack
            Color lining = new Color(film.r * 0.40f, film.g * 0.40f, film.b * 0.46f);          // the inside, seen down the neck

            // Plastic is by a distance the shiniest surface in the item set — the satin wedding dress
            // sits at 0.42 and the sacking at 0.03, so this reads as a different material at a glance.
            PartMat filmMat = PartMat.Of(film, smoothness: 0.66f);
            PartMat filmDeepMat = PartMat.Of(filmDeep, smoothness: 0.58f);
            PartMat sheenMat = PartMat.Of(sheen, smoothness: 0.72f);
            PartMat glintMat = PartMat.Of(glint, smoothness: 0.80f);
            PartMat tapeMat = PartMat.Of(tape, smoothness: 0.26f);
            PartMat tapeEdgeMat = PartMat.Of(tapeEdge, smoothness: 0.20f);
            PartMat cordMat = PartMat.Of(cord, smoothness: 0.30f);
            PartMat printMat = PartMat.Of(print, smoothness: 0.34f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.44f);

            // Lays a flat band between two points, aimed from one to the other — used for the tape runs,
            // the gusset creases and the drawstring, so each is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: a short tube of sheeting, barely shaped, because a bag has no shaping in it ────────
            Sphere(root, "TopEdge", new Vector3(0.118f, 0f, 0f), Vector3.zero, new Vector3(0.07f, 0.052f, 0.208f), filmMat);
            Sphere(root, "Bust", new Vector3(0.055f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.054f, 0.216f), filmMat);
            Sphere(root, "Waist", new Vector3(-0.045f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.052f, 0.200f), filmMat);
            Sphere(root, "Hip", new Vector3(-0.150f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.052f, 0.222f), filmMat);
            Sphere(root, "Skirt", new Vector3(-0.245f, -0.002f, 0f), Vector3.zero, new Vector3(0.12f, 0.050f, 0.232f), filmMat);

            // ── Faceting: the hard angular planes polythene takes instead of folds ────────────────────────
            // Each facet is a flat plane with a shaded neighbour and a hard glint along the ridge between
            // them. This is the whole look of the item, so it is the densest thing in the model.
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f;
                float x = Mathf.Lerp(0.120f, -0.280f, t);
                float halfZ = Mathf.Lerp(0.088f, 0.112f, t);
                float lean = Mathf.Sin(i * 2.7f) * 26f;          // every facet catches the light differently

                var plane = Block(root, "Facet" + i, new Vector3(x, 0.034f, Mathf.Sin(i * 1.7f) * 0.030f),
                    new Vector3(0.052f, 0.012f, halfZ * 1.7f), sheenMat);
                plane.localEulerAngles = new Vector3(0f, lean, 0f);

                var shade = Block(root, "FacetShade" + i, new Vector3(x - 0.018f, 0.029f, Mathf.Sin(i * 1.7f) * 0.030f + 0.026f),
                    new Vector3(0.030f, 0.010f, halfZ * 1.5f), filmDeepMat);
                shade.localEulerAngles = new Vector3(0f, lean, 0f);

                var ridge = Block(root, "Glint" + i, new Vector3(x + 0.012f, 0.041f, Mathf.Sin(i * 1.7f) * 0.030f),
                    new Vector3(0.010f, 0.006f, halfZ * 1.55f), glintMat);
                ridge.localEulerAngles = new Vector3(0f, lean, 0f);
            }

            // Crinkles running crosswise, the concertina a bag keeps from being folded in its box.
            for (int i = 0; i < 7; i++)
            {
                float z = (i / 6f - 0.5f) * 0.190f;
                var crease = Block(root, "Crinkle" + i, new Vector3(-0.070f, 0.038f, z),
                    new Vector3(0.330f, 0.007f, 0.010f), glintMat);
                crease.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.9f) * 6f, 0f);
                Block(root, "CrinkleShade" + i, new Vector3(-0.070f, 0.032f, z + 0.013f),
                    new Vector3(0.320f, 0.006f, 0.008f), filmDeepMat);
            }

            // ── The bag's drawstring hem, kept as the strapless top edge and pulled tight ─────────────────
            Band("HemTube", new Vector3(0.158f, 0f, -0.092f), new Vector3(0.158f, 0f, 0.092f), 0.030f, 0.034f, 0.024f, filmMat);
            Band("HemTubeGlint", new Vector3(0.166f, 0f, -0.088f), new Vector3(0.166f, 0f, 0.088f), 0.008f, 0.048f, 0.006f, glintMat);
            for (int i = 0; i < 11; i++)
            {
                float z = (i / 10f - 0.5f) * 0.170f;
                Sphere(root, "Gather" + i, new Vector3(0.154f, 0.044f, z),
                    new Vector3(0f, Mathf.Cos(i * 1.6f) * 16f, 0f), new Vector3(0.030f, 0.016f, 0.016f), sheenMat);
                if (i % 2 == 0)
                    Block(root, "GatherShade" + i, new Vector3(0.146f, 0.036f, z + 0.008f),
                        new Vector3(0.018f, 0.008f, 0.009f), filmDeepMat);
            }
            // The drawstring itself, drawn out of the hem and knotted off to one side.
            Band("Drawstring", new Vector3(0.158f, 0f, -0.020f), new Vector3(0.158f, 0f, 0.070f), 0.009f, 0.050f, 0.008f, cordMat);
            var knot = new PartNode("DrawKnot");
            knot.SetParent(root, false);
            knot.localPosition = new Vector3(0.162f, 0.052f, 0.082f);
            knot.localEulerAngles = new Vector3(0f, -34f, 0f);
            Sphere(knot, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.024f, 0.016f, 0.022f), cordMat);
            Block(knot, "TailA", new Vector3(-0.030f, -0.002f, 0.014f), new Vector3(0.052f, 0.008f, 0.008f), cordMat);
            Block(knot, "TailB", new Vector3(-0.024f, -0.003f, -0.012f), new Vector3(0.040f, 0.007f, 0.007f), cordMat);

            // The dark inside of the tube, visible past the top edge.
            Sphere(root, "Inside", new Vector3(0.132f, 0.016f, 0f), Vector3.zero, new Vector3(0.050f, 0.030f, 0.150f), liningMat);

            // ── Parcel tape instead of seams: this is what is actually holding the dress together ─────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Band("TapeSeam" + side, new Vector3(0.140f, 0f, s * 0.094f), new Vector3(-0.280f, 0f, s * 0.108f),
                    0.030f, 0.040f, 0.010f, tapeMat);
                Band("TapeSeamEdge" + side, new Vector3(0.140f, 0f, s * 0.110f), new Vector3(-0.280f, 0f, s * 0.124f),
                    0.008f, 0.042f, 0.007f, tapeEdgeMat);
                // Tape goes on in overlapping strips, never one clean run.
                for (int i = 0; i < 4; i++)
                {
                    float x = Mathf.Lerp(0.110f, -0.240f, i / 3f);
                    var strip = Block(root, "TapeStrip" + side + i, new Vector3(x, 0.044f, s * (0.100f + i * 0.004f)),
                        new Vector3(0.090f, 0.007f, 0.034f), tapeMat);
                    strip.localEulerAngles = new Vector3(0f, s * (4f + i * 3f), 0f);
                }
            }
            // A patch of tape over a split across the middle, put on at an angle.
            var patch = new PartNode("TapePatch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(-0.108f, 0.042f, -0.048f);
            patch.localEulerAngles = new Vector3(0f, 34f, 0f);
            Block(patch, "Strip", Vector3.zero, new Vector3(0.120f, 0.008f, 0.040f), tapeMat);
            Block(patch, "StripEdgeA", new Vector3(0f, 0.005f, 0.020f), new Vector3(0.118f, 0.006f, 0.005f), tapeEdgeMat);
            Block(patch, "StripEdgeB", new Vector3(0f, 0.005f, -0.020f), new Vector3(0.118f, 0.006f, 0.005f), tapeEdgeMat);
            Block(patch, "SplitUnder", new Vector3(0f, -0.004f, 0f), new Vector3(0.096f, 0.006f, 0.012f), liningMat);

            // ── The bag's own gusset and base seams, still where the factory put them ─────────────────────
            Band("GussetCrease", new Vector3(0.130f, 0f, -0.034f), new Vector3(-0.270f, 0f, -0.040f), 0.012f, 0.040f, 0.008f, glintMat);
            Band("GussetShade", new Vector3(0.130f, 0f, -0.022f), new Vector3(-0.270f, 0f, -0.028f), 0.016f, 0.034f, 0.008f, filmDeepMat);
            Band("BaseSeam", new Vector3(-0.196f, 0f, -0.108f), new Vector3(-0.196f, 0f, 0.108f), 0.014f, 0.038f, 0.010f, filmDeepMat);

            // ── Printed markings off the sack: a recycling mark and a band of text ────────────────────────
            var mark = new PartNode("SackPrint");
            mark.SetParent(root, false);
            mark.localPosition = new Vector3(0.016f, 0.044f, 0.046f);
            mark.localEulerAngles = new Vector3(0f, -9f, 0f);
            for (int i = 0; i < 3; i++)
            {
                // Three chasing arrows set round a triangle.
                float deg = i * 120f;
                float rad = deg * Mathf.Deg2Rad;
                var arrow = Block(mark, "Arrow" + i,
                    new Vector3(0.020f * Mathf.Cos(rad), 0f, 0.020f * Mathf.Sin(rad)),
                    new Vector3(0.030f, 0.005f, 0.009f), printMat);
                arrow.localEulerAngles = new Vector3(0f, -deg + 30f, 0f);
            }
            for (int i = 0; i < 4; i++)
                Block(mark, "Text" + i, new Vector3(-0.048f, 0f, -0.026f + i * 0.018f),
                    new Vector3(0.024f, 0.005f, 0.011f), printMat);

            // ── Cut hem, already splitting: plastic tears rather than frays ───────────────────────────────
            Block(root, "HemCut", new Vector3(-0.298f, 0.006f, 0f), new Vector3(0.016f, 0.050f, 0.226f), filmDeepMat);
            Block(root, "HemCutGlint", new Vector3(-0.290f, 0.038f, 0f), new Vector3(0.007f, 0.007f, 0.220f), glintMat);
            // A few tears running up from the cut edge, each a narrow dark slit with a lifted lip.
            for (int i = 0; i < 4; i++)
            {
                float z = (i / 3f - 0.5f) * 0.170f;
                float len = 0.026f + Mathf.Abs(Mathf.Sin(i * 2.3f)) * 0.030f;
                var tear = Block(root, "Tear" + i, new Vector3(-0.294f + len * 0.5f, 0.036f, z),
                    new Vector3(len, 0.008f, 0.007f), liningMat);
                tear.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.9f) * 22f, 0f);
                var lip = Block(root, "TearLip" + i, new Vector3(-0.294f + len * 0.5f, 0.042f, z + 0.009f),
                    new Vector3(len * 0.9f, 0.006f, 0.006f), glintMat);
                lip.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.9f) * 22f, 0f);
            }
            // One corner that has curled up on itself, the way cut polythene does.
            var curl = new PartNode("HemCurl");
            curl.SetParent(root, false);
            curl.localPosition = new Vector3(-0.296f, 0.030f, 0.096f);
            curl.localEulerAngles = new Vector3(0f, 28f, 0f);
            Sphere(curl, "Roll", Vector3.zero, Vector3.zero, new Vector3(0.046f, 0.024f, 0.034f), filmMat);
            Block(curl, "RollGlint", new Vector3(0f, 0.014f, 0f), new Vector3(0.040f, 0.005f, 0.010f), glintMat);
            Block(curl, "RollUnder", new Vector3(-0.010f, -0.008f, 0f), new Vector3(0.030f, 0.006f, 0.026f), liningMat);

            return root;
        }

        /// <summary>
        /// A knee-length patchwork dress pieced together out of salvaged pale denim, laid flat face-up with
        /// shoulders at +X and hem at −X. It is the third and last way the set gets a dress out of old jeans,
        /// and the only one not made from a single garment: this is a grid of squares cut from whatever was
        /// left of several worn-out pairs and sewn edge to edge, so no two blocks quite match in shade and
        /// the twill in each one runs whichever way that scrap happened to lie. Several squares still carry
        /// the piece of their old garment they were cut from — a hem, a felled seam, a pocket corner with a
        /// rivet still in it — and one has worn through completely, leaving bare warp laddering the hole.
        /// </summary>
        private static PartNode BuildDressKneesCraftedDenimLight(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The same washed-out blue as the long light denim dress, so the two read as one cloth.
            Color washed = new Color(0.615f, 0.680f, 0.760f);
            Color fabric = Color.Lerp(washed, def.Tint, 0.10f);                                   // soft stonewashed denim
            Color fabricDeep = new Color(fabric.r * 0.70f, fabric.g * 0.73f, fabric.b * 0.80f);   // seam shadow and fold valleys
            Color cloud = Color.Lerp(fabric, new Color(0.86f, 0.89f, 0.93f), 0.60f);              // cloudy stonewash blotching
            Color bleach = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.89f), 0.80f);             // splashed bleach, near white
            Color warp = new Color(0.88f, 0.89f, 0.90f);                                          // bare threads laddering the hole
            Color topstitch = new Color(0.93f, 0.92f, 0.88f);                                     // white contrast thread
            Color brass = new Color(0.78f, 0.62f, 0.28f);                                         // the rivets left in the scraps
            Color lining = new Color(fabric.r * 0.48f, fabric.g * 0.48f, fabric.b * 0.52f);       // dark inside, seen through the hole
            Color tape = Color.Lerp(fabric, new Color(0.52f, 0.58f, 0.68f), 0.55f);               // binding and the wrap ties

            // Five shades for the blocks, so the patchwork never falls into a pattern.
            PartMat[] blockMats =
            {
                PartMat.Of(fabric, smoothness: 0.13f),
                PartMat.Of(Color.Lerp(fabric, new Color(0.50f, 0.57f, 0.67f), 0.55f), smoothness: 0.12f),
                PartMat.Of(Color.Lerp(fabric, new Color(0.78f, 0.82f, 0.87f), 0.50f), smoothness: 0.14f),
                PartMat.Of(Color.Lerp(fabric, new Color(0.42f, 0.50f, 0.62f), 0.45f), smoothness: 0.12f),
                PartMat.Of(Color.Lerp(fabric, new Color(0.68f, 0.71f, 0.74f), 0.40f), smoothness: 0.13f),
            };
            PartMat fabricMat = blockMats[0];
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.10f);
            PartMat cloudMat = PartMat.Of(cloud, smoothness: 0.15f);
            PartMat bleachMat = PartMat.Of(bleach, smoothness: 0.16f);
            PartMat warpMat = PartMat.Of(warp, smoothness: 0.09f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.14f);
            PartMat brassMat = PartMat.Of(brass, smoothness: 0.78f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat tapeMat = PartMat.Of(tape, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // block seams, the binding and the ties, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: the usual knee-length silhouette, but every mound is only a backing for the blocks ──
            Sphere(root, "Shoulders", new Vector3(0.148f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.050f, 0.216f), fabricDeepMat);
            Sphere(root, "Bodice", new Vector3(0.062f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.230f), fabricDeepMat);
            Sphere(root, "Waist", new Vector3(-0.052f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.050f, 0.190f), fabricDeepMat);
            Sphere(root, "SkirtTop", new Vector3(-0.160f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.268f), fabricDeepMat);
            Sphere(root, "SkirtMid", new Vector3(-0.272f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.330f), fabricDeepMat);
            Sphere(root, "SkirtHem", new Vector3(-0.372f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.046f, 0.372f), fabricDeepMat);

            // ── The patchwork itself: a grid of squares, each its own shade, each with its own wale ───────
            // The grid widens as it goes down the dress so the blocks follow the flare of the skirt.
            const int rows = 7, cols = 5;
            for (int r = 0; r < rows; r++)
            {
                float x = Mathf.Lerp(0.150f, -0.400f, r / (rows - 1f));
                float halfZ = Mathf.Lerp(0.090f, 0.172f, r / (rows - 1f));
                float bw = 0.076f;                                   // block length along X
                float bd = halfZ * 2f / cols;                        // block width along Z at this row

                for (int c = 0; c < cols; c++)
                {
                    float z = (c + 0.5f) / cols * halfZ * 2f - halfZ;
                    int pick = (r * 3 + c * 2 + (r * c) % 3) % blockMats.Length;   // shuffled so no two neighbours match
                    var block = new PartNode("Block" + r + "_" + c);
                    block.SetParent(root, false);
                    block.localPosition = new Vector3(x, 0.030f, z);
                    block.localEulerAngles = new Vector3(0f, Mathf.Sin(r * 2.1f + c) * 4f, 0f);   // each sewn on slightly askew

                    Block(block, "Cloth", Vector3.zero, new Vector3(bw, 0.012f, bd * 0.94f), blockMats[pick]);
                    // The scrap's twill, running whichever way this piece happened to be cut.
                    float wale = ((r + c) % 2 == 0) ? 63f : -63f;
                    for (int w = 0; w < 3; w++)
                    {
                        var line = Block(block, "Wale" + w, new Vector3((w - 1) * 0.024f, 0.007f, 0f),
                            new Vector3(0.005f, 0.005f, bd * 0.80f), cloudMat);
                        line.localEulerAngles = new Vector3(0f, wale, 0f);
                    }
                }
            }

            // ── Every block edge is a seam, and all of them are topstitched in white ──────────────────────
            for (int r = 0; r < rows - 1; r++)
            {
                float x = Mathf.Lerp(0.150f, -0.400f, (r + 0.5f) / (rows - 1f));
                float halfZ = Mathf.Lerp(0.090f, 0.172f, (r + 0.5f) / (rows - 1f));
                Band("RowSeam" + r, new Vector3(x, 0f, -halfZ), new Vector3(x, 0f, halfZ), 0.006f, 0.040f, 0.005f, topstitchMat);
                Band("RowRidge" + r, new Vector3(x, 0f, -halfZ), new Vector3(x, 0f, halfZ), 0.018f, 0.034f, 0.010f, fabricDeepMat);
            }
            for (int c = 1; c < cols; c++)
            {
                float t = c / (float)cols;
                Band("ColSeam" + c,
                    new Vector3(0.150f, 0f, (t - 0.5f) * 0.180f), new Vector3(-0.400f, 0f, (t - 0.5f) * 0.344f),
                    0.006f, 0.040f, 0.005f, topstitchMat);
                Band("ColRidge" + c,
                    new Vector3(0.150f, 0f, (t - 0.5f) * 0.180f), new Vector3(-0.400f, 0f, (t - 0.5f) * 0.344f),
                    0.018f, 0.034f, 0.010f, fabricDeepMat);
            }

            // ── Blocks that still carry the piece of the old garment they were cut from ───────────────────
            // A square cut across somebody's leg hem, the double stitching still in it.
            var hemScrap = new PartNode("ScrapLegHem");
            hemScrap.SetParent(root, false);
            hemScrap.localPosition = new Vector3(-0.058f, 0.038f, 0.104f);
            hemScrap.localEulerAngles = new Vector3(0f, 3f, 0f);
            Block(hemScrap, "Fold", Vector3.zero, new Vector3(0.024f, 0.010f, 0.062f), cloudMat);
            Block(hemScrap, "StitchA", new Vector3(0.008f, 0.006f, 0f), new Vector3(0.005f, 0.005f, 0.060f), topstitchMat);
            Block(hemScrap, "StitchB", new Vector3(-0.008f, 0.006f, 0f), new Vector3(0.005f, 0.005f, 0.060f), topstitchMat);

            // A square with a felled seam running across it, cut clean through at both ends.
            var seamScrap = new PartNode("ScrapFelledSeam");
            seamScrap.SetParent(root, false);
            seamScrap.localPosition = new Vector3(-0.238f, 0.038f, -0.086f);
            seamScrap.localEulerAngles = new Vector3(0f, 28f, 0f);
            Block(seamScrap, "Ridge", Vector3.zero, new Vector3(0.078f, 0.010f, 0.020f), fabricMat);
            Block(seamScrap, "StitchA", new Vector3(0f, 0.006f, 0.006f), new Vector3(0.076f, 0.005f, 0.005f), topstitchMat);
            Block(seamScrap, "StitchB", new Vector3(0f, 0.006f, -0.006f), new Vector3(0.076f, 0.005f, 0.005f), topstitchMat);

            // A square taken off a pocket corner, with the rivet still in it.
            var pocketScrap = new PartNode("ScrapPocketCorner");
            pocketScrap.SetParent(root, false);
            pocketScrap.localPosition = new Vector3(-0.150f, 0.038f, -0.112f);
            pocketScrap.localEulerAngles = new Vector3(0f, -14f, 0f);
            Block(pocketScrap, "Corner", Vector3.zero, new Vector3(0.046f, 0.010f, 0.040f), cloudMat);
            Block(pocketScrap, "EdgeA", new Vector3(0.021f, 0.006f, 0f), new Vector3(0.006f, 0.005f, 0.038f), topstitchMat);
            Block(pocketScrap, "EdgeB", new Vector3(0f, 0.006f, 0.018f), new Vector3(0.044f, 0.005f, 0.005f), topstitchMat);
            Sphere(pocketScrap, "Rivet", new Vector3(0.016f, 0.008f, 0.013f), Vector3.zero, new Vector3(0.013f, 0.008f, 0.013f), brassMat);

            // A square with a belt loop still attached, standing off the cloth.
            var loopScrap = new PartNode("ScrapBeltLoop");
            loopScrap.SetParent(root, false);
            loopScrap.localPosition = new Vector3(0.096f, 0.038f, -0.062f);
            loopScrap.localEulerAngles = new Vector3(0f, 9f, 0f);
            Block(loopScrap, "Loop", Vector3.zero, new Vector3(0.040f, 0.009f, 0.018f), fabricMat);
            Block(loopScrap, "BarA", new Vector3(0.018f, 0.006f, 0f), new Vector3(0.007f, 0.005f, 0.018f), topstitchMat);
            Block(loopScrap, "BarB", new Vector3(-0.018f, 0.006f, 0f), new Vector3(0.007f, 0.005f, 0.018f), topstitchMat);

            // ── One block worn clean through, with bare warp laddering across the hole ────────────────────
            var hole = new PartNode("WornBlock");
            hole.SetParent(root, false);
            hole.localPosition = new Vector3(-0.318f, 0.034f, 0.128f);
            hole.localEulerAngles = new Vector3(0f, -11f, 0f);
            Block(hole, "Void", Vector3.zero, new Vector3(0.058f, 0.014f, 0.050f), liningMat);
            for (int i = 0; i < 10; i++)
            {
                float deg = i * 36f;
                float rad = deg * Mathf.Deg2Rad;
                var lip = Block(hole, "Lip" + i,
                    new Vector3(0.034f * Mathf.Cos(rad), 0.006f, 0.032f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.010f, 0.013f), cloudMat);
                lip.localEulerAngles = new Vector3(0f, -deg, 0f);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = (i / 5f - 0.5f) * 0.050f;
                var rung = Block(hole, "Ladder" + i, new Vector3(x, 0.010f, Mathf.Sin(i * 1.9f) * 0.004f),
                    new Vector3(0.005f, 0.004f, 0.054f + (i % 3) * 0.005f), warpMat);
                rung.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 2.4f) * 8f, 0f);
            }
            Block(hole, "Snapped", new Vector3(0.024f, 0.012f, 0.036f), new Vector3(0.004f, 0.004f, 0.022f), warpMat);

            // ── Neckline and armholes bound in tape, since a raw patchwork edge would not hold ────────────
            Sphere(root, "Neckline", new Vector3(0.154f, 0.014f, 0f), Vector3.zero, new Vector3(0.080f, 0.034f, 0.122f), liningMat);
            const int bindings = 14;
            for (int i = 0; i < bindings; i++)
            {
                float deg = i * 360f / bindings;
                float rad = deg * Mathf.Deg2Rad;
                var bind = Block(root, "Binding" + i,
                    new Vector3(0.154f + 0.046f * Mathf.Cos(rad), 0.030f, 0.072f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.026f, 0.012f), tapeMat);
                bind.localEulerAngles = new Vector3(0f, -deg, 0f);
                var st = Block(root, "BindStitch" + i,
                    new Vector3(0.154f + 0.058f * Mathf.Cos(rad), 0.044f, 0.090f * Mathf.Sin(rad)),
                    new Vector3(0.012f, 0.005f, 0.006f), topstitchMat);
                st.localEulerAngles = new Vector3(0f, -deg, 0f);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var arm = new PartNode("ArmHole" + side);
                arm.SetParent(root, false);
                arm.localPosition = new Vector3(0.092f, 0.016f, s * 0.118f);
                arm.localEulerAngles = new Vector3(0f, s * -18f, 0f);
                Sphere(arm, "Void", Vector3.zero, Vector3.zero, new Vector3(0.096f, 0.030f, 0.062f), liningMat);
                for (int i = 0; i < 12; i++)
                {
                    float deg = i * 30f;
                    float rad = deg * Mathf.Deg2Rad;
                    var bind = Block(arm, "Bind" + i,
                        new Vector3(0.054f * Mathf.Cos(rad), 0.012f, 0.038f * Mathf.Sin(rad)),
                        new Vector3(0.016f, 0.026f, 0.011f), tapeMat);
                    bind.localEulerAngles = new Vector3(0f, -deg, 0f);
                    var st = Block(arm, "BindStitch" + i,
                        new Vector3(0.066f * Mathf.Cos(rad), 0.026f, 0.050f * Mathf.Sin(rad)),
                        new Vector3(0.011f, 0.005f, 0.005f), topstitchMat);
                    st.localEulerAngles = new Vector3(0f, -deg, 0f);
                }
            }

            // ── Wrap ties in the same tape, brought round from the sides to knot at the front ─────────────
            for (int s = -1; s <= 1; s += 2)
                Band(s > 0 ? "TieL" : "TieR",
                    new Vector3(-0.072f, 0f, s * 0.080f), new Vector3(-0.052f, 0f, s * 0.184f), 0.020f, 0.040f, 0.010f, tapeMat);
            var bow = new PartNode("Bow");
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(-0.066f, 0.046f, 0.146f);
            bow.localEulerAngles = new Vector3(0f, -32f, 0f);
            Sphere(bow, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.028f, 0.018f, 0.024f), tapeMat);
            Sphere(bow, "LoopA", new Vector3(0.026f, -0.002f, 0.024f), new Vector3(0f, 34f, 0f), new Vector3(0.048f, 0.012f, 0.026f), tapeMat);
            Block(bow, "TailA", new Vector3(-0.050f, -0.003f, 0.012f), new Vector3(0.078f, 0.009f, 0.018f), tapeMat);
            Block(bow, "TailB", new Vector3(-0.040f, -0.004f, -0.016f), new Vector3(0.062f, 0.008f, 0.016f), fabricDeepMat);

            // ── Bleach splashes across several blocks at once, which is how they read as later damage ─────
            Vector2[] splashes =
            {
                new Vector2(-0.096f,  0.128f), new Vector2(-0.128f,  0.152f),
                new Vector2(-0.344f, -0.128f), new Vector2(-0.372f, -0.158f), new Vector2( 0.040f, -0.116f),
            };
            for (int i = 0; i < splashes.Length; i++)
            {
                float d = 0.020f + (i % 3) * 0.012f;
                Sphere(root, "BleachSpot" + i, new Vector3(splashes[i].x, 0.042f, splashes[i].y), Vector3.zero,
                    new Vector3(d, 0.008f, d * 0.82f), bleachMat);
            }

            // Washed-soft folds: rounded, not the hard creases the stiff denims get.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f * 2f - 1f;
                Sphere(root, "Fold" + i, new Vector3(-0.280f, 0.040f, t * 0.130f),
                    new Vector3(0f, t * 11f, 0f), new Vector3(0.250f, 0.012f, 0.034f), cloudMat);
                Sphere(root, "FoldShade" + i, new Vector3(-0.288f, 0.035f, t * 0.130f + 0.028f),
                    new Vector3(0f, t * 11f, 0f), new Vector3(0.230f, 0.010f, 0.020f), fabricDeepMat);
            }

            // ── Hem: a strip of binding run right round the bottom row, since patchwork cannot be turned ──
            Block(root, "HemBind", new Vector3(-0.420f, 0.006f, 0f), new Vector3(0.024f, 0.048f, 0.348f), tapeMat);
            Block(root, "HemBindStitch", new Vector3(-0.404f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.340f), topstitchMat);
            Block(root, "HemBindEdge", new Vector3(-0.434f, 0.004f, 0f), new Vector3(0.010f, 0.044f, 0.348f), fabricDeepMat);

            // A knotted tape hanging loop at the back neck, as on the rest of the crafted dresses.
            var loop = new PartNode("HangLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(0.152f, 0.044f, 0.062f);
            loop.localEulerAngles = new Vector3(0f, 24f, 0f);
            Block(loop, "LoopA", new Vector3(0f, 0f, -0.011f), new Vector3(0.030f, 0.005f, 0.006f), tapeMat);
            Block(loop, "LoopB", new Vector3(0f, 0f, 0.011f), new Vector3(0.030f, 0.005f, 0.006f), tapeMat);
            Block(loop, "LoopEnd", new Vector3(0.016f, 0f, 0f), new Vector3(0.006f, 0.005f, 0.026f), tapeMat);

            return root;
        }

        /// <summary>
        /// A knee-length black denim dress made out of a trucker jacket, laid flat face-up with shoulders at
        /// +X and hem at −X. The knee-length indigo dress was a pair of jeans turned upside down; this one is
        /// the other obvious way to get a dress out of denim, and looks nothing like it as a result. The
        /// jacket is kept from the shoulders down: its buttoned front placket runs the length of the body,
        /// both flap chest pockets are still on it with the pleats either side of them, and the sleeves have
        /// been cut off at the shoulder seam. Its waist band was unpicked and a gathered skirt hung off the
        /// raw edge instead. Being black denim, every cut and every rubbed seam has gone pale and grey.
        /// </summary>
        private static PartNode BuildDressKneesCraftedDenimBlack(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The same sulphur black as the long black denim dress, so the two read as one cloth.
            Color jet = new Color(0.135f, 0.130f, 0.145f);
            Color fabric = Color.Lerp(jet, def.Tint, 0.08f);                                      // the jacket, surface-dyed black
            Color fabricDeep = new Color(fabric.r * 0.58f, fabric.g * 0.58f, fabric.b * 0.64f);   // seam shadow, the darkest note
            Color skirtCloth = Color.Lerp(fabric, new Color(0.28f, 0.27f, 0.30f), 0.55f);         // the skirt, off a more faded pair
            Color twill = Color.Lerp(fabric, new Color(0.34f, 0.33f, 0.37f), 0.45f);              // the diagonal wale, only just visible
            Color crock = Color.Lerp(fabric, new Color(0.52f, 0.51f, 0.53f), 0.62f);              // grey where the dye has rubbed off
            Color abrade = Color.Lerp(fabric, new Color(0.68f, 0.67f, 0.70f), 0.70f);             // silvered high points
            Color weft = new Color(0.72f, 0.70f, 0.68f);                                          // the pale weft at every cut edge
            Color topstitch = Color.Lerp(fabric, new Color(0.40f, 0.39f, 0.38f), 0.55f);          // tonal thread, not gold
            Color gunmetal = new Color(0.30f, 0.30f, 0.33f);                                      // darkened jacket buttons and rivets
            Color lining = new Color(fabric.r * 0.50f, fabric.g * 0.50f, fabric.b * 0.55f);       // the inside, seen through the neck

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.07f);   // black denim kills highlights
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.05f);
            PartMat skirtMat = PartMat.Of(skirtCloth, smoothness: 0.08f);
            PartMat twillMat = PartMat.Of(twill, smoothness: 0.10f);
            PartMat crockMat = PartMat.Of(crock, smoothness: 0.09f);
            PartMat abradeMat = PartMat.Of(abrade, smoothness: 0.14f);
            PartMat weftMat = PartMat.Of(weft, smoothness: 0.08f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.12f);
            PartMat gunmetalMat = PartMat.Of(gunmetal, smoothness: 0.66f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.05f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // yoke seams, the placket and the fade lines, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── The jacket body, boxy and square-shouldered the way a trucker jacket is cut ───────────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.058f, 0.244f), fabricMat);
            Sphere(root, "Chest", new Vector3(0.062f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.060f, 0.250f), fabricMat);
            Sphere(root, "Body", new Vector3(-0.050f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.058f, 0.242f), fabricMat);
            // ── and the skirt hung off it, in a lighter, more worn black ──────────────────────────────────
            Sphere(root, "SkirtTop", new Vector3(-0.176f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.052f, 0.276f), skirtMat);
            Sphere(root, "SkirtMid", new Vector3(-0.286f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.052f, 0.330f), skirtMat);
            Sphere(root, "SkirtLow", new Vector3(-0.386f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.048f, 0.360f), skirtMat);

            // ── Twill wale across the whole dress, pitched low because black shows it barely at all ───────
            for (int i = 0; i < 22; i++)
            {
                float x = Mathf.Lerp(0.150f, -0.420f, i / 21f);
                float halfZ = Mathf.Lerp(0.108f, 0.156f, i / 21f);
                var wale = Block(root, "Wale" + i, new Vector3(x, 0.034f, 0f),
                    new Vector3(0.006f, 0.005f, halfZ * 2.05f), twillMat);
                wale.localEulerAngles = new Vector3(0f, 63f, 0f);
            }

            // ── The jacket's buttoned front placket, running the length of the body ───────────────────────
            Band("Placket", new Vector3(0.176f, 0f, -0.018f), new Vector3(-0.120f, 0f, -0.018f), 0.048f, 0.034f, 0.014f, fabricMat);
            Band("PlacketEdge", new Vector3(0.176f, 0f, -0.044f), new Vector3(-0.120f, 0f, -0.044f), 0.010f, 0.038f, 0.010f, fabricDeepMat);
            Band("PlacketStitchA", new Vector3(0.174f, 0f, 0.004f), new Vector3(-0.118f, 0f, 0.004f), 0.006f, 0.044f, 0.005f, topstitchMat);
            Band("PlacketStitchB", new Vector3(0.174f, 0f, -0.040f), new Vector3(-0.118f, 0f, -0.040f), 0.006f, 0.044f, 0.005f, topstitchMat);
            // The placket stands proud, so it is the first thing to rub grey.
            Band("PlacketCrock", new Vector3(0.172f, 0f, -0.018f), new Vector3(-0.116f, 0f, -0.018f), 0.014f, 0.046f, 0.005f, crockMat);
            const int buttons = 5;
            for (int i = 0; i < buttons; i++)
            {
                float x = Mathf.Lerp(0.156f, -0.104f, i / (buttons - 1f));
                Sphere(root, "Button" + i, new Vector3(x, 0.046f, -0.018f), Vector3.zero,
                    new Vector3(0.022f, 0.011f, 0.022f), gunmetalMat);
                Sphere(root, "ButtonDish" + i, new Vector3(x, 0.052f, -0.018f), Vector3.zero,
                    new Vector3(0.011f, 0.005f, 0.011f), fabricDeepMat);
                // A worn halo round each one where the metal has chafed the dye away.
                Sphere(root, "ButtonHalo" + i, new Vector3(x, 0.042f, -0.018f), Vector3.zero,
                    new Vector3(0.038f, 0.008f, 0.038f), crockMat);
            }

            // ── Collar, folded flat back on itself at the top ─────────────────────────────────────────────
            Sphere(root, "Neckline", new Vector3(0.150f, 0.014f, 0f), Vector3.zero, new Vector3(0.074f, 0.036f, 0.118f), liningMat);
            Block(root, "CollarStand", new Vector3(0.192f, 0.036f, 0f), new Vector3(0.026f, 0.014f, 0.170f), fabricMat);
            Block(root, "CollarLeaf", new Vector3(0.222f, 0.034f, 0f), new Vector3(0.044f, 0.012f, 0.184f), fabricMat);
            Block(root, "CollarStitch", new Vector3(0.238f, 0.042f, 0f), new Vector3(0.006f, 0.006f, 0.176f), topstitchMat);
            Block(root, "CollarCrock", new Vector3(0.242f, 0.041f, 0f), new Vector3(0.010f, 0.006f, 0.180f), crockMat);
            for (int s = -1; s <= 1; s += 2)
            {
                var point = Block(root, s > 0 ? "CollarPointL" : "CollarPointR",
                    new Vector3(0.230f, 0.036f, s * 0.086f), new Vector3(0.052f, 0.012f, 0.040f), fabricMat);
                point.localEulerAngles = new Vector3(0f, s * 22f, 0f);
            }

            // ── Chest flap pockets, with the jacket's signature pleat either side of each ─────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var chest = new PartNode("ChestPocket" + side);
                chest.SetParent(root, false);
                chest.localPosition = new Vector3(0.072f, 0.036f, s * 0.108f);
                chest.localEulerAngles = new Vector3(0f, s * -4f, 0f);
                Block(chest, "Pocket", Vector3.zero, new Vector3(0.086f, 0.011f, 0.076f), fabricMat);
                Block(chest, "PocketStitch", new Vector3(-0.040f, 0.007f, 0f), new Vector3(0.006f, 0.005f, 0.072f), topstitchMat);
                // The flap, buttoned down over the mouth.
                Block(chest, "Flap", new Vector3(0.030f, 0.008f, 0f), new Vector3(0.038f, 0.011f, 0.078f), fabricMat);
                Block(chest, "FlapEdge", new Vector3(0.012f, 0.010f, 0f), new Vector3(0.008f, 0.008f, 0.078f), fabricDeepMat);
                Block(chest, "FlapStitch", new Vector3(0.020f, 0.014f, 0f), new Vector3(0.006f, 0.005f, 0.072f), topstitchMat);
                Block(chest, "FlapCrock", new Vector3(0.011f, 0.015f, 0f), new Vector3(0.009f, 0.005f, 0.074f), crockMat);
                Sphere(chest, "FlapButton", new Vector3(0.016f, 0.016f, 0f), Vector3.zero, new Vector3(0.018f, 0.009f, 0.018f), gunmetalMat);
                // The two knife pleats that run down past the pocket on a trucker jacket front.
                for (int p = -1; p <= 1; p += 2)
                {
                    var pleat = Block(root, (s > 0 ? "PleatL" : "PleatR") + (p > 0 ? "A" : "B"),
                        new Vector3(0.020f, 0.040f, s * (0.076f + (p > 0 ? 0.070f : 0f))),
                        new Vector3(0.230f, 0.008f, 0.014f), twillMat);
                    pleat.localEulerAngles = new Vector3(0f, s * 2f, 0f);
                    Block(root, (s > 0 ? "PleatShadeL" : "PleatShadeR") + (p > 0 ? "A" : "B"),
                        new Vector3(0.020f, 0.034f, s * (0.076f + (p > 0 ? 0.070f : 0f)) + 0.012f),
                        new Vector3(0.220f, 0.006f, 0.008f), fabricDeepMat);
                }
            }

            // ── Sleeves cut off at the shoulder seam: raw armholes showing the pale weft all round ────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var arm = new PartNode("ArmHole" + side);
                arm.SetParent(root, false);
                arm.localPosition = new Vector3(0.098f, 0.016f, s * 0.132f);
                arm.localEulerAngles = new Vector3(0f, s * -20f, 0f);
                Sphere(arm, "Void", Vector3.zero, Vector3.zero, new Vector3(0.100f, 0.032f, 0.066f), liningMat);
                for (int i = 0; i < 12; i++)
                {
                    float deg = i * 30f;
                    float rad = deg * Mathf.Deg2Rad;
                    var cut = Block(arm, "Cut" + i,
                        new Vector3(0.056f * Mathf.Cos(rad), 0.012f, 0.040f * Mathf.Sin(rad)),
                        new Vector3(0.018f, 0.022f, 0.011f), weftMat);
                    cut.localEulerAngles = new Vector3(0f, -deg, 0f);
                    // The old armhole seam, left in place just inside the cut.
                    var seam = Block(arm, "OldSeam" + i,
                        new Vector3(0.044f * Mathf.Cos(rad), 0.020f, 0.030f * Mathf.Sin(rad)),
                        new Vector3(0.012f, 0.005f, 0.006f), topstitchMat);
                    seam.localEulerAngles = new Vector3(0f, -deg, 0f);
                }
            }

            // ── Where the jacket's waist band was unpicked, and the skirt gathered on to the raw edge ─────
            Band("JoinRidge", new Vector3(-0.126f, 0f, -0.116f), new Vector3(-0.126f, 0f, 0.116f), 0.026f, 0.032f, 0.014f, fabricDeepMat);
            Band("JoinWeft", new Vector3(-0.120f, 0f, -0.114f), new Vector3(-0.120f, 0f, 0.114f), 0.010f, 0.042f, 0.006f, weftMat);
            Band("JoinStitch", new Vector3(-0.132f, 0f, -0.114f), new Vector3(-0.132f, 0f, 0.114f), 0.006f, 0.044f, 0.005f, topstitchMat);
            for (int i = 0; i < 13; i++)
            {
                float z = (i / 12f - 0.5f) * 0.220f;
                Sphere(root, "Gather" + i, new Vector3(-0.150f, 0.034f, z),
                    new Vector3(0f, Mathf.Sin(i * 1.5f) * 11f, 0f), new Vector3(0.048f, 0.014f, 0.020f), twillMat);
                Sphere(root, "GatherShade" + i, new Vector3(-0.156f, 0.028f, z + 0.015f), Vector3.zero,
                    new Vector3(0.040f, 0.011f, 0.011f), fabricDeepMat);
            }
            // The needle holes left behind where the band used to be stitched on.
            for (int i = 0; i < 16; i++)
            {
                float z = (i / 15f - 0.5f) * 0.222f;
                Block(root, "OldBandHole" + i, new Vector3(-0.112f, 0.040f, z), new Vector3(0.007f, 0.005f, 0.005f), crockMat);
            }

            // ── Crocking: the grey the dye rubs away to, wherever the cloth gets handled ──────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band(s > 0 ? "SideSeamL" : "SideSeamR",
                    new Vector3(0.140f, 0f, s * 0.114f), new Vector3(-0.124f, 0f, s * 0.112f), 0.006f, 0.040f, 0.005f, topstitchMat);
                Band(s > 0 ? "SideCrockL" : "SideCrockR",
                    new Vector3(0.138f, 0f, s * 0.114f), new Vector3(-0.122f, 0f, s * 0.112f), 0.009f, 0.044f, 0.005f, crockMat);
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.190f - i * 0.030f;
                    Band((s > 0 ? "CrockLineL" : "CrockLineR") + i,
                        new Vector3(x, 0f, s * 0.040f), new Vector3(x - 0.050f, 0f, s * (0.108f + i * 0.010f)),
                        0.011f, 0.038f, 0.006f, crockMat);
                }
            }
            Sphere(root, "ElbowCrock", new Vector3(0.056f, 0.040f, -0.086f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.011f, 0.058f), crockMat);

            // Hard creases with silvered abrasion along their crowns — the high points rub first and lightest.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f * 2f - 1f;
                var ridge = Block(root, "Crease" + i, new Vector3(-0.286f + Mathf.Abs(t) * 0.040f, 0.038f, t * 0.120f),
                    new Vector3(0.240f, 0.010f, 0.014f), twillMat);
                ridge.localEulerAngles = new Vector3(0f, t * 10f, 0f);
                var shine = Block(root, "CreaseAbrade" + i, new Vector3(-0.286f + Mathf.Abs(t) * 0.040f, 0.044f, t * 0.120f),
                    new Vector3(0.210f, 0.005f, 0.007f), abradeMat);
                shine.localEulerAngles = new Vector3(0f, t * 10f, 0f);
                var valley = Block(root, "CreaseShade" + i, new Vector3(-0.292f + Mathf.Abs(t) * 0.040f, 0.032f, t * 0.120f + 0.024f),
                    new Vector3(0.224f, 0.008f, 0.010f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, t * 10f, 0f);
            }

            // ── Raw cut hem on the skirt, fraying pale against the black ──────────────────────────────────
            Block(root, "HemCut", new Vector3(-0.440f, 0.004f, 0f), new Vector3(0.018f, 0.048f, 0.348f), weftMat);
            Block(root, "HemStay", new Vector3(-0.420f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.340f), topstitchMat);
            for (int i = 0; i < 15; i++)
            {
                float z = (i / 14f - 0.5f) * 0.336f;
                float len = 0.016f + Mathf.Abs(Mathf.Sin(i * 2.8f)) * 0.028f;   // every thread a different length
                var thread = Block(root, "FrayThread" + i, new Vector3(-0.452f - len * 0.5f, 0.026f, z),
                    new Vector3(len, 0.005f, 0.004f), weftMat);
                thread.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.8f) * 24f, 0f);
            }

            // The jacket's woven size tab, still stitched inside the back of the collar.
            var tab = new PartNode("SizeTab");
            tab.SetParent(root, false);
            tab.localPosition = new Vector3(0.196f, 0.046f, 0.058f);
            tab.localEulerAngles = new Vector3(0f, 16f, 0f);
            Block(tab, "TabCloth", Vector3.zero, new Vector3(0.024f, 0.005f, 0.030f), crockMat);
            Block(tab, "TabInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.012f, 0.004f, 0.016f), fabricDeepMat);

            return root;
        }

        /// <summary>
        /// A knee-length indigo denim dress made by turning a pair of jeans upside down, laid flat face-up
        /// with shoulders at +X and hem at −X. Where the long denim dress was cut into panels and rebuilt,
        /// this one keeps the jeans nearly intact and simply wears them the other way up: the waistband has
        /// become the neckband and still carries its belt loops, the button fly runs down the front as the
        /// closure, and the back yoke and its seams sit where they always did. The legs have been split open
        /// and dropped to make the skirt, which leaves two gaps at the sides — filled with triangular godets
        /// let in from a darker pair — and the opened flat-felled inseam running right down to the hem.
        /// </summary>
        private static PartNode BuildDressKneesCraftedDenim(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The same indigo as the long denim dress, so the two read as the same cloth.
            Color indigo = new Color(0.225f, 0.310f, 0.455f);
            Color fabric = Color.Lerp(indigo, def.Tint, 0.10f);                                   // heavy indigo denim
            Color fabricDeep = new Color(fabric.r * 0.66f, fabric.g * 0.68f, fabric.b * 0.74f);   // seam shadow, crease valleys
            Color godet = Color.Lerp(fabric, new Color(0.14f, 0.19f, 0.30f), 0.55f);              // the darker pair the godets came from
            Color twill = Color.Lerp(fabric, new Color(0.52f, 0.60f, 0.72f), 0.38f);              // the diagonal wale
            Color whisker = Color.Lerp(fabric, new Color(0.72f, 0.77f, 0.84f), 0.55f);            // worn pale fade lines
            Color topstitch = new Color(0.80f, 0.64f, 0.28f);                                     // gold contrast thread
            Color copper = new Color(0.72f, 0.46f, 0.22f);                                        // rivets and the fly button
            Color steel = new Color(0.68f, 0.70f, 0.73f);                                         // the fly zip
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.46f);       // dark inside, seen through the neck
            Color label = new Color(0.55f, 0.36f, 0.22f);                                         // the leather patch, still on the band

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.09f);   // dry denim
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.07f);
            PartMat godetMat = PartMat.Of(godet, smoothness: 0.08f);
            PartMat twillMat = PartMat.Of(twill, smoothness: 0.12f);
            PartMat whiskerMat = PartMat.Of(whisker, smoothness: 0.10f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.16f);
            PartMat copperMat = PartMat.Of(copper, smoothness: 0.72f);
            PartMat steelMat = PartMat.Of(steel, smoothness: 0.80f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.06f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.35f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // felled seams, the godet joins and the fade lines, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: narrow across the top where the jeans' waist was, spreading out down the legs ───────
            Sphere(root, "Neckband", new Vector3(0.152f, 0f, 0f), Vector3.zero, new Vector3(0.09f, 0.058f, 0.196f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.062f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.058f, 0.222f), fabricMat);
            Sphere(root, "Hip", new Vector3(-0.070f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.058f, 0.268f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.200f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.056f, 0.310f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.318f, -0.002f, 0f), Vector3.zero, new Vector3(0.16f, 0.054f, 0.330f), fabricMat);

            // ── Twill wale: the same right-hand lean the whole denim family shares ────────────────────────
            for (int i = 0; i < 20; i++)
            {
                float x = Mathf.Lerp(0.150f, -0.350f, i / 19f);
                float halfZ = Mathf.Lerp(0.084f, 0.152f, i / 19f);
                var wale = Block(root, "Wale" + i, new Vector3(x, 0.034f, 0f),
                    new Vector3(0.006f, 0.005f, halfZ * 2.05f), twillMat);
                wale.localEulerAngles = new Vector3(0f, 63f, 0f);
            }

            // ── The jeans' waistband, now the neckband, with its belt loops still on it ───────────────────
            Band("Waistband", new Vector3(0.196f, 0f, -0.088f), new Vector3(0.196f, 0f, 0.088f), 0.040f, 0.034f, 0.026f, fabricMat);
            Band("WaistbandStitchA", new Vector3(0.212f, 0f, -0.086f), new Vector3(0.212f, 0f, 0.086f), 0.006f, 0.046f, 0.005f, topstitchMat);
            Band("WaistbandStitchB", new Vector3(0.180f, 0f, -0.086f), new Vector3(0.180f, 0f, 0.086f), 0.006f, 0.046f, 0.005f, topstitchMat);
            for (int i = 0; i < 5; i++)
            {
                float z = (i / 4f - 0.5f) * 0.150f;
                Block(root, "BeltLoop" + i, new Vector3(0.196f, 0.050f, z), new Vector3(0.048f, 0.008f, 0.020f), fabricMat);
                Block(root, "LoopBarA" + i, new Vector3(0.216f, 0.054f, z), new Vector3(0.008f, 0.005f, 0.020f), topstitchMat);
                Block(root, "LoopBarB" + i, new Vector3(0.176f, 0.054f, z), new Vector3(0.008f, 0.005f, 0.020f), topstitchMat);
            }
            // The neck opening itself, cut through the band's original top edge.
            Sphere(root, "Neckline", new Vector3(0.146f, 0.014f, 0f), Vector3.zero, new Vector3(0.070f, 0.036f, 0.112f), liningMat);

            // ── Button fly, kept whole and now running down the front as the closure ──────────────────────
            var fly = new PartNode("Fly");
            fly.SetParent(root, false);
            fly.localPosition = new Vector3(0.048f, 0.036f, -0.026f);
            Block(fly, "FlyPlacket", Vector3.zero, new Vector3(0.190f, 0.010f, 0.046f), fabricMat);
            // The curved topstitch line round the fly, traced as short straight runs.
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                float x = Mathf.Lerp(0.088f, -0.086f, t);
                float z = 0.020f - t * t * 0.034f;
                var curve = Block(fly, "FlyStitch" + i, new Vector3(x, 0.007f, z), new Vector3(0.028f, 0.005f, 0.006f), topstitchMat);
                curve.localEulerAngles = new Vector3(0f, t * 26f, 0f);
            }
            Block(fly, "FlyEdge", new Vector3(0f, 0.004f, 0.024f), new Vector3(0.185f, 0.008f, 0.008f), fabricDeepMat);
            // Zip teeth showing in the gap of the fly.
            for (int i = 0; i < 10; i++)
            {
                float x = 0.070f - i * 0.016f;
                int s = (i % 2 == 0) ? 1 : -1;
                Block(fly, "Tooth" + i, new Vector3(x, 0.007f, -0.004f + s * 0.004f), new Vector3(0.007f, 0.005f, 0.010f), steelMat);
            }
            Block(fly, "Slider", new Vector3(0.086f, 0.009f, -0.004f), new Vector3(0.024f, 0.008f, 0.018f), steelMat);
            // The shank button at the top of the fly, sitting on the waistband as it always did.
            Sphere(root, "FlyButton", new Vector3(0.166f, 0.052f, -0.044f), Vector3.zero, new Vector3(0.024f, 0.012f, 0.024f), copperMat);
            Sphere(root, "FlyButtonDish", new Vector3(0.166f, 0.058f, -0.044f), Vector3.zero, new Vector3(0.012f, 0.005f, 0.012f), fabricDeepMat);
            Block(root, "Buttonhole", new Vector3(0.166f, 0.048f, -0.014f), new Vector3(0.010f, 0.006f, 0.030f), fabricDeepMat);

            // ── The back yoke, left exactly where the jeans had it ────────────────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band((s > 0 ? "YokeSeamL" : "YokeSeamR") + "A",
                    new Vector3(0.116f, 0f, s * 0.006f), new Vector3(0.062f, 0f, s * 0.112f), 0.006f, 0.040f, 0.005f, topstitchMat);
                Band((s > 0 ? "YokeSeamL" : "YokeSeamR") + "B",
                    new Vector3(0.104f, 0f, s * 0.006f), new Vector3(0.050f, 0f, s * 0.110f), 0.006f, 0.040f, 0.005f, topstitchMat);
                Band((s > 0 ? "YokeRidgeL" : "YokeRidgeR"),
                    new Vector3(0.110f, 0f, s * 0.006f), new Vector3(0.056f, 0f, s * 0.111f), 0.020f, 0.032f, 0.012f, fabricDeepMat);
            }

            // ── Godets: triangles of darker denim let into the sides where the legs would not meet ───────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var wedge = new PartNode("Godet" + side);
                wedge.SetParent(root, false);
                wedge.localPosition = new Vector3(-0.230f, 0.030f, s * 0.126f);
                wedge.localEulerAngles = new Vector3(0f, s * -9f, 0f);
                // A wedge, built as stacked bars each wider than the one above it.
                for (int i = 0; i < 5; i++)
                {
                    float x = 0.086f - i * 0.048f;
                    float w = 0.030f + i * 0.026f;
                    Block(wedge, "Wedge" + i, new Vector3(x, 0f, 0f), new Vector3(0.050f, 0.012f, w), godetMat);
                }
                // The two seams holding the godet in, doubled in gold like everything else on a pair of jeans.
                for (int e = -1; e <= 1; e += 2)
                {
                    var seam = Block(wedge, e > 0 ? "GodetSeamA" : "GodetSeamB",
                        new Vector3(-0.010f, 0.008f, e * 0.052f), new Vector3(0.210f, 0.005f, 0.006f), topstitchMat);
                    seam.localEulerAngles = new Vector3(0f, e * 16f, 0f);
                    var seam2 = Block(wedge, e > 0 ? "GodetSeamA2" : "GodetSeamB2",
                        new Vector3(-0.010f, 0.008f, e * 0.062f), new Vector3(0.200f, 0.005f, 0.006f), topstitchMat);
                    seam2.localEulerAngles = new Vector3(0f, e * 16f, 0f);
                }
            }

            // ── The opened inseam running from the crotch straight down to the hem ────────────────────────
            Band("InseamRidge", new Vector3(-0.096f, 0f, 0f), new Vector3(-0.360f, 0f, 0f), 0.024f, 0.032f, 0.014f, fabricDeepMat);
            Band("InseamA", new Vector3(-0.096f, 0f, 0.007f), new Vector3(-0.360f, 0f, 0.007f), 0.006f, 0.042f, 0.005f, topstitchMat);
            Band("InseamB", new Vector3(-0.096f, 0f, -0.007f), new Vector3(-0.360f, 0f, -0.007f), 0.006f, 0.042f, 0.005f, topstitchMat);
            // The bar tack at the top of the split, where the crotch seam was cut and had to be stopped.
            Block(root, "BarTack", new Vector3(-0.090f, 0.044f, 0f), new Vector3(0.010f, 0.006f, 0.030f), topstitchMat);

            // ── The front coin pocket, still in its corner ────────────────────────────────────────────────
            var coin = new PartNode("CoinPocket");
            coin.SetParent(root, false);
            coin.localPosition = new Vector3(0.104f, 0.038f, 0.088f);
            coin.localEulerAngles = new Vector3(0f, -12f, 0f);
            Block(coin, "Cloth", Vector3.zero, new Vector3(0.048f, 0.010f, 0.044f), fabricMat);
            Block(coin, "Mouth", new Vector3(0.024f, 0.005f, 0f), new Vector3(0.010f, 0.010f, 0.044f), fabricDeepMat);
            Block(coin, "MouthStitch", new Vector3(0.016f, 0.008f, 0f), new Vector3(0.006f, 0.005f, 0.040f), topstitchMat);
            for (int s = -1; s <= 1; s += 2)
                Sphere(coin, s > 0 ? "RivetL" : "RivetR", new Vector3(0.018f, 0.008f, s * 0.018f), Vector3.zero,
                    new Vector3(0.011f, 0.007f, 0.011f), copperMat);

            // ── Copper rivets at the old hip pocket corners ───────────────────────────────────────────────
            Vector2[] rivets =
            {
                new Vector2(0.062f,  0.104f), new Vector2(0.062f, -0.104f),
                new Vector2(0.010f,  0.118f), new Vector2(0.010f, -0.118f),
            };
            for (int i = 0; i < rivets.Length; i++)
            {
                Sphere(root, "Rivet" + i, new Vector3(rivets[i].x, 0.042f, rivets[i].y), Vector3.zero,
                    new Vector3(0.014f, 0.009f, 0.014f), copperMat);
                Sphere(root, "RivetDish" + i, new Vector3(rivets[i].x, 0.047f, rivets[i].y), Vector3.zero,
                    new Vector3(0.006f, 0.004f, 0.006f), fabricDeepMat);
            }

            // ── Wear: whiskering off the hips and honeycombing where the knees used to be ─────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.056f - i * 0.024f;
                    Band((s > 0 ? "WhiskerL" : "WhiskerR") + i,
                        new Vector3(x, 0f, s * 0.026f), new Vector3(x - 0.048f, 0f, s * (0.090f + i * 0.010f)),
                        0.010f, 0.040f, 0.006f, whiskerMat);
                }
                // The knee fade, now sitting low on the skirt where the leg used to bend.
                Band((s > 0 ? "KneeFadeL" : "KneeFadeR"),
                    new Vector3(-0.252f, 0f, s * 0.048f), new Vector3(-0.252f, 0f, s * 0.132f),
                    0.038f, 0.038f, 0.010f, whiskerMat);
            }
            Sphere(root, "SeatFade", new Vector3(-0.030f, 0.038f, -0.016f), new Vector3(0f, 7f, 0f), new Vector3(0.14f, 0.012f, 0.110f), whiskerMat);

            // Denim creases hard: sharp raised ridges with dark valleys beside them.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f * 2f - 1f;
                var ridge = Block(root, "Crease" + i, new Vector3(-0.230f + Mathf.Abs(t) * 0.040f, 0.040f, t * 0.108f),
                    new Vector3(0.250f, 0.010f, 0.014f), twillMat);
                ridge.localEulerAngles = new Vector3(0f, t * 9f, 0f);
                var valley = Block(root, "CreaseShade" + i, new Vector3(-0.236f + Mathf.Abs(t) * 0.040f, 0.034f, t * 0.108f + 0.022f),
                    new Vector3(0.234f, 0.008f, 0.010f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, t * 9f, 0f);
            }

            // ── Hem: the original leg hems, so the two halves of it do not quite line up ──────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                float off = s > 0 ? 0f : 0.014f;   // one leg was always a little longer than the other
                Block(root, s > 0 ? "LegHemL" : "LegHemR",
                    new Vector3(-0.376f - off, 0.006f, s * 0.086f), new Vector3(0.030f, 0.054f, 0.180f), fabricMat);
                Block(root, s > 0 ? "LegHemEdgeL" : "LegHemEdgeR",
                    new Vector3(-0.392f - off, 0.004f, s * 0.086f), new Vector3(0.012f, 0.050f, 0.180f), fabricDeepMat);
                Block(root, s > 0 ? "LegHemStitchL" : "LegHemStitchR",
                    new Vector3(-0.364f - off, 0.036f, s * 0.086f), new Vector3(0.006f, 0.006f, 0.174f), topstitchMat);
            }
            // Across the godets the hem is new, and cut level between the two old leg hems.
            for (int s = -1; s <= 1; s += 2)
                Block(root, s > 0 ? "GodetHemL" : "GodetHemR",
                    new Vector3(-0.370f, 0.005f, s * 0.164f), new Vector3(0.026f, 0.048f, 0.062f), godetMat);

            // The leather brand patch, still sitting on the waistband where it was sewn.
            var patch = new PartNode("LeatherPatch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(0.198f, 0.052f, 0.062f);
            patch.localEulerAngles = new Vector3(0f, 8f, 0f);
            Block(patch, "PatchLeather", Vector3.zero, new Vector3(0.028f, 0.006f, 0.044f), labelMat);
            Block(patch, "PatchEmboss", new Vector3(0f, 0.004f, 0f), new Vector3(0.015f, 0.004f, 0.028f), fabricDeepMat);
            for (int i = 0; i < 4; i++)
                Block(patch, "PatchStitch" + i, new Vector3(-0.016f + (i % 2) * 0.032f, 0.004f, -0.025f + (i / 2) * 0.050f),
                    new Vector3(0.008f, 0.004f, 0.008f), topstitchMat);

            return root;
        }

        /// <summary>
        /// A knee-length cotton sundress run up out of household linen, laid flat face-up with shoulders at
        /// +X and hem at −X. It is the best-made thing in the crafted set and is built to look it: a proper
        /// gathered waist seam joining a fitted top to a full skirt, armholes and neckline finished in bias
        /// binding cut on the cross, a band of rick-rack braid laid round the skirt, and long ties sewn to
        /// the back to cross over and tie in front. The home-made tells are the pleasant kind — a bodice and
        /// skirt plainly cut from two different sheets, a pocket in a third print, thread loops instead of
        /// buttonholes, and a hem that has been let down, leaving the old fold line pressed across it.
        /// </summary>
        private static PartNode BuildDressKneesCraftedCotton(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The same washed sheet cotton as the long crafted cotton dress, so the two read as a pair.
            Color calico = new Color(0.855f, 0.830f, 0.745f);
            Color fabric = Color.Lerp(calico, def.Tint, 0.22f);                                   // the skirt sheet
            Color fabricDeep = new Color(fabric.r * 0.72f, fabric.g * 0.71f, fabric.b * 0.68f);   // shaded gather valleys
            Color fabricHi = Color.Lerp(fabric, Color.white, 0.30f);                              // sun-bleached raised folds
            Color bodiceCloth = Color.Lerp(fabric, new Color(0.72f, 0.74f, 0.70f), 0.45f);        // a second sheet, cooler and greener
            Color bodiceDeep = new Color(bodiceCloth.r * 0.74f, bodiceCloth.g * 0.73f, bodiceCloth.b * 0.70f);
            Color print = Color.Lerp(fabric, new Color(0.46f, 0.40f, 0.52f), 0.55f);              // the pocket, a third print entirely
            Color bias = Color.Lerp(fabric, new Color(0.58f, 0.46f, 0.40f), 0.50f);               // bias binding round the edges
            Color braid = Color.Lerp(fabric, new Color(0.78f, 0.52f, 0.42f), 0.60f);              // rick-rack braid
            Color thread = Color.Lerp(fabric, new Color(0.52f, 0.50f, 0.44f), 0.55f);             // small even hand stitching
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.44f, fabric.b * 0.44f);       // dark inside, seen through the neck
            Color shell = new Color(0.88f, 0.86f, 0.80f);                                         // the two shell buttons

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.11f);   // soft matte cotton
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.08f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.14f);
            PartMat bodiceMat = PartMat.Of(bodiceCloth, smoothness: 0.11f);
            PartMat bodiceDeepMat = PartMat.Of(bodiceDeep, smoothness: 0.08f);
            PartMat printMat = PartMat.Of(print, smoothness: 0.10f);
            PartMat biasMat = PartMat.Of(bias, smoothness: 0.12f);
            PartMat braidMat = PartMat.Of(braid, smoothness: 0.13f);
            PartMat threadMat = PartMat.Of(thread, smoothness: 0.09f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat shellMat = PartMat.Of(shell, smoothness: 0.60f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // binding, the braid and the ties, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Fitted bodice in the second sheet: shaped in to a real waist, which no other crafted
            //    dress in the set bothers with ────────────────────────────────────────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.148f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.048f, 0.212f), bodiceMat);
            Sphere(root, "Bodice", new Vector3(0.062f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.050f, 0.226f), bodiceMat);
            Sphere(root, "Waist", new Vector3(-0.046f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.176f), bodiceMat);
            // Darts taken either side of the front to pull the width out of the bodice.
            for (int s = -1; s <= 1; s += 2)
            {
                var dart = Block(root, s > 0 ? "DartL" : "DartR",
                    new Vector3(0.030f, 0.028f, s * 0.062f), new Vector3(0.130f, 0.006f, 0.007f), threadMat);
                dart.localEulerAngles = new Vector3(0f, s * 11f, 0f);
                Sphere(root, s > 0 ? "DartFoldL" : "DartFoldR", new Vector3(0.030f, 0.024f, s * 0.070f),
                    new Vector3(0f, s * 11f, 0f), new Vector3(0.120f, 0.012f, 0.020f), bodiceDeepMat);
            }

            // ── Gathered waist seam: the skirt bunched on to the bodice, the join everything hangs from ───
            Band("WaistSeam", new Vector3(-0.086f, 0f, -0.092f), new Vector3(-0.086f, 0f, 0.092f), 0.007f, 0.034f, 0.006f, threadMat);
            for (int i = 0; i < 13; i++)
            {
                float z = (i / 12f - 0.5f) * 0.180f;
                Sphere(root, "Gather" + i, new Vector3(-0.108f, 0.034f, z),
                    new Vector3(0f, Mathf.Sin(i * 1.4f) * 10f, 0f), new Vector3(0.048f, 0.014f, 0.019f), fabricHiMat);
                Sphere(root, "GatherShade" + i, new Vector3(-0.114f, 0.028f, z + 0.014f), Vector3.zero,
                    new Vector3(0.040f, 0.011f, 0.010f), fabricDeepMat);
            }

            // ── Full skirt in the first sheet, gathered wide and falling to the knee ──────────────────────
            Sphere(root, "SkirtTop", new Vector3(-0.152f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.262f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.262f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.330f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.362f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.046f, 0.376f), fabricMat);

            // Soft rounded folds running down the skirt — cotton drapes, so no hard edges anywhere.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f * 2f - 1f;
                Sphere(root, "Drape" + i, new Vector3(-0.260f, 0.034f, t * 0.130f),
                    new Vector3(0f, t * 12f, 0f), new Vector3(0.270f, 0.014f, 0.040f), fabricHiMat);
                Sphere(root, "DrapeShade" + i, new Vector3(-0.268f, 0.029f, t * 0.130f + 0.032f),
                    new Vector3(0f, t * 12f, 0f), new Vector3(0.250f, 0.011f, 0.024f), fabricDeepMat);
            }

            // ── Bias binding round the neckline, cut on the cross so it takes the curve ───────────────────
            Sphere(root, "Neckline", new Vector3(0.156f, 0.012f, 0f), Vector3.zero, new Vector3(0.084f, 0.032f, 0.126f), liningMat);
            const int bindings = 16;
            for (int i = 0; i < bindings; i++)
            {
                float deg = i * 360f / bindings;
                float rad = deg * Mathf.Deg2Rad;
                var bind = Block(root, "Binding" + i,
                    new Vector3(0.156f + 0.048f * Mathf.Cos(rad), 0.026f, 0.074f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.024f, 0.012f), biasMat);
                bind.localEulerAngles = new Vector3(0f, -deg, 0f);
                var st = Block(root, "BindStitch" + i,
                    new Vector3(0.156f + 0.060f * Mathf.Cos(rad), 0.038f, 0.092f * Mathf.Sin(rad)),
                    new Vector3(0.012f, 0.005f, 0.006f), threadMat);
                st.localEulerAngles = new Vector3(0f, -deg, 0f);
            }

            // ── Sleeveless: bound armholes, no sleeve at all, cut high at the shoulder ────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var arm = new PartNode("ArmHole" + side);
                arm.SetParent(root, false);
                arm.localPosition = new Vector3(0.092f, 0.014f, s * 0.116f);
                arm.localEulerAngles = new Vector3(0f, s * -18f, 0f);
                Sphere(arm, "Void", Vector3.zero, Vector3.zero, new Vector3(0.098f, 0.030f, 0.062f), liningMat);
                for (int i = 0; i < 12; i++)
                {
                    float deg = i * 30f;
                    float rad = deg * Mathf.Deg2Rad;
                    var bind = Block(arm, "Bind" + i,
                        new Vector3(0.056f * Mathf.Cos(rad), 0.010f, 0.038f * Mathf.Sin(rad)),
                        new Vector3(0.016f, 0.024f, 0.011f), biasMat);
                    bind.localEulerAngles = new Vector3(0f, -deg, 0f);
                    var st = Block(arm, "BindStitch" + i,
                        new Vector3(0.068f * Mathf.Cos(rad), 0.022f, 0.050f * Mathf.Sin(rad)),
                        new Vector3(0.011f, 0.005f, 0.005f), threadMat);
                    st.localEulerAngles = new Vector3(0f, -deg, 0f);
                }
            }

            // ── Rick-rack braid round the skirt: a zigzag of short bars laid alternately left and right ───
            const int zigs = 22;
            for (int i = 0; i < zigs; i++)
            {
                float z = (i / (zigs - 1f) - 0.5f) * 0.330f;
                int dir = (i % 2 == 0) ? 1 : -1;
                var zig = Block(root, "RickRack" + i, new Vector3(-0.318f + dir * 0.011f, 0.036f, z),
                    new Vector3(0.030f, 0.006f, 0.011f), braidMat);
                zig.localEulerAngles = new Vector3(0f, dir * 52f, 0f);
            }
            Band("BraidStitch", new Vector3(-0.318f, 0f, -0.166f), new Vector3(-0.318f, 0f, 0.166f), 0.005f, 0.042f, 0.004f, threadMat);

            // ── Thread loops and two shell buttons at the back neck, in place of worked buttonholes ───────
            for (int i = 0; i < 2; i++)
            {
                float x = 0.126f - i * 0.062f;
                Sphere(root, "Button" + i, new Vector3(x, 0.036f, -0.020f), Vector3.zero,
                    new Vector3(0.017f, 0.008f, 0.017f), shellMat);
                Sphere(root, "ButtonHole" + i, new Vector3(x, 0.040f, -0.020f), Vector3.zero,
                    new Vector3(0.006f, 0.004f, 0.006f), fabricDeepMat);
                // The loop itself, a bight of thread standing out past the edge.
                Block(root, "LoopA" + i, new Vector3(x + 0.004f, 0.034f, -0.038f), new Vector3(0.005f, 0.005f, 0.022f), threadMat);
                Block(root, "LoopB" + i, new Vector3(x - 0.004f, 0.034f, -0.038f), new Vector3(0.005f, 0.005f, 0.022f), threadMat);
                Block(root, "LoopEnd" + i, new Vector3(x, 0.034f, -0.049f), new Vector3(0.013f, 0.005f, 0.005f), threadMat);
            }

            // ── Long back ties, brought round to cross and knot at the front ──────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band(s > 0 ? "TieL" : "TieR",
                    new Vector3(-0.076f, 0f, s * 0.084f), new Vector3(-0.052f, 0f, s * 0.196f),
                    0.022f, 0.036f, 0.010f, bodiceMat);
            }
            var bow = new PartNode("Bow");
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(-0.070f, 0.042f, -0.150f);
            bow.localEulerAngles = new Vector3(0f, 28f, 0f);
            Sphere(bow, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.030f, 0.018f, 0.026f), bodiceMat);
            Sphere(bow, "LoopA", new Vector3(0.028f, -0.002f, -0.024f), new Vector3(0f, -36f, 0f), new Vector3(0.052f, 0.012f, 0.028f), bodiceMat);
            Sphere(bow, "LoopB", new Vector3(-0.012f, -0.002f, -0.036f), new Vector3(0f, 24f, 0f), new Vector3(0.046f, 0.012f, 0.026f), bodiceMat);
            Block(bow, "TailA", new Vector3(-0.054f, -0.003f, -0.012f), new Vector3(0.084f, 0.009f, 0.020f), bodiceMat);
            Block(bow, "TailB", new Vector3(-0.044f, -0.004f, 0.018f), new Vector3(0.068f, 0.008f, 0.018f), bodiceDeepMat);

            // ── One patch pocket in a third print, sewn on square and slightly off-centre ─────────────────
            var pocket = new PartNode("Pocket");
            pocket.SetParent(root, false);
            pocket.localPosition = new Vector3(-0.212f, 0.036f, 0.104f);
            pocket.localEulerAngles = new Vector3(0f, 7f, 0f);
            Block(pocket, "PatchCloth", Vector3.zero, new Vector3(0.084f, 0.010f, 0.076f), printMat);
            Block(pocket, "Mouth", new Vector3(0.042f, 0.004f, 0f), new Vector3(0.012f, 0.010f, 0.076f), fabricDeepMat);
            for (int i = 0; i < 10; i++)
            {
                float t = i / 10f;
                Vector3 pos = t < 0.34f ? new Vector3(0.038f - t * 0.220f, 0.006f, 0.036f)
                            : t < 0.67f ? new Vector3(-0.038f, 0.006f, 0.036f - (t - 0.34f) * 0.220f)
                                        : new Vector3(-0.034f + (t - 0.67f) * 0.210f, 0.006f, -0.036f);
                Block(pocket, "PocketStitch" + i, pos, new Vector3(0.012f, 0.005f, 0.005f), threadMat);
            }
            Block(pocket, "PrintStripe", new Vector3(0f, 0.005f, 0f), new Vector3(0.008f, 0.006f, 0.062f), fabricHiMat);

            // ── Let-down hem: turned and stitched, with the old fold line still pressed in above it ───────
            Block(root, "HemFold", new Vector3(-0.418f, 0.004f, 0f), new Vector3(0.020f, 0.046f, 0.352f), fabricHiMat);
            Block(root, "HemStitch", new Vector3(-0.400f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.340f), threadMat);
            // The crease the hem used to sit on, and the faint line of the old stitching beside it.
            Block(root, "OldCrease", new Vector3(-0.352f, 0.036f, 0f), new Vector3(0.010f, 0.007f, 0.348f), fabricDeepMat);
            Block(root, "OldCreaseHi", new Vector3(-0.344f, 0.040f, 0f), new Vector3(0.006f, 0.005f, 0.344f), fabricHiMat);
            for (int i = 0; i < 12; i++)
            {
                float z = (i / 11f - 0.5f) * 0.330f;
                Block(root, "OldStitchMark" + i, new Vector3(-0.362f, 0.038f, z), new Vector3(0.008f, 0.005f, 0.006f), fabricDeepMat);
            }

            // A knotted tape hanging loop at the back neck instead of a maker's label, as on the rest.
            var loop = new PartNode("HangLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(0.152f, 0.038f, 0.064f);
            loop.localEulerAngles = new Vector3(0f, 23f, 0f);
            Block(loop, "LoopA", new Vector3(0f, 0f, -0.011f), new Vector3(0.030f, 0.005f, 0.006f), biasMat);
            Block(loop, "LoopB", new Vector3(0f, 0f, 0.011f), new Vector3(0.030f, 0.005f, 0.006f), biasMat);
            Block(loop, "LoopEnd", new Vector3(0.016f, 0f, 0f), new Vector3(0.006f, 0.005f, 0.026f), biasMat);

            return root;
        }

        /// <summary>
        /// A knee-length shift made out of a single feed sack, laid flat face-up with shoulders at +X and
        /// hem at −X. It is the short counterpart to the long burlap dress and shares its hessian, but the
        /// construction is a different and cruder idea: rather than being cut into panels and sewn up, the
        /// sack has simply had a hole cut for the head and one at each corner for the arms, and is worn as
        /// it is. That leaves the sack's own machine-stitched bottom seam doing duty as the hem, its printed
        /// stencil sitting square across the front where it always was, and its drawstring mouth still
        /// threaded through the top with the loose cord knotted over each shoulder to take the weight.
        /// </summary>
        private static PartNode BuildDressKneesCraftedBurlap(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The same jute as the long burlap dress, so the pair read as one material.
            Color jute = new Color(0.545f, 0.435f, 0.255f);
            Color fabric = Color.Lerp(jute, def.Tint, 0.14f);                                     // coarse hessian
            Color fabricDeep = new Color(fabric.r * 0.66f, fabric.g * 0.64f, fabric.b * 0.60f);   // shaded creases, weave gaps
            Color slub = Color.Lerp(fabric, new Color(0.74f, 0.64f, 0.44f), 0.50f);               // lit threads of the open weave
            Color sun = Color.Lerp(fabric, new Color(0.80f, 0.72f, 0.55f), 0.45f);                // the side of the sack left in the light
            Color twine = new Color(0.80f, 0.74f, 0.56f);                                         // drawstring cord and hand stitching
            Color fray = Color.Lerp(fabric, new Color(0.70f, 0.62f, 0.44f), 0.35f);               // loose threads round the cut holes
            Color lining = new Color(fabric.r * 0.38f, fabric.g * 0.38f, fabric.b * 0.38f);       // dark inside, seen through the holes
            Color stencil = new Color(0.30f, 0.28f, 0.24f);                                       // the sack's printed markings
            Color stain = Color.Lerp(fabric, new Color(0.34f, 0.28f, 0.18f), 0.55f);              // whatever the sack held before

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.03f);   // sacking: as flat as anything in the set
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.02f);
            PartMat slubMat = PartMat.Of(slub, smoothness: 0.06f);
            PartMat sunMat = PartMat.Of(sun, smoothness: 0.05f);
            PartMat twineMat = PartMat.Of(twine, smoothness: 0.05f);
            PartMat frayMat = PartMat.Of(fray, smoothness: 0.04f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.03f);
            PartMat stencilMat = PartMat.Of(stencil, smoothness: 0.04f);
            PartMat stainMat = PartMat.Of(stain, smoothness: 0.05f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // sack seams, the cut edges and the cord, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: a plain rectangle, near enough the same width top to bottom, because it is a sack ───
            Sphere(root, "SackTop", new Vector3(0.148f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.056f, 0.250f), fabricMat);
            Sphere(root, "SackUpper", new Vector3(0.055f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.058f, 0.256f), fabricMat);
            Sphere(root, "SackMid", new Vector3(-0.070f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.058f, 0.258f), fabricMat);
            Sphere(root, "SackLow", new Vector3(-0.190f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.056f, 0.262f), fabricMat);
            Sphere(root, "SackBase", new Vector3(-0.300f, -0.002f, 0f), Vector3.zero, new Vector3(0.15f, 0.054f, 0.256f), fabricMat);

            // The half of the sack that sat facing the light has gone a shade lighter than the other.
            Sphere(root, "SunFade", new Vector3(-0.090f, 0.036f, 0.070f), new Vector3(0f, 4f, 0f),
                new Vector3(0.44f, 0.012f, 0.130f), sunMat);

            // ── Open weave: the coarse grid of slubs and gaps that says burlap on sight ───────────────────
            for (int gx = 0; gx < 11; gx++)
            {
                float x = Mathf.Lerp(0.150f, -0.330f, gx / 10f);
                Block(root, "Warp" + gx, new Vector3(x, 0.033f, 0f), new Vector3(0.009f, 0.006f, 0.215f), slubMat);
                Block(root, "WarpGap" + gx, new Vector3(x + 0.024f, 0.030f, 0f), new Vector3(0.006f, 0.005f, 0.205f), fabricDeepMat);
            }
            for (int gz = 0; gz < 7; gz++)
            {
                float z = (gz / 6f - 0.5f) * 0.200f;
                Block(root, "Weft" + gz, new Vector3(-0.090f, 0.035f, z), new Vector3(0.470f, 0.006f, 0.009f), slubMat);
                Block(root, "WeftGap" + gz, new Vector3(-0.090f, 0.031f, z + 0.016f), new Vector3(0.455f, 0.005f, 0.006f), fabricDeepMat);
            }

            // ── The sack's own side seams, machine-run and left exactly as they came ──────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band(s > 0 ? "SackSeamL" : "SackSeamR",
                    new Vector3(0.150f, 0f, s * 0.112f), new Vector3(-0.320f, 0f, s * 0.115f), 0.007f, 0.040f, 0.006f, fabricDeepMat);
                Band(s > 0 ? "SackSeamRidgeL" : "SackSeamRidgeR",
                    new Vector3(0.150f, 0f, s * 0.112f), new Vector3(-0.320f, 0f, s * 0.115f), 0.020f, 0.032f, 0.012f, fabricMat);
            }

            // ── Head hole: cut out with a knife, not shaped, and shedding threads all the way round ───────
            Sphere(root, "HeadHole", new Vector3(0.150f, 0.014f, 0f), Vector3.zero, new Vector3(0.090f, 0.036f, 0.128f), liningMat);
            const int rim = 14;
            for (int i = 0; i < rim; i++)
            {
                float deg = i * 360f / rim;
                float rad = deg * Mathf.Deg2Rad;
                var cut = Block(root, "HeadCut" + i,
                    new Vector3(0.150f + 0.050f * Mathf.Cos(rad), 0.030f, 0.076f * Mathf.Sin(rad)),
                    new Vector3(0.020f, 0.022f, 0.012f), frayMat);
                cut.localEulerAngles = new Vector3(0f, -deg, 0f);
                // A whipped stitch here and there to stop the cut running any further.
                if (i % 3 == 0)
                {
                    var whip = Block(root, "WhipStitch" + i,
                        new Vector3(0.150f + 0.062f * Mathf.Cos(rad), 0.040f, 0.094f * Mathf.Sin(rad)),
                        new Vector3(0.016f, 0.005f, 0.007f), twineMat);
                    whip.localEulerAngles = new Vector3(0f, -deg + 24f, 0f);
                }
            }

            // ── Arm holes: one cut at each top corner, no sleeve and no facing ────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var arm = new PartNode("ArmHole" + side);
                arm.SetParent(root, false);
                arm.localPosition = new Vector3(0.086f, 0.016f, s * 0.124f);
                arm.localEulerAngles = new Vector3(0f, s * -16f, 0f);
                Sphere(arm, "Void", Vector3.zero, Vector3.zero, new Vector3(0.104f, 0.032f, 0.072f), liningMat);
                for (int i = 0; i < 10; i++)
                {
                    float deg = i * 36f;
                    float rad = deg * Mathf.Deg2Rad;
                    var cut = Block(arm, "Cut" + i,
                        new Vector3(0.058f * Mathf.Cos(rad), 0.012f, 0.044f * Mathf.Sin(rad)),
                        new Vector3(0.018f, 0.020f, 0.011f), frayMat);
                    cut.localEulerAngles = new Vector3(0f, -deg, 0f);
                    if (i % 4 == 0)
                        Block(arm, "Thread" + i, new Vector3(0.070f * Mathf.Cos(rad), 0.020f, 0.056f * Mathf.Sin(rad)),
                            new Vector3(0.024f, 0.005f, 0.005f), frayMat).localEulerAngles = new Vector3(0f, -deg, 0f);
                }
            }

            // ── The sack's drawstring mouth, still threaded, knotted over each shoulder to hold it up ─────
            Band("MouthCasing", new Vector3(0.196f, 0f, -0.108f), new Vector3(0.196f, 0f, 0.108f), 0.026f, 0.034f, 0.020f, fabricMat);
            Band("MouthStitch", new Vector3(0.182f, 0f, -0.104f), new Vector3(0.182f, 0f, 0.104f), 0.006f, 0.044f, 0.005f, twineMat);
            for (int i = 0; i < 8; i++)
            {
                float z = (i / 7f - 0.5f) * 0.190f;
                Sphere(root, "MouthGather" + i, new Vector3(0.192f, 0.042f, z), new Vector3(0f, Mathf.Sin(i * 1.6f) * 14f, 0f),
                    new Vector3(0.032f, 0.014f, 0.016f), slubMat);
                if (i % 2 == 0)
                    Sphere(root, "Grommet" + i, new Vector3(0.200f, 0.044f, z), Vector3.zero,
                        new Vector3(0.014f, 0.010f, 0.014f), fabricDeepMat);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var tie = new PartNode("ShoulderTie" + side);
                tie.SetParent(root, false);
                tie.localPosition = new Vector3(0.186f, 0.046f, s * 0.084f);
                tie.localEulerAngles = new Vector3(0f, s * 34f, 0f);
                Sphere(tie, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.026f, 0.018f, 0.024f), twineMat);
                Block(tie, "CordA", new Vector3(-0.038f, -0.002f, 0.012f), new Vector3(0.062f, 0.010f, 0.010f), twineMat);
                Block(tie, "CordB", new Vector3(-0.030f, -0.003f, -0.014f), new Vector3(0.048f, 0.009f, 0.009f), twineMat);
                Block(tie, "CordTip", new Vector3(-0.072f, -0.003f, 0.020f), new Vector3(0.022f, 0.008f, 0.008f), frayMat);
            }

            // ── Waist drawn in with a length of the same cord, wrapped twice and knotted at the side ──────
            Block(root, "BeltNear", new Vector3(-0.055f, 0.042f, 0f), new Vector3(0.017f, 0.015f, 0.220f), twineMat);
            Block(root, "BeltFar", new Vector3(-0.082f, 0.041f, 0.006f), new Vector3(0.015f, 0.014f, 0.212f), twineMat);
            for (int i = 0; i < 11; i++)
            {
                float z = (i / 10f - 0.5f) * 0.205f;
                var twist = Block(root, "BeltTwist" + i, new Vector3(-0.055f, 0.048f, z), new Vector3(0.013f, 0.006f, 0.010f), fabricDeepMat);
                twist.localEulerAngles = new Vector3(0f, 33f, 0f);
            }
            var beltKnot = new PartNode("BeltKnot");
            beltKnot.SetParent(root, false);
            beltKnot.localPosition = new Vector3(-0.068f, 0.048f, -0.100f);
            beltKnot.localEulerAngles = new Vector3(0f, 42f, 0f);
            Sphere(beltKnot, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.032f, 0.022f, 0.028f), twineMat);
            Block(beltKnot, "TailA", new Vector3(-0.050f, -0.004f, 0.014f), new Vector3(0.080f, 0.012f, 0.013f), twineMat);
            Block(beltKnot, "TailB", new Vector3(-0.040f, -0.005f, -0.018f), new Vector3(0.062f, 0.011f, 0.012f), twineMat);
            for (int i = 0; i < 4; i++)
                Block(beltKnot, "Strand" + i, new Vector3(-0.098f, -0.004f, 0.006f + i * 0.006f),
                    new Vector3(0.026f, 0.005f, 0.004f), frayMat);

            // ── The sack's stencil, sitting square across the front exactly where it was printed ──────────
            var mark = new PartNode("SackStencil");
            mark.SetParent(root, false);
            mark.localPosition = new Vector3(-0.010f, 0.038f, 0.004f);
            mark.localEulerAngles = new Vector3(0f, -3f, 0f);
            Block(mark, "RuleTop", new Vector3(0.062f, 0f, 0f), new Vector3(0.010f, 0.005f, 0.150f), stencilMat);
            Block(mark, "RuleLow", new Vector3(-0.070f, 0f, 0f), new Vector3(0.010f, 0.005f, 0.150f), stencilMat);
            for (int i = 0; i < 5; i++)
                Block(mark, "GlyphA" + i, new Vector3(0.026f, 0f, -0.060f + i * 0.030f),
                    new Vector3(0.030f, 0.005f, 0.019f), stencilMat);
            for (int i = 0; i < 3; i++)
                Block(mark, "GlyphB" + i, new Vector3(-0.024f, 0f, -0.034f + i * 0.034f),
                    new Vector3(0.024f, 0.005f, 0.021f), stencilMat);
            Sphere(mark, "StampRing", new Vector3(-0.044f, 0f, 0.070f), Vector3.zero, new Vector3(0.048f, 0.006f, 0.044f), stencilMat);
            Sphere(mark, "StampCentre", new Vector3(-0.044f, 0.002f, 0.070f), Vector3.zero, new Vector3(0.030f, 0.006f, 0.028f), fabricMat);

            // A dark stain low down from whatever the sack was carrying before it was cut up.
            Sphere(root, "Stain", new Vector3(-0.246f, 0.036f, -0.074f), new Vector3(0f, 16f, 0f), new Vector3(0.12f, 0.011f, 0.072f), stainMat);
            Sphere(root, "StainEdge", new Vector3(-0.268f, 0.034f, -0.096f), new Vector3(0f, 22f, 0f), new Vector3(0.07f, 0.010f, 0.042f), fabricDeepMat);

            // ── Hem: the sack's own factory-sewn bottom seam, so this one is finished where the long
            //    burlap dress's cut hem is not — the whole point of using the sack the way up it came ─────
            Block(root, "BaseSeam", new Vector3(-0.366f, 0.004f, 0f), new Vector3(0.026f, 0.052f, 0.250f), fabricDeepMat);
            Block(root, "BaseSeamStitchA", new Vector3(-0.356f, 0.032f, 0f), new Vector3(0.006f, 0.006f, 0.242f), twineMat);
            Block(root, "BaseSeamStitchB", new Vector3(-0.372f, 0.032f, 0f), new Vector3(0.006f, 0.006f, 0.242f), twineMat);
            // The two stiff dog-ears the corners of a sack always keep.
            for (int s = -1; s <= 1; s += 2)
            {
                Sphere(root, s > 0 ? "CornerEarL" : "CornerEarR",
                    new Vector3(-0.372f, 0.006f, s * 0.108f), new Vector3(0f, s * 26f, 0f),
                    new Vector3(0.060f, 0.040f, 0.048f), fabricMat);
            }

            // Hessian is stiff: hard creases and standing ridges, never a soft drape.
            Sphere(root, "CreaseA", new Vector3(-0.150f, 0.036f, -0.070f), new Vector3(0f, -8f, 0f), new Vector3(0.24f, 0.014f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.262f, 0.034f, 0.082f), new Vector3(0f, 13f, 0f), new Vector3(0.16f, 0.013f, 0.026f), fabricDeepMat);
            Sphere(root, "RidgeA", new Vector3(-0.120f, 0.040f, 0.040f), new Vector3(0f, 6f, 0f), new Vector3(0.26f, 0.012f, 0.026f), slubMat);
            Sphere(root, "RidgeB", new Vector3(0.030f, 0.040f, -0.066f), new Vector3(0f, -10f, 0f), new Vector3(0.15f, 0.012f, 0.024f), slubMat);

            // A knotted twine hanging loop in place of a label, as on the rest of the crafted dresses.
            var loop = new PartNode("HangLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(0.170f, 0.042f, 0.058f);
            loop.localEulerAngles = new Vector3(0f, 26f, 0f);
            Block(loop, "LoopA", new Vector3(0f, 0f, -0.011f), new Vector3(0.030f, 0.005f, 0.005f), twineMat);
            Block(loop, "LoopB", new Vector3(0f, 0f, 0.011f), new Vector3(0.030f, 0.005f, 0.005f), twineMat);
            Block(loop, "LoopEnd", new Vector3(0.016f, 0f, 0f), new Vector3(0.005f, 0.005f, 0.026f), twineMat);

            return root;
        }

        /// <summary>
        /// A long dress cut down from a pair of old stonewashed jeans, laid flat face-up with shoulders at
        /// +X and hem at −X. It closes out the crafted denim set as the pale one, and where the black dress
        /// shows its wear by going grey, this one has already been washed almost white to begin with — so
        /// its wear has to be structural instead. The cloth is blotched and cloudy from the stones, bleached
        /// out in patches, and worn clean through in two places where the ladders of bare warp thread bridge
        /// the holes. What is left is soft rather than stiff, and it is finished in the light-denim idiom:
        /// white contrast topstitching, bright brass, and a pale frayed hem that hardly reads against it.
        /// </summary>
        private static PartNode BuildDressLongCraftedDenimLight(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Washed-out blue. As with the other two denims the catalogue tint is held right back, since the
            // colour is the entire reason this variant exists separately from the indigo dress.
            Color washed = new Color(0.615f, 0.680f, 0.760f);
            Color fabric = Color.Lerp(washed, def.Tint, 0.10f);                                   // soft stonewashed denim
            Color fabricDeep = new Color(fabric.r * 0.70f, fabric.g * 0.73f, fabric.b * 0.80f);   // seam shadow and fold valleys
            Color fabricAlt = Color.Lerp(fabric, new Color(0.50f, 0.57f, 0.67f), 0.60f);          // a panel off a darker pair
            Color twill = Color.Lerp(fabric, new Color(0.46f, 0.53f, 0.64f), 0.40f);              // the diagonal wale, read dark here
            Color cloud = Color.Lerp(fabric, new Color(0.86f, 0.89f, 0.93f), 0.60f);              // cloudy blotching left by the stones
            Color bleach = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.89f), 0.80f);             // splashed bleach, near white
            Color warp = new Color(0.88f, 0.89f, 0.90f);                                          // bare threads laddering across the holes
            Color topstitch = new Color(0.93f, 0.92f, 0.88f);                                     // white contrast thread
            Color brass = new Color(0.78f, 0.62f, 0.28f);                                         // bright rivets, zip and shank button
            Color lining = new Color(fabric.r * 0.48f, fabric.g * 0.48f, fabric.b * 0.52f);       // the dark inside, seen through holes
            Color label = new Color(0.68f, 0.52f, 0.34f);                                         // sun-faded leather waistband patch

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.13f);   // washed soft: the most reflective of the three denims
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.10f);
            PartMat fabricAltMat = PartMat.Of(fabricAlt, smoothness: 0.12f);
            PartMat twillMat = PartMat.Of(twill, smoothness: 0.11f);
            PartMat cloudMat = PartMat.Of(cloud, smoothness: 0.15f);
            PartMat bleachMat = PartMat.Of(bleach, smoothness: 0.16f);
            PartMat warpMat = PartMat.Of(warp, smoothness: 0.09f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.14f);
            PartMat brassMat = PartMat.Of(brass, smoothness: 0.78f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.34f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // felled seams, the fade lines and the warp ladders, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: the family silhouette, but softer-edged — this denim has been washed limp ───────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.052f, 0.220f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.066f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.230f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.052f, 0f, 0f), Vector3.zero, new Vector3(0.13f, 0.052f, 0.194f), fabricMat);
            Sphere(root, "SkirtTop", new Vector3(-0.195f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.250f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.345f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.050f, 0.278f), fabricAltMat);
            Sphere(root, "SkirtLow", new Vector3(-0.480f, -0.002f, 0f), Vector3.zero, new Vector3(0.17f, 0.048f, 0.292f), fabricMat);

            // ── Twill wale: same right-hand lean as its siblings, but darker than the cloth for once ──────
            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Lerp(0.160f, -0.520f, i / 25f);
                float halfZ = Mathf.Lerp(0.092f, 0.132f, i / 25f);
                var wale = Block(root, "Wale" + i, new Vector3(x, 0.032f, 0f),
                    new Vector3(0.006f, 0.005f, halfZ * 2.05f), twillMat);
                wale.localEulerAngles = new Vector3(0f, 63f, 0f);
            }

            // ── Stonewash blotching: soft cloudy patches with no edges, scattered over the whole dress ────
            for (int i = 0; i < 14; i++)
            {
                float x = Mathf.Lerp(0.140f, -0.500f, i / 13f) + Mathf.Sin(i * 3.1f) * 0.030f;
                float z = Mathf.Sin(i * 2.2f) * 0.100f;
                Sphere(root, "Cloud" + i, new Vector3(x, 0.034f, z), new Vector3(0f, i * 29f, 0f),
                    new Vector3(0.090f + (i % 3) * 0.030f, 0.011f, 0.058f + (i % 4) * 0.016f), cloudMat);
            }

            // ── Felled seams, doubled and picked out in white the way pale jeans always are ───────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "A",
                    new Vector3(0.130f, 0f, s * 0.098f), new Vector3(-0.500f, 0f, s * 0.140f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "B",
                    new Vector3(0.130f, 0f, s * 0.084f), new Vector3(-0.500f, 0f, s * 0.126f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledRidgeL" : "FelledRidgeR"),
                    new Vector3(0.130f, 0f, s * 0.091f), new Vector3(-0.500f, 0f, s * 0.133f), 0.020f, 0.030f, 0.012f, fabricDeepMat);
            }
            Band("PanelSeamA", new Vector3(-0.128f, 0f, -0.212f), new Vector3(-0.128f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamB", new Vector3(-0.140f, 0f, -0.212f), new Vector3(-0.140f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamRidge", new Vector3(-0.134f, 0f, -0.210f), new Vector3(-0.134f, 0f, 0.210f), 0.024f, 0.030f, 0.012f, fabricDeepMat);
            Band("PanelSeamLowA", new Vector3(-0.418f, 0f, -0.248f), new Vector3(-0.418f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamLowB", new Vector3(-0.430f, 0f, -0.248f), new Vector3(-0.430f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);

            // ── Neckline: bound in white-stitched binding, the tidiest edge on the whole crafted set ──────
            Sphere(root, "Neckline", new Vector3(0.162f, 0.012f, 0f), Vector3.zero, new Vector3(0.074f, 0.034f, 0.118f), liningMat);
            const int bindings = 14;
            for (int i = 0; i < bindings; i++)
            {
                float deg = i * 360f / bindings;
                float rad = deg * Mathf.Deg2Rad;
                var bind = Block(root, "Binding" + i,
                    new Vector3(0.162f + 0.046f * Mathf.Cos(rad), 0.026f, 0.072f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.024f, 0.012f), fabricAltMat);
                bind.localEulerAngles = new Vector3(0f, -deg, 0f);
                var st = Block(root, "BindStitch" + i,
                    new Vector3(0.162f + 0.058f * Mathf.Cos(rad), 0.040f, 0.090f * Mathf.Sin(rad)),
                    new Vector3(0.013f, 0.005f, 0.006f), topstitchMat);
                st.localEulerAngles = new Vector3(0f, -deg, 0f);
            }
            Sphere(root, "ShankButton", new Vector3(0.146f, 0.040f, 0.062f), Vector3.zero, new Vector3(0.020f, 0.011f, 0.020f), brassMat);
            Sphere(root, "ShankDish", new Vector3(0.146f, 0.045f, 0.062f), Vector3.zero, new Vector3(0.011f, 0.005f, 0.011f), fabricDeepMat);

            // ── Brass zip up the back ─────────────────────────────────────────────────────────────────────
            var zip = new PartNode("Zip");
            zip.SetParent(root, false);
            zip.localPosition = new Vector3(0.030f, 0.036f, -0.014f);
            Block(zip, "Tape", Vector3.zero, new Vector3(0.230f, 0.008f, 0.026f), fabricAltMat);
            for (int i = 0; i < 18; i++)
            {
                float x = (i / 17f - 0.5f) * 0.215f;
                int s = (i % 2 == 0) ? 1 : -1;
                Block(zip, "Tooth" + i, new Vector3(x, 0.006f, s * 0.005f), new Vector3(0.007f, 0.005f, 0.011f), brassMat);
            }
            Block(zip, "Slider", new Vector3(-0.086f, 0.008f, 0f), new Vector3(0.026f, 0.008f, 0.020f), brassMat);
            Block(zip, "Pull", new Vector3(-0.110f, 0.010f, 0f), new Vector3(0.026f, 0.005f, 0.010f), brassMat);
            for (int s = -1; s <= 1; s += 2)
                Block(zip, s > 0 ? "TapeStitchL" : "TapeStitchR", new Vector3(0f, 0.008f, s * 0.015f),
                    new Vector3(0.220f, 0.005f, 0.005f), topstitchMat);

            // ── Worn clean through in two places: a hole with bare warp laddering across it ───────────────
            // The black dress shows its age by going grey, which this cloth is already too pale to do — so
            // this one is worn structurally instead, and the ladders are the feature the whole model turns on.
            for (int h = 0; h < 2; h++)
            {
                int s = h == 0 ? 1 : -1;
                var hole = new PartNode(h == 0 ? "HoleUpper" : "HoleLower");
                hole.SetParent(root, false);
                hole.localPosition = new Vector3(-0.238f - h * 0.135f, 0.032f, s * (0.058f + h * 0.020f));
                hole.localEulerAngles = new Vector3(0f, s * 13f, 0f);

                float wide = 0.062f + h * 0.020f;
                // The dark inside of the dress showing through the gap.
                Block(hole, "Void", Vector3.zero, new Vector3(wide, 0.014f, 0.052f), liningMat);
                // Softened, thinned cloth all round the opening before it gives out entirely.
                for (int i = 0; i < 10; i++)
                {
                    float deg = i * 36f;
                    float rad = deg * Mathf.Deg2Rad;
                    var lip = Block(hole, "Lip" + i,
                        new Vector3(wide * 0.62f * Mathf.Cos(rad), 0.006f, 0.036f * Mathf.Sin(rad)),
                        new Vector3(0.020f, 0.010f, 0.014f), cloudMat);
                    lip.localEulerAngles = new Vector3(0f, -deg, 0f);
                }
                // The ladders themselves: bare warp threads still bridging the hole, sagging and uneven.
                int rungs = 6 + h;
                for (int i = 0; i < rungs; i++)
                {
                    float x = (i / (rungs - 1f) - 0.5f) * wide * 0.90f;
                    var rung = Block(hole, "Ladder" + i, new Vector3(x, 0.010f, Mathf.Sin(i * 1.9f) * 0.004f),
                        new Vector3(0.005f, 0.004f, 0.058f + (i % 3) * 0.006f), warpMat);
                    rung.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 2.4f) * 7f, 0f);
                }
                // A couple of threads that have already snapped and curled off to one side.
                Block(hole, "SnappedA", new Vector3(wide * 0.40f, 0.012f, 0.040f), new Vector3(0.004f, 0.004f, 0.026f), warpMat);
                Block(hole, "SnappedB", new Vector3(-wide * 0.30f, 0.012f, -0.042f), new Vector3(0.004f, 0.004f, 0.020f), warpMat);
            }

            // ── Brass rivets at the stress points, still bright against the pale cloth ────────────────────
            Vector2[] rivets =
            {
                new Vector2(-0.118f,  0.108f), new Vector2(-0.118f, -0.108f),
                new Vector2(-0.150f,  0.086f), new Vector2(-0.150f, -0.086f),
                new Vector2( 0.096f,  0.096f), new Vector2( 0.096f, -0.096f),
            };
            for (int i = 0; i < rivets.Length; i++)
            {
                Sphere(root, "Rivet" + i, new Vector3(rivets[i].x, 0.040f, rivets[i].y), Vector3.zero,
                    new Vector3(0.014f, 0.009f, 0.014f), brassMat);
                Sphere(root, "RivetDish" + i, new Vector3(rivets[i].x, 0.045f, rivets[i].y), Vector3.zero,
                    new Vector3(0.006f, 0.004f, 0.006f), fabricDeepMat);
            }

            // ── Splashed bleach: hard-edged near-white spots, quite unlike the soft stonewash clouding ────
            Vector2[] splashes =
            {
                new Vector2(-0.086f,  0.132f), new Vector2(-0.118f,  0.156f), new Vector2(-0.150f,  0.126f),
                new Vector2(-0.404f, -0.116f), new Vector2(-0.436f, -0.146f), new Vector2( 0.052f, -0.128f),
            };
            for (int i = 0; i < splashes.Length; i++)
            {
                float d = 0.020f + (i % 3) * 0.014f;
                Sphere(root, "BleachSpot" + i, new Vector3(splashes[i].x, 0.038f, splashes[i].y), Vector3.zero,
                    new Vector3(d, 0.009f, d * 0.82f), bleachMat);
            }
            Band("BleachRun", new Vector3(-0.130f, 0f, 0.140f), new Vector3(-0.220f, 0f, 0.172f), 0.012f, 0.038f, 0.007f, bleachMat);

            // Washed-soft folds: rounded rather than the sharp creases the stiff denims get.
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f * 2f - 1f;
                Sphere(root, "Fold" + i, new Vector3(-0.330f + Mathf.Abs(t) * 0.045f, 0.036f, t * 0.104f),
                    new Vector3(0f, t * 8f, 0f), new Vector3(0.340f, 0.013f, 0.034f), cloudMat);
                Sphere(root, "FoldShade" + i, new Vector3(-0.338f + Mathf.Abs(t) * 0.045f, 0.031f, t * 0.104f + 0.026f),
                    new Vector3(0f, t * 8f, 0f), new Vector3(0.310f, 0.010f, 0.020f), fabricDeepMat);
            }

            // ── Frayed hem, gone almost white, so it barely separates from the cloth above it ─────────────
            Block(root, "HemCut", new Vector3(-0.548f, 0.004f, 0f), new Vector3(0.018f, 0.048f, 0.292f), bleachMat);
            Block(root, "HemStay", new Vector3(-0.526f, 0.034f, 0f), new Vector3(0.006f, 0.006f, 0.286f), topstitchMat);
            for (int i = 0; i < 16; i++)
            {
                float z = (i / 15f - 0.5f) * 0.280f;
                float len = 0.018f + Mathf.Abs(Mathf.Sin(i * 2.6f)) * 0.032f;   // every thread a different length
                var thread = Block(root, "FrayThread" + i, new Vector3(-0.560f - len * 0.5f, 0.026f, z),
                    new Vector3(len, 0.005f, 0.004f), warpMat);
                thread.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.5f) * 22f, 0f);
            }

            // The sun-faded leather waistband patch off the old jeans, resewn at the back neck.
            var patch = new PartNode("LeatherPatch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(0.132f, 0.040f, 0.070f);
            patch.localEulerAngles = new Vector3(0f, 19f, 0f);
            Block(patch, "PatchLeather", Vector3.zero, new Vector3(0.030f, 0.006f, 0.042f), labelMat);
            Block(patch, "PatchEmboss", new Vector3(0f, 0.004f, 0f), new Vector3(0.016f, 0.004f, 0.026f), fabricDeepMat);
            for (int i = 0; i < 4; i++)
                Block(patch, "PatchStitch" + i, new Vector3(-0.017f + (i % 2) * 0.034f, 0.004f, -0.024f + (i / 2) * 0.048f),
                    new Vector3(0.008f, 0.004f, 0.008f), topstitchMat);

            return root;
        }

        /// <summary>
        /// A long dress cut down from black jeans, laid flat face-up with shoulders at +X and hem at −X. It
        /// shares the indigo dress's twill and its jeans construction, but everything the indigo one does in
        /// gold and copper this one does dark and tonal, so the whole garment reads as one flat black mass
        /// broken only by wear: black denim is dyed on the surface rather than through, so it goes grey
        /// wherever it has been rubbed and shows a pale weft along every cut edge. That is used as the whole
        /// visual language here — crocked grey creases, silvered abrasion on the raised folds, chalky ends
        /// on a raw cut hem, and gunmetal hardware that barely separates from the cloth.
        /// </summary>
        private static PartNode BuildDressLongCraftedDenimBlack(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Sulphur-black denim, kept almost free of the catalogue tint: this variant exists to be the
            // black one, and letting the tint lift it would collapse it back into the indigo dress.
            Color jet = new Color(0.135f, 0.130f, 0.145f);
            Color fabric = Color.Lerp(jet, def.Tint, 0.08f);                                      // black denim, dyed on the surface
            Color fabricDeep = new Color(fabric.r * 0.58f, fabric.g * 0.58f, fabric.b * 0.64f);   // seam shadow, the darkest note
            Color fabricAlt = Color.Lerp(fabric, new Color(0.26f, 0.25f, 0.28f), 0.60f);          // a panel off a more worn pair
            Color twill = Color.Lerp(fabric, new Color(0.34f, 0.33f, 0.37f), 0.45f);              // the diagonal wale, only just visible
            Color crock = Color.Lerp(fabric, new Color(0.52f, 0.51f, 0.53f), 0.62f);              // grey where the dye has rubbed off
            Color abrade = Color.Lerp(fabric, new Color(0.68f, 0.67f, 0.70f), 0.70f);             // silvered high points of the folds
            Color weft = new Color(0.72f, 0.70f, 0.68f);                                          // the pale undyed weft at cut edges
            Color topstitch = Color.Lerp(fabric, new Color(0.40f, 0.39f, 0.38f), 0.55f);          // tonal black thread, not gold
            Color gunmetal = new Color(0.30f, 0.30f, 0.33f);                                      // darkened rivets and zip
            Color lining = new Color(fabric.r * 0.50f, fabric.g * 0.50f, fabric.b * 0.55f);       // the inside, seen through the neck
            Color label = new Color(0.22f, 0.20f, 0.20f);                                         // black leather waistband patch

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.07f);   // flatter than indigo: black denim kills highlights
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.05f);
            PartMat fabricAltMat = PartMat.Of(fabricAlt, smoothness: 0.07f);
            PartMat twillMat = PartMat.Of(twill, smoothness: 0.10f);
            PartMat crockMat = PartMat.Of(crock, smoothness: 0.09f);
            PartMat abradeMat = PartMat.Of(abrade, smoothness: 0.14f);
            PartMat weftMat = PartMat.Of(weft, smoothness: 0.08f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.12f);
            PartMat gunmetalMat = PartMat.Of(gunmetal, smoothness: 0.66f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.05f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.30f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // felled seams, the fade lines and the cut edges, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: the same stiff slab-sided column as the indigo dress ────────────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.056f, 0.218f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.066f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.058f, 0.228f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.052f, 0f, 0f), Vector3.zero, new Vector3(0.13f, 0.056f, 0.192f), fabricMat);
            Sphere(root, "SkirtTop", new Vector3(-0.195f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.056f, 0.248f), fabricAltMat);
            Sphere(root, "SkirtMid", new Vector3(-0.345f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.276f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.480f, -0.002f, 0f), Vector3.zero, new Vector3(0.17f, 0.052f, 0.290f), fabricAltMat);

            // ── Twill wale: the same right-hand lean, but pitched so low it is barely there ───────────────
            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Lerp(0.160f, -0.520f, i / 25f);
                float halfZ = Mathf.Lerp(0.092f, 0.132f, i / 25f);
                var wale = Block(root, "Wale" + i, new Vector3(x, 0.032f, 0f),
                    new Vector3(0.006f, 0.005f, halfZ * 2.05f), twillMat);
                wale.localEulerAngles = new Vector3(0f, 63f, 0f);
            }

            // ── Felled seams, doubled as on any jeans, but in thread matched to the cloth ─────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "A",
                    new Vector3(0.130f, 0f, s * 0.098f), new Vector3(-0.500f, 0f, s * 0.140f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "B",
                    new Vector3(0.130f, 0f, s * 0.084f), new Vector3(-0.500f, 0f, s * 0.126f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledRidgeL" : "FelledRidgeR"),
                    new Vector3(0.130f, 0f, s * 0.091f), new Vector3(-0.500f, 0f, s * 0.133f), 0.020f, 0.030f, 0.012f, fabricDeepMat);
                // Where the seam stands proud it has rubbed grey along its whole length — on black denim the
                // seams are the first thing to go, and they outline the garment as a result.
                Band((s > 0 ? "SeamCrockL" : "SeamCrockR"),
                    new Vector3(0.128f, 0f, s * 0.091f), new Vector3(-0.496f, 0f, s * 0.133f), 0.008f, 0.042f, 0.005f, crockMat);
            }
            Band("PanelSeamA", new Vector3(-0.128f, 0f, -0.212f), new Vector3(-0.128f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamB", new Vector3(-0.140f, 0f, -0.212f), new Vector3(-0.140f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamRidge", new Vector3(-0.134f, 0f, -0.210f), new Vector3(-0.134f, 0f, 0.210f), 0.024f, 0.030f, 0.012f, fabricDeepMat);
            Band("PanelSeamCrock", new Vector3(-0.134f, 0f, -0.208f), new Vector3(-0.134f, 0f, 0.208f), 0.009f, 0.042f, 0.005f, crockMat);
            Band("PanelSeamLowA", new Vector3(-0.418f, 0f, -0.248f), new Vector3(-0.418f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamLowB", new Vector3(-0.430f, 0f, -0.248f), new Vector3(-0.430f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);

            // ── Neckline: cut open and left unfaced, so the pale weft shows all round the raw edge ────────
            Sphere(root, "Neckline", new Vector3(0.162f, 0.012f, 0f), Vector3.zero, new Vector3(0.074f, 0.036f, 0.118f), liningMat);
            const int edges = 14;
            for (int i = 0; i < edges; i++)
            {
                float deg = i * 360f / edges;
                float rad = deg * Mathf.Deg2Rad;
                var cut = Block(root, "NeckCut" + i,
                    new Vector3(0.162f + 0.046f * Mathf.Cos(rad), 0.030f, 0.072f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.020f, 0.010f), weftMat);
                cut.localEulerAngles = new Vector3(0f, -deg, 0f);
                var stay = Block(root, "NeckStay" + i,
                    new Vector3(0.162f + 0.058f * Mathf.Cos(rad), 0.040f, 0.090f * Mathf.Sin(rad)),
                    new Vector3(0.012f, 0.005f, 0.006f), topstitchMat);
                stay.localEulerAngles = new Vector3(0f, -deg, 0f);
            }
            Sphere(root, "ShankButton", new Vector3(0.146f, 0.040f, 0.062f), Vector3.zero, new Vector3(0.020f, 0.011f, 0.020f), gunmetalMat);
            Sphere(root, "ShankDish", new Vector3(0.146f, 0.045f, 0.062f), Vector3.zero, new Vector3(0.011f, 0.005f, 0.011f), fabricDeepMat);

            // ── Blackened zip up the back, its teeth reading as texture rather than glint ─────────────────
            var zip = new PartNode("Zip");
            zip.SetParent(root, false);
            zip.localPosition = new Vector3(0.030f, 0.036f, -0.014f);
            Block(zip, "Tape", Vector3.zero, new Vector3(0.230f, 0.008f, 0.026f), fabricDeepMat);
            for (int i = 0; i < 18; i++)
            {
                float x = (i / 17f - 0.5f) * 0.215f;
                int s = (i % 2 == 0) ? 1 : -1;
                Block(zip, "Tooth" + i, new Vector3(x, 0.006f, s * 0.005f), new Vector3(0.007f, 0.005f, 0.011f), gunmetalMat);
            }
            Block(zip, "Slider", new Vector3(-0.086f, 0.008f, 0f), new Vector3(0.026f, 0.008f, 0.020f), gunmetalMat);
            Block(zip, "Pull", new Vector3(-0.110f, 0.010f, 0f), new Vector3(0.026f, 0.005f, 0.010f), gunmetalMat);
            for (int s = -1; s <= 1; s += 2)
                Block(zip, s > 0 ? "TapeStitchL" : "TapeStitchR", new Vector3(0f, 0.008f, s * 0.015f),
                    new Vector3(0.220f, 0.005f, 0.005f), topstitchMat);

            // ── The pockets were unpicked off this pair, leaving their ghosts behind ──────────────────────
            // A removed pocket leaves the cloth under it unfaded, so its outline sits darker than the rest —
            // the neatest way to say the same jeans origin without repeating the indigo dress's pocket.
            for (int p = 0; p < 2; p++)
            {
                int s = p == 0 ? 1 : -1;
                var ghost = new PartNode(p == 0 ? "PocketGhostL" : "PocketGhostR");
                ghost.SetParent(root, false);
                ghost.localPosition = new Vector3(-0.252f - p * 0.020f, 0.034f, s * 0.086f);
                ghost.localEulerAngles = new Vector3(0f, s * -6f, 0f);
                Block(ghost, "Shadow", Vector3.zero, new Vector3(0.104f, 0.010f, 0.092f), fabricMat);
                // Holes left by the thread that used to hold it on, right round the outline.
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 14f;
                    Vector3 pos = t < 0.25f ? new Vector3(0.050f, 0.006f, -0.044f + t * 0.352f)
                                : t < 0.50f ? new Vector3(0.050f - (t - 0.25f) * 0.400f, 0.006f, 0.044f)
                                : t < 0.75f ? new Vector3(-0.050f, 0.006f, 0.044f - (t - 0.50f) * 0.352f)
                                            : new Vector3(-0.050f + (t - 0.75f) * 0.400f, 0.006f, -0.044f);
                    Block(ghost, "NeedleHole" + i, pos, new Vector3(0.008f, 0.005f, 0.005f), crockMat);
                }
            }

            // ── Gunmetal rivets left in at the stress points, dulled rather than bright ───────────────────
            Vector2[] rivets =
            {
                new Vector2(-0.118f,  0.108f), new Vector2(-0.118f, -0.108f),
                new Vector2(-0.150f,  0.086f), new Vector2(-0.150f, -0.086f),
                new Vector2( 0.096f,  0.096f), new Vector2( 0.096f, -0.096f),
            };
            for (int i = 0; i < rivets.Length; i++)
            {
                Sphere(root, "Rivet" + i, new Vector3(rivets[i].x, 0.040f, rivets[i].y), Vector3.zero,
                    new Vector3(0.014f, 0.009f, 0.014f), gunmetalMat);
                // The dye has worn away in a ring round each rivet where the metal chafes the cloth.
                Sphere(root, "RivetHalo" + i, new Vector3(rivets[i].x, 0.036f, rivets[i].y), Vector3.zero,
                    new Vector3(0.030f, 0.008f, 0.030f), crockMat);
            }

            // ── Crocking: the grey the dye rubs away to, laid on wherever the cloth gets handled ─────────
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.160f - i * 0.026f;
                    Band((s > 0 ? "CrockLineL" : "CrockLineR") + i,
                        new Vector3(x, 0f, s * 0.030f), new Vector3(x - 0.052f, 0f, s * (0.098f + i * 0.008f)),
                        0.010f, 0.038f, 0.006f, crockMat);
                }
            }
            Sphere(root, "SeatCrock", new Vector3(-0.230f, 0.036f, -0.020f), new Vector3(0f, 8f, 0f), new Vector3(0.18f, 0.012f, 0.120f), crockMat);
            Sphere(root, "LapCrock", new Vector3(-0.060f, 0.038f, 0.052f), new Vector3(0f, -12f, 0f), new Vector3(0.13f, 0.011f, 0.070f), crockMat);

            // Hard creases with silvered abrasion along their crowns — the high points rub first and lightest.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f * 2f - 1f;
                var ridge = Block(root, "Crease" + i, new Vector3(-0.330f + Mathf.Abs(t) * 0.050f, 0.038f, t * 0.100f),
                    new Vector3(0.330f, 0.010f, 0.014f), twillMat);
                ridge.localEulerAngles = new Vector3(0f, t * 7f, 0f);
                var shine = Block(root, "CreaseAbrade" + i, new Vector3(-0.330f + Mathf.Abs(t) * 0.050f, 0.044f, t * 0.100f),
                    new Vector3(0.290f, 0.005f, 0.007f), abradeMat);
                shine.localEulerAngles = new Vector3(0f, t * 7f, 0f);
                var valley = Block(root, "CreaseShade" + i, new Vector3(-0.336f + Mathf.Abs(t) * 0.050f, 0.032f, t * 0.100f + 0.022f),
                    new Vector3(0.310f, 0.008f, 0.010f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, t * 7f, 0f);
            }

            // ── Raw cut hem: never turned, and fraying out in pale weft against the black ────────────────
            Block(root, "HemCut", new Vector3(-0.548f, 0.004f, 0f), new Vector3(0.018f, 0.052f, 0.290f), weftMat);
            Block(root, "HemStay", new Vector3(-0.526f, 0.036f, 0f), new Vector3(0.006f, 0.006f, 0.284f), topstitchMat);
            for (int i = 0; i < 16; i++)
            {
                float z = (i / 15f - 0.5f) * 0.278f;
                float len = 0.016f + Mathf.Abs(Mathf.Sin(i * 2.9f)) * 0.030f;   // every thread a different length
                var thread = Block(root, "FrayThread" + i, new Vector3(-0.560f - len * 0.5f, 0.028f, z),
                    new Vector3(len, 0.005f, 0.004f), weftMat);
                thread.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.7f) * 24f, 0f);
            }
            // A curtain of loose warp hanging just above the fray, greyer than the threads below it.
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f - 0.5f) * 0.260f;
                Block(root, "LooseWarp" + i, new Vector3(-0.540f, 0.034f, z), new Vector3(0.028f, 0.005f, 0.006f), crockMat);
            }

            // The black leather waistband patch off the old jeans, resewn at the back neck.
            var patch = new PartNode("LeatherPatch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(0.132f, 0.040f, 0.070f);
            patch.localEulerAngles = new Vector3(0f, 19f, 0f);
            Block(patch, "PatchLeather", Vector3.zero, new Vector3(0.030f, 0.006f, 0.042f), labelMat);
            Block(patch, "PatchEmboss", new Vector3(0f, 0.004f, 0f), new Vector3(0.016f, 0.004f, 0.026f), crockMat);
            for (int i = 0; i < 4; i++)
                Block(patch, "PatchStitch" + i, new Vector3(-0.017f + (i % 2) * 0.034f, 0.004f, -0.024f + (i / 2) * 0.048f),
                    new Vector3(0.008f, 0.004f, 0.008f), topstitchMat);

            return root;
        }

        /// <summary>
        /// A long dress cut down from old jeans, laid flat face-up with shoulders at +X and hem at −X. It is
        /// the third of the crafted long dresses and is the heavy one: stiff indigo denim that holds a shape
        /// rather than draping, ruled all over with the diagonal twill lines the other two have not got, and
        /// put together with the hardware jeans come with — copper rivets, a metal zip up the back, and gold
        /// contrast topstitching doubled along every felled seam. Being made from several old pairs shows in
        /// the mismatched panel shades, a back pocket left on the skirt with its arcuate stitching intact,
        /// and pale whiskering worn into the cloth where the original garments used to crease.
        /// </summary>
        private static PartNode BuildDressLongCraftedDenim(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Indigo is the whole identity of this variant, so the catalogue tint barely registers — the
            // black and light denim siblings are what the colour axis is for.
            Color indigo = new Color(0.225f, 0.310f, 0.455f);
            Color fabric = Color.Lerp(indigo, def.Tint, 0.10f);                                   // heavy indigo denim
            Color fabricDeep = new Color(fabric.r * 0.66f, fabric.g * 0.68f, fabric.b * 0.74f);   // shaded creases, seam shadow
            Color fabricAlt = Color.Lerp(fabric, new Color(0.34f, 0.42f, 0.55f), 0.55f);          // a panel off a different pair
            Color twill = Color.Lerp(fabric, new Color(0.52f, 0.60f, 0.72f), 0.38f);              // the diagonal weave lines
            Color whisker = Color.Lerp(fabric, new Color(0.72f, 0.77f, 0.84f), 0.55f);            // worn pale fade lines
            Color topstitch = new Color(0.80f, 0.64f, 0.28f);                                     // gold contrast thread
            Color copper = new Color(0.72f, 0.46f, 0.22f);                                        // rivets and the shank button
            Color steel = new Color(0.68f, 0.70f, 0.73f);                                         // zip teeth and pull
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.46f);       // dark inside, seen through the neck
            Color label = new Color(0.55f, 0.36f, 0.22f);                                         // leather patch off the waistband

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.09f);   // dry denim: a touch of sheen, no more
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.07f);
            PartMat fabricAltMat = PartMat.Of(fabricAlt, smoothness: 0.09f);
            PartMat twillMat = PartMat.Of(twill, smoothness: 0.12f);
            PartMat whiskerMat = PartMat.Of(whisker, smoothness: 0.10f);
            PartMat topstitchMat = PartMat.Of(topstitch, smoothness: 0.16f);
            PartMat copperMat = PartMat.Of(copper, smoothness: 0.72f);
            PartMat steelMat = PartMat.Of(steel, smoothness: 0.80f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.06f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.35f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // twill lines, the felled seams and the whiskering, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: stiff and slab-sided, holding its own shape the way heavy denim does ────────────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.056f, 0.218f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.066f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.058f, 0.228f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.052f, 0f, 0f), Vector3.zero, new Vector3(0.13f, 0.056f, 0.192f), fabricMat);
            Sphere(root, "SkirtTop", new Vector3(-0.195f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.056f, 0.248f), fabricAltMat);
            Sphere(root, "SkirtMid", new Vector3(-0.345f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.276f), fabricAltMat);
            Sphere(root, "SkirtLow", new Vector3(-0.480f, -0.002f, 0f), Vector3.zero, new Vector3(0.17f, 0.052f, 0.290f), fabricMat);

            // ── Twill: the diagonal wale running across the whole dress, denim's one unmistakable tell ────
            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Lerp(0.160f, -0.520f, i / 25f);
                float halfZ = Mathf.Lerp(0.092f, 0.132f, i / 25f);
                var wale = Block(root, "Wale" + i, new Vector3(x, 0.032f, 0f),
                    new Vector3(0.006f, 0.005f, halfZ * 2.05f), twillMat);
                wale.localEulerAngles = new Vector3(0f, 63f, 0f);   // all leaning the same way, as a right-hand twill does
            }

            // ── Felled seams, each doubled in gold thread the way jeans seams always are ──────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "A",
                    new Vector3(0.130f, 0f, s * 0.098f), new Vector3(-0.500f, 0f, s * 0.140f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledSeamL" : "FelledSeamR") + "B",
                    new Vector3(0.130f, 0f, s * 0.084f), new Vector3(-0.500f, 0f, s * 0.126f), 0.006f, 0.038f, 0.005f, topstitchMat);
                Band((s > 0 ? "FelledRidgeL" : "FelledRidgeR"),
                    new Vector3(0.130f, 0f, s * 0.091f), new Vector3(-0.500f, 0f, s * 0.133f), 0.020f, 0.030f, 0.012f, fabricDeepMat);
            }
            // The join where the skirt panels were pieced in from another pair, doubled the same way.
            Band("PanelSeamA", new Vector3(-0.128f, 0f, -0.212f), new Vector3(-0.128f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamB", new Vector3(-0.140f, 0f, -0.212f), new Vector3(-0.140f, 0f, 0.212f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamRidge", new Vector3(-0.134f, 0f, -0.210f), new Vector3(-0.134f, 0f, 0.210f), 0.024f, 0.030f, 0.012f, fabricDeepMat);
            Band("PanelSeamLowA", new Vector3(-0.418f, 0f, -0.248f), new Vector3(-0.418f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);
            Band("PanelSeamLowB", new Vector3(-0.430f, 0f, -0.248f), new Vector3(-0.430f, 0f, 0.248f), 0.006f, 0.038f, 0.005f, topstitchMat);

            // ── Neckline: a plain faced scoop with a shank button and rivets at the corners ───────────────
            Sphere(root, "Neckline", new Vector3(0.162f, 0.012f, 0f), Vector3.zero, new Vector3(0.074f, 0.036f, 0.118f), liningMat);
            const int facings = 12;
            for (int i = 0; i < facings; i++)
            {
                float deg = i * 360f / facings;
                float rad = deg * Mathf.Deg2Rad;
                var face = Block(root, "NeckFacing" + i,
                    new Vector3(0.162f + 0.044f * Mathf.Cos(rad), 0.026f, 0.070f * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.022f, 0.012f), fabricDeepMat);
                face.localEulerAngles = new Vector3(0f, -deg, 0f);
                Block(root, "NeckStitch" + i,
                    new Vector3(0.162f + 0.056f * Mathf.Cos(rad), 0.038f, 0.088f * Mathf.Sin(rad)),
                    new Vector3(0.014f, 0.005f, 0.006f), topstitchMat).localEulerAngles = new Vector3(0f, -deg, 0f);
            }
            Sphere(root, "ShankButton", new Vector3(0.146f, 0.040f, 0.062f), Vector3.zero, new Vector3(0.020f, 0.011f, 0.020f), copperMat);
            Sphere(root, "ShankDish", new Vector3(0.146f, 0.045f, 0.062f), Vector3.zero, new Vector3(0.011f, 0.005f, 0.011f), fabricDeepMat);

            // ── Metal zip running up the back, teeth picked out one by one ────────────────────────────────
            var zip = new PartNode("Zip");
            zip.SetParent(root, false);
            zip.localPosition = new Vector3(0.030f, 0.036f, -0.014f);
            Block(zip, "Tape", Vector3.zero, new Vector3(0.230f, 0.008f, 0.026f), fabricDeepMat);
            for (int i = 0; i < 18; i++)
            {
                float x = (i / 17f - 0.5f) * 0.215f;
                int s = (i % 2 == 0) ? 1 : -1;
                Block(zip, "Tooth" + i, new Vector3(x, 0.006f, s * 0.005f), new Vector3(0.007f, 0.005f, 0.011f), steelMat);
            }
            Block(zip, "Slider", new Vector3(-0.086f, 0.008f, 0f), new Vector3(0.026f, 0.008f, 0.020f), steelMat);
            Block(zip, "Pull", new Vector3(-0.110f, 0.010f, 0f), new Vector3(0.026f, 0.005f, 0.010f), steelMat);
            for (int s = -1; s <= 1; s += 2)
                Block(zip, s > 0 ? "TapeStitchL" : "TapeStitchR", new Vector3(0f, 0.008f, s * 0.015f),
                    new Vector3(0.220f, 0.005f, 0.005f), topstitchMat);

            // ── A back pocket left on the skirt, arcuate stitching and rivets and all ─────────────────────
            var pocket = new PartNode("BackPocket");
            pocket.SetParent(root, false);
            pocket.localPosition = new Vector3(-0.258f, 0.036f, 0.084f);
            pocket.localEulerAngles = new Vector3(0f, -6f, 0f);
            Block(pocket, "PocketCloth", Vector3.zero, new Vector3(0.104f, 0.011f, 0.092f), fabricMat);
            Block(pocket, "PocketMouth", new Vector3(0.052f, 0.005f, 0f), new Vector3(0.014f, 0.011f, 0.092f), fabricDeepMat);
            Block(pocket, "MouthStitchA", new Vector3(0.044f, 0.008f, 0f), new Vector3(0.006f, 0.005f, 0.086f), topstitchMat);
            Block(pocket, "MouthStitchB", new Vector3(0.032f, 0.008f, 0f), new Vector3(0.006f, 0.005f, 0.086f), topstitchMat);
            // The arcuate: two shallow curves meeting in the middle, traced as short straight runs.
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float t = i / 4f;
                    float z = s * Mathf.Lerp(0.042f, 0.002f, t);
                    float x = Mathf.Lerp(0.012f, -0.030f, t) - t * t * 0.016f;
                    var arc = Block(pocket, (s > 0 ? "ArcuateL" : "ArcuateR") + i, new Vector3(x, 0.008f, z),
                        new Vector3(0.026f, 0.005f, 0.006f), topstitchMat);
                    arc.localEulerAngles = new Vector3(0f, s * (26f + i * 9f), 0f);
                }
            }
            for (int s = -1; s <= 1; s += 2)
                Sphere(pocket, s > 0 ? "PocketRivetL" : "PocketRivetR", new Vector3(0.040f, 0.008f, s * 0.038f),
                    Vector3.zero, new Vector3(0.013f, 0.008f, 0.013f), copperMat);

            // ── Copper rivets punched in at the stress points, as they are on the original jeans ──────────
            Vector2[] rivets =
            {
                new Vector2(-0.118f,  0.108f), new Vector2(-0.118f, -0.108f),
                new Vector2(-0.150f,  0.086f), new Vector2(-0.150f, -0.086f),
                new Vector2( 0.096f,  0.096f), new Vector2( 0.096f, -0.096f),
            };
            for (int i = 0; i < rivets.Length; i++)
            {
                Sphere(root, "Rivet" + i, new Vector3(rivets[i].x, 0.040f, rivets[i].y), Vector3.zero,
                    new Vector3(0.014f, 0.009f, 0.014f), copperMat);
                Sphere(root, "RivetDish" + i, new Vector3(rivets[i].x, 0.045f, rivets[i].y), Vector3.zero,
                    new Vector3(0.006f, 0.004f, 0.006f), fabricDeepMat);
            }

            // ── Whiskering: pale fade lines worn in where the old jeans used to crease ────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.160f - i * 0.026f;
                    Band((s > 0 ? "WhiskerL" : "WhiskerR") + i,
                        new Vector3(x, 0f, s * 0.030f), new Vector3(x - 0.052f, 0f, s * (0.098f + i * 0.008f)),
                        0.010f, 0.038f, 0.006f, whiskerMat);
                }
            }
            Band("HoneycombA", new Vector3(-0.300f, 0f, -0.070f), new Vector3(-0.300f, 0f, 0.070f), 0.012f, 0.038f, 0.007f, whiskerMat);
            Band("HoneycombB", new Vector3(-0.336f, 0f, -0.058f), new Vector3(-0.336f, 0f, 0.058f), 0.011f, 0.038f, 0.007f, whiskerMat);
            Sphere(root, "SeatFade", new Vector3(-0.230f, 0.036f, -0.020f), new Vector3(0f, 8f, 0f), new Vector3(0.18f, 0.012f, 0.120f), whiskerMat);

            // ── Cut hem turned up into a deep cuff, the usual way a dress gets made out of trousers ───────
            Block(root, "Cuff", new Vector3(-0.548f, 0.006f, 0f), new Vector3(0.056f, 0.056f, 0.286f), fabricAltMat);
            Block(root, "CuffEdge", new Vector3(-0.578f, 0.004f, 0f), new Vector3(0.014f, 0.052f, 0.286f), fabricDeepMat);
            Block(root, "CuffStitchA", new Vector3(-0.526f, 0.036f, 0f), new Vector3(0.006f, 0.006f, 0.278f), topstitchMat);
            Block(root, "CuffStitchB", new Vector3(-0.538f, 0.036f, 0f), new Vector3(0.006f, 0.006f, 0.278f), topstitchMat);
            for (int i = 0; i < 7; i++)
            {
                float z = (i / 6f - 0.5f) * 0.250f;
                Sphere(root, "CuffRoll" + i, new Vector3(-0.564f, 0.034f, z), Vector3.zero,
                    new Vector3(0.030f, 0.012f, 0.030f), twillMat);
            }

            // Denim creases hard rather than draping: a few sharp raised ridges with dark valleys beside them.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f * 2f - 1f;
                var ridge = Block(root, "Crease" + i, new Vector3(-0.330f + Mathf.Abs(t) * 0.050f, 0.038f, t * 0.100f),
                    new Vector3(0.330f, 0.010f, 0.014f), twillMat);
                ridge.localEulerAngles = new Vector3(0f, t * 7f, 0f);
                var valley = Block(root, "CreaseShade" + i, new Vector3(-0.336f + Mathf.Abs(t) * 0.050f, 0.032f, t * 0.100f + 0.022f),
                    new Vector3(0.310f, 0.008f, 0.010f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, t * 7f, 0f);
            }

            // The leather brand patch off the old waistband, kept and resewn at the back neck.
            var patch = new PartNode("LeatherPatch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(0.132f, 0.040f, 0.070f);
            patch.localEulerAngles = new Vector3(0f, 19f, 0f);
            Block(patch, "PatchLeather", Vector3.zero, new Vector3(0.030f, 0.006f, 0.042f), labelMat);
            Block(patch, "PatchEmboss", new Vector3(0f, 0.004f, 0f), new Vector3(0.016f, 0.004f, 0.026f), fabricDeepMat);
            for (int i = 0; i < 4; i++)
                Block(patch, "PatchStitch" + i, new Vector3(-0.017f + (i % 2) * 0.034f, 0.004f, -0.024f + (i / 2) * 0.048f),
                    new Vector3(0.008f, 0.004f, 0.008f), topstitchMat);

            return root;
        }

        /// <summary>
        /// A long dress run up at home out of bedsheet cotton, laid flat face-up with shoulders at +X and
        /// hem at −X. It is the burlap dress's better-made sibling and is built as the direct answer to it:
        /// the same hand-sewn origin, but soft pale cloth that falls in easy rounded folds instead of hard
        /// creases, a drawstring gathered neckline rather than a cut slit, a tiered prairie skirt whose
        /// gathers bunch at every tier seam, and a properly turned and hemmed edge with no fraying anywhere.
        /// The home-made tells are kept but made neater — small even hand stitches, a square patch of a
        /// different print set into the skirt, and a row of odd buttons that plainly came off other clothes.
        /// </summary>
        private static PartNode BuildDressLongCraftedCotton(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Undyed sheet cotton: pale and warm. The catalogue tint gets a little more say here than on the
            // burlap, since plain cotton plausibly takes a colour, but not enough to lose the bleached look.
            Color calico = new Color(0.855f, 0.830f, 0.745f);
            Color fabric = Color.Lerp(calico, def.Tint, 0.22f);                                   // soft washed cotton
            Color fabricDeep = new Color(fabric.r * 0.72f, fabric.g * 0.71f, fabric.b * 0.68f);   // shaded gather valleys
            Color fabricHi = Color.Lerp(fabric, Color.white, 0.30f);                              // sun-bleached raised folds
            Color print = Color.Lerp(fabric, new Color(0.42f, 0.48f, 0.52f), 0.55f);              // the odd patch, off another garment
            Color thread = Color.Lerp(fabric, new Color(0.52f, 0.50f, 0.44f), 0.55f);             // small even hand stitching
            Color drawstring = Color.Lerp(fabric, new Color(0.70f, 0.64f, 0.50f), 0.50f);         // cotton tape at the neck and waist
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.44f, fabric.b * 0.44f);       // dark inside, seen through the neck
            Color horn = new Color(0.58f, 0.50f, 0.36f);                                          // salvaged buttons, no two alike
            Color shell = new Color(0.88f, 0.86f, 0.80f);

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.11f);   // soft matte cotton: dull, but not sackcloth-dull
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.08f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.14f);
            PartMat printMat = PartMat.Of(print, smoothness: 0.10f);
            PartMat threadMat = PartMat.Of(thread, smoothness: 0.09f);
            PartMat drawstringMat = PartMat.Of(drawstring, smoothness: 0.10f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.07f);
            PartMat hornMat = PartMat.Of(horn, smoothness: 0.45f);
            PartMat shellMat = PartMat.Of(shell, smoothness: 0.60f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // tier seams, the casings and the ties, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: a soft column, gently shaped at the waist because cotton will take a shape ──────────
            Sphere(root, "Shoulders", new Vector3(0.152f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.050f, 0.220f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.068f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.232f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.048f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.048f, 0.186f), fabricMat);

            // ── Tiered prairie skirt: three rectangles, each gathered on to the one above it ──────────────
            float[] tierX = { -0.170f, -0.320f, -0.465f };
            float[] tierZ = { 0.250f, 0.290f, 0.320f };
            for (int t = 0; t < 3; t++)
            {
                Sphere(root, "Tier" + t, new Vector3(tierX[t], t == 2 ? -0.002f : 0f, 0f), Vector3.zero,
                    new Vector3(t == 2 ? 0.17f : 0.18f, 0.050f - t * 0.002f, tierZ[t]), fabricMat);

                // The seam this tier is gathered on to, and the bunching that gathering makes just below it.
                float seamX = tierX[t] + (t == 0 ? 0.088f : 0.078f);
                Band("TierSeam" + t, new Vector3(seamX, 0f, -tierZ[t] * 0.40f), new Vector3(seamX, 0f, tierZ[t] * 0.40f),
                    0.007f, 0.032f, 0.007f, threadMat);
                for (int g = 0; g < 9; g++)
                {
                    float z = (g / 8f - 0.5f) * tierZ[t] * 0.82f;
                    Sphere(root, "Gather" + t + "_" + g, new Vector3(seamX - 0.026f, 0.032f, z),
                        new Vector3(0f, Mathf.Sin(g * 1.3f + t) * 9f, 0f),
                        new Vector3(0.046f, 0.013f, 0.020f), fabricHiMat);
                    Sphere(root, "GatherShade" + t + "_" + g, new Vector3(seamX - 0.030f, 0.027f, z + 0.018f),
                        Vector3.zero, new Vector3(0.038f, 0.010f, 0.011f), fabricDeepMat);
                }
            }

            // Soft rounded folds falling the length of the skirt — cotton drapes, it does not stand up.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f * 2f - 1f;
                Sphere(root, "Drape" + i, new Vector3(-0.300f + Mathf.Abs(t) * 0.040f, 0.034f, t * 0.118f),
                    new Vector3(0f, t * 9f, 0f), new Vector3(0.400f, 0.014f, 0.040f), fabricHiMat);
                Sphere(root, "DrapeShade" + i, new Vector3(-0.310f + Mathf.Abs(t) * 0.040f, 0.029f, t * 0.118f + 0.032f),
                    new Vector3(0f, t * 9f, 0f), new Vector3(0.370f, 0.011f, 0.024f), fabricDeepMat);
            }

            // ── Drawstring neckline: the top edge gathered on a tape run through a stitched casing ────────
            Sphere(root, "Neckline", new Vector3(0.166f, 0.012f, 0f), Vector3.zero, new Vector3(0.070f, 0.032f, 0.120f), liningMat);
            Band("CasingTop", new Vector3(0.196f, 0f, -0.082f), new Vector3(0.196f, 0f, 0.082f), 0.022f, 0.030f, 0.020f, fabricMat);
            Band("CasingStitch", new Vector3(0.184f, 0f, -0.078f), new Vector3(0.184f, 0f, 0.078f), 0.006f, 0.040f, 0.005f, threadMat);
            for (int i = 0; i < 10; i++)
            {
                float z = (i / 9f - 0.5f) * 0.155f;
                Sphere(root, "NeckGather" + i, new Vector3(0.192f, 0.038f, z), new Vector3(0f, Mathf.Cos(i * 1.5f) * 12f, 0f),
                    new Vector3(0.030f, 0.014f, 0.014f), fabricHiMat);
            }
            // The tape itself, drawn out of the casing and knotted at the front.
            Band("TapeL", new Vector3(0.196f, 0f, 0.012f), new Vector3(0.244f, 0f, 0.058f), 0.009f, 0.042f, 0.008f, drawstringMat);
            Band("TapeR", new Vector3(0.196f, 0f, -0.010f), new Vector3(0.238f, 0f, -0.052f), 0.009f, 0.042f, 0.008f, drawstringMat);
            Sphere(root, "TapeKnot", new Vector3(0.198f, 0.044f, 0f), Vector3.zero, new Vector3(0.022f, 0.016f, 0.020f), drawstringMat);

            // ── Salvaged buttons down the front placket, plainly cut off several different garments ───────
            Band("Placket", new Vector3(0.150f, 0f, -0.010f), new Vector3(-0.050f, 0f, -0.010f), 0.030f, 0.030f, 0.010f, fabricHiMat);
            Band("PlacketStitch", new Vector3(0.148f, 0f, 0.006f), new Vector3(-0.048f, 0f, 0.006f), 0.005f, 0.038f, 0.005f, threadMat);
            const int buttons = 6;
            for (int i = 0; i < buttons; i++)
            {
                float x = Mathf.Lerp(0.140f, -0.040f, i / (buttons - 1f));
                bool pale = (i % 2 == 0);
                float d = 0.014f + (i % 3) * 0.003f;                     // no two the same size either
                Sphere(root, "Button" + i, new Vector3(x, 0.037f, -0.010f), Vector3.zero,
                    new Vector3(d, 0.008f, d), pale ? shellMat : hornMat);
                Sphere(root, "ButtonHole" + i, new Vector3(x, 0.041f, -0.010f), Vector3.zero,
                    new Vector3(d * 0.35f, 0.004f, d * 0.35f), fabricDeepMat);
            }

            // ── Small even hand stitching down the side seams: neat, but still visibly done by hand ───────
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 16; i++)
                {
                    float x = Mathf.Lerp(0.130f, -0.500f, i / 15f);
                    float halfZ = Mathf.Lerp(0.098f, 0.148f, i / 15f);
                    var st = Block(root, (s > 0 ? "SeamStitchL" : "SeamStitchR") + i,
                        new Vector3(x, 0.036f, s * halfZ), new Vector3(0.006f, 0.005f, 0.016f), threadMat);
                    st.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.4f) * 8f, 0f);   // far straighter than the burlap's
                }
            }

            // ── Set-in sleeves, short and softly gathered at the head ─────────────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var sleeve = new PartNode("Sleeve" + side);
                sleeve.SetParent(root, false);
                sleeve.localPosition = new Vector3(0.104f, 0f, s * 0.148f);
                sleeve.localEulerAngles = new Vector3(0f, s * -21f, 0f);

                Sphere(sleeve, "Head", Vector3.zero, Vector3.zero, new Vector3(0.115f, 0.048f, 0.110f), fabricMat);
                Sphere(sleeve, "Body", new Vector3(-0.008f, 0f, s * 0.082f), Vector3.zero, new Vector3(0.125f, 0.046f, 0.110f), fabricMat);
                Block(sleeve, "Cuff", new Vector3(-0.010f, 0.001f, s * 0.132f), new Vector3(0.120f, 0.048f, 0.018f), fabricHiMat);
                Block(sleeve, "CuffStitch", new Vector3(-0.010f, 0.026f, s * 0.118f), new Vector3(0.108f, 0.005f, 0.005f), threadMat);
                for (int i = 0; i < 5; i++)
                {
                    float x = (i / 4f - 0.5f) * 0.100f;
                    Sphere(sleeve, "HeadGather" + i, new Vector3(x, 0.028f, s * 0.014f),
                        new Vector3(0f, s * 20f, 0f), new Vector3(0.028f, 0.012f, 0.018f), fabricHiMat);
                }
            }

            // ── Waist tie: a strip of the same cotton, bowed off to one side ──────────────────────────────
            Band("Tie", new Vector3(-0.086f, 0f, -0.092f), new Vector3(-0.086f, 0f, 0.092f), 0.026f, 0.036f, 0.010f, drawstringMat);
            var bow = new PartNode("Bow");
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(-0.086f, 0.042f, 0.088f);
            bow.localEulerAngles = new Vector3(0f, -30f, 0f);
            Sphere(bow, "BowKnot", Vector3.zero, Vector3.zero, new Vector3(0.030f, 0.018f, 0.026f), drawstringMat);
            Sphere(bow, "LoopA", new Vector3(0.026f, -0.002f, 0.026f), new Vector3(0f, 38f, 0f), new Vector3(0.050f, 0.012f, 0.028f), drawstringMat);
            Sphere(bow, "LoopB", new Vector3(-0.010f, -0.002f, 0.038f), new Vector3(0f, -26f, 0f), new Vector3(0.046f, 0.012f, 0.026f), drawstringMat);
            Block(bow, "TailA", new Vector3(-0.052f, -0.003f, 0.014f), new Vector3(0.080f, 0.009f, 0.020f), drawstringMat);
            Block(bow, "TailB", new Vector3(-0.042f, -0.004f, -0.020f), new Vector3(0.066f, 0.008f, 0.018f), fabricDeepMat);

            // ── A square of some other print set into the skirt where the cotton ran short ────────────────
            var patch = new PartNode("Patch");
            patch.SetParent(root, false);
            patch.localPosition = new Vector3(-0.372f, 0.036f, -0.086f);
            patch.localEulerAngles = new Vector3(0f, 8f, 0f);
            Block(patch, "PatchCloth", Vector3.zero, new Vector3(0.096f, 0.010f, 0.086f), printMat);
            for (int i = 0; i < 12; i++)
            {
                // Stitches all the way round the four sides of the square.
                float t = i / 12f;
                Vector3 pos = t < 0.25f ? new Vector3(0.046f, 0.006f, -0.042f + t * 0.336f)
                            : t < 0.50f ? new Vector3(0.046f - (t - 0.25f) * 0.376f, 0.006f, 0.042f)
                            : t < 0.75f ? new Vector3(-0.048f, 0.006f, 0.042f - (t - 0.50f) * 0.336f)
                                        : new Vector3(-0.048f + (t - 0.75f) * 0.376f, 0.006f, -0.042f);
                Block(patch, "PatchStitch" + i, pos, new Vector3(0.012f, 0.005f, 0.005f), threadMat);
            }
            Block(patch, "PrintStripeA", new Vector3(0.018f, 0.005f, 0f), new Vector3(0.008f, 0.006f, 0.070f), fabricHiMat);
            Block(patch, "PrintStripeB", new Vector3(-0.020f, 0.005f, 0f), new Vector3(0.008f, 0.006f, 0.070f), fabricHiMat);

            // ── Turned and stitched hem: finished properly, and pointedly not frayed ──────────────────────
            Block(root, "HemFold", new Vector3(-0.548f, 0.004f, 0f), new Vector3(0.022f, 0.048f, 0.300f), fabricHiMat);
            Block(root, "HemStitch", new Vector3(-0.528f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.288f), threadMat);
            Sphere(root, "HemShade", new Vector3(-0.512f, 0.022f, 0f), Vector3.zero, new Vector3(0.04f, 0.014f, 0.290f), fabricDeepMat);

            // A knotted tape hanging loop at the back neck in place of a maker's label, as on the burlap.
            var loop = new PartNode("HangLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(0.160f, 0.040f, 0.066f);
            loop.localEulerAngles = new Vector3(0f, 22f, 0f);
            Block(loop, "LoopA", new Vector3(0f, 0f, -0.011f), new Vector3(0.030f, 0.005f, 0.006f), drawstringMat);
            Block(loop, "LoopB", new Vector3(0f, 0f, 0.011f), new Vector3(0.030f, 0.005f, 0.006f), drawstringMat);
            Block(loop, "LoopEnd", new Vector3(0.016f, 0f, 0f), new Vector3(0.006f, 0.005f, 0.026f), drawstringMat);

            return root;
        }

        /// <summary>
        /// A long dress someone cut out of feed sacks and sewed by hand, laid flat face-up with shoulders at
        /// +X and hem at −X. It borrows the full-length column of the tailored long dress and then makes a
        /// point of being nothing like it: stiff hessian that hangs in a few hard creases instead of draping,
        /// a coarse open weave read as a grid of slubs across the cloth, panels of slightly mismatched sack
        /// joined by crooked hand-stitching in pale twine, a neckline that is just a slit laced shut with
        /// cord, two patch pockets sewn on at different heights, and a raw unfinished hem shedding loose
        /// threads. A printed stencil left over from the sack it was made of still shows across the chest.
        /// </summary>
        private static PartNode BuildDressLongCraftedBurlap(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Burlap is a fixed, unmistakable jute brown; the catalogue tint only shifts it slightly so
            // sacking never comes out looking like dyed dress cloth.
            Color jute = new Color(0.545f, 0.435f, 0.255f);
            Color fabric = Color.Lerp(jute, def.Tint, 0.14f);                                     // coarse hessian
            Color fabricDeep = new Color(fabric.r * 0.66f, fabric.g * 0.64f, fabric.b * 0.60f);   // shaded creases, weave gaps
            Color fabricAlt = Color.Lerp(fabric, new Color(0.62f, 0.52f, 0.34f), 0.55f);          // a second sack, not quite matching
            Color slub = Color.Lerp(fabric, new Color(0.74f, 0.64f, 0.44f), 0.50f);               // lit threads of the open weave
            Color twine = new Color(0.80f, 0.74f, 0.56f);                                         // pale hand-stitching and lacing cord
            Color fray = Color.Lerp(fabric, new Color(0.70f, 0.62f, 0.44f), 0.35f);               // loose threads at the raw hem
            Color lining = new Color(fabric.r * 0.38f, fabric.g * 0.38f, fabric.b * 0.38f);       // dark inside, seen through the slit
            Color stencil = new Color(0.30f, 0.28f, 0.24f);                                       // faded print off the original sack

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.03f);   // sacking: flatter and duller even than the black robe
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.02f);
            PartMat fabricAltMat = PartMat.Of(fabricAlt, smoothness: 0.03f);
            PartMat slubMat = PartMat.Of(slub, smoothness: 0.06f);
            PartMat twineMat = PartMat.Of(twine, smoothness: 0.05f);
            PartMat frayMat = PartMat.Of(fray, smoothness: 0.04f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.03f);
            PartMat stencilMat = PartMat.Of(stencil, smoothness: 0.04f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // slit facings, the seams and the lacing, so each run is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Body: a long stiff column, boxy through the middle because sacking will not be shaped ─────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.054f, 0.215f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.065f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.056f, 0.230f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.055f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.054f, 0.205f), fabricMat);
            Sphere(root, "SkirtTop", new Vector3(-0.190f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.054f, 0.235f), fabricAltMat);
            Sphere(root, "SkirtMid", new Vector3(-0.330f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.258f), fabricAltMat);
            Sphere(root, "SkirtLow", new Vector3(-0.470f, -0.002f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.272f), fabricAltMat);

            // ── Open weave: a coarse grid of slubs and gaps, the single thing that says burlap on sight ───
            for (int gx = 0; gx < 13; gx++)
            {
                float x = Mathf.Lerp(0.150f, -0.500f, gx / 12f);
                float halfZ = Mathf.Lerp(0.090f, 0.118f, gx / 12f);
                Block(root, "Warp" + gx, new Vector3(x, 0.031f, 0f), new Vector3(0.009f, 0.006f, halfZ * 2f), slubMat);
                Block(root, "WarpGap" + gx, new Vector3(x + 0.026f, 0.028f, 0f), new Vector3(0.006f, 0.005f, halfZ * 1.9f), fabricDeepMat);
            }
            for (int gz = 0; gz < 7; gz++)
            {
                float z = (gz / 6f - 0.5f) * 0.205f;
                Block(root, "Weft" + gz, new Vector3(-0.175f, 0.033f, z), new Vector3(0.640f, 0.006f, 0.009f), slubMat);
                Block(root, "WeftGap" + gz, new Vector3(-0.175f, 0.029f, z + 0.017f), new Vector3(0.620f, 0.005f, 0.006f), fabricDeepMat);
            }

            // ── Panel joins: the sack pieces are different sizes, so the seams sit crooked and off-square ──
            var joinWaist = Block(root, "PanelJoinWaist", new Vector3(-0.120f, 0.036f, 0.006f), new Vector3(0.010f, 0.008f, 0.220f), twineMat);
            joinWaist.localEulerAngles = new Vector3(0f, 4.5f, 0f);   // sewn on a slant, as a first attempt would be
            var joinLow = Block(root, "PanelJoinLow", new Vector3(-0.395f, 0.036f, -0.004f), new Vector3(0.009f, 0.008f, 0.250f), twineMat);
            joinLow.localEulerAngles = new Vector3(0f, -3.5f, 0f);

            // Big uneven running stitches over the joins and down the sides, each one a slightly different
            // length and angle — the giveaway that this was sewn by hand rather than machined.
            for (int i = 0; i < 16; i++)
            {
                float z = (i / 15f - 0.5f) * 0.215f;
                var st = Block(root, "HandStitchWaist" + i, new Vector3(-0.120f + Mathf.Sin(i * 1.7f) * 0.006f, 0.040f, z),
                    new Vector3(0.020f + (i % 3) * 0.005f, 0.005f, 0.007f), twineMat);
                st.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 2.1f) * 22f, 0f);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 14; i++)
                {
                    float x = Mathf.Lerp(0.120f, -0.480f, i / 13f);
                    var st = Block(root, (s > 0 ? "SideStitchL" : "SideStitchR") + i,
                        new Vector3(x, 0.038f, s * (0.104f + i * 0.0018f)),
                        new Vector3(0.007f, 0.005f, 0.019f + (i % 4) * 0.004f), twineMat);
                    st.localEulerAngles = new Vector3(0f, Mathf.Cos(i * 1.9f) * 18f, 0f);
                }
            }

            // ── Neckline: no shaping, just a cut slit laced shut with cord through punched eyelets ────────
            Band("SlitOpen", new Vector3(0.196f, 0f, 0f), new Vector3(0.070f, 0f, 0.004f), 0.038f, 0.018f, 0.032f, liningMat);
            Band("SlitEdgeL", new Vector3(0.198f, 0f, 0.030f), new Vector3(0.072f, 0f, 0.030f), 0.016f, 0.032f, 0.026f, fabricDeepMat);
            Band("SlitEdgeR", new Vector3(0.198f, 0f, -0.026f), new Vector3(0.072f, 0f, -0.024f), 0.016f, 0.032f, 0.026f, fabricDeepMat);
            const int eyelets = 5;
            for (int i = 0; i < eyelets; i++)
            {
                float x = Mathf.Lerp(0.180f, 0.086f, i / (eyelets - 1f));
                for (int s = -1; s <= 1; s += 2)
                    Sphere(root, (s > 0 ? "EyeletL" : "EyeletR") + i, new Vector3(x, 0.036f, s * 0.030f), Vector3.zero,
                        new Vector3(0.013f, 0.010f, 0.013f), fabricDeepMat);
                // Cord crossing from one side to the other, alternating which way it leans.
                float x2 = Mathf.Lerp(0.180f, 0.086f, Mathf.Min(1f, (i + 1f) / (eyelets - 1f)));
                int dir = (i % 2 == 0) ? 1 : -1;
                Band("Lace" + i, new Vector3(x, 0f, dir * 0.030f), new Vector3(x2, 0f, -dir * 0.030f),
                    0.008f, 0.042f, 0.007f, twineMat);
            }
            // The loose ends of the lacing left hanging past the top of the slit.
            Band("LaceEndA", new Vector3(0.186f, 0f, 0.032f), new Vector3(0.236f, 0f, 0.070f), 0.008f, 0.042f, 0.007f, twineMat);
            Band("LaceEndB", new Vector3(0.186f, 0f, -0.030f), new Vector3(0.226f, 0f, -0.062f), 0.008f, 0.042f, 0.007f, twineMat);
            Sphere(root, "LaceKnot", new Vector3(0.186f, 0.046f, 0.002f), Vector3.zero, new Vector3(0.020f, 0.014f, 0.018f), twineMat);

            // ── Sleeves: plain rectangles of sack tacked into the armholes, cut off short and left raw ────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var sleeve = new PartNode("Sleeve" + side);
                sleeve.SetParent(root, false);
                sleeve.localPosition = new Vector3(0.108f, 0f, s * 0.140f);
                sleeve.localEulerAngles = new Vector3(0f, s * -18f, 0f);

                Block(sleeve, "Panel", new Vector3(0f, 0f, s * 0.055f), new Vector3(0.150f, 0.050f, 0.130f), fabricMat);
                Block(sleeve, "CuffRaw", new Vector3(0f, 0.001f, s * 0.122f), new Vector3(0.150f, 0.048f, 0.014f), frayMat);
                for (int i = 0; i < 6; i++)
                {
                    float x = (i / 5f - 0.5f) * 0.130f;
                    Block(sleeve, "CuffThread" + i, new Vector3(x, 0.026f, s * 0.140f),
                        new Vector3(0.005f, 0.004f, 0.020f + (i % 3) * 0.006f), frayMat);
                    var tack = Block(sleeve, "ArmTack" + i, new Vector3(x, 0.030f, s * 0.004f),
                        new Vector3(0.006f, 0.005f, 0.018f), twineMat);
                    tack.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 2.3f) * 20f, 0f);
                }
            }

            // ── Two patch pockets, sewn on by eye and so not level with one another ───────────────────────
            for (int p = 0; p < 2; p++)
            {
                int s = p == 0 ? 1 : -1;
                var pocket = new PartNode(p == 0 ? "PocketL" : "PocketR");
                pocket.SetParent(root, false);
                pocket.localPosition = new Vector3(-0.215f - p * 0.048f, 0.034f, s * 0.072f);   // one sits lower than the other
                pocket.localEulerAngles = new Vector3(0f, s * (6f + p * 5f), 0f);
                Block(pocket, "Patch", Vector3.zero, new Vector3(0.090f, 0.010f, 0.078f), fabricMat);
                Block(pocket, "Mouth", new Vector3(0.046f, 0.004f, 0f), new Vector3(0.010f, 0.010f, 0.078f), fabricDeepMat);
                for (int i = 0; i < 7; i++)
                {
                    float t = i / 6f;
                    // Stitches running down one side, across the bottom and back up the other.
                    Vector3 pos = t < 0.34f ? new Vector3(0.040f - t * 0.230f, 0.006f, 0.041f)
                                : t < 0.67f ? new Vector3(-0.044f, 0.006f, 0.041f - (t - 0.34f) * 0.250f)
                                            : new Vector3(-0.040f + (t - 0.67f) * 0.230f, 0.006f, -0.041f);
                    Block(pocket, "PocketStitch" + i, pos, new Vector3(0.014f, 0.005f, 0.006f), twineMat);
                }
            }

            // ── Rope belt tied round the middle, because there is no shaping to hold the dress in ─────────
            Block(root, "Belt", new Vector3(-0.088f, 0.042f, 0f), new Vector3(0.018f, 0.016f, 0.210f), twineMat);
            for (int i = 0; i < 10; i++)
            {
                float z = (i / 9f - 0.5f) * 0.195f;
                var twist = Block(root, "BeltTwist" + i, new Vector3(-0.088f, 0.049f, z), new Vector3(0.014f, 0.006f, 0.010f), fabricDeepMat);
                twist.localEulerAngles = new Vector3(0f, 32f, 0f);
            }
            var beltKnot = new PartNode("BeltKnot");
            beltKnot.SetParent(root, false);
            beltKnot.localPosition = new Vector3(-0.088f, 0.048f, -0.098f);
            beltKnot.localEulerAngles = new Vector3(0f, 40f, 0f);
            Sphere(beltKnot, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.034f, 0.024f, 0.030f), twineMat);
            Block(beltKnot, "TailA", new Vector3(-0.056f, -0.004f, 0.016f), new Vector3(0.090f, 0.013f, 0.014f), twineMat);
            Block(beltKnot, "TailB", new Vector3(-0.046f, -0.005f, -0.020f), new Vector3(0.072f, 0.012f, 0.013f), twineMat);

            // ── Raw hem: never turned or finished, just the cut edge shedding a fringe of loose threads ───
            Block(root, "HemRaw", new Vector3(-0.548f, 0.004f, 0f), new Vector3(0.020f, 0.050f, 0.260f), frayMat);
            for (int i = 0; i < 14; i++)
            {
                float z = (i / 13f - 0.5f) * 0.250f;
                float len = 0.018f + Mathf.Abs(Mathf.Sin(i * 2.7f)) * 0.028f;   // every thread a different length
                var thread = Block(root, "FrayThread" + i, new Vector3(-0.560f - len * 0.5f, 0.026f, z),
                    new Vector3(len, 0.005f, 0.005f), frayMat);
                thread.localEulerAngles = new Vector3(0f, Mathf.Sin(i * 1.6f) * 26f, 0f);
            }

            // ── Faded stencil left over from the sack the dress was cut out of ────────────────────────────
            var mark = new PartNode("SackStencil");
            mark.SetParent(root, false);
            mark.localPosition = new Vector3(0.030f, 0.038f, -0.014f);
            mark.localEulerAngles = new Vector3(0f, -7f, 0f);
            Block(mark, "StencilBar", Vector3.zero, new Vector3(0.012f, 0.005f, 0.128f), stencilMat);
            Block(mark, "StencilRow", new Vector3(-0.038f, 0f, 0.010f), new Vector3(0.010f, 0.005f, 0.092f), stencilMat);
            for (int i = 0; i < 4; i++)
                Block(mark, "StencilGlyph" + i, new Vector3(0.038f, 0f, -0.048f + i * 0.032f),
                    new Vector3(0.026f, 0.005f, 0.018f), stencilMat);

            // Hessian is stiff: a few hard creases rather than the soft drape the tailored dresses get.
            Sphere(root, "CreaseA", new Vector3(-0.290f, 0.034f, -0.078f), new Vector3(0f, -9f, 0f), new Vector3(0.28f, 0.014f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.420f, 0.032f, 0.086f), new Vector3(0f, 12f, 0f), new Vector3(0.22f, 0.013f, 0.026f), fabricDeepMat);
            Sphere(root, "RidgeA", new Vector3(-0.240f, 0.038f, 0.038f), new Vector3(0f, 5f, 0f), new Vector3(0.30f, 0.012f, 0.026f), slubMat);
            Sphere(root, "RidgeB", new Vector3(0.010f, 0.038f, -0.070f), new Vector3(0f, -11f, 0f), new Vector3(0.16f, 0.012f, 0.024f), slubMat);

            // No care label on this one — nobody made it in a factory. A scrap of twine knotted through the
            // back neck stands in for one, the way a hand-made thing gets a hanging loop instead.
            var loop = new PartNode("HangLoop");
            loop.SetParent(root, false);
            loop.localPosition = new Vector3(0.166f, 0.040f, 0.062f);
            loop.localEulerAngles = new Vector3(0f, 24f, 0f);
            Block(loop, "LoopA", new Vector3(0f, 0f, -0.011f), new Vector3(0.030f, 0.005f, 0.005f), twineMat);
            Block(loop, "LoopB", new Vector3(0f, 0f, 0.011f), new Vector3(0.030f, 0.005f, 0.005f), twineMat);
            Block(loop, "LoopEnd", new Vector3(0.016f, 0f, 0f), new Vector3(0.005f, 0.005f, 0.026f), twineMat);

            return root;
        }

        /// <summary>
        /// A long black robe laid flat, face-up, shoulders at +X and hem at −X. Where the dresses are cut to
        /// a body, this is deliberately shapeless: straight sides falling from shoulder to hem with no waist
        /// seam at all, wide bell sleeves flung out either side, and a full-length front opening that gapes
        /// down the middle to show a duller inner facing. A pointed hood lies collapsed above the shoulders
        /// and a knotted rope cord is coiled across the waist with its tasselled ends trailing off. The cloth
        /// is matte and light-swallowing — the item's beige catalogue tint is discarded almost entirely so
        /// the robe is actually black, with only cold grey sheen on the raised folds to keep it readable.
        /// </summary>
        private static PartNode BuildBlackRobe(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The catalogue tint for this item is a beige, so it is allowed only the faintest say: a black
            // robe that isn't black reads as a mistake.
            Color ink = new Color(0.085f, 0.082f, 0.092f);
            Color cloth = Color.Lerp(ink, def.Tint, 0.07f);                                   // heavy matte wool
            Color clothDeep = new Color(cloth.r * 0.55f, cloth.g * 0.55f, cloth.b * 0.60f);   // shaded fold valleys
            Color clothHi = Color.Lerp(cloth, new Color(0.42f, 0.42f, 0.47f), 0.42f);         // cold grey sheen on raised folds
            Color facing = Color.Lerp(cloth, new Color(0.30f, 0.28f, 0.30f), 0.30f);          // inner facing down the opening
            Color lining = new Color(cloth.r * 0.40f, cloth.g * 0.40f, cloth.b * 0.46f);      // the dark inside the hood
            Color stitch = Color.Lerp(cloth, new Color(0.50f, 0.49f, 0.46f), 0.45f);          // pale topstitching
            Color rope = new Color(0.34f, 0.31f, 0.25f);                                      // hemp cord belt
            Color ropeHi = Color.Lerp(rope, new Color(0.58f, 0.54f, 0.45f), 0.45f);           // lit side of the cord
            Color label = new Color(0.62f, 0.61f, 0.57f);                                     // woven care label

            PartMat clothMat = PartMat.Of(cloth, smoothness: 0.06f);   // matte: the least reflective garment in the set
            PartMat clothDeepMat = PartMat.Of(clothDeep, smoothness: 0.04f);
            PartMat clothHiMat = PartMat.Of(clothHi, smoothness: 0.12f);
            PartMat facingMat = PartMat.Of(facing, smoothness: 0.09f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.05f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.10f);
            PartMat ropeMat = PartMat.Of(rope, smoothness: 0.08f);
            PartMat ropeHiMat = PartMat.Of(ropeHi, smoothness: 0.10f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.10f);

            // ── Body: straight-sided and unfitted, barely wider at the hem than at the shoulder ───────────
            Sphere(root, "Yoke", new Vector3(0.145f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.052f, 0.250f), clothMat);
            Sphere(root, "Chest", new Vector3(0.045f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.054f, 0.260f), clothMat);
            Sphere(root, "BodyMid", new Vector3(-0.100f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.268f), clothMat);
            Sphere(root, "BodyLow", new Vector3(-0.245f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.052f, 0.288f), clothMat);
            Sphere(root, "Skirt", new Vector3(-0.385f, -0.002f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.310f), clothMat);

            // Side seams running the whole length uninterrupted — the giveaway that there is no waist here.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "SideSeamL" : "SideSeamR",
                    new Vector3(-0.140f, 0.029f, s * 0.128f), new Vector3(0.520f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * -2.5f, 0f);
            }

            // ── Full-length front opening: a gaping dark channel edged with the inner facing ───────────────
            Block(root, "OpeningGap", new Vector3(-0.120f, 0.026f, 0f), new Vector3(0.560f, 0.018f, 0.030f), liningMat);
            for (int s = -1; s <= 1; s += 2)
            {
                var edge = Block(root, s > 0 ? "FacingL" : "FacingR",
                    new Vector3(-0.120f, 0.030f, s * 0.032f), new Vector3(0.560f, 0.012f, 0.028f), facingMat);
                edge.localEulerAngles = new Vector3(0f, s * 1.5f, 0f);
                Block(root, s > 0 ? "FacingStitchL" : "FacingStitchR",
                    new Vector3(-0.120f, 0.037f, s * 0.048f), new Vector3(0.540f, 0.005f, 0.006f), stitchMat);
            }

            // ── Bell sleeves: narrow at the armhole, flaring wide open at the cuff ────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var sleeve = new PartNode("Sleeve" + side);
                sleeve.SetParent(root, false);
                sleeve.localPosition = new Vector3(0.090f, 0f, s * 0.150f);
                sleeve.localEulerAngles = new Vector3(0f, s * -24f, 0f);   // flung out and a little down the body

                Sphere(sleeve, "Head", Vector3.zero, Vector3.zero, new Vector3(0.105f, 0.050f, 0.110f), clothMat);
                Sphere(sleeve, "Upper", new Vector3(0f, 0f, s * 0.090f), Vector3.zero, new Vector3(0.115f, 0.048f, 0.115f), clothMat);
                Sphere(sleeve, "Flare", new Vector3(-0.010f, 0f, s * 0.180f), Vector3.zero, new Vector3(0.165f, 0.046f, 0.130f), clothMat);
                Sphere(sleeve, "Bell", new Vector3(-0.018f, -0.001f, s * 0.255f), Vector3.zero, new Vector3(0.210f, 0.044f, 0.115f), clothMat);
                Block(sleeve, "CuffEdge", new Vector3(-0.020f, 0.002f, s * 0.300f), new Vector3(0.190f, 0.046f, 0.016f), facingMat);
                Sphere(sleeve, "CuffMouth", new Vector3(-0.020f, 0.020f, s * 0.288f), Vector3.zero, new Vector3(0.150f, 0.020f, 0.024f), liningMat);
                var armSeam = Block(sleeve, "ArmSeam", new Vector3(0.004f, 0.028f, s * 0.030f),
                    new Vector3(0.120f, 0.006f, 0.010f), stitchMat);
                armSeam.localEulerAngles = new Vector3(0f, s * 30f, 0f);

                // Drape gathering along the underside of the bell, deepening toward the cuff.
                for (int i = 0; i < 4; i++)
                {
                    float z = s * (0.120f + i * 0.055f);
                    Sphere(sleeve, "Drape" + i, new Vector3(-0.048f - i * 0.014f, 0.028f, z),
                        new Vector3(0f, s * (52f + i * 7f), 0f), new Vector3(0.090f, 0.012f, 0.030f), clothHiMat);
                    Sphere(sleeve, "DrapeShade" + i, new Vector3(0.030f + i * 0.010f, 0.024f, z),
                        new Vector3(0f, s * -(48f + i * 6f), 0f), new Vector3(0.075f, 0.010f, 0.024f), clothDeepMat);
                }
            }

            // ── Pointed hood, collapsed back over the shoulders and reaching past the top of the robe ─────
            var hood = new PartNode("Hood");
            hood.SetParent(root, false);
            hood.localPosition = new Vector3(0.215f, 0.006f, 0f);
            hood.localEulerAngles = new Vector3(0f, -11f, 0f);   // slumped a little to one side, as dropped cloth lies
            Sphere(hood, "Mouth", new Vector3(-0.020f, 0.010f, 0f), Vector3.zero, new Vector3(0.080f, 0.034f, 0.170f), liningMat);
            Sphere(hood, "Crown", new Vector3(0.070f, 0f, 0f), Vector3.zero, new Vector3(0.170f, 0.056f, 0.200f), clothMat);
            Sphere(hood, "Peak", new Vector3(0.168f, -0.002f, 0.014f), new Vector3(0f, 12f, 0f), new Vector3(0.130f, 0.044f, 0.120f), clothMat);
            Sphere(hood, "Point", new Vector3(0.236f, -0.004f, 0.034f), new Vector3(0f, 26f, 0f), new Vector3(0.080f, 0.030f, 0.060f), clothMat);
            Block(hood, "RidgeSeam", new Vector3(0.088f, 0.032f, 0f), new Vector3(0.230f, 0.007f, 0.008f), stitchMat);
            Sphere(hood, "CrownFold", new Vector3(0.060f, 0.032f, -0.052f), new Vector3(0f, -22f, 0f), new Vector3(0.150f, 0.014f, 0.040f), clothHiMat);
            Sphere(hood, "CrownShade", new Vector3(0.080f, 0.028f, 0.060f), new Vector3(0f, 18f, 0f), new Vector3(0.130f, 0.012f, 0.034f), clothDeepMat);
            // Bound edge running around the opening of the hood.
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f - 0.5f) * 0.185f;
                Block(hood, "HoodBinding" + i, new Vector3(-0.052f + Mathf.Abs(z) * 0.24f, 0.024f, z),
                    new Vector3(0.020f, 0.024f, 0.026f), facingMat);
            }

            // ── Knotted rope cord coiled across the waist, its tasselled ends trailing off to one side ────
            var cord = new PartNode("Cord");
            cord.SetParent(root, false);
            cord.localPosition = new Vector3(-0.070f, 0.038f, 0f);
            Block(cord, "BeltNear", new Vector3(0f, 0f, 0f), new Vector3(0.020f, 0.019f, 0.235f), ropeMat);
            Block(cord, "BeltHilite", new Vector3(0f, 0.008f, 0f), new Vector3(0.009f, 0.008f, 0.225f), ropeHiMat);
            var beltFar = Block(cord, "BeltFar", new Vector3(-0.034f, -0.002f, 0.010f), new Vector3(0.017f, 0.016f, 0.215f), ropeMat);
            beltFar.localEulerAngles = new Vector3(0f, 4f, 0f);
            // The twist of the hemp, read as a run of short slanted segments along the cord.
            for (int i = 0; i < 12; i++)
            {
                float z = (i / 11f - 0.5f) * 0.215f;
                var twist = Block(cord, "Twist" + i, new Vector3(0.002f, 0.010f, z), new Vector3(0.016f, 0.007f, 0.011f), ropeHiMat);
                twist.localEulerAngles = new Vector3(0f, 34f, 0f);
            }

            var knot = new PartNode("Knot");
            knot.SetParent(cord, false);
            knot.localPosition = new Vector3(-0.008f, 0.006f, 0.112f);
            knot.localEulerAngles = new Vector3(0f, -38f, 0f);
            Sphere(knot, "KnotBall", Vector3.zero, Vector3.zero, new Vector3(0.040f, 0.030f, 0.036f), ropeMat);
            Sphere(knot, "KnotHilite", new Vector3(0f, 0.012f, 0f), Vector3.zero, new Vector3(0.026f, 0.014f, 0.022f), ropeHiMat);
            Block(knot, "EndNear", new Vector3(-0.070f, -0.004f, 0.020f), new Vector3(0.115f, 0.015f, 0.016f), ropeMat);
            Block(knot, "EndFar", new Vector3(-0.058f, -0.005f, -0.026f), new Vector3(0.095f, 0.014f, 0.015f), ropeMat);
            // Frayed tassels on both loose ends.
            for (int e = 0; e < 2; e++)
            {
                Vector3 tip = e == 0 ? new Vector3(-0.132f, -0.005f, 0.028f) : new Vector3(-0.110f, -0.006f, -0.034f);
                Sphere(knot, "TasselHead" + e, tip, Vector3.zero, new Vector3(0.026f, 0.020f, 0.022f), ropeMat);
                for (int i = 0; i < 5; i++)
                    Block(knot, "Strand" + e + "_" + i,
                        new Vector3(tip.x - 0.022f, tip.y, tip.z - 0.012f + i * 0.006f),
                        new Vector3(0.030f, 0.006f, 0.004f), ropeHiMat);
            }

            // ── Heavy weighted hem across the bottom, with its blind stitch just above it ─────────────────
            Block(root, "Hem", new Vector3(-0.452f, 0.004f, 0f), new Vector3(0.020f, 0.052f, 0.290f), facingMat);
            Block(root, "HemStitch", new Vector3(-0.434f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.275f), stitchMat);

            // Long vertical folds hanging the length of the robe, the way heavy wool falls in columns.
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f * 2f - 1f;
                float z = t * 0.108f;
                Sphere(root, "Fold" + i, new Vector3(-0.230f + Mathf.Abs(t) * 0.030f, 0.032f, z),
                    new Vector3(0f, t * 6f, 0f), new Vector3(0.430f, 0.014f, 0.036f), clothHiMat);
                Sphere(root, "FoldShade" + i, new Vector3(-0.240f + Mathf.Abs(t) * 0.030f, 0.027f, z + 0.030f),
                    new Vector3(0f, t * 6f, 0f), new Vector3(0.400f, 0.011f, 0.022f), clothDeepMat);
            }
            Sphere(root, "CreaseA", new Vector3(-0.360f, 0.026f, -0.150f), new Vector3(0f, 20f, 0f), new Vector3(0.17f, 0.012f, 0.034f), clothDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.020f, 0.028f, 0.135f), new Vector3(0f, -15f, 0f), new Vector3(0.14f, 0.012f, 0.030f), clothDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.128f, 0.032f, 0.058f);
            labelFrame.localEulerAngles = new Vector3(0f, 20f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A full ivory wedding gown laid flat, face-up, shoulders at +X and the hem sweeping off to −X.
        /// It is the largest garment in the set and is built to read as such: a boned, lace-overlaid bodice
        /// with a sweetheart neckline and a row of covered pearl buttons down the back closure, cinched at a
        /// beaded waist belt, below which a ballgown skirt spreads through stacked tulle tiers into a wide
        /// hem — and then keeps going, a cathedral train trailing well past it and fanning out into a pool of
        /// cloth. A short tulle veil lies folded over the shoulder end, its comb tucked in beside the neck.
        /// The item tint only faintly warms the fabric so the gown always stays ivory rather than taking on
        /// the drab clothing colour the other dresses share.
        /// </summary>
        private static PartNode BuildWeddingDress(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // A wedding dress is ivory by definition, so the tint is only allowed to tinge it.
            Color ivory = new Color(0.955f, 0.940f, 0.895f);
            Color fabric = Color.Lerp(ivory, def.Tint, 0.12f);                                    // duchess satin
            Color fabricDeep = new Color(fabric.r * 0.80f, fabric.g * 0.79f, fabric.b * 0.76f);   // shaded folds / pleat valleys
            Color fabricHi = Color.Lerp(fabric, Color.white, 0.35f);                              // satin catching the light
            Color tulle = Color.Lerp(fabric, Color.white, 0.20f);                                 // sheer net over-layers
            Color lace = Color.Lerp(fabric, new Color(0.88f, 0.86f, 0.80f), 0.45f);               // corded lace appliqué
            Color stitch = Color.Lerp(fabric, new Color(0.86f, 0.84f, 0.77f), 0.55f);             // topstitching and boning channels
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.43f, fabric.b * 0.42f);       // dark inside, seen through the neck
            Color pearl = new Color(0.97f, 0.96f, 0.93f);                                         // buttons, beading, veil comb
            Color label = new Color(0.90f, 0.89f, 0.84f);                                         // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.42f);      // satin, the glossiest cloth in the set
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.30f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.48f);
            PartMat tulleMat = PartMat.Of(tulle, smoothness: 0.22f);
            PartMat laceMat = PartMat.Of(lace, smoothness: 0.18f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.12f);
            PartMat pearlMat = PartMat.Of(pearl, smoothness: 0.70f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // ── Boned bodice: shoulders at +X, drawn hard in to a corseted waist ───────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.050f, 0.215f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.062f, 0f, 0f), Vector3.zero, new Vector3(0.21f, 0.052f, 0.230f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.058f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.048f, 0.165f), fabricMat);

            // Boning channels running the length of the bodice, a pair either side of the centre front.
            for (int s = -1; s <= 1; s += 2)
            {
                for (int b = 0; b < 2; b++)
                {
                    float z = s * (0.030f + b * 0.042f);
                    var bone = Block(root, (s > 0 ? "BoneL" : "BoneR") + b,
                        new Vector3(0.055f, 0.029f, z), new Vector3(0.185f, 0.006f, 0.006f), stitchMat);
                    bone.localEulerAngles = new Vector3(0f, s * (5f + b * 5f), 0f);
                }
            }

            // Corded lace appliqué scattered over the bodice, each motif set at its own angle.
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                float x = Mathf.Lerp(0.135f, -0.030f, t);
                float z = Mathf.Sin(i * 2.3f) * 0.078f;
                Sphere(root, "LaceMotif" + i, new Vector3(x, 0.030f, z), new Vector3(0f, i * 37f, 0f),
                    new Vector3(0.042f, 0.010f, 0.026f), laceMat);
            }

            // ── Sweetheart neckline: two soft scallops over a dark hollow, edged in lace ───────────────────
            Sphere(root, "Neckline", new Vector3(0.158f, 0.012f, 0f), Vector3.zero, new Vector3(0.078f, 0.034f, 0.120f), liningMat);
            for (int s = -1; s <= 1; s += 2)
            {
                Sphere(root, s > 0 ? "SweetheartL" : "SweetheartR", new Vector3(0.176f, 0.020f, s * 0.036f),
                    Vector3.zero, new Vector3(0.046f, 0.026f, 0.062f), fabricHiMat);
                Block(root, s > 0 ? "NeckLaceL" : "NeckLaceR", new Vector3(0.186f, 0.026f, s * 0.040f),
                    new Vector3(0.014f, 0.010f, 0.070f), laceMat);
            }
            Sphere(root, "SweetheartDip", new Vector3(0.168f, 0.022f, 0f), Vector3.zero,
                new Vector3(0.026f, 0.018f, 0.030f), fabricDeepMat);

            // ── Covered pearl buttons marching down the back closure from the neck to the waist ───────────
            const int buttons = 12;
            for (int i = 0; i < buttons; i++)
            {
                float x = Mathf.Lerp(0.148f, -0.052f, i / (buttons - 1f));
                Sphere(root, "Button" + i, new Vector3(x, 0.034f, -0.006f), Vector3.zero,
                    new Vector3(0.011f, 0.010f, 0.011f), pearlMat);
            }
            Block(root, "ButtonPlacket", new Vector3(0.048f, 0.030f, -0.006f), new Vector3(0.205f, 0.008f, 0.020f), fabricHiMat);

            // ── Beaded waist belt where the skirt is set on to the bodice ──────────────────────────────────
            Block(root, "WaistBelt", new Vector3(-0.082f, 0.030f, 0f), new Vector3(0.026f, 0.012f, 0.180f), fabricHiMat);
            for (int i = 0; i < 11; i++)
            {
                float z = (i / 10f - 0.5f) * 0.160f;
                Sphere(root, "Bead" + i, new Vector3(-0.082f, 0.038f, z), Vector3.zero,
                    new Vector3(0.010f, 0.009f, 0.010f), pearlMat);
            }
            Sphere(root, "WaistShade", new Vector3(-0.100f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.170f), fabricDeepMat);

            // ── Ballgown skirt: mounds widening fast out of the waist into a very broad hem ────────────────
            Sphere(root, "SkirtTop", new Vector3(-0.150f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.056f, 0.280f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.285f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.058f, 0.400f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.420f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.054f, 0.480f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.530f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.050f, 0.470f), fabricMat);

            // Gathered pleats fanning from the waist to the hem: raised satin ridges with shaded valleys.
            const int pleats = 11;
            for (int i = 0; i < pleats; i++)
            {
                float t = i / (pleats - 1f) * 2f - 1f;                     // −1 at one side of the skirt, +1 at the other
                Vector3 from = new Vector3(-0.115f, 0f, t * 0.072f);       // where it leaves the waist belt
                Vector3 to = new Vector3(-0.560f, 0f, t * 0.225f);         // where it reaches the hem
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;          // aim the part's length from waist to hem
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var ridge = Block(root, "Pleat" + i, new Vector3(mid.x, 0.032f, mid.z),
                    new Vector3(len, 0.011f, 0.028f), fabricHiMat);
                ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                var valley = Block(root, "PleatShade" + i, new Vector3(mid.x, 0.027f, mid.z + 0.032f),
                    new Vector3(len * 0.92f, 0.009f, 0.013f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Stacked tulle tiers: sheer scalloped layers laid over the skirt, each wider than the last ──
            for (int tier = 0; tier < 3; tier++)
            {
                float x = -0.235f - tier * 0.130f;
                float halfZ = 0.190f + tier * 0.055f;
                Sphere(root, "TulleTier" + tier, new Vector3(x, 0.034f, 0f), Vector3.zero,
                    new Vector3(0.11f, 0.014f, halfZ * 2f), tulleMat);
                for (int i = 0; i < 7; i++)
                {
                    float z = (i / 6f - 0.5f) * halfZ * 1.9f;
                    Sphere(root, "Scallop" + tier + "_" + i, new Vector3(x - 0.052f, 0.036f, z), Vector3.zero,
                        new Vector3(0.036f, 0.012f, 0.052f), tulleMat);
                }
            }

            // ── Faced hem across the wide end, with its blind stitch and lace edging just above it ────────
            Block(root, "Hem", new Vector3(-0.598f, 0.004f, 0f), new Vector3(0.018f, 0.052f, 0.430f), fabricHiMat);
            Block(root, "HemStitch", new Vector3(-0.582f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.410f), stitchMat);
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f - 0.5f) * 0.400f;
                Sphere(root, "HemLace" + i, new Vector3(-0.606f, 0.024f, z), Vector3.zero,
                    new Vector3(0.030f, 0.012f, 0.044f), laceMat);
            }

            // ── Cathedral train: the skirt keeps running past the hem and pools out behind it ─────────────
            var train = new PartNode("Train");
            train.SetParent(root, false);
            train.localPosition = new Vector3(-0.660f, 0.004f, 0f);
            train.localEulerAngles = new Vector3(0f, 9f, 0f);   // swung a little to one side, as dropped cloth falls
            Sphere(train, "TrainNeck", new Vector3(0.020f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.044f, 0.340f), fabricMat);
            Sphere(train, "TrainBody", new Vector3(-0.130f, 0f, 0.020f), Vector3.zero, new Vector3(0.22f, 0.042f, 0.400f), fabricMat);
            Sphere(train, "TrainPool", new Vector3(-0.290f, -0.002f, 0.048f), new Vector3(0f, 14f, 0f), new Vector3(0.24f, 0.038f, 0.430f), fabricMat);
            Sphere(train, "TrainTip", new Vector3(-0.430f, -0.003f, 0.086f), new Vector3(0f, 24f, 0f), new Vector3(0.18f, 0.032f, 0.300f), fabricMat);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.060f - i * 0.095f;
                Sphere(train, "TrainFold" + i, new Vector3(x, 0.026f, i * 0.026f - 0.040f),
                    new Vector3(0f, 20f + i * 11f, 0f), new Vector3(0.22f, 0.014f, 0.048f), fabricHiMat);
                Sphere(train, "TrainCrease" + i, new Vector3(x - 0.030f, 0.020f, i * 0.030f + 0.060f),
                    new Vector3(0f, -16f - i * 9f, 0f), new Vector3(0.17f, 0.011f, 0.036f), fabricDeepMat);
            }
            Block(train, "TrainHem", new Vector3(-0.505f, 0.002f, 0.100f), new Vector3(0.016f, 0.036f, 0.250f), fabricHiMat);

            // ── Short tulle veil folded over the shoulder end, its comb tucked in beside the neck ─────────
            var veil = new PartNode("Veil");
            veil.SetParent(root, false);
            veil.localPosition = new Vector3(0.235f, 0.030f, 0.055f);
            veil.localEulerAngles = new Vector3(0f, -26f, 0f);
            Sphere(veil, "VeilFoldTop", Vector3.zero, Vector3.zero, new Vector3(0.150f, 0.020f, 0.130f), tulleMat);
            Sphere(veil, "VeilFoldLow", new Vector3(-0.030f, -0.010f, 0.055f), new Vector3(0f, 18f, 0f), new Vector3(0.130f, 0.016f, 0.110f), tulleMat);
            Sphere(veil, "VeilTail", new Vector3(0.090f, -0.004f, 0.070f), new Vector3(0f, 34f, 0f), new Vector3(0.110f, 0.012f, 0.080f), tulleMat);
            for (int i = 0; i < 6; i++)
            {
                float z = (i / 5f - 0.5f) * 0.120f;
                Sphere(veil, "VeilEdge" + i, new Vector3(-0.078f, 0.006f, z), Vector3.zero,
                    new Vector3(0.026f, 0.010f, 0.034f), laceMat);
            }
            Block(veil, "Comb", new Vector3(0.062f, 0.012f, -0.030f), new Vector3(0.014f, 0.006f, 0.060f), pearlMat);
            for (int i = 0; i < 5; i++)
                Block(veil, "CombTooth" + i, new Vector3(0.050f, 0.012f, -0.052f + i * 0.011f),
                    new Vector3(0.012f, 0.004f, 0.003f), pearlMat);

            // A few soft folds and shaded creases so the satin looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.330f, 0.034f, -0.130f), new Vector3(0f, -20f, 0f), new Vector3(0.24f, 0.016f, 0.056f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.030f, 0.034f, 0.062f), new Vector3(0f, 15f, 0f), new Vector3(0.15f, 0.015f, 0.044f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.470f, 0.026f, 0.150f), new Vector3(0f, 24f, 0f), new Vector3(0.19f, 0.012f, 0.036f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.190f, 0.028f, 0.115f), new Vector3(0f, -13f, 0f), new Vector3(0.14f, 0.012f, 0.032f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.126f, 0.034f, -0.046f);
            labelFrame.localEulerAngles = new Vector3(0f, -16f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A knee-length dress laid flat, face-up: a fitted bodice with cap sleeves and a bound scoop
        /// neckline, nipped in at a stitched waist seam, below which a gathered A-line skirt fans out to a
        /// wide hem — the flare and the pleats radiating down it are what set the dress apart from the flat,
        /// straight-sided tops. A self-fabric sash crosses the waist and its knot and ties trail off to one
        /// side so the slow spin always has something to catch. Takes the item's tint as the dress fabric.
        /// </summary>
        private static PartNode BuildDressKnees(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                            // light dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / pleat valleys
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised pleats catching light
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.91f, 0.88f), 0.14f);              // neck binding, sleeve and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen through the neck
            Color sash = new Color(fabric.r * 0.62f, fabric.g * 0.62f, fabric.b * 0.62f);        // self-fabric belt, a shade deeper
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.16f);   // finer weave than jersey, faint sheen
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.14f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.18f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.15f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat sashMat = PartMat.Of(sash, smoothness: 0.20f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // ── Bodice: shoulders at +X, drawn in to a narrow waist ────────────────────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.140f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.048f, 0.235f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.055f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.245f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.060f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.190f), fabricMat);

            // Princess seams curving down the front of the bodice from each shoulder to the waist.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "PrincessSeamL" : "PrincessSeamR",
                    new Vector3(0.060f, 0.027f, s * 0.070f), new Vector3(0.180f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 9f, 0f);
            }

            // ── Gathered A-line skirt: each mound wider than the last, so the hem is the broadest part ─────
            Sphere(root, "SkirtTop", new Vector3(-0.145f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.050f, 0.255f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.255f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.330f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.355f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.046f, 0.390f), fabricMat);

            // Knife pleats fanning out from the waist to the hem: raised ridges with shaded valleys between
            // them, each laid along the line it follows so the skirt reads as gathered cloth rather than a slab.
            const int pleats = 7;
            for (int i = 0; i < pleats; i++)
            {
                float t = i / (pleats - 1f) * 2f - 1f;                     // −1 at one side of the skirt, +1 at the other
                Vector3 from = new Vector3(-0.115f, 0f, t * 0.070f);       // where it leaves the waist seam
                Vector3 to = new Vector3(-0.395f, 0f, t * 0.185f);         // where it reaches the hem
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;          // aim the part's length from waist to hem
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var ridge = Block(root, "Pleat" + i, new Vector3(mid.x, 0.028f, mid.z),
                    new Vector3(len, 0.010f, 0.026f), fabricHiMat);
                ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                var valley = Block(root, "PleatShade" + i, new Vector3(mid.x, 0.024f, mid.z + 0.030f),
                    new Vector3(len * 0.92f, 0.008f, 0.012f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Waist seam where the gathered skirt is set on to the bodice, with the join shaded just below it.
            Block(root, "WaistSeam", new Vector3(-0.098f, 0.028f, 0f), new Vector3(0.007f, 0.006f, 0.200f), stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.112f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.190f), fabricDeepMat);

            // ── Cap sleeves, each a soft short cap off the shoulder with a faced edge and an armhole seam ──
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "Sleeve" + side, new Vector3(0.110f, 0f, s * 0.160f), Vector3.zero,
                    new Vector3(0.115f, 0.048f, 0.115f), fabricMat);
                Block(root, "SleeveFacing" + side, new Vector3(0.098f, 0.001f, s * 0.212f),
                    new Vector3(0.100f, 0.050f, 0.016f), trimMat);
                var armSeam = Block(root, "ArmSeam" + side, new Vector3(0.108f, 0.028f, s * 0.104f),
                    new Vector3(0.115f, 0.006f, 0.010f), stitchMat);
                armSeam.localEulerAngles = new Vector3(0f, s * 28f, 0f);
            }

            // ── Scoop neckline: a dark hollow ringed by a narrow bound edge ────────────────────────────────
            Sphere(root, "Neckline", new Vector3(0.152f, 0.010f, 0f), Vector3.zero, new Vector3(0.090f, 0.034f, 0.130f), liningMat);
            const int bindings = 16;
            const float nCx = 0.152f, nrx = 0.050f, nrz = 0.076f;
            for (int i = 0; i < bindings; i++)
            {
                float deg = i * 360f / bindings;
                float rad = deg * Mathf.Deg2Rad;
                var bind = Block(root, "Binding" + i,
                    new Vector3(nCx + nrx * Mathf.Cos(rad), 0.022f, nrz * Mathf.Sin(rad)),
                    new Vector3(0.018f, 0.022f, 0.010f), trimMat);
                bind.localEulerAngles = new Vector3(0f, -deg, 0f);   // lay each piece of the binding across the edge
            }

            // ── Faced hem across the wide end, with its blind stitch just above it ─────────────────────────
            Block(root, "Hem", new Vector3(-0.418f, 0.004f, 0f), new Vector3(0.016f, 0.048f, 0.360f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.404f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.340f), stitchMat);

            // ── Self-fabric sash across the waist, knotted off to one side with its ties trailing ─────────
            Block(root, "Sash", new Vector3(-0.085f, 0.032f, 0f), new Vector3(0.032f, 0.010f, 0.230f), sashMat);
            Block(root, "SashEdgeTop", new Vector3(-0.070f, 0.036f, 0f), new Vector3(0.005f, 0.006f, 0.220f), fabricDeepMat);
            Block(root, "SashEdgeLow", new Vector3(-0.100f, 0.036f, 0f), new Vector3(0.005f, 0.006f, 0.220f), fabricDeepMat);

            var sashKnot = new PartNode("SashKnot");
            sashKnot.SetParent(root, false);
            sashKnot.localPosition = new Vector3(-0.085f, 0.038f, 0.105f);
            sashKnot.localEulerAngles = new Vector3(0f, -34f, 0f);
            Sphere(sashKnot, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.048f, 0.020f, 0.040f), sashMat);
            Block(sashKnot, "TieNear", new Vector3(-0.048f, -0.002f, 0.018f), new Vector3(0.080f, 0.008f, 0.026f), sashMat);
            Block(sashKnot, "TieFar", new Vector3(-0.040f, -0.002f, -0.024f), new Vector3(0.066f, 0.008f, 0.024f), sashMat);
            Block(sashKnot, "TieTip", new Vector3(-0.092f, -0.003f, 0.030f), new Vector3(0.030f, 0.007f, 0.028f), fabricDeepMat);

            // A few soft folds and shaded creases so the cloth looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.210f, 0.030f, -0.090f), new Vector3(0f, -18f, 0f), new Vector3(0.19f, 0.016f, 0.05f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.020f, 0.030f, 0.055f), new Vector3(0f, 16f, 0f), new Vector3(0.15f, 0.016f, 0.045f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.310f, 0.024f, 0.110f), new Vector3(0f, 22f, 0f), new Vector3(0.15f, 0.012f, 0.032f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.075f, 0.026f, -0.060f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.118f, 0.030f, 0.040f);
            labelFrame.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A knee-length strappy sundress laid flat face-up, shoulders at +X and hem at −X. It shares the
        /// pleated A-line skirt of the plain knee dress, but the sleeved, scoop-necked bodice is replaced by
        /// the strap dress's own cut: a straight shirred top edge gathered on rows of elastic, a side zip,
        /// and two narrow adjustable shoulder straps — one reaching out past the shoulder, the other fallen
        /// and trailing across the cloth so the slow spin has something to read against.
        /// </summary>
        private static PartNode BuildDressKneesStraps(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // light dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / pleat valleys
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised pleats catching light
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.91f, 0.88f), 0.14f);              // strap edges and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen over the top edge
            Color slider = new Color(0.78f, 0.79f, 0.82f);                                       // small metal strap adjusters and zip
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.16f);   // finer weave than jersey, faint sheen
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.14f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.18f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.15f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat sliderMat = PartMat.Of(slider, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat narrow band between two points on the garment, aimed from one to the other, with a
            // lighter edge running down it — used for every strap run so a strap is a single call.
            void Strap(string name, Vector3 from, Vector3 to, float width, float y, PartMat bandMat)
            {
                float sdx = to.x - from.x, sdz = to.z - from.z;
                float len = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                float yaw = Mathf.Atan2(-sdz, sdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);

                var band = Block(root, name, mid, new Vector3(len, 0.007f, width), bandMat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                var edge = Block(root, name + "Edge", new Vector3(mid.x, y + 0.005f, mid.z),
                    new Vector3(len * 0.92f, 0.004f, width * 0.36f), trimMat);
                edge.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Bodice: a narrow strapless top drawn in to the waist, no sleeve heads at the shoulders ────
            Sphere(root, "BodiceTop", new Vector3(0.128f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.190f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.050f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.050f, 0.215f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.060f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.190f), fabricMat);

            // Straight bound top edge with the dark inside of the dress showing just behind it.
            Block(root, "TopEdge", new Vector3(0.170f, 0.004f, 0f), new Vector3(0.014f, 0.046f, 0.176f), trimMat);
            Block(root, "TopEdgeStitch", new Vector3(0.158f, 0.026f, 0f), new Vector3(0.005f, 0.006f, 0.168f), stitchMat);
            Sphere(root, "TopHollow", new Vector3(0.150f, 0.014f, 0f), Vector3.zero, new Vector3(0.040f, 0.028f, 0.150f), liningMat);

            // Rows of shirring gathering the top of the bodice: raised elastic lines with shade between them.
            const int shirRows = 4;
            for (int i = 0; i < shirRows; i++)
            {
                float x = 0.146f - i * 0.017f;
                float halfZ = 0.082f - i * 0.004f;
                Block(root, "Shirr" + i, new Vector3(x, 0.027f, 0f), new Vector3(0.007f, 0.006f, halfZ * 2f), fabricHiMat);
                Block(root, "ShirrShade" + i, new Vector3(x - 0.008f, 0.024f, 0f), new Vector3(0.005f, 0.005f, halfZ * 1.9f), fabricDeepMat);
            }

            // Gathers pinched into the cloth under the shirring, so the top reads as elasticated rather than cut straight.
            for (int i = 0; i < 7; i++)
            {
                float z = (i / 6f * 2f - 1f) * 0.070f;
                Sphere(root, "Gather" + i, new Vector3(0.118f, 0.026f, z), new Vector3(0f, 0f, 0f),
                    new Vector3(0.070f, 0.010f, 0.012f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Shoulder straps ───────────────────────────────────────────────────────────────────────────
            // One strap still reaches out past the shoulder, folding back where it would cross to the back
            // of the dress; the other has slipped off and trails away over the cloth.
            Strap("StrapFrontL", new Vector3(0.163f, 0f, 0.055f), new Vector3(0.258f, 0f, 0.108f), 0.022f, 0.030f, fabricMat);
            Strap("StrapBackL", new Vector3(0.258f, 0f, 0.108f), new Vector3(0.214f, 0f, 0.158f), 0.022f, 0.032f, fabricMat);
            Block(root, "StrapTackL", new Vector3(0.166f, 0.036f, 0.056f), new Vector3(0.012f, 0.006f, 0.026f), stitchMat);
            var sliderL = Block(root, "StrapSliderL", new Vector3(0.243f, 0.036f, 0.098f), new Vector3(0.010f, 0.008f, 0.030f), sliderMat);
            sliderL.localEulerAngles = new Vector3(0f, -29f, 0f);

            Strap("StrapFallenA", new Vector3(0.163f, 0f, -0.055f), new Vector3(0.236f, 0f, -0.112f), 0.022f, 0.030f, fabricMat);
            Strap("StrapFallenB", new Vector3(0.236f, 0f, -0.112f), new Vector3(0.148f, 0f, -0.176f), 0.022f, 0.032f, fabricMat);
            Block(root, "StrapTackR", new Vector3(0.166f, 0.036f, -0.056f), new Vector3(0.012f, 0.006f, 0.026f), stitchMat);
            var sliderR = Block(root, "StrapSliderR", new Vector3(0.196f, 0.038f, -0.150f), new Vector3(0.010f, 0.008f, 0.030f), sliderMat);
            sliderR.localEulerAngles = new Vector3(0f, 36f, 0f);
            Sphere(root, "StrapEndR", new Vector3(0.140f, 0.030f, -0.182f), Vector3.zero, new Vector3(0.030f, 0.012f, 0.026f), fabricDeepMat);

            // ── Gathered A-line skirt: each mound wider than the last, so the hem is the broadest part ─────
            Sphere(root, "SkirtTop", new Vector3(-0.145f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.050f, 0.255f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.255f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.330f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.355f, -0.002f, 0f), Vector3.zero, new Vector3(0.14f, 0.046f, 0.390f), fabricMat);

            // Knife pleats fanning out from the waist to the hem: raised ridges with shaded valleys between
            // them, each laid along the line it follows so the skirt reads as gathered cloth rather than a slab.
            const int pleats = 7;
            for (int i = 0; i < pleats; i++)
            {
                float t = i / (pleats - 1f) * 2f - 1f;                     // −1 at one side of the skirt, +1 at the other
                Vector3 from = new Vector3(-0.115f, 0f, t * 0.070f);       // where it leaves the waist seam
                Vector3 to = new Vector3(-0.395f, 0f, t * 0.185f);         // where it reaches the hem
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;          // aim the part's length from waist to hem
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var ridge = Block(root, "Pleat" + i, new Vector3(mid.x, 0.028f, mid.z),
                    new Vector3(len, 0.010f, 0.026f), fabricHiMat);
                ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                var valley = Block(root, "PleatShade" + i, new Vector3(mid.x, 0.024f, mid.z + 0.030f),
                    new Vector3(len * 0.92f, 0.008f, 0.012f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Waist seam where the gathered skirt is set on to the bodice, with the join shaded just below it.
            Block(root, "WaistSeam", new Vector3(-0.098f, 0.028f, 0f), new Vector3(0.007f, 0.006f, 0.190f), stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.112f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.185f), fabricDeepMat);

            // ── Side zip running from under the arm down past the waist, closing the fitted bodice ────────
            Block(root, "ZipTape", new Vector3(0.020f, 0.030f, 0.104f), new Vector3(0.170f, 0.005f, 0.014f), fabricDeepMat);
            for (int i = 0; i < 9; i++)
            {
                float x = 0.096f - i * 0.019f;
                Block(root, "ZipTooth" + i, new Vector3(x, 0.034f, 0.104f), new Vector3(0.008f, 0.004f, 0.016f), sliderMat);
            }
            Block(root, "ZipSlider", new Vector3(-0.062f, 0.036f, 0.104f), new Vector3(0.022f, 0.008f, 0.022f), sliderMat);
            Block(root, "ZipPull", new Vector3(-0.084f, 0.036f, 0.104f), new Vector3(0.020f, 0.005f, 0.010f), sliderMat);

            // ── Faced hem across the wide end, with its blind stitch just above it ─────────────────────────
            Block(root, "Hem", new Vector3(-0.418f, 0.004f, 0f), new Vector3(0.016f, 0.048f, 0.360f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.404f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.340f), stitchMat);

            // Princess seams curving down the front of the bodice from the strap tacks to the waist.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "PrincessSeamL" : "PrincessSeamR",
                    new Vector3(0.055f, 0.027f, s * 0.062f), new Vector3(0.170f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 10f, 0f);
            }

            // A few soft folds and shaded creases so the cloth looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.210f, 0.030f, -0.090f), new Vector3(0f, -18f, 0f), new Vector3(0.19f, 0.016f, 0.05f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.020f, 0.030f, 0.050f), new Vector3(0f, 16f, 0f), new Vector3(0.14f, 0.016f, 0.042f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.310f, 0.024f, 0.110f), new Vector3(0f, 22f, 0f), new Vector3(0.15f, 0.012f, 0.032f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.072f, 0.026f, -0.058f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back of the top edge, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.104f, 0.030f, 0.046f);
            labelFrame.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A full-length dress laid flat, face-up: a sleeveless-shouldered bodice with a deep V neckline and
        /// soft flutter sleeves, gathered at a high empire waist under a narrow self-fabric tie belt, below
        /// which a long column skirt falls almost twice the length of the knee dress and only gently widens,
        /// its cloth broken by long vertical drape folds rather than a wide fan of pleats. A thigh-high slit
        /// runs up one flank and the belt is knotted off to the other, so the slow spin always has something
        /// to catch. Takes the item's tint as the dress fabric.
        /// </summary>
        private static PartNode BuildDressLong(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // light dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / drape valleys
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised drape ridges catching light
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.91f, 0.88f), 0.14f);              // neck, sleeve, slit and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen through neck and slit
            Color belt = new Color(fabric.r * 0.62f, fabric.g * 0.62f, fabric.b * 0.62f);        // self-fabric tie belt, a shade deeper
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.20f);   // drapier, softer-falling cloth than the knee dress
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.16f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.22f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.18f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat beltMat = PartMat.Of(belt, smoothness: 0.24f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // V facings, the armhole seams and the slit edges, so each run of binding is a single call.
            PartRef Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                return band;
            }

            // ── Bodice: shoulders at +X, drawn in to a high empire waist ──────────────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.048f, 0.225f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.075f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.050f, 0.235f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.020f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.180f), fabricMat);

            // ── Deep V neckline: the dark inside showing between two faced edges that meet at a point ─────
            Band("NeckOpenL", new Vector3(0.196f, 0f, 0.054f), new Vector3(0.104f, 0f, 0f), 0.044f, 0.016f, 0.030f, liningMat);
            Band("NeckOpenR", new Vector3(0.196f, 0f, -0.054f), new Vector3(0.104f, 0f, 0f), 0.044f, 0.016f, 0.030f, liningMat);
            Band("NeckFacingL", new Vector3(0.202f, 0f, 0.064f), new Vector3(0.100f, 0f, 0f), 0.018f, 0.028f, 0.024f, trimMat);
            Band("NeckFacingR", new Vector3(0.202f, 0f, -0.064f), new Vector3(0.100f, 0f, 0f), 0.018f, 0.028f, 0.024f, trimMat);
            Band("NeckStitchL", new Vector3(0.190f, 0f, 0.046f), new Vector3(0.108f, 0f, 0f), 0.006f, 0.032f, 0.006f, stitchMat);
            Band("NeckStitchR", new Vector3(0.190f, 0f, -0.046f), new Vector3(0.108f, 0f, 0f), 0.006f, 0.032f, 0.006f, stitchMat);
            Sphere(root, "NeckPoint", new Vector3(0.100f, 0.026f, 0f), Vector3.zero, new Vector3(0.030f, 0.014f, 0.026f), trimMat);

            // Princess seams curving down the front of the bodice from each shoulder to the waist.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "PrincessSeamL" : "PrincessSeamR",
                    new Vector3(0.072f, 0.027f, s * 0.072f), new Vector3(0.190f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 8f, 0f);
            }

            // ── Flutter sleeves: a soft petal off each shoulder with a second, lighter one falling behind ──
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "Sleeve" + side, new Vector3(0.132f, 0f, s * 0.150f), new Vector3(0f, s * -18f, 0f),
                    new Vector3(0.130f, 0.046f, 0.120f), fabricMat);
                Sphere(root, "SleeveFlutter" + side, new Vector3(0.086f, 0.008f, s * 0.174f), new Vector3(0f, s * -34f, 0f),
                    new Vector3(0.115f, 0.030f, 0.090f), fabricHiMat);
                var sleeveEdge = Block(root, "SleeveFacing" + side, new Vector3(0.100f, 0.002f, s * 0.202f),
                    new Vector3(0.125f, 0.046f, 0.014f), trimMat);
                sleeveEdge.localEulerAngles = new Vector3(0f, s * 20f, 0f);
                Band("ArmSeam" + side, new Vector3(0.186f, 0f, s * 0.098f), new Vector3(0.084f, 0f, s * 0.118f),
                    0.009f, 0.030f, 0.006f, stitchMat);
            }

            // ── Column skirt: far longer than the knee dress and only gently widening as it falls ─────────
            Sphere(root, "SkirtTop", new Vector3(-0.115f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.230f), fabricMat);
            Sphere(root, "SkirtUpper", new Vector3(-0.220f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.252f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.325f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.270f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.420f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.048f, 0.286f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.505f, -0.002f, 0f), Vector3.zero, new Vector3(0.16f, 0.046f, 0.298f), fabricMat);

            // Long vertical drape folds running the whole length of the skirt: raised ridges with shaded
            // valleys beside them, barely spreading, so the skirt reads as a falling column of cloth.
            const int drapes = 6;
            for (int i = 0; i < drapes; i++)
            {
                float t = i / (drapes - 1f) * 2f - 1f;                     // −1 at one side of the skirt, +1 at the other
                Vector3 from = new Vector3(-0.065f, 0f, t * 0.050f);       // where it leaves the waist
                Vector3 to = new Vector3(-0.520f, 0f, t * 0.118f);         // where it reaches the hem
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;          // aim the part's length from waist to hem
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var ridge = Block(root, "Drape" + i, new Vector3(mid.x, 0.028f, mid.z),
                    new Vector3(len, 0.010f, 0.022f), fabricHiMat);
                ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                var valley = Block(root, "DrapeShade" + i, new Vector3(mid.x, 0.024f, mid.z + 0.024f),
                    new Vector3(len * 0.94f, 0.008f, 0.011f), fabricDeepMat);
                valley.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Empire waist seam where the long skirt is gathered on, with the join shaded just below it.
            Block(root, "WaistSeam", new Vector3(-0.052f, 0.028f, 0f), new Vector3(0.007f, 0.006f, 0.185f), stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.066f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.180f), fabricDeepMat);
            for (int i = 0; i < 7; i++)
            {
                float z = (i / 6f * 2f - 1f) * 0.072f;
                Sphere(root, "Gather" + i, new Vector3(-0.086f, 0.026f, z), Vector3.zero,
                    new Vector3(0.060f, 0.010f, 0.012f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Thigh-high slit up one flank: dark inside showing between two faced edges ──────────────────
            Band("SlitOpen", new Vector3(-0.300f, 0f, 0.100f), new Vector3(-0.505f, 0f, 0.120f), 0.032f, 0.024f, 0.010f, liningMat);
            Band("SlitEdgeOuter", new Vector3(-0.298f, 0f, 0.124f), new Vector3(-0.503f, 0f, 0.144f), 0.016f, 0.030f, 0.008f, trimMat);
            Band("SlitEdgeInner", new Vector3(-0.302f, 0f, 0.076f), new Vector3(-0.507f, 0f, 0.096f), 0.016f, 0.030f, 0.008f, trimMat);
            Block(root, "SlitTack", new Vector3(-0.296f, 0.032f, 0.100f), new Vector3(0.010f, 0.006f, 0.052f), stitchMat);

            // ── Narrow self-fabric tie belt across the empire waist, knotted off to the far flank ─────────
            Block(root, "Belt", new Vector3(-0.040f, 0.032f, 0f), new Vector3(0.026f, 0.010f, 0.200f), beltMat);
            Block(root, "BeltEdgeTop", new Vector3(-0.029f, 0.036f, 0f), new Vector3(0.005f, 0.006f, 0.192f), fabricDeepMat);
            Block(root, "BeltEdgeLow", new Vector3(-0.051f, 0.036f, 0f), new Vector3(0.005f, 0.006f, 0.192f), fabricDeepMat);

            var beltKnot = new PartNode("BeltKnot");
            beltKnot.SetParent(root, false);
            beltKnot.localPosition = new Vector3(-0.040f, 0.038f, -0.092f);
            beltKnot.localEulerAngles = new Vector3(0f, 26f, 0f);
            Sphere(beltKnot, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.042f, 0.018f, 0.036f), beltMat);
            Block(beltKnot, "TieNear", new Vector3(-0.052f, -0.002f, 0.016f), new Vector3(0.086f, 0.008f, 0.022f), beltMat);
            Block(beltKnot, "TieFar", new Vector3(-0.044f, -0.002f, -0.022f), new Vector3(0.070f, 0.008f, 0.020f), beltMat);
            Block(beltKnot, "TieTip", new Vector3(-0.098f, -0.003f, 0.026f), new Vector3(0.028f, 0.007f, 0.024f), fabricDeepMat);

            // ── Faced hem across the long end, with its blind stitch just above it ─────────────────────────
            Sphere(root, "HemPool", new Vector3(-0.548f, 0.004f, 0f), Vector3.zero, new Vector3(0.09f, 0.040f, 0.288f), fabricMat);
            Block(root, "Hem", new Vector3(-0.575f, 0.004f, 0f), new Vector3(0.016f, 0.048f, 0.272f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.561f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.256f), stitchMat);

            // A few soft folds and shaded creases so the long cloth looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.250f, 0.030f, -0.086f), new Vector3(0f, -12f, 0f), new Vector3(0.22f, 0.016f, 0.048f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(-0.430f, 0.030f, -0.052f), new Vector3(0f, 9f, 0f), new Vector3(0.18f, 0.014f, 0.042f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.360f, 0.024f, 0.058f), new Vector3(0f, 14f, 0f), new Vector3(0.19f, 0.012f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.048f, 0.026f, -0.062f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.140f, 0.030f, 0.052f);
            labelFrame.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A full-length strappy sundress laid flat, face-up, shoulders at +X and hem at −X. It takes the
        /// shirred, strapless top and narrow adjustable shoulder straps of the knee-length strap dress, but
        /// hangs it on a floor-length skirt built in three gathered tiers — each set on below a ruffled seam
        /// and a little wider than the last — so it reads as a maxi rather than either the plain long dress
        /// (one drape-folded column) or the short strap dress (a single fanned pleat skirt). One strap is
        /// still fallen and trailing across the cloth, which keeps the slow spin readable.
        /// </summary>
        private static PartNode BuildDressLongStraps(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // light dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / tier shadows
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised gathers catching light
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.91f, 0.88f), 0.14f);              // strap edges and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen over the top edge
            Color slider = new Color(0.78f, 0.79f, 0.82f);                                       // strap adjusters and zip
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.20f);   // drapier than the knee dress, like the long one
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.16f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.22f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.18f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat sliderMat = PartMat.Of(slider, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat narrow band between two points on the garment, aimed from one to the other, with a
            // lighter edge running down it — used for every strap run so a strap is a single call.
            void Strap(string name, Vector3 from, Vector3 to, float width, float y, PartMat bandMat)
            {
                float sdx = to.x - from.x, sdz = to.z - from.z;
                float len = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                float yaw = Mathf.Atan2(-sdz, sdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);

                var band = Block(root, name, mid, new Vector3(len, 0.007f, width), bandMat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
                var edge = Block(root, name + "Edge", new Vector3(mid.x, y + 0.005f, mid.z),
                    new Vector3(len * 0.92f, 0.004f, width * 0.36f), trimMat);
                edge.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Bodice: a narrow strapless top drawn in to the waist, no sleeve heads at the shoulders ────
            Sphere(root, "BodiceTop", new Vector3(0.128f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.185f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.050f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.050f, 0.210f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.055f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.180f), fabricMat);

            // Straight bound top edge with the dark inside of the dress showing just behind it.
            Block(root, "TopEdge", new Vector3(0.170f, 0.004f, 0f), new Vector3(0.014f, 0.046f, 0.172f), trimMat);
            Block(root, "TopEdgeStitch", new Vector3(0.158f, 0.026f, 0f), new Vector3(0.005f, 0.006f, 0.164f), stitchMat);
            Sphere(root, "TopHollow", new Vector3(0.150f, 0.014f, 0f), Vector3.zero, new Vector3(0.040f, 0.028f, 0.148f), liningMat);

            // Rows of shirring gathering the top of the bodice: raised elastic lines with shade between them.
            const int shirRows = 4;
            for (int i = 0; i < shirRows; i++)
            {
                float x = 0.146f - i * 0.017f;
                float halfZ = 0.080f - i * 0.004f;
                Block(root, "Shirr" + i, new Vector3(x, 0.027f, 0f), new Vector3(0.007f, 0.006f, halfZ * 2f), fabricHiMat);
                Block(root, "ShirrShade" + i, new Vector3(x - 0.008f, 0.024f, 0f), new Vector3(0.005f, 0.005f, halfZ * 1.9f), fabricDeepMat);
            }

            // Gathers pinched into the cloth under the shirring, so the top reads as elasticated rather than cut straight.
            for (int i = 0; i < 7; i++)
            {
                float z = (i / 6f * 2f - 1f) * 0.068f;
                Sphere(root, "BodiceGather" + i, new Vector3(0.118f, 0.026f, z), Vector3.zero,
                    new Vector3(0.070f, 0.010f, 0.012f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Shoulder straps: one reaching out past the shoulder, one slipped off and trailing ─────────
            Strap("StrapFrontL", new Vector3(0.163f, 0f, 0.054f), new Vector3(0.268f, 0f, 0.104f), 0.020f, 0.030f, fabricMat);
            Strap("StrapBackL", new Vector3(0.268f, 0f, 0.104f), new Vector3(0.222f, 0f, 0.160f), 0.020f, 0.032f, fabricMat);
            Block(root, "StrapTackL", new Vector3(0.166f, 0.036f, 0.055f), new Vector3(0.012f, 0.006f, 0.024f), stitchMat);
            var sliderL = Block(root, "StrapSliderL", new Vector3(0.250f, 0.036f, 0.094f), new Vector3(0.010f, 0.008f, 0.028f), sliderMat);
            sliderL.localEulerAngles = new Vector3(0f, -26f, 0f);

            Strap("StrapFallenA", new Vector3(0.163f, 0f, -0.054f), new Vector3(0.244f, 0f, -0.116f), 0.020f, 0.030f, fabricMat);
            Strap("StrapFallenB", new Vector3(0.244f, 0f, -0.116f), new Vector3(0.146f, 0f, -0.190f), 0.020f, 0.032f, fabricMat);
            Block(root, "StrapTackR", new Vector3(0.166f, 0.036f, -0.055f), new Vector3(0.012f, 0.006f, 0.024f), stitchMat);
            var sliderR = Block(root, "StrapSliderR", new Vector3(0.198f, 0.038f, -0.161f), new Vector3(0.010f, 0.008f, 0.028f), sliderMat);
            sliderR.localEulerAngles = new Vector3(0f, 37f, 0f);
            Sphere(root, "StrapEndR", new Vector3(0.138f, 0.030f, -0.196f), Vector3.zero, new Vector3(0.030f, 0.012f, 0.026f), fabricDeepMat);

            // ── Tiered maxi skirt: three gathered tiers falling to the floor, each set on below a ruffled
            //    seam and a shade wider than the one above, so the length reads as tiers rather than a column.
            float[] tierX = { -0.150f, -0.310f, -0.465f };      // centre of each tier
            float[] tierLen = { 0.19f, 0.19f, 0.18f };          // how far along the dress each tier runs
            float[] tierWidth = { 0.250f, 0.300f, 0.344f };     // each tier a little fuller than the last
            float[] seamX = { -0.075f, -0.232f, -0.388f };      // the gathered seam each tier hangs from

            for (int t = 0; t < tierX.Length; t++)
            {
                Sphere(root, "Tier" + t, new Vector3(tierX[t], 0f, 0f), Vector3.zero,
                    new Vector3(tierLen[t], 0.050f - t * 0.002f, tierWidth[t]), fabricMat);

                // Seam the tier is gathered on to, shaded underneath so the tier above overhangs it.
                float seamZ = (t == 0 ? 0.185f : tierWidth[t - 1] * 0.86f);
                Block(root, "TierSeam" + t, new Vector3(seamX[t], 0.028f, 0f), new Vector3(0.007f, 0.006f, seamZ), stitchMat);
                Sphere(root, "TierShade" + t, new Vector3(seamX[t] - 0.014f, 0.022f, 0f), Vector3.zero,
                    new Vector3(0.05f, 0.014f, seamZ), fabricDeepMat);

                // Ruffle of gathers pinched along the seam, alternating lit and shaded.
                for (int i = 0; i < 9; i++)
                {
                    float z = (i / 8f * 2f - 1f) * seamZ * 0.48f;
                    Sphere(root, "TierGather" + t + "_" + i, new Vector3(seamX[t] - 0.026f, 0.026f, z), Vector3.zero,
                        new Vector3(0.052f, 0.010f, 0.012f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
                }

                // Short vertical folds inside the tier, spreading a little as they fall.
                for (int i = 0; i < 5; i++)
                {
                    float f = i / 4f * 2f - 1f;
                    Vector3 from = new Vector3(seamX[t] - 0.030f, 0f, f * tierWidth[t] * 0.30f);
                    Vector3 to = new Vector3(seamX[t] - tierLen[t] * 0.92f, 0f, f * tierWidth[t] * 0.42f);
                    float dx = to.x - from.x, dz = to.z - from.z;
                    float len = Mathf.Sqrt(dx * dx + dz * dz);
                    float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;   // aim the fold from its seam down the tier
                    Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                    var ridge = Block(root, "TierFold" + t + "_" + i, new Vector3(mid.x, 0.028f, mid.z),
                        new Vector3(len, 0.010f, 0.020f), fabricHiMat);
                    ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                    var valley = Block(root, "TierFoldShade" + t + "_" + i, new Vector3(mid.x, 0.024f, mid.z + 0.022f),
                        new Vector3(len * 0.94f, 0.008f, 0.010f), fabricDeepMat);
                    valley.localEulerAngles = new Vector3(0f, yaw, 0f);
                }
            }

            // ── Side zip running from under the arm down past the waist, closing the shirred bodice ───────
            Block(root, "ZipTape", new Vector3(0.024f, 0.030f, 0.100f), new Vector3(0.160f, 0.005f, 0.014f), fabricDeepMat);
            for (int i = 0; i < 9; i++)
            {
                float x = 0.094f - i * 0.018f;
                Block(root, "ZipTooth" + i, new Vector3(x, 0.034f, 0.100f), new Vector3(0.008f, 0.004f, 0.016f), sliderMat);
            }
            Block(root, "ZipSlider", new Vector3(-0.058f, 0.036f, 0.100f), new Vector3(0.022f, 0.008f, 0.022f), sliderMat);
            Block(root, "ZipPull", new Vector3(-0.080f, 0.036f, 0.100f), new Vector3(0.020f, 0.005f, 0.010f), sliderMat);

            // ── Faced hem across the wide end, pooling slightly where the long skirt meets the floor ──────
            Sphere(root, "HemPool", new Vector3(-0.548f, 0.004f, 0f), Vector3.zero, new Vector3(0.09f, 0.042f, 0.336f), fabricMat);
            Block(root, "Hem", new Vector3(-0.578f, 0.004f, 0f), new Vector3(0.016f, 0.048f, 0.310f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.564f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.292f), stitchMat);

            // Princess seams curving down the front of the bodice from the strap tacks to the waist.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "PrincessSeamL" : "PrincessSeamR",
                    new Vector3(0.055f, 0.027f, s * 0.060f), new Vector3(0.170f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 10f, 0f);
            }

            // A few soft folds and shaded creases so the long cloth looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.238f, 0.030f, -0.092f), new Vector3(0f, -14f, 0f), new Vector3(0.17f, 0.016f, 0.048f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(-0.470f, 0.030f, -0.070f), new Vector3(0f, 10f, 0f), new Vector3(0.16f, 0.014f, 0.044f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.372f, 0.024f, 0.086f), new Vector3(0f, 16f, 0f), new Vector3(0.16f, 0.012f, 0.032f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.070f, 0.026f, -0.058f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back of the top edge, sitting off-centre.
            var labelFrame = new PartNode("Label");
            labelFrame.SetParent(root, false);
            labelFrame.localPosition = new Vector3(0.104f, 0.030f, 0.046f);
            labelFrame.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(labelFrame, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(labelFrame, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A plain everyday dress laid flat, face-up, shoulders at +X and hem at −X. Where the other dresses
        /// close at a hidden side zip, this one is a shirtwaist: it buttons all the way down a raised centre
        /// placket, under a pointed shirt collar folded open at the neck, and has proper set-in short sleeves
        /// finished with turned cuffs rather than soft caps or bare straps. A matching self-fabric belt is
        /// threaded through loops at the waist and buckled off to one side, its tail trailing over a softly
        /// draped skirt that falls to mid-calf — longer than the knee dress, well short of the maxi — with a
        /// patch pocket at each hip. Takes the item's tint as the dress cloth.
        /// </summary>
        private static PartNode BuildDressNormal(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // everyday dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / drape valleys
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised placket and folds catching light
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.91f, 0.88f), 0.14f);              // collar, cuff, pocket and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen through the neck
            Color belt = new Color(fabric.r * 0.62f, fabric.g * 0.62f, fabric.b * 0.62f);        // self-fabric belt, a shade deeper
            Color button = Color.Lerp(fabric, new Color(0.96f, 0.95f, 0.90f), 0.62f);            // pale shirt buttons down the front
            Color metal = new Color(0.78f, 0.79f, 0.82f);                                        // small belt buckle
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.16f);   // finer weave than jersey, faint sheen
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.14f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.18f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.15f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat beltMat = PartMat.Of(belt, smoothness: 0.20f);
            PartMat buttonMat = PartMat.Of(button, smoothness: 0.55f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // ── Bodice: shoulders at +X, drawn in to a natural waist ──────────────────────────────────────
            Sphere(root, "Shoulders", new Vector3(0.148f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.048f, 0.245f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.060f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.250f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.055f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.196f), fabricMat);

            // Princess seams and bust darts shaping the front of the bodice.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var seam = Block(root, "PrincessSeam" + side,
                    new Vector3(0.062f, 0.027f, s * 0.072f), new Vector3(0.180f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 9f, 0f);
                var dart = Block(root, "Dart" + side,
                    new Vector3(0.086f, 0.028f, s * 0.086f), new Vector3(0.070f, 0.006f, 0.006f), stitchMat);
                dart.localEulerAngles = new Vector3(0f, s * -26f, 0f);
            }

            // ── Pointed shirt collar folded open over a dark neck hollow ──────────────────────────────────
            Sphere(root, "NeckHollow", new Vector3(0.170f, 0.010f, 0f), Vector3.zero, new Vector3(0.070f, 0.030f, 0.110f), liningMat);
            Block(root, "CollarStand", new Vector3(0.178f, 0.004f, 0f), new Vector3(0.018f, 0.044f, 0.118f), trimMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var leaf = Block(root, "CollarLeaf" + side,
                    new Vector3(0.156f, 0.030f, s * 0.050f), new Vector3(0.078f, 0.008f, 0.054f), trimMat);
                leaf.localEulerAngles = new Vector3(0f, s * 24f, 0f);
                Sphere(root, "CollarPoint" + side, new Vector3(0.122f, 0.030f, s * 0.072f), new Vector3(0f, s * 24f, 0f),
                    new Vector3(0.048f, 0.008f, 0.040f), trimMat);
                var leafStitch = Block(root, "CollarStitch" + side,
                    new Vector3(0.154f, 0.036f, s * 0.062f), new Vector3(0.068f, 0.005f, 0.005f), stitchMat);
                leafStitch.localEulerAngles = new Vector3(0f, s * 24f, 0f);
            }

            // ── Button placket: the shirtwaist's signature, running from the collar well down the skirt ───
            const float plkTop = 0.150f, plkBot = -0.300f;
            const float plkMid = (plkTop + plkBot) * 0.5f, plkLen = plkTop - plkBot;
            Block(root, "Placket", new Vector3(plkMid, 0.030f, 0f), new Vector3(plkLen, 0.010f, 0.036f), fabricHiMat);
            Block(root, "PlacketEdgeL", new Vector3(plkMid, 0.036f, 0.017f), new Vector3(plkLen * 0.98f, 0.005f, 0.005f), stitchMat);
            Block(root, "PlacketEdgeR", new Vector3(plkMid, 0.036f, -0.017f), new Vector3(plkLen * 0.98f, 0.005f, 0.005f), stitchMat);

            const int buttons = 9;
            for (int i = 0; i < buttons; i++)
            {
                float x = 0.128f - i * 0.050f;
                Rod(root, "Button" + i, new Vector3(x, 0.038f, 0f), Vector3.zero, 0.020f, 0.006f, buttonMat);
                Rod(root, "ButtonEye" + i, new Vector3(x, 0.042f, 0f), Vector3.zero, 0.008f, 0.004f, stitchMat);
                // Worked buttonhole bars just off the edge of the placket, on the overlapping side.
                Block(root, "Buttonhole" + i, new Vector3(x, 0.036f, 0.026f), new Vector3(0.014f, 0.005f, 0.006f), stitchMat);
            }

            // ── Set-in short sleeves, each finished with a turned cuff band ────────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "Sleeve" + side, new Vector3(0.118f, 0f, s * 0.175f), Vector3.zero,
                    new Vector3(0.145f, 0.048f, 0.150f), fabricMat);
                Sphere(root, "SleeveFold" + side, new Vector3(0.130f, 0.030f, s * 0.184f), new Vector3(0f, s * -12f, 0f),
                    new Vector3(0.100f, 0.014f, 0.048f), fabricHiMat);
                Block(root, "Cuff" + side, new Vector3(0.112f, 0.001f, s * 0.246f), new Vector3(0.110f, 0.050f, 0.020f), trimMat);
                Block(root, "CuffStitch" + side, new Vector3(0.112f, 0.028f, s * 0.232f), new Vector3(0.100f, 0.006f, 0.006f), stitchMat);
                var armSeam = Block(root, "ArmSeam" + side, new Vector3(0.112f, 0.028f, s * 0.108f),
                    new Vector3(0.140f, 0.006f, 0.010f), stitchMat);
                armSeam.localEulerAngles = new Vector3(0f, s * 26f, 0f);
            }

            // Waist seam where the skirt is set on to the bodice, with the join shaded just below it.
            Block(root, "WaistSeam", new Vector3(-0.096f, 0.028f, 0f), new Vector3(0.007f, 0.006f, 0.205f), stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.110f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.195f), fabricDeepMat);

            // ── Softly draped mid-calf skirt: only gently flared, so it hangs rather than fans out ─────────
            Sphere(root, "SkirtTop", new Vector3(-0.150f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.250f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.265f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.300f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.375f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.048f, 0.330f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.455f, -0.002f, 0f), Vector3.zero, new Vector3(0.12f, 0.046f, 0.336f), fabricMat);

            // Drape folds hanging from the waist to the hem, kept clear of the buttoned centre front.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                for (int k = 0; k < 3; k++)
                {
                    Vector3 from = new Vector3(-0.118f, 0f, s * (0.040f + k * 0.045f));   // where it leaves the waist
                    Vector3 to = new Vector3(-0.436f, 0f, s * (0.080f + k * 0.085f));     // where it reaches the hem
                    float dx = to.x - from.x, dz = to.z - from.z;
                    float len = Mathf.Sqrt(dx * dx + dz * dz);
                    float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;                     // aim the fold from waist to hem
                    Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                    var ridge = Block(root, "Fold" + side + k, new Vector3(mid.x, 0.028f, mid.z),
                        new Vector3(len, 0.010f, 0.024f), fabricHiMat);
                    ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                    var valley = Block(root, "FoldShade" + side + k, new Vector3(mid.x, 0.024f, mid.z + s * 0.026f),
                        new Vector3(len * 0.90f, 0.008f, 0.012f), fabricDeepMat);
                    valley.localEulerAngles = new Vector3(0f, yaw, 0f);
                }
            }

            // ── Patch pocket at each hip, topstitched on over the skirt ───────────────────────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Block(root, "Pocket" + side, new Vector3(-0.205f, 0.030f, s * 0.152f), new Vector3(0.100f, 0.010f, 0.086f), fabricMat);
                Block(root, "PocketTop" + side, new Vector3(-0.158f, 0.034f, s * 0.152f), new Vector3(0.012f, 0.008f, 0.086f), trimMat);
                Block(root, "PocketTopStitch" + side, new Vector3(-0.168f, 0.037f, s * 0.152f), new Vector3(0.005f, 0.005f, 0.078f), stitchMat);
                Block(root, "PocketEdge" + side, new Vector3(-0.252f, 0.036f, s * 0.152f), new Vector3(0.005f, 0.005f, 0.080f), stitchMat);
            }

            // ── Self-fabric belt threaded through loops at the waist and buckled off to one side ──────────
            Block(root, "Belt", new Vector3(-0.082f, 0.034f, 0f), new Vector3(0.040f, 0.010f, 0.240f), beltMat);
            Block(root, "BeltEdgeTop", new Vector3(-0.064f, 0.039f, 0f), new Vector3(0.005f, 0.006f, 0.230f), fabricDeepMat);
            Block(root, "BeltEdgeLow", new Vector3(-0.100f, 0.039f, 0f), new Vector3(0.005f, 0.006f, 0.230f), fabricDeepMat);
            for (int i = 0; i < 4; i++)
            {
                float z = -0.150f + i * 0.100f;
                Block(root, "BeltLoop" + i, new Vector3(-0.082f, 0.040f, z), new Vector3(0.052f, 0.006f, 0.012f), fabricMat);
            }

            Block(root, "Buckle", new Vector3(-0.082f, 0.042f, 0.086f), new Vector3(0.052f, 0.008f, 0.040f), metalMat);
            Block(root, "BuckleHole", new Vector3(-0.082f, 0.047f, 0.086f), new Vector3(0.034f, 0.006f, 0.024f), fabricDeepMat);
            Block(root, "BucklePin", new Vector3(-0.082f, 0.049f, 0.086f), new Vector3(0.006f, 0.005f, 0.044f), metalMat);

            var beltTail = Block(root, "BeltTail", new Vector3(-0.086f, 0.036f, 0.172f), new Vector3(0.038f, 0.008f, 0.132f), beltMat);
            beltTail.localEulerAngles = new Vector3(0f, -14f, 0f);
            Rod(root, "BeltHoleA", new Vector3(-0.086f, 0.041f, 0.150f), Vector3.zero, 0.008f, 0.006f, liningMat);
            Rod(root, "BeltHoleB", new Vector3(-0.088f, 0.041f, 0.180f), Vector3.zero, 0.008f, 0.006f, liningMat);
            Sphere(root, "BeltTailTip", new Vector3(-0.094f, 0.036f, 0.238f), new Vector3(0f, -20f, 0f),
                new Vector3(0.036f, 0.010f, 0.040f), fabricDeepMat);

            // ── Faced hem across the wide end, with its blind stitch just above it ─────────────────────────
            Block(root, "Hem", new Vector3(-0.482f, 0.004f, 0f), new Vector3(0.016f, 0.048f, 0.310f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.468f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.292f), stitchMat);

            // A few soft folds and shaded creases so the cloth looks relaxed rather than pressed flat.
            Sphere(root, "DrapeA", new Vector3(-0.300f, 0.030f, -0.096f), new Vector3(0f, -16f, 0f), new Vector3(0.18f, 0.016f, 0.048f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.392f, 0.024f, 0.104f), new Vector3(0f, 20f, 0f), new Vector3(0.15f, 0.012f, 0.032f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.072f, 0.026f, -0.062f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var careLabel = new PartNode("Label");
            careLabel.SetParent(root, false);
            careLabel.localPosition = new Vector3(0.120f, 0.030f, 0.044f);
            careLabel.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(careLabel, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(careLabel, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// A satin negligee laid flat, face-up, shoulders at +X and hem at −X: a short bias-cut slip in
        /// liquid satin — by far the glossiest cloth in the wardrobe — hung from spaghetti straps so fine
        /// they are barely there, with scalloped lace edging the V of the bust and running right round the
        /// hem, a little bow at the bust point and a lace-trimmed slit up one flank. There is no zip, no
        /// belt and no pleating: cut on the bias, the cloth falls in soft diagonal ripples crossed by long
        /// streaks of sheen rather than the straight pleats, tiers and drapes of the other dresses, and it
        /// stops well above the knee, making it the shortest of them. One strap is slipped off and trailing,
        /// which keeps the slow spin readable. Takes the item's tint as the satin.
        /// </summary>
        private static PartNode BuildDressSatinNegligee(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // satin slip cloth
            Color fabricDeep = new Color(fabric.r * 0.72f, fabric.g * 0.72f, fabric.b * 0.72f);  // shaded hollows between the ripples
            Color fabricHi = Color.Lerp(fabric, new Color(0.97f, 0.96f, 0.94f), 0.34f);          // ripple crests, lifted further than matte cloth
            Color sheen = Color.Lerp(fabric, Color.white, 0.55f);                                // long specular streaks only satin gets
            Color lace = Color.Lerp(fabric, new Color(0.97f, 0.96f, 0.94f), 0.58f);              // pale lace edging
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // fine contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen through the V and the slit
            Color slider = new Color(0.80f, 0.81f, 0.84f);                                       // tiny strap adjusters
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            // Satin is the glossiest fabric in the wardrobe — well past the raincoat's wet PVC — while the
            // lace stays matte, so the two materials read apart even where they meet along an edge.
            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.78f);
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.66f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.86f);
            PartMat sheenMat = PartMat.Of(sheen, smoothness: 0.92f);
            PartMat laceMat = PartMat.Of(lace, smoothness: 0.16f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.14f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.30f);
            PartMat sliderMat = PartMat.Of(slider, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // neckline facings, the seams and the slit edges so each run is a single call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                float yaw = Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg;
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Runs scalloped lace edging along an edge: a narrow braid with a row of round scallops hanging
            // off it and a picot hole punched above each one. <paramref name="side"/> flips which way the
            // scallops hang, so the same call trims a neckline inwards or a hem outwards.
            void Lace(string name, Vector3 from, Vector3 to, int scallops, float y, float depth, int side)
            {
                float ldx = to.x - from.x, ldz = to.z - from.z;
                float len = Mathf.Sqrt(ldx * ldx + ldz * ldz);
                float yaw = Mathf.Atan2(-ldz, ldx) * Mathf.Rad2Deg;
                float px = -ldz / len * side, pz = ldx / len * side;   // across the edge, the way the scallops fall

                var braid = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, 0.007f, depth * 0.5f), laceMat);
                braid.localEulerAngles = new Vector3(0f, yaw, 0f);

                for (int i = 0; i < scallops; i++)
                {
                    float t = (i + 0.5f) / scallops;
                    float ex = from.x + ldx * t, ez = from.z + ldz * t;
                    Sphere(root, name + "Scallop" + i, new Vector3(ex + px * depth * 0.44f, y, ez + pz * depth * 0.44f),
                        Vector3.zero, new Vector3(depth * 0.80f, 0.009f, depth * 0.80f), laceMat);
                    Sphere(root, name + "Picot" + i, new Vector3(ex - px * depth * 0.12f, y + 0.005f, ez - pz * depth * 0.12f),
                        Vector3.zero, new Vector3(depth * 0.24f, 0.006f, depth * 0.24f), liningMat);
                }
            }

            // Lays one fine spaghetti strap between two points, with a satin highlight down its length.
            void Strap(string name, Vector3 from, Vector3 to, float y)
            {
                float sdx = to.x - from.x, sdz = to.z - from.z;
                float len = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                float yaw = Mathf.Atan2(-sdz, sdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);

                var cord = Block(root, name, mid, new Vector3(len, 0.007f, 0.009f), fabricMat);
                cord.localEulerAngles = new Vector3(0f, yaw, 0f);
                var gleam = Block(root, name + "Gleam", new Vector3(mid.x, y + 0.005f, mid.z),
                    new Vector3(len * 0.88f, 0.004f, 0.004f), sheenMat);
                gleam.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Slip body: shaped over the bust, skimming the waist, flaring only a little to a short hem ──
            Sphere(root, "CupL", new Vector3(0.158f, 0f, 0.050f), new Vector3(0f, -14f, 0f), new Vector3(0.085f, 0.048f, 0.096f), fabricMat);
            Sphere(root, "CupR", new Vector3(0.158f, 0f, -0.050f), new Vector3(0f, 14f, 0f), new Vector3(0.085f, 0.048f, 0.096f), fabricMat);
            Sphere(root, "Bust", new Vector3(0.116f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.046f, 0.186f), fabricMat);
            Sphere(root, "Midriff", new Vector3(0.026f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.046f, 0.172f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.048f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.046f, 0.180f), fabricMat);
            Sphere(root, "Hip", new Vector3(-0.142f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.050f, 0.214f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.242f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.048f, 0.232f), fabricMat);
            Sphere(root, "HemFlare", new Vector3(-0.306f, -0.002f, 0f), Vector3.zero, new Vector3(0.11f, 0.044f, 0.240f), fabricMat);

            // ── Lace-trimmed V bust: the dark inside showing between two edges that meet at a point ───────
            Band("NeckOpenL", new Vector3(0.198f, 0f, 0.078f), new Vector3(0.132f, 0f, 0f), 0.048f, 0.016f, 0.030f, liningMat);
            Band("NeckOpenR", new Vector3(0.198f, 0f, -0.078f), new Vector3(0.132f, 0f, 0f), 0.048f, 0.016f, 0.030f, liningMat);
            Lace("NeckLaceL", new Vector3(0.196f, 0f, 0.082f), new Vector3(0.130f, 0f, 0.004f), 5, 0.028f, 0.030f, 1);
            Lace("NeckLaceR", new Vector3(0.196f, 0f, -0.082f), new Vector3(0.130f, 0f, -0.004f), 5, 0.028f, 0.030f, -1);
            Sphere(root, "NeckPoint", new Vector3(0.126f, 0.026f, 0f), Vector3.zero, new Vector3(0.028f, 0.014f, 0.024f), laceMat);

            // Seams shaping the cups, and the fine seam they are set on to just under the bust.
            Band("CupSeamL", new Vector3(0.192f, 0f, 0.084f), new Vector3(0.108f, 0f, 0.040f), 0.006f, 0.030f, 0.006f, stitchMat);
            Band("CupSeamR", new Vector3(0.192f, 0f, -0.084f), new Vector3(0.108f, 0f, -0.040f), 0.006f, 0.030f, 0.006f, stitchMat);
            Block(root, "UnderBustSeam", new Vector3(0.086f, 0.028f, 0f), new Vector3(0.006f, 0.006f, 0.176f), stitchMat);

            // Little satin bow tacked at the bust point, tails falling to one side.
            var bow = new PartNode("Bow");
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(0.122f, 0.032f, 0f);
            bow.localEulerAngles = new Vector3(0f, 22f, 0f);
            Sphere(bow, "LoopL", new Vector3(0f, 0f, 0.024f), new Vector3(0f, 28f, 0f), new Vector3(0.044f, 0.014f, 0.030f), fabricHiMat);
            Sphere(bow, "LoopR", new Vector3(0f, 0f, -0.024f), new Vector3(0f, -28f, 0f), new Vector3(0.044f, 0.014f, 0.030f), fabricHiMat);
            Sphere(bow, "Knot", Vector3.zero, Vector3.zero, new Vector3(0.020f, 0.016f, 0.018f), fabricMat);
            Block(bow, "TailNear", new Vector3(-0.030f, -0.002f, 0.012f), new Vector3(0.052f, 0.006f, 0.012f), fabricMat);
            Block(bow, "TailFar", new Vector3(-0.026f, -0.002f, -0.014f), new Vector3(0.044f, 0.006f, 0.011f), fabricDeepMat);

            // ── Spaghetti straps: one reaching out over the shoulder, one slipped off and trailing ────────
            Strap("StrapFrontL", new Vector3(0.194f, 0f, 0.076f), new Vector3(0.286f, 0f, 0.116f), 0.030f);
            Strap("StrapBackL", new Vector3(0.286f, 0f, 0.116f), new Vector3(0.240f, 0f, 0.172f), 0.032f);
            Block(root, "StrapTackL", new Vector3(0.196f, 0.036f, 0.077f), new Vector3(0.010f, 0.006f, 0.016f), stitchMat);
            var sliderL = Block(root, "StrapSliderL", new Vector3(0.268f, 0.036f, 0.108f), new Vector3(0.008f, 0.007f, 0.018f), sliderMat);
            sliderL.localEulerAngles = new Vector3(0f, -24f, 0f);

            Strap("StrapFallenA", new Vector3(0.194f, 0f, -0.076f), new Vector3(0.264f, 0f, -0.136f), 0.030f);
            Strap("StrapFallenB", new Vector3(0.264f, 0f, -0.136f), new Vector3(0.168f, 0f, -0.204f), 0.032f);
            Block(root, "StrapTackR", new Vector3(0.196f, 0.036f, -0.077f), new Vector3(0.010f, 0.006f, 0.016f), stitchMat);
            var sliderR = Block(root, "StrapSliderR", new Vector3(0.214f, 0.038f, -0.172f), new Vector3(0.008f, 0.007f, 0.018f), sliderMat);
            sliderR.localEulerAngles = new Vector3(0f, 35f, 0f);
            Sphere(root, "StrapEndR", new Vector3(0.160f, 0.030f, -0.210f), Vector3.zero, new Vector3(0.024f, 0.010f, 0.018f), fabricDeepMat);

            // ── Bias-cut ripples: the skirt is cut across the grain, so the cloth breaks into soft diagonal
            //    waves running off the hip rather than the straight pleats or tiers of the other dresses.
            const int ripples = 5;
            for (int i = 0; i < ripples; i++)
            {
                Vector3 from = new Vector3(-0.062f, 0f, -0.095f + i * 0.045f);
                Vector3 to = new Vector3(-0.300f, 0f, from.z + 0.038f);
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;      // aim the wave from the hip down across the skirt
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var crest = Block(root, "Ripple" + i, new Vector3(mid.x, 0.028f, mid.z),
                    new Vector3(len, 0.010f, 0.024f), fabricHiMat);
                crest.localEulerAngles = new Vector3(0f, yaw, 0f);
                var hollow = Block(root, "RippleShade" + i, new Vector3(mid.x, 0.024f, mid.z + 0.026f),
                    new Vector3(len * 0.94f, 0.008f, 0.012f), fabricDeepMat);
                hollow.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Long streaks of light sliding across the satin, cutting the ripples at their own angle.
            Sphere(root, "SheenA", new Vector3(-0.180f, 0.032f, -0.026f), new Vector3(0f, -22f, 0f), new Vector3(0.24f, 0.012f, 0.030f), sheenMat);
            Sphere(root, "SheenB", new Vector3(-0.088f, 0.032f, 0.070f), new Vector3(0f, 26f, 0f), new Vector3(0.17f, 0.010f, 0.024f), sheenMat);
            Sphere(root, "SheenC", new Vector3(0.062f, 0.032f, -0.058f), new Vector3(0f, -30f, 0f), new Vector3(0.13f, 0.010f, 0.022f), sheenMat);

            // French side seams running the length of the slip, and the join where the skirt meets the body.
            Band("SideSeamL", new Vector3(0.150f, 0f, 0.086f), new Vector3(-0.300f, 0f, 0.116f), 0.006f, 0.028f, 0.006f, stitchMat);
            Band("SideSeamR", new Vector3(0.150f, 0f, -0.086f), new Vector3(-0.300f, 0f, -0.116f), 0.006f, 0.028f, 0.006f, stitchMat);
            Sphere(root, "HipShade", new Vector3(-0.078f, 0.022f, 0f), Vector3.zero, new Vector3(0.05f, 0.014f, 0.176f), fabricDeepMat);

            // ── Short slit up one flank, its dark inside showing between two lace-trimmed edges ────────────
            Band("SlitOpen", new Vector3(-0.190f, 0f, 0.108f), new Vector3(-0.318f, 0f, 0.122f), 0.030f, 0.024f, 0.010f, liningMat);
            Lace("SlitLace", new Vector3(-0.188f, 0f, 0.124f), new Vector3(-0.316f, 0f, 0.138f), 4, 0.030f, 0.024f, -1);
            Block(root, "SlitTack", new Vector3(-0.186f, 0.032f, 0.108f), new Vector3(0.008f, 0.006f, 0.044f), stitchMat);

            // ── Scalloped lace right round the short hem, in place of the plain faced hems of the others ──
            Lace("HemLace", new Vector3(-0.330f, 0f, -0.118f), new Vector3(-0.330f, 0f, 0.118f), 11, 0.020f, 0.032f, 1);
            Block(root, "HemStitch", new Vector3(-0.316f, 0.026f, 0f), new Vector3(0.005f, 0.006f, 0.226f), stitchMat);

            // A couple of shaded creases so the slippery cloth looks relaxed rather than pressed flat.
            Sphere(root, "CreaseA", new Vector3(-0.256f, 0.024f, 0.074f), new Vector3(0f, 18f, 0f), new Vector3(0.14f, 0.012f, 0.028f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.046f, 0.026f, 0.062f), new Vector3(0f, -16f, 0f), new Vector3(0.11f, 0.012f, 0.026f), fabricDeepMat);

            // Woven care label sewn inside the back of the bust edge, sitting off-centre.
            var negligeeLabel = new PartNode("Label");
            negligeeLabel.SetParent(root, false);
            negligeeLabel.localPosition = new Vector3(0.096f, 0.030f, 0.048f);
            negligeeLabel.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(negligeeLabel, "LabelTab", Vector3.zero, new Vector3(0.024f, 0.005f, 0.030f), labelMat);
            Block(negligeeLabel, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.013f, 0.004f, 0.018f), liningMat);

            return root;
        }

        /// <summary>
        /// A short day dress laid flat, face-up, shoulders at +X and hem at −X: the briefest of the everyday
        /// dresses, cut as a skater dress. A rounded Peter Pan collar sits at the neck, puffed and gathered
        /// short sleeves stand off each shoulder, and an elasticated waist casing — no sash, belt or zip —
        /// gathers the cloth high, from where a circle skirt breaks into godet flutes radiating from a single
        /// point and finishes in a curved, scalloped hemline rather than the straight faced hems of the other
        /// dresses. One corner of that hem is flipped back, showing the dark inside and keeping the slow spin
        /// readable. Takes the item's tint as the dress fabric.
        /// </summary>
        private static PartNode BuildDressShort(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // light dress cloth
            Color fabricDeep = new Color(fabric.r * 0.78f, fabric.g * 0.78f, fabric.b * 0.78f);  // shaded folds / flute hollows
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.92f), 0.20f);          // raised flutes catching light
            Color trim = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.90f), 0.30f);              // contrast collar, cuff and hem piping
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.5f);             // contrast topstitching
            Color lining = new Color(fabric.r * 0.42f, fabric.g * 0.42f, fabric.b * 0.42f);      // dark inside, seen at the neck and turned hem
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.16f);   // finer weave than jersey, faint sheen
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.14f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.18f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.18f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // The skirt is a circle cut: every flute and every scallop of the hem is measured out from this
            // one point at the waist, which is what gives the short dress its curved, swinging hemline.
            const float hubX = -0.062f;
            const float hemR = 0.225f;      // waist to hem — short enough to sit well above the knee
            const float hemSpan = 58f;      // how far the skirt sweeps round to each side

            // ── Bodice: shoulders at +X, drawn in to a waist sitting high on the body ─────────────────────
            Sphere(root, "Shoulders", new Vector3(0.140f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.048f, 0.230f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.058f, 0f, 0f), Vector3.zero, new Vector3(0.20f, 0.050f, 0.240f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.020f, 0f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.185f), fabricMat);

            // Princess seams curving down the front of the bodice from each shoulder to the waist.
            for (int s = -1; s <= 1; s += 2)
            {
                var seam = Block(root, s > 0 ? "PrincessSeamL" : "PrincessSeamR",
                    new Vector3(0.062f, 0.027f, s * 0.068f), new Vector3(0.170f, 0.006f, 0.007f), stitchMat);
                seam.localEulerAngles = new Vector3(0f, s * 9f, 0f);
            }

            // ── Circle skirt: short mounds widening quickly, so it flares far more sharply than it falls ──
            Sphere(root, "SkirtTop", new Vector3(-0.112f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.290f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.196f, 0f, 0f), Vector3.zero, new Vector3(0.19f, 0.050f, 0.400f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.258f, -0.002f, 0f), Vector3.zero, new Vector3(0.15f, 0.046f, 0.430f), fabricMat);

            // Godet flutes radiating from the single waist point: raised ridges with a hollow beside each, all
            // fanning out along their own radius so the skirt reads as a swinging circle cut rather than the
            // knife pleats set along the waist of the knee-length dress.
            const int flutes = 9;
            for (int i = 0; i < flutes; i++)
            {
                float deg = (i / (flutes - 1f) * 2f - 1f) * 52f;
                float rad = deg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                Vector3 from = new Vector3(hubX - 0.075f * cos, 0f, 0.075f * sin);   // where it leaves the waist casing
                Vector3 to = new Vector3(hubX - 0.205f * cos, 0f, 0.205f * sin);     // where it runs out at the hem
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;                    // aim the part out along its radius
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var ridge = Block(root, "Flute" + i, new Vector3(mid.x, 0.028f, mid.z),
                    new Vector3(len, 0.010f, 0.024f), fabricHiMat);
                ridge.localEulerAngles = new Vector3(0f, yaw, 0f);
                var hollow = Block(root, "FluteShade" + i, new Vector3(mid.x, 0.024f, mid.z + 0.026f),
                    new Vector3(len * 0.90f, 0.008f, 0.012f), fabricDeepMat);
                hollow.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Elasticated waist casing: a gathered band in place of the sash, belt or zip of the others ─
            Block(root, "WaistCasing", new Vector3(-0.062f, 0.030f, 0f), new Vector3(0.030f, 0.010f, 0.195f), trimMat);
            Block(root, "CasingSeamTop", new Vector3(-0.048f, 0.034f, 0f), new Vector3(0.005f, 0.006f, 0.188f), stitchMat);
            Block(root, "CasingSeamLow", new Vector3(-0.076f, 0.034f, 0f), new Vector3(0.005f, 0.006f, 0.188f), stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.086f, 0.022f, 0f), Vector3.zero, new Vector3(0.04f, 0.014f, 0.185f), fabricDeepMat);
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f * 2f - 1f) * 0.082f;
                Sphere(root, "WaistGather" + i, new Vector3(-0.062f, 0.034f, z), Vector3.zero,
                    new Vector3(0.030f, 0.010f, 0.012f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Puffed short sleeves: a gathered head standing off each shoulder over an elasticated band ─
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "SleevePuff" + side, new Vector3(0.118f, 0.004f, s * 0.168f), Vector3.zero,
                    new Vector3(0.135f, 0.056f, 0.130f), fabricMat);
                Sphere(root, "SleeveCrown" + side, new Vector3(0.126f, 0.026f, s * 0.164f), Vector3.zero,
                    new Vector3(0.090f, 0.020f, 0.090f), fabricHiMat);

                // Gathers pinched round the sleeve head where it is set into the armhole.
                for (int i = 0; i < 5; i++)
                {
                    float t = i / 4f * 2f - 1f;
                    Sphere(root, "SleeveGather" + side + i, new Vector3(0.152f + t * 0.012f, 0.026f, s * (0.128f + Mathf.Abs(t) * 0.018f)),
                        new Vector3(0f, s * 34f, 0f), new Vector3(0.030f, 0.010f, 0.014f),
                        (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
                }

                // Elasticated cuff band closing the puff at the outer edge of the sleeve.
                Block(root, "SleeveCuff" + side, new Vector3(0.112f, 0.002f, s * 0.226f),
                    new Vector3(0.104f, 0.050f, 0.018f), trimMat);
                Block(root, "SleeveCuffStitch" + side, new Vector3(0.112f, 0.028f, s * 0.212f),
                    new Vector3(0.096f, 0.006f, 0.006f), stitchMat);

                var armSeam = Block(root, "ArmSeam" + side, new Vector3(0.112f, 0.028f, s * 0.100f),
                    new Vector3(0.115f, 0.006f, 0.010f), stitchMat);
                armSeam.localEulerAngles = new Vector3(0f, s * 30f, 0f);
            }

            // ── Peter Pan collar: two rounded leaves meeting at the front over a dark neck hollow ─────────
            Sphere(root, "NeckHollow", new Vector3(0.150f, 0.010f, 0f), Vector3.zero, new Vector3(0.078f, 0.032f, 0.115f), liningMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "Collar" + side, new Vector3(0.166f, 0.022f, s * 0.048f), new Vector3(0f, s * 22f, 0f),
                    new Vector3(0.078f, 0.020f, 0.090f), trimMat);
                Sphere(root, "CollarRoll" + side, new Vector3(0.180f, 0.028f, s * 0.044f), new Vector3(0f, s * 22f, 0f),
                    new Vector3(0.046f, 0.016f, 0.074f), fabricHiMat);
                var collarStitch = Block(root, "CollarStitch" + side, new Vector3(0.164f, 0.034f, s * 0.052f),
                    new Vector3(0.062f, 0.005f, 0.006f), stitchMat);
                collarStitch.localEulerAngles = new Vector3(0f, s * 26f, 0f);
            }
            Sphere(root, "CollarPoint", new Vector3(0.148f, 0.026f, 0f), Vector3.zero, new Vector3(0.040f, 0.018f, 0.036f), trimMat);

            // ── Curved, scalloped hemline swept round the waist point — the short dress's clearest tell ───
            const int scallops = 13;
            for (int i = 0; i < scallops; i++)
            {
                float deg = (i / (scallops - 1f) * 2f - 1f) * hemSpan;
                float rad = deg * Mathf.Deg2Rad;
                Vector3 p = new Vector3(hubX - hemR * Mathf.Cos(rad), 0f, hemR * Mathf.Sin(rad));

                Sphere(root, "Scallop" + i, new Vector3(p.x, 0.006f, p.z), new Vector3(0f, -deg, 0f),
                    new Vector3(0.052f, 0.046f, 0.058f), fabricMat);
                Sphere(root, "ScallopEdge" + i, new Vector3(p.x - 0.008f * Mathf.Cos(rad), 0.024f, p.z + 0.008f * Mathf.Sin(rad)),
                    new Vector3(0f, -deg, 0f), new Vector3(0.030f, 0.014f, 0.050f), trimMat);
            }

            // ── One corner of that hem flipped back on itself, showing the dark inside of the skirt ───────
            var flip = new PartNode("HemFlip");
            flip.SetParent(root, false);
            flip.localPosition = new Vector3(-0.196f, 0.030f, -0.176f);
            flip.localEulerAngles = new Vector3(0f, -38f, 0f);
            Sphere(flip, "FlipUnder", Vector3.zero, Vector3.zero, new Vector3(0.110f, 0.016f, 0.070f), liningMat);
            Sphere(flip, "FlipFace", new Vector3(-0.014f, 0.010f, 0.006f), Vector3.zero, new Vector3(0.086f, 0.014f, 0.056f), fabricMat);
            Block(flip, "FlipEdge", new Vector3(0.046f, 0.010f, 0f), new Vector3(0.016f, 0.014f, 0.062f), trimMat);
            Block(flip, "FlipCrease", new Vector3(-0.048f, 0.010f, 0f), new Vector3(0.008f, 0.010f, 0.058f), fabricDeepMat);

            // A few soft folds and shaded creases so the cloth looks relaxed rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.150f, 0.030f, 0.096f), new Vector3(0f, 24f, 0f), new Vector3(0.15f, 0.016f, 0.044f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.028f, 0.030f, 0.052f), new Vector3(0f, 16f, 0f), new Vector3(0.14f, 0.016f, 0.042f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.208f, 0.024f, 0.132f), new Vector3(0f, 30f, 0f), new Vector3(0.13f, 0.012f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.080f, 0.026f, -0.060f), new Vector3(0f, -14f, 0f), new Vector3(0.12f, 0.012f, 0.030f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var shortLabel = new PartNode("Label");
            shortLabel.SetParent(root, false);
            shortLabel.localPosition = new Vector3(0.116f, 0.030f, 0.042f);
            shortLabel.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(shortLabel, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.034f), labelMat);
            Block(shortLabel, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// The little black cocktail dress laid flat, face-up, bust at +X and hem at −X. Alone among the
        /// dresses it is cut as a strapless sheath: a boned sweetheart top with no strap, sleeve or collar at
        /// all, a slim satin ribbon at the waist, and a narrow pencil skirt that stops narrowing rather than
        /// flaring, so its hem is the *tightest* part where every sibling's is the widest. A concealed centre
        /// zip runs down the back and a short kick vent is cut into the hem so the skirt can be walked in.
        /// The item's tint only warms the near-black cloth — the name is the colour here.
        /// </summary>
        private static PartNode BuildDressSmallBlackStrapless(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // The catalogue tint is the generic clothing grey, but this dress is named for its colour, so the
            // cloth is taken almost all the way to black and the tint is left only as a faint cast in it.
            Color fabric = Color.Lerp(def.Tint, new Color(0.055f, 0.055f, 0.065f), 0.86f);
            Color fabricDeep = new Color(fabric.r * 0.55f, fabric.g * 0.55f, fabric.b * 0.55f);   // shaded folds and seam hollows
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.96f), 0.20f);           // raised boning and gathers catching light
            Color sheen = Color.Lerp(fabric, new Color(0.96f, 0.96f, 0.97f), 0.34f);              // satin highlights — black needs a stronger lift to read
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.92f, 0.94f), 0.12f);               // bound edges and facings
            Color stitch = Color.Lerp(fabric, new Color(0.90f, 0.89f, 0.84f), 0.30f);             // fine tonal topstitching
            Color lining = new Color(0.62f, 0.55f, 0.50f);                                       // pale lining, the only thing that shows inside a black dress
            Color grip = Color.Lerp(lining, Color.white, 0.30f);                                 // silicone gripper tape inside the top edge
            Color metal = new Color(0.74f, 0.75f, 0.78f);                                        // concealed zip and hook
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.34f);   // crepe with a satin face — glossier than the day dresses, short of the negligee
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.24f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.38f);
            PartMat sheenMat = PartMat.Of(sheen, smoothness: 0.52f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.40f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.16f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.18f);
            PartMat gripMat = PartMat.Of(grip, smoothness: 0.60f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // sweetheart edges, the shaping seams and the vent facings, so each run of binding is one call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg, 0f);
            }

            // ── Bodice: a bare, boned top nipping in hard to the waist, no shoulder line at all ───────────
            Sphere(root, "BustTop", new Vector3(0.166f, 0f, 0f), Vector3.zero, new Vector3(0.09f, 0.046f, 0.196f), fabricMat);
            Sphere(root, "Bust", new Vector3(0.096f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.052f, 0.204f), fabricMat);
            Sphere(root, "Midriff", new Vector3(0.010f, 0f, 0f), Vector3.zero, new Vector3(0.13f, 0.048f, 0.178f), fabricMat);

            // Shaped sweetheart top edge: two bound curves rising to the sides from a shallow dip at the centre,
            // with the pale lining and its gripper tape showing just inside them.
            Sphere(root, "TopHollow", new Vector3(0.170f, 0.014f, 0f), Vector3.zero, new Vector3(0.046f, 0.026f, 0.160f), liningMat);
            Block(root, "GripTape", new Vector3(0.174f, 0.028f, 0f), new Vector3(0.012f, 0.006f, 0.150f), gripMat);
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Band("SweetheartEdge" + side, new Vector3(0.184f, 0f, s * 0.014f), new Vector3(0.202f, 0f, s * 0.092f),
                    0.016f, 0.026f, 0.046f, trimMat);
                Band("SweetheartStitch" + side, new Vector3(0.176f, 0f, s * 0.016f), new Vector3(0.192f, 0f, s * 0.086f),
                    0.005f, 0.036f, 0.006f, stitchMat);
            }
            Sphere(root, "SweetheartDip", new Vector3(0.178f, 0.024f, 0f), Vector3.zero, new Vector3(0.034f, 0.020f, 0.036f), trimMat);

            // Boning channels running the height of the bodice — the strapless dress holds itself up, so the
            // bones are its structure and its clearest tell against the strappy and sleeved dresses.
            const int bones = 6;
            for (int i = 0; i < bones; i++)
            {
                float t = i / (bones - 1f) * 2f - 1f;                    // −1 at one side of the bodice, +1 at the other
                Vector3 from = new Vector3(0.186f, 0f, t * 0.086f);      // where the bone meets the top edge
                Vector3 to = new Vector3(0.000f, 0f, t * 0.068f);        // where it runs out at the waist
                float dx = to.x - from.x, dz = to.z - from.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);

                var bone = Block(root, "Bone" + i, new Vector3(mid.x, 0.030f, mid.z), new Vector3(len, 0.008f, 0.014f), fabricHiMat);
                bone.localEulerAngles = new Vector3(0f, yaw, 0f);
                var channel = Block(root, "BoneStitch" + i, new Vector3(mid.x, 0.034f, mid.z + 0.011f), new Vector3(len * 0.94f, 0.005f, 0.005f), stitchMat);
                channel.localEulerAngles = new Vector3(0f, yaw, 0f);
                var hollow = Block(root, "BoneShade" + i, new Vector3(mid.x, 0.026f, mid.z - 0.013f), new Vector3(len * 0.90f, 0.006f, 0.009f), fabricDeepMat);
                hollow.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // Curved bust darts shaping the cups above the boning.
            for (int s = -1; s <= 1; s += 2)
            {
                Band("BustDart" + (s > 0 ? "L" : "R"), new Vector3(0.150f, 0f, s * 0.096f), new Vector3(0.116f, 0f, s * 0.048f),
                    0.006f, 0.036f, 0.006f, stitchMat);
                Sphere(root, "Cup" + (s > 0 ? "L" : "R"), new Vector3(0.128f, 0.026f, s * 0.058f), new Vector3(0f, s * 18f, 0f),
                    new Vector3(0.084f, 0.020f, 0.062f), fabricHiMat);
            }

            // ── Narrow satin ribbon at the waist, tied in a small flat bow off to one flank ───────────────
            Block(root, "Ribbon", new Vector3(-0.026f, 0.032f, 0f), new Vector3(0.026f, 0.008f, 0.176f), sheenMat);
            Block(root, "RibbonEdgeTop", new Vector3(-0.014f, 0.037f, 0f), new Vector3(0.005f, 0.005f, 0.170f), fabricHiMat);
            Block(root, "RibbonEdgeLow", new Vector3(-0.038f, 0.037f, 0f), new Vector3(0.005f, 0.005f, 0.170f), fabricHiMat);
            Sphere(root, "WaistShade", new Vector3(-0.048f, 0.022f, 0f), Vector3.zero, new Vector3(0.04f, 0.014f, 0.172f), fabricDeepMat);

            var bow = new PartNode("Bow");
            bow.SetParent(root, false);
            bow.localPosition = new Vector3(-0.028f, 0.038f, -0.092f);
            bow.localEulerAngles = new Vector3(0f, -34f, 0f);
            Sphere(bow, "LoopA", new Vector3(0f, 0f, 0.038f), new Vector3(0f, 24f, 0f), new Vector3(0.052f, 0.014f, 0.052f), sheenMat);
            Sphere(bow, "LoopB", new Vector3(0f, 0f, -0.038f), new Vector3(0f, -24f, 0f), new Vector3(0.052f, 0.014f, 0.052f), sheenMat);
            Block(bow, "Knot", Vector3.zero, new Vector3(0.024f, 0.016f, 0.020f), fabricHiMat);
            var tailA = Block(bow, "TailA", new Vector3(-0.046f, 0f, 0.024f), new Vector3(0.080f, 0.006f, 0.018f), sheenMat);
            tailA.localEulerAngles = new Vector3(0f, 26f, 0f);
            var tailB = Block(bow, "TailB", new Vector3(-0.052f, -0.002f, -0.020f), new Vector3(0.092f, 0.006f, 0.018f), fabricMat);
            tailB.localEulerAngles = new Vector3(0f, -18f, 0f);

            // ── Pencil skirt: it widens over the hip, then draws back in, so the hem is its narrowest point ─
            Sphere(root, "SkirtHip", new Vector3(-0.106f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.050f, 0.214f), fabricMat);
            Sphere(root, "SkirtThigh", new Vector3(-0.196f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.206f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.282f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.048f, 0.192f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.336f, -0.002f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.184f), fabricMat);

            // The skirt is fitted, not gathered, so it carries shaping seams and long satin highlights instead
            // of the pleats, flutes, drapes and tiers of the other dresses.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Band("SideSeam" + side, new Vector3(-0.070f, 0f, s * 0.100f), new Vector3(-0.340f, 0f, s * 0.086f),
                    0.007f, 0.028f, 0.006f, stitchMat);
                Band("HipDart" + side, new Vector3(-0.072f, 0f, s * 0.044f), new Vector3(-0.150f, 0f, s * 0.056f),
                    0.006f, 0.030f, 0.005f, stitchMat);
                Sphere(root, "Sheen" + side, new Vector3(-0.200f, 0.030f, s * 0.052f), new Vector3(0f, s * -4f, 0f),
                    new Vector3(0.240f, 0.012f, 0.030f), sheenMat);
                Sphere(root, "SkirtShade" + side, new Vector3(-0.230f, 0.024f, s * 0.096f), new Vector3(0f, s * -6f, 0f),
                    new Vector3(0.200f, 0.010f, 0.024f), fabricDeepMat);
            }

            // ── Concealed zip down the centre back, with the hook and eye that finishes it at the top edge ─
            Block(root, "ZipTape", new Vector3(0.040f, 0.030f, 0f), new Vector3(0.300f, 0.005f, 0.016f), fabricDeepMat);
            for (int i = 0; i < 13; i++)
            {
                Block(root, "ZipTooth" + i, new Vector3(0.172f - i * 0.022f, 0.034f, 0f), new Vector3(0.009f, 0.004f, 0.010f), metalMat);
            }
            Block(root, "ZipSlider", new Vector3(-0.114f, 0.036f, 0f), new Vector3(0.024f, 0.008f, 0.018f), metalMat);
            Block(root, "ZipPull", new Vector3(-0.140f, 0.036f, 0f), new Vector3(0.022f, 0.005f, 0.008f), metalMat);
            Rod(root, "Hook", new Vector3(0.190f, 0.034f, 0.012f), Vector3.zero, 0.010f, 0.005f, metalMat);
            Rod(root, "Eye", new Vector3(0.190f, 0.034f, -0.012f), Vector3.zero, 0.010f, 0.005f, metalMat);

            // ── Kick vent cut into the hem so a skirt this narrow can be walked in: the pale lining showing
            //    between two faced edges, one of them flipped open — and the spin's asymmetry into the bargain.
            Band("VentLining", new Vector3(-0.246f, 0f, 0.006f), new Vector3(-0.348f, 0f, 0.006f), 0.048f, 0.020f, 0.026f, liningMat);
            Band("VentEdgeNear", new Vector3(-0.244f, 0f, 0.030f), new Vector3(-0.350f, 0f, 0.030f), 0.014f, 0.030f, 0.024f, trimMat);
            Block(root, "VentTack", new Vector3(-0.242f, 0.036f, 0.006f), new Vector3(0.008f, 0.006f, 0.044f), stitchMat);

            var ventFlap = new PartNode("VentFlap");
            ventFlap.SetParent(root, false);
            ventFlap.localPosition = new Vector3(-0.300f, 0.028f, -0.028f);
            ventFlap.localEulerAngles = new Vector3(0f, -24f, 0f);
            Block(ventFlap, "FlapUnder", Vector3.zero, new Vector3(0.110f, 0.010f, 0.046f), liningMat);
            Block(ventFlap, "FlapFace", new Vector3(0f, 0.008f, -0.006f), new Vector3(0.096f, 0.008f, 0.034f), fabricMat);
            Block(ventFlap, "FlapEdge", new Vector3(0f, 0.012f, -0.024f), new Vector3(0.100f, 0.008f, 0.010f), trimMat);

            // ── Faced hem across the narrow end, with its blind stitch just above it ───────────────────────
            Block(root, "Hem", new Vector3(-0.362f, 0.004f, 0f), new Vector3(0.016f, 0.046f, 0.176f), trimMat);
            Block(root, "HemStitch", new Vector3(-0.348f, 0.026f, 0f), new Vector3(0.006f, 0.006f, 0.166f), stitchMat);

            // A couple of soft folds and shaded creases so the cloth looks laid down rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.148f, 0.030f, -0.062f), new Vector3(0f, -14f, 0f), new Vector3(0.15f, 0.014f, 0.038f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.268f, 0.024f, -0.070f), new Vector3(0f, 16f, 0f), new Vector3(0.12f, 0.010f, 0.026f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.062f, 0.026f, -0.086f), new Vector3(0f, -12f, 0f), new Vector3(0.11f, 0.010f, 0.026f), fabricDeepMat);

            // Woven care label sewn inside the back of the top edge, sitting off-centre.
            var strapless = new PartNode("Label");
            strapless.SetParent(root, false);
            strapless.localPosition = new Vector3(0.146f, 0.030f, 0.044f);
            strapless.localEulerAngles = new Vector3(0f, 18f, 0f);
            Block(strapless, "LabelTab", Vector3.zero, new Vector3(0.024f, 0.005f, 0.030f), labelMat);
            Block(strapless, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.013f, 0.004f, 0.018f), liningMat);

            return root;
        }

        /// <summary>
        /// The little black slip dress laid flat, face-up, neckline at +X and hem at −X. It is the strapless
        /// dress's twin in colour and nothing else: where that one is boned, zipped and cut as a pencil, this
        /// is an unstructured bias-cut slip that simply pulls on — a soft draped cowl neck instead of a
        /// sweetheart edge, a pair of fine spaghetti straps that cross behind the back and hang on adjuster
        /// rings, narrow lace edging at the neckline and hem, and a skirt that falls on the bias so it widens
        /// steadily to a slit hem. Its seams and sheen all run diagonally, which is the bias cut's clearest
        /// tell against every other dress in the set. The item's tint only warms the near-black cloth — the
        /// name is the colour here.
        /// </summary>
        private static PartNode BuildDressSmallBlackStraps(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            // Matched to its strapless twin: the catalogue tint is generic clothing grey, so the cloth is
            // taken almost all the way to black and the tint is left only as a faint cast in it.
            Color fabric = Color.Lerp(def.Tint, new Color(0.055f, 0.055f, 0.065f), 0.86f);
            Color fabricDeep = new Color(fabric.r * 0.55f, fabric.g * 0.55f, fabric.b * 0.55f);   // shaded folds and cowl hollows
            Color fabricHi = Color.Lerp(fabric, new Color(0.95f, 0.95f, 0.96f), 0.22f);           // raised drape crowns catching light
            Color sheen = Color.Lerp(fabric, new Color(0.96f, 0.96f, 0.97f), 0.40f);              // long bias satin highlights — slip satin is glossier than the crepe twin
            Color trim = Color.Lerp(fabric, new Color(0.92f, 0.92f, 0.94f), 0.12f);               // bound edges and facings
            Color stitch = Color.Lerp(fabric, new Color(0.90f, 0.89f, 0.84f), 0.28f);             // fine tonal topstitching
            Color lace = Color.Lerp(fabric, new Color(0.86f, 0.85f, 0.88f), 0.34f);               // narrow lace edging at neck and hem
            Color lining = new Color(0.62f, 0.55f, 0.50f);                                       // pale lining, the only thing that shows inside a black dress
            Color metal = new Color(0.74f, 0.75f, 0.78f);                                        // strap adjuster rings
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.46f);   // liquid satin — glossier than the crepe sheath, short of the negligee
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.30f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.50f);
            PartMat sheenMat = PartMat.Of(sheen, smoothness: 0.62f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.42f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.16f);
            PartMat laceMat = PartMat.Of(lace, smoothness: 0.20f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.18f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // bias seams, the lace runs and the slit facings, so each run is one call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg, 0f);
            }

            // Lays one fine spaghetti strap between two points: a thin cord with a lighter edge along it.
            void Strap(string name, Vector3 from, Vector3 to, float y)
            {
                float sdx = to.x - from.x, sdz = to.z - from.z;
                float len = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                float yaw = Mathf.Atan2(-sdz, sdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);

                var cord = Block(root, name, mid, new Vector3(len, 0.008f, 0.011f), fabricMat);
                cord.localEulerAngles = new Vector3(0f, yaw, 0f);
                var gloss = Block(root, name + "Sheen", new Vector3(mid.x, y + 0.005f, mid.z),
                    new Vector3(len * 0.90f, 0.004f, 0.005f), sheenMat);
                gloss.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Body: an unstructured slip, softly shaped rather than nipped in — no boning anywhere ──────
            Sphere(root, "NeckDrape", new Vector3(0.158f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.048f, 0.184f), fabricMat);
            Sphere(root, "Bust", new Vector3(0.086f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.052f, 0.198f), fabricMat);
            Sphere(root, "Midriff", new Vector3(0.000f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.048f, 0.186f), fabricMat);

            // ── Cowl neckline: loose folds swagging across the front over the pale lining, with lace on top ─
            Sphere(root, "CowlHollow", new Vector3(0.164f, 0.012f, 0f), Vector3.zero, new Vector3(0.052f, 0.026f, 0.150f), liningMat);
            const int swags = 4;
            for (int i = 0; i < swags; i++)
            {
                float t = i / (swags - 1f);                       // 0 at the top edge, 1 down into the bust
                float x = 0.186f - t * 0.056f;
                float halfZ = 0.062f + t * 0.026f;                // each swag hangs a little wider than the one above
                Sphere(root, "CowlFold" + i, new Vector3(x, 0.030f - t * 0.004f, 0f),
                    new Vector3(0f, (i % 2 == 0) ? 6f : -6f, 0f), new Vector3(0.030f, 0.014f, halfZ * 2f), fabricHiMat);
                Sphere(root, "CowlShade" + i, new Vector3(x - 0.014f, 0.022f, 0f), Vector3.zero,
                    new Vector3(0.018f, 0.010f, halfZ * 1.8f), fabricDeepMat);
            }

            // Narrow lace edging bound along the cowl's top edge, scalloped by a row of small picots.
            Block(root, "NeckLace", new Vector3(0.196f, 0.026f, 0f), new Vector3(0.014f, 0.008f, 0.150f), laceMat);
            for (int i = 0; i < 9; i++)
            {
                float z = (i / 8f * 2f - 1f) * 0.070f;
                Sphere(root, "NeckPicot" + i, new Vector3(0.204f, 0.026f, z), Vector3.zero,
                    new Vector3(0.014f, 0.008f, 0.014f), laceMat);
            }

            // ── Fine spaghetti straps: one reaching out over the shoulder, the other crossed behind it ────
            Strap("StrapFrontL", new Vector3(0.192f, 0f, 0.062f), new Vector3(0.276f, 0f, 0.108f), 0.032f);
            Strap("StrapBackL", new Vector3(0.276f, 0f, 0.108f), new Vector3(0.216f, 0f, -0.044f), 0.034f);
            Strap("StrapFrontR", new Vector3(0.192f, 0f, -0.062f), new Vector3(0.272f, 0f, -0.106f), 0.032f);
            Strap("StrapBackR", new Vector3(0.272f, 0f, -0.106f), new Vector3(0.214f, 0f, 0.042f), 0.036f);   // laid over the other where they cross
            Sphere(root, "StrapCross", new Vector3(0.234f, 0.040f, -0.001f), new Vector3(0f, 24f, 0f),
                new Vector3(0.038f, 0.014f, 0.026f), fabricHiMat);

            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Block(root, "StrapTack" + side, new Vector3(0.194f, 0.036f, s * 0.062f),
                    new Vector3(0.010f, 0.006f, 0.016f), stitchMat);
                // Flat adjuster ring lying on the cloth, with a dark bore standing in for the hole the strap
                // threads through — the slip's only hardware, in place of the twin's zip.
                Rod(root, "StrapRing" + side, new Vector3(0.258f, 0.036f, s * 0.094f), Vector3.zero, 0.026f, 0.006f, metalMat);
                Rod(root, "StrapRingBore" + side, new Vector3(0.258f, 0.040f, s * 0.094f), Vector3.zero, 0.014f, 0.005f, fabricDeepMat);
            }

            // ── Bias-cut skirt: it keeps widening all the way down, the reverse of its pencil-cut twin ────
            Sphere(root, "SkirtHip", new Vector3(-0.100f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.050f, 0.212f), fabricMat);
            Sphere(root, "SkirtThigh", new Vector3(-0.196f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.252f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.288f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.048f, 0.288f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.352f, -0.002f, 0f), Vector3.zero, new Vector3(0.11f, 0.046f, 0.300f), fabricMat);

            // Bias seams and satin highlights all run on the diagonal — the cut's signature. There is no zip
            // and no vent: a bias slip has enough give to be pulled on, so the only opening is the hem slit.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Band("BiasSeam" + side, new Vector3(-0.048f, 0f, s * 0.022f), new Vector3(-0.340f, 0f, s * 0.128f),
                    0.006f, 0.030f, 0.005f, stitchMat);
                Band("BiasSheen" + side, new Vector3(-0.086f, 0f, s * 0.104f), new Vector3(-0.318f, 0f, s * 0.036f),
                    0.026f, 0.032f, 0.010f, sheenMat);
                Band("BiasShade" + side, new Vector3(-0.124f, 0f, s * 0.150f), new Vector3(-0.316f, 0f, s * 0.104f),
                    0.020f, 0.024f, 0.008f, fabricDeepMat);
            }

            // The waist is only a soft seam here rather than the twin's ribbon and bow.
            Band("WaistSeam", new Vector3(-0.048f, 0f, 0.098f), new Vector3(-0.048f, 0f, -0.098f), 0.006f, 0.030f, 0.005f, stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.062f, 0.022f, 0f), Vector3.zero, new Vector3(0.036f, 0.014f, 0.180f), fabricDeepMat);

            // ── High slit up one side of the skirt: the pale lining showing between two faced edges, with the
            //    upper edge fallen open across the cloth — and the spin's asymmetry into the bargain.
            Band("SlitLining", new Vector3(-0.190f, 0f, 0.116f), new Vector3(-0.344f, 0f, 0.152f), 0.046f, 0.020f, 0.024f, liningMat);
            Band("SlitFacing", new Vector3(-0.188f, 0f, 0.142f), new Vector3(-0.346f, 0f, 0.178f), 0.014f, 0.030f, 0.022f, trimMat);
            Block(root, "SlitTack", new Vector3(-0.184f, 0.036f, 0.120f), new Vector3(0.008f, 0.006f, 0.040f), stitchMat);

            var slitFlap = new PartNode("SlitFlap");
            slitFlap.SetParent(root, false);
            slitFlap.localPosition = new Vector3(-0.278f, 0.030f, 0.078f);
            slitFlap.localEulerAngles = new Vector3(0f, 32f, 0f);
            Sphere(slitFlap, "FlapUnder", Vector3.zero, Vector3.zero, new Vector3(0.130f, 0.016f, 0.070f), liningMat);
            Sphere(slitFlap, "FlapFace", new Vector3(-0.010f, 0.010f, -0.008f), Vector3.zero, new Vector3(0.104f, 0.014f, 0.054f), fabricMat);
            Block(slitFlap, "FlapEdge", new Vector3(0f, 0.012f, -0.030f), new Vector3(0.100f, 0.010f, 0.010f), trimMat);

            // ── Lace-edged hem across the wide end, picots hanging past it ─────────────────────────────────
            Block(root, "Hem", new Vector3(-0.378f, 0.004f, 0f), new Vector3(0.014f, 0.046f, 0.286f), trimMat);
            Block(root, "HemLace", new Vector3(-0.390f, 0.020f, 0f), new Vector3(0.016f, 0.010f, 0.278f), laceMat);
            for (int i = 0; i < 11; i++)
            {
                float z = (i / 10f * 2f - 1f) * 0.132f;
                Sphere(root, "HemPicot" + i, new Vector3(-0.400f, 0.018f, z), Vector3.zero,
                    new Vector3(0.016f, 0.010f, 0.016f), laceMat);
            }
            Block(root, "HemStitch", new Vector3(-0.364f, 0.026f, 0f), new Vector3(0.005f, 0.006f, 0.272f), stitchMat);

            // Slithery bias folds and shaded creases, so the satin looks poured down rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.166f, 0.030f, -0.086f), new Vector3(0f, 22f, 0f), new Vector3(0.17f, 0.014f, 0.040f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.052f, 0.030f, 0.066f), new Vector3(0f, -18f, 0f), new Vector3(0.13f, 0.014f, 0.034f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.286f, 0.024f, -0.108f), new Vector3(0f, -24f, 0f), new Vector3(0.14f, 0.010f, 0.028f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(0.048f, 0.026f, -0.094f), new Vector3(0f, 14f, 0f), new Vector3(0.11f, 0.010f, 0.026f), fabricDeepMat);

            // Woven care label sewn inside the back of the neckline, sitting off-centre.
            var straps = new PartNode("Label");
            straps.SetParent(root, false);
            straps.localPosition = new Vector3(0.140f, 0.030f, 0.046f);
            straps.localEulerAngles = new Vector3(0f, -22f, 0f);
            Block(straps, "LabelTab", Vector3.zero, new Vector3(0.024f, 0.005f, 0.030f), labelMat);
            Block(straps, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.013f, 0.004f, 0.018f), liningMat);

            return root;
        }

        /// <summary>
        /// The little strapless sundress laid flat, face-up, top edge at +X and hem at −X. It is the plain
        /// cotton answer to the black cocktail pair: same bare top, none of the evening tailoring. A straight
        /// bandeau finished with a wide fold-over frill rather than a boned sweetheart curve, an elasticated
        /// back panel instead of boning, a gathered dirndl waist with no belt, bag pockets dropped into both
        /// side seams, a short zip at one side seam rather than down the centre back, and a full gathered
        /// skirt with a deep cuff turned up on the outside of the hem — one corner of which has flopped back
        /// down. It takes the item's tint at full strength, so unlike its black siblings this one is whatever
        /// colour the catalogue says.
        /// </summary>
        private static PartNode BuildDressSmallStrapless(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // plain cotton sundress cloth, tint straight through
            Color fabricDeep = new Color(fabric.r * 0.76f, fabric.g * 0.76f, fabric.b * 0.76f);  // shaded gathers and seam hollows
            Color fabricHi = Color.Lerp(fabric, new Color(0.96f, 0.96f, 0.93f), 0.22f);          // raised gather crowns catching light
            Color reverse = Color.Lerp(fabric, new Color(0.90f, 0.90f, 0.86f), 0.42f);           // washed-out back of the cloth, seen on the turned cuff
            Color trim = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.90f), 0.30f);              // bound edges and pocket facings
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.50f);            // contrast topstitching
            Color elastic = new Color(fabric.r * 0.62f, fabric.g * 0.62f, fabric.b * 0.62f);     // elastic threads across the back panel
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.44f, fabric.b * 0.44f);      // shaded inside, seen at the top edge and pocket mouths
            Color loop = Color.Lerp(fabric, new Color(0.95f, 0.94f, 0.90f), 0.55f);              // pale ribbon hanging loops
            Color metal = new Color(0.76f, 0.77f, 0.80f);                                        // side zip
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.12f);   // flat cotton — the least shine of any of the small dresses
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.10f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.14f);
            PartMat reverseMat = PartMat.Of(reverse, smoothness: 0.10f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.14f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat elasticMat = PartMat.Of(elastic, smoothness: 0.20f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat loopMat = PartMat.Of(loop, smoothness: 0.22f);
            PartMat metalMat = PartMat.Of(metal, smoothness: 0.55f, metallic: 0.85f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // side seams, pocket bags and cuff edges, so each run is one call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg, 0f);
            }

            // ── Bandeau: a straight tube of a top, barely shaped, with no bones holding it up ──────────────
            Sphere(root, "BandeauTop", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.048f, 0.196f), fabricMat);
            Sphere(root, "Bandeau", new Vector3(0.084f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.050f, 0.198f), fabricMat);
            Sphere(root, "Midriff", new Vector3(0.012f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.048f, 0.190f), fabricMat);

            // Straight bound top edge, cut level right across instead of dipping and rising like a sweetheart.
            Block(root, "TopEdge", new Vector3(0.184f, 0.004f, 0f), new Vector3(0.013f, 0.046f, 0.184f), trimMat);
            Block(root, "TopEdgeStitch", new Vector3(0.172f, 0.026f, 0f), new Vector3(0.005f, 0.006f, 0.176f), stitchMat);
            Sphere(root, "TopHollow", new Vector3(0.164f, 0.014f, 0f), Vector3.zero, new Vector3(0.040f, 0.028f, 0.164f), liningMat);

            // ── Fold-over frill: the top of the bandeau turned down on itself, showing the cloth's pale back
            //    and breaking the straight edge with a run of soft points.
            Block(root, "FrillFold", new Vector3(0.156f, 0.032f, 0f), new Vector3(0.052f, 0.012f, 0.184f), reverseMat);
            Block(root, "FrillCrease", new Vector3(0.182f, 0.038f, 0f), new Vector3(0.008f, 0.008f, 0.180f), fabricHiMat);
            Block(root, "FrillStitch", new Vector3(0.130f, 0.038f, 0f), new Vector3(0.005f, 0.006f, 0.172f), stitchMat);
            const int frills = 9;
            for (int i = 0; i < frills; i++)
            {
                float z = (i / (frills - 1f) * 2f - 1f) * 0.082f;
                Sphere(root, "FrillPoint" + i, new Vector3(0.126f, 0.036f, z), new Vector3(0f, (i % 2 == 0) ? 10f : -10f, 0f),
                    new Vector3(0.044f, 0.014f, 0.030f), (i % 2 == 0) ? reverseMat : fabricDeepMat);
            }

            // ── Elasticated back panel: rows of shirring gripping one half of the bandeau, which is what
            //    holds this dress up where the black one has boning. Only the back is elasticated, so the
            //    rows stop short of the centre instead of running the whole way across.
            const int rows = 5;
            for (int i = 0; i < rows; i++)
            {
                float x = 0.148f - i * 0.019f;
                Block(root, "ShirrRow" + i, new Vector3(x, 0.028f, -0.104f), new Vector3(0.008f, 0.006f, 0.086f), elasticMat);
                Block(root, "ShirrShade" + i, new Vector3(x - 0.008f, 0.024f, -0.104f), new Vector3(0.005f, 0.005f, 0.082f), fabricDeepMat);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = 0.146f - i * 0.014f;
                Sphere(root, "BackGather" + i, new Vector3(x, 0.032f, -0.104f), Vector3.zero,
                    new Vector3(0.014f, 0.010f, 0.088f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Gathered dirndl waist: no belt, sash or ribbon at all — just a seam with the skirt's fullness
            //    pinched into it, which is the plainest waist of the three small dresses.
            Band("WaistSeam", new Vector3(-0.038f, 0f, 0.096f), new Vector3(-0.038f, 0f, -0.096f), 0.007f, 0.030f, 0.006f, stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.052f, 0.022f, 0f), Vector3.zero, new Vector3(0.036f, 0.014f, 0.186f), fabricDeepMat);
            const int gathers = 13;
            for (int i = 0; i < gathers; i++)
            {
                float z = (i / (gathers - 1f) * 2f - 1f) * 0.090f;
                Sphere(root, "WaistGather" + i, new Vector3(-0.058f, 0.032f, z), Vector3.zero,
                    new Vector3(0.038f, 0.012f, 0.011f), (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Full gathered skirt: it opens out steadily and finishes at its widest, the sundress shape ──
            Sphere(root, "SkirtTop", new Vector3(-0.108f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.232f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.208f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.050f, 0.300f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.296f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.048f, 0.344f), fabricMat);
            Sphere(root, "SkirtHem", new Vector3(-0.352f, -0.002f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.352f), fabricMat);

            // Soft vertical gather lines falling out of the waist, fading away before they reach the hem.
            const int falls = 7;
            for (int i = 0; i < falls; i++)
            {
                float t = i / (falls - 1f) * 2f - 1f;
                Band("Fall" + i, new Vector3(-0.082f, 0f, t * 0.086f), new Vector3(-0.268f, 0f, t * 0.146f),
                    0.014f, 0.030f, 0.008f, (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // ── Bag pockets dropped into both side seams: a facing at the mouth with the shaded inside of the
            //    bag showing through it, and the bag itself lying under the skirt cloth.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Band("SideSeam" + side, new Vector3(-0.062f, 0f, s * 0.100f), new Vector3(-0.336f, 0f, s * 0.164f),
                    0.007f, 0.028f, 0.006f, stitchMat);

                Band("PocketMouth" + side, new Vector3(-0.096f, 0f, s * 0.108f), new Vector3(-0.176f, 0f, s * 0.130f),
                    0.020f, 0.030f, 0.012f, liningMat);
                Band("PocketFacing" + side, new Vector3(-0.094f, 0f, s * 0.122f), new Vector3(-0.178f, 0f, s * 0.144f),
                    0.012f, 0.034f, 0.010f, trimMat);
                Sphere(root, "PocketBag" + side, new Vector3(-0.150f, 0.020f, s * 0.096f), new Vector3(0f, s * -12f, 0f),
                    new Vector3(0.120f, 0.016f, 0.090f), fabricDeepMat);
                Block(root, "PocketTackTop" + side, new Vector3(-0.094f, 0.036f, s * 0.116f),
                    new Vector3(0.010f, 0.006f, 0.020f), stitchMat);
                Block(root, "PocketTackLow" + side, new Vector3(-0.178f, 0.036f, s * 0.136f),
                    new Vector3(0.010f, 0.006f, 0.020f), stitchMat);
            }

            // ── Short zip set into one side seam only, running up from the waist — no centre back zip here ─
            var sideZip = new PartNode("SideZip");
            sideZip.SetParent(root, false);
            sideZip.localPosition = new Vector3(0.062f, 0.032f, 0.098f);
            sideZip.localEulerAngles = new Vector3(0f, -8f, 0f);
            Block(sideZip, "Tape", Vector3.zero, new Vector3(0.180f, 0.005f, 0.016f), fabricDeepMat);
            for (int i = 0; i < 8; i++)
            {
                Block(sideZip, "Tooth" + i, new Vector3(0.076f - i * 0.021f, 0.004f, 0f), new Vector3(0.008f, 0.004f, 0.009f), metalMat);
            }
            Block(sideZip, "Slider", new Vector3(-0.082f, 0.006f, 0f), new Vector3(0.022f, 0.008f, 0.017f), metalMat);
            Block(sideZip, "Pull", new Vector3(-0.104f, 0.006f, 0f), new Vector3(0.020f, 0.005f, 0.008f), metalMat);

            // ── Deep cuff turned up on the outside of the hem, showing the cloth's pale back all round ─────
            Block(root, "HemCuff", new Vector3(-0.336f, 0.026f, 0f), new Vector3(0.048f, 0.012f, 0.336f), reverseMat);
            Block(root, "HemCuffEdge", new Vector3(-0.360f, 0.032f, 0f), new Vector3(0.010f, 0.008f, 0.330f), fabricHiMat);
            Block(root, "HemCuffStitch", new Vector3(-0.310f, 0.034f, 0f), new Vector3(0.005f, 0.006f, 0.322f), stitchMat);
            Block(root, "Hem", new Vector3(-0.376f, 0.004f, 0f), new Vector3(0.016f, 0.046f, 0.330f), trimMat);

            // One corner of that cuff has flopped back down, breaking the ring and giving the slow spin its
            // asymmetry — the plain-cotton counterpart to the black dresses' fallen vent and slit flaps.
            var cuffDrop = new PartNode("CuffDrop");
            cuffDrop.SetParent(root, false);
            cuffDrop.localPosition = new Vector3(-0.322f, 0.028f, -0.196f);
            cuffDrop.localEulerAngles = new Vector3(0f, 28f, 0f);
            Sphere(cuffDrop, "DropUnder", Vector3.zero, Vector3.zero, new Vector3(0.116f, 0.016f, 0.078f), fabricMat);
            Sphere(cuffDrop, "DropFace", new Vector3(-0.012f, 0.010f, 0.006f), Vector3.zero, new Vector3(0.090f, 0.014f, 0.060f), reverseMat);
            Block(cuffDrop, "DropEdge", new Vector3(-0.046f, 0.010f, 0f), new Vector3(0.014f, 0.014f, 0.062f), trimMat);
            Block(cuffDrop, "DropCrease", new Vector3(0.046f, 0.010f, 0f), new Vector3(0.008f, 0.010f, 0.058f), fabricDeepMat);

            // ── Pale ribbon hanging loops stitched inside the top edge, tumbled out where the dress lies ───
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var hang = new PartNode("HangLoop" + side);
                hang.SetParent(root, false);
                hang.localPosition = new Vector3(0.176f, 0.034f, s * 0.070f);
                hang.localEulerAngles = new Vector3(0f, s * 40f, 0f);
                Sphere(hang, "Loop", Vector3.zero, Vector3.zero, new Vector3(0.058f, 0.010f, 0.030f), loopMat);
                Sphere(hang, "LoopHole", new Vector3(0f, 0.004f, 0f), Vector3.zero, new Vector3(0.038f, 0.008f, 0.014f), fabricDeepMat);
                Block(hang, "LoopTack", new Vector3(-0.028f, 0.004f, 0f), new Vector3(0.010f, 0.006f, 0.016f), stitchMat);
            }

            // Soft folds and shaded creases so the cotton looks laid down rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.176f, 0.030f, 0.078f), new Vector3(0f, 26f, 0f), new Vector3(0.16f, 0.014f, 0.042f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.046f, 0.030f, -0.062f), new Vector3(0f, -16f, 0f), new Vector3(0.12f, 0.014f, 0.034f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.262f, 0.024f, 0.126f), new Vector3(0f, 30f, 0f), new Vector3(0.14f, 0.010f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.124f, 0.026f, -0.070f), new Vector3(0f, -20f, 0f), new Vector3(0.12f, 0.010f, 0.028f), fabricDeepMat);

            // Woven care label sewn inside the back of the bandeau, sitting off-centre.
            var sundress = new PartNode("Label");
            sundress.SetParent(root, false);
            sundress.localPosition = new Vector3(0.106f, 0.030f, 0.052f);
            sundress.localEulerAngles = new Vector3(0f, 16f, 0f);
            Block(sundress, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.032f), labelMat);
            Block(sundress, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>
        /// The little strappy wrap dress laid flat, face-up, neckline at +X and hem at −X. It is the only
        /// wrapped garment in the set: the whole front crosses over itself on the diagonal, so one front edge
        /// lies across the other from the shoulder down past the hip, the underlap shows along it, and the
        /// hem finishes on a slant rather than square. Its straps are wide self-fabric ties knotted into a
        /// small bow on each shoulder — no sliders, rings or elastic anywhere — and it closes with a thin
        /// waist tie passed through a slot in the side seam. Takes the item's tint at full strength.
        /// </summary>
        private static PartNode BuildDressSmallStraps(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // light sundress cloth, tint straight through
            Color fabricDeep = new Color(fabric.r * 0.76f, fabric.g * 0.76f, fabric.b * 0.76f);  // shade under the crossed-over front
            Color fabricHi = Color.Lerp(fabric, new Color(0.96f, 0.96f, 0.93f), 0.20f);          // the overlap's raised face catching light
            Color underlap = new Color(fabric.r * 0.86f, fabric.g * 0.86f, fabric.b * 0.86f);    // the front panel lying underneath
            Color trim = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.90f), 0.28f);              // bound wrap edges and hem facing
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.50f);            // contrast topstitching
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.44f, fabric.b * 0.44f);      // shaded inside, seen at the neck and the tie slot
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.18f);   // soft viscose — a touch more fluid than the cotton sundress
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.14f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.22f);
            PartMat underlapMat = PartMat.Of(underlap, smoothness: 0.16f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.18f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // wrap edges, the slant hem and the side seams, so each run is one call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg, 0f);
            }

            // Lays one wide self-fabric tie between two points: a flat strip with a lighter edge along it.
            void Tie(string name, Vector3 from, Vector3 to, float width, float y, PartMat mat)
            {
                float tdx = to.x - from.x, tdz = to.z - from.z;
                float len = Mathf.Sqrt(tdx * tdx + tdz * tdz);
                float yaw = Mathf.Atan2(-tdz, tdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);

                var strip = Block(root, name, mid, new Vector3(len, 0.008f, width), mat);
                strip.localEulerAngles = new Vector3(0f, yaw, 0f);
                var edge = Block(root, name + "Edge", new Vector3(mid.x, y + 0.005f, mid.z),
                    new Vector3(len * 0.92f, 0.004f, width * 0.34f), trimMat);
                edge.localEulerAngles = new Vector3(0f, yaw, 0f);
            }

            // ── Body: a soft unstructured bodice, the two front panels crossed one over the other ─────────
            Sphere(root, "BodiceTop", new Vector3(0.150f, 0f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.184f), fabricMat);
            Sphere(root, "Bodice", new Vector3(0.076f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.200f), fabricMat);
            Sphere(root, "Waist", new Vector3(-0.006f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.048f, 0.184f), fabricMat);

            // ── The wrap: the underlapping panel first, then the overlapping one laid across it, so the
            //    front reads as two panels crossing on the diagonal rather than one closed front.
            Sphere(root, "UnderPanel", new Vector3(0.068f, 0.022f, -0.056f), new Vector3(0f, -26f, 0f),
                new Vector3(0.230f, 0.014f, 0.150f), underlapMat);
            Band("UnderEdge", new Vector3(0.176f, 0f, -0.020f), new Vector3(-0.086f, 0f, -0.130f), 0.014f, 0.030f, 0.014f, trimMat);

            Sphere(root, "OverPanel", new Vector3(0.070f, 0.032f, 0.052f), new Vector3(0f, 24f, 0f),
                new Vector3(0.240f, 0.016f, 0.156f), fabricHiMat);
            Band("OverEdge", new Vector3(0.182f, 0f, 0.014f), new Vector3(-0.104f, 0f, -0.086f), 0.016f, 0.042f, 0.016f, trimMat);
            Band("OverStitch", new Vector3(0.174f, 0f, 0.030f), new Vector3(-0.098f, 0f, -0.068f), 0.005f, 0.052f, 0.006f, stitchMat);
            Band("WrapShade", new Vector3(0.166f, 0f, -0.004f), new Vector3(-0.112f, 0f, -0.104f), 0.030f, 0.026f, 0.010f, fabricDeepMat);

            // Gathers pinched into the overlap where it is drawn across to the far side seam.
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                Sphere(root, "WrapGather" + i, new Vector3(-0.020f - t * 0.052f, 0.040f, -0.024f - t * 0.048f),
                    new Vector3(0f, -42f, 0f), new Vector3(0.016f, 0.012f, 0.062f),
                    (i % 2 == 0) ? fabricHiMat : fabricDeepMat);
            }

            // The V the two panels leave at the neck, with the shaded inside showing down inside it.
            Sphere(root, "NeckV", new Vector3(0.174f, 0.014f, -0.004f), new Vector3(0f, 12f, 0f),
                new Vector3(0.058f, 0.026f, 0.086f), liningMat);
            Band("NeckBindL", new Vector3(0.196f, 0f, 0.030f), new Vector3(0.148f, 0f, -0.060f), 0.012f, 0.032f, 0.026f, trimMat);
            Band("NeckBindR", new Vector3(0.190f, 0f, -0.038f), new Vector3(0.150f, 0f, 0.046f), 0.012f, 0.026f, 0.026f, trimMat);

            // ── Wide self-fabric shoulder ties, knotted into a small bow on each shoulder ─────────────────
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                float lean = s > 0 ? 1f : 0.88f;   // the two shoulders fall slightly differently where the dress lies

                Tie("ShoulderTie" + side, new Vector3(0.190f, 0f, s * 0.070f), new Vector3(0.262f * lean, 0f, s * 0.116f),
                    0.026f, 0.030f, fabricMat);
                Block(root, "TieTack" + side, new Vector3(0.192f, 0.036f, s * 0.070f),
                    new Vector3(0.012f, 0.006f, 0.026f), stitchMat);

                var knot = new PartNode("ShoulderBow" + side);
                knot.SetParent(root, false);
                knot.localPosition = new Vector3(0.268f * lean, 0.038f, s * 0.124f);
                knot.localEulerAngles = new Vector3(0f, s * -34f, 0f);
                Sphere(knot, "LoopA", new Vector3(0f, 0f, 0.030f), new Vector3(0f, 22f, 0f), new Vector3(0.046f, 0.014f, 0.044f), fabricMat);
                Sphere(knot, "LoopB", new Vector3(0f, 0f, -0.030f), new Vector3(0f, -22f, 0f), new Vector3(0.046f, 0.014f, 0.044f), fabricMat);
                Block(knot, "Knot", Vector3.zero, new Vector3(0.022f, 0.016f, 0.018f), fabricHiMat);
                var tail = Block(knot, "Tail", new Vector3(-0.040f, 0f, 0.018f), new Vector3(0.068f, 0.006f, 0.016f), fabricMat);
                tail.localEulerAngles = new Vector3(0f, 24f, 0f);
                Block(knot, "TailTip", new Vector3(-0.076f, -0.002f, 0.030f), new Vector3(0.020f, 0.006f, 0.018f), fabricDeepMat);
            }

            // ── Waist tie: a thin belt run round the body and passed out through a slot in the side seam,
            //    tied in a plain flat knot on the hip — no buckle, no bow, no elastic casing.
            Band("WaistSeam", new Vector3(-0.040f, 0f, 0.094f), new Vector3(-0.040f, 0f, -0.094f), 0.006f, 0.030f, 0.006f, stitchMat);
            Tie("WaistTie", new Vector3(-0.042f, 0f, 0.096f), new Vector3(-0.042f, 0f, -0.096f), 0.018f, 0.036f, fabricMat);
            Block(root, "TieSlot", new Vector3(-0.042f, 0.040f, -0.100f), new Vector3(0.022f, 0.008f, 0.014f), liningMat);

            var hipKnot = new PartNode("HipKnot");
            hipKnot.SetParent(root, false);
            hipKnot.localPosition = new Vector3(-0.058f, 0.044f, -0.122f);
            hipKnot.localEulerAngles = new Vector3(0f, -28f, 0f);
            Block(hipKnot, "Knot", Vector3.zero, new Vector3(0.030f, 0.014f, 0.024f), fabricHiMat);
            var tieNear = Block(hipKnot, "TieNear", new Vector3(-0.056f, -0.002f, 0.016f), new Vector3(0.090f, 0.008f, 0.018f), fabricMat);
            tieNear.localEulerAngles = new Vector3(0f, 22f, 0f);
            var tieFar = Block(hipKnot, "TieFar", new Vector3(-0.050f, -0.002f, -0.020f), new Vector3(0.076f, 0.008f, 0.017f), fabricMat);
            tieFar.localEulerAngles = new Vector3(0f, -16f, 0f);
            Block(hipKnot, "TieTip", new Vector3(-0.102f, -0.003f, 0.028f), new Vector3(0.024f, 0.007f, 0.020f), fabricDeepMat);

            // ── Wrap skirt: it carries the crossover down past the hip, so the two panels stay separate all
            //    the way to the hem and the whole skirt swings open along one diagonal edge.
            Sphere(root, "SkirtHip", new Vector3(-0.106f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.222f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.204f, 0f, 0f), Vector3.zero, new Vector3(0.17f, 0.050f, 0.284f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.292f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.048f, 0.320f), fabricMat);

            Sphere(root, "SkirtUnder", new Vector3(-0.208f, 0.020f, -0.086f), new Vector3(0f, -14f, 0f),
                new Vector3(0.280f, 0.014f, 0.170f), underlapMat);
            Sphere(root, "SkirtOver", new Vector3(-0.196f, 0.032f, 0.072f), new Vector3(0f, 12f, 0f),
                new Vector3(0.300f, 0.016f, 0.184f), fabricHiMat);

            // The overlap's long front edge, running from the waist knot right down to the hem.
            Band("WrapEdge", new Vector3(-0.086f, 0f, -0.100f), new Vector3(-0.330f, 0f, -0.034f), 0.016f, 0.042f, 0.016f, trimMat);
            Band("WrapEdgeStitch", new Vector3(-0.092f, 0f, -0.082f), new Vector3(-0.326f, 0f, -0.018f), 0.005f, 0.052f, 0.006f, stitchMat);
            Band("WrapEdgeShade", new Vector3(-0.100f, 0f, -0.126f), new Vector3(-0.336f, 0f, -0.060f), 0.028f, 0.026f, 0.010f, fabricDeepMat);

            for (int s = -1; s <= 1; s += 2)
            {
                Band("SideSeam" + (s > 0 ? "L" : "R"), new Vector3(-0.058f, 0f, s * 0.098f), new Vector3(-0.312f, 0f, s * 0.150f),
                    0.007f, 0.028f, 0.006f, stitchMat);
            }

            // ── Slant hem: the wrap makes the two panels finish at different heights, so the hem runs on a
            //    diagonal across the dress instead of straight across like every sibling's.
            Band("HemFacing", new Vector3(-0.372f, 0f, 0.120f), new Vector3(-0.288f, 0f, -0.176f), 0.020f, 0.008f, 0.046f, trimMat);
            Band("HemStitch", new Vector3(-0.356f, 0f, 0.116f), new Vector3(-0.276f, 0f, -0.168f), 0.005f, 0.030f, 0.006f, stitchMat);
            Sphere(root, "HemCornerLow", new Vector3(-0.378f, 0f, 0.116f), new Vector3(0f, 18f, 0f),
                new Vector3(0.070f, 0.046f, 0.110f), fabricMat);
            Sphere(root, "HemCornerHigh", new Vector3(-0.292f, 0f, -0.178f), new Vector3(0f, -22f, 0f),
                new Vector3(0.070f, 0.046f, 0.096f), fabricMat);

            // The high corner of the wrap has fallen open where the dress lies, showing the panel beneath it —
            // which is what gives the slow spin its asymmetry.
            var wrapFall = new PartNode("WrapFall");
            wrapFall.SetParent(root, false);
            wrapFall.localPosition = new Vector3(-0.286f, 0.028f, -0.146f);
            wrapFall.localEulerAngles = new Vector3(0f, -36f, 0f);
            Sphere(wrapFall, "FallUnder", Vector3.zero, Vector3.zero, new Vector3(0.126f, 0.016f, 0.082f), underlapMat);
            Sphere(wrapFall, "FallFace", new Vector3(-0.012f, 0.010f, 0.008f), Vector3.zero, new Vector3(0.098f, 0.014f, 0.062f), fabricMat);
            Block(wrapFall, "FallEdge", new Vector3(0.050f, 0.010f, 0f), new Vector3(0.016f, 0.014f, 0.066f), trimMat);
            Block(wrapFall, "FallCrease", new Vector3(-0.050f, 0.010f, 0f), new Vector3(0.008f, 0.010f, 0.060f), fabricDeepMat);

            // Soft folds and shaded creases so the cloth looks laid down rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.164f, 0.036f, 0.122f), new Vector3(0f, 30f, 0f), new Vector3(0.16f, 0.014f, 0.040f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(0.038f, 0.038f, 0.086f), new Vector3(0f, 20f, 0f), new Vector3(0.12f, 0.014f, 0.032f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.248f, 0.024f, 0.152f), new Vector3(0f, 26f, 0f), new Vector3(0.13f, 0.010f, 0.028f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.140f, 0.026f, -0.146f), new Vector3(0f, -30f, 0f), new Vector3(0.12f, 0.010f, 0.026f), fabricDeepMat);

            // Woven care label sewn inside the back neck, sitting off-centre.
            var wrapDress = new PartNode("Label");
            wrapDress.SetParent(root, false);
            wrapDress.localPosition = new Vector3(0.126f, 0.030f, 0.070f);
            wrapDress.localEulerAngles = new Vector3(0f, -20f, 0f);
            Block(wrapDress, "LabelTab", Vector3.zero, new Vector3(0.025f, 0.005f, 0.031f), labelMat);
            Block(wrapDress, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.013f, 0.004f, 0.019f), liningMat);

            return root;
        }

        /// <summary>
        /// A plain everyday strappy pinafore laid flat, face-up, top edge at +X and hem at −X. It is the
        /// workaday member of the strap family and shares nothing with the others' evening tricks: the top is
        /// a flat square bib with deep scooped armholes cut either side of it — no shirring, no boning, no
        /// bandeau — and it hangs from two wide straps that cross over each other and button onto that bib
        /// with real buttons through worked buttonholes, so there are no sliders, ties or elastic anywhere.
        /// One strap has been unbuttoned and left flopped out past the shoulder, its empty buttonhole showing
        /// and its button sitting bare on the bib. Below the waist seam it is a panelled A-line skirt cut in
        /// gores rather than gathered, tiered, pleated or wrapped, falling to mid-calf, with a patch pocket on
        /// the bib and one on each hip. It pulls on over the head, so unlike every sibling it carries no zip.
        /// Takes the item's tint as the dress cloth.
        /// </summary>
        private static PartNode BuildDressStraps(ItemDef def, PartNode parent, out float baseHeight)
        {
            // Hover high enough that the gentle bob never dips the flat garment through the floor.
            baseHeight = 0.13f;

            var root = new PartNode("Visual");
            root.SetParent(parent, false);

            Color fabric = def.Tint;                                                             // sturdy everyday dress cloth, tint straight through
            Color fabricDeep = new Color(fabric.r * 0.76f, fabric.g * 0.76f, fabric.b * 0.76f);  // shaded seam hollows and creases
            Color fabricHi = Color.Lerp(fabric, new Color(0.96f, 0.96f, 0.93f), 0.20f);          // raised folds and strap crowns catching light
            Color reverse = Color.Lerp(fabric, new Color(0.90f, 0.90f, 0.86f), 0.38f);           // washed-out back of the cloth, seen on the turned hem facing
            Color trim = Color.Lerp(fabric, new Color(0.94f, 0.93f, 0.90f), 0.26f);              // bound armholes, bib edge and pocket hems
            Color stitch = Color.Lerp(fabric, new Color(0.93f, 0.92f, 0.82f), 0.50f);            // contrast topstitching, heavy on a workaday dress
            Color lining = new Color(fabric.r * 0.44f, fabric.g * 0.44f, fabric.b * 0.44f);      // shaded inside, seen through the armholes and buttonholes
            Color button = new Color(0.86f, 0.83f, 0.74f);                                       // plain horn buttons
            Color buttonRim = new Color(0.68f, 0.65f, 0.57f);                                    // their shaded rims and drilled holes
            Color label = new Color(0.90f, 0.89f, 0.84f);                                        // woven care label

            PartMat fabricMat = PartMat.Of(fabric, smoothness: 0.14f);   // matte cotton drill — the plainest cloth of the strap family
            PartMat fabricDeepMat = PartMat.Of(fabricDeep, smoothness: 0.12f);
            PartMat fabricHiMat = PartMat.Of(fabricHi, smoothness: 0.16f);
            PartMat reverseMat = PartMat.Of(reverse, smoothness: 0.10f);
            PartMat trimMat = PartMat.Of(trim, smoothness: 0.16f);
            PartMat stitchMat = PartMat.Of(stitch, smoothness: 0.12f);
            PartMat liningMat = PartMat.Of(lining, smoothness: 0.10f);
            PartMat buttonMat = PartMat.Of(button, smoothness: 0.42f);
            PartMat buttonRimMat = PartMat.Of(buttonRim, smoothness: 0.30f);
            PartMat labelMat = PartMat.Of(label, smoothness: 0.12f);

            // Lays a flat band between two points on the garment, aimed from one to the other — used for the
            // gore seams, armhole binding and hem runs, so each line of the pattern is one call.
            void Band(string name, Vector3 from, Vector3 to, float width, float y, float thick, PartMat mat)
            {
                float bdx = to.x - from.x, bdz = to.z - from.z;
                float len = Mathf.Sqrt(bdx * bdx + bdz * bdz);
                var band = Block(root, name, new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f),
                    new Vector3(len, thick, width), mat);
                band.localEulerAngles = new Vector3(0f, Mathf.Atan2(-bdz, bdx) * Mathf.Rad2Deg, 0f);
            }

            // Lays one wide pinafore strap between two points: a flat strip with topstitching down both of
            // its long edges, which is how a strap of this kind is actually made up.
            void Strap(string name, Vector3 from, Vector3 to, float width, float y, PartMat mat)
            {
                float sdx = to.x - from.x, sdz = to.z - from.z;
                float len = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                float yaw = Mathf.Atan2(-sdz, sdx) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);
                float rad = yaw * Mathf.Deg2Rad;
                // Sideways step across the strap, so the two stitch lines sit on its edges rather than its centre.
                Vector3 side = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * (width * 0.32f);

                var strip = Block(root, name, mid, new Vector3(len, 0.009f, width), mat);
                strip.localEulerAngles = new Vector3(0f, yaw, 0f);
                for (int e = -1; e <= 1; e += 2)
                {
                    var edge = Block(root, name + (e > 0 ? "StitchL" : "StitchR"),
                        new Vector3(mid.x + side.x * e, y + 0.006f, mid.z + side.z * e),
                        new Vector3(len * 0.94f, 0.004f, width * 0.14f), stitchMat);
                    edge.localEulerAngles = new Vector3(0f, yaw, 0f);
                }
            }

            // Drops one plain horn button on the cloth: a shallow disc with a shaded rim and two drilled holes.
            void Button(string name, Vector3 at)
            {
                Rod(root, name, at, Vector3.zero, 0.030f, 0.006f, buttonMat);
                Rod(root, name + "Rim", new Vector3(at.x, at.y + 0.002f, at.z), Vector3.zero, 0.022f, 0.004f, buttonRimMat);
                Block(root, name + "HoleA", new Vector3(at.x, at.y + 0.005f, at.z - 0.006f), new Vector3(0.005f, 0.003f, 0.005f), liningMat);
                Block(root, name + "HoleB", new Vector3(at.x, at.y + 0.005f, at.z + 0.006f), new Vector3(0.005f, 0.003f, 0.005f), liningMat);
            }

            // ── Bib: a flat square-cut top, stiffened by its facing, with none of the shaping the party
            //    dresses carry — it is simply a rectangle of cloth standing up from the waist seam.
            Sphere(root, "BibTop", new Vector3(0.156f, 0f, 0f), Vector3.zero, new Vector3(0.09f, 0.046f, 0.168f), fabricMat);
            Sphere(root, "Bib", new Vector3(0.082f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.048f, 0.176f), fabricMat);
            Sphere(root, "Midriff", new Vector3(0.008f, 0f, 0f), Vector3.zero, new Vector3(0.12f, 0.048f, 0.192f), fabricMat);

            // Square bound top edge, cut dead level from corner to corner, with its facing stitched down
            // a finger's width below it.
            Block(root, "BibEdge", new Vector3(0.196f, 0.004f, 0f), new Vector3(0.013f, 0.046f, 0.152f), trimMat);
            Block(root, "BibEdgeStitch", new Vector3(0.184f, 0.028f, 0f), new Vector3(0.005f, 0.006f, 0.146f), stitchMat);
            Block(root, "BibFacingStitch", new Vector3(0.150f, 0.028f, 0f), new Vector3(0.005f, 0.006f, 0.150f), stitchMat);
            Sphere(root, "BibTopHollow", new Vector3(0.176f, 0.014f, 0f), Vector3.zero, new Vector3(0.032f, 0.026f, 0.132f), liningMat);

            // Square corners where the bib meets the strap ends, squared off rather than curved away.
            for (int s = -1; s <= 1; s += 2)
            {
                Block(root, "BibCorner" + (s > 0 ? "L" : "R"), new Vector3(0.184f, 0.006f, s * 0.066f),
                    new Vector3(0.030f, 0.044f, 0.024f), fabricMat);
            }

            // ── Deep scooped armholes cut either side, between the bib and the waist: the shaded inside of
            //    the dress shows through each scoop, and both are bound and topstitched all round.
            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                Sphere(root, "Armhole" + side, new Vector3(0.104f, 0.016f, s * 0.100f), new Vector3(0f, s * -18f, 0f),
                    new Vector3(0.130f, 0.026f, 0.056f), liningMat);
                Band("ArmBindUpper" + side, new Vector3(0.170f, 0f, s * 0.074f), new Vector3(0.096f, 0f, s * 0.116f),
                    0.014f, 0.030f, 0.026f, trimMat);
                Band("ArmBindLower" + side, new Vector3(0.096f, 0f, s * 0.116f), new Vector3(0.026f, 0f, s * 0.092f),
                    0.014f, 0.030f, 0.026f, trimMat);
                Band("ArmStitch" + side, new Vector3(0.164f, 0f, s * 0.062f), new Vector3(0.032f, 0f, s * 0.080f),
                    0.005f, 0.040f, 0.006f, stitchMat);
            }

            // ── Crossed straps: two wide bands running up out of the back waist and over each other, so the
            //    pair reads as a crossover back rather than two parallel shoulder straps.
            Strap("StrapA", new Vector3(0.020f, 0f, 0.108f), new Vector3(0.196f, 0f, -0.062f), 0.030f, 0.032f, fabricMat);
            Strap("StrapB", new Vector3(0.020f, 0f, -0.108f), new Vector3(0.198f, 0f, 0.058f), 0.030f, 0.044f, fabricMat);

            // Where the two cross, the upper one casts a shade on the lower and lifts slightly over it.
            Sphere(root, "StrapCrossShade", new Vector3(0.112f, 0.038f, 0.002f), new Vector3(0f, -38f, 0f),
                new Vector3(0.068f, 0.012f, 0.038f), fabricDeepMat);
            Sphere(root, "StrapCrossLift", new Vector3(0.116f, 0.052f, 0.004f), new Vector3(0f, 34f, 0f),
                new Vector3(0.084f, 0.012f, 0.034f), fabricHiMat);

            // Both straps are anchored into the back waist seam with a stitched box tack apiece.
            for (int s = -1; s <= 1; s += 2)
            {
                Block(root, "StrapTack" + (s > 0 ? "L" : "R"), new Vector3(0.022f, 0.040f, s * 0.108f),
                    new Vector3(0.024f, 0.006f, 0.030f), stitchMat);
            }

            // Strap A is done up: its worked buttonhole is pulled over the bib button, which sits proud in it.
            Block(root, "ButtonholeDone", new Vector3(0.196f, 0.050f, -0.062f), new Vector3(0.022f, 0.006f, 0.009f), liningMat);
            Button("ButtonDone", new Vector3(0.196f, 0.052f, -0.062f));

            // Strap B has been unbuttoned and flopped out past the shoulder, dragging its end across the
            // cloth — that loose strap is what gives the slow spin its asymmetry.
            Strap("StrapLoose", new Vector3(0.198f, 0f, 0.058f), new Vector3(0.286f, 0f, 0.146f), 0.030f, 0.040f, fabricMat);
            Sphere(root, "StrapLooseEnd", new Vector3(0.296f, 0.040f, 0.156f), new Vector3(0f, -44f, 0f),
                new Vector3(0.052f, 0.014f, 0.034f), fabricMat);
            Block(root, "ButtonholeOpen", new Vector3(0.292f, 0.050f, 0.152f), new Vector3(0.024f, 0.006f, 0.010f), liningMat);
            Sphere(root, "StrapLooseShade", new Vector3(0.262f, 0.030f, 0.126f), new Vector3(0f, -44f, 0f),
                new Vector3(0.090f, 0.010f, 0.040f), fabricDeepMat);

            // Its button is left standing bare on the bib with the empty buttonhole nowhere near it.
            Button("ButtonBare", new Vector3(0.196f, 0.032f, 0.062f));

            // ── Waist seam: a plain flat-felled joining seam, double-stitched, with no belt, sash or casing ─
            Band("WaistSeam", new Vector3(-0.028f, 0f, 0.100f), new Vector3(-0.028f, 0f, -0.100f), 0.008f, 0.030f, 0.007f, stitchMat);
            Band("WaistSeamSecond", new Vector3(-0.040f, 0f, 0.098f), new Vector3(-0.040f, 0f, -0.098f), 0.005f, 0.030f, 0.006f, stitchMat);
            Sphere(root, "WaistShade", new Vector3(-0.050f, 0.022f, 0f), Vector3.zero, new Vector3(0.030f, 0.014f, 0.192f), fabricDeepMat);

            // ── Panelled A-line skirt: cut in gores that widen steadily to mid-calf. No gathers at the waist,
            //    no tiers, no pleats — the flare comes from the shape of the panels themselves.
            Sphere(root, "SkirtHip", new Vector3(-0.106f, 0f, 0f), Vector3.zero, new Vector3(0.15f, 0.050f, 0.218f), fabricMat);
            Sphere(root, "SkirtMid", new Vector3(-0.216f, 0f, 0f), Vector3.zero, new Vector3(0.18f, 0.050f, 0.276f), fabricMat);
            Sphere(root, "SkirtLow", new Vector3(-0.320f, 0f, 0f), Vector3.zero, new Vector3(0.16f, 0.048f, 0.322f), fabricMat);
            Sphere(root, "SkirtHemFan", new Vector3(-0.386f, -0.002f, 0f), Vector3.zero, new Vector3(0.10f, 0.046f, 0.336f), fabricMat);

            // The gore seams themselves: each runs from the waist to the hem, splaying as the panels widen,
            // with a shaded hollow along one side and a stitch line down the other.
            float[] gore = { -0.062f, -0.020f, 0.020f, 0.062f };
            for (int g = 0; g < gore.Length; g++)
            {
                float topZ = gore[g] * 1.55f;
                float hemZ = gore[g] * 2.55f;
                Band("GoreSeam" + g, new Vector3(-0.052f, 0f, topZ), new Vector3(-0.404f, 0f, hemZ), 0.007f, 0.030f, 0.006f, stitchMat);
                Band("GoreHollow" + g, new Vector3(-0.060f, 0f, topZ - 0.012f), new Vector3(-0.400f, 0f, hemZ - 0.018f),
                    0.020f, 0.024f, 0.008f, fabricDeepMat);
                Band("GoreCrown" + g, new Vector3(-0.070f, 0f, topZ + 0.014f), new Vector3(-0.396f, 0f, hemZ + 0.020f),
                    0.024f, 0.028f, 0.008f, fabricHiMat);
            }

            // Side seams closing the outermost panels, running the whole length of the skirt.
            for (int s = -1; s <= 1; s += 2)
            {
                Band("SideSeam" + (s > 0 ? "L" : "R"), new Vector3(-0.046f, 0f, s * 0.100f), new Vector3(-0.400f, 0f, s * 0.164f),
                    0.007f, 0.028f, 0.006f, stitchMat);
            }

            // ── Patch pockets: one square on the bib, one on each hip, each a slab of cloth topstitched down
            //    round three sides with a hemmed mouth across the top.
            Block(root, "BibPocket", new Vector3(0.086f, 0.034f, 0f), new Vector3(0.078f, 0.008f, 0.076f), fabricHiMat);
            Block(root, "BibPocketHem", new Vector3(0.120f, 0.040f, 0f), new Vector3(0.014f, 0.008f, 0.076f), trimMat);
            Block(root, "BibPocketMouth", new Vector3(0.112f, 0.044f, 0f), new Vector3(0.005f, 0.006f, 0.070f), liningMat);
            Block(root, "BibPocketStitch", new Vector3(0.054f, 0.040f, 0f), new Vector3(0.005f, 0.006f, 0.072f), stitchMat);

            for (int s = -1; s <= 1; s += 2)
            {
                string side = s > 0 ? "L" : "R";
                var pocket = new PartNode("HipPocket" + side);
                pocket.SetParent(root, false);
                pocket.localPosition = new Vector3(-0.146f, 0.032f, s * 0.108f);
                pocket.localEulerAngles = new Vector3(0f, s * -8f, 0f);
                Block(pocket, "Patch", Vector3.zero, new Vector3(0.096f, 0.008f, 0.086f), fabricHiMat);
                Block(pocket, "Hem", new Vector3(0.042f, 0.006f, 0f), new Vector3(0.016f, 0.008f, 0.086f), trimMat);
                Block(pocket, "Mouth", new Vector3(0.034f, 0.010f, 0f), new Vector3(0.005f, 0.006f, 0.078f), liningMat);
                Block(pocket, "StitchLow", new Vector3(-0.046f, 0.006f, 0f), new Vector3(0.005f, 0.006f, 0.080f), stitchMat);
                Block(pocket, "TackTop", new Vector3(0.040f, 0.012f, 0.040f), new Vector3(0.014f, 0.006f, 0.010f), stitchMat);
                Block(pocket, "TackLow", new Vector3(0.040f, 0.012f, -0.040f), new Vector3(0.014f, 0.006f, 0.010f), stitchMat);
            }

            // ── Deep turned hem at mid-calf, showing the pale back of the cloth, held by two rows of stitching
            //    — the sturdy machine finish of a working dress rather than a cuff, frill or facing tape.
            Block(root, "HemTurn", new Vector3(-0.388f, 0.026f, 0f), new Vector3(0.044f, 0.012f, 0.324f), reverseMat);
            Block(root, "HemTurnEdge", new Vector3(-0.408f, 0.032f, 0f), new Vector3(0.010f, 0.008f, 0.318f), fabricHiMat);
            Block(root, "HemStitchOuter", new Vector3(-0.400f, 0.034f, 0f), new Vector3(0.005f, 0.006f, 0.312f), stitchMat);
            Block(root, "HemStitchInner", new Vector3(-0.366f, 0.034f, 0f), new Vector3(0.005f, 0.006f, 0.316f), stitchMat);
            Block(root, "Hem", new Vector3(-0.424f, 0.004f, 0f), new Vector3(0.016f, 0.046f, 0.318f), trimMat);

            // Soft folds and shaded creases so the drill looks laid down rather than pressed flat.
            Sphere(root, "FoldA", new Vector3(-0.256f, 0.030f, -0.108f), new Vector3(0f, -22f, 0f), new Vector3(0.17f, 0.014f, 0.044f), fabricHiMat);
            Sphere(root, "FoldB", new Vector3(-0.078f, 0.030f, 0.086f), new Vector3(0f, 18f, 0f), new Vector3(0.13f, 0.014f, 0.036f), fabricHiMat);
            Sphere(root, "CreaseA", new Vector3(-0.336f, 0.024f, 0.130f), new Vector3(0f, 28f, 0f), new Vector3(0.15f, 0.010f, 0.030f), fabricDeepMat);
            Sphere(root, "CreaseB", new Vector3(-0.170f, 0.026f, -0.062f), new Vector3(0f, -18f, 0f), new Vector3(0.12f, 0.010f, 0.028f), fabricDeepMat);

            // Woven care label sewn inside the back of the bib, sitting off-centre.
            var pinafore = new PartNode("Label");
            pinafore.SetParent(root, false);
            pinafore.localPosition = new Vector3(0.150f, 0.030f, -0.052f);
            pinafore.localEulerAngles = new Vector3(0f, -14f, 0f);
            Block(pinafore, "LabelTab", Vector3.zero, new Vector3(0.026f, 0.005f, 0.032f), labelMat);
            Block(pinafore, "LabelInk", new Vector3(0f, 0.003f, 0f), new Vector3(0.014f, 0.004f, 0.020f), liningMat);

            return root;
        }

        /// <summary>Builds one cylindrical dry cell under <paramref name="parent"/> in its own yaw frame,
        /// lying on its side along the frame's local X: a steel casing with a dark jacket and gold band over
        /// the middle, a bright rolled crimp at each end, a raised copper positive terminal at +X and a flat
        /// negative cap at −X, with printed + and − marks on top. Yaw lets callers flip a cell end-for-end.</summary>
        private static void BuildCell(PartNode parent, Vector3 center, float yawDeg,
            PartMat wrapMat, PartMat wrapDeepMat, PartMat bandMat, PartMat steelMat, PartMat steelHiMat,
            PartMat capMat, PartMat capHiMat, PartMat markMat)
        {
            var cell = new PartNode("Cell");
            cell.SetParent(parent, false);
            cell.localPosition = center;
            cell.localEulerAngles = new Vector3(0f, yawDeg, 0f);

            const float cellD = 0.13f;                // cell diameter
            const float r = cellD * 0.5f;             // radius, for placing the polarity marks on the top surface
            Vector3 layX = new Vector3(0f, 0f, 90f);  // lays a Rod's length along the cell's local X axis

            // Steel casing runs the full length; the dark jacket wraps its middle so bare metal shows only at the ends.
            Rod(cell, "Casing", Vector3.zero, layX, cellD, 0.34f, steelMat);
            Rod(cell, "Jacket", new Vector3(-0.005f, 0f, 0f), layX, cellD + 0.004f, 0.28f, wrapMat);

            // Bright gold label band near the positive end and a shaded trim line near the negative end.
            Rod(cell, "Band", new Vector3(0.075f, 0f, 0f), layX, cellD + 0.008f, 0.05f, bandMat);
            Rod(cell, "Trim", new Vector3(-0.10f, 0f, 0f), layX, cellD + 0.006f, 0.012f, wrapDeepMat);

            // Negative end: a bright rolled crimp and a flat, slightly inset steel terminal.
            Rod(cell, "NegCrimp", new Vector3(-0.164f, 0f, 0f), layX, cellD, 0.012f, steelHiMat);
            Rod(cell, "NegCap", new Vector3(-0.172f, 0f, 0f), layX, cellD - 0.02f, 0.010f, steelMat);

            // Positive end: a bright rolled crimp, then a raised copper terminal button with a bright contact tip.
            Rod(cell, "PosCrimp", new Vector3(0.164f, 0f, 0f), layX, cellD, 0.012f, steelHiMat);
            Rod(cell, "PosRing", new Vector3(0.174f, 0f, 0f), layX, cellD * 0.66f, 0.014f, capMat);
            Rod(cell, "PosNub", new Vector3(0.186f, 0f, 0f), layX, cellD * 0.42f, 0.018f, capMat);
            Rod(cell, "PosPip", new Vector3(0.196f, 0f, 0f), layX, cellD * 0.42f - 0.006f, 0.006f, capHiMat);

            // Printed + and − polarity marks sitting a hair proud of the jacket's top surface.
            Block(cell, "PlusH", new Vector3(0.045f, r + 0.006f, 0f), new Vector3(0.026f, 0.004f, 0.006f), markMat);
            Block(cell, "PlusV", new Vector3(0.045f, r + 0.006f, 0f), new Vector3(0.006f, 0.004f, 0.026f), markMat);
            Block(cell, "Minus", new Vector3(-0.06f, r + 0.006f, 0f), new Vector3(0.026f, 0.004f, 0.006f), markMat);
        }

        /// <summary>Lays one flat loop of the coil under <paramref name="parent"/>: <paramref name="segments"/>
        /// short cord segments placed tangent around a circle of the given radius and height. Each segment's
        /// material cycles through the three strand tones so the loop reads as laid rope rather than a plain
        /// tube; keeping <paramref name="segments"/> off a multiple of three drifts the banding round the loop.</summary>
        private static void RopeRing(PartNode parent, float radius, float y, int segments, float cordD,
            PartMat a, PartMat b, PartMat c)
        {
            float chord = 2f * radius * Mathf.Sin(Mathf.PI / segments);   // gap between neighbouring samples
            float segLen = chord * 1.4f;                                  // overlap so the tube reads continuous
            for (int i = 0; i < segments; i++)
            {
                float deg = i * 360f / segments;
                float rad = deg * Mathf.Deg2Rad;
                Vector3 p = new Vector3(radius * Mathf.Cos(rad), y, radius * Mathf.Sin(rad));
                // A Rod at this point tilted (90, -deg, 0) lays its length along the ring's tangent.
                PartMat m = (i % 3 == 0) ? a : (i % 3 == 1) ? b : c;
                Rod(parent, "Coil" + i, p, new Vector3(90f, -deg, 0f), cordD, segLen, m);
            }
        }

        /// <summary>Builds one torn strip of cloth under <paramref name="parent"/> in its own position and
        /// tilt frame: a thin flat band drooping out from the bundle along its local +X, split into a
        /// couple of frayed tongues at the torn tip so the ripped edge reads where it trails.</summary>
        private static void BuildRagStrip(PartNode parent, Vector3 localPos, Vector3 euler,
            float length, float width, PartMat clothMat, PartMat shadeMat)
        {
            var strip = new PartNode("Strip");
            strip.SetParent(parent, false);
            strip.localPosition = localPos;
            strip.localEulerAngles = euler;

            // Flat band running out along local +X, with a shaded underside so the drooping strip has depth.
            Block(strip, "Band", new Vector3(length * 0.5f, 0f, 0f), new Vector3(length, 0.006f, width), clothMat);
            Block(strip, "BandUnder", new Vector3(length * 0.5f, -0.0035f, 0f), new Vector3(length - 0.010f, 0.003f, width - 0.008f), shadeMat);

            // Frayed tongues at the torn tip: two narrow tabs splayed apart with a gap between them.
            var frayL = Block(strip, "FrayL", new Vector3(length + 0.018f, 0f, width * 0.22f), new Vector3(0.040f, 0.005f, width * 0.34f), clothMat);
            frayL.localEulerAngles = new Vector3(0f, -13f, 0f);
            var frayR = Block(strip, "FrayR", new Vector3(length + 0.016f, -0.002f, -width * 0.24f), new Vector3(0.034f, 0.005f, width * 0.30f), shadeMat);
            frayR.localEulerAngles = new Vector3(0f, 15f, 0f);
        }

        /// <summary>Builds one steel nail under <paramref name="parent"/> in its own position and tilt
        /// frame: a chisel-pointed shank capped by a flat round head with a bright catch-light. Built
        /// standing on its point so callers can stand a jumble head-up in the box or lay a few flat by
        /// tilting the frame (a quarter-turn about X lays it on its side).</summary>
        private static void BuildNail(PartNode parent, Vector3 localPos, Vector3 euler,
            PartMat steelMat, PartMat steelDeepMat, PartMat steelHiMat)
        {
            var n = new PartNode("Nail");
            n.SetParent(parent, false);
            n.localPosition = localPos;
            n.localEulerAngles = euler;

            const float shankD = 0.0085f;  // shank diameter
            const float headD = 0.020f;    // flat head, a couple of times the shank

            // Two stepped cones for the point, then the shank, then the flat head with a glinting top.
            Cylinder(n, "TipLow", 0.005f, 0.004f, 0.010f, steelDeepMat);
            Cylinder(n, "TipHigh", 0.015f, 0.007f, 0.010f, steelMat);
            Cylinder(n, "Shank", 0.070f, shankD, 0.100f, steelMat);
            Cylinder(n, "Head", 0.1235f, headD, 0.007f, steelMat);
            Cylinder(n, "HeadTop", 0.1275f, headD - 0.004f, 0.002f, steelHiMat);
        }

        /// <summary>Builds one lying red-and-white capsule under <paramref name="parent"/>: a white
        /// cylinder body with a fatter coloured half and a rounded sphere at each tip, laid flat along X
        /// and yawed by <paramref name="yaw"/> so a couple can be scattered at different angles.</summary>
        private static void BuildCapsule(PartNode parent, Vector3 localPos, float yaw, PartMat whiteMat, PartMat colourMat)
        {
            var cap = new PartNode("Capsule");
            cap.SetParent(parent, false);
            cap.localPosition = localPos;
            cap.localEulerAngles = new Vector3(0f, yaw, 0f);

            const float pillD = 0.045f;   // capsule diameter
            const float pillLen = 0.09f;  // capsule body length along X
            const float half = pillLen * 0.5f;
            const float lay = 90f;        // lays a Rod's length along X

            Rod(cap, "Body", Vector3.zero, new Vector3(0f, 0f, lay), pillD, pillLen, whiteMat);
            // Coloured half sits a hair fatter so it cleanly hides the white body it overlaps.
            Rod(cap, "Half", new Vector3(half * 0.5f, 0f, 0f), new Vector3(0f, 0f, lay), pillD + 0.002f, half, colourMat);
            Sphere(cap, "TipWhite", new Vector3(-half, 0f, 0f), Vector3.zero, new Vector3(pillD, pillD, pillD), whiteMat);
            Sphere(cap, "TipColour", new Vector3(half, 0f, 0f), Vector3.zero, new Vector3(pillD + 0.002f, pillD + 0.002f, pillD + 0.002f), colourMat);
        }

        /// <summary>Builds one round, scored tablet under <paramref name="parent"/> in its own position and
        /// tilt frame: a shallow disc with a gently domed top and a score line across it, laid at
        /// <paramref name="euler"/> so a few can be scattered at different angles.</summary>
        private static void BuildTablet(PartNode parent, Vector3 localPos, Vector3 euler, PartMat tabletMat, PartMat scoreMat)
        {
            var t = new PartNode("Tablet");
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localEulerAngles = euler;

            const float d = 0.05f;       // tablet diameter
            const float thick = 0.015f;  // tablet thickness

            Cylinder(t, "Core", 0f, d, thick, tabletMat);
            Sphere(t, "TopDome", new Vector3(0f, thick * 0.5f, 0f), Vector3.zero, new Vector3(d, thick * 0.9f, d), tabletMat);
            Block(t, "Score", new Vector3(0f, thick * 0.5f + 0.004f, 0f), new Vector3(d * 0.72f, 0.004f, 0.004f), scoreMat);
        }

        /// <summary>A point on a circle in the X-Y (side-on) plane, used to trace curved parts such as the
        /// crowbar's gooseneck as a series of short straight segments.</summary>
        private static Vector3 ArcPoint(float cx, float cy, float r, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a), 0f);
        }

        /// <summary>Adds a cylinder part of the given real height (metres), centred at
        /// <paramref name="localY"/> on the model root.</summary>
        private static PartRef Cylinder(PartNode parent, string name, float localY,
            float diameter, float height, PartMat mat)
        {
            // A Unity cylinder mesh is 2 units tall, so scale.y is half the real height.
            return parent.Add(new Part(PartShape.Cylinder, new Vector3(0f, localY, 0f), Quaternion.identity,
                new Vector3(diameter, height * 0.5f, diameter), mat));
        }

        /// <summary>Adds a cube part of the given real size (metres), centred at
        /// <paramref name="localPos"/> on the model root.</summary>
        private static PartRef Block(PartNode parent, string name, Vector3 localPos, Vector3 size, PartMat mat)
        {
            return parent.Add(new Part(PartShape.Cube, localPos, Quaternion.identity, size, mat));
        }

        /// <summary>Adds a sphere part at <paramref name="localPos"/> with an optional tilt; a Unity sphere
        /// mesh is one unit across, so <paramref name="size"/> is its diameter on each axis (letting the
        /// caller squash it into an ellipsoid).</summary>
        private static PartRef Sphere(PartNode parent, string name, Vector3 localPos,
            Vector3 localEuler, Vector3 size, PartMat mat)
        {
            return parent.Add(new Part(PartShape.Sphere, localPos, Quaternion.Euler(localEuler), size, mat));
        }

        /// <summary>Adds a cylinder part at <paramref name="localPos"/> with an optional tilt — for stalks
        /// and other slender pieces the axis-aligned <see cref="Cylinder"/> can't place. A Unity cylinder
        /// mesh is two units tall, so scale.y is half the real height.</summary>
        private static PartRef Rod(PartNode parent, string name, Vector3 localPos,
            Vector3 localEuler, float diameter, float height, PartMat mat)
        {
            return parent.Add(new Part(PartShape.Cylinder, localPos, Quaternion.Euler(localEuler),
                new Vector3(diameter, height * 0.5f, diameter), mat));
        }
    }
}
