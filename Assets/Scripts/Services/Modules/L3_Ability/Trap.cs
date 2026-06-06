using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 陷阱实体。通过 TargetFilterCallback 决定哪些实体触发哪些被动。
    ///
    /// 使用方式：
    /// 1. GameObject 挂 Trap + AbilityExecutor + Collider(isTrigger)
    /// 2. 设置 targetLayers（如 Character）
    /// 3. AbilityExecutor 配置 PassiveAbilitySO（trigger = OnEnterArea）
    /// 4. 实体进入 → OnTriggerEnter → 匹配被动 → TargetFilterCallback(passive, target) → 层检查
    /// </summary>
    [RequireComponent(typeof(AbilityExecutor))]
    public sealed class Trap : MonoBehaviour
    {
        [Header("Filter")]
        [Tooltip("只有这些层的实体才会触发陷阱。")]
        [SerializeField] private LayerMask targetLayers;

        private AbilityExecutor ability;

        private void Awake()
        {
            ability = GetComponent<AbilityExecutor>();

            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            ability.TargetFilterCallback = ShouldTrigger;
        }

        private string ShouldTrigger(PassiveAbilitySO passive, GameObject target)
        {
            if (target == null) return "NullTarget";

            int targetLayer = 1 << target.layer;
            if ((targetLayers.value & targetLayer) == 0)
                return $"Layer({LayerMask.LayerToName(target.layer)})";

            return null;
        }
    }
}
