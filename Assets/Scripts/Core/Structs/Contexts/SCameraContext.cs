using System;
using UnityEngine;

/// <summary>
/// Immutable snapshot describing the currently active gameplay camera.
/// Shared via GameContext so any subsystem can reason about camera pose/FOV deterministically.
/// </summary>
[Serializable]
public struct SCameraContext
{
    public SCameraContext(
        Vector3 position,
        Quaternion rotation,
        float fieldOfView,
        float nearClipPlane,
        float farClipPlane,
        bool isOrthographic,
        float orthographicSize)
    {
        Position = position;
        Rotation = rotation;
        FieldOfView = fieldOfView;
        NearClipPlane = nearClipPlane;
        FarClipPlane = farClipPlane;
        IsOrthographic = isOrthographic;
        OrthographicSize = orthographicSize;
    }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public float FieldOfView { get; }
    public float NearClipPlane { get; }
    public float FarClipPlane { get; }
    public bool IsOrthographic { get; }
    public float OrthographicSize { get; }

    public Vector3 Forward => Rotation * Vector3.forward;
    public Vector3 Up => Rotation * Vector3.up;
}
