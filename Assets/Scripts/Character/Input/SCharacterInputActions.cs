using System;

namespace Game.Character.Input
{
    [Serializable]
    public readonly struct SCharacterInputActions
    {
        public SCharacterInputActions(
            SMoveIAction moveAction, SMoveIAction lastMoveAction,
            SLookIAction lookAction,
            SCrouchIAction crouchAction, SProneIAction proneAction,
            SWalkIAction walkAction, SRunIAction runAction,
            SSprintIAction sprintAction, SJumpIAction jumpAction,
            SStandIAction standAction)
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

        public SMoveIAction MoveAction { get; }
        public SMoveIAction LastMoveAction { get; }
        public SLookIAction LookAction { get; }
        public SCrouchIAction CrouchAction { get; }
        public SProneIAction ProneAction { get; }
        public SWalkIAction WalkAction { get; }
        public SRunIAction RunAction { get; }
        public SSprintIAction SprintAction { get; }
        public SJumpIAction JumpAction { get; }
        public SStandIAction StandAction { get; }

        public static SCharacterInputActions None => new(
            SMoveIAction.None, SMoveIAction.None,
            SLookIAction.None,
            SCrouchIAction.None, SProneIAction.None,
            SWalkIAction.None, SRunIAction.None,
            SSprintIAction.None, SJumpIAction.None,
            SStandIAction.None);
    }
}
