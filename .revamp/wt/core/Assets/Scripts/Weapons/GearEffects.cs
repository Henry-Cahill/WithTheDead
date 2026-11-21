using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What worn armour and Zone suits do to the body besides stopping rounds: plates and exo-frames weigh on the
    /// legs, an exo-frame carries more, and a suit's upgrades follow it. Read every frame as a stat-mod source.
    /// </summary>
    public class GearEffects : MonoBehaviour, IStatModSource
    {
        private static readonly ClothingSlot[] s_Slots = { ClothingSlot.Armor, ClothingSlot.Head, ClothingSlot.Torso };
        private PlayerClothing _clothing;

        public void ApplyMods(StatMods m)
        {
            if (_clothing == null) _clothing = GetComponent<PlayerClothing>();
            if (_clothing == null) return;
            for (int i = 0; i < s_Slots.Length; i++)
            {
                PlayerClothing.WornPiece p = _clothing.GetPiece(s_Slots[i]);
                if (!p.Exists) continue;
                ArmorStats a = WeaponUpgrades.Effective(Ballistics.Armor(p.Def.Id), p.Extra);
                if (a == null) continue;
                m.MoveSpeed *= 1f - Mathf.Clamp01(a.MovePenalty);
                m.CarryBonusKg += a.CarryBonusKg;
                if (a.ErgoPenalty > 0f) m.AimSpread *= 1f + a.ErgoPenalty;
            }
        }
    }
}
