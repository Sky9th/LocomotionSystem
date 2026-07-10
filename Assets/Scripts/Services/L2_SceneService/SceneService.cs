using System.Collections;
using System.Collections.Generic;
using RedDust.Assets;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Modding;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RedDust.GameScene
{
    /// <summary>
    /// L2 scene loading hub.
    /// Public surface is a single <see cref="Load"/> method — everything else is private.
    /// </summary>
    public class SceneService : ModuleChildMono, IGameplaySessionHandler
    {
#if UNITY_EDITOR
        private const string EditorStartupSceneNameKey = "RedDust.Editor.StartupSceneName";
#endif

        [SerializeField] private List<SceneLoadConfigSO> _configs = new();

        private EventHub _eventHub;
        private AssetService _assetService;
        private SceneTransition _gate;
        private SceneLoadConfigSO _currentConfig;

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            if (!GameContext.Instance.TryResolveService(out _assetService)) return;

            _gate = new SceneTransition(_assetService, _eventHub);

        }

        // ── Public API ──

        /// <summary>
        /// Load the default startup scene (MainMenu, or editor-preference override).
        /// Called once by GameService after all services are wired.
        /// </summary>
        public void Load()
        {
#if UNITY_EDITOR
            var preferred = ConsumeEditorScenePreference();
            if (!string.IsNullOrEmpty(preferred) && System.Enum.TryParse<SceneId>(preferred, out var id))
            {
                Load(id);
                return;
            }
#endif
            Load(SceneId.MainMenu);
        }

        /// <summary>
        /// Load or reload a scene by its fixed identifier.
        /// </summary>
        public void Load(SceneId sceneId) => StartCoroutine(LoadRoutine(sceneId));

        private IEnumerator LoadRoutine(SceneId sceneId)
        {
            var config = _configs.Find(c => c.Scene == sceneId);
            if (config == null)
            {
                Debug.LogError($"[SceneService] No config for '{sceneId}'.");
                yield break;
            }

            yield return _assetService.EnsureInitialized();
            yield return EnsureBootReady();

            var previous = _currentConfig;
            _currentConfig = config;
            yield return _gate.Transition(config, previous);
        }

        // ── Private ──

        private IEnumerator EnsureBootReady()
        {
            if (_assetService.BootComplete) yield break;

            bool done = false;
            _assetService.LoadByLabels<Object>(
                new List<string> { SceneAssetLabel.Boot.ToLabelStrings()[0] },
                () => { done = true; });
            while (!done) yield return null;

            _assetService.RunBootInit();

            // Load HybridCLR AOT metadata before any mod loading
            bool metadataDone = false;
            _assetService.LoadAOTMetadata(() => { metadataDone = true; });
            while (!metadataDone) yield return null;

            // Load mods now that HybridCLR AOT metadata is ready.
            // Mod DLLs reference AOT types; LoadMetadataForAOTAssembly must
            // complete first so the HybridCLR interpreter can resolve them.
            if (GameContext.Instance.TryResolveService(out ModService modService))
                modService.LoadAllMods();
        }

        public void OnGameplaySessionEnd()
        {
            _currentConfig = null;
            _gate.OnGameplaySessionEnd();
        }

        /// <summary>
        /// Read and immediately clear the editor scene preference key.
        /// Returns the scene name set by a custom editor boot flow, or null.
        /// </summary>
        private static string ConsumeEditorScenePreference()
        {
#if UNITY_EDITOR
            string sceneName = SessionState.GetString(EditorStartupSceneNameKey, string.Empty);
            if (!string.IsNullOrEmpty(sceneName))
                SessionState.EraseString(EditorStartupSceneNameKey);
            return sceneName;
#else
            return null;
#endif
        }
    }
}
