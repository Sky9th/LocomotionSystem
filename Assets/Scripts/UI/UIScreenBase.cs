using UnityEngine;

/// <summary>
/// Base class for full-screen, mutually exclusive UI screens.
/// Typical examples: main menu, pause menu, settings, inventory, etc.
///
/// UIManager controls the lifetime of these instances and will call
/// SetVisible/OnEnter/OnExit according to navigation.
/// </summary>
public abstract class UIScreenBase : UIElementBase
{

    /// <summary>
    /// Called by UIManager when this screen becomes the active screen.
    /// </summary>
    public virtual void OnEnter(object payload)
    {
    }

    /// <summary>
    /// Called by UIManager when this screen is no longer the active screen.
    /// </summary>
    public virtual void OnExit()
    {
    }

    // Visibility behavior is inherited from UIElementBase.SetVisible.
}
