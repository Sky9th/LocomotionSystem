using System;

namespace Game.Locomotion.Discrete.Structs
{
    /// <summary>
    /// Bundles high-level locomotion state, posture, gait and condition
    /// into a single immutable value type.
    /// </summary>
    [Serializable]
    public readonly struct SLocomotionDiscrete
    {
        public SLocomotionDiscrete(
            ELocomotionPhase phase,
            EPosture posture,
            EMovementGait gait,
            ELocomotionCondition condition,
            bool isTurning)
        {
            Phase = phase;
            Posture = posture;
            Gait = gait;
            Condition = condition;
            IsTurning = isTurning;
        }

        public ELocomotionPhase Phase { get; }
        public EPosture Posture { get; }
        public EMovementGait Gait { get; }
        public ELocomotionCondition Condition { get; }
        public bool IsTurning { get; }
    }
}
