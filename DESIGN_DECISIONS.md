# CLONE-Z — Design decisions

The mechanics breakdown (*Survival Shooter Mechanics Breakdown*, Oct 2026) says to "answer tensions 1, 2, 5 and 10 in writing" before building. This file answers all twelve. It also records the three design rules, the rights rule and the blended core loop as CLONE-Z implements them. See [MECHANICS_COVERAGE.md](MECHANICS_COVERAGE.md) for where each mechanic lives in the code.

## The premise and the three rules

The Zones are alien leftovers that don't care about you. People go in because they need the money. The rent, the debt and the family are in the [Upkeep](Assets/Scripts/Hub/Screens/HomeScreens.cs) screen.

1. **The Zone is indifferent, not evil.** Anomalies follow fixed rules per type ([AnomalyCatalog](Assets/Scripts/Zone/Anomalies.cs)). Bolts trigger them first. Emissions give two minutes' warning. Hazards can be learned, and deaths should read as mistakes.
2. **Knowledge is the real loot.** The account keeps map marks, anomaly rules and identified artifact properties through deaths and wipes (`AccountProfile.Knowledge`, `ArtifactKnowledge`). Rumours, dead stalkers' PDAs and stash locations are sold or looted as information.
3. **Every trip costs something.** Rent comes due every day. Going in costs a cordon pass or a helicopter seat. Insurance has a premium. Gear wears out, and the Zone leaves marks on the body that outlive death ([ZoneExposure](Assets/Scripts/Zone/ZoneHazards.cs)).

## Rights

Roadside Picnic supplies the premise and structure only. Its title, character names, place names and object names are not used. CLONE-Z invents its own: Zones such as *Kestrel* and *the Spillway*, the *Passing*, the *Lodestar* (standing in for the wish-granting sphere), the *Sunken Star*, factions such as *Freehold*, *Vanguard*, *the Lantern Syndicate*, *the Wardens*, *the Hollow Choir* and *the Institute*, and the hub *the Kettle*. Check with a lawyer before shipping anything commercial. This file isn't legal advice.

## The twelve tensions

| # | Tension | CLONE-Z's answer |
| --- | --- | --- |
| 1 | Instanced raids or a persistent world? | **Hybrid.** A persistent hub (the bar: stash, hideout, traders, upkeep) plus raid instances into Zones. Some threads persist per Zone: A-Life squads, emptied open-zone containers, buried stashes, outpost owners, corpses waiting under corpse recovery, your **camp** and your **cars**. The Open Zone run has no clock, which is GZW's middle ground. |
| 2 | What does death take? | **"You lose what you carried — never what you learned, never your pouch."** The character survives by default. Ironman and server permadeath are opt-in. Insurance returns unlooted pieces, and corpse recovery (GZW) is a sandbox option. Scav and hunter runs use a borrowed body, so their deaths cost the runner nothing. |
| 3 | Survival needs vs. session length | Drain rates are auto-tuned to raid length (`SandboxSettings.AutoTuneDrainToRaid`), with hunger and thirst sliders on top. Needs bite in long Open Zone runs and stay background noise in short ones. |
| 4 | How many deep systems? | Medicine (limbs, blood, shock), ballistics (armour classes) and the Zone (anomalies, artifacts) are deep by default. Medicine, ballistics and diseases each have a sandbox switch for new players, and the inventory has a simple list view beside the grid. |
| 5 | PvP-led or PvE-led? | **PvE-led, PvP by server rule.** The AI is the content: A-Life factions, the dead, mutants, wildlife. PvP, friendly fire and hunters are server toggles. The netcode is built for PvP anyway: server authority, lag-compensated hits and line-of-sight culling. |
| 6 | Zombies, mutants or humans? | **The dead are the main pressure.** They are noise-driven and come in hordes. Mutants are the secondary threat, each a puzzle (the Shade cloaks, the Puppeteer drives the dead, the Thumper hits through cover). Humans fight from cover. The [ThreatDirector](Assets/Scripts/AI/Creatures/Creatures.cs) favours humans by day and mutants by night, as in Metro Exodus. |
| 7 | Handcrafted or systemic? | **Systemic plus landmarks.** A-Life, anomalies, hordes, emissions and the Central Economy carry the hours. A few handcrafted anchors (landmarks, the Lodestar questline, the cemeteries' uncanny dead) give the Zones identity. |
| 8 | Player market or closed economy? | **Traders first, a gated market second.** The trader economy (loyalty, barters) is the backbone. The flea market is level-gated, accepts found-in-raid goods only, charges listing fees and can be switched off per server. |
| 9 | Wipes or permanence? | **Optional seasons.** `SeasonLengthDays` sets the season length (0 = never). A wipe resets characters, stash, outposts, camps and cars, but keeps account knowledge. |
| 10 | Which camera where? | All three views (and bodycam) in PvE and co-op. PvP raids can be forced to first person (`FirstPersonOnlyInPvp`). The server only sends what a character's head can see, which stops third-person peeking and isometric wallhacks. The same line of sight drives the isometric fog of war. |
| 11 | Monsters played by people? | **A later-mode toggle, built.** Hunter runs (Shade, Hound or Puppeteer) join the host's raid on servers that allow them. A hunter uses a borrowed body and is paid per runner taken. |
| 12 | Realism vs. readability | **An accessibility floor.** Brightness, motion blur, lens distortion, camera shake, FOV, HUD style (full, minimal, diegetic, hardcore), crosshair, subtitles and fog readability are all adjustable. |

## The blended loop

It follows the doc's proposal: Tarkov's raid-and-extract skeleton, STALKER's Zone and Metro's pressure inside each run, and Zomboid's skills carrying over.

```
the bar (hub) ── gear up, jobs, rent ──► the door: Zone + how you go in (on foot past the cordon / helicopter LZ)
      ▲                                    and as whom (runner, scav, Open Zone, hunter)
      │                                                     │
  sell, repair,                                   loot · anomalies + bolts · artifacts
  upgrade, craft,                                 A-Life · the dead · mutants · emissions
  pay upkeep                                      camps · cars · drones · other runners
      │                                                     │
      └──── extract (conditional exits, a flare for the helicopter) ◄── or die: lose what you carried,
                                                                        keep skills, pouch and knowledge
```

## Networking model

* **Stack:** Netcode for GameObjects with Unity Transport, built at runtime. Game state travels as named messages, with no networked prefabs.
* **Authority:** The host's machine runs the Zone (AI, emissions, helicopter, A-Life, outposts, explosions, economy restocks). When the host goes in, the raid opens and anyone in their own hub can follow. The host can't leave while others are still inside.
* **Visibility:** Bodies are forwarded only to clients whose eye has line of sight to them, or who are very close or in the same squad. AI goes out at 10 Hz within 85 m with the same LOS rule.
* **Hits:** Clients send shots, and the server resolves them against remote bodies rewound to the shooter's view time.
