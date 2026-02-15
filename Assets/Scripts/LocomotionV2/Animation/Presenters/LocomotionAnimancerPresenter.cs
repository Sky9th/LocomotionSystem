using UnityEngine;
using Animancer;
using Animancer.TransitionLibraries;
using Game.Locomotion.Agent;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Animation.Layers;

namespace Game.Locomotion.Animation.Presenters
{
    /// <summary>
    /// MonoBehaviour bridge between a LocomotionAgent and the
    /// Animancer-based locomotion animation controller.
    ///
    /// Reads the latest SPlayerLocomotion snapshot from the agent
    /// each frame and forwards it to the animation controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocomotionAnimancerPresenter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Game.Locomotion.Agent.LocomotionAgent agent;
        [SerializeField] private NamedAnimancerComponent animancer;
        [SerializeField] private AnimancerStringProfile animancerStringProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;

        [Header("Head Look")]
        [SerializeField] private AvatarMask headMask;

        private LocomotionAnimationController controller;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<Game.Locomotion.Agent.LocomotionAgent>();
            }

            if (animancer == null)
            {
                animancer = GetComponentInChildren<NamedAnimancerComponent>();
            }

            if (animancer != null && animancerStringProfile != null && animationProfile != null)
            {
                // Configure the dedicated head layer (index 1) to use the
                // supplied AvatarMask so head look only affects the upper body.
                if (headMask != null)
                {
                    AnimancerLayer headLayer = animancer.Layers[1];
                    headLayer.Mask = headMask;
                }

                controller = new LocomotionAnimationController(
                    animancer,
                    animancerStringProfile,
                    animationProfile,
                    new BaseLocomotionLayer(),
                    new HeadLookLayer());
            }
        }

        private void Update()
        {
            if (controller == null || agent == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            controller.UpdateAnimations(agent.Snapshot, deltaTime);
        }
    }
}
