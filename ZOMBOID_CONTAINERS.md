# CLONE-Z — Zomboid Container List

A verbose reference of **every Project Zomboid Build 42 container type** — world furniture and appliances, bags/clothing containers, vehicle parts, corpse inventories and the floor — extracted from the real game files in `ref\` (the git-ignored PZ install) and mapped to CLONE-Z's container types ([Assets/Scripts/Items/LootContainer.cs](Assets/Scripts/Items/LootContainer.cs)). The catalogue uses the same tab-separated, id-first layout as the Zomboid item list (`ref\pz_items_tsv_backup.txt`), so `Bag_Schoolbag<TAB>Small Backpack`-style rows paste straight into other tools.

- **PZ data:** Build **42.19** (`Core.gameVersion = new GameVersion(42, 19, "")` in `ref\projectzomboid.jar`; `ref\SVNRevision.txt` = 964); the distribution files are dated April–May 2026 and the jar June 2026. Extracted 2026-10-16.
- **Coverage:** 547 rows — furniture 26, appliance 20, storage 25, bag 319, vehicle 12, body 128, floor 1, other 16.
- **Sources (all read-only under `ref\`):**
  - `media\lua\server\Items\Distributions.lua` — 367 room definitions (+35 merge aliases from `SuburbsDistributions.lua`, e.g. `clinic`→`medical`), each mapping container keys to procedural lists or direct `rolls`/`items`/`junk` tables; the generic `all` table; 263 top-level bag/item-container tables.
  - `media\lua\server\Items\ProceduralDistributions.lua` — 1,400 procedural lists; `Distribution_*.lua` — `ClutterTables` junk tables and `BagsAndContainers`.
  - `media\lua\server\Vehicles\VehicleDistributions.lua` + `VehicleDistribution_*Junk.lua` — 218 vehicle distribution keys (Normal + Specific sets).
  - `media\lua\shared\Translate\EN\IG_UI.json` (`IGUI_ContainerTitle_*`, `IGUI_VehiclePart*`) and `ItemName.json` — display names; `media\lua\shared\Definitions\ContainerButtonIcons.lua` — button icons; `media\lua\shared\StashDescriptions\*.lua` — stash/cache container forms.
  - `media\*.tiles` — the binary tile definitions in engine load order (`newtiledefinitions`, `erosion`, `overlays`, `b42chunkcaching`, then the `noiseworks.patch`): 60,278 sprites, 63 distinct `container` values, `ContainerCapacity`, `Freezer`/`FreezerCapacity`, `IsFridge`, `IsoType`.
  - `media\scripts\generated\items\*.txt` — bag `Capacity`/`WeightReduction` (308 `ItemType = base:container` items) and part-item `MaxCapacity`; `media\scripts\generated\vehicles\**` — 241 vehicle scripts with templates resolved.
  - `media\maps\Muldraugh, KY\*.lotheader` — 4,065 map cells, 85,005 room instances (527 room names); used **only** to order the "top rooms" lists.
  - `projectzomboid.jar` decompiled with `ref\_tools\cfr.jar` (scratch copy; `ref\_decompiled` holds only a partial set): `ItemContainer`, `ItemPickerJava`, `IsoObject`, `IsoWorld`, `IsoDeadBody`, `IsoStove`, `IsoBarbecue`, `IsoFireplace`, `IsoCompost`, `IsoFeedingTrough`, `IsoMannequin`, `IsoThumpable`, `IsoWheelieBin`, `ClothingWasherLogic`/`ClothingDryerLogic`, `VehiclePart`, `BaseVehicle`, `VehicleScript`, `RBBasic`, `scripting.objects.ContainerType` — engine defaults and behaviour.
- **Excluded:** throw-away container types created by UI/debug Lua (`object` in `ISGrabItemAction`, the `kitchen`/`bathroom`/… containers of `ISLootStressTestUI`) — they never exist in a world.

## Column legend

| Column | Meaning |
|---|---|
| `id` | PZ container type key exactly as the engine uses it (`ItemContainer.type`): the tile `container` property, a Distributions.lua key, a vehicle part id, or — for bags — the item id. |
| `name` | English display name: `IGUI_ContainerTitle_<id>` (tile/body types), `IGUI_VehiclePart<id>` (vehicle parts — what the loot window shows), `ItemName.json` `Base.<id>` (bags). Every `Outfit_*`/`body` corpse shows the corpse title. `?` = no translation exists (the game would print `!Needs IGUI_ContainerTitle…`). |
| `group` | furniture · appliance · storage · bag · vehicle · body · floor · other. Rows are sorted by group in that order, then by `id` (case-insensitive). |
| `capacity` | Effective full capacity (weight units). Tile types: the most common `ContainerCapacity` across the B42 map — each sprite with that `container` weighted by the number of map cells whose lotheader lists it (sprite counts only when no sprite is used on the map; ties → smaller); sprites without `ContainerCapacity` use the **engine default 50** (`ItemContainer.capacity = 50`), except composter 30 and campfire 10. Freezer: `FreezerCapacity` the same way (default 15). Corpses 12. Bags: script `Capacity`. Vehicle parts: most common value over all vehicle scripts. `getCapacity()` clamps world containers to 100, bags to 50, vehicle parts to 1000. Organized ×1.3 (at least +1) / Disorganized ×0.7 apply to every container except character inventories, corpses and the floor. `?` = unknown (no sprite/script defines it); `-` = not a physical container. |
| `rooms` | Tile/body/other rows: distinct room definitions in Distributions.lua that list this key (the generic `all` table counts as one; merge aliases are not double-counted). **Bag** rows: distinct room definitions whose loot (procedural lists, direct tables, junk) can spawn the bag item itself. **Vehicle** rows: vehicle distribution keys (`VehicleDistributions[1]`) whose Normal/Specific sets fill the part. |
| `procLists` | Distinct `ProceduralDistributions.list` entries referenced by this key's `procList`s (direct tables are not counted). Bag rows: procedural lists that spawn the bag **and** refill it through their `bags` table. Vehicle rows: distinct `VehicleDistributions.*` tables feeding the part. |
| `topLoot` | Up to 8 item display names ordered by aggregate weight (see *How loot shares are computed*); identical display names (colour/texture variants) are merged; commas inside names are dropped. Empty = the container never rolls loot. |
| `icon` | Texture of `ContainerButtonIcons[id]` (`media/ui/<name>.png` or `media/textures/<name>.png`); `-` = no entry → world containers fall back to `Container_Shelf` (`ISInventoryPage.conDefault`), bags show their own item icon, corpses use the inventorymale/female icons. |
| `clonezType` | Closest CLONE-Z `ContainerType` (Crate, Locker, Fridge, MedCabinet, Desk, ToolBench, Wardrobe, Pantry, GunLocker, Shelf, Corpse), `Sack` for bags and the floor (CLONE-Z drops items into a sack), `-` = no equivalent yet. |

### How loot shares are computed

Mirrors `ItemPickerJava`: every item entry is checked once per roll with probability `chance/100` (`Rand.Next(10000) < chance × 100 × lootModifier`), so an entry contributes **rolls × min(chance, 100) / 100** expected spawns per fill; junk entries use chance × 1.4 (`getBaseChanceMultiplier`) and their own rolls. A procedural container picks **one** list per fill, weighted by `weightChance` (missing/0 → 1, as in `rollProceduralItemInternal`); lists gated by `forceForTiles`/`forceForZones`/`forceForRooms`/`forceForItems` are counted with that same weight (marked *forced* below), and `min`/`max` per-building counts are ignored. Each room definition that lists the container contributes once (the map is not weighted — shares describe the tables, not world frequency). Vehicle parts: per vehicle key the Normal set gets 1 − p and each Specific set p / n, with p = (specialLootChance + 1) / 100 (default 8 → 9%; skin-specific keys never use Specific sets), as in `BaseVehicle.randomizeContainers`. Items that no longer exist (unknown or `Obsolete`) are skipped like the engine does. Sandbox loot multipliers are assumed equal; spawning stops early in the game when a container is full.

Fill order (`ItemPickerJava.fillContainerInternal`): the room's own entry for the container type → the room's `other` entry (skipped for the 40 *no-generic* types such as fridge, stove, bin, postbox) → the room's `all` entry → the generic top-level `all` table. Rooms without any definition (e.g. `hallway`, `diningroom`, `elevator` — 7,518 map instances) use the generic `all` table. A bag spawned in a non-shop container is filled from the spawning list's `bags` table when it has one, otherwise from the bag's own table (subject to its `fillRand`, and not in the `NoContainerFillRooms` rooms such as stores).

## Catalogue

```tsv
id	name	group	capacity	rooms	procLists	topLoot	icon	clonezType
clothingrack	Clothing Rack	furniture	50	30	31	T-shirt, T-shirt - Music, Wallet, T-shirt - Sports, Shirt - Lumberjack, Suit Pants, Leather Coat, Leather Jacket	-	Wardrobe
coffin	Coffin	furniture	30	1	0	-	Item_Coffin_Plain	-
counter	Cupboard	furniture	50	218	256	Plastic Bag, Tote Bag, Magazine, Paper Bag, Paperback, Sheet of Paper, Mug, Dish Towel	Container_Counter	Pantry
cupboard	Cupboard	furniture	?	0	0	-	-	Pantry
desk	Desk	furniture	20	53	31	Paperwork, Vehicle Key, Note, Magazine, Menu, Paperback, Money, Receipt	Container_Drawer	Desk
dishescabinet	Cabinet	furniture	25	7	5	Wine Glass, Bottle of Fine Red Wine, Brandy, Port, Vermouth, Bottle of Whiskey, Sherry, Scotch	-	Pantry
displaycase	Display Case	furniture	20	22	42	Digital Watch, Wrist Watch - Classic, Whetstone, Kitchen Knife, Camera, Pizza Slice, Cigar, Fleshing Tool	-	Shelf
displaycasebakery	Display Case	furniture	20	18	13	Cone, T-shirt - Music, Cake Slice, Chocolate Donut, Donut, Frosted Donut, Jelly Donut, Cherry Pie Slice	-	Pantry
dresser	Drawer	furniture	20	24	19	Money, Paperwork, Paperback, Hand-drawn Armor Schematic, Magazine, Sheet of Paper, Bra - Strapless, Bra	Container_Drawer	Wardrobe
filingcabinet	File Cabinet	furniture	15	20	7	Paperwork, Sheet of Paper, Vehicle Key, Note, Comic Book, Notebook, Index Card, Clipboard	Container_Drawer	Desk
grocerstand	Grocer Display	furniture	20	12	20	Can of Soda, Cola, Diet Cola, Ginger Ale, Orange Soda, Water Bottle, Beer Bottle, Beer Can	-	Pantry
locker	Locker	furniture	50	53	77	Magazine, Paperback, Gum, Patrol Revolver, Box of 5.56x45mm Rounds, Holster, JS-2000 Shotgun, Kneepad	-	Locker
mannequin	Mannequin	furniture	50	0	0	-	Container_Mannequin	Wardrobe
medicine	Cabinet	furniture	10	11	6	Bandage - Adhesive, Alcohol Wipes, Painkillers, Bandage, Surgical Gloves, Rag, Cotton Balls, Tweezers	Container_Medicine	MedCabinet
metal_shelves	Shelves	furniture	50	192	356	Painkillers, Plank, Baseball Bat, Bath Towel, Medium Leather (Crude), Steel Sheet, Camera Film, Box of 5.56x45mm Rounds	Container_Shelf	Shelf
militarylocker	Locker	furniture	50	9	8	Box of 5.56x45mm Rounds, M9 Pistol, Box of .30-30 Rounds, Box of 12g Rounds, Box of 7.62x51mm Rounds, Box of 9x19mm Rounds, Small Protective Military Case, Box of .357 Magnum Rounds	-	GunLocker
officedrawers	Drawer	furniture	?	1	1	Note, Paperwork, Sheet of Paper, Magazine, Index Card, Notebook, Paperclip, Pencil	Container_Drawer	Desk
overhead	Cupboard	furniture	50	15	20	Paper Napkins, Magazine, Drinking Glass, Bowl, Money Bundle, Mug, Cigarette Pack, Sugar Cubes	-	Pantry
restaurantdisplay	Display	furniture	50	30	50	Pizza Slice, Fries, Burger, Chicken Nuggets, Fish (Fried), Chicken Wing, Ice Cream, Fried Onion Rings	-	Fridge
safe	?	furniture	?	1	1	-	-	-
schooldesk	?	furniture	?	4	2	Note, Sheet of Paper, Book, Notebook, Paperback, Pencil, Magazine, Eraser	-	Desk
shelves	Shelves	furniture	20	127	336	Book, Drinking Glass, Paperback, VHS - Retail, Magazine, Sheet of Paper, Prop Book, Comic Book	Container_Shelf	Shelf
shelvesmag	Magazine Stand	furniture	15	18	16	Magazine, Comic Book, Newspaper, Brochure, Flier, Magazine: TV Monthly, Paperback, HottieZ	-	Shelf
sidetable	Drawer	furniture	10	29	21	Hair Gel, Hair Spray, Bottle of Hair Dye, Magazine, Surgical Gloves, Surgical Mask, Sewing Pattern, Paperback	Container_Drawer	Desk
toolcabinet	Tool Cabinet	furniture	20	22	16	Welding Rods, Nuts and Bolts, Large Hook, Screwdriver, Welding Torch, Medium Leather (Crude), Pliers, Box of Screws	Container_ToolCabinet	ToolBench
wardrobe	Cabinet	furniture	25	19	29	Sheet, Bottle of Hair Dye, Hand-drawn Armor Schematic, Bath Towel, Magazine, Hair Spray, Melee Weapon Plan, Pillow	Container_Cabinet	Wardrobe
barbecue	Barbecue	appliance	15	3	2	Basting Brush, Carving Fork, Grill Brush, Kitchen Tongs, Oven Mitt, Spatula, Beef Patty, Corn	Container_Oven	-
barbecuepropane	Barbecue	appliance	15	3	2	Basting Brush, Carving Fork, Grill Brush, Kitchen Tongs, Oven Mitt, Spatula, Beef Patty, Corn	Container_Oven	-
cashregister	Cash Register	appliance	5	1	0	Money, Receipt, Note, Pencil, Key, Notepad, Vehicle Key, Calculator	-	-
clothingdryer	Clothing Dryer	appliance	20	10	15	Dish Towel, Rag, Bath Towel, Sheet, Dryer Lint, Shoes, Long Socks, Socks	Container_ClothingDryer	Wardrobe
clothingwasher	Clothing Washer	appliance	20	10	15	Dish Towel, Rag (Dirty), Bath Towel, Sheet, Shoes, T-shirt, Police T-shirt - Deputy, Police Jacket	Container_ClothingWasher	Wardrobe
coffeemaker	Coffee Maker	appliance	?	0	0	-	Container_Oven	-
dishwasher	Dishwasher	appliance	10	0	0	-	-	-
displaycasebutcher	Display Case	appliance	50	6	5	Pork Chop, Chicken Leg, Shrimp, Steak, Beef Patty, Ground Beef, Bacon, Baloney	-	Fridge
fireplace	Fireplace	appliance	20	9	3	Money, Brochure, Flier, Mail, Old Newspaper, Magazine, Paperwork, Catalog	Container_Oven	-
freezer	Freezer	appliance	20	100	32	Ice Cream, Bag of Fries, Butter, Beef Patty, Shrimp, Ground Beef, Margarine, Bag of Chicken Nuggets	Container_Freezer	Fridge
fridge	Fridge	appliance	40	100	54	Water Bottle, Imported Beer, Beer Can, Beer Bottle, Milk Carton, Cola, Diet Cola, Ginger Ale	Container_Fridge	Fridge
icecream	Freezer	appliance	?	0	0	-	Container_Freezer	Fridge
microwave	Microwave	appliance	5	9	1	Bacon, Bowl of Oatmeal, TV Dinner	Container_Microwave	-
stonefurnace	Furnace	appliance	50	0	0	-	-	-
stove	Oven	appliance	15	23	8	Beef Patty, Rag (Dirty), Tin Can, Baking Tray, Roasting Pan, Baking Pan, Muffin Tray, Cockroach	Container_Oven	-
toaster	Toaster	appliance	?	0	0	-	Container_Oven	-
vendingGt	Vending Machine	appliance	?	0	0	-	Container_Vendingt	-
vendingpop	Vending Machine	appliance	20	1	0	Cola, Diet Cola, Ginger Ale, Orange Soda, Bottle	Container_Vendingt	Fridge
vendingsnack	Vending Machine	appliance	20	1	0	Chips - Barbecue, Chips - Plain, Chips - Salt & Vinegar, Chips - Sour Cream & Onion, Pork Rinds, Pretzels, Peanuts, Sunflower Seeds	Container_Vendingt	Pantry
woodstove	Stove	appliance	15	8	2	Twigs, Firewood, Tree Branch, Frying Pan, Fireplace Poker, Old Newspaper, Bellows, Cooking Pot	Container_Oven	-
bin	Garbage	storage	10	21	9	Bandage (Dirty), Receipt, Paper Napkins, Straw, Fountain Cup, Fries, Rag (Dirty), Tin Can	Container_Garbage	-
BombBox	?	storage	?	1	0	Molotov Cocktail, Smoke Bomb, Aerosol Bomb, Pipe Bomb, Remote Smoke Bomb, Smoke Bomb with Medium-Range Sensor, Smoke Bomb with Timer, Fire Bomb	-	GunLocker
BoozeBox	?	storage	?	1	0	Bottle of Whiskey, Brandy, Gin, Rum, Scotch, Tequila, Vodka, Beer Bottle	-	Pantry
cardboardbox	Box	storage	25	142	348	Paperwork, Camera Film, Book, Newspaper, Sheet of Paper, Water Bottle, Photograph, Magazine	Container_Crate	Crate
clothingdryerbasic	Laundry Cart	storage	20	17	21	Bath Towel, Sheet, Dish Towel, Beer Bottle (empty), Bottle of Whiskey (empty), Hospital Gown, Produce Box - Medium, Basketball	-	Wardrobe
crate	Crate	storage	60	113	345	Plank, Steel Bar, Produce Box - Medium, Steel Sheet, Baseball Bat, Steel Ingot, Sack, Flint Nodule	Container_Crate	Crate
dumpster	Dumpster	storage	30	1	0	Garbage Bag, Empty Pallet, Dryer Lint, Paperwork, T-shirt, Pants, Game Pieces, Bra - Strapless	Container_Garbage	-
FoodBox	?	storage	60	1	0	Canned Fruit Beverage, Beef Jerky, Bouillon Cube, Crackers, Dehydrated Meat Stick, Jar of Bell Peppers, Jar of Broccoli, Jar of Cabbage	-	Pantry
garagestorage	Crate	storage	?	0	0	-	-	Crate
GunBox	?	storage	18	2	0	Box of .357 Magnum Rounds, Box of .38 Special Rounds, Box of .44 Magnum Rounds, Box of .45 ACP Rounds, Box of 12g Rounds, Box of 5.56x45mm Rounds, Box of 7.62x51mm Rounds, Box of 9x19mm Rounds	-	GunLocker
logs	Logs	storage	20	1	0	Log, Wood Splinters, Broken Branch, Firewood, Tree Branch, Large Branch, Pillbug, Sapling	Item_Logs	-
MedicalBox	?	storage	7	1	0	Bandage, Antibiotics, Bottle of Whiskey, Painkillers, Antidepressants, Beta Blockers, Forceps, Sleeping Pills	-	MedCabinet
militarycrate	Military Crate	storage	50	7	10	Electrical Wire, Small Protective Military Case, Box of 5.56x45mm Rounds, Ammunition Box, Power Bar, Box of .30-30 Rounds, Box of 12g Rounds, Box of 7.62x51mm Rounds	Container_Crate	Crate
newspaper_dispatch	Newspaper Box	storage	5	1	0	Newspaper	-	-
newspaper_herald	Newspaper Box	storage	5	1	0	Newspaper	-	-
newspaper_knews	Newspaper Box	storage	5	1	0	Newspaper	-	-
newspaper_times	Newspaper Box	storage	5	1	0	Newspaper	-	-
plankstash	Stash	storage	7	5	5	Money, 12g Round, SN38 Revolver, Magazine: HottieZ, M9 Pistol, Box of .357 Magnum Rounds, Box of .38 Special Rounds, Box of .44 Magnum Rounds	-	Crate
postbox	Mailbox	storage	5	1	0	Flier, Newspaper, Brochure, Mail, Magazine, Handwritten Letter, Parking Ticket, Speeding Ticket	Container_MailBox	-
ShotgunBox	?	storage	7	2	0	Box of 12g Rounds, JS-2000 Shotgun, Double Barrel Shotgun, Choke Tube - Full, Choke Tube - Improved, Recoil Pad, Shells Bandolier, Screwdriver	-	GunLocker
smallbox	Box	storage	10	54	138	Paperwork, Camera Film, Railroad Spike, Sheet of Paper, Pencil, Notepad, Magazine, Notebook	Container_Crate	Crate
smallcrate	Crate	storage	25	4	27	Plastic Tray, Produce Box - Small, Paper Napkins, Produce Box - Extra Small, Produce Box - Medium, Cherry, Strawberries, Canned Fruit Cocktail	Container_Crate	Crate
SurvivorCrate	Crate	storage	60	3	0	Water Bottle, Water Purification Tablets, Beef Jerky, Butterfly Knife, Candle, Compass, Dehydrated Meat Stick, Firestarting Block	-	Crate
ToolsBox	?	storage	60	1	0	Box of Nails, Funnel, Lead Pipe, Measuring Tape, Ratchet Wrench, Rope, Rubber Hose, Screwdriver	-	ToolBench
wheeliebin	?	storage	50	0	0	-	-	-
AmmoStrap_Brown_Bullets	Bullets Bandolier	bag	1	0	0	-	-	Sack
AmmoStrap_Brown_Shells	Shells Bandolier	bag	1	0	0	-	-	Sack
AmmoStrap_Bullets	Bullets Bandolier	bag	1	18	0	5.56x45mm Round	-	Sack
AmmoStrap_Bullets_308	Bullets Bandolier	bag	1	1	0	7.62x51mm Round	-	Sack
AmmoStrap_Bullets_38	?	bag	?	0	0	.38 Special Round	-	Sack
AmmoStrap_Bullets_44	?	bag	?	0	0	.44 Magnum Round	-	Sack
AmmoStrap_Bullets_45	?	bag	?	0	0	.45 ACP Round	-	Sack
AmmoStrap_Bullets_9mm	?	bag	?	0	0	9x19mm Round	-	Sack
AmmoStrap_Shells	Shells Bandolier	bag	1	20	0	12g Round	-	Sack
Bag_ALICE_BeltSus	ALICE Belt and Suspenders	bag	6	1	0	Gum, First Aid Kit - Military, Gas Mask Filter, Alcohol Wipes, Bandage, Canteen, Chewing Tobacco, Compass	-	Sack
Bag_ALICE_BeltSus_Camo	ALICE Belt and Suspenders	bag	6	6	0	-	-	Sack
Bag_ALICE_BeltSus_Green	ALICE Belt and Suspenders	bag	6	6	0	-	-	Sack
Bag_ALICEpack	Military Backpack	bag	28	23	0	Gum, Bandage - Adhesive, Compass, Granola Bar, Alcohol Wipes, Beef Jerky, Dehydrated Meat Stick, Matchbook	-	Sack
Bag_ALICEpack_Army	Military Backpack	bag	28	8	0	Gum, Plastic Spork, First Aid Kit - Military, Gas Mask Filter, Alcohol Wipes, Bandage, Canteen, Chewing Tobacco	-	Sack
Bag_ALICEpack_DesertCamo	Military Backpack	bag	28	2	0	Gum, Plastic Spork, First Aid Kit - Military, Gas Mask Filter, Alcohol Wipes, Bandage, Canteen, Chewing Tobacco	-	Sack
Bag_AmmoBox	Ammunition Box	bag	6	5	0	Box of 5.56x45mm Rounds	-	Sack
Bag_AmmoBox_308	Ammunition Box	bag	6	3	0	Box of 7.62x51mm Rounds	-	Sack
Bag_AmmoBox_38	Ammunition Box	bag	6	0	0	Box of .38 Special Rounds	-	Sack
Bag_AmmoBox_44	Ammunition Box	bag	6	0	0	Box of .44 Magnum Rounds	-	Sack
Bag_AmmoBox_45	Ammunition Box	bag	6	1	0	Box of .45 ACP Rounds	-	Sack
Bag_AmmoBox_9mm	Ammunition Box	bag	6	3	0	Box of 9x19mm Rounds	-	Sack
Bag_AmmoBox_Hunting	Ammunition Box	bag	6	7	0	Box of .30-30 Rounds, Box of 12g Rounds, Box of 5.56x45mm Rounds, Box of 7.62x51mm Rounds	-	Sack
Bag_AmmoBox_Mixed	Ammunition Box	bag	6	5	0	Box of 5.56x45mm Rounds, Box of .38 Special Rounds, Box of 12g Rounds, Box of 9x19mm Rounds, Box of .30-30 Rounds, Box of .357 Magnum Rounds, Box of .44 Magnum Rounds, Box of .45 ACP Rounds	-	Sack
Bag_AmmoBox_ShotgunShells	Ammunition Box	bag	6	4	0	Box of 12g Rounds	-	Sack
Bag_BaseballBag	Duffel Bag	bag	18	3	0	Baseball, Gum, Baseball Bat, Metal Baseball Bat, Pencil, Notepad, Sports Bottle, Whistle	-	Sack
Bag_BigHikingBag	Big Hiking Backpack	bag	22	25	0	Gum, Bandage - Adhesive, Granola Bar, Alcohol Wipes, Apricots (Dried), Compass, Dehydrated Meat Stick, Insect Repellent	-	Sack
Bag_BigHikingBag_Travel	Big Hiking Backpack	bag	22	5	0	Gum, Bandage - Adhesive, Granola Bar, Alcohol Wipes, Apricots (Dried), Compass, Dehydrated Meat Stick, Insect Repellent	-	Sack
Bag_BirthdayBasket	Basket	bag	6	0	0	Birthday Card, CD, Comic Book, Diary, Earbuds, Handheld Game Console, Headphones, Paperback	-	Sack
Bag_BowlingBallBag	Bowling Ball Bag	bag	6	2	0	Pencil, Bowling Shoes, Notepad, Money, Bath Towel, Fingerless Gloves - Leather, Bowling Shirt - Blue, Bowling Shirt - Brown	-	Sack
Bag_BreakdownBag	Duffel Bag	bag	18	0	0	Electrical Wire, Flashlight, Funnel, Lug Wrench, Rope, Rubber Hose, Screwdriver, Sheet	-	Sack
Bag_BurglarBag	Duffel Bag	bag	18	9	0	Crowbar, Duct Tape, Rope, Balaclava, Hacksaw, Leather Gloves, ID Card - Blank, Ball-peen Hammer	-	Sack
Bag_ChestRig	Chest Rig	bag	5	6	0	-	-	Sack
Bag_ChestRig_Tarp	Tarp Chest Rig	bag	5	0	0	-	-	Sack
Bag_ClothSatchel_Burlap	Crafted Burlap Satchel	bag	12	0	0	-	-	Sack
Bag_ClothSatchel_Cotton	Crafted Cotton Satchel	bag	12	0	0	-	-	Sack
Bag_ClothSatchel_Denim	Crafted Denim Satchel	bag	12	0	0	-	-	Sack
Bag_ClothSatchel_DenimBlack	Crafted Denim Satchel	bag	12	0	0	-	-	Sack
Bag_ClothSatchel_DenimLight	Crafted Denim Satchel	bag	12	0	0	-	-	Sack
Bag_ConstructionBag	?	bag	?	0	0	Box of Nails, Duct Tape, Measuring Tape, Rag, Claw Hammer, Dust Mask, Hacksaw, Hard Hat	-	Sack
Bag_CraftedFramepack_Large	Simple Framepack	bag	20	0	0	-	-	Sack
Bag_CraftedFramepack_Large2	Framepack	bag	25	0	0	-	-	Sack
Bag_CraftedFramepack_Large3	Large Framepack	bag	35	0	0	-	-	Sack
Bag_CraftedFramepack_Small	Small Simple Framepack	bag	15	0	0	-	-	Sack
Bag_CrudeLeatherBag	Small Leather Backpack	bag	14	0	0	-	-	Sack
Bag_CrudeTarpBag	Small Tarp Backpack	bag	14	0	0	-	-	Sack
Bag_Dancer	Tote Bag	bag	6	10	0	Gum, Can of Soda, Bra - Strapless, Bra, Caffeine Pills, Underpants, Cigarette Pack, Diary	-	Sack
Bag_DeadMice	Sack	bag	15	0	0	Dead Mouse	-	Sack
Bag_DeadRats	Sack	bag	15	0	0	Dead Rat	-	Sack
Bag_DeadRoaches	Sack	bag	15	0	0	Cockroach	-	Sack
Bag_DoctorBag	Doctor Bag	bag	8	0	0	Alcohol Wipes, Bandage - Adhesive, Tongue Depressor, Stethoscope, Bandage, Painkillers, Suture Needle, Forceps	-	Sack
Bag_DoctorBag_Money	?	bag	?	0	0	Money, Money Bundle, Stock Certificate, Pouch	-	Sack
Bag_DuffelBag	Duffel Bag	bag	18	6	0	Gum, Bath Towel, CD, Long Shorts - Sport, Shorts - Sport, Socks, Sport Pants, T-shirt - Sports	-	Sack
Bag_DuffelBagTINT	Duffel Bag	bag	18	32	0	Gum, Bath Towel, CD, Long Shorts - Sport, Shorts - Sport, Socks, Sport Pants, T-shirt - Sports	-	Sack
Bag_FannyPackBack	Fanny Pack	bag	1	0	0	Gum, Key Ring, Vehicle Key, Tissue, Wallet, Alcohol Wipes, Bandage - Adhesive, Comb	-	Sack
Bag_FannyPackBack_Hide	Hide Fanny Pack	bag	1	0	0	-	-	Sack
Bag_FannyPackBack_Tarp	Tarp Fanny Pack - Back	bag	1	0	0	-	-	Sack
Bag_FannyPackFront	Fanny Pack	bag	1	37	0	Gum, Key Ring, Vehicle Key, Tissue, Wallet, Alcohol Wipes, Bandage - Adhesive, Comb	-	Sack
Bag_FannyPackFront_Hide	Hide Fanny Pack	bag	1	0	0	-	-	Sack
Bag_FannyPackFront_Tarp	Tarp Fanny Pack	bag	1	0	0	-	-	Sack
Bag_FishingBasket	Fishing Basket	bag	6	14	0	Black Crappie, Bluegill, Green Sunfish, Largemouth Bass, Redear Sunfish, Sauger, Smallmouth Bass, Spotted Bass	-	Sack
Bag_FluteCase	Flute Case	bag	1	14	0	Flute	-	Sack
Bag_FoodCanned	Duffel Bag	bag	18	0	0	Can Opener, Canned Beans, Canned Carrots, Canned Chili, Canned Corn, Canned Corned Beef, Canned Fruit Cocktail, Canned Mushroom Soup	-	Sack
Bag_FoodSnacks	Duffel Bag	bag	18	0	0	Beef Jerky, Dehydrated Meat Stick, Granola Bar, Milk Chocolate Bar, Licorice, Chips - Barbecue, Chips - Plain, Chips - Salt & Vinegar	-	Sack
Bag_FruitBasket	?	bag	?	0	0	Apple, Orange, Banana, Cherry, Grapefruit, Grapes, Mango, Peach	-	Sack
Bag_GardenBasket	Garden Basket	bag	6	17	0	Agriculture I: "Better Gardening", Gardening Trowel, Hand Drill, Hand Fork, Kneepad, Leather Gloves, Measuring Tape, Pliers	-	Sack
Bag_GolfBag	Golf Bag	bag	18	4	0	Golf Tee, Golf Ball, Golf Club, Notepad, Pencil, Visor, Cigarette Pack, Golf Cap	-	Sack
Bag_GolfBag_Melee	Golf Bag	bag	18	6	0	Baseball Bat, Field Hockey Stick, Ice Hockey Stick, Lacrosse Stick, Metal Baseball Bat, Crowbar, Hatchet, Wooden Spear	-	Sack
Bag_Gunny	Gunny Sack	bag	20	0	0	-	-	Sack
Bag_HideSack	Hide Sack	bag	20	0	0	-	-	Sack
Bag_HideSatchel	Crafted Hide Satchel	bag	12	0	0	-	-	Sack
Bag_HideSlingBag	Hide Sling Bag	bag	14	0	0	-	-	Sack
Bag_HikingBag_Travel	Hiking Backpack	bag	20	5	0	Gum, Bandage - Adhesive, Granola Bar, Alcohol Wipes, Apricots (Dried), Compass, Dehydrated Meat Stick, Insect Repellent	-	Sack
Bag_HospitalBasket	?	bag	?	0	0	Teddy Bear, Sympathy Card, Boris The Badger, Eye of Cthulhu, Fluffyfoot The Bunny, Freddy The Fox, Furbert The Squirrel, Jacques The Beaver	-	Sack
Bag_HydrationBackpack	Hydration Pack	bag	12	18	0	-	-	Sack
Bag_HydrationBackpack_Camo	Hydration Pack	bag	12	0	0	-	-	Sack
Bag_InmateEscapedBag	Duffel Bag	bag	18	0	0	Rag, Sheet Rope, Glass Shiv, Rope, Shiv, Duct Tape, Ice Pick, Simple Metal Knife	-	Sack
Bag_JanitorToolbox	Toolbox	bag	8	5	0	Pipe Wrench, Claw Hammer, Flashlight, Hacksaw, Measuring Tape, Pliers, Screwdriver, Wrench	-	Sack
Bag_Laundry	Laundry Bag	bag	20	0	0	Pants, T-shirt, Bath Towel, Sheet, Long Socks, Socks, Underpants, Tank Top	-	Sack
Bag_LaundryHospital	Laundry Bag	bag	20	0	0	Sheet, Bath Towel, Hospital Gown, Medical Scrubs, Pants - Medical	-	Sack
Bag_LaundryLinen	Laundry Bag	bag	20	0	0	Bath Towel, Sheet	-	Sack
Bag_Mail	Mail Bag	bag	20	2	0	Magazine, Brochure, Flier, Newspaper, Parcel - Extra Small, Parcel - Small, Parcel - Medium, Magazine: Crossword	-	Sack
Bag_MedicalBag	Trauma Bag	bag	18	17	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Suture Needle, Painkillers, Stethoscope, Cotton Balls, Tongue Depressor	-	Sack
Bag_Military	Duffel Bag	bag	18	8	0	Gum, Plastic Spork, First Aid Kit - Military, Gas Mask Filter, Alcohol Wipes, Bandage, Canteen, Chewing Tobacco	-	Sack
Bag_MoneyBag	Duffel Bag	bag	18	17	0	Money, Money Bundle, Stock Certificate, Pouch	-	Sack
Bag_NormalHikingBag	Hiking Backpack	bag	20	22	0	Gum, Bandage - Adhesive, Granola Bar, Alcohol Wipes, Apricots (Dried), Compass, Dehydrated Meat Stick, Insect Repellent	-	Sack
Bag_PicnicBasket	Picnic Basket	bag	6	3	0	Juice Box, Baguette, Butter Knife, Chocolate Milk - Personal-sized, Croissant, Fork, Fruit Muffin, Granola Bar	-	Sack
Bag_Police	Police Duffel Bag	bag	18	9	0	Holster, JS-2000 Shotgun, M9 Pistol, Nightstick, Patrol Revolver, Bulletproof Vest - Police, Holster - Ankle, Holster - Shoulder	-	Sack
Bag_ProtectiveCase	Protective Case	bag	7	7	0	-	-	Sack
Bag_ProtectiveCase_Survivalist	Protective Case	bag	7	0	0	Bandage - Adhesive, Beef Jerky, Candle, Compass, Dehydrated Meat Stick, Fishing Hook, Fishing Line, Frying Pan	-	Sack
Bag_ProtectiveCase_Tools	Protective Case	bag	7	0	0	Flashlight - Angle Headed, Funnel, Marker - Black, Measuring Tape, Nails, Nuts and Bolts, Pliers, Rubber Hose	-	Sack
Bag_ProtectiveCaseBulky	Bulky Protective Container	bag	16	7	0	-	-	Sack
Bag_ProtectiveCaseBulky_Audio	Bulky Protective Container	bag	16	0	0	Speaker, Microphone, Electrical Wire, Headphones, Power Bar	-	Sack
Bag_ProtectiveCaseBulky_HAMRadio1	Bulky Protective Container	bag	16	0	0	Premium Technologies Ham Radio	-	Sack
Bag_ProtectiveCaseBulky_SCBA	Bulky Protective Container	bag	16	0	0	SCBA (No Tank), Oxygen Tank	-	Sack
Bag_ProtectiveCaseBulky_Survivalist	Bulky Military Container	bag	16	0	0	Bandage - Adhesive, Beef Jerky, Candle, Compass, Dehydrated Meat Stick, Fishing Hook, Fishing Line, Flashlight	-	Sack
Bag_ProtectiveCaseBulkyAmmo	Bulky Ammo Container	bag	16	4	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_308	Bulky Ammo Container	bag	16	3	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_38	Bulky Ammo Container	bag	16	0	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_44	Bulky Ammo Container	bag	16	0	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_45	Bulky Ammo Container	bag	16	1	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_556	Bulky Ammo Container	bag	16	3	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_9mm	Bulky Ammo Container	bag	16	3	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_Hunting	Bulky Ammo Container	bag	16	1	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyAmmo_ShotgunShells	Bulky Ammo Container	bag	16	3	0	Ammunition Box	-	Sack
Bag_ProtectiveCaseBulkyHazard	Biohazard Container	bag	16	6	0	Bandage (Dirty), Cold Pack, Scalpel	-	Sack
Bag_ProtectiveCaseBulkyMilitary	Bulky Military Container	bag	16	2	0	-	-	Sack
Bag_ProtectiveCaseBulkyMilitary_HAMRadio2	Bulky Military Container	bag	16	5	0	US ARMY COMM. Ham Radio	-	Sack
Bag_ProtectiveCaseMilitary	Protective Military Case	bag	7	8	0	-	-	Sack
Bag_ProtectiveCaseMilitary_Medical	Protective Case - Military	bag	7	0	0	-	-	Sack
Bag_ProtectiveCaseMilitary_Tools	Protective Case - Military	bag	7	0	0	Flashlight - Angle Headed, Funnel, Marker - Black, Measuring Tape, Nails, Nuts and Bolts, Pliers, Rubber Hose	-	Sack
Bag_ProtectiveCaseSmall	Small Protective Case	bag	4	7	0	-	-	Sack
Bag_ProtectiveCaseSmall_Armorer	Small Protective Case	bag	4	4	0	Screwdriver, Choke Tube - Full, Recoil Pad, Red Dot Sight, x2 Scope, Bullets Bandolier, Choke Tube - Improved, Shells Bandolier	-	Sack
Bag_ProtectiveCaseSmall_Electronics	Small Protective Case	bag	4	0	0	Battery, Cordless Phone, Earbuds, Flashlight, Headphones, Power Bar, Premium Tech. Walkie Talkie, CD Player	-	Sack
Bag_ProtectiveCaseSmall_FirstAid	Small Protective Case	bag	4	0	0	Alcohol Wipes, Bandage - Adhesive, Suture Needle, Bandage, Surgical Gloves, Cotton Balls, Forceps, Adhesive Tape	-	Sack
Bag_ProtectiveCaseSmall_KeyCutting	Small Protective Case	bag	4	2	0	Blank Key, Small File Set, Magazine: The Cheapskate's John Spiral - Escaping Captivity	-	Sack
Bag_ProtectiveCaseSmall_Pistol1	Small Protective Case	bag	4	2	0	Box of 9x19mm Rounds, M9 Magazine, M9 Pistol, Gun Light, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Pistol2	Small Protective Case	bag	4	1	0	Box of .45 ACP Rounds, M1911 Auto Magazine, M1911 Pistol, Gun Light, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Pistol3	Small Protective Case	bag	4	1	0	Box of .44 Magnum Rounds, B-F Magazine, B-F Pistol, Gun Light, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Revolver1	Small Protective Case	bag	4	1	0	Box of .38 Special Rounds, SN38 Revolver, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Revolver2	Small Protective Case	bag	4	1	0	Box of .357 Magnum Rounds, Patrol Revolver, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Revolver3	Small Protective Case	bag	4	1	0	Box of .44 Magnum Rounds, Magnum, x2 Scope, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmall_Survivalist	Small Protective Case	bag	4	0	0	Fishing Hook, Fishing Line, Needle, Thread, Bandage - Adhesive, Candle, Compass, Magnesium Firestarter	-	Sack
Bag_ProtectiveCaseSmall_WalkieTalkie	Small Protective Case	bag	4	0	0	Premium Tech. Walkie Talkie, Battery, Box of Batteries	-	Sack
Bag_ProtectiveCaseSmall_WalkieTalkiePolice	Small Protective Case	bag	4	0	0	Tactical Walkie Talkie, Battery, Box of Batteries	-	Sack
Bag_ProtectiveCaseSmallMilitary	Small Protective Military Case	bag	4	2	0	-	-	Sack
Bag_ProtectiveCaseSmallMilitary_FirstAid	Small Protective Case - Military	bag	4	1	0	Alcohol Wipes, Bandage - Adhesive, Suture Needle, Bandage, Surgical Gloves, Cotton Balls, Forceps, Adhesive Tape	-	Sack
Bag_ProtectiveCaseSmallMilitary_Pistol1	Small Protective Case - Military	bag	4	2	0	Box of 9x19mm Rounds, M9 Magazine, M9 Pistol, Gun Light, Holster - Shoulder	-	Sack
Bag_ProtectiveCaseSmallMilitary_WalkieTalkie	Small Protective Case - Military	bag	4	5	0	US Army Walkie Talkie, Battery, Box of Batteries	-	Sack
Bag_RifleCase	Gun Case	bag	7	2	0	-	-	Sack
Bag_RifleCase_Police	Gun Case	bag	7	0	0	Box of 7.62x51mm Rounds, MSR788 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_RifleCase_Police2	Gun Case	bag	7	0	0	Box of 7.62x51mm Rounds, M1A Magazine, M1A Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_RifleCase_Police3	Gun Case	bag	7	0	0	Box of 5.56x45mm Rounds, M16 Magazine, M16 Assault Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, Red Dot Sight, x4 Scope	-	Sack
Bag_RifleCaseCloth	Cloth Gun Case	bag	7	12	0	Box of 5.56x45mm Rounds, MSR700 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_RifleCaseCloth2	Cloth Gun Case	bag	7	11	0	Box of 7.62x51mm Rounds, MSR788 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_RifleCaseClothCamo	Cloth Gun Case	bag	7	4	0	Box of 7.62x51mm Rounds, MSR788 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_RifleCaseGreen	Gun Case - Military	bag	7	4	0	Box of 5.56x45mm Rounds, M16 Magazine, M16 Assault Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, Red Dot Sight, x4 Scope	-	Sack
Bag_RifleCaseGreen2	Gun Case - Military	bag	7	0	0	Box of 7.62x51mm Rounds, M1A Magazine, M1A Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
Bag_Satchel	Satchel	bag	12	30	0	Book, Sheet of Paper, Comic Book, Gum, Paperback, Pencil, Pen - Black, Pen - Blue	-	Sack
Bag_Satchel_Fishing	Fishing Satchel	bag	12	5	0	Fishing Hook, Fishing Line, Bobber, Bottle Opener, Fillet Knife, Fishing I: "Dean's Good Fishing Guide", Flashlight - Angle Headed, Insect Repellent	-	Sack
Bag_Satchel_Leather	Leather Satchel	bag	13	0	0	Book, Sheet of Paper, Comic Book, Gum, Paperback, Pencil, Pen - Black, Pen - Blue	-	Sack
Bag_Satchel_Mail	Mail Satchel	bag	12	2	0	Brochure, Flier, Old Newspaper, Parcel - Extra Small, Magazine, Parcel - Small, Parcel - Medium, Magazine: Crossword	-	Sack
Bag_Satchel_Medical	Medical Satchel	bag	12	17	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Suture Needle, Painkillers, Stethoscope, Cotton Balls, Tongue Depressor	-	Sack
Bag_Satchel_Military	Military Satchel	bag	12	2	0	First Aid Kit - Military, Gas Mask Filter, Alcohol Wipes, Bandage, Compass, Dehydrated Meat Stick, Earbuds, Flashlight - Angle Headed	-	Sack
Bag_SatchelPhoto	Satchel	bag	12	5	0	Camera Film, Camera, Photograph, Sheet of Paper, High-end Camera, Photo Album, Book, Gum	-	Sack
Bag_SaxophoneCase	Saxophone Case	bag	3	14	0	Saxophone	-	Sack
Bag_Schoolbag	Small Backpack	bag	15	15	0	Book, Comic Book, Paperback, Pencil, Pen - Black, Pen - Blue, Pen - Red, Gum	-	Sack
Bag_Schoolbag_Kids	Small Backpack	bag	15	3	0	Comic Book, Crayons, Doodle, Handheld Game Console, Magazine, Paperback, Book, Brick Toys	-	Sack
Bag_Schoolbag_Medical	Small Backpack	bag	12	0	0	-	-	Sack
Bag_Schoolbag_Patches	Small Backpack	bag	15	3	0	Book, Comic Book, Paperback, Pencil, Pen - Black, Pen - Blue, Pen - Red, Gum	-	Sack
Bag_Schoolbag_Travel	Small Backpack	bag	15	5	0	Book, Comic Book, Paperback, Pencil, Pen - Black, Pen - Blue, Pen - Red, CD	-	Sack
Bag_SewingBasket	?	bag	?	0	0	Yarn, Knitting Needles, Measuring Tape, Sewing Kit	-	Sack
Bag_SheetSlingBag	Sheet Sling Bag	bag	10	0	0	-	-	Sack
Bag_Sheriff	Sheriff Duffel Bag	bag	18	0	0	Holster, JS-2000 Shotgun, M9 Pistol, Nightstick, Patrol Revolver, Bulletproof Vest - Police, Holster - Ankle, Holster - Shoulder	-	Sack
Bag_ShotgunBag	Duffel Bag	bag	18	0	0	12g Round, JS-2000 Shotgun, Box of 12g Rounds	-	Sack
Bag_ShotgunCase_Police	Gun Case	bag	7	0	0	Box of 12g Rounds, JS-2000 Shotgun, Choke Tube - Full, Choke Tube - Improved, Shells Bandolier	-	Sack
Bag_ShotgunCaseCloth	Cloth Gun Case	bag	7	11	0	Box of 12g Rounds, JS-2000 Shotgun, Choke Tube - Full, Choke Tube - Improved, Shells Bandolier	-	Sack
Bag_ShotgunCaseCloth2	Cloth Gun Case	bag	7	11	0	Box of 12g Rounds, Double Barrel Shotgun, Shells Bandolier	-	Sack
Bag_ShotgunCaseGreen	Gun Case - Military	bag	7	3	0	Box of 12g Rounds, JS-2000 Shotgun, Choke Tube - Full, Choke Tube - Improved, Shells Bandolier	-	Sack
Bag_ShotgunDblBag	Duffel Bag	bag	18	0	0	12g Round, Double Barrel Shotgun, Box of 12g Rounds	-	Sack
Bag_ShotgunDblSawnoffBag	Duffel Bag	bag	18	4	0	12g Round, Sawed-off Double Barrel Shotgun, Box of 12g Rounds	-	Sack
Bag_ShotgunSawnoffBag	Duffel Bag	bag	18	5	0	12g Round, Sawed-off JS-2000 Shotgun, Box of 12g Rounds	-	Sack
Bag_Skill_Maintenance_DuffelBag	?	bag	?	0	0	Maintenance I: "Basic Repairs", Duct Tape, Epoxy, Fiberglass Tape, Wood Glue, Maintenance II: "Better Tools Made Easy", Flashlight - Angle Headed, Funnel	-	Sack
Bag_SurvivorBag	Military Backpack	bag	28	0	0	Candle, Gas Mask Filter, Paperback, Beef Jerky, Canned Beans, Canned Carrots, Canned Chili, Canned Corn	-	Sack
Bag_SWAT	SWAT Duffel Bag	bag	18	1	0	Holster, JS-2000 Shotgun, M9 Pistol, Nightstick, Patrol Revolver, Bulletproof Vest - Police, Holster - Ankle, Holster - Shoulder	-	Sack
Bag_TarpFramepack_Large	Simple Framepack	bag	20	0	0	-	-	Sack
Bag_TarpFramepack_Small	Small Simple Framepack	bag	15	0	0	-	-	Sack
Bag_TarpSack	Tarp Sack	bag	20	0	0	-	-	Sack
Bag_TarpSlingBag	Tarp Sling Bag	bag	12	0	0	-	-	Sack
Bag_TennisBag	Duffel Bag	bag	18	0	0	Tennis Ball, Tennis Racket, Shorts - Sport, Sneakers, Long Shorts - Sport, T-shirt - Sports, Sport Pants, Gum	-	Sack
Bag_ToolBag	Duffel Bag	bag	18	2	0	Box of Nails, Funnel, Gardening Trowel, Leaf Rake, Measuring Tape, Rake, Ratchet Wrench, Rope	-	Sack
Bag_TrashBag	Garbage Bag	bag	20	12	0	Dryer Lint, Paperwork, T-shirt, Pants, Game Pieces, Bra - Strapless, Underpants, Bra	-	Sack
Bag_TreasureBag	Sack	bag	15	0	0	Diamond, Emerald, Gold Ring, Ruby, Sapphire, Trophy, Goblet, Gold Bangle	-	Sack
Bag_TrumpetCase	Trumpet Case	bag	1	14	0	Trumpet	-	Sack
Bag_ViolinCase	Violin Case	bag	1	11	0	Violin	-	Sack
Bag_WeaponBag	Duffel Bag	bag	18	7	0	Box of .357 Magnum Rounds, Box of .38 Special Rounds, Box of .44 Magnum Rounds, Box of .45 ACP Rounds, Box of 12g Rounds, Box of 5.56x45mm Rounds, Box of 7.62x51mm Rounds, Box of 9x19mm Rounds	-	Sack
Bag_WorkerBag	Duffel Bag	bag	18	5	0	Gum, Pencil, Lunchbox, Lunchbag, Pen - Black, Pen - Blue, Pen - Red, Sports Bottle	-	Sack
Briefcase	Briefcase	bag	8	37	0	Paperwork, Business Card, Gum, Magazine, Paperclip, Notepad, Pager, Paperback	-	Sack
Briefcase_Money	Briefcase	bag	8	13	0	Money, Stock Certificate, Money Bundle, Pouch	-	Sack
Cashbox	Cashbox	bag	2	13	0	Money	-	Sack
CigarBox	Cigar Box	bag	2	9	0	Cigar	-	Sack
CigarBox_Gaming	Cigar Box	bag	2	8	0	-	-	Sack
CigarBox_Keepsakes	Cigar Box	bag	2	10	0	-	-	Sack
CigarBox_Kids	Cigar Box	bag	2	5	0	-	-	Sack
CookieJar	Cookie Jar	bag	2	4	0	Chocolate Chip Cookie, Oatmeal Cookie, Chocolate Cookie, Raspberry Shortbread Cookie, Shortbread Cookie, Sugar Cookie	-	Sack
CookieJar_Bear	Cookie Jar - Teddy Bear	bag	2	5	0	Chocolate Chip Cookie, Oatmeal Cookie, Chocolate Cookie, Raspberry Shortbread Cookie, Shortbread Cookie, Sugar Cookie	-	Sack
Cooler	Cooler	bag	12	24	0	-	-	Sack
Cooler_Beer	Cooler	bag	12	2	0	Beer Bottle, Beer Can, Pack of Beer Bottles, Pack of Beer Cans, Cold Pack	-	Sack
Cooler_Meat	Cooler	bag	12	0	0	Beef Patty, Pack of Hotdogs, Cold Pack, Chicken Leg, Pork Chop, Sausage, Steak	-	Sack
Cooler_Seafood	Cooler	bag	12	0	0	Cold Pack, Mussels, Oysters, Shrimp, Crayfish, Salmon, Black Crappie, Blue Catfish	-	Sack
Cooler_Soda	Cooler	bag	12	0	0	Cola, Diet Cola, Ginger Ale, Orange Soda, Cold Pack, Bottle	-	Sack
DiceBag	Pouch	bag	1	32	0	Dice - 10-sided, Dice - 12-sided, Dice - 20-sided, Dice - 4-sided, Dice - 6-sided, Dice - 8-sided, Dice - Percentile, Dice	-	Sack
EmptySandbag	Sack	bag	15	16	1	-	-	Sack
FirstAidKit	First Aid Kit	bag	4	27	0	Alcohol Wipes, Bandage - Adhesive, Suture Needle, Bandage, Surgical Gloves, Cotton Balls, Forceps, Adhesive Tape	-	Sack
FirstAidKit_Camping	First Aid Kit - Camping	bag	2	15	0	Alcohol Wipes, Bandage - Adhesive, Tweezers, Bandage, Painkillers, Cotton Balls, Adhesive Tape, Antibiotics	-	Sack
FirstAidKit_Camping_New	First Aid Kit - Camping	bag	2	8	0	Alcohol Wipes, Bandage - Adhesive, Bandage, Painkillers, Tweezers, Water Purification Tablets, Adhesive Tape, Antibiotics	-	Sack
FirstAidKit_Military	First Aid Kit - Military	bag	2	7	0	Alcohol Wipes, Bandage, Painkillers, Surgical Gloves, Suture Needle, Cotton Balls, Forceps, Tweezers	-	Sack
FirstAidKit_New	First Aid Kit	bag	4	9	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Suture Needle, Forceps, Surgical Gloves, Adhesive Tape, Bottle of Disinfectant	-	Sack
FirstAidKit_NewPro	First Aid Kit	bag	4	11	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Painkillers, Suture Needle, Surgical Gloves, Adhesive Tape, Cotton Balls	-	Sack
FirstAidKit_Pro	?	bag	?	0	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Suture Needle, Forceps, Surgical Gloves, Adhesive Tape, Bottle of Disinfectant	-	Sack
Flightcase	Flight Case	bag	7	15	0	Acoustic Guitar, Electric Bass, Electric Guitar, Guitar Pick, Tuning Fork	-	Sack
Garbagebag	Garbage Bag	bag	20	18	0	-	-	Sack
GemBag	Pouch	bag	1	12	0	Diamond, Emerald, Ruby, Sapphire, Loupe	-	Sack
GroceryBag1	Plastic Bag	bag	8	2	0	Juice Box, Pickle, Processed Cheese, Cola, Diet Cola, Ginger Ale, Milk Carton, Yeast	-	Sack
GroceryBag2	Plastic Bag	bag	8	2	0	Pickle, Apple, Banana, Black Beans (Dried), Broccoli, Cabbage, Canned Carrots, Canned Corn	-	Sack
GroceryBag3	Plastic Bag	bag	8	2	0	Granola Bar, Gum, Milk Chocolate Bar, Licorice, Candy Cane, Candy Package, Canned Beans, Canned Chili	-	Sack
GroceryBag4	Plastic Bag	bag	8	2	0	Flour, Chocolate Chips, Cocoa Powder, White Sugar, Aluminum Foil, Baking Soda, Brown Sugar, Butter	-	Sack
GroceryBag5	Plastic Bag	bag	8	2	0	Ground Beef, Steak, Chips - Barbecue, Chips - Plain, Chips - Salt & Vinegar, Chips - Sour Cream & Onion, Cola, Diet Cola	-	Sack
GroceryBagGourmet	Plastic Bag	bag	8	0	0	Hot Sauce, Jar of Bell Peppers, Jar of Broccoli, Jar of Cabbage, Jar of Carrots, Jar of Eggplants, Jar of Leeks, Jar of Potatoes	-	Sack
Guitarcase	Guitar Case	bag	5	16	0	Acoustic Guitar, Electric Bass, Electric Guitar, Guitar Pick, Tuning Fork	-	Sack
HalloweenCandyBucket	Halloween Candy Bucket	bag	2	4	0	Candy Corn, Halloween Candy, Candied Apple, Granola Bar, Licorice, Butterchunkers Bar, Caramel Candies, Chocolate Candy	-	Sack
Handbag	Handbag	bag	6	40	0	Money, Credit Card, Gum, Key Ring, Lighter - Disposable, Matchbook, Note, Notepad	-	Sack
Hatbox	Hatbox	bag	1	3	0	-	-	Sack
HollowBook	Book	bag	2	14	0	Money, Credit Card, Photograph, Bottle of Whiskey, Butterfly Knife, Caffeine Pills, Locket, Rolling Papers	-	Sack
HollowBook_Handgun	Book	bag	2	4	0	SN38 Revolver, M9 Pistol, Magnum, Patrol Revolver, Military Medal	-	Sack
HollowBook_Kids	Book	bag	2	3	0	Money, Comic Book, Doodle, Photograph, Handheld Game Console, Magnifying Glass, Playing Cards, Chess Pieces	-	Sack
HollowBook_Prison	Book	bag	2	1	0	Money, Caffeine Pills, Painkillers, File, Notepad, Pager, Shiv, Cigarette Pack	-	Sack
HollowBook_Valuables	Book	bag	2	7	0	Money, Credit Card, Stock Certificate, Caffeine Pills, Flask, Poker Chips, SN38 Revolver, Diamond	-	Sack
HollowBook_Whiskey	Book	bag	2	6	0	Bottle of Whiskey	-	Sack
HollowFancyBook	Leatherbound Book	bag	2	0	0	-	-	Sack
HolsterShoulder	Holster - Shoulder	bag	1	24	0	-	-	Sack
Humidor	Humidor	bag	2	1	0	Cigar	-	Sack
JewelleryBox	Jewelry Box	bag	2	5	0	Choker, Necklace - Yin and Yang Symbol, Silver Bangle, Silver Belly Button Dangle, Silver Belly Button Ring, Silver Belly Button Stud, Silver Chain Bracelet, Silver Nose Ring	-	Sack
JewelleryBox_Fancy	Jewelry Box	bag	2	3	0	Dangly Earrings - Pearl, Earrings - Pearl, Old Photograph, Pearl Necklace, Gold Earrings - Large Looped, Gold Earrings - Medium Looped, Gold Earrings - Small Looped, Gold Earrings - Small Looped Top	-	Sack
KeyRing	Key Ring	bag	1	124	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Bass	Key Ring - Bass	bag	1	30	0	Key, Vehicle Key, Key Ring Bottle Opener, Compass, Handiknife, P38 Can Opener, Whistle	-	Sack
KeyRing_BlueFox	Key Ring - Blue Fox	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Bug	Key Ring - Bug	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_CarDealer	Key Ring	bag	1	1	0	Vehicle Key	-	Sack
KeyRing_Clover	Key Ring - Four-Leaf Clover	bag	1	13	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_EagleFlag	Key Ring - American Eagle	bag	1	26	0	Key, Vehicle Key, Key Ring Bottle Opener, Compass, Handiknife, P38 Can Opener, Whistle	-	Sack
KeyRing_EightBall	Key Ring - Eight Ball	bag	1	21	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Forged	Key Ring - Forged	bag	1	0	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Forged_Gold	Key Ring - Gold	bag	1	4	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Forged_Silver	Key Ring - Silver	bag	1	4	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Hotdog	Key Ring - Hotdog	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Kitty	Key Ring - Kitty	bag	1	21	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Large	Key Ring - Large	bag	2	0	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Nolans	Key Ring - Nolan's Used Cars	bag	1	2	0	Vehicle Key	-	Sack
KeyRing_Panther	Key Ring - Panther	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_PineTree	Key Ring - Pine Tree	bag	1	30	0	Key, Vehicle Key, Key Ring Bottle Opener, Compass, Handiknife, P38 Can Opener, Whistle	-	Sack
KeyRing_PrayingHands	Key Ring - Praying Hands	bag	1	25	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_RabbitFoot	Key Ring - Rabbit Foot	bag	1	13	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Racing12	Key Ring - 12	bag	1	0	0	-	-	Sack
KeyRing_Racing34	Key Ring - 34	bag	1	0	0	-	-	Sack
KeyRing_Racing58	Key Ring - 58	bag	1	0	0	-	-	Sack
KeyRing_RainbowStar	Key Ring - Rainbow Star	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_RubberDuck	Key Ring - Rubber Duck	bag	1	20	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_SecurityPass	Security Pass Key Ring	bag	1	12	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Sexy	Key Ring - Sexy	bag	1	16	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_Spiffos	Key Ring - Spiffos	bag	1	2	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_StinkyFace	Key Ring - Stinky Face	bag	1	3	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
KeyRing_WestMaple	Key Ring - West Maple Country Club	bag	1	3	0	Key, Vehicle Key, Key Ring Bottle Opener	-	Sack
Lunchbag	Lunchbag	bag	2	43	0	Apple, Banana, Fruit Muffin, Granola Bar, Muffin, Orange, Yogurt, Beef Jerky	-	Sack
Lunchbox	Lunchbox	bag	4	43	0	Apple, Banana, Fruit Muffin, Granola Bar, Muffin, Orange, Yogurt, Beef Jerky	-	Sack
Lunchbox2	Lunchbox	bag	4	43	0	Apple, Banana, Fruit Muffin, Granola Bar, Muffin, Orange, Yogurt, Beef Jerky	-	Sack
MakeupCase_Professional	Makeup Case	bag	4	4	0	Eye Makeup, Foundation Makeup, Hair Spray, Bottle of Hair Dye, Comb, Hair Gel, Lipstick, Perfume	-	Sack
PaperBag	Paper Bag	bag	2	36	0	-	-	Sack
Paperbag_Jays	Paper Bag	bag	2	10	0	Fried Chicken, Paper Napkins, Straw, Cornbread, Fries, Plastic Fork, Plastic Knife	-	Sack
Paperbag_Spiffos	Paper Bag	bag	2	11	0	Burger, Paper Napkins, Straw, Fried Onion Rings, Fries	-	Sack
Parcel_ExtraLarge	Parcel - Extra Large	bag	20	2	0	Barbecue, Desktop Computer, Fitness Contraption, Satellite Dish, Bulky Protective Container, Premium Technologies Ham Radio	-	Sack
Parcel_ExtraSmall	Parcel - Extra Small	bag	1	3	0	Long Socks, Socks, Bobber, Book, CD, Cheese, Cigar, Cologne	-	Sack
Parcel_Large	Parcel - Large	bag	10	2	0	Microwave, Mini Fridge, Premium Technologies Television	-	Sack
Parcel_Medium	Parcel - Medium	bag	5	3	0	Boxed Red Wine, Coffee X-Press, Red Wine, Toaster, White Wine	-	Sack
Parcel_Small	Parcel - Small	bag	2	2	0	Bath Towel, Bottle of Whiskey, CD Player, Camera, High-end Camera, Kettle, Premium Technologies Radio, Sweater	-	Sack
PencilCase	Pencil Case	bag	1	8	0	Pencil, Crayons, Eraser, Rubber Band, Pen - Black, Pen - Blue, Pen - Green, Pen - Red	-	Sack
PencilCase_Gaming	Pencil Case	bag	1	0	0	-	-	Sack
PhotoAlbum	Photo Album	bag	5	9	0	Photograph, Doodle, Handwritten Letter, Postcard, Old Photograph	-	Sack
PhotoAlbum_Old	Photo Album	bag	5	5	0	Old Photograph, Handwritten Letter, Postcard, Locket	-	Sack
PistolCase1	Handgun Case	bag	4	3	0	Box of 9x19mm Rounds, M9 Magazine, M9 Pistol, Gun Light, Holster - Shoulder	-	Sack
PistolCase2	Handgun Case	bag	4	1	0	Box of .45 ACP Rounds, M1911 Auto Magazine, M1911 Pistol, Gun Light, Holster - Shoulder	-	Sack
PistolCase3	Handgun Case	bag	4	1	0	Box of .44 Magnum Rounds, B-F Magazine, B-F Pistol, Gun Light, Holster - Shoulder	-	Sack
Plasticbag	Plastic Bag	bag	8	33	0	-	-	Sack
Plasticbag_Bags	Plastic Bag	bag	8	10	0	Plastic Bag	-	Sack
Plasticbag_Clothing	Plastic Bag	bag	8	0	0	Long Socks, Socks, T-shirt, Bra, Underpants, Boxers, Bra - Strapless, Briefs	-	Sack
Present_ExtraLarge	Gift - Extra Large	bag	20	0	0	Barbecue, Desktop Computer, Fitness Contraption, Satellite Dish, Bulky Protective Container, Premium Technologies Ham Radio	-	Sack
Present_ExtraSmall	Gift - Extra Small	bag	1	0	0	Long Socks, Socks, Bobber, Book, CD, Cheese, Cigar, Cologne	-	Sack
Present_Large	Gift - Large	bag	10	0	0	Microwave, Mini Fridge, Premium Technologies Television	-	Sack
Present_Medium	Gift - Medium	bag	5	0	0	Boxed Red Wine, Coffee X-Press, Red Wine, Toaster, White Wine	-	Sack
Present_Small	Gift - Small	bag	2	0	0	Bath Towel, Bottle of Whiskey, CD Player, Camera, High-end Camera, Kettle, Premium Technologies Radio, Sweater	-	Sack
ProduceBox_ExtraLarge	Produce Box - Extra Large	bag	20	0	0	-	-	Sack
ProduceBox_ExtraSmall	Produce Box - Extra Small	bag	1	1	2	-	-	Sack
ProduceBox_Large	Produce Box - Large	bag	10	1	3	-	-	Sack
ProduceBox_Medium	Produce Box - Medium	bag	5	11	14	-	-	Sack
ProduceBox_Small	Produce Box - Small	bag	2	1	4	-	-	Sack
Purse	Purse	bag	6	40	0	Money, Credit Card, Gum, Key Ring, Lighter - Disposable, Matchbook, Note, Notepad	-	Sack
RevolverCase1	Handgun Case	bag	4	2	0	Box of .38 Special Rounds, SN38 Revolver, Holster - Shoulder	-	Sack
RevolverCase2	Handgun Case	bag	4	2	0	Box of .357 Magnum Rounds, Patrol Revolver, Holster - Shoulder	-	Sack
RevolverCase3	Handgun Case	bag	4	2	0	Box of .44 Magnum Rounds, Magnum, x2 Scope, Holster - Shoulder	-	Sack
RifleCase1	Gun Case	bag	7	10	0	Box of 5.56x45mm Rounds, MSR700 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
RifleCase2	Gun Case	bag	7	11	0	Box of 7.62x51mm Rounds, MSR788 Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
RifleCase3	Gun Case	bag	7	4	0	Box of 7.62x51mm Rounds, M1A Magazine, M1A Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, x4 Scope, x8 Scope	-	Sack
RifleCase4	Gun Case	bag	7	0	0	Box of 5.56x45mm Rounds, M16 Magazine, M16 Assault Rifle, x2 Scope, Bullets Bandolier, Recoil Pad, Red Dot Sight, x4 Scope	-	Sack
SeedBag	Pouch	bag	1	18	0	Seed Packet - Basil, Seed Packet - Broccoli, Seed Packet - Cabbage, Seed Packet - Carrot, Seed Packet - Cauliflower, Seed Packet - Chamomile, Seed Packet - Chives, Seed Packet - Cilantro	-	Sack
SeedBag_Farming	Pouch	bag	1	8	0	Seed Packet - Corn, Seed Packet - Wheat, Seed Packet - Barley, Seed Packet - Flax, Seed Packet - Rye, Seed Packet - Sunflower, Seed Packet - Hops, Seed Packet - Onion	-	Sack
SewingKit	Sewing Kit	bag	2	18	0	Needle, Thread, Button, Thimble, Buckle, Measuring Tape, Awl, Scissors	-	Sack
Shoebox	Shoebox	bag	1	2	0	Shoes, Fancy Shoes, Strapped Shoes, Sneakers, Western Boots, Western Boots - Embroidered, Western Boots - Snakeskin	-	Sack
ShotgunCase1	Gun Case	bag	7	11	0	Box of 12g Rounds, JS-2000 Shotgun, Choke Tube - Full, Choke Tube - Improved, Shells Bandolier	-	Sack
ShotgunCase2	Gun Case	bag	7	11	0	Box of 12g Rounds, Double Barrel Shotgun, Shells Bandolier	-	Sack
Suitcase	Suitcase	bag	16	17	0	Long Socks, Socks, T-shirt, Bra, Underpants, Boxers, Bra - Strapless, Briefs	-	Sack
Tacklebox	Tacklebox	bag	6	37	0	Fishing Hook, Fishing Line, Bobber, Bottle Opener, Fillet Knife, Fishing I: "Dean's Good Fishing Guide", Flashlight - Angle Headed, Insect Repellent	-	Sack
TakeoutBox_Chinese	Chinese Takeout Container	bag	2	9	0	Fried Shrimp, Meat Dumpling, Meat Steam Bun, Shrimp Dumpling, Spring Roll, Tofu (Fried)	-	Sack
TakeoutBox_Styrofoam	Takeout Container	bag	2	9	0	Burger, Fries, Burrito, Fried Chicken, Hot Dog, Pizza Slice, Fried Onion Rings	-	Sack
Toolbox	Toolbox	bag	8	15	0	Flashlight - Angle Headed, Funnel, Marker - Black, Measuring Tape, Nails, Nuts and Bolts, Pliers, Rubber Hose	-	Sack
Toolbox_Farming	Toolbox	bag	8	8	0	Agriculture I: "Better Gardening", Fleshing Tool, Gardening Trowel, Hand Drill, Hand Fork, Kneepad, Leather Gloves, Measuring Tape	-	Sack
Toolbox_Fishing	Toolbox	bag	8	4	0	Fishing Hook, Fishing Line, Bobber, Bottle Opener, Fillet Knife, Fishing I: "Dean's Good Fishing Guide", Flashlight - Angle Headed, Insect Repellent	-	Sack
Toolbox_Gardening	Toolbox	bag	8	7	0	Agriculture I: "Better Gardening", Gardening Trowel, Hand Drill, Hand Fork, Kneepad, Leather Gloves, Measuring Tape, Pliers	-	Sack
Toolbox_Mechanic	Toolbox	bag	8	17	0	Electrical Wire, Funnel, Lug Wrench, Mechanics I: "Carzone's Repair Guide", Rag (Dirty), Ratchet Wrench, Rubber Hose, Screwdriver	-	Sack
Toolbox_Wooden	Wooden Toolbox	bag	6	1	0	Flashlight - Angle Headed, Funnel, Marker - Black, Measuring Tape, Nails, Nuts and Bolts, Pliers, Rubber Hose	-	Sack
ToolRoll_Fabric	Tool Roll - Fabric	bag	4	0	0	Kitchen Knife, Fillet Knife, Paring Knife, Sushi Knife, Whetstone, Basting Brush, Carving Fork, Grill Brush	-	Sack
ToolRoll_Leather	Tool Roll - Leather	bag	4	18	0	Flashlight - Angle Headed, Funnel, Marker - Black, Measuring Tape, Nails, Nuts and Bolts, Pliers, Rubber Hose	-	Sack
Tote	Tote Bag	bag	6	14	0	-	-	Sack
Tote_Bags	Tote Bag	bag	6	8	0	Plastic Bag	-	Sack
Tote_Clothing	Tote Bag	bag	6	0	0	Long Socks, Socks, T-shirt, Bra, Underpants, Boxers, Bra - Strapless, Briefs	-	Sack
Wallet	Wallet	bag	1	31	0	Money, Photograph, Business Card, Credit Card, Receipt, Parking Ticket, Speeding Ticket, Postcard	-	Sack
Wallet_Female	Wallet	bag	1	1	0	Money, ID Card, Credit Card, Photograph, Business Card, Receipt, Parking Ticket, Speeding Ticket	-	Sack
Wallet_Hide	Hide Wallet	bag	1	2	0	Money, Photograph, Business Card, Credit Card, Receipt, Parking Ticket, Speeding Ticket, Postcard	-	Sack
Wallet_Male	Wallet	bag	1	1	0	Money, ID Card, Credit Card, Photograph, Business Card, Receipt, Parking Ticket, Speeding Ticket	-	Sack
WheatSack	Sack	bag	15	9	0	Wheat Sheaf (Dried)	-	Sack
WheatSeedSack	Sack	bag	15	9	0	Wheat Seeds	-	Sack
GloveBox	Glove Box	vehicle	10	217	65	Paperback, Receipt, Notepad, Magazine, Pencil, Tissue, Battery, Bandage - Adhesive	Container_GloveCompartment	-
SeatFrontLeft	Driver's Seat	vehicle	20	217	7	Notepad, Pencil, Magazine, Tissue, Adhesive Tape, Cigarette Pack, Matchbook, Paper Napkins	Container_Carseat	-
SeatFrontRight	Front Right Seat	vehicle	20	211	58	Magazine, Tissue, Notepad, Pencil, Paper Napkins, Cigarette Pack, Adhesive Tape, Matchbook	Container_Carseat	-
SeatMiddleLeft	Middle Left Seat	vehicle	20	0	0	-	Container_Carseat	-
SeatMiddleRight	Middle Right Seat	vehicle	20	0	0	-	Container_Carseat	-
SeatRearLeft	Rear Left Seat	vehicle	20	113	21	Magazine, Tissue, Notepad, Pencil, Plastic Bag, Paper Napkins, Adhesive Tape, Matchbook	Container_Carseat	-
SeatRearRight	Rear Right Seat	vehicle	20	113	21	Magazine, Tissue, Notepad, Pencil, Plastic Bag, Paper Napkins, Adhesive Tape, Matchbook	Container_Carseat	-
TrailerAnimalEggs	Egg Container	vehicle	10	0	0	-	-	-
TrailerAnimalFood	Food Container	vehicle	20	0	0	-	-	-
TrailerTrunk	Trailer	vehicle	100	138	19	Plank, Brick, Rope, Rubber Hose, Wire, Sack, Box of Nails, Rag	Container_TruckBed	-
TruckBed	Trunk	vehicle	130	217	72	Plank, Brick, Rope, Gas Can (empty), Rubber Hose, Sack, Plastic Bag, Wire	Container_TruckBed	-
TruckBedOpen	Trunk	vehicle	110	165	38	Plank, Brick, Rope, Rubber Hose, Sack, Gas Can (empty), Wire, Rag	Container_TruckBed	-
body	Corpse	body	12	4	0	Alcohol Wipes, Bandage, Bandage - Adhesive, Painkillers, Claw Hammer, Cooking Recipe Clipping, Nails, Fillet Knife	-	Corpse
inventoryfemale	Corpse	body	12	1	0	Wallet, ID Card, Comb, Credit Card, Diary, Earbuds, Eye Makeup, Foundation Makeup	Container_DeadPerson_FemaleZombie	Corpse
inventorymale	Corpse	body	12	1	0	Wallet, ID Card, Comb, Credit Card, Diary, Earbuds, Handwritten Letter, Locket	Container_DeadPerson_MaleZombie	Corpse
Outfit_Agent	Corpse	body	12	1	0	Badge, Notepad, Pen - Black, Lighter - Disposable, Cigarette Pack	-	Corpse
Outfit_AirportSecurityTarmac	Corpse	body	12	1	0	Badge, Notepad, Pen - Black, Lighter - Disposable	-	Corpse
Outfit_AmbulanceDriver	Corpse	body	12	1	0	Notepad, Pencil, Penlight, Surgical Gloves, Surgical Mask, Flashlight, Pager, First Aid Kit	-	Corpse
Outfit_ArmyCamoDesert	Corpse	body	12	1	0	5.56x45mm Round, Compass, Dehydrated Meat Stick, Notepad, P38 Can Opener, Pencil, Beef Jerky, M16 Magazine	-	Corpse
Outfit_ArmyCamoGreen	Corpse	body	12	1	0	5.56x45mm Round, Compass, Dehydrated Meat Stick, Notepad, P38 Can Opener, Pencil, Beef Jerky, M16 Magazine	-	Corpse
Outfit_Backpacker	Corpse	body	12	1	0	Passport, Plastic Spork, Lighter - Disposable, Rolled Cigarette, Rolling Papers, Sports Bottle, Magazine: Legalize It!, Compass	-	Corpse
Outfit_Bandit	Corpse	body	12	1	0	Hunting Knife, Rolled Cigarette, Switchblade, Firecrackers, Rolling Papers, Chewing Tobacco, Flask, Pouch of Tobacco	-	Corpse
Outfit_Bandit_Early	Corpse	body	12	1	0	Hunting Knife, Rolled Cigarette, Switchblade, Firecrackers, Rolling Papers, Chewing Tobacco, Flask, Pouch of Tobacco	-	Corpse
Outfit_Bandit_Late	Corpse	body	12	1	0	Hunting Knife, Rolled Cigarette, Switchblade, Firecrackers, Rolling Papers, Chewing Tobacco, Flask, Pouch of Tobacco	-	Corpse
Outfit_Bandit_Mid	Corpse	body	12	1	0	Hunting Knife, Rolled Cigarette, Switchblade, Firecrackers, Rolling Papers, Chewing Tobacco, Flask, Pouch of Tobacco	-	Corpse
Outfit_BaseballPlayer_KY	Corpse	body	12	1	0	Gum, Chewing Tobacco	-	Corpse
Outfit_BaseballPlayer_Rangers	Corpse	body	12	1	0	Gum, Chewing Tobacco	-	Corpse
Outfit_BaseballPlayer_Z	Corpse	body	12	1	0	Gum, Chewing Tobacco	-	Corpse
Outfit_Bathrobe	Corpse	body	12	1	0	-	-	Corpse
Outfit_Biker	Corpse	body	12	1	0	Beer Can, Hunting Knife, Switchblade, Cigarillo, Rolled Cigarette, SN38 Revolver, Chewing Tobacco, Cigarette Pack	-	Corpse
Outfit_Bob	Corpse	body	12	1	0	-	-	Corpse
Outfit_BountyHunter	Corpse	body	12	1	0	Badge, Zip Ties, Magazine, Notepad, Lighter - Disposable, Cigarette Pack, SN38 Revolver	-	Corpse
Outfit_Camper	Corpse	body	12	1	0	Compass, Insect Repellent, Plastic Spork, Dehydrated Meat Stick, Granola Bar, Whistle, Magnesium Firestarter, Water Purification Tablets	-	Corpse
Outfit_Classy	Corpse	body	12	1	0	Money, Credit Card, Business Card, Stock Certificate, Fountain Pen, Lighter, Pocketwatch, Cigar	-	Corpse
Outfit_ClubGoer	Corpse	body	12	1	0	Caffeine Pills, Gum, Gummy Bears, Gummy Fish Candy, Gummy Worms, Lollipop, Whistle, Paperback	-	Corpse
Outfit_ConstructionWorker	Corpse	body	12	1	0	Nails, Screws, Duct Tape, Notepad, Pencil, Lighter - Disposable, Chewing Tobacco, Cigarette Pack	-	Corpse
Outfit_Cook_Generic	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Lighter - Disposable, Cigarette Pack	-	Corpse
Outfit_Cook_Spiffos	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Lighter - Disposable, Cigarette Pack	-	Corpse
Outfit_CostumeWildWestBoss	Corpse	body	12	1	0	Toy Badge	-	Corpse
Outfit_CostumeWildWestLawman	Corpse	body	12	1	0	Toy Badge	-	Corpse
Outfit_CostumeWildWestMayor	Corpse	body	12	1	0	Toy Badge	-	Corpse
Outfit_CostumeWildWestOutlaw	Corpse	body	12	1	0	-	-	Corpse
Outfit_Cultist	Corpse	body	12	1	0	-	-	Corpse
Outfit_Dean	Corpse	body	12	1	0	ID Card, Compass, Insect Repellent, Magnesium Firestarter, P38 Can Opener, Water Purification Tablets	-	Corpse
Outfit_Detective	Corpse	body	12	1	0	Badge, Notepad, Pager, Pencil, SN38 Revolver, Lighter - Disposable, Cigarette Pack	-	Corpse
Outfit_Doctor	Corpse	body	12	1	0	Pager, Penlight, Surgical Gloves, Surgical Mask, First Aid Kit	-	Corpse
Outfit_Duke	Corpse	body	12	1	0	Camera Film, Credit Card, Electrical Wire, ID Card, Pager, Battery	-	Corpse
Outfit_Evacuee	Corpse	body	12	1	0	Photograph, Diary, Dog Chew Toy, Doll, Doodle, Handwritten Letter, Leash, Mouse Toy	-	Corpse
Outfit_ExterminatorSuited	Corpse	body	12	1	0	Mouse Trap, Duct Tape, Garbage Bag, Rat Poison, Rubber Gloves, Lighter - Disposable, Cigarette Pack, Respirator Filters	-	Corpse
Outfit_Farmer	Corpse	body	12	1	0	Chewing Tobacco, Magazine: American Homesteading, Magazine: Blooms and Blossoms, Magazine: Cropping for Cash, Magazine: Growing Your Own, Magazine: Kentucky Farmer - June 1993, Magazine: Kentucky Farmer - May 1993, Magazine: The Farmers Guide	-	Corpse
Outfit_FiremanFullSuit	Corpse	body	12	1	0	Badge, First Aid Kit	-	Corpse
Outfit_FiremanStripper	Corpse	body	12	1	0	Money	-	Corpse
Outfit_Fisherman	Corpse	body	12	1	0	Fishing Hook, Fishing Line, Insect Repellent, Jig Lure, Minnow Lure, Pliers, Chewing Tobacco, Fillet Knife	-	Corpse
Outfit_Foreman	Corpse	body	12	1	0	Calculator, Clipboard, Graph Paper, Notepad, Pencil, Pen - Blue, Lighter - Disposable, Chewing Tobacco	-	Corpse
Outfit_Fossoil	Corpse	body	12	1	0	Rag, Rag (Dirty), Lighter - Disposable, Chewing Tobacco, Cigarette Pack	-	Corpse
Outfit_Frank_Hemingway	Corpse	body	12	1	0	Credit Card, ID Card, Pager	-	Corpse
Outfit_Gas2Go	Corpse	body	12	1	0	Rag, Rag (Dirty), Lighter - Disposable, Chewing Tobacco, Cigarette Pack	-	Corpse
Outfit_Gaudy	Corpse	body	12	1	0	Credit Card, Money, Business Card, Paperback, Cigar, Lighter	-	Corpse
Outfit_Ghillie	Corpse	body	12	1	0	5.56x45mm Round, Compass, Dehydrated Meat Stick, Notepad, P38 Can Opener, Pencil, Beef Jerky, M16 Magazine	-	Corpse
Outfit_Golfer	Corpse	body	12	1	0	Golf Ball, Golf Tee, Notepad, Pencil, Pager, Magazine, Paperback	-	Corpse
Outfit_Goth	Corpse	body	12	1	0	Candy Corn, Caramel Candies, Eye Makeup, Foundation Makeup, Lipstick, Lollipop, Rubber Spider, Diary	-	Corpse
Outfit_Groucho	Corpse	body	12	1	0	Cigar, Magazine	-	Corpse
Outfit_Grunge	Corpse	body	12	1	0	Beer Can, Painkillers, Rolled Cigarette, Lighter - Disposable, Pouch of Tobacco, Rolling Papers, Flask, Firecrackers	-	Corpse
Outfit_GuitarGuy	Corpse	body	12	1	0	Guitar Pick, Beer Can, Lighter - Disposable, Cigarette Pack, Flask, Magazine, Paperback	-	Corpse
Outfit_Hobbo	Corpse	body	12	1	0	Bottle of Whiskey (empty), Rolled Cigarette, Rolling Papers, Toilet Paper, Flask, Pouch of Tobacco, Pocket Knife	-	Corpse
Outfit_Hobbyist	Corpse	body	12	1	0	Pouch	-	Corpse
Outfit_HonorStudent	Corpse	body	12	1	0	Pen - Blue, Pen - Red, Tissue, Calculator, Geometry Compass, Fountain Pen	-	Corpse
Outfit_HospitalPatient	Corpse	body	12	1	0	-	-	Corpse
Outfit_HospitalPatientBathrobe	Corpse	body	12	1	0	-	-	Corpse
Outfit_Hunter	Corpse	body	12	1	0	.30-30 Round, 12g Round, 5.56x45mm Round, 7.62x51mm Round, Chewing Tobacco, Compass, Dehydrated Meat Stick, Insect Repellent	-	Corpse
Outfit_Inmate	Corpse	body	12	1	0	Rolled Cigarette, Glass Shiv, Shiv, Simple Metal Knife	-	Corpse
Outfit_InmateKhaki	Corpse	body	12	1	0	Rolled Cigarette, Glass Shiv, Shiv, Simple Metal Knife	-	Corpse
Outfit_Jackie_Jaye	Corpse	body	12	1	0	Credit Card, ID Card, Lipstick, Pager	-	Corpse
Outfit_Joan	Corpse	body	12	1	0	Credit Card, ID Card, Pager	-	Corpse
Outfit_John	Corpse	body	12	1	0	ID Card	-	Corpse
Outfit_Judge_Matt_Hass	Corpse	body	12	1	0	Photograph of Judge Matt Hass (Autographed), ID Card, Security Pass Key Ring	-	Corpse
Outfit_Kate	Corpse	body	12	1	0	-	-	Corpse
Outfit_Kirsty_Kormick	Corpse	body	12	1	0	Credit Card, ID Card, Pager	-	Corpse
Outfit_Mayor_West_point	Corpse	body	12	1	0	Business Card, Credit Card, ID Card, Cigar	-	Corpse
Outfit_McCoys	Corpse	body	12	1	0	Nails, Notepad, Pencil, Duct Tape, Screws, Chewing Tobacco, Cigarette Pack, Whetstone	-	Corpse
Outfit_Mechanic	Corpse	body	12	1	0	Notepad, Pencil, Rag, Rag (Dirty), Screws, Chewing Tobacco, Cigarette Pack, Magazine	-	Corpse
Outfit_MetalWorker	Corpse	body	12	1	0	Notepad, Pencil, Rag, Rag (Dirty), Screws, Chewing Tobacco, Cigarette Pack, Handiknife	-	Corpse
Outfit_Mob	Corpse	body	12	1	0	Ice Pick, Money Bundle, Suspicious Package	-	Corpse
Outfit_Naked	Corpse	body	12	1	0	-	-	Corpse
Outfit_NakedVeil	Corpse	body	12	1	0	-	-	Corpse
Outfit_Nolan	Corpse	body	12	1	0	Business Card, Flier, ID Card, Key Ring - Nolan's Used Cars, Pager	-	Corpse
Outfit_Nurse	Corpse	body	12	1	0	Penlight, Surgical Gloves, Surgical Mask, Flashlight, Pager, First Aid Kit	-	Corpse
Outfit_OfficeWorker	Corpse	body	12	1	0	Credit Card, Business Card, Pencil, Eraser, Pen - Black, Pen - Blue, Pen - Red, Calculator	-	Corpse
Outfit_OfficeWorkerSkirt	Corpse	body	12	1	0	Credit Card, Business Card, Pencil, Eraser, Pen - Black, Pen - Blue, Pen - Red, Calculator	-	Corpse
Outfit_Pharmacist	Corpse	body	12	1	0	Surgical Gloves, Surgical Mask, Pager, First Aid Kit	-	Corpse
Outfit_PokerDealer	Corpse	body	12	1	0	Money, Playing Cards, Poker Chips	-	Corpse
Outfit_Police	Corpse	body	12	1	0	Badge, 9x19mm Round, Notepad, Zip Ties, M9 Magazine, Pen - Black, Whistle, Box of 9x19mm Rounds	-	Corpse
Outfit_PoliceState	Corpse	body	12	1	0	Badge, .38 Special Round, Notepad, Zip Ties, Pen - Black, .357 Magnum Round, .44 Magnum Round, .45 ACP Round	-	Corpse
Outfit_PoliceStripper	Corpse	body	12	1	0	Money	-	Corpse
Outfit_Priest	Corpse	body	12	1	0	Necklace - Crucifix	-	Corpse
Outfit_PrisonGuard	Corpse	body	12	1	0	Badge, Notepad, Zip Ties, Pen - Black, Whistle	-	Corpse
Outfit_PrivateMilitia	Corpse	body	12	1	0	12g Round, 5.56x45mm Round, 7.62x51mm Round, Dehydrated Meat Stick, Zip Ties, Beef Jerky, Butterfly Knife, Switchblade	-	Corpse
Outfit_Punk	Corpse	body	12	1	0	Beer Bottle, Beer Can, Bottle of Whiskey, Switchblade, Bottle of Hair Dye, Hair Spray, Cigarillo, Comb	-	Corpse
Outfit_Raider	Corpse	body	12	1	0	Rolled Cigarette, Hunting Knife, Switchblade, Whistle, Chewing Tobacco, Bottle of Whiskey	-	Corpse
Outfit_Ranger	Corpse	body	12	1	0	Badge, 12g Round, Compass, Notebook, Pen - Black, Whistle, Box of 12g Rounds, Chewing Tobacco	-	Corpse
Outfit_Redneck	Corpse	body	12	1	0	Beer Can, Chewing Tobacco, Dehydrated Meat Stick, Beef Jerky, Cigarette Pack, Bottle of Whiskey, Butterfly Knife, Handiknife	-	Corpse
Outfit_Rev_Peter_Watts	Corpse	body	12	1	0	ID Card, Key Ring - Praying Hands	-	Corpse
Outfit_Rocker	Corpse	body	12	1	0	Beer Can, Lighter - Disposable, Cigarette Pack, Flask, Pouch of Tobacco, Rolling Papers, Firecrackers, Magazine: HottieZ	-	Corpse
Outfit_Sanitation	Corpse	body	12	1	0	Garbage Bag, Dust Mask, Rubber Gloves, Lighter - Disposable, Cigarette Pack, Flask	-	Corpse
Outfit_Sir_Twiggy	Corpse	body	12	1	0	ID Card	-	Corpse
Outfit_Stripper	Corpse	body	12	1	0	Money	-	Corpse
Outfit_StripperBlack	Corpse	body	12	1	0	Money	-	Corpse
Outfit_StripperNaked	Corpse	body	12	1	0	Money	-	Corpse
Outfit_StripperPink	Corpse	body	12	1	0	Money	-	Corpse
Outfit_Student	Corpse	body	12	1	0	Calculator, Notebook, Pencil, CD, Comic Book, Handheld Game Console, CD Player, Earbuds	-	Corpse
Outfit_Survivalist	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist02	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist02_Late	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist02_Mid	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist03	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist03_Late	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist03_Mid	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist04	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist04_Late	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist04_Mid	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist05	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist05_Late	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist05_Mid	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist_Late	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Survivalist_Mid	Corpse	body	12	1	0	Photograph, Butterfly Knife, Chewing Tobacco, Compass, Dehydrated Meat Stick, Diary, Handiknife, Journal	-	Corpse
Outfit_Teacher	Corpse	body	12	1	0	Calculator, Notebook, Pencil, Pen - Blue, Pen - Red, Cigarette Pack, Bottle of Whiskey	-	Corpse
Outfit_ThunderGas	Corpse	body	12	1	0	Rag, Rag (Dirty), Cigarette Pack	-	Corpse
Outfit_Tourist	Corpse	body	12	1	0	Disposable Camera, Money, Brochure, Camera, Passport, High-end Camera, Cocktail Umbrella, Handiknife	-	Corpse
Outfit_Trader	Corpse	body	12	1	0	Money, Stock Certificate, Credit Card, Pager, Business Card, Index Card, Pencil, Eraser	-	Corpse
Outfit_Trucker	Corpse	body	12	1	0	Caffeine Pills, Chewing Tobacco, Lighter - Disposable, Magazine: HottieZ, Cigarette Pack, Cigarillo, Flask, Harmonica	-	Corpse
Outfit_Varsity	Corpse	body	12	1	0	CD, Comic Book, CD Player, Earbuds, Cigarette Pack, Headphones, Bottle of Whiskey, Magazine: HottieZ	-	Corpse
Outfit_Waiter_Classy	Corpse	body	12	1	0	Money, Dish Towel, Notepad, Receipt, Pen - Blue, Corkscrew, Pocket Knife	-	Corpse
Outfit_Waiter_Diner	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_Market	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_PileOCrepe	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_PizzaWhirled	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_Restaurant	Corpse	body	12	1	0	Money, Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_Spiffo	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Waiter_TacoDelPancho	Corpse	body	12	1	0	Dish Towel, Notepad, Pencil, Receipt, Cigarette Pack	-	Corpse
Outfit_Woodcut	Corpse	body	12	1	0	Nails, ID Card, Key Ring - Sexy	-	Corpse
floor	Ground	floor	50	0	0	-	Container_Floor	Sack
all	?	other	-	2	2	Orange Soda, Cola, Diet Cola, Ginger Ale, Bottle	-	-
brazier	Brazier	other	10	1	0	Tin Can, Twigs, Tree Branch, Log, Smashed Bottle, Broken Glass	Container_Campfire	-
campfire	Campfire	other	10	1	0	Tin Can, Twigs, Tree Branch, Log, Smashed Bottle, Broken Glass	Container_Campfire	-
composter	Composter	other	30	0	0	-	Container_Composter	-
corn	Corn	other	-	0	0	-	Container_Plant	-
doghouse	Doghouse	other	20	3	0	Water Dish, Animal Bone, Dog Chew Toy, Pillow, Dog Tag, Large Animal Bone, Leash, Rubber Duck	-	-
fruitbusha	Plant	other	-	0	0	-	Container_Plant	-
fruitbushb	Plant	other	-	0	0	-	Container_Plant	-
fruitbushc	Plant	other	-	0	0	-	Container_Plant	-
fruitbushd	Plant	other	-	0	0	-	Container_Plant	-
fruitbushe	Plant	other	-	0	0	-	Container_Plant	-
other	?	other	-	10	6	Tin Can, Receipt, Beer Can, Paperback, Cola, Diet Cola, Ginger Ale, Cockroach	-	-
scarecrow	?	other	?	0	0	-	Item_Scarecrow	-
shelter	Shelter	other	20	1	0	Rag (Dirty), Pop Can, Beer Bottle, Beer Can, Bottle of Whiskey (empty), Copper Scrap, Garbage Bag, Old Newspaper	Item_Tent2_Open	-
tent	Tent	other	50	3	0	Paperback, Compass, Magazine, Book, Cheap Sleeping Bag, Sleeping Bag, Insect Repellent, Matchbox	Item_Tent2_Open	-
trough	Trough	other	50	1	0	-	-	-
```

## CLONE-Z mapping

CLONE-Z rolls every container from `ItemDatabase.RollLoot` (min–max draws from a weighted table with an empty-draw weight) into an effectively unlimited `Inventory(999f)`; search times come from `CloneZConfig` (Desk/MedCabinet 1.4 s, ToolBench 3.2 s, Corpse 1.6 s, others 2.3 s). The seed tables are `ContentDatabase.LootSeedSql` (curated items) plus `DistributeCatalogueLoot`, which spreads every non-curated item of `Assets/Resources/pz_items.txt` across tables by category (bulk counts below are items per category), after which `LootRebalanceSql` raises each empty weight to at least 12% of the table's entry weight; the content DB is the source of truth once seeded. Today the world spawns only the start-house Crate, zombie corpses and the player's dropped sacks. "Missing" lists the PZ items with the highest share across the mapped PZ containers (each mapped type weighted equally; all `Outfit_*` together count as one) that CLONE-Z does not put in that table, with where CLONE-Z routes them now (→ tables, or *not in catalogue* when `pz_items.txt` lacks the id) — suggestions only.

| CLONE-Z type | Stands in for (PZ ids) | CLONE-Z loot today (seed) | Notable PZ loot it is missing (suggestions) |
|---|---|---|---|
| **Crate** | `cardboardbox`, `crate`, `garagestorage`, `militarycrate`, `plankstash`, `smallbox`, `smallcrate`, `SurvivorCrate` | 2–4 rolls, empty weight 6; 22 curated: 9mm Rounds, Baseball Bat, Battery, Box of Nails, Cabbage Seeds, Can Opener, Canned Beans, Canned Soup, Crackers, Crowbar, Duct Tape, Empty Bottle, Garden Trowel, Leather Gloves, Leather Jacket, Potato Seeds, Rain Coat, Rope, Shotgun Shells, Tomato Seeds, Wooden Plank, Work Trousers; + bulk PZ catalogue: Material 1851, Weapon 381, Food (shelf-stable) 273 | Paperwork 1.6% (→ not in catalogue), Produce Box - Medium 0.9% (→ not in catalogue), M9 Pistol 0.8% (→ GunLocker/Locker), Small Protective Military Case 0.8% (→ not in catalogue), Produce Box - Small 0.8% (→ not in catalogue), JS-2000 Shotgun 0.7% (→ GunLocker/Locker) |
| **Locker** | `locker` | 2–4 rolls, empty weight 8; 21 curated: 9mm Rounds, Bandage, Baseball Bat, Baseball Cap, Battery, Empty Bottle, Hoodie, Jeans, Kitchen Knife, Leather Boots, M9 Pistol, Padded Jacket, Painkillers, Potato Chips, Ripped Sheets, Sneakers, Soda Can, T-Shirt, Thread, Wool Gloves, Wool Hat; + bulk PZ catalogue: Clothing 641, Firearm 22 | Gum 2.0% (→ Pantry/Crate/Fridge), Box of 5.56x45mm Rounds 1.6% (→ Shelf/Crate/ToolBench), Bulletproof Vest - Police 1.1% (→ Shelf/Crate/ToolBench), Combination Padlock 1.0% (→ not in catalogue), Money 1.0% (→ Shelf/Crate/ToolBench), Holster 0.8% (→ Shelf/Crate/ToolBench) |
| **Fridge** | `restaurantdisplay`, `displaycasebutcher`, `freezer`, `fridge`, `icecream`, `vendingpop` | 2–4 rolls, empty weight 4; 15 curated: Apple, Bread, Cabbage, Canned Beans, Canned Soup, Carrot, Cereal, Empty Bottle, Juice Box, Potato, Raw Chicken, Raw Meat, Soda Can, Tomato, Water Bottle; + bulk PZ catalogue: Food (perishable) 431, Food (shelf-stable) 273, Drink 28 | Cold Pack 0.5% (→ Shelf/Crate/ToolBench), Tin Can 0.2% (→ Shelf/Crate/ToolBench), Rag (Dirty) 0.2% (→ Shelf/Crate/ToolBench), Bottle of Disinfectant 0.2% (→ Shelf/Crate/ToolBench), Rag 0.2% (→ Shelf/Crate/ToolBench), Lunchbag 0.1% (→ not in catalogue) |
| **MedCabinet** | `medicine`, `MedicalBox` | 1–3 rolls, empty weight 5; 7 curated: Antibiotics, Bandage, Beta Blockers, Disinfectant, First Aid Kit, Painkillers, Ripped Sheets; + bulk PZ catalogue: none | Bottle of Whiskey 5.5% (→ Pantry/Crate/Fridge), Bandage - Adhesive 4.7% (→ Shelf/Crate/ToolBench), Alcohol Wipes 4.1% (→ Shelf/Crate/ToolBench), Rag 3.5% (→ Shelf/Crate/ToolBench), Suture Needle 3.4% (→ Shelf/Crate/ToolBench), Tweezers 3.2% (→ Shelf/Crate/ToolBench) |
| **Desk** | `desk`, `filingcabinet`, `officedrawers`, `schooldesk`, `sidetable` | 1–3 rolls, empty weight 9; 12 curated: 9mm Rounds, Bandage, Battery, Beta Blockers, Can Opener, Crackers, Duct Tape, Kitchen Knife, Painkillers, Potato Chips, Sewing Needle, Thread; + bulk PZ catalogue: none | Paperwork 13.3% (→ not in catalogue), Sheet of Paper 10.5% (→ not in catalogue), Note 7.1% (→ not in catalogue), Vehicle Key 3.0% (→ not in catalogue), Notebook 2.4% (→ not in catalogue), Pencil 2.4% (→ Crate/GunLocker/ToolBench) |
| **ToolBench** | `toolcabinet`, `ToolsBox` | 2–4 rolls, empty weight 5; 16 curated: Battery, Box of Nails, Can Opener, Carrot Seeds, Crowbar, Duct Tape, Fire Axe, Frying Pan, Garden Trowel, Hammer, Potato Seeds, Rope, Shotgun, Shotgun Shells, Watering Can, Wooden Plank; + bulk PZ catalogue: Material 1851, Weapon 381 | Safety Goggles 0.4% (→ Wardrobe/Locker/Corpse), Dust Mask 0.3% (→ Wardrobe/Locker/Corpse), Ear Protectors 0.3% (→ Wardrobe/Locker/Corpse), Tool Roll - Leather 0.3% (→ not in catalogue), Half Mask Respirator 0.2% (→ Wardrobe/Locker/Corpse), Hard Hat 0.1% (→ Wardrobe/Locker/Corpse) |
| **Wardrobe** | `clothingrack`, `dresser`, `mannequin`, `wardrobe`, `clothingdryer`, `clothingwasher`, `clothingdryerbasic` | 2–4 rolls, empty weight 6; 8 curated: Hoodie, Jeans, Leather Boots, Padded Jacket, Sewing Needle, T-Shirt, Thread, Wool Hat; + bulk PZ catalogue: Clothing 641 | Dish Towel 14.5% (→ Shelf/Crate/ToolBench), Bath Towel 8.6% (→ Shelf/Crate/ToolBench), Sheet 7.3% (→ Shelf/Crate/ToolBench), Rag (Dirty) 4.4% (→ Shelf/Crate/ToolBench), Rag 4.2% (→ Shelf/Crate/ToolBench), Money 1.5% (→ Shelf/Crate/ToolBench) |
| **Pantry** | `counter`, `cupboard`, `dishescabinet`, `displaycasebakery`, `grocerstand`, `overhead`, `vendingsnack`, `BoozeBox`, `FoodBox` | 2–4 rolls, empty weight 5; 6 curated: Can Opener, Canned Beans, Canned Soup, Cereal, Crackers, Potato Chips; + bulk PZ catalogue: Food (perishable) 431, Food (shelf-stable) 273, Drink 28 | Wine Glass 2.9% (→ Shelf/Crate/ToolBench), Water Bottle 1.0% (→ Shelf/Crate/ToolBench), Paper Napkins 0.8% (→ Shelf/Crate/ToolBench), Bottle Opener 0.6% (→ Shelf/Crate/ToolBench), Corkscrew 0.6% (→ Shelf/Crate/ToolBench), Plastic Bag 0.5% (→ not in catalogue) |
| **GunLocker** | `militarylocker`, `BombBox`, `GunBox`, `ShotgunBox` | 1–3 rolls, empty weight 7; 4 curated: 9mm Rounds, M9 Pistol, Shotgun, Shotgun Shells; + bulk PZ catalogue: Weapon 381, Firearm 22 | Box of 12g Rounds 23.8% (→ Shelf/Crate/ToolBench), Box of 5.56x45mm Rounds 4.5% (→ Shelf/Crate/ToolBench), Box of 7.62x51mm Rounds 3.5% (→ Shelf/Crate/ToolBench), Box of 9x19mm Rounds 3.5% (→ Shelf/Crate/ToolBench), Box of .357 Magnum Rounds 3.3% (→ Shelf/Crate/ToolBench), Box of .38 Special Rounds 3.3% (→ Shelf/Crate/ToolBench) |
| **Shelf** | `displaycase`, `metal_shelves`, `shelves`, `shelvesmag` | 2–4 rolls, empty weight 6; 5 curated: Battery, Box of Nails, Duct Tape, Rope, Wooden Plank; + bulk PZ catalogue: Material 1851 | Comic Book 3.3% (→ not in catalogue), Magazine 2.7% (→ not in catalogue), Newspaper 1.9% (→ not in catalogue), Brochure 1.5% (→ not in catalogue), Flier 1.4% (→ not in catalogue), Magazine: TV Monthly 1.3% (→ not in catalogue) |
| **Corpse** | `body`, `inventoryfemale`, `inventorymale`, 125 × `Outfit_*` | 1–2 rolls, empty weight 34; 16 curated: 9mm Rounds, Bandage, Baseball Cap, Battery, Crackers, Jeans, Kitchen Knife, Leather Boots, Painkillers, Potato Chips, Ripped Sheets, Shotgun Shells, Sneakers, Soda Can, T-Shirt, Wool Gloves; + bulk PZ catalogue: Clothing 641 | Wallet 14.1% (→ not in catalogue), ID Card 5.6% (→ not in catalogue), Money 2.0% (→ Shelf/Crate/ToolBench), Alcohol Wipes 1.5% (→ Shelf/Crate/ToolBench), Bandage - Adhesive 1.5% (→ Shelf/Crate/ToolBench), Pencil 1.5% (→ Crate/GunLocker/ToolBench) |
| **Sack** | `floor` + all 319 bag/item containers (`Bag_*`, wallets, cases, boxes, …) | No loot table — a collider-less pickup holding one dropped stack (`LootContainer.SpawnBag`), searched instantly. | PZ bags are real containers: capacity (e.g. Small Backpack 15, Duffel Bag 18) plus `WeightReduction`, an equip slot and their own loot tables — see ITEMS_ENHANCEMENTS.md §D. |

## Container details

Per container: capacity and its source, behaviour from the engine/Lua, the most common map rooms that define it (room-instance counts from the B42 lotheaders; `all` = generic fallback), the feeding procedural lists (by summed selection weight, rooms using each in parentheses, `*` = forced list), and the top loot with its share of expected spawns. Bags list the rooms that can spawn the bag itself and the bag's own contents; vehicle parts list the vehicle distribution tables that fill them.

**— furniture —**

### clothingrack — Clothing Rack
- furniture · capacity 50 · icon - · CLONE-Z: Wardrobe
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Accepts only `Clothing` or items tagged FitsClothingRack (`ItemContainer.isInvalidClothingRackItem`).
- Sprites: 23 sprites, 23 used on the map in 130 of 4,065 cells; capacity by map use: 50 (517) · 25 (193); 11 sprite(s) without `ContainerCapacity` → default 50 — e.g. Small Clothes Rack, Large Clothes Rack, Long Shelves, Open Wardrobe
- Rooms (30 defs): `all`, clothingstore (147), clothingstorage (28), giftstore (28), hunting (26), departmentstore (25), zippeestore (21); +23 more
- Procedural lists (31): ZippeeClothing (3), CampingStoreClothes (4), BandMerchClothes (2), WeddingStoreDresses (2), CampingStoreLegwear (3), ArmySurplusOutfit (2), ClothingStoresSport (4), BackstageClothingRack (1); …; plus 2 direct table(s)
- Top loot: T-shirt 6.0%, T-shirt - Music 6.0%, Wallet 3.8%, T-shirt - Sports 3.6%, Shirt - Lumberjack 3.0%, Suit Pants 2.7%, Leather Coat 2.0%, Leather Jacket 1.9%, Tank Top 1.9%, Wedding Dress 1.9%, Military Pants - Green Camo 1.8%, Military Shorts - Green Camo 1.8%

### coffin — Coffin
- furniture · capacity 30 · icon Item_Coffin_Plain · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- `all.coffin` is an empty table.
- Sprites: 8 sprites, 8 used on the map in 4 of 4,065 cells; capacity by map use: 30 (14) — e.g. Flat Coffin, Upright Coffin
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: none — the table(s) are empty

### counter — Cupboard
- furniture · capacity 50 · icon Container_Counter · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Sprites: 257 sprites, 207 used on the map in 610 of 4,065 cells; capacity by map use: 50 (9,193) · 20 (92) · 40 (27) · 30 (19); 209 sprite(s) without `ContainerCapacity` → default 50 — e.g. Oak Counter, Birchwood Counter, Oak Corner Counter, Wooden Counter
- Rooms (218 defs): `all`, bathroom (15,472), livingroom (10,624), kitchen (7,401), office (3,904), garagestorage (1,318), kidsbedroom (1,295); +211 more
- Procedural lists (256, * = forced): StoreCounterBags (28), StoreCounterBagsFancy (13), StoreCounterCleaning* (85), Empty* (15), CarnivalPrizes (5), OfficeCounter (5), MedicalClinicDrugs (7), StoreKitchenCleaning* (43); …; plus 23 direct table(s)
- Top loot: Plastic Bag 3.5%, Tote Bag 3.2%, Magazine 2.4%, Paper Bag 2.3%, Paperback 1.4%, Sheet of Paper 1.2%, Mug 1.2%, Dish Towel 1.2%, Painkillers 1.1%, Paper Napkins 1.1%, Money 1.1%, Book 1.0%

### cupboard — Cupboard
- furniture · capacity ? · icon - · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Registered with a title only; no sprite or distribution uses it.
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### desk — Desk
- furniture · capacity 20 · icon Container_Drawer · CLONE-Z: Desk
- Engine: registered `ContainerType`.
- Sprites: 62 sprites, 46 used on the map in 328 of 4,065 cells; capacity by map use: 20 (1,139) · 50 (138) · 10 (37) · 5 (20) · 7 (4); 8 sprite(s) without `ContainerCapacity` → default 50 — e.g. Office Desk, Dark School Bench, Office 2 Desk, Medical Desk
- Rooms (53 defs): `all`, bedroom (11,303), office (3,904), janitor (688), breakroom (321), medical (168), security (122); +46 more
- Procedural lists (31, * = forced): MedicalOfficeDesk* (9), PoliceDesk* (3), OfficeDesk (2), SpiffosDesk* (2), CarDealerDesk (1), CyberCafeDesk (1), DaycareDesk (1), DeskGeneric (1); …; plus 18 direct table(s)
- Top loot: Paperwork 7.7%, Vehicle Key 6.8%, Note 3.6%, Magazine 3.4%, Menu 3.0%, Paperback 2.6%, Money 2.0%, Receipt 2.0%, Index Card 1.7%, Credit Card 1.6%, Book 1.5%, Gum 1.5%

### dishescabinet — Cabinet
- furniture · capacity 25 · icon - · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 177 of 4,065 cells; capacity by map use: 25 (672) — e.g. China Cabinet
- Rooms (7 defs): `all`, office (3,904), officestorage (171), cafe (61), viplounge (10), pawnshop (9), captainoffice (1)
- Procedural lists (5): DishCabinetVIPLounge (1), PoliceCaptainCabinet (1), DishCabinetGeneric (1), DishCabinetLiquor (1), FoodGourmet (1); plus 4 direct table(s)
- Top loot: Wine Glass 22.6%, Bottle of Fine Red Wine 11.2%, Brandy 6.9%, Port 5.2%, Vermouth 5.2%, Bottle of Whiskey 3.9%, Sherry 3.7%, Scotch 3.4%, Red Wine 3.3%, White Wine 3.3%, Corkscrew 2.9%, Matchbox 2.8%

### displaycase — Display Case
- furniture · capacity 20 · icon - · CLONE-Z: Shelf
- Engine: registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 48 of 4,065 cells; capacity by map use: 20 (190) — e.g. Glass Display Counter, Glass Corner Display Counter
- Rooms (22 defs): clothingstore (147), bar (109), electronicstore (45), giftstore (28), hunting (26), departmentstore (25); +16 more
- Procedural lists (42): StoreDisplayWatches (3), GunStoreRifles (3), GunStoreKnives (3), OptometristGlasses (2), BoxingMemorabilia (1), CameraStoreDisplayCase (1), CandyStoreSnacks (1), KnifeStoreCutlery (1); …
- Top loot: Digital Watch 5.4%, Wrist Watch - Classic 4.5%, Whetstone 4.0%, Kitchen Knife 2.8%, Camera 2.4%, Pizza Slice 2.4%, Cigar 2.0%, Fleshing Tool 1.7%, Hunting Knife 1.7%, Hatchet 1.7%, Comic Book 1.6%, Prescription Glasses 1.4%

### displaycasebakery — Display Case
- furniture · capacity 20 · icon - · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Sprites: 24 sprites, 16 used on the map in 70 of 4,065 cells; capacity by map use: 20 (208) — e.g. Rounded Glass Display Counter, Glass Display Stand, Grocery Trapezoid Counter, Pie Display Stand
- Rooms (18 defs): `all`, restaurantdining (82), grocery (77), cafe (61), cafeteriakitchen (47), cafeteria (38), bakery (30); +11 more
- Procedural lists (13): BakeryCake (9), BakeryMisc (8), BakeryDoughnuts (6), BakeryPie (9), BakeryBread (7), BandMerchClothes (1), CandyStoreSnacks (1), CrateConesIceCream (1); …; plus 1 direct table(s)
- Top loot: Cone 7.2%, T-shirt - Music 5.2%, Cake Slice 5.1%, Chocolate Donut 4.5%, Donut 4.5%, Frosted Donut 4.5%, Jelly Donut 4.5%, Cherry Pie Slice 3.9%, Bread 3.4%, Ice Cream 2.0%, Bagel 1.5%, Black Forest Cake Slice 1.4%

### dresser — Drawer
- furniture · capacity 20 · icon Container_Drawer · CLONE-Z: Wardrobe
- Engine: registered `ContainerType`.
- Sprites: 24 sprites, 24 used on the map in 449 of 4,065 cells; capacity by map use: 20 (2,885) — e.g. Mirror And Drawers, Light Wood Drawers, Fancy Oak Drawers, Fancy Drawers
- Rooms (24 defs): `all`, bedroom (11,303), office (3,904), kidsbedroom (1,295), motelroom (652), breakroom (321), ww_bedroom (47); +17 more
- Procedural lists (19, * = forced): OfficeCounter (2), StripClubDressers (2), BedroomDresserChild* (2), BackstageDresser (1), BandPracticeClothing (1), CarpenterOutfit (1), ChefOutfit (1), DresserGeneric (1); …; plus 5 direct table(s)
- Top loot: Money 8.5%, Paperwork 5.5%, Paperback 3.8%, Hand-drawn Armor Schematic 3.4%, Magazine 3.3%, Sheet of Paper 2.9%, Bra - Strapless 2.3%, Bra 1.9%, Camera Film 1.5%, Underpants 1.3%, Fleshing Tool 1.3%, Handwritten Letter 1.3%

### filingcabinet — File Cabinet
- furniture · capacity 15 · icon Container_Drawer · CLONE-Z: Desk
- Engine: registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 281 of 4,065 cells; capacity by map use: 15 (893) — e.g. White File Cabinet, Grey File Cabinet
- Rooms (20 defs): `all`, office (3,904), kidsbedroom (1,295), officestorage (171), security (122), policeoffice (43), cardealershipoffice (26); +13 more
- Procedural lists (7): PoliceFilingCabinet (10), FilingCabinetGeneric (3), CarDealerFilingCabinet (1), CrateComics (1), CyberCafeFilingCabinet (1), JewelryStorageAll (1), NolansFilingCabinet (1); plus 2 direct table(s)
- Top loot: Paperwork 46.8%, Sheet of Paper 22.4%, Vehicle Key 7.2%, Note 4.3%, Comic Book 1.9%, Notebook 1.6%, Index Card 1.6%, Clipboard 1.4%, Notepad 1.4%, Handwritten Letter 1.3%, Graph Paper 1.0%, Receipt 1.0%

### grocerstand — Grocer Display
- furniture · capacity 20 · icon - · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Sprites: 12 sprites, 12 used on the map in 71 of 4,065 cells; capacity by map use: 20 (209) — e.g. Shop Display Counter, Greene 's Grocery Display Counter, Trapzoid Shop Shelves
- Rooms (12 defs): `all`, grocery (77), cafeteriakitchen (47), liquorstore (39), cafeteria (38), bakery (30), zippeestore (21); +5 more
- Procedural lists (20): StoreShelfDrinks (3), CrateBeer (1), CafeteriaFruit (2), GroceryStandVegetables1 (3), GroceryStandVegetables2 (3), GroceryStandVegetables5 (3), CafeteriaDrinks (2), CafeteriaSnacks (2); …; plus 2 direct table(s)
- Top loot: Can of Soda 6.8%, Cola 6.7%, Diet Cola 6.7%, Ginger Ale 6.7%, Orange Soda 6.7%, Water Bottle 6.7%, Beer Bottle 2.2%, Beer Can 2.2%, Apple 2.1%, Banana 2.1%, Orange 1.9%, Cherry 1.5%

### locker — Locker
- furniture · capacity 50 · icon - · CLONE-Z: Locker
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 20 sprites, 20 used on the map in 278 of 4,065 cells; capacity by map use: 50 (345) · 10 (279) · 40 (133) · 30 (5); 8 sprite(s) without `ContainerCapacity` → default 50 — e.g. Small Chest, Yellow Wall Locker, Green Wall Locker, Blue Wall Locker
- Rooms (53 defs): `all`, bathroom (15,472), bedroom (11,303), livingroom (10,624), garagestorage (1,318), kidsbedroom (1,295), laundry (1,193); +46 more
- Procedural lists (77, * = forced): FireDeptLockers* (6), GunStoreAmmunition (3), FactoryLockers* (5), BandPracticeClothing (2), PoliceStorageGuns* (3), SchoolLockersBad (2), GymLockers* (4), HospitalLockers* (4); …; plus 4 direct table(s)
- Top loot: Magazine 3.6%, Paperback 2.9%, Gum 2.0%, Patrol Revolver 1.6%, Box of 5.56x45mm Rounds 1.6%, Holster 1.3%, JS-2000 Shotgun 1.2%, Kneepad 1.1%, Bulletproof Vest - Police 1.1%, Coveralls 1.1%, T-shirt - Firefighter 1.1%, M9 Pistol 1.1%

### mannequin — Mannequin
- furniture · capacity 50 · icon Container_Mannequin · CLONE-Z: Wardrobe
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Created by `IsoMannequin` as `mannequin` (the registry spells it `Mannequin`); accepts only wearable clothing or equippable bags (`IsoMannequin.isItemAllowedInContainer`). Scarecrows are mannequin scripts (`scripts/generated/mannequins.txt`).
- Capacity: ItemContainer default (the creating class sets no capacity).
- No Distributions.lua entry — never rolls loot.

### medicine — Cabinet
- furniture · capacity 10 · icon Container_Medicine · CLONE-Z: MedCabinet
- Engine: registered `ContainerType`.
- Sprites: 16 sprites, 12 used on the map in 320 of 4,065 cells; capacity by map use: 10 (660) · 30 (76) — e.g. Medicine Cabinet, Wide Medical Cabinet, First Aid Cabinet
- Rooms (11 defs): `all`, bathroom (15,472), security (122), oldmedical (3), policeswat (2), coroneroffice (1), waitingroom (1); +4 more
- Procedural lists (6): MedicalCabinet (3), SafehouseMedical_Mid (2), BathroomCabinet (1), MorgueTools (1), SafehouseMedical (1), Empty (1); plus 3 direct table(s)
- Top loot: Bandage - Adhesive 9.5%, Alcohol Wipes 8.3%, Painkillers 7.0%, Bandage 4.7%, Surgical Gloves 4.1%, Rag 4.0%, Cotton Balls 3.5%, Tweezers 3.4%, Scalpel 3.0%, Suture Needle 3.0%, Bottle of Disinfectant 2.7%, Surgical Mask 2.6%

### metal_shelves — Shelves
- furniture · capacity 50 · icon Container_Shelf · CLONE-Z: Shelf
- Engine: registered `ContainerType`.
- Sprites: 16 sprites, 16 used on the map in 572 of 4,065 cells; capacity by map use: 50 (1,876) · 30 (1,209) · 20 (4); 8 sprite(s) without `ContainerCapacity` → default 50 — e.g. Metal Wall Shelves, Large Metal Shelves, Metal Locker, Crude Metal Wall Shelves
- Rooms (192 defs): `all`, bathroom (15,472), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318), laundry (1,193); +185 more
- Procedural lists (356): GunStoreAmmunition (6), PoliceEvidence (4), GasStorageCombo (4), BarShelfLiquor (3), RadioFactoryComponents (3), Empty (5), CrateTools (9), CrateLeather (3); …; plus 2 direct table(s)
- Top loot: Painkillers 1.5%, Plank 1.4%, Baseball Bat 0.9%, Bath Towel 0.9%, Medium Leather (Crude) 0.9%, Steel Sheet 0.9%, Camera Film 0.9%, Box of 5.56x45mm Rounds 0.9%, Sheet of Paper 0.9%, Box of Batteries 0.8%, Steel Bar 0.8%, Sack of Clay 0.8%

### militarylocker — Locker
- furniture · capacity 50 · icon - · CLONE-Z: GunLocker
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 25 of 4,065 cells; capacity by map use: 50 (36) · 40 (7); 4 sprite(s) without `ContainerCapacity` → default 50 — e.g. Military Locker, Crude Tall Locker
- Rooms (9 defs): `all`, security (122), gunstorestorage (27), gunstore (21), armystorage (20), armysurplus (8), weldingworkshop (3); +2 more
- Procedural lists (8): GunStoreAmmunition (2), PoliceStorageGuns (2), ArmyStorageGuns (2), ArmySurplusCases (1), SWATStorageGuns (1), WeldingWorkshopTools (1), ArmyStorageAmmunition (1), ArmyStorageOutfit (1)
- Top loot: Box of 5.56x45mm Rounds 8.4%, M9 Pistol 7.2%, Box of .30-30 Rounds 4.2%, Box of 12g Rounds 4.2%, Box of 7.62x51mm Rounds 4.2%, Box of 9x19mm Rounds 4.2%, Small Protective Military Case 4.2%, Box of .357 Magnum Rounds 3.6%, Box of .38 Special Rounds 3.6%, Box of .44 Magnum Rounds 3.6%, Box of .45 ACP Rounds 3.6%, JS-2000 Shotgun 3.6%

### officedrawers — Drawer
- furniture · capacity ? · icon Container_Drawer · CLONE-Z: Desk
- Engine: registered `ContainerType`.
- Only the generic `all.officedrawers` entry exists; no B42 sprite defines this container.
- Capacity: no sprite defines this container type.
- Rooms (1 def): `all`
- Procedural lists (1): OfficeDrawers (1)
- Top loot: Note 10.7%, Paperwork 10.7%, Sheet of Paper 10.7%, Magazine 5.0%, Index Card 3.6%, Notebook 3.6%, Paperclip 3.6%, Pencil 3.6%, Staples 3.6%, Calculator 2.9%, Clipboard 2.9%, Eraser 2.9%

### overhead — Cupboard
- furniture · capacity 50 · icon - · CLONE-Z: Pantry
- Engine: registered `ContainerType`.
- Sprites: 66 sprites, 16 used on the map in 129 of 4,065 cells; capacity by map use: 50 (188); 66 sprite(s) without `ContainerCapacity` → default 50 — e.g. Floating Trailer Counter, Floating Spiffo Counter, Floating Burger Counter, Floating Icecream Counter
- Rooms (15 defs): livingroom (10,624), kitchen (7,401), office (3,904), laundry (1,193), motelroom (652), breakroom (321); +9 more
- Procedural lists (20): BreakRoomCounter (3), LaundryCleaning (1), StoreCounterTobacco (1), WildWestKitchen (1), Empty (1), DrugLabSupplies (1), BarShelfLiquor (1), StoreShelfSnacks (1); …; plus 4 direct table(s)
- Top loot: Paper Napkins 5.3%, Magazine 2.6%, Drinking Glass 2.4%, Bowl 2.3%, Money Bundle 2.2%, Mug 1.9%, Cigarette Pack 1.8%, Sugar Cubes 1.8%, Candle 1.8%, Matchbook 1.8%, Coffee 1.8%, Menu 1.7%

### restaurantdisplay — Display
- furniture · capacity 50 · icon - · CLONE-Z: Fridge
- Engine: registered `ContainerType`.
- Sprites: 36 sprites, 28 used on the map in 81 of 4,065 cells; capacity by map use: 50 (421) · 20 (96); 20 sprite(s) without `ContainerCapacity` → default 50 — e.g. Cantina Food Counter, Corner A Counter, Middle Counter, Corner B Counter
- Rooms (30 defs): `all`, restaurantdining (82), restaurantkitchen (82), cafe (61), cafeteriakitchen (47), cafekitchen (43), cafeteria (38); +23 more
- Procedural lists (50): ServingTrayPizza (11), FreezerIceCream (2), BakeryMisc (3), ServingTrayFries (11), BakeryDoughnuts (5), ServingTrayBurgers (8), ServingTrayChickenNuggets (9), ServingTrayFishFried (2); …; plus 1 direct table(s)
- Top loot: Pizza Slice 17.0%, Fries 5.5%, Burger 3.9%, Chicken Nuggets 3.8%, Fish (Fried) 3.5%, Chicken Wing 3.3%, Ice Cream 3.2%, Fried Onion Rings 3.2%, Chocolate Donut 2.5%, Donut 2.5%, Frosted Donut 2.5%, Jelly Donut 2.5%

### safe — ?
- furniture · capacity ? · icon - · CLONE-Z: -
- Only `mayorwestpointoffice.safe`; its list `MayorWestPointSafe` is empty in this build.
- Capacity: no sprite defines this container type.
- Rooms (1 def): mayorwestpointoffice (1)
- Procedural lists (1): MayorWestPointSafe (1)
- Top loot: none — the table(s) are empty

### schooldesk — ?
- furniture · capacity ? · icon - · CLONE-Z: Desk
- Listed in 4 school rooms, but no B42 sprite sets `container = schooldesk`, so the entries are currently unreachable.
- Capacity: no sprite defines this container type.
- Rooms (4 defs): elementaryclassroom (74), classroom (72), secondaryclassroom (67), schoolstorage (15)
- Procedural lists (2): ClassroomDesk (2), ClassroomSecondaryDesk (1); plus 1 direct table(s)
- Top loot: Note 16.9%, Sheet of Paper 16.9%, Book 6.0%, Notebook 5.6%, Paperback 5.6%, Pencil 5.6%, Magazine 5.6%, Eraser 4.5%, Doodle 3.0%, Adhesive Tape 2.3%, Gum 2.3%, Pen - Black 2.3%

### shelves — Shelves
- furniture · capacity 20 · icon Container_Shelf · CLONE-Z: Shelf
- Engine: registered `ContainerType`.
- Sprites: 182 sprites, 152 used on the map in 525 of 4,065 cells; capacity by map use: 20 (2,434) · 40 (1,280) · 15 (736) · 50 (725) · 10 (516) · 25 (263) · 30 (219) · 5 (99); 44 sprite(s) without `ContainerCapacity` → default 50 — e.g. Oakwood Shelves, Hardwood Wall Shelves, Beige Rack Shop Shelves, Low Shelves
- Rooms (127 defs): `all`, bathroom (15,472), livingroom (10,624), kitchen (7,401), office (3,904), kidsbedroom (1,295), laundry (1,193); +120 more
- Procedural lists (336, * = forced): BarShelfLiquor* (6), LivingRoomShelf (5), WildWestShelves (3), StoreShelfDrinks (8), StoreShelfSnacks (8), StoreShelfCombo* (24), ClothingStoresShoes (3), MedicalOfficeBooks* (3); …; plus 3 direct table(s)
- Top loot: Book 4.4%, Drinking Glass 2.6%, Paperback 2.2%, VHS - Retail 1.7%, Magazine 1.7%, Sheet of Paper 1.3%, Prop Book 1.1%, Comic Book 0.8%, Cocktail Umbrella 0.8%, Cigarette Pack 0.8%, CD 0.8%, Bath Towel 0.8%

### shelvesmag — Magazine Stand
- furniture · capacity 15 · icon - · CLONE-Z: Shelf
- Engine: registered `ContainerType`.
- Sprites: 20 sprites, 20 used on the map in 102 of 4,065 cells; capacity by map use: 15 (352) — e.g. Magazine Stand, Red Magazine Shelf, Blue Magazine Shelf
- Rooms (18 defs): `all`, gasstore (103), grocery (77), liquorstore (39), pharmacy (31), giftstore (28), hunting (26); +11 more
- Procedural lists (16): MagazineRackMixed (10), MagazineRackNewspaper (9), GunStoreMagazineRack (2), MagazineRackMaps (7), GiftStoreCards (1), HospitalMagazineRack (1), LiquorStoreMagazineRack (1), OutdoorSupplyMagazines (1); …
- Top loot: Magazine 35.0%, Comic Book 12.5%, Newspaper 7.6%, Brochure 5.8%, Flier 5.3%, Magazine: TV Monthly 5.3%, Paperback 2.4%, HottieZ 2.0%, Magazine: Crossword 1.8%, Magazine: Wordsearch 1.8%, Birthday Card 1.1%, Sympathy Card 0.8%

### sidetable — Drawer
- furniture · capacity 10 · icon Container_Drawer · CLONE-Z: Desk
- Engine: registered `ContainerType`.
- Sprites: 14 sprites, 14 used on the map in 446 of 4,065 cells; capacity by map use: 10 (1,683) · 15 (319) — e.g. Bedroom Drawers, Small Table, Brown Low Table, Pale Drawers
- Rooms (29 defs): `all`, bedroom (11,303), livingroom (10,624), office (3,904), kidsbedroom (1,295), motelroom (652), prisoncells (308); +22 more
- Procedural lists (21, * = forced): HospitalRoomWardrobe (2), MedicalStorageOutfit (2), BedroomSidetableChild* (2), MedicalClinicTools (1), MorgueTools (1), MotelSideTable (1), OfficeCounter (1), SalonShelfHaircare (1); …; plus 12 direct table(s)
- Top loot: Hair Gel 3.5%, Hair Spray 3.5%, Bottle of Hair Dye 3.5%, Magazine 2.8%, Surgical Gloves 2.6%, Surgical Mask 2.6%, Sewing Pattern 2.5%, Paperback 2.5%, Medical Scrubs 2.1%, Pants - Medical 2.1%, Surgical Cap 2.1%, Book 2.1%

### toolcabinet — Tool Cabinet
- furniture · capacity 20 · icon Container_ToolCabinet · CLONE-Z: ToolBench
- Engine: registered `ContainerType`.
- Sprites: 12 sprites, 12 used on the map in 167 of 4,065 cells; capacity by map use: 20 (236) · 5 (8) — e.g. Red Mobile Tool Cabinet, Branch Workbench
- Rooms (22 defs): `all`, storageunit (695), farmstorage (134), toolstore (52), railroadrepair (14), firestorage (13), carsupply (9); +15 more
- Procedural lists (16): MetalShopTools (3), FireStorageMechanics (2), ToolCabinetMechanics (3), CabinetFactoryTools (1), CrateLeather (1), FurnitureFactoryTools (1), KnifeFactoryTools (1), PaintStudioTools (1); …; plus 3 direct table(s)
- Top loot: Welding Rods 5.2%, Nuts and Bolts 4.0%, Large Hook 3.5%, Screwdriver 3.3%, Welding Torch 2.9%, Medium Leather (Crude) 2.5%, Pliers 2.4%, Box of Screws 2.4%, Oxygen Tank 2.4%, Propane Lantern Bottle 2.4%, Electrical Wire 2.1%, Whetstone 1.8%

### wardrobe — Cabinet
- furniture · capacity 25 · icon Container_Cabinet · CLONE-Z: Wardrobe
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 48 sprites, 48 used on the map in 413 of 4,065 cells; capacity by map use: 25 (4,224) — e.g. Light Wood Wardrobe, Fancy Oak Wardrobe, Dark Fancy Wardrobe, Fancy White Wardrobe
- Rooms (19 defs): `all`, bedroom (11,303), livingroom (10,624), kidsbedroom (1,295), motelroom (652), aesthetic (80), elementaryclassroom (74); +12 more
- Procedural lists (29, * = forced): WardrobeChild* (3), WardrobeGeneric (2), CarpenterOutfit (1), ChefOutfit (1), CrateSalonSupplies (1), DrugLabOutfit (1), FarmerOutfit (1), WildWestBedroom (1); …; plus 2 direct table(s)
- Top loot: Sheet 3.2%, Bottle of Hair Dye 3.1%, Hand-drawn Armor Schematic 3.1%, Bath Towel 2.5%, Magazine 2.5%, Hair Spray 2.0%, Melee Weapon Plan 2.0%, Pillow 1.9%, Dust Mask 1.7%, Safety Goggles 1.4%, Kneepad 1.3%, Book 1.2%

**— appliance —**

### barbecue — Barbecue
- appliance · capacity 15 · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoBarbecue` sprite; container temperature comes from the barbecue (`getTemprature`).
- Sprites: 1 sprite, 1 used on the map in 202 of 4,065 cells; capacity by map use: 15 (202) — e.g. Charcoal Barbecue; IsoType IsoBarbecue×1
- Rooms (3 defs): `all`, toolstore (52), barbecuestore (8)
- Procedural lists (2, * = forced): BBQCharcoal (1), BBQCharcoalRich* (1); plus 2 direct table(s)
- Top loot: Basting Brush 9.9%, Carving Fork 9.9%, Grill Brush 9.9%, Kitchen Tongs 9.9%, Oven Mitt 9.9%, Spatula 9.9%, Beef Patty 8.4%, Corn 5.7%, Potato 5.7%, Charcoal 3.9%, Lighter - BBQ 3.9%, Chicken Leg 3.0%

### barbecuepropane — Barbecue
- appliance · capacity 15 · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoBarbecue` sprites; propane tank add/remove/toggle handlers in `client/ISUI/LootWindow/Handlers/PropaneBarbecue*.lua`.
- Sprites: 8 sprites, 8 used on the map in 175 of 4,065 cells; capacity by map use: 15 (266) — e.g. Full Jorge Foreguy Barbecue, Jorge Foreguy Barbecue; IsoType IsoBarbecue×8
- Rooms (3 defs): `all`, toolstore (52), barbecuestore (8)
- Procedural lists (2, * = forced): BBQPropane (1), BBQPropaneRich* (1); plus 2 direct table(s)
- Top loot: Basting Brush 10.6%, Carving Fork 10.6%, Grill Brush 10.6%, Kitchen Tongs 10.6%, Oven Mitt 10.6%, Spatula 10.6%, Beef Patty 9.0%, Corn 6.0%, Potato 6.0%, Lighter - BBQ 4.2%, Chicken Leg 3.1%, Pork Chop 3.0%

### cashregister — Cash Register
- appliance · capacity 5 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 158 of 4,065 cells; capacity by map use: 5 (448) — e.g. Cash Register, Black Cash Register
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Money 81.3%, Receipt 8.6%, Note 4.3%, Pencil 1.7%, Key 1.3%, Notepad 0.9%, Vehicle Key 0.9%, Calculator 0.4%, Eraser 0.1%, Pen - Black 0.1%, Pen - Blue 0.1%, Pen - Red 0.1%

### clothingdryer — Clothing Dryer
- appliance · capacity 20 · icon Container_ClothingDryer · CLONE-Z: Wardrobe
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- `IsoClothingDryer` sprites; 1 fill in 5 spawns wet clothes; locked while running (`ClothingDryerLogic`). Stacked washer/dryer objects default to capacity 20 (`IsoStackedWasherDryer`).
- Sprites: 4 sprites, 4 used on the map in 212 of 4,065 cells; capacity by map use: 20 (377) — e.g. White Clothing Dryer; IsoType IsoClothingDryer×4
- Rooms (10 defs): `all`, laundry (1,193), janitor (688), cafeteriakitchen (47), barstorage (15), tailoringworkshop (13), policeoutfitstorage (1); +3 more
- Procedural lists (15): SafehouseDryer (3), BarLaundry (1), CafeteriaKitchenLaundry (1), PoliceStorageOutfit (1), DryerEmpty (1), LaundryLoad1 (1), LaundryLoad2 (1), LaundryLoad3 (1); …; plus 3 direct table(s)
- Top loot: Dish Towel 35.6%, Rag 24.5%, Bath Towel 9.7%, Sheet 8.7%, Dryer Lint 4.8%, Shoes 2.2%, Long Socks 1.8%, Socks 1.8%, T-shirt 1.8%, Police T-shirt - Deputy 1.4%, Police Jacket 1.1%, Police Pants - Deputy 1.1%

### clothingwasher — Clothing Washer
- appliance · capacity 20 · icon Container_ClothingWasher · CLONE-Z: Wardrobe
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- `IsoClothingWasher` / `IsoCombinationWasherDryer` sprites. Each fill spawns its clothes wet (1 in 2) or dirty (`doRollItemInternal`); items can't be added or removed while it runs (`ClothingWasherLogic`).
- Sprites: 8 sprites, 8 used on the map in 316 of 4,065 cells; capacity by map use: 20 (826) — e.g. White Washing Machine, Blue Combo Washer Dryer; IsoType IsoCombinationWasherDryer×4, IsoClothingWasher×4
- Rooms (10 defs): `all`, laundry (1,193), janitor (688), cafeteriakitchen (47), barstorage (15), tailoringworkshop (13), policeoutfitstorage (1); +3 more
- Procedural lists (15): SafehouseWasher (3), BarLaundry (1), CafeteriaKitchenLaundry (1), PoliceStorageOutfit (1), Empty (1), LaundryLoad1 (1), LaundryLoad2 (1), LaundryLoad3 (1); …; plus 3 direct table(s)
- Top loot: Dish Towel 38.5%, Rag (Dirty) 26.5%, Bath Towel 10.5%, Sheet 9.4%, Shoes 2.4%, T-shirt 1.9%, Police T-shirt - Deputy 1.5%, Police Jacket 1.2%, Police Pants - Deputy 1.2%, Police Pants - Trooper 1.2%, Long Socks 1.0%, Socks 1.0%

### coffeemaker — Coffee Maker
- appliance · capacity ? · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `isStove()` type with title/icon, but no B42 sprite sets `container = coffeemaker`; the B42 coffee machine is a CraftBench entity (`entity_coffeemachine.txt`).
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### dishwasher — Dishwasher
- appliance · capacity 10 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Has sprites but no distribution entry, and it is a no-generic type, so it always spawns empty.
- Sprites: 8 sprites, 0 used on the map in 0 of 4,065 cells; capacity by sprite count (no sprite is used on the map): 10 (8) — e.g. Brown Dishwasher, Metal Dishwasher
- No Distributions.lua entry — never rolls loot.

### displaycasebutcher — Display Case
- appliance · capacity 50 · icon - · CLONE-Z: Fridge
- Engine: registered `ContainerType`.
- All 6 sprites carry `IsFridge`, so the case behaves as a fridge (cools while powered).
- Sprites: 6 sprites, 6 used on the map in 27 of 4,065 cells; capacity by map use: 50 (93); 6 sprite(s) without `ContainerCapacity` → default 50 — e.g. White Display Counter
- Rooms (6 defs): grocery (77), butcher (17), gigamart (9), barbecuestore (8), gigamartkitchen (5), seafoodkitchen (2)
- Procedural lists (5): ButcherFish (6), ButcherChops (5), ButcherSmoked (5), ButcherChicken (4), ButcherGround (4)
- Top loot: Pork Chop 14.9%, Chicken Leg 10.9%, Shrimp 7.7%, Steak 7.4%, Beef Patty 7.1%, Ground Beef 7.1%, Bacon 6.0%, Baloney 6.0%, Ham 6.0%, Salami 6.0%, Sausage 6.0%, Mussels 2.0%

### fireplace — Fireplace
- appliance · capacity 20 · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 6 sprites, 6 used on the map in 126 of 4,065 cells; capacity by map use: 20 (193) — e.g. Fireplace
- Rooms (9 defs): `all`, office (3,904), ww_livingroom (16), universitylibrary (11), westerndining (5), catfish_dining (2), SafehouseLoot (0); +2 more
- Procedural lists (3): SafehouseFireplace (2), SafehouseFireplace_Late (1), WildWestWoodStove (1); plus 5 direct table(s)
- Top loot: Money 17.5%, Brochure 12.3%, Flier 12.3%, Mail 12.3%, Old Newspaper 5.6%, Magazine 5.3%, Paperwork 5.3%, Catalog 3.5%, Phonebook 3.5%, Twigs 2.2%, Firewood 1.9%, Bandage (Dirty) 1.4%

### freezer — Freezer
- appliance · capacity 20 · icon Container_Freezer · CLONE-Z: Fridge
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Never a tile `container` value: created from the `Freezer` tile property — as a secondary container on 48 fridge sprites, or as the only container on 8 standalone freezer sprites (Popsicle/Chest freezers). Capacity = `FreezerCapacity`, default 15 (`IsoObject.createContainersFromSpriteProperties`).
- Powered → temperature 0.2 like a fridge; freezable food that spawns in a `freezer` is frozen on the spot (`ItemPickerJava.doRollItemInternal`).
- Sprites: 56 sprites, 56 used on the map in 545 of 4,065 cells; capacity by map use: 20 (2,891) · 5 (455) · 30 (196)
- Rooms (100 defs): `all`, livingroom (10,624), kitchen (7,401), office (3,904), garagestorage (1,318), derelict (1,038), breakroom (321); +93 more
- Procedural lists (32, * = forced): FreezerIceCream* (11), BakeryKitchenFreezer (4), ArenaKitchenFreezer (3), FreezerFrozenFood* (3), CafeteriaKitchenFreezer (2), DinerKitchenFreezer (2), RestaurantKitchenFreezer (2), SpiffosKitchenFreezer (2); …; plus 49 direct table(s)
- Top loot: Ice Cream 12.5%, Bag of Fries 10.5%, Butter 6.1%, Beef Patty 5.9%, Shrimp 3.6%, Ground Beef 3.5%, Margarine 3.1%, Bag of Chicken Nuggets 3.0%, Pack of Hotdogs 2.8%, Little Bait Fish 2.8%, Steak 2.7%, Chicken Leg 2.5%

### fridge — Fridge
- appliance · capacity 40 · icon Container_Fridge · CLONE-Z: Fridge
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Cooling: `ItemContainer.getTemprature()` is 0.2 while `isPowered()` (grid power inside a room, or a generator) and 1.0 otherwise; fridge-type containers are constructed with `ageFactor 0.02`, `cookingFactor 0`.
- Any container whose tile has `IsFridge` also counts as a fridge (`isFridge()`); 48 fridge sprites additionally carry `Freezer`, which adds a secondary `freezer` container.
- Generator draw 0.08 per fridge or freezer object, 0.13 when one object has both (`IsoObject.getGeneratorPowerConsumption`); powered fridges add a hum ambient emitter.
- House `kitchen` rooms have no `fridge` entry (they list counter/freezer/microwave/overhead/stove/…), so kitchen fridges roll the generic `all.fridge` lists.
- Sprites: 54 sprites, 54 used on the map in 544 of 4,065 cells; capacity by map use: 40 (2,631) · 50 (489) · 20 (455); 16 sprite(s) without `ContainerCapacity` → default 50 — e.g. Plain Fridge, Green Fridge, White Fridge, Red Fridge
- Rooms (100 defs): `all`, livingroom (10,624), office (3,904), garagestorage (1,318), derelict (1,038), breakroom (321), changeroom (229); +93 more
- Procedural lists (54): FridgeSoda (20), FridgeBeer (8), FridgeOffice (7), FridgeWater (14), BakeryKitchenFridge (4), FridgeSnacks (11), FridgeMedical (3), FridgeVIPLounge (3); …; plus 8 direct table(s)
- Top loot: Water Bottle 7.4%, Imported Beer 3.2%, Beer Can 3.1%, Beer Bottle 2.9%, Milk Carton 2.7%, Cola 2.5%, Diet Cola 2.5%, Ginger Ale 2.5%, Egg Carton 2.2%, Bread 2.2%, Beef Patty 2.1%, Orange Soda 2.0%

### icecream — Freezer
- appliance · capacity ? · icon Container_Freezer · CLONE-Z: Fridge
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Registered type with title "Freezer" and the freezer icon, but no B42 sprite or distribution uses it.
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### microwave — Microwave
- appliance · capacity 5 · icon Container_Microwave · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoStove` sprites: switches itself off without power, heats to at most 100 (`MaximumMicrowaveTemperature`) on a timer; running with metal inside can break it and start a fire (`IsoStove.update`).
- Sprites: 8 sprites, 8 used on the map in 292 of 4,065 cells; capacity by map use: 5 (779) — e.g. White Microwave, Chrome Microwave; IsoType IsoStove×8
- Rooms (9 defs): `all`, kitchen (7,401), cafe (61), cafekitchen (43), spiffoskitchen (28), kitchen_crepe (10), viplounge (10); +2 more
- Procedural lists (1): KitchenMicrowave (1); plus 8 direct table(s)
- Top loot: Bacon 33.3%, Bowl of Oatmeal 33.3%, TV Dinner 33.3%

### stonefurnace — Furnace
- appliance · capacity 50 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- 3 of 4 sprites are `IsoBarbecue`; `ISInventoryTransferAction` special-cases transfers into a stonefurnace.
- Sprites: 4 sprites, 4 used on the map in 1 of 4,065 cells; capacity by map use: 50 (4); 4 sprite(s) without `ContainerCapacity` → default 50; IsoType IsoBarbecue×3
- No Distributions.lua entry — never rolls loot.

### stove — Oven
- appliance · capacity 15 · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoStove` sprites (title "Oven" — PZ has no separate `oven` type): needs power; while powered the container temperature follows the stove (max 200, `MaximumStoveTemperature`). `isStove()` is true for stove, toaster and coffeemaker.
- Sprites: 32 sprites, 32 used on the map in 516 of 4,065 cells; capacity by map use: 15 (2,350) · 20 (276) · 5 (142) — e.g. Modern Oven, Green Oven, Red Oven, Grey Oven; IsoType IsoStove×32
- Rooms (23 defs): `all`, kitchen (7,401), derelict (1,038), cafe (61), cafekitchen (43), burgerkitchen (34), spiffoskitchen (28); +16 more
- Procedural lists (8, * = forced): StoveSpiffos* (3), DerelictHouseStove (1), SafehouseStove (1), SafehouseStove_Late (1), SafehouseStove_Mid (1), StoveGeneric (1), StoveClassy* (1), StoveRedneck* (1); plus 15 direct table(s)
- Top loot: Beef Patty 32.6%, Rag (Dirty) 6.9%, Tin Can 6.9%, Baking Tray 3.2%, Roasting Pan 3.2%, Baking Pan 3.2%, Muffin Tray 3.1%, Cockroach 2.8%, Grill Brush 2.4%, Kitchen Tongs 2.4%, Spatula 2.4%, Rodent Meat 2.3%

### toaster — Toaster
- appliance · capacity ? · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `isStove()` type with title/icon, but no B42 sprite sets `container = toaster`; the B42 toaster is a CraftBench entity (`scripts/generated/entities/appliances/workstations/entity_toaster.txt`).
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### vendingGt — Vending Machine
- appliance · capacity ? · icon Container_Vendingt · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Registered with title/icon only; no sprite or distribution uses it.
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### vendingpop — Vending Machine
- appliance · capacity 20 · icon Container_Vendingt · CLONE-Z: Fridge
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 108 of 4,065 cells; capacity by map use: 20 (162) — e.g. Small Soda Machine
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Cola 24.4%, Diet Cola 24.4%, Ginger Ale 24.4%, Orange Soda 24.4%, Bottle 2.4%

### vendingsnack — Vending Machine
- appliance · capacity 20 · icon Container_Vendingt · CLONE-Z: Pantry
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 91 of 4,065 cells; capacity by map use: 20 (144) — e.g. Large Machine
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Chips - Barbecue 15.2%, Chips - Plain 15.2%, Chips - Salt & Vinegar 15.2%, Chips - Sour Cream & Onion 15.2%, Pork Rinds 10.1%, Pretzels 10.1%, Peanuts 5.1%, Sunflower Seeds 5.1%, Tortilla Chips 5.1%, Licorice 0.4%, Butterchunkers Bar 0.3%, Chocolate Candy 0.3%

### woodstove — Stove
- appliance · capacity 15 · icon Container_Oven · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoFireplace` sprites (Antique/Crafted Oven); temperature from the fire (`getTemprature`).
- Sprites: 8 sprites, 8 used on the map in 58 of 4,065 cells; capacity by map use: 15 (73) — e.g. Antique Oven, Crafted Oven; IsoType IsoFireplace×8
- Rooms (8 defs): `all`, ww_kitchen (21), ww_livingroom (16), ww_bar (9), westerndining (5), ww_generalstore (2), woodcraftset (1); +1 more
- Procedural lists (2): Empty (5), WildWestWoodStove (5); plus 3 direct table(s)
- Top loot: Twigs 22.6%, Firewood 18.1%, Tree Branch 13.6%, Frying Pan 10.8%, Fireplace Poker 9.4%, Old Newspaper 9.1%, Bellows 7.0%, Cooking Pot 5.4%, Tongs 4.0%

**— storage —**

### bin — Garbage
- storage · capacity 10 · icon Container_Garbage · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- `all.bin` is `isTrash` + `ignoreZombieDensity` (spawned items are trashed) with `ClutterTables.BinJunk`.
- Sprites: 19 sprites, 19 used on the map in 475 of 4,065 cells; capacity by map use: 10 (947) · 20 (353) · 50 (78); 7 sprite(s) without `ContainerCapacity` → default 50 — e.g. Recycle Bin, Grey Garbage Bin, Round Bin, Green Garbage Bin
- Rooms (21 defs): `all`, bathroom (15,472), medical (168), bar (109), hospitalroom (64), medicaloffice (30), spiffoskitchen (28); +14 more
- Procedural lists (9): BinHospital (4), BinBar (3), BinFireStation (2), BinJays (2), BinSpiffos (2), BinBathroom (1), SafehouseBin (1), SafehouseBin_Late (1); …; plus 4 direct table(s)
- Top loot: Bandage (Dirty) 9.6%, Receipt 8.9%, Paper Napkins 8.7%, Straw 5.2%, Fountain Cup 5.0%, Fries 4.8%, Rag (Dirty) 4.5%, Tin Can 3.8%, Cocktail Umbrella 3.6%, Beer Can 3.1%, Fried Chicken 2.4%, Burger 2.4%

### BombBox — ?
- storage · capacity ? · icon - · CLONE-Z: GunLocker
- Only defined in the `BombCache1` cache room, and no StashDescriptions entry uses that spawnTable — unreachable in this build.
- Capacity: no sprite defines this container type.
- Rooms (1 def): BombCache1 (0)
- Direct tables only (1).
- Top loot: Molotov Cocktail 21.8%, Smoke Bomb 10.9%, Aerosol Bomb 6.5%, Pipe Bomb 6.5%, Remote Smoke Bomb 6.5%, Smoke Bomb with Medium-Range Sensor 6.5%, Smoke Bomb with Timer 6.5%, Fire Bomb 4.4%, Aerosol Bomb with Medium-Range Sensor 2.2%, Aerosol Bomb with Timer 2.2%, Pipe Bomb with Medium-Range Sensor 2.2%, Pipe Bomb with Timer 2.2%

### BoozeBox — ?
- storage · capacity ? · icon - · CLONE-Z: Pantry
- Only defined in the `BoozeCache1` cache room; the one stash using that spawnTable (`WorldStashMap8`) declares no stash container, so no stash places a BoozeBox.
- Capacity: no sprite defines this container type.
- Rooms (1 def): BoozeCache1 (0)
- Direct tables only (1).
- Top loot: Bottle of Whiskey 9.7%, Brandy 9.7%, Gin 9.7%, Rum 9.7%, Scotch 9.7%, Tequila 9.7%, Vodka 9.7%, Beer Bottle 3.9%, Boxed Red Wine 3.9%, Red Wine 3.9%, White Wine 3.9%, Bottle Opener 2.3%

### cardboardbox — Box
- storage · capacity 25 · icon Container_Crate · CLONE-Z: Crate
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 348 of 4,065 cells; capacity by map use: 25 (1,458) — e.g. Cartbox
- Rooms (142 defs): `all`, bathroom (15,472), bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904); +135 more
- Procedural lists (348): PoliceFileBox (7), GasStorageCombo (6), CrateOfficeSupplies (6), CrateNewspapersNew (3), RadioFactoryComponents (3), GiftStoreToys (4), CrateBooksSchool (3), CratePaint (6); …
- Top loot: Paperwork 7.0%, Camera Film 6.1%, Book 1.8%, Newspaper 1.6%, Sheet of Paper 1.2%, Water Bottle 1.0%, Photograph 0.9%, Magazine 0.9%, Painkillers 0.8%, Golf Club 0.8%, Notepad 0.8%, Paperback 0.8%

### clothingdryerbasic — Laundry Cart
- storage · capacity 20 · icon - · CLONE-Z: Wardrobe
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Despite the name a plain basket: its 2 sprites are "Washing Bin" (no IsoType); the UI title is "Laundry Cart".
- Sprites: 2 sprites, 2 used on the map in 68 of 4,065 cells; capacity by map use: 20 (101) — e.g. Washing Bin
- Rooms (17 defs): `all`, laundry (1,193), janitor (688), motelroom (652), security (122), hospitalroom (64), cafeteriakitchen (47); +10 more
- Procedural lists (21, * = forced): GymTowels (4), LaundryHospital* (3), BarLaundry (1), BreweryEmptyBottles (1), CafeteriaKitchenLaundry (1), FryFactoryPotatoes (1), MotelLinens (1), PostOfficeParcels (1); …; plus 2 direct table(s)
- Top loot: Bath Towel 28.8%, Sheet 21.3%, Dish Towel 12.9%, Beer Bottle (empty) 7.0%, Bottle of Whiskey (empty) 7.0%, Hospital Gown 3.8%, Produce Box - Medium 3.8%, Basketball 3.5%, Soccer Ball 3.5%, Pillow 1.9%, Dryer Lint 1.0%, Mail Bag 1.0%

### crate — Crate
- storage · capacity 60 · icon Container_Crate · CLONE-Z: Crate
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Player-built containers (`IsoThumpable.setIsContainer`) are always type `crate`, capacity from the sprite's `ContainerCapacity`.
- Sprites: 16 sprites, 16 used on the map in 390 of 4,065 cells; capacity by map use: 60 (656) · 40 (49) · 80 (1) — e.g. Crate, Metal Crate, Fancy Metal Crate
- Rooms (113 defs): `all`, bathroom (15,472), bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904); +106 more
- Procedural lists (345): FryFactoryPotatoes (7), CrateLumber (11), ToolFactoryBarStock (6), GunStoreAmmunition (3), GasStorageCombo (3), CrateTools (13), ToolFactoryIngots (4), BatFactoryBats (2); …; plus 2 direct table(s)
- Top loot: Plank 5.0%, Steel Bar 1.9%, Produce Box - Medium 1.9%, Steel Sheet 1.4%, Baseball Bat 1.4%, Steel Ingot 1.3%, Sack 1.3%, Flint Nodule 1.3%, Small Handle 1.0%, Animal Feed Bag 1.0%, Long Stick 0.9%, Box of 5.56x45mm Rounds 0.9%

### dumpster — Dumpster
- storage · capacity 30 · icon Container_Garbage · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 12 sprites, 8 used on the map in 174 of 4,065 cells; capacity by map use: 30 (644) — e.g. Orange Dumpster, Blue Dumpster
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Garbage Bag 77.3%, Empty Pallet 5.2%, Dryer Lint 0.2%, Paperwork 0.2%, T-shirt 0.07%, Pants 0.07%, Game Pieces 0.07%, Bra - Strapless 0.06%, Underpants 0.06%, Bra 0.05%, Visor 0.05%, Wood Splinters 0.05%

### FoodBox — ?
- storage · capacity 60 · icon - · CLONE-Z: Pantry
- Stash-map cache type: an annotated stash map (`StashDescriptions`) places the container and its `spawnTable` cache room fills it.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): sprite carpentry_01_16 cap 60 ×4 (FoodCache1); sprite furniture_storage_02_19 cap 25 ×3 (FoodCache1)
- Rooms (1 def): FoodCache1 (0)
- Direct tables only (1).
- Top loot: Canned Fruit Beverage 2.8%, Beef Jerky 2.0%, Bouillon Cube 2.0%, Crackers 2.0%, Dehydrated Meat Stick 2.0%, Jar of Bell Peppers 2.0%, Jar of Broccoli 2.0%, Jar of Cabbage 2.0%, Jar of Carrots 2.0%, Jar of Eggplants 2.0%, Jar of Leeks 2.0%, Jar of Potatoes 2.0%

### garagestorage — Crate
- storage · capacity ? · icon - · CLONE-Z: Crate
- Engine: registered `ContainerType`.
- Registered with title "Crate", but ContainerButtonIcons spells the key `garage_storage`, so the button falls back to the default icon; no sprite or distribution uses the type (`garagestorage` is also a very common room name).
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### GunBox — ?
- storage · capacity 18 · icon - · CLONE-Z: GunLocker
- Stash-map cache type: an annotated stash map (`StashDescriptions`) places the container and its `spawnTable` cache room fills it.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): item Duffel Bag (Bag_DuffelBagTINT) cap 18 ×9 (GunCache1); item Ammunition Box (Bag_AmmoBox_Mixed) cap 6 ×6 (GunCache2); item Duffel Bag (Bag_DuffelBagTINT) cap 18 ×3 (GunCache2); item Duffel Bag (Bag_FoodCanned) cap 18 ×2 (GunCache2); sprite floors_interior_tilesandwood_01_57 cap 7 ×1 (GunCache1); sprite floors_interior_tilesandwood_01_62 cap 7 ×1 (GunCache1); item Duffel Bag (Bag_ShotgunDblBag) cap 18 ×1 (GunCache2); sprite carpentry_01_16 cap 60 ×1 (GunCache1)
- Rooms (2 defs): GunCache1 (0), GunCache2 (0)
- Direct tables only (2).
- Top loot: Box of .357 Magnum Rounds 9.6%, Box of .38 Special Rounds 9.6%, Box of .44 Magnum Rounds 9.6%, Box of .45 ACP Rounds 9.6%, Box of 12g Rounds 9.6%, Box of 5.56x45mm Rounds 9.6%, Box of 7.62x51mm Rounds 9.6%, Box of 9x19mm Rounds 9.6%, Money 3.9%, MSR700 Rifle 2.6%, Bullets Bandolier 1.3%, JS-2000 Shotgun 1.3%

### logs — Logs
- storage · capacity 20 · icon Item_Logs · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 14 sprites, 14 used on the map in 156 of 4,065 cells; capacity by map use: 20 (457) · 45 (73) — e.g. 5 Log Stack, 6 Log Stack, Large Log Stack
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Log 58.8%, Wood Splinters 11.8%, Broken Branch 5.9%, Firewood 5.9%, Tree Branch 5.9%, Large Branch 4.7%, Pillbug 4.7%, Sapling 2.4%

### MedicalBox — ?
- storage · capacity 7 · icon - · CLONE-Z: MedCabinet
- Stash-map cache type: an annotated stash map (`StashDescriptions`) places the container and its `spawnTable` cache room fills it.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): sprite floors_interior_tilesandwood_01_62 cap 7 ×2 (MedicalCache1); sprite location_community_medical_01_89 cap 10 ×1 (MedicalCache1); item Trauma Bag (Bag_MedicalBag) cap 18 ×1 (MedicalCache1)
- Rooms (1 def): MedicalCache1 (0)
- Direct tables only (1).
- Top loot: Bandage 22.2%, Antibiotics 11.1%, Bottle of Whiskey 11.1%, Painkillers 11.1%, Antidepressants 3.7%, Beta Blockers 3.7%, Forceps 3.7%, Sleeping Pills 3.7%, Suture Needle 3.7%, Bottle of Disinfectant 3.0%, Medical Shears 3.0%, Rag 3.0%

### militarycrate — Military Crate
- storage · capacity 50 · icon Container_Crate · CLONE-Z: Crate
- Engine: can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 12 sprites, 12 used on the map in 21 of 4,065 cells; capacity by map use: 50 (71) — e.g. Military Crate
- Rooms (7 defs): `all`, office (3,904), security (122), gunstorestorage (27), gunstore (21), armystorage (20), armysurplus (8)
- Procedural lists (10): ArmyStorageElectronics (2), ArmySurplusAmmoBoxes (1), ArmySurplusCases (1), CrateHumanitarian (1), GunStoreAmmunition (1), PoliceStorageGuns (1), ArmyStorageOutfit (1), ArmyStorageMedical (1); …
- Top loot: Electrical Wire 8.0%, Small Protective Military Case 5.5%, Box of 5.56x45mm Rounds 5.2%, Ammunition Box 4.9%, Power Bar 3.4%, Box of .30-30 Rounds 2.6%, Box of 12g Rounds 2.6%, Box of 7.62x51mm Rounds 2.6%, Box of 9x19mm Rounds 2.6%, Protective Military Case 2.5%, M9 Pistol 2.5%, Box of .357 Magnum Rounds 2.4%

### newspaper_dispatch — Newspaper Box
- storage · capacity 5 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 6 of 4,065 cells; capacity by map use: 5 (9)
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Newspaper 100.0%

### newspaper_herald — Newspaper Box
- storage · capacity 5 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 4 of 4,065 cells; capacity by map use: 5 (7)
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Newspaper 100.0%

### newspaper_knews — Newspaper Box
- storage · capacity 5 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 7 of 4,065 cells; capacity by map use: 5 (10)
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Newspaper 100.0%

### newspaper_times — Newspaper Box
- storage · capacity 5 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 3 of 4,065 cells; capacity by map use: 5 (6)
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Newspaper 100.0%

### plankstash — Stash
- storage · capacity 7 · icon - · CLONE-Z: Crate
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Hidden under loose floor planks: `RBBasic` swaps floor tiles (e.g. `floors_interior_tilesandwood_01_40` → `_56`) to create stash squares.
- Sprites: 9 sprites, 0 used on the map in 0 of 4,065 cells; capacity by sprite count (no sprite is used on the map): 7 (9)
- Rooms (5 defs): `all`, bedroom (11,303), BoozeCache1 (0), GunCache1 (0), ShotgunCache1 (0)
- Procedural lists (5): PlankStashGun (2), PlankStashMisc (2), PlankStashMagazine (1), PlankStashMoney (2), PlankStashGold (2); plus 3 direct table(s)
- Top loot: Money 13.4%, 12g Round 9.4%, SN38 Revolver 3.9%, Magazine: HottieZ 3.4%, M9 Pistol 3.1%, Box of .357 Magnum Rounds 3.1%, Box of .38 Special Rounds 3.1%, Box of .44 Magnum Rounds 3.1%, Box of .45 ACP Rounds 3.1%, Box of 12g Rounds 3.1%, Box of 9x19mm Rounds 3.1%, Credit Card 3.1%

### postbox — Mailbox
- storage · capacity 5 · icon Container_MailBox · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 8 sprites, 8 used on the map in 330 of 4,065 cells; capacity by map use: 5 (651) · 15 (141) — e.g. Mail Box, Public Mail Box
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Flier 43.5%, Newspaper 12.8%, Brochure 10.2%, Mail 10.2%, Magazine 5.1%, Handwritten Letter 2.6%, Parking Ticket 2.6%, Speeding Ticket 2.6%, Birthday Card 1.3%, Parcel - Extra Small 1.3%, Sympathy Card 1.3%, Parcel - Medium 0.6%

### ShotgunBox — ?
- storage · capacity 7 · icon - · CLONE-Z: GunLocker
- Stash-map cache type: an annotated stash map (`StashDescriptions`) places the container and its `spawnTable` cache room fills it.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): sprite floors_interior_tilesandwood_01_61 cap 7 ×2 (ShotgunCache1); sprite floors_interior_tilesandwood_01_62 cap 7 ×2 (ShotgunCache1); sprite carpentry_01_16 cap 60 ×1 (SurvivorCache1); item Duffel Bag (Bag_DuffelBagTINT) cap 18 ×1 (ShotgunCache1); sprite carpentry_01_16 cap 60 ×1 (ShotgunCache1)
- Rooms (2 defs): ShotgunCache1 (0), ShotgunCache2 (0)
- Direct tables only (2).
- Top loot: Box of 12g Rounds 81.5%, JS-2000 Shotgun 12.2%, Double Barrel Shotgun 3.0%, Choke Tube - Full 0.7%, Choke Tube - Improved 0.7%, Recoil Pad 0.7%, Shells Bandolier 0.6%, Screwdriver 0.5%

### smallbox — Box
- storage · capacity 10 · icon Container_Crate · CLONE-Z: Crate
- Engine: registered `ContainerType`.
- Sprites: 6 sprites, 6 used on the map in 274 of 4,065 cells; capacity by map use: 10 (616) — e.g. Cartboard Box, Battered Cartboard Box, Vegetable Basket
- Rooms (54 defs): `all`, bathroom (15,472), bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904); +47 more
- Procedural lists (138): CrateOfficeSupplies (4), CrateTools (4), RailYardSpikes (2), PoliceFileBox (2), BookstoreBooks (1), CarpenterTools (1), ChefTools (1), ElectronicStoreMisc (1); …; plus 2 direct table(s)
- Top loot: Paperwork 4.0%, Camera Film 3.4%, Railroad Spike 2.6%, Sheet of Paper 2.3%, Pencil 1.7%, Notepad 1.5%, Magazine 1.3%, Notebook 1.2%, Old Newspaper 1.1%, Cardboard Box 1.0%, Twine 1.0%, Whetstone 0.9%

### smallcrate — Crate
- storage · capacity 25 · icon Container_Crate · CLONE-Z: Crate
- Engine: registered `ContainerType`.
- Sprites: 2 sprites, 2 used on the map in 22 of 4,065 cells; capacity by map use: 25 (34) — e.g. Small Crate
- Rooms (4 defs): grocery (77), grocerystorage (56), cafeteriakitchen (47), producestorage (42)
- Procedural lists (27): GroceryStorageCrate1 (3), CafeteriaTrays (1), GroceryStorageCrate2 (3), GroceryStorageCrate3 (3), Empty (1), ProduceStorageEmptyBoxes (1), ProduceStorageRottenFruit (1), ProduceStorageRottenVeg (1); …
- Top loot: Plastic Tray 19.3%, Produce Box - Small 5.4%, Paper Napkins 5.3%, Produce Box - Extra Small 4.9%, Produce Box - Medium 4.0%, Cherry 3.1%, Strawberries 3.1%, Canned Fruit Cocktail 1.9%, Canned Carrots 1.9%, Canned Corn 1.9%, Canned Peaches 1.9%, Canned Peas 1.9%

### SurvivorCrate — Crate
- storage · capacity 60 · icon - · CLONE-Z: Crate
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- Stash container type (StashDescriptions) placed as crate sprite `carpentry_01_16` in the SurvivorCache rooms.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): sprite carpentry_01_16 cap 60 ×3 (SurvivorCache1)
- Rooms (3 defs): SurvivorCache1 (0), SurvivorCache2 (0), SurvivorCacheBigBuilding (0)
- Direct tables only (3).
- Top loot: Water Bottle 6.7%, Water Purification Tablets 2.5%, Beef Jerky 2.2%, Butterfly Knife 2.2%, Candle 2.2%, Compass 2.2%, Dehydrated Meat Stick 2.2%, Firestarting Block 2.2%, Granola Bar 2.2%, Gum 2.2%, Hunting Knife 2.2%, Insect Repellent 2.2%

### ToolsBox — ?
- storage · capacity 60 · icon - · CLONE-Z: ToolBench
- Stash-map cache type: an annotated stash map (`StashDescriptions`) places the container and its `spawnTable` cache room fills it.
- Capacity: most common over its stash placements; varies by stash form (see below).
- Stash forms (StashDescriptions): sprite carpentry_01_16 cap 60 ×5 (ToolsCache1); item Duffel Bag (Bag_DuffelBagTINT) cap 18 ×3 (ToolsCache1)
- Rooms (1 def): ToolsCache1 (0)
- Direct tables only (1).
- Top loot: Box of Nails 10.2%, Funnel 3.4%, Lead Pipe 3.4%, Measuring Tape 3.4%, Ratchet Wrench 3.4%, Rope 3.4%, Rubber Hose 3.4%, Screwdriver 3.4%, Steel Wool 3.4%, Tarp 3.4%, Twine 3.4%, Wood Saw 3.4%

### wheeliebin — ?
- storage · capacity 50 · icon - · CLONE-Z: -
- Created only by the Java class `IsoWheelieBin` (a pushable bin; contents weight slows pushing, `/ 500` in `update`); no sprite, title, icon or distribution.
- Capacity: ItemContainer default (the creating class sets no capacity).
- No Distributions.lua entry — never rolls loot.

**— bag —**

### AmmoStrap_Brown_Bullets — Bullets Bandolier
- bag · capacity 1 · weight reduction 85% · equips ammostrap · run speed ×0.98 · AcceptItemFunction `AcceptItemFunction.AmmoStrap_Bullets` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### AmmoStrap_Brown_Shells — Shells Bandolier
- bag · capacity 1 · weight reduction 85% · equips ammostrap · run speed ×0.98 · AcceptItemFunction `AcceptItemFunction.AmmoStrap_Shells` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### AmmoStrap_Bullets — Bullets Bandolier
- bag · capacity 1 · weight reduction 85% · equips ammostrap · run speed ×0.98 · AcceptItemFunction `AcceptItemFunction.AmmoStrap_Bullets` · CLONE-Z: Sack
- Spawns in 18 room defs: garagestorage (1,318), changeroom (229), security (122), hunting (26), gunstore (21), armystorage (20); +12 more
- Contents (rolls 4): 5.56x45mm Round 100.0%

### AmmoStrap_Bullets_308 — Bullets Bandolier
- bag · capacity 1 · weight reduction 85% · equips ammostrap · run speed ×0.98 · AcceptItemFunction `AcceptItemFunction.AmmoStrap_Bullets` · CLONE-Z: Sack
- Spawns in 1 room def: policeswat (2)
- Contents (rolls 4): 7.62x51mm Round 100.0%

### AmmoStrap_Bullets_38 — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): .38 Special Round 100.0%

### AmmoStrap_Bullets_44 — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): .44 Magnum Round 100.0%

### AmmoStrap_Bullets_45 — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): .45 ACP Round 100.0%

### AmmoStrap_Bullets_9mm — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): 9x19mm Round 100.0%

### AmmoStrap_Shells — Shells Bandolier
- bag · capacity 1 · weight reduction 85% · equips ammostrap · run speed ×0.98 · AcceptItemFunction `AcceptItemFunction.AmmoStrap_Shells` · CLONE-Z: Sack
- Spawns in 20 room defs: garagestorage (1,318), changeroom (229), security (122), gunstore (21), armystorage (20), policestorage (19); +14 more
- Contents (rolls 4): 12g Round 100.0%

### Bag_ALICE_BeltSus — ALICE Belt and Suspenders
- bag · capacity 6 · weight reduction 80% · equips webbing · run speed ×0.96 · MaxItemSize `1.3` · CLONE-Z: Sack
- Spawns in 1 room def: SafehouseLoot_Late (0)
- Contents (rolls 1): Gum 28.6%, First Aid Kit - Military 11.4%, Gas Mask Filter 5.7%, Alcohol Wipes 2.9%, Bandage 2.9%, Canteen 2.9%, Chewing Tobacco 2.9%, Compass 2.9%, Dehydrated Meat Stick 2.9%, Earbuds 2.9%, Flashlight - Angle Headed 2.9%, Granola Bar 2.9%

### Bag_ALICE_BeltSus_Camo — ALICE Belt and Suspenders
- bag · capacity 6 · weight reduction 80% · equips webbing · run speed ×0.96 · MaxItemSize `1.3` · CLONE-Z: Sack
- Spawns in 6 room defs: garagestorage (1,318), armytent (40), pawnshop (9), armysurplus (8), oldarmy (5), pawnshopoffice (1)
- No distribution entry — spawns empty.

### Bag_ALICE_BeltSus_Green — ALICE Belt and Suspenders
- bag · capacity 6 · weight reduction 80% · equips webbing · run speed ×0.96 · MaxItemSize `1.3` · CLONE-Z: Sack
- Spawns in 6 room defs: garagestorage (1,318), armytent (40), pawnshop (9), armysurplus (8), oldarmy (5), pawnshopoffice (1)
- No distribution entry — spawns empty.

### Bag_ALICEpack — Military Backpack
- bag · capacity 28 · weight reduction 85% · weight 2.0 · equips back · run speed ×0.94 · CLONE-Z: Sack
- Spawns in 23 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +17 more
- Contents (rolls 1): Gum 10.7%, Bandage - Adhesive 8.5%, Compass 8.5%, Granola Bar 8.5%, Alcohol Wipes 4.3%, Beef Jerky 4.3%, Dehydrated Meat Stick 4.3%, Matchbook 4.3%, Plastic Spork 4.3%, Poncho 4.3%, Whistle 4.3%, Bandana 4.3%

### Bag_ALICEpack_Army — Military Backpack
- bag · capacity 28 · weight reduction 85% · weight 2.0 · equips back · run speed ×0.94 · CLONE-Z: Sack
- Spawns in 8 room defs: armytent (40), armystorage (20), pawnshop (9), armysurplus (8), oldarmy (5), pawnshopstorage (3); +2 more
- Contents (rolls 1): Gum 22.7%, Plastic Spork 22.7%, First Aid Kit - Military 9.1%, Gas Mask Filter 4.5%, Alcohol Wipes 2.3%, Bandage 2.3%, Canteen 2.3%, Chewing Tobacco 2.3%, Compass 2.3%, Dehydrated Meat Stick 2.3%, Earbuds 2.3%, Flashlight - Angle Headed 2.3%

### Bag_ALICEpack_DesertCamo — Military Backpack
- bag · capacity 28 · weight reduction 85% · weight 2.0 · equips back · run speed ×0.94 · CLONE-Z: Sack
- Spawns in 2 room defs: pawnshop (9), armysurplus (8)
- Contents (rolls 1): Gum 22.7%, Plastic Spork 22.7%, First Aid Kit - Military 9.1%, Gas Mask Filter 4.5%, Alcohol Wipes 2.3%, Bandage 2.3%, Canteen 2.3%, Chewing Tobacco 2.3%, Compass 2.3%, Dehydrated Meat Stick 2.3%, Earbuds 2.3%, Flashlight - Angle Headed 2.3%

### Bag_AmmoBox — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 5 room defs: armystorage (20), armysurplus (8), policeswat (2), SafehouseLoot_Late (0), `all`
- Contents (rolls 4): Box of 5.56x45mm Rounds 100.0%

### Bag_AmmoBox_308 — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 4): Box of 7.62x51mm Rounds 100.0%

### Bag_AmmoBox_38 — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Box of .38 Special Rounds 100.0%

### Bag_AmmoBox_44 — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Box of .44 Magnum Rounds 100.0%

### Bag_AmmoBox_45 — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 1 room def: policeswat (2)
- Contents (rolls 4): Box of .45 ACP Rounds 100.0%

### Bag_AmmoBox_9mm — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 4): Box of 9x19mm Rounds 100.0%

### Bag_AmmoBox_Hunting — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 7 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229), attic (50); +1 more
- Contents (rolls 3): Box of .30-30 Rounds 25.0%, Box of 12g Rounds 25.0%, Box of 5.56x45mm Rounds 25.0%, Box of 7.62x51mm Rounds 25.0%

### Bag_AmmoBox_Mixed — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 5 room defs: garagestorage (1,318), druglab (5), pawnshopoffice (1), SafehouseLoot_Late (0), SafehouseLoot_Mid (0)
- Contents (rolls 2): Box of 5.56x45mm Rounds 21.4%, Box of .38 Special Rounds 14.3%, Box of 12g Rounds 14.3%, Box of 9x19mm Rounds 14.3%, Box of .30-30 Rounds 7.1%, Box of .357 Magnum Rounds 7.1%, Box of .44 Magnum Rounds 7.1%, Box of .45 ACP Rounds 7.1%, Box of 7.62x51mm Rounds 7.1%

### Bag_AmmoBox_ShotgunShells — Ammunition Box
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 4 room defs: armystorage (20), policeswat (2), SafehouseLoot_Late (0), `all`
- Contents (rolls 4): Box of 12g Rounds 100.0%

### Bag_BaseballBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 3 room defs: sportstore (9), baseballstore (2), baseballgiftstorage (1)
- Contents (rolls 2): Baseball 16.7%, Gum 16.7%, Baseball Bat 8.4%, Metal Baseball Bat 8.4%, Pencil 8.4%, Notepad 6.0%, Sports Bottle 6.0%, Whistle 6.0%, Money 3.6%, Athletic Cup 2.4%, Bath Towel 2.4%, Clipboard 2.4%

### Bag_BigHikingBag — Big Hiking Backpack
- bag · capacity 22 · weight reduction 80% · weight 1.5 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 25 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +19 more
- Contents (rolls 2): Gum 10.7%, Bandage - Adhesive 8.5%, Granola Bar 8.5%, Alcohol Wipes 4.3%, Apricots (Dried) 4.3%, Compass 4.3%, Dehydrated Meat Stick 4.3%, Insect Repellent 4.3%, Peanuts 4.3%, Socks - Heavy 4.3%, Sports Bottle 4.3%, Sunflower Seeds 4.3%

### Bag_BigHikingBag_Travel — Big Hiking Backpack
- bag · capacity 22 · weight reduction 80% · weight 1.5 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 5 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50)
- Contents (rolls 2): Gum 10.7%, Bandage - Adhesive 8.5%, Granola Bar 8.5%, Alcohol Wipes 4.3%, Apricots (Dried) 4.3%, Compass 4.3%, Dehydrated Meat Stick 4.3%, Insect Repellent 4.3%, Peanuts 4.3%, Socks - Heavy 4.3%, Sports Bottle 4.3%, Sunflower Seeds 4.3%

### Bag_BirthdayBasket — Basket
- bag · capacity 6 · weight reduction 50% · weight 1.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Birthday Card 21.9%, CD 5.5%, Comic Book 5.5%, Diary 5.5%, Earbuds 5.5%, Handheld Game Console 5.5%, Headphones 5.5%, Paperback 5.5%, Toys-R-Mine Walkie Talkie 5.5%, Book 2.7%, Brick Toys 2.7%, Crayons 2.7%

### Bag_BowlingBallBag — Bowling Ball Bag
- bag · capacity 6 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: changeroom (229), bowlingalley (5)
- Contents (rolls 2): Pencil 26.0%, Bowling Shoes 18.6%, Notepad 18.6%, Money 11.1%, Bath Towel 7.4%, Fingerless Gloves - Leather 7.4%, Bowling Shirt - Blue 1.5%, Bowling Shirt - Brown 1.5%, Bowling Shirt - Green 1.5%, Bowling Shirt - Lime Green 1.5%, Bowling Shirt - Pink 1.5%, Bowling Shirt - White 1.5%

### Bag_BreakdownBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Electrical Wire 5.4%, Flashlight 5.4%, Funnel 5.4%, Lug Wrench 5.4%, Rope 5.4%, Rubber Hose 5.4%, Screwdriver 5.4%, Sheet 5.4%, Tire Iron 5.4%, Tire Pump 5.4%, Toilet Paper 5.4%, Whistle 5.4%

### Bag_BurglarBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 9 room defs: derelict (1,038), policestorage (19), drugshack (7), druglab (5), evidenceroom (2), policeswat (2); +3 more
- Contents (rolls 1): Crowbar 14.0%, Duct Tape 9.8%, Rope 9.8%, Balaclava 7.0%, Hacksaw 7.0%, Leather Gloves 7.0%, ID Card - Blank 4.2%, Ball-peen Hammer 2.8%, Bolt Cutters 2.8%, Claw Hammer 2.8%, Club Hammer 2.8%, Pliers 2.8%

### Bag_ChestRig — Chest Rig
- bag · capacity 5 · weight reduction 85% · equips webbing · run speed ×0.99 · MaxItemSize `1.3` · CLONE-Z: Sack
- Spawns in 6 room defs: pawnshop (9), armysurplus (8), pawnshopoffice (1), SafehouseLoot (0), SafehouseLoot_Late (0), SafehouseLoot_Mid (0)
- Contents (rolls 1): empty table

### Bag_ChestRig_Tarp — Tarp Chest Rig
- bag · capacity 5 · weight reduction 85% · equips webbing · run speed ×0.99 · MaxItemSize `1.3` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ClothSatchel_Burlap — Crafted Burlap Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ClothSatchel_Cotton — Crafted Cotton Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ClothSatchel_Denim — Crafted Denim Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ClothSatchel_DenimBlack — Crafted Denim Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ClothSatchel_DenimLight — Crafted Denim Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ConstructionBag — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of Nails 6.9%, Duct Tape 6.9%, Measuring Tape 6.9%, Rag 6.9%, Claw Hammer 4.9%, Dust Mask 4.9%, Hacksaw 4.9%, Hard Hat 4.9%, Pliers 4.9%, Safety Goggles 4.9%, Screwdriver 4.9%, Wrench 4.9%

### Bag_CraftedFramepack_Large — Simple Framepack
- bag · capacity 20 · weight reduction 75% · weight 2.0 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_CraftedFramepack_Large2 — Framepack
- bag · capacity 25 · weight reduction 80% · weight 3.0 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_CraftedFramepack_Large3 — Large Framepack
- bag · capacity 35 · weight reduction 75% · weight 4.0 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_CraftedFramepack_Small — Small Simple Framepack
- bag · capacity 15 · weight reduction 70% · weight 1.5 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_CrudeLeatherBag — Small Leather Backpack
- bag · capacity 14 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_CrudeTarpBag — Small Tarp Backpack
- bag · capacity 14 · weight reduction 60% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_Dancer — Tote Bag
- bag · capacity 6 · weight reduction 50% · weight 0.5 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 10 room defs: derelict (1,038), policestorage (19), stripclubvip (12), drugshack (7), stripclub (6), evidenceroom (2); +4 more
- Contents (rolls 1): Gum 18.1%, Can of Soda 12.6%, Bra - Strapless 7.9%, Bra 6.5%, Caffeine Pills 5.4%, Underpants 4.0%, Cigarette Pack 3.6%, Diary 1.8%, Hair Dryer 1.8%, Hair Gel 1.8%, Hair Iron 1.8%, Hair Spray 1.8%

### Bag_DeadMice — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 12): Dead Mouse 100.0%

### Bag_DeadRats — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 8): Dead Rat 100.0%

### Bag_DeadRoaches — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 20): Cockroach 100.0%

### Bag_DoctorBag — Doctor Bag
- bag · capacity 8 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Alcohol Wipes 16.9%, Bandage - Adhesive 16.9%, Tongue Depressor 16.9%, Stethoscope 9.4%, Bandage 5.6%, Painkillers 5.6%, Suture Needle 5.6%, Forceps 3.8%, Antidepressants 1.9%, Cold Pack 1.9%, Cotton Balls 1.9%, Medical Shears 1.9%

### Bag_DoctorBag_Money — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Money 54.9%, Money Bundle 42.4%, Stock Certificate 2.5%, Pouch 0.2%

### Bag_DuffelBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 6 room defs: hunting (26), fishingstorage (12), campingstorage (8), camping (6), outdoorsupply (0), outdoorsupply_storage (0)
- Contents (rolls 1): Gum 31.7%, Bath Towel 6.3%, CD 6.3%, Long Shorts - Sport 6.3%, Shorts - Sport 6.3%, Socks 6.3%, Sport Pants 6.3%, T-shirt - Sports 6.3%, Tank Top 6.3%, Baseball Cap 3.2%, CD Player 3.2%, Earbuds 3.2%

### Bag_DuffelBagTINT — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 32 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), laundry (1,193); +26 more
- Contents (rolls 1): Gum 31.7%, Bath Towel 6.3%, CD 6.3%, Long Shorts - Sport 6.3%, Shorts - Sport 6.3%, Socks 6.3%, Sport Pants 6.3%, T-shirt - Sports 6.3%, Tank Top 6.3%, Baseball Cap 3.2%, CD Player 3.2%, Earbuds 3.2%
- Also a container key in 4 cache room(s) (GunCache1, GunCache2, ShotgunCache1, ToolsCache1) — those tables fill it when a stash spawns it: 12g Round 5.4%, Box of 12g Rounds 4.8%, M9 Pistol 4.8%, SN38 Revolver 4.8%, Box of Nails 3.6%, Sawed-off JS-2000 Shotgun 3.0%

### Bag_FannyPackBack — Fanny Pack
- bag · capacity 1 · weight reduction 85% · weight 0.2 · equips fannypackback · run speed ×0.98 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Gum 11.7%, Key Ring 11.7%, Vehicle Key 11.7%, Tissue 9.4%, Wallet 8.2%, Alcohol Wipes 4.7%, Bandage - Adhesive 4.7%, Comb 4.7%, Lighter - Disposable 4.7%, ID Card 4.1%, Granola Bar 2.3%, Milk Chocolate Bar 2.3%

### Bag_FannyPackBack_Hide — Hide Fanny Pack
- bag · capacity 1 · weight reduction 65% · weight 0.2 · equips fannypackback · run speed ×0.98 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_FannyPackBack_Tarp — Tarp Fanny Pack - Back
- bag · capacity 1 · weight reduction 60% · weight 0.2 · equips fannypackback · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_FannyPackFront — Fanny Pack
- bag · capacity 1 · weight reduction 85% · weight 0.2 · equips fannypackfront · run speed ×0.98 · CLONE-Z: Sack
- Spawns in 37 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295); +31 more
- Contents (rolls 4): Gum 11.7%, Key Ring 11.7%, Vehicle Key 11.7%, Tissue 9.4%, Wallet 8.2%, Alcohol Wipes 4.7%, Bandage - Adhesive 4.7%, Comb 4.7%, Lighter - Disposable 4.7%, ID Card 4.1%, Granola Bar 2.3%, Milk Chocolate Bar 2.3%

### Bag_FannyPackFront_Hide — Hide Fanny Pack
- bag · capacity 1 · weight reduction 65% · weight 0.2 · equips fannypackfront · run speed ×0.98 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_FannyPackFront_Tarp — Tarp Fanny Pack
- bag · capacity 1 · weight reduction 60% · weight 0.2 · equips fannypackfront · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_FishingBasket — Fishing Basket
- bag · capacity 6 · weight reduction 50% · weight 1.5 · equips back · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 14 room defs: closet (5,489), garagestorage (1,318), storageunit (695), toolstore (52), shed (35), hunting (26); +8 more
- Contents (rolls 1): Black Crappie 7.7%, Bluegill 7.7%, Green Sunfish 7.7%, Largemouth Bass 7.7%, Redear Sunfish 7.7%, Sauger 7.7%, Smallmouth Bass 7.7%, Spotted Bass 7.7%, Striped Bass 7.7%, Walleye 7.7%, White Bass 7.7%, White Crappie 7.7%

### Bag_FluteCase — Flute Case
- bag · capacity 1 · weight reduction 50% · weight 1.0 · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 14 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295), storageunit (695); +8 more
- Contents (rolls 1): Flute 100.0%

### Bag_FoodCanned — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Can Opener 13.5%, Canned Beans 5.4%, Canned Carrots 5.4%, Canned Chili 5.4%, Canned Corn 5.4%, Canned Corned Beef 5.4%, Canned Fruit Cocktail 5.4%, Canned Mushroom Soup 5.4%, Canned Peaches 5.4%, Canned Peas 5.4%, Canned Pineapple 5.4%, Canned Potato 5.4%

### Bag_FoodSnacks — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Beef Jerky 9.7%, Dehydrated Meat Stick 9.7%, Granola Bar 9.7%, Milk Chocolate Bar 9.7%, Licorice 6.5%, Chips - Barbecue 3.2%, Chips - Plain 3.2%, Chips - Salt & Vinegar 3.2%, Chips - Sour Cream & Onion 3.2%, Choco Cakes 3.2%, Gummy Bears 3.2%, Gummy Worms 3.2%

### Bag_FruitBasket — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Apple 22.6%, Orange 22.6%, Banana 6.5%, Cherry 6.5%, Grapefruit 6.5%, Grapes 6.5%, Mango 6.5%, Peach 6.5%, Pear 6.5%, Strawberries 6.5%, Pineapple 3.2%

### Bag_GardenBasket — Garden Basket
- bag · capacity 6 · weight reduction 50% · weight 1.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 17 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695), barn (170); +11 more
- Contents (rolls 3): Agriculture I: "Better Gardening" 3.5%, Gardening Trowel 3.5%, Hand Drill 3.5%, Hand Fork 3.5%, Kneepad 3.5%, Leather Gloves 3.5%, Measuring Tape 3.5%, Pliers 3.5%, Rubber Hose 3.5%, Scissors 3.5%, Slug Repellent 3.5%, Summer Hat 3.5%

### Bag_GolfBag — Golf Bag
- bag · capacity 18 · weight reduction 65% · weight 1.2 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 4 room defs: closet (5,489), sportstore (9), gymstorage (6), golfstore (1)
- Contents (rolls 2): Golf Tee 36.1%, Golf Ball 29.6%, Golf Club 29.6%, Notepad 1.6%, Pencil 1.6%, Visor 0.7%, Cigarette Pack 0.3%, Golf Cap 0.3%, Leather Gloves 0.2%

### Bag_GolfBag_Melee — Golf Bag
- bag · capacity 18 · weight reduction 65% · weight 1.2 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 6 room defs: SafehouseLoot (0), SafehouseLoot_Late (0), SafehouseLoot_Mid (0), SurvivorCache1 (0), SurvivorCache2 (0), SurvivorCacheBigBuilding (0)
- Contents (rolls 4): Baseball Bat 11.1%, Field Hockey Stick 11.1%, Ice Hockey Stick 11.1%, Lacrosse Stick 11.1%, Metal Baseball Bat 11.1%, Crowbar 5.6%, Hatchet 5.6%, Wooden Spear 5.6%, Garden Fork 3.4%, Baseball Bat with Nails 3.3%, Firefighter Axe 3.3%, Garden Hoe 3.3%

### Bag_Gunny — Gunny Sack
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_HideSack — Hide Sack
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_HideSatchel — Crafted Hide Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_HideSlingBag — Hide Sling Bag
- bag · capacity 14 · weight reduction 60% · weight 0.8 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_HikingBag_Travel — Hiking Backpack
- bag · capacity 20 · weight reduction 75% · weight 1.2 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Spawns in 5 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50)
- Contents (rolls 2): Gum 10.7%, Bandage - Adhesive 8.5%, Granola Bar 8.5%, Alcohol Wipes 4.3%, Apricots (Dried) 4.3%, Compass 4.3%, Dehydrated Meat Stick 4.3%, Insect Repellent 4.3%, Peanuts 4.3%, Socks - Heavy 4.3%, Sports Bottle 4.3%, Sunflower Seeds 4.3%

### Bag_HospitalBasket — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Teddy Bear 57.7%, Sympathy Card 42.0%, Boris The Badger 0.03%, Eye of Cthulhu 0.03%, Fluffyfoot The Bunny 0.03%, Freddy The Fox 0.03%, Furbert The Squirrel 0.03%, Jacques The Beaver 0.03%, Moley The Mole 0.03%, Pancake The Hedgehog 0.03%, Pancho the Dog 0.03%, Plumpabug the Aphid 0.03%

### Bag_HydrationBackpack — Hydration Pack
- bag · capacity 12 · weight reduction 85% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 18 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229), attic (50); +12 more
- No distribution entry — spawns empty.

### Bag_HydrationBackpack_Camo — Hydration Pack
- bag · capacity 12 · weight reduction 85% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_InmateEscapedBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Rag 16.6%, Sheet Rope 16.6%, Glass Shiv 11.8%, Rope 11.8%, Shiv 11.8%, Duct Tape 4.7%, Ice Pick 4.7%, Simple Metal Knife 4.7%, Zip Ties 4.7%, Awl 2.4%, Claw Hammer 2.4%, File 2.4%

### Bag_JanitorToolbox — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 5 room defs: bathroom (15,472), janitor (688), gasstorage (33), schoolstorage (15), zippeestorage (12)
- Contents (rolls 3): Pipe Wrench 14.6%, Claw Hammer 7.3%, Flashlight 7.3%, Hacksaw 7.3%, Measuring Tape 7.3%, Pliers 7.3%, Screwdriver 7.3%, Wrench 7.3%, Rag 5.8%, Rag (Dirty) 5.8%, Crowbar 2.9%, Duct Tape 2.9%

### Bag_Laundry — Laundry Bag
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Pants 7.3%, T-shirt 7.0%, Bath Towel 6.1%, Sheet 6.1%, Long Socks 6.1%, Socks 6.1%, Underpants 5.8%, Tank Top 3.0%, Knee-length Skirt 2.4%, Long Skirt 2.4%, Skirt 2.4%, Long Sleeve T-shirt 2.4%

### Bag_LaundryHospital — Laundry Bag
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Sheet 59.1%, Bath Towel 16.1%, Hospital Gown 16.1%, Medical Scrubs 4.3%, Pants - Medical 4.3%

### Bag_LaundryLinen — Laundry Bag
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Bath Towel 50.0%, Sheet 50.0%

### Bag_Mail — Mail Bag
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: post (32), poststorage (9)
- Contents (rolls 4): Magazine 16.5%, Brochure 13.2%, Flier 13.2%, Newspaper 6.6%, Parcel - Extra Small 6.6%, Parcel - Small 5.3%, Parcel - Medium 2.6%, Magazine: Crossword 1.3%, Magazine: Wordsearch 1.3%, Parcel - Large 1.3%, Birthday Card 0.7%, Handwritten Letter 0.7%

### Bag_MedicalBag — Trauma Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 17 room defs: bathroom (15,472), garagestorage (1,318), breakroom (321), changeroom (229), hospitalroom (64), armystorage (20); +11 more
- Contents (rolls 2): Alcohol Wipes 13.8%, Bandage 13.8%, Bandage - Adhesive 13.8%, Suture Needle 13.8%, Painkillers 9.2%, Stethoscope 7.7%, Cotton Balls 4.6%, Tongue Depressor 4.6%, Forceps 3.1%, Antidepressants 1.5%, Bottle of Disinfectant 1.5%, Cold Pack 1.5%

### Bag_Military — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 8 room defs: armytent (40), armystorage (20), pawnshop (9), armysurplus (8), oldarmy (5), pawnshopstorage (3); +2 more
- Contents (rolls 1): Gum 22.7%, Plastic Spork 22.7%, First Aid Kit - Military 9.1%, Gas Mask Filter 4.5%, Alcohol Wipes 2.3%, Bandage 2.3%, Canteen 2.3%, Chewing Tobacco 2.3%, Compass 2.3%, Dehydrated Meat Stick 2.3%, Earbuds 2.3%, Flashlight - Angle Headed 2.3%

### Bag_MoneyBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 17 room defs: bedroom (11,303), derelict (1,038), prisoncells (308), policestorage (19), policelocker (9), drugshack (7); +11 more
- Contents (rolls 4): Money 54.9%, Money Bundle 42.4%, Stock Certificate 2.5%, Pouch 0.2%

### Bag_NormalHikingBag — Hiking Backpack
- bag · capacity 20 · weight reduction 75% · weight 1.2 · equips back · run speed ×0.96 · CLONE-Z: Sack
- Spawns in 22 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +16 more
- Contents (rolls 2): Gum 10.7%, Bandage - Adhesive 8.5%, Granola Bar 8.5%, Alcohol Wipes 4.3%, Apricots (Dried) 4.3%, Compass 4.3%, Dehydrated Meat Stick 4.3%, Insect Repellent 4.3%, Peanuts 4.3%, Socks - Heavy 4.3%, Sports Bottle 4.3%, Sunflower Seeds 4.3%

### Bag_PicnicBasket — Picnic Basket
- bag · capacity 6 · weight reduction 50% · weight 1.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 3 room defs: livingroom (10,624), ww_generalstore (2), `all`
- Contents (rolls 2): Juice Box 6.7%, Baguette 2.8%, Butter Knife 2.8%, Chocolate Milk - Personal-sized 2.8%, Croissant 2.8%, Fork 2.8%, Fruit Muffin 2.8%, Granola Bar 2.8%, Milk - Personal-sized 2.8%, Muffin 2.8%, Spoon 2.8%, Yogurt 2.8%

### Bag_Police — Police Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 9 room defs: security (122), policestorage (19), policelocker (9), policeswat (2), captainoffice (1), pawnshopoffice (1); +3 more
- Contents (rolls 4): Holster 13.2%, JS-2000 Shotgun 13.2%, M9 Pistol 13.2%, Nightstick 13.2%, Patrol Revolver 13.2%, Bulletproof Vest - Police 6.6%, Holster - Ankle 6.6%, Holster - Shoulder 6.6%, Shells Bandolier 6.6%, JS-14 Rifle 2.6%, JS-3T Tactical Shotgun 2.6%, Magnum 2.6%

### Bag_ProtectiveCase — Protective Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 7 room defs: electronicstore (45), musicstore (11), pawnshop (9), camping (6), electronicsstorage (6), outdoorsupply (0); +1 more
- No distribution entry — spawns empty.

### Bag_ProtectiveCase_Survivalist — Protective Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Bandage - Adhesive 4.7%, Beef Jerky 4.7%, Candle 4.7%, Compass 4.7%, Dehydrated Meat Stick 4.7%, Fishing Hook 4.7%, Fishing Line 4.7%, Frying Pan 4.7%, Granola Bar 4.7%, Magnesium Firestarter 4.7%, Matchbook 4.7%, Needle 4.7%

### Bag_ProtectiveCase_Tools — Protective Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Flashlight - Angle Headed 3.2%, Funnel 3.2%, Marker - Black 3.2%, Measuring Tape 3.2%, Nails 3.2%, Nuts and Bolts 3.2%, Pliers 3.2%, Rubber Hose 3.2%, Screwdriver 3.2%, Screws 3.2%, Twine 3.2%, Whetstone 3.2%

### Bag_ProtectiveCaseBulky — Bulky Protective Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 7 room defs: electronicstore (45), musicstore (11), pawnshop (9), camping (6), electronicsstorage (6), outdoorsupply (0); +1 more
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseBulky_Audio — Bulky Protective Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Speaker 34.5%, Microphone 27.3%, Electrical Wire 12.7%, Headphones 12.7%, Power Bar 12.7%

### Bag_ProtectiveCaseBulky_HAMRadio1 — Bulky Protective Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Premium Technologies Ham Radio 100.0%

### Bag_ProtectiveCaseBulky_SCBA — Bulky Protective Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): SCBA (No Tank) 62.5%, Oxygen Tank 37.5%

### Bag_ProtectiveCaseBulky_Survivalist — Bulky Military Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Bandage - Adhesive 4.1%, Beef Jerky 4.1%, Candle 4.1%, Compass 4.1%, Dehydrated Meat Stick 4.1%, Fishing Hook 4.1%, Fishing Line 4.1%, Flashlight 4.1%, Frying Pan 4.1%, Granola Bar 4.1%, Magnesium Firestarter 4.1%, Matchbook 4.1%

### Bag_ProtectiveCaseBulkyAmmo — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 4 room defs: garagestorage (1,318), armysurplus (8), pawnshopoffice (1), SafehouseLoot_Late (0)
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_308 — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_38 — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_44 — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_45 — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: policeswat (2)
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_556 — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_9mm — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_Hunting — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyAmmo_ShotgunShells — Bulky Ammo Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), policeswat (2), `all`
- Contents (rolls 1): Ammunition Box 100.0%

### Bag_ProtectiveCaseBulkyHazard — Biohazard Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 6 room defs: medical (168), medicaloffice (30), hospitalstorage (15), dentist (11), dentiststorage (5), oldmedical (3)
- Contents (rolls 1): Bandage (Dirty) 55.6%, Cold Pack 38.9%, Scalpel 5.6%

### Bag_ProtectiveCaseBulkyMilitary — Bulky Military Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: gunstore (21), armysurplus (8)
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseBulkyMilitary_HAMRadio2 — Bulky Military Container
- bag · capacity 16 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 5 room defs: office (3,904), armystorage (20), pawnshop (9), pawnshopstorage (3), pawnshopoffice (1)
- Contents (rolls 1): US ARMY COMM. Ham Radio 100.0%

### Bag_ProtectiveCaseMilitary — Protective Military Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 8 room defs: office (3,904), armytent (40), gunstore (21), armystorage (20), pawnshop (9), armysurplus (8); +2 more
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseMilitary_Medical — Protective Case - Military
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseMilitary_Tools — Protective Case - Military
- bag · capacity 7 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Flashlight - Angle Headed 3.2%, Funnel 3.2%, Marker - Black 3.2%, Measuring Tape 3.2%, Nails 3.2%, Nuts and Bolts 3.2%, Pliers 3.2%, Rubber Hose 3.2%, Screwdriver 3.2%, Screws 3.2%, Twine 3.2%, Whetstone 3.2%

### Bag_ProtectiveCaseSmall — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 7 room defs: electronicstore (45), musicstore (11), pawnshop (9), camping (6), electronicsstorage (6), outdoorsupply (0); +1 more
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseSmall_Armorer — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 4 room defs: garagestorage (1,318), druglab (5), pawnshopoffice (1), SafehouseLoot_Late (0)
- Contents (rolls 1): Screwdriver 34.1%, Choke Tube - Full 11.4%, Recoil Pad 11.4%, Red Dot Sight 11.4%, x2 Scope 11.4%, Bullets Bandolier 4.5%, Choke Tube - Improved 4.5%, Shells Bandolier 4.5%, x4 Scope 4.5%, x8 Scope 2.3%

### Bag_ProtectiveCaseSmall_Electronics — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Battery 14.9%, Cordless Phone 12.4%, Earbuds 12.4%, Flashlight 12.4%, Headphones 12.4%, Power Bar 12.4%, Premium Tech. Walkie Talkie 12.4%, CD Player 5.0%, Handheld Game Console 5.0%, Box of Batteries 1.0%

### Bag_ProtectiveCaseSmall_FirstAid — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Alcohol Wipes 19.5%, Bandage - Adhesive 19.5%, Suture Needle 19.5%, Bandage 7.3%, Surgical Gloves 7.3%, Cotton Balls 4.9%, Forceps 4.9%, Adhesive Tape 2.4%, Cold Pack 2.4%, Medical Shears 2.4%, Painkillers 2.4%, Safety Scissors 2.4%

### Bag_ProtectiveCaseSmall_KeyCutting — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: garagestorage (1,318), shed (35)
- Contents (rolls 2): Blank Key 64.9%, Small File Set 32.5%, Magazine: The Cheapskate's John Spiral - Escaping Captivity 2.6%

### Bag_ProtectiveCaseSmall_Pistol1 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: garagestorage (1,318), gunstore (21)
- Contents (rolls 1): Box of 9x19mm Rounds 45.8%, M9 Magazine 28.0%, M9 Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### Bag_ProtectiveCaseSmall_Pistol2 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .45 ACP Rounds 45.8%, M1911 Auto Magazine 28.0%, M1911 Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### Bag_ProtectiveCaseSmall_Pistol3 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .44 Magnum Rounds 45.8%, B-F Magazine 28.0%, B-F Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### Bag_ProtectiveCaseSmall_Revolver1 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .38 Special Rounds 64.1%, SN38 Revolver 35.6%, Holster - Shoulder 0.4%

### Bag_ProtectiveCaseSmall_Revolver2 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .357 Magnum Rounds 64.1%, Patrol Revolver 35.6%, Holster - Shoulder 0.4%

### Bag_ProtectiveCaseSmall_Revolver3 — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .44 Magnum Rounds 63.2%, Magnum 35.1%, x2 Scope 1.4%, Holster - Shoulder 0.4%

### Bag_ProtectiveCaseSmall_Survivalist — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Fishing Hook 11.6%, Fishing Line 11.6%, Needle 11.6%, Thread 11.6%, Bandage - Adhesive 8.3%, Candle 8.3%, Compass 8.3%, Magnesium Firestarter 8.3%, P38 Can Opener 8.3%, Matchbook 3.3%, Penlight 3.3%, Survival Magazine Page 1.7%

### Bag_ProtectiveCaseSmall_WalkieTalkie — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Premium Tech. Walkie Talkie 70.1%, Battery 28.0%, Box of Batteries 1.9%

### Bag_ProtectiveCaseSmall_WalkieTalkiePolice — Small Protective Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Tactical Walkie Talkie 70.1%, Battery 28.0%, Box of Batteries 1.9%

### Bag_ProtectiveCaseSmallMilitary — Small Protective Military Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: gunstore (21), armysurplus (8)
- No distribution entry — spawns empty.

### Bag_ProtectiveCaseSmallMilitary_FirstAid — Small Protective Case - Military
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: armytent (40)
- Contents (rolls 1): Alcohol Wipes 19.5%, Bandage - Adhesive 19.5%, Suture Needle 19.5%, Bandage 7.3%, Surgical Gloves 7.3%, Cotton Balls 4.9%, Forceps 4.9%, Adhesive Tape 2.4%, Cold Pack 2.4%, Medical Shears 2.4%, Painkillers 2.4%, Safety Scissors 2.4%

### Bag_ProtectiveCaseSmallMilitary_Pistol1 — Small Protective Case - Military
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: armystorage (20), `all`
- Contents (rolls 1): Box of 9x19mm Rounds 45.8%, M9 Magazine 28.0%, M9 Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### Bag_ProtectiveCaseSmallMilitary_WalkieTalkie — Small Protective Case - Military
- bag · capacity 4 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 5 room defs: office (3,904), armystorage (20), pawnshop (9), pawnshopstorage (3), pawnshopoffice (1)
- Contents (rolls 1): US Army Walkie Talkie 70.1%, Battery 28.0%, Box of Batteries 1.9%

### Bag_RifleCase — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 2 room defs: gunstore (21), pawnshop (9)
- No distribution entry — spawns empty.

### Bag_RifleCase_Police — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 7.62x51mm Rounds 61.9%, MSR788 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### Bag_RifleCase_Police2 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 7.62x51mm Rounds 44.9%, M1A Magazine 27.4%, M1A Rifle 24.9%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### Bag_RifleCase_Police3 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 5.56x45mm Rounds 44.7%, M16 Magazine 27.3%, M16 Assault Rifle 24.8%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, Red Dot Sight 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### Bag_RifleCaseCloth — Cloth Gun Case
- bag · capacity 7 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 12 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +6 more
- Contents (rolls 1): Box of 5.56x45mm Rounds 61.9%, MSR700 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### Bag_RifleCaseCloth2 — Cloth Gun Case
- bag · capacity 7 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 7.62x51mm Rounds 61.9%, MSR788 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### Bag_RifleCaseClothCamo — Cloth Gun Case
- bag · capacity 7 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 4 room defs: gunstore (21), armystorage (20), SafehouseLoot_Late (0), `all`
- Contents (rolls 1): Box of 7.62x51mm Rounds 61.9%, MSR788 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### Bag_RifleCaseGreen — Gun Case - Military
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 4 room defs: gunstore (21), armystorage (20), SafehouseLoot_Late (0), `all`
- Contents (rolls 1): Box of 5.56x45mm Rounds 44.7%, M16 Magazine 27.3%, M16 Assault Rifle 24.8%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, Red Dot Sight 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### Bag_RifleCaseGreen2 — Gun Case - Military
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 7.62x51mm Rounds 44.9%, M1A Magazine 27.4%, M1A Rifle 24.9%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### Bag_Satchel — Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 30 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295); +24 more
- Contents (rolls 1): Book 24.7%, Sheet of Paper 24.7%, Comic Book 12.4%, Gum 4.1%, Paperback 4.1%, Pencil 4.1%, Pen - Black 3.3%, Pen - Blue 3.3%, Pen - Red 3.3%, Eraser 2.5%, Rubber Band 2.5%, Adhesive Tape 1.6%

### Bag_Satchel_Fishing — Fishing Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 5 room defs: closet (5,489), garagestorage (1,318), storageunit (695), shed (35), campworkerdorm (0)
- Contents (rolls 2): Fishing Hook 14.2%, Fishing Line 14.2%, Bobber 4.7%, Bottle Opener 4.7%, Fillet Knife 4.7%, Fishing I: "Dean's Good Fishing Guide" 4.7%, Flashlight - Angle Headed 4.7%, Insect Repellent 4.7%, Jig Lure 4.7%, Minnow Lure 4.7%, Pliers 4.7%, Pocket Knife 4.7%

### Bag_Satchel_Leather — Leather Satchel
- bag · capacity 13 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Book 25.6%, Sheet of Paper 25.6%, Comic Book 12.8%, Gum 4.3%, Paperback 4.3%, Pencil 4.3%, Pen - Black 3.4%, Pen - Blue 3.4%, Pen - Red 3.4%, Eraser 2.6%, Rubber Band 2.6%, Adhesive Tape 1.7%

### Bag_Satchel_Mail — Mail Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: post (32), poststorage (9)
- Contents (rolls 4): Brochure 14.6%, Flier 14.6%, Old Newspaper 7.3%, Parcel - Extra Small 7.3%, Magazine 7.3%, Parcel - Small 5.9%, Parcel - Medium 2.9%, Magazine: Crossword 1.5%, Magazine: Wordsearch 1.5%, Parcel - Large 1.5%, Birthday Card 0.7%, Handwritten Letter 0.7%

### Bag_Satchel_Medical — Medical Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 17 room defs: bathroom (15,472), garagestorage (1,318), breakroom (321), changeroom (229), hospitalroom (64), armystorage (20); +11 more
- Contents (rolls 2): Alcohol Wipes 13.8%, Bandage 13.8%, Bandage - Adhesive 13.8%, Suture Needle 13.8%, Painkillers 9.2%, Stethoscope 7.7%, Cotton Balls 4.6%, Tongue Depressor 4.6%, Forceps 3.1%, Antidepressants 1.5%, Bottle of Disinfectant 1.5%, Cold Pack 1.5%

### Bag_Satchel_Military — Military Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: pawnshop (9), armysurplus (8)
- Contents (rolls 1): First Aid Kit - Military 20.0%, Gas Mask Filter 10.0%, Alcohol Wipes 5.0%, Bandage 5.0%, Compass 5.0%, Dehydrated Meat Stick 5.0%, Earbuds 5.0%, Flashlight - Angle Headed 5.0%, Granola Bar 5.0%, Holster 5.0%, Lighter 5.0%, Military T-shirt 5.0%

### Bag_SatchelPhoto — Satchel
- bag · capacity 12 · weight reduction 65% · weight 1.0 · equips satchel · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 5 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50)
- Contents (rolls 1): Camera Film 33.9%, Camera 17.0%, Photograph 17.0%, Sheet of Paper 5.1%, High-end Camera 3.4%, Photo Album 3.4%, Book 1.7%, Gum 1.7%, Notepad 1.7%, Pager 1.7%, Pencil 1.7%, Magazine 1.7%

### Bag_SaxophoneCase — Saxophone Case
- bag · capacity 3 · weight reduction 50% · weight 1.0 · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 14 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295), storageunit (695); +8 more
- Contents (rolls 1): Saxophone 100.0%

### Bag_Schoolbag — Small Backpack
- bag · capacity 15 · weight reduction 70% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 15 room defs: livingroom (10,624), closet (5,489), motelroomoccupied (45), hunting (26), bookstore (25), secondaryhall (22); +9 more
- Contents (rolls 1): Book 29.4%, Comic Book 9.8%, Paperback 9.8%, Pencil 9.8%, Pen - Black 7.8%, Pen - Blue 7.8%, Pen - Red 7.8%, Gum 3.9%, CD 2.0%, Scissors 2.0%, CD Player 1.0%, Chips - Plain 1.0%

### Bag_Schoolbag_Kids — Small Backpack
- bag · capacity 15 · weight reduction 70% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 3 room defs: bedroom (11,303), kidsbedroom (1,295), daycare (11)
- Contents (rolls 1): Comic Book 7.8%, Crayons 7.8%, Doodle 7.8%, Handheld Game Console 7.8%, Magazine 7.8%, Paperback 7.8%, Book 3.9%, Brick Toys 3.9%, Doll 3.9%, Pencil 3.9%, Playing Cards 3.9%, Teddy Bear 3.9%

### Bag_Schoolbag_Medical — Small Backpack
- bag · capacity 12 · weight reduction 70% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_Schoolbag_Patches — Small Backpack
- bag · capacity 15 · weight reduction 70% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 3 room defs: livingroom (10,624), secondaryhall (22), elementaryschool (19)
- Contents (rolls 1): Book 29.4%, Comic Book 9.8%, Paperback 9.8%, Pencil 9.8%, Pen - Black 7.8%, Pen - Blue 7.8%, Pen - Red 7.8%, Gum 3.9%, CD 2.0%, Scissors 2.0%, CD Player 1.0%, Chips - Plain 1.0%

### Bag_Schoolbag_Travel — Small Backpack
- bag · capacity 15 · weight reduction 70% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 5 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50)
- Contents (rolls 1): Book 30.3%, Comic Book 10.1%, Paperback 10.1%, Pencil 10.1%, Pen - Black 8.1%, Pen - Blue 8.1%, Pen - Red 8.1%, CD 2.0%, Scissors 2.0%, CD Player 1.0%, Chips - Plain 1.0%, Diet Cola 1.0%

### Bag_SewingBasket — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Yarn 51.3%, Knitting Needles 40.1%, Measuring Tape 8.0%, Sewing Kit 0.6%

### Bag_SheetSlingBag — Sheet Sling Bag
- bag · capacity 10 · weight reduction 50% · weight 0.8 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_Sheriff — Sheriff Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Holster 13.2%, JS-2000 Shotgun 13.2%, M9 Pistol 13.2%, Nightstick 13.2%, Patrol Revolver 13.2%, Bulletproof Vest - Police 6.6%, Holster - Ankle 6.6%, Holster - Shoulder 6.6%, Shells Bandolier 6.6%, JS-14 Rifle 2.6%, JS-3T Tactical Shotgun 2.6%, Magnum 2.6%

### Bag_ShotgunBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): 12g Round 50.0%, JS-2000 Shotgun 27.8%, Box of 12g Rounds 22.2%

### Bag_ShotgunCase_Police — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 12g Rounds 62.5%, JS-2000 Shotgun 34.7%, Choke Tube - Full 1.4%, Choke Tube - Improved 0.7%, Shells Bandolier 0.7%

### Bag_ShotgunCaseCloth — Cloth Gun Case
- bag · capacity 7 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 12g Rounds 62.5%, JS-2000 Shotgun 34.7%, Choke Tube - Full 1.4%, Choke Tube - Improved 0.7%, Shells Bandolier 0.7%

### Bag_ShotgunCaseCloth2 — Cloth Gun Case
- bag · capacity 7 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 12g Rounds 63.8%, Double Barrel Shotgun 35.5%, Shells Bandolier 0.7%

### Bag_ShotgunCaseGreen — Gun Case - Military
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 3 room defs: armystorage (20), SafehouseLoot_Late (0), `all`
- Contents (rolls 1): Box of 12g Rounds 62.5%, JS-2000 Shotgun 34.7%, Choke Tube - Full 1.4%, Choke Tube - Improved 0.7%, Shells Bandolier 0.7%

### Bag_ShotgunDblBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): 12g Round 50.0%, Double Barrel Shotgun 27.8%, Box of 12g Rounds 22.2%

### Bag_ShotgunDblSawnoffBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 4 room defs: bar (109), barstorage (15), barkitchen (12), barcountertwiggy (1)
- Contents (rolls 1): 12g Round 50.0%, Sawed-off Double Barrel Shotgun 27.8%, Box of 12g Rounds 22.2%

### Bag_ShotgunSawnoffBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 5 room defs: bar (109), barstorage (15), barkitchen (12), dinerbackroom (2), barcountertwiggy (1)
- Contents (rolls 1): 12g Round 50.0%, Sawed-off JS-2000 Shotgun 27.8%, Box of 12g Rounds 22.2%

### Bag_Skill_Maintenance_DuffelBag — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Maintenance I: "Basic Repairs" 12.1%, Duct Tape 7.3%, Epoxy 7.3%, Fiberglass Tape 7.3%, Wood Glue 7.3%, Maintenance II: "Better Tools Made Easy" 4.8%, Flashlight - Angle Headed 2.4%, Funnel 2.4%, Maintenance III: "Home Guide to Tool Repairs" 2.4%, Marker - Black 2.4%, Measuring Tape 2.4%, Pliers 2.4%

### Bag_SurvivorBag — Military Backpack
- bag · capacity 28 · weight reduction 85% · weight 2.0 · equips back · run speed ×0.94 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Candle 1.4%, Gas Mask Filter 1.4%, Paperback 1.4%, Beef Jerky 0.9%, Canned Beans 0.9%, Canned Carrots 0.9%, Canned Chili 0.9%, Canned Corn 0.9%, Canned Corned Beef 0.9%, Canned Dog Food 0.9%, Canned Fruit Cocktail 0.9%, Canned Mushroom Soup 0.9%

### Bag_SWAT — SWAT Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 1 room def: swatlocker (0)
- Contents (rolls 4): Holster 13.2%, JS-2000 Shotgun 13.2%, M9 Pistol 13.2%, Nightstick 13.2%, Patrol Revolver 13.2%, Bulletproof Vest - Police 6.6%, Holster - Ankle 6.6%, Holster - Shoulder 6.6%, Shells Bandolier 6.6%, JS-14 Rifle 2.6%, JS-3T Tactical Shotgun 2.6%, Magnum 2.6%

### Bag_TarpFramepack_Large — Simple Framepack
- bag · capacity 20 · weight reduction 70% · weight 2.0 · equips back · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_TarpFramepack_Small — Small Simple Framepack
- bag · capacity 15 · weight reduction 65% · weight 1.5 · equips back · run speed ×0.98 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_TarpSack — Tarp Sack
- bag · capacity 20 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_TarpSlingBag — Tarp Sling Bag
- bag · capacity 12 · weight reduction 55% · weight 0.8 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### Bag_TennisBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Tennis Ball 38.6%, Tennis Racket 30.5%, Shorts - Sport 10.2%, Sneakers 10.2%, Long Shorts - Sport 4.1%, T-shirt - Sports 4.1%, Sport Pants 2.0%, Gum 0.2%, Water Bottle 0.2%

### Bag_ToolBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 2 room defs: drugshack (7), druglab (5)
- Contents (rolls 3): Box of Nails 4.1%, Funnel 4.1%, Gardening Trowel 4.1%, Leaf Rake 4.1%, Measuring Tape 4.1%, Rake 4.1%, Ratchet Wrench 4.1%, Rope 4.1%, Rubber Hose 4.1%, Screwdriver 4.1%, Twine 4.1%, Wood Saw 4.1%

### Bag_TrashBag — Garbage Bag
- bag · capacity 20 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 12 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), derelict (1,038), storageunit (695); +6 more
- Contents (rolls 2; junk ClutterTables.BinJunk): Dryer Lint 1.3%, Paperwork 1.3%, T-shirt 0.4%, Pants 0.4%, Game Pieces 0.4%, Bra - Strapless 0.3%, Underpants 0.3%, Bra 0.3%, Visor 0.3%, Wood Splinters 0.3%, Apron 0.3%, Bandana 0.3%

### Bag_TreasureBag — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 4): Diamond 7.7%, Emerald 7.7%, Gold Ring 7.7%, Ruby 7.7%, Sapphire 7.7%, Trophy 7.7%, Goblet 3.1%, Gold Bangle 3.1%, Gold Chain Bracelet 3.1%, Gold Earrings - Large Looped 3.1%, Gold Earrings - Medium Looped 3.1%, Gold Earrings - Small Looped 3.1%

### Bag_TrumpetCase — Trumpet Case
- bag · capacity 1 · weight reduction 50% · weight 1.0 · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 14 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295), storageunit (695); +8 more
- Contents (rolls 1): Trumpet 100.0%

### Bag_ViolinCase — Violin Case
- bag · capacity 1 · weight reduction 50% · weight 1.0 · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 11 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50), musicstore (11); +5 more
- Contents (rolls 1): Violin 100.0%

### Bag_WeaponBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 7 room defs: policestorage (19), druglab (5), evidenceroom (2), policeswat (2), policearchive (1), policegunstorage (1); +1 more
- Contents (rolls 2): Box of .357 Magnum Rounds 3.0%, Box of .38 Special Rounds 3.0%, Box of .44 Magnum Rounds 3.0%, Box of .45 ACP Rounds 3.0%, Box of 12g Rounds 3.0%, Box of 5.56x45mm Rounds 3.0%, Box of 7.62x51mm Rounds 3.0%, Box of 9x19mm Rounds 3.0%, Butterfly Knife 3.0%, Hunting Knife 3.0%, JS-2000 Shotgun 3.0%, Sawed-off JS-2000 Shotgun 3.0%

### Bag_WorkerBag — Duffel Bag
- bag · capacity 18 · weight reduction 65% · weight 1.0 · equips back · run speed ×0.95 · CLONE-Z: Sack
- Spawns in 5 room defs: bathroom (15,472), laundry (1,193), breakroom (321), changeroom (229), factorystorage (11)
- Contents (rolls 1): Gum 10.5%, Pencil 10.5%, Lunchbox 8.5%, Lunchbag 8.4%, Pen - Black 8.4%, Pen - Blue 8.4%, Pen - Red 8.4%, Sports Bottle 8.4%, Matchbook 4.2%, CD 2.1%, Lighter - Disposable 2.1%, Milk Chocolate Bar 2.1%

### Briefcase — Briefcase
- bag · capacity 8 · weight reduction 50% · weight 1.5 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 37 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), closet (5,489), office (3,904), garagestorage (1,318); +31 more
- Contents (rolls 1): Paperwork 46.6%, Business Card 4.7%, Gum 4.7%, Magazine 4.7%, Paperclip 4.7%, Notepad 3.7%, Pager 3.7%, Paperback 3.7%, Pen - Black 3.7%, Book 1.9%, Clipboard 1.9%, Money 1.9%

### Briefcase_Money — Briefcase
- bag · capacity 8 · weight reduction 50% · weight 1.5 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 13 room defs: bedroom (11,303), policeoffice (43), policestorage (19), druglab (5), changeroomjockey (2), detectiveoffice (2); +7 more
- Contents (rolls 4): Money 85.2%, Stock Certificate 9.9%, Money Bundle 4.3%, Pouch 0.6%

### Cashbox — Cashbox
- bag · capacity 2 · weight reduction 50% · weight 0.5 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 13 room defs: bedroom (11,303), office (3,904), breakroom (321), cardealershipoffice (26), hotdogstand (6), cybercafe (1); +7 more
- Contents (rolls 4): Money 100.0%

### CigarBox — Cigar Box
- bag · capacity 2 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 9 room defs: bedroom (11,303), office (3,904), breakroom (321), cardealershipoffice (26), tobaccostore (2), ww_generalstore (2); +3 more
- Contents (rolls 4): Cigar 100.0%

### CigarBox_Gaming — Cigar Box
- bag · capacity 2 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 8 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695), attic (50); +2 more
- No distribution entry — spawns empty.

### CigarBox_Keepsakes — Cigar Box
- bag · capacity 2 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 10 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50); +4 more
- No distribution entry — spawns empty.

### CigarBox_Kids — Cigar Box
- bag · capacity 2 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 5 room defs: bedroom (11,303), kidsbedroom (1,295), elementaryclassroom (74), classroom (72), daycare (11)
- No distribution entry — spawns empty.

### CookieJar — Cookie Jar
- bag · capacity 2 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 4 room defs: livingroom (10,624), kitchen (7,401), ww_generalstore (2), SurvivorCacheBigBuilding (0)
- Contents (rolls 4): Chocolate Chip Cookie 40.0%, Oatmeal Cookie 20.0%, Chocolate Cookie 10.0%, Raspberry Shortbread Cookie 10.0%, Shortbread Cookie 10.0%, Sugar Cookie 10.0%

### CookieJar_Bear — Cookie Jar - Teddy Bear
- bag · capacity 2 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 5 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), kidsbedroom (1,295), SurvivorCacheBigBuilding (0)
- Contents (rolls 4): Chocolate Chip Cookie 40.0%, Oatmeal Cookie 20.0%, Chocolate Cookie 10.0%, Raspberry Shortbread Cookie 10.0%, Shortbread Cookie 10.0%, Sugar Cookie 10.0%

### Cooler — Cooler
- bag · capacity 12 · weight reduction 50% · weight 1.5 · run speed ×0.9 · CLONE-Z: Sack
- Spawns in 24 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695); +18 more
- Contents (rolls 1): empty table

### Cooler_Beer — Cooler
- bag · capacity 12 · weight reduction 50% · weight 1.5 · run speed ×0.9 · CLONE-Z: Sack
- Spawns in 2 room defs: livingroom (10,624), `all`
- Contents (rolls 1): Beer Bottle 36.7%, Beer Can 36.7%, Pack of Beer Bottles 10.0%, Pack of Beer Cans 10.0%, Cold Pack 6.7%

### Cooler_Meat — Cooler
- bag · capacity 12 · weight reduction 50% · weight 1.5 · run speed ×0.9 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Beef Patty 38.5%, Pack of Hotdogs 19.2%, Cold Pack 11.5%, Chicken Leg 7.7%, Pork Chop 7.7%, Sausage 7.7%, Steak 7.7%

### Cooler_Seafood — Cooler
- bag · capacity 12 · weight reduction 50% · weight 1.5 · run speed ×0.9 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Cold Pack 59.5%, Mussels 6.8%, Oysters 6.8%, Shrimp 6.8%, Crayfish 3.4%, Salmon 3.4%, Black Crappie 0.9%, Blue Catfish 0.9%, Bluegill 0.9%, Channel Catfish 0.9%, Flathead Catfish 0.9%, Freshwater Drum 0.9%

### Cooler_Soda — Cooler
- bag · capacity 12 · weight reduction 50% · weight 1.5 · run speed ×0.9 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Cola 28.9%, Diet Cola 28.9%, Ginger Ale 28.9%, Orange Soda 8.3%, Cold Pack 4.1%, Bottle 0.8%

### DiceBag — Pouch
- bag · capacity 1 · weight reduction 30% · weight 0.1 · CLONE-Z: Sack
- Spawns in 32 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), kidsbedroom (1,295); +26 more
- Contents (rolls 4): Dice - 10-sided 13.6%, Dice - 12-sided 13.6%, Dice - 20-sided 13.6%, Dice - 4-sided 13.6%, Dice - 6-sided 13.6%, Dice - 8-sided 13.6%, Dice - Percentile 13.6%, Dice 4.9%

### EmptySandbag — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 16 room defs: barn (170), farmstorage (134), restaurantkitchen (82), toolstore (52), cafeteriakitchen (47), restaurantkitchen_fancy (16); +10 more
- Refilled by the `bags` table of: CrateCoke
- No distribution entry — spawns empty.

### FirstAidKit — First Aid Kit
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 27 room defs: bathroom (15,472), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), breakroom (321); +21 more
- Contents (rolls 1): Alcohol Wipes 19.5%, Bandage - Adhesive 19.5%, Suture Needle 19.5%, Bandage 7.3%, Surgical Gloves 7.3%, Cotton Balls 4.9%, Forceps 4.9%, Adhesive Tape 2.4%, Cold Pack 2.4%, Medical Shears 2.4%, Painkillers 2.4%, Safety Scissors 2.4%

### FirstAidKit_Camping — First Aid Kit - Camping
- bag · capacity 2 · weight reduction 50% · weight 0.3 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 15 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229), attic (50); +9 more
- Contents (rolls 1): Alcohol Wipes 18.0%, Bandage - Adhesive 18.0%, Tweezers 11.3%, Bandage 6.8%, Painkillers 6.8%, Cotton Balls 4.5%, Adhesive Tape 2.3%, Antibiotics 2.3%, Beef Jerky 2.3%, Caffeine Pills 2.3%, Cold Pack 2.3%, Granola Bar 2.3%

### FirstAidKit_Camping_New — First Aid Kit - Camping
- bag · capacity 2 · weight reduction 50% · weight 0.3 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 8 room defs: hunting (26), fishingstorage (12), pawnshop (9), armysurplus (8), campingstorage (8), camping (6); +2 more
- Contents (rolls 3): Alcohol Wipes 24.2%, Bandage - Adhesive 24.2%, Bandage 9.1%, Painkillers 9.1%, Tweezers 6.1%, Water Purification Tablets 6.1%, Adhesive Tape 3.0%, Antibiotics 3.0%, Caffeine Pills 3.0%, Cold Pack 3.0%, Cotton Balls 3.0%, Box of Adhesive Bandages 2.4%

### FirstAidKit_Military — First Aid Kit - Military
- bag · capacity 2 · weight reduction 50% · weight 0.3 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 7 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50), armystorage (20); +1 more
- Contents (rolls 3): Alcohol Wipes 19.3%, Bandage 16.9%, Painkillers 16.9%, Surgical Gloves 7.2%, Suture Needle 7.2%, Cotton Balls 4.8%, Forceps 4.8%, Tweezers 4.8%, Adhesive Tape 2.4%, Antibiotics 2.4%, Medical Shears 2.4%, Safety Scissors 2.4%

### FirstAidKit_New — First Aid Kit
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 9 room defs: gasstore (103), gas2go (18), fossoil (13), carsupply (9), carsupplysport (1), SafehouseLoot (0); +3 more
- Contents (rolls 3): Alcohol Wipes 17.4%, Bandage 17.4%, Bandage - Adhesive 17.4%, Suture Needle 17.4%, Forceps 4.3%, Surgical Gloves 4.3%, Adhesive Tape 2.2%, Bottle of Disinfectant 2.2%, Cold Pack 2.2%, Cotton Balls 2.2%, First Aid I: "A Scouts Injury Guide" 2.2%, Medical Shears 2.2%

### FirstAidKit_NewPro — First Aid Kit
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 11 room defs: bathroom (15,472), garagestorage (1,318), breakroom (321), changeroom (229), security (122), firestorage (13); +5 more
- Contents (rolls 3): Alcohol Wipes 13.5%, Bandage 13.5%, Bandage - Adhesive 13.5%, Painkillers 11.8%, Suture Needle 11.8%, Surgical Gloves 5.1%, Adhesive Tape 3.4%, Cotton Balls 3.4%, Forceps 3.4%, Medical Shears 3.4%, Safety Scissors 3.4%, Tweezers 3.4%

### FirstAidKit_Pro — ?
- bag · capacity ? · **no item script in B42 data** (distribution entry only) · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Alcohol Wipes 17.6%, Bandage 17.6%, Bandage - Adhesive 17.6%, Suture Needle 17.6%, Forceps 4.4%, Surgical Gloves 4.4%, Adhesive Tape 2.2%, Bottle of Disinfectant 2.2%, Cold Pack 2.2%, Cotton Balls 2.2%, First Aid I: "A Scouts Injury Guide" 2.2%, Medical Shears 2.2%

### Flightcase — Flight Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 15 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295), storageunit (695); +9 more
- Contents (rolls 100): Acoustic Guitar 30.7%, Electric Bass 30.7%, Electric Guitar 30.7%, Guitar Pick 7.5%, Tuning Fork 0.4%

### Garbagebag — Garbage Bag
- bag · capacity 20 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 18 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), derelict (1,038); +12 more
- Contents (rolls 1): empty table

### GemBag — Pouch
- bag · capacity 1 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 12 room defs: bedroom (11,303), departmentstore (25), policestorage (19), jewelrystore (12), jewelrystorage (8), druglab (5); +6 more
- Contents (rolls 4): Diamond 23.7%, Emerald 23.7%, Ruby 23.7%, Sapphire 23.7%, Loupe 5.4%

### GroceryBag1 — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 2 room defs: grocery (77), grocerystorage (56)
- Contents (rolls 1): Juice Box 3.9%, Pickle 3.4%, Processed Cheese 3.4%, Cola 2.2%, Diet Cola 2.2%, Ginger Ale 2.2%, Milk Carton 2.2%, Yeast 2.2%, Apple 1.1%, Banana 1.1%, Broccoli 1.1%, Butter 1.1%

### GroceryBag2 — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 2 room defs: grocery (77), grocerystorage (56)
- Contents (rolls 1): Pickle 3.4%, Apple 2.3%, Banana 2.3%, Black Beans (Dried) 2.3%, Broccoli 2.3%, Cabbage 2.3%, Canned Carrots 2.3%, Canned Corn 2.3%, Canned Fruit Cocktail 2.3%, Canned Peaches 2.3%, Canned Peas 2.3%, Canned Pineapple 2.3%

### GroceryBag3 — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 2 room defs: grocery (77), grocerystorage (56)
- Contents (rolls 1): Granola Bar 5.1%, Gum 5.1%, Milk Chocolate Bar 5.1%, Licorice 5.1%, Candy Cane 2.6%, Candy Package 2.6%, Canned Beans 2.6%, Canned Chili 2.6%, Canned Fruit Cocktail 2.6%, Canned Mushroom Soup 2.6%, Canned Spaghetti Bolognese 2.6%, Canned Vegetable Soup 2.6%

### GroceryBag4 — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 2 room defs: grocery (77), grocerystorage (56)
- Contents (rolls 1): Flour 21.4%, Chocolate Chips 10.7%, Cocoa Powder 10.7%, White Sugar 10.7%, Aluminum Foil 8.9%, Baking Soda 8.9%, Brown Sugar 3.6%, Butter 3.6%, Can of Oats 3.6%, Lard 3.6%, Margarine 3.6%, Olive Oil 3.6%

### GroceryBag5 — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 2 room defs: grocery (77), grocerystorage (56)
- Contents (rolls 1): Ground Beef 23.2%, Steak 10.8%, Chips - Barbecue 4.6%, Chips - Plain 4.6%, Chips - Salt & Vinegar 4.6%, Chips - Sour Cream & Onion 4.6%, Cola 4.6%, Diet Cola 4.6%, Ginger Ale 4.6%, Orange Soda 4.6%, Processed Cheese 4.6%, Tortilla Chips 4.6%

### GroceryBagGourmet — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Hot Sauce 5.5%, Jar of Bell Peppers 5.5%, Jar of Broccoli 5.5%, Jar of Cabbage 5.5%, Jar of Carrots 5.5%, Jar of Eggplants 5.5%, Jar of Leeks 5.5%, Jar of Potatoes 5.5%, Jar of Radishes 5.5%, Jar of Tomatoes 5.5%, Balsamic Vinegar 4.4%, Capers 4.4%

### Guitarcase — Guitar Case
- bag · capacity 5 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 16 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), kidsbedroom (1,295), storageunit (695); +10 more
- Contents (rolls 100): Acoustic Guitar 30.7%, Electric Bass 30.7%, Electric Guitar 30.7%, Guitar Pick 7.5%, Tuning Fork 0.4%

### HalloweenCandyBucket — Halloween Candy Bucket
- bag · capacity 2 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 4 room defs: bedroom (11,303), livingroom (10,624), kidsbedroom (1,295), daycare (11)
- Contents (rolls 2): Candy Corn 6.9%, Halloween Candy 6.9%, Candied Apple 5.5%, Granola Bar 5.5%, Licorice 4.1%, Butterchunkers Bar 2.8%, Caramel Candies 2.8%, Chocolate Candy 2.8%, Crackle Bar 2.8%, Deux Bar 2.8%, Galactic Dairy Bar 2.8%, Gum 2.8%

### Handbag — Handbag
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.99 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 40 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), office (3,904), garagestorage (1,318), laundry (1,193); +34 more
- Contents (rolls 2): Money 26.5%, Credit Card 3.3%, Gum 3.3%, Key Ring 3.3%, Lighter - Disposable 3.3%, Matchbook 3.3%, Note 3.3%, Notepad 3.3%, Pen - Black 3.3%, Pen - Blue 3.3%, Receipt 3.3%, Tissue 3.3%

### Hatbox — Hatbox
- bag · capacity 1 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 3 room defs: bedroom (11,303), ww_generalstore (2), `all`
- No distribution entry — spawns empty.

### HollowBook — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 14 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695); +8 more
- Contents (rolls 1): Money 52.1%, Credit Card 14.6%, Photograph 14.6%, Bottle of Whiskey 2.1%, Butterfly Knife 2.1%, Caffeine Pills 2.1%, Locket 2.1%, Rolling Papers 2.1%, SN38 Revolver 2.1%, Seed Packet - Hemp 2.1%, Smoking Pipe 2.1%, Switchblade 2.1%

### HollowBook_Handgun — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 4 room defs: bedroom (11,303), drugshack (7), druglab (5), `all`
- Contents (rolls 10): SN38 Revolver 45.5%, M9 Pistol 18.2%, Magnum 18.2%, Patrol Revolver 18.2%, Military Medal 0.00%

### HollowBook_Kids — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 3 room defs: bedroom (11,303), kidsbedroom (1,295), daycare (11)
- Contents (rolls 1): Money 38.4%, Comic Book 10.8%, Doodle 10.8%, Photograph 10.8%, Handheld Game Console 3.1%, Magnifying Glass 3.1%, Playing Cards 3.1%, Chess Pieces 3.1%, Game Pieces 3.1%, Doll 1.5%, Firecrackers 1.5%, Friendship Bracelet 1.5%

### HollowBook_Prison — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 1 room def: prisoncells (308)
- Contents (rolls 1): Money 33.3%, Caffeine Pills 9.3%, Painkillers 9.3%, File 6.7%, Notepad 6.7%, Pager 6.7%, Shiv 6.7%, Cigarette Pack 2.7%, Flask 2.7%, Glass Shiv 2.7%, Lighter - Disposable 2.7%, Butterfly Knife 1.3%

### HollowBook_Valuables — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 7 room defs: bedroom (11,303), livingroom (10,624), druglab (5), SafehouseLoot (0), SafehouseLoot_Late (0), SafehouseLoot_Mid (0); +1 more
- Contents (rolls 1): Money 69.3%, Credit Card 21.9%, Stock Certificate 3.5%, Caffeine Pills 1.2%, Flask 1.2%, Poker Chips 1.2%, SN38 Revolver 1.2%, Diamond 0.1%, Emerald 0.1%, ID Card - Blank 0.1%, Pouch 0.1%, Ruby 0.1%

### HollowBook_Whiskey — Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Spawns in 6 room defs: bedroom (11,303), office (3,904), recreation (32), drugshack (7), druglab (5), `all`
- Contents (rolls 1): Bottle of Whiskey 100.0%

### HollowFancyBook — Leatherbound Book
- bag · capacity 2 · weight 1.0 · MaxItemSize `1.6` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### HolsterShoulder — Holster - Shoulder
- bag · capacity 1 · weight reduction 85% · weight 0.4 · equips shoulderholster · run speed ×0.99 · AcceptItemFunction `AcceptItemFunction.HolsterShoulder` · CLONE-Z: Sack
- Spawns in 24 room defs: bedroom (11,303), office (3,904), garagestorage (1,318), security (122), policeoffice (43), gunstore (21); +18 more
- No distribution entry — spawns empty.

### Humidor — Humidor
- bag · capacity 2 · weight reduction 50% · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: bedroom (11,303)
- Contents (rolls 4): Cigar 100.0%

### JewelleryBox — Jewelry Box
- bag · capacity 2 · weight reduction 50% · weight 1.0 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 5 room defs: bedroom (11,303), stripclubvip (12), stripclub (6), backstage (4), `all`
- Contents (rolls 2): Choker 6.7%, Necklace - Yin and Yang Symbol 5.3%, Silver Bangle 5.3%, Silver Belly Button Dangle 5.3%, Silver Belly Button Ring 5.3%, Silver Belly Button Stud 5.3%, Silver Chain Bracelet 5.3%, Silver Nose Ring 5.3%, Silver Nose Stud 5.3%, Choker with Amber Stone 2.7%, Digital Watch - Metallic Dress Style 2.7%, Necklace - Crucifix 2.7%

### JewelleryBox_Fancy — Jewelry Box
- bag · capacity 2 · weight reduction 50% · weight 1.0 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 3 room defs: bedroom (11,303), ww_generalstore (2), `all`
- Contents (rolls 2): Dangly Earrings - Pearl 4.2%, Earrings - Pearl 4.2%, Old Photograph 4.2%, Pearl Necklace 4.2%, Gold Earrings - Large Looped 3.3%, Gold Earrings - Medium Looped 3.3%, Gold Earrings - Small Looped 3.3%, Gold Earrings - Small Looped Top 3.3%, Gold Long Necklace 3.3%, Gold Necklace 3.3%, Pocketwatch 3.3%, Silver Bangle 3.3%

### KeyRing — Key Ring
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 124 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), kitchen (7,401), office (3,904), garagestorage (1,318); +118 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Bass — Key Ring - Bass
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 30 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +24 more
- Contents (rolls 2): Key 80.7%, Vehicle Key 16.1%, Key Ring Bottle Opener 2.3%, Compass 0.2%, Handiknife 0.2%, P38 Can Opener 0.2%, Whistle 0.2%

### KeyRing_BlueFox — Key Ring - Blue Fox
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Bug — Key Ring - Bug
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_CarDealer — Key Ring
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 1 room def: cardealershipoffice (26)
- Contents (rolls 2): Vehicle Key 100.0%

### KeyRing_Clover — Key Ring - Four-Leaf Clover
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 13 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695); +7 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_EagleFlag — Key Ring - American Eagle
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 26 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +20 more
- Contents (rolls 2): Key 80.7%, Vehicle Key 16.1%, Key Ring Bottle Opener 2.3%, Compass 0.2%, Handiknife 0.2%, P38 Can Opener 0.2%, Whistle 0.2%

### KeyRing_EightBall — Key Ring - Eight Ball
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 21 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +15 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Forged — Key Ring - Forged
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Forged_Gold — Key Ring - Gold
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 4 room defs: giftstore (28), toystore (15), giftstorage (7), toystorestorage (1)
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Forged_Silver — Key Ring - Silver
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 4 room defs: giftstore (28), toystore (15), giftstorage (7), toystorestorage (1)
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Hotdog — Key Ring - Hotdog
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Kitty — Key Ring - Kitty
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 21 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +15 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Large — Key Ring - Large
- bag · capacity 2 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Nolans — Key Ring - Nolan's Used Cars
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 2 room defs: nolansoffice (2), `all`
- Contents (rolls 2): Vehicle Key 100.0%

### KeyRing_Panther — Key Ring - Panther
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_PineTree — Key Ring - Pine Tree
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 30 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +24 more
- Contents (rolls 2): Key 80.7%, Vehicle Key 16.1%, Key Ring Bottle Opener 2.3%, Compass 0.2%, Handiknife 0.2%, P38 Can Opener 0.2%, Whistle 0.2%

### KeyRing_PrayingHands — Key Ring - Praying Hands
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 25 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +19 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_RabbitFoot — Key Ring - Rabbit Foot
- bag · capacity 1 · weight reduction 85% · weight 0.1 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 13 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695); +7 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Racing12 — Key Ring - 12
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### KeyRing_Racing34 — Key Ring - 34
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### KeyRing_Racing58 — Key Ring - 58
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### KeyRing_RainbowStar — Key Ring - Rainbow Star
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_RubberDuck — Key Ring - Rubber Duck
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 20 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +14 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_SecurityPass — Security Pass Key Ring
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 12 room defs: office (3,904), security (122), toolstore (52), policeoffice (43), policelocker (9), prisonstorage (3); +6 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Sexy — Key Ring - Sexy
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 16 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), office (3,904), garagestorage (1,318); +10 more
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_Spiffos — Key Ring - Spiffos
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 2 room defs: office (3,904), spiffoskitchen (28)
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_StinkyFace — Key Ring - Stinky Face
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 3 room defs: livingroom (10,624), kitchen (7,401), SurvivorCacheBigBuilding (0)
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### KeyRing_WestMaple — Key Ring - West Maple Country Club
- bag · capacity 1 · weight reduction 85% · weight 0.05 · AcceptItemFunction `AcceptItemFunction.KeyRing` · CLONE-Z: Sack
- Spawns in 3 room defs: bedroom (11,303), livingroom (10,624), `all`
- Contents (rolls 2): Key 81.4%, Vehicle Key 16.3%, Key Ring Bottle Opener 2.3%

### Lunchbag — Lunchbag
- bag · capacity 2 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 43 room defs: bathroom (15,472), bedroom (11,303), office (3,904), garagestorage (1,318), janitor (688), breakroom (321); +37 more
- Contents (rolls 2): Apple 6.7%, Banana 6.7%, Fruit Muffin 6.7%, Granola Bar 6.7%, Muffin 6.7%, Orange 6.7%, Yogurt 6.7%, Beef Jerky 3.3%, Burrito 3.3%, Chocolate Bread 3.3%, Chocolate Chip Cookie 3.3%, Chocolate Cookie 3.3%

### Lunchbox — Lunchbox
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 43 room defs: bathroom (15,472), bedroom (11,303), office (3,904), garagestorage (1,318), janitor (688), breakroom (321); +37 more
- Contents (rolls 2): Apple 6.7%, Banana 6.7%, Fruit Muffin 6.7%, Granola Bar 6.7%, Muffin 6.7%, Orange 6.7%, Yogurt 6.7%, Beef Jerky 3.3%, Burrito 3.3%, Chocolate Bread 3.3%, Chocolate Chip Cookie 3.3%, Chocolate Cookie 3.3%

### Lunchbox2 — Lunchbox
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 43 room defs: bathroom (15,472), bedroom (11,303), office (3,904), garagestorage (1,318), janitor (688), breakroom (321); +37 more
- Contents (rolls 2): Apple 6.7%, Banana 6.7%, Fruit Muffin 6.7%, Granola Bar 6.7%, Muffin 6.7%, Orange 6.7%, Yogurt 6.7%, Beef Jerky 3.3%, Burrito 3.3%, Chocolate Bread 3.3%, Chocolate Chip Cookie 3.3%, Chocolate Cookie 3.3%

### MakeupCase_Professional — Makeup Case
- bag · capacity 4 · weight reduction 50% · weight 1.0 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 4 room defs: bedroom (11,303), jackiejayeoffice (1), judgematthassset (1), `all`
- Contents (rolls 4): Eye Makeup 15.3%, Foundation Makeup 15.3%, Hair Spray 15.3%, Bottle of Hair Dye 7.4%, Comb 4.4%, Hair Gel 4.4%, Lipstick 4.4%, Perfume 4.4%, Scissors 4.4%, Tissue 4.4%, Tweezers 4.4%, Box of Tissues 2.2%

### PaperBag — Paper Bag
- bag · capacity 2 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 36 room defs: livingroom (10,624), kitchen (7,401), gasstore (103), grocery (77), electronicstore (45), liquorstore (39); +30 more
- Contents (rolls 1): empty table

### Paperbag_Jays — Paper Bag
- bag · capacity 2 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 10 room defs: livingroom (10,624), breakroom (321), hospitalroom (64), jayschicken_dining (12), daycare (11), jayschicken_kitchen (11); +4 more
- Contents (rolls 1): Fried Chicken 24.3%, Paper Napkins 20.3%, Straw 20.3%, Cornbread 10.8%, Fries 10.8%, Plastic Fork 6.8%, Plastic Knife 6.8%

### Paperbag_Spiffos — Paper Bag
- bag · capacity 2 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 11 room defs: livingroom (10,624), breakroom (321), hospitalroom (64), spiffoskitchen (28), spiffo_dining (21), daycare (11); +5 more
- Contents (rolls 1): Burger 28.1%, Paper Napkins 23.4%, Straw 23.4%, Fried Onion Rings 12.5%, Fries 12.5%

### Parcel_ExtraLarge — Parcel - Extra Large
- bag · capacity 20 · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: post (32), poststorage (9)
- Contents (rolls 100): Barbecue 24.4%, Desktop Computer 24.4%, Fitness Contraption 24.4%, Satellite Dish 24.4%, Bulky Protective Container 1.2%, Premium Technologies Ham Radio 1.2%

### Parcel_ExtraSmall — Parcel - Extra Small
- bag · capacity 1 · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: post (32), poststorage (9), `all`
- Contents (rolls 100): Long Socks 8.6%, Socks 8.6%, Bobber 2.9%, Book 2.9%, CD 2.9%, Cheese 2.9%, Cigar 2.9%, Cologne 2.9%, Doll 2.9%, Handheld Game Console 2.9%, Handiknife 2.9%, Harmonica 2.9%

### Parcel_Large — Parcel - Large
- bag · capacity 10 · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: post (32), poststorage (9)
- Contents (rolls 100): Microwave 50.0%, Mini Fridge 25.0%, Premium Technologies Television 25.0%

### Parcel_Medium — Parcel - Medium
- bag · capacity 5 · weight 1.0 · CLONE-Z: Sack
- Spawns in 3 room defs: post (32), poststorage (9), `all`
- Contents (rolls 100): Boxed Red Wine 20.0%, Coffee X-Press 20.0%, Red Wine 20.0%, Toaster 20.0%, White Wine 20.0%

### Parcel_Small — Parcel - Small
- bag · capacity 2 · weight 1.0 · CLONE-Z: Sack
- Spawns in 2 room defs: post (32), poststorage (9)
- Contents (rolls 100): Bath Towel 9.0%, Bottle of Whiskey 9.0%, CD Player 9.0%, Camera 9.0%, High-end Camera 9.0%, Kettle 9.0%, Premium Technologies Radio 9.0%, Sweater 9.0%, Sweater - Round Neck 9.0%, Sweater - Turtleneck 9.0%, Sweater - V-neck 9.0%, Copper Kettle 0.9%

### PencilCase — Pencil Case
- bag · capacity 1 · weight reduction 50% · weight 0.3 · CLONE-Z: Sack
- Spawns in 8 room defs: bedroom (11,303), kidsbedroom (1,295), elementaryclassroom (74), classroom (72), secondaryclassroom (67), secondaryhall (22); +2 more
- Contents (rolls 2): Pencil 26.0%, Crayons 13.0%, Eraser 10.4%, Rubber Band 10.4%, Pen - Black 5.2%, Pen - Blue 5.2%, Pen - Green 5.2%, Pen - Red 5.2%, Safety Scissors 5.2%, Geometry Compass 2.6%, Marker - Black 2.6%, Multi-Color Pen 2.6%

### PencilCase_Gaming — Pencil Case
- bag · capacity 1 · weight reduction 50% · weight 0.3 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### PhotoAlbum — Photo Album
- bag · capacity 5 · weight 0.5 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 9 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), attic (50), joanstudio (1); +3 more
- Contents (rolls 4): Photograph 76.3%, Doodle 7.6%, Handwritten Letter 7.6%, Postcard 7.6%, Old Photograph 0.8%

### PhotoAlbum_Old — Photo Album
- bag · capacity 5 · weight 0.5 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 5 room defs: livingroom (10,624), joanstudio (1), SafehouseLoot (0), SafehouseLoot_Late (0), SafehouseLoot_Mid (0)
- Contents (rolls 4): Old Photograph 66.7%, Handwritten Letter 13.3%, Postcard 13.3%, Locket 6.7%

### PistolCase1 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 3 room defs: garagestorage (1,318), gunstore (21), pawnshop (9)
- Contents (rolls 1): Box of 9x19mm Rounds 45.8%, M9 Magazine 28.0%, M9 Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### PistolCase2 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .45 ACP Rounds 45.8%, M1911 Auto Magazine 28.0%, M1911 Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### PistolCase3 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 1 room def: garagestorage (1,318)
- Contents (rolls 1): Box of .44 Magnum Rounds 45.8%, B-F Magazine 28.0%, B-F Pistol 25.4%, Gun Light 0.5%, Holster - Shoulder 0.3%

### Plasticbag — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 33 room defs: derelict (1,038), gasstore (103), grocery (77), electronicstore (45), pharmacy (31), hunting (26); +27 more
- Contents (rolls 1): empty table

### Plasticbag_Bags — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 10 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), derelict (1,038), storageunit (695); +4 more
- Contents (rolls 10): Plastic Bag 100.0%

### Plasticbag_Clothing — Plastic Bag
- bag · capacity 8 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Long Socks 10.1%, Socks 10.1%, T-shirt 3.8%, Bra 3.5%, Underpants 3.5%, Boxers 3.1%, Bra - Strapless 3.0%, Briefs 2.5%, Pants 2.5%, Belt 2.0%, Hoodie 2.0%, Socks - Heavy 2.0%

### Present_ExtraLarge — Gift - Extra Large
- bag · capacity 20 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 100): Barbecue 24.4%, Desktop Computer 24.4%, Fitness Contraption 24.4%, Satellite Dish 24.4%, Bulky Protective Container 1.2%, Premium Technologies Ham Radio 1.2%

### Present_ExtraSmall — Gift - Extra Small
- bag · capacity 1 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 100): Long Socks 8.6%, Socks 8.6%, Bobber 2.9%, Book 2.9%, CD 2.9%, Cheese 2.9%, Cigar 2.9%, Cologne 2.9%, Doll 2.9%, Handheld Game Console 2.9%, Handiknife 2.9%, Harmonica 2.9%

### Present_Large — Gift - Large
- bag · capacity 10 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 100): Microwave 50.0%, Mini Fridge 25.0%, Premium Technologies Television 25.0%

### Present_Medium — Gift - Medium
- bag · capacity 5 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 100): Boxed Red Wine 20.0%, Coffee X-Press 20.0%, Red Wine 20.0%, Toaster 20.0%, White Wine 20.0%

### Present_Small — Gift - Small
- bag · capacity 2 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 100): Bath Towel 9.0%, Bottle of Whiskey 9.0%, CD Player 9.0%, Camera 9.0%, High-end Camera 9.0%, Kettle 9.0%, Premium Technologies Radio 9.0%, Sweater 9.0%, Sweater - Round Neck 9.0%, Sweater - Turtleneck 9.0%, Sweater - V-neck 9.0%, Copper Kettle 0.9%

### ProduceBox_ExtraLarge — Produce Box - Extra Large
- bag · capacity 20 · weight 1.0 · CLONE-Z: Sack
- Not spawned by any room table.
- No distribution entry — spawns empty.

### ProduceBox_ExtraSmall — Produce Box - Extra Small
- bag · capacity 1 · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: producestorage (42)
- Refilled by the `bags` table of: ProduceStorageCherry, ProduceStorageStrawberries
- No distribution entry — spawns empty.

### ProduceBox_Large — Produce Box - Large
- bag · capacity 10 · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: producestorage (42)
- Refilled by the `bags` table of: ProduceStorageCabbages, ProduceStorageKale, ProduceStorageLettuce
- No distribution entry — spawns empty.

### ProduceBox_Medium — Produce Box - Medium
- bag · capacity 5 · weight 1.0 · CLONE-Z: Sack
- Spawns in 11 room defs: restaurantkitchen (82), producestorage (42), spiffoskitchen (28), restaurantkitchen_fancy (16), dinerkitchen (15), jayschicken_kitchen (11); +5 more
- Refilled by the `bags` table of: FryFactoryPotatoes, ProduceStorageBellPeppers, ProduceStorageBroccoli, ProduceStorageCarrots, ProduceStorageCauliflower, ProduceStorageCorn, ProduceStorageEggplant, ProduceStorageGreenpeas, ProduceStorageLeeks, ProduceStorageOnions, ProduceStoragePotatoes, ProduceStorageRadishes, ProduceStorageSweetPotatoes, ProduceStorageTomatoes
- No distribution entry — spawns empty.

### ProduceBox_Small — Produce Box - Small
- bag · capacity 2 · weight 1.0 · CLONE-Z: Sack
- Spawns in 1 room def: producestorage (42)
- Refilled by the `bags` table of: ProduceStorageApples, ProduceStorageGrapes, ProduceStoragePeaches, ProduceStoragePears
- No distribution entry — spawns empty.

### Purse — Purse
- bag · capacity 6 · weight reduction 55% · weight 1.0 · run speed ×0.99 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 40 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), office (3,904), garagestorage (1,318), laundry (1,193); +34 more
- Contents (rolls 2): Money 26.5%, Credit Card 3.3%, Gum 3.3%, Key Ring 3.3%, Lighter - Disposable 3.3%, Matchbook 3.3%, Note 3.3%, Notepad 3.3%, Pen - Black 3.3%, Pen - Blue 3.3%, Receipt 3.3%, Tissue 3.3%

### RevolverCase1 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: garagestorage (1,318), `all`
- Contents (rolls 1): Box of .38 Special Rounds 64.1%, SN38 Revolver 35.6%, Holster - Shoulder 0.4%

### RevolverCase2 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: garagestorage (1,318), `all`
- Contents (rolls 1): Box of .357 Magnum Rounds 64.1%, Patrol Revolver 35.6%, Holster - Shoulder 0.4%

### RevolverCase3 — Handgun Case
- bag · capacity 4 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 2 room defs: garagestorage (1,318), `all`
- Contents (rolls 1): Box of .44 Magnum Rounds 63.2%, Magnum 35.1%, x2 Scope 1.4%, Holster - Shoulder 0.4%

### RifleCase1 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 10 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +4 more
- Contents (rolls 1): Box of 5.56x45mm Rounds 61.9%, MSR700 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### RifleCase2 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 7.62x51mm Rounds 61.9%, MSR788 Rifle 34.4%, x2 Scope 1.4%, Bullets Bandolier 0.7%, Recoil Pad 0.7%, x4 Scope 0.7%, x8 Scope 0.3%

### RifleCase3 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 4 room defs: bedroom (11,303), garagestorage (1,318), SafehouseLoot_Late (0), `all`
- Contents (rolls 1): Box of 7.62x51mm Rounds 44.9%, M1A Magazine 27.4%, M1A Rifle 24.9%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### RifleCase4 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Box of 5.56x45mm Rounds 44.7%, M16 Magazine 27.3%, M16 Assault Rifle 24.8%, x2 Scope 1.0%, Bullets Bandolier 0.5%, Recoil Pad 0.5%, Red Dot Sight 0.5%, x4 Scope 0.5%, x8 Scope 0.2%

### SeedBag — Pouch
- bag · capacity 1 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 18 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695); +12 more
- Contents (rolls 4): Seed Packet - Basil 3.1%, Seed Packet - Broccoli 3.1%, Seed Packet - Cabbage 3.1%, Seed Packet - Carrot 3.1%, Seed Packet - Cauliflower 3.1%, Seed Packet - Chamomile 3.1%, Seed Packet - Chives 3.1%, Seed Packet - Cilantro 3.1%, Seed Packet - Cucumber 3.1%, Seed Packet - Garlic 3.1%, Seed Packet - Green Peas 3.1%, Seed Packet - Lemongrass 3.1%

### SeedBag_Farming — Pouch
- bag · capacity 1 · weight reduction 50% · weight 0.1 · CLONE-Z: Sack
- Spawns in 8 room defs: closet (5,489), garagestorage (1,318), storageunit (695), barn (170), farmstorage (134), shed (35); +2 more
- Contents (rolls 4): Seed Packet - Corn 11.8%, Seed Packet - Wheat 11.8%, Seed Packet - Barley 5.9%, Seed Packet - Flax 5.9%, Seed Packet - Rye 5.9%, Seed Packet - Sunflower 5.9%, Seed Packet - Hops 3.0%, Seed Packet - Onion 3.0%, Seed Packet - Potato 3.0%, Seed Packet - Pumpkin 3.0%, Seed Packet - Strawberry 3.0%, Seed Packet - Sugar Beet 3.0%

### SewingKit — Sewing Kit
- bag · capacity 2 · weight reduction 50% · weight 1.0 · MaxItemSize `0.3` · CLONE-Z: Sack
- Spawns in 18 room defs: bathroom (15,472), bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318); +12 more
- Contents (rolls 1): Needle 35.1%, Thread 28.6%, Button 18.2%, Thimble 9.1%, Buckle 3.9%, Measuring Tape 2.6%, Awl 1.3%, Scissors 1.3%

### Shoebox — Shoebox
- bag · capacity 1 · weight reduction 50% · weight 0.5 · CLONE-Z: Sack
- Spawns in 2 room defs: bedroom (11,303), `all`
- Contents (rolls 10): Shoes 50.0%, Fancy Shoes 16.7%, Strapped Shoes 16.7%, Sneakers 8.3%, Western Boots 6.7%, Western Boots - Embroidered 0.8%, Western Boots - Snakeskin 0.8%

### ShotgunCase1 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 12g Rounds 62.5%, JS-2000 Shotgun 34.7%, Choke Tube - Full 1.4%, Choke Tube - Improved 0.7%, Shells Bandolier 0.7%

### ShotgunCase2 — Gun Case
- bag · capacity 7 · weight reduction 50% · weight 1.0 · run speed ×0.97 · CLONE-Z: Sack
- Spawns in 11 room defs: bedroom (11,303), livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), changeroom (229); +5 more
- Contents (rolls 1): Box of 12g Rounds 63.8%, Double Barrel Shotgun 35.5%, Shells Bandolier 0.7%

### Suitcase — Suitcase
- bag · capacity 16 · weight reduction 50% · weight 3.0 · run speed ×0.98 · CLONE-Z: Sack
- Spawns in 17 room defs: bathroom (15,472), bedroom (11,303), closet (5,489), garagestorage (1,318), laundry (1,193), breakroom (321); +11 more
- Contents (rolls 1): Long Socks 10.1%, Socks 10.1%, T-shirt 3.8%, Bra 3.5%, Underpants 3.5%, Boxers 3.1%, Bra - Strapless 3.0%, Briefs 2.5%, Pants 2.5%, Belt 2.0%, Hoodie 2.0%, Socks - Heavy 2.0%

### Tacklebox — Tacklebox
- bag · capacity 6 · weight reduction 50% · weight 1.5 · run speed ×0.97 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 37 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), derelict (1,038); +31 more
- Contents (rolls 2): Fishing Hook 14.2%, Fishing Line 14.2%, Bobber 4.7%, Bottle Opener 4.7%, Fillet Knife 4.7%, Fishing I: "Dean's Good Fishing Guide" 4.7%, Flashlight - Angle Headed 4.7%, Insect Repellent 4.7%, Jig Lure 4.7%, Minnow Lure 4.7%, Pliers 4.7%, Pocket Knife 4.7%

### TakeoutBox_Chinese — Chinese Takeout Container
- bag · capacity 2 · weight reduction 50% · weight 0.3 · CLONE-Z: Sack
- Spawns in 9 room defs: livingroom (10,624), office (3,904), security (122), policeoffice (43), cardealershipoffice (26), warehouse (26); +3 more
- Contents (rolls 2): Fried Shrimp 16.7%, Meat Dumpling 16.7%, Meat Steam Bun 16.7%, Shrimp Dumpling 16.7%, Spring Roll 16.7%, Tofu (Fried) 16.7%

### TakeoutBox_Styrofoam — Takeout Container
- bag · capacity 2 · weight reduction 50% · weight 0.3 · CLONE-Z: Sack
- Spawns in 9 room defs: livingroom (10,624), office (3,904), security (122), policeoffice (43), cardealershipoffice (26), warehouse (26); +3 more
- Contents (rolls 10): Burger 27.2%, Fries 15.3%, Burrito 13.6%, Fried Chicken 13.6%, Hot Dog 13.6%, Pizza Slice 13.6%, Fried Onion Rings 3.1%

### Toolbox — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 15 room defs: bathroom (15,472), livingroom (10,624), closet (5,489), garagestorage (1,318), laundry (1,193), storageunit (695); +9 more
- Contents (rolls 1): Flashlight - Angle Headed 3.2%, Funnel 3.2%, Marker - Black 3.2%, Measuring Tape 3.2%, Nails 3.2%, Nuts and Bolts 3.2%, Pliers 3.2%, Rubber Hose 3.2%, Screwdriver 3.2%, Screws 3.2%, Twine 3.2%, Whetstone 3.2%

### Toolbox_Farming — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 8 room defs: closet (5,489), garagestorage (1,318), storageunit (695), barn (170), farmstorage (134), shed (35); +2 more
- Contents (rolls 3): Agriculture I: "Better Gardening" 3.3%, Fleshing Tool 3.3%, Gardening Trowel 3.3%, Hand Drill 3.3%, Hand Fork 3.3%, Kneepad 3.3%, Leather Gloves 3.3%, Measuring Tape 3.3%, Pliers 3.3%, Rubber Hose 3.3%, Scissors 3.3%, Summer Hat 3.3%

### Toolbox_Fishing — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 4 room defs: closet (5,489), garagestorage (1,318), storageunit (695), shed (35)
- Contents (rolls 2): Fishing Hook 14.2%, Fishing Line 14.2%, Bobber 4.7%, Bottle Opener 4.7%, Fillet Knife 4.7%, Fishing I: "Dean's Good Fishing Guide" 4.7%, Flashlight - Angle Headed 4.7%, Insect Repellent 4.7%, Jig Lure 4.7%, Minnow Lure 4.7%, Pliers 4.7%, Pocket Knife 4.7%

### Toolbox_Gardening — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 7 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695), attic (50); +1 more
- Contents (rolls 3): Agriculture I: "Better Gardening" 3.5%, Gardening Trowel 3.5%, Hand Drill 3.5%, Hand Fork 3.5%, Kneepad 3.5%, Leather Gloves 3.5%, Measuring Tape 3.5%, Pliers 3.5%, Rubber Hose 3.5%, Scissors 3.5%, Slug Repellent 3.5%, Summer Hat 3.5%

### Toolbox_Mechanic — Toolbox
- bag · capacity 8 · weight reduction 50% · weight 2.0 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 17 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), mechanic (604), farmstorage (134); +11 more
- Contents (rolls 3): Electrical Wire 5.3%, Funnel 5.3%, Lug Wrench 5.3%, Mechanics I: "Carzone's Repair Guide" 5.3%, Rag (Dirty) 5.3%, Ratchet Wrench 5.3%, Rubber Hose 5.3%, Screwdriver 5.3%, Tire Iron 5.3%, Wrench 5.3%, Penlight 3.9%, Calipers 2.6%

### Toolbox_Wooden — Wooden Toolbox
- bag · capacity 6 · weight reduction 50% · weight 1.0 · run speed ×0.97 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 1 room def: ww_generalstore (2)
- Contents (rolls 1): Flashlight - Angle Headed 3.2%, Funnel 3.2%, Marker - Black 3.2%, Measuring Tape 3.2%, Nails 3.2%, Nuts and Bolts 3.2%, Pliers 3.2%, Rubber Hose 3.2%, Screwdriver 3.2%, Screws 3.2%, Twine 3.2%, Whetstone 3.2%

### ToolRoll_Fabric — Tool Roll - Fabric
- bag · capacity 4 · weight reduction 50% · weight 0.5 · MaxItemSize `1.0` · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 3): Kitchen Knife 21.7%, Fillet Knife 14.5%, Paring Knife 14.5%, Sushi Knife 7.2%, Whetstone 7.2%, Basting Brush 5.8%, Carving Fork 5.8%, Grill Brush 5.8%, Kitchen Tongs 5.8%, Ladle 5.8%, Spatula 5.8%

### ToolRoll_Leather — Tool Roll - Leather
- bag · capacity 4 · weight reduction 50% · weight 0.5 · MaxItemSize `1.0` · CLONE-Z: Sack
- Spawns in 18 room defs: livingroom (10,624), closet (5,489), garagestorage (1,318), storageunit (695), mechanic (604), farmstorage (134); +12 more
- Contents (rolls 1): Flashlight - Angle Headed 3.2%, Funnel 3.2%, Marker - Black 3.2%, Measuring Tape 3.2%, Nails 3.2%, Nuts and Bolts 3.2%, Pliers 3.2%, Rubber Hose 3.2%, Screwdriver 3.2%, Screws 3.2%, Twine 3.2%, Whetstone 3.2%

### Tote — Tote Bag
- bag · capacity 6 · weight reduction 50% · weight 0.5 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 14 room defs: clothingstore (147), bakery (30), giftstore (28), bookstore (25), toystore (15), tailoringworkshop (13); +8 more
- Contents (rolls 1): empty table

### Tote_Bags — Tote Bag
- bag · capacity 6 · weight reduction 50% · weight 0.5 · MaxItemSize `2.0` · CLONE-Z: Sack
- Spawns in 8 room defs: livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), storageunit (695), attic (50); +2 more
- Contents (rolls 10): Plastic Bag 100.0%

### Tote_Clothing — Tote Bag
- bag · capacity 6 · weight reduction 50% · weight 0.5 · MaxItemSize `2.0` · CLONE-Z: Sack
- Not spawned by any room table.
- Contents (rolls 1): Long Socks 10.1%, Socks 10.1%, T-shirt 3.8%, Bra 3.5%, Underpants 3.5%, Boxers 3.1%, Bra - Strapless 3.0%, Briefs 2.5%, Pants 2.5%, Belt 2.0%, Hoodie 2.0%, Socks - Heavy 2.0%

### Wallet — Wallet
- bag · capacity 1 · weight 0.2 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 31 room defs: bedroom (11,303), livingroom (10,624), kitchen (7,401), closet (5,489), garagestorage (1,318), derelict (1,038); +25 more
- Contents (rolls 1): Money 68.1%, Photograph 6.2%, Business Card 6.2%, Credit Card 6.2%, Receipt 6.2%, Parking Ticket 2.5%, Speeding Ticket 2.5%, Postcard 1.2%, Handwritten Letter 0.6%, Scratch Ticket 0.1%, Doodle 0.06%, Silver Coin 0.06%

### Wallet_Female — Wallet
- bag · capacity 1 · weight 0.2 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 1 room def: `all`
- Contents (rolls 1): Money 51.1%, ID Card 23.2%, Credit Card 6.5%, Photograph 4.6%, Business Card 4.6%, Receipt 4.6%, Parking Ticket 1.9%, Speeding Ticket 1.9%, Postcard 0.9%, Handwritten Letter 0.5%, Scratch Ticket 0.09%, Doodle 0.05%

### Wallet_Hide — Hide Wallet
- bag · capacity 1 · weight 0.2 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 2 room defs: ww_blacksmith (1), ww_toolstore (1)
- Contents (rolls 1): Money 68.1%, Photograph 6.2%, Business Card 6.2%, Credit Card 6.2%, Receipt 6.2%, Parking Ticket 2.5%, Speeding Ticket 2.5%, Postcard 1.2%, Handwritten Letter 0.6%, Scratch Ticket 0.1%, Doodle 0.06%, Silver Coin 0.06%

### Wallet_Male — Wallet
- bag · capacity 1 · weight 0.2 · AcceptItemFunction `AcceptItemFunction.Wallet` · MaxItemSize `0.2` · CLONE-Z: Sack
- Spawns in 1 room def: `all`
- Contents (rolls 1): Money 51.1%, ID Card 23.2%, Credit Card 6.5%, Photograph 4.6%, Business Card 4.6%, Receipt 4.6%, Parking Ticket 1.9%, Speeding Ticket 1.9%, Postcard 0.9%, Handwritten Letter 0.5%, Scratch Ticket 0.09%, Doodle 0.05%

### WheatSack — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 9 room defs: closet (5,489), garagestorage (1,318), storageunit (695), barn (170), farmstorage (134), shed (35); +3 more
- Contents (rolls 6): Wheat Sheaf (Dried) 100.0%

### WheatSeedSack — Sack
- bag · capacity 15 · weight reduction 50% · weight 0.5 · run speed ×0.99 · CLONE-Z: Sack
- Spawns in 9 room defs: closet (5,489), garagestorage (1,318), storageunit (695), barn (170), farmstorage (134), shed (35); +3 more
- Contents (rolls 10): Wheat Seeds 100.0%

**— vehicle —**

### GloveBox — Glove Box
- vehicle part · capacity 10 · over 216 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 10×154, 5×52, 3×10 · icon Container_GloveCompartment · CLONE-Z: -
- Capacity = installed part item `GloveBox1/2/3` (MaxCapacity 3/10/5 for vehicle mechanicType 1/2/3) unless the vehicle script sets `container { capacity }`; reachable only from the front seats or through the open front-right door (`Vehicles.ContainerAccess.GloveBox`).
- Filled by 217 vehicle keys from 65 tables: GloveBox, ConstructionWorkerGloveBox, LuxuryGloveBox, PoliceGloveBox, MetalWelderGloveBox, MechanicGloveBox, CarpenterGloveBox, GardenerGloveBox; …
- Top loot: Paperback 3.4%, Receipt 3.1%, Notepad 2.9%, Magazine 2.9%, Pencil 2.9%, Tissue 2.8%, Battery 2.7%, Bandage - Adhesive 2.7%, Paperwork 2.6%, Pen - Black 2.4%, Cigarette Pack 2.3%, Alcohol Wipes 2.2%

### SeatFrontLeft — Driver's Seat
- vehicle part · capacity 20 · over 216 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 20×216 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- Filled by 217 vehicle keys from 7 tables: DriverSeat, Seat, TailoringSeatFront, LeatherSeatFront, ButcherSeatFront, EvacueeSeatFront, DancerSeatFront
- Top loot: Notepad 5.8%, Pencil 5.8%, Magazine 5.6%, Tissue 5.5%, Adhesive Tape 4.4%, Cigarette Pack 4.4%, Matchbook 4.4%, Paper Napkins 4.4%, Pen - Black 4.4%, Pen - Blue 4.4%, Pen - Red 4.4%, Eye Makeup 3.3%

### SeatFrontRight — Front Right Seat
- vehicle part · capacity 20 · over 216 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 20×216 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- Filled by 211 vehicle keys from 58 tables: ConstructionWorkerSeatFront, LuxurySeat, MetalWelderSeatFront, MechanicSeatFront, CarpenterSeatFront, PoliceSeatFront, GardenerSeatFront, EmptySeat; …
- Top loot: Magazine 4.2%, Tissue 3.5%, Notepad 3.4%, Pencil 3.4%, Paper Napkins 2.7%, Cigarette Pack 2.7%, Adhesive Tape 2.7%, Matchbook 2.7%, Pen - Black 2.7%, Pen - Red 2.7%, Pen - Blue 2.6%, Rubber Band 2.0%

### SeatMiddleLeft — Middle Left Seat
- vehicle part · capacity 20 · over 9 vehicle scripts (e.g. VanSeats, VanSeatsAirportShuttle, VanSeats_Creature): 20×9 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- No vehicle distribution fills this part.

### SeatMiddleRight — Middle Right Seat
- vehicle part · capacity 20 · over 9 vehicle scripts (e.g. VanSeats, VanSeatsAirportShuttle, VanSeats_Creature): 20×9 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- No vehicle distribution fills this part.

### SeatRearLeft — Rear Left Seat
- vehicle part · capacity 20 · over 61 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 20×61 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- Filled by 113 vehicle keys from 21 tables: SeatRear, LuxurySeatRear, PoliceSeatRear, RangerSeatRear, SurvivalistSeatRear, DrinkerSeatRear, TaxiSeatRear, PackRatSeatRear; …
- Top loot: Magazine 5.9%, Tissue 5.4%, Notepad 4.9%, Pencil 4.9%, Plastic Bag 4.6%, Paper Napkins 4.0%, Adhesive Tape 3.9%, Matchbook 3.9%, Pen - Black 3.9%, Pen - Blue 3.9%, Pen - Red 3.9%, Rubber Band 2.9%

### SeatRearRight — Rear Right Seat
- vehicle part · capacity 20 · over 61 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 20×61 · icon Container_Carseat · CLONE-Z: -
- Capacity from the seat part item (`NormalCarSeat1-3`, MaxCapacity 20); quartered while someone sits there (`ItemContainer.getCapacity`); an empty seat always accepts one item of weight ≤ 50; a corpse fits only if the seat holds ≤ 5 weight.
- Filled by 113 vehicle keys from 21 tables: SeatRear, LuxurySeatRear, PoliceSeatRear, RangerSeatRear, SurvivalistSeatRear, DrinkerSeatRear, TaxiSeatRear, PackRatSeatRear; …
- Top loot: Magazine 5.9%, Tissue 5.4%, Notepad 4.9%, Pencil 4.9%, Plastic Bag 4.6%, Paper Napkins 4.0%, Adhesive Tape 3.9%, Matchbook 3.9%, Pen - Black 3.9%, Pen - Blue 3.9%, Pen - Red 3.9%, Rubber Band 2.9%

### TrailerAnimalEggs — Egg Container
- vehicle part · capacity 10 · over 2 vehicle scripts (e.g. Trailer_Horsebox, Trailer_Livestock): 10×2 · icon - · CLONE-Z: -
- Livestock-trailer egg container (`template_trunk.txt`, capacity 10); no loot distribution.
- No vehicle distribution fills this part.

### TrailerAnimalFood — Food Container
- vehicle part · capacity 20 · over 2 vehicle scripts (e.g. Trailer_Horsebox, Trailer_Livestock): 20×2 · icon - · CLONE-Z: -
- Livestock-trailer container (`template_trunk.txt`, capacity 20, `Vehicles.Update.TrailerAnimalFood`); no loot distribution.
- No vehicle distribution fills this part.

### TrailerTrunk — Trailer
- vehicle part · capacity 100 · over 2 vehicle scripts (e.g. Trailer, TrailerCover): 100×2 · icon Container_TruckBed · CLONE-Z: -
- Template capacity 100, not condition-scaled; `VehiclePart.setCondition` resets it to max(80, condition).
- Filled by 138 vehicle keys from 19 tables: TrunkHeavy, TrunkStandard, ConstructionWorkerTruckBed, MetalWelderTruckBed, MechanicTruckBed, CarpenterTruckBed, GardenerTruckBed, ElectricianTruckBed; …
- Top loot: Plank 11.2%, Brick 3.7%, Rope 2.6%, Rubber Hose 1.9%, Wire 1.8%, Sack 1.6%, Box of Nails 1.6%, Rag 1.4%, Rag (Dirty) 1.4%, Duct Tape 1.4%, Gas Can (empty) 1.3%, Screwdriver 1.3%

### TruckBed — Trunk
- vehicle part · capacity 130 · over 199 vehicle scripts (e.g. CarLightsBulletinSheriff, CarLightsKST, CarLightsLouisvilleCounty): 130×86, 160×37, 45×19, 55×17, 40×15, 50×9, 60×6, 85×5, 20×3, 30×2 · icon Container_TruckBed · CLONE-Z: -
- Capacity from the trunk part item (Small/Normal/BigTrunk1-3, VanSeatsTrunk2: 30–150) or the vehicle's `container { capacity }` override, scaled by part condition (`conditionAffectsCapacity`, minimum 5); needs the trunk door open (`ContainerAccess.TruckBed`).
- Filled by 217 vehicle keys from 72 tables: TrunkHeavy, TrunkStandard, ConstructionWorkerTruckBed, LuxuryTruckBed, PoliceTruckBed, MetalWelderTruckBed, MechanicTruckBed, CarpenterTruckBed; …
- Top loot: Plank 6.9%, Brick 2.5%, Rope 1.8%, Gas Can (empty) 1.7%, Rubber Hose 1.6%, Sack 1.4%, Plastic Bag 1.4%, Wire 1.3%, Newspaper 1.3%, Magazine 1.3%, Ratchet Wrench 1.3%, Screwdriver 1.3%

### TruckBedOpen — Trunk
- vehicle part · capacity 110 · over 17 vehicle scripts (e.g. PickUpTruck, PickUpTruckJPLandscaping, PickUpTruckLightsAirport): 110×17 · icon Container_TruckBed · CLONE-Z: -
- Open pickup beds: no door check (`ContainerAccess.TruckBedOpen`); capacity 110 on every vehicle that has one.
- Filled by 165 vehicle keys from 38 tables: TrunkHeavy, ConstructionWorkerTruckBed, LuxuryTruckBed, MetalWelderTruckBed, MechanicTruckBed, CarpenterTruckBed, GardenerTruckBed, ElectricianTruckBed; …
- Top loot: Plank 9.7%, Brick 3.5%, Rope 2.3%, Rubber Hose 2.1%, Sack 1.8%, Gas Can (empty) 1.8%, Wire 1.7%, Rag 1.4%, Rag (Dirty) 1.4%, Plastic Bag 1.4%, Stone Block 1.4%, Box of Nails 1.4%

**— body —**

### body — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Profession houses (`RBBasic.doProfessionBuilding`, rooms Carpenter/Chef/Farmer/Nurse): the profession corpse's inventory rolls `<Profession>.body`.
- Rooms (4 defs): Carpenter (0), Chef (0), Farmer (0), Nurse (0)
- Direct tables only (4).
- Top loot: Alcohol Wipes 6.1%, Bandage 6.1%, Bandage - Adhesive 6.1%, Painkillers 6.1%, Claw Hammer 3.1%, Cooking Recipe Clipping 3.1%, Nails 3.1%, Fillet Knife 1.5%, Hacksaw 1.5%, Kitchen Knife 1.5%, Meat Cleaver 1.5%, Paring Knife 1.5%

### inventoryfemale — Corpse
- body · capacity 12 · icon Container_DeadPerson_FemaleZombie · CLONE-Z: Corpse
- Engine: registered `ContainerType`.
- Same rules as `inventorymale` (capacity 12, outfit table first, default table second).
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Wallet 54.3%, ID Card 21.7%, Comb 1.1%, Credit Card 1.1%, Diary 1.1%, Earbuds 1.1%, Eye Makeup 1.1%, Foundation Makeup 1.1%, Handwritten Letter 1.1%, Lipstick 1.1%, Locket 1.1%, Magazine 1.1%

### inventorymale — Corpse
- body · capacity 12 · icon Container_DeadPerson_MaleZombie · CLONE-Z: Corpse
- Engine: registered `ContainerType`.
- `IsoDeadBody.setContainer`: capacity 12 (reset to 8 when a saved corpse is reloaded, `readInventory`). Filled from `all.Outfit_<outfit>` first, then this default table when the outfit keeps `defaultInventoryLoot` (`ItemPickerJava.fillContainerInternal`); skeletons are never filled; worn bags use the outfit's `bags` table or the bag's own table.
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Wallet 56.4%, ID Card 22.6%, Comb 1.1%, Credit Card 1.1%, Diary 1.1%, Earbuds 1.1%, Handwritten Letter 1.1%, Locket 1.1%, Magazine 1.1%, Newspaper 1.1%, Note 1.1%, Notebook 1.1%

### Outfit_Agent — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Agent` (`all.Outfit_Agent`, rolls 1).
- Loot: Badge 80.0%, Notepad 8.0%, Pen - Black 8.0%, Lighter - Disposable 3.2%, Cigarette Pack 0.8%

### Outfit_AirportSecurityTarmac — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `AirportSecurityTarmac` (`all.Outfit_AirportSecurityTarmac`, rolls 1).
- Loot: Badge 80.6%, Notepad 8.1%, Pen - Black 8.1%, Lighter - Disposable 3.2%

### Outfit_AmbulanceDriver — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `AmbulanceDriver` (`all.Outfit_AmbulanceDriver`, rolls 1).
- Loot: Notepad 15.6%, Pencil 15.6%, Penlight 15.6%, Surgical Gloves 15.6%, Surgical Mask 15.6%, Flashlight 12.5%, Pager 6.2%, First Aid Kit 3.1%

### Outfit_ArmyCamoDesert — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ArmyCamoDesert` (`all.Outfit_ArmyCamoDesert`, rolls 1).
- Loot: 5.56x45mm Round 12.6%, Compass 12.6%, Dehydrated Meat Stick 12.6%, Notepad 12.6%, P38 Can Opener 12.6%, Pencil 12.6%, Beef Jerky 10.1%, M16 Magazine 10.1%, Box of 5.56x45mm Rounds 1.3%, Chewing Tobacco 1.3%, Plastic Spork 1.3%, Handiknife 0.1%

### Outfit_ArmyCamoGreen — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ArmyCamoGreen` (`all.Outfit_ArmyCamoGreen`, rolls 1).
- Loot: 5.56x45mm Round 12.6%, Compass 12.6%, Dehydrated Meat Stick 12.6%, Notepad 12.6%, P38 Can Opener 12.6%, Pencil 12.6%, Beef Jerky 10.1%, M16 Magazine 10.1%, Box of 5.56x45mm Rounds 1.3%, Chewing Tobacco 1.3%, Plastic Spork 1.3%, Handiknife 0.1%

### Outfit_Backpacker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Backpacker` (`all.Outfit_Backpacker`, rolls 1).
- Loot: Passport 31.6%, Plastic Spork 15.8%, Lighter - Disposable 12.7%, Rolled Cigarette 12.7%, Rolling Papers 6.3%, Sports Bottle 6.3%, Magazine: Legalize It! 3.2%, Compass 1.6%, Disposable Camera 1.6%, Handwritten Letter 1.6%, Photograph 1.6%, Postcard 1.6%

### Outfit_Bandit — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bandit` (`all.Outfit_Bandit`, rolls 1); worn bags filled from BagsAndContainers.BanditBag.
- Loot: Hunting Knife 22.1%, Rolled Cigarette 22.1%, Switchblade 22.1%, Firecrackers 11.0%, Rolling Papers 11.0%, Chewing Tobacco 2.8%, Flask 2.8%, Pouch of Tobacco 2.8%, Whetstone 2.8%, Handiknife 0.3%, Pocket Knife 0.3%

### Outfit_Bandit_Early — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bandit_Early` (`all.Outfit_Bandit_Early`, rolls 1); worn bags filled from BagsAndContainers.BanditBag_Early.
- Loot: Hunting Knife 22.1%, Rolled Cigarette 22.1%, Switchblade 22.1%, Firecrackers 11.0%, Rolling Papers 11.0%, Chewing Tobacco 2.8%, Flask 2.8%, Pouch of Tobacco 2.8%, Whetstone 2.8%, Handiknife 0.3%, Pocket Knife 0.3%

### Outfit_Bandit_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bandit_Late` (`all.Outfit_Bandit_Late`, rolls 1); worn bags filled from BagsAndContainers.BanditBag_Late.
- Loot: Hunting Knife 22.1%, Rolled Cigarette 22.1%, Switchblade 22.1%, Firecrackers 11.0%, Rolling Papers 11.0%, Chewing Tobacco 2.8%, Flask 2.8%, Pouch of Tobacco 2.8%, Whetstone 2.8%, Handiknife 0.3%, Pocket Knife 0.3%

### Outfit_Bandit_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bandit_Mid` (`all.Outfit_Bandit_Mid`, rolls 1); worn bags filled from BagsAndContainers.BanditBag_Mid.
- Loot: Hunting Knife 22.1%, Rolled Cigarette 22.1%, Switchblade 22.1%, Firecrackers 11.0%, Rolling Papers 11.0%, Chewing Tobacco 2.8%, Flask 2.8%, Pouch of Tobacco 2.8%, Whetstone 2.8%, Handiknife 0.3%, Pocket Knife 0.3%

### Outfit_BaseballPlayer_KY — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `BaseballPlayer_KY` (`all.Outfit_BaseballPlayer_KY`, rolls 1).
- Loot: Gum 66.7%, Chewing Tobacco 33.3%

### Outfit_BaseballPlayer_Rangers — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `BaseballPlayer_Rangers` (`all.Outfit_BaseballPlayer_Rangers`, rolls 1).
- Loot: Gum 66.7%, Chewing Tobacco 33.3%

### Outfit_BaseballPlayer_Z — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `BaseballPlayer_Z` (`all.Outfit_BaseballPlayer_Z`, rolls 1).
- Loot: Gum 66.7%, Chewing Tobacco 33.3%

### Outfit_Bathrobe — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bathrobe` (`all.Outfit_Bathrobe`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_Biker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Biker` (`all.Outfit_Biker`, rolls 1).
- Loot: Beer Can 22.7%, Hunting Knife 18.1%, Switchblade 18.1%, Cigarillo 9.1%, Rolled Cigarette 9.1%, SN38 Revolver 9.1%, Chewing Tobacco 2.3%, Cigarette Pack 2.3%, Flask 2.3%, M9 Pistol 2.3%, Pouch of Tobacco 2.3%, Rolling Papers 2.3%

### Outfit_Bob — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Bob` (`all.Outfit_Bob`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_BountyHunter — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `BountyHunter` (`all.Outfit_BountyHunter`, rolls 1).
- Loot: Badge 65.8%, Zip Ties 13.2%, Magazine 10.5%, Notepad 6.6%, Lighter - Disposable 2.6%, Cigarette Pack 0.7%, SN38 Revolver 0.7%

### Outfit_Camper — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Camper` (`all.Outfit_Camper`, rolls 1).
- Loot: Compass 23.7%, Insect Repellent 23.7%, Plastic Spork 23.7%, Dehydrated Meat Stick 9.5%, Granola Bar 9.5%, Whistle 4.7%, Magnesium Firestarter 2.4%, Water Purification Tablets 2.4%, Handiknife 0.2%, Pocket Knife 0.2%

### Outfit_Classy — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Classy` (`all.Outfit_Classy`, rolls 1).
- Loot: Money 68.6%, Credit Card 20.0%, Business Card 2.9%, Stock Certificate 2.9%, Fountain Pen 2.3%, Lighter 1.1%, Pocketwatch 1.1%, Cigar 0.6%, Magazine 0.3%, Passport 0.3%

### Outfit_ClubGoer — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ClubGoer` (`all.Outfit_ClubGoer`, rolls 1).
- Loot: Caffeine Pills 21.3%, Gum 21.3%, Gummy Bears 10.6%, Gummy Fish Candy 10.6%, Gummy Worms 10.6%, Lollipop 10.6%, Whistle 10.6%, Paperback 4.3%

### Outfit_ConstructionWorker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ConstructionWorker` (`all.Outfit_ConstructionWorker`, rolls 1).
- Loot: Nails 25.7%, Screws 25.7%, Duct Tape 12.9%, Notepad 12.9%, Pencil 12.9%, Lighter - Disposable 5.1%, Chewing Tobacco 1.3%, Cigarette Pack 1.3%, Flask 1.3%, Cigar 0.6%, Handiknife 0.1%, Pocket Knife 0.1%

### Outfit_Cook_Generic — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Cook_Generic` (`all.Outfit_Cook_Generic`, rolls 1).
- Loot: Dish Towel 44.4%, Notepad 22.2%, Pencil 22.2%, Lighter - Disposable 8.9%, Cigarette Pack 2.2%

### Outfit_Cook_Spiffos — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Cook_Spiffos` (`all.Outfit_Cook_Spiffos`, rolls 1).
- Loot: Dish Towel 44.4%, Notepad 22.2%, Pencil 22.2%, Lighter - Disposable 8.9%, Cigarette Pack 2.2%

### Outfit_CostumeWildWestBoss — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `CostumeWildWestBoss` (`all.Outfit_CostumeWildWestBoss`, rolls 1).
- Loot: Toy Badge 100.0%

### Outfit_CostumeWildWestLawman — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `CostumeWildWestLawman` (`all.Outfit_CostumeWildWestLawman`, rolls 1); worn bags filled from BagsAndContainers.Empty.
- Loot: Toy Badge 100.0%

### Outfit_CostumeWildWestMayor — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `CostumeWildWestMayor` (`all.Outfit_CostumeWildWestMayor`, rolls 1).
- Loot: Toy Badge 100.0%

### Outfit_CostumeWildWestOutlaw — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `CostumeWildWestOutlaw` (`all.Outfit_CostumeWildWestOutlaw`, rolls 1); worn bags filled from BagsAndContainers.Empty.
- Loot: empty table

### Outfit_Cultist — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Cultist` (`all.Outfit_Cultist`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_Dean — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Dean` (`all.Outfit_Dean`, rolls 1); skips the default inventorymale/female table.
- Loot: ID Card 28.6%, Compass 14.3%, Insect Repellent 14.3%, Magnesium Firestarter 14.3%, P38 Can Opener 14.3%, Water Purification Tablets 14.3%

### Outfit_Detective — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Detective` (`all.Outfit_Detective`, rolls 1).
- Loot: Badge 69.0%, Notepad 6.9%, Pager 6.9%, Pencil 6.9%, SN38 Revolver 6.9%, Lighter - Disposable 2.8%, Cigarette Pack 0.7%

### Outfit_Doctor — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Doctor` (`all.Outfit_Doctor`, rolls 1).
- Loot: Pager 61.0%, Penlight 12.2%, Surgical Gloves 12.2%, Surgical Mask 12.2%, First Aid Kit 2.4%

### Outfit_Duke — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Duke` (`all.Outfit_Duke`, rolls 1); skips the default inventorymale/female table.
- Loot: Camera Film 34.4%, Credit Card 15.6%, Electrical Wire 15.6%, ID Card 15.6%, Pager 15.6%, Battery 3.1%

### Outfit_Evacuee — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Evacuee` (`all.Outfit_Evacuee`, rolls 1).
- Loot: Photograph 39.7%, Diary 7.9%, Dog Chew Toy 4.0%, Doll 4.0%, Doodle 4.0%, Handwritten Letter 4.0%, Leash 4.0%, Mouse Toy 4.0%, Necklace - Crucifix 4.0%, Reading Glasses 4.0%, Teddy Bear 4.0%, Toilet Paper 4.0%

### Outfit_ExterminatorSuited — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ExterminatorSuited` (`all.Outfit_ExterminatorSuited`, rolls 1).
- Loot: Mouse Trap 30.3%, Duct Tape 15.2%, Garbage Bag 15.2%, Rat Poison 15.2%, Rubber Gloves 15.2%, Lighter - Disposable 6.1%, Cigarette Pack 1.5%, Respirator Filters 1.5%

### Outfit_Farmer — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Farmer` (`all.Outfit_Farmer`, rolls 1).
- Loot: Chewing Tobacco 21.0%, Magazine: American Homesteading 2.6%, Magazine: Blooms and Blossoms 2.6%, Magazine: Cropping for Cash 2.6%, Magazine: Growing Your Own 2.6%, Magazine: Kentucky Farmer - June 1993 2.6%, Magazine: Kentucky Farmer - May 1993 2.6%, Magazine: The Farmers Guide 2.6%, Magazine: Thyme Time 2.6%, Pouch 2.6%, Seed Packet - Basil 2.6%, Seed Packet - Broccoli 2.6%

### Outfit_FiremanFullSuit — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `FiremanFullSuit` (`all.Outfit_FiremanFullSuit`, rolls 1).
- Loot: Badge 90.9%, First Aid Kit 9.1%

### Outfit_FiremanStripper — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `FiremanStripper` (`all.Outfit_FiremanStripper`, rolls 1).
- Loot: Money 100.0%

### Outfit_Fisherman — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Fisherman` (`all.Outfit_Fisherman`, rolls 1).
- Loot: Fishing Hook 11.5%, Fishing Line 11.5%, Insect Repellent 11.5%, Jig Lure 11.5%, Minnow Lure 11.5%, Pliers 11.5%, Chewing Tobacco 9.2%, Fillet Knife 9.2%, Premium Fishing Line 4.6%, Magazine: Angler USA - June 1993 2.3%, Magazine: Angler USA - May 1993 2.3%, Box of Fishing Hooks 1.1%

### Outfit_Foreman — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Foreman` (`all.Outfit_Foreman`, rolls 1).
- Loot: Calculator 15.5%, Clipboard 15.5%, Graph Paper 15.5%, Notepad 15.5%, Pencil 15.5%, Pen - Blue 12.4%, Lighter - Disposable 6.2%, Chewing Tobacco 1.6%, Cigarette Pack 1.6%, Cigar 0.8%

### Outfit_Fossoil — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Fossoil` (`all.Outfit_Fossoil`, rolls 1).
- Loot: Rag 38.5%, Rag (Dirty) 38.5%, Lighter - Disposable 15.4%, Chewing Tobacco 3.8%, Cigarette Pack 3.8%

### Outfit_Frank_Hemingway — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Frank_Hemingway` (`all.Outfit_Frank_Hemingway`, rolls 1); skips the default inventorymale/female table.
- Loot: Credit Card 33.3%, ID Card 33.3%, Pager 33.3%

### Outfit_Gas2Go — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Gas2Go` (`all.Outfit_Gas2Go`, rolls 1).
- Loot: Rag 43.5%, Rag (Dirty) 43.5%, Lighter - Disposable 8.7%, Chewing Tobacco 2.2%, Cigarette Pack 2.2%

### Outfit_Gaudy — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Gaudy` (`all.Outfit_Gaudy`, rolls 1).
- Loot: Credit Card 62.5%, Money 29.6%, Business Card 3.3%, Paperback 3.3%, Cigar 0.7%, Lighter 0.7%

### Outfit_Ghillie — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Ghillie` (`all.Outfit_Ghillie`, rolls 1).
- Loot: 5.56x45mm Round 12.6%, Compass 12.6%, Dehydrated Meat Stick 12.6%, Notepad 12.6%, P38 Can Opener 12.6%, Pencil 12.6%, Beef Jerky 10.1%, M16 Magazine 10.1%, Box of 5.56x45mm Rounds 1.3%, Chewing Tobacco 1.3%, Handiknife 1.3%, Magazine: HottieZ 0.1%

### Outfit_Golfer — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Golfer` (`all.Outfit_Golfer`, rolls 1).
- Loot: Golf Ball 21.7%, Golf Tee 21.7%, Notepad 21.7%, Pencil 21.7%, Pager 8.7%, Magazine 2.2%, Paperback 2.2%

### Outfit_Goth — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Goth` (`all.Outfit_Goth`, rolls 1).
- Loot: Candy Corn 11.8%, Caramel Candies 11.8%, Eye Makeup 11.8%, Foundation Makeup 11.8%, Lipstick 11.8%, Lollipop 11.8%, Rubber Spider 11.8%, Diary 4.7%, Fountain Pen 4.7%, Lighter - Disposable 4.7%, Cigarette Pack 1.2%, Paperback 1.2%

### Outfit_Groucho — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Groucho` (`all.Outfit_Groucho`, rolls 1).
- Loot: Cigar 66.7%, Magazine 33.3%

### Outfit_Grunge — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Grunge` (`all.Outfit_Grunge`, rolls 1).
- Loot: Beer Can 24.3%, Painkillers 24.3%, Rolled Cigarette 19.4%, Lighter - Disposable 9.7%, Pouch of Tobacco 9.7%, Rolling Papers 9.7%, Flask 2.4%, Firecrackers 0.2%, Magazine: HottieZ 0.2%

### Outfit_GuitarGuy — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `GuitarGuy` (`all.Outfit_GuitarGuy`, rolls 1).
- Loot: Guitar Pick 84.7%, Beer Can 8.5%, Lighter - Disposable 3.4%, Cigarette Pack 0.8%, Flask 0.8%, Magazine 0.8%, Paperback 0.8%

### Outfit_Hobbo — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Hobbo` (`all.Outfit_Hobbo`, rolls 1).
- Loot: Bottle of Whiskey (empty) 22.2%, Rolled Cigarette 22.2%, Rolling Papers 22.2%, Toilet Paper 22.2%, Flask 8.9%, Pouch of Tobacco 2.2%, Pocket Knife 0.2%

### Outfit_Hobbyist — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Hobbyist` (`all.Outfit_Hobbyist`, rolls 1).
- Loot: Pouch 100.0%

### Outfit_HonorStudent — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `HonorStudent` (`all.Outfit_HonorStudent`, rolls 1).
- Loot: Pen - Blue 23.3%, Pen - Red 23.3%, Tissue 23.3%, Calculator 18.6%, Geometry Compass 9.3%, Fountain Pen 2.3%

### Outfit_HospitalPatient — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `HospitalPatient` (`all.Outfit_HospitalPatient`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_HospitalPatientBathrobe — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `HospitalPatientBathrobe` (`all.Outfit_HospitalPatientBathrobe`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_Hunter — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Hunter` (`all.Outfit_Hunter`, rolls 1).
- Loot: .30-30 Round 10.8%, 12g Round 10.8%, 5.56x45mm Round 10.8%, 7.62x51mm Round 10.8%, Chewing Tobacco 10.8%, Compass 10.8%, Dehydrated Meat Stick 10.8%, Insect Repellent 10.8%, Beef Jerky 8.7%, Whistle 2.2%, Water Purification Tablets 1.1%, Whetstone 1.1%

### Outfit_Inmate — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Inmate` (`all.Outfit_Inmate`, rolls 1); skips the default inventorymale/female table.
- Loot: Rolled Cigarette 76.9%, Glass Shiv 7.7%, Shiv 7.7%, Simple Metal Knife 7.7%

### Outfit_InmateKhaki — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `InmateKhaki` (`all.Outfit_InmateKhaki`, rolls 1); skips the default inventorymale/female table.
- Loot: Rolled Cigarette 76.9%, Glass Shiv 7.7%, Shiv 7.7%, Simple Metal Knife 7.7%

### Outfit_Jackie_Jaye — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Jackie_Jaye` (`all.Outfit_Jackie_Jaye`, rolls 1); skips the default inventorymale/female table.
- Loot: Credit Card 25.0%, ID Card 25.0%, Lipstick 25.0%, Pager 25.0%

### Outfit_Joan — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Joan` (`all.Outfit_Joan`, rolls 1); skips the default inventorymale/female table.
- Loot: Credit Card 33.3%, ID Card 33.3%, Pager 33.3%

### Outfit_John — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `John` (`all.Outfit_John`, rolls 1); skips the default inventorymale/female table.
- Loot: ID Card 100.0%

### Outfit_Judge_Matt_Hass — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Judge_Matt_Hass` (`all.Outfit_Judge_Matt_Hass`, rolls 1); skips the default inventorymale/female table.
- Loot: Photograph of Judge Matt Hass (Autographed) 66.7%, ID Card 16.7%, Security Pass Key Ring 16.7%

### Outfit_Kate — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Kate` (`all.Outfit_Kate`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_Kirsty_Kormick — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Kirsty_Kormick` (`all.Outfit_Kirsty_Kormick`, rolls 1); skips the default inventorymale/female table.
- Loot: Credit Card 33.3%, ID Card 33.3%, Pager 33.3%

### Outfit_Mayor_West_point — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Mayor_West_point` (`all.Outfit_Mayor_West_point`, rolls 1); skips the default inventorymale/female table.
- Loot: Business Card 28.6%, Credit Card 28.6%, ID Card 28.6%, Cigar 14.3%

### Outfit_McCoys — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `McCoys` (`all.Outfit_McCoys`, rolls 1).
- Loot: Nails 20.3%, Notepad 20.3%, Pencil 20.3%, Duct Tape 16.3%, Screws 16.3%, Chewing Tobacco 2.0%, Cigarette Pack 2.0%, Whetstone 2.0%, Handiknife 0.2%, Pocket Knife 0.2%

### Outfit_Mechanic — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Mechanic` (`all.Outfit_Mechanic`, rolls 1).
- Loot: Notepad 18.8%, Pencil 18.8%, Rag 18.8%, Rag (Dirty) 18.8%, Screws 18.8%, Chewing Tobacco 1.9%, Cigarette Pack 1.9%, Magazine 1.9%, Handiknife 0.2%, Pocket Knife 0.2%

### Outfit_MetalWorker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `MetalWorker` (`all.Outfit_MetalWorker`, rolls 1).
- Loot: Notepad 19.9%, Pencil 19.9%, Rag 19.9%, Rag (Dirty) 19.9%, Screws 15.9%, Chewing Tobacco 2.0%, Cigarette Pack 2.0%, Handiknife 0.2%, Pocket Knife 0.2%

### Outfit_Mob — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Mob` (`all.Outfit_Mob`, rolls 1).
- Loot: Ice Pick 33.3%, Money Bundle 33.3%, Suspicious Package 33.3%

### Outfit_Naked — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Naked` (`all.Outfit_Naked`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_NakedVeil — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `NakedVeil` (`all.Outfit_NakedVeil`, rolls 1); skips the default inventorymale/female table.
- Loot: empty table

### Outfit_Nolan — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Nolan` (`all.Outfit_Nolan`, rolls 1); skips the default inventorymale/female table.
- Loot: Business Card 41.2%, Flier 14.7%, ID Card 14.7%, Key Ring - Nolan's Used Cars 14.7%, Pager 14.7%

### Outfit_Nurse — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Nurse` (`all.Outfit_Nurse`, rolls 1).
- Loot: Penlight 22.7%, Surgical Gloves 22.7%, Surgical Mask 22.7%, Flashlight 18.2%, Pager 9.1%, First Aid Kit 4.5%

### Outfit_OfficeWorker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `OfficeWorker` (`all.Outfit_OfficeWorker`, rolls 1).
- Loot: Credit Card 26.0%, Business Card 13.0%, Pencil 13.0%, Eraser 10.4%, Pen - Black 10.4%, Pen - Blue 10.4%, Pen - Red 10.4%, Calculator 1.3%, Clipboard 1.3%, Magazine 1.3%, Pager 1.3%, Paperwork 1.3%

### Outfit_OfficeWorkerSkirt — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `OfficeWorkerSkirt` (`all.Outfit_OfficeWorkerSkirt`, rolls 1).
- Loot: Credit Card 26.0%, Business Card 13.0%, Pencil 13.0%, Eraser 10.4%, Pen - Black 10.4%, Pen - Blue 10.4%, Pen - Red 10.4%, Calculator 1.3%, Clipboard 1.3%, Magazine 1.3%, Pager 1.3%, Paperwork 1.3%

### Outfit_Pharmacist — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Pharmacist` (`all.Outfit_Pharmacist`, rolls 1).
- Loot: Surgical Gloves 38.5%, Surgical Mask 38.5%, Pager 15.4%, First Aid Kit 7.7%

### Outfit_PokerDealer — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `PokerDealer` (`all.Outfit_PokerDealer`, rolls 1).
- Loot: Money 33.3%, Playing Cards 33.3%, Poker Chips 33.3%

### Outfit_Police — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Police` (`all.Outfit_Police`, rolls 1).
- Loot: Badge 50.5%, 9x19mm Round 10.1%, Notepad 10.1%, Zip Ties 10.1%, M9 Magazine 8.1%, Pen - Black 8.1%, Whistle 2.0%, Box of 9x19mm Rounds 1.0%, Pocket Knife 0.1%

### Outfit_PoliceState — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `PoliceState` (`all.Outfit_PoliceState`, rolls 1).
- Loot: Badge 46.1%, .38 Special Round 9.2%, Notepad 9.2%, Zip Ties 9.2%, Pen - Black 7.4%, .357 Magnum Round 5.5%, .44 Magnum Round 5.5%, .45 ACP Round 3.7%, Whistle 1.8%, Box of .38 Special Rounds 0.9%, Box of .357 Magnum Rounds 0.5%, Box of .44 Magnum Rounds 0.5%

### Outfit_PoliceStripper — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `PoliceStripper` (`all.Outfit_PoliceStripper`, rolls 1).
- Loot: Money 100.0%

### Outfit_Priest — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Priest` (`all.Outfit_Priest`, rolls 1).
- Loot: Necklace - Crucifix 100.0%

### Outfit_PrisonGuard — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `PrisonGuard` (`all.Outfit_PrisonGuard`, rolls 1).
- Loot: Badge 62.5%, Notepad 12.5%, Zip Ties 12.5%, Pen - Black 10.0%, Whistle 2.5%

### Outfit_PrivateMilitia — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `PrivateMilitia` (`all.Outfit_PrivateMilitia`, rolls 1); worn bags filled from BagsAndContainers.BanditBag.
- Loot: 12g Round 13.0%, 5.56x45mm Round 13.0%, 7.62x51mm Round 13.0%, Dehydrated Meat Stick 13.0%, Zip Ties 13.0%, Beef Jerky 10.4%, Butterfly Knife 10.4%, Switchblade 10.4%, Whistle 2.6%, Chewing Tobacco 1.3%

### Outfit_Punk — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Punk` (`all.Outfit_Punk`, rolls 1).
- Loot: Beer Bottle 16.1%, Beer Can 16.1%, Bottle of Whiskey 16.1%, Switchblade 12.9%, Bottle of Hair Dye 9.6%, Hair Spray 9.6%, Cigarillo 6.4%, Comb 6.4%, Hair Gel 3.2%, Cigarette Pack 1.6%, Rolling Papers 1.6%, Firecrackers 0.2%

### Outfit_Raider — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Raider` (`all.Outfit_Raider`, rolls 1).
- Loot: Rolled Cigarette 39.8%, Hunting Knife 23.9%, Switchblade 23.9%, Whistle 8.0%, Chewing Tobacco 4.0%, Bottle of Whiskey 0.4%

### Outfit_Ranger — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Ranger` (`all.Outfit_Ranger`, rolls 1).
- Loot: Badge 70.3%, 12g Round 7.0%, Compass 7.0%, Notebook 7.0%, Pen - Black 5.6%, Whistle 1.4%, Box of 12g Rounds 0.7%, Chewing Tobacco 0.7%, Handiknife 0.07%, Pocket Knife 0.07%

### Outfit_Redneck — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Redneck` (`all.Outfit_Redneck`, rolls 1).
- Loot: Beer Can 25.3%, Chewing Tobacco 25.3%, Dehydrated Meat Stick 25.3%, Beef Jerky 20.2%, Cigarette Pack 2.5%, Bottle of Whiskey 0.3%, Butterfly Knife 0.3%, Handiknife 0.3%, Magazine: HottieZ 0.3%, Pocket Knife 0.3%, Scratch Ticket 0.3%

### Outfit_Rev_Peter_Watts — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Rev_Peter_Watts` (`all.Outfit_Rev_Peter_Watts`, rolls 1); skips the default inventorymale/female table.
- Loot: ID Card 50.0%, Key Ring - Praying Hands 50.0%

### Outfit_Rocker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Rocker` (`all.Outfit_Rocker`, rolls 1).
- Loot: Beer Can 54.9%, Lighter - Disposable 22.0%, Cigarette Pack 5.5%, Flask 5.5%, Pouch of Tobacco 5.5%, Rolling Papers 5.5%, Firecrackers 0.5%, Magazine: HottieZ 0.5%

### Outfit_Sanitation — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Sanitation` (`all.Outfit_Sanitation`, rolls 1).
- Loot: Garbage Bag 43.5%, Dust Mask 21.7%, Rubber Gloves 21.7%, Lighter - Disposable 8.7%, Cigarette Pack 2.2%, Flask 2.2%

### Outfit_Sir_Twiggy — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Sir_Twiggy` (`all.Outfit_Sir_Twiggy`, rolls 1); skips the default inventorymale/female table.
- Loot: ID Card 100.0%

### Outfit_Stripper — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Stripper` (`all.Outfit_Stripper`, rolls 1).
- Loot: Money 100.0%

### Outfit_StripperBlack — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `StripperBlack` (`all.Outfit_StripperBlack`, rolls 1).
- Loot: Money 100.0%

### Outfit_StripperNaked — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `StripperNaked` (`all.Outfit_StripperNaked`, rolls 1).
- Loot: Money 100.0%

### Outfit_StripperPink — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `StripperPink` (`all.Outfit_StripperPink`, rolls 1).
- Loot: Money 100.0%

### Outfit_Student — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Student` (`all.Outfit_Student`, rolls 1).
- Loot: Calculator 21.2%, Notebook 21.2%, Pencil 21.2%, CD 8.5%, Comic Book 8.5%, Handheld Game Console 8.5%, CD Player 4.2%, Earbuds 4.2%, Headphones 2.1%, Firecrackers 0.2%, RPG Manual 0.2%

### Outfit_Survivalist — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist` (`all.Outfit_Survivalist`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist02 — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist02` (`all.Outfit_Survivalist02`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist02_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist02_Late` (`all.Outfit_Survivalist02_Late`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Late.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist02_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist02_Mid` (`all.Outfit_Survivalist02_Mid`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Mid.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist03 — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist03` (`all.Outfit_Survivalist03`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist03_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist03_Late` (`all.Outfit_Survivalist03_Late`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Late.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist03_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist03_Mid` (`all.Outfit_Survivalist03_Mid`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Mid.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist04 — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist04` (`all.Outfit_Survivalist04`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist04_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist04_Late` (`all.Outfit_Survivalist04_Late`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Late.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist04_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist04_Mid` (`all.Outfit_Survivalist04_Mid`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Mid.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist05 — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist05` (`all.Outfit_Survivalist05`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist05_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist05_Late` (`all.Outfit_Survivalist05_Late`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Late.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist05_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist05_Mid` (`all.Outfit_Survivalist05_Mid`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Mid.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist_Late — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist_Late` (`all.Outfit_Survivalist_Late`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Late.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Survivalist_Mid — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Survivalist_Mid` (`all.Outfit_Survivalist_Mid`, rolls 1); worn bags filled from BagsAndContainers.SurvivorBag_Mid.
- Loot: Photograph 22.2%, Butterfly Knife 7.4%, Chewing Tobacco 7.4%, Compass 7.4%, Dehydrated Meat Stick 7.4%, Diary 7.4%, Handiknife 7.4%, Journal 7.4%, Plastic Spork 7.4%, Water Bottle 7.4%, Beef Jerky 5.9%, Magazine: Wilderness Survival 1.5%

### Outfit_Teacher — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Teacher` (`all.Outfit_Teacher`, rolls 1).
- Loot: Calculator 21.2%, Notebook 21.2%, Pencil 21.2%, Pen - Blue 17.0%, Pen - Red 17.0%, Cigarette Pack 2.1%, Bottle of Whiskey 0.2%

### Outfit_ThunderGas — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `ThunderGas` (`all.Outfit_ThunderGas`, rolls 1).
- Loot: Rag 47.6%, Rag (Dirty) 47.6%, Cigarette Pack 4.8%

### Outfit_Tourist — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Tourist` (`all.Outfit_Tourist`, rolls 1); worn bags filled from BagsAndContainers.Tourist.
- Loot: Disposable Camera 27.0%, Money 27.0%, Brochure 13.5%, Camera 13.5%, Passport 13.5%, High-end Camera 5.4%, Cocktail Umbrella 0.1%, Handiknife 0.1%

### Outfit_Trader — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Trader` (`all.Outfit_Trader`, rolls 1).
- Loot: Money 46.1%, Stock Certificate 17.3%, Credit Card 13.4%, Pager 9.6%, Business Card 1.9%, Index Card 1.9%, Pencil 1.9%, Eraser 1.5%, Pen - Black 1.5%, Pen - Blue 1.5%, Pen - Red 1.5%, Cigar 0.4%

### Outfit_Trucker — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Trucker` (`all.Outfit_Trucker`, rolls 1).
- Loot: Caffeine Pills 44.4%, Chewing Tobacco 22.2%, Lighter - Disposable 8.9%, Magazine: HottieZ 8.9%, Cigarette Pack 2.2%, Cigarillo 2.2%, Flask 2.2%, Harmonica 2.2%, Key Ring - American Eagle 2.2%, Pouch of Tobacco 2.2%, Rolling Papers 2.2%, M1911 Pistol 0.03%

### Outfit_Varsity — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Varsity` (`all.Outfit_Varsity`, rolls 1).
- Loot: CD 28.0%, Comic Book 28.0%, CD Player 14.0%, Earbuds 14.0%, Cigarette Pack 7.0%, Headphones 7.0%, Bottle of Whiskey 0.7%, Magazine: HottieZ 0.7%, Rubber Spider 0.7%

### Outfit_Waiter_Classy — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_Classy` (`all.Outfit_Waiter_Classy`, rolls 1).
- Loot: Money 85.1%, Dish Towel 3.5%, Notepad 3.5%, Receipt 3.5%, Pen - Blue 2.8%, Corkscrew 1.4%, Pocket Knife 0.04%

### Outfit_Waiter_Diner — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_Diner` (`all.Outfit_Waiter_Diner`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Waiter_Market — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_Market` (`all.Outfit_Waiter_Market`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Waiter_PileOCrepe — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_PileOCrepe` (`all.Outfit_Waiter_PileOCrepe`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Waiter_PizzaWhirled — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_PizzaWhirled` (`all.Outfit_Waiter_PizzaWhirled`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Waiter_Restaurant — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_Restaurant` (`all.Outfit_Waiter_Restaurant`, rolls 1).
- Loot: Money 42.3%, Dish Towel 14.1%, Notepad 14.1%, Pencil 14.1%, Receipt 14.1%, Cigarette Pack 1.4%

### Outfit_Waiter_Spiffo — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_Spiffo` (`all.Outfit_Waiter_Spiffo`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Waiter_TacoDelPancho — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Waiter_TacoDelPancho` (`all.Outfit_Waiter_TacoDelPancho`, rolls 1).
- Loot: Dish Towel 24.4%, Notepad 24.4%, Pencil 24.4%, Receipt 24.4%, Cigarette Pack 2.4%

### Outfit_Woodcut — Corpse
- body · capacity 12 · icon - · CLONE-Z: Corpse
- Zombie/corpse wearing outfit `Woodcut` (`all.Outfit_Woodcut`, rolls 1); skips the default inventorymale/female table.
- Loot: Nails 52.4%, ID Card 23.8%, Key Ring - Sexy 23.8%

**— floor —**

### floor — Ground
- floor · capacity 50 · icon Container_Floor · CLONE-Z: Sack
- Engine: registered `ContainerType`.
- One `floor` container per player (`ISInventoryPage.GetFloorContainer`); a square holds at most 50 weight on the ground (`ISDropWorldItemAction.isValid`, `ISInventoryTransferAction:floorHasRoomFor`); no Organized/Disorganized modifier; never rolled — it only shows dropped items.
- Capacity: ItemContainer default; ground limit 50 weight per square.
- No Distributions.lua entry — never rolls loot.

**— other —**

### all — ?
- other · capacity - · icon - · CLONE-Z: -
- Inside a room, an `all` entry is rolled for every container of that room in addition to the container's own entry (`fillContainerTypeInternal`); only `sodatruck` and `pawnshopcooking` use it. (The top-level `all` table is the generic fallback room, counted in every `rooms` column.)
- Capacity: not a physical container (distribution key).
- Rooms (2 defs): sodatruck (6), pawnshopcooking (1)
- Procedural lists (2): CrateSodaBottles (1), CrateSodaCans (1); plus 1 direct table(s)
- Top loot: Orange Soda 52.4%, Cola 14.3%, Diet Cola 14.3%, Ginger Ale 14.3%, Bottle 4.8%

### brazier — Brazier
- other · capacity 10 · icon Container_Campfire · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoFireplace` sprites; temperature from the fire.
- Sprites: 3 sprites, 3 used on the map in 6 of 4,065 cells; capacity by map use: 10 (8) — e.g. Simple Brazier; IsoType IsoFireplace×3
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Tin Can 28.6%, Twigs 28.6%, Tree Branch 17.1%, Log 11.4%, Smashed Bottle 11.4%, Broken Glass 2.9%

### campfire — Campfire
- other · capacity 10 · icon Container_Campfire · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- 6 of 9 sprites are `IsoFireplace` cooking pits; Lua campfires (`SCampfireGlobalObject:addContainer`) create a `campfire` container with capacity 10 unless the sprite sets `ContainerCapacity`.
- Sprites: 9 sprites, 9 used on the map in 73 of 4,065 cells; capacity by map use: 10 (68) · 15 (16) — e.g. Campfire, Simple Cooking Pit, Cooking Pit; IsoType IsoFireplace×6
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Tin Can 28.6%, Twigs 28.6%, Tree Branch 17.1%, Log 11.4%, Smashed Bottle 11.4%, Broken Glass 2.9%

### composter — Composter
- other · capacity 30 · icon Container_Composter · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoCompost`: capacity = `ContainerCapacity`, default 30; created already explored, so it never rolls loot.
- Sprites: 4 sprites, 4 used on the map in 55 of 4,065 cells; capacity by map use: 30 (55) · 20 (2); 1 sprite(s) without `ContainerCapacity` → default 30 — e.g. Composter; IsoType IsoCompost×4
- No Distributions.lua entry — never rolls loot.

### corn — Corn
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- No Distributions.lua entry — never rolls loot.

### doghouse — Doghouse
- other · capacity 20 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 4 sprites, 4 used on the map in 213 of 4,065 cells; capacity by map use: 20 (342) — e.g. Dog House
- Rooms (3 defs): `all`, kennels (81), gardenstore (12)
- Direct tables only (3).
- Top loot: Water Dish 33.3%, Animal Bone 13.3%, Dog Chew Toy 13.3%, Pillow 13.3%, Dog Tag 6.7%, Large Animal Bone 6.7%, Leash 6.7%, Rubber Duck 6.7%

### fruitbusha — Plant
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- Sprites: 1 sprite, 1 used on the map in 1,529 of 4,065 cells — e.g. Bush
- No Distributions.lua entry — never rolls loot.

### fruitbushb — Plant
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- Sprites: 1 sprite, 1 used on the map in 1,526 of 4,065 cells — e.g. Bush
- No Distributions.lua entry — never rolls loot.

### fruitbushc — Plant
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- Sprites: 1 sprite, 1 used on the map in 1,532 of 4,065 cells — e.g. Bush
- No Distributions.lua entry — never rolls loot.

### fruitbushd — Plant
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- Sprites: 2 sprites, 2 used on the map in 1,547 of 4,065 cells — e.g. Bush
- No Distributions.lua entry — never rolls loot.

### fruitbushe — Plant
- other · capacity - · icon Container_Plant · CLONE-Z: -
- Engine: registered `ContainerType`.
- `IsoObject.createContainersFromSpriteProperties` explicitly skips this type — no item container is created from the sprite.
- Sprites: 2 sprites, 2 used on the map in 1,560 of 4,065 cells — e.g. Bush
- No Distributions.lua entry — never rolls loot.

### other — ?
- other · capacity - · icon - · CLONE-Z: -
- Room-level fallback: a room's `other` entry fills any container type the room doesn't list, except no-generic types (`ItemPickerJava.fillContainerTypeInternal`). The generic `all` table has one too.
- Capacity: not a physical container (distribution key).
- Rooms (10 defs): `all`, empty (5,336), derelict (1,038), storageunit (695), motelroom (652), furniturestore (40), studio (34); +3 more
- Procedural lists (6): DerelictHouseJunk (2), OtherGeneric (1), DerelictHouseCrime (2), DerelictHouseDrugs (2), DerelictHouseParty (2), DerelictHouseSquatter (2); plus 7 direct table(s)
- Top loot: Tin Can 6.0%, Receipt 4.5%, Beer Can 3.1%, Paperback 3.0%, Cola 2.3%, Diet Cola 2.3%, Ginger Ale 2.3%, Cockroach 2.3%, Straw 2.2%, Garbage Bag 2.0%, Old Newspaper 1.7%, Notepad 1.7%

### scarecrow — ?
- other · capacity ? · icon Item_Scarecrow · CLONE-Z: -
- Key exists only in ContainerButtonIcons; B42 scarecrows are `IsoMannequin` objects whose container type is `mannequin`.
- Capacity: no sprite defines this container type.
- No Distributions.lua entry — never rolls loot.

### shelter — Shelter
- other · capacity 20 · icon Item_Tent2_Open · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 32 sprites, 32 used on the map in 9 of 4,065 cells; capacity by map use: 20 (55) — e.g. Hide Shelter, Tarp Tent, Lean-To Shelter, Tarp Shelter
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: Rag (Dirty) 7.4%, Pop Can 7.4%, Beer Bottle 3.7%, Beer Can 3.7%, Bottle of Whiskey (empty) 3.7%, Copper Scrap 3.7%, Garbage Bag 3.7%, Old Newspaper 3.7%, Orange Soda (empty) 3.7%, Rubber Band 3.7%, Scrap Metal 3.7%, Spoon 3.7%

### tent — Tent
- other · capacity 50 · icon Item_Tent2_Open · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); can hold a human corpse (`canHumanCorpseFit`); registered `ContainerType`.
- Sprites: 20 sprites, 20 used on the map in 37 of 4,065 cells; capacity by map use: 50 (114) · 30 (20) — e.g. Brown Tent, Green Tent, Blue Tent, Yellow Tent
- Rooms (3 defs): `all`, armysurplus (8), camping (6)
- Direct tables only (3).
- Top loot: Paperback 20.5%, Compass 10.3%, Magazine 10.3%, Book 10.3%, Cheap Sleeping Bag 7.7%, Sleeping Bag 5.8%, Insect Repellent 5.1%, Matchbox 5.1%, Fanny Pack 2.6%, Firestarting Block 2.6%, Magnesium Firestarter 2.6%, Western Canteen 2.6%

### trough — Trough
- other · capacity 50 · icon - · CLONE-Z: -
- Engine: no-generic type (never uses a room's `other` table); registered `ContainerType`.
- `IsoFeedingTrough`: accepts only Food or animal-feed drainables; the item container exists only while the trough holds no fluid (`checkContainer`). `all.trough` is an empty table.
- Sprites: 14 sprites, 14 used on the map in 226 of 4,065 cells; capacity by map use: 50 (334) · 70 (218) · 100 (181) · 35 (69); 2 sprite(s) without `ContainerCapacity` → default 50 — e.g. Large Metal Trough, Animal Trough, Small Metal Trough, Huge Metal Trough
- Rooms (1 def): `all`
- Direct tables only (1).
- Top loot: none — the table(s) are empty

## Data gaps

- **Names (`?`, 24 rows):** `safe`, `schooldesk`, `BombBox`, `BoozeBox`, `FoodBox`, `GunBox`, `MedicalBox`, `ShotgunBox`, `ToolsBox`, `wheeliebin`, `AmmoStrap_Bullets_38`, `AmmoStrap_Bullets_44`, `AmmoStrap_Bullets_45`, `AmmoStrap_Bullets_9mm`, `Bag_ConstructionBag`, `Bag_DoctorBag_Money`, `Bag_FruitBasket`, `Bag_HospitalBasket`, `Bag_SewingBasket`, `Bag_Skill_Maintenance_DuffelBag`, `FirstAidKit_Pro`, `all`, `other`, `scarecrow` — no `IGUI_ContainerTitle_*` / `IGUI_VehiclePart*` / `ItemName.json` entry exists for these keys in the EN translation files (when a stash uses a bag item the loot window shows that bag's name; the distribution-only bag keys have no item script at all).
- **Capacity (`?`, 23 rows):** `cupboard`, `officedrawers`, `safe`, `schooldesk`, `coffeemaker`, `icecream`, `toaster`, `vendingGt`, `garagestorage`, `scarecrow` have no B42 sprite or object that creates them (registered/titled or distribution-only keys); `AmmoStrap_Bullets_38`, `AmmoStrap_Bullets_44`, `AmmoStrap_Bullets_45`, `AmmoStrap_Bullets_9mm`, `Bag_ConstructionBag`, `Bag_DoctorBag_Money`, `Bag_FruitBasket`, `Bag_HospitalBasket`, `Bag_SewingBasket`, `Bag_Skill_Maintenance_DuffelBag`, `FirstAidKit_Pro` are distribution entries for items that have no script (dead tables); `BombBox`, `BoozeBox` are cache types that no StashDescriptions entry places. Anything created as a plain `ItemContainer` would get the default 50.
- **World frequency:** container placements live in the `.lotpack` map data (3.8 GB), which was not parsed; room counts come from the lotheaders and say nothing about how many containers of a type a room holds — hence the unweighted shares.
- **Sandbox-dependent values** (loot multipliers per category, rolls multiplier, zombie-density loot bonus, loot respawn) are left at their defaults/neutral.
