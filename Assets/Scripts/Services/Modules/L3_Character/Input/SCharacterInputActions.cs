using System;
using RedDust.GameInput;

namespace RedDust.Character
{
    [Serializable]
    public readonly struct SCharacterInputActions
    {
        public SCharacterInputActions(
            SIActionMove moveAction, SIActionMove lastMoveAction,
            SIActionLook lookAction,
            SIActionCrouch crouchAction, SIActionProne proneAction,
            SIActionWalk walkAction, SIActionRun runAction,
            SIActionSprint sprintAction, SIActionJump jumpAction,
            SIActionStand standAction)
        {
            MoveAction = moveAction;
            LastMoveAction = lastMoveAction;
            LookAction = lookAction;
            CrouchAction = crouchAction;
            ProneAction = proneAction;
            WalkAction = walkAction;
            RunAction = runAction;
            SprintAction = sprintAction;
            JumpAction = jumpAction;
            StandAction = standAction;
        }

        public SIActionMove MoveAction { get; }
        public SIActionMove LastMoveAction { get; }
        public SIActionLook LookAction { get; }
        public SIActionCrouch CrouchAction { get; }
        public SIActionProne ProneAction { get; }
        public SIActionWalk WalkAction { get; }
        public SIActionRun RunAction { get; }
        public SIActionSprint SprintAction { get; }
        public SIActionJump JumpAction { get; }
        public SIActionStand StandAction { get; }

        public static SCharacterInputActions None => new(
            SIActionMove.None, SIActionMove.None,
            SIActionLook.None,
            SIActionCrouch.None, SIActionProne.None,
            SIActionWalk.None, SIActionRun.None,
            SIActionSprint.None, SIActionJump.None,
            SIActionStand.None);
    }
}
