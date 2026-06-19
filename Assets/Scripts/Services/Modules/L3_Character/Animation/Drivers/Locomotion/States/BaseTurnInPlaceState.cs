using UnityEngine;
using Animancer;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseTurnInPlaceState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseTurnInPlaceState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedIdle
            && Owner.Ctx.Discrete.IsTurning;

        public override void OnEnterState()
        {
            var turnAngle = Owner.Ctx.Motor.TurnAngle;
            var transition = turnAngle > 0f
                ? Owner.AnimSet?.turnInPlace90R : Owner.AnimSet?.turnInPlace90L;
            Owner.Play(transition);
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            Owner.ApplyTurnStepRotation();
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
        }
    }
}
