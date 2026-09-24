#!/usr/bin/env python3
"""Convert an Aseprite sprite-sheet JSON export into MonoGame.Extended atlas JSON.

Aseprite exports sprite-sheet metadata in two shapes:
  * "JSON Array"  — "frames": [ { "filename": "<name>.png", "frame": {...}, ... }, ... ]
  * "JSON Hash"   — "frames": { "<name>.png": { "frame": {...}, ... }, ... }
Both are converted to the MonoGame.Extended "monogame-extended" atlas shape
used in this repo:

  "textures": [ { "filename": "<slug>-texture.png", "size": {...},
                  "frames": { "<slug>-<animation>-NN": { "frame": {...} }, ... } } ],
  "meta": { "dataformat": "monogame-extended", ... }

Frame names are derived from Aseprite frameTags: for a tag spanning source
frames [from..to], the exported frames in that range get consecutive names
<slug>-<tag>-NN. Frames outside every tag get <slug>-frame-NN (frame index in
the export order). This matches the "<slug>-<chain>-NN" prefix convention that
SpriteAnimationDef/DefineFrames consume in MonoGameLearning.

Texture metadata comes from meta.image (Array/Hash both set it) and
meta.size; per-frame rotation/trim are preserved so the pipeline writer can
emit them (the extended TexturePackerWriter reads frame.Rotated, Size, Offset,
Pivot). No image data is read or written.

A 'handle' slice (name matched case-insensitively) is read for weapon anchor
authoring: each key's pivot (relative to the slice bounds) is combined with the
bounds origin into a frame-local handle point, and printed as a C# paste block
for the weapon def (carry + swing offsets). Keys without a pivot are skipped
with a one-line note. Absent slices leave the atlas output unchanged.

A 'hand' slice (same case-insensitive match) is read for actor hand-anchor
authoring. It is the actor-side counterpart to 'handle': a weapon def stores only
its own grip offset, while the actor owns where its hand is per animation frame.
Each key's point is ``bounds.origin + pivot`` in frame-local pixels, converted to
**center-relative** coordinates by subtracting half the frame size (the region
rect for untrimmed frames, the frame's ``sourceSize`` when trimmed), i.e. the
offset from the sprite region center that SpriteSheetAsset uses as the actor's
Position. Points are grouped by frameTag in region order and printed as a C#
paste block for a HandAnchorTable. Absent slices leave the atlas output unchanged.

Usage:
  python3 aseprite_to_monogame_extended.py <input.json>... [--name <slug>]
      [--texture-name <file>] [--out-dir DIR]

If --name is omitted, the output filename AND the in-atlas texture reference
both use meta.image's basename without extension (e.g. "bat-texture-sheet" for
image "bat-texture-sheet.png"). Pass --name <slug> to write <slug>.json and
reference the texture as <slug>-texture.png.

Uses only the Python 3 standard library.
"""

import json
import os
import re
import sys


def parse_frames(doc):
    """Return the export's frames as a list of region dicts in export order.

    Array: each element carries "filename"; that basename (minus extension) is the
    Aseprite frame name (used for duplicate detection). Hash: the object key is the
    name. Both rely on meta.image for the source texture, so regions are always
    grouped to a single <slug>-texture.png unless --texture-name is given.
    """
    raw = doc.get("frames")
    if isinstance(raw, list):
        items = raw
    elif isinstance(raw, dict):
        items = [dict(frame, filename=name) for name, frame in raw.items()]
    else:
        raise ValueError(f"unsupported 'frames' type: {type(raw).__name__}; "
                         "expected a JSON array (Aseprite 'JSON Array') or "
                         "object (Aseprite 'JSON Hash') export")

    regions = []
    stems = []
    for i, item in enumerate(items):
        name_source = item.get("filename")
        if not name_source:
            raise ValueError(f"frame {i} has no 'filename'")
        stems.append(os.path.splitext(os.path.basename(name_source))[0])
        frame = item.get("frame")
        if not isinstance(frame, dict):
            raise ValueError(f"frame {i} ('{name_source}') has no 'frame' rect")
        try:
            x, y = int(frame["x"]), int(frame["y"])
            w, h = int(frame["w"]), int(frame["h"])
        except (KeyError, TypeError, ValueError):
            raise ValueError(f"frame {i} ('{name_source}') has a non-integer 'frame' rect")
        if w <= 0 or h <= 0:
            raise ValueError(f"frame {i} ('{name_source}') has non-positive size {w}x{h}")
        regions.append({
            "rect": {"x": x, "y": y, "w": w, "h": h},
            "rotated": bool(item.get("rotated", False)),
            "trimmed": bool(item.get("trimmed", False)),
            "spriteSourceSize": item.get("spriteSourceSize"),
            "sourceSize": item.get("sourceSize"),
        })

    if len(set(stems)) != len(stems):
        seen = set()
        dups = sorted({s for s in stems if s in seen or seen.add(s)})
        raise ValueError(f"duplicate frame names from Aseprite export: {', '.join(dups)}")

    return regions


def tag_runs(doc, frame_count):
    """Map source-frame indices to normalized animation names from frameTags."""
    tags = doc.get("meta", {}).get("frameTags") or []
    runs = {}
    for tag in tags:
        name, from_idx, to_idx = (
            tag.get("name", ""),
            int(tag.get("from", 0)),
            int(tag.get("to", 9_999_999)),
        )
        if not name:
            raise ValueError(f"frameTag with no 'name'; add a name in Aseprite or export without tags")
        slug = re.sub(r"[^a-z0-9-]", "", name.strip().lower())
        if not slug:
            raise ValueError(f"frameTag '{name}' has no usable characters after normalization")
        if from_idx < 0 or to_idx >= frame_count or from_idx > to_idx:
            raise ValueError(
                f"frameTag '{name}' spans frames {from_idx}..{to_idx} but the export "
                f"has {frame_count} frame(s)")
        range_idx = range(from_idx, to_idx + 1)
        if any(i in runs for i in range_idx):
            overlap = next(i for i in range_idx if i in runs)
            raise ValueError(f"frameTag '{name}' overlaps another tag at frame {overlap}")
        for i in range_idx:
            runs[i] = {
                "slug": slug,
                "offset": i - from_idx,
            }
    return runs


def compose_frame_keys(regions, tag_runs, slug):
    """Build region names + a per-tag name map for the atlas output."""
    frame_names = {}
    used = set()
    names_by_tag = {}

    for i, r in enumerate(regions):
        tag = tag_runs.get(i)
        if tag:
            base = f"{slug}-{tag['slug']}-{tag['offset']:02d}"
            names_by_tag.setdefault(tag["slug"], []).append(base)
        else:
            base = f"{slug}-frame-{i:02d}"
        if base in used:
            raise ValueError(f"duplicate region name after normalization: '{base}'")
        used.add(base)
        frame_names[i] = base

    return frame_names, names_by_tag


def slice_handle_offsets(doc, frame_names, names_by_tag):
    """Read the 'handle' slice (case-insensitive) and return paste-ready offsets.

    A slice key stores its pivot relative to the slice bounds origin, so the
    frame-local handle point is ``bounds.origin + pivot`` (frames are untrimmed,
    so these are already in the 0-based frame coords the atlas regions use).
    Returns ``(carry_offset, [swing_offset, ...])`` in Hold/Swing region order,
    or ``None`` when no compatible slice is exported.
    """
    slices = doc.get("meta", {}).get("slices") or []
    target = next(
        (s for s in slices if str(s.get("name", "")).strip().lower() == "handle"),
        None)
    if target is None:
        return None

    offsets = {}
    for key in target.get("keys", []):
        try:
            frame = int(key.get("frame", -1))
        except (TypeError, ValueError):
            continue
        pivot = key.get("pivot")
        bounds = key.get("bounds")
        if not isinstance(pivot, dict) or not isinstance(bounds, dict):
            print(f"    note: handle slice key frame {frame} has no pivot; skipped")
            continue
        try:
            offsets[frame] = (
                int(bounds["x"]) + int(pivot["x"]),
                int(bounds["y"]) + int(pivot["y"]))
        except (KeyError, TypeError, ValueError):
            print(f"    note: handle slice key frame {frame} has non-integer bounds/pivot; skipped")

    name_to_idx = {name: i for i, name in frame_names.items()}
    carry_names = names_by_tag.get("hold") or []
    swing_names = names_by_tag.get("swing") or []
    carry_idx = name_to_idx.get(carry_names[0]) if carry_names else None
    swing_idxs = [i for n in swing_names if (i := name_to_idx.get(n)) is not None]

    carry_off = offsets.get(carry_idx) if carry_idx is not None else None
    swing_offs = [offsets.get(i) for i in swing_idxs]
    return carry_off, swing_offs


def emit_handle_offsets(doc, frame_names, names_by_tag):
    """Print the C# paste block for BatWeapon.cs from the 'handle' slice.

    Atlas JSON output is unaffected: offsets ride in C# at use time, not in the
    XNB. Missing/malformed keys degrade to a warning so re-exports without a
    slice still convert cleanly.
    """
    result = slice_handle_offsets(doc, frame_names, names_by_tag)
    if result is None:
        print("    note: no 'handle' slice in export — weapon anchors stay hand-tuned")
        return

    carry_off, swing_offs = result
    if not swing_offs:
        print("    note: handle slice has no swing frames — paste block incomplete")
        return
    if carry_off is None or any(o is None for o in swing_offs):
        missing = [i for i, o in enumerate([carry_off, *swing_offs]) if o is None]
        tag = "carry (Hold)" if missing[0] == 0 else f"swing frame {missing[0] - 1}"
        print(f"    note: handle slice missing pivot for {tag} — paste block incomplete")
        return

    def vec2(off):
        return f"new Vector2({off[0]}, {off[1]})"

    swing_line = ",  ".join(vec2(o) for o in swing_offs)
    print("  C# handle offsets (paste into BatWeapon.cs, then re-tune hands in-game):")
    print(f"    CarryHandleOffset  = {vec2(carry_off)},   // Hold frame")
    print(f"    SwingHandleOffsets = [ {swing_line} ],")


def slice_hand_anchors(doc, regions, frame_names, names_by_tag):
    """Read the 'hand' slice (case-insensitive) and return per-tag hand points.

    The actor-side hand point for a frame is ``bounds.origin + pivot`` in
    frame-local pixels, shifted to **center-relative** coordinates by subtracting
    half the frame size (region rect when untrimmed, ``sourceSize`` when trimmed)
    so it is directly the offset from the actor's Position. Returns
    ``{tag_slug: [(x, y) | None, ...]}`` in region order, or ``None`` when no
    compatible slice is exported. ``None`` entries mark frames with no slice key.
    """
    slices = doc.get("meta", {}).get("slices") or []
    target = next(
        (s for s in slices if str(s.get("name", "")).strip().lower() == "hand"),
        None)
    if target is None:
        return None

    points = {}
    for key in target.get("keys", []):
        try:
            frame = int(key.get("frame", -1))
        except (TypeError, ValueError):
            continue
        pivot = key.get("pivot")
        bounds = key.get("bounds")
        if not isinstance(pivot, dict) or not isinstance(bounds, dict):
            print(f"    note: hand slice key frame {frame} has no pivot; skipped")
            continue
        try:
            points[frame] = (
                int(bounds["x"]) + int(pivot["x"]),
                int(bounds["y"]) + int(pivot["y"]))
        except (KeyError, TypeError, ValueError):
            print(f"    note: hand slice key frame {frame} has non-integer bounds/pivot; skipped")

    name_to_idx = {name: i for i, name in frame_names.items()}
    by_tag = {}
    for slug, names in names_by_tag.items():
        entries = []
        for name in names:
            idx = name_to_idx.get(name)
            if idx is None:
                continue
            point = points.get(idx)
            if point is None:
                entries.append(None)
                continue
            region = regions[idx]
            src_w, src_h = region["rect"]["w"], region["rect"]["h"]
            if region["trimmed"] and region["sourceSize"]:
                src_w, src_h = region["sourceSize"]["w"], region["sourceSize"]["h"]
            entries.append((point[0] - src_w / 2, point[1] - src_h / 2))
        by_tag[slug] = entries
    return by_tag


def emit_hand_anchors(doc, regions, frame_names, names_by_tag):
    """Print the C# paste block for an actor HandAnchorTable from the 'hand' slice.

    Atlas JSON output is unaffected: the anchors ride in C# at use time, not in
    the XNB. Missing/malformed keys degrade to a warning so re-exports without a
    slice (or with sparse keys) still convert cleanly.
    """
    result = slice_hand_anchors(doc, regions, frame_names, names_by_tag)
    if result is None:
        print("    note: no 'hand' slice in export — actor hand anchors not emitted")
        return
    if not result:
        print("    note: hand slice present but no tagged frames — paste block not emitted")
        return

    def vec2(point):
        return f"new Vector2({point[0]:g}, {point[1]:g})"

    print("  C# hand anchors (paste into AnimatedSprites/<Name>Sprite.cs; offsets are from the sprite origin):")
    for slug, entries in result.items():
        if not entries:
            continue
        missing = [i for i, p in enumerate(entries) if p is None]
        if missing:
            print(f"    note: hand slice missing pivot for '{slug}' frame(s) {missing} — {slug} entry skipped")
            continue
        line = ",  ".join(vec2(p) for p in entries)
        print(f"    (\"{slug}\", [ {line} ]),")


def convert(path, slug, texture_name, out_dir):
    with open(path, "r", encoding="utf-8") as f:
        doc = json.load(f)

    image = doc.get("meta", {}).get("image")
    if not image:
        raise ValueError(f"{path}: no 'meta.image' (is this an Aseprite sprite-sheet export?)")

    regions = parse_frames(doc)
    runs = tag_runs(doc, len(regions))
    frame_names, names_by_tag = compose_frame_keys(regions, runs, slug)

    if texture_name:
        tex_name = texture_name
    else:
        tex_name = os.path.splitext(os.path.basename(image))[0] + ".png"
    meta_size = doc.get("meta", {}).get("size")
    if meta_size:
        self_size = (meta_size["w"], meta_size["h"])
    else:
        max_w = max(r["rect"]["w"] for r in regions)
        max_h = max(r["rect"]["h"] for r in regions)
        self_size = (max_w, max_h)
        print(f"    note: atlas size guessed from frame bounds ({self_size[0]}x{self_size[1]}), "
              "verify it matches the real texture")

    frames_out = {}
    for i, r in enumerate(regions):
        frame_out = {"frame": r["rect"]}
        if r["rotated"]:
            frame_out["rotated"] = 1
        if r["trimmed"]:
            sss = r["spriteSourceSize"] or {"x": 0, "y": 0, "w": r["rect"]["w"], "h": r["rect"]["h"]}
            src = r["sourceSize"] or {"w": r["rect"]["w"], "h": r["rect"]["h"]}
            frame_out["size"] = {"w": src["w"], "h": src["h"]}
            frame_out["offset"] = {"x": sss["x"], "y": sss["y"]}
        frames_out[frame_names[i]] = frame_out

    atlas = {
        "textures": [{
            "filename": tex_name,
            "format": "RGBA8888",
            "size": {"w": self_size[0], "h": self_size[1]},
            "frames": frames_out,
        }],
        "meta": {
            "app": "aseprite_to_monogame_extended",
            "dataformat": "monogame-extended",
            "version": "1.0",
        },
    }

    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, f"{slug}.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(atlas, f, indent=2)

    print(f"wrote {out_path}")
    print("  C# (AnimatedSprites/<Name>Sprite.cs):")
    for name in sorted(names_by_tag):
        print(f"    new SpriteAnimationDef(\"{name}\", \"{slug}-{name}\", {len(names_by_tag[name])}, false)")
    if not names_by_tag:
        print(f"    (no frameTags in export — add tags in Aseprite for named animations)")
    print(f"  {len(regions)} frame(s) from '{os.path.basename(image)}' into '{tex_name}'")
    emit_handle_offsets(doc, frame_names, names_by_tag)
    emit_hand_anchors(doc, regions, frame_names, names_by_tag)


def main(argv):
    if len(argv) < 2 or argv[1] in ("-h", "--help"):
        print(__doc__)
        return 2 if len(argv) < 2 else 0

    args = argv[1:]
    name, texture_name, out_dir = None, None, None
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

    if name and len(inputs) > 1:
        print("error: --name applies to a single input", file=sys.stderr)
        return 2

    for inp in inputs:
        if not os.path.isfile(inp):
            print(f"error: {inp}: no such file", file=sys.stderr)
            return 2
        slug = name if name else os.path.splitext(os.path.basename(inp))[0]
        target = out_dir if out_dir else os.path.dirname(os.path.abspath(inp))
        try:
            convert(inp, slug, texture_name, target)
        except ValueError as e:
            print(f"error: {e}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))