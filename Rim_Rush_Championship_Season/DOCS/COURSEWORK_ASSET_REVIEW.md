# Coursework Asset Review

Reviewed on: `2026-04-23`

This note was prepared against:

- `DOCS/评分标准.txt`
- `DOCS/lecturer_asset_resources_guide.pdf`

Its purpose is to document the currently used audio materials, clarify which visual assets can honestly be described as AI-generated, and flag any coursework submission risks before the final hand-in.

## 1. What The Module Documents Require

From the marking guidance and lecturer handout, the project should be able to show:

- sensible and legally aware use of tools, assets, and resources
- a clear credit note for each external asset
- source page
- creator or institution
- license or usage page
- date accessed
- changes made
- no use of assets with unclear rights
- a README that explains the game and includes credits for external assets or resources

## 2. Current Project Review

### What is already in a safer state

- The rebuilt core Halloween `SFX` set is newly generated inside this project and ships as project-authored waveforms.
- The current menu background music has a clear public source and clear `CC0` usage terms.
- The dedicated Halloween skill FX atlas is already documented as rebuilt from custom AI key art.
- The Halloween supersheet replacements inside `texture2.png` are already documented as custom AI-generated replacement components.

### What is currently risky

#### 2.1 Blanket claim that "all current image assets are AI-generated"

This claim is **not currently supportable** from the repository evidence.

Why:

- `gameplay.png` / `interface.png` are still active runtime atlases, but their provenance notes are not yet complete enough to support an “all visual assets are AI-generated” claim.
- `sk2.json`, `texture2.json`, and the non-effect character portions of `texture2.png` are still active runtime animation/content files, and the repo does not yet document the whole active character sheet as fully AI-generated coursework art.
- `logo.png`, `scoreboard_halloween.png`, and `popup_halloween.png` are active runtime images, but their AI/source notes are not currently documented in the repo.
- The top-right icon family currently comes from the user-provided sheet `四个图标.png`; the repo does not currently contain a formal note proving that sheet as an AI-generated source asset.

Because of that, the safe statement for the report is:

> The newly created Halloween replacement art in this coursework pass was AI-generated and then edited, cropped, and integrated by the student, but some runtime atlases, character sheets, and interface files in the current build still need separate provenance notes or replacement before making an "all images are AI-generated" claim.

#### 2.2 Collision audio still needs provenance notes

The following audio is still active or stored in the runtime package without complete provenance notes:

- active collision/gameplay audio:
  - `10_B_Ring.ogg`
  - `16_B_Bounce.ogg`
  - `21_B_NET.ogg`
  - `22_B_Brick.ogg`
  - `23_B_Basket.ogg`

If any of these came from previously imported materials or another unclear source, they are a coursework risk under the lecturer guidance and should either be:

- replaced
- documented with a valid source and license
- or explicitly removed from the final playable build

#### 2.3 Fonts also need source notes

This is outside the audio/image scope you asked for, but it is still a real submission risk because these fonts are active runtime resources:

- `Impact.ttf`
- `Impact2.ttf`
- `CfCrackBold.ttf`
- `AgencyBold.ttf`
- `Rajdhani-SemiBold.ttf`
- `Rajdhani-Bold.ttf`
- `Griffy-Regular.ttf`

The current repository does not include a font provenance note. That should be added before submission.

## 3. Audio Asset Register

### 3.1 Active self-authored/generated audio

| Runtime Files | Use | Source Page | Creator | License / Rights Basis | Accessed | Changes Made | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Sound/2_M_Whistle.wav`, `4_P_Teleport.wav`, `5_P_Swoosh.wav`, `6_P_Energy.wav`, `7_P_Stunned.wav`, `8_B_Steel.wav`, `9_M_Buzzer.wav`, `11_P_MegaStart.wav`, `13_P_Shield.wav`, `17_P_Dash.wav`, `18_P_SuperDash.wav`, `19_M_Countdown.wav`, `20_ButtonSnd.wav` | Core match, skill, and UI cues used by the current runtime | N/A - produced locally through `Tools/Audio/generate_halloween_core_sfx.py` | Student / project-authored synthesis pipeline | Original coursework asset authored in-project; no external waveform reuse documented | N/A | Generated locally with `numpy`, `scipy`, ADSR/filter/echo synthesis, then retuned for a darker and less synthetic match to the current visual style | Safe to describe as self-authored/generated coursework audio |

### 3.2 Active public music source

| Runtime File | Use | Source Page | Creator | License / Rights Page | Accessed | Changes Made | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Sound/24_TrackSnd.ogg` | Main menu background music loop | [OpenGameArt - Spooky Action Loop "Hallow Quest"](https://opengameart.org/content/spooky-action-loop-hallow-quest) | Zane Little Music | `CC0`, see [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `2026-04-23` | Selected the more energetic Halloween action loop and updated the current Unity music asset while keeping the live resource key and playback wiring stable | Safe to use if this source page and license are kept in the final submission notes |

### 3.3 Active runtime audio that still needs follow-up

| Runtime Files | Use | Current Provenance State | Risk |
| --- | --- | --- | --- |
| `10_B_Ring.ogg`, `16_B_Bounce.ogg`, `21_B_NET.ogg`, `22_B_Brick.ogg`, `23_B_Basket.ogg` | Ring, bounce, net, brick, and basket collision feedback | Stored in the runtime package, but the repo does not currently record a source page, creator, license, access date, or edit note | Medium to high risk if these were imported earlier without complete source records or any clear usage note |

## 4. Image Asset Status

### 4.1 Image assets that can currently be described as AI-generated or AI-assisted custom replacements

| Files | Runtime Status | Safe Description |
| --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/skillfx.png` and `skillfx.json` | Active | Custom Halloween skill FX atlas rebuilt from AI-generated key art and repacked for runtime use |
| Replacement subtextures inside `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture2.png` for `fx_Blur_mol0..4`, `fx_fire_0..4`, `gsgbfyjgkh`, `fx_smoke_0..7`, `part1`, `fx_spl_0`, `fx_spl2_0` | Active | AI-generated Halloween effect components inserted into the current DragonBones supersheet while keeping the live layout/timing contract stable |

### 4.2 Image assets that are custom/project-generated, but are not yet safe to label as AI-generated from repo evidence alone

| Files | Runtime Status | Why They Should Not Yet Be Claimed As AI-Generated |
| --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_ghoul_green.png`, `ball_halloween_pumpkin_ember.png`, `ball_halloween_moonlit_violet.png` | Active | The repo records them as project-generated/redrawn Halloween ball art, but it does not currently include a clear AI provenance note |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/pause_button.png`, `music_button_on.png`, `music_button_off.png`, `help_button.png` | Active | These now come from the user-provided `四个图标.png` sheet; the repo should not automatically call that sheet AI-generated unless you want to document it explicitly as such |

### 4.3 Active runtime image assets that are currently bundled but undocumented

| Files | Runtime Status | Current Provenance State |
| --- | --- | --- |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/gameplay.png` and `gameplay.json` | Active | Current runtime atlas package; provenance note still incomplete for an “all images are AI-generated” claim |
| `Assets/BasketballLegends2020/Resources/BL2020/Atlases/interface.png` and `interface.json` | Active | Current runtime atlas package; provenance note still incomplete for an “all images are AI-generated” claim |
| `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/sk2.json`, `texture2.json`, and non-replaced parts of `texture2.png` | Active | Current runtime animation package; rebuilt into the active Halloween roster, but not currently documented as fully AI-generated |
| `Assets/BasketballLegends2020/Resources/BL2020/Images/logo.png` | Active | Source note is currently too vague for a final coursework submission |
| `Assets/BasketballLegends2020/Resources/BL2020/Hud/scoreboard_halloween.png` and `popup_halloween.png` | Active | Active runtime HUD textures, but no source/provenance note is currently present in the repo |

## 5. Recommended Submission Wording

### Safe wording to use now

You can safely say:

> The custom Halloween replacement sound effects were self-authored inside the project, and the current menu music uses a documented public `CC0` source. Several of the new Halloween replacement visual effects were AI-generated and then edited, cropped, and integrated by the student. Some runtime atlases, HUD files, and character sheet resources in the current build still need fuller provenance notes before the project can claim that all shipped visuals are AI-generated.

### Wording that should not be used yet

Do **not** currently write:

> All images used in the project are AI-generated.

That sentence is too strong for the current repository evidence.

## 6. Recommended Next Actions Before Final Submission

1. Replace or document the remaining active image packages:
   - `gameplay.png`
   - `interface.png`
   - `logo.png`
   - `scoreboard_halloween.png`
   - `popup_halloween.png`
2. Decide whether the four top-right icons should be declared as AI-generated. If yes, add a clear note that `四个图标.png` is your own AI-generated source sheet.
3. Replace or fully document the remaining active collision sounds.
4. Add a separate font credits note for the current bundled `.ttf` files.
5. Keep this file, `ASSET_CREDITS.md`, and the final README consistent with each other before submission.
