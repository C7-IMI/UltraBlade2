#!/usr/bin/env python3
"""Rewrite text evidence as UTF-8 with LF line endings.

Committed evidence must hash the same as the file on disk, and the repository's
.gitattributes normalises text to LF. Unity writes CRLF logs and some tools run with
Windows text newlines, so this pass makes the evidence deterministic before the Gypsy
toolpack hashes it. Binary files (PNG, FBX) and files with NUL bytes are left untouched.

Usage:
    python scripts/normalize_evidence.py --paths artifacts/ublade2 artifacts/hero
"""

import argparse
import sys
from pathlib import Path

TEXT_SUFFIXES = {".log", ".json", ".txt", ".md", ".csv", ".xml", ".yaml", ".yml"}


def normalize_file(path):
    data = path.read_bytes()
    if b"\0" in data:
        return False
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError:
        return False
    if text.startswith("\ufeff"):
        text = text[1:]
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    if normalized == text:
        return False
    path.write_text(normalized, encoding="utf-8", newline="\n")
    return True


def main(argv=None):
    parser = argparse.ArgumentParser(description="Normalise evidence text to UTF-8 LF.")
    parser.add_argument("--paths", nargs="+", default=["artifacts/ublade2", "artifacts/hero"])
    args = parser.parse_args(argv)
    root = Path(__file__).resolve().parents[1]
    changed = 0
    seen = 0
    for relative in args.paths:
        folder = root / relative
        if not folder.is_dir():
            continue
        for path in sorted(folder.rglob("*")):
            if not path.is_file() or path.suffix.lower() not in TEXT_SUFFIXES:
                continue
            seen += 1
            if normalize_file(path):
                changed += 1
                print("normalized " + path.relative_to(root).as_posix())
    print("EVIDENCE_NORMALIZED files={} changed={}".format(seen, changed))
    return 0


if __name__ == "__main__":
    sys.exit(main())
