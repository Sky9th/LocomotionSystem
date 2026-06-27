using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// 事件通道汇入点。ModuleChildMono，挂载在 ModuleHub 所在 GameObject。
    /// 发布方和订阅方通过 Get&lt;T&gt;() 获取通道引用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EventHub : ModuleChildMono
    {
        [SerializeField] private GameEvent[] channels = Array.Empty<GameEvent>();

        private readonly Dictionary<Type, GameEvent> lookup = new();

        protected override void Awake()
        {
            base.Awake();
            foreach (var ch in channels)
            {
                if (ch != null)
                    lookup[ch.GetType()] = ch;
            }
        }

        /// <summary>按类型获取事件通道。未注册时告警。</summary>
        public T Get<T>() where T : GameEvent
        {
            if (lookup.TryGetValue(typeof(T), out var ch))
                return ch as T;
            var names = new string[lookup.Count];
            var i = 0;
            foreach (var kv in lookup) names[i++] = kv.Value.name;
            Debug.LogError($"[EventHub] Channel '{typeof(T).Name}' not found in {gameObject.name}."
                + $" Available: [{string.Join(", ", names)}]");
            return null;
        }

        public override void OnWire() { }
    }
}
