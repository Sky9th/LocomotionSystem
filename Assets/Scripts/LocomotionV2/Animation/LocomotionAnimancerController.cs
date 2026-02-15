using UnityEngine;
using Animancer;
using Animancer.TransitionLibraries;

namespace Game.Locomotion.Animation
{
    /// <summary>
    /// Animancer-based presentation controller for LocomotionAgent.
    ///
    /// Consumes SPlayerLocomotion snapshots from the agent and drives
    /// basic Idle/Walk/Run/Sprint animation states. More advanced
    /// behaviour such as turn-in-place and airborne handling will be
    /// added later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocomotionAnimancerController
    {
        [Header("Dependencies")]
        private AnimancerComponent animancer;

        public AnimancerComponent Animancer => animancer;
        private AnimancerStringProfile animancerStringProfile;

        public void Initialize(AnimancerComponent animancer, AnimancerStringProfile animancerStringProfile)
        {
            this.animancer = animancer;
            this.animancerStringProfile = animancerStringProfile;
        }

        public void PlayLocomotionState(SPlayerLocomotion snapshot)
        {
            animancer.TryPlay(animancerStringProfile.idleL);
        }
    }
}