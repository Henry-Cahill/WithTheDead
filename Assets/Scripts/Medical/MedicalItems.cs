using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What the field medicine does. Medical depth through simple steps rather than a big menu (Zomboid): a splint
    /// for a broken bone, a tourniquet or haemostatic gauze for a heavy bleed, tweezers for a lodged shard, a suture
    /// kit for a deep wound, a field kit to patch hit points back, a surgical kit for a blacked-out limb, and blood
    /// for the bloodless — of the right type. Every treatment trains First Aid, which makes them all work better.
    /// </summary>
    public sealed class MedicalItems : IGameModule
    {
        public int Order => 20;

        public void Init()
        {
            ItemActions.RegisterUse("splint", (inv, s) => Treat(inv, s, l => l.Limbs != null && l.Limbs.Splint(), "You set the bone and bind it tight.", "Nothing is broken."));
            ItemActions.RegisterUse("tourniquet", (inv, s) => Treat(inv, s, l => l.Wounds != null && l.Wounds.ApplyTourniquet(),
                "The tourniquet holds. Don't leave it on too long.", "No heavy bleeding on an arm or leg."));
            ItemActions.RegisterUse("hemostat", (inv, s) => Treat(inv, s, l => l.Wounds != null && l.Wounds.PackHeavyBleed(), "You pack the wound. The bleeding stops.", "No heavy bleeding."));
            ItemActions.RegisterUse("suturekit", (inv, s) => Treat(inv, s, l => l.Wounds != null && l.Wounds.Stitch(), "Twelve stitches. It'll close now.", "No wound needs stitching.", consume: true));
            ItemActions.RegisterUse("tweezers", (inv, s) => Treat(inv, s, l => l.Wounds != null && l.Wounds.RemoveShard(), "Out it comes.", "Nothing is lodged in you.", consume: false));
            ItemActions.RegisterUse("fieldkit", FieldKit);
            ItemActions.RegisterUse("surgicalkit", (inv, s) => Treat(inv, s, l => l.Limbs != null && l.Limbs.Surgery(),
                "The limb works again. It will never be quite the same.", "No limb is blacked out."));
            ItemActions.RegisterUse("morphine", (inv, s) => Treat(inv, s, l => l.Wounds != null && l.Wounds.RelievePain(1f), "Warmth floods in. The pain is somewhere far away.", "You're not in pain."));
            ItemActions.RegisterUse("herbs", Herbs);
            ItemActions.RegisterUse("bloodtest", BloodTest);
            ItemActions.RegisterUse("bloodbag_empty", DrawBlood);
            ItemActions.RegisterUse(def => def.Id.StartsWith("bloodbag_") && def.Id != "bloodbag_empty", Transfuse);
        }

        private struct Body
        {
            public PlayerStats Stats;
            public PlayerLimbs Limbs;
            public PlayerWounds Wounds;
            public PlayerBlood Blood;
            public PlayerSkills Skills;
        }

        private static Body Of(PlayerInventory inv) => new Body
        {
            Stats = inv.GetComponent<PlayerStats>(),
            Limbs = inv.GetComponent<PlayerLimbs>(),
            Wounds = inv.GetComponent<PlayerWounds>(),
            Blood = inv.GetComponent<PlayerBlood>(),
            Skills = inv.GetComponent<PlayerSkills>(),
        };

        private static bool Treat(PlayerInventory inv, ItemStack s, System.Func<Body, bool> act, string ok, string nothing, bool consume = true)
        {
            Body b = Of(inv);
            if (!act(b)) { GameEvents.RaiseToast(nothing); return true; }
            if (consume) inv.Inventory.Remove(s, 1);
            Done(b, ok);
            return true;
        }

        private static void Done(Body b, string message)
        {
            if (b.Skills != null) b.Skills.AddXp(PlayerSkills.Skill.FirstAid, 4f);
            Sfx.PlayUI(SfxIds.Bandage);
            GameEvents.RaiseToast(message);
        }

        // Dressings and painkillers: stops a light bleed, takes the edge off, and patches hit points back.
        private static bool FieldKit(PlayerInventory inv, ItemStack s)
        {
            Body b = Of(inv);
            float mult = b.Skills != null ? b.Skills.FirstAidMult : 1f;
            bool did = false;
            if (b.Wounds != null)
            {
                did |= b.Wounds.ApplyBandage(true);
                did |= b.Wounds.Disinfect();
                if (b.Stats != null && b.Stats.Pain > 10f) did |= b.Wounds.RelievePain(0.4f);
            }
            if (b.Limbs != null) did |= b.Limbs.HealLimbs(70f * mult) > 0.5f;
            if (!did) { GameEvents.RaiseToast("You're not hurt."); return true; }
            inv.Inventory.Remove(s, 1);
            Done(b, "Patched up.");
            return true;
        }

        private static bool Herbs(PlayerInventory inv, ItemStack s)
        {
            Body b = Of(inv);
            if (b.Wounds != null) b.Wounds.RelievePain(0.2f);
            if (b.Stats != null) b.Stats.Sickness = Mathf.Max(0f, b.Stats.Sickness - 10f);
            inv.Inventory.Remove(s, 1);
            Sfx.PlayUI(SfxIds.Eat);
            GameEvents.RaiseToast("Bitter. Your stomach settles a little.");
            return true;
        }

        private static bool BloodTest(PlayerInventory inv, ItemStack s)
        {
            Body b = Of(inv);
            CharacterProfile c = ProfileStore.ActiveCharacter;
            string type = b.Blood != null ? b.Blood.BloodType : "?";
            if (c != null && !GameSession.IsBorrowedBody) { c.SetFlag("bloodknown"); ProfileStore.Save(); }
            inv.Inventory.Remove(s, 1);
            Done(b, "Your blood type is " + type + ".");
            return true;
        }

        private static bool DrawBlood(PlayerInventory inv, ItemStack s)
        {
            Body b = Of(inv);
            if (b.Blood == null) return true;
            if (!inv.Has("ivkit")) { GameEvents.RaiseToast("You need an IV kit to draw blood."); return true; }
            if (!b.Blood.DrawBlood()) { GameEvents.RaiseToast("You can't spare a pint right now."); return true; }
            inv.Consume("ivkit", 1);
            inv.Inventory.Remove(s, 1);
            string bag = BloodTypes.BagIdFor(b.Blood.BloodType);
            ItemDef def = ItemDatabase.Get(bag) ?? ItemDatabase.Get("bloodbag_o_pos");
            inv.Inventory.Add(def, 1, enforceWeight: false);
            Done(b, "A pint of your own blood, for someone who needs it more.");
            return true;
        }

        private static bool Transfuse(PlayerInventory inv, ItemStack s)
        {
            Body b = Of(inv);
            if (b.Blood == null) return true;
            if (!inv.Has("ivkit")) { GameEvents.RaiseToast("You need an IV kit to transfuse blood."); return true; }
            string type = BloodTypes.FromBagId(s.Def.Id);
            // Attempt transfusion first; only consume resources on success
            bool success = b.Blood.Transfuse(type, out string message);
            if (!success)
            {
                // Show failure message; do not consume ivkit or blood bag
                GameEvents.RaiseToast(message);
                return true;
            }
            // Successful transfusion: consume IV kit and used blood bag
            inv.Consume("ivkit", 1);
            inv.Inventory.Remove(s, 1);
            Sfx.PlayUI(SfxIds.Injector);
            if (b.Skills != null) b.Skills.AddXp(PlayerSkills.Skill.FirstAid, 6f);
            GameEvents.RaiseToast(message);
            return true;
        }
    }
}
