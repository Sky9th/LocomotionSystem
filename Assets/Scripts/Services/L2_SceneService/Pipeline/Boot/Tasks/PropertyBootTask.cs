using System.Collections.Generic;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: extracts all PropertyDefSOs from the catalog and
    /// registers them into GameRegistry.
    /// </summary>
    public class PropertyBootTask : IBootTask
    {
        public string Description => "Registering property definitions...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var defs = catalog.Get<PropertyDefSO>();

            // Also catch any that FindObjectsOfTypeAll finds but catalog missed
            var allInMemory = Resources.FindObjectsOfTypeAll<PropertyDefSO>();
            var merged = new HashSet<PropertyDefSO>(defs);
            int catalogCount = defs.Count;
            foreach (var d in allInMemory)
                merged.Add(d);
            defs = new List<PropertyDefSO>(merged);
            int missed = defs.Count - catalogCount;

            GameService.Instance.AssetRegistry.InitPropertyDefs(defs);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[PropertyBootTask] === {defs.Count} PropertyDefSOs registered (catalog={catalogCount}, missed={missed}) ===");
            foreach (var d in defs)
                sb.AppendLine($"  {d.name}  Type={d.Type}  Id={d.Id}");
            Debug.Log(sb.ToString());

            var testDef = GameService.Instance.AssetRegistry.FindPropertyDef("HP");
            if (testDef != null)
                Debug.Log($"[PropertyBootTask] Self-test PASSED — 'HP' found: Type={testDef.Type}");
            else
                Debug.LogError("[PropertyBootTask] Self-test FAILED — 'HP' not found!");
        }
    }
}
