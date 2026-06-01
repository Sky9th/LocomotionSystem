using System;
using System.Collections.Generic;
using RedDust.Core;
using RedDust.GameState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入事件泛型基类。Unity Input System 是真实发布者，
    /// 订阅者通过 Register/Unregister 收听。
    /// </summary>
    public abstract class InputEvent<T> : EventChannelBase, IInputEvent
    {
        [Header("Unity Input")]
        [SerializeField] private InputActionReference inputAction;

        private InputAction runtimeAction;
        private bool isContextBound;
        private readonly List<Action<T>> listeners = new();

        public override int ListenerCount => listeners.Count;

        // ── IInputEvent Lifecycle ──

        public void InitializeEvent()
        {
            if (isContextBound) return;

            runtimeAction = inputAction?.action;
            if (runtimeAction == null)
            {
                Debug.LogError($"InputEvent '{name}' is missing an InputAction reference.");
                return;
            }

            runtimeAction.performed += OnPerformed;
            runtimeAction.canceled += OnCanceled;
            isContextBound = true;
        }

        public void EnableEvent()
        {
            if (!isContextBound || runtimeAction == null) return;
            runtimeAction.Enable();
        }

        public void DisableEvent()
        {
            runtimeAction?.Disable();
        }

        public void DisposeEvent()
        {
            DisableEvent();
            if (runtimeAction != null)
            {
                runtimeAction.performed -= OnPerformed;
                runtimeAction.canceled -= OnCanceled;
            }
            isContextBound = false;
            runtimeAction = null;
        }

        public bool SupportsState(EGameState state) => OnSupportsState(state);

        protected virtual bool OnSupportsState(EGameState state) => true;

        // ── Subclass Contract ──

        /// <summary>Unity Input System performed 回调。子类翻译并调用 Raise(payload)。</summary>
        protected abstract void OnPerformed(InputAction.CallbackContext ctx);

        /// <summary>Unity Input System canceled 回调。</summary>
        protected abstract void OnCanceled(InputAction.CallbackContext ctx);

        // ── Channel API ──

        public void Register(Action<T> handler)
        {
            if (!listeners.Contains(handler))
                listeners.Add(handler);
        }

        public void Unregister(Action<T> handler)
        {
            listeners.Remove(handler);
        }

        public void Raise(T payload)
        {
#if UNITY_EDITOR
            NotifyRaised();
#endif
            for (int i = listeners.Count - 1; i >= 0; i--)
                listeners[i]?.Invoke(payload);
        }

        public override void ClearAllListeners() => listeners.Clear();
    }
}
