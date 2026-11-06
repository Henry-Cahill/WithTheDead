# Zombies — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** the dead's AI and senses, hordes, spawning and population, attacks on people and buildings, infection,
corpses, Wave Survival's waves, the dead in co-op, and their sound.
**Code:** [Assets/Scripts/Zombies](../Assets/Scripts/Zombies) ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs),
[ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs)), [NoiseSystem.cs](../Assets/Scripts/Core/NoiseSystem.cs),
[ZombieVisibility.cs](../Assets/Scripts/Rendering/ZombieVisibility.cs), [HumanoidBodyBuilder.cs](../Assets/Scripts/Characters/HumanoidBodyBuilder.cs),
[ZombieVoices.cs](../Assets/Scripts/Audio/ZombieVoices.cs), `SendAi` in [NetWorld.cs](../Assets/Scripts/Net/NetWorld.cs#L109).

## Where it stands

- Four kinds: shambler, fast shambler, sprinter and crawler ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs#L8)).
  They have sight and hearing, chase, bite and scratch, and attack barricades, built pieces, raid doors and interior
  walls on the way to their prey ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs#L467)).
- Raid population is 40 × the Zone's density × the sandbox's **Zombie population**
  ([GameBootstrapper.cs](../Assets/Scripts/Bootstrap/GameBootstrapper.cs#L464)). On the streamed map, the dead far
  from every survivor are let go and replacements are spawned nearer
  ([ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs#L292)).
- Wave Survival: 6 dead plus 3 per wave, tougher ones from wave 2, sprinters from wave 4
  ([CloneZConfig.cs](../Assets/Scripts/Core/CloneZConfig.cs#L135)), at most 45 alive at once
  ([ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs#L25)).
- Corpses carry loot for 150 seconds ([CloneZConfig.cs](../Assets/Scripts/Core/CloneZConfig.cs#L257)). The Puppeteer
  mutant can drive the dead. In co-op, the host sends the dead at 10 Hz within 85 m to clients that can see them.

## P1 — next up

- [ ] **Make "Sprinter share" and "The dead return" do something** · P1 · S · Bug
  - **Why:** Both are on the sandbox screen ([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L320)) and
    saved ([SandboxSettings.cs](../Assets/Scripts/Core/SandboxSettings.cs#L22)), but no game code reads them. The
    sprinter roll is a fixed `r < 0.12f` ([ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs#L142)), the
    setting's default value hard-coded. The dead always come back. **Zombie population** does work.
  - **Do:** Use `SprinterShare` in the type roll, for raids and waves (with the wave unlock still applying). Honour
    `ZombieRespawn`: with it off, a Zone, or a streamed piece, stops topping up the dead it has lost.
  - **Done when:** Sprinter share at 0 gives no sprinters and at 1 gives only sprinters, and with "The dead return" off
    a cleared street stays clear.

- [ ] **Hordes that move as hordes** · P1 · L · Gap
  - **Why:** The design says "the dead are the main pressure… noise-driven and come in hordes" (DESIGN_DECISIONS.md,
    tension 6). Today each zombie on its own goes for the loudest noise it can hear. A noise pulse lasts 0.4 s
    ([NoiseSystem.cs](../Assets/Scripts/Core/NoiseSystem.cs#L21)), and a helicopter flyover pulls individuals rather
    than a crowd. There's no horde that drifts across the map, and none that follows a car.
  - **Do:** Add horde groups: a leader with a migration target, followers that steer with it, and a group size that
    grows as it passes stragglers. Keep a horde's position and count when its piece is let go, so it can arrive later.
    Make big noises (a helicopter, a gunfight, a car alarm) re-target whole groups.
  - **Done when:** A helicopter flyover visibly pulls a horde along its path, and the horde is still out there later.

- [ ] **Let the dead remember a cleared street** · P1 · M · Save
  - **Why:** The dead far from every survivor are let go, and replacements spawn later near the survivors
    ([ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs#L292)). So a street the squad cleared fills again,
    and a horde they lured away is simply gone. Containers already remember being emptied per piece; the dead should
    too.
  - **Do:** Keep a per-piece count of the dead (alive and killed, plus hordes passing through), and rebuild a piece's
    dead from that, with respawn per the sandbox rule above.
  - **Done when:** In the streaming smoke run, a block cleared, left and returned to has the same dead count, or fewer
    if respawn is off.

- [ ] **Teach the dead fences and windows** · P1 · L · Gap
  - **Why:** The dead know raid doors, wall partitions, barricades and built pieces
    ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs#L467)), but nothing about windows or fences: no code in
    `Assets/Scripts/Zombies` mentions either. In Project Zomboid the dead break windows, climb through them and vault
    low fences. Here a window is a wall to them, which makes ground-floor windows and garden fences unreasonably safe.
  - **Do:** Let the dead break and climb through `HomeWindow` and `RaidWindow` (boarded ones get beaten on like
    barricades), and vault low fences with a slow animation. Crawlers can squeeze under, and sprinters vault faster.
  - **Done when:** A survivor behind an unboarded window isn't safe, and a boarded one buys time.

- [ ] **AI that costs less far away** · P1 · M · Perf
  - **Why:** Every zombie runs `Update` every frame ([ZombieAI.cs](../Assets/Scripts/Zombies/ZombieAI.cs#L144)) and
    looks for prey every 0.3 to 0.5 s with line casts. Rendering visibility is budgeted (24 checks a frame), but the AI
    isn't. Bigger hordes (above) multiply it.
  - **Do:** Tick the dead at a rate by distance and state: full rate near the survivors, 2 to 5 Hz far away, and asleep
    when idle and out of earshot. Budget the line casts per frame as `ZombieVisibility` does. Measure with 200 dead.
  - **Done when:** 200 dead in a streamed town cost under an agreed budget (say 2 ms a frame) on the dev PC.

## P2 — soon

- [ ] **Check: does a survivor who dies of infection rise?** · P2 · M · Gap
  - **Why:** With lethal infection, the survivor dies "turned into one of them"
    ([PlayerStats.cs](../Assets/Scripts/Player/PlayerStats.cs#L215)), but no zombie seems to rise from the body.
    Project Zomboid's most memorable moment is meeting yourself, in your gear. In co-op it means a friend has to put you
    down.
  - **Do:** If nothing rises, spawn one of the dead at the body, with the survivor's look (`HumanoidLook`) and their
    carried loot on it.
  - **Done when:** Dying of infection leaves a zombie in the survivor's clothes, carrying their kit.

- [ ] **Hearing through walls** · P2 · M · Gap
  - **Why:** Hearing is distance only ([NoiseSystem.cs](../Assets/Scripts/Core/NoiseSystem.cs#L24)). Walls and floors
    don't dampen it, so sneaking indoors buys nothing, and a gunshot in a basement carries as far as one in the street.
    The audio side already muffles through walls (`SfxAcoustics`).
  - **Do:** Apply the same occlusion (a cheap line cast through walls) to the radius a pulse reaches the dead with.
  - **Done when:** The same shot draws noticeably fewer dead from inside a closed house.

- [ ] **Waves that get harder, not just longer** · P2 · S · Balance
  - **Why:** Waves grow by 3 dead each ([CloneZConfig.cs](../Assets/Scripts/Core/CloneZConfig.cs#L136)), but at most 45
    are alive at once ([ZombieSpawner.cs](../Assets/Scripts/Zombies/ZombieSpawner.cs#L25)). Wave 14 is exactly 45; from
    wave 15 on, a wave just trickles in for longer.
  - **Do:** Escalate through the mix (more sprinters, tougher dead, special dead), several spawn directions at once,
    and pressure on barricades. Maybe raise the cap with the AI work above. Add a difficulty setting.
  - **Done when:** Wave 15 feels harder than wave 10 in a playtest, not just longer.

- [ ] **A few special dead** · P2 · L · Content
  - **Why:** All four kinds are speed and toughness variants. Some counterplay variety would help, without turning the
    dead into mutants (the Zone has those).
  - **Do:** Pick two or three and keep them rare. A *screamer* who calls the dead when it sees you. *Riot-gear dead*
    who need armour-piercing rounds or a blade to the head (ties into the ballistics). The *fresh dead*, who still try
    door handles. Dress the dead by place, too: police at the station, nurses at the clinic.
  - **Done when:** Each new kind has its own counter, and their looks are built through `HumanoidBodyBuilder`, never
    as child objects (those caused floating bits).

- [ ] **Send clients what the dead are doing** · P2 · M · Co-op
  - **Why:** `SendAi` sends position, rotation, alive, hunting and spawn data
    ([NetWorld.cs](../Assets/Scripts/Net/NetWorld.cs#L109)), but not attacks, staggers, targets or health. A client
    can see the dead animate wrongly around a fight.
  - **Do:** Send attack, stagger and hit events as they happen (they're rare compared with movement), and send health
    only when it changes.
  - **Done when:** In the host/client smoke test, both sides see the same dead stagger from the same shove.

## P3 — nice to have

- [ ] **Send the dead a client can hear, not just see** · P3 · M · Co-op — beyond about 12 m, a zombie goes to a client
  only if the client can see it or already knows it ([NetWorld.cs](../Assets/Scripts/Net/NetWorld.cs#L109)). The dead
  beating on the door behind you pop in late. Include the dead that are attacking or hunting that client. **Done
  when:** the dead at the door are there when it breaks.
- [ ] **A horde you can hear coming** · P3 · M · Polish — the voices are per zombie
  ([ZombieVoices.cs](../Assets/Scripts/Audio/ZombieVoices.cs)). Add a distant horde bed that grows with the count and
  direction of the dead nearby, so a horde is heard before it's seen. **Done when:** a horde a street away can be told
  from a few strays by ear.
- [ ] **Fill the town at the start** · P3 · S · Bug — the first ambient spawn makes about twice the target in
  attempts, and on awkward ground some fail, so a town can start thin until the top-up loop catches up. Log the
  shortfall and retry the misses. **Done when:** the start log shows the full count.
- [ ] **Corpses that stay** · P3 · M · Save — corpse loot vanishes after 150 s
  ([CloneZConfig.cs](../Assets/Scripts/Core/CloneZConfig.cs#L257)) and corpses aren't saved. That's fine for
  performance, but a battle site with no bodies after a few minutes reads oddly. Keep the bodies of the dead the
  survivor killed for a raid or a day, as cheap static meshes, with their loot on a timer. **Done when:** a fought-over
  street still has bodies when the survivor comes back through.
