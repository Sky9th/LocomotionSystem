using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 扇形搜索。OverlapSphere + 前方角度过滤。横斩、霰弹。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Search/Cone", fileName = "Search_Cone_")]
    public sealed class ConeSearchSO : AbilitySearchSO
    {
        [Header("Cone")]
        [Range(0f, 360f)]
        [Tooltip("扇形角度（全角）。目标在 angle/2 半角内即命中。")]
        public float angle = 90f;

        private void OnEnable()
        {
            searchType = ESearchType.Cone;
        }
    }
}
