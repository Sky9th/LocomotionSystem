using Animancer;
using UnityEngine;

namespace RedDust.Character.Animation
{
    /// <summary>
    /// 攀爬/穿越动画引用集合。TraversalDriver 专用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "TraversalAnimationSetSO",
        menuName = "RedDust/Animation/Traversal/Traversal Animation Set")]
    public sealed class TraversalAnimationSetSO : ScriptableObject
    {
        [Header("Climb Up")]
        public StringAsset climbUpHalfMeter;
        public StringAsset climbUp1meter;
        public StringAsset climbUp2meter;

        [Header("Climb Down")]
        public StringAsset climbDown1meter;
        public StringAsset climbDown2meter;

        [Header("Land")]
        public StringAsset landFromWall;
    }
}
