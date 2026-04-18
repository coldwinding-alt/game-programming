using UnityEngine;

namespace BasketballLegends2020
{
    public static class BLObjectsData
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

        public const float BasketIndent = 25f;
        public const float BasketRadius = 30f;
        public const float BasketCenter = BasketIndent + BasketRadius;
        public const float BasketCenter2 = BLConstants.Width - BasketCenter;
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

        public const float PaintStartX = 100f;
        public const float PaintMiddleX = 200f;
        public const float DunkZone1Y = 280f;
        public const float DunkZone2Y = 300f;
        public const float DunkX = 100f;
        public const float DunkY = 180f;
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
        public const float StunDuration = 22f / 30f;
        public const float ThreePointsDistance = 500f;
        public const float DashDelay = 1f;
        public const float DigTime = 3f;
        public const float EnergyTime = 3f;

        public const float FloorY = 420f;
        public const float BallFloorY = FloorY - BallRadius;
    }
}
