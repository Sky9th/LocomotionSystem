using UnityEngine;

/// <summary>
/// Applies global time-scale requests produced by input handlers or other systems.
/// Keeps the actual Time.timeScale mutation centralized.
/// </summary>
public class TimeScaleManager : BaseService
{
    [SerializeField, Min(0.01f)] private float minScale = 0.01f;
    [SerializeField, Min(0.01f)] private float maxScale = 1f;

    private float defaultScale = 1f;

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        defaultScale = Mathf.Max(Time.timeScale, minScale);
        return true;
    }

    protected override void SubscribeToDispatcher()
    {
        Dispatcher.Subscribe<STimeScaleIAction>(HandleTimeScaleRequested);
    }

    private void HandleTimeScaleRequested(STimeScaleIAction action, MetaStruct meta)
    {
        float clamped = Mathf.Clamp(action.TargetScale, minScale, maxScale);
        Time.timeScale = clamped;
        Debug.Log($"[TimeScaleService] Applied time scale: {clamped}", this);
    }

    private void OnDisable()
    {
        RestoreDefaultScale();
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
        {
            Dispatcher.Unsubscribe<STimeScaleIAction>(HandleTimeScaleRequested);
        }

        RestoreDefaultScale();
    }

    private void RestoreDefaultScale()
    {
        if (Application.isPlaying)
        {
            Time.timeScale = defaultScale;
        }
    }
}
