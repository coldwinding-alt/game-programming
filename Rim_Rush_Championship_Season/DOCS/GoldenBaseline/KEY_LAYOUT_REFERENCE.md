# Key Layout Reference

- UI logical width: `800`
- UI logical height: `480`
- Render size: `1066 x 640`
- Pixels per unit: `100`
- Render scale: `1.333333`

## Menu Shell

| Element | X | Y | Width | Height | Extra |
| --- | ---: | ---: | ---: | ---: | --- |
| Background center | 400 | 240 | 800 | 480 | frame driven |
| Logo center | 400 | 68 | - | - | scale `0.78` x `0.68` |
| Music button | 770 | 44 | 60 | 60 | icon target `58` |
| Help button | 706 | 44 | 60 | 60 | icon target `58` |

## HUD

| Element | X | Y | Width | Height | Extra |
| --- | ---: | ---: | ---: | ---: | --- |
| Scoreboard center | 400 | 88 | 360 | - | backdrop width target |
| Timer | 400 | 110 | - | - | text |
| Pause button | 770 | 44 | 60 | 60 | icon target `58` |
| Music button | 706 | 44 | 60 | 60 | icon target `58` |
| Help button | 642 | 44 | 60 | 60 | icon target `58` |
| Countdown center | 400 | 172 | 360 | - | popup width target |
| Message popup center | 400 | 236 | 432 | - | popup width target |

## Gameplay Anchors

| Element | X | Y | Extra |
| --- | ---: | ---: | --- |
| Left basket center | 50 | 200 | radius `30` |
| Right basket center | 750 | 200 | radius `30` |
| Left neutral spawn | 370 | 385 | player restart without serve |
| Right neutral spawn | 430 | 385 | player restart without serve |
| Left serve spawn | 50 | 385 | player restart after opponent score |
| Right serve spawn | 750 | 385 | player restart after opponent score |
| Floor Y | - | 420 | player floor |
| Ball floor Y | - | 402 | ball rest height |
| Ball pickup center Y | - | 340 | carried ball baseline |
