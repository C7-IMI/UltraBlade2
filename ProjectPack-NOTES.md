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
| Product | a **game** — not a website, not a web application; the engine and target are not selected yet (`DEFERRED.md` UB2-D1) |
| Last applied | 2026-10-26 — the pack was copied and applied in the change that created this file |

## What was carried, and what changed

Parts 1–10 are carried from ProjectGypsy's PG-1 copy with no portable rule rewritten. The interface design and
redesign rules stay **parked** in the addendum, exactly as they were parked in the source; this repository is a game,
so the reason for parking holds. Two things were re-authored for this repository:

1. **The header** — provenance, the "this copy" line and the revision history (§8.2 asks a copy to say whose rules it
   is and which half is aspirational).
2. **Part 11** — ProjectGypsy's Part 11 was that repository's own overlay; this repository carries a re-authored
   overlay with the same twelve rules and this repository's facts (no engine yet, the peer list, the deferred ids).

Nothing was declined or parked that ProjectGypsy had not already declined or parked. The interface addendum carries
only provenance-line edits (its title, "parked by" line, "why it is parked" line, the deferred id and the restore-step
revision number); every rule block in it is verbatim.

## §11 mapping — what ProjectGypsy's Part 11 became here

| ProjectGypsy rule | In this copy |
| --- | --- |
| 11.1 this repository is a game; interface rules parked | carried → **11.1** (and records that engine/target are not chosen) |
| 11.2 verify before reporting | carried → **11.2** (adds: no game build exists, so no game claim) |
| 11.3 work in slices and keep going | carried → **11.3** |
| 11.4 records move with the work | carried → **11.4**; now mechanised — the records files and `scripts/records_check.py` exist |
| 11.5 peer projects are separate | carried → **11.5**, peer list updated for this repository |
| 11.6 nothing fake in the build | carried → **11.6** |
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
| `<GUARD_LIST>` | [`scripts/check_all.py`](./scripts/check_all.py) — the pack-wide list |
| `<CONTRACT_ARTEFACT>` | none yet — no published API (**UB2-D5**) |
| `<CONTRACT_GUIDE>` | the same |
| `<PRODUCT_DOCS>` | [`README.md`](./README.md), [`AGENTS.md`](./AGENTS.md), this file, `ProjectPack.md`, `DEFERRED.md`; the relative-links guard runs in `scripts/check_all.py` |
| `<DEFERRED_REGISTER>` | [`DEFERRED.md`](./DEFERRED.md) |
| `<HEALTH_COMMAND>` | `python scripts/check_all.py` (no game health command exists yet — **UB2-D2**) |
| `<SWEEP_COMMAND>` | none — no test rig writes state yet |
| `<DESIGN_LANGUAGE>` | **removed** — interface material is parked (addendum, **UB2-D3**) |
| `<MOCKUP_DIR>` | **removed** — parked with the design material (**UB2-D3**) |

## §8.1 — the day-one decisions, answered

| The decision | This repository's answer |
| --- | --- |
| Where the prompt log and changelog live, and how the version is computed | [`Retrace.md`](./Retrace.md), [`BuildNotes.md`](./BuildNotes.md) and [`scripts/next-version.py`](./scripts/next-version.py); guarded by [`scripts/records_check.py`](./scripts/records_check.py) |
| Which generated copies exist, and who regenerates them | No generated **documentation** copies yet (there is no generated documentation artefact). A game's export and captures will be generated and gitignored once a game exists — **UB2-D4** |
| The single check list, and how the hook and CI are kept in step | [`scripts/check_all.py`](./scripts/check_all.py) is the one list; [`.githooks/pre-commit`](./.githooks/pre-commit) and [`.github/workflows/checks.yml`](./.github/workflows/checks.yml) both run it, and its caller-coverage guard compares them |
| The published contract artefact and what checks it | Not applicable yet — no API exists (**UB2-D5**) |
| Where the product's own documentation lives, and what keeps it reachable | `README.md`, `AGENTS.md` and the pack files; `scripts/check_all.py` resolves every relative Markdown link |
| The design language, and what guards it | **Parked** — the web design rules are in the addendum and not in force (**UB2-D3**). No game-side standard has been chosen; inventing one is an operator decision |
| How many interfaces and themes exist, and what they are called | Not decided; parked with the interface material. Do not claim interface facts until the game's interface standard exists |
| Whether the product runs as more than one instance, and what makes scheduled work safe | Nothing runs yet; when a server exists it answers the four questions (§2.20, Part 11.7) |
| Which bug classes this codebase actually produces | None recorded yet; there is no defect history to classify. The sweep table starts empty and grows from the first real findings |
| Where the scratch rigs live, and how they are reaped | None started by this repository's work |
| Whether anything commits on somebody's behalf | No. Commits are made by a person or a session; no auto-publisher exists (**UB2-D10** is not triggered) |
| Which layers need a rollback document | None yet (**UB2-D6**); `ProjectPack.md` §2.13 and Part 11.9 state what each will need |
| The token and context budget, and which loop is the expensive one | Not measured at scale. The expensive step today is reading the 1,535-line pack; the bootstrap itself is seconds. No cache exists |
| The register of deliberately deferred work | [`DEFERRED.md`](./DEFERRED.md) |
| Which artefacts are stamped, and who bumps the stamp | None — nothing is distributed (**UB2-D11**) |
| What is checked before a deploy, without a subscription | Nothing deployed, no templates, no infrastructure (**UB2-D12**) |
| What runs on the operator's machine, and how it is registered | Nothing yet; the repo pre-commit hook is installed via `core.hooksPath=.githooks` by `scripts/install-hooks.py` once, and that is the only machine registration (**UB2-D10**) |
| Whether the parked interface rules are restored, and which ones | **No**. The addendum is not in force until a recorded decision restores it or replaces it with a game-side standard (**UB2-D3**) |

## §2.31 — which rules are in force today (applied 2026-10-26, pack UB2-1)

This repository is **a game with rules, records, one check list, a tracked hook and a CI gate — and no game code
yet**. There is no engine decision, no manifest, no game tree, no build and no test suite; nothing here claims one.

- **In force as conduct, not mechanised:** Part 1's laws; §2.1–§2.4 records and prose (records now guarded);
  §2.5 one source (nothing generated yet); §2.6–§2.8 (when assets or distributables exist); §2.13 reversibility;
  §2.14–§2.16 permissions, sweeps and secrets (no subject yet); §2.17–§2.19 reproducibility, evidence and budget;
  §2.20 scale-and-move; §2.21 and §2.22–§2.30 operations (when a build, deploy or running service exists);
  §2.31 itself; §2.32 text and encoding; §2.33/§2.34 peer separation and context limits; Part 10 (operator's
  rules); Part 12; and Part 11 in this copy.
- **Mechanised today:** `python scripts/check_all.py` — records guard, tracked-text encoding, relative Markdown
  links, and the hook/CI caller-coverage guard. The pre-commit hook and the CI workflow both run that same list.
- **Parked — not in force:** every interface design and redesign rule, plus the browser-verification rule. The full
  list is in the addendum and in the table above. They are not to be applied, quoted as requirements, or partially
  restored without the recorded decision the addendum asks for.
- **Not triggered yet:** §2.8/§2.21 (nothing distributed or deployed), §2.25 health (nothing runs), §2.29/§2.30
  (no auto-publisher, no machine automation), §2.24/§2.26 rigs and residue (no rig), §2.13 (no configurable layer).
- **Deliberately not carried:** the C7NTRY rules listed in `ProjectPack.md` Part 11.13.

## §8.2 — the inventory of check kinds, filled or declined

Part 9 is the menu. This repository builds only the rows whose subject exists; the rest are named with their
deferred id so a missing check is visible rather than silent.

| Check kind | Status | What compares / why not |
| --- | --- | --- |
| records | **PASS** | `scripts/records_check.py` checks `BuildNotes.md` newest-first, its header and `Retrace.md` numbering and statuses |
| newest-first / version computation | **PASS** | records check + `scripts/next-version.py` |
| encoding | **PASS** | `scripts/check_all.py` checks tracked text index blobs are UTF-8 with no BOM/CR |
| documentation links | **PASS** | `scripts/check_all.py` resolves every relative Markdown link |
| hook/CI caller coverage | **PASS** | `scripts/check_all.py` compares the hook's and CI's invocation of the list |
| secret scan | not yet | no code or configuration carries a credential yet — **UB2-D8** |
| typecheck / build / tests | not yet | no game code, engine or test suite exists — **UB2-D2**, **UB2-D9** |
| generated artefacts | not yet | nothing is generated yet — **UB2-D4** |
| contract / curated prose | not yet | no API — **UB2-D5** |
| deploy templates, auto-publish gate, leak sweep, audit sweep, artefact stamp, health, token lint | n/a | each has no subject in this repository yet; the token-lint and browser rows are **parked** with the interface material |

## How the pack is enforced here

The pack has one enforced list, `scripts/check_all.py`, run by the tracked pre-commit hook and the CI caller:

```text
python scripts/check_all.py
python scripts/records_check.py       # the records guard on its own
python scripts/next-version.py        # the next computed version
```

The hook is installed once per clone with `python scripts/install-hooks.py` (it sets `core.hooksPath=.githooks`);
CI runs the same list on every push and pull request. There is no second list.

## How this application of the pack was verified

| Claim | Observation |
| --- | --- |
| The source is ProjectGypsy's pack PG-1 | SHA-256 of the source file at copy time: `09AEA2CA0BBE30521413FD910C92080915E75A40D0706231A57170A85EF5AF1C`; the copy's header records it |
| Only the header and Part 11 differ from the source | an independent line comparison of `ProjectPack.md` against the source marks no changed line outside the header block and the Part 11 block |
| The addendum is the source with provenance lines only | source SHA-256 `1FA4DD1D7ED0753CF635A0ACF8FBC1FD4F42DA922CA9EA30CD6AF7F974F39121`; the changed lines are the parked line, the copy sentence, the "why" sentence, the deferred id and the restore-step revision number |
| Text is UTF-8 without a BOM and has no CRLF | `python scripts/check_all.py` → `PASS encoding` line (see the bootstrap report) |
| Links resolve | `python scripts/check_all.py` → `PASS links` line |
| The hook and CI run the same list | `python scripts/check_all.py` → `PASS callers` line |
| Every check can fail | a throwaway copy of the tree with a broken BuildNotes header, a staged BOM plus a broken link, and a hook argument produced `FAIL records`, `FAIL encoding`, `FAIL links` and `FAIL callers` — `checks: FAIL (0/4 passed)` — before the copy was deleted |
| The records agree | `python scripts/check_all.py` → `PASS records` line |
| Not verified, and not claimed | there is no game build, test suite, engine decision, `project.gypsy.json` or design document yet (**UB2-D1**, **UB2-D2**); no Gypsy check has been run against this repository |

## Restoring the interface material

See `ProjectPack-ADDENDUM-Interface.md` → "Restoring it". In short: decide whether the web rules, a game-side
standard or part of either is wanted; record it here under §8.1 (**UB2-D3**); put the sections back with their stubs,
renumbering the Part 11 rules (ProjectGypsy's 11.1/11.4/11.5/11.10/11.11 now belong to this copy's re-authored
Part 11) and renaming section C (this copy holds §2.13 under it); remove the parked statements; bump this copy's
revision (UB2-2); re-run the verification above.
