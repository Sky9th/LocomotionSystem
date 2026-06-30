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
        public ActiveAbilitySO Ability;
        public AbilityExecutor Executor;
        public Entity WeaponEntity;
        public Vector3 Origin;
        public Vector3 Direction;

        public List<GameObject> Targets;
        public List<SDamageInfo> Hits;
    }
}
