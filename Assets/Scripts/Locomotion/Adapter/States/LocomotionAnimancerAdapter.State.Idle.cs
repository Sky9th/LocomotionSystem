using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;
using Animancer;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private sealed class IdleState : State
        {

            private AnimancerState idleState;

            public IdleState(LocomotionAnimancerAdapter adapter) : base(adapter)
            {
                // Prefer turning-in-place over moving when both could apply.
                AddTransition(LocomotionConditions.IsTurningInPlace, () => Adapter.turnState);
                AddTransition(LocomotionConditions.IsMoving, () => Adapter.moveState);
            }

            public override void OnEnterState()
            {
                Logger.Log(Adapter.agent.Snapshot.IsLeftFootOnFront);
                if (Adapter.agent.Snapshot.IsLeftFootOnFront)
                {
                    Logger.Log("Playing left idle");
                    idleState = Adapter.baseLayer.TryPlay(Adapter.alias.idleL);
                } else
                {
                    Logger.Log("Playing right idle");
                    idleState = Adapter.baseLayer.TryPlay(Adapter.alias.idleR);
                }
            }

            public override void Update()
            {
                if (TryApplyTransitions())
                {
                    return;
                }
            }
        }
    }
}
