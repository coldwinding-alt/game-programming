// Shooting and scoring decision logic

// Processes the detection of whether the basketball is thrown into the basket, calculates the number of points (2 points or 3 points), determines whether the ball touches the edge of the basket, and scores after tip-ins and blocks.


namespace mlp
{
    /// <summary>
    /// Shot and score determiner: handles the detection of whether the basketball is shot into the basket, calculates the number of points (2 points or 3 points), determines whether the ball is blocked, and the triggering logic of the basket sensor.

    /// </summary>
    public sealed class mlpMatchProcessor
    {
        private bool canScore = true;          // Switch: Whether the score can still be determined in this round (to prevent repeated determination)

        private bool upperSensorPassed;        // Switch: Whether the ball has passed the sensor above the basket

        private int shotSide;                  // Half court of the shooting team: -1 left half, 1 right half

        private bool isHuman;                  // Whether the shooter (or the team to whom the shot belongs after the block) is a player

        private int throwType;                 // Shot type: 0=long three-point shot, positive number=normal shot, negative number=special shot

        private int shotPlayerNo = -1;         // Shooting player number, -1 means not set

        private int blockSide;                 // Half of the court where the shot-blocking team is: 0 = no one blocks the shot, -1/1 is the blocking team

        private bool blockIsHuman;             // Whether the person blocking the shot is a player

        public int ThrowType => throwType;
        public int ShotSide => shotSide;
        public int ShotPlayerNo => shotPlayerNo;
        public int BlockSide => blockSide;
        public bool IsHuman => isHuman;

        /// <summary>
        /// Clear the tracking status of all shots and blocks to prepare for a new round of offense.
        /// </summary>
        public void Reset()
        {
            canScore = true;
            upperSensorPassed = false;
            shotPlayerNo = -1;
            blockSide = 0;
            blockIsHuman = false;
        }

        /// <summary>
        /// Record a shooting motion. Reset the previous state and save the shooter, half and shot type.
        /// </summary>
        /// <param name="side">The half of the court where the shooter is (-1 = left, 1 = right). </param>
        /// <param name="shotByHuman">Whether the player manually shoots the ball, false means AI shooting. </param>
        /// <param name="shotThrowType">0 = long range shot, positive number = normal shot, negative number = special shot. </param>
        public void Shoot(int side, bool shotByHuman, int shotThrowType, int shooterPlayerNo = -1)
        {
            Reset();
            shotSide = side;
            isHuman = shotByHuman;
            throwType = shotThrowType;
            shotPlayerNo = shooterPlayerNo;
        }

        /// <summary>
        /// Record a blocked shot attempt. Even if the ball ends up going in, it still counts as 2 points if it was touched by the blocker.
        /// </summary>
        /// <param name="side">The half of the court where the player blocking the shot is (-1 = left, 1 = right). </param>
        /// <param name="blockedByHuman">Whether the player blocks the shot manually, false means AI blocks the shot. </param>
        public void Block(int side, bool blockedByHuman)
        {
            // Preserve the current shot/sensor link so that a blocked ball can still be determined correctly if it continues to fly along its original path to the basket.
            blockSide = side;
            blockIsHuman = blockedByHuman;
        }

        /// <summary>
        /// Handle basket sensor trigger events. The ball must first pass through the upper sensor and then the lower sensor to be considered a valid goal.
        /// </summary>
        /// <param name="sensorType">0 = upper sensor, non-zero = lower sensor. </param>
        /// <returns> Returns true when the ball successfully enters the basket (the upper and lower sensors are triggered in sequence). </returns>
        public bool ProcessSensor(int sensorType)
        {
            // 1. The score has already been determined and will not be determined again.

            if (!canScore)
            {
                return false;
            }

            // 2. The upper sensor triggers: the marking ball has passed the upper

            if (sensorType == 0)
            {
                upperSensorPassed = true;
                return false;
            }

            // 3. The lower sensor is triggered and the upper part is passed: a valid goal
            if (upperSensorPassed)
            {
                canScore = false;
                upperSensorPassed = false;
                return true;
            }

            // 4. The bottom triggers first (without passing the top): invalid, subsequent scoring is prohibited
            canScore = false;
            return false;
        }

        /// <summary> Score calculation
        /// Calculate the score for a successful goal. A shot scored after being blocked always counts 2 points; a shot from distance counts 3 points; otherwise the default value is used.
        /// </summary>
        /// <param name="scoringSide">The half of the court where the scorer is playing (-1 = left, 1 = right). </param>
        /// <param name="fallbackPoints">Default points value (usually 2 or 3 depending on distance). </param>
        /// <returns>The points earned for this goal. </returns>
        public int ResolvePointsForScore(int scoringSide, int fallbackPoints)
        {
            // 1. Goals scored after being blocked by the opponent will be settled as 2 points.

            if (blockSide == -scoringSide)
            {
                isHuman = blockIsHuman;
                throwType = 2;
                return 2;
            }

            // 2. Long-distance shots (throwType == 0) count for 3 points

            if (throwType == 0)
            {
                return 3;
            }

            // 3. Normal shot (throwType > 0) counts 2 points
            if (throwType > 0)
            {
                return 2;
            }

            // 4. In other cases, use the passed default score.
            return fallbackPoints;
        }
    }
}
