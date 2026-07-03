#!/usr/bin/env python3
"""
Builds Generated/asset_manifest.json by scanning ALL authored art, audio, 3D model,
and UI libraries across the workspace.
"""

import json
import os
import struct

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Generated", "asset_manifest.json")
PNG_MAGIC = b"\x89PNG\r\n\x1a\n"

def png_dims(path):
    try:
        with open(path, "rb") as f:
            header = f.read(24)
        if header[:8] == PNG_MAGIC:
            w, h = struct.unpack(">II", header[16:24])
            return [w, h]
    except Exception:
        pass
    return None

def scan(base, kind):
    items = []
    if not os.path.isdir(base):
        return items
    for dirpath, _, filenames in os.walk(base):
        for name in sorted(filenames):
            if name.startswith(".") or name.endswith(".json") or name.endswith(".md") or name.endswith(".txt") or name.endswith(".py"):
                continue
            path = os.path.join(dirpath, name)
            rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
            res_rel = os.path.relpath(path, os.path.join(ROOT, "Assets", "Resources"))
            res_key = os.path.splitext(res_rel)[0].replace(os.sep, "/") if "Resources" in path else os.path.splitext(rel)[0]
            category = os.path.relpath(dirpath, base).replace(os.sep, "/")
            entry = {
                "key": os.path.splitext(name)[0],
                "category": kind + ("/" + category if category != "." else ""),
                "path": rel,
                "resourcePath": res_key,
                "bytes": os.path.getsize(path),
                "type": os.path.splitext(name)[1].lstrip(".").lower(),
            }
            dims = png_dims(path)
            if dims:
                entry["dimensions"] = dims
            items.append(entry)
    return items

def main():
    assets = (
        scan(os.path.join(ROOT, "Assets", "Resources", "Art"), "art") +
        scan(os.path.join(ROOT, "Assets", "Resources", "Audio"), "audio") +
        scan(os.path.join(ROOT, "Assets", "Resources", "Models"), "models") +
        scan(os.path.join(ROOT, "Assets", "Audio"), "audio_master") +
        scan(os.path.join(ROOT, "UI"), "ui_system")
    )
    by_cat = {}
    for a in assets:
        by_cat[a["category"]] = by_cat.get(a["category"], 0) + 1

    manifest = {
        "project": "Blood Ring Apex Royale 3D Enterprise",
        "description": "Comprehensive equalized art, audio, 3D model, and UI asset manifest. All entries are real shipped files.",
        "assetCount": len(assets),
        "categories": dict(sorted(by_cat.items())),
        "assets": assets,
    }

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(manifest, f, indent=2)
    print(f"Wrote {OUT} with {len(assets)} asset(s) across {len(by_cat)} categories.")

if __name__ == "__main__":
    main()
