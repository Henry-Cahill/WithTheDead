using System.Collections.Generic;
using System.Globalization;
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
        public const int SchemaVersion = 6;   // v6: gear columns (containers, devices, refills, grid footprint)

        private const string FileName = "clonez_content.db";
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        // The full column list for the item table, shared by the insert and the select so their
        // ordering can never drift apart. 72 columns; the placeholder count is derived from it.
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
            "maxCondition,conditionLossChance,description," +
            "calories,carbohydrates,proteins,lipids," +
            "isCanned,twoHanded," +
            "gearKind,containerCapacity,gridW,gridH,chargeSeconds,sizeW,sizeH";

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
            "  maxCondition INTEGER, conditionLossChance INTEGER, description TEXT," +
            "  calories REAL, carbohydrates REAL, proteins REAL, lipids REAL," +
            "  isCanned INTEGER, twoHanded INTEGER," +
            "  gearKind INTEGER DEFAULT 0, containerCapacity REAL DEFAULT 0, gridW INTEGER DEFAULT 0, gridH INTEGER DEFAULT 0," +
            "  chargeSeconds REAL DEFAULT 0, sizeW INTEGER DEFAULT 0, sizeH INTEGER DEFAULT 0);" +

            "CREATE TABLE loot_table (type INTEGER PRIMARY KEY, minRolls INTEGER, maxRolls INTEGER, emptyWeight INTEGER);" +

            "CREATE TABLE loot_entry (" +
            "  seq INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  tableType INTEGER, itemId TEXT, weight INTEGER, minCount INTEGER, maxCount INTEGER," +
            "  FOREIGN KEY (tableType) REFERENCES loot_table(type));" +

            "CREATE TABLE crop (" +
            "  id TEXT PRIMARY KEY," +
            "  name TEXT, seedId TEXT, produceId TEXT, growHours REAL, yieldMin INTEGER, yieldMax INTEGER," +
            "  ripeR REAL, ripeG REAL, ripeB REAL);";

        // The curated loot tables, authored directly as INSERT statements — one source of truth for every
        // hand-placed drop. Weights are final (PZ B42 relative rarity already folded in). The tableType
        // column is (int)ContainerType: 0 Crate, 1 Locker, 2 Fridge, 3 MedCabinet, 4 Desk, 5 ToolBench,
        // 6 Wardrobe, 7 Pantry, 8 GunLocker, 9 Shelf, 10 Corpse.
        private const string LootSeedSql = @"
INSERT INTO loot_table (type,minRolls,maxRolls,emptyWeight) VALUES
  (0,2,4,6),(1,2,4,8),(2,2,4,4),(3,1,3,5),(4,1,3,9),(5,2,4,5),
  (6,2,4,6),(7,2,4,5),(8,1,3,7),(9,2,4,6),(10,1,2,34);

-- Crate
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (0,'cannedbeans',3,1,2),(0,'cannedsoup',3,1,2),(0,'crackers',7,1,3),
  (0,'plank',7,1,2),(0,'nails',3,1,2),(0,'ducttape',3,1,1),
  (0,'rope',7,1,1),(0,'baseballbat',2,1,1),(0,'crowbar',2,1,1),
  (0,'shotgunshells',3,2,6),(0,'ammo9mm',3,4,10),
  (0,'battery',5,1,2),(0,'raincoat',3,1,1),(0,'trousers',1,1,1),
  (0,'emptybottle',5,1,2),
  (0,'potatoseeds',4,1,3),(0,'cabbageseeds',3,1,2),(0,'tomatoseeds',3,1,2),
  (0,'trowel',4,1,1),
  (0,'leathergloves',4,1,1),(0,'leatherjacket',5,1,1),(0,'canopener',2,1,1);

-- Locker
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (1,'rag',7,1,3),(1,'bandage',5,1,2),(1,'chips',5,1,2),
  (1,'soda',5,1,2),(1,'kitchenknife',3,1,1),(1,'baseballbat',2,1,1),
  (1,'pistol',1,1,1),(1,'ammo9mm',3,4,10),
  (1,'pills',7,1,1),(1,'battery',5,1,1),
  (1,'tshirt',7,1,1),(1,'hoodie',5,1,1),(1,'jeans',1,1,1),
  (1,'emptybottle',5,1,2),
  (1,'sneakers',4,1,1),(1,'beanie',4,1,1),(1,'cap',3,1,1),
  (1,'jacket',1,1,1),(1,'gloves',4,1,1),(1,'boots',7,1,1),
  (1,'thread',3,1,2);

-- Fridge
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (2,'waterbottle',9,1,2),(2,'soda',5,1,3),(2,'juicebox',3,1,2),
  (2,'emptybottle',5,1,2),
  (2,'cannedsoup',3,1,2),(2,'apple',5,1,3),(2,'breadloaf',5,1,1),
  (2,'cerealbox',7,1,1),(2,'cannedbeans',3,1,2),
  (2,'rawmeat',7,1,2),(2,'chicken',5,1,1),(2,'potato',7,1,3),
  (2,'cabbage',5,1,1),(2,'tomato',5,1,2),(2,'carrot',5,1,2);

-- MedCabinet
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (3,'bandage',5,1,3),(3,'pills',7,1,2),(3,'antibiotics',3,1,1),
  (3,'disinfectant',5,1,2),(3,'firstaid',3,1,1),(3,'rag',7,1,2),
  (3,'betablockers',4,1,2);

-- Desk
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (4,'crackers',7,1,2),(4,'chips',5,1,2),(4,'pills',7,1,1),
  (4,'bandage',5,1,1),(4,'battery',5,1,2),(4,'ducttape',3,1,1),
  (4,'ammo9mm',3,3,8),(4,'kitchenknife',2,1,1),
  (4,'betablockers',2,1,1),(4,'canopener',2,1,1),
  (4,'needle',3,1,1),(4,'thread',3,1,2);

-- ToolBench
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (5,'nails',3,1,3),(5,'plank',7,1,2),(5,'ducttape',3,1,2),
  (5,'hammer',5,1,1),(5,'crowbar',4,1,1),(5,'fireaxe',2,1,1),
  (5,'shotgun',1,1,1),(5,'shotgunshells',3,2,6),
  (5,'fryingpan',3,1,1),(5,'rope',7,1,1),(5,'battery',5,1,2),
  (5,'trowel',4,1,1),(5,'wateringcan',3,1,1),
  (5,'potatoseeds',4,1,3),(5,'carrotseeds',3,1,2),(5,'canopener',3,1,1);

-- Wardrobe
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (6,'tshirt',7,1,1),(6,'jeans',1,1,1),(6,'hoodie',5,1,1),
  (6,'jacket',1,1,1),(6,'boots',7,1,1),(6,'beanie',4,1,1),
  (6,'needle',4,1,1),(6,'thread',4,1,2);

-- Pantry
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (7,'cannedbeans',3,1,2),(7,'cannedsoup',3,1,2),(7,'crackers',7,1,2),
  (7,'cerealbox',7,1,1),(7,'chips',5,1,1),(7,'canopener',3,1,1);

-- GunLocker
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (8,'pistol',3,1,1),(8,'shotgun',2,1,1),(8,'ammo9mm',5,4,12),
  (8,'shotgunshells',4,2,8);

-- Shelf
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (9,'nails',3,1,3),(9,'plank',7,1,2),(9,'ducttape',3,1,1),
  (9,'rope',7,1,1),(9,'battery',5,1,2);

-- Corpse
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (10,'chips',5,1,1),(10,'crackers',7,1,1),(10,'soda',5,1,1),
  (10,'bandage',5,1,1),(10,'pills',7,1,1),(10,'battery',5,1,1),
  (10,'ammo9mm',4,2,6),(10,'shotgunshells',2,1,3),
  (10,'kitchenknife',2,1,1),(10,'rag',7,1,2),
  (10,'tshirt',7,1,1),(10,'jeans',1,1,1),(10,'cap',3,1,1),
  (10,'sneakers',4,1,1),(10,'gloves',4,1,1),(10,'boots',7,1,1);

-- Gear (v6): worn containers, night vision, gas masks + filters, flashlights and winter clothing.
-- Night vision and military packs stay rare (lockers / gun lockers); torches and filters are common.
INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES
  (0,'flashlight',3,1,1),(0,'gasmaskfilter',2,1,1),(0,'fannypack',2,1,1),(0,'schoolbag',2,1,1),
  (1,'flashlight',3,1,1),(1,'chestrig',2,1,1),(1,'thighbag',2,1,1),(1,'nvg',1,1,1),
  (1,'gasmask',2,1,1),(1,'gasmaskfilter',3,1,2),(1,'camojacket',2,1,1),(1,'cargopants',3,1,1),
  (1,'parka',2,1,1),(1,'greenbeanie',3,1,1),(1,'hikingbag',2,1,1),(1,'schoolbag',2,1,1),
  (3,'gasmaskfilter',2,1,1),
  (4,'flashlight',3,1,1),
  (5,'flashlight',3,1,1),(5,'gasmask',2,1,1),(5,'gasmaskfilter',3,1,2),
  (6,'parka',3,1,1),(6,'camojacket',2,1,1),(6,'cargopants',3,1,1),(6,'greenbeanie',4,1,1),
  (6,'schoolbag',3,1,1),(6,'hikingbag',2,1,1),(6,'fannypack',3,1,1),
  (8,'nvg',2,1,1),(8,'chestrig',3,1,1),(8,'thighbag',2,1,1),(8,'militarypack',2,1,1),
  (8,'gasmask',2,1,1),(8,'gasmaskfilter',3,1,2),(8,'camojacket',2,1,1),
  (9,'flashlight',2,1,1),(9,'gasmaskfilter',1,1,1),
  (10,'flashlight',2,1,1),(10,'greenbeanie',2,1,1),(10,'schoolbag',1,1,1),(10,'chestrig',1,1,1),
  (10,'camojacket',1,1,1),(10,'gasmaskfilter',1,1,1);
";

        // Once the imported catalogue is spread across the tables, keep the big ones a little sparse:
        // raise each table's empty-draw weight to at least 12% of its total entry weight.
        private const string LootRebalanceSql =
            "UPDATE loot_table SET emptyWeight = max(emptyWeight, " +
            "CAST(ROUND(COALESCE((SELECT SUM(weight) FROM loot_entry WHERE tableType = loot_table.type), 0) * 0.12) AS INTEGER));";

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
                    if (!TableExists(db, "content_meta") || !TableExists(db, "item") || !TableExists(db, "loot_table") || !TableExists(db, "loot_entry") || !TableExists(db, "crop")) return false;

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
        public static void Write(string path, string curatedItemsSql, string bulkItemsSql, List<FarmCrop> crops)
        {
            using (var db = new SqliteConnection(path))
            {
                db.Execute("BEGIN;");
                try
                {
                    db.Execute(DropSql);
                    db.Execute(SchemaSql);
                    WriteMeta(db);
                    HashSet<string> curatedIds = WriteItems(db, curatedItemsSql, bulkItemsSql);
                    WriteLoot(db, curatedIds);
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
            // A single literal INSERT (schema version inlined; timestamp quoted).
            db.Execute("INSERT INTO content_meta (id,schemaVersion,generatedAtUtc) VALUES (0, "
                + SchemaVersion + ", " + Q(System.DateTime.UtcNow.ToString("o")) + ");");
        }

        /// <summary>Seeds the item table from two INSERT scripts — the curated catalogue first, then the
        /// bulk PZ catalogue (INSERT OR IGNORE, so a curated row always wins an id clash). Returns the
        /// curated id set (every row present after the curated script) so loot distribution can skip them.</summary>
        private static HashSet<string> WriteItems(SqliteConnection db, string curatedItemsSql, string bulkItemsSql)
        {
            if (!string.IsNullOrEmpty(curatedItemsSql)) db.Execute(curatedItemsSql);   // Assets/Resources/curated_items.txt

            var curated = new HashSet<string>();
            using (var q = db.Prepare("SELECT id FROM item;"))
                while (q.Step()) curated.Add(q.GetString(0));

            if (!string.IsNullOrEmpty(bulkItemsSql))
            {
                db.Execute(bulkItemsSql);                                                   // Assets/Resources/pz_items.txt
                db.Execute(GearPatchSql);
            }
            return curated;
        }

        // The bulk PZ catalogue carries its own batteries, torches, filters and gas masks; make them work
        // like the curated gear (one charge per instance, so devices don't stack) instead of being dead loot.
        private const string GearPatchSql =
            "UPDATE item SET gearKind=5 WHERE id IN ('Battery');" +
            "UPDATE item SET gearKind=6 WHERE id IN ('GasmaskFilter','GasmaskFilterCrafted');" +
            "UPDATE item SET gearKind=3, maxStack=1, maxCondition=100, conditionLossChance=50, chargeSeconds=1200, sizeW=2, sizeH=1" +
            " WHERE id IN ('HandTorch','Torch','FlashLight_AngleHead','FlashLight_AngleHead_Army','Flashlight_Crafted');" +
            "UPDATE item SET gearKind=4, isClothing=1, clothingSlot=9, maxStack=1, maxCondition=100, conditionLossChance=50, chargeSeconds=600" +
            " WHERE id IN ('Hat_GasMask','Hat_NBCmask','Hat_ImprovisedGasMask','Hat_BuildersRespirator');";

        // SQL literal formatters for the generated crop / meta / loot INSERTs. Strings are single-quoted
        // with embedded quotes doubled; numbers use the invariant culture (a decimal point, never a comma).
        private static string Q(string s) => s == null ? "NULL" : "'" + s.Replace("'", "''") + "'";
        private static string Q(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string Q(float v) => v.ToString(CultureInfo.InvariantCulture);

        // Seeds the loot tables entirely as INSERT statements: the curated tables/entries from the literal
        // LootSeedSql, then the imported catalogue spread across them by category, then a rebalance of the
        // empty-draw weights. One source of truth for every drop.
        private static void WriteLoot(SqliteConnection db, HashSet<string> curatedIds)
        {
            db.Execute(LootSeedSql);
            DistributeCatalogueLoot(db, curatedIds);
            db.Execute(LootRebalanceSql);
        }

        /// <summary>Spreads every imported (non-curated) item across the tables that suit its category, as
        /// literal loot_entry INSERTs (read the item rows from the DB, then execute one script). Curated
        /// items keep the hand-authored placement in <see cref="LootSeedSql"/>.</summary>
        private static void DistributeCatalogueLoot(SqliteConnection db, HashSet<string> curatedIds)
        {
            // Build a batch of INSERT statements to avoid many DB executions.
            var sb = new StringBuilder();
            // Iterate all items to decide where they belong.
            using (var q = db.Prepare("SELECT id, category, perishable FROM item;"))
                while (q.Step())
                {
                    string id = q.GetString(0);
                    // Skip items that were already placed by curated loot.
                    if (curatedIds != null && curatedIds.Contains(id)) continue;   // hand-placed already
                    // Determine if food is perishable for special handling (fridge vs pantry).
                    bool perishable = q.GetBool(2);
                    // Map item category to appropriate container types and loot weights.
                    switch ((ItemCategory)q.GetInt(1))
                    {
                        case ItemCategory.Food:
                            if (perishable) { LootRow(sb, ContainerType.Fridge, id, 3, 1, 2); LootRow(sb, ContainerType.Pantry, id, 1, 1, 1); }
                            else { LootRow(sb, ContainerType.Pantry, id, 3, 1, 2); LootRow(sb, ContainerType.Crate, id, 1, 1, 1); LootRow(sb, ContainerType.Fridge, id, 1, 1, 1); }
                            break;
                        case ItemCategory.Drink:
                            LootRow(sb, ContainerType.Fridge, id, 3, 1, 2); LootRow(sb, ContainerType.Pantry, id, 2, 1, 1);
                            break;
                        case ItemCategory.Clothing:
                            LootRow(sb, ContainerType.Wardrobe, id, 4, 1, 1); LootRow(sb, ContainerType.Locker, id, 2, 1, 1); LootRow(sb, ContainerType.Corpse, id, 1, 1, 1);
                            break;
                        case ItemCategory.Weapon:
                            LootRow(sb, ContainerType.Crate, id, 1, 1, 1); LootRow(sb, ContainerType.GunLocker, id, 1, 1, 1); LootRow(sb, ContainerType.ToolBench, id, 1, 1, 1);
                            break;
                        case ItemCategory.Firearm:
                            LootRow(sb, ContainerType.GunLocker, id, 3, 1, 1); LootRow(sb, ContainerType.Locker, id, 1, 1, 1);
                            break;
                        case ItemCategory.Ammo:
                            LootRow(sb, ContainerType.GunLocker, id, 3, 2, 8); LootRow(sb, ContainerType.Crate, id, 1, 2, 6);
                            break;
                        case ItemCategory.Medical:
                            LootRow(sb, ContainerType.MedCabinet, id, 4, 1, 2); LootRow(sb, ContainerType.Desk, id, 1, 1, 1);
                            break;
                        case ItemCategory.Material:
                            LootRow(sb, ContainerType.Shelf, id, 3, 1, 2); LootRow(sb, ContainerType.Crate, id, 2, 1, 2); LootRow(sb, ContainerType.ToolBench, id, 2, 1, 2);
                            break;
                        default:
                            LootRow(sb, ContainerType.Desk, id, 2, 1, 1); LootRow(sb, ContainerType.Shelf, id, 1, 1, 1);
                            break;
                    }
                }
            // Execute the accumulated INSERTs if any rows were added.
            if (sb.Length > 0) db.Execute(sb.ToString());
        }

        /// <summary>Appends one literal loot_entry INSERT (same style as every other seed write).</summary>
        private static void LootRow(StringBuilder sb, ContainerType type, string id, int weight, int min, int max)
        {
            sb.Append("INSERT INTO loot_entry (tableType,itemId,weight,minCount,maxCount) VALUES (")
              .Append((int)type).Append(',').Append(Q(id)).Append(',').Append(weight).Append(',')
              .Append(min).Append(',').Append(max).Append(");\n");
        }

        private static void WriteCrops(SqliteConnection db, List<FarmCrop> crops)
        {
            if (crops == null || crops.Count == 0) return;
            var sb = new StringBuilder("INSERT INTO crop (id,name,seedId,produceId,growHours,yieldMin,yieldMax,ripeR,ripeG,ripeB) VALUES\n");
            for (int i = 0; i < crops.Count; i++)
            {
                FarmCrop c = crops[i];
                if (i > 0) sb.Append(",\n");
                sb.Append('(').Append(Q(c.Id)).Append(',').Append(Q(c.Name)).Append(',').Append(Q(c.SeedId)).Append(',').Append(Q(c.ProduceId)).Append(',')
                  .Append(Q(c.GrowHours)).Append(',').Append(Q(c.YieldMin)).Append(',').Append(Q(c.YieldMax)).Append(',')
                  .Append(Q(c.RipeColor.r)).Append(',').Append(Q(c.RipeColor.g)).Append(',').Append(Q(c.RipeColor.b)).Append(')');
            }
            sb.Append(';');
            db.Execute(sb.ToString());
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

        /// <summary>Loads the full content catalogue from the database.</summary>
        private static void ReadItems(SqliteConnection db, Dictionary<string, ItemDef> items)
        {
            // Query all columns from item table in the order defined by ItemCols.
            using (var st = db.Prepare("SELECT " + ItemCols + " FROM item;"))
                // Iterate each row and construct ItemDef objects.
                // Group entries by their container type.
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
                        Calories = st.GetFloat(59),
                        Carbohydrates = st.GetFloat(60),
                        Proteins = st.GetFloat(61),
                        Lipids = st.GetFloat(62),
                        IsCanned = st.GetBool(63),
                        TwoHanded = st.GetBool(64),
                        Gear = (GearKind)st.GetInt(65),
                        ContainerCapacity = st.GetFloat(66),
                        GridW = st.GetInt(67),
                        GridH = st.GetInt(68),
                        ChargeSeconds = st.GetFloat(69),
                        SizeW = st.GetInt(70),
                        SizeH = st.GetInt(71),
                    };
                    // Store the definition keyed by its id for fast lookup.
                    items[d.Id] = d;
                }
        }

        /// <summary>Loads the loot tables and their entries from the database.</summary>
        private static void ReadTables(SqliteConnection db, Dictionary<ContainerType, LootTable> tables)
        {
            // Temporary map from table type id to list of loot entries.
            var entriesByType = new Dictionary<int, List<LootEntry>>();
            // Load all loot_entry rows ordered by seq to preserve defined order.
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

            // Load loot_table metadata and combine with collected entries.
            using (var st = db.Prepare("SELECT type,minRolls,maxRolls,emptyWeight FROM loot_table;"))
                while (st.Step())
                {
                    int type = st.GetInt(0);
                    // Use empty list if no entries exist for this type.
                    tables[(ContainerType)type] = new LootTable
                    {
                        MinRolls = st.GetInt(1),
                        MaxRolls = st.GetInt(2),
                        EmptyWeight = st.GetInt(3),
                        Entries = entriesByType.TryGetValue(type, out List<LootEntry> list) ? list.ToArray() : new LootEntry[0],
                    };
                }
        }

        /// <summary>Loads the farming crop definitions from the database.</summary>
        private static void ReadCrops(SqliteConnection db, List<FarmCrop> crops)
        {
            // Query all crop definition fields.
            using (var st = db.Prepare(
                "SELECT id,name,seedId,produceId,growHours,yieldMin,yieldMax,ripeR,ripeG,ripeB FROM crop;"))
                // Iterate each crop row and add to the output list.
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
    }
}
