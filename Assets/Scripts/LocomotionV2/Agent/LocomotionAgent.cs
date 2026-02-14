using System;
using UnityEngine;
using Game.Locomotion.Input;
using Game.Locomotion.Logic;
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

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool autoResolveAnimator = true;

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

        // Encapsulated movement state (velocity + local velocity helpers).
        private readonly LocomotionMovementState movementState = new LocomotionMovementState();

        // Encapsulated turn state logic.
        private readonly LocomotionTurnState turnState = new LocomotionTurnState();

        // Encapsulated foot placement state.
        private readonly LocomotionFootState footState = new LocomotionFootState();

        // Centralized input module (subscribes & buffers IAactions).
        private LocomotionInputModule inputModule;

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
            if (animator == null && autoResolveAnimator)
            {
                animator = GetComponentInChildren<Animator>();
            }
            InitializeSnapshot();
        }

        private void OnEnable()
        {
            EnsureInputModuleCreated();

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

            movementState.Reset();
            turnState.Reset();
            footState.Reset();
        }

        /// <summary>
        /// Per-frame simulation entry. In the initial version this only keeps
        /// the snapshot in sync with the Transform, without real kinematics.
        /// </summary>
        private void Simulate(float deltaTime)
        {
            // TODO V2: Wire up Input / Logic / Computation modules here
            // to generate a complete locomotion snapshot.

            // Current phase: keep the snapshot in sync with the Transform,
            // read move intent from the centralized input module, and
            // apply a minimal Idle/Walk decision with localVelocity output.

            SPlayerMoveIAction moveAction = SPlayerMoveIAction.None;
            SPlayerLookIAction lookAction = SPlayerLookIAction.None;
            inputModule?.GetLatestInput(out moveAction, out lookAction);

            // Rotate the camera / follow anchor based on look input
            // before deriving locomotion heading and head look angles.
            LocomotionCameraAnchorLogic.UpdateAnchorRotation(followAnchor, lookAction, config);

            Vector3 position = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, transform);

            float maxSlopeAngle = config != null ? config.MaxGroundSlopeAngle : 0f;
            SGroundContact groundContact = LocomotionGroundDetection.SampleGround(position, groundRayLength, groundLayerMask, maxSlopeAngle);

            // Update world and local velocities via the movement state.
            movementState.Update(
                locomotionHeading,
                moveAction,
                config,
                deltaTime,
                out Vector3 velocity,
                out Vector2 localVelocity);

            // Minimal logic: evaluate discrete locomotion mode.
            LocomotionModeLogic.Evaluate(
                velocity,
                groundContact,
                config,
                out SLocomotionDiscreteState mode);

            // Evaluate turning angle and state based on the
            // difference between the body forward and the desired
            // locomotion heading, using the shared turn state logic.
            turnState.Update(bodyForward, locomotionHeading, config, deltaTime);

            Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                followAnchor,
                modelRoot,
                transform,
                config);

            // Update foot placement state.
            footState.Update(
                animator,
                modelRoot,
                transform,
                locomotionHeading);

            snapshot = new SPlayerLocomotion(
                position,
                velocity: velocity,
                locomotionHeading: locomotionHeading,
                bodyForward: bodyForward,
                localVelocity: localVelocity,
                lookDirection: lookDirection,
                state: mode.State,
                groundContact: groundContact,
                turnAngle: turnState.TurnAngle,
                isTurning: turnState.IsTurningInPlace,
                isLeftFootOnFront: footState.IsLeftFootOnFront,
                posture: mode.Posture,
                gait: mode.Gait,
                condition: mode.Condition);

            PushSnapshot();
        }

        private void InitializeSnapshot()
        {
            Vector3 position = transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, transform);

            movementState.Reset();
            turnState.Reset();
            footState.Reset();

            snapshot = new SPlayerLocomotion(
                position,
                velocity: Vector3.zero,
                locomotionHeading: locomotionHeading,
                bodyForward: bodyForward,
                localVelocity: Vector2.zero,
                lookDirection: Vector2.zero,
                state: ELocomotionState.GroundedIdle,
                groundContact: SGroundContact.None,
                turnAngle: 0f,
                isTurning: false,
                isLeftFootOnFront: footState.IsLeftFootOnFront,
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
