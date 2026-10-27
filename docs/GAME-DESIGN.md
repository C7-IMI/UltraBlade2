# UltraBlade 2 - HyperBlade fidelity design

This is the design and fidelity mapping for the playable slice after the 1996 HyperBlade research pass. The
reference facts and sources are in [HYPERBLADE-REFERENCE.md](./HYPERBLADE-REFERENCE.md); the original data-derived
team and drome catalogs are generated into `TeamCatalog.cs` and `ArenaCatalog.cs`.

## The sport

- Two teams of **2 forwards + 1 AI goalie**, with a four-skater/one-goalie roster of five; two bench forwards are
  available with the **1/2 substitution keys**.
- One **rok** (the ball/head) is carried on the jak and thrown into a small goal cut into the **elliptical drome**
  wall at each long end. A normal goal is **1 point**; scoring through the **multiplier** is **2 or 4 points**.
  The multiplier can be nullified by the opposing team carrying the rok back through the gate.
- A match is **3 periods** (the original help file defaults to 5:00; the code supports 3/5/7 minute options; this
  slice uses 5:00 for a real match and a short 20 s period for the headless gate).
- No referees, no penalties, no offside, no overtime; ties are possible. The slice tracks **injuries** and
  **fatalities** and keeps downed dromers out until the next goal or period, approximated for a bench that is not
  visually fielded.
- **Gauntlet**: the single-player ladder. The player's club faces the other 13 clubs; a win advances, a loss or
  tie restarts the run, and progress saves between matches. This replaces the earlier four-round season.

## Drome

- Runtime-built **elliptical bowl** from `DromeBuilder.cs`: an ellipse floor (72 segments x 9 rings), a curved wall
  band that leans inward to a lip, small goals cut into the wall at the long ends with recessed cases and nets,
  painted radial/concentric livery (central red star, concentric arcs, end fields, creases), and the drome props
  from the original `DATA/*.ENV` files.
- Proportions follow the help-file dimensions (220 x 135 x 70 ft) mapped to a 48 x 29.5 unit ellipse; the catalog
  scales the original environment coordinates by 0.10 (x) and 0.1273 (z).
- The camera is a low continuous third-person chase with no broadcast cuts or celebrations, matching the capture
  evidence.

## Controls (mapped from the original DATA/BOOIE.INI + DFTKEYBD.CFG)

| Action | Original | UltraBlade 2 |
| --- | --- | --- |
| Move | arrows | arrows (and WASD) |
| Attack / slash | S | S or right mouse |
| Speedburst | D | D or Left Shift |
| Power shot (hold) | Space | Space (hold/release) |
| Quick shot | F | F |
| Pass / call for pass | C | C |
| Jump | E | E |
| Substitute bench | 1/2 | 1/2 |
| Pause | P/Esc | P/Esc |

## Teams and dromes (14, generated from the original DATA files)

Chicago Machine (The Machine Shop), Houston Lone Stars (SuperDrome), Las Vegas Black Jack (The Jakpot), Los
Angeles Shock Wave (The Pit), Seattle Fury (The Mountain of Pain), Toronto Crusade (Drome of Death), Leeds Knights
(The Rook), Moscow T-34s (The Red Sphere), Munich Sturmnacht (The Grosse Haus), New York Glory (The Zoo), Tokyo
Ronin (ItamiDrome: House of Pain), Warsaw Ambush (The Slaughter House), plus the shipped hidden clubs Jamaica
DredNoks and BLAM! Nemesis. Each has its five-man roster, per-difficulty ratings, home/away kit colours and its own
drome prop layout.

## HUD (help-file terminology)

Team scores, PERIOD n/3 and timer top-centre with three period pips; roster down the left with four names and
segmented health bars (yellow = controlled, green healthy, red low, bench dimmed); speedburst and shot-power meters
bottom-left; shot-cam and rear-view framed viewports top corners; elliptical radar bottom-right with rok, teammates,
opponents, goals and a goal pointer; ROK carrier line; floating banner for GOAL/SAVED/MULTIPLIER/KICKOFF and the last
event-feed lines. The result screen shows the original dromer columns (PTS FGM FGA FG% 2X 4X POSS INT MULT INJ FAT)
and goalie stats (SHT SAV ERR %).

## Drome objects implemented

Ramps, multiplier gates, turnstiles, hurdles, laser hurdles, limbo poles, green/red randomizer flags and armory
crates are built from the per-drome layouts. Multiplier gates affect the multiplier; turnstiles/hurdles/lasers damage
and stun skaters; ramps/armories/randomizers are present as props (their deeper power-up effects are approximated).
Killball, mines and swarfs are not yet simulated; they are listed as future work.

## Build and verification modes

- Default player launch: HyperBlade-fidelity Gauntlet/manu with the current matchup shown in the menu.
- --demo: preserves the legacy port contract (three training crystals, `GYPSY_DEMO_OK collected=3`, hero capture
  evidence) so the earlier acceptance gate stays green.
- --match: headless 3 x 20 s autoplay league match; writes `UBLADE2_MATCH_OK home=<n> away=<n> winner=<n>` and a v2
  match evidence JSON.
- --match-capture, --menu-capture, --ui-capture: windowed 1280x720 captures with the HyperBlade HUD/screens.
- --league-test: Gauntlet PlayerPrefs save/load proof.

## Honest limits

Original assets (textures, models, audio, FMV, code) are not shipped; this is a source-data-driven homage. Bench
fatigue/death bookkeeping, the full killball/swarf/mine weapon economy, wall-riding trick repertoire, network play
and the rendered crowd/exterior/story presentation are approximated or absent. Goal-mouth geometry is derived from
the manual's 10-ft goal and the elliptical-dome description, not from a legible original frame.
