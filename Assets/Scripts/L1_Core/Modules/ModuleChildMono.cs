using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// MB 子模块基类。不自注册——由父 ModuleHub.Awake 通过 GetComponentsInChildren 扫描发现并注册。
    /// Awake 无基类逻辑，留给子类做仅依赖自身序列化字段的 setup。
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
