# DBLiteRuntime API Reference

## Public API Surface

The public interface only includes `DBLiteFactory` (3 methods) and `DBLiteArmature` (6 methods + 2 events + 1 property).

All other classes (`DBLiteSkeleton`, `DBLiteArmatureData`, `DBLiteBoneTrack`, `DBLiteSlotInstance`, etc.) are internal implementation details and are not called directly from outside.

---

## DBLiteFactory

### `EnsureLoaded()`

```csharp
DBLiteFactory.Instance.EnsureLoaded();
```

Loads `sk2.json`, `texture2.json`, and `texture2.png` from `Resources/mlp/DragonBones/`.
This is usually not needed manually - `BuildArmature()` calls it internally.

### `BuildArmature(armatureName, objectName)`

```csharp
var armature = DBLiteFactory.Instance.BuildArmature("playerSmall", "Player_0");
// Returns DBLiteArmature
```

| Parameter | Description |
|------|------|
| `armatureName` | Skeleton name. This project is fixed to `"playerSmall"` |
| `objectName` | Name of the generated GameObject |

### `GetTextureSprite(spriteName, textureAtlasKey)`

```csharp
var sprite = DBLiteFactory.Instance.GetTextureSprite("head1", "texture2");
```

---

## DBLiteArmature

### Task -> Interface Quick Reference

| Task | Interface |
|------|------|
| Play / switch animation | `Play(name)` |
| Stop on the first frame of an animation | `StopAtStart(name)` |
| Change outfit (switch character appearance) | `GetChildArmature(name).Play(skin)` |
| Hide the ball in hand during a dunk | `SetSlotHidden("ball", true)` |
| Refresh rendering | `RefreshPose()` |
| Run logic after an animation finishes | `AnimationComplete += handler` |
| Trigger gameplay logic from a keyframe | `FrameEvent += handler` |
| Speed up / slow down playback | `PlaybackSpeed = 1.5f` |

---

### `Play(string animationName, bool restart = true)`

Switches and plays an animation.

```csharp
armature.Play("idle");
armature.Play("jump_wb");
armature.Play("dash");
armature.Play("dunk1");
armature.Play("run_wb", restart: false);  // Do not reset progress for the same animation
```

Inside the project, this is wrapped by `mlpPlayerObject.PlayState()` (`mlpGameplayObjects.cs:5416`):

```csharp
private void PlayState(string state)
{
    SetAnimationPlaybackSpeed(state);
    if (visualState == state) return;
    visualState = state;
    armature?.Play(state);
}
```

#### Full animation list

| Animation | With-ball version | Trigger condition |
|------|---------|---------|
| `idle` | `idle_wb` | Standing still |
| `run` | `run_wb` | Horizontal movement |
| `jump` | `jump_wb` | Jump takeoff |
| `landing` | `landing_wb` | Landing |
| — | `fly1` ~ `fly5` / `fly1_wb` ~ `fly5_wb` | In the air |
| `dash` | — | Dash |
| `steal` | — | Steal |
| `throw_land` | — | Shot release |
| `pumpStart` / `pumpEnd` | — | Pump fake |
| `blockStart` / `blockEnd` | — | Block |
| `stun` | — | Stunned |
| `dunk1` | — | Dunk |
| `megadunk` / `megadunk_end` | — | Super dunk |
| `md_start` / `md_mid` / `md_end` | — | Super dash |

`_wb` = With Ball

---

### `StopAtStart(string animationName)`

Loads an animation and freezes it on frame 0.

```csharp
armature.StopAtStart("idle");
```

---

### `GetChildArmature(string slotName)`

Gets a child armature reference for outfit switching.

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
  ├── ball          (normal slot)
  └── ball_front    (normal slot)
```

```csharp
var headArmature = armature.GetChildArmature("head");
headArmature?.Play("head3");                      // Switch to character 3's head

var bodyArmature = armature.GetChildArmature("body");
bodyArmature?.Play("body1");                      // Switch to body type 1
```

See `mlpPlayersData.SwitchPlayer()` (`mlpPlayersData.cs:356`) for the full outfit-switching flow:

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
// Hide the ball in hand during a dunk
armature.SetSlotHidden("ball", true);
armature.SetSlotHidden("ball_front", true);

// Restore
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

Fires when a non-looping animation finishes playing.

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

Fires when an animation reaches a marked frame. Event points are defined in `sk2.json`.

```csharp
armature.FrameEvent += (animName, eventName) =>
{
    if (eventName == "releaseBall")  GameCore.Ball.Release();
    if (eventName == "footstep")     mlpAudio.Instance.Play(footstepSfx);
};
```

`sk2.json` frame-event format:

```json
{ "name": "dunk1", "frame": [
    { "duration": 10 },
    { "duration": 5, "event": "releaseBall" },
    { "duration": 10 }
]}
```

This means frame 15 triggers `releaseBall`.

---

### `PlaybackSpeed` (property)

```csharp
armature.PlaybackSpeed = 1.2f;   // 1.2x
armature.PlaybackSpeed = 0.8f;   // 0.8x
armature.PlaybackSpeed = 1f;     // Normal
```

---

## Construction and Initialization

```csharp
// In PlayerObject constructor: mlpGameplayObjects.cs:3344
var armature = mlpPlayersData.BuildGameplayArmature($"playerSmall_{team}_{playerNo}");
armature.transform.SetParent(graphic.transform, false);
armature.transform.localPosition = new Vector3(0f, -35f, 0f);
armature.transform.localScale = Vector3.one * mlpConstants.PixelPerfectCharacterScale;

// Outfit switching
mlpPlayersData.ApplyCharacter(armature, characterId);

// Subscribe to events
armature.AnimationComplete += OnAnimationComplete;
armature.FrameEvent += OnAnimationFrameEvent;
```

---

## Call Chain

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
