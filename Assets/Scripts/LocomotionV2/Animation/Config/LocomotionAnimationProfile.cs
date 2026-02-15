using UnityEngine;

namespace Game.Locomotion.Animation.Config
{
    /// <summary>
    /// Scriptable configuration describing how locomotion animation
    /// should respond to the locomotion snapshot. This asset stores
    /// thresholds and tuning values only – concrete animations are
    /// resolved via Animancer transition libraries and alias keys.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionAnimationProfile",
        menuName = "Game/Locomotion/Animation Profile")]
    public sealed class LocomotionAnimationProfile : ScriptableObject
    {
        [Header("Movement Speeds")]
        [Min(0f)] public float walkSpeedThreshold = 1f;
        [Min(0f)] public float runSpeedThreshold = 3f;
        [Min(0f)] public float sprintSpeedThreshold = 5f;

        [Header("Head Look Limits")]
        [Range(0f, 90f)] public float maxHeadYawDegrees = 75f;
        [Range(0f, 90f)] public float maxHeadPitchDegrees = 75f;
        [Min(0f)] public float headLookSmoothingSpeed = 540f;

        [Header("Turn In Place")]
        [Range(0f, 180f)] public float turnEnterAngle = 65f;
        [Range(0f, 180f)] public float turnExitAngle = 20f;
        [Min(0f)] public float turnDebounceDuration = 0.25f;
        [Range(0f, 45f)] public float lookStabilityAngle = 2f;
        [Min(0f)] public float lookStabilityDuration = 0.15f;
        [Range(0f, 25f)] public float turnCompletionAngle = 5f;
        [Min(0f)] public float walkTurnSpeed = 360f;

        [Header("Airborne")]
        public float hardLandingVelocity = -8f;
    }
}
