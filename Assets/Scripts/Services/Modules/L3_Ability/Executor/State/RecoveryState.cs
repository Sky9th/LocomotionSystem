using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⑧ 后摇等待。动画收尾段——刀收回/枪回落/手势收拢。
    /// 公式：实际后摇 = recoveryDuration / animationSpeed（speed=1.0 为基准）。
    /// canCancelRecovery=false 时 CanBeInterrupted 返回 false（后摇霸体）。
    /// 通过 → CompletedState。
    /// </summary>
    public class RecoveryState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Recovery;

        private float _recoveryDuration;
        private float _elapsed;

        public override void OnEnter(ref SActiveAbilityContext ctx)
        {
            var activation = ctx.Ability.activation;
            if (activation != null && activation.recoveryDuration > 0f)
            {
                float speed = activation.animationSpeed > 0f ? activation.animationSpeed : 1f;
                _recoveryDuration = activation.recoveryDuration / speed;
            }
            else
            {
                _recoveryDuration = 0f;
            }
            _elapsed = 0f;
        }

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            if (ctx.Executor.IsAnimationActive)
                return ctx.Executor.IsAnimationClipFinished() ? new CompletedState() : this;

            if (_recoveryDuration <= 0f)
                return new CompletedState();

            _elapsed += dt;
            if (_elapsed < _recoveryDuration)
                return this;

            return new CompletedState();
        }

        public override bool CanBeInterrupted(ref SActiveAbilityContext ctx)
            => ctx.Ability.activation?.canCancelRecovery ?? true;

        public override void OnInterrupted(ref SActiveAbilityContext ctx)
        {
            ctx.Executor.ReleaseAbilityAnimation();
        }
    }
}
