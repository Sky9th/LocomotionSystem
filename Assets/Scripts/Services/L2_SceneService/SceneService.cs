using System.Collections;
using RedDust.Core;
using RedDust.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedDust.GameScene
{
    public class SceneService : ModuleChildMono
    {
        [SerializeField, Min(0.1f)] private float minLoadingDisplayTime = 0.5f;

        private EventHub _eventHub;
        private string currentContentScene;

        public string CurrentContentScene => currentContentScene;

        public void SetCurrentContentScene(string sceneName)
        {
            currentContentScene = sceneName;
        }

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            _eventHub.Get<SceneLoadRequestEvent>().Register(HandleLoadSceneRequest);
            _eventHub.Get<SceneReloadRequestEvent>().Register(HandleReloadSceneRequest);
            _eventHub.Get<SceneUnloadRequestEvent>().Register(HandleUnloadSceneRequest);
        }

        private void PublishSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
        {
            GameContext.Instance.UpdateSnapshot(snapshot);
        }

        private void OnDestroy()
        {
            if (_eventHub != null)
            {
                _eventHub.Get<SceneLoadRequestEvent>().Unregister(HandleLoadSceneRequest);
                _eventHub.Get<SceneReloadRequestEvent>().Unregister(HandleReloadSceneRequest);
                _eventHub.Get<SceneUnloadRequestEvent>().Unregister(HandleUnloadSceneRequest);
            }
        }

        private void HandleLoadSceneRequest(SLoadSceneRequest request)
        {
            StartCoroutine(LoadContentScene(request.SceneName));
        }

        private void HandleReloadSceneRequest(SReloadSceneRequest request)
        {
            StartCoroutine(ReloadContentScene(request.SceneName));
        }

        private void HandleUnloadSceneRequest(SUnloadSceneRequest request)
        {
            var sceneName = string.IsNullOrEmpty(request.SceneName) ? currentContentScene : request.SceneName;
            if (string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(UnloadContentScene(sceneName));
        }

        private IEnumerator LoadContentScene(string sceneName)
        {
            var previousScene = currentContentScene;

            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                currentContentScene = sceneName;
                _eventHub.Get<SceneLoadCompleteEvent>().Raise(new SSceneLoadComplete(sceneName, previousScene));
                yield break;
            }

            _eventHub.Get<SceneLoadStartEvent>().Raise(new SSceneLoadStart(sceneName));

            PublishSnapshot(new SSceneTransition(sceneName, previousScene, true));

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone)
                yield return null;

            if (!string.IsNullOrEmpty(previousScene))
            {
                var oldScene = SceneManager.GetSceneByName(previousScene);
                if (oldScene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(oldScene);
            }
            currentContentScene = sceneName;

            var elapsed = 0f;
            while (elapsed < minLoadingDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            PublishSnapshot(new SSceneTransition(sceneName, previousScene, false));
            _eventHub.Get<SceneLoadCompleteEvent>().Raise(new SSceneLoadComplete(sceneName, previousScene));
        }

        private IEnumerator ReloadContentScene(string sceneName)
        {
            var previousScene = currentContentScene;

            _eventHub.Get<SceneLoadStartEvent>().Raise(new SSceneLoadStart(sceneName));

            PublishSnapshot(new SSceneTransition(sceneName, previousScene, true));

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone)
                yield return null;

            if (!string.IsNullOrEmpty(previousScene))
            {
                var oldScene = SceneManager.GetSceneByName(previousScene);
                if (oldScene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(oldScene);
            }
            currentContentScene = sceneName;

            var elapsed = 0f;
            while (elapsed < minLoadingDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            PublishSnapshot(new SSceneTransition(sceneName, previousScene, false));
            _eventHub.Get<SceneLoadCompleteEvent>().Raise(new SSceneLoadComplete(sceneName, previousScene));
        }

        private IEnumerator UnloadContentScene(string sceneName)
        {
            _eventHub.Get<SceneLoadStartEvent>().Raise(new SSceneLoadStart(sceneName));

            PublishSnapshot(new SSceneTransition(null, sceneName, true));

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);

            currentContentScene = null;

            var elapsed = 0f;
            while (elapsed < minLoadingDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            PublishSnapshot(new SSceneTransition(null, sceneName, false));
            _eventHub.Get<SceneLoadCompleteEvent>().Raise(new SSceneLoadComplete(null, sceneName));
        }


    }
}
