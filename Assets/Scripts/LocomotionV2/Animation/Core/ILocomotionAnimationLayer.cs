namespace Game.Locomotion.Animation.Core
{
    /// <summary>
    /// Base interface for all locomotion animation layers.
    /// Each layer is responsible for a single concern such as
    /// base movement, turning in place, airborne, upper body, etc.
    /// </summary>
    internal interface ILocomotionAnimationLayer
    {
        void Update(in LocomotionAnimationContext context);
    }
}
