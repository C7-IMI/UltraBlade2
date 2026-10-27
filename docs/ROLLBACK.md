# Rollback plan - settings, league save and build identity

This is the rollback record for the first configurable layers in the shipped playable slice. It describes what is
actually stored by the current build, not an aspirational schema. The implementation lives in
unity/UBlade2/Assets/Scripts/GameTypes.cs (settings) and LeagueData.cs (season).

## 1. What rollback means

- Never start with deletion. Back up first, identify the build, then change one layer at a time.
- A rollback that cannot preserve a save must say so before it runs; it must not pretend the save survived.
- Peer repositories are never part of a rollback and are never written to.
- The preferred fix for a bad release is usually roll-forward: ship a compatible build with fixed behaviour.

## 2. Player settings - PlayerPrefs keys

Settings are stored in Unity PlayerPrefs under the fixed prefix `ublade2.settings.`. Keys are never renamed or
re-typed inside this namespace. A breaking change uses a new prefix (for example `ublade2.settings.v2.`) and keeps
the old keys readable by the older build.

| Key | Type | Default | Accepted | Meaning |
| --- | --- | --- | --- | --- |
| `ublade2.settings.master` | float | 0.85 | 0.0-1.0 | Master output volume. |
| `ublade2.settings.music` | float | 0.50 | 0.0-1.0 | Music volume. |
| `ublade2.settings.sfx` | float | 0.80 | 0.0-1.0 | Effects volume. |
| `ublade2.settings.difficulty` | int | 2 | 1-3 | Preferred difficulty label (the league opponent sets the actual match difficulty). |
| `ublade2.settings.touch` | int | 1 on mobile, 0 on desktop first run | 0 or 1 | Show touch controls in a match. |
| `ublade2.settings.fullscreen` | int | 0 | 0 or 1 | Fullscreen preference. |

Read/write rules:

- Missing keys or out-of-range values fall back to the documented default and are clamped; they do not crash.
- Settings are loaded before the first interactive frame and saved on change and on quit.
- Unknown `ublade2.settings.*` keys are preserved by the current build (it never enumerates and deletes them).
- `PlayerPrefs.DeleteAll()` is forbidden. Reset writes the documented defaults for the known keys (or deletes only
  those exact keys) after asking for confirmation.
- Do not put credentials, purchases, progression authority or anything security-sensitive into PlayerPrefs.

## 3. League save - PlayerPrefs key `ublade2.league.v1`

The league season is one JSON string stored in PlayerPrefs under the single key `ublade2.league.v1` (constant
`LeagueData.SaveKey`). It is not a separate JSON file and there is no `.bak` file. The JSON is a Unity
`JsonUtility` document over the save fields:

    { round, wins, losses, points, results, lastResult }

- `round` is the 0-based index of the next league round; `results` is the per-round result array.
- The value is written with PlayerPrefs.Save() (one flush) after a resolved ranked match, a new season, or on quit.
- The file location on Windows is the Unity PlayerPrefs registry key below; on Android it is the shared_prefs XML
  for the package. There is no external-files league JSON.
- Empty or invalid JSON deserialises to a fresh season; the current build does not quarantine a bad value because it
  cannot distinguish one bad key from a first run. Back up the PlayerPrefs store before changing a build (below).

Storage locations:

- Windows player registry: `HKCU\Software\C7-IMI\UBlade2` (PlayerPrefs values under the value names above).
- Unity editor: `HKCU\Software\Unity\UnityEditor\C7-IMI\UBlade2`.
- Android: `shared_prefs/com.c7imi.ublade2.v2.playerprefs.xml` in the private app data; readable with a debug
  build through `adb shell run-as com.c7imi.ublade2`.

## 4. Build and version identity

A build is identified by the whole tuple, not by the product name alone:

| Field | Current value / source |
| --- | --- |
| Unity bundle version | `Current version:` in BuildNotes.md, copied by BuildUBlade2 at build time (logged as `UBLADE2_VERSION`). |
| Android version code | 1 (ProjectSettings.asset). |
| Git commit | `git rev-parse HEAD` at build time, recorded in the release stamp. |
| Build time | UTC timestamp in the release stamp. |
| Artifact | player executable or APK path, plus its SHA-256. |
| Data schemas | settings prefix and league save shape described above. |

`scripts/ublade2_release.ps1` writes the stamped release record to `artifacts/ublade2-release/release.json`
(schema `ublade2.release/v1`). A rollback record uses the actual artifact hash; it must not invent a stamp. The
Windows build is an unsigned development build and the Android APK uses the debug key, as documented in
docs/RELEASE.md.

## 5. Expand-only rule

- New save/settings fields are optional for old readers and have defaults when absent.
- Never rename or re-type a field in place. Add a new field or a new namespace/version.
- A breaking change gets a new identity (settings prefix v2, league save key v2, or a new bundle version code).
- Old builds must keep working against the old fields; new builds read old data and write only additive fields.

## 6. Rolling back a bad settings or save release

1. Identify the build from the release stamp (version, git HEAD, hashes).
2. Back up the PlayerPrefs store: copy the registry key on Windows (`reg export`) or the shared_prefs XML on Android
   before changing anything.
3. If only settings are bad, overwrite the six documented keys with their defaults and restart the game.
4. If a save is suspected bad, restore the backed-up PlayerPrefs value. If no valid copy exists, delete only the
   `ublade2.league.v1` key (never DeleteAll) and start a fresh season; say so in the release note.
5. Re-run the matching gate: `powershell -File scripts/ublade2_unity.ps1 -Mode league-test` (save/load marker
   `UBLADE2_LEAGUE_SAVE_OK`) and the interactive smoke check on the candidate build.

## 7. What breaks if this is done casually

- `PlayerPrefs.DeleteAll()` wipes every key, including keys another component or a newer build owns.
- Deleting the PlayerPrefs key while the game is running lets the running game overwrite the change on quit.
- Writing a screenshot or file into the PlayerPrefs store is not supported; use the app's persistent data path
  for diagnostic files (the mobile in-app capture writes `ublade2-mobile-frame.png` there, not a save).
- Treating the local save as an authority lets a hand-edited value rewrite league progress; it is single-player local
  data and is not an anti-cheat boundary.

## 8. Verification commands

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_unity.ps1 -Mode league-test
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_unity.ps1 -Mode build
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1 -NoBuild
    reg query "HKCU\Software\C7-IMI\UBlade2"

On Android with a debug build and adb connected:

    adb shell run-as com.c7imi.ublade2 cat shared_prefs/com.c7imi.ublade2.v2.playerprefs.xml

Expected league self-test marker: `UBLADE2_LEAGUE_SAVE_OK round=<n> wins=1 points=3 key=ublade2.league.v1 passed=True`.

## 9. Closure and review

- Closes UB2-D6: the first configurable layers (settings and league save) now have a rollback record and an
  automated save/load check.
- Reopen when a second configurable layer (graphics preset, feature flag, live data) is added, or when the save
  format changes identity.
