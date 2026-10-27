#!/usr/bin/env python3
"""Validate the playable-slice captures: the match frame (world + HUD) and the menu frame.

The match capture must be a 1280x720 frame with a colourful rendered world and a readable HUD
strip at the top. The menu capture must show the dark neon UI with real colour variety.
Writes one JSON document and prints a marker.

Usage:
    python scripts/check_match_capture.py --kind match --frame artifacts/ublade2-pro/match.png \
        --evidence artifacts/ublade2-pro/match-runtime.json --out artifacts/ublade2-pro/match-capture.json
    python scripts/check_match_capture.py --kind menu --frame artifacts/ublade2-pro/menu.png \
        --out artifacts/ublade2-pro/menu-capture.json
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


def stats(image, box):
    crop = image.crop(box)
    pixels = list(crop.getdata())
    total = len(pixels)
    non_background = 0
    saturated = 0
    buckets = set()
    for red, green, blue in pixels:
        brightest = max(red, green, blue)
        darkest = min(red, green, blue)
        if brightest > 60 or brightest - darkest > 40:
            non_background += 1
        if brightest > 60 and brightest - darkest > 30:
            saturated += 1
        buckets.add((red // 16, green // 16, blue // 16))
    return {
        "pixels": total,
        "nonBackground": non_background,
        "nonBackgroundRatio": round((non_background / float(total)) if total else 0.0, 4),
        "saturated": saturated,
        "distinctColors": len(buckets),
    }


def main(argv=None):
    parser = argparse.ArgumentParser(description="Validate a UBlade2 playable-slice capture.")
    parser.add_argument("--kind", choices=["match", "menu"], required=True)
    parser.add_argument("--frame", required=True)
    parser.add_argument("--evidence", default=None)
    parser.add_argument("--out", required=True)
    args = parser.parse_args(argv)

    result = {
        "schema": "ublade2.match.capture-check/v1",
        "kind": args.kind,
        "frame": args.frame,
        "passed": False,
        "errors": [],
        "warnings": [],
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    try:
        image = Image.open(Path(args.frame)).convert("RGB")
    except Exception as exc:  # noqa: BLE001 - an unreadable frame is state, not a crash
        result["errors"].append("frame unreadable: " + str(exc))
        write(Path(args.out), result)
        return 1

    width, height = image.size
    result["width"] = width
    result["height"] = height
    if args.kind == "match" and (width, height) != (1280, 720):
        result["errors"].append("match frame is {}x{}, expected 1280x720".format(width, height))
    if width < 1000 or height < 600:
        result["errors"].append("frame is too small: {}x{}".format(width, height))

    whole = stats(image, (0, 0, width, height))
    result["whole"] = whole
    # The menu is a deliberately dark neon screen; the match frame is a bright arena. Use a per-kind bar.
    min_ratio = 0.15 if args.kind == "match" else 0.04
    min_colours = 40 if args.kind == "match" else 80
    if whole["nonBackgroundRatio"] < min_ratio:
        result["errors"].append("frame is only {:.1f}% non-background, expected at least {:.0f}%".format(
            whole["nonBackgroundRatio"] * 100.0, min_ratio * 100.0))
    if whole["distinctColors"] < min_colours:
        result["errors"].append("frame has only {} colour buckets, expected at least {}".format(
            whole["distinctColors"], min_colours))

    if args.kind == "match":
        hud = stats(image, (0, 0, width, max(1, height // 6)))
        result["hudStrip"] = hud
        if hud["nonBackgroundRatio"] < 0.05:
            result["errors"].append("HUD strip is only {:.1f}% non-background, expected at least 5%".format(
                hud["nonBackgroundRatio"] * 100.0))
        centre = stats(image, (width // 3, height // 4, width * 2 // 3, height * 3 // 4))
        result["heroRegion"] = centre
        if centre["nonBackgroundRatio"] < 0.06:
            result["errors"].append("centre region is only {:.1f}% non-background, expected at least 6%".format(
                centre["nonBackgroundRatio"] * 100.0))
        evidence_path = Path(args.evidence) if args.evidence else None
        if evidence_path is not None and evidence_path.is_file():
            try:
                evidence = json.loads(evidence_path.read_text(encoding="utf-8-sig"))
                result["match"] = {
                    "playerScore": evidence.get("playerScore"),
                    "aiScore": evidence.get("aiScore"),
                    "winner": evidence.get("winner"),
                    "frames": evidence.get("frames"),
                    "phase": evidence.get("phase"),
                }
            except Exception as exc:  # noqa: BLE001
                result["warnings"].append("evidence unreadable: " + str(exc))
        else:
            result["warnings"].append("match evidence missing: " + str(evidence_path))
    else:
        accent = stats(image, (width // 4, height // 2, width * 3 // 4, height * 4 // 5))
        result["accentRegion"] = accent
        if accent["nonBackgroundRatio"] < 0.01:
            result["errors"].append("menu accent region is only {:.1f}% non-background, expected at least 2%".format(
                accent["nonBackgroundRatio"] * 100.0))

    result["passed"] = not result["errors"]
    write(Path(args.out), result)
    marker = "MATCH_CAPTURE_CHECK_OK" if args.kind == "match" else "MENU_CAPTURE_CHECK_OK"
    if result["passed"]:
        print("{} frame={} nonBg={:.1f}% colours={}".format(
            marker, args.frame, whole["nonBackgroundRatio"] * 100.0, whole["distinctColors"]))
        return 0
    print((marker.replace("_OK", "_FAIL")) + " " + "; ".join(result["errors"]))
    return 1


def write(path, document):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(document, indent=2) + "\n")


if __name__ == "__main__":
    sys.exit(main())
