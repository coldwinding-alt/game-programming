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
        /// Broadcasts a player action event to all registered listeners (such as the tutorial system).
        /// </summary>
        /// <param name="signal">The type of action that occurred (e.g. Shoot, Dunk, Steal).</param>
        /// <param name="side">Which team the player belongs to (-1 = left, 1 = right).</param>
        /// <param name="playerNo">Index of the player within their team.</param>
        public void Dispatch(mlpPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
