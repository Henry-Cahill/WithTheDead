using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CloneZ.Db;

namespace CloneZ
{
    /// <summary>
    /// The relational persistence layer: decomposes a <see cref="SaveData"/> snapshot into
    /// normalised SQLite tables on <see cref="Write"/> and reassembles it on <see cref="Read"/>.
    /// One table per DTO collection, with child rows (pack/container items) linked by foreign key.
    ///
    /// <see cref="SaveSystem"/> owns the capture/apply logic and calls into this store for I/O;
    /// this class knows nothing about the live game — only the DTO graph and the schema.
    /// Every write is a single transaction that drops and rebuilds the schema, so the file is
    /// always a clean, consistent snapshot that exactly matches the current DTOs.
    /// </summary>
    public static class SqliteSaveStore
    {
        private const string DropSql =
            "DROP TABLE IF EXISTS container_item;" +
            "DROP TABLE IF EXISTS player_item;" +
            "DROP TABLE IF EXISTS clothing;" +
            "DROP TABLE IF EXISTS wound;" +
            "DROP TABLE IF EXISTS player_skill;" +
            "DROP TABLE IF EXISTS water_source;" +
            "DROP TABLE IF EXISTS farm_plot;" +
            "DROP TABLE IF EXISTS barricade;" +
            "DROP TABLE IF EXISTS container;" +
            "DROP TABLE IF EXISTS player;" +
            "DROP TABLE IF EXISTS meta;";

        private const string SchemaSql =
            "CREATE TABLE meta (" +
            "  id INTEGER PRIMARY KEY CHECK (id = 0)," +
            "  version INTEGER, savedAtUtc TEXT," +
            "  day INTEGER, hour REAL, weather INTEGER, rain REAL, fog REAL, cloud REAL," +
            "  mainsOn INTEGER, shutoffDay INTEGER, kills INTEGER, wave INTEGER, timeSurvived REAL);" +

            "CREATE TABLE player (" +
            "  id INTEGER PRIMARY KEY CHECK (id = 0)," +
            "  posX REAL, posY REAL, posZ REAL, facingYaw REAL," +
            "  health REAL, endurance REAL, hunger REAL, thirst REAL, fatigue REAL, infection REAL," +
            "  infected INTEGER, pain REAL, panic REAL, temperature REAL, wetness REAL, sickness REAL," +
            "  antibioticTimer REAL, painkillerTimer REAL, painkillerRelief REAL, betaTimer REAL," +
            "  equippedWeaponId TEXT, loadedAmmo INTEGER);" +

            "CREATE TABLE player_skill (idx INTEGER PRIMARY KEY, xp REAL);" +

            "CREATE TABLE wound (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  type INTEGER, bleeding INTEGER, bandaged INTEGER, cleanBandage INTEGER," +
            "  disinfected INTEGER, infected INTEGER, prone INTEGER," +
            "  sepsis REAL, healing REAL, healSeconds REAL);" +

            "CREATE TABLE clothing (slot INTEGER PRIMARY KEY, itemId TEXT);" +

            "CREATE TABLE player_item (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  itemId TEXT, count INTEGER, spawnHours REAL, condition INTEGER);" +

            "CREATE TABLE container (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  kind INTEGER, posX REAL, posY REAL, posZ REAL, yaw REAL," +
            "  searched INTEGER, lifetime REAL);" +

            "CREATE TABLE container_item (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  containerId INTEGER, itemId TEXT, count INTEGER, spawnHours REAL, condition INTEGER," +
            "  FOREIGN KEY (containerId) REFERENCES container(id));" +

            "CREATE TABLE barricade (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, fwdX REAL, fwdY REAL, fwdZ REAL," +
            "  planks INTEGER, health REAL, maxHealth REAL, perPlank REAL);" +

            "CREATE TABLE farm_plot (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, cropId TEXT, state INTEGER," +
            "  water REAL, growth REAL, health REAL, lastHours REAL);" +

            "CREATE TABLE water_source (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, kind INTEGER, water REAL);";

        #region Write
        /// <summary>Persists a snapshot to <paramref name="path"/> as a single atomic transaction.</summary>
        public static void Write(string path, SaveData data)
        {
            if (data == null) return;
            using (var db = new SqliteConnection(path))
            {
                db.Execute("BEGIN;");
                try
                {
                    db.Execute(DropSql);
                    db.Execute(SchemaSql);
                    WriteMeta(db, data);
                    WritePlayer(db, data.Player);
                    WriteContainers(db, data.Containers);
                    WriteBarricades(db, data.Barricades);
                    WriteFarm(db, data.Farm);
                    WriteWater(db, data.Water);
                    db.Execute("COMMIT;");
                }
                catch
                {
                    try { db.Execute("ROLLBACK;"); } catch { /* connection is being torn down anyway */ }
                    throw;
                }
            }
        }

        private static void WriteMeta(SqliteConnection db, SaveData d)
        {
            using (var st = db.Prepare(
                "INSERT INTO meta (id,version,savedAtUtc,day,hour,weather,rain,fog,cloud,mainsOn,shutoffDay,kills,wave,timeSurvived)" +
                " VALUES (0,?,?,?,?,?,?,?,?,?,?,?,?,?);"))
            {
                st.Bind(1, d.Version).Bind(2, d.SavedAtUtc).Bind(3, d.Day).Bind(4, d.Hour)
                  .Bind(5, d.Weather).Bind(6, d.Rain).Bind(7, d.Fog).Bind(8, d.Cloud)
                  .Bind(9, d.MainsOn).Bind(10, d.ShutoffDay).Bind(11, d.Kills).Bind(12, d.Wave).Bind(13, d.TimeSurvived);
                st.Step();
            }
        }

        private static void WritePlayer(SqliteConnection db, PlayerSave p)
        {
            if (p == null) return;

            using (var st = db.Prepare(
                "INSERT INTO player (id,posX,posY,posZ,facingYaw,health,endurance,hunger,thirst,fatigue,infection," +
                "infected,pain,panic,temperature,wetness,sickness,antibioticTimer,painkillerTimer,painkillerRelief," +
                "betaTimer,equippedWeaponId,loadedAmmo)" +
                " VALUES (0,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);"))
            {
                st.Bind(1, p.Position.x).Bind(2, p.Position.y).Bind(3, p.Position.z).Bind(4, p.FacingYaw)
                  .Bind(5, p.Health).Bind(6, p.Endurance).Bind(7, p.Hunger).Bind(8, p.Thirst).Bind(9, p.Fatigue).Bind(10, p.Infection)
                  .Bind(11, p.Infected).Bind(12, p.Pain).Bind(13, p.Panic).Bind(14, p.Temperature).Bind(15, p.Wetness).Bind(16, p.Sickness)
                  .Bind(17, p.AntibioticTimer).Bind(18, p.PainkillerTimer).Bind(19, p.PainkillerRelief).Bind(20, p.BetaTimer)
                  .Bind(21, p.EquippedWeaponId).Bind(22, p.LoadedAmmo);
                st.Step();
            }

            if (p.SkillXp != null)
                using (var st = db.Prepare("INSERT INTO player_skill (idx,xp) VALUES (?,?);"))
                    for (int i = 0; i < p.SkillXp.Length; i++)
                    {
                        st.Reset();
                        st.Bind(1, i).Bind(2, p.SkillXp[i]);
                        st.Step();
                    }

            if (p.Wounds != null)
                using (var st = db.Prepare(
                    "INSERT INTO wound (type,bleeding,bandaged,cleanBandage,disinfected,infected,prone,sepsis,healing,healSeconds)" +
                    " VALUES (?,?,?,?,?,?,?,?,?,?);"))
                    foreach (WoundSave w in p.Wounds)
                    {
                        st.Reset();
                        st.Bind(1, w.Type).Bind(2, w.Bleeding).Bind(3, w.Bandaged).Bind(4, w.CleanBandage).Bind(5, w.Disinfected)
                          .Bind(6, w.Infected).Bind(7, w.Prone).Bind(8, w.Sepsis).Bind(9, w.Healing).Bind(10, w.HealSeconds);
                        st.Step();
                    }

            if (p.Clothing != null)
                using (var st = db.Prepare("INSERT INTO clothing (slot,itemId) VALUES (?,?);"))
                    foreach (ClothingSave c in p.Clothing)
                    {
                        st.Reset();
                        st.Bind(1, c.Slot).Bind(2, c.Id);
                        st.Step();
                    }

            if (p.Items != null)
                using (var st = db.Prepare("INSERT INTO player_item (itemId,count,spawnHours,condition) VALUES (?,?,?,?);"))
                    foreach (StackSave it in p.Items)
                    {
                        st.Reset();
                        st.Bind(1, it.Id).Bind(2, it.Count).Bind(3, it.SpawnHours).Bind(4, it.Condition);
                        st.Step();
                    }
        }

        private static void WriteContainers(SqliteConnection db, List<ContainerSave> containers)
        {
            if (containers == null) return;
            using (var cst = db.Prepare(
                "INSERT INTO container (kind,posX,posY,posZ,yaw,searched,lifetime) VALUES (?,?,?,?,?,?,?);"))
            using (var ist = db.Prepare(
                "INSERT INTO container_item (containerId,itemId,count,spawnHours,condition) VALUES (?,?,?,?,?);"))
            {
                foreach (ContainerSave c in containers)
                {
                    cst.Reset();
                    cst.Bind(1, c.Kind).Bind(2, c.Position.x).Bind(3, c.Position.y).Bind(4, c.Position.z)
                       .Bind(5, c.Yaw).Bind(6, c.Searched).Bind(7, c.Lifetime);
                    cst.Step();
                    long containerId = db.LastInsertRowId;

                    if (c.Items == null) continue;
                    foreach (StackSave it in c.Items)
                    {
                        ist.Reset();
                        ist.Bind(1, containerId).Bind(2, it.Id).Bind(3, it.Count).Bind(4, it.SpawnHours).Bind(5, it.Condition);
                        ist.Step();
                    }
                }
            }
        }

        private static void WriteBarricades(SqliteConnection db, List<BarricadeSave> barricades)
        {
            if (barricades == null) return;
            using (var st = db.Prepare(
                "INSERT INTO barricade (posX,posY,posZ,fwdX,fwdY,fwdZ,planks,health,maxHealth,perPlank)" +
                " VALUES (?,?,?,?,?,?,?,?,?,?);"))
                foreach (BarricadeSave b in barricades)
                {
                    st.Reset();
                    st.Bind(1, b.Position.x).Bind(2, b.Position.y).Bind(3, b.Position.z)
                      .Bind(4, b.Forward.x).Bind(5, b.Forward.y).Bind(6, b.Forward.z)
                      .Bind(7, b.Planks).Bind(8, b.Health).Bind(9, b.MaxHealth).Bind(10, b.PerPlank);
                    st.Step();
                }
        }

        private static void WriteFarm(SqliteConnection db, List<FarmPlotSave> farm)
        {
            if (farm == null) return;
            using (var st = db.Prepare(
                "INSERT INTO farm_plot (posX,posY,posZ,cropId,state,water,growth,health,lastHours)" +
                " VALUES (?,?,?,?,?,?,?,?,?);"))
                foreach (FarmPlotSave f in farm)
                {
                    st.Reset();
                    st.Bind(1, f.Position.x).Bind(2, f.Position.y).Bind(3, f.Position.z)
                      .Bind(4, f.CropId).Bind(5, f.State).Bind(6, f.Water).Bind(7, f.Growth).Bind(8, f.Health).Bind(9, f.LastHours);
                    st.Step();
                }
        }

        private static void WriteWater(SqliteConnection db, List<WaterSourceSave> water)
        {
            if (water == null) return;
            using (var st = db.Prepare(
                "INSERT INTO water_source (posX,posY,posZ,kind,water) VALUES (?,?,?,?,?);"))
                foreach (WaterSourceSave w in water)
                {
                    st.Reset();
                    st.Bind(1, w.Position.x).Bind(2, w.Position.y).Bind(3, w.Position.z).Bind(4, w.Kind).Bind(5, w.Water);
                    st.Step();
                }
        }
        #endregion

        #region Read
        /// <summary>Rebuilds a <see cref="SaveData"/> from the database, or null if there is no valid save.</summary>
        public static SaveData Read(string path)
        {
            if (!File.Exists(path)) return null;
            using (var db = new SqliteConnection(path))
            {
                if (!TableExists(db, "meta")) return null;

                var data = new SaveData();
                if (!ReadMeta(db, data)) return null;
                data.Player = ReadPlayer(db);
                ReadContainers(db, data);
                ReadBarricades(db, data);
                ReadFarm(db, data);
                ReadWater(db, data);
                return data;
            }
        }

        private static bool TableExists(SqliteConnection db, string name)
        {
            using (var st = db.Prepare("SELECT 1 FROM sqlite_master WHERE type='table' AND name=?;"))
            {
                st.Bind(1, name);
                return st.Step();
            }
        }

        private static bool ReadMeta(SqliteConnection db, SaveData d)
        {
            using (var st = db.Prepare(
                "SELECT version,savedAtUtc,day,hour,weather,rain,fog,cloud,mainsOn,shutoffDay,kills,wave,timeSurvived FROM meta WHERE id=0;"))
            {
                if (!st.Step()) return false;
                d.Version = st.GetInt(0);
                d.SavedAtUtc = st.GetString(1);
                d.Day = st.GetInt(2);
                d.Hour = st.GetFloat(3);
                d.Weather = st.GetInt(4);
                d.Rain = st.GetFloat(5);
                d.Fog = st.GetFloat(6);
                d.Cloud = st.GetFloat(7);
                d.MainsOn = st.GetBool(8);
                d.ShutoffDay = st.GetInt(9);
                d.Kills = st.GetInt(10);
                d.Wave = st.GetInt(11);
                d.TimeSurvived = st.GetFloat(12);
                return true;
            }
        }

        private static PlayerSave ReadPlayer(SqliteConnection db)
        {
            PlayerSave p = null;
            using (var st = db.Prepare(
                "SELECT posX,posY,posZ,facingYaw,health,endurance,hunger,thirst,fatigue,infection,infected," +
                "pain,panic,temperature,wetness,sickness,antibioticTimer,painkillerTimer,painkillerRelief," +
                "betaTimer,equippedWeaponId,loadedAmmo FROM player WHERE id=0;"))
            {
                if (st.Step())
                    p = new PlayerSave
                    {
                        Position = new Vector3(st.GetFloat(0), st.GetFloat(1), st.GetFloat(2)),
                        FacingYaw = st.GetFloat(3),
                        Health = st.GetFloat(4),
                        Endurance = st.GetFloat(5),
                        Hunger = st.GetFloat(6),
                        Thirst = st.GetFloat(7),
                        Fatigue = st.GetFloat(8),
                        Infection = st.GetFloat(9),
                        Infected = st.GetBool(10),
                        Pain = st.GetFloat(11),
                        Panic = st.GetFloat(12),
                        Temperature = st.GetFloat(13),
                        Wetness = st.GetFloat(14),
                        Sickness = st.GetFloat(15),
                        AntibioticTimer = st.GetFloat(16),
                        PainkillerTimer = st.GetFloat(17),
                        PainkillerRelief = st.GetFloat(18),
                        BetaTimer = st.GetFloat(19),
                        EquippedWeaponId = st.GetString(20),
                        LoadedAmmo = st.GetInt(21),
                    };
            }
            if (p == null) return null;

            var skills = new List<KeyValuePair<int, float>>();
            using (var st = db.Prepare("SELECT idx,xp FROM player_skill ORDER BY idx;"))
                while (st.Step()) skills.Add(new KeyValuePair<int, float>(st.GetInt(0), st.GetFloat(1)));
            if (skills.Count > 0)
            {
                int max = 0;
                foreach (var kv in skills) if (kv.Key > max) max = kv.Key;
                p.SkillXp = new float[max + 1];
                foreach (var kv in skills) p.SkillXp[kv.Key] = kv.Value;
            }

            using (var st = db.Prepare(
                "SELECT type,bleeding,bandaged,cleanBandage,disinfected,infected,prone,sepsis,healing,healSeconds FROM wound ORDER BY id;"))
                while (st.Step())
                    p.Wounds.Add(new WoundSave
                    {
                        Type = st.GetInt(0),
                        Bleeding = st.GetBool(1),
                        Bandaged = st.GetBool(2),
                        CleanBandage = st.GetBool(3),
                        Disinfected = st.GetBool(4),
                        Infected = st.GetBool(5),
                        Prone = st.GetBool(6),
                        Sepsis = st.GetFloat(7),
                        Healing = st.GetFloat(8),
                        HealSeconds = st.GetFloat(9),
                    });

            var clothes = new List<ClothingSave>();
            using (var st = db.Prepare("SELECT slot,itemId FROM clothing ORDER BY slot;"))
                while (st.Step())
                    clothes.Add(new ClothingSave { Slot = st.GetInt(0), Id = st.GetString(1) });
            p.Clothing = clothes.ToArray();

            using (var st = db.Prepare("SELECT itemId,count,spawnHours,condition FROM player_item ORDER BY seq;"))
                while (st.Step())
                    p.Items.Add(new StackSave
                    {
                        Id = st.GetString(0),
                        Count = st.GetInt(1),
                        SpawnHours = st.GetFloat(2),
                        Condition = st.GetInt(3),
                    });

            return p;
        }

        private static void ReadContainers(SqliteConnection db, SaveData d)
        {
            var byId = new Dictionary<long, ContainerSave>();
            using (var st = db.Prepare("SELECT id,kind,posX,posY,posZ,yaw,searched,lifetime FROM container ORDER BY id;"))
                while (st.Step())
                {
                    long id = st.GetInt64(0);
                    var c = new ContainerSave
                    {
                        Kind = st.GetInt(1),
                        Position = new Vector3(st.GetFloat(2), st.GetFloat(3), st.GetFloat(4)),
                        Yaw = st.GetFloat(5),
                        Searched = st.GetBool(6),
                        Lifetime = st.GetFloat(7),
                    };
                    byId[id] = c;
                    d.Containers.Add(c);
                }

            using (var st = db.Prepare(
                "SELECT containerId,itemId,count,spawnHours,condition FROM container_item ORDER BY seq;"))
                while (st.Step())
                    if (byId.TryGetValue(st.GetInt64(0), out ContainerSave c))
                        c.Items.Add(new StackSave
                        {
                            Id = st.GetString(1),
                            Count = st.GetInt(2),
                            SpawnHours = st.GetFloat(3),
                            Condition = st.GetInt(4),
                        });
        }

        private static void ReadBarricades(SqliteConnection db, SaveData d)
        {
            using (var st = db.Prepare(
                "SELECT posX,posY,posZ,fwdX,fwdY,fwdZ,planks,health,maxHealth,perPlank FROM barricade ORDER BY id;"))
                while (st.Step())
                    d.Barricades.Add(new BarricadeSave
                    {
                        Position = new Vector3(st.GetFloat(0), st.GetFloat(1), st.GetFloat(2)),
                        Forward = new Vector3(st.GetFloat(3), st.GetFloat(4), st.GetFloat(5)),
                        Planks = st.GetInt(6),
                        Health = st.GetFloat(7),
                        MaxHealth = st.GetFloat(8),
                        PerPlank = st.GetFloat(9),
                    });
        }

        private static void ReadFarm(SqliteConnection db, SaveData d)
        {
            using (var st = db.Prepare(
                "SELECT posX,posY,posZ,cropId,state,water,growth,health,lastHours FROM farm_plot ORDER BY id;"))
                while (st.Step())
                    d.Farm.Add(new FarmPlotSave
                    {
                        Position = new Vector3(st.GetFloat(0), st.GetFloat(1), st.GetFloat(2)),
                        CropId = st.GetString(3),
                        State = st.GetInt(4),
                        Water = st.GetFloat(5),
                        Growth = st.GetFloat(6),
                        Health = st.GetFloat(7),
                        LastHours = st.GetFloat(8),
                    });
        }

        private static void ReadWater(SqliteConnection db, SaveData d)
        {
            using (var st = db.Prepare("SELECT posX,posY,posZ,kind,water FROM water_source ORDER BY id;"))
                while (st.Step())
                    d.Water.Add(new WaterSourceSave
                    {
                        Position = new Vector3(st.GetFloat(0), st.GetFloat(1), st.GetFloat(2)),
                        Kind = st.GetInt(3),
                        Water = st.GetFloat(4),
                    });
        }
        #endregion
    }
}
