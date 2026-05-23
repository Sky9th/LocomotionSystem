/// <summary>
/// Scene status pushed to GameContext. Poll for current scene identity,
/// subscribe to SSceneLoadComplete for transition timing.
/// </summary>
public readonly struct SSceneTransition
{
    public readonly string CurrentScene;
    public readonly string PreviousScene;
    public readonly bool IsLoading;

    public SSceneTransition(string currentScene, string previousScene, bool isLoading)
    {
        CurrentScene = currentScene;
        PreviousScene = previousScene;
        IsLoading = isLoading;
    }
}
