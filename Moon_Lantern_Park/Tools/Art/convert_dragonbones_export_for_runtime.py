"""
把 DragonBones 编辑器导出的三件套文件，转换成 Moon Lantern Park 项目里
这个自定义 DBLite 运行时可以直接读取的格式。

====================================
一、这个脚本是干什么的
====================================
这个项目真正运行时读取的是：

- sk2.json
- texture2.json
- texture2.png

但 DragonBones 编辑器导出来的通常是：

- <基础名>_ske.json
- <基础名>_tex.json
- <基础名>_tex.png

例如：

- NewProject_3_ske.json
- NewProject_3_tex.json
- NewProject_3_tex.png

两边并不是完全同一种格式。

原因是：Moon Lantern Park 这个项目里使用的不是“官方 DragonBones 原样运行时”，
而是项目自己的轻量运行时（DBLite）。因此从 DragonBones 编辑器重新导出的文件，
通常还需要再做一次“修正 / 转换”，才能重新放回项目使用。

这个脚本就是专门做这件事的。

====================================
二、这个脚本会做哪些事情
====================================
1. 自动找到一套完整的 DragonBones 导出文件：
   - xxx_ske.json
   - xxx_tex.json
   - xxx_tex.png

2. 把旧版 DragonBones 导出的动画时间轴结构，
   转成项目运行时可以识别的结构。

3. 保留项目模板 sk2.json 中一些运行时强依赖的结构。
   其中最关键的是 `playerSmall`：
   它在这个项目里不仅仅是一个普通骨架，
   还承担了换头、换身体、换手脚子骨架的入口。
   所以它的骨骼 / 插槽 / 皮肤结构不能被编辑器导出结果直接覆盖。

4. 把结果输出成项目固定文件名：
   - sk2.json
   - texture2.json
   - texture2.png

5. 如果你使用 `--install-to-project`，
   脚本会在覆盖项目正式资源之前，自动把旧文件备份起来。

====================================
三、默认读取目录和默认输出目录
====================================
默认读取目录：

- C:\\Users\\你的用户名\\Desktop\\动画

默认输出目录：

- 当前仓库下的 tmp\\dragonbones_runtime_export

如果加上：

- --install-to-project

就会直接输出到项目正式目录：

- Assets\\mlp\\Resources\\mlp\\DragonBones

并且会自动备份旧文件到：

- Assets\\mlp\\Resources\\mlp\\DragonBones\\_backup\\时间戳

====================================
四、最常用的命令
====================================
1. 用默认目录读取，并输出到 tmp：

   python Tools\\Art\\convert_dragonbones_export_for_runtime.py

2. 指定导出源目录：

   python Tools\\Art\\convert_dragonbones_export_for_runtime.py --source-dir C:\\AppData

3. 指定基础名：

   python Tools\\Art\\convert_dragonbones_export_for_runtime.py --source-dir C:\\AppData --base-name NewProject_3

4. 直接覆盖安装到项目里（会自动备份旧文件）：

   python Tools\\Art\\convert_dragonbones_export_for_runtime.py --source-dir C:\\AppData --base-name NewProject_3 --install-to-project

5. 如果你平时就是导出到桌面的“动画”文件夹，并且目录里最新那套就是你刚导出的文件，
   那么最省事的命令就是：

   python Tools\\Art\\convert_dragonbones_export_for_runtime.py --install-to-project

====================================
五、什么时候建议加 --base-name
====================================
如果你的导出目录里同时放了多套 DragonBones 导出文件，
那就建议加 `--base-name`。

因为当你不指定 `--base-name` 时，
脚本会默认选择“最近修改时间最新”的那套 *_ske.json。

====================================
六、自动备份是怎么工作的
====================================
当你加了 `--install-to-project` 时：

1. 脚本会检查项目当前目录里是否已经存在：
   - sk2.json
   - texture2.json
   - texture2.png

2. 如果存在，就会先复制到：
   - Assets\\mlp\\Resources\\mlp\\DragonBones\\_backup\\YYYYMMDD_HHMMSS

3. 然后再把新转换出来的文件覆盖写入正式目录。

这样就算新资源效果不对，你也可以很方便地从备份目录回滚。
"""
from __future__ import annotations

import argparse
import copy
import json
import shutil
from collections import Counter
from datetime import datetime
from pathlib import Path
from typing import Any


# 仓库根目录：通过当前脚本的位置反推出项目根目录。
REPO_ROOT = Path(__file__).resolve().parents[2]

# 项目真正读取 DragonBones 运行时资源的目录。
PROJECT_DRAGONBONES_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "DragonBones"

# 默认源目录：用户桌面的“动画”文件夹。
# 这正是你前面描述里一直在使用的导出目录。
DEFAULT_SOURCE_DIR = Path.home() / "Desktop" / "动画"

# 默认输出目录：先输出到 tmp，方便检查结果，避免直接覆盖正式资源。
DEFAULT_OUTPUT_DIR = REPO_ROOT / "tmp" / "dragonbones_runtime_export"

# 默认备份目录：只有在 --install-to-project 时才会真正用到。
DEFAULT_BACKUP_ROOT = PROJECT_DRAGONBONES_DIR / "_backup"

# 项目当前正式使用的 sk2.json 模板。
# 之所以叫模板，是因为我们转换的时候并不是完全照搬 DragonBones 导出结果，
# 而是需要借用项目现成资源中的部分结构，特别是 playerSmall。
TEMPLATE_SKELETON_PATH = PROJECT_DRAGONBONES_DIR / "sk2.json"

# 运行时固定使用的三个目标文件名。
RUNTIME_SKELETON_NAME = "sk2.json"
RUNTIME_TEXTURE_JSON_NAME = "texture2.json"
RUNTIME_TEXTURE_PNG_NAME = "texture2.png"


def parse_args() -> argparse.Namespace:
    """解析命令行参数。"""

    parser = argparse.ArgumentParser(
        description="把 DragonBones 编辑器导出文件转换成 Moon Lantern Park 项目可直接使用的运行时文件。",
    )
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=DEFAULT_SOURCE_DIR,
        help="导出源目录。目录里需要包含 *_ske.json / *_tex.json / *_tex.png 三个文件。",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="输出目录。未使用 --install-to-project 时，会把生成的 sk2.json / texture2.json / texture2.png 写到这里。",
    )
    parser.add_argument(
        "--base-name",
        default=None,
        help="显式指定导出基础名，例如 NewProject_3。脚本会据此寻找 NewProject_3_ske.json 等文件。",
    )
    parser.add_argument(
        "--install-to-project",
        action="store_true",
        help="把转换后的文件直接写入 Assets/mlp/Resources/mlp/DragonBones，并在覆盖前自动备份旧文件。",
    )
    parser.add_argument(
        "--backup-dir",
        type=Path,
        default=DEFAULT_BACKUP_ROOT,
        help="安装到项目时使用的备份根目录。默认是 Assets/mlp/Resources/mlp/DragonBones/_backup。",
    )
    return parser.parse_args()


def load_json(path: Path) -> dict[str, Any]:
    """读取 JSON 文件并解析成 Python 字典。"""

    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_json(path: Path, payload: dict[str, Any]) -> None:
    """把 Python 字典写回 JSON 文件。"""

    # 确保目标目录存在。
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as handle:
        # ensure_ascii=False：保留中文；
        # separators：输出更紧凑，避免产生大量无意义空格。
        json.dump(payload, handle, ensure_ascii=False, separators=(",", ":"))


def compact_float(value: float) -> int | float:
    """
    压缩浮点数表示，避免输出太长的小数。

    例如：
    - 12.0 -> 12
    - 3.1415926 -> 3.1416
    """

    # 先统一保留到 4 位小数，动画数据已经足够用了。
    rounded = round(float(value), 4)

    # 如果四舍五入后等价于整数，就直接输出整数。
    if abs(rounded - int(rounded)) < 0.00001:
        return int(rounded)
    return rounded


def legacy_duration(frame: dict[str, Any]) -> int:
    """
    读取旧版 DragonBones 帧的 duration，并统一兜底成非负整数。
    """

    return max(0, int(frame.get("duration", 0) or 0))


def legacy_frame_starts(frames: list[dict[str, Any]]) -> list[int]:
    """
    计算每个关键帧在整条时间轴上的起始时间。

    例如 duration 为 [3, 2, 5]，
    对应起始时间就是 [0, 3, 5]。
    """

    starts: list[int] = []
    current = 0
    for frame in frames:
        starts.append(current)
        current += legacy_duration(frame)
    return starts


def has_unreachable_terminal_key(frames: list[dict[str, Any]], animation_duration: int) -> bool:
    """
    判断最后一个关键帧是不是“不可到达的循环尾帧”。

    这种情况多见于循环动画导出：
    - 最后一帧 duration 为 0
    - 它的起始位置已经等于或超过动画总时长

    这种尾帧通常只是为了在编辑器里闭环显示，
    运行时实际上不会真正播到它。
    """

    if not frames:
        return False

    starts = legacy_frame_starts(frames)
    return legacy_duration(frames[-1]) <= 0 and starts[-1] >= animation_duration


def trim_terminal_loop_key(frames: list[dict[str, Any]], animation_duration: int) -> list[dict[str, Any]]:
    """
    如果最后一帧只是不可到达的循环补帧，就把它裁掉。
    """

    if len(frames) > 1 and has_unreachable_terminal_key(frames, animation_duration):
        return frames[:-1]
    return frames


def choose_output_frame_rate(source_skeleton: dict[str, Any], template_skeleton: dict[str, Any]) -> int:
    """
    选择输出 skeleton 的 frameRate。

    规则：
    1. 优先统计源 skeleton 中各 armature 的 frameRate，取出现次数最多的值。
    2. 如果源数据没有可靠 frameRate，就回退到模板 skeleton 的 frameRate。
    3. 最后再兜底为 30。
    """

    frame_rates = []
    for armature in source_skeleton.get("armature", []):
        rate = armature.get("frameRate")
        if isinstance(rate, int) and rate > 0:
            frame_rates.append(rate)

    if frame_rates:
        return Counter(frame_rates).most_common(1)[0][0]

    template_rate = template_skeleton.get("frameRate")
    if isinstance(template_rate, int) and template_rate > 0:
        return template_rate

    return 30


def convert_static_transform(transform: dict[str, Any] | None) -> dict[str, Any]:
    """
    转换静态 transform 数据。

    这里只保留运行时真正关心的：
    - x / y
    - skX / skY（或由 rotate 推导）
    - scX / scY

    并且会把默认值清掉，避免 JSON 变得很臃肿。
    """

    if not isinstance(transform, dict):
        return {}

    result: dict[str, Any] = {}

    # 位置：默认是 0，只有真的偏移了才写入。
    x = transform.get("x", 0)
    y = transform.get("y", 0)
    if abs(float(x)) > 0.00001:
        result["x"] = compact_float(x)
    if abs(float(y)) > 0.00001:
        result["y"] = compact_float(y)

    # 旋转：有些数据直接给 skX，有些只给 rotate。
    rotation = None
    if "skX" in transform:
        rotation = float(transform.get("skX", 0) or 0)
    elif "rotate" in transform:
        rotation = float(transform.get("rotate", 0) or 0)

    # 输出时把 skX 和 skY 保持一致，符合项目当前资源格式。
    if rotation is not None and abs(rotation) > 0.00001:
        result["skX"] = compact_float(rotation)
        result["skY"] = compact_float(rotation)

    # 缩放：默认值是 1，只有改动过才写。
    scale_x = float(transform.get("scX", 1) or 1)
    scale_y = float(transform.get("scY", 1) or 1)
    if abs(scale_x - 1.0) > 0.00001:
        result["scX"] = compact_float(scale_x)
    if abs(scale_y - 1.0) > 0.00001:
        result["scY"] = compact_float(scale_y)

    return result


def build_translate_frame(source_frame: dict[str, Any]) -> dict[str, Any]:
    """
    把旧版骨骼关键帧转换成平移帧（translateFrame）。
    """

    transform = source_frame.get("transform", {}) or {}
    result: dict[str, Any] = {}

    # duration 为 0 时不写，保持输出更紧凑。
    duration = legacy_duration(source_frame)
    if duration > 0:
        result["duration"] = duration

    # 只提取平移信息。
    x = float(transform.get("x", 0) or 0)
    y = float(transform.get("y", 0) or 0)
    if abs(x) > 0.00001:
        result["x"] = compact_float(x)
    if abs(y) > 0.00001:
        result["y"] = compact_float(y)

    # tweenEasing 为 None 时需要保留，表示明确关闭插值。
    if "tweenEasing" in source_frame and source_frame["tweenEasing"] is None:
        result["tweenEasing"] = None

    return result


def build_rotate_frame(source_frame: dict[str, Any]) -> dict[str, Any]:
    """
    把旧版骨骼关键帧转换成旋转帧（rotateFrame）。
    """

    transform = source_frame.get("transform", {}) or {}
    result: dict[str, Any] = {}
    duration = legacy_duration(source_frame)
    if duration > 0:
        result["duration"] = duration

    # 旧格式有两种常见写法：
    # 1. 直接给 skX
    # 2. 用 rotate + skew 组合
    if "skX" in transform:
        rotation = float(transform.get("skX", 0) or 0)
    else:
        rotation = float(transform.get("rotate", 0) or 0) + float(transform.get("skew", 0) or 0)

    if abs(rotation) > 0.00001:
        result["rotate"] = compact_float(rotation)

    if "tweenEasing" in source_frame and source_frame["tweenEasing"] is None:
        result["tweenEasing"] = None

    return result


def build_scale_frame(source_frame: dict[str, Any]) -> dict[str, Any]:
    """
    把旧版骨骼关键帧转换成缩放帧（scaleFrame）。
    """

    transform = source_frame.get("transform", {}) or {}
    result: dict[str, Any] = {}
    duration = legacy_duration(source_frame)
    if duration > 0:
        result["duration"] = duration

    # 注意：目标运行时里的 scaleFrame 字段名是 x / y，
    # 不是 scX / scY。
    scale_x = float(transform.get("scX", 1) or 1)
    scale_y = float(transform.get("scY", 1) or 1)
    if abs(scale_x - 1.0) > 0.00001:
        result["x"] = compact_float(scale_x)
    if abs(scale_y - 1.0) > 0.00001:
        result["y"] = compact_float(scale_y)

    if "tweenEasing" in source_frame and source_frame["tweenEasing"] is None:
        result["tweenEasing"] = None

    return result


def convert_bone_track(source_track: dict[str, Any], animation_duration: int, loops: bool) -> dict[str, Any] | None:
    """
    转换一条骨骼轨道。

    旧版 DragonBones 轨道常把位移、旋转、缩放都混在一组 frame 里，
    而这个项目运行时要分别读取：
    - translateFrame
    - rotateFrame
    - scaleFrame

    所以这里会把一条旧轨道拆成三条“并列帧列表”。
    """

    frames = source_track.get("frame", []) or []
    if not frames:
        return None

    # 对循环动画，先去掉不可到达的尾部补帧。
    frames = trim_terminal_loop_key(frames, animation_duration) if loops else list(frames)

    track: dict[str, Any] = {"name": source_track["name"]}
    track["translateFrame"] = [build_translate_frame(frame) for frame in frames]
    track["rotateFrame"] = [build_rotate_frame(frame) for frame in frames]
    track["scaleFrame"] = [build_scale_frame(frame) for frame in frames]
    return track


def convert_slot_track(source_track: dict[str, Any], animation_duration: int, loops: bool) -> dict[str, Any] | None:
    """
    转换一条插槽轨道。

    这里主要处理：
    - displayIndex 切换 -> displayFrame
    - 透明度颜色信息 -> colorFrame
    """

    frames = source_track.get("frame", []) or []
    if not frames:
        return None

    frames = trim_terminal_loop_key(frames, animation_duration) if loops else list(frames)

    track: dict[str, Any] = {"name": source_track["name"]}
    display_frames: list[dict[str, Any]] = []

    # 只有存在颜色动画时，才生成 colorFrame。
    has_color = any("color" in frame for frame in frames)
    color_frames: list[dict[str, Any]] = []

    for frame in frames:
        display_entry: dict[str, Any] = {}
        duration = legacy_duration(frame)
        if duration > 0:
            display_entry["duration"] = duration

        # value 表示显示第几个 display。
        display_entry["value"] = int(frame.get("displayIndex", 0) or 0)
        display_frames.append(display_entry)

        if has_color:
            color_entry: dict[str, Any] = {}
            if duration > 0:
                color_entry["duration"] = duration

            color_data = frame.get("color", {}) or {}

            # 目前项目运行时实际用到的是透明度倍率 aM，
            # 所以这里先只保留它。
            color_entry["value"] = {"aM": int(color_data.get("aM", 100) or 100)}
            color_frames.append(color_entry)

    track["displayFrame"] = display_frames
    if has_color:
        track["colorFrame"] = color_frames

    return track


def convert_event_frames(source_frames: list[dict[str, Any]], animation_duration: int, loops: bool) -> list[dict[str, Any]]:
    """
    转换动画级别的事件帧。

    旧数据里事件名可能存放在：
    - frame["events"][0]["name"]
    - frame["event"]
    """

    if not source_frames:
        return []

    frames = trim_terminal_loop_key(source_frames, animation_duration) if loops else list(source_frames)
    converted: list[dict[str, Any]] = []

    for frame in frames:
        event_name = None

        events = frame.get("events", []) or []
        if events:
            first_event = events[0] or {}
            event_name = first_event.get("name")
        elif "event" in frame:
            event_name = frame.get("event")

        # 既没有事件名，又没有实际时长的帧，对运行时没有意义。
        if not event_name:
            duration = legacy_duration(frame)
            if duration <= 0:
                continue

        entry: dict[str, Any] = {}
        duration = legacy_duration(frame)
        if duration > 0:
            entry["duration"] = duration
        if event_name:
            entry["event"] = event_name

        converted.append(entry)

    return converted


def adjust_animation_duration(source_animation: dict[str, Any], loops: bool) -> int:
    """
    修正动画总时长。

    对非循环动画来说，旧导出有时会出现：
    - 最后一帧的起始点已经到达 duration
    - 但最后一帧 duration 是 0

    这样如果不修，最后一帧其实永远播不到。
    所以这里会把时长延长 1 帧，把最后关键帧纳入可播放范围。
    """

    # 动画时长最少保留 1，避免出现 0 帧动画。
    duration = max(1, int(source_animation.get("duration", 1) or 1))
    if loops:
        return duration

    # 骨骼轨道、插槽轨道、动画级事件帧都要检查。
    sequences: list[list[dict[str, Any]]] = []
    sequences.extend(track.get("frame", []) or [] for track in (source_animation.get("bone", []) or []))
    sequences.extend(track.get("frame", []) or [] for track in (source_animation.get("slot", []) or []))
    sequences.append(source_animation.get("frame", []) or [])

    for frames in sequences:
        if not frames:
            continue

        starts = legacy_frame_starts(frames)
        if legacy_duration(frames[-1]) <= 0 and starts[-1] >= duration:
            duration = starts[-1] + 1

    return duration


def convert_animation(source_animation: dict[str, Any]) -> dict[str, Any]:
    """
    转换单个 animation 节点。

    这里会统一处理：
    - playTimes
    - fadeInTime
    - frame（事件帧）
    - bone（骨骼轨道）
    - slot（插槽轨道）
    """

    # playTimes = 0 表示无限循环。
    play_times_raw = source_animation.get("playTimes", 1)
    play_times = 1 if play_times_raw is None else int(play_times_raw)
    loops = play_times == 0
    animation_duration = adjust_animation_duration(source_animation, loops)

    converted: dict[str, Any] = {
        "name": source_animation["name"],
        "duration": animation_duration,
        "playTimes": 0 if loops else play_times,
    }

    if "fadeInTime" in source_animation:
        converted["fadeInTime"] = compact_float(source_animation.get("fadeInTime", 0))

    # 先处理动画根级别事件帧。
    event_frames = convert_event_frames(source_animation.get("frame", []) or [], animation_duration, loops)
    if event_frames:
        converted["frame"] = event_frames

    # 再处理骨骼轨道。
    bone_tracks: list[dict[str, Any]] = []
    for source_track in source_animation.get("bone", []) or []:
        converted_track = convert_bone_track(source_track, animation_duration, loops)
        if converted_track is not None:
            bone_tracks.append(converted_track)
    if bone_tracks:
        converted["bone"] = bone_tracks

    # 最后处理插槽轨道。
    slot_tracks: list[dict[str, Any]] = []
    for source_track in source_animation.get("slot", []) or []:
        converted_track = convert_slot_track(source_track, animation_duration, loops)
        if converted_track is not None:
            slot_tracks.append(converted_track)
    if slot_tracks:
        converted["slot"] = slot_tracks

    return converted


def convert_bone(source_bone: dict[str, Any]) -> dict[str, Any]:
    """转换骨骼静态定义。"""

    converted: dict[str, Any] = {
        "inheritScale": bool(source_bone.get("inheritScale", False)),
        "name": source_bone["name"],
    }

    if source_bone.get("parent"):
        converted["parent"] = source_bone["parent"]

    # transform 会被精简后写入。
    transform = convert_static_transform(source_bone.get("transform"))
    if transform:
        converted["transform"] = transform

    return converted


def convert_slot_definition(source_slot: dict[str, Any]) -> dict[str, Any]:
    """
    转换插槽静态定义。

    parent 表示这个 slot 挂在哪个 bone 下。
    """

    converted: dict[str, Any] = {
        "name": source_slot["name"],
        "parent": source_slot.get("parent", source_slot["name"]),
    }

    display_index = int(source_slot.get("displayIndex", 0) or 0)

    # 默认 displayIndex 为 0，只有不是 0 时才写出。
    if display_index != 0:
        converted["displayIndex"] = display_index

    return converted


def convert_display(source_display: dict[str, Any]) -> dict[str, Any]:
    """
    转换 skin 里的单个 display。

    name / path 对应资源名；
    type 可能是 image、armature 等；
    transform 是这个 display 的局部偏移。
    """

    # DragonBones 有时把名字写在 name，有时写在 path。
    name = source_display.get("name") or source_display.get("path")
    if not name:
        raise ValueError("Display is missing both 'name' and 'path'.")

    converted: dict[str, Any] = {"name": name}
    display_type = source_display.get("type", "image") or "image"

    # image 是默认类型，其他类型才显式写出。
    if display_type != "image":
        converted["type"] = display_type

    transform = convert_static_transform(source_display.get("transform"))
    if transform:
        converted["transform"] = transform

    return converted


def convert_skin(source_skin: dict[str, Any]) -> dict[str, Any]:
    """
    转换一个 skin。

    这里会遍历 skin 下所有 slot，并把各自的 display 列表整理出来。
    """

    converted_slots = []
    for source_slot in source_skin.get("slot", []) or []:
        slot_entry = {"name": source_slot["name"]}
        slot_entry["display"] = [convert_display(display) for display in source_slot.get("display", []) or []]
        converted_slots.append(slot_entry)

    return {"slot": converted_slots}


def find_armature(armatures: list[dict[str, Any]], name: str) -> dict[str, Any]:
    """按名字查找 armature，找不到就抛错。"""

    for armature in armatures:
        if armature.get("name") == name:
            return armature
    raise ValueError(f"Could not find armature '{name}'.")


def convert_armature(source_armature: dict[str, Any], template_player: dict[str, Any] | None) -> dict[str, Any]:
    """
    转换单个 armature。

    这里最关键的特殊逻辑是 `playerSmall`：

    - 对普通 armature：
      直接按 DragonBones 导出数据转换。

    - 对 `playerSmall`：
      不直接使用导出结果里的 bone / slot / skin，
      而是强制沿用项目模板中的这三部分结构。

    原因是 `playerSmall` 在游戏里承担的是“角色总装配骨架”职责，
    代码会通过 head / body / hand / leg 这些 slot 找它下面挂的子骨架，
    再动态切换子动画。如果这里被普通导出数据替换掉，
    运行时代码就找不到原本预期的子骨架结构了。
    """

    converted: dict[str, Any] = {
        "name": source_armature["name"],
    }

    if source_armature["name"] == "playerSmall" and template_player is not None:
        # 对 playerSmall，保留项目模板中的静态结构。
        converted["bone"] = copy.deepcopy(template_player.get("bone", []))
        converted["slot"] = copy.deepcopy(template_player.get("slot", []))
        converted["skin"] = copy.deepcopy(template_player.get("skin", []))
    else:
        # 普通 armature 直接按导出数据转换即可。
        bones = [convert_bone(source_bone) for source_bone in source_armature.get("bone", []) or []]
        if bones:
            converted["bone"] = bones

        slots = [convert_slot_definition(source_slot) for source_slot in source_armature.get("slot", []) or []]
        if slots:
            converted["slot"] = slots

        source_skins = source_armature.get("skin", []) or []
        if source_skins:
            # 目标运行时当前只使用第一个 skin。
            converted["skin"] = [convert_skin(source_skins[0])]

    # 不管是不是 playerSmall，动画都采用你最新从编辑器里导出的内容。
    animations = [convert_animation(animation) for animation in source_armature.get("animation", []) or []]
    if animations:
        converted["animation"] = animations

    return converted


def convert_skeleton(source_skeleton: dict[str, Any], template_skeleton: dict[str, Any]) -> dict[str, Any]:
    """
    转换整个 skeleton 文件，生成最终 sk2.json 的内容。
    """

    source_armatures = source_skeleton.get("armature", []) or []
    if not source_armatures:
        raise ValueError("The source skeleton does not contain any armatures.")

    # 从模板中找出 playerSmall，以便后面对它做特殊保留。
    template_player = find_armature(template_skeleton.get("armature", []) or [], "playerSmall")

    converted_armatures = [
        convert_armature(source_armature, template_player)
        for source_armature in source_armatures
    ]

    return {
        # frameRate 优先参考源数据，源数据不可靠时再回退模板。
        "frameRate": choose_output_frame_rate(source_skeleton, template_skeleton),

        # 这些顶层元信息尽量沿用项目当前模板，减少运行时差异。
        "name": template_skeleton.get("name", "MoonLanternPark_RuntimeDB"),
        "version": template_skeleton.get("version", "5.5"),
        "compatibleVersion": template_skeleton.get("compatibleVersion", "5.5"),

        # userData 仅作为辅助说明，方便以后回看文件来源。
        "userData": {
            "generator": "Tools/Art/convert_dragonbones_export_for_runtime.py",
            "note": "已由脚本把 DragonBones 编辑器导出格式转换为 DBLite 运行时兼容格式。",
        },
        "armature": converted_armatures,
    }


def convert_texture_json(source_texture: dict[str, Any], runtime_name: str) -> dict[str, Any]:
    """
    转换纹理图集 JSON。

    这里主要做三件事：
    1. name 改成和最终 skeleton 一致
    2. imagePath 固定成运行时文件名 texture2.png
    3. 保留或补齐 pixelsPerUnit
    """

    converted = dict(source_texture)
    converted["name"] = runtime_name
    converted["imagePath"] = RUNTIME_TEXTURE_PNG_NAME
    converted["pixelsPerUnit"] = float(source_texture.get("pixelsPerUnit", 1.0) or 1.0)
    return converted


def derive_base_name(skeleton_path: Path) -> str:
    """
    从 `xxx_ske.json` 推导出基础名 `xxx`。
    """

    if not skeleton_path.name.endswith("_ske.json"):
        raise ValueError(f"Unexpected skeleton filename: {skeleton_path.name}")
    return skeleton_path.name[:-len("_ske.json")]


def find_export_set(source_dir: Path, explicit_base_name: str | None) -> tuple[str, Path, Path, Path]:
    """
    在源目录里寻找一套完整的 DragonBones 导出文件。

    返回：
    - 基础名
    - skeleton 路径
    - texture json 路径
    - texture png 路径
    """

    if not source_dir.exists():
        raise FileNotFoundError(f"Source directory does not exist: {source_dir}")

    if explicit_base_name:
        # 指定了基础名时，只匹配这一套。
        candidates = list(source_dir.rglob(f"{explicit_base_name}_ske.json"))
    else:
        # 没指定基础名时，按“最近修改时间”从新到旧找。
        candidates = sorted(
            source_dir.rglob("*_ske.json"),
            key=lambda path: path.stat().st_mtime,
            reverse=True,
        )

    for skeleton_path in candidates:
        base_name = derive_base_name(skeleton_path)
        texture_json_path = skeleton_path.with_name(f"{base_name}_tex.json")
        texture_png_path = skeleton_path.with_name(f"{base_name}_tex.png")

        # 必须三件套都存在，才算完整导出。
        if texture_json_path.exists() and texture_png_path.exists():
            return base_name, skeleton_path, texture_json_path, texture_png_path

    raise FileNotFoundError(
        "Could not find a complete DragonBones export set. Expected "
        "<base>_ske.json, <base>_tex.json, and <base>_tex.png in the source directory."
    )


def ensure_unique_backup_dir(backup_root: Path) -> Path:
    """
    生成一个不会冲突的备份目录。

    目录名格式：
    - YYYYMMDD_HHMMSS
    - YYYYMMDD_HHMMSS_02
    - YYYYMMDD_HHMMSS_03
    """

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    candidate = backup_root / timestamp
    suffix = 2

    while candidate.exists():
        candidate = backup_root / f"{timestamp}_{suffix:02d}"
        suffix += 1

    return candidate


def backup_existing_runtime_files(target_dir: Path, backup_root: Path) -> Path | None:
    """
    备份项目当前正式资源文件。

    只有在目标文件确实存在时才会创建备份目录。
    如果目标文件一个都不存在，就返回 None。
    """

    existing_files = [
        target_dir / RUNTIME_SKELETON_NAME,
        target_dir / RUNTIME_TEXTURE_JSON_NAME,
        target_dir / RUNTIME_TEXTURE_PNG_NAME,
    ]
    existing_files = [path for path in existing_files if path.exists()]

    if not existing_files:
        return None

    backup_root.mkdir(parents=True, exist_ok=True)
    backup_dir = ensure_unique_backup_dir(backup_root)
    backup_dir.mkdir(parents=True, exist_ok=False)

    for path in existing_files:
        # 使用 copy2 保留文件时间等元信息，回滚时更稳妥。
        shutil.copy2(path, backup_dir / path.name)

    return backup_dir


def main() -> None:
    """脚本主入口。"""

    # 1. 解析命令行参数。
    args = parse_args()

    # 2. 展开并标准化路径，避免相对路径带来的歧义。
    source_dir = args.source_dir.expanduser().resolve()
    output_dir = PROJECT_DRAGONBONES_DIR if args.install_to_project else args.output_dir.expanduser().resolve()
    backup_root = args.backup_dir.expanduser().resolve()

    # 3. 自动定位 DragonBones 三件套源文件。
    base_name, skeleton_path, texture_json_path, texture_png_path = find_export_set(source_dir, args.base_name)

    # 4. 读取模板和导出数据。
    template_skeleton = load_json(TEMPLATE_SKELETON_PATH)
    source_skeleton = load_json(skeleton_path)
    source_texture = load_json(texture_json_path)

    # 5. 执行核心转换。
    converted_skeleton = convert_skeleton(source_skeleton, template_skeleton)
    converted_texture = convert_texture_json(source_texture, converted_skeleton["name"])

    # 6. 如果要直接安装到项目，就先备份旧资源。
    backup_dir = None
    if args.install_to_project:
        backup_dir = backup_existing_runtime_files(output_dir, backup_root)

    # 7. 准备最终输出路径。
    output_dir.mkdir(parents=True, exist_ok=True)
    skeleton_output_path = output_dir / RUNTIME_SKELETON_NAME
    texture_json_output_path = output_dir / RUNTIME_TEXTURE_JSON_NAME
    texture_png_output_path = output_dir / RUNTIME_TEXTURE_PNG_NAME

    # 8. 写出两个 JSON，并复制 PNG。
    write_json(skeleton_output_path, converted_skeleton)
    write_json(texture_json_output_path, converted_texture)
    shutil.copy2(texture_png_path, texture_png_output_path)

    # 9. 在控制台打印清晰结果，方便你确认本次到底读了什么、写到了哪里。
    print(f"源导出基础名: {base_name}")
    print(f"骨骼输入文件: {skeleton_path}")
    print(f"纹理输入文件: {texture_json_path}")
    print(f"图片输入文件: {texture_png_path}")
    if backup_dir is not None:
        print(f"旧资源备份目录: {backup_dir}")
    elif args.install_to_project:
        print("旧资源备份目录: 本次未创建（目标目录中没有旧文件可备份）")
    print(f"骨骼输出文件: {skeleton_output_path}")
    print(f"纹理输出文件: {texture_json_output_path}")
    print(f"图片输出文件: {texture_png_output_path}")


if __name__ == "__main__":
    # 只有脚本被直接执行时，才会进入 main。
    main()
