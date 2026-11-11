# Survival — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** needs (hunger, thirst, sleep, temperature, wetness), wounds, limbs, blood and disease, skills and books,
items and inventory, loot, crafting, cooking, farming, foraging, water and power, and the survival side of Wave
Survival.
**Code:** [Assets/Scripts/Player](../Assets/Scripts/Player), [Assets/Scripts/Medical](../Assets/Scripts/Medical),
[Assets/Scripts/Survival](../Assets/Scripts/Survival), [Assets/Scripts/Items](../Assets/Scripts/Items),
[FarmPlot.cs](../Assets/Scripts/World/FarmPlot.cs), [CookingStation.cs](../Assets/Scripts/World/CookingStation.cs),
[WaterMains.cs](../Assets/Scripts/Core/WaterMains.cs), [PowerGrid.cs](../Assets/Scripts/Core/PowerGrid.cs),
[SandboxSettings.cs](../Assets/Scripts/Core/SandboxSettings.cs), the `CloneZConfig*.cs` tuning files.

## Where it stands

The survival layer is broad and mostly real. Needs, wounds and limbs, blood and shock, diseases, use-based skills and
skill books, farming, cooking at stoves and campfires, utility shutoff, a grid inventory with worn bags, and item
condition with wear and repair are all in. Raid characters keep their diseases in the profile
([Diseases.cs](../Assets/Scripts/Survival/Diseases.cs#L40)).

[ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) is out of date. Its status, checked against the code:

| | Enhancement | Status | Evidence |
|---|---|---|---|
| A | Condition & durability | Done | A wear roll per swing or shot ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L499) → `Wear`, [PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L553)) |
| B | Repair | Done | `TryRepair` with a cap that shrinks per repair ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L616)) |
| C | Clothing condition & holes | Partial | Garments lose condition when they stop a hit ([PlayerClothing.cs](../Assets/Scripts/Player/PlayerClothing.cs#L149)); no holes, no needle-and-thread patching |
| D | Bags | Done, minus weight reduction | [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md#L56) |
| E | Literature | Partial | Skill books are read (`SkillBooks.Read`, [FieldSurvival.cs](../Assets/Scripts/Survival/FieldSurvival.cs#L623)); no recipe magazines |
| F | Light & battery | Done | `PlayerGear` (flashlight, night vision, gas-mask filter) |
| G | Throwables | Done | Grenades, pipe bombs, molotovs (a spreading fire) and noise lures (firecrackers, an alarm clock, a timer radio) on one arc ([Throwables.cs](../Assets/Scripts/Weapons/Throwables.cs)); breach charges ([Camps.cs](../Assets/Scripts/World/Camps.cs#L625)). See [Weapons.md](Weapons.md) |
| H | Inventory UX | Partial | Context menu and Split done; hotbar and favourites missing (see [UI.md](UI.md)) |
| I | Can opener | Done | `IsCanned`, `TryOpenCan`, an empty tin ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L279)); a starter can opener ([GameBootstrapper.cs](../Assets/Scripts/Bootstrap/GameBootstrapper.cs#L888)) |
| J | Campfire | Done | Burns planks, rags and paper, warms, and is a cooking station ([FieldSurvival.cs](../Assets/Scripts/Survival/FieldSurvival.cs#L367)) |
| K | Equipped weight & two-handed | Done | Equipped-weapon weight cut ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L231)); `TwoHanded` drives the swing and grid footprint ([InventoryLayout.cs](../Assets/Scripts/Items/InventoryLayout.cs#L92)) |
| L | Variety & rarity | Partial | ~3,600 items, but no rarity tier to weight spawns |

## P1 — next up

- [ ] **Make cooking take time** · P1 · S · Gap
  - **Why:** `TryCook` swaps a raw item for its cooked form at once at any lit stove in range
    ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L366)). There's no wait, no risk and no reason to
    guard the stove, unlike everything else in a game where every action is timed.
  - **Do:** Make it a timed action like the other field jobs, a few seconds per serving and longer for big meals, with a
    progress bar. Add burnt food if the survivor walks away and smell or noise from frying. When cooking has duration,
    save pots in progress (today a stove saves only its position,
    [SaveSystem.cs](../Assets/Scripts/Core/SaveSystem.cs#L223)).
  - **Done when:** Cooking shows progress, can be interrupted, and burns food that's left too long.

- [ ] **Give worn bags their weight reduction** · P1 · S · Balance
  - **Why:** Bags add capacity and their own grid, but the felt weight of what's inside is unchanged
    ([ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md#L56)). Only the equipped weapon gets a cut
    ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L231)). In Project Zomboid a good pack is the
    biggest upgrade you can find; here it's only a bigger box.
  - **Do:** Add `WeightReduction` (%) to bag item defs (a school bag a little, a military pack a lot), apply it to the
    contents' weight in the load calculation, and show it in the bag tooltip.
  - **Done when:** The same load weighs less in a hiking bag than in the pockets, and the tooltip says by how much.

- [ ] **Save everything a Wave Survival run needs** · P1 · M · Save
  - **Why:** Wave Survival saves and continues (F5, **Continue**), but its player record
    ([SaveData.cs](../Assets/Scripts/Core/SaveData.cs#L76)) has health, needs, infection and sickness, and no list of
    active diseases. Raid characters keep theirs in the profile, which a wave run doesn't use. Field jobs in progress
    (fishing, traps, foraging timers in [FieldSurvival.cs](../Assets/Scripts/Survival/FieldSurvival.cs)) aren't in the
    save either.
  - **Do:** Add the disease list (and their timers, if they have stages) and any running field jobs to the wave save.
    Then extend the wave smoke test's save-and-continue check to compare the survivor's whole state before and after.
  - **Done when:** Save, quit and **Continue** brings back the same diseases, needs and running jobs.

## P2 — soon

- [ ] **Re-seed the editor's content database when the catalogue changes** · P2 · S · Dev workflow
  - **Why:** The editor's `clonez_content.db` is rebuilt only when `SchemaVersion` changes
    ([ContentDatabase.cs](../Assets/Scripts/Items/ContentDatabase.cs#L224)), while release builds re-seed whenever their
    build changes ([L278](../Assets/Scripts/Items/ContentDatabase.cs#L278)). That's why the README tells developers to
    delete the file by hand after pulling item changes, and it's easy to forget and test stale items.
  - **Do:** Store a hash of the seed (curated SQL, bulk SQL and crops) in `content_meta`. If it differs, re-seed. If the
    database was edited by hand in a SQLite browser (keep that workflow), log a warning instead and add a
    **CloneZ ▸ Content ▸ Re-seed** menu item.
  - **Done when:** Pulling an item change and pressing Play shows the change with no manual delete.

- [ ] **Clothing holes and patching** · P2 · M · Gap
  - **Why:** [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) C. Garments lose condition when they stop a bite or
    scratch ([PlayerClothing.cs](../Assets/Scripts/Player/PlayerClothing.cs#L149)), but there are no holes and no
    sewing. Ripping clothes into rags already gives thread ([FieldSurvival.cs](../Assets/Scripts/Survival/FieldSurvival.cs#L277)),
    so half the loop exists.
  - **Do:** Give each garment hole slots by body part, where a hole drops protection and insulation for that part. Add
    patching with needle, thread and rag (Tailoring could be a new skill or part of an existing one). Show holes in the
    health panel's clothing lines.
  - **Done when:** A jacket that took bites shows holes, insulates less, and can be patched.

- [ ] **Water and power beyond on and off** · P2 · M · Gap
  - **Why:** Mains water and the grid are a switch with a shutoff day
    ([WaterMains.cs](../Assets/Scripts/Core/WaterMains.cs), [PowerGrid.cs](../Assets/Scripts/Core/PowerGrid.cs)). After
    the shutoff, Project Zomboid's mid-game is about rain barrels, generators and fuel, and that game is missing here.
  - **Do:** Add rain collectors (build menu), generators that burn fuel and power a building within a radius (noise
    draws the dead), and the water left in a building's pipes and tanks after the shutoff.
  - **Done when:** After the shutoff a base can have water and power again through the survivor's own effort.

- [ ] **A rarity tier for loot** · P2 · S · Content
  - **Why:** [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) L. With ~3,600 items spread across loot tables by
    category ([ContentDatabase.cs](../Assets/Scripts/Items/ContentDatabase.cs), `DistributeCatalogueLoot`), a rare find
    and a common one are weighted the same within their category.
  - **Do:** Add a rarity column (common, uncommon, rare, very rare) to the item table, set it for weapons, medicine,
    bags and tools first, and weight the spread by it.
  - **Done when:** Rare items show up noticeably less often in a loot sample of a thousand containers.

## P3 — nice to have

- [ ] **Save skill XP by name, not by position** · P3 · S · Tech debt — `SkillXp` is a `float[]` in
  `PlayerSkills.Skill` order ([SaveData.cs](../Assets/Scripts/Core/SaveData.cs#L87)). Adding or reordering a skill
  shifts everyone's XP into the wrong skill. Save id-and-XP pairs, reading old arrays once. **Done when:** a test save
  survives a new skill being added in the middle.
- [ ] **Harvests ignore carry weight** · P3 · S · Balance — a harvest goes into the pack "without a weight check"
  ([PlayerFarming.cs](../Assets/Scripts/Player/PlayerFarming.cs#L366)). Fine for a cabbage, odd for a field of potatoes.
  Drop what doesn't fit beside the plot. **Done when:** a harvest never overloads the survivor silently.
- [ ] **Recipe magazines** · P3 · M · Content — the other half of [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) E.
  Magazines that permanently unlock field-craft or hideout recipes, kept by the character (or the account, as
  knowledge). **Done when:** a magazine unlocks a recipe that wasn't craftable before.
- [ ] **Farming depth** · P3 · M · Gap — crops have water, growth and one health value
  ([FarmPlot.cs](../Assets/Scripts/World/FarmPlot.cs)). Add pests, rot from overwatering, fertiliser (compost from
  spoiled food), and seasons, which matter in a winter setting with greenhouses or indoor pots. **Done when:** a careless
  farmer can lose a crop to something other than thirst.
- [ ] **Bring ITEMS_ENHANCEMENTS.md up to date** · P3 · S · Docs — copy the status table above into it, so the next
  person doesn't rebuild what exists (can openers and campfires were nearly re-planned).

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text). They're fixtures in `World/Fixtures`, but they're
about cooking, water and warmth:

- [ ] "Toggle all burners" on the kitchen stove (`KitchenStove.cs`): failed (tasks.md line 5311).
- [ ] "Refill cooler" on the store water cooler, saved across loads (`RaidStoreWaterCooler.cs`): failed (tasks.md line
  6309).
- [ ] The water heater's pilot flame as a warmth source (`RaidStoreWaterHeater.cs`, an `IWarmthSource` like the
  campfire): failed (tasks.md line 9460).
