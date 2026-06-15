using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// 事件通道资产持有组件。挂载在需要事件通信的 GameObject 上，
    /// 发布方和订阅方通过 Get&lt;T&gt;() 获取通道引用。
    /// OnEnable / OnDisable 自动驱动已注册的 IEventListener。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EventHub : MonoBehaviour
    {
        [SerializeField] private EventChannelBase[] channels = Array.Empty<EventChannelBase>();

        private readonly Dictionary<Type, EventChannelBase> lookup = new();
        private readonly List<IEventListener> listeners = new();

        private void Awake()
        {
            foreach (var ch in channels)
            {
                if (ch != null)
                    lookup[ch.GetType()] = ch;
            }
        }

        private void OnEnable()
        {
            foreach (var l in listeners)
                l.BindEvents();
        }

        private void OnDisable()
        {
            foreach (var l in listeners)
                l.UnbindEvents();
        }

        /// <summary>按类型获取事件通道。未注册时返回 null。</summary>
        public T Get<T>() where T : EventChannelBase
            => lookup.TryGetValue(typeof(T), out var ch) ? ch as T : null;

        /// <summary>注册事件监听者。若组件已启用则立即 BindEvents，覆盖 OnEnable 先于注册的时序。</summary>
        public void RegisterListener(IEventListener listener)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
                if (isActiveAndEnabled)
                    listener.BindEvents();
            }
        }
    }
}
