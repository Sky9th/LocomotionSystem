using System.Collections;
using System.Collections.Generic;
using RedDust.Core;
using RedDust.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedDust.GameScene
{
    /// <summary>
    /// Orchestrates a scene transition: waits for boot gate, raises start/complete events,
    /// coordinates parallel scene + asset loading with weighted progress, enforces min display
    /// time, manages asset label lifecycle (release on unload), and guards against concurrent transitions.
    /// </summary>
    public class TransitionGate : IGameplaySessionHandler
    {
        private readonly BootPipeline _boot;
        private readonly SceneLoader _loader;
        private readonly LoadProgress _progress;
        private readonly EventHub _eventHub;

        private bool _isTransitioning;

        public TransitionGate(BootPipeline boot, SceneLoader loader, LoadProgress progress, EventHub eventHub)
        {
            _boot = boot;
            _loader = loader;
            _progress = progress;
            _eventHub = eventHub;
        }

        /// <summary>
        /// Execute a scene transition. Rejects concurrent calls via _isTransitioning guard.
        /// </summary>
        public IEnumerator Begin(
            SceneLoadConfigSO config,
            string previousSceneName = null,
            string previousScenePath = null,
            SceneAssetLabel previousAssetLabels = SceneAssetLabel.None)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[TransitionGate] Transition already in progress. Ignoring request.");
                yield break;
            }

            _isTransitioning = true;

            // Wait for boot tasks
            yield return _boot.WaitUntilTasksComplete();

            // Raise start event
            _eventHub?.Get<SceneTransitionEvent>()?.Raise(
                new SSceneTransition(config.SceneName, previousSceneName, SceneTransitionPhase.Started));
            _progress.Publish($"Loading {config.SceneName}...", 0f);

            // Parallel: scene + asset labels (weighted 70/30)
            _progress.BeginComposite(2);

            var labelOp = _loader.LoadLabelAsync(config.AssetLabels.ToLabelStrings(), null);

            float sceneProgress = 0f;
            bool labelsDone = false;
            bool sceneDone = false;

            var sceneHandle = SceneManager.LoadSceneAsync(config.ScenePath, LoadSceneMode.Additive);
            if (sceneHandle == null)
            {
                Debug.LogError($"[TransitionGate] Failed to start scene load: '{config.ScenePath}'");
                _isTransitioning = false;
                yield break;
            }

            while (!sceneHandle.isDone || !labelsDone)
            {
                if (!sceneHandle.isDone)
                {
                    sceneProgress = sceneHandle.progress;
                    _progress.UpdateTrack(0, sceneProgress * 0.7f);
                }
                else if (!sceneDone)
                {
                    sceneDone = true;
                    _progress.UpdateTrack(0, 0.7f);
                }

                if (labelsDone)
                    _progress.UpdateTrack(1, 0.3f);

                if (!labelsDone && !labelOp.MoveNext())
                {
                    labelsDone = true;
                    _progress.UpdateTrack(1, 0.3f);
                }

                _progress.Publish($"Loading {config.SceneName}...", _progress.TotalProgress);
                yield return null;
            }

            var loadedScene = SceneManager.GetSceneByPath(config.ScenePath);
            if (loadedScene.isLoaded)
                SceneManager.SetActiveScene(loadedScene);

            // Unload previous scene and release its labels
            if (!string.IsNullOrEmpty(previousSceneName))
            {
                Debug.Log($"[TransitionGate] Unloading previous scene: '{previousSceneName}'.");
                yield return _loader.UnloadSceneAsync(previousSceneName);
                _loader.ReleaseLabels(previousAssetLabels.ToLabelStrings());
            }

            yield return WaitMinDisplay(config.MinDisplayTime);

            _eventHub?.Get<SceneTransitionEvent>()?.Raise(
                new SSceneTransition(config.SceneName, previousSceneName, SceneTransitionPhase.Completed));

            _progress.Clear();
            _isTransitioning = false;
        }

        private IEnumerator WaitMinDisplay(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void OnGameplaySessionEnd()
        {
            _isTransitioning = false;
        }
    }
}
