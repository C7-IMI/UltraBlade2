# ULTRABLADE 2

The working home for **UltraBlade 2**.

The first game — a modern remake of the 1996 future-sport classic _HyperBlade_, built with Godot 4.7.2 — lives in
ProjectGypsy and is the **legacy/reference implementation**. This repository is the production Unity port of the same
gameplay contract and hero.

## Status

**Unity 6000.6.7f1, PC only.** The Unity project is [unity/UBlade2](./unity/UBlade2), pinned by
[project.gypsy.json](./project.gypsy.json). After researching the original 1996 PC game HyperBlade (manual, help file,
shipped CD data, videos, screenshots and the three operator-provided YouTube references), the playable slice now
follows that sport: an **elliptical drome**, a carried/thrown **rok**, 1/2/4-point goals through a **multiplier**,
2 forwards + AI goalie with a bench, jak attacks, three periods, a **14-team Transnational HyperBlade League** and the
Gauntlet ladder, with a stamped Windows release. The port's original three-collectible demo and Wayfarer hero capture
remain green as the legacy gate. Reference/fidelity mapping: [docs/HYPERBLADE-REFERENCE.md](./docs/HYPERBLADE-REFERENCE.md);
acceptance record: [docs/UNITY-PORT.md](./docs/UNITY-PORT.md). The mobile target is parked by operator decision
(PC-only milestone, UB2-D15).

- **Playable game:** HyperBlade-style 2-vs-2 skaters plus AI goalies on a **110 m elliptical half-pipe bowl** (40 m
  faceted shell, 6 m curved floor), rok pickups, charged power shots, quick shots, passes, jak attacks, speedburst,
  substitutions, drome hazards and multiplier gates; 3 periods, 14 teams with per-player ratings and per-team
  dromes, Gauntlet progression, full stats screen, HUD scoreboard/period pips/roster health/shot+rear-view
  cams/radar, and an 85-degree player-following chase camera. Design/controls:
  [docs/GAME-DESIGN.md](./docs/GAME-DESIGN.md); research/fidelity:
  [docs/HYPERBLADE-REFERENCE.md](./docs/HYPERBLADE-REFERENCE.md); REA demo analysis:
  [docs/REA-HYPERBLADE-ANALYSIS.md](./docs/REA-HYPERBLADE-ANALYSIS.md); UI standard:
  [docs/UI-STANDARD.md](./docs/UI-STANDARD.md).
- **Legacy port gate:** CharacterController movement, autopilot, three crystals, follow camera, deterministic demo
  printing `GYPSY_DEMO_OK collected=3` and exiting 0 on the final build (`UBLADE2_DEMO_OK exit=0`).
- **Hero:** 23 bones including `cape_a/b/c`, `idle`/`run`/`reach` clips, `Smile` blend shape, 512² maps, LODGroup
  1126/615/306 triangles, a generated hero prefab (`WayfarerLOD.prefab`) and spring cape.
- **Evidence:** Windows player build, headless demo, windowed 1280x720 capture inspected visually and measured by
  pixel analysis.
- **Release:** **PC-only** stamped release `UBLADE2_RELEASE_OK version=2026.10.26.012` (Windows ZIP
  `dist/UBlade2-2026.10.26.012-win64.zip`, SHA-256 `7eeff05760b54c2ef670ce4631b8fb0c8ce47695973a40e747003b63317b329f`);
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
- [docs/REA-HYPERBLADE-ANALYSIS.md](./docs/REA-HYPERBLADE-ANALYSIS.md) — REA analysis of the demo plus the camera/arena/decomp findings used for the fidelity fix.
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
