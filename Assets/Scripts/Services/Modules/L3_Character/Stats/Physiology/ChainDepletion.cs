using RedDust.Character;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    /// <summary>
    /// 连锁枯竭。当来源属性耗尽时，目标属性持续受伤。
    /// 例如：饥饿归零→扣血。
    /// </summary>
    internal abstract class ChainDepletion : Physiology
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
