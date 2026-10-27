# Release packaging - PC only

UltraBlade 2 is a **PC-only playable slice** at this milestone. The operator parked the mobile/Android target on
2026-10-26; the Android build evidence and capability remain in the repository for a future pass but are not part of
this release and are not claimed as complete.

`scripts/ublade2_release.ps1` builds (unless `-NoBuild`) and packages the Windows player, then writes the release
record. It does not publish, sign for release, or run the acceptance demo/capture gates.

## What a release contains

| Deliverable | Source | Signing |
| --- | --- | --- |
| `dist/UBlade2-<version>-win64.zip` | `unity/UBlade2/Build/` Windows player | unsigned development build |

The ZIP contains `UBlade2.exe`, `UBlade2_Data/`, `UnityPlayer.dll`, `MonoBleedingEdge/` and the Build-root `*.dll`
files. `Build/Android`, build logs and build outputs are not in it.

`artifacts/ublade2-release/` receives:

- `release.json` - the machine-readable stamp, schema `ublade2.release/v1`;
- `release.txt` - the same facts for a person;
- `build-windows.log` when the script ran the build.

## Reproduce

From the repository root, on Windows with Unity 6000.6.7f1:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1

To package the build already in `unity/UBlade2/Build/` without running Unity:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1 -NoBuild

Use `-Version <version>` to override the BuildNotes value. A successful run prints one line and exits 0:

    UBLADE2_RELEASE_OK version=<version> win_sha256=<sha256> package=dist/UBlade2-<version>-win64.zip

A failed run prints `UBLADE2_RELEASE_FAIL: <reason>` to stderr and exits nonzero; it does not print the success
marker. The script requires the Windows executable and payload, refuses a Windows ZIP below 20 MB, and requires a
git checkout for `git rev-parse HEAD`.

## Artifact identity stamping (ProjectPack 2.8)

Each release is identified by all of:

- the version, read from `BuildNotes.md` unless overridden;
- `generatedAt`, UTC in ISO-8601;
- `gitHead`, from `git rev-parse HEAD` at packaging time;
- `unityVersion`, from `unity/UBlade2/ProjectSettings/ProjectVersion.txt`;
- the byte size and SHA-256 of the Windows ZIP and the player executable;
- `platform: windows` and the explicit note that mobile is parked and absent.

The Unity bundle version is stamped from `BuildNotes.md`'s `Current version:` by `BuildUBlade2` and logged as
`UBLADE2_VERSION`; it is not hand-typed into the project.

## Identity limits (honest)

- The Windows build is an **unsigned development build**. Windows SmartScreen may warn; there is no code-signing
  certificate in this repository.
- There is no installer, auto-updater, store listing or license gate.
- The release stamp records `gitHead` but not whether the worktree was dirty; it is a record and review check, not
  an enforced anti-rebuild guard.
- A failed run leaves the previous `release.json`/`release.txt` in place; delete `artifacts/ublade2-release/` before
  a release run if a stale stamp would be misleading.

## What "playable build" means here

The Windows player is the playable production slice: main menu, league match against an AI, crystal scoring, strikes
and stuns, four arenas, procedural audio, settings, pause, results and a four-round season. The acceptance evidence
for the slice is in `docs/GAME-DESIGN.md`, `artifacts/ublade2-pro/` and the `ublade2.game` Gypsy toolpack/slice.

## Mobile

Mobile is **parked by operator decision** (2026-10-26, PC-only milestone). Do not attach an Android APK to a PC
release or claim a mobile pass from the earlier partial evidence. See `capabilities/engine.unity.port.mobile.json`
and `DEFERRED.md` (UB2-D15).
