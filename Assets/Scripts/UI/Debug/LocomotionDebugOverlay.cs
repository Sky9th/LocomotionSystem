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

    public override void OnHide() { base.OnHide(); }

    protected override void Refresh()
    {
        if (text == null && summaryText == null && leftText == null && rightText == null) return;

        var context = GameContext;
        if (context == null || !context.TryGetSnapshot(out SCharacterSnapshot snapshot))
        { RenderMissingSnapshot(); return; }

        static string f(bool v) => v ? "True" : "False";
        static string d(float v) => float.IsPositiveInfinity(v) ? "<inf>" : v.ToString("F3");
        void Header(StringBuilder sb, string t) { sb.AppendLine(t); sb.AppendLine(new string('-', t.Length)); }
        void KV(StringBuilder sb, string k, string v) => sb.AppendLine($"{k,-24}: {v}");

        var kin = snapshot.Kinematic;

        summaryBuilder.Clear()
            .Append("GROUND ").Append(f(kin.GroundContact.IsGrounded))
            .Append(" | OBSTACLE ").Append(f(kin.ForwardObstacleDetection.HasHit));

        leftBuilder.Clear();
        Header(leftBuilder, "Kinematic");
        KV(leftBuilder, "Position", kin.Position.ToString("F2"));
        KV(leftBuilder, "BodyForward", kin.BodyForward.ToString("F2"));
        KV(leftBuilder, "Heading", kin.LocomotionHeading.ToString("F2"));
        KV(leftBuilder, "LookDirection", kin.LookDirection.ToString("F2"));

        rightBuilder.Clear();
        Header(rightBuilder, "Ground");
        KV(rightBuilder, "IsGrounded", f(kin.GroundContact.IsGrounded));
        KV(rightBuilder, "Distance", d(kin.GroundContact.DistanceToGround));
        KV(rightBuilder, "WalkableSlope", f(kin.GroundContact.IsWalkableSlope));
        KV(rightBuilder, "ContactPoint", kin.GroundContact.ContactPoint.ToString("F2"));
        rightBuilder.AppendLine();
        Header(rightBuilder, "Obstacle");
        KV(rightBuilder, "HasHit", f(kin.ForwardObstacleDetection.HasHit));
        KV(rightBuilder, "CanClimb", f(kin.ForwardObstacleDetection.CanClimb));
        KV(rightBuilder, "Distance", d(kin.ForwardObstacleDetection.Distance));
        KV(rightBuilder, "Height", d(kin.ForwardObstacleDetection.ObstacleHeight));

        if (summaryText != null) summaryText.text = summaryBuilder.ToString();
        if (leftText != null) leftText.text = leftBuilder.ToString();
        if (rightText != null) rightText.text = rightBuilder.ToString();
        if (text != null) { builder.Clear(); builder.Append(summaryBuilder).AppendLine().Append(leftBuilder).AppendLine().Append(rightBuilder); text.text = builder.ToString(); }
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
