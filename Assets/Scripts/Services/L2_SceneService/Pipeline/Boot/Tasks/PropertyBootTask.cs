using System.Collections;
using System.Linq;
using RedDust.Addressables;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all PropertyDefSO assets and initializes PropertyDefinitionRegistry.
    /// </summary>
    public class PropertyBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading property definitions...";

        public PropertyBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            bool done = false;
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            _addressables.LoadByLabel<PropertyDefSO>(label, defs =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[PropertyBootTask] === {defs.Count} PropertyDefSOs ===");
                foreach (var d in defs)
                    sb.AppendLine($"  {d.name}  Type={d.Type}  Id={d.Id}");
                Debug.Log(sb.ToString());
                PropertyDefinitionRegistry.Initialize(defs);
                done = true;
            });

            while (!done)
                yield return null;
        }
    }
}
