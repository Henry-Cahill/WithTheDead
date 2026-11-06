# Vehicles — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** drivable cars, the cars parked on the Kentucky map, fuel, keys and parts, the helicopter, drones, and
vehicles in co-op.
**Code:** [Vehicles.cs](../Assets/Scripts/World/Vehicles.cs) (`Vehicle`, `VehicleDriver`, `VehicleModule`; the
`Assets/Scripts/Vehicles` folder is empty), [VehiclePass.cs](../Assets/Scripts/World/Map/VehiclePass.cs) and its
partials (the map's parked cars), [Helicopter.cs](../Assets/Scripts/World/Zone/Helicopter.cs),
[Gadgets.cs](../Assets/Scripts/Gadgets/Gadgets.cs) (drones), `VehicleSave` in
[ProfileData.cs](../Assets/Scripts/Profile/ProfileData.cs#L328).

## Where it stands

- **Drivable cars** (`VehicleModule`, [Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L878)) are set down on clear
  spots in a raid. They have wheels, battery, spark plug and radiator to fit, fuel from a gas can, wear on the engine,
  tyres and battery, hotwiring, headlights and engine noise. They run over the dead, sync in co-op, and are saved per
  Zone.
- **The Kentucky map's parked cars** ([VehiclePass.cs](../Assets/Scripts/World/Map/VehiclePass.cs#L14)) are period
  scenery in 12 body styles, with wear such as open doors, crashes, burns and flat tyres. One in five has a glovebox
  (horn and ignition) and a searchable trunk. None can be driven.
- **The helicopter** handles insertion at a chosen landing zone, flare extraction and random flyovers.
- **Drones**: an FPV and a ground drone, with a grenade payload and a light.

## P1 — next up

- [ ] **Let some of the parked cars be driven** · P1 · L · Gap
  - **Why:** The Kentucky map has hundreds of cars in driveways, lots and ditches, and players will try every one. The
    drivable cars are a separate handful dropped on clear spots, all with the same generic body
    ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L87)). Project Zomboid's car loop (find one, find its key, fix
    it, fuel it) needs the cars that are already there.
  - **Do:** When a streamed piece is built, promote a share of intact parked cars (no `Burnt` or `Crashed` wear,
    [VehiclePass.Models.cs](../Assets/Scripts/World/Map/VehiclePass.Models.cs#L37)) to real `Vehicle`s, keeping the
    prop's body, paint and trunk contents. Roll missing parts and fuel per car from its place (a police lot's cruisers
    are better kept). Save promoted cars with the piece, like containers.
  - **Done when:** In Bardstown a player can pick a parked car, fix it, drive it away, and find it where they left it.

- [ ] **Trunks on drivable cars** · P1 · M · Gap
  - **Why:** `Vehicle.GetActions` offers the seat, parts, fuel, lights, hotwiring and removal
    ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L228)), but no storage, while the parked wrecks do have trunks.
    A car is the best loot hauler in the genre, and here it can't carry anything.
  - **Do:** Give each `Vehicle` a trunk, a `StorageFixture` sized by body style. Save its contents in `VehicleSave` and
    sync it like other containers.
  - **Done when:** Loot put in a trunk is still there after driving, extracting and coming back.

- [ ] **Refuel at gas stations** · P1 · M · Gap
  - **Why:** Fuel only comes from a carried gas can ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L359)). The
    map's gas stations have pumps and forecourts
    ([VehiclePass.Streets.cs](../Assets/Scripts/World/Map/VehiclePass.Streets.cs#L493)), but the pumps are scenery.
  - **Do:** Make pumps interactable: they fill a car or a can while the grid is up
    ([PowerGrid.cs](../Assets/Scripts/Core/PowerGrid.cs)). After the shutoff they need a generator nearby, and they
    hold a finite amount, saved per station.
  - **Done when:** A car can be driven to a pump and filled, and pumps go dead with the power.

## P2 — soon

- [ ] **Body styles that drive differently** · P2 · L · Content
  - **Why:** The map knows 12 body styles ([VehiclePass.Models.cs](../Assets/Scripts/World/Map/VehiclePass.Models.cs#L7)),
    but every drivable car is the same 1.8 × 4.2 m sedan.
  - **Do:** Give drivable cars the prop's body, with size, mass, top speed, fuel tank and trunk size per style. A
    pickup has a bed, a van a big trunk, a cruiser a light bar.
  - **Done when:** A pickup, a sedan and a van are each drivable and feel different.

- [ ] **A camera for driving** · P2 · M · Polish
  - **Why:** Nothing in the camera code changes for a car. The normal view follows the seated survivor
    ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L596), `VehicleDriver`). At driving speed the isometric view
    shows too little road ahead.
  - **Do:** While driving, zoom out with speed and lead the camera along the velocity in the isometric view, and use a
    chase camera in the shoulder view.
  - **Done when:** At top speed on a highway the road ahead is visible far enough to react to a wreck.

- [ ] **Passenger seats** · P2 · L · Co-op
  - **Why:** A car has one `Seat` and one `Driver` ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L29)). In co-op
    the squad can't ride together.
  - **Do:** Add passenger seats (count by body), synced enter and exit, and passengers who can shoot from the windows
    in the shoulder and first-person views.
  - **Done when:** Two players drive off together and both get out safely.

- [ ] **A key for each car** · P2 · M · Gap
  - **Why:** Any item called `carkey` starts any car ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L276),
    [L402](../Assets/Scripts/World/Vehicles.cs#L402)). Finding *the* key is half the fun in Project Zomboid.
  - **Do:** Give each car's key an id in its item data, naming the car, and spawn it nearby: the owner's house, the
    glovebox, a corpse by the car. Otherwise, hotwire.
  - **Done when:** A key only starts its own car, and its tooltip says which.

- [ ] **Check vehicle authority in co-op** · P2 · M · Co-op
  - **Why:** The driving client sends position, yaw, speed and engine state, and the server relays them unchecked
    (`OnDrive`, [Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L767)). Part and fuel changes from any client are
    accepted the same way (`OnState`, [L834](../Assets/Scripts/World/Vehicles.cs#L834)). Run-over damage is worked out
    on the driver's machine (`RunOver`, [L533](../Assets/Scripts/World/Vehicles.cs#L533)). Among friends that's fine,
    but it's a problem before PvP or public servers, and it needs checking that a zombie run over by a client's car
    dies for everyone.
  - **Do:** Test run-overs with a client driving. Before PvP, have the server sanity-check speed and teleports, own
    part and fuel state, and apply run-over damage.
  - **Done when:** The host/client smoke test drives a client's car through the dead and both sides agree on the
    bodies.

- [ ] **Check the helicopter in co-op** · P2 · M · Co-op
  - **Why:** Flyovers are mirrored, but insertion and flare-extraction helicopters may be spawned only where they're
    called ([Helicopter.cs](../Assets/Scripts/World/Zone/Helicopter.cs#L146)). If so, a squadmate never sees the
    helicopter that's taking them out.
  - **Do:** Test both with two players. If either is local-only, have the server announce the flight (start, path,
    landing, take-off).
  - **Done when:** Both players see the same helicopter land for the flare.

## P3 — nice to have

- [ ] **A horn** · P3 · S · Gap — drivable cars have no horn (the map wrecks' gloveboxes do). A horn is a
  `NoiseSystem` pulse, a classic way to pull a horde off a building. **Done when:** honking draws the dead from a
  block away.
- [ ] **Save engine heat and headlights** · P3 · S · Save — `VehicleSave`
  ([ProfileData.cs](../Assets/Scripts/Profile/ProfileData.cs#L328)) keeps parts, fuel, engine health, charge, tyres,
  position and yaw, but not heat or lights. A car left with its lights on should come back with a flatter battery.
  **Done when:** both round-trip through a save.
- [ ] **Engine and road sound** · P3 · M · Polish — remote engines are pitched by replicated speed alone, with no
  revs or throttle. Add revs, gear changes, skids, tyre bursts and gravel vs asphalt. **Done when:** a car pulling away
  sounds like it from inside and from the street.
- [ ] **Run-overs without scanning every actor** · P3 · S · Perf — `RunOver` walks `Actor.All` every tick
  ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L533)). Use an overlap box in front of the bumper. **Done when:**
  driving through a town with 200 actors costs no more than an empty road.
- [ ] **A helicopter event that crashes** · P3 · M · Content — Project Zomboid's helicopter ends in a crash site with
  loot and a crowd of the dead. Kestrel has a static `crashed_helicopter` landmark, but there's no crash event. Have a
  flyover sometimes come down, leaving a persistent, lootable wreck that pulls a horde. **Done when:** a flyover can end
  in a crash site the squad can find.

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text):

- [ ] **Close:** "cache the PlayerStats component in VehicleDriver" (tasks.md line 47) is already done:
  `_stats ??= GetComponent<PlayerStats>()` ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L655)). Mark it `[~]`.
- [ ] **Check, then close:** "a head-light for drones with a toggle" (tasks.md line 8644). The drone HUD already shows
  `LIGHT ON` ([Gadgets.cs](../Assets/Scripts/Gadgets/Gadgets.cs#L547)), so it probably exists.
- [ ] Side mirrors, door handles and a roof antenna on the parked cars (`VehiclePass.Models.cs`): failed (tasks.md line
  12583). If redone, build them into the car body's mesh in `VehiclePass.Models`, never as `CreatePrimitive` children
  (those caused floating geometry).
- [ ] `/// <summary>` docs for `Helicopter.cs`: failed (tasks.md line 245).
