using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 射线搜索。Raycast + 近线目标检测。手枪、步枪。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Search/Ray", fileName = "Search_Ray_")]
    public sealed class RaySearchSO : AbilitySearchSO
    {
        [Header("Ray")]
        [Tooltip("是否需要视线。开启则目标与攻击者之间不能有遮挡。")]
        public bool requiresLineOfSight;

        private void OnEnable()
        {
            searchType = ESearchType.RayLine;
        }
    }
}
