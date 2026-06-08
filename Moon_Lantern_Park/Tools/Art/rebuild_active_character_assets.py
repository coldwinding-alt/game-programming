"""
角色活动部件资源重建脚本

本脚本负责将美术提供的角色原始素材（带透明背景的PNG图片）中的各个身体部件
（头、身体、腿、手等）自动分离，并替换到DragonBones纹理图集中对应的位置。

工作流程：
1. 从 ArtSource/ 目录读取每个角色的原始素材图片
2. 使用连通域分析（scipy或BFS回退方案）将图片分割为独立的部件
3. 按面积大小排序，最大的3个部件分别对应头、身体、腿
4. 剩余部件用于匹配各个手部插槽
5. 对每个部件进行尺寸适配（缩放+偏移），使其与DragonBones纹理图集中的目标区域对齐
6. 将适配后的部件写回纹理图集，原地更新 texture2.png

支持的8个角色：
- pumpkin（南瓜哥/REAPER）
- frankenstein（科学怪人/GHOST CLOWN）
- mummy（木乃伊/SKULL PIRATE）
- vampire（吸血鬼/VAMPIRE）
- candle（蜡烛人/CANDLEMAN）
- scarecrow（稻草人/SCARECROW）
- witch（女巫/WITCH）
- blackcat（黑猫/BLACK CAT）

依赖：Pillow (PIL), numpy, scipy（可选，用于加速连通域分析）
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter, ImageOps

try:
    from scipy import ndimage
except ImportError:  # pragma: no cover - scipy是可选的本地工具依赖
    ndimage = None


# 项目根目录（Tools/Art/ 的上两级）
REPO_ROOT = Path(__file__).resolve().parents[2]

# 美术原始素材目录，存放带透明背景的角色PNG图片
ART_SOURCE_DIR = REPO_ROOT / "ArtSource"

# DragonBones纹理图集目录
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "mlp" / "Resources" / "mlp" / "DragonBones"

# DragonBones纹理图集的图片和JSON配置文件路径
TEXTURE_PATH = DRAGON_BONES_DIR / "texture2.png"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"

# Alpha通道阈值：低于此值的像素被视为完全透明
ALPHA_THRESHOLD = 8

# 最小连通域面积：小于此面积的连通域被视为噪声，会被忽略
MIN_COMPONENT_AREA = 1000

# 缩放系数的搜索范围：从0.75到1.25，步长0.05
# 用于在适配部件到目标区域时搜索最佳缩放比例
SCALE_FACTORS = tuple(0.75 + 0.05 * step for step in range(11))

# 偏移量的搜索范围：-6到+6像素
# 用于在适配部件到目标区域时搜索最佳位置
OFFSET_RANGE = range(-6, 7)

# 大型身体部件名称列表（按面积从大到小分配）
LARGE_PARTS = ("head", "body", "leg")

# 手部部件名称列表
HAND_PARTS = ("left_hand", "right_hand", "dig_hand")

# 图片缩小后的锐化参数
SHARPEN_RADIUS = 0.6       # 锐化半径
SHARPEN_PERCENT = 115       # 锐化强度百分比
SHARPEN_THRESHOLD = 1       # 锐化阈值（亮度差低于此值的边缘不锐化）


@dataclass(frozen=True)
class CharacterSource:
    """
    角色原始素材配置

    属性:
        id: 角色的内部标识符，与DragonBones纹理图集中的命名一致
        source_file: 原始素材文件名（位于 ArtSource/ 目录下）
    """
    id: str
    source_file: str


@dataclass(frozen=True)
class SourceComponent:
    """
    从原始素材中提取的单个连通域（身体部件）

    属性:
        area: 连通域的像素面积
        box: 连通域的边界框 (x_min, y_min, x_max, y_max)
        image: 从原始图片中裁剪出的部件图片（包含边界框内的所有像素）
    """
    area: int
    box: tuple[int, int, int, int]
    image: Image.Image


@dataclass(frozen=True)
class RenderResult:
    """
    部件适配到目标区域后的渲染结果

    属性:
        score: 适配质量评分（Alpha通道的均方误差，越小越好）
        rendered: 已适配并渲染到目标画布上的图片
        flipped: 是否使用了水平翻转
        scale_factor: 使用的缩放系数
        offset_x: 相对于居中位置的X偏移量
        offset_y: 相对于居中位置的Y偏移量
    """
    score: float
    rendered: Image.Image
    flipped: bool
    scale_factor: float
    offset_x: int
    offset_y: int


# 所有角色的原始素材配置
# source_file中的中文文件名是美术提供的原始素材命名
CHARACTERS = (
    CharacterSource("pumpkin", "南瓜哥透明.png"),
    CharacterSource("frankenstein", "科学怪人透明.png"),
    CharacterSource("mummy", "木乃伊透明.png"),
    CharacterSource("vampire", "吸血鬼透明.png"),
    CharacterSource("candle", "蜡烛人透明底.png"),
    CharacterSource("scarecrow", "稻草人透明底.png"),
    CharacterSource("witch", "女巫透明底.png"),
    CharacterSource("blackcat", "黑猫透明底.png"),
)


def load_json(path: Path) -> dict:
    """
    读取并解析JSON文件

    参数:
        path: JSON文件路径

    返回:
        解析后的字典对象
    """
    return json.loads(path.read_text(encoding="utf-8"))


def trim_to_alpha(image: Image.Image) -> Image.Image:
    """
    裁剪图片，去除四周完全透明的区域

    参数:
        image: 输入的RGBA图片

    返回:
        裁剪后的图片
    """
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def extract_components_with_scipy(image: Image.Image) -> list[SourceComponent]:
    """
    使用scipy库提取图片中的连通域（高速版本）

    利用scipy.ndimage的连通域标记算法，将Alpha通道中非透明的连续区域
    识别为独立的身体部件。这是首选方案，速度较快。

    算法步骤：
    1. 将Alpha通道二值化（>=阈值为前景）
    2. 使用ndimage.label进行连通域标记
    3. 使用ndimage.find_objects获取每个连通域的切片信息
    4. 过滤掉面积太小的噪声区域
    5. 按面积从大到小排序

    参数:
        image: 输入的RGBA图片

    返回:
        按面积降序排列的连通域列表
    """
    alpha = np.asarray(image.getchannel("A")) >= ALPHA_THRESHOLD
    labels, _ = ndimage.label(alpha)
    slices = ndimage.find_objects(labels)
    components: list[SourceComponent] = []
    for label_index, item in enumerate(slices, start=1):
        if item is None:
            continue

        y_slice, x_slice = item
        mask = labels[item] == label_index
        area = int(mask.sum())
        if area < MIN_COMPONENT_AREA:
            continue

        box = (x_slice.start, y_slice.start, x_slice.stop, y_slice.stop)
        components.append(SourceComponent(area=area, box=box, image=image.crop(box)))

    components.sort(key=lambda item: item.area, reverse=True)
    return components


def extract_components_fallback(image: Image.Image) -> list[SourceComponent]:
    """
    使用BFS（广度优先搜索）提取图片中的连通域（回退版本）

    当scipy不可用时，使用纯Python实现的BFS算法来识别连通域。
    虽然速度较慢，但不依赖额外的库。

    算法步骤：
    1. 遍历每个像素，找到未访问的非透明像素
    2. 从该像素开始BFS，标记所有相连的非透明像素为同一个连通域
    3. 记录连通域的边界框和面积
    4. 过滤掉面积太小的噪声区域
    5. 按面积从大到小排序

    参数:
        image: 输入的RGBA图片

    返回:
        按面积降序排列的连通域列表
    """
    alpha = image.getchannel("A")
    pixels = alpha.load()
    width, height = alpha.size
    seen = bytearray(width * height)  # 记录已访问的像素
    components: list[SourceComponent] = []

    def index(x: int, y: int) -> int:
        return y * width + x

    for y in range(height):
        for x in range(width):
            pixel_index = index(x, y)
            if seen[pixel_index] or pixels[x, y] < ALPHA_THRESHOLD:
                continue

            # 从当前像素开始BFS
            queue = [(x, y)]
            seen[pixel_index] = 1
            min_x = max_x = x
            min_y = max_y = y
            area = 0

            while queue:
                current_x, current_y = queue.pop()
                area += 1
                min_x = min(min_x, current_x)
                max_x = max(max_x, current_x)
                min_y = min(min_y, current_y)
                max_y = max(max_y, current_y)

                # 探索四个相邻像素
                for next_x, next_y in (
                    (current_x + 1, current_y),
                    (current_x - 1, current_y),
                    (current_x, current_y + 1),
                    (current_x, current_y - 1),
                ):
                    if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                        continue

                    next_index = index(next_x, next_y)
                    if seen[next_index] or pixels[next_x, next_y] < ALPHA_THRESHOLD:
                        continue

                    seen[next_index] = 1
                    queue.append((next_x, next_y))

            if area < MIN_COMPONENT_AREA:
                continue

            box = (min_x, min_y, max_x + 1, max_y + 1)
            components.append(SourceComponent(area=area, box=box, image=image.crop(box)))

    components.sort(key=lambda item: item.area, reverse=True)
    return components


def extract_components(image: Image.Image) -> list[SourceComponent]:
    """
    提取图片中的连通域（自动选择最佳可用方案）

    优先使用scipy版本（速度快），如果scipy未安装则回退到BFS版本。

    参数:
        image: 输入的RGBA图片

    返回:
        按面积降序排列的连通域列表
    """
    if ndimage is not None:
        return extract_components_with_scipy(image)
    return extract_components_fallback(image)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """
    使用预乘Alpha的方式对图片进行高质量缩放

    使用预乘Alpha和批量像素操作保持半透明边缘稳定。同时在缩小时会应用锐化滤镜，
    防止细节丢失。

    参数:
        image: 输入的RGBA图片
        size: 目标尺寸 (宽度, 高度)

    返回:
        缩放后的RGBA图片
    """
    if image.size == size:
        return image.copy()

    rgba = np.asarray(image, dtype=np.float32)
    alpha = rgba[..., 3:4] / 255.0
    premultiplied = rgba[..., :3] * alpha

    rgb_image = Image.fromarray(np.clip(premultiplied, 0, 255).astype(np.uint8), mode="RGB")
    alpha_image = Image.fromarray(np.clip(alpha[..., 0] * 255.0, 0, 255).astype(np.uint8), mode="L")

    resample = Image.Resampling.LANCZOS
    resized_rgb = rgb_image.resize(size, resample)
    resized_alpha = alpha_image.resize(size, resample)

    # 缩小时应用锐化，防止细节模糊
    if size[0] < image.width or size[1] < image.height:
        sharpen = ImageFilter.UnsharpMask(
            radius=SHARPEN_RADIUS,
            percent=SHARPEN_PERCENT,
            threshold=SHARPEN_THRESHOLD,
        )
        resized_rgb = resized_rgb.filter(sharpen)
        resized_alpha = resized_alpha.filter(sharpen)

    # 还原预乘Alpha
    rgb = np.asarray(resized_rgb, dtype=np.float32)
    alpha = np.asarray(resized_alpha, dtype=np.float32) / 255.0
    restored = np.zeros((size[1], size[0], 4), dtype=np.uint8)

    safe_alpha = np.where(alpha > 1e-6, alpha, 1.0)
    restored[..., :3] = np.clip(rgb / safe_alpha[..., None], 0, 255).astype(np.uint8)
    restored[..., 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    restored[..., :3][alpha <= 1e-6] = 0
    return Image.fromarray(restored, mode="RGBA")


def alpha_mse(left: np.ndarray, right: np.ndarray) -> float:
    """
    计算两个Alpha通道之间的均方误差（MSE）

    用于评估源部件与目标区域之间的形状匹配程度。
    MSE越小表示两个形状越接近。

    参数:
        left: 第一个Alpha通道数组
        right: 第二个Alpha通道数组

    返回:
        均方误差值
    """
    return float(np.mean((left - right) ** 2))


def clamp(value: int, minimum: int, maximum: int) -> int:
    """
    将整数值限制在指定范围内

    参数:
        value: 输入值
        minimum: 最小值
        maximum: 最大值

    返回:
        限制后的值
    """
    return max(minimum, min(maximum, value))


def fit_component_to_target(source: Image.Image, target: Image.Image, allow_flip: bool) -> RenderResult:
    """
    将源部件适配到目标区域，搜索最佳的缩放和位置组合

    该函数通过暴力搜索找到使源部件的Alpha通道与目标区域的Alpha通道
    最匹配的缩放比例和偏移位置。搜索空间包括：
    - 是否水平翻转（可选）
    - 缩放系数（SCALE_FACTORS定义的范围）
    - X/Y偏移量（OFFSET_RANGE定义的范围）

    匹配质量使用Alpha通道的均方误差来衡量。

    参数:
        source: 源部件图片
        target: 目标区域图片（从DragonBones纹理图集中裁剪）
        allow_flip: 是否允许水平翻转（手部部件允许翻转以获得更好的匹配）

    返回:
        最佳匹配结果，包含渲染后的图片和匹配参数
    """
    canvas_width, canvas_height = target.size
    target_alpha = np.asarray(target.getchannel("A"), dtype=np.float32) / 255.0
    fit_box = target.getchannel("A").getbbox() or (0, 0, canvas_width, canvas_height)
    fit_width = max(1, fit_box[2] - fit_box[0])
    fit_height = max(1, fit_box[3] - fit_box[1])
    source = trim_to_alpha(source)

    best: RenderResult | None = None

    # 构建搜索方向列表：原图 + 可选的水平翻转
    orientations = ((False, source),)
    if allow_flip:
        orientations += ((True, ImageOps.mirror(source)),)

    for flipped, oriented in orientations:
        # 计算基础缩放比（使部件适配目标区域）
        base_scale = min(fit_width / oriented.width, fit_height / oriented.height)

        for scale_factor in SCALE_FACTORS:
            scaled_width = max(1, int(round(oriented.width * base_scale * scale_factor)))
            scaled_height = max(1, int(round(oriented.height * base_scale * scale_factor)))

            # 缩放后不能超过画布尺寸
            if scaled_width > canvas_width or scaled_height > canvas_height:
                continue

            resized = premultiplied_resize(oriented, (scaled_width, scaled_height))
            resized_alpha = np.asarray(resized.getchannel("A"), dtype=np.float32) / 255.0

            # 计算居中位置
            centered_x = int(round((fit_box[0] + fit_box[2] - scaled_width) / 2.0))
            centered_y = int(round((fit_box[1] + fit_box[3] - scaled_height) / 2.0))

            for offset_x in OFFSET_RANGE:
                for offset_y in OFFSET_RANGE:
                    paste_x = clamp(centered_x + offset_x, 0, canvas_width - scaled_width)
                    paste_y = clamp(centered_y + offset_y, 0, canvas_height - scaled_height)

                    # 将部件的Alpha通道放置到画布上
                    candidate_alpha = np.zeros((canvas_height, canvas_width), dtype=np.float32)
                    candidate_alpha[paste_y : paste_y + scaled_height, paste_x : paste_x + scaled_width] = resized_alpha

                    # 计算与目标的MSE
                    score = alpha_mse(candidate_alpha, target_alpha)
                    if best is not None and score >= best.score:
                        continue

                    # 渲染最终结果
                    rendered = Image.new("RGBA", target.size, (0, 0, 0, 0))
                    rendered.paste(resized, (paste_x, paste_y), resized)
                    best = RenderResult(
                        score=score,
                        rendered=rendered,
                        flipped=flipped,
                        scale_factor=scale_factor,
                        offset_x=paste_x - centered_x,
                        offset_y=paste_y - centered_y,
                    )

    if best is None:
        raise ValueError(f"Could not fit source component {source.size} into target canvas {target.size}.")

    return best


def build_target_lookup(texture_json: dict) -> dict[str, dict]:
    """
    从DragonBones纹理JSON中构建子纹理名称到配置的查找表

    参数:
        texture_json: DragonBones纹理图集的JSON配置

    返回:
        子纹理名称 -> 子纹理配置字典的映射
    """
    return {sub_texture["name"]: sub_texture for sub_texture in texture_json["SubTexture"]}


def extract_target_crop(texture: Image.Image, sub_texture: dict) -> Image.Image:
    """
    从纹理图集中裁剪指定子纹理区域

    参数:
        texture: 完整的纹理图集图片
        sub_texture: 子纹理配置字典（包含x, y, width, height）

    返回:
        裁剪后的子纹理图片
    """
    return texture.crop(
        (
            sub_texture["x"],
            sub_texture["y"],
            sub_texture["x"] + sub_texture["width"],
            sub_texture["y"] + sub_texture["height"],
        )
    )


def paste_target_crop(texture: Image.Image, sub_texture: dict, crop: Image.Image) -> None:
    """
    将处理后的图片粘贴回纹理图集中的指定位置

    参数:
        texture: 完整的纹理图集图片（会被原地修改）
        sub_texture: 子纹理配置字典（包含x, y）
        crop: 要粘贴的图片
    """
    texture.paste(crop, (sub_texture["x"], sub_texture["y"]), crop)


def rebuild_character(texture: Image.Image, targets: dict[str, dict], character: CharacterSource) -> None:
    """
    重建单个角色的所有身体部件

    处理流程：
    1. 读取角色的原始素材图片
    2. 提取所有连通域作为身体部件
    3. 将最大的3个部件分配给头、身体、腿
    4. 对剩余部件（手部）尝试所有可能的匹配，选择最佳的
    5. 将适配后的部件写回纹理图集

    参数:
        texture: 完整的DragonBones纹理图集图片（会被原地修改）
        targets: 子纹理名称到配置的查找表
        character: 角色配置
    """
    source_path = ART_SOURCE_DIR / character.source_file
    if not source_path.exists():
        raise FileNotFoundError(f"Missing source character art: {source_path}")

    source_image = Image.open(source_path).convert("RGBA")
    components = extract_components(source_image)
    if len(components) < 5:
        raise ValueError(
            f"Expected at least 5 significant components in {source_path.name}, found {len(components)}."
        )

    # 将最大的3个部件分配给头、身体、腿
    large_assignments = {
        "head": components[0],
        "body": components[1],
        "leg": components[2],
    }
    # 剩余部件用于手部匹配
    hand_components = components[3:]

    print(f"[{character.id}] components={len(components)}")

    # 处理大型部件（头、身体、腿）
    for part in LARGE_PARTS:
        target_name = f"custom_{part}_{character.id}"
        target_crop = extract_target_crop(texture, targets[target_name])
        result = fit_component_to_target(large_assignments[part].image, target_crop, allow_flip=False)
        paste_target_crop(texture, targets[target_name], result.rendered)
        component_index = components.index(large_assignments[part]) + 1
        print(
            f"  {part}: component {component_index} score={result.score:.4f} "
            f"scale={result.scale_factor:.2f} offset=({result.offset_x},{result.offset_y})"
        )

    # 处理手部部件（允许翻转以获得更好的匹配）
    for part in HAND_PARTS:
        target_name = f"custom_{part}_{character.id}"
        target_crop = extract_target_crop(texture, targets[target_name])
        best_component_index = -1
        best_result: RenderResult | None = None

        # 遍历所有候选手部部件，选择匹配度最高的
        for index, component in enumerate(hand_components, start=4):
            result = fit_component_to_target(component.image, target_crop, allow_flip=True)
            if best_result is None or result.score < best_result.score:
                best_result = result
                best_component_index = index

        if best_result is None:
            raise ValueError(f"Could not assign {part} for {character.id}.")

        paste_target_crop(texture, targets[target_name], best_result.rendered)
        print(
            f"  {part}: component {best_component_index} score={best_result.score:.4f} "
            f"flipped={best_result.flipped} scale={best_result.scale_factor:.2f} "
            f"offset=({best_result.offset_x},{best_result.offset_y})"
        )


def main() -> None:
    """
    主函数：重建所有角色的身体部件

    读取DragonBones纹理图集，遍历所有角色配置，
    为每个角色重建身体部件并原地更新纹理图集。
    """
    texture = Image.open(TEXTURE_PATH).convert("RGBA")
    texture_json = load_json(TEXTURE_JSON_PATH)
    targets = build_target_lookup(texture_json)

    for character in CHARACTERS:
        rebuild_character(texture, targets, character)

    texture.save(TEXTURE_PATH)
    print(f"Rebuilt current character sprites in-place from ArtSource: {TEXTURE_PATH}")


if __name__ == "__main__":
    main()
