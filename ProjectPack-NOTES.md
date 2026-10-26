# ProjectPack — notes beside the pack

`ProjectPack.md` is the portable pack. This file is the only place the pack's placeholders, day-one decisions and
"which rules are in force" statement are resolved for **this** repository (§8.1, §8.2). The pack itself is not edited
here: the project's side lives here, and the pack's parked interface material lives in
[`ProjectPack-ADDENDUM-Interface.md`](./ProjectPack-ADDENDUM-Interface.md).

## This copy

| | |
| --- | --- |
| Held by | this repository (UltraBlade 2) |
| Base | ProjectGypsy's `ProjectPack.md` revision **PG-1**, 2026-10-25, SHA-256 `09AEA2CA0BBE30521413FD910C92080915E75A40D0706231A57170A85EF5AF1C` |
| Base of the base | C7NTRY's `ProjectPack.md` revision **2.5**, 2026-10-23, SHA-256 `FC5754DDCDB25535B6808042291C083B6B0BC13F27E36EC8035945AEB70ABE65` |
| This copy | revision **UB2-1**, 2026-10-26 — Parts 1–10 carried, the header re-authored, Part 11 re-authored for this repository |
| Interface material | **parked, not in force** — inherited from ProjectGypsy; `ProjectPack-ADDENDUM-Interface.md` (source SHA-256 `1FA4DD1D7ED0753CF635A0ACF8FBC1FD4F42DA922CA9EA30CD6AF7F974F39121`) |
| Product | a **game** — not a website, not a web application; the production pipeline is **Unity 6000.6.7f1, Windows PC**, pinned by `project.gypsy.json`. The first game (Godot 4.7.2) stays in ProjectGypsy as the reference; mobile is blocked (`DEFERRED.md` UB2-D13) |
| Last applied | 2026-10-26 — the pack was copied and applied in the change that created this file; Stage B was then completed against it (Unity 6000.6.7f1, PC), without changing the pack |

## What was carried, and what changed

Parts 1–10 are carried from ProjectGypsy's PG-1 copy with no portable rule rewritten. The interface design and
redesign rules stay **parked** in the addendum, exactly as they were parked in the source; this repository is a game,
so the reason for parking holds. Two things were re-authored for this repository:

1. **The header** — provenance, the "this copy" line and the revision history (§8.2 asks a copy to say whose rules it
   is and which half is aspirational).
2. **Part 11** — ProjectGypsy's Part 11 was that repository's own overlay; this repository carries a re-authored
   overlay with the same twelve rules and this repository's facts (engine pinned, the peer list, the deferred ids).

Nothing was declined or parked that ProjectGypsy had not already declined or parked. The interface addendum carries
only provenance-line edits (its title, "parked by" line, "why it is parked" line, the deferred id and the restore-step
revision number); every rule block in it is verbatim. The Unity port changed project facts, not the pack.

## §11 mapping — what ProjectGypsy's Part 11 became here

| ProjectGypsy rule | In this copy |
| --- | --- |
| 11.1 this repository is a game; interface rules parked | carried → **11.1** (and records the Unity port and the parked interface standard) |
| 11.2 verify before reporting | carried → **11.2** (verify the Unity build/demo/capture by running them) |
| 11.3 work in slices and keep going | carried → **11.3** |
| 11.4 records move with the work | carried → **11.4**; mechanised — the records files and `scripts/records_check.py` exist |
| 11.5 peer projects are separate | carried → **11.5**, peer list updated; ProjectGypsy is read-only for this repository |
| 11.6 nothing fake in the build | carried → **11.6** (mobile and unported features are reported as absent) |
| 11.7 every change ready to scale and to move | carried → **11.7** |
| 11.8 a retrofit is expand-only | carried → **11.8** |
| 11.9 the rollback documents | carried → **11.9** |
| 11.10 keeping this overlay true | carried → **11.10**, source named as ProjectGypsy PG-1 |
| 11.11 keep going: slice, delegate, one report | carried → **11.11** |
| 11.12 the operator's standing rules for autonomous runs | carried → **11.12** |
| 11.13 the C7NTRY rules this copy does not carry | carried → **11.13**, table re-checked against this repository |

## The placeholders, resolved

| Placeholder | In this repository |
| --- | --- |
| `<PROMPT_LOG>` | [`Retrace.md`](./Retrace.md) |
| `<CHANGELOG>` | [`BuildNotes.md`](./BuildNotes.md) |
| version script | [`scripts/next-version.py`](./scripts/next-version.py) |
| `<GUARD_LIST>` | [`scripts/check_all.py`](./scripts/check_all.py) — the fast list (5 checks) |
| `<CONTRACT_ARTEFACT>` | none yet — no published API (**UB2-D5**) |
| `<CONTRACT_GUIDE>` | the same |
| `<PRODUCT_DOCS>` | `README.md`, `AGENTS.md`, `docs/README.md`, `docs/UNITY-PORT.md`, this file, `ProjectPack.md`, `DEFERRED.md`; the relative-links guard runs in `scripts/check_all.py` |
| `<DEFERRED_REGISTER>` | [`DEFERRED.md`](./DEFERRED.md) |
| `<HEALTH_COMMAND>` | `python scripts/gypsy_unity.py toolpacks run ublade2.unity` (the engine gates; the fast tree check is `python scripts/check_all.py`) |
| `<SWEEP_COMMAND>` | none — the Unity player, demo and capture exit by themselves; no rig writes state |
| `<DESIGN_LANGUAGE>` | **removed** — interface material is parked (addendum, **UB2-D3**) |
| `<MOCKUP_DIR>` | **removed** — parked with the design material (**UB2-D3**) |

## §8.1 — the day-one decisions, answered

| The decision | This repository's answer |
| --- | --- |
| Which engine and targets, and where the manifest lives | **Unity 6000.6.7f1, PC**; `project.gypsy.json` at the repository root (adapter unity-6, targets pc). Mobile is absent until Android Build Support exists (**UB2-D13**). The Unity project is `unity/UBlade2`; `paths.game` and `paths.artifacts` in the manifest point there. |
| Where the prompt log and changelog live, and how the version is computed | [`Retrace.md`](./Retrace.md), [`BuildNotes.md`](./BuildNotes.md) and [`scripts/next-version.py`](./scripts/next-version.py); guarded by [`scripts/records_check.py`](./scripts/records_check.py) |
| Which generated copies exist, and who regenerates them | No generated **documentation** copies. The Unity player build, demo log and capture are generated by `scripts/ublade2_unity.ps1` into `artifacts/ublade2/` and validated by the Gypsy toolpack; the hero import report is written by the build (`artifacts/ublade2/hero-import.json`). The acceptance evidence artifacts are committed (force-added) while transient Unity `Library/`/`Build/` stay ignored |
| The single check list, and how the hook and CI are kept in step | [`scripts/check_all.py`](./scripts/check_all.py) is the one fast list (records, encoding, links, local Gypsy manifest schemas, hook/CI caller coverage); [`.githooks/pre-commit`](./.githooks/pre-commit) and [`.github/workflows/checks.yml`](./.github/workflows/checks.yml) both run it, and its caller-coverage guard compares them. The heavy Unity gates are the Gypsy toolpack/slice, not the fast list (**UB2-D9**) |
| The published contract artefact and what checks it | Not applicable yet — no API exists (**UB2-D5**) |
| Where the product's own documentation lives, and what keeps it reachable | `README.md`, `AGENTS.md`, `docs/README.md`, `docs/UNITY-PORT.md` and the pack files; `scripts/check_all.py` resolves every relative Markdown link |
| The design language, and what guards it | **Parked** — the web design rules are in the addendum and not in force (**UB2-D3**). No game-side standard has been chosen; inventing one is an operator decision |
| How many interfaces and themes exist, and what they are called | Not decided; parked with the interface material. The Unity port has no menu/HUD yet (**UB2-D14**) |
| Whether the product runs as more than one instance, and what makes scheduled work safe | Nothing runs as a service; the Unity player is a local game. When a server exists it answers the four questions (§2.20, Part 11.7) |
| Which bug classes this codebase actually produces | No finding history yet. The Unity port's first recorded behaviours were a null graphics device in headless capture (avoided by a visible window) and a Blender glTF helper mesh leaking into the FBX (stripped in the converter) |
| Where the scratch rigs live, and how they are reaped | None; the Unity player exits itself and the PowerShell wrapper waits for it |
| Whether anything commits on somebody's behalf | No. Commits are made by a person or a session; no auto-publisher exists (**UB2-D10** not triggered) |
| Which layers need a rollback document | None yet (**UB2-D6**); `ProjectPack.md` §2.13 and Part 11.9 state what each will need |
| The token and context budget, and which loop is the expensive one | Measured on this machine: Unity batchmode build ≈ 70 s; headless demo ≈ 5 s; windowed capture ≈ 10 s; the fast check list < 2 s. No cache exists |
| The register of deliberately deferred work | [`DEFERRED.md`](./DEFERRED.md) |
| Which artefacts are stamped, and who bumps the stamp | None — nothing is distributed (**UB2-D11**) |
| What is checked before a deploy, without a subscription | Nothing deployed, no templates, no infrastructure (**UB2-D12**) |
| What runs on the operator's machine, and how it is registered | The Unity editor install and the Git hook `core.hooksPath=.githooks` (installed once by `scripts/install-hooks.py`); no scheduled tasks or services (**UB2-D10**) |
| Whether the parked interface rules are restored, and which ones | **No**. The addendum is not in force until a recorded decision restores it or replaces it with a game-side standard (**UB2-D3**) |

## §2.31 — which rules are in force today (applied 2026-10-26, pack UB2-1)

This repository is **a game with rules, records, one fast check list, a Unity 6000.6.7f1 Windows port whose
build/demo/capture gates pass, and a Gypsy toolpack/slice for the engine gates**. Mobile is blocked and is not
claimed; the full production game surface is not ported (**UB2-D14**).

- **In force as conduct, not mechanised:** Part 1's laws; §2.1–§2.4 records and prose (records guarded); §2.5 one
  source; §2.6–§2.8 (when assets or distributables exist); §2.13 reversibility; §2.14–§2.16 permissions, sweeps and
  secrets; §2.17–§2.19 reproducibility, evidence and budget; §2.20 scale-and-move; §2.21–§2.30 operations (when a
  deploy or running service exists); §2.31 itself; §2.32 text and encoding; §2.33/§2.34 peer separation and context
  limits; Part 10 (operator's rules); Part 12; and Part 11 in this copy.
- **Mechanised today:** `python scripts/check_all.py` — records guard, tracked-text encoding, relative Markdown
  links, the local Gypsy manifest schemas (toolpacks, slices, capabilities) and the hook/CI caller-coverage guard.
  The engine gates are mechanised through Gypsy: `python scripts/gypsy_unity.py toolpacks run ublade2.unity` and
  `python scripts/gypsy_unity.py slice run ublade2.unity` (real Unity build, demo, capture and pixel analysis with
  state under `workstream/state/`). The mobile path has the same treatment through
  `toolpacks/ublade2.unity.mobile.json` and `slices/ublade2.unity.mobile.json` (Android APK build, adb install/launch,
  on-device markers; a clear visual capture is outstanding, UB2-D15). `python scripts/gypsy_unity.py check` runs
  Gypsy's project check.
- **Parked — not in force:** every interface design and redesign rule, plus the browser-verification rule. The full
  list is in the addendum and in the table above. They are not to be applied, quoted as requirements, or partially
  restored without the recorded decision the addendum asks for.
- **Not triggered yet:** §2.8/§2.21 (nothing distributed or deployed), §2.25 health (no service), §2.29/§2.30
  (no auto-publisher, no machine automation), §2.24/§2.26 rigs and residue (no rig), §2.13 (no configurable layer).
- **Deliberately not carried:** the C7NTRY rules listed in `ProjectPack.md` Part 11.13.

## §8.2 — the inventory of check kinds, filled or declined

Part 9 is the menu. This repository builds the rows whose subject exists; the rest are named with their deferred id so
a missing check is visible rather than silent.

| Check kind | Status | What compares / why not |
| --- | --- | --- |
| records | **PASS** | `scripts/records_check.py` checks `BuildNotes.md` newest-first, its header and `Retrace.md` numbering and statuses |
| newest-first / version computation | **PASS** | records check + `scripts/next-version.py` |
| encoding | **PASS** | `scripts/check_all.py` checks tracked text index blobs are UTF-8 with no BOM/CR |
| documentation links | **PASS** | `scripts/check_all.py` resolves every relative Markdown link |
| local Gypsy manifests | **PASS** | `scripts/check_all.py` validates toolpack/slice/capability schemas, ids, argv and artifact paths, validator and step kinds; the mobile capability is recorded as partial (build/install/launch/markers verified, clear visual capture outstanding) |
| hook/CI caller coverage | **PASS** | `scripts/check_all.py` compares the hook's and CI's invocation of the list |
| Unity Android build / install / launch | **PASS, via the mobile slice** | `toolpacks/ublade2.unity.mobile.json` + `slices/ublade2.unity.mobile.json`: Android APK build, adb install/launch, on-device markers and the deterministic demo marker. A clear visual capture is outstanding (UB2-D15) and is not claimed |
| Unity build / demo / capture | **PASS, via the Gypsy slice** | `toolpacks/ublade2.unity.json` + `slices/ublade2.unity.json`: batchmode build, headless marker, windowed capture, hero pixel analysis, with Gypsy state and hashes. Not in the fast list because it needs Unity and a licence (**UB2-D9**) |
| generated artefacts | **PASS, via the toolpack** | the build, demo log and capture are generated by scripts into gitignored `artifacts/ublade2/` and validated by the toolpack; no generated documentation artefact exists |
| hero import | **PASS, via the build's import report** | `hero-import.json`: 23 bones, clips, Smile, three 512² maps, LOD triangles; the report is a JSON validator target of the toolpack |
| secret scan | not yet | no code or configuration carries a credential yet — **UB2-D8** |
| contract / curated prose | not yet | no API — **UB2-D5** |
| deploy templates, auto-publish gate, leak sweep, audit sweep, artefact stamp, health, token lint | n/a | each has no subject in this repository yet; the token-lint and browser rows are **parked** with the interface material |

## How the pack is enforced here

The fast list is `scripts/check_all.py`, run by the tracked pre-commit hook and the CI caller:

    python scripts/check_all.py

The engine gates run through Gypsy with this repository as the project root:

    python scripts/gypsy_unity.py check
    python scripts/gypsy_unity.py capabilities
    python scripts/gypsy_unity.py toolpacks check
    python scripts/gypsy_unity.py toolpacks run ublade2.unity
    python scripts/gypsy_unity.py slice run ublade2.unity

The adapter `scripts/gypsy_unity.py` imports Gypsy's modules read-only and never writes to the Gypsy checkout; its
state lives under this repository's `workstream/state/`. There is no second fast list.

## How this application of the pack was verified

| Claim | Observation |
| --- | --- |
| The source is ProjectGypsy's pack PG-1 | SHA-256 of the source file at copy time: `09AEA2CA0BBE30521413FD910C92080915E75A40D0706231A57170A85EF5AF1C`; the copy's header records it |
| Only the header and Part 11 differ from the source | an independent line comparison of `ProjectPack.md` against the source marks no changed line outside the header block and the Part 11 block |
| The addendum is the source with provenance lines only | source SHA-256 `1FA4DD1D7ED0753CF635A0ACF8FBC1FD4F42DA922CA9EA30CD6AF7F974F39121`; the changed lines are the parked line, the copy sentence, the "why" sentence, the deferred id and the restore-step revision number |
| Text is UTF-8 without a BOM and has no CRLF | `python scripts/check_all.py` → `PASS encoding` line |
| Links resolve | `python scripts/check_all.py` → `PASS links` line |
| The local Gypsy manifests are valid | `python scripts/check_all.py` → `PASS manifests` line; Gypsy's own validator agrees (`UBLADE2_CAPABILITIES_OK count=5 errors=0`, toolpacks check errors=0) |
| The hook and CI run the same list | `python scripts/check_all.py` → `PASS callers` line |
| The records agree | `python scripts/check_all.py` → `PASS records` line |
| The Unity port passes its gates | `UBLADE2_BUILD Succeeded bytes=97444534`, `GYPSY_DEMO_OK collected=3 frames=67` exit 0, capture-check passed; full evidence in `docs/UNITY-PORT.md` |
| Not verified, and not claimed | mobile (Android Build Support absent, UB2-D13); the rest of the production game (UB2-D14); no contract artefact (UB2-D5); no deploy (UB2-D12) |

## Restoring the interface material

See `ProjectPack-ADDENDUM-Interface.md` → "Restoring it". In short: decide whether the web rules, a game-side
standard or part of either is wanted; record it here under §8.1 (**UB2-D3**); put the sections back with their stubs,
renumbering the Part 11 rules (ProjectGypsy's 11.1/11.4/11.5/11.10/11.11 now belong to this copy's re-authored
Part 11) and renaming section C (this copy holds §2.13 under it); remove the parked statements; bump this copy's
revision (UB2-2); re-run the verification above.
