using Game.Locomotion.State.Core;

namespace Game.Locomotion.State.Layers
{
    /// <summary>
    /// Posture layer implementation that derives <see cref="EPostureState"/>
    /// from the player's discrete posture intents (stand / crouch / prone).
    /// </summary>
    internal sealed class PostureStateLayer : ILocomotionStateLayer<EPostureState>
    {
        public EPostureState Current { get; private set; } = EPostureState.Standing;

        public void Reset(EPostureState defaultState)
        {
            Current = defaultState;
        }

        public void Update(in LocomotionStateContext context)
        {
            // Start from previous frame's posture so that lack of input
            // keeps the current posture.
            EPostureState posture = context.PreviousState.Posture;

            // Explicit stand intent has the highest priority.
            if (context.StandAction.HasInput)
            {
                posture = EPostureState.Standing;
            }
            else if (context.ProneAction.HasInput)
            {
                posture = EPostureState.Prone;
            }
            else if (context.CrouchAction.HasInput)
            {
                posture = EPostureState.Crouching;
            }

            Current = posture;
        }
    }
}
