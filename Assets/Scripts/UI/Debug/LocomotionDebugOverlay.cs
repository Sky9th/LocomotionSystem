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
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI leftText;
    [SerializeField] private TextMeshProUGUI rightText;

    private readonly StringBuilder builder = new();
    private readonly StringBuilder summaryBuilder = new();
    private readonly StringBuilder leftBuilder = new();
    private readonly StringBuilder rightBuilder = new();

    public override void OnShow(object payload)
    {
        ClearAllLabels();

        base.OnShow(payload);
        Refresh();
    }

    public override void OnHide()
    {
        base.OnHide();
        ClearAllLabels();
    }

    protected override void Refresh()
    {
        if (text == null && summaryText == null && leftText == null && rightText == null)
        {
            return;
        }

        if (GameContext == null || !GameContext.TryGetSnapshot(out SLocomotion snapshot))
        {
            RenderMissingSnapshot();
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

        static void AppendHeader(StringBuilder target, string title)
        {
            target.AppendLine(title);
            target.AppendLine(new string('-', title.Length));
        }

        static void AppendKeyValue(StringBuilder target, string key, string value)
        {
            target.Append(key).Append(": ").AppendLine(value);
        }

        if (summaryText != null || leftText != null || rightText != null)
        {
            RenderDockedLayout(snapshot, AppendHeader, AppendKeyValue, FormatBool, FormatDistance, FormatLayerSnapshot);
            return;
        }

        builder.Clear();
        AppendHeader(builder, "Locomotion Snapshot");

        builder.AppendLine();
        builder.AppendLine("[Discrete]");
        AppendKeyValue(builder, "Phase", snapshot.DiscreteState.Phase.ToString());
        AppendKeyValue(builder, "Posture", snapshot.DiscreteState.Posture.ToString());
        AppendKeyValue(builder, "Gait", snapshot.DiscreteState.Gait.ToString());
        AppendKeyValue(builder, "Condition", snapshot.DiscreteState.Condition.ToString());
        AppendKeyValue(builder, "IsTurning", FormatBool(snapshot.DiscreteState.IsTurning));

        builder.AppendLine();
        builder.AppendLine("[Traversal]");
        AppendKeyValue(builder, "Type", snapshot.Traversal.Type.ToString());
        AppendKeyValue(builder, "Stage", snapshot.Traversal.Stage.ToString());
        AppendKeyValue(builder, "HasRequest", FormatBool(snapshot.Traversal.HasRequest));
        AppendKeyValue(builder, "IsActive", FormatBool(snapshot.Traversal.IsActive));
        AppendKeyValue(builder, "ObstacleHeight", FormatDistance(snapshot.Traversal.ObstacleHeight));
        AppendKeyValue(builder, "ObstaclePoint", snapshot.Traversal.ObstaclePoint.ToString("F2"));
        AppendKeyValue(builder, "TargetPoint", snapshot.Traversal.TargetPoint.ToString("F2"));
        AppendKeyValue(builder, "FacingDirection", snapshot.Traversal.FacingDirection.ToString("F2"));

        builder.AppendLine();
        builder.AppendLine("[Motor]");
        AppendKeyValue(builder, "Position", snapshot.Motor.Position.ToString("F2"));
        AppendKeyValue(builder, "DesiredLocalVelocity", snapshot.Motor.DesiredLocalVelocity.ToString("F2"));
        AppendKeyValue(builder, "DesiredPlanarVelocity", snapshot.Motor.DesiredPlanarVelocity.ToString("F2"));
        AppendKeyValue(builder, "ActualLocalVelocity", snapshot.Motor.ActualLocalVelocity.ToString("F2"));
        AppendKeyValue(builder, "ActualPlanarVelocity", snapshot.Motor.ActualPlanarVelocity.ToString("F2"));
        AppendKeyValue(builder, "ActualSpeed", snapshot.Motor.ActualSpeed.ToString("F2"));
        AppendKeyValue(builder, "LocomotionHeading", snapshot.Motor.LocomotionHeading.ToString("F2"));
        AppendKeyValue(builder, "BodyForward", snapshot.Motor.BodyForward.ToString("F2"));
        AppendKeyValue(builder, "LookDirection", snapshot.Motor.LookDirection.ToString("F2"));
        AppendKeyValue(builder, "TurnAngle", snapshot.Motor.TurnAngle.ToString("F1"));
        AppendKeyValue(builder, "IsLeftFootOnFront", FormatBool(snapshot.Motor.IsLeftFootOnFront));

        AppendKeyValue(builder, "Ground.IsGrounded", FormatBool(snapshot.Motor.GroundContact.IsGrounded));
        AppendKeyValue(builder, "Ground.DistanceToGround", FormatDistance(snapshot.Motor.GroundContact.DistanceToGround));
        AppendKeyValue(builder, "Ground.IsWalkableSlope", FormatBool(snapshot.Motor.GroundContact.IsWalkableSlope));
        AppendKeyValue(builder, "Ground.ContactPoint", snapshot.Motor.GroundContact.ContactPoint.ToString("F2"));
        AppendKeyValue(builder, "Ground.ContactNormal", snapshot.Motor.GroundContact.ContactNormal.ToString("F2"));

        AppendKeyValue(builder, "Obstacle.HasHit", FormatBool(snapshot.Motor.ForwardObstacleDetection.HasHit));
        AppendKeyValue(builder, "Obstacle.HasTopSurface", FormatBool(snapshot.Motor.ForwardObstacleDetection.HasTopSurface));
        AppendKeyValue(builder, "Obstacle.IsSlope", FormatBool(snapshot.Motor.ForwardObstacleDetection.IsSlope));
        AppendKeyValue(builder, "Obstacle.IsObstacle", FormatBool(snapshot.Motor.ForwardObstacleDetection.IsObstacle));
        AppendKeyValue(builder, "Obstacle.CanClimb", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanClimb));
        AppendKeyValue(builder, "Obstacle.CanVault", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanVault));
        AppendKeyValue(builder, "Obstacle.CanStepOver", FormatBool(snapshot.Motor.ForwardObstacleDetection.CanStepOver));
        AppendKeyValue(builder, "Obstacle.Distance", FormatDistance(snapshot.Motor.ForwardObstacleDetection.Distance));
        AppendKeyValue(builder, "Obstacle.Height", FormatDistance(snapshot.Motor.ForwardObstacleDetection.ObstacleHeight));
        AppendKeyValue(builder, "Obstacle.SurfaceAngle", snapshot.Motor.ForwardObstacleDetection.SurfaceAngle.ToString("F1"));
        AppendKeyValue(builder, "Obstacle.Point", snapshot.Motor.ForwardObstacleDetection.Point.ToString("F2"));
        AppendKeyValue(builder, "Obstacle.Normal", snapshot.Motor.ForwardObstacleDetection.Normal.ToString("F2"));
        AppendKeyValue(builder, "Obstacle.TopPoint", snapshot.Motor.ForwardObstacleDetection.TopPoint.ToString("F2"));
        AppendKeyValue(builder, "Obstacle.TopNormal", snapshot.Motor.ForwardObstacleDetection.TopNormal.ToString("F2"));
        AppendKeyValue(builder, "Obstacle.HitLayer", snapshot.Motor.ForwardObstacleDetection.HitLayer.ToString());

        builder.AppendLine();
        builder.AppendLine("[Animation]");
        AppendKeyValue(builder, "HasAny", FormatBool(snapshot.Animation.HasAny));
        AppendKeyValue(builder, "BaseLayer", FormatLayerSnapshot(snapshot.Animation.BaseLayer));
        AppendKeyValue(builder, "HeadLookLayer", FormatLayerSnapshot(snapshot.Animation.HeadLookLayer));
        AppendKeyValue(builder, "FootstepLayer", FormatLayerSnapshot(snapshot.Animation.FootstepLayer));

        text.text = builder.ToString();
    }

    private void RenderMissingSnapshot()
    {
        const string message = "Locomotion: <no snapshot>";

        if (text != null)
        {
            text.text = message;
        }

        if (summaryText != null)
        {
            summaryText.text = message;
        }

        if (leftText != null)
        {
            leftText.text = string.Empty;
        }

        if (rightText != null)
        {
            rightText.text = string.Empty;
        }
    }

    private void ClearAllLabels()
    {
        if (text != null)
        {
            text.text = string.Empty;
        }

        if (summaryText != null)
        {
            summaryText.text = string.Empty;
        }

        if (leftText != null)
        {
            leftText.text = string.Empty;
        }

        if (rightText != null)
        {
            rightText.text = string.Empty;
        }
    }

    private void RenderDockedLayout(
        SLocomotion snapshot,
        System.Action<StringBuilder, string> appendHeader,
        System.Action<StringBuilder, string, string> appendKeyValue,
        System.Func<bool, string> formatBool,
        System.Func<float, string> formatDistance,
        System.Func<SLocomotionAnimationLayerSnapshot, string> formatLayerSnapshot)
    {
        summaryBuilder.Clear();
        leftBuilder.Clear();
        rightBuilder.Clear();

        summaryBuilder
            .Append("PHASE ").Append(snapshot.DiscreteState.Phase)
            .Append(" | POSTURE ").Append(snapshot.DiscreteState.Posture)
            .Append(" | GAIT ").Append(snapshot.DiscreteState.Gait)
            .Append(" | TRV ").Append(snapshot.Traversal.Type).Append('/').Append(snapshot.Traversal.Stage)
            .AppendLine();

        summaryBuilder
            .Append("SPD ").Append(snapshot.Motor.ActualSpeed.ToString("F2"))
            .Append(" | GND ").Append(formatBool(snapshot.Motor.GroundContact.IsGrounded))
            .Append(" | HIT ").Append(formatBool(snapshot.Motor.ForwardObstacleDetection.HasHit))
            .Append(" | CLIMB ").Append(formatBool(snapshot.Motor.ForwardObstacleDetection.CanClimb))
            .Append(" | TURN ").Append(formatBool(snapshot.DiscreteState.IsTurning));

        appendHeader(leftBuilder, "State");
        appendKeyValue(leftBuilder, "Phase", snapshot.DiscreteState.Phase.ToString());
        appendKeyValue(leftBuilder, "Posture", snapshot.DiscreteState.Posture.ToString());
        appendKeyValue(leftBuilder, "Gait", snapshot.DiscreteState.Gait.ToString());
        appendKeyValue(leftBuilder, "Condition", snapshot.DiscreteState.Condition.ToString());
        appendKeyValue(leftBuilder, "IsTurning", formatBool(snapshot.DiscreteState.IsTurning));

        leftBuilder.AppendLine();
        appendHeader(leftBuilder, "Traversal");
        appendKeyValue(leftBuilder, "Type", snapshot.Traversal.Type.ToString());
        appendKeyValue(leftBuilder, "Stage", snapshot.Traversal.Stage.ToString());
        appendKeyValue(leftBuilder, "HasRequest", formatBool(snapshot.Traversal.HasRequest));
        appendKeyValue(leftBuilder, "IsActive", formatBool(snapshot.Traversal.IsActive));
        appendKeyValue(leftBuilder, "ObstacleHeight", formatDistance(snapshot.Traversal.ObstacleHeight));
        appendKeyValue(leftBuilder, "ObstaclePoint", snapshot.Traversal.ObstaclePoint.ToString("F2"));
        appendKeyValue(leftBuilder, "TargetPoint", snapshot.Traversal.TargetPoint.ToString("F2"));
        appendKeyValue(leftBuilder, "FacingDirection", snapshot.Traversal.FacingDirection.ToString("F2"));

        leftBuilder.AppendLine();
        appendHeader(leftBuilder, "Motion");
        appendKeyValue(leftBuilder, "Position", snapshot.Motor.Position.ToString("F2"));
        appendKeyValue(leftBuilder, "DesiredLocalVelocity", snapshot.Motor.DesiredLocalVelocity.ToString("F2"));
        appendKeyValue(leftBuilder, "DesiredPlanarVelocity", snapshot.Motor.DesiredPlanarVelocity.ToString("F2"));
        appendKeyValue(leftBuilder, "ActualLocalVelocity", snapshot.Motor.ActualLocalVelocity.ToString("F2"));
        appendKeyValue(leftBuilder, "ActualPlanarVelocity", snapshot.Motor.ActualPlanarVelocity.ToString("F2"));
        appendKeyValue(leftBuilder, "ActualSpeed", snapshot.Motor.ActualSpeed.ToString("F2"));
        appendKeyValue(leftBuilder, "LocomotionHeading", snapshot.Motor.LocomotionHeading.ToString("F2"));
        appendKeyValue(leftBuilder, "BodyForward", snapshot.Motor.BodyForward.ToString("F2"));
        appendKeyValue(leftBuilder, "LookDirection", snapshot.Motor.LookDirection.ToString("F2"));
        appendKeyValue(leftBuilder, "TurnAngle", snapshot.Motor.TurnAngle.ToString("F1"));
        appendKeyValue(leftBuilder, "IsLeftFootOnFront", formatBool(snapshot.Motor.IsLeftFootOnFront));

        appendHeader(rightBuilder, "Ground");
        appendKeyValue(rightBuilder, "IsGrounded", formatBool(snapshot.Motor.GroundContact.IsGrounded));
        appendKeyValue(rightBuilder, "DistanceToGround", formatDistance(snapshot.Motor.GroundContact.DistanceToGround));
        appendKeyValue(rightBuilder, "IsWalkableSlope", formatBool(snapshot.Motor.GroundContact.IsWalkableSlope));
        appendKeyValue(rightBuilder, "ContactPoint", snapshot.Motor.GroundContact.ContactPoint.ToString("F2"));
        appendKeyValue(rightBuilder, "ContactNormal", snapshot.Motor.GroundContact.ContactNormal.ToString("F2"));

        rightBuilder.AppendLine();
        appendHeader(rightBuilder, "Obstacle");
        appendKeyValue(rightBuilder, "HasHit", formatBool(snapshot.Motor.ForwardObstacleDetection.HasHit));
        appendKeyValue(rightBuilder, "HasTopSurface", formatBool(snapshot.Motor.ForwardObstacleDetection.HasTopSurface));
        appendKeyValue(rightBuilder, "IsSlope", formatBool(snapshot.Motor.ForwardObstacleDetection.IsSlope));
        appendKeyValue(rightBuilder, "IsObstacle", formatBool(snapshot.Motor.ForwardObstacleDetection.IsObstacle));
        appendKeyValue(rightBuilder, "CanClimb", formatBool(snapshot.Motor.ForwardObstacleDetection.CanClimb));
        appendKeyValue(rightBuilder, "CanVault", formatBool(snapshot.Motor.ForwardObstacleDetection.CanVault));
        appendKeyValue(rightBuilder, "CanStepOver", formatBool(snapshot.Motor.ForwardObstacleDetection.CanStepOver));
        appendKeyValue(rightBuilder, "Distance", formatDistance(snapshot.Motor.ForwardObstacleDetection.Distance));
        appendKeyValue(rightBuilder, "Height", formatDistance(snapshot.Motor.ForwardObstacleDetection.ObstacleHeight));
        appendKeyValue(rightBuilder, "SurfaceAngle", snapshot.Motor.ForwardObstacleDetection.SurfaceAngle.ToString("F1"));
        appendKeyValue(rightBuilder, "Point", snapshot.Motor.ForwardObstacleDetection.Point.ToString("F2"));
        appendKeyValue(rightBuilder, "Normal", snapshot.Motor.ForwardObstacleDetection.Normal.ToString("F2"));
        appendKeyValue(rightBuilder, "TopPoint", snapshot.Motor.ForwardObstacleDetection.TopPoint.ToString("F2"));
        appendKeyValue(rightBuilder, "TopNormal", snapshot.Motor.ForwardObstacleDetection.TopNormal.ToString("F2"));
        appendKeyValue(rightBuilder, "HitLayer", snapshot.Motor.ForwardObstacleDetection.HitLayer.ToString());

        rightBuilder.AppendLine();
        appendHeader(rightBuilder, "Animation");
        appendKeyValue(rightBuilder, "HasAny", formatBool(snapshot.Animation.HasAny));
        appendKeyValue(rightBuilder, "BaseLayer", formatLayerSnapshot(snapshot.Animation.BaseLayer));
        appendKeyValue(rightBuilder, "HeadLookLayer", formatLayerSnapshot(snapshot.Animation.HeadLookLayer));
        appendKeyValue(rightBuilder, "FootstepLayer", formatLayerSnapshot(snapshot.Animation.FootstepLayer));

        if (summaryText != null)
        {
            summaryText.text = summaryBuilder.ToString();
        }

        if (leftText != null)
        {
            leftText.text = leftBuilder.ToString();
        }

        if (rightText != null)
        {
            rightText.text = rightBuilder.ToString();
        }

        if (text != null)
        {
            builder.Clear();
            builder.Append(summaryBuilder);
            builder.AppendLine();
            builder.Append(leftBuilder);
            builder.AppendLine();
            builder.Append(rightBuilder);
            text.text = builder.ToString();
        }
    }
}
