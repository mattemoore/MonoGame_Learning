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


def synthetic_actor_doc(keys):
    """A 4-frame, idle+run actor export with a 'hand' slice (50x37 untrimmed frames)."""
    frames = {}
    for i in range(4):
        frames[f"adventurer {i}.aseprite"] = {
            "frame": {"x": i * 50, "y": 0, "w": 50, "h": 37},
            "rotated": False,
            "trimmed": False,
            "spriteSourceSize": {"x": 0, "y": 0, "w": 50, "h": 37},
            "sourceSize": {"w": 50, "h": 37},
            "duration": 100,
        }
    return {
        "frames": frames,
        "meta": {
            "image": "adventurer-texture.png",
            "size": {"w": 200, "h": 37},
            "frameTags": [
                {"name": "idle", "from": 0, "to": 1, "color": "#000000ff"},
                {"name": "run", "from": 2, "to": 3, "color": "#000000ff"},
            ],
            "slices": [{"name": "hand", "color": "#0000ffff", "keys": keys}],
        },
    }


def hand_keys(points):
    """Build hand slice keys from frame-local points, using a 3x4 box around each."""
    return [
        {"frame": f, "bounds": {"x": x - 1, "y": y - 1, "w": 3, "h": 4},
         "pivot": {"x": 1, "y": 1}}
        for f, (x, y) in enumerate(points)
    ]


class HandAnchorsTest(unittest.TestCase):
    POINTS = [(18, 24), (18, 22), (21, 11), (31, 21)]
    # frame-local point minus frame half-size (25, 18.5)
    EXPECTED = {
        "idle": [(-7, 5.5), (-7, 3.5)],
        "run": [(-4, -7.5), (6, 2.5)],
    }

    def assert_points(self, got, expected):
        self.assertEqual(len(got), len(expected))
        for (gx, gy), (ex, ey) in zip(got, expected):
            self.assertAlmostEqual(gx, ex, places=4)
            self.assertAlmostEqual(gy, ey, places=4)

    def test_points_are_center_relative_and_grouped_by_tag(self):
        doc = synthetic_actor_doc(hand_keys(self.POINTS))
        regions = conv.parse_frames(doc)
        runs = conv.tag_runs(doc, len(regions))
        frame_names, names_by_tag = conv.compose_frame_keys(regions, runs, "adventurer")

        result = conv.slice_hand_anchors(doc, regions, frame_names, names_by_tag)

        self.assertEqual(sorted(result), ["idle", "run"])
        self.assert_points(result["idle"], self.EXPECTED["idle"])
        self.assert_points(result["run"], self.EXPECTED["run"])

    def test_missing_slice_returns_none(self):
        doc = synthetic_actor_doc([])
        doc["meta"]["slices"] = [{"name": "handle", "keys": []}]
        regions = conv.parse_frames(doc)
        frame_names, names_by_tag = conv.compose_frame_keys(regions, conv.tag_runs(doc, len(regions)), "adventurer")

        self.assertIsNone(conv.slice_hand_anchors(doc, regions, frame_names, names_by_tag))

    def test_missing_pivot_marks_none_and_emits_note(self):
        keys = hand_keys(self.POINTS)
        del keys[1]["pivot"]  # frame 1 has no pivot
        doc = synthetic_actor_doc(keys)
        regions = conv.parse_frames(doc)
        frame_names, names_by_tag = conv.compose_frame_keys(regions, conv.tag_runs(doc, len(regions)), "adventurer")

        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            result = conv.slice_hand_anchors(doc, regions, frame_names, names_by_tag)

        self.assertIsNone(result["idle"][1], "A key with no pivot leaves a None placeholder")
        self.assertIn("no pivot", buf.getvalue())

    def test_paste_block_prints_from_synthetic_json(self):
        doc = synthetic_actor_doc(hand_keys(self.POINTS))

        with tempfile.TemporaryDirectory() as tmp:
            src = os.path.join(tmp, "adventurer.json")
            with open(src, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            out = os.path.join(tmp, "out")

            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = conv.main(["conv", src, "--name", "adventurer", "--out-dir", out])

            self.assertEqual(rc, 0)
            output = buf.getvalue()
            self.assertIn('("idle", [ new Vector2(-7, 5.5),  new Vector2(-7, 3.5) ])', output)
            self.assertIn('("run", [ new Vector2(-4, -7.5),  new Vector2(6, 2.5) ])', output)

    def test_absent_hand_slice_warns_not_fails(self):
        doc = synthetic_actor_doc([])
        doc["meta"]["slices"] = []

        with tempfile.TemporaryDirectory() as tmp:
            src = os.path.join(tmp, "adventurer.json")
            with open(src, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            out = os.path.join(tmp, "out")
            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                rc = conv.main(["conv", src, "--name", "adventurer", "--out-dir", out])

            self.assertEqual(rc, 0)
            self.assertIn("no 'hand' slice", buf.getvalue())


if __name__ == "__main__":
    unittest.main()