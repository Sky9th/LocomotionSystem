using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private sealed class IdleState : State
        {
            public IdleState(LocomotionAnimancerAdapter adapter) : base(adapter)
            {
                // Prefer turning-in-place over moving when both could apply.
                AddTransition(LocomotionConditions.IsTurningInPlace, () => Adapter.turnState);
                AddTransition(LocomotionConditions.IsMoving, () => Adapter.moveState);
            }

            public override void OnEnterState()
            {
                Adapter.baseLayer.TryPlay(Adapter.alias.idle);
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
