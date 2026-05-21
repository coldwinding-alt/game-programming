from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter, ImageOps

try:
    from scipy import ndimage
except ImportError:  # pragma: no cover - scipy is optional for local tooling.
    ndimage = None


REPO_ROOT = Path(__file__).resolve().parents[2]
ART_SOURCE_DIR = REPO_ROOT / "ArtSource"
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "rimrush" / "Resources" / "rimrush" / "DragonBones"
TEXTURE_PATH = DRAGON_BONES_DIR / "texture2.png"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"

ALPHA_THRESHOLD = 8
MIN_COMPONENT_AREA = 1000
SCALE_FACTORS = tuple(0.75 + 0.05 * step for step in range(11))
OFFSET_RANGE = range(-6, 7)
LARGE_PARTS = ("head", "body", "leg")
HAND_PARTS = ("left_hand", "right_hand", "dig_hand")
SHARPEN_RADIUS = 0.6
SHARPEN_PERCENT = 115
SHARPEN_THRESHOLD = 1


@dataclass(frozen=True)
class CharacterSource:
    id: str
    source_file: str


@dataclass(frozen=True)
class SourceComponent:
    area: int
    box: tuple[int, int, int, int]
    image: Image.Image


@dataclass(frozen=True)
class RenderResult:
    score: float
    rendered: Image.Image
    flipped: bool
    scale_factor: float
    offset_x: int
    offset_y: int


CHARACTERS = (
    CharacterSource("pumpkin", "\u5357\u74dc\u54e5\u900f\u660e.png"),
    CharacterSource("frankenstein", "\u79d1\u5b66\u602a\u4eba\u900f\u660e.png"),
    CharacterSource("mummy", "\u6728\u4e43\u4f0a\u900f\u660e.png"),
    CharacterSource("vampire", "\u5438\u8840\u9b3c\u900f\u660e.png"),
    CharacterSource("candle", "\u8721\u70db\u4eba\u900f\u660e\u5e95.png"),
    CharacterSource("scarecrow", "\u7a3b\u8349\u4eba\u900f\u660e\u5e95.png"),
    CharacterSource("witch", "\u5973\u5deb\u900f\u660e\u5e95.png"),
    CharacterSource("blackcat", "\u9ed1\u732b\u900f\u660e\u5e95.png"),
)


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def trim_to_alpha(image: Image.Image) -> Image.Image:
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def extract_components_with_scipy(image: Image.Image) -> list[SourceComponent]:
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
    alpha = image.getchannel("A")
    pixels = alpha.load()
    width, height = alpha.size
    seen = bytearray(width * height)
    components: list[SourceComponent] = []

    def index(x: int, y: int) -> int:
        return y * width + x

    for y in range(height):
        for x in range(width):
            pixel_index = index(x, y)
            if seen[pixel_index] or pixels[x, y] < ALPHA_THRESHOLD:
                continue

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
    if ndimage is not None:
        return extract_components_with_scipy(image)
    return extract_components_fallback(image)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
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
    if size[0] < image.width or size[1] < image.height:
        sharpen = ImageFilter.UnsharpMask(
            radius=SHARPEN_RADIUS,
            percent=SHARPEN_PERCENT,
            threshold=SHARPEN_THRESHOLD,
        )
        resized_rgb = resized_rgb.filter(sharpen)
        resized_alpha = resized_alpha.filter(sharpen)

    rgb = np.asarray(resized_rgb, dtype=np.float32)
    alpha = np.asarray(resized_alpha, dtype=np.float32) / 255.0
    restored = np.zeros((size[1], size[0], 4), dtype=np.uint8)

    safe_alpha = np.where(alpha > 1e-6, alpha, 1.0)
    restored[..., :3] = np.clip(rgb / safe_alpha[..., None], 0, 255).astype(np.uint8)
    restored[..., 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    restored[..., :3][alpha <= 1e-6] = 0
    return Image.fromarray(restored, mode="RGBA")


def alpha_mse(left: np.ndarray, right: np.ndarray) -> float:
    return float(np.mean((left - right) ** 2))


def clamp(value: int, minimum: int, maximum: int) -> int:
    return max(minimum, min(maximum, value))


def fit_component_to_target(source: Image.Image, target: Image.Image, allow_flip: bool) -> RenderResult:
    canvas_width, canvas_height = target.size
    target_alpha = np.asarray(target.getchannel("A"), dtype=np.float32) / 255.0
    fit_box = target.getchannel("A").getbbox() or (0, 0, canvas_width, canvas_height)
    fit_width = max(1, fit_box[2] - fit_box[0])
    fit_height = max(1, fit_box[3] - fit_box[1])
    source = trim_to_alpha(source)

    best: RenderResult | None = None
    orientations = ((False, source),)
    if allow_flip:
        orientations += ((True, ImageOps.mirror(source)),)

    for flipped, oriented in orientations:
        base_scale = min(fit_width / oriented.width, fit_height / oriented.height)
        for scale_factor in SCALE_FACTORS:
            scaled_width = max(1, int(round(oriented.width * base_scale * scale_factor)))
            scaled_height = max(1, int(round(oriented.height * base_scale * scale_factor)))
            if scaled_width > canvas_width or scaled_height > canvas_height:
                continue

            resized = premultiplied_resize(oriented, (scaled_width, scaled_height))
            resized_alpha = np.asarray(resized.getchannel("A"), dtype=np.float32) / 255.0
            centered_x = int(round((fit_box[0] + fit_box[2] - scaled_width) / 2.0))
            centered_y = int(round((fit_box[1] + fit_box[3] - scaled_height) / 2.0))

            for offset_x in OFFSET_RANGE:
                for offset_y in OFFSET_RANGE:
                    paste_x = clamp(centered_x + offset_x, 0, canvas_width - scaled_width)
                    paste_y = clamp(centered_y + offset_y, 0, canvas_height - scaled_height)
                    candidate_alpha = np.zeros((canvas_height, canvas_width), dtype=np.float32)
                    candidate_alpha[paste_y : paste_y + scaled_height, paste_x : paste_x + scaled_width] = resized_alpha
                    score = alpha_mse(candidate_alpha, target_alpha)
                    if best is not None and score >= best.score:
                        continue

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
    return {sub_texture["name"]: sub_texture for sub_texture in texture_json["SubTexture"]}


def extract_target_crop(texture: Image.Image, sub_texture: dict) -> Image.Image:
    return texture.crop(
        (
            sub_texture["x"],
            sub_texture["y"],
            sub_texture["x"] + sub_texture["width"],
            sub_texture["y"] + sub_texture["height"],
        )
    )


def paste_target_crop(texture: Image.Image, sub_texture: dict, crop: Image.Image) -> None:
    texture.paste(crop, (sub_texture["x"], sub_texture["y"]), crop)


def rebuild_character(texture: Image.Image, targets: dict[str, dict], character: CharacterSource) -> None:
    source_path = ART_SOURCE_DIR / character.source_file
    if not source_path.exists():
        raise FileNotFoundError(f"Missing source character art: {source_path}")

    source_image = Image.open(source_path).convert("RGBA")
    components = extract_components(source_image)
    if len(components) < 5:
        raise ValueError(
            f"Expected at least 5 significant components in {source_path.name}, found {len(components)}."
        )

    large_assignments = {
        "head": components[0],
        "body": components[1],
        "leg": components[2],
    }
    hand_components = components[3:]

    print(f"[{character.id}] components={len(components)}")

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

    for part in HAND_PARTS:
        target_name = f"custom_{part}_{character.id}"
        target_crop = extract_target_crop(texture, targets[target_name])
        best_component_index = -1
        best_result: RenderResult | None = None

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
    texture = Image.open(TEXTURE_PATH).convert("RGBA")
    texture_json = load_json(TEXTURE_JSON_PATH)
    targets = build_target_lookup(texture_json)

    for character in CHARACTERS:
        rebuild_character(texture, targets, character)

    texture.save(TEXTURE_PATH)
    print(f"Rebuilt current character sprites in-place from ArtSource: {TEXTURE_PATH}")


if __name__ == "__main__":
    main()
