#!/usr/bin/env python3
"""Smoke tests for aseprite_to_monogame_extended.py weapon-anchor handling.

Run with:  python3 -m unittest Utils.test_aseprite_to_monogame_extended
(no file I/O beyond a tempdir; uses only the standard library).
"""

import contextlib
import io
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(__file__))
import aseprite_to_monogame_extended as conv  # noqa: E402


def synthetic_doc(keys):
    """A 5-frame, Hold+Swing bat export with a 'handle' slice."""
    frames = {}
    for i in range(5):
        frames[f"bat {i}.aseprite"] = {
            "frame": {"x": i * 64, "y": 0, "w": 64, "h": 64},
            "rotated": False,
            "trimmed": False,
            "spriteSourceSize": {"x": 0, "y": 0, "w": 64, "h": 64},
            "sourceSize": {"w": 64, "h": 64},
            "duration": 100,
        }
    return {
        "frames": frames,
        "meta": {
            "image": "bat-texture.png",
            "size": {"w": 320, "h": 64},
            "frameTags": [
                {"name": "Hold", "from": 0, "to": 0, "color": "#000000ff"},
                {"name": "Swing", "from": 1, "to": 4, "color": "#000000ff"},
            ],
            "slices": [{"name": "handle", "color": "#0000ffff", "keys": keys}],
        },
    }


class SliceOffsetsTest(unittest.TestCase):
    def test_offsets_equal_bounds_origin_plus_pivot(self):
        doc = synthetic_doc([{
            "frame": f,
            "bounds": {"x": bx, "y": by, "w": 10, "h": 11},
            "pivot": {"x": px, "y": py},
        } for f, (bx, by, px, py) in enumerate([
            (25, 49, 4, 5), (42, 11, 4, 5), (25, 5, 4, 5),
            (13, 11, 4, 5), (4, 25, 4, 5),
        ])])

        frame_names = {0: "bat-hold-00", 1: "bat-swing-00", 2: "bat-swing-01",
                       3: "bat-swing-02", 4: "bat-swing-03"}
        names_by_tag = {
            "hold": ["bat-hold-00"],
            "swing": ["bat-swing-00", "bat-swing-01", "bat-swing-02", "bat-swing-03"],
        }

        carry, swings = conv.slice_handle_offsets(doc, frame_names, names_by_tag)
        self.assertEqual(carry, (29, 54))
        self.assertEqual(swings, [(46, 16), (29, 10), (17, 16), (8, 30)])

    def test_missing_slice_returns_none(self):
        doc = synthetic_doc([])
        doc["meta"]["slices"] = [{"name": "other", "keys": []}]
        self.assertIsNone(conv.slice_handle_offsets(
            doc, {0: "bat-hold-00"}, {"hold": ["bat-hold-00"], "swing": []}))

    def test_key_without_pivot_is_skipped(self):
        doc = synthetic_doc([
            {"frame": 0, "bounds": {"x": 25, "y": 49, "w": 10, "h": 11}},
            {"frame": 1, "bounds": {"x": 42, "y": 11, "w": 10, "h": 11}, "pivot": {"x": 4, "y": 5}},
        ])
        frame_names = {0: "bat-hold-00", 1: "bat-swing-00"}
        names_by_tag = {"hold": ["bat-hold-00"], "swing": ["bat-swing-00"]}

        with contextlib.redirect_stdout(io.StringIO()):
            carry, swings = conv.slice_handle_offsets(doc, frame_names, names_by_tag)
        self.assertIsNone(carry)
        self.assertEqual(swings, [(46, 16)])

    def test_paste_block_prints_from_synthetic_json(self):
        doc = synthetic_doc([{
            "frame": f,
            "bounds": {"x": bx, "y": by, "w": 10, "h": 11},
            "pivot": {"x": px, "y": py},
        } for f, (bx, by, px, py) in enumerate([
            (25, 49, 4, 5), (42, 11, 4, 5), (25, 5, 4, 5),
            (13, 11, 4, 5), (4, 25, 4, 5),
        ])])

        with tempfile.TemporaryDirectory() as tmp:
            src = os.path.join(tmp, "bat.json")
            with open(src, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            out = os.path.join(tmp, "out")

            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = conv.main(["conv", src, "--name", "bat", "--out-dir", out])

            self.assertEqual(rc, 0)
            output = buf.getvalue()
            self.assertIn("CarryHandleOffset  = new Vector2(29, 54)", output)
            self.assertIn("new Vector2(46, 16)", output)
            self.assertIn("new Vector2(29, 10)", output)
            self.assertIn("new Vector2(17, 16)", output)
            self.assertIn("new Vector2(8, 30)", output)

    def test_absent_slice_warns_not_fails(self):
        doc = synthetic_doc([])
        doc["meta"]["slices"] = []
        with tempfile.TemporaryDirectory() as tmp:
            src = os.path.join(tmp, "bat.json")
            with open(src, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            out = os.path.join(tmp, "out")
            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = conv.main(["conv", src, "--name", "bat", "--out-dir", out])
            self.assertEqual(rc, 0)
            self.assertIn("no 'handle' slice", buf.getvalue())

    def test_hold_only_export_warns_never_prints_empty_swing_array(self):
        doc = synthetic_doc([{
            "frame": 0,
            "bounds": {"x": 25, "y": 49, "w": 10, "h": 11},
            "pivot": {"x": 4, "y": 5},
        }])
        doc["meta"]["frameTags"] = [{"name": "Hold", "from": 0, "to": 0, "color": "#000000ff"}]

        with tempfile.TemporaryDirectory() as tmp:
            src = os.path.join(tmp, "bat.json")
            with open(src, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            out = os.path.join(tmp, "out")
            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = conv.main(["conv", src, "--name", "bat", "--out-dir", out])
            self.assertEqual(rc, 0)
            output = buf.getvalue()
            self.assertIn("no swing frames", output)
            self.assertNotIn("SwingHandleOffsets = [", output)

    def test_tag_name_is_sanitized_to_safe_charset(self):
        doc = synthetic_doc([])
        doc["meta"]["frameTags"] = [
            {"name": 'Swing "@;evil()', "from": 0, "to": 0, "color": "#000000ff"},
        ]
        runs = conv.tag_runs(doc, frame_count=1)
        self.assertEqual(runs[0]["slug"], "swingevil")


if __name__ == "__main__":
    unittest.main()