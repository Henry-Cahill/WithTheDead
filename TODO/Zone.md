# Zone — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** the raid-and-extract loop. That's the Zones and their hazards (anomalies, bolts, artifacts, detectors,
emissions, radiation), the cordon, A-Life and factions, outposts, mutants and wildlife, extraction and raid kinds
(runner, scav, open Zone, hunter, helicopter), and the hub's economy (traders, flea market, hideout, insurance, upkeep,
wipes).
**Code:** [Assets/Scripts/Zone](../Assets/Scripts/Zone), [World/Zone](../Assets/Scripts/World/Zone),
[AI/Humans](../Assets/Scripts/AI/Humans), [AI/Creatures](../Assets/Scripts/AI/Creatures),
[Assets/Scripts/Extraction](../Assets/Scripts/Extraction), [Assets/Scripts/Economy](../Assets/Scripts/Economy),
[Assets/Scripts/Hub](../Assets/Scripts/Hub), [Assets/Scripts/Profile](../Assets/Scripts/Profile). The design is in
[DESIGN_DECISIONS.md](../DESIGN_DECISIONS.md), and where each mechanic lives is in [MECHANICS_COVERAGE.md](../MECHANICS_COVERAGE.md).

## Where it stands

Every mechanic in the design has code. The content behind it is thin:

| | Count | Where |
|---|---|---|
| Zones | Kestrel Flats, Mirelock Woods, the Spillway, and statewide Kentucky (featured: Bardstown) | [ZoneCatalog.cs](../Assets/Scripts/Zone/ZoneCatalog.cs#L54) |
| Anomaly types | 5: sinkhole, embermoss, staticbloom, greysyrup, tollgate | [Anomalies.cs](../Assets/Scripts/Zone/Anomalies.cs) |
| Artifacts | 11 | [ArtifactCatalog.cs](../Assets/Scripts/Zone/ArtifactCatalog.cs#L63) |
| Mutants / wildlife | 4 (hound, shade, puppeteer, thumper) / 3 (wolf, bear, deer) | [CreatureCatalog.cs](../Assets/Scripts/AI/Creatures/CreatureCatalog.cs#L65) |
| Human factions | 7: Freehold, Vanguard and the Lantern Syndicate (joinable); Wardens, scavs, the Institute and the Hollow Choir | [Factions.cs](../Assets/Scripts/Core/Factions.cs#L15) |
| Traders | 6: barkeep, fence, Institute, gunsmith, medic, guide | [TraderCatalog.cs](../Assets/Scripts/Economy/TraderCatalog.cs#L47) |
| Hideout modules | 11 | [Hideout.cs](../Assets/Scripts/Economy/Hideout.cs#L50) |

Upkeep is 1,400 rent a week and 600 for the family every three days, with debt growing 1% a day
([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L15)). Wipes (`ProfileStore.Wipe`,
[ProfileStore.cs](../Assets/Scripts/Profile/ProfileStore.cs#L279)) keep account knowledge.

## P1 — next up

- [ ] **Make Kentucky a Zone, not just a map** · P1 · L · Content
  - **Why:** Walking out of the Kettle puts runners in Bardstown, so that's where most raids happen. But the Kentucky
    Zone has no landmarks ("Zone overlay for Kentucky: 0 landmarks, 0 trees" in the editor log; the trees come from
    the map's own vegetation pass), and only what the generic overlay gives it ("93 anomalies in 52 fields, radiation
    spots 1"). Its A-Life has no faction mix of its own and silently borrows Kestrel's
    ([ALife.cs](../Assets/Scripts/AI/Humans/ALife.cs#L48), fallback at [L153](../Assets/Scripts/AI/Humans/ALife.cs#L153)).
    No quests are set there either (see [Quests.md](Quests.md)).
  - **Do:** Give Kentucky an identity from its real places. Landmarks come from OSM features (the courthouse square,
    distilleries, the hospital, the rail yard). Place anomaly fields by land use (industrial ground and the rail yard
    hotter than farmland), and put a cordon checkpoint on the roads out of town. Add mutant lairs in the woods, a
    `bardstown` faction mix, and A-Life routes along real roads between those places.
  - **Done when:** The Kentucky Zone logs its own landmarks and faction mix, and a raid there has places worth walking
    to.

- [ ] **Check the upkeep against what a beginner earns** · P1 · M · Balance
  - **Why:** Rent and family come to about 2,800 a week, and missed payments go on a debt that compounds daily
    ([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L15), [L27](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L27)).
    Nobody has checked that against what a new runner sells per raid ([Trading.cs](../Assets/Scripts/Economy/Trading.cs))
    and how often they die. "Every trip costs something" (rule 3) shouldn't become a debt spiral in the first hour,
    especially for friends trying the game.
  - **Do:** Write a small simulation or spreadsheet: beginner raid value by Zone, death rate, insurance returns, scav
    runs as a free income floor. Then tune rent, family cost and interest, or add a grace period for new runners.
  - **Done when:** A careful beginner breaks even in about three raids a week, and the numbers are written down in
    DESIGN_DECISIONS.md.

- [ ] **Keep the hand-made Zones healthy** · P1 · M · QA
  - **Why:** Since the Kettle moved to Bardstown, Kestrel Flats, Mirelock Woods and the Spillway are reached only
    through the door's **Other ways in**. Every authored quest still happens there. The solo smoke test deploys into
    Kestrel by default (`-clonez-smoke-zone` picks another), so Mirelock and the Spillway can break unnoticed.
  - **Do:** Run the solo smoke test against all four Zones (a loop in [Smoke.ps1](../Tools/Smoke/Smoke.ps1)), and fix
    whatever fails.
  - **Done when:** `Smoke.ps1` has a mode that runs every Zone, and all pass.

## P2 — soon

- [ ] **More anomaly types and artifacts** · P2 · L · Content
  - **Why:** 5 anomaly types and 11 artifacts carry rule 2, "knowledge is the real loot". Players will have learned
    them all within a few evenings.
  - **Do:** Add 3 or 4 anomaly types, each with a fixed rule, a bolt reaction, a detector signature and a tell you can
    learn (rule 1: deaths should read as mistakes). Add 10 to 15 artifacts with Zone-specific tables, so where you go
    decides what you can find. Keep the hidden-property identification loop for each.
  - **Done when:** Each Zone has at least one anomaly type and three artifacts found nowhere else.

- [ ] **More mutants, each a puzzle** · P2 · L · Content
  - **Why:** Four mutants (hound, shade, puppeteer, thumper). The design wants each to be a puzzle with its own counter,
    and the `ThreatDirector` hands the night to mutants, which is a lot of nights for four kinds.
  - **Do:** Add 2 or 3: something that hunts by sound only (punishes running and shooting), something that nests in
    buildings (ties to Kentucky's interiors), and an emission-born thing that appears only after a blowout.
  - **Done when:** Each new mutant has a counter a player can learn and a drop worth hunting.

- [ ] **Trader stock that moves** · P2 · M · Content
  - **Why:** Each trader's offers come from a fixed catalogue
    ([TraderCatalog.cs](../Assets/Scripts/Economy/TraderCatalog.cs#L131)), so buying looks the same every day. The
    Central Economy ([CentralEconomy.cs](../Assets/Scripts/Economy/CentralEconomy.cs)) restocks loot with nominal and
    minimum counts, and the shelves could follow the same idea.
  - **Do:** Restock traders on a timer from a pool by loyalty level. Let scarcity follow the world (no medicine after
    the clinic's convoy was hit), and let faction standing change prices.
  - **Done when:** Two days at the Kettle show different shelves.

- [ ] **Playtest scav runs and hunter runs with people** · P2 · M · QA
  - **Why:** Both loops exist, and hunters have a smoke mode (`hunter`). But nobody has played them for an evening:
    scav cooldown and kit, fence reputation, a hunter's reward per runner taken, and how it feels to be hunted.
  - **Do:** Run a session with friends on the co-op server with hunters allowed. Note what's unclear or unfair, and file
    the findings here.
  - **Done when:** Each mode has been played for an evening and has a short list of fixes.

- [ ] **Check that the Zone's state is the host's in co-op** · P2 · L · Co-op
  - **Why:** Outposts change through the server (`NetWorld.RequestOutpost`). It isn't checked whether artifact pickups,
    anomaly triggers, emission timing, A-Life outcomes and extraction state are equally host-owned, or whether a late
    joiner gets them.
  - **Do:** List each shared mutation, test it with two players plus a late joiner, and route any that aren't through
    the server.
  - **Done when:** Two players never both pick up the same artifact, and a late joiner sees emitted anomalies and taken
    outposts.

## P3 — nice to have

- [ ] **Hideout modules that do more than one thing** · P3 · M · Gap — 11 modules
  ([Hideout.cs](../Assets/Scripts/Economy/Hideout.cs#L50)), most with one passive effect (faster insurance, more stash,
  faster recovery). Give each a second use or a choice, for example the generator powering a radio that picks up
  rumours. **Done when:** every module has something to decide about.
- [ ] **Wildlife that behaves like wildlife** · P3 · M · Gap — wolves hunt in packs, deer flee and can be hunted for
  meat and hides (a use for the Trapping and cooking loop), and bears defend a territory. **Done when:** each animal has
  a behaviour a player can read and use.
- [ ] **A cordon with more than a toll** · P3 · M · Gap — the cordon offers a pass, a bribe or a risk, plus disabling
  searchlights with a crowbar ([ZoneCordon.cs](../Assets/Scripts/World/Zone/ZoneCordon.cs#L397)). Add papers that can
  be forged, Warden patrols that remember faces (Heat), and a smuggler's route that costs a favour. **Done when:**
  getting in has three real options with different costs.
- [ ] **Factions present everywhere they belong** · P3 · M · Content — Kestrel has 5 factions in its mix and Mirelock 6
  ([ALife.cs](../Assets/Scripts/AI/Humans/ALife.cs#L50)). Each Zone could use one faction it's known for, with that
  faction's own jobs. **Done when:** each Zone has a signature faction.

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text):

- [ ] Comments explaining `KettleLobby.cs` (tasks.md line 12643, still open).
- [ ] `/// <summary>` docs for `Outposts.cs` (line 45) and `ZoneOverlays.cs` (line 7025): failed.
- [ ] Cache `transform` in `HumanNpc` and `HumanNpcAvatar` (line 3651): failed. Profile first; it's probably not worth
  it.
- [ ] A rust-stained valve on the tanks in `ZoneOverlays.cs` (line 9860): failed. If redone, build it into the tank's
  RaidMesh, never as a `CreatePrimitive` child.

## See also

- The Lodestar basin at the bottom of the Spillway and the wish: [Quests.md](Quests.md).
- Helicopter insertion and extraction in co-op: [Vehicles.md](Vehicles.md).
