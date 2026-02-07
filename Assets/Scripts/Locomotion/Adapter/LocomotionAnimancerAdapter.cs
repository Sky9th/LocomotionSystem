using UnityEngine;
using Animancer;
using Animancer.FSM;
using UnityEditor.Animations;
using Object = UnityEngine.Object;
using Animancer.TransitionLibraries;

/// <summary>
/// Animancer-based presentation adapter for <see cref="LocomotionAgent"/>.
/// Consumes SPlayerLocomotion snapshots and a LocomotionAnimationProfile
/// to drive idle / move / turn-in-place / airborne animation states.
/// </summary>
namespace Game.Locomotion.Adapter
{
    [DisallowMultipleComponent]
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
    [Header("Dependencies")]
    [SerializeField] private LocomotionAgent agent;
    [SerializeField] private NamedAnimancerComponent animancer;
    [SerializeField] private AnimancerStringProfile alias;

    [Header("Header Look")]
    [SerializeField] private AvatarMask headerMask;

    [Header("Head Look Smoothing")]
    [SerializeField] private float headYawSpeed = 5f;
    [SerializeField] private float headPitchSpeed = 5f;

    private AnimancerLayer baseLayer;

    private StateMachine<State> stateMachine;

    private IdleState idleState;
    private TurnInPlaceState turnState;
    private MoveState moveState;

    public LocomotionAgent Agent => agent;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<LocomotionAgent>();
        }
        if (animancer == null)
        {
            animancer = GetComponentInChildren<NamedAnimancerComponent>();
        }

        baseLayer = animancer.Layers[0];

        headLayer = animancer.Layers[1];
        headLayer.Mask = headerMask;

        idleState = new IdleState(this);
        turnState = new TurnInPlaceState(this);
        moveState = new MoveState(this);

        stateMachine = new StateMachine<State>(idleState);
    }

    private void OnEnable()
    {
        stateMachine.InitializeAfterDeserialize();
    }

    void Update()
    {
        if (agent == null || animancer == null)
        {
            return;
        }

        // Update animation state machine before applying layered effects like head look.
        stateMachine.CurrentState?.Update();

        UpdateLookDirection();
    }

}
}
