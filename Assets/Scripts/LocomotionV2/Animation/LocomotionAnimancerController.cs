using UnityEngine;
using Animancer;
using Game.Locomotion.Agent;

namespace Game.Locomotion.Animation
{
    /// <summary>
    /// Animancer-based presentation controller for LocomotionAgent.
    ///
    /// Consumes SPlayerLocomotion snapshots from the agent and drives
    /// basic Idle/Walk/Run/Sprint animation states. More advanced
    /// behaviour such as turn-in-place and airborne handling will be
    /// added later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocomotionAnimancerController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LocomotionAgent agent;
        [SerializeField] private AnimancerComponent animancer;

        [Header("Clips")]
        [SerializeField] private ClipTransition idle;
        [SerializeField] private ClipTransition walk;
        [SerializeField] private ClipTransition run;
        [SerializeField] private ClipTransition sprint;

        private EMovementGait lastGait = EMovementGait.Idle;

        public LocomotionAgent Agent => agent;
        public AnimancerComponent Animancer => animancer;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponentInParent<LocomotionAgent>();
            }

            if (animancer == null)
            {
                animancer = GetComponentInChildren<AnimancerComponent>();
            }
        }

        private void OnEnable()
        {
            // Ensure an initial state is playing so the character is never frozen.
            if (animancer != null && idle != null)
            {
                animancer.Play(idle);
                lastGait = EMovementGait.Idle;
            }
        }

        private void Update()
        {
            if (agent == null || animancer == null)
            {
                return;
            }

            SPlayerLocomotion snapshot = agent.Snapshot;
            UpdateLocomotionState(snapshot);
        }

        private void UpdateLocomotionState(SPlayerLocomotion snapshot)
        {
            EMovementGait targetGait = snapshot.Gait;

            if (targetGait == lastGait)
            {
                return;
            }

            switch (targetGait)
            {
                case EMovementGait.Idle:
                    PlayIfValid(idle, EMovementGait.Idle);
                    break;

                case EMovementGait.Walk:
                    PlayIfValid(walk, EMovementGait.Walk);
                    break;

                case EMovementGait.Run:
                    PlayIfValid(run, EMovementGait.Run);
                    break;

                case EMovementGait.Sprint:
                    PlayIfValid(sprint, EMovementGait.Sprint);
                    break;

                default:
                    PlayIfValid(idle, EMovementGait.Idle);
                    break;
            }
        }

        private void PlayIfValid(ClipTransition clip, EMovementGait gait)
        {
            if (clip == null || animancer == null)
            {
                return;
            }

            animancer.Play(clip);
            lastGait = gait;
        }
    }
}