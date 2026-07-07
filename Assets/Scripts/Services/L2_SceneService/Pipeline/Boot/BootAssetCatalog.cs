using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Holds all boot-loaded assets, grouped by type.
    ///
    /// Populated once by BootPipeline after a single Addressables load.
    /// Keeps a strong C# reference to all loaded assets (_allAssets) so
    /// the Unity native GC never sees them as unreferenced.
    ///
    /// Each boot task calls Get&lt;T&gt;() to extract the assets it needs.
    /// </summary>
    public class BootAssetCatalog
    {
        private readonly List<UnityEngine.Object> _allAssets;

        public BootAssetCatalog(IList<UnityEngine.Object> allAssets)
        {
            _allAssets = new List<UnityEngine.Object>(allAssets.Count);
            foreach (var a in allAssets)
            {
                if (a == null) continue;
                _allAssets.Add(a);
            }
        }

        /// <summary>Return all assets assignable to T (including subclasses).</summary>
        public List<T> Get<T>() where T : UnityEngine.Object
        {
            var result = new List<T>();
            var targetType = typeof(T);

            foreach (var a in _allAssets)
            {
                if (a is T t)
                    result.Add(t);
            }

            return result;
        }

        public int TotalCount => _allAssets.Count;
    }
}
