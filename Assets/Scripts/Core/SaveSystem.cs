using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Reads and writes the game to disk and knows how to capture a live run into a
    /// <see cref="SaveData"/> and pour a <see cref="SaveData"/> back into a freshly
    /// built world. Disk I/O is a normalised SQLite database (see
    /// <see cref="SqliteSaveStore"/>) stored as a single file under
    /// <see cref="Application.persistentDataPath"/>.
    ///
    /// The bootstrapper drives loading: it builds the static arena, then (if a save
    /// exists) calls <see cref="ApplyWorld"/> before baking the navmesh and
    /// <see cref="ApplyPlayer"/> after the survivor is spawned.
    /// </summary>
    public static class SaveSystem
    {
        public const int SaveVersion = 1;
        private const string FileName = "clonez_save.db";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool HasSave => File.Exists(FilePath);

        #region Disk
        public static void Save(SaveData data)
        {
            if (data == null) return;
            try
            {
                SqliteSaveStore.Write(FilePath, data);
                Debug.Log("[CloneZ] Game saved \u2192 " + FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[CloneZ] Save failed: " + e.Message);
            }
        }

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            if (!HasSave) return false;
            try
            {
                data = SqliteSaveStore.Read(FilePath);
                if (data == null || data.Version <= 0) { data = null; return false; }
                Debug.Log("[CloneZ] Save loaded from " + FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[CloneZ] Load failed: " + e.Message);
                data = null;
                return false;
            }
        }

        public static void Delete()
        {
            try
            {
                if (HasSave) File.Delete(FilePath);
                // Remove any transient SQLite sidecar files as well.
                DeleteIfExists(FilePath + "-journal");
                DeleteIfExists(FilePath + "-wal");
                DeleteIfExists(FilePath + "-shm");
            }
            catch (Exception e) { Debug.LogError("[CloneZ] Could not delete save: " + e.Message); }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        #endregion

        #region Capture
        /// <summary>Snapshots the entire live run into a <see cref="SaveData"/>.</summary>
        public static SaveData Capture()
        {
            var data = new SaveData
            {
                Version = SaveVersion,
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
            };

            if (TimeOfDay.Instance != null)
            {
                data.Day = TimeOfDay.Instance.Day;
                data.Hour = TimeOfDay.Instance.Hour;
            }
            if (WeatherSystem.Instance != null)
            {
                data.Weather = (int)WeatherSystem.Instance.Current;
                data.Rain = WeatherSystem.Instance.RainIntensity;
                data.Fog = WeatherSystem.Instance.FogIntensity;
                data.Cloud = WeatherSystem.Instance.Cloudiness;
            }
            if (WaterMains.Instance != null)
            {
                data.MainsOn = WaterMains.Instance.IsOn;
                data.ShutoffDay = WaterMains.Instance.ShutoffDay;
            }

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                data.Kills = gm.ZombieKills;
                data.Wave = gm.Wave;
                data.TimeSurvived = gm.TimeSurvived;
                if (gm.Player != null) data.Player = CapturePlayer(gm.Player);
            }

            for (int i = 0; i < LootContainer.All.Count; i++)
            {
                LootContainer c = LootContainer.All[i];
                if (c != null) data.Containers.Add(CaptureContainer(c));
            }
            for (int i = 0; i < Barricade.All.Count; i++)
            {
                Barricade b = Barricade.All[i];
                if (b != null && b.Planks > 0)
                    data.Barricades.Add(new BarricadeSave
                    {
                        Position = b.transform.position,
                        Forward = b.transform.forward,
                        Planks = b.Planks,
                        Health = b.Health,
                        MaxHealth = b.MaxHealth,
                        PerPlank = b.PerPlankHealth,
                    });
            }
            for (int i = 0; i < FarmPlot.All.Count; i++)
            {
                FarmPlot f = FarmPlot.All[i];
                if (f == null) continue;
                data.Farm.Add(new FarmPlotSave
                {
                    Position = f.transform.position,
                    CropId = f.Crop != null ? f.Crop.Id : null,
                    State = (int)f.State,
                    Water = f.Water,
                    Growth = f.Growth01,
                    Health = f.Health,
                    LastHours = f.LastHours,
                });
            }
            for (int i = 0; i < WaterSource.All.Count; i++)
            {
                WaterSource w = WaterSource.All[i];
                if (w == null) continue;
                data.Water.Add(new WaterSourceSave
                {
                    Position = w.transform.position,
                    Kind = (int)w.Kind,
                    Water = w.Water,
                });
            }

            return data;
        }

        private static PlayerSave CapturePlayer(PlayerStats p)
        {
            GameObject go = p.gameObject;
            var s = new PlayerSave
            {
                Position = p.transform.position,
                FacingYaw = p.transform.eulerAngles.y,
                Health = p.Health,
                Endurance = p.Endurance,
                Hunger = p.Hunger,
                Thirst = p.Thirst,
                Fatigue = p.Fatigue,
                Infection = p.Infection,
                Infected = p.Infected,
                Pain = p.Pain,
                Panic = p.Panic,
                Temperature = p.Temperature,
                Wetness = p.Wetness,
                Sickness = p.Sickness,
            };

            var skills = go.GetComponent<PlayerSkills>();
            if (skills != null)
            {
                var values = (PlayerSkills.Skill[])Enum.GetValues(typeof(PlayerSkills.Skill));
                s.SkillXp = new float[values.Length];
                for (int i = 0; i < values.Length; i++) s.SkillXp[i] = skills.GetXp(values[i]);
            }

            var wounds = go.GetComponent<PlayerWounds>();
            if (wounds != null)
            {
                for (int i = 0; i < wounds.Wounds.Count; i++)
                {
                    Wound w = wounds.Wounds[i];
                    s.Wounds.Add(new WoundSave
                    {
                        Type = (int)w.Type,
                        Bleeding = w.Bleeding,
                        Bandaged = w.Bandaged,
                        CleanBandage = w.CleanBandage,
                        Disinfected = w.Disinfected,
                        Infected = w.Infected,
                        Prone = w.Prone,
                        Sepsis = w.Sepsis,
                        Healing = w.Healing,
                        HealSeconds = w.HealSeconds,
                    });
                }
                s.AntibioticTimer = wounds.AntibioticTimer;
                s.PainkillerTimer = wounds.PainkillerTimer;
                s.PainkillerRelief = wounds.PainkillerRelief;
            }

            var panic = go.GetComponent<PlayerPanic>();
            if (panic != null) s.BetaTimer = panic.BetaSecondsRemaining;

            var clothing = go.GetComponent<PlayerClothing>();
            if (clothing != null)
            {
                var worn = new List<ClothingSave>();
                ClothingSlot[] slots = PlayerClothing.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    ItemDef d = clothing.GetWorn(slots[i]);
                    if (d != null) worn.Add(new ClothingSave { Slot = (int)slots[i], Id = d.Id });
                }
                s.Clothing = worn.ToArray();
            }

            var inv = go.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                List<ItemStack> stacks = inv.Inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st == null || st.Def == null) continue;
                    s.Items.Add(new StackSave { Id = st.Def.Id, Count = st.Count, SpawnHours = st.SpawnHours, Condition = st.Condition });
                }
                s.EquippedWeaponId = inv.EquippedWeapon != null ? inv.EquippedWeapon.Id : null;
            }

            var combat = go.GetComponent<PlayerCombat>();
            if (combat != null) s.LoadedAmmo = combat.LoadedAmmo;

            return s;
        }

        private static ContainerSave CaptureContainer(LootContainer c)
        {
            var save = new ContainerSave
            {
                Kind = c.IsSack ? LootContainer.KindSack : (int)c.Type,
                Position = c.transform.position,
                Yaw = c.transform.eulerAngles.y,
                Searched = c.Searched,
                Lifetime = c.Lifetime,
            };
            if (c.Contents != null)
            {
                List<ItemStack> stacks = c.Contents.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st == null || st.Def == null) continue;
                    save.Items.Add(new StackSave { Id = st.Def.Id, Count = st.Count, SpawnHours = st.SpawnHours, Condition = st.Condition });
                }
            }
            return save;
        }
        #endregion

        #region Apply
        /// <summary>Restores the world clock, weather, utilities, score and every dynamic world object.
        /// Call after the static arena is built but BEFORE the navmesh bake (world containers carry
        /// colliders that must carve into the bake).</summary>
        public static void ApplyWorld(SaveData data, Transform world)
        {
            if (data == null) return;

            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(data.Day, data.Hour);
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.Restore((Weather)data.Weather, data.Rain, data.Fog, data.Cloud);
            if (WaterMains.Instance != null) WaterMains.Instance.Restore(data.MainsOn, data.ShutoffDay);
            GameManager.Instance?.RestoreProgress(data.Kills, data.Wave, data.TimeSurvived);

            for (int i = 0; i < data.Containers.Count; i++)
            {
                ContainerSave c = data.Containers[i];
                LootContainer.SpawnSaved(world, c.Kind, c.Position, c.Yaw, c.Searched, c.Lifetime, c.Items);
            }
            for (int i = 0; i < data.Barricades.Count; i++)
            {
                BarricadeSave b = data.Barricades[i];
                Barricade.SpawnRestored(world, b.Position, b.Forward, b.Planks, b.Health, b.MaxHealth, b.PerPlank);
            }
            for (int i = 0; i < data.Farm.Count; i++)
            {
                FarmPlotSave f = data.Farm[i];
                FarmPlot plot = FarmPlot.Spawn(world, f.Position);
                plot.Restore(f.CropId, (FarmPlot.PlotState)f.State, f.Water, f.Growth, f.Health, f.LastHours);
            }
            for (int i = 0; i < data.Water.Count; i++)
            {
                WaterSourceSave w = data.Water[i];
                WaterSource src = WaterSource.Spawn(world, w.Position, (WaterSourceKind)w.Kind);
                src.Restore(w.Water);
            }
        }

        /// <summary>Pours the saved survivor state onto the freshly built player (called after BuildPlayer).</summary>
        public static void ApplyPlayer(SaveData data, PlayerStats p)
        {
            if (data == null || data.Player == null || p == null) return;
            PlayerSave s = data.Player;
            GameObject go = p.gameObject;

            // Place the survivor (disable the CharacterController first — it overrides direct moves).
            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            go.transform.position = s.Position;
            go.transform.rotation = Quaternion.Euler(0f, s.FacingYaw, 0f);
            if (cc != null) cc.enabled = true;

            p.Health = s.Health;
            p.Endurance = s.Endurance;
            p.Hunger = s.Hunger;
            p.Thirst = s.Thirst;
            p.Fatigue = s.Fatigue;
            p.Infection = s.Infection;
            p.Infected = s.Infected;
            p.Pain = s.Pain;
            p.Panic = s.Panic;
            p.Temperature = s.Temperature;
            p.Wetness = s.Wetness;
            p.Sickness = s.Sickness;

            var skills = go.GetComponent<PlayerSkills>();
            if (skills != null && s.SkillXp != null) skills.LoadXp(s.SkillXp);

            var wounds = go.GetComponent<PlayerWounds>();
            if (wounds != null) wounds.LoadState(s.Wounds, s.AntibioticTimer, s.PainkillerTimer, s.PainkillerRelief);

            var panic = go.GetComponent<PlayerPanic>();
            if (panic != null) panic.RestoreBeta(s.BetaTimer);

            var clothing = go.GetComponent<PlayerClothing>();
            if (clothing != null && s.Clothing != null)
            {
                for (int i = 0; i < s.Clothing.Length; i++)
                {
                    ItemDef d = ItemDatabase.Get(s.Clothing[i].Id);
                    if (d != null) clothing.Equip(d);
                }
            }

            var inv = go.GetComponent<PlayerInventory>();
            if (inv != null) inv.RestoreState(s.Items, s.EquippedWeaponId, s.LoadedAmmo);
        }
        #endregion
    }
}
