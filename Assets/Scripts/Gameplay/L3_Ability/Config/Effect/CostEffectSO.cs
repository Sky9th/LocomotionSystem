using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 资源消耗/恢复效果。正=消耗, 负=恢复。
    ///
    /// TODO: 当前仅支持绝对值扣除（amount=50 → 固定扣 50）。
    /// 常规消耗模式缺失：
    ///   - 当前值百分比（消耗 10% 生命）
    ///   - 最大值百分比（消耗 20% 法力上限）
    ///   - 混合模式（固定 + 百分比）
    /// 需增加 ECostMode 枚举 (Absolute / PercentCurrent / PercentMax) 和 mode 字段。
    /// 特殊/动态消耗仍走 ApplyCostCallback 回调处理。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Cost", fileName = "CostEffect_")]
    public sealed class CostEffectSO : EffectSO
    {
        [Header("Cost")]
        [Tooltip("消耗的属性定义。拖入 PropertyDefSO 资产。")]
        public PropertyDefSO def;

        [Tooltip("消耗量。正=扣减, 负=恢复。")]
        public float amount;
    }
}
