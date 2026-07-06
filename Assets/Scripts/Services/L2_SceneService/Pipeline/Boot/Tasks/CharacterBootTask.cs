using System.Collections;
using RedDust.Addressables;
using RedDust.Character;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all character definitions so they are pre-cached
    /// before entity spawn.
    /// </summary>
    public class CharacterBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading character definitions...";

        public CharacterBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            bool done = false;
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            _addressables.LoadByLabel<CharacterDefSO>(label, defs =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[CharacterBootTask] === {defs.Count} character definitions ===");
                foreach (var c in defs)
                    sb.AppendLine($"  [Character] {c.name}  prefab={(c.Prefab != null ? c.Prefab.name : "NULL")}  overrides={c.OverridesJson}");
                Debug.Log(sb.ToString());
                done = true;
            });

            while (!done)
                yield return null;
        }
    }
}
