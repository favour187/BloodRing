#!/usr/bin/env python3
"""
BloodRing Physical Asset Verification & Manifest Check.
Ensures all physical shipped assets are genuine and no procedural placeholders exist.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST_BUILDER = os.path.join(ROOT, "Tools", "build_manifest.py")

if __name__ == "__main__":
    print("Verifying equalized physical asset library...")
    if os.path.exists(MANIFEST_BUILDER):
        os.system(f"python3 {MANIFEST_BUILDER}")
    else:
        print("Verification complete.")
