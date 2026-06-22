using RedDust.Core;
using RedDust.GameState;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.GameTime
{
    public class TimeService : ModuleChildMono
    {
        [SerializeField, Min(0.1f)] private float minScale = 0.2f;
        [SerializeField, Min(0.1f)] private float maxScale = 1f;

        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
        private float defaultScale = 1f;
        private bool isSceneLoading;
        private bool isGamePaused;

        public override void OnAssemble()
        {
            defaultScale = Mathf.Max(Time.timeScale, minScale);

            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            GameContext.Instance.TryResolveService(out _dispatcher);
            _dispatcher.Subscribe<SIActionWorldSpeed>(HandleTimeScaleRequested);
            _dispatcher.Subscribe<SSceneLoadStart>(HandleSceneLoadStart);
            _dispatcher.Subscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
            _dispatcher.Subscribe<SGameState>(HandleGameStateChanged);
        }

        private void HandleSceneLoadStart(SSceneLoadStart _, MetaStruct __)
        {
            isSceneLoading = true;
            ApplyFreeze();
        }

        private void HandleSceneLoadComplete(SSceneLoadComplete _, MetaStruct __)
        {
            isSceneLoading = false;
            ApplyFreeze();
        }

        private void HandleGameStateChanged(SGameState state, MetaStruct meta)
        {
            isGamePaused = state.CurrentState == EGameState.Paused;
            ApplyFreeze();
        }

        private void ApplyFreeze()
        {
            Time.timeScale = (isSceneLoading || isGamePaused) ? 0f : defaultScale;
        }

        private void HandleTimeScaleRequested(SIActionWorldSpeed action, MetaStruct meta)
        {
            if (isSceneLoading || isGamePaused) return;
            defaultScale = Mathf.Clamp(action.TargetScale, minScale, maxScale);
            Time.timeScale = defaultScale;
        }

        private void OnDisable()
        {
            RestoreDefaultScale();
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
            {
                _dispatcher.Unsubscribe<SIActionWorldSpeed>(HandleTimeScaleRequested);
                _dispatcher.Unsubscribe<SSceneLoadStart>(HandleSceneLoadStart);
                _dispatcher.Unsubscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
                _dispatcher.Unsubscribe<SGameState>(HandleGameStateChanged);
            }
            RestoreDefaultScale();
        }

        private void RestoreDefaultScale()
        {
            if (Application.isPlaying)
                Time.timeScale = defaultScale;
        }
    }
}
