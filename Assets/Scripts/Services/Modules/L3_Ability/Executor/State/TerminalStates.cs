namespace RedDust.Ability
{
    /// <summary>空闲终态。永远返回自身。</summary>
    public class IdleState : AbilityState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Idle;
        public override IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt) => this;
    }

    /// <summary>拒绝终态。永远返回自身。</summary>
    public class RejectedState : AbilityState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Rejected;
        public override IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt) => this;
    }

    /// <summary>完成终态。永远返回自身。</summary>
    public class CompletedState : AbilityState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Completed;
        public override IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt) => this;
    }
}
