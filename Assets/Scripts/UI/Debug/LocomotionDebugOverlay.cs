using System.Text;
using UnityEngine;
using TMPro;

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
        base.OnShow(payload);
        Refresh();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    protected override void Refresh()
    {
        if (text == null && summaryText == null && leftText == null && rightText == null)
            return;

        var context = GameContext;
        if (context == null || !context.TryGetSnapshot(out SCharacterSnapshot snapshot))
        {
            RenderMissingSnapshot();
            return;
        }

        static string f(bool v) => v ? "True" : "False";
        static string d(float v) => float.IsPositiveInfinity(v) ? "<inf>" : v.ToString("F3");

        void Header(StringBuilder sb, string t) { sb.AppendLine(t); sb.AppendLine(new string('-', t.Length)); }
        void KV(StringBuilder sb, string k, string v) => sb.AppendLine($"{k,-24}: {v}");

        // summary
        summaryBuilder.Clear()
            .Append("PHASE ").Append(snapshot.DiscreteState.Phase)
            .Append(" | POSTURE ").Append(snapshot.DiscreteState.Posture)
            .Append(" | GAIT ").Append(snapshot.DiscreteState.Gait)
            .Append(" | TURN ").Append(f(snapshot.DiscreteState.IsTurning));

        // state
        leftBuilder.Clear();
        Header(leftBuilder, "State");
        KV(leftBuilder, "Phase", snapshot.DiscreteState.Phase.ToString());
        KV(leftBuilder, "Posture", snapshot.DiscreteState.Posture.ToString());
        KV(leftBuilder, "Gait", snapshot.DiscreteState.Gait.ToString());
        KV(leftBuilder, "Condition", snapshot.DiscreteState.Condition.ToString());
        KV(leftBuilder, "IsTurning", f(snapshot.DiscreteState.IsTurning));
        leftBuilder.AppendLine();
        Header(leftBuilder, "Traversal");
        KV(leftBuilder, "Type", snapshot.Traversal.Type.ToString());
        KV(leftBuilder, "Stage", snapshot.Traversal.Stage.ToString());
        KV(leftBuilder, "HasRequest", f(snapshot.Traversal.HasRequest));
        KV(leftBuilder, "ObstacleHeight", d(snapshot.Traversal.ObstacleHeight));
        leftBuilder.AppendLine();
        Header(leftBuilder, "Kinematic");
        KV(leftBuilder, "Position", snapshot.Kinematic.Position.ToString("F2"));
        KV(leftBuilder, "BodyForward", snapshot.Kinematic.BodyForward.ToString("F2"));
        KV(leftBuilder, "LookDirection", snapshot.Kinematic.LookDirection.ToString("F2"));
        leftBuilder.AppendLine();
        Header(leftBuilder, "Motor");
        KV(leftBuilder, "DesiredLocalVel", snapshot.Motor.DesiredLocalVelocity.ToString("F2"));
        KV(leftBuilder, "ActualLocalVel", snapshot.Motor.ActualLocalVelocity.ToString("F2"));
        KV(leftBuilder, "ActualSpeed", snapshot.Motor.ActualSpeed.ToString("F2"));
        KV(leftBuilder, "Heading", snapshot.Motor.LocomotionHeading.ToString("F2"));
        KV(leftBuilder, "TurnAngle", snapshot.Motor.TurnAngle.ToString("F1"));

        // ground + obstacle
        rightBuilder.Clear();
        Header(rightBuilder, "Ground");
        KV(rightBuilder, "IsGrounded", f(snapshot.Kinematic.GroundContact.IsGrounded));
        KV(rightBuilder, "Distance", d(snapshot.Kinematic.GroundContact.DistanceToGround));
        KV(rightBuilder, "WalkableSlope", f(snapshot.Kinematic.GroundContact.IsWalkableSlope));
        KV(rightBuilder, "ContactPoint", snapshot.Kinematic.GroundContact.ContactPoint.ToString("F2"));
        rightBuilder.AppendLine();
        Header(rightBuilder, "Obstacle");
        KV(rightBuilder, "HasHit", f(snapshot.Kinematic.ForwardObstacleDetection.HasHit));
        KV(rightBuilder, "CanClimb", f(snapshot.Kinematic.ForwardObstacleDetection.CanClimb));
        KV(rightBuilder, "Distance", d(snapshot.Kinematic.ForwardObstacleDetection.Distance));
        KV(rightBuilder, "Height", d(snapshot.Kinematic.ForwardObstacleDetection.ObstacleHeight));

        if (summaryText != null) summaryText.text = summaryBuilder.ToString();
        if (leftText != null) leftText.text = leftBuilder.ToString();
        if (rightText != null) rightText.text = rightBuilder.ToString();

        if (text != null)
        {
            builder.Clear();
            builder.Append(summaryBuilder).AppendLine();
            builder.Append(leftBuilder).AppendLine();
            builder.Append(rightBuilder);
            text.text = builder.ToString();
        }
    }

    private void RenderMissingSnapshot()
    {
        var msg = "[No Character Snapshot]";
        if (text != null) text.text = msg;
        if (summaryText != null) summaryText.text = msg;
        if (leftText != null) leftText.text = "";
        if (rightText != null) rightText.text = "";
    }
}
