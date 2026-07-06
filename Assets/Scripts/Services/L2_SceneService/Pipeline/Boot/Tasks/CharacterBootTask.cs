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
                Debug.Log($"[CharacterBootTask] Loaded {defs.Count} character definitions.");
                done = true;
            });

            while (!done)
                yield return null;
        }
    }
}
