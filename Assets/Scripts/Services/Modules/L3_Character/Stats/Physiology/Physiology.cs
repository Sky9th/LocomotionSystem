using RedDust.Character;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    /// <summary>
    /// 生理规则抽象根。代表角色身体的固有规律——物理/生理层面的持久化行为，
    /// 与 Buff（外部施加的临时影响）有本质区别。帧驱动，永久生效。
    /// </summary>
    internal abstract class Physiology
    {
        internal abstract void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt);
    }
}
