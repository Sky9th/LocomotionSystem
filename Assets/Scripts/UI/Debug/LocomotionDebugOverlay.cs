using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Debug overlay for visualizing the current SPlayerLocomotion snapshot.
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

        builder.Clear();
        builder.AppendLine("Locomotion Snapshot");
        builder.AppendLine("-------------------");
         builder.Append("State: ").AppendLine(snapshot.DiscreteState.Phase.ToString());
         builder.Append("Posture: ").AppendLine(snapshot.DiscreteState.Posture.ToString());
         builder.Append("Gait: ").AppendLine(snapshot.DiscreteState.Gait.ToString());
         builder.Append("Condition: ").AppendLine(snapshot.DiscreteState.Condition.ToString());
         builder.Append("Position: ").Append(snapshot.Agent.Position.ToString("F2")).AppendLine();
         builder.Append("DesiredLocalVelocity: ").Append(snapshot.Agent.DesiredLocalVelocity.ToString("F2")).AppendLine();
         builder.Append("DesiredPlanarVelocity: ").Append(snapshot.Agent.DesiredPlanarVelocity.ToString("F2")).AppendLine();
         builder.Append("ActualPlanarVelocity: ").Append(snapshot.Agent.ActualPlanarVelocity.ToString("F2")).Append(" (speed=")
             .Append(snapshot.Agent.ActualSpeed.ToString("F2")).AppendLine(")");
         builder.Append("LocomotionHeading: ").Append(snapshot.Agent.LocomotionHeading.ToString("F2")).AppendLine();
         builder.Append("BodyForward: ").Append(snapshot.Agent.BodyForward.ToString("F2")).AppendLine();
         builder.Append("LookDir: ").Append(snapshot.Agent.LookDirection.ToString("F2")).AppendLine();
         builder.Append("IsGrounded: ").Append(snapshot.Agent.IsGrounded).AppendLine();
         builder.Append("Turning").Append(snapshot.DiscreteState.IsTurning).Append(", Angle=").Append(snapshot.Agent.TurnAngle.ToString("F1")).AppendLine();
        builder.Append("LeftFootFront: ").Append(snapshot.Agent.IsLeftFootOnFront).AppendLine();

        text.text = builder.ToString();
    }
}
