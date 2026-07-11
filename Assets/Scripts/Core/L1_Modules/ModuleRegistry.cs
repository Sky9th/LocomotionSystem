using RedDust.Core.Modules;
using System.Collections.Generic;

namespace RedDust.Core.Modules
{
    public sealed class ModuleRegistry
    {
        readonly List<IModuleChild> _modules = new();

        public int Count => _modules.Count;

        internal void Register(IModuleChild module)
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
