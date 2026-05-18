# Font Provenance

Reviewed on: `2026-05-18`

This note records what the repository can currently prove about the runtime font files bundled under `Assets/BasketballLegends2020/Resources/BL2020/Fonts`.

## Verified exact upstream matches

The following files were verified as exact `SHA256` matches against the current upstream files in the official Google Fonts repository:

| Runtime File | Local Verification Basis | Upstream Source | License Basis | Current Status |
| --- | --- | --- | --- | --- |
| `Rajdhani-Bold.ttf` | Exact `SHA256` match: `691470DD3286A14E9677940D0BF75796179841BA5215CBDA1A2C8910A3226AFD` | `google/fonts` `ofl/rajdhani`: <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `OFL.txt` in the same upstream family directory (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |
| `Rajdhani-SemiBold.ttf` | Exact `SHA256` match: `94BBD25A18CA665999FEB05A537DE9FD2B860DCFB78BBE9CA00270825BF235DA` | `google/fonts` `ofl/rajdhani`: <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `OFL.txt` in the same upstream family directory (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |
| `Griffy-Regular.ttf` | Exact `SHA256` match: `C889C3F8D169631386A297EAE8B5BDEFBF8D06AA9F4F325FAEC31B9B5F38EEB5` | `google/fonts` `ofl/griffy`: <https://github.com/google/fonts/tree/main/ofl/griffy> | `OFL.txt` in the same upstream family directory (`SIL Open Font License 1.1`) | Safe to describe as an exact upstream Google Fonts file currently bundled in the project |

## Runtime fonts that are still unresolved

The repository does not currently contain a source page, creator note, or clear license note for these bundled runtime font files:

- `Impact.ttf`
- `Impact2.ttf`
- `AgencyBold.ttf`
- `CfCrackBold.ttf`

These files are still active runtime resources in the current build, so they should not be described as open-source or coursework-authored fonts unless that provenance is documented later.

## Safe submission wording

You can safely say:

> The current build includes verified Google Fonts copies for `Rajdhani-Bold`, `Rajdhani-SemiBold`, and `Griffy-Regular`, and those exact files are documented in the repository. A separate subset of bundled legacy UI fonts still requires either clearer provenance records or replacement before a fully final submission package.

## Recommended follow-up

- Keep `Rajdhani-Bold.ttf`, `Rajdhani-SemiBold.ttf`, and `Griffy-Regular.ttf` as documented runtime fonts.
- Replace or document `Impact.ttf`, `Impact2.ttf`, `AgencyBold.ttf`, and `CfCrackBold.ttf` before any final formal coursework hand-in that requires complete asset provenance.
