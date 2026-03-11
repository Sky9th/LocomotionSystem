using UnityEngine;
using Animancer;
using Game.Locomotion.Agent;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Animation.Layers;
using Game.Locomotion.Animation.Layers.Base;
using System.Collections.Generic;
using Game.Locomotion.Motor;

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
        [SerializeField] private LocomotionAliasProfile animancerStringProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;

        [Header("Root Motion")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool forwardRootMotion = true;
        [SerializeField] private bool applyRootMotionPlanarPositionOnly = true;

        [Header("Head Look")]
        [SerializeField] private AvatarMask headMask;

        [Header("Footsteps")]
        [SerializeField] private AvatarMask footMask;

        private LocomotionAnimationController controller;

        private SLocomotionAnimation lastAnimation;

        public SLocomotionAnimation LastAnimation => lastAnimation;

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

        private void Start()
        {
            if (agent == null)
            {
                agent = GetComponentInParent<LocomotionAgent>();
            }

            if (animancer == null)
            {
                animancer = GetComponent<NamedAnimancerComponent>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            LocomotionMotor motor = agent != null ? agent.Motor : null;
            if (animancer != null && animancerStringProfile != null && animationProfile != null && agent != null && agent.Profile != null && motor != null)
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
                    motor,
                    new BaseLayer(baseLayer),
                    new HeadLookLayer(headLayer),
                    new FootLayer(footLayer));
            }
        }

        private void OnAnimatorMove()
        {
            if (!forwardRootMotion)
            {
                return;
            }

            if (animator == null || agent == null)
            {
                return;
            }

            LocomotionMotor motor = agent.Motor;
            if (motor == null)
            {
                return;
            }

            Vector3 deltaPosition = animator.deltaPosition;
            Quaternion deltaRotation = animator.deltaRotation;

            motor.ApplyDeltaPosition(deltaPosition);
            motor.ApplyDeltaRotation(deltaRotation);
        }

        /// <summary>
        /// Evaluates locomotion animations for the given snapshot and returns the animation output.
        /// Intended to be called by <see cref="LocomotionAgent"/> so the agent can publish a single merged snapshot.
        /// </summary>
        public SLocomotionAnimation Evaluate(in SLocomotion snapshot, float deltaTime)
        {
            if (controller == null)
            {
                lastAnimation = default;
                return lastAnimation;
            }

            controller.UpdateAnimations(snapshot, deltaTime);
            lastAnimation = BuildAnimationSnapshot(controller.AnimationSnapshots);

            return lastAnimation;
        }

        private static SLocomotionAnimation BuildAnimationSnapshot(
            IReadOnlyDictionary<string, SLocomotionAnimationLayerSnapshot> layerSnapshots)
        {
            if (layerSnapshots == null)
            {
                return default;
            }

            SLocomotionAnimationLayerSnapshot baseLayer = default;
            SLocomotionAnimationLayerSnapshot headLookLayer = default;
            SLocomotionAnimationLayerSnapshot footstepLayer = default;

            // Keep this mapping minimal and stable; layer names are owned by the layers.
            if (layerSnapshots.TryGetValue("BaseLocomotion", out var baseSnapshot))
            {
                baseLayer = baseSnapshot;
            }

            if (layerSnapshots.TryGetValue("HeadLook", out var headSnapshot))
            {
                headLookLayer = headSnapshot;
            }

            if (layerSnapshots.TryGetValue("Footstep", out var footSnapshot))
            {
                footstepLayer = footSnapshot;
            }

            return new SLocomotionAnimation(baseLayer, headLookLayer, footstepLayer);
        }
    }
}
