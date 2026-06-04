using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 资源消耗/恢复效果。正=消耗, 负=恢复。多个消耗类型=数组多个 CostEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Cost", fileName = "CostEffect_")]
    public sealed class CostEffectSO : EffectSO
    {
        [Header("Cost")]
        [Tooltip("消耗的资源 stat。引用 GameplayTag 定位 StatInstance。")]
        public GameplayTagDefinitionSO statTag;

        [Tooltip("消耗量。正=扣减, 负=恢复。")]
        public float amount;
    }
}
