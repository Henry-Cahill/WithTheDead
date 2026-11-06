# Quests — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** story quests, daily contracts, the Lodestar questline, onboarding, the quest UI and quest progress in co-op.
**Code:** [QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs) (quests, contracts, `QuestsScreen`, `HudQuestTracker`),
[HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs) (`LodestarScreen`),
[ServiceScreens.cs](../Assets/Scripts/Hub/Screens/ServiceScreens.cs) (`RumoursScreen`),
[ProfileData.cs](../Assets/Scripts/Profile/ProfileData.cs) (quest and Lodestar state),
[Progression.cs](../Assets/Scripts/Profile/Progression.cs), [NetSocial.cs](../Assets/Scripts/Net/NetSocial.cs) (`ShareQuest`),
[ZoneLandmarks.cs](../Assets/Scripts/World/Zone/ZoneLandmarks.cs).

## Where it stands

- 17 hand-written quests from the six traders, built from six objective kinds: kill, visit, extract, fetch (found in
  raid), key events and extract-with ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L8)).
- Daily contracts generated from the day number, XP and money rewards, a HUD tracker for up to three quests, and
  progress kept in the profile.
- The Lodestar questline reveals where the Lodestar is and stops there: nothing builds the basin and nothing makes the
  wish.
- Every authored quest happens in Kestrel Flats, Mirelock Woods or the Spillway. None happens on the Kentucky map, where
  the Kettle now stands and where a raid starts when you walk out of its door.

## P1 — next up

- [ ] **Finish the Lodestar: build the basin and make the wish** · P1 · L · Gap
  - **Why:** The Lodestar is the story anchor of the design ("a rumoured wish deep in the Spillway, behind a questline
    and clues, with a wish that has a price"), but the questline ends at a hub hand-in. `q_lodestar` only sets
    `SiteRevealed` ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L297)). `ZoneLandmarks.Build` skips
    `lodestar_site` on purpose ([ZoneLandmarks.cs](../Assets/Scripts/World/Zone/ZoneLandmarks.cs#L36)) and nothing else
    uses `WorldMarkers.LodestarSite`, so the basin is never built in any Zone. `LodestarState.WishMade` and `Wish`
    ([ProfileData.cs](../Assets/Scripts/Profile/ProfileData.cs#L285)) are shown on the Lodestar screen
    ([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L459)) but nothing ever sets them.
  - **Do:** Build the basin at the bottom of the Spillway and mark it on the map once it's revealed. Add a last
    objective that sends the runner there. At the basin, offer the wish as a choice with a price, in keeping with rule 1
    (the Zone is indifferent, not evil). Save the choice in the account (it's knowledge, so it outlives a wipe) and let
    the Lodestar screen and the barkeep's rumours ([ServiceScreens.cs](../Assets/Scripts/Hub/Screens/ServiceScreens.cs#L444))
    react to it.
  - **Done when:** A runner who has finished `q_lodestar` can reach the basin, make a wish, pay its price, and still see
    the result on the Lodestar screen after a restart.

- [ ] **Make the first quest work where raids now start** · P1 · S · Bug
  - **Why:** Walking out of the Kettle starts a raid in Bardstown (`ZoneCatalog.FeaturedZone = "bardstown"`,
    [ZoneCatalog.cs](../Assets/Scripts/Zone/ZoneCatalog.cs#L54)). The first story quest, `q_first_steps`, wants
    "Get out of Kestrel Flats alive" (`extract:kestrel`, [QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L54)),
    which only the door's **Other ways in** reaches. A new runner who does the obvious thing finishes a raid and makes no
    progress.
  - **Do:** Either accept an extraction from Bardstown, or change the brief so it explicitly sends the runner to Kestrel
    through the deploy screen and say so on the quest card.
  - **Done when:** A fresh runner can finish `q_first_steps` by the route the README describes, and the lobby smoke run
    ([SmokeTest.cs](../Assets/Scripts/Dev/SmokeTest.cs)) checks it.

- [ ] **Write a Kentucky / Bardstown quest arc** · P1 · M · Content
  - **Why:** The statewide map is the main playground now, but every authored extract and visit objective names
    `kestrel`, `mirelock`, `spillway` or their landmarks ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L50)).
    The contract generator's visit targets are Zone landmarks too: `crashed_helicopter`, `quarantine_tent`,
    `drowned_chapel`, `hunters_hide`, `cooling_tower` ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L171)).
  - **Do:** Use what the Kentucky map already knows. Buildings have typed rooms and loot by use (police station,
    pharmacy, school, clinic, workshop), so quests can say "bring back the pharmacy's ledger", "clear the police station
    armoury" or "reach Louisville road". Add a visit key per building type or OSM id, and give the Kettle's traders
    Bardstown-flavoured jobs.
  - **Done when:** At least five quests (two of them story quests) take place on the Kentucky map, and contracts can
    pick Kentucky targets.

- [ ] **Add a first-raid walkthrough** · P1 · M · Gap
  - **Why:** There is no tutorial. The first quest is an ordinary job, and the README lists about 25 keys. Friends who
    download the game from the co-op page land in a deep game with nothing to steer them. F1 key hints exist, but
    nothing explains the loop: gear up, raid, loot, find an exit, extract, sell, hand in.
  - **Do:** Add a short chain of key objectives that fire from things the game already raises: take the starter kit
    from the stash, walk out of the door, open a container, open the exits list (M), extract, sell something, hand in.
    Each step is a one-line quest card plus a subtitle. Make it skippable, and never show it again once done or
    skipped.
  - **Done when:** A brand-new runner is walked through their first raid without reading the README, and an
    experienced player can skip it in one click.

## P2 — soon

- [ ] **Check every quest target at startup, and tie landmark objectives to their Zone** · P2 · S · Tech debt
  - **Why:** Targets are free strings: item ids, landmark ids, `faction:` and `kind:` prefixes. A rename breaks a quest
    silently. The authored visit objectives also carry no zone
    ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L67), [L91](../Assets/Scripts/Quests/QuestBook.cs#L91)), so a
    matching visit anywhere completes them, while the text says "(Kestrel Flats)" or "(Mirelock Woods)".
  - **Do:** Pass the zone on the authored landmark objectives. Add a validation that runs in development builds and in
    the smoke test: every fetch target is an item in `ItemDatabase`, every visit target is a landmark or building some
    Zone can build, every zone id is in `ZoneCatalog`.
  - **Done when:** The smoke test fails when a quest points at something that doesn't exist.

- [ ] **Share the right progress with the squad** · P2 · M · Co-op
  - **Why:** `NetSocial.ShareQuest` forwards kills, visits, outpost captures and surviving an emission
    ([NetSocial.cs](../Assets/Scripts/Net/NetSocial.cs#L305)), but not extraction or extract-with. A squad that gets out
    together progresses differently. The server also takes the progress key a client sends at face value. That's
    harmless among friends, but it matters before any public server.
  - **Do:** Write down in DESIGN_DECISIONS.md what each objective kind means in a squad, then implement it. Have the
    server derive shared progress from what it saw happen rather than from the client's word.
  - **Done when:** The host/client smoke test extracts together and both runners' quests move.

- [ ] **Give daily contracts more variety** · P2 · M · Content
  - **Why:** Contracts pick one of five templates (kill the dead in a Zone, hand in mutant parts, visit a landmark, kill
    scavengers, extract) with flat rewards ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L150)). After a week
    they repeat.
  - **Do:** Add templates per trader (the medic wants medicine, the fence wants valuables, the Institute wants
    readings). Scale difficulty and reward with level and Zone depth, and add Kentucky-map templates (see the arc
    above).
  - **Done when:** There are at least ten templates, each trader has its own, and rewards scale.

- [ ] **Quest markers and pinning** · P2 · M · Polish
  - **Why:** The HUD tracker shows the first three active quests as text
    ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L475)). In a streamed, statewide map, "Visit the drowned
    chapel" with no direction is a lot to ask.
  - **Do:** Let the runner pin which quests the HUD shows. Put revealed quest locations on the clipboard map
    (`ZoneMapScreen` in [Navigation.cs](../Assets/Scripts/Survival/Navigation.cs)), respecting "no GPS by default": the
    marker appears only once a rumour, PDA or map item reveals it.
  - **Done when:** Pinned quests show on the HUD, and revealed targets are on the map.

## P3 — nice to have

- [ ] **Use the quest state `Ready`, or delete it** · P3 · S · Tech debt — `QuestState` defines `Ready = 2` ("objectives
  done, ready to hand in"), but completion jumps straight to `Done` ([ProfileData.cs](../Assets/Scripts/Profile/ProfileData.cs#L224),
  [QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L285)). A real `Ready` state would let the HUD say "hand in at
  the Kettle". **Done when:** the state is used by the HUD and screen, or removed.
- [ ] **Explain found-in-raid hand-ins** · P3 · S · Polish — fetch hand-ins also take found-in-raid items from the stash
  ([QuestBook.cs](../Assets/Scripts/Quests/QuestBook.cs#L237)). That's reasonable, but the quest card doesn't say so.
  **Done when:** the card says where items are taken from.
- [ ] **Wave Survival milestones** · P3 · M · Gap — wave runs have no objectives and grant no XP
  ([Progression.cs](../Assets/Scripts/Profile/Progression.cs#L30)). Add optional goals (reach wave 10 without losing a
  barricade, survive a night without shooting) and a best-runs table on the save. **Done when:** a wave run shows its
  goals and remembers the best.
- [ ] **Give the quest givers a voice** · P3 · M · Content — every quest comes from one of six trader ids with a
  one-paragraph brief. Add short subtitle dialogue on accept and hand-in, a follow-up for the big quests, and reactions
  to the hidden moral points ([MoralLedger.cs](../Assets/Scripts/Economy/MoralLedger.cs)), e.g. a trader who heard you
  let someone go. **Done when:** the story quests have accept and hand-in lines.
- [ ] **Let Java scripts add quests** · P3 · M · Gap — scripts get `LocationVisited`, `ItemLooted` and `ActorKilled`
  events, but `Game` offers only `toast`, `subtitle`, `log`, `after` and `version`
  ([Game.java](../Java/withthedead.api/withthedead/api/Game.java)), so a script can narrate but not give a job. See the
  Java scripting item in the [index](README.md#cross-cutting). **Done when:** a script can offer, track and complete a
  quest.

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text):

- [ ] Add `/// <summary>` docs to the public members of `QuestBook.cs`: failed on 2026-10-19 (tasks.md line 241).
  Retry with a stronger model or by hand.
