using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>The stash: move things between the stash shelves and what you carry. The secure pouch survives death.</summary>
    public sealed class StashScreen : HubScreenBase
    {
        protected override string Title => "Stash — " + ProfileStore.StashInventory.Stacks.Count + " stacks";
        private readonly List<HubItemRef> _items = new List<HubItemRef>();

        protected override void BuildContent()
        {
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f);
            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Stash (rows " + Character.StashRows + ")", 20);
            RectTransform stash = HubUI.Scroll(cols[0], 40f, 0f, 3f);
            HubKit.Collect(_items, null, false, false, true);
            int i = 0;
            foreach (HubItemRef r in _items)
            {
                RectTransform row = HubUI.ItemRow(stash, r.Stack, Tip, i++, null, 38f);
                HubItemRef pick = r;
                HubUI.Btn(row, "Take", () => Move(pick, HubKit.Pack, false), 60f);
                if (r.Stack.Count > 1) HubUI.Btn(row, "All", () => Move(pick, HubKit.Pack, true), 46f);
            }

            HubUI.Card(cols[1]);
            HubUI.AreaHeading(cols[1], "Carried (into the next raid)", 20);
            RectTransform kit = HubUI.Scroll(cols[1], 40f, 0f, 3f);
            HubKit.Collect(_items, null, false, true, false);
            i = 0;
            foreach (HubItemRef r in _items)
            {
                bool pouch = HubKit.InPouch(r.Stack);
                RectTransform row = HubUI.ItemRow(kit, r.Stack, Tip, i++, null, 38f, pouch ? "  <size=12><color=#8fd18b>secure pouch</color></size>" : null);
                HubItemRef pick = r;
                HubUI.Btn(row, "Store", () => Move(pick, ProfileStore.StashInventory, true), 60f);
            }
            HubUI.Note(kit, "Everything carried is at risk in a raid — except what's in the secure pouch, and what you insured (it comes back if nobody takes it).", UIKit.TextDim);
            HubUI.Note(kit, "Use the inventory (I) to wear gear and arrange the pack and pouch.", UIKit.TextDim);
        }

        private void Move(HubItemRef r, Inventory to, bool all)
        {
            if (to == null || !r.StillValid) { Refresh(); return; }
            int n = all ? r.Stack.Count : 1;
            int moved = r.Owner.TransferTo(to, r.Stack, n, to != ProfileStore.StashInventory);
            if (moved <= 0) Report(false, "No room.");
            ProfileStore.CommitStash();
            HubKit.AfterChange();
            Click();
            Refresh();
        }
    }

    /// <summary>The medic: patches limbs, sets bones, tops up blood, flushes radiation and treats illness — for money.</summary>
    public sealed class MedicScreen : HubScreenBase
    {
        protected override string Title => "Sister Agathe — treatment";

        protected override void BuildContent()
        {
            RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.2f, 0.05f, 0.8f, 0.95f), 8f, 16);
            HubUI.Card((RectTransform)col.parent);
            Actor me = Actor.LocalPlayer;
            var limbs = me != null ? me.GetComponent<PlayerLimbs>() : null;
            var blood = me != null ? me.GetComponent<PlayerBlood>() : null;
            var rad = me != null ? me.GetComponent<PlayerRadiation>() : null;
            var dis = me != null ? me.GetComponent<PlayerDiseases>() : null;
            var stats = me != null ? me.GetComponent<PlayerStats>() : null;
            HubUI.Heading(col, "What ails you", 22);

            float missing = 0f;
            if (limbs != null) foreach (BodyZone z in BodyZones.All) { missing += limbs.Max(z) - limbs.Hp(z); if (limbs.IsFractured(z)) missing += 60f; }
            int limbCost = Mathf.RoundToInt(missing * 4f);
            Treat(col, "Patch wounds and set bones", limbCost, missing > 0.5f, () => { limbs.FullHeal(); if (stats != null) stats.Heal(100f); });

            float bloodMissing = blood != null ? PlayerBlood.MaxBlood - blood.Blood : 0f;
            Treat(col, "Transfusion (top up blood)", Mathf.RoundToInt(bloodMissing * 0.5f), bloodMissing > 50f, () => blood.Blood = PlayerBlood.MaxBlood);

            float dose = rad != null ? rad.Dose : 0f;
            Treat(col, "Flush radiation (" + (dose * 6f).ToString("0.00") + " Sv)", Mathf.RoundToInt(dose * 2400f), dose > 0.01f, () => rad.Purge(1f));

            bool ill = dis != null && (dis.Has(PlayerDiseases.Cholera) || dis.Has(PlayerDiseases.Cold));
            Treat(col, "Treat illness", 450, ill, () => { dis.Cure(PlayerDiseases.Cholera, "Cholera treated."); dis.Cure(PlayerDiseases.Cold, "Cold treated."); });

            Treat(col, "Settle the stomach", 150, stats != null && stats.Sickness > 5f, () => stats.Sickness = 0f);
            HubUI.Para(col, "Nothing she can do for the Zone's marks. Those are yours now.", 14, UIKit.TextDim, FontStyle.Italic);
            RectTransform row = HubUI.ButtonRow(col);
            ServiceButton(row, "Trade", HubScreens.Trader, TraderCatalog.Medic);
            ServiceButton(row, "Insurance", HubScreens.Insurance);
        }

        private void Treat(Transform col, string what, int cost, bool needed, System.Action heal)
        {
            RectTransform row = HubUI.Row(col, 44f, new Color(1f, 1f, 1f, 0.04f));
            HubUI.Cell(row, what, 420f, 16, needed ? UIKit.TextColor : UIKit.TextDim);
            HubUI.Cell(row, needed ? HubUI.NeedMoney(cost, Carried) : "—", 140f, 15);
            HubUI.Btn(row, "Treat", () =>
            {
                if (!Money.TrySpend(cost, Carried)) { Report(false, "You can't pay for that."); return; }
                heal();
                var limbs = Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerLimbs>() : null;
                if (limbs != null) limbs.SaveToProfile();
                HubKit.AfterChange();
                Report(true, "Done. Try not to come back like that.");
                Refresh();
            }, 90f, 32f, null, needed);
        }
    }

    /// <summary>Insurance: pay a premium on gear you carry; if you die and nobody loots it, it comes back after a couple of days.</summary>
    public sealed class InsuranceScreen : HubScreenBase
    {
        protected override string Title => "Insurance";
        private readonly List<HubItemRef> _items = new List<HubItemRef>();

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1.2f, 1f);
            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Insure what you carry", 20);
            RectTransform list = HubUI.Scroll(cols[0], 40f, 0f, 3f);
            HubKit.Collect(_items, null, true, true, false);
            int i = 0;
            foreach (HubItemRef r in _items)
            {
                if (!InsuranceDesk.Insurable(r.Stack)) continue;
                bool insured = r.Stack.Extra != null && r.Stack.Extra.Insured;
                RectTransform row = HubUI.ItemRow(list, r.Stack, Tip, i++, null, 38f, insured ? "  <size=12><color=#8fd18b>insured</color></size>" : null);
                if (insured) continue;
                HubUI.Cell(row, HubUI.NeedMoney(InsuranceDesk.Premium(r.Stack), Carried), 110f, 14);
                HubItemRef pick = r;
                HubUI.Btn(row, "Insure", () =>
                {
                    bool ok = InsuranceDesk.Insure(pick.Stack, Carried, out string msg);
                    if (ok) { pick.Commit(); HubKit.AfterChange(); }
                    Report(ok, msg);
                    Refresh();
                }, 80f);
            }
            HubUI.Card(cols[1]);
            RectTransform side = HubUI.Stack(cols[1], 8f, 14);
            HubUI.Heading(side, "Claims", 20);
            int ready = InsuranceDesk.ReadyClaims(c);
            HubUI.Para(side, c.Insurance.Count + " claim(s) open, " + ready + " ready to collect. Claims return " + SandboxSettings.Current.InsuranceReturnDays + " days after you lose the gear — unless someone else picked it up.", 15);
            HubUI.Btn(side, "Collect returned gear", () =>
            {
                int n = InsuranceDesk.Collect(c);
                ProfileStore.CommitStash();
                Report(n > 0, n > 0 ? n + " item(s) back in your stash." : "Nothing has come back yet.");
                Refresh();
            }, 260f, 36f, null, ready > 0);
            HubUI.Heading(side, "Bodies in the Zone", 18);
            foreach (CorpseRecord r in c.Corpses)
                if (!r.Recovered && !r.Looted) HubUI.Note(side, "Your body in " + ZoneCatalog.Get(r.ZoneId).Name + " (" + r.Items.Count + " items) — go and get it.");
        }
    }

    /// <summary>
    /// The technician: repairs (each repair restores a little less), and branching upgrades for guns and armour —
    /// one path per tier, so every build is a choice. Mods are fitted from the inventory (or stripped off found guns).
    /// </summary>
    public sealed class TechnicianScreen : HubScreenBase
    {
        protected override string Title => "Kasimir — repairs and upgrades";
        private readonly List<HubItemRef> _items = new List<HubItemRef>();
        private HubItemRef _sel;

        protected override void BuildContent()
        {
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1.2f);
            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Your gear", 20);
            RectTransform list = HubUI.Scroll(cols[0], 40f, 0f, 3f);
            HubKit.Collect(_items, d => d.IsFirearm || Ballistics.Armor(d.Id) != null || (d.IsWeapon && d.IsDamageable), true, true, true);
            int i = 0;
            foreach (HubItemRef r in _items)
            {
                RectTransform row = HubUI.ItemRow(list, r.Stack, Tip, i++, null, 38f, "  <size=12><color=#9aa0a8>" + r.SourceLabel + "</color></size>");
                HubItemRef pick = r;
                HubUI.Btn(row, "Select", () => { _sel = pick; Refresh(); }, 70f);
            }

            HubUI.Card(cols[1]);
            RectTransform side = HubUI.Scroll(cols[1], 0f, 0f, 6f);
            if (_sel == null || !_sel.StillValid) { HubUI.Para(side, "Pick a gun or a piece of armour.", 16, UIKit.TextDim); return; }
            ItemStack s = _sel.Stack;
            HubUI.Heading(side, s.Def.DisplayName, 22);
            HubUI.Para(side, HubUI.StackTooltip(s), 14);

            if (s.Def.IsDamageable && s.Def.MaxCondition > 0 && s.Condition < s.Def.MaxCondition)
            {
                float cap = Mathf.Max(0.5f, 1f - 0.08f * s.TimesRepaired);
                int target = Mathf.RoundToInt(s.Def.MaxCondition * cap);
                int cost = Mathf.Max(20, Mathf.RoundToInt(Money.BaseValue(s.Def) * 0.35f * (1f - s.Condition01)));
                RectTransform row = HubUI.Row(side, 44f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, "Repair to " + HubUI.Pct((float)target / s.Def.MaxCondition) + " (repaired " + s.TimesRepaired + "x)", 360f, 15);
                HubUI.Cell(row, HubUI.NeedMoney(cost, Carried), 110f, 14);
                HubUI.Btn(row, "Repair", () =>
                {
                    if (!Money.TrySpend(cost, Carried)) { Report(false, "Not enough money."); return; }
                    s.Condition = Mathf.Max(s.Condition, target);
                    s.TimesRepaired++;
                    _sel.Commit();
                    ProfileStore.CommitStash();
                    HubKit.AfterChange();
                    Report(true, "Good as it'll get.");
                    Refresh();
                }, 90f);
            }

            bool armor = !s.Def.IsFirearm;
            for (int tier = 1; tier <= 3; tier++)
            {
                var opts = new List<WeaponUpgrades.Node>();
                WeaponUpgrades.TierOptions(armor, tier, opts);
                if (opts.Count == 0) continue;
                HubUI.Section(side, "Tier " + tier + (opts.Count > 1 ? " — choose one" : ""));
                foreach (WeaponUpgrades.Node n in opts)
                {
                    bool has = s.Extra != null && s.Extra.Upgrades.Contains(n.Id);
                    bool can = WeaponUpgrades.CanApply(s, n, out string why);
                    RectTransform row = HubUI.Row(side, 50f, has ? new Color(0.3f, 0.6f, 0.3f, 0.15f) : new Color(1f, 1f, 1f, 0.04f));
                    HubUI.Cell(row, n.Name + "\n<size=13><color=#9aa0a8>" + n.Effect + (n.Part != null ? " — needs " + Trading.NameOf(n.Part) : "") + "</color></size>", 380f, 15);
                    HubUI.Cell(row, has ? "fitted" : HubUI.NeedMoney(n.Cost, Carried), 110f, 14);
                    if (has) continue;
                    WeaponUpgrades.Node pick = n;
                    HubUI.Btn(row, "Fit", () =>
                    {
                        if (n.Part != null && Trading.CountOwned(n.Part, Carried) <= 0) { Report(false, "Needs " + Trading.NameOf(n.Part)); return; }
                        if (!Money.TrySpend(pick.Cost, Carried)) { Report(false, "Not enough money."); return; }
                        if (pick.Part != null) Trading.TakeOwned(pick.Part, 1, Carried);
                        if (s.Extra == null) s.Extra = new ItemExtra();
                        s.Extra.Upgrades.Add(pick.Id);
                        _sel.Commit();
                        ProfileStore.CommitStash();
                        HubKit.AfterChange();
                        Report(true, pick.Name + " fitted. That path is chosen now.");
                        Refresh();
                    }, 70f, 30f, null, can);
                }
            }
        }
    }

    /// <summary>The guide: travel between the hubs (a fee; the first trip unlocks the place), and the way into each Zone.</summary>
    public sealed class GuideScreen : HubScreenBase
    {
        protected override string Title => "Old Lev — the roads between";

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.15f, 0.05f, 0.85f, 0.95f), 8f, 16);
            HubUI.Card((RectTransform)col.parent);
            HubUI.Heading(col, "You are at " + HubCatalog.Get(c.Hub).Name, 22);
            foreach (HubDef h in HubCatalog.All)
            {
                bool here = h.Id == c.Hub, known = c.UnlockedHubs.Contains(h.Id);
                int cost = known ? h.TravelCost : h.PassageCost;
                RectTransform row = HubUI.Row(col, 64f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, h.Name + "\n<size=13><color=#9aa0a8>" + h.Blurb + "</color></size>", 620f, 16);
                if (here) { HubUI.Cell(row, "You're here", 140f, 14, UIKit.Good); continue; }
                bool can = c.Level >= h.MinLevel;
                HubUI.Cell(row, can ? HubUI.NeedMoney(cost, Carried) : "Level " + h.MinLevel, 120f, 14);
                HubDef pick = h;
                HubUI.Btn(row, known ? "Travel" : "Unlock", () =>
                {
                    if (!Money.TrySpend(cost, Carried)) { Report(false, "Not enough money."); return; }
                    if (!c.UnlockedHubs.Contains(pick.Id)) c.UnlockedHubs.Add(pick.Id);
                    c.Hub = pick.Id;
                    HubKit.AfterChange();
                    GameFlow.GoToHub();
                }, 100f, 32f, null, can);
            }
            HubUI.Para(col, "Each hub sits nearest one Zone: going in from there is cheaper (see the door).", 14, UIKit.TextDim);
        }
    }

    /// <summary>
    /// The door: pick a Zone (each with its own rules), how you go in (on foot through the cordon — buy a pass or risk
    /// the belt — or by helicopter to a landing zone you choose), and as whom (your runner, a scavenger with borrowed
    /// kit, the open Zone with no clock, or — on servers that allow it — something that hunts runners).
    /// </summary>
    public sealed class DeployScreen : HubScreenBase
    {
        protected override string Title => "Into the Zone";
        private static string s_Zone = ZoneCatalog.DefaultZone;
        private static int s_Lz = -1;
        private static RaidKind s_Kind = RaidKind.Operative;
        private static bool s_Heli;

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1.2f, 1f);
            HubUI.Card(cols[0]);
            RectTransform zones = HubUI.Scroll(cols[0], 0f, 0f, 6f);
            HubUI.Heading(zones, "Choose a Zone", 22);
            foreach (ZoneDef z in ZoneCatalog.All)
            {
                bool ok = c.Level >= z.MinLevel;
                RectTransform card = HubUI.CardColumn(zones, s_Zone == z.Id ? new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.18f) : new Color(1f, 1f, 1f, 0.04f));
                HubUI.Heading(card, z.Name + "  <size=14><color=#9aa0a8>depth " + z.Depth + (ok ? "" : " — level " + z.MinLevel) + "</color></size>", 19);
                HubUI.Para(card, z.Blurb, 14);
                HubUI.Para(card, "<i>" + z.Rule + "</i>", 14, UIKit.TextDim);
                ZoneDef pick = z;
                HubUI.Btn(HubUI.ButtonRow(card), "Pick", () => { s_Zone = pick.Id; Click(); Refresh(); }, 120f, 30f, null, ok);
            }

            HubUI.Card(cols[1]);
            RectTransform side = HubUI.Stack(cols[1], 8f, 14);
            ZoneDef zone = ZoneCatalog.Get(s_Zone);
            HubUI.Heading(side, "How you go in", 20);
            string[] kinds = { "Your runner (everything carried is at risk)", "Scavenger run (borrowed kit, nothing to lose)", "Open Zone (no clock: stay as long as you last)", "Hunter (play one of the things in the dark)" };
            HubUI.Cycle(side, "Run", kinds, (int)s_Kind, v => { s_Kind = (RaidKind)v; Refresh(); });
            if (s_Kind == RaidKind.Monster && !ServerRules.AllowPlayerMonsters) HubUI.Note(side, "This server doesn't allow hunters.", UIKit.Bad);
            if (s_Kind == RaidKind.Scav && ProfileStore.NowUnix < c.ScavReadyUnix)
                HubUI.Note(side, "Scav run ready in " + HubUI.Duration(c.ScavReadyUnix - ProfileStore.NowUnix), UIKit.Warn);

            int heliCost = 400 + zone.Depth * 350;
            HubUI.Cycle(side, "Insertion", new[] { "On foot (through the cordon)", "Helicopter (" + Money.Format(heliCost) + ")" }, s_Heli ? 1 : 0, v => { s_Heli = v == 1; Refresh(); });
            if (s_Heli) HubUI.Cycle(side, "Landing zone", new[] { "Random", "North", "East", "South", "West" }, s_Lz + 1, v => { s_Lz = v - 1; Refresh(); });
            else if (zone.HasCordon)
            {
                HubUI.Para(side, CordonRules.PassBought ? "You have a pass for the gate." : "Without a pass, the Wardens shoot anyone in the restricted belt. A pass costs " + Money.Format(CordonGate.Bribe) + " at the gate (or a Zone Permit).", 14,
                    CordonRules.PassBought ? UIKit.Good : UIKit.Warn);
            }
            if (CompanionService.HasHired(out string mate)) HubUI.Para(side, mate + " goes in with you.", 14, UIKit.Good);
            HubUI.Para(side, "Every trip costs something. You lose what you carry if you die — never what you learned, never your pouch.", 14, UIKit.TextDim, FontStyle.Italic);

            bool canGo = c.Level >= zone.MinLevel && !(s_Kind == RaidKind.Monster && !ServerRules.AllowPlayerMonsters)
                && !(s_Kind == RaidKind.Scav && ProfileStore.NowUnix < c.ScavReadyUnix);
            HubUI.Btn(side, "Go — " + zone.Name, () =>
            {
                if (s_Heli && s_Kind != RaidKind.Monster && !Money.TrySpend(heliCost, Carried)) { Report(false, "You can't pay the pilot."); return; }
                HubKit.AfterChange();
                if (s_Kind == RaidKind.Scav) c.ScavReadyUnix = ProfileStore.NowUnix + SandboxSettings.Current.ScavCooldownMinutes * 60L;
                GameFlow.Deploy(zone.Id, s_Heli ? s_Lz : RaidWorldRules.OnFoot, s_Kind);
            }, 320f, 44f, UIKit.Accent, canGo, 18);
        }
    }

    /// <summary>The barkeep's rumours: pay for a tip that points at something real (an artifact field, an anomaly's rule, a stash).</summary>
    public sealed class RumoursScreen : HubScreenBase
    {
        protected override string Title => "Mott — rumours";

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            AccountProfile a = ProfileStore.Account;
            RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.2f, 0.1f, 0.8f, 0.9f), 10f, 16);
            HubUI.Card((RectTransform)col.parent);
            HubUI.Heading(col, "\"Information costs. Wrong information costs more.\"", 20);
            var rng = new System.Random(c.Day * 97 + c.Raids);
            Rumour(col, "How an anomaly behaves", 250, () =>
            {
                var ids = new List<string>(AnomalyCatalog.All.Keys);
                ids.RemoveAll(id => a.Knows("anomaly_rule:" + id));
                if (ids.Count == 0) return "You already know everything I do about them.";
                AnomalyDef d = AnomalyCatalog.Get(ids[rng.Next(ids.Count)]);
                a.Learn("anomaly_rule:" + d.Id);
                return d.Name + ": " + d.Rule;
            });
            Rumour(col, "What an artifact does", 400, () =>
            {
                var defs = new List<ArtifactCatalog.Def>(ArtifactCatalog.All);
                ArtifactCatalog.Def d = defs[rng.Next(defs.Count)];
                foreach (string p in d.Properties)
                    if (!ArtifactCatalog.IsKnown(d.Id, p)) { ArtifactCatalog.Learn(d.Id, p, false); return d.Name + ": " + ArtifactCatalog.Prop(p).Label; }
                return "Nothing you don't know about the " + d.Name + ".";
            });
            Rumour(col, "Where a stash is buried", 700, () =>
            {
                var stack = new ItemStack(ItemDatabase.Get("pda"), 1) { Extra = new ItemExtra { Note = "pda:" + rng.Next() } };
                ItemActions.TryUse(HubKit.Carried, stack);
                return "Heard it from a runner who didn't come back. It's in your notebook.";
            });
            Rumour(col, "Where the Lodestar is", 3000, () =>
            {
                if (a.Lodestar.SiteRevealed) return "You know as much as I do. The bottom of the Spillway.";
                a.Lodestar.Clues.Add("rumour");
                return a.Lodestar.Clues.Count >= 3 ? "Below the dam, where the Choir sings. That's all anyone knows." : "Pilgrims burn pages as they go. Find the pages and you find the way.";
            });
        }

        private void Rumour(Transform col, string what, int cost, System.Func<string> reveal)
        {
            RectTransform row = HubUI.Row(col, 44f, new Color(1f, 1f, 1f, 0.04f));
            HubUI.Cell(row, what, 420f, 16);
            HubUI.Cell(row, HubUI.NeedMoney(cost, Carried), 120f, 14);
            HubUI.Btn(row, "Pay", () =>
            {
                if (!Money.TrySpend(cost, Carried)) { Report(false, "Mott shakes his head."); return; }
                string line = reveal();
                GameEvents.RaiseSubtitle("Mott", line);
                HubKit.AfterChange();
                Report(true, line);
                Refresh();
            }, 80f);
        }
    }
}
