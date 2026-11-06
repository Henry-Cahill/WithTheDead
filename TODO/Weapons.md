# Weapons — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** guns, ammunition, attachments and upgrades, melee, throwables and explosives, armour and damage, combat
feel and sound, and hits in co-op.
**Code:** [Assets/Scripts/Weapons](../Assets/Scripts/Weapons) ([Ballistics.cs](../Assets/Scripts/Weapons/Ballistics.cs),
[WeaponItems.cs](../Assets/Scripts/Weapons/WeaponItems.cs), [WeaponUpgrades.cs](../Assets/Scripts/Weapons/WeaponUpgrades.cs)),
[ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs) (the guns, ammo and attachments),
[PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs), [CombatResolver.cs](../Assets/Scripts/Core/CombatResolver.cs),
[DamageModel.cs](../Assets/Scripts/Core/DamageModel.cs), `Explosives` in [Camps.cs](../Assets/Scripts/World/Camps.cs#L630),
[NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs), [SfxSynth.Weapons.cs](../Assets/Scripts/Audio/Synth/Recipes/SfxSynth.Weapons.cs).

## Where it stands

- **Six guns:** pistol, shotgun, AK-74, Mosin, PP-9 SMG and the flare gun
  ([ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs#L80)). That file also registers 19 kinds of
  round and 13 attachments across seven slots.
- The ballistics are working: penetration against armour class, armour wear, bleeding and fractures, per-pellet
  shotgun fire ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L457)), fouling, jamming and cleaning, and
  a wear roll per shot or swing that can break the weapon ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L499)).
  There's also a technician's upgrade tree, and stripping attachments from enemies' guns.
- Melee covers knife, bat, bare hands and shove. Two-handed weapons get their own swing.
- Grenades are thrown and breach charges armed from the pack (`Explosives`, [Camps.cs](../Assets/Scripts/World/Camps.cs#L632)).
- In co-op the server resolves shots with lag compensation, and friendly fire and PvP are server rules
  ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L289), [NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs#L622)).

## P1 — next up

- [ ] **More guns, and period ones** · P1 · L · Content
  - **Why:** An extraction shooter with weapon modding and trader tiers has six guns, one of them a flare gun. Kentucky
    in 1993 is full of civilian guns, and Project Zomboid players will look for them.
  - **Do:** Add 8 to 12 guns in two groups. **Kentucky:** a .38 revolver, an M1911, a .22 rifle, a pump shotgun, a
    hunting rifle in .30-06, a lever action in .30-30 and an AR-15. **Zone and trader tier:** an SVD-style DMR, a
    PKM-style LMG, an AKS-74U. Each needs `GunStats` and an item def (with a loot-table place and a trader price), a
    model through the item pipeline, a hand pose (`WeaponHandIK`) and a sound recipe.
  - **Done when:** Each trader tier and each kind of building (gun shop, police, farmhouse) has guns of its own.

- [ ] **Attachments you can see, and lights and lasers that work** · P1 · M · Gap
  - **Why:** Attachments change the numbers (`ModStats`, [Ballistics.cs](../Assets/Scripts/Weapons/Ballistics.cs#L35)),
    but nothing builds them onto the gun in the survivor's hands: no character or item-model code mentions mod slots or
    attachment ids. A tactical light or laser sets a flag and changes spread, but casts no light and draws no dot. In
    first person and bodycam especially, the gun never changes.
  - **Do:** Give gun models anchors per slot, and build each attachment's model there when the build changes. A light
    attachment adds a spot light at the muzzle with a toggle, using battery like the flashlight; a laser draws a dot
    where the muzzle points.
  - **Done when:** Fitting a red dot, suppressor or light visibly changes the held gun, and the light lights.

- [ ] **Molotovs and noise lures** · P1 · M · Gap
  - **Why:** [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) G. Grenades and breach charges are the only things to
    throw or set ([Camps.cs](../Assets/Scripts/World/Camps.cs#L632)). Horde management, Project Zomboid's bread and
    butter, has no tools: no firecrackers or alarm clock to pull the dead away, and no molotov to burn a crowd at a
    choke point.
  - **Do:** Reuse the grenade's throw arc (`Explosives.Throw`). A **noise lure** (firecrackers, an alarm clock, a radio
    on a timer) is a timed `NoiseSystem` pulse. A **molotov** is a burning area that damages and spreads, crafted from a
    bottle, fuel and a rag. A **pipe bomb** is a crafted grenade.
  - **Done when:** A thrown alarm clock pulls a horde off a house, and a molotov burns the dead in a doorway.

## P2 — soon

- [ ] **Special ammunition that does what its card says** · P2 · M · Gap
  - **Why:** Night Shift added tracer, incendiary, "Cold-Snap", "Heavy-Velocity" and "Fuel-Gel" rounds with
    descriptions that promise fire, glowing trails and sticky residue
    ([ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs#L56)). `BulletDamage` uses damage, penetration,
    armour damage, bleed and fracture only ([CombatResolver.cs](../Assets/Scripts/Core/CombatResolver.cs#L126)), and
    every shot already draws a tracer ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L479)). The code
    comments are also odd ("fire-slug … freezing zombies in place").
  - **Do:** For each round, implement the effect (incendiary sets the hit actor burning; only tracers draw a visible
    trail) or rewrite the description to match what it does. Remove the ones that don't earn a place.
  - **Done when:** Every special round's card describes its real effect.

- [ ] **Melee with weight** · P2 · M · Gap
  - **Why:** A swing is one arc query and one damage value ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L178)).
    Knife, bat and fists differ in damage and the two-handed swing, but not in reach, arc, swing time, stagger or
    knockdown. In a zombie game melee is most of the fighting.
  - **Do:** Add reach, arc, wind-up and recovery, and stagger and knockdown chances per weapon. Allow a shove-then-stomp
    on downed dead, and make hitting multiple targets depend on the arc.
  - **Done when:** A bat, a knife and an axe feel different against three zombies in a corridor.

- [ ] **Hits you can hear and read** · P2 · M · Polish
  - **Why:** Every melee impact plays one `MeleeHit` sound ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L223)).
    Armour penetration is rolled ([Ballistics.cs](../Assets/Scripts/Weapons/Ballistics.cs)), but a stopped round sounds
    like a hit.
  - **Do:** Add separate sounds for blade, blunt and fist, and for flesh, armour plate and helmet. Mark penetrated, blunt
    and ricochet hits clearly (sound plus a small hit-marker variant where the HUD style allows it).
  - **Done when:** Without looking, a player can tell a plate stopped their round.

- [ ] **Check that explosions follow the friendly-fire rule** · P2 · S · Co-op
  - **Why:** `CombatResolver.Explosion` damages everything in its radius without the `CanHarm` test bullets and melee use
    ([CombatResolver.cs](../Assets/Scripts/Core/CombatResolver.cs#L164)). Remote players filter friendly fire when hit
    ([NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs#L622)), but it's unclear whether the thrower's own squad, the
    local survivor, companions and drones are spared.
  - **Do:** Test grenade, breach charge and drone grenade against a squadmate with friendly fire off, and add the rule
    to `Explosion` if anything takes damage.
  - **Done when:** With friendly fire off, a grenade at a squadmate's feet hurts only the dead.

- [ ] **Have the server check what the shooter holds** · P2 · M · Co-op
  - **Why:** The server clamps range and the damage multiplier, but takes the client's word for weapon, ammunition,
    origin and direction (`OnShot`, [NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs#L217)), and melee hits likewise.
    That's fine among friends, but required before public PvP (DESIGN_DECISIONS.md tension 5: "the netcode is built for
    PvP anyway").
  - **Do:** Check that the shooter has that gun equipped and that round loaded, and enforce fire rate and magazine
    count server-side.
  - **Done when:** A modified client can't fire a gun it doesn't hold.

- [ ] **Save the exact magazine** · P2 · M · Save
  - **Why:** `RestoreFirearm` brings back the loaded count and the chosen ammunition
    ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L322)). A magazine mixed from two kinds of round, or an
    ammo id that no longer exists, comes back as the default.
  - **Do:** Save the magazine's contents as a list of rounds, and fall back loudly (a log line) rather than silently.
  - **Done when:** A mixed magazine survives save and load round by round.

- [ ] **A balance sheet** · P2 · M · Balance
  - **Why:** Damage is per round and multiplied straight through `BulletDamage`, and the currency is itself a round
    (`ZoneContent.Currency`). Nobody has written down time-to-kill per gun and round against the dead's limbs and each
    armour class, or what a kill costs in money.
  - **Do:** Write an editor or test script that prints time-to-kill and cost-per-kill for every gun and round
    combination against a shambler, a sprinter, and armour classes 2 to 5. Tune from that.
  - **Done when:** The sheet exists, and each gun has a role it's best at.

## P3 — nice to have

- [ ] **A sound for each gun** · P3 · M · Polish — reports come from four category recipes (pistol, rifle, shotgun,
  sniper; see [SfxSynth.Weapons.cs](../Assets/Scripts/Audio/Synth/Recipes/SfxSynth.Weapons.cs)), so the AK and the
  PP-9 sound like their category. Give each gun its own recipe parameters, and suppressed variants. **Done when:** the
  AK and the Mosin are told apart by ear.
- [ ] **Remove the no-op pellet multiplier** · P3 · S · Tech debt — `pelletMult = pellets > 1 ? 1f : 1f`
  ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L458)) is left from an earlier design. Shotguns fire one
  full-damage cast per pellet, which is right. **Done when:** the variable is gone and the comment stays.
- [ ] **Update the README's "combat rewrite in development"** · P3 · S · Docs — the README still lists "a
  ballistics-based combat rewrite" as in development, but server-resolved ballistics are what the game runs. Say what's
  left (melee, attachments, balance) or drop the line. **Done when:** the README matches the code.

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text):

- [ ] **Close:** "Place breaching charge" on `CampGate` (tasks.md line 3412). Breach charges are armed from the pack
  (`Explosives.Arm`), and the solo smoke test already blows a camp gate with one. Mark it `[~]`.
