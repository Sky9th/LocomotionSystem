using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// MB 子模块基类。OnAssemble 中自动向上查找父 ModuleBehaviour 注册。
    /// </summary>
    public abstract class ModuleComponent : MonoBehaviour, IInitializable
    {
        private bool _registered;

        public virtual void OnAssemble()
        {
            if (!_registered)
            {
                GetComponentInParent<ModuleBehaviour>()?.Registry?.Register(this);
                _registered = true;
            }
        }

        public virtual void OnWire() { }
    }
}
