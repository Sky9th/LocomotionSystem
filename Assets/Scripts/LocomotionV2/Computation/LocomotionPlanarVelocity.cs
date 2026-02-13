using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Helper for deriving local planar velocity from world-space
    /// velocity, locomotion heading and move input.
    /// </summary>
    internal static class LocomotionPlanarVelocity
    {
        internal static void Evaluate(
            Vector3 worldVelocity,
            Vector3 locomotionHeading,
            SPlayerMoveIAction moveAction,
            ref Vector2 lastMoveInput,
            out Vector2 localVelocity)
        {
            if (moveAction.HasInput)
            {
                lastMoveInput = moveAction.RawInput.normalized;
                localVelocity = lastMoveInput * worldVelocity.magnitude;
                return;
            }

            if (lastMoveInput.sqrMagnitude > Mathf.Epsilon && worldVelocity.sqrMagnitude > Mathf.Epsilon)
            {
                localVelocity = lastMoveInput.normalized * worldVelocity.magnitude;
                return;
            }

            Vector3 planarVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
            if (planarVelocity.sqrMagnitude <= Mathf.Epsilon)
            {
                localVelocity = Vector2.zero;
                return;
            }

            Vector3 forward = locomotionHeading.sqrMagnitude > Mathf.Epsilon
                ? locomotionHeading.normalized
                : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float localY = Vector3.Dot(planarVelocity, forward);
            float localX = Vector3.Dot(planarVelocity, right);

            localVelocity = new Vector2(localX, localY);
        }
    }
}
