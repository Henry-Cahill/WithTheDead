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
    /// exists) calls <see cref="ApplyWorld"/> before baking the navmesh,
    /// <see cref="ApplyFixtures"/> once the whole home and yard stand, and
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
            if (PowerGrid.Instance != null)
            {
                data.PowerOn = PowerGrid.Instance.IsOn;
                data.PowerShutoffDay = PowerGrid.Instance.ShutoffDay;
                data.BreakerOn = PowerGrid.Instance.MainBreakerOn;
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
            // Ground items are no longer captured here. They used to be written into this list as
            // sacks, one container plus one stack object each, which made both the save file and the
            // load time grow with the number of things dropped in the world. They now go straight from
            // GroundItemStore into their own chunk-indexed table in SqliteSaveStore.
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
            for (int i = 0; i < BuildPiece.All.Count; i++)
            {
                BuildPiece p = BuildPiece.All[i];
                if (p != null && p.Health > 0f)
                    data.House.Add(new HousePieceSave
                    {
                        Type = (int)p.Type,
                        Position = p.transform.position,
                        Yaw = p.transform.eulerAngles.y,
                        Health = p.Health,
                        MaxHealth = p.MaxHealth,
                        Prebuilt = p.Prebuilt,
                    });
            }
            data.HasPrebuiltFlags = true;
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

            // Pre-placed props the survivor may have torn down (bed / stove / cover). Capturing the
            // survivors means a demolished one simply isn't written, so it stays gone on reload.
            data.HasWorldProps = true;
            for (int i = 0; i < SleepSpot.All.Count; i++)
            {
                SleepSpot s = SleepSpot.All[i];
                if (s != null) data.Props.Add(new PropSave { Kind = PropSave.KindBed, Position = s.transform.position });
            }
            for (int i = 0; i < CookingStation.All.Count; i++)
            {
                CookingStation s = CookingStation.All[i];
                if (s != null) data.Props.Add(new PropSave { Kind = PropSave.KindStove, Position = s.transform.position });
            }
            for (int i = 0; i < Cover.All.Count; i++)
            {
                Cover cv = Cover.All[i];
                if (cv != null) data.Props.Add(new PropSave { Kind = PropSave.KindCover, Position = cv.transform.position, Size = cv.Size });
            }

            // The starter home's furniture; like the props, anything dismantled simply isn't written.
            data.HasFurniture = true;
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f != null)
                    data.Furniture.Add(new FurnitureSave { Kind = (int)f.Kind, Position = f.transform.position, Yaw = f.Yaw, Seed = f.Seed });
            }

            // Every interactive fixture's state (doors, windows, taps, lamps…); their storage rode along with
            // the containers above, under its fixture key.
            data.HasFixtures = true;
            Interactable.CaptureAll(data.Fixtures);

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

            // Saved sitting down, the survivor would come back inside the chair's collider: keep the spot they stood on.
            var seat = go.GetComponent<PlayerSeat>();
            if (seat != null && seat.IsSeated)
            {
                s.Position = seat.StoodFrom;
                s.FacingYaw = seat.StoodYaw;
            }

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
                        Zone = (int)w.Zone,
                        Flags = (w.NeedsStitches ? 1 : 0) | (w.Lodged ? 2 : 0) | (w.Tourniquet ? 4 : 0),
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
                    PlayerClothing.WornPiece piece = clothing.GetPiece(slots[i]);
                    if (piece.Exists) worn.Add(new ClothingSave { Slot = (int)slots[i], Id = piece.Def.Id, Condition = piece.Condition, TimesRepaired = piece.TimesRepaired, Extra = ItemExtra.Serialize(piece.Extra) });
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
                    s.Items.Add(new StackSave { Id = st.Def.Id, Count = st.Count, SpawnHours = st.SpawnHours, Condition = st.Condition, TimesRepaired = st.TimesRepaired, Extra = ItemExtra.Serialize(st.Extra) });
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
                FixtureKey = c.FixtureKey,
            };
            if (c.Contents != null)
            {
                List<ItemStack> stacks = c.Contents.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st == null || st.Def == null) continue;
                    save.Items.Add(new StackSave { Id = st.Def.Id, Count = st.Count, SpawnHours = st.SpawnHours, Condition = st.Condition, TimesRepaired = st.TimesRepaired, Extra = ItemExtra.Serialize(st.Extra) });
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
            if (PowerGrid.Instance != null) PowerGrid.Instance.Restore(data.PowerOn, data.PowerShutoffDay, data.BreakerOn);
            GameManager.Instance?.RestoreProgress(data.Kills, data.Wave, data.TimeSurvived);

            // House pieces first: the containers, fittings and furniture standing on the floor rest on it.
            for (int i = 0; i < data.House.Count; i++)
            {
                HousePieceSave h = data.House[i];
                var type = (BuildPieceType)h.Type;
                // A save from before pieces recorded it: whatever still stands in one of the starter home's own
                // slots is taken for the original house, so loading it can't turn the free home into planks.
                bool prebuilt = data.HasPrebuiltFlags ? h.Prebuilt : StarterHome.IsBlueprintSlot(type, h.Position);
                BuildPiece.SpawnRestored(world, type, h.Position, h.Yaw, h.Health, h.MaxHealth, prebuilt);
            }
            for (int i = 0; i < data.Containers.Count; i++)
            {
                ContainerSave c = data.Containers[i];
                // A fixture's storage (a cupboard, a drawer, the fridge) is rebuilt with its fixture; its contents
                // are poured back in by ApplyFixtures once the whole home stands.
                if (!string.IsNullOrEmpty(c.FixtureKey)) continue;
                if (c.Kind == LootContainer.KindSack)
                {
                    // A dropped ground item, saved as a single-stack sack. Parented to the scene root
                    // (null) so its mesh is never collected into the navmesh bake.
                    if (c.Items != null && c.Items.Count > 0)
                    {
                        StackSave st = c.Items[0];
                        ItemDef def = ItemDatabase.Get(st.Id);
                        if (def != null)
                            WorldItem.Spawn(null, c.Position, def, st.Count,
                                def.Perishable ? st.SpawnHours : float.NaN,
                                def.RestoredCondition(st.Condition),
                                st.TimesRepaired, ItemExtra.Parse(st.Extra));
                    }
                }
                else
                {
                    LootContainer.SpawnSaved(world, c.Kind, c.Position, c.Yaw, c.Searched, c.Lifetime, c.Items);
                }
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
            for (int i = 0; i < data.Props.Count; i++)
            {
                PropSave p = data.Props[i];
                switch (p.Kind)
                {
                    case PropSave.KindBed: SleepSpot.Spawn(world, p.Position); break;
                    case PropSave.KindStove: CookingStation.Spawn(world, p.Position); break;
                    case PropSave.KindCover: Cover.Spawn(world, p.Position, p.Size); break;
                }
            }
            for (int i = 0; i < data.Furniture.Count; i++)
            {
                FurnitureSave f = data.Furniture[i];
                if (!System.Enum.IsDefined(typeof(HomeFurnitureKind), f.Kind)) continue;
                HomeFurniture.Spawn(world, (HomeFurnitureKind)f.Kind, f.Position, f.Yaw, f.Seed);
            }
        }

        /// <summary>Puts every saved fixture's state (doors, windows, taps, lamps…) and its storage's contents back.
        /// Call once the whole world stands — after the navmesh bake and the yard are built — so every fixture
        /// exists to receive its state. Fixtures the save doesn't mention keep their fresh-run defaults.</summary>
        public static void ApplyFixtures(SaveData data)
        {
            if (data == null) return;
            Interactable.RestoreAll(data.Fixtures, data.Containers);
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
                    if (d != null) clothing.Equip(d, d.RestoredCondition(s.Clothing[i].Condition), s.Clothing[i].TimesRepaired, ItemExtra.Parse(s.Clothing[i].Extra));
                }
            }

            var inv = go.GetComponent<PlayerInventory>();
            if (inv != null) inv.RestoreState(s.Items, s.EquippedWeaponId, s.LoadedAmmo);
        }
        #endregion
    }
}
