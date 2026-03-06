using UnityEngine;

namespace Game.Locomotion.Config
{
    /// <summary>
    /// Scriptable configuration describing the core locomotion capabilities
    /// of a character: movement speeds, acceleration, ground limits and
    /// basic climb heights. This asset is consumed by the locomotion
    /// simulation and state logic, while animation-specific tuning lives
    /// in LocomotionAnimationProfile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionProfile",
        menuName = "Game/Locomotion/Locomotion Profile")]
    public sealed class LocomotionProfile : ScriptableObject
    {
        [Header("Motion Settings")]
        [Min(0f)] public float moveSpeed = 4f;
        [Min(0f)] public float acceleration = 5f;

        [Header("Ground Settings")]
        [Range(0f, 89f)] public float maxGroundSlopeAngle = 55f;

        [Header("Ground Probe")]
        [Min(0f)] public float groundRayLength = 0.1f;
        public LayerMask groundLayerMask = ~0;

        [Header("Ground Lock")]
        public bool enableGroundLocking = true;
        public float groundLockVerticalOffset = 0f;

        [Header("Head Look Limits")]
        [Range(0f, 90f)] public float maxHeadYawDegrees = 75f;
        [Range(0f, 90f)] public float maxHeadPitchDegrees = 75f;
        [Min(0f)] public float headLookRotationSpeed = 1f;

        [Header("Turn In Place")]
        [Range(0f, 180f)] public float turnEnterAngle = 65f;
        [Range(0f, 180f)] public float turnExitAngle = 20f;
        [Min(0f)] public float turnDebounceDuration = 0.25f;
        [Range(0f, 45f)] public float lookStabilityAngle = 2f;
        [Min(0f)] public float lookStabilityDuration = 0.15f;
        [Range(0f, 25f)] public float turnCompletionAngle = 5f;
    }
}
