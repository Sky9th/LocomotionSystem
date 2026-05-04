using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    internal interface ICharacterAnimationDriver
    {
        int ChannelMask { get; }
        void Drive(in SCharacterSnapshot snapshot, float dt);
        void OnInterrupted(AnimationRequest by);
        void OnResumed();
    }
}
