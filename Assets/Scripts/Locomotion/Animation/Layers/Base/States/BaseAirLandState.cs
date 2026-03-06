using Animancer;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Conditions;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseAirLandState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLandState(BaseLayer owner) : base(owner)
        {
        }

        public override bool CanEnterState => Owner.Snapshot.Motor.IsGrounded;

        public override bool CanExitState => true;

        public override void OnEnterState()
        {
            Owner.Play(Owner.AliasProfile.AirLand);
        }

        public override void Tick()
        {
            if(Owner.HasCurrentAnimationCompleted())
            {
                // Let the FSM drive its own transitions.
                if (Owner.TrySetState(BaseStateKey.Idle))
                {
                    return;
                }
                // Let the FSM drive its own transitions.
                if (Owner.TrySetState(BaseStateKey.Moving))
                {
                    return;
                }

                // Let the FSM drive its own transitions.
                if (Owner.TrySetState(BaseStateKey.TurnInPlace))
                {
                    return;
                }

                // Let the FSM drive its own transitions.
                if (Owner.TrySetState(BaseStateKey.TurnInMoving))
                {
                    return;
                }

            }

        }
    }
}
