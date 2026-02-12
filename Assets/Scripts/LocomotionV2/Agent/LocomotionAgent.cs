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
            float deltaTime = GameTime.Delta;
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
            // TODO V2: Wire up Input / Logic / Computation modules here
            // to generate a complete locomotion snapshot.

            // Current phase: keep the snapshot in sync with the Transform,
            // read move intent from the centralized input module, and
            // apply a minimal Idle/Walk decision with localVelocity output.

            SPlayerMoveIAction moveAction = SPlayerMoveIAction.None;
            if (inputModule != null && inputModule.TryGetAction(out SPlayerMoveIAction buffered))
            {
                moveAction = buffered;
            }

            Vector3 position = modelRoot.transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = GetLocomotionHeading();

            SGroundContact groundContact = GroundDetection.SampleGround(position, groundRayLength, groundLayerMask);

            // Compute simple world-space planar velocity from heading,
            // input intensity and the shared locomotion config.
            Vector3 velocity = LocomotionKinematics.ComputePlanarVelocity(locomotionHeading, moveAction, config);

            // Minimal logic: evaluate discrete locomotion mode and
            // corresponding local planar velocity in a single step.
            LocomotionModeLogic.Evaluate(
                velocity,
                groundContact,
                moveAction,
                out Vector2 localVelocity,
                out SLocomotionDiscreteState mode);

            snapshot = new SPlayerLocomotion(
                position,
                velocity: velocity,
                locomotionHeading: locomotionHeading,
                bodyForward: bodyForward,
                localVelocity: localVelocity,
                lookDirection: Vector2.zero,
                state: mode.State,
                groundContact: groundContact,
                turnAngle: 0f,
                isTurning: false,
                isLeftFootOnFront: true,
                posture: mode.Posture,
                gait: mode.Gait,
                condition: mode.Condition);

            PushSnapshot();
        }

        private void InitializeSnapshot()
        {
            Vector3 position = transform.position;
            Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
            Vector3 locomotionHeading = GetLocomotionHeading();

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
                isLeftFootOnFront: true,
                posture: EPostureState.Standing,
                gait: EMovementGait.Idle,
                condition: ELocomotionCondition.Normal);

            PushSnapshot();
        }

        private void PushSnapshot()
        {
            GameContext.Instance.UpdateSnapshot(snapshot);
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
                Transform model = transform.Find("Model");
                if (model != null)
                {
                    modelRoot = model;
                }
            }

            if (followAnchor == null)
            {
                Transform follow = transform.Find("Follow");
                if (follow != null)
                {
                    followAnchor = follow;
                }
            }
        }

        private Vector3 GetLocomotionHeading()
        {
            Transform source = followAnchor != null ? followAnchor : transform;
            Vector3 forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }
            Vector3 origin = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 forward = GetLocomotionHeading();

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
