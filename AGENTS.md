# AGENTS.md — ULTRABLADE 2

Entry point for any model working in this repository. Read this file, then the documents it names, before acting.

## What this repository is

**UltraBlade 2** is the next UltraBlade game project. The first game — a modern remake of the 1996 future-sport
classic *HyperBlade*, built with Godot 4.7.2 — lives in [C7-IMI/UltraBlade](https://github.com/C7-IMI/UltraBlade).
This repository was created on 2026-10-26 and currently holds the **working rules, records and checks**, not a game.
The engine, target and game design are **not chosen yet** ([`DEFERRED.md`](./DEFERRED.md) UB2-D1, UB2-D2): do not
describe game content, a build, a test suite or a playable slice that does not exist.

## What governs your work, in this order

1. **[`ProjectPack.md`](./ProjectPack.md)** — the standing rules, this copy's revision **UB2-1**. Parts 1–10 are the
   portable pack; **Part 11 is this repository's overlay**.
2. **[`ProjectPack-NOTES.md`](./ProjectPack-NOTES.md)** — how the pack resolves *here*: the placeholders, the §8.1
   answers, the §8.2 inventory of checks, which rules are actually in force, and the §11 mapping. When the pack and
   this repository differ, this file says how, and it is the file to correct if it is wrong.
3. **[`ProjectPack-ADDENDUM-Interface.md`](./ProjectPack-ADDENDUM-Interface.md)** — interface design/redesign
   material, **parked and not in force**. Read its first page so you know what is parked. Do not apply it, quote it
   as a requirement, or restore any of it without the recorded operator decision it asks for.
4. **[`DEFERRED.md`](./DEFERRED.md)** — the work deliberately not done. Read the entry covering an area before you
   change that area, and do not quietly close a deferred item by implementing it halfway.
5. **[`Retrace.md`](./Retrace.md)** and **[`BuildNotes.md`](./BuildNotes.md)** — the prompt log and the changelog.
   Every prompt is logged; every change gets a script-computed version.
6. **[`scripts/check_all.py`](./scripts/check_all.py)** — the one check list. The tracked pre-commit hook and the
   GitHub Actions caller both run it.

## Use Gypsy Studio (Gypsy)

This project is developed with **Gypsy Studio** (short: **Gypsy**), the local workstream/orchestration tool. Do not
reimplement its probes, checks, slices, toolpacks or evidence by hand.

**Invocation trigger:** any imperative invocation verb followed by "Gypsy" — Open, Load, Use, Invoke, Launch, Start,
Call, Run, Show, Import, Activate, Bring up — means load/use Gypsy. Negated requests do not trigger.

- **Open / Launch / Show / Bring up Gypsy:** verify Gypsy, ensure the dashboard is up, report
  `http://127.0.0.1:8765/` and summarize state.
- **Load / Import Gypsy:** load/verify Gypsy and import the guide/template above.
- **Use / Run / Invoke Gypsy:** route the current task through the Gypsy CLI; start with
  `gypsy check --project <this repo>` (once a manifest exists).

### Locate Gypsy

    GYPSY_HOME = C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy

Verify before starting:

    python "$env:GYPSY_HOME\workstream\gypsy.py" version

If Gypsy cannot be found, stop and report the block. Do not silently substitute ad-hoc shell orchestration, and do
not fake the dashboard, the commands or the state.

### New project bootstrap

There is **no `project.gypsy.json` here yet**, on purpose: the engine, version and targets have not been decided
(**UB2-D1**). When the operator decides, initialize with the actual engine rather than a guess:

    python "$env:GYPSY_HOME\workstream\gypsy.py" project init \
      --path "<this repository>" --engine <godot|unity|ue5|flax> --version <version> --targets <targets>

Then inspect the manifest and run:

    python "$env:GYPSY_HOME\workstream\gypsy.py" check --project "<this repository>"

Use the installed engine the operator actually selected; do not pretend an engine is available.

### Existing project check

Once `project.gypsy.json` exists, do not overwrite it. Read it, then run
`python "$env:GYPSY_HOME\workstream\gypsy.py" check --project "<this repository>"`.

## The rules that bind every turn

- **Never claim a check you did not run.** Report exactly what was verified, by what command, and what was not
  (§1.10, Part 11.2).
- **This is a game, not a web app.** No design-language, mockup-first, visual-bar, viewport, token-lint,
  accessibility-pass or browser-gate rule applies until the addendum is restored or replaced by a recorded decision
  (Part 11.1, UB2-D3). If a request needs interface design, say the standard is parked and treat the choice as the
  operator's — do not invent one.
- **Verify by running.** There is no build yet: say so rather than implying a verification. Once a build exists,
  verify by running it and reading back what you captured (§2.18).
- **Record the work.** A change is not finished until its [`Retrace.md`](./Retrace.md) and
  [`BuildNotes.md`](./BuildNotes.md) entries exist, with a version from
  [`scripts/next-version.py`](./scripts/next-version.py) (§2.1).
- **One list of checks:** [`scripts/check_all.py`](./scripts/check_all.py); the tracked pre-commit hook and the CI
  caller both run it, and its coverage guard compares the two (§2.22–§2.23). Never keep a second list.
- **Peer projects are separate** (Part 11.5): ProjectGypsy, UltraBlade, LeftBehind and the C7-IMI projects are
  peers. A prompt asked here is logged here; nothing crosses without explicit instruction.
- **Nothing fake in the build** (Part 11.6): no dead controls, no pretend features; unavailable behaviour says so.
- **Background agents are evidence only after they are read** (§10.8). A dispatch is not progress; read the status
  *and* the work before recording it.
- **Handle text carefully** (§2.32): UTF-8 without a BOM, no round-trip through PowerShell, read back what you wrote.

## First action in a new session

1. Read this file, `ProjectPack-NOTES.md` §2.31 and `DEFERRED.md`.
2. Check `git status` and the newest records entry; if the records do not exist yet, say so rather than pretending.
3. Run `python scripts/check_all.py` and report what it prints. If the hook is not installed in this clone, run
   `python scripts/install-hooks.py` once.
4. Ask what the session is for, then run the matching prompt from `ProjectPack.md` Part 3 — **P2** onward. The **P1**
   bootstrap is complete; do not re-bootstrap. Prompts P4 and P16 are parked; do not renumber the others.

## What exists, and what does not

**Exists:** the public repository, this instruction file, the pack copy and its notes, the parked interface addendum,
the deferred register, the prompt log, the changelog, the one check list, the tracked hook and the CI workflow.

**Does not exist:** an engine decision, a `project.gypsy.json` manifest, a game tree, game code, assets, a build, a
test suite, a playable build, interface design or a game-design document. Do not claim any of them. What is missing
is in [`DEFERRED.md`](./DEFERRED.md) with the condition that unblocks it.
