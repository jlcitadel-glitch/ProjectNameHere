#!/usr/bin/env python3
"""Check that every asset file has a .meta and no orphaned .meta files exist."""

import os
import sys

SKIP_DIRS = {"Library", "Temp", "obj", "Logs", "Build", "Builds",
             "MemoryCaptures", "UserSettings", ".git", ".vs", ".vscode",
             ".idea", ".gradle", ".beads", ".claude", "ci", ".github"}

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"


def annotation(level, file, msg):
    if GITHUB_ACTIONS:
        print(f"::{level} file={file}::{msg}")
    else:
        tag = "ERROR" if level == "error" else "WARN"
        print(f"  [{tag}] {file}: {msg}")


def check_meta_files(root):
    assets_dir = os.path.join(root, "Assets")
    if not os.path.isdir(assets_dir):
        print("ERROR: Assets/ directory not found")
        return 1

    missing = []
    orphaned = []

    for dirpath, dirnames, filenames in os.walk(assets_dir):
        # Skip excluded directories
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]

        rel_dir = os.path.relpath(dirpath, root)

        # Check that every non-meta file has a .meta
        for fname in filenames:
            if fname.endswith(".meta"):
                continue
            meta_path = os.path.join(dirpath, fname + ".meta")
            if not os.path.exists(meta_path):
                rel = os.path.join(rel_dir, fname)
                missing.append(rel)

        # Check that every .meta has a corresponding asset
        for fname in filenames:
            if not fname.endswith(".meta"):
                continue
            asset_name = fname[:-5]  # strip .meta
            asset_path = os.path.join(dirpath, asset_name)
            if not os.path.exists(asset_path) and not os.path.isdir(asset_path):
                rel = os.path.join(rel_dir, fname)
                orphaned.append(rel)

        # Check that every subdirectory has a .meta
        for dname in dirnames:
            meta_path = os.path.join(dirpath, dname + ".meta")
            if not os.path.exists(meta_path):
                rel = os.path.join(rel_dir, dname)
                missing.append(rel)

    # Report results
    errors = 0

    if missing:
        print(f"\nMissing .meta files ({len(missing)}):")
        for f in sorted(missing):
            annotation("error", f.replace("\\", "/"), "Missing .meta file")
            errors += 1

    if orphaned:
        print(f"\nOrphaned .meta files ({len(orphaned)}):")
        for f in sorted(orphaned):
            annotation("warning", f.replace("\\", "/"), "Orphaned .meta (asset deleted but .meta remains)")

    if not missing and not orphaned:
        print("All meta files OK")
    elif not missing:
        print(f"\nNo missing metas. {len(orphaned)} orphaned meta(s) (warnings only).")

    return 1 if errors > 0 else 0


def main():
    # Find project root (walk up from script location to find Assets/)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(script_dir)

    if not os.path.isdir(os.path.join(root, "Assets")):
        print(f"ERROR: Cannot find Assets/ directory from {root}")
        return 1

    print("=" * 60)
    print("Meta File Integrity Check")
    print("=" * 60)

    return check_meta_files(root)


if __name__ == "__main__":
    sys.exit(main())
