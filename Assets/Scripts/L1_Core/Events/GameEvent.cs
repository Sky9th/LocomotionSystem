using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// 非泛型事件通道标记——用于任意通道的类型约束。
    /// 不动态逻辑，只为 EventHub / PassiveAbilitySO / Editor 提供比 ScriptableObject 更窄的类型。
    /// </summary>
    public abstract class GameEvent : ScriptableObject
    {
        public abstract int ListenerCount { get; }
        public abstract void ClearAllListeners();
    }

    /// <summary>
    /// 泛型事件通道。一个 .asset 实例 = 一条事件管道。
    /// 发布者持有引用调用 Raise(T)，订阅者持有引用调用 Register(Action&lt;T&gt;)。
    /// </summary>
    public abstract class GameEvent<T> : GameEvent
    {
        private readonly List<Action<T>> listeners = new();

        /// <summary>当前注册的 listener 数量（仅运行时）</summary>
        public override int ListenerCount => listeners.Count;

        /// <summary>订阅此事件通道。</summary>
        public void Register(Action<T> handler)
        {
            if (!listeners.Contains(handler))
                listeners.Add(handler);
        }

        /// <summary>取消订阅。</summary>
        public void Unregister(Action<T> handler)
        {
            listeners.Remove(handler);
        }

        /// <summary>发布事件，通知所有订阅者。</summary>
        public void Raise(T payload)
        {
#if UNITY_EDITOR
            Editor_NotifyRaised();
#endif
            // 倒序遍历，防止回调中修改列表导致异常
            for (int i = listeners.Count - 1; i >= 0; i--)
                listeners[i]?.Invoke(payload);
        }

        /// <summary>清空所有已注册的 listener</summary>
        public override void ClearAllListeners()
        {
            listeners.Clear();
        }

#if UNITY_EDITOR
        /// <summary>任何事件通道 Raise 时触发。Editor 工具订阅此事件实现运行时拓扑高亮。</summary>
        public static event Action<GameEvent<T>> OnAnyRaised;

        private void Editor_NotifyRaised() => OnAnyRaised?.Invoke(this);

        [ContextMenu("Raise (Test)")]
        private void RaiseTest() => Raise(default);
#endif
    }
}
