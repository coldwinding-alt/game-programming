"""
颁奖界面UI资源生成脚本

本脚本负责生成游戏中颁奖/结算界面所需的UI美术资源，包括：
1. 奖项展示面板（awards_showcase_panel.png）：一个大型装饰性边框面板，
   用于展示比赛结果和奖项信息
2. 结果铭牌（awards_result_plaque.png）：一个水平横幅式的铭牌，
   用于显示比赛结果文字
3. 颁奖台底座（awards_podium_base.png）：包含三个高低错落的颁奖台台阶，
   分别对应第1、2、3名

视觉风格：
- 深海军蓝底色配金色边框，营造高端电竞/万圣节竞技氛围
- 青色和橙色的发光装饰线条
- 蝙蝠纹章作为装饰元素
- 带有噪声纹理和渐变效果的面板表面

所有图形均通过代码程序化绘制，不依赖外部图片素材。

依赖：Pillow (PIL)
"""
from __future__ import annotations

from pathlib import Path
from typing import Iterable, Tuple

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


# RGBA颜色类型
Color = Tuple[int, int, int, int]

# 项目根目录（Tools/Art/ 的上两级）
ROOT = Path(__file__).resolve().parents[2]

# UI资源输出目录，会被Unity运行时加载
OUTPUT_DIR = ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "Images" / "UI"

# 预定义的调色板颜色常量，统一管理UI的色彩方案
NAVY = (8, 17, 34, 255)           # 海军蓝（主背景色）
NAVY_DEEP = (4, 10, 20, 255)     # 深海军蓝（暗部渐变终点）
NAVY_LIGHT = (18, 40, 72, 255)   # 浅海军蓝（亮部）
CYAN = (72, 244, 255, 255)       # 青色（高亮强调色）
CYAN_SOFT = (84, 190, 255, 255)  # 柔和青色
GOLD = (244, 198, 96, 255)       # 金色（边框和装饰主色）
COPPER = (230, 121, 54, 255)     # 铜色（暖色装饰）
LIME = (182, 246, 84, 255)       # 青柠绿（第2名配色）
ORANGE = (255, 174, 74, 255)     # 橙色（暖色装饰）
WHITE = (255, 255, 255, 255)     # 纯白色
TRANSPARENT = (0, 0, 0, 0)       # 完全透明


def rounded_mask(size: tuple[int, int], radius: int, inset: int = 0) -> Image.Image:
    """
    生成圆角矩形的灰度遮罩

    创建一个指定尺寸的灰度图片，其中圆角矩形区域为白色（255），
    其余区域为黑色（0）。用于将其他图片裁剪为圆角矩形形状。

    参数:
        size: 遮罩尺寸 (宽度, 高度)
        radius: 圆角半径（像素）
        inset: 内缩距离（像素），遮罩边缘向内收缩的距离

    返回:
        灰度模式的圆角矩形遮罩图片
    """
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle(
        (inset, inset, size[0] - 1 - inset, size[1] - 1 - inset),
        radius=radius,
        fill=255,
    )
    return mask


def overlay(base: Image.Image, top: Image.Image, mask: Image.Image | None = None) -> Image.Image:
    """
    使用遮罩将顶层图片合成到底层图片上

    如果未指定遮罩，则使用顶层图片的Alpha通道作为遮罩。

    参数:
        base: 底层图片
        top: 顶层图片
        mask: 可选的灰度遮罩，白色区域显示顶层，黑色区域显示底层

    返回:
        合成后的图片
    """
    if mask is None:
        mask = top.split()[-1]
    return Image.composite(top, base, mask)


def vertical_gradient(size: tuple[int, int], top: Color, bottom: Color) -> Image.Image:
    """
    生成从上到下的垂直线性渐变图片

    在指定尺寸的画布上，从上到下线性插值生成渐变色。
    常用于面板的背景填充，营造深度感。

    参数:
        size: 画布尺寸 (宽度, 高度)
        top: 顶部颜色（RGBA元组）
        bottom: 底部颜色（RGBA元组）

    返回:
        渐变填充的RGBA图片
    """
    img = Image.new("RGBA", size)
    px = img.load()
    height = max(1, size[1] - 1)
    for y in range(size[1]):
        t = y / height
        color = tuple(int(top[i] * (1 - t) + bottom[i] * t) for i in range(4))
        for x in range(size[0]):
            px[x, y] = color
    return img


def radial_glow(size: tuple[int, int], inner: Color, outer: Color, squish_y: float = 1.0) -> Image.Image:
    """
    生成径向渐变发光效果

    从中心向外辐射的圆形渐变，使用平滑插值（smoothstep）实现柔和过渡。
    支持Y轴压缩（squish_y参数），可以生成椭圆形的光晕效果。

    参数:
        size: 画布尺寸 (宽度, 高度)
        inner: 中心颜色（RGBA元组）
        outer: 边缘颜色（RGBA元组）
        squish_y: Y轴压缩系数，>1时产生水平椭圆，<1时产生垂直椭圆

    返回:
        径向渐变的RGBA图片
    """
    width, height = size
    img = Image.new("RGBA", size)
    px = img.load()
    cx = (width - 1) / 2.0
    cy = (height - 1) / 2.0
    max_dist = max(1.0, min(width, height) * 0.5)
    for y in range(height):
        for x in range(width):
            dx = x - cx
            dy = (y - cy) / max(0.001, squish_y)
            dist = min(1.0, (dx * dx + dy * dy) ** 0.5 / max_dist)
            # 使用smoothstep函数实现平滑过渡：3t² - 2t³
            eased = dist * dist * (3.0 - 2.0 * dist)
            color = tuple(int(inner[i] * (1 - eased) + outer[i] * eased) for i in range(4))
            px[x, y] = color
    return img


def apply_noise(image: Image.Image, alpha: int = 16) -> None:
    """
    在图片上叠加噪声纹理，增加表面质感

    生成随机噪声并以半透明方式叠加到目标图片上，
    模拟金属或石材表面的细微颗粒感，避免纯色区域看起来过于平坦。

    参数:
        image: 目标图片（会被原地修改）
        alpha: 噪声的最大不透明度（0-255），值越大噪声越明显
    """
    noise = Image.effect_noise(image.size, 12).convert("L")
    noise = ImageOps.autocontrast(noise)
    tinted = Image.new("RGBA", image.size, (255, 255, 255, 0))
    tinted.putalpha(noise.point(lambda value: value * alpha // 255))
    image.alpha_composite(tinted)


def add_shadow(canvas: Image.Image, box: tuple[int, int, int, int], radius: int, offset: tuple[int, int], opacity: int) -> None:
    """
    在画布上添加圆角矩形投影效果

    在指定位置绘制一个带有高斯模糊的黑色阴影，营造元素浮起的立体感。

    参数:
        canvas: 目标画布（会被原地修改）
        box: 圆角矩形的边界框 (左, 上, 右, 下)
        radius: 圆角半径
        offset: 阴影偏移量 (x, y)
        opacity: 阴影不透明度（0-255）
    """
    shadow = Image.new("RGBA", canvas.size, TRANSPARENT)
    draw = ImageDraw.Draw(shadow)
    shifted = (box[0] + offset[0], box[1] + offset[1], box[2] + offset[0], box[3] + offset[1])
    draw.rounded_rectangle(shifted, radius=radius, fill=(0, 0, 0, opacity))
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=32))
    canvas.alpha_composite(shadow)


def add_line_glow(canvas: Image.Image, line_box: tuple[int, int, int, int], color: Color, blur: int) -> None:
    """
    在画布上添加发光线条效果

    绘制一个带有高斯模糊的彩色矩形，产生柔和的线条发光效果，
    常用于面板边缘的装饰性发光条纹。

    参数:
        canvas: 目标画布（会被原地修改）
        line_box: 线条矩形的边界框 (左, 上, 右, 下)
        color: 发光颜色（RGBA元组）
        blur: 高斯模糊半径
    """
    glow = Image.new("RGBA", canvas.size, TRANSPARENT)
    draw = ImageDraw.Draw(glow)
    draw.rounded_rectangle(line_box, radius=max(1, blur // 3), fill=color)
    canvas.alpha_composite(glow.filter(ImageFilter.GaussianBlur(blur)))


def draw_bat_crest(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float, fill: Color, outline: Color) -> None:
    """
    绘制蝙蝠纹章装饰图案

    蝙蝠纹章由展开的翅膀多边形和中心圆形徽章组成，
    是本游戏万圣节主题的核心视觉元素之一，用于装饰面板的顶部和底部。

    参数:
        draw: PIL的ImageDraw对象
        cx: 纹章中心的X坐标
        cy: 纹章中心的Y坐标
        scale: 整体缩放比例
        fill: 填充颜色（翅膀内部和内圆）
        outline: 轮廓颜色（翅膀边框和外圆）
    """
    # 蝙蝠翅膀的多边形顶点坐标（相对于中心点）
    wing = [
        (-120, 16),
        (-98, -12),
        (-70, -10),
        (-52, -38),
        (-26, -14),
        (0, -42),
        (26, -14),
        (52, -38),
        (70, -10),
        (98, -12),
        (120, 16),
        (70, 8),
        (24, 30),
        (0, 18),
        (-24, 30),
        (-70, 8),
    ]
    # 根据缩放系数和中心位置变换顶点坐标
    points = [(cx + int(x * scale), cy + int(y * scale)) for x, y in wing]
    draw.polygon(points, fill=fill, outline=outline)
    # 绘制中心的圆形徽章（外圈+内圈）
    orb_radius = int(20 * scale)
    draw.ellipse((cx - orb_radius, cy - orb_radius, cx + orb_radius, cy + orb_radius), fill=outline)
    draw.ellipse((cx - orb_radius + 6, cy - orb_radius + 6, cx + orb_radius - 6, cy + orb_radius - 6), fill=fill)


def draw_corner_plate(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], mirror_x: bool = False, mirror_y: bool = False) -> None:
    """
    绘制面板角落的金属装饰板

    四角装饰板是不规则多边形，带有金色边框和深色填充，
    通过mirror_x和mirror_y参数实现四个角落的镜像对称。

    参数:
        draw: PIL的ImageDraw对象
        box: 装饰板的边界框 (左, 上, 右, 下)
        mirror_x: 是否水平镜像（用于右侧角落）
        mirror_y: 是否垂直镜像（用于底部角落）
    """
    left, top, right, bottom = box
    width = right - left
    height = bottom - top
    # 装饰板的多边形顶点（不规则五边形形状）
    points = [
        (0, 0),
        (width, 0),
        (width - 28, 44),
        (width - 56, 64),
        (18, height),
        (0, height),
    ]
    # 根据镜像参数变换顶点
    transformed = []
    for x, y in points:
        tx = width - x if mirror_x else x
        ty = height - y if mirror_y else y
        transformed.append((left + tx, top + ty))

    # 绘制填充和金色边框
    draw.polygon(transformed, fill=(94, 44, 22, 210))
    draw.line(transformed + [transformed[0]], fill=(255, 187, 101, 240), width=10)


def create_awards_showcase_panel() -> Image.Image:
    """
    创建奖项展示面板

    这是一个大型的装饰性边框面板（2200x1440像素），具有以下视觉层次：
    1. 最底层：黑色投影，营造浮起感
    2. 背景填充：从深蓝到更深蓝的垂直渐变 + 径向光泽 + 暗角效果 + 噪声纹理
    3. 多层边框：金色外框 → 青色内框 → 白色细线 → 更内层白线
    4. 左右两侧：橙色发光装饰条
    5. 四个角落：金属装饰板
    6. 顶部和底部：蝙蝠纹章

    返回:
        奖项展示面板的RGBA图片
    """
    size = (2200, 1440)
    image = Image.new("RGBA", size, TRANSPARENT)
    frame_box = (116, 104, size[0] - 116, size[1] - 104)
    add_shadow(image, frame_box, radius=156, offset=(0, 24), opacity=180)

    # 构建面板背景：渐变 + 光泽 + 暗角 + 噪声
    inner_size = (frame_box[2] - frame_box[0], frame_box[3] - frame_box[1])
    fill = vertical_gradient(inner_size, (7, 18, 34, 246), (2, 7, 16, 240))
    sheen = radial_glow(inner_size, (18, 48, 84, 18), (0, 0, 0, 0), squish_y=1.24)
    fill.alpha_composite(sheen)
    vignette = radial_glow(inner_size, (0, 0, 0, 0), (0, 0, 0, 118), squish_y=1.08)
    fill.alpha_composite(vignette)
    apply_noise(fill, alpha=10)

    # 使用圆角遮罩将背景裁剪后粘贴到面板
    panel_mask = rounded_mask(inner_size, 140)
    panel = Image.new("RGBA", size, TRANSPARENT)
    panel.paste(fill, frame_box[:2], panel_mask)
    image.alpha_composite(panel)

    # 顶部青色发光和底部橙色发光（色彩对比增加视觉层次）
    glow_mask = rounded_mask(inner_size, 140, inset=12).filter(ImageFilter.GaussianBlur(24))
    cyan_glow = Image.new("RGBA", size, TRANSPARENT)
    cyan_glow.paste((36, 190, 255, 72), frame_box[:2], glow_mask)
    image.alpha_composite(cyan_glow)

    orange_glow = Image.new("RGBA", size, TRANSPARENT)
    orange_glow.paste((255, 144, 58, 54), frame_box[:2], glow_mask.transpose(Image.Transpose.FLIP_TOP_BOTTOM))
    image.alpha_composite(orange_glow)

    # 绘制多层边框线
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(frame_box, radius=140, outline=(234, 180, 93, 255), width=22)
    draw.rounded_rectangle(
        (frame_box[0] + 26, frame_box[1] + 26, frame_box[2] - 26, frame_box[3] - 26),
        radius=122,
        outline=(22, 196, 255, 188),
        width=6,
    )
    draw.rounded_rectangle(
        (frame_box[0] + 44, frame_box[1] + 44, frame_box[2] - 44, frame_box[3] - 44),
        radius=108,
        outline=(255, 255, 255, 28),
        width=4,
    )
    draw.rounded_rectangle(
        (frame_box[0] + 86, frame_box[1] + 92, frame_box[2] - 86, frame_box[3] - 92),
        radius=88,
        outline=(255, 255, 255, 14),
        width=2,
    )

    # 左右两侧的橙色发光装饰条
    accent_left = (frame_box[0] + 18, frame_box[1] + 206, frame_box[0] + 34, frame_box[3] - 206)
    accent_right = (frame_box[2] - 34, frame_box[1] + 206, frame_box[2] - 18, frame_box[3] - 206)
    add_line_glow(image, accent_left, (255, 138, 42, 180), blur=18)
    add_line_glow(image, accent_right, (255, 138, 42, 180), blur=18)
    draw.rounded_rectangle(accent_left, radius=8, fill=(255, 173, 92, 230))
    draw.rounded_rectangle(accent_right, radius=8, fill=(255, 173, 92, 230))

    # 四个角落的金属装饰板
    corner_w = 236
    corner_h = 188
    draw_corner_plate(draw, (frame_box[0] - 12, frame_box[1] - 12, frame_box[0] - 12 + corner_w, frame_box[1] - 12 + corner_h))
    draw_corner_plate(draw, (frame_box[2] - corner_w + 12, frame_box[1] - 12, frame_box[2] + 12, frame_box[1] - 12 + corner_h), mirror_x=True)
    draw_corner_plate(draw, (frame_box[0] - 12, frame_box[3] - corner_h + 12, frame_box[0] - 12 + corner_w, frame_box[3] + 12), mirror_y=True)
    draw_corner_plate(draw, (frame_box[2] - corner_w + 12, frame_box[3] - corner_h + 12, frame_box[2] + 12, frame_box[3] + 12), mirror_x=True, mirror_y=True)

    # 顶部和底部的蝙蝠纹章
    draw_bat_crest(draw, size[0] // 2, frame_box[1] + 46, 1.28, fill=(26, 30, 45, 255), outline=(229, 182, 92, 255))
    draw_bat_crest(draw, size[0] // 2, frame_box[3] - 46, 1.12, fill=(26, 30, 45, 255), outline=(229, 182, 92, 228))
    return image


def create_awards_result_plaque() -> Image.Image:
    """
    创建比赛结果铭牌

    这是一个水平横幅式的铭牌（1664x352像素），用于显示比赛结果文字。
    视觉元素包括：
    1. 圆角矩形背景（蓝色渐变）
    2. 金色外边框 + 青色内边框
    3. 左右两侧的径向光晕（青色和橙色）
    4. 顶部和底部的金色锯齿装饰

    返回:
        比赛结果铭牌的RGBA图片
    """
    size = (1664, 352)
    image = Image.new("RGBA", size, TRANSPARENT)
    box = (42, 34, size[0] - 42, size[1] - 34)
    add_shadow(image, box, radius=118, offset=(0, 18), opacity=180)

    # 蓝色渐变背景
    base = vertical_gradient((box[2] - box[0], box[3] - box[1]), (14, 38, 68, 245), (4, 11, 22, 235))
    base_mask = rounded_mask(base.size, 118)
    base_layer = Image.new("RGBA", size, TRANSPARENT)
    base_layer.paste(base, box[:2], base_mask)
    image.alpha_composite(base_layer)

    # 金色外边框和青色内边框
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(box, radius=118, outline=(238, 188, 92, 255), width=18)
    draw.rounded_rectangle(
        (box[0] + 20, box[1] + 20, box[2] - 20, box[3] - 20),
        radius=100,
        outline=(47, 210, 255, 170),
        width=6,
    )

    # 左右两侧的径向光晕
    flare_left = radial_glow((280, 280), (34, 208, 255, 92), TRANSPARENT, squish_y=1.6)
    flare_right = radial_glow((280, 280), (255, 148, 52, 96), TRANSPARENT, squish_y=1.6)
    image.alpha_composite(flare_left, (80, 36))
    image.alpha_composite(flare_right, (size[0] - 360, 36))

    # 顶部锯齿形装饰图案
    notch_top = [
        (size[0] // 2 - 86, 22),
        (size[0] // 2 - 26, 58),
        (size[0] // 2, 42),
        (size[0] // 2 + 26, 58),
        (size[0] // 2 + 86, 22),
        (size[0] // 2 + 68, 10),
        (size[0] // 2, 28),
        (size[0] // 2 - 68, 10),
    ]
    # 底部锯齿（通过垂直翻转顶部锯齿获得）
    notch_bottom = [(x, size[1] - y) for x, y in notch_top]
    draw.polygon(notch_top, fill=(233, 182, 90, 255))
    draw.polygon(notch_bottom, fill=(233, 182, 90, 210))
    return image


def draw_step(draw: ImageDraw.ImageDraw, front_box: tuple[int, int, int, int], top_inset: int, face_top: Color, face_bottom: Color, line_color: Color) -> None:
    """
    绘制颁奖台的单个台阶

    每个台阶包含三个视觉层次：
    1. 顶面：一个平行四边形，模拟3D透视效果
    2. 正面：从上到下的渐变填充，带竖条纹理
    3. 边框：金色圆角边框 + 顶部水平分割线

    参数:
        draw: PIL的ImageDraw对象
        front_box: 正面矩形的边界框 (左, 上, 右, 下)
        top_inset: 顶面相对于正面的内缩量（控制透视角度）
        face_top: 正面顶部颜色
        face_bottom: 正面底部颜色
        line_color: 顶部水平分割线的颜色
    """
    left, top, right, bottom = front_box
    width = right - left

    # 绘制3D透视的顶面（平行四边形）
    top_surface = [
        (left + top_inset, top),
        (right - top_inset, top),
        (right - top_inset - 34, top - 26),
        (left + top_inset + 34, top - 26),
    ]
    draw.polygon(top_surface, fill=(30, 58, 86, 245), outline=(235, 187, 97, 255))

    # 绘制正面渐变（逐行填充实现垂直渐变）
    for y in range(top, bottom):
        t = (y - top) / max(1, bottom - top)
        color = tuple(int(face_top[i] * (1 - t) + face_bottom[i] * t) for i in range(4))
        draw.line((left, y, right, y), fill=color)

    # 金色边框
    draw.rounded_rectangle(front_box, radius=28, outline=(232, 184, 94, 255), width=10)

    # 顶部水平分割线
    draw.line((left + 24, top + 28, right - 24, top + 28), fill=line_color, width=5)

    # 正面竖条纹理（模拟金属拉丝效果）
    rib_color = (255, 255, 255, 14)
    for x in range(left + 34, right - 30, 32):
        draw.line((x, top + 22, x, bottom - 18), fill=rib_color, width=2)


def create_awards_podium_base() -> Image.Image:
    """
    创建颁奖台底座

    包含三个高低错落的台阶，分别代表第2名（左）、第1名（中）、第3名（右）。
    每个台阶有不同的颜色方案：
    - 左侧（第2名）：青色调
    - 中间（第1名）：绿色调（最高）
    - 右侧（第3名）：橙色调

    还包含地面阴影、各台阶的径向发光和水平装饰线。

    返回:
        颁奖台底座的RGBA图片
    """
    size = (2048, 832)
    image = Image.new("RGBA", size, TRANSPARENT)
    draw = ImageDraw.Draw(image)

    # 地面阴影（水平椭圆形）
    floor_shadow = radial_glow((1500, 240), (0, 0, 0, 140), TRANSPARENT, squish_y=0.42)
    image.alpha_composite(floor_shadow, (274, 560))

    # 三个台阶的位置和尺寸
    left_box = (228, 368, 760, 668)
    center_box = (644, 248, 1404, 708)
    right_box = (1288, 406, 1820, 684)

    # 绘制三个台阶（颜色方案：青色/绿色/橙色）
    draw_step(draw, left_box, 42, (25, 110, 136, 245), (8, 26, 46, 245), (104, 243, 255, 180))
    draw_step(draw, center_box, 54, (32, 146, 90, 250), (8, 30, 40, 248), (162, 245, 108, 205))
    draw_step(draw, right_box, 42, (146, 90, 32, 245), (18, 26, 42, 245), (255, 182, 90, 185))

    # 各台阶下方的径向发光效果
    glow_left = radial_glow((520, 240), (54, 204, 255, 84), TRANSPARENT, squish_y=0.55)
    glow_center = radial_glow((720, 300), (116, 255, 134, 98), TRANSPARENT, squish_y=0.6)
    glow_right = radial_glow((520, 240), (255, 160, 72, 80), TRANSPARENT, squish_y=0.55)
    image.alpha_composite(glow_left, (234, 326))
    image.alpha_composite(glow_center, (670, 214))
    image.alpha_composite(glow_right, (1294, 364))

    # 水平装饰线
    deck_line = (186, 564, 1864, 564)
    draw.line(deck_line, fill=(255, 255, 255, 22), width=4)
    draw.line((226, 694, 1822, 694), fill=(11, 226, 255, 110), width=4)
    return image


def main() -> None:
    """
    主函数：生成所有颁奖界面UI资源并保存

    生成三个UI资源文件：
    1. awards_showcase_panel.png - 奖项展示面板
    2. awards_result_plaque.png - 比赛结果铭牌
    3. awards_podium_base.png - 颁奖台底座
    """
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    assets = {
        "awards_showcase_panel.png": create_awards_showcase_panel(),
        "awards_result_plaque.png": create_awards_result_plaque(),
        "awards_podium_base.png": create_awards_podium_base(),
    }

    for file_name, image in assets.items():
        image.save(OUTPUT_DIR / file_name)
        print(f"wrote {OUTPUT_DIR / file_name}")


if __name__ == "__main__":
    main()
