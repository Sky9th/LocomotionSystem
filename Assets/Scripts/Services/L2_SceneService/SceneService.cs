using System.Collections;
using RedDust.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedDust.GameScene
{
    public class SceneService : ModuleComponent
    {
        [SerializeField, Min(0.1f)] private float minLoadingDisplayTime = 0.5f;

        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
        private string currentContentScene;

        public string CurrentContentScene => currentContentScene;

        public void SetCurrentContentScene(string sceneName)
        {
            currentContentScene = sceneName;
        }

        public override void OnAssemble()
        {
        }

        public override void OnWire()
        {
            GameContext.Instance.RegisterService(this);
            GameContext.Instance.TryResolveService(out _dispatcher);
            _dispatcher.Subscribe<SLoadSceneRequest>(HandleLoadSceneRequest);
            _dispatcher.Subscribe<SReloadSceneRequest>(HandleReloadSceneRequest);
            _dispatcher.Subscribe<SUnloadSceneRequest>(HandleUnloadSceneRequest);

            GameService.Instance?.NotifyServiceWired();
        }

        private void PublishSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
        {
            GameContext.Instance.UpdateSnapshot(snapshot);
            _dispatcher.Publish(snapshot);
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
            {
                _dispatcher.Unsubscribe<SLoadSceneRequest>(HandleLoadSceneRequest);
                _dispatcher.Unsubscribe<SReloadSceneRequest>(HandleReloadSceneRequest);
                _dispatcher.Unsubscribe<SUnloadSceneRequest>(HandleUnloadSceneRequest);
            }
        }

        private void HandleLoadSceneRequest(SLoadSceneRequest request, MetaStruct meta)
        {
            StartCoroutine(LoadContentScene(request.SceneName));
        }

        private void HandleReloadSceneRequest(SReloadSceneRequest request, MetaStruct meta)
        {
            StartCoroutine(ReloadContentScene(request.SceneName));
        }

        private void HandleUnloadSceneRequest(SUnloadSceneRequest request, MetaStruct meta)
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
                _dispatcher.Publish(new SSceneLoadComplete(sceneName, previousScene));
                yield break;
            }

            _dispatcher.Publish(new SSceneLoadStart(sceneName));

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
            _dispatcher.Publish(new SSceneLoadComplete(sceneName, previousScene));
        }

        private IEnumerator ReloadContentScene(string sceneName)
        {
            var previousScene = currentContentScene;

            _dispatcher.Publish(new SSceneLoadStart(sceneName));

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
            _dispatcher.Publish(new SSceneLoadComplete(sceneName, previousScene));
        }

        private IEnumerator UnloadContentScene(string sceneName)
        {
            _dispatcher.Publish(new SSceneLoadStart(sceneName));

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
            _dispatcher.Publish(new SSceneLoadComplete(null, sceneName));
        }


    }
}
