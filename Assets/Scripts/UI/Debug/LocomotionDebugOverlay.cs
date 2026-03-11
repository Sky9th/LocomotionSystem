using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Debug overlay for visualizing the current SLocomotion snapshot.
///
/// This overlay is treated as a read-only view over GameContext: it polls
/// the latest locomotion snapshot each frame and writes a formatted summary
/// into a TextMeshPro label. It does not modify gameplay state.
///
/// Typical usage:
/// - Registered as an overlay entry in UIManager (e.g. id "LocomotionDebug").
/// - Toggled via UIManager.ToggleDebugOverlay from an input action.
/// </summary>
public class LocomotionDebugOverlay : UIOverlayBase
{
    [Header("Locomotion Debug UI")]
    [SerializeField] private TextMeshProUGUI text;

    private readonly StringBuilder builder = new();

    public override void OnShow(object payload)
    {
        if (text != null)
        {
            text.text = string.Empty;
        }
        base.OnShow(payload);
        Refresh();
    }

    public override void OnHide()
    {
        base.OnHide();
        if (text != null)
        {
            text.text = string.Empty;
        }
    }

    protected override void Refresh()
    {
        if (text == null)
        {
            return;
        }

        if (GameContext == null || !GameContext.TryGetSnapshot(out SLocomotion snapshot))
        {
            text.text = "Locomotion: <no snapshot>";
            return;
        }

        static string FormatBool(bool value) => value ? "True" : "False";

        static string FormatDistance(float value)
        {
            if (float.IsPositiveInfinity(value))
            {
                return "<inf>";
            }

            return value.ToString("F3");
        }

        static string FormatLayerSnapshot(SLocomotionAnimationLayerSnapshot layer)
        {
            string layerName = string.IsNullOrEmpty(layer.LayerName) ? "<none>" : layer.LayerName;
            string aliasName = layer.Alias != null ? layer.Alias.name : "<none>";
            return $"{layerName}, alias={aliasName}, t={layer.NormalizedTime:F2}";
        }

        void AppendHeader(string title)
        {
            builder.AppendLine(title);
            builder.AppendLine(new string('-', title.Length));
        }

        void AppendKeyValue(string key, string value)
        {
            builder.Append(key).Append(": ").AppendLine(value);
        }

        builder.Clear();
        AppendHeader("Locomotion Snapshot");

        builder.AppendLine();
        builder.AppendLine("[Discrete]");
        AppendKeyValue("Phase", snapshot.DiscreteState.Phase.ToString());
        AppendKeyValue("Posture", snapshot.DiscreteState.Posture.ToString());
        AppendKeyValue("Gait", snapshot.DiscreteState.Gait.ToString());
        AppendKeyValue("Condition", snapshot.DiscreteState.Condition.ToString());
        AppendKeyValue("IsTurning", FormatBool(snapshot.DiscreteState.IsTurning));

        builder.AppendLine();
        builder.AppendLine("[Motor]");
        AppendKeyValue("Position", snapshot.Motor.Position.ToString("F2"));
        AppendKeyValue("DesiredLocalVelocity", snapshot.Motor.DesiredLocalVelocity.ToString("F2"));
        AppendKeyValue("DesiredPlanarVelocity", snapshot.Motor.DesiredPlanarVelocity.ToString("F2"));
        AppendKeyValue("ActualLocalVelocity", snapshot.Motor.ActualLocalVelocity.ToString("F2"));
        AppendKeyValue("ActualPlanarVelocity", snapshot.Motor.ActualPlanarVelocity.ToString("F2"));
        AppendKeyValue("ActualSpeed", snapshot.Motor.ActualSpeed.ToString("F2"));
        AppendKeyValue("LocomotionHeading", snapshot.Motor.LocomotionHeading.ToString("F2"));
        AppendKeyValue("BodyForward", snapshot.Motor.BodyForward.ToString("F2"));
        AppendKeyValue("LookDirection", snapshot.Motor.LookDirection.ToString("F2"));
        AppendKeyValue("TurnAngle", snapshot.Motor.TurnAngle.ToString("F1"));
        AppendKeyValue("IsLeftFootOnFront", FormatBool(snapshot.Motor.IsLeftFootOnFront));

        AppendKeyValue("Ground.IsGrounded", FormatBool(snapshot.Motor.GroundContact.IsGrounded));
        AppendKeyValue("Ground.DistanceToGround", FormatDistance(snapshot.Motor.GroundContact.DistanceToGround));
        AppendKeyValue("Ground.IsWalkableSlope", FormatBool(snapshot.Motor.GroundContact.IsWalkableSlope));
        AppendKeyValue("Ground.ContactPoint", snapshot.Motor.GroundContact.ContactPoint.ToString("F2"));
        AppendKeyValue("Ground.ContactNormal", snapshot.Motor.GroundContact.ContactNormal.ToString("F2"));

        AppendKeyValue("Obstacle.HasHit", FormatBool(snapshot.Motor.ForwardObstacleDetection.HasHit));
        AppendKeyValue("Obstacle.HasTopSurface", FormatBool(snapshot.Motor.ForwardObstacleDetection.HasTopSurface));
        AppendKeyValue("Obstacle.IsSlope", FormatBool(snapshot.Motor.ForwardObstacleDetection.IsSlope));
        AppendKeyValue("Obstacle.IsObstacle", FormatBool(snapshot.Motor.ForwardObstacleDetection.IsObstacle));
        AppendKeyValue("Obstacle.CanClimb", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanClimb));
        AppendKeyValue("Obstacle.CanVault", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanVault));
        AppendKeyValue("Obstacle.CanStepOver", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanStepOver));
        AppendKeyValue("Obstacle.Distance", FormatDistance(snapshot.Motor.ForwardObstacleDetection.Distance));
        AppendKeyValue("Obstacle.Height", FormatDistance(snapshot.Motor.ForwardObstacleDetection.ObstacleHeight));
        AppendKeyValue("Obstacle.SurfaceAngle", snapshot.Motor.ForwardObstacleDetection.SurfaceAngle.ToString("F1"));
        AppendKeyValue("Obstacle.Point", snapshot.Motor.ForwardObstacleDetection.Point.ToString("F2"));
        AppendKeyValue("Obstacle.Normal", snapshot.Motor.ForwardObstacleDetection.Normal.ToString("F2"));
        AppendKeyValue("Obstacle.TopPoint", snapshot.Motor.ForwardObstacleDetection.TopPoint.ToString("F2"));
        AppendKeyValue("Obstacle.TopNormal", snapshot.Motor.ForwardObstacleDetection.TopNormal.ToString("F2"));
        AppendKeyValue("Obstacle.HitLayer", snapshot.Motor.ForwardObstacleDetection.HitLayer.ToString());

        builder.AppendLine();
        builder.AppendLine("[Animation]");
        AppendKeyValue("HasAny", FormatBool(snapshot.Animation.HasAny));
        AppendKeyValue("BaseLayer", FormatLayerSnapshot(snapshot.Animation.BaseLayer));
        AppendKeyValue("HeadLookLayer", FormatLayerSnapshot(snapshot.Animation.HeadLookLayer));
        AppendKeyValue("FootstepLayer", FormatLayerSnapshot(snapshot.Animation.FootstepLayer));

        text.text = builder.ToString();
    }
}
