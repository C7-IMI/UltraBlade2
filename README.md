# ULTRABLADE 2

The working home for **UltraBlade 2**.

The first game — a modern remake of the 1996 future-sport classic _HyperBlade_, built with Godot 4.7.2 — lives in
[C7-IMI/UltraBlade](https://github.com/C7-IMI/UltraBlade). This repository is the fresh workspace for the second
project.

## Status

Repository initialized on 2026-10-26 with the working rules, records and checks. There is **no game code yet**: the
engine, target and design are not chosen (see [DEFERRED.md](./DEFERRED.md)). Do not expect a build.

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
[BuildNotes.md](./BuildNotes.md). The one check list is `python scripts/check_all.py`, run by the tracked pre-commit
hook (install once with `python scripts/install-hooks.py`) and by GitHub Actions. It covers the records guard,
UTF-8/LF encoding, relative Markdown links and hook/CI caller coverage.

## Repository conventions

- Committed text is UTF-8 with LF line endings (`.gitattributes` enforces this).
- Generated output — builds, captures, local reference material — stays out of git (`.gitignore`).
