#!/usr/bin/env python3
"""
Import game-icons.net icons as white-on-transparent PNGs for Unity.

Supports two input modes:

  1. ZIP archive from game-icons.net (recommended):
     python ci/convert_svg_icons.py --zip game-icons.net.png.zip [--size 128]

     Download from: https://game-icons.net/archives/ffffff/transparent/game-icons.net.png.zip
     Already white-on-transparent — just resizes to target size.

  2. SVG repo clone (requires Cairo native library):
     python ci/convert_svg_icons.py --svg <repo_root> [--size 128]

     Clone from: https://github.com/game-icons/icons
     Renders white-on-black SVGs to white-on-transparent PNGs.

Output: <project>/Assets/_Project/Art/UI/Icons/Skills/<author>/<name>.png

White-on-transparent is essential because Unity Image.color multiplies the
sprite color, so white icons tint correctly to any color.

Requirements:
    pip install Pillow
    pip install cairosvg  (only for SVG mode)
"""

import argparse
import sys
import zipfile
from pathlib import Path


def resize_png(input_path, output_path: Path, size: int) -> bool:
    """Resize a PNG to the target size, ensuring RGBA mode."""
    try:
        from PIL import Image

        img = Image.open(input_path).convert("RGBA")

        if img.size != (size, size):
            img = img.resize((size, size), Image.LANCZOS)

        output_path.parent.mkdir(parents=True, exist_ok=True)
        img.save(str(output_path), "PNG")
        return True

    except Exception as e:
        name = getattr(input_path, 'name', str(input_path))
        print(f"  ERROR: {name}: {e}", file=sys.stderr)
        return False


def import_from_zip(zip_path: Path, output_root: Path, size: int):
    """Extract and resize PNGs from the game-icons.net zip archive.

    Zip structure: icons/ffffff/transparent/1x1/<author>/<name>.png
    """
    if not zip_path.is_file():
        print(f"ERROR: ZIP file not found: {zip_path}", file=sys.stderr)
        sys.exit(1)

    from io import BytesIO

    converted = 0
    skipped = 0
    failed = 0

    with zipfile.ZipFile(str(zip_path), 'r') as zf:
        png_entries = [n for n in zf.namelist() if n.endswith('.png')]
        print(f"Found {len(png_entries)} PNGs in {zip_path.name}")
        print(f"Output: {output_root} @ {size}x{size}")

        for entry in png_entries:
            # Parse: icons/ffffff/transparent/1x1/<author>/<name>.png
            parts = entry.replace("\\", "/").split("/")
            if len(parts) < 2:
                continue

            author = parts[-2]
            name = Path(parts[-1]).stem
            output_path = output_root / author / f"{name}.png"

            # Skip if output already exists
            if output_path.exists():
                skipped += 1
                continue

            try:
                png_data = BytesIO(zf.read(entry))
                if resize_png(png_data, output_path, size):
                    converted += 1
                else:
                    failed += 1
            except Exception as e:
                print(f"  ERROR: {entry}: {e}", file=sys.stderr)
                failed += 1

    print(f"\nDone: {converted} imported, {skipped} skipped (exist), {failed} failed")
    if failed > 0:
        sys.exit(1)


def convert_svg_to_white_png(svg_path: Path, output_path: Path, size: int) -> bool:
    """Convert a white-on-black SVG to a white-on-transparent PNG.

    game-icons.net SVGs are white foreground (#fff) on a black rectangle.
    We use the brightness (grayscale) as the alpha channel and set RGB to
    white everywhere, producing white-on-transparent output.
    """
    try:
        import cairosvg
        from PIL import Image
        from io import BytesIO

        png_bytes = cairosvg.svg2png(
            url=str(svg_path),
            output_width=size,
            output_height=size,
        )

        img = Image.open(BytesIO(png_bytes)).convert("RGBA")

        # Convert to grayscale to get brightness (white=255, black=0)
        grayscale = img.convert("L")

        # Create new image: RGB = white everywhere, A = brightness
        white = Image.new("L", img.size, 255)
        img = Image.merge("RGBA", (white, white, white, grayscale))

        output_path.parent.mkdir(parents=True, exist_ok=True)
        img.save(str(output_path), "PNG")
        return True

    except Exception as e:
        print(f"  ERROR: {svg_path.name}: {e}", file=sys.stderr)
        return False


def import_from_svg(svg_root: Path, output_root: Path, size: int):
    """Scan SVG repo and convert all icons."""
    if not svg_root.is_dir():
        print(f"ERROR: SVG root not found: {svg_root}", file=sys.stderr)
        sys.exit(1)

    svg_files = []
    for author_dir in sorted(svg_root.iterdir()):
        if not author_dir.is_dir() or author_dir.name.startswith("."):
            continue
        for svg_file in sorted(author_dir.glob("*.svg")):
            svg_files.append((author_dir.name, svg_file))

    if not svg_files:
        for svg_file in sorted(svg_root.glob("*.svg")):
            svg_files.append(("unknown", svg_file))

    if not svg_files:
        print(f"No SVG files found under: {svg_root}", file=sys.stderr)
        sys.exit(1)

    print(f"Found {len(svg_files)} SVGs in {svg_root}")
    print(f"Output: {output_root} @ {size}x{size}")

    converted = 0
    failed = 0

    for author, svg_path in svg_files:
        icon_name = svg_path.stem
        output_path = output_root / author / f"{icon_name}.png"

        if output_path.exists() and output_path.stat().st_mtime > svg_path.stat().st_mtime:
            converted += 1
            continue

        if convert_svg_to_white_png(svg_path, output_path, size):
            converted += 1
        else:
            failed += 1

    print(f"\nDone: {converted} converted, {failed} failed")
    if failed > 0:
        sys.exit(1)


def main():
    parser = argparse.ArgumentParser(
        description="Import game-icons.net icons as white-on-transparent PNGs"
    )
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument(
        "--zip",
        type=Path,
        help="Path to game-icons.net PNG zip archive (recommended)",
    )
    group.add_argument(
        "--svg",
        type=Path,
        help="Path to game-icons SVG repo clone (requires Cairo)",
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
        script_dir = Path(__file__).resolve().parent
        project_root = script_dir.parent
        args.output = project_root / "Assets" / "_Project" / "Art" / "UI" / "Icons" / "Skills"

    output_root = args.output.resolve()

    if args.zip:
        import_from_zip(args.zip.resolve(), output_root, args.size)
    else:
        import_from_svg(args.svg.resolve(), output_root, args.size)


if __name__ == "__main__":
    main()
