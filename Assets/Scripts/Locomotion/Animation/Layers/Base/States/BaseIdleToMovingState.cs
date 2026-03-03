namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleToMovingState : BaseLayerFsmState
    {
        public BaseIdleToMovingState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override void OnEnterState()
        {
            if (Owner.Snapshot.TurnAngle > 0)
            {
                Owner.Play(Owner.AliasProfile.idleToRun180R);
            } 
            else
            {
                Owner.Play(Owner.AliasProfile.idleToRun180L);
            }
        }

        public override void OnExitState()
        {
            // Ensure the next state starts with the correct animation if we were interrupted.
            Logger.Log($"IdleToMoving state exiting. Interrupted: {!Owner.HasCurrentAnimationCompleted()}");
        }

        public override void Tick()
        {
            if (Owner.Snapshot.State == ELocomotionState.GroundedIdle)
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            if (Owner.Snapshot.State == ELocomotionState.GroundedMoving)
            {
                Owner.TrySetState(BaseStateKey.Moving);
                return;
            }
            
            if (Owner.HasCurrentAnimationCompleted())
            {
                Owner.TrySetState(BaseStateKey.Moving);
                return;
            }
        }

        public override bool CanEnterState => Owner.Snapshot.State == ELocomotionState.GroundedMoving;

        public override bool CanExitState => !Owner.Snapshot.IsTurning;
    }
}
