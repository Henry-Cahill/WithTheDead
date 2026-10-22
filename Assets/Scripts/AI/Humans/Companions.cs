using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Hired guns. A Freehold loner met in the Zone can be paid to tag along, or one can be hired at the bar before a
    /// raid. Companions follow, fight your enemies, and take orders (U cycles them): follow, hold here, and go first —
    /// walk the route ahead of you. Some routes can only be passed if someone goes first and pays for it; sending a
    /// companion is a choice the Zone remembers. A companion who survives the raid stays on for the next one.
    /// </summary>
    public static class CompanionService
    {
        public enum Order { Follow, Hold, GoFirst }

        public const string HubScreenId = "companion";
        private const string HireFlag = "companion:";

        /// <summary>The currently hired companion, if any.</summary>
        public static HumanNpc Current { get; private set; }
        /// <summary>The current order assigned to the companion.</summary>
        public static Order CurrentOrder { get; private set; }

        /// <summary>Determines whether the given NPC can be hired as a companion.</summary>
        public static bool CanHire(HumanNpc npc)
        {
            if (npc == null || npc.IsDead || Current != null || !GameSession.IsExtraction || GameSession.IsBorrowedBody) return false;
            if (npc.Actor.Faction != Factions.Freehold || npc.Actor.Kind == ActorKind.Companion) return false;
            return npc.SquadTag == null || (npc.SquadTag is ALifeSquad s && s.Live.Count <= 1);
        }

        /// <summary>Calculates the hiring fee for a given NPC based on their skill.
        /// </summary>
        public static int FeeFor(HumanNpc npc) => npc == null ? 0 : 500 + Mathf.RoundToInt(npc.Loadout.Skill * 1500f);

        /// <summary>Attempts to hire the given NPC as a companion, charging the fee and setting them up.
        /// </summary>
        public static void Hire(HumanNpc npc, InteractContext ctx)
        {
            int fee = FeeFor(npc);
            if (!Money.TrySpend(fee, ctx.Inventory)) { ctx.Notify("You can't afford them."); return; }
            if (npc.SquadTag is ALifeSquad s) { s.Live.Remove(npc); s.Members = Mathf.Max(0, s.Members - 1); }
            Recruit(npc);
            GameEvents.RaiseSubtitle(npc.Name, "Paid is paid. Lead on — I'll watch your back.");
            // Recorded so a companion who survives the raid stays on for the next one (see OnRaidStarted/OnRaidEnded).
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null) { ClearFlag(c); c.SetFlag(HireFlag + npc.Name + "|" + npc.Loadout.Id); }
        }

        private static void Recruit(HumanNpc npc)
        {
            Actor me = Actor.LocalPlayer;
            if (me != null && string.IsNullOrEmpty(me.Squad)) me.Squad = "squad_local";
            npc.SquadTag = null;
            npc.Actor.Kind = ActorKind.Companion;
            npc.Actor.Squad = me != null ? me.Squad : "squad_local";
            if (me != null) npc.Actor.Faction = me.Faction;
            npc.SetFollow(me);
            npc.Died += OnCompanionDied;
            Current = npc;
            CurrentOrder = Order.Follow;
        }

        /// <summary>Sets the current order for the hired companion.</summary>
        public static void SetOrder(Order o)
        {
            if (Current == null || Current.IsDead) return;
            CurrentOrder = o;
            Actor me = Actor.LocalPlayer;
            switch (o)
            {
                case Order.Follow:
                    Current.SetFollow(me);
                    GameEvents.RaiseSubtitle(Current.Name, "Right behind you.");
                    break;
                case Order.Hold:
                    Current.SetGuard(Current.transform.position);
                    GameEvents.RaiseSubtitle(Current.Name, "Holding here.");
                    break;
                case Order.GoFirst:
                {
                    Vector3 ahead = me != null ? me.transform.position + Flat(Camera.main != null ? Camera.main.transform.forward : me.transform.forward) * 14f : Current.transform.position;
                    if (me != null && ViewState.Current == CameraView.Isometric)
                    {
                        var pc = me.GetComponent<PlayerController>();
                        if (pc != null) ahead = me.transform.position + Flat(pc.AimPoint - me.transform.position) * 14f;
                    }
                    Current.HoldPost = false;
                    Current.TravelTo(ahead);
                    s_SentAheadAt = Time.time;
                    GameEvents.RaiseSubtitle(Current.Name, "Me first? ...Fine. Watch where I step.");
                    break;
                }
            }
        }

        private static float s_SentAheadAt = -999f;

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }

        private static void OnCompanionDied(HumanNpc npc)
        {
            if (npc != Current) return;
            Current = null;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null) ClearFlag(c);
            // Sent ahead and killed by what they were sent to find: the deadly route took its toll, and you chose who paid.
            if (Time.time - s_SentAheadAt < 45f)
            {
                MoralLedger.Record("sent someone ahead to die", MoralLedger.SacrificedSomeone);
                AccountProfile a = ProfileStore.Account;
                if (a != null) a.Lodestar.Pilgrims++;
            }
            GameEvents.RaiseToast(npc.Name + " is dead.");
        }

        private static void ClearFlag(CharacterProfile c) => c.Flags.RemoveAll(f => f.StartsWith(HireFlag));

        /// <summary>A companion hired at the bar (or one who survived the last raid) joins at the insertion point.</summary>
        /// <summary>Handles actions at the start of a raid, spawning any hired companion.</summary>
        public static void OnRaidStarted()
        {
            Current = null;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            Actor me = Actor.LocalPlayer;
            if (c == null || me == null || GameSession.IsBorrowedBody) return;
            string flag = c.Flags.Find(f => f.StartsWith(HireFlag));
            if (flag == null) return;
            string[] parts = flag.Substring(HireFlag.Length).Split('|');
            string loadout = parts.Length > 1 ? parts[1] : HumanLoadouts.Freehold;
            HumanNpc npc = HumanNpc.Spawn(me.transform.position - me.transform.forward * 2f + me.transform.right, me.Faction, loadout);
            Recruit(npc);
        }

        /// <summary>Handles actions at the end of a raid, clearing the hired companion if needed.</summary>
        public static void OnRaidEnded(RaidOutcome outcome)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            // Left in the Zone when you die; they come home with you when you get out together.
            if (c != null && (Current == null || Current.IsDead || outcome != RaidOutcome.Extracted)) ClearFlag(c);
            Current = null;
        }

        #region The hiring board at the bar
        internal static void RegisterHubScreen() => HubScreens.Register(HubScreenId, arg => UIScreen.Show<CompanionScreen>());

                /// <summary>Data for a hireable companion candidate.</summary>
        public sealed class Candidate { public string Name, Loadout; public float Skill; public int Fee; }

        /// <summary>Today's three runners for hire (they change with the hub day).</summary>
        public static List<Candidate> Candidates()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            var rng = new System.Random((c != null ? c.Day : 1) * 7919 + (c != null ? HumanUtil.StableHash(c.Id) : 0));
            var list = new List<Candidate>();
            string[] kits = { HumanLoadouts.Freehold, HumanLoadouts.Freehold, HumanLoadouts.Scav };
            for (int i = 0; i < 3; i++)
            {
                HumanLoadout l = HumanLoadouts.Get(kits[i]);
                float skill = Mathf.Clamp01(l.Skill + (float)rng.NextDouble() * 0.3f - 0.1f);
                list.Add(new Candidate { Name = HumanNames.Roll(Factions.Freehold, rng), Loadout = l.Id, Skill = skill, Fee = 500 + Mathf.RoundToInt(skill * 1500f) });
            }
            return list;
        }

        /// <summary>Checks if a companion has been hired and returns their name.</summary>
        public static bool HasHired(out string name)
        {
            name = null;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            string flag = c != null ? c.Flags.Find(f => f.StartsWith(HireFlag)) : null;
            if (flag == null) return false;
            name = flag.Substring(HireFlag.Length).Split('|')[0];
            return true;
        }

        /// <summary>Attempts to hire the given candidate at the bar, spending money and recording the hire.</summary>
        public static bool HireAtBar(Candidate cand, PlayerInventory carried)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || cand == null || !Money.TrySpend(cand.Fee, carried)) return false;
            ClearFlag(c);
            c.SetFlag(HireFlag + cand.Name + "|" + cand.Loadout);
            ProfileStore.Save();
            return true;
        }
        #endregion
    }

    /// <summary>The order key (U by default): follow → hold → go first → follow.</summary>
    public sealed class CompanionOrders : MonoBehaviour
    {
        private void Update()
        {
            if (!GameInput.Pressed(GameAction.Orders)) return;
            if (CompanionService.Current == null) { GameEvents.RaiseToast("Nobody is with you."); return; }
            var next = (CompanionService.Order)(((int)CompanionService.CurrentOrder + 1) % 3);
            CompanionService.SetOrder(next);
            GameEvents.RaiseToast("Order: " + (next == CompanionService.Order.GoFirst ? "go first" : next.ToString().ToLowerInvariant()));
        }
    }

    /// <summary>The bar's hiring board.</summary>
    public sealed class CompanionScreen : HubScreenBase
    {
        protected override string Title => "Runners for hire";

        protected override void BuildContent()
        {
            RectTransform area = HubUI.Area(Body, 0.15f, 0.08f, 0.85f, 0.95f);
            HubUI.Card(area);
            RectTransform col = HubUI.Stack(area, 8f, 16);
            HubUI.Heading(col, "Who'll walk with you", 24);
            HubUI.Para(col, "A hired runner follows you in, fights at your side and takes orders (U): follow, hold, or go first. " +
                "Some routes need someone to go first. The fee is paid now; if they come back alive, they stay on.", 16, UIKit.TextDim);
            if (CompanionService.HasHired(out string name))
            {
                HubUI.Para(col, UIKit.Colored(name + " is already waiting for you at the door.", UIKit.Good), 18);
                return;
            }
            foreach (CompanionService.Candidate cand in CompanionService.Candidates())
            {
                RectTransform row = HubUI.Row(col, 56f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, cand.Name, 360f, 18);
                HubUI.Cell(row, HumanLoadouts.Get(cand.Loadout).Title, 220f, 16, UIKit.TextDim);
                HubUI.Cell(row, "Skill " + HubUI.Pct(cand.Skill), 160f, 16);
                HubUI.Spacer(row);
                CompanionService.Candidate pick = cand;
                HubUI.Btn(row, "Hire — " + Money.Format(cand.Fee), () =>
                {
                    bool ok = CompanionService.HireAtBar(pick, Carried);
                    Report(ok, ok ? pick.Name + " will meet you at the insertion point." : "You can't afford " + pick.Name + ".");
                    Refresh();
                }, 240f, 40f);
            }
        }
    }
}
