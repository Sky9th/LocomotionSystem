using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 标签互斥规则（全局资产，一份）。
    /// exclusionRoots 指定一组父标签，父标签下的所有子标签互相排斥——
    /// 角色不能同时持有同组内的两个标签。
    ///
    /// 门控逻辑：CombatComponent 检查技能 selfTag 与角色当前 OwnedTags
    /// 是否在同一个互斥组内。是 → 拒绝激活。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Tag Mutual Exclusion", fileName = "TagMutualExclusion")]
    public sealed class TagMutualExclusionSO : ScriptableObject
    {
        [Tooltip("互斥根标签。每个父标签下的所有子标签互为排斥。")]
        public GameplayTagDefinitionSO[] exclusionRoots;
    }
}
