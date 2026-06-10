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
        public static readonly Vector2 Gravity = new Vector2(0f, 450f);

        public const float BallRadius = 18f;
        public const float BallGravMass = 2f;
        public const float BallBounce = -400f;
        public const float BallUpVelocityY = -500f;
        public const float BallStealVelocityXBase = 400f;
        public const float BallStealVelocityXAdd = 200f;
        public const float BallStealVelocityY = -100f;
        public const float BallIndentYCenter = 300f;
        public const float BallIndentYPlayer = 340f;
        public const float VerticalDispersion = 0.1f;
        public const float Dispersion = 0.01f;

        public const float BasketIndent = 20f;
        public const float BasketRadius = 30f;
        public const float BasketCenter = BasketIndent + BasketRadius;
        public const float BasketCenter2 = mlpConstants.Width - BasketCenter;
        public const float BasketHeight = 200f;
        public const float BasketPartRadius = 7f;
        public const float GlassWidth = 12f;
        public const float GlassHeight = 120f;
        public const float GlassY = 20f - GlassHeight;
        public const float SensorHalf = 25f;
        public const float SensorWidth = 2f * SensorHalf;
        public const float SensorHeight = 5f;
        public const float SensorUp = -10f;
        public const float SensorDown = 15f;

        public const float PlayerJump = -600f;
        public const float PlayerMove = 250f;
        public const float PlayerMoveWithBall = 0.85f * PlayerMove;
        public const float PlayerIndentX = 30f;
        public const float PlayerIndentY = 385f;
        public const float PlayersHandsWidth = 30f;
        public const float PlayersHandsHeight = 80f;
        public const float BallPickupDistanceX = PlayersHandsWidth * 0.5f + BallRadius;
        public const float BallPickupDistanceY = PlayersHandsHeight * 0.5f + BallRadius;
        public const float StealDistance = 55f;
        public const float IndentGeneralX = 50f;
        public const float BlockWidth = 20f;
        public const float BlockHeight = 70f;
        public const float JumpBlockWidth = 10f;
        public const float JumpBlockHeight = 70f;
        public const float BlockStartDuration = 3f / 30f;
        public const float BlockEndDuration = 5f / 30f;
        public const float PumpStartDuration = 4f / 30f;
        public const float PumpEndDuration = 4f / 30f;

        public const float PaintStartX = 100f;
        public const float PaintMiddleX = 200f;
        public const float DunkZone1Y = 280f;
        public const float DunkZone2Y = 300f;
        public const float DunkX = 100f;
        public const float DunkY = 180f;
        public const float DunkChanceToComplete = 0.9f;
        // Keep these synced with the generated dunk1/dunk2/dunk3 frame counts in
        // Tools/Art/rebuild_runtime_dragonbones_skeleton.py.
        public const float Dunk1Duration = 24f / 30f;
        public const float Dunk2Duration = 15f / 30f;
        public const float Dunk3Duration = 24f / 30f;
        // Runtime feel tuning layered over the generated animation frame counts above.
        public const float Dunk1TravelDuration = 19f / 30f;
        public const float Dunk2TravelDuration = 12f / 30f;
        public const float Dunk3TravelDuration = 18f / 30f;
        public const float Dunk1ReleaseTime = 18f / 30f;
        public const float Dunk2ReleaseTime = 9f / 30f;
        public const float Dunk3ReleaseTime = 14f / 30f;
        public const float Dunk1AnimationSpeed = 1.16f;
        public const float Dunk2AnimationSpeed = 1.12f;
        public const float Dunk3AnimationSpeed = 1.16f;
        public const float AlleyOopX = 160f;
        public const float AlleyOopY = 150f;
        public const float SuperDashX1 = 150f;
        public const float SuperDashX2 = 650f;
        public const float SuperDashY = 385f;

        public const float OpponentDelta = 60f;
        public const float IdealJumpBallJump = 0.5f;
        public const float IdealAttackJump = 0.41f;
        public const float ChanceForThree = 0.2f;
        public const float ChanceForThree2 = 0.4f;
        public const float AttackZoneStart = 120f;
        public const float AttackZoneEnd = 350f;
        public const float DashZoneStart = 300f;
        public const float DashZoneEnd = 700f;
        public const float DefensePoint = 250f;
        public const float StealDuration = 0.3f;
        public const float StealFrameEventTime = 8f / 30f;
        public const float StealAnimationDuration = 13f / 30f;
        public const float StunDuration = 22f / 30f;
        public const float ThreePointsDistance = mlpConstants.Width2;
        public const float DashDelay = 1f;
        public const float DashDoubleTapWindow = 0.55f;
        public const float DashInputBuffer = 0.22f;
        public const float DigTime = 3f;
        public const float EnergyTime = 3f;
        public const float DunkPickupLock = 0.22f;

        public const float FloorY = 420f;
        public const float BallFloorY = FloorY - BallRadius;
    }
}
