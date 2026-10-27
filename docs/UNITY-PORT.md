# UNITY PORT - Stage B acceptance record

**Status: UltraBlade 2 is a **PC-only playable game** on Unity 6000.6.7f1 with a stamped Windows release. The original
port gates (build, hero, demo, capture, gameplay) still pass, and a playable production slice now ships: main menu,
league matches against an AI, crystal scoring, strikes/stuns, four arenas, procedural audio, settings, pause, results
and a four-round season. Mobile is **parked by operator decision** (2026-10-26); the earlier partial Android evidence
is preserved but is not part of this release and is not claimed complete (UB2-D15). The ProjectGypsy Godot
implementation was neither modified nor deleted; it remains the legacy/reference implementation.**

This is a port with real acceptance evidence, not an engine toggle. Every claim below is attached to a command, a
marker, a measurement or a file hash. What was not verified is named in "Limits".

## 1. Boundaries and environment

| Item | Value |
| --- | --- |
| Source of truth | ProjectGypsy at `C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy` (read-only for this work) |
| Destination | this repository (UltraBlade 2) |
| Unity editor | `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`, version `6000.6.7f1_b22767f04f04` |
| Installed build support | `windowsstandalonesupport`, `WebGLSupport`, `AndroidPlayer` |
| Android Build Support | **installed 2026-10-26** through the Unity CLI (module + SDK/NDK/OpenJDK/command-line tools) |
| Licence | active; proven by a real batchmode Windows player build (see section 5) |
| Gypsy Studio | `Gypsy Studio 0.1.0` at the source path above; imported read-only, never modified |

### Preflight evidence (as instructed)

    python "<GYPSY_HOME>/workstream/gypsy.py" version
    -> Gypsy Studio 0.1.0 (exit 0)
    python "<GYPSY_HOME>/workstream/gypsy.py" check --project "<ULTRABLADE_HOME>"
    -> checks: PASS | capabilities 50 | tools 16 | project UBlade2 (exit 0)
    "<Unity>/Editor/Unity.exe" -version
    -> 6000.6.7f1 (exit 0)
    Editor/Data/PlaybackEngines
    -> windowsstandalonesupport, WebGLSupport
    git status / HEAD
    -> port changes staged and owned by this work; HEAD 3809c2c unchanged; origin/main 3809c2c

The documented path `C:\Program Files\Unity 6000.6.7f1\Editor\Unity.exe` exists and prints `6000.6.7f1`; the
scripts use the Hub path `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`. The only PlaybackEngines
are Windows Standalone and WebGL — Android is absent, so mobile is not in scope and not claimed.

### Source asset inventory (read from GYPSY_HOME, never modified)

| Source | Measured |
| --- | --- |
| Godot game | `game/`: 5 `.tscn` scenes, 7 `.gd` scripts, 25 asset files — reference only |
| Hero FBX | `assets/hero/wayfarer.fbx`, 1,149,484 bytes (23 bones, idle/run/reach, Smile, 512² maps) |
| Hero LOD sources | `wayfarer_lod1.glb` 877,796 bytes, `wayfarer_lod2.glb` 832,252 bytes (converted to FBX) |
| Hero textures | `wayfarer_atlas.png` 339,981, `wayfarer_normal.png` 90,718, `wayfarer_roughmetal.png` 261,620 — all 512x512 |
| Art brief, LOD manifest, validation, reference captures | `assets/hero/ART-BRIEF.md`, `wayfarer_lods.json`, `artifacts/hero/validation.json`, `generation-report.json`, `evidence.json`, `hero-preview-*.png`, `game-exact-*.png` |
| LeftBehind build/demo reference | `unity/LeftBehind` (9 asset files), `toolpacks/leftbehind.unity.json`, `artifacts/leftbehind/build.log` and `frame.png`, `Build/LeftBehind.exe` |

The ProjectGypsy checkout was read, never written: the port imports Gypsy's Python modules with bytecode writing
disabled and runs its own manifests with this repository as the project root (section 6).

## 2. Project manifest

`project.gypsy.json` at the repository root:

    {"engine": {"id": "unity", "version": "6000.6.7f1", "adapter": "unity-6"},
     "targets": ["pc"],
     "paths": {"game": "unity/UBlade2", "artifacts": "artifacts/ublade2"}}

Confirmed:

    python scripts/gypsy_unity.py check
    -> checks: PASS, capabilities 50, tools 16, project UBlade2
    -> UBLADE2_GYPSY_CHECK exit=0

Mobile is deliberately absent from `targets` until Unity Android Build Support is installed and a real APK build is
planned (`DEFERRED.md` UB2-D13).

## 3. Gameplay port

The same three-collectible contract as the reference slice, in `unity/UBlade2/Assets/Scripts/UBlade2Game.cs`:

- a player with a Unity `CharacterController` (height 1.8 m, radius 0.4 m, gravity 9.81 m/s²),
- the same 6 m/s movement speed and the same nearest-crystal autopilot,
- three collectible crystals with collection state (collected at < 1.35 m horizontal distance),
- a follow camera (third-person, 55° field of view, 1280x720 window),
- a deterministic demo path that prints exactly one marker and exits 0, or `GYPSY_DEMO_FAIL` and exits 1.

Measured: the autopilot collects all three crystals on deterministic fixed-timestep frames:

    GYPSY_DEMO_OK collected=3 frames=67

The same run was executed twice (once directly, once through the Gypsy toolpack) with the same marker and exit 0.
Demo log: `artifacts/ublade2/demo.log`; capture log: `artifacts/ublade2/capture.log`.

## 4. Hero port

Sources, all read from ProjectGypsy and copied/converted into this repository:

| Asset | Source | Result |
| --- | --- | --- |
| Wayfarer LOD0 | `assets/hero/wayfarer.fbx` | copied byte-identical (SHA-256 `97e8c7f95ba7faecedddc2fd9bb3238ed0e16251b74076947005bfb032ed6286`) |
| Wayfarer LOD1 | `assets/hero/wayfarer_lod1.glb` | converted to FBX with Blender 5.2.3 (SHA-256 `f31201462e0a318d3ef59ef741eaba2fba64bfc8895f797d834f5ced4671357f`) |
| Wayfarer LOD2 | `assets/hero/wayfarer_lod2.glb` | converted to FBX with Blender 5.2.3 (SHA-256 `857ad41fdb0897da18dda98db1b6ba0a1234600b159be46707a23d69c69924bf`) |
| Textures | `assets/hero/textures/*.png` | copied, 512x512 each (atlas, normal, roughmetal) |

Unity does not import glTF natively, so the two LOD GLBs were converted to FBX by
`scripts/hero/glb_to_fbx.py` with Blender 5.2.3 LTS (exit 0, marker `LOD_FBX_OK <path>`), then verified by
re-import (`scripts/hero/verify_fbx.py`, marker `LOD_FBX_VERIFY_OK ... bones=23 actions=idle,reach,run meshes=1`).
The conversion also strips a stray Blender bone-display mesh (`Icosphere`) that the glTF importer creates and that
otherwise leaks into the FBX as an unskinned 80-triangle object. Provenance: `artifacts/hero/lod-conversion.json`.

Unity import report (`artifacts/ublade2/hero-import.json`, written by the build):

    WAYFARER_IMPORT_OK bones=23 clips=idle,reach,run smile=Smile maps=3 lod0Tris=1126 lod1Tris=615 lod2Tris=306
    WAYFARER_IMPORT_DETAIL height=2.023 materials=Wayfarer_Cloth,Wayfarer_Skin,Wayfarer_Leather,Wayfarer_Accent missing=none

- 23 bones including `cape_a`, `cape_b`, `cape_c`;
- clips `idle`, `run`, `reach`;
- `Smile` blend shape on the LOD0 skinned mesh (LOD0 is the only LOD with morph targets — the source LOD1/LOD2 GLBs
  contain none, recorded rather than invented);
- four materials remapped at runtime from the 512² atlas and normal map;
- LOD triangle counts 1126 / 615 / 306, matching the Godot source manifest.

Runtime hero systems (`unity/UBlade2/Assets/Scripts/HeroVisual.cs`):

- an `LODGroup` with three LODs (screen-relative transition heights 0.30 / 0.12 / 0.04) and three animators driven by
  one controller; measured LOD probe: distance 3.0 -> level 0, 5.2 -> 0, 8.0 -> 1, 12.0 -> 1, 20.0 -> 2;
- driven Smile (blend weight 0 -> 100 on collection) and Reach trigger on all three LODs;
- spring cape on the three cape bones, driven by planar speed;
- runtime evidence: `UBLADE_HERO_RUNTIME_OK visible=True lod=0 smile=100.0 cape=3 bones=23
  lod0Tris=1126 lod1Tris=615 lod2Tris=306 rect=547,237,185x315`.

A real prefab asset is generated by the build: `Assets/Resources/Hero/WayfarerLOD.prefab` (89,069 bytes) contains
the LODGroup, the three animators and the visual rotation. `HeroVisual` instantiates that prefab under the player, so
the CharacterController and collision are unchanged. The Blender/glTF model's visible forward is -Z while the
controller forward is +Z: the first capture showed the hero facing the camera while moving away (a forward mismatch).
The prefab rotates the visual 180 degrees; the re-captured frame shows the hero from behind, so forward matches the
controller and the cape is visible. Build markers: `UBLADE_PREFAB_SAVED ... lods=3` and `WAYFARER_PREFAB_OK ...
lods=3`; `hero-import.json` carries `prefabLodLevels: 3`, which the toolpack validates.

The capture is a 1280x720 windowed frame showing the Wayfarer standing on the arena floor between the platforms,
seen from behind so forward matches the controller: blue-grey cloth, brown leather, helmet and cape are visible
(section 5).

## 5. Build and capture

Build (Unity batchmode, `BuildUBlade2.Build`):

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_unity.ps1 -Mode build
    -> UBLADE2_BUILD_OK path=unity/UBlade2/Build/UBlade2.exe editorExit=0

Build log marker `UBLADE2_BUILD Succeeded bytes=97467934`; the executable itself is 667,136 bytes
(`unity/UBlade2/Build/UBlade2.exe`); `artifacts/ublade2/build.json` records the result and byte total.

Demo (headless deterministic acceptance):

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_unity.ps1 -Mode demo
    -> GYPSY_DEMO_OK collected=3 frames=67, exit 0, UBLADE2_DEMO_OK

Capture (visible window; hidden-window capture is known to fail, recorded in the LeftBehind work):

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_unity.ps1 -Mode capture
    -> UBLADE2_CAPTURE_OK frame=artifacts/ublade2/frame.png, exit 0

Measured frame (`artifacts/ublade2/frame.png`): 1280x720 PNG, 176,670 bytes, SHA-256
`50c0c8735c9ef46ff265d01bd21006588ab3a30c35b1a0e37d57cbb349982d1b`. The player wrote its hero screen rect into
`artifacts/ublade2/hero-runtime.json` (rect 547,237,185x315; 58,275 px). The pixel analysis
(`scripts/check_hero_capture.py` -> `artifacts/ublade2/capture-check.json`) measured inside that rect:
90.7% non-background pixels, 50,544 saturated pixels, 82 distinct colour buckets — a real, textured hero, not a flat
background. The image was also inspected by eye: the Wayfarer is centred and visible.

## 6. Gypsy integration

Gypsy's CLI resolves toolpacks, slices and capabilities from its own checkout, and its toolpack/slice runners hard-code
that checkout as the project root. This repository must not modify ProjectGypsy, so
`scripts/gypsy_unity.py` imports Gypsy's modules read-only (`sys.dont_write_bytecode = True`) and runs this
repository's manifests with `project_root=<this repo>` and state under `workstream/state/`. The validation, state
schemas and orchestrator are Gypsy's; none of them is reimplemented.

| Piece | Path | Result |
| --- | --- | --- |
| Capability manifests | `capabilities/engine.unity.port.build.json`, `...demo.json`, `...capture.json`, `...mobile.json` (blocked, not a pass), `capabilities/art.hero.wayfarer.unity.json` | `UBLADE2_CAPABILITIES count=5 errors=0`, `UBLADE2_CAPABILITIES_OK` |
| Toolpack | `toolpacks/ublade2.unity.json` | `UBLADE2_TOOLPACKS count=1 errors=0`; run: `passed=True artifacts=10 validators=8` |
| Slice | `slices/ublade2.unity.json` | run: `passed=True steps=2 artifacts=8` in 23.4 s (run `20261026T214139Z-518818`); state `workstream/state/slices/ublade2.unity.json` |
| Project check | `project.gypsy.json` | `checks: PASS, capabilities 50, tools 16, project UBlade2` (targets pc, mobile) |
| Mobile toolpack/slice | `toolpacks/ublade2.unity.mobile.json`, `slices/ublade2.unity.mobile.json` | Android build + adb install/launch + screenshot/logcat validators; added and manifest-validated, exercised manually on the emulator (section 7) |

Toolpack state (Gypsy schema `gypsy.toolpack-state/v1`): `workstream/state/toolpacks/ublade2.unity-*.json`.
The toolpack's validators check the executable, the build marker, the hero import JSON (bones=23, smile=Smile, maps=3,
prefabLodLevels=3, LOD triangles 1126/615/306), the demo marker, the hero runtime JSON (visible, bones=23, cape=3, screen pixels),
the 1280x720 PNG and the capture-check JSON.

The repository's own check list now includes a `manifests` check that validates these local Gypsy manifests
(schemas, ids, argv/artifact paths, validator kinds, step kinds, capability fields) — 12 manifests (3 toolpacks, 3 slices, 6 capabilities):

    python scripts/check_all.py
    -> checks: PASS (5/5)

Committed and pushed: `afae4804690a0d1356ca981f02d047d3c75c418c` (`origin/main`, public); GitHub Actions
`checks` run `40408803952` succeeded in 14 s.

## 7. Mobile: parked by operator decision

The operator directed that UltraBlade 2 is a **PC-only game at the moment** (2026-10-26). Mobile is parked, not part of
the release, and not claimed complete. The earlier work is preserved for a future pass:

- Unity Android Build Support was installed with the Unity CLI (AndroidPlayer, OpenJDK 17, SDK, NDK, command-line
  tools) and a signed debug APK of the game was built.
- The ARM64 APK installed and launched on AVD `UBlade2_API35` (x86_64 API 35, libndk translation, adb over ports
  40000/40001 because the host excludes 5554-5586) and on-device logcat showed `UBLADE2_START`, `UBLADE_HERO_OK` and the
  deterministic `GYPSY_DEMO_OK collected=3`.
- An in-app framebuffer capture (`Texture2D.ReadPixels`) produced a real 2400x1080 frame with clear HUD, scores,
  timer and touch controls; the world itself renders very dark under the emulator's Vulkan surface.
- `capabilities/engine.unity.port.mobile.json` is `planned`, the mobile toolpack/slice remain as future infrastructure,
  and `project.gypsy.json` targets are `pc` only.

## 7b. HyperBlade-fidelity playable slice (PC-only)

The slice was rebuilt against research of the 1996 PC game HyperBlade (Wizbang!/Activision). Reference sources and the
full fidelity mapping are in `docs/HYPERBLADE-REFERENCE.md`; the design is in `docs/GAME-DESIGN.md`. The authoritative
data came from the retail CD: `DATA/*.HTM` (14 teams, five-man rosters, per-difficulty ratings and goalie ratings),
`DATA/*.ENV` (the 14 drome prop layouts), `DATA/BOOIE.INI`/`DFTKEYBD.CFG` (controls), `DATA/INFOBTXT.LST` (HUD
strings), `GAME.HLP`/`HELP.HLP` (rules/tips/drome descriptions) and `JUMBO.TXT` (jumbotron sponsor ads), plus the
manual OCR and the three operator-provided YouTube captures analysed frame by frame.

What ships in the playable slice:

- **Elliptical drome**: runtime-generated ellipse floor/props, a curved inward-leaning shell/wall with a lip, small
  goals cut into the long ends, radial/concentric livery with the central red star and end fields, and the per-drome
  prop layout. No boards/glass/stands were added; the visible crowd/jumbotron was deliberately removed to match the
  in-game video evidence.
- **Sport**: carried/thrown rok, charged power shot (Space) and quick shot (F), pass (C), speedburst (D/Shift),
  jump (E), jak attack (S/right mouse), substitutions (1/2), 2 forwards + AI goalie per side with a five-man roster,
  three periods, **1/2/4-point goals** through the multiplier (and multiplier nullification), injuries/fatalities
  and downed players out until the next goal/period.
- **Teams/dromes**: all 14 clubs and arenas reconstructed from the shipped data (Chicago Machine, Houston Lone Stars,
  Las Vegas Black Jack, Los Angeles Shock Wave, Seattle Fury, Toronto Crusade, Leeds Knights, Moscow T-34s, Munich
  Sturmnacht, New York Glory, Tokyo Ronin, Warsaw Ambush, Jamaica DredNoks, BLAM! Nemesis) with kit colours derived
  from the game's home/away textures and five named dromers + a named goalie each.
- **Gauntlet**: the single-player ladder (the player's club faces the other 13 clubs; a loss or tie restarts the run;
  progress is saved in PlayerPrefs playlist key ublade2.season.v1).
- **Presentation**: HyperBlade HUD terminology (scoreboard with period pips, roster/health, speedburst and shot-power
  meters, shot-cam/rear-view frames, radar/rok finder/goal finder), a per-period sponsor gag, and the full dromer/goalie
  stat screen (PTS FGM FGA FG% 2X 4X POSS INT MULT INJ FAT / SHT SAV ERR %). Menus were restyled to the brushed-steel
  HyperBlade look.

Measured evidence from the final Windows player (Unity bundle version 2026.10.26.010 unless the release is rebuilt):

- build and version markers: `UBLADE2_BUILD Succeeded` and `UBLADE2_VERSION 2026.10.26.010`;
- legacy gate: `UBLADE2_DEMO_OK exit=0` / `GYPSY_DEMO_OK collected=3`, `UBLADE2_CAPTURE_OK` and `CAPTURE_CHECK_OK`;
- headless three-period match: `UBLADE2_MATCH_OK home=<n> away=<n> winner=<n> frames=<n> periods=3 phase=Ended` with a
  `ublade2.match.runtime/v2` evidence JSON (the final numbers are in BuildNotes 2026.10.26.010 and
  `artifacts/ublade2-pro/match-runtime.json`);
- league/Gauntlet save proof: `UBLADE2_LEAGUE_SAVE_OK ... passed=True`;
- 1280x720 match capture `MATCH_CAPTURE_CHECK_OK`, menu capture `MENU_CAPTURE_CHECK_OK` and six UI screenshots
  `UI_CAPTURES_CHECK_OK screens=6` inspected (elliptical drome, goals, dromers, HUD, roster, radar, stats screen);
- PC-only release stamp `UBLADE2_RELEASE_OK version=2026.10.26.010` with the ZIP/EXE hashes recorded in
  `artifacts/ublade2-release/release.json` and BuildNotes.

Honest limits: original assets/code/textures/audio/FMV are not shipped; this is a source-data-driven homage. The
killball/swarf/mine weapon economy, full bench fatigue/permadeath bookkeeping, wall-riding tricks, rendered crowd and
the exterior/FMV presentation are approximated or absent. Goal-mouth geometry is derived from the manual's 10-ft goal
and the elliptical-dome description (no legible original frame existed in the supplied videos). These are recorded in
DEFERRED.md and docs/GAME-DESIGN.md rather than claimed.

## 8. Limits and things deliberately not done

- The playable slice is a prototype scope: runtime-primitive arenas, one hero model, procedural audio, an offline
  four-round league and no online play. Imported arena art, a deeper roster/league, licensed media, a gamepad mapping
  screen and an installer are future production work and are not claimed.
- LOD1/LOD2 only appear at distance (probe levels 1 and 2 at 8 m and 20 m); the capture is LOD0. LOD0 is the only mesh
  with the Smile blend shape (true of the source assets).
- The hero's visible forward was initially 180 degrees from the controller (the first capture showed the face while
  moving away); the prefab now rotates the visual and the final capture was re-taken. The earlier frame is not cited.
- The FBX LOD clips imported into Unity are named `Wayfarer_Rig|idle` etc.; the runtime animator uses the LOD0 FBX
  clips (`idle`/`run`/`reach`), so the naming does not affect the port. Recorded for a future cleanup.
- Mobile is parked by operator decision (section 7): Android Build Support and a launched emulator pass exist from
  earlier work, but mobile is not part of the PC-only release and is not claimed complete (UB2-D15).
- The Unity engine-gate CI workflow (`.github/workflows/engine.yml`) is dispatch-only until a self-hosted
  Unity-licensed runner exists; it has not run. The local wrapper/gates and the hosted fast CI are the actual evidence.
- The Windows release is unsigned; the release stamp records the git HEAD and hashes but not a dirty-worktree flag.
- The Unity player window is closed by the player itself (`Application.Quit(0)`); no process, browser or temp file was
  left running. The capture frame was written to a temp path by the player and copied into `artifacts/ublade2/` by the
  wrapper.

## 9. Acceptance matrix

| Gate | Required evidence | Status |
| --- | --- | --- |
| Project | `project.gypsy.json` pins Unity 6000.6.7f1/unity-6 and target pc | **PASS** — section 2 |
| Build | Windows player built by batchmode, build log success | **PASS** — `UBLADE2_BUILD Succeeded bytes=97543422`, version 2026.10.26.008 |
| Demo | exact `GYPSY_DEMO_OK collected=3` marker, exit 0 | **PASS** — `frames=67`, exit 0 |
| Gameplay | 3 collectibles, movement/autopilot, no regression | **PASS** — three collects at frames 15/33/67 |
| Hero | 23 bones, 3 clips, Smile, 512² maps, LODs, visible capture | **PASS** — section 4 |
| Capture | 1280x720 frame inspected | **PASS** — 90.7% non-background in hero rect, inspected |
| Playable | HyperBlade-style sport on an elliptical drome, menu/HUD/pause/settings/results/stat screen | **PASS** — headless three-period match 7-2, match capture with the elliptical drome and HUD, six UI screenshots, Gauntlet save/load check, 14 data-derived teams/dromes (section 7b) |
| Release | stamped Windows distributable with hashes | **PASS** — `UBLADE2_RELEASE_OK version=2026.10.26.010 win_sha256=2bb791a8...054b1d`, release.json + release.txt |
| Mobile | mobile target | **PARKED** — operator decision: PC-only for this milestone; earlier partial APK/launcher evidence preserved but not claimed complete (UB2-D15) |
| Records | Gypsy checks pass and evidence is committed | **PASS** — `checks: PASS (7/7)`, 12 manifests valid (3 toolpacks, 3 slices, 6 capabilities); commit, push and CI result are recorded in the final report/BuildNotes 2026.10.26.008 |

## 10. Reproduce

    python scripts/gypsy_unity.py check
    python scripts/gypsy_unity.py capabilities
    python scripts/gypsy_unity.py toolpacks check
    python scripts/gypsy_unity.py toolpacks run ublade2.unity
    python scripts/gypsy_unity.py slice run ublade2.unity
    python scripts/gypsy_unity.py toolpacks run ublade2.game
    python scripts/gypsy_unity.py slice run ublade2.game
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1
    python scripts/check_all.py

Expected markers: `WAYFARER_IMPORT_OK`, `WAYFARER_PREFAB_OK`, `UBLADE2_BUILD Succeeded`, `GYPSY_DEMO_OK collected=3`,
`UBLADE_HERO_RUNTIME_OK`, `CAPTURE_CHECK_OK`, `UBLADE2_MATCH_OK`, `UBLADE2_LEAGUE_SAVE_OK`, `MATCH_CAPTURE_CHECK_OK`,
`UI_CAPTURES_CHECK_OK`, `UBLADE2_RELEASE_OK`, `UBLADE2_GYPSY_CHECK exit=0`, `checks: PASS (7/7)`.

## 11. Provenance

| File | SHA-256 |
| --- | --- |
| `unity/UBlade2/Assets/Resources/Hero/wayfarer.fbx` | `97e8c7f95ba7faecedddc2fd9bb3238ed0e16251b74076947005bfb032ed6286` |
| `unity/UBlade2/Assets/Resources/Hero/wayfarer_lod1.fbx` | `f31201462e0a318d3ef59ef741eaba2fba64bfc8895f797d834f5ced4671357f` |
| `unity/UBlade2/Assets/Resources/Hero/wayfarer_lod2.fbx` | `857ad41fdb0897da18dda98db1b6ba0a1234600b159be46707a23d69c69924bf` |
| `artifacts/ublade2/frame.png` | `17842a717ab8796b4379251252835e9c41c49e3ae154846e9b4e6091439e6f9b` |
| `artifacts/ublade2/hero-import.json` | `dbd934dda6fd6b3501d5795c4203fef4e4845722ab8611dbaa2ca7bab114b2f1` |
| `artifacts/hero/lod-conversion.json` | `f0fd927f6ed5dbf2db366d3a0240544a5bc2ada7d33f1bf911965662ed2e1c43` |
| `unity/UBlade2/Build/Android/UBlade2.apk` | `894795f2672e639ae3502c86715a8d254f08cdcd087c90f35be70605cb349046` |
| `artifacts/ublade2-mobile/screen.png` | `f3ca3df3770291f84fed9c2369de462d3641442c3637094042094113cd68bf70` |
| `artifacts/ublade2-mobile/logcat-launch.txt` | `91108e4b52c1689c66e81fc6adeacb9c76ec321759fd6293d6ee1b35b81915fd` |
| `artifacts/ublade2-mobile/logcat-demo.txt` | `86ffc98d52653fc46d607b960a4fea126dac368fc8ea449548500cafb4bf879a` |
| `unity/UBlade2/Build/UBlade2.exe` | `9806abde4a406ed68647e9505852c49f6aa72a372f4f084e2aa2821affea57a2` |
| `artifacts/ublade2-pro/match.png` | `f737e95d9da806dfda611a9dc77a31e2c1a2c9dec8dc53a0edd43a2d04b0e57c` |
| `artifacts/ublade2-pro/menu.png` | `6fe8c440a96319654b51eb6a508329ddfa4e7e0af3fcf0d36f35268269238294` |
| `artifacts/ublade2-pro/match-runtime.json` | `eec6524951ccbc9c16d5e259385d25874db6d6037d65f1915bb21dc94957ca57` |
| `artifacts/ublade2-pro/league-save.json` | `485f6d2371181ea315624b2d63494140577d42532ccdabe710cf430444c4d006` |
| `artifacts/ublade2-pro/ui-captures.json` | `eba8814a291a198f93a6409e7be8bee695c7ef1da118c3a525c398e13562a388` |
| `artifacts/ublade2-pro/ui/contact-sheet.png` | `55d1e08aa8b9eba04ebd992773a755df16a7bcb7b741f64786489fb14669b7b4` |
| `artifacts/ublade2-release/release.json` | `d038c8fe03c0a048e299a809632339d5e33a4decd89c6567d1000294e9924030` |
| `dist/UBlade2-2026.10.26.010-win64.zip` | `2bb791a82f72f4c6f1c79bbdccb734aff0b55a074271cd079cbbc01e33054b1d` |

Unity build: `6000.6.7f1 (b22767f04f04)`; Blender: `5.2.3 LTS`; Python: `3.14.2`; Pillow: `12.0.0`.
