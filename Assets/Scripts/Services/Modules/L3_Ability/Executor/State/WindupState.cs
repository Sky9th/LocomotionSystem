using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ③ 前摇计时。等待 windupDuration / animationSpeed，前摇结束后进 CooldownState。
    /// 对应 AbilityActivationSO 的 Windup 阶段。Fire 和 Recovery 分别由 ExecutionState / RecoveryState 负责。
    ///
    /// 设计决策：CD 在前摇后而非 Cost commit 点。
    ///   理由：PvE 生存游戏，被丧尸打断=已受伤+已扣体力，不应再加技能锁。
    ///   Cost（体力/弹药）已是主要 spam 防线。尸潮被围时频繁放技能，前摇被断无 CD 惩罚更公平。
    ///
    /// 公式：实际前摇 = windupDuration / animationSpeed（speed=1.0 为基准）。
    /// canCancelWindup=false 时 CanBeInterrupted 返回 false（前摇霸体）。
    /// </summary>
    public class WindupState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Windup;

        private float _windupDuration;
        private float _elapsed;

        public override void OnEnter(ref SActiveAbilityContext ctx)
        {
            var active = ctx.Ability as ActiveAbilitySO;
            var activation = active?.activation;
            if (activation != null && activation.windupDuration > 0f)
            {
                float speed = activation.animationSpeed > 0f ? activation.animationSpeed : 1f;
                _windupDuration = activation.windupDuration / speed;
            }
            else
            {
                _windupDuration = 0f;
            }
            _elapsed = 0f;

            ctx.Executor.SubmitAbilityAnimation(activation);
        }

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            if (ctx.SkipAnim || _windupDuration <= 0f)
                return new CooldownState();

            if (ctx.Executor.IsAnimationActive)
                return ctx.Executor.IsAnimationFireMarkerReached() ? new CooldownState() : this;

            _elapsed += dt;
            if (_elapsed < _windupDuration)
                return this;

            return new CooldownState();
        }

        public override bool CanBeInterrupted(ref SActiveAbilityContext ctx)
            => (ctx.Ability as ActiveAbilitySO)?.activation?.canCancelWindup ?? true;

        public override void OnInterrupted(ref SActiveAbilityContext ctx)
        {
            ctx.Executor.ReleaseAbilityAnimation();
        }
    }
}
