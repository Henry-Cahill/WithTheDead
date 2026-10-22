using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The pressure behind every run (Roadside Picnic's desperation): rent on the room above the bar, a debt with
    /// interest, family waiting on money. A day passes with every raid (or a night's sleep in the hub). Selling
    /// artifacts to the fence builds heat; when it's high, the Wardens come to collect. Seasons end in a wipe when
    /// the server sets a length — characters restart, the account keeps everything it learned.
    /// </summary>
    public static class Upkeep
    {
        public const int RentPerWeek = 1400, FamilyPerVisit = 600;
        public const float DebtInterest = 0.01f;

        /// <summary>
        /// Advances the in‑game day for the given character, applying rent, debt, heat and season logic.
        /// </summary>
        public static void AdvanceDay(CharacterProfile c, string why)
        {
            if (c == null) return;
            c.Day++;
            CorpseRecovery.OnDayPassed(c);
            foreach (string n in Hideout.Tick(c)) GameEvents.RaiseToast(n);
            c.Debt = Mathf.Round(c.Debt * (1f + DebtInterest));

            if (c.Day > c.RentPaidThroughDay)
            {
                c.MissedPayments++;
                GameEvents.RaiseToast("Rent is overdue (" + (c.Day - c.RentPaidThroughDay) + " day(s)). The landlord adds it to your debt.");
                c.Debt += RentPerWeek / 7f;
            }
            if (c.Day > c.FamilyPaidThroughDay)
            {
                MoralLedger.Record("left the family without money", MoralLedger.MissedFamily);
                if (c.Day == c.FamilyPaidThroughDay + 1) GameEvents.RaiseSubtitle("Letter from home", "\"The children ask where you are. We're out of coal. Please.\"");
            }

            // Heat: the Wardens come for the black market's best customers.
            if (c.Heat >= 1.5f)
            {
                Inventory stash = ProfileStore.StashInventory;
                ItemStack art = stash != null ? stash.Stacks.Find(s => ArtifactCatalog.IsArtifact(s.Def)) : null;
                if (art != null)
                {
                    stash.Remove(art, 1);
                    GameEvents.RaiseSubtitle("Wardens", "\"Routine inspection.\" They leave with a " + art.Def.DisplayName + " from your stash.");
                }
                else
                {
                    c.Debt += 1500f;
                    GameEvents.RaiseSubtitle("Wardens", "\"Fine for unlicensed trade.\" 1,500 added to what you owe.");
                }
                c.Heat = Mathf.Max(0f, c.Heat - 1f);
            }
            c.Heat = Mathf.Max(0f, c.Heat - 0.15f);

            if (ProfileStore.SeasonExpired)
            {
                GameEvents.RaiseSubtitle("Radio", "The season is over. Everyone starts again — but you remember what the Zone taught you.");
                ProfileStore.Wipe();
            }
            GameEvents.RaiseDayAdvanced(c.Day);
            ProfileStore.CommitStash();
            ProfileStore.Save();
        }

        /// <summary>
        /// Attempts to pay the weekly rent from the player's carried money.
        /// </summary>
        public static bool PayRent(CharacterProfile c, PlayerInventory carried)
        {
            if (!Money.TrySpend(RentPerWeek, carried)) return false;
            c.RentPaidThroughDay = Mathf.Max(c.RentPaidThroughDay, c.Day) + 7;
            c.MissedPayments = 0;
            return true;
        }

        /// <summary>
        /// Sends money home to the family, extending the family support period.
        /// </summary>
        public static bool SendHome(CharacterProfile c, PlayerInventory carried)
        {
            if (!Money.TrySpend(FamilyPerVisit, carried)) return false;
            c.FamilyPaidThroughDay = Mathf.Max(c.FamilyPaidThroughDay, c.Day) + 3;
            MoralLedger.Record("sent money home", MoralLedger.PaidFamily);
            return true;
        }

        /// <summary>
        /// Pays down the player's debt by the specified amount, up to the debt total.
        /// </summary>
        public static bool PayDebt(CharacterProfile c, PlayerInventory carried, int amount)
        {
            amount = Mathf.Min(amount, Mathf.CeilToInt(c.Debt));
            if (amount <= 0 || !Money.TrySpend(amount, carried)) return false;
            c.Debt = Mathf.Max(0f, c.Debt - amount);
            return true;
        }
    }

    public sealed class UpkeepScreen : HubScreenBase
    {
        protected override string Title => "The landlord's ledger — day " + Character.Day;

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.2f, 0.05f, 0.8f, 0.95f), 10f, 16);
            HubUI.Card((RectTransform)col.parent);
            Line(col, "Rent (the room above the bar)", "Paid through day " + c.RentPaidThroughDay + (c.Day > c.RentPaidThroughDay ? "  — OVERDUE" : ""), "Pay a week (" + Money.Format(Upkeep.RentPerWeek) + ")",
                () => Upkeep.PayRent(c, Carried));
            Line(col, "Family", "Provided for through day " + c.FamilyPaidThroughDay, "Send money home (" + Money.Format(Upkeep.FamilyPerVisit) + ")",
                () => Upkeep.SendHome(c, Carried));
            Line(col, "Debt (1% a day)", Money.Format(Mathf.CeilToInt(c.Debt)), "Pay 1,000", () => Upkeep.PayDebt(c, Carried, 1000));
            Line(col, "", "", "Pay it all", () => Upkeep.PayDebt(c, Carried, Mathf.CeilToInt(c.Debt)));
            HubUI.Para(col, "Heat: " + (c.Heat < 0.5f ? "nobody's looking at you" : c.Heat < 1.5f ? "people are asking questions" : "the Wardens are coming"), 16,
                c.Heat < 0.5f ? UIKit.Good : c.Heat < 1.5f ? UIKit.Warn : UIKit.Bad);
            HubUI.Para(col, "A day passes with every trip into the Zone. Insurance comes back, rent comes due, the hideout keeps working.", 14, UIKit.TextDim);
            HubUI.Btn(col, "Sleep until morning (a day passes)", () => { Upkeep.AdvanceDay(c, "slept"); Report(true, "Morning. Day " + c.Day + "."); Refresh(); }, 360f, 40f);
        }

        private void Line(Transform col, string what, string state, string action, System.Func<bool> pay)
        {
            RectTransform row = HubUI.Row(col, 46f, new Color(1f, 1f, 1f, 0.04f));
            HubUI.Cell(row, what, 300f, 17);
            HubUI.Cell(row, state, 320f, 15, UIKit.TextDim);
            HubUI.Btn(row, action, () => { bool ok = pay(); if (ok) HubKit.AfterChange(); Report(ok, ok ? "Paid." : "Not enough money."); Refresh(); }, 250f);
        }
    }

    /// <summary>The hideout: modules built from loot, crafts that run in real time, fuel, and the animals.</summary>
    public sealed class HideoutScreen : HubScreenBase
    {
        protected override string Title => "Hideout" + (Hideout.Powered(Character) ? "" : "  (no power)");

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            if (c == null) return;
            HideoutInitHelper.EnsureInitialized(c);
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1.1f);
            HubUI.Card(cols[0]);
            RectTransform mods = HubUI.Scroll(cols[0], 0f, 0f, 6f);
            foreach (ModuleDef m in Hideout.Modules)
            {
                int lvl = Hideout.LevelOf(c, m.Id);
                RectTransform card = HubUI.CardColumn(mods, new Color(1f, 1f, 1f, lvl > 0 ? 0.06f : 0.03f));
                HubUI.Heading(card, m.Name + "  <size=14><color=#9aa0a8>level " + lvl + "/" + m.Levels.Count + (m.NeedsPower ? " · needs power" : "") + "</color></size>", 18);
                HubUI.Para(card, m.Blurb, 14, UIKit.TextDim);
                ModuleLevel next = m.Levels.Find(l => l.Level == lvl + 1);
                if (next == null) continue;
                var need = new System.Text.StringBuilder(HubUI.NeedMoney(next.Money, Carried));
                foreach ((string id, int n) in next.Items) need.Append("  ").Append(HubUI.Need(id, n, Carried));
                HubUI.Para(card, "Next: " + next.Effect + "\n" + need, 14);
                bool can = Hideout.CanUpgrade(c, m, Carried, out string why);
                ModuleDef pick = m;
                RectTransform row = HubUI.ButtonRow(card);
                HubUI.Btn(row, lvl == 0 ? "Build" : "Upgrade", () =>
                {
                    bool ok = Hideout.Upgrade(c, pick, Carried, out string msg);
                    if (ok) { HubKit.SyncPouch(c); HubKit.AfterChange(); }
                    Report(ok, msg);
                    Refresh();
                }, 140f, 30f, null, can);
                if (!can) HubUI.Note(row, why, UIKit.TextDim);
                if (m.Id == "generator" && lvl > 0)
                    HubUI.Btn(row, "Refuel (gas can)", () => { bool ok = Hideout.Refuel(c, Carried, out string msg); Report(ok, msg); Refresh(); }, 160f);
            }

            HubUI.Card(cols[1]);
            RectTransform crafts = HubUI.Scroll(cols[1], 0f, 0f, 4f);
            HubUI.Section(crafts, "Running");
            foreach (CraftJob j in c.Crafts)
            {
                Recipe r = Hideout.GetRecipe(j.RecipeId);
                if (r == null) continue;
                CraftJob job = j;
                Live(HubUI.Note(crafts, ""), () => Hideout.Describe(r) + " — " + HubUI.Duration(System.Math.Max(0, job.DoneUnix - ProfileStore.NowUnix)));
            }
            HubUI.Section(crafts, "Crafts");
            foreach (Recipe r in Hideout.Recipes)
            {
                if (r.Field || Hideout.LevelOf(c, r.Module) < 1) continue;
                var sb = new System.Text.StringBuilder();
                foreach ((string id, int n) in r.Inputs) sb.Append(HubUI.Need(id, n, Carried)).Append("  ");
                RectTransform row = HubUI.Row(crafts, 50f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, Hideout.Describe(r) + " <size=12><color=#9aa0a8>(" + Hideout.Get(r.Module).Name + " " + r.ModuleLevel + ", " + r.Minutes.ToString("0") + " min)</color></size>\n<size=13>" + sb + "</size>", 470f, 14);
                bool can = Hideout.CanCraft(c, r, Carried, out string why);
                Recipe pick = r;
                HubUI.Btn(row, "Start", () => { bool ok = Hideout.StartCraft(c, pick, Carried, out string msg); if (ok) HubKit.AfterChange(); Report(ok, msg); Refresh(); }, 70f, 30f, null, can);
            }
        }

        protected override void OnSecond()
        {
            CharacterProfile c = Character;
            if (c != null && c.Crafts.Exists(j => j.DoneUnix <= ProfileStore.NowUnix)) Refresh();
        }
    }

    /// <summary>The mirror: who your runner is — profession, traits, faction standing, skills, the Zone's marks.</summary>
    public sealed class CharacterScreen : HubScreenBase
    {
        protected override string Title => Character.Name + " — level " + Character.Level;

        protected override void BuildContent()
        {
            CharacterProfile c = Character;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f, 1f);
            HubUI.Card(cols[0]);
            RectTransform a = HubUI.Scroll(cols[0], 0f, 0f, 4f);
            HubUI.Heading(a, c.Name, 24);
            ProfessionDef prof = ProfessionCatalog.Get(c.Profession);
            HubUI.Para(a, (prof != null ? prof.Name : c.Profession) + " · " + Factions.NameOf(c.Faction) + " · blood " + c.BloodType + (c.Ironman ? " · IRONMAN" : ""), 15);
            HubUI.LabeledBar(a, "Level " + c.Level, Mathf.RoundToInt(c.Xp) + " XP", Progression.LevelProgress(c), UIKit.Accent);
            HubUI.Section(a, "Traits");
            foreach (string t in c.Traits)
            {
                TraitDef tr = ProfessionCatalog.Trait(t);
                HubUI.Note(a, tr != null ? tr.Name + " — " + tr.Blurb : t, tr != null && tr.Cost < 0 ? UIKit.Warn : UIKit.Good);
            }
            HubUI.Section(a, "Record");
            HubUI.Note(a, "Raids " + c.Raids + " · extracts " + c.Extracts + " · deaths " + c.Deaths + " · kills " + c.Kills + " · artifacts " + c.ArtifactsFound);
            HubUI.Note(a, "Modifiers now: " + PlayerModifiers.Current.Describe(), UIKit.TextDim);
            HubUI.Btn(a, "Change runner (main menu)", () => GameFlow.ToMainMenu(), 280f, 34f);

            HubUI.Card(cols[1]);
            RectTransform s = HubUI.Scroll(cols[1], 0f, 0f, 3f);
            HubUI.Heading(s, "Skills (learn by doing)", 20);
            var skills = Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerSkills>() : null;
            if (skills != null)
                foreach (PlayerSkills.Skill sk in System.Enum.GetValues(typeof(PlayerSkills.Skill)))
                    HubUI.LabeledBar(s, sk.ToString(), "Lv " + skills.GetLevel(sk), skills.LevelProgress(sk), new Color(0.5f, 0.75f, 0.45f), 5f);

            HubUI.Card(cols[2]);
            RectTransform f = HubUI.Scroll(cols[2], 0f, 0f, 4f);
            HubUI.Heading(f, "Standing", 20);
            foreach (string id in Factions.AllIds)
            {
                if (id == Factions.Zone) continue;
                float rep = c.GetFactionRep(id);
                HubUI.LabeledBar(f, Factions.NameOf(id), Mathf.RoundToInt(rep).ToString(), Mathf.InverseLerp(-1000f, 1000f, rep), Factions.Get(id).Color, 6f);
            }
            HubUI.Section(f, "The Zone's marks");
            if (c.ZoneMarks.Count == 0) HubUI.Note(f, "None yet. (Exposure " + c.Exposure.ToString("0.00") + ")", UIKit.TextDim);
            foreach (string m in c.ZoneMarks) HubUI.Note(f, ZoneMarkRegistry.NameOf(m) + " — " + ZoneMarkRegistry.Describe(m));
        }
    }

    /// <summary>
    /// The player's own presentation and accessibility settings (tension 12: realism vs readability) — brightness,
    /// motion blur, lens distortion, camera shake, field of view, fog readability, HUD style (full, minimal,
    /// diegetic, hardcore), crosshair, subtitles, audio and voice.
    /// </summary>
    public sealed class SettingsScreen : HubScreenBase
    {
        protected override string Title => "Settings";
        protected override bool NeedsCharacter => false;
        protected override bool ShowMoney => false;

        protected override void BuildContent()
        {
            GameSettings g = GameSettings.Current;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f);
            HubUI.Card(cols[0]);
            RectTransform a = HubUI.Stack(cols[0], 6f, 16);
            HubUI.Heading(a, "Picture (accessibility floor)", 20);
            UIKit.Slider(a, "Brightness", -1f, 1f, g.Brightness, v => g.Brightness = v);
            UIKit.Toggle(a, "Motion blur", g.MotionBlur, v => g.MotionBlur = v);
            UIKit.Slider(a, "Lens distortion (bodycam)", 0f, 1f, g.LensDistortion, v => g.LensDistortion = v);
            UIKit.Slider(a, "Camera shake", 0f, 1f, g.CameraShake, v => g.CameraShake = v);
            UIKit.Slider(a, "Field of view", 55f, 100f, g.FieldOfView, v => g.FieldOfView = v, "0", true);
            UIKit.Slider(a, "Fog readability", 0f, 1f, g.FogReadability, v => g.FogReadability = v);
            HubUI.Cycle(a, "HUD", new[] { "Full", "Minimal", "Diegetic (watch, map, notebook)", "Hardcore (no HUD, no crosshair)" }, (int)g.Hud, v => g.Hud = (HudMode)v);
            UIKit.Toggle(a, "Crosshair", g.Crosshair, v => g.Crosshair = v);
            UIKit.Toggle(a, "Subtitles", g.Subtitles, v => g.Subtitles = v);

            HubUI.Card(cols[1]);
            RectTransform b = HubUI.Stack(cols[1], 6f, 16);
            HubUI.Heading(b, "Sound and voice", 20);
            UIKit.Slider(b, "Master volume", 0f, 1f, g.MasterVolume, v => { g.MasterVolume = v; AudioListener.volume = v; });
            UIKit.Slider(b, "Voice volume", 0f, 1f, g.VoiceVolume, v => g.VoiceVolume = v);
            UIKit.Toggle(b, "Microphone (proximity voice)", g.Microphone, v => g.Microphone = v);
            UIKit.Toggle(b, "Push to talk (Caps Lock)", g.PushToTalk, v => g.PushToTalk = v);
            HubUI.Heading(b, "Controls", 20);
            UIKit.Slider(b, "Mouse sensitivity", 0.2f, 3f, g.MouseSensitivity, v => g.MouseSensitivity = v);
            HubUI.Btn(b, "Save", () => { g.Save(); Report(true, "Saved."); }, 200f, 40f, UIKit.Accent);
        }
    }

    /// <summary>The sandbox rules (the host's on a server): every dial that changes how hard or how long the game is.</summary>
    public sealed class SandboxScreen : HubScreenBase
    {
        protected override string Title => "Sandbox rules" + (GameSession.Networked && !NetAuthority.IsServer ? " (set by the host)" : "");
        protected override bool NeedsCharacter => false;
        protected override bool ShowMoney => false;

        protected override void BuildContent()
        {
            SandboxSettings s = SandboxSettings.Current;
            bool edit = !GameSession.Networked || NetAuthority.IsServer;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f, 1f);
            HubUI.Card(cols[0]);
            RectTransform a = HubUI.Stack(cols[0], 4f, 14);
            HubUI.Heading(a, "The dead and the world", 18);
            UIKit.Slider(a, "Zombie population", 0f, 3f, s.ZombiePopulation, v => s.ZombiePopulation = v);
            UIKit.Slider(a, "Sprinter share", 0f, 1f, s.SprinterShare, v => s.SprinterShare = v);
            UIKit.Toggle(a, "The dead return", s.ZombieRespawn, v => s.ZombieRespawn = v);
            HubUI.Cycle(a, "Infection", new[] { "A bite is lethal", "A bite might be", "Off" }, (int)s.Infection, v => s.Infection = (InfectionMode)v);
            UIKit.Slider(a, "Raid length (hours)", 2f, 14f, s.RaidHours, v => s.RaidHours = v, "0", true);
            UIKit.Toggle(a, "Open Zone has no clock", s.OpenZoneNoTimer, v => s.OpenZoneNoTimer = v);
            UIKit.Slider(a, "Night darkness", 0f, 1f, s.NightDarkness, v => s.NightDarkness = v);
            UIKit.Slider(a, "Anomaly density", 0f, 3f, s.AnomalyDensity, v => s.AnomalyDensity = v);
            UIKit.Slider(a, "Emission every (min, 0 off)", 0f, 60f, s.EmissionIntervalMinutes, v => s.EmissionIntervalMinutes = v, "0", true);
            UIKit.Slider(a, "Psi-storm share", 0f, 1f, s.PsiStormChance, v => s.PsiStormChance = v);
            UIKit.Toggle(a, "Uncanny events", s.UncannyEvents, v => s.UncannyEvents = v);

            HubUI.Card(cols[1]);
            RectTransform b = HubUI.Stack(cols[1], 4f, 14);
            HubUI.Heading(b, "Loot, needs and death", 18);
            UIKit.Slider(b, "Loot abundance", 0.2f, 3f, s.LootAbundance, v => s.LootAbundance = v);
            UIKit.Toggle(b, "Central economy (loot targets)", s.CentralEconomy, v => s.CentralEconomy = v);
            HubUI.Para(b, "Loot targets live in " + CentralEconomy.FilePath + " (nominal, min, lifetime, restock per category and item).", 12, UIKit.TextDim);
            HubUI.Btn(b, "Reload economy.json", () => { CentralEconomy.Load(); Report(true, "Economy reloaded: " + CentralEconomy.Config.Categories.Count + " categories, " + CentralEconomy.Config.Items.Count + " item rules."); }, 260f, 30f, null, edit);
            UIKit.Slider(b, "Hunger rate", 0f, 3f, s.HungerRate, v => s.HungerRate = v);
            UIKit.Slider(b, "Thirst rate", 0f, 3f, s.ThirstRate, v => s.ThirstRate = v);
            UIKit.Toggle(b, "Tune needs to raid length", s.AutoTuneDrainToRaid, v => s.AutoTuneDrainToRaid = v);
            UIKit.Toggle(b, "Deep medical (limbs, blood)", s.DeepMedical, v => s.DeepMedical = v);
            UIKit.Toggle(b, "Deep ballistics (armour classes)", s.DeepBallistics, v => s.DeepBallistics = v);
            UIKit.Toggle(b, "Diseases", s.Diseases, v => s.Diseases = v);
            HubUI.Cycle(b, "Death", new[] { "Lose what you carried", "Gear waits on your body" }, (int)s.Death, v => s.Death = (DeathPenalty)v);
            UIKit.Toggle(b, "Permadeath characters", s.PermadeathCharacters, v => s.PermadeathCharacters = v);
            UIKit.Slider(b, "Season length (days, 0 never)", 0f, 120f, s.SeasonLengthDays, v => s.SeasonLengthDays = Mathf.RoundToInt(v), "0", true);

            HubUI.Card(cols[2]);
            RectTransform cc = HubUI.Stack(cols[2], 4f, 14);
            HubUI.Heading(cc, "Server rules", 18);
            UIKit.Toggle(cc, "PvP", s.PvpEnabled, v => s.PvpEnabled = v);
            UIKit.Toggle(cc, "Friendly fire", s.FriendlyFire, v => s.FriendlyFire = v);
            UIKit.Toggle(cc, "First person only in PvP raids", s.FirstPersonOnlyInPvp, v => s.FirstPersonOnlyInPvp = v);
            UIKit.Toggle(cc, "Players can play monsters", s.AllowPlayerMonsters, v => s.AllowPlayerMonsters = v);
            UIKit.Slider(cc, "Voice range (m)", 5f, 80f, s.VoiceRange, v => s.VoiceRange = v, "0", true);
            UIKit.Slider(cc, "Max players", 2f, 16f, s.MaxPlayers, v => s.MaxPlayers = Mathf.RoundToInt(v), "0", true);
            UIKit.Toggle(cc, "Flea market", s.FleaMarket, v => s.FleaMarket = v);
            UIKit.Slider(cc, "Flea market level", 1f, 30f, s.FleaMinLevel, v => s.FleaMinLevel = Mathf.RoundToInt(v), "0", true);
            UIKit.Toggle(cc, "Bases safe while owners are offline", s.OfflineRaidProtection, v => s.OfflineRaidProtection = v);
            HubUI.Btn(cc, edit ? "Save rules" : "Only the host can change these", () => { s.Save(); Report(true, "Rules saved (they apply from the next raid)."); }, 300f, 40f, UIKit.Accent, edit);
        }
    }

    /// <summary>The field manual (GZW rebuilt onboarding after launch: plan it early). Pages on the loop, the controls of
    /// each camera, survival, medicine, the Zone, combat, the economy, factions, co-op and the accessibility options.</summary>
    public sealed class ManualScreen : HubScreenBase
    {
        protected override string Title => "Field manual";
        protected override bool NeedsCharacter => false;
        protected override bool ShowMoney => false;
        private static int s_Page;

        private static readonly (string title, string body)[] Pages =
        {
            ("The loop", "Gear up at the bar → go into a Zone (on foot through the cordon, or by helicopter to a landing zone you choose) → loot, hunt artifacts, do jobs → get out through an exit (some have conditions: pay, a switch, no backpack, a flare for the helicopter) → sell, repair, upgrade, pay the rent → go again.\n\nYou lose what you carried if you die — never what you learned, never the contents of your secure pouch. Insured gear comes back if nobody takes it."),
            ("Controls", "WASD move · Shift sprint · Ctrl sneak · Space vault/jump · LMB attack/fire · RMB aim down sights (mouse-look views) · MMB shove · R reload · E interact · Q menu · I/Tab inventory · V change camera · H health · K skills · M HUD info · F farming · G drink/fill · N night vision · L flashlight · B build · F1 hints · F2 wipe the visor · F3 hands up (hold to struggle out of cuffs) · F4 runners and squads · F5 quicksave (wave survival).\n\nThe Zone: X throw a bolt · O detector · C field crafting · P map · J notebook · U companion orders · Y walkie (hold to talk on its channel) · Caps Lock talk to anyone nearby · ` throw a grenade.\n\nPer view: isometric — Alt shows your aim line, Z rotates the view; over the shoulder — Z swaps shoulder; first person/bodycam — Z holds your breath while aiming.\n\nDriving: W start/drive · S reverse · A/D steer · Space brake · E get out. Drones: WASD fly · mouse turn · Space/Ctrl climb/sink · LMB drop the grenade · E land."),
            ("Cameras", "Three views of one simulation: isometric (sees the horde, but walls and fog hide what you can't see), over the shoulder, and first person / bodycam (wide lens, heavy motion, no crosshair in hardcore). What you can see, hear and hit is decided by your head and your weapon, never the camera. PvP servers can force first person."),
            ("Survival", "Hunger, thirst, temperature, wetness, fatigue, illness. Needs are tuned to raid length. Drink clean water (boil it or use tablets — tainted water brings cholera). Wet and cold brings colds (you'll sneeze). Forage, fish at the river, set snares, light fires (C). Read skill books to learn faster."),
            ("Medicine", "Seven body zones. Head or thorax at zero kills you; a blacked-out limb still works, badly. Bleeds (light and heavy — tourniquets and hemostats), fractures (splints), pain (painkillers, morphine), blood loss (transfusions — blood types matter: O- gives to anyone), shock and unconsciousness."),
            ("The Zone", "Anomalies follow rules — learn them. Throw bolts (X) before you step: the trap reacts to the bolt first. Artifacts lie inside anomalies, invisible until a good enough detector finds them. Wear them on your belt to learn what they do — every one has a price (often radiation). When the siren goes, get under a solid roof within two minutes. Radiation stays in you: anti-rad, iodine, the medic."),
            ("Combat", "Ammo matters more than the gun: penetration against armour class. Each zone has armour or not. Guns get dirty and jam — clean them. Strip mods off guns you find. The technician repairs and upgrades along branching paths. People fight from cover; some surrender (spare them, cuff them or rob them — it's remembered)."),
            ("Economy", "Traders gate their best stock behind loyalty (level, standing, money spent). The Institute pays fair for artifacts and identifies them; the fence pays more and the Wardens notice. The flea market takes found-in-raid goods only. The hideout turns junk into ammo, medicine, fuel and drives. Rent, debt and family come due."),
            ("Factions", "Freehold, Vanguard and the Lantern Syndicate can be joined. Wardens guard the cordon (shoot anyone in the restricted belt without a pass), Scavengers raid, the Institute studies, the Hollow Choir guards the deep Zone. Who you work for decides where you are safe. Outposts can be taken for your faction; they lock for 30 minutes."),
            ("Co-op and other players", "The Zone runs on the host's machine: when the host goes in, anyone can follow from the door — as their runner, a scavenger, or a hunter (one of the things in the dark, paid for every runner it takes). Proximity voice (Caps Lock) and walkies on channels (hold Y; use the walkie to change channel). Squads (F4) can't hurt each other and share job progress. Hands up with F3; cuff or tie someone helpless, make them drop their gun, feed them, dress a wound, give them blood (the right type), shake them awake, trade hand to hand. A runner who dies leaves a body with everything they carried."),
            ("Camps, cars and gadgets", "Build in the Zone (B) and it's your camp: it stands where you left it next time — but only while you're in the Zone (nobody can raid it while you're away). Hang a gate in a doorway and fit a combination lock; raiders need a crowbar, a lot of swinging, or explosives (breaching charges, grenades).\n\nCars are found broken: fit wheels, a battery, a spark plug and a radiator, pour in fuel from a gas can, hotwire it (Mechanics helps). Loud and fast; tyres wear and burst, no radiator means it overheats.\n\nThe field tablet holds your kit, your gun's build (fit and strip parts), saved loadouts (refilled from the stash in the bar), the map and your drones. FPV drones fly and can drop a grenade; ground drones crawl quietly. Your body stays where you left it while you fly."),
            ("The economy dial", "Loot follows a Central Economy: each kind of item has a target count in a raid's Zone, a floor that long raids are restocked to, and a lifetime on the ground — all in economy.json (see the sandbox rules). Server hosts tune it without touching code."),
            ("Accessibility", "Settings: brightness, motion blur, lens distortion, camera shake, field of view, fog readability, HUD style (full, minimal, diegetic, hardcore), crosshair, subtitles. Readability is a floor, not an option."),
            ("Spillway folklore", "Old wives tell of a ghostly light that lures wanderers to the dam. The tale warns of a cursed stone that whispers. It’s just superstition, but the night can be scary when the wind whistles through the broken concrete."),
            ("Winter hazards", "Freezing wind bites through even thick coats. Ice patches on roofs can give you a nasty fall, and prolonged exposure causes hypothermia. Light a fire, stay dry, and keep your breath visible as a warning."),
            ("Radio chatter", "Static‑filled broadcasts drift through the zone. Occasionally you’ll catch a fragment: “...wardens moving east…”, “...flea market opens at dusk…”, “...supply drop scheduled…”. Listen for clues about safe routes or new quests."),
            ("Survivor myths", "Stories circulate about “immune blood” that cures any disease or a “golden artifact” that grants endless ammo. Most are hoaxes that waste time, but a few turn out to be real—investigate before you spend resources chasing legends."),
            ("Snowdrift shelters", "Dig a shallow trench in deep snow, pile drifts over it and add a windbreak; staying low conserves body heat and shields you from the biting wind."),
            ("Radio static lore", "Fragments crackle over static: “… stash at the old well …”, “… raid incoming east …”, “… supply drop near the power line …”. Those hints point to hidden loot or looming threats."),
            ("Fire‑making tips", "Gather dry twigs, pine bark and small branches; strike flint for sparks, then coax a flame with pine resin for a longer burn. In winter the resin fuels fire when wood is scarce."),
            ("Old maps", "Hand‑drawn maps in the bar show safe routes, fresh water sources, and marked danger zones; study them before venturing out to avoid ambushes."),
            ("Thin ice warning", "Thin ice on the riverbank; tread carefully or you’ll plunge into icy water."),
            ("Fuel shortage", "Fuel shortages hit the town; the pumps are sputtering and the lights flicker."),
            ("Wardens near power line", "Wardens spotted near the south power line; stay clear until they move."),
            ("Mold warning", "Mold kills faster than the cold; keep food sealed and dry."),
            ("Bar graffiti fire", "Keep the fire burning, the night watches."),
            ("Hunting handbook", "Deer tracks are clearer after fresh snow; follow the prints for a good hunt."),

            ("Power grid woes", "The town's generator sputters on cold nights. Unstable power means the lights flicker and the Wardens patrol the substation more often. Keep a spare battery and a flashlight handy; a dark hub saves on heat but draws attention."),
            ("Cold storage", "Root cellars stay below freezing even in winter, perfect for keeping meat from the hunt. Seal containers tightly, or the thaw will attract flies and rot the food faster than the cold does."),
            ("Bandit whispers", "Rumors drift from the south road about a group of scavengers using a rusted pickup to raid the bar after dark. They favor the old silo as a hideout; avoid it unless you’re armed and ready."),
            ("Zombie mutation", "Early 1993 reports mention a strange strain that leaves the dead slower but harder to kill. Their gait is lumbering, and they seem less affected by light. Keep your flare ready; a burst can blind them momentarily."),
            ("Snow‑bound roads", "Heavy snow blocks the main highway near the old mill. Vehicles get stuck, and foot travel is the only safe path. Mark your steps to avoid walking circles."),
            ("Radio tower repair", "The old TV tower still broadcasts on a cracked frequency. Repairing it gives a reliable channel for supply drops, but the Wardens monitor any signal spikes."),

            ("Cold night whispers", "When the wind howls past the broken shutters, you can hear faint voices warning of hidden caches—follow the sound, but beware of thin ice."),
            ("Frostbite tips", "Exposed skin turns white within minutes. Rub soot on your hands, wrap spare rags around your feet, and keep a fire going; a simple scarf can mean the difference between life and death."),
            ("Snowbound supply lines", "The county road to the east is blocked by drifts. Travelers mark safe passes with charcoal‑black stones; a well‑placed marker can guide you to the abandoned fuel depot."),
            ("Old church bell", "The rusted bell in the town’s derelict church still rings on windy nights. Legend says ringing it summons a wandering trader; the sound also draws the Wardens, so use it wisely."),
            ("Broken tractor", "A rusted John Deere sits half‑buried in snow near the silo. If you scavenge its battery and engine parts, you can jury‑rig a makeshift snow‑mobile for faster travel across the frozen fields."),
            ("Mysterious footprints", "Deep, irregular tracks lead away from the bar toward the riverbank. They’re too large for any known animal—some say they belong to a mutated zombie variant that stalks the ice."),
        };

        protected override void BuildContent()
        {
            RectTransform[] cols = HubUI.Columns(Body, 16f, 0.35f, 1f);
            HubUI.Card(cols[0]);
            RectTransform idx = HubUI.Scroll(cols[0], 0f, 0f, 4f);   // 33 pages no longer fit a fixed stack
            for (int i = 0; i < Pages.Length; i++)
            {
                int p = i;
                HubUI.Btn(idx, Pages[i].title, () => { s_Page = p; Sfx.PlayUI(SfxIds.PageTurn, 0.5f); Refresh(); }, 280f, 34f, i == s_Page ? UIKit.Accent : UIKit.ButtonColor);
            }
            HubUI.Card(cols[1]);
            RectTransform page = HubUI.Scroll(cols[1], 0f, 0f, 6f);
            HubUI.Heading(page, Pages[s_Page].title, 26);
            HubUI.Para(page, Pages[s_Page].body, 18);
        }
    }

    /// <summary>
    /// The Lodestar (the doc's Golden Sphere): a rumoured light at the bottom of the Spillway that grants a wish, at a
    /// terrible price. Clues come from pilgrims' pages, a Choir hymnal and a survey photograph; the barkeep pieces them
    /// together. Whether it is real is for each player to find out. On a server the progress is shared by everyone.
    /// </summary>
    public sealed class LodestarScreen : HubScreenBase
    {
        protected override string Title => "The Lodestar";

        protected override void BuildContent()
        {
            AccountProfile a = ProfileStore.Account;
            RectTransform col = HubUI.Stack(HubUI.Area(Body, 0.2f, 0.1f, 0.8f, 0.9f), 10f, 16);
            HubUI.Card((RectTransform)col.parent);
            HubUI.Heading(col, a.Lodestar.SiteRevealed ? "You know where it is." : "A rumour, for now.", 24);
            HubUI.Para(col, a.Lodestar.SiteRevealed
                ? "Below the dam, where the Choir sings. The last stretch takes a toll: something there kills the first one through. Pilgrims who went first: " + a.Lodestar.Pilgrims + "."
                : "Pilgrims burned pages as they walked. Collect them (and the Choir's hymnal, and the photograph) and bring them to Mott.", 17);
            if (a.Lodestar.WishMade) HubUI.Para(col, "A wish was made: \"" + a.Lodestar.Wish + "\". The Zone answered. You know how.", 17, UIKit.Warn);
        }
    }

    /// <summary>Registers every hub screen with the station registry.</summary>
    public sealed class HubScreensModule : IGameModule
    {
        public int Order => 70;

        // Stored handler for RaidEnded to allow proper unsubscription on re‑initialisation.
        private static readonly System.Action<RaidOutcome, string> s_RaidEnded = (o, e) => Upkeep.AdvanceDay(ProfileStore.ActiveCharacter, "raid");

        public void Init()
        {
            HubScreens.Register(HubScreens.Trader, arg => UIScreen.Show<TraderScreen>(s => s.Arg = arg));
            HubScreens.Register(HubScreens.Flea, arg => UIScreen.Show<FleaScreen>());
            HubScreens.Register(HubScreens.Stash, arg => UIScreen.Show<StashScreen>());
            HubScreens.Register(HubScreens.Medic, arg => UIScreen.Show<MedicScreen>());
            HubScreens.Register(HubScreens.Insurance, arg => UIScreen.Show<InsuranceScreen>());
            HubScreens.Register(HubScreens.Technician, arg => UIScreen.Show<TechnicianScreen>());
            HubScreens.Register(HubScreens.Guide, arg => UIScreen.Show<GuideScreen>());
            HubScreens.Register(HubScreens.Deploy, arg => UIScreen.Show<DeployScreen>());
            HubScreens.Register(HubScreens.Hideout, arg => UIScreen.Show<HideoutScreen>());
            HubScreens.Register(HubScreens.Upkeep, arg => UIScreen.Show<UpkeepScreen>());
            HubScreens.Register(HubScreens.Character, arg => UIScreen.Show<CharacterScreen>());
            HubScreens.Register(HubScreens.Settings, arg => UIScreen.Show<SettingsScreen>());
            HubScreens.Register(HubScreens.Sandbox, arg => UIScreen.Show<SandboxScreen>());
            HubScreens.Register(HubScreens.Manual, arg => UIScreen.Show<ManualScreen>());
            HubScreens.Register(HubScreens.Lodestar, arg => UIScreen.Show<LodestarScreen>());
            HubScreens.Register("rumours", arg => UIScreen.Show<RumoursScreen>());
            // Ensure we don't accumulate multiple handlers across hub re‑initialisations.
            GameEvents.RaidEnded -= s_RaidEnded;
            GameEvents.RaidEnded += s_RaidEnded;
        }
    }
}
