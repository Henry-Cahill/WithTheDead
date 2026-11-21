using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A trader's counter: what they sell at your loyalty level (money or barter), what they'll buy from your stash and
    /// pack, and their standing with you. The Institute pays fixed, fair prices for artifacts and identifies them; the
    /// fence pays more for the same artifact and adds heat — the Wardens notice who is selling what.
    /// </summary>
    public sealed class TraderScreen : HubScreenBase
    {
        private TraderDef T => TraderCatalog.Get(Arg) ?? TraderCatalog.Get(TraderCatalog.Barkeep);
        protected override string Title => T.Name + " — " + T.Role;
        private readonly List<HubItemRef> _items = new List<HubItemRef>();

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            TraderDef t = T;
            int ll = Trading.LoyaltyOf(c, t);
            RectTransform head = HubUI.Band(Body, 0f, 56f);
            HubUI.Card(head);
            RectTransform hs = HubUI.Stack(head, 2f, 8);
            string next = Trading.NextLevelNeeds(c, t);
            HubUI.Para(hs, "<b>Loyalty " + ll + "</b>   standing " + c.Trader(t.Id).Rep.ToString("0.00") + "   restock in " + Trading.RestockIn(c, t)
                + (next != null ? "   <color=#9aa0a8>next: " + next + "</color>" : ""), 15);
            HubUI.Para(hs, t.Greeting, 14, UIKit.TextDim, FontStyle.Italic);

            RectTransform rest = HubUI.Below(Body, 64f);
            RectTransform[] cols = HubUI.Columns(rest, 16f, 1f, 1f);

            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Buy", 20);
            RectTransform buy = HubUI.Scroll(cols[0], 40f, 0f, 3f);
            int i = 0;
            foreach (TraderOffer o in t.Offers)
            {
                ItemDef d = ItemDatabase.Get(o.ItemId);
                if (d == null) continue;
                bool visible = Trading.Visible(c, t, o);
                if (!visible && o.Loyalty > ll + 1) continue;
                RectTransform row = HubUI.Row(buy, 40f, i++ % 2 == 0 ? new Color(1f, 1f, 1f, 0.03f) : (Color?)null);
                string price = o.Price > 0 ? HubUI.NeedMoney(o.Price, Carried) : "";
                if (o.Barter != null)
                    foreach ((string id, int n) in o.Barter) price += " " + HubUI.Need(id, n, Carried);
                HubUI.Cell(row, HubUI.ItemName(d) + (o.Count > 1 ? " x" + o.Count : ""), 330f, 15);
                HubUI.Cell(row, price, 230f, 14);
                if (!visible) { HubUI.Cell(row, "LL" + o.Loyalty, 60f, 14, UIKit.TextDim); continue; }
                HubUI.Cell(row, Trading.StockLeft(c, t, o) + " left", 70f, 13, UIKit.TextDim);
                TraderOffer pick = o;
                bool can = Trading.CanBuy(c, t, o, Carried, out string why);
                Button b = HubUI.Btn(row, "Buy", () =>
                {
                    bool ok = Trading.Buy(c, t, pick, Carried, out string msg);
                    if (ok) HubKit.AfterChange();
                    Report(ok, msg);
                    Refresh();
                }, 70f, 30f, null, can);
                if (!can && Tip != null) AttachTip(b.gameObject, why);
            }

            HubUI.Card(cols[1]);
            HubUI.AreaHeading(cols[1], "Sell (stash and pack)", 20);
            RectTransform sell = HubUI.Scroll(cols[1], 40f, 0f, 3f);
            HubKit.Collect(_items, d => Trading.WillBuy(t, d), false, true, true);
            i = 0;
            foreach (HubItemRef r in _items)
            {
                if (HubKit.InPouch(r.Stack)) continue;
                int unit = Trading.SellPrice(t, r.Stack);
                if (unit <= 0) continue;
                RectTransform row = HubUI.ItemRow(sell, r.Stack, Tip, i++, null, 40f, "  <size=12><color=#9aa0a8>" + r.SourceLabel + "</color></size>");
                HubUI.Cell(row, Money.Format(unit), 110f, 14, UIKit.Good);
                HubItemRef pick = r;
                HubUI.Btn(row, "Sell", () => DoSell(c, t, pick, 1), 60f);
                if (r.Stack.Count > 1) HubUI.Btn(row, "All", () => DoSell(c, t, pick, pick.Stack.Count), 50f);
            }
            if (t.Identifies)
                HubUI.Note(sell, "Dr. Brandt identifies every artifact you sell him — all of its properties go in your notes.", UIKit.TextDim);
            if (!t.Legal)
                HubUI.Note(sell, "Black market: better prices for artifacts, but every one adds heat (current " + c.Heat.ToString("0.0") + ").", UIKit.Warn);
            RectTransform services = HubUI.ButtonRow(sell);
            ServiceButton(services, "Jobs", HubScreens.Quests, t.Id);
            if (t.Repairs || t.Upgrades) ServiceButton(services, "Technician", HubScreens.Technician);
            if (t.Insures) ServiceButton(services, "Insurance", HubScreens.Insurance);
        }

        private void DoSell(CharacterProfile c, TraderDef t, HubItemRef r, int n)
        {
            if (!r.StillValid) { Refresh(); return; }
            int got = Trading.Sell(c, t, r.Owner, r.Stack, n, out string msg);
            if (got > 0) { HubKit.AfterChange(); ProfileStore.CommitStash(); Sfx.PlayUI(SfxIds.Cash); }
            Report(got > 0, msg);
            Refresh();
        }

        private void AttachTip(GameObject go, string text)
        {
            var trig = go.AddComponent<HubTooltipTrigger>();
            trig.Tip = Tip;
            trig.Text = () => text;
        }
    }

    /// <summary>
    /// The flea market (tension 8 — a player market with guard rails): opens at a character level (or with the Intel
    /// Centre), takes a listing fee, only accepts found-in-raid goods, bans artifacts (so the Zone's finds go through
    /// the Institute or the fence), and caps listings per day. Sellers are other runners — on a server, the other
    /// players; offline, simulated runners who list and buy at market prices.
    /// </summary>
    public sealed class FleaScreen : HubScreenBase
    {
        protected override string Title => "Flea market";
        private const int MaxListings = 4;

        public static bool Open(CharacterProfile c, out string why)
        {
            why = null;
            if (!SandboxSettings.Current.FleaMarket) { why = "The flea market is closed on this server."; return false; }
            if (c.Level < SandboxSettings.Current.FleaMinLevel && Hideout.LevelOf(c, "intel") < 1)
            { why = "Reach level " + SandboxSettings.Current.FleaMinLevel + " (or build the Intel Centre) to trade here."; return false; }
            return true;
        }

        public static bool Allowed(ItemStack s, out string why)
        {
            why = null;
            if (s?.Def == null || s.Def.Id == Money.Id) { why = "Not tradeable"; return false; }
            if (ArtifactCatalog.IsArtifact(s.Def)) { why = "Artifacts are banned from the market"; return false; }
            if (!s.IsFoundInRaid()) { why = "Only found-in-raid goods can be listed"; return false; }
            return true;
        }

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            if (!Open(c, out string why))
            {
                HubUI.Para(HubUI.Stack(HubUI.Area(Body, 0.25f, 0.4f, 0.75f, 0.6f)), why, 20, UIKit.Warn);
                return;
            }
            Settle(c);
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1.1f, 1f);

            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Offers from other runners", 20);
            RectTransform offers = HubUI.Scroll(cols[0], 40f, 0f, 3f);
            int i = 0;
            foreach (FleaListing l in c.Listings)
            {
                if (l.Mine || l.Sold) continue;
                ItemDef d = ItemDatabase.Get(l.Item.Id);
                if (d == null) continue;
                RectTransform row = HubUI.Row(offers, 38f, i++ % 2 == 0 ? new Color(1f, 1f, 1f, 0.03f) : (Color?)null);
                HubUI.Cell(row, HubUI.ItemName(d) + (l.Item.Count > 1 ? " x" + l.Item.Count : ""), 300f, 15);
                HubUI.Cell(row, l.Seller, 150f, 13, UIKit.TextDim);
                HubUI.Cell(row, HubUI.NeedMoney(l.Price, Carried), 120f, 14);
                FleaListing pick = l;
                HubUI.Btn(row, "Buy", () =>
                {
                    if (!Money.TrySpend(pick.Price, Carried)) { Report(false, "Not enough money."); return; }
                    pick.Sold = true;
                    ProfileStore.AddSave(ProfileStore.StashInventory, pick.Item);
                    ProfileStore.CommitStash();
                    HubKit.AfterChange();
                    Report(true, "Bought — it's in your stash.");
                    Refresh();
                }, 60f);
            }

            HubUI.Card(cols[1]);
            HubUI.AreaHeading(cols[1], "Your listings", 20);
            RectTransform mine = HubUI.Scroll(cols[1], 40f, 0f, 3f);
            int count = 0;
            foreach (FleaListing l in c.Listings)
            {
                if (!l.Mine) continue;
                count++;
                ItemDef d = ItemDatabase.Get(l.Item.Id);
                HubUI.Note(mine, (d != null ? d.DisplayName : l.Item.Id) + " — " + Money.Format(l.Price) + (l.Sold ? "  SOLD" : "  (listed)"), l.Sold ? UIKit.Good : UIKit.TextColor);
            }
            c.Listings.RemoveAll(l => l.Mine && l.Sold && l.ExpiresUnix < ProfileStore.NowUnix);
            HubUI.Section(mine, "List something (fee 5%)");
            var items = new List<HubItemRef>();
            HubKit.Collect(items, null, false, false, true);
            foreach (HubItemRef r in items)
            {
                if (!Allowed(r.Stack, out _)) continue;
                int value = Money.BaseValue(r.Def);
                int price = Mathf.RoundToInt(value * 1.15f);
                RectTransform row = HubUI.ItemRow(mine, r.Stack, Tip, 0, null, 38f);
                HubUI.Cell(row, Money.Format(price), 100f, 14);
                HubItemRef pick = r;
                HubUI.Btn(row, "List", () =>
                {
                    if (CountMine(c) >= MaxListings) { Report(false, "You can only have " + MaxListings + " listings."); return; }
                    int fee = Mathf.Max(1, Mathf.RoundToInt(price * 0.05f));
                    if (!Money.TrySpend(fee, Carried)) { Report(false, "You can't cover the fee."); return; }
                    var save = ProfileStore.ToSave(pick.Stack);
                    save.Count = 1;
                    pick.Owner.Remove(pick.Stack, 1);
                    c.Listings.Add(new FleaListing { Id = "m" + ProfileStore.NowUnix, Seller = c.Name, Item = save, Price = price, Mine = true,
                        ListedUnix = ProfileStore.NowUnix, ExpiresUnix = ProfileStore.NowUnix + 3600 * 24 });
                    ProfileStore.CommitStash();
                    Report(true, "Listed for " + Money.Format(price) + ".");
                    Refresh();
                }, 60f, 30f, null, count < MaxListings);
            }
        }

        private static int CountMine(CharacterProfile c) => c.Listings.FindAll(l => l.Mine && !l.Sold).Count;

        // Offline the market is simulated: runners list a few things, and fairly priced listings of yours sell.
        private static void Settle(CharacterProfile c)
        {
            long now = ProfileStore.NowUnix;
            foreach (FleaListing l in c.Listings)
            {
                if (!l.Mine || l.Sold) continue;
                ItemDef d = ItemDatabase.Get(l.Item.Id);
                float fair = d != null ? Money.BaseValue(d) * 1.3f : 0f;
                if (now - l.ListedUnix > 600 && l.Price <= fair) { l.Sold = true; Money.Give(l.Price); }
            }
            c.Listings.RemoveAll(l => !l.Mine && (l.Sold || l.ExpiresUnix < now));
            int others = c.Listings.FindAll(l => !l.Mine).Count;
            var rng = new System.Random((int)(now / 1800));
            var pool = new List<string>(ItemDatabase.AllIds);
            for (int i = others; i < 12 && pool.Count > 0; i++)
            {
                string id = pool[rng.Next(pool.Count)];
                ItemDef d = ItemDatabase.Get(id);
                if (d == null || ArtifactCatalog.IsArtifact(d) || id == Money.Id || Money.BaseValue(d) <= 0) continue;
                c.Listings.Add(new FleaListing
                {
                    Id = "n" + now + "_" + i, Seller = HumanNames.Roll(Factions.Freehold, rng),
                    Item = new StackSave { Id = id, Count = Mathf.Max(1, Mathf.Min(d.MaxStack, 1 + rng.Next(3))) },
                    Price = Mathf.RoundToInt(Money.BaseValue(d) * (0.95f + (float)rng.NextDouble() * 0.7f)),
                    ListedUnix = now, ExpiresUnix = now + 3600 * 3,
                });
            }
        }
    }
}
