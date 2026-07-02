namespace RedDust.GameScene
{
    /// <summary>
    /// Published via SceneLoadCompleteEvent when a scene transition finishes.
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
