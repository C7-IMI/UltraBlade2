#!/usr/bin/env python3
"""High-confidence secret scan over the files git tracks.

The scan deliberately looks for a short list of patterns that are almost always real
credentials, not for entropy or every occurrence of the word "password":

    AWS access key          AKIA followed by 16 upper-case letters/digits
    PEM private key header  -----BEGIN ... PRIVATE KEY-----
    GitHub token            ghp_... and github_pat_...
    Slack token             xox[baprs]-...
    password assignment     password or passwd with a non-placeholder value
    keystore password       storePassword/keyPassword/... with a non-placeholder value

Placeholders and documentation examples are allowed. Binary suffixes and any file whose
bytes contain NUL are skipped. The output names a file and line for every finding and
never prints the matched value. Exit code 0 means pass; 1 means a finding or scan error.

Usage:
    python scripts/check_secrets.py
    python scripts/check_secrets.py --self-test
    python scripts/check_secrets.py --root DIR
"""

import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

BINARY_SUFFIXES = {
    ".7z", ".a", ".apk", ".avi", ".bin", ".bmp", ".bundle", ".class", ".dat",
    ".dds", ".dll", ".dylib", ".exr", ".fbx", ".flac", ".gif", ".gz", ".ico",
    ".jar", ".jpeg", ".jpg", ".ktx", ".lib", ".m4a", ".mov", ".mp3", ".mp4",
    ".o", ".ogg", ".otf", ".pdb", ".pdf", ".png", ".psd", ".rar", ".so",
    ".tar", ".tga", ".tgz", ".tif", ".tiff", ".ttf", ".wav", ".webm", ".webp",
    ".woff", ".woff2", ".zip",
}

TOKEN_RULES = (
    (re.compile(r"\bAKIA[0-9A-Z]{16}\b"), "AWS access key"),
    (
        re.compile(r"-----BEGIN (?:(?:RSA|EC|DSA|OPENSSH|ENCRYPTED) )?PRIVATE KEY-----"),
        "PEM private key header",
    ),
    (re.compile(r"\bghp_[A-Za-z0-9]{36}\b"), "GitHub token"),
    (re.compile(r"\bgithub_pat_[A-Za-z0-9_]{60,255}\b"), "GitHub token"),
    (re.compile(r"\bxox[baprs]-[A-Za-z0-9-]{10,}\b"), "Slack token"),
)

ASSIGNMENT_RULES = (
    (
        re.compile(
            r"(?i)(?<![\w.-])(?P<key>password|passwd)(?![\w.-])"
            r"\s*[\"']?\s*[:=]\s*(?P<value>.+)$"
        ),
        "generic password assignment to a non-placeholder value",
    ),
    (
        re.compile(
            r"(?i)(?<![\w.-])(?P<key>"
            r"storePassword|keyStorePassword|keystorePassword|keyPassword|"
            r"keyaliasPassword|signingKeyPassword|signingPassword|"
            r"storePass|keystorePass|keyaliasPass|keyPass|"
            r"unityKeystorePass|unityKeyaliasPass"
            r")(?![\w.-])\s*[\"']?\s*[:=]\s*(?P<value>.+)$"
        ),
        "keystore signing password assignment to a non-placeholder value",
    ),
)

PLACEHOLDER_VALUES = {
    "", "changeme", "change-me", "change_me", "example", "replace_me",
    "replace-me", "replaceme", "password", "your-password", "your_password",
    "yourpassword", "***", "******", "<password>", "[password]",
    "$password", "%password%", "none", "null", "nil", "todo", "tbd",
}

DOC_SUFFIXES = (".md", ".markdown", ".rst")
FENCE = chr(96) * 3
BACKTICK = chr(96)


def _short(text, limit=120):
    text = " ".join(str(text).split())
    return text if len(text) <= limit else text[: limit - 3] + "..."


def _value_of(raw):
    """Return the literal on the right of an assignment, without quotes or a comment."""
    value = raw.strip()
    if value[:1] in {"'", '"', BACKTICK}:
        quote = value[0]
        end = value.find(quote, 1)
        value = value[1:end] if end >= 0 else value[1:]
    else:
        value = re.split(r"\s+#", value, maxsplit=1)[0]
        value = value.strip().rstrip(",;")
    return value.strip()


def _is_placeholder(value):
    """True when a password-like value is visibly not a credential."""
    if not value:
        return True
    lower = value.lower()
    if lower in PLACEHOLDER_VALUES:
        return True
    if lower.startswith(("your-", "your_")):
        return True
    if set(value) == {"*"}:
        return True
    if lower.startswith(("$", "%")) and (lower.endswith("%") or lower.startswith("$")):
        return True
    if lower.startswith("{{") and lower.endswith("}}"):
        return True
    if lower.startswith("<") and lower.endswith(">"):
        return True
    if lower.startswith("[") and lower.endswith("]"):
        return True
    if "os.environ" in lower or "process.env" in lower or "getenv" in lower:
        return True
    if re.match(r"^[A-Za-z_][A-Za-z0-9_.]*\(", value):
        return True
    if lower.startswith(("self.", "this.", "args.", "config.", "settings.")):
        return True
    return False


def _is_docs_path(path):
    normal = path.replace("\\", "/").lower()
    return normal.endswith(DOC_SUFFIXES) or normal.startswith("docs/")


def scan_text(text, path="<memory>"):
    """Return (path, line, reason) findings for one text blob."""
    findings = []
    docs = _is_docs_path(path)
    in_fence = False
    for number, line in enumerate(text.splitlines(), 1):
        stripped = line.lstrip()
        if stripped.startswith(FENCE) or stripped.startswith("~~~"):
            in_fence = not in_fence
            continue
        for pattern, reason in TOKEN_RULES:
            if pattern.search(line):
                item = (path, number, reason)
                if item not in findings:
                    findings.append(item)
        for pattern, reason in ASSIGNMENT_RULES:
            match = pattern.search(line)
            if not match:
                continue
            value = _value_of(match.group("value"))
            if _is_placeholder(value):
                continue
            if docs and (in_fence or BACKTICK in line):
                continue  # documentation example, not live configuration
            item = (path, number, reason)
            if item not in findings:
                findings.append(item)
    return findings


def _tracked_files(root):
    try:
        proc = subprocess.run(
            ["git", "ls-files", "-z"],
            cwd=str(root),
            capture_output=True,
        )
    except FileNotFoundError:
        raise RuntimeError("git is not on PATH")
    if proc.returncode != 0:
        raise RuntimeError(
            "git ls-files failed: " + proc.stderr.decode("utf-8", "replace").strip()
        )
    return [
        item.decode("utf-8", "surrogateescape")
        for item in proc.stdout.split(b"\0")
        if item
    ]


def run_scan(root):
    try:
        tracked = _tracked_files(root)
    except RuntimeError as exc:
        print("FAIL secrets - " + str(exc))
        return 1
    findings = []
    scanned = 0
    binary = 0
    for relative in tracked:
        path = root / relative
        if path.suffix.lower() in BINARY_SUFFIXES:
            binary += 1
            continue
        if not path.is_file():
            findings.append((relative, 0, "tracked file missing from the working tree"))
            continue
        try:
            data = path.read_bytes()
        except OSError as exc:
            findings.append((relative, 0, "could not read: " + _short(exc)))
            continue
        if b"\0" in data:
            binary += 1
            continue
        scanned += 1
        findings.extend(scan_text(data.decode("utf-8", "replace"), relative))
    if findings:
        print("FAIL secrets - {} high-confidence finding(s)".format(len(findings)))
        for path, line, reason in findings:
            print("{}:{}: {}".format(path, line, reason))
        return 1
    print(
        "PASS secrets - no high-confidence secrets in {} tracked files "
        "({} binary skipped)".format(scanned, binary)
    )
    return 0


def self_test():
    fake_aws = "AKIA" + "Q" * 16
    fake_github = "ghp_" + "A" * 36
    fake_pat = "github_pat_" + "B" * 82
    fake_slack = "xoxb-" + "C" * 24
    fake_pem = "-----BEGIN " + "RSA PRIVATE KEY" + "-----"
    fake_value = "not-a-real-secret-42"
    password_key = "pass" + "word"
    keystore_key = "store" + "Password"

    def assignment(key, value):
        return key + ' = "' + value + '"'

    cases = [
        ("aws-key", assignment("key", fake_aws), True, "selftest/aws.txt"),
        ("pem-header", fake_pem, True, "selftest/key.pem"),
        ("github-ghp", assignment("token", fake_github), True, "selftest/gh.txt"),
        ("github-pat", assignment("token", fake_pat), True, "selftest/pat.txt"),
        ("slack", assignment("token", fake_slack), True, "selftest/slack.txt"),
        ("password", assignment(password_key, fake_value), True, "selftest/settings.py"),
        ("keystore", assignment(keystore_key, fake_value), True, "selftest/build.gradle"),
        ("empty", assignment(password_key, ""), False, "selftest/settings.py"),
        ("changeme", assignment(password_key, "changeme"), False, "selftest/settings.py"),
        ("example", assignment(password_key, "example"), False, "selftest/settings.py"),
        ("replace-me", assignment(password_key, "REPLACE_ME"), False, "selftest/settings.py"),
        ("stars", assignment(password_key, "***"), False, "selftest/settings.py"),
        ("your-prefix", assignment(password_key, "your-password"), False, "selftest/settings.py"),
        ("env-reference", assignment(password_key, "$DB_PASSWORD"), False, "selftest/settings.py"),
        (
            "docs-example",
            FENCE + "\n" + assignment(password_key, fake_value) + "\n" + FENCE,
            False,
            "docs/example.md",
        ),
    ]
    failures = 0
    for label, text, expected, path in cases:
        caught = bool(scan_text(text, path))
        if caught != expected:
            failures += 1
            print(
                "FAIL self-test - {}: expected {}, got {}".format(
                    label, "caught" if expected else "allowed", "caught" if caught else "allowed"
                )
            )
    if failures:
        print("FAIL self-test - {} synthetic case(s) failed".format(failures))
        return 1
    print(
        "PASS self-test - {} synthetic cases: fake key caught, placeholders and a "
        "docs example allowed".format(len(cases))
    )
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description="Scan git-tracked files for high-confidence secrets.")
    parser.add_argument("--self-test", action="store_true", help="run synthetic matcher cases and exit")
    parser.add_argument("--root", help="repository root to scan (default: the parent of scripts/)")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    root = Path(args.root).resolve() if args.root else ROOT
    return run_scan(root)


if __name__ == "__main__":
    sys.exit(main())
