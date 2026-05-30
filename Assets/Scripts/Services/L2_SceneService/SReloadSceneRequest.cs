namespace RedDust.GameScene
{
    public readonly struct SReloadSceneRequest
    {
        public readonly string SceneName;

        public SReloadSceneRequest(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
