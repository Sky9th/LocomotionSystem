using UnityEngine;

namespace RedDust.Character.Animation
{
    /// <summary>
    /// Scriptable configuration describing how locomotion animation
    /// should respond to the locomotion snapshot. This asset stores
    /// thresholds and tuning values only – concrete animations are
    /// resolved via Animancer transition libraries and alias keys.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionAnimationConfigSO",
        menuName = "RedDust/Animation/Locomotion Animation Config")]
    public sealed class LocomotionAnimationConfigSO : ScriptableObject
    {
        [Header("Head Look")]
        [Min(0f)] public float headLookSmoothingSpeed = 5f;

        [Header("Turn Speeds By Mode")]
        public AnimationModeConfigSO[] modeProfiles;
        [Min(0f)] public float defaultInPlaceTurnSpeed = 360f;
        [Min(0f)] public float defaultMovingTurnSpeed = 720f;

        [Header("Airborne")]
        public float landDistanceThreshold = 0.5f;

        [Header("Landing Levels")]
        [Tooltip("坠落距离低于此值不触发落地动画，直接回 Idle")]
        public float landMinFallDistance = 0.2f;
        public float landLightMaxFallDistance = 1.0f;
        public float landMediumMaxFallDistance = 3.0f;
        public float landLightTriggerDistance = 0.3f;
        public float landMediumTriggerDistance = 0.6f;
        public float landHardTriggerDistance = 1.0f;

        /// <summary>
        /// Returns the configured turn speed in degrees per second
        /// for the given posture and gait. If no matching mode is
        /// found, a default in-place or moving speed is used.
        /// </summary>
        public float GetTurnSpeed(EPosture posture, EMovementGait gait, bool isMoving)
        {
            if (!isMoving)
            {
                return defaultInPlaceTurnSpeed;
            }

            if (modeProfiles != null)
            {
                for (int i = 0; i < modeProfiles.Length; i++)
                {
                    AnimationModeConfigSO mode = modeProfiles[i];
                    if (mode == null)
                    {
                        continue;
                    }

                    if (mode.Posture == posture && mode.Gait == gait)
                    {
                        return mode.MovingTurnSpeed;
                    }
                }
            }

            return defaultMovingTurnSpeed;
        }

        // Note: modeProfiles & GetTurnSpeed are animation-only tuning for
        // model rotation speed. All core locomotion thresholds now live in
        // LocomotionProfileSO.
    }
}
