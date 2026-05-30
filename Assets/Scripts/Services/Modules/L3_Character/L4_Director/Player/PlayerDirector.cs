using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Director
{
    internal sealed class PlayerDirector : ICharacterDirector
    {
        private readonly Transform ownerTransform;
        private readonly PlayerInputReceiver receiver;

        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;

        internal PlayerDirector(CharacterActor owner)
        {
            ownerTransform = owner.transform;
            receiver = new PlayerInputReceiver(owner);
        }

        internal void Subscribe() => receiver.Subscribe();
        internal void Unsubscribe() => receiver.Unsubscribe();
        internal void Reset()
        {
            receiver.Reset();
            currentGait = EMovementGait.Idle;
            currentPosture = EPosture.Standing;
        }

        public SCharacterIntent Evaluate()
        {
            return new SCharacterIntent(
                ComputeLocomotionHeading(),
                ComputeAimDirection(),
                ResolveDesiredGait(),
                ResolveDesiredPosture(),
                false); // 攀爬/跳跃由寻路系统决定，非玩家输入
        }

        private Vector3 ComputeAimDirection()
        {
            if (receiver.HasMouseGround)
            {
                var dir = receiver.MouseGroundPosition - ownerTransform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > Mathf.Epsilon)
                    return dir.normalized;
            }
            return ownerTransform.forward;
        }

        private Vector3 ComputeLocomotionHeading()
        {
            return ComputeAimDirection(); // TODO Phase 4 — separate from aim
        }

        private EMovementGait ResolveDesiredGait()
        {
            if (!receiver.MoveAction.HasInput)
            {
                currentGait = EMovementGait.Idle;
                return currentGait;
            }

            if (receiver.SprintAction.Button.IsRequested)
                currentGait = currentGait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;

            if (currentGait == EMovementGait.Idle)
                currentGait = EMovementGait.Run;

            return currentGait;
        }

        private EPosture ResolveDesiredPosture()
        {
            if (receiver.StandAction.Button.IsRequested)
                currentPosture = EPosture.Standing;
            else if (receiver.ProneAction.Button.IsRequested)
                currentPosture = EPosture.Prone;
            else if (receiver.CrouchAction.Button.IsRequested)
                currentPosture = EPosture.Crouching;

            return currentPosture;
        }
    }
}
