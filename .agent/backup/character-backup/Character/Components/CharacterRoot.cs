using Game.Character.Animation.Components;
using Game.Character.Animation.Drivers;
using Game.Character.Input;
using Game.Character.Kinematic;
using Game.Locomotion.Agent;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Config;
using UnityEngine;

namespace Game.Character.Components
{
    [DisallowMultipleComponent]
    public sealed class CharacterRoot : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private CharacterAnimationController characterAnimation;

        [Header("Config")]
        [SerializeField] private LocomotionProfile locomotionProfile;
        [SerializeField] private LocomotionAliasProfile locomotionAlias;
        [SerializeField] private LocomotionAnimationProfile locomotionAnimationProfile;

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        public CharacterAnimationController CharacterAnimation => characterAnimation;
        public bool IsPlayer => isPlayer;

        private CharacterLocomotion characterLocomotion;
        private CharacterInputModule inputModule;
        private CharacterKinematic characterKinematic;

        private void Awake()
        {
            if (characterAnimation == null)
            {
                characterAnimation = GetComponent<CharacterAnimationController>();
            }

            characterLocomotion = GetComponentInChildren<CharacterLocomotion>();
            inputModule = new CharacterInputModule(this);
            characterKinematic = new CharacterKinematic(transform, characterLocomotion?.ModelRoot, locomotionProfile);
        }

        private void OnEnable()
        {
            if (autoSubscribeInput)
            {
                inputModule?.Subscribe();
            }
        }

        private void OnDisable()
        {
            inputModule?.Unsubscribe();
            inputModule?.Reset();
            characterKinematic?.Reset();
        }

        private void Update()
        {
            float deltaTime = TimeConstants.Delta;
            if (deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            GameContext context = GameContext.Instance;
            if (context == null)
            {
                return;
            }

            var ctx = new CharacterFrameContext();

            inputModule.ReadActions(out ctx.Input);

            SCameraContext cameraControl = default;
            bool hasCameraControl = isPlayer && context.TryGetSnapshot(out cameraControl);

            Vector3 viewForward = hasCameraControl
                ? (cameraControl.AnchorRotation * Vector3.forward)
                : Vector3.zero;

            ctx.Kinematic = characterKinematic.Evaluate(
                locomotionProfile,
                viewForward,
                deltaTime);

            characterLocomotion?.Simulate(ref ctx, viewForward, deltaTime);

            var snapshot = new SCharacterSnapshot(
                ctx.Kinematic,
                ctx.Motor,
                ctx.Discrete,
                ctx.Traversal);

            context.UpdateSnapshot(snapshot);

            if (context.TryResolveService(out EventDispatcher dispatcher))
            {
                dispatcher.Publish(snapshot);
            }
        }

        private void Start()
        {
            if (characterAnimation == null)
            {
                return;
            }

            var motor = characterLocomotion?.Motor;
            characterAnimation.SetMotor(motor);

            var locoDriver = new LocomotionDriver(
                motor,
                locomotionProfile,
                locomotionAlias,
                locomotionAnimationProfile);

            var traversalDriver = new TraversalDriver(locomotionAlias);

            characterAnimation.RegisterDriver(locoDriver);
            characterAnimation.RegisterDriver(traversalDriver);
        }
    }
}
