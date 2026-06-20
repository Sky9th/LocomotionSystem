using RedDust.Core;
using RedDust.GameState;
using UnityEngine;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入服务。绑定 InputAction、管理 EventSO 生命周期、响应 GameState 门控。
    /// </summary>
    [DisallowMultipleComponent]
    public class InputService : ModuleComponent
    {
        [SerializeField] private InputEventBase[] inputEvents = System.Array.Empty<InputEventBase>();

        // ── Lifecycle ──

        public override void OnAssemble()
        {
            foreach (var evt in inputEvents)
                evt.InitializeEvent();
        }

        public override void OnWire()
        {
            GameContext.Instance.RegisterService(this);
            GameService.Instance?.NotifyServiceWired();
        }

        private void OnEnable()
        {
            foreach (var evt in inputEvents)
                evt.EnableEvent();
        }

        private void OnDisable()
        {
            foreach (var evt in inputEvents)
                evt.DisableEvent();
        }

        private void OnDestroy()
        {
            foreach (var evt in inputEvents)
                evt.DisposeEvent();
        }

        private void LateUpdate()
        {
            for (int i = 0; i < inputEvents.Length; i++)
                inputEvents[i].ClearFrameSignals();
        }

        // ── Game State ──

        // TODO: GameState 门控暂未实装 — 需由 GameStateService 在状态切换时调用
        public void ApplyGameState(EGameState state)
        {
            bool canEnable = isActiveAndEnabled;
            foreach (var evt in inputEvents)
            {
                if (evt.SupportsState(state) && canEnable)
                    evt.EnableEvent();
                else
                    evt.DisableEvent();
            }
        }

    }
}
