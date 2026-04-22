# Asset Credits

This file records the active resource files included in the coursework build, when they were added to the repository, how they are used in the game, and their current replacement status.

## Current Resource Files

| Path | Use | Source Note | Date | Modified | Current Status |
| --- | --- | --- | --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.json` | Court, basket, HUD, and gameplay sprite atlas | current project content package | 2026-04-18 | copied into Unity `Resources` for runtime loading | active runtime asset package; can be replaced later if the visual direction changes |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.json` | Menu and interface sprite atlas | current project content package | 2026-04-18 | copied into Unity `Resources` for runtime loading | active runtime asset package; can be replaced later if the visual direction changes |
| `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/Players.json`, `sk.json`, `sk2.json`, `texture.png`, `texture.json`, `texture2.png`, and `texture2.json` | Character animation data and texture layout for the lightweight runtime | current project content package | 2026-04-18 | integrated into the Unity runtime content pipeline | active gameplay animation package; character visuals continue to be updated in place |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/logo.png` | Title logo used on the main menu | project UI asset package | 2026-04-18 | copied into Unity `Resources` | active menu branding asset |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_ghoul_green.png`, `ball_halloween_pumpkin_ember.png`, and `ball_halloween_moonlit_violet.png` | Match ball theme variants loaded at runtime for both the loose ball object and the in-hand DragonBones ball slots | project-generated Halloween icon-style gameplay ball pack redrawn to match the roster/skill icon art direction and resized to `36x36` transparent sprites | 2026-04-22 | regenerated, cropped, and centered under Unity `Resources/BL2020/Images/Gameplay` for runtime loading | active gameplay replacement art for per-match ball randomization |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/basket_halloween_main.png` and `basket_halloween_front_rim.png` | Earlier hoop overlay experiment retained in the repository for reference only | project-generated hoop reskin test pack from the earlier Halloween art pass | 2026-04-22 | kept on disk but no longer used by live runtime rendering | inactive reference art; gameplay hoops now render from the original `gameplay` atlas |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/skillfx.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/skillfx.json` | Dedicated shield and teleport skill FX atlas loaded independently from `gameplay` | project-generated Halloween skill FX atlas rebuilt from custom AI key art while preserving the original frame names, source sizes, and runtime timing contract | 2026-04-22 | generated and added under Unity `Resources/BL2020/Atlases` for runtime lookup through `BLAssets.Atlases.SkillFx` | active runtime skill FX package for `ShieldMC*` and `teleport*`; intentionally isolated from current hoop and ball rollback work |
| `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture2.png` | DragonBones texture sheet for super dunk and super dash visuals | in-place Halloween replacement of `man_fire_01` and `shield_animation_01 / shield_anim` source subtextures using custom AI-generated ember, smoke, streak, and speed-smear components while keeping `texture2.json` and `sk2.json` layout/timing intact | 2026-04-22 | updated existing texture sheet in place without renaming subtextures | active runtime supersheet update for `fx_Blur_mol0..4`, `fx_fire_0..4`, `gsgbfyjgkh`, `fx_smoke_0..7`, `part1`, `fx_spl_0`, and `fx_spl2_0` |
| `Assets/BasketballLegends2020/Resources/BL2020/Sound/*.ogg` | Match, UI, and feedback audio | project audio package | 2026-04-18 | imported as Unity audio assets | active runtime audio package |
| `prototype.jpg` | Archive concept image retained in the repository root | existing repository file on `main` | 2026-04-18 | not modified in this pass | archive image for internal review |

## Working Rule For Later Cleanup

- Any replacement art, audio, or animation data should be added here with the exact file path.
- If an asset is replaced, update the row instead of leaving the current source note ambiguous.
- Keep this file focused on the assets that ship with or directly support the current playable build.
