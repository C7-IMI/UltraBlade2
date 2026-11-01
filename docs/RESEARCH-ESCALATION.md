# Research escalation — ProjectPack rule 10.15

When a task cannot be completed with the current tools or methods, do not stop at "can't". Research
additional tools, plugins, extensions, resources and methods, install and verify them when approved,
then extend Gypsy with the reusable part.

## Trigger

- A real attempt has failed, or the current tool set cannot produce the required result.
- The operator has granted full approval, or has approved the specific install/elevation.
- The work is blocked but potentially solvable with a different instrument or method.

## Workflow

1. **Write down the blocker.** What exactly failed, with evidence: command, render, error, measurement.
2. **Research broadly.**
   - official docs / engine manuals / tool documentation;
   - plugin, extension and asset catalogs;
   - community write-ups, ArtStation/80.lv/GDC talks, comparable shipped work;
   - papers/talks for the technique, not just the tool.
3. **Prefer what is installed.** Probe versions and capabilities before installing anything.
4. **Install with provenance.** Check licence, version, source, install path and rollback. Record the
   exact install/use command.
5. **Run a verification spike.** Prove the new tool/method on a small case before touching the
   production asset.
6. **Extend Gypsy** with the reusable part, without modifying `GYPSY_HOME`:
   - a capability manifest in `capabilities/`;
   - a toolpack action in `toolpacks/`;
   - a slice in `slices/` when the work is repeatable end-to-end;
   - a script when the action needs one.
7. **Verify.** Run `scripts/research_inventory.py`, `python scripts/gypsy_unity.py capabilities`,
   `python scripts/gypsy_unity.py toolpacks check`, the new toolpack/slice, and
   `python scripts/check_all.py`.
8. **Record.** Add the Retrace entry and BuildNotes version; state what was found, installed,
   used, rejected and what remains blocked.

## This repository's extension

| Piece | Path |
| --- | --- |
| Capability | `capabilities/process.research.escalation.json` |
| Toolpack | `toolpacks/ublade2.research.json` |
| Slice | `slices/ublade2.research.json` |
| Inventory script | `scripts/research_inventory.py` |
| Inventory artifact | `artifacts/research/inventory.json` |

Run:

    python scripts/research_inventory.py --out artifacts/research/inventory.json
    python scripts/gypsy_unity.py capabilities
    python scripts/gypsy_unity.py toolpacks check
    python scripts/gypsy_unity.py toolpacks run ublade2.research
    python scripts/gypsy_unity.py slice run ublade2.research
    python scripts/check_all.py

## What the inventory contains

- Resources: web research, Blender, SculptForge + Bridge, Blender add-ons, the researched
  **Blender Copilot `sculpt_brush_stroke`** bmesh-brush method, the official Blender sculpt
  operator API, Python/Pillow, and the project-local Gypsy adapter.
- Methods: blocker statement, broad research, installed-first probe, provenance install,
  verification spike, world-space bmesh brush strokes for local forms, Gypsy extension, records
  and check list.
- Limits: external accounts/credentials/subscriptions still need the operator; never modify
  the Gypsy checkout; never copy peer-project or unlicensed assets.

## Example: the skater helmet block

The helmet work failed repeatedly with scripted loft/boolean methods. The escalation path is:

1. Research additional sculpting/retopology tools and references (Blender sculpt brushes,
   SculptForge bridge, hard-surface retopo, Destiny 2 / Warframe / Halo / Dead Space / Killzone
   references).
2. Probe and verify the toolchain on a small head blockout.
3. Extend Gypsy with this capability, toolpack and slice so the next blocked task reuses the
   same escalation path.
4. Record the failed methods and the accepted method; do not commit a failed asset as finished.

The existing helmet brief and handoff are in the replacement asset folder:
`HELMET-SCULPT-BRIEF.md`, `HELMET-HANDOFF-SPEC.md` and `HELMET-RESEARCH.md`.
