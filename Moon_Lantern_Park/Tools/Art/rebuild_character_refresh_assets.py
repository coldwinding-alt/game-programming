"""
角色换装刷新资源重建脚本

本脚本负责为游戏中部分角色生成换装后的视觉资源。与rebuild_active_character_assets.py
不同，本脚本不是简单地将原始素材贴到纹理图集，而是基于模板部件进行主题化重新着色，
从而为新角色（REAPER、GHOST CLOWN、SKULL PIRATE）创建独特的身体外观。

工作流程：
1. 从 ArtSource/CharacterRefresh/ 读取新角色的图标素材
2. 对新角色的头部：去除背景后适配到DragonBones纹理图集和肖像图集的对应插槽
3. 对身体/腿/手部：基于现有角色的模板轮廓，根据主题配色方案重新着色
   - REAPER使用blackcat（黑猫）的模板形状
   - GHOST CLOWN使用scarecrow（稻草人）的模板形状
   - SKULL PIRATE混合使用witch（女巫）和scarecrow的模板形状
4. 重新着色时会保留原始形状的轮廓和明暗关系，只改变颜色
5. 同时更新肖像图集（portraits_ui.png）和DragonBones纹理图集（texture2.png）
6. 在 tmp/ 目录下生成预览拼图

支持的3个换装角色：
- REAPER（死神）：暗灰色身体 + 绿色装饰
- GHOST CLOWN（幽灵小丑）：红绿条纹 + 金色装饰
- SKULL PIRATE（骷髅海盗）：深蓝海军色 + 红金装饰

依赖：Pillow (PIL)
"""
from __future__ import annotations

import json
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


# 项目根目录
REPO_ROOT = Path(__file__).resolve().parents[2]

# 角色换装原始素材目录
CHARACTER_SOURCE_DIR = REPO_ROOT / "ArtSource" / "CharacterRefresh"

# 临时预览输出目录
TMP_DIR = REPO_ROOT / "tmp" / "character_refresh"

# DragonBones纹理图集路径
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "DragonBones"
PORTRAITS_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "Portraits"

TEXTURE_PATH = DRAGON_BONES_DIR / "texture2.png"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"
PORTRAITS_PATH = PORTRAITS_DIR / "portraits_ui.png"
PORTRAITS_JSON_PATH = PORTRAITS_DIR / "portraits_ui.json"

# 身体部件重着色时使用的轮廓颜色（深紫黑色）
OUTLINE_COLOR = (17, 14, 18)

# DragonBones纹理图集的目标像素密度（像素/单位）
DRAGON_BONES_TARGET_PIXELS_PER_UNIT = 2.0


@dataclass(frozen=True)
class CharacterRefresh:
    """
    角色换装配置

    属性:
        internal_id: 角色内部标识符（与DragonBones中的命名一致）
        visible_name: 角色显示名称
        portrait_source: 肖像素材文件名
        gameplay_source: 游戏内素材文件名
        body_theme: 身体主题配色方案名称（"reaper"/"clown"/"pirate"）
        body_style_source: 身体模板来源角色的ID
        leg_style_source: 腿部模板来源角色的ID
        hand_style_source: 手部模板来源角色的ID
        portrait_scale: 肖像缩放比例
        portrait_anchor_y: 肖像垂直锚点
        portrait_anchor_x: 肖像水平锚点
        gameplay_scale: 游戏内头部缩放比例
        gameplay_anchor_y: 游戏内头部垂直锚点
        gameplay_anchor_x: 游戏内头部水平锚点
        gameplay_allow_overflow: 是否允许头部超出画布边界
    """
    internal_id: str
    visible_name: str
    portrait_source: str
    gameplay_source: str
    body_theme: str
    body_style_source: str
    leg_style_source: str
    hand_style_source: str
    portrait_scale: float
    portrait_anchor_y: float
    portrait_anchor_x: float
    gameplay_scale: float
    gameplay_anchor_y: float
    gameplay_anchor_x: float
    gameplay_allow_overflow: bool = False


# 换装角色配置列表
REPLACEMENTS = (
    CharacterRefresh(
        internal_id="pumpkin",
        visible_name="REAPER",
        portrait_source="reaper_icon_source.png",
        gameplay_source="reaper_icon_source.png",
        body_theme="reaper",
        body_style_source="blackcat",
        leg_style_source="blackcat",
        hand_style_source="blackcat",
        portrait_scale=0.99,
        portrait_anchor_y=0.56,
        portrait_anchor_x=0.55,
        gameplay_scale=1.18,
        gameplay_anchor_y=0.72,
        gameplay_anchor_x=0.54,
        gameplay_allow_overflow=True,
    ),
    CharacterRefresh(
        internal_id="frankenstein",
        visible_name="GHOST CLOWN",
        portrait_source="ghost_clown_icon_source.png",
        gameplay_source="ghost_clown_icon_source.png",
        body_theme="clown",
        body_style_source="scarecrow",
        leg_style_source="scarecrow",
        hand_style_source="scarecrow",
        portrait_scale=0.98,
        portrait_anchor_y=0.58,
        portrait_anchor_x=0.5,
        gameplay_scale=1.2,
        gameplay_anchor_y=0.74,
        gameplay_anchor_x=0.51,
        gameplay_allow_overflow=True,
    ),
    CharacterRefresh(
        internal_id="mummy",
        visible_name="SKULL PIRATE",
        portrait_source="skull_pirate_icon_source.png",
        gameplay_source="skull_pirate_icon_source.png",
        body_theme="pirate",
        body_style_source="witch",
        leg_style_source="scarecrow",
        hand_style_source="scarecrow",
        portrait_scale=0.97,
        portrait_anchor_y=0.58,
        portrait_anchor_x=0.5,
        gameplay_scale=1.4,
        gameplay_anchor_y=0.74,
        gameplay_anchor_x=0.5,
        gameplay_allow_overflow=True,
    ),
)


def load_json(path: Path) -> dict:
    """读取并解析JSON文件"""
    return json.loads(path.read_text(encoding="utf-8"))


def save_json(path: Path, data: dict) -> None:
    """
    将字典数据保存为JSON文件

    使用紧凑格式（无空格分隔符）减小文件体积，
    确保中文字符直接输出而非转义。

    参数:
        path: 输出文件路径
        data: 要保存的字典数据
    """
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def atlas_lookup(atlas_json: dict) -> dict[str, dict]:
    """
    从纹理图集JSON中构建子纹理名称到配置的查找表

    参数:
        atlas_json: 纹理图集的JSON配置

    返回:
        子纹理名称 -> 子纹理配置的映射
    """
    return {item["name"]: item for item in atlas_json["SubTexture"]}


def trim_alpha(image: Image.Image) -> Image.Image:
    """裁剪图片，去除四周完全透明的区域"""
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def border_key_color(image: Image.Image) -> tuple[int, int, int]:
    """
    检测图片边缘的主要颜色（色度键颜色）

    通过采样图片四周边缘像素，统计出现频率最高的颜色。

    参数:
        image: 输入的RGBA图片

    返回:
        边缘主色的RGB元组
    """
    src = image.convert("RGBA")
    pixels = src.load()
    samples: list[tuple[int, int, int]] = []

    for x in range(src.width):
        samples.append(pixels[x, 0][:3])
        samples.append(pixels[x, src.height - 1][:3])

    for y in range(1, src.height - 1):
        samples.append(pixels[0, y][:3])
        samples.append(pixels[src.width - 1, y][:3])

    return Counter(samples).most_common(1)[0][0]


def remove_chroma_background(
    image: Image.Image,
    *,
    transparent_threshold: float = 16.0,
    opaque_threshold: float = 84.0,
) -> Image.Image:
    """
    去除图片的纯色背景（色度键抠图）

    使用基于边缘色键的抠图流程，并采用较柔和的边缘过渡参数。

    在半透明边缘区域执行去溢色处理（despill），
    防止背景的霓虹色渗透到角色轮廓中。

    参数:
        image: 输入的RGBA图片（带纯色背景）
        transparent_threshold: 完全透明的颜色距离阈值
        opaque_threshold: 完全不透明的颜色距离阈值

    返回:
        去除背景后的RGBA图片
    """
    src = image.convert("RGBA")
    key = border_key_color(src)
    pixels = src.load()
    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    out_pixels = out.load()
    span = max(0.0001, opaque_threshold - transparent_threshold)

    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = pixels[x, y]
            if a <= 0:
                continue

            distance = ((r - key[0]) ** 2 + (g - key[1]) ** 2 + (b - key[2]) ** 2) ** 0.5
            if distance <= transparent_threshold:
                continue

            if distance >= opaque_threshold:
                out_pixels[x, y] = (r, g, b, a)
                continue

            alpha_scale = (distance - transparent_threshold) / span
            out_alpha = int(round(a * alpha_scale))
            if out_alpha <= 0:
                continue

            # 去溢色处理：减少轮廓边缘处的背景色渗透
            despill = 0.42 * (1.0 - alpha_scale)
            out_pixels[x, y] = (
                int(round(r * (1.0 - despill) + key[0] * despill * 0.18)),
                int(round(g * (1.0 - despill) + key[1] * despill * 0.18)),
                int(round(b * (1.0 - despill) + key[2] * despill * 0.18)),
                out_alpha,
            )

    return trim_alpha(out)


def prepare_source_image(source_name: str) -> Image.Image:
    """
    准备角色源图片：自动检测是否需要去除背景

    如果图片的Alpha通道全为255（完全不透明），说明是带纯色背景的截图，
    需要去除背景。否则直接裁剪透明边缘。

    参数:
        source_name: 源图片文件名

    返回:
        处理后的RGBA图片
    """
    source_path = CHARACTER_SOURCE_DIR / source_name
    source = Image.open(source_path).convert("RGBA")
    alpha_min, alpha_max = source.getchannel("A").getextrema()
    if alpha_min == 255 and alpha_max == 255:
        return remove_chroma_background(source)

    return trim_alpha(source)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """
    使用预乘Alpha的方式对图片进行高质量缩放

    逐像素实现预乘Alpha缩放，确保半透明边缘在缩放后保持正确。

    参数:
        image: 输入的RGBA图片
        size: 目标尺寸 (宽度, 高度)

    返回:
        缩放后的RGBA图片
    """
    if image.size == size:
        return image.copy()

    src = image.convert("RGBA")
    src_data = src.load()
    temp = Image.new("RGBA", src.size, (0, 0, 0, 0))
    temp_data = temp.load()

    # 预乘Alpha
    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = src_data[x, y]
            temp_data[x, y] = (
                int(round(r * a / 255.0)),
                int(round(g * a / 255.0)),
                int(round(b * a / 255.0)),
                a,
            )

    resized = temp.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out_data = out.load()
    resized_data = resized.load()

    # 还原Alpha
    for y in range(size[1]):
        for x in range(size[0]):
            r, g, b, a = resized_data[x, y]
            if a <= 0:
                out_data[x, y] = (0, 0, 0, 0)
                continue

            scale = 255.0 / a
            out_data[x, y] = (
                min(255, int(round(r * scale))),
                min(255, int(round(g * scale))),
                min(255, int(round(b * scale))),
                a,
            )

    return out


def fit_subject_to_canvas(
    source: Image.Image,
    canvas_size: tuple[int, int],
    *,
    scale: float = 1.0,
    margin: float = 0.06,
    anchor_x: float = 0.5,
    anchor_y: float = 0.5,
    allow_overflow: bool = False,
) -> Image.Image:
    """
    将角色主体图片适配到指定尺寸的画布上

    在常规主体适配流程基础上支持允许角色头部超出画布边界。

    参数:
        source: 角色主体的RGBA图片
        canvas_size: 目标画布尺寸 (宽度, 高度)
        scale: 额外的缩放系数
        margin: 画布边缘的留白比例
        anchor_x: 水平锚点
        anchor_y: 垂直锚点
        allow_overflow: 是否允许超出画布边界

    返回:
        包含适配后角色的画布图片
    """
    source = trim_alpha(source.convert("RGBA"))
    available_width = max(1, int(round(canvas_size[0] * (1.0 - margin * 2.0))))
    available_height = max(1, int(round(canvas_size[1] * (1.0 - margin * 2.0))))
    ratio = min(available_width / source.width, available_height / source.height) * scale
    target_size = (
        max(1, int(round(source.width * ratio))),
        max(1, int(round(source.height * ratio))),
    )

    fitted = premultiplied_resize(source, target_size)
    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    if allow_overflow:
        # 允许超出：直接根据锚点计算位置，不约束在边距范围内
        paste_x = int(round((canvas_size[0] - fitted.width) * anchor_x))
        paste_y = int(round((canvas_size[1] - fitted.height) * anchor_y))
    else:
        # 不允许超出：确保角色在边距约束范围内
        min_x = int(round(canvas_size[0] * margin))
        max_x = canvas_size[0] - fitted.width - min_x
        min_y = int(round(canvas_size[1] * margin))
        max_y = canvas_size[1] - fitted.height - min_y
        paste_x = min_x if max_x <= min_x else int(round(min_x + (max_x - min_x) * anchor_x))
        paste_y = min_y if max_y <= min_y else int(round(min_y + (max_y - min_y) * anchor_y))
    canvas.alpha_composite(fitted, (paste_x, paste_y))
    return canvas


def ensure_dragon_bones_resolution(texture: Image.Image, texture_json: dict) -> tuple[Image.Image, dict]:
    """
    确保DragonBones纹理图集的像素密度符合目标值

    如果当前纹理的pixelsPerUnit与目标值不匹配，
    会对整张纹理图集和所有子纹理坐标进行等比缩放。

    参数:
        texture: 纹理图集图片
        texture_json: 纹理图集的JSON配置

    返回:
        (调整后的纹理图片, 调整后的JSON配置) 元组
    """
    current_pixels_per_unit = float(texture_json.get("pixelsPerUnit", 1.0))
    scale_factor = DRAGON_BONES_TARGET_PIXELS_PER_UNIT / max(0.0001, current_pixels_per_unit)
    if abs(scale_factor - 1.0) <= 0.0001:
        texture_json["pixelsPerUnit"] = DRAGON_BONES_TARGET_PIXELS_PER_UNIT
        return texture, texture_json

    # 缩放整张纹理
    texture = premultiplied_resize(
        texture,
        (int(round(texture.width * scale_factor)), int(round(texture.height * scale_factor))),
    )

    # 缩放所有子纹理的坐标和尺寸
    for sub in texture_json["SubTexture"]:
        for key in ("x", "y", "width", "height"):
            sub[key] = int(round(sub[key] * scale_factor))

    texture_json["pixelsPerUnit"] = DRAGON_BONES_TARGET_PIXELS_PER_UNIT
    return texture, texture_json


def crop_subtexture(texture: Image.Image, sub: dict) -> Image.Image:
    """
    从纹理图集中裁剪指定子纹理区域

    参数:
        texture: 完整的纹理图集图片
        sub: 子纹理配置字典

    返回:
        裁剪后的子纹理图片
    """
    return texture.crop((sub["x"], sub["y"], sub["x"] + sub["width"], sub["y"] + sub["height"]))


def paste_subtexture(texture: Image.Image, sub: dict, image: Image.Image) -> None:
    """
    将图片粘贴到纹理图集中的指定子纹理位置

    先清除目标区域的旧内容，再粘贴新内容。

    参数:
        texture: 完整的纹理图集图片（会被原地修改）
        sub: 子纹理配置字典
        image: 要粘贴的新图片
    """
    cleared = Image.new("RGBA", (sub["width"], sub["height"]), (0, 0, 0, 0))
    texture.paste(cleared, (sub["x"], sub["y"]))
    texture.alpha_composite(image.convert("RGBA"), (sub["x"], sub["y"]))


def luminance(pixel: tuple[int, int, int, int]) -> float:
    """
    计算像素的相对亮度值

    使用ITU-R BT.709标准的亮度系数（R:0.2126, G:0.7152, B:0.0722），
    将RGB颜色转换为0.0~1.0的亮度值。用于在重着色时保留原始形状的明暗关系。

    参数:
        pixel: RGBA像素元组

    返回:
        0.0~1.0的亮度值
    """
    return (0.2126 * pixel[0] + 0.7152 * pixel[1] + 0.0722 * pixel[2]) / 255.0


def mix_color(base: tuple[int, int, int], shade: float) -> tuple[int, int, int]:
    """
    将基础颜色与明暗系数混合

    将基础颜色的每个通道乘以明暗系数，并限制在0.55~1.25范围内，
    防止过暗或过亮。用于根据原始形状的明暗关系调整主题颜色。

    参数:
        base: 基础颜色RGB
        shade: 明暗系数（1.0为原始亮度）

    返回:
        混合后的RGB颜色
    """
    shade = max(0.55, min(1.25, shade))
    return tuple(max(0, min(255, int(round(channel * shade)))) for channel in base)


def is_outline(alpha: list[list[int]], x: int, y: int) -> bool:
    """
    判断像素是否为轮廓边缘

    如果一个非透明像素的四个相邻像素中有任何一个为透明，
    则该像素被判定为轮廓边缘。

    参数:
        alpha: 二维Alpha值数组（按行存储）
        x: 像素X坐标
        y: 像素Y坐标

    返回:
        是否为轮廓边缘
    """
    if alpha[y][x] <= 0:
        return False

    width = len(alpha[0])
    height = len(alpha)
    for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0)):
        nx = x + dx
        ny = y + dy
        if nx < 0 or nx >= width or ny < 0 or ny >= height or alpha[ny][nx] <= 0:
            return True
    return False


def design_color(theme: str, part: str, xn: float, yn: float) -> tuple[int, int, int]:
    """
    根据主题、部件类型和归一化坐标计算设计颜色

    这是角色主题化着色的核心函数。根据角色的相对位置（xn, yn均为0~1）
    和部件类型（body/leg/hand），返回对应的主题颜色。

    每个主题都有独特的配色方案：
    - reaper（死神）：暗灰身体 + 绿色能量装饰 + 白色鞋子
    - clown（小丑）：红绿条纹身体 + 金色领子 + 花色鞋子
    - pirate（海盗）：深蓝海军身体 + 红色帽子 + 金色纽扣装饰

    参数:
        theme: 主题名称（"reaper"/"clown"/"pirate"）
        part: 部件类型（"body"/"leg"/"left_hand"/"right_hand"等）
        xn: 水平归一化坐标（0.0=左边缘, 1.0=右边缘）
        yn: 垂直归一化坐标（0.0=上边缘, 1.0=下边缘）

    返回:
        设计颜色的RGB元组
    """
    if theme == "reaper":
        if part == "body":
            if yn < 0.18:
                return (170, 176, 188)          # 浅灰色领口
            if 0.33 < yn < 0.56 and 0.41 < xn < 0.59:
                return (56, 214, 144)            # 绿色能量核心装饰
            if xn < 0.18 or xn > 0.82:
                return (118, 124, 135)           # 侧边浅灰色
            if yn > 0.76:
                return (28, 34, 40)              # 底部深色
            return (33, 36, 43)                  # 主体深灰色
        if part == "leg":
            if yn > 0.7:
                return (225, 225, 232)           # 白色鞋子
            if 0.28 < yn < 0.48 and 0.34 < xn < 0.66:
                return (56, 214, 144)            # 绿色膝盖装饰
            return (24, 27, 34)                  # 深灰色裤腿
        # 手部默认配色
        if yn < 0.3:
            return (96, 106, 118)                # 浅灰色手腕
        if 0.24 < yn < 0.5 and 0.42 < xn < 0.58:
            return (56, 214, 144)                # 绿色手部装饰
        return (34, 38, 45)                      # 深灰色手部主体

    if theme == "clown":
        if part == "body":
            if yn < 0.22:
                # 领口：白色和红色交替条纹
                return (252, 231, 191) if int(xn * 6) % 2 == 0 else (212, 56, 60)
            if 0.32 < yn < 0.56 and 0.38 < xn < 0.62:
                return (245, 200, 70)            # 金色纽扣装饰
            if yn > 0.76:
                # 底部：左侧白色，右侧青绿色
                return (248, 240, 221) if xn < 0.5 else (94, 186, 177)
            # 主体：左侧红色，右侧青绿色
            return (206, 46, 54) if xn < 0.5 else (77, 177, 168)
        if part == "leg":
            if yn > 0.7:
                return (241, 240, 232)           # 白色鞋子
            if xn < 0.28:
                return (206, 46, 54)             # 红色袜子
            if xn > 0.72:
                return (77, 177, 168)            # 青绿色袜子
            return (252, 231, 191)               # 米白色裤腿
        # 手部默认配色
        if yn < 0.28:
            return (245, 200, 70)                # 金色手腕
        if xn < 0.5:
            return (243, 244, 240)               # 白色左手
        return (230, 233, 239)                   # 浅灰右手

    if theme == "pirate":
        if part == "body":
            if yn < 0.22:
                return (189, 48, 49)             # 红色帽子
            if 0.15 < xn < 0.26 or 0.74 < xn < 0.85:
                return (219, 182, 74)            # 金色肩章
            if 0.34 + yn * 0.2 < xn < 0.48 + yn * 0.2:
                return (98, 66, 39)              # 棕色腰带（斜向）
            if 0.42 < xn < 0.58 and 0.26 < yn < 0.66 and int((yn - 0.26) * 14) % 3 == 0:
                return (225, 183, 69)            # 金色纽扣装饰
            if yn > 0.76:
                return (32, 39, 54)              # 深色底边
            return (32, 53, 99)                  # 深蓝色海军身体
        if part == "leg":
            if yn > 0.7:
                return (234, 230, 219)           # 浅色鞋子
            if 0.25 < xn < 0.48:
                return (189, 48, 49)             # 红色裤缝装饰
            if 0.56 < xn < 0.74 and 0.22 < yn < 0.46:
                return (225, 183, 69)            # 金色膝盖装饰
            return (26, 31, 42)                  # 深蓝色裤腿
        # 手部默认配色
        if yn < 0.24:
            return (38, 56, 98)                  # 深蓝色手腕
        return (229, 223, 210)                   # 浅色手部

    raise ValueError(f"Unknown theme: {theme}")


def build_thematic_part(template: Image.Image, theme: str, part: str) -> Image.Image:
    """
    基于模板形状和主题配色构建新的身体部件

    这是实现角色换装视觉效果的核心函数。它保留模板形状的轮廓和明暗关系，
    但将填充颜色替换为主题配色方案。

    处理流程：
    1. 遍历模板图片的每个非透明像素
    2. 如果像素在轮廓边缘，使用统一的轮廓颜色
    3. 否则，根据像素在部件中的相对位置查询主题颜色
    4. 根据原始像素的亮度调整主题颜色的明暗
    5. 输出重新着色后的部件图片

    参数:
        template: 模板形状的RGBA图片（来自已有角色的部件）
        theme: 主题名称
        part: 部件类型

    返回:
        重新着色后的RGBA图片
    """
    src = template.convert("RGBA")
    width, height = src.size
    src_pixels = src.load()
    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    out_pixels = out.load()

    # 预构建Alpha值的二维数组，用于快速轮廓检测
    alpha_rows = [[src_pixels[x, y][3] for x in range(width)] for y in range(height)]

    bbox = src.getchannel("A").getbbox()
    if bbox is None:
        return out

    min_x, min_y, max_x, max_y = bbox
    range_x = max(1, max_x - min_x - 1)
    range_y = max(1, max_y - min_y - 1)

    for y in range(height):
        for x in range(width):
            r, g, b, a = src_pixels[x, y]
            if a <= 0:
                continue

            # 轮廓像素使用统一的深色轮廓
            if is_outline(alpha_rows, x, y):
                out_pixels[x, y] = (*OUTLINE_COLOR, a)
                continue

            # 计算归一化坐标并查询主题颜色
            xn = (x - min_x) / range_x
            yn = (y - min_y) / range_y
            base = design_color(theme, part, xn, yn)

            # 根据原始亮度调整颜色明暗，保留形状的立体感
            shade = 0.74 + luminance((r, g, b, a)) * 0.58
            recolored = mix_color(base, shade)
            out_pixels[x, y] = (*recolored, a)

    return out


def build_preview(
    texture: Image.Image,
    texture_lookup: dict[str, dict],
    portraits: Image.Image,
    portrait_lookup: dict[str, dict],
) -> None:
    """
    生成角色换装的预览拼图

    将所有换装角色的头部、身体部件和肖像排列在一个大图上，
    方便一次性检查所有换装效果。

    预览图包含三个区域：
    1. 游戏内头部预览（3倍放大）
    2. 游戏内身体/腿/手部件预览
    3. 肖像头部预览

    参数:
        texture: DragonBones纹理图集
        texture_lookup: 纹理图集查找表
        portraits: 肖像图集
        portrait_lookup: 肖像图集查找表
    """
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    preview = Image.new("RGBA", (1220, 700), (22, 22, 26, 255))
    draw = ImageDraw.Draw(preview)

    # 区域标题
    section_titles = (
        ("Gameplay Heads", 18, 14),
        ("Gameplay Body / Leg / Hands", 18, 252),
        ("Portraits", 18, 482),
    )
    for title, x, y in section_titles:
        draw.text((x, y), title, fill=(236, 236, 241, 255))

    start_x = 22
    step_x = 392
    body_rows = ("body", "leg", "left_hand", "right_hand")

    for index, replacement in enumerate(REPLACEMENTS):
        x_base = start_x + index * step_x
        draw.text((x_base, 38), replacement.visible_name, fill=(236, 236, 241, 255))

        # 头部预览（3倍放大）
        head_name = f"custom_head_{replacement.internal_id}"
        head_crop = trim_alpha(crop_subtexture(texture, texture_lookup[head_name]))
        head_preview = premultiplied_resize(head_crop, (head_crop.width * 3, head_crop.height * 3))
        preview.alpha_composite(head_preview, (x_base + 18, 68))

        # 身体部件预览
        for row_index, part in enumerate(body_rows):
            part_name = f"custom_{part}_{replacement.internal_id}"
            crop = trim_alpha(crop_subtexture(texture, texture_lookup[part_name]))
            canvas = fit_subject_to_canvas(crop, (160, 120), margin=0.06)
            preview.alpha_composite(canvas, (x_base + row_index * 90, 294))
            draw.text((x_base + row_index * 90, 274), part, fill=(180, 180, 186, 255))

        # 肖像预览
        portrait_name = f"custom_head_{replacement.internal_id}"
        portrait_crop = trim_alpha(crop_subtexture(portraits, portrait_lookup[portrait_name]))
        portrait_canvas = fit_subject_to_canvas(portrait_crop, (260, 180), margin=0.02)
        preview.alpha_composite(portrait_canvas, (x_base, 510))

    preview.save(TMP_DIR / "character_refresh_preview.png")


def update_portraits() -> tuple[Image.Image, dict[str, dict]]:
    """
    更新肖像图集

    为每个换装角色生成新的头部肖像并写入肖像图集。

    返回:
        (更新后的肖像图片, 肖像查找表) 元组
    """
    portraits = Image.open(PORTRAITS_PATH).convert("RGBA")
    portrait_json = load_json(PORTRAITS_JSON_PATH)
    lookup = atlas_lookup(portrait_json)

    for replacement in REPLACEMENTS:
        source = prepare_source_image(replacement.portrait_source)
        target_name = f"custom_head_{replacement.internal_id}"
        sub = lookup[target_name]
        rendered = fit_subject_to_canvas(
            source,
            (sub["width"], sub["height"]),
            scale=replacement.portrait_scale,
            margin=0.05,
            anchor_x=replacement.portrait_anchor_x,
            anchor_y=replacement.portrait_anchor_y,
        )
        paste_subtexture(portraits, sub, rendered)

    portraits.save(PORTRAITS_PATH)
    return portraits, lookup


def update_dragon_bones() -> tuple[Image.Image, dict[str, dict]]:
    """
    更新DragonBones纹理图集

    为每个换装角色更新头部和身体部件：
    - 头部：从新角色的图标素材中提取，适配到对应插槽
    - 身体/腿/手：从模板角色的已有部件重新着色生成

    返回:
        (更新后的纹理图片, 纹理查找表) 元组
    """
    texture = Image.open(TEXTURE_PATH).convert("RGBA")
    texture_json = load_json(TEXTURE_JSON_PATH)
    texture, texture_json = ensure_dragon_bones_resolution(texture, texture_json)
    lookup = atlas_lookup(texture_json)
    source_texture = texture.copy()

    for replacement in REPLACEMENTS:
        # 更新头部：从新素材适配
        source = prepare_source_image(replacement.gameplay_source)
        head_name = f"custom_head_{replacement.internal_id}"
        head_sub = lookup[head_name]
        rendered_head = fit_subject_to_canvas(
            source,
            (head_sub["width"], head_sub["height"]),
            scale=replacement.gameplay_scale,
            margin=0.02,
            anchor_x=replacement.gameplay_anchor_x,
            anchor_y=replacement.gameplay_anchor_y,
            allow_overflow=replacement.gameplay_allow_overflow,
        )
        paste_subtexture(texture, head_sub, rendered_head)

        # 更新身体部件：从模板重新着色
        for part in ("body", "leg", "left_hand", "right_hand", "dig_hand"):
            part_name = f"custom_{part}_{replacement.internal_id}"
            sub = lookup[part_name]

            # 根据部件类型选择对应的模板来源角色
            if part == "body":
                style_source = replacement.body_style_source
            elif part == "leg":
                style_source = replacement.leg_style_source
            else:
                style_source = replacement.hand_style_source

            # 从原始纹理中裁剪模板，重新着色后粘贴
            template_name = f"custom_{part}_{style_source}"
            template = crop_subtexture(source_texture, lookup[template_name])
            themed = build_thematic_part(template, replacement.body_theme, "leg" if part == "leg" else part)
            paste_subtexture(texture, sub, themed)

    texture.save(TEXTURE_PATH)
    save_json(TEXTURE_JSON_PATH, texture_json)
    return texture, lookup


def main() -> None:
    """
    主函数：执行角色换装资源的完整更新流程

    1. 更新肖像图集
    2. 更新DragonBones纹理图集
    3. 生成预览拼图
    """
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    portraits, portrait_lookup = update_portraits()
    texture, texture_lookup = update_dragon_bones()
    build_preview(texture, texture_lookup, portraits, portrait_lookup)
    print(f"Updated character refresh atlases from {CHARACTER_SOURCE_DIR}")
    print(f"Preview saved to {TMP_DIR / 'character_refresh_preview.png'}")


if __name__ == "__main__":
    main()
