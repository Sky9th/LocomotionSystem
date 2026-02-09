using UnityEngine;

/// <summary>
/// Base class for overlay-style UI elements which can stack on top of each other.
/// Typical examples: HUD, notifications, debug panels, tooltips, etc.
///
/// UIManager controls the lifetime of these instances and will call
/// SetVisible/OnShow/OnHide according to overlay state.
/// </summary>
public abstract class UIOverlayBase : UIElementBase
{
    /// <summary>
    /// Overlays opt into the shared refresh loop by default so they only
    /// need to override Refresh() in most cases.
    /// </summary>
    protected override bool ShouldAutoRefresh => true;

    /// <summary>
    /// Called by UIManager when this overlay is shown.
    /// </summary>
    public virtual void OnShow(object payload)
    {
        ResetRefreshTimer();
    }

    /// <summary>
    /// Called by UIManager when this overlay is hidden.
    /// </summary>
    public virtual void OnHide()
    {
        ResetRefreshTimer();
    }

    // Visibility behavior is inherited from UIElementBase.SetVisible.
}
