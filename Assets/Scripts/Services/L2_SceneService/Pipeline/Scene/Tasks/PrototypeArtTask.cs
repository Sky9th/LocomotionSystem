using System.Collections;
using RedDust.Addressables;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Scene-layer example: loads PolygonPrototype art assets.
    /// Assets under Assets/Art/PolygonPrototype/ tagged with "prototype-art"
    /// are loaded/unloaded with the prototype scene via TransitionGate.
    ///
    /// Future biome tasks (ForestChunkTask, CityChunkTask, etc.) follow the same pattern:
    /// one Task per label group, loaded by TransitionGate during scene transitions.
    /// </summary>
    public class PrototypeArtTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading prototype art assets...";

        public PrototypeArtTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            bool done = false;
            var label = SceneAssetLabel.PrototypeArt.ToLabelStrings()[0];
            _addressables.LoadByLabel<GameObject>(label, results =>
            {
                Debug.Log($"[PrototypeArtTask] Loaded {results.Count} prototype art assets.");
                done = true;
            });

            while (!done)
                yield return null;
        }
    }
}
