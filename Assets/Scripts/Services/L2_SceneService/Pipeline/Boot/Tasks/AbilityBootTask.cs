using System.Collections;
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
            int total = 0;
            int remaining = 7;
            bool done = false;

            _addressables.LoadByLabel<ActiveAbilitySO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<PassiveAbilitySO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilityTreeSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilityActivationSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<AbilitySearchSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<EffectSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<NoiseEventSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });

            while (!done)
                yield return null;

            Debug.Log($"[AbilityBootTask] Loaded {total} ability assets.");
        }
    }
}
