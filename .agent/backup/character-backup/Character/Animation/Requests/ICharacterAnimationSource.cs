namespace Game.Character.Animation.Requests
{
    /// <summary>
    /// Contract implemented by gameplay subsystems that want to provide
    /// a final animation request for a specific channel.
    /// </summary>
    public interface ICharacterAnimationSource
    {
        ECharacterAnimationSource Source { get; }

        bool TryBuildRequest(
            ECharacterAnimationChannel channel,
            out CharacterAnimationRequest request);
    }
}