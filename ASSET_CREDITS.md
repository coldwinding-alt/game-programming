# Asset Credits

This file records the current resource files included in the coursework build, their immediate source, the repository import date, whether they were modified during Unity migration, and their current replacement status.

## Current Migrated Resource Files

| Path | Use | Source | Date | Modified | Current Status |
| --- | --- | --- | --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.json` | Court, basket, HUD, and gameplay sprite atlas | local archived reference package used during coursework migration | 2026-04-18 | copied into Unity `Resources`; content kept as-is for the current port | reference material migrated for coursework build; pending later replacement/cleanup |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.json` | Menu and interface sprite atlas | local archived reference package used during coursework migration | 2026-04-18 | copied into Unity `Resources`; content kept as-is for the current port | reference material migrated for coursework build; pending later replacement/cleanup |
| `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/Players.json`, `sk.json`, `sk2.json`, `texture.png`, `texture.json`, `texture2.png`, and `texture2.json` | Player animation data and texture layout for the lightweight runtime | local archived reference package used during coursework migration | 2026-04-18 | copied into Unity `Resources`; loaded through a custom subset runtime | reference material migrated for coursework build; pending later replacement/cleanup |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/logo.png` | Title logo used on the main menu | local archived reference package used during coursework migration | 2026-04-18 | copied into Unity `Resources`; content kept as-is for the current port | reference material migrated for coursework build; pending later replacement/cleanup |
| `Assets/BasketballLegends2020/Resources/BL2020/Sound/*.ogg` | Match, UI, and feedback audio | local archived reference package used during coursework migration | 2026-04-18 | copied into Unity `Resources`; imported as Unity audio assets | reference material migrated for coursework build; pending later replacement/cleanup |
| `prototype.jpg` | Existing repository concept image retained from the earlier repository baseline | existing repository file on `main` | 2026-04-18 | not modified in this pass | repository archive/reference image |

## Working Rule For Later Cleanup

- Any replacement art, audio, or animation data should be added here with the exact file path.
- If a migrated reference asset is replaced, update the row instead of leaving the old source note ambiguous.
- The current resource package is kept only to support the coursework build while later cleanup and replacement work is still pending.
