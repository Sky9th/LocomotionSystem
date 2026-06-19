using UnityEngine;
using Animancer;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseTurnInPlaceState : LocomotionLayerFsmState<BaseLayer>
    {
        // TODO: migrated to ITransition
        // private StringAsset selectedAlias;

        public BaseTurnInPlaceState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedIdle
            && Owner.Ctx.Discrete.IsTurning;

        public override void OnEnterState()
        {
            // selectedAlias = Owner.Ctx.Motor.TurnAngle > 0f
            //     ? Owner.Alias.turnInPlace90R : Owner.Alias.turnInPlace90L;
            // Owner.Play(selectedAlias);
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            Owner.ApplyTurnStepRotation();
            // if (selectedAlias != null) Owner.PlayIfChanged(selectedAlias);
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
        }
    }
}
