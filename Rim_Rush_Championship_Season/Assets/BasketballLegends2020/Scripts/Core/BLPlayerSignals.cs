using System;

namespace BasketballLegends2020
{
    public enum BLPlayerSignalType
    {
        StartSteal,
        Steal,
        JumpA,
        Pump,
        Dash,
        Stun
    }

    public sealed class BLPlayerSignalBus
    {
        public event Action<BLPlayerSignalType, int, int> OnSignal;

        public void Dispatch(BLPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
