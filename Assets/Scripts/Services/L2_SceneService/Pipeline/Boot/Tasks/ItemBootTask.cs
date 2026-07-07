using System.Collections.Generic;
using RedDust.Core;
using RedDust.Entities;
using RedDust.Items;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: registers all item/weapon presets into ItemRegistry.
    /// </summary>
    public class ItemBootTask : IBootTask
    {
        public string Description => "Registering item presets...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var items = catalog.Get<ItemDefSO>();
            var melees = catalog.Get<MeleeWeaponSO>();
            var ranged = catalog.Get<RangedWeaponSO>();

            var allPresets = new List<PropertyPresetSO>();
            allPresets.AddRange(items);
            allPresets.AddRange(melees);
            allPresets.AddRange(ranged);

            GameService.Instance.AssetRegistry.InitItems(allPresets);

            int total = items.Count + melees.Count + ranged.Count;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[ItemBootTask] === {total} item assets registered (Item={items.Count}, MeleeWeapon={melees.Count}, RangedWeapon={ranged.Count}) ===");
            foreach (var i in items)
                sb.AppendLine($"  [Item] {i.name}  prefab={PrefabName(i.Prefab)}");
            foreach (var m in melees)
                sb.AppendLine($"  [MeleeWeapon] {m.name}  prefab={PrefabName(m.Prefab)}");
            foreach (var r in ranged)
                sb.AppendLine($"  [RangedWeapon] {r.name}  prefab={PrefabName(r.Prefab)}");
            Debug.Log(sb.ToString());
        }

        private static string PrefabName(GameObject prefab)
        {
            if (prefab == null) return "NULL";
            return prefab.name;
        }
    }
}
