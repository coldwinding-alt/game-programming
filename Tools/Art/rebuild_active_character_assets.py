import copy
import json
from pathlib import Path

from PIL import Image


REPO_ROOT = Path(__file__).resolve().parents[2]
DRAGON_BONES_DIR = REPO_ROOT / "Assets" / "BasketballLegends2020" / "Resources" / "BL2020" / "DragonBones"
TEXTURE_PATH = DRAGON_BONES_DIR / "texture2.png"
TEXTURE_JSON_PATH = DRAGON_BONES_DIR / "texture2.json"
SKELETON_PATH = DRAGON_BONES_DIR / "sk2.json"

ATLAS_SIZE = (1024, 1024)
PADDING = 2

CHARACTER_SPECS = [
    {
        "id": "pumpkin",
        "head_source": "LeBron James0",
        "body_source": "b0",
        "left_hand_source": "h01",
        "right_hand_source": "h1",
        "dig_hand_source": "hh1",
        "leg_source": "l3",
        "head_anim_source": "head1",
        "body_anim_source": "body1",
        "hand_anim_source": "hand2",
        "leg_anim_source": "leg4",
    },
    {
        "id": "frankenstein",
        "head_source": "Draymond Green0",
        "body_source": "b2",
        "left_hand_source": "h00",
        "right_hand_source": "h0",
        "dig_hand_source": "hh0",
        "leg_source": "l8",
        "head_anim_source": "head6",
        "body_anim_source": "body3",
        "hand_anim_source": "hand1",
        "leg_anim_source": "leg9",
    },
    {
        "id": "mummy",
        "head_source": "Brook Lopez",
        "body_source": "b4",
        "left_hand_source": "h02",
        "right_hand_source": "h2",
        "dig_hand_source": "hh2",
        "leg_source": "l14",
        "head_anim_source": "head9",
        "body_anim_source": "body5",
        "hand_anim_source": "hand3",
        "leg_anim_source": "leg15",
    },
    {
        "id": "vampire",
        "head_source": "Marcus Smart0",
        "body_source": "b6",
        "left_hand_source": "h03",
        "right_hand_source": "h3",
        "dig_hand_source": "hh3",
        "leg_source": "l15",
        "head_anim_source": "head12",
        "body_anim_source": "body7",
        "hand_anim_source": "hand4",
        "leg_anim_source": "leg16",
    },
    {
        "id": "candle",
        "head_source": "custom_head_candle",
        "body_source": "custom_body_candle",
        "left_hand_source": "h04",
        "right_hand_source": "h4",
        "dig_hand_source": "hh4",
        "leg_source": "l16",
        "head_anim_source": "head14",
        "body_anim_source": "body9",
        "hand_anim_source": "hand5",
        "leg_anim_source": "leg17",
    },
    {
        "id": "scarecrow",
        "head_source": "custom_head_scarecrow",
        "body_source": "custom_body_scarecrow",
        "left_hand_source": "h05",
        "right_hand_source": "h5",
        "dig_hand_source": "hh5",
        "leg_source": "l17",
        "head_anim_source": "head15",
        "body_anim_source": "body10",
        "hand_anim_source": "hand6",
        "leg_anim_source": "leg18",
    },
    {
        "id": "witch",
        "head_source": "custom_head_witch",
        "body_source": "custom_body_witch",
        "left_hand_source": "h06",
        "right_hand_source": "h6",
        "dig_hand_source": "hh6",
        "leg_source": "l18",
        "head_anim_source": "head16",
        "body_anim_source": "body11",
        "hand_anim_source": "hand7",
        "leg_anim_source": "leg19",
    },
    {
        "id": "blackcat",
        "head_source": "custom_head_blackcat",
        "body_source": "custom_body_blackcat",
        "left_hand_source": "h07",
        "right_hand_source": "h7",
        "dig_hand_source": "hh7",
        "leg_source": "l19",
        "head_anim_source": "head17",
        "body_anim_source": "body12",
        "hand_anim_source": "hand8",
        "leg_anim_source": "leg20",
    },
]


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def save_json(path: Path, payload: dict) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def find_armature(root: dict, name: str) -> dict:
    return next(armature for armature in root["armature"] if armature["name"] == name)


def rewrite_character_armature(armature: dict, source_specs: list[tuple[str, str]], slot_name: str, animation_prefix: str) -> None:
    slot = armature["skin"][0]["slot"][0]
    old_displays = slot["display"]
    old_index_by_name = {display["name"]: index for index, display in enumerate(old_displays)}
    old_animations = {animation["name"]: animation for animation in armature["animation"]}

    new_displays = []
    new_animations = []
    for new_index, (source_name, target_name, source_animation_name) in enumerate(source_specs):
        display = copy.deepcopy(old_displays[old_index_by_name[source_name]])
        display["name"] = target_name
        new_displays.append(display)

        animation = copy.deepcopy(old_animations[source_animation_name])
        animation["name"] = f"{animation_prefix}{new_index + 1}"
        slot_tracks = animation.setdefault("slot", [])
        track = next((item for item in slot_tracks if item.get("name") == slot_name), None)
        if track is None:
            track = {"name": slot_name}
            slot_tracks.append(track)
        track["displayFrame"] = [{"value": new_index}]
        new_animations.append(animation)

    slot["display"] = new_displays
    armature["animation"] = new_animations


def collect_image_display_names(root: dict) -> set[str]:
    names: set[str] = set()
    for armature in root["armature"]:
        skins = armature.get("skin", [])
        if not skins:
            continue

        for slot in skins[0].get("slot", []):
            for display in slot.get("display", []):
                if display.get("type", "image") == "armature":
                    continue
                name = display.get("name")
                if name:
                    names.add(name)

    return names


def build_source_name_map() -> dict[str, str]:
    mapping: dict[str, str] = {}
    for spec in CHARACTER_SPECS:
        suffix = spec["id"]
        mapping[f"custom_head_{suffix}"] = spec["head_source"]
        mapping[f"custom_body_{suffix}"] = spec["body_source"]
        mapping[f"custom_left_hand_{suffix}"] = spec["left_hand_source"]
        mapping[f"custom_right_hand_{suffix}"] = spec["right_hand_source"]
        mapping[f"custom_dig_hand_{suffix}"] = spec["dig_hand_source"]
        mapping[f"custom_leg_{suffix}"] = spec["leg_source"]
    return mapping


def repack_texture(image_path: Path, atlas_path: Path, used_names: set[str], source_name_map: dict[str, str]) -> None:
    atlas_json = load_json(atlas_path)
    sub_textures = {sub_texture["name"]: sub_texture for sub_texture in atlas_json["SubTexture"]}
    source_image = Image.open(image_path).convert("RGBA")

    crops: list[tuple[str, Image.Image]] = []
    for name in sorted(used_names):
        source_name = source_name_map.get(name, name)
        if source_name not in sub_textures:
            raise KeyError(f"Missing source subtexture '{source_name}' for '{name}'.")

        sub_texture = sub_textures[source_name]
        crop = source_image.crop(
            (
                sub_texture["x"],
                sub_texture["y"],
                sub_texture["x"] + sub_texture["width"],
                sub_texture["y"] + sub_texture["height"],
            )
        )
        crops.append((name, crop))

    packed_positions: list[dict[str, int | str]] = []
    packed_image = Image.new("RGBA", ATLAS_SIZE, (0, 0, 0, 0))

    x = 0
    y = 0
    row_height = 0
    for name, crop in crops:
        width, height = crop.size
        if x + width > ATLAS_SIZE[0]:
            x = 0
            y += row_height + PADDING
            row_height = 0

        if y + height > ATLAS_SIZE[1]:
            raise ValueError("Packed character atlas exceeds 1024x1024; increase atlas size or reduce retained assets.")

        packed_image.paste(crop, (x, y))
        packed_positions.append(
            {
                "name": name,
                "x": x,
                "y": y,
                "width": width,
                "height": height,
            }
        )
        x += width + PADDING
        row_height = max(row_height, height)

    packed_image.save(image_path)
    atlas_json["SubTexture"] = packed_positions
    save_json(atlas_path, atlas_json)


def main() -> None:
    root = load_json(SKELETON_PATH)

    rewrite_character_armature(
        find_armature(root, "HeadsDB"),
        [
            (spec["head_source"], f"custom_head_{spec['id']}", spec["head_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer 4",
        "head",
    )
    rewrite_character_armature(
        find_armature(root, "BodyDB"),
        [
            (spec["body_source"], f"custom_body_{spec['id']}", spec["body_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_5",
        "body",
    )
    rewrite_character_armature(
        find_armature(root, "dbanims/LeftHandDB"),
        [
            (spec["left_hand_source"], f"custom_left_hand_{spec['id']}", spec["hand_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_3",
        "hand",
    )
    rewrite_character_armature(
        find_armature(root, "dbanims/RightHandDB"),
        [
            (spec["right_hand_source"], f"custom_right_hand_{spec['id']}", spec["hand_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_4",
        "hand",
    )
    rewrite_character_armature(
        find_armature(root, "dbanims/LeftHandDB2"),
        [
            (spec["dig_hand_source"], f"custom_dig_hand_{spec['id']}", spec["hand_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_4",
        "hand",
    )
    rewrite_character_armature(
        find_armature(root, "dbanims/LegsDB"),
        [
            (spec["leg_source"], f"custom_leg_{spec['id']}", spec["leg_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_5",
        "leg",
    )
    rewrite_character_armature(
        find_armature(root, "dbanims/LegsDB2"),
        [
            (spec["leg_source"], f"custom_leg_{spec['id']}", spec["leg_anim_source"])
            for spec in CHARACTER_SPECS
        ],
        "Layer_5",
        "leg",
    )

    used_names = collect_image_display_names(root)
    source_name_map = build_source_name_map()
    repack_texture(TEXTURE_PATH, TEXTURE_JSON_PATH, used_names, source_name_map)
    save_json(SKELETON_PATH, root)

    print(f"Rebuilt active character atlas and skeleton at {DRAGON_BONES_DIR}")


if __name__ == "__main__":
    main()
