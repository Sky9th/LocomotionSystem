using System;
using RedDust.Core;
using RedDust.GameState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入事件抽象基类。收归生命周期、InputAction 绑定、通道逻辑，
    /// 子类只覆盖 OnPerformed/OnCanceled，翻译输入后调用 Raise()。
    /// </summary>
    public abstract class InputEventBase : EventChannelBase
    {
        [Header("Input Binding")]
        [SerializeField] private InputActionReference inputAction;

        public InputActionReference InputActionRef => inputAction;

        [Header("State Permission")]
        [SerializeField] private EGameState[] supportedStates = Array.Empty<EGameState>();

        private InputAction runtimeAction;
        private bool initialized;

        /// <summary>绑定的 InputAction（子类只读）</summary>
        protected InputAction RuntimeAction => runtimeAction;

        // ── Lifecycle ──

        public void InitializeEvent()
        {
            if (initialized) return;

            runtimeAction = inputAction?.action;
            if (runtimeAction == null)
            {
                Debug.LogError($"{GetType().Name} '{name}' is missing an InputAction reference.");
                return;
            }

            runtimeAction.performed += OnPerformed;
            runtimeAction.canceled += OnCanceled;
            initialized = true;
        }

        public void EnableEvent() => runtimeAction?.Enable();

        public void DisableEvent() => runtimeAction?.Disable();

        public void DisposeEvent()
        {
            DisableEvent();
            if (runtimeAction != null)
            {
                runtimeAction.performed -= OnPerformed;
                runtimeAction.canceled -= OnCanceled;
            }
            initialized = false;
            runtimeAction = null;
        }

        public bool SupportsState(EGameState state)
        {
            if (supportedStates == null || supportedStates.Length == 0)
                return true;

            for (int i = 0; i < supportedStates.Length; i++)
                if (supportedStates[i] == state)
                    return true;

            return false;
        }

        // ── Frame Cleanup ──

        /// <summary>帧末调用。子类有边沿信号则 override 归零。</summary>
        public virtual void ClearFrameSignals() { }

        // ── Subclass Contract ──

        /// <summary>Unity Input 触发时回调。子类更新数据后调用 Raise()。</summary>
        protected abstract void OnPerformed(InputAction.CallbackContext ctx);

        /// <summary>Unity Input 取消时回调。子类归零数据后调用 Raise()。</summary>
        protected abstract void OnCanceled(InputAction.CallbackContext ctx);

        /// <summary>通知所有订阅者。</summary>
        protected void Raise()
        {
#if UNITY_EDITOR
            NotifyRaised();
#endif
            InvokeOnRaised();
        }

        private void OnDestroy() => DisposeEvent();
    }
}
