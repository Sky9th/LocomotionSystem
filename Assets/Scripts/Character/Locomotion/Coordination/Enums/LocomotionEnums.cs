public enum ELocomotionPhase
{
    GroundedIdle = 0,
    GroundedMoving = 1,
    Airborne = 2,
    Landing = 3
}

public enum EPosture
{
    Standing = 0,
    Crouching = 1,
    Prone = 2
}

public enum EMovementGait
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Sprint = 3,
    Crawl = 4
}

public enum ELocomotionCondition
{
    Normal = 0,
    InjuredLight = 1,
    InjuredHeavy = 2
}

public enum ELocomotionTraversalType
{
    None = 0,
    Climb = 1,
    Vault = 2,
    StepOver = 3
}

public enum ELocomotionTraversalStage
{
    Idle = 0,
    Requested = 1,
    Committed = 2,
    Completed = 3,
    Canceled = 4
}
