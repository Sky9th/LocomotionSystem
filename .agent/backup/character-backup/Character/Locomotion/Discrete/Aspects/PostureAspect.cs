using Game.Locomotion.Discrete.Interface;
using Game.Character.Input;

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

        public void Update(in SCharacterKinematic kinematic, in SLocomotionMotor motor, in SCharacterInputActions actions)
        {
            // Start from previous frame's posture so that lack of input
            // keeps the current posture.
            EPosture posture;

            // Explicit stand intent has the highest priority.
            if (actions.StandAction.Button.IsRequested)
            {
                posture = EPosture.Standing;
            }
            else if (actions.ProneAction.Button.IsRequested)
            {
                posture = EPosture.Prone;
            }
            else if (actions.CrouchAction.Button.IsRequested)
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
