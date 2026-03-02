using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.State.Layers;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseTurnInPlaceState : BaseLayerFsmState
    {
        private StringAsset selectedAlias;

        public BaseTurnInPlaceState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                if (Owner.AliasProfile == null)
                {
                    return false;
                }

                SLocomotion snapshot = Owner.Snapshot;
                if (snapshot.State != ELocomotionState.GroundedIdle)
                {
                    return false;
                }

                if (!snapshot.IsTurning)
                {
                    return false;
                }

                return true;
            }
        }

        public override bool CanExitState
        {
            get
            {
                // Allow leaving the turn segment if the animation is complete
                // or if higher-level logic indicates we started moving.
                if (Owner.Snapshot.State == ELocomotionState.GroundedMoving)
                {
                    return true;
                }

                if (Mathf.Abs(Owner.Snapshot.TurnAngle) < Owner.context.LocomotionProfile.turnExitAngle)
                {
                    Logger.Log($"Allowing early exit from turn since turn angle {Owner.Snapshot.TurnAngle} is below exit threshold.");
                    return true;
                }

                return Owner.HasCurrentAnimationCompleted();
            }
        }

        public override void OnEnterState()
        {
            selectedAlias = ResolveTurnAlias(Owner.AliasProfile, Owner.Snapshot.TurnAngle);
            Owner.Play(selectedAlias);
        }

        public override void OnExitState()
        {
            Logger.Log($"Exiting {nameof(BaseTurnInPlaceState)}");
        }

        public override void Tick()
        {
            // If we started moving, exit immediately (the owning layer will
            // pick the correct locomotion animation for movement).
            if (Owner.Snapshot.State == ELocomotionState.GroundedMoving)
            {
                Logger.Log("Exiting turn since we started moving.");
                Owner.TrySetState(BaseStateKey.IdleToMoving);
                return;
            }

            // Once the turn clip finishes, exit back to idle.
            if (Owner.HasCurrentAnimationCompleted())
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            // Keep playing the selected alias while locked.
            if (selectedAlias != null)
            {
                Owner.PlayIfChanged(selectedAlias);
            }
        }

        private static StringAsset ResolveTurnAlias(AnimancerStringProfile alias, float angle)
        {
            if (alias == null)
            {
                return null;
            }

            bool isRightTurn = angle > 0f;

            if (isRightTurn)
            {
                return alias.turnInPlace90R;
            }

            return alias.turnInPlace90L;
        }
    }
}
