#!/usr/bin/env python3
"""
Scans all C# files under Assets/Scripts and replaces GameObject.CreatePrimitive(...)
with real 3D mesh model asset loading via BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D(...).
"""

import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS_DIR = os.path.join(ROOT, "Assets", "Scripts")

replacements = {
    "GameObject.CreatePrimitive(PrimitiveType.Cube)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj")',
    "GameObject.CreatePrimitive(PrimitiveType.Sphere)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj")',
    "GameObject.CreatePrimitive(PrimitiveType.Cylinder)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj")',
    "GameObject.CreatePrimitive(PrimitiveType.Capsule)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Capsule.obj")',
    "GameObject.CreatePrimitive(PrimitiveType.Quad)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Quad.obj")',
    "GameObject.CreatePrimitive(PrimitiveType.Plane)": 'BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Plane.obj")',
}

files_modified = 0
total_replacements = 0

for root, _, files in os.walk(SCRIPTS_DIR):
    for f in files:
        if f.endswith(".cs") and f != "BloodRingArtLibrary.cs":
            path = os.path.join(root, f)
            with open(path, "r", encoding="utf-8", errors="ignore") as file:
                content = file.read()
            
            orig = content
            for old, new in replacements.items():
                count = content.count(old)
                if count > 0:
                    content = content.replace(old, new)
                    total_replacements += count
            
            if content != orig:
                with open(path, "w", encoding="utf-8") as file:
                    file.write(content)
                files_modified += 1
                print(f"Replaced primitives in: {os.path.relpath(path, ROOT)}")

print(f"\nCompleted! Modified {files_modified} files with {total_replacements} total 3D model replacements.")
