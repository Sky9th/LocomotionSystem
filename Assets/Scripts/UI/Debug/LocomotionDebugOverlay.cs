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
        builder.AppendLine("[Agent]");
        AppendKeyValue("Position", snapshot.Agent.Position.ToString("F2"));
        AppendKeyValue("DesiredLocalVelocity", snapshot.Agent.DesiredLocalVelocity.ToString("F2"));
        AppendKeyValue("DesiredPlanarVelocity", snapshot.Agent.DesiredPlanarVelocity.ToString("F2"));
        AppendKeyValue("ActualLocalVelocity", snapshot.Agent.ActualLocalVelocity.ToString("F2"));
        AppendKeyValue("ActualPlanarVelocity", snapshot.Agent.ActualPlanarVelocity.ToString("F2"));
        AppendKeyValue("ActualSpeed", snapshot.Agent.ActualSpeed.ToString("F2"));
        AppendKeyValue("LocomotionHeading", snapshot.Agent.LocomotionHeading.ToString("F2"));
        AppendKeyValue("BodyForward", snapshot.Agent.BodyForward.ToString("F2"));
        AppendKeyValue("LookDirection", snapshot.Agent.LookDirection.ToString("F2"));
        AppendKeyValue("TurnAngle", snapshot.Agent.TurnAngle.ToString("F1"));
        AppendKeyValue("IsLeftFootOnFront", FormatBool(snapshot.Agent.IsLeftFootOnFront));

        AppendKeyValue("Ground.IsGrounded", FormatBool(snapshot.Agent.GroundContact.IsGrounded));
        AppendKeyValue("Ground.ContactPoint", snapshot.Agent.GroundContact.ContactPoint.ToString("F2"));
        AppendKeyValue("Ground.ContactNormal", snapshot.Agent.GroundContact.ContactNormal.ToString("F2"));

        builder.AppendLine();
        builder.AppendLine("[Animation]");
        AppendKeyValue("HasAny", FormatBool(snapshot.Animation.HasAny));
        AppendKeyValue("BaseLayer", FormatLayerSnapshot(snapshot.Animation.BaseLayer));
        AppendKeyValue("HeadLookLayer", FormatLayerSnapshot(snapshot.Animation.HeadLookLayer));
        AppendKeyValue("FootstepLayer", FormatLayerSnapshot(snapshot.Animation.FootstepLayer));

        text.text = builder.ToString();
    }
}
