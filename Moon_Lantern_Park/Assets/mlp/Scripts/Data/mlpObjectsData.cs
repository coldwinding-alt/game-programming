// Game object physical parameters
// Define the physical values ​​of basketballs, players, baskets and other game objects: gravity, bounce coefficient, movement speed, shooting power, dunk range, etc. The movement of all objects in the game refers to these values.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Physical parameters of game objects: Define the physical values ​​of basketballs, players, baskets and other objects - gravity, bounce coefficient, movement speed, shooting power, dunk range, etc.

    /// </summary>
    public static class mlpObjectsData
    {
        // Global gravity vector (pixels/second²), with a Y component of 450 for downward acceleration and an X of 0 for no horizontal gravity
        public static readonly Vector2 Gravity = new Vector2(0f, 450f);

        // --- Basketball physical parameters ---

        public const float BallRadius = 18f;               // Basketball collision radius (pixels), used for collision detection of baskets, backboards, shields, etc.

        public const float BallGravMass = 2f;              // Basketball gravity multiplier, actual gravity = Gravity.y × BallGravMass, making the ball fall faster than other objects

        public const float BallBounce = -400f;             // The vertical speed of the basketball when it hits the ground (negative value = upward). The larger the value, the higher it will bounce.

        public const float BallUpVelocityY = -500f;        // The upward initial velocity when the basketball starts/jumps (negative value = upward), which determines the height of the opening throw.

        public const float BallStealVelocityXBase = 400f;  // The basic horizontal speed of the ball after a steal (pixels/second), the direction is determined by the direction of the stealer
        public const float BallStealVelocityXAdd = 200f;   // The farther the steal is, the additional horizontal speed will make the ball fly farther

        public const float BallStealVelocityY = -100f;     // The vertical speed of the ball after the steal (negative value = upward), allowing the ball to bounce slightly

        public const float BallIndentYCenter = 300f;       // The basketball's starting Y coordinate (in pixels, measured from the top of the screen) when the ball jumps at midcourt

        public const float BallIndentYPlayer = 340f;       // The Y coordinate offset of the basketball when the player holds the ball, controlling the height of the ball in the hand

        public const float VerticalDispersion = 0.1f;      // The maximum deviation coefficient in the vertical direction of a shot. The higher the release point (jump shot), the greater the deviation.

        public const float Dispersion = 0.01f;             // The basic random offset coefficient of the shot is superimposed on distance, height, running and other factors to determine whether the ball hits the target.


        // --- Basket and backboard parameters ---

        public const float BasketIndent = 20f;             // The distance (in pixels) between the edge of the basket and the edge of the court, when the basket is not close to the wall

        public const float BasketRadius = 30f;             // Basket radius (pixels), determines the size of the basket and shooting tolerance

        public const float BasketCenter = BasketIndent + BasketRadius;   // X coordinate of center of left basket (pixels)

        public const float BasketCenter2 = mlpConstants.Width - BasketCenter; // X coordinate (pixels) of the center of the right basket, mirrored

        public const float BasketHeight = 200f;            // The height (in pixels) of the center of the basket from the top of the screen, controlling the position of the basket in the screen

        public const float BasketPartRadius = 7f;          // The radius (pixels) of the hoop's collision with the cylinder, superimposed with BallRadius to calculate the collision between the ball and the hoop.

        public const float GlassWidth = 12f;               // The horizontal distance (in pixels) between the backboard glass plane and the court boundary, which determines the X position of the backboard collision surface

        public const float GlassHeight = 120f;             // The vertical height of the backboard glass (in pixels). The ball will collide with the backboard only within this range.

        public const float GlassY = 20f - GlassHeight;     // Y coordinate offset of the top of the backboard glass (relative to the height of the basket), negative values indicate above the basket

        public const float SensorHalf = 25f;               // Score sensor half width (pixels), 25 pixels left and right of the sensor center to determine the goal

        public const float SensorWidth = 2f * SensorHalf;  // The total width of the scoring sensor (pixels) through which the ball must pass to be considered a valid goal path

        public const float SensorHeight = 5f;              // Vertical thickness of the scoring sensor in pixels, height of the decision band between the upper and lower sensors

        public const float SensorUp = -10f;                // The Y offset of the upper sensor relative to the height of the basket (negative = above), the ball will be considered a goal if it passes this line first and then the lower line

        public const float SensorDown = 15f;               // Y offset of the lower sensor relative to the height of the basket (positive value = below), the ball crossing this line to confirm a goal


        // --- Player/player parameters ---

        public const float PlayerJump = -600f;             // The initial vertical speed of the player's jump (negative value = upward). The higher the value, the higher the jump.

        public const float PlayerMove = 250f;              // Player's horizontal movement speed when empty-handed (pixels/second)
        public const float PlayerMoveWithBall = 0.85f * PlayerMove; // The movement speed of holding the ball is 85% of the unarmed speed. Holding the ball will slow down slightly.

        public const float PlayerIndentX = 30f;            // The minimum distance (in pixels) between the player's collision body and the left and right boundaries of the field to prevent players from walking out of the field

        public const float PlayerIndentY = 385f;           // The Y coordinate (in pixels) of the player's feet when standing, i.e. the player's position on the ground

        public const float PlayersHandsWidth = 30f;        // The width (pixels) of the player's hand collision body, used to determine the ball pickup and steal range

        public const float PlayersHandsHeight = 80f;       // The height of the player's hand collision body (pixels), the ball pickup/stealing judgment range in the vertical direction

        public const float BallPickupDistanceX = PlayersHandsWidth * 0.5f + BallRadius;  // Effective distance for picking up the ball in the horizontal direction (half hand width + ball radius)

        public const float BallPickupDistanceY = PlayersHandsHeight * 0.5f + BallRadius; // Effective distance for picking up the ball in the vertical direction (half height of hand + ball radius)

        public const float StealDistance = 55f;            // The horizontal distance threshold (pixels) for the steal to be triggered. The steal can only be initiated within this range.

        public const float IndentGeneralX = 50f;           // The player's general horizontal activity boundary indentation amount (pixels), limiting the player's movement range

        public const float BlockWidth = 20f;               // The width of the block judgment area (pixels). You can block shots if you stand in front of the ball holder within this range.

        public const float BlockHeight = 70f;              // The height of the block determination area (pixels), the effective block range in the vertical direction

        public const float JumpBlockWidth = 10f;           // The width (pixels) of the judgment area when jumping to block a shot. The jumping block is narrower than the standing block but can block high balls.

        public const float JumpBlockHeight = 70f;          // The height of the judgment area when jumping to block the shot (pixels)

        public const float BlockStartDuration = 3f / 30f;  // Duration of the starting phase of the shot-blocking action (seconds), 3 frames @ 30fps, during which the shot-blocking action can be triggered

        public const float BlockEndDuration = 5f / 30f;    // Duration of the closing phase of the block action (seconds), 5 frames @ 30fps, there is a brief stiffness after the block

        public const float PumpStartDuration = 4f / 30f;   // Duration of the initial phase of the feint (seconds), 4 frames @ 30fps

        public const float PumpEndDuration = 4f / 30f;     // Duration of feint closing phase (seconds), 4 frames @ 30fps


        // --- Dunk system parameters ---

        public const float PaintStartX = 100f;             // The starting X coordinate (pixels) of the three-second zone (paint area). Only when you enter this area can you attempt a dunk.

        public const float PaintMiddleX = 200f;            // The X coordinate (pixels) of the center line of the three-second zone. AI uses this to determine whether it is deep into the dunk position.

        public const float DunkZone1Y = 280f;              // Y coordinate (pixels) of the upper boundary of the dunk trigger area. Players must be below this height to dunk.

        public const float DunkZone2Y = 300f;              // Y coordinate (pixel) of the lower boundary of the dunk trigger area, which together with DunkZone1Y defines the dunk effective area

        public const float DunkX = 100f;                   // The horizontal offset (in pixels) of the player flying towards the basket in the dunk animation, which controls the take-off distance of the dunk.

        public const float DunkY = 180f;                   // The vertical offset (pixels) of the player flying towards the basket in the dunk animation, which controls the take-off height of the dunk.
        public const float DunkChanceToComplete = 0.9f;    // Probability of successfully completing the dunk (90%), 10% probability of being bounced off the basket (dunk)

        // The total number of frames for each of the three dunk animations (needs to be synchronized with the number of frames generated in Tools/Art/rebuild_runtime_dragonbones_skeleton.py)

        public const float Dunk1Duration = 24f / 30f;      // Total dunk type 1 animation duration (seconds), 24 frames @ 30fps

        public const float Dunk2Duration = 15f / 30f;      // Total animation duration (seconds) for dunk type 2, 15 frames @ 30fps, fastest dunk

        public const float Dunk3Duration = 24f / 30f;      // Total dunk type 3 animation duration (seconds), 24 frames @ 30fps


        // Fine-tuning of the feel during runtime: the actual flight time of the player from takeoff to flying to the basket (slightly shorter than the total duration of the animation to ensure a smooth feel)

        public const float Dunk1TravelDuration = 19f / 30f; // Duration of dunk type 1 flight (seconds)

        public const float Dunk2TravelDuration = 12f / 30f; // Duration of dunk type 2 flight (seconds)

        public const float Dunk3TravelDuration = 18f / 30f; // Duration of dunk type 3 flight (seconds)


        // The moment when the ball is released from the hand (the first few seconds during the flight when the ball is released and enters the basket), it must be earlier than the end of the flight to ensure that the ball arrives first

        public const float Dunk1ReleaseTime = 18f / 30f;   // Dunk Type 1 Ball Release Time (Seconds)

        public const float Dunk2ReleaseTime = 9f / 30f;    // Dunk type 2 ball release time (seconds)

        public const float Dunk3ReleaseTime = 14f / 30f;   // Dunk type 3 ball release time (seconds)


        // Dunk skeleton animation playback speed multiplier, >1 means accelerated playback to make the animation more compact and powerful

        public const float Dunk1AnimationSpeed = 1.16f;    // Dunk type 1 animation playback speed (16% speedup)

        public const float Dunk2AnimationSpeed = 1.12f;    // Dunk type 2 animation playback speed (12% speedup)

        public const float Dunk3AnimationSpeed = 1.16f;    // Dunk type 3 animation playback speed (16% speedup)


        // --- Alley-oop and sprint parameters ---

        public const float AlleyOopX = 160f;               // Horizontal offset (in pixels) of the alley catch point where the ball flies to this X position near the basket

        public const float AlleyOopY = 150f;               // The Y coordinate (pixels) of the alley catch point. The player jumps to this height to complete the alley catch.

        public const float SuperDashX1 = 150f;             // The X coordinate (in pixels) of the left boundary of the super sprint starting area, within which sprint can be launched

        public const float SuperDashX2 = 650f;             // Right edge X coordinate of the super sprint starting area (pixels)

        public const float SuperDashY = 385f;              // Y coordinate (pixels) of super sprint, you need to be at ground height to sprint


        // ---AI and game rules parameters ---
        public const float OpponentDelta = 60f;            // The horizontal distance (pixels) between AI opponents and player players to control the degree of defensive closeness

        public const float IdealJumpBallJump = 0.5f;       // AI's ideal take-off time when jumping the ball (0~1 ratio), the closer it is to 0.5, it means jumping when the ball reaches the highest point

        public const float IdealAttackJump = 0.41f;        // AI's ideal shooting timing when attacking (0~1 ratio), controls AI's shooting rhythm

        public const float ChanceForThree = 0.2f;          // The probability that the AI will choose to shoot a three-pointer in a close area (20%)

        public const float ChanceForThree2 = 0.4f;         // The probability of AI choosing to shoot a three-pointer from a long distance (40%), with a preference for three-pointers from a distance

        public const float AttackZoneStart = 120f;         // The starting X coordinate of the attack area (pixels). When entering this area, the AI starts to consider offensive actions.

        public const float AttackZoneEnd = 350f;           // The end X coordinate of the attack area (pixels), beyond which the AI will no longer attempt to attack

        public const float DashZoneStart = 300f;           // The starting X coordinate of the sprint area (pixels), within this range the AI can launch a sprint breakthrough

        public const float DashZoneEnd = 700f;             // Sprint area end X coordinate (pixels)

        public const float DefensePoint = 250f;            // The reference X coordinate (pixels) of the AI's defensive position. The AI will return to this position when returning to defense.

        public const float StealDuration = 0.3f;           // The total duration of the tackle action (seconds) during which the player is in the tackle animation

        public const float StealFrameEventTime = 8f / 30f; // The moment (seconds) when the tackle is determined to be successful in the tackle animation, when the ball is actually touched at 8 frames @ 30fps

        public const float StealAnimationDuration = 13f / 30f; // Total steal animation duration (seconds), 13 frames @ 30fps, including closing move

        public const float StunDuration = 22f / 30f;       // Stun time after being intercepted (seconds), 22 frames @ 30fps, unable to move during this period

        public const float ThreePointsDistance = mlpConstants.Width2; // Three-point line distance in pixels, equal to half the field width (400 pixels)

        public const float DashDelay = 1f;                 // Sprint skill cooling time (seconds), you need to wait for this time after using it before you can sprint again

        public const float DashDoubleTapWindow = 0.55f;    // The time window (seconds) for double-clicking the direction key to trigger sprinting. The interval between two key presses is considered a double-click within this period.

        public const float DashInputBuffer = 0.22f;        // Sprint input buffering time (seconds), early key presses can also be buffered and triggered at the appropriate time

        public const float DigTime = 3f;                   // Charging time (seconds) for charging/dribbling breakthrough, press and hold this time and then release to initiate breakthrough

        public const float EnergyTime = 3f;                // Cooling time for skill energy recovery (seconds), you need to wait for this time to recover after using the skill

        public const float DunkPickupLock = 0.22f;         // The ball pick-up and lock time after dunking (seconds) to prevent the opponent from grabbing the ball immediately after dunking


        // --- Site boundary parameters ---

        public const float FloorY = 420f;                  // Ground Y coordinate (pixels), where the player is standing and the bottom boundary of the ball's bounce

        public const float BallFloorY = FloorY - BallRadius; // The actual Y coordinate (pixels) of the ball hitting the ground, the exact bounce point after taking into account the ball radius
    }
}
