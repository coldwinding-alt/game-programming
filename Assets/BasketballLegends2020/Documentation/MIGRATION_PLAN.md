# Basketball Legends 2020 Unity Migration Plan

目标：以 `_reference_do_not_ship/basketball-legends-2020-ovo` 中的 H5/Phaser 版本为核心参考，把原版代码结构、状态流、资源 key、图集与 DragonBones 数据尽量逐模块迁移到 Unity 2022，而不是另起一套相似玩法。

## 参考源码分层

原版游戏本体位于 `basketball_legends_2020.min.js`，这是一个 webpack 模块束，虽然文件名为 min，但保留了大量类名、方法名和模块边界。网页壳、广告 SDK、站点补丁、Poki/GameDistribution 等文件只作为运行包装参考，不进入 Unity 版本。

核心资源位于：

- `assets/atlases/gameplay.png/json`：球场、球、篮筐、阴影、HUD、特效按钮等 TexturePacker 图集。
- `assets/atlases/interface.png/json`：菜单、按钮、徽章、界面背景图集。
- `assets/images/logo.png`：Logo。
- `assets/images/texture.png + assets/data/sk.json + texture.json`：菜单/选人用 DragonBones 大角色。
- `assets/images/texture2.png + assets/data/sk2.json + texture2.json`：比赛中 `playerSmall` DragonBones 角色。
- `assets/data/Players.json`：选人、赛后静态拼图布局数据。
- `assets/sound/*.ogg`：原版音效与音乐。

## 原版模块到 Unity 模块映射

| 原版模块/类 | 作用 | Unity 迁移目标 |
| --- | --- | --- |
| `Constants` | 800x480 逻辑宽高、1066x640 显示区、比赛时间、固定步长 | `BLConstants` |
| `Images / Atlases / Sounds / JSONData` | 原版资源 key 列表 | `BLAssets` |
| `ObjectsData` | 篮球、篮筐、球员、AI 区域等数值 | `BLObjectsData` |
| `Inventory / MatchData` | gameMode、队伍、球员、比分、比赛配置 | `BLInventory`, `BLMatchData` |
| `PlayersData` | DragonBones 初始化、皮肤/手/腿切换 | `BLPlayersData`, `DBLiteArmature` |
| `GameBuilder` | 构建 Arena/Basket/Ball/Player/GUI | `BLGameBuilder` |
| `MainGameCore` | 比赛状态、计时、得分、重开、球权事件 | `BLGameCore` |
| `ArenaObject` | 场地背景与边界 | `BLArenaObject` |
| `BasketObject` | 篮筐、篮板、篮网、传感器 | `BLBasketObject` |
| `BallObject` | 球体物理、投篮轨迹、进球/篮筐事件 | `BLBallObject` |
| `PlayerObject` | 球员状态机、动作、投篮、抢断、超杀 | `BLPlayerObject` |
| `PlayerController* / AIController*` | 键盘、双人、AI 输入 | `IBLPlayerController`, `BLKeyboardController`, `BLAIController` |
| `MainGameView / InfoPanel / Timer / MatchPreloader` | 显示分层与 HUD | `BLGameView`, `BLHudView` |

## Unity 实现策略

1. 保留原版像素坐标系。Phaser 中比赛世界以 `800x480` 为物理逻辑区，Unity 使用 `BLPixelRoot` 做坐标转换，尽量用原始数值驱动物体。
2. 资源 key 不重命名。`BLAtlasCache` 运行时解析 TexturePacker JSON，按 `BallMC0000`、`BasketGraphic0000` 等原 key 提供 Sprite。
3. DragonBones 先实现项目内轻量运行时。`DBLite` 解析 `sk/sk2 + texture/texture2`，支持本项目需要的骨骼、slot、displayFrame、translateFrame、rotateFrame、scaleFrame 和嵌套 armature。这样可以直接消费原版 JSON，不依赖外部插件。
4. 物理先用 Unity 2D 刚体/碰撞器承接 Nape 数据。数值直接来自 `ObjectsData`，投篮速度公式直接迁移 `BallObject.calcVel/calcThrowVel/calcDispersion`。
5. UI 先以原版图集和文本复刻主流程：快速开始、1P/2P、训练、比赛 HUD。后续再补齐锦标赛、完整选人、赛后面板与成就统计。

## 当前执行顺序

1. 建 Unity 2022 工程骨架。
2. 复制核心资源到 `Assets/BasketballLegends2020/Resources/BL2020`。
3. 实现 JSON/图集/音频/DragonBones 轻量加载层。
4. 迁移 MatchData、GameBuilder、核心对象与一条可运行 quick match。
5. 运行 Unity batchmode 编译，记录问题。

## 质量检查点

- Unity 2022.3 可直接打开工程。
- 场景启动后不依赖 `_reference_do_not_ship` 运行。
- 资源 key 与原版一致，减少后续对照成本。
- 迁移文档同步记录完成度与偏差。
