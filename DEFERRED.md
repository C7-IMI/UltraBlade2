# DEFERRED.md - the work deliberately not done

The register (section 2.28) requires: what it is, where it is, why it is deferred, what would unblock it, what would
break if somebody fixed it casually, and who owns the decision. Read the entry covering an area before you change
that area. An item is closed only with its evidence.

This repository is a PC-only Unity 6000.6.7f1 game project: a working rules pack, records, one fast check list, a
playable production slice (menu, league matches against an AI, crystal scoring, strikes/stuns, four arenas, procedural
audio, settings, pause, results, four-round season) and a stamped Windows release. The mobile target is parked by
operator decision. Nothing below is claimed as built unless its row says how it was verified.

## Open

No open deferred items remain as of 2026-10-26. The remaining limits are stated in `docs/UNITY-PORT.md` section 8 and
in the closed rows below (notably the engine-gate CI activation and the parked mobile target).

## Closed in the 2026-10-26 production slice pass

| ID | What was closed | Closed on | Evidence / decision |
| --- | --- | --- | --- |
| **UB2-D3** | Game interface design/UI-UX standard | 2026-10-26 | `docs/UI-STANDARD.md` records the game-side standard (HUD hierarchy, safe areas, 44px touch targets, menu->match->pause->result state machine); it replaces the parked web addendum as the recorded decision. |
| **UB2-D5** | Generated contract artefact | 2026-10-26 | `docs/CONTRACT-DECISION.md`: the game exposes no external API or message contract, so the requirement is not applicable; a trigger for reopening is recorded. |
| **UB2-D6** | Rollback documents for the first configurable layers | 2026-10-26 | `docs/ROLLBACK.md` covers the shipped PlayerPrefs settings keys (`ublade2.settings.*`) and league save (`ublade2.league.v1`), plus the expand-only rule. Runtime proof: `powershell -File scripts/ublade2_unity.ps1 -Mode league-test` -> `UBLADE2_LEAGUE_SAVE_OK round=1 wins=1 points=3 passed=True` (`artifacts/ublade2-pro/league-save.json`). |
| **UB2-D7** | Mechanised peer-project boundary guard | 2026-10-26 | `scripts/check_peer_boundary.py` reads the peers list in `project.gypsy.json`, verifies existence/HEAD/cleanliness, and is part of `scripts/check_all.py` (`PASS peers - PEER_BOUNDARY_OK peers=1`). |
| **UB2-D8** | Secret scan | 2026-10-26 | `scripts/check_secrets.py` scans tracked files for high-confidence secret patterns, has a `--self-test`, and is part of `check_all.py` (`PASS secrets`). It is in place before any credential-bearing file. |
| **UB2-D9** | Unity engine gates in CI | 2026-10-26 | `.github/workflows/engine.yml` adds the dispatch-only `engine-checks` workflow on a `[self-hosted, unity]` runner (check + `toolpacks run ublade2.unity` + `slice run ublade2.unity`). Activation is honest: no self-hosted Unity-licensed runner exists yet, so the workflow has **not run**; the hosted fast CI and local engine gates remain the actual evidence. |
| **UB2-D10** | Machine-level automation | 2026-10-26 | Decision: not applicable. The game and its release are operator-driven; no scheduled task, boot script or runner shim is needed. Reopen when something must run on a schedule or at boot. |
| **UB2-D11** | Stamped distributable artefact | 2026-10-26 | `scripts/ublade2_release.ps1` builds and stamps a PC-only release. Evidence: `UBLADE2_RELEASE_OK version=2026.10.26.008 package=dist/UBlade2-2026.10.26.008-win64.zip`, ZIP 50,871,958 bytes SHA-256 `fef1d1b4be9c23f38633ffd568f8d81a6b0e43d6ae0c916112ec0d0cf80d7355`, player EXE SHA-256 `9806abde4a406ed68647e9505852c49f6aa72a372f4f084e2aa2821affea57a2`, Unity bundle version `2026.10.26.008`. `artifacts/ublade2-release/release.json` carries version, git HEAD, Unity version and both hashes. |
| **UB2-D12** | Deploy preflight and environment contract | 2026-10-26 | Decision: not applicable. This is an offline single-player game; there is no deployable service, template or infrastructure. Reopen with the first deployed backend. |
| **UB2-D14** | The rest of the production game surface | 2026-10-26 | The playable slice now ships HUD, main menu, settings, pause, results, league table/season, AI opponent, strikes/stuns, four arenas and procedural audio. Evidence: `UBlade2.exe` build `97543422` bytes; demo `GYPSY_DEMO_OK collected=3`; match `UBLADE2_MATCH_OK player=1 ai=2 winner=2 frames=176 phase=Ended`; six 1280x720 UI screenshots (`artifacts/ublade2-pro/ui/`, contact sheet included); `UI_CAPTURES_CHECK_OK`; PC-only release packaged. Imported arena art, online play, a deeper roster and licensed media remain future production work, recorded in `docs/GAME-DESIGN.md` rather than silently claimed. |
| **UB2-D15** | Mobile target | 2026-10-26 | **Parked by operator decision: UltraBlade 2 is PC-only for this milestone.** Earlier work is preserved but not claimed complete: Android Build Support installed, debug APK built/installed/launched on an API 35 emulator, on-device markers captured, and an in-app framebuffer capture exists with clear HUD/touch controls but a dark world under the emulator Vulkan surface. `capabilities/engine.unity.port.mobile.json` is `planned`; mobile is not in the release. |

## Earlier closed items

| ID | Closed on | Evidence |
| --- | --- | --- |
| **UB2-D1** the engine/target decision and the project manifest | 2026-10-26 | `project.gypsy.json` pins engine unity 6000.6.7f1 (adapter unity-6), targets pc, lists ProjectGypsy under peers; `python scripts/gypsy_unity.py check` passes. |
| **UB2-D2** the game tree, build, demo and capture verification | 2026-10-26 | `unity/UBlade2` builds and runs; demo `GYPSY_DEMO_OK collected=3`, 1280x720 hero capture inspected and pixel-checked (`CAPTURE_CHECK_OK`), evidence in `artifacts/ublade2/`. |
| **UB2-D4** generated documentation copies and the game's generated artefacts | 2026-10-26 | generated build/demo/capture evidence is produced by the wrappers into `artifacts/` and validated by the Gypsy game toolpack; there is no generated documentation artefact to copy. |
| **UB2-D13** Android Build Support and a launched emulator pass | 2026-10-26 | Unity CLI installed Android Build Support; `com.c7imi.ublade2` debug APK built and launched on AVD `UBlade2_API35` with `UBLADE2_START`, `UBLADE_HERO_OK` and `GYPSY_DEMO_OK collected=3` in logcat. Parked with the mobile target. |

## Standing note

The interface addendum remains parked and preserved verbatim; `docs/UI-STANDARD.md` is the active game-side standard.
The ProjectGypsy Godot implementation was not modified, deleted or 'migrated'; it remains the reference. Mobile is
parked by the operator decision of 2026-10-26 and must not be claimed as a completed mobile release.
