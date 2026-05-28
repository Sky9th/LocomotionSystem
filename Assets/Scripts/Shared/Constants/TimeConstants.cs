using UnityEngine;

/// <summary>
/// Centralized access point for commonly used time values.
/// Wraps UnityEngine.Time so gameplay systems can use a single
/// place to retrieve delta times and avoid scattering direct
/// Time.* calls throughout the codebase.
/// </summary>
public static class TimeConstants
{
    /// <summary>
    /// Scaled delta time for frame-based simulation.
    /// Equivalent to Time.deltaTime.
    /// </summary>
    public static float Delta => UnityEngine.Time.deltaTime;

    /// <summary>
    /// Unscaled delta time, unaffected by Time.timeScale.
    /// </summary>
    public static float UnscaledDelta => UnityEngine.Time.unscaledDeltaTime;

    /// <summary>
    /// Fixed-step delta time used by the physics loop.
    /// </summary>
    public static float FixedDelta => UnityEngine.Time.fixedDeltaTime;

    /// <summary>
    /// Scaled game time since startup.
    /// </summary>
    public static float Time => UnityEngine.Time.time;

    /// <summary>
    /// Unscaled game time since startup.
    /// </summary>
    public static float UnscaledTime => UnityEngine.Time.unscaledTime;

    /// <summary>
    /// Current global time scale.
    /// </summary>
    public static float TimeScale => UnityEngine.Time.timeScale;
}