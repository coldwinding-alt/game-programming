// 投篮和得分判定逻辑
// 处理篮球是否投进篮筐的检测，计算得几分（2 分还是 3 分），判断球有没有碰到篮筐边缘，以及补篮和盖帽后的得分。

namespace rimrush
{
    // Lightweight score-context helper for basket sensor processing.
    public sealed class rimrushMatchProcessor
    {
        private bool canScore = true;
        private bool upperSensorPassed;
        private int shotSide;
        private bool isHuman;
        private int throwType;
        private int shotPlayerNo = -1;
        private int blockSide;
        private bool blockIsHuman;

        public int ThrowType => throwType;
        public int ShotSide => shotSide;
        public int ShotPlayerNo => shotPlayerNo;
        public int BlockSide => blockSide;
        public bool IsHuman => isHuman;

        /// <summary>
        /// Clears all shot and block tracking state, preparing for a new play.
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
        /// Records that a shot was taken. Resets previous state and stores who shot, from which side, and the throw type.
        /// </summary>
        /// <param name="side">Which side of the court the shooter is on (-1 = left, 1 = right).</param>
        /// <param name="shotByHuman">True if the shot was taken by a human player, false for AI.</param>
        /// <param name="shotThrowType">0 = long-range shot, positive = normal shot, negative = special.</param>
        public void Shoot(int side, bool shotByHuman, int shotThrowType, int shooterPlayerNo = -1)
        {
            Reset();
            shotSide = side;
            isHuman = shotByHuman;
            throwType = shotThrowType;
            shotPlayerNo = shooterPlayerNo;
        }

        /// <summary>
        /// Records that a block was attempted. This lets a scored ball still count as 2 points if it was deflected by the blocker.
        /// </summary>
        /// <param name="side">Which side the blocking player is on (-1 = left, 1 = right).</param>
        /// <param name="blockedByHuman">True if the block was by a human player, false for AI.</param>
        public void Block(int side, bool blockedByHuman)
        {
            // Preserve the current shot/sensor chain so a blocked ball can still
            // resolve if it continues through the original basket path.
            blockSide = side;
            blockIsHuman = blockedByHuman;
        }

        /// <summary>
        /// Processes a basket sensor trigger. The upper sensor must be hit before the lower sensor
        /// for the basket to count as a valid score.
        /// </summary>
        /// <param name="sensorType">0 = upper sensor, nonzero = lower sensor.</param>
        /// <returns>True if the ball scored (upper then lower sensor were both triggered).</returns>
        public bool ProcessSensor(int sensorType)
        {
            if (!canScore)
            {
                return false;
            }

            if (sensorType == 0)
            {
                upperSensorPassed = true;
                return false;
            }

            if (upperSensorPassed)
            {
                canScore = false;
                upperSensorPassed = false;
                return true;
            }

            canScore = false;
            return false;
        }

        /// <summary>
        /// Calculates how many points a successful basket is worth. Blocked shots that still go in
        /// are always worth 2 points; long-range shots are worth 3; everything else uses the fallback value.
        /// </summary>
        /// <param name="scoringSide">Which side scored (-1 = left, 1 = right).</param>
        /// <param name="fallbackPoints">Default point value (usually 2 or 3 based on distance).</param>
        /// <returns>The number of points awarded for this basket.</returns>
        public int ResolvePointsForScore(int scoringSide, int fallbackPoints)
        {
            // Scores armed by a self-block chain are settled as 2 points.
            if (blockSide == -scoringSide)
            {
                isHuman = blockIsHuman;
                throwType = 2;
                return 2;
            }

            if (throwType == 0)
            {
                return 3;
            }

            if (throwType > 0)
            {
                return 2;
            }

            return fallbackPoints;
        }
    }
}
