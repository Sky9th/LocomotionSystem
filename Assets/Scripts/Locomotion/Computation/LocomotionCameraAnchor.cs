using UnityEngine;
using Game.Locomotion.Config;

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
            LocomotionProfile locomotionProfile)
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
            float maxPitch = locomotionProfile != null ? locomotionProfile.maxHeadPitchDegrees : 0f;

            float rotationSpeed = locomotionProfile != null ? locomotionProfile.headLookRotationSpeed : 1f;
            Vector2 lookDelta = lookAction.Delta * rotationSpeed;

            pitch += lookDelta.y;
            if (maxPitch > 0f)
            {
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            }

            euler.x = pitch;
            euler.y += lookDelta.x;

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
