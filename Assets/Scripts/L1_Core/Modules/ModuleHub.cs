using UnityEngine;

namespace RedDust.Core
{
    public abstract class ModuleHub : MonoBehaviour
    {
        private ModuleRegistry _registry;
        internal ModuleRegistry Registry => _registry ??= new ModuleRegistry();

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

        protected virtual void Start()
        {
            Registry.OnWireAll();
        }
    }
}
