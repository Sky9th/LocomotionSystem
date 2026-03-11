using UnityEngine;
using Game.Locomotion.Config;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Head look and view direction helpers for Locomotion v2.
    /// Computes clamped local yaw/pitch between body and follow anchor.
    /// </summary>
    internal static class LocomotionHeadLook
    {
        internal static Vector3 EvaluatePlanarHeading(Vector3 viewForward, Transform rootTransform)
        {
            Vector3 forward = viewForward.sqrMagnitude > Mathf.Epsilon
                ? viewForward
                : (rootTransform != null ? rootTransform.forward : Vector3.forward);

            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        internal static Vector2 Evaluate(
            Vector3 viewForward,
            Transform modelRoot,
            Transform rootTransform,
            LocomotionProfile locomotionProfile)
        {
            Vector3 rawForward = viewForward;
            if (rawForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            Quaternion targetRotation = Quaternion.LookRotation(rawForward, Vector3.up);

            Quaternion bodyRotation = rootTransform != null ? rootTransform.rotation : Quaternion.identity;
            if (modelRoot != null)
            {
                Vector3 bodyForward = modelRoot.forward;
                bodyForward.y = 0f;
                if (bodyForward.sqrMagnitude > Mathf.Epsilon)
                {
                    bodyRotation = Quaternion.LookRotation(bodyForward.normalized, Vector3.up);
                }
            }

            Quaternion localDelta = Quaternion.Inverse(bodyRotation) * targetRotation;
            Vector3 euler = localDelta.eulerAngles;

            float yaw = NormalizeAngle180(euler.y);
            float pitch = -NormalizeAngle180(euler.x);

            float maxYaw = locomotionProfile != null ? locomotionProfile.maxHeadYawDegrees : 0f;
            float maxPitch = locomotionProfile != null ? locomotionProfile.maxHeadPitchDegrees : 0f;

            if (maxYaw > 0f)
            {
                yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
            }
            else
            {
                yaw = 0f;
            }

            if (maxPitch > 0f)
            {
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            }
            else
            {
                pitch = 0f;
            }

            return new Vector2(yaw, pitch);
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
