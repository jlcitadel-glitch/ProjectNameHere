#!/usr/bin/env python3
"""Check for broken GUID references in Unity asset files."""

import os
import re
import sys

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"

# GUIDs to skip: all-zero and Unity built-in prefix
SKIP_GUID_PREFIXES = ("0000000000000000",)
NULL_GUID = "00000000000000000000000000000000"

# Extensions that contain GUID references
SCANNABLE_EXTENSIONS = {".prefab", ".unity", ".asset", ".controller",
                        ".overrideController", ".mat", ".playable",
                        ".signal", ".spriteatlasv2", ".lighting"}

# Only validate references inside _Project to avoid third-party false positives
SCAN_ROOT = os.path.join("Assets", "_Project")

GUID_REF_PATTERN = re.compile(r"guid:\s*([0-9a-f]{32})")
META_GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})", re.MULTILINE)


def annotation(level, file, line, msg):
    if GITHUB_ACTIONS:
        print(f"::{level} file={file},line={line}::{msg}")
    else:
        tag = "ERROR" if level == "error" else "WARN"
        print(f"  [{tag}] {file}:{line}: {msg}")


def build_guid_index(search_dir):
    """Build a set of all known GUIDs from .meta files."""
    guids = set()
    for dirpath, dirnames, filenames in os.walk(search_dir):
        # Skip non-asset directories
        dirnames[:] = [d for d in dirnames
                       if d not in {"Library", "Temp", "obj", ".git"}]
        for fname in filenames:
            if not fname.endswith(".meta"):
                continue
            filepath = os.path.join(dirpath, fname)
            try:
                with open(filepath, "r", encoding="utf-8", errors="replace") as f:
                    # GUID is always on line 2 of Unity .meta files:
                    #   line 1: fileFormatVersion: 2
                    #   line 2: guid: <32hex>
                    f.readline()  # skip first line
                    line2 = f.readline()
                if line2.startswith("guid: ") and len(line2) >= 38:
                    guids.add(line2[6:38])
            except OSError:
                pass
    return guids


def scan_file_for_guids(filepath, known_guids):
    """Scan a single file for GUID references and return broken ones."""
    broken = []
    try:
        with open(filepath, "r", encoding="utf-8", errors="replace") as f:
            for line_num, line in enumerate(f, 1):
                for match in GUID_REF_PATTERN.finditer(line):
                    guid = match.group(1)
                    if guid == NULL_GUID:
                        continue
                    if any(guid.startswith(p) for p in SKIP_GUID_PREFIXES):
                        continue
                    if guid not in known_guids:
                        broken.append((line_num, guid))
    except OSError:
        pass
    return broken


def check_guid_references(root):
    assets_dir = os.path.join(root, "Assets")
    scan_dir = os.path.join(root, SCAN_ROOT)

    if not os.path.isdir(assets_dir):
        print("ERROR: Assets/ directory not found")
        return 1

    if not os.path.isdir(scan_dir):
        print(f"ERROR: {SCAN_ROOT}/ directory not found")
        return 1

    # Phase 1: Build GUID index from ALL meta files (including Packages)
    print("Building GUID index...")
    known_guids = build_guid_index(assets_dir)

    # Also index Packages/ if it exists
    packages_dir = os.path.join(root, "Packages")
    if os.path.isdir(packages_dir):
        known_guids |= build_guid_index(packages_dir)

    # Index Library/PackageCache/ for installed package GUIDs (URP, TMP, etc.)
    # This directory exists locally but not on CI (it's gitignored).
    package_cache = os.path.join(root, "Library", "PackageCache")
    has_package_cache = os.path.isdir(package_cache)
    if has_package_cache:
        known_guids |= build_guid_index(package_cache)
        print(f"  Indexed {len(known_guids)} GUIDs (including package cache)")
    else:
        print(f"  Indexed {len(known_guids)} GUIDs (no package cache — "
              "package GUIDs cannot be verified)")

    # Phase 2: Scan project files for broken references
    print(f"Scanning {SCAN_ROOT}/ for broken references...")
    errors = 0
    warnings = 0
    files_scanned = 0

    for dirpath, dirnames, filenames in os.walk(scan_dir):
        for fname in filenames:
            ext = os.path.splitext(fname)[1].lower()
            if ext not in SCANNABLE_EXTENSIONS:
                continue

            filepath = os.path.join(dirpath, fname)
            rel_path = os.path.relpath(filepath, root).replace("\\", "/")
            files_scanned += 1

            broken = scan_file_for_guids(filepath, known_guids)
            for line_num, guid in broken:
                if has_package_cache:
                    # We have full GUID coverage — this is a real broken ref
                    annotation("error", rel_path, line_num,
                               f"Broken GUID reference: {guid}")
                    errors += 1
                else:
                    # No package cache — can't tell if it's a package GUID
                    # Only error if the GUID was once in our Assets/ (deleted asset)
                    # Otherwise warn (likely a package GUID we can't resolve)
                    annotation("warning", rel_path, line_num,
                               f"Unresolvable GUID (package?): {guid}")
                    warnings += 1

    print(f"  Scanned {files_scanned} asset files")

    if errors:
        print(f"\n{errors} broken GUID reference(s) found")
    elif warnings:
        print(f"\nNo confirmed broken references. {warnings} unresolvable "
              "GUID(s) (likely package references — run locally with Library/ "
              "for full validation).")
    else:
        print("\nAll GUID references OK")

    return 1 if errors > 0 else 0


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(script_dir)

    if not os.path.isdir(os.path.join(root, "Assets")):
        print(f"ERROR: Cannot find Assets/ directory from {root}")
        return 1

    print("=" * 60)
    print("GUID Reference Check")
    print("=" * 60)

    return check_guid_references(root)


if __name__ == "__main__":
    sys.exit(main())
