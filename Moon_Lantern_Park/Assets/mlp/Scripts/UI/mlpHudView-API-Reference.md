# mlpHudView API

`mlpHudView` 是比赛 HUD 的视图层，所有 UI 元素运行时创建。GameCore 持有其实例并通过以下接口驱动。

---

## 数据更新

### `UpdateScore(int left, int right)`
刷新比分板和暂停画面上的分数。

```csharp
hud.UpdateScore(MatchData.MatchScore[0], MatchData.MatchScore[1]);
```

### `UpdateTimer(float secondsLeft)`
刷新计时器文字，同步更新暂停画面的冻结时间。

```csharp
hud.UpdateTimer(endTime - matchTime);
```

### `SetTimerVisible(bool visible)`
显示或隐藏比赛计时器。训练/教程模式下隐藏。

---

## 比赛事件消息

### `ShowMessage(string message, float duration = 1.2f, bool showBackdrop = true)`
屏幕中央弹出大字消息（"BASKET"、"3 POINT"、"GO!!!" 等）。消息内容决定文字颜色。

```csharp
hud.ShowMessage("BASKET", 1.2f);
hud.ShowMessage("3 POINT", 1.2f, false);  // 不显示背景
```

### `ShowBonusNotice(string message, float duration = 0.9f)`
右上角小字加分提示（"HELL DASH!" 等）。带缩放弹入动画。

### `HideMessage()` / `HideBonusNotice()`
隐藏对应消息。

---

## 暂停

### `ConsumePauseCommand()`
读取并清除 HUD 按钮产生的暂停命令，返回 `mlpPauseCommand` 枚举值：
- `None` — 无操作
- `Toggle` — 切换暂停
- `Resume` — 恢复比赛
- `Menu` — 返回主菜单

```csharp
// GameCore 每帧轮询
HandlePauseCommand(hud.ConsumePauseCommand());
```

### `ShowPauseOverlay()` / `HidePauseOverlay()`
显示/隐藏暂停遮罩面板。

### `BeginResumeCountdown(float duration)` / `EndResumeCountdown()`
隐藏暂停画面并启动"RESUMING IN"倒计时 / 倒计时结束后恢复右上角按钮。

---

## 倒计时

### `StartCountdown(float duration, string caption = "")`
启动倒计时，数字上方显示可选标题。

```csharp
hud.StartCountdown(3f, "TIP OFF IN");    // 赛前
hud.StartCountdown(3f, "OVERTIME IN");   // 加时
```

### `UpdateCountdown(float dt)`
每帧驱动倒计时，返回 `true` 表示仍在进行，`false` 表示结束。

```csharp
preMatchCountdown = hud.UpdateCountdown(dt);
if (!preMatchCountdown) { /* 倒计时结束，吹哨开球 */ }
```

### `HideCountdown()`
隐藏并重置倒计时状态。

---

## 赛后结算

### `ShowPostMatch(int winner, int leftScore, int rightScore)`
显示赛后结果卡片。`winner < 0` 为左侧胜，`> 0` 为右侧胜。玩家模式自动显示 "VICTORY"/"DEFEAT"，观战模式显示 "PLAYER 1 WINS"。

```csharp
hud.ShowPostMatch(postMatchWinner, MatchData.MatchScore[0], MatchData.MatchScore[1]);
```

### `HidePostMatch()`
隐藏赛后卡片，恢复比分板。

---

## 每帧驱动

### `Update(float dt)`
HUD 主更新循环：按钮悬停检测、消息淡出、倒计时脉冲动画、赛后卡片入场动画。

```csharp
// GameCore.Update 中每帧调用
hud.Update(dt);
```

---

## 状态查询

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsPostMatchVisible` | `bool` | 赛后结算界面是否可见 |
| `IsPauseOverlayVisible` | `bool` | 暂停画面是否可见 |
