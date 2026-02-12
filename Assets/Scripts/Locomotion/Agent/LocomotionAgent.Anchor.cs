using UnityEngine;

/// <summary>
/// Anchor rotation and alignment helpers for LocomotionAgent.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{
    private void UpdateAnchorRotation()
    {
        if (followAnchor != null)
        {
            Vector3 euler = followAnchor.rotation.eulerAngles;
            euler.z = 0f;
            float pitch = NormalizeAngle180(euler.x);
            pitch = Mathf.Clamp(pitch + LastLookAction.Delta.y, -config.MaxHeadPitchDegrees, config.MaxHeadPitchDegrees);
            euler.x = pitch;
            euler.y += LastLookAction.Delta.x;
            followAnchor.rotation = Quaternion.Euler(euler);
        }
    }

    private void AlignPlayerToModel()
    {
        var worldPos = modelRoot.position;
        transform.position = worldPos;
        modelRoot.localPosition = Vector3.zero;
    }
    
}
