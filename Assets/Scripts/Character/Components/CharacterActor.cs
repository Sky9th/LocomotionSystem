using UnityEngine;
using Game.Character.Config;
using Game.Character.Input;
using Game.Character.Kinematic;
using Game.Character.Locomotion;

namespace Game.Character.Components
{
    [DisallowMultipleComponent]
    public sealed class CharacterActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private CharacterProfile characterProfile;

        [Header("Locomotion")]
        [SerializeField] private LocomotionProfile locomotionProfile;

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        public bool IsPlayer => isPlayer;

        private CharacterInputModule inputModule;
        private CharacterKinematic characterKinematic;
        private ILocomotionSimulator locomotionSimulator;

        private void Awake()
        {
            inputModule = new CharacterInputModule(this);
            characterKinematic = new CharacterKinematic(transform, transform, characterProfile);
            locomotionSimulator = new GroundLocomotion();
        }

        private void OnEnable()
        {
            if (autoSubscribeInput) inputModule?.Subscribe();
        }

        private void OnDisable()
        {
            inputModule?.Unsubscribe();
            inputModule?.Reset();
            characterKinematic?.Reset();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= Mathf.Epsilon) return;

            GameContext context = GameContext.Instance;
            if (context == null) return;

            var ctx = new CharacterFrameContext();

            inputModule.ReadActions(out ctx.Input);

            inputModule.ReadCameraControl(out var cameraControl);
            Vector3 viewForward = isPlayer
                ? (cameraControl.AnchorRotation * Vector3.forward)
                : Vector3.zero;

            ctx.Kinematic = characterKinematic.Evaluate(characterProfile, viewForward, deltaTime);

            locomotionSimulator.Simulate(ref ctx, locomotionProfile, deltaTime);

            var snapshot = new SCharacterSnapshot(
                ctx.Kinematic,
                new SLocomotionState(ctx.Motor, ctx.Discrete));

            context.UpdateSnapshot(snapshot);
        }
    }
}
