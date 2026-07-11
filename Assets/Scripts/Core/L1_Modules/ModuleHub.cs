using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// Module 树父节点。不实现 IModuleChild（需要既父又子的 Hub 显式添加接口）。
    ///
    /// Hub 子类生命周期模式：
    ///   Awake:  [pre-assemble: 创建 C# 子模块、添加 MB 子组件] → base.Awake()
    ///   Start:  [pre-wire: 构建子模块依赖的共享资源] → base.Start() → [post-wire: Publish 初始状态]
    /// </summary>
    public abstract class ModuleHub : MonoBehaviour
    {
        private ModuleRegistry _registry;
        internal ModuleRegistry Registry => _registry ??= new ModuleRegistry();

        /// <summary>
        /// 扫描 ModuleChildMono 子节点 → 判主 → Register → OnAssembleAll。
        /// 子类在 base.Awake() 之前创建 C# 子模块和添加 MB 子组件。
        /// </summary>
        protected virtual void Awake()
        {
            foreach (var child in GetComponentsInChildren<ModuleChildMono>(includeInactive: true))
            {
                var owner = child.GetComponentInParent<ModuleHub>(includeInactive: true);
                if (owner == this)
                    Registry.Register(child);
            }
            Registry.OnAssembleAll();
        }

        /// <summary>
        /// OnWireAll → 子模块的 OnWire（事件订阅已在此时全部就位）。
        /// 子类在 base.Start() 之后可以 Publish 初始状态。
        /// </summary>
        protected virtual void Start()
        {
            Registry.OnWireAll();
        }
    }
}
