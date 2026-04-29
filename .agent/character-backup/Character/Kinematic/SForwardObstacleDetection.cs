using System;
using UnityEngine;

/// <summary>
/// Immutable forward obstacle detection snapshot.
/// Contains probe configuration, raw hit data and derived traversal judgments.
/// </summary>
[Serializable]
public readonly struct SForwardObstacleDetection
{
    public SForwardObstacleDetection(
        bool hasHit,
        bool hasTopSurface,
        bool isSlope,
        bool isObstacle,
        bool canClimb,
        bool canVault,
        bool canStepOver,
        float distance,
        float obstacleHeight,
        Vector3 point,
        Vector3 normal,
        Vector3 topPoint,
        Vector3 topNormal,
        float surfaceAngle,
        Vector3 direction,
        Collider collider)
    {
        HasHit = hasHit;
        HasTopSurface = hasTopSurface;
        IsSlope = isSlope;
        IsObstacle = isObstacle;
        CanClimb = canClimb;
        CanVault = canVault;
        CanStepOver = canStepOver;
        Distance = distance;
        ObstacleHeight = obstacleHeight;
        Point = point;
        Normal = normal;
        TopPoint = topPoint;
        TopNormal = topNormal;
        SurfaceAngle = surfaceAngle;
        Direction = direction;
        Collider = collider;
    }

    public bool HasHit { get; }
    public bool HasTopSurface { get; }
    public bool IsSlope { get; }
    public bool IsObstacle { get; }
    public bool CanClimb { get; }
    public bool CanVault { get; }
    public bool CanStepOver { get; }
    public float Distance { get; }
    public float ObstacleHeight { get; }
    public Vector3 Point { get; }
    public Vector3 Normal { get; }
    public Vector3 TopPoint { get; }
    public Vector3 TopNormal { get; }
    public float SurfaceAngle { get; }
    public Vector3 Direction { get; }
    public Collider Collider { get; }
    public int HitLayer => Collider != null ? Collider.gameObject.layer : -1;

    public static SForwardObstacleDetection None => new SForwardObstacleDetection(
        hasHit: false,
        hasTopSurface: false,
        isSlope: false,
        isObstacle: false,
        canClimb: false,
        canVault: false,
        canStepOver: false,
        distance: float.PositiveInfinity,
        obstacleHeight: float.PositiveInfinity,
        point: Vector3.zero,
        normal: Vector3.zero,
        topPoint: Vector3.zero,
        topNormal: Vector3.up,
        surfaceAngle: 90f,
        direction: Vector3.forward,
        collider: null);
}
