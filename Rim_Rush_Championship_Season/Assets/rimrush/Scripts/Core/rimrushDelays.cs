// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushDelays 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

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
        /// Executes Full Delay for the FullDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="range">Input value used by this step of the workflow.</param>
        /// <param name="fixedDelay">Input value used by this step of the workflow.</param>
        public FullDelay(float range, float fixedDelay = 0f)
        {
            this.range = range;
            this.fixedDelay = fixedDelay;
        }

        /// <summary>
        /// Executes Activate for the FullDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public virtual void Activate()
        {
            delta = 0f;
            dispersion = Random.value * range;
            delay = fixedDelay + dispersion;
        }

        /// <summary>
        /// Executes Update for the FullDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Reset for the FullDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public virtual void Reset()
        {
            delta = -1f;
        }
    }

    public sealed class UseDelay : FullDelay
    {
        /// <summary>
        /// Executes Use Delay for the UseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="range">Input value used by this step of the workflow.</param>
        public UseDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Executes Activate for the UseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Negative Delay for the NegativeDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="range">Input value used by this step of the workflow.</param>
        /// <param name="fixedDelay">Input value used by this step of the workflow.</param>
        public NegativeDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Executes Activate for the NegativeDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Simple Delay for the SimpleDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="range">Input value used by this step of the workflow.</param>
        public SimpleDelay(float range)
            : base(range)
        {
        }

        /// <summary>
        /// Executes Activate for the SimpleDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes AIUse Delay for the AIUseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="range">Input value used by this step of the workflow.</param>
        /// <param name="fixedDelay">Input value used by this step of the workflow.</param>
        public AIUseDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        /// <summary>
        /// Executes Activate for the AIUseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public override void Activate()
        {
            delta = 0f;
            delay = Random.value * range;
        }

        /// <summary>
        /// Executes Update for the AIUseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Use It for the AIUseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void UseIt()
        {
            delta = -1f - fixedDelay;
        }

        /// <summary>
        /// Executes Skip It for the AIUseDelay workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void SkipIt()
        {
            delta = -1f - fixedDelay * 0.5f;
        }
    }
}
