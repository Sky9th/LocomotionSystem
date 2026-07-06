using System.Collections;
using RedDust.Addressables;
using RedDust.Items;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all item definitions so they are pre-cached
    /// and ready for entity spawn without per-item dependency resolution.
    /// </summary>
    public class ItemBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading item definitions...";

        public ItemBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            int total = 0;
            int remaining = 3;
            bool done = false;

            _addressables.LoadByLabel<ItemDefSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<MeleeWeaponSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<RangedWeaponSO>(label, r => { total += r.Count; if (--remaining <= 0) done = true; });

            while (!done)
                yield return null;

            Debug.Log($"[ItemBootTask] Loaded {total} item assets.");
        }
    }
}
