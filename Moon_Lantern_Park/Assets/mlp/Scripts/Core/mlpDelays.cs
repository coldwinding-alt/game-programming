// 延时计时器工具类
// 提供几种不同类型的计时器，比如倒计时结束后触发、使用后冷却等。AI 和游戏逻辑用这些计时器来控制节奏，比如 AI 等几秒再做动作。

using UnityEngine;

namespace mlp
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
        /// 创建一个计时器，等待固定基础延迟加上给定范围内的随机时长。
        /// </summary>
        /// <param name="range">在固定延迟之上额外添加的最大随机时长（秒）。</param>
        /// <param name="fixedDelay">始终添加的基础延迟（秒）。</param>
        public FullDelay(float range, float fixedDelay = 0f)
        {
            this.range = range;
            this.fixedDelay = fixedDelay;
        }

        /// <summary>
        /// 启动计时器。实际延迟为固定延迟加上 0 到 range 之间的随机值。
        /// </summary>
        public virtual void Activate()
        {
            delta = 0f;
            dispersion = Random.value * range;
            delay = fixedDelay + dispersion;
        }

        /// <summary>
        /// 计时器前进 dt 秒。延迟结束返回 1，仍在计时返回 0，计时器未激活返回 -1。
        /// </summary>
        /// <param name="dt">自上次更新以来经过的时间（秒）。</param>
        /// <returns>1 = 延迟完成，0 = 仍在计时，-1 = 计时器未激活。</returns>
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
        /// 停止计时器并重置，使其可以重新激活。
        /// </summary>
        public virtual void Reset()
        {
            delta = -1f;
        }
    }

    public sealed class UseDelay : FullDelay
    {
        /// <summary>
        /// 创建一个始终等待精确指定秒数的计时器（无随机变化）。
        /// </summary>
        /// <param name="range">延迟时长（秒）。</param>
        public UseDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// 以 range 值作为固定延迟启动计时器。
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
        /// 创建一个计时器，随机部分可以加到固定延迟上，也可以从固定延迟中减去。最终延迟为 fixedDelay 加减 range 内的随机值。
        /// </summary>
        /// <param name="range">加减的最大随机偏移量（秒）。</param>
        /// <param name="fixedDelay">基础延迟（秒）。</param>
        public NegativeDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// 以 50% 的概率加或减随机部分来启动计时器。
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
        /// 创建一个等待 0 到给定范围之间随机时长的计时器。
        /// </summary>
        /// <param name="range">最大延迟时长（秒）。</param>
        public SimpleDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// 以 0 到 range 之间的随机延迟启动计时器。
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
        /// 创建一个 AI 角色使用的冷却计时器。支持强制重新开始冷却或提前结束冷却。
        /// </summary>
        /// <param name="range">冷却时间的最大随机部分（秒）。</param>
        /// <param name="fixedDelay">基础冷却时间（秒）。</param>
        public AIUseDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// 以 0 到 range 之间的随机时长启动冷却。
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = Random.value * range;
        }

        /// <summary>
        /// AI 冷却计时器前进。冷却完成返回 1，正在倒计时返回 0，空闲或刚完成返回 -1，强制冷却进行中返回 -2。
        /// </summary>
        /// <param name="dt">自上次更新以来经过的时间（秒）。</param>
        /// <returns>1 = 冷却完成，0 = 正在倒计时，-1 = 空闲，-2 = 强制冷却进行中。</returns>
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
        /// 强制冷却以完整的固定延迟时长重新开始。当 AI 执行了需要触发冷却的动作时调用此方法。
        /// </summary>
        public void UseIt()
        {
            delta = -1f - fixedDelay;
        }

        /// <summary>
        /// 将冷却时间缩短固定延迟的一半，使 AI 能更快地再次行动。
        /// </summary>
        public void SkipIt()
        {
            delta = -1f - fixedDelay * 0.5f;
        }
    }
}
