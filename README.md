# ULTRABLADE 2

The working home for **UltraBlade 2**.

The first game — a modern remake of the 1996 future-sport classic _HyperBlade_, built with Godot 4.7.2 — lives in
ProjectGypsy and is the **legacy/reference implementation**. This repository is the production Unity port of the same
gameplay contract and hero.

## Status

**Unity 6000.6.7f1, PC only.** The Unity project is [unity/UBlade2](./unity/UBlade2), pinned by
[project.gypsy.json](./project.gypsy.json). The original three-collectible contract and Wayfarer hero gates still pass,
and a **playable production slice** now ships: main menu, league matches against an AI, crystal scoring, strikes and
stuns, four arenas, procedural audio, settings, pause, results and a four-round season, with a stamped Windows release.
The full acceptance record, commands and measured evidence are in [docs/UNITY-PORT.md](./docs/UNITY-PORT.md); the
mobile target is parked by operator decision (PC-only milestone, UB2-D15).

- **Playable game:** main menu, settings, league table/season, AI opponent, three-crystal scoring, strike/stun combat,
  dash, four arenas, HUD/score/timer, procedural audio, pause and result screens. Design and controls:
  [docs/GAME-DESIGN.md](./docs/GAME-DESIGN.md); UI standard: [docs/UI-STANDARD.md](./docs/UI-STANDARD.md).
- **Legacy port gate:** CharacterController movement, autopilot, three crystals, follow camera, deterministic demo
  printing `GYPSY_DEMO_OK collected=3` and exiting 0 on the final build (`UBLADE2_DEMO_OK exit=0`).
- **Hero:** 23 bones including `cape_a/b/c`, `idle`/`run`/`reach` clips, `Smile` blend shape, 512² maps, LODGroup
  1126/615/306 triangles, a generated hero prefab (`WayfarerLOD.prefab`) and spring cape.
- **Evidence:** Windows player build, headless demo, windowed 1280x720 capture inspected visually and measured by
  pixel analysis.
- **Release:** **PC-only** stamped release `UBLADE2_RELEASE_OK version=2026.10.26.008` (Windows ZIP
  `dist/UBlade2-2026.10.26.008-win64.zip`, SHA-256 `fef1d1b4be9c23f38633ffd568f8d81a6b0e43d6ae0c916112ec0d0cf80d7355`);
  see [docs/RELEASE.md](./docs/RELEASE.md) and `artifacts/ublade2-release/release.json`.
- **Mobile:** parked by operator decision (2026-10-26, PC-only milestone). Earlier partial Android build/launcher
  evidence is preserved but is not part of the release and is not claimed complete (UB2-D15); see
  `capabilities/engine.unity.port.mobile.json`.
- **Verification:** `checks: PASS (7/7)`, 12 local Gypsy manifests valid (3 toolpacks, 3 slices, 6 capabilities);
  match/league/UI/release evidence is in `artifacts/ublade2-pro/` and `artifacts/ublade2-release/`.
- **Work state:** committed and pushed; GitHub Actions `checks` is green and the working tree is clean. The current
  commit hashes and CI runs are recorded in [Retrace.md](./Retrace.md) and [BuildNotes.md](./BuildNotes.md).

## Unity port at a glance

- [docs/UNITY-PORT.md](./docs/UNITY-PORT.md) — acceptance record, evidence, commands, limits.
- [unity/UBlade2](./unity/UBlade2) — the Unity 6000.6.7f1 project.
- [toolpacks/ublade2.unity.json](./toolpacks/ublade2.unity.json) — Gypsy toolpack (build, demo, capture, capture-check
  and their validators).
- [slices/ublade2.unity.json](./slices/ublade2.unity.json) — the Gypsy slice (Unity toolpack, then the check list).
- [toolpacks/ublade2.game.json](./toolpacks/ublade2.game.json) — playable-slice toolpack (build, legacy demo/capture,
  match, league save test, match/menu/UI captures and validators).
- [slices/ublade2.game.json](./slices/ublade2.game.json) — the playable-slice Gypsy slice.
- [capabilities](./capabilities) — Unity port, mobile (parked) and playable-slice capability manifests.

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
