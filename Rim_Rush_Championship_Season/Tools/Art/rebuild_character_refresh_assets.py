from __future__ import annotations

import json
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


REPO_ROOT = Path(__file__).resolve().parents[2]
CHARACTER_SOURCE_DIR = REPO_ROOT / "ArtSource" / "CharacterRefresh"
TMP_DIR = REPO_ROOT / "tmp" / "character_refresh"

DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "rimrush" / "Resources" / "rimrush" / "DragonBones"
PORTRAITS_DIR = REPO_ROOT / "Assets" / "rimrush" / "Resources" / "rimrush" / "Portraits"

TEXTURE_PATH = DRAGON_BONES_DIR / "texture2.png"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"
PORTRAITS_PATH = PORTRAITS_DIR / "portraits_ui.png"
PORTRAITS_JSON_PATH = PORTRAITS_DIR / "portraits_ui.json"

OUTLINE_COLOR = (17, 14, 18)
DRAGON_BONES_TARGET_PIXELS_PER_UNIT = 2.0


@dataclass(frozen=True)
class CharacterRefresh:
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
    return json.loads(path.read_text(encoding="utf-8"))


def save_json(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def atlas_lookup(atlas_json: dict) -> dict[str, dict]:
    return {item["name"]: item for item in atlas_json["SubTexture"]}


def trim_alpha(image: Image.Image) -> Image.Image:
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def border_key_color(image: Image.Image) -> tuple[int, int, int]:
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

            # Light despill keeps the contour clean against neon key colors.
            despill = 0.42 * (1.0 - alpha_scale)
            out_pixels[x, y] = (
                int(round(r * (1.0 - despill) + key[0] * despill * 0.18)),
                int(round(g * (1.0 - despill) + key[1] * despill * 0.18)),
                int(round(b * (1.0 - despill) + key[2] * despill * 0.18)),
                out_alpha,
            )

    return trim_alpha(out)


def prepare_source_image(source_name: str) -> Image.Image:
    source_path = CHARACTER_SOURCE_DIR / source_name
    source = Image.open(source_path).convert("RGBA")
    alpha_min, alpha_max = source.getchannel("A").getextrema()
    if alpha_min == 255 and alpha_max == 255:
        return remove_chroma_background(source)

    return trim_alpha(source)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    if image.size == size:
        return image.copy()

    src = image.convert("RGBA")
    src_data = src.load()
    temp = Image.new("RGBA", src.size, (0, 0, 0, 0))
    temp_data = temp.load()

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
        paste_x = int(round((canvas_size[0] - fitted.width) * anchor_x))
        paste_y = int(round((canvas_size[1] - fitted.height) * anchor_y))
    else:
        min_x = int(round(canvas_size[0] * margin))
        max_x = canvas_size[0] - fitted.width - min_x
        min_y = int(round(canvas_size[1] * margin))
        max_y = canvas_size[1] - fitted.height - min_y
        paste_x = min_x if max_x <= min_x else int(round(min_x + (max_x - min_x) * anchor_x))
        paste_y = min_y if max_y <= min_y else int(round(min_y + (max_y - min_y) * anchor_y))
    canvas.alpha_composite(fitted, (paste_x, paste_y))
    return canvas


def ensure_dragon_bones_resolution(texture: Image.Image, texture_json: dict) -> tuple[Image.Image, dict]:
    current_pixels_per_unit = float(texture_json.get("pixelsPerUnit", 1.0))
    scale_factor = DRAGON_BONES_TARGET_PIXELS_PER_UNIT / max(0.0001, current_pixels_per_unit)
    if abs(scale_factor - 1.0) <= 0.0001:
        texture_json["pixelsPerUnit"] = DRAGON_BONES_TARGET_PIXELS_PER_UNIT
        return texture, texture_json

    texture = premultiplied_resize(
        texture,
        (int(round(texture.width * scale_factor)), int(round(texture.height * scale_factor))),
    )

    for sub in texture_json["SubTexture"]:
        for key in ("x", "y", "width", "height"):
            sub[key] = int(round(sub[key] * scale_factor))

    texture_json["pixelsPerUnit"] = DRAGON_BONES_TARGET_PIXELS_PER_UNIT
    return texture, texture_json


def crop_subtexture(texture: Image.Image, sub: dict) -> Image.Image:
    return texture.crop((sub["x"], sub["y"], sub["x"] + sub["width"], sub["y"] + sub["height"]))


def paste_subtexture(texture: Image.Image, sub: dict, image: Image.Image) -> None:
    cleared = Image.new("RGBA", (sub["width"], sub["height"]), (0, 0, 0, 0))
    texture.paste(cleared, (sub["x"], sub["y"]))
    texture.alpha_composite(image.convert("RGBA"), (sub["x"], sub["y"]))


def luminance(pixel: tuple[int, int, int, int]) -> float:
    return (0.2126 * pixel[0] + 0.7152 * pixel[1] + 0.0722 * pixel[2]) / 255.0


def mix_color(base: tuple[int, int, int], shade: float) -> tuple[int, int, int]:
    shade = max(0.55, min(1.25, shade))
    return tuple(max(0, min(255, int(round(channel * shade)))) for channel in base)


def is_outline(alpha: list[list[int]], x: int, y: int) -> bool:
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
    if theme == "reaper":
        if part == "body":
            if yn < 0.18:
                return (170, 176, 188)
            if 0.33 < yn < 0.56 and 0.41 < xn < 0.59:
                return (56, 214, 144)
            if xn < 0.18 or xn > 0.82:
                return (118, 124, 135)
            if yn > 0.76:
                return (28, 34, 40)
            return (33, 36, 43)
        if part == "leg":
            if yn > 0.7:
                return (225, 225, 232)
            if 0.28 < yn < 0.48 and 0.34 < xn < 0.66:
                return (56, 214, 144)
            return (24, 27, 34)
        if yn < 0.3:
            return (96, 106, 118)
        if 0.24 < yn < 0.5 and 0.42 < xn < 0.58:
            return (56, 214, 144)
        return (34, 38, 45)

    if theme == "clown":
        if part == "body":
            if yn < 0.22:
                return (252, 231, 191) if int(xn * 6) % 2 == 0 else (212, 56, 60)
            if 0.32 < yn < 0.56 and 0.38 < xn < 0.62:
                return (245, 200, 70)
            if yn > 0.76:
                return (248, 240, 221) if xn < 0.5 else (94, 186, 177)
            return (206, 46, 54) if xn < 0.5 else (77, 177, 168)
        if part == "leg":
            if yn > 0.7:
                return (241, 240, 232)
            if xn < 0.28:
                return (206, 46, 54)
            if xn > 0.72:
                return (77, 177, 168)
            return (252, 231, 191)
        if yn < 0.28:
            return (245, 200, 70)
        if xn < 0.5:
            return (243, 244, 240)
        return (230, 233, 239)

    if theme == "pirate":
        if part == "body":
            if yn < 0.22:
                return (189, 48, 49)
            if 0.15 < xn < 0.26 or 0.74 < xn < 0.85:
                return (219, 182, 74)
            if 0.34 + yn * 0.2 < xn < 0.48 + yn * 0.2:
                return (98, 66, 39)
            if 0.42 < xn < 0.58 and 0.26 < yn < 0.66 and int((yn - 0.26) * 14) % 3 == 0:
                return (225, 183, 69)
            if yn > 0.76:
                return (32, 39, 54)
            return (32, 53, 99)
        if part == "leg":
            if yn > 0.7:
                return (234, 230, 219)
            if 0.25 < xn < 0.48:
                return (189, 48, 49)
            if 0.56 < xn < 0.74 and 0.22 < yn < 0.46:
                return (225, 183, 69)
            return (26, 31, 42)
        if yn < 0.24:
            return (38, 56, 98)
        return (229, 223, 210)

    raise ValueError(f"Unknown theme: {theme}")


def build_thematic_part(template: Image.Image, theme: str, part: str) -> Image.Image:
    src = template.convert("RGBA")
    width, height = src.size
    src_pixels = src.load()
    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    out_pixels = out.load()
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

            if is_outline(alpha_rows, x, y):
                out_pixels[x, y] = (*OUTLINE_COLOR, a)
                continue

            xn = (x - min_x) / range_x
            yn = (y - min_y) / range_y
            base = design_color(theme, part, xn, yn)
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
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    preview = Image.new("RGBA", (1220, 700), (22, 22, 26, 255))
    draw = ImageDraw.Draw(preview)
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

        head_name = f"custom_head_{replacement.internal_id}"
        head_crop = trim_alpha(crop_subtexture(texture, texture_lookup[head_name]))
        head_preview = premultiplied_resize(head_crop, (head_crop.width * 3, head_crop.height * 3))
        preview.alpha_composite(head_preview, (x_base + 18, 68))

        for row_index, part in enumerate(body_rows):
            part_name = f"custom_{part}_{replacement.internal_id}"
            crop = trim_alpha(crop_subtexture(texture, texture_lookup[part_name]))
            canvas = fit_subject_to_canvas(crop, (160, 120), margin=0.06)
            preview.alpha_composite(canvas, (x_base + row_index * 90, 294))
            draw.text((x_base + row_index * 90, 274), part, fill=(180, 180, 186, 255))

        portrait_name = f"custom_head_{replacement.internal_id}"
        portrait_crop = trim_alpha(crop_subtexture(portraits, portrait_lookup[portrait_name]))
        portrait_canvas = fit_subject_to_canvas(portrait_crop, (260, 180), margin=0.02)
        preview.alpha_composite(portrait_canvas, (x_base, 510))

    preview.save(TMP_DIR / "character_refresh_preview.png")


def update_portraits() -> tuple[Image.Image, dict[str, dict]]:
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
    texture = Image.open(TEXTURE_PATH).convert("RGBA")
    texture_json = load_json(TEXTURE_JSON_PATH)
    texture, texture_json = ensure_dragon_bones_resolution(texture, texture_json)
    lookup = atlas_lookup(texture_json)
    source_texture = texture.copy()

    for replacement in REPLACEMENTS:
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

        for part in ("body", "leg", "left_hand", "right_hand", "dig_hand"):
            part_name = f"custom_{part}_{replacement.internal_id}"
            sub = lookup[part_name]
            if part == "body":
                style_source = replacement.body_style_source
            elif part == "leg":
                style_source = replacement.leg_style_source
            else:
                style_source = replacement.hand_style_source

            template_name = f"custom_{part}_{style_source}"
            template = crop_subtexture(source_texture, lookup[template_name])
            themed = build_thematic_part(template, replacement.body_theme, "leg" if part == "leg" else part)
            paste_subtexture(texture, sub, themed)

    texture.save(TEXTURE_PATH)
    save_json(TEXTURE_JSON_PATH, texture_json)
    return texture, lookup


def main() -> None:
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    portraits, portrait_lookup = update_portraits()
    texture, texture_lookup = update_dragon_bones()
    build_preview(texture, texture_lookup, portraits, portrait_lookup)
    print(f"Updated character refresh atlases from {CHARACTER_SOURCE_DIR}")
    print(f"Preview saved to {TMP_DIR / 'character_refresh_preview.png'}")


if __name__ == "__main__":
    main()
