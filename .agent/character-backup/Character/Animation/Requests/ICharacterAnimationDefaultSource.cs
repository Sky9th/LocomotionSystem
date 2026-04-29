namespace Game.Character.Animation.Requests
{
    /// <summary>
    /// Optional contract for subsystems that provide the default animation
    /// request of a channel when no override request is active.
    /// </summary>
    public interface ICharacterAnimationDefaultSource
    {
        bool TryBuildDefaultRequest(
            ECharacterAnimationChannel channel,
            out CharacterAnimationRequest request);
    }
}