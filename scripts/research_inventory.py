#!/usr/bin/env python3
"""Research-escalation inventory for UltraBlade 2 (ProjectPack rule 10.15).

Emits the machine-readable list of resources, methods and the Gypsy extension used when a
task cannot be completed with the current tools. It performs no network access and changes
nothing except the requested output file.

Usage:
    python scripts/research_inventory.py [--out artifacts/research/inventory.json]
"""

import argparse
import json
import os
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
SCHEMA = "ublade2.research-inventory/v1"
RULE = "10.15"


def probe(name, candidates):
    for candidate in candidates:
        if shutil.which(candidate):
            return {"name": name, "status": "installed", "command": candidate}
        path = Path(candidate)
        if path.is_file():
            return {"name": name, "status": "installed", "command": str(path)}
    return {"name": name, "status": "not-detected", "command": None}


def build_inventory():
    resources = [
        {
            "name": "Web research",
            "kind": "reference",
            "status": "available",
            "use": "official docs, engine manuals, ArtStation/80.lv/GDC material, comparable shipped work",
        },
        {
            "name": "Blender",
            "kind": "tool",
            "status": probe("Blender", [
                "blender",
                r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe",
            ])["status"],
            "use": "sculpt, retopology, procedural modelling, renders, export",
        },
        {
            "name": "SculptForge + Bridge",
            "kind": "tool",
            "status": "declared-installed",
            "use": "sculpting and Blender bridge for the replacement skater asset",
        },
        {
            "name": "Blender Copilot sculpt_brush_stroke",
            "kind": "method",
            "status": "researched",
            "use": "world-space bmesh brush strokes (draw/clay/flatten/crease/grab) for local head/helmet forms without screen coordinates",
            "source": "https://glama.ai/mcp/servers/dwgx/blender-copilot/tools/sculpt_brush_stroke",
        },
        {
            "name": "Blender sculpt operator API",
            "kind": "reference",
            "status": "available",
            "use": "official bpy.ops.sculpt reference for background-script brush constraints",
            "source": "https://docs.blender.org/api/current/bpy.ops.sculpt.html",
        },
        {
            "name": "Blender add-ons",
            "kind": "tool",
            "status": "declared-installed",
            "use": "PolyQuilt, Hardflow, Booltron, LoopTools, Auto Mirror, Material Utilities, PAWS Bakery, Bake Preflight",
        },
        {
            "name": "Python standard library + Pillow",
            "kind": "tool",
            "status": "available",
            "use": "inventory, checks, capture analysis; no new runtime dependency",
        },
        {
            "name": "Gypsy project-local adapter",
            "kind": "tool",
            "status": "installed",
            "use": "run this repository's toolpacks/slices/capabilities without changing GYPSY_HOME",
        },
    ]
    methods = [
        "State the blocked task and the evidence of the failed attempt before searching.",
        "Research broadly: official documentation, tool/plugin catalogs, community write-ups, comparable shipped work, papers/talks.",
        "Prefer a tool already installed; if a new install is needed, check licence, provenance, version and rollback.",
        "Run a small verification spike before changing the production asset or pipeline.",
        "For head/helmet forms, use world-space bmesh brush strokes (flatten, clay, crease, grab) rather than screen-coordinate operators; verify normals and manifoldness.",
        "Record what was found, installed and used; keep failed methods in the record, not in the build.",
        "Extend Gypsy with the reusable part: capability, toolpack action and (where useful) a slice.",
        "Verify with the project check list and the relevant Gypsy capability/toolpack/slice command.",
    ]
    return {
        "schema": SCHEMA,
        "rule": RULE,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "resources": resources,
        "methods": methods,
        "gypsy_extension": {
            "capability": "process.research.escalation",
            "toolpack": "ublade2.research",
            "slice": "ublade2.research",
            "command": "python scripts/gypsy_unity.py toolpacks run ublade2.research",
        },
        "limits": [
            "External accounts, credentials, subscriptions and third-party targets still need operator input.",
            "Do not modify the Gypsy checkout; add project-local manifests only.",
            "Do not copy peer-project data or unlicensed assets.",
        ],
    }


def main(argv=None):
    parser = argparse.ArgumentParser(description="Emit the UltraBlade 2 research-escalation inventory.")
    parser.add_argument("--out", default="artifacts/research/inventory.json")
    args = parser.parse_args(argv)
    output = Path(args.out)
    if not output.is_absolute():
        output = REPO / output
    output.parent.mkdir(parents=True, exist_ok=True)
    document = build_inventory()
    output.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print("UBLADE2_RESEARCH_INVENTORY_OK out={} resources={}".format(output, len(document["resources"])))
    return 0


if __name__ == "__main__":
    sys.exit(main())
