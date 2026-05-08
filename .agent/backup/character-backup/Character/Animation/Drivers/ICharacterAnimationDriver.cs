using Game.Character.Animation.Components;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    public interface ICharacterAnimationDriver
    {
        ECharacterAnimationChannel Channel { get; }
        EAnimationInterruption Priority { get; }
        ECharacterAnimationRequestMode Mode { get; }

        void Initialize(CharacterAnimationController controller);
        void Update(float deltaTime);

        CharacterAnimationRequest BuildRequest();

        void OnInterrupted();
        void OnResumed();
    }
}