using RedDust.Core;
using UnityEngine;
using RedDust.Character;
using RedDust.Character.Pathfinding;

namespace RedDust.Character.Director
{
    internal sealed class PlayerDirector : ICharacterDirector
    {
        private readonly Transform modelRoot;
        private readonly PlayerInput input;
        private readonly PathfindingAgent agent;

        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;

        internal PlayerDirector(EventHub channels, Transform modelRoot, CharacterActor owner)
        {
            this.modelRoot = modelRoot;
            input = new PlayerInput(channels);
            channels.RegisterListener(input);
            agent = owner.GetComponent<PathfindingAgent>();
        }

        public SCharacterIntent Evaluate()
        {
            ProcessClickToMove();

            bool hasActivePath = agent != null && agent.HasPath && !agent.HasReachedDestination;

            var intent = new SCharacterIntent(
                ComputeHeading(),
                ComputeAim(),
                ResolveGait(),
                ResolvePosture(),
                false,
                agent?.DesiredVelocity ?? Vector3.zero,
                hasActivePath);

            if (agent != null && agent.HasPath)
                input.SecondaryRequested = false;
            input.ClearFrameSignals();

            return intent;
        }

        private void ProcessClickToMove()
        {
            if (agent == null) return;
            if (!input.SecondaryRequested) return;
            if (!input.HasMouseGround) return;

            agent.SetDestination(input.MouseGroundPosition);
            currentGait = EMovementGait.Run;
        }

        private Vector3 ComputeAim()
        {
            if (input.HasMouseGround)
            {
                var dir = input.MouseGroundPosition - modelRoot.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > Mathf.Epsilon)
                    return dir.normalized;
            }
            return modelRoot.forward;
        }

        private Vector3 ComputeHeading()
        {
            if (agent != null && agent.HasPath)
            {
                var desired = agent.DesiredVelocity;
                if (desired.sqrMagnitude > Mathf.Epsilon)
                    return desired.normalized;
            }
            return modelRoot.forward;
        }

        private EMovementGait ResolveGait()
        {
            bool hasPath = agent != null && agent.HasPath && !agent.HasReachedDestination;
            bool wantsMove = input.SecondaryRequested || hasPath;

            if (wantsMove)
            {
                if (input.SprintRequested)
                    currentGait = currentGait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;

                if (currentGait == EMovementGait.Idle)
                    currentGait = EMovementGait.Run;
            }
            else
            {
                currentGait = EMovementGait.Idle;
            }

            return currentGait;
        }

        private EPosture ResolvePosture()
        {
            if (input.StandRequested)
                currentPosture = EPosture.Standing;
            else if (input.ProneRequested)
                currentPosture = EPosture.Prone;
            else if (input.CrouchRequested)
                currentPosture = EPosture.Crouching;

            return currentPosture;
        }
    }
}
