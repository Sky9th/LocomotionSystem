using System;
using UnityEngine;

/// <summary>
/// Signals a desired global time scale multiplier.
/// </summary>
[Serializable]
public struct STimeScaleIAction
{
    public STimeScaleIAction(float targetScale)
    {
        TargetScale = Mathf.Max(0.01f, targetScale);
    }

    public float TargetScale { get; }
}
