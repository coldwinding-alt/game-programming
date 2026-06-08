"""
技能图标资源构建脚本

本脚本负责为游戏中8个可玩角色生成技能图标资源。每个角色会生成两张图片：
1. 基础技能图标（base icon）：包含角色抠图、阴影和发光效果的完整图标
2. 充能遮罩（charge mask）：用于技能充能进度条的暗色遮罩版本

工作流程：
1. 从 ArtSource/SkillIcons/ 读取角色原始素材（带纯色背景的截图）
2. 自动检测并去除背景色（色度抠图）
3. 将角色主体适配到统一尺寸的画布上
4. 叠加阴影和发光效果生成最终图标
5. 生成暗色版本的充能遮罩
6. 输出到 Assets/mlp/Resources/mlp/Images/SkillIcons/
7. 在 tmp/ 目录下生成预览拼图方便检查

依赖：Pillow (PIL)
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Tuple

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont


# RGBA颜色类型，每个分量为0-255的整数
Color = Tuple[int, int, int, int]

# 项目根目录（Tools/Art/ 的上两级）
REPO_ROOT = Path(__file__).resolve().parents[2]

# 原始素材目录，存放带纯色背景的角色截图
SOURCE_DIR = REPO_ROOT / "ArtSource" / "SkillIcons"

# 最终输出目录，技能图标会被Unity运行时加载
OUTPUT_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "Images" / "SkillIcons"

# 临时目录，用于存放预览拼图等中间产物
TMP_DIR = REPO_ROOT / "tmp" / "skill_icons"

# 技能图标的正方形尺寸（像素）
ICON_SIZE = 512

# 预览拼图中每个缩略图的尺寸和间距
PREVIEW_TILE = 210
PREVIEW_MARGIN = 24

# 完全透明的像素颜色
TRANSPARENT = (0, 0, 0, 0)

# 充能遮罩使用的深海军蓝色底色
MASK_NAVY = (14, 20, 30, 255)


@dataclass(frozen=True)
class SkillIconBuild:
    """
    单个角色技能图标的构建配置

    属性:
        display_name: 角色的显示名称，用于预览拼图中的标签
        source_name: 原始素材文件名（位于 ArtSource/SkillIcons/ 目录下）
        output_key: 输出文件名的前缀，如 "reaper" 会生成 "reaper_skill_icon.png"
        accent: 角色的主色调RGB值，用于生成发光效果
        scale: 角色主体在画布中的缩放比例，默认0.92
        anchor_x: 角色主体在画布中的水平锚点位置（0.0=左边缘, 0.5=居中, 1.0=右边缘）
        anchor_y: 角色主体在画布中的垂直锚点位置（0.0=上边缘, 0.5=居中, 1.0=下边缘）
    """
    display_name: str
    source_name: str
    output_key: str
    accent: Tuple[int, int, int]
    scale: float = 0.92
    anchor_x: float = 0.5
    anchor_y: float = 0.5


# 所有角色的技能图标构建配置列表
# 每个角色的素材文件、主色调和定位参数各不相同，需要单独调整
SKILLS: tuple[SkillIconBuild, ...] = (
    SkillIconBuild("REAPER", "reaper_skill_source.png", "reaper", (66, 240, 228), scale=0.94, anchor_x=0.54, anchor_y=0.52),
    SkillIconBuild("GHOST CLOWN", "ghost_clown_skill_source.png", "ghost_clown", (82, 228, 255), scale=0.91, anchor_x=0.5, anchor_y=0.52),
    SkillIconBuild("SKULL PIRATE", "skull_pirate_skill_source.png", "skull_pirate", (112, 244, 220), scale=0.9, anchor_x=0.5, anchor_y=0.54),
    SkillIconBuild("VAMPIRE", "vampire_skill_source.png", "vampire", (255, 82, 110), scale=0.9, anchor_x=0.5, anchor_y=0.52),
    SkillIconBuild("CANDLEMAN", "candleman_skill_source.png", "candleman", (255, 176, 58), scale=0.92, anchor_x=0.5, anchor_y=0.53),
    SkillIconBuild("SCARECROW", "scarecrow_skill_source.png", "scarecrow", (255, 198, 74), scale=0.91, anchor_x=0.52, anchor_y=0.53),
    SkillIconBuild("WITCH", "witch_skill_source.png", "witch", (112, 212, 255), scale=0.9, anchor_x=0.5, anchor_y=0.52),
    SkillIconBuild("BLACK CAT", "black_cat_skill_source.png", "black_cat", (174, 104, 255), scale=0.93, anchor_x=0.5, anchor_y=0.51),
)


def trim_alpha(image: Image.Image) -> Image.Image:
    """
    裁剪图片，去除四周完全透明的区域

    通过Alpha通道获取有效像素的边界框，然后裁剪到该边界。
    如果图片完全透明，则保持原图尺寸不做裁剪。

    参数:
        image: 输入的RGBA图片

    返回:
        裁剪后的图片
    """
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def border_key_color(image: Image.Image) -> tuple[int, int, int]:
    """
    检测图片边缘的主要颜色（色度键颜色）

    通过采样图片四周边缘的所有像素，统计出现频率最高的颜色，
    该颜色即为背景色度键的颜色。这对于自动去除纯色背景至关重要。

    参数:
        image: 输入的RGBA图片

    返回:
        出现频率最高的边缘颜色的RGB元组
    """
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    samples: list[tuple[int, int, int]] = []

    # 采样顶部和底部边缘的像素
    for x in range(rgba.width):
        samples.append(pixels[x, 0][:3])
        samples.append(pixels[x, rgba.height - 1][:3])

    # 采样左侧和右侧边缘的像素（排除已采样的角落）
    for y in range(1, rgba.height - 1):
        samples.append(pixels[0, y][:3])
        samples.append(pixels[rgba.width - 1, y][:3])

    # 统计每种颜色出现的次数
    counts: dict[tuple[int, int, int], int] = {}
    for sample in samples:
        counts[sample] = counts.get(sample, 0) + 1

    # 返回出现次数最多的颜色
    return max(counts.items(), key=lambda item: item[1])[0]


def remove_chroma_background(
    image: Image.Image,
    *,
    transparent_threshold: float = 16.0,
    opaque_threshold: float = 112.0,
) -> Image.Image:
    """
    去除图片的纯色背景（色度键抠图）

    该函数实现了基于颜色距离的渐进式抠图算法：
    1. 自动检测背景色（通过边缘采样）
    2. 对每个像素计算其与背景色的欧氏距离
    3. 距离小于透明阈值的像素被视为纯背景，直接丢弃
    4. 距离大于不透明阈值的像素被视为前景，完全保留
    5. 介于两者之间的像素按比例半透明化（实现柔和的边缘过渡）
    6. 对半透明边缘区域执行轻微的去溢色处理，避免背景色渗透到轮廓边缘

    参数:
        image: 输入的RGBA图片（带纯色背景）
        transparent_threshold: 颜色距离低于此值的像素被完全剔除（默认16.0）
        opaque_threshold: 颜色距离高于此值的像素被完全保留（默认112.0）

    返回:
        去除背景后的RGBA图片
    """
    src = image.convert("RGBA")
    key = border_key_color(src)
    src_pixels = src.load()
    out = Image.new("RGBA", src.size, TRANSPARENT)
    out_pixels = out.load()
    span = max(1.0, opaque_threshold - transparent_threshold)

    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = src_pixels[x, y]
            if a <= 0:
                continue

            # 计算当前像素与背景色的欧氏距离
            distance = ((r - key[0]) ** 2 + (g - key[1]) ** 2 + (b - key[2]) ** 2) ** 0.5

            # 距离太近，认为是纯背景，跳过
            if distance <= transparent_threshold:
                continue

            # 距离足够远，认为是纯前景，直接保留
            if distance >= opaque_threshold:
                out_pixels[x, y] = (r, g, b, a)
                continue

            # 中间地带：按比例设置半透明度，实现柔和边缘
            alpha_scale = (distance - transparent_threshold) / span
            out_alpha = max(0, min(255, int(round(a * alpha_scale))))
            if out_alpha <= 0:
                continue

            # 去溢色处理：边缘像素会带有背景色的溢出，
            # 将其向背景色方向轻微偏移以减少轮廓处的颜色污染
            despill = 0.32 * (1.0 - alpha_scale)
            out_pixels[x, y] = (
                int(round(r * (1.0 - despill) + key[0] * despill * 0.1)),
                int(round(g * (1.0 - despill) + key[1] * despill * 0.1)),
                int(round(b * (1.0 - despill) + key[2] * despill * 0.1)),
                out_alpha,
            )

    return trim_alpha(out)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """
    使用预乘Alpha的方式对图片进行高质量缩放

    普通的图片缩放会在半透明边缘产生颜色溢出（暗边或亮边），
    预乘Alpha缩放通过以下步骤解决这个问题：
    1. 将RGB通道乘以Alpha通道（预乘）
    2. 使用LANCZOS算法缩放预乘后的图片
    3. 将缩放后的RGB通道除以Alpha通道还原

    这样可以确保半透明边缘在缩放后保持正确的颜色和透明度。

    参数:
        image: 输入的RGBA图片
        size: 目标尺寸 (宽度, 高度)

    返回:
        缩放后的RGBA图片
    """
    if image.size == size:
        return image.copy()

    src = image.convert("RGBA")
    src_pixels = src.load()
    temp = Image.new("RGBA", src.size, TRANSPARENT)
    temp_pixels = temp.load()

    # 步骤1：预乘Alpha - 将RGB值乘以Alpha/255
    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = src_pixels[x, y]
            temp_pixels[x, y] = (
                int(round(r * a / 255.0)),
                int(round(g * a / 255.0)),
                int(round(b * a / 255.0)),
                a,
            )

    # 步骤2：使用LANCZOS重采样进行缩放
    resized = temp.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", size, TRANSPARENT)
    out_pixels = out.load()
    resized_pixels = resized.load()

    # 步骤3：还原Alpha - 将预乘的RGB值除以Alpha
    for y in range(size[1]):
        for x in range(size[0]):
            r, g, b, a = resized_pixels[x, y]
            if a <= 0:
                continue

            scale = 255.0 / max(1, a)
            out_pixels[x, y] = (
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
    scale: float,
    margin: float,
    anchor_x: float,
    anchor_y: float,
) -> Image.Image:
    """
    将角色主体图片适配到指定尺寸的画布上

    该函数完成以下工作：
    1. 裁剪源图片的透明边缘
    2. 根据画布尺寸和边距计算可用区域
    3. 按比例缩放角色使其适配可用区域
    4. 根据锚点参数确定角色在画布中的位置
    5. 将缩放后的角色粘贴到画布上

    参数:
        source: 角色主体的RGBA图片
        canvas_size: 目标画布尺寸 (宽度, 高度)
        scale: 额外的缩放系数（在适配可用区域的基础上再缩放）
        margin: 画布边缘的留白比例（0.0~0.5）
        anchor_x: 水平锚点（0.0=贴左, 0.5=居中, 1.0=贴右）
        anchor_y: 垂直锚点（0.0=贴顶, 0.5=居中, 1.0=贴底）

    返回:
        包含适配后角色的画布图片
    """
    source = trim_alpha(source.convert("RGBA"))

    # 计算扣除边距后的可用区域尺寸
    available_width = max(1, int(round(canvas_size[0] * (1.0 - margin * 2.0))))
    available_height = max(1, int(round(canvas_size[1] * (1.0 - margin * 2.0))))

    # 计算保持宽高比的缩放比，再乘以额外缩放系数
    ratio = min(available_width / source.width, available_height / source.height) * scale
    target_size = (
        max(1, int(round(source.width * ratio))),
        max(1, int(round(source.height * ratio))),
    )
    fitted = premultiplied_resize(source, target_size)

    # 在可用区域内根据锚点计算粘贴位置
    canvas = Image.new("RGBA", canvas_size, TRANSPARENT)
    min_x = int(round(canvas_size[0] * margin))
    max_x = canvas_size[0] - fitted.width - min_x
    min_y = int(round(canvas_size[1] * margin))
    max_y = canvas_size[1] - fitted.height - min_y
    paste_x = min_x if max_x <= min_x else int(round(min_x + (max_x - min_x) * anchor_x))
    paste_y = min_y if max_y <= min_y else int(round(min_y + (max_y - min_y) * anchor_y))
    canvas.alpha_composite(fitted, (paste_x, paste_y))
    return canvas


def alpha_glow(subject: Image.Image, rgb: tuple[int, int, int], blur: int, opacity: int) -> Image.Image:
    """
    基于Alpha通道生成发光效果层

    提取主体图片的Alpha通道，对其进行高斯模糊，然后用指定颜色填充，
    生成一个柔和的发光光晕效果。这个效果会叠加在主体下方，营造霓虹灯般的光晕。

    参数:
        subject: 主体RGBA图片
        rgb: 发光颜色的RGB值
        blur: 高斯模糊半径，值越大光晕越扩散
        opacity: 发光不透明度（0-255）

    返回:
        发光效果的RGBA图片层
    """
    alpha = subject.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    glow = Image.new("RGBA", subject.size, (*rgb, 0))
    glow.putalpha(alpha.point(lambda value: min(255, value * opacity // 255)))
    return glow


def alpha_shadow(subject: Image.Image, blur: int, offset: tuple[int, int], opacity: int) -> Image.Image:
    """
    基于Alpha通道生成阴影效果层

    提取主体图片的Alpha通道，对其进行高斯模糊后偏移指定距离，
    生成一个柔和的投影效果。这个阴影会叠加在最底层。

    参数:
        subject: 主体RGBA图片
        blur: 高斯模糊半径
        offset: 阴影偏移量 (x, y)，正值表示向右下方偏移
        opacity: 阴影不透明度（0-255）

    返回:
        阴影效果的RGBA图片层
    """
    alpha = subject.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    shadow = Image.new("RGBA", subject.size, (0, 0, 0, 0))
    shifted = Image.new("RGBA", subject.size, (0, 0, 0, 0))
    shadow_mask = alpha.point(lambda value: min(255, value * opacity // 255))
    shifted.putalpha(shadow_mask)
    shadow.alpha_composite(shifted, offset)
    return shadow


def compose_base_icon(skill: SkillIconBuild, subject: Image.Image) -> Image.Image:
    """
    合成基础技能图标

    将多个效果层按照从底到顶的顺序叠加：
    1. 最底层：黑色投影（向下偏移8像素）
    2. 第二层：角色主色调的柔和大光晕（模糊半径28）
    3. 第三层：角色主色调加深版的小光晕（模糊半径10，更锐利）
    4. 最顶层：角色主体本身

    这种多层叠加方式使图标看起来有深度感和霓虹发光效果。

    参数:
        skill: 角色的构建配置
        subject: 已适配到画布上的角色主体图片

    返回:
        合成后的基础技能图标
    """
    accent = skill.accent
    canvas = Image.new("RGBA", subject.size, TRANSPARENT)
    canvas.alpha_composite(alpha_shadow(subject, blur=14, offset=(0, 8), opacity=110))
    canvas.alpha_composite(alpha_glow(subject, accent, blur=28, opacity=150))
    canvas.alpha_composite(alpha_glow(subject, tuple(int(channel * 0.65) for channel in accent), blur=10, opacity=110))
    canvas.alpha_composite(subject)
    return canvas


def compose_charge_mask(subject: Image.Image) -> Image.Image:
    """
    合成充能遮罩版本的技能图标

    生成用于技能充能进度条的暗色遮罩图标。处理步骤：
    1. 将主体大幅去饱和（保留15%色彩）
    2. 降低对比度和亮度，使其变暗
    3. 叠加一层深海军蓝色调
    4. 添加投影效果

    最终效果是一个暗淡的、偏蓝色调的图标，用于表示技能尚未准备好的状态。

    参数:
        subject: 已适配到画布上的角色主体图片

    返回:
        充能遮罩版本的图标
    """
    dim = subject.convert("RGBA")
    # 大幅去饱和，只保留15%的原始色彩
    dim = ImageEnhance.Color(dim).enhance(0.15)
    # 降低对比度
    dim = ImageEnhance.Contrast(dim).enhance(0.9)
    # 大幅降低亮度
    dim = ImageEnhance.Brightness(dim).enhance(0.34)

    # 叠加深海军蓝色调，使图标偏蓝色
    navy_wash = Image.new("RGBA", dim.size, MASK_NAVY)
    navy_wash.putalpha(subject.getchannel("A").point(lambda value: min(255, int(round(value * 0.88)))))
    dim = ImageChops.screen(dim, navy_wash)

    # 添加投影和最终合成
    canvas = Image.new("RGBA", subject.size, TRANSPARENT)
    canvas.alpha_composite(alpha_shadow(subject, blur=10, offset=(0, 6), opacity=92))
    canvas.alpha_composite(dim)
    return canvas


def build_icon_pair(skill: SkillIconBuild) -> tuple[Image.Image, Image.Image]:
    """
    为单个角色构建一对技能图标（基础图标 + 充能遮罩）

    完整处理流程：
    1. 读取原始素材文件
    2. 去除纯色背景
    3. 将角色适配到标准画布尺寸
    4. 分别合成基础图标和充能遮罩

    参数:
        skill: 角色的构建配置

    返回:
        (基础图标, 充能遮罩) 的元组
    """
    source_path = SOURCE_DIR / skill.source_name
    source = Image.open(source_path).convert("RGBA")
    cutout = remove_chroma_background(source)
    subject = fit_subject_to_canvas(
        cutout,
        (ICON_SIZE, ICON_SIZE),
        scale=skill.scale,
        margin=0.07,
        anchor_x=skill.anchor_x,
        anchor_y=skill.anchor_y,
    )

    base_icon = compose_base_icon(skill, subject)
    charge_mask = compose_charge_mask(subject)
    return base_icon, charge_mask


def preview_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, *, font: ImageFont.ImageFont, fill: tuple[int, int, int]) -> None:
    """
    在预览图上绘制文字标签

    参数:
        draw: PIL的ImageDraw对象
        xy: 文字左上角坐标
        text: 要绘制的文字内容
        font: 字体对象
        fill: 文字颜色RGB
    """
    draw.text(xy, text, font=font, fill=fill)


def build_preview_sheet(pairs: Iterable[tuple[SkillIconBuild, Image.Image, Image.Image]]) -> None:
    """
    生成所有角色技能图标的预览拼图

    将所有角色的基础图标和充能遮罩排列在一个大图上，
    方便一次性检查所有图标的视觉效果。每行一个角色，
    左侧是基础图标，右侧是充能遮罩。

    参数:
        pairs: 可迭代的 (角色配置, 基础图标, 充能遮罩) 元组序列
    """
    items = list(pairs)
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    cols = 2  # 每行显示两列（基础图标 + 充能遮罩）
    rows = len(items)
    tile_width = PREVIEW_TILE * 2 + PREVIEW_MARGIN * 3
    tile_height = PREVIEW_TILE + 78  # 缩略图高度 + 标签区域高度
    sheet = Image.new("RGBA", (tile_width * cols, rows * tile_height + PREVIEW_MARGIN), (10, 14, 20, 255))
    draw = ImageDraw.Draw(sheet)

    # 尝试加载字体，失败则使用默认字体
    try:
        title_font = ImageFont.truetype("arial.ttf", 20)
        body_font = ImageFont.truetype("arial.ttf", 16)
    except OSError:
        title_font = ImageFont.load_default()
        body_font = ImageFont.load_default()

    for row, (skill, base_icon, charge_mask) in enumerate(items):
        top = PREVIEW_MARGIN + row * tile_height
        left = PREVIEW_MARGIN

        # 绘制每行的背景面板
        panel = Image.new("RGBA", (sheet.width - PREVIEW_MARGIN * 2, PREVIEW_TILE + 56), (18, 24, 36, 255))
        sheet.alpha_composite(panel, (left, top))

        # 左侧：基础图标（带深色背景底色）
        orb = Image.new("RGBA", (PREVIEW_TILE, PREVIEW_TILE), (12, 18, 26, 255))
        sheet.alpha_composite(orb, (left + PREVIEW_MARGIN, top + 24))
        sheet.alpha_composite(base_icon.resize((PREVIEW_TILE, PREVIEW_TILE), Image.Resampling.LANCZOS), (left + PREVIEW_MARGIN, top + 24))

        # 右侧：充能遮罩
        sheet.alpha_composite(charge_mask.resize((PREVIEW_TILE, PREVIEW_TILE), Image.Resampling.LANCZOS), (left + PREVIEW_MARGIN * 2 + PREVIEW_TILE, top + 24))

        # 绘制标签文字
        preview_label(draw, (left + PREVIEW_MARGIN, top + PREVIEW_TILE + 30), skill.display_name, font=title_font, fill=(235, 241, 248))
        preview_label(draw, (left + PREVIEW_MARGIN * 2 + PREVIEW_TILE, top + PREVIEW_TILE + 32), "charge mask", font=body_font, fill=(158, 176, 196))

    # 保存预览拼图
    (TMP_DIR / "skill_icons_preview.png").parent.mkdir(parents=True, exist_ok=True)
    sheet.save(TMP_DIR / "skill_icons_preview.png")


def main() -> None:
    """
    主函数：构建所有角色的技能图标并生成预览

    遍历所有角色配置，生成技能图标对并保存到输出目录，
    最后生成一张包含所有图标的预览拼图。
    """
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    built: list[tuple[SkillIconBuild, Image.Image, Image.Image]] = []

    for skill in SKILLS:
        base_icon, charge_mask = build_icon_pair(skill)
        base_path = OUTPUT_DIR / f"{skill.output_key}_skill_icon.png"
        mask_path = OUTPUT_DIR / f"{skill.output_key}_skill_charge_mask.png"
        base_icon.save(base_path)
        charge_mask.save(mask_path)
        built.append((skill, base_icon, charge_mask))
        print(f"wrote {base_path}")
        print(f"wrote {mask_path}")

    build_preview_sheet(built)
    print(f"preview -> {TMP_DIR / 'skill_icons_preview.png'}")


if __name__ == "__main__":
    main()
