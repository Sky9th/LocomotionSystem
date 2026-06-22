namespace RedDust.Core
{
    /// <summary>
    /// 模块树子节点协议。OnAssemble 在 Hub.Awake 末尾调用（构造子模块、收集引用），
    /// OnWire 在 Hub.Start 中调用（跨模块连线、事件订阅）。
    /// ModuleHub 自身不实现此接口——只有 ModuleChildMono 和 ModuleChild 实现。
    /// </summary>
    public interface IModuleChild
    {
        /// <summary>收集引用、创建孙子模块。Hub.Awake 末尾由 OnAssembleAll 遍历快照调用。</summary>
        void OnAssemble();

        /// <summary>跨模块连线、事件订阅。Hub.Start 中由 OnWireAll 调用。</summary>
        void OnWire();
    }
}
