using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道。持有 <see cref="StateMachine{SActiveAbilityContext}"/>，
    /// 入口 Start → 每帧 Tick → Completed/Rejected。
    /// 公共 API 不暴露 IState——外部通过 Context.State（enum）判断当前阶段。
    /// </summary>
    public class ActiveAbilityPipeline
    {
        private readonly StateMachine<SActiveAbilityContext> _fsm = new();
        private SActiveAbilityContext _ctx;

        // State 链由每个 State 内部决定下一站，Pipeline 不组装。

        /// <summary>管道空闲——未启动或已完成。Rejected 不算空闲（避免被输入重试死循环）。</summary>
        public bool IsIdle => _fsm.Current == null || _fsm.Current is CompletedState;

        public SActiveAbilityContext Context => _ctx;
        public float StateTime => _fsm.StateTime;

        // TODO: UI 查询 API — 仅服务于 UI 层轮询管道状态，后续提取到只读接口。
        /// <summary>当前管道状态枚举。管道空闲时返回 Completed。</summary>
        public EActiveAbilityState CurrentState =>
            (_fsm.Current as AbilityPipelineState)?.Id ?? EActiveAbilityState.Completed;

        /// <summary>
        /// 启动管道。从 Gating 状态开始。
        /// </summary>
        public bool Start(
            ActiveAbilitySO ability,
            AbilityExecutor executor,
            Vector3 origin,
            Vector3 direction,
            Entity weaponEntity = null)
        {
            if (!IsIdle && _fsm.Current is not RejectedState)
                return false;

            _ctx = new SActiveAbilityContext
            {
                Ability = ability,
                Executor = executor,
                WeaponEntity = weaponEntity,
                Origin = origin,
                Direction = direction,
            };

            return _fsm.Start(new GatingState(), ref _ctx);
        }

        /// <summary>
        /// 逐帧驱动。检测终态并打印日志。
        /// </summary>
        public void Tick(float dt)
        {
            var prev = _fsm.Current;
            _fsm.Tick(ref _ctx, dt);

            if (_fsm.Current == prev) return;
        }

        /// <summary>
        /// 外部打断（被击/沉默/玩家取消）。
        /// </summary>
        public bool Interrupt(IState<SActiveAbilityContext> target)
        {
            return _fsm.Interrupt(target, ref _ctx);
        }
    }
}
