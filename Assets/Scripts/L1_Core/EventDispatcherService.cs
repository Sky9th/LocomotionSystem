using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// Lightweight event bus used to decouple gameplay systems.
    /// </summary>
    [System.Obsolete("替换为 EventHub — EventDispatcher 即将废弃")]
    [DisallowMultipleComponent]
    public class EventDispatcherService : ModuleComponent
    {
        private readonly Dictionary<Type, List<Delegate>> listeners = new();
        [SerializeField] private List<string> inspectorListeners = new();

        public override void OnWire()
        {
            GameContext.Instance.RegisterService(this);

            GameService.Instance?.NotifyServiceWired();
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
}
