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

            if (_recoveryDuration > 0f)
                Debug.Log($"[Recovery] Enter: {ctx.Ability.internalName} duration={_recoveryDuration:F2}s (raw={activation.recoveryDuration:F2}s / speed={activation.animationSpeed:F2})");
        }

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            if (_recoveryDuration <= 0f)
            {
                Debug.Log($"[Recovery] No recovery → Completed");
                return new CompletedState();
            }

            _elapsed += dt;
            if (_elapsed < _recoveryDuration)
                return this;

            Debug.Log($"[Recovery] Done: {ctx.Ability.internalName} {_elapsed:F1}s → Completed");
            return new CompletedState();
        }

        public override bool CanBeInterrupted(ref SActiveAbilityContext ctx)
        {
            bool canCancel = ctx.Ability.activation?.canCancelRecovery ?? true;
            if (!canCancel)
                Debug.Log($"[Recovery] Interrupt denied: canCancelRecovery=false");
            return canCancel;
        }
    }
}
