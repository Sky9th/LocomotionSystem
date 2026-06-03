using RedDust.Character;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    internal abstract class PassiveGainRule : CharacterStatRule
    {
        private float pending;

        public void Gain(float amount) => pending += amount;

        internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
        {
            if (pending <= 0) return;
            stats.Get(TargetPath())?.Modify(pending);
            pending = 0;
        }

        protected abstract string TargetPath();
    }
}
