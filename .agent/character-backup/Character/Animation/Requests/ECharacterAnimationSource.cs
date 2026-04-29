namespace Game.Character.Animation.Requests
{
    /// <summary>
    /// Declares which gameplay subsystem produced the animation request.
    /// </summary>
    public enum ECharacterAnimationSource
    {
        Unknown = 0,
        Locomotion = 1,
        Traversal = 2,
        Ability = 3,
        Interaction = 4,
        Movie = 5,
        HitReact = 6,
    }
}