# UltraBlade 2 - playable slice design

This is the recorded design for the playable production slice that follows the Unity port's three-collectible
contract. It is a real, playable game loop, not a mockup: menu, league matches against an AI, scoring, combat,
audio, settings, pause, results and a season, built at runtime by unity/UBlade2/Assets/Scripts/UBlade2Game.cs and
its gameplay/UI/audio/input modules.

## Loop

1. Main menu - start the next league match, open the league table, open settings, quit.
2. League match - the Wayfarers face the next league opponent in that opponent's arena.
3. Match - 120 seconds, first to 5 points. Three crystals spawn in the arena and respawn 2.5 s after a collect.
   Collect a crystal to score. Strike the opponent (or get struck) to stun and knock back for 1.1 s.
4. Result - win/lose/draw, final score, league update; continue to the next match, rematch or return to the menu.
5. Season - four rounds; win = 3 points. After the final round the league table is complete and a new season
   can be started.

## Teams and arenas

| Round | Opponent | Difficulty | Arena |
| --- | --- | --- | --- |
| 1 | Razorwings | 1 | Neon Rink |
| 2 | Ironclads | 2 | Sky Deck |
| 3 | Nightfall | 2 | Foundry |
| 4 | Void Syndicate | 3 | Void Dome |

The arenas are runtime-built variants (floor colour, platform layout, accent lighting and crystal positions) rather
than imported art; importing arena art is a later production task.

## Controls

| Action | Windows | Android |
| --- | --- | --- |
| Move | WASD or arrow keys | left-half drag = virtual joystick |
| Strike | Space or left mouse | right virtual button (upper) |
| Dash | Left Shift | right virtual button (lower) |
| Pause | Escape or P | pause state in the UI |

Gamepad-specific mapping is not implemented; menus and HUD are mouse/touch driven through IMGUI.

## Settings and persistence

- Settings: master/music/SFX volume, difficulty, touch controls, fullscreen. Saved to PlayerPrefs under the
  ublade2.settings. prefix.
- League season: saved under ublade2.league.v1; see docs/ROLLBACK.md for the expand-only rule and reset path.
- The game is offline only; no accounts, network, telemetry or external API.

## Audio

Procedural audio only: generated tones/noise for collect, hit, whistle, click, stun, dash, victory and defeat, plus a
short looping music bed per arena. There are no imported audio assets and no third-party licences involved.

## Build and verification modes

- Default player launch (Windows or Android): interactive menu.
- --demo: preserves the original port contract - autopilot collects exactly three crystals and prints
  GYPSY_DEMO_OK collected=3, with the hero runtime evidence and 1280x720 capture path unchanged.
- --match: headless deterministic 20-second match to 2 points, autopilot on both sides, prints
  UBLADE2_MATCH_OK player=<n> ai=<n> winner=<w> frames=<n>; evidence in artifacts/ublade2-pro/.
- --match-capture=<png>: windowed match with HUD, captures artifacts/ublade2-pro/match.png, prints
  UBLADE2_MATCH_CAPTURE_OK.
- --menu-capture=<png>: windowed main menu capture, prints UBLADE2_MENU_CAPTURE_OK.
- Android intents: --ez ublade2_demo true runs the legacy demo; --ez ublade2_capture true starts an autoplay
  match, writes an in-app framebuffer PNG to Application.persistentDataPath, prints
  UBLADE2_MOBILE_CAPTURE_OK path=... and keeps running so adb can pull it.

## What this slice deliberately does not include

Online play, a full roster/league of many teams, imported arena or character art beyond the Wayfarer hero, licensed
music/commentary, a gamepad mapping screen, and a store/installer pipeline. Those are recorded in DEFERRED.md where
they still apply.

## Related records

- docs/UI-STANDARD.md - the game UI/UX standard.
- docs/ROLLBACK.md - settings/save rollback.
- docs/RELEASE.md - packaging and identity limits.
- docs/UNITY-PORT.md - the port acceptance record and the original PC/mobile gates.
