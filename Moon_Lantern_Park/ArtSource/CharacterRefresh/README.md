# Character Refresh Sources

This folder is the source-of-truth for the May 2026 roster refresh that replaces the first three Halloween characters.

## Mapping

- `custom_head_pumpkin` runtime slot -> `REAPER`
- `custom_head_frankenstein` runtime slot -> `GHOST CLOWN`
- `custom_head_mummy` runtime slot -> `SKULL PIRATE`

The runtime still keeps the stable DragonBones/DBLite slot keys so gameplay code and character selection do not need to change. `sk2.json` itself is generated from `Tools/Art/rebuild_runtime_dragonbones_skeleton.py`; do not treat it as an exported legacy editor project.

## Source Files

- `reaper_icon_source.png`
- `ghost_clown_icon_source.png`
- `skull_pirate_icon_source.png`

These are the current right-facing portrait/gameplay source images on chroma-key backgrounds. They were generated with ChatGPT image generation and then kept as project source inputs for the local rebuild pipeline. The rebuild script removes the chroma key automatically, so committed cutout intermediates are no longer required for the live pipeline.

Older first-pass sources and cutout intermediates were removed after the pipeline switched to chroma-key cleanup directly inside `rebuild_character_refresh_assets.py`, so the folder now keeps only the active rebuild inputs.

## Rebuild Workflow

Run:

```powershell
python Tools/Art/rebuild_character_refresh_assets.py
```

The script updates:

- `Assets/mlp/Resources/mlp/Portraits/portraits_ui.png`
- `Assets/mlp/Resources/mlp/DragonBones/texture2.png`
- `Assets/mlp/Resources/mlp/DragonBones/texture2.json`

The DragonBones atlas is rebuilt at 2x resolution and tagged with `pixelsPerUnit = 2.0` so gameplay characters stay the same on-screen size while rendering from a sharper source atlas.

It also writes a visual verification sheet to:

- `tmp/character_refresh/character_refresh_preview.png`

When changing the runtime skeleton or animation contract, run:

```powershell
python Tools/Art/rebuild_runtime_dragonbones_skeleton.py
```

That script rewrites `Assets/mlp/Resources/mlp/DragonBones/sk2.json` from the project-owned DBLite-compatible definition and keeps the `texture2.json` root package name aligned with the current project.
