using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道执行上下文。Start 时填充，管道结束后外部可读取全貌。
    /// struct + ref 传递：零 GC，State 间通过 ref 共享可变状态。
    /// </summary>
    public struct SActiveAbilityContext
    {
        /// <summary>技能数据定义（ActiveAbilitySO 或 PassiveAbilitySO）。</summary>
        public AbilitySO Ability;

        /// <summary>本次执行的技能实例。用于副作用溯源。</summary>
        public AbilityInstance Instance;

        public AbilityExecutor Executor;
        public Entity WeaponEntity;
        public Vector3 Origin;
        public Vector3 Direction;

        /// <summary>跳过动画阶段（Windup/Recovery）。被动技能使用。</summary>
        public bool SkipAnim;

        /// <summary>保证命中的目标（事件被动目标/点选目标等）。与物理查询结果合并去重。</summary>
        public List<GameObject> GuaranteedTargets;

        public List<GameObject> Targets;
        public List<SDamageInfo> Hits;
    }
}
