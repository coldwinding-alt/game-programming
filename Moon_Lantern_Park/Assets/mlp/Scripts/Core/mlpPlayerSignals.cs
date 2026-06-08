// 玩家动作信号系统
// 当玩家做出特定动作（投篮、扣篮、抢断、盖帽、得分等）时发出通知。教程系统和其他模块监听这些信号来做出反应，比如教程检测玩家是否完成了指定操作。

using System;

namespace mlp
{
    public enum mlpPlayerSignalType
    {
        StartSteal,
        Steal,
        StealSuccess,
        JumpA,
        Pump,
        Dash,
        Shoot,
        Dunk,
        PutbackDunk,
        Block,
        Score,
        Super,
        Stun
    }

    public sealed class mlpPlayerSignalBus
    {
        public event Action<mlpPlayerSignalType, int, int> OnSignal;

        /// <summary>
        /// 向所有注册的监听器（如教程系统）广播玩家动作事件。
        /// </summary>
        /// <param name="signal">发生的动作类型（如投篮、扣篮、抢断等）。</param>
        /// <param name="side">玩家所属队伍（-1 = 左侧，1 = 右侧）。</param>
        /// <param name="playerNo">球员在队伍中的索引。</param>
        public void Dispatch(mlpPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
