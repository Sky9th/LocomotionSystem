using RedDust.Core.GameService;
using System.Collections;
using AS = RedDust.Services.AssetService.AssetService;
using RedDust.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedDust.Services.Scene
{
    /// <summary>
    /// Pure scene transition: parallel scene + asset loading, weighted progress,
    /// label lifecycle, concurrent guard. No boot awareness — caller handles that.
    /// </summary>
    public class SceneTransition : IGameplaySessionHandler
    {
        private readonly AS _assetService;
        private readonly EventHub _eventHub;

        private bool _isTransitioning;

        public SceneTransition(AS assetService, EventHub eventHub)
        {
            _assetService = assetService;
            _eventHub = eventHub;
        }

        /// <summary>
        /// Execute a scene transition: guard → parallel scene+labels → activate → unload previous → events.
        /// </summary>
        public IEnumerator Transition(SceneLoadConfigSO config, SceneLoadConfigSO previous = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneTransition] Transition already in progress. Ignoring request.");
                yield break;
            }

            _isTransitioning = true;

            var prevName = previous != null ? previous.SceneName : null;
            var prevLabels = previous != null ? previous.AssetLabels : SceneAssetLabel.None;

            _eventHub?.Get<SceneTransitionEvent>()?.Raise(
                new SSceneTransition(config.SceneName, prevName, SceneTransitionPhase.Started));
            PublishProgress($"Loading {config.SceneName}...", 0f);

            // Kick off parallel loading
            bool labelsDone = false;
            _assetService.LoadByLabels<Object>(config.AssetLabels.ToLabelStrings(), () => { labelsDone = true; });

            var sceneHandle = SceneManager.LoadSceneAsync(config.ScenePath, LoadSceneMode.Additive);
            if (sceneHandle == null)
            {
                Debug.LogError($"[SceneTransition] Failed to start scene load: '{config.ScenePath}'");
                _isTransitioning = false;
                yield break;
            }

            // Wait for both, publishing progress: scene 70% + labels 30%
            while (!sceneHandle.isDone || !labelsDone)
            {
                float p = sceneHandle.isDone ? 0.7f : sceneHandle.progress * 0.7f;
                if (labelsDone) p += 0.3f;
                PublishProgress($"Loading {config.SceneName}...", p);
                yield return null;
            }

            // Activate new scene
            var loadedScene = SceneManager.GetSceneByPath(config.ScenePath);
            if (loadedScene.isLoaded)
                SceneManager.SetActiveScene(loadedScene);

            // Unload previous
            if (previous != null)
            {
                var prevScene = SceneManager.GetSceneByName(previous.SceneName);
                if (prevScene.isLoaded)
                {
                    Debug.Log($"[SceneTransition] Unloading previous scene: '{previous.SceneName}'.");
                    yield return SceneManager.UnloadSceneAsync(prevScene);
                }
                _assetService.ReleaseLabel(prevLabels.ToLabelStrings().ToArray());
            }

            // Min display time to prevent flash
            float elapsed = 0f;
            while (elapsed < config.MinDisplayTime) { elapsed += UnityEngine.Time.unscaledDeltaTime; yield return null; }

            _eventHub?.Get<SceneTransitionEvent>()?.Raise(
                new SSceneTransition(config.SceneName, prevName, SceneTransitionPhase.Completed));
            PublishProgress(null, 1f);
            _isTransitioning = false;
        }

        private void PublishProgress(string phase, float progress)
        {
            _eventHub?.Get<SceneProgressEvent>()?.Raise(new SLoadingProgress(phase, Mathf.Clamp01(progress)));
        }

        public void OnGameplaySessionEnd()
        {
            _isTransitioning = false;
        }
    }
}
