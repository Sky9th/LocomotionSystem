using System.Collections.Generic;
using DG.Tweening;
using RedDust.Character;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.GameState;
using RedDust.Properties;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.UI
{

    public class UIService : ModuleChildMono, IGameplaySessionHandler
    {
        [Header("Config")]
        [SerializeField] private UIPanelConfigSO panelConfig;
        [SerializeField] private Transform screenContainer;
        [SerializeField] private Transform overlayContainer;
        [SerializeField] private Transform modalContainer;
        [SerializeField] private CanvasGroup loadingCanvasGroup;

        private EventHub _eventHub;
        private readonly Dictionary<UIScreenId, PanelState> screenStates = new();
        private readonly Dictionary<UIOverlayId, PanelState> overlayStates = new();
        private UIScreen currentScreen;
        private UIScreenId currentScreenId;
        private bool hasCurrentScreen;
        private readonly List<UIOverlay> activeOverlays = new();
        private EGameState pendingTargetState;
        private CharacterActor _playerActor;

        public bool IsInputBlocked { get; private set; }

        public override void OnAssemble()
        {
            if (panelConfig != null) panelConfig.BuildLookup();

            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            _eventHub.Get<GameStateChangedEvent>().Register(HandleGameState);
            _eventHub.Get<PlayerSpawnedEvent>().Register(HandlePlayerSpawned);
            _eventHub.Get<SceneLoadStartEvent>().Register(HandleSceneLoadStart);
            _eventHub.Get<SceneLoadCompleteEvent>().Register(HandleSceneLoadComplete);
        }

        private void OnDestroy()
        {
            if (_eventHub == null) return;
            _eventHub.Get<GameStateChangedEvent>().Unregister(HandleGameState);
            _eventHub.Get<PlayerSpawnedEvent>().Unregister(HandlePlayerSpawned);
            _eventHub.Get<SceneLoadStartEvent>().Unregister(HandleSceneLoadStart);
            _eventHub.Get<SceneLoadCompleteEvent>().Unregister(HandleSceneLoadComplete);
        }

        // ---- Public API ----

        public void ShowScreen(UIScreenId id, object args = null)
        {
            if (!TryGetScreen(id, out UIScreen screen)) return;
            if (screen == currentScreen) return;

            if (currentScreen != null)
            {
                var old = currentScreen;
                var oldId = currentScreenId;
                currentScreen = null;
                hasCurrentScreen = false;

                var exitSeq = old.PlayExitSequence();
                if (exitSeq != null)
                    exitSeq.OnComplete(() =>
                    {
                        Destroy(old.gameObject);
                        screenStates.Remove(oldId);
                        ActivateScreen(screen, id, args);
                    });
                else
                    ActivateScreen(screen, id, args);
            }
            else
            {
                ActivateScreen(screen, id, args);
            }
        }

        private void ActivateScreen(UIScreen screen, UIScreenId id, object args)
        {
            currentScreen = screen;
            currentScreenId = id;
            hasCurrentScreen = true;
            screen.gameObject.SetActive(true);
            screen.PlayEnterSequence(args);
        }

        public void HideScreen(UIScreenId id)
        {
            if (!TryGetScreen(id, out UIScreen screen)) return;
            if (screen == currentScreen)
            {
                currentScreen = null;
                hasCurrentScreen = false;
            }

            screenStates.Remove(id);
            var exitSeq = screen.PlayExitSequence();
            if (exitSeq != null)
                exitSeq.OnComplete(() =>
                {
                    if (screen != null) Destroy(screen.gameObject);
                });
            else
                Destroy(screen.gameObject);
        }

        public void ShowOverlay(UIOverlayId id, object args = null)
        {
            if (!TryGetOverlay(id, out UIOverlay overlay)) return;

            var exists = activeOverlays.Find(o => o.gameObject == overlay.gameObject);
            if (exists != null) return;

            activeOverlays.Add(overlay);
            overlay.gameObject.SetActive(true);
            overlay.PlayEnterSequence(args);
        }

        public void HideOverlay(UIOverlayId id)
        {
            if (!TryGetOverlay(id, out UIOverlay overlay)) return;
            if (!activeOverlays.Remove(overlay)) return;

            var exitSeq = overlay.PlayExitSequence();
            if (exitSeq != null)
                exitSeq.OnComplete(() =>
                {
                    if (overlay != null) Destroy(overlay.gameObject);
                });
            else if (overlay != null)
                Destroy(overlay.gameObject);
        }

        private void HideAllOverlays()
        {
            foreach (var overlay in activeOverlays)
            {
                if (overlay != null)
                    Destroy(overlay.gameObject);
            }
            activeOverlays.Clear();
            overlayStates.Clear();
        }

        public void OnGameplaySessionEnd()
        {
            _playerActor = null;
            HideAllOverlays();
        }

        public bool TryGetSnapshot<T>(out T snapshot) where T : struct
        {
            snapshot = default;
            return GameContext.Instance != null && GameContext.Instance.TryGetSnapshot(out snapshot);
        }

        public bool TryGetPlayerProps(out PropertyTable props)
        {
            props = _playerActor != null ? _playerActor.BuildContext.Properties : null;
            return props != null;
        }

        public void RequestNewGame()
        {
            StartSceneTransition("NewGame", EGameState.Playing);
        }

        public void RequestMainMenu()
        {
            if (IsInputBlocked) return;
            IsInputBlocked = true;
            pendingTargetState = EGameState.MainMenu;

            if (currentScreen != null)
            {
                var exitSeq = currentScreen.PlayExitSequence();
                if (exitSeq != null)
                {
                    exitSeq.OnComplete(() =>
                    {
                        Destroy(currentScreen.gameObject);
                        screenStates.Remove(currentScreenId);
                        currentScreen = null;
                        hasCurrentScreen = false;
                        _eventHub.Get<SceneUnloadRequestEvent>().Raise(new SUnloadSceneRequest(null));
                    });
                    return;
                }
                Destroy(currentScreen.gameObject);
                screenStates.Remove(currentScreenId);
                currentScreen = null;
                hasCurrentScreen = false;
            }

            _eventHub.Get<SceneUnloadRequestEvent>().Raise(new SUnloadSceneRequest(null));
        }

        public void RequestResume()
        {
            _eventHub.Get<GameStateChangeRequestEvent>().Raise(new SGameStateRequest(EGameState.Playing));
        }

        public void RequestQuit()
        {
            Application.Quit();
        }

        // ---- Internal ----

        private void StartSceneTransition(string sceneName, EGameState targetState)
        {
            if (IsInputBlocked) return;
            IsInputBlocked = true;
            pendingTargetState = targetState;

            if (currentScreen != null)
            {
                var exitSeq = currentScreen.PlayExitSequence();
                if (exitSeq != null)
                {
                    exitSeq.OnComplete(() =>
                    {
                        Destroy(currentScreen.gameObject);
                        screenStates.Remove(currentScreenId);
                        currentScreen = null;
                        hasCurrentScreen = false;
                        _eventHub.Get<SceneLoadRequestEvent>().Raise(new SLoadSceneRequest(sceneName));
                    });
                    return;
                }
                Destroy(currentScreen.gameObject);
                screenStates.Remove(currentScreenId);
                currentScreen = null;
                hasCurrentScreen = false;
            }

            _eventHub.Get<SceneLoadRequestEvent>().Raise(new SLoadSceneRequest(sceneName));
        }

        private void HandleSceneLoadStart(SSceneLoadStart _)
        {
            loadingCanvasGroup.alpha = 1f;
        }

        private void HandleSceneLoadComplete(SSceneLoadComplete evt)
        {
            loadingCanvasGroup.alpha = 0f;

            if (pendingTargetState != EGameState.Initializing)
                _eventHub.Get<GameStateChangeRequestEvent>().Raise(new SGameStateRequest(pendingTargetState));

            IsInputBlocked = false;
        }

        private bool TryGetScreen(UIScreenId id, out UIScreen screen)
        {
            screen = null;

            if (screenStates.TryGetValue(id, out var state))
            {
                screen = state.Instance as UIScreen;
                return screen != null;
            }

            if (!panelConfig.TryGetScreen(id, out var prefab))
            {
                Debug.LogError($"[UIService] Screen '{id}' not found in PanelConfig.", this);
                return false;
            }

            if (prefab == null)
            {
                Debug.LogError($"[UIService] Screen '{id}' prefab is null.", this);
                return false;
            }

            var instance = Instantiate(prefab, screenContainer);
            instance.name = id.ToString();

            screen = instance.GetComponent<UIScreen>();
            if (screen == null)
            {
                Destroy(instance);
                return false;
            }

            screenStates[id] = new PanelState { Instance = screen };
            screen.Initialize(this);
            return true;
        }

        private bool TryGetOverlay(UIOverlayId id, out UIOverlay overlay)
        {
            overlay = null;

            if (overlayStates.TryGetValue(id, out var state))
            {
                overlay = state.Instance as UIOverlay;
                return overlay != null;
            }

            if (!panelConfig.TryGetOverlay(id, out var prefab))
            {
                Debug.LogError($"[UIService] Overlay '{id}' not found in PanelConfig.", this);
                return false;
            }

            if (prefab == null)
            {
                Debug.LogError($"[UIService] Overlay '{id}' prefab is null.", this);
                return false;
            }

            var instance = Instantiate(prefab, overlayContainer);
            instance.name = id.ToString();

            overlay = instance.GetComponent<UIOverlay>();
            if (overlay == null)
            {
                Destroy(instance);
                return false;
            }

            overlayStates[id] = new PanelState { Instance = overlay };
            overlay.Initialize(this);
            return true;
        }

        private void HandlePlayerSpawned(SPlayerSpawnedEvent evt)
        {
            if (!evt.IsLocalPlayer) return;
            _playerActor = evt.Root != null ? evt.Root.GetComponent<CharacterActor>() : null;
        }

        private void HandleGameState(SGameState state)
        {
            switch (state.CurrentState)
            {
                case EGameState.MainMenu:
                    ShowScreen(UIScreenId.MainMenu);
                    break;
                case EGameState.Paused:
                    ShowScreen(UIScreenId.PauseMenu);
                    break;
                case EGameState.Playing:
                    if (hasCurrentScreen)
                        HideScreen(currentScreenId);
                    if (state.PreviousState != EGameState.Paused)
                        ShowOverlay(UIOverlayId.VitalsOverlay);
                    break;
            }
        }

        private class PanelState
        {
            public MonoBehaviour Instance;
        }
    }
}
