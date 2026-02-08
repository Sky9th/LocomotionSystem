using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private sealed class TurnInPlaceState : State
        {
            public TurnInPlaceState(LocomotionAnimancerAdapter adapter) : base(adapter)
            {
                AddTransition(LocomotionConditions.NotTurning, () => Adapter.idleState);
                AddTransition(LocomotionConditions.IsMoving, () => Adapter.moveState);
            }

            public override void OnEnterState()
            {
                PlayTurnClip();
            }

            public override void Update()
            {
                if (TryApplyTransitions())
                {
                    return;
                }
            }

            private void PlayTurnClip()
            {
                var snapshot = Adapter.agent.Snapshot;
                float angle = Mathf.Abs(snapshot.TurnAngle);

                if (angle < 90f)
                {
                    Adapter.baseLayer.TryPlay(snapshot.TurnAngle > 0f ? Adapter.alias.turnInPlace90R : Adapter.alias.turnInPlace90L);
                }
                else
                {
                    Adapter.baseLayer.TryPlay(snapshot.TurnAngle > 0f ? Adapter.alias.turnInPlace180R : Adapter.alias.turnInPlace180L);
                }
            }
        }
    }
}
