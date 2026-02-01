using System;

/// <summary>
/// Payload emitted whenever the player presses the Escape key (typically used
/// to trigger pause/menu flows).
/// </summary>
[Serializable]
public struct SUIEscapeIAction
{
    public SUIEscapeIAction(bool isPressed)
    {
        IsPressed = isPressed;
    }

    public bool IsPressed { get; }

    public static SUIEscapeIAction Pressed => new SUIEscapeIAction(true);
}
