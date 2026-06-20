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
        public ClipTransition climbUpHalfMeter;
        public ClipTransition climbUp1meter;
        public ClipTransition climbUp2meter;

        [Header("Climb Down")]
        public ClipTransition climbDown1meter;
        public ClipTransition climbDown2meter;

        [Header("Land")]
        public ClipTransition landFromWall;
    }
}
