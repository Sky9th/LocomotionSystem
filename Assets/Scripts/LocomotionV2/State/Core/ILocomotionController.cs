using UnityEngine;

namespace Game.Locomotion.State.Core
{
    /// <summary>
    /// High-level locomotion controller abstraction for the v2 state module.
    ///
    /// Represents a complete locomotion rule set for a given archetype
    /// (Human, Zombie, etc.) and exposes a single entry point to evaluate
    /// discrete locomotion state from a <see cref="LocomotionStateContext"/>.
    /// </summary>
    internal interface ILocomotionController
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

        /// <summary>Current signed planar turn angle in degrees.</summary>
        float CurrentTurnAngle { get; }

        /// <summary>Whether the character is currently performing an in-place turn.</summary>
        bool IsTurningInPlace { get; }

        /// <summary>
        /// Evaluate and return a new discrete locomotion state for the
        /// supplied context. Implementations are expected to cache the
        /// resulting state in <see cref="CurrentState"/> and update any
        /// additional state-related outputs such as turning information.
        /// </summary>
        SLocomotionDiscreteState UpdateDiscreteState(in LocomotionStateContext context, float deltaTime);
    }
}
