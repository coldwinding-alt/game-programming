// 游戏物体物理参数
// 定义篮球、球员、篮筐等游戏物体的物理数值：重力、弹跳系数、移动速度、投篮力量、扣篮范围等。游戏里所有物体的运动都参考这些数值。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏物体物理参数：定义篮球、球员、篮筐等物体的物理数值——重力、弹跳系数、移动速度、投篮力量、扣篮范围等。
    /// </summary>
    public static class mlpObjectsData
    {
        // 全局重力向量（像素/秒²），Y 分量 450 表示向下加速，X 为 0 表示无水平重力
        public static readonly Vector2 Gravity = new Vector2(0f, 450f);

        // --- 篮球物理参数 ---
        public const float BallRadius = 18f;               // 篮球碰撞半径（像素），用于篮筐、篮板、护盾等碰撞检测
        public const float BallGravMass = 2f;              // 篮球重力倍率，实际重力 = Gravity.y × BallGravMass，使球下落比其他物体更快
        public const float BallBounce = -400f;             // 篮球触地反弹的垂直速度（负值=向上），数值越大弹得越高
        public const float BallUpVelocityY = -500f;        // 篮球起始/跳球时的向上初速度（负值=向上），决定开场抛球高度
        public const float BallStealVelocityXBase = 400f;  // 抢断后球飞出的基础水平速度（像素/秒），方向由抢断者朝向决定
        public const float BallStealVelocityXAdd = 200f;   // 抢断距离越远，额外叠加的水平速度，使远距离抢断球飞得更远
        public const float BallStealVelocityY = -100f;     // 抢断后球飞出的垂直速度（负值=向上），让球轻微弹起
        public const float BallIndentYCenter = 300f;       // 篮球在中场跳球时的起始 Y 坐标（像素，从屏幕顶部算起）
        public const float BallIndentYPlayer = 340f;       // 篮球在球员手中持球时的 Y 坐标偏移，控制球在手中的高度
        public const float VerticalDispersion = 0.1f;      // 投篮垂直方向最大偏移系数，出手点越高（跳投）偏差越大
        public const float Dispersion = 0.01f;             // 投篮基础随机偏移系数，叠加距离、高度、跑动等因素后决定球是否命中

        // --- 篮筐与篮板参数 ---
        public const float BasketIndent = 20f;             // 篮筐边缘距场地边界的内缩距离（像素），篮筐不紧贴墙壁
        public const float BasketRadius = 30f;             // 篮圈半径（像素），决定篮筐大小和投篮容错
        public const float BasketCenter = BasketIndent + BasketRadius;   // 左侧篮筐中心的 X 坐标（像素）
        public const float BasketCenter2 = mlpConstants.Width - BasketCenter; // 右侧篮筐中心的 X 坐标（像素），镜像对称
        public const float BasketHeight = 200f;            // 篮筐中心距屏幕顶部的高度（像素），控制篮筐在画面中的位置
        public const float BasketPartRadius = 7f;          // 篮圈碰撞圆柱的半径（像素），与 BallRadius 叠加计算球与篮圈的碰撞
        public const float GlassWidth = 12f;               // 篮板玻璃平面距场地边界的水平距离（像素），决定篮板碰撞面的 X 位置
        public const float GlassHeight = 120f;             // 篮板玻璃的垂直高度（像素），球在此范围内才会与篮板发生碰撞
        public const float GlassY = 20f - GlassHeight;     // 篮板玻璃顶部 Y 坐标偏移（相对于篮筐高度），负值表示在篮筐上方
        public const float SensorHalf = 25f;               // 得分传感器半宽（像素），传感器中心左右各 25 像素判定进球
        public const float SensorWidth = 2f * SensorHalf;  // 得分传感器总宽度（像素），球必须穿过此宽度才算有效进球路径
        public const float SensorHeight = 5f;              // 得分传感器的垂直厚度（像素），上下传感器之间的判定带高度
        public const float SensorUp = -10f;                // 上传感器相对于篮筐高度的 Y 偏移（负=上方），球先过此线再过下线才算进球
        public const float SensorDown = 15f;               // 下传感器相对于篮筐高度的 Y 偏移（正值=下方），球穿过此线确认进球

        // --- 玩家/球员参数 ---
        public const float PlayerJump = -600f;             // 球员跳跃的初始垂直速度（负值=向上），数值越大跳得越高
        public const float PlayerMove = 250f;              // 球员空手时的水平移动速度（像素/秒）
        public const float PlayerMoveWithBall = 0.85f * PlayerMove; // 持球移动速度，为空手速度的 85%，持球会略微减速
        public const float PlayerIndentX = 30f;            // 球员碰撞体距场地左右边界的最小距离（像素），防止球员走出球场
        public const float PlayerIndentY = 385f;           // 球员站立时脚部的 Y 坐标（像素），即球员在地面上的位置
        public const float PlayersHandsWidth = 30f;        // 球员手部碰撞体的宽度（像素），用于判定拾球和抢断范围
        public const float PlayersHandsHeight = 80f;       // 球员手部碰撞体的高度（像素），垂直方向的拾球/抢断判定范围
        public const float BallPickupDistanceX = PlayersHandsWidth * 0.5f + BallRadius;  // 水平方向拾球有效距离（手部半宽+球半径）
        public const float BallPickupDistanceY = PlayersHandsHeight * 0.5f + BallRadius; // 垂直方向拾球有效距离（手部半高+球半径）
        public const float StealDistance = 55f;            // 抢断触发的水平距离阈值（像素），在此范围内才能发动抢断
        public const float IndentGeneralX = 50f;           // 球员通用的水平活动边界内缩量（像素），限制球员移动范围
        public const float BlockWidth = 20f;               // 盖帽判定区域的宽度（像素），站在持球者前方此范围内可盖帽
        public const float BlockHeight = 70f;              // 盖帽判定区域的高度（像素），垂直方向的有效盖帽范围
        public const float JumpBlockWidth = 10f;           // 跳起盖帽时判定区域的宽度（像素），跳盖比站盖更窄但可盖高球
        public const float JumpBlockHeight = 70f;          // 跳起盖帽时判定区域的高度（像素）
        public const float BlockStartDuration = 3f / 30f;  // 盖帽动作起手阶段的持续时间（秒），3 帧 @ 30fps，此期间可触发盖帽
        public const float BlockEndDuration = 5f / 30f;    // 盖帽动作收招阶段的持续时间（秒），5 帧 @ 30fps，盖帽后有短暂硬直
        public const float PumpStartDuration = 4f / 30f;   // 虚晃（假投）动作起手阶段的持续时间（秒），4 帧 @ 30fps
        public const float PumpEndDuration = 4f / 30f;     // 虚晃动作收招阶段的持续时间（秒），4 帧 @ 30fps

        // --- 扣篮系统参数 ---
        public const float PaintStartX = 100f;             // 三秒区（油漆区）起始 X 坐标（像素），进入此区域才能尝试扣篮
        public const float PaintMiddleX = 200f;            // 三秒区中线 X 坐标（像素），AI 用此判断是否深入到扣篮位置
        public const float DunkZone1Y = 280f;              // 扣篮触发区域的上边界 Y 坐标（像素），球员需在此高度以下才能扣篮
        public const float DunkZone2Y = 300f;              // 扣篮触发区域的下边界 Y 坐标（像素），与 DunkZone1Y 共同定义扣篮有效区域
        public const float DunkX = 100f;                   // 扣篮动画中球员飞向篮筐的水平偏移量（像素），控制扣篮起跳距离
        public const float DunkY = 180f;                   // 扣篮动画中球员飞向篮筐的垂直偏移量（像素），控制扣篮起跳高度
        public const float DunkChanceToComplete = 0.9f;    // 扣篮成功完成的概率（90%），10% 概率被篮筐弹开（扣飞）
        // 三种扣篮动画各自的总帧数（需与 Tools/Art/rebuild_runtime_dragonbones_skeleton.py 中生成的帧数同步）
        public const float Dunk1Duration = 24f / 30f;      // 扣篮类型 1 的动画总时长（秒），24 帧 @ 30fps
        public const float Dunk2Duration = 15f / 30f;      // 扣篮类型 2 的动画总时长（秒），15 帧 @ 30fps，最快的扣篮
        public const float Dunk3Duration = 24f / 30f;      // 扣篮类型 3 的动画总时长（秒），24 帧 @ 30fps

        // 运行时手感微调：球员从起跳到飞到篮筐的实际飞行时长（比动画总时长略短，保证手感流畅）
        public const float Dunk1TravelDuration = 19f / 30f; // 扣篮类型 1 的飞行时长（秒）
        public const float Dunk2TravelDuration = 12f / 30f; // 扣篮类型 2 的飞行时长（秒）
        public const float Dunk3TravelDuration = 18f / 30f; // 扣篮类型 3 的飞行时长（秒）

        // 球从手中释放的时刻（飞行中的第几秒松手入筐），早于飞行结束保证球先到
        public const float Dunk1ReleaseTime = 18f / 30f;   // 扣篮类型 1 球释放时刻（秒）
        public const float Dunk2ReleaseTime = 9f / 30f;    // 扣篮类型 2 球释放时刻（秒）
        public const float Dunk3ReleaseTime = 14f / 30f;   // 扣篮类型 3 球释放时刻（秒）

        // 扣篮骨骼动画播放速度倍率，>1 表示加速播放使动画更紧凑有力
        public const float Dunk1AnimationSpeed = 1.16f;    // 扣篮类型 1 动画播放速度（16% 加速）
        public const float Dunk2AnimationSpeed = 1.12f;    // 扣篮类型 2 动画播放速度（12% 加速）
        public const float Dunk3AnimationSpeed = 1.16f;    // 扣篮类型 3 动画播放速度（16% 加速）

        // --- 空接与冲刺参数 ---
        public const float AlleyOopX = 160f;               // 空接接球点的水平偏移量（像素），球飞向篮筐附近的此 X 位置
        public const float AlleyOopY = 150f;               // 空接接球点的 Y 坐标（像素），球员跳到此高度完成空接
        public const float SuperDashX1 = 150f;             // 超级冲刺起始区域的左边界 X 坐标（像素），在此范围内可发动冲刺
        public const float SuperDashX2 = 650f;             // 超级冲刺起始区域的右边界 X 坐标（像素）
        public const float SuperDashY = 385f;              // 超级冲刺的 Y 坐标（像素），需在地面高度才能冲刺

        // --- AI 与游戏规则参数 ---
        public const float OpponentDelta = 60f;            // AI 对手与玩家球员保持的水平间距（像素），控制防守贴身程度
        public const float IdealJumpBallJump = 0.5f;       // 跳球时 AI 理想起跳时机（0~1 比例），越接近 0.5 表示在球到最高点时跳
        public const float IdealAttackJump = 0.41f;        // 进攻时 AI 理想投篮起跳时机（0~1 比例），控制 AI 投篮节奏
        public const float ChanceForThree = 0.2f;          // AI 在近距离区域选择投三分球的概率（20%）
        public const float ChanceForThree2 = 0.4f;         // AI 在远距离区域选择投三分球的概率（40%），远距离更倾向三分
        public const float AttackZoneStart = 120f;         // 进攻区域起始 X 坐标（像素），进入此区域 AI 开始考虑进攻动作
        public const float AttackZoneEnd = 350f;           // 进攻区域结束 X 坐标（像素），超过此位置 AI 不再尝试进攻
        public const float DashZoneStart = 300f;           // 冲刺区域起始 X 坐标（像素），在此范围内 AI 可发动冲刺突破
        public const float DashZoneEnd = 700f;             // 冲刺区域结束 X 坐标（像素）
        public const float DefensePoint = 250f;            // AI 防守站位的参考 X 坐标（像素），AI 回防时会回到此位置附近
        public const float StealDuration = 0.3f;           // 抢断动作的总持续时间（秒），此期间球员处于抢断动画中
        public const float StealFrameEventTime = 8f / 30f; // 抢断动画中判定抢断成功的时刻（秒），8 帧 @ 30fps 时实际触碰球
        public const float StealAnimationDuration = 13f / 30f; // 抢断动画总时长（秒），13 帧 @ 30fps，含收招
        public const float StunDuration = 22f / 30f;       // 被抢断后的眩晕硬直时间（秒），22 帧 @ 30fps，期间无法行动
        public const float ThreePointsDistance = mlpConstants.Width2; // 三分线距离（像素），等于场地宽度的一半（400 像素）
        public const float DashDelay = 1f;                 // 冲刺技能冷却时间（秒），使用后需等待此时间才能再次冲刺
        public const float DashDoubleTapWindow = 0.55f;    // 双击方向键触发冲刺的时间窗口（秒），两次按键间隔在此内视为双击
        public const float DashInputBuffer = 0.22f;        // 冲刺输入缓冲时间（秒），提前按键也能被缓冲并在适当时机触发
        public const float DigTime = 3f;                   // 蓄力/运球突破的蓄力时间（秒），按住此时间后释放发动突破
        public const float EnergyTime = 3f;                // 技能能量恢复的冷却时间（秒），使用技能后需等待此时间恢复
        public const float DunkPickupLock = 0.22f;         // 扣篮后球的拾取锁定时间（秒），防止扣篮后立即被对方抢球

        // --- 场地边界参数 ---
        public const float FloorY = 420f;                  // 地面 Y 坐标（像素），球员站立位置和球弹跳的底部边界
        public const float BallFloorY = FloorY - BallRadius; // 球触地的实际 Y 坐标（像素），考虑球半径后的精确弹跳点
    }
}
