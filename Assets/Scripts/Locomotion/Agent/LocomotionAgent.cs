using System;
using UnityEngine;
using Game.Locomotion.Computation;
using Game.Locomotion.Discrete.Coordination;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Input;
using Game.Locomotion.Config;
using Game.Locomotion.Animation.Presenters;

namespace Game.Locomotion.Agent
{
    /// <summary>
    /// Entry point for the new locomotion pipeline.
    ///
    /// LocomotionAgentV2 coordinates input, logic and computation modules
    /// and exposes an immutable locomotion snapshot for other systems.
    /// In the current phase it provides only a minimal skeleton without
    /// full locomotion computation or animation integration.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class LocomotionAgent : MonoBehaviour, ILocomotionModelRotator
    {
        [Header("Rig References")]
        [SerializeField] private Transform followAnchor;
        [SerializeField] private Transform modelRoot;

        [Header("Config")]
        [SerializeField] private LocomotionProfile locomotionProfile;
        [SerializeField, Min(0f)] private float groundRayLength = 1.5f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Grounding")]
        [SerializeField] private bool enableGroundLocking = true;
        [SerializeField] private float groundLockVerticalOffset = 0f;
        [SerializeField, Min(0f)] private float groundLockMaxStepPerFrame = 0.25f;

        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Animation")]
        [SerializeField] private LocomotionAnimancerPresenter animancerPresenter;

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        // Latest locomotion snapshot for this character.
        private SLocomotion snapshot = SLocomotion.Default;

        // Centralized input module (subscribes global IAactions and forwards
        // them into this agent's internal buffer).
        private LocomotionInputModule inputModule;

        // New v2 locomotion discrete coordinator (currently using the Human archetype).
        private ILocomotionCoordinator locomotionCoordinator;

        // Smoothed world-space velocity and last non-zero move input
        // used to derive local planar velocity.
        private Vector3 currentVelocity = Vector3.zero;
        private Vector2 currentLocalVelocity = Vector2.zero;

        /// <summary>Whether this agent represents the primary player.</summary>
        public bool IsPlayer => isPlayer;

        /// <summary>Latest locomotion snapshot computed by this agent.</summary>
        public SLocomotion Snapshot => snapshot;

        /// <summary>Anchor transform used as camera / desired heading reference.</summary>
        public Transform FollowAnchor => followAnchor;

        /// <summary>Root transform of the visual character model.</summary>
        public Transform ModelRoot => modelRoot;

        /// <summary>Core locomotion capability profile used by this agent.</summary>
        public LocomotionProfile Profile => locomotionProfile;

        public void RotateModelYaw(float deltaAngleDegrees)
        {
            Transform root = modelRoot != null ? modelRoot : transform;
            root.rotation = Quaternion.AngleAxis(deltaAngleDegrees, Vector3.up) * root.rotation;
        }

        private void Awake()
        {
            ResolveRigReferencesIfNeeded();

            if (animancerPresenter == null)
            {
                animancerPresenter = GetComponent<LocomotionAnimancerPresenter>();
            }
        }

        private void OnEnable()
        {
            EnsureLocomotionControllerCreated();
            EnsureInputModuleCreated();
            InitializeSnapshot();
            if (autoSubscribeInput)
            {
                inputModule?.Subscribe();
            }
        }

        private void Update()
        {
            float deltaTime = TimeConstants.Delta;
            if (deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            Simulate(deltaTime);
        }

        private void LateUpdate()
        {
            // When using animator root motion on the visual model, the
            // modelRoot will be driven forward while the Agent's root
            // transform (and thus the followAnchor) would otherwise
            // stay behind. Align the Agent to the model each frame so
            // that followAnchor moves together with the visual.
            AlignToModelRootPosition();

            // Root motion typically runs before LateUpdate, so this is
            // the safest place to apply a post-root-motion grounding
            // correction (program takes over the Y axis).
            ApplyGroundLockIfEnabled();
        }

        private void OnDisable()
        {
            // Reset to a safe default state so external systems do not
            // keep consuming stale locomotion data from a disabled agent.
            inputModule?.Unsubscribe();
            inputModule?.Reset();
            InitializeSnapshot();
        }

        private void EnsureInputModuleCreated()
        {
            if (inputModule == null)
            {
                inputModule = new LocomotionInputModule(this);
            }
        }

        /// <summary>
        /// Per-frame simulation entry. In the initial version this only keeps
        /// the snapshot in sync with the Transform, without real kinematics.
        /// </summary>
        private void Simulate(float deltaTime)
        {
            // Read aggregated input from the Input module.
            SLocomotionInputActions inputActions = SLocomotionInputActions.None;
            inputModule?.ReadActions(out inputActions);

            // Apply look input to rotate the follow anchor so that
            // subsequent heading and head-look computations are
            // based on the updated camera orientation.
            LocomotionCameraAnchor.UpdateRotation(
                followAnchor,
                inputActions.LookAction,
                locomotionProfile);

            Vector3 position = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, transform);

            float maxSlopeAngle = locomotionProfile != null ? locomotionProfile.maxGroundSlopeAngle : 0f;
            SGroundContact groundContact = LocomotionGroundDetection.SampleGround(
                position,
                groundRayLength,
                groundLayerMask,
                maxSlopeAngle);

            // Compute desired local planar velocity from move input and
            // MoveSpeed, then convert it to world-space and smooth
            // towards it using the configured acceleration. This keeps
            // the actual movement speed normalized by MoveSpeed without
            // per-gait speed caps.
            float moveSpeed = locomotionProfile != null ? locomotionProfile.moveSpeed : 0f;
            Vector2 desiredLocalVelocity = LocomotionKinematics.ComputeDesiredPlanarVelocity(
                inputActions.MoveAction.Equals(SMoveIAction.None) ? inputActions.LastMoveAction : inputActions.MoveAction,
                moveSpeed);

            float acceleration = locomotionProfile != null ? locomotionProfile.acceleration : 0f;
            currentLocalVelocity = LocomotionKinematics.SmoothVelocity(
                currentLocalVelocity,
                desiredLocalVelocity,
                acceleration,
                deltaTime);

            Vector3 desiredVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                desiredLocalVelocity,
                locomotionHeading);
            currentVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                currentLocalVelocity,
                locomotionHeading);

            if (locomotionCoordinator != null)
            {
                // 1) Evaluate discrete state.
                // turnAngle is an Agent probe; whether we are "turning in place"
                // is evaluated by Discrete and aggregated into DiscreteState.
                float turnAngle = LocomotionKinematics.ComputeSignedPlanarTurnAngle(bodyForward, locomotionHeading);

                var agentProbeSnapshot = new SLocomotionAgent(
                    position,
                    desiredLocalVelocity,
                    desiredVelocity,
                    currentLocalVelocity,
                    currentVelocity,
                    currentVelocity.magnitude,
                    locomotionHeading,
                    bodyForward,
                    Vector2.zero,
                    groundContact,
                    turnAngle,
                    isLeftFootOnFront: false);

                SLocomotionDiscrete mode = locomotionCoordinator.Evaluate(
                    in agentProbeSnapshot,
                    locomotionProfile,
                    in inputActions,
                    deltaTime);

                // 2) Evaluate head look from follow anchor towards the body.
                Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                    followAnchor,
                    modelRoot,
                    transform,
                    locomotionProfile);

                // 3) Assemble the external locomotion snapshot DTO.
                var agentSnapshot = new SLocomotionAgent(
                    position,
                    desiredLocalVelocity,
                    desiredVelocity,
                    currentLocalVelocity,
                    currentVelocity,
                    currentVelocity.magnitude,
                    locomotionHeading,
                    bodyForward,
                    lookDirection,
                    groundContact,
                    turnAngle,
                    isLeftFootOnFront: false);

                // Assemble once: core + discrete + (optional) animation output.
                var baseSnapshot = new SLocomotion(agentSnapshot, mode);
                SLocomotionAnimation animation = animancerPresenter != null
                    ? animancerPresenter.Evaluate(in baseSnapshot, deltaTime)
                    : default;

                snapshot = new SLocomotion(agentSnapshot, mode, animation);
            }

            PushSnapshot();
        }

        private void InitializeSnapshot()
        {
            Vector3 position = transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, transform);

            currentVelocity = Vector3.zero;
            currentLocalVelocity = Vector2.zero;

            var agentSnapshot = new SLocomotionAgent(
                position,
                Vector2.zero,
                Vector3.zero,
                Vector2.zero,
                Vector3.zero,
                0f,
                locomotionHeading,
                bodyForward,
                Vector2.zero,
                SGroundContact.None,
                0f,
                true);

            snapshot = new SLocomotion(
                agentSnapshot,
                new SLocomotionDiscrete(
                    ELocomotionPhase.GroundedIdle,
                    EPosture.Standing,
                    EMovementGait.Idle,
                    ELocomotionCondition.Normal,
                    isTurning: false));

            PushSnapshot();
        }

        private void PushSnapshot()
        {
            GameContext context = GameContext.Instance;
            if (context != null)
            {
                context.UpdateSnapshot(snapshot);
            }
        }

        private void EnsureLocomotionControllerCreated()
        {
            if (locomotionCoordinator == null)
            {
                // For now we always use the Human archetype.
                locomotionCoordinator = new HumanLocomotionCoordinator();
            }
        }

        /// <summary>
        /// Align the Agent's root transform to the animated model root
        /// position so that any children such as the followAnchor move
        /// together with animator-driven root motion.
        /// </summary>
        private void AlignToModelRootPosition()
        {
            if (modelRoot == null)
            {
                return;
            }

            Vector3 worldPos = modelRoot.position;
            transform.position = worldPos;
            modelRoot.localPosition = Vector3.zero;
        }

        private void ApplyGroundLockIfEnabled()
        {
            if (!enableGroundLocking)
            {
                return;
            }

            Vector3 origin = transform.position;
            float maxSlopeAngle = locomotionProfile != null ? locomotionProfile.maxGroundSlopeAngle : 0f;
            SGroundContact groundContact = LocomotionGroundDetection.SampleGround(
                origin,
                groundRayLength,
                groundLayerMask,
                maxSlopeAngle);

            if (!groundContact.IsGrounded)
            {
                return;
            }

            float targetY = groundContact.ContactPoint.y + groundLockVerticalOffset;
            Vector3 position = transform.position;
            float deltaY = targetY - position.y;

            if (groundLockMaxStepPerFrame > 0f)
            {
                deltaY = Mathf.Clamp(deltaY, -groundLockMaxStepPerFrame, groundLockMaxStepPerFrame);
            }

            if (Mathf.Abs(deltaY) <= Mathf.Epsilon)
            {
                return;
            }

            position.y += deltaY;
            transform.position = position;
        }

        private void ResolveRigReferencesIfNeeded()
        {
            if (modelRoot == null)
            {
            Transform model = transform.Find(CommonConstants.ModelChildName);
            if (model != null)
            {
                modelRoot = model;
            }
        }

        if (followAnchor == null)
        {
            Transform follow = transform.Find(CommonConstants.FollowAnchorChildName);
            if (follow != null)
            {
                followAnchor = follow;
            }
        }
        }

    }
}
