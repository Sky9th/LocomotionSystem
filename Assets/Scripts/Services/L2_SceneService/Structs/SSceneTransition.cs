namespace RedDust.GameScene
{
    public enum SceneTransitionPhase { Started, Completed }

    /// <summary>
    /// Published via SceneTransitionEvent. Started = loading screen up,
    /// Completed = scene loaded and screen down.
    /// </summary>
    public readonly struct SSceneTransition
    {
        public readonly string SceneName;
        public readonly string PreviousScene;
        public readonly SceneTransitionPhase Phase;

        public SSceneTransition(string sceneName, string previousScene, SceneTransitionPhase phase)
        {
            SceneName = sceneName;
            PreviousScene = previousScene;
            Phase = phase;
        }
    }
}
