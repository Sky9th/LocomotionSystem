using System.Collections;
using RedDust.Addressables;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all PropertyDefSO assets via Addressables label "boot"
    /// and populates PropertyDefinitionRegistry before the first scene activates.
    /// </summary>
    public class PropertyDefBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading property definitions...";

        public PropertyDefBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            bool done = false;
            var bootLabel = SceneAssetLabel.Boot.ToLabelStrings()[0];
            _addressables.LoadByLabel<PropertyDefSO>(bootLabel, defs =>
            {
                Debug.Log($"[PropertyDefBootTask] Loaded {defs.Count} PropertyDefSOs from label '{bootLabel}'.");
                PropertyDefinitionRegistry.Initialize(defs);
                done = true;
            });

            while (!done)
                yield return null;
        }
    }
}
