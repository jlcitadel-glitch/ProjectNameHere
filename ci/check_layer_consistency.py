#!/usr/bin/env python3
"""Check that layer, sorting layer, and tag references in C# match TagManager."""

import os
import re
import sys

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"

# Unity built-in tags that are always available (not in TagManager.asset tags list)
BUILTIN_TAGS = {"Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera",
                "Player", "GameController", "Enemy"}

# Patterns to find layer/tag/sorting layer references in C#
LAYER_NAME_TO_LAYER = re.compile(
    r'LayerMask\.NameToLayer\(\s*"([^"]+)"\s*\)')
LAYER_GET_MASK = re.compile(
    r'LayerMask\.GetMask\(\s*"([^"]+)"(?:\s*,\s*"([^"]+)")*\s*\)')
LAYER_GET_MASK_ALL = re.compile(r'"([^"]+)"')
SORTING_LAYER_ASSIGN = re.compile(
    r'sortingLayerName\s*=\s*"([^"]+)"')
COMPARE_TAG = re.compile(
    r'CompareTag\(\s*"([^"]+)"\s*\)')

# For detecting editor scripts (warnings only)
EDITOR_PATH_SEGMENTS = {"Editor", "editor"}


def annotation(level, file, line, msg):
    if GITHUB_ACTIONS:
        print(f"::{level} file={file},line={line}::{msg}")
    else:
        tag = "ERROR" if level == "error" else "WARN"
        print(f"  [{tag}] {file}:{line}: {msg}")


def parse_tag_manager(root):
    """Parse TagManager.asset for layers, sorting layers, and tags."""
    tm_path = os.path.join(root, "ProjectSettings", "TagManager.asset")
    if not os.path.exists(tm_path):
        print("ERROR: ProjectSettings/TagManager.asset not found")
        return None, None, None

    with open(tm_path, "r", encoding="utf-8", errors="replace") as f:
        content = f.read()

    # Parse physics layers (lines under "layers:" until next key)
    layers = set()
    in_layers = False
    in_sorting = False
    sorting_layers = set()
    tags = set()
    in_tags = False

    for line in content.splitlines():
        stripped = line.strip()

        # Tags section
        if stripped.startswith("tags:"):
            in_tags = True
            in_layers = False
            in_sorting = False
            # Handle inline empty: "tags: []"
            if "[]" in stripped:
                in_tags = False
            continue

        # Layers section
        if stripped == "layers:":
            in_layers = True
            in_tags = False
            in_sorting = False
            continue

        # Sorting layers section
        if stripped == "m_SortingLayers:":
            in_sorting = True
            in_layers = False
            in_tags = False
            continue

        # Rendering layers ends sorting
        if stripped.startswith("m_RenderingLayers:"):
            in_sorting = False
            continue

        if in_tags and stripped.startswith("- "):
            tag_val = stripped[2:].strip()
            if tag_val:
                tags.add(tag_val)

        if in_layers:
            if stripped.startswith("- "):
                layer_name = stripped[2:].strip()
                if layer_name:
                    layers.add(layer_name)
            elif not stripped.startswith("-"):
                in_layers = False

        if in_sorting:
            name_match = re.match(r"-\s*name:\s*(.+)", stripped)
            if name_match:
                sorting_layers.add(name_match.group(1).strip())

    return layers, sorting_layers, tags


def is_editor_script(filepath):
    """Check if a file is in an Editor folder."""
    parts = filepath.replace("\\", "/").split("/")
    return bool(EDITOR_PATH_SEGMENTS.intersection(parts))


def scan_scripts(root, layers, sorting_layers, tags):
    """Scan C# scripts for layer/tag references and cross-check."""
    scripts_dir = os.path.join(root, "Assets", "_Project", "Scripts")
    if not os.path.isdir(scripts_dir):
        print(f"WARNING: {scripts_dir} not found, scanning Assets/ instead")
        scripts_dir = os.path.join(root, "Assets")

    errors = 0
    warnings = 0
    files_scanned = 0

    all_tags = tags | BUILTIN_TAGS

    for dirpath, dirnames, filenames in os.walk(scripts_dir):
        dirnames[:] = [d for d in dirnames if d not in {".git", "obj"}]

        for fname in filenames:
            if not fname.endswith(".cs"):
                continue

            filepath = os.path.join(dirpath, fname)
            rel_path = os.path.relpath(filepath, root).replace("\\", "/")
            is_editor = is_editor_script(rel_path)
            level = "warning" if is_editor else "error"
            files_scanned += 1

            try:
                with open(filepath, "r", encoding="utf-8", errors="replace") as f:
                    lines = f.readlines()
            except OSError:
                continue

            for line_num, line in enumerate(lines, 1):
                # Check LayerMask.NameToLayer
                for match in LAYER_NAME_TO_LAYER.finditer(line):
                    name = match.group(1)
                    if name not in layers:
                        annotation(level, rel_path, line_num,
                                   f'Layer "{name}" not defined in TagManager')
                        if is_editor:
                            warnings += 1
                        else:
                            errors += 1

                # Check LayerMask.GetMask — extract all quoted strings in the call
                for outer_match in LAYER_GET_MASK.finditer(line):
                    full_call = line[outer_match.start():outer_match.end()]
                    for inner in LAYER_GET_MASK_ALL.finditer(full_call):
                        name = inner.group(1)
                        if name not in layers:
                            annotation(level, rel_path, line_num,
                                       f'Layer "{name}" not defined in TagManager')
                            if is_editor:
                                warnings += 1
                            else:
                                errors += 1

                # Check sorting layer assignments
                for match in SORTING_LAYER_ASSIGN.finditer(line):
                    name = match.group(1)
                    if name not in sorting_layers:
                        annotation(level, rel_path, line_num,
                                   f'Sorting layer "{name}" not defined in TagManager')
                        if is_editor:
                            warnings += 1
                        else:
                            errors += 1

                # Check CompareTag
                for match in COMPARE_TAG.finditer(line):
                    name = match.group(1)
                    if name not in all_tags:
                        annotation(level, rel_path, line_num,
                                   f'Tag "{name}" not defined in TagManager')
                        if is_editor:
                            warnings += 1
                        else:
                            errors += 1

    print(f"  Scanned {files_scanned} C# files")
    return errors, warnings


def check_layer_consistency(root):
    print("Parsing TagManager.asset...")
    layers, sorting_layers, tags = parse_tag_manager(root)

    if layers is None:
        return 1

    print(f"  Physics layers: {sorted(layers)}")
    print(f"  Sorting layers: {sorted(sorting_layers)}")
    print(f"  Custom tags: {sorted(tags)}")
    print(f"  Built-in tags: {sorted(BUILTIN_TAGS)}")

    print("\nScanning C# scripts...")
    errors, warnings = scan_scripts(root, layers, sorting_layers, tags)

    if errors:
        print(f"\n{errors} error(s), {warnings} warning(s)")
    elif warnings:
        print(f"\nNo errors. {warnings} warning(s) (editor scripts only).")
    else:
        print("\nAll layer/tag references OK")

    return 1 if errors > 0 else 0


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(script_dir)

    if not os.path.isdir(os.path.join(root, "ProjectSettings")):
        print(f"ERROR: Cannot find ProjectSettings/ from {root}")
        return 1

    print("=" * 60)
    print("Layer / Tag Consistency Check")
    print("=" * 60)

    return check_layer_consistency(root)


if __name__ == "__main__":
    sys.exit(main())
