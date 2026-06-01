using System;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// 所有事件通道资产的抽象基类。
    /// 提供公共类型标记（供 Editor 扫描）和 Editor 期追踪钩子。
    /// </summary>
    public abstract class EventChannelBase : ScriptableObject
    {
        /// <summary>当前注册的 listener 数量（仅运行时）</summary>
        public abstract int ListenerCount { get; }

        /// <summary>清空所有已注册的 listener</summary>
        public abstract void ClearAllListeners();

#if UNITY_EDITOR
        /// <summary>
        /// 任何事件通道 Raise 时触发。Editor 工具订阅此事件实现运行时拓扑高亮。
        /// </summary>
        public static event Action<EventChannelBase> OnAnyRaised;

        protected void NotifyRaised() => OnAnyRaised?.Invoke(this);
#endif
    }
}
