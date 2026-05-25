using Game.Character.Animation.Requests;
using Game.Character.Components;

namespace Game.Character.Animation.Drivers
{
    internal interface ICharacterAnimationDriver
    {
        int ChannelMask { get; }
        void Evaluate(in CharacterFrameContext ctx, float dt);
        void Drive(in CharacterFrameContext ctx, float dt);
        void OnStarted();
        void OnCompleted();
        void OnInterrupted(AnimationRequest by);
        void OnResumed();
    }
}
