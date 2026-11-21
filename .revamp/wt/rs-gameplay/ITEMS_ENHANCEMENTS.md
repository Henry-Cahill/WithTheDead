# CLONE-Z — Item System Enhancements

A verbose, Project Zomboid–grounded catalogue of candidate enhancements to the item
system. Each entry lists the gameplay purpose, PZ grounding, the concrete data model,
the files/functions to wire, dependencies, save impact, and a rough size.

## Current baseline

- `Assets/Scripts/Items/ItemDatabase.cs` — `ItemDef` (food/drink/medical/weapon/firearm/ammo/clothing/water/seed fields), `ItemStack` (`Count` + `SpawnHours` freshness), `ItemDatabase` catalogue + loot tables.
- `Assets/Scripts/Items/Inventory.cs` — weight-capped stack list (`Add`/`Remove`/`Transfer`; merges keep the older `SpawnHours`).
- `Assets/Scripts/Items/LootContainer.cs` — world containers + corpses + dropped sacks.
- `Assets/Scripts/Player/PlayerInventory.cs` — `Use()` (eat/drink/heal/treat/equip/wear), `Equip`/`Drop`/`TakeOff`/`TryCook`/`TryPurify`; capacity = `PlayerCarryCapacity * Skills.CarryCapacityMult`.
- `Assets/Scripts/UI/InventoryUIController.cs` (+ `.List.cs`, `.Picker.cs`, `.Grid*.cs`) — two views over the same window API: an Inventory-Tetris-style **grid** (default — the survivor's pockets plus a section per worn bag, the open container or the ground, drag-and-drop with rotation, Transfer All / Transfer only… / Tidy up) and the PZ-style three-pane **list** (verbose details lists for the bag and for the open container / floor, collapsible stacks, a CONTAINERS column, a right-click context menu, double-click to move, tooltips). The toggle is remembered in PlayerPrefs.
- `Assets/Scripts/Core/CloneZConfig.cs` — tuning constants. Save via `Assets/Scripts/Core/SaveData.cs` + `SaveSystem.cs` (`StackSave` already persists `SpawnHours`).

## Key architectural insight

The only per-instance item state today is `ItemStack.SpawnHours` (freshness). Any
**condition/durability** enhancement reuses that exact pattern: add a field on
`ItemStack`, persist it in `StackSave`, and force damageable items to `MaxStack = 1`
(unique condition can't share a stack). **Condition (A) is the backbone** that repair
(B), clothing wear (C) and can-openers (I) build on.

---

## A. Item condition & durability  *(backbone — medium)*

[+] **Gameplay:** weapons, tools and guns wear out with use and eventually break, so loot stays a renewable need and maintenance matters.
- **PZ grounding:** `InventoryItem.getCondition()`/`getConditionMax()` (0..max; starts full, breaks at 0). Each qualifying use rolls `Rand.NextBool(getConditionLowerChance())`, so the script's `ConditionLowerChanceOneIn` is a **1-in-N toughness** (higher N = tougher, degrades less often). Real B42 values span a wide range: crowbar 70, wood axe 40, hammer/axe 30–35, machete 25, baseball bat 20, hunting knife 15, fighting knife 10, kitchen/steak knife 2 (very fragile) — so "one number per category" is a simplification. Firearms wear a separate `headCondition` and jam via `HandWeapon.checkJam` (jam odds climb as condition falls, worsened by low Aiming/Strength).
- **Data model:** `ItemDef.MaxCondition` (int), `ItemDef.ConditionLossChance` (1-in-N), and a new **per-instance** `ItemStack.Condition` initialised in the `ItemStack` constructor next to `SpawnHours`.
- **Wiring:** `PlayerCombat` melee swing + firearm shot roll `ConditionLossChance` → decrement condition; at 0, remove the stack and revert to bare hands. Tool jobs in `PlayerBuilder` (hammer) and `PlayerFarming` (trowel/watering can) degrade on completion. Low firearm condition adds a jam roll in the fire path.
- **UI:** a condition bar/percentage on each row in `InventoryUIController`; a "Condition" line in `ItemDef.Tooltip()`.
- **Save:** `StackSave` gains a `Condition` field (mirrors `SpawnHours`); `PlayerInventory.RestoreState` + equipped-weapon restore carry it.
- **Constraint:** damageable items must be `MaxStack = 1` (weapons/tools already are).

## B. Item repair  *(small — depends on A)*

- **Gameplay:** patch a worn weapon/tool back up instead of discarding it, with skill gating quality.
- **PZ grounding:** duct tape / glue / scrap repairs restore a *fraction* toward max with a **diminishing cap** each time (a heavily-repaired item never returns to pristine).
- **Data model:** `CloneZConfig` repair constants (restore fraction, cap decay per repair); optional `ItemDef.RepairMaterialId`.
- **Wiring:** `PlayerInventory.TryRepair(stack)` consumes a material (`ducttape` already exists) and raises `ItemStack.Condition`; a **Repair** button in `InventoryUIController` when the held item is damaged and a material is on hand.

## C. Clothing condition & holes  *(medium — depends on A)*

- **Gameplay:** armour wears where it's hit; ragged clothes insulate and protect less until repaired.
- **PZ grounding:** clothing `Condition` drops when a garment absorbs a bite/scratch; holes reduce bite/scratch defense; needle + thread patches restore it.
- **Data model:** reuse `ItemStack.Condition` for worn pieces; scale `PlayerClothing.TotalInsulation` and `BlocksBite`/`BlocksScratch` by condition.
- **Wiring:** the existing bite/scratch block in `PlayerClothing` decrements the garment's condition; a needle+thread repair path; `HealthPanelController` clothing lines show condition.

## D. Bags & backpacks — carry containers  *(medium-large)*

- **Gameplay:** finding a backpack is a major upgrade — more carry weight and a big survival multiplier.
- **PZ grounding:** bags add capacity and apply a `WeightReduction` to their contents (a good pack roughly halves felt weight).
- **Data model:** `ItemDef.IsContainer`, `ContainerCapacity` (extra kg), `WeightReduction` (%); a new **Back** slot in the `ClothingSlot` enum.
- **Wiring:** equipping a bag raises `Inventory.MaxWeight` in `PlayerInventory` (stacks with the Strength-based `CarryCapacityMult` in `Update`); items `schoolbag`/`duffelbag`/`militarypack` in `ItemDatabase`; a Back-slot button in `HealthPanelController`; save the worn bag.
- **Status:** shipped as worn containers on new `Back` / `Rig` / `Thigh` slots (`schoolbag`, `hikingbag`, `militarypack`, `chestrig`, `thighbag`, `fannypack`; `ItemDef.Gear = Container`, `ContainerCapacity`, `GridW/GridH`). Each worn bag adds its own grid section with a weight cap (`InventoryLayout` / `GridSection`); taking one off spills its contents into the remaining space and drops the overflow. No weight reduction yet.

## E. Literature — skill books & recipe magazines  *(medium)*

- **Gameplay:** reading accelerates a skill or unlocks recipes — a reason to hoard books and spend safe time indoors.
- **PZ grounding:** `SkillBook` grants an XP multiplier for a skill within a level band; recipe magazines permanently unlock craftables.
- **Data model:** new `ItemCategory.Literature`; `ItemDef.SkillBook` (a `PlayerSkills.Skill`), `BookLevelMin/Max`, `BookXpMultiplier`.
- **Wiring:** a **Read** timed action (mirrors the sleep timer) sets a temporary XP multiplier in `PlayerSkills`; magazines flip a recipe-unlocked flag used by the slice-6 crafting path; a **Read** button in the inventory UI; save active multiplier timers.

## F. Light sources & battery power  *(medium — ties to day/night)*

- **Gameplay:** night becomes navigable with a flashlight, but batteries drain and light is a visibility tradeoff.
- **PZ grounding:** flashlight/torch cast light and consume battery/fuel; lighters and candles burn down.
- **Data model:** `ItemDef.IsLightSource`, `LightRange`, `LightDrainPerSec`, `NeedsBattery`.
- **Wiring:** a new `PlayerLight` component toggles a URP light, drains the existing `battery` item (finally giving it a use), and reads `TimeOfDay` darkness; a HUD on/off + charge indicator.
- **Status:** shipped in `PlayerGear` (+ `FlashlightFX`): a carried `flashlight` (L), night-vision goggles worn on the `Eyes` slot (N) and a gas mask on `Mask` whose filter burns down in `ToxicZone` gas. Device charge lives in `ItemStack.Condition` (`ItemDef.UsesCharge`, `ChargeSeconds`); a `battery` or `gasmaskfilter` used from the pack tops it up. The HUD shows the battery and the "Filter %" readout.

## G. Throwables  *(medium)*

- **Gameplay:** molotovs clear a horde with fire; noisemakers lure zombies away — new tactical options.
- **PZ grounding:** molotov / aerosol bomb / pipe bomb with fire, explosive or noise effects.
- **Data model:** `ItemDef.IsThrowable`, `ThrowDamage`, `ThrowRadius`, `ThrowKind` (Fire/Explosive/Noise).
- **Wiring:** a `PlayerThrow` component (aim + arc), an AoE effect reusing the `CombatFX` self-destruct pattern, and a `NoiseSystem` pulse for lures.

## H. Inventory UX — split / hotbar / favourites / context menu  *(medium — pure QoL)*

- **Gameplay:** faster, less fiddly item handling under pressure.
- **Features:** split a stack (shift-click), a **1–5 hotbar** for quick-equip/use, favourite-lock so Drop and Take-All skip an item, and a right-click context menu consolidating row actions.
- **Wiring:** mostly `InventoryUIController`; a small split helper in `Inventory`; a new hotbar strip in `HUDController` bound through `GameBootstrapper`; save hotbar assignments + favourite flags.
- **Status:** the right-click context menu (with Take 1 / Store 1 / Drop 1, double-click to move and whole-stack moves) shipped with the verbose inventory; split, hotbar and favourites remain.

## I. Canned food needs a can opener  *(small — depends on A)*

- **Gameplay:** tinned food (your most reliable calories) needs the right tool, making a can opener a prized find.
- **PZ grounding:** canned goods require a can opener (fast) or a sharp knife (slow, small condition hit); opening yields an empty tin.
- **Data model:** `ItemDef.IsCanned`; new `canopener` and `tincan` items.
- **Wiring:** gate the `Use` path in `PlayerInventory` for `cannedbeans`/`cannedsoup` on a can opener or knife (knife path degrades via A); spawn a `tincan` crafting material on open.

## J. Fuel & campfire — cook without a stove  *(large — ties slices 6/9/10)*

- **Gameplay:** cook, boil water and stay warm in the field, freeing you from the spawn stove.
- **PZ grounding:** a campfire built from fuel (planks/twigs), lit with matches/lighter, enabling cooking and radiating heat.
- **Data model:** `ItemDef.IsFuel`, `BurnTimeHours`.
- **Wiring:** a new `World/Campfire.cs` following the `CookingStation` pattern, a build recipe in `PlayerBuilder`, and a warmth radius read by `PlayerTemperature`.

## K. Equipped-weapon weight reduction & two-handed  *(small)*

- **Gameplay:** the weapon in your hands weighs less on your carry total; heavy arms occupy both hands.
- **PZ grounding:** equipped weapons contribute reduced weight; two-handed weapons block off-hand items.
- **Wiring:** subtract an equipped-weapon discount in the `PlayerInventory.Weight`/`Load` calculation; a `ItemDef.TwoHanded` flag in `ItemDatabase`.

## L. Item variety & loot-table tuning  *(small — content only)*

- **Gameplay:** a richer, more varied loot pool with meaningful rarity.
- **Data model:** an `ItemDef.Rarity` tier used to weight spawns; more melee (baseball bat, crowbar, machete), food and medical entries.
- **Wiring:** entirely within `ItemDatabase.BuildItems` + the loot tables — low risk, no new systems.

---

## Cohesive bundles for a "15th slice"

1. **Durability & Repair** = A + B + C + I + K — the condition backbone; deepest PZ authenticity.
2. **Loadout & Knowledge** = D + E + H — bags, skill books, inventory QoL; progression-focused.
3. **Tools of Survival** = F + G + J — light, throwables, campfire; action/utility.

## Excluded as too heavy for one slice

- Full PZ nutrition (calories / carbs / proteins / lipids).
- Nested bag sub-inventories.
- Weapon attachment sockets (scopes, lights, slings).
