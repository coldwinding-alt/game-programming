# Character Refresh Sources

This folder is the source-of-truth for the May 2026 roster refresh that replaces the first three Halloween characters.

## Mapping

- `custom_head_pumpkin` runtime slot -> `REAPER ACOLYTE`
- `custom_head_frankenstein` runtime slot -> `GHOST CLOWN`
- `custom_head_mummy` runtime slot -> `SKULL PIRATE`

The runtime still keeps the original DragonBones slot keys so the existing `sk2.json` animation package does not need to be renamed.

## Source Files

- `reaper_acolyte_source.png`
- `ghost_clown_source.png`
- `skull_pirate_source.png`

These are the original generated portrait sources on chroma-key backgrounds.

- `reaper_acolyte_cutout.png`
- `ghost_clown_cutout.png`
- `skull_pirate_cutout.png`

These are the transparency-clean cutouts used by `Tools/Art/rebuild_character_refresh_assets.py`.

## Rebuild Workflow

Run:

```powershell
python Tools/Art/rebuild_character_refresh_assets.py
```

The script updates:

- `Assets/rimrush/Resources/rimrush/Portraits/portraits_ui.png`
- `Assets/rimrush/Resources/rimrush/DragonBones/texture2.png`

It also writes a visual verification sheet to:

- `tmp/character_refresh/character_refresh_preview.png`
