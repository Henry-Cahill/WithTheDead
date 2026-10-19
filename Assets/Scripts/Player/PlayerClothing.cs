using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The clothing the survivor is wearing — one garment per body slot. Clothing
    /// insulates against the cold (feeding <see cref="PlayerTemperature"/>) and can
    /// turn aside a zombie's teeth or nails: when a strike lands it rolls against the
    /// covering garment's bite/scratch defence, mirroring Project Zomboid's clothing
    /// protection. Wet clothes insulate far less. Equipping is driven from the
    /// inventory (<see cref="PlayerInventory.Use"/>), which hands worn pieces back to
    /// the bag when they're replaced.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerClothing : MonoBehaviour
    {
        private static readonly ClothingSlot[] s_Slots =
            (ClothingSlot[])System.Enum.GetValues(typeof(ClothingSlot));
        public static ClothingSlot[] Slots => s_Slots;

        // Where a zombie strike is most likely to land — the torso and legs take the brunt.
        private static readonly (ClothingSlot Slot, float Weight)[] s_HitAreas =
        {
            (ClothingSlot.Torso, 0.40f), (ClothingSlot.Legs, 0.24f),
            (ClothingSlot.Hands, 0.14f), (ClothingSlot.Head, 0.12f), (ClothingSlot.Feet, 0.10f),
        };

        /// <summary>A worn garment plus its per-piece wear. Reuses the condition model from
        /// <see cref="ItemStack"/>: full at <see cref="ItemDef.MaxCondition"/>, worn through into a
        /// useless hole at 0. <see cref="TimesRepaired"/> rides along so mending caps diminish.</summary>
        public readonly struct WornPiece
        {
            public readonly ItemDef Def;
            public readonly int Condition;
            public readonly int TimesRepaired;
            public readonly ItemExtra Extra;
            public WornPiece(ItemDef def, int condition, int timesRepaired, ItemExtra extra = null)
            { Def = def; Condition = condition; TimesRepaired = timesRepaired; Extra = extra; }
            public bool Exists => Def != null;
            public float Condition01 => Def == null || Def.MaxCondition <= 0
                ? 1f : Mathf.Clamp01((float)Condition / Def.MaxCondition);
        }

        private readonly Dictionary<ClothingSlot, WornPiece> _worn = new Dictionary<ClothingSlot, WornPiece>();

        /// <summary>Raised whenever the worn set changes (for UI refresh).</summary>
        public event System.Action Changed;

        public ItemDef GetWorn(ClothingSlot slot) => _worn.TryGetValue(slot, out WornPiece p) ? p.Def : null;
        public bool IsWearing(ClothingSlot slot) => _worn.ContainsKey(slot);

        /// <summary>The full worn piece (def + wear) on a slot, or an empty <see cref="WornPiece"/>.</summary>
        public WornPiece GetPiece(ClothingSlot slot) => _worn.TryGetValue(slot, out WornPiece p) ? p : default;

        /// <summary>How intact the garment on a slot is, 0..1 (1 when the slot is bare).</summary>
        public float GetCondition01(ClothingSlot slot) => _worn.TryGetValue(slot, out WornPiece p) ? p.Condition01 : 1f;

        public int WornCount => _worn.Count;

        /// <summary>Wears a garment on its slot, carrying its condition and repair history. Returns
        /// whatever it replaced (to hand back to the bag); <see cref="WornPiece.Exists"/> is false
        /// when the slot was bare. A negative <paramref name="condition"/> means a pristine garment.</summary>
        public WornPiece Equip(ItemDef def, int condition = -1, int timesRepaired = 0, ItemExtra extra = null)
        {
            if (def == null || !def.IsClothing) return default;
            _worn.TryGetValue(def.ClothingSlot, out WornPiece prev);
            if (prev.Def == def) return default;
            int cond = condition >= 0 ? Mathf.Clamp(condition, 0, def.MaxCondition) : def.MaxCondition;
            _worn[def.ClothingSlot] = new WornPiece(def, cond, Mathf.Max(0, timesRepaired), ItemExtra.CopyOf(extra));
            Changed?.Invoke();
            return prev;
        }

        /// <summary>Takes a garment off; returns it (with its wear) so the caller can drop it in the
        /// bag, or an empty <see cref="WornPiece"/> if the slot was bare.</summary>
        public WornPiece Remove(ClothingSlot slot)
        {
            if (!_worn.TryGetValue(slot, out WornPiece prev)) return default;
            _worn.Remove(slot);
            Changed?.Invoke();
            return prev;
        }

        /// <summary>Sets the condition of the piece worn on a slot — its wear, or for a device (night
        /// vision, gas mask) its charge. Silent by default so a draining battery doesn't rebuild every
        /// UI each tick; pass <paramref name="notify"/> to raise <see cref="Changed"/>.</summary>
        public void SetCondition(ClothingSlot slot, int condition, bool notify = false)
        {
            if (!_worn.TryGetValue(slot, out WornPiece p) || p.Def == null) return;
            int c = Mathf.Clamp(condition, 0, Mathf.Max(0, p.Def.MaxCondition));
            if (c == p.Condition) return;
            _worn[slot] = new WornPiece(p.Def, c, p.TimesRepaired, p.Extra);
            if (notify) Changed?.Invoke();
        }

        /// <summary>Total warmth from everything worn; holes (low condition) and soaking both bleed it away.</summary>
        public float TotalInsulation(float wetness)
        {
            float sum = 0f;
            foreach (KeyValuePair<ClothingSlot, WornPiece> kv in _worn)
                sum += kv.Value.Def.Insulation * kv.Value.Condition01;   // a threadbare garment barely insulates
            float wetMult = 1f - CloneZConfig.WetInsulationLoss * Mathf.Clamp01(wetness);
            return sum * wetMult;
        }

        /// <summary>Best waterproofing among worn clothes — a raincoat keeps most of the rain off
        /// (a holed one leaks).</summary>
        public float WaterResistance()
        {
            float best = 0f;
            foreach (KeyValuePair<ClothingSlot, WornPiece> kv in _worn)
            {
                float wr = kv.Value.Def.WaterResistance * kv.Value.Condition01;
                if (wr > best) best = wr;
            }
            return best;
        }

        /// <summary>Rolls whether worn clothing stops an incoming bite (true = no wound got through).</summary>
        public bool BlocksBite() => RollBlock(true);

        /// <summary>Rolls whether worn clothing turns an incoming scratch (true = no wound got through).</summary>
        public bool BlocksScratch() => RollBlock(false);

        // Picks the struck body area; a garment there absorbs the blow (wearing toward a hole) and
        // turns it with a chance scaled by how intact it still is.
        private bool RollBlock(bool bite)
        {
            ClothingSlot area = RollHitArea();
            if (!_worn.TryGetValue(area, out WornPiece piece)) return false;

            float defence = (bite ? piece.Def.BiteDefense : piece.Def.ScratchDefense) * piece.Condition01;
            WearPiece(area, piece);   // a covered strike frays the fabric whether or not it turns the blow
            return defence > 0f && Random.value < defence;
        }

        // One chance-gated point of wear on a struck garment (1-in-ConditionLossChance); a garment
        // worn to 0 is shredded into a useless rag and falls off the slot.
        private void WearPiece(ClothingSlot slot, WornPiece piece)
        {
            if (Random.Range(0, Mathf.Max(1, piece.Def.ConditionLossChance)) != 0) return;
            int cond = piece.Condition - 1;
            if (cond > 0) _worn[slot] = new WornPiece(piece.Def, cond, piece.TimesRepaired, piece.Extra);
            else _worn.Remove(slot);
            Changed?.Invoke();
        }

        private static ClothingSlot RollHitArea()
        {
            float total = 0f;
            for (int i = 0; i < s_HitAreas.Length; i++) total += s_HitAreas[i].Weight;
            float pick = Random.value * total;
            for (int i = 0; i < s_HitAreas.Length; i++)
            {
                if (pick < s_HitAreas[i].Weight) return s_HitAreas[i].Slot;
                pick -= s_HitAreas[i].Weight;
            }
            return ClothingSlot.Torso;
        }

        /// <summary>A short comma-list of what's worn, for the health panel.</summary>
        public string Summary()
        {
            var names = new List<string>(s_Slots.Length);
            for (int i = 0; i < s_Slots.Length; i++)
            {
                ItemDef d = GetWorn(s_Slots[i]);
                if (d != null) names.Add(d.DisplayName);
            }
            return names.Count == 0 ? "Nothing worn" : string.Join(" \u00b7 ", names);
        }
    }
}
