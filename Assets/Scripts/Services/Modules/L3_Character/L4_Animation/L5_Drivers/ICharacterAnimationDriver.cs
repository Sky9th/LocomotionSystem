using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers
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
