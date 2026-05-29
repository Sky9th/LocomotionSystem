using RedDust.Core;
using RedDust.GameState;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.GameTime
{
    public class TimeService : BaseService
    {
        [SerializeField, Min(0.1f)] private float minScale = 0.2f;
        [SerializeField, Min(0.1f)] private float maxScale = 1f;

        private float defaultScale = 1f;
        private bool isSceneLoading;
        private bool isGamePaused;

        protected override bool OnRegister(GameContext context)
        {
            context.RegisterService(this);
            defaultScale = Mathf.Max(Time.timeScale, minScale);
            return true;
        }

        protected override void OnSubscriptionsActivated()
        {
            Dispatcher.Subscribe<SIActionWorldSpeed>(HandleTimeScaleRequested);
            Dispatcher.Subscribe<SSceneLoadStart>(HandleSceneLoadStart);
            Dispatcher.Subscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
            Dispatcher.Subscribe<SGameState>(HandleGameStateChanged);
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
            if (Dispatcher != null)
            {
                Dispatcher.Unsubscribe<SIActionWorldSpeed>(HandleTimeScaleRequested);
                Dispatcher.Unsubscribe<SSceneLoadStart>(HandleSceneLoadStart);
                Dispatcher.Unsubscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
                Dispatcher.Unsubscribe<SGameState>(HandleGameStateChanged);
            }
            RestoreDefaultScale();
        }

        private void RestoreDefaultScale()
        {
            if (Application.isPlaying)
                Time.timeScale = defaultScale;
        }

        protected override void OnServicesReady() { }

        protected override void OnDispatcherAttached() { }
    }
}
