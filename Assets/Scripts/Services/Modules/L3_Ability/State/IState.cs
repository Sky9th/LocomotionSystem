namespace RedDust.Ability
{
    /// <summary>
    /// 状态机中的单个状态。TContext 为状态共享上下文，ref 传递保证 struct 场景零拷贝可变。
    ///
    /// [MARK] 零领域依赖。若有其他模块需要状态机，可提至 Shared/。
    ///
    /// 流转规则：
    ///   主动：OnTick 返回 != this → CanExit(当前) + CanEnter(下一) → OnExit → OnEnter
    ///   打断：Interrupt(target) → CanBeInterrupted(当前) → OnInterrupted → 强制切到 target
    /// </summary>
    public interface IState<TContext>
    {
        // ── 主动流转钩子 ──

        /// <summary>流转进入此 State 前，状态机调用。false = 拒绝进入。</summary>
        bool CanEnter(ref TContext ctx);

        /// <summary>流转离开此 State 前，状态机调用。false = 留在当前。</summary>
        bool CanExit(ref TContext ctx);

        // ── 打断钩子 ──

        /// <summary>外部 Interrupt 时，状态机先调此方法。false = 拒绝打断（霸体/坚韧）。</summary>
        bool CanBeInterrupted(ref TContext ctx);

        /// <summary>被打断时回调。做清理——取消动画、移除临时 Tag 等。</summary>
        void OnInterrupted(ref TContext ctx);

        // ── 生命周期 ──

        void OnEnter(ref TContext ctx);
        void OnExit(ref TContext ctx);

        /// <summary>每帧驱动。返回自身 = 留在当前；返回其他 = 请求流转。</summary>
        IState<TContext> OnTick(ref TContext ctx, float dt);
    }
}
