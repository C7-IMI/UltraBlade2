# DEFERRED.md — the work deliberately not done

The register §2.28 requires: what it is, where it is, why it is deferred, what would unblock it, **what would break
if somebody fixed it casually**, and who owns the decision. Read the entry covering an area before you change that
area. An item is closed only with its evidence.

This repository is a game project with working rules, records, one fast check list, a Unity 6000.6.7f1 Windows port
that passes its build/demo/capture gates, and a Gypsy toolpack/slice for the engine gates. The port is a vertical
slice of the gameplay contract and the hero — not the full production game. Nothing below is claimed as built.

| ID | What is deferred | Where | Why deferred | Unblocks when | What breaks if fixed casually | Owner |
| --- | --- | --- | --- | --- | --- | --- |
| **UB2-D3** | The game's interface design and UI/UX standard | `ProjectPack-ADDENDUM-Interface.md` | the web pack's interface rules were parked because this is a game; no game-side standard exists yet (see `ProjectPack-NOTES.md` §8.1) | the operator decides whether to restore the addendum, replace it with a game-side standard, or defer further | a half-restored rule contradicts Part 11.1 and the parked inventory | operator |
| **UB2-D5** | A generated contract artefact and its guide | docs | no API or published interface exists; if the game never exposes one this stays closed as "not applicable" | the game exposes an API or message contract | a hand-written contract drifts from the API the moment the API exists | operator decision when an API appears |
| **UB2-D6** | Rollback documents for configurable layers, and the first application of the expand-only rule | docs | the Unity port has no user-configurable layer yet (no settings screen, no feature flags) | the first configurable layer (graphics/difficulty/feature flag) or live data exists | a rollback document for a layer that does not exist is fiction | the change that adds it |
| **UB2-D7** | A mechanised peer-project boundary guard | scripts | Part 11.5's guard needs a peer list and records to compare; the boundary is a discipline, not a check | the first guard implementation | a guard written before the records settle compares nothing and reports green | next build slice |
| **UB2-D8** | Secret scan over the working tree and history | scripts | no code or configuration carries a credential yet | the first code or configuration key exists | a scanner added later cannot undo a credential already committed; add it before the first secret-bearing file | the change that adds the first credential-bearing configuration |
| **UB2-D9** | Unity engine gates in CI | .github/workflows | the Unity build/demo/capture gates need a machine with Unity 6000.6.7f1 and an active licence; the fast check list deliberately stays engine-free, and the gates run through the Gypsy slice (`python scripts/gypsy_unity.py slice run ublade2.unity`). A manifest-schema check was added to the fast list instead | a self-hosted runner (or CI image) with Unity and an activated licence exists | putting a Unity build in a hosted runner without a licence produces a red gate nobody can fix | operator / CI owner |
| **UB2-D10** | Machine-level automation: scheduled tasks, boot scripts, runner shims | scripts or startup | nothing runs on the operator's machine as part of this project yet | something needs to run on a schedule or at boot | automation registered without its script is a machine-level orphan | the change that needs it |
| **UB2-D11** | Stamped distributable artefacts | build | nothing is distributed | the first installer or client build | shipping a changed payload under an old identity (§2.8) | the release slice |
| **UB2-D12** | Deploy preflight, environment contract and workflow-shell checks | .github, scripts | nothing is deployed and there is no infrastructure | the first deployable service or template exists | a preflight for a deploy that does not exist is decoration | the change that adds a deployment |
| **UB2-D13** | Unity Android Build Support, a signed debug APK and a launched device/emulator pass | Unity editor install, `project.gypsy.json` targets | Android Build Support is **not installed**; `Editor/Data/PlaybackEngines` contains only `windowsstandalonesupport` and `WebGLSupport`. Mobile was not required for this port, so no APK was built and no device evidence exists | install Unity Android Build Support (SDK/NDK/JDK) through Unity Hub, add the Android target, build a signed debug APK, launch it on a clean host/device and capture a screenshot plus logcat | claiming a mobile pass from a manifest, a build alone or an emulator that never launched is false evidence (the LeftBehind work recorded exactly that emulator failure). The blocked state is recorded in `capabilities/engine.unity.port.mobile.json`, maturity `blocked` | operator |
| **UB2-D14** | The rest of the production game: HUD, menu, pause/settings, audio, combat/teams, league content and arena art | `unity/UBlade2` | the instruction was the three-collectible contract and the hero with acceptance evidence; the Unity project is that vertical slice, not a port of every Godot feature | the next production slice is planned against the reference game | porting surface features before the movement/collect/hero contract is stable multiplies rework and hides the port's real gaps | operator / next slice |

## Standing note

The interface design/redesign material is **parked, not deferred work to complete**: it is complete and preserved
verbatim in `ProjectPack-ADDENDUM-Interface.md`. UB2-D3 is the decision that either restores it, replaces it with a
game-side standard, or leaves it parked longer. Do not treat "parked" as "forgotten", and do not partially apply it.
The Unity port is **not** "mobile ready" and the Godot implementation is **not** "migrated": it remains in
ProjectGypsy as the reference.

## Closed

| ID | Closed on | Evidence |
| --- | --- | --- |
| **UB2-D1** the engine/target decision and the project manifest | 2026-10-26 | `project.gypsy.json` pins engine unity 6000.6.7f1 (adapter unity-6) and target pc; `python scripts/gypsy_unity.py check` prints `checks: PASS, capabilities 50, tools 16, project UBlade2` (exit 0). Mobile is deliberately absent (UB2-D13). |
| **UB2-D2** the game tree, build, demo and capture verification | 2026-10-26 | `unity/UBlade2` builds and runs: `UBLADE2_BUILD Succeeded bytes=97444534`, demo `GYPSY_DEMO_OK collected=3 frames=67` exit 0, 1280x720 capture inspected and pixel-checked. Evidence in `docs/UNITY-PORT.md` and `artifacts/ublade2/`. |
| **UB2-D4** generated documentation copies and the game's generated artefacts | 2026-10-26 | the Unity build, demo log and capture are generated by scripts (`scripts/ublade2_unity.ps1`, `scripts/check_hero_capture.py`) into `artifacts/ublade2/` (acceptance evidence committed; transient Unity output ignored) and validated by the Gypsy toolpack; there is still no generated documentation artefact to copy. |
