using UnityEngine;

namespace RedDust.Core
{
    public abstract class ModuleBehaviour : MonoBehaviour, IInitializable
    {
        private ModuleRegistry _registry;
        internal ModuleRegistry Registry => _registry ??= new ModuleRegistry();

        protected virtual void Awake()
        {
            OnAssemble();
        }

        protected virtual void Start()
        {
            Registry.OnAssembleAll();
            OnWire();
        }

        public virtual void OnAssemble() {}

        public virtual void OnWire()
        {
            Registry.OnWireAll();
        }
    }
}
