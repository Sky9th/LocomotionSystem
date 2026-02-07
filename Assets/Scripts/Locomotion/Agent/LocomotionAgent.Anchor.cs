using UnityEngine;

/// <summary>
/// Foot placement helpers for LocomotionAgent: sampling left/right foot positions
/// and determining which foot is currently leading along the character's forward axis.
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
    
}
