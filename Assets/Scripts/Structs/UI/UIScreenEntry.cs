using System;

/// <summary>
/// Serializable mapping between a string id and a UIScreenBase instance.
/// Used by UIManager to configure available screens in the Inspector.
/// </summary>
[Serializable]
public struct UIScreenEntry
{
    public string id;
    public UIScreenBase screen;
}