using System;
using UnityEngine;

namespace RedDust.Services.Time
{
    /// <summary>
    /// Signals a desired world speed multiplier for gameplay.
    /// </summary>
    [Serializable]
    public struct SIActionWorldSpeed
    {
        public SIActionWorldSpeed(float targetScale)
        {
            TargetScale = Mathf.Max(0.01f, targetScale);
        }

        public float TargetScale { get; }
    }
}
