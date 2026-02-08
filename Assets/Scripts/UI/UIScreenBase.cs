using UnityEngine;

/// <summary>
/// Base class for fullscreen UI screens such as main menu, pause menu, or inventory.
/// Only one screen is active at a time, managed by UIService.
/// </summary>
public abstract class UIScreenBase : MonoBehaviour
{
    /// <summary>
    /// True when this screen is currently visible.
    /// Implementations may override visibility behavior (e.g. animations),
    /// but should keep this property in sync.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Called by UIService when this screen becomes the active screen.
    /// Use payload to receive navigation parameters if needed.
    /// </summary>
    public virtual void OnEnter(object payload)
    {
        SetVisible(true);
    }

    /// <summary>
    /// Called by UIService when this screen is no longer the active screen.
    /// </summary>
    public virtual void OnExit()
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