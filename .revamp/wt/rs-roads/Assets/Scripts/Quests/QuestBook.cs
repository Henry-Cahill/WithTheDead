using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    public enum ObjectiveKind { Kill, Visit, Extract, Fetch, Key, ExtractWith }

    public sealed class QuestObjective
    {
        public ObjectiveKind Kind;
        /// <summary>Kill: "faction:wardens", "kind:Zombie", "creature:hound", "human". Visit: a landmark id. Extract: a
        /// zone id or "any". Fetch / ExtractWith: an item id, "artifact", "mod" or "mutantpart". Key: a progress key.</summary>
        public string Target;
        public int Count = 1;
        public string Zone;                 // only counts in this zone (null = anywhere)
        public bool FoundInRaid = true;     // fetch: must be found in raid
        public string Text;
    }

    public sealed class QuestDef
    {
        public string Id, Giver, Title, Brief, Requires;
        public int MinLevel = 1;
        public bool Repeatable;
        public readonly List<QuestObjective> Objectives = new List<QuestObjective>();
        public int RewardMoney, RewardXp;
        public float RewardRep = 0.05f;
        public (string Id, int Count)[] RewardItems = new (string, int)[0];
        public string RewardFlag;
    }

    /// <summary>
    /// Jobs from the bar's traders (Tarkov's questlines with found-in-raid hand-ins), a board of repeatable contracts
    /// that refreshes every day (Gray Zone Warfare's replayable tasks), and the tutorial chain that teaches the loop.
    /// Progress flows through <see cref="GameEvents.QuestProgress"/> keys, so a squad's netcode only has to forward
    /// those keys for everyone in the squad to share progress on the same objectives.
    /// </summary>
    public static class QuestBook
    {
        private static readonly Dictionary<string, QuestDef> s_Defs = new Dictionary<string, QuestDef>();
        private static readonly System.Text.StringBuilder _summarySb = new System.Text.StringBuilder();
        public static IEnumerable<QuestDef> All => s_Defs.Values;

        static QuestBook()
        {
            const string bar = TraderCatalog.Barkeep, fence = TraderCatalog.Fence, inst = TraderCatalog.Institute,
                gun = TraderCatalog.Gunsmith, med = TraderCatalog.Medic, guide = TraderCatalog.Guide;

            Q("q_first_steps", bar, "First Steps", "Go in, look around, come back alive. That's the whole job, the first time.", 600, 400)
                .O(ObjectiveKind.Extract, "kestrel", 1, "Get out of Kestrel Flats alive");
            Q("q_bolts", bar, "Feel the Ground", "Throw a bolt before you take a step. Watch what the ground does with it.", 450, 350, "q_first_steps", items: new[] { ("bolt", 20) })
                .O(ObjectiveKind.Key, "bolt_anomaly", 3, "Set off anomalies with thrown bolts");
            Q("q_shelter", bar, "Under a Roof", "When the sky goes red you've got two minutes. Spend them well.", 900, 600, "q_bolts")
                .O(ObjectiveKind.Key, "survive_emission", 1, "Live through an emission in the Zone");
            Q("q_lodestar", bar, "The Light at the Bottom", "Pilgrims talk about a light under the dam that answers wishes. Bring me what they left behind and I'll tell you what I know.", 1500, 1200, "q_shelter", lvl: 8, flag: "lodestar_site")
                .O(ObjectiveKind.Fetch, "lodestar_page", 3, "Hand in burned journal pages", false)
                .O(ObjectiveKind.Fetch, "lodestar_hymnal", 1, "Hand in a Choir hymnal", false)
                .O(ObjectiveKind.Fetch, "lodestar_photo", 1, "Hand in the survey photograph", false);

            Q("q_samples", inst, "Samples", "Anything you pull out of an anomaly, we want to see first. Untouched, if you please.", 2500, 900)
                .O(ObjectiveKind.Fetch, "artifact", 2, "Hand in artifacts found in raid");
            Q("q_readings", inst, "Readings", "Our instruments need placing at two sites. Just stand there. That's all.", 1800, 800, "q_samples")
                .O(ObjectiveKind.Visit, "crashed_helicopter", 1, "Visit the crashed helicopter (Kestrel Flats)")
                .O(ObjectiveKind.Visit, "drowned_chapel", 1, "Visit the drowned chapel (Mirelock Woods)");
            Q("q_star", inst, "The Sunken Star", "Every crew in the Zone will be after it. Bring it to us and we'll pay what it's worth.", 9000, 3000, "q_readings", lvl: 6)
                .O(ObjectiveKind.ExtractWith, "art_sunkenstar", 1, "Extract carrying the Sunken Star");

            Q("q_ledger", fence, "The Ledger", "Somebody took my ledger to the Zone. Names, debts, dates. I want it back unread.", 2200, 700)
                .O(ObjectiveKind.Fetch, "fence_ledger", 1, "Recover the fence's ledger", false);
            Q("q_tax", fence, "Tax Collectors", "The Wardens skim every runner who crosses. Skim them back.", 3200, 1100, "q_ledger")
                .O(ObjectiveKind.Kill, "faction:wardens", 3, "Kill Cordon Wardens");

            Q("q_parts", gun, "Parts Bin", "Strip what you can off the guns you find. I need parts, not stories.", 1600, 600)
                .O(ObjectiveKind.Fetch, "mod", 2, "Hand in weapon attachments found in raid");
            Q("q_pests", gun, "Pest Control", "The dead are thick around the cordon. Thin them.", 1400, 700)
                .O(ObjectiveKind.Kill, "kind:Zombie", 15, "Put down the dead");
            Q("q_outpost", gun, "Hold the Line", "Take an outpost for your people and hold it.", 4000, 1500, "q_pests", lvl: 5)
                .O(ObjectiveKind.Key, "capture_outpost", 1, "Capture a warfare outpost");

            Q("q_supplies", med, "Supplies", "Bandages, kits. Clean ones, from the Zone.", 1200, 500)
                .O(ObjectiveKind.Fetch, "bandage", 3, "Hand in bandages found in raid")
                .O(ObjectiveKind.Fetch, "fieldkit", 1, "Hand in a field medkit found in raid");
            Q("q_mercy", med, "Mercy", "Not everyone with a gun needs to die. Show me you know that.", 1000, 800, "q_supplies")
                .O(ObjectiveKind.Key, "spare", 1, "Let someone who surrendered go");

            Q("q_ground", guide, "Learn the Ground", "A map is paper. The ground is the ground. Walk it.", 1300, 900)
                .O(ObjectiveKind.Visit, "quarantine_tent", 1, "Find the quarantine tent")
                .O(ObjectiveKind.Visit, "warden_checkpoint", 1, "Find the warden checkpoint");
            Q("q_deeper", guide, "Deeper", "Mirelock. The fog doesn't lift. Come back anyway.", 2500, 1400, "q_ground", lvl: 5)
                .O(ObjectiveKind.Extract, "mirelock", 1, "Extract from Mirelock Woods");
            Q("q_bottom", guide, "The Bottom of the Dam", "Few go to the Spillway. Fewer come back.", 6000, 3000, "q_deeper", lvl: 12)
                .O(ObjectiveKind.Extract, "spillway", 1, "Extract from the Spillway");
            Q("q_hunter", guide, "Hunter", "Wolves have been taking the camp's dogs. Bring me proof.", 1500, 800)
                .O(ObjectiveKind.Fetch, "wolf_pelt", 2, "Hand in wolf pelts");
        }

        private sealed class Builder
        {
            public QuestDef D;
            public Builder O(ObjectiveKind k, string target, int n, string text, bool fir = true, string zone = null)
            {
                D.Objectives.Add(new QuestObjective { Kind = k, Target = target, Count = n, Text = text, FoundInRaid = fir, Zone = zone });
                return this;
            }
        }

        private static Builder Q(string id, string giver, string title, string brief, int money, int xp, string requires = null, int lvl = 1,
            (string, int)[] items = null, string flag = null)
        {
            var d = new QuestDef { Id = id, Giver = giver, Title = title, Brief = brief, RewardMoney = money, RewardXp = xp, Requires = requires, MinLevel = lvl, RewardFlag = flag };
            if (items != null) d.RewardItems = items;
            s_Defs[id] = d;
            return new Builder { D = d };
        }

        public static QuestDef Get(string id)
        {
            if (id == null) return null;
            if (s_Defs.TryGetValue(id, out QuestDef d)) return d;
            return id.StartsWith("c_") ? Contract(id) : null;
        }

        #region Contracts
        /// <summary>Today's contract ids (three, refreshed each hub day).</summary>
        public static List<string> TodaysContracts(CharacterProfile c)
        {
            var list = new List<string>();
            int day = c != null ? c.Day : 1;
            for (int i = 0; i < 3; i++) list.Add("c_" + day + "_" + i);
            return list;
        }

        // A contract is rebuilt from its id, so only progress needs saving.
        private static QuestDef Contract(string id)
        {
            string[] p = id.Split('_');
            if (p.Length < 3 || !int.TryParse(p[1], out int day) || !int.TryParse(p[2], out int n)) return null;
            var rng = new System.Random(day * 131 + n * 7);
            string[] zones = { "kestrel", "mirelock", "spillway" };
            string zone = zones[rng.Next(n == 0 ? 1 : 3)];
            ZoneDef z = ZoneCatalog.Get(zone);
            var d = new QuestDef { Id = id, Repeatable = true, RewardRep = 0.02f };
            int kind = rng.Next(5);
            switch (kind)
            {
                case 0:
                    d.Giver = TraderCatalog.Gunsmith; d.Title = "Contract: Cull";
                    d.Objectives.Add(new QuestObjective { Kind = ObjectiveKind.Kill, Target = "kind:Zombie", Count = 8 + rng.Next(10), Zone = zone, Text = "Kill the dead in " + z.Name });
                    break;
                case 1:
                    d.Giver = TraderCatalog.Institute; d.Title = "Contract: Specimens";
                    d.Objectives.Add(new QuestObjective { Kind = ObjectiveKind.Fetch, Target = "mutantpart", Count = 2 + rng.Next(3), Text = "Hand in mutant parts (found in raid)" });
                    break;
                case 2:
                    d.Giver = TraderCatalog.Institute; d.Title = "Contract: Field Survey";
                    string[] marks = { "crashed_helicopter", "quarantine_tent", "drowned_chapel", "hunters_hide", "cooling_tower" };
                    string m = marks[rng.Next(marks.Length)];
                    d.Objectives.Add(new QuestObjective { Kind = ObjectiveKind.Visit, Target = m, Count = 1, Text = "Visit the " + HumanUtil.Pretty(m) });
                    break;
                case 3:
                    d.Giver = TraderCatalog.Fence; d.Title = "Contract: Debt Collection";
                    d.Objectives.Add(new QuestObjective { Kind = ObjectiveKind.Kill, Target = "faction:scavs", Count = 2 + rng.Next(3), Text = "Kill scavengers who owe the Fence" });
                    break;
                default:
                    d.Giver = TraderCatalog.Barkeep; d.Title = "Contract: Out and Back";
                    d.Objectives.Add(new QuestObjective { Kind = ObjectiveKind.Extract, Target = zone, Count = 1, Text = "Extract from " + z.Name });
                    break;
            }
            d.Brief = "A day's work from the board. Paid on delivery.";
            d.RewardMoney = 600 + rng.Next(900) + z.Depth * 500;
            d.RewardXp = 250 + z.Depth * 150;
            return d;
        }
        #endregion

        #region State
        public static bool Available(CharacterProfile c, QuestDef d)
        {
            if (c == null || d == null) return false;
            if (c.Level < d.MinLevel) return false;
            QuestState q = c.Quest(d.Id);
            if (q != null && q.Status != QuestState.Available) return false;
            if (!string.IsNullOrEmpty(d.Requires)) { QuestState r = c.Quest(d.Requires); if (r == null || r.Status != QuestState.Done) return false; }
            return true;
        }

        public static void Accept(CharacterProfile c, QuestDef d)
        {
            QuestState q = c.Quest(d.Id);
            if (q == null) { q = new QuestState { Id = d.Id }; c.Quests.Add(q); }
            q.Status = QuestState.Active;
            q.AcceptedUnix = ProfileStore.NowUnix;
            q.Progress.Clear();
            foreach (QuestObjective o in d.Objectives) q.Progress.Add(0f);
            ProfileStore.Save();
        }

        public static bool ObjectiveDone(QuestDef d, QuestState q, int i) => i < q.Progress.Count && q.Progress[i] >= d.Objectives[i].Count;

        public static bool AllDone(QuestDef d, QuestState q)
        {
            for (int i = 0; i < d.Objectives.Count; i++)
                if (d.Objectives[i].Kind != ObjectiveKind.Fetch && !ObjectiveDone(d, q, i)) return false;
            return true;
        }

        /// <summary>Hands in what the fetch objectives need (found-in-raid where required) and pays out.</summary>
        public static bool TryTurnIn(CharacterProfile c, QuestDef d, PlayerInventory carried, out string message)
        {
            QuestState q = c.Quest(d.Id);
            message = null;
            if (q == null || q.Status != QuestState.Active) return false;
            if (!AllDone(d, q)) { message = "Not finished yet."; return false; }
            var take = new List<(ItemStack s, Inventory from, int n)>();
            foreach (QuestObjective o in d.Objectives)
            {
                if (o.Kind != ObjectiveKind.Fetch) continue;
                int need = o.Count;
                Gather(o, carried != null ? carried.Inventory : null, ref need, take);
                Gather(o, ProfileStore.StashInventory, ref need, take);
                if (need > 0) { message = "Still need " + need + "x " + FetchName(o) + (o.FoundInRaid ? " (found in raid)" : ""); return false; }
            }
            foreach (var t in take) t.from.Remove(t.s, t.n);
            Complete(c, d, q);
            message = "Done: " + d.Title + " — " + Money.Format(d.RewardMoney) + ", " + d.RewardXp + " XP";
            return true;
        }

        private static void Gather(QuestObjective o, Inventory inv, ref int need, List<(ItemStack, Inventory, int)> take)
        {
            // Skip if the objective is zone‑restricted and we are not in that zone.
            if (o.Zone != null && GameSession.ZoneId != o.Zone) return;
            if (inv == null || need <= 0) return;
            foreach (ItemStack s in inv.Stacks)
            {
                if (need <= 0) break;
                if (!Matches(o.Target, s.Def)) continue;
                if (o.FoundInRaid && (s.Extra == null || !s.Extra.FoundInRaid)) continue;
                int n = Mathf.Min(need, s.Count);
                take.Add((s, inv, n));
                need -= n;
            }
        }

        private static bool Matches(string target, ItemDef d)
        {
            if (d == null) return false;
            switch (target)
            {
                case "artifact": return ArtifactCatalog.IsArtifact(d.Id);
                case "mod": return Ballistics.Mod(d.Id) != null;
                case "mutantpart": return d.Id == "hound_fang" || d.Id == "shade_tendril" || d.Id == "puppeteer_gland" || d.Id == "thumper_hoof";
                default: return d.Id == target;
            }
        }

        public static string FetchName(QuestObjective o)
        {
            switch (o.Target)
            {
                case "artifact": return "artifacts";
                case "mod": return "weapon attachments";
                case "mutantpart": return "mutant parts";
                default: return Trading.NameOf(o.Target);
            }
        }

        private static void Complete(CharacterProfile c, QuestDef d, QuestState q)
        {
            q.Status = QuestState.Done;
            q.CompletedDay = c.Day;
            Money.Give(d.RewardMoney);
            Progression.AddXp(d.RewardXp, d.Title);
            TraderState t = c.Trader(d.Giver);
            t.Rep += d.RewardRep;
            foreach ((string id, int n) in d.RewardItems) ProfileStore.AddSave(ProfileStore.StashInventory, new StackSave { Id = id, Count = n });
            if (!string.IsNullOrEmpty(d.RewardFlag))
            {
                c.SetFlag(d.RewardFlag);
                if (d.RewardFlag == "lodestar_site" && ProfileStore.Account != null) ProfileStore.Account.Lodestar.SiteRevealed = true;
            }
            TraderDef giver = TraderCatalog.Get(d.Giver);
            if (giver != null && !string.IsNullOrEmpty(giver.Faction)) Factions.AdjustReputation(giver.Faction, 20f, "finished a job");
            ProfileStore.CommitStash();
            ProfileStore.Save();
        }

        public static string Summary(QuestState q)
        {
            QuestDef d = Get(q.Id);
            if (d == null) return q.Id;
            // using static StringBuilder _summarySb
            _summarySb.Clear();
            _summarySb.Append("<b>").Append(d.Title).Append("</b>");
            for (int i = 0; i < d.Objectives.Count; i++)
            {
                QuestObjective o = d.Objectives[i];
                float p = i < q.Progress.Count ? q.Progress[i] : 0f;
                bool done = o.Kind != ObjectiveKind.Fetch && p >= o.Count;
                _summarySb.Append("\n  ").Append(done ? "\u2714 " : "\u2022 ").Append(o.Text);
                if (o.Kind != ObjectiveKind.Fetch && o.Count > 1) _summarySb.Append(" (").Append(Mathf.Min(o.Count, (int)p)).Append('/').Append(o.Count).Append(')');
            }
            return _summarySb.ToString();
        }
        #endregion

        #region Progress
        /// <summary>Applies one progress key to the active character's jobs (from local play or a squadmate).</summary>
        public static void Apply(string key, float amount)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || string.IsNullOrEmpty(key) || GameSession.IsMonsterRun) return;   // a hunter's kills aren't the runner's jobs
            foreach (QuestState q in c.Quests)
            {
                if (q.Status != QuestState.Active) continue;
                QuestDef d = Get(q.Id);
                if (d == null) continue;
                for (int i = 0; i < d.Objectives.Count; i++)
                {
                    QuestObjective o = d.Objectives[i];
                    if (!KeyMatches(o, key)) continue;
                    while (q.Progress.Count <= i) q.Progress.Add(0f);
                    if (q.Progress[i] >= o.Count) continue;
                    q.Progress[i] = Mathf.Min(o.Count, q.Progress[i] + amount);
                    if (q.Progress[i] >= o.Count) GameEvents.RaiseToast("Objective done: " + o.Text);
                }
            }
        }

        private static bool KeyMatches(QuestObjective o, string key)
        {
            if (o.Zone != null && GameSession.ZoneId != o.Zone && !key.StartsWith("extract:")) return false;
            switch (o.Kind)
            {
                case ObjectiveKind.Kill: return key == "kill:" + o.Target;
                case ObjectiveKind.Visit: return key == "visit:" + o.Target;
                case ObjectiveKind.Extract: return key == "extract:" + o.Target || (o.Target == "any" && key.StartsWith("extract:"));
                case ObjectiveKind.ExtractWith: return key == "extractwith:" + o.Target;
                case ObjectiveKind.Key: return key == o.Target;
                default: return false;
            }
        }

        internal static void Wire()
        {
            GameEvents.QuestProgress += Apply;
            GameEvents.ActorKilled += k =>
            {
                if (k.Killer == null || k.Killer != Actor.LocalPlayer || k.Victim == null) return;
                Actor v = k.Victim;
                GameEvents.RaiseQuestProgress("kill:kind:" + v.Kind, 1f);
                if (!string.IsNullOrEmpty(v.Faction)) GameEvents.RaiseQuestProgress("kill:faction:" + v.Faction, 1f);
                if (v.Kind == ActorKind.Human) GameEvents.RaiseQuestProgress("kill:human", 1f);
                var cr = v.GetComponent<Creature>();
                if (cr != null) GameEvents.RaiseQuestProgress("kill:creature:" + cr.Def.Id, 1f);
                Progression.AddXp(v.Kind == ActorKind.Zombie ? 10f : v.Kind == ActorKind.Human ? 80f : cr != null ? cr.Def.Xp : 20f);
            };
            GameEvents.LocationVisited += id => { GameEvents.RaiseQuestProgress("visit:" + id, 1f); Progression.AddXp(60f, "found " + HumanUtil.Pretty(id)); };
            GameEvents.EmissionStateChanged += active =>
            {
                if (!active && Actor.LocalPlayer != null && Actor.LocalPlayer.IsAlive) GameEvents.RaiseQuestProgress("survive_emission", 1f);
            };
            GameEvents.RaidEnded += (outcome, exit) =>
            {
                if (outcome != RaidOutcome.Extracted) return;
                GameEvents.RaiseQuestProgress("extract:" + GameSession.ZoneId, 1f);
                PlayerInventory inv = HubKit.Carried;
                if (inv != null)
                    foreach (ItemStack s in inv.Inventory.Stacks)
                        if (s.Def != null && s.Def.Id == "art_sunkenstar") GameEvents.RaiseQuestProgress("extractwith:art_sunkenstar", 1f);
                Progression.AddXp(300f + ZoneCatalog.Current.Depth * 200f, "extracted");
            };
            GameEvents.ArtifactPropertyLearned += (a, p) => Progression.AddXp(40f, "learned an artifact property");
            GameEvents.ItemCrafted += (id, n) => Progression.AddXp(10f);
        }
        #endregion
    }

    public sealed class QuestModule : IGameModule
    {
        public int Order => 65;

        public void Init()
        {
            QuestBook.Wire();
            HubScreens.Register(HubScreens.Quests, arg => UIScreen.Show<QuestsScreen>(s => s.Arg = arg));
            WorldHooks.PlayerBuilt += p => { if (p != null && Object.FindAnyObjectByType<HudQuestTracker>() == null) HudQuestTracker.Create(); };
        }
    }

    /// <summary>The jobs screen: one trader's jobs (or the whole board), today's contracts, and handing in.</summary>
    public sealed class QuestsScreen : HubScreenBase
    {
        protected override string Title => string.IsNullOrEmpty(Arg) ? "Notice board — jobs and contracts" : "Work for " + TraderCatalog.Get(Arg).Name;

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1.2f, 1f);
            HubUI.Card(cols[0]);
            HubUI.AreaHeading(cols[0], "Jobs", 20);
            RectTransform list = HubUI.Scroll(cols[0], 40f, 0f, 6f);
            foreach (QuestDef d in QuestBook.All)
            {
                if (!string.IsNullOrEmpty(Arg) && d.Giver != Arg) continue;
                QuestState q = c.Quest(d.Id);
                bool avail = QuestBook.Available(c, d);
                if (!avail && (q == null || q.Status == QuestState.Available)) continue;
                Entry(list, c, d, q);
            }
            HubUI.Card(cols[1]);
            HubUI.AreaHeading(cols[1], "Today's contracts (day " + c.Day + ")", 20);
            RectTransform board = HubUI.Scroll(cols[1], 40f, 0f, 6f);
            foreach (string id in QuestBook.TodaysContracts(c))
            {
                QuestDef d = QuestBook.Get(id);
                if (d == null || (!string.IsNullOrEmpty(Arg) && d.Giver != Arg)) continue;
                Entry(board, c, d, c.Quest(id));
            }
        }

        private void Entry(Transform list, CharacterProfile c, QuestDef d, QuestState q)
        {
            RectTransform card = HubUI.CardColumn(list, new Color(1f, 1f, 1f, 0.04f));
            string giver = TraderCatalog.Get(d.Giver) != null ? TraderCatalog.Get(d.Giver).Name : d.Giver;
            HubUI.Heading(card, d.Title + "  <size=14><color=#9aa0a8>— " + giver + "</color></size>", 18);
            HubUI.Para(card, d.Brief, 14, UIKit.TextDim);
            if (q != null && (q.Status == QuestState.Active || q.Status == QuestState.Done)) HubUI.Para(card, QuestBook.Summary(q), 14);
            else foreach (QuestObjective o in d.Objectives) HubUI.Para(card, "\u2022 " + o.Text + (o.Count > 1 ? " (" + o.Count + ")" : ""), 14);
            HubUI.Para(card, "Reward: " + Money.Format(d.RewardMoney) + ", " + d.RewardXp + " XP", 14, UIKit.Good);
            RectTransform row = HubUI.ButtonRow(card);
            if (q == null || q.Status == QuestState.Available)
                HubUI.Btn(row, "Take the job", () => { QuestBook.Accept(c, d); Report(true, "Accepted: " + d.Title); Refresh(); }, 160f);
            else if (q.Status == QuestState.Active)
                HubUI.Btn(row, "Hand in", () =>
                {
                    bool ok = QuestBook.TryTurnIn(c, d, Carried, out string msg);
                    if (ok) HubKit.AfterChange();
                    Report(ok, msg);
                    Refresh();
                }, 160f);
            else HubUI.Note(row, "Completed", UIKit.Good);
        }
    }

    /// <summary>The active jobs in the corner of the HUD (hidden in the diegetic and hardcore HUDs: check the notebook).</summary>
    public sealed class HudQuestTracker : MonoBehaviour
    {
        private Text _text;
        private float _next;
        // Reused StringBuilder to avoid per‑second allocations.
        private readonly StringBuilder _sb = new StringBuilder();

        public static HudQuestTracker Create()
        {
            RectTransform slot = HudLayer.Anchor("Quests", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -120f), new Vector2(380f, 260f));
            var h = slot.gameObject.AddComponent<HudQuestTracker>();
            h._text = UIKit.Label(slot, "", 14, new Color(0.9f, 0.9f, 0.85f, 0.9f), TextAnchor.UpperRight);
            UIKit.Stretch(h._text.rectTransform);
            return h;
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) { _text.text = ""; return; }
            _sb.Clear();
            int n = 0;
            // Use a for‑loop to avoid per‑iteration allocation of the enumerator.
            var quests = c.Quests;
            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                if (q.Status != QuestState.Active || n >= 3) continue;
                _sb.Append(QuestBook.Summary(q)).Append('\n');
                n++;
            }
            _text.text = _sb.ToString();
        }
    }
}
