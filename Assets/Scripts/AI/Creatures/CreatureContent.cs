using UnityEngine;

namespace CloneZ
{
    /// <summary>Carcass loot: hides, pelts and the mutant parts the Institute and the Fence pay for.</summary>
    public sealed class CreatureContent : IContentProvider
    {
        public int Order => 30;

        public void Register(ContentBuilder b)
        {
            Part(b, "hound_fang", "Hound Fang", 0.05f, 10, "A yellowed fang as long as a finger. The Institute buys them by the dozen.");
            Part(b, "shade_tendril", "Shade Tendril", 0.3f, 3, "It still twitches when you aren't looking at it.");
            Part(b, "puppeteer_gland", "Puppeteer Gland", 0.4f, 2, "A grey sac from the base of the skull. Hums faintly.");
            Part(b, "thumper_hoof", "Thumper Hoof", 3.5f, 1, "A plate of horn the size of a dinner tray.", 2, 2);
            Part(b, "wolf_pelt", "Wolf Pelt", 1.2f, 2, "Warm, thick, and worth something to the right buyer.", 2, 2);
            Part(b, "bear_pelt", "Bear Pelt", 4.5f, 1, "Heavy. A trophy, or a winter coat.", 3, 2);
            Part(b, "deer_hide", "Deer Hide", 1.4f, 2, "Tanned, it makes good leather.", 2, 2);
            ItemDef raw = b.Item("rawmeat", "Raw Meat", ItemCategory.Food, 0.6f, 4, 1, 1, "Fresh. Cook it first.");
            if (!b.InDatabase("rawmeat"))
            {
                raw.HungerRestore = 0.2f;
                raw.Perishable = true;
                raw.ShelfLifeHours = 24f;
                raw.Cookable = true;
                raw.CookedId = "cookedmeat";
                raw.RawHazard = true;
            }
            ItemDef cooked = b.Item("cookedmeat", "Cooked Meat", ItemCategory.Food, 0.5f, 4, 1, 1, "Grilled over a fire.");
            if (!b.InDatabase("cookedmeat"))
            {
                cooked.HungerRestore = 0.32f;
                cooked.Perishable = true;
                cooked.ShelfLifeHours = 48f;
                cooked.IsCookedFood = true;
            }
        }

        private static void Part(ContentBuilder b, string id, string name, float w, int stack, string desc, int sw = 1, int sh = 1)
            => b.Item(id, name, ItemCategory.Material, w, stack, sw, sh, desc);
    }

    /// <summary>Psi-block pills: the mind is dulled for a few minutes and psi attacks slide off it.</summary>
    public sealed class PsiShield : MonoBehaviour, IStatModSource
    {
        private float _until;

        public void Take(float seconds) => _until = Mathf.Max(_until, Time.time + seconds);

        public void ApplyMods(StatMods mods)
        {
            if (Time.time > _until) return;
            mods.Resist.Psi += 0.6f;
            mods.AimSpread *= 1.08f;
        }
    }

    /// <summary>Wires the people and creatures into the survivor: companion orders, psi-block pills.</summary>
    public sealed class ZoneLifeModule : IGameModule
    {
        public int Order => 55;

        public void Init()
        {
            ItemActions.RegisterUse("psiblock", (inv, stack) =>
            {
                var shield = inv.GetComponent<PsiShield>();
                if (shield == null) shield = inv.gameObject.AddComponent<PsiShield>();
                shield.Take(300f);
                inv.Inventory.Remove(stack, 1);
                Sfx.PlayUI(SfxIds.Eat, 0.5f);
                GameEvents.RaiseToast("Psi-block taken: your thoughts go quiet and far away.");
                return true;
            });
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<CompanionOrders>() == null) p.gameObject.AddComponent<CompanionOrders>();
            };
        }
    }
}
