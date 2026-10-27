# BuildNotes - the changelog

Current version: 2026.10.26.008

BuildNotes is UltraBlade 2's dated, versioned changelog (`<CHANGELOG>` in the pack). Newest entry first; versions
are `YYYY.M.D.NNN` with a zero-padded sequence, and the next one is computed by
[`scripts/next-version.py`](./scripts/next-version.py) rather than typed. [`Retrace.md`](./Retrace.md) is the prompt
log these entries answer. Records are guarded by [`scripts/records_check.py`](./scripts/records_check.py) and run by
[`scripts/check_all.py`](./scripts/check_all.py).

## How versions 001-008 were assigned

No changelog existed before this bootstrap, so the bootstrap assigned `2026.10.26.001` to the repository-creation
commit (the only commit already in `git log`) and `2026.10.26.002` to the pack application. `2026.10.26.003` (the
Unity port) was computed by `scripts/next-version.py` from the previous date, and `2026.10.26.004` (the
Stage B completion pass) was computed the same way, and `2026.10.26.005` (the commit/push close-out) by the
script. `2026.10.26.006` (status-line correction after the push) is computed the same way; from then on the number is
computed by the script. `2026.10.26.007` (the Android module install and mobile launch) is computed the same way.

This changelog has **no generated documentation copies yet**. There is no generated documentation artefact to
regenerate; when the game arrives, its export and captures will be generated artefacts under gitignored `build/` and
`artifacts/`. [`scripts/records_check.py`](./scripts/records_check.py) checks the source, its `Current version:` header
and [`Retrace.md`](./Retrace.md); it will compare generated copies when they exist.

## 2026.10.26.008

**Deliver a playable PC production slice, close the deferred register, stamp a Windows release, and park mobile by
operator decision.**

- Date: 2026-10-26
- Commit: this change (commit hash, push and CI run in the final report).
- Change: implemented the playable production slice on top of the port: match rules/scoring/season (`MatchRules.cs`,
  `LeagueData.cs`), AI opponent (`AiController.cs`), strike/stun/dash combat (`CombatSystem.cs`), neon IMGUI screens
  (`GameUI.cs`), procedural audio (`GameAudio.cs`), keyboard/mouse/touch input (`GameInput.cs`) and the host
  state machine/captures/league save test in `UBlade2Game.cs`. Added wrapper modes `match`, `league-test`,
  `match-capture`, `menu-capture`, `ui-capture`; Android build/in-app capture support; Windows release packaging
  (`scripts/ublade2_release.ps1`, PC-only); match/UI capture validators; the `ublade2.game` toolpack/slice and
  `game.playable.slice` capability; secret scan, peer boundary check, dispatch-only engine workflow, UI standard,
  rollback record and contract decision. Updated README/AGENTS/DEFERRED/UNITY-PORT/NOTES for the PC-only playable
  release. The mobile target is parked by operator decision (2026-10-26) and is not part of the release.
- Verification: Windows build `UBLADE2_VERSION 2026.10.26.008`, `UBLADE2_BUILD Succeeded bytes=97543422`. Legacy gate:
  `UBLADE2_DEMO_OK exit=0` with `GYPSY_DEMO_OK collected=3`, `UBLADE2_CAPTURE_OK` and `CAPTURE_CHECK_OK`. Headless
  match: `UBLADE2_MATCH_OK player=1 ai=2 winner=2 frames=176 phase=Ended` with `UBLADE2_MATCH_EVIDENCE passed=True`.
  League save/load: `UBLADE2_LEAGUE_SAVE_OK round=1 wins=1 points=3 passed=True`. Captures: match
  `MATCH_CAPTURE_CHECK_OK` (76.9% non-background, 349 colours), menu `MENU_CAPTURE_CHECK_OK`, six UI screenshots
  `UI_CAPTURES_CHECK_OK screens=6` inspected (menu/league/settings/match/pause/result contact sheet). Release:
  `UBLADE2_RELEASE_OK version=2026.10.26.008 package=dist/UBlade2-2026.10.26.008-win64.zip`; ZIP 50,871,958 bytes
  SHA-256 `fef1d1b4be9c23f38633ffd568f8d81a6b0e43d6ae0c916112ec0d0cf80d7355`; player EXE SHA-256
  `9806abde4a406ed68647e9505852c49f6aa72a372f4f084e2aa2821affea57a2`. Fast list: `checks: PASS (7/7)` with 12
  manifests (3 toolpacks, 3 slices, 6 capabilities), `PASS secrets` and `PEER_BOUNDARY_OK peers=1`.
- Deferred register: all items closed with evidence - D3 UI standard, D5 contract not applicable, D6 rollback +
  league save proof, D7 peer guard, D8 secret scan, D9 dispatch-only engine workflow (activation pending a self-hosted
  runner, not claimed as run), D10/D12 not applicable decisions, D11 stamped release, D14 playable slice, D15 mobile
  parked by operator decision (PC-only). Earlier D1/D2/D4/D13 remain closed.
- Not verified, and not claimed: the dispatch-only CI engine workflow has not run; the Windows release is unsigned
  and has no installer; imported arena art/online play/deeper league/licensed media are future production work; mobile
  is parked and is not claimed complete.
- Note: the release was built from the working tree immediately before the release commit, so `release.json`
  `gitHead` records the preceding commit (`03d60a5...`) and not the final release commit; the final commit/push/CI are
  in the final report. This is exactly the dirty-worktree limitation documented in `docs/RELEASE.md`.
- Method: four subagents produced the gameplay core, presentation modules, process scripts/docs and release packaging;
  the Lead integrated, compiled, ran every gate, fixed the reported integration issues (settings difficulty and
  fullscreen wiring, HUD round metadata, audio settings application, touch-on-desktop, ROLLBACK/implementation drift,
  league save proof) and recorded the work.


## 2026.10.26.007

**Install Unity Android Build Support, build and sign a debug APK, launch it on an emulator, and verify the mobile
markers (clear mobile visual capture still outstanding).**

- Date: 2026-10-26
- Commit: this change.
- Change: installed **Unity Android Build Support** with the Unity CLI
  (`editors module add 6000.6.7f1 --module android --child-modules --accept-eula --non-interactive`): AndroidPlayer,
  OpenJDK 17.0.18, SDK build-tools 36.0.0, platform-tools 37.0.1, platforms 34/36/37, NDK r27c and command-line
  tools 16.0. Added the Android build path to `BuildUBlade2.BuildAndroid` (bundle `com.c7imi.ublade2`, IL2CPP, Vulkan,
  ARM64, minSdk 26, target 36, debug signing), an Android intent-extra demo hook to the player, and the Android/mobile
  scripts `scripts/ublade2_android.ps1` and `scripts/check_mobile_capture.py`. Added `toolpacks/ublade2.unity.mobile.json`,
  `slices/ublade2.unity.mobile.json` and the `engine.unity.port.mobile` capability (maturity **partial**); added the
  mobile target to `project.gypsy.json`; updated README, AGENTS, DEFERRED (closed UB2-D13, opened UB2-D15),
  ProjectPack-NOTES and `docs/UNITY-PORT.md` section 7. Stopped and cleaned the emulators afterwards; the copied
  editor and download leftovers were deleted.
- Verification: Unity CLI reported all Android modules `status: installed` and `AndroidPlayer` is present. Build:
  `UBLADE2_ANDROID_BUILD Succeeded bytes=121948467`; APK 15,398,546 bytes, SHA-256
  `894795f2672e639ae3502c86715a8d254f08cdcd087c90f35be70605cb349046`; `apksigner verify` reports v2 scheme verified
  with the Android Debug certificate; `aapt2 dump badging` reports package `com.c7imi.ublade2`, minSdk 26, target 36,
  compileSdk 36. Device: AVD `UBlade2_API35` (google_apis x86_64, API 35) ran under emulator 37.2.12 with WHPX. The
  host excludes the 5554-5586 TCP range, so the emulator was started with `-ports 40000,40001` and adb connected to
  `127.0.0.1:40001`; the ARM64 APK runs through `libndk_translation.so` (`abilist=x86_64,arm64-v8a`). `adb install`
  reported `Success`; logcat contains `UBLADE2_START demo=False`, `UBLADE_HERO_OK lodLevels=3 bones=23 smile=Smile
  cape=3 prefab=WayfarerLOD` and `UBLADE2_WORLD crystals=3`; the on-device deterministic demo (intent extra
  `ublade2_demo`) printed `GYPSY_DEMO_OK collected=3 frames=66`. Screenshot `artifacts/ublade2-mobile/screen.png`
  (2400x1080, SHA-256 `f3ca3df3770291f84fed9c2369de462d3641442c3637094042094113cd68bf70`) is **near-black** because
  `adb screencap` does not capture the emulator's Vulkan swapchain on this host — recorded as **UB2-D15**, not faked.
  Gypsy: `UBLADE2_CAPABILITIES count=5 errors=0`, `UBLADE2_TOOLPACKS count=2 errors=0`, two slices listed,
  `checks: PASS (5/5)` with 9 local manifests (2 toolpacks, 2 slices, 5 capabilities). The desktop PC gates and their
  committed evidence are unchanged; the Android changes do not affect the Windows player path.
- Not verified, and not claimed: a clear mobile visual capture and a physical-device run (UB2-D15); the full
  production game surface (UB2-D14); the desktop PC artifacts were not regenerated after the Android-only changes.


## 2026.10.26.006

**Correct stale "uncommitted" status lines after the port push.**

- Date: 2026-10-26
- Commit: this change.
- Change: README's "Work state" line and the limits bullet in `docs/UNITY-PORT.md` still described the port as
  uncommitted after it was committed and pushed as `afae480` (close-out `abd433a`). Both now state the committed
  hashes and the two successful GitHub Actions runs. Historical entries (2026.10.26.003/004 and Retrace 3/4) keep
  their statements about the state at the time they were written; 2026.10.26.005 records the commit/push close-out.
- Verification: `python scripts/check_all.py` -> `checks: PASS (5/5)` on this tree; the change is documentation text
  only, no engine artifact was regenerated, and the pushed evidence is unchanged. The commit and its CI run are in
  the GitHub history for this change.


## 2026.10.26.005

**Commit and push the Unity port, verify CI, and close the Stage B Records acceptance row.**

- Date: 2026-10-26
- Commit: this change - the records/CI close-out commit, pushed after the port commit `afae480`.
- Change: committed the Stage B port as `afae480` (128 files, +19,183/-114) including the Unity project, hero
  prefab, scripts, the Gypsy toolpack/slice/capability manifests, the port records and docs, and the acceptance
  evidence artifacts force-added under `artifacts/ublade2/` and `artifacts/hero/`; pushed origin/main
  `3809c2c..afae480` and recorded the CI result; documented the commit/push evidence here and in
  `docs/UNITY-PORT.md`; the acceptance matrix row "Records: evidence is committed" moved from PARTIAL to PASS.
  `scripts/normalize_evidence.py` keeps text evidence UTF-8/LF so committed evidence hashes equal the hashes Gypsy
  recorded at run time.
- Verification: `git push` exit 0 (`3809c2c..afae480`); local HEAD = origin/main =
  `afae4804690a0d1356ca981f02d047d3c75c418c`; repository visibility PUBLIC; GitHub Actions run `40408803952`
  ("checks") completed **success** in 14 s; fast check list `checks: PASS (5/5)` on the committed tree; final Gypsy
  slice state `workstream/state/slices/ublade2.unity.json` (run `20261026T215755Z-e6e9b2`) passed=True, steps=2,
  artifacts=8 in 22.89 s; toolpack aggregate state `workstream/state/toolpacks/ublade2.unity-20261026T215755Z-bfefd4.json`
  passed=True, artifacts=10, validators=8.
- Not verified, and not claimed: mobile - Android Build Support absent, blocked capability manifest, no APK/device
  evidence (UB2-D13); the rest of the production game (HUD, menu, audio, combat, teams) is not ported (UB2-D14).


## 2026.10.26.004

**Complete Stage B against the porting instructions: literal preflight, hero prefab with controller-forward visual,
mobile capability manifest, source inventory and preflight evidence.**

- Date: 2026-10-26
- Commit: this change - the port commit, made and pushed in response to the operator's explicit request (prompt 5).
  The records and the acceptance evidence artifacts are part of it; the commit hash and CI run are recorded in
  2026.10.26.005 after the push.
- Change: ran the instructions' literal preflight (Gypsy version and `check --project`, Unity `-version`, build-support
  modules, destination git snapshot, source-asset inventory); added a real prefab asset generated by the build -
  `unity/UBlade2/Assets/Resources/Hero/WayfarerLOD.prefab` (LODGroup, three animators, and a 180-degree rotation so
  the visual forward matches the CharacterController) - and made `HeroVisual` instantiate it, leaving the
  CharacterController and collision unchanged; recorded `prefabLodLevels=3` in `hero-import.json` and added it to
  the toolpack's hero-import validator; added the blocked mobile capability
  `capabilities/engine.unity.port.mobile.json`; documented preflight evidence, source inventory, the
  prefab/forward validation and final measurements in `docs/UNITY-PORT.md`; updated README, AGENTS, DEFERRED and
  ProjectPack-NOTES. No changes to ProjectGypsy.
- Verification: literal preflight - `Gypsy Studio 0.1.0` exit 0; `gypsy check --project` -> `checks: PASS,
  capabilities 50, tools 16, project UBlade2` exit 0; `Unity -version` -> `6000.6.7f1` exit 0; PlaybackEngines only
  `windowsstandalonesupport`, `WebGLSupport`; HEAD `3809c2c` unchanged. Build `UBLADE2_BUILD Succeeded
  bytes=97467934` with `WAYFARER_PREFAB_OK ... lods=3`; exe 667,136 bytes SHA-256
  `9806abde4a406ed68647e9505852c49f6aa72a372f4f084e2aa2821affea57a2`; demo `GYPSY_DEMO_OK collected=3 frames=67`
  exit 0; capture 1280x720 PNG 176,670 bytes SHA-256
  `50c0c8735c9ef46ff265d01bd21006588ab3a30c35b1a0e37d57cbb349982d1b`; capture-check `CAPTURE_CHECK_OK` heroRect
  185x315, 90.7% non-background, 82 colour buckets, inspected (hero seen from behind, forward matches the controller);
  hero import `bones=23 clips=idle,reach,run smile=Smile maps=3 prefabLodLevels=3 lod0Tris=1126 lod1Tris=615
  lod2Tris=306`; Gypsy: capabilities `count=5 errors=0`, toolpacks `count=1 errors=0`, slice `passed=True steps=2
  artifacts=8` in 23.4 s; fast check list `checks: PASS (5/5)`. Gypsy tests (run earlier in this session):
  `Ran 282 tests ... OK (skipped=2)` exit 0; Gypsy checkout clean before and after, HEAD unchanged.
- Not verified, and not claimed: mobile - Unity Android Build Support is absent; the blocked capability manifest
  records it and no APK/device evidence exists (UB2-D13); the rest of the production game (HUD, menu, audio, combat,
  teams) is not ported (UB2-D14); the acceptance row "Records: evidence is committed" is outstanding because
  committing requires an explicit operator request (the operator's standing rule).


## 2026.10.26.003

**Port the three-collectible gameplay contract and the Wayfarer hero to Unity 6000.6.7f1, with build/demo/capture
acceptance evidence.**

- Date: 2026-10-26
- Commit: none - the operator asked for the work to stay uncommitted; it is in the working tree (staged but not
  committed at the time of this entry).
- Change: created the **Unity 6000.6.7f1** project `unity/UBlade2` and pinned `project.gypsy.json` to unity
  6000.6.7f1 (adapter unity-6, target pc; mobile deliberately absent); ported the CharacterController movement,
  nearest-crystal autopilot, three crystals, follow camera and deterministic demo; ported the Wayfarer hero
  (`wayfarer.fbx` plus LOD1/LOD2 GLB->FBX converted with Blender 5.2.3, 512² maps, LODGroup 1126/615/306 triangles,
  `Smile` blend shape, spring cape); added `scripts/ublade2_unity.ps1`, `scripts/check_hero_capture.py`,
  `scripts/gypsy_unity.py`, `scripts/hero/*`; added the Gypsy manifests `toolpacks/ublade2.unity.json`,
  `slices/ublade2.unity.json` and four `capabilities/*.json`; added `docs/UNITY-PORT.md` and `docs/README.md`;
  added a `manifests` check to `scripts/check_all.py` (5 checks); updated README, AGENTS, DEFERRED and
  ProjectPack-NOTES. ProjectGypsy was read only - not modified, its records untouched, its Godot game untouched.
- Verification: build log `UBLADE2_BUILD Succeeded bytes=97444534`; demo `GYPSY_DEMO_OK collected=3 frames=67` exit 0;
  windowed capture 1280x720, SHA-256 `f4acdd3c8e8d9b01f579c467737e88a70ea8a1a0227d21a2796eeb347bb494cc`, inspected
  visually and measured by `scripts/check_hero_capture.py` (hero rect 180x304, 90.22% non-background, 115 colour
  buckets, `CAPTURE_CHECK_OK`); hero import `bones=23 clips=idle,reach,run smile=Smile maps=3 lod0Tris=1126
  lod1Tris=615 lod2Tris=306` and runtime `UBLADE_HERO_RUNTIME_OK visible=True lod=0 smile=100.0 cape=3`; Gypsy:
  `UBLADE2_CAPABILITIES_OK count=4 errors=0`, `UBLADE2_TOOLPACKS_OK`, toolpack run `passed=True artifacts=10
  validators=8` (state under `workstream/state/toolpacks/`), project check `checks: PASS, capabilities 50, tools 16,
  project UBlade2` exit 0, Gypsy tests `282 tests OK (skipped=2)` exit 0 with the Gypsy checkout clean before and
  after; fast check list `checks: PASS (5/5)`. The new `manifests` check was proved able to fail: a
  deliberately broken manifest (unknown validator kind, escaping artifact path) in a throwaway directory printed
  `MANIFESTS_CHECK_FAILED_AS_EXPECTED`, while this tree passed; the probe directory was deleted.
- Not verified, and not claimed: mobile - Unity Android Build Support is not installed, no APK or device evidence
  exists (UB2-D13); the rest of the production game (HUD, menu, audio, combat, teams) is not ported (UB2-D14).


- Superseded evidence: the capture frame and the Unity build outputs were regenerated by 2026.10.26.004 (hero
  prefab and controller-forward fix); this entry keeps the measurements of its own run, and the current artifact
  hashes are recorded in 2026.10.26.004.

## 2026.10.26.002

**Apply the ProjectPack from ProjectGypsy, and bootstrap the records and check list.**

- Date: 2026-10-26
- Commit: this change.
- Change: copied ProjectGypsy's pack revision PG-1 into `ProjectPack.md` as revision **UB2-1** (Parts 1-10 carried;
  header and Part 11 re-authored for this repository), carried the parked interface material into
  `ProjectPack-ADDENDUM-Interface.md`, and wrote `ProjectPack-NOTES.md` resolving the placeholders, the §8.1
  decisions, the §2.31 in-force list, the §8.2 check inventory and the Part 11 mapping. Added `AGENTS.md` (pack
  rules plus the Gypsy Studio invocation block), `DEFERRED.md`, the prompt log `Retrace.md`, this changelog,
  `scripts/next-version.py`, `scripts/records_check.py`, `scripts/check_all.py`, `scripts/install-hooks.py`, the
  tracked `.githooks/pre-commit` and `.github/workflows/checks.yml`. `README.md` updated to point at the rules and
  the records.
- Verification: `python scripts/check_all.py` -> `checks: PASS (4/4)` (records, encoding, links, callers);
  `python scripts/install-hooks.py` -> `core.hooksPath = .githooks` (read back from git config);
  `ProjectPack-NOTES.md` "How this application of the pack was verified" records the provenance hashes and the diff
  Every check was also shown able to fail in a throwaway copy: a broken BuildNotes header made `records`
  FAIL; a staged BOM plus a broken link made `encoding` and `links` FAIL; a hook argument made `callers` FAIL
  (`checks: FAIL (0/4 passed)`). The copy was deleted afterwards.
  scope. Not verified, and not claimed: no game build, test suite, engine choice, `project.gypsy.json` manifest or
  design document exists (UB2-D1, UB2-D2).

## 2026.10.26.001

**Initial commit: UltraBlade 2 project workspace.**

- Date: 2026-10-26
- Commit: `2ee21f9`.
- Change: `README.md`, `.gitignore` and `.gitattributes` for the new repository; the public GitHub repository
  C7-IMI/UltraBlade2 created under the C7-IMI account (owner `C7-IMI`).
- Verification: `gh repo view C7-IMI/UltraBlade2 --json visibility,url` -> PUBLIC and the repository URL; an
  unauthenticated `GET /repos/C7-IMI/UltraBlade2` returned HTTP 200.
