using RedDust.Core.Modules;
using UnityEngine;

namespace RedDust.Core.Modules
{
    public abstract class ModuleChild : IModuleChild
    {
        protected ModuleChild(ModuleRegistry parent)
        {
            parent.Register(this);
        }

        public virtual void OnAssemble() { }
        public virtual void OnWire() { }
    }
}
