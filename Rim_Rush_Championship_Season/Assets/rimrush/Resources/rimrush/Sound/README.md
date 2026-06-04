# Sound 目录说明

本目录包含游戏中使用的所有音频文件（18 个 WAV 音效 + 1 个 OGG 背景音乐）。

所有音效通过 `rimrushAudio.cs` 单例管理器播放，音效键常量定义在 `rimrushAssets.Sounds` 中。

---

## 背景音乐

| 文件 | 键名 | 用途 | 触发位置 |
|------|------|------|----------|
| bgm.ogg | MenuMusic | 主菜单和游戏进行中的背景音乐循环 | `rimrushGameBootstrap.cs` — 进入菜单或开始游戏时播放 |

## 比赛音效

| 文件 | 键名 | 用途 | 触发位置 |
|------|------|------|----------|
| whistle.wav | MWhistle | 裁判哨声 | `rimrushGameCore.cs` — 比赛正式开始时、暂停恢复倒计时结束时、训练/教程模式立即开赛时 |
| buzzer.wav | MBuzzer | 比赛结束蜂鸣 | `rimrushGameCore.cs` — 比赛计时器归零时 |
| countdown.wav | MCountdown | 倒计时滴答声 | `rimrushHudView.cs` — 赛前 "TIP OFF IN" 倒计时和暂停恢复倒计时的每一秒（3、2、1） |

## 玩家技能音效

| 文件 | 键名 | 用途 | 触发位置 |
|------|------|------|----------|
| teleport.wav | PTeleport | 传送音效 | `rimrushGameplayObjects.cs` — 空接超级投篮时玩家传送离开和重新出现在篮筐位置 |
| swoosh.wav | PSwoosh | 空气挥动声 | `rimrushGameplayObjects.cs` — 投篮出手时、抢断动作发起时、普通扣篮和补篮扣篮起跳时 |
| energy.wav | PEnergy | 超级能量条充满 | `rimrushGameplayObjects.cs` — 玩家超级能量条通过充能或自然积累达到满值时 |
| stunned.wav | PStunned | 眩晕/冰冻效果 | `rimrushGameplayObjects.cs` — 被冰冻类技能冻结时、被成功抢断后眩晕时 |
| mega_dunk.wav | PMegaStart | 超级扣篮启动 | `rimrushGameplayObjects.cs` — 玩家进入超级扣篮飞行阶段时 |
| shield.wav | PShield | 护盾激活 | `rimrushGameplayObjects.cs` — 护盾在场上生成并显示时 |
| dash.wav | PDash | 普通冲刺 | `rimrushGameplayObjects.cs` — 玩家在地面发动冲刺时 |
| super_dash.wav | PSuperDash | 超级冲刺 | `rimrushGameplayObjects.cs` — 玩家发动超级冲刺技能时 |

## 篮球物理音效

| 文件 | 键名 | 用途 | 触发位置 |
|------|------|------|----------|
| clash.wav | BSteel | 碰撞/对抗声 | `rimrushGameCore.cs` — 成功抢断时、捡起地上 loose ball 时；`rimrushGameplayObjects.cs` — 盖帽成功时、球碰到护盾弹开时 |
| rim_hit.wav | BRing | 篮筐碰撞 | `rimrushGameplayObjects.cs` — 球砸到篮筐时（音量随球速缩放） |
| ball_bounce.wav | BBounce | 篮球弹跳 | `rimrushGameplayObjects.cs` — 球从地面弹起时 |
| net.wav | BNet | 球网声 | `rimrushGameplayObjects.cs` — 球穿过球网得分时 |
| brick.wav | BBrick | 打铁/扣篮失败 | `rimrushGameplayObjects.cs` — 扣篮尝试失败时 |
| basket.wav | BBasket | 篮板碰撞/得分确认 | `rimrushGameplayObjects.cs` — 球砸到篮板时；`rimrushGameCore.cs` — 得分确认时 |

## UI 音效

| 文件 | 键名 | 用途 | 触发位置 |
|------|------|------|----------|
| button.wav | Button | 按钮点击 | `rimrushHudView.cs` — HUD 按钮点击释放时；`rimrushHelpPanel.cs` — 帮助面板关闭时、面板内按钮点击时 |
