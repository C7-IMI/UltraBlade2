#!/usr/bin/env python3
"""Project-local Gypsy adapter for the UltraBlade 2 Unity port.

Gypsy Studio's CLI resolves toolpacks, slices and capability manifests from its own checkout,
and its toolpack/slice runners hard-code that checkout as the project root. UltraBlade 2 must
not modify GYPSY_HOME, so this adapter imports Gypsy's own modules read-only (no bytecode
writes) and runs THIS repository's manifests with project_root=<this repo> and state under
workstream/state. Validation, state schemas and the orchestrator are Gypsy's, not a copy.

Usage:
    python scripts/gypsy_unity.py check
    python scripts/gypsy_unity.py capabilities
    python scripts/gypsy_unity.py toolpacks list|check
    python scripts/gypsy_unity.py toolpacks run ublade2.unity [--action build|demo|capture|capture-check]
    python scripts/gypsy_unity.py slice list
    python scripts/gypsy_unity.py slice run ublade2.unity [--json]
"""

import argparse
import importlib
import json
import os
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
DEFAULT_GYPSY_HOME = Path(r"C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy")


def resolve_gypsy_home(value):
    if value:
        return Path(value)
    if os.environ.get("GYPSY_HOME"):
        return Path(os.environ["GYPSY_HOME"])
    return DEFAULT_GYPSY_HOME


def gypsy_module(home, name="gypsy"):
    sys.dont_write_bytecode = True
    workstream = str(home / "workstream")
    if workstream not in sys.path:
        sys.path.insert(0, workstream)
    return importlib.import_module(name)


def state_dir():
    path = REPO / "workstream" / "state"
    path.mkdir(parents=True, exist_ok=True)
    return path


def command_check(home):
    import subprocess

    environment = dict(os.environ)
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    process = subprocess.run(
        [sys.executable, str(home / "workstream" / "gypsy.py"), "check", "--project", str(REPO)],
        cwd=str(REPO),
        capture_output=True,
        text=True,
        env=environment,
        timeout=600,
    )
    if process.stdout.strip():
        print(process.stdout.strip())
    if process.stderr.strip():
        print(process.stderr.strip(), file=sys.stderr)
    print("UBLADE2_GYPSY_CHECK exit={}".format(process.returncode))
    return process.returncode


def command_capabilities(home):
    gypsy = gypsy_module(home)
    capabilities = []
    for path in sorted((REPO / "capabilities").glob("*.json")):
        document = json.loads(path.read_text(encoding="utf-8-sig"))
        document["_file"] = path.name
        capabilities.append(document)
    errors = gypsy.validate_capabilities(capabilities)
    for error in errors:
        print("FAIL " + error)
    print("UBLADE2_CAPABILITIES count={} errors={}".format(len(capabilities), len(errors)))
    print("UBLADE2_CAPABILITIES_OK" if not errors else "UBLADE2_CAPABILITIES_FAIL")
    return 0 if not errors else 1


def load_toolpacks(home):
    toolpacks = gypsy_module(home, "toolpacks")
    manifests = toolpacks.load_all(REPO / "toolpacks")
    return toolpacks, manifests


def command_toolpacks(home, args):
    toolpacks, manifests = load_toolpacks(home)
    if args.toolpacks_command == "list":
        for toolpack_id in sorted(manifests):
            manifest = manifests[toolpack_id]
            print("{}: {} (actions: {})".format(
                toolpack_id,
                manifest.get("name"),
                ", ".join(action["id"] for action in manifest.get("actions", []))))
        return 0
    if args.toolpacks_command == "check":
        problems = {}
        for toolpack_id, manifest in manifests.items():
            errors = toolpacks.validate_manifest(manifest)
            if errors:
                problems[toolpack_id] = errors
        for toolpack_id, errors in problems.items():
            for error in errors:
                print("FAIL {}: {}".format(toolpack_id, error))
        print("UBLADE2_TOOLPACKS count={} errors={}".format(len(manifests), len(problems)))
        print("UBLADE2_TOOLPACKS_OK" if not problems else "UBLADE2_TOOLPACKS_FAIL")
        return 0 if not problems else 1
    if args.toolpacks_command == "run":
        if args.toolpack_id not in manifests:
            print("FAIL unknown toolpack {!r}; have {}".format(args.toolpack_id, sorted(manifests)), file=sys.stderr)
            return 2
        manifest = manifests[args.toolpack_id]
        if args.action:
            result = toolpacks.run_action(manifest, args.action, project_root=REPO, state_dir=state_dir())
        else:
            result = toolpacks.run_toolpack(manifest, project_root=REPO, state_dir=state_dir())
        if args.json:
            print(json.dumps(result, indent=2))
        else:
            print("toolpack {}: passed={} artifacts={} validators={} state={}".format(
                result.get("toolpack"), result.get("passed"),
                len(result.get("artifacts", [])), len(result.get("validators", [])),
                result.get("statePath")))
            for error in result.get("errors", []):
                print("ERROR " + error)
        print("UBLADE2_TOOLPACK_OK toolpack={} passed=True state={}".format(
            manifest["id"], result.get("statePath")) if result.get("passed")
            else "UBLADE2_TOOLPACK_FAIL toolpack={} errors={}".format(manifest["id"], result.get("errors")))
        return 0 if result.get("passed") else 1
    return 2


def command_slice(home, args):
    slices = gypsy_module(home, "slices")
    orchestrator = gypsy_module(home, "orchestrator")
    manifests = slices.load_all(REPO / "slices")
    if args.slices_command == "list":
        for slice_id in sorted(manifests):
            manifest = manifests[slice_id]
            print("{}: {} (steps: {})".format(
                slice_id, manifest.get("description", ""),
                ", ".join(step["id"] for step in manifest.get("steps", []))))
        return 0
    if args.slices_command == "run":
        if args.slice_id not in manifests:
            print("FAIL unknown slice {!r}; have {}".format(args.slice_id, sorted(manifests)), file=sys.stderr)
            return 2
        result = orchestrator.run_slice(manifests[args.slice_id], project_root=REPO, state_dir=state_dir())
        if args.json:
            print(json.dumps(result, indent=2))
        else:
            print("slice {}: passed={} steps={} artifacts={} state={}".format(
                result.get("slice"), result.get("passed"), len(result.get("steps", [])),
                len(result.get("artifacts", [])), result.get("statePath")))
            for error in result.get("errors", []):
                print("ERROR " + error)
        print("UBLADE2_SLICE_OK slice={} passed=True state={}".format(args.slice_id, result.get("statePath")) if result.get("passed")
              else "UBLADE2_SLICE_FAIL slice={} errors={}".format(args.slice_id, result.get("errors")))
        return 0 if result.get("passed") else 1
    return 2


def main(argv=None):
    parser = argparse.ArgumentParser(description="Project-local Gypsy adapter for UltraBlade 2.")
    parser.add_argument("--gypsy-home", help="Gypsy Studio checkout (default: GYPSY_HOME or the known path)")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("check", help="run Gypsy's project check against this repository")
    sub.add_parser("capabilities", help="validate this repository's capability manifests with Gypsy's validator")

    toolpacks_parser = sub.add_parser("toolpacks")
    toolpack_commands = toolpacks_parser.add_subparsers(dest="toolpacks_command", required=True)
    toolpack_commands.add_parser("list")
    toolpack_commands.add_parser("check")
    toolpack_run = toolpack_commands.add_parser("run")
    toolpack_run.add_argument("toolpack_id")
    toolpack_run.add_argument("--action")
    toolpack_run.add_argument("--json", action="store_true")

    slice_parser = sub.add_parser("slice")
    slice_commands = slice_parser.add_subparsers(dest="slices_command", required=True)
    slice_commands.add_parser("list")
    slice_run = slice_commands.add_parser("run")
    slice_run.add_argument("slice_id")
    slice_run.add_argument("--json", action="store_true")

    args = parser.parse_args(argv)
    home = resolve_gypsy_home(args.gypsy_home)
    if not (home / "workstream" / "gypsy.py").is_file():
        print("FAIL Gypsy Studio not found at " + str(home), file=sys.stderr)
        return 2
    if args.command == "check":
        return command_check(home)
    if args.command == "capabilities":
        return command_capabilities(home)
    if args.command == "toolpacks":
        return command_toolpacks(home, args)
    if args.command == "slice":
        return command_slice(home, args)
    return 2


if __name__ == "__main__":
    sys.exit(main())
