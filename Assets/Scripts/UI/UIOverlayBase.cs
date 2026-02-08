using UnityEngine;

/// <summary>
/// Base class for non-fullscreen UI overlays such as HUD, debug panels, and tooltips.
/// Overlays are managed by UIService and can be stacked on top of each other.
/// </summary>
public abstract class UIOverlayBase : MonoBehaviour
{
    /// <summary>
    /// True when this overlay is currently visible.
    /// Implementations may override visibility behavior (e.g. CanvasGroup),
    /// but should keep this property in sync.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Called by UIService when the overlay is shown.
    /// Payload can be used to pass contextual information (e.g. target entity).
    /// </summary>
    public virtual void OnShow(object payload)
    {
        SetVisible(true);
    }

    /// <summary>
    /// Called by UIService when the overlay is hidden.
    /// </summary>
    public virtual void OnHide()
    {
        SetVisible(false);
    }

    /// <summary>
    /// Unified entry point for toggling visibility.
    /// Default implementation uses GameObject active state.
    /// </summary>
    public virtual void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }
}