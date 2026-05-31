using UnityEngine;
using Animancer;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseTurnInMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        private StringAsset selectedAlias;

        public BaseTurnInMovingState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
        {
            get
            {
                var disc = Owner.Ctx.Discrete;
                if (disc.Phase != ELocomotionPhase.GroundedMoving || !disc.IsTurning) return false;

                var vel = Owner.Ctx.Motor.DesiredLocalVelocity;
                var speed = Owner.LocoProfile != null ? Owner.LocoProfile.GetSpeedForGait(Owner.Ctx.Discrete.Gait) : 0f;
                var forwardThreshold = speed > 0f ? speed * 0.9f : 0.01f;
                var lateralThreshold = speed > 0f ? speed * 0.1f : 0.01f;
                return vel.y >= forwardThreshold && Mathf.Abs(vel.x) <= lateralThreshold;
            }
        }

        public override bool CanExitState => true;

        public override void OnEnterState()
        {
            var turnAngle = Owner.Ctx.Motor.TurnAngle;
            var isRight = turnAngle > 0f;
            selectedAlias = Owner.Ctx.Discrete.Gait switch
            {
                EMovementGait.Walk => isRight ? Owner.Alias.turnInWalk180R : Owner.Alias.turnInWalk180L,
                EMovementGait.Run  => isRight ? Owner.Alias.turnInRun180R  : Owner.Alias.turnInRun180L,
                _                  => isRight ? Owner.Alias.turnInSprint180R : Owner.Alias.turnInSprint180L
            };
            Owner.PlayFromStart(selectedAlias);
        }

        public override void Tick()
        {
            var vel = Owner.Ctx.Motor.DesiredLocalVelocity;
            var speed = Owner.LocoProfile != null ? Owner.LocoProfile.GetSpeedForGait(Owner.Ctx.Discrete.Gait) : 0f;
            if (vel.y < speed * 0.9f || Mathf.Abs(vel.x) > speed * 0.1f)
            { Owner.ForceSetState(BaseStateKey.Moving); return; }

            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;

            if (Owner.HasCompleted())
            {
                if (Owner.Ctx.Discrete.IsTurning)
                {
                    var turnAngle = Owner.Ctx.Motor.TurnAngle;
                    var isRight = turnAngle > 0f;
                    selectedAlias = Owner.Ctx.Discrete.Gait switch
                    {
                        EMovementGait.Walk => isRight ? Owner.Alias.turnInWalk180R : Owner.Alias.turnInWalk180L,
                        EMovementGait.Run  => isRight ? Owner.Alias.turnInRun180R  : Owner.Alias.turnInRun180L,
                        _                  => isRight ? Owner.Alias.turnInSprint180R : Owner.Alias.turnInSprint180L
                    };
                    Owner.PlayFromStart(selectedAlias);
                    return;
                }
                Owner.ForceSetState(BaseStateKey.Moving);
            }
        }
    }
}
