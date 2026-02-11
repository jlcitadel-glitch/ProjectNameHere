#!/usr/bin/env python3
"""
Build the Unity project from the command line.

Usage:
    python ci/build.py                    # Development build
    python ci/build.py --release          # Release build
    python ci/build.py --output Builds/X  # Custom output path

Requires Unity 6000.3.4f1 installed via Unity Hub.
"""

import argparse
import os
import subprocess
import sys
import time

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)

# Unity Hub standard install paths (Windows)
UNITY_PATHS = [
    os.path.join("C:", os.sep, "Program Files", "Unity", "Hub", "Editor",
                 "6000.3.4f1", "Editor", "Unity.exe"),
]


def find_unity():
    """Find the Unity Editor executable."""
    for path in UNITY_PATHS:
        if os.path.isfile(path):
            return path

    # Try environment variable
    env_path = os.environ.get("UNITY_EDITOR_PATH")
    if env_path and os.path.isfile(env_path):
        return env_path

    return None


def main():
    parser = argparse.ArgumentParser(description="Build ProjectNameHere")
    parser.add_argument("--release", action="store_true",
                        help="Release build (default: development)")
    parser.add_argument("--output", default=None,
                        help="Output path (default: Builds/ProjectNameHere.exe)")
    parser.add_argument("--unity", default=None,
                        help="Path to Unity Editor executable")
    args = parser.parse_args()

    unity_path = args.unity or find_unity()
    if not unity_path:
        print("ERROR: Unity Editor not found.")
        print("Install Unity 6000.3.4f1 via Unity Hub or set UNITY_EDITOR_PATH.")
        return 1

    print(f"Unity: {unity_path}")
    print(f"Project: {PROJECT_ROOT}")

    # Build output
    output_path = args.output or os.path.join(PROJECT_ROOT, "Builds",
                                               "ProjectNameHere.exe")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    build_type = "Release" if args.release else "Development"
    print(f"Build type: {build_type}")
    print(f"Output: {output_path}")

    # Log file
    log_path = os.path.join(PROJECT_ROOT, "Builds", "build.log")
    os.makedirs(os.path.dirname(log_path), exist_ok=True)

    cmd = [
        unity_path,
        "-batchmode",
        "-nographics",
        "-projectPath", PROJECT_ROOT,
        "-executeMethod", "ProjectName.Editor.BuildScript.BatchBuild",
        "-buildPath", output_path,
        "-logFile", log_path,
        "-quit",
    ]

    if not args.release:
        cmd.append("-development")

    print(f"\nStarting build...")
    start = time.time()

    try:
        result = subprocess.run(cmd, timeout=600)
        elapsed = time.time() - start

        if result.returncode == 0:
            if os.path.isfile(output_path):
                size_mb = os.path.getsize(output_path) / (1024 * 1024)
                print(f"\nBuild succeeded: {size_mb:.1f} MB ({elapsed:.0f}s)")
            else:
                print(f"\nBuild process completed ({elapsed:.0f}s) but output "
                      f"not found at {output_path}")
                print(f"Check log: {log_path}")
                return 1
        else:
            print(f"\nBuild FAILED (exit code {result.returncode}, {elapsed:.0f}s)")
            print(f"Check log: {log_path}")
            return 1

    except subprocess.TimeoutExpired:
        print("\nBuild TIMEOUT (10 min)")
        return 1
    except FileNotFoundError:
        print(f"\nERROR: Unity executable not found at {unity_path}")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
