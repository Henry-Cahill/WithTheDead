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
            "DROP TABLE IF EXISTS ground_item;" +
            "DROP TABLE IF EXISTS ground_type;" +
            "DROP TABLE IF EXISTS container_item;" +
            "DROP TABLE IF EXISTS player_item;" +
            "DROP TABLE IF EXISTS clothing;" +
            "DROP TABLE IF EXISTS wound;" +
            "DROP TABLE IF EXISTS player_skill;" +
            "DROP TABLE IF EXISTS home_fixture;" +
            "DROP TABLE IF EXISTS home_furniture;" +
            "DROP TABLE IF EXISTS world_prop;" +
            "DROP TABLE IF EXISTS water_source;" +
            "DROP TABLE IF EXISTS farm_plot;" +
            "DROP TABLE IF EXISTS house_piece;" +
            "DROP TABLE IF EXISTS barricade;" +
            "DROP TABLE IF EXISTS container;" +
            "DROP TABLE IF EXISTS player;" +
            "DROP TABLE IF EXISTS meta;";

        private const string SchemaSql =
            "CREATE TABLE meta (" +
            "  id INTEGER PRIMARY KEY CHECK (id = 0)," +
            "  version INTEGER, savedAtUtc TEXT," +
            "  day INTEGER, hour REAL, weather INTEGER, rain REAL, fog REAL, cloud REAL," +
            "  mainsOn INTEGER, shutoffDay INTEGER, kills INTEGER, wave INTEGER, timeSurvived REAL," +
            "  powerOn INTEGER, powerShutoffDay INTEGER, breakerOn INTEGER);" +

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
            "  sepsis REAL, healing REAL, healSeconds REAL, zone INTEGER, flags INTEGER);" +

            "CREATE TABLE clothing (slot INTEGER PRIMARY KEY, itemId TEXT, condition INTEGER, timesRepaired INTEGER, extra TEXT);" +

            "CREATE TABLE player_item (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  itemId TEXT, count INTEGER, spawnHours REAL, condition INTEGER, timesRepaired INTEGER, extra TEXT);" +

            "CREATE TABLE container (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  kind INTEGER, posX REAL, posY REAL, posZ REAL, yaw REAL," +
            "  searched INTEGER, lifetime REAL, fixtureKey TEXT);" +

            "CREATE TABLE container_item (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  containerId INTEGER, itemId TEXT, count INTEGER, spawnHours REAL, condition INTEGER, timesRepaired INTEGER, extra TEXT," +
            "  FOREIGN KEY (containerId) REFERENCES container(id));" +

            "CREATE TABLE barricade (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, fwdX REAL, fwdY REAL, fwdZ REAL," +
            "  planks INTEGER, health REAL, maxHealth REAL, perPlank REAL);" +

            "CREATE TABLE house_piece (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  type INTEGER, posX REAL, posY REAL, posZ REAL, yaw REAL, health REAL, maxHealth REAL, prebuilt INTEGER);" +

            "CREATE TABLE farm_plot (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, cropId TEXT, state INTEGER," +
            "  water REAL, growth REAL, health REAL, lastHours REAL);" +

            "CREATE TABLE water_source (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  posX REAL, posY REAL, posZ REAL, kind INTEGER, water REAL);" +

            "CREATE TABLE world_prop (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  kind INTEGER, posX REAL, posY REAL, posZ REAL, sizeX REAL, sizeY REAL, sizeZ REAL);" +

            "CREATE TABLE home_furniture (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  kind INTEGER, posX REAL, posY REAL, posZ REAL, yaw REAL, seed INTEGER);" +

            // The state of every interactive fixture (door open / locked, tap running, lamp lit…), keyed by
            // the fixture's stable id; what state/a/b/text mean is up to each fixture.
            "CREATE TABLE home_fixture (" +
            "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  key TEXT, state INTEGER, a REAL, b REAL, text TEXT);" +

            // Items lying on the ground. These used to ride in the container table, one row per item
            // plus one per stack, which made both the save and the load scale with the number of things
            // dropped in the world — untenable once that runs to millions. They now have a table of
            // their own, filed by world chunk and indexed on it, so a chunk's worth of items can be
            // read back on its own without touching the rest. The item id is interned in ground_type
            // so the per-item row stays narrow.
            "CREATE TABLE ground_type (idx INTEGER PRIMARY KEY, itemId TEXT);" +

            "CREATE TABLE ground_item (" +
            "  cx INTEGER, cz INTEGER, type INTEGER," +
            "  x REAL, z REAL, count INTEGER, spawnHours REAL," +
            "  condition INTEGER, timesRepaired INTEGER, yaw INTEGER, extra TEXT);" +

            "CREATE INDEX ground_item_chunk ON ground_item (cx, cz);";

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
                    WriteGroundItems(db);
                    WriteBarricades(db, data.Barricades);
                    WriteHouse(db, data.House);
                    WriteFarm(db, data.Farm);
                    WriteWater(db, data.Water);
                    WriteProps(db, data.Props);
                    WriteFurniture(db, data.Furniture);
                    WriteFixtures(db, data.Fixtures);
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
                "INSERT INTO meta (id,version,savedAtUtc,day,hour,weather,rain,fog,cloud,mainsOn,shutoffDay,kills,wave,timeSurvived,powerOn,powerShutoffDay,breakerOn)" +
                " VALUES (0,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);"))
            {
                st.Bind(1, d.Version).Bind(2, d.SavedAtUtc).Bind(3, d.Day).Bind(4, d.Hour)
                  .Bind(5, d.Weather).Bind(6, d.Rain).Bind(7, d.Fog).Bind(8, d.Cloud)
                  .Bind(9, d.MainsOn).Bind(10, d.ShutoffDay).Bind(11, d.Kills).Bind(12, d.Wave).Bind(13, d.TimeSurvived)
                  .Bind(14, d.PowerOn).Bind(15, d.PowerShutoffDay).Bind(16, d.BreakerOn);
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
                    "INSERT INTO wound (type,bleeding,bandaged,cleanBandage,disinfected,infected,prone,sepsis,healing,healSeconds,zone,flags)" +
                    " VALUES (?,?,?,?,?,?,?,?,?,?,?,?);"))
                    foreach (WoundSave w in p.Wounds)
                    {
                        st.Reset();
                        st.Bind(1, w.Type).Bind(2, w.Bleeding).Bind(3, w.Bandaged).Bind(4, w.CleanBandage).Bind(5, w.Disinfected)
                          .Bind(6, w.Infected).Bind(7, w.Prone).Bind(8, w.Sepsis).Bind(9, w.Healing).Bind(10, w.HealSeconds)
                          .Bind(11, w.Zone).Bind(12, w.Flags);
                        st.Step();
                    }

            if (p.Clothing != null)
                using (var st = db.Prepare("INSERT INTO clothing (slot,itemId,condition,timesRepaired,extra) VALUES (?,?,?,?,?);"))
                    foreach (ClothingSave c in p.Clothing)
                    {
                        st.Reset();
                        st.Bind(1, c.Slot).Bind(2, c.Id).Bind(3, c.Condition).Bind(4, c.TimesRepaired).Bind(5, c.Extra);
                        st.Step();
                    }

            if (p.Items != null)
                using (var st = db.Prepare("INSERT INTO player_item (itemId,count,spawnHours,condition,timesRepaired,extra) VALUES (?,?,?,?,?,?);"))
                    foreach (StackSave it in p.Items)
                    {
                        st.Reset();
                        st.Bind(1, it.Id).Bind(2, it.Count).Bind(3, it.SpawnHours).Bind(4, it.Condition).Bind(5, it.TimesRepaired).Bind(6, it.Extra);
                        st.Step();
                    }
        }

        private static void WriteContainers(SqliteConnection db, List<ContainerSave> containers)
        {            if (containers == null) return;
            using (var cst = db.Prepare(
                "INSERT INTO container (kind,posX,posY,posZ,yaw,searched,lifetime,fixtureKey) VALUES (?,?,?,?,?,?,?,?);"))
            using (var ist = db.Prepare(
                "INSERT INTO container_item (containerId,itemId,count,spawnHours,condition,timesRepaired,extra) VALUES (?,?,?,?,?,?,?);"))
            {
                foreach (ContainerSave c in containers)
                {
                    cst.Reset();
                    cst.Bind(1, c.Kind).Bind(2, c.Position.x).Bind(3, c.Position.y).Bind(4, c.Position.z)
                       .Bind(5, c.Yaw).Bind(6, c.Searched).Bind(7, c.Lifetime).Bind(8, c.FixtureKey);
                    cst.Step();
                    long containerId = db.LastInsertRowId;

                    if (c.Items == null) continue;
                    foreach (StackSave it in c.Items)
                    {
                        ist.Reset();
                        ist.Bind(1, containerId).Bind(2, it.Id).Bind(3, it.Count).Bind(4, it.SpawnHours).Bind(5, it.Condition).Bind(6, it.TimesRepaired).Bind(7, it.Extra);
                        ist.Step();
                    }
                }
            }
        }

        /// <summary>
        /// Writes every item lying on the ground, filed by chunk.
        ///
        /// This reads <see cref="GroundItemStore"/> directly rather than going through
        /// <see cref="SaveData"/>. Ground items are the one thing in the world whose count is
        /// unbounded, so routing millions of them through a list of DTOs — a container object and a
        /// stack object each — would cost more in garbage than in database writes.
        /// </summary>
        private static void WriteGroundItems(SqliteConnection db)
        {
            // Intern the item ids first, so each row carries a small integer instead of a string.
            var typeIndex = new Dictionary<string, int>(128);
            using (var tst = db.Prepare("INSERT INTO ground_type (idx,itemId) VALUES (?,?);"))
            {
                foreach (GroundItemStore.Chunk chunk in GroundItemStore.AllChunks)
                {
                    for (int i = 0; i < chunk.Items.Count; i++)
                    {
                        ItemDef def = GroundItemStore.TypeAt(chunk.Items[i].TypeIndex);
                        if (def == null || typeIndex.ContainsKey(def.Id)) continue;

                        int idx = typeIndex.Count;
                        typeIndex[def.Id] = idx;
                        tst.Reset();
                        tst.Bind(1, idx).Bind(2, def.Id);
                        tst.Step();
                    }
                }
            }

            using (var st = db.Prepare(
                "INSERT INTO ground_item (cx,cz,type,x,z,count,spawnHours,condition,timesRepaired,yaw,extra)" +
                " VALUES (?,?,?,?,?,?,?,?,?,?,?);"))
            {
                foreach (GroundItemStore.Chunk chunk in GroundItemStore.AllChunks)
                {
                    GroundItemStore.Coords(chunk.Key, out int cx, out int cz);

                    for (int i = 0; i < chunk.Items.Count; i++)
                    {
                        GroundItemStore.Item it = chunk.Items[i];
                        ItemDef def = GroundItemStore.TypeAt(it.TypeIndex);
                        if (def == null || !typeIndex.TryGetValue(def.Id, out int type)) continue;

                        st.Reset();
                        st.Bind(1, cx).Bind(2, cz).Bind(3, type)
                          .Bind(4, it.X).Bind(5, it.Z).Bind(6, it.Count).Bind(7, it.SpawnHours)
                          .Bind(8, (int)it.Condition).Bind(9, (int)it.TimesRepaired).Bind(10, (int)it.Yaw)
                          .Bind(11, ItemExtra.Serialize(GroundItemStore.ExtraOf(it.Id)));
                        st.Step();
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

        private static void WriteHouse(SqliteConnection db, List<HousePieceSave> house)
        {
            if (house == null) return;
            using (var st = db.Prepare(
                "INSERT INTO house_piece (type,posX,posY,posZ,yaw,health,maxHealth,prebuilt) VALUES (?,?,?,?,?,?,?,?);"))
                foreach (HousePieceSave h in house)
                {
                    st.Reset();
                    st.Bind(1, h.Type).Bind(2, h.Position.x).Bind(3, h.Position.y).Bind(4, h.Position.z)
                      .Bind(5, h.Yaw).Bind(6, h.Health).Bind(7, h.MaxHealth).Bind(8, h.Prebuilt);
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

        private static void WriteProps(SqliteConnection db, List<PropSave> props)
        {
            if (props == null) return;
            using (var st = db.Prepare(
                "INSERT INTO world_prop (kind,posX,posY,posZ,sizeX,sizeY,sizeZ) VALUES (?,?,?,?,?,?,?);"))
                foreach (PropSave p in props)
                {
                    st.Reset();
                    st.Bind(1, p.Kind).Bind(2, p.Position.x).Bind(3, p.Position.y).Bind(4, p.Position.z)
                      .Bind(5, p.Size.x).Bind(6, p.Size.y).Bind(7, p.Size.z);
                    st.Step();
                }
        }

        private static void WriteFurniture(SqliteConnection db, List<FurnitureSave> furniture)
        {
            if (furniture == null) return;
            using (var st = db.Prepare(
                "INSERT INTO home_furniture (kind,posX,posY,posZ,yaw,seed) VALUES (?,?,?,?,?,?);"))
                foreach (FurnitureSave f in furniture)
                {
                    st.Reset();
                    st.Bind(1, f.Kind).Bind(2, f.Position.x).Bind(3, f.Position.y).Bind(4, f.Position.z)
                      .Bind(5, f.Yaw).Bind(6, f.Seed);
                    st.Step();
                }
        }

        private static void WriteFixtures(SqliteConnection db, List<FixtureSave> fixtures)
        {
            if (fixtures == null) return;
            using (var st = db.Prepare("INSERT INTO home_fixture (key,state,a,b,text) VALUES (?,?,?,?,?);"))
                foreach (FixtureSave f in fixtures)
                {
                    if (f == null || string.IsNullOrEmpty(f.Key)) continue;
                    st.Reset();
                    st.Bind(1, f.Key).Bind(2, f.State).Bind(3, f.A).Bind(4, f.B).Bind(5, f.Text);
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
                ReadGroundItems(db);
                ReadBarricades(db, data);
                ReadHouse(db, data);
                ReadFarm(db, data);
                ReadWater(db, data);
                ReadProps(db, data);
                ReadFurniture(db, data);
                ReadFixtures(db, data);
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

        // PRAGMA arguments can't be bound, so the table name goes into the statement text; callers only ever
        // pass one of this store's own table names.
        private static bool ColumnExists(SqliteConnection db, string table, string column)
        {
            using (var st = db.Prepare("PRAGMA table_info(" + table + ");"))
                while (st.Step())
                    if (st.GetString(1) == column) return true;   // columns: cid, name, type, notnull, dflt_value, pk
            return false;
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
            }

            // The power grid came later: an older save keeps the defaults (power on, failure day 0 = schedule one).
            if (ColumnExists(db, "meta", "powerOn"))
                using (var st = db.Prepare("SELECT powerOn,powerShutoffDay,breakerOn FROM meta WHERE id=0;"))
                    if (st.Step())
                    {
                        d.PowerOn = st.GetBool(0);
                        d.PowerShutoffDay = st.GetInt(1);
                        d.BreakerOn = st.GetBool(2);
                    }
            return true;
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

            bool woundZones = ColumnExists(db, "wound", "zone");   // saves from before the limb model had none
            using (var st = db.Prepare(woundZones
                ? "SELECT type,bleeding,bandaged,cleanBandage,disinfected,infected,prone,sepsis,healing,healSeconds,zone,flags FROM wound ORDER BY id;"
                : "SELECT type,bleeding,bandaged,cleanBandage,disinfected,infected,prone,sepsis,healing,healSeconds FROM wound ORDER BY id;"))
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
                        Zone = woundZones ? st.GetInt(10) : (int)BodyZone.Thorax,
                        Flags = woundZones ? st.GetInt(11) : 0,
                    });

            var clothes = new List<ClothingSave>();
            bool clothExtra = ColumnExists(db, "clothing", "extra");   // saves from before item extras had none
            using (var st = db.Prepare(clothExtra
                ? "SELECT slot,itemId,condition,timesRepaired,extra FROM clothing ORDER BY slot;"
                : "SELECT slot,itemId,condition,timesRepaired FROM clothing ORDER BY slot;"))
                while (st.Step())
                    clothes.Add(new ClothingSave { Slot = st.GetInt(0), Id = st.GetString(1), Condition = st.GetInt(2), TimesRepaired = st.GetInt(3), Extra = clothExtra ? st.GetString(4) : null });
            p.Clothing = clothes.ToArray();

            bool itemExtra = ColumnExists(db, "player_item", "extra");
            using (var st = db.Prepare(itemExtra
                ? "SELECT itemId,count,spawnHours,condition,timesRepaired,extra FROM player_item ORDER BY seq;"
                : "SELECT itemId,count,spawnHours,condition,timesRepaired FROM player_item ORDER BY seq;"))
                while (st.Step())
                    p.Items.Add(new StackSave
                    {
                        Id = st.GetString(0),
                        Count = st.GetInt(1),
                        SpawnHours = st.GetFloat(2),
                        Condition = st.GetInt(3),
                        TimesRepaired = st.GetInt(4),
                        Extra = itemExtra ? st.GetString(5) : null,
                    });

            return p;
        }

        private static void ReadContainers(SqliteConnection db, SaveData d)
        {
            var byId = new Dictionary<long, ContainerSave>();
            bool keys = ColumnExists(db, "container", "fixtureKey");   // older saves had no fixture storage
            using (var st = db.Prepare(keys
                ? "SELECT id,kind,posX,posY,posZ,yaw,searched,lifetime,fixtureKey FROM container ORDER BY id;"
                : "SELECT id,kind,posX,posY,posZ,yaw,searched,lifetime FROM container ORDER BY id;"))
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
                        FixtureKey = keys ? st.GetString(8) : null,
                    };
                    byId[id] = c;
                    d.Containers.Add(c);
                }

            bool cExtra = ColumnExists(db, "container_item", "extra");
            using (var st = db.Prepare(cExtra
                ? "SELECT containerId,itemId,count,spawnHours,condition,timesRepaired,extra FROM container_item ORDER BY seq;"
                : "SELECT containerId,itemId,count,spawnHours,condition,timesRepaired FROM container_item ORDER BY seq;"))
                while (st.Step())
                    if (byId.TryGetValue(st.GetInt64(0), out ContainerSave c))
                        c.Items.Add(new StackSave
                        {
                            Id = st.GetString(1),
                            Count = st.GetInt(2),
                            SpawnHours = st.GetFloat(3),
                            Condition = st.GetInt(4),
                            TimesRepaired = st.GetInt(5),
                            Extra = cExtra ? st.GetString(6) : null,
                        });
        }

        /// <summary>
        /// Restores ground items straight into <see cref="GroundItemStore"/>, bypassing
        /// <see cref="SaveData"/> for the same reason the write does.
        ///
        /// Saves written before ground items had a table of their own carry them as sacks in the
        /// container list instead; those are still restored by <c>SaveSystem.ApplyWorld</c>, so old
        /// saves keep working and simply migrate to the new table the next time they are written.
        /// </summary>
        private static void ReadGroundItems(SqliteConnection db)
        {
            // Cleared here rather than in SaveSystem.ApplyWorld, because that runs after this and
            // would otherwise wipe the items this just restored. Doing it before the table check also
            // means loading a save that predates the ground_item table still empties the world first.
            GroundItemStore.Clear();

            if (!TableExists(db, "ground_item")) return;

            var types = new Dictionary<int, ItemDef>(128);
            using (var st = db.Prepare("SELECT idx,itemId FROM ground_type;"))
            {
                while (st.Step())
                {
                    ItemDef def = ItemDatabase.Get(st.GetString(1));
                    if (def != null) types[st.GetInt(0)] = def;
                }
            }

            bool gExtra = ColumnExists(db, "ground_item", "extra");
            using (var st = db.Prepare(gExtra
                ? "SELECT type,x,z,count,spawnHours,condition,timesRepaired,yaw,extra FROM ground_item;"
                : "SELECT type,x,z,count,spawnHours,condition,timesRepaired,yaw FROM ground_item;"))
            {
                while (st.Step())
                {
                    if (!types.TryGetValue(st.GetInt(0), out ItemDef def)) continue;

                    GroundItemStore.Add(def,
                        new Vector3(st.GetFloat(1), 0f, st.GetFloat(2)),
                        st.GetInt(3),
                        st.GetFloat(4),
                        st.GetInt(5),
                        st.GetInt(6),
                        st.GetInt(7) * (360f / 256f),
                        gExtra ? ItemExtra.Parse(st.GetString(8)) : null);
                }
            }
        }

        /// <summary>
        /// Reads back just the items in one chunk. Nothing calls this yet — the load path still
        /// restores the whole world at once — but it is the hook chunk streaming needs, and it is the
        /// reason <c>ground_item</c> is indexed on (cx, cz): pulling one chunk's items costs an index
        /// seek rather than a scan of every item in the save.
        /// </summary>
        public static void ReadGroundChunk(string path, int cx, int cz)
        {
            if (!File.Exists(path)) return;
            using (var db = new SqliteConnection(path))
            {
                if (!TableExists(db, "ground_item")) return;

                var types = new Dictionary<int, ItemDef>(128);
                using (var st = db.Prepare("SELECT idx,itemId FROM ground_type;"))
                {
                    while (st.Step())
                    {
                        ItemDef def = ItemDatabase.Get(st.GetString(1));
                        if (def != null) types[st.GetInt(0)] = def;
                    }
                }

                using (var st = db.Prepare(
                    "SELECT type,x,z,count,spawnHours,condition,timesRepaired,yaw FROM ground_item WHERE cx=? AND cz=?;"))
                {
                    st.Bind(1, cx).Bind(2, cz);
                    while (st.Step())
                    {
                        if (!types.TryGetValue(st.GetInt(0), out ItemDef def)) continue;

                        GroundItemStore.Add(def,
                            new Vector3(st.GetFloat(1), 0f, st.GetFloat(2)),
                            st.GetInt(3),
                            st.GetFloat(4),
                            st.GetInt(5),
                            st.GetInt(6),
                            st.GetInt(7) * (360f / 256f));
                    }
                }
            }
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

        private static void ReadHouse(SqliteConnection db, SaveData d)
        {
            if (!TableExists(db, "house_piece")) return;   // older saves predate house building
            // Saves from before pieces recorded whether the game raised them have no prebuilt column.
            d.HasPrebuiltFlags = ColumnExists(db, "house_piece", "prebuilt");
            using (var st = db.Prepare(d.HasPrebuiltFlags
                ? "SELECT type,posX,posY,posZ,yaw,health,maxHealth,prebuilt FROM house_piece ORDER BY id;"
                : "SELECT type,posX,posY,posZ,yaw,health,maxHealth FROM house_piece ORDER BY id;"))
                while (st.Step())
                    d.House.Add(new HousePieceSave
                    {
                        Type = st.GetInt(0),
                        Position = new Vector3(st.GetFloat(1), st.GetFloat(2), st.GetFloat(3)),
                        Yaw = st.GetFloat(4),
                        Health = st.GetFloat(5),
                        MaxHealth = st.GetFloat(6),
                        Prebuilt = d.HasPrebuiltFlags && st.GetBool(7),
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

        private static void ReadProps(SqliteConnection db, SaveData d)
        {
            if (!TableExists(db, "world_prop")) return;   // pre-furniture-demolish save -> loader rebuilds defaults
            d.HasWorldProps = true;
            using (var st = db.Prepare("SELECT kind,posX,posY,posZ,sizeX,sizeY,sizeZ FROM world_prop ORDER BY id;"))
                while (st.Step())
                    d.Props.Add(new PropSave
                    {
                        Kind = st.GetInt(0),
                        Position = new Vector3(st.GetFloat(1), st.GetFloat(2), st.GetFloat(3)),
                        Size = new Vector3(st.GetFloat(4), st.GetFloat(5), st.GetFloat(6)),
                    });
        }

        private static void ReadFurniture(SqliteConnection db, SaveData d)
        {
            if (!TableExists(db, "home_furniture")) return;   // saved before the home was furnished -> loader furnishes it
            d.HasFurniture = true;
            using (var st = db.Prepare("SELECT kind,posX,posY,posZ,yaw,seed FROM home_furniture ORDER BY id;"))
                while (st.Step())
                    d.Furniture.Add(new FurnitureSave
                    {
                        Kind = st.GetInt(0),
                        Position = new Vector3(st.GetFloat(1), st.GetFloat(2), st.GetFloat(3)),
                        Yaw = st.GetFloat(4),
                        Seed = st.GetInt(5),
                    });
        }

        private static void ReadFixtures(SqliteConnection db, SaveData d)
        {
            if (!TableExists(db, "home_fixture")) return;   // saved before the fixtures were interactive -> loader lays them out
            d.HasFixtures = true;
            using (var st = db.Prepare("SELECT key,state,a,b,text FROM home_fixture ORDER BY id;"))
                while (st.Step())
                    d.Fixtures.Add(new FixtureSave
                    {
                        Key = st.GetString(0),
                        State = st.GetInt(1),
                        A = st.GetFloat(2),
                        B = st.GetFloat(3),
                        Text = st.GetString(4),
                    });
        }
        #endregion
    }
}
