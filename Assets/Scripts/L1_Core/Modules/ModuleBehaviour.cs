using UnityEngine;

namespace RedDust.Core
{
    public abstract class ModuleBehaviour : MonoBehaviour, IInitializable
    {
        internal ModuleRegistry Registry { get; private set; }

        protected virtual void Awake()
        {
            Registry = new ModuleRegistry();

            foreach (var m in GetComponentsInChildren<IInitializable>())
            {
                if (m is ModuleBehaviour) continue;
                Registry.Register(m);
            }

            OnAssemble();
            Registry.OnAssembleAll();
        }

        protected virtual void Start() => OnWire();

        public virtual void OnAssemble() { }

        public virtual void OnWire()
        {
            Registry.OnWireAll();
        }
    }
}
