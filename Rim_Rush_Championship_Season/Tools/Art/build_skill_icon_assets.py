from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Tuple

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont


Color = Tuple[int, int, int, int]
REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = REPO_ROOT / "ArtSource" / "SkillIcons"
OUTPUT_DIR = REPO_ROOT / "Assets" / "rimrush" / "Resources" / "rimrush" / "Images" / "SkillIcons"
TMP_DIR = REPO_ROOT / "tmp" / "skill_icons"

ICON_SIZE = 512
PREVIEW_TILE = 210
PREVIEW_MARGIN = 24
TRANSPARENT = (0, 0, 0, 0)
MASK_NAVY = (14, 20, 30, 255)


@dataclass(frozen=True)
class SkillIconBuild:
    display_name: str
    source_name: str
    output_key: str
    accent: Tuple[int, int, int]
    scale: float = 0.92
    anchor_x: float = 0.5
    anchor_y: float = 0.5


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
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image.copy()


def border_key_color(image: Image.Image) -> tuple[int, int, int]:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    samples: list[tuple[int, int, int]] = []

    for x in range(rgba.width):
        samples.append(pixels[x, 0][:3])
        samples.append(pixels[x, rgba.height - 1][:3])

    for y in range(1, rgba.height - 1):
        samples.append(pixels[0, y][:3])
        samples.append(pixels[rgba.width - 1, y][:3])

    counts: dict[tuple[int, int, int], int] = {}
    for sample in samples:
        counts[sample] = counts.get(sample, 0) + 1

    return max(counts.items(), key=lambda item: item[1])[0]


def remove_chroma_background(
    image: Image.Image,
    *,
    transparent_threshold: float = 16.0,
    opaque_threshold: float = 112.0,
) -> Image.Image:
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

            distance = ((r - key[0]) ** 2 + (g - key[1]) ** 2 + (b - key[2]) ** 2) ** 0.5
            if distance <= transparent_threshold:
                continue

            if distance >= opaque_threshold:
                out_pixels[x, y] = (r, g, b, a)
                continue

            alpha_scale = (distance - transparent_threshold) / span
            out_alpha = max(0, min(255, int(round(a * alpha_scale))))
            if out_alpha <= 0:
                continue

            despill = 0.32 * (1.0 - alpha_scale)
            out_pixels[x, y] = (
                int(round(r * (1.0 - despill) + key[0] * despill * 0.1)),
                int(round(g * (1.0 - despill) + key[1] * despill * 0.1)),
                int(round(b * (1.0 - despill) + key[2] * despill * 0.1)),
                out_alpha,
            )

    return trim_alpha(out)


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    if image.size == size:
        return image.copy()

    src = image.convert("RGBA")
    src_pixels = src.load()
    temp = Image.new("RGBA", src.size, TRANSPARENT)
    temp_pixels = temp.load()

    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = src_pixels[x, y]
            temp_pixels[x, y] = (
                int(round(r * a / 255.0)),
                int(round(g * a / 255.0)),
                int(round(b * a / 255.0)),
                a,
            )

    resized = temp.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", size, TRANSPARENT)
    out_pixels = out.load()
    resized_pixels = resized.load()

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
    source = trim_alpha(source.convert("RGBA"))
    available_width = max(1, int(round(canvas_size[0] * (1.0 - margin * 2.0))))
    available_height = max(1, int(round(canvas_size[1] * (1.0 - margin * 2.0))))
    ratio = min(available_width / source.width, available_height / source.height) * scale
    target_size = (
        max(1, int(round(source.width * ratio))),
        max(1, int(round(source.height * ratio))),
    )
    fitted = premultiplied_resize(source, target_size)

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
    alpha = subject.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    glow = Image.new("RGBA", subject.size, (*rgb, 0))
    glow.putalpha(alpha.point(lambda value: min(255, value * opacity // 255)))
    return glow


def alpha_shadow(subject: Image.Image, blur: int, offset: tuple[int, int], opacity: int) -> Image.Image:
    alpha = subject.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    shadow = Image.new("RGBA", subject.size, (0, 0, 0, 0))
    shifted = Image.new("RGBA", subject.size, (0, 0, 0, 0))
    shadow_mask = alpha.point(lambda value: min(255, value * opacity // 255))
    shifted.putalpha(shadow_mask)
    shadow.alpha_composite(shifted, offset)
    return shadow


def compose_base_icon(skill: SkillIconBuild, subject: Image.Image) -> Image.Image:
    accent = skill.accent
    canvas = Image.new("RGBA", subject.size, TRANSPARENT)
    canvas.alpha_composite(alpha_shadow(subject, blur=14, offset=(0, 8), opacity=110))
    canvas.alpha_composite(alpha_glow(subject, accent, blur=28, opacity=150))
    canvas.alpha_composite(alpha_glow(subject, tuple(int(channel * 0.65) for channel in accent), blur=10, opacity=110))
    canvas.alpha_composite(subject)
    return canvas


def compose_charge_mask(subject: Image.Image) -> Image.Image:
    dim = subject.convert("RGBA")
    dim = ImageEnhance.Color(dim).enhance(0.15)
    dim = ImageEnhance.Contrast(dim).enhance(0.9)
    dim = ImageEnhance.Brightness(dim).enhance(0.34)

    navy_wash = Image.new("RGBA", dim.size, MASK_NAVY)
    navy_wash.putalpha(subject.getchannel("A").point(lambda value: min(255, int(round(value * 0.88)))))
    dim = ImageChops.screen(dim, navy_wash)

    canvas = Image.new("RGBA", subject.size, TRANSPARENT)
    canvas.alpha_composite(alpha_shadow(subject, blur=10, offset=(0, 6), opacity=92))
    canvas.alpha_composite(dim)
    return canvas


def build_icon_pair(skill: SkillIconBuild) -> tuple[Image.Image, Image.Image]:
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
    draw.text(xy, text, font=font, fill=fill)


def build_preview_sheet(pairs: Iterable[tuple[SkillIconBuild, Image.Image, Image.Image]]) -> None:
    items = list(pairs)
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    cols = 2
    rows = len(items)
    tile_width = PREVIEW_TILE * 2 + PREVIEW_MARGIN * 3
    tile_height = PREVIEW_TILE + 78
    sheet = Image.new("RGBA", (tile_width * cols, rows * tile_height + PREVIEW_MARGIN), (10, 14, 20, 255))
    draw = ImageDraw.Draw(sheet)
    try:
        title_font = ImageFont.truetype("arial.ttf", 20)
        body_font = ImageFont.truetype("arial.ttf", 16)
    except OSError:
        title_font = ImageFont.load_default()
        body_font = ImageFont.load_default()

    for row, (skill, base_icon, charge_mask) in enumerate(items):
        top = PREVIEW_MARGIN + row * tile_height
        left = PREVIEW_MARGIN
        panel = Image.new("RGBA", (sheet.width - PREVIEW_MARGIN * 2, PREVIEW_TILE + 56), (18, 24, 36, 255))
        sheet.alpha_composite(panel, (left, top))

        orb = Image.new("RGBA", (PREVIEW_TILE, PREVIEW_TILE), (12, 18, 26, 255))
        sheet.alpha_composite(orb, (left + PREVIEW_MARGIN, top + 24))
        sheet.alpha_composite(base_icon.resize((PREVIEW_TILE, PREVIEW_TILE), Image.Resampling.LANCZOS), (left + PREVIEW_MARGIN, top + 24))
        sheet.alpha_composite(charge_mask.resize((PREVIEW_TILE, PREVIEW_TILE), Image.Resampling.LANCZOS), (left + PREVIEW_MARGIN * 2 + PREVIEW_TILE, top + 24))

        preview_label(draw, (left + PREVIEW_MARGIN, top + PREVIEW_TILE + 30), skill.display_name, font=title_font, fill=(235, 241, 248))
        preview_label(draw, (left + PREVIEW_MARGIN * 2 + PREVIEW_TILE, top + PREVIEW_TILE + 32), "charge mask", font=body_font, fill=(158, 176, 196))

    (TMP_DIR / "skill_icons_preview.png").parent.mkdir(parents=True, exist_ok=True)
    sheet.save(TMP_DIR / "skill_icons_preview.png")


def main() -> None:
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
