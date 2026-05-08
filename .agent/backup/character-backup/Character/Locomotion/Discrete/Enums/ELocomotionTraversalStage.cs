/// <summary>
/// Lifecycle stage for a traversal request or execution.
/// </summary>
public enum ELocomotionTraversalStage
{
    Idle = 0,
    Requested = 1,
    Committed = 2,
    Completed = 3,
    Canceled = 4
}