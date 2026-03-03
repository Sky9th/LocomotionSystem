using Animancer;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleState : BaseLayerFsmState
    {
        public BaseIdleState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override void OnEnterState()
        {
            Owner.Play(Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null);
        }

        public override void Tick()
        {
            // Let the FSM drive its own transitions.
            if (Owner.Snapshot.State == ELocomotionState.GroundedMoving)
            {
                if (Owner.TrySetState(BaseStateKey.IdleToMoving))
                {
                    return;
                }
            }

            if (Owner.Snapshot.State == ELocomotionState.GroundedIdle && Owner.Snapshot.IsTurning)
            {
                if (Owner.TrySetState(BaseStateKey.TurnInPlace))
                {
                    return;
                }
            }

            StringAsset idleAlias = Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null;
            Owner.PlayIfChanged(idleAlias);
        }
    }
}
