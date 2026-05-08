using Animancer;
using Game.Character.Animation.Components;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    public sealed class FullBodyCharacterAnimationDriver : ICharacterAnimationDriver
    {
        private CharacterAnimationController controller;
        private CharacterAnimationRequest pendingRequest;

        public ECharacterAnimationChannel Channel => ECharacterAnimationChannel.FullBody;
        public EAnimationInterruption Priority => EAnimationInterruption.Ability;
        public ECharacterAnimationRequestMode Mode => ECharacterAnimationRequestMode.OneShot;

        public void Initialize(CharacterAnimationController controller)
        {
            this.controller = controller;
        }

        public void Update(float deltaTime)
        {
        }

        public void SetRequest(CharacterAnimationRequest request)
        {
            if (request == null)
            {
                pendingRequest = null;
                return;
            }

            if (request.Channel != ECharacterAnimationChannel.FullBody)
            {
                return;
            }

            pendingRequest = request;
        }

        public CharacterAnimationRequest BuildRequest()
        {
            if (pendingRequest == null)
            {
                return null;
            }

            CharacterAnimationRequest request = pendingRequest;
            pendingRequest = null;
            return request;
        }

        public void OnInterrupted()
        {
        }

        public void OnResumed()
        {
        }
    }
}