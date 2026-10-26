using System.Collections.Generic;
using UnityEngine;
using System;

namespace CloneZ
{
    /// <summary>
    /// Buying, bartering and selling against the active character's stash, and the loyalty that gates what each
    /// trader will show you. Bought goods are never found-in-raid; barter goods come out of the stash first and
    /// then the pack (when one is given).
    /// </summary>
    public static class Trading
    {
        /// <summary>The highest loyalty level the character currently qualifies for with a trader.</summary>
        /// <summary>Computes the highest loyalty tier a character currently reaches with a specific trader.</summary>
    public static int LoyaltyOf(CharacterProfile c, TraderDef t)
        {
            if (c == null || t == null) return 1;
            TraderState s = c.Trader(t.Id);
            int best = 1;
            for (int i = 0; i < t.Levels.Count; i++)
            {
                LoyaltyLevel l = t.Levels[i];
                if (c.Level >= l.MinCharacterLevel && s.Rep >= l.MinRep && s.Spent >= l.MinSpent) best = Mathf.Max(best, l.Level);
            }
            s.Loyalty = best;
            return best;
        }

        /// <summary>What the next loyalty level needs, as a line of text (null at the top level).</summary>
        /// <summary>Generates a description of what the next loyalty level requires, or null if already at max.</summary>
            public static string NextLevelNeeds(CharacterProfile c, TraderDef t)
        {
            int cur = LoyaltyOf(c, t);
            LoyaltyLevel next = t.Levels.Find(l => l.Level == cur + 1);
            if (next == null) return null;
            TraderState s = c.Trader(t.Id);
            return "LL" + next.Level + ": level " + next.MinCharacterLevel + " (" + c.Level + "), standing " + next.MinRep.ToString("0.00")
                + " (" + s.Rep.ToString("0.00") + "), spent " + Money.Format(next.MinSpent) + " (" + Money.Format(s.Spent) + ")";
        }

        /// <summary>Determines whether a trader's offer is currently visible to the character.</summary>
/// <summary>Returns true if the offer is currently visible to the character (loyalty, quest, item availability).</summary>
    public static bool Visible(CharacterProfile c, TraderDef t, TraderOffer o)
        {
            if (o.Loyalty > LoyaltyOf(c, t)) return false;
            if (!string.IsNullOrEmpty(o.Quest))
            {
                QuestState q = c.Quest(o.Quest);
                if (q == null || q.Status != QuestState.Done) return false;
            }
            return string.IsNullOrEmpty(o.ItemId) || ItemDatabase.Get(o.ItemId) != null;
        }

        /// <summary>Units left of an offer, restocking on the trader's timer.</summary>
        /// <summary>Returns the amount of stock left for a trader's offer, resetting on restock timer.</summary>
public static int StockLeft(CharacterProfile c, TraderDef t, TraderOffer o)
        {
            TraderState s = c.Trader(t.Id);
            if (s.Stock == null) s.Stock = new List<OfferStock>();
            long now = ProfileStore.NowUnix;
            if (now >= s.RestockUnix)
            {
                s.Stock.Clear();
                s.RestockUnix = now + (long)(t.RestockHours * 3600f);
            }
            OfferStock st = s.Stock.Find(x => x.OfferId == o.Id);
            if (st == null) { st = new OfferStock { OfferId = o.Id, Left = o.Stock }; s.Stock.Add(st); }
            return st.Left;
        }

        /// <summary>Returns a human‑readable time until the trader's stock refreshes.</summary>
public static string RestockIn(CharacterProfile c, TraderDef t)
        {
            long secs = Math.Max(0, c.Trader(t.Id).RestockUnix - ProfileStore.NowUnix);
            if (secs <= 0) return "now";
            return secs >= 3600 ? (secs / 3600) + "h " + (secs % 3600 / 60) + "m" : (secs / 60) + "m " + (secs % 60) + "s";
        }

        /// <summary>Whether an offer can be taken right now (money, barter goods, stock), and why not.</summary>
        /// <summary>Determines if the character can currently purchase the offer (stock, money, barter).</summary>
public static bool CanBuy(CharacterProfile c, TraderDef t, TraderOffer o, PlayerInventory carried, out string reason)
        {
            reason = null;
            if (!Visible(c, t, o)) { reason = "Not available"; return false; }
            if (StockLeft(c, t, o) <= 0) { reason = "Sold out until restock"; return false; }
            if (o.Price > 0 && Money.Available(carried) < o.Price) { reason = "Not enough money"; return false; }
            if (o.Barter != null)
                foreach ((string id, int n) in o.Barter)
                    if (CountOwned(id, carried) < n) { reason = "Need " + n + "x " + NameOf(id); return false; }
            return true;
        }

        /// <summary>Attempts to buy the given offer, updating inventory, stock and trader reputation. Returns success and a message.</summary>
public static bool Buy(CharacterProfile c, TraderDef t, TraderOffer o, PlayerInventory carried, out string message)
        {
            // Verify purchase eligibility and obtain failure reason
            if (!CanBuy(c, t, o, carried, out message)) return false;
            // Deduct money if the offer has a price and ensure the player can afford it
            if (o.Price > 0 && !Money.TrySpend(o.Price, carried)) { message = "Not enough money"; return false; }
            // Remove required barter items from the player's inventory
            if (o.Barter != null)
                foreach ((string id, int n) in o.Barter) TakeOwned(id, n, carried);

            // Add purchased item(s) to the player's stash (doesn't check weight limits)
            ItemDef def = ItemDatabase.Get(o.ItemId);
            Inventory stash = ProfileStore.StashInventory;
            int count = Mathf.Max(1, o.Count);
            stash.Add(def, count, enforceWeight: false);
            // Ensure the trader's stock list exists and decrement the remaining stock for this offer
            TraderState s = c.Trader(t.Id);
            if (s.Stock == null) s.Stock = new List<OfferStock>();
                OfferStock st = s.Stock.Find(x => x.OfferId == o.Id);
                if (st == null) { st = new OfferStock { OfferId = o.Id, Left = o.Stock }; s.Stock.Add(st); }
                st.Left = Mathf.Max(0, st.Left - 1);
            // Update trader reputation based on money spent (capped to avoid runaway values)
            s.Spent += o.Price;
            s.Rep = Mathf.Clamp(s.Rep + Mathf.Min(0.01f, o.Price / 400000f), -1f, 1f);
            // Build user‑facing confirmation that the item was added to the stash
            message = "Bought " + def.DisplayName + (count > 1 ? " x" + count : "") + " \u2014 it's in your stash.";
            ProfileStore.Save();
            return true;
        }

        /// <summary>Determines whether the trader will buy a given item (based on level, category, artifact flag, etc.).</summary>
/// <summary>Determines whether the trader will buy a given item (based on level, category, artifact flag, etc.).</summary>
public static bool WillBuy(TraderDef t, ItemDef d)
        {
            if (t == null || d == null || d.Id == Money.Id) return false;
            if (ArtifactCatalog.IsArtifact(d)) return t.BuysArtifacts;
            if (t.Buys == null) return true;
            for (int i = 0; i < t.Buys.Length; i++) if (t.Buys[i] == d.Category) return true;
            return false;
        }

        /// <summary>What a trader pays for one unit of a stack (worn gear pays less; found-in-raid a little more).</summary>
        /// <summary>Calculates how many currency units a trader will pay for one unit of the given stack.</summary>
/// <summary>Calculates how many currency units a trader will pay for one unit of the given stack.</summary>
public static int SellPrice(TraderDef t, ItemStack s)
        {
            if (s == null || s.Def == null || !WillBuy(t, s.Def)) return 0;
            float v = Money.BaseValue(s.Def);
            v *= ArtifactCatalog.IsArtifact(s.Def) ? t.ArtifactRatio : t.BuyRatio;
            if (s.Def.IsDamageable && !s.Def.UsesCharge) v *= Mathf.Lerp(0.25f, 1f, s.Condition01);
            if (s.IsFoundInRaid()) v *= 1.1f;
            return Mathf.Max(1, Mathf.RoundToInt(v));
        }

        /// <summary>Sells <paramref name="count"/> of a stack from <paramref name="from"/>. Returns the money received.</summary>
        /// <summary>Sells a number of items from an inventory to a trader, returning the total money earned.</summary>
/// <summary>Sells a number of items from an inventory to a trader, returning the total money earned.</summary>
public static int Sell(CharacterProfile c, TraderDef t, Inventory from, ItemStack s, int count, out string message)
        {
            // Guard against null trader definition
            if (t == null)
            {
                message = "Unknown trader";
                return 0;
            }
            message = null;
            int unit = SellPrice(t, s);
            if (unit <= 0 || from == null || !from.Stacks.Contains(s))
            {
                // Use trader name if available, otherwise fallback
                string traderName = t != null ? t.Name : "Trader";
                message = traderName + " won't buy that.";
                return 0;
            }
            count = Mathf.Clamp(count, 1, s.Count);
            ItemDef def = s.Def;
            int total = unit * count;
            from.Remove(s, count);
            Money.Give(total);
            TraderState st = c.Trader(t.Id);
            st.Rep = Mathf.Clamp(st.Rep + Mathf.Min(0.005f, total / 800000f), -1f, 1f);
            message = "Sold " + def.DisplayName + (count > 1 ? " x" + count : "") + " for " + Money.Format(total) + ".";

            if (ArtifactCatalog.IsArtifact(def))
            {
                if (t.Identifies)
                {
                    int learned = ArtifactCatalog.LearnAll(def.Id);
                    if (learned > 0) message += " Dr. Brandt's notes reveal " + learned + " propert" + (learned == 1 ? "y" : "ies") + ".";
                    Factions.AdjustReputation(Factions.Institute, 15f, null);
                }
                if (!t.Legal)
                {
                    c.Heat += 0.18f * count;
                    message += " The wrong people will hear about this.";
                }
            }
            ProfileStore.Save();
            return total;
        }

        #region Owned goods (stash, then pack)
        /// <summary>Counts how many of a given item id the player owns across stash and carried inventory.</summary>
/// <summary>Counts how many of a given item id the player owns across stash and carried inventory.</summary>
public static int CountOwned(string id, PlayerInventory carried)
            => Count(ProfileStore.StashInventory, id) + (carried != null ? Count(carried.Inventory, id) : 0);

        /// <summary>Counts how many of a given item id are present in the specified inventory.</summary>
/// <summary>Counts how many of a given item id are present in the specified inventory.</summary>
public static int Count(Inventory inv, string id)
        {
            if (inv == null) return 0;
            int n = 0;
            for (int i = 0; i < inv.Stacks.Count; i++) if (inv.Stacks[i].Def != null && inv.Stacks[i].Def.Id == id) n += inv.Stacks[i].Count;
            return n;
        }

        /// <summary>Attempts to take a quantity of an item from the player's stash and carried inventory, returning the amount actually taken.</summary>
/// <summary>Attempts to take a quantity of an item from the player's stash and carried inventory, returning the amount actually taken.</summary>
public static int TakeOwned(string id, int count, PlayerInventory carried)
        {
            int left = Take(ProfileStore.StashInventory, id, count);
            if (left > 0 && carried != null) left = Take(carried.Inventory, id, left);
            return count - left;
        }

        /// <summary>Removes up to a given count of the specified item id from the inventory, returning how many remain to take.</summary>
/// <summary>Removes up to a given count of the specified item id from the inventory, returning how many remain to take.</summary>
public static int Take(Inventory inv, string id, int count)
        {
            if (inv == null) return count;
            for (int i = inv.Stacks.Count - 1; i >= 0 && count > 0; i--)
            {
                ItemStack s = inv.Stacks[i];
                if (s.Def == null || s.Def.Id != id) continue;
                int t = Mathf.Min(count, s.Count);
                inv.Remove(s, t);
                count -= t;
            }
            return count;
        }

        /// <summary>Gets the display name of an item id, falling back to the id if not found.</summary>
/// <summary>Gets the display name of an item id, falling back to the id if not found.</summary>
public static string NameOf(string id) { ItemDef d = ItemDatabase.Get(id); return d != null ? d.DisplayName : id; }
        #endregion
    }
}
