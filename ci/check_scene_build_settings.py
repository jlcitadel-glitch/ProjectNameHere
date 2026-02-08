#!/usr/bin/env python3
"""Validate that build scenes exist on disk and GUIDs match their .meta files."""

import os
import re
import sys

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"

SCENE_ENTRY_PATTERN = re.compile(
    r"path:\s*(.+\.unity)\s*\n\s*guid:\s*([0-9a-f]{32})",
    re.MULTILINE)
META_GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})", re.MULTILINE)


def annotation(level, file, msg):
    if GITHUB_ACTIONS:
        print(f"::{level} file={file}::{msg}")
    else:
        tag = "ERROR" if level == "error" else "WARN"
        print(f"  [{tag}] {file}: {msg}")


def check_scene_build_settings(root):
    settings_path = os.path.join(root, "ProjectSettings", "EditorBuildSettings.asset")
    if not os.path.exists(settings_path):
        print("ERROR: ProjectSettings/EditorBuildSettings.asset not found")
        return 1

    with open(settings_path, "r", encoding="utf-8", errors="replace") as f:
        content = f.read()

    # Also check for enabled flag — extract scene blocks more carefully
    # Unity format:
    #   - enabled: 1
    #     path: Assets/Scenes/Foo.unity
    #     guid: abc123...
    scene_block = re.compile(
        r"-\s*enabled:\s*(\d+)\s*\n"
        r"\s*path:\s*(.+\.unity)\s*\n"
        r"\s*guid:\s*([0-9a-f]{32})",
        re.MULTILINE)

    scenes = scene_block.findall(content)

    if not scenes:
        print("WARNING: No scenes found in build settings")
        return 0

    errors = 0
    settings_rel = "ProjectSettings/EditorBuildSettings.asset"

    print(f"Found {len(scenes)} scene(s) in build settings:\n")

    for enabled, scene_path, expected_guid in scenes:
        scene_path = scene_path.strip()
        enabled = enabled.strip() == "1"
        status = "enabled" if enabled else "disabled"
        full_path = os.path.join(root, scene_path.replace("/", os.sep))
        meta_path = full_path + ".meta"

        print(f"  [{status}] {scene_path}")

        # Check scene file exists
        if not os.path.exists(full_path):
            annotation("error", settings_rel,
                       f'Build scene not found on disk: {scene_path}')
            errors += 1
            continue

        # Check .meta exists
        if not os.path.exists(meta_path):
            annotation("error", settings_rel,
                       f'Scene .meta file missing: {scene_path}.meta')
            errors += 1
            continue

        # Check GUID matches
        with open(meta_path, "r", encoding="utf-8", errors="replace") as f:
            meta_content = f.read(512)

        match = META_GUID_PATTERN.search(meta_content)
        if not match:
            annotation("error", settings_rel,
                       f'Cannot read GUID from {scene_path}.meta')
            errors += 1
        elif match.group(1) != expected_guid:
            annotation("error", settings_rel,
                       f'GUID mismatch for {scene_path}: '
                       f'build settings has {expected_guid}, '
                       f'.meta has {match.group(1)}')
            errors += 1
        else:
            print(f"    GUID OK: {expected_guid}")

    if errors:
        print(f"\n{errors} error(s) in build scene validation")
    else:
        print("\nAll build scenes OK")

    return 1 if errors > 0 else 0


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(script_dir)

    if not os.path.isdir(os.path.join(root, "ProjectSettings")):
        print(f"ERROR: Cannot find ProjectSettings/ from {root}")
        return 1

    print("=" * 60)
    print("Build Scene Validation")
    print("=" * 60)

    return check_scene_build_settings(root)


if __name__ == "__main__":
    sys.exit(main())
