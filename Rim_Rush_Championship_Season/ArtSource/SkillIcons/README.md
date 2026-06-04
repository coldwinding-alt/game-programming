# Skill Icon Sources

This folder stores the June 2026 high-resolution source art used to rebuild the standalone skill icons.

## Mapping

- `reaper_skill_source.png` -> `REAPER` / `DASH STEAL`
- `ghost_clown_skill_source.png` -> `GHOST CLOWN` / `BLINK DUNK`
- `skull_pirate_skill_source.png` -> `SKULL PIRATE` / `HOOP SHIELD`
- `vampire_skill_source.png` -> `VAMPIRE` / `BLINK DUNK`
- `candleman_skill_source.png` -> `CANDLEMAN` / `SPEED BOOST`
- `scarecrow_skill_source.png` -> `SCARECROW` / `NEXT SCORE +2`
- `witch_skill_source.png` -> `WITCH` / `FREEZE 2 SEC`
- `black_cat_skill_source.png` -> `BLACK CAT` / `DASH STEAL`

These images were generated on flat chroma-key backgrounds so the local rebuild script can remove the background and export clean transparent UI icons.

## Rebuild Workflow

Run:

```powershell
python Tools/Art/build_skill_icon_assets.py
```

The script writes final UI-ready textures to:

- `Assets/rimrush/Resources/rimrush/Images/SkillIcons/`

It also writes a quick verification sheet to:

- `tmp/skill_icons/skill_icons_preview.png`
