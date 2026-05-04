using Animancer;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseMovingState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Snapshot.Locomotion.Discrete.Phase == ELocomotionPhase.GroundedMoving
            && !Owner.Snapshot.Locomotion.Discrete.IsTurning;
        }
    }
}
