using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Kinematic
{
    internal static class CharacterHeadLook
    {
        internal static Vector3 EvaluatePlanarHeading(Vector3 viewForward, Transform rootTransform)
        {
            Vector3 heading = viewForward;
            heading.y = 0f;
            if (heading.sqrMagnitude > Mathf.Epsilon) return heading.normalized;

            heading = rootTransform.forward;
            heading.y = 0f;
            return heading.sqrMagnitude > Mathf.Epsilon ? heading.normalized : Vector3.forward;
        }

        internal static Vector2 Evaluate(Vector3 viewForward, Transform modelRoot, Transform rootTransform,
            float maxHeadYaw, float maxHeadPitch)
        {
            var targetRotation = viewForward.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(viewForward)
                : rootTransform.rotation;

            var bodyRotation = modelRoot != null ? modelRoot.rotation : rootTransform.rotation;
            var delta = Quaternion.Inverse(bodyRotation) * targetRotation;
            var euler = delta.eulerAngles;

            float yaw = NormalizeAngle180(euler.y);
            float pitch = -NormalizeAngle180(euler.x);

            float maxYaw = Mathf.Max(1e-3f, maxHeadYaw);
            float maxPitch = Mathf.Max(1e-3f, maxHeadPitch);

            float normYaw = Mathf.Clamp(yaw / maxYaw, -1f, 1f);
            float normPitch = Mathf.Clamp(pitch / maxPitch, -1f, 1f);
            return new Vector2(normYaw, normPitch);
        }

        private static float NormalizeAngle180(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
