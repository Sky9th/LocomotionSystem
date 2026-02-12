/// <summary>
/// High-level locomotion state used to roughly categorize the current behaviour.
/// Detailed posture (standing/crouching/prone), gait (walk/run/sprint/crawl) and condition
/// are represented by EPostureState, EMovementGait and ELocomotionCondition.
/// </summary>
public enum ELocomotionState
{
    /// <summary>On the ground with no significant movement (typically corresponds to Idle).</summary>
    GroundedIdle = 0,

    /// <summary>On the ground and moving (Walk / Run / Sprint / Crawl, etc.).</summary>
    GroundedMoving = 1,

    /// <summary>In the air: jumping, ascending or falling.</summary>
    Airborne = 2,

    /// <summary>Transition phase right after landing.</summary>
    Landing = 3
}
