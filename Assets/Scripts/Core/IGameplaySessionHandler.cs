namespace RedDust.Core
{
    /// <summary>
    /// Implement on a BaseService that owns state scoped to one gameplay session.
    /// GameService calls this when the session ends (return to MainMenu, etc.).
    /// </summary>
    public interface IGameplaySessionHandler
    {
        void OnGameplaySessionEnd();
    }
}
