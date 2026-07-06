using System.Collections;
using System.Collections.Generic;
using RedDust.Addressables;
using RedDust.Items;
using UnityEngine;

namespace RedDust.GameScene
{
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
            var items = new List<ItemDefSO>();
            var melees = new List<MeleeWeaponSO>();
            var ranged = new List<RangedWeaponSO>();
            int remaining = 3;
            bool done = false;

            _addressables.LoadByLabel<ItemDefSO>(label, r => { items = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<MeleeWeaponSO>(label, r => { melees = r; if (--remaining <= 0) done = true; });
            _addressables.LoadByLabel<RangedWeaponSO>(label, r => { ranged = r; if (--remaining <= 0) done = true; });

            while (!done)
                yield return null;

            int total = items.Count + melees.Count + ranged.Count;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[ItemBootTask] === {total} item assets (Item={items.Count}, MeleeWeapon={melees.Count}, RangedWeapon={ranged.Count}) ===");

            foreach (var i in items)
                sb.AppendLine($"  [Item] {i.name}  prefab={PrefabName(i.Prefab)}  overrides={i.OverridesJson}");
            foreach (var m in melees)
                sb.AppendLine($"  [MeleeWeapon] {m.name}  prefab={PrefabName(m.Prefab)}  overrides={m.OverridesJson}");
            foreach (var r in ranged)
                sb.AppendLine($"  [RangedWeapon] {r.name}  prefab={PrefabName(r.Prefab)}  overrides={r.OverridesJson}");
            Debug.Log(sb.ToString());
        }

        private static string PrefabName(GameObject prefab)
        {
            if (prefab == null) return "NULL";
            return prefab.name;
        }
    }
}
