// 投篮和得分判定逻辑
// 处理篮球是否投进篮筐的检测，计算得几分（2 分还是 3 分），判断球有没有碰到篮筐边缘，以及补篮和盖帽后的得分。

namespace mlp
{
    /// <summary>
    /// 投篮和得分判定器：处理篮球是否投进篮筐的检测，计算得几分（2 分还是 3 分），判断球有没有被盖帽，以及篮筐传感器的触发逻辑。
    /// </summary>
    public sealed class mlpMatchProcessor
    {
        private bool canScore = true;          // 开关：本轮是否还能判定得分（防止重复判定）
        private bool upperSensorPassed;        // 开关：球是否已经经过篮筐上方传感器
        private int shotSide;                  // 投篮方所在半场：-1 左半场，1 右半场
        private bool isHuman;                  // 投篮的(或盖帽后归属方)是否为玩家
        private int throwType;                 // 投篮类型：0=远投三分，正数=普通投篮，负数=特殊投篮
        private int shotPlayerNo = -1;         // 投篮球员编号，-1 表示未设定
        private int blockSide;                 // 盖帽方所在半场：0=没人盖帽，-1/1 为盖帽方
        private bool blockIsHuman;             // 盖帽的是否为玩家

        public int ThrowType => throwType;
        public int ShotSide => shotSide;
        public int ShotPlayerNo => shotPlayerNo;
        public int BlockSide => blockSide;
        public bool IsHuman => isHuman;

        /// <summary>
        /// 清除所有投篮和盖帽的追踪状态，为新一轮进攻做准备。
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
        /// 记录一次投篮动作。重置之前的状态，并保存投篮者信息、所在半场和投篮类型。
        /// </summary>
        /// <param name="side">投篮者所在半场（-1 = 左侧，1 = 右侧）。</param>
        /// <param name="shotByHuman">是否为玩家手动投篮，false 表示 AI 投篮。</param>
        /// <param name="shotThrowType">0 = 远距离投篮，正数 = 普通投篮，负数 = 特殊投篮。</param>
        public void Shoot(int side, bool shotByHuman, int shotThrowType, int shooterPlayerNo = -1)
        {
            Reset();
            shotSide = side;
            isHuman = shotByHuman;
            throwType = shotThrowType;
            shotPlayerNo = shooterPlayerNo;
        }

        /// <summary>
        /// 记录一次盖帽尝试。即使球最终进了，如果被盖帽者碰触过，仍按 2 分计算。
        /// </summary>
        /// <param name="side">盖帽球员所在半场（-1 = 左侧，1 = 右侧）。</param>
        /// <param name="blockedByHuman">是否为玩家手动盖帽，false 表示 AI 盖帽。</param>
        public void Block(int side, bool blockedByHuman)
        {
            // 保留当前的投篮/传感器链路，这样被盖帽的球如果继续沿着原篮筐路径飞行，仍然可以正确判定。
            blockSide = side;
            blockIsHuman = blockedByHuman;
        }

        /// <summary>
        /// 处理篮筐传感器触发事件。球必须先经过上方传感器再经过下方传感器，才算有效进球。
        /// </summary>
        /// <param name="sensorType">0 = 上方传感器，非零 = 下方传感器。</param>
        /// <returns>球成功入篮（上下传感器依次触发）时返回 true。</returns>
        public bool ProcessSensor(int sensorType)
        {
            // 1. 已经判定过得分，不再重复判定
            if (!canScore)
            {
                return false;
            }

            // 2. 上方传感器触发：标记球已通过上方
            if (sensorType == 0)
            {
                upperSensorPassed = true;
                return false;
            }

            // 3. 下方传感器触发且上方已通过：有效进球
            if (upperSensorPassed)
            {
                canScore = false;
                upperSensorPassed = false;
                return true;
            }

            // 4. 下方先触发（未经过上方）：无效，禁止后续得分
            canScore = false;
            return false;
        }

        /// <summary> 算分
        /// 计算一次成功进球的得分。被盖帽后仍进的球始终计 2 分；远距离投篮计 3 分；其他情况使用默认分值。
        /// </summary>
        /// <param name="scoringSide">得分方所在半场（-1 = 左侧，1 = 右侧）。</param>
        /// <param name="fallbackPoints">默认分值（通常根据距离为 2 或 3）。</param>
        /// <returns>本次进球获得的分数。</returns>
        public int ResolvePointsForScore(int scoringSide, int fallbackPoints)
        {
            // 1. 被对手盖帽后仍然进的球，统一按 2 分结算
            if (blockSide == -scoringSide)
            {
                isHuman = blockIsHuman;
                throwType = 2;
                return 2;
            }

            // 2. 远距离投篮（throwType == 0）计 3 分
            if (throwType == 0)
            {
                return 3;
            }

            // 3. 普通投篮（throwType > 0）计 2 分
            if (throwType > 0)
            {
                return 2;
            }

            // 4. 其他情况使用传入的默认分值
            return fallbackPoints;
        }
    }
}
