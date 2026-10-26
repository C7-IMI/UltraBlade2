# AGENTS.md — ULTRABLADE 2

Entry point for any model working in this repository. Read this file, then the documents it names, before acting.

## What this repository is

**UltraBlade 2** is the next UltraBlade game project. Its **production pipeline is Unity 6000.6.7f1 on Windows PC**:
the new Unity project lives at [unity/UBlade2](./unity/UBlade2) and is pinned by
[project.gypsy.json](./project.gypsy.json) (engine unity, version 6000.6.7f1, adapter unity-6, targets pc and mobile).

The first game — a modern remake of the 1996 future-sport classic *HyperBlade*, built with Godot 4.7.2 — lives in
ProjectGypsy and remains the **legacy/reference implementation**. It was not deleted, modified or "migrated": this is
a port with acceptance evidence, recorded in [docs/UNITY-PORT.md](./docs/UNITY-PORT.md).

**Mobile is partially verified.** Unity Android Build Support is installed and a signed debug APK is built,
installed and launched on the local API 35 emulator; on-device logcat shows `UBLADE2_START`, `UBLADE_HERO_OK` and the
deterministic `GYPSY_DEMO_OK collected=3`. The emulator's Vulkan surface returns a near-black frame to `adb screencap`,
so a clear mobile visual capture and physical-device launch are outstanding (UB2-D15). Do not claim a full mobile pass
until that is captured.

## What governs your work, in this order

1. **[ProjectPack.md](./ProjectPack.md)** — the standing rules, this copy's revision **UB2-1**. Parts 1–10 are the
   portable pack; **Part 11 is this repository's overlay**.
2. **[ProjectPack-NOTES.md](./ProjectPack-NOTES.md)** — how the pack resolves *here*: the placeholders, the §8.1
   answers, the §8.2 inventory of checks, which rules are actually in force, and the §11 mapping. When the pack and
   this repository differ, this file says how, and it is the file to correct if it is wrong.
3. **[ProjectPack-ADDENDUM-Interface.md](./ProjectPack-ADDENDUM-Interface.md)** — interface design/redesign
   material, **parked and not in force**. Read its first page so you know what is parked. Do not apply it, quote it
   as a requirement, or restore any of it without the recorded operator decision it asks for.
4. **[DEFERRED.md](./DEFERRED.md)** — the work deliberately not done. Read the entry covering an area before you
   change that area, and do not quietly close a deferred item by implementing it halfway.
5. **[Retrace.md](./Retrace.md)** and **[BuildNotes.md](./BuildNotes.md)** — the prompt log and the changelog.
   Every prompt is logged; every change gets a script-computed version.
6. **[scripts/check_all.py](./scripts/check_all.py)** — the one fast check list: records, encoding, links, local
   Gypsy manifest schemas, hook/CI caller coverage. The tracked pre-commit hook and the GitHub Actions caller both
   run it. Engine gates (Unity build, demo, capture) are not in it; they live in the Gypsy toolpack/slice.
7. **[docs/UNITY-PORT.md](./docs/UNITY-PORT.md)** — the Unity port acceptance record: gates, measured evidence,
   commands, provenance and limits.

## Use Gypsy Studio (Gypsy)

This project is developed with **Gypsy Studio** (short: **Gypsy**), the local workstream/orchestration tool. Do not
reimplement its probes, checks, slices, toolpacks or evidence by hand.

**Invocation trigger:** any imperative invocation verb followed by "Gypsy" — Open, Load, Use, Invoke, Launch, Start,
Call, Run, Show, Import, Activate, Bring up — means load/use Gypsy. Negated requests do not trigger.

- **Open / Launch / Show / Bring up Gypsy:** verify Gypsy, ensure the dashboard is up, report
  `http://127.0.0.1:8765/` and summarize state.
- **Load / Import Gypsy:** load/verify Gypsy and import the guide/template above.
- **Use / Run / Invoke Gypsy:** route the current task through the Gypsy CLI; for this repository start with
  `python scripts/gypsy_unity.py check`.

### Locate Gypsy

    GYPSY_HOME = C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy

Verify before starting:

    python "$env:GYPSY_HOME\workstream\gypsy.py" version

If Gypsy cannot be found, stop and report the block. Do not silently substitute ad-hoc shell orchestration, and do
not fake the dashboard, the commands or the state.

### The project-local adapter (important boundary)

Gypsy's own CLI resolves toolpacks, slices and capabilities from its checkout and hard-codes that checkout as the
project root. This repository must **not modify GYPSY_HOME**, so `scripts/gypsy_unity.py` imports Gypsy's modules
read-only (bytecode writing disabled) and runs **this repository's** manifests with `project_root=<this repo>`:

    python scripts/gypsy_unity.py check
    python scripts/gypsy_unity.py capabilities
    python scripts/gypsy_unity.py toolpacks list
    python scripts/gypsy_unity.py toolpacks check
    python scripts/gypsy_unity.py toolpacks run ublade2.unity
    python scripts/gypsy_unity.py slice run ublade2.unity

Do not add toolpacks, slices or capabilities to the Gypsy checkout. This repository's manifests live in
[toolpacks/](./toolpacks), [slices/](./slices) and [capabilities](./capabilities) and are validated with Gypsy's own
validators and state schemas.

### Existing project check

`project.gypsy.json` exists. Do not overwrite it. Read it, then run:

    python scripts/gypsy_unity.py check

### New project bootstrap

Only for a **new** project: `python "$env:GYPSY_HOME\workstream\gypsy.py" project init --path <dir> --engine <id>
--version <version> --targets <targets>`, then inspect the manifest and run `gypsy check --project <dir>`. Do not
re-bootstrap this repository.

## The rules that bind every turn

- **Never claim a check you did not run.** Report exactly what was verified, by what command, and what was not
  (§1.10, Part 11.2).
- **Verify by running.** The Unity player build, headless demo and windowed capture are the evidence: use
  `scripts/ublade2_unity.ps1 -Mode build|demo|capture` or the Gypsy toolpack/slice. Read back the frame that was
  captured; do not describe it from expectation.
- **This is a game, not a web app.** No design-language, mockup-first, visual-bar, viewport, token-lint,
  accessibility-pass or browser-gate rule applies until the addendum is restored or replaced by a recorded decision
  (Part 11.1, UB2-D3).
- **Mobile is partially verified, not a full pass.** Android Build Support is installed; a signed debug APK builds,
  installs and launches on the local API 35 emulator, and on-device logcat shows `UBLADE2_START`, `UBLADE_HERO_OK` and
  `GYPSY_DEMO_OK collected=3`. The emulator's Vulkan surface returns a near-black `adb screencap`, so a clear mobile
  visual capture and a physical-device run are outstanding (UB2-D15). Do not claim a full mobile visual pass.
- **Do not modify the Gypsy checkout.** It is a read-only peer for this repository; work only here. Its records and
  its Godot game stay as they are.
- **Record the work.** A change is not finished until its [Retrace.md](./Retrace.md) and
  [BuildNotes.md](./BuildNotes.md) entries exist, with a version from
  [scripts/next-version.py](./scripts/next-version.py) (§2.1). The operator controls commits: do not commit or push
  unless asked.
- **One fast list of checks:** [scripts/check_all.py](./scripts/check_all.py); the tracked pre-commit hook and the CI
  caller both run it, and its coverage guard compares the two (§2.22–§2.23). Never keep a second list; the heavy
  engine gates belong to the Gypsy slice.
- **Peer projects are separate** (Part 11.5): ProjectGypsy, UltraBlade (the first game), LeftBehind and the C7-IMI
  projects are peers. A prompt asked here is logged here; nothing crosses without explicit instruction.
- **Nothing fake in the build** (Part 11.6): no dead controls, no pretend features; unavailable behaviour says so.
- **Background agents are evidence only after they are read** (§10.8). A dispatch is not progress; read the status
  *and* the work before recording it.
- **Handle text carefully** (§2.32): UTF-8 without a BOM, no round-trip through PowerShell, read back what you wrote.

## First action in a new session

1. Read this file, `ProjectPack-NOTES.md` §2.31, `DEFERRED.md` and `docs/UNITY-PORT.md`.
2. Check `git status` and the newest records entry; if the records do not exist yet, say so rather than pretending.
3. Run `python scripts/check_all.py` and report what it prints. If the hook is not installed in this clone, run
   `python scripts/install-hooks.py` once.
4. For engine work, run `python scripts/gypsy_unity.py check` and then the relevant toolpack/slice; never trust a
   build claim that did not come from a run.
5. Ask what the session is for, then run the matching prompt from `ProjectPack.md` Part 3 — **P2** onward. The **P1**
   bootstrap is complete. Prompts P4 and P16 are parked; do not renumber the others.

## What exists, and what does not

**Exists:** the Unity 6000.6.7f1 project `unity/UBlade2`, Android Build Support with a signed debug APK installed and
launched on a local API 35 emulator (on-device markers verified), the three-collectible gameplay demo, the Wayfarer hero port
(23 bones, three clips, Smile, three 512² maps, LODGroup, generated hero prefab `WayfarerLOD.prefab`, spring cape), the Windows player build, the headless demo
marker, the inspected 1280x720 capture, the Gypsy toolpack/slice/capability manifests, the port acceptance record,
the working rules, the records, the fast check list, the tracked hook and the CI workflow.

**Does not exist:** a clear mobile visual capture, a physical-device run (UB2-D15), the full production game surface
(HUD, menu, pause/settings, audio, combat, teams, league content), a published contract artefact, rollback documents
for a configurable layer, deploy preflight, a secret scanner, or a mechanised peer-project boundary guard. Do not claim any
of them; the missing ones are in [DEFERRED.md](./DEFERRED.md) with the condition that unblocks them.
