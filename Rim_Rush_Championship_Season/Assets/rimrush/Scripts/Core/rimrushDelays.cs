// 延时计时器工具类
// 提供几种不同类型的计时器，比如倒计时结束后触发、使用后冷却等。AI 和游戏逻辑用这些计时器来控制节奏，比如 AI 等几秒再做动作。

using UnityEngine;

namespace rimrush
{
    public interface IDelay
    {
        void Activate();
        int Update(float dt);
        void Reset();
    }

    public class FullDelay : IDelay
    {
        protected readonly float range;
        protected readonly float fixedDelay;
        protected float delta = -1f;
        protected float delay;
        protected float dispersion;

        /// <summary>
        /// Creates a timer that waits for a fixed base delay plus a random amount within the given range.
        /// </summary>
        /// <param name="range">Maximum random extra time (in seconds) added on top of the fixed delay.</param>
        /// <param name="fixedDelay">Base delay (in seconds) that is always added.</param>
        public FullDelay(float range, float fixedDelay = 0f)
        {
            this.range = range;
            this.fixedDelay = fixedDelay;
        }

        /// <summary>
        /// Starts the timer. The actual delay will be the fixed delay plus a random value between 0 and the range.
        /// </summary>
        public virtual void Activate()
        {
            delta = 0f;
            dispersion = Random.value * range;
            delay = fixedDelay + dispersion;
        }

        /// <summary>
        /// Ticks the timer forward by dt seconds. Returns 1 when the delay has elapsed, 0 while still waiting, or -1 if the timer was never activated.
        /// </summary>
        /// <param name="dt">Elapsed time in seconds since the last update.</param>
        /// <returns>1 = delay finished, 0 = still counting, -1 = timer not active.</returns>
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
        /// Stops the timer and resets it so it can be activated again.
        /// </summary>
        public virtual void Reset()
        {
            delta = -1f;
        }
    }

    public sealed class UseDelay : FullDelay
    {
        /// <summary>
        /// Creates a timer that always waits exactly the given number of seconds (no random variation).
        /// </summary>
        /// <param name="range">Delay duration in seconds.</param>
        public UseDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Starts the timer with a fixed delay equal to the range value.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = range;
        }
    }

    public sealed class NegativeDelay : FullDelay
    {
        /// <summary>
        /// Creates a timer where the random portion can be subtracted from or added to the fixed delay.
        /// The final delay is fixedDelay plus or minus a random value within the range.
        /// </summary>
        /// <param name="range">Maximum random offset (in seconds) that is added or subtracted.</param>
        /// <param name="fixedDelay">Base delay (in seconds).</param>
        public NegativeDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Starts the timer with a 50/50 chance of adding or subtracting the random portion.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            var sign = Random.value <= 0.5f ? -1f : 1f;
            delay = fixedDelay + sign * dispersion;
        }
    }

    public sealed class SimpleDelay : FullDelay
    {
        /// <summary>
        /// Creates a timer that waits a random amount of time between 0 and the given range.
        /// </summary>
        /// <param name="range">Maximum delay in seconds.</param>
        public SimpleDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Starts the timer with a random delay between 0 and the range value.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = range * Random.value;
        }
    }

    public sealed class AIUseDelay : FullDelay
    {
        /// <summary>
        /// Creates a cooldown timer used by AI characters. Supports forcing the cooldown to restart early or expire sooner.
        /// </summary>
        /// <param name="range">Maximum random portion (in seconds) of the cooldown.</param>
        /// <param name="fixedDelay">Base cooldown time (in seconds).</param>
        public AIUseDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Starts the cooldown with a random duration between 0 and the range value.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = Random.value * range;
        }

        /// <summary>
        /// Ticks the AI cooldown timer. Returns 1 when the cooldown finishes, 0 while counting down,
        /// -1 when the timer is idle or just finished, and -2 when a forced cooldown is still in progress.
        /// </summary>
        /// <param name="dt">Elapsed time in seconds since the last update.</param>
        /// <returns>1 = cooldown done, 0 = still counting, -1 = idle, -2 = forced cooldown running.</returns>
        public override int Update(float dt)
        {
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

            if (Mathf.Approximately(delta, -1f))
            {
                return -1;
            }

            delta += dt;
            if (delta >= -1f)
            {
                delta = -1f;
                return -1;
            }

            return -2;
        }

        /// <summary>
        /// Forces the cooldown to restart at its full fixed-delay duration.
        /// Call this when the AI performs an action that should trigger a cooldown.
        /// </summary>
        public void UseIt()
        {
            delta = -1f - fixedDelay;
        }

        /// <summary>
        /// Shortens the cooldown by half the fixed delay, so the AI can act again sooner.
        /// </summary>
        public void SkipIt()
        {
            delta = -1f - fixedDelay * 0.5f;
        }
    }
}
