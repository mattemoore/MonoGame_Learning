#!/usr/bin/env python3
"""Convert .achj animation-chain JSON files into MonoGame.Extended texture-atlas JSON.

Maps each .achj chain to a run of atlas frames named <slug>-<chain>-NN (one
MonoGame.Extended animation per chain) and groups frames by source texture. No
image data is read, cropped, or written — the atlas simply references the art by
filename, so the output is drop-in for the texture packer importer once the
referenced texture files are in place next to the JSON.

When every frame uses a single source texture the atlas references it as
<slug>-texture.png (override with --texture-name); multi-texture achjs keep each
source's basename so filenames never collide.

The MonoGame.Extended atlas format cannot express per-frame flipHorizontal,
relativeX/relativeY offsets, or frameLength — those are printed as notes so you
can bake flips manually and configure offsets (SwingAnchors) in code.

Usage:
  python3 achj_to_monogame_extended.py <input.achj>... [--name <slug>]
      [--texture-name <file>] [--out-dir DIR]

Uses only the Python 3 standard library.
"""

import json
import os
import struct
import sys


def png_size(path):
    """Return (w, h) from a PNG header, or None if the file is missing/not PNG."""
    try:
        with open(path, "rb") as f:
            head = f.read(24)
    except OSError:
        return None
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", head[16:24])


def resolve_texture(raw, achj_dir):
    candidates = []
    if os.path.isabs(raw):
        candidates.append(raw)
    else:
        candidates.append(os.path.join(achj_dir, raw))
        candidates.append(raw)
    for c in candidates:
        if os.path.isfile(c):
            return c
    return candidates[0]


def convert(achj_path, slug, out_dir, single_texture_name):
    with open(achj_path, "r", encoding="utf-8") as f:
        doc = json.load(f)
    chains = doc.get("animationChains") or []
    if not chains:
        raise ValueError(f"{achj_path}: no animationChains")

    achj_dir = os.path.dirname(os.path.abspath(achj_path))
    textures = {}   # key: normalized filename -> list of frame dicts
    chain_notes = []  # (title, [frame note lines])
    seen_names = set()
    texture_paths = {}  # filename -> resolved source path

    for chain in chains:
        chain_name = chain["name"].lower().replace(" ", "-")
        chain_frames = chain.get("frames") or []
        if not chain_frames:
            raise ValueError(f"{achj_path}: chain '{chain_name}' has no frames")
        frame_notes = []
        for i, f in enumerate(chain_frames):
            src_path = resolve_texture(f["textureName"], achj_dir)
            tex_name = os.path.basename(src_path)
            key = tex_name
            textures.setdefault(key, [])
            texture_paths.setdefault(key, src_path)

            x = round(f["leftCoordinate"])
            y = round(f["topCoordinate"])
            w = round(f["rightCoordinate"] - f["leftCoordinate"])
            h = round(f["bottomCoordinate"] - f["topCoordinate"])
            if w <= 0 or h <= 0:
                raise ValueError(f"{achj_path}: chain '{chain_name}' frame {i} has non-positive size")

            name = f"{slug}-{chain_name}-{i:02d}"
            if name in seen_names:
                raise ValueError(f"{achj_path}: duplicate frame name '{name}' after chain-name normalization")
            seen_names.add(name)

            textures[key].append({"name": name, "x": x, "y": y, "w": w, "h": h})

            parts = []
            if f.get("flipHorizontal"):
                parts.append("flipH (bake manually)")
            rel_x = f.get("relativeX")
            rel_y = f.get("relativeY")
            if rel_x is not None or rel_y is not None:
                parts.append(f"offset({rel_x or 0},{rel_y or 0})")
            if parts:
                frame_notes.append(f"    {name}: {' '.join(parts)}")
        chain_notes.append((f"animation '{chain_name}': prefix '{slug}-{chain_name}', {len(chain_frames)} frame(s)", frame_notes))

    texture_refs = []
    for tex_name, frames in textures.items():
        if len(textures) == 1:
            tex_name_out = single_texture_name if single_texture_name else f"{slug}-texture.png"
        else:
            tex_name_out = tex_name

        size = png_size(texture_paths[tex_name])
        if size is None:
            text_size = (max(f["x"] + f["w"] for f in frames), max(f["y"] + f["h"] for f in frames))
            size = text_size
            print(f"    note: '{os.path.basename(texture_paths[tex_name])}' unavailable, "
                  f"size inferred from frame bounds ({text_size[0]}x{text_size[1]}) — verify it matches the real texture")
        if len(textures) > 1:
            print(f"    note: frames from '{tex_name}' — keep that filename next to the JSON")

        texture_refs.append({
            "filename": tex_name_out,
            "format": "RGBA8888",
            "size": {"w": size[0], "h": size[1]},
            "frames": {
                f["name"]: {"frame": {"x": f["x"], "y": f["y"], "w": f["w"], "h": f["h"]}}
                for f in frames
            },
        })

    atlas = {
        "textures": texture_refs,
        "meta": {"app": "achj_to_monogame_extended", "dataformat": "monogame-extended", "version": "1.2"},
    }
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, f"{slug}.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(atlas, f, indent=2)

    print(f"wrote {out_path}")
    print("  C# (AnimatedSprites/<Name>Sprite.cs):")
    for title, _ in chain_notes:
        chain_name = title.split("'")[1]
        count = sum(1 for tx in texture_refs for flag in tx["frames"] if flag.startswith(f"{slug}-{chain_name}-"))
        print(f"    new SpriteAnimationDef(\"{chain_name}\", \"{slug}-{chain_name}\", {count}, false)")
    for title, frame_notes in chain_notes:
        print(f"  {title}")
        for line in frame_notes:
            print(line)


def main(argv):
    if len(argv) < 2 or argv[1] in ("-h", "--help"):
        print(__doc__)
        return 2 if len(argv) < 2 else 0

    args = argv[1:]
    name = None
    texture_name = None
    out_dir = None
    inputs = []
    i = 0
    while i < len(args):
        a = args[i]
        if a == "--name":
            name = args[i + 1]
            i += 2
        elif a == "--texture-name":
            texture_name = args[i + 1]
            i += 2
        elif a == "--out-dir":
            out_dir = args[i + 1]
            i += 2
        else:
            inputs.append(a)
            i += 1

    if not inputs:
        print("error: no .achj input files", file=sys.stderr)
        return 2
    if (name or texture_name) and len(inputs) > 1:
        print("error: --name/--texture-name apply to a single input", file=sys.stderr)
        return 2

    for achj in inputs:
        if not os.path.isfile(achj):
            print(f"error: {achj}: no such file", file=sys.stderr)
            return 2
        slug = name if name else os.path.splitext(os.path.basename(achj))[0]
        target = out_dir if out_dir else os.path.dirname(os.path.abspath(achj))
        try:
            convert(achj, slug, target, texture_name)
        except ValueError as e:
            print(f"error: {e}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))