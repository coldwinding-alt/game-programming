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

        public FullDelay(float range, float fixedDelay = 0f)
        {
            this.range = range;
            this.fixedDelay = fixedDelay;
        }

        public virtual void Activate()
        {
            delta = 0f;
            dispersion = Random.value * range;
            delay = fixedDelay + dispersion;
        }

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

        public virtual void Reset()
        {
            delta = -1f;
        }
    }

    public sealed class UseDelay : FullDelay
    {
        public UseDelay(float range)
            : base(range)
        {
        }

        public override void Activate()
        {
            delta = 0f;
            delay = range;
        }
    }

    public sealed class NegativeDelay : FullDelay
    {
        public NegativeDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        public override void Activate()
        {
            base.Activate();
            var sign = Random.value <= 0.5f ? -1f : 1f;
            delay = fixedDelay + sign * dispersion;
        }
    }

    public sealed class SimpleDelay : FullDelay
    {
        public SimpleDelay(float range)
            : base(range)
        {
        }

        public override void Activate()
        {
            delta = 0f;
            delay = range * Random.value;
        }
    }

    public sealed class AIUseDelay : FullDelay
    {
        public AIUseDelay(float range, float fixedDelay = 0f)
            : base(range, fixedDelay)
        {
        }

        public override void Activate()
        {
            delta = 0f;
            delay = Random.value * range;
        }

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

        public void UseIt()
        {
            delta = -1f - fixedDelay;
        }

        public void SkipIt()
        {
            delta = -1f - fixedDelay * 0.5f;
        }
    }
}
