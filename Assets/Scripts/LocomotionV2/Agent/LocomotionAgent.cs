using System;
using UnityEngine;
using Game.Locomotion.Input;
using Game.Locomotion.Computation;
using Game.Locomotion.State.Core;
using Game.Locomotion.State.Controllers;
using Game.Locomotion.Computation;

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

        // Centralized input module (subscribes & buffers IAactions).
        private LocomotionInputModule inputModule;

        // New v2 state controller (currently using the Human archetype).
        private ILocomotionController locomotionController;

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

        private void OnDisable()
        {
            // Reset to a safe default state so external systems do not
            // keep consuming stale locomotion data from a disabled agent.
            inputModule?.Unsubscribe();
            InitializeSnapshot();
        }

        /// <summary>
        /// Per-frame simulation entry. In the initial version this only keeps
        /// the snapshot in sync with the Transform, without real kinematics.
        /// </summary>
        private void Simulate(float deltaTime)
        {
            SPlayerMoveIAction moveAction = SPlayerMoveIAction.None;
            SPlayerLookIAction lookAction = SPlayerLookIAction.None;
            SPlayerCrouchIAction crouchAction = SPlayerCrouchIAction.None;
            SPlayerProneIAction proneAction = SPlayerProneIAction.None;
            SPlayerJumpIAction jumpAction = SPlayerJumpIAction.None;
            SPlayerStandIAction standAction = SPlayerStandIAction.None;
            inputModule?.GetLatestInput(out moveAction, out lookAction, out crouchAction, out proneAction, out jumpAction, out standAction);

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

            Vector3 velocity = Vector3.zero;
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
                snapshot = new SPlayerLocomotion(
                    position,
                    velocity: velocity,
                    locomotionHeading: locomotionHeading,
                    bodyForward: bodyForward,
                    localVelocity: Vector2.zero,
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

        private void EnsureInputModuleCreated()
        {
            if (inputModule == null)
            {
                inputModule = new LocomotionInputModule(this);
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
