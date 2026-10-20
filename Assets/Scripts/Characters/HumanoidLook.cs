using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Overall body proportions a humanoid is built with. Every build stands the same
    /// height; they differ in breadth, so one set of joint heights serves them all.</summary>
    public enum BodyBuild : byte { Athletic, Average, Slim, Heavy }

    public enum HairStyle : byte { Bald, Buzz, Short, Long, Ponytail, Messy }

    public enum HeadwearStyle : byte { None, Cap, CapBackward, Beanie, Helmet, WideBrim, Bandana, Mask, Glasses }

    public enum TopStyle : byte { None, TShirt, Tank, LongSleeve, Hoodie, Jacket, Coat, Vest, Dress }

    public enum BottomStyle : byte { Underwear, Pants, Shorts, SkirtShort, SkirtKnee, SkirtLong }

    public enum FootwearStyle : byte { Barefoot, Shoes, Boots, Sandals, Socks }

    public enum HandwearStyle : byte { Bare, Gloves, Fingerless }

    /// <summary>
    /// The colour regions a humanoid mesh is split into. Each region is one submesh drawn with a
    /// shared material chosen per character, so characters that differ only in colour share a mesh.
    /// </summary>
    public enum HumanoidSlot : byte
    {
        Skin, Hair, Eyes, Mouth, Top, TopTrim, Bottom, BottomTrim, Shoes, Soles, Gloves, Hat, HatTrim, Gore,
    }

    /// <summary>
    /// Everything that changes a humanoid's geometry. Two characters with the same shape share one
    /// baked mesh however they are coloured — which is what keeps a horde of differently dressed
    /// zombies cheap: the mesh is built once and only the per-region materials differ.
    /// </summary>
    public struct HumanoidShape : IEquatable<HumanoidShape>
    {
        public BodyBuild Build;
        public HairStyle Hair;
        public HeadwearStyle Headwear;
        public TopStyle Top;
        public BottomStyle Bottom;
        public FootwearStyle Feet;
        public HandwearStyle Hands;
        /// <summary>Undead details: a gaping mouth, plus blood laid out from <see cref="Gore"/>.</summary>
        public bool Zombie;
        /// <summary>Seed for the blood splashes (0 = none).</summary>
        public byte Gore;
        /// <summary>Sleeves torn off at the shoulder: bit 0 = left arm, bit 1 = right arm.</summary>
        public byte TornSleeves;

        private int Packed =>
            (int)Build | ((int)Hair << 2) | ((int)Headwear << 5) | ((int)Top << 9) | ((int)Bottom << 13) |
            ((int)Feet << 16) | ((int)Hands << 19) | ((Zombie ? 1 : 0) << 21) | ((TornSleeves & 3) << 22) | (Gore << 24);

        public bool Equals(HumanoidShape other) => Packed == other.Packed;
        public override bool Equals(object obj) => obj is HumanoidShape o && Equals(o);
        public override int GetHashCode() => Packed;
        public override string ToString() => Packed.ToString("X8");
    }

    /// <summary>
    /// How one humanoid looks: its <see cref="HumanoidShape"/> plus the colour and finish of every
    /// region. Clothing is applied per slot with <see cref="Wear"/>, which reads the garment's cut
    /// from its name and its colour from the item's tint, so whatever the survivor puts on shows on
    /// the body.
    /// </summary>
    public sealed class HumanoidLook
    {
        public HumanoidShape Shape;
        /// <summary>Uniform size multiplier (1 = a 1.8 m adult).</summary>
        public float Scale = 1f;

        public Color SkinColor = new Color(0.78f, 0.56f, 0.41f);
        public Color HairColor = new Color(0.19f, 0.12f, 0.08f);
        public Color EyeColor = new Color(0.09f, 0.06f, 0.05f);
        public Color MouthColor = new Color(0.56f, 0.31f, 0.27f);
        public Color TopColor = new Color(0.2f, 0.55f, 0.65f);
        public Color TopTrimColor = new Color(0.14f, 0.40f, 0.47f);
        public Color BottomColor = new Color(0.22f, 0.24f, 0.28f);
        public Color BottomTrimColor = new Color(0.13f, 0.14f, 0.16f);
        public Color ShoeColor = new Color(0.35f, 0.24f, 0.18f);
        public Color SoleColor = new Color(0.10f, 0.09f, 0.08f);
        public Color GloveColor = new Color(0.35f, 0.24f, 0.18f);
        public Color HatColor = new Color(0.4f, 0.45f, 0.6f);
        public Color HatTrimColor = new Color(0.28f, 0.32f, 0.42f);
        public Color GoreColor = new Color(0.30f, 0.035f, 0.03f);
        /// <summary>What shows on the hips when no legwear is worn.</summary>
        public Color UnderwearColor = new Color(0.22f, 0.24f, 0.28f);

        public float TopGloss = 0.08f;
        public float BottomGloss = 0.08f;
        public float ShoeGloss = 0.25f;
        public float GloveGloss = 0.15f;
        public float HatGloss = 0.15f;

        private static readonly Color BeltColor = new Color(0.22f, 0.15f, 0.10f);
        private static readonly Color DarkSole = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color LightSole = new Color(0.86f, 0.85f, 0.82f);
        private static readonly Color DarkGlass = new Color(0.07f, 0.08f, 0.09f);

        /// <summary>Creates a shallow copy of this HumanoidLook.</summary>
        public HumanoidLook Clone() => (HumanoidLook)MemberwiseClone();

        /// <summary>Gets the colour of the given humanoid slot, falling back to Gore for unknown slots.</summary>
        public Color ColorOf(HumanoidSlot slot)
        {
            switch (slot)
            {
                case HumanoidSlot.Skin: return SkinColor;
                case HumanoidSlot.Hair: return HairColor;
                case HumanoidSlot.Eyes: return EyeColor;
                case HumanoidSlot.Mouth: return MouthColor;
                case HumanoidSlot.Top: return TopColor;
                case HumanoidSlot.TopTrim: return TopTrimColor;
                case HumanoidSlot.Bottom: return BottomColor;
                case HumanoidSlot.BottomTrim: return BottomTrimColor;
                case HumanoidSlot.Shoes: return ShoeColor;
                case HumanoidSlot.Soles: return SoleColor;
                case HumanoidSlot.Gloves: return GloveColor;
                case HumanoidSlot.Hat: return HatColor;
                case HumanoidSlot.HatTrim: return HatTrimColor;
                default: return GoreColor;
            }
        }

        /// <summary>Gets the gloss value for the given humanoid slot, using defaults for body parts and custom values for clothing slots.</summary>
        public float GlossOf(HumanoidSlot slot)
        {
            switch (slot)
            {
                case HumanoidSlot.Skin: return 0.32f;
                case HumanoidSlot.Hair: return 0.22f;
                case HumanoidSlot.Eyes: return 0.7f;
                case HumanoidSlot.Mouth: return 0.45f;
                case HumanoidSlot.Top:
                case HumanoidSlot.TopTrim: return TopGloss;
                case HumanoidSlot.Bottom:
                case HumanoidSlot.BottomTrim: return BottomGloss;
                case HumanoidSlot.Shoes: return ShoeGloss;
                case HumanoidSlot.Soles: return 0.2f;
                case HumanoidSlot.Gloves: return GloveGloss;
                case HumanoidSlot.Hat: return HatGloss;
                case HumanoidSlot.HatTrim: return Shape.Headwear == HeadwearStyle.Glasses ? 0.85f : HatGloss;
                default: return 0.6f;   // wet blood
            }
        }

        /// <summary>
        /// Dresses one body slot. A null garment strips the slot back to the body (bare skin, bare
        /// feet, underwear on the hips). The garment's cut comes from its name — a "Leather Jacket"
        /// gets a collar, zip and long sleeves, "Shorts" stop above the knee — and its colour from the
        /// item's tint, so the model always matches the inventory.
        /// </summary>
        public void Wear(ClothingSlot slot, ItemDef def)
        {
            string k = GarmentStyles.Key(def);
            Color c = def != null ? def.Tint : Color.white;

            switch (slot)
            {
                case ClothingSlot.Head:
                    Shape.Headwear = def == null ? HeadwearStyle.None : GarmentStyles.Headwear(k);
                    HatColor = c;
                    HatTrimColor = Shape.Headwear == HeadwearStyle.Glasses ? DarkGlass : Shade(c, 0.7f);
                    HatGloss = GarmentStyles.Gloss(k, Shape.Headwear == HeadwearStyle.Helmet ? 0.5f : 0.12f);
                    break;

                case ClothingSlot.Torso:
                    Shape.Top = def == null ? TopStyle.None : GarmentStyles.Top(k);
                    TopColor = c;
                    TopTrimColor = Shade(c, 0.72f);
                    TopGloss = GarmentStyles.Gloss(k, 0.08f);
                    break;

                case ClothingSlot.Legs:
                    if (def == null)
                    {
                        Shape.Bottom = BottomStyle.Underwear;
                        BottomColor = UnderwearColor;
                        BottomTrimColor = Shade(UnderwearColor, 0.6f);
                        BottomGloss = 0.1f;
                    }
                    else
                    {
                        Shape.Bottom = GarmentStyles.Bottom(k);
                        BottomColor = c;
                        bool belted = Shape.Bottom == BottomStyle.Pants || Shape.Bottom == BottomStyle.Shorts;
                        BottomTrimColor = belted ? BeltColor : Shade(c, 0.72f);
                        BottomGloss = GarmentStyles.Gloss(k, 0.08f);
                    }
                    break;

                case ClothingSlot.Feet:
                    Shape.Feet = def == null ? FootwearStyle.Barefoot : GarmentStyles.Feet(k);
                    ShoeColor = c;
                    SoleColor = GarmentStyles.LightSoled(k) ? LightSole : DarkSole;
                    ShoeGloss = GarmentStyles.Gloss(k, Shape.Feet == FootwearStyle.Boots ? 0.3f : 0.18f);
                    break;

                case ClothingSlot.Hands:
                    Shape.Hands = def == null ? HandwearStyle.Bare : GarmentStyles.Hands(k);
                    GloveColor = c;
                    GloveGloss = GarmentStyles.Gloss(k, 0.12f);
                    break;
            }
        }

        /// <summary>Clone Z — the survivor the player controls: an athletic adult with warm tan skin and short dark-brown hair. The outfit comes from whatever is worn (see <see cref="Wear"/>). Creates the default survivor look with athletic build, short hair, and default colours. The look is then dressed with empty slots (no clothing).</summary>
                public static HumanoidLook Survivor()
        {
            var look = new HumanoidLook();
            look.Shape.Build = BodyBuild.Athletic;
            look.Shape.Hair = HairStyle.Short;
            look.SkinColor = new Color(0.78f, 0.56f, 0.41f);
            look.HairColor = new Color(0.19f, 0.12f, 0.08f);
            look.EyeColor = new Color(0.09f, 0.06f, 0.05f);
            look.MouthColor = new Color(0.56f, 0.31f, 0.27f);
            look.UnderwearColor = new Color(0.22f, 0.24f, 0.28f);
            for (int i = 0; i < PlayerClothing.Slots.Length; i++) look.Wear(PlayerClothing.Slots[i], null);
            return look;
        }

        // Fixed palettes keep the number of distinct zombie materials bounded however many spawn.
        private static readonly Color[] s_Cloth =
        {
            new Color(0.62f, 0.62f, 0.60f), new Color(0.28f, 0.30f, 0.34f), new Color(0.18f, 0.22f, 0.36f),
            new Color(0.30f, 0.38f, 0.55f), new Color(0.55f, 0.18f, 0.16f), new Color(0.62f, 0.50f, 0.30f),
            new Color(0.35f, 0.42f, 0.28f), new Color(0.45f, 0.30f, 0.20f), new Color(0.70f, 0.66f, 0.52f),
            new Color(0.24f, 0.45f, 0.48f), new Color(0.50f, 0.36f, 0.52f), new Color(0.78f, 0.72f, 0.30f),
            new Color(0.85f, 0.84f, 0.80f), new Color(0.12f, 0.12f, 0.13f), new Color(0.60f, 0.36f, 0.42f),
            new Color(0.25f, 0.35f, 0.25f),
        };

        private static readonly Color[] s_HairColors =
        {
            new Color(0.08f, 0.07f, 0.06f), new Color(0.20f, 0.13f, 0.08f), new Color(0.38f, 0.25f, 0.14f),
            new Color(0.66f, 0.55f, 0.35f), new Color(0.52f, 0.52f, 0.50f), new Color(0.50f, 0.24f, 0.12f),
        };

        private static readonly Color Grime = new Color(0.30f, 0.26f, 0.20f);

        /// <summary>A random dead survivor: random build, hair and grimy clothes, pale eyes, a gaping
        /// mouth and blood. The skin is set from the zombie type afterwards.</summary>
        public static HumanoidLook RandomZombie(System.Random rng)
        {
            var look = new HumanoidLook();
            HumanoidShape s = default;
            s.Zombie = true;
            s.Gore = (byte)rng.Next(1, 256);

            double r = rng.NextDouble();
            s.Build = r < 0.45 ? BodyBuild.Average : r < 0.70 ? BodyBuild.Slim : r < 0.90 ? BodyBuild.Heavy : BodyBuild.Athletic;

            r = rng.NextDouble();
            s.Hair = r < 0.12 ? HairStyle.Bald : r < 0.25 ? HairStyle.Buzz : r < 0.55 ? HairStyle.Short
                   : r < 0.70 ? HairStyle.Messy : r < 0.90 ? HairStyle.Long : HairStyle.Ponytail;

            r = rng.NextDouble();
            s.Top = r < 0.30 ? TopStyle.TShirt : r < 0.50 ? TopStyle.LongSleeve : r < 0.62 ? TopStyle.Hoodie
                  : r < 0.74 ? TopStyle.Jacket : r < 0.84 ? TopStyle.Tank : r < 0.90 ? TopStyle.Dress
                  : r < 0.95 ? TopStyle.Coat : r < 0.98 ? TopStyle.Vest : TopStyle.None;

            r = rng.NextDouble();
            s.Bottom = r < 0.68 ? BottomStyle.Pants : r < 0.84 ? BottomStyle.Shorts : r < 0.94 ? BottomStyle.SkirtKnee
                     : r < 0.97 ? BottomStyle.SkirtShort : BottomStyle.Underwear;
            if (s.Top == TopStyle.Dress && s.Bottom != BottomStyle.Pants) s.Bottom = BottomStyle.Underwear;

            r = rng.NextDouble();
            s.Feet = r < 0.50 ? FootwearStyle.Shoes : r < 0.75 ? FootwearStyle.Boots : r < 0.95 ? FootwearStyle.Barefoot : FootwearStyle.Socks;

            s.Hands = rng.NextDouble() < 0.08 ? HandwearStyle.Gloves : HandwearStyle.Bare;

            r = rng.NextDouble();
            s.Headwear = r < 0.76 ? HeadwearStyle.None : r < 0.84 ? HeadwearStyle.Cap : r < 0.91 ? HeadwearStyle.Beanie
                       : r < 0.94 ? HeadwearStyle.CapBackward : r < 0.96 ? HeadwearStyle.Helmet
                       : r < 0.98 ? HeadwearStyle.WideBrim : HeadwearStyle.Glasses;

            bool sleeved = s.Top == TopStyle.TShirt || s.Top == TopStyle.LongSleeve || s.Top == TopStyle.Hoodie
                        || s.Top == TopStyle.Jacket || s.Top == TopStyle.Coat;
            if (sleeved && rng.NextDouble() < 0.25) s.TornSleeves = (byte)(rng.Next(2) == 0 ? 1 : 2);

            look.Shape = s;
            look.Scale = 0.94f + (float)rng.NextDouble() * 0.11f;
            look.SkinColor = new Color(0.36f, 0.5f, 0.30f);
            look.HairColor = Color.Lerp(s_HairColors[rng.Next(s_HairColors.Length)], new Color(0.4f, 0.4f, 0.38f), 0.25f);
            look.EyeColor = new Color(0.86f, 0.84f, 0.66f);
            look.MouthColor = new Color(0.20f, 0.03f, 0.03f);

            look.TopColor = Dirty(s_Cloth[rng.Next(s_Cloth.Length)]);
            look.TopTrimColor = Shade(look.TopColor, 0.72f);
            look.BottomColor = Dirty(s_Cloth[rng.Next(s_Cloth.Length)]);
            look.BottomTrimColor = s.Bottom == BottomStyle.Pants || s.Bottom == BottomStyle.Shorts ? BeltColor : Shade(look.BottomColor, 0.72f);
            look.ShoeColor = Dirty(s_Cloth[rng.Next(s_Cloth.Length)]);
            look.SoleColor = rng.Next(3) == 0 ? Dirty(LightSole) : DarkSole;
            look.GloveColor = Dirty(s_Cloth[rng.Next(s_Cloth.Length)]);
            look.HatColor = Dirty(s_Cloth[rng.Next(s_Cloth.Length)]);
            look.HatTrimColor = s.Headwear == HeadwearStyle.Glasses ? DarkGlass : Shade(look.HatColor, 0.7f);
            look.UnderwearColor = Dirty(new Color(0.8f, 0.78f, 0.74f));
            if (s.Bottom == BottomStyle.Underwear)
            {
                look.BottomColor = look.UnderwearColor;
                look.BottomTrimColor = Shade(look.UnderwearColor, 0.7f);
            }
            return look;
        }

        public static Color Shade(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

        private static Color Dirty(Color c) => Color.Lerp(c, Grime, 0.3f);
    }

    /// <summary>
    /// Reads a garment's cut from its id and display name. The catalogue holds hundreds of clothes,
    /// so the body only knows a handful of cuts — a cap, a helmet, a T-shirt, a coat, shorts, a
    /// skirt, boots — and every item is sorted into the closest one by keyword.
    /// </summary>
    public static class GarmentStyles
    {
        public static string Key(ItemDef def) =>
            def == null ? string.Empty : ((def.Id ?? string.Empty) + " " + (def.DisplayName ?? string.Empty)).ToLowerInvariant();

        private static bool Any(string key, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
                if (key.IndexOf(words[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        public static HeadwearStyle Headwear(string k)
        {
            if (Any(k, "glasses", "goggles", "monocle", "shades", "spectacles", "aviator")) return HeadwearStyle.Glasses;
            if (Any(k, "mask", "balaclava", "respirator", "shemagh", "scarf")) return HeadwearStyle.Mask;
            if (Any(k, "helmet", "hardhat", "hard hat", "riot", "welding")) return HeadwearStyle.Helmet;
            if (Any(k, "bandana", "headband", "sweatband", "headwrap", "head wrap")) return HeadwearStyle.Bandana;
            if (Any(k, "beanie", "wool", "knit", "toque", "bobble", "earmuff")) return HeadwearStyle.Beanie;
            if (Any(k, "cap", "visor", "jockey", "baseball"))
                return Any(k, "reverse", "backward") ? HeadwearStyle.CapBackward : HeadwearStyle.Cap;
            if (Any(k, "cowboy", "sun hat", "sunhat", "boonie", "bonnie", "bucket", "fedora", "ranger", "stetson",
                    "brim", "straw", "fishing", "sombrero", "stovepipe", "top hat", "tophat", "bowler", "trilby",
                    "panama", "sheriff", "safari"))
                return HeadwearStyle.WideBrim;
            return HeadwearStyle.Beanie;
        }

        public static TopStyle Top(string k)
        {
            if (Any(k, "dress", "gown")) return TopStyle.Dress;
            if (Any(k, "tshirt", "t-shirt", "t shirt", "tee shirt", "polo", "shortsleeve", "short sleeve", "jersey")) return TopStyle.TShirt;
            if (Any(k, "tank", "vest top", "singlet", "bandeau", "strapless", "sleeveless", "bra", "bikini", "crop top", "halter", "camisole")) return TopStyle.Tank;
            if (Any(k, "hoodie", "hooded", "sweatshirt")) return TopStyle.Hoodie;
            if (Any(k, "coat", "trench", "parka", "poncho", "robe", "duster", "ghillie")) return TopStyle.Coat;
            if (Any(k, "jacket", "blazer", "bomber", "windbreaker", "anorak", "fleece", "puffer", "padded", "varsity")) return TopStyle.Jacket;
            if (Any(k, "vest", "waistcoat", "gilet", "bulletproof", "kevlar", "apron")) return TopStyle.Vest;
            return TopStyle.LongSleeve;
        }

        public static BottomStyle Bottom(string k)
        {
            if (Any(k, "skirt"))
            {
                if (Any(k, "short", "mini")) return BottomStyle.SkirtShort;
                if (Any(k, "long", "maxi")) return BottomStyle.SkirtLong;
                return BottomStyle.SkirtKnee;
            }
            if (Any(k, "briefs", "underpants", "boxers", "panties", "knickers", "thong", "bikini", "speedo", "underwear")) return BottomStyle.Underwear;
            if (Any(k, "shorts", "trunks", "hotpants")) return BottomStyle.Shorts;
            return BottomStyle.Pants;
        }

        public static FootwearStyle Feet(string k)
        {
            if (Any(k, "sock")) return FootwearStyle.Socks;
            if (Any(k, "sandal", "flip", "slipper", "croc")) return FootwearStyle.Sandals;
            if (Any(k, "boot", "wellies", "wellington", "cowboy", "riding")) return FootwearStyle.Boots;
            if (Any(k, "wrap", "rag", "tarp", "burlap", "twine", "tire", "tyre")) return FootwearStyle.Socks;
            return FootwearStyle.Shoes;
        }

        public static HandwearStyle Hands(string k) => Any(k, "fingerless") ? HandwearStyle.Fingerless : HandwearStyle.Gloves;

        public static bool LightSoled(string k) => Any(k, "sneaker", "trainer", "tennis", "running", "sport");

        /// <summary>Surface gloss from the material named in the garment, or the given default.</summary>
        public static float Gloss(string k, float fallback)
        {
            if (Any(k, "leather", "latex", "rubber", "vinyl", "patent")) return 0.42f;
            if (Any(k, "rain", "plastic", "tarp", "garbage", "pvc", "wellies")) return 0.55f;
            if (Any(k, "helmet", "metal", "chainmail", "armour", "armor")) return 0.5f;
            return fallback;
        }
    }
}
