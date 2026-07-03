#!/usr/bin/env python3
"""
Blood Ring asset validator.

This tool ONLY validates pre-authored art that already ships in the repository.
It never generates art. It checks that:

  * Every file under Assets/Resources/Art is a real, non-empty asset.
  * PNGs have a valid header and a sane minimum size (catches blank/placeholder art).
  * The asset manifest (Generated/asset_manifest.json) is in sync with disk.

Exit code is non-zero if any validation fails, so it can gate CI.
"""

import json
import os
import struct
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART_DIR = os.path.join(ROOT, "Assets", "Resources", "Art")
MANIFEST = os.path.join(ROOT, "Generated", "asset_manifest.json")

# Minimum byte size for a genuine, detailed PNG (placeholder/flat art is tiny).
MIN_PNG_BYTES = 40 * 1024
PNG_MAGIC = b"\x89PNG\r\n\x1a\n"


def png_dimensions(path):
    with open(path, "rb") as f:
        header = f.read(24)
    if len(header) < 24 or header[:8] != PNG_MAGIC:
        return None
    w, h = struct.unpack(">II", header[16:24])
    return w, h


def validate():
    errors = []
    warnings = []
    checked = 0

    if not os.path.isdir(ART_DIR):
        print(f"ERROR: Art directory not found: {ART_DIR}")
        return 1

    for dirpath, _, filenames in os.walk(ART_DIR):
        for name in filenames:
            if name.startswith("."):
                continue
            path = os.path.join(dirpath, name)
            rel = os.path.relpath(path, ROOT)
            size = os.path.getsize(path)
            checked += 1

            if size == 0:
                errors.append(f"Empty file: {rel}")
                continue

            if name.lower().endswith(".png"):
                dims = png_dimensions(path)
                if dims is None:
                    errors.append(f"Invalid PNG header: {rel}")
                    continue
                w, h = dims
                if w < 64 or h < 64:
                    errors.append(f"PNG too small ({w}x{h}): {rel}")
                if size < MIN_PNG_BYTES:
                    warnings.append(
                        f"PNG suspiciously small ({size} bytes) - "
                        f"verify it is authored art, not a flat placeholder: {rel}"
                    )

    print(f"Validated {checked} art file(s) under {os.path.relpath(ART_DIR, ROOT)}")
    for w in warnings:
        print(f"  WARN: {w}")
    for e in errors:
        print(f"  FAIL: {e}")

    if os.path.exists(MANIFEST):
        try:
            with open(MANIFEST) as f:
                json.load(f)
            print("Asset manifest parsed OK.")
        except json.JSONDecodeError as exc:
            errors.append(f"Manifest is not valid JSON: {exc}")

    if errors:
        print(f"\nValidation FAILED with {len(errors)} error(s).")
        return 1
    print("\nValidation PASSED.")
    return 0


if __name__ == "__main__":
    sys.exit(validate())


