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
# 主骨骼动画定义函数
# ============================================================

def main_idle() -> dict:
    """
    生成无球站立动画

    角色自然站立，身体轻微上下浮动，头部微动，
    双手自然下垂并轻微摆动。24帧循环，淡入0.1秒。

    返回:
        idle动画定义
    """
    duration = 24
    bones = [
        track("body", translate=translate_frames((8, 0, 0), (8, 0, -2), (8, 0, 0))),
        track("head", translate=translate_frames((8, 0, 0), (8, 1, -3), (8, 0, 0)), rotate=rotate_frames((8, 0), (8, -3), (8, 0))),
        track("left hand", translate=translate_frames((8, 0, 0), (8, -2, -2), (8, 0, 0)), rotate=rotate_frames((8, 4), (8, 12), (8, 4))),
        track("right hand", translate=translate_frames((8, 0, 0), (8, 2, -1), (8, 0, 0)), rotate=rotate_frames((8, -4), (8, -12), (8, -4))),
    ]
    return animation("idle", duration, loop=True, bones=bones, slots=loop_pose(duration, with_ball=False), fade=0.1)


def main_run() -> dict:
    """
    生成无球跑步动画

    角色奔跑时身体上下弹跳，双腿交替迈步，双臂自然摆动。
    18帧循环，节奏较快以体现运动感。

    返回:
        run动画定义
    """
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
    """
    生成持球站立动画（with_ball = wb）

    角色持球站立，左手托球上下弹动，身体轻微摇晃。
    包含"floor0"事件（球触地时机）。18帧循环。

    返回:
        idle_wb动画定义
    """
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
    """
    生成持球跑步动画

    角色持球奔跑，球在手中上下弹跳，身体前倾。
    包含"floor"事件。14帧循环，节奏紧凑。

    返回:
        run_wb动画定义
    """
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
    """
    生成跳跃类动画的通用模板

    跳跃动画包含多个阶段（jump、fly1~fly5），每个阶段的上升高度和旋转角度不同，
    通过参数组合生成不同的变体。角色在空中双臂上举，双腿蜷缩。

    参数:
        name: 动画名称
        with_ball: 是否持球
        duration: 动画帧数
        lift: 上升高度（像素值，越大跳得越高）
        rotate: 空中旋转角度

    返回:
        跳跃动画定义
    """
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
    """
    生成落地动画

    角色落地时身体先压缩再弹回，模拟缓冲效果。
    8帧，先下压后回弹，同时身体略微变扁再恢复。

    参数:
        name: 动画名称
        with_ball: 是否持球

    返回:
        落地动画定义
    """
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
    """
    生成投篮落地动画

    角色在空中完成投篮动作后落地。动画分两阶段：
    1. 前5帧：准备投篮姿态，手臂后摆蓄力
    2. 后5帧：投篮出手，球飞出，触发"throw"事件

    球在第5帧后从显示中消失（display_value切换为-1）。

    返回:
        throw_land动画定义
    """
    duration = 10
    slots = with_ball_slots(duration)
    # 球在前5帧显示，后5帧隐藏（模拟投出）
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
    """
    生成扣篮动画

    角色起跳后单手或双手将球扣入篮筐。
    动画包含蓄力阶段和扣篮阶段，在指定帧触发"dunk"事件。
    球和球前景插槽在扣篮出手后隐藏。

    参数:
        name: 动画名称（dunk1/dunk2/dunk3）
        duration: 动画总帧数
        windup: 蓄力阶段的上升高度
        reach: 扣篮时的最高到达高度

    返回:
        扣篮动画定义
    """
    event_at = max(7, duration - 5)
    slots = with_ball_slots(duration, front=True)
    # 球在扣篮出手后隐藏
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
    """
    生成抢断动画

    角色快速伸出右手试图抢夺对手的球。
    12帧，右手快速前伸后收回，在第7帧触发"action"事件。

    返回:
        steal动画定义
    """
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
    """
    生成假动作或盖帽的起始/结束动画

    假动作（pump）：角色双手持球向上抬起再放下，用于晃过防守者。
    盖帽（block）：角色双手上举试图阻挡对方投篮，眼睛和特效会激活。

    参数:
        name: 动画名称
        pump: True=假动作, False=盖帽
        start: True=起始阶段, False=结束阶段

    返回:
        假动作/盖帽动画定义
    """
    duration = 7
    slots = with_ball_slots(duration) if pump else hidden_optional_slots(duration)
    if not pump:
        # 盖帽时激活眼睛和眩晕特效
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
        # 假动作时球也跟着上下移动
        bones.append(track("ball", translate=translate_frames((duration, 0, -30 if start else 0)), scale=scale_frames((duration, 1.18 if start else 1, 0.72 if start else 1))))
    return animation(name, duration, bones=bones, slots=slots, fade=0.01)


def dash(name: str, *, with_ball: bool) -> dict:
    """
    生成冲刺动画

    角色快速向前冲刺，身体前倾，双腿大步迈开。
    8帧循环，激活眩晕特效作为速度线条。

    参数:
        name: 动画名称
        with_ball: 是否持球

    返回:
        冲刺动画定义
    """
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
    """
    生成挖球动画

    角色蹲下用挖掘手和挖掘腿尝试从对手手中挖球。
    三个阶段（dig1/dig2/dig3）的伸展距离递增。

    参数:
        name: 动画名称
        phase: 挖球阶段（1/2/3），越大伸展越远

    返回:
        挖球动画定义
    """
    duration = 8
    slots = hidden_optional_slots(duration)
    # 激活挖掘手和挖掘腿插槽
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
    """
    生成眩晕动画

    角色被击晕后身体左右摇晃，眼睛显示眩晕图案，
    头顶有旋转的星星特效。20帧循环。

    返回:
        stun动画定义
    """
    duration = 20
    slots = hidden_optional_slots(duration)
    # 激活眩晕眼睛、眩晕特效和星星特效
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
    """
    生成情绪动画（开心或悲伤）

    开心（happiness）：角色身体上抬，双手张开。
    悲伤（sad/sad0）：角色身体下沉，头部低垂，显示悲伤特效。

    参数:
        name: 动画名称
        happy: True=开心, False=悲伤

    返回:
        情绪动画定义
    """
    duration = 24
    bend = -5 if happy else 7
    lift = -4 if happy else 2
    slots = hidden_optional_slots(duration)
    if not happy:
        # 悲伤时激活特效（值5对应眼泪）
        slots[5] = slot_track("effects stun", display_value=5, duration=duration)
    bones = [
        track("body", translate=translate_frames((12, 0, lift), (12, 0, 0))),
        track("head", translate=translate_frames((12, 0, lift - 2), (12, 0, 0)), rotate=rotate_frames((12, bend), (12, 0))),
        track("left hand", rotate=rotate_frames((12, -20 if happy else 18), (12, 0))),
        track("right hand", rotate=rotate_frames((12, 20 if happy else -18), (12, 0))),
    ]
    return animation(name, duration, loop=True, bones=bones, slots=slots, fade=0.1)


def mega(name: str, *, end: bool = False, fly: bool = False) -> dict:
    """
    生成超级扣篮动画

    超级扣篮是游戏中最华丽的扣篮动作，角色飞到极高位置后猛烈扣下。
    包含三种变体：
    - megadunk：扣篮主体动作，触发"mega"事件
    - megadunk_fly：空中飞行阶段，循环播放
    - megadunk_end：扣篮结束落地

    参数:
        name: 动画名称
        end: 是否为结束阶段
        fly: 是否为飞行阶段（循环播放）

    返回:
        超级扣篮动画定义
    """
    duration = 18 if not end else 12
    slots = with_ball_slots(duration, front=True)
    # 激活眼睛和火焰特效（值7对应火焰）
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
    """
    生成超级扣篮的起始/中间/结束阶段动画（md = mega dunk的缩写）

    超级扣篮分为三个阶段，每个阶段有持球和无球两种变体：
    - md_start/md_start_wb：起始蓄力阶段（7帧）
    - md_mid/md_mid_wb：中间飞行阶段（14帧，循环）
    - md_end/md_end_wb：结束落地阶段（7帧）

    参数:
        name: 动画名称
        with_ball: 是否持球
        start: 是否为起始阶段
        end: 是否为结束阶段

    返回:
        超级扣篮阶段动画定义
    """
    duration = 7 if start or end else 14
    slots = with_ball_slots(duration) if with_ball else hidden_optional_slots(duration)
    # 激活眼睛和特效
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


# ============================================================
# 骨骼和子骨骼构建函数
# ============================================================

def main_armature() -> dict:
    """
    构建主骨骼（playerSmall）的完整定义

    主骨骼包含：
    - 13个骨骼节点（身体各部位+特效挂载点）
    - 13个插槽（控制各部位的显示层级和初始状态）
    - 皮肤配置（定义每个插槽的候选显示对象）
    - 42个动画（覆盖所有游戏动作）

    骨骼层级结构：
    body（根骨骼）
    ├── head
    │   ├── eyes（眩晕眼睛）
    │   └── effects stun star（星星特效）
    ├── left hand
    ├── right hand
    ├── left leg
    ├── right leg
    ├── dighand（挖掘手）
    ├── digleg（挖掘腿）
    ├── ball（手持球）
    ├── ball_front（前景球）
    └── effects stun（眩晕/火焰/眼泪特效）

    返回:
        主骨骼完整定义字典
    """
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
        slot("ball", display_index=-1),        # 默认隐藏
        slot("head"),
        slot("ball_front", display_index=-1),  # 默认隐藏
        slot("right hand"),
        slot("eyes", display_index=-1),        # 默认隐藏
        slot("dighand", display_index=-1),     # 默认隐藏
        slot("digleg", display_index=-1),      # 默认隐藏
        slot("effects stun", display_index=-1),      # 默认隐藏
        slot("effects stun star", display_index=-1), # 默认隐藏
    ]

    # 皮肤配置：定义每个插槽可以显示的所有候选项
    skin = [
        # 身体部件使用子骨骼（armature）类型，运行时通过切换动画名来选择角色
        skin_slot("left leg", [display("dbanims/LegsDB", type_="armature")]),
        skin_slot("left hand", [display("dbanims/LeftHandDB", type_="armature")]),
        skin_slot("body", [display("BodyDB", type_="armature")]),
        skin_slot("right leg", [display("dbanims/LegsDB", type_="armature")]),
        # 球使用图片类型
        skin_slot("ball", [display(".Game/ball/BallClip")]),
        skin_slot("head", [display("HeadsDB", type_="armature")]),
        skin_slot("ball_front", [display(".Game/ball/BallClip")]),
        skin_slot("right hand", [display("dbanims/RightHandDB", type_="armature")]),
        # 眼睛有两个候选项：眩晕眼睛和护盾动画
        skin_slot("eyes", [display("dbanims/eyes_stunned", type_="armature"), display("shield_animation_01", type_="armature")]),
        skin_slot("dighand", [display("dbanims/LeftHandDB2", type_="armature")]),
        skin_slot("digleg", [display("dbanims/LegsDB2", type_="armature")]),
        # 特效插槽包含多种特效候选项
        skin_slot(
            "effects stun",
            [
                display("dbanims/kuynya_01"),                            # 眩晕螺旋
                display("dbanims/backwind_01", type_="armature"),        # 背风动画
                display("dbanims/circles1"),                              # 圆圈1
                display("dbanims/circle2"),                               # 圆圈2
                display("dbanims/circle3"),                               # 圆圈3
                display("dbanims/tears_01", type_="armature"),           # 眼泪
                display("dbanims/CupDB", type_="armature"),              # 奖杯
                display("man_fire_01", type_="armature"),                # 火焰
            ],
        ),
        skin_slot("effects stun star", [display("dbanims/star_0123")]),  # 眩晕星星
    ]

    # 组装所有动画
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
            "note": "项目自研的DBLite兼容骨骼数据。运行时名称保持稳定以确保游戏逻辑兼容性。",
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
