using Game.Character.Components;
using Game.Stats;

namespace Game.Character.Stats.Rules
{
    internal abstract class BatchDamageRule : CharacterStatRule
    {
        private float pending;

        public void Add(float amount) => pending += amount;

        protected abstract string TargetPath();

        internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
        {
            if (pending <= 0) return;
            stats.Get(TargetPath())?.Modify(-pending);
            pending = 0;
        }
    }
}
