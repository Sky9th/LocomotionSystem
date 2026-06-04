using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 圆形搜索。OverlapSphere 自身周围。旋风斩、战吼、光环。Phase 4.2+ 实现。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Search/Circle", fileName = "Search_Circle_")]
    public sealed class CircleSearchSO : SkillSearchSO
    {
        // Circle 无额外字段，仅使用基类的 range / targetMask / maxTargets / targetFilter

        private void OnEnable()
        {
            searchType = ECombatSearchType.Circle;
        }
    }
}
