using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: rebuilds FullTag caches for all RdTagDefSOs in the catalog
    /// in BFS root-first order, fixing build-side OnEnable deserialization ordering.
    /// </summary>
    public class TagBootTask : IBootTask
    {
        public string Description => "Rebuilding tag caches...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var tags = catalog.Get<RdTagDefSO>();
            RebuildAllCaches(tags);
        }

        // --- RebuildAllCaches (same as before, but takes List<RdTagDefSO>) ---

        public static void RebuildAllCaches(List<RdTagDefSO> loadedTags)
        {
            var allInMemory = Resources.FindObjectsOfTypeAll<RdTagDefSO>();
            var allTags = new HashSet<RdTagDefSO>(allInMemory);
            int loadedCount = loadedTags.Count;
            int totalInMemory = allTags.Count;
            int missedByLabel = totalInMemory - loadedCount;

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
                    Debug.LogWarning($"[TagBootTask] Tag '{tag.name}' (leaf='{tag.LeafName}') has broken parent chain — refreshing as root.");
                    tag.RefreshCache();
                }
            }

            Debug.Log($"[TagBootTask] Rebuilt {refreshed.Count} tag caches (loaded={loadedCount}, totalInMemory={totalInMemory}, missedByLabel={missedByLabel}).");

            if (missedByLabel > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[TagBootTask] {missedByLabel} tags were loaded via scene references (not Addressables label):");
                foreach (var tag in allTags)
                {
                    if (!loadedTags.Contains(tag))
                        sb.Append($"\n  {tag.name} → FullTag='{tag.FullTag}'  depth={tag.Depth}");
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}
