using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 斩杀效果。目标 HP 低于阈值时即死。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Effect/Execute", fileName = "GE_Execute_")]
    public sealed class ExecuteEffectSO : GameplayEffectSO
    {
        [Header("Execute")]
        [Range(0f, 1f)]
        [Tooltip("HP 阈值。目标当前 HP% 低于此值时即死。")]
        public float hpThreshold;
    }
}
