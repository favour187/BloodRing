#!/usr/bin/env python3
"""
Blood Ring — Deterministic .meta generator.

The project was historically committed WITHOUT Unity .meta files, which means
every fresh clone/CI machine generated random GUIDs, breaking asset references
and making builds non-reproducible. This tool creates stable, deterministic
.meta files (GUID = md5 of the asset's project-relative path) so GUIDs are
version-controlled and identical on every machine.

Run:  python3 Tools/generate_meta_files.py [--force]

NOTE: Because the repo shipped without metas, existing scene/prefab component
wiring that referenced the original author's GUIDs must be re-linked once inside
the Unity Editor. This tool makes GUIDs deterministic from now on; it cannot
recover GUIDs that were never committed.
"""
import os, sys, hashlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
FORCE = "--force" in sys.argv

# Extensions we never create metas for (Unity/temporary/ignored)
SKIP_EXT = {".meta"}

def guid_for(rel_path):
    return hashlib.md5(rel_path.replace(os.sep, "/").encode("utf-8")).hexdigest()[:32]

def importer_block(ext):
    if ext == ".cs":
        return ("MonoImporter:\n"
                "  externalObjects: {}\n"
                "  serializedVersion: 2\n"
                "  defaultReferences: []\n"
                "  executionOrder: 0\n"
                "  icon: {instanceID: 0}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext in (".png", ".jpg", ".jpeg", ".tga", ".psd"):
        return ("TextureImporter:\n"
                "  internalIDToNameTable: []\n"
                "  externalObjects: {}\n"
                "  serializedVersion: 12\n"
                "  mipmaps:\n"
                "    mipMapMode: 0\n"
                "    enableMipMap: 1\n"
                "  textureType: 0\n"
                "  textureShape: 1\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext in (".wav", ".mp3", ".ogg", ".aif", ".aiff"):
        return ("AudioImporter:\n"
                "  externalObjects: {}\n"
                "  serializedVersion: 6\n"
                "  defaultSettings:\n"
                "    loadType: 0\n"
                "    sampleRateSetting: 0\n"
                "    compressionFormat: 1\n"
                "    quality: 1\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext == ".obj":
        return ("ModelImporter:\n"
                "  serializedVersion: 22200\n"
                "  internalIDToNameTable: []\n"
                "  externalObjects: {}\n"
                "  materials:\n"
                "    materialImportMode: 2\n"
                "  meshes:\n"
                "    globalScale: 1\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext in (".asset", ".mat", ".controller", ".anim", ".physicMaterial", ".mixer"):
        return ("NativeFormatImporter:\n"
                "  externalObjects: {}\n"
                "  mainObjectFileID: 11400000\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext == ".prefab":
        return ("PrefabImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    if ext == ".unity":
        return ("DefaultImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    return ("DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n")

def write_meta(target_path, is_folder):
    meta_path = target_path + ".meta"
    if os.path.exists(meta_path) and not FORCE:
        return False
    rel = os.path.relpath(target_path, ROOT)
    guid = guid_for(rel)
    if is_folder:
        body = ("fileFormatVersion: 2\n"
                f"guid: {guid}\n"
                "folderAsset: yes\n"
                "DefaultImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n")
    else:
        ext = os.path.splitext(target_path)[1].lower()
        body = ("fileFormatVersion: 2\n"
                f"guid: {guid}\n" + importer_block(ext))
    with open(meta_path, "w") as f:
        f.write(body)
    return True

def main():
    created = 0
    for base in (ASSETS, os.path.join(ROOT, "Packages")):
        if not os.path.isdir(base):
            continue
        for cur, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if not d.endswith(".meta")]
            for d in dirs:
                if write_meta(os.path.join(cur, d), True):
                    created += 1
            for fn in files:
                ext = os.path.splitext(fn)[1].lower()
                if ext in SKIP_EXT:
                    continue
                if write_meta(os.path.join(cur, fn), False):
                    created += 1
    print(f"[generate_meta_files] created {created} .meta files (deterministic GUIDs).")

if __name__ == "__main__":
    main()
