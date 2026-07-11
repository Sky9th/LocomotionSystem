using RedDust.Core.GameContext;
using RedDust.Core.Structs;
using RedDust.Core.Modules;
using RedDust.Core.Events;
using RedDust.Services.Input;
using RedDust.Services.GameState;
using RedDust.Services.Scene;
using UnityEngine;

namespace RedDust.Services.Time
{
    public class TimeService : ModuleChildMono
    {
        [SerializeField, Min(0.1f)] private float minScale = 0.2f;
        [SerializeField, Min(0.1f)] private float maxScale = 1f;

        private EventHub _eventHub;
        private float defaultScale = 1f;
        private bool isSceneLoading;
        private bool isGamePaused;

        public override void OnAssemble()
        {
            defaultScale = Mathf.Max(UnityEngine.Time.timeScale, minScale);

            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;

            _eventHub.Get<SceneTransitionEvent>().Register(HandleSceneTransition);
            _eventHub.Get<GameStateChangedEvent>().Register(HandleGameStateChanged);
            _eventHub.Get<InputTimeSlowEvent>().Register(HandleTimeSlow);
            _eventHub.Get<InputTimeResumeEvent>().Register(HandleTimeResume);
        }

        // ── Time Events (EventHub) ──

        private void HandleTimeSlow(SButtonInputPayload payload)
        {
            if (!payload.IsRequested) return;
            if (isSceneLoading || isGamePaused) return;
            defaultScale = minScale;
            UnityEngine.Time.timeScale = defaultScale;
        }

        private void HandleTimeResume(SButtonInputPayload payload)
        {
            if (!payload.IsRequested) return;
            if (isSceneLoading || isGamePaused) return;
            defaultScale = maxScale;
            UnityEngine.Time.timeScale = defaultScale;
        }

        // ── Scene / GameState Events ──

        private void HandleSceneTransition(SSceneTransition evt)
        {
            isSceneLoading = evt.Phase == SceneTransitionPhase.Started;
            ApplyFreeze();
        }

        private void HandleGameStateChanged(SGameState state)
        {
            isGamePaused = state.CurrentState == EGameState.Paused;
            ApplyFreeze();
        }

        private void ApplyFreeze()
        {
            UnityEngine.Time.timeScale = (isSceneLoading || isGamePaused) ? 0f : defaultScale;
        }

        // ── Lifecycle ──

        private void OnDisable()
        {
            RestoreDefaultScale();
        }

        private void OnDestroy()
        {
            if (_eventHub != null)
            {
                _eventHub.Get<SceneTransitionEvent>().Unregister(HandleSceneTransition);
                _eventHub.Get<GameStateChangedEvent>().Unregister(HandleGameStateChanged);
                _eventHub.Get<InputTimeSlowEvent>().Unregister(HandleTimeSlow);
                _eventHub.Get<InputTimeResumeEvent>().Unregister(HandleTimeResume);
            }

            RestoreDefaultScale();
        }

        private void RestoreDefaultScale()
        {
            if (Application.isPlaying)
                UnityEngine.Time.timeScale = defaultScale;
        }
    }
}
