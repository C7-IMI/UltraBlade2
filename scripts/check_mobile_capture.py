#!/usr/bin/env python3
"""Validate the Android screenshot: a real device frame with the game rendered.

Checks the PNG dimensions, the non-background share and the colour variety of the frame,
and that the matching logcat shows the Unity player and the UBlade2 hero markers.

Usage:
    python scripts/check_mobile_capture.py --frame artifacts/ublade2-mobile/screen.png \
        --logcat artifacts/ublade2-mobile/logcat-launch.txt \
        --out artifacts/ublade2-mobile/capture-check.json
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


def main(argv=None):
    parser = argparse.ArgumentParser(description="Validate the UBlade2 Android screenshot.")
    parser.add_argument("--frame", required=True)
    parser.add_argument("--logcat", default=None)
    parser.add_argument("--out", required=True)
    args = parser.parse_args(argv)

    result = {
        "schema": "ublade2.mobile.capture-check/v1",
        "frame": args.frame,
        "passed": False,
        "errors": [],
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    try:
        image = Image.open(Path(args.frame)).convert("RGB")
    except Exception as exc:  # noqa: BLE001
        result["errors"].append("frame unreadable: " + str(exc))
        write(Path(args.out), result)
        return 1
    width, height = image.size
    result["width"] = width
    result["height"] = height
    if width < 1000 or height < 1000:
        result["errors"].append("frame is {}x{}, expected at least 1000x1000".format(width, height))

    stat_image = Image.open(Path(args.frame)).convert("RGB")
    small = stat_image.resize((max(1, width // 8), max(1, height // 8)))
    pixels = list(small.getdata())
    non_background = 0
    buckets = set()
    for red, green, blue in pixels:
        brightest = max(red, green, blue)
        darkest = min(red, green, blue)
        if brightest > 55 or brightest - darkest > 30:
            non_background += 1
        buckets.add((red // 16, green // 16, blue // 16))
    ratio = non_background / float(len(pixels)) if pixels else 0.0
    result.update(
        sampledPixels=len(pixels),
        nonBackgroundRatio=round(ratio, 4),
        distinctColors=len(buckets),
    )
    if ratio < 0.05:
        result["errors"].append("frame is {:.1f}% non-background, expected at least 5%".format(ratio * 100))
    if len(buckets) < 20:
        result["errors"].append("frame has only {} colour buckets, expected at least 20".format(len(buckets)))

    if args.logcat:
        log_path = Path(args.logcat)
        if not log_path.is_file():
            result["errors"].append("logcat missing: " + args.logcat)
        else:
            text = log_path.read_text(encoding="utf-8-sig", errors="replace")
            for marker in ("UBLADE2_START", "UBLADE_HERO_OK"):
                if marker not in text:
                    result["errors"].append("logcat lacks " + marker)

    result["passed"] = not result["errors"]
    write(Path(args.out), result)
    if result["passed"]:
        print("MOBILE_CAPTURE_OK frame={} resolution={}x{} nonBg={:.1f}% colours={}".format(
            args.frame, width, height, ratio * 100, len(buckets)))
        return 0
    print("MOBILE_CAPTURE_FAIL " + "; ".join(result["errors"]))
    return 1


def write(path, document):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(document, indent=2) + "\n")


if __name__ == "__main__":
    sys.exit(main())
