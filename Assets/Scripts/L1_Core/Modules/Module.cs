using UnityEngine;

namespace RedDust.Core
{
    public abstract class Module : IInitializable
    {
        protected Module(ModuleRegistry parent)
        {
            parent.Register(this);
        }

        public virtual void OnAssemble() { }
        public virtual void OnWire() { }
    }
}
