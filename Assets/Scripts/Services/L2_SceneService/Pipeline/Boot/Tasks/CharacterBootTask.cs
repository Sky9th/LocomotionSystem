using System.Collections.Generic;
using RedDust.Character;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: registers all CharacterDefSOs into CharacterRegistry.
    /// </summary>
    public class CharacterBootTask : IBootTask
    {
        public string Description => "Registering character definitions...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var defs = catalog.Get<CharacterDefSO>();
            GameService.Instance.AssetRegistry.InitCharacters(defs);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[CharacterBootTask] === {defs.Count} character definitions registered ===");
            foreach (var c in defs)
                sb.AppendLine($"  [Character] {c.name}  prefab={(c.Prefab != null ? c.Prefab.name : "NULL")}");
            Debug.Log(sb.ToString());
        }
    }
}
