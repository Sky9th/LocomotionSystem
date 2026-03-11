using System;
using UnityEngine;

/// <summary>
/// Immutable snapshot describing the currently active gameplay camera.
/// Shared via GameContext so any subsystem can reason about camera and anchor poses deterministically.
/// </summary>
[Serializable]
public struct SCameraContext
{
    public SCameraContext(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        Vector3 anchorPosition,
        Quaternion anchorRotation,
        Vector2 lookDelta)
    {
        CameraPosition = cameraPosition;
        CameraRotation = cameraRotation;
        AnchorPosition = anchorPosition;
        AnchorRotation = anchorRotation;
        LookDelta = lookDelta;
    }

    public SCameraContext(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        Vector2 lookDelta)
    {
        CameraPosition = cameraPosition;
        CameraRotation = cameraRotation;
        AnchorPosition = cameraPosition;
        AnchorRotation = cameraRotation;
        LookDelta = lookDelta;
    }

    /// <summary>
    /// World-space pose of the currently active rendered camera.
    /// </summary>
    public Vector3 CameraPosition { get; }
    public Quaternion CameraRotation { get; }

    /// <summary>
    /// Authoritative gameplay anchor pose (used for heading/look).
    /// </summary>
    public Vector3 AnchorPosition { get; }
    public Quaternion AnchorRotation { get; }

    /// <summary>
    /// Applied look delta for this frame (X = yaw, Y = pitch), after CameraManager tuning.
    /// </summary>
    public Vector2 LookDelta { get; }

    public float YawDelta => LookDelta.x;
    public float PitchDelta => LookDelta.y;
}
