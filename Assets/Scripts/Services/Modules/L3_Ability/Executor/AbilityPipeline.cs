using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能管道。持有 <see cref="StateMachine{SActiveAbilityContext}"/>。
    /// 主动和被动技能共用——入口 Start → 每帧 Tick → Completed/Rejected。
    /// </summary>
    public class AbilityPipeline
    {
        private readonly StateMachine<SActiveAbilityContext> _fsm = new();
        private SActiveAbilityContext _ctx;

        /// <summary>未启动或已结束（含 Rejected）。</summary>
        public bool IsIdle => _fsm.Current == null || _fsm.Current is CompletedState || _fsm.Current is RejectedState;

        public SActiveAbilityContext Context => _ctx;
        public float StateTime => _fsm.StateTime;

        public bool Start(
            AbilityInstance instance,
            AbilityExecutor executor,
            Vector3 origin,
            Vector3 direction,
            Entity weaponEntity = null,
            bool skipAnim = false,
            List<GameObject> guaranteedTargets = null)
        {
            if (!IsIdle)
                return false;

            _ctx = new SActiveAbilityContext
            {
                Ability = instance.Definition,
                Instance = instance,
                Executor = executor,
                WeaponEntity = weaponEntity,
                Origin = origin,
                Direction = direction,
                SkipAnim = skipAnim,
                GuaranteedTargets = guaranteedTargets,
            };

            return _fsm.Start(new GatingState(), ref _ctx);
        }

        public void Tick(float dt) => _fsm.Tick(ref _ctx, dt);
    }
}
