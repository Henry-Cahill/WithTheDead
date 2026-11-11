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

- **Sixteen guns:** the first six (pistol, shotgun, AK-74, Mosin, PP-9 SMG and the flare gun), seven period guns from
  Kentucky (.38 revolver, M1911, .22 rifle, police pump shotgun, .30-06 hunting rifle, .30-30 lever action, AR-15) and
  three from the Zone (SVD, PKM, AKS-74U), all in [ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs#L80).
  That file also registers 23 kinds of round and 13 attachments across seven slots.
- The ballistics are working: penetration against armour class, armour wear, bleeding and fractures, per-pellet
  shotgun fire ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L457)), fouling, jamming and cleaning, and
  a wear roll per shot or swing that can break the weapon ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L499)).
  There's also a technician's upgrade tree, and stripping attachments from enemies' guns.
- Melee weapons each handle their own way ([MeleeStats.cs](../Assets/Scripts/Weapons/MeleeStats.cs)): reach, arc, wind-up
  and recovery, and chances to stagger or knock down the dead; a swing meets one to three bodies by its arc. The shove can
  put the dead on the ground for a stomp. Two-handed weapons get their own swing.
- Grenades, pipe bombs, molotovs and three noise lures are thrown from the pack on one arc (`Throwables`,
  [Throwables.cs](../Assets/Scripts/Weapons/Throwables.cs)); breach charges are armed (`Explosives`,
  [Camps.cs](../Assets/Scripts/World/Camps.cs#L625)).
- In co-op the server resolves shots with lag compensation, and friendly fire and PvP are server rules
  ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L289), [NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs#L622)).

## P1 — next up

- [x] **More guns, and period ones** · P1 · L · Content
  - **Done (2026-11-09):** Ten guns, registered by `RegisterKentuckyGuns` and `RegisterZoneGuns`
    ([ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs)). Kentucky's: a .38 revolver, an M1911, a .22
    rifle, a police pump shotgun (the old shotgun already pumped, so this is the short police gun), a .30-06 hunting rifle,
    a .30-30 lever action and a semi-automatic AR-15. The Zone's: the SVD, the belt-fed PKM and the AKS-74U. Seven new
    rounds (.38 Special, .45 ACP, .22 LR, .30-06, .30-30, 5.56 ball and M855). Each gun has its own `GunStats`, its own
    report and reload ([SfxSynth.Weapons.cs](../Assets/Scripts/Audio/Synth/Recipes/SfxSynth.Weapons.cs);
    `WeaponAudio.ShotSound` picks them for the survivor and the people alike), a hand-built model
    ([ItemModels.Guns.cs](../Assets/Scripts/World/ItemModels.Guns.cs)) and a hold of its own (`GunStats.Hold`: where the
    fist closes and where `WeaponHandIK` puts the off hand). The models went through the item pipeline: `ItemModelExport`
    now writes the hand-built items' catalogue, and `catalog.py` queues items that only code registers, guns first. Three
    looks of each are in [GeneratedItemMeshes](../Assets/Resources/GeneratedItemMeshes), reviewed in the smoke mode's
    gallery: three that came out lying diagonally or on their side were made again from new seeds, and the PKM's
    descriptions (`out/mesh/descriptions.json` on the Spark) were rewritten by hand after all three of its looks were
    drawn as an RPK.
    Loot: three new container types ([LootContainer.cs](../Assets/Scripts/Items/LootContainer.cs)), each with guns of its
    own (`RegisterGunLoot` in [ZoneContent.Gear.cs](../Assets/Scripts/Content/ZoneContent.Gear.cs)). A farmhouse's **gun
    cabinet** has the .22 and the lever action; homes out in farm country now loot as farmhouses
    ([MapGameplay.Loot.cs](../Assets/Scripts/World/Map/MapGameplay.Loot.cs)). A gun shop's **gun counter** has the AR-15
    and the M1911. A police **weapons locker** (in armouries and police car trunks) has the police pump. The Zone's
    military gun locker gets the AKS-74U, SVD and PKM. Traders: the gunsmith sells guns at every loyalty level (LL3 and
    LL4 sold none): LL1 the .38 and .22; LL2 the M1911, lever action, police pump and AKS-74U; LL3 the .30-06, AR-15 and
    SVD; LL4 the PKM. The fence and barters stock some too ([TraderCatalog.cs](../Assets/Scripts/Economy/TraderCatalog.cs)).
    The people and scavs carry them ([HumanLoadouts.cs](../Assets/Scripts/AI/Humans/HumanLoadouts.cs),
    [RaidLoadout.cs](../Assets/Scripts/Extraction/RaidLoadout.cs)). The new `guns` smoke mode
    ([SmokeTest.Guns.cs](../Assets/Scripts/Dev/SmokeTest.Guns.cs)) checks all of it, every look in the hands included:
    306 checks.
    Fixed on the way: a Night Shift edit (40a7fe0a0, 2026-10-26) left `MoreLoot` in
    [InteriorPass.Rooms.cs](../Assets/Scripts/World/Map/InteriorPass.Rooms.cs) placing none of a room's further loot,
    so rooms had less loot than their profiles asked for. `GunShape` measured a hand-built model from its parts' corners
    only, so a long forend or stock counted only at its ends; it now sees every part, for attachments and holds alike.
    A look whose box is stretched by a bipod or a long magazine could leave a hand closing on air; the hand now goes
    onto the gun's outline there (`HumanoidRig`).
    Left over: the stance seats the butt in the shoulder and the off hand reaches about 0.55 m from it, so the SVD and
    PKM are held ahead of the magazine rather than by their handguards (blading the stance would fix it). The AK-74,
    Mosin, PP-9 and flare gun still have no model of their own and draw as a plain box. The Project Zomboid catalogue's
    own firearms (`pz_items.txt`) still turn up in gun lockers and plain lockers, wanting rounds that don't exist.
  - **Why:** An extraction shooter with weapon modding and trader tiers has six guns, one of them a flare gun. Kentucky
    in 1993 is full of civilian guns, and Project Zomboid players will look for them.
  - **Do:** Add 8 to 12 guns in two groups. **Kentucky:** a .38 revolver, an M1911, a .22 rifle, a pump shotgun, a
    hunting rifle in .30-06, a lever action in .30-30 and an AR-15. **Zone and trader tier:** an SVD-style DMR, a
    PKM-style LMG, an AKS-74U. Each needs `GunStats` and an item def (with a loot-table place and a trader price), a
    model through the item pipeline, a hand pose (`WeaponHandIK`) and a sound recipe.
  - **Done when:** Each trader tier and each kind of building (gun shop, police, farmhouse) has guns of its own.

- [x] **Attachments you can see, and lights and lasers that work** · P1 · M · Gap
  - **Done (2026-11-08):** [WeaponAttachments.cs](../Assets/Scripts/Weapons/WeaponAttachments.cs) builds each fitted
    attachment onto the gun in the hand (`HumanoidRig.SetHeldMods`, fed by `PlayerAvatar` for the survivor and by
    `HumanNpcAvatar` for the people's rolled kit), rebuilt only when the gun's mods change: red dot (a reflex sight on a
    handgun's slide, a tube on a riser on a long gun), 4x scope in rings, suppressors, compensator, long barrel (a can goes
    on its end), short-barrel nut, light and laser (under a handgun's frame, on a long gun's right side), foregrip (the off
    hand moves onto it), tactical and folding stocks, and extended magazines. Each sits on the model as drawn:
    [GunShape.cs](../Assets/Scripts/Weapons/GunShape.cs) measures every gun model's outline once (a generated mesh is
    read back from its file), so all three looks of the pistol and shotgun get their kit on their own slide, top and
    muzzle. One generated shotgun look (`shotgun@1`) was modelled end for end and held butt first; it's now detected and
    turned round in the hold. Each attachment is one baked mesh shared by everything holding that gun. The kit hides with
    the body in first person. The light ([WeaponTactical.cs](../Assets/Scripts/Weapons/WeaponTactical.cs)) is a shadowed
    spot from its lens, under the shadow budget, aimed where the barrel points and swung onto the shot's line when the gun
    comes up (aiming down the sights now raises the figure's gun). L switches the device on the gun in hand, else the
    flashlight. The light runs a battery like the flashlight's (`GearKind.WeaponLight`; its charge moves onto the gun as
    `ItemExtra.LightUsed` while fitted and back off with it), sputters when low, takes batteries, lights the HUD's light
    icon, and the dead see it and are seen in it as with the flashlight. The laser draws a red dot (and a faint beam) where
    the barrel points, and where the round will go once aimed (`PlayerCombat.ShotLine`). The new `attachments` smoke mode
    ([SmokeTest.Attachments.cs](../Assets/Scripts/Dev/SmokeTest.Attachments.cs)) checks all of it by night with five guns:
    89 checks, the light's spot going from 0.35 to 1.0 in luminance, the dot within a millimetre of the shot's end.
    Left over: first person draws no body or gun at all, so the kit only shows in the other views (the light and laser work
    in every view), and co-op peers see each other's guns bare (the snapshot sends the held item's id only).

- [x] **Molotovs and noise lures** · P1 · M · Gap
  - **Done (2026-11-09):** Everything thrown goes through `Throwables` ([Throwables.cs](../Assets/Scripts/Weapons/Throwables.cs)),
    the grenade's arc made shared: in the isometric view a throw is now a lob that comes down on the ground under the
    cursor (up to 16 m) instead of always flying the same 13 m, and the grenade key throws another of whatever was thrown
    last. Five new items ([ZoneContent.Gear.cs](../Assets/Scripts/Content/ZoneContent.Gear.cs#L144)), each with a model
    ([ItemModels.Throwables.cs](../Assets/Scripts/World/ItemModels.Throwables.cs)), loot, an economy rule and a trader (the
    bar's clocks, firecrackers and radio; the fence's molotovs and pipe bombs). The **molotov** is a field craft from an empty
    bottle, a rag and a litre poured out of a gas can (`Recipe.FuelLitres`, [Hideout.cs](../Assets/Scripts/Economy/Hideout.cs#L120)).
    It bursts on whatever it hits into a fire ([Fire.cs](../Assets/Scripts/Weapons/Fire.cs#L81), `FirePatch`) 2 m round
    that spreads in tongues along the floor (walls and drops stop them) and burns for 14 s. Whatever stands in it catches
    (`Aflame`): the dead take 10 hit points a second, don't flinch from a burn
    ([ZombieHealth.cs](../Assets/Scripts/Zombies/ZombieHealth.cs)), keep walking, burn on for 7 s out of it and set alight
    anyone they brush against; the living take 7 a second on random limbs. The **pipe bomb** is a crafted grenade (scrap,
    chemicals, duct tape) with a longer fuse and a smaller blast. Three **noise lures** ([NoiseLure.cs](../Assets/Scripts/Weapons/NoiseLure.cs)):
    firecrackers (7 s of bangs, heard 45 m off), a wind-up alarm clock (rings 4 s after it's thrown, for 30 s, 32 m) and a
    timer radio (comes on after 7 s and plays for a minute, 24 m). Their pulses are lures (`NoiseSystem.EmitLure`): the dead
    come to them as to any noise, and one that has lost sight of its prey for 2 s drops the chase for one
    (`ZombieAI.DropChaseFor`), which is what pulls them off a door they were battering. Mutants come too (a "lure" world
    event). The clock and the radio are left lying where they stop, to be picked up again. They have sounds of their own
    ([SfxSynth.Throwables.cs](../Assets/Scripts/Audio/Synth/Recipes/SfxSynth.Throwables.cs)): the alarm's twin bells, a
    firecracker, the radio's tinny tune. In co-op the server sounds the lures and burns the fire (`NetThrowables`), and its
    AI snapshot says which of the dead are alight. The new `throwables` smoke mode
    ([SmokeTest.Throwables.cs](../Assets/Scripts/Dev/SmokeTest.Throwables.cs)) checks all of it: 23 checks, all passing.
    A horde of ten set on the survivor shut in a house went from 5 m off its door to about 1 m from an alarm clock tossed
    14 m out, every one off the chase within 3 s of the ring. Six of the dead coming through a doorway all caught fire
    from a molotov thrown into it and burned to death within 8 s. (The run stands the town's people down first: an armed
    squad comes to the din and shoots the dead.)
    Fixed on the way: a zombie that heard a noise froze where it stood. `TickInvestigate` returned before setting off
    whenever an investigation was under way ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs#L409)), and had since
    the first commit. Now the dead really do go to gunfire, footsteps and every other noise.
    Left over: the fire burns creatures only, not buildings, barricades or loot, and co-op peers see one another burn only
    while they stand in the flames.
  - **Why:** [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) G. Grenades and breach charges are the only things to
    throw or set ([Camps.cs](../Assets/Scripts/World/Camps.cs#L632)). Horde management, Project Zomboid's bread and
    butter, has no tools: no firecrackers or alarm clock to pull the dead away, and no molotov to burn a crowd at a
    choke point.
  - **Do:** Reuse the grenade's throw arc (`Explosives.Throw`). A **noise lure** (firecrackers, an alarm clock, a radio
    on a timer) is a timed `NoiseSystem` pulse. A **molotov** is a burning area that damages and spreads, crafted from a
    bottle, fuel and a rag. A **pipe bomb** is a crafted grenade.
  - **Done when:** A thrown alarm clock pulls a horde off a house, and a molotov burns the dead in a doorway.

## P2 — soon

- [x] **Special ammunition that does what its card says** · P2 · M · Gap
  - **Done (2026-11-09):** A round now carries its effect (`AmmoStats.Tracer`, `AmmoStats.Incendiary`,
    [Ballistics.cs](../Assets/Scripts/Weapons/Ballistics.cs)), and its card names it in an "Effect" line
    ([WeaponItems.cs](../Assets/Scripts/Weapons/WeaponItems.cs)). Only the 5.45 **tracer** draws a trail now: a red streak
    that flies from the muzzle to where the round stopped and fades there (`CombatFX.Tracer`,
    [CombatFX.cs](../Assets/Scripts/Core/CombatFX.cs)). A client's peers see it too, because the server's echo of the shot
    (`cz.shotfx`) says whether it was a tracer (a listen-server host's own shots aren't echoed at all; see the item below).
    The old streak on every shot is gone, and so is the new material it made each time. The 9mm
    and 12g **incendiaries** set whoever they hit alight with the molotov's burning (`CombatResolver.Ignite` calls
    `Aflame.Ignite`), under the friendly-fire rule a runner's body keeps. Their burns, and the fire passed on from the
    burning, count as the gun's: `Aflame` keeps the weapon that lit it. A co-op runner the server has burning now sees it on
    themselves (`cz.dmg` carries it). **Cold-Snap**, **Heavy-Velocity** and **Fuel-Gel** are gone. Nothing models cold air,
    overcharged wear or a sticky residue, Cold-Snap out-stat the BP round, and the incendiaries cover Fuel-Gel's niche.
    Nothing referred to them and no one could get them. The other four Night Shift rounds keep their numbers and get cards
    that say what those numbers do: the 9mm FMJ, the 12g flechette (one bundle of darts, not "hundreds"), the 5.45 FMJ
    (nothing made it "surprisingly accurate") and the 7.62x54R AP (nothing made its recoil hurt). The three special rounds
    were unobtainable too, with no loot, trader or recipe. The technician now sells them (the tracer from loyalty 2, the
    incendiaries from 3), the Zone's military gun lockers hold tracers and 9mm incendiaries, and Kentucky gun-shop counters
    hold the 12g. The new `rounds` smoke mode ([SmokeTest.Rounds.cs](../Assets/Scripts/Dev/SmokeTest.Rounds.cs)) checks it
    through the trigger: 31 checks, all passing. A plain round drew no trail and a tracer round one streak, and a plain 9mm
    set nobody alight. A 9mm and a 12g incendiary each set a zombie alight that burned to death 4.9 s and 5.4 s after the
    shot, credited to the pistol and the shotgun.
    Left over: the 12g incendiary is one burning slug, not a dragon's-breath spray; an incendiary that hits a wall or the
    floor starts no fire; armour that stops the round doesn't stop the burning; NPCs never load special rounds; and co-op
    peers still don't see a third runner burn.
  - **Why:** Night Shift added tracer, incendiary, "Cold-Snap", "Heavy-Velocity" and "Fuel-Gel" rounds with
    descriptions that promise fire, glowing trails and sticky residue
    ([ZoneContent.Arms.cs](../Assets/Scripts/Content/ZoneContent.Arms.cs#L56)). `BulletDamage` uses damage, penetration,
    armour damage, bleed and fracture only ([CombatResolver.cs](../Assets/Scripts/Core/CombatResolver.cs#L126)), and
    every shot already draws a tracer ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L479)). The code
    comments are also odd ("fire-slug … freezing zombies in place").
  - **Do:** For each round, implement the effect (incendiary sets the hit actor burning; only tracers draw a visible
    trail) or rewrite the description to match what it does. Remove the ones that don't earn a place.
  - **Done when:** Every special round's card describes its real effect.

- [x] **Melee with weight** · P2 · M · Gap
  - **Done (2026-11-09):** Each melee weapon handles its own way ([MeleeStats.cs](../Assets/Scripts/Weapons/MeleeStats.cs)).
    The catalogue keeps its damage, reach, arc, swing time and knockback. On top of those, each weapon has:
    - a wind-up: the blow lands that long after the press, on whatever is in reach and arc then;
    - a recovery before the next swing, stomp or shove. A click late in a recovery is kept, and swings the moment it's over;
    - a chance to stagger the dead. A stagger spoils a lunge; a blow that doesn't stagger only makes them flinch, and they
      keep coming;
    - a chance to knock them down (`ZombieAI.KnockDown`). They lie for 2–3 s, then get up, and their hit box lies along
      the body while they're down.

    The six hand-made weapons are tuned by hand; the rest are worked out from weight, grip and kind. Bare hands get a profile
    of their own: a 1.5 m, 70° punch instead of the 2.4 m, 90° swing they had. The arc sets how many bodies a swing carries
    through: one under 75°, two up to 95°, three beyond. Each further body takes 60% of the share before it. An attack with
    one of the dead on the ground nearest at the feet (knocked down, or a crawler) is a stomp. It lands on the head if that's
    in reach, and anywhere else on the body for half; boots and Strength make it harder. A swing passes over the downed while
    anything stands in it, and comes down on them when nothing does: on the head if it's in reach (a likelier crit), else on
    the nearest of the body, with no crit.
    A shove knocks the dead down 30% of the time, more from behind or into one already reeling. The animator times the swing
    to the wind-up and has a stomp, a fall and a getting-up. Co-op carries a blow's chances to the server, and the dead's
    ground state back to the clients. The item card shows the swing, the bodies and the chances.

    The new `melee` smoke mode ([SmokeTest.Melee.cs](../Assets/Scripts/Dev/SmokeTest.Melee.cs)) walls in a corridor and checks
    all of it through the mouse: 57 checks, all passing (the wave run still passes its 857). Against three of the dead coming
    down the corridor:
    - the knife (0.10 s wind-up, one body) cleared it in 5.7 s with 6 stabs, though one of the dead got a lunge in and it
      landed;
    - the bat (0.28 s, up to three bodies) cleared it in 4.7 s with 3 swings, one through two of them, and none got a lunge off;
    - the axe (0.40 s, two bodies) split two with its first chop and cleared it in 4.2 s with 2 swings, none getting a lunge off.

    Over 16 blows each at one of the dead, the knife staggered it 3 times and never knocked it down. The bat staggered it 7
    times and knocked it down 7; the axe staggered it 4 times and knocked it down 8. A stomp from beside a shoved-down zombie's
    head did 0.91 of a full one; from its feet, its head 3.1 m off, 0.52. **CloneZ ▸ Smoke Test ▸ Watch Melee** plays the
    same run in the editor's Game view.

    Left over: people and mutants ignore the stagger and knockdown chances, and other runners don't see a remote player's
    swing or stomp. The survivor doesn't step into a stomp, so at full reach the heel lands short of the body.
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

- [ ] **Let a host's own shots reach the other runners** · P2 · S · Co-op
  - **Why:** A shot reaches the other runners only when a client sends it to the server: `OnShot` resolves it and echoes
    it to everyone else as `cz.shotfx` ([NetPlayers.cs](../Assets/Scripts/Net/NetPlayers.cs)). A listen-server host
    resolves its own shots locally (`CombatResolver.Fire` → `Resolve`) and nothing sends them on, so its friends don't
    hear its gunfire or see its firing animation or a tracer round's streak. The dedicated server has no player of its own,
    so the usual co-op setup isn't affected. Found while making tracers draw for peers.
  - **Do:** On the server, echo the host survivor's own shots (`CombatResolver.ShotResolved` with the local player as the
    shooter) to every client as `cz.shotfx`, as `OnShot` does for theirs.
  - **Done when:** In a host-and-client session, the client hears the host fire and sees the streak of a host's tracer.

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
  PP-9 sound like their category. The ten period guns have reports and reloads of their own (`GunStats.Sound` and
  `ReloadSound`, which `WeaponAudio.ShotSound` takes first); the first six don't yet. Give each gun its own recipe
  parameters, and suppressed variants. **Done when:** the AK and the Mosin are told apart by ear.
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
