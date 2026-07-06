using RedDust.Addressables;

namespace RedDust.GameScene
{
    public readonly struct SRuntimeSceneState
    {
        public readonly string SceneName;
        public readonly string ScenePath;
        public readonly SceneAssetLabel AssetLabels;

        public SRuntimeSceneState(string sceneName, string scenePath, SceneAssetLabel assetLabels)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            AssetLabels = assetLabels;
        }
    }
}
