#!/usr/bin/env python3
"""Build artifacts/hero/lod-conversion.json from the real files and the captured logs.

Every hash and byte count is recomputed here with hashlib / os.path.getsize; the
commands, exit codes and verification facts are read back from the two logs that the
Blender runs actually produced.  Nothing is asserted that was not measured.

Usage (from the repository root):
    python scripts/hero/build_lod_report.py
"""

import datetime
import hashlib
import json
import os
import re
import subprocess
import sys

SCHEMA = "ublade2.hero.lod-conversion/v1"
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
HERO_DIR = os.path.join(REPO, "unity", "UBlade2", "Assets", "Resources", "Hero")
ART_DIR = os.path.join(REPO, "artifacts", "hero")
CONV_LOG = os.path.join(ART_DIR, "lod-conversion.log")
VERIFY_LOG = os.path.join(ART_DIR, "lod-fbx-verify.log")
LODS_JSON = os.path.join(ART_DIR, "source", "wayfarer_lods.json")
REPORT = os.path.join(ART_DIR, "lod-conversion.json")

MIN_BYTES = 100000
EXPECT_BONES = 23
EXPECT_ACTIONS = ["idle", "run", "reach"]

LODS = [
    {
        "level": "lod1",
        "input": r"C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy\assets\hero\wayfarer_lod1.glb",
        "output": os.path.join(HERO_DIR, "wayfarer_lod1.fbx"),
    },
    {
        "level": "lod2",
        "input": r"C:\OneDrive\OneDrive - Cyber 7 Group\GHRepo\Games\ProjectGypsy\assets\hero\wayfarer_lod2.glb",
        "output": os.path.join(HERO_DIR, "wayfarer_lod2.fbx"),
    },
]


def sha256_of(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_text(path):
    with open(path, "r", encoding="utf-8", newline="") as handle:
        return handle.read().replace("\r\n", "\n").replace("\r", "\n")


def read_blocks(text):
    """Split a log into ('RUN 1/2 : wayfarer_lod1', body) pairs."""
    parts = re.split(r"^=== (.+?) ===$", text, flags=re.M)
    return [(parts[i].strip(), parts[i + 1]) for i in range(1, len(parts), 2)]


def split_argv(command_line):
    """Split a Windows command line into argv, honouring double quotes."""
    if not command_line:
        return []
    argv = []
    for match in re.finditer(r'"([^"]*)"|(\S+)', command_line):
        argv.append(match.group(1) if match.group(1) is not None else match.group(2))
    return argv


def parse_conversion_log(text):
    records = []
    for header, body in read_blocks(text):
        command = re.search(r"^command\s+(.+)$", body, flags=re.M)
        code = re.search(r"^exit code\s+(-?\d+)", body, flags=re.M)
        marker = re.search(r"^LOD_FBX_OK (.+)$", body, flags=re.M)
        records.append({
            "run": header,
            "command_line": command.group(1).strip() if command else None,
            "exit_code": int(code.group(1)) if code else None,
            "marker": marker.group(1).strip() if marker else None,
        })
    return records


def parse_verify_log(text):
    records = []
    for header, body in read_blocks(text):
        command = re.search(r"^command\s+(.+)$", body, flags=re.M)
        code = re.search(r"^exit code\s+(-?\d+)", body, flags=re.M)
        marker = re.search(r"^LOD_FBX_VERIFY_OK .*?file=(.+?) bones=(\d+) actions=(\S*) meshes=(\d+)",
                           body, flags=re.M)
        detail = None
        for line in body.split("\n"):
            if line.startswith("LOD_FBX_VERIFY_DETAIL "):
                detail = json.loads(line[len("LOD_FBX_VERIFY_DETAIL "):])
        records.append({
            "run": header,
            "command_line": command.group(1).strip() if command else None,
            "exit_code": int(code.group(1)) if code else None,
            "marker": marker.group(0).strip() if marker else None,
            "detail": detail,
        })
    return records


def blender_version():
    try:
        out = subprocess.run([BLENDER, "--version"], capture_output=True, text=True, timeout=120)
        first = (out.stdout or "").strip().splitlines()
        if first:
            return first[0].strip()
    except Exception:
        pass
    if os.path.isfile(CONV_LOG):
        match = re.search(r"^# blender\s*:\s*(.+)$", read_text(CONV_LOG), flags=re.M)
        if match:
            return match.group(1).strip()
    return "unknown"


def main():
    for path in (CONV_LOG, VERIFY_LOG, LODS_JSON):
        if not os.path.isfile(path):
            sys.stderr.write("missing required evidence file: %s\n" % path)
            return 1

    manifest = json.loads(read_text(LODS_JSON))
    manifest_by_file = {item["file"]: item for item in manifest.get("lods", [])}

    conv = parse_conversion_log(read_text(CONV_LOG))
    ver = parse_verify_log(read_text(VERIFY_LOG))

    if len(conv) != len(LODS) or len(ver) != len(LODS):
        sys.stderr.write("expected %d conversion and %d verification runs, found %d and %d\n"
                         % (len(LODS), len(LODS), len(conv), len(ver)))
        return 1

    inputs, outputs, commands, verification = [], [], [], []
    failures = []

    for index, lod in enumerate(LODS):
        level = lod["level"]
        src, dst = lod["input"], lod["output"]

        for label, path in (("input", src), ("output", dst)):
            if not os.path.isfile(path):
                failures.append("%s %s missing: %s" % (level, label, path))

        entry_in = {
            "level": level,
            "path": src,
            "exists": os.path.isfile(src),
            "bytes": os.path.getsize(src) if os.path.isfile(src) else None,
            "sha256": sha256_of(src) if os.path.isfile(src) else None,
        }
        known = manifest_by_file.get(os.path.basename(src))
        if known:
            entry_in["triangles"] = known.get("triangles")
            entry_in["vertices"] = known.get("vertices")
            entry_in["triangles_source"] = "artifacts/hero/source/wayfarer_lods.json"
            entry_in["sha256_matches_manifest"] = entry_in["sha256"] == known.get("sha256")
            if not entry_in["sha256_matches_manifest"]:
                failures.append("%s input sha256 does not match wayfarer_lods.json" % level)

        entry_out = {
            "level": level,
            "path": dst,
            "exists": os.path.isfile(dst),
            "bytes": os.path.getsize(dst) if os.path.isfile(dst) else None,
            "sha256": sha256_of(dst) if os.path.isfile(dst) else None,
        }
        if entry_out["bytes"] is not None and entry_out["bytes"] <= MIN_BYTES:
            failures.append("%s output is only %d bytes (<= %d)" % (level, entry_out["bytes"], MIN_BYTES))

        run_conv, run_ver = conv[index], ver[index]
        commands.append({
            "level": level,
            "stage": "convert",
            "argv": split_argv(run_conv["command_line"]),
            "command_line": run_conv["command_line"],
            "exit_code": run_conv["exit_code"],
            "log": "artifacts/hero/lod-conversion.log",
        })
        commands.append({
            "level": level,
            "stage": "verify",
            "argv": split_argv(run_ver["command_line"]),
            "command_line": run_ver["command_line"],
            "exit_code": run_ver["exit_code"],
            "log": "artifacts/hero/lod-fbx-verify.log",
        })
        if run_conv["exit_code"] != 0:
            failures.append("%s conversion exited %s" % (level, run_conv["exit_code"]))
        if run_ver["exit_code"] != 0:
            failures.append("%s verification exited %s" % (level, run_ver["exit_code"]))
        if not run_conv["marker"]:
            failures.append("%s conversion log has no LOD_FBX_OK marker" % level)
        if not run_ver["marker"]:
            failures.append("%s verification log has no LOD_FBX_VERIFY_OK marker" % level)

        detail = run_ver["detail"] or {}
        bones = detail.get("bones", 0)
        actions = detail.get("actions", [])
        skinned = detail.get("skinned_meshes", [])
        if bones != EXPECT_BONES:
            failures.append("%s re-import reports %d bones (expected %d)" % (level, bones, EXPECT_BONES))
        missing = [a for a in EXPECT_ACTIONS if a.lower() not in {x.lower() for x in actions}]
        if missing:
            failures.append("%s re-import is missing actions: %s" % (level, ", ".join(missing)))
        if not skinned:
            failures.append("%s re-import found no skinned mesh" % level)

        verification.append({
            "level": level,
            "bones": bones,
            "actions": actions,
            "actions_raw": detail.get("actions_raw", []),
            "expected_actions_present": not missing,
            "skinned_meshes": skinned,
            "meshes": detail.get("meshes", []),
            "triangles_total": detail.get("triangles_total"),
            "passed": bool(detail.get("passed")),
            "command_line": run_ver["command_line"],
            "log": "artifacts/hero/lod-fbx-verify.log",
        })

        inputs.append(entry_in)
        outputs.append(entry_out)

    report = {
        "schema": SCHEMA,
        "generated_at": datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
        "blender": blender_version(),
        "inputs": inputs,
        "outputs": outputs,
        "commands": commands,
        "verification": verification,
        "checks": {
            "outputs_over_100000_bytes": all(
                (o["bytes"] or 0) > MIN_BYTES for o in outputs),
            "bones_23_per_lod": all(v["bones"] == EXPECT_BONES for v in verification),
            "actions_idle_run_reach_per_lod": all(v["expected_actions_present"] for v in verification),
            "skinned_mesh_per_lod": all(bool(v["skinned_meshes"]) for v in verification),
            "all_commands_exit_zero": all(c["exit_code"] == 0 for c in commands),
        },
        "failures": failures,
        "passed": not failures,
        "notes": [
            "FBX animation take names are Blender's '<object>|<action>' form "
            "('Wayfarer_Rig|idle', 'Wayfarer_Rig|run', 'Wayfarer_Rig|reach'); a re-import "
            "prefixes the armature name again, so Blender action datablocks read "
            "'Wayfarer_Rig|Wayfarer_Rig|idle'. Unity shows the take name, i.e. 'Wayfarer_Rig|idle'.",
            "The glTF importer's hidden 'glTF_not_exported' bone-display icosphere is stripped "
            "before export so the FBX contains exactly one skinned mesh.",
        ],
    }

    with open(REPORT, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(report, handle, indent=2, sort_keys=False)
        handle.write("\n")

    print("wrote %s" % REPORT)
    print("passed=%s failures=%d" % (report["passed"], len(failures)))
    for failure in failures:
        print("  FAIL: %s" % failure)
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
