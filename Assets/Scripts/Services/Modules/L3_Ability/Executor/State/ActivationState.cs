using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ③ 动画激活。Windup 前摇——启动 AnimationClip，等待 windupDuration / animationSpeed。
    /// 前摇结束后冷却开始（→ CooldownState），与后续 Fire + Recovery 重叠。
    ///
    /// TODO: Windup 计时 — OnEnter 读 windupDuration / animationSpeed，OnTick 累时穿透
    /// 当前: 单帧透传占位。
    /// 通过 → CooldownState。
    /// </summary>
    public class ActivationState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Activation;

        // TODO: float _windupDuration; float _elapsed; OnEnter/OnTick 计时模式
        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            Debug.Log($"[Activation] ③ {ctx.Ability.internalName} — TODO: windup timer → Cooldown");
            return new CooldownState();
        }
    }
}
