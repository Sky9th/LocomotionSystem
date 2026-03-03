using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Locomotion.Computation;
using Game.Locomotion.State.Core;
using Game.Locomotion.State.Controllers;
using Game.Locomotion.Input;
using Game.Locomotion.Config;

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

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        // Latest locomotion snapshot for this character.
        private SLocomotion snapshot = SLocomotion.Default;

        // Centralized input module (subscribes global IAactions and forwards
        // them into this agent's internal buffer).
        private LocomotionInputModule inputModule;

        // Per-agent IAction buffer keyed by payload type.
        private readonly Dictionary<Type, object> inputActionBuffer = new();
        private readonly Dictionary<Type, object> lastInputActionBuffer = new();

        // New v2 locomotion state controller (currently using the Human archetype).
        private ILocomotionStateController locomotionStateController;

        // Dedicated helper for planar turning logic so that state
        // controllers can focus purely on discrete locomotion state.
        // private readonly LocomotionTurn turnHelper = new LocomotionTurn();

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

        /// <summary>
        /// Called by input/AI modules to push the latest IAction payload
        /// into this agent's per-type input buffer.
        /// </summary>
        internal void TryPutAction<TAction>(TAction action) where TAction : struct
        {
            if (action.Equals(default(TAction)))
            {
                inputActionBuffer.Remove(typeof(TAction));
            }
            else
            {
                lastInputActionBuffer[typeof(TAction)] = inputActionBuffer.ContainsKey(typeof(TAction)) ? (TAction)inputActionBuffer[typeof(TAction)] : default;
                inputActionBuffer[typeof(TAction)] = action;
            }
        }
        
        internal bool TryGetAction<TAction>(out TAction action) where TAction : struct
        {
            if (inputActionBuffer.TryGetValue(typeof(TAction), out object boxed) && boxed is TAction typed)
            {
                action = typed;
                return true;
            }
            action = default;
            return false;
        }

        internal bool TryGetLastAction<TAction>(out TAction action) where TAction : struct
        {
            if (lastInputActionBuffer.TryGetValue(typeof(TAction), out object boxed) && boxed is TAction typed)
            {
                action = typed;
                return true;
            }
            action = default;
            return false;
        }

        private void Awake()
        {
            ResolveRigReferencesIfNeeded();
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
            inputActionBuffer.Clear();
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
            // Read the latest buffered IAactions produced by upstream
            // input/AI modules. InputModule is responsible for seeding
            // sensible defaults so we can assume each type is present.
            TryGetAction(out SMoveIAction moveAction);
            TryGetLastAction(out SMoveIAction lastMoveAction);
            TryGetAction(out SLookIAction lookAction);
            TryGetAction(out SCrouchIAction crouchAction);
            TryGetAction(out SProneIAction proneAction);
            TryGetAction(out SWalkIAction walkAction);
            TryGetAction(out SRunIAction runAction);
            TryGetAction(out SSprintIAction sprintAction);
            TryGetAction(out SJumpIAction jumpAction);
            TryGetAction(out SStandIAction standAction);


            // Apply look input to rotate the follow anchor so that
            // subsequent heading and head-look computations are
            // based on the updated camera orientation.
            LocomotionCameraAnchor.UpdateRotation(
                followAnchor,
                lookAction,
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
                moveAction.Equals(SMoveIAction.None) ? lastMoveAction : moveAction,
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

            var stateContext = new SLocomotionStateContext(
                currentVelocity,
                bodyForward,
                locomotionHeading,
                groundContact,
                locomotionProfile,
                moveAction,
                lookAction,
                crouchAction,
                proneAction,
                walkAction,
                runAction,
                sprintAction,
                jumpAction,
                standAction);

            if (locomotionStateController != null)
            {
                // 1) Evaluate full locomotion state (discrete + turn)
                // via the state controller.
                SLocomotionStateFrame stateFrame = locomotionStateController.Evaluate(in stateContext, deltaTime);
                SLocomotionDiscreteState mode = stateFrame.DiscreteState;
                float turnAngle = stateFrame.TurnAngle;
                bool isTurning = stateFrame.IsTurning;

                // 2) Evaluate head look from follow anchor towards the body.
                Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                    followAnchor,
                    modelRoot,
                    transform,
                    locomotionProfile);

                // 3) Assemble the external locomotion snapshot DTO.
                snapshot = new SLocomotion(
                    position,
                    desiredLocalVelocity: desiredLocalVelocity,
                    actualLocalVelocity: currentLocalVelocity,
                    desiredPlanarVelocity: desiredVelocity,
                    actualPlanarVelocity: currentVelocity,
                    actualSpeed: currentVelocity.magnitude,
                    locomotionHeading: locomotionHeading,
                    bodyForward: bodyForward,
                    lookDirection: lookDirection,
                    discreteState: mode,
                    groundContact: groundContact,
                    turnAngle: turnAngle,
                    isTurning: isTurning,
                    isLeftFootOnFront: false,
                    posture: mode.Posture,
                    gait: mode.Gait,
                    condition: mode.Condition);
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

            snapshot = new SLocomotion(
                position,
                desiredLocalVelocity: Vector2.zero,
                actualLocalVelocity: Vector2.zero,
                desiredPlanarVelocity: Vector3.zero,
                actualPlanarVelocity: Vector3.zero,
                actualSpeed: 0f,
                locomotionHeading: locomotionHeading,
                bodyForward: bodyForward,
                lookDirection: Vector2.zero,
                discreteState: new SLocomotionDiscreteState(
                    ELocomotionState.GroundedIdle,
                    EPostureState.Standing,
                    EMovementGait.Idle,
                    ELocomotionCondition.Normal),
                groundContact: SGroundContact.None,
                turnAngle: 0f,
                isTurning: false,
                isLeftFootOnFront: true,
                posture: EPostureState.Standing,
                gait: EMovementGait.Idle,
                condition: ELocomotionCondition.Normal);

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
            if (locomotionStateController == null)
            {
                // For now we always use the Human archetype.
                locomotionStateController = new HumanLocomotionStateController();
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
