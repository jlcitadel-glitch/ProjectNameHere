#!/usr/bin/env python3
"""Run all CI checks and report aggregate results."""

import os
import subprocess
import sys
import time


CHECKS = [
    ("Meta File Integrity", "check_meta_files.py"),
    ("GUID References", "check_guid_references.py"),
    ("Layer/Tag Consistency", "check_layer_consistency.py"),
    ("Build Scene Validation", "check_scene_build_settings.py"),
]

GITHUB_ACTIONS = os.environ.get("GITHUB_ACTIONS") == "true"


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    python = sys.executable
    results = []

    print("=" * 60)
    print("Unity CI — Asset Validation Suite")
    print("=" * 60)
    print()

    overall_start = time.time()

    for name, script in CHECKS:
        script_path = os.path.join(script_dir, script)

        if not os.path.exists(script_path):
            print(f"SKIP: {name} — {script} not found")
            results.append((name, None))
            continue

        start = time.time()
        try:
            result = subprocess.run(
                [python, script_path],
                capture_output=False,
                timeout=120,
            )
            elapsed = time.time() - start
            results.append((name, result.returncode))
            print(f"  -> {'FAIL' if result.returncode else 'PASS'} ({elapsed:.1f}s)")
        except subprocess.TimeoutExpired:
            elapsed = time.time() - start
            print(f"  -> TIMEOUT ({elapsed:.1f}s)")
            results.append((name, 1))
        except Exception as e:
            print(f"  -> ERROR: {e}")
            results.append((name, 1))

        print()

    overall_elapsed = time.time() - overall_start

    # Summary
    print("=" * 60)
    print("Summary")
    print("=" * 60)

    failed = 0
    for name, code in results:
        if code is None:
            status = "SKIP"
        elif code == 0:
            status = "PASS"
        else:
            status = "FAIL"
            failed += 1
        print(f"  {status:4s}  {name}")

    print()
    print(f"Total: {len(results)} checks, "
          f"{sum(1 for _, c in results if c == 0)} passed, "
          f"{failed} failed, "
          f"{sum(1 for _, c in results if c is None)} skipped "
          f"({overall_elapsed:.1f}s)")

    if GITHUB_ACTIONS and failed:
        print(f"\n::error::CI failed: {failed} check(s) did not pass")

    return 1 if failed > 0 else 0


if __name__ == "__main__":
    sys.exit(main())
