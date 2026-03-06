using Animancer;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Conditions;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseAirLoopState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLoopState(BaseLayer owner) : base(owner)
        {
        }

        public override bool CanEnterState => !Owner.Snapshot.Motor.GroundContact.IsGrounded;

        public override void OnEnterState()
        {
            Owner.Play(Owner.AliasProfile.AirLoop);
        }

        public override void Tick()
        {

            // Let the FSM drive its own transitions.
            if (Owner.TrySetState(BaseStateKey.AirLand))
            {
                return;
            }
        }
    }
}
