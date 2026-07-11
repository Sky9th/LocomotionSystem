using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 标签互斥规则（全局资产，一份）。
    /// exclusionRoots 指定一组父标签，父标签下的所有子标签互相排斥——
    /// 角色不能同时持有同组内的两个标签。
    ///
    /// 门控逻辑：AbilityExecutor 检查技能 abilityTag.Parent 与角色当前 OwnedTags
    /// 是否冲突（前缀匹配）。冲突 → 拒绝激活。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Tag Mutual Exclusion", fileName = "TagMutualExclusion_")]
    public sealed class TagMutualExclusionSO : ScriptableObject
    {
        [Tooltip("互斥根标签。每个父标签下的所有子标签互为排斥。")]
        public RdTagDefSO[] exclusionRoots;
    }
}
