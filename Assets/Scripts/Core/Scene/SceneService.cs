using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneService : BaseService
{
    [SerializeField, Min(0.1f)] private float minLoadingDisplayTime = 0.5f;

    private string currentContentScene;

    public string CurrentContentScene => currentContentScene;

    public void SetCurrentContentScene(string sceneName)
    {
        currentContentScene = sceneName;
    }

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        return true;
    }

    protected override void OnSubscriptionsActivated()
    {
        Dispatcher.Subscribe<SLoadSceneRequest>(HandleLoadSceneRequest);
        Dispatcher.Subscribe<SUnloadSceneRequest>(HandleUnloadSceneRequest);
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
        {
            Dispatcher.Unsubscribe<SLoadSceneRequest>(HandleLoadSceneRequest);
            Dispatcher.Unsubscribe<SUnloadSceneRequest>(HandleUnloadSceneRequest);
        }
    }

    private void HandleLoadSceneRequest(SLoadSceneRequest request, MetaStruct meta)
    {
        StartCoroutine(LoadContentScene(request.SceneName));
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

        Dispatcher.Publish(new SSceneLoadStart(sceneName));

        PushSnapshot(sceneName, previousScene, true);

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

        PushSnapshot(sceneName, previousScene, false);
        Dispatcher.Publish(new SSceneLoadComplete(sceneName, previousScene));
    }

    private IEnumerator UnloadContentScene(string sceneName)
    {
        Dispatcher.Publish(new SSceneLoadStart(sceneName));

        PushSnapshot(null, sceneName, true);

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

        PushSnapshot(null, sceneName, false);
        Dispatcher.Publish(new SSceneLoadComplete(null, sceneName));
    }

    private void PushSnapshot(string current, string previous, bool isLoading)
    {
        GameContext?.UpdateSnapshot(new SSceneTransition(current, previous, isLoading));
    }

    protected override void OnServicesReady() { }

    protected override void OnDispatcherAttached() { }
}
