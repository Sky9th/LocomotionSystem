using System.Collections.Generic;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: extracts all PropertyTreeSOs from the catalog and
    /// registers them into GameRegistry.
    /// </summary>
    public class PropertyTreeBootTask : IBootTask
    {
        public string Description => "Registering property trees...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var trees = catalog.Get<PropertyTreeSO>();
            GameService.Instance.AssetRegistry.InitPropertyTrees(trees);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[PropertyTreeBootTask] === {trees.Count} PropertyTreeSOs registered ===");
            foreach (var t in trees)
                sb.AppendLine($"  {t.name}  inheritsFrom={(t.InheritsFrom != null ? t.InheritsFrom.name : "(root)")}");
            Debug.Log(sb.ToString());

            // Full self-test via registry
            var humanTree = GameService.Instance.AssetRegistry.FindPropertyTree("Human");
            if (humanTree != null)
            {
                var missing = new List<string>();
                var resolved = humanTree.ResolveStructure();
                foreach (var kv in resolved)
                    if (kv.Value == null) missing.Add(kv.Key);
                if (missing.Count == 0)
                    Debug.Log($"[PropertyTreeBootTask] Full self-test PASSED — Human tree: {resolved.Count} paths, 0 missing defs.");
                else
                    Debug.LogError($"[PropertyTreeBootTask] Full self-test FAILED — Human tree missing {missing.Count} defs: [{string.Join(", ", missing)}]");
            }
            else
                Debug.LogError("[PropertyTreeBootTask] Full self-test FAILED — 'Human' PropertyTreeSO not found in registry.");
        }
    }
}
