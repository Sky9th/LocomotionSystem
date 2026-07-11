namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// Ability Pipeline State 基类。在 <see cref="IState{SActiveAbilityContext}"/> 上加 <see cref="Id"/>，
    /// 使外部可通过 <see cref="EActiveAbilityState"/> 枚举判断当前处于哪个步骤。
    ///
    /// [MARK] Pipeline 通用 State 模式。暂无其他消费者，暂放 L3_Ability；若有其他 Pipeline 可提至 L3 或 Utility。
    /// </summary>
    public abstract class AbilityPipelineState : IState<SActiveAbilityContext>
    {
        public abstract EActiveAbilityState Id { get; }

        public virtual bool CanEnter(ref SActiveAbilityContext ctx) => true;
        public virtual bool CanExit(ref SActiveAbilityContext ctx) => true;
        public virtual bool CanBeInterrupted(ref SActiveAbilityContext ctx) => true;
        public virtual void OnInterrupted(ref SActiveAbilityContext ctx) { }
        public virtual void OnEnter(ref SActiveAbilityContext ctx) { }
        public virtual void OnExit(ref SActiveAbilityContext ctx) { }
        public abstract IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt);
    }
}
