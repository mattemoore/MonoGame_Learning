#!/usr/bin/env python3
"""Build the Adventurer actor sheet (`adventurer.aseprite`) from the named source frames.

Deterministic replacement for hand-assembling the sheet in the GUI: imports exactly the
37 frames the game uses, in canonical animation order, with the 9 tags and their ranges,
then exports the Aseprite JSON Array + texture the converter consumes.

Why: GUI import repeatedly scrambled the frames (duplicate-merged `die-04`/`die-06`,
`die-01`/`die-04` missing, attack runs out of order). This script pins frame order, counts,
tag names, and ranges.

Frames imported (the first `count` PNGs of each prefix):
    idle-00..03 (4), attack1-00..03 (4), attack2-00..03 (4), attack3-00..03 (4),
    run-00..05 (6), hurt-00..02 (3), die-00..06 (7), fall-00..01 (2), stand-00..02 (3)
Tag for the last group is `getup` (its source prefix is `adventurer-stand`).

Usage:
    python3 Utils/build_adventurer_sheet.py

Overwrites `adventurer.aseprite`, `adventurer.json`, and `adventurer-texture.png` in
`MonoGameLearning.Game/Sources/Adventurer/`. Re-running overwrites any `hand` slice
authored in the GUI, so only rebuild the .aseprite when the frame set/order changes;
preserve GUI slice work by exporting (not rebuilding) after authoring pivots.
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import tempfile

# (tag key, source PNG prefix, frame count)
SPEC = [
    ("idle", "adventurer-idle", 4),
    ("attack1", "adventurer-attack1", 4),
    ("attack2", "adventurer-attack2", 4),
    ("attack3", "adventurer-attack3", 4),
    ("run", "adventurer-run", 6),
    ("hurt", "adventurer-hurt", 3),
    ("die", "adventurer-die", 7),
    ("fall", "adventurer-fall", 2),
    ("getup", "adventurer-stand", 3),
]

LUA = """local DIR = {dir}
local OUT = {out}
app.command.NewFile{{ ui=false, width=50, height=37, colorMode="rgb" }}
local s = app.activeSprite
local layer = s.layers[1]
local files = {{{files}}}
for i = 1, #files do
  if i > 1 then s:newFrame() end
  local src = app.open(DIR .. files[i])
  s:newCel(layer, s.frames[i], src.cels[1].image, Point(0, 0))
  src:close()
end
-- Aseprite's newTag is 1-based; the exported JSON/tags use 0-based frame numbers.
local tags = {{{tags}}}
for _, t in ipairs(tags) do
  local tag = s:newTag(t[2], t[3])
  tag.name = t[1]
end
s:saveAs(OUT)
print(string.format("built %dx%d frames=%d tags=%d -> %s", s.width, s.height, #s.frames, #s.tags, OUT))
"""


def build_plan() -> tuple[list[str], list[tuple[str, int, int]]]:
    files: list[str] = []
    tags: list[tuple[str, int, int]] = []
    index = 0
    for key, prefix, count in SPEC:
        tags.append((key, index, index + count - 1))
        index += count
        files.extend(f"{prefix}-{i:02d}.png" for i in range(count))
    return files, tags


def lua_str(value: str) -> str:
    """Quote a Python string as a Lua double-quoted string literal."""
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def run(cmd: list[str]) -> None:
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.stdout.strip():
        print(result.stdout.strip())
    if result.returncode != 0:
        sys.exit(f"command failed ({result.returncode}): {' '.join(cmd)}\n{result.stderr.strip()}")


def main() -> int:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    default_dir = os.path.join(root, "MonoGameLearning.Game", "Sources", "Adventurer")
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", default=default_dir, help="directory holding the source PNGs and output")
    args = ap.parse_args()

    if not shutil.which("aseprite"):
        sys.exit("aseprite CLI not found on PATH")

    files, tags = build_plan()
    missing = [f for f in files if not os.path.exists(os.path.join(args.dir, f))]
    if missing:
        sys.exit(f"missing source frames: {missing}")

    aseprite_path = os.path.join(args.dir, "adventurer.aseprite")
    json_path = os.path.join(args.dir, "adventurer.json")
    texture_path = os.path.join(args.dir, "adventurer-texture.png")

    lua = LUA.format(
        dir=lua_str(args.dir + os.sep),
        out=lua_str(aseprite_path),
        files=", ".join(lua_str(f) for f in files),
        tags=", ".join(f'{{"{k}", {a + 1}, {b + 1}}}' for k, a, b in tags),
    )
    with tempfile.NamedTemporaryFile("w", suffix=".lua", delete=False) as handle:
        handle.write(lua)
        lua_path = handle.name

    try:
        print(f"building {len(files)} frames / {len(tags)} tags")
        run(["aseprite", "-b", "--script", lua_path])
        # Export the sheet the converter reads. Trim stays off (fixed 50x37 cells), and
        # --list-tags/--list-slices are what put tags and the hand slice into the JSON.
        run([
            "aseprite", "-b", aseprite_path,
            "--sheet", texture_path,
            "--data", json_path,
            "--format", "json",
            "--list-tags", "--list-slices",
        ])
    finally:
        os.unlink(lua_path)

    print(f"wrote {aseprite_path}")
    print(f"wrote {json_path}")
    print(f"wrote {texture_path}")
    print("next: open adventurer.aseprite in the GUI, add the `hand` slice pivots, then re-export")
    return 0


if __name__ == "__main__":
    sys.exit(main())
