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

        public void Dispatch(rimrushPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
