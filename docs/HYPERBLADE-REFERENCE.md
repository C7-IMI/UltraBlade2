# HyperBlade (1996) reference and fidelity mapping

This document records what was researched about the original 1996 PC game **HyperBlade** (Wizbang! Software
Productions / Activision) and how UltraBlade 2 maps each finding. Sources are cited at the end. Facts are graded in
the work notes as confirmed (game data/help file), derived (dominant kit colours), or inference (video frames).

## The sport

- HyperBlade is a 21st-century "battlesport" in the **Transnational HyperBlade League (THL)**, set in 2065. It mixes
  hockey, lacrosse, jai-alai and boxing inside a curved **drome**.
- The ball is the **rok**. Players are **dromers**, skate on blades, wear two-tone armour and carry a long curved
  **jak** blade/scoop in the left hand.
- Each side fields **2 forwards + 1 AI goalie**; the roster has 5 players (2 bench forwards). A match is **3 periods**
  (help file default 5:00; options 3/5/7 minutes).
- Goals: **1 point**; a multiplier makes it **2 or 4 points**; scoring with an opponent's decapitated head is a 4x
  bonus. The opposition can carry the rok back through the multiplier to nullify/steal it.
- **No referees, no penalties, no offside, no overtime**; ties are legal. Injuries and fatalities are tracked.
- **Wipe Out Rule**: kill all four opposing forwards and then score to win; if a whole team is killed, the next score
  ends the game with an automatic win.
- Modes: **Exhibition**, **Gauntlet** (13 must-win matches; a loss or tie restarts the run; dead dromers stay dead
  for the run; save between matches), **Training** and **LAN multi-player** (2-4 players). There is no league season.
- Drome objects: **Killball Charger** (limbo pole/gate), **randomizer** flags at the lip, **ramp**, **turnstile**,
  **armory** (mines/swarfs), **multiplier**, **hurdle**, **laser hurdle**, **mine**, **swarf**, **startgun**.

## Drome

- Dimensions given by the original help file: **220 ft long x 135 ft wide x 70 ft deep**; an ellipsoid ("inside of a
  Graf Zeppelin") with curved transitions that go vertical, a grindable lip and roll-out deck. Goals are **10 ft
  across**, one at each end.
- Video evidence: the floor is a continuous painted surface that sweeps upward into a faceted shell; there are **no
  boards, glass, stands, crowd or jumbotron inside the playing volume** in the captured original. The floor livery is
  radial/concentric: a central hub with a rayed sunburst, concentric arcs, and one large saturated end field per
  drome; no hockey centre line or face-off dots. The default 1996 renderer is deliberately **flat-shaded**, untextured
  and ambient-only (Direct3D texture mapping was an optional later path).
- Each team owns a drome; data/*.ENV files carry the props for all 14 dromes. The catalogue here is generated from
  those files.

## Teams

League (game-data names): Chicago Machine, Houston Lone Stars, Las Vegas Black Jack, Los Angeles Shock Wave,
Seattle Fury, Toronto Crusade, Leeds Knights, Moscow T-34s, Munich Sturmnacht, New York Glory, Tokyo Ronin,
Warsaw Ambush, plus the two shipped-but-hidden clubs Jamaica DredNoks and BLAM! Nemesis (Gauntlet final bosses).
Kit colours in TeamCatalog.cs are derived from the dominant colours of the shipped HOME/AWAY BMP textures and the
1996 team pages. The five-man rosters and per-difficulty ratings come from the shipped DATA/*.HTM files.

## Controls (original)

Arrows = accelerate/stop/rotate; **S** attack; **D** speedburst; **Space** power shot (hold to charge) / call for
pass; **C** pass (offense) / change player (defense); **F** quick shot; **E** jump; **1/2** substitute bench slots;
**P/Esc** pause.

UltraBlade 2 maps: arrows or WASD move, **S** attack, **D/Shift** speedburst, **Space** charged power shot, **F**
quick shot, **C** pass, **E** jump, **1/2** substitute, **P/Esc** pause. The AI controls the teammate, opponents and
both goalies.

## HUD (original, from the help file's labelled diagram)

Speedburst indicator top-left; roster down the left (yellow = you/playing, red = others playing, green = bench,
black = dead/injured); team score and health bars bottom-left; period clock with **three period pips** (active red);
radar bottom-right (rok yellow, teammate blue, opponents two reds) with rok-finder line and goal-finder pointer;
shot power meter and a shot-cam picture-in-picture when carrying; rear-view cam inset when chased; floating callouts
for goals, interceptions, multiplier, killball, injuries and fatalities.

UltraBlade 2's HUD implements these five groups with the same terminology (speedburst, roster colours, period pips,
radar/rok finder/goal finder, shot power, shot/rear-view frames) and the same 11-column dromer stat table plus goalie
SHT/SAV/ERR/%.

## Presentation

- Menus in the original are a 3D lobby/locker-room with a translucent cyan **holoover** panel, team comparison bars
  and a Gauntlet warning. Period breaks show a score panel plus a rotating parody sponsor ad (NoBones, SPECSAVERS,
  HUGE!, Hellmet...). The soundtrack is nine techno CD tracks; there are crowd/announcer banks and per-team stings.
- UltraBlade 2's menus/period-break banners use the same fiction and sponsor gags, and the procedural audio keeps
  the techno/crowd motif without copying the original recordings.

## What UltraBlade 2 approximates or does not claim

- It is an original Unity homage, not a byte-identical port: no original textures, models, audio, FMV or code are
  shipped. Dromes are runtime low-poly reconstructions in the original proportions and livery logic.
- Goal-mouth geometry is unverified in the video evidence; the small wall aperture follows the manual's 10-ft goal
  and the elliptical-dome description.
- Bench fatigue/death bookkeeping, the full weapon set (killball, swarf, mines), wall-riding tricks and network play
  are approximated or not implemented; the playable slice models the core sport (rok, goals, multiplier, AI goalie,
  2-vs-2+bench roster, health/jak combat, hazards, three periods, 14 teams, dromes).

## Sources

- Wikipedia: https://en.wikipedia.org/wiki/Hyper-Blade
- Archive.org manual OCR: https://archive.org/details/hyper-blade-manual (retail manual + quick reference card)
- Original game CD data: DATA/*.HTM (teams/ratings), DATA/*.ENV (dromes/props), DATA/BOOIE.INI and DFTKEYBD.CFG
  (controls), DATA/INFOBTXT.LST (HUD strings), GAME.HLP/HELP.HLP, JUMBO.TXT (jumbotron ads), ENUMIDS.H (stats).
- Manual-transcribed help-file facts and team tables in .work/hyperblade-ref/RESEARCH-RULES.md (local work notes).
- Video evidence (operator-supplied):
  - https://www.youtube.com/watch?v=EpScHKYEEpE - Hyperblade PC gameplay, no commentary
  - https://www.youtube.com/watch?v=FNt_1BBHiwE - THL 2066 finals, BLAM! Nemesis vs Tokio Ronins
  - https://www.youtube.com/watch?v=iAJBpyQwRXo - HyperBlade 1996 official trailer
- Visual frame analysis in .work/hyperblade-ref/RESEARCH-VISUAL.md (local work notes; not committed).
