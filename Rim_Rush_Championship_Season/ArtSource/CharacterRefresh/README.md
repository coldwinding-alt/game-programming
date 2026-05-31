# Character Refresh Sources

This folder is the source-of-truth for the May 2026 roster refresh that replaces the first three Halloween characters.

## Mapping

- `custom_head_pumpkin` runtime slot -> `REAPER ACOLYTE`
- `custom_head_frankenstein` runtime slot -> `GHOST CLOWN`
- `custom_head_mummy` runtime slot -> `SKULL PIRATE`

The runtime still keeps the original DragonBones slot keys so the existing `sk2.json` animation package does not need to be renamed.

## Source Files

- `reaper_acolyte_icon_source.png`
- `ghost_clown_icon_source.png`
- `skull_pirate_icon_source.png`

These are the current right-facing portrait/gameplay source images on chroma-key backgrounds. The rebuild script removes the chroma key automatically, so committed cutout intermediates are no longer required for the live pipeline.

- `reaper_acolyte_source.png`
- `ghost_clown_source.png`
- `skull_pirate_source.png`

These older first-pass sources are kept only for reference.

## Rebuild Workflow

Run:

```powershell
python Tools/Art/rebuild_character_refresh_assets.py
```

The script updates:

- `Assets/rimrush/Resources/rimrush/Portraits/portraits_ui.png`
- `Assets/rimrush/Resources/rimrush/DragonBones/texture2.png`
- `Assets/rimrush/Resources/rimrush/DragonBones/texture2.json`

The DragonBones atlas is rebuilt at 2x resolution and tagged with `pixelsPerUnit = 2.0` so gameplay characters stay the same on-screen size while rendering from a sharper source atlas.

It also writes a visual verification sheet to:

- `tmp/character_refresh/character_refresh_preview.png`
