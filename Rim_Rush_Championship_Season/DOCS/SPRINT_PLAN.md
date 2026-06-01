# 开发看板

更新时间：2026-06-01

## 对齐来源

- 本文件现在按 GitHub Project 实际看板同步整理
- GitHub Project：`Halloween Arcade Basketball - Development Board`
- Project 地址：`https://github.com/users/coldwinding-alt/projects/1`

## 看板列说明

- `Backlog`：已经记录，但还没有准备现在开始
- `Ready`：范围清楚，可以开始做
- `In Progress`：正在做
- `Testing`：功能已做，正在验证
- `Blocked`：被卡住，等外部条件
- `Done`：已经完成并验证

## 看板规则

- 每张卡尽量控制在 `0.5-2` 天
- 优先从 `Ready` 拉任务，不直接从 `Backlog` 开做
- `In Progress` 同时不超过 `3` 张
- `Testing` 同时不超过 `2` 张
- 如果测试发现新问题，拆成新的跟进卡片

## 当前 GitHub Project 状态

### Backlog

- `#8` First 8-character balance pass
- `#9` Character visual polish
- `#11` Audio feedback polish
- `#12` Prepare submission pack
- `#14` Training tips
- `#17` Menu text cleanup
- `#19` Review asset provenance
- `#20` Ball theme check
- `#25` Drive player visual containers, animation mounts, and spawn anchors from prefab or scene references

### Ready

- `#13` Change tournament to level mode
- `#22` Add editor preview for runtime UI
- `#23` Preview main UI before Play
- `#34` Plan new game modes

### In Progress

- `#31` Cover all gameplay and features in the tutorial
- `#32` Turn the help page into a first-screen tutorial level
- `#33` Guide players from tutorial completion into the main game
- `#36` Make tutorial the main first-screen entry

### Testing

- 当前没有卡片

### Blocked

- 当前没有卡片

### Done

- `#2` Establish Unity project baseline and repository workflow
- `#3` Ship a playable 1v1 match loop, controls, scoring, and AI baseline
- `#4` Polish possession flow, score feedback, and key on-court responses
- `#5` Integrate runtime content loading and the DragonBones character pipeline
- `#6` Run a full regression pass across the 8-character build
- `#7` Restructure menu flow, match setup, and HUD around character-based play
- `#15` Verify roster asset integration across selection, match flow, and tournament draws
- `#35` Build help page

## 现在最值得讲给老师听的主线

- 现在正在做的重点是 `#31`、`#32`、`#33`、`#36`
- 也就是：
- 把原来藏在 `help` 里的内容改成首屏教程入口
- 把帮助页升级成真正的教程关卡流程
- 让教程覆盖主要玩法和主要特性
- 让玩家完成教程后知道下一步去哪里正式开始游戏

## 说明

- 如果 GitHub Project 看板后面有移动卡片，本文件也应该跟着同步
- 这份文件现在不再自己定义一套新卡片，而是直接跟 GitHub Project 对齐
