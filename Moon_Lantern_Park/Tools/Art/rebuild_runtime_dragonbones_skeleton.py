"""
DragonBones运行时骨骼数据生成脚本

本脚本负责程序化生成游戏中角色动画系统的完整骨骼数据（sk2.json）。
与纹理图集构建脚本不同，本脚本不处理图片资源，而是生成纯数据结构的
JSON文件，定义了DragonBones骨骼动画系统所需的全部骨骼、插槽、皮肤和动画。

生成的骨骼数据结构：
1. 主骨骼（playerSmall）：包含所有角色共享的骨骼树、插槽配置和42个动画
2. 角色部件选择器骨骼（7个）：头、身体、腿、左手、右手、挖掘手、挖掘腿
3. 特效骨骼（7个）：背风动画、眩晕眼睛、眼泪、奖杯、火焰、护盾（2个）

主骨骼支持的动画类型：
- 基础移动：idle（站立）、run（跑步）、idle_wb（持球站立）、run_wb（持球跑步）
- 跳跃系列：jump、fly1~fly5、landing（及其持球变体 _wb）
- 投篮：throw_land（投篮落地）
- 扣篮：dunk1/dunk2/dunk3（三种扣篮）、megadunk系列
- 防守：steal（抢断）、pumpStart/pumpEnd（假动作）、blockStart/blockEnd（盖帽）
- 特殊：dash/dash_wb（冲刺）、dig1/dig2/dig3（挖球）、stun（眩晕）
- 情绪：sad/sad0（悲伤）、happiness（开心）
- 超级扣篮：md_start/md_mid/md_end系列（及其持球变体 _wb）

运行时名称（RUNTIME_NAME）与texture2.json中的name字段保持一致，
确保DragonBones运行时能正确关联骨骼数据和纹理图集。

依赖：无外部依赖（纯Python标准库）
"""
from __future__ import annotations

import json
from pathlib import Path


# 项目根目录
REPO_ROOT = Path(__file__).resolve().parents[2]

# DragonBones资源目录
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "DragonBones"

# 输出文件路径
SKELETON_PATH = DRAGON_BONES_DIR / "sk2.json"           # 骨骼数据输出
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"   # 纹理图集JSON（用于验证和更新）

# 运行时名称，与texture2.json中的name字段一致
RUNTIME_NAME = "MoonLanternPark_RuntimeDB"

# 骨骼动画帧率（帧/秒）
FRAME_RATE = 30

# 所有角色的内部标识符列表
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

# 主骨骼（playerSmall）必须包含的动画名称列表
# 用于验证生成的骨骼数据是否完整
MAIN_ANIMATIONS = (
    "idle",          # 无球站立
    "run",           # 无球跑步
    "idle_wb",       # 持球站立
    "run_wb",        # 持球跑步
    "throw_land",    # 投篮落地
    "jump",          # 起跳（无球）
    "fly1", "fly2", "fly3", "fly4", "fly5",  # 空中飞行阶段（无球）
    "landing",       # 落地（无球）
    "jump_wb",       # 起跳（持球）
    "fly1_wb", "fly2_wb", "fly3_wb", "fly4_wb", "fly5_wb",  # 空中飞行（持球）
    "landing_wb",    # 落地（持球）
    "dunk1", "dunk2", "dunk3",  # 三种扣篮
    "steal",         # 抢断
    "pumpStart",     # 假动作开始
    "pumpEnd",       # 假动作结束
    "dash_wb",       # 持球冲刺
    "dash",          # 无球冲刺
    "dig1", "dig2", "dig3",  # 挖球三阶段
    "stun",          # 眩晕
    "sad", "sad0",   # 悲伤情绪（两种变体）
    "happiness",     # 开心情绪
    "blockStart",    # 盖帽开始
    "blockEnd",      # 盖帽结束
    "megadunk",      # 超级扣篮
    "megadunk_fly",  # 超级扣篮飞行
    "megadunk_end",  # 超级扣篮结束
    "md_start_wb",   # 超级扣篮起始（持球）
    "md_mid_wb",     # 超级扣篮中间（持球）
    "md_end_wb",     # 超级扣篮结束（持球）
    "md_start",      # 超级扣篮起始（无球）
    "md_mid",        # 超级扣篮中间（无球）
    "md_end",        # 超级扣篮结束（无球）
)


# ============================================================
# 基础数据构建辅助函数
# ============================================================

def compact_float(value: float) -> int | float:
    """
    将浮点数紧凑化：如果小数部分接近0则转为整数

    这样可以减小JSON文件体积（整数不带小数点），
    同时避免不必要的精度损失。

    参数:
        value: 输入浮点数

    返回:
        整数或保留4位小数的浮点数
    """
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
    """
    构建DragonBones变换数据字典

    只包含非默认值的字段，减小JSON体积。默认值定义：
    - 位移：x=0, y=0
    - 旋转：rotate=0（同时设置skX和skY）
    - 缩放：scale_x=1.0, scale_y=1.0

    参数:
        x: 水平位移
        y: 垂直位移
        rotate: 旋转角度（度）
        scale_x: 水平缩放
        scale_y: 垂直缩放

    返回:
        变换数据字典（仅包含非默认值的键）
    """
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
    """
    构建骨骼定义数据

    每个骨骼代表骨骼树中的一个节点，有名称、初始位置和旋转。
    所有骨骼都禁用继承父级缩放（inheritScale=False），
    以避免父级动画对子级产生意外的缩放叠加。

    参数:
        name: 骨骼名称
        x: 初始X坐标（相对于父级）
        y: 初始Y坐标（相对于父级）
        rotate: 初始旋转角度

    返回:
        骨骼定义字典
    """
    item: dict[str, object] = {"inheritScale": False, "name": name}
    tf = transform(x=x, y=y, rotate=rotate)
    if tf:
        item["transform"] = tf
    return item


def slot(name: str, parent: str | None = None, display_index: int = 0) -> dict:
    """
    构建插槽定义数据

    插槽是骨骼树上用于显示可视内容（图片或子骨骼）的挂载点。
    每个插槽绑定到同名骨骼（或指定的父级骨骼），
    display_index控制初始显示哪个内容（-1表示隐藏）。

    参数:
        name: 插槽名称
        parent: 父级骨骼名称（默认与插槽同名）
        display_index: 初始显示索引（-1隐藏, 0显示第一个）

    返回:
        插槽定义字典
    """
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
    """
    构建显示对象定义

    显示对象是插槽中实际显示的内容。type_参数决定显示类型：
    - "image"：显示纹理图集中的图片（默认）
    - "armature"：显示子骨骼动画（用于部件选择器和特效）

    参数:
        name: 显示对象名称（对应纹理图集中的图片名或子骨骼名）
        type_: 显示类型
        x/y/rotate/scale_x/scale_y: 变换参数

    返回:
        显示对象定义字典
    """
    item: dict[str, object] = {"name": name}
    if type_ != "image":
        item["type"] = type_
    tf = transform(x=x, y=y, rotate=rotate, scale_x=scale_x, scale_y=scale_y)
    if tf:
        item["transform"] = tf
    return item


def skin_slot(name: str, displays: list[dict]) -> dict:
    """
    构建皮肤插槽数据

    皮肤插槽定义了一个插槽可以显示的所有候选项。
    运行时通过切换display_index来选择显示哪个候选项。

    参数:
        name: 插槽名称
        displays: 候选显示对象列表

    返回:
        皮肤插槽字典
    """
    return {"name": name, "display": displays}


def frame(duration: int = 1, **values: object) -> dict:
    """
    构建动画关键帧

    每个关键帧包含持续时间（帧数）和该帧的属性值。
    duration=1表示该帧持续1帧（即30fps下的1/30秒）。
    通过**values传入该帧需要设置的任意属性。

    参数:
        duration: 帧持续时间（帧数）
        **values: 该帧的属性键值对

    返回:
        关键帧字典
    """
    item: dict[str, object] = {}
    if duration != 1:
        item["duration"] = duration
    for key, value in values.items():
        if value is not None:
            item[key] = value
    return item


# ============================================================
# 关键帧序列构建函数
# ============================================================

def translate_frames(*items: tuple[int, float, float]) -> list[dict]:
    """
    构建位移关键帧序列

    每个元组包含 (持续时间, X位移, Y位移)。

    参数:
        items: 位移关键帧数据元组

    返回:
        位移关键帧列表
    """
    return [frame(duration, x=compact_float(x), y=compact_float(y)) for duration, x, y in items]


def rotate_frames(*items: tuple[int, float]) -> list[dict]:
    """
    构建旋转关键帧序列

    每个元组包含 (持续时间, 旋转角度)。

    参数:
        items: 旋转关键帧数据元组

    返回:
        旋转关键帧列表
    """
    return [frame(duration, rotate=compact_float(angle)) for duration, angle in items]


def scale_frames(*items: tuple[int, float, float]) -> list[dict]:
    """
    构建缩放关键帧序列

    每个元组包含 (持续时间, X缩放, Y缩放)。

    参数:
        items: 缩放关键帧数据元组

    返回:
        缩放关键帧列表
    """
    return [frame(duration, x=compact_float(x), y=compact_float(y)) for duration, x, y in items]


# ============================================================
# 动画轨道和动画构建函数
# ============================================================

def track(
    name: str,
    *,
    translate: list[dict] | None = None,
    rotate: list[dict] | None = None,
    scale: list[dict] | None = None,
) -> dict:
    """
    构建骨骼动画轨道

    每个轨道控制一个骨骼在动画中的变换序列。
    一个轨道可以同时包含位移、旋转和缩放关键帧。

    参数:
        name: 目标骨骼名称
        translate: 位移关键帧列表
        rotate: 旋转关键帧列表
        scale: 缩放关键帧列表

    返回:
        骨骼动画轨道字典
    """
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
    """
    构建插槽动画轨道

    插槽轨道控制插槽的显示状态和透明度。
    display_value=-1表示隐藏，0表示显示第一个候选项。

    参数:
        name: 目标插槽名称
        display_value: 显示索引（-1隐藏, 0+显示对应候选项）
        duration: 帧持续时间
        alpha: 透明度（0-255）

    返回:
        插槽动画轨道字典
    """
    item: dict[str, object] = {"name": name}
    if display_value is not None:
        item["displayFrame"] = [frame(duration, value=display_value)]
    if alpha is not None:
        item["colorFrame"] = [frame(duration, value={"aM": alpha})]
    return item


def event_frames(duration: int, at_frame: int, event_name: str) -> list[dict]:
    """
    构建事件触发帧序列

    在动画的指定帧位置触发一个命名事件，游戏逻辑可以监听这些事件
    来执行对应的业务逻辑（如投篮、扣篮、抢断等）。

    事件帧序列的结构：先播放at_frame帧空白帧，然后在剩余帧中触发事件。

    参数:
        duration: 动画总帧数
        at_frame: 事件触发的帧位置（1-based）
        event_name: 事件名称

    返回:
        事件帧列表
    """
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
    """
    构建完整的动画定义

    每个动画包含名称、时长、是否循环、骨骼轨道、插槽轨道和事件帧。
    fadeInTime控制动画切换时的淡入过渡时间。

    参数:
        name: 动画名称
        duration: 动画总帧数
        loop: 是否循环播放（playTimes=0表示无限循环）
        bones: 骨骼动画轨道列表
        slots: 插槽动画轨道列表
        frames: 事件帧列表
        fade: 淡入过渡时间（秒）

    返回:
        动画定义字典
    """
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


# ============================================================
# 插槽配置辅助函数
# ============================================================

def hidden_optional_slots(duration: int) -> list[dict]:
    """
    构建所有可选插槽的默认隐藏配置

    游戏中角色有多个可选显示的插槽（球、眼睛、特效等），
    默认全部隐藏（display_index=-1）。各个动画函数会根据需要
    覆盖特定插槽的显示状态。

    隐藏的插槽列表（按顺序）：
    0. ball - 手持球
    1. ball_front - 前景球（扣篮时使用）
    2. eyes - 眩晕眼睛动画
    3. dighand - 挖掘手
    4. digleg - 挖掘腿
    5. effects stun - 眩晕特效
    6. effects stun star - 眩晕星星特效

    参数:
        duration: 帧持续时间

    返回:
        全部隐藏的插槽轨道列表
    """
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
    """
    构建持球状态的插槽配置

    基于默认隐藏配置，显示球插槽。front参数控制是否同时显示前景球
    （扣篮等需要球在角色前方的动画需要开启）。

    参数:
        duration: 帧持续时间
        front: 是否同时显示前景球

    返回:
        持球状态的插槽轨道列表
    """
    slots = hidden_optional_slots(duration)
    slots[0] = slot_track("ball", display_value=0, duration=duration)
    slots[1] = slot_track("ball_front", display_value=0 if front else -1, duration=duration)
    return slots


def loop_pose(duration: int, *, with_ball: bool) -> list[dict]:
    """
    构建循环姿态的插槽配置

    用于idle、run等循环动画。根据是否持球选择不同的插槽配置。

    参数:
        duration: 帧持续时间
        with_ball: 是否持球

    返回:
        循环姿态的插槽轨道列表
    """
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    return slots


# ============================================================
# Active main armature animation builders
# ============================================================

def player_armature_base() -> dict:
    """Return the minimal main armature shell for the active runtime data."""
    return {"name": "playerSmall"}


def event_slots(duration: int, visible_until: int, *, front: bool = False) -> list[dict]:
    slots = with_ball_slots(duration, front=front)
    visible_until = max(1, min(duration - 1, visible_until))
    hidden_after = max(1, duration - visible_until)
    slots[0] = {"name": "ball", "displayFrame": [frame(visible_until, value=0), frame(hidden_after, value=-1)]}
    if front:
        slots[1] = {"name": "ball_front", "displayFrame": [frame(visible_until, value=0), frame(hidden_after, value=-1)]}
    return slots


def custom_idle() -> dict:
    duration = 22
    bones = [
        track("body", translate=translate_frames((6, 0, 0), (5, 0.4, 1.8), (6, -0.3, 0.4), (5, 0, 0)), scale=scale_frames((6, 1, 1), (5, 0.99, 1.015), (6, 1.005, 0.995), (5, 1, 1))),
        track("head", translate=translate_frames((6, -0.4, -0.5), (5, 0.5, -2.2), (6, 0.2, -1.0), (5, -0.4, -0.5)), rotate=rotate_frames((6, -1.5), (5, 2.0), (6, 0.8), (5, -1.5))),
        track("left hand", translate=translate_frames((6, -1.2, 0.4), (5, -2.4, 2.3), (6, -0.6, 1.0), (5, -1.2, 0.4)), rotate=rotate_frames((6, 6), (5, 13), (6, 8), (5, 6))),
        track("right hand", translate=translate_frames((6, 1.3, 0.8), (5, 2.0, 2.0), (6, 0.8, 1.4), (5, 1.3, 0.8)), rotate=rotate_frames((6, -7), (5, -14), (6, -9), (5, -7))),
        track("left leg", translate=translate_frames((11, 0.5, 0.3), (11, 0, 0)), rotate=rotate_frames((11, -2), (11, 0))),
        track("right leg", translate=translate_frames((11, -0.5, 0), (11, 0, 0.2)), rotate=rotate_frames((11, 2), (11, 0))),
    ]
    return animation("idle", duration, loop=True, bones=bones, slots=loop_pose(duration, with_ball=False), fade=0.08)


def custom_run() -> dict:
    duration = 21
    bones = [
        track("body", translate=translate_frames((2, 1.4, -3.4), (2, 0.9, -1.8), (2, 0.2, 0.5), (2, 0.6, -1.6), (2, 1.3, -3.8), (2, 0.9, -1.7), (2, 0.2, 0.6), (2, 0.6, -1.5), (2, 1.4, -3.6), (2, 1.0, -1.9), (1, 1.5, -3.7)), rotate=rotate_frames((2, -4), (2, -2), (2, 1), (2, 3), (2, 4), (2, 2), (2, -1), (2, -3), (2, -4), (2, -2), (1, -4.5))),
        track("head", translate=translate_frames((2, 2.2, -6.6), (2, 1.7, -4.5), (2, 0.8, -1.8), (2, 1.2, -3.9), (2, 2.1, -6.8), (2, 1.6, -4.3), (2, 0.7, -1.7), (2, 1.2, -3.8), (2, 2.2, -6.7), (2, 1.7, -4.4), (1, 2.3, -6.9)), rotate=rotate_frames((2, -6), (2, -3), (2, 1), (2, 4), (2, 6), (2, 3), (2, -1), (2, -4), (2, -6), (2, -3), (1, -6.5))),
        track("left leg", translate=translate_frames((2, 13, -5), (2, 8, -2), (2, 1, 1), (2, -7, 0), (2, -13, 1), (2, -9, 2), (2, -2, -8), (2, 5, -15), (2, 12, -13), (2, 15, -8), (1, 14, -6)), rotate=rotate_frames((2, -32), (2, -16), (2, 0), (2, 18), (2, 34), (2, 24), (2, 6), (2, -14), (2, -28), (2, -36), (1, -34))),
        track("right leg", translate=translate_frames((2, -13, 1), (2, -8, 2), (2, -1, -8), (2, 6, -15), (2, 13, -13), (2, 15, -6), (2, 9, -2), (2, 2, 0), (2, -6, 0), (2, -12, 1), (1, -14, 1)), rotate=rotate_frames((2, 34), (2, 22), (2, 4), (2, -16), (2, -30), (2, -34), (2, -18), (2, 0), (2, 18), (2, 32), (1, 35))),
        track("left hand", translate=translate_frames((2, -7, 2), (2, -5, 1), (2, 0, -1), (2, 6, -5), (2, 10, -7), (2, 8, -5), (2, 2, -2), (2, -4, 1), (2, -8, 2), (2, -9, 2), (1, -8, 2)), rotate=rotate_frames((2, 24), (2, 16), (2, 4), (2, -12), (2, -24), (2, -18), (2, -4), (2, 12), (2, 24), (2, 27), (1, 25))),
        track("right hand", translate=translate_frames((2, 9, -5), (2, 7, -4), (2, 1, -1), (2, -5, 2), (2, -8, 3), (2, -6, 2), (2, 0, -1), (2, 6, -5), (2, 10, -7), (2, 9, -5), (1, 9, -6)), rotate=rotate_frames((2, -22), (2, -16), (2, -3), (2, 14), (2, 24), (2, 16), (2, 4), (2, -12), (2, -24), (2, -22), (1, -23))),
    ]
    return animation("run", duration, loop=True, bones=bones, slots=loop_pose(duration, with_ball=False), fade=0.08)


def custom_idle_wb() -> dict:
    duration = 13
    bones = [
        track("body", translate=translate_frames((4, 0, -1), (3, 0.5, 1.5), (3, 0, -0.5), (3, 0, -1)), rotate=rotate_frames((4, -2), (3, 2), (3, -1), (3, -2))),
        track("head", translate=translate_frames((4, 1, -3), (3, 1.5, 0.5), (3, 1, -2), (3, 1, -3)), rotate=rotate_frames((4, 3), (3, -2), (3, 2), (3, 3))),
        track("left hand", translate=translate_frames((4, 5, -18), (3, 7, -4), (3, 5, -16), (3, 5, -18)), rotate=rotate_frames((4, -34), (3, -14), (3, -32), (3, -34))),
        track("right hand", translate=translate_frames((4, -4, -5), (3, 2, 0), (3, -2, -4), (3, -4, -5)), rotate=rotate_frames((4, 16), (3, 8), (3, 14), (3, 16))),
        track("ball", translate=translate_frames((4, 1, 0), (3, 2, 24), (3, 1, 2), (3, 1, 0)), scale=scale_frames((4, 1, 1), (3, 1.14, 0.76), (3, 1.02, 0.96), (3, 1, 1))),
    ]
    return animation("idle_wb", duration, loop=True, bones=bones, slots=with_ball_slots(duration), frames=event_frames(duration, 6, "floor0"), fade=0.06)


def custom_run_wb() -> dict:
    duration = 21
    bones = [
        track("body", translate=translate_frames((2, 1.6, -4.0), (2, 1.0, -2.1), (2, 0.2, 0.2), (2, 0.7, -1.9), (2, 1.5, -4.2), (2, 1.0, -2.0), (2, 0.2, 0.3), (2, 0.7, -1.8), (2, 1.6, -4.1), (2, 1.1, -2.2), (1, 1.7, -4.3)), rotate=rotate_frames((2, -5), (2, -3), (2, 0), (2, 2), (2, 4), (2, 2), (2, -1), (2, -3), (2, -5), (2, -3), (1, -5.5))),
        track("head", translate=translate_frames((2, 2.8, -7.3), (2, 2.1, -5.1), (2, 1.0, -2.4), (2, 1.7, -4.6), (2, 2.8, -7.5), (2, 2.0, -5.0), (2, 0.9, -2.3), (2, 1.6, -4.5), (2, 2.8, -7.4), (2, 2.1, -5.1), (1, 2.9, -7.6)), rotate=rotate_frames((2, -7), (2, -4), (2, 0), (2, 3), (2, 5), (2, 3), (2, -1), (2, -4), (2, -7), (2, -4), (1, -7.5))),
        track("left leg", translate=translate_frames((2, 12, -6), (2, 7, -3), (2, 1, 0), (2, -6, 0), (2, -12, 1), (2, -8, 1), (2, -2, -7), (2, 5, -13), (2, 11, -12), (2, 14, -7), (1, 13, -7)), rotate=rotate_frames((2, -30), (2, -15), (2, 0), (2, 16), (2, 32), (2, 22), (2, 5), (2, -12), (2, -26), (2, -32), (1, -32))),
        track("right leg", translate=translate_frames((2, -12, 1), (2, -7, 1), (2, -1, -7), (2, 6, -13), (2, 12, -12), (2, 14, -7), (2, 8, -3), (2, 2, 0), (2, -6, 0), (2, -12, 1), (1, -13, 1)), rotate=rotate_frames((2, 32), (2, 21), (2, 4), (2, -14), (2, -28), (2, -32), (2, -16), (2, 0), (2, 16), (2, 32), (1, 33))),
        track("left hand", translate=translate_frames((2, 7, -20), (2, 8, -15), (2, 9, -7), (2, 9, -2), (2, 8, -11), (2, 7, -18), (2, 6, -21), (2, 7, -16), (2, 8, -7), (2, 8, -2), (1, 7, -21)), rotate=rotate_frames((2, -40), (2, -34), (2, -24), (2, -16), (2, -27), (2, -36), (2, -42), (2, -35), (2, -24), (2, -16), (1, -42))),
        track("right hand", translate=translate_frames((2, -4, -7), (2, -3, -5), (2, 0, -2), (2, 2, 0), (2, 0, -2), (2, -3, -5), (2, -4, -7), (2, -2, -5), (2, 1, -2), (2, 2, 0), (1, -4, -8)), rotate=rotate_frames((2, 26), (2, 22), (2, 15), (2, 10), (2, 15), (2, 22), (2, 26), (2, 22), (2, 15), (2, 10), (1, 27))),
        track("ball", translate=translate_frames((2, 2, -3), (2, 3, 7), (2, 4, 24), (2, 4, 13), (2, 5, -1), (2, 6, -12), (2, 5, -2), (2, 4, 8), (2, 4, 25), (2, 4, 12), (1, 2, -4)), scale=scale_frames((2, 1, 1), (2, 1.05, 0.92), (2, 1.22, 0.64), (2, 1.08, 0.88), (2, 1, 1), (2, 0.96, 1.04), (2, 1, 1), (2, 1.05, 0.92), (2, 1.22, 0.64), (2, 1.08, 0.88), (1, 1, 1))),
    ]
    return animation("run_wb", duration, loop=True, bones=bones, slots=with_ball_slots(duration), frames=event_frames(duration, 5, "floor"), fade=0.08)


def custom_air_pose(name: str, *, with_ball: bool, duration: int = 1, lift: float = 0, tilt: float = 0, ball_high: bool = False) -> dict:
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    left_arm_y = -44 if with_ball and ball_high else -36 if with_ball else -32
    right_arm_y = -42 if with_ball and ball_high else -34 if with_ball else -30
    bones = [
        track("body", translate=translate_frames((duration, 1.5, -lift)), rotate=rotate_frames((duration, tilt))),
        track("head", translate=translate_frames((duration, 2.5, -lift - 6)), rotate=rotate_frames((duration, tilt * 0.6))),
        track("left hand", translate=translate_frames((duration, 5, -lift + left_arm_y)), rotate=rotate_frames((duration, -78 + tilt))),
        track("right hand", translate=translate_frames((duration, 10, -lift + right_arm_y)), rotate=rotate_frames((duration, -74 + tilt))),
        track("left leg", translate=translate_frames((duration, 7, -5)), rotate=rotate_frames((duration, 32))),
        track("right leg", translate=translate_frames((duration, -5, -6)), rotate=rotate_frames((duration, 54))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((duration, 8, -lift - (44 if ball_high else 34))), rotate=rotate_frames((duration, 18))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_jump(name: str, *, with_ball: bool) -> dict:
    duration = 6
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    bones = [
        track("body", translate=translate_frames((2, 0, 4), (2, 1, -12), (2, 1, -20)), scale=scale_frames((2, 1.04, 0.94), (2, 0.98, 1.04), (2, 1, 1))),
        track("head", translate=translate_frames((2, 0, 2), (2, 2, -15), (2, 2, -25)), rotate=rotate_frames((2, 4), (2, -6), (2, -4))),
        track("left hand", translate=translate_frames((2, 2, -10), (2, 4, -36), (2, 5, -48)), rotate=rotate_frames((2, -18), (2, -68), (2, -86))),
        track("right hand", translate=translate_frames((2, -2, -10), (2, 8, -34), (2, 10, -47)), rotate=rotate_frames((2, 18), (2, -58), (2, -82))),
        track("left leg", translate=translate_frames((2, 5, 3), (2, 7, -4), (2, 5, -8)), rotate=rotate_frames((2, 18), (2, 42), (2, 50))),
        track("right leg", translate=translate_frames((2, -5, 3), (2, -5, -4), (2, -4, -8)), rotate=rotate_frames((2, -16), (2, 38), (2, 58))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((2, 2, -8), (2, 7, -31), (2, 8, -43)), rotate=rotate_frames((2, 0), (2, 16), (2, 24))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_landing(name: str, *, with_ball: bool) -> dict:
    duration = 7
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    bones = [
        track("body", translate=translate_frames((2, 0, 5), (3, 0, 1), (2, 0, 0)), scale=scale_frames((2, 1.05, 0.9), (3, 0.98, 1.05), (2, 1, 1))),
        track("head", translate=translate_frames((2, 1, 3), (3, 0.5, -1), (2, 0, 0)), rotate=rotate_frames((2, 6), (3, -2), (2, 0))),
        track("left leg", translate=translate_frames((2, 4, 2), (3, 1, 0), (2, 0, 0)), rotate=rotate_frames((2, 24), (3, 6), (2, 0))),
        track("right leg", translate=translate_frames((2, -3, 2), (3, -1, 0), (2, 0, 0)), rotate=rotate_frames((2, 20), (3, 5), (2, 0))),
        track("left hand", rotate=rotate_frames((2, -16), (3, 3), (2, 0))),
        track("right hand", rotate=rotate_frames((2, 18), (3, -4), (2, 0))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((2, 3, -12), (3, 1, 1), (2, 0, 0))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_throw_land() -> dict:
    duration = 8
    release = 4
    slots = event_slots(duration, release)
    bones = [
        track("body", translate=translate_frames((2, -1, 1), (2, 0, -8), (2, 2, -10), (2, 0, 0)), rotate=rotate_frames((2, 4), (2, -10), (2, -6), (2, 0))),
        track("head", translate=translate_frames((2, 0, -1), (2, 2, -12), (2, 3, -14), (2, 0, 0)), rotate=rotate_frames((2, 5), (2, -12), (2, -7), (2, 0))),
        track("left hand", translate=translate_frames((2, 4, -22), (2, 5, -40), (2, 5, -50), (2, 1, -12)), rotate=rotate_frames((2, -48), (2, -76), (2, -96), (2, -25))),
        track("right hand", translate=translate_frames((2, -2, -20), (2, 8, -39), (2, 10, -50), (2, 4, -12)), rotate=rotate_frames((2, 34), (2, -64), (2, -94), (2, -34))),
        track("left leg", translate=translate_frames((2, 4, 0), (2, 5, -2), (2, 2, -1), (2, 0, 0)), rotate=rotate_frames((2, 16), (2, 20), (2, 10), (2, 0))),
        track("right leg", translate=translate_frames((2, -3, 0), (2, -4, -1), (2, -2, 0), (2, 0, 0)), rotate=rotate_frames((2, -8), (2, -12), (2, -4), (2, 0))),
        track("ball", translate=translate_frames((2, 6, -26), (2, 8, -43), (1, 20, -52), (3, 36, -55)), scale=scale_frames((2, 1, 1), (2, 1.04, 0.96), (1, 1.08, 0.88), (3, 1, 1))),
    ]
    return animation("throw_land", duration, bones=bones, slots=slots, frames=event_frames(duration, release, "throw"), fade=0.01)


def custom_dunk(name: str, duration: int, event_at: int, *, style: int) -> dict:
    visual_release_frame = max(1, event_at - 1)
    slots = event_slots(duration, visual_release_frame, front=True)
    slots[0] = slot_track("ball", display_value=-1, duration=duration)
    wind = 4 if style == 1 else 6
    lift = max(1, event_at - wind - 2)
    push = 2
    finish = max(1, duration - wind - lift - push)
    side = -1 if style == 2 else 1
    lean = 4 + style * 1.5
    release_x = 15 + style * 2
    release_y = -70 - style * 3
    hang_y = -20 - style * 2
    gather_ball_y = -34 - style * 2
    bones = [
        track("body", translate=translate_frames((wind, -2 * side, 4), (lift, 2 * side, hang_y), (push, lean * side, hang_y - 2), (finish, 1 * side, -6)), rotate=rotate_frames((wind, -5 * side), (lift, 7 * side), (push, 10 * side), (finish, 2 * side)), scale=scale_frames((wind, 1.04, 0.94), (lift, 0.99, 1.03), (push, 0.99, 1.03), (finish, 1, 1))),
        track("head", translate=translate_frames((wind, -2 * side, -1), (lift, 2.5 * side, hang_y - 7), (push, lean * side, hang_y - 9), (finish, 0.5 * side, -4)), rotate=rotate_frames((wind, -5 * side), (lift, 6 * side), (push, 8 * side), (finish, 1 * side))),
        track("left hand", translate=translate_frames((wind, 4, -34), (lift, 6, -60 - style), (push, release_x - 5, release_y + 3), (finish, 2, -26)), rotate=rotate_frames((wind, -70), (lift, -94), (push, -112), (finish, -30))),
        track("right hand", translate=translate_frames((wind, 2, -32), (lift, release_x - 5, -58 - style), (push, release_x, release_y), (finish, 7, -24)), rotate=rotate_frames((wind, -48), (lift, -88), (push, -108), (finish, -24))),
        track("left leg", translate=translate_frames((wind, 4, 2), (lift, 5, -6), (push, 4, -7), (finish, 1, 1)), rotate=rotate_frames((wind, 18), (lift, 34), (push, 38), (finish, 8))),
        track("right leg", translate=translate_frames((wind, -4, 2), (lift, -5, -5), (push, -5, -6), (finish, -1, 1)), rotate=rotate_frames((wind, -16), (lift, 30), (push, 34), (finish, 7))),
        track("ball", translate=translate_frames((wind, 8 + style, gather_ball_y), (lift, release_x - 6, -60 - style), (push, release_x, release_y), (finish, release_x, release_y)), scale=scale_frames((wind, 1.04, 1.04), (lift, 1.08, 1.08), (push, 1, 1), (finish, 1, 1))),
        track("ball_front", translate=translate_frames((wind, 8 + style, gather_ball_y), (lift, release_x - 6, -60 - style), (push, release_x, release_y), (finish, release_x, release_y)), scale=scale_frames((wind, 1.04, 1.04), (lift, 1.08, 1.08), (push, 1, 1), (finish, 1, 1))),
    ]
    return animation(name, duration, bones=bones, slots=slots, frames=event_frames(duration, event_at, "dunk"), fade=0.01)


def custom_steal() -> dict:
    duration = 13
    bones = [
        track("body", translate=translate_frames((2, 0, 0), (3, -4, 1), (3, 12, -3), (2, 20, -7), (3, 0, 0)), rotate=rotate_frames((2, 0), (3, 6), (3, -8), (2, -14), (3, 0))),
        track("head", translate=translate_frames((2, 0, -1), (3, -2, 0), (3, 15, -6), (2, 23, -9), (3, 0, 0)), rotate=rotate_frames((2, 0), (3, 5), (3, -12), (2, -17), (3, 0))),
        track("right hand", translate=translate_frames((2, -4, 2), (3, -20, 7), (3, 30, -12), (2, 70, -20), (3, 2, 0)), rotate=rotate_frames((2, 12), (3, 42), (3, -60), (2, -112), (3, 0))),
        track("left hand", translate=translate_frames((2, 0, 0), (3, -7, 3), (3, -14, 5), (2, -18, 7), (3, 0, 0)), rotate=rotate_frames((2, 0), (3, 18), (3, 28), (2, 34), (3, 0))),
        track("left leg", translate=translate_frames((2, 0, 0), (3, -3, 1), (3, 10, -2), (2, 14, -4), (3, 0, 0)), rotate=rotate_frames((2, 0), (3, 8), (3, -18), (2, -24), (3, 0))),
        track("right leg", translate=translate_frames((2, 0, 0), (3, 4, 2), (3, -8, 2), (2, -12, 3), (3, 0, 0)), rotate=rotate_frames((2, 0), (3, 14), (3, 22), (2, 28), (3, 0))),
    ]
    return animation("steal", duration, bones=bones, slots=hidden_optional_slots(duration), frames=event_frames(duration, 8, "action"), fade=0.01)


def custom_pump_block(name: str, *, pump: bool, start: bool) -> dict:
    duration = 4 if pump else (3 if start else 5)
    slots = with_ball_slots(duration) if pump else hidden_optional_slots(duration)
    if not pump:
        if start:
            bones = [
                track("body", translate=translate_frames((duration, 0, 9)), scale=scale_frames((duration, 1.05, 0.9))),
                track("head", translate=translate_frames((duration, 0.5, 5)), rotate=rotate_frames((duration, 5))),
                track("left hand", translate=translate_frames((duration, 4, -8)), rotate=rotate_frames((duration, -18))),
                track("right hand", translate=translate_frames((duration, -4, -8)), rotate=rotate_frames((duration, 18))),
                track("left leg", translate=translate_frames((duration, 3, 5)), rotate=rotate_frames((duration, 12))),
                track("right leg", translate=translate_frames((duration, -3, 5)), rotate=rotate_frames((duration, -10))),
            ]
        else:
            bones = [
                track("body", translate=translate_frames((2, 0, 9), (3, 0, 0)), scale=scale_frames((2, 1.05, 0.9), (3, 1, 1))),
                track("head", translate=translate_frames((2, 0.5, 5), (3, 0, 0)), rotate=rotate_frames((2, 5), (3, 0))),
                track("left hand", translate=translate_frames((2, 4, -8), (3, 0, 0)), rotate=rotate_frames((2, -18), (3, 0))),
                track("right hand", translate=translate_frames((2, -4, -8), (3, 0, 0)), rotate=rotate_frames((2, 18), (3, 0))),
                track("left leg", translate=translate_frames((2, 3, 5), (3, 0, 0)), rotate=rotate_frames((2, 12), (3, 0))),
                track("right leg", translate=translate_frames((2, -3, 5), (3, 0, 0)), rotate=rotate_frames((2, -10), (3, 0))),
            ]
        return animation(name, duration, bones=bones, slots=slots, fade=0.01)

    arm_y = -34 if start else -6
    body_y = -8 if start else 0
    bones = [
        track("body", translate=translate_frames((duration, 0, body_y)), scale=scale_frames((duration, 1.02 if start else 1, 0.97 if start else 1))),
        track("head", translate=translate_frames((duration, 1, body_y - 3)), rotate=rotate_frames((duration, -3 if start else 0))),
        track("left hand", translate=translate_frames((duration, 6, arm_y)), rotate=rotate_frames((duration, -68 if start else -5))),
        track("right hand", translate=translate_frames((duration, -5, arm_y)), rotate=rotate_frames((duration, 66 if start else 4))),
    ]
    if pump:
        bones.append(track("ball", translate=translate_frames((duration, 2, -26 if start else 0)), scale=scale_frames((duration, 1.12 if start else 1, 0.78 if start else 1))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_dash(name: str, *, with_ball: bool) -> dict:
    duration = 11
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    slots[5] = slot_track("effects stun", display_value=1, duration=duration)
    bones = [
        track("body", translate=translate_frames((3, 13, -6), (4, 7, -4), (4, 1, -1)), rotate=rotate_frames((3, -17), (4, -10), (4, 0))),
        track("head", translate=translate_frames((3, 17, -8), (4, 9, -5), (4, 1, -1)), rotate=rotate_frames((3, -21), (4, -12), (4, 0))),
        track("left leg", translate=translate_frames((3, -17, -6), (4, -6, -2), (4, 7, -2)), rotate=rotate_frames((3, 60), (4, 16), (4, -22))),
        track("right leg", translate=translate_frames((3, 22, -9), (4, 11, -5), (4, -5, -2)), rotate=rotate_frames((3, -52), (4, -18), (4, 20))),
        track("left hand", translate=translate_frames((3, 9, -13), (4, 2, -8), (4, 0, 0)), rotate=rotate_frames((3, -42), (4, -18), (4, 0))),
        track("right hand", translate=translate_frames((3, 14, -8), (4, 7, -4), (4, 0, 0)), rotate=rotate_frames((3, 34), (4, 18), (4, 0))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((3, 11, -5), (4, 6, -2), (4, 0, 0)), rotate=rotate_frames((3, 35), (4, 20), (4, 0))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_dig(name: str, *, phase: int, duration: int) -> dict:
    slots = hidden_optional_slots(duration)
    slots[3] = slot_track("dighand", display_value=0, duration=duration)
    slots[4] = slot_track("digleg", display_value=0, duration=duration)
    reach = 12 + phase * 8
    mid = max(3, duration // 2)
    end = max(1, duration - mid)
    bones = [
        track("body", translate=translate_frames((mid, reach * 0.35, 5), (end, 0, 0)), rotate=rotate_frames((mid, -9), (end, 0))),
        track("head", translate=translate_frames((mid, reach * 0.45, 2), (end, 0, 0)), rotate=rotate_frames((mid, -14), (end, 0))),
        track("dighand", translate=translate_frames((mid, reach + 24, -5), (end, 0, 0)), rotate=rotate_frames((mid, -68), (end, 0))),
        track("digleg", translate=translate_frames((mid, -8, 4), (end, 0, 0)), rotate=rotate_frames((mid, 38), (end, 0))),
        track("left hand", translate=translate_frames((mid, -5, 2), (end, 0, 0)), rotate=rotate_frames((mid, 18), (end, 0))),
    ]
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def custom_stun() -> dict:
    duration = 33
    slots = hidden_optional_slots(duration)
    slots[2] = slot_track("eyes", display_value=0, duration=duration)
    slots[5] = slot_track("effects stun", display_value=0, duration=duration)
    slots[6] = slot_track("effects stun star", display_value=0, duration=duration)
    bones = [
        track("body", translate=translate_frames((5, -3, 1), (5, 3, -2), (5, -2, 1), (5, 2, -1), (13, 0, 0)), rotate=rotate_frames((5, -4), (5, 5), (5, -5), (5, 4), (13, 0))),
        track("head", translate=translate_frames((5, -5, -1), (5, 5, -3), (5, -4, -1), (5, 4, -3), (13, 0, 0)), rotate=rotate_frames((5, -11), (5, 12), (5, -10), (5, 10), (13, 0))),
        track("left hand", rotate=rotate_frames((5, 16), (5, -12), (5, 14), (5, -10), (13, 0))),
        track("right hand", rotate=rotate_frames((5, -16), (5, 12), (5, -14), (5, 10), (13, 0))),
        track("effects stun star", rotate=rotate_frames((6, 0), (6, 110), (6, 220), (6, 330), (9, 480))),
    ]
    return animation("stun", duration, bones=bones, slots=slots, fade=0.01)


def custom_mood(name: str, *, happy: bool, duration: int, loop: bool) -> dict:
    slots = hidden_optional_slots(duration)
    if not happy:
        slots[5] = slot_track("effects stun", display_value=5, duration=duration)
    if happy:
        bones = [
            track("body", translate=translate_frames((5, 0, -5), (5, 0, -1), (5, 0, -6), (duration - 15, 0, -1)), rotate=rotate_frames((5, -5), (5, 5), (5, -4), (duration - 15, 4))),
            track("head", translate=translate_frames((5, 0, -9), (5, 0, -4), (5, 0, -10), (duration - 15, 0, -4)), rotate=rotate_frames((5, -8), (5, 6), (5, -6), (duration - 15, 5))),
            track("left hand", translate=translate_frames((5, 4, -26), (5, 8, -12), (5, 5, -28), (duration - 15, 8, -12)), rotate=rotate_frames((5, -76), (5, -30), (5, -80), (duration - 15, -28))),
            track("right hand", translate=translate_frames((5, -4, -25), (5, -8, -10), (5, -5, -27), (duration - 15, -8, -10)), rotate=rotate_frames((5, 74), (5, 28), (5, 78), (duration - 15, 28))),
        ]
    else:
        split = max(1, duration // 2)
        bones = [
            track("body", translate=translate_frames((split, 0, 5), (duration - split, 0, 3)), rotate=rotate_frames((split, 4), (duration - split, 2))),
            track("head", translate=translate_frames((split, 0, 8), (duration - split, 0, 5)), rotate=rotate_frames((split, 12), (duration - split, 8))),
            track("left hand", translate=translate_frames((split, -2, 4), (duration - split, 0, 2)), rotate=rotate_frames((split, 22), (duration - split, 12))),
            track("right hand", translate=translate_frames((split, 2, 4), (duration - split, 0, 2)), rotate=rotate_frames((split, -22), (duration - split, -12))),
        ]
    return animation(name, duration, loop=loop, bones=bones, slots=slots, fade=0.06)


def custom_mega(name: str, *, end: bool = False, fly: bool = False) -> dict:
    if end:
        duration = 10
        slots = event_slots(duration, 4)
        slots[2] = slot_track("eyes", display_value=1, duration=duration)
        slots[5] = slot_track("effects stun", display_value=7, duration=duration)
        bones = [
            track("body", translate=translate_frames((4, 6, -38), (3, 2, -18), (3, 0, 0)), rotate=rotate_frames((4, 16), (3, 8), (3, 0))),
            track("head", translate=translate_frames((4, 7, -48), (3, 2, -22), (3, 0, 0)), rotate=rotate_frames((4, 18), (3, 8), (3, 0))),
            track("left hand", translate=translate_frames((4, 6, -64), (3, 4, -26), (3, 0, 0)), rotate=rotate_frames((4, -92), (3, -36), (3, 0))),
            track("right hand", translate=translate_frames((4, 28, -60), (3, 12, -24), (3, 0, 0)), rotate=rotate_frames((4, -84), (3, -30), (3, 0))),
            track("ball", translate=translate_frames((4, 27, -64), (3, 28, -70), (3, 0, 0)), scale=scale_frames((4, 1.25, 1.25), (3, 0.8, 0.8), (3, 1, 1))),
        ]
        return animation("megadunk_end", duration, bones=bones, slots=slots, frames=event_frames(duration, 4, "mega"), fade=0.01)
    duration = 5 if not fly else 1
    slots = with_ball_slots(duration)
    slots[2] = slot_track("eyes", display_value=1, duration=duration)
    slots[5] = slot_track("effects stun", display_value=7, duration=duration)
    bones = [
        track("body", translate=translate_frames((duration, 5, -34)), rotate=rotate_frames((duration, 13))),
        track("head", translate=translate_frames((duration, 6, -44)), rotate=rotate_frames((duration, 16))),
        track("left hand", translate=translate_frames((duration, 4, -62)), rotate=rotate_frames((duration, -88))),
        track("right hand", translate=translate_frames((duration, 24, -58)), rotate=rotate_frames((duration, -76))),
        track("left leg", translate=translate_frames((duration, -8, -6)), rotate=rotate_frames((duration, 54))),
        track("right leg", translate=translate_frames((duration, 10, -5)), rotate=rotate_frames((duration, 24))),
        track("ball", translate=translate_frames((duration, 23, -61)), scale=scale_frames((duration, 1.18, 1.18))),
    ]
    return animation(name, duration, loop=fly, bones=bones, slots=slots, fade=0.01)


def custom_md(name: str, *, with_ball: bool, start: bool, end: bool) -> dict:
    duration = 5 if start or end else 1
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    if start:
        x, y, rot = 16, -9, -18
    elif end:
        x, y, rot = -7, 2, 6
    else:
        x, y, rot = 8, -7, -12
    bones = [
        track("body", translate=translate_frames((duration, x, y)), rotate=rotate_frames((duration, rot))),
        track("head", translate=translate_frames((duration, x + 3, y - 4)), rotate=rotate_frames((duration, rot * 0.9))),
        track("left leg", translate=translate_frames((duration, -10, -4)), rotate=rotate_frames((duration, 46))),
        track("right leg", translate=translate_frames((duration, 16, -5)), rotate=rotate_frames((duration, -38))),
        track("left hand", translate=translate_frames((duration, 8, -18)), rotate=rotate_frames((duration, -44))),
        track("right hand", translate=translate_frames((duration, 14, -10)), rotate=rotate_frames((duration, 26))),
    ]
    if with_ball:
        bones.append(track("ball", translate=translate_frames((duration, x + 4, y - 9))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def main_armature() -> dict:
    data = player_armature_base()
    data["bone"] = [
        bone("left leg", 4.04, -7.95),
        bone("left hand", 16.75, -28.86),
        bone("body", -5.37, -29.66),
        bone("right leg", -4.69, -5.04),
        bone("head", -6.56, -71.81),
        bone("right hand", -25.99, -27.14, 15),
        bone("dighand", -34.09, -28.86),
        bone("digleg", -11.25, -5.04),
        {"inheritScale": False, "name": "ball", "transform": {"x": 34.19, "y": -51.43, "scX": 0.9, "scY": 1.1077}},
        {"inheritScale": False, "name": "ball_front", "transform": {"x": 11.21, "y": -16.97, "skX": 77.73, "skY": 77.73, "scX": 0.9985, "scY": 0.9984}},
        bone("effects stun", -7.48, -70.19),
        bone("eyes", 2.91, -76.32, 13.81),
        bone("effects stun star", -18.86, -73.59, -15),
    ]
    data["slot"] = [
        slot("left leg"),
        slot("left hand"),
        slot("body"),
        slot("right leg"),
        slot("ball"),
        slot("head"),
        slot("ball_front"),
        slot("right hand"),
        slot("eyes"),
        slot("dighand"),
        slot("digleg"),
        slot("effects stun"),
        slot("effects stun star"),
    ]
    data["skin"] = [
        {
            "slot": [
                skin_slot("left leg", [display("dbanims/LegsDB", type_="armature", x=1.11, y=2.91)]),
                skin_slot("left hand", [display("dbanims/LeftHandDB", type_="armature")]),
                skin_slot("body", [display("BodyDB", type_="armature")]),
                skin_slot("right leg", [display("dbanims/LegsDB", type_="armature")]),
                skin_slot("ball", [display(".Game/ball/BallClip", x=-0.06, y=0.04)]),
                skin_slot("head", [display("HeadsDB", type_="armature")]),
                skin_slot("ball_front", [display(".Game/ball/BallClip", x=0.06, y=0.06)]),
                skin_slot("right hand", [display("dbanims/RightHandDB", type_="armature", x=0.02, y=0.02)]),
                skin_slot("eyes", [display("dbanims/eyes_stunned", type_="armature", y=0.04), display("shield_animation_01", type_="armature", x=0.04)]),
                skin_slot("dighand", [display("dbanims/LeftHandDB2", type_="armature")]),
                skin_slot("digleg", [display("dbanims/LegsDB2", type_="armature")]),
                skin_slot(
                    "effects stun",
                    [
                        display("dbanims/kuynya_01", x=-0.02, y=0.02),
                        display("dbanims/backwind_01", type_="armature"),
                        display("dbanims/circles1"),
                        display("dbanims/circle2", y=0.02),
                        display("dbanims/circle3", y=0.02),
                        display("dbanims/tears_01", type_="armature", x=-0.02, y=0.02),
                        display("dbanims/CupDB", type_="armature", x=-0.02, y=0.02),
                        display("man_fire_01", type_="armature", x=-0.12, y=-0.18),
                    ],
                ),
                skin_slot("effects stun star", [display("dbanims/star_0123", y=0.04)]),
            ]
        }
    ]
    data["animation"] = [
        custom_idle(),
        custom_run(),
        custom_idle_wb(),
        custom_run_wb(),
        custom_throw_land(),
        custom_jump("jump", with_ball=False),
        custom_air_pose("fly1", with_ball=False, lift=24, tilt=-5),
        custom_air_pose("fly2", with_ball=False, lift=31, tilt=2),
        custom_air_pose("fly3", with_ball=False, lift=36, tilt=7),
        custom_air_pose("fly4", with_ball=False, lift=30, tilt=-8),
        custom_air_pose("fly5", with_ball=False, lift=22, tilt=0),
        custom_landing("landing", with_ball=False),
        custom_jump("jump_wb", with_ball=True),
        custom_air_pose("fly1_wb", with_ball=True, lift=24, tilt=-5, ball_high=True),
        custom_air_pose("fly2_wb", with_ball=True, lift=31, tilt=2, ball_high=True),
        custom_air_pose("fly3_wb", with_ball=True, lift=36, tilt=7, ball_high=True),
        custom_air_pose("fly4_wb", with_ball=True, lift=30, tilt=-8, ball_high=True),
        custom_air_pose("fly5_wb", with_ball=True, lift=22, tilt=0, ball_high=True),
        custom_landing("landing_wb", with_ball=True),
        custom_dunk("dunk1", 24, 18, style=0),
        custom_dunk("dunk2", 15, 9, style=1),
        custom_dunk("dunk3", 24, 14, style=2),
        custom_steal(),
        custom_pump_block("pumpStart", pump=True, start=True),
        custom_pump_block("pumpEnd", pump=True, start=False),
        custom_dash("dash_wb", with_ball=True),
        custom_dash("dash", with_ball=False),
        custom_dig("dig1", phase=1, duration=31),
        custom_dig("dig2", phase=2, duration=20),
        custom_dig("dig3", phase=3, duration=24),
        custom_stun(),
        custom_mood("sad", happy=False, duration=15, loop=False),
        custom_mood("sad0", happy=False, duration=4, loop=False),
        custom_mood("happiness", happy=True, duration=19, loop=True),
        custom_pump_block("blockStart", pump=False, start=True),
        custom_pump_block("blockEnd", pump=False, start=False),
        custom_mega("megadunk"),
        custom_mega("megadunk_fly", fly=True),
        custom_mega("megadunk_end", end=True),
        custom_md("md_start_wb", with_ball=True, start=True, end=False),
        custom_md("md_mid_wb", with_ball=True, start=False, end=False),
        custom_md("md_end_wb", with_ball=True, start=False, end=True),
        custom_md("md_start", with_ball=False, start=True, end=False),
        custom_md("md_mid", with_ball=False, start=False, end=False),
        custom_md("md_end", with_ball=False, start=False, end=True),
    ]
    return data


def picker_armature(name: str, slot_name: str, display_names: list[str], animation_names: list[str]) -> dict:
    """
    构建角色部件选择器骨骼

    选择器骨骼是一种特殊的子骨骼，通过切换动画来选择显示哪个角色的部件。
    每个动画对应一个角色的部件图片，运行时通过播放对应动画名来切换角色。

    例如 HeadsDB 骨骼有8个动画（head1~head8），每个动画显示不同角色的头部图片。
    游戏逻辑通过设置当前播放的动画名来切换角色外观。

    参数:
        name: 骨骼名称（如"HeadsDB"、"BodyDB"等）
        slot_name: 插槽名称
        display_names: 候选显示对象名称列表（对应纹理图集中的图片名）
        animation_names: 动画名称列表（每个对应一个角色）

    返回:
        选择器骨骼定义字典
    """
    bones = [bone(slot_name)]
    slots = [slot(slot_name)]
    skin = [skin_slot(slot_name, [display(item) for item in display_names])]
    animations = [
        animation(animation_name, slots=[slot_track(slot_name, display_value=index)])
        for index, animation_name in enumerate(animation_names)
    ]
    return {"name": name, "bone": bones, "slot": slots, "skin": [{"slot": skin}], "animation": animations}


def character_part_armatures() -> list[dict]:
    """
    构建所有角色部件选择器骨骼

    为以下7个部件各创建一个选择器骨骼：
    - LegsDB：腿部选择器
    - LeftHandDB：左手选择器
    - BodyDB：身体选择器
    - HeadsDB：头部选择器
    - RightHandDB：右手选择器
    - LeftHandDB2：挖掘手选择器
    - LegsDB2：挖掘腿选择器

    每个选择器骨骼包含8个动画（对应8个角色），运行时通过播放
    对应动画名来切换角色外观。

    返回:
        所有选择器骨骼定义列表
    """
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
    """
    构建背风动画特效骨骼

    背风特效用于角色快速移动时的风痕效果。
    3帧循环切换三种风痕图案，营造速度感。

    返回:
        背风动画骨骼定义
    """
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
    """
    构建眩晕眼睛动画骨骼

    眩晕眼睛由左右两个独立的眼睛骨骼组成，
    每个眼睛以相反方向旋转，模拟眩晕时的螺旋眼效果。
    12帧循环。

    返回:
        眩晕眼睛骨骼定义
    """
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
    """
    构建眼泪动画骨骼

    悲伤情绪时显示的流泪效果。5帧循环，泪滴从上往下掉落，
    同时逐帧切换不同的泪滴形状以模拟流动。

    返回:
        眼泪动画骨骼定义
    """
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
    """
    构建奖杯动画骨骼

    奖杯有三种状态（铜/银/金），通过切换显示索引来选择。
    每个状态对应不同的奖杯图片。默认隐藏。

    返回:
        奖杯骨骼定义
    """
    displays = ["dbanims/cup_01", "dbanims/cup_02", "dbanims/cup_03"]
    return {
        "name": "dbanims/CupDB",
        "bone": [bone("cup sprite")],
        "slot": [slot("cup sprite", display_index=-1)],
        "skin": [{"slot": [skin_slot("cup sprite", [display(item) for item in displays])]}],
        "animation": [
            animation("cup0", slots=[slot_track("cup sprite", display_value=-1)]),  # 隐藏
            animation("cup1", slots=[slot_track("cup sprite", display_value=0)]),   # 铜杯
            animation("cup2", slots=[slot_track("cup sprite", display_value=1)]),   # 银杯
            animation("cup3", slots=[slot_track("cup sprite", display_value=2)]),   # 金杯
        ],
    }


def fire_armature() -> dict:
    """
    构建火焰动画骨骼

    超级扣篮时角色身上燃烧的火焰特效。
    由模糊层（fire blur）和核心层（fire core）组成，
    两层以不同节奏循环5帧，营造闪烁的火焰效果。

    返回:
        火焰动画骨骼定义
    """
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
    """
    构建护盾动画骨骼

    护盾特效包含两个骨骼：
    1. 护盾包装器（shield_animation_01）：外层容器，包含护盾子骨骼
    2. 护盾本体（shield_anim）：核心动画，包含核心、烟雾、火花和光线

    护盾本体有6个组件：
    - shield core：核心圆盘，持续脉动缩放
    - shield smoke：烟雾粒子，8帧循环
    - shield spark a/b：火花粒子，随机漂移
    - shield streak a/b：光线条纹，持续旋转

    返回:
        护盾骨骼定义列表
    """
    # 外层包装器
    wrapper = {
        "name": "shield_animation_01",
        "bone": [bone("shield wrapper", 40, 0)],
        "slot": [slot("shield wrapper")],
        "skin": [{"slot": [skin_slot("shield wrapper", [display("shield_anim", type_="armature", scale_x=1.2, scale_y=1.2)])]}],
        "animation": [animation("anim")],
    }

    # 护盾本体
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
    """
    构建所有特效骨骼

    汇总所有特效骨骼：背风、眩晕眼睛、眼泪、奖杯、火焰、护盾（2个）。

    返回:
        所有特效骨骼定义列表
    """
    return [
        backwind_armature(),
        eyes_armature(),
        tears_armature(),
        cup_armature(),
        fire_armature(),
        *shield_armatures(),
    ]


# ============================================================
# 完整骨架和验证
# ============================================================

def skeleton() -> dict:
    """
    构建完整的DragonBones骨架数据

    组装所有骨骼（主骨骼 + 角色部件选择器 + 特效），
    附加元数据（帧率、版本号、生成器信息）。

    返回:
        完整的骨架数据字典
    """
    return {
        "frameRate": FRAME_RATE,
        "name": RUNTIME_NAME,
        "version": "5.5",
        "compatibleVersion": "5.5",
        "userData": {
            "generator": "Tools/Art/rebuild_runtime_dragonbones_skeleton.py",
            "note": "Project-authored DBLite-compatible skeleton. Runtime names stay stable for gameplay compatibility.",
        },
        "armature": [main_armature(), *character_part_armatures(), *fx_armatures()],
    }


def collect_display_references(data: dict) -> tuple[set[str], set[str]]:
    """
    收集骨架数据中所有显示对象的引用

    遍历所有骨骼的皮肤配置，收集所有引用的图片名和子骨骼名。
    用于后续验证：确保所有引用的图片和子骨骼都存在。

    参数:
        data: 完整的骨架数据

    返回:
        (图片引用集合, 骨骼名称集合) 元组
    """
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

    # 验证所有引用的子骨骼都存在
    missing_armatures = armature_refs - armatures
    if missing_armatures:
        raise RuntimeError(f"Missing child armatures: {sorted(missing_armatures)}")

    return image_refs, armatures


def validate(data: dict, texture_json: dict) -> None:
    """
    验证骨架数据的完整性

    检查以下内容：
    1. 所有引用的图片在纹理图集中都存在
    2. 主骨骼包含所有必需的动画
    3. 所有必需的帧事件都被触发
    4. playerSmall骨骼存在

    参数:
        data: 完整的骨架数据
        texture_json: 纹理图集的JSON配置

    异常:
        RuntimeError: 如果验证失败
    """
    image_refs, armatures = collect_display_references(data)
    texture_names = {item["name"] for item in texture_json.get("SubTexture", [])}

    # 验证图片引用
    missing_images = image_refs - texture_names
    if missing_images:
        raise RuntimeError(f"Missing texture references: {sorted(missing_images)}")

    # 验证主骨骼动画完整性
    main = next(item for item in data["armature"] if item["name"] == "playerSmall")
    animation_names = {item["name"] for item in main["animation"]}
    missing_animations = set(MAIN_ANIMATIONS) - animation_names
    if missing_animations:
        raise RuntimeError(f"Missing playerSmall animations: {sorted(missing_animations)}")

    # 验证帧事件
    required_events = {"floor0", "floor", "throw", "dunk", "action", "mega"}
    events: set[str] = set()
    for anim in main["animation"]:
        for item in anim.get("frame", []):
            if "event" in item:
                events.add(item["event"])
    missing_events = required_events - events
    if missing_events:
        raise RuntimeError(f"Missing frame events: {sorted(missing_events)}")

    expected_animation_events = {
        "idle_wb": "floor0",
        "run_wb": "floor",
        "throw_land": "throw",
        "dunk1": "dunk",
        "dunk2": "dunk",
        "dunk3": "dunk",
        "steal": "action",
        "megadunk_end": "mega",
    }
    animations_by_name = {item["name"]: item for item in main["animation"]}
    for animation_name, event_name in expected_animation_events.items():
        animation_data = animations_by_name.get(animation_name)
        if animation_data is None:
            raise RuntimeError(f"Missing playerSmall animation: {animation_name}")
        animation_events = {
            item["event"]
            for item in animation_data.get("frame", [])
            if "event" in item
        }
        if event_name not in animation_events:
            raise RuntimeError(f"Animation {animation_name} is missing frame event {event_name}.")

    expected_dunk_event_frames = {
        "dunk1": 18,
        "dunk2": 9,
        "dunk3": 14,
    }
    for animation_name, event_frame in expected_dunk_event_frames.items():
        animation_data = animations_by_name[animation_name]
        cursor = 0
        dunk_event_at = None
        for item in animation_data.get("frame", []):
            if item.get("event") == "dunk":
                dunk_event_at = cursor
                break
            cursor += item.get("duration", 1)
        if dunk_event_at != event_frame:
            raise RuntimeError(f"Animation {animation_name} dunk event moved to {dunk_event_at}; expected {event_frame}.")

        slots_by_name = {item["name"]: item for item in animation_data.get("slot", [])}
        ball_frames = slots_by_name["ball"]["displayFrame"]
        if any(item.get("value") != -1 for item in ball_frames):
            raise RuntimeError(f"Animation {animation_name} must keep the back ball slot hidden.")

        front_frames = slots_by_name["ball_front"]["displayFrame"]
        if not front_frames or front_frames[0].get("value") != 0:
            raise RuntimeError(f"Animation {animation_name} must show ball_front before release.")

        cursor = 0
        hidden_front_at = None
        for item in front_frames:
            if item.get("value") == -1:
                hidden_front_at = cursor
                break
            cursor += item.get("duration", 1)
        if hidden_front_at is None or hidden_front_at >= event_frame:
            raise RuntimeError(f"Animation {animation_name} must hide ball_front before the dunk event.")

    misplaced_mega = {
        anim["name"]
        for anim in main["animation"]
        if anim["name"] != "megadunk_end"
        for item in anim.get("frame", [])
        if item.get("event") == "mega"
    }
    if misplaced_mega:
        raise RuntimeError(f"The mega event must stay on megadunk_end: {sorted(misplaced_mega)}")

    # 验证playerSmall骨骼存在
    if "playerSmall" not in armatures:
        raise RuntimeError("playerSmall armature is missing.")


def update_texture_json_name() -> None:
    """
    更新texture2.json中的根名称为运行时名称

    确保纹理图集的name字段与骨骼数据的name字段一致，
    这样DragonBones运行时才能正确关联两者。
    """
    texture_json = json.loads(TEXTURE_JSON_PATH.read_text(encoding="utf-8"))
    texture_json["name"] = RUNTIME_NAME
    TEXTURE_JSON_PATH.write_text(json.dumps(texture_json, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def main() -> None:
    """
    主函数：生成骨骼数据并验证

    1. 构建完整的骨架数据
    2. 读取纹理图集JSON并验证完整性
    3. 将骨骼数据写入sk2.json
    4. 更新texture2.json的根名称
    """
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
