# World — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** the statewide Kentucky map and its streaming, buildings, interiors and furniture, fixtures and
interactions, the starter home, base building and barricades, weather and time, power and water in the world, and the
Kettle's town.
**Code:** [Assets/Scripts/World](../Assets/Scripts/World) (about 110,000 lines), especially
[World/Map](../Assets/Scripts/World/Map) ([KentuckyMap.cs](../Assets/Scripts/World/Map/KentuckyMap.cs),
[KentuckyStreamer.cs](../Assets/Scripts/World/Map/KentuckyStreamer.cs) and the passes),
[World/Fixtures](../Assets/Scripts/World/Fixtures), [World/Interaction](../Assets/Scripts/World/Interaction),
[BuildPiece.cs](../Assets/Scripts/World/BuildPiece.cs), [Barricade.cs](../Assets/Scripts/World/Barricade.cs),
[Camps.cs](../Assets/Scripts/World/Camps.cs), [WeatherSystem.cs](../Assets/Scripts/Core/WeatherSystem.cs),
[Tools/World](../Tools/World/README.md) (the map data).

## Where it stands

- The whole of Kentucky from OpenStreetMap, in 256 m cells, streamed around the survivor in 128 m pieces, with
  interiors furnished on demand and NavMesh baked per piece. The Kettle stands on a reserved lot in Bardstown.
- About 70 kinds of interactable (67 `Interactable` subclasses) with real actions. Doors lock, bash and kick in;
  windows break, board up and can be climbed through; taps, stoves, radios, lights and storage all work. The starter
  home and its yard are hand-built for Wave Survival.
- Base building and barricades, camps with gates and locks, and an offline-raid rule.
- Winter weather, time of day, and a power and water shutoff.
- The code guards itself well. Two of those guards are hiding real bugs (below).

## P1 — next up

- [ ] **Find what makes furniture with NaN vertices** · P1 · M · Bug
  - **Why:** The editor log has "[CloneZ] A map mesh had vertices that aren't finite numbers (made harmless):
    World/Streamed/Piece 11914_9093/Interior KY2622504492 L1/Bldg_KY2622504492 L1/Furniture (48 of 9108 vertices)".
    `RaidMesh` folds the bad triangles away ([RaidMesh.cs](../Assets/Scripts/World/RaidMesh.cs#L918)), so nothing
    explodes. But some furniture builder got a zero or NaN size, direction or normal for this building: most likely a
    degenerate room (a zero-length wall, or a room smaller than the piece it's meant to hold). The furniture it meant to
    draw is missing.
  - **Do:** Make the warning name the builder and the room, for example by tagging the mesh's sub-parts in debug
    builds. Reproduce with building KY2622504492 in Bardstown, and fix the input (skip or clamp degenerate rooms before
    furnishing).
  - **Done when:** A full Bardstown streaming smoke run (`-clonez-smoke-zone bardstown`) logs no "aren't finite
    numbers" lines.

- [ ] **Make sure fixture changes survive streaming, extraction and a late join** · P1 · L · Save / Co-op
  - **Why:** The Wave Survival save lists the starter home's fixtures, containers, barricades, house pieces, farm and
    water ([SaveData.cs](../Assets/Scripts/Core/SaveData.cs)). On the streamed map, containers are remembered per piece,
    but there's no single list of which other states (a door kicked in, a light left on, a window boarded) survive (a)
    the piece being let go and built again, (b) extraction and a later raid, and (c) a squadmate joining late. In co-op,
    fixtures sync by forwarding actions as they happen ([NetWorld.cs](../Assets/Scripts/Net/NetWorld.cs)), so a late
    joiner could see defaults.
  - **Do:** Write the table (fixture × the three cases) by testing, and fix the gaps. For late joins, have the server
    send the current state of fixtures in a client's interest area when they arrive.
  - **Done when:** The table is in [MECHANICS_COVERAGE.md](../MECHANICS_COVERAGE.md), and the streaming smoke run also
    checks a door state, not just a container.

- [ ] **Keep streaming off the main thread's back** · P1 · L · Perf
  - **Why:** `KentuckyStreamer.Update` finishes regions, gathers focus points, plans and builds every frame
    ([KentuckyStreamer.cs](../Assets/Scripts/World/Map/KentuckyStreamer.cs#L494)). NavMesh source collection walks the
    hierarchy ([L1464](../Assets/Scripts/World/Map/KentuckyStreamer.cs#L1464)), and agent recovery calls
    `FindObjectsByType<NavMeshAgent>` ([L1502](../Assets/Scripts/World/Map/KentuckyStreamer.cs#L1502)). Driving or
    sprinting across town is where hitches show.
  - **Do:** Give each step a per-frame time budget and log hitches over, say, 50 ms with the step that caused them
    (the dedicated server's status line already reports the slowest frame). Cache NavMesh sources per piece, and have
    agents register with the streamer.
  - **Done when:** A 10 km drive in the streaming smoke run has no frame over the budget, or each one is logged with its
    cause.

## P2 — soon

- [ ] **Close out the doors-and-windows tasks, and add the two missing verbs** · P2 · S · Gap
  - **Why:** Night Shift kept failing door and window tasks because most of them already existed. Raid doors can be
    kicked in, oiled and reinforced ([RaidDoor.cs](../Assets/Scripts/World/Fixtures/RaidDoor.cs#L185)). Home doors can
    be forced, locked and bashed ([HomeDoor.cs](../Assets/Scripts/World/Fixtures/HomeDoor.cs#L182)). Home windows can be
    broken and climbed through ([HomeWindow.cs](../Assets/Scripts/World/Fixtures/HomeWindow.cs#L210),
    [L236](../Assets/Scripts/World/Fixtures/HomeWindow.cs#L236)). Raid windows can be smashed, boarded up and have the
    boards pried off ([RaidWindow.cs](../Assets/Scripts/World/Fixtures/RaidWindow.cs#L199),
    [L404](../Assets/Scripts/World/Fixtures/RaidWindow.cs#L404), [L330](../Assets/Scripts/World/Fixtures/RaidWindow.cs#L330)).
    Two things are missing: the roll-up door can be secured, banged and repaired, but not pried open
    ([RollUpShutter.cs](../Assets/Scripts/World/Fixtures/RollUpShutter.cs#L110)), and a locked raid door can only be
    kicked, not worked open with a crowbar or axe as camp locks can ([Camps.cs](../Assets/Scripts/World/Camps.cs#L383)).
  - **Do:** Mark the obsolete failed tasks `[~]`: "Break the window" (tasks.md line 6881), "Board the window" (6097) and
    "Break the door open" (8901). Add "Pry open the roll-up door" with a crowbar (12624, still open) and a quieter,
    slower crowbar or axe option on locked raid doors.
  - **Done when:** The queue no longer lists work that exists, and both new verbs are saved like the rest.

- [ ] **Find out why the snowfield drops out of physics** · P2 · M · Bug (engine)
  - **Why:** Once per map, a few seconds after it first follows the survivor, the moving snowfield collider vanishes
    from the physics scene while still reading as enabled, and the survivor would fall through the world.
    `KeepGroundFound` checks every frame and toggles the collider to put it back
    ([KentuckyStreamer.cs](../Assets/Scripts/World/Map/KentuckyStreamer.cs#L562)). The log shows it happening ("…put
    back."). It works, but it's a workaround for what looks like a bug in the Unity 6000.7 alpha. Note that
    `_groundLost > 10` only stops the log lines; the recovery keeps running.
  - **Do:** Build a minimal repro (one moving box collider plus a raycast) and report it to Unity. Re-test on every
    Unity upgrade. Alternatively, drop the pattern: use static snowfield tiles per streamed piece instead of one box
    that moves.
  - **Done when:** The ground is never lost on a newer Unity or with static tiles, and the workaround can go.

- [ ] **Let the dead walk on player-built floors** · P2 · M · Gap
  - **Why:** Only the starter home's own floors go into the NavMesh bake; every floor or platform a survivor builds is
    kept out ([BuildPiece.cs](../Assets/Scripts/World/BuildPiece.cs#L58), [L460](../Assets/Scripts/World/BuildPiece.cs#L460)).
    Walls carve the mesh, but a raised platform is a place the dead can't path to, which makes it a safe spot.
  - **Do:** Rebake the local NavMesh tile when a floor is built or torn down (demolition already rebuilds,
    [L395](../Assets/Scripts/World/BuildPiece.cs#L395)), and add stairs or ramps so it connects.
  - **Done when:** The dead follow a survivor onto a built platform by its stairs.

- [ ] **Street lights that go dark with the grid** · P2 · M · Content
  - **Why:** The map summary counts lights per piece, and the statewide build logs "0 lights" for 25 pieces
    ([KentuckyMap.cs](../Assets/Scripts/World/Map/KentuckyMap.cs#L111),
    [KentuckyStreamer.cs](../Assets/Scripts/World/Map/KentuckyStreamer.cs#L993)). Streets are pitch dark at night with
    the power on, and the shutoff (`PowerGrid`) has no visible moment. Several Night Shift tasks wanted lamps (tasks.md
    lines 9892 and 9641, and a lamp toggle at 7808).
  - **Do:** Place lamps from OpenStreetMap's `highway=street_lamp` nodes, plus evenly along main streets where none are
    mapped. Light only those near the survivor, with shadows off. Turn them off at the shutoff, all at once or block by
    block.
  - **Done when:** Bardstown's main streets are lit at night until the power fails.

- [ ] **Redo the failed street and building detail in the right place** · P2 · L · Content
  - **Why:** Most failed Night Shift tasks were map props and building detail: curbs and sidewalks with colliders
    (6767), fire hydrants (6672), benches (9689), trash cans (9641), road signposts (9529), porch steps and rails
    (6204), porch swings (8279), front doors (8820), façade variety, storefront signage and shutters (6432, 11362,
    11400), awning chains (10018), pallets (11113), guard-rail detail (11415), fuel-station and school canopies (11216,
    11450), the stair tower (12468), riverbank debris and driftwood (12567, 11476), baseboards (12637, open), mailboxes
    (374, 10000), bookshelf, vending-machine and wall-clock detail (6822, 7991, 6323), and gas-pump jitter (5858).
    Local models failed them for a reason: they need to know the map passes' rules.
  - **Do:** Pick the ones that matter for play and readability first (curbs and sidewalks, front doors, porch steps,
    hydrants, signage). Build each into the pass that owns the object as RaidMesh geometry. Respect `site.Take` and
    `site.Free`, put ground-floor props and their `PropCollider` in `site.Town.Out`, and keep anything past the playable
    square collider-free in `site.Town.Far` ([LESSONS.md](../Tools/NightShift/memory/LESSONS.md)). Never use
    `CreatePrimitive` children: they caused the floating geometry.
  - **Done when:** The chosen props are in, and the floating-geometry report is clean.

- [ ] **Catch duplicate fixture save keys** · P2 · M · Save
  - **Why:** Fixture keys come from positions (`Interactable.KeyFor`). When two fixtures share a key, only the first is
    saved and restored, with a warning ([Interactable.cs](../Assets/Scripts/World/Interaction/Interactable.cs#L251)). A
    generator change can create collisions nobody notices.
  - **Do:** Have the smoke runs fail on duplicate keys, and include the floor or level and the fixture type in the key.
  - **Done when:** A smoke run reports zero duplicate keys across a streamed town.

- [ ] **Camp gate upgrades** · P2 · M · Gap — "Reinforce gate" with planks, nails and a hammer for +25% health
  (tasks.md line 12349), and an auto-closing gate (9010), both failed on `CampGate` in
  [Camps.cs](../Assets/Scripts/World/Camps.cs). Both suit the base-raiding loop. **Done when:** gates can be reinforced
  and set to close by themselves, with the state saved and synced.

- [ ] **Check that power, water and barricades agree in co-op** · P2 · M · Co-op
  - **Why:** The world packet carries the hour, day and weather ([NetWorld.cs](../Assets/Scripts/Net/NetWorld.cs#L84)).
    `PowerGrid` and `WaterMains` decide the shutoff on their own clock
    ([PowerGrid.cs](../Assets/Scripts/Core/PowerGrid.cs#L70)). If every client has the host's shutoff day, they agree;
    if not, one player's lights stay on. Barricade plank adds and damage should be checked the same way
    ([Barricade.cs](../Assets/Scripts/World/Barricade.cs)).
  - **Do:** Test with a client across the shutoff day and while boarding a window. Send the state if anything differs.
  - **Done when:** Both machines agree after the shutoff and after barricading.

## P3 — nice to have

- [ ] **Flavour interactions** · P3 · S each · Content — failed Night Shift ideas worth a second look: read a letter
  from a bookcase (`LivingReading.cs`, tasks.md line 8428), inspect a clock or a house number for a clue
  (`ClockFixture.cs`, `OutdoorHouseNumber.cs`, 7140, 8211, 5105), repair a picture frame (6653), and work the breaker
  panel quietly (3962). Clues are best tied to rumours and stashes (see [Quests.md](Quests.md)) so they pay off.
  **Done when:** the ones kept do something beyond a toast.
- [ ] **A stable random stream per map feature** · P3 · S · Tech debt — a `MapBuild` helper in
  [WorldStore.cs](../Assets/Scripts/World/Map/WorldStore.cs) that hands out a deterministic `System.Random` for any
  feature key (failed, tasks.md line 4696). Neighbouring pieces already have to agree, so a single helper prevents
  drift. **Done when:** passes stop seeding `System.Random` by hand.
- [ ] **Weather work only when the weather changes** · P3 · S · Perf — `WeatherSystem.Update` blends and re-applies fog
  and rain every frame ([WeatherSystem.cs](../Assets/Scripts/Core/WeatherSystem.cs#L110)). Skip the apply while the
  values are steady. **Done when:** a still night costs nothing in `WeatherSystem`.
- [ ] **Comments explaining `BuildingPass.Materials.cs`** · P3 · S · Docs — failed (tasks.md line 10582).
