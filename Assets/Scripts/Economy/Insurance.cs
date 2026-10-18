using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Insurance, Tarkov style: pay a share of an item's value and, if you die carrying it and nobody walks off
    /// with it, it comes back a couple of days later. A gold sink, and a reason for killers to loot (or hide)
    /// what they kill.
    /// </summary>
    public static class InsuranceDesk
    {
        public const float PremiumShare = 0.12f;
        public const string Insurer = TraderCatalog.Medic;

        public static bool Insurable(ItemStack s) => s != null && s.Def != null && s.Def.Id != Money.Id
            && (s.Def.IsFirearm || s.Def.IsWeapon || s.Def.IsClothing || Ballistics.Mod(s.Def.Id) != null || s.Def.IsDamageable);

        public static int Premium(ItemStack s)
        {
            int v = Money.BaseValue(s.Def);
            if (s.Extra != null) for (int i = 0; i < s.Extra.Mods.Count; i++) v += Money.BaseValue(ItemDatabase.Get(s.Extra.Mods[i]));
            return Mathf.Max(20, Mathf.RoundToInt(v * PremiumShare));
        }

        /// <summary>Insures a stack (paid from the stash, then the pack).</summary>
        public static bool Insure(ItemStack s, PlayerInventory carried, out string message)
        {
            message = null;
            if (!Insurable(s)) { message = "That can't be insured."; return false; }
            if (s.IsInsured()) { message = "Already insured."; return false; }
            int cost = Premium(s);
            if (!Money.TrySpend(cost, carried)) { message = "Insurance costs " + Money.Format(cost) + "."; return false; }
            s.Ensure().Insured = true;
            message = s.Def.DisplayName + " insured for " + Money.Format(cost) + ".";
            ProfileStore.Save();
            return true;
        }

        /// <summary>
        /// Files a claim for every insured item among those lost, removing them from <paramref name="lost"/>. Each
        /// one has a chance to have been taken by whoever found the body (more where other players roam).
        /// </summary>
        public static void FileClaims(CharacterProfile c, List<StackSave> lost, Vector3 where)
        {
            if (c == null || lost == null) return;
            var claim = new InsuranceClaim { TraderId = Insurer, ReturnOnDay = c.Day + Mathf.Max(1, SandboxSettings.Current.InsuranceReturnDays) };
            float takenChance = GameSession.Networked && ServerRules.PvpEnabled ? 0.45f : 0.25f;
            for (int i = lost.Count - 1; i >= 0; i--)
            {
                ItemExtra x = ItemExtra.Parse(lost[i].Extra);
                if (x == null || !x.Insured) continue;
                StackSave s = lost[i];
                lost.RemoveAt(i);
                if (Random.value < takenChance) continue;           // someone extracted with it
                x.Insured = false;
                x.FoundInRaid = false;
                s.Extra = ItemExtra.Serialize(x);
                claim.Items.Add(s);
            }
            if (claim.Items.Count > 0) c.Insurance.Add(claim);
        }

        /// <summary>Claims whose day has come (ready at the insurer's counter).</summary>
        public static int ReadyClaims(CharacterProfile c)
        {
            int n = 0;
            if (c == null) return 0;
            foreach (InsuranceClaim ic in c.Insurance) if (c.Day >= ic.ReturnOnDay) n += ic.Items.Count;
            return n;
        }

        /// <summary>Moves every returned item into the stash. Returns how many came back.</summary>
        public static int Collect(CharacterProfile c)
        {
            if (c == null) return 0;
            int n = 0;
            Inventory stash = ProfileStore.StashInventory;
            for (int i = c.Insurance.Count - 1; i >= 0; i--)
            {
                InsuranceClaim ic = c.Insurance[i];
                if (c.Day < ic.ReturnOnDay) continue;
                foreach (StackSave s in ic.Items) n += ProfileStore.AddSave(stash, s) > 0 ? 1 : 0;
                c.Insurance.RemoveAt(i);
            }
            if (n > 0) ProfileStore.Save();
            return n;
        }
    }

    /// <summary>
    /// Gray Zone Warfare's softer death: under the corpse-recovery rule your body and gear stay where you fell.
    /// The next raid into that zone finds it waiting — unless the Zone's scavengers got there first.
    /// </summary>
    public sealed class CorpseRecovery : IGameModule
    {
        public int Order => 50;

        private static readonly List<KeyValuePair<CorpseRecord, LootContainer>> s_Live = new List<KeyValuePair<CorpseRecord, LootContainer>>();

        public static void Record(CharacterProfile c, string zone, Vector3 position, List<StackSave> items)
        {
            if (c == null || items == null || items.Count == 0) return;
            c.Corpses.Add(new CorpseRecord { ZoneId = zone, Position = position, Items = new List<StackSave>(items), DiedOnDay = c.Day });
            if (c.Corpses.Count > 6) c.Corpses.RemoveAt(0);   // the oldest bodies are gone for good
        }

        public void Init()
        {
            WorldHooks.RaidStarted += OnRaidStarted;
            GameEvents.RaidEnded += OnRaidEnded;
        }

        private static void OnRaidStarted(Transform world, RaidMapInfo map, ZoneDef zone)
        {
            s_Live.Clear();
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || GameSession.IsBorrowedBody) return;
            foreach (CorpseRecord r in c.Corpses)
            {
                if (r.Recovered || r.Looted || r.ZoneId != zone.Id || r.Items.Count == 0) continue;
                LootContainer box = LootContainer.SpawnSaved(world, (int)ContainerType.Corpse, r.Position, 0f, true, 0f, r.Items);
                if (box == null) continue;
                box.DisplayName = "Your body (" + c.Name + ")";
                s_Live.Add(new KeyValuePair<CorpseRecord, LootContainer>(r, box));
                GameEvents.RaiseToast("Your body still lies somewhere in " + zone.Name + ".");
            }
        }

        private static void OnRaidEnded(RaidOutcome outcome, string exit)
        {
            if (s_Live.Count == 0) return;
            foreach (KeyValuePair<CorpseRecord, LootContainer> kv in s_Live)
            {
                if (kv.Value == null) continue;
                kv.Key.Items = ProfileStore.ToSaves(kv.Value.Contents);
                if (kv.Key.Items.Count == 0) kv.Key.Recovered = true;
            }
            s_Live.Clear();
            ProfileStore.Save();
        }

        /// <summary>Each passing day the Zone's scavengers may find an unrecovered body first.</summary>
        public static void OnDayPassed(CharacterProfile c)
        {
            foreach (CorpseRecord r in c.Corpses)
                if (!r.Recovered && !r.Looted && Random.value < 0.18f) r.Looted = true;
            c.Corpses.RemoveAll(r => r.Recovered || r.Looted || c.Day - r.DiedOnDay > 10);
        }
    }
}
