using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight event bus used to decouple gameplay systems.
/// </summary>
[DisallowMultipleComponent]
public class EventDispatcher : BaseService
{
    private readonly Dictionary<Type, List<Delegate>> listeners = new();
    [SerializeField] private List<string> inspectorListeners = new();

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        return true;
    }

    public void Subscribe<TPayload>(Action<TPayload, MetaStruct> handler)
    {
        if (handler == null)
        {
            return;
        }

        var key = typeof(TPayload);
        if (!listeners.TryGetValue(key, out var handlers))
        {
            handlers = new List<Delegate>();
            listeners.Add(key, handlers);
        }

        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
            RefreshInspectorListeners();
        }
    }

    public void Unsubscribe<TPayload>(Action<TPayload, MetaStruct> handler)
    {
        if (handler == null)
        {
            return;
        }

        var key = typeof(TPayload);
        if (!listeners.TryGetValue(key, out var handlers))
        {
            return;
        }

        handlers.Remove(handler);
        if (handlers.Count == 0)
        {
            listeners.Remove(key);
        }

        RefreshInspectorListeners();
    }

    public void Publish<TPayload>(TPayload payload)
    {
        MetaStruct meta = new MetaStruct
        {
            Timestamp = Time.time,
            FrameIndex = (uint)Time.frameCount
        };

        var key = typeof(TPayload);
        if (!listeners.TryGetValue(key, out var handlers))
        {
            return;
        }

        var snapshot = handlers.ToArray();
        foreach (var handler in snapshot)
        {
            if (handler is Action<TPayload, MetaStruct> typed)
            {
                typed.Invoke(payload, meta);
            }
        }
    }

    public void Clear()
    {
        listeners.Clear();
        inspectorListeners.Clear();
    }

    private void RefreshInspectorListeners()
    {
        inspectorListeners.Clear();
        foreach (var entry in listeners)
        {
            inspectorListeners.Add($"{entry.Key.Name}: {entry.Value.Count} handlers");
        }
    }
}
