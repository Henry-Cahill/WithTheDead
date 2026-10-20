using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The mirror cabinet over the bathroom basin: a white wall cabinet with a mirrored door, kept as a
    /// <see cref="StorageFixture"/> ("Bathroom cabinet" — a medicine cabinet's odds and ends at most; the home is meant to
    /// be lean, and one rebuilt by the survivor starts bare). Its extra action, "Look in the mirror" (the mirror's on the
    /// door, so an open cabinet is swung shut first), tells the survivor how they look: the one to three things the face
    /// in the glass gives away most — exhaustion, a sick pallor, hunger, thirst, wet hair, the cold or the heat, pain,
    /// fear, wounds and, quietly, the turn — which steadies or rattles the nerves a little (not more than once a minute).
    /// </summary>
    internal static class MirrorCabinet
    {
        // One thing the mirror shows: the part of you it's about (only the worst per part is told), how much it stands
        // out (0..1, more is worse) and the words for it.
        private readonly struct Sign
        {
            public readonly int Part;
            public readonly float Weight;
            public readonly string Text;
            public Sign(int part, float weight, string text) { Part = part; Weight = weight; Text = text; }
        }

        private const int Eyes = 0, Skin = 1, Cheeks = 2, Lips = 3, Hair = 4, Hurt = 5, Jaw = 6;
        private const float Notable = 0.3f;   // the least a sign has to stand out to be mentioned
        private const float Alarming = 0.75f; // a sign this bad (or any wound bleeding, or the turn) rattles the nerves

        private static readonly List<Sign> s_Signs = new List<Sign>(12);
        private static float s_NervesAgainAt;

        /// <summary>Builds the cabinet's storage under <paramref name="wall"/> (the wall piece): <paramref name="point"/> is the
        /// door's middle, a few centimetres proud of it, and <paramref name="door"/> the mirrored door hinged to swing open.
        /// A <paramref name="stocked"/> cabinet (the house as it came) is rolled from a lean medicine cabinet's table; one
        /// the survivor's rebuilt wall brings back starts bare.</summary>
        public static StorageFixture Create(Transform wall, Vector3 point, FixturePart door, int seed, bool stocked)
        {
            var options = new StorageOptions
            {
                Noun = "bathroom cabinet",
                ContainerName = "Bathroom cabinet",
                LootType = ContainerType.MedCabinet,
                StartEmpty = !stocked,
                EmptyChance = CloneZConfig.MirrorCabinetEmptyChance,
                MaxStacks = CloneZConfig.MirrorCabinetMaxStacks,
            };
            return Create(wall, point, door, seed, options);
        }

        /// <summary>The cabinet stocked as <paramref name="options"/> says (a raid house's: one of its main containers, see
        /// <see cref="RaidBuilding.MainStock"/>); its look in the mirror is added to them.</summary>
        public static StorageFixture Create(Transform wall, Vector3 point, FixturePart door, int seed, StorageOptions options)
        {
            options ??= new StorageOptions { LootType = ContainerType.MedCabinet };
            options.Noun ??= "bathroom cabinet";
            options.ContainerName ??= "Bathroom cabinet";
            StorageFixture cabinet = null;
            // The mirror's on the door: an open cabinet is swung shut first.
            options.ExtraActions = (ctx, into) => into.Add(new InteractAction("Look in the mirror", c =>
            {
                if (cabinet != null && cabinet.IsOpen) cabinet.SetOpen(false, true);
                Look(c);
            }));
            cabinet = StorageFixture.Create(wall, "Bathroom cabinet", Interactable.KeyFor("mirrorcabinet", point), point,
                new[] { door }, null, seed, options);
            return cabinet;
        }

        /// <summary>"Look in the mirror": a short account of how the survivor looks, from their state.</summary>
        public static void Look(InteractContext ctx)
        {
            PlayerStats s = ctx.Stats;
            if (s == null) return;
            s_Signs.Clear();
            Gather(ctx, s);
            if (s_Signs.Count == 0)
            {
                ctx.Notify("You look about as well as anyone can these days.");
                Steady(ctx, -CloneZConfig.MirrorCalm);
                return;
            }

            // The worst first, one per part of you, three at most.
            s_Signs.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            var told = new StringBuilder("In the mirror: ");
            int count = 0, parts = 0;
            float worst = s_Signs[0].Weight;
            for (int i = 0; i < s_Signs.Count && count < 3; i++)
            {
                Sign sign = s_Signs[i];
                if ((parts & (1 << sign.Part)) != 0) continue;
                parts |= 1 << sign.Part;
                if (count > 0) told.Append("; ");
                told.Append(sign.Text);
                count++;
            }
            told.Append('.');

            bool turning = s.Infected, bleeding = ctx.Wounds != null && ctx.Wounds.IsBleeding;
            if (turning) told.Append(" Probably just the light in here.");
            else if (bleeding) told.Append(" You should see to that.");
            else if (worst >= Alarming) told.Append(" You've looked better.");
            else told.Append(" Could be worse.");
            ctx.Notify(told.ToString());

            if (turning || bleeding || worst >= Alarming) Steady(ctx, CloneZConfig.MirrorUnsettle);
            else Steady(ctx, -CloneZConfig.MirrorCalm * 0.5f);
        }

        // The nerves move at most once a minute, whichever way, however often you look.
        private static void Steady(InteractContext ctx, float delta)
        {
            if (Time.time < s_NervesAgainAt) return;
            s_NervesAgainAt = Time.time + CloneZConfig.MirrorCalmCooldown;
            BathroomFixtures.Nerves(ctx, delta);
        }

        private static void Add(int part, float weight, string text)
        {
            if (weight >= Notable) s_Signs.Add(new Sign(part, weight, text));
        }

        // Everything the face in the glass might give away, each with how much it stands out.
        private static void Gather(InteractContext ctx, PlayerStats s)
        {
            if (s.Infected) Add(Skin, 1.1f, "a grey, waxy cast to your skin");

            float f = s.Fatigue;
            if (f >= 0.95f) Add(Eyes, 1f, "eyes you can barely keep open");
            else if (f >= CloneZConfig.FatigueExhaustedThreshold) Add(Eyes, 0.8f, "hollow, red-rimmed eyes");
            else if (f >= 0.75f) Add(Eyes, 0.55f, "dark smudges under your eyes");
            else if (f >= 0.6f) Add(Eyes, 0.35f, "a tired, heavy-lidded look");

            int sick = ctx.Sickness != null ? ctx.Sickness.Level
                : s.Sickness >= CloneZConfig.SicknessLevel4 ? 4 : s.Sickness >= CloneZConfig.SicknessLevel3 ? 3
                : s.Sickness >= CloneZConfig.SicknessLevel2 ? 2 : s.Sickness >= CloneZConfig.SicknessLevel1 ? 1 : 0;
            if (sick >= 4) Add(Skin, 1f, "a face the colour of old putty, slick with sweat");
            else if (sick == 3) Add(Skin, 0.8f, "a pale, clammy face with a sickly sheen");
            else if (sick == 2) Add(Skin, 0.55f, "a pale, clammy face");
            else if (sick == 1) Add(Skin, 0.3f, "a slightly peaky look");

            float h = s.Hunger;
            if (h >= 0.92f) Add(Cheeks, 1f, "a gaunt face, all cheekbone");
            else if (h >= 0.75f) Add(Cheeks, 0.75f, "hollow cheeks");
            else if (h >= 0.55f) Add(Cheeks, 0.45f, "a face thinner than you remember");

            float th = s.Thirst;
            if (th >= 0.92f) Add(Lips, 1f, "cracked, bleeding lips");
            else if (th >= 0.75f) Add(Lips, 0.75f, "parched lips split at the corners");
            else if (th >= 0.55f) Add(Lips, 0.45f, "dry, chapped lips");

            float w = s.Wetness;
            if (w >= 0.8f) Add(Hair, 0.6f, "soaked hair plastered to your head");
            else if (w >= 0.55f) Add(Hair, 0.45f, "wet, straggling hair");
            else if (w >= CloneZConfig.WetnessDampThreshold) Add(Hair, 0.3f, "hair still damp");

            int warmth = ctx.Temperature != null ? ctx.Temperature.Level
                : s.Temperature < CloneZConfig.TempHypothermiaThreshold + 0.02f ? -2 : s.Temperature < CloneZConfig.TempComfortLow ? -1
                : s.Temperature > CloneZConfig.TempHyperthermiaThreshold - 0.01f ? 2 : s.Temperature > CloneZConfig.TempComfortHigh ? 1 : 0;
            if (warmth <= -2) Add(Skin, 1f, "chattering teeth and skin gone blotchy with cold");
            else if (warmth == -1) Add(Skin, 0.45f, "goosebumps and a pinched, chilly look");
            else if (warmth >= 2) Add(Skin, 0.9f, "a beetroot-red face streaming with sweat");
            else if (warmth == 1) Add(Skin, 0.45f, "a flushed, shiny face");

            float pain = s.PainNormalized;
            if (pain >= 0.55f) Add(Jaw, 0.6f, "a jaw clenched against the pain");
            else if (pain >= 0.3f) Add(Jaw, 0.35f, "a wince you can't quite hide");

            int panic = ctx.Panic != null ? ctx.Panic.Level
                : s.Panic >= CloneZConfig.PanicLevel3 ? 3 : s.Panic >= CloneZConfig.PanicLevel2 ? 2 : 0;
            if (panic >= 3) Add(Eyes, 0.6f, "wide, darting eyes");
            else if (panic == 2) Add(Eyes, 0.35f, "a tight, haunted look about the eyes");

            PlayerWounds wounds = ctx.Wounds;
            if (wounds == null || wounds.WoundCount == 0) return;
            if (wounds.IsBleeding) Add(Hurt, 0.9f, "fresh blood where you've been hurt");
            else if (wounds.HasInfectedWound) Add(Hurt, 0.85f, "an angry, swollen wound");
            else if (wounds.CanBandage) Add(Hurt, 0.55f, "cuts and scrapes you ought to see to");
            else Add(Hurt, 0.3f, "bandages here and there");
        }
    }
}
