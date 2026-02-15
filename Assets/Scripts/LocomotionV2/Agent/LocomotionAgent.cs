using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Locomotion.Computation;
using Game.Locomotion.State.Core;
using Game.Locomotion.State.Controllers;
using Game.Locomotion.Input;

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
    public class LocomotionAgent : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private Transform followAnchor;
        [SerializeField] private Transform modelRoot;

        [Header("Config")]
        [SerializeField] private LocomotionConfigProfile config;
        [SerializeField, Min(0f)] private float groundRayLength = 1.5f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField, Min(0.1f)] private float debugForwardLength = 2f;

        // Latest locomotion snapshot for this character.
        private SPlayerLocomotion snapshot = SPlayerLocomotion.Default;

        // Centralized input module (subscribes global IAactions and forwards
        // them into this agent's internal buffer).
        private LocomotionInputModule inputModule;

        // Per-agent IAction buffer keyed by payload type.
        private readonly Dictionary<Type, object> inputActionBuffer = new();

        // New v2 state controller (currently using the Human archetype).
        private ILocomotionController locomotionController;

        // Smoothed world-space velocity and last non-zero move input
        // used to derive local planar velocity.
        private Vector3 currentVelocity = Vector3.zero;
        private Vector2 lastMoveInput = Vector2.zero;

        // Last evaluated discrete locomotion state used to seed
        // the next frame's state context.
        private SLocomotionDiscreteState lastDiscreteState =
            new SLocomotionDiscreteState(
                ELocomotionState.GroundedIdle,
                EPostureState.Standing,
                EMovementGait.Idle,
                ELocomotionCondition.Normal);

        /// <summary>Whether this agent represents the primary player.</summary>
        public bool IsPlayer => isPlayer;

        /// <summary>Latest locomotion snapshot computed by this agent.</summary>
        public SPlayerLocomotion Snapshot => snapshot;

        /// <summary>Anchor transform used as camera / desired heading reference.</summary>
        public Transform FollowAnchor => followAnchor;

        /// <summary>Root transform of the visual character model.</summary>
        public Transform ModelRoot => modelRoot;

        /// <summary>
        /// Called by input/AI modules to push the latest IAction payload
        /// into this agent's per-type input buffer.
        /// </summary>
        internal void TryPutAction<TAction>(TAction action) where TAction : struct
        {
            inputActionBuffer[typeof(TAction)] = action;
        }

        /// <summary>
        /// Tries to read the latest buffered IAction of the given type.
        /// Returns false and a default-initialized action when the type
        /// has never been seen for this agent.
        /// </summary>
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

        /// <summary>
        /// Returns the latest buffered IAction of the given type. Assumes
        /// that upstream modules have populated the buffer with a sensible
        /// default value (for example the static None value) during
        /// initialization and subscription.
        /// </summary>
        internal TAction GetAction<TAction>() where TAction : struct
        {
            if (inputActionBuffer.TryGetValue(typeof(TAction), out object boxed) && boxed is TAction typed)
            {
                return typed;
            }

            return default;
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(LocomotionAgent)} on '{name}' requires a {nameof(LocomotionConfigProfile)}.", this);
            }
            
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
            SMoveIAction moveAction = GetAction<SMoveIAction>();
            SLookIAction lookAction = GetAction<SLookIAction>();
            SCrouchIAction crouchAction = GetAction<SCrouchIAction>();
            SProneIAction proneAction = GetAction<SProneIAction>();
            SWalkIAction walkAction = GetAction<SWalkIAction>();
            SRunIAction runAction = GetAction<SRunIAction>();
            SSprintIAction sprintAction = GetAction<SSprintIAction>();
            SJumpIAction jumpAction = GetAction<SJumpIAction>();
            SStandIAction standAction = GetAction<SStandIAction>();


            // Apply look input to rotate the follow anchor so that
            // subsequent heading and head-look computations are
            // based on the updated camera orientation.
            LocomotionCameraAnchor.UpdateRotation(
                followAnchor,
                lookAction,
                config);

            Vector3 position = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, transform);

            float maxSlopeAngle = config != null ? config.MaxGroundSlopeAngle : 0f;
            SGroundContact groundContact = LocomotionGroundDetection.SampleGround(
                position,
                groundRayLength,
                groundLayerMask,
                maxSlopeAngle);

            // Compute desired planar velocity from move input and
            // MoveSpeed, then smooth towards it using the configured
            // acceleration. This keeps the actual movement speed
            // normalized by MoveSpeed without per-gait speed caps.
            Vector3 desiredVelocity = LocomotionKinematics.ComputeDesiredPlanarVelocity(
                locomotionHeading,
                moveAction,
                config);
            float acceleration = config != null ? config.Acceleration : 0f;
            currentVelocity = LocomotionKinematics.SmoothVelocity(
                currentVelocity,
                desiredVelocity,
                acceleration,
                deltaTime);

            Vector3 velocity = currentVelocity;
            var stateContext = new LocomotionStateContext(
                velocity,
                bodyForward,
                locomotionHeading,
                groundContact,
                config,
                lastDiscreteState,
                moveAction,
                lookAction,
                crouchAction,
                proneAction,
                walkAction,
                runAction,
                sprintAction,
                jumpAction,
                standAction);

            if (locomotionController != null)
            {
                // 1) Evaluate discrete state and all state-related outputs via the controller.
                SLocomotionDiscreteState mode = locomotionController.UpdateDiscreteState(in stateContext, deltaTime);
                lastDiscreteState = mode;

                // 2) Evaluate head look from follow anchor towards the body.
                Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                    followAnchor,
                    modelRoot,
                    transform,
                    config);

                float turnAngle = locomotionController.CurrentTurnAngle;
                bool isTurningInPlace = locomotionController.IsTurningInPlace;

                // 3) Assemble the external locomotion snapshot DTO.
                // Derive local planar velocity using the shared helper.
                LocomotionPlanarVelocity.Evaluate(
                    velocity,
                    locomotionHeading,
                    moveAction,
                    ref lastMoveInput,
                    out Vector2 localVelocity);

                snapshot = new SPlayerLocomotion(
                    position,
                    velocity: velocity,
                    locomotionHeading: locomotionHeading,
                    bodyForward: bodyForward,
                    localVelocity: localVelocity,
                    lookDirection: lookDirection,
                    discreteState: mode,
                    groundContact: groundContact,
                    turnAngle: turnAngle,
                    isTurning: isTurningInPlace,
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
            lastMoveInput = Vector2.zero;

            snapshot = new SPlayerLocomotion(
                position,
                velocity: Vector3.zero,
                locomotionHeading: locomotionHeading,
                bodyForward: bodyForward,
                localVelocity: Vector2.zero,
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
            if (locomotionController == null)
            {
                // For now we always use the Human archetype.
                locomotionController = new HumanLocomotionController();
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

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }
            Vector3 origin = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 forward = LocomotionHeading.Evaluate(followAnchor, transform);

            // Draw locomotion heading in a color based on the high-level state.
            Color headingColor = Color.cyan;
            switch (snapshot.State)
            {
                case ELocomotionState.GroundedMoving:
                    headingColor = Color.green;
                    break;
                case ELocomotionState.Airborne:
                    headingColor = Color.yellow;
                    break;
            }

            Gizmos.color = headingColor;
            Gizmos.DrawLine(origin, origin + forward * debugForwardLength);

            // Visualize ground detection ray and contact point.
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundRayLength);

            if (snapshot.GroundContact.IsGrounded)
            {
                Gizmos.color = Color.white;
                Vector3 contactPoint = snapshot.GroundContact.ContactPoint;
                Vector3 contactNormal = snapshot.GroundContact.ContactNormal.normalized;
                Gizmos.DrawSphere(contactPoint, 0.03f);
                Gizmos.DrawLine(contactPoint, contactPoint + contactNormal * 0.3f);
            }
        }
    }
}
