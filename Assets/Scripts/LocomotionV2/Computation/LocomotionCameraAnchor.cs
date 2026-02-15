using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Utility methods for updating the camera / follow anchor
    /// rotation based on aggregated look input.
    ///
    /// This is the v2 equivalent of the legacy anchor rotation
    /// helper and is intended to be called from the LocomotionAgent
    /// after sampling SPlayerLookIAction.
    /// </summary>
    internal static class LocomotionCameraAnchor
    {
        internal static void UpdateRotation(
            Transform followAnchor,
            SLookIAction lookAction,
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
