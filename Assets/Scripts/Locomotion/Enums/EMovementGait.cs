/// <summary>
/// Gait layer: describes how the character moves on the ground and at what speed tier.
/// Does not include posture information and is meant to be combined with <see cref="EPosture"/>.
/// </summary>
public enum EMovementGait
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Sprint = 3,
    Crawl = 4
}
