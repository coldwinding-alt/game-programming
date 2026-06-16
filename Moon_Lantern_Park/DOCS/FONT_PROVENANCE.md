# Font Provenance

Reviewed on: `2026-06-04`

This note records what the repository can currently prove about the runtime font files bundled under `Assets/mlp/Resources/mlp/Fonts`.

## Verified exact upstream matches kept under their original family names

The following files were verified as exact `SHA256` matches against the current upstream files in the official Google Fonts repository:

| Runtime File | Local Verification Basis | Upstream Source | License Basis | Current Status |
| --- | --- | --- | --- | --- |
| `Rajdhani-Bold.ttf` | Exact `SHA256` match: `691470DD3286A14E9677940D0BF75796179841BA5215CBDA1A2C8910A3226AFD` | `google/fonts` `ofl/rajdhani`: <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `DOCS/FontLicenses/Rajdhani-OFL.txt` (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |
| `Rajdhani-SemiBold.ttf` | Exact `SHA256` match: `94BBD25A18CA665999FEB05A537DE9FD2B860DCFB78BBE9CA00270825BF235DA` | `google/fonts` `ofl/rajdhani`: <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `DOCS/FontLicenses/Rajdhani-OFL.txt` (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |
| `Griffy-Regular.ttf` | Exact `SHA256` match: `C889C3F8D169631386A297EAE8B5BDEFBF8D06AA9F4F325FAEC31B9B5F38EEB5` | `google/fonts` `ofl/griffy`: <https://github.com/google/fonts/tree/main/ofl/griffy> | `DOCS/FontLicenses/Griffy-OFL.txt` (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |

## Compatibility filenames replaced with safe Google Fonts copies

On `2026-06-04`, the repository replaced the previously risky or unclear bundled font binaries while keeping the existing Unity resource filenames stable so code paths, prefabs, and serialized references did not need a large rename pass.

| Runtime File | Bundled Upstream Family/File | Local Verification Basis | Upstream Source | License Basis | Current Status |
| --- | --- | --- | --- | --- | --- |
| `Impact.ttf` | `Anton-Regular.ttf` | Exact `SHA256` match: `A4BA3A92350EBB031DA0CB47630AC49EB265082CA1BC0450442F4A83AB947CAB` | `google/fonts` `ofl/anton`: <https://github.com/google/fonts/tree/main/ofl/anton> | `DOCS/FontLicenses/Anton-OFL.txt` (`SIL Open Font License 1.1`) | Safe runtime replacement; compatibility filename retained to avoid code/prefab churn |
| `Impact2.ttf` | `Anton-Regular.ttf` | Exact `SHA256` match: `A4BA3A92350EBB031DA0CB47630AC49EB265082CA1BC0450442F4A83AB947CAB` | `google/fonts` `ofl/anton`: <https://github.com/google/fonts/tree/main/ofl/anton> | `DOCS/FontLicenses/Anton-OFL.txt` (`SIL Open Font License 1.1`) | Safe runtime replacement; compatibility filename retained to avoid code/prefab churn |
| `AgencyBold.ttf` | `BarlowCondensed-Bold.ttf` | Exact `SHA256` match: `E476562EC9C1E16CF16475895B511F08C804F438CC9A9F80A44EA50A0EEB5B65` | `google/fonts` `ofl/barlowcondensed`: <https://github.com/google/fonts/tree/main/ofl/barlowcondensed> | `DOCS/FontLicenses/BarlowCondensed-OFL.txt` (`SIL Open Font License 1.1`) | Safe runtime replacement; compatibility filename retained to avoid code/prefab churn |
| `CfCrackBold.ttf` | `Bungee-Regular.ttf` | Exact `SHA256` match: `C4F5361CE120AF3E6B9156D0BF379FA19CDA2EA0CD18AC01FD99596C6BF66E3F` | `google/fonts` `ofl/bungee`: <https://github.com/google/fonts/tree/main/ofl/bungee> | `DOCS/FontLicenses/Bungee-OFL.txt` (`SIL Open Font License 1.1`) | Safe runtime replacement; compatibility filename retained to avoid code/prefab churn |

## Safe submission wording

You can safely say:

> The current build uses documented Google Fonts runtime files for `Rajdhani-Bold`, `Rajdhani-SemiBold`, and `Griffy-Regular`, and it replaces the former legacy `Impact`, `Impact2`, `AgencyBold`, and `CfCrackBold` bundle slots with exact Google Fonts copies (`Anton`, `Barlow Condensed Bold`, and `Bungee`) while keeping the Unity resource filenames stable for compatibility.

## Unity/TextMesh Pro default resources

The repository also contains Unity/TextMesh Pro default support resources under `Assets/TextMesh Pro/`.

| File | Source / status | License or attribution basis | Notes |
| --- | --- | --- | --- |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | Unity/TextMesh Pro default font resource | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` (`SIL Open Font License 1.1`) | Package support font, separate from the project-selected runtime font set under `Assets/mlp/Resources/mlp/Fonts/` |
| `Assets/TextMesh Pro/Sprites/EmojiOne.png` | Unity/TextMesh Pro default emoji sprite sample | `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt` | Package default sprite sample, not a project-authored AI image asset |

These TextMesh Pro files should not be described as ChatGPT-generated project image assets. If they remain in the final repository, they should be treated as Unity/TMP package default resources retained with their included license/attribution files.

## Recommended follow-up

- Keep `DOCS/FontLicenses/` with the repository so the bundled `OFL` texts stay alongside the coursework submission materials.
- If you later rename the compatibility filenames to their real family names, update `mlpFontCache`, `mlpTmpFontCache`, `ASSET_CREDITS.md`, and this file in the same change.
