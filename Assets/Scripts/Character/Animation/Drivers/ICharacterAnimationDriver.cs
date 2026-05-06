using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    internal interface ICharacterAnimationDriver
    {
        int ChannelMask { get; }
        void Evaluate(in SCharacterSnapshot snapshot, float dt);
        void Drive(in SCharacterSnapshot snapshot, float dt);
        void OnStarted();
        void OnCompleted();
        void OnInterrupted(AnimationRequest by);
        void OnResumed();
    }
}
