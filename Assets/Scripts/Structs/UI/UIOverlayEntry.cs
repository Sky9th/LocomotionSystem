using System;

/// <summary>
/// Serializable mapping between a string id and a UIOverlayBase instance.
/// Used by UIManager to configure available overlays in the Inspector.
/// </summary>
[Serializable]
public struct UIOverlayEntry
{
    public string id;
    public UIOverlayBase overlay;
}