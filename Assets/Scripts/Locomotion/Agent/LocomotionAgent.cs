using UnityEngine;
using Game.Locomotion.Discrete.Coordination;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Input;
using Game.Locomotion.Config;
using Game.Locomotion.Animation.Presenters;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Motor;

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
    public partial class LocomotionAgent : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private Transform modelRoot;

        [Header("Config")]
        [SerializeField] private LocomotionProfile locomotionProfile;

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

        // Motor module owning per-agent kinematic state and transform corrections.
        private LocomotionMotor motor;
        internal LocomotionMotor Motor => motor;

        /// <summary>Whether this agent represents the primary player.</summary>
        public bool IsPlayer => isPlayer;

        /// <summary>Latest locomotion snapshot computed by this agent.</summary>
        public SLocomotion Snapshot => snapshot;

        /// <summary>Root transform of the visual character model.</summary>
        public Transform ModelRoot => modelRoot;

        /// <summary>Core locomotion capability profile used by this agent.</summary>
        public LocomotionProfile Profile => locomotionProfile;

        private void Awake()
        {
            ResolveRigReferencesIfNeeded();

            if (animancerPresenter == null)
            {
                animancerPresenter = GetComponentInChildren<LocomotionAnimancerPresenter>();
            }
        }

        private void OnEnable()
        {
            EnsureMotorCreated();
            EnsureLocomotionControllerCreated();
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
            inputModule?.Reset();
            motor?.Reset();
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

            bool hasCameraControl = false;
            SCameraContext cameraControl = default;
            inputModule?.ReadCameraControl(out hasCameraControl, out cameraControl);

            Vector3 viewForward = hasCameraControl
                ? (cameraControl.AnchorRotation * Vector3.forward)
                : Vector3.zero;

            // Evaluate motor output (pose + kinematics + probes).
            SLocomotionMotor motorOutput = motor.Evaluate(
                locomotionProfile,
                in inputActions,
                viewForward,
                deltaTime);

            SLocomotionDiscrete mode = locomotionCoordinator.Evaluate(
                in motorOutput,
                locomotionProfile,
                in inputActions,
                deltaTime);

            // Assemble once: core + discrete + (optional) animation output.
            var baseSnapshot = new SLocomotion(motorOutput, mode);
            SLocomotionAnimation animation = animancerPresenter != null
                ? animancerPresenter.Evaluate(in baseSnapshot, deltaTime)
                : default;

            snapshot = new SLocomotion(motorOutput, mode, animation);


            PushSnapshot();
        }

        private void PushSnapshot()
        {
            GameContext context = GameContext.Instance;
            if (context != null)
            {
                context.UpdateSnapshot(snapshot);

                if (context.TryResolveService(out EventDispatcher dispatcher))
                {
                    dispatcher.Publish(snapshot);
                }
            }
        }

        private void EnsureLocomotionControllerCreated()
        {
            if (locomotionCoordinator == null)
            {
                // For now we always use the Human archetype.
                locomotionCoordinator = new LocomotionCoordinatorHuman();
            }
        }

        private void EnsureInputModuleCreated()
        {
            if (inputModule == null)
            {
                inputModule = new LocomotionInputModule(this);
            }
        }

        private void EnsureMotorCreated()
        {
            if (motor == null)
            {
                motor = new LocomotionMotor(transform, modelRoot, locomotionProfile);
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
        }

    }
}
