using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers
{
    internal interface ICharacterAnimationDriver
    {
        int ChannelMask { get; }
        void Evaluate(in SCharacterFrameContext ctx, float dt);
        void Drive(in SCharacterFrameContext ctx, float dt);
        void OnStarted(AnimationRequest request);
        void OnCompleted();
        void OnInterrupted(AnimationRequest by);
        void OnResumed();
    }
}
