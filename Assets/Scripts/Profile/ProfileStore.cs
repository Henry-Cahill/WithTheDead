using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Loads, saves and edits the account profile (<c>clonez_profile.json</c> under the persistent data path):
    /// the characters, each one's stash, traders, quests, hideout and long-term scars. The stash is exposed as a
    /// live gridded <see cref="Inventory"/> (<see cref="StashInventory"/>) for the hub's UI; edits land back in
    /// the profile on <see cref="Save"/>.
    ///
    /// The old single extracted-kit file from before profiles existed is folded into a first character
    /// automatically, so nobody loses the gear they carried out.
    /// </summary>
    public static class ProfileStore
    {
        public const string FileName = "clonez_profile.json";
        public const int StashWidth = 10;

        private static AccountProfile s_Account;
        private static Inventory s_Stash;
        private static CharacterProfile s_StashOwner;

                /// <summary>Full path to the currently used profile file (or override).
        /// </summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, PathOverride ?? FileName);

        /// <summary>Test harnesses only (see <see cref="SmokeTest"/>): a different profile file, and no touching the
        /// player's legacy files. Set before anything loads the profile.</summary>
        public static string PathOverride;
                /// <summary>True when a test profile override is active, isolating file I/O.</summary>
        public static bool Sandboxed => PathOverride != null;

                /// <summary>Current Unix timestamp (seconds since epoch) used for time‑stamping profile events.</summary>
        public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>The account, loaded on first use (a fresh one when no file exists).</summary>
        public static AccountProfile Account
        {
            get
            {
                if (s_Account == null) Load();
                return s_Account;
            }
        }

                /// <summary>The currently active character, or <c>null</c> if none is loaded.</summary>
        public static CharacterProfile ActiveCharacter => Account != null ? Account.Active : null;
                /// <summary>Whether the active character is alive (has a profile and is not dead).</summary>
        public static bool HasLivingCharacter => ActiveCharacter != null && !ActiveCharacter.Dead;

        public static event Action Changed;

        #region I/O
                /// <summary>Loads the profile from disk (or creates a fresh one) and sets up the static cache.</summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    s_Account = JsonUtility.FromJson<AccountProfile>(File.ReadAllText(FilePath));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not read the profile (" + e.Message + "); keeping a backup and starting fresh.");
                TryBackupCorrupt();
                s_Account = null;
            }
            if (s_Account == null)
            {
                s_Account = new AccountProfile { SeasonStartedUnix = NowUnix };
                MigrateOldKit();
            }
            Normalize(s_Account);
            s_Stash = null;
            s_StashOwner = null;
        }

        /// <summary>Writes the profile (committing the live stash first). Atomic: a crash mid-write never tears it.</summary>
        public static void Save()
        {
            if (s_Account == null) return;
            CommitStash();
            try
            {
                string path = FilePath, tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(s_Account, false));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not save the profile: " + e.Message);
            }
            Changed?.Invoke();
        }

        private static void TryBackupCorrupt()
        {
            try { File.Copy(FilePath, FilePath + ".corrupt-" + NowUnix, true); } catch { /* best effort */ }
        }

        // JsonUtility leaves missing lists null for old files; give every list a value so callers never check.
        private static void Normalize(AccountProfile a)
        {
            if (a.Characters == null) a.Characters = new List<CharacterProfile>();
            if (a.Knowledge == null) a.Knowledge = new List<string>();
            if (a.ArtifactKnowledge == null) a.ArtifactKnowledge = new List<string>();
            if (a.Lodestar == null) a.Lodestar = new LodestarState();
            if (a.Lodestar.Clues == null) a.Lodestar.Clues = new List<string>();
            if (a.Outposts == null) a.Outposts = new List<OutpostState>();
            if (a.Zones == null) a.Zones = new List<ZoneWorldState>();
            if (a.Flags == null) a.Flags = new List<string>();
            foreach (CharacterProfile c in a.Characters) NormalizeCharacter(c);
            if (a.ActiveIndex >= a.Characters.Count) a.ActiveIndex = Mathf.Max(0, a.Characters.Count - 1);
        }

                /// <summary>Normalises a newly loaded or created account to ensure all collections are non‑null.</summary>
        public static void NormalizeCharacter(CharacterProfile c)
        {
            if (c == null) return;
            if (string.IsNullOrEmpty(c.Id)) c.Id = Guid.NewGuid().ToString("N").Substring(0, 12);
            c.Traits ??= new List<string>();
            c.SkillXp ??= new List<float>();
            c.Books ??= new List<SkillBookState>();
            c.Stash ??= new List<StackSave>();
            c.Insurance ??= new List<InsuranceClaim>();
            c.Listings ??= new List<FleaListing>();
            c.Traders ??= new List<TraderState>();
            c.FactionRep ??= new List<FactionRepEntry>();
            c.Quests ??= new List<QuestState>();
            c.Hideout ??= new List<HideoutModuleState>();
            c.Crafts ??= new List<CraftJob>();
            c.Animals ??= new List<AnimalState>();
            c.ZoneMarks ??= new List<string>();
            c.Health ??= new HealthCarry();
            c.Health.LimbHp ??= new List<float>();
            c.Health.Fractured ??= new List<bool>();
            c.Health.Diseases ??= new List<string>();
            c.Corpses ??= new List<CorpseRecord>();
            c.Stashes ??= new List<BuriedStash>();
            c.Flags ??= new List<string>();
            c.UnlockedHubs ??= new List<string> { "kettle" };
            if (c.UnlockedHubs.Count == 0) c.UnlockedHubs.Add("kettle");
            if (string.IsNullOrEmpty(c.Hub)) c.Hub = "kettle";
            if (c.StashRows <= 0) c.StashRows = 14;
            foreach (TraderState t in c.Traders) t.Stock ??= new List<OfferStock>();
            foreach (QuestState q in c.Quests) q.Progress ??= new List<float>();
            foreach (InsuranceClaim ic in c.Insurance) ic.Items ??= new List<StackSave>();
            foreach (CorpseRecord cr in c.Corpses) cr.Items ??= new List<StackSave>();
            foreach (BuriedStash bs in c.Stashes) bs.Items ??= new List<StackSave>();
        }

        // Before profiles there was one extracted-kit file; turn it into the first character's kit.
        private static void MigrateOldKit()
        {
            if (Sandboxed) return;
            RaidKit old = RaidLoadout.LoadLegacyFile();
            if (old == null || old.IsEmpty) return;
            CharacterProfile c = NewCharacter(SurvivorNames.Roll(), "drifter", new List<string>(), Factions.Freehold, "O+", false);
            c.Kit = old;
            s_Account.Characters.Add(c);
            s_Account.ActiveIndex = 0;
            RaidLoadout.DeleteLegacyFile();
            Debug.Log("[CloneZ] Migrated the old extracted kit into the profile's first character.");
        }
        #endregion

        #region Characters
        /// <summary>A fresh character with starter money, standing and hideout (not yet added to the account).</summary>
                /// <summary>Creates a new character with starter money, equipment and hideout (not yet added to the account).</summary>
        public static CharacterProfile NewCharacter(string name, string profession, List<string> traits, string faction, string bloodType, bool ironman)
        {
            var c = new CharacterProfile
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 12),
                Name = string.IsNullOrWhiteSpace(name) ? "Runner" : name.Trim(),
                Profession = profession,
                Traits = traits ?? new List<string>(),
                Faction = faction,
                BloodType = bloodType,
                Ironman = ironman,
                CreatedUnix = NowUnix,
            };
            NormalizeCharacter(c);
            c.AddFactionRep(faction, 0f);
            StarterPack.Apply(c);
            return c;
        }

        /// <summary>Adds a character and makes it the active one. <paramref name="replaceSlot"/> names a dead
        /// character's slot for it to take; otherwise it takes a free slot, or with the roster full the first dead one's.</summary>
        public static void AddCharacter(CharacterProfile c, int replaceSlot = -1)
        {
            if (c == null) return;
            List<CharacterProfile> roster = Account.Characters;
            if (replaceSlot >= 0 && replaceSlot < roster.Count && roster[replaceSlot].Dead)
            {
                roster[replaceSlot] = c;
                Account.ActiveIndex = replaceSlot;
            }
            else if (roster.Count >= AccountProfile.MaxCharacters)
            {
                // Replace the first dead character, else refuse.
                int dead = roster.FindIndex(x => x.Dead);
                if (dead < 0) return;
                roster[dead] = c;
                Account.ActiveIndex = dead;
            }
            else
            {
                roster.Add(c);
                Account.ActiveIndex = roster.Count - 1;
            }
            s_Stash = null;
            Save();
        }

                /// <summary>Selects the active character by index, committing any pending stash changes.</summary>
        public static void SelectCharacter(int index)
        {
            if (index < 0 || index >= Account.Characters.Count) return;
            CommitStash();
            Account.ActiveIndex = index;
            s_Stash = null;
            Save();
        }

                /// <summary>Deletes the character at the given index, adjusting the active index as needed.</summary>
        public static void DeleteCharacter(int index)
        {
            if (index < 0 || index >= Account.Characters.Count) return;
            Account.Characters.RemoveAt(index);
            Account.ActiveIndex = Mathf.Clamp(Account.ActiveIndex, 0, Mathf.Max(0, Account.Characters.Count - 1));
            s_Stash = null;
            Save();
        }

        /// <summary>
        /// Ends the season: every character starts over (level, stash, traders, hideout, quests) but keeps their
        /// name, profession, traits and faction — and the account keeps everything it has learned about the Zone.
        /// </summary>
                /// <summary>Wipes the season, resetting all characters while preserving their core identities.</summary>
        public static void Wipe()
        {
            AccountProfile a = Account;
            var fresh = new List<CharacterProfile>();
            foreach (CharacterProfile old in a.Characters)
            {
                if (old.Dead && old.Ironman) continue;
                CharacterProfile n = NewCharacter(old.Name, old.Profession, old.Traits, old.Faction, old.BloodType, old.Ironman);
                n.Id = old.Id;
                fresh.Add(n);
            }
            a.Characters = fresh;
            a.ActiveIndex = 0;
            a.Season++;
            a.Wipes++;
            a.SeasonStartedUnix = NowUnix;
            a.Outposts.Clear();
            a.Zones.Clear();
            s_Stash = null;
            Save();
        }

        /// <summary>True when the season has run its configured length and a wipe is due.</summary>
                /// <summary>True when the season length has elapsed and a wipe is due.</summary>
        public static bool SeasonExpired
        {
            get
            {
                int days = SandboxSettings.Current.SeasonLengthDays;
                if (days <= 0) return false;
                return NowUnix - Account.SeasonStartedUnix > days * 86400L;
            }
        }
        #endregion

        #region Stash (live inventory)
        /// <summary>The active character's stash as a gridded inventory (built on first use; written back on Save).</summary>
                /// <summary>The live stash inventory for the active character (built on demand).</summary>
        public static Inventory StashInventory
        {
            get
            {
                CharacterProfile c = ActiveCharacter;
                if (c == null) return null;
                if (s_Stash != null && s_StashOwner == c) return s_Stash;
                s_Stash = BuildStash(c);
                s_StashOwner = c;
                return s_Stash;
            }
        }

        private static Inventory BuildStash(CharacterProfile c)
        {
            var inv = new Inventory(99999f);
            var section = new GridSection("Stash", StashWidth, Mathf.Max(6, c.StashRows), 0f);
            inv.SetLayout(new InventoryLayout(section));
            for (int i = 0; i < c.Stash.Count; i++)
            {
                StackSave st = c.Stash[i];
                ItemDef def = st != null ? ItemDatabase.Get(st.Id) : null;
                if (def == null || st.Count <= 0) continue;
                inv.Add(def, st.Count, enforceWeight: false,
                    spawnHours: def.Perishable ? st.SpawnHours : float.NaN,
                    condition: def.RestoredCondition(st.Condition),
                    timesRepaired: st.TimesRepaired, extra: ItemExtra.Parse(st.Extra));
            }
            return inv;
        }

        /// <summary>Grows the stash grid (a hideout stash upgrade).</summary>
                /// <summary>Sets the stash grid size (rows) and rebuilds the live inventory.</summary>
        public static void SetStashRows(int rows)
        {
            CharacterProfile c = ActiveCharacter;
            if (c == null) return;
            CommitStash();
            c.StashRows = rows;
            s_Stash = null;
        }

        /// <summary>Writes the live stash back into the profile's list.</summary>
                /// <summary>Writes the live stash inventory back into the profile's stored list.</summary>
        public static void CommitStash()
        {
            if (s_Stash == null || s_StashOwner == null) return;
            s_StashOwner.Stash = ToSaves(s_Stash);
        }

                /// <summary>Converts a live inventory into a list of saved stack records.</summary>
        public static List<StackSave> ToSaves(Inventory inv)
        {
            var list = new List<StackSave>();
            if (inv == null) return list;
            for (int i = 0; i < inv.Stacks.Count; i++)
            {
                ItemStack s = inv.Stacks[i];
                if (s == null || s.Def == null || s.Count <= 0) continue;
                list.Add(ToSave(s));
            }
            return list;
        }

        /// <summary>Creates a saved stack record from an inventory stack.</summary>
        public static StackSave ToSave(ItemStack s) => new StackSave
        {
            Id = s.Def.Id,
            Count = s.Count,
            SpawnHours = s.SpawnHours,
            Condition = s.Condition,
            TimesRepaired = s.TimesRepaired,
            Extra = ItemExtra.Serialize(s.Extra),
        };

        /// <summary>Adds a saved stack to an inventory (forced in, keeping wear and extras).</summary>
        public static int AddSave(Inventory inv, StackSave st)
        {
            ItemDef def = st != null ? ItemDatabase.Get(st.Id) : null;
            if (inv == null || def == null || st.Count <= 0) return 0;
            return inv.Add(def, st.Count, enforceWeight: false,
                spawnHours: def.Perishable ? st.SpawnHours : float.NaN,
                condition: def.RestoredCondition(st.Condition),
                timesRepaired: st.TimesRepaired, extra: ItemExtra.Parse(st.Extra));
        }
        #endregion
    }
}
