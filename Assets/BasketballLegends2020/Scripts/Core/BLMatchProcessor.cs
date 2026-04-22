namespace BasketballLegends2020
{
    // Lightweight score-context helper for basket sensor processing.
    public sealed class BLMatchProcessor
    {
        private bool canScore = true;
        private bool upperSensorPassed;
        private int shotSide;
        private bool isHuman;
        private int throwType;
        private int blockSide;
        private bool blockIsHuman;

        public int ThrowType => throwType;
        public int ShotSide => shotSide;
        public int BlockSide => blockSide;
        public bool IsHuman => isHuman;

        public void Reset()
        {
            canScore = true;
            upperSensorPassed = false;
            blockSide = 0;
            blockIsHuman = false;
        }

        public void Shoot(int side, bool shotByHuman, int shotThrowType)
        {
            Reset();
            shotSide = side;
            isHuman = shotByHuman;
            throwType = shotThrowType;
        }

        public void Block(int side, bool blockedByHuman)
        {
            Reset();
            blockSide = side;
            blockIsHuman = blockedByHuman;
        }

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
