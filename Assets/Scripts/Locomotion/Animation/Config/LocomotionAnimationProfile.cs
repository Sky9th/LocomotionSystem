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
        [Header("Head Look")]
        [Min(0f)] public float headLookSmoothingSpeed = 540f;

        [Header("Turn Speeds By Mode")]
        public LocomotionModeProfile[] modeProfiles;
        [Min(0f)] public float defaultInPlaceTurnSpeed = 360f;
        [Min(0f)] public float defaultMovingTurnSpeed = 360f;

        /// <summary>
        /// Returns the configured turn speed in degrees per second
        /// for the given posture and gait. If no matching mode is
        /// found, a default in-place or moving speed is used.
        /// </summary>
        public float GetTurnSpeed(EPostureState posture, EMovementGait gait, bool isMoving)
        {
            if (modeProfiles != null && isMoving)
            {
                for (int i = 0; i < modeProfiles.Length; i++)
                {
                    LocomotionModeProfile mode = modeProfiles[i];
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
        // LocomotionProfile.
    }
}
