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

        private readonly Dictionary<ClothingSlot, ItemDef> _worn = new Dictionary<ClothingSlot, ItemDef>();

        /// <summary>Raised whenever the worn set changes (for UI refresh).</summary>
        public event System.Action Changed;

        public ItemDef GetWorn(ClothingSlot slot) => _worn.TryGetValue(slot, out ItemDef d) ? d : null;
        public bool IsWearing(ClothingSlot slot) => GetWorn(slot) != null;

        public int WornCount
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<ClothingSlot, ItemDef> kv in _worn)
                    if (kv.Value != null) n++;
                return n;
            }
        }

        /// <summary>Wears a garment on its slot; returns whatever it replaced (to return to the bag), or null.</summary>
        public ItemDef Equip(ItemDef def)
        {
            if (def == null || !def.IsClothing) return null;
            ItemDef prev = GetWorn(def.ClothingSlot);
            if (prev == def) return null;
            _worn[def.ClothingSlot] = def;
            Changed?.Invoke();
            return prev;
        }

        /// <summary>Takes a garment off; returns it so the caller can drop it in the bag, or null if bare.</summary>
        public ItemDef Remove(ClothingSlot slot)
        {
            ItemDef prev = GetWorn(slot);
            if (prev == null) return null;
            _worn.Remove(slot);
            Changed?.Invoke();
            return prev;
        }

        /// <summary>Total warmth from everything worn; soaked clothes lose most of their insulation.</summary>
        public float TotalInsulation(float wetness)
        {
            float sum = 0f;
            foreach (KeyValuePair<ClothingSlot, ItemDef> kv in _worn)
                if (kv.Value != null) sum += kv.Value.Insulation;
            float wetMult = 1f - CloneZConfig.WetInsulationLoss * Mathf.Clamp01(wetness);
            return sum * wetMult;
        }

        /// <summary>Best waterproofing among worn clothes — a raincoat keeps most of the rain off.</summary>
        public float WaterResistance()
        {
            float best = 0f;
            foreach (KeyValuePair<ClothingSlot, ItemDef> kv in _worn)
                if (kv.Value != null && kv.Value.WaterResistance > best) best = kv.Value.WaterResistance;
            return best;
        }

        /// <summary>Rolls whether worn clothing stops an incoming bite (true = no wound got through).</summary>
        public bool BlocksBite() => RollBlock(true);

        /// <summary>Rolls whether worn clothing turns an incoming scratch (true = no wound got through).</summary>
        public bool BlocksScratch() => RollBlock(false);

        private bool RollBlock(bool bite)
        {
            ItemDef piece = GetWorn(RollHitArea());
            if (piece == null) return false;
            float defence = bite ? piece.BiteDefense : piece.ScratchDefense;
            return defence > 0f && Random.value < defence;
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
