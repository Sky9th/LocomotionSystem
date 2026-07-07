using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Finalize task: re-scans ALL RdTagDefSOs in memory (including those
    /// loaded as dependencies by later tasks) and rebuilds FullTag caches.
    /// </summary>
    public class TagFinalizeTask : IBootTask
    {
        public string Description => "Finalizing tag caches...";

        public void Resolve(BootAssetCatalog catalog)
        {
            // Re-run with all tags currently in memory
            var allInMemory = new List<RdTagDefSO>(Resources.FindObjectsOfTypeAll<RdTagDefSO>());
            TagBootTask.RebuildAllCaches(allInMemory);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[TagFinalizeTask] === Final pass: {allInMemory.Count} RdTagDefSOs in memory ===");
            foreach (var t in allInMemory)
                sb.AppendLine($"  {t.FullTag}  depth={t.Depth}  parent='{(t.Parent != null ? t.Parent.FullTag : "(root)")}'");
            Debug.Log(sb.ToString());
        }
    }
}
