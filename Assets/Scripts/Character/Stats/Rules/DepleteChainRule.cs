using Game.Character.Components;
using Game.Stats;

namespace Game.Character.Stats.Rules
{
    internal abstract class DepleteChainRule : CharacterStatRule
    {
        protected abstract string SourcePath();
        protected abstract string TargetPath();
        protected abstract float DamagePerSec();

        internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
        {
            var src = stats.Get(SourcePath());
            var tgt = stats.Get(TargetPath());
            if (src == null || tgt == null) return;

            if (src.Current <= src.Def.Min)
                tgt.Modify(-DamagePerSec() * dt);
        }
    }
}
