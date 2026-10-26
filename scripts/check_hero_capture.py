#!/usr/bin/env python3
"""Check that the Unity capture frame is a real 1280x720 frame with the hero in it.

Reads the runtime evidence written by the player (hero screen rect in pixels) and measures the
pixels inside that rect: the hero must cover a meaningful share of it and show real material
colour variation, not a flat background. Writes one JSON document and prints a marker.

Usage:
    python scripts/check_hero_capture.py --frame artifacts/ublade2/frame.png \
        --evidence artifacts/ublade2/hero-runtime.json --out artifacts/ublade2/capture-check.json
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


def main(argv=None):
    parser = argparse.ArgumentParser(description="Validate the UBlade2 hero capture frame.")
    parser.add_argument("--frame", required=True)
    parser.add_argument("--evidence", default=None)
    parser.add_argument("--out", required=True)
    args = parser.parse_args(argv)

    frame = Path(args.frame)
    out = Path(args.out)
    result = {
        "schema": "ublade2.capture.check/v1",
        "frame": args.frame,
        "passed": False,
        "errors": [],
        "warnings": [],
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }

    try:
        image = Image.open(frame).convert("RGB")
    except Exception as exc:  # noqa: BLE001 - a bad frame is state, not a crash
        result["errors"].append("frame unreadable: " + str(exc))
        write(out, result)
        return 1

    width, height = image.size
    result["width"] = width
    result["height"] = height
    if (width, height) != (1280, 720):
        result["errors"].append("frame is {}x{}, expected 1280x720".format(width, height))

    evidence = {}
    evidence_path = Path(args.evidence) if args.evidence else None
    if evidence_path is not None and evidence_path.is_file():
        try:
            evidence = json.loads(evidence_path.read_text(encoding="utf-8-sig"))
        except Exception as exc:  # noqa: BLE001
            result["warnings"].append("evidence unreadable: " + str(exc))
    else:
        result["warnings"].append("evidence file missing: " + str(evidence_path))
    result["heroVisibleFromEvidence"] = evidence.get("heroVisible")

    rect = [evidence.get("heroRectX"), evidence.get("heroRectY"),
            evidence.get("heroRectWidth"), evidence.get("heroRectHeight")]
    if not all(isinstance(value, int) for value in rect) or rect[2] <= 0 or rect[3] <= 0:
        rect = [width // 3, height // 4, width // 3, height // 2]
        result["warnings"].append("hero rect missing from evidence; using the centre third")
    x, y, box_width, box_height = rect
    x = max(0, min(x, width - 1))
    y = max(0, min(y, height - 1))
    box_width = max(1, min(box_width, width - x))
    box_height = max(1, min(box_height, height - y))
    result["heroRect"] = [x, y, box_width, box_height]

    pixels = list(image.crop((x, y, x + box_width, y + box_height)).getdata())
    total = len(pixels)
    non_background = 0
    saturated = 0
    colour_buckets = set()
    for red, green, blue in pixels:
        brightest = max(red, green, blue)
        darkest = min(red, green, blue)
        if brightest > 60 or brightest - darkest > 40:
            non_background += 1
        if brightest > 60 and brightest - darkest > 30:
            saturated += 1
        colour_buckets.add((red // 16, green // 16, blue // 16))
    ratio = non_background / float(total) if total else 0.0
    result.update(
        pixels=total,
        nonBackgroundPixels=non_background,
        nonBackgroundRatio=round(ratio, 4),
        saturatedPixels=saturated,
        distinctColors=len(colour_buckets),
    )

    if box_width * box_height < 3000:
        result["errors"].append("hero rect is only {}x{} pixels".format(box_width, box_height))
    if ratio < 0.08:
        result["errors"].append("hero rect is {:.1f}% non-background, expected at least 8%".format(ratio * 100.0))
    if len(colour_buckets) < 10:
        result["errors"].append("hero rect has only {} colour buckets, expected at least 10".format(len(colour_buckets)))
    if evidence.get("heroVisible") is not True:
        result["errors"].append("runtime evidence does not report heroVisible=true")

    result["passed"] = not result["errors"]
    write(out, result)
    if result["passed"]:
        print("CAPTURE_CHECK_OK frame={} heroRect={}x{} nonBg={:.1f}% colours={}".format(
            args.frame, box_width, box_height, ratio * 100.0, len(colour_buckets)))
        return 0
    print("CAPTURE_CHECK_FAIL " + "; ".join(result["errors"]))
    return 1


def write(path, document):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(document, indent=2) + "\n")


if __name__ == "__main__":
    sys.exit(main())
