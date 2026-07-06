using System.Collections;
using System.Collections.Generic;
using RedDust.Core;
using RedDust.Addressables;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: loads all RdTagDefSO assets and rebuilds FullTag caches
    /// in root-first order, fixing Build-side OnEnable ordering issues.
    /// </summary>
    public class TagBootTask : IBootTask
    {
        private readonly AddressablesService _addressables;

        public string Description => "Loading tag definitions...";

        public TagBootTask(AddressablesService addressables)
        {
            _addressables = addressables;
        }

        public IEnumerator Execute()
        {
            bool done = false;
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            _addressables.LoadByLabel<RdTagDefSO>(label, tags =>
            {
                Debug.Log($"[TagBootTask] Loaded {tags.Count} RdTagDefSOs.");
                RebuildAllCaches(tags);
                done = true;
            });

            while (!done)
                yield return null;
        }

        /// <summary>
        /// BFS from roots (parent == null) down to leaves.
        /// Guarantees parent.FullTag is valid before child rebuilds.
        /// </summary>
        private static void RebuildAllCaches(List<RdTagDefSO> allTags)
        {
            var refreshed = new HashSet<RdTagDefSO>();
            var queue = new Queue<RdTagDefSO>();

            foreach (var tag in allTags)
            {
                if (tag.Parent == null)
                {
                    tag.RefreshCache();
                    refreshed.Add(tag);
                    queue.Enqueue(tag);
                }
            }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                foreach (var tag in allTags)
                {
                    if (tag.Parent == parent && refreshed.Add(tag))
                    {
                        tag.RefreshCache();
                        queue.Enqueue(tag);
                    }
                }
            }

            foreach (var tag in allTags)
            {
                if (refreshed.Add(tag))
                {
                    Debug.LogWarning($"[TagBootTask] Tag '{tag.name}' has broken parent chain — refreshing as root.");
                    tag.RefreshCache();
                }
            }

            Debug.Log($"[TagBootTask] Rebuilt {allTags.Count} tag caches.");
        }
    }
}
