# DBLiteRuntime API Reference

## Public API Surface

对外接口仅 `DBLiteFactory`（3 个方法）和 `DBLiteArmature`（6 个方法 + 2 个事件 + 1 个属性）。

其余类（`DBLiteSkeleton`、`DBLiteArmatureData`、`DBLiteBoneTrack`、`DBLiteSlotInstance` 等）均为内部实现，外部不直接调用。

---

## DBLiteFactory

### `EnsureLoaded()`

```csharp
DBLiteFactory.Instance.EnsureLoaded();
```

从 `Resources/mlp/DragonBones/` 加载 `sk2.json`、`texture2.json`、`texture2.png`。
通常无需手动调用——`BuildArmature()` 内部自动调用。

### `BuildArmature(armatureName, objectName)`

```csharp
var armature = DBLiteFactory.Instance.BuildArmature("playerSmall", "Player_0");
// 返回 DBLiteArmature
```

| 参数 | 说明 |
|------|------|
| `armatureName` | 骨架名，当前项目固定为 `"playerSmall"` |
| `objectName` | 生成的 GameObject 名称 |

### `GetTextureSprite(spriteName, textureAtlasKey)`

```csharp
var sprite = DBLiteFactory.Instance.GetTextureSprite("head1", "texture2");
```

---

## DBLiteArmature

### 任务 → 接口速查

| 任务 | 接口 |
|------|------|
| 播放/切换动画 | `Play(name)` |
| 停在动画首帧 | `StopAtStart(name)` |
| 换装（切换角色外观） | `GetChildArmature(name).Play(skin)` |
| 扣篮时隐藏手中的球 | `SetSlotHidden("ball", true)` |
| 刷新渲染 | `RefreshPose()` |
| 动画播完后执行逻辑 | `AnimationComplete += handler` |
| 关键帧触发游戏逻辑 | `FrameEvent += handler` |
| 加速/减速播放 | `PlaybackSpeed = 1.5f` |

---

### `Play(string animationName, bool restart = true)`

切换并播放动画。

```csharp
armature.Play("idle");
armature.Play("jump_wb");
armature.Play("dash");
armature.Play("dunk1");
armature.Play("run_wb", restart: false);  // 同动画不重置进度
```

项目内通过 `mlpPlayerObject.PlayState()` 封装调用（`mlpGameplayObjects.cs:5416`）：

```csharp
private void PlayState(string state)
{
    SetAnimationPlaybackSpeed(state);
    if (visualState == state) return;
    visualState = state;
    armature?.Play(state);
}
```

#### 动画名称全集

| 动画 | 有球版本 | 触发条件 |
|------|---------|---------|
| `idle` | `idle_wb` | 静止站立 |
| `run` | `run_wb` | 水平移动 |
| `jump` | `jump_wb` | 起跳 |
| `landing` | `landing_wb` | 落地 |
| — | `fly1` ~ `fly5` / `fly1_wb` ~ `fly5_wb` | 空中 |
| `dash` | — | 冲刺 |
| `steal` | — | 抢断 |
| `throw_land` | — | 投篮出手 |
| `pumpStart` / `pumpEnd` | — | 假动作 |
| `blockStart` / `blockEnd` | — | 盖帽 |
| `stun` | — | 被眩晕 |
| `dunk1` | — | 扣篮 |
| `megadunk` / `megadunk_end` | — | 超级扣篮 |
| `md_start` / `md_mid` / `md_end` | — | 超级冲刺 |

`_wb` = With Ball

---

### `StopAtStart(string animationName)`

加载动画并冻结在第 0 帧。

```csharp
armature.StopAtStart("idle");
```

---

### `GetChildArmature(string slotName)`

获取子骨架引用，用于换装。

```
playerSmall
  ├── head
  ├── body
  ├── left hand
  ├── right hand
  ├── dighand
  ├── left leg
  ├── right leg
  ├── digleg
  ├── ball          (普通插槽)
  └── ball_front    (普通插槽)
```

```csharp
var headArmature = armature.GetChildArmature("head");
headArmature?.Play("head3");                      // 切换为角色3的头部

var bodyArmature = armature.GetChildArmature("body");
bodyArmature?.Play("body1");                      // 切换为体型1的身体
```

完整换装流程见 `mlpPlayersData.SwitchPlayer()` (`mlpPlayersData.cs:356`)：

```csharp
armature.GetChildArmature("head")?.Play("head" + (skinId + 1));
armature.GetChildArmature("body")?.Play("body" + (formId + 1));
armature.GetChildArmature("left hand")?.Play("hand" + hand);
armature.GetChildArmature("right hand")?.Play("hand" + hand);
armature.GetChildArmature("dighand")?.Play("hand" + hand);
armature.GetChildArmature("left leg")?.Play(leg);
armature.GetChildArmature("right leg")?.Play(leg);
armature.GetChildArmature("digleg")?.Play(leg);
armature.Play("idle");
```

---

### `SetSlotHidden(string slotName, bool hidden)`

```csharp
// 扣篮时隐藏手中的球
armature.SetSlotHidden("ball", true);
armature.SetSlotHidden("ball_front", true);

// 恢复
armature.SetSlotHidden("ball", false);
armature.SetSlotHidden("ball_front", false);
```

---

### `RefreshPose()`

```csharp
armature.RefreshPose();
```

---

### `AnimationComplete` (event)

非循环动画播放完毕时触发。

```csharp
armature.AnimationComplete += animName =>
{
    if (animName == "megadunk")      PlayState("megadunk_end");
    if (animName == "steal")         ResolveStealAttempt();
    if (animName == "megadunk_end")  PlayState("idle");
};
```

---

### `FrameEvent` (event)

动画到达标记帧时触发。事件点由 sk2.json 定义。

```csharp
armature.FrameEvent += (animName, eventName) =>
{
    if (eventName == "releaseBall")  GameCore.Ball.Release();
    if (eventName == "footstep")     mlpAudio.Instance.Play(footstepSfx);
};
```

sk2.json 帧事件格式：

```json
{ "name": "dunk1", "frame": [
    { "duration": 10 },
    { "duration": 5, "event": "releaseBall" },
    { "duration": 10 }
]}
```

表示第 15 帧触发 `releaseBall`。

---

### `PlaybackSpeed` (property)

```csharp
armature.PlaybackSpeed = 1.2f;   // 1.2x
armature.PlaybackSpeed = 0.8f;   // 0.8x
armature.PlaybackSpeed = 1f;     // 正常
```

---

## 构造与初始化

```csharp
// mlpGameplayObjects.cs:3344 — PlayerObject 构造函数中
var armature = mlpPlayersData.BuildGameplayArmature($"playerSmall_{team}_{playerNo}");
armature.transform.SetParent(graphic.transform, false);
armature.transform.localPosition = new Vector3(0f, -35f, 0f);
armature.transform.localScale = Vector3.one * mlpConstants.PixelPerfectCharacterScale;

// 换装
mlpPlayersData.ApplyCharacter(armature, characterId);

// 订阅事件
armature.AnimationComplete += OnAnimationComplete;
armature.FrameEvent += OnAnimationFrameEvent;
```

---

## 调用链

```
PlayState("jump_wb")          → armature.Play("jump_wb")
PlayState("dash")             → armature.Play("dash")
SetAnimationPlaybackSpeed()   → armature.PlaybackSpeed = ...

ApplyCharacter():
  SwitchPlayer():
    GetChildArmature("head")?.Play("head3")
    GetChildArmature("body")?.Play("body1")
    GetChildArmature("left hand")?.Play("hand2")
    armature.Play("idle")

SetDunkBallSlotsHidden()      → armature.SetSlotHidden("ball", ...)
                              → armature.SetSlotHidden("ball_front", ...)
```
