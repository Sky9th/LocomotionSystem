using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// 泛型事件通道。一个 .asset 实例 = 一条事件管道。
    /// 发布者持有引用调用 Raise()，订阅者持有引用调用 Register()/Unregister()。
    /// </summary>
    public abstract class GameEvent<T> : EventChannelBase
    {
        // 不序列化 — 运行时动态填充
        private readonly List<Action<T>> listeners = new();

        public override int ListenerCount => listeners.Count;

        /// <summary>订阅此事件通道。通常在 OnEnable 中调用。</summary>
        public void Register(Action<T> handler)
        {
            if (!listeners.Contains(handler))
                listeners.Add(handler);
        }

        /// <summary>取消订阅。通常在 OnDisable 中调用。</summary>
        public void Unregister(Action<T> handler)
        {
            listeners.Remove(handler);
        }

        /// <summary>发布事件，通知所有订阅者。</summary>
        public void Raise(T payload)
        {
#if UNITY_EDITOR
            NotifyRaised();
#endif
            // 倒序遍历，防止回调中修改列表导致异常
            for (int i = listeners.Count - 1; i >= 0; i--)
                listeners[i]?.Invoke(payload);
        }

        public override void ClearAllListeners()
        {
            listeners.Clear();
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Test)")]
        private void RaiseTest() => Raise(default);
#endif
    }
}
