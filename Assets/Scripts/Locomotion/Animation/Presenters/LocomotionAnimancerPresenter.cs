using UnityEngine;
using Animancer;
using Game.Locomotion.Agent;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Animation.Layers;
using Game.Locomotion.Animation.Layers.Base;
using Game.Locomotion.Config;
using System.Collections.Generic;

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
        [SerializeField] private LocomotionAgent agent;
        [SerializeField] private NamedAnimancerComponent animancer;
        [SerializeField] private AnimancerStringProfile animancerStringProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;

        [Header("Head Look")]
        [SerializeField] private AvatarMask headMask;

        [Header("Footsteps")]
        [SerializeField] private AvatarMask footMask;

        private LocomotionAnimationController controller;

        public IReadOnlyDictionary<string, SLocomotionAnimationLayerSnapshot> AnimationSnapshots
        {
            get
            {
                if (controller == null)
                {
                    return null;
                }

                return controller.AnimationSnapshots;
            }
        }

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<LocomotionAgent>();
            }

            if (animancer == null)
            {
                animancer = GetComponentInChildren<NamedAnimancerComponent>();
            }

            if (animancer != null && animancerStringProfile != null && animationProfile != null && agent != null && agent.Profile != null)
            {
                // Ensure the graph has enough layers for: base (0), head (1), footsteps (2).
                animancer.Layers.SetMinCount(3);
                AnimancerLayer baseLayer = animancer.Layers[0];

                AnimancerLayer headLayer = animancer.Layers[1];
                headLayer.Mask = headMask;
    
                AnimancerLayer footLayer = animancer.Layers[2];
                footLayer.Mask = footMask;

                controller = new LocomotionAnimationController(
                    animancer,
                    animancerStringProfile,
                    agent.Profile,
                    animationProfile,
                    agent,
                    new BaseLayer(baseLayer),
                    new HeadLookLayer(headLayer),
                    new FootLayer(footLayer));
            }
        }

        private void Update()
        {
            if (controller == null || agent == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            SLocomotion snapshot = agent.Snapshot;
            controller.UpdateAnimations(snapshot, deltaTime);
        }
    }
}
