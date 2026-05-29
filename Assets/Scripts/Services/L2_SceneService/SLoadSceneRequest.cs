namespace RedDust.GameScene
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
