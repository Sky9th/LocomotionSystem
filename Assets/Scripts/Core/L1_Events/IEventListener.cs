namespace RedDust.Core
{
    /// <summary>
    /// 事件监听者接口。任何需要订阅 SO Event Channel 的模块实现此接口，
    /// 由宿主在适当时机调用。
    /// </summary>
    public interface IEventListener
    {
        void BindEvents();
        void UnbindEvents();
    }
}
