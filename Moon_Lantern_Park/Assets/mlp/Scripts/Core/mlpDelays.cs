// Delay timer tool class
// Provides several different types of timers, such as triggering after countdown, cooling after use, etc. AI and game logic use these timers to control pacing, such as how many seconds the AI ​​waits before making an action.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Unified interface for delay timers. All timers support three operations: start, update per frame, and reset.
    /// </summary>
    public interface IDelay
    {
        void Activate();
        int Update(float dt);
        void Reset();
    }

    /// <summary>
    /// Basic timer: wait for fixed time + random time. All other timers inherit from this class.
    /// </summary>
    public class FullDelay : IDelay
    {
        protected readonly float range;
        protected readonly float fixedDelay;
        protected float delta = -1f;
        protected float delay;
        protected float dispersion;

        /// <summary>
        /// Creates a timer that waits for a fixed base delay plus a random amount of time within a given range.
        /// </summary>
        /// <param name="range">The maximum random amount of time (in seconds) to add to the fixed delay. </param>
        /// <param name="fixedDelay">The base delay in seconds that is always added. </param>
        public FullDelay(float range, float fixedDelay = 0f)
        {
            this.range = range;
            this.fixedDelay = fixedDelay;
        }

        /// <summary>
        /// Start timer. The actual delay is the fixed delay plus a random value between 0 and range.
        /// </summary>
        public virtual void Activate()
        {
            delta = 0f;
            dispersion = Random.value * range;
            delay = fixedDelay + dispersion;
        }

        /// <summary>
        /// The timer advances dt seconds. Returns 1 when the delay is over, 0 when the timer is still counting, and -1 when the timer is not activated.
        /// </summary>
        /// <param name="dt">The time in seconds since the last update. </param>
        /// <returns>1 = delayed completion, 0 = still counting, -1 = timer not active. </returns>
        public virtual int Update(float dt)
        {
            if (delta < 0f)
            {
                return -1;
            }

            delta += dt;
            if (delta >= delay)
            {
                delta = -1f;
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Stop the timer and reset it so it can be reactivated.
        /// </summary>
        public virtual void Reset()
        {
            delta = -1f;
        }
    }

    /// <summary>
    /// Precise timer: always waits for a fixed number of seconds, no random variation. Used for scenes that require precise time, such as skill cooldown.
    /// </summary>
    public sealed class UseDelay : FullDelay
    {
        /// <summary>
        /// Create a timer that always waits for a precisely specified number of seconds (no random variation).
        /// </summary>
        /// <param name="range">Delay length (seconds). </param>
        public UseDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Starts a timer with a range value as a fixed delay.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = range;
        }
    }

    /// <summary>
    /// Bidirectional random timer: The random part may be longer or shorter than the fixed time. Used for scenes that require a change of pace.
    /// </summary>
    public sealed class NegativeDelay : FullDelay
    {
        /// <summary>
        /// Create a timer where the random portion can be added to or subtracted from the fixed delay. The final delay is fixedDelay plus or minus a random value within range.
        /// </summary>
        /// <param name="range">Maximum random offset in seconds for addition and subtraction. </param>
        /// <param name="fixedDelay">Base delay (seconds). </param>
        public NegativeDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Start the timer by adding or subtracting a random portion with a 50% probability.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            var sign = Random.value <= 0.5f ? -1f : 1f;
            delay = fixedDelay + sign * dispersion;
        }
    }

    /// <summary>
    /// Simple random timer: waits for a random amount of time between 0 and the specified range. Used for scenarios such as AI response delays.
    /// </summary>
    public sealed class SimpleDelay : FullDelay
    {
        /// <summary>
        /// Create a timer that waits for a random amount of time between 0 and the given range.
        /// </summary>
        /// <param name="range">Maximum delay time (seconds). </param>
        public SimpleDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Start the timer with a random delay between 0 and range.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = range * Random.value;
        }
    }

    /// <summary>
    /// AI cooldown timer: exclusive for AI characters, supports forced restart of cooldown and early end of cooldown. Used to control the pace of AI action.
    /// </summary>
    public sealed class AIUseDelay : FullDelay
    {
        /// <summary>
        /// Create a cooldown timer used by the AI ​​character. Supports forced restart of cooldown or early end of cooldown.
        /// </summary>
        /// <param name="range">The maximum random portion of the cooldown in seconds. </param>
        /// <param name="fixedDelay">Base cooldown time (seconds). </param>
        public AIUseDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Starts the cooldown with a random duration between 0 and range.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = Random.value * range;
        }

        /// <summary>
        /// AI cooldown timer advances. Returns 1 when the cooling is completed, 0 when the countdown is in progress, -1 when idle or just completed, and -2 when the forced cooling is in progress.
        /// </summary>
        /// <param name="dt">The time in seconds since the last update. </param>
        /// <returns>1 = Cooldown completed, 0 = Countdown in progress, -1 = Idle, -2 = Forced cooldown in progress. </returns>
        public override int Update(float dt)
        {
            // 1. During normal countdown (delta >= 0): accumulated time, returns 1 when expired
            if (delta >= 0f)
            {
                delta += dt;
                if (delta >= delay)
                {
                    delta = -1f;
                    return 1;
                }

                return 0;
            }

            // 2. Idle state (delta is approximately equal to -1): return -1

            if (Mathf.Approximately(delta, -1f))
            {
                return -1;
            }

            // 3. Forced cooling (delta < -1): cumulative time, cooling ends when -1

            delta += dt;
            if (delta >= -1f)
            {
                delta = -1f;
                return -1;
            }

            // 4. Forced cooling is still in progress, returning -2
            return -2;
        }

        /// <summary>
        /// Forced cooldown restarts with the full fixed delay length. This method is called when the AI ​​performs an action that needs to trigger a cooldown.
        /// </summary>
        public void UseIt()
        {
            delta = -1f - fixedDelay;
        }

        /// <summary>
        /// Reduces the cooldown by half the fixed delay, allowing the AI ​​to act again faster.

        /// </summary>
        public void SkipIt()
        {
            delta = -1f - fixedDelay * 0.5f;
        }
    }
}
