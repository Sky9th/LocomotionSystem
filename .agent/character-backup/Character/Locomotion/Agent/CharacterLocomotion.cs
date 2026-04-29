using UnityEngine;
using Game.Character.Components;
using Game.Character.Input;
using Game.Locomotion.Discrete.Coordination;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Config;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Motor;

namespace Game.Locomotion.Agent
{
    [DisallowMultipleComponent]
    public partial class CharacterLocomotion : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private Transform modelRoot;

        [Header("Config")]
        [SerializeField] private LocomotionProfile locomotionProfile;

        private ILocomotionCoordinator locomotionCoordinator;
        private LocomotionMotor motor;
        internal LocomotionMotor Motor => motor;

        public Transform ModelRoot => modelRoot;
        public LocomotionProfile Profile => locomotionProfile;

        private void Awake()
        {
            ResolveRigReferencesIfNeeded();
        }

        private void OnEnable()
        {
            EnsureMotorCreated();
            EnsureLocomotionControllerCreated();
        }

        private void OnDisable()
        {
            motor?.Reset();
        }

        internal void Simulate(
            ref CharacterFrameContext ctx,
            Vector3 viewForward,
            float deltaTime)
        {
            ctx.Motor = motor.Evaluate(
                in ctx.Kinematic,
                locomotionProfile,
                in ctx.Input,
                viewForward,
                deltaTime);

            ctx.Discrete = locomotionCoordinator.Evaluate(
                in ctx.Kinematic,
                in ctx.Motor,
                locomotionProfile,
                in ctx.Input,
                deltaTime);

            ctx.Traversal = locomotionCoordinator.CurrentTraversal;
        }

        private void EnsureLocomotionControllerCreated()
        {
            if (locomotionCoordinator == null)
            {
                locomotionCoordinator = new LocomotionCoordinatorHuman();
            }
        }

        private void EnsureMotorCreated()
        {
            if (motor == null)
            {
                motor = new LocomotionMotor(transform);
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
