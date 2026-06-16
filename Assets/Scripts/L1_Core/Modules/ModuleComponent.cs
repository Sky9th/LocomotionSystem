using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// MB 子模块基类。OnAssemble 中自动向上查找父 ModuleBehaviour 注册。
    /// </summary>
    public abstract class ModuleComponent : MonoBehaviour, IInitializable
    {
        private bool _registered;

        /// <summary>
        /// 动态添加的组件在 Awake 中自注册到父 ModuleBehaviour 的 Registry。
        /// Unity 保证 Awake 深度优先，父 Registry 已就位。
        /// </summary>
        protected virtual void Awake()
        {
            if (_registered) return;
            GetComponentInParent<ModuleBehaviour>()?.Registry?.Register(this);
            _registered = true;
        }

        public virtual void OnAssemble()
        {
        }

        public virtual void OnWire() { }
    }
}
