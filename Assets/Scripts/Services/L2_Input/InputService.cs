using System;
using RedDust.Core;
using RedDust.GameState;
using UnityEngine;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入服务。管理 InputEvent 资产的生命周期：初始化、启停、状态权限。
    /// </summary>
    [DisallowMultipleComponent]
    public class InputService : BaseService
    {
        [SerializeField] private EventChannelBase[] inputEvents = Array.Empty<EventChannelBase>();

        private bool inputEventsConfigured;
        private EGameState currentGameState = EGameState.Initializing;
        private bool hasGameStateSnapshot;

        // ── Lifecycle ──

        protected override bool OnRegister(GameContext context)
        {
            context.RegisterService(this);
            inputEventsConfigured = false;
            return true;
        }

        protected override void OnDispatcherAttached()
        {
            base.OnDispatcherAttached();
            InitializeInputEvents();

            if (isActiveAndEnabled)
                EnableInputEvents();

            SyncInitialGameState();
        }

        protected override void OnSubscriptionsActivated()
        {
            Dispatcher.Subscribe<SGameState>(HandleGameStateChanged);
        }

        private void OnEnable()
        {
            if (IsRegistered)
                EnableInputEvents();
        }

        private void OnDisable()
        {
            DisableInputEvents();
        }

        private void OnDestroy()
        {
            Dispatcher?.Unsubscribe<SGameState>(HandleGameStateChanged);
            DisposeInputEvents();
        }

        // ── InputEvent 管理 ──

        private void InitializeInputEvents()
        {
            if (inputEventsConfigured) return;

            foreach (var obj in inputEvents)
            {
                if (obj is IInputEvent evt)
                    evt.InitializeEvent();
            }

            inputEventsConfigured = true;
        }

        private void EnableInputEvents()
        {
            if (!inputEventsConfigured) return;

            foreach (var obj in inputEvents)
            {
                if (obj is IInputEvent evt)
                    evt.EnableEvent();
            }
        }

        private void DisableInputEvents()
        {
            if (!inputEventsConfigured) return;

            foreach (var obj in inputEvents)
            {
                if (obj is IInputEvent evt)
                    evt.DisableEvent();
            }
        }

        private void DisposeInputEvents()
        {
            if (!inputEventsConfigured) return;

            foreach (var obj in inputEvents)
            {
                if (obj is IInputEvent evt)
                    evt.DisposeEvent();
            }
        }

        // ── Game State ──

        private void HandleGameStateChanged(SGameState snapshot, MetaStruct meta)
        {
            ApplyGameState(snapshot.CurrentState);
        }

        private void SyncInitialGameState()
        {
            if (GameContext != null && GameContext.TryGetSnapshot(out SGameState snapshot))
                ApplyGameState(snapshot.CurrentState, force: true);
            else
                ApplyGameState(EGameState.Initializing, force: true);
        }

        private void ApplyGameState(EGameState nextState, bool force = false)
        {
            if (!force && hasGameStateSnapshot && nextState == currentGameState)
                return;

            currentGameState = nextState;
            hasGameStateSnapshot = true;

            if (inputEventsConfigured)
                EnforceStatePermissions();
        }

        private void EnforceStatePermissions()
        {
            if (!inputEventsConfigured) return;

            bool canEnable = IsRegistered && isActiveAndEnabled;
            foreach (var obj in inputEvents)
            {
                if (obj is not IInputEvent evt) continue;

                bool supportsState = hasGameStateSnapshot ? evt.SupportsState(currentGameState) : true;
                if (!supportsState || !canEnable)
                    evt.DisableEvent();
                else
                    evt.EnableEvent();
            }
        }

        protected override void OnServicesReady() { }
    }
}
