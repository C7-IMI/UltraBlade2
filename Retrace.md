# Retrace - the prompt log

Retrace is UltraBlade 2's prompt log (`<PROMPT_LOG>` in the pack). Every request is recorded here in the order it
was made: its date, the request, its status, and the commit or evidence it produced. The newest entry is at the
bottom and the prompt numbers are contiguous from 1.

Read it with [`BuildNotes.md`](./BuildNotes.md), the dated versioned changelog. `python scripts/records_check.py` (run
by `python scripts/check_all.py`) fails if an entry loses its number or status, if BuildNotes is not newest-first, if
BuildNotes' `Current version:` header disagrees with its newest heading, or if a version repeats.

## How this log was started

This log starts on 2026-10-26 with the repository's first two prompts. Both were asked in the same session, and the
pack was applied in that session: entry 1 is the repository creation, entry 2 is the pack application. The request
lines are the prompts as written (they were terse); per-prompt durations were not preserved, and that gap is stated
rather than invented. From entry 3 on, the request is appended as written and the record is written in the same
commit as the change it describes.

Each entry has:

- **Request** - the prompt as written.
- **Status** - Completed, In progress or Blocked. A request that changed nothing says so explicitly.
- **Version** - the BuildNotes version the change produced, or why no version belongs to it.
- **Commit/evidence** - the commit hash, or the file and observation that are the evidence.

## 1 - 2026-10-26

- Request: "Create a new repo called UltraBlade 2. Make it public"
- Status: Completed
- Version: 2026.10.26.001
- Commit/evidence: `2ee21f9` - the initial commit. The repository https://github.com/C7-IMI/UltraBlade2 was verified
  public with `gh repo view C7-IMI/UltraBlade2 --json visibility,url` (`"visibility":"PUBLIC"`) and with an
  unauthenticated `GET /repos/C7-IMI/UltraBlade2` that returned HTTP 200; `main` tracks `origin/main`.

## 2 - 2026-10-26

- Request: "copy the ProjectPack from ProjectGypsy to this repo, read it, then apply it to this session"
- Status: Completed
- Version: 2026.10.26.002
- Commit/evidence: the pack-application commit - `ProjectPack.md` (revision UB2-1),
  `ProjectPack-ADDENDUM-Interface.md`, `ProjectPack-NOTES.md`, `AGENTS.md`, `DEFERRED.md`, this log,
  `BuildNotes.md`, `scripts/next-version.py`, `scripts/records_check.py`, `scripts/check_all.py`,
  `scripts/install-hooks.py`, `.githooks/pre-commit` and `.github/workflows/checks.yml`. The application is verified
  in the table at the end of `ProjectPack-NOTES.md`; `python scripts/check_all.py` prints the check list's result,
  recorded in `BuildNotes.md`.

## 3 - 2026-10-26

- Request: "You are the coding agent for UltraBlade 2. We are migrating the proven ProjectGypsy game/hero to Unity as
  the production pipeline. ... Create a new Unity project called UBlade2 for this instead of using LeftBehind"
  (quoted in full below).
- Status: Completed - the gameplay, hero, build, demo and capture gates pass on Unity 6000.6.7f1/Windows PC. Mobile
  is blocked (Android Build Support not installed) and is not claimed. Gypsy's checkout was not modified: clean
  before and after, HEAD `7480a06c375338e532d060dbcf589416c992e2f2`. Nothing was committed or pushed.
- Version: 2026.10.26.003
- Commit/evidence: none - the operator asked for the work to stay uncommitted. Evidence: `docs/UNITY-PORT.md`;
  build log `artifacts/ublade2/build.log` (`UBLADE2_BUILD Succeeded bytes=97444534`); demo log
  `artifacts/ublade2/demo.log` (`GYPSY_DEMO_OK collected=3 frames=67`, exit 0); capture
  `artifacts/ublade2/frame.png` (1280x720, SHA-256 `f4acdd3c8e8d9b01f579c467737e88a70ea8a1a0227d21a2796eeb347bb494cc`,
  inspected); `artifacts/ublade2/capture-check.json` (`CAPTURE_CHECK_OK`, hero rect 180x304, 90.22% non-background);
  `artifacts/ublade2/hero-import.json` (bones=23, clips=idle,reach,run, smile=Smile, maps=3, LOD tris
  1126/615/306); Gypsy toolpack state `workstream/state/toolpacks/ublade2.unity-*.json` (passed=True, 10 artifacts,
  8 validators); `UBLADE2_GYPSY_CHECK exit=0`; Gypsy tests `282 tests OK (skipped=2)`.

  ```text
  You are the coding agent for UltraBlade 2. We are migrating the proven ProjectGypsy game/hero to Unity as the
  production pipeline. This is a port with real acceptance evidence, not an engine toggle.

  Gypsy Studio checkout: <GYPSY_HOME>
  UltraBlade 2 checkout: <ULTRABLADE_HOME>
  Read first: <GYPSY_HOME>/docs/GYPSY-AGENT-GUIDE.md and <GYPSY_HOME>/docs/UNITY-PORT-INSTRUCTIONS.md
  Also read the target project's AGENTS.md / AGENTS.gypsy.md if present.
  Do not modify <GYPSY_HOME>, its records, or its Godot game. Work only in <ULTRABLADE_HOME>. Do not delete or call
  the Godot project migrated. Do not commit or push unless the operator asks.

  Goal
  Migrate the current game to Unity 6000.6.7f1 as a full production pipeline:

  Create the Unity project and project.gypsy.json with engine unity, version 6000.6.7f1, target pc (add mobile only
  after Unity Android Build Support is installed).
  Port the same three-collectible gameplay contract: CharacterController movement, autopilot, crystals, follow
  camera, deterministic demo that prints exactly GYPSY_DEMO_OK collected=3 frames=<n> and exits 0.
  Port the Wayfarer hero: wayfarer.fbx (23 bones incl. cape_a/b/c, idle/run/reach, Smile blend shape, 512² maps),
  runtime LODGroup/LOD switching, driven Smile, spring cape, visible 1280x720 capture.
  Build a Windows player, run the demo, capture a frame, and inspect it.
  Install Unity Android Build Support only if mobile is required; build/launch a signed debug APK and capture
  screenshot + logcat. Do not claim mobile before that.
  Add a Unity toolpack/slice with marker/capture validators, capability manifests, docs and records.
  Run Gypsy checks/tests and report exact commands, exit codes, markers, artifact paths, visibility, and limits.
  Current known state
  Unity licence is active; LeftBehind builds/runs on Windows.
  Unity Windows Standalone Build Support is installed.
  Unity Android Build Support is NOT installed.
  Unity engine build support is proven through LeftBehind, not a general adapter.
  The hero LOD sources are GLB; Unity does not import glTF natively. Export LOD1/LOD2 to FBX or approve a glTF
  package.
  Device launch in the current host is blocked by an emulator multi-instance error; use a clean host/device and
  record real evidence.
  Stop conditions
  Stop and report if Unity Android is required but missing, the hero import fails, the marker cannot be produced,
  capture cannot show the hero, or Gypsy/Unity cannot be found. Never fake a build, marker, capture or device pass.

  Create a new Unity project called UBlade2 for this instead of using LeftBehind
  ```
- Superseded evidence: the capture frame and build outputs were regenerated by prompt 4 (2026.10.26.004, hero
  prefab and controller-forward fix); the hashes above belong to this entry's run, and the current artifact hashes
  are recorded in entry 4.


## 4 - 2026-10-26

- Request: "Here are the porting instructions: Use this from the UltraBlade 2 repository/session. Do not run Stage B
  in the ProjectGypsy checkout. ... Never substitute "it should work" for a run. Never mark Stage B complete from a
  passing manifest alone." (quoted in full below).
- Status: Completed, with one acceptance row reported openly rather than faked: literal preflight, gameplay, hero,
  build, demo, 1280x720 capture, Gypsy toolpack/slice, capability manifests and docs/records all pass; mobile is
  blocked and not claimed; "Records: evidence is committed" is **outstanding** because the operator's standing
  instruction forbids committing or pushing without an explicit request.
- Version: 2026.10.26.004
- Commit/evidence: none - staged, not committed. Evidence: `docs/UNITY-PORT.md`; literal preflight outputs
  (`Gypsy Studio 0.1.0`, `checks: PASS, capabilities 50, tools 16, project UBlade2`, `Unity -version 6000.6.7f1`);
  build log `artifacts/ublade2/build.log` (`UBLADE2_BUILD Succeeded bytes=97467934`,
  `WAYFARER_PREFAB_OK ... lods=3`); demo log `artifacts/ublade2/demo.log` (`GYPSY_DEMO_OK collected=3 frames=67`
  exit 0); capture `artifacts/ublade2/frame.png` (1280x720, 176,670 bytes, SHA-256
  `50c0c8735c9ef46ff265d01bd21006588ab3a30c35b1a0e37d57cbb349982d1b`, inspected); capture check
  `artifacts/ublade2/capture-check.json` (`CAPTURE_CHECK_OK`, hero rect 185x315, 90.7% non-background);
  `artifacts/ublade2/hero-import.json` (`prefabLodLevels=3`, bones=23, clips, Smile, 3 maps, LOD triangles);
  Gypsy state `workstream/state/slices/ublade2.unity.json` (passed=True steps=2 artifacts=8) and
  `workstream/state/toolpacks/ublade2.unity-20261026T214139Z-cbfd38.json`; fast check list `checks: PASS (5/5)`.

  ```text
  Here are the porting instructions:

  Use this from the UltraBlade 2 repository/session. Do not run Stage B in the ProjectGypsy checkout. The current
  ProjectGypsy game stays Godot as the legacy/reference implementation. This is a port with explicit acceptance
  evidence, not an engine toggle.

  0. Roles and boundaries
  Source of truth for the current game: the ProjectGypsy checkout at <GYPSY_HOME>.
  Destination: the separate UltraBlade 2 checkout at <ULTRABLADE_HOME>.
  Gypsy Studio CLI: python "<GYPSY_HOME>/workstream/gypsy.py".
  Read <GYPSY_HOME>/docs/GYPSY-AGENT-GUIDE.md before starting.
  Do not modify the ProjectGypsy checkout, its records, or its Unity LeftBehind proof.
  Do not delete the Godot project or call it migrated before the Unity markers/captures pass.

  1. Preflight
  Verify Gypsy:

  Code block

      python "<GYPSY_HOME>/workstream/gypsy.py" version
      python "<GYPSY_HOME>/workstream/gypsy.py" check --project "<ULTRABLADE_HOME>"

  Inspect Unity:

  Code block

      "C:/Program Files/Unity 6000.6.7f1/Editor/Unity.exe" -version

  Licence must be active; LeftBehind proves the current licence.
  Windows Standalone Build Support is installed.
  Android Build Support is NOT installed. If mobile is in scope, install it through Unity Hub first. Until then,
  Unity mobile is not available and must not be claimed.

  Snapshot the destination:
  Git status must be clean or the existing changes must be explicitly owned.
  Create/keep a project manifest at the destination root.

  Inventory the source assets to port:
  Godot game scene/scripts under <GYPSY_HOME>/game/.
  Hero FBX <GYPSY_HOME>/assets/hero/wayfarer.fbx (23 bones, 3 clips, 512² maps).
  Hero LOD sources wayfarer_lod1.glb / wayfarer_lod2.glb — Unity does not import glTF natively; export them as FBX
  from Blender or add an approved glTF package. Do not assume Unity can load the GLBs.
  Textures, art brief, validation report and reference captures.

  2. Create the Unity project
  From the destination:

  Code block

      python "<GYPSY_HOME>/workstream/gypsy.py" project init       --path "<ULTRABLADE_HOME>"       --engine unity
      --version 6000.6.7f1 --targets pc

  Project init now defaults to Unity/PC when engine and version are omitted, but pass them explicitly so the manifest
  is unambiguous.

  Then:

  Code block

      python "<GYPSY_HOME>/workstream/gypsy.py" check --project "<ULTRABLADE_HOME>"

  Do not overwrite an existing project.gypsy.json. Add mobile to targets only when the Android module is installed
  and a real APK build is planned.

  3. Port gameplay first
  Port the same contract before art:
  a player CharacterController/CharacterBody equivalent with the same movement speed, gravity and autopilot;
  three collectibles and collection state;
  follow camera;
  the deterministic demo path that prints exactly:

  Code block

      GYPSY_DEMO_OK collected=3 frames=<n>

  and exits 0; failure prints GYPSY_DEMO_FAIL and exits 1;
  preserve the same gameplay acceptance: three collected, bounded frames, headless execution, exit code.

  Use the LeftBehind Unity project as the build/demo reference: <GYPSY_HOME>/unity/LeftBehind/ and
  <GYPSY_HOME>/toolpacks/leftbehind.unity.json. Do not copy it blindly; its scene and scripts are a minimal
  second-engine proof.

  4. Port the hero
  Import wayfarer.fbx into Unity; verify:
  23 bones incl. cape_a/b/c;
  clips idle/run/reach;
  Smile blend shape on the skinned mesh;
  512² base/normal/roughness maps.
  Build a prefab under the player visual. Preserve the CharacterController/collision. Recommended:
  Animator Controller with Idle/Run/Reach states;
  SkinnedMeshRenderer + blend shape proxy for Smile;
  LODGroup with LOD0/1/2 (export LOD1/2 to FBX or add glTF support);
  C# equivalents of the Godot systems: speed to idle/run, collect to Reach + Smile, distance to LOD, spring cape.
  Validate visually at the same ~1.9 m scale, feet on ground, forward direction matching the controller.

  5. Build, demo, capture
  Add a Unity Editor build method analogous to BuildLeftBehind.Build, and a Gypsy toolpack/slice with:
  build action: Unity batchmode BuildPipeline.BuildPlayer to Windows player;
  demo action: headless/player run, log artifact, marker validation;
  capture action: windowed run, screenshot/movie artifact, PNG validator.
  Required evidence:

  Code block

      a build success line in the log
      GYPSY_DEMO_OK collected=3 frames=<n>
      a 1280x720 capture with the hero visible

  Use a visible window for ScreenCapture; hidden-window capture failed in the LeftBehind work and is recorded there.

  6. Android/mobile (only after installing the module)
  Install Unity Android Build Support (SDK/NDK/JDK) via Unity Hub.
  Add the Android target and build a signed debug APK.
  Launch on an emulator or device and capture a screenshot plus logcat.
  Only then claim a mobile pass. If the launch is blocked, record the exact host/device failure; do not fake it.

  7. Gypsy integration
  Add capability manifests for the verified Unity systems (build/demo/capture/mobile).
  Add a Unity toolpack and slice with validators for the marker and capture.
  Update project.gypsy.json, project docs, AGENTS.md/target AGENTS.gypsy.md, records, and automation.
  Keep the Godot implementation as the reference until Unity passes all acceptance checks. Then make Unity the
  default for this project.

  Acceptance matrix
  Gate	Required evidence
  Project	project.gypsy.json pins Unity 6000.6.7f1/adapter and targets
  Build	Windows player built by batchmode, build log success
  Demo	exact GYPSY_DEMO_OK marker, exit 0
  Gameplay	3 collectibles, movement/autopilot, no regression
  Hero	23 bones, 3 clips, Smile, maps, LODs, visible capture
  Capture	1280x720 frame inspected
  Mobile	module installed, APK built, launched, screenshot/logcat
  Records	Gypsy checks pass and evidence is committed

  Stop conditions
  Stop and report the exact block if:
  Unity Android Build Support is absent and mobile is required;
  the hero FBX/LOD import fails;
  the marker cannot be produced;
  capture cannot show the hero;
  Gypsy or Unity cannot be found.

  Never substitute "it should work" for a run. Never mark Stage B complete from a passing manifest alone.
  ```

## 5 - 2026-10-26

- Request: "keep going. and make sure to push it to the repo"
- Status: Completed - committed the staged Stage B port as `afae480`, pushed it to `origin/main`, verified the CI run,
  and moved the acceptance matrix "Records: evidence is committed" row from PARTIAL to PASS.
- Version: 2026.10.26.005
- Commit/evidence: `afae4804690a0d1356ca981f02d047d3c75c418c` ("Port UltraBlade 2 to Unity 6000.6.7f1 with acceptance
  evidence", 128 files, +19,183/-114); `git push` output `3809c2c..afae480 main -> main` (exit 0); local HEAD =
  origin/main = `afae480`; `gh repo view C7-IMI/UltraBlade2` -> PUBLIC; GitHub Actions run `40408803952` ("checks")
  completed success in 14 s; the same commit's pre-commit hook ran the fast check list (PASS 5/5). This records/CI
  close-out is committed as "this change" (2026.10.26.005).

