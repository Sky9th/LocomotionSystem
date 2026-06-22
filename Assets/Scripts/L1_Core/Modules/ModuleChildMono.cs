using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// MB 子模块基类。不自注册——由父 ModuleHub.Awake 通过 GetComponentsInChildren 扫描发现并注册。
    ///
    /// 生命周期职责边界（详见 IModuleChild）：
    ///   OnAssemble — 收集自身引用、创建孙子。不碰其他模块。
    ///   OnWire     — 解析其他模块、订阅事件。不 Publish。
    ///   OnEnable   — 启用自身运行态。不解引用、不订阅。
    ///   OnDisable  — 重置运行时状态。不取消订阅。
    ///   OnDestroy  — 取消所有事件订阅、释放资源。
    /// </summary>
    public abstract class ModuleChildMono : MonoBehaviour, IModuleChild
    {
        /// <summary>
        /// 基类 Awake 为空。子模块初始化放在 OnAssemble（由父 Hub.Awake 末尾的 OnAssembleAll 驱动）。
        /// </summary>
        protected virtual void Awake() { }

        public virtual void OnAssemble()
        {
        }

        public virtual void OnWire() { }
    }
}
