from __future__ import annotations

import json
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "DragonBones"
SKELETON_PATH = DRAGON_BONES_DIR / "sk2.json"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"

RUNTIME_NAME = "MoonLanternPark_RuntimeDB"
FRAME_RATE = 30

CHARACTER_IDS = (
    "pumpkin",
    "frankenstein",
    "mummy",
    "vampire",
    "candle",
    "scarecrow",
    "witch",
    "blackcat",
)

MAIN_ANIMATIONS = (
    "idle",
    "run",
    "idle_wb",
    "run_wb",
    "throw_land",
    "jump",
    "fly1",
    "fly2",
    "fly3",
    "fly4",
    "fly5",
    "landing",
    "jump_wb",
    "fly1_wb",
    "fly2_wb",
    "fly3_wb",
    "fly4_wb",
    "fly5_wb",
    "landing_wb",
    "dunk1",
    "dunk2",
    "dunk3",
    "steal",
    "pumpStart",
    "pumpEnd",
    "dash_wb",
    "dash",
    "dig1",
    "dig2",
    "dig3",
    "stun",
    "sad",
    "sad0",
    "happiness",
    "blockStart",
    "blockEnd",
    "megadunk",
    "megadunk_fly",
    "megadunk_end",
    "md_start_wb",
    "md_mid_wb",
    "md_end_wb",
    "md_start",
    "md_mid",
    "md_end",
)


def compact_float(value: float) -> int | float:
    rounded = round(value, 4)
    if abs(rounded - int(rounded)) < 0.00001:
        return int(rounded)
    return rounded


def transform(
    *,
    x: float = 0.0,
    y: float = 0.0,
    rotate: float = 0.0,
    scale_x: float = 1.0,
    scale_y: float = 1.0,
) -> dict:
    result: dict[str, int | float] = {}
    if abs(x) > 0.00001:
        result["x"] = compact_float(x)
    if abs(y) > 0.00001:
        result["y"] = compact_float(y)
    if abs(rotate) > 0.00001:
        result["skX"] = compact_float(rotate)
        result["skY"] = compact_float(rotate)
    if abs(scale_x - 1.0) > 0.00001:
        result["scX"] = compact_float(scale_x)
    if abs(scale_y - 1.0) > 0.00001:
        result["scY"] = compact_float(scale_y)
    return result


def bone(name: str, x: float = 0.0, y: float = 0.0, rotate: float = 0.0) -> dict:
    item: dict[str, object] = {"inheritScale": False, "name": name}
    tf = transform(x=x, y=y, rotate=rotate)
    if tf:
        item["transform"] = tf
    return item


def slot(name: str, parent: str | None = None, display_index: int = 0) -> dict:
    item: dict[str, object] = {"name": name, "parent": parent or name}
    if display_index != 0:
        item["displayIndex"] = display_index
    return item


def display(
    name: str,
    *,
    type_: str = "image",
    x: float = 0.0,
    y: float = 0.0,
    rotate: float = 0.0,
    scale_x: float = 1.0,
    scale_y: float = 1.0,
) -> dict:
    item: dict[str, object] = {"name": name}
    if type_ != "image":
        item["type"] = type_
    tf = transform(x=x, y=y, rotate=rotate, scale_x=scale_x, scale_y=scale_y)
    if tf:
        item["transform"] = tf
    return item


def skin_slot(name: str, displays: list[dict]) -> dict:
    return {"name": name, "display": displays}


def frame(duration: int = 1, **values: object) -> dict:
    item: dict[str, object] = {}
    if duration != 1:
        item["duration"] = duration
    for key, value in values.items():
        if value is not None:
            item[key] = value
    return item


def translate_frames(*items: tuple[int, float, float]) -> list[dict]:
    return [frame(duration, x=compact_float(x), y=compact_float(y)) for duration, x, y in items]


def rotate_frames(*items: tuple[int, float]) -> list[dict]:
    return [frame(duration, rotate=compact_float(angle)) for duration, angle in items]


def scale_frames(*items: tuple[int, float, float]) -> list[dict]:
    return [frame(duration, x=compact_float(x), y=compact_float(y)) for duration, x, y in items]


def track(
    name: str,
    *,
    translate: list[dict] | None = None,
    rotate: list[dict] | None = None,
    scale: list[dict] | None = None,
) -> dict:
    item: dict[str, object] = {"name": name}
    if translate:
        item["translateFrame"] = translate
    if rotate:
        item["rotateFrame"] = rotate
    if scale:
        item["scaleFrame"] = scale
    return item


def slot_track(
    name: str,
    *,
    display_value: int | None = None,
    duration: int = 1,
    alpha: int | None = None,
) -> dict:
    item: dict[str, object] = {"name": name}
    if display_value is not None:
        item["displayFrame"] = [frame(duration, value=display_value)]
    if alpha is not None:
        item["colorFrame"] = [frame(duration, value={"aM": alpha})]
    return item


def event_frames(duration: int, at_frame: int, event_name: str) -> list[dict]:
    at_frame = max(1, min(duration - 1, at_frame))
    return [frame(at_frame), frame(duration - at_frame, event=event_name)]


def animation(
    name: str,
    duration: int = 1,
    *,
    loop: bool = False,
    bones: list[dict] | None = None,
    slots: list[dict] | None = None,
    frames: list[dict] | None = None,
    fade: float | None = None,
) -> dict:
    item: dict[str, object] = {"name": name}
    if duration != 1:
        item["duration"] = duration
    if loop:
        item["playTimes"] = 0
    if fade is not None:
        item["fadeInTime"] = fade
    if frames:
        item["frame"] = frames
    if bones:
        item["bone"] = bones
    if slots:
        item["slot"] = slots
    return item


def hidden_optional_slots(duration: int) -> list[dict]:
    return [
        slot_track("ball", display_value=-1, duration=duration),
        slot_track("ball_front", display_value=-1, duration=duration),
        slot_track("eyes", display_value=-1, duration=duration),
        slot_track("dighand", display_value=-1, duration=duration),
        slot_track("digleg", display_value=-1, duration=duration),
        slot_track("effects stun", display_value=-1, duration=duration),
        slot_track("effects stun star", display_value=-1, duration=duration),
    ]


def with_ball_slots(duration: int, *, front: bool = False) -> list[dict]:
    slots = hidden_optional_slots(duration)
    slots[0] = slot_track("ball", display_value=0, duration=duration)
    slots[1] = slot_track("ball_front", display_value=0 if front else -1, duration=duration)
    return slots


def loop_pose(duration: int, *, with_ball: bool) -> list[dict]:
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    return slots


def main_idle() -> dict:
    duration = 24
    bones = [
        track("body", translate=translate_frames((8, 0, 0), (8, 0, -2), (8, 0, 0))),
        track("head", translate=translate_frames((8, 0, 0), (8, 1, -3), (8, 0, 0)), rotate=rotate_frames((8, 0), (8, -3), (8, 0))),
        track("left hand", translate=translate_frames((8, 0, 0), (8, -2, -2), (8, 0, 0)), rotate=rotate_frames((8, 4), (8, 12), (8, 4))),
        track("right hand", translate=translate_frames((8, 0, 0), (8, 2, -1), (8, 0, 0)), rotate=rotate_frames((8, -4), (8, -12), (8, -4))),
    ]
    return animation("idle", duration, loop=True, bones=bones, slots=loop_pose(duration, with_ball=False), fade=0.1)


def main_run() -> dict:
    duration = 18
    bones = [
        track("body", translate=translate_frames((5, 0, 1), (4, 0, -6), (5, 0, 1), (4, 0, -6))),
        track("head", translate=translate_frames((5, 1, 1), (4, -1, -7), (5, 2, 2), (4, -1, -7)), rotate=rotate_frames((5, 5), (4, -7), (5, 5), (4, -7))),
        track("left leg", translate=translate_frames((5, 10, -4), (4, -15, -10), (5, 12, -4), (4, -17, -10)), rotate=rotate_frames((5, -32), (4, 36), (5, -32), (4, 36))),
        track("right leg", translate=translate_frames((5, -13, -8), (4, 18, -5), (5, -15, -8), (4, 20, -5)), rotate=rotate_frames((5, 38), (4, -28), (5, 38), (4, -28))),
        track("left hand", translate=translate_frames((5, 10, -6), (4, -6, 2), (5, 9, -7), (4, -5, 1)), rotate=rotate_frames((5, -15), (4, 20), (5, -15), (4, 20))),
        track("right hand", translate=translate_frames((5, -7, 2), (4, 10, -6), (5, -8, 1), (4, 11, -7)), rotate=rotate_frames((5, 18), (4, -22), (5, 18), (4, -22))),
    ]
    return animation("run", duration, loop=True, bones=bones, slots=loop_pose(duration, with_ball=False), fade=0.1)


def main_idle_wb() -> dict:
    duration = 18
    bones = [
        track("body", translate=translate_frames((6, 0, 0), (6, 0, -2), (6, 0, 0))),
        track("head", translate=translate_frames((6, 1, 0), (6, 2, -2), (6, 1, 0)), rotate=rotate_frames((6, 4), (6, 9), (6, 4))),
        track("left hand", translate=translate_frames((6, 8, -18), (6, 8, 4), (6, 8, -18)), rotate=rotate_frames((6, -36), (6, -8), (6, -36))),
        track("right hand", translate=translate_frames((6, -1, 2), (6, 2, 5), (6, -1, 2)), rotate=rotate_frames((6, 8), (6, 18), (6, 8))),
        track("ball", translate=translate_frames((6, 0, 0), (6, 0, 30), (6, 0, 0)), scale=scale_frames((6, 1, 1), (6, 1.2, 0.68), (6, 1, 1))),
    ]
    return animation(
        "idle_wb",
        duration,
        loop=True,
        bones=bones,
        slots=with_ball_slots(duration),
        frames=event_frames(duration, 9, "floor0"),
        fade=0.1,
    )


def main_run_wb() -> dict:
    duration = 14
    bones = [
        track("body", translate=translate_frames((3, 3, -8), (4, 5, -2), (3, 2, -9), (4, 4, -1)), rotate=rotate_frames((3, -4), (4, 6), (3, -4), (4, 6))),
        track("head", translate=translate_frames((3, 5, -10), (4, 10, -2), (3, 5, -10), (4, 9, -2)), rotate=rotate_frames((3, -10), (4, 3), (3, -10), (4, 3))),
        track("left leg", translate=translate_frames((3, 14, -16), (4, -8, -4), (3, 16, -12), (4, -10, -5)), rotate=rotate_frames((3, -42), (4, 28), (3, -34), (4, 34))),
        track("right leg", translate=translate_frames((3, -18, -12), (4, 18, 0), (3, -16, -14), (4, 16, 0)), rotate=rotate_frames((3, 54), (4, -10), (3, 48), (4, -14))),
        track("left hand", translate=translate_frames((7, 11, -26), (7, 13, 0)), rotate=rotate_frames((7, -40), (7, -15))),
        track("right hand", translate=translate_frames((7, -6, -15), (7, 2, -4)), rotate=rotate_frames((7, 42), (7, 18))),
        track("ball", translate=translate_frames((7, 4, -4), (3, 6, 28), (4, 5, -4)), scale=scale_frames((7, 1, 1), (3, 1.25, 0.6), (4, 1, 1))),
    ]
    return animation(
        "run_wb",
        duration,
        loop=True,
        bones=bones,
        slots=with_ball_slots(duration),
        frames=event_frames(duration, 7, "floor"),
        fade=0.1,
    )


def jump_like(name: str, *, with_ball: bool, duration: int, lift: float, rotate: float = 0.0) -> dict:
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    bones = [
        track("body", translate=translate_frames((duration - 1, 0, -lift), (1, 0, -lift)), rotate=rotate_frames((duration, rotate))),
        track("head", translate=translate_frames((duration - 1, 2, -lift - 6), (1, 2, -lift - 6)), rotate=rotate_frames((duration, rotate * 0.8))),
        track("left hand", translate=translate_frames((duration - 1, 8, -lift - 30), (1, 8, -lift - 30)), rotate=rotate_frames((duration, -70 + rotate))),
        track("right hand", translate=translate_frames((duration - 1, -4, -lift - 8), (1, -4, -lift - 8)), rotate=rotate_frames((duration, 45 + rotate))),
        track("left leg", translate=translate_frames((duration, 5, -8)), rotate=rotate_frames((duration, 45))),
        track("right leg", translate=translate_frames((duration, -4, -8)), rotate=rotate_frames((duration, 68))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((duration, 5, -lift - 2)), rotate=rotate_frames((duration, 35))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def landing_like(name: str, *, with_ball: bool) -> dict:
    duration = 8
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    bones = [
        track("body", translate=translate_frames((3, 0, -6), (3, 0, 4), (2, 0, 0)), scale=scale_frames((3, 1.04, 0.92), (3, 0.98, 1.08), (2, 1, 1))),
        track("head", translate=translate_frames((3, 2, -8), (3, 1, 4), (2, 0, 0)), rotate=rotate_frames((3, -8), (3, 3), (2, 0))),
        track("left leg", translate=translate_frames((3, 6, -5), (3, 2, 2), (2, 0, 0)), rotate=rotate_frames((3, 35), (3, 10), (2, 0))),
        track("right leg", translate=translate_frames((3, -5, -4), (3, -2, 2), (2, 0, 0)), rotate=rotate_frames((3, 28), (3, 8), (2, 0))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((3, 4, -20), (3, 2, 4), (2, 0, 0))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def throw_land() -> dict:
    duration = 10
    slots = with_ball_slots(duration)
    slots[0] = {
        "name": "ball",
        "displayFrame": [frame(5, value=0), frame(5, value=-1)],
    }
    bones = [
        track("body", translate=translate_frames((4, 0, -4), (3, 1, -8), (3, 0, 0))),
        track("head", translate=translate_frames((4, 2, -4), (3, 2, -10), (3, 0, 0)), rotate=rotate_frames((4, 5), (3, -8), (3, 0))),
        track("left hand", translate=translate_frames((4, 8, -24), (3, -10, -12), (3, 2, 0)), rotate=rotate_frames((4, -55), (3, -105), (3, 8))),
        track("right hand", translate=translate_frames((4, 4, -4), (3, 34, -24), (3, 0, 0)), rotate=rotate_frames((4, 20), (3, -95), (3, 0))),
        track("ball", translate=translate_frames((5, 2, -2), (1, 30, -24), (4, 34, -32)), rotate=rotate_frames((5, 0), (1, 75), (4, 120))),
    ]
    return animation(
        "throw_land",
        duration,
        bones=bones,
        slots=slots,
        frames=event_frames(duration, 5, "throw"),
        fade=0.01,
    )


def dunk(name: str, duration: int, windup: float, reach: float) -> dict:
    event_at = max(7, duration - 5)
    slots = with_ball_slots(duration, front=True)
    slots[0] = {
        "name": "ball",
        "displayFrame": [frame(event_at, value=0), frame(duration - event_at, value=-1)],
    }
    slots[1] = {
        "name": "ball_front",
        "displayFrame": [frame(event_at, value=0), frame(duration - event_at, value=-1)],
    }
    bones = [
        track("body", translate=translate_frames((5, 0, -windup), (duration - 5, 4, -reach)), rotate=rotate_frames((5, -8), (duration - 5, 8))),
        track("head", translate=translate_frames((5, 1, -windup - 8), (duration - 5, 5, -reach - 10)), rotate=rotate_frames((5, -10), (duration - 5, 10))),
        track("left hand", translate=translate_frames((5, 10, -windup - 25), (duration - 5, 8, -reach - 52)), rotate=rotate_frames((5, -65), (duration - 5, -92))),
        track("right hand", translate=translate_frames((5, -6, -windup - 5), (duration - 5, 28, -reach - 48)), rotate=rotate_frames((5, 35), (duration - 5, -82))),
        track("ball", translate=translate_frames((event_at, 20, -reach - 45), (duration - event_at, 28, -reach - 55)), scale=scale_frames((event_at, 1.05, 1.05), (duration - event_at, 0.9, 0.9))),
        track("ball_front", translate=translate_frames((event_at, 20, -reach - 45), (duration - event_at, 28, -reach - 55))),
    ]
    return animation(name, duration, bones=bones, slots=slots, frames=event_frames(duration, event_at, "dunk"), fade=0.01)


def steal() -> dict:
    duration = 12
    bones = [
        track("body", translate=translate_frames((4, 0, -2), (4, 12, -4), (4, 0, 0)), rotate=rotate_frames((4, 0), (4, -8), (4, 0))),
        track("head", translate=translate_frames((4, 0, -2), (4, 15, -6), (4, 0, 0)), rotate=rotate_frames((4, 0), (4, -12), (4, 0))),
        track("right hand", translate=translate_frames((4, 0, 0), (4, 42, -10), (4, 0, 0)), rotate=rotate_frames((4, 0), (4, -70), (4, 0))),
        track("left hand", translate=translate_frames((4, 0, 0), (4, -8, 4), (4, 0, 0)), rotate=rotate_frames((4, 0), (4, 22), (4, 0))),
    ]
    return animation(
        "steal",
        duration,
        bones=bones,
        slots=hidden_optional_slots(duration),
        frames=event_frames(duration, 7, "action"),
        fade=0.01,
    )


def pump_block(name: str, *, pump: bool, start: bool) -> dict:
    duration = 7
    slots = with_ball_slots(duration) if pump else hidden_optional_slots(duration)
    if not pump:
        slots[2] = slot_track("eyes", display_value=1, duration=duration)
        slots[5] = slot_track("effects stun", display_value=1, duration=duration)
    height = -12 if start else 0
    arms = -62 if start else -10
    bones = [
        track("body", translate=translate_frames((duration, 0, height)), scale=scale_frames((duration, 1.03 if start else 1, 0.96 if start else 1))),
        track("head", translate=translate_frames((duration, 1, height - 2)), rotate=rotate_frames((duration, -4 if start else 0))),
        track("left hand", translate=translate_frames((duration, 5, arms)), rotate=rotate_frames((duration, -70 if start else 0))),
        track("right hand", translate=translate_frames((duration, -5, arms)), rotate=rotate_frames((duration, 70 if start else 0))),
    ]
    if pump:
        bones.append(track("ball", translate=translate_frames((duration, 0, -30 if start else 0)), scale=scale_frames((duration, 1.18 if start else 1, 0.72 if start else 1))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def dash(name: str, *, with_ball: bool) -> dict:
    duration = 8
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    slots[5] = slot_track("effects stun", display_value=1, duration=duration)
    bones = [
        track("body", translate=translate_frames((4, 10, -6), (4, 0, -2)), rotate=rotate_frames((4, -12), (4, 0))),
        track("head", translate=translate_frames((4, 14, -8), (4, 2, -2)), rotate=rotate_frames((4, -16), (4, 0))),
        track("left leg", translate=translate_frames((4, -18, -6), (4, 8, -2)), rotate=rotate_frames((4, 58), (4, -24))),
        track("right leg", translate=translate_frames((4, 20, -8), (4, -6, -2)), rotate=rotate_frames((4, -46), (4, 22))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((4, 8, -4), (4, 0, 0))))
    return animation(name, duration, loop=True, bones=bones, slots=slots, fade=0.01)


def dig(name: str, *, phase: int) -> dict:
    duration = 8
    slots = hidden_optional_slots(duration)
    slots[3] = slot_track("dighand", display_value=0, duration=duration)
    slots[4] = slot_track("digleg", display_value=0, duration=duration)
    reach = phase * 8
    bones = [
        track("body", translate=translate_frames((duration, 5 + reach, -4))),
        track("head", translate=translate_frames((duration, 6 + reach, -8)), rotate=rotate_frames((duration, -10))),
        track("dighand", translate=translate_frames((duration, 34 + reach, -8)), rotate=rotate_frames((duration, -64))),
        track("digleg", translate=translate_frames((duration, -8, -4)), rotate=rotate_frames((duration, 34))),
    ]
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def stun() -> dict:
    duration = 20
    slots = hidden_optional_slots(duration)
    slots[2] = slot_track("eyes", display_value=0, duration=duration)
    slots[5] = slot_track("effects stun", display_value=0, duration=duration)
    slots[6] = slot_track("effects stun star", display_value=0, duration=duration)
    bones = [
        track("body", translate=translate_frames((5, -2, 0), (5, 2, -2), (5, -2, 0), (5, 2, -2))),
        track("head", translate=translate_frames((5, -3, -2), (5, 3, -3), (5, -3, -2), (5, 3, -3)), rotate=rotate_frames((5, -8), (5, 8), (5, -8), (5, 8))),
        track("effects stun star", rotate=rotate_frames((5, 0), (5, 90), (5, 180), (5, 270))),
    ]
    return animation("stun", duration, loop=True, bones=bones, slots=slots, fade=0.01)


def mood(name: str, *, happy: bool) -> dict:
    duration = 24
    bend = -5 if happy else 7
    lift = -4 if happy else 2
    slots = hidden_optional_slots(duration)
    if not happy:
        slots[5] = slot_track("effects stun", display_value=5, duration=duration)
    bones = [
        track("body", translate=translate_frames((12, 0, lift), (12, 0, 0))),
        track("head", translate=translate_frames((12, 0, lift - 2), (12, 0, 0)), rotate=rotate_frames((12, bend), (12, 0))),
        track("left hand", rotate=rotate_frames((12, -20 if happy else 18), (12, 0))),
        track("right hand", rotate=rotate_frames((12, 20 if happy else -18), (12, 0))),
    ]
    return animation(name, duration, loop=True, bones=bones, slots=slots, fade=0.1)


def mega(name: str, *, end: bool = False, fly: bool = False) -> dict:
    duration = 18 if not end else 12
    slots = with_ball_slots(duration, front=True)
    slots[2] = slot_track("eyes", display_value=1, duration=duration)
    slots[5] = slot_track("effects stun", display_value=7, duration=duration)
    bones = [
        track("body", translate=translate_frames((duration, 4 if not end else 0, -28 if not end else 0)), rotate=rotate_frames((duration, 12 if not end else 0))),
        track("head", translate=translate_frames((duration, 4 if not end else 0, -36 if not end else 0)), rotate=rotate_frames((duration, 14 if not end else 0))),
        track("left hand", translate=translate_frames((duration, 6, -55 if not end else -5)), rotate=rotate_frames((duration, -80 if not end else 0))),
        track("right hand", translate=translate_frames((duration, 24 if not end else 0, -50 if not end else 0)), rotate=rotate_frames((duration, -70 if not end else 0))),
        track("ball", translate=translate_frames((duration, 22 if not end else 0, -54 if not end else 0)), scale=scale_frames((duration, 1.3 if not end else 1, 1.3 if not end else 1))),
    ]
    frames = event_frames(duration, duration - 3, "mega") if name == "megadunk" else None
    return animation(name, duration, loop=fly, bones=bones, slots=slots, frames=frames, fade=0.01)


def md(name: str, *, with_ball: bool, start: bool, end: bool) -> dict:
    duration = 7 if start or end else 14
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    slots[2] = slot_track("eyes", display_value=1, duration=duration)
    slots[5] = slot_track("effects stun", display_value=1, duration=duration)
    offset = 18 if start else -10 if end else 8
    bones = [
        track("body", translate=translate_frames((duration, offset, -8)), rotate=rotate_frames((duration, -16 if not end else 5))),
        track("head", translate=translate_frames((duration, offset + 4, -10)), rotate=rotate_frames((duration, -18 if not end else 6))),
        track("left leg", translate=translate_frames((duration, -12, -4)), rotate=rotate_frames((duration, 55))),
        track("right leg", translate=translate_frames((duration, 20, -4)), rotate=rotate_frames((duration, -45))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((duration, offset, -6))))
    return animation(name, duration, loop=not start and not end, bones=bones, slots=slots, fade=0.01)


def main_armature() -> dict:
    bones = [
        bone("left leg", 7, -9),
        bone("left hand", 18, -31),
        bone("body", 0, -32),
        bone("right leg", -8, -8),
        bone("head", 0, -72),
        bone("right hand", -23, -30, 10),
        bone("dighand", -30, -30),
        bone("digleg", -12, -7),
        bone("ball", 31, -52, 4),
        bone("ball_front", 12, -18, 78),
        bone("effects stun", 0, -71),
        bone("eyes", 0, -76),
        bone("effects stun star", -18, -73, -12),
    ]
    slots = [
        slot("left leg"),
        slot("left hand"),
        slot("body"),
        slot("right leg"),
        slot("ball", display_index=-1),
        slot("head"),
        slot("ball_front", display_index=-1),
        slot("right hand"),
        slot("eyes", display_index=-1),
        slot("dighand", display_index=-1),
        slot("digleg", display_index=-1),
        slot("effects stun", display_index=-1),
        slot("effects stun star", display_index=-1),
    ]
    skin = [
        skin_slot("left leg", [display("dbanims/LegsDB", type_="armature")]),
        skin_slot("left hand", [display("dbanims/LeftHandDB", type_="armature")]),
        skin_slot("body", [display("BodyDB", type_="armature")]),
        skin_slot("right leg", [display("dbanims/LegsDB", type_="armature")]),
        skin_slot("ball", [display(".Game/ball/BallClip")]),
        skin_slot("head", [display("HeadsDB", type_="armature")]),
        skin_slot("ball_front", [display(".Game/ball/BallClip")]),
        skin_slot("right hand", [display("dbanims/RightHandDB", type_="armature")]),
        skin_slot("eyes", [display("dbanims/eyes_stunned", type_="armature"), display("shield_animation_01", type_="armature")]),
        skin_slot("dighand", [display("dbanims/LeftHandDB2", type_="armature")]),
        skin_slot("digleg", [display("dbanims/LegsDB2", type_="armature")]),
        skin_slot(
            "effects stun",
            [
                display("dbanims/kuynya_01"),
                display("dbanims/backwind_01", type_="armature"),
                display("dbanims/circles1"),
                display("dbanims/circle2"),
                display("dbanims/circle3"),
                display("dbanims/tears_01", type_="armature"),
                display("dbanims/CupDB", type_="armature"),
                display("man_fire_01", type_="armature"),
            ],
        ),
        skin_slot("effects stun star", [display("dbanims/star_0123")]),
    ]
    animations = [
        main_idle(),
        main_run(),
        main_idle_wb(),
        main_run_wb(),
        throw_land(),
        jump_like("jump", with_ball=False, duration=8, lift=24),
        jump_like("fly1", with_ball=False, duration=10, lift=30, rotate=-6),
        jump_like("fly2", with_ball=False, duration=10, lift=36, rotate=4),
        jump_like("fly3", with_ball=False, duration=10, lift=40, rotate=10),
        jump_like("fly4", with_ball=False, duration=10, lift=34, rotate=-8),
        jump_like("fly5", with_ball=False, duration=10, lift=28, rotate=0),
        landing_like("landing", with_ball=False),
        jump_like("jump_wb", with_ball=True, duration=8, lift=24),
        jump_like("fly1_wb", with_ball=True, duration=10, lift=30, rotate=-6),
        jump_like("fly2_wb", with_ball=True, duration=10, lift=36, rotate=4),
        jump_like("fly3_wb", with_ball=True, duration=10, lift=40, rotate=10),
        jump_like("fly4_wb", with_ball=True, duration=10, lift=34, rotate=-8),
        jump_like("fly5_wb", with_ball=True, duration=10, lift=28, rotate=0),
        landing_like("landing_wb", with_ball=True),
        dunk("dunk1", 18, 16, 42),
        dunk("dunk2", 20, 20, 50),
        dunk("dunk3", 22, 12, 58),
        steal(),
        pump_block("pumpStart", pump=True, start=True),
        pump_block("pumpEnd", pump=True, start=False),
        dash("dash_wb", with_ball=True),
        dash("dash", with_ball=False),
        dig("dig1", phase=1),
        dig("dig2", phase=2),
        dig("dig3", phase=3),
        stun(),
        mood("sad", happy=False),
        mood("sad0", happy=False),
        mood("happiness", happy=True),
        pump_block("blockStart", pump=False, start=True),
        pump_block("blockEnd", pump=False, start=False),
        mega("megadunk"),
        mega("megadunk_fly", fly=True),
        mega("megadunk_end", end=True),
        md("md_start_wb", with_ball=True, start=True, end=False),
        md("md_mid_wb", with_ball=True, start=False, end=False),
        md("md_end_wb", with_ball=True, start=False, end=True),
        md("md_start", with_ball=False, start=True, end=False),
        md("md_mid", with_ball=False, start=False, end=False),
        md("md_end", with_ball=False, start=False, end=True),
    ]
    return {"name": "playerSmall", "bone": bones, "slot": slots, "skin": [{"slot": skin}], "animation": animations}


def picker_armature(name: str, slot_name: str, display_names: list[str], animation_names: list[str]) -> dict:
    bones = [bone(slot_name)]
    slots = [slot(slot_name)]
    skin = [skin_slot(slot_name, [display(item) for item in display_names])]
    animations = [
        animation(animation_name, slots=[slot_track(slot_name, display_value=index)])
        for index, animation_name in enumerate(animation_names)
    ]
    return {"name": name, "bone": bones, "slot": slots, "skin": [{"slot": skin}], "animation": animations}


def character_part_armatures() -> list[dict]:
    leg_displays = [f"custom_leg_{item}" for item in CHARACTER_IDS]
    left_hand_displays = [f"custom_left_hand_{item}" for item in CHARACTER_IDS]
    right_hand_displays = [f"custom_right_hand_{item}" for item in CHARACTER_IDS]
    dig_hand_displays = [f"custom_dig_hand_{item}" for item in CHARACTER_IDS]
    body_displays = [f"custom_body_{item}" for item in CHARACTER_IDS]
    head_displays = [f"custom_head_{item}" for item in CHARACTER_IDS]
    hand_anims = [f"hand{index}" for index in range(1, 9)]
    leg_anims = [f"leg{index}" for index in range(1, 9)]
    return [
        picker_armature("dbanims/LegsDB", "leg sprite", leg_displays, leg_anims),
        picker_armature("dbanims/LeftHandDB", "left hand sprite", left_hand_displays, hand_anims),
        picker_armature("BodyDB", "body sprite", body_displays, [f"body{index}" for index in range(1, 9)]),
        picker_armature("HeadsDB", "head sprite", head_displays, [f"head{index}" for index in range(1, 9)]),
        picker_armature("dbanims/RightHandDB", "right hand sprite", right_hand_displays, hand_anims),
        picker_armature("dbanims/LeftHandDB2", "dig hand sprite", dig_hand_displays, hand_anims),
        picker_armature("dbanims/LegsDB2", "dig leg sprite", leg_displays, leg_anims),
    ]


def backwind_armature() -> dict:
    return picker_armature(
        "dbanims/backwind_01",
        "wind sprite",
        ["wind0", "wind1", "wind2"],
        ["wind0", "wind1", "wind2"],
    ) | {
        "animation": [
            animation(
                "anim",
                6,
                loop=True,
                slots=[{"name": "wind sprite", "displayFrame": [frame(2, value=0), frame(2, value=1), frame(2, value=2)]}],
            )
        ]
    }


def eyes_armature() -> dict:
    bones = [bone("eye left", 8, 0), bone("eye right", -8, 0)]
    slots = [slot("eye left"), slot("eye right")]
    skin = [
        skin_slot("eye left", [display("dbanims/eye34635")]),
        skin_slot("eye right", [display("dbanims/eye23434")]),
    ]
    animations = [
        animation(
            "stuneye",
            12,
            loop=True,
            bones=[
                track("eye left", rotate=rotate_frames((3, 0), (3, 90), (3, 180), (3, 270))),
                track("eye right", rotate=rotate_frames((3, 0), (3, -90), (3, -180), (3, -270))),
            ],
        )
    ]
    return {"name": "dbanims/eyes_stunned", "bone": bones, "slot": slots, "skin": [{"slot": skin}], "animation": animations}


def tears_armature() -> dict:
    displays = ["tears01", "tears02", "tears03", "tears04", "tears05"]
    return {
        "name": "dbanims/tears_01",
        "bone": [bone("tear sprite")],
        "slot": [slot("tear sprite")],
        "skin": [{"slot": [skin_slot("tear sprite", [display(item) for item in displays])]}],
        "animation": [
            animation(
                "anim",
                5,
                loop=True,
                bones=[track("tear sprite", translate=translate_frames((1, 0, 0), (1, 1, -1), (1, -1, -2), (1, 1, -3), (1, 0, -4)))],
                slots=[{"name": "tear sprite", "displayFrame": [frame(1, value=index) for index in range(5)]}],
            )
        ],
    }


def cup_armature() -> dict:
    displays = ["dbanims/cup_01", "dbanims/cup_02", "dbanims/cup_03"]
    return {
        "name": "dbanims/CupDB",
        "bone": [bone("cup sprite")],
        "slot": [slot("cup sprite", display_index=-1)],
        "skin": [{"slot": [skin_slot("cup sprite", [display(item) for item in displays])]}],
        "animation": [
            animation("cup0", slots=[slot_track("cup sprite", display_value=-1)]),
            animation("cup1", slots=[slot_track("cup sprite", display_value=0)]),
            animation("cup2", slots=[slot_track("cup sprite", display_value=1)]),
            animation("cup3", slots=[slot_track("cup sprite", display_value=2)]),
        ],
    }


def fire_armature() -> dict:
    blur = [f"fx_Blur_mol{index}" for index in range(5)]
    flame = [f"fx_fire_{index}" for index in range(5)]
    return {
        "name": "man_fire_01",
        "bone": [bone("fire blur", -8, 0), bone("fire core", 0, -4)],
        "slot": [slot("fire blur"), slot("fire core")],
        "skin": [
            {
                "slot": [
                    skin_slot("fire blur", [display(item) for item in blur]),
                    skin_slot("fire core", [display(item) for item in flame]),
                ]
            }
        ],
        "animation": [
            animation(
                "fire2",
                5,
                loop=True,
                bones=[
                    track("fire blur", translate=translate_frames((1, 0, 0), (1, 2, -1), (1, 3, -2), (1, 1, -1), (1, 0, 0))),
                    track("fire core", translate=translate_frames((1, 0, 0), (1, 1, 1), (1, -1, 2), (1, 1, 1), (1, 0, 0))),
                ],
                slots=[
                    {"name": "fire blur", "displayFrame": [frame(1, value=index) for index in range(5)]},
                    {"name": "fire core", "displayFrame": [frame(1, value=index) for index in range(5)]},
                ],
            )
        ],
    }


def shield_armatures() -> list[dict]:
    wrapper = {
        "name": "shield_animation_01",
        "bone": [bone("shield wrapper", 40, 0)],
        "slot": [slot("shield wrapper")],
        "skin": [{"slot": [skin_slot("shield wrapper", [display("shield_anim", type_="armature", scale_x=1.2, scale_y=1.2)])]}],
        "animation": [animation("anim")],
    }
    smoke = [f"fx_smoke_{index}" for index in range(8)]
    shield = {
        "name": "shield_anim",
        "bone": [
            bone("shield core"),
            bone("shield smoke", -8, 4),
            bone("shield spark a", 18, -18),
            bone("shield spark b", -20, 18),
            bone("shield streak a", 28, -6),
            bone("shield streak b", 16, 18),
        ],
        "slot": [
            slot("shield core"),
            slot("shield smoke"),
            slot("shield spark a"),
            slot("shield spark b"),
            slot("shield streak a"),
            slot("shield streak b"),
        ],
        "skin": [
            {
                "slot": [
                    skin_slot("shield core", [display("gsgbfyjgkh", scale_x=0.55, scale_y=0.55)]),
                    skin_slot("shield smoke", [display(item) for item in smoke]),
                    skin_slot("shield spark a", [display("part1", scale_x=2, scale_y=2)]),
                    skin_slot("shield spark b", [display("part1", scale_x=2, scale_y=2)]),
                    skin_slot("shield streak a", [display("fx_spl_0", scale_x=0.7, scale_y=0.7)]),
                    skin_slot("shield streak b", [display("fx_spl2_0", scale_x=0.9, scale_y=0.9)]),
                ]
            }
        ],
        "animation": [
            animation(
                "unnamed",
                16,
                loop=True,
                bones=[
                    track("shield core", scale=scale_frames((4, 0.9, 0.9), (4, 1.05, 1.05), (4, 0.95, 1.0), (4, 1.0, 0.95))),
                    track("shield smoke", translate=translate_frames((4, 0, 0), (4, 3, -2), (4, -2, 2), (4, 0, 0))),
                    track("shield spark a", translate=translate_frames((4, 0, 0), (4, 10, -6), (4, -6, 4), (4, 0, 0))),
                    track("shield spark b", translate=translate_frames((4, 0, 0), (4, -8, 6), (4, 5, -4), (4, 0, 0))),
                    track("shield streak a", rotate=rotate_frames((4, -20), (4, 35), (4, 80), (4, -20))),
                    track("shield streak b", rotate=rotate_frames((4, 40), (4, 90), (4, -30), (4, 40))),
                ],
                slots=[{"name": "shield smoke", "displayFrame": [frame(2, value=index) for index in range(8)]}],
            )
        ],
    }
    return [wrapper, shield]


def fx_armatures() -> list[dict]:
    return [
        backwind_armature(),
        eyes_armature(),
        tears_armature(),
        cup_armature(),
        fire_armature(),
        *shield_armatures(),
    ]


def skeleton() -> dict:
    return {
        "frameRate": FRAME_RATE,
        "name": RUNTIME_NAME,
        "version": "5.5",
        "compatibleVersion": "5.5",
        "userData": {
            "generator": "Tools/Art/rebuild_runtime_dragonbones_skeleton.py",
            "note": "Project-authored DBLite-compatible skeleton data. Runtime names are kept stable for gameplay compatibility.",
        },
        "armature": [main_armature(), *character_part_armatures(), *fx_armatures()],
    }


def collect_display_references(data: dict) -> tuple[set[str], set[str]]:
    armatures = {item["name"] for item in data["armature"]}
    image_refs: set[str] = set()
    armature_refs: set[str] = set()

    for armature in data["armature"]:
        for skin in armature.get("skin", []):
            for skin_slot_data in skin.get("slot", []):
                for display_data in skin_slot_data.get("display", []):
                    name = display_data.get("name")
                    if not name:
                        continue
                    if display_data.get("type") == "armature":
                        armature_refs.add(name)
                    else:
                        image_refs.add(name)

    missing_armatures = armature_refs - armatures
    if missing_armatures:
        raise RuntimeError(f"Missing child armatures: {sorted(missing_armatures)}")

    return image_refs, armatures


def validate(data: dict, texture_json: dict) -> None:
    image_refs, armatures = collect_display_references(data)
    texture_names = {item["name"] for item in texture_json.get("SubTexture", [])}
    missing_images = image_refs - texture_names
    if missing_images:
        raise RuntimeError(f"Missing texture references: {sorted(missing_images)}")

    main = next(item for item in data["armature"] if item["name"] == "playerSmall")
    animation_names = {item["name"] for item in main["animation"]}
    missing_animations = set(MAIN_ANIMATIONS) - animation_names
    if missing_animations:
        raise RuntimeError(f"Missing playerSmall animations: {sorted(missing_animations)}")

    required_events = {"floor0", "floor", "throw", "dunk", "action", "mega"}
    events: set[str] = set()
    for anim in main["animation"]:
        for item in anim.get("frame", []):
            if "event" in item:
                events.add(item["event"])
    missing_events = required_events - events
    if missing_events:
        raise RuntimeError(f"Missing frame events: {sorted(missing_events)}")

    if "playerSmall" not in armatures:
        raise RuntimeError("playerSmall armature is missing.")


def update_texture_json_name() -> None:
    texture_json = json.loads(TEXTURE_JSON_PATH.read_text(encoding="utf-8"))
    texture_json["name"] = RUNTIME_NAME
    TEXTURE_JSON_PATH.write_text(json.dumps(texture_json, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def main() -> None:
    data = skeleton()
    texture_json = json.loads(TEXTURE_JSON_PATH.read_text(encoding="utf-8"))
    validate(data, texture_json)

    SKELETON_PATH.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    update_texture_json_name()

    print(
        f"Wrote {SKELETON_PATH.relative_to(REPO_ROOT)} with "
        f"{len(data['armature'])} armatures and {len(main_armature()['animation'])} player animations."
    )
    print(f"Updated {TEXTURE_JSON_PATH.relative_to(REPO_ROOT)} root name to {RUNTIME_NAME}.")


if __name__ == "__main__":
    main()
