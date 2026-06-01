// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushMatchProcessor 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

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
        /// Executes Reset for the rimrushMatchProcessor workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Shoot for the rimrushMatchProcessor workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="shotByHuman">Input value used by this step of the workflow.</param>
        /// <param name="shotThrowType">Input value used by this step of the workflow.</param>
        public void Shoot(int side, bool shotByHuman, int shotThrowType, int shooterPlayerNo = -1)
        {
            Reset();
            shotSide = side;
            isHuman = shotByHuman;
            throwType = shotThrowType;
            shotPlayerNo = shooterPlayerNo;
        }

        /// <summary>
        /// Executes Block for the rimrushMatchProcessor workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="blockedByHuman">Input value used by this step of the workflow.</param>
        public void Block(int side, bool blockedByHuman)
        {
            // Preserve the current shot/sensor chain so a blocked ball can still
            // resolve if it continues through the original basket path.
            blockSide = side;
            blockIsHuman = blockedByHuman;
        }

        /// <summary>
        /// Executes Process Sensor for the rimrushMatchProcessor workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="sensorType">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Resolve Points For Score for the rimrushMatchProcessor workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
        /// <param name="fallbackPoints">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
