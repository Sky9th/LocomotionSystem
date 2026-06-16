namespace RedDust.Core
{
    /// <summary>
    /// 递归树初始化协议 —— Unity Awake/Start 无序调用的补充。
    /// OnAssemble 在 Awake 末尾调用，构造子模块、收集引用。
    /// OnWire 在 Start 里调用，子模块跨同级连线。
    /// 与 IGameplaySessionHandler（会话级）正交，按需组合。
    /// </summary>
    public interface IInitializable
    {
        /// <summary>递归构建子树。构造子模块 → 触发子模块递归 → 收集引用 → 返回时子树完整。</summary>
        void OnAssemble();

        /// <summary>跨同级连线。通知子模块递归 → 跨模块绑定 → 事件订阅 → 返回时全树可运转。</summary>
        void OnWire();
    }
}
