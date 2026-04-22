# Asset Credits

This file records the active resource files included in the coursework build, when they were added to the repository, how they are used in the game, and their current replacement status.

## Current Resource Files

| Path | Use | Source Note | Date | Modified | Current Status |
| --- | --- | --- | --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.json` | Court, basket, HUD, and gameplay sprite atlas | current project content package | 2026-04-18 | copied into Unity `Resources` for runtime loading | active runtime asset package; can be replaced later if the visual direction changes |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.png` and `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.json` | Menu and interface sprite atlas | current project content package | 2026-04-18 | copied into Unity `Resources` for runtime loading | active runtime asset package; can be replaced later if the visual direction changes |
| `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/Players.json`, `sk.json`, `sk2.json`, `texture.png`, `texture.json`, `texture2.png`, and `texture2.json` | Character animation data and texture layout for the lightweight runtime | current project content package | 2026-04-18 | integrated into the Unity runtime content pipeline | active gameplay animation package; character visuals continue to be updated in place |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/logo.png` | Title logo used on the main menu | project UI asset package | 2026-04-18 | copied into Unity `Resources` | active menu branding asset |
| `Assets/BasketballLegends2020/Resources/BL2020/Sound/*.ogg` | Match, UI, and feedback audio | project audio package | 2026-04-18 | imported as Unity audio assets | active runtime audio package |
| `prototype.jpg` | Archive concept image retained in the repository root | existing repository file on `main` | 2026-04-18 | not modified in this pass | archive image for internal review |

## Working Rule For Later Cleanup

- Any replacement art, audio, or animation data should be added here with the exact file path.
- If an asset is replaced, update the row instead of leaving the current source note ambiguous.
- Keep this file focused on the assets that ship with or directly support the current playable build.
