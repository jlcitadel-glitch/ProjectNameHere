#!/usr/bin/env python3
"""
Batch SVG -> white-on-transparent PNG converter for game-icons.net icons.

Usage:
    python ci/convert_svg_icons.py <svg_repo_root> [--output <output_dir>] [--size 128]

The game-icons.net repo has black-on-transparent SVGs organized as:
    <root>/<author>/<icon-name>.svg

Output: white-on-transparent PNGs at the specified size under:
    <output_dir>/<author>/<icon-name>.png

White-on-transparent is essential because Unity Image.color multiplies the
sprite color, so white icons tint correctly to any color.

Requirements:
    pip install cairosvg Pillow
"""

import argparse
import os
import sys
from pathlib import Path


def convert_svg_to_white_png(svg_path: Path, output_path: Path, size: int) -> bool:
    """Convert a black-on-transparent SVG to a white-on-transparent PNG."""
    try:
        import cairosvg
        from PIL import Image, ImageOps

        # Render SVG to PNG bytes at target size
        png_bytes = cairosvg.svg2png(
            url=str(svg_path),
            output_width=size,
            output_height=size,
        )

        # Load with Pillow, invert colors while preserving alpha
        from io import BytesIO
        img = Image.open(BytesIO(png_bytes)).convert("RGBA")

        # Split channels
        r, g, b, a = img.split()

        # Invert RGB (black -> white) but keep alpha as-is
        r = ImageOps.invert(r)
        g = ImageOps.invert(g)
        b = ImageOps.invert(b)

        img = Image.merge("RGBA", (r, g, b, a))

        # Save
        output_path.parent.mkdir(parents=True, exist_ok=True)
        img.save(str(output_path), "PNG")
        return True

    except Exception as e:
        print(f"  ERROR: {svg_path.name}: {e}", file=sys.stderr)
        return False


def main():
    parser = argparse.ArgumentParser(
        description="Convert game-icons.net SVGs to white-on-transparent PNGs"
    )
    parser.add_argument(
        "svg_root",
        type=Path,
        help="Root directory of SVG icons (e.g. game-icons repo clone)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output directory (default: Assets/_Project/Art/UI/Icons/Skills/)",
    )
    parser.add_argument(
        "--size",
        type=int,
        default=128,
        help="Output PNG size in pixels (default: 128)",
    )
    args = parser.parse_args()

    if args.output is None:
        # Default to project icons folder
        script_dir = Path(__file__).resolve().parent
        project_root = script_dir.parent
        args.output = project_root / "Assets" / "_Project" / "Art" / "UI" / "Icons" / "Skills"

    svg_root = args.svg_root.resolve()
    output_root = args.output.resolve()

    if not svg_root.is_dir():
        print(f"ERROR: SVG root not found: {svg_root}", file=sys.stderr)
        sys.exit(1)

    # Find all SVGs organized as <author>/<name>.svg
    svg_files = []
    for author_dir in sorted(svg_root.iterdir()):
        if not author_dir.is_dir():
            continue
        # Skip hidden dirs
        if author_dir.name.startswith("."):
            continue
        for svg_file in sorted(author_dir.glob("*.svg")):
            svg_files.append((author_dir.name, svg_file))

    if not svg_files:
        # Also try flat structure (all SVGs in root with author in filename)
        for svg_file in sorted(svg_root.glob("*.svg")):
            svg_files.append(("unknown", svg_file))

    if not svg_files:
        print(f"No SVG files found under: {svg_root}", file=sys.stderr)
        sys.exit(1)

    print(f"Found {len(svg_files)} SVGs in {svg_root}")
    print(f"Output: {output_root} @ {args.size}x{args.size}")

    converted = 0
    failed = 0

    for author, svg_path in svg_files:
        icon_name = svg_path.stem
        output_path = output_root / author / f"{icon_name}.png"

        # Skip if output already exists and is newer than source
        if output_path.exists() and output_path.stat().st_mtime > svg_path.stat().st_mtime:
            converted += 1
            continue

        if convert_svg_to_white_png(svg_path, output_path, args.size):
            converted += 1
        else:
            failed += 1

    print(f"\nDone: {converted} converted, {failed} failed")
    if failed > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
