namespace RedDust.Ability
{
    /// <summary>
    /// 泛型状态机。只负责流转——不关心 TState 是什么、TContext 里有什么。
    ///
    /// [MARK] 零领域依赖。若有其他模块需要状态机，可提至 Shared/。
    ///
    /// 主动流转：Tick → OnTick 返回下一站 → CanExit + CanEnter 双验证 → Transition
    /// 外部打断：Interrupt → CanBeInterrupted → OnInterrupted → 强制 Transition
    /// </summary>
    public class StateMachine<TContext>
    {
        private IState<TContext> _current;

        /// <summary>当前状态。</summary>
        public IState<TContext> Current => _current;

        /// <summary>上一个状态。首次 Start 时为 null。</summary>
        public IState<TContext> Previous { get; private set; }

        /// <summary>进入当前状态后累计秒数。</summary>
        public float StateTime { get; private set; }

        /// <summary>
        /// 启动状态机。调 first.CanEnter 验证，失败返回 false。
        /// </summary>
        public bool Start(IState<TContext> first, TContext ctx)
        {
            if (first == null || !first.CanEnter(ctx)) return false;
            _current = first;
            StateTime = 0f;
            _current.OnEnter(ctx);
            return true;
        }

        /// <summary>
        /// 逐帧驱动。当前 State 的 OnTick 返回新 State 时触发流转检查。
        /// </summary>
        public void Tick(TContext ctx, float dt)
        {
            if (_current == null) return;

            var next = _current.OnTick(ctx, dt);
            if (next != null && next != _current && _current.CanExit(ctx) && next.CanEnter(ctx))
            {
                Transition(next, ctx);
            }
            StateTime += dt;
        }

        /// <summary>
        /// 外部强行打断当前 State。CanBeInterrupted 是唯一闸门。
        /// 打断后走正常 OnExit → OnEnter 生命周期。
        /// </summary>
        public bool Interrupt(IState<TContext> target, TContext ctx)
        {
            if (_current == null || target == null) return false;
            if (!_current.CanBeInterrupted(ctx)) return false;

            _current.OnInterrupted(ctx);
            Transition(target, ctx);
            return true;
        }

        private void Transition(IState<TContext> next, TContext ctx)
        {
            _current.OnExit(ctx);
            Previous = _current;
            _current = next;
            StateTime = 0f;
            _current.OnEnter(ctx);
        }
    }
}
