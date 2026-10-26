# ULTRABLADE 2

The working home for **UltraBlade 2**.

The first game — a modern remake of the 1996 future-sport classic _HyperBlade_, built with Godot 4.7.2 — lives in
ProjectGypsy and is the **legacy/reference implementation**. This repository is the production Unity port of the same
gameplay contract and hero.

## Status

**Unity 6000.6.7f1, Windows PC.** The Unity project is [unity/UBlade2](./unity/UBlade2), pinned by
[project.gypsy.json](./project.gypsy.json). The three-collectible gameplay contract and the Wayfarer hero pass their
build, demo and capture gates; the full acceptance record, commands and measured evidence are in
[docs/UNITY-PORT.md](./docs/UNITY-PORT.md).

- **Gameplay:** CharacterController movement, autopilot, three crystals, follow camera, deterministic demo printing
  `GYPSY_DEMO_OK collected=3 frames=67` and exiting 0.
- **Hero:** 23 bones including `cape_a/b/c`, `idle`/`run`/`reach` clips, `Smile` blend shape, 512² maps, LODGroup
  1126/615/306 triangles, a generated hero prefab (`WayfarerLOD.prefab`) and spring cape.
- **Evidence:** Windows player build, headless demo, windowed 1280x720 capture inspected visually and measured by
  pixel analysis.
- **Mobile:** not available — Unity Android Build Support is not installed; the blocked state is recorded in
  `capabilities/engine.unity.port.mobile.json` and no APK is claimed (DEFERRED UB2-D13).
- **Work state:** committed and pushed as `afae480` (the port) and `abd433a` (records close-out); GitHub
  Actions `checks` runs `40408803952` and `40408942482` both succeeded, and the working tree is clean.

## Unity port at a glance

- [docs/UNITY-PORT.md](./docs/UNITY-PORT.md) — acceptance record, evidence, commands, limits.
- [unity/UBlade2](./unity/UBlade2) — the Unity 6000.6.7f1 project.
- [toolpacks/ublade2.unity.json](./toolpacks/ublade2.unity.json) — Gypsy toolpack (build, demo, capture, capture-check
  and their validators).
- [slices/ublade2.unity.json](./slices/ublade2.unity.json) — the Gypsy slice (Unity toolpack, then the check list).
- [capabilities](./capabilities) — Unity build/demo/capture and hero-import capability manifests.

    python scripts/gypsy_unity.py check
    python scripts/gypsy_unity.py toolpacks run ublade2.unity
    python scripts/gypsy_unity.py slice run ublade2.unity
    python scripts/check_all.py

## Working rules

This project is governed by the **ProjectPack**, copied from ProjectGypsy and applied here as revision UB2-1:

- [ProjectPack.md](./ProjectPack.md) — the standing rules; Parts 1-10 are the portable pack, Part 11 is this
  repository's overlay.
- [ProjectPack-NOTES.md](./ProjectPack-NOTES.md) — how the pack resolves for this repository.
- [ProjectPack-ADDENDUM-Interface.md](./ProjectPack-ADDENDUM-Interface.md) — interface design material, parked and
  not in force.
- [AGENTS.md](./AGENTS.md) — entry point for any model working here.
- [DEFERRED.md](./DEFERRED.md) — the work deliberately not done, with what unblocks it.

## Records and checks

Every prompt is logged in [Retrace.md](./Retrace.md); every change gets a computed version in
[BuildNotes.md](./BuildNotes.md). The one fast check list is `python scripts/check_all.py`, run by the tracked
pre-commit hook (install once with `python scripts/install-hooks.py`) and by GitHub Actions. It covers the records
guard, UTF-8/LF encoding, relative Markdown links, the local Gypsy manifest schemas and hook/CI caller coverage.
Engine gates (Unity build, demo, capture, pixel analysis) live in the Gypsy toolpack and slice, not in the fast list.

## Repository conventions

- Committed text is UTF-8 with LF line endings (`.gitattributes` enforces this).
- Unity `Library/`, `Temp/`, `Logs/`, `Build/` and transient engine output stay out of git (`.gitignore`).
- The acceptance evidence (1280x720 capture, demo/capture/build logs, hero import/runtime JSON, Gypsy state) is
  committed under `artifacts/ublade2/` and `artifacts/hero/` so the port's proof travels with the repository.
