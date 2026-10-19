using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What each artifact actually does. Litter, not treasure: nothing here is ever shown until it has been
    /// learned — by wearing the thing long enough to feel each effect, by handing it to the Institute, or by
    /// buying the rumour in the bar. Learned properties are account knowledge (they survive death and wipes)
    /// and can be passed on: knowledge is the real loot.
    /// </summary>
    public static class ArtifactCatalog
    {
        public sealed class Property
        {
            public string Id, Label;
            public bool Drawback;
            public float LearnSeconds;          // worn this long before the effect becomes noticeable
            public System.Action<StatMods> Apply;
        }

        public sealed class Def
        {
            public string Id, Name;
            public Color Color;
            public int DetectorTier;            // the detector tier needed to sense it at all
            public int Value;                   // the Institute's fixed price, in military-grade rounds
            public string[] Properties;
            public bool LuresMutants;
        }

        private static readonly Dictionary<string, Property> s_Props = new Dictionary<string, Property>();
        private static readonly Dictionary<string, Def> s_Defs = new Dictionary<string, Def>();

        static ArtifactCatalog()
        {
            P("carry", "+8 kg carry capacity", false, 40f, m => m.CarryBonusKg += 8f);
            P("rad", "Emits radiation", true, 25f, m => m.RadiationPerSec += 0.02f);
            P("stamina", "Stamina recovers faster", false, 35f, m => m.StaminaRegen *= 1.3f);
            P("bleedup", "Wounds bleed harder", true, 60f, m => m.BleedRate *= 1.35f);
            P("shockres", "Resists electric shocks", false, 50f, m => m.Resist.Shock += 0.3f);
            P("burnres", "Resists burns", false, 50f, m => m.Resist.Burn += 0.3f);
            P("warm", "Keeps the cold out", false, 30f, m => m.ColdResist += 0.4f);
            P("calm", "Steadies the nerves", false, 40f, m => m.PanicRate *= 0.6f);
            P("purge", "Slowly purges radiation", false, 90f, m => m.RadiationPurge += 0.012f);
            P("hunger", "Makes you hungry", true, 70f, m => m.HungerRate *= 1.3f);
            P("bleeddown", "Wounds close faster", false, 60f, m => m.BleedRate *= 0.5f);
            P("heal", "Accelerates healing", false, 75f, m => m.HealRate *= 1.6f);
            P("psiweak", "Opens the mind to psi attacks", true, 80f, m => m.Resist.Psi -= 0.25f);
            P("fast", "Lightens the step", false, 30f, m => m.MoveSpeed *= 1.08f);
            P("quiet", "Muffles your footsteps", false, 45f, m => m.NoiseMult *= 0.7f);
            P("tired", "Wears you out", true, 70f, m => m.FatigueRate *= 1.35f);
            P("bulletres", "Turns bullets", false, 60f, m => m.Resist.Bullet += 0.15f);
            P("slow", "Weighs your legs", true, 25f, m => m.MoveSpeed *= 0.94f);
            P("psires", "Shields the mind", false, 55f, m => m.Resist.Psi += 0.4f);
            P("hidden", "Bends light around you", false, 65f, m => m.VisibilityMult *= 0.78f);
            P("panic", "Fills you with dread", true, 35f, m => m.PanicRate *= 1.45f);
            P("chemres", "Resists acids and gas", false, 50f, m => m.Resist.Chemical += 0.4f);
            P("thirst", "Dries you out", true, 60f, m => m.ThirstRate *= 1.3f);
            P("enduring", "Stamina lasts longer", false, 40f, m => m.StaminaDrain *= 0.7f);
            P("lure", "Draws mutants to you", true, 120f, m => { });

            D("art_hollowstone", "Hollowstone", new Color(0.62f, 0.58f, 0.52f), 1, 650, "carry", "rad");
            D("art_coldspark", "Coldspark", new Color(0.45f, 0.75f, 0.95f), 1, 700, "shockres", "stamina", "bleedup");
            D("art_emberheart", "Emberheart", new Color(0.95f, 0.45f, 0.18f), 2, 1100, "burnres", "warm", "rad");
            D("art_mosscalm", "Mosscalm", new Color(0.45f, 0.70f, 0.38f), 1, 600, "calm", "purge", "hunger");
            D("art_needlebloom", "Needlebloom", new Color(0.85f, 0.35f, 0.50f), 2, 1400, "bleeddown", "heal", "psiweak");
            D("art_driftshell", "Drift Shell", new Color(0.80f, 0.82f, 0.70f), 2, 1200, "fast", "quiet", "tired");
            D("art_lodeknot", "Lode Knot", new Color(0.35f, 0.32f, 0.30f), 3, 2400, "bulletres", "slow", "rad");
            D("art_whisperglass", "Whisper Glass", new Color(0.70f, 0.62f, 0.90f), 3, 2600, "psires", "hidden", "panic");
            D("art_greywax", "Greywax", new Color(0.58f, 0.60f, 0.62f), 1, 750, "chemres", "purge", "thirst");
            D("art_saltlung", "Saltlung", new Color(0.92f, 0.92f, 0.88f), 1, 550, "enduring", "hunger");
            D("art_sunkenstar", "Sunken Star", new Color(1f, 0.90f, 0.55f), 3, 9000, "heal", "bulletres", "carry", "lure", "rad");
            s_Defs["art_sunkenstar"].LuresMutants = true;
        }

        private static void P(string id, string label, bool drawback, float learn, System.Action<StatMods> apply)
            => s_Props[id] = new Property { Id = id, Label = label, Drawback = drawback, LearnSeconds = learn, Apply = apply };

        private static void D(string id, string name, Color color, int tier, int value, params string[] props)
            => s_Defs[id] = new Def { Id = id, Name = name, Color = color, DetectorTier = tier, Value = value, Properties = props };

        public static Def Get(string id) => id != null && s_Defs.TryGetValue(id, out Def d) ? d : null;
        public static Property Prop(string id) => id != null && s_Props.TryGetValue(id, out Property p) ? p : null;
        public static bool IsArtifact(string id) => id != null && s_Defs.ContainsKey(id);
        public static bool IsArtifact(ItemDef def) => def != null && IsArtifact(def.Id);
        public static IEnumerable<Def> All => s_Defs.Values;

        public static string NameOf(string id) { Def d = Get(id); return d != null ? d.Name : id; }
        public static Color ColorOf(string id) { Def d = Get(id); return d != null ? d.Color : Color.white; }

        #region Knowledge
        private static string Key(string artifact, string prop) => artifact + ":" + prop;

        public static bool IsKnown(string artifact, string prop)
            => ProfileStore.Account != null && ProfileStore.Account.ArtifactKnowledge.Contains(Key(artifact, prop));

        public static int KnownCount(string artifact)
        {
            Def d = Get(artifact);
            if (d == null) return 0;
            int n = 0;
            for (int i = 0; i < d.Properties.Length; i++) if (IsKnown(artifact, d.Properties[i])) n++;
            return n;
        }

        /// <summary>Learns one property (returns true when it was new). Raises <see cref="GameEvents.ArtifactPropertyLearned"/>.</summary>
        public static bool Learn(string artifact, string prop, bool announce = true)
        {
            AccountProfile a = ProfileStore.Account;
            if (a == null || IsKnown(artifact, prop)) return false;
            a.ArtifactKnowledge.Add(Key(artifact, prop));
            GameEvents.RaiseArtifactLearned(artifact, prop);
            if (announce)
            {
                Property p = Prop(prop);
                GameEvents.RaiseToast(NameOf(artifact) + ": " + (p != null ? p.Label : prop));
            }
            return true;
        }

        /// <summary>Learns every property of an artifact (the Institute's analysis).</summary>
        public static int LearnAll(string artifact)
        {
            Def d = Get(artifact);
            if (d == null) return 0;
            int n = 0;
            for (int i = 0; i < d.Properties.Length; i++) if (Learn(artifact, d.Properties[i], false)) n++;
            return n;
        }

        /// <summary>Tooltip lines: the known properties, and a "???" for each one still unknown.</summary>
        public static string Describe(string artifact)
        {
            Def d = Get(artifact);
            if (d == null) return null;
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#c8ccd2>Detector:</color> <b>tier ").Append(d.DetectorTier).Append("</b>");
            for (int i = 0; i < d.Properties.Length; i++)
            {
                string pid = d.Properties[i];
                Property p = Prop(pid);
                if (IsKnown(artifact, pid))
                    sb.Append("\n<color=").Append(p != null && p.Drawback ? "#e07a5f" : "#8fd18b").Append(">\u2022 ").Append(p != null ? p.Label : pid).Append("</color>");
                else
                    sb.Append("\n<color=#7f8590>\u2022 ??? (wear it to find out)</color>");
            }
            return sb.ToString();
        }
        #endregion
    }

    /// <summary>ABO/Rh blood types: who can receive whose blood (DayZ's transfusion rules).</summary>
    public static class BloodTypes
    {
        public static readonly string[] All = { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" };
        // Rough real-world frequencies, used to roll a new character's type.
        private static readonly float[] Weights = { 0.07f, 0.37f, 0.06f, 0.36f, 0.02f, 0.08f, 0.01f, 0.03f };

        public static string Roll(System.Random rng = null)
        {
            float r = rng != null ? (float)rng.NextDouble() : Random.value, acc = 0f;
            for (int i = 0; i < All.Length; i++) { acc += Weights[i]; if (r <= acc) return All[i]; }
            return "O+";
        }

        /// <summary>True when a recipient can safely take a donor's blood.</summary>
        public static bool Compatible(string donor, string recipient)
        {
            if (string.IsNullOrEmpty(donor) || string.IsNullOrEmpty(recipient)) return false;
            bool dRh = donor.EndsWith("+"), rRh = recipient.EndsWith("+");
            if (dRh && !rRh) return false;
            string d = donor.TrimEnd('+', '-'), r = recipient.TrimEnd('+', '-');
            if (d == "O") return true;
            if (r == "AB") return true;
            return d == r;
        }

        /// <summary>"bloodbag_ab_pos" → "AB+".</summary>
        public static string FromBagId(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("bloodbag_")) return null;
            string[] p = id.Substring(9).Split('_');
            if (p.Length < 2) return null;
            return p[0].ToUpperInvariant() + (p[1] == "pos" ? "+" : "-");
        }

        public static string BagIdFor(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            string g = type.TrimEnd('+', '-').ToLowerInvariant();
            return "bloodbag_" + g + "_" + (type.EndsWith("+") ? "pos" : "neg");
        }
    }
}
