using System.Collections.Generic;
using RedDust.Addressables;
using RedDust.Core;
using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// L2 loading hub. Owns all loading: boot preload, full scene transitions,
    /// reload, unload, and (future) streaming / background preload.
    /// </summary>
    public class SceneService : ModuleChildMono, IGameplaySessionHandler
    {
        private readonly struct RuntimeSceneState
        {
            public readonly string SceneName;
            public readonly string ScenePath;
            public readonly SceneAssetLabel AssetLabels;

            public RuntimeSceneState(string sceneName, string scenePath, SceneAssetLabel assetLabels)
            {
                SceneName = sceneName;
                ScenePath = scenePath;
                AssetLabels = assetLabels;
            }
        }

        [SerializeField] private SceneLoadConfigSO _firstSceneConfig;
        [SerializeField] private List<SceneLoadConfigSO> _configs = new();

        private EventHub _eventHub;
        private BootPipeline _boot;
        private SceneLoader _loader;
        private TransitionGate _gate;
        private LoadProgress _progress;
        private RuntimeSceneState? _currentState;

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
            _boot = new BootPipeline();
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            GameContext.Instance.TryResolveService(out AddressablesService addressables);

            _progress = new LoadProgress(_eventHub);
            _loader = new SceneLoader(addressables, this);
            _gate = new TransitionGate(_boot, _loader, _progress, _eventHub);
            _boot.Initialize(addressables, _gate, _progress);
            _boot.Register(new PropertyDefBootTask(addressables));

            _eventHub.Get<SceneRequestEvent>().Register(HandleSceneRequest);
        }

        private void OnDestroy()
        {
            if (_eventHub == null) return;
            _eventHub.Get<SceneRequestEvent>().Unregister(HandleSceneRequest);
        }

        // ── Public API ──

        public void BeginPreload(string preferredSceneName = null)
        {
            var initialConfig = ResolveInitialConfig(preferredSceneName);
            _currentState = CreateRuntimeState(initialConfig);
            StartCoroutine(_boot.Run(initialConfig));
        }

        public void RegisterBootTask(IBootTask task) => _boot.Register(task);

        // ── Event handlers ──

        private void HandleSceneRequest(SSceneRequest request)
        {
            var sceneName = string.IsNullOrEmpty(request.SceneName)
                ? _currentState?.SceneName
                : request.SceneName;

            switch (request.Type)
            {
                case SceneRequestType.Load:
                case SceneRequestType.Reload:
                    if (string.IsNullOrEmpty(sceneName)) return;
                    var config = _configs.Find(c => c.SceneName == sceneName);
                    if (config != null)
                    {
                        RuntimeSceneState? previousState = _currentState;
                        _currentState = CreateRuntimeState(config);
                        StartCoroutine(_gate.Begin(
                            config,
                            previousState?.SceneName,
                            previousState?.ScenePath,
                            previousState?.AssetLabels ?? SceneAssetLabel.None));
                    }
                    else
                        Debug.LogError($"[SceneService] No config for '{sceneName}'.");
                    break;

                case SceneRequestType.Unload:
                    if (string.IsNullOrEmpty(sceneName)) return;
                    StartCoroutine(_loader.UnloadSceneAsync(sceneName));
                    _currentState = null;
                    break;
            }
        }

        public void OnGameplaySessionEnd()
        {
            _currentState = null;
            _gate.OnGameplaySessionEnd();
        }

        private SceneLoadConfigSO ResolveInitialConfig(string preferredSceneName)
        {
            if (string.IsNullOrEmpty(preferredSceneName))
                return _firstSceneConfig;

            if (_firstSceneConfig != null && _firstSceneConfig.SceneName == preferredSceneName)
                return _firstSceneConfig;

            var config = _configs.Find(c => c.SceneName == preferredSceneName);
            if (config != null)
                return config;

            Debug.LogWarning($"[SceneService] Preferred startup scene '{preferredSceneName}' not found. Falling back to firstSceneConfig.");
            return _firstSceneConfig;
        }

        private static RuntimeSceneState CreateRuntimeState(SceneLoadConfigSO config)
        {
            return new RuntimeSceneState(config.SceneName, config.ScenePath, config.AssetLabels);
        }
    }
}
