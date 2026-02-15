using UnityEngine;
using Game.Locomotion.Computation;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Encapsulates foot placement state for a locomotion agent.
    ///
    /// Tracks which foot is currently in front along the character's
    /// forward axis and delegates the actual computation to
    /// LocomotionFootPlacement.
    /// </summary>
    internal sealed class LocomotionFootState
    {
        public bool IsLeftFootOnFront { get; private set; } = true;

        public void Reset(bool defaultLeftFootOnFront = true)
        {
            IsLeftFootOnFront = defaultLeftOnFront(defaultLeftFootOnFront);
        }

        public void Update(
            Animator animator,
            Transform modelRoot,
            Transform rootTransform,
            Vector3 locomotionHeading)
        {
            IsLeftFootOnFront = LocomotionFootPlacement.EvaluateIsLeftFootOnFront(
                animator,
                modelRoot,
                rootTransform,
                locomotionHeading,
                IsLeftFootOnFront);
        }

        private static bool defaultLeftOnFront(bool value) => value;
    }
}
