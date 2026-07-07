using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Core;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: registers AbilityTreeSOs into AbilityTreeRegistry.
    /// Other ability types (Active, Passive, Activation, Search, Effect, Noise)
    /// are just pre-loaded by the catalog — they're referenced by trees.
    /// </summary>
    public class AbilityBootTask : IBootTask
    {
        public string Description => "Registering ability trees...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var trees = catalog.Get<AbilityTreeSO>();
            GameService.Instance.AssetRegistry.InitAbilityTrees(trees);

            int actives = catalog.Get<ActiveAbilitySO>().Count;
            int passives = catalog.Get<PassiveAbilitySO>().Count;
            int activations = catalog.Get<AbilityActivationSO>().Count;
            int searches = catalog.Get<AbilitySearchSO>().Count;
            int effects = catalog.Get<EffectSO>().Count;
            int noises = catalog.Get<NoiseEventSO>().Count;

            int total = trees.Count + actives + passives + activations + searches + effects + noises;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[AbilityBootTask] === {total} ability assets (Tree={trees.Count}, Active={actives}, Passive={passives}, Activation={activations}, Search={searches}, Effect={effects}, Noise={noises}) ===");

            foreach (var t in trees)
            {
                sb.AppendLine($"  [Tree] {t.treeId}  nodes={t.nodes?.Length ?? 0}");
                if (t.nodes != null)
                    foreach (var node in t.nodes)
                        sb.AppendLine($"    node={node.nodeId}  ability={SoName(node.ability)}  passive={SoName(node.passive)}");
            }
            Debug.Log(sb.ToString());
        }

        private static string SoName(ScriptableObject so, string fallback = "(none)")
        {
            if (so == null) return fallback;
            return so.name;
        }
    }
}
