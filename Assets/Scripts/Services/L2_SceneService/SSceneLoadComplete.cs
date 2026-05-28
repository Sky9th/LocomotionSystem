namespace RedDust.SceneService
{
    /// <summary>
    /// Published via EventDispatcher when a scene transition finishes.
    /// Subscribe in OnSubscriptionsActivated to react immediately.
    /// </summary>
    public readonly struct SSceneLoadComplete
    {
        public readonly string SceneName;
        public readonly string PreviousScene;

        public SSceneLoadComplete(string sceneName, string previousScene)
        {
            SceneName = sceneName;
            PreviousScene = previousScene;
        }
    }
}
