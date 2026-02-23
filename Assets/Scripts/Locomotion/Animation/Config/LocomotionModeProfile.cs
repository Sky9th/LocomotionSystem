using UnityEngine;

namespace Game.Locomotion.Animation.Config
{
    /// <summary>
    /// ScriptableObject describing locomotion tuning for a specific
    /// posture + gait combination, primarily used to drive turn speeds.
    /// This is v2-only and should not be used by the legacy Locomotion.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionModeProfile",
        menuName = "Game/Locomotion/Mode Profile")]
    public sealed class LocomotionModeProfile : ScriptableObject
    {
        [Header("Locomotion Mode Key")]
        [SerializeField] private EPostureState posture = EPostureState.Standing;
        [SerializeField] private EMovementGait gait = EMovementGait.Walk;

        [Header("Turn Speeds (deg/sec)")]
        [SerializeField, Min(0f)] private float movingTurnSpeed = 360f;

        [Header("Turn Angles (deg)")]
        [SerializeField, Range(0f, 180f)] private float enterAngle = 90f;
        [SerializeField, Range(0f, 180f)] private float exitAngle = 20f;

        /// <summary>Posture this mode is configured for.</summary>
        public EPostureState Posture => posture;

        /// <summary>Gait this mode is configured for.</summary>
        public EMovementGait Gait => gait;

        /// <summary>Turn speed when moving (walk/run/etc.).</summary>
        public float MovingTurnSpeed => movingTurnSpeed;

        /// <summary>Angle threshold for entering a dedicated turn animation.</summary>
        public float EnterAngle => enterAngle;

        /// <summary>Angle threshold for exiting a dedicated turn animation.</summary>
        public float ExitAngle => exitAngle;
    }
}
