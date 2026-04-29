namespace Game.Character.Animation.Requests
{
    /// <summary>
    /// External playback state reported by the animation system for a request.
    /// </summary>
    public enum ECharacterAnimationPlaybackState
    {
        None = 0,
        Pending = 1,
        Accepted = 2,
        Playing = 3,
        Completed = 4,
        Rejected = 5,
        Interrupted = 6,
    }
}