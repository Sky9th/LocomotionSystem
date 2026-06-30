namespace RedDust.Ability
{
    /// <summary>
    /// 能力 State 基类。在 <see cref="IState{SActiveAbilityContext}"/> 上加 <see cref="Id"/>，
    /// 使外部可通过 <see cref="EActiveAbilityState"/> 枚举判断当前处于哪个步骤。
    ///
    /// [MARK] 领域绑定层。IState / StateMachine 是泛型，此处粘合 SActiveAbilityContext + ActiveAbilityState。
    ///        若其他模块需要不同上下文，参考此模式创建自己的基类。
    /// </summary>
    public abstract class AbilityState : IState<SActiveAbilityContext>
    {
        public abstract EActiveAbilityState Id { get; }

        public virtual bool CanEnter(SActiveAbilityContext ctx) => true;
        public virtual bool CanExit(SActiveAbilityContext ctx) => true;
        public virtual bool CanBeInterrupted(SActiveAbilityContext ctx) => true;
        public virtual void OnInterrupted(SActiveAbilityContext ctx) { }
        public virtual void OnEnter(SActiveAbilityContext ctx) { }
        public virtual void OnExit(SActiveAbilityContext ctx) { }
        public abstract IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt);
    }
}
