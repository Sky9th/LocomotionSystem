using System.Collections.Generic;

namespace RedDust.Core
{
    public sealed class ModuleRegistry
    {
        readonly List<IInitializable> _modules = new();

        public int Count => _modules.Count;

        internal void Register(IInitializable module)
        {
            if (!_modules.Contains(module))
                _modules.Add(module);
        }

        public void OnAssembleAll()
        {
            foreach (var m in _modules) m.OnAssemble();
        }

        public void OnWireAll()
        {
            foreach (var m in _modules) m.OnWire();
        }
    }
}
