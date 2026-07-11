using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 陷阱实体。通过 OnTriggerEnter → NotifyPassiveEvent 触发被动技能管线。
    ///
    /// 使用方式：
    /// 1. GameObject 挂 Trap + AbilityExecutor + Collider(isTrigger)
    /// 2. 被动技能的 AbilitySearchSO.targetMask 控制触发层
    /// 3. AbilityExecutor 通过 AbilityForest + SyncInstances 配置被动（trigger = OnEnterArea）
    /// 4. 实体进入 → OnTriggerEnter → NotifyPassiveEvent → Pipeline FSM
    /// </summary>
    [RequireComponent(typeof(AbilityExecutor))]
    public sealed class Trap : MonoBehaviour
    {
        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }
    }
}
