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

        /// <summary>管道空闲——未启动或已到达终态（Completed/Rejected）。</summary>
        public bool IsIdle => _fsm.Current == null
            || _fsm.Current is CompletedState
            || _fsm.Current is RejectedState;

        public SActiveAbilityContext Context => _ctx;
        public float StateTime => _fsm.StateTime;

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
            _ctx = new SActiveAbilityContext
            {
                Ability = ability,
                Executor = executor,
                WeaponEntity = weaponEntity,
                Origin = origin,
                Direction = direction,
            };

            Debug.Log($"[ActivePipeline] Start: {ability.internalName} | origin={origin} dir={direction}" +
                      (weaponEntity != null ? $" | weapon={weaponEntity.Preset.name}" : ""));

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

            var id = (_fsm.Current as AbilityPipelineState)?.Id;
            Debug.Log($"[ActivePipeline] {(prev as AbilityPipelineState)?.Id} → {id}");

            switch (id)
            {
                case EActiveAbilityState.Completed:
                    Debug.Log($"[ActivePipeline] ✅ Pipeline completed | targets={_ctx.Targets?.Count ?? 0} | hits={_ctx.Hits?.Count ?? 0}");
                    break;
                case EActiveAbilityState.Rejected:
                    Debug.LogWarning($"[ActivePipeline] ❌ Pipeline rejected | ability={_ctx.Ability.internalName}");
                    break;
            }
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
