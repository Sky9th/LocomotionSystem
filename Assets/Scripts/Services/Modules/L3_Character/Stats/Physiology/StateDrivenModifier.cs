using RedDust.Character;
using RedDust.Stats;
using UnityEngine;

namespace RedDust.Character.Stats
{
    /// <summary>
    /// 状态驱动修改器。当角色状态满足条件时，自动挂/摘 StatModifier。
    /// 例如：冲刺时耐力消耗倍率提升。
    /// </summary>
    internal abstract class StateDrivenModifier : Physiology
    {
        private readonly StatModifier mod;
        private bool wasActive;

        protected StateDrivenModifier(object owner, StatModifier m)
        {
            mod = m;
            mod.Owner = owner;
        }

        protected abstract bool ShouldActivate(CharacterFrameContext ctx);
        protected abstract string StatPath();

        internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
        {
            var s = stats.Get(StatPath());
            if (s == null) return;

            bool active = ShouldActivate(ctx);
            if (active && !wasActive)
            {
                s.AddModifier(mod);
                wasActive = true;
                Debug.Log($"[Physiology] {GetType().Name}: modifier ON at {StatPath()}");
            }
            if (!active && wasActive)
            {
                s.RemoveByOwner(mod.Owner);
                wasActive = false;
                Debug.Log($"[Physiology] {GetType().Name}: modifier OFF at {StatPath()}");
            }
        }
    }
}
