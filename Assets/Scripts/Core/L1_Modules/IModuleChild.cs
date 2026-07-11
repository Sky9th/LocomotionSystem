using RedDust.Core.Modules;
namespace RedDust.Core.Modules
{
    /// <summary>
    /// 模块树子节点协议。每个阶段的职责边界：
    ///
    /// OnAssemble — 组装阶段
    ///   收集自身引用（GetComponent）、创建孙子模块、注册到全局容器。
    ///   禁止：解析其他模块（TryResolveService）、订阅事件、执行业务逻辑。
    ///
    /// OnWire — 连线阶段
    ///   解析其他模块、订阅事件。所有连线就位后才允许 Publish。
    ///   禁止：Publish 初始状态（放 Start）、执行业务逻辑。
    ///
    /// OnEnable — 激活阶段
    ///   启用自身运行态。禁止：收集引用、订阅事件。
    ///
    /// OnDisable — 休眠阶段
    ///   重置自身运行时状态。禁止：取消事件订阅（放 OnDestroy）。
    ///
    /// OnDestroy — 销毁阶段
    ///   取消所有事件订阅、释放资源。
    ///
    /// 嵌套 Hub：ModuleHub 自身不实现此接口。需要既父又子的 Hub
    /// （如 AnimationBrain）显式 : ModuleHub, IModuleChild，Awake 中自注册。
    /// </summary>
    public interface IModuleChild
    {
        /// <summary>收集自身引用、创建孙子模块。Hub.Awake 末尾由 OnAssembleAll 遍历快照调用。</summary>
        void OnAssemble();

        /// <summary>解析其他模块、订阅事件。Hub.Start 中由 OnWireAll 调用。</summary>
        void OnWire();
    }
}
