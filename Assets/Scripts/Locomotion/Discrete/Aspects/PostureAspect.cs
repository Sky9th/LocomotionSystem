using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Input;

namespace Game.Locomotion.Discrete.Aspects
{
    /// <summary>
    /// Posture layer implementation that derives <see cref="EPosture"/>
    /// from the player's discrete posture intents (stand / crouch / prone).
    /// </summary>
    internal sealed class PostureAspect : ILocomotionAspect<EPosture>
    {
        public EPosture Current { get; private set; } = EPosture.Standing;

        public void Reset(EPosture defaultState)
        {
            Current = defaultState;
        }

        public void Update(in SLocomotionMotor agent, in SLocomotionInputActions actions)
        {
            // Start from previous frame's posture so that lack of input
            // keeps the current posture.
            EPosture posture;

            // Explicit stand intent has the highest priority.
            if (actions.StandAction.HasInput)
            {
                posture = EPosture.Standing;
            }
            else if (actions.ProneAction.HasInput)
            {
                posture = EPosture.Prone;
            }
            else if (actions.CrouchAction.HasInput)
            {
                posture = EPosture.Crouching;
            }
            else
            {
                posture = Current;
            }

            Current = posture;
        }
    }
}
