using UnityEngine;
using RedDust.Character;
using RedDust.Character.Pathfinding;

namespace RedDust.Character.Director
{
    internal sealed class PlayerDirector : ICharacterDirector
    {
        private readonly Transform ownerTransform;
        private readonly PlayerInputReceiver receiver;
        private readonly PathfindingAgent agent;

        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;

        internal PlayerDirector(CharacterActor owner)
        {
            ownerTransform = owner.transform;
            receiver = new PlayerInputReceiver(owner);
            agent = owner.GetComponent<PathfindingAgent>();
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
            ProcessClickToMove();

            return new SCharacterIntent(
                ComputeLocomotionHeading(),
                ComputeAimDirection(),
                ResolveDesiredGait(),
                ResolveDesiredPosture(),
                false, // 攀爬/跳跃由寻路系统决定，非玩家输入
                ComputeSpeedMultiplier());
        }

        private void ProcessClickToMove()
        {
            if (agent == null) return;
            if (!receiver.SecondaryInteractAction.Button.IsRequested) return;
            if (!receiver.HasMouseGround) return;

            agent.SetDestination(receiver.MouseGroundPosition);
            currentGait = EMovementGait.Run;
        }

        private float ComputeSpeedMultiplier()
        {
            return agent != null ? agent.DesiredSpeedMultiplier : 1f;
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
            if (agent != null && agent.HasPath && !agent.HasReachedDestination)
                return agent.PathDirection;

            return ComputeAimDirection(); // fallback: aim = locomotion when not pathfinding
        }

        private EMovementGait ResolveDesiredGait()
        {
            bool hasPathfindingIntent = agent != null && agent.HasPath && !agent.HasReachedDestination;

            if (!receiver.MoveAction.HasInput && !hasPathfindingIntent)
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
