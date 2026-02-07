using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;
using Animancer;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private sealed class MoveState : State
        {
            private Vector2MixerState currentMoveState;
            private LocomotionAnimancerAdapter adapter;

            public MoveState(LocomotionAnimancerAdapter adapter) : base(adapter)
            {
                this.adapter = adapter;
                AddTransition(LocomotionConditions.IsStopped, () => Adapter.idleState);
                AddTransition(LocomotionConditions.IsTurningInPlace, () => Adapter.turnState);
            }

            public override void OnEnterState()
            {
                currentMoveState = (Vector2MixerState)Adapter.baseLayer.TryPlay(Adapter.alias.walkMixer);
            }

            public override void Update()
            {
                if (TryApplyTransitions())
                {
                    return;
                }

                currentMoveState.Parameter = adapter.agent.Snapshot.LocalVelocity;
                // TODO: update walk/run blending based on snapshot.Velocity here.
            }
        }
    }
}
