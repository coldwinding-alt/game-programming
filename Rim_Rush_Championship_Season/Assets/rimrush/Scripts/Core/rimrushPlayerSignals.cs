// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushPlayerSignals 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System;

namespace rimrush
{
    public enum rimrushPlayerSignalType
    {
        StartSteal,
        Steal,
        JumpA,
        Pump,
        Dash,
        Stun
    }

    public sealed class rimrushPlayerSignalBus
    {
        public event Action<rimrushPlayerSignalType, int, int> OnSignal;

        /// <summary>
        /// Executes Dispatch for the rimrushPlayerSignalBus workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="signal">Input value used by this step of the workflow.</param>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="playerNo">Input value used by this step of the workflow.</param>
        public void Dispatch(rimrushPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
