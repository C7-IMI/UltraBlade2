#!/usr/bin/env python3
"""Mechanised peer-project boundary guard.

Reads the "peers" list from project.gypsy.json at the repository root. Every path is
relative to that root and is treated as read-only: this script only runs git commands
inside each peer and never writes, stages, cleans or fetches there.

For each peer it verifies that the path exists, that git can resolve HEAD, and that
git status --porcelain is empty. It prints one PEER_CLEAN line per clean peer and
PEER_BOUNDARY_OK peers=N at the end. A missing path, dirty tree, unreadable git
metadata or malformed peer entry exits 1.

Usage:
    python scripts/check_peer_boundary.py
    python scripts/check_peer_boundary.py --root DIR
"""

import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = "project.gypsy.json"


def _git(peer, *args):
    try:
        return subprocess.run(
            ["git", "-C", str(peer), *args],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except FileNotFoundError:
        return None


def _first_line(text):
    text = (text or "").strip()
    return text.splitlines()[0] if text else ""


def _read_peers(root):
    manifest = root / MANIFEST
    try:
        document = json.loads(manifest.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return None, "PEER_CONFIG {} is missing".format(MANIFEST)
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        return None, "PEER_CONFIG {} is unreadable: {}".format(MANIFEST, _first_line(exc))
    if not isinstance(document, dict):
        return None, "PEER_CONFIG {} is not a JSON object".format(MANIFEST)
    peers = document.get("peers")
    if not isinstance(peers, list) or not peers:
        return None, "PEER_CONFIG {} has no non-empty peers list".format(MANIFEST)
    problems = []
    seen = set()
    for index, peer in enumerate(peers):
        label = "peers[{}]".format(index)
        if not isinstance(peer, dict):
            problems.append("PEER_CONFIG {} is not an object".format(label))
            continue
        for field in ("id", "path", "role"):
            if not isinstance(peer.get(field), str) or not peer[field].strip():
                problems.append("PEER_CONFIG {}.{} must be a non-empty string".format(label, field))
        identifier = peer.get("id")
        if isinstance(identifier, str):
            if identifier in seen:
                problems.append("PEER_CONFIG duplicate peer id {}".format(identifier))
            seen.add(identifier)
        path = peer.get("path")
        if isinstance(path, str):
            try:
                if Path(path).is_absolute():
                    problems.append(
                        "PEER_CONFIG {}.path must be relative to the repo root".format(label)
                    )
            except (OSError, ValueError):
                problems.append("PEER_CONFIG {}.path is not a usable path".format(label))
    if problems:
        return None, "\n".join(problems)
    return peers, None


def check(root):
    peers, error = _read_peers(root)
    if error:
        for line in error.splitlines():
            print(line)
        print("PEER_BOUNDARY_FAIL peers=0")
        return 1
    failures = 0
    for peer in peers:
        identifier = peer["id"]
        relative = peer["path"]
        path = (root / relative).resolve()
        if not path.is_dir():
            print("PEER_MISSING id={} path={}".format(identifier, relative))
            failures += 1
            continue
        head_proc = _git(path, "rev-parse", "HEAD")
        if head_proc is None:
            print("PEER_GIT id={} path={} reason=git is not on PATH".format(identifier, relative))
            failures += 1
            continue
        if head_proc.returncode != 0:
            reason = _first_line(head_proc.stderr) or "git rev-parse HEAD failed"
            print(
                "PEER_GIT id={} path={} reason={}".format(
                    identifier, relative, reason.replace("\n", " ")
                )
            )
            failures += 1
            continue
        head = head_proc.stdout.strip()
        status_proc = _git(path, "status", "--porcelain")
        if status_proc is None:
            print("PEER_GIT id={} path={} reason=git is not on PATH".format(identifier, relative))
            failures += 1
            continue
        if status_proc.returncode != 0:
            reason = _first_line(status_proc.stderr) or "git status --porcelain failed"
            print(
                "PEER_GIT id={} path={} reason={}".format(
                    identifier, relative, reason.replace("\n", " ")
                )
            )
            failures += 1
            continue
        dirty = status_proc.stdout.strip()
        if dirty:
            print("PEER_DIRTY id={} path={}".format(identifier, relative))
            for line in dirty.splitlines()[:10]:
                print("  " + line)
            failures += 1
            continue
        print("PEER_CLEAN id={} head={}".format(identifier, head))
    if failures:
        print("PEER_BOUNDARY_FAIL peers={} failures={}".format(len(peers), failures))
        return 1
    print("PEER_BOUNDARY_OK peers={}".format(len(peers)))
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description="Check configured peer repositories are clean and read-only.")
    parser.add_argument("--root", help="repository root (default: the parent of scripts/)")
    args = parser.parse_args(argv)
    root = Path(args.root).resolve() if args.root else ROOT
    return check(root)


if __name__ == "__main__":
    sys.exit(main())
