using UnityEngine;
using Game.Locomotion.Config;
using Game.Locomotion.Input;
using Game.Locomotion.Discrete.Structs;

namespace Game.Locomotion.Discrete.Interface
{
    /// <summary>
    /// High-level locomotion controller abstraction for the v2 state module.
    ///
    /// Represents a complete locomotion rule set for a given archetype
    /// (Human, Zombie, etc.) and exposes a single entry point to evaluate
    /// discrete locomotion state from the current agent probes and input intent.
    /// </summary>
    internal interface ILocomotionCoordinator
    {
        /// <summary>Latest evaluated discrete locomotion state.</summary>
        SLocomotionDiscrete CurrentState { get; }

        /// <summary>Latest evaluated traversal snapshot.</summary>
        SLocomotionTraversal CurrentTraversal { get; }

        /// <summary>Current high-level locomotion phase (Grounded / Airborne ...).</summary>
        ELocomotionPhase CurrentPhase { get; }

        /// <summary>Current posture (Standing / Crouching / Prone ...).</summary>
        EPosture CurrentPosture { get; }

        /// <summary>Current movement gait (Idle / Walk / Run / Sprint ...).</summary>
        EMovementGait CurrentGait { get; }

        /// <summary>Current locomotion condition (Normal / Injured ...).</summary>
        ELocomotionCondition CurrentCondition { get; }

        /// <summary>
        /// Evaluate the discrete locomotion state.
        /// </summary>
        SLocomotionDiscrete Evaluate(
            in SLocomotionMotor agent,
            LocomotionProfile profile,
            in SLocomotionInputActions actions,
            float deltaTime);

    }
}
