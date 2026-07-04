using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⑦ 冷却施加。冷却从 Activation 入口开始，与 Fire+Recovery 重叠。
    /// StartCooldown(min=0.05s) 防止帧级连发，CleanupExpiredCooldowns 自动清理。
    /// 单帧穿透 → ExecutionState。
    /// </summary>
    public class CooldownState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Cooldown;

        private const float MinCooldown = 0.05f;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;

            float duration = a.cooldownDuration > 0f ? a.cooldownDuration : MinCooldown;
            e.StartCooldown(a, duration);

            return new ExecutionState();
        }
    }
}
