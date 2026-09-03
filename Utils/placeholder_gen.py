#!/usr/bin/env python3
"""Generate placeholder melee-weapon art for the MonoGame learning project.

Creates three assets in the target directory, matching the conventions used by
the existing bat weapon:

  <slug>-texture.png   sprite sheet: N swing frames laid out horizontally
  <slug>.json          TexturePacker atlas (monogame-extended dataformat)
  <slug>-pickup.png    static pickup icon (one frame of the sheet, diagonal)

Usage:
  python3 placeholder_gen.py <slug> <out_dir> [frames=N] [frame_w=W] [frame_h=H]

The texture is drawn as a simple "baton" bar that tilts across swing frames so
the swing motion is readable in-game. Real art can replace these files later
without code changes as long as it keeps the same frame count and naming.

Requires only the Python 3 standard library (zlib/struct) — no PIL dependency.
"""

import json
import math
import os
import struct
import sys
import zlib


def _chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_png(path, w, h, rgba_rows):
    raw = b"".join(b"\x00" + bytes(rgba_rows[y * w * 4:(y + 1) * w * 4]) for y in range(h))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(_chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)))
        f.write(_chunk(b"IDAT", zlib.compress(raw, 9)))
        f.write(_chunk(b"IEND", b""))


def _rot_point(cx, cy, x, y, angle_deg):
    a = math.radians(angle_deg)
    dx, dy = x - cx, y - cy
    return (cx + dx * math.cos(a) - dy * math.sin(a),
            cy + dx * math.sin(a) + dy * math.cos(a))


def draw_baton(w, h, angle_deg, bar_w, bar_h, color):
    px = bytearray(w * h * 4)
    cx, cy = (w - 1) / 2, (h - 1) / 2
    bw, bh = bar_w / 2, bar_h / 2
    for y in range(h):
        for x in range(w):
            rx, ry = _rot_point(cx, cy, x, y, -angle_deg)
            if abs(rx - cx) <= bw and abs(ry - cy) <= bh:
                i = (y * w + x) * 4
                px[i:i + 4] = bytes(color)
    return px


def _shade(color, factor):
    return (min(255, int(c * factor)) for c in color)


def main():
    if len(sys.argv) < 3:
        print("usage: python3 placeholder_gen.py <slug> <out_dir> [frames=N] [frame_w=W] [frame_h=H]")
        sys.exit(2)
    slug = sys.argv[1]
    out_dir = sys.argv[2]
    frames = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    frame_w = int(sys.argv[4]) if len(sys.argv) > 4 else 12
    frame_h = int(sys.argv[5]) if len(sys.argv) > 5 else 40

    os.makedirs(out_dir, exist_ok=True)
    base = (90, 140, 200)
    angles = [-60, -35, -10, 15, 40, 60][:frames]
    while len(angles) < frames:
        angles.append(angles[-1])

    sheet = bytearray(frame_w * frames * frame_h * 4)
    for i in range(frames):
        cell = draw_baton(frame_w, frame_h, angles[i],
                          max(3, frame_w * 0.4), max(10, frame_h * 0.6),
                          _shade(base, 0.55 + 0.15 * i))
        for y in range(frame_h):
            src = y * frame_w * 4
            dst = (y * (frame_w * frames) + i * frame_w) * 4
            sheet[dst:dst + frame_w * 4] = cell[src:src + frame_w * 4]

    write_png(os.path.join(out_dir, f"{slug}-texture.png"), frame_w * frames, frame_h, sheet)
    write_png(os.path.join(out_dir, f"{slug}-pickup.png"), frame_w, frame_h,
              draw_baton(frame_w, frame_h, 45, max(3, frame_w * 0.4), max(10, frame_h * 0.6), base))

    atlas = {
        "textures": [{
            "filename": f"{slug}-texture.png",
            "format": "RGBA8888",
            "size": {"w": frame_w * frames, "h": frame_h},
            "frames": {
                f"{slug}-{i:02d}": {"frame": {"x": i * frame_w, "y": 0, "w": frame_w, "h": frame_h}}
                for i in range(frames)
            },
        }],
        "meta": {"app": "TexturePacker (placeholder)", "dataformat": "monogame-extended", "version": "1.2"},
    }
    with open(os.path.join(out_dir, f"{slug}.json"), "w") as f:
        json.dump(atlas, f, indent=2)

    print(f"wrote {slug}-texture.png ({frame_w * frames}x{frame_h}, {frames} frames)")
    print(f"wrote {slug}.json")
    print(f"wrote {slug}-pickup.png ({frame_w}x{frame_h})")


if __name__ == "__main__":
    main()