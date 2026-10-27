#!/usr/bin/env python3
"""The one check list for UltraBlade 2 (ProjectPack rules 2.22 and 2.23).

This file names every check and runs it, in order. Both callers run this file with no
arguments:

    .githooks/pre-commit          tracked hook, installed by scripts/install-hooks.py
    .github/workflows/checks.yml  CI gate on push and pull request

The "callers" check is the coverage guard: it fails when either caller is missing or when
the two callers invoke a different command (a subset, for instance). To add a check, add an
entry to CHECKS; both callers then run it because they run this file.

Output is one PASS or FAIL line per check and a summary line. The exit code is 0 only when
every check passes.

Usage:
    python scripts/check_all.py             # run the list
    python scripts/check_all.py --list      # print the checks and exit
    python scripts/check_all.py --root DIR  # run against another checkout (diagnostics)
"""

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]

HOOK_FILE = ".githooks/pre-commit"
CI_FILE = ".github/workflows/checks.yml"

FENCE = "\x60\x60\x60"
TILDE_FENCE = "~~~"

LINK_RE = re.compile(r"!?\[[^\]]*\]\(\s*(<[^>]*>|[^)\s]*)(?:\s+\"[^\"]*\")?\s*\)")
SCHEME_RE = re.compile(r"^[A-Za-z][A-Za-z0-9+.-]*:")


class CheckFailed(Exception):
    """A check found a problem; the message is printed as the FAIL detail."""


def _summarise(problems):
    shown = problems[:10]
    text = "\n".join(shown)
    if len(problems) > len(shown):
        text += "\n... and {} more".format(len(problems) - len(shown))
    return text


def _git(*args):
    try:
        return subprocess.run(["git", *args], cwd=str(ROOT), capture_output=True)
    except FileNotFoundError:
        raise CheckFailed("git is not on PATH; the tracked-file checks need git")


def _tracked_files():
    proc = _git("ls-files", "-z")
    if proc.returncode != 0:
        raise CheckFailed(
            "git ls-files failed (is this a git checkout?): "
            + proc.stderr.decode("utf-8", "replace").strip()
        )
    return [
        item.decode("utf-8", "surrogateescape") for item in proc.stdout.split(b"\0") if item
    ]


def check_records():
    proc = subprocess.run(
        [
            sys.executable,
            str(ROOT / "scripts" / "records_check.py"),
            "--root",
            str(ROOT),
        ],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
    )
    output = proc.stdout.strip()
    if proc.returncode != 0:
        raise CheckFailed(output or proc.stderr.strip() or "records_check exited nonzero")
    prefix = "PASS records - "
    return output[len(prefix):] if output.startswith(prefix) else output


def check_encoding():
    # The bytes compared are the index blobs, i.e. what a commit would actually record. A
    # checkout's line-ending conversion (core.autocrlf) must not decide whether this guard
    # passes, and at pre-commit time the index is exactly the content being committed.
    proc = _git("ls-files", "-s", "-z")
    if proc.returncode != 0:
        raise CheckFailed(
            "git ls-files -s failed (is this a git checkout?): "
            + proc.stderr.decode("utf-8", "replace").strip()
        )
    blobs = []
    for entry in proc.stdout.split(b"\0"):
        if not entry:
            continue
        meta, _, path = entry.partition(b"\t")
        parts = meta.split()
        if len(parts) != 3:
            raise CheckFailed("could not parse git ls-files output")
        mode, sha = parts[0], parts[1]
        if mode == b"160000":
            continue  # submodule commit: not a blob
        blobs.append((path.decode("utf-8", "surrogateescape"), sha))
    if not blobs:
        raise CheckFailed("no tracked files in the index")
    request = b"".join(sha + b"\n" for _, sha in blobs)
    try:
        batch = subprocess.run(
            ["git", "cat-file", "--batch"],
            cwd=str(ROOT),
            input=request,
            capture_output=True,
        )
    except FileNotFoundError:
        raise CheckFailed("git is not on PATH; the tracked-file checks need git")
    if batch.returncode != 0:
        raise CheckFailed(
            "git cat-file failed: " + batch.stderr.decode("utf-8", "replace").strip()
        )
    data = batch.stdout
    position = 0
    problems = []
    text_files = 0
    binary_files = 0
    for path, _ in blobs:
        newline = data.find(b"\n", position)
        if newline < 0:
            raise CheckFailed("could not parse git cat-file --batch output")
        header = data[position:newline].split()
        if len(header) != 3 or header[1] != b"blob":
            raise CheckFailed("unexpected git object for " + path)
        size = int(header[2])
        start = newline + 1
        blob = data[start:start + size]
        position = start + size + 1
        if b"\0" in blob:
            binary_files += 1
            continue
        text_files += 1
        if (
            blob.startswith(b"\xef\xbb\xbf")
            or blob.startswith(b"\xff\xfe")
            or blob.startswith(b"\xfe\xff")
        ):
            problems.append(path + ": byte-order mark")
            continue
        if b"\r" in blob:
            problems.append(path + ": carriage return (CRLF or CR line ending)")
        try:
            blob.decode("utf-8")
        except UnicodeDecodeError as exc:
            problems.append("{}: not valid UTF-8 at byte {}".format(path, exc.start))
    if problems:
        raise CheckFailed(_summarise(problems))
    detail = "{} tracked text files (index content): UTF-8, no BOM, no CR".format(text_files)
    if binary_files:
        detail += "; {} binary files skipped".format(binary_files)
    return detail


def check_links():
    files = [path for path in _tracked_files() if path.lower().endswith(".md")]
    if not files:
        raise CheckFailed("no tracked Markdown files found")
    problems = []
    checked = 0
    for relative in files:
        path = ROOT / relative
        if not path.is_file():
            problems.append(relative + ": tracked but missing from the working tree")
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        in_fence = False
        for number, line in enumerate(text.splitlines(), 1):
            stripped = line.lstrip()
            if stripped.startswith(FENCE) or stripped.startswith(TILDE_FENCE):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            for match in LINK_RE.finditer(line):
                target = match.group(1).strip()
                if target.startswith("<") and target.endswith(">"):
                    target = target[1:-1].strip()
                if not target or target.startswith("#") or SCHEME_RE.match(target):
                    continue
                checked += 1
                path_part = unquote(target.split("#", 1)[0].split("?", 1)[0])
                if not path_part:
                    continue
                if not (path.parent / path_part).exists():
                    problems.append("{}:{}: {}".format(relative, number, target))
    if problems:
        raise CheckFailed(_summarise(problems))
    return "{} relative links in {} tracked Markdown files resolve".format(checked, len(files))


TOOLPACK_VALIDATOR_KINDS = {"file", "json", "text", "wav", "png", "glb"}
SLICE_STEP_KINDS = {"game.demo", "game.build", "game.capture", "check_all", "tests", "records", "command", "toolpack"}
CAPABILITY_MATURITY = {"stable", "partial", "experimental", "planned", "blocked"}
CAPABILITY_REQUIRED = {
    "id", "layer", "kind", "maturity", "description",
    "requires", "inputs", "outputs", "checks", "provenance",
}


def _artifact_path_errors(value, label):
    if not isinstance(value, str) or not value.strip():
        return [label + " must be a non-empty repo-relative path string"]
    path = Path(value)
    if path.is_absolute() or path.anchor:
        return [label + " must be repo-relative: " + str(value)]
    if ".." in path.parts:
        return [label + " must not escape the repository: " + str(value)]
    return []


def _load_manifest(path, problems):
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        problems.append(path.name + ": unreadable JSON: " + str(exc))
        return None
    if not isinstance(document, dict):
        problems.append(path.name + ": not a JSON object")
        return None
    return document


def check_manifests():
    problems = []
    totals = {"toolpacks": 0, "slices": 0, "capabilities": 0}
    seen = {"toolpacks": set(), "slices": set(), "capabilities": set()}

    for path in sorted((ROOT / "toolpacks").glob("*.json")):
        totals["toolpacks"] += 1
        label = "toolpacks/" + path.name
        document = _load_manifest(path, problems)
        if document is None:
            continue
        if document.get("schema") != "gypsy.toolpack/v1":
            problems.append(label + ": schema must be gypsy.toolpack/v1")
        if not document.get("id"):
            problems.append(label + ": missing id")
        elif document["id"] in seen["toolpacks"]:
            problems.append(label + ": duplicate toolpack id " + str(document["id"]))
        seen["toolpacks"].add(document.get("id"))
        actions = document.get("actions")
        if not isinstance(actions, list) or not actions:
            problems.append(label + ": actions must be a non-empty list")
            actions = []
        for index, action in enumerate(actions):
            item = "{} actions[{}]".format(label, index)
            if not action.get("id"):
                problems.append(item + ": missing id")
            argv = action.get("argv")
            if not isinstance(argv, list) or not argv or not all(isinstance(part, str) for part in argv):
                problems.append(item + ": argv must be a non-empty list of strings")
            for artifact in action.get("artifacts") or []:
                problems.extend(_artifact_path_errors(artifact, item + " artifact"))
        validators = document.get("validators")
        if not isinstance(validators, list) or not validators:
            problems.append(label + ": validators must be a non-empty list")
            validators = []
        for index, validator in enumerate(validators):
            item = "{} validators[{}]".format(label, index)
            for field in ("id", "kind", "artifact"):
                if field not in validator:
                    problems.append(item + ": missing " + field)
            if validator.get("kind") not in TOOLPACK_VALIDATOR_KINDS:
                problems.append(item + ": unknown validator kind " + repr(validator.get("kind")))
            problems.extend(_artifact_path_errors(validator.get("artifact"), item + " artifact"))

    for path in sorted((ROOT / "slices").glob("*.json")):
        totals["slices"] += 1
        label = "slices/" + path.name
        document = _load_manifest(path, problems)
        if document is None:
            continue
        if document.get("schema") != "gypsy.slice/v1":
            problems.append(label + ": schema must be gypsy.slice/v1")
        if not document.get("id"):
            problems.append(label + ": missing id")
        elif document["id"] in seen["slices"]:
            problems.append(label + ": duplicate slice id " + str(document["id"]))
        seen["slices"].add(document.get("id"))
        steps = document.get("steps")
        if not isinstance(steps, list) or not steps:
            problems.append(label + ": steps must be a non-empty list")
            steps = []
        for index, step in enumerate(steps):
            item = "{} steps[{}]".format(label, index)
            if not step.get("id"):
                problems.append(item + ": missing id")
            if step.get("kind") not in SLICE_STEP_KINDS:
                problems.append(item + ": unknown step kind " + repr(step.get("kind")))
            if step.get("kind") == "toolpack":
                args = step.get("args") or {}
                if not args.get("toolpack"):
                    problems.append(item + ": toolpack step needs args.toolpack")
                path_arg = args.get("path")
                if path_arg:
                    problems.extend(_artifact_path_errors(path_arg, item + " args.path"))
        for artifact in document.get("artifacts") or []:
            problems.extend(_artifact_path_errors(artifact, label + " artifact"))

    for path in sorted((ROOT / "capabilities").glob("*.json")):
        totals["capabilities"] += 1
        label = "capabilities/" + path.name
        document = _load_manifest(path, problems)
        if document is None:
            continue
        if document.get("schema") != "gypsy.capability/v1":
            problems.append(label + ": schema must be gypsy.capability/v1")
        missing = CAPABILITY_REQUIRED - set(document)
        if missing:
            problems.append(label + ": missing fields " + str(sorted(missing)))
        if document.get("maturity") not in CAPABILITY_MATURITY:
            problems.append(label + ": unknown maturity " + repr(document.get("maturity")))
        identifier = document.get("id")
        if identifier in seen["capabilities"]:
            problems.append(label + ": duplicate capability id " + str(identifier))
        seen["capabilities"].add(identifier)

    if problems:
        raise CheckFailed(_summarise(problems))
    return (
        "{} manifests parse and satisfy the local schema ({} toolpacks, {} slices, {} capabilities)".format(
            sum(totals.values()), totals["toolpacks"], totals["slices"], totals["capabilities"]
        )
    )


def _run_script_check(script, identifier):
    proc = subprocess.run(
        [sys.executable, str(ROOT / "scripts" / script), "--root", str(ROOT)],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    lines = [line.rstrip() for line in (proc.stdout or "").splitlines() if line.strip()]
    if proc.returncode != 0:
        detail = lines or [
            (proc.stderr or "").strip()
            or "{} exited {}".format(script, proc.returncode)
        ]
        first = detail[0]
        for prefix in ("FAIL {} - ".format(identifier), "FAIL "):
            if first.startswith(prefix):
                first = first[len(prefix):]
                break
        raise CheckFailed("\n".join([first] + detail[1:]))
    prefix = "PASS {} - ".format(identifier)
    if lines and lines[0].startswith(prefix):
        return lines[0][len(prefix):]
    return lines[-1] if lines else "{} exited 0".format(script)


def check_secrets():
    return _run_script_check("check_secrets.py", "secrets")


def check_peers():
    return _run_script_check("check_peer_boundary.py", "peers")


def _invocations(path, label, problems):
    relative = path.relative_to(ROOT)
    if not path.is_file():
        problems.append("{} is missing ({})".format(label, relative))
        return []
    found = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        code = line.strip()
        if not code or code.startswith("#"):
            continue
        if "check_all.py" not in code:
            continue
        match = re.search(r"check_all\.py(.*)", code)
        rest = match.group(1).strip().strip("\"'")
        tokens = tuple(part for part in re.split(r"\s+", rest) if part)
        found.append((number, tokens))
    if not found:
        problems.append("{} does not run scripts/check_all.py".format(label))
    return found


def check_callers():
    problems = []
    hook = _invocations(ROOT / HOOK_FILE, "the pre-commit hook", problems)
    ci = _invocations(ROOT / CI_FILE, "the CI workflow", problems)
    invocations = [tokens for _, tokens in hook + ci]
    if invocations:
        distinct = set(invocations)
        if len(distinct) > 1:
            rendered = ", ".join(
                "'" + " ".join(tokens) + "'" for tokens in sorted(distinct)
            )
            problems.append("the hook and CI invoke different arguments: " + rendered)
        elif invocations[0]:
            problems.append(
                "the hook and CI invoke scripts/check_all.py with arguments ('{}'); "
                "both must run the whole list with no arguments".format(
                    " ".join(invocations[0])
                )
            )
    workflow = ROOT / CI_FILE
    if workflow.is_file():
        text = workflow.read_text(encoding="utf-8")
        for required in ("push", "pull_request", "setup-python"):
            if required not in text:
                problems.append("the CI workflow does not mention '{}'".format(required))
    if problems:
        raise CheckFailed(_summarise(problems))
    return (
        "the .githooks/pre-commit hook and .github/workflows/checks.yml both run "
        "scripts/check_all.py with no arguments"
    )


CHECKS = [
    (
        "records",
        "BuildNotes newest-first with a matching header; Retrace numbering and statuses",
        check_records,
    ),
    (
        "encoding",
        "every tracked text file is UTF-8 without a BOM and without CR line endings",
        check_encoding,
    ),
    ("links", "every relative Markdown link resolves", check_links),
    (
        "manifests",
        "toolpacks, slices and capabilities parse and satisfy the local Gypsy schemas",
        check_manifests,
    ),
    (
        "secrets",
        "git-tracked files carry no high-confidence credentials",
        check_secrets,
    ),
    (
        "peers",
        "the project.gypsy.json peers exist, are git checkouts and are clean",
        check_peers,
    ),
    (
        "callers",
        "the hook and CI run this same list (coverage guard, rules 2.22-2.23)",
        check_callers,
    ),

]


def main(argv=None):
    global ROOT
    parser = argparse.ArgumentParser(description="UltraBlade 2's one check list.")
    parser.add_argument(
        "--list", action="store_true", help="print the check ids and descriptions, then exit"
    )
    parser.add_argument("--root", help="repository root to check (default: the parent of scripts/)")
    args = parser.parse_args(argv)
    if args.root:
        ROOT = Path(args.root).resolve()
    if args.list:
        for identifier, description, _ in CHECKS:
            print("{} - {}".format(identifier, description))
        return 0
    print("checklist: {} checks".format(len(CHECKS)))
    failures = 0
    for identifier, _, check in CHECKS:
        try:
            detail = check()
        except CheckFailed as exc:
            failures += 1
            lines = str(exc).splitlines() or ["failed"]
            print("FAIL {} - {}".format(identifier, lines[0]))
            for line in lines[1:]:
                print("     " + line)
        except Exception as exc:  # a crash is a failed check, not a traceback
            failures += 1
            print("FAIL {} - unexpected {}: {}".format(identifier, type(exc).__name__, exc))
        else:
            print("PASS {} - {}".format(identifier, detail))
    passed = len(CHECKS) - failures
    if failures:
        print("checks: FAIL ({}/{} passed)".format(passed, len(CHECKS)))
        return 1
    print("checks: PASS ({}/{})".format(passed, len(CHECKS)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
