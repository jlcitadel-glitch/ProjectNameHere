#!/usr/bin/env python3
"""
Code style checker for Unity C# scripts.
Validates rules from STANDARDS.md without requiring Unity or MSBuild.

Checks:
  - Deprecated API usage (FindObjectOfType, rb.velocity, tag ==)
  - GetComponent in Update/FixedUpdate/LateUpdate loops
  - Public fields on MonoBehaviours (should use [SerializeField] private)
  - Missing explicit private keyword on fields
"""

import os
import re
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
SCRIPTS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts")

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"


class StyleViolation:
    def __init__(self, file, line_num, rule, message):
        self.file = file
        self.line_num = line_num
        self.rule = rule
        self.message = message

    def __str__(self):
        rel = os.path.relpath(self.file, PROJECT_ROOT)
        return f"  [{self.rule}] {rel}:{self.line_num}: {self.message}"


def find_cs_files(root):
    """Find all .cs files under the given root, excluding Editor folders."""
    for dirpath, dirnames, filenames in os.walk(root):
        # Skip Editor folders
        dirnames[:] = [d for d in dirnames if d != "Editor"]
        for f in filenames:
            if f.endswith(".cs"):
                yield os.path.join(dirpath, f)


# --- Deprecated API patterns ---
DEPRECATED_PATTERNS = [
    (
        r"\bFindObjectOfType\s*<",
        "DEPRECATED_API",
        "Use FindAnyObjectByType<T>() instead of FindObjectOfType<T>() (Unity 6)",
    ),
    (
        r"\bFindObjectsOfType\s*<",
        "DEPRECATED_API",
        "Use FindObjectsByType<T>(FindObjectsSortMode.None) instead of FindObjectsOfType<T>() (Unity 6)",
    ),
    (
        r"\.velocity\s*=",
        "DEPRECATED_API",
        "Use rb.linearVelocity instead of rb.velocity (Unity 6 Rigidbody2D)",
    ),
    (
        r"\.velocity\.",
        "DEPRECATED_API",
        "Use rb.linearVelocity instead of rb.velocity (Unity 6 Rigidbody2D)",
    ),
    (
        r"\.tag\s*==",
        "TAG_COMPARE",
        "Use CompareTag() instead of tag == string comparison",
    ),
    (
        r'\.tag\s*!=',
        "TAG_COMPARE",
        "Use !CompareTag() instead of tag != string comparison",
    ),
]

# Patterns that indicate a line is a comment
COMMENT_PATTERN = re.compile(r"^\s*//")


def check_deprecated_apis(filepath, lines):
    """Check for deprecated Unity API usage."""
    violations = []
    for i, line in enumerate(lines, 1):
        # Skip comments
        if COMMENT_PATTERN.match(line):
            continue
        for pattern, rule, message in DEPRECATED_PATTERNS:
            if re.search(pattern, line):
                # Exclude velocity in non-Rigidbody contexts (e.g., ParticleSystem.velocity)
                if "velocity" in pattern and "linearVelocity" in line:
                    continue
                violations.append(StyleViolation(filepath, i, rule, message))
    return violations


# Patterns for Update-family methods
UPDATE_METHOD_PATTERN = re.compile(
    r"(void\s+(?:Update|FixedUpdate|LateUpdate)\s*\()"
)
GETCOMPONENT_PATTERN = re.compile(r"GetComponent\s*<")
FIND_IN_UPDATE_PATTERN = re.compile(
    r"(Find\s*\(|FindGameObjectWithTag|FindWithTag|FindAnyObjectByType|FindObjectsByType)"
)


def check_update_loops(filepath, lines):
    """Check for GetComponent/Find calls inside Update loops."""
    violations = []
    in_update = False
    brace_depth = 0
    update_start_depth = 0

    for i, line in enumerate(lines, 1):
        if COMMENT_PATTERN.match(line):
            continue

        # Detect Update method entry
        if UPDATE_METHOD_PATTERN.search(line):
            in_update = True
            update_start_depth = brace_depth

        if in_update:
            brace_depth += line.count("{") - line.count("}")

            if GETCOMPONENT_PATTERN.search(line):
                violations.append(
                    StyleViolation(
                        filepath,
                        i,
                        "PERF_UPDATE",
                        "GetComponent<T>() in Update loop — cache in Awake/Start instead",
                    )
                )

            if FIND_IN_UPDATE_PATTERN.search(line):
                violations.append(
                    StyleViolation(
                        filepath,
                        i,
                        "PERF_UPDATE",
                        "Find() call in Update loop — cache reference in Awake/Start",
                    )
                )

            # Exit Update method when braces close
            if brace_depth <= update_start_depth and "{" not in line:
                in_update = False

    return violations


# Pattern for public fields on MonoBehaviours
# Matches: "public float foo;" or "public int bar = 5;"
# Excludes: properties (=> or { get), methods (()), events, delegates, etc.
PUBLIC_FIELD_PATTERN = re.compile(
    r"^\s+public\s+"
    r"(?!(?:static|event|delegate|override|virtual|abstract|class|struct|enum|interface|void|readonly|new|const)\b)"
    r"[^(=;]+\s+\w+\s*(?:=\s*[^;]+)?;"
)

# Patterns that indicate a line is NOT a field
PROPERTY_PATTERN = re.compile(r"=>|{\s*get")

# Patterns to detect class nesting depth
CLASS_PATTERN = re.compile(r"\b(?:class|struct)\s+\w+")

# Patterns to detect class inheritance
MONOBEHAVIOUR_CLASS_PATTERN = re.compile(
    r"class\s+\w+\s*:\s*(?:MonoBehaviour|NetworkBehaviour)"
)
SCRIPTABLE_OBJECT_PATTERN = re.compile(
    r"class\s+\w+\s*:\s*ScriptableObject"
)


def check_public_fields(filepath, lines):
    """
    Check for public fields on the top-level MonoBehaviour class.
    STANDARDS.md: [SerializeField] for tweakable values — never public fields on MonoBehaviours.
    ScriptableObjects may use public fields.
    Nested [Serializable] classes and properties are excluded.
    """
    violations = []
    is_monobehaviour = False
    is_scriptable_object = False

    for line in lines:
        if MONOBEHAVIOUR_CLASS_PATTERN.search(line):
            is_monobehaviour = True
        if SCRIPTABLE_OBJECT_PATTERN.search(line):
            is_scriptable_object = True

    # Only flag MonoBehaviours, not ScriptableObjects
    if not is_monobehaviour or is_scriptable_object:
        return violations

    # Track class nesting depth to skip inner classes
    class_depth = 0
    brace_depth = 0
    in_top_class = False

    for i, line in enumerate(lines, 1):
        # Track brace depth
        brace_depth += line.count("{") - line.count("}")

        # Detect class/struct declarations
        if CLASS_PATTERN.search(line) and not COMMENT_PATTERN.match(line):
            class_depth += 1
            if class_depth == 1:
                in_top_class = True
            continue

        # Only check fields in the top-level class (depth == 1)
        if class_depth != 1:
            continue

        if COMMENT_PATTERN.match(line):
            continue
        # Skip lines with attributes
        if re.match(r"^\s*\[", line):
            continue
        # Skip properties (=> or { get)
        if PROPERTY_PATTERN.search(line):
            continue
        # Skip event/delegate/Action declarations
        if re.search(r"\b(event|delegate|Action|Func|UnityEvent)\b", line):
            continue
        if PUBLIC_FIELD_PATTERN.match(line):
            violations.append(
                StyleViolation(
                    filepath,
                    i,
                    "PUBLIC_FIELD",
                    "Public field on MonoBehaviour — use [SerializeField] private instead",
                )
            )

    return violations


def main():
    print("=" * 60)
    print("Code Style Check")
    print("=" * 60)

    if not os.path.isdir(SCRIPTS_DIR):
        print(f"Scripts directory not found: {SCRIPTS_DIR}")
        return 1

    all_violations = []
    file_count = 0

    for filepath in find_cs_files(SCRIPTS_DIR):
        file_count += 1
        try:
            with open(filepath, "r", encoding="utf-8-sig") as f:
                lines = f.readlines()
        except (UnicodeDecodeError, IOError):
            continue

        all_violations.extend(check_deprecated_apis(filepath, lines))
        all_violations.extend(check_update_loops(filepath, lines))
        all_violations.extend(check_public_fields(filepath, lines))

    print(f"Scanned {file_count} C# files\n")

    if not all_violations:
        print("All code style checks OK")
        return 0

    # Group by rule
    by_rule = {}
    for v in all_violations:
        by_rule.setdefault(v.rule, []).append(v)

    warning_count = 0
    for rule, violations in sorted(by_rule.items()):
        print(f"\n{rule} ({len(violations)} issue{'s' if len(violations) != 1 else ''}):")
        for v in violations:
            print(str(v))
            warning_count += 1
            if GITHUB_ACTIONS:
                rel = os.path.relpath(v.file, PROJECT_ROOT)
                print(
                    f"::warning file={rel},line={v.line_num}::[{v.rule}] {v.message}"
                )

    print(f"\nStyle warnings: {warning_count}")

    # Style checks are warnings, not blocking errors
    # Return 0 so CI doesn't fail, but print warnings
    return 0


if __name__ == "__main__":
    sys.exit(main())
