using RedDust.Stats;
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
        [Tooltip("消耗的属性定义。拖入 StatDefinitionSO 资产。")]
        public StatDefinitionSO statDef;

        [Tooltip("消耗量。正=扣减, 负=恢复。")]
        public float amount;
    }
}
