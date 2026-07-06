using System.Collections;
using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Addressables;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all ability-related SOs (Active, Passive, Tree, Activation,
    /// Search, Effect, Noise) so they are pre-cached and don't cause per-entity
    /// dependency resolution during gameplay.
    /// </summary>
    public class AbilityBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading ability definitions...";

        public AbilityBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            var actives = new List<ActiveAbilitySO>();
            var passives = new List<PassiveAbilitySO>();
            var trees = new List<AbilityTreeSO>();
            var activations = new List<AbilityActivationSO>();
            var searches = new List<AbilitySearchSO>();
            var effects = new List<EffectSO>();
            var noises = new List<NoiseEventSO>();
            int remaining = 7;
            bool done = false;

            _addressables.LoadByLabel<ActiveAbilitySO>(label, r => { actives = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<PassiveAbilitySO>(label, r => { passives = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilityTreeSO>(label, r => { trees = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilityActivationSO>(label, r => { activations = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilitySearchSO>(label, r => { searches = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<EffectSO>(label, r => { effects = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<NoiseEventSO>(label, r => { noises = r; if (--remaining <= 0) done = true; });

            while (!done)
                yield return null;

            int total = actives.Count + passives.Count + trees.Count + activations.Count + searches.Count + effects.Count + noises.Count;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[AbilityBootTask] === {total} ability assets (Active={actives.Count}, Passive={passives.Count}, Tree={trees.Count}, Activation={activations.Count}, Search={searches.Count}, Effect={effects.Count}, Noise={noises.Count}) ===");

            foreach (var a in activations)
                sb.AppendLine($"  [Activation] {a.name}");
            foreach (var s in searches)
                sb.AppendLine($"  [Search] {s.name}");
            foreach (var e in effects)
                sb.AppendLine($"  [Effect] {e.name}  type={e.GetType().Name}");
            foreach (var n in noises)
                sb.AppendLine($"  [Noise] {n.name}");
            foreach (var p in passives)
                sb.AppendLine($"  [Passive] {p.name}  tag={(p.abilityTag != null ? p.abilityTag.FullTag : "NULL")}  cd={p.cooldownDuration}s");
            foreach (var a in actives)
                sb.AppendLine($"  [Active] {a.name}  tag={(a.abilityTag != null ? a.abilityTag.FullTag : "NULL")}  activation={SoName(a.activation)}  search={SoName(a.search)}  noise={SoName(a.noise)}  cd={a.cooldownDuration}s  targetFx={a.targetEffects?.Length ?? 0}  selfFx={a.selfEffects?.Length ?? 0}");
            foreach (var t in trees)
            {
                sb.AppendLine($"  [Tree] {t.treeId}  nodes={t.nodes?.Length ?? 0}  weaponTags=[{TagNames(t.compatibleWeaponTags)}]  gripTags=[{TagNames(t.compatibleGripTags)}]");
                if (t.nodes != null)
                    foreach (var node in t.nodes)
                        sb.AppendLine($"    node={node.nodeId}  ability={SoName(node.ability, "(none)")}  passive={SoName(node.passive, "(none)")}");
            }
            Debug.Log(sb.ToString());
        }

        private static string SoName(ScriptableObject so, string fallback = "NULL")
        {
            if (so == null) return fallback;
            return so.name;
        }

        private static string TagNames(Core.RdTagDefSO[] tags)
        {
            if (tags == null || tags.Length == 0) return "";
            return string.Join(", ", System.Array.ConvertAll(tags, t => t != null ? t.FullTag : "NULL"));
        }
    }
}
