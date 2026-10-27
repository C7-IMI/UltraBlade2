#!/usr/bin/env python3
"""Validate the six playable-slice UI screenshots captured by the player.

Checks every expected PNG exists, is 1280x720, and shows real rendered UI content. The match frame
is brighter than the dark menu/league/settings/pause/result screens, so the bars are per screen.
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image

EXPECTED = {
    "menu.png": 0.04,
    "league.png": 0.025,
    "settings.png": 0.025,
    "match.png": 0.15,
    "pause.png": 0.02,
    "result.png": 0.02,
}


def main(argv=None):
    parser = argparse.ArgumentParser(description="Validate UBlade2 UI screenshots.")
    parser.add_argument("--dir", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args(argv)

    root = Path(args.dir)
    result = {
        "schema": "ublade2.ui.captures/v1",
        "dir": args.dir,
        "passed": False,
        "screens": {},
        "errors": [],
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    for name, min_ratio in EXPECTED.items():
        path = root / name
        if not path.is_file():
            result["errors"].append("missing screenshot: " + name)
            continue
        try:
            image = Image.open(path).convert("RGB")
        except Exception as exc:  # noqa: BLE001
            result["errors"].append("unreadable screenshot {}: {}".format(name, exc))
            continue
        width, height = image.size
        pixels = list(image.resize((max(1, width // 8), max(1, height // 8))).getdata())
        non_background = 0
        buckets = set()
        for red, green, blue in pixels:
            brightest = max(red, green, blue)
            darkest = min(red, green, blue)
            if brightest > 60 or brightest - darkest > 40:
                non_background += 1
            buckets.add((red // 16, green // 16, blue // 16))
        ratio = non_background / float(len(pixels)) if pixels else 0.0
        screen = {
            "width": width,
            "height": height,
            "nonBackgroundRatio": round(ratio, 4),
            "distinctColors": len(buckets),
        }
        result["screens"][name] = screen
        if (width, height) != (1280, 720):
            result["errors"].append("{} is {}x{}, expected 1280x720".format(name, width, height))
        if ratio < min_ratio:
            result["errors"].append("{} is only {:.1f}% non-background, expected at least {:.1f}%".format(
                name, ratio * 100.0, min_ratio * 100.0))
        if len(buckets) < 30:
            result["errors"].append("{} has only {} colour buckets, expected at least 30".format(name, len(buckets)))

    result["passed"] = not result["errors"]
    Path(args.out).parent.mkdir(parents=True, exist_ok=True)
    with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(result, indent=2) + "\n")
    if result["passed"]:
        print("UI_CAPTURES_CHECK_OK dir={} screens={}".format(args.dir, len(result["screens"])))
        return 0
    print("UI_CAPTURES_CHECK_FAIL " + "; ".join(result["errors"]))
    return 1


if __name__ == "__main__":
    sys.exit(main())
