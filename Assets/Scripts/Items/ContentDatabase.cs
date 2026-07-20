using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using CloneZ.Db;
using LootTable = CloneZ.ItemDatabase.LootTable;
using LootEntry = CloneZ.ItemDatabase.LootEntry;

namespace CloneZ
{
    /// <summary>
    /// The relational store for the game's <b>static content</b> — the item catalogue,
    /// the per-container loot tables and the farming crops. Where <see cref="SqliteSaveStore"/>
    /// persists a live run, this persists the reference data the game is built from.
    ///
    /// The database is the source of truth. The C# catalogues in <see cref="ItemDatabase"/> and
    /// <see cref="FarmCrop"/> are only a <i>bootstrap seed</i>: they are written to
    /// <c>clonez_content.db</c> just once, when no usable database exists yet (a first run, a
    /// corrupt file, or an incompatible <see cref="SchemaVersion"/>). Every launch then loads the
    /// content back out of that database and never overwrites it, so edits made in any SQLite
    /// browser persist and win over the built-in catalogue. If the database can't be opened at all
    /// the seed is used directly, so content is never lost.
    /// </summary>
    public static class ContentDatabase
    {
        /// <summary>The structural (schema) version. Bump this ONLY when the table layout changes —
        /// a mismatch triggers a one-time rebuild from the seed. Editing item <i>values</i> needs no
        /// bump; those live in the database, which is the source of truth.</summary>
        public const int SchemaVersion = 1;

        private const string FileName = "clonez_content.db";
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        // The full column list for the item table, shared by the insert and the select so their
        // ordering can never drift apart. 59 columns; the placeholder count is derived from it.
        private const string ItemCols =
            "id,displayName,category,weight,maxStack,tintR,tintG,tintB," +
            "hungerRestore,thirstRestore,healthRestore,infectionReduce," +
            "perishable,shelfLifeHours,cookable,cookedId,isCookedFood,rawHazard," +
            "isSeed,cropId," +
            "isWaterContainer,holdsWater,waterTainted,emptyId,fillCleanId,fillTaintedId,purifiedId," +
            "stopsBleeding,sterileBandage,disinfects,curesWoundInfection,painRelief,calmsPanic," +
            "isClothing,clothingSlot,insulation,biteDefense,scratchDefense,waterResistance," +
            "isWeapon,weaponDamage,weaponRange,weaponArc,swingCooldown,knockback,weaponSkill," +
            "isFirearm,firearmDamage,firearmRange,pelletsPerShot,magazineSize,fireCooldown,reloadTime,firearmSpread,ammoId,firearmNoise," +
            "maxCondition,conditionLossChance,description";

        private const string DropSql =
            "DROP TABLE IF EXISTS loot_entry;" +
            "DROP TABLE IF EXISTS loot_table;" +
            "DROP TABLE IF EXISTS crop;" +
            "DROP TABLE IF EXISTS item;" +
            "DROP TABLE IF EXISTS content_meta;";

        private const string SchemaSql =
            "CREATE TABLE content_meta (id INTEGER PRIMARY KEY CHECK (id = 0), schemaVersion INTEGER, generatedAtUtc TEXT);" +

            "CREATE TABLE item (" +
            "  id TEXT PRIMARY KEY," +
            "  displayName TEXT, category INTEGER, weight REAL, maxStack INTEGER," +
            "  tintR REAL, tintG REAL, tintB REAL," +
            "  hungerRestore REAL, thirstRestore REAL, healthRestore REAL, infectionReduce REAL," +
            "  perishable INTEGER, shelfLifeHours REAL, cookable INTEGER, cookedId TEXT, isCookedFood INTEGER, rawHazard INTEGER," +
            "  isSeed INTEGER, cropId TEXT," +
            "  isWaterContainer INTEGER, holdsWater INTEGER, waterTainted INTEGER, emptyId TEXT, fillCleanId TEXT, fillTaintedId TEXT, purifiedId TEXT," +
            "  stopsBleeding INTEGER, sterileBandage INTEGER, disinfects INTEGER, curesWoundInfection INTEGER, painRelief REAL, calmsPanic INTEGER," +
            "  isClothing INTEGER, clothingSlot INTEGER, insulation REAL, biteDefense REAL, scratchDefense REAL, waterResistance REAL," +
            "  isWeapon INTEGER, weaponDamage REAL, weaponRange REAL, weaponArc REAL, swingCooldown REAL, knockback REAL, weaponSkill INTEGER," +
            "  isFirearm INTEGER, firearmDamage REAL, firearmRange REAL, pelletsPerShot INTEGER, magazineSize INTEGER," +
            "  fireCooldown REAL, reloadTime REAL, firearmSpread REAL, ammoId TEXT, firearmNoise REAL," +
            "  maxCondition INTEGER, conditionLossChance INTEGER, description TEXT);" +

            "CREATE TABLE loot_table (type INTEGER PRIMARY KEY, minRolls INTEGER, maxRolls INTEGER, emptyWeight INTEGER);" +

            "CREATE TABLE loot_entry (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  tableType INTEGER, itemId TEXT, weight INTEGER, minCount INTEGER, maxCount INTEGER," +
            "  FOREIGN KEY (tableType) REFERENCES loot_table(type));" +

            "CREATE TABLE crop (" +
            "  id TEXT PRIMARY KEY," +
            "  name TEXT, seedId TEXT, produceId TEXT, growHours REAL, yieldMin INTEGER, yieldMax INTEGER," +
            "  ripeR REAL, ripeG REAL, ripeB REAL);";

        private static string s_ItemInsertSql;

        /// <summary>
        /// True when a structurally usable content database already exists — the file is present, it
        /// carries the expected tables, its <see cref="SchemaVersion"/> matches this build, and it
        /// holds at least one item. When true the database is loaded as-is (it is the source of
        /// truth); the built-in seed is written only when this is false — a first run, a corrupt
        /// file, or an incompatible schema.
        /// </summary>
        public static bool HasUsableContent(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                using (var db = new SqliteConnection(path))
                {
                    if (!TableExists(db, "content_meta") || !TableExists(db, "item")) return false;

                    using (var st = db.Prepare("SELECT schemaVersion FROM content_meta WHERE id=0;"))
                        if (!st.Step() || st.GetInt(0) != SchemaVersion) return false;

                    using (var st = db.Prepare("SELECT COUNT(*) FROM item;"))
                        return st.Step() && st.GetInt(0) > 0;
                }
            }
            catch { return false; }
        }

        #region Write (seed)
        /// <summary>Writes the seed catalogue to disk as a single atomic transaction.</summary>
        public static void Write(string path, List<ItemDef> items, Dictionary<ContainerType, LootTable> tables, List<FarmCrop> crops)
        {
            using (var db = new SqliteConnection(path))
            {
                db.Execute("BEGIN;");
                try
                {
                    db.Execute(DropSql);
                    db.Execute(SchemaSql);
                    WriteMeta(db);
                    WriteItems(db, items);
                    WriteTables(db, tables);
                    WriteCrops(db, crops);
                    db.Execute("COMMIT;");
                }
                catch
                {
                    try { db.Execute("ROLLBACK;"); } catch { /* connection is being torn down anyway */ }
                    throw;
                }
            }
            Debug.Log("[CloneZ] Seeded content database \u2192 " + path);
        }

        private static void WriteMeta(SqliteConnection db)
        {
            using (var st = db.Prepare("INSERT INTO content_meta (id,schemaVersion,generatedAtUtc) VALUES (0,?,?);"))
            {
                st.Bind(1, SchemaVersion).Bind(2, System.DateTime.UtcNow.ToString("o"));
                st.Step();
            }
        }

        private static void WriteItems(SqliteConnection db, List<ItemDef> items)
        {
            if (items == null) return;
            using (var st = db.Prepare(ItemInsertSql()))
                foreach (ItemDef d in items)
                {
                    st.Reset();
                    st.Bind(1, d.Id).Bind(2, d.DisplayName).Bind(3, (int)d.Category).Bind(4, d.Weight).Bind(5, d.MaxStack)
                      .Bind(6, d.Tint.r).Bind(7, d.Tint.g).Bind(8, d.Tint.b)
                      .Bind(9, d.HungerRestore).Bind(10, d.ThirstRestore).Bind(11, d.HealthRestore).Bind(12, d.InfectionReduce)
                      .Bind(13, d.Perishable).Bind(14, d.ShelfLifeHours).Bind(15, d.Cookable).Bind(16, d.CookedId).Bind(17, d.IsCookedFood).Bind(18, d.RawHazard)
                      .Bind(19, d.IsSeed).Bind(20, d.CropId)
                      .Bind(21, d.IsWaterContainer).Bind(22, d.HoldsWater).Bind(23, d.WaterTainted).Bind(24, d.EmptyId).Bind(25, d.FillCleanId).Bind(26, d.FillTaintedId).Bind(27, d.PurifiedId)
                      .Bind(28, d.StopsBleeding).Bind(29, d.SterileBandage).Bind(30, d.Disinfects).Bind(31, d.CuresWoundInfection).Bind(32, d.PainRelief).Bind(33, d.CalmsPanic)
                      .Bind(34, d.IsClothing).Bind(35, (int)d.ClothingSlot).Bind(36, d.Insulation).Bind(37, d.BiteDefense).Bind(38, d.ScratchDefense).Bind(39, d.WaterResistance)
                      .Bind(40, d.IsWeapon).Bind(41, d.WeaponDamage).Bind(42, d.WeaponRange).Bind(43, d.WeaponArc).Bind(44, d.SwingCooldown).Bind(45, d.Knockback).Bind(46, (int)d.WeaponSkill)
                      .Bind(47, d.IsFirearm).Bind(48, d.FirearmDamage).Bind(49, d.FirearmRange).Bind(50, d.PelletsPerShot).Bind(51, d.MagazineSize).Bind(52, d.FireCooldown).Bind(53, d.ReloadTime).Bind(54, d.FirearmSpread).Bind(55, d.AmmoId).Bind(56, d.FirearmNoise)
                      .Bind(57, d.MaxCondition).Bind(58, d.ConditionLossChance).Bind(59, d.Description);
                    st.Step();
                }
        }

        private static void WriteTables(SqliteConnection db, Dictionary<ContainerType, LootTable> tables)
        {
            if (tables == null) return;
            using (var tst = db.Prepare("INSERT INTO loot_table (type,minRolls,maxRolls,emptyWeight) VALUES (?,?,?,?);"))
            using (var est = db.Prepare("INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES (?,?,?,?,?);"))
                foreach (KeyValuePair<ContainerType, LootTable> kv in tables)
                {
                    int type = (int)kv.Key;
                    LootTable t = kv.Value;
                    tst.Reset();
                    tst.Bind(1, type).Bind(2, t.MinRolls).Bind(3, t.MaxRolls).Bind(4, t.EmptyWeight);
                    tst.Step();

                    if (t.Entries == null) continue;
                    foreach (LootEntry e in t.Entries)
                    {
                        est.Reset();
                        est.Bind(1, type).Bind(2, e.Id).Bind(3, e.Weight).Bind(4, e.Min).Bind(5, e.Max);
                        est.Step();
                    }
                }
        }

        private static void WriteCrops(SqliteConnection db, List<FarmCrop> crops)
        {
            if (crops == null) return;
            using (var st = db.Prepare(
                "INSERT INTO crop (id,name,seedId,produceId,growHours,yieldMin,yieldMax,ripeR,ripeG,ripeB)" +
                " VALUES (?,?,?,?,?,?,?,?,?,?);"))
                foreach (FarmCrop c in crops)
                {
                    st.Reset();
                    st.Bind(1, c.Id).Bind(2, c.Name).Bind(3, c.SeedId).Bind(4, c.ProduceId).Bind(5, c.GrowHours)
                      .Bind(6, c.YieldMin).Bind(7, c.YieldMax).Bind(8, c.RipeColor.r).Bind(9, c.RipeColor.g).Bind(10, c.RipeColor.b);
                    st.Step();
                }
        }
        #endregion

        #region Read
        /// <summary>Loads the full content catalogue from the database.</summary>
        public static void Read(string path,
            out Dictionary<string, ItemDef> items,
            out Dictionary<ContainerType, LootTable> tables,
            out List<FarmCrop> crops)
        {
            items = new Dictionary<string, ItemDef>();
            tables = new Dictionary<ContainerType, LootTable>();
            crops = new List<FarmCrop>();
            using (var db = new SqliteConnection(path))
            {
                ReadItems(db, items);
                ReadTables(db, tables);
                ReadCrops(db, crops);
            }
        }

        private static void ReadItems(SqliteConnection db, Dictionary<string, ItemDef> items)
        {
            using (var st = db.Prepare("SELECT " + ItemCols + " FROM item;"))
                while (st.Step())
                {
                    var d = new ItemDef
                    {
                        Id = st.GetString(0),
                        DisplayName = st.GetString(1),
                        Category = (ItemCategory)st.GetInt(2),
                        Weight = st.GetFloat(3),
                        MaxStack = st.GetInt(4),
                        Tint = new Color(st.GetFloat(5), st.GetFloat(6), st.GetFloat(7)),
                        HungerRestore = st.GetFloat(8),
                        ThirstRestore = st.GetFloat(9),
                        HealthRestore = st.GetFloat(10),
                        InfectionReduce = st.GetFloat(11),
                        Perishable = st.GetBool(12),
                        ShelfLifeHours = st.GetFloat(13),
                        Cookable = st.GetBool(14),
                        CookedId = st.GetString(15),
                        IsCookedFood = st.GetBool(16),
                        RawHazard = st.GetBool(17),
                        IsSeed = st.GetBool(18),
                        CropId = st.GetString(19),
                        IsWaterContainer = st.GetBool(20),
                        HoldsWater = st.GetBool(21),
                        WaterTainted = st.GetBool(22),
                        EmptyId = st.GetString(23),
                        FillCleanId = st.GetString(24),
                        FillTaintedId = st.GetString(25),
                        PurifiedId = st.GetString(26),
                        StopsBleeding = st.GetBool(27),
                        SterileBandage = st.GetBool(28),
                        Disinfects = st.GetBool(29),
                        CuresWoundInfection = st.GetBool(30),
                        PainRelief = st.GetFloat(31),
                        CalmsPanic = st.GetBool(32),
                        IsClothing = st.GetBool(33),
                        ClothingSlot = (ClothingSlot)st.GetInt(34),
                        Insulation = st.GetFloat(35),
                        BiteDefense = st.GetFloat(36),
                        ScratchDefense = st.GetFloat(37),
                        WaterResistance = st.GetFloat(38),
                        IsWeapon = st.GetBool(39),
                        WeaponDamage = st.GetFloat(40),
                        WeaponRange = st.GetFloat(41),
                        WeaponArc = st.GetFloat(42),
                        SwingCooldown = st.GetFloat(43),
                        Knockback = st.GetFloat(44),
                        WeaponSkill = (PlayerSkills.Skill)st.GetInt(45),
                        IsFirearm = st.GetBool(46),
                        FirearmDamage = st.GetFloat(47),
                        FirearmRange = st.GetFloat(48),
                        PelletsPerShot = st.GetInt(49),
                        MagazineSize = st.GetInt(50),
                        FireCooldown = st.GetFloat(51),
                        ReloadTime = st.GetFloat(52),
                        FirearmSpread = st.GetFloat(53),
                        AmmoId = st.GetString(54),
                        FirearmNoise = st.GetFloat(55),
                        MaxCondition = st.GetInt(56),
                        ConditionLossChance = st.GetInt(57),
                        Description = st.GetString(58),
                    };
                    items[d.Id] = d;
                }
        }

        private static void ReadTables(SqliteConnection db, Dictionary<ContainerType, LootTable> tables)
        {
            var entriesByType = new Dictionary<int, List<LootEntry>>();
            using (var st = db.Prepare("SELECT tableType,itemId,weight,minCount,maxCount FROM loot_entry ORDER BY seq;"))
                while (st.Step())
                {
                    int type = st.GetInt(0);
                    if (!entriesByType.TryGetValue(type, out List<LootEntry> list))
                    {
                        list = new List<LootEntry>();
                        entriesByType[type] = list;
                    }
                    list.Add(new LootEntry(st.GetString(1), st.GetInt(2), st.GetInt(3), st.GetInt(4)));
                }

            using (var st = db.Prepare("SELECT type,minRolls,maxRolls,emptyWeight FROM loot_table;"))
                while (st.Step())
                {
                    int type = st.GetInt(0);
                    tables[(ContainerType)type] = new LootTable
                    {
                        MinRolls = st.GetInt(1),
                        MaxRolls = st.GetInt(2),
                        EmptyWeight = st.GetInt(3),
                        Entries = entriesByType.TryGetValue(type, out List<LootEntry> list) ? list.ToArray() : new LootEntry[0],
                    };
                }
        }

        private static void ReadCrops(SqliteConnection db, List<FarmCrop> crops)
        {
            using (var st = db.Prepare(
                "SELECT id,name,seedId,produceId,growHours,yieldMin,yieldMax,ripeR,ripeG,ripeB FROM crop;"))
                while (st.Step())
                    crops.Add(new FarmCrop
                    {
                        Id = st.GetString(0),
                        Name = st.GetString(1),
                        SeedId = st.GetString(2),
                        ProduceId = st.GetString(3),
                        GrowHours = st.GetFloat(4),
                        YieldMin = st.GetInt(5),
                        YieldMax = st.GetInt(6),
                        RipeColor = new Color(st.GetFloat(7), st.GetFloat(8), st.GetFloat(9)),
                    });
        }
        #endregion

        private static bool TableExists(SqliteConnection db, string name)
        {
            using (var st = db.Prepare("SELECT 1 FROM sqlite_master WHERE type='table' AND name=?;"))
            {
                st.Bind(1, name);
                return st.Step();
            }
        }

        /// <summary>Builds the item INSERT once, deriving the placeholder count from <see cref="ItemCols"/>.</summary>
        private static string ItemInsertSql()
        {
            if (s_ItemInsertSql == null)
            {
                int n = ItemCols.Split(',').Length;
                var q = new StringBuilder();
                for (int i = 0; i < n; i++) { if (i > 0) q.Append(','); q.Append('?'); }
                s_ItemInsertSql = "INSERT INTO item (" + ItemCols + ") VALUES (" + q + ");";
            }
            return s_ItemInsertSql;
        }
    }
}
