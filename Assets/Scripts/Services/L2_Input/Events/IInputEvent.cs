using RedDust.GameState;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入事件生命周期。供 InputService 统一管理。
    /// </summary>
    public interface IInputEvent
    {
        void InitializeEvent();
        void EnableEvent();
        void DisableEvent();
        void DisposeEvent();
        bool SupportsState(EGameState state);
    }
}
