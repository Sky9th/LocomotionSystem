namespace Game.Character.Animation.Requests
{
    /// <summary>
    /// Describes the lifetime pattern of an animation request.
    /// </summary>
    public enum ECharacterAnimationRequestMode
    {
        Continuous = 0,
        OneShot = 1,
        Latched = 2,
    }
}