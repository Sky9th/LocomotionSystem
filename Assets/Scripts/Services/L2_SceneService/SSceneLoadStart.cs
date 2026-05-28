namespace RedDust.SceneService
{
    public readonly struct SSceneLoadStart
    {
        public readonly string SceneName;

        public SSceneLoadStart(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
