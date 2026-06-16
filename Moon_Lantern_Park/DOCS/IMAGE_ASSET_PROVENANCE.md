# Image Asset Provenance

Reviewed on: `2026-06-16`

This document records the source and copyright position for the project-specific image assets used by Moon Lantern Park.

## Scope

This document covers the project-authored game image assets under:

- `Assets/mlp/Resources/mlp/**/*.png`
- `ArtSource/**/*.png`

It does not cover Unity/TextMesh Pro package default resources such as:

- `Assets/TextMesh Pro/Sprites/EmojiOne.png`

Those package resources are documented separately in `ASSET_CREDITS.md` and `DOCS/FONT_PROVENANCE.md`.

## Main Declaration

The project-specific game image assets in Moon Lantern Park are documented as ChatGPT image-generation based project art. The final Unity PNG files may be direct selected outputs, edited/cropped cutouts, composites, atlas textures, charge masks, or locally generated UI derivatives created from the project's visual direction and tooling.

No third-party photographs, commercial sprite sheets, downloaded UI packs, downloaded character art, or downloaded icon packs are intentionally used as project game art.

Recommended credit wording:

> Project image assets were created from ChatGPT image generation and project-side editing, compositing, procedural export, or Unity integration for Moon Lantern Park.

## AI Image Generation Record

| Field | Record |
| --- | --- |
| Tool used | ChatGPT image generation |
| Purpose | Generate the visual basis for Halloween-themed arcade basketball characters, portraits, icons, UI imagery, story images, arena/backdrop art, ball skins, and visual-effect source art |
| Output used | PNG image outputs used directly, as source inputs for local rebuild scripts, or as visual references/source material for project-side composites and UI derivatives |
| What was modified | Cropping, transparent-background cleanup, chroma-key removal, resizing, color adjustment, atlas packing, icon mask creation, UI compositing, procedural UI export, and Unity import configuration |
| What was created by the project | Final Unity resource layout, image selection, post-processing choices, atlas metadata, DBLite/DragonBones-compatible rebuild data, UI derivative generation, UI integration, and gameplay use |
| What was used unchanged from third-party image sources | None intentionally used for project game art |
| Current limitation | Exact prompt history is not stored in this repository. Future generated image assets should preserve prompt notes, generation date, and tool version where possible. |

## Runtime Image Inventory

The current project runtime contains `75` project PNG files under `Assets/mlp/Resources/mlp`.

| Folder | Count | Runtime files | Use in game |
| --- | ---: | --- | --- |
| `Assets/mlp/Resources/mlp/Atlases` | 3 | `gameplay.png`, `interface.png`, `skillfx.png` | Core gameplay, interface, and skill-effect sprite atlases |
| `Assets/mlp/Resources/mlp/DragonBones` | 1 | `texture2.png` | Character animation texture sheet |
| `Assets/mlp/Resources/mlp/Portraits` | 1 | `portraits_ui.png` | Character select and match-intro portrait atlas |
| `Assets/mlp/Resources/mlp/Hud` | 2 | `scoreboard_halloween.png`, `popup_halloween.png` | Scoreboard and match popup UI |
| `Assets/mlp/Resources/mlp/Help` | 9 | `help_board.png`, `help_card.png`, `help_chip.png`, `help_dim.png`, `help_keycap.png`, `help_line.png`, `help_spotlight.png`, `help_stage.png`, `help_tab.png` | Tutorial/help overlay visual pieces |
| `Assets/mlp/Resources/mlp/Images` | 7 | `logo.png`, `menu_background_halloween_spotlight.png`, `menu_background_moonlit_gym.png`, `pause_button.png`, `music_button_on.png`, `music_button_off.png`, `help_button.png` | Menu branding, menu backgrounds, and global icon buttons |
| `Assets/mlp/Resources/mlp/Images/Gameplay` | 15 | `arena_halloween_backdrop.png`, `basket_halloween_rim.png`, `basket_halloween_front_ear.png`, `player_fallback_avatar.png`, `player_shadow_primary.png`, `player_shadow_primary_red.png`, `player_shadow_secondary.png`, `player_shadow_ball.png`, `ball_halloween_ghoul_green.png`, `ball_halloween_pumpkin_ember.png`, `ball_halloween_moonlit_violet.png`, `ball_halloween_jack_o_lantern.png`, `ball_halloween_evil_eye.png`, `ball_halloween_cursed_8ball.png`, `ball_halloween_candy_swirl.png` | Arena, basket pieces, player shadows, fallback avatar, and ball skins |
| `Assets/mlp/Resources/mlp/Images/SkillFx` | 4 | `reaper_dash_core.png`, `reaper_dash_accent.png`, `bad_luck_core.png`, `bad_luck_accent.png` | Standalone skill-effect textures |
| `Assets/mlp/Resources/mlp/Images/SkillIcons` | 16 | `*_skill_icon.png`, `*_skill_charge_mask.png` for the 8 playable characters | Skill icons and charge masks in character and HUD presentation |
| `Assets/mlp/Resources/mlp/Images/Story` | 6 | `adventure_comic_page_01.png`, `adventure_comic_page_02.png`, `adventure_comic_page_03.png`, `tournament_comic_page_01.png`, `tournament_comic_page_02.png`, `tournament_comic_page_03.png` | Adventure and tournament story presentation |
| `Assets/mlp/Resources/mlp/Images/UI` | 11 | `adventure_treasure_map_bg.png`, `awards_podium_base.png`, `awards_result_plaque.png`, `awards_showcase_panel.png`, `emblem_orb.png`, `energy_button_plate.png`, `frame_match_card_active.png`, `frame_match_card_idle.png`, `frame_panel_large.png`, `menu_button_plate.png`, `panel_fill_soft.png` | Adventure map, awards UI, menus, panels, match cards, and UI framing |

## Source Image Inventory

The current `ArtSource` folder contains `11` source PNG files used as higher-resolution rebuild inputs.

| Folder | Source files | Use |
| --- | --- | --- |
| `ArtSource/CharacterRefresh` | `reaper_icon_source.png`, `ghost_clown_icon_source.png`, `skull_pirate_icon_source.png` | Source images for the May 2026 character/portrait refresh pipeline |
| `ArtSource/SkillIcons` | `reaper_skill_source.png`, `ghost_clown_skill_source.png`, `skull_pirate_skill_source.png`, `vampire_skill_source.png`, `candleman_skill_source.png`, `scarecrow_skill_source.png`, `witch_skill_source.png`, `black_cat_skill_source.png` | Source images for rebuilding final skill icons and charge masks |

## Modification and Integration Notes

| Asset group | Main edits made after ChatGPT image generation |
| --- | --- |
| Character refresh images | Chroma-key cleanup, cutout preparation, atlas repacking, portrait alignment, DBLite/DragonBones texture integration |
| Skill icons | Chroma-key removal, centered icon layout, glow/shadow treatment, charge-mask generation, final export to Unity `Resources` |
| Menu/UI/HUD/help images | Cropping, resizing, panel composition, transparent-edge cleanup, visual consistency adjustments, Unity import settings |
| Ball and gameplay images | Cropping, masking to transparent sprites, scale tuning, gameplay readability checks |
| Story images | Exported as story/comic pages and imported as Unity runtime textures |
| Atlases and animation texture sheets | Packed into runtime texture sheets with matching JSON metadata and stable resource keys |

## Legal and Ethical Notes

- The project should not claim that AI-generated image outputs are licensed under a third-party open-source image license such as CC0 or CC BY.
- The project should credit the use of ChatGPT image generation and project-side processing because the coursework submission asks for external resources and AI declarations.
- Future image generations should avoid asking for direct imitation of living artists, copyrighted franchises, commercial game characters, logos, or trademarked visual identities.
- Future replacements should keep prompt notes, generation date, selected output filenames, and final edited runtime paths together in this document.

## Submission Checklist For Images

- State that project game images are based on ChatGPT image generation and project-side processing.
- State that the images were edited and integrated by the project author.
- State that no downloaded third-party image packs are intentionally used as project game art.
- Keep Unity/TextMesh Pro default `EmojiOne.png` separate from the project-authored AI image declaration.
- Point assessors to `ASSET_CREDITS.md` and this file for the complete image provenance record.
