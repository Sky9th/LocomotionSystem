using UnityEngine;
using Animancer;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseTurnInPlaceState : LocomotionLayerFsmState<BaseLayer>
    {
        private StringAsset selectedAlias;

        public BaseTurnInPlaceState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Snapshot.Locomotion.Discrete.Phase == ELocomotionPhase.GroundedIdle
            && Owner.Snapshot.Locomotion.Discrete.IsTurning;

        public override void OnEnterState()
        {
            selectedAlias = Owner.Snapshot.Locomotion.Motor.TurnAngle > 0f
                ? Owner.Alias.turnInPlace90R : Owner.Alias.turnInPlace90L;
            Owner.Play(selectedAlias);
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            Owner.ApplyTurnStepRotation();
            if (selectedAlias != null) Owner.PlayIfChanged(selectedAlias);
            if (Owner.TrySetState(BaseStateKey.IdleToMoving)) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
        }
    }
}
