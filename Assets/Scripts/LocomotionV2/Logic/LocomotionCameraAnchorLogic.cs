using UnityEngine;

namespace Game.Locomotion.Logic
{
    /// <summary>
    /// Camera / follow anchor rotation helpers for Locomotion v2.
    ///
    /// Ports the legacy LocomotionAgent anchor rotation so that
    /// look input (mouse/controller deltas) can rotate the follow
    /// anchor each frame.
    /// </summary>
    internal static class LocomotionCameraAnchorLogic
    {
        internal static void UpdateAnchorRotation(
            Transform followAnchor,
            SPlayerLookIAction lookAction,
            LocomotionConfigProfile config)
        {
            if (followAnchor == null)
            {
                return;
            }

            if (!lookAction.HasDelta)
            {
                return;
            }

            Vector3 euler = followAnchor.rotation.eulerAngles;
            euler.z = 0f;

            float pitch = NormalizeAngle180(euler.x);
            float maxPitch = config != null ? config.MaxHeadPitchDegrees : 0f;

            pitch += lookAction.Delta.y;
            if (maxPitch > 0f)
            {
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            }

            euler.x = pitch;
            euler.y += lookAction.Delta.x;

            followAnchor.rotation = Quaternion.Euler(euler);
        }

        private static float NormalizeAngle180(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }
            else if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
    }
}
