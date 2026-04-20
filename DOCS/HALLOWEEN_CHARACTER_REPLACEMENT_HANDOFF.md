# Halloween Character Replacement Handoff

## 1. Task Goal

This document is the working handoff and execution guide for replacing the player character visuals in the Unity port with four original Halloween-themed characters.

The goal is not to rebuild the animation system from scratch. The goal is to keep the current gameplay logic, current DragonBones motion system, and current Unity-side state machine, then replace only the character skin parts so the game still feels like the original, but with original Halloween-themed art.

The target result is:

- only 4 playable character designs in the project
- premium, polished, high-end Halloween basketball visual style
- clean replacement workflow that can continue across conversations
- Gemini-generated source art that can be handed to Codex for final Unity integration

## 2. Current Project Facts You Must Remember

### 2.1 How character visuals currently work

The current project does not use a normal Unity Animator-based sprite sheet workflow for the players.

It uses a lightweight DragonBones runtime:

- `Assets/BasketballLegends2020/Scripts/DragonBonesLite/DBLiteRuntime.cs`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/sk.json`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/sk2.json`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture.png`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture2.png`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture.json`
- `Assets/BasketballLegends2020/Resources/BL2020/DragonBones/texture2.json`

The runtime loads a main armature called `playerSmall`, then swaps child armatures or sub-parts for the character look.

Current visual part slots:

- `head`
- `body`
- `left hand`
- `right hand`
- `left leg`
- `right leg`
- `dighand`
- `digleg`

Current action and motion names that must remain compatible:

- `idle`
- `run`
- `idle_wb`
- `run_wb`
- `jump`
- `jump_wb`
- `landing`
- `landing_wb`
- `throw_land`
- `steal`
- `stun`
- `dash`
- `pumpStart`
- `pumpEnd`
- `blockStart`
- `blockEnd`
- `dunk1`
- `dunk2`
- `dunk3`
- `md_start`
- `md_mid`
- `md_end`
- `megadunk`
- `megadunk_end`

Important animation events already wired in code:

- `throw`
- `action`
- `dunk`
- `mega`

Important gameplay files tied to the character system:

- `Assets/BasketballLegends2020/Scripts/Data/BLPlayersData.cs`
- `Assets/BasketballLegends2020/Scripts/GameObjects/BLGameplayObjects.cs`
- `Assets/BasketballLegends2020/Scripts/Core/BLGameCore.cs`
- `Assets/BasketballLegends2020/Scripts/Core/BLMatchData.cs`

### 2.2 What this means for replacement art

For this project, the safest replacement strategy is:

- keep the existing armature names
- keep the existing animation names
- keep the existing slot structure
- replace the visual skin parts only

So the replacement is a skin swap task, not a full animation remake task.

### 2.3 Current practical gameplay scope

The current playable build only spawns one player per side during match play.

That is very important because it makes the art replacement task much easier:

- you do not need to prepare full 3-player teams now
- you only need 4 character designs total for the next pass
- the current best target is 4 teams with 1 player each

## 3. Recommended Replacement Strategy

### 3.1 Target scope

Recommended next target:

- `4` teams
- `1` player per team
- `4` unique heads
- `4` unique bodies
- `4` unique left-arm parts
- `4` unique right-arm parts
- `4` unique leg parts
- optional `4` alternate body variants for away/home color differences

### 3.2 Do not split the character the wrong way

For this project, do not think of the art as:

- head
- body
- shoes
- jersey

That is not the best split for the current rig.

The correct production split is:

- `head`: face, hair, head accessories, tiny neck base
- `body`: torso, jersey, shoulders, chest, waist/upper shorts area, but no arms
- `hand_left`: one isolated left arm piece from shoulder seam to fingers
- `hand_right`: one isolated right arm piece from shoulder seam to fingers
- `leg`: one isolated single leg piece from upper thigh area to shoe, designed to be reused on both sides

No separate shoe file is needed.

No separate jersey file is needed.

### 3.3 Shape rules that help the rig survive

To stay compatible with the current skeleton and motions:

- keep all characters adult athletic proportions
- keep silhouette readable and compact
- keep shoulders close to the torso
- avoid giant hats
- avoid giant wings
- avoid long capes
- avoid long skirts
- avoid floor-length coats
- avoid oversized armor
- avoid very long hair hanging below the chest
- avoid props attached to hands
- avoid a basketball in the art

The Halloween theme should come from color, texture, symbols, trims, masks, face design, hair, glowing accents, and costume patterning, not from big loose accessories.

## 4. Delivery Spec For Each Character

Create one folder per character.

Recommended folder naming:

```text
Character_01_PumpkinCaptain
Character_02_VampireShooter
Character_03_PhantomGuard
Character_04_ReaperDunker
```

Expected files:

```text
full_preview.png
parts_sheet.png
head.png
body.png
body_alt.png
hand_left.png
hand_right.png
leg.png
notes.txt
```

`body_alt.png` is optional but recommended.

`notes.txt` can contain:

- character name
- color palette
- what changed from previous version

### 4.1 Color space and format

Use:

- PNG
- sRGB
- lossless export
- no JPG
- no watermark
- no text
- no signature

DPI does not matter here. Pixel dimensions matter.

### 4.2 Background rule

Best case:

- transparent background

Fallback if Gemini refuses transparency:

- pure flat green background `#00FF00`
- no gradients
- no shadows on the ground
- no smoke behind the subject

### 4.3 Recommended working pixel sizes

Do not ask Gemini for the tiny final atlas size. That will produce muddy results.

Generate larger clean source parts first, then downscale during integration.

Recommended working sizes:

| File | Recommended canvas | Subject occupancy |
| --- | --- | --- |
| `full_preview.png` | `2048 x 2048` | character uses about `70%` height |
| `parts_sheet.png` | `2048 x 2048` | five parts spaced with wide margins |
| `head.png` | `640 x 640` | head uses about `65%` to `75%` height |
| `body.png` | `640 x 768` | torso uses about `65%` to `75%` height |
| `body_alt.png` | `640 x 768` | same as `body.png` |
| `hand_left.png` | `640 x 384` | arm uses about `70%` width |
| `hand_right.png` | `640 x 384` | arm uses about `70%` width |
| `leg.png` | `640 x 384` | leg uses about `70%` height |

### 4.4 Why these sizes are safe

The current DragonBones atlas is small in final use.

Existing reference part sizes inside `texture2.json` are approximately:

- head around `55 x 84` to `65 x 94`
- body around `19 x 34`
- left hand around `17 x 14`
- right hand around `16 x 14`
- leg around `26 x 13`

That means generation should happen at a larger working size, then be cleaned and scaled down later.

## 5. Visual Direction

The new characters should look:

- Halloween-themed
- premium
- polished
- stylish
- game-ready
- high-end
- athletic
- readable at small size
- dramatic but not messy

The correct mood is:

- stylish Halloween sports game
- arcade basketball hero select quality
- strong silhouette
- clean materials
- controlled glow
- rich but restrained color contrast
- cool rim light
- sharp shape language

The wrong mood is:

- horror movie gore
- cheap costume party
- muddy dark fantasy
- low-budget cartoon
- overly realistic skin pores
- painterly mess
- blurry mobile-game slop

### 5.1 Global art rules

Always keep these rules in every prompt:

- premium high-end 2D game art
- clean silhouette
- readable at small size
- adult athletic basketball player
- Halloween theme
- no gore
- no blood
- no severed limbs
- no exposed organs
- no zombie decay
- no slime dripping everywhere
- no oversized props
- no text
- no watermark
- no logo
- no crowd
- no basketball in hand
- no floor shadow for isolated parts
- no dramatic camera perspective
- no fisheye distortion
- no extreme foreshortening

## 6. Recommended Four Character Concepts

These four are chosen because they are visually different, easy to read, and still safe for the current rig.

### 6.1 Character 01: Pumpkin Captain

Theme:

- premium jack-o-lantern athlete
- orange and matte black base
- antique gold detail
- subtle teal moonlight rim light

Key design language:

- confident team captain
- handsome heroic face
- carved pumpkin emblem on jersey
- glowing seam details, but restrained
- no giant pumpkin head

### 6.2 Character 02: Vampire Shooter

Theme:

- elegant vampire superstar
- burgundy, black, bone white, antique gold

Key design language:

- composed and dangerous
- sleek hair
- aristocratic collar integrated into jersey shape
- refined rather than monstrous
- no giant cape

### 6.3 Character 03: Moonlit Phantom Guard

Theme:

- ghostly speed player
- midnight navy, icy cyan, silver

Key design language:

- agile
- cool spectral energy accents
- smoky edge motifs on uniform
- not fully transparent
- still solid and readable as a game character

### 6.4 Character 04: Obsidian Reaper Dunker

Theme:

- reaper-inspired power forward
- charcoal black, toxic green, bone ivory

Key design language:

- powerful
- sharp angular face paint or mask details
- skeletal motifs as premium graphic design, not gore
- no giant scythe
- no hanging robe

## 7. Generation Workflow With Gemini

Do not jump straight into separate part generation.

Use this order:

1. generate one full character concept image
2. pick the best version
3. use that approved concept as a visual reference for the parts sheet
4. generate the parts sheet
5. if any single part is weak, rerun a single-part prompt
6. optionally generate `body_alt.png`
7. send the final folder to Codex for integration

If Gemini supports image reference in the same thread, always attach the approved `full_preview.png` when generating the parts sheet and the individual repair parts.

## 8. Direct Copy Prompts For Gemini

All prompts below are written to be pasted directly into Gemini.

If Gemini supports extra settings:

- choose the highest image quality
- choose a square canvas for `full_preview.png` and `parts_sheet.png`
- choose PNG export if available

### 8.1 Character 01 Full Preview Prompt

```text
Create one single original Halloween basketball character for a Unity 2D sports game. Output one centered full-body character only, no environment, no court, no crowd, no text, no logo, no watermark, no extra objects. Style: premium high-end 2D game art, polished arcade sports hero art, clean silhouette, readable at small size, sharp shape design, controlled glow, rich but restrained contrast, elegant material rendering, modern stylized illustration, not photorealistic, not painterly messy. Character theme: Pumpkin Captain. Adult athletic male basketball player, handsome face, confident captain energy, slight three-quarter front-facing pose, standing relaxed but ready, full body visible, premium Halloween style. Color palette: burnt orange, matte black, antique gold, tiny hint of teal moonlight rim light. Costume design: premium basketball jersey and shorts with a refined jack-o-lantern crest on the chest, subtle glowing pumpkin seam lines, high-end sneakers, clean sport trims, premium sporty materials, small tasteful Halloween accents. Head design: normal human head, not a pumpkin head, stylish hair, strong eyebrows, subtle Halloween makeup or glow details around the eyes, attractive and readable face. Keep the silhouette compact and animation-friendly. No cape, no giant hat, no wings, no robe, no floating props, no scythe, no basketball, no smoke cloud behind the body. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00 with no texture and no shadow. Use a 2048x2048 high-resolution square composition, character occupying about 70 percent of canvas height, centered with clean margins on all sides, sharp edges, clean alpha-like separation, sRGB, crisp details, high clarity.
```

### 8.2 Character 02 Full Preview Prompt

```text
Create one single original Halloween basketball character for a Unity 2D sports game. Output one centered full-body character only, no environment, no court, no crowd, no text, no logo, no watermark, no extra objects. Style: premium high-end 2D game art, polished arcade sports hero art, clean silhouette, readable at small size, sharp shape language, stylish controlled lighting, elegant and expensive look, modern stylized illustration, not photorealistic, not messy painting. Character theme: Vampire Shooter. Adult athletic male basketball player, elegant superstar energy, calm confident expression, slight three-quarter front-facing pose, full body visible. Color palette: deep burgundy, black, bone white, antique gold, tiny crimson glow accents. Costume design: luxurious basketball jersey and shorts with a refined vampire noble aesthetic, sharp collar shape integrated into the jersey without becoming a long cape, subtle bat-wing line motifs in the trim, polished sneakers, premium sports fabrics, tasteful Halloween detailing. Head design: handsome pale face, stylish swept-back hair, subtle vampire influence, sharp eyes, no gore, no blood on mouth, no giant fangs, no horror exaggeration. Keep the silhouette compact and animation-friendly. No cape, no wings, no throne, no goblet, no basketball, no giant jewelry, no dramatic flying pose. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00 with no texture and no shadow. Use a 2048x2048 high-resolution square composition, character occupying about 70 percent of canvas height, centered with clean margins, crisp edges, sRGB, clean output, premium quality.
```

### 8.3 Character 03 Full Preview Prompt

```text
Create one single original Halloween basketball character for a Unity 2D sports game. Output one centered full-body character only, no environment, no court, no crowd, no text, no logo, no watermark, no extra objects. Style: premium high-end 2D game art, polished arcade sports hero art, clean silhouette, readable at small size, modern stylized sports illustration, sharp forms, elegant glow control, premium character concept art for a game, not photorealistic, not messy, not low-detail cartoon. Character theme: Moonlit Phantom Guard. Adult athletic basketball player, fast agile guard energy, slight three-quarter front-facing pose, full body visible. Color palette: midnight navy, icy cyan, silver, tiny cool white highlights. Costume design: premium basketball jersey and shorts with subtle ghostly vapor line motifs along the seams, sleek sporty silhouette, refined spectral accents, premium sneakers, high-end materials, clean and readable shapes. Head design: attractive human face with subtle ghost influence, cool eyes, controlled spectral makeup, stylish hair, no fully transparent body, no missing limbs, no horror decay. The character must still feel solid and game-readable, not like a blurry smoke cloud. Keep silhouette compact and rig-friendly. No robe, no sheet ghost costume, no floating chains, no basketball, no giant aura cloud. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00 with no texture and no shadow. Use a 2048x2048 high-resolution square composition, centered character, about 70 percent canvas height, crisp edges, sRGB, premium clarity.
```

### 8.4 Character 04 Full Preview Prompt

```text
Create one single original Halloween basketball character for a Unity 2D sports game. Output one centered full-body character only, no environment, no court, no crowd, no text, no logo, no watermark, no extra objects. Style: premium high-end 2D game art, polished arcade sports hero art, strong silhouette, readable at small size, high-end stylized sports concept art, clean graphic forms, premium material rendering, controlled contrast, not photorealistic, not painterly muddy. Character theme: Obsidian Reaper Dunker. Adult athletic powerful basketball player, dominant forward or dunker energy, slight three-quarter front-facing pose, full body visible. Color palette: charcoal black, toxic green, bone ivory, small metallic accents. Costume design: premium basketball jersey and shorts with sharp reaper-inspired graphic motifs, elegant skeletal linework used like luxury sports branding, premium sneakers, compact and powerful silhouette. Head design: human face with refined reaper styling, subtle mask or face-paint feel, intense eyes, no gore, no skull exposed flesh, no dripping blood. Keep the design premium, stylish, athletic, and clean. No scythe, no hood covering the whole face, no cape, no giant cloth strips, no giant shoulder armor, no basketball. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00 with no texture and no shadow. Use a 2048x2048 high-resolution square composition, character occupying about 70 percent of canvas height, centered, crisp edges, sRGB, premium clarity.
```

## 9. Parts Sheet Prompts

Important: attach the approved `full_preview.png` of the same character as image reference if Gemini supports it.

The parts sheet should contain exactly five isolated parts:

- one head
- one torso/body
- one left arm piece
- one right arm piece
- one single leg piece

Do not ask Gemini for an assembled character in the parts sheet step.

### 9.1 Character 01 Parts Sheet Prompt

```text
Using the approved Pumpkin Captain character design as the exact visual reference, create one clean 2D game character parts sheet for animation integration. Output one square 2048x2048 image with exactly five isolated parts only, evenly spaced with large clean gaps between them, no assembled full character, no extra duplicates, no text labels, no shadows, no environment. Parts required: 1 head, 1 body torso, 1 left arm piece, 1 right arm piece, 1 single leg piece. Style must match the approved Pumpkin Captain exactly: same face, same hair, same costume language, same colors, same premium polished high-end Halloween basketball look. Part rules: the head must include face, hair, ears, small neck base, and any tight head accessory, but no shoulders. The body must include torso, jersey, shoulders, chest, waist, upper shorts connection area, but no head and no arms. The left arm piece must be one isolated arm from shoulder seam to fingers, with sleeve integrated, suitable for rotation around the shoulder. The right arm piece must be one isolated mirrored-orientation arm from shoulder seam to fingers, with sleeve integrated, suitable for rotation around the shoulder. The leg piece must be one isolated single leg from upper thigh to shoe, designed to be reused by the rig, with sporty readable shape and no pelvis section. Keep all parts compact, centered in their own regions, flat game-friendly lighting, no dramatic perspective, no foreshortening, no floating smoke, no basketball. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00. High clarity, crisp edges, sRGB, clean export, premium game asset source art.
```

### 9.2 Character 02 Parts Sheet Prompt

```text
Using the approved Vampire Shooter character design as the exact visual reference, create one clean 2D game character parts sheet for animation integration. Output one square 2048x2048 image with exactly five isolated parts only, evenly spaced with large clean gaps between them, no assembled full character, no extra duplicates, no text labels, no shadows, no environment. Parts required: 1 head, 1 body torso, 1 left arm piece, 1 right arm piece, 1 single leg piece. Style must match the approved Vampire Shooter exactly: same face, same hairstyle, same costume, same burgundy black gold palette, same premium elegant Halloween sports look. The head must include face, hair, ears, small neck base, and any tight head accessory, but no shoulders. The body must include torso, jersey, shoulders, chest, waist, upper shorts connection area, but no head and no arms. The left arm piece must be one isolated left arm from shoulder seam to fingers, sleeve included. The right arm piece must be one isolated right arm from shoulder seam to fingers, sleeve included. The leg piece must be one isolated single leg from upper thigh to shoe, clean athletic silhouette, no pelvis. Keep the design compact and rig-friendly, no dramatic perspective, no long cloth, no cape, no bats, no goblets, no basketball, no fog cloud. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00. High-resolution, sRGB, crisp edges, clean output, premium game source art.
```

### 9.3 Character 03 Parts Sheet Prompt

```text
Using the approved Moonlit Phantom Guard character design as the exact visual reference, create one clean 2D game character parts sheet for animation integration. Output one square 2048x2048 image with exactly five isolated parts only, evenly spaced with large clean gaps between them, no assembled full character, no extra duplicates, no text labels, no shadows, no environment. Parts required: 1 head, 1 body torso, 1 left arm piece, 1 right arm piece, 1 single leg piece. Style must match the approved Moonlit Phantom Guard exactly: same face, same hair, same spectral sports costume, same midnight navy icy cyan silver palette, same premium polished game-art finish. The head must include face, hair, ears, small neck base, and any tight head detail, but no shoulders. The body must include torso, jersey, shoulders, chest, waist, upper shorts connection area, but no head and no arms. The left arm piece must be one isolated left arm from shoulder seam to fingers, sleeve included. The right arm piece must be one isolated right arm from shoulder seam to fingers, sleeve included. The leg piece must be one isolated single leg from upper thigh to shoe, clean athletic readable shape, no pelvis. Keep all parts solid and readable, not transparent smoke blobs, no dramatic perspective, no ghost tail, no floating mist, no basketball. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00. High-resolution, sRGB, crisp clean edges, premium source art for a stylized Unity sports game.
```

### 9.4 Character 04 Parts Sheet Prompt

```text
Using the approved Obsidian Reaper Dunker character design as the exact visual reference, create one clean 2D game character parts sheet for animation integration. Output one square 2048x2048 image with exactly five isolated parts only, evenly spaced with large clean gaps between them, no assembled full character, no extra duplicates, no text labels, no shadows, no environment. Parts required: 1 head, 1 body torso, 1 left arm piece, 1 right arm piece, 1 single leg piece. Style must match the approved Obsidian Reaper Dunker exactly: same face, same hairstyle or mask detail, same costume, same charcoal toxic green bone-ivory palette, same premium high-end Halloween basketball look. The head must include face, hair, ears, small neck base, and any tight head accessory, but no shoulders. The body must include torso, jersey, shoulders, chest, waist, upper shorts connection area, but no head and no arms. The left arm piece must be one isolated left arm from shoulder seam to fingers, sleeve included. The right arm piece must be one isolated right arm from shoulder seam to fingers, sleeve included. The leg piece must be one isolated single leg from upper thigh to shoe, powerful athletic silhouette, no pelvis. Keep everything clean, compact, animation-friendly, and premium. No scythe, no giant hood, no robe strips, no gore, no basketball, no dramatic perspective. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00. High-resolution, sRGB, crisp edges, clean export, premium game asset source art.
```

## 10. Optional Alternate Body Prompt

Use this only after the base body is approved.

Attach the approved `body.png` and `full_preview.png` of the same character as reference if Gemini supports it.

```text
Create one alternate torso body part for the exact same approved Halloween basketball character. Output only one isolated body part, no head, no arms, no legs, no environment, no text, no watermark. Keep exactly the same character identity, same silhouette, same costume construction, same shoulder width, same waist shape, same emblem placement, same premium 2D game art style. Change only the jersey colorway into an alternate away uniform version while preserving the same Halloween theme and same premium look. The body part must include torso, jersey, shoulders, chest, waist, and upper shorts connection area only. No dramatic perspective, no painterly texture mess, no floating cloth. Background must be transparent PNG. If transparent is not possible, use a perfectly flat pure green background #00FF00. Use a 640x768 high-resolution canvas, centered part, crisp edges, sRGB, clean export.
```

## 11. Emergency Single-Part Repair Prompts

If the parts sheet is good overall but one part is bad, use the matching repair prompt below and attach the approved `full_preview.png` as reference.

### 11.1 Head Repair Prompt

```text
Create one isolated head part only for the exact approved Halloween basketball character. Keep the same face identity, same hairstyle, same expression family, same premium stylized 2D game art style, same color palette. Include face, hair, ears, and a small neck base only. No shoulders, no torso, no arms, no background elements, no text, no watermark. Keep the head centered on a 640x640 canvas, readable at small size, crisp edges, no dramatic perspective, no extreme side view, no painterly mess. Background must be transparent PNG, or pure green #00FF00 if transparency is not possible.
```

### 11.2 Body Repair Prompt

```text
Create one isolated torso body part only for the exact approved Halloween basketball character. Keep the same costume language, same jersey materials, same silhouette, same emblem placement, same premium stylized 2D game art style. Include shoulders, torso, chest, waist, and upper shorts connection area only. No head, no arms, no legs, no background elements, no text, no watermark. Keep the part centered on a 640x768 canvas, crisp edges, readable at small size, flat game-friendly lighting, no dramatic perspective. Background must be transparent PNG, or pure green #00FF00 if transparency is not possible.
```

### 11.3 Left Arm Repair Prompt

```text
Create one isolated left arm part only for the exact approved Halloween basketball character. Keep the same costume, same sleeve shape, same glove or wrist detail if present, same premium stylized 2D game art style. Include one full left arm from shoulder seam to fingers only. No torso, no head, no right arm, no background elements, no text, no watermark. Keep the part centered on a 640x384 canvas, clean silhouette, crisp edges, no dramatic perspective, no basketball in hand. Background must be transparent PNG, or pure green #00FF00 if transparency is not possible.
```

### 11.4 Right Arm Repair Prompt

```text
Create one isolated right arm part only for the exact approved Halloween basketball character. Keep the same costume, same sleeve shape, same glove or wrist detail if present, same premium stylized 2D game art style. Include one full right arm from shoulder seam to fingers only. No torso, no head, no left arm, no background elements, no text, no watermark. Keep the part centered on a 640x384 canvas, clean silhouette, crisp edges, no dramatic perspective, no basketball in hand. Background must be transparent PNG, or pure green #00FF00 if transparency is not possible.
```

### 11.5 Leg Repair Prompt

```text
Create one isolated single leg part only for the exact approved Halloween basketball character. Keep the same costume style, same shorts trim continuation, same sock and sneaker design language, same premium stylized 2D game art style. Include one single leg from upper thigh to shoe only. No pelvis, no torso, no second leg, no background elements, no text, no watermark. Keep the part centered on a 640x384 canvas, compact athletic silhouette, crisp edges, readable at small size, no dramatic perspective. Background must be transparent PNG, or pure green #00FF00 if transparency is not possible.
```

## 12. Common Correction Prompts

If Gemini drifts away from the target, use one of these short correction prompts:

### 12.1 Too messy

```text
Make the design cleaner, flatter, more readable, more premium, and more suitable for a polished 2D game character. Reduce visual clutter, reduce noise, reduce loose accessories, keep the silhouette compact.
```

### 12.2 Too scary

```text
Keep the Halloween theme, but remove horror gore and remove monster exaggeration. Make it stylish, athletic, handsome, premium, and game-friendly instead of frightening.
```

### 12.3 Too realistic

```text
Push this toward stylized premium 2D game art, not photorealism. Cleaner shapes, simpler materials, stronger silhouette, less skin texture, less realism, more game-readability.
```

### 12.4 Too much perspective

```text
Reduce perspective distortion and foreshortening. Make the part look flatter, cleaner, and easier to use as a rigged 2D animation piece.
```

### 12.5 Character identity drift

```text
Match the approved reference exactly in face identity, hairstyle, costume language, and color palette. Do not redesign the character. Only refine the existing approved design.
```

## 13. Quality Checklist Before Sending Assets To Codex

Check every file at 100 percent zoom:

- clean edge
- no background leftovers
- no green spill if chroma fallback was used
- no watermark
- no text
- no accidental prop
- no basketball
- no floor shadow
- no missing fingers
- no broken ankle shape
- no mismatched color compared with the preview
- no part touching the canvas edge

Check the set as a whole:

- all parts clearly belong to the same character
- same face, same hair, same jersey details
- same palette across all parts
- same rendering style across all parts
- body width and head size feel consistent
- left arm and right arm feel like the same costume
- leg matches the same shoe and sock design

## 14. What To Hand Back To Codex

When the generation stage is done, send:

- the character folder or folders
- tell Codex which version of each character is approved
- tell Codex whether `body_alt.png` exists
- tell Codex if any part still needs cleanup

Then Codex can do the next integration pass:

- reduce the project to 4 characters
- map the 4 new skins into the current runtime
- replace the current character atlas content
- tune offsets, scale, and slot fit
- verify `idle`, `run`, `jump`, `throw_land`, `steal`, `stun`, `block`, `pump`, `dunk`, and super moves

## 15. Practical Notes For Future Conversations

These points should not be rediscovered again:

- the project uses DragonBones-style armature skin swapping, not a normal Unity-only animator pipeline
- the best replacement strategy is to keep the rig and replace skin parts only
- the art split needed by the project is `head`, `body`, `hand_left`, `hand_right`, `leg`
- the current playable match scope is effectively 1 player per side, so 4 characters total is a practical next target
- this document supersedes the older garbled `DOCS/HALLOWEEN_ASSET_PROMPTS.md` for the character-replacement workflow
