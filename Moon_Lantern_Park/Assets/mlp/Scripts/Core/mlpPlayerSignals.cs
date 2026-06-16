// Player action signal system
// Notify when a player takes a specific action (shoot, dunk, steal, block, score, etc.). The tutorial system and other modules listen to these signals to react, such as the tutorial detecting whether the player has completed a specified action.

using System;

namespace mlp
{
    /// <summary>
    /// Player action signal types: identifiers for various actions such as shooting, dunking, stealing, blocking, scoring, etc.

    /// </summary>
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

    /// <summary>
    /// Player action signal bus: Broadcast notifications when players make specific actions, and modules such as the tutorial system listen to these signals to respond.

    /// </summary>
    public sealed class mlpPlayerSignalBus
    {
        public event Action<mlpPlayerSignalType, int, int> OnSignal;

        /// <summary>
        /// Broadcast player action events to all registered listeners (such as the tutorial system).
        /// </summary>
        /// <param name="signal">The type of action that occurred (e.g. shot, dunk, steal, etc.). </param>
        /// <param name="side">The team the player belongs to (-1 = left, 1 = right). </param>
        /// <param name="playerNo">The index of the player in the team. </param>
        public void Dispatch(mlpPlayerSignalType signal, int side, int playerNo)
        {
            OnSignal?.Invoke(signal, side, playerNo);
        }
    }
}
