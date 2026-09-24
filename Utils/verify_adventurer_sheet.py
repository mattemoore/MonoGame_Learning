#!/usr/bin/env python3
"""Verify an exported Aseprite actor sheet against the frames the game expects.

Checks the Aseprite JSON Array export (plus its texture) for the Adventurer actor:
frame count/size, the 9 required animation tags with the right frame counts, that each
tag's frames pixel-match the intended source PNGs in order, and the presence of the
`hand` slice with one pivot per frame. Read-only: writes nothing.

Usage:
    python3 Utils/verify_adventurer_sheet.py
    python3 Utils/verify_adventurer_sheet.py --json <path> --texture <path> --sources-dir <dir>
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import struct
import sys
import zlib

# The expected frame/tag spec is owned by the builder so the two tools cannot disagree.
# The source frames are the first `count` frames of each prefix, in order; extra source
# frames are intentionally unused.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_adventurer_sheet import SPEC as EXPECTED  # noqa: E402

FRAME_W, FRAME_H = 50, 37
EXPECTED_TOTAL = sum(c for _, _, c in EXPECTED)


def decode_png(path: str) -> tuple[int, int, bytes]:
    """Minimal 8-bit PNG decoder returning (w, h, rgba bytes). No external deps."""
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", f"not a PNG: {path}"
    pos, idat, palette = 8, b"", None
    width = height = colortype = None
    while pos < len(data):
        length = struct.unpack_from(">I", data, pos)[0]
        ctype = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if ctype == b"IHDR":
            width, height, depth, colortype, _, _, interlace = struct.unpack(">IIBBBBB", chunk)
            assert depth == 8 and interlace == 0, f"unsupported PNG {path} depth={depth} interlace={interlace}"
        elif ctype == b"PLTE":
            palette = chunk
        elif ctype == b"IDAT":
            idat += chunk
        elif ctype == b"IEND":
            break
    raw = zlib.decompress(idat)
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[colortype]
    stride = width * channels
    out = bytearray(height * stride)
    prev = bytes(stride)
    p = 0
    for y in range(height):
        filt = raw[p]
        p += 1
        line = bytearray(raw[p:p + stride])
        p += stride
        if filt == 1:
            for i in range(channels, stride):
                line[i] = (line[i] + line[i - channels]) & 255
        elif filt == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif filt == 3:
            for i in range(stride):
                a = line[i - channels] if i >= channels else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif filt == 4:
            for i in range(stride):
                a = line[i - channels] if i >= channels else 0
                b = prev[i]
                c = prev[i - channels] if i >= channels else 0
                est = a + b - c
                pa, pb, pc = abs(est - a), abs(est - b), abs(est - c)
                pred = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pred) & 255
        out[y * stride:(y + 1) * stride] = line
        prev = bytes(line)
    rgba = bytearray(width * height * 4)
    for i in range(width * height):
        if colortype == 6:
            rgba[i * 4:i * 4 + 4] = out[i * 4:i * 4 + 4]
        elif colortype == 2:
            rgba[i * 4:i * 4 + 3] = out[i * 3:i * 3 + 3]
            rgba[i * 4 + 3] = 255
        elif colortype == 3:
            idx = out[i]
            rgba[i * 4:i * 4 + 3] = palette[idx * 3:idx * 3 + 3]
            rgba[i * 4 + 3] = 255
        elif colortype == 4:
            g = out[i * 2]
            rgba[i * 4:i * 4 + 3] = bytes([g, g, g])
            rgba[i * 4 + 3] = out[i * 2 + 1]
        else:
            g = out[i]
            rgba[i * 4:i * 4 + 4] = bytes([g, g, g, 255])
    return width, height, bytes(rgba)


def frame_bytes(texture: bytes, tex_w: int, rect: dict) -> bytes:
    x, y, w, h = rect["x"], rect["y"], rect["w"], rect["h"]
    buf = bytearray()
    for row in range(h):
        start = ((y + row) * tex_w + x) * 4
        buf += texture[start:start + w * 4]
    return bytes(buf)


def main() -> int:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    adv = os.path.join(root, "MonoGameLearning.Game", "Sources", "Adventurer")
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", default=os.path.join(adv, "adventurer.json"))
    ap.add_argument("--texture", default=os.path.join(adv, "adventurer-texture.png"))
    ap.add_argument("--sources-dir", default=adv)
    args = ap.parse_args()

    failures: list[str] = []
    if not os.path.exists(args.json):
        print(f"FAIL: missing {args.json}")
        return 1
    doc = json.load(open(args.json))
    meta = doc.get("meta", {})
    frames = doc.get("frames")
    frames = frames if isinstance(frames, list) else list(frames.values())
    size = meta.get("size", {})
    print(f"sheet: {len(frames)} frames, size {size.get('w')}x{size.get('h')}, image {meta.get('image')}")

    if len(frames) != EXPECTED_TOTAL:
        failures.append(f"frame count {len(frames)} != expected {EXPECTED_TOTAL}")
    # Trim off: every exported frame must be the full untrimmed cell.
    bad_cells = [
        i for i, f in enumerate(frames)
        if (f.get("frame", {}).get("w"), f.get("frame", {}).get("h")) != (FRAME_W, FRAME_H)
    ]
    if bad_cells:
        failures.append(f"frames not untrimmed {FRAME_W}x{FRAME_H}: {bad_cells}")

    # Decode the exported texture once, and every needed source frame.
    tex_w, tex_h, tex = decode_png(args.texture)
    if tex_w != FRAME_W * len(frames) and tex_w != size.get("w"):
        print(f"note: texture is {tex_w}x{tex_h}")
    source_hash: dict[str, str] = {}
    source_px: dict[str, str] = {}
    for _, prefix, count in EXPECTED:
        for i in range(count):
            path = os.path.join(args.sources_dir, f"{prefix}-{i:02d}.png")
            if not os.path.exists(path):
                failures.append(f"missing source frame {os.path.basename(path)}")
                continue
            source_hash[f"{prefix}-{i:02d}"] = hashlib.sha1(decode_png(path)[2]).hexdigest()

    tags = {t.get("name"): t for t in meta.get("frameTags", [])}
    print(f"tags: {sorted(tags)}")
    missing = [k for k, _, _ in EXPECTED if k not in tags]
    extra = [k for k in tags if k not in {e[0] for e in EXPECTED}]
    if missing:
        failures.append(f"missing tags: {missing}")
    if extra:
        failures.append(f"unexpected tags: {extra}")

    seen: set[int] = set()
    for key, prefix, count in EXPECTED:
        tag = tags.get(key)
        if tag is None:
            print(f"  [{key}] MISSING")
            continue
        start, end = tag.get("from"), tag.get("to")
        got = end - start + 1 if start is not None else 0
        problems = []
        if got != count:
            problems.append(f"{got} frames != {count}")
        match_ok = True
        for i in range(min(got, count)):
            idx = start + i
            if idx >= len(frames) or idx < 0:
                match_ok = False
                problems.append(f"range {idx} out of bounds")
                break
            seen.add(idx)
            if not os.path.exists(args.texture) or not source_hash:
                continue
            rect = frames[idx].get("frame", {})
            if (rect.get("w"), rect.get("h")) == (FRAME_W, FRAME_H):
                got_hash = hashlib.sha1(frame_bytes(tex, tex_w, rect)).hexdigest()
                if got_hash != source_hash.get(f"{prefix}-{i:02d}"):
                    match_ok = False
                    problems.append(f"frame {idx} art != {prefix}-{i:02d}")
        status = "OK" if not problems else "BAD"
        print(f"  [{key}] {start}-{end} ({got}/{count}) {status}" + (f" :: {'; '.join(problems)}" if problems else ""))
        if problems:
            failures.append(f"tag '{key}': " + "; ".join(problems))

    untagged = [i for i in range(len(frames)) if i not in seen]
    if untagged:
        failures.append(f"untagged/stray frames: {untagged}")

    # Slices: the hand slice must exist with a pivot on every frame once authoring is done.
    slices = {s.get("name"): s for s in meta.get("slices", [])}
    print(f"slices: {sorted(slices)}")
    if "hand" not in slices:
        failures.append("no 'hand' slice (author it once frames/tags are correct)")
    else:
        keys = slices["hand"].get("keys", [])
        covered = {k.get("frame") for k in keys}
        absent = [i for i in range(len(frames)) if i not in covered]
        print(f"  [hand] {len(keys)} keys, {len(absent)} frames without a pivot")
        for k in keys:
            b, pv = k.get("bounds", {}), k.get("pivot", {})
            cx = b.get("x", 0) + pv.get("x", 0) - FRAME_W / 2
            cy = b.get("y", 0) + pv.get("y", 0) - FRAME_H / 2
            print(f"    frame {k.get('frame'):>2}: pivot ({pv.get('x')},{pv.get('y')}) -> center-relative ({cx:g},{cy:g})")
        if absent:
            failures.append(f"'hand' slice missing pivots for frames {absent}")

    print()
    if failures:
        print("RESULT: FAIL")
        for f in failures:
            print(f" - {f}")
        return 1
    print("RESULT: PASS — frames, tags, and hand pivots all match the expected sheet")
    return 0


if __name__ == "__main__":
    sys.exit(main())
