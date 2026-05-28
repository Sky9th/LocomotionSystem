namespace RedDust.SceneService
{
    public readonly struct SLoadSceneRequest
    {
        public readonly string SceneName;

        public SLoadSceneRequest(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
