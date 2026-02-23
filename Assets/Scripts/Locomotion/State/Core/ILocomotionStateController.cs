using UnityEngine;

namespace Game.Locomotion.State.Core
{
    /// <summary>
    /// High-level locomotion controller abstraction for the v2 state module.
    ///
    /// Represents a complete locomotion rule set for a given archetype
    /// (Human, Zombie, etc.) and exposes a single entry point to evaluate
    /// discrete locomotion state from a <see cref="SLocomotionStateContext"/>.
    /// </summary>
    internal interface ILocomotionStateController
    {
        /// <summary>Latest evaluated discrete locomotion state.</summary>
        SLocomotionDiscreteState CurrentState { get; }

        /// <summary>Current high-level locomotion phase (Grounded / Airborne ...).</summary>
        ELocomotionState CurrentPhase { get; }

        /// <summary>Current posture (Standing / Crouching / Prone ...).</summary>
        EPostureState CurrentPosture { get; }

        /// <summary>Current movement gait (Idle / Walk / Run / Sprint ...).</summary>
        EMovementGait CurrentGait { get; }

        /// <summary>Current locomotion condition (Normal / Injured ...).</summary>
        ELocomotionCondition CurrentCondition { get; }

        /// <summary>
        /// Evaluate a full locomotion state frame (discrete state +
        /// turning information) for the supplied context.
        /// </summary>
        SLocomotionStateFrame Evaluate(in SLocomotionStateContext context, float deltaTime);

    }
}
