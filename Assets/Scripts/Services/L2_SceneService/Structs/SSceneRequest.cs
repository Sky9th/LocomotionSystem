namespace RedDust.GameScene
{
    public enum SceneRequestType { Load, Reload, Unload }

    public readonly struct SSceneRequest
    {
        public readonly string SceneName;
        public readonly SceneRequestType Type;

        public SSceneRequest(string sceneName, SceneRequestType type = SceneRequestType.Load)
        {
            SceneName = sceneName;
            Type = type;
        }
    }
}
