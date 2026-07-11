using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 技能目标搜索定义。回答「往哪打」。
    /// 抽象基类——具体搜索形状由子类 SO 定义。
    /// ActiveAbilitySO 持有此引用，AbilityPipeline 消费。
    ///
    /// 对标 UE GAS TargetData：定义搜索形状和过滤规则，不执行搜索本身。
    /// </summary>
    public abstract class AbilitySearchSO : ScriptableObject
    {
        [Header("Common")]
        [Tooltip("搜索类型。子类 OnEnable 自设，不可手动修改。")]
        public ESearchType searchType;

        [Tooltip("搜索距离。Cone=锥长, RayLine=射线长, Circle=半径。")]
        public float range;

        [Tooltip("物理层遮罩。")]
        public LayerMask targetMask = ~0;

        [Tooltip("最大命中目标数。≤0 无限制。")]
        public int maxTargets;

        [Tooltip("目标筛选。Phase 4.2+ 实现。")]
        public ETargetFilter targetFilter;
    }
}
